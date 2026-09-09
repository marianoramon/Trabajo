# Buscador CAD (regla iLogic)

`BuscadorFicheros.iLogicVb` es la regla externa de iLogic (Inventor 2026) que busca
piezas, ensamblajes y planos sobre un índice de red e indica en qué ensamblajes se
encuentra la pieza seleccionada.

## Cómo funciona el índice

1. **Reindexar** / **Actualizar** recorre la carpeta y escribe `IndiceBuscador_*.txt`
   con nombre, fecha y ruta de cada `.ipt`, `.iam` e `.idw`. Solo lee el sistema de
   ficheros: **no necesita abrir nada en Inventor**.
2. Al terminar lanza un proceso aparte (PowerShell + una instancia oculta de Inventor)
   que recorre cada `.iam` y escribe `IndiceBuscador_*.txt.usos` con las relaciones
   `R <ensamblaje> <componente>`. Mientras corre puedes seguir trabajando.
3. El panel derecho invierte ese mapa y lista los ensamblajes que contienen la pieza.

## Teclado y cierre (v7.6)

Un formulario **no modal** dentro de Inventor no tiene bucle de mensajes propio:
Inventor procesa las teclas primero y se queda con Intro y Escape. Por eso la regla
no depende de ellas:

- **La búsqueda se lanza sola** 350 ms después de dejar de teclear, a partir de 2
  caracteres. No hace falta pulsar Intro.
- **El botón ✕ de la cabecera** cierra la ventana. No hace falta pulsar Escape.
- Intro y Escape siguen cableados por cuatro vías (`ProcessCmdKey`, `ProcessDialogKey`,
  `KeyDown` del formulario e `IMessageFilter`) por si Inventor las suelta.
- Relanzar la regla activa la ventana ya abierta en lugar de duplicarla.

## Interfaz

Ventana sin bordes de 1320 × 742 con cabecera propia (arrastre desde la cabecera,
redimensión desde la esquina inferior derecha). Paleta oscura ajustada al esquema de
Inventor, con acento ámbar en las acciones principales.

| Franja | Contenido |
| --- | --- |
| Cabecera | Título, píldora de índice, pestaña activa y `– □ ✕` |
| Tarjeta de índice | Unidad, carpeta a indexar, carpeta del índice, **Reindexar** / **Actualizar** |
| Búsqueda | Campo grande, botón limpiar y botón Buscar |
| Filtros | Todos · `.ipt` Piezas · `.iam` Ensamblajes · `.idw` Planos + contador |
| Lista | Código y nombre, formato, modificado, ruta compartida |
| Panel derecho | Ensamblajes vinculados, ruta del seleccionado y **Abrir ensamblaje IAM** |
| Acciones | Copiar ruta, Copiar fichero, Mover, Borrar, Explorar carpeta, **Abrir en Inventor** |

## Limitaciones conocidas

- Los ensamblajes solo salen si su carpeta está **indexada**. Una pieza usada en un
  conjunto de una carpeta sin indexar parecerá no usada.
- El indexado por **Nº de pieza y descripción** (iProperties) está desactivado: exige
  abrir cada fichero en Inventor y bloquea la sesión. El código se conserva en
  `LeerDocumentoParaIndice` por si se vuelve a habilitar.
- Se ignoran las carpetas `OldVersions`.
