"""Escáner de archivos del proyecto - busca IPT, DXF, IDW, DWF."""

from pathlib import Path
from datetime import datetime
from typing import Optional

from core.models import AppConfig, FileCheckResult, SyncStatus
from core.code_mapper import CodeMapper


class FileScanner:
    """Escanea las carpetas del proyecto y encuentra archivos relacionados."""

    def __init__(self, config: AppConfig, code_mapper: CodeMapper):
        self.config = config
        self.code_mapper = code_mapper

    def _find_file_by_code(
        self, folder: Path, code: str, extensions: list[str]
    ) -> Optional[Path]:
        """Busca un archivo por código en una carpeta (recursivamente).

        Busca archivos cuyo nombre empiece con el código dado.
        """
        if not folder.exists():
            return None

        code_upper = code.upper()
        for ext in extensions:
            # Búsqueda directa: código.ext
            direct = folder / f"{code}{ext}"
            if direct.exists():
                return direct

            # Búsqueda recursiva
            for f in folder.rglob(f"*{ext}"):
                file_code = CodeMapper.extract_code_from_filename(f.name)
                if file_code == code_upper:
                    return f

        return None

    def _get_mtime(self, path: Path) -> datetime:
        """Obtiene la fecha de modificación de un archivo."""
        return datetime.fromtimestamp(path.stat().st_mtime)

    def scan_ipt_files(self) -> list[Path]:
        """Encuentra todos los archivos .ipt en la carpeta de piezas."""
        ipt_folder = Path(self.config.ipt_folder)
        if not ipt_folder.exists():
            return []
        return sorted(ipt_folder.rglob("*.ipt"))

    def scan_single_file(self, ipt_path: Path) -> FileCheckResult:
        """Verifica una única pieza .ipt y busca sus archivos relacionados."""
        codigo = CodeMapper.extract_code_from_filename(ipt_path.name)
        ipt_modified = self._get_mtime(ipt_path)

        result = FileCheckResult(
            codigo=codigo,
            ipt_path=ipt_path,
            ipt_modified=ipt_modified,
        )

        # Buscar DXF
        dxf_code = self.code_mapper.get_dxf_code(codigo)
        dxf_folder = Path(self.config.dxf_folder)
        dxf_path = self._find_file_by_code(dxf_folder, dxf_code, [".dxf", ".DXF"])
        if dxf_path:
            result.dxf_path = dxf_path
            result.dxf_modified = self._get_mtime(dxf_path)

        # Buscar IDW (plano)
        drawing_code = self.code_mapper.get_drawing_code(codigo)
        dwf_folder = Path(self.config.dwf_folder)
        idw_path = self._find_file_by_code(dwf_folder, drawing_code, [".idw", ".IDW"])
        if idw_path:
            result.idw_path = idw_path
            result.idw_modified = self._get_mtime(idw_path)

        # Buscar DWF
        dwf_path = self._find_file_by_code(dwf_folder, drawing_code, [".dwf", ".DWF"])
        if dwf_path:
            result.dwf_path = dwf_path
            result.dwf_modified = self._get_mtime(dwf_path)

        # Verificar copia de IPT para plegado
        if self.config.ipt_bending_copy_folder:
            bending_folder = Path(self.config.ipt_bending_copy_folder)
            result.ipt_copy_target = bending_folder / ipt_path.name
            if result.ipt_copy_target.exists():
                copy_mtime = self._get_mtime(result.ipt_copy_target)
                result.ipt_copy_needed = ipt_modified > copy_mtime
            else:
                result.ipt_copy_needed = True

        return result

    def scan_all(self) -> list[FileCheckResult]:
        """Escanea todas las piezas y devuelve los resultados."""
        ipt_files = self.scan_ipt_files()
        results = []
        for ipt_path in ipt_files:
            result = self.scan_single_file(ipt_path)
            results.append(result)
        return results
