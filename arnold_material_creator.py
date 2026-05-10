"""
Arnold Material Creator para Cinema 4D
======================================

Script para Cinema 4D (con C4DtoA / Arnold Render instalado).

Funcionamiento:
    1. Pregunta una carpeta con mapas de texturas.
    2. Agrupa los archivos por nombre de material detectando los sufijos
       (BaseColor, Roughness, Normal, Displacement, AO, Specular, Metalness,
       Opacity, Emission, etc).
    3. Por cada grupo crea un material Arnold (standard_surface) y conecta
       cada mapa pasando previamente por un nodo `triplanar`, de modo que
       todas las texturas se proyectan triplanar.

Cómo usarlo:
    - Cinema 4D > Script Manager > New Script > pega este archivo > Execute.
    - O guárdalo en la carpeta de scripts de C4D y ejecútalo desde el menú
      Extensions > User Scripts.

Probado con C4DtoA 4.x sobre Cinema 4D R23 - 2024 (API GraphView clásica).
"""

import os
import re
import c4d
from c4d import gui


# ---------------------------------------------------------------------------
# IDs de Arnold (C4DtoA)
# ---------------------------------------------------------------------------
ARNOLD_SHADER_NETWORK = 1033991      # Material Arnold (Shader Network)
ARNOLD_SHADER         = 1033990      # Nodo genérico Arnold (su tipo se setea por nombre)
ARNOLD_NODE_PARAM_SHADER_NAME = c4d.C4DAI_SHADER_NAME if hasattr(c4d, "C4DAI_SHADER_NAME") else 1000


# Extensiones de imagen aceptadas
VALID_EXTS = (".jpg", ".jpeg", ".png", ".tif", ".tiff",
              ".exr", ".hdr", ".tx", ".bmp", ".tga", ".psd")


# Diccionario de palabras clave para detectar el tipo de mapa.
# El primer match gana. Se compara sobre el nombre de archivo en minúsculas.
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


# ---------------------------------------------------------------------------
# Detección de mapas
# ---------------------------------------------------------------------------
def detect_map_type(filename):
    """Devuelve el tipo de mapa o None si no se reconoce."""
    base = os.path.splitext(filename)[0].lower()
    for map_type, keywords in TEXTURE_KEYWORDS.items():
        for kw in keywords:
            pattern = r"(?:^|[_\-\.\s])" + re.escape(kw) + r"(?:[_\-\.\s\d]|$)"
            if re.search(pattern, base):
                return map_type
    return None


def derive_material_name(filename):
    """Quita el sufijo del mapa para obtener el nombre del material."""
    base = os.path.splitext(filename)[0]
    for keywords in TEXTURE_KEYWORDS.values():
        for kw in keywords:
            base = re.sub(r"[_\-\.\s]" + re.escape(kw) + r".*$", "", base, flags=re.IGNORECASE)
    # Quita resoluciones tipo "_4K", "_2K"
    base = re.sub(r"[_\-\.\s]?\d{1,2}[kK]\b.*$", "", base)
    return base.strip(" _-.") or "Material"


def group_textures(folder, recursive=True):
    """Recorre la carpeta y devuelve {nombre_material: {tipo: ruta}}."""
    materials = {}
    walker = os.walk(folder) if recursive else [(folder, [], os.listdir(folder))]
    for root, _, files in walker:
        for fname in files:
            if not fname.lower().endswith(VALID_EXTS):
                continue
            mtype = detect_map_type(fname)
            if not mtype:
                continue
            mname = derive_material_name(fname)
            materials.setdefault(mname, {})
            # Si ya hay un mapa de ese tipo no lo sobreescribimos
            materials[mname].setdefault(mtype, os.path.join(root, fname))
    return materials


# ---------------------------------------------------------------------------
# Helpers de la red de nodos Arnold
# ---------------------------------------------------------------------------
def _set_shader_type(node, shader_name):
    """Asigna el tipo de shader Arnold por nombre (e.g. 'image', 'triplanar')."""
    try:
        node[ARNOLD_NODE_PARAM_SHADER_NAME] = shader_name
    except Exception:
        # En algunas builds el ID literal es 1000
        node[1000] = shader_name


def create_node(master, shader_name, x=0, y=0):
    """Crea un nodo Arnold dentro del master del material."""
    parent = master.GetRoot()
    node = master.CreateNode(parent, ARNOLD_SHADER, None, x, y)
    if node is None:
        return None
    _set_shader_type(node, shader_name)
    return node


