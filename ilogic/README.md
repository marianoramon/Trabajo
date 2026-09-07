# Buscador de ficheros en la red (regla iLogic)

`BuscadorFicheros.iLogicVb` es la regla externa de iLogic (Inventor 2026) que busca
piezas, ensamblajes y planos sobre un indice de red y, ademas, indica en que
ensamblajes se encuentra la pieza seleccionada.

## Como funciona la consulta de usos

1. **Indexar ahora** / **Actualizar** genera `IndiceBuscador_*.txt` (nombre, fecha y,
   opcionalmente, Nº de pieza y descripcion).
2. Al terminar, la regla recorre cada `.iam` del indice y escribe el fichero de
   relaciones `IndiceBuscador_*.txt.usos` con las lineas `R <ensamblaje> <componente>`.
3. Al seleccionar un resultado, el panel inferior invierte ese mapa y muestra los
   ensamblajes que contienen la pieza.

## Panel "Ensamblajes que utilizan la pieza" (v5.7)

| Columna | Contenido |
| --- | --- |
| Pieza buscada | Fichero seleccionado en los resultados |
| Ensamblaje | Nombre del ensamblaje que lo contiene |
| Uso | `Directo` si lo monta directamente, `Nivel N` si llega a traves de N subconjuntos; se anade `- conjunto final` cuando el ensamblaje no esta montado en ningun otro |
| Ruta del ensamblaje | Ruta completa (UNC si la unidad es de red) |

- Doble clic o Enter abre el ensamblaje en Inventor (si ya esta abierto, lo activa).
- Ctrl+C copia las rutas seleccionadas.
- Se ignoran las copias de `OldVersions`.
- Si falta el fichero `.usos`, el panel avisa de que hay que pulsar **Actualizar**.
