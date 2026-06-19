# Plano de soldadura automático (regla iLogic)

Genera el **plano de soldadura** del **ensamblaje activo (.iam)** en Inventor.

Fichero: [`PlanoSoldadura.iLogicVb`](PlanoSoldadura.iLogicVb)

## Qué hace

Con un ensamblaje `.iam` abierto y guardado, al ejecutar la regla:

1. Crea un plano desde **tu plantilla de soldadura**.
2. Si el conjunto mide > 800 mm, pasa la hoja a **A3 apaisado**.
3. Coloca **vistas** con escala automática: frontal (principal) + superior +
   lateral + isométrica.
4. Inserta la **lista de componentes (BOM)** arriba a la derecha.
5. Pone **globos numerados** en las piezas del conjunto (ligados a la BOM),
   en filas centradas sobre la vista principal (sin solapamientos).
6. Añade la **nota de soldadura** (esquina superior izquierda).
7. Añade la **secuencia de soldadura** (13 pasos, esquina inferior izquierda
   sobre el cajetín). Edita `SECUENCIA_SOLDADURA` para personalizar los pasos.
8. Rellena el **cajetín** con las iProperties del ensamblaje (artículo,
   descripción, escala, centro = SOLDADURA…).
9. Guarda el `.idw` + **PDF** + **DWF** en la **misma carpeta que el .iam**.

## Antes de usarla: configurar la plantilla

Abre la regla y edita arriba la ruta de **tu** plantilla de soldadura (sin
extensión; la regla prueba `.idw` y `.dwg`):

```vb
Dim RUTA_PLANTILLA_BASE As String = "Q:\BIBLIOTECA INVENTOR 2019\PLANTILLAS 2019\PLANO SOLDADURA V19 -LOGO NUEVO"
```

También puedes ajustar la nota, el centro de trabajo y la **secuencia de soldadura**:

```vb
Dim NOTA_SOLDADURA As String = "NOTA: Todas las soldaduras cordon continuo a doble cara donde sea accesible."
Dim CENTRO_TRABAJO As String = "SOLDADURA"

' Edita los 13 pasos para adaptarlos a tu conjunto:
Dim SECUENCIA_SOLDADURA As String = _
    "SECUENCIA DE SOLDADURA:" & vbCrLf & _
    "1. Limpiar y desengrasas todas las superficies a soldar." & vbCrLf & _
    ...
```

La secuencia se coloca automáticamente en la **esquina inferior izquierda**, sobre el cajetín.

## Instalar y ejecutar

Igual que la regla del buscador: **Herramientas → iLogic → Reglas externas**
(recomendado) o pégala en una regla nueva. Abre el ensamblaje, guárdalo y
ejecuta la regla.

## Notas / límites de esta primera versión

- Es una **base automática**: coloca vistas, BOM, globos, nota de soldadura
  y secuencia de soldadura. La distribución fina y los elementos específicos
  del conjunto (mover globos, **vistas de sección** B-B/C-C…, **vistas de
  detalle** ampliadas) se añaden **a mano** sobre el plano generado, ya que
  requieren conocer la geometría concreta del modelo.
- Los **globos** se reparten en filas centradas sobre la vista principal con
  separación fija de 1,8 cm entre centros. Si el conjunto tiene muchas piezas
  puede que tengas que reposicionar alguno manualmente.
- La **BOM** usa el estilo de lista de la plantilla. Si tu plantilla no tiene
  estilo de lista, la tabla puede salir con formato por defecto.
- Cada bloque va protegido con `Try`, así que aunque falle una parte (p. ej.
  una vista proyectada), el plano se genera igualmente.

Si algo no sale como en tus planos de ejemplo, dime qué parte (vistas, BOM,
globos, cajetín) y lo afino.
