"""Verificador de sincronización - compara timestamps entre archivos."""

from core.models import FileCheckResult, SyncStatus


class SyncChecker:
    """Compara fechas de modificación para determinar si los archivos están actualizados."""

    def __init__(self, tolerance_seconds: int = 60):
        self.tolerance_seconds = tolerance_seconds

    def check(self, result: FileCheckResult) -> FileCheckResult:
        """Actualiza los estados de sincronización de un FileCheckResult."""
        ipt_ts = result.ipt_modified.timestamp()

        # Verificar DXF
        if result.dxf_path and result.dxf_modified:
            dxf_ts = result.dxf_modified.timestamp()
            if ipt_ts - dxf_ts > self.tolerance_seconds:
                result.dxf_status = SyncStatus.OUTDATED
            else:
                result.dxf_status = SyncStatus.OK
        elif result.dxf_path is None:
            result.dxf_status = SyncStatus.MISSING

        # Verificar IDW
        if result.idw_path and result.idw_modified:
            idw_ts = result.idw_modified.timestamp()
            if ipt_ts - idw_ts > self.tolerance_seconds:
                result.idw_status = SyncStatus.OUTDATED
            else:
                result.idw_status = SyncStatus.OK
        elif result.idw_path is None:
            result.idw_status = SyncStatus.MISSING

        # Verificar DWF
        if result.dwf_path and result.dwf_modified:
            dwf_ts = result.dwf_modified.timestamp()
            if ipt_ts - dwf_ts > self.tolerance_seconds:
                result.dwf_status = SyncStatus.OUTDATED
            else:
                result.dwf_status = SyncStatus.OK
        elif result.dwf_path is None:
            result.dwf_status = SyncStatus.MISSING

        return result

    def check_all(self, results: list[FileCheckResult]) -> list[FileCheckResult]:
        """Verifica la sincronización de todos los resultados."""
        return [self.check(r) for r in results]
