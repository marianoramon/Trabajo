"""Componentes reutilizables de la interfaz Streamlit."""

import streamlit as st
from core.models import SyncStatus


# Colores para los estados
STATUS_COLORS = {
    SyncStatus.OK: "#28a745",          # Verde
    SyncStatus.OUTDATED: "#dc3545",    # Rojo
    SyncStatus.MISSING: "#6c757d",     # Gris
    SyncStatus.ERROR: "#fd7e14",       # Naranja
}

STATUS_ICONS = {
    SyncStatus.OK: "✅",
    SyncStatus.OUTDATED: "🔴",
    SyncStatus.MISSING: "⚪",
    SyncStatus.ERROR: "⚠️",
}


def status_badge(status: SyncStatus) -> str:
    """Genera HTML para un badge de estado coloreado."""
    color = STATUS_COLORS[status]
    icon = STATUS_ICONS[status]
    return f'{icon} <span style="color:{color};font-weight:bold">{status.value}</span>'


def metric_card(label: str, value: int, color: str = "#333"):
    """Muestra una métrica con estilo de card."""
    st.markdown(
        f"""
        <div style="
            padding: 1rem;
            border-radius: 0.5rem;
            border: 1px solid #ddd;
            text-align: center;
            background: white;
        ">
            <div style="font-size: 2rem; font-weight: bold; color: {color};">{value}</div>
            <div style="font-size: 0.85rem; color: #666;">{label}</div>
        </div>
        """,
        unsafe_allow_html=True,
    )
