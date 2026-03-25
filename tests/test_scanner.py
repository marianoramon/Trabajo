"""Tests para el escáner de archivos."""

import tempfile
import time
from pathlib import Path

import pytest

from core.models import AppConfig
from core.scanner import FileScanner
from core.code_mapper import CodeMapper


@pytest.fixture
def temp_project(tmp_path):
    """Crea una estructura de proyecto temporal para testing."""
    ipt_dir = tmp_path / "piezas"
    dxf_dir = tmp_path / "corte"
    dwf_dir = tmp_path / "planos"
    bending_dir = tmp_path / "plegado"

    ipt_dir.mkdir()
    dxf_dir.mkdir()
    dwf_dir.mkdir()
    bending_dir.mkdir()

    # Crear archivos de prueba
    (ipt_dir / "A01955.ipt").write_text("ipt content")
    time.sleep(0.05)
    (dxf_dir / "A01955.dxf").write_text("dxf content")
    (dwf_dir / "A01955.idw").write_text("idw content")

    (ipt_dir / "A02000.ipt").write_text("ipt content 2")
    # Sin DXF ni IDW para A02000

    config = AppConfig(
        ipt_folder=str(ipt_dir),
        dxf_folder=str(dxf_dir),
        dwf_folder=str(dwf_dir),
        ipt_bending_copy_folder=str(bending_dir),
    )

    mapper = CodeMapper(str(tmp_path / "mappings.csv"))

    return config, mapper, tmp_path


def test_scan_ipt_files(temp_project):
    config, mapper, _ = temp_project
    scanner = FileScanner(config, mapper)
    files = scanner.scan_ipt_files()
    assert len(files) == 2
    names = {f.name for f in files}
    assert "A01955.ipt" in names
    assert "A02000.ipt" in names


def test_scan_single_file_found(temp_project):
    config, mapper, _ = temp_project
    scanner = FileScanner(config, mapper)
    ipt_path = Path(config.ipt_folder) / "A01955.ipt"
    result = scanner.scan_single_file(ipt_path)

    assert result.codigo == "A01955"
    assert result.dxf_path is not None
    assert result.dxf_path.name == "A01955.dxf"
    assert result.idw_path is not None
    assert result.idw_path.name == "A01955.idw"


def test_scan_single_file_missing(temp_project):
    config, mapper, _ = temp_project
    scanner = FileScanner(config, mapper)
    ipt_path = Path(config.ipt_folder) / "A02000.ipt"
    result = scanner.scan_single_file(ipt_path)

    assert result.codigo == "A02000"
    assert result.dxf_path is None
    assert result.idw_path is None


def test_scan_all(temp_project):
    config, mapper, _ = temp_project
    scanner = FileScanner(config, mapper)
    results = scanner.scan_all()
    assert len(results) == 2


def test_ipt_copy_needed_when_missing(temp_project):
    config, mapper, _ = temp_project
    scanner = FileScanner(config, mapper)
    ipt_path = Path(config.ipt_folder) / "A01955.ipt"
    result = scanner.scan_single_file(ipt_path)

    assert result.ipt_copy_needed is True
    assert result.ipt_copy_target is not None


def test_ipt_copy_not_needed_when_exists(temp_project):
    config, mapper, tmp_path = temp_project
    # Copiar el IPT a la carpeta de plegado
    bending_dir = Path(config.ipt_bending_copy_folder)
    ipt_path = Path(config.ipt_folder) / "A01955.ipt"
    import shutil
    shutil.copy2(str(ipt_path), str(bending_dir / "A01955.ipt"))

    scanner = FileScanner(config, mapper)
    result = scanner.scan_single_file(ipt_path)

    assert result.ipt_copy_needed is False


def test_empty_folder(tmp_path):
    empty_dir = tmp_path / "empty"
    empty_dir.mkdir()

    config = AppConfig(
        ipt_folder=str(empty_dir),
        dxf_folder=str(empty_dir),
        dwf_folder=str(empty_dir),
    )
    mapper = CodeMapper(str(tmp_path / "mappings.csv"))
    scanner = FileScanner(config, mapper)

    assert scanner.scan_ipt_files() == []
    assert scanner.scan_all() == []
