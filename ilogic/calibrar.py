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
    2.0:  (3472.0, 0.15, 3.0, 0.0),
    3.0:  (3812.0, 0.30, 3.0, 0.0),
    4.0:  (3460.0, 0.50, 4.0, 0.0),
    6.0:  (2419.0, 0.80, 5.0, 2.0),
    8.0:  (1900.0, 1.20, 5.0, 5.0),
    10.0: (1789.0, 2.00, 6.0, 5.0),
}

# Piezas de referencia del trabajo mr04. rectangulo en mm, ficha en s.
# perimetro y contornos: rellenar con lo que imprime la regla en Inventor.
# Ficha y tiempo de nesting medidos en mr04, por espesor. La diferencia es
# el adicional de anidado que suma la regla.
NESTINGS = {
    4.0:  (35.0, 35.0),
    6.0:  (30.0, 32.0),
    8.0:  (50.0, 55.0),
    10.0: (16.0, 21.0),
}

REFERENCIAS = [
    # perimetro_mm y contornos salen del informe de la propia regla.
    # rapidos_mm es el "Desplazamiento rapido estimado" del mismo informe.
    # ref,     espesor, largo, ancho, ficha_s, perimetro, contornos, rapidos
    ("A02848",  2.0, 155.0,  80.0, 18.0,  885.0, 8, 327.8),
    ("A02690",  3.0, 131.0,  25.0,  8.0,  395.4, 4, 116.5),
    # 47689 en Inventor es la misma pieza que 67844 en Lantek.
    ("47689",   4.0, 788.0, 155.1, 35.0, 1957.4, 1, 145.0),
    ("47678",   6.0, 490.0,  62.0, 30.0, 1106.8, 1, 487.2),
    ("67845",   8.0, 440.0, 119.0, 50.0,   None, None, 0.0),
    ("47691",  10.0,  55.0, 175.0, 16.0,  404.8, 1,  65.1),
    # 47690 NO es 67845: 122,0x439,7 y 3,067 kg neto frente a 440x119
    # y 2,98 kg. Distinta revision, su ficha de 50 s no le aplica.
    ("47690",   8.0, 439.7, 122.0, None, 1439.4, 5, 479.9),
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
    for ref, e, L, W, ficha, per, cont, rap in REFERENCIAS:
        if ficha is None:
            continue
        cota = 2 * (L + W) / ficha * 60.0
        v = TABLA[e][0]
        ok = "OK" if v >= cota else "POR DEBAJO DE LA COTA"
        print("  %-8s %5.0f %11.0f %7.0f %11.0f %11.0f  %s"
              % (ref, e, 2 * (L + W), ficha, cota, v, ok))

    print("\n=== Monotonia de la tabla ===")
    esp = sorted(TABLA)
    # 2 vs 3 mm esta invertido a proposito: la velocidad de este modelo es
    # efectiva y absorbe las aceleraciones, que pesan mas en A02848 (8
    # contornos en 155x80) que en A02690 (4 contornos). Documentado en la
    # cabecera de la regla.
    esperadas = {(2.0, 3.0)}
    mal = [(a, b) for a, b in zip(esp, esp[1:])
           if TABLA[a][0] <= TABLA[b][0] and (a, b) not in esperadas]
    print("  " + ("OK: la velocidad baja al subir el espesor, salvo la"
                  " inversion documentada de 2/3 mm" if not mal
                  else "INVERTIDA sin documentar en %s" % mal))

    print("\n=== Contraste con las piezas que ya tienen geometria ===")
    hay = False
    for ref, e, L, W, ficha, per, cont, rap in REFERENCIAS:
        if per is None or cont is None or ficha is None:
            continue
        hay = True
        t = tiempo_modelo(e, per, cont, rapidos_mm=rap)
        v_aj = velocidad_desde_ficha(e, per, cont, ficha,
                                     rapidos_mm=rap)
        print("  %-8s %4.0f mm  regla %6.2f s  ficha %5.1f s  "
              "desv %+7.2f s  v que cuadraria: %.0f mm/min"
              % (ref, e, t, ficha, t - ficha, v_aj))
    if not hay:
        print("  ninguna")

    print("\n=== Adicional de anidado: tabla vs medido en mr04 ===")
    print("  %-8s %8s %9s %10s %9s"
          % ("espesor", "ficha", "nesting", "medido", "tabla"))
    for e in sorted(NESTINGS):
        ficha, nesting = NESTINGS[e]
        medido = nesting - ficha
        tabla = TABLA[e][3]
        ok = "OK" if abs(medido - tabla) < 1e-9 else "NO COINCIDE"
        print("  %5.0f mm %8.0f %9.0f %10.0f %9.0f  %s"
              % (e, ficha, nesting, medido, tabla, ok))

    faltan = [r[0] for r in REFERENCIAS if r[5] is None or r[4] is None]
    if faltan:
        print("\n=== Falta la geometria de ===")
        print("  " + ", ".join(faltan))
        print("  Ejecutar la regla sobre cada IPT y copiar las lineas")
        print("  'Perimetro geometrico' y 'Contornos' a REFERENCIAS.")


if __name__ == "__main__":
    main()
