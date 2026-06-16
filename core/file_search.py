"""Buscador de ficheros de Autodesk Inventor a través de la red.

Recorre una o varias carpetas (rutas de red UNC o unidades mapeadas) y
encuentra los ficheros de Inventor y relacionados que cumplan los filtros
de búsqueda (nombre, tipo, fecha de modificación y tamaño).
"""

from __future__ import annotations

import os
from dataclasses import dataclass, field
from datetime import datetime
from pathlib import Path
from typing import Iterable, Optional


# Tipos de fichero de Autodesk Inventor y relacionados, con su etiqueta.
# La clave es la extensión en minúsculas (con el punto).
INVENTOR_FILE_TYPES: dict[str, str] = {
    ".ipt": "Pieza (IPT)",
    ".iam": "Ensamblaje (IAM)",
    ".idw": "Plano (IDW)",
    ".dwg": "Plano (DWG)",
    ".ipn": "Presentación (IPN)",
    ".ipj": "Proyecto (IPJ)",
    ".dwf": "Publicado (DWF)",
    ".dwfx": "Publicado (DWFX)",
    ".dxf": "Corte (DXF)",
    ".step": "Intercambio (STEP)",
    ".stp": "Intercambio (STP)",
    ".igs": "Intercambio (IGS)",
    ".iges": "Intercambio (IGES)",
    ".sat": "Intercambio (SAT)",
    ".pdf": "Documento (PDF)",
}


@dataclass
class SearchResult:
    """Un fichero encontrado durante la búsqueda."""

    path: Path
    name: str
    extension: str
    size_bytes: int
    modified: datetime
    root: Path

    @property
    def type_label(self) -> str:
        """Etiqueta legible del tipo de fichero."""
        return INVENTOR_FILE_TYPES.get(self.extension, self.extension.upper().lstrip("."))

    @property
    def size_human(self) -> str:
        """Tamaño del fichero en formato legible."""
        size = float(self.size_bytes)
        for unit in ("B", "KB", "MB", "GB", "TB"):
            if size < 1024 or unit == "TB":
                return f"{size:.0f} {unit}" if unit == "B" else f"{size:.1f} {unit}"
            size /= 1024
        return f"{size:.1f} TB"


@dataclass
class SearchCriteria:
    """Criterios de búsqueda de ficheros."""

    # Texto que debe contener el nombre del fichero (sin distinguir mayúsculas).
    query: str = ""
    # Extensiones a incluir (en minúsculas con punto). Vacío = todos los Inventor.
    extensions: list[str] = field(default_factory=list)
    # Filtrar por fecha de modificación.
    modified_after: Optional[datetime] = None
    modified_before: Optional[datetime] = None
    # Tamaño mínimo en bytes (None = sin límite).
    min_size_bytes: Optional[int] = None
    # Límite máximo de resultados para evitar búsquedas enormes.
    max_results: int = 1000

    def active_extensions(self) -> set[str]:
        """Extensiones efectivas: las indicadas o todas las de Inventor."""
        if self.extensions:
            return {e.lower() for e in self.extensions}
        return set(INVENTOR_FILE_TYPES.keys())


@dataclass
class SearchReport:
    """Resultado de una búsqueda: ficheros encontrados y metadatos."""

    results: list[SearchResult] = field(default_factory=list)
    scanned_files: int = 0
    truncated: bool = False
    errors: list[str] = field(default_factory=list)
    duration_seconds: float = 0.0

    @property
    def total_found(self) -> int:
        return len(self.results)


def _matches(result_name: str, query: str) -> bool:
    """Comprueba si el nombre contiene el texto buscado (case-insensitive)."""
    if not query:
        return True
    return query.lower() in result_name.lower()


class FileSearchEngine:
    """Busca ficheros de Inventor en una o varias carpetas de red."""

    def __init__(self, roots: Iterable[str | Path]):
        # Conserva solo rutas no vacías, eliminando duplicados.
        seen: set[str] = set()
        self.roots: list[Path] = []
        for r in roots:
            r_str = str(r).strip()
            if r_str and r_str not in seen:
                seen.add(r_str)
                self.roots.append(Path(r_str))

    def search(self, criteria: SearchCriteria) -> SearchReport:
        """Ejecuta la búsqueda según los criterios indicados."""
        report = SearchReport()
        start = datetime.now()
        extensions = criteria.active_extensions()

        for root in self.roots:
            if not root.exists():
                report.errors.append(f"Carpeta no encontrada: {root}")
                continue
            self._search_root(root, criteria, extensions, report)
            if report.truncated:
                break

        # Resultados más recientes primero.
        report.results.sort(key=lambda r: r.modified, reverse=True)
        report.duration_seconds = (datetime.now() - start).total_seconds()
        return report

    def _search_root(
        self,
        root: Path,
        criteria: SearchCriteria,
        extensions: set[str],
        report: SearchReport,
    ) -> None:
        """Recorre recursivamente una carpeta acumulando coincidencias."""
        # os.walk es más robusto que rglob ante errores de permisos en red.
        for dirpath, _dirnames, filenames in os.walk(root, onerror=report.errors.append):
            for filename in filenames:
                ext = os.path.splitext(filename)[1].lower()
                if ext not in extensions:
                    continue
                if not _matches(filename, criteria.query):
                    continue

                full = Path(dirpath) / filename
                try:
                    stat = full.stat()
                except OSError as exc:
                    report.errors.append(f"No accesible: {full} ({exc})")
                    continue

                report.scanned_files += 1
                modified = datetime.fromtimestamp(stat.st_mtime)

                if criteria.modified_after and modified < criteria.modified_after:
                    continue
                if criteria.modified_before and modified > criteria.modified_before:
                    continue
                if criteria.min_size_bytes is not None and stat.st_size < criteria.min_size_bytes:
                    continue

                report.results.append(
                    SearchResult(
                        path=full,
                        name=filename,
                        extension=ext,
                        size_bytes=stat.st_size,
                        modified=modified,
                        root=root,
                    )
                )

                if len(report.results) >= criteria.max_results:
                    report.truncated = True
                    return
