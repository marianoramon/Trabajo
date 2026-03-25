"""Gestión de copias de archivos IPT a la carpeta de plegado."""

import shutil
from pathlib import Path
from datetime import datetime
from dataclasses import dataclass, field

from core.models import FileCheckResult


@dataclass
class CopyOperation:
    """Registro de una operación de copia."""
    source: Path
    destination: Path
    timestamp: datetime
    success: bool
    error: str = ""


class FileManager:
    """Gestiona la copia de archivos IPT a la carpeta de plegado."""

    def __init__(self):
        self.history: list[CopyOperation] = []

    def copy_ipt_for_bending(self, result: FileCheckResult) -> CopyOperation:
        """Copia un archivo IPT a la carpeta de plegado."""
        if not result.ipt_copy_target:
            op = CopyOperation(
                source=result.ipt_path,
                destination=Path(""),
                timestamp=datetime.now(),
                success=False,
                error="No hay carpeta destino configurada",
            )
            self.history.append(op)
            return op

        try:
            # Crear carpeta destino si no existe
            result.ipt_copy_target.parent.mkdir(parents=True, exist_ok=True)

            shutil.copy2(str(result.ipt_path), str(result.ipt_copy_target))

            op = CopyOperation(
                source=result.ipt_path,
                destination=result.ipt_copy_target,
                timestamp=datetime.now(),
                success=True,
            )
            result.ipt_copy_needed = False

        except Exception as e:
            op = CopyOperation(
                source=result.ipt_path,
                destination=result.ipt_copy_target,
                timestamp=datetime.now(),
                success=False,
                error=str(e),
            )

        self.history.append(op)
        return op

    def copy_all_pending(
        self, results: list[FileCheckResult]
    ) -> list[CopyOperation]:
        """Copia todos los IPT que necesitan actualización."""
        operations = []
        for result in results:
            if result.ipt_copy_needed and result.ipt_copy_target:
                op = self.copy_ipt_for_bending(result)
                operations.append(op)
        return operations

    def get_history_as_dicts(self) -> list[dict]:
        """Devuelve el historial de copias como lista de diccionarios."""
        return [
            {
                "Origen": str(op.source),
                "Destino": str(op.destination),
                "Fecha": op.timestamp.strftime("%Y-%m-%d %H:%M:%S"),
                "Estado": "OK" if op.success else f"ERROR: {op.error}",
            }
            for op in self.history
        ]
