# Buscador de Ficheros — Complemento (Add-In) de Autodesk Inventor

Aplicación que **se integra en Inventor a través de su API .NET**. Se carga al
arrancar Inventor y añade un botón **"Buscar ficheros"** en la pestaña
**Herramientas** de la cinta. Al pulsarlo se abre una ventana para buscar
**piezas (.ipt)** y **ensamblajes (.iam)** en la red por **código** y abrirlos
directamente en Inventor.

A diferencia de la regla iLogic (carpeta `../inventor`), esto es un **add-in
compilado**: botón permanente en la cinta, disponible siempre, sin tener que
ejecutar una regla.

## Contenido del proyecto

| Fichero | Función |
|---|---|
| `BuscadorFicheros.csproj` | Proyecto C# (.NET Framework 4.8) |
| `StandardAddInServer.cs` | Servidor del add-in + botón en la cinta |
| `SearchForm.cs` | Ventana de búsqueda (WinForms) |
| `FileSearcher.cs` | Motor de búsqueda en carpetas de red |
| `Config.cs` | Lee las carpetas desde `Documentos\BuscadorInventor\carpetas.txt` |
| `BuscadorFicheros.addin` | Manifiesto que Inventor lee para cargar el add-in |

## Requisitos

- Autodesk Inventor instalado (probado para 2022+; ajusta la versión en el `.csproj`).
- **Visual Studio 2022** (o Build Tools) con **.NET Framework 4.8 SDK**.
- Permisos de administrador para registrar el add-in (regasm).

## 1. Compilar

1. Abre `BuscadorFicheros.csproj` en Visual Studio (o usa `dotnet build`).
2. **Importante:** en el `.csproj`, ajusta la ruta de
   `Autodesk.Inventor.Interop.dll` a tu versión de Inventor, p. ej.:
   ```
   C:\Program Files\Autodesk\Inventor 2024\Bin\Public Assemblies\Autodesk.Inventor.Interop.dll
   ```
3. Compila en **Release**. Obtendrás `bin\Release\net48\BuscadorFicheros.dll`
   y el `BuscadorFicheros.addin` copiado al lado.

```powershell
dotnet build BuscadorFicheros.csproj -c Release
```

## 2. Registrar el add-in (COM)

Inventor carga el add-in vía COM, así que la DLL debe registrarse. Abre una
consola **como administrador** en la carpeta de salida y ejecuta:

```powershell
# Ruta de RegAsm del .NET Framework 4.x (ajústala si difiere)
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\RegAsm.exe BuscadorFicheros.dll /codebase
```

Para **desregistrar** más adelante:

```powershell
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\RegAsm.exe BuscadorFicheros.dll /unregister
```

## 3. Instalar el manifiesto `.addin`

Copia `BuscadorFicheros.addin` a la carpeta de add-ins de Inventor (crea la
carpeta `Addins` si no existe). Usa **una** de estas ubicaciones:

- Solo tu usuario:
  `%APPDATA%\Autodesk\Inventor <AÑO>\Addins\`
- Todos los usuarios del equipo:
  `%PROGRAMDATA%\Autodesk\Inventor <AÑO>\Addins\`

> El `.addin` apunta a `BuscadorFicheros.dll` por nombre. Como la DLL se
> registró con `/codebase`, Inventor la localizará. Si prefieres, indica la
> ruta completa en el elemento `<Assembly>` del `.addin`.

## 4. Configurar las carpetas de red

La primera vez se crea automáticamente:

```
Documentos\BuscadorInventor\carpetas.txt
```

Edítalo y pon una ruta por línea (UNC o unidad mapeada):

```
\\servidor\DISEÑOS
Q:\CORTE LASER
```

## 5. Usar

1. Arranca Inventor → en **Herramientas** aparece el botón **Buscar ficheros**.
   (En *Mis complementos* / *Add-In Manager* puedes verlo cargado.)
2. Pulsa el botón → escribe el **código** (p. ej. `A01955`), elige **Todos /
   Piezas / Ensamblajes** y pulsa **Buscar**.
3. Selecciona un resultado y **Abrir en Inventor** (o doble clic).

La búsqueda es por coincidencia parcial del código en el nombre, recorre
subcarpetas, ignora carpetas sin permiso y se limita a 500 resultados.

## Notas

- Si el botón no aparece: abre **Herramientas → Add-Ins** (Complementos),
  comprueba que *Buscador de Ficheros* está en la lista y con *Cargar al inicio*
  marcado. Revisa que el `.addin` esté en la carpeta correcta y la DLL registrada.
- El "código" se busca en el **nombre del fichero**. Si tu código vive en una
  iProperty (Número de pieza), se puede ampliar para leer iProperties; pídelo.
