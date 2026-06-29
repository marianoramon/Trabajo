Sub Main()

    ' PLEGADO v10.26 - escala normalizada maxima real y SOLO REVISION SIN GUARDAR
    ' Layout normalizado segun planos correctos A4/A3.
    ' La unica vista base del modelo plegado es VISTA PRINCIPAL.
    ' VISTA LATERAL, VISTA SUPERIOR e ISOMETRICA se crean con AddProjectedView.
    ' Todas las vistas proyectadas heredan automaticamente escala, orientacion y estado del modelo.
    ' El DESARROLLO sigue siendo una vista base independiente porque representa el Flat Pattern.
    ' Fuerza la salida del entorno de desarrollo antes de crear la vista principal.
    ' MODO SEGURO: genera el plano para revision, pero NO guarda, NO reemplaza y NO exporta archivos.
    ' El rotulo COD PLEG usa texto simple, limita su posicion a la hoja y nunca bloquea el plano.


    Dim invApp As Inventor.Application = ThisApplication
    Dim tg As TransientGeometry = invApp.TransientGeometry

    Dim RUTA_PLANTILLA_BASE As String = "Q:\BIBLIOTECA INVENTOR 2019\PLANTILLAS 2019\PLANO METALPLAK V19 -LOGO NUEVO"
    Dim RUTA_BASE_PLANOS_IDW As String = "Q:\DISEÑOS\PLANOS PRODUCCIÓN"
    Dim NOMBRE_CARPETA_SALIDA As String = "02_PLANOS_PLEGADO"
    Dim NOMBRE_SIMBOLO_DATOS As String = "DATOS PLEGADO"

    ' Escalas normalizadas de reduccion: 1:1, 1:2, 1:5, 1:10, 1:20, 1:50 y 1:100.
    Dim ESCALAS() As Double = {1, 0.5, 0.2, 0.1, 0.05, 0.02, 0.01}
    Dim AUMENTAR_ESCALA_UN_PASO As Boolean = False

    ' False = solo genera y deja abierto el plano para revision manual.
    ' No guarda el IPT, no crea/reemplaza IDW y no exporta PDF/DWF.
    ' Cambiar a True solo cuando se quiera recuperar el guardado automatico.
    Dim GUARDADO_AUTOMATICO As Boolean = False

    Dim docActivo As Document = invApp.ActiveDocument

    If docActivo Is Nothing Then
        MessageBox.Show("No hay ningun documento activo.", "Plano de plegado")
        Exit Sub
    End If

    If docActivo.DocumentType <> DocumentTypeEnum.kPartDocumentObject Then
        MessageBox.Show("Esta version es solo para pieza individual. Abre una pieza .IPT de chapa.", "Plano de plegado")
        Exit Sub
    End If

    Dim partDoc As PartDocument = TryCast(docActivo, PartDocument)

    If partDoc Is Nothing Then
        MessageBox.Show("No se ha podido leer la pieza activa.", "Plano de plegado")
        Exit Sub
    End If

    If partDoc.FullFileName = "" Then
        MessageBox.Show("Guarda primero la pieza antes de generar el plano.", "Plano de plegado")
        Exit Sub
    End If

    If Not EsPiezaChapa(partDoc) Then
        MessageBox.Show("La pieza activa no es de chapa. Revisa que el subtipo sea Chapa.", "Plano de plegado")
        Exit Sub
    End If

    Try
        CrearPlanoPlegadoPiezaIndividual(invApp, tg, partDoc, RUTA_PLANTILLA_BASE, RUTA_BASE_PLANOS_IDW, NOMBRE_CARPETA_SALIDA, NOMBRE_SIMBOLO_DATOS, ESCALAS, AUMENTAR_ESCALA_UN_PASO, GUARDADO_AUTOMATICO)

        If GUARDADO_AUTOMATICO Then
            MessageBox.Show("Plano de plegado generado y guardado correctamente.", "Plano de plegado")
        Else
            MessageBox.Show( _
                "Plano de plegado generado para revision." & vbCrLf & vbCrLf & _
                "No se ha guardado ni reemplazado ningun archivo." & vbCrLf & _
                "Tampoco se han exportado PDF ni DWF." & vbCrLf & vbCrLf & _
                "Cuando lo revises, utiliza GUARDAR COMO manualmente.", _
                "Plano de plegado - sin guardar")
        End If
    Catch ex As Exception
        MessageBox.Show("No se ha podido generar el plano de plegado:" & vbCrLf & vbCrLf & ex.Message, "Plano de plegado")
    End Try

End Sub

'---------------------------------------------------------
' FUNCION PRINCIPAL
'---------------------------------------------------------

