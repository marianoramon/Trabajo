' PLANO UNIFICADO CON SOPORTE PARA PINTURA
' Integración de planos: plegado, pintura, soldadura y despiece
' v10.13 + v1.20 - Pintura RAL implementada

Sub Main()

    Dim invApp As Inventor.Application = ThisApplication
    Dim tg As TransientGeometry = invApp.TransientGeometry

    Dim form As New FormularioSeleccionPlano()
    Dim tipoPlano As String = ""
    Dim incluirCodigoPlegado As Boolean = True

    If form.ShowDialog() = System.Windows.Forms.DialogResult.OK Then
        tipoPlano = form.PlanoSeleccionado
        incluirCodigoPlegado = form.IncluirCodigoPlegado
    End If

    If tipoPlano = "" Then
        Exit Sub
    End If

    Try
        Select Case tipoPlano
            Case "PLEGADO"
                EjecutarPlanoPlegado(invApp, tg, incluirCodigoPlegado)
            Case "PINTURA"
                EjecutarPlanoPintura(invApp, tg)
            Case "SOLDADURA"
                EjecutarPlanoSoldadura(invApp, tg)
            Case "DESPIECE"
                EjecutarDespieceVisual(invApp, tg)
        End Select
    Catch ex As Exception
        MessageBox.Show("Error al generar el plano:" & vbCrLf & vbCrLf & ex.Message, "Plano Unificado")
    End Try

End Sub

Sub EjecutarPlanoPlegado(ByVal invApp As Inventor.Application, ByVal tg As TransientGeometry, ByVal incluirCodigoPlegado As Boolean)

    Dim RUTA_PLANTILLA_BASE As String = "Q:\BIBLIOTECA INVENTOR 2019\PLANTILLAS 2019\PLANO METALPLAK V19 -LOGO NUEVO"
    Dim RUTA_BASE_PLANOS_IDW As String = "Q:\DISEÑOS\PLANOS PRODUCCIÓN"
    Dim NOMBRE_CARPETA_SALIDA As String = "02_PLANOS_PLEGADO"
    Dim NOMBRE_SIMBOLO_DATOS As String = "DATOS PLEGADO"

    Dim ESCALAS() As Double = {1, 0.5, 0.3333333333, 0.25, 0.2, 0.1666666667, 0.1428571429, 0.125, 0.1111111111, 0.1, 0.0833333333, 0.0666666667, 0.05, 0.0333333333, 0.025, 0.02, 0.01}

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
        CrearPlanoPlegadoPiezaIndividual(invApp, tg, partDoc, RUTA_PLANTILLA_BASE, RUTA_BASE_PLANOS_IDW, NOMBRE_CARPETA_SALIDA, NOMBRE_SIMBOLO_DATOS, ESCALAS, False, incluirCodigoPlegado)
        MessageBox.Show("Plano de plegado generado correctamente. El plano queda abierto para revision.", "Plano de plegado")
    Catch ex As Exception
        MessageBox.Show("No se ha podido generar el plano de plegado:" & vbCrLf & vbCrLf & ex.Message, "Plano de plegado")
    End Try

End Sub

Sub EjecutarPlanoPintura(ByVal invApp As Inventor.Application, ByVal tg As TransientGeometry)
    Dim RUTA_PLANTILLA_METALPLAK As String = "Q:\BIBLIOTECA INVENTOR 2019\PLANTILLAS 2019\PLANO METALPLAK V19 -LOGO NUEVO.idw"
    Dim NOMBRE_CARPETA_SALIDA As String = "03_PLANOS_PINTURA"
    Dim CODIGO_RAL_9005 As String = "AP02554"
    Dim CODIGO_RAL_7012 As String = "AP02645"
    Dim CENTRO_TRABAJO As String = "PINTURA"
    Dim ESCALAS() As Double = {1, 0.5, 0.3333333333, 0.25, 0.2, 0.1666666667, 0.125, 0.1, 0.0666666667, 0.05, 0.0333333333, 0.025, 0.02, 0.01}

    Try
        CrearPlanoPinturaRAL(invApp, tg, RUTA_PLANTILLA_METALPLAK, NOMBRE_CARPETA_SALIDA, CODIGO_RAL_9005, CODIGO_RAL_7012, CENTRO_TRABAJO, ESCALAS)
        MessageBox.Show("Plano de pintura generado correctamente.", "Plano Pintura")
    Catch ex As Exception
        MessageBox.Show("No se ha podido generar el plano de pintura:" & vbCrLf & vbCrLf & ex.Message, "Plano de pintura")
    End Try
End Sub

Sub EjecutarPlanoSoldadura(ByVal invApp As Inventor.Application, ByVal tg As TransientGeometry)
    MessageBox.Show("Plano de soldadura - Función a implementar.", "Plano Soldadura")
End Sub

Sub EjecutarDespieceVisual(ByVal invApp As Inventor.Application, ByVal tg As TransientGeometry)
    MessageBox.Show("Despiece visual - Función a implementar.", "Despiece Visual")
End Sub

'---------------------------------------------------------
' PINTURA RAL - IMPLEMENTACION COMPLETA
'---------------------------------------------------------