def add_out_port(node, port_name):
    port = node.AddPort(c4d.GV_PORT_OUTPUT, c4d.DescID(c4d.DescLevel(0, 0, 0)), message=False)
    # Mejor: usar AddPortIsVisible por nombre cuando es posible
    return port


def connect_by_name(master, src_node, src_port, dst_node, dst_port):
    """Conecta puertos por nombre (Arnold expone puertos virtuales)."""
    out_port = None
    in_port = None

    # Reusa puertos existentes
    for p in src_node.GetOutPorts():
        if p.GetName(src_node) == src_port or p.GetMainID() == src_port:
            out_port = p
            break
    if out_port is None:
        try:
            out_port = src_node.AddPort(c4d.GV_PORT_OUTPUT, src_port, message=True)
        except Exception:
            out_port = src_node.AddPort(c4d.GV_PORT_OUTPUT, src_port)

    for p in dst_node.GetInPorts():
        if p.GetName(dst_node) == dst_port or p.GetMainID() == dst_port:
            in_port = p
            break
    if in_port is None:
        try:
            in_port = dst_node.AddPort(c4d.GV_PORT_INPUT, dst_port, message=True)
        except Exception:
            in_port = dst_node.AddPort(c4d.GV_PORT_INPUT, dst_port)

    if out_port and in_port:
        return out_port.Connect(in_port)
    return False


def create_image_with_triplanar(master, file_path, raw=False, x=0, y=0):
    """Crea image -> triplanar y devuelve (image_node, triplanar_node)."""
    img = create_node(master, "image", x, y)
    if img is None:
        return None, None

    # Ruta del archivo
    try:
        img[c4d.C4DAIP_IMAGE_FILENAME] = file_path
    except Exception:
        # Fallback genérico
        for pid in (c4d.C4DAIP_IMAGE_FILENAME if hasattr(c4d, "C4DAIP_IMAGE_FILENAME") else 1, 1):
            try:
                img[pid] = file_path
                break
            except Exception:
                continue

    # Color space: raw para mapas no-color (roughness, normal, metal, disp, etc.)
    if raw:
        try:
            img[c4d.C4DAIP_IMAGE_COLOR_SPACE] = 1   # 'Raw' / Non-Color
        except Exception:
            pass

    tri = create_node(master, "triplanar", x + 280, y)
    if tri is not None:
        connect_by_name(master, img, "output", tri, "input")
    return img, tri