Sub CrearPlanoPlegadoPiezaIndividual(ByVal invApp As Inventor.Application, _
                                     ByVal tg As TransientGeometry, _
                                     ByVal partDoc As PartDocument, _
                                     ByVal rutaPlantillaBase As String, _
                                     ByVal rutaBasePlanosIDW As String, _
                                     ByVal nombreCarpetaSalida As String, _
                                     ByVal nombreSimboloDatos As String, _
                                     ByVal escalasDisponibles() As Double, _
                                     ByVal aumentarEscalaUnPaso As Boolean, _
                                     ByVal guardarAutomaticamente As Boolean)

    Dim smDef As SheetMetalComponentDefinition = TryCast(partDoc.ComponentDefinition, SheetMetalComponentDefinition)

    If smDef Is Nothing Then
        Throw New Exception("No se puede acceder a la definicion de chapa de la pieza.")
    End If

    If Not smDef.HasFlatPattern Then
        Try
            smDef.Unfold()
            partDoc.Update2(True)

            ' En modo de revision no se guarda automaticamente la pieza IPT.
            If guardarAutomaticamente Then
                partDoc.Save()
            End If
        Catch ex As Exception
            Throw New Exception("La pieza no se puede desplegar automaticamente. Detalle: " & ex.Message)
        End Try
    End If

    If Not smDef.HasFlatPattern Then
        Throw New Exception("La pieza sigue sin tener desarrollo plano despues de intentar desplegarla.")
    End If

    ' IMPORTANTE:
    ' Si la pieza ya tenia desarrollo y la regla se ejecuta estando dentro de Flat Pattern,
    ' la vista de desarrollo se crea, pero Inventor puede rechazar la primera vista plegada.
    ' Por eso se fuerza siempre la vuelta al modelo plegado, exista o no un desarrollo previo.
    ForzarSalidaEntornoDesarrollo(partDoc)

    '---------------------------------------------------------
    ' LEER iPROPERTIES DE LA PIEZA
    '---------------------------------------------------------

    Dim codPleg As String = LeerPropiedad(partDoc, "Design Tracking Properties", "Part Number")
    Dim codAlmacen As String = LeerPropiedad(partDoc, "Design Tracking Properties", "Stock Number")
    Dim descripcion As String = LeerPropiedad(partDoc, "Design Tracking Properties", "Description")
    Dim proyecto As String = LeerPropiedad(partDoc, "Design Tracking Properties", "Project")
    ' El campo PLANO REALIZADO usa el usuario actual configurado en Inventor.
    ' Si Inventor no devuelve un nombre, se usa el usuario actual de Windows.
    Dim disenador As String = ObtenerAutorActual(invApp)
    Dim material As String = LeerPropiedad(partDoc, "Design Tracking Properties", "Material")

    Dim revision As String = LeerPropiedad(partDoc, "Inventor Summary Information", "Revision Number")
    If revision = "" Then revision = LeerPropiedad(partDoc, "Summary Information", "Revision Number")
    If revision = "" Then revision = LeerPropiedad(partDoc, "Design Tracking Properties", "Revision Number")

    Dim estadoDiseno As String = LeerPropiedad(partDoc, "Design Tracking Properties", "Design Status")
    Dim revisadoPor As String = LeerPropiedad(partDoc, "Design Tracking Properties", "Checked By")
    Dim aprobadoFabPor As String = LeerPropiedad(partDoc, "Design Tracking Properties", "Mfg Approved By")

    Dim codCorte As String = LeerPropiedadUsuario(partDoc, "CORTE")
    Dim espesorTexto As String = LeerPropiedadUsuario(partDoc, "ESPESOR")
    Dim matriz As String = LeerPropiedadUsuario(partDoc, "MATRIZ")

    ' El nombre del IPT es la fuente prioritaria para el nombre del plano.
    ' Ejemplo: A02896.ipt o A02896 - DESCRIPCION.ipt -> A02896.idw
    Dim codigoDesdeArchivo As String = ExtraerCodigoDesdeNombrePieza(partDoc.FullFileName)
    If codigoDesdeArchivo <> "" Then codPleg = codigoDesdeArchivo

    If codPleg = "" And codAlmacen <> "" Then codPleg = codAlmacen
    If codPleg = "" Then codPleg = System.IO.Path.GetFileNameWithoutExtension(partDoc.FullFileName)

    codPleg = NormalizarCodigoParaRango(codPleg)

    If codCorte = "" Then codCorte = codAlmacen
    If codCorte = "" Then codCorte = codPleg
    If espesorTexto = "" Then espesorTexto = ObtenerEspesorChapa(partDoc)
    If descripcion = "" Then descripcion = System.IO.Path.GetFileNameWithoutExtension(partDoc.FullFileName)

    '---------------------------------------------------------
    ' CODIGO DE PLEGADO MANUAL
    '---------------------------------------------------------
    ' Antes de insertar el rotulo COD PLEG, la regla pregunta el codigo.
    ' Como valor inicial reutiliza la propiedad personalizada COD_PLEGADO,
    ' si ya existe en la pieza.
    Dim codigoPlegadoManual As String = _
        PedirCodigoPlegado(partDoc)

    If codigoPlegadoManual <> "" Then
        EscribirPropiedadUsuario(partDoc, "COD_PLEGADO", codigoPlegadoManual)
    End If

    '---------------------------------------------------------
    ' LOCALIZAR PLANTILLA Y CREAR CARPETA DE SALIDA
    '---------------------------------------------------------

    Dim rutaPlantilla As String = ObtenerRutaPlantilla(rutaPlantillaBase)

    If rutaPlantilla = "" Then
        Throw New Exception("No se ha encontrado la plantilla. Revisa la ruta o anade la extension .idw/.dwg.")
    End If

    If Not System.IO.File.Exists(rutaPlantilla) Then
        Throw New Exception("No existe la plantilla: " & rutaPlantilla)
    End If

    '---------------------------------------------------------
    ' CARPETA DE SALIDA SEGUN ESTRUCTURA PLANOS PRODUCCION
    '---------------------------------------------------------
    '
    ' Ya no guarda el IDW en la carpeta de la pieza.
    ' Guarda en:
    ' Q:\DISEÑOS\PLANOS PRODUCCIÓN\RANGO IDW
    '
    ' Ejemplos:
    ' A02871 -> A02000-A02999 IDW
    ' A02034 -> A02000-A02999 IDW
    ' 44561  -> 44000-44999 IDW si existe esa carpeta
    ' 84561  -> 84500-84999 IDW si existe la estructura por bloques de 500
    '
    ' La regla primero intenta respetar las carpetas existentes.
    ' Si no encuentra carpeta existente, crea la carpeta de rango de 1000.

    Dim carpetaSalida As String = ObtenerCarpetaPlanoIDWPorCodigo(rutaBasePlanosIDW, codPleg)

    ' En modo manual no se crean carpetas ni se escribe nada en disco.
    If guardarAutomaticamente Then
        If Not System.IO.Directory.Exists(carpetaSalida) Then
            System.IO.Directory.CreateDirectory(carpetaSalida)
        End If
    End If

    '---------------------------------------------------------
    ' CREAR PLANO
    '---------------------------------------------------------

    ' Compatibilidad Inventor 2026 con plantillas IDW historicas:
    ' Documents.Add puede quedar bloqueado al migrarlas. Se abre el IDW base
    ' y al final se guarda con el codigo nuevo; el original no se modifica.
    Dim drawingDoc As DrawingDocument = TryCast(invApp.Documents.Open(rutaPlantilla, True), DrawingDocument)

    If drawingDoc Is Nothing Then
        Throw New Exception("No se ha podido abrir el IDW base.")
    End If

    Dim sheet As Sheet = drawingDoc.ActiveSheet

    ' La plantilla debe quedar limpia antes de crear las vistas.
    ' Esto evita duplicados cuando el IDW base contiene vistas antiguas.
    BorrarTodasLasVistasExistentes(sheet)

    ' Si el desarrollo de la pieza supera 800 mm, usar A3 apaisado.
    Dim mayorDimensionPiezaMM As Double = ObtenerMayorDimensionFlatPatternMM(smDef)
    Dim piezaLargaA3 As Boolean = mayorDimensionPiezaMM > 800

    If piezaLargaA3 Then
        AplicarA3Apaisado(sheet)
    End If

    Dim ancho As Double = sheet.Width
    Dim alto As Double = sheet.Height
    Dim hojaVertical As Boolean = alto > ancho

    BorrarSimbolosExistentes(sheet, nombreSimboloDatos)

    Dim flatOptions As NameValueMap = invApp.TransientObjects.CreateNameValueMap()
    flatOptions.Add("SheetMetalFoldedModel", False)

    ' Para las vistas plegadas no se reutiliza un NameValueMap global.
    ' Inventor crea normalmente el modelo plegado sin opciones adicionales.
    ' La funcion CrearVistaBasePlegada realiza un segundo intento con la opcion explicita.

    '---------------------------------------------------------
    ' ESCALA Y DISTRIBUCION DE VISTAS
    '---------------------------------------------------------

    Dim escalaPrincipal As Double = CalcularEscalaLongitudinalPlegado(smDef, sheet, piezaLargaA3, escalasDisponibles)
    Dim escalaDesarrollo As Double = escalaPrincipal

    Dim orientacionPrincipal As ViewOrientationTypeEnum
    Dim orientacionSecundariaNoUsada As ViewOrientationTypeEnum
    Dim orientacionExtremoNoUsada As ViewOrientationTypeEnum
    ObtenerOrientacionesPiezaLarga(partDoc, orientacionPrincipal, orientacionSecundariaNoUsada, orientacionExtremoNoUsada)

    Dim escalaTexto As String = FormatearEscala(escalaPrincipal)
    Dim centroTrabajo As String = "PLEGADO"
    Dim referenciaCliente As String = codPleg

    Dim pPrincipal As Point2d
    Dim pLateral As Point2d
    Dim pSuperior As Point2d
    Dim pIso As Point2d
    Dim pDesarrollo As Point2d
    Dim pSimbolo As Point2d

    Dim hojaApaisada As Boolean = ancho > alto

    If hojaApaisada Then

        '---------------------------------------------------------
        ' ESTRUCTURA A3 APAISADA - ESCALA MAXIMA NORMALIZADA
        '---------------------------------------------------------
        ' Todas las vistas plegadas nacen de VISTA PRINCIPAL.
        ' DESARROLLO es la unica vista base adicional por ser Flat Pattern.
        ' Todas las vistas, incluido el desarrollo, usan la misma escala.
        ' La escala se selecciona exclusivamente entre:
        ' 1:1, 1:2, 1:5, 1:10, 1:20, 1:50 y 1:100.
        ' Se mide la geometria real de las vistas y se escoge la mayor escala
        ' que permite ocupar el A3 sin invadir borde, cajetin ni otras vistas.

        Dim escalaProvisionalA3 As Double = 0.1

        '---------------------------------------------------------
        ' 1) CREAR DESARROLLO PROVISIONAL
        '---------------------------------------------------------

        pDesarrollo = tg.CreatePoint2d(ancho * 0.30, alto * 0.82)

        Dim vDesarrollo As DrawingView = sheet.DrawingViews.AddBaseView( _
            partDoc, _
            pDesarrollo, _
            escalaProvisionalA3, _
            ViewOrientationTypeEnum.kDefaultViewOrientation, _
            DrawingViewStyleEnum.kHiddenLineRemovedDrawingViewStyle, _
            "", _
            Nothing, _
            flatOptions)

        vDesarrollo.Name = "DESARROLLO"
        QuitarEtiquetaVista(vDesarrollo)
        ActivarAristasTangentes(vDesarrollo)
        GirarVistaHorizontalSiNecesario(vDesarrollo)

        '---------------------------------------------------------
        ' 2) CREAR VISTA PRINCIPAL PROVISIONAL
        '---------------------------------------------------------

        pPrincipal = tg.CreatePoint2d(ancho * 0.27, alto * 0.56)

        Dim vPrincipal As DrawingView = CrearVistaBasePlegada( _
            invApp, sheet, partDoc, pPrincipal, escalaProvisionalA3, _
            orientacionPrincipal, "VISTA PRINCIPAL")

        vPrincipal.Name = "VISTA PRINCIPAL"
        QuitarEtiquetaVista(vPrincipal)
        ActivarAristasTangentes(vPrincipal)
        GirarVistaHorizontalSiNecesario(vPrincipal)

        drawingDoc.Update2(True)

        '---------------------------------------------------------
        ' 3) CREAR TODAS LAS VISTAS DESDE LA PRINCIPAL
        '---------------------------------------------------------

        pLateral = tg.CreatePoint2d(vPrincipal.Position.X + 8.0, vPrincipal.Position.Y)

        Dim vLateral As DrawingView = CrearVistaProyectada( _
            sheet, vPrincipal, pLateral, "VISTA LATERAL")

        vLateral.Name = "VISTA LATERAL"
        QuitarEtiquetaVista(vLateral)
        ActivarAristasTangentes(vLateral)

        pSuperior = tg.CreatePoint2d(vPrincipal.Position.X, vPrincipal.Position.Y - 6.0)

        Dim vSuperior As DrawingView = CrearVistaProyectada( _
            sheet, vPrincipal, pSuperior, "VISTA SUPERIOR")

        vSuperior.Name = "VISTA SUPERIOR"
        QuitarEtiquetaVista(vSuperior)
        ActivarAristasTangentes(vSuperior)

        pIso = tg.CreatePoint2d(vPrincipal.Position.X + 7.0, vPrincipal.Position.Y - 7.0)

        Dim vIso As DrawingView = CrearVistaProyectada( _
            sheet, vPrincipal, pIso, "ISOMETRICA")

        vIso.Name = "ISOMETRICA"
        QuitarEtiquetaVista(vIso)
        ActivarAristasTangentes(vIso)

        drawingDoc.Update2(True)

        '---------------------------------------------------------
        ' 4) CALCULAR Y APLICAR LA MAYOR ESCALA NORMALIZADA
        '---------------------------------------------------------

        Dim escalaComunA3 As Double = SeleccionarEscalaYDistribuirA3( _
            tg, _
            drawingDoc, _
            sheet, _
            vDesarrollo, _
            vPrincipal, _
            vLateral, _
            vSuperior, _
            vIso, _
            escalasDisponibles, _
            pSimbolo)

        escalaPrincipal = escalaComunA3
        escalaDesarrollo = escalaComunA3
        escalaTexto = FormatearEscala(escalaComunA3)

        drawingDoc.Update2(True)

        '---------------------------------------------------------
        ' 6) ACOTAR DESPUES DE ESCALAR Y COLOCAR
        '---------------------------------------------------------

        AcotarVistaBasica(sheet, tg, vDesarrollo, True, True, 0.8)
        AcotarVistaBasica(sheet, tg, vPrincipal, True, True, 0.8)
        AcotarVistaBasica(sheet, tg, vLateral, True, True, 0.8, True)
        AcotarVistaBasica(sheet, tg, vSuperior, False, True, 0.8)

        '---------------------------------------------------------
        ' 7) SIMBOLO DATOS PLEGADO Y CODIGO DE PLEGADO
        '---------------------------------------------------------

        InsertarSimboloDatosPlegado( _
            drawingDoc, _
            sheet, _
            nombreSimboloDatos, _
            pSimbolo, _
            matriz, _
            codCorte, _
            espesorTexto)

        If codigoPlegadoManual <> "" Then
            Dim pCodigoPlegado As Point2d = _
                tg.CreatePoint2d(pSimbolo.X, pSimbolo.Y + 2.2)

            InsertarRotuloCodigoPlegado( _
                sheet, _
                tg, _
                pCodigoPlegado, _
                codigoPlegadoManual)
        End If

    Else


        '---------------------------------------------------------
        ' ESTRUCTURA A4 VERTICAL - PATRON MANUAL METALPLAK
        '---------------------------------------------------------
        '
        ' Distribucion objetivo:
        '   1. Vista principal arriba izquierda.
        '   2. Vista lateral arriba derecha.
        '   3. Vista superior en la zona central izquierda.
        '   4. Isometrica proyectada en diagonal en la zona central derecha.
        '   5. Desarrollo abajo izquierda.
        '   6. Simbolo DATOS PLEGADO a la derecha del desarrollo.
        '
        ' Se eliminan:
        '   - orientaciones automaticas que generaban vistas incorrectas;
        '   - giro automatico de vistas;
        '   - escalas demasiado pequeñas.
        '
        ' La escala se calcula despues de crear las vistas, midiendo su
        ' tamaño real y buscando la mayor escala normalizada que cabe
        ' simultaneamente en las zonas del plano.

        Dim yCajetinSupA4 As Double = ObtenerYSuperiorCajetin(sheet)

        ' Posiciones centrales del patron A4 vertical.
        Dim xColumnaIzquierda As Double = ancho * 0.30
        Dim xColumnaDerecha As Double = ancho * 0.73

        Dim yFilaSuperior As Double = alto * 0.835
        Dim yFilaCentral As Double = alto * 0.585
        Dim yFilaInferior As Double = yCajetinSupA4 + ((alto * 0.43 - yCajetinSupA4) / 2.0)

        ' Zonas disponibles para cada vista.
        Dim zonaPrincipalW As Double = ancho * 0.47
        Dim zonaPrincipalH As Double = alto * 0.235

        Dim zonaLateralW As Double = ancho * 0.23
        Dim zonaLateralH As Double = alto * 0.235

        Dim zonaSuperiorW As Double = ancho * 0.47
        Dim zonaSuperiorH As Double = alto * 0.17

        Dim zonaIsoW As Double = ancho * 0.34
        Dim zonaIsoH As Double = alto * 0.22

        Dim zonaDesarrolloW As Double = ancho * 0.49
        Dim zonaDesarrolloH As Double = Math.Max(5.5, (alto * 0.43) - yCajetinSupA4 - 0.8)

        ' En A4 vertical se fijan las orientaciones del plano manual.
        Dim orientacionPrincipalA4 As ViewOrientationTypeEnum = _
            ViewOrientationTypeEnum.kFrontViewOrientation

        Dim orientacionLateralA4 As ViewOrientationTypeEnum = _
            ViewOrientationTypeEnum.kRightViewOrientation

        Dim orientacionSuperiorA4 As ViewOrientationTypeEnum = _
            ViewOrientationTypeEnum.kTopViewOrientation

        ' Escala provisional para poder medir cada vista.
        Dim escalaProvisional As Double = 0.2

        '---------------------------------------------------------
        ' 1) VISTA PRINCIPAL - ARRIBA IZQUIERDA
        '---------------------------------------------------------

        Dim vPrincipal As DrawingView = CrearVistaBasePlegada( _
            invApp, sheet, partDoc, _
            tg.CreatePoint2d(xColumnaIzquierda, yFilaSuperior), _
            escalaProvisional, orientacionPrincipalA4, "VISTA PRINCIPAL")

        vPrincipal.Name = "VISTA PRINCIPAL"
        QuitarEtiquetaVista(vPrincipal)
        ActivarAristasTangentes(vPrincipal)

        '---------------------------------------------------------
        ' 2) VISTA LATERAL - ARRIBA DERECHA
        '---------------------------------------------------------

        Dim vLateral As DrawingView = CrearVistaProyectada( _
            sheet, vPrincipal, _
            tg.CreatePoint2d(xColumnaDerecha, yFilaSuperior), _
            "VISTA LATERAL")

        vLateral.Name = "VISTA LATERAL"
        QuitarEtiquetaVista(vLateral)
        ActivarAristasTangentes(vLateral)

        '---------------------------------------------------------
        ' 3) VISTA SUPERIOR - CENTRO IZQUIERDA
        '---------------------------------------------------------

        Dim vSuperior As DrawingView = CrearVistaProyectada( _
            sheet, vPrincipal, _
            tg.CreatePoint2d(xColumnaIzquierda, yFilaCentral), _
            "VISTA SUPERIOR")

        vSuperior.Name = "VISTA SUPERIOR"
        QuitarEtiquetaVista(vSuperior)
        ActivarAristasTangentes(vSuperior)

        '---------------------------------------------------------
        ' 4) ISOMETRICA - CENTRO DERECHA
        '---------------------------------------------------------

        ' La isometrica tambien se obtiene como proyeccion diagonal de la principal.
        Dim vIso As DrawingView = CrearVistaProyectada( _
            sheet, vPrincipal, _
            tg.CreatePoint2d(xColumnaDerecha, yFilaCentral), _
            "ISOMETRICA")

        vIso.Name = "ISOMETRICA"
        QuitarEtiquetaVista(vIso)
        ActivarAristasTangentes(vIso)

        '---------------------------------------------------------
        ' 5) DESARROLLO - ABAJO IZQUIERDA
        '---------------------------------------------------------

        Dim vDesarrollo As DrawingView = sheet.DrawingViews.AddBaseView( _
            partDoc, _
            tg.CreatePoint2d(xColumnaIzquierda, yFilaInferior), _
            escalaProvisional, _
            ViewOrientationTypeEnum.kDefaultViewOrientation, _
            DrawingViewStyleEnum.kHiddenLineRemovedDrawingViewStyle, _
            "", _
            Nothing, _
            flatOptions)

        vDesarrollo.Name = "DESARROLLO"
        QuitarEtiquetaVista(vDesarrollo)
        ActivarAristasTangentes(vDesarrollo)

        '---------------------------------------------------------
        ' CALCULO Y DISTRIBUCION A4 A ESCALA MAXIMA NORMALIZADA
        '---------------------------------------------------------
        ' Se prueba cada escala normalizada sobre las vistas reales.
        ' Se usa todo el ancho util de la hoja, sin limitar artificialmente
        ' la vista principal al 47 % del papel.

        Dim escalaComunA4 As Double = SeleccionarEscalaYDistribuirA4( _
            tg, _
            drawingDoc, _
            sheet, _
            vDesarrollo, _
            vPrincipal, _
            vLateral, _
            vSuperior, _
            vIso, _
            escalasDisponibles, _
            pSimbolo)

        escalaPrincipal = escalaComunA4
        escalaDesarrollo = escalaComunA4
        escalaTexto = FormatearEscala(escalaComunA4)

        drawingDoc.Update2(True)

        '---------------------------------------------------------
        ' ACOTACION DESPUES DE ESCALAR Y COLOCAR
        '---------------------------------------------------------

        AcotarVistaBasica(sheet, tg, vPrincipal, True, True, 0.75)
        AcotarVistaBasica(sheet, tg, vLateral, True, True, 0.75)
        AcotarVistaBasica(sheet, tg, vSuperior, False, True, 0.75)
        AcotarVistaBasica(sheet, tg, vDesarrollo, True, True, 0.75)

        '---------------------------------------------------------
        ' 6) SIMBOLO DATOS PLEGADO - JUNTO AL DESARROLLO
        '---------------------------------------------------------

        InsertarSimboloDatosPlegado( _
            drawingDoc, _
            sheet, _
            nombreSimboloDatos, _
            pSimbolo, _
            matriz, _
            codCorte, _
            espesorTexto)

        ' Rotulo COD PLEG encima del simbolo de datos, como en el plano patron.
        If codigoPlegadoManual <> "" Then
            Dim pCodigoPlegadoA4 As Point2d = _
                tg.CreatePoint2d(pSimbolo.X, pSimbolo.Y + 2.2)

            InsertarRotuloCodigoPlegado( _
                sheet, _
                tg, _
                pCodigoPlegadoA4, _
                codigoPlegadoManual)
        End If

    End If

    '---------------------------------------------------------
    ' VALIDACION FINAL DE VISTAS
    '---------------------------------------------------------
    ' Evita guardar/exportar como correcto un plano que se haya quedado solo
    ' con el desarrollo, o al que le falte alguna vista de fabricacion.
    drawingDoc.Update2(True)
    ValidarPlanoCompleto(sheet, hojaApaisada)

    '---------------------------------------------------------
    ' ESCRIBIR PROPIEDADES EN EL PLANO
    '---------------------------------------------------------

    EscribirPropiedadUsuario(drawingDoc, "COD_PLEG", codPleg)
    EscribirPropiedadUsuario(drawingDoc, "COD_PLEGADO", codigoPlegadoManual)
    EscribirPropiedadUsuario(drawingDoc, "COD_ALMACEN", codAlmacen)
    EscribirPropiedadUsuario(drawingDoc, "CORTE", codCorte)
    EscribirPropiedadUsuario(drawingDoc, "ESPESOR", espesorTexto)
    EscribirPropiedadUsuario(drawingDoc, "MATRIZ", matriz)
    EscribirPropiedadUsuario(drawingDoc, "MATERIAL", material)
    EscribirPropiedadUsuario(drawingDoc, "PROYECTO", proyecto)
    EscribirPropiedadUsuario(drawingDoc, "DISENADOR", disenador)
    EscribirPropiedadUsuario(drawingDoc, "ESTADO_DISENO", estadoDiseno)
    EscribirPropiedadUsuario(drawingDoc, "REVISADO_POR", revisadoPor)
    EscribirPropiedadUsuario(drawingDoc, "APROBADO_FABRICACION_POR", aprobadoFabPor)
    EscribirPropiedadUsuario(drawingDoc, "ESCALA", escalaTexto)
    EscribirPropiedadUsuario(drawingDoc, "ARTICULO", codPleg)
    EscribirPropiedadUsuario(drawingDoc, "ARTÍCULO", codPleg)
    EscribirPropiedadUsuario(drawingDoc, "Nº DE PIEZA", codPleg)
    EscribirPropiedadUsuario(drawingDoc, "NUMERO DE PIEZA", codPleg)
    EscribirPropiedadUsuario(drawingDoc, "NÚMERO DE PIEZA", codPleg)
    EscribirPropiedadUsuario(drawingDoc, "CENTRO", centroTrabajo)
    EscribirPropiedadUsuario(drawingDoc, "REFERENCIA CLIENTE", referenciaCliente)
    EscribirPropiedadUsuario(drawingDoc, "REFERENCIA_CLIENTE", referenciaCliente)
    EscribirPropiedadUsuario(drawingDoc, "CODIGO CLIENTE", referenciaCliente)
    EscribirPropiedadUsuario(drawingDoc, "CÓDIGO CLIENTE", referenciaCliente)
    EscribirPropiedadUsuario(drawingDoc, "COD_CLIENTE", referenciaCliente)

    EscribirPropiedad(drawingDoc, "Design Tracking Properties", "Part Number", codPleg)
    EscribirPropiedad(drawingDoc, "Design Tracking Properties", "Stock Number", codAlmacen)

    ' Rellenar tambien posibles textos solicitados/prompts del cajetin.
    ' Algunas plantillas no leen iProperties, sino entradas solicitadas del propio cajetin.
    RellenarPromptsCajetin(sheet, codPleg, centroTrabajo, escalaTexto, referenciaCliente)

    ' Repetimos propiedades estandar por compatibilidad con el cajetin.
    EscribirPropiedad(drawingDoc, "Design Tracking Properties", "Part Number", codPleg)
    EscribirPropiedad(drawingDoc, "Design Tracking Properties", "Stock Number", codAlmacen)
    EscribirPropiedad(drawingDoc, "Design Tracking Properties", "Description", descripcion)
    EscribirPropiedad(drawingDoc, "Design Tracking Properties", "Project", proyecto)
    EscribirPropiedad(drawingDoc, "Design Tracking Properties", "Designer", disenador)
    EscribirPropiedad(drawingDoc, "Inventor Summary Information", "Author", disenador)
    EscribirPropiedad(drawingDoc, "Summary Information", "Author", disenador)
    EscribirPropiedad(drawingDoc, "Inventor Summary Information", "Revision Number", revision)
    EscribirPropiedad(drawingDoc, "Summary Information", "Revision Number", revision)

    drawingDoc.Update2(True)

    '---------------------------------------------------------
    ' GUARDADO / EXPORTACION
    '---------------------------------------------------------
    ' Por defecto GUARDADO_AUTOMATICO = False.
    ' En ese modo no se guarda el IDW, no se reemplaza ningun archivo,
    ' no se crea ninguna carpeta y no se exportan PDF ni DWF.

    Dim nombreBase As String = LimpiarNombreArchivo(codPleg)
    If nombreBase.Length > 130 Then nombreBase = nombreBase.Substring(0, 130)

    Dim extensionPlano As String = System.IO.Path.GetExtension(rutaPlantilla).ToLower()
    If extensionPlano <> ".idw" And extensionPlano <> ".dwg" Then extensionPlano = ".idw"

    Dim rutaPlano As String = System.IO.Path.Combine(carpetaSalida, nombreBase & extensionPlano)
    Dim rutaPDF As String = System.IO.Path.Combine(carpetaSalida, nombreBase & ".pdf")
    Dim rutaDWF As String = System.IO.Path.Combine(carpetaSalida, nombreBase & ".dwf")

    If guardarAutomaticamente Then
        GuardarPlanoConNombreCodigo(drawingDoc, rutaPlano, nombreBase)
        ExportarPDF(invApp, drawingDoc, rutaPDF)
        ExportarDWF(invApp, drawingDoc, rutaDWF)
    End If

    ' El plano queda abierto para revision y guardado manual mediante GUARDAR COMO.
    drawingDoc.Activate()