Sub CrearPlanoPinturaRAL(ByVal invApp As Inventor.Application, _
                         ByVal tg As TransientGeometry, _
                         ByVal rutaPlantillaMetalplak As String, _
                         ByVal nombreCarpetaSalida As String, _
                         ByVal codigoRal9005 As String, _
                         ByVal codigoRal7012 As String, _
                         ByVal centroTrabajo As String, _
                         ByVal escalasDisponibles() As Double)

    Dim paso As String = "INICIO"

    Try
        paso = "Leer documento activo"
        Dim modelDoc As Document = invApp.ActiveDocument

        If modelDoc Is Nothing Then
            Throw New Exception("No hay ningun documento activo.")
        End If

        If modelDoc.DocumentType <> DocumentTypeEnum.kAssemblyDocumentObject AndAlso _
           modelDoc.DocumentType <> DocumentTypeEnum.kPartDocumentObject Then
            Throw New Exception("Esta regla debe ejecutarse desde un ensamblaje .IAM o una pieza .IPT.")
        End If

        If modelDoc.FullFileName = "" Then
            Throw New Exception("Guarda primero el modelo antes de generar el plano de pintura.")
        End If

        paso = "Detectar RAL desde nombre"
        Dim nombreModelo As String = System.IO.Path.GetFileNameWithoutExtension(modelDoc.FullFileName)
        Dim ral As String = DetectarRALDesdeNombre(nombreModelo)

        If ral = "" Then
            Throw New Exception("No se ha detectado RAL en el nombre del fichero." & vbCrLf & vbCrLf & _
                                "El nombre debe contener R9005 o R7012." & vbCrLf & vbCrLf & _
                                "Nombre actual:" & vbCrLf & nombreModelo)
        End If

        Dim codigoPintura As String = ObtenerCodigoPinturaPorRAL(ral, codigoRal9005, codigoRal7012)

        If codigoPintura = "" Then
            Throw New Exception("RAL detectado pero sin codigo de pintura configurado: RAL " & ral)
        End If

        paso = "Leer propiedades modelo"
        Dim codigoModelo As String = LeerPropiedad(modelDoc, "Design Tracking Properties", "Part Number")
        Dim codAlmacen As String = LeerPropiedad(modelDoc, "Design Tracking Properties", "Stock Number")
        Dim descripcion As String = LeerPropiedad(modelDoc, "Design Tracking Properties", "Description")
        Dim proyecto As String = LeerPropiedad(modelDoc, "Design Tracking Properties", "Project")
        Dim disenador As String = LeerPropiedad(modelDoc, "Design Tracking Properties", "Designer")

        Dim revision As String = LeerPropiedad(modelDoc, "Inventor Summary Information", "Revision Number")
        If revision = "" Then revision = LeerPropiedad(modelDoc, "Summary Information", "Revision Number")
        If revision = "" Then revision = LeerPropiedad(modelDoc, "Design Tracking Properties", "Revision Number")
        If revision = "" Then revision = "00"

        If codigoModelo = "" And codAlmacen <> "" Then codigoModelo = codAlmacen
        If codigoModelo = "" Then codigoModelo = nombreModelo
        If codAlmacen = "" Then codAlmacen = codigoModelo
        If descripcion = "" Then descripcion = nombreModelo
        If proyecto = "" Then proyecto = "60.- PULVER AGRO"

        Dim textoAcabado As String = "ACABADO RAL " & ral
        Dim nombreSimboloRAL As String = "ACABADO RAL-" & ral
        Dim descripcionPlano As String = descripcion

        If Not descripcionPlano.ToUpper().Contains("R" & ral) AndAlso _
           Not descripcionPlano.ToUpper().Contains("RAL " & ral) AndAlso _
           Not descripcionPlano.ToUpper().Contains("RAL" & ral) Then
            descripcionPlano = descripcionPlano & " R" & ral
        End If

        paso = "Crear carpeta salida"
        Dim carpetaModelo As String = System.IO.Path.GetDirectoryName(modelDoc.FullFileName)

        If carpetaModelo.ToUpper().StartsWith("H:") Then
            carpetaModelo = "Q:" & carpetaModelo.Substring(2)
        End If

        Dim carpetaSalida As String = System.IO.Path.Combine(carpetaModelo, nombreCarpetaSalida)

        If Not System.IO.Directory.Exists(carpetaSalida) Then
            System.IO.Directory.CreateDirectory(carpetaSalida)
        End If

        paso = "Preparar rutas de salida"
        Dim codigoNombreFichero As String = ObtenerCodigoNombreFichero(modelDoc, codigoModelo, codAlmacen, nombreModelo)
        Dim descripcionNombreFichero As String = ObtenerDescripcionNombreFichero(modelDoc, descripcionPlano, nombreModelo)
        Dim nombreBase As String = CrearNombreBaseDesdeIProperties(codigoNombreFichero, descripcionNombreFichero)

        If nombreBase.Length > 120 Then nombreBase = nombreBase.Substring(0, 120).Trim()

        Dim codigoPiezaCajetin As String = ObtenerCodigoPiezaModelo(modelDoc, codigoModelo, codAlmacen, nombreModelo)
        Dim articuloCajetin As String = codigoPiezaCajetin
        Dim articuloFinalCajetin As String = codigoPiezaCajetin
        Dim referenciaClienteCajetin As String = codigoPintura

        Dim rutaPlano As String = System.IO.Path.Combine(carpetaSalida, nombreBase & ".idw")
        rutaPlano = ResolverRutaLibre(rutaPlano)

        Dim rutaPDF As String = System.IO.Path.ChangeExtension(rutaPlano, ".pdf")
        Dim rutaDWF As String = System.IO.Path.ChangeExtension(rutaPlano, ".dwf")

        paso = "Crear plano con plantilla Metalplak"
        Dim drawingDoc As DrawingDocument = CrearPlanoMetalplakSeguro(invApp, rutaPlantillaMetalplak)

        If drawingDoc Is Nothing Then
            Throw New Exception("No se ha podido crear el plano con la plantilla Metalplak.")
        End If

        paso = "Forzar unidades milimetros"
        ForzarUnidadesMilimetros(drawingDoc)

        paso = "Guardar plano nuevo"
        drawingDoc.SaveAs(rutaPlano, False)

        paso = "Leer hoja"
        Dim sheet As Sheet = drawingDoc.ActiveSheet

        If sheet Is Nothing Then
            Throw New Exception("El plano no tiene hoja activa.")
        End If

        Dim ancho As Double = sheet.Width
        Dim alto As Double = sheet.Height

        paso = "Calcular escala"
        Dim escalaPrincipal As Double = CalcularEscalaA4Maxima(modelDoc, sheet, escalasDisponibles)
        Dim escalaIso As Double = escalaPrincipal

        If escalaPrincipal <= 0 Then escalaPrincipal = 0.2
        If escalaIso <= 0 Then escalaIso = escalaPrincipal

        Dim escalaTexto As String = FormatearEscalaFormato(escalaPrincipal)

        Dim pPrincipal As Point2d = tg.CreatePoint2d(ancho * 0.25, alto * 0.73)
        Dim pLateral As Point2d = tg.CreatePoint2d(ancho * 0.64, alto * 0.73)
        Dim pSuperior As Point2d = tg.CreatePoint2d(ancho * 0.25, alto * 0.47)
        Dim pIso As Point2d = tg.CreatePoint2d(ancho * 0.64, alto * 0.46)
        Dim pSimboloRAL As Point2d = CalcularPuntoAcabadoRAL(sheet, tg)

        paso = "Crear vista principal"
        Dim vPrincipal As DrawingView = CrearVistaBaseSegura(invApp, sheet, modelDoc, pPrincipal, escalaPrincipal, ViewOrientationTypeEnum.kFrontViewOrientation)

        If vPrincipal Is Nothing Then
            Throw New Exception("No se ha podido crear la vista principal.")
        End If

        vPrincipal.Name = "VISTA PRINCIPAL"
        QuitarEtiquetaVista(vPrincipal)
        ActivarAristasTangentes(vPrincipal)

        paso = "Acotar vista principal"
        AcotarVistaBasicaPintura(sheet, tg, vPrincipal, True, True, 0.8)

        paso = "Crear vista lateral"
        Try
            Dim vLateral As DrawingView = sheet.DrawingViews.AddProjectedView(vPrincipal, pLateral, DrawingViewStyleEnum.kHiddenLineRemovedDrawingViewStyle)
            vLateral.Name = "VISTA LATERAL"
            QuitarEtiquetaVista(vLateral)
            ActivarAristasTangentes(vLateral)
            AcotarVistaBasicaPintura(sheet, tg, vLateral, True, False, 0.8)
        Catch
        End Try

        paso = "Crear vista superior"
        Try
            Dim vSuperior As DrawingView = sheet.DrawingViews.AddProjectedView(vPrincipal, pSuperior, DrawingViewStyleEnum.kHiddenLineRemovedDrawingViewStyle)
            vSuperior.Name = "VISTA SUPERIOR"
            QuitarEtiquetaVista(vSuperior)
            ActivarAristasTangentes(vSuperior)
        Catch
        End Try

        paso = "Crear isometrica"
        Try
            Dim vIso As DrawingView = CrearVistaBaseSegura(invApp, sheet, modelDoc, pIso, escalaIso, ViewOrientationTypeEnum.kIsoTopRightViewOrientation)
            If vIso IsNot Nothing Then
                vIso.Name = "ISOMETRICA"
                QuitarEtiquetaVista(vIso)
                ActivarAristasTangentes(vIso)
            End If
        Catch
        End Try

        paso = "Insertar acabado RAL"
        BorrarAcabadosRAL(sheet)
        InsertarAcabadoRALSeguro(drawingDoc, sheet, tg, nombreSimboloRAL, textoAcabado, pSimboloRAL)

        paso = "Escribir propiedades plano"
        EscribirPropiedadUsuario(drawingDoc, "ACABADO", textoAcabado)
        EscribirPropiedadUsuario(drawingDoc, "RAL", ral)
        EscribirPropiedadUsuario(drawingDoc, "CODIGO_PINTURA", codigoPintura)
        EscribirPropiedadUsuario(drawingDoc, "COD_PINTURA", codigoPintura)
        EscribirPropiedadUsuario(drawingDoc, "COD_MODELO", codigoModelo)
        EscribirPropiedadUsuario(drawingDoc, "COD_ALMACEN", codAlmacen)
        EscribirPropiedadUsuario(drawingDoc, "PROYECTO", proyecto)
        EscribirPropiedadUsuario(drawingDoc, "DISENADOR", disenador)
        EscribirPropiedadUsuario(drawingDoc, "ESCALA", escalaTexto)
        EscribirPropiedadUsuario(drawingDoc, "ARTICULO", articuloCajetin)
        EscribirPropiedadUsuario(drawingDoc, "ARTÍCULO", articuloCajetin)
        EscribirPropiedadUsuario(drawingDoc, "<Nº DE PIEZA>", codigoPiezaCajetin)
        EscribirPropiedadUsuario(drawingDoc, "Nº DE PIEZA", codigoPiezaCajetin)
        EscribirPropiedadUsuario(drawingDoc, "CENTRO", centroTrabajo)
        EscribirPropiedadUsuario(drawingDoc, "REFERENCIA CLIENTE", referenciaClienteCajetin)
        EscribirPropiedadUsuario(drawingDoc, "COD_CLIENTE", referenciaClienteCajetin)

        EscribirPropiedad(drawingDoc, "Design Tracking Properties", "Part Number", codigoPiezaCajetin)
        EscribirPropiedad(drawingDoc, "Design Tracking Properties", "Description", descripcionPlano)
        EscribirPropiedad(drawingDoc, "Design Tracking Properties", "Project", proyecto)
        EscribirPropiedad(drawingDoc, "Inventor Summary Information", "Revision Number", revision)

        paso = "Rellenar prompts cajetin"
        RellenarPromptsCajetinPintura(sheet, articuloCajetin, articuloFinalCajetin, centroTrabajo, escalaTexto, referenciaClienteCajetin, descripcionPlano, textoAcabado)

        paso = "Actualizar plano"
        drawingDoc.Update2(True)

        paso = "Guardar plano final"
        drawingDoc.Save()

        paso = "Exportar PDF"
        ExportarPDF(invApp, drawingDoc, rutaPDF)

        paso = "Exportar DWF"
        ExportarDWF(invApp, drawingDoc, rutaDWF)

        paso = "Activar plano"
        drawingDoc.Activate()

    Catch ex As Exception
        Throw New Exception("PASO: " & paso & vbCrLf & vbCrLf & "ERROR:" & vbCrLf & ex.Message)
    End Try

