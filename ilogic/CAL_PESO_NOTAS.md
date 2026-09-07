# CAL_PESO — discrepancias frente a Lantek Expert

Notas de la revisión de la versión 1.5 y de los cambios aplicados en la 1.6.

## Resumen

La regla tenía **dos discrepancias estructurales** (peso rectangular/2 y factor
de calibración aplicado a las penetraciones) y **cuatro defectos** que provocan
saltos o errores fijos. Ninguno es un fallo de programación en sentido estricto:
son decisiones de modelo que no reproducen lo que hace Lantek.

## Peso

### 1. `rectángulo / 2` no es el peso que informa Lantek

Lantek da el peso neto de la pieza (área real × espesor × densidad), o el bruto
del rectángulo envolvente. Dividir el rectángulo entre 2 sólo coincide si el
aprovechamiento de la pieza dentro de su rectángulo es exactamente del 50 %.
En piezas compactas ese aprovechamiento está entre 0,70 y 0,90, así que la regla
queda entre un 30 % y un 45 % por debajo del peso neto.

Es un **criterio de imputación de empresa**, no un error. Se mantiene tal cual.
La 1.6 añade el peso neto real y el porcentaje de aprovechamiento al informe,
para poder ver de un vistazo cuánto se separa de Lantek en cada pieza.

### 2. Densidad 8050 kg/m³ — CONFIRMADA

Se sospechó que debía ser 7850 (acero al carbono estándar) y **era incorrecto**.
Despejada del peso de chapa de tres nestings de `mr04`:

| Chapa | Formato | Peso Lantek | Densidad |
|---|---|---|---|
| 0103335440- | 5000 × 1505 × 4 mm | 242,31 kg | 8050,2 |
| 2J | 6000 × 1507 × 6 mm | 436,73 kg | 8050,0 |
| 3RB | 4000 × 1510 × 8 mm | 388,98 kg | 8050,1 |

La constante se queda en 8050. Cerrado.

### 3. Rectángulo envolvente en otra orientación

`FlatPattern.Length/Width` es el bounding box en la orientación en la que
Inventor genera el desarrollo. Lantek gira la pieza al ángulo óptimo del
nesting, así que su rectángulo puede ser menor. Sin corrección aplicada.

## Tiempo

### 4. El factor empírico se aplicaba también a las penetraciones (causa principal)

En la 1.5:

    factor = 10 / (395,4/3100·60 + 4·2,0 + 116,5/18000·60) = 10 / 16,04 = 0,6234

De los 16,04 s del modelo, **8 s eran penetraciones** (el 50 %), por asumir
2,0 s por perforación en 3 mm. La perforación real en 3 mm con nitrógeno está
en torno a 0,2–0,4 s. El factor no corregía la velocidad: compensaba unas
penetraciones infladas rebajando todos los términos por igual. Los valores
efectivos que quedaban eran:

| Magnitud | Valor implícito en la 1.5 | Valor plausible |
|---|---|---|
| Velocidad de corte | 4973 mm/min | ~2820 mm/min |
| Penetración por contorno | 1,25 s | ~0,30 s |

Un único factor multiplicativo sobre una suma de términos con proporciones
distintas no puede ajustar dos familias de piezas a la vez. Efecto medido:

| Geometría | v1.5 | v1.6 | Desviación de la 1.5 |
|---|---|---|---|
| Referencia A02690 | 9,86 s | 10,00 s | −1,4 % |
| Perímetro largo, 2 contornos | 17,21 s | 26,64 s | **−35 %** |
| 30 agujeros pequeños | 44,89 s | 24,07 s | **+87 %** |

Es decir: la 1.5 **subestima** las piezas de perímetro largo y **sobreestima**
mucho las piezas con muchos contornos. Es exactamente el patrón de "a veces
cuadra y a veces no".

**Corrección (1.6):** se calibra la velocidad efectiva despejándola del punto de
referencia, con la penetración como término independiente:

    t_corte = 10 − 4·0,30 − 116,5/18000·60 = 8,4117 s
    v_ef    = 395,4 / 8,4117 · 60 = 2820,6 mm/min

