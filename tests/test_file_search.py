"""Tests para el buscador de ficheros en la red."""

from datetime import datetime, timedelta

import pytest

from core.file_search import FileSearchEngine, SearchCriteria


@pytest.fixture
def network_folders(tmp_path):
    """Crea dos carpetas simulando recursos de red con ficheros Inventor."""
    disenos = tmp_path / "DISEÑOS"
    corte = tmp_path / "CORTE"
    sub = disenos / "60-PROYECTO"
    sub.mkdir(parents=True)
    corte.mkdir()

    (sub / "A01955-brida.ipt").write_text("ipt")
    (sub / "A01955-conjunto.iam").write_text("iam")
    (sub / "A02000-soporte.ipt").write_text("ipt")
    (sub / "plano-A01955.idw").write_text("idw")
    (sub / "notas.txt").write_text("texto no Inventor")
    (corte / "A01955.dxf").write_text("dxf")

    return [str(disenos), str(corte)]


def test_finds_inventor_files_recursively(network_folders):
    engine = FileSearchEngine(network_folders)
    report = engine.search(SearchCriteria())

    names = {r.name for r in report.results}
    assert "A01955-brida.ipt" in names
    assert "A01955.dxf" in names
    # El .txt no es un tipo de Inventor y se ignora.
    assert "notas.txt" not in names


def test_filter_by_query(network_folders):
    engine = FileSearchEngine(network_folders)
    report = engine.search(SearchCriteria(query="A01955"))

    # A01955-brida.ipt, A01955-conjunto.iam, plano-A01955.idw, A01955.dxf
    assert report.total_found == 4
    assert all("A01955" in r.name for r in report.results)


def test_filter_by_extension(network_folders):
    engine = FileSearchEngine(network_folders)
    report = engine.search(SearchCriteria(extensions=[".ipt"]))

    assert report.total_found == 2
    assert all(r.extension == ".ipt" for r in report.results)


def test_query_is_case_insensitive(network_folders):
    engine = FileSearchEngine(network_folders)
    report = engine.search(SearchCriteria(query="brida"))

    assert report.total_found == 1
    assert report.results[0].name == "A01955-brida.ipt"


def test_max_results_truncates(network_folders):
    engine = FileSearchEngine(network_folders)
    report = engine.search(SearchCriteria(max_results=2))

    assert report.truncated is True
    assert report.total_found == 2


def test_missing_folder_reports_error(tmp_path):
    engine = FileSearchEngine([str(tmp_path / "no-existe")])
    report = engine.search(SearchCriteria())

    assert report.total_found == 0
    assert any("no encontrada" in e.lower() for e in report.errors)


def test_filter_by_modified_after(network_folders):
    engine = FileSearchEngine(network_folders)
    future = datetime.now() + timedelta(days=1)
    report = engine.search(SearchCriteria(modified_after=future))

    assert report.total_found == 0


def test_results_sorted_by_modified_desc(network_folders):
    engine = FileSearchEngine(network_folders)
    report = engine.search(SearchCriteria())

    mods = [r.modified for r in report.results]
    assert mods == sorted(mods, reverse=True)


def test_duplicate_roots_are_deduplicated(network_folders):
    root = network_folders[0]
    engine = FileSearchEngine([root, root])
    assert len(engine.roots) == 1