End Sub

'---------------------------------------------------------
' FUNCIONES DE PINTURA RAL
'---------------------------------------------------------

Function DetectarRALDesdeNombre(ByVal nombreFichero As String) As String
    If nombreFichero Is Nothing Then Return ""
    Dim n As String = nombreFichero.ToUpper()
    n = n.Replace(" ", "").Replace("-", "").Replace("_", "").Replace(".", "")
    If n.Contains("RAL9005") Then Return "9005"
    If n.Contains("R9005") Then Return "9005"
    If n.Contains("RAL7012") Then Return "7012"
    If n.Contains("R7012") Then Return "7012"
    Return ""
End Function

Function ObtenerCodigoPinturaPorRAL(ByVal ral As String, ByVal codigo9005 As String, ByVal codigo7012 As String) As String
    If ral = "9005" Then Return codigo9005
    If ral = "7012" Then Return codigo7012
    Return ""
End Function

Function ObtenerCodigoPiezaModelo(ByVal modelDoc As Document, ByVal codigoModelo As String, ByVal codAlmacen As String, ByVal nombreModelo As String) As String
    Dim codigo As String = LimpiarValorIProperty(codigoModelo)
    If codigo <> "" Then Return codigo
    codigo = LeerPropiedadUsuarioPrioritaria(modelDoc, New String() { _
        "Nº DE PIEZA", "<Nº DE PIEZA>", "NUMERO DE PIEZA", "NÚMERO DE PIEZA", _
        "PART NUMBER", "CODIGO PIEZA", "CÓDIGO PIEZA", "CODIGO", "CÓDIGO", "CORTE"})
    codigo = LimpiarValorIProperty(codigo)
    If codigo <> "" Then Return codigo
    codigo = LimpiarValorIProperty(codAlmacen)
    If codigo <> "" Then Return codigo
    Return LimpiarValorIProperty(nombreModelo)
