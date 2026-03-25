"""Página principal de verificación de piezas."""

import pandas as pd
import streamlit as st

from core.models import SyncStatus, FileCheckResult, ProjectReport
from core.scanner import FileScanner
from core.sync_checker import SyncChecker
from core.dxf_validator import DxfValidator
from core.file_manager import FileManager
from core.code_mapper import CodeMapper
from ui.pages.configuracion import load_config
from ui.components import status_badge, metric_card


def _status_emoji(status: SyncStatus) -> str:
    icons = {
        SyncStatus.OK: "✅",
        SyncStatus.OUTDATED: "🔴",
        SyncStatus.MISSING: "⚪",
        SyncStatus.ERROR: "⚠️",
    }
    return f"{icons[status]} {status.value}"


def _build_results_df(results: list[FileCheckResult]) -> pd.DataFrame:
    """Construye un DataFrame con los resultados para mostrar en tabla."""
    rows = []
    for r in results:
        dxf_issues_text = ""
        if r.dxf_issues:
            dxf_issues_text = "; ".join(
                f"{issue.issue_type.value}" for issue in r.dxf_issues
            )

        rows.append({
            "Código": r.codigo,
            "IPT": r.ipt_path.name,
            "Fecha IPT": r.ipt_modified.strftime("%d/%m/%Y %H:%M"),
            "DXF Estado": _status_emoji(r.dxf_status),
            "Fecha DXF": (
                r.dxf_modified.strftime("%d/%m/%Y %H:%M")
                if r.dxf_modified else "-"
            ),
            "Plano Estado": _status_emoji(r.idw_status),
            "Fecha Plano": (
                r.idw_modified.strftime("%d/%m/%Y %H:%M")
                if r.idw_modified else "-"
            ),
            "DXF Validación": dxf_issues_text if dxf_issues_text else "✅ OK",
            "Copia IPT": "⚠️ Necesaria" if r.ipt_copy_needed else "✅ OK",
        })

    return pd.DataFrame(rows)


