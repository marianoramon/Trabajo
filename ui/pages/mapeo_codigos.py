"""Página de gestión de mapeos de códigos corte/plegado."""

import io
import pandas as pd
import streamlit as st

from core.code_mapper import CodeMapper
from ui.pages.configuracion import load_config


def render():
    """Renderiza la página de mapeo de códigos."""
    st.header("Mapeo de Códigos Corte / Plegado")
    st.markdown(
        "Gestiona la correspondencia entre códigos de pieza, corte y plegado. "
        "Para el **sistema nuevo** (A00280+) no necesitas mapeos, se usa el mismo código. "
        "Para el **sistema antiguo** (ej: 45017/45018) debes añadirlos aquí."
    )

    config = load_config()
    mapper = CodeMapper(config.mappings_file)

    # Tabla actual
    st.subheader("Tabla de correspondencias")

    mappings_data = mapper.get_all_mappings_as_dicts()
    if mappings_data:
        df = pd.DataFrame(mappings_data)
        st.dataframe(df, use_container_width=True, hide_index=True)
    else:
        st.info("No hay mapeos configurados. Añade uno o importa un CSV.")

    # Añadir nuevo mapeo
    st.subheader("Añadir mapeo")
    col1, col2, col3, col4 = st.columns([3, 3, 3, 2])

    with col1:
        new_pieza = st.text_input("Código Pieza", key="new_pieza")
    with col2:
        new_corte = st.text_input("Código Corte", key="new_corte")
    with col3:
        new_plegado = st.text_input("Código Plegado", key="new_plegado")
    with col4:
        st.markdown("<br>", unsafe_allow_html=True)
        if st.button("Añadir", type="primary"):
            if new_pieza:
                mapper.add_mapping(new_pieza, new_corte, new_plegado)
                mapper.save()
                st.success(f"Mapeo añadido: {new_pieza}")
                st.rerun()
            else:
                st.error("El código de pieza es obligatorio.")

    # Eliminar mapeo
    if mappings_data:
        st.subheader("Eliminar mapeo")
        codes = [m["Código Pieza"] for m in mappings_data]
        del_code = st.selectbox("Selecciona código a eliminar", codes)
        if st.button("Eliminar"):
            mapper.remove_mapping(del_code)
            mapper.save()
            st.success(f"Mapeo eliminado: {del_code}")
            st.rerun()

    # Importar/Exportar CSV
    st.subheader("Importar / Exportar")

    col_imp, col_exp = st.columns(2)

    with col_imp:
        st.markdown("**Importar CSV**")
        st.markdown("Formato: `codigo_pieza;codigo_corte;codigo_plegado`")
        uploaded = st.file_uploader("Seleccionar CSV", type=["csv"])
        if uploaded:
            try:
                content = uploaded.read().decode("utf-8-sig")
                reader = pd.read_csv(io.StringIO(content), sep=";")
                count = 0
                for _, row in reader.iterrows():
                    pieza = str(row.get("codigo_pieza", "")).strip()
                    corte = str(row.get("codigo_corte", "")).strip()
                    plegado = str(row.get("codigo_plegado", "")).strip()
                    if pieza and pieza != "nan":
                        mapper.add_mapping(pieza, corte, plegado)
                        count += 1
                mapper.save()
                st.success(f"Importados {count} mapeos.")
                st.rerun()
            except Exception as e:
                st.error(f"Error al importar: {e}")

    with col_exp:
        st.markdown("**Exportar CSV**")
        if mappings_data:
            df_export = pd.DataFrame(mappings_data)
            csv_data = df_export.to_csv(index=False, sep=";")
            st.download_button(
                "Descargar CSV",
                csv_data,
                file_name="mappings_export.csv",
                mime="text/csv",
            )
        else:
            st.info("No hay mapeos para exportar.")

    # Búsqueda
    st.subheader("Buscar código")
    search_code = st.text_input("Buscar por código", key="search_code")
    if search_code:
        dxf_code = mapper.get_dxf_code(search_code)
        drawing_code = mapper.get_drawing_code(search_code)
        is_new = CodeMapper.is_new_system(search_code)

        st.markdown(f"**Sistema**: {'Nuevo' if is_new else 'Antiguo'}")
        st.markdown(f"**Código DXF (corte)**: `{dxf_code}`")
        st.markdown(f"**Código Plano (plegado)**: `{drawing_code}`")
