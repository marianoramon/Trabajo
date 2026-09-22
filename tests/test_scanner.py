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


@pytest.fixture
def planos_variados(tmp_path):
    """Proyecto con planos nombrados como en taller (prefijos, revisiones, mayúsculas)."""
    ipt_dir = tmp_path / "piezas"
    dxf_dir = tmp_path / "corte"
    dwf_dir = tmp_path / "planos"
    for d in (ipt_dir, dxf_dir, dwf_dir):
        d.mkdir()

    casos = {
        "A01956": "A01956_PLEGADO.idw",
        "A01957": "PLANO A01957.idw",
        "A01958": "A01958 REV B.IDW",
        "A01959": "A01959.Idw",
    }
    for codigo, plano in casos.items():
        (ipt_dir / f"{codigo}.ipt").write_text("ipt")
        (dwf_dir / plano).write_text("idw")

    # Pieza del sistema nuevo con mapeo explícito a otro código de plano
    (ipt_dir / "A01960.ipt").write_text("ipt")
    (dwf_dir / "P7788.idw").write_text("idw")
    mappings = tmp_path / "mappings.csv"
    mappings.write_text(
        "codigo_pieza;codigo_corte;codigo_plegado\nA01960;A01960;P7788\n",
        encoding="utf-8",
    )

    config = AppConfig(
        ipt_folder=str(ipt_dir),
        dxf_folder=str(dxf_dir),
        dwf_folder=str(dwf_dir),
    )
    return FileScanner(config, CodeMapper(str(mappings))), ipt_dir, casos


@pytest.mark.parametrize(
    "codigo,plano",
    [
        ("A01956", "A01956_PLEGADO.idw"),
        ("A01957", "PLANO A01957.idw"),
        ("A01958", "A01958 REV B.IDW"),
        ("A01959", "A01959.Idw"),
    ],
)
def test_encuentra_planos_con_nombres_de_taller(planos_variados, codigo, plano):
    scanner, ipt_dir, _ = planos_variados
    result = scanner.scan_single_file(ipt_dir / f"{codigo}.ipt")
    assert result.idw_path is not None, f"No se encontró el plano de {codigo}"
    assert result.idw_path.name == plano


def test_mapeo_explicito_gana_al_sistema_nuevo(planos_variados):
    scanner, ipt_dir, _ = planos_variados
    result = scanner.scan_single_file(ipt_dir / "A01960.ipt")
    assert result.idw_path is not None
    assert result.idw_path.name == "P7788.idw"


def test_no_confunde_codigos_parecidos(tmp_path):
    ipt_dir = tmp_path / "piezas"
    dwf_dir = tmp_path / "planos"
    ipt_dir.mkdir()
    dwf_dir.mkdir()
    (ipt_dir / "A01955.ipt").write_text("ipt")
    (dwf_dir / "A019551.idw").write_text("idw")

    config = AppConfig(
        ipt_folder=str(ipt_dir), dxf_folder=str(dwf_dir), dwf_folder=str(dwf_dir)
    )
    scanner = FileScanner(config, CodeMapper(str(tmp_path / "mappings.csv")))
    result = scanner.scan_single_file(ipt_dir / "A01955.ipt")
    assert result.idw_path is None


def test_scan_ipt_files_acepta_extension_en_mayusculas(tmp_path):
    ipt_dir = tmp_path / "piezas"
    ipt_dir.mkdir()
    (ipt_dir / "A01955.IPT").write_text("ipt")

    config = AppConfig(
        ipt_folder=str(ipt_dir), dxf_folder=str(ipt_dir), dwf_folder=str(ipt_dir)
    )
    scanner = FileScanner(config, CodeMapper(str(tmp_path / "mappings.csv")))
    assert len(scanner.scan_ipt_files()) == 1