End Function

Function ObtenerCodigoNombreFichero(ByVal modelDoc As Document, ByVal codigoModelo As String, ByVal codAlmacen As String, ByVal nombreModelo As String) As String
    Dim codigo As String = LimpiarValorIProperty(codigoModelo)
    If codigo <> "" Then Return codigo
    codigo = LimpiarValorIProperty(codAlmacen)
    If codigo <> "" Then Return codigo
    codigo = LeerPropiedadUsuarioPrioritaria(modelDoc, New String() { _
        "ARTICULO FINAL", "ARTÍCULO FINAL", "ARTICULO", "ARTÍCULO", "CODIGO", "CÓDIGO"})
    codigo = LimpiarValorIProperty(codigo)
    If codigo <> "" Then Return codigo
    Return LimpiarValorIProperty(nombreModelo)
End Function

Function ObtenerDescripcionNombreFichero(ByVal modelDoc As Document, ByVal descripcionPlano As String, ByVal nombreModelo As String) As String
    Dim desc As String = LimpiarValorIProperty(descripcionPlano)
    If desc <> "" Then Return desc
    desc = LeerPropiedadUsuarioPrioritaria(modelDoc, New String() { _
        "DESCRIPCION", "DESCRIPCIÓN", "DENOMINACION", "DENOMINACIÓN", "TITULO", "TÍTULO"})
    desc = LimpiarValorIProperty(desc)
    If desc <> "" Then Return desc
    Return LimpiarValorIProperty(nombreModelo)
End Function

Function CrearNombreBaseDesdeIProperties(ByVal codigo As String, ByVal descripcion As String) As String
    codigo = LimpiarValorIProperty(codigo)
    descripcion = LimpiarValorIProperty(descripcion)
    If codigo = "" And descripcion = "" Then Return "PLANO_PINTURA"
    If codigo = "" Then Return LimpiarNombreArchivo(descripcion)
    If descripcion = "" Then Return LimpiarNombreArchivo(codigo)
    If descripcion.ToUpper().StartsWith(codigo.ToUpper()) Then
        Return LimpiarNombreArchivo(descripcion)
    End If
    Return LimpiarNombreArchivo(codigo & " - " & descripcion)
End Function

Function LeerPropiedadUsuarioPrioritaria(ByVal doc As Document, ByVal nombres() As String) As String
    Try
        Dim ps As PropertySet = doc.PropertySets.Item("Inventor User Defined Properties")
        For Each nombre As String In nombres
            Try
                Dim p As Inventor.Property = ps.Item(nombre)
                If p IsNot Nothing AndAlso p.Value IsNot Nothing Then
                    Dim v As String = LimpiarValorIProperty(CStr(p.Value))
                    If v <> "" Then Return v
                End If
            Catch
            End Try
        Next
    Catch
    End Try
    Return ""
End Function

Function LimpiarValorIProperty(ByVal valor As String) As String
    If valor Is Nothing Then Return ""
    valor = CStr(valor).Trim()
    If valor = "" Then Return ""
    If valor.StartsWith("<") AndAlso valor.EndsWith(">") Then Return ""
    Dim vUp As String = valor.ToUpper()
    If vUp = "N/A" OrElse vUp = "NA" OrElse vUp = "SIN CODIGO" OrElse vUp = "SIN DESCRIPCION" Then Return ""
    Return valor
End Function

