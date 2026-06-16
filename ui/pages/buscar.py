"""Página de búsqueda de ficheros de Autodesk Inventor en la red."""

from datetime import datetime, time

import pandas as pd
import streamlit as st

from core.file_search import (
    INVENTOR_FILE_TYPES,
    FileSearchEngine,
    SearchCriteria,
)
from ui.pages.configuracion import load_config, save_config


def _results_to_dataframe(results) -> pd.DataFrame:
    """Convierte los resultados de búsqueda en un DataFrame para mostrar."""
    return pd.DataFrame(
        [
            {
                "Nombre": r.name,
                "Tipo": r.type_label,
                "Tamaño": r.size_human,
                "Modificado": r.modified.strftime("%Y-%m-%d %H:%M"),
                "Ruta": str(r.path),
            }
            for r in results
        ]
    )


def render():
    """Renderiza la página de búsqueda de ficheros."""
    st.header("🔎 Buscar ficheros en la red")
    st.markdown(
        "Busca ficheros de Autodesk Inventor (.ipt, .iam, .idw, .dwg…) "
        "en las carpetas de red configuradas."
    )

    config = load_config()

    # --- Carpetas de red donde buscar ---
    with st.expander("📁 Carpetas de red a buscar", expanded=not config.search_folders):
        st.caption(
            "Indica una ruta por línea. Acepta rutas UNC (\\\\servidor\\carpeta) "
            "o unidades mapeadas (Q:\\DISEÑOS)."
        )
        folders_text = st.text_area(
            "Carpetas",
            value="\n".join(config.search_folders),
            height=120,
            label_visibility="collapsed",
            placeholder="\\\\servidor\\DISEÑOS\nQ:\\CORTE LASER",
        )
        if st.button("💾 Guardar carpetas"):
            config.search_folders = [
                line.strip() for line in folders_text.splitlines() if line.strip()
            ]
            save_config(config)
            st.success("Carpetas guardadas.")
            st.rerun()

    roots = [line.strip() for line in folders_text.splitlines() if line.strip()]
    if not roots:
        st.info("Añade al menos una carpeta de red para poder buscar.")
        return

    # --- Filtros de búsqueda ---
    st.subheader("Filtros")
    col1, col2 = st.columns([2, 2])

    with col1:
        query = st.text_input(
            "Nombre contiene",
            placeholder="Ej: 60-1234, brida, soporte…",
            help="Busca ficheros cuyo nombre contenga este texto.",
        )

    with col2:
        type_labels = st.multiselect(
            "Tipos de fichero",
            options=list(INVENTOR_FILE_TYPES.keys()),
            format_func=lambda ext: INVENTOR_FILE_TYPES[ext],
            help="Vacío = todos los tipos de Inventor.",
        )

    with st.expander("Filtros avanzados"):
        adv1, adv2, adv3 = st.columns(3)
        with adv1:
            use_date = st.checkbox("Filtrar por fecha de modificación")
            date_after = st.date_input(
                "Modificado desde",
                value=None,
                disabled=not use_date,
            )
        with adv2:
            min_size_mb = st.number_input(
                "Tamaño mínimo (MB)",
                min_value=0.0,
                value=0.0,
                step=0.5,
            )
        with adv3:
            max_results = st.number_input(
                "Máximo de resultados",
                min_value=10,
                max_value=10000,
                value=1000,
                step=100,
            )

    # --- Ejecutar búsqueda ---
    if st.button("🔎 Buscar", type="primary"):
        criteria = SearchCriteria(
            query=query.strip(),
            extensions=list(type_labels),
            modified_after=(
                datetime.combine(date_after, time.min)
                if use_date and date_after
                else None
            ),
            min_size_bytes=int(min_size_mb * 1024 * 1024) if min_size_mb else None,
            max_results=int(max_results),
        )

        engine = FileSearchEngine(roots)
        with st.spinner("Buscando en la red…"):
            report = engine.search(criteria)

        st.session_state["search_report"] = report

    # --- Mostrar resultados ---
    report = st.session_state.get("search_report")
    if report is None:
        return

    st.markdown("---")
    m1, m2, m3 = st.columns(3)
    m1.metric("Encontrados", report.total_found)
    m2.metric("Analizados", report.scanned_files)
    m3.metric("Tiempo", f"{report.duration_seconds:.1f} s")

    if report.truncated:
        st.warning(
            f"Se alcanzó el límite de {report.total_found} resultados. "
            "Afina los filtros para ver el resto."
        )

    if report.errors:
        with st.expander(f"⚠️ Avisos ({len(report.errors)})"):
            for err in report.errors[:50]:
                st.text(err)

    if not report.results:
        st.info("No se encontraron ficheros con esos criterios.")
        return

    df = _results_to_dataframe(report.results)
    st.dataframe(df, use_container_width=True, hide_index=True)

    st.download_button(
        "⬇️ Descargar resultados (CSV)",
        data=df.to_csv(index=False).encode("utf-8-sig"),
        file_name="busqueda_ficheros.csv",
        mime="text/csv",
    )