def render():
    """Renderiza la página de verificación."""
    st.header("Verificación de Piezas")

    config = load_config()

    # Validar configuración
    if not config.ipt_folder or not config.dxf_folder:
        st.warning(
            "Configura las carpetas de trabajo en la página de **Configuración** antes de verificar."
        )
        return

    # Controles
    col_btn, col_filter = st.columns([1, 2])
    with col_btn:
        run_scan = st.button("🔍 Verificar piezas", type="primary", use_container_width=True)
    with col_filter:
        filter_option = st.selectbox(
            "Filtrar resultados",
            [
                "Todos",
                "Solo problemas",
                "DXF desactualizados",
                "Planos desactualizados",
                "DXF con errores geométricos",
                "Copia IPT pendiente",
            ],
        )

    validate_dxf = st.checkbox("Validar geometría DXF (más lento)", value=True)

    if run_scan:
        mapper = CodeMapper(config.mappings_file)
        scanner = FileScanner(config, mapper)
        checker = SyncChecker(config.timestamp_tolerance_seconds)
        validator = DxfValidator()

        with st.spinner("Escaneando carpetas..."):
            results = scanner.scan_all()

        with st.spinner("Verificando sincronización..."):
            results = checker.check_all(results)

        if validate_dxf:
            progress_bar = st.progress(0, text="Validando DXF...")
            for i, result in enumerate(results):
                if result.dxf_path:
                    result.dxf_issues = validator.validate(result.dxf_path)
                progress_bar.progress(
                    (i + 1) / len(results),
                    text=f"Validando DXF {i + 1}/{len(results)}...",
                )
            progress_bar.empty()

        # Guardar en session_state
        st.session_state["scan_results"] = results
        st.session_state["report"] = ProjectReport(
            project_name=config.ipt_folder,
            results=results,
        )

    # Mostrar resultados
    if "scan_results" in st.session_state:
        results = st.session_state["scan_results"]
        report = st.session_state["report"]

        # Métricas resumen
        st.subheader("Resumen")
        cols = st.columns(6)
        with cols[0]:
            metric_card("Total piezas", report.total_files, "#333")
        with cols[1]:
            metric_card("Todo OK", report.ok_count, "#28a745")
        with cols[2]:
            metric_card("DXF desactualizado", report.outdated_dxf_count, "#dc3545")
        with cols[3]:
            metric_card("Plano desactualizado", report.outdated_idw_count, "#dc3545")
        with cols[4]:
            metric_card("DXF con errores", report.dxf_with_issues_count, "#fd7e14")
        with cols[5]:
            metric_card("Copias pendientes", report.copies_needed_count, "#ffc107")

        # Filtrar resultados
        filtered = results
        if filter_option == "Solo problemas":
            filtered = [r for r in results if not r.overall_ok]
        elif filter_option == "DXF desactualizados":
            filtered = [r for r in results if r.dxf_status == SyncStatus.OUTDATED]
        elif filter_option == "Planos desactualizados":
            filtered = [r for r in results if r.idw_status == SyncStatus.OUTDATED]
        elif filter_option == "DXF con errores geométricos":
            filtered = [r for r in results if r.has_dxf_problems]
        elif filter_option == "Copia IPT pendiente":
            filtered = [r for r in results if r.ipt_copy_needed]

        # Tabla de resultados
        st.subheader(f"Resultados ({len(filtered)} piezas)")
        if filtered:
            df = _build_results_df(filtered)
            st.dataframe(df, use_container_width=True, hide_index=True)
        else:
            st.info("No hay piezas que coincidan con el filtro seleccionado.")

        # Detalle de pieza seleccionada
        if filtered:
            st.subheader("Detalle de pieza")
            codes = [r.codigo for r in filtered]
            selected_code = st.selectbox("Seleccionar pieza", codes)

            if selected_code:
                selected = next(r for r in filtered if r.codigo == selected_code)

                col_info, col_issues = st.columns(2)

                with col_info:
                    st.markdown("**Archivos:**")
                    st.markdown(f"- **IPT**: `{selected.ipt_path}`")
                    st.markdown(
                        f"  Modificado: {selected.ipt_modified.strftime('%d/%m/%Y %H:%M:%S')}"
                    )
                    if selected.dxf_path:
                        st.markdown(f"- **DXF**: `{selected.dxf_path}`")
                        st.markdown(
                            f"  Modificado: {selected.dxf_modified.strftime('%d/%m/%Y %H:%M:%S')}"
                        )
                    else:
                        st.markdown("- **DXF**: _No encontrado_")
                    if selected.idw_path:
                        st.markdown(f"- **IDW**: `{selected.idw_path}`")
                        st.markdown(
                            f"  Modificado: {selected.idw_modified.strftime('%d/%m/%Y %H:%M:%S')}"
                        )
                    else:
                        st.markdown("- **IDW**: _No encontrado_")
                    if selected.dwf_path:
                        st.markdown(f"- **DWF**: `{selected.dwf_path}`")

                with col_issues:
                    st.markdown("**Problemas DXF:**")
                    if selected.dxf_issues:
                        for issue in selected.dxf_issues:
                            st.markdown(
                                f"- 🔴 **{issue.issue_type.value}**: {issue.description}"
                            )
                            if issue.layer:
                                st.markdown(f"  Capa: `{issue.layer}`")
                    else:
                        st.markdown("✅ Sin problemas detectados")

        # Acciones
        st.subheader("Acciones")

        pending_copies = [r for r in results if r.ipt_copy_needed]
        if pending_copies:
            st.markdown(
                f"Hay **{len(pending_copies)}** archivos IPT que necesitan copiarse "
                f"a la carpeta de plegado."
            )
            if st.button(
                f"Copiar {len(pending_copies)} IPT a carpeta de plegado",
                type="secondary",
            ):
                file_mgr = FileManager()
                operations = file_mgr.copy_all_pending(results)
                ok_count = sum(1 for op in operations if op.success)
                err_count = sum(1 for op in operations if not op.success)

                if ok_count > 0:
                    st.success(f"{ok_count} archivos copiados correctamente.")
                if err_count > 0:
                    st.error(f"{err_count} errores al copiar:")
                    for op in operations:
                        if not op.success:
                            st.markdown(f"- `{op.source.name}`: {op.error}")

                # Mostrar historial
                if operations:
                    st.markdown("**Historial de copias:**")
                    hist_df = pd.DataFrame(file_mgr.get_history_as_dicts())
                    st.dataframe(hist_df, use_container_width=True, hide_index=True)
        else:
            st.info("No hay copias de IPT pendientes.")
