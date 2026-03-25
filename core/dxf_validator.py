"""Validador de archivos DXF - detecta problemas geométricos."""

import math
from pathlib import Path
from typing import Optional

from core.models import DxfIssue, DxfIssueType

try:
    import ezdxf
    from ezdxf import recover
    from ezdxf.entities import LWPolyline, Line, Arc, Circle, Polyline
    EZDXF_AVAILABLE = True
except ImportError:
    EZDXF_AVAILABLE = False


# Tolerancia para comparar coordenadas (en unidades del dibujo, normalmente mm)
COORD_TOLERANCE = 0.01
# Longitud mínima para considerar un segmento válido
MIN_SEGMENT_LENGTH = 0.05


class DxfValidator:
    """Valida la integridad geométrica de archivos DXF para corte láser."""

    def __init__(self, tolerance: float = COORD_TOLERANCE,
                 min_length: float = MIN_SEGMENT_LENGTH):
        self.tolerance = tolerance
        self.min_length = min_length

    def validate(self, dxf_path: Path) -> list[DxfIssue]:
        """Valida un archivo DXF y devuelve la lista de problemas encontrados."""
        if not EZDXF_AVAILABLE:
            return [DxfIssue(
                issue_type=DxfIssueType.STRUCTURAL,
                description="Librería ezdxf no instalada. Ejecute: pip install ezdxf",
            )]

        issues: list[DxfIssue] = []

        try:
            doc, auditor = recover.readfile(str(dxf_path))
        except IOError:
            issues.append(DxfIssue(
                issue_type=DxfIssueType.STRUCTURAL,
                description=f"No se puede leer el archivo: {dxf_path.name}",
            ))
            return issues
        except ezdxf.DXFStructureError:
            issues.append(DxfIssue(
                issue_type=DxfIssueType.STRUCTURAL,
                description=f"Archivo DXF corrupto o inválido: {dxf_path.name}",
            ))
            return issues

        # Errores del auditor
        if auditor.has_errors:
            for error in auditor.errors:
                issues.append(DxfIssue(
                    issue_type=DxfIssueType.STRUCTURAL,
                    description=f"Error auditor: {error.message}",
                ))

        msp = doc.modelspace()
        entities = list(msp)

        # Sin geometría
        if len(entities) == 0:
            issues.append(DxfIssue(
                issue_type=DxfIssueType.NO_GEOMETRY,
                description="El archivo DXF no contiene ninguna entidad geométrica",
            ))
            return issues

        # Verificar cada entidad
        issues.extend(self._check_lwpolylines(msp))
        issues.extend(self._check_polylines(msp))
        issues.extend(self._check_lines(msp))
        issues.extend(self._check_arcs(msp))
        issues.extend(self._check_duplicates(entities))

        return issues

    def _check_lwpolylines(self, msp) -> list[DxfIssue]:
        """Verifica polilíneas ligeras (LWPOLYLINE)."""
        issues = []
        for entity in msp.query("LWPOLYLINE"):
            layer = entity.dxf.layer

            # Contorno abierto
            if not entity.closed:
                points = list(entity.get_points(format="xy"))
                if len(points) >= 2:
                    first = points[0]
                    last = points[-1]
                    dist = math.sqrt(
                        (first[0] - last[0]) ** 2 + (first[1] - last[1]) ** 2
                    )
                    if dist > self.tolerance:
                        issues.append(DxfIssue(
                            issue_type=DxfIssueType.UNCLOSED_CONTOUR,
                            description=(
                                f"LWPOLYLINE abierta en capa '{layer}'. "
                                f"Distancia entre extremos: {dist:.3f}mm"
                            ),
                            entity_type="LWPOLYLINE",
                            layer=layer,
                        ))

            # Segmentos diminutos
            points = list(entity.get_points(format="xy"))
            for i in range(len(points) - 1):
                dx = points[i + 1][0] - points[i][0]
                dy = points[i + 1][1] - points[i][1]
                seg_len = math.sqrt(dx * dx + dy * dy)
                if seg_len < self.min_length and seg_len > 0:
                    issues.append(DxfIssue(
                        issue_type=DxfIssueType.TINY_SEGMENT,
                        description=(
                            f"Segmento diminuto ({seg_len:.4f}mm) "
                            f"en LWPOLYLINE, capa '{layer}'"
                        ),
                        entity_type="LWPOLYLINE",
                        layer=layer,
                    ))
                    break  # Solo reportar una vez por polilínea

        return issues

    def _check_polylines(self, msp) -> list[DxfIssue]:
        """Verifica polilíneas 2D/3D (POLYLINE)."""
        issues = []
        for entity in msp.query("POLYLINE"):
            layer = entity.dxf.layer

            if not entity.is_closed:
                vertices = list(entity.vertices)
                if len(vertices) >= 2:
                    first = vertices[0].dxf.location
                    last = vertices[-1].dxf.location
                    dist = math.sqrt(
                        (first.x - last.x) ** 2 + (first.y - last.y) ** 2
                    )
                    if dist > self.tolerance:
                        issues.append(DxfIssue(
                            issue_type=DxfIssueType.UNCLOSED_CONTOUR,
                            description=(
                                f"POLYLINE abierta en capa '{layer}'. "
                                f"Distancia entre extremos: {dist:.3f}mm"
                            ),
                            entity_type="POLYLINE",
                            layer=layer,
                        ))

        return issues

    def _check_lines(self, msp) -> list[DxfIssue]:
        """Verifica líneas (LINE) de longitud cero."""
        issues = []
        for entity in msp.query("LINE"):
            start = entity.dxf.start
            end = entity.dxf.end
            length = math.sqrt(
                (end.x - start.x) ** 2
                + (end.y - start.y) ** 2
                + (end.z - start.z) ** 2
            )
            if length < self.tolerance:
                issues.append(DxfIssue(
                    issue_type=DxfIssueType.ZERO_LENGTH,
                    description=(
                        f"LINE de longitud cero en capa '{entity.dxf.layer}'"
                    ),
                    entity_type="LINE",
                    layer=entity.dxf.layer,
                ))

        return issues

    def _check_arcs(self, msp) -> list[DxfIssue]:
        """Verifica arcos (ARC) con radio cero."""
        issues = []
        for entity in msp.query("ARC"):
            if entity.dxf.radius < self.tolerance:
                issues.append(DxfIssue(
                    issue_type=DxfIssueType.ZERO_LENGTH,
                    description=(
                        f"ARC con radio cero en capa '{entity.dxf.layer}'"
                    ),
                    entity_type="ARC",
                    layer=entity.dxf.layer,
                ))

        return issues

    def _check_duplicates(self, entities) -> list[DxfIssue]:
        """Detecta entidades duplicadas (mismas coordenadas)."""
        issues = []
        seen_lines: list[tuple] = []

        for entity in entities:
            if entity.dxftype() == "LINE":
                start = entity.dxf.start
                end = entity.dxf.end
                key = (
                    round(start.x, 2), round(start.y, 2),
                    round(end.x, 2), round(end.y, 2),
                    entity.dxf.layer,
                )
                # También verificar invertida
                key_rev = (
                    round(end.x, 2), round(end.y, 2),
                    round(start.x, 2), round(start.y, 2),
                    entity.dxf.layer,
                )
                if key in seen_lines or key_rev in seen_lines:
                    issues.append(DxfIssue(
                        issue_type=DxfIssueType.DUPLICATE,
                        description=(
                            f"LINE duplicada en capa '{entity.dxf.layer}'"
                        ),
                        entity_type="LINE",
                        layer=entity.dxf.layer,
                    ))
                else:
                    seen_lines.append(key)

        return issues