End Sub

 '---------------------------------------------------------
' CREACION ROBUSTA DE VISTAS PLEGADAS
'---------------------------------------------------------

Sub ForzarSalidaEntornoDesarrollo(ByVal partDoc As PartDocument)

    If partDoc Is Nothing Then Exit Sub

    Try
        Dim smDef As SheetMetalComponentDefinition = _
            TryCast(partDoc.ComponentDefinition, SheetMetalComponentDefinition)

        If smDef Is Nothing Then Exit Sub

        If smDef.HasFlatPattern Then
            Try
                smDef.FlatPattern.ExitEdit()
            Catch
                ' Si el desarrollo no estaba en edicion, ExitEdit puede no hacer nada.
            End Try
        End If

        partDoc.Update2(True)

    Catch ex As Exception
        Throw New Exception( _
            "No se ha podido volver al modelo plegado antes de crear las vistas. " & _
            "Detalle: " & ex.Message)
    End Try

End Sub


Function CrearVistaBasePlegada(ByVal invApp As Inventor.Application, _
                                ByVal sheet As Sheet, _
                                ByVal partDoc As PartDocument, _
                                ByVal punto As Point2d, _
                                ByVal escala As Double, _
                                ByVal orientacion As ViewOrientationTypeEnum, _
                                ByVal nombreVista As String) As DrawingView

    If sheet Is Nothing Then
        Throw New Exception("No existe una hoja activa para crear la vista '" & nombreVista & "'.")
    End If

    If partDoc Is Nothing Then
        Throw New Exception("No existe una pieza valida para crear la vista '" & nombreVista & "'.")
    End If

    If escala <= 0 Then escala = 0.1

    ForzarSalidaEntornoDesarrollo(partDoc)

    Dim errorPrimerIntento As String = ""

    ' Primer intento: el modelo plegado es el comportamiento normal de AddBaseView.
    Try
        Return sheet.DrawingViews.AddBaseView( _
            partDoc, _
            punto, _
            escala, _
            orientacion, _
            DrawingViewStyleEnum.kHiddenLineRemovedDrawingViewStyle, _
            "", _
            Nothing, _
            Nothing)
    Catch ex As Exception
        errorPrimerIntento = ex.Message
    End Try

    ' Segundo intento: compatibilidad con versiones/plantillas que exigen
    ' declarar de forma explicita SheetMetalFoldedModel=True.
    Try
        Dim opciones As NameValueMap = _
            invApp.TransientObjects.CreateNameValueMap()

        opciones.Add("SheetMetalFoldedModel", True)

        Return sheet.DrawingViews.AddBaseView( _
            partDoc, _
            punto, _
            escala, _
            orientacion, _
            DrawingViewStyleEnum.kHiddenLineRemovedDrawingViewStyle, _
            "", _
            Nothing, _
            opciones)

    Catch ex As Exception
        Throw New Exception( _
            "No se ha podido crear la vista plegada '" & nombreVista & "'." & vbCrLf & _
            "Primer intento: " & errorPrimerIntento & vbCrLf & _
            "Segundo intento: " & ex.Message & vbCrLf & vbCrLf & _
            "Comprueba que la pieza no tenga errores de chapa, que el desarrollo " & _
            "pueda cerrarse y que el modelo plegado sea actualizable.")
    End Try

End Function


Function CrearVistaProyectada(ByVal sheet As Sheet, _
                               ByVal vistaPrincipal As DrawingView, _
                               ByVal punto As Point2d, _
                               ByVal nombreVista As String) As DrawingView

    If sheet Is Nothing Then
        Throw New Exception("No existe una hoja activa para crear la vista proyectada '" & nombreVista & "'.")
    End If

    If vistaPrincipal Is Nothing Then
        Throw New Exception("No existe la vista principal para crear la vista proyectada '" & nombreVista & "'.")
    End If

    Try
        Dim vista As DrawingView = sheet.DrawingViews.AddProjectedView( _
            vistaPrincipal, _
            punto, _
            DrawingViewStyleEnum.kHiddenLineRemovedDrawingViewStyle)

        Return vista

    Catch ex As Exception
        Throw New Exception( _
            "No se ha podido crear la vista proyectada '" & nombreVista & "' desde VISTA PRINCIPAL." & vbCrLf & _
            "Detalle: " & ex.Message & vbCrLf & vbCrLf & _
            "Comprueba que la posicion de proyeccion sea valida y que la vista principal este actualizada.")
    End Try

End Function


Sub ValidarPlanoCompleto(ByVal sheet As Sheet, _
                         ByVal hojaApaisada As Boolean)

    If sheet Is Nothing Then
        Throw New Exception("No se puede validar el plano porque no existe una hoja activa.")
    End If

    Dim obligatorias As New System.Collections.Generic.List(Of String)

    obligatorias.Add("DESARROLLO")
    obligatorias.Add("VISTA PRINCIPAL")
    obligatorias.Add("VISTA SUPERIOR")
    obligatorias.Add("ISOMETRICA")

    obligatorias.Add("VISTA LATERAL")

    Dim faltantes As New System.Collections.Generic.List(Of String)

    For Each nombre As String In obligatorias
        If Not ExisteVistaPorNombre(sheet, nombre) Then
            faltantes.Add(nombre)
        End If
    Next

    If faltantes.Count > 0 Then
        Throw New Exception( _
            "El plano ha quedado incompleto y no se guardara ni se exportara." & vbCrLf & _
            "Faltan estas vistas: " & String.Join(", ", faltantes.ToArray()) & vbCrLf & vbCrLf & _
            "La regla se ha detenido para evitar generar un IDW/PDF incorrecto.")
    End If

End Sub


Function ExisteVistaPorNombre(ByVal sheet As Sheet, _
                               ByVal nombreBuscado As String) As Boolean

    Try
        For Each v As DrawingView In sheet.DrawingViews
            Try
                If String.Equals(Trim(v.Name), Trim(nombreBuscado), _
                                 System.StringComparison.OrdinalIgnoreCase) Then
                    Return True
                End If
            Catch
            End Try
        Next
    Catch
    End Try

    Return False

End Function


 '---------------------------------------------------------
' LIMPIEZA DE VISTAS EXISTENTES
'---------------------------------------------------------

Sub BorrarTodasLasVistasExistentes(ByVal sheet As Sheet)

    Try
        If sheet Is Nothing Then Exit Sub

        ' Se borra desde el final para no alterar los indices.
        ' Al borrar una vista base, Inventor elimina también sus proyectadas.
        For i As Integer = sheet.DrawingViews.Count To 1 Step -1
            Try
                Dim vista As DrawingView = sheet.DrawingViews.Item(i)
                vista.Delete()
            Catch
            End Try
        Next

    Catch
    End Try

End Sub


'---------------------------------------------------------
' ACOTACION AUTOMATICA BASICA
'---------------------------------------------------------

Sub AcotarVistaBasica(ByVal sheet As Sheet, _
                      ByVal tg As TransientGeometry, _
                      ByVal vista As DrawingView, _
                      ByVal acotarHorizontal As Boolean, _
                      ByVal acotarVertical As Boolean, _
                      ByVal offsetCm As Double, _
                      Optional ByVal verticalDerecha As Boolean = False)
    Try
        If vista Is Nothing Then Exit Sub

        Dim curvaMinX As DrawingCurve = Nothing
        Dim curvaMaxX As DrawingCurve = Nothing
        Dim curvaMinY As DrawingCurve = Nothing
        Dim curvaMaxY As DrawingCurve = Nothing

        Dim intentoMinX As PointIntentEnum = PointIntentEnum.kStartPointIntent
        Dim intentoMaxX As PointIntentEnum = PointIntentEnum.kStartPointIntent
        Dim intentoMinY As PointIntentEnum = PointIntentEnum.kStartPointIntent
        Dim intentoMaxY As PointIntentEnum = PointIntentEnum.kStartPointIntent

        Dim minX As Double = 999999
        Dim maxX As Double = -999999
        Dim minY As Double = 999999
        Dim maxY As Double = -999999

        For Each dc As DrawingCurve In vista.DrawingCurves
            Try
                RegistrarPuntoExtremo(dc, PointIntentEnum.kStartPointIntent, dc.StartPoint, curvaMinX, intentoMinX, minX, curvaMaxX, intentoMaxX, maxX, curvaMinY, intentoMinY, minY, curvaMaxY, intentoMaxY, maxY)
            Catch
            End Try

            Try
                RegistrarPuntoExtremo(dc, PointIntentEnum.kEndPointIntent, dc.EndPoint, curvaMinX, intentoMinX, minX, curvaMaxX, intentoMaxX, maxX, curvaMinY, intentoMinY, minY, curvaMaxY, intentoMaxY, maxY)
            Catch
            End Try
        Next

        If acotarHorizontal AndAlso curvaMinX IsNot Nothing AndAlso curvaMaxX IsNot Nothing Then
            Try
                Dim intentH1 As GeometryIntent = sheet.CreateGeometryIntent(curvaMinX, intentoMinX)
                Dim intentH2 As GeometryIntent = sheet.CreateGeometryIntent(curvaMaxX, intentoMaxX)
                Dim ptTextoH As Point2d = tg.CreatePoint2d(vista.Position.X, vista.Position.Y - (vista.Height / 2.0) - offsetCm)
                sheet.DrawingDimensions.GeneralDimensions.AddLinear(ptTextoH, intentH1, intentH2, DimensionTypeEnum.kHorizontalDimensionType)
            Catch
            End Try
        End If

        If acotarVertical AndAlso curvaMinY IsNot Nothing AndAlso curvaMaxY IsNot Nothing Then
            Try
                Dim intentV1 As GeometryIntent = sheet.CreateGeometryIntent(curvaMinY, intentoMinY)
                Dim intentV2 As GeometryIntent = sheet.CreateGeometryIntent(curvaMaxY, intentoMaxY)
                Dim xTextoV As Double = vista.Position.X - (vista.Width / 2.0) - offsetCm
                If verticalDerecha Then xTextoV = vista.Position.X + (vista.Width / 2.0) + offsetCm
                Dim ptTextoV As Point2d = tg.CreatePoint2d(xTextoV, vista.Position.Y)
                sheet.DrawingDimensions.GeneralDimensions.AddLinear(ptTextoV, intentV1, intentV2, DimensionTypeEnum.kVerticalDimensionType)
            Catch
            End Try
        End If

    Catch
    End Try
End Sub

Sub RegistrarPuntoExtremo(ByVal dc As DrawingCurve, _
                          ByVal intento As PointIntentEnum, _
                          ByVal p As Point2d, _
                          ByRef curvaMinX As DrawingCurve, _
                          ByRef intentoMinX As PointIntentEnum, _
                          ByRef minX As Double, _
                          ByRef curvaMaxX As DrawingCurve, _
                          ByRef intentoMaxX As PointIntentEnum, _
                          ByRef maxX As Double, _
                          ByRef curvaMinY As DrawingCurve, _
                          ByRef intentoMinY As PointIntentEnum, _
                          ByRef minY As Double, _
                          ByRef curvaMaxY As DrawingCurve, _
                          ByRef intentoMaxY As PointIntentEnum, _
                          ByRef maxY As Double)
    Try
        If p Is Nothing Then Exit Sub

        If p.X < minX Then
            minX = p.X
            curvaMinX = dc
            intentoMinX = intento
        End If

        If p.X > maxX Then
            maxX = p.X
            curvaMaxX = dc
            intentoMaxX = intento
        End If

        If p.Y < minY Then
            minY = p.Y
            curvaMinY = dc
            intentoMinY = intento
        End If

        If p.Y > maxY Then
            maxY = p.Y
            curvaMaxY = dc
            intentoMaxY = intento
        End If
    Catch
    End Try
