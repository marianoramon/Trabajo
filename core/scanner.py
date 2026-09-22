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

        La extensión se compara sin distinguir mayúsculas ('.idw', '.IDW',
        '.Idw'). Se prefiere el nombre exacto; después el código al inicio del
        nombre; y por último el código como palabra dentro del nombre
        ('PLANO A01957.idw').
        """
        if not folder.exists() or not code:
            return None

        code_upper = code.upper()
        exts = {
            (ext if ext.startswith(".") else f".{ext}").lower()
            for ext in extensions
        }

        exact: list[Path] = []
        by_prefix: list[Path] = []
        by_token: list[Path] = []

        for f in sorted(folder.rglob("*")):
            if f.suffix.lower() not in exts or not f.is_file():
                continue

            if f.stem.upper() == code_upper:
                exact.append(f)
            elif CodeMapper.extract_code_from_filename(f.name) == code_upper:
                by_prefix.append(f)
            elif CodeMapper.code_in_filename(f.name, code_upper):
                by_token.append(f)

        for candidates in (exact, by_prefix, by_token):
            if candidates:
                return candidates[0]

        return None

    def _get_mtime(self, path: Path) -> datetime:
        """Obtiene la fecha de modificación de un archivo."""
        return datetime.fromtimestamp(path.stat().st_mtime)

    def scan_ipt_files(self) -> list[Path]:
        """Encuentra todos los archivos .ipt en la carpeta de piezas."""
        ipt_folder = Path(self.config.ipt_folder)
        if not ipt_folder.exists():
            return []
        return sorted(
            f for f in ipt_folder.rglob("*")
            if f.suffix.lower() == ".ipt" and f.is_file()
        )

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
        dxf_path = self._find_file_by_code(dxf_folder, dxf_code, [".dxf"])
        if dxf_path:
            result.dxf_path = dxf_path
            result.dxf_modified = self._get_mtime(dxf_path)

        # Buscar IDW (plano)
        drawing_code = self.code_mapper.get_drawing_code(codigo)
        dwf_folder = Path(self.config.dwf_folder)
        idw_path = self._find_file_by_code(dwf_folder, drawing_code, [".idw"])
        if idw_path:
            result.idw_path = idw_path
            result.idw_modified = self._get_mtime(idw_path)

        # Buscar DWF
        dwf_path = self._find_file_by_code(dwf_folder, drawing_code, [".dwf"])
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
