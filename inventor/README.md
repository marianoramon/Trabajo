# Buscador de ficheros dentro de Autodesk Inventor (regla iLogic)

Esta regla se ejecuta **dentro de Autodesk Inventor**. Escribes un **código**
y te busca las **piezas (.ipt)** y **ensamblajes (.iam)** que lo contienen en
las carpetas de red, y te permite **abrirlos directamente** en Inventor.

Fichero de la regla: [`BuscarFicheros.iLogicVb`](BuscarFicheros.iLogicVb)

> Requiere Inventor 2019 o posterior (iLogic permite definir clases).

---

## 1. Configurar las carpetas de red

Tienes dos opciones:

**Opción A — Editar la regla** (rápido)
Abre `BuscarFicheros.iLogicVb` y cambia la lista del principio:

```vb
carpetas.Add("\\servidor\DISEÑOS")
carpetas.Add("Q:\DISEÑOS")
```

**Opción B — Fichero de configuración** (recomendado, no tocas la regla)
Crea el fichero de texto:

```
Documentos\BuscadorInventor\carpetas.txt
```

Una ruta por línea (las líneas que empiezan por `#` se ignoran):

```
\\servidor\DISEÑOS
Q:\CORTE LASER
```

Si este fichero existe, sus carpetas tienen prioridad sobre la lista de la regla.

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