Function CrearPlanoMetalplakSeguro(ByVal invApp As Inventor.Application, ByVal rutaPlantilla As String) As DrawingDocument
    Try
        If rutaPlantilla IsNot Nothing Then
            rutaPlantilla = rutaPlantilla.Trim()
        Else
            rutaPlantilla = ""
        End If
        If rutaPlantilla <> "" AndAlso System.IO.File.Exists(rutaPlantilla) Then
            Dim d0 As DrawingDocument = TryCast(invApp.Documents.Add(DocumentTypeEnum.kDrawingDocumentObject, rutaPlantilla, True), DrawingDocument)
            If d0 IsNot Nothing Then Return d0
        End If
    Catch
    End Try
    Try
        Dim d As DrawingDocument = TryCast(invApp.Documents.Add(DocumentTypeEnum.kDrawingDocumentObject), DrawingDocument)
        If d IsNot Nothing Then Return d
    Catch
    End Try
    Throw New Exception("No se pudo crear el plano.")
End Function

Sub ForzarUnidadesMilimetros(ByVal drawingDoc As DrawingDocument)
    Try
        drawingDoc.UnitsOfMeasure.LengthUnits = UnitsTypeEnum.kMillimeterLengthUnits
    Catch
    End Try
End Sub

Function CrearVistaBaseSegura(ByVal invApp As Inventor.Application, ByVal sheet As Sheet, ByVal modelDoc As Document, _
                              ByVal punto As Point2d, ByVal escala As Double, ByVal orientacion As ViewOrientationTypeEnum) As DrawingView
    Try
        Return sheet.DrawingViews.AddBaseView(modelDoc, punto, escala, orientacion, DrawingViewStyleEnum.kHiddenLineRemovedDrawingViewStyle)
    Catch
    End Try
    Try
        Dim opts As NameValueMap = invApp.TransientObjects.CreateNameValueMap()
        Return sheet.DrawingViews.AddBaseView(modelDoc, punto, escala, orientacion, DrawingViewStyleEnum.kHiddenLineRemovedDrawingViewStyle, "", Nothing, opts)
    Catch
    End Try
    Return Nothing
End Function

Function CalcularPuntoAcabadoRAL(ByVal sheet As Sheet, ByVal tg As TransientGeometry) As Point2d
    Dim OFFSET_X_CM As Double = 3.2
    Dim OFFSET_Y_CM As Double = 0.55
    Try
        If sheet IsNot Nothing AndAlso sheet.TitleBlock IsNot Nothing Then
            Dim tbBox As Box2d = sheet.TitleBlock.RangeBox
            Dim x As Double = tbBox.MinPoint.X + OFFSET_X_CM
            Dim y As Double = tbBox.MaxPoint.Y + OFFSET_Y_CM
            Return tg.CreatePoint2d(x, y)
        End If
    Catch
    End Try
    Try
        Return tg.CreatePoint2d(sheet.Width * 0.18, sheet.Height * 0.21)
    Catch
        Return tg.CreatePoint2d(3, 7)
    End Try
End Function

Sub InsertarAcabadoRALSeguro(ByVal drawingDoc As DrawingDocument, ByVal sheet As Sheet, ByVal tg As TransientGeometry, _
                             ByVal nombreSimbolo As String, ByVal textoAcabado As String, ByVal punto As Point2d)
    Dim def As SketchedSymbolDefinition = BuscarDefinicionSimboloRAL(drawingDoc, nombreSimbolo)
    If def IsNot Nothing Then
        Try
            sheet.SketchedSymbols.Add(def, punto, 0, 1)
            Exit Sub
        Catch
        End Try
        For n As Integer = 1 To 6
            Try
                Dim prompts(n - 1) As String
                For i As Integer = 0 To n - 1
                    prompts(i) = ""
                Next
                sheet.SketchedSymbols.Add(def, punto, 0, 1, prompts)
                Exit Sub
            Catch
            End Try
        Next
    End If
    InsertarEtiquetaAcabadoRAL(sheet, tg, textoAcabado, punto)
End Sub

Function BuscarDefinicionSimboloRAL(ByVal drawingDoc As DrawingDocument, ByVal nombreSolicitado As String) As SketchedSymbolDefinition
    If drawingDoc Is Nothing Then Return Nothing
    If nombreSolicitado Is Nothing Then nombreSolicitado = ""
    nombreSolicitado = nombreSolicitado.Trim()
    Try
        Return drawingDoc.SketchedSymbolDefinitions.Item(nombreSolicitado)
    Catch
    End Try
    Dim solicitadoNormalizado As String = NormalizarNombreSimbolo(nombreSolicitado)
    Try
        For Each defTmp As SketchedSymbolDefinition In drawingDoc.SketchedSymbolDefinitions
            Try
                If NormalizarNombreSimbolo(defTmp.Name) = solicitadoNormalizado Then
                    Return defTmp
                End If
            Catch
            End Try
        Next
    Catch
    End Try
    Dim ral As String = ExtraerRALDesdeNombreSimbolo(nombreSolicitado)
    If ral <> "" Then
        Try
            For Each defTmp As SketchedSymbolDefinition In drawingDoc.SketchedSymbolDefinitions
                Try
                    Dim n As String = defTmp.Name.ToUpper()
                    If n.Contains("ACABADO") AndAlso n.Contains("RAL") AndAlso n.Contains(ral) Then
                        Return defTmp
                    End If
                Catch
                End Try
            Next
        Catch
        End Try
    End If
    Return Nothing
End Function

Sub InsertarEtiquetaAcabadoRAL(ByVal sheet As Sheet, ByVal tg As TransientGeometry, ByVal textoAcabado As String, ByVal punto As Point2d)
    Try
        Dim nota As GeneralNote = sheet.DrawingNotes.GeneralNotes.AddFitted(punto, textoAcabado)
        Try
            nota.FormattedText = "<StyleOverride FontSize='0.25'>" & textoAcabado & "</StyleOverride>"
        Catch
        End Try
    Catch
    End Try
End Sub

Function NormalizarNombreSimbolo(ByVal texto As String) As String
    If texto Is Nothing Then texto = ""
    texto = texto.ToUpper()
    texto = texto.Replace(" ", "").Replace("-", "").Replace("_", "").Replace(".", "")
    Return texto
