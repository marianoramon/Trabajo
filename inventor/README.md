# Buscador de ficheros dentro de Autodesk Inventor (regla iLogic)

Esta regla se ejecuta **dentro de Autodesk Inventor**. Funciona con un **índice**:
recorre una vez la carpeta de red y guarda un fichero índice; después las
búsquedas por **código** son **casi instantáneas** porque leen el índice, no la
red. Encuentra **piezas (.ipt)**, **ensamblajes (.iam)** y **planos (.idw)**
y los **abre** en Inventor. Las **copias de seguridad** de Inventor (carpeta
`OldVersions`) se **excluyen** y no aparecen en las búsquedas.

Fichero de la regla: [`BuscarFicheros.iLogicVb`](BuscarFicheros.iLogicVb)

> Requiere Inventor 2019 o posterior (iLogic permite definir clases).

---

## 1. Cómo funciona (índice + búsqueda)

En la parte superior de la ventana eliges:

- **Carpeta a indexar**: la ruta de red que se recorrerá (UNC o unidad). Por
  defecto `P:\`. Botón **Examinar...** y desplegable **Unidad** (incluye la
  *ruta de red* `\\192.168.10.217\DISEÑO` y tus unidades `P:\`, `Q:\`…).
- **Carpeta del índice**: dónde se guarda el fichero índice `IndiceBuscador.txt`.
  Por defecto `Documentos\BuscadorInventor`. Puedes ponerlo en una carpeta de
  red para **compartir el índice con todo el equipo**.

### Opciones de indexado

- **Indexar también Nº de pieza (más lento)**: además del nombre del archivo,
  lee la iProperty **"Número de pieza" (Part Number)** de cada `.ipt`/`.iam`
  usando el **Apprentice Server** de Inventor. Así puedes buscar por el código
  aunque esté en el Nº de pieza y no en el nombre. La primera vez tarda más.
  Sin marcar, el indexado es **rápido** (solo nombre).

> **La indexación va siempre en segundo plano**: tras pulsar **Indexar ahora**
> puedes **cerrar la ventana y seguir trabajando** en Inventor con normalidad;
> el índice se sigue creando solo y queda guardado al terminar. Vuelve a abrir
> la herramienta cuando quieras buscar.

> La herramienta **nunca indexa sola**: solo se indexa cuando pulsas
> **Indexar ahora**. Al abrir la regla únicamente se cargan las rutas guardadas.

**Reindexado incremental:** al reindexar, los ficheros que **no han cambiado**
(misma fecha) **conservan su Nº de pieza** sin volver a leerlo. Por eso la
primera indexación completa tarda, pero las siguientes son rápidas (solo se
leen los ficheros nuevos o modificados).

### Varias unidades de red (varios índices)

Puedes indexar **varias unidades/carpetas de red**, cada una en su **propio
fichero** de índice: `IndiceBuscador_<ruta>.txt` dentro de la carpeta del índice.

- Para añadir una unidad: pon su ruta en **Carpeta a indexar** (o elígela en
  **Unidad**) y pulsa **Indexar ahora**. Se crea/actualiza su índice.
- Repite con otra unidad → se crea otro fichero de índice.
- La **búsqueda usa TODOS los índices** de la carpeta a la vez (combina los
  resultados y elimina duplicados). El recuadro de info muestra cuántos índices
  hay y qué carpetas raíz cubren.

### Flujo de uso

1. **Indexar ahora** cada unidad (una vez, o cuando haya cambios). Te dice
   cuántos ficheros ha indexado.
2. **Buscar**: escribe el código, elige *Todos / Piezas / Ensamblajes* y pulsa
   **Buscar**. Busca en **todos los índices** comparando el código con el
   **nombre** del archivo y con el **Nº de pieza**. Resultados al instante.
3. Con un resultado seleccionado:
   - **Abrir en Inventor** (o doble clic) → lo abre en Inventor.
   - **Abrir carpeta** → abre el **Explorador de Windows** con el fichero
     seleccionado.
   - La columna **Ruta** muestra la ubicación completa del fichero.

> El índice es una "foto" del momento en que se creó. Si añades o mueves
> ficheros, pulsa **Indexar ahora**. Si abres
> un fichero que ya no existe, la regla te avisa de que reindexes.

Las opciones (rutas y casillas) se recuerdan entre sesiones.

Las rutas elegidas se **recuerdan** entre sesiones en
`Documentos\BuscadorInventor\ajustes.txt`. Para cambiar los valores por defecto
de fábrica, edita las constantes al principio de la clase:

```vb
Private Const RUTA_DEFECTO As String = "P:\"
Private Const RUTA_RED     As String = "\\192.168.10.217\DISEÑO"
```

---

## 2. Instalar la regla en Inventor

### Como **Regla Externa** (recomendado: disponible en todos los documentos)

1. En Inventor: pestaña **Herramientas** → panel **iLogic** → **Reglas externas**
   (el navegador de iLogic, pestaña *External Rules*).
2. La primera vez, define la **carpeta de reglas externas**: botón derecho →
   *Set the External Rule Directories* y elige una carpeta (puede ser una de red
   para compartirla con el equipo).
3. Copia `BuscarFicheros.iLogicVb` a esa carpeta.
4. Aparecerá en la lista de reglas externas. **Doble clic** para ejecutarla.

### Alternativa rápida: regla interna

1. Abre cualquier documento (o uno nuevo).
2. **Administrar/Herramientas** → **Agregar regla** (*Add Rule*), ponle nombre
   (p. ej. *Buscar ficheros*).
3. Pega el contenido de `BuscarFicheros.iLogicVb` y guarda.
4. Ejecútala con doble clic en el navegador de iLogic.

---

## 3. (Opcional) Botón en la cinta de Inventor

Para tenerlo a un clic:

1. Ejecuta la regla una vez (para que Inventor la registre).
2. **Herramientas** → **Personalizar** (o clic derecho en la cinta →
   *Personalizar comandos de usuario*).
3. En la categoría **iLogic / Reglas externas**, arrastra *BuscarFicheros* a una
   pestaña o panel de la cinta.

También puedes asignarle un **atajo de teclado** desde *Personalizar*.

---

## 4. Uso

1. Lanza la regla → se abre la ventana **Buscar ficheros en la red**.
2. Escribe el **código** de la pieza (p. ej. `A01955`) y pulsa **Buscar** (o Enter).
3. Filtra por **Todos**, **Piezas (.ipt)** o **Ensamblajes (.iam)**.
4. Selecciona un resultado y pulsa **Abrir en Inventor** (o doble clic).

La búsqueda es por *coincidencia parcial* del código en el nombre del fichero,
sin distinguir mayúsculas, y recorre todas las subcarpetas. Las carpetas sin
permiso se omiten sin error. Para no saturar, se limita a 500 resultados (afina
el código si lo alcanzas).