End Sub

'---------------------------------------------------------
' ACOTACION DE PLEGADOS A 90 GRADOS EN SECCION
' Exterior a exterior sobre vista lateral / perfil
'---------------------------------------------------------

Sub AcotarPlegados90ExteriorExteriorEnSeccion(ByVal sheet As Sheet, _
                                             ByVal tg As TransientGeometry, _
                                             ByVal vista As DrawingView, _
                                             ByVal offsetInicialCm As Double, _
                                             ByVal saltoOffsetCm As Double, _
                                             ByVal toleranciaAngularGrados As Double, _
                                             ByVal longitudMinimaLineaCm As Double, _
                                             ByVal separacionMinimaCotaCm As Double, _
                                             ByVal maximoCotas As Integer)
    Try
        If vista Is Nothing Then Exit Sub

        Dim estacionesX As New System.Collections.Generic.List(Of Double)
        Dim curvasX As New System.Collections.Generic.List(Of DrawingCurve)
        Dim intentosX As New System.Collections.Generic.List(Of PointIntentEnum)
        Dim puntosX As New System.Collections.Generic.List(Of Point2d)

        Dim estacionesY As New System.Collections.Generic.List(Of Double)
        Dim curvasY As New System.Collections.Generic.List(Of DrawingCurve)
        Dim intentosY As New System.Collections.Generic.List(Of PointIntentEnum)
        Dim puntosY As New System.Collections.Generic.List(Of Point2d)

        Dim toleranciaAgrupar As Double = 0.08

        ' 1) Detectar lineas horizontales y verticales del perfil.
        '    Las verticales generan estaciones X.
        '    Las horizontales generan estaciones Y.
        For Each dc As DrawingCurve In vista.DrawingCurves
            Try
                If Not EsLineaRectaVista(dc) Then Continue For

                Dim p1 As Point2d = dc.StartPoint
                Dim p2 As Point2d = dc.EndPoint
                If p1 Is Nothing Or p2 Is Nothing Then Continue For

                Dim longitud As Double = Distancia2DLocal(p1, p2)
                If longitud < longitudMinimaLineaCm Then Continue For

                Dim ang As Double = AnguloLineaVistaGrados(p1, p2)

                If EsLineaVerticalVista(ang, toleranciaAngularGrados) Then
                    Dim xMedio As Double = (p1.X + p2.X) / 2.0
                    AgregarEstacionCota(estacionesX, curvasX, intentosX, puntosX, xMedio, dc, PointIntentEnum.kStartPointIntent, p1, toleranciaAgrupar)
                ElseIf EsLineaHorizontalVista(ang, toleranciaAngularGrados) Then
                    Dim yMedio As Double = (p1.Y + p2.Y) / 2.0
                    AgregarEstacionCota(estacionesY, curvasY, intentosY, puntosY, yMedio, dc, PointIntentEnum.kStartPointIntent, p1, toleranciaAgrupar)
                End If
            Catch
            End Try
        Next

        OrdenarEstacionesCota(estacionesX, curvasX, intentosX, puntosX)
        OrdenarEstacionesCota(estacionesY, curvasY, intentosY, puntosY)

        Dim cotasCreadas As Integer = 0
        Dim offsetActual As Double = offsetInicialCm

        ' 2) Cotas horizontales exterior-exterior entre estaciones X consecutivas.
        '    Colocadas por encima de la vista.
        For i As Integer = 0 To estacionesX.Count - 2
            If cotasCreadas >= maximoCotas Then Exit For

            Try
                Dim distancia As Double = Math.Abs(estacionesX(i + 1) - estacionesX(i))
                If distancia < separacionMinimaCotaCm Then Continue For

                Dim gi1 As GeometryIntent = sheet.CreateGeometryIntent(curvasX(i), puntosX(i))
                Dim gi2 As GeometryIntent = sheet.CreateGeometryIntent(curvasX(i + 1), puntosX(i + 1))

                Dim xTexto As Double = (estacionesX(i) + estacionesX(i + 1)) / 2.0
                Dim yTexto As Double = vista.Position.Y + (vista.Height / 2.0) + offsetActual
                Dim ptTexto As Point2d = tg.CreatePoint2d(xTexto, yTexto)

                sheet.DrawingDimensions.GeneralDimensions.AddLinear(ptTexto, gi1, gi2, DimensionTypeEnum.kHorizontalDimensionType)

                cotasCreadas += 1
                offsetActual += saltoOffsetCm
            Catch
            End Try
        Next

        ' 3) Cotas verticales exterior-exterior entre estaciones Y consecutivas.
        '    Colocadas a la derecha de la vista, como en el plano patron.
        offsetActual = offsetInicialCm

        For i As Integer = 0 To estacionesY.Count - 2
            If cotasCreadas >= maximoCotas Then Exit For

            Try
                Dim distancia As Double = Math.Abs(estacionesY(i + 1) - estacionesY(i))
                If distancia < separacionMinimaCotaCm Then Continue For

                Dim gi1 As GeometryIntent = sheet.CreateGeometryIntent(curvasY(i), puntosY(i))
                Dim gi2 As GeometryIntent = sheet.CreateGeometryIntent(curvasY(i + 1), puntosY(i + 1))

                Dim xTexto As Double = vista.Position.X + (vista.Width / 2.0) + offsetActual
                Dim yTexto As Double = (estacionesY(i) + estacionesY(i + 1)) / 2.0
                Dim ptTexto As Point2d = tg.CreatePoint2d(xTexto, yTexto)

                sheet.DrawingDimensions.GeneralDimensions.AddLinear(ptTexto, gi1, gi2, DimensionTypeEnum.kVerticalDimensionType)

                cotasCreadas += 1
                offsetActual += saltoOffsetCm
            Catch
            End Try
        Next

    Catch
        ' No bloqueamos el plano si falla la acotacion de seccion.
    End Try
End Sub

Function EsLineaRectaVista(ByVal dc As DrawingCurve) As Boolean
    Try
        If dc Is Nothing Then Return False
        If dc.StartPoint Is Nothing Then Return False
        If dc.EndPoint Is Nothing Then Return False

        ' Si tiene centro, normalmente es arco/circulo. Lo descartamos para no acotar radios como rectas.
        Try
            Dim c As Point2d = dc.CenterPoint
            If c IsNot Nothing Then Return False
        Catch
        End Try

        Return True
    Catch
        Return False
    End Try
End Function

Function AnguloLineaVistaGrados(ByVal p1 As Point2d, ByVal p2 As Point2d) As Double
    Try
        Dim a As Double = Math.Atan2(p2.Y - p1.Y, p2.X - p1.X) * 180.0 / Math.PI
        If a < 0 Then a += 180.0
        If a >= 180.0 Then a -= 180.0
        Return a
    Catch
        Return 0
    End Try
End Function

Function EsLineaHorizontalVista(ByVal ang As Double, ByVal tol As Double) As Boolean
    Try
        If Math.Abs(ang - 0) <= tol Then Return True
        If Math.Abs(ang - 180) <= tol Then Return True
        Return False
    Catch
        Return False
    End Try
End Function

Function EsLineaVerticalVista(ByVal ang As Double, ByVal tol As Double) As Boolean
    Try
        If Math.Abs(ang - 90) <= tol Then Return True
        Return False
    Catch
        Return False
    End Try
End Function

Function Distancia2DLocal(ByVal p1 As Point2d, ByVal p2 As Point2d) As Double
    Try
        Return Math.Sqrt(((p2.X - p1.X) ^ 2) + ((p2.Y - p1.Y) ^ 2))
    Catch
        Return 0
    End Try
End Function

Sub AgregarEstacionCota(ByVal estaciones As System.Collections.Generic.List(Of Double), _
                        ByVal curvas As System.Collections.Generic.List(Of DrawingCurve), _
                        ByVal intentos As System.Collections.Generic.List(Of PointIntentEnum), _
                        ByVal puntos As System.Collections.Generic.List(Of Point2d), _
                        ByVal valor As Double, _
                        ByVal curva As DrawingCurve, _
                        ByVal intento As PointIntentEnum, _
                        ByVal punto As Point2d, _
                        ByVal tolerancia As Double)
    Try
        For i As Integer = 0 To estaciones.Count - 1
            If Math.Abs(estaciones(i) - valor) <= tolerancia Then
                Exit Sub
            End If
        Next

        estaciones.Add(valor)
        curvas.Add(curva)
        intentos.Add(intento)
        puntos.Add(punto)
    Catch
    End Try
End Sub

Sub OrdenarEstacionesCota(ByVal estaciones As System.Collections.Generic.List(Of Double), _
                          ByVal curvas As System.Collections.Generic.List(Of DrawingCurve), _
                          ByVal intentos As System.Collections.Generic.List(Of PointIntentEnum), _
                          ByVal puntos As System.Collections.Generic.List(Of Point2d))
    Try
        If estaciones.Count < 2 Then Exit Sub

        For i As Integer = 0 To estaciones.Count - 2
            For j As Integer = i + 1 To estaciones.Count - 1
                If estaciones(j) < estaciones(i) Then
                    Dim tempV As Double = estaciones(i)
                    estaciones(i) = estaciones(j)
                    estaciones(j) = tempV

                    Dim tempC As DrawingCurve = curvas(i)
                    curvas(i) = curvas(j)
                    curvas(j) = tempC

                    Dim tempI As PointIntentEnum = intentos(i)
                    intentos(i) = intentos(j)
                    intentos(j) = tempI

                    Dim tempP As Point2d = puntos(i)
                    puntos(i) = puntos(j)
                    puntos(j) = tempP
                End If
            Next
        Next
    Catch
    End Try
End Sub

 '---------------------------------------------------------
' CODIGO DE PLEGADO MANUAL
'---------------------------------------------------------

Function PedirCodigoPlegado(ByVal partDoc As PartDocument) As String

    Dim valorAnterior As String = _
        LeerPropiedadUsuario(partDoc, "COD_PLEGADO")

    Dim valor As String = ""

    Do
        valor = Microsoft.VisualBasic.Interaction.InputBox( _
            "Introduce el codigo de plegado que debe aparecer en el plano." & vbCrLf & vbCrLf & _
            "Se mostrara con el formato:" & vbCrLf & _
            "COD PLEG" & vbCrLf & _
            "0000", _
            "Codigo de plegado", _
            valorAnterior)

        valor = Trim(valor)

        If valor <> "" Then
            Return valor
        End If

        Dim respuesta As System.Windows.Forms.DialogResult = _
            System.Windows.Forms.MessageBox.Show( _
                "No has introducido ningun codigo de plegado." & vbCrLf & vbCrLf & _
                "¿Quieres continuar y generar el plano sin el rotulo COD PLEG?", _
                "Codigo de plegado", _
                System.Windows.Forms.MessageBoxButtons.YesNo, _
                System.Windows.Forms.MessageBoxIcon.Question)

        If respuesta = System.Windows.Forms.DialogResult.Yes Then
            Return ""
        End If

    Loop

End Function


Sub InsertarRotuloCodigoPlegado(ByVal sheet As Sheet, _
                                ByVal tg As TransientGeometry, _
                                ByVal punto As Point2d, _
                                ByVal codigo As String)

    ' v10.21
    ' Esta rutina no debe detener nunca la generacion del plano.
    ' En algunas versiones de Inventor, AddFitted devuelve E_INVALIDARG cuando:
    '   - el punto queda demasiado cerca o fuera del borde de la hoja;
    '   - se usa texto XML/FormattedText que esa version no interpreta correctamente.
    ' Por eso se usa texto simple, se limita el punto a una zona segura de la hoja
    ' y se dispone de un segundo intento con dos notas independientes.

    Try
        If sheet Is Nothing Then Exit Sub
        If tg Is Nothing Then Exit Sub
        If punto Is Nothing Then Exit Sub
        If codigo Is Nothing OrElse Trim(codigo) = "" Then Exit Sub

        Dim codigoLimpio As String = Trim(codigo)

        ' Zona segura para que la nota no quede fuera del formato.
        ' Las unidades del plano son centimetros.
        Dim margenLateral As Double = 1.0
        Dim margenInferior As Double = 1.0
        Dim margenSuperior As Double = 2.2

        Dim xSeguro As Double = punto.X
        Dim ySeguro As Double = punto.Y

        If xSeguro < margenLateral Then xSeguro = margenLateral
        If xSeguro > sheet.Width - margenLateral Then _
            xSeguro = sheet.Width - margenLateral

        If ySeguro < margenInferior Then ySeguro = margenInferior
        If ySeguro > sheet.Height - margenSuperior Then _
            ySeguro = sheet.Height - margenSuperior

        Dim puntoSeguro As Point2d = tg.CreatePoint2d(xSeguro, ySeguro)

        ' Primer intento: una sola nota de texto simple.
        ' Se evita deliberadamente StyleOverride y <Br/>, origen habitual
        ' del error 0x80070057 en plantillas IDW antiguas.
        Try
            Dim textoPlano As String = _
                "COD PLEG" & vbCrLf & codigoLimpio

            Dim nota As GeneralNote = _
                sheet.DrawingNotes.GeneralNotes.AddFitted( _
                    puntoSeguro, _
                    textoPlano)

            Try
                nota.HorizontalJustification = _
                    HorizontalTextAlignmentEnum.kAlignTextCenter
            Catch
            End Try

            Exit Sub

        Catch
            ' Se realiza un segundo intento mas basico.
        End Try

        ' Segundo intento: dos notas simples independientes.
        ' Esta alternativa evita que un salto de linea incompatible bloquee AddFitted.
        Try
            Dim yTitulo As Double = ySeguro + 0.35
            Dim yCodigo As Double = ySeguro - 0.35

            If yTitulo > sheet.Height - 1.0 Then _
                yTitulo = sheet.Height - 1.0

            If yCodigo < margenInferior Then _
                yCodigo = margenInferior

            Dim notaTitulo As GeneralNote = _
                sheet.DrawingNotes.GeneralNotes.AddFitted( _
                    tg.CreatePoint2d(xSeguro, yTitulo), _
                    "COD PLEG")

            Dim notaCodigo As GeneralNote = _
                sheet.DrawingNotes.GeneralNotes.AddFitted( _
                    tg.CreatePoint2d(xSeguro, yCodigo), _
                    codigoLimpio)

            Try
                notaTitulo.HorizontalJustification = _
                    HorizontalTextAlignmentEnum.kAlignTextCenter
            Catch
            End Try

            Try
                notaCodigo.HorizontalJustification = _
                    HorizontalTextAlignmentEnum.kAlignTextCenter
            Catch
            End Try

        Catch
            ' El rotulo es informativo. Si la plantilla concreta no admite notas,
            ' se continua con las vistas, propiedades, guardado y exportacion.
        End Try

    Catch
        ' Nunca bloquear el plano por el rotulo COD PLEG.
    End Try

