"""Tests para el validador de DXF."""

import tempfile
from pathlib import Path

import pytest

from core.models import DxfIssueType
from core.dxf_validator import DxfValidator

try:
    import ezdxf
    EZDXF_AVAILABLE = True
except ImportError:
    EZDXF_AVAILABLE = False


def _create_dxf_with_closed_polyline(path: Path):
    """Crea un DXF con una polilínea cerrada (sin errores)."""
    doc = ezdxf.new()
    msp = doc.modelspace()
    msp.add_lwpolyline(
        [(0, 0), (100, 0), (100, 100), (0, 100)],
        close=True,
    )
    doc.saveas(str(path))


def _create_dxf_with_open_polyline(path: Path):
    """Crea un DXF con una polilínea abierta."""
    doc = ezdxf.new()
    msp = doc.modelspace()
    msp.add_lwpolyline(
        [(0, 0), (100, 0), (100, 100), (0, 50)],
        close=False,
    )
    doc.saveas(str(path))


def _create_dxf_with_zero_length_line(path: Path):
    """Crea un DXF con una línea de longitud cero."""
    doc = ezdxf.new()
    msp = doc.modelspace()
    msp.add_lwpolyline([(0, 0), (100, 0), (100, 100), (0, 100)], close=True)
    msp.add_line((50, 50), (50, 50))
    doc.saveas(str(path))


def _create_dxf_with_duplicate_lines(path: Path):
    """Crea un DXF con líneas duplicadas."""
    doc = ezdxf.new()
    msp = doc.modelspace()
    msp.add_line((0, 0), (100, 100))
    msp.add_line((0, 0), (100, 100))  # duplicada
    doc.saveas(str(path))


def _create_empty_dxf(path: Path):
    """Crea un DXF sin geometría."""
    doc = ezdxf.new()
    doc.saveas(str(path))


@pytest.mark.skipif(not EZDXF_AVAILABLE, reason="ezdxf no instalado")
class TestDxfValidator:
    def test_closed_polyline_no_issues(self, tmp_path):
        dxf_file = tmp_path / "closed.dxf"
        _create_dxf_with_closed_polyline(dxf_file)

        validator = DxfValidator()
        issues = validator.validate(dxf_file)

        contour_issues = [i for i in issues if i.issue_type == DxfIssueType.UNCLOSED_CONTOUR]
        assert len(contour_issues) == 0

    def test_open_polyline_detected(self, tmp_path):
        dxf_file = tmp_path / "open.dxf"
        _create_dxf_with_open_polyline(dxf_file)

        validator = DxfValidator()
        issues = validator.validate(dxf_file)

        contour_issues = [i for i in issues if i.issue_type == DxfIssueType.UNCLOSED_CONTOUR]
        assert len(contour_issues) >= 1

    def test_zero_length_line_detected(self, tmp_path):
        dxf_file = tmp_path / "zero_len.dxf"
        _create_dxf_with_zero_length_line(dxf_file)

        validator = DxfValidator()
        issues = validator.validate(dxf_file)

        zero_issues = [i for i in issues if i.issue_type == DxfIssueType.ZERO_LENGTH]
        assert len(zero_issues) >= 1

    def test_duplicate_lines_detected(self, tmp_path):
        dxf_file = tmp_path / "dupes.dxf"
        _create_dxf_with_duplicate_lines(dxf_file)

        validator = DxfValidator()
        issues = validator.validate(dxf_file)

        dup_issues = [i for i in issues if i.issue_type == DxfIssueType.DUPLICATE]
        assert len(dup_issues) >= 1

    def test_empty_dxf_detected(self, tmp_path):
        dxf_file = tmp_path / "empty.dxf"
        _create_empty_dxf(dxf_file)

        validator = DxfValidator()
        issues = validator.validate(dxf_file)

        no_geom_issues = [i for i in issues if i.issue_type == DxfIssueType.NO_GEOMETRY]
        assert len(no_geom_issues) >= 1

    def test_nonexistent_file(self, tmp_path):
        dxf_file = tmp_path / "nonexistent.dxf"

        validator = DxfValidator()
        issues = validator.validate(dxf_file)

        structural_issues = [i for i in issues if i.issue_type == DxfIssueType.STRUCTURAL]
        assert len(structural_issues) >= 1

    def test_corrupt_file(self, tmp_path):
        dxf_file = tmp_path / "corrupt.dxf"
        dxf_file.write_text("this is not a valid DXF file")

        validator = DxfValidator()
        issues = validator.validate(dxf_file)

        structural_issues = [i for i in issues if i.issue_type == DxfIssueType.STRUCTURAL]
        assert len(structural_issues) >= 1