Reconstruye la referencia exactamente en 10,000 s.

### 5. Faltaban las entradas y salidas por contorno

Lantek corta una entrada por contorno (2–5 mm) a velocidad de corte, y la
longitud que informa normalmente ya las incluye. El perímetro de Inventor no.
Se comparaban dos magnitudes distintas y además la calibración se hizo con ellas
mezcladas. La 1.6 añade `ENTRADA_*_MM` por perfil (3,0 mm en el de 3 mm) y lo
suma al perímetro antes de dividir por la velocidad.

### 6. Tolerancia de espesor de ±0,01 mm en la rama de 3 mm

Con una chapa de 2,98 o 3,02 mm la calibración **no entraba** y la pieza caía al
perfil general: factor 1,0 en vez de 0,62 y +5 s en vez de +4. El tiempo saltaba
un ~60 % sin ningún aviso. Además era incoherente con el ±0,05 de la rama de
6 mm. La 1.6 usa ±0,15 mm en ambas.

### 7. Huecos y salto en la escalera de suplementos

El criterio `<5 → +1; >10 y <30 → +5; >30 → +10` deja sin cubrir la banda
5–10 s y los valores exactos 5, 10 y 30 s, que caían a suplemento **0**:

| Tiempo base | Suplemento 1.5 | Resultado 1.5 |
|---|---|---|
| 4,9 s | +1 | 10 s |
| 5,0 s exacto | +0 | 5 s |
| 7 s | +0 | 10 s |
| 10,0 s exacto | +0 | 10 s |
| 10,1 s | +5 | 20 s |
| 30,0 s exacto | +0 | 30 s |

Entre 9,9 s y 10,1 s el resultado final salta de 10 s a 20 s, y muchas piezas
pequeñas caen justo ahí.

**Corrección (1.6):** escalera continua y monótona — `≤5 → +1; ≤30 → +5;
>30 → +10`.

**CONFIRMAR:** la banda 5–10 s se ha asignado a **+5 s**. Si en producción debe
ser +1 s, cambiar la primera condición de `ObtenerSuplementoTiempo` a 10,0.

### 8. El adicional de anidado se suma entero a cada pieza

Los +4 s (A02690) y +5 s (A02950) salieron de anidados de **una** unidad. En un
nesting de N piezas Lantek reparte ese tiempo común entre las N. Comparar contra
un anidado de 20 piezas sumando 4 s a cada una sobreestima. Sin corrección: haría
falta conocer el número de piezas del anidado, que la regla no tiene.

### 9. No hay modelo de aceleración

En contornos pequeños (agujeros de Ø8, esquinas vivas) la máquina nunca alcanza
la velocidad de régimen; Lantek sí lo modela. Las piezas con mucho detalle fino
se quedan siempre cortas en el término de recorrido. No corregido — requeriría
un modelo de aceleración o una velocidad efectiva dependiente del radio medio
del contorno.

## Defectos menores corregidos o señalados

- **Eliminado** el bloque `If codigo.StartsWith("A02950")`: seleccionaba por
  código de artículo, justo lo que la cabecera dice no hacer.
- `FlatPattern.TopFace` devuelve **una sola** cara. Si el desarrollo queda
  partido en varias caras superiores se pierde geometría de corte en silencio.
  La 1.6 informa del número de contornos y de la longitud, para contrastarlos
  con la ficha de Lantek.
- `EdgeLoops` cuenta un contorno por loop. Aristas de partición, marcados o
  embuticiones cuentan penetraciones que no existen. Mismo control.
- `EstimarRecorridoRapidoMm` usa el punto medio de la primera arista de cada
  loop y arranca del mínimo del RangeBox. No reproduce la secuenciación de
  Lantek, pero a 18000 mm/min aporta menos de 1 s. Sin cambios.

## Para seguir calibrando

Hace falta la ficha de Lantek (**no el anidado**) de 5–6 piezas de acero de
3 mm con geometrías deliberadamente distintas:

1. Dos o tres de perímetro largo y pocos contornos (chapa recortada, sin
   agujeros).
2. Dos o tres con muchos agujeros pequeños (≥20 contornos).
3. Una intermedia.

De cada una: **longitud de corte, número de perforaciones y tiempo de ficha**.
Con eso se ajustan `VELOCIDAD_CORTE_3MM_MM_MIN` y `PENETRACION_3MM_S` por
mínimos cuadrados sobre dos incógnitas, en lugar de despejar una sola desde un
único punto. Los perfiles de 6 mm y el general siguen **sin ningún punto medido**.


---

# Datos de `mr04` (07/09/2026) y calibración 1.7

Listado de elementos de trabajo con las seis piezas y tres nestings.

## Piezas de referencia

| Bono | Ref | Espesor | Gas | Rectángulo | Neto | Rect. | Ficha |
|---|---|---|---|---|---|---|---|
| 1 | A02848 | 2 mm | N2-120 | 155 × 80 | 0,16 kg | 0,20 kg | 18 s |
| 2 | A02690 | 3 mm | N2-120 | 131 × 25 | 0,07 kg | 0,08 kg | 8 s |
| 6 | 67844 | 4 mm | O2-212 | 788 × 155 | 3,73 kg | 3,93 kg | 35 s |
| 3 | 47678 | 6 mm | O2-212 | 490 × 62 | 1,42 kg | 1,47 kg | 30 s |
| 4 | 67845 | 8 mm | O2-212 | 440 × 119 | 2,98 kg | 3,37 kg | 50 s |
| 5 | 47691 | 10 mm | O2-212 | 55 × 175 | 0,49 kg | 0,77 kg | 16 s |

## El cálculo del rectángulo es exacto

Con densidad 8050 la regla reproduce el "Peso del rectángulo" de Lantek en las
seis piezas; las diferencias son milésimas de redondeo (máx. 0,005 kg). El
rectángulo, el espesor y la densidad están bien. **Toda la discrepancia de peso
es el divisor `/2`.**

## El divisor `/2` frente al peso neto

| Ref | Neto Lantek | Imputado regla | Desviación | Aprovechamiento real |
|---|---|---|---|---|
| A02848 | 0,16 | 0,10 | −37,5 % | 80,0 % |
| A02690 | 0,07 | 0,05 | −28,6 % | 87,5 % |
| 67844 | 3,73 | 2,00 | **−46,4 %** | 94,9 % |
| 47678 | 1,42 | 0,75 | **−47,2 %** | 96,6 % |
| 67845 | 2,98 | 1,70 | −43,0 % | 88,4 % |
| 47691 | 0,49 | 0,40 | −18,4 % | 63,6 % |

El aprovechamiento real va del 64 % al 97 %; el divisor asume 50 % y no lo
acierta en ninguna pieza. **Es criterio de imputación de empresa y no se cambia
por iniciativa propia**, pero conviene decidirlo con estos números delante.

## La ficha de A02690 son 8 s, no 10 s

La 1.5 y la 1.6 estaban ancladas a 10 s, un 25 % alto. Con 8 s la velocidad
calibrada de 3 mm pasa de 2820,6 a **3700 mm/min**:

    t_corte = 8 − 4·0,30 − 116,5/18000·60 = 6,4117 s
    v_ef    = 395,4 / 6,4117 · 60 = 3700 mm/min

Los 395,4 mm de Lantek **ya incluyen las entradas**: son 383,4 mm de perímetro
geométrico más 4 entradas de 3 mm. `calibrar.py` recibe el perímetro geométrico,
no el de Lantek.

Hipótesis de dónde salieron los 10 s: el catálogo tiene dos tecnologías para
3 mm (Oxygen-212 y Nitrogen-120) y la medida pudo tomarse de la de oxígeno.

## El adicional de anidado no es fijo

