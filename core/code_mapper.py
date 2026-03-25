"""Mapeo de códigos entre piezas, corte y plegado."""

import csv
import re
from pathlib import Path
from typing import Optional

from core.models import CodeMapping


class CodeMapper:
    """Gestiona la correspondencia entre códigos de pieza, corte y plegado.

    Sistema nuevo (A00280+): mismo código para corte y plegado.
    Sistema antiguo: códigos diferentes, se leen de mappings.csv.
    """

    def __init__(self, mappings_file: str = "mappings.csv"):
        self.mappings_file = Path(mappings_file)
        self.mappings: list[CodeMapping] = []
        self._index_by_pieza: dict[str, CodeMapping] = {}
        self._index_by_corte: dict[str, CodeMapping] = {}
        self._index_by_plegado: dict[str, CodeMapping] = {}
        self.load()

    def load(self):
        """Carga los mapeos desde el archivo CSV."""
        self.mappings.clear()
        self._index_by_pieza.clear()
        self._index_by_corte.clear()
        self._index_by_plegado.clear()

        if not self.mappings_file.exists():
            return

        with open(self.mappings_file, "r", encoding="utf-8-sig") as f:
            reader = csv.DictReader(f, delimiter=";")
            for row in reader:
                mapping = CodeMapping(
                    codigo_pieza=row.get("codigo_pieza", "").strip(),
                    codigo_corte=row.get("codigo_corte", "").strip(),
                    codigo_plegado=row.get("codigo_plegado", "").strip(),
                )
                self.mappings.append(mapping)
                self._index_by_pieza[mapping.codigo_pieza.upper()] = mapping
                if mapping.codigo_corte:
                    self._index_by_corte[mapping.codigo_corte.upper()] = mapping
                if mapping.codigo_plegado:
                    self._index_by_plegado[mapping.codigo_plegado.upper()] = mapping

    def save(self):
        """Guarda los mapeos al archivo CSV."""
        with open(self.mappings_file, "w", encoding="utf-8", newline="") as f:
            writer = csv.DictWriter(
                f,
                fieldnames=["codigo_pieza", "codigo_corte", "codigo_plegado"],
                delimiter=";",
            )
            writer.writeheader()
            for m in self.mappings:
                writer.writerow({
                    "codigo_pieza": m.codigo_pieza,
                    "codigo_corte": m.codigo_corte,
                    "codigo_plegado": m.codigo_plegado,
                })

    @staticmethod
    def is_new_system(code: str) -> bool:
        """Determina si un código pertenece al sistema nuevo (A00280+)."""
        match = re.match(r"^[Aa](\d+)", code)
        if match:
            num = int(match.group(1))
            return num >= 280
        return False

    @staticmethod
    def extract_code_from_filename(filename: str) -> str:
        """Extrae el código de pieza del nombre de archivo.

        Ejemplos:
            'A01955.ipt' -> 'A01955'
            'A01955_desarrollo.dxf' -> 'A01955'
            '45017.ipt' -> '45017'
            '45017-SOPORTE.ipt' -> '45017'
        """
        stem = Path(filename).stem
        # Intenta extraer código alfanumérico al inicio
        match = re.match(r"^([Aa]\d+|\d+)", stem)
        if match:
            return match.group(1).upper()
        return stem.upper()

    def get_dxf_code(self, pieza_code: str) -> str:
        """Obtiene el código DXF (corte) para un código de pieza.

        Si es sistema nuevo, devuelve el mismo código.
        Si es sistema antiguo, busca en la tabla de mapeos.
        """
        code_upper = pieza_code.upper()

        if self.is_new_system(pieza_code):
            return pieza_code

        # Buscar en mapeos
        mapping = self._index_by_pieza.get(code_upper)
        if mapping and mapping.codigo_corte:
            return mapping.codigo_corte

        # Buscar por plegado (el código podría ser de plegado)
        mapping = self._index_by_plegado.get(code_upper)
        if mapping and mapping.codigo_corte:
            return mapping.codigo_corte

        # Sin mapeo, asumir mismo código
        return pieza_code

    def get_drawing_code(self, pieza_code: str) -> str:
        """Obtiene el código de plano (plegado) para un código de pieza.

        Si es sistema nuevo, devuelve el mismo código.
        Si es sistema antiguo, busca en la tabla de mapeos.
        """
        code_upper = pieza_code.upper()

        if self.is_new_system(pieza_code):
            return pieza_code

        # Buscar en mapeos
        mapping = self._index_by_pieza.get(code_upper)
        if mapping and mapping.codigo_plegado:
            return mapping.codigo_plegado

        # Buscar por corte (el código podría ser de corte)
        mapping = self._index_by_corte.get(code_upper)
        if mapping and mapping.codigo_plegado:
            return mapping.codigo_plegado

        # Sin mapeo, asumir mismo código
        return pieza_code

    def add_mapping(self, pieza: str, corte: str, plegado: str):
        """Añade un nuevo mapeo."""
        mapping = CodeMapping(
            codigo_pieza=pieza.strip(),
            codigo_corte=corte.strip(),
            codigo_plegado=plegado.strip(),
        )
        # Reemplazar si ya existe
        self.mappings = [
            m for m in self.mappings
            if m.codigo_pieza.upper() != pieza.upper()
        ]
        self.mappings.append(mapping)
        self._index_by_pieza[mapping.codigo_pieza.upper()] = mapping
        if mapping.codigo_corte:
            self._index_by_corte[mapping.codigo_corte.upper()] = mapping
        if mapping.codigo_plegado:
            self._index_by_plegado[mapping.codigo_plegado.upper()] = mapping

    def remove_mapping(self, pieza_code: str):
        """Elimina un mapeo por código de pieza."""
        code_upper = pieza_code.upper()
        mapping = self._index_by_pieza.get(code_upper)
        if mapping:
            self.mappings = [
                m for m in self.mappings
                if m.codigo_pieza.upper() != code_upper
            ]
            self._index_by_pieza.pop(code_upper, None)
            self._index_by_corte.pop(mapping.codigo_corte.upper(), None)
            self._index_by_plegado.pop(mapping.codigo_plegado.upper(), None)

    def get_all_mappings_as_dicts(self) -> list[dict]:
        """Devuelve todos los mapeos como lista de diccionarios (para DataFrame)."""
        return [
            {
                "Código Pieza": m.codigo_pieza,
                "Código Corte": m.codigo_corte,
                "Código Plegado": m.codigo_plegado,
            }
            for m in self.mappings
        ]
