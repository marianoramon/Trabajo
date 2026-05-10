"""
Arnold Material Creator para Cinema 4D R26 / C4DtoA 4.x
=========================================================

Compatible: Cinema 4D R26 (26.x) con C4DtoA 4.2+

Instrucciones:
    1. Cinema 4D > Extensions > Script Manager > Nuevo script
    2. Pega o carga este archivo y pulsa "Ejecutar"
    3. Selecciona la carpeta con los mapas de textura
    4. El script agrupa los archivos por nombre de material, detecta
       el tipo de mapa por sufijo y crea materiales Arnold standard_surface
       donde cada imagen pasa por un nodo Triplanar.

Mapas reconocidos (por sufijo en el nombre de archivo):
    Base Color   : basecolor, base_color, albedo, diffuse, diff, color, _col, _bc
    Roughness    : roughness, rough, _rgh
    Metalness    : metalness, metallic, metal, _mtl
    Specular     : specular, spec
    Normal       : normal, normaldx, normalgl, _nrm, _norm
    Displacement : displacement, disp, height, _hgt
    AO           : ambientocclusion, ambient_occlusion, _ao, occlusion
    Opacity      : opacity, alpha, transparency
    Emission     : emission, emissive, emit
    Bump         : bump
"""

import os
import re
import c4d
from c4d import gui

# ──────────────────────────────────────────────────────────────────────────────
# IDs del plugin Arnold (C4DtoA) – no cambian entre versiones menores
# ──────────────────────────────────────────────────────────────────────────────
ARNOLD_SHADER_NETWORK = 1033991   # Tipo de material Arnold (Shader Network)
ARNOLD_SHADER         = 1033990   # Nodo Arnold genérico dentro del grafo

# ──────────────────────────────────────────────────────────────────────────────
# Extensiones de imagen válidas
# ──────────────────────────────────────────────────────────────────────────────
VALID_EXTS = (".jpg", ".jpeg", ".png", ".tif", ".tiff",
              ".exr", ".hdr", ".tx", ".bmp", ".tga", ".psd")

# ──────────────────────────────────────────────────────────────────────────────
# Palabras clave para detectar tipos de mapa
# ──────────────────────────────────────────────────────────────────────────────
TEXTURE_KEYWORDS = {
    "base_color":   ["basecolor", "base_color", "albedo", "diffuse", "diff", "color", "_col", "_bc"],
    "roughness":    ["roughness", "rough", "_rgh"],
    "metalness":    ["metalness", "metallic", "metal", "_mtl"],
    "specular":     ["specular", "spec"],
    "normal":       ["normaldx", "normalgl", "normal", "_nrm", "_norm"],
    "displacement": ["displacement", "disp", "height", "_hgt"],
    "ao":           ["ambientocclusion", "ambient_occlusion", "_ao", "occlusion"],
    "opacity":      ["opacity", "alpha", "transparency"],
    "emission":     ["emission", "emissive", "emit"],
    "bump":         ["bump"],
}

# Orden de prioridad para mostrar en el grafo (de arriba a abajo)
MAP_ORDER = ["base_color", "ao", "specular", "roughness", "metalness",
             "normal", "bump", "emission", "opacity", "displacement"]


# ──────────────────────────────────────────────────────────────────────────────
# Utilidades: detección de archivos
# ──────────────────────────────────────────────────────────────────────────────

def detect_map_type(filename):
    """Devuelve el tipo de mapa según el nombre de archivo, o None."""
    base = os.path.splitext(filename)[0].lower()
    for map_type, keywords in TEXTURE_KEYWORDS.items():
        for kw in keywords:
            if re.search(r"(?:^|[_\-\.\s])" + re.escape(kw) + r"(?:[_\-\.\s\d]|$)", base):
                return map_type
    return None


def derive_material_name(filename):
    """Elimina el sufijo de tipo de mapa para obtener el nombre base del material."""
    base = os.path.splitext(filename)[0]
    for keywords in TEXTURE_KEYWORDS.values():
        for kw in keywords:
            base = re.sub(r"[_\-\.\s]" + re.escape(kw) + r".*$", "", base, flags=re.IGNORECASE)
    # Elimina resoluciones tipo _4K, _2K, etc.
    base = re.sub(r"[_\-\.\s]?\d{1,2}[kK]\b.*$", "", base)
    return base.strip(" _-.") or "Material"


