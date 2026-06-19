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
5. Pone **globos numerados** en las piezas del conjunto (ligados a la BOM).
6. Añade la **nota de soldadura**.
7. Rellena el **cajetín** con las iProperties del ensamblaje (artículo,
   descripción, escala, centro = SOLDADURA…).
8. Guarda el `.idw` + **PDF** + **DWF** en la **misma carpeta que el .iam**.

## Antes de usarla: configurar la plantilla

Abre la regla y edita arriba la ruta de **tu** plantilla de soldadura (sin
extensión; la regla prueba `.idw` y `.dwg`):

```vb
Dim RUTA_PLANTILLA_BASE As String = "Q:\BIBLIOTECA INVENTOR 2019\PLANTILLAS 2019\PLANO SOLDADURA V19 -LOGO NUEVO"
```

También puedes ajustar la nota y el centro de trabajo:

```vb
Dim NOTA_SOLDADURA As String = "NOTA: Todas las soldaduras cordon continuo a doble cara donde sea accesible."
Dim CENTRO_TRABAJO As String = "SOLDADURA"
```

## Instalar y ejecutar

Igual que la regla del buscador: **Herramientas → iLogic → Reglas externas**
(recomendado) o pégala en una regla nueva. Abre el ensamblaje, guárdalo y
ejecuta la regla.

## Notas / límites de esta primera versión

- Es una **base automática**: coloca vistas, BOM, globos y nota. La
  distribución fina (mover globos que se solapen, vistas de detalle/sección
  como B-B, C-C…, secuencia de soldeo) se hace a mano sobre el plano generado.
- Los **globos** se reparten en dos filas sobre la vista principal; puede que
  tengas que reposicionar alguno.
- La **BOM** usa el estilo de lista de la plantilla. Si tu plantilla no tiene
  estilo de lista, la tabla puede salir con formato por defecto.
- Cada bloque va protegido con `Try`, así que aunque falle una parte (p. ej.
  una vista proyectada), el plano se genera igualmente.

Si algo no sale como en tus planos de ejemplo, dime qué parte (vistas, BOM,
globos, cajetín) y lo afino.
