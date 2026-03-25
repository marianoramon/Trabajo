"""Página de configuración de rutas y parámetros."""

import json
from pathlib import Path

import streamlit as st
from core.models import AppConfig


CONFIG_FILE = "config.json"


def load_config() -> AppConfig:
    """Carga la configuración desde el archivo JSON."""
    config_path = Path(CONFIG_FILE)
    if config_path.exists():
        with open(config_path, "r", encoding="utf-8") as f:
            data = json.load(f)
            return AppConfig(**data)
    return AppConfig()


def save_config(config: AppConfig):
    """Guarda la configuración en el archivo JSON."""
    with open(CONFIG_FILE, "w", encoding="utf-8") as f:
        json.dump({
            "ipt_folder": config.ipt_folder,
            "dxf_folder": config.dxf_folder,
            "dwf_folder": config.dwf_folder,
            "ipt_bending_copy_folder": config.ipt_bending_copy_folder,
            "timestamp_tolerance_seconds": config.timestamp_tolerance_seconds,
            "mappings_file": config.mappings_file,
        }, f, indent=2, ensure_ascii=False)


def render():
    """Renderiza la página de configuración."""
    st.header("Configuración")
    st.markdown("Configura las rutas de las carpetas de trabajo.")

    config = load_config()

    with st.form("config_form"):
        st.subheader("Carpetas de trabajo")

        ipt_folder = st.text_input(
            "Carpeta de piezas IPT",
            value=config.ipt_folder,
            help="Ruta a la carpeta donde están los archivos .ipt (ej: Q:\\DISEÑOS\\60- PULVER AGRO)",
        )

        dxf_folder = st.text_input(
            "Carpeta de DXF (corte)",
            value=config.dxf_folder,
            help="Ruta a la carpeta de archivos DXF para corte (ej: Q:\\CORTE LASER\\01-PRODUCCIÓN VIGENTE)",
        )

        dwf_folder = st.text_input(
            "Carpeta de planos DWF/IDW",
            value=config.dwf_folder,
            help="Ruta a la carpeta de planos de plegado (ej: Q:\\DISEÑOS\\PLANOS PRODUCCIÓN)",
        )

        ipt_bending = st.text_input(
            "Carpeta destino copia IPT (plegado)",
            value=config.ipt_bending_copy_folder,
            help="Carpeta donde se copian los .ipt para que los planos de plegado se actualicen",
        )

        st.subheader("Parámetros")

        tolerance = st.number_input(
            "Tolerancia de tiempo (segundos)",
            min_value=0,
            max_value=3600,
            value=config.timestamp_tolerance_seconds,
            help="Margen de tolerancia al comparar fechas. Si la diferencia es menor, se considera actualizado.",
        )

        mappings_file = st.text_input(
            "Archivo de mapeos de códigos",
            value=config.mappings_file,
            help="Ruta al archivo CSV con la tabla de correspondencias corte/plegado",
        )

        submitted = st.form_submit_button("Guardar configuración", type="primary")

        if submitted:
            new_config = AppConfig(
                ipt_folder=ipt_folder,
                dxf_folder=dxf_folder,
                dwf_folder=dwf_folder,
                ipt_bending_copy_folder=ipt_bending,
                timestamp_tolerance_seconds=tolerance,
                mappings_file=mappings_file,
            )
            save_config(new_config)
            st.success("Configuración guardada correctamente.")

    # Mostrar estado de las carpetas
    st.subheader("Estado de las carpetas")
    config = load_config()
    folders = {
        "Piezas IPT": config.ipt_folder,
        "DXF Corte": config.dxf_folder,
        "Planos DWF/IDW": config.dwf_folder,
        "Copia IPT Plegado": config.ipt_bending_copy_folder,
    }

    for name, path_str in folders.items():
        if path_str:
            p = Path(path_str)
            if p.exists():
                file_count = sum(1 for _ in p.rglob("*") if _.is_file())
                st.markdown(f"**{name}**: `{path_str}` - ✅ Existe ({file_count} archivos)")
            else:
                st.markdown(f"**{name}**: `{path_str}` - ❌ No encontrada")
        else:
            st.markdown(f"**{name}**: _No configurada_")