# ---------------------------------------------------------------------------
# Construcción del material
# ---------------------------------------------------------------------------
def build_material(name, maps):
    """Crea un material Arnold con los mapas indicados."""
    mat = c4d.BaseMaterial(ARNOLD_SHADER_NETWORK)
    if mat is None:
        return None
    mat.SetName(name)

    master = mat.GetNodeMaster()
    if master is None:
        return None

    # Standard Surface
    std = create_node(master, "standard_surface", 900, 0)
    if std is None:
        return mat

    y = -600
    step = 220

    # ---- Base Color
    if "base_color" in maps:
        _, tri = create_image_with_triplanar(master, maps["base_color"], raw=False, x=0, y=y)
        if tri:
            # Si hay AO, lo multiplicamos por encima del base color
            if "ao" in maps:
                _, ao_tri = create_image_with_triplanar(master, maps["ao"], raw=True,
                                                        x=0, y=y + step)
                mult = create_node(master, "multiply", 600, y)
                if mult and ao_tri:
                    connect_by_name(master, tri, "output", mult, "input1")
                    connect_by_name(master, ao_tri, "output", mult, "input2")
                    connect_by_name(master, mult, "output", std, "base_color")
                else:
                    connect_by_name(master, tri, "output", std, "base_color")
            else:
                connect_by_name(master, tri, "output", std, "base_color")
        y += step

    # ---- Specular weight
    if "specular" in maps:
        _, tri = create_image_with_triplanar(master, maps["specular"], raw=True, x=0, y=y)
        if tri:
            connect_by_name(master, tri, "r", std, "specular")
        y += step

    # ---- Roughness
    if "roughness" in maps:
        _, tri = create_image_with_triplanar(master, maps["roughness"], raw=True, x=0, y=y)
        if tri:
            connect_by_name(master, tri, "r", std, "specular_roughness")
        y += step

    # ---- Metalness
    if "metalness" in maps:
        _, tri = create_image_with_triplanar(master, maps["metalness"], raw=True, x=0, y=y)
        if tri:
            connect_by_name(master, tri, "r", std, "metalness")
        y += step

    # ---- Normal -> normal_map -> standard_surface.normal
    if "normal" in maps:
        _, tri = create_image_with_triplanar(master, maps["normal"], raw=True, x=0, y=y)
        if tri:
            nmap = create_node(master, "normal_map", 600, y)
            if nmap is not None:
                try:
                    nmap[c4d.C4DAIP_NORMAL_MAP_COLOR_TO_SIGNED] = True
                except Exception:
                    pass
                connect_by_name(master, tri, "output", nmap, "input")
                connect_by_name(master, nmap, "output", std, "normal")
        y += step

    # ---- Bump (si existe pero no hay normal lo conectamos como bump2d)
    if "bump" in maps and "normal" not in maps:
        _, tri = create_image_with_triplanar(master, maps["bump"], raw=True, x=0, y=y)
        if tri:
            bmp = create_node(master, "bump2d", 600, y)
            if bmp is not None:
                connect_by_name(master, tri, "r", bmp, "bump_map")
                connect_by_name(master, bmp, "output", std, "normal")
        y += step

    # ---- Emission
    if "emission" in maps:
        _, tri = create_image_with_triplanar(master, maps["emission"], raw=False, x=0, y=y)
        if tri:
            try:
                std[c4d.C4DAIP_STANDARD_SURFACE_EMISSION] = 1.0
            except Exception:
                pass
            connect_by_name(master, tri, "output", std, "emission_color")
        y += step

    # ---- Opacity
    if "opacity" in maps:
        _, tri = create_image_with_triplanar(master, maps["opacity"], raw=True, x=0, y=y)
        if tri:
            connect_by_name(master, tri, "output", std, "opacity")
        y += step

    # ---- Displacement: se conecta al puerto displacement del propio material output.
    # En el grafo Arnold el "Material" raíz tiene los puertos shader/displacement.
    if "displacement" in maps:
        _, tri = create_image_with_triplanar(master, maps["displacement"], raw=True, x=0, y=y)
        if tri:
            # Centrado en torno a 0 (-0.5..0.5)
            rng = create_node(master, "range", 600, y)
            if rng is not None:
                try:
                    rng[c4d.C4DAIP_RANGE_OUTPUT_MIN] = c4d.Vector(-0.5)
                    rng[c4d.C4DAIP_RANGE_OUTPUT_MAX] = c4d.Vector(0.5)
                except Exception:
                    pass
                connect_by_name(master, tri, "output", rng, "input")
                # Conecta al output del material (puerto displacement)
                root = master.GetRoot()
                # El nodo raíz del grafo expone "displacement"
                connect_by_name(master, rng, "output", root, "displacement")
            else:
                root = master.GetRoot()
                connect_by_name(master, tri, "output", root, "displacement")
        y += step

    # ---- Conexión final standard_surface -> root.shader
    root = master.GetRoot()
    connect_by_name(master, std, "output", root, "shader")

    mat.Update(True, True)
    return mat


# ---------------------------------------------------------------------------
# Punto de entrada
# ---------------------------------------------------------------------------
def main():
    doc = c4d.documents.GetActiveDocument()
    if doc is None:
        gui.MessageDialog("No hay documento activo.")
        return

    folder = c4d.storage.LoadDialog(
        title="Selecciona la carpeta con las texturas",
        flags=c4d.FILESELECT_DIRECTORY,
    )
    if not folder:
        return

    materials = group_textures(folder, recursive=True)
    if not materials:
        gui.MessageDialog("No se encontraron texturas reconocibles en la carpeta.")
        return

    doc.StartUndo()
    created = 0
    for name, maps in sorted(materials.items()):
        mat = build_material(name, maps)
        if mat is not None:
            doc.InsertMaterial(mat)
            doc.AddUndo(c4d.UNDOTYPE_NEW, mat)
            created += 1
    doc.EndUndo()

    c4d.EventAdd()
    gui.MessageDialog(
        "Se crearon {0} material(es) Arnold con proyeccion Triplanar.".format(created)
    )


if __name__ == "__main__":
    main()
