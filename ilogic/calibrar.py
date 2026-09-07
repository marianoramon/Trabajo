"""Calibracion de los perfiles de corte de CAL_PESO.iLogicVb.

Modelo de la regla, por pieza:

    t_ficha = (perimetro + contornos * entrada) / v * 60
              + contornos * t_penetracion
              + rapidos / v_rapida * 60

Con una sola pieza por espesor solo se puede despejar v fijando
t_penetracion. Con dos o mas piezas del mismo espesor se ajustan las dos
a la vez por minimos cuadrados, que es lo que hace falta para cerrar la
calibracion.

Uso: rellenar MEDICIONES con lo que informa la ficha de Lantek y la
propia regla (que imprime "Perimetro geometrico" y "Contornos"), y
ejecutar:  python3 ilogic/calibrar.py
"""

V_RAPIDA_MM_MIN = 18000.0

# Tabla actual de CAL_PESO.iLogicVb: espesor -> (v, t_pen, entrada, anidado)
TABLA = {
    2.0:  (5000.0, 0.15, 3.0, 0.0),
    3.0:  (3700.0, 0.30, 3.0, 0.0),
    4.0:  (3400.0, 0.50, 4.0, 0.0),
    6.0:  (2400.0, 0.80, 5.0, 2.0),
    8.0:  (1900.0, 1.20, 5.0, 5.0),
    10.0: (1750.0, 2.00, 6.0, 7.0),
}

# Piezas de referencia del trabajo mr04. rectangulo en mm, ficha en s.
# perimetro y contornos: rellenar con lo que imprime la regla en Inventor.
REFERENCIAS = [
    # perimetro_mm es el PERIMETRO GEOMETRICO que imprime la regla desde
    # Inventor, sin entradas. La longitud que informa Lantek ya las lleva:
    # A02690 son 395,4 mm en Lantek = 383,4 de perimetro + 4 entradas de 3.
    # ref,      espesor, largo,  ancho, ficha_s, perimetro_mm, contornos
    ("A02848",  2.0,  155.0,  80.0, 18.0, None, None),
    ("A02690",  3.0,  131.0,  25.0,  8.0, 383.4, 4),
    ("67844",   4.0,  788.0, 155.0, 35.0, None, None),
    ("47678",   6.0,  490.0,  62.0, 30.0, None, None),
    ("67845",   8.0,  440.0, 119.0, 50.0, None, None),
    ("47691",  10.0,   55.0, 175.0, 16.0, None, None),
]


def tiempo_modelo(espesor, perimetro, contornos, rapidos_mm=0.0):
    v, t_pen, entrada, _ = TABLA[espesor]
    longitud = perimetro + contornos * entrada
    return (longitud / v * 60.0
            + contornos * t_pen
            + rapidos_mm / V_RAPIDA_MM_MIN * 60.0)


def velocidad_desde_ficha(espesor, perimetro, contornos, ficha_s,
                          rapidos_mm=0.0):
    """Despeja v dejando fijos t_penetracion y entrada."""
    _, t_pen, entrada, _ = TABLA[espesor]
    resto = ficha_s - contornos * t_pen - rapidos_mm / V_RAPIDA_MM_MIN * 60.0
    if resto <= 0:
        return None
    return (perimetro + contornos * entrada) / resto * 60.0


def main():
    print("=== Cota inferior de velocidad por espesor ===")
    print("La longitud real de corte es mayor que el perimetro del")
    print("rectangulo, asi que la velocidad real supera esta cota.\n")
    print("  %-8s %6s %11s %7s %11s %11s %s"
          % ("ref", "esp", "perim_bbox", "ficha", "cota", "tabla", ""))
    for ref, e, L, W, ficha, per, cont in REFERENCIAS:
        cota = 2 * (L + W) / ficha * 60.0
        v = TABLA[e][0]
        ok = "OK" if v >= cota else "POR DEBAJO DE LA COTA"
        print("  %-8s %5.0f %11.0f %7.0f %11.0f %11.0f  %s"
              % (ref, e, 2 * (L + W), ficha, cota, v, ok))

    print("\n=== Monotonia de la tabla ===")
    esp = sorted(TABLA)
    mal = [(a, b) for a, b in zip(esp, esp[1:]) if TABLA[a][0] <= TABLA[b][0]]
    print("  " + ("OK: la velocidad baja al subir el espesor" if not mal
                  else "INVERTIDA en %s" % mal))

    print("\n=== Contraste con las piezas que ya tienen geometria ===")
    hay = False
    for ref, e, L, W, ficha, per, cont in REFERENCIAS:
        if per is None or cont is None:
            continue
        hay = True
        t = tiempo_modelo(e, per, cont, rapidos_mm=116.5)
        v_aj = velocidad_desde_ficha(e, per, cont, ficha,
                                     rapidos_mm=116.5)
        print("  %-8s %4.0f mm  regla %6.2f s  ficha %5.1f s  "
              "desv %+7.2f s  v que cuadraria: %.0f mm/min"
              % (ref, e, t, ficha, t - ficha, v_aj))
    if not hay:
        print("  ninguna")

    faltan = [r[0] for r in REFERENCIAS if r[5] is None]
    if faltan:
        print("\n=== Falta la geometria de ===")
        print("  " + ", ".join(faltan))
        print("  Ejecutar la regla sobre cada IPT y copiar las lineas")
        print("  'Perimetro geometrico' y 'Contornos' a REFERENCIAS.")


if __name__ == "__main__":
    main()