End Function

Function ExtraerRALDesdeNombreSimbolo(ByVal texto As String) As String
    If texto Is Nothing Then Return ""
    Dim numeros As String = ""
    For i As Integer = 1 To texto.Length
        Dim c As String = Mid(texto, i, 1)
        If c >= "0" AndAlso c <= "9" Then
            numeros &= c
        End If
    Next
    If numeros.Length >= 4 Then
        Return numeros.Substring(numeros.Length - 4, 4)
    End If
    Return numeros
End Function

Sub BorrarAcabadosRAL(ByVal sheet As Sheet)
    Try
        For i As Integer = sheet.SketchedSymbols.Count To 1 Step -1
            Dim s As SketchedSymbol = sheet.SketchedSymbols.Item(i)
            Try
                Dim n As String = s.Definition.Name.ToUpper()
                If n.Contains("ACABADO") AndAlso n.Contains("RAL") Then
                    s.Delete()
                End If
            Catch
            End Try
        Next
    Catch
    End Try
    Try
        For i As Integer = sheet.DrawingNotes.GeneralNotes.Count To 1 Step -1
            Dim nota As GeneralNote = sheet.DrawingNotes.GeneralNotes.Item(i)
            Try
                Dim t As String = nota.Text.ToUpper()
                If t.Contains("ACABADO") AndAlso t.Contains("RAL") Then
                    nota.Delete()
                End If
            Catch
            End Try
        Next
    Catch
    End Try
End Sub

Sub AcotarVistaBasicaPintura(ByVal sheet As Sheet, ByVal tg As TransientGeometry, ByVal vista As DrawingView, _
                             ByVal acotarHorizontal As Boolean, ByVal acotarVertical As Boolean, ByVal offsetCm As Double)
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
                Dim ptTextoV As Point2d = tg.CreatePoint2d(vista.Position.X - (vista.Width / 2.0) - offsetCm, vista.Position.Y)
                sheet.DrawingDimensions.GeneralDimensions.AddLinear(ptTextoV, intentV1, intentV2, DimensionTypeEnum.kVerticalDimensionType)
            Catch
            End Try
        End If
    Catch
    End Try
End Sub

Sub RellenarPromptsCajetinPintura(ByVal sheet As Sheet, ByVal articulo As String, ByVal articuloFinal As String, _
                                  ByVal centro As String, ByVal escala As String, ByVal referenciaCliente As String, _
                                  ByVal descripcion As String, ByVal acabado As String)
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
                Dim tu As String = NormalizarTextoCampo(t)
                If EsCampoNumeroPieza(tu) Then
                    Try
                        tb.SetPromptResultText(txt, articulo)
                    Catch
                    End Try
                ElseIf tu.Contains("ARTICULOFINAL") Then
                    Try
                        tb.SetPromptResultText(txt, articuloFinal)
                    Catch
                    End Try
                ElseIf tu.Contains("ARTICULO") Then
                    Try
                        tb.SetPromptResultText(txt, articulo)
                    Catch
                    End Try
                ElseIf tu.Contains("CENTRO") Then
                    Try
                        tb.SetPromptResultText(txt, centro)
                    Catch
                    End Try
                ElseIf tu.Contains("ESCALA") Then
                    Try
                        tb.SetPromptResultText(txt, escala)
                    Catch
                    End Try
                ElseIf tu.Contains("REFERENCIACLIENTE") Or tu.Contains("CODIGOCLIENTE") Then
                    Try
                        tb.SetPromptResultText(txt, referenciaCliente)
                    Catch
                    End Try
                ElseIf tu.Contains("DESCRIPCION") Then
                    Try
                        tb.SetPromptResultText(txt, descripcion)
                    Catch
                    End Try
                ElseIf tu.Contains("ACABADO") Or tu.Contains("RAL") Then
                    Try
                        tb.SetPromptResultText(txt, acabado)
                    Catch
                    End Try
                End If
            Catch
            End Try
        Next
    Catch
    End Try
End Sub

Function EsCampoNumeroPieza(ByVal textoNormalizado As String) As Boolean
    If textoNormalizado Is Nothing Then Return False
    If textoNormalizado.Contains("NDEPIEZA") Then Return True
    If textoNormalizado.Contains("NUMERODEPIEZA") Then Return True
    If textoNormalizado.Contains("PARTNUMBER") Then Return True
    Return False
End Function

Function NormalizarTextoCampo(ByVal texto As String) As String
    If texto Is Nothing Then texto = ""
    texto = texto.ToUpper()
    texto = texto.Replace("Á", "A").Replace("É", "E").Replace("Í", "I").Replace("Ó", "O").Replace("Ú", "U")
    texto = texto.Replace("Ü", "U").Replace("Ñ", "N")
    texto = texto.Replace("º", "").Replace("ª", "").Replace("°", "")
    texto = texto.Replace(" ", "").Replace("_", "").Replace("-", "").Replace(".", "")
    texto = texto.Replace(":", "").Replace("<", "").Replace(">", "").Replace("/", "")
    Return texto
End Function

