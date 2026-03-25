"""Tests para el verificador de sincronización."""

from datetime import datetime, timedelta
from pathlib import Path

import pytest

from core.models import FileCheckResult, SyncStatus
from core.sync_checker import SyncChecker


def _make_result(
    ipt_time: datetime,
    dxf_time: datetime | None = None,
    idw_time: datetime | None = None,
    dwf_time: datetime | None = None,
) -> FileCheckResult:
    """Crea un FileCheckResult de prueba con timestamps dados."""
    result = FileCheckResult(
        codigo="TEST001",
        ipt_path=Path("/fake/TEST001.ipt"),
        ipt_modified=ipt_time,
    )
    if dxf_time is not None:
        result.dxf_path = Path("/fake/TEST001.dxf")
        result.dxf_modified = dxf_time
    if idw_time is not None:
        result.idw_path = Path("/fake/TEST001.idw")
        result.idw_modified = idw_time
    if dwf_time is not None:
        result.dwf_path = Path("/fake/TEST001.dwf")
        result.dwf_modified = dwf_time
    return result


class TestSyncChecker:
    def test_dxf_ok_when_newer(self):
        now = datetime.now()
        result = _make_result(
            ipt_time=now - timedelta(hours=1),
            dxf_time=now,
        )
        checker = SyncChecker(tolerance_seconds=60)
        checked = checker.check(result)
        assert checked.dxf_status == SyncStatus.OK

    def test_dxf_outdated_when_older(self):
        now = datetime.now()
        result = _make_result(
            ipt_time=now,
            dxf_time=now - timedelta(hours=1),
        )
        checker = SyncChecker(tolerance_seconds=60)
        checked = checker.check(result)
        assert checked.dxf_status == SyncStatus.OUTDATED

    def test_dxf_missing(self):
        now = datetime.now()
        result = _make_result(ipt_time=now)
        checker = SyncChecker(tolerance_seconds=60)
        checked = checker.check(result)
        assert checked.dxf_status == SyncStatus.MISSING

    def test_idw_ok_when_newer(self):
        now = datetime.now()
        result = _make_result(
            ipt_time=now - timedelta(hours=1),
            idw_time=now,
        )
        checker = SyncChecker(tolerance_seconds=60)
        checked = checker.check(result)
        assert checked.idw_status == SyncStatus.OK

    def test_idw_outdated_when_older(self):
        now = datetime.now()
        result = _make_result(
            ipt_time=now,
            idw_time=now - timedelta(hours=1),
        )
        checker = SyncChecker(tolerance_seconds=60)
        checked = checker.check(result)
        assert checked.idw_status == SyncStatus.OUTDATED

    def test_tolerance_within_range(self):
        now = datetime.now()
        result = _make_result(
            ipt_time=now,
            dxf_time=now - timedelta(seconds=30),
        )
        checker = SyncChecker(tolerance_seconds=60)
        checked = checker.check(result)
        assert checked.dxf_status == SyncStatus.OK

    def test_tolerance_beyond_range(self):
        now = datetime.now()
        result = _make_result(
            ipt_time=now,
            dxf_time=now - timedelta(seconds=120),
        )
        checker = SyncChecker(tolerance_seconds=60)
        checked = checker.check(result)
        assert checked.dxf_status == SyncStatus.OUTDATED

    def test_check_all(self):
        now = datetime.now()
        results = [
            _make_result(ipt_time=now, dxf_time=now - timedelta(hours=1)),
            _make_result(ipt_time=now, dxf_time=now),
        ]
        checker = SyncChecker(tolerance_seconds=60)
        checked = checker.check_all(results)
        assert checked[0].dxf_status == SyncStatus.OUTDATED
        assert checked[1].dxf_status == SyncStatus.OK

    def test_dwf_sync_check(self):
        now = datetime.now()
        result = _make_result(
            ipt_time=now,
            dwf_time=now - timedelta(hours=2),
        )
        checker = SyncChecker(tolerance_seconds=60)
        checked = checker.check(result)
        assert checked.dwf_status == SyncStatus.OUTDATED