def group_textures(folder):
    """Recorre la carpeta recursivamente y agrupa texturas por material."""
    materials = {}
    for root_dir, _, files in os.walk(folder):
        for fname in sorted(files):
            if not fname.lower().endswith(VALID_EXTS):
                continue
            mtype = detect_map_type(fname)
            if not mtype:
                continue
            mname = derive_material_name(fname)
            materials.setdefault(mname, {})
            materials[mname].setdefault(mtype, os.path.join(root_dir, fname))
    return materials


# ──────────────────────────────────────────────────────────────────────────────
# Utilidades: acceso a parámetros por descripción (robusto en R26)
# ──────────────────────────────────────────────────────────────────────────────

def _set_param_by_ident(node, ident_str, value):
    """
    Establece un parámetro buscándolo por su identificador de descripción.
    Este método funciona con cualquier versión de C4DtoA en R26 sin depender
    de constantes numéricas que pueden variar.
    """
    try:
        desc = node.GetDescription(c4d.DESCFLAGS_DESC_NONE)
        for pid, bc, _ in desc:
            if bc.GetString(c4d.DESC_IDENT) == ident_str:
                node.SetParameter(pid, value, c4d.DESCFLAGS_SET_FORCESET)
                return True
    except Exception:
        pass
    return False


def _set_param_safe(node, c4d_const_name, fallback_ident, value):
    """
    Intenta: 1) constante C4D nombrada, 2) búsqueda por ident, 3) silencio.
    """
    const = getattr(c4d, c4d_const_name, None)
    if const is not None:
        try:
            node[const] = value
            return True
        except Exception:
            pass
    return _set_param_by_ident(node, fallback_ident, value)


# ──────────────────────────────────────────────────────────────────────────────
# Utilidades: creación de nodos y conexión de puertos
# ──────────────────────────────────────────────────────────────────────────────

def create_node(master, shader_type, x=0, y=0):
    """Crea un nodo Arnold GvNode del tipo dado dentro del master."""
    root = master.GetRoot()
    node = master.CreateNode(root, ARNOLD_SHADER, None, x, y)
    if node is None:
        return None
    # Asignar tipo de shader Arnold
    if not _set_param_safe(node, "C4DAI_SHADER_NAME", "shader_name", shader_type):
        # Último recurso: ID numérico conocido del parámetro de nombre en C4DtoA 4.x
        try:
            node[1000] = shader_type
        except Exception:
            pass
    return node


def _find_port(node, direction, name):
    """Busca un puerto por nombre; lo crea si no existe."""
    ports = node.GetOutPorts() if direction == c4d.GV_PORT_OUTPUT else node.GetInPorts()
    for p in ports:
        try:
            if p.GetName(node) == name:
                return p
        except Exception:
            continue
    # Añadir el puerto si no se encontró
    try:
        return node.AddPort(direction, name, message=False)
    except Exception:
        try:
            return node.AddPort(direction, name)
        except Exception:
            return None


def connect(src_node, src_port, dst_node, dst_port):
    """Conecta dos nodos Arnold por nombre de puerto."""
    out = _find_port(src_node, c4d.GV_PORT_OUTPUT, src_port)
    inp = _find_port(dst_node, c4d.GV_PORT_INPUT,  dst_port)
    if out and inp:
        try:
            return out.Connect(inp)
        except Exception:
            pass
    return False


# ──────────────────────────────────────────────────────────────────────────────
# Bloque imagen + triplanar
# ──────────────────────────────────────────────────────────────────────────────

def create_image_triplanar(master, filepath, raw, x, y):
    """
    Crea:  [image] --> [triplanar]
    Devuelve (image_node, triplanar_node).
    raw=True  → color space Raw / Non-Color (roughness, normal, metalness, etc.)
    raw=False → color space Auto/sRGB       (base color, emission)
    """
    img = create_node(master, "image", x, y)
    if img is None:
        return None, None

    # Ruta del archivo — probamos varias constantes / idents
    _set_param_safe(img, "C4DAIP_IMAGE_FILENAME", "filename", filepath)

    # Color space: 0 = auto/sRGB, 1 = raw
    if raw:
        _set_param_safe(img, "C4DAIP_IMAGE_COLOR_SPACE", "color_space", 1)

    tri = create_node(master, "triplanar", x + 300, y)
    if tri is None:
        return img, None

    connect(img, "output", tri, "input")
    return img, tri