Function CalcularEscalaA4Maxima(ByVal modelDoc As Document, ByVal sheet As Sheet, ByVal escalasDisponibles() As Double) As Double
    Try
        Dim box As Box = ObtenerRangeBoxModelo(modelDoc)
        If box Is Nothing Then Return 0.2
        Dim dxCm As Double = Math.Abs(box.MaxPoint.X - box.MinPoint.X)
        Dim dyCm As Double = Math.Abs(box.MaxPoint.Y - box.MinPoint.Y)
        Dim dzCm As Double = Math.Abs(box.MaxPoint.Z - box.MinPoint.Z)
        If dxCm <= 0 Then dxCm = 1
        If dyCm <= 0 Then dyCm = 1
        If dzCm <= 0 Then dzCm = 1
        Dim dimMayor As Double = Math.Max(dxCm, Math.Max(dyCm, dzCm))
        Dim dimMedia As Double = dxCm + dyCm + dzCm - dimMayor - Math.Min(dxCm, Math.Min(dyCm, dzCm))
        Dim dimMenor As Double = Math.Min(dxCm, Math.Min(dyCm, dzCm))
        Dim anchoVistaLarga As Double = dimMayor
        Dim altoVistaLarga As Double = Math.Max(dimMedia, dimMenor)
        Dim anchoVistaCorta As Double = Math.Max(dimMedia, dimMenor)
        Dim altoVistaCorta As Double = Math.Max(dimMedia, dimMenor)
        Dim anchoHoja As Double = sheet.Width
        Dim altoHoja As Double = sheet.Height
        Dim ySuperiorCajetin As Double = 0
        Try
            If sheet.TitleBlock IsNot Nothing Then
                ySuperiorCajetin = sheet.TitleBlock.RangeBox.MaxPoint.Y
            End If
        Catch
            ySuperiorCajetin = altoHoja * 0.18
        End Try
        Dim margenX As Double = 1.6
        Dim margenSuperior As Double = 1.4
        Dim margenSobreCajetin As Double = 1.6
        Dim anchoUtil As Double = anchoHoja - (2 * margenX)
        Dim altoUtil As Double = altoHoja - ySuperiorCajetin - margenSuperior - margenSobreCajetin
        If anchoUtil <= 5 Then anchoUtil = anchoHoja * 0.86
        If altoUtil <= 5 Then altoUtil = altoHoja * 0.66
        Dim anchoZonaIzquierda As Double = anchoUtil * 0.40
        Dim anchoZonaDerecha As Double = anchoUtil * 0.62
        Dim altoZonaSuperior As Double = altoUtil * 0.45
        Dim altoZonaInferior As Double = altoUtil * 0.48
        For Each s As Double In escalasDisponibles
            Dim cabeLateral As Boolean = (anchoVistaLarga * s <= anchoZonaDerecha) AndAlso (altoVistaLarga * s <= altoZonaSuperior)
            Dim cabePrincipal As Boolean = (anchoVistaCorta * s <= anchoZonaIzquierda) AndAlso (altoVistaCorta * s <= altoZonaSuperior)
            Dim cabeSuperior As Boolean = (anchoVistaCorta * s <= anchoZonaIzquierda) AndAlso (altoVistaLarga * s <= altoZonaInferior)
            Dim cabeIso As Boolean = (anchoVistaLarga * s <= anchoZonaDerecha) AndAlso (altoVistaLarga * s <= altoZonaInferior)
            If cabeLateral AndAlso cabePrincipal AndAlso cabeSuperior AndAlso cabeIso Then
                Return s
            End If
        Next
        Return escalasDisponibles(escalasDisponibles.Length - 1)
    Catch
        Return 0.2
    End Try
End Function

Function ObtenerRangeBoxModelo(ByVal modelDoc As Document) As Box
    Try
        If modelDoc.DocumentType = DocumentTypeEnum.kAssemblyDocumentObject Then
            Dim asm As AssemblyDocument = TryCast(modelDoc, AssemblyDocument)
            If asm IsNot Nothing Then
                Return asm.ComponentDefinition.RangeBox
            End If
        ElseIf modelDoc.DocumentType = DocumentTypeEnum.kPartDocumentObject Then
            Dim p As PartDocument = TryCast(modelDoc, PartDocument)
            If p IsNot Nothing Then
                Return p.ComponentDefinition.RangeBox
            End If
        End If
    Catch
    End Try
    Return Nothing
End Function

Function FormatearEscalaFormato(ByVal escala As Double) As String
    Try
        If escala <= 0 Then Return ""
        Dim divisor As Double = 1 / escala
        Dim divisorRedondeado As Integer = CInt(Math.Round(divisor, 0))
        If divisorRedondeado < 1 Then divisorRedondeado = 1
        Return "1:" & divisorRedondeado.ToString()
    Catch
        Return ""
    End Try
End Function

Function ResolverRutaLibre(ByVal ruta As String) As String
    If Not System.IO.File.Exists(ruta) Then Return ruta
    Dim carpeta As String = System.IO.Path.GetDirectoryName(ruta)
    Dim nombre As String = System.IO.Path.GetFileNameWithoutExtension(ruta)
    Dim ext As String = System.IO.Path.GetExtension(ruta)
    For i As Integer = 1 To 99
        Dim rutaNueva As String = System.IO.Path.Combine(carpeta, nombre & "_" & i.ToString("00") & ext)
        If Not System.IO.File.Exists(rutaNueva) Then
            Return rutaNueva
        End If
    Next
    Return System.IO.Path.Combine(carpeta, nombre & "_" & Now.ToString("yyyyMMdd_HHmmss") & ext)
End Function

Sub ActivarAristasTangentes(ByVal v As DrawingView)
    Try
        v.DisplayTangentEdges = True
    Catch
    End Try
End Sub

Sub QuitarEtiquetaVista(ByVal v As DrawingView)
    Try
        v.ShowLabel = False
    Catch
    End Try
End Sub

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

Function LimpiarNombreArchivo(ByVal nombre As String) As String
    Dim invalidos() As Char = System.IO.Path.GetInvalidFileNameChars()
    For Each c As Char In invalidos
        nombre = nombre.Replace(c, "_"c)
    Next
    nombre = nombre.Replace("  ", " ").Trim()
    If nombre = "" Then nombre = "PLANO_PINTURA"
    Return nombre
End Function

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
    End Try
End Sub

