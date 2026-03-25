"""
Herramienta de Verificación de Piezas CAD
=========================================
Aplicación Streamlit para verificar la sincronización entre archivos
.ipt (Autodesk Inventor), .dxf (corte) y .idw/.dwf (planos de plegado).

Ejecutar con: streamlit run app.py
"""

import streamlit as st

st.set_page_config(
    page_title="Verificador de Piezas CAD",
    page_icon="🔧",
    layout="wide",
    initial_sidebar_state="expanded",
)

# Navegación lateral
st.sidebar.title("Verificador CAD")
st.sidebar.markdown("---")

page = st.sidebar.radio(
    "Navegación",
    ["Verificación", "Configuración", "Mapeo de Códigos"],
    index=0,
)

st.sidebar.markdown("---")
st.sidebar.markdown(
    """
    **Flujo de trabajo:**
    1. Configura las rutas
    2. Añade mapeos de códigos (sistema antiguo)
    3. Verifica las piezas
    """
)

# Renderizar página seleccionada
if page == "Verificación":
    from ui.pages.verificacion import render
    render()
elif page == "Configuración":
    from ui.pages.configuracion import render
    render()
elif page == "Mapeo de Códigos":
    from ui.pages.mapeo_codigos import render
    render()