# ──────────────────────────────────────────────────────────────────────────────
# Construcción del material completo
# ──────────────────────────────────────────────────────────────────────────────

def build_material(name, maps):
    """Crea y devuelve un BaseMaterial Arnold con todos los mapas conectados."""
    mat = c4d.BaseMaterial(ARNOLD_SHADER_NETWORK)
    if mat is None:
        return None
    mat.SetName(name)

    master = mat.GetNodeMaster()
    if master is None:
        return None

    root   = master.GetRoot()
    STEP   = 250        # separación vertical entre mapas en el grafo
    x_img  = 0         # columna de nodos imagen
    x_tri  = 310       # columna de nodos triplanar
    x_aux  = 660       # columna de nodos auxiliares (normal_map, range, multiply…)
    x_std  = 1000      # columna standard_surface
    y      = 0

    # ── Standard Surface ─────────────────────────────────────────────────────
    std = create_node(master, "standard_surface", x_std, 0)
    if std is None:
        return mat

    # ── Base Color ───────────────────────────────────────────────────────────
    if "base_color" in maps:
        _, tri = create_image_triplanar(master, maps["base_color"], raw=False,
                                        x=x_img, y=y)
        if tri:
            if "ao" in maps:
                # AO × BaseColor con nodo multiply
                _, ao_tri = create_image_triplanar(master, maps["ao"], raw=True,
                                                   x=x_img, y=y + STEP)
                mult = create_node(master, "multiply", x_aux, y + STEP // 2)
                if mult and ao_tri:
                    connect(tri,    "output", mult, "input1")
                    connect(ao_tri, "output", mult, "input2")
                    connect(mult,   "output", std,  "base_color")
                    y += STEP
                else:
                    connect(tri, "output", std, "base_color")
            else:
                connect(tri, "output", std, "base_color")
        y += STEP

    # ── Specular ─────────────────────────────────────────────────────────────
    if "specular" in maps:
        _, tri = create_image_triplanar(master, maps["specular"], raw=True,
                                        x=x_img, y=y)
        if tri:
            connect(tri, "output", std, "specular")
        y += STEP

    # ── Roughness ────────────────────────────────────────────────────────────
    if "roughness" in maps:
        _, tri = create_image_triplanar(master, maps["roughness"], raw=True,
                                        x=x_img, y=y)
        if tri:
            connect(tri, "output", std, "specular_roughness")
        y += STEP

    # ── Metalness ────────────────────────────────────────────────────────────
    if "metalness" in maps:
        _, tri = create_image_triplanar(master, maps["metalness"], raw=True,
                                        x=x_img, y=y)
        if tri:
            connect(tri, "output", std, "metalness")
        y += STEP

    # ── Normal ───────────────────────────────────────────────────────────────
    if "normal" in maps:
        _, tri = create_image_triplanar(master, maps["normal"], raw=True,
                                        x=x_img, y=y)
        if tri:
            nmap = create_node(master, "normal_map", x_aux, y)
            if nmap:
                connect(tri,  "output", nmap, "input")
                connect(nmap, "output", std,  "normal")
            else:
                connect(tri, "output", std, "normal")
        y += STEP

    # ── Bump (solo si no hay mapa Normal) ────────────────────────────────────
    elif "bump" in maps:
        _, tri = create_image_triplanar(master, maps["bump"], raw=True,
                                        x=x_img, y=y)
        if tri:
            bmp = create_node(master, "bump2d", x_aux, y)
            if bmp:
                connect(tri, "r",      bmp, "bump_map")
                connect(bmp, "output", std, "normal")
            else:
                connect(tri, "r", std, "normal")
        y += STEP

    # ── Emission ─────────────────────────────────────────────────────────────
    if "emission" in maps:
        _, tri = create_image_triplanar(master, maps["emission"], raw=False,
                                        x=x_img, y=y)
        if tri:
            # Activar peso de emission a 1
            _set_param_safe(std, "C4DAIP_STANDARD_SURFACE_EMISSION",
                            "emission", 1.0)
            connect(tri, "output", std, "emission_color")
        y += STEP

    # ── Opacity ──────────────────────────────────────────────────────────────
    if "opacity" in maps:
        _, tri = create_image_triplanar(master, maps["opacity"], raw=True,
                                        x=x_img, y=y)
        if tri:
            connect(tri, "output", std, "opacity")
        y += STEP

    # ── Displacement ─────────────────────────────────────────────────────────
    if "displacement" in maps:
        _, tri = create_image_triplanar(master, maps["displacement"], raw=True,
                                        x=x_img, y=y)
        if tri:
            rng = create_node(master, "range", x_aux, y)
            if rng:
                # Recentrar 0-1 a -0.5..0.5 para desplazamiento centrado
                _set_param_by_ident(rng, "output_min",
                                    c4d.Vector(-0.5, -0.5, -0.5))
                _set_param_by_ident(rng, "output_max",
                                    c4d.Vector(0.5,  0.5,  0.5))
                connect(tri, "output", rng,  "input")
                connect(rng, "output", root, "displacement")
            else:
                connect(tri, "output", root, "displacement")
        y += STEP

    # ── Salida final: standard_surface → shader output del material ───────────
    connect(std, "output", root, "shader")

    mat.Update(True, True)
    return mat


# ──────────────────────────────────────────────────────────────────────────────
# Diálogo de vista previa
# ──────────────────────────────────────────────────────────────────────────────

def build_preview_text(materials):
    """Devuelve texto con el resumen de materiales y mapas detectados."""
    lines = ["Se van a crear {0} material(es):\n".format(len(materials))]
    for mname in sorted(materials):
        maps = materials[mname]
        lines.append("  [{0}]".format(mname))
        for mtype in MAP_ORDER:
            if mtype in maps:
                lines.append("    + {0:<14} {1}".format(
                    mtype, os.path.basename(maps[mtype])))
        lines.append("")
    return "\n".join(lines)


# ──────────────────────────────────────────────────────────────────────────────
# Punto de entrada
# ──────────────────────────────────────────────────────────────────────────────

def main():
    doc = c4d.documents.GetActiveDocument()
    if doc is None:
        gui.MessageDialog("No hay documento activo en Cinema 4D.")
        return

    # Verificar que el plugin Arnold está cargado
    if c4d.plugins.FindPlugin(ARNOLD_SHADER_NETWORK) is None:
        gui.MessageDialog(
            "C4DtoA (Arnold) no encontrado.\n"
            "Asegúrate de que Arnold está instalado y activado."
        )
        return

    folder = c4d.storage.LoadDialog(
        title="Selecciona la carpeta con las texturas",
        flags=c4d.FILESELECT_DIRECTORY,
    )
    if not folder:
        return

    materials = group_textures(folder)
    if not materials:
        gui.MessageDialog(
            "No se encontraron texturas con sufijos reconocibles en:\n" + folder
        )
        return

    # Confirmación con vista previa
    if not gui.QuestionDialog(build_preview_text(materials) + "\n¿Crear materiales?"):
        return

    doc.StartUndo()
    created = 0
    errors  = []

    for name in sorted(materials):
        maps = materials[name]
        try:
            mat = build_material(name, maps)
            if mat is not None:
                doc.InsertMaterial(mat)
                doc.AddUndo(c4d.UNDOTYPE_NEW, mat)
                created += 1
            else:
                errors.append("{0}: No se pudo crear (GetNodeMaster devolvió None)".format(name))
        except Exception as exc:
            errors.append("{0}: {1}".format(name, exc))

    doc.EndUndo()
    c4d.EventAdd()

    msg = "Materiales Arnold creados con Triplanar: {0}".format(created)
    if errors:
        msg += "\n\nAvisos / Errores:\n" + "\n".join(errors)
    gui.MessageDialog(msg)


if __name__ == "__main__":
    main()