Sub RegistrarPuntoExtremo(ByVal dc As DrawingCurve, ByVal intento As PointIntentEnum, ByVal p As Point2d, _
                          ByRef curvaMinX As DrawingCurve, ByRef intentoMinX As PointIntentEnum, ByRef minX As Double, _
                          ByRef curvaMaxX As DrawingCurve, ByRef intentoMaxX As PointIntentEnum, ByRef maxX As Double, _
                          ByRef curvaMinY As DrawingCurve, ByRef intentoMinY As PointIntentEnum, ByRef minY As Double, _
                          ByRef curvaMaxY As DrawingCurve, ByRef intentoMaxY As PointIntentEnum, ByRef maxY As Double)
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
' CLASE DIÁLOGO DE SELECCIÓN
'---------------------------------------------------------

Class FormularioSeleccionPlano
    Inherits System.Windows.Forms.Form

    Public PlanoSeleccionado As String = ""
    Public IncluirCodigoPlegado As Boolean = True
    Private rbPlegado As System.Windows.Forms.RadioButton
    Private rbPintura As System.Windows.Forms.RadioButton
    Private rbSoldadura As System.Windows.Forms.RadioButton
    Private rbDespiece As System.Windows.Forms.RadioButton
    Private chkCodigoPlegado As System.Windows.Forms.CheckBox
    Private lblOpciones As System.Windows.Forms.Label

    Sub New()
        Me.Text = "Seleccionar Plano a Generar"
        Me.Width = 380
        Me.Height = 420
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False

        Dim lblTitulo As New System.Windows.Forms.Label()
        lblTitulo.Text = "Elige el tipo de plano:"
        lblTitulo.Top = 15
        lblTitulo.Left = 20
        lblTitulo.Width = 300
        lblTitulo.Height = 25
        Me.Controls.Add(lblTitulo)

        rbPlegado = New System.Windows.Forms.RadioButton()
        rbPlegado.Text = "Plano de Plegado"
        rbPlegado.Top = 50
        rbPlegado.Left = 30
        rbPlegado.Width = 280
        rbPlegado.Height = 25
        rbPlegado.Checked = True
        AddHandler rbPlegado.CheckedChanged, AddressOf RadioButton_CheckedChanged
        Me.Controls.Add(rbPlegado)

        rbPintura = New System.Windows.Forms.RadioButton()
        rbPintura.Text = "Plano de Pintura RAL"
        rbPintura.Top = 85
        rbPintura.Left = 30
        rbPintura.Width = 280
        rbPintura.Height = 25
        AddHandler rbPintura.CheckedChanged, AddressOf RadioButton_CheckedChanged
        Me.Controls.Add(rbPintura)

        rbSoldadura = New System.Windows.Forms.RadioButton()
        rbSoldadura.Text = "Plano de Soldadura"
        rbSoldadura.Top = 120
        rbSoldadura.Left = 30
        rbSoldadura.Width = 280
        rbSoldadura.Height = 25
        AddHandler rbSoldadura.CheckedChanged, AddressOf RadioButton_CheckedChanged
        Me.Controls.Add(rbSoldadura)

        rbDespiece = New System.Windows.Forms.RadioButton()
        rbDespiece.Text = "Despiece Visual"
        rbDespiece.Top = 155
        rbDespiece.Left = 30
        rbDespiece.Width = 280
        rbDespiece.Height = 25
        AddHandler rbDespiece.CheckedChanged, AddressOf RadioButton_CheckedChanged
        Me.Controls.Add(rbDespiece)

        lblOpciones = New System.Windows.Forms.Label()
        lblOpciones.Text = "Opciones de Plegado:"
        lblOpciones.Top = 195
        lblOpciones.Left = 30
        lblOpciones.Width = 280
        lblOpciones.Height = 20
        lblOpciones.Visible = True
        Me.Controls.Add(lblOpciones)

        chkCodigoPlegado = New System.Windows.Forms.CheckBox()
        chkCodigoPlegado.Text = "Incluir rótulo COD PLEGADO"
        chkCodigoPlegado.Top = 220
        chkCodigoPlegado.Left = 40
        chkCodigoPlegado.Width = 280
        chkCodigoPlegado.Height = 25
        chkCodigoPlegado.Checked = True
        chkCodigoPlegado.Visible = True
        Me.Controls.Add(chkCodigoPlegado)

        Dim btnEjecutar As New System.Windows.Forms.Button()
        btnEjecutar.Text = "Ejecutar"
        btnEjecutar.Width = 90
        btnEjecutar.Height = 30
        btnEjecutar.Top = 290
        btnEjecutar.Left = 95
        btnEjecutar.DialogResult = System.Windows.Forms.DialogResult.OK
        Me.Controls.Add(btnEjecutar)
        Me.AcceptButton = btnEjecutar

        Dim btnCancelar As New System.Windows.Forms.Button()
        btnCancelar.Text = "Cancelar"
        btnCancelar.Width = 90
        btnCancelar.Height = 30
        btnCancelar.Top = 290
        btnCancelar.Left = 195
        btnCancelar.DialogResult = System.Windows.Forms.DialogResult.Cancel
        Me.Controls.Add(btnCancelar)
        Me.CancelButton = btnCancelar
    End Sub

    Private Sub RadioButton_CheckedChanged(ByVal sender As Object, ByVal e As EventArgs)
        Dim mostrarOpciones As Boolean = rbPlegado.Checked
        lblOpciones.Visible = mostrarOpciones
        chkCodigoPlegado.Visible = mostrarOpciones
    End Sub

    Protected Overrides Sub OnFormClosing(ByVal e As System.Windows.Forms.FormClosingEventArgs)
        If Me.DialogResult = System.Windows.Forms.DialogResult.OK Then
            If rbPlegado.Checked Then
                PlanoSeleccionado = "PLEGADO"
                IncluirCodigoPlegado = chkCodigoPlegado.Checked
            ElseIf rbPintura.Checked Then
                PlanoSeleccionado = "PINTURA"
            ElseIf rbSoldadura.Checked Then
                PlanoSeleccionado = "SOLDADURA"
            ElseIf rbDespiece.Checked Then
                PlanoSeleccionado = "DESPIECE"
            End If
        End If
        MyBase.OnFormClosing(e)
    End Sub
End Class
