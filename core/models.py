"""Modelos de datos para la herramienta de verificación de piezas CAD."""

from dataclasses import dataclass, field
from enum import Enum
from pathlib import Path
from datetime import datetime
from typing import Optional


class SyncStatus(Enum):
    """Estado de sincronización de un archivo respecto al .ipt fuente."""
    OK = "OK"
    OUTDATED = "DESACTUALIZADO"
    MISSING = "FALTANTE"
    ERROR = "ERROR"


class DxfIssueType(Enum):
    """Tipos de problemas detectados en archivos DXF."""
    UNCLOSED_CONTOUR = "Contorno abierto"
    ZERO_LENGTH = "Entidad longitud cero"
    DUPLICATE = "Entidad duplicada"
    NO_GEOMETRY = "Sin geometría"
    STRUCTURAL = "Error estructural DXF"
    TINY_SEGMENT = "Segmento diminuto"


@dataclass
class DxfIssue:
    """Un problema detectado en un archivo DXF."""
    issue_type: DxfIssueType
    description: str
    entity_type: Optional[str] = None
    layer: Optional[str] = None


@dataclass
class CodeMapping:
    """Mapeo entre código de pieza, código de corte y código de plegado."""
    codigo_pieza: str
    codigo_corte: str
    codigo_plegado: str


@dataclass
class FileCheckResult:
    """Resultado de la verificación de una pieza."""
    codigo: str
    ipt_path: Path
    ipt_modified: datetime

    # DXF para corte
    dxf_path: Optional[Path] = None
    dxf_modified: Optional[datetime] = None
    dxf_status: SyncStatus = SyncStatus.MISSING

    # Plano IDW
    idw_path: Optional[Path] = None
    idw_modified: Optional[datetime] = None
    idw_status: SyncStatus = SyncStatus.MISSING

    # DWF para operario
    dwf_path: Optional[Path] = None
    dwf_modified: Optional[datetime] = None
    dwf_status: SyncStatus = SyncStatus.MISSING

    # Copia de IPT a carpeta de plegado
    ipt_copy_needed: bool = False
    ipt_copy_target: Optional[Path] = None

    # Problemas en DXF
    dxf_issues: list[DxfIssue] = field(default_factory=list)

    @property
    def has_dxf_problems(self) -> bool:
        return len(self.dxf_issues) > 0

    @property
    def overall_ok(self) -> bool:
        return (
            self.dxf_status == SyncStatus.OK
            and self.idw_status == SyncStatus.OK
            and not self.has_dxf_problems
            and not self.ipt_copy_needed
        )


@dataclass
class ProjectReport:
    """Informe completo de verificación de un proyecto."""
    project_name: str
    results: list[FileCheckResult] = field(default_factory=list)
    scan_timestamp: datetime = field(default_factory=datetime.now)

    @property
    def total_files(self) -> int:
        return len(self.results)

    @property
    def ok_count(self) -> int:
        return sum(1 for r in self.results if r.overall_ok)

    @property
    def outdated_dxf_count(self) -> int:
        return sum(1 for r in self.results if r.dxf_status == SyncStatus.OUTDATED)

    @property
    def outdated_idw_count(self) -> int:
        return sum(1 for r in self.results if r.idw_status == SyncStatus.OUTDATED)

    @property
    def missing_dxf_count(self) -> int:
        return sum(1 for r in self.results if r.dxf_status == SyncStatus.MISSING)

    @property
    def missing_idw_count(self) -> int:
        return sum(1 for r in self.results if r.idw_status == SyncStatus.MISSING)

    @property
    def dxf_with_issues_count(self) -> int:
        return sum(1 for r in self.results if r.has_dxf_problems)

    @property
    def copies_needed_count(self) -> int:
        return sum(1 for r in self.results if r.ipt_copy_needed)


@dataclass
class AppConfig:
    """Configuración de la aplicación."""
    ipt_folder: str = ""
    dxf_folder: str = ""
    dwf_folder: str = ""
    ipt_bending_copy_folder: str = ""
    timestamp_tolerance_seconds: int = 60
    mappings_file: str = "mappings.csv"