| Espesor | Ficha | Nesting | Adicional |
|---|---|---|---|
| 4 mm | 35 s | 35 s | **+0 s** |
| 6 mm | 30 s | 32 s | **+2 s** |
| 8 mm | 50 s | 55 s | **+5 s** |

La 1.6 sumaba +4 s (3 mm) o +5 s (resto) siempre. Los de 2, 3 y 10 mm siguen sin
medir; en la tabla van 0, 0 y 7 s, coherentes con la serie pero no medidos.
**Falta el tiempo de nesting del anidado de 3 mm (chapa 1151575311).**

## Tabla de perfiles 1.7

Sustituye a las tres ramas sueltas de la 1.6, que dejaban cinco de los seis
espesores en el perfil "general" y con las velocidades invertidas (5000 mm/min a
6 mm frente a 3100 al resto: más rápido cuanto más grueso).

Sólo 3 mm está calibrado. El resto son valores **acotados, no medidos**: la cota
inferior es el perímetro del rectángulo de la pieza de referencia dividido entre
su ficha. Como la longitud real de corte supera ese perímetro, la velocidad real
es algo mayor que la cota. Los valores elegidos respetan la cota y decrecen con
el espesor, que es lo mínimo exigible al modelo.

| Espesor | Gas | Cota inferior | Tabla | Penetración | Entrada | Anidado | Estado |
|---|---|---|---|---|---|---|---|
| 2 mm | N2 | 1567 | 5000 | 0,15 s | 3 mm | 0 s | extrapolado de 3 mm |
| 3 mm | N2 | 2340 | **3700** | 0,30 s | 3 mm | 0 s | **calibrado** |
| 4 mm | O2 | 3233 | 3400 | 0,50 s | 4 mm | 0 s | acotado |
| 6 mm | O2 | 2208 | 2400 | 0,80 s | 5 mm | 2 s | acotado |
| 8 mm | O2 | 1342 | 1900 | 1,20 s | 5 mm | 5 s | acotado |
| 10 mm | O2 | 1725 | 1750 | 2,00 s | 6 mm | 7 s | acotado |

La cota de 2 mm (1567 mm/min) no restringe nada: A02848 tiene un 20 % de su
rectángulo en agujeros, así que su ficha está dominada por longitud de corte
interior y penetraciones, no por el perímetro exterior.

## Para cerrar la calibración

Ejecutar la regla sobre los seis IPT y copiar las líneas **"Perímetro
geométrico"** y **"Contornos"** de cada informe a `REFERENCIAS` en
`ilogic/calibrar.py`. Con eso el script contrasta el modelo contra las seis
fichas y despeja la velocidad que cuadraría en cada espesor. Al calcular
cualquiera de las seis piezas, la regla ya imprime su propia desviación frente
al tiempo real de Lantek.

Con dos o más piezas por espesor se podrían ajustar velocidad y penetración a la
vez; con una sola hay que fijar la penetración y despejar la velocidad.


---

# Calibración 1.8: cuatro espesores ajustados contra ficha

Se ejecutó la regla 1.7 sobre las piezas reales en Inventor y se ajustó la
velocidad de cada espesor para que reproduzca su ficha de Lantek.

## Desviación que tenía la 1.7 y velocidad resultante

| Espesor | Pieza | Perímetro | Cont. | Regla 1.7 | Lantek | Desv. | v 1.7 | **v 1.8** |
|---|---|---|---|---|---|---|---|---|
| 2 mm | A02848 | 885,0 | 8 | 13,20 s | 18 s | **−26,66 %** | 5000 | **3472** |
| 3 mm | A02690 | 395,4 | 4 | 8,19 s | 8 s | +2,44 % | 3700 | **3812** |
| 6 mm | 47678 | 1106,8 | 1 | 30,22 s | 30 s | +0,73 % | 2400 | **2419** |
| 10 mm | 47691 | 404,8 | 1 | 16,30 s | 16 s | +1,88 % | 1750 | **1789** |

Los cuatro reproducen ahora su ficha con error nulo.