End Sub


Function EscaparTextoInventor(ByVal texto As String) As String

    If texto Is Nothing Then Return ""

    texto = texto.Replace("&", "&amp;")
    texto = texto.Replace("<", "&lt;")
    texto = texto.Replace(">", "&gt;")
    texto = texto.Replace("""", "&quot;")
    texto = texto.Replace("'", "&apos;")

    Return texto

End Function


'---------------------------------------------------------
' SIMBOLO DE BOCETO DATOS PLEGADO
'---------------------------------------------------------

Sub InsertarSimboloDatosPlegado(ByVal drawingDoc As DrawingDocument, _
                                ByVal sheet As Sheet, _
                                ByVal nombreSimbolo As String, _
                                ByVal punto As Point2d, _
                                ByVal matriz As String, _
                                ByVal codCorte As String, _
                                ByVal espesorTexto As String)
    Try
        Dim def As SketchedSymbolDefinition = drawingDoc.SketchedSymbolDefinitions.Item(nombreSimbolo)

        Dim prompts(2) As String
        prompts(0) = matriz
        prompts(1) = codCorte
        prompts(2) = espesorTexto

        Try
            sheet.SketchedSymbols.Add(def, punto, 0, 1, prompts)
        Catch
            sheet.SketchedSymbols.Add(def, punto, 0, 1)
        End Try
    Catch ex As Exception
        Throw New Exception("No se ha encontrado o no se ha podido insertar el simbolo de boceto '" & nombreSimbolo & "'. Detalle: " & ex.Message)
    End Try
End Sub

Sub BorrarSimbolosExistentes(ByVal sheet As Sheet, ByVal nombreSimbolo As String)
    Try
        For i As Integer = sheet.SketchedSymbols.Count To 1 Step -1
            Dim s As SketchedSymbol = sheet.SketchedSymbols.Item(i)
            Try
                If s.Definition.Name.ToUpper() = nombreSimbolo.ToUpper() Then
                    s.Delete()
                End If
            Catch
            End Try
        Next
    Catch
    End Try
End Sub

'---------------------------------------------------------
' RELLENO DE CAJETIN
'---------------------------------------------------------

Sub RellenarPromptsCajetin(ByVal sheet As Sheet, _
                           ByVal articulo As String, _
                           ByVal centro As String, _
                           ByVal escala As String, _
                           ByVal referenciaCliente As String)
    Try
        If sheet Is Nothing Then Exit Sub
        If sheet.TitleBlock Is Nothing Then Exit Sub

        Dim tb As TitleBlock = sheet.TitleBlock
        Dim def As TitleBlockDefinition = tb.Definition
        If def Is Nothing Then Exit Sub

        For Each txt As Inventor.TextBox In def.Sketch.TextBoxes
            Try
                Dim t As String = ""

                Try
                    t = txt.Text
                Catch
                    t = ""
                End Try

                If t = "" Then
                    Try
                        t = txt.FormattedText
                    Catch
                        t = ""
                    End Try
                End If

                Dim tu As String = t.ToUpper()

                If tu.Contains("ARTICULO") Or _
                   tu.Contains("ARTÍCULO") Or _
                   tu.Contains("Nº DE PIEZA") Or _
                   tu.Contains("N DE PIEZA") Or _
                   tu.Contains("NUMERO DE PIEZA") Or _
                   tu.Contains("NÚMERO DE PIEZA") Then
                    Try
                        tb.SetPromptResultText(txt, articulo)
                    Catch
                    End Try
                End If

                If tu.Contains("CENTRO") Then
                    Try
                        tb.SetPromptResultText(txt, centro)
                    Catch
                    End Try
                End If

                If tu.Contains("ESCALA") Then
                    Try
                        tb.SetPromptResultText(txt, escala)
                    Catch
                    End Try
                End If

                If tu.Contains("REFERENCIA CLIENTE") Or _
                   tu.Contains("CODIGO CLIENTE") Or _
                   tu.Contains("CÓDIGO CLIENTE") Or _
                   tu.Contains("COD_CLIENTE") Or _
                   tu.Contains("COD. CLIENTE") Or _
                   tu.Contains("<CODIGO CLIENTE>") Or _
                   tu.Contains("<CÓDIGO CLIENTE>") Then
                    Try
                        tb.SetPromptResultText(txt, referenciaCliente)
                    Catch
                    End Try
                End If

            Catch
            End Try
        Next
    Catch
        ' No bloqueamos el plano si el cajetin no usa prompts.
    End Try
End Sub

'---------------------------------------------------------
' VISUALIZACION DE VISTAS
'---------------------------------------------------------

Sub ActivarAristasTangentes(ByVal v As DrawingView)
    Try
        v.DisplayTangentEdges = True
    Catch
        ' Si alguna vista no admite esta propiedad, no bloqueamos la regla.
    End Try
End Sub

'---------------------------------------------------------
' ETIQUETAS
'---------------------------------------------------------

Sub QuitarEtiquetaVista(ByVal v As DrawingView)
    Try
        v.ShowLabel = False
    Catch
    End Try
End Sub

Sub MostrarEtiquetaVista(ByVal v As DrawingView)
    Try
        v.ShowLabel = True
    Catch
    End Try
End Sub

Sub GirarVistaHorizontalSiNecesario(ByVal v As DrawingView)
    Try
        If v.Height > v.Width * 1.15 Then
            v.Rotation = v.Rotation + (Math.PI / 2.0)
        End If
    Catch
    End Try
End Sub

 '---------------------------------------------------------
' ESCALA Y DISTRIBUCION MAXIMA A3 / A4
'---------------------------------------------------------

Function SeleccionarEscalaYDistribuirA3( _
    ByVal tg As TransientGeometry, _
    ByVal drawingDoc As DrawingDocument, _
    ByVal sheet As Sheet, _
    ByVal vDesarrollo As DrawingView, _
    ByVal vPrincipal As DrawingView, _
    ByVal vLateral As DrawingView, _
    ByVal vSuperior As DrawingView, _
    ByVal vIso As DrawingView, _
    ByVal escalasDisponibles() As Double, _
    ByRef puntoSimbolo As Point2d) As Double

    Dim escalaElegida As Double = escalasDisponibles(escalasDisponibles.Length - 1)

    For Each s As Double In escalasDisponibles
        If s <= 0 Then Continue For

        Try
            vPrincipal.Scale = s
            vDesarrollo.Scale = s
            drawingDoc.Update2(True)

            If CabeDistribucionA3(sheet, vDesarrollo, vPrincipal, vLateral, vSuperior, vIso) Then
                escalaElegida = s
                Exit For
            End If
        Catch
        End Try
    Next

    vPrincipal.Scale = escalaElegida
    vDesarrollo.Scale = escalaElegida
    drawingDoc.Update2(True)

    DistribuirVistasA3AprovechamientoReal( _
        tg, sheet, vDesarrollo, vPrincipal, vLateral, vSuperior, vIso, puntoSimbolo)

    Return escalaElegida

End Function


Function CabeDistribucionA3( _
    ByVal sheet As Sheet, _
    ByVal vDesarrollo As DrawingView, _
    ByVal vPrincipal As DrawingView, _
    ByVal vLateral As DrawingView, _
    ByVal vSuperior As DrawingView, _
    ByVal vIso As DrawingView) As Boolean

    Try
        Dim margenIzq As Double = 1.0
        Dim margenDer As Double = 1.0
        Dim margenSup As Double = 0.7
        Dim margenSobreCajetin As Double = 0.7

        Dim separacionH As Double = 0.7
        Dim separacionV As Double = 0.45
        Dim reservaCotaX As Double = 0.65
        Dim reservaCotaY As Double = 0.8
        Dim anchoSimbolo As Double = 5.2
        Dim altoSimbolo As Double = 3.6

        Dim yInferior As Double = ObtenerYSuperiorCajetin(sheet) + margenSobreCajetin
        Dim ySuperior As Double = sheet.Height - margenSup

        Dim anchoUtil As Double = sheet.Width - margenIzq - margenDer
        Dim altoUtil As Double = ySuperior - yInferior

        If anchoUtil <= 0 OrElse altoUtil <= 0 Then Return False

        ' Fila 1: desarrollo a la izquierda y símbolo a la derecha.
        Dim anchoFila1 As Double = _
            reservaCotaX + vDesarrollo.Width + separacionH + anchoSimbolo
        Dim altoFila1 As Double = _
            Math.Max(vDesarrollo.Height + reservaCotaY, altoSimbolo)

        ' Fila 2: principal y lateral proyectada.
        Dim anchoFila2 As Double = _
            reservaCotaX + vPrincipal.Width + separacionH + _
            vLateral.Width + reservaCotaX
        Dim altoFila2 As Double = _
            Math.Max(vPrincipal.Height + reservaCotaY, _
                     vLateral.Height + reservaCotaY)

        ' Fila 3: superior proyectada. Usa todo el ancho disponible.
        Dim anchoFila3 As Double = reservaCotaX + vSuperior.Width
        Dim altoFila3 As Double = vSuperior.Height + reservaCotaY

        ' Fila 4: isométrica. No se obliga a compartir fila con otra vista larga.
        Dim anchoFila4 As Double = vIso.Width + 0.3
        Dim altoFila4 As Double = vIso.Height + 0.35

        Dim altoTotal As Double = _
            altoFila1 + altoFila2 + altoFila3 + altoFila4 + _
            (3.0 * separacionV)

        Dim anchoMaximo As Double = Math.Max( _
            Math.Max(anchoFila1, anchoFila2), _
            Math.Max(anchoFila3, anchoFila4))

        Return (anchoMaximo <= anchoUtil) AndAlso (altoTotal <= altoUtil)

    Catch
        Return False
    End Try

End Function


Sub DistribuirVistasA3AprovechamientoReal( _
    ByVal tg As TransientGeometry, _
    ByVal sheet As Sheet, _
    ByVal vDesarrollo As DrawingView, _
    ByVal vPrincipal As DrawingView, _
    ByVal vLateral As DrawingView, _
    ByVal vSuperior As DrawingView, _
    ByVal vIso As DrawingView, _
    ByRef puntoSimbolo As Point2d)

    Dim margenIzq As Double = 1.0
    Dim margenDer As Double = 1.0
    Dim margenSup As Double = 0.7
    Dim margenSobreCajetin As Double = 0.7

    Dim separacionH As Double = 0.7
    Dim separacionVBase As Double = 0.45
    Dim reservaCotaX As Double = 0.65
    Dim reservaCotaY As Double = 0.8
    Dim anchoSimbolo As Double = 5.2
    Dim altoSimbolo As Double = 3.6

    Dim yInferior As Double = ObtenerYSuperiorCajetin(sheet) + margenSobreCajetin
    Dim ySuperior As Double = sheet.Height - margenSup
    Dim altoUtil As Double = ySuperior - yInferior

    Dim altoFila1 As Double = _
        Math.Max(vDesarrollo.Height + reservaCotaY, altoSimbolo)
    Dim altoFila2 As Double = _
        Math.Max(vPrincipal.Height + reservaCotaY, _
                 vLateral.Height + reservaCotaY)
    Dim altoFila3 As Double = vSuperior.Height + reservaCotaY
    Dim altoFila4 As Double = vIso.Height + 0.35

    Dim altoBase As Double = _
        altoFila1 + altoFila2 + altoFila3 + altoFila4 + _
        (3.0 * separacionVBase)

    Dim sobrante As Double = Math.Max(0, altoUtil - altoBase)
    Dim separacionV As Double = separacionVBase + Math.Min(1.0, sobrante / 3.0)

    Dim yCursor As Double = ySuperior

    ' FILA 1: DESARROLLO + DATOS PLEGADO
    Dim yFila1 As Double = yCursor - (altoFila1 / 2.0)
    Dim xDesarrollo As Double = _
        margenIzq + reservaCotaX + (vDesarrollo.Width / 2.0)

    vDesarrollo.Position = tg.CreatePoint2d(xDesarrollo, yFila1)

    puntoSimbolo = tg.CreatePoint2d( _
        sheet.Width - margenDer - (anchoSimbolo / 2.0), _
        yCursor - 1.0)

    yCursor = yCursor - altoFila1 - separacionV

    ' FILA 2: PRINCIPAL + LATERAL PROYECTADA
    Dim yFila2 As Double = yCursor - (altoFila2 / 2.0)
    Dim xPrincipal As Double = _
        margenIzq + reservaCotaX + (vPrincipal.Width / 2.0)
    Dim xLateral As Double = _
        sheet.Width - margenDer - reservaCotaX - (vLateral.Width / 2.0)

    vPrincipal.Position = tg.CreatePoint2d(xPrincipal, yFila2)
    vLateral.Position = tg.CreatePoint2d(xLateral, yFila2)

    yCursor = yCursor - altoFila2 - separacionV

    ' FILA 3: SUPERIOR, ALINEADA CON LA PRINCIPAL
    Dim yFila3 As Double = yCursor - (altoFila3 / 2.0)
    vSuperior.Position = tg.CreatePoint2d(xPrincipal, yFila3)

    yCursor = yCursor - altoFila3 - separacionV

    ' FILA 4: ISOMETRICA, APROVECHANDO LA ZONA INFERIOR DERECHA
    Dim yFila4 As Double = yCursor - (altoFila4 / 2.0)
    Dim xIso As Double = _
        sheet.Width - margenDer - (vIso.Width / 2.0)

    vIso.Position = tg.CreatePoint2d(xIso, yFila4)

End Sub


Function SeleccionarEscalaYDistribuirA4( _
    ByVal tg As TransientGeometry, _
    ByVal drawingDoc As DrawingDocument, _
    ByVal sheet As Sheet, _
    ByVal vDesarrollo As DrawingView, _
    ByVal vPrincipal As DrawingView, _
    ByVal vLateral As DrawingView, _
    ByVal vSuperior As DrawingView, _
    ByVal vIso As DrawingView, _
    ByVal escalasDisponibles() As Double, _
    ByRef puntoSimbolo As Point2d) As Double

    Dim escalaElegida As Double = escalasDisponibles(escalasDisponibles.Length - 1)

    For Each s As Double In escalasDisponibles
        If s <= 0 Then Continue For

        Try
            vPrincipal.Scale = s
            vDesarrollo.Scale = s
            drawingDoc.Update2(True)

            If CabeDistribucionA4(sheet, vDesarrollo, vPrincipal, vLateral, vSuperior, vIso) Then
                escalaElegida = s
                Exit For
            End If
        Catch
        End Try
    Next

    vPrincipal.Scale = escalaElegida
    vDesarrollo.Scale = escalaElegida
    drawingDoc.Update2(True)

    DistribuirVistasA4AprovechamientoReal( _
        tg, sheet, vDesarrollo, vPrincipal, vLateral, vSuperior, vIso, puntoSimbolo)

    Return escalaElegida

End Function


Function CabeDistribucionA4( _
    ByVal sheet As Sheet, _
    ByVal vDesarrollo As DrawingView, _
    ByVal vPrincipal As DrawingView, _
    ByVal vLateral As DrawingView, _
    ByVal vSuperior As DrawingView, _
    ByVal vIso As DrawingView) As Boolean

    Try
        Dim margenIzq As Double = 0.8
        Dim margenDer As Double = 0.8
        Dim margenSup As Double = 0.65
        Dim margenSobreCajetin As Double = 0.55

        Dim separacionH As Double = 0.55
        Dim separacionV As Double = 0.35
        Dim reservaCotaX As Double = 0.55
        Dim reservaCotaY As Double = 0.7
        Dim anchoSimbolo As Double = 5.0
        Dim altoSimbolo As Double = 3.5

        Dim yInferior As Double = ObtenerYSuperiorCajetin(sheet) + margenSobreCajetin
        Dim ySuperior As Double = sheet.Height - margenSup

        Dim anchoUtil As Double = sheet.Width - margenIzq - margenDer
        Dim altoUtil As Double = ySuperior - yInferior

        If anchoUtil <= 0 OrElse altoUtil <= 0 Then Return False

        ' Fila 1: principal + lateral.
        Dim anchoFila1 As Double = _
            reservaCotaX + vPrincipal.Width + separacionH + _
            vLateral.Width + reservaCotaX
        Dim altoFila1 As Double = _
            Math.Max(vPrincipal.Height + reservaCotaY, _
                     vLateral.Height + reservaCotaY)

        ' Fila 2: superior.
        Dim anchoFila2 As Double = reservaCotaX + vSuperior.Width
        Dim altoFila2 As Double = vSuperior.Height + reservaCotaY

        ' Fila 3: isométrica.
        Dim anchoFila3 As Double = vIso.Width + 0.25
        Dim altoFila3 As Double = vIso.Height + 0.3

        ' Fila 4: desarrollo + símbolo.
        Dim anchoFila4 As Double = _
            reservaCotaX + vDesarrollo.Width + separacionH + anchoSimbolo
        Dim altoFila4 As Double = _
            Math.Max(vDesarrollo.Height + reservaCotaY, altoSimbolo)

        Dim altoTotal As Double = _
            altoFila1 + altoFila2 + altoFila3 + altoFila4 + _
            (3.0 * separacionV)

        Dim anchoMaximo As Double = Math.Max( _
            Math.Max(anchoFila1, anchoFila2), _
            Math.Max(anchoFila3, anchoFila4))

        Return (anchoMaximo <= anchoUtil) AndAlso (altoTotal <= altoUtil)

    Catch
        Return False
    End Try

End Function


Sub DistribuirVistasA4AprovechamientoReal( _
    ByVal tg As TransientGeometry, _
    ByVal sheet As Sheet, _
    ByVal vDesarrollo As DrawingView, _
    ByVal vPrincipal As DrawingView, _
    ByVal vLateral As DrawingView, _
    ByVal vSuperior As DrawingView, _
    ByVal vIso As DrawingView, _
    ByRef puntoSimbolo As Point2d)

    Dim margenIzq As Double = 0.8
    Dim margenDer As Double = 0.8
    Dim margenSup As Double = 0.65
    Dim margenSobreCajetin As Double = 0.55

    Dim separacionVBase As Double = 0.35
    Dim reservaCotaX As Double = 0.55
    Dim reservaCotaY As Double = 0.7
    Dim anchoSimbolo As Double = 5.0
    Dim altoSimbolo As Double = 3.5

    Dim yInferior As Double = ObtenerYSuperiorCajetin(sheet) + margenSobreCajetin
    Dim ySuperior As Double = sheet.Height - margenSup
    Dim altoUtil As Double = ySuperior - yInferior

    Dim altoFila1 As Double = _
        Math.Max(vPrincipal.Height + reservaCotaY, _
                 vLateral.Height + reservaCotaY)
    Dim altoFila2 As Double = vSuperior.Height + reservaCotaY
    Dim altoFila3 As Double = vIso.Height + 0.3
    Dim altoFila4 As Double = _
        Math.Max(vDesarrollo.Height + reservaCotaY, altoSimbolo)

    Dim altoBase As Double = _
        altoFila1 + altoFila2 + altoFila3 + altoFila4 + _
        (3.0 * separacionVBase)

    Dim sobrante As Double = Math.Max(0, altoUtil - altoBase)
    Dim separacionV As Double = separacionVBase + Math.Min(0.8, sobrante / 3.0)

    Dim yCursor As Double = ySuperior

    ' FILA 1: PRINCIPAL + LATERAL
    Dim yFila1 As Double = yCursor - (altoFila1 / 2.0)
    Dim xPrincipal As Double = _
        margenIzq + reservaCotaX + (vPrincipal.Width / 2.0)
    Dim xLateral As Double = _
        sheet.Width - margenDer - reservaCotaX - (vLateral.Width / 2.0)

    vPrincipal.Position = tg.CreatePoint2d(xPrincipal, yFila1)
    vLateral.Position = tg.CreatePoint2d(xLateral, yFila1)

    yCursor = yCursor - altoFila1 - separacionV

    ' FILA 2: SUPERIOR ALINEADA CON PRINCIPAL
    Dim yFila2 As Double = yCursor - (altoFila2 / 2.0)
    vSuperior.Position = tg.CreatePoint2d(xPrincipal, yFila2)

    yCursor = yCursor - altoFila2 - separacionV

    ' FILA 3: ISOMETRICA A LA DERECHA
    Dim yFila3 As Double = yCursor - (altoFila3 / 2.0)
    Dim xIso As Double = _
        sheet.Width - margenDer - (vIso.Width / 2.0)
    vIso.Position = tg.CreatePoint2d(xIso, yFila3)

    yCursor = yCursor - altoFila3 - separacionV

    ' FILA 4: DESARROLLO + DATOS PLEGADO
    Dim yFila4 As Double = yCursor - (altoFila4 / 2.0)
    Dim xDesarrollo As Double = _
        margenIzq + reservaCotaX + (vDesarrollo.Width / 2.0)

    vDesarrollo.Position = tg.CreatePoint2d(xDesarrollo, yFila4)

    puntoSimbolo = tg.CreatePoint2d( _
        sheet.Width - margenDer - (anchoSimbolo / 2.0), _
        yCursor - 0.9)

End Sub


 '---------------------------------------------------------
' ESCALA REAL BASADA EN VISTAS YA CREADAS
'---------------------------------------------------------

Function CalcularEscalaMaximaVistaCreada( _
    ByVal vista As DrawingView, _
    ByVal anchoZona As Double, _
    ByVal altoZona As Double, _
    ByVal escalasDisponibles() As Double) As Double

    Try
        If vista Is Nothing Then Return 0.1
        If vista.Scale <= 0 Then Return 0.1

        ' Recuperar las dimensiones reales de la geometria a escala 1:1
        ' a partir del tamaño actual de la vista.
        Dim anchoReal As Double = vista.Width / vista.Scale
        Dim altoReal As Double = vista.Height / vista.Scale

        ' Reserva del 10 % para cotas, separaciones y tolerancias.
        Dim anchoUtil As Double = anchoZona * 0.90
        Dim altoUtil As Double = altoZona * 0.90

        For Each s As Double In escalasDisponibles
            If (anchoReal * s <= anchoUtil) AndAlso _
               (altoReal * s <= altoUtil) Then
                Return s
            End If
        Next

        Return escalasDisponibles(escalasDisponibles.Length - 1)

    Catch
        Return 0.1
    End Try

End Function


Function CalcularEscalaComunVistasCreadas( _
    ByVal vista1 As DrawingView, _
    ByVal anchoZona1 As Double, _
    ByVal altoZona1 As Double, _
    ByVal vista2 As DrawingView, _
    ByVal anchoZona2 As Double, _
    ByVal altoZona2 As Double, _
    ByVal vista3 As DrawingView, _
    ByVal anchoZona3 As Double, _
    ByVal altoZona3 As Double, _
    ByVal escalasDisponibles() As Double) As Double

    Try
        Dim e1 As Double = CalcularEscalaMaximaVistaCreada( _
            vista1, anchoZona1, altoZona1, escalasDisponibles)

        Dim e2 As Double = CalcularEscalaMaximaVistaCreada( _
            vista2, anchoZona2, altoZona2, escalasDisponibles)

        Dim e3 As Double = CalcularEscalaMaximaVistaCreada( _
            vista3, anchoZona3, altoZona3, escalasDisponibles)

        Return Math.Min(e1, Math.Min(e2, e3))

    Catch
        Return 0.1
    End Try

End Function


'---------------------------------------------------------
' ESCALAS Y ORIENTACIONES v10.13
'---------------------------------------------------------

Function CalcularEscalaLongitudinalPlegado(ByVal smDef As SheetMetalComponentDefinition, _
                                           ByVal sheet As Sheet, _
                                           ByVal usarA3 As Boolean, _
                                           ByVal escalasDisponibles() As Double) As Double
    Try
        Dim flatW As Double = 1
        Dim flatH As Double = 1
        ObtenerDimensionesDesarrollo(smDef, flatW, flatH)

        Dim largoMM As Double = Math.Max(flatW, flatH) * 10.0
        Dim escalaTope As Double

        If usarA3 Then
            If largoMM > 1600 Then
                escalaTope = 0.0833333333   ' 1:12
            ElseIf largoMM > 1100 Then
                escalaTope = 0.1            ' 1:10
            Else
                escalaTope = 0.125          ' 1:8
            End If
        Else
            If largoMM > 600 Then
                escalaTope = 0.1            ' 1:10
            ElseIf largoMM > 350 Then
                escalaTope = 0.125          ' 1:8
            Else
                escalaTope = 0.2            ' 1:5
            End If
        End If

        Dim anchoZona As Double
        Dim altoZona As Double

        If usarA3 Then
            anchoZona = sheet.Width * 0.70
            altoZona = (sheet.Height - ObtenerYSuperiorCajetin(sheet)) * 0.24
        Else
            anchoZona = sheet.Width * 0.78
            altoZona = (sheet.Height - ObtenerYSuperiorCajetin(sheet)) * 0.19
        End If

        For Each s As Double In escalasDisponibles
            If s <= escalaTope + 0.0000001 Then
                If CabeRectanguloEnZona(flatW, flatH, anchoZona, altoZona, s, True) Then Return s
            End If
        Next

        Return escalasDisponibles(escalasDisponibles.Length - 1)
    Catch
        If usarA3 Then Return 0.0833333333
        Return 0.1
    End Try
End Function

Sub ObtenerOrientacionesPiezaLarga(ByVal partDoc As PartDocument, _
                                   ByRef principal As ViewOrientationTypeEnum, _
                                   ByRef secundaria As ViewOrientationTypeEnum, _
                                   ByRef extremo As ViewOrientationTypeEnum)
    principal = ViewOrientationTypeEnum.kFrontViewOrientation
    secundaria = ViewOrientationTypeEnum.kTopViewOrientation
    extremo = ViewOrientationTypeEnum.kRightViewOrientation

    Try
        Dim box As Box = partDoc.ComponentDefinition.RangeBox
        Dim dx As Double = Math.Abs(box.MaxPoint.X - box.MinPoint.X)
        Dim dy As Double = Math.Abs(box.MaxPoint.Y - box.MinPoint.Y)
        Dim dz As Double = Math.Abs(box.MaxPoint.Z - box.MinPoint.Z)

        ' Inventor: Front proyecta XY, Top proyecta XZ y Right proyecta YZ.
        If dx >= dy AndAlso dx >= dz Then
            principal = ViewOrientationTypeEnum.kFrontViewOrientation
            secundaria = ViewOrientationTypeEnum.kTopViewOrientation
            extremo = ViewOrientationTypeEnum.kRightViewOrientation
        ElseIf dy >= dx AndAlso dy >= dz Then
            principal = ViewOrientationTypeEnum.kFrontViewOrientation
            secundaria = ViewOrientationTypeEnum.kRightViewOrientation
            extremo = ViewOrientationTypeEnum.kTopViewOrientation
        Else
            principal = ViewOrientationTypeEnum.kTopViewOrientation
            secundaria = ViewOrientationTypeEnum.kRightViewOrientation
            extremo = ViewOrientationTypeEnum.kFrontViewOrientation
        End If
    Catch
    End Try
End Sub

 '---------------------------------------------------------
' CODIGO DE ARCHIVO Y GUARDADO SEGURO
'---------------------------------------------------------

Function ExtraerCodigoDesdeNombrePieza(ByVal rutaCompleta As String) As String
    Try
        If rutaCompleta Is Nothing OrElse Trim(rutaCompleta) = "" Then Return ""

        Dim nombre As String = _
            System.IO.Path.GetFileNameWithoutExtension(rutaCompleta).ToUpper()

        ' Prioridad para codigos Metalplak tipo A02896.
        Dim coincidenciaA As System.Text.RegularExpressions.Match = _
            System.Text.RegularExpressions.Regex.Match( _
                nombre, _
                "(?<![A-Z0-9])A[0-9]{5,}(?![0-9])")

        If coincidenciaA.Success Then Return coincidenciaA.Value

        ' Compatibilidad con codigos historicos exclusivamente numericos.
        Dim coincidenciaNumerica As System.Text.RegularExpressions.Match = _
            System.Text.RegularExpressions.Regex.Match( _
                nombre, _
                "(?<![0-9])[0-9]{5,}(?![0-9])")

        If coincidenciaNumerica.Success Then Return coincidenciaNumerica.Value

        Return ""
    Catch
        Return ""
    End Try
End Function


Sub GuardarPlanoConNombreCodigo(ByVal drawingDoc As DrawingDocument, _
                                ByVal rutaPlano As String, _
                                ByVal codigoEsperado As String)

    If drawingDoc Is Nothing Then
        Throw New Exception("No existe documento de plano para guardar.")
    End If

    If rutaPlano Is Nothing OrElse Trim(rutaPlano) = "" Then
        Throw New Exception("La ruta de guardado del plano esta vacia.")
    End If

    Dim carpeta As String = System.IO.Path.GetDirectoryName(rutaPlano)
    If carpeta = "" Then
        Throw New Exception("No se ha podido determinar la carpeta del plano.")
    End If

    If Not System.IO.Directory.Exists(carpeta) Then
        System.IO.Directory.CreateDirectory(carpeta)
    End If

    Dim nombreDestino As String = _
        System.IO.Path.GetFileNameWithoutExtension(rutaPlano).ToUpper()

    If nombreDestino <> Trim(codigoEsperado).ToUpper() Then
        Throw New Exception( _
            "El nombre calculado para el plano no coincide con el codigo de la pieza." & vbCrLf & _
            "Codigo esperado: " & codigoEsperado & vbCrLf & _
            "Nombre calculado: " & nombreDestino)
    End If

    Dim rutaActual As String = ""
    Try
        rutaActual = drawingDoc.FullFileName
    Catch
        rutaActual = ""
    End Try

    Dim mismaRuta As Boolean = False
    Try
        mismaRuta = String.Equals( _
            System.IO.Path.GetFullPath(rutaActual), _
            System.IO.Path.GetFullPath(rutaPlano), _
            System.StringComparison.OrdinalIgnoreCase)
    Catch
        mismaRuta = False
    End Try

    Try
        If mismaRuta Then
            drawingDoc.Save()
        Else
            ' Los planos son documentos generados. Si ya existe uno con el mismo
            ' codigo, se reemplaza para evitar que Inventor conserve el nombre
            ' de la plantilla abierta.
            If System.IO.File.Exists(rutaPlano) Then
                Try
                    System.IO.File.SetAttributes(rutaPlano, System.IO.FileAttributes.Normal)
                    System.IO.File.Delete(rutaPlano)
                Catch exBorrar As Exception
                    Throw New Exception( _
                        "Ya existe el plano '" & rutaPlano & _
                        "' y no se puede reemplazar. Comprueba que no este abierto." & vbCrLf & _
                        "Detalle: " & exBorrar.Message)
                End Try
            End If

            drawingDoc.SaveAs(rutaPlano, False)
        End If
    Catch exGuardar As Exception
        Throw New Exception( _
            "No se ha podido guardar el plano con el codigo '" & _
            codigoEsperado & "'." & vbCrLf & _
            "Ruta prevista: " & rutaPlano & vbCrLf & _
            "Detalle: " & exGuardar.Message)
    End Try

    ' Verificacion final: el documento abierto debe haber dejado de llamarse
    ' como la plantilla y debe apuntar exactamente al fichero del codigo.
    Dim rutaFinal As String = ""
    Try
        rutaFinal = drawingDoc.FullFileName
    Catch
        rutaFinal = ""
    End Try

    Dim guardadoCorrecto As Boolean = False
    Try
        guardadoCorrecto = String.Equals( _
            System.IO.Path.GetFullPath(rutaFinal), _
            System.IO.Path.GetFullPath(rutaPlano), _
            System.StringComparison.OrdinalIgnoreCase)
    Catch
        guardadoCorrecto = False
    End Try

    If Not guardadoCorrecto Then
        Throw New Exception( _
            "Inventor no ha cambiado el nombre del documento al codigo de la pieza." & vbCrLf & _
            "Nombre esperado: " & rutaPlano & vbCrLf & _
            "Nombre actual: " & rutaFinal)
    End If

End Sub


'---------------------------------------------------------
' PLANTILLA
'---------------------------------------------------------

Function ObtenerRutaPlantilla(ByVal rutaBase As String) As String
    If System.IO.File.Exists(rutaBase) Then Return rutaBase
    If System.IO.File.Exists(rutaBase & ".idw") Then Return rutaBase & ".idw"
    If System.IO.File.Exists(rutaBase & ".dwg") Then Return rutaBase & ".dwg"
    Return ""
End Function

'---------------------------------------------------------
' FORMATO DE HOJA
'---------------------------------------------------------

Sub AplicarA3Apaisado(ByVal sheet As Sheet)
    Try
        ' Cambia la hoja activa a A3 y mueve borde/cajetin con la hoja.
        sheet.ChangeSize(DrawingSheetSizeEnum.kA3DrawingSheetSize, True)
    Catch
        Try
            sheet.Size = DrawingSheetSizeEnum.kA3DrawingSheetSize
        Catch
        End Try
    End Try

    Try
        sheet.Orientation = PageOrientationTypeEnum.kLandscapePageOrientation
    Catch
        ' Si la plantilla ya queda apaisada o la propiedad no esta disponible, no bloqueamos.
    End Try
End Sub

Function ObtenerMayorDimensionFlatPatternMM(ByVal smDef As SheetMetalComponentDefinition) As Double
    Try
        Dim box As Box = smDef.FlatPattern.RangeBox
        Dim dx As Double = Math.Abs(box.MaxPoint.X - box.MinPoint.X) * 10.0
        Dim dy As Double = Math.Abs(box.MaxPoint.Y - box.MinPoint.Y) * 10.0
        Dim dz As Double = Math.Abs(box.MaxPoint.Z - box.MinPoint.Z) * 10.0
        Return Math.Max(dx, Math.Max(dy, dz))
    Catch
        Return 0
    End Try
End Function


'---------------------------------------------------------
' CARPETAS PLANOS PRODUCCION POR RANGO DE CODIGO
'---------------------------------------------------------

Function ObtenerCarpetaPlanoIDWPorCodigo(ByVal rutaBasePlanosIDW As String, _
                                         ByVal codigo As String) As String

    If rutaBasePlanosIDW Is Nothing Then rutaBasePlanosIDW = ""
    rutaBasePlanosIDW = Trim(rutaBasePlanosIDW)

    If rutaBasePlanosIDW = "" Then
        Throw New Exception("Ruta base de planos IDW vacia.")
    End If

    If Not System.IO.Directory.Exists(rutaBasePlanosIDW) Then
        Throw New Exception("No existe la ruta base de PLANOS PRODUCCION:" & vbCrLf & rutaBasePlanosIDW & vbCrLf & vbCrLf & "Revisa que la unidad Q: este conectada.")
    End If

    Dim nombreCarpeta As String = ObtenerNombreCarpetaRangoIDWExistenteOPreferido(rutaBasePlanosIDW, codigo)

    If nombreCarpeta = "" Then
        nombreCarpeta = "00_REVISAR_CODIGO_IDW"
    End If

    Return System.IO.Path.Combine(rutaBasePlanosIDW, nombreCarpeta)

End Function


Function ObtenerNombreCarpetaRangoIDWExistenteOPreferido(ByVal rutaBasePlanosIDW As String, _
                                                         ByVal codigo As String) As String

    If codigo Is Nothing Then Return ""

    codigo = NormalizarCodigoParaRango(codigo)

    If codigo = "" Then Return ""

    Dim prefijoA As Boolean = False
    Dim parteNumerica As String = ""

    If codigo.ToUpper().StartsWith("A") Then
        prefijoA = True
        parteNumerica = ExtraerDigitosIniciales(codigo.Substring(1))
    Else
        parteNumerica = ExtraerDigitosIniciales(codigo)
    End If

    If parteNumerica = "" Then Return ""

    Dim numero As Integer = 0

    Try
        numero = CInt(parteNumerica)
    Catch
        Return ""
    End Try

    If numero < 0 Then Return ""

    If prefijoA Then
        ' La estructura A visible es por bloques de 1000:
        ' A00000-A00999 IDW
        ' A01000-A01999 IDW
        ' A02000-A02999 IDW
        Dim carpetaA As String = CrearNombreRango(numero, 1000, True)

        If System.IO.Directory.Exists(System.IO.Path.Combine(rutaBasePlanosIDW, carpetaA)) Then
            Return carpetaA
        End If

        Return carpetaA
    End If

    ' En codigos numericos puede haber instalaciones con bloque de 1000
    ' y tambien estructura por bloque de 500, como se ve en PLANOS PRODUCCION.
    '
    ' Prioridad:
    ' 1) Si existe carpeta de 1000, usarla. Ejemplo pedido: 44000-44999 IDW.
    ' 2) Si no existe, buscar carpeta de 500. Ejemplo estructura vista: 84500-84999 IDW.
    ' 3) Si no existe ninguna, crear/preparar carpeta de 1000.

    Dim carpeta1000 As String = CrearNombreRango(numero, 1000, False)
    If System.IO.Directory.Exists(System.IO.Path.Combine(rutaBasePlanosIDW, carpeta1000)) Then
        Return carpeta1000
    End If

    Dim carpeta500 As String = CrearNombreRango(numero, 500, False)
    If System.IO.Directory.Exists(System.IO.Path.Combine(rutaBasePlanosIDW, carpeta500)) Then
        Return carpeta500
    End If

    Return carpeta1000

End Function


Function CrearNombreRango(ByVal numero As Integer, _
                          ByVal bloque As Integer, _
                          ByVal conPrefijoA As Boolean) As String

    If bloque <= 0 Then bloque = 1000

    Dim inicio As Integer = (numero \ bloque) * bloque
    Dim fin As Integer = inicio + bloque - 1

    Dim inicioTxt As String = inicio.ToString("00000")
    Dim finTxt As String = fin.ToString("00000")

    If conPrefijoA Then
        Return "A" & inicioTxt & "-A" & finTxt & " IDW"
    End If

    Return inicioTxt & "-" & finTxt & " IDW"

End Function


Function NormalizarCodigoParaRango(ByVal codigo As String) As String

    If codigo Is Nothing Then Return ""

    codigo = Trim(codigo)

    If codigo = "" Then Return ""

    codigo = QuitarExtensionInventorLocal(codigo)
    codigo = QuitarIndiceInventorLocal(codigo)

    If codigo.Contains(" - ") Then
        codigo = Trim(codigo.Substring(0, codigo.IndexOf(" - ")))
    End If

    If codigo.Contains(" ") Then
        codigo = Trim(codigo.Substring(0, codigo.IndexOf(" ")))
    End If

    codigo = codigo.ToUpper()

    Return codigo

End Function


Function ExtraerDigitosIniciales(ByVal texto As String) As String

    If texto Is Nothing Then Return ""

    texto = Trim(texto)

    Dim salida As String = ""

    For i As Integer = 0 To texto.Length - 1
        Dim c As String = texto.Substring(i, 1)

        If c >= "0" AndAlso c <= "9" Then
            salida = salida & c
        Else
            Exit For
        End If
    Next

    Return salida

End Function


Function QuitarIndiceInventorLocal(ByVal txt As String) As String

    If txt Is Nothing Then Return ""

    txt = Trim(txt)

    Dim pos As Integer = txt.LastIndexOf(":")

    If pos <= 0 Then Return txt

    Dim cola As String = txt.Substring(pos + 1)

    If ExtraerDigitosIniciales(cola) = cola Then
        Return Trim(txt.Substring(0, pos))
    End If

    Return txt

End Function


Function QuitarExtensionInventorLocal(ByVal txt As String) As String

    If txt Is Nothing Then Return ""

    txt = Trim(txt)

    If txt.ToUpper().EndsWith(".IPT") Then Return Trim(txt.Substring(0, txt.Length - 4))
    If txt.ToUpper().EndsWith(".IAM") Then Return Trim(txt.Substring(0, txt.Length - 4))
    If txt.ToUpper().EndsWith(".IDW") Then Return Trim(txt.Substring(0, txt.Length - 4))
    If txt.ToUpper().EndsWith(".DWG") Then Return Trim(txt.Substring(0, txt.Length - 4))

    Return txt

End Function

'---------------------------------------------------------
' VALIDACIONES Y DATOS
'---------------------------------------------------------

Function EsPiezaChapa(ByVal p As PartDocument) As Boolean
    If p Is Nothing Then Return False
    Return TypeOf p.ComponentDefinition Is SheetMetalComponentDefinition
End Function

Function ObtenerEspesorChapa(ByVal p As PartDocument) As String
    Try
        Dim smDef As SheetMetalComponentDefinition = TryCast(p.ComponentDefinition, SheetMetalComponentDefinition)
        If smDef Is Nothing Then Return ""

        Dim espesorCm As Double = smDef.Thickness.Value
        Dim espesorMm As Double = espesorCm * 10.0
        Return Math.Round(espesorMm, 2).ToString().Replace(",", ".") & " mm"
    Catch
        Return ""
    End Try
End Function


Function LimitarEscalaMaximaPorDesarrollo(ByVal smDef As SheetMetalComponentDefinition, _
                                           ByVal sheet As Sheet, _
                                           ByVal escalaActual As Double) As Double
    Try
        If smDef Is Nothing Then Return escalaActual
        If sheet Is Nothing Then Return escalaActual
        If escalaActual <= 0 Then Return escalaActual

        Dim flatW As Double = 1
        Dim flatH As Double = 1
        ObtenerDimensionesDesarrollo(smDef, flatW, flatH)

        Dim mayor As Double = Math.Max(flatW, flatH)

        ' Inventor trabaja en cm.
        ' 90 cm = 900 mm aprox.
        If sheet.Width > sheet.Height Then
            If mayor >= 90 AndAlso escalaActual > 0.1666666667 Then
                Return 0.1666666667  ' 1:6
            End If
        End If

        Return escalaActual

    Catch
        Return escalaActual
    End Try
End Function


Function CalcularEscalaGeneralPlanoPlegado(ByVal partDoc As PartDocument, _
                                           ByVal smDef As SheetMetalComponentDefinition, _
                                           ByVal sheet As Sheet, _
                                           ByVal escalasDisponibles() As Double) As Double
    Try
        '---------------------------------------------------------
        ' v10.12 - ESCALA EQUILIBRADA
        '---------------------------------------------------------
        '
        ' v10.10 fallaba por conservadora: salia 1:9 / 1:15.
        ' v10.11 fallaba por agresiva: salia 1:5 y las vistas invadian cajetin.
        '
        ' Criterio nuevo:
        ' - El desarrollo sigue siendo la vista principal del plano.
        ' - Pero la escala tambien valida que las vistas plegadas entren debajo
        '   sin invadir cajetin ni pisarse con la isometrica.
        ' - Se usa la mayor escala normalizada 1:X que entra en TODO el layout.
        '
        ' Para la pieza larga mostrada, el resultado esperado es aprox. 1:6,
        ' no 1:9 ni 1:5.

        If smDef Is Nothing Then Return 0.1
        If sheet Is Nothing Then Return 0.1

        Dim anchoHoja As Double = sheet.Width
        Dim altoHoja As Double = sheet.Height
        Dim hojaApaisada As Boolean = anchoHoja > altoHoja

        Dim yCajetinSup As Double = ObtenerYSuperiorCajetin(sheet)

        Dim margenX As Double = 1.5
        Dim margenSuperior As Double = 1.0
        Dim margenCajetin As Double = 1.2

        Dim anchoUtil As Double = anchoHoja - (2 * margenX)
        Dim altoUtil As Double = altoHoja - yCajetinSup - margenSuperior - margenCajetin

        If anchoUtil <= 5 Then anchoUtil = anchoHoja * 0.90
        If altoUtil <= 5 Then altoUtil = altoHoja * 0.68

        Dim flatW As Double = 1
        Dim flatH As Double = 1
        ObtenerDimensionesDesarrollo(smDef, flatW, flatH)

        Dim modMayor As Double = 1
        Dim modMedia As Double = 1
        Dim modMenor As Double = 1
        ObtenerDimensionesModeloOrdenadas(partDoc, modMayor, modMedia, modMenor)

        If hojaApaisada Then

            '---------------------------------------------------------
            ' ZONAS REALES DEL LAYOUT APAISADO
            '---------------------------------------------------------
            ' Arriba izquierda: desarrollo + cotas.
            ' Centro superior: simbolo de datos.
            ' Debajo izquierda: vista principal / superior / lateral.
            ' Derecha: isometrica, sin invadir cajetin.
            '
            ' Estas zonas son deliberadamente realistas, no excesivamente
            ' conservadoras, pero impiden que 1:5 invada el cajetin.

            Dim zonaDesW As Double = anchoUtil * 0.54
            Dim zonaDesH As Double = altoUtil * 0.44

            Dim zonaPlegW As Double = anchoUtil * 0.42
            Dim zonaPlegH As Double = altoUtil * 0.36

            Dim zonaLatW As Double = anchoUtil * 0.20
            Dim zonaLatH As Double = altoUtil * 0.50

            Dim zonaIsoW As Double = anchoUtil * 0.36
            Dim zonaIsoH As Double = altoUtil * 0.46

            For Each s As Double In escalasDisponibles

                Dim cabeDes As Boolean = CabeRectanguloEnZonaEscalaReal(flatW, flatH, zonaDesW, zonaDesH, s, True)

                ' Vista principal plegada: normalmente mayor x media.
                Dim cabePleg As Boolean = CabeRectanguloEnZonaEscalaReal(modMayor, modMedia, zonaPlegW, zonaPlegH, s, True)

                ' Vista lateral/seccion: puede ser media x menor o media x mayor
                ' segun orientacion de la pieza. Usamos una validacion prudente.
                Dim cabeLat As Boolean = CabeRectanguloEnZonaEscalaReal(modMedia, modMayor, zonaLatW, zonaLatH, s, True)

                ' Isometrica: necesita margen para no pisar cajetin.
                Dim cabeIso As Boolean = CabeRectanguloEnZonaEscalaReal(modMayor, modMedia, zonaIsoW, zonaIsoH, s, True)

                If cabeDes AndAlso cabePleg AndAlso cabeLat AndAlso cabeIso Then
                    Return s
                End If

            Next

        Else

            Dim zonaDesWV As Double = anchoUtil * 0.72
            Dim zonaDesHV As Double = altoUtil * 0.32

            Dim zonaPlegWV As Double = anchoUtil * 0.42
            Dim zonaPlegHV As Double = altoUtil * 0.24

            For Each s As Double In escalasDisponibles

                Dim cabeDesV As Boolean = CabeRectanguloEnZonaEscalaReal(flatW, flatH, zonaDesWV, zonaDesHV, s, True)
                Dim cabePlegV As Boolean = CabeRectanguloEnZonaEscalaReal(modMayor, modMedia, zonaPlegWV, zonaPlegHV, s, True)

                If cabeDesV AndAlso cabePlegV Then
                    Return s
                End If

            Next

        End If

        Return escalasDisponibles(escalasDisponibles.Length - 1)

    Catch
        Return 0.1
    End Try
End Function


Function CabeRectanguloEnZonaEscalaReal(ByVal w As Double, _
                                        ByVal h As Double, _
                                        ByVal zonaW As Double, _
                                        ByVal zonaH As Double, _
                                        ByVal escala As Double, _
                                        ByVal permitirGiro As Boolean) As Boolean
    Try
        If w <= 0 Or h <= 0 Or zonaW <= 0 Or zonaH <= 0 Or escala <= 0 Then Return False

        ' Margen más realista que el anterior.
        ' Antes se penalizaba demasiado la pieza y bajaba la escala.
        Dim margenCotas As Double = 0.92

        Dim zW As Double = zonaW * margenCotas
        Dim zH As Double = zonaH * margenCotas

        If (w * escala <= zW) AndAlso (h * escala <= zH) Then Return True

        If permitirGiro Then
            If (h * escala <= zW) AndAlso (w * escala <= zH) Then Return True
        End If

        Return False

    Catch
        Return False
    End Try
End Function


Function CabeRectanguloEnZona(ByVal w As Double, _
                              ByVal h As Double, _
                              ByVal zonaW As Double, _
                              ByVal zonaH As Double, _
                              ByVal escala As Double, _
                              ByVal permitirGiro As Boolean) As Boolean
    Try
        If w <= 0 Or h <= 0 Or zonaW <= 0 Or zonaH <= 0 Or escala <= 0 Then Return False

        ' Factor de margen para cotas y separaciones.
        Dim margenCotas As Double = 0.92

        Dim zW As Double = zonaW * margenCotas
        Dim zH As Double = zonaH * margenCotas

        If (w * escala <= zW) AndAlso (h * escala <= zH) Then Return True

        If permitirGiro Then
            If (h * escala <= zW) AndAlso (w * escala <= zH) Then Return True
        End If

        Return False

    Catch
        Return False
    End Try
End Function


Sub ObtenerDimensionesDesarrollo(ByVal smDef As SheetMetalComponentDefinition, _
                                 ByRef anchoFlat As Double, _
                                 ByRef altoFlat As Double)
    anchoFlat = 1
    altoFlat = 1

    Try
        If Not smDef.HasFlatPattern Then
            smDef.Unfold()
            Try
                smDef.FlatPattern.ExitEdit()
            Catch
            End Try
        End If

        Dim box As Box = smDef.FlatPattern.RangeBox

        Dim dx As Double = Math.Abs(box.MaxPoint.X - box.MinPoint.X)
        Dim dy As Double = Math.Abs(box.MaxPoint.Y - box.MinPoint.Y)
        Dim dz As Double = Math.Abs(box.MaxPoint.Z - box.MinPoint.Z)

        ' En desarrollo puede venir en XY, XZ o YZ segun orientacion interna.
        ' Cogemos las dos dimensiones mayores para evitar que una Z casi nula falsee el calculo.
        Dim a As Double = Math.Max(dx, Math.Max(dy, dz))
        Dim c As Double = Math.Min(dx, Math.Min(dy, dz))
        Dim b As Double = dx + dy + dz - a - c

        If a <= 0 Then a = 1
        If b <= 0 Then b = Math.Max(c, 1)

        anchoFlat = a
        altoFlat = b

    Catch
        anchoFlat = 1
        altoFlat = 1
    End Try
End Sub


Sub ObtenerDimensionesModeloOrdenadas(ByVal partDoc As PartDocument, _
                                      ByRef mayor As Double, _
                                      ByRef media As Double, _
                                      ByRef menor As Double)
    mayor = 1
    media = 1
    menor = 1

    Try
        Dim box As Box = partDoc.ComponentDefinition.RangeBox

        Dim dx As Double = Math.Abs(box.MaxPoint.X - box.MinPoint.X)
        Dim dy As Double = Math.Abs(box.MaxPoint.Y - box.MinPoint.Y)
        Dim dz As Double = Math.Abs(box.MaxPoint.Z - box.MinPoint.Z)

        mayor = Math.Max(dx, Math.Max(dy, dz))
        menor = Math.Min(dx, Math.Min(dy, dz))
        media = dx + dy + dz - mayor - menor

        If mayor <= 0 Then mayor = 1
        If media <= 0 Then media = 1
        If menor <= 0 Then menor = 1

    Catch
        mayor = 1
        media = 1
        menor = 1
    End Try
End Sub


Function ObtenerYSuperiorCajetin(ByVal sheet As Sheet) As Double
    Try
        If sheet IsNot Nothing AndAlso sheet.TitleBlock IsNot Nothing Then
            Return sheet.TitleBlock.RangeBox.MaxPoint.Y
        End If
    Catch
    End Try

    Try
        Return sheet.Height * 0.18
    Catch
        Return 5
    End Try
End Function

Function CalcularEscalaParaZona(ByVal smDef As SheetMetalComponentDefinition, _
                                ByVal anchoZonaCm As Double, _
                                ByVal altoZonaCm As Double, _
                                ByVal escalasDisponibles() As Double) As Double
    Try
        Dim w As Double = 1
        Dim h As Double = 1
        ObtenerDimensionesDesarrollo(smDef, w, h)

        For Each s As Double In escalasDisponibles
            If CabeRectanguloEnZona(w, h, anchoZonaCm, altoZonaCm, s, True) Then Return s
        Next

        Return escalasDisponibles(escalasDisponibles.Length - 1)
    Catch
        Return 0.1
    End Try
End Function


Function CalcularEscalaModeloPlegado(ByVal partDoc As PartDocument, _
                                     ByVal anchoZonaCm As Double, _
                                     ByVal altoZonaCm As Double, _
                                     ByVal escalasDisponibles() As Double) As Double
    Try
        Dim mayor As Double = 1
        Dim media As Double = 1
        Dim menor As Double = 1

        ObtenerDimensionesModeloOrdenadas(partDoc, mayor, media, menor)

        For Each s As Double In escalasDisponibles
            If CabeRectanguloEnZona(mayor, media, anchoZonaCm, altoZonaCm, s, True) Then Return s
        Next

        Return escalasDisponibles(escalasDisponibles.Length - 1)
    Catch
        Return 0.1
    End Try
End Function

Function SubirUnPasoEscala(ByVal escalaActual As Double, ByVal escalasDisponibles() As Double) As Double
    Try
        Dim mejorIndice As Integer = escalasDisponibles.Length - 1
        Dim mejorDiferencia As Double = 999999

        For i As Integer = 0 To escalasDisponibles.Length - 1
            Dim dif As Double = Math.Abs(escalasDisponibles(i) - escalaActual)
            If dif < mejorDiferencia Then
                mejorDiferencia = dif
                mejorIndice = i
            End If
        Next

        If mejorIndice > 0 Then
            Return escalasDisponibles(mejorIndice - 1)
        End If

        Return escalasDisponibles(mejorIndice)
    Catch
        Return escalaActual
    End Try
End Function

Function FormatearEscala(ByVal escala As Double) As String
    Try
        If escala <= 0 Then Return ""

        Dim divisor As Double = 1 / escala
        Dim divisorEntero As Integer = CInt(Math.Round(divisor, 0))

        If divisorEntero < 1 Then divisorEntero = 1

        Return "1:" & divisorEntero.ToString()
    Catch
        Return ""
    End Try
End Function

 '---------------------------------------------------------
' USUARIO ACTUAL DE INVENTOR / WINDOWS
'---------------------------------------------------------

Function ObtenerAutorActual(ByVal invApp As Inventor.Application) As String

    Dim autor As String = ""

    ' 1. Usuario configurado en:
    '    Herramientas > Opciones de la aplicación > General > Nombre de usuario
    Try
        Dim opcionesGenerales As Object = invApp.GeneralOptions

        Dim valorUsuario As Object = _
            Microsoft.VisualBasic.Interaction.CallByName( _
                opcionesGenerales, _
                "UserName", _
                Microsoft.VisualBasic.CallType.Get)

        If valorUsuario IsNot Nothing Then
            autor = Trim(CStr(valorUsuario))
        End If
    Catch
        autor = ""
    End Try

    ' 2. Respaldo: usuario que ha iniciado sesión en Windows.
    If autor = "" Then
        Try
            autor = Trim(System.Environment.UserName)
        Catch
            autor = ""
        End Try
    End If

    ' 3. Último respaldo para no dejar vacío PLANO REALIZADO.
    If autor = "" Then autor = "USUARIO"

    Return autor

End Function


'---------------------------------------------------------
' iPROPERTIES
'---------------------------------------------------------

Function LeerPropiedad(ByVal doc As Document, ByVal setName As String, ByVal propName As String) As String
    Try
        Dim ps As PropertySet = doc.PropertySets.Item(setName)
        Dim p As Inventor.Property = ps.Item(propName)
        If p Is Nothing Then Return ""
        If p.Value Is Nothing Then Return ""
        Return CStr(p.Value)
    Catch
        Return ""
    End Try
End Function

Function LeerPropiedadUsuario(ByVal doc As Document, ByVal propName As String) As String
    Try
        Dim ps As PropertySet = doc.PropertySets.Item("Inventor User Defined Properties")
        Dim p As Inventor.Property = ps.Item(propName)
        If p Is Nothing Then Return ""
        If p.Value Is Nothing Then Return ""
        Return CStr(p.Value)
    Catch
        Return ""
    End Try
End Function

Sub EscribirPropiedad(ByVal doc As Document, ByVal setName As String, ByVal propName As String, ByVal value As String)
    Try
        Dim ps As PropertySet = doc.PropertySets.Item(setName)
        Try
            Dim p As Inventor.Property = ps.Item(propName)
            p.Value = value
        Catch
        End Try
    Catch
    End Try
End Sub

Sub EscribirPropiedadUsuario(ByVal doc As Document, ByVal propName As String, ByVal value As String)
    Try
        Dim ps As PropertySet = doc.PropertySets.Item("Inventor User Defined Properties")
        Try
            Dim p As Inventor.Property = ps.Item(propName)
            p.Value = value
        Catch
            ps.Add(value, propName)
        End Try
    Catch
    End Try
End Sub

'---------------------------------------------------------
' UTILIDADES
'---------------------------------------------------------

Function LimpiarNombreArchivo(ByVal nombre As String) As String
    Dim invalidos() As Char = System.IO.Path.GetInvalidFileNameChars()
    For Each c As Char In invalidos
        nombre = nombre.Replace(c, "_"c)
    Next
    nombre = nombre.Replace("  ", " ").Trim()
    If nombre = "" Then nombre = "PLANO_PLEGADO"
    Return nombre
End Function

'---------------------------------------------------------
' EXPORTACION PDF / DWF
'---------------------------------------------------------

Sub ExportarPDF(ByVal invApp As Inventor.Application, ByVal drawingDoc As DrawingDocument, ByVal rutaPDF As String)
    Try
        Dim pdfAddIn As TranslatorAddIn = invApp.ApplicationAddIns.ItemById("{0AC6FD96-2F4D-42CE-8BE0-8AEA580399E4}")
        Dim context As TranslationContext = invApp.TransientObjects.CreateTranslationContext()
        context.Type = IOMechanismEnum.kFileBrowseIOMechanism

        Dim options As NameValueMap = invApp.TransientObjects.CreateNameValueMap()

        If pdfAddIn.HasSaveCopyAsOptions(drawingDoc, context, options) Then
            options.Value("All_Color_AS_Black") = 0
            options.Value("Remove_Line_Weights") = 0
            options.Value("Vector_Resolution") = 400
            options.Value("Sheet_Range") = PrintRangeEnum.kPrintAllSheets
        End If

        Dim data As DataMedium = invApp.TransientObjects.CreateDataMedium()
        data.FileName = rutaPDF
        pdfAddIn.SaveCopyAs(drawingDoc, context, options, data)
    Catch ex As Exception
        Throw New Exception("Error exportando PDF: " & ex.Message)
    End Try
End Sub

Sub ExportarDWF(ByVal invApp As Inventor.Application, ByVal drawingDoc As DrawingDocument, ByVal rutaDWF As String)
    Try
        Dim dwfAddIn As TranslatorAddIn = invApp.ApplicationAddIns.ItemById("{0AC6FD95-2F4D-42CE-8BE0-8AEA580399E4}")
        Dim context As TranslationContext = invApp.TransientObjects.CreateTranslationContext()
        context.Type = IOMechanismEnum.kFileBrowseIOMechanism

        Dim options As NameValueMap = invApp.TransientObjects.CreateNameValueMap()

        If dwfAddIn.HasSaveCopyAsOptions(drawingDoc, context, options) Then
            options.Value("Launch_Viewer") = 0
            options.Value("Publish_All_Sheets") = 1
        End If

        Dim data As DataMedium = invApp.TransientObjects.CreateDataMedium()
        data.FileName = rutaDWF
        dwfAddIn.SaveCopyAs(drawingDoc, context, options, data)
    Catch
        ' No bloqueamos si falla DWF.
    End Try
End Sub