**El método de acotación se validó bien.** Las velocidades de 6 y 10 mm eran
estimaciones sin medir y quedaron a un 0,8 % y un 2,2 % del valor real. Las de
4 y 8 mm, que siguen sin pieza de referencia, deberían andar igual de cerca.

## La longitud que informa Lantek no incluye las entradas

El perímetro que Inventor da para A02690 es **395,4 mm**, exactamente el valor
que Lantek informa como longitud de corte. La suposición de la 1.6 —que la
longitud de Lantek ya incluía las entradas— era incorrecta.

Las entradas siguen en el modelo porque la máquina las corta y su tiempo está
dentro de la ficha, pero no son comparables con la longitud que informa Lantek.
El diagnóstico de la regla compara ya el perímetro geométrico.

## Los 2 mm quedan por debajo de los 3 mm, y no es un error

La velocidad ajustada de 2 mm (3472) es menor que la de 3 mm (3812). La
velocidad de este modelo es **efectiva**: absorbe aceleraciones y
deceleraciones, que pesan más cuanto más fragmentada es la pieza. A02848 son
8 contornos en una chapa de 155 × 80 con 885 mm de perímetro; A02690 son 4
contornos en 131 × 25.

Consecuencia práctica: en una pieza de 2 mm de contorno sencillo este perfil
**sobreestimará** el tiempo. Para separar velocidad de aceleración hace falta la
ficha de una segunda pieza de 2 mm simple.

## El peso neto sí coincide exacto

| Ref | Neto regla | Neto Lantek |
|---|---|---|
| A02848 | 0,165 kg | 0,16 kg |
| A02690 | 0,069 kg | 0,07 kg |
| 47678 | 1,423 kg | 1,42 kg |
| 47691 | 0,490 kg | 0,49 kg |

El área de la cara superior del desarrollo está bien calculada. Si algún día se
cambia el criterio de imputación al peso neto, cuadra con Lantek sin tocar nada
más. Toda la desviación de peso sigue siendo el divisor `/2`.

## Sigue pendiente

- Ficha de Lantek de una pieza de **4 mm** y otra de **8 mm**.
- Ficha de una pieza de **2 mm de contorno simple**, para separar velocidad de
  aceleración.
- Ficha de **47690** (8 mm, `CAJ REF APO EJE3000 PL`): se calculó con la regla
  pero no estaba en `mr04`, así que no hay tiempo real contra el que contrastar.
  La regla le estima 53,85 s.
- Tiempo de nesting del anidado de 3 mm (chapa 1151575311).
- Suplemento de la banda 5-10 s.


---

# 1.9: el adicional de anidado de 10 mm era extrapolado

La página 7 del listado de `mr04` (nesting 28734, chapa 1151631516) da el dato
que faltaba. El anidado de 10 mm tarda **21 s** frente a los 16 s de ficha de
47691, así que el adicional real es **+5 s**, no los +7 s que se habían
extrapolado suponiendo que la serie seguía creciendo.

| Espesor | Ficha | Nesting | Adicional |
|---|---|---|---|
| 4 mm | 35 s | 35 s | +0 s |
| 6 mm | 30 s | 32 s | +2 s |
| 8 mm | 50 s | 55 s | +5 s |
| **10 mm** | **16 s** | **21 s** | **+5 s** (era +7) |

**La serie se estanca en 5 s**, no crece con el espesor. Con la corrección,
47691 queda exacto en los dos tiempos: 15,99 s de ficha (Lantek 16 s) y 20,99 s
de anidado (Lantek 21 s).

Sólo quedan sin medir los adicionales de 2 y 3 mm. Se asumen 0 s, coherente con
el +0 s medido a 4 mm.

`calibrar.py` incorpora ahora `NESTINGS` y contrasta los cuatro adicionales
medidos contra la tabla de la regla, igual que ya hacía con las fichas.

## Densidad: cuarta confirmación

Chapa 3000 × 1510 × 10 mm con 364,67 kg → **8050,1 kg/m³**. Cuarta comprobación
independiente, en el cuarto espesor distinto. Cerrado del todo.
