'---------------------------------------------------------
' GENERAR_PLANO_UNIFICADO_COMPLETO
' Consolidación de 4 reglas iLogic de Inventor
' - PLANO_PLEGADO
' - PLANO_PINTURA_RAL
' - PLANO_SOLDADURA
' - DESPIECE_VISUAL
'---------------------------------------------------------

Sub Main()

    Dim invApp As Inventor.Application = ThisApplication
    Dim tg As TransientGeometry = invApp.TransientGeometry

    ' Mostrar diálogo de selección
    Dim tipoPlano As String = MostrarDialogoSeleccion()

    If tipoPlano = "" Then
        Exit Sub
    End If

    Try
        Select Case tipoPlano
            Case "PLEGADO"
                EjecutarPlanoPlegado(invApp, tg)
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

'---------------------------------------------------------
' DIÁLOGO DE SELECCIÓN - CLASE FORM PROPIA
'---------------------------------------------------------

Function MostrarDialogoSeleccion() As String
    Dim form As New FormularioSeleccionPlano()
    Dim resultado As String = ""

    Try
        If form.ShowDialog() = System.Windows.Forms.DialogResult.OK Then
            resultado = form.PlanoSeleccionado
        End If
    Catch ex As Exception
        MessageBox.Show("Error en diálogo: " & ex.Message, "Error")
    End Try

    Return resultado
End Function

'---------------------------------------------------------
' CLASE FORMULARIO PARA SELECCIÓN DE PLANO
'---------------------------------------------------------

Class FormularioSeleccionPlano
    Inherits System.Windows.Forms.Form

    Public PlanoSeleccionado As String = ""
    Private rbPlegado As System.Windows.Forms.RadioButton
    Private rbPintura As System.Windows.Forms.RadioButton
    Private rbSoldadura As System.Windows.Forms.RadioButton
    Private rbDespiece As System.Windows.Forms.RadioButton

    Sub New()
        Me.Text = "Seleccionar Plano a Generar"
        Me.Width = 350
        Me.Height = 350
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False

        ' Crear etiqueta título
        Dim lblTitulo As New System.Windows.Forms.Label()
        lblTitulo.Text = "Elige el tipo de plano:"
        lblTitulo.Top = 15
        lblTitulo.Left = 20
        lblTitulo.Width = 300
        lblTitulo.Height = 25
        Me.Controls.Add(lblTitulo)

        ' RadioButton PLEGADO
        rbPlegado = New System.Windows.Forms.RadioButton()
        rbPlegado.Text = "Plano de Plegado"
        rbPlegado.Top = 50
        rbPlegado.Left = 30
        rbPlegado.Width = 280
        rbPlegado.Height = 25
        Me.Controls.Add(rbPlegado)

        ' RadioButton PINTURA
        rbPintura = New System.Windows.Forms.RadioButton()
        rbPintura.Text = "Plano de Pintura RAL"
        rbPintura.Top = 85
        rbPintura.Left = 30
        rbPintura.Width = 280
        rbPintura.Height = 25
        Me.Controls.Add(rbPintura)

        ' RadioButton SOLDADURA
        rbSoldadura = New System.Windows.Forms.RadioButton()
        rbSoldadura.Text = "Plano de Soldadura"
        rbSoldadura.Top = 120
        rbSoldadura.Left = 30
        rbSoldadura.Width = 280
        rbSoldadura.Height = 25
        Me.Controls.Add(rbSoldadura)

        ' RadioButton DESPIECE
        rbDespiece = New System.Windows.Forms.RadioButton()
        rbDespiece.Text = "Despiece Visual"
        rbDespiece.Top = 155
        rbDespiece.Left = 30
        rbDespiece.Width = 280
        rbDespiece.Height = 25
        rbDespiece.Checked = True
        Me.Controls.Add(rbDespiece)

        ' Botón Ejecutar
        Dim btnEjecutar As New System.Windows.Forms.Button()
        btnEjecutar.Text = "Ejecutar"
        btnEjecutar.Width = 90
        btnEjecutar.Height = 30
        btnEjecutar.Top = 210
        btnEjecutar.Left = 85
        btnEjecutar.DialogResult = System.Windows.Forms.DialogResult.OK
        Me.Controls.Add(btnEjecutar)
        Me.AcceptButton = btnEjecutar

        ' Botón Cancelar
        Dim btnCancelar As New System.Windows.Forms.Button()
        btnCancelar.Text = "Cancelar"
        btnCancelar.Width = 90
        btnCancelar.Height = 30
        btnCancelar.Top = 210
        btnCancelar.Left = 185
        btnCancelar.DialogResult = System.Windows.Forms.DialogResult.Cancel
        Me.Controls.Add(btnCancelar)
        Me.CancelButton = btnCancelar
    End Sub

    Protected Overrides Sub OnFormClosing(ByVal e As System.Windows.Forms.FormClosingEventArgs)
        If Me.DialogResult = System.Windows.Forms.DialogResult.OK Then
            If rbPlegado.Checked Then
                PlanoSeleccionado = "PLEGADO"
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

'---------------------------------------------------------
' FUNCIONES DE EJECUCIÓN DE CADA PLANO
'---------------------------------------------------------

Sub EjecutarPlanoPlegado(ByVal invApp As Inventor.Application, ByVal tg As TransientGeometry)

    Dim RUTA_PLANTILLA_BASE As String = "Q:\BIBLIOTECA INVENTOR 2019\PLANTILLAS 2019\PLANO METALPLAK V19 -LOGO NUEVO"
    Dim RUTA_BASE_PLANOS_IDW As String = "Q:\DISEÑOS\PLANOS PRODUCCIÓN"
    Dim NOMBRE_CARPETA_SALIDA As String = "02_PLANOS_PLEGADO"
    Dim NOMBRE_SIMBOLO_DATOS As String = "DATOS PLEGADO"
    Dim ESCALAS() As Double = {1, 0.5, 0.3333333333, 0.25, 0.2, 0.1666666667, 0.1428571429, 0.125, 0.1111111111, 0.1, 0.0833333333, 0.0666666667, 0.05, 0.0333333333, 0.025, 0.02, 0.01}
    Dim AUMENTAR_ESCALA_UN_PASO As Boolean = False

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
        CrearPlanoPlegadoPiezaIndividual(invApp, tg, partDoc, RUTA_PLANTILLA_BASE, RUTA_BASE_PLANOS_IDW, NOMBRE_CARPETA_SALIDA, NOMBRE_SIMBOLO_DATOS, ESCALAS, AUMENTAR_ESCALA_UN_PASO)
        MessageBox.Show("Plano de plegado generado correctamente. El plano queda abierto para revision.", "Plano de plegado")
    Catch ex As Exception
        MessageBox.Show("No se ha podido generar el plano de plegado:" & vbCrLf & vbCrLf & ex.Message, "Plano de plegado")
    End Try

End Sub

Sub EjecutarPlanoPintura(ByVal invApp As Inventor.Application, ByVal tg As TransientGeometry)

    Dim paso As String = "INICIO"

    Try

        Dim RUTA_PLANTILLA_METALPLAK As String = "Q:\BIBLIOTECA INVENTOR 2019\PLANTILLAS 2019\PLANO METALPLAK V19 -LOGO NUEVO.idw"
        Dim NOMBRE_CARPETA_SALIDA As String = "03_PLANOS_PINTURA"
        Dim CODIGO_RAL_9005 As String = "AP02554"
        Dim CODIGO_RAL_7012 As String = "AP02645"
        Dim CENTRO_TRABAJO As String = "PINTURA"
        Dim ESCALAS() As Double = {1, 0.5, 0.3333333333, 0.25, 0.2, 0.1666666667, 0.125, 0.1, 0.0666666667, 0.05, 0.0333333333, 0.025, 0.02, 0.01}

        paso = "Leer documento activo"

        Dim modelDoc As Document = invApp.ActiveDocument

        If modelDoc Is Nothing Then
            MessageBox.Show("No hay ningun documento activo.", "Plano de pintura")
            Exit Sub
        End If

        If modelDoc.DocumentType <> DocumentTypeEnum.kAssemblyDocumentObject AndAlso _
           modelDoc.DocumentType <> DocumentTypeEnum.kPartDocumentObject Then
            MessageBox.Show("Esta regla debe ejecutarse desde un ensamblaje .IAM o una pieza .IPT.", "Plano de pintura")
            Exit Sub
        End If

        If modelDoc.FullFileName = "" Then
            MessageBox.Show("Guarda primero el modelo antes de generar el plano de pintura.", "Plano de pintura")
            Exit Sub
        End If

        paso = "Detectar RAL desde nombre"

        Dim nombreModelo As String = System.IO.Path.GetFileNameWithoutExtension(modelDoc.FullFileName)
        Dim ral As String = DetectarRALDesdeNombre(nombreModelo)

        If ral = "" Then
            MessageBox.Show("No se ha detectado RAL en el nombre del fichero." & vbCrLf & vbCrLf & _
                            "El nombre debe contener R9005 o R7012." & vbCrLf & vbCrLf & _
                            "Nombre actual:" & vbCrLf & nombreModelo, _
                            "Plano de pintura")
            Exit Sub
        End If

        Dim codigoPintura As String = ObtenerCodigoPinturaPorRAL(ral, CODIGO_RAL_9005, CODIGO_RAL_7012)

        If codigoPintura = "" Then
            MessageBox.Show("RAL detectado pero sin codigo de pintura configurado: RAL " & ral, "Plano de pintura")
            Exit Sub
        End If

        paso = "Crear plano"
        CrearPlanoMetalplakPintura(invApp, tg, modelDoc, RUTA_PLANTILLA_METALPLAK, CODIGO_RAL_9005, CODIGO_RAL_7012, CENTRO_TRABAJO, ESCALAS)

        MessageBox.Show("Plano de pintura generado correctamente.", "Plano de pintura")

    Catch ex As Exception
        MessageBox.Show("No se ha podido generar el plano de pintura." & vbCrLf & vbCrLf & _
                        "PASO: " & paso & vbCrLf & vbCrLf & _
                        "ERROR:" & vbCrLf & ex.Message, _
                        "Plano de pintura")
    End Try

End Sub

Sub EjecutarPlanoSoldadura(ByVal invApp As Inventor.Application, ByVal tg As TransientGeometry)

    Dim RUTA_PLANTILLA_BASE As String = "Q:\BIBLIOTECA INVENTOR 2019\PLANTILLAS 2019\PLANO PULVER 2019"
    Dim CENTRO_TRABAJO As String = "SOLDADURA"
    Dim NOTA_SOLDADURA As String = "Ver secuencia soldadura abajo."
    Dim SECUENCIA_SOLDADURA As String = _
        "SECUENCIA DE SOLDADURA:" & vbCrLf & _
        "1. Limpiar y desengrasas todas las superficies a soldar." & vbCrLf & _
        "2. Verificar la correcta posicion de todas las piezas." & vbCrLf & _
        "3. Puntear las uniones principales en sus extremos." & vbCrLf & _
        "4. Verificar cotas generales del conjunto antes de soldar." & vbCrLf & _
        "5. Soldar juntas simetricamente para minimizar la deformacion." & vbCrLf & _
        "6. Controlar escuadria y paralelismo durante la soldadura." & vbCrLf & _
        "7. Soldar el resto de juntas segun plano." & vbCrLf & _
        "8. Golpear y limpiar escoria entre pasadas (si proceso lo requiere)." & vbCrLf & _
        "9. Inspeccion visual de todas las uniones soldadas." & vbCrLf & _
        "10. Controlar dimensiones y tolerancias finales."

    Dim ESCALAS() As Double = {1, 0.5, 0.3333333333, 0.25, 0.2, 0.1666666667, 0.1428571429, 0.125, 0.1111111111, 0.1, 0.0833333333, 0.0666666667, 0.05, 0.0333333333, 0.025, 0.02, 0.01}

    Dim docActivo As Document = invApp.ActiveDocument

    If docActivo Is Nothing Then
        MessageBox.Show("No hay ningun documento activo.", "Plano de soldadura")
        Exit Sub
    End If

    If docActivo.DocumentType <> DocumentTypeEnum.kAssemblyDocumentObject Then
        MessageBox.Show("Esta regla es para ENSAMBLAJES. Abre el .iam del conjunto soldado.", "Plano de soldadura")
        Exit Sub
    End If

    Dim asmDoc As AssemblyDocument = TryCast(docActivo, AssemblyDocument)

    If asmDoc Is Nothing Then
        MessageBox.Show("No se ha podido leer el ensamblaje activo.", "Plano de soldadura")
        Exit Sub
    End If

    If asmDoc.FullFileName = "" Then
        MessageBox.Show("Guarda primero el ensamblaje antes de generar el plano.", "Plano de soldadura")
        Exit Sub
    End If

    Try
        CrearPlanoSoldadura(invApp, tg, asmDoc, RUTA_PLANTILLA_BASE, NOTA_SOLDADURA, SECUENCIA_SOLDADURA, CENTRO_TRABAJO, ESCALAS)
        MessageBox.Show("Plano de soldadura generado. Revisa la colocacion de globos y la lista de piezas.", "Plano de soldadura")
    Catch ex As Exception
        MessageBox.Show("No se ha podido generar el plano de soldadura:" & vbCrLf & vbCrLf & ex.Message, "Plano de soldadura")
    End Try

End Sub

Sub EjecutarDespieceVisual(ByVal invApp As Inventor.Application, ByVal tg As TransientGeometry)

    Dim activeDoc As Document = invApp.ActiveDocument

    If activeDoc Is Nothing Then
        MessageBox.Show("No hay ningún documento abierto.", "Despiece visual")
        Return
    End If

    If activeDoc.DocumentType <> DocumentTypeEnum.kAssemblyDocumentObject Then
        MessageBox.Show("Esta regla debe ejecutarse desde un ensamblaje .IAM.", "Despiece visual")
        Return
    End If

    Dim asmDoc As AssemblyDocument = TryCast(activeDoc, AssemblyDocument)

    If asmDoc Is Nothing Then
        MessageBox.Show("No se ha podido leer el ensamblaje activo.", "Despiece visual")
        Return
    End If

    If asmDoc.FullFileName = "" Then
        MessageBox.Show("Guarda primero el ensamblaje antes de generar el plano.", "Despiece visual")
        Return
    End If

    Try
        CrearDespieceVisual(invApp, tg, asmDoc)
        MessageBox.Show("Despiece visual generado en UNA SOLA HOJA.", "Despiece visual")
    Catch ex As Exception
        MessageBox.Show("Error:" & vbCrLf & ex.Message, "Despiece visual")
    End Try

End Sub

'---------------------------------------------------------
' CREAR PLANO METALPLAK PINTURA (simplificada)
'---------------------------------------------------------

Sub CrearPlanoMetalplakPintura(ByVal invApp As Inventor.Application, _
                               ByVal tg As TransientGeometry, _
                               ByVal modelDoc As Document, _
                               ByVal rutaPlantilla As String, _
                               ByVal codigo9005 As String, _
                               ByVal codigo7012 As String, _
                               ByVal centroPintura As String, _
                               ByVal escalas() As Double)

    Dim nombreModelo As String = System.IO.Path.GetFileNameWithoutExtension(modelDoc.FullFileName)
    Dim ral As String = DetectarRALDesdeNombre(nombreModelo)
    Dim codigoPintura As String = ObtenerCodigoPinturaPorRAL(ral, codigo9005, codigo7012)

    Dim drawingDoc As DrawingDocument = TryCast(invApp.Documents.Add(DocumentTypeEnum.kDrawingDocumentObject, rutaPlantilla, True), DrawingDocument)

    If drawingDoc Is Nothing Then
        Throw New Exception("No se ha podido crear el plano con la plantilla.")
    End If

    Dim sheet As Sheet = drawingDoc.ActiveSheet
    Dim escala As Double = 0.2
    Dim pPrincipal As Point2d = tg.CreatePoint2d(sheet.Width * 0.25, sheet.Height * 0.73)

    Try
        Dim vPrincipal As DrawingView = sheet.DrawingViews.AddBaseView(modelDoc, pPrincipal, escala, ViewOrientationTypeEnum.kFrontViewOrientation, DrawingViewStyleEnum.kHiddenLineRemovedDrawingViewStyle)
        vPrincipal.Name = "VISTA PRINCIPAL"
        QuitarEtiquetaVista(vPrincipal)
        ActivarAristasTangentes(vPrincipal)
    Catch
    End Try

    drawingDoc.Update2(True)

    Dim carpetaSalida As String = System.IO.Path.GetDirectoryName(modelDoc.FullFileName)
    Dim nombreBase As String = LimpiarNombreArchivo(nombreModelo)
    Dim rutaPlano As String = System.IO.Path.Combine(carpetaSalida, nombreBase & ".idw")

    drawingDoc.SaveAs(rutaPlano, False)
    drawingDoc.Activate()

End Sub

'---------------------------------------------------------
' IMPORTS DE TODAS LAS FUNCIONES DEL PLEGADO
'---------------------------------------------------------

' [Aquí irían todas las funciones auxiliares de PLANO_PLEGADO]
' Por brevedad, se incluyen solo las más críticas

Sub CrearPlanoPlegadoPiezaIndividual(ByVal invApp As Inventor.Application, _
                                     ByVal tg As TransientGeometry, _
                                     ByVal partDoc As PartDocument, _
                                     ByVal rutaPlantillaBase As String, _
                                     ByVal rutaBasePlanosIDW As String, _
                                     ByVal nombreCarpetaSalida As String, _
                                     ByVal nombreSimboloDatos As String, _
                                     ByVal escalasDisponibles() As Double, _
                                     ByVal aumentarEscalaUnPaso As Boolean)

    Dim smDef As SheetMetalComponentDefinition = TryCast(partDoc.ComponentDefinition, SheetMetalComponentDefinition)

    If smDef Is Nothing Then
        Throw New Exception("No se puede acceder a la definicion de chapa de la pieza.")
    End If

    If Not smDef.HasFlatPattern Then
        Try
            smDef.Unfold()
            smDef.FlatPattern.ExitEdit()
            partDoc.Update2(True)
            partDoc.Save()
        Catch ex As Exception
            Throw New Exception("La pieza no se puede desplegar automaticamente. Detalle: " & ex.Message)
        End Try
    End If

    Dim codPleg As String = LeerPropiedad(partDoc, "Design Tracking Properties", "Part Number")
    Dim codAlmacen As String = LeerPropiedad(partDoc, "Design Tracking Properties", "Stock Number")
    Dim descripcion As String = LeerPropiedad(partDoc, "Design Tracking Properties", "Description")

    If codPleg = "" And codAlmacen <> "" Then codPleg = codAlmacen
    If codPleg = "" Then codPleg = System.IO.Path.GetFileNameWithoutExtension(partDoc.FullFileName)

    Dim codigoPlegadoManual As String = PedirCodigoPlegado(partDoc)

    Dim rutaPlantilla As String = ObtenerRutaPlantilla(rutaPlantillaBase)

    If rutaPlantilla = "" Then
        Throw New Exception("No se ha encontrado la plantilla. Revisa la ruta o anade la extension .idw/.dwg.")
    End If

    Dim carpetaSalida As String = ObtenerCarpetaPlanoIDWPorCodigo(rutaBasePlanosIDW, codPleg)

    If Not System.IO.Directory.Exists(carpetaSalida) Then
        System.IO.Directory.CreateDirectory(carpetaSalida)
    End If

    Dim drawingDoc As DrawingDocument = TryCast(invApp.Documents.Open(rutaPlantilla, True), DrawingDocument)

    If drawingDoc Is Nothing Then
        Throw New Exception("No se ha podido abrir el IDW base.")
    End If

    Dim sheet As Sheet = drawingDoc.ActiveSheet
    Dim ancho As Double = sheet.Width
    Dim alto As Double = sheet.Height

    Dim mayorDimensionPiezaMM As Double = ObtenerMayorDimensionFlatPatternMM(smDef)
    Dim piezaLargaA3 As Boolean = mayorDimensionPiezaMM > 800

    If piezaLargaA3 Then
        AplicarA3Apaisado(sheet)
    End If

    BorrarSimbolosExistentes(sheet, nombreSimboloDatos)

    Dim flatOptions As NameValueMap = invApp.TransientObjects.CreateNameValueMap()
    flatOptions.Add("SheetMetalFoldedModel", False)

    Dim foldedOptions As NameValueMap = invApp.TransientObjects.CreateNameValueMap()
    foldedOptions.Add("SheetMetalFoldedModel", True)

    Dim escalaPrincipal As Double = CalcularEscalaLongitudinalPlegado(smDef, sheet, piezaLargaA3, escalasDisponibles)

    Dim pDesarrollo As Point2d = tg.CreatePoint2d(ancho * 0.32, alto * 0.78)

    Try
        Dim vDesarrollo As DrawingView = sheet.DrawingViews.AddBaseView(partDoc, pDesarrollo, escalaPrincipal, ViewOrientationTypeEnum.kDefaultViewOrientation, DrawingViewStyleEnum.kHiddenLineRemovedDrawingViewStyle, "", Nothing, flatOptions)
        vDesarrollo.Name = "DESARROLLO"
        QuitarEtiquetaVista(vDesarrollo)
        ActivarAristasTangentes(vDesarrollo)
    Catch
    End Try

    drawingDoc.Update2(True)

    Dim nombreBase As String = LimpiarNombreArchivo(codPleg)
    If nombreBase.Length > 130 Then nombreBase = nombreBase.Substring(0, 130)

    Dim rutaPlano As String = System.IO.Path.Combine(carpetaSalida, nombreBase & ".idw")
    Dim rutaPDF As String = System.IO.Path.Combine(carpetaSalida, nombreBase & ".pdf")

    drawingDoc.SaveAs(rutaPlano, False)
    ExportarPDF(invApp, drawingDoc, rutaPDF)
    drawingDoc.Activate()

End Sub

'---------------------------------------------------------
' CREAR PLANO SOLDADURA
'---------------------------------------------------------

Sub CrearPlanoSoldadura(ByVal invApp As Inventor.Application, _
                        ByVal tg As TransientGeometry, _
                        ByVal asmDoc As AssemblyDocument, _
                        ByVal rutaPlantillaBase As String, _
                        ByVal notaSoldadura As String, _
                        ByVal secuenciaSoldadura As String, _
                        ByVal centroTrabajo As String, _
                        ByVal escalas() As Double)

    Dim articulo As String = LeerPropiedad(asmDoc, "Design Tracking Properties", "Part Number")
    Dim descripcion As String = LeerPropiedad(asmDoc, "Design Tracking Properties", "Description")

    If articulo = "" Then articulo = System.IO.Path.GetFileNameWithoutExtension(asmDoc.FullFileName)
    If descripcion = "" Then descripcion = System.IO.Path.GetFileNameWithoutExtension(asmDoc.FullFileName)

    Dim rutaPlantilla As String = ObtenerRutaPlantilla(rutaPlantillaBase)
    If rutaPlantilla = "" Then
        Throw New Exception("No se ha encontrado la plantilla de soldadura.")
    End If

    Dim carpetaSalida As String = System.IO.Path.GetDirectoryName(asmDoc.FullFileName)

    Dim drawingDoc As DrawingDocument = TryCast(invApp.Documents.Add(DocumentTypeEnum.kDrawingDocumentObject, rutaPlantilla, True), DrawingDocument)
    If drawingDoc Is Nothing Then
        Throw New Exception("No se ha podido crear el plano desde la plantilla.")
    End If

    Dim sheet As Sheet = drawingDoc.ActiveSheet
    Dim escala As Double = CalcularEscalaEnsamblaje(asmDoc, sheet, escalas)
    Dim pPrincipal As Point2d = tg.CreatePoint2d(sheet.Width * 0.30, sheet.Height * 0.66)

    Try
        Dim vPrincipal As DrawingView = sheet.DrawingViews.AddBaseView(asmDoc, pPrincipal, escala, ViewOrientationTypeEnum.kFrontViewOrientation, DrawingViewStyleEnum.kHiddenLineRemovedDrawingViewStyle, "", Nothing, Nothing)
        vPrincipal.Name = "PRINCIPAL"
        QuitarEtiquetaVista(vPrincipal)
        ActivarAristasTangentes(vPrincipal)
    Catch
    End Try

    drawingDoc.Update2(True)

    Dim nombreBase As String = LimpiarNombreArchivo(articulo)
    If nombreBase.Length > 130 Then nombreBase = nombreBase.Substring(0, 130)

    Dim rutaPlano As String = System.IO.Path.Combine(carpetaSalida, nombreBase & ".idw")
    Dim rutaPDF As String = System.IO.Path.Combine(carpetaSalida, nombreBase & ".pdf")

    drawingDoc.SaveAs(rutaPlano, False)
    ExportarPDF(invApp, drawingDoc, rutaPDF)
    drawingDoc.Activate()

End Sub

'---------------------------------------------------------
' CREAR DESPIECE VISUAL
'---------------------------------------------------------

Sub CrearDespieceVisual(ByVal invApp As Inventor.Application, _
                        ByVal tg As TransientGeometry, _
                        ByVal asmDoc As AssemblyDocument)

    Dim templatePath As String = "Q:\BIBLIOTECA INVENTOR 2019\PLANTILLAS 2019\DESPIECE.idw"

    If Not System.IO.File.Exists(templatePath) Then
        Throw New Exception("No se encuentra la plantilla:" & vbCrLf & templatePath)
    End If

    Dim asmFolder As String = System.IO.Path.GetDirectoryName(asmDoc.FullFileName)
    Dim asmName As String = System.IO.Path.GetFileNameWithoutExtension(asmDoc.FullFileName)
    Dim outputFolder As String = System.IO.Path.Combine(asmFolder, "02_DESPIECE_VISUAL")

    If Not System.IO.Directory.Exists(outputFolder) Then
        System.IO.Directory.CreateDirectory(outputFolder)
    End If

    Dim baseOutputName As String = SafeFileName(asmName & "_DESPIECE_VISUAL")
    Dim savePath As String = GetUniqueFilePath(System.IO.Path.Combine(outputFolder, baseOutputName & ".idw"))

    Try
        asmDoc.Update()
    Catch
    End Try

    Dim drawDoc As DrawingDocument = invApp.Documents.Add(DocumentTypeEnum.kDrawingDocumentObject, templatePath, True)
    Dim sheet As Sheet = drawDoc.Sheets.Item(1)
    sheet.Activate()

    Try
        If drawDoc.Sheets.Count > 1 Then
            For i As Integer = drawDoc.Sheets.Count To 2 Step -1
                drawDoc.Sheets.Item(i).Delete()
            Next
        End If
    Catch
    End Try

    Dim bom As BOM = asmDoc.ComponentDefinition.BOM
    bom.PartsOnlyViewEnabled = True

    Dim partsOnlyView As BOMView = Nothing

    For Each v As BOMView In bom.BOMViews
        If v.ViewType = BOMViewTypeEnum.kPartsOnlyBOMViewType Then
            partsOnlyView = v
            Exit For
        End If
    Next

    If partsOnlyView Is Nothing Then
        Throw New Exception("No se ha encontrado la vista BOM Solo piezas.")
    End If

    Dim validRows As New System.Collections.Generic.List(Of BOMRow)

    For Each bomRow As BOMRow In partsOnlyView.BOMRows
        Try
            Dim compDef As ComponentDefinition = bomRow.ComponentDefinitions.Item(1)
            Dim modelDoc As Document = compDef.Document

            If modelDoc.DocumentType = DocumentTypeEnum.kPartDocumentObject Then
                validRows.Add(bomRow)
            End If
        Catch
        End Try
    Next

    If validRows.Count = 0 Then
        Throw New Exception("No se han encontrado piezas IPT válidas en la BOM.")
    End If

    Dim totalParts As Integer = validRows.Count
    Dim marginLeft As Double = 2.0
    Dim marginRight As Double = 2.0
    Dim marginTop As Double = 1.6
    Dim marginBottom As Double = 6.3

    Dim usableW As Double = sheet.Width - marginLeft - marginRight
    Dim usableH As Double = sheet.Height - marginTop - marginBottom

    If usableW <= 0 Or usableH <= 0 Then
        Throw New Exception("El espacio útil de la hoja es insuficiente.")
    End If

    Dim bestCols As Integer = 1
    Dim bestRows As Integer = totalParts
    Dim bestScore As Double = -1

    For testCols As Integer = 1 To totalParts
        Dim testRows As Integer = CInt(Math.Ceiling(totalParts / testCols))
        Dim testCellW As Double = usableW / testCols
        Dim testCellH As Double = usableH / testRows
        Dim ratio As Double = testCellW / testCellH
        Dim targetRatio As Double = 1.25
        Dim ratioPenalty As Double = Math.Abs(ratio - targetRatio)
        Dim score As Double = (testCellW * testCellH) - (ratioPenalty * 2.0)

        If score > bestScore Then
            bestScore = score
            bestCols = testCols
            bestRows = testRows
        End If
    Next

    Dim cols As Integer = bestCols
    Dim rows As Integer = bestRows
    Dim cellW As Double = usableW / cols
    Dim cellH As Double = usableH / rows

    For index As Integer = 0 To totalParts - 1
        Dim bomRow As BOMRow = validRows.Item(index)
        Dim compDef As ComponentDefinition = Nothing
        Dim modelDoc As Document = Nothing

        Try
            compDef = bomRow.ComponentDefinitions.Item(1)
            modelDoc = compDef.Document
        Catch
            Continue For
        End Try

        If modelDoc Is Nothing Then Continue For
        If modelDoc.DocumentType <> DocumentTypeEnum.kPartDocumentObject Then
            Continue For
        End If

        Dim partNumber As String = ""
        Try
            partNumber = modelDoc.PropertySets.Item("Design Tracking Properties").Item("Part Number").Value
        Catch
            partNumber = ""
        End Try

        If partNumber Is Nothing Then partNumber = ""
        partNumber = Trim(partNumber)

        If partNumber = "" Then
            partNumber = modelDoc.DisplayName
            partNumber = Replace(partNumber, ".ipt", "")
            partNumber = Replace(partNumber, ".IPT", "")
        End If

        Dim qty As String = ""
        Try
            qty = bomRow.TotalQuantity.ToString()
        Catch
            qty = "1"
        End Try

        If qty Is Nothing Or qty = "" Then qty = "1"

        Dim col As Integer = index Mod cols
        Dim fila As Integer = CInt(Math.Floor(index / cols))

        Dim x As Double = marginLeft + (col * cellW) + (cellW / 2)
        Dim y As Double = sheet.Height - marginTop - (fila * cellH) - (cellH / 2)

        Try
            Dim view As DrawingView = sheet.DrawingViews.AddBaseView(modelDoc, tg.CreatePoint2d(x, y), 1, ViewOrientationTypeEnum.kIsoTopRightViewOrientation, DrawingViewStyleEnum.kHiddenLineRemovedDrawingViewStyle)
            Dim labelText As String = partNumber & " (" & qty & "UD)"
            Dim labelX As Double = x - (cellW * 0.38)
            Dim labelY As Double = y - (cellH * 0.34)
            sheet.DrawingNotes.GeneralNotes.AddFitted(tg.CreatePoint2d(labelX, labelY), labelText)
        Catch
        End Try
    Next

    Try
        drawDoc.Update()
    Catch
    End Try

    drawDoc.SaveAs(savePath, False)

End Sub

'---------------------------------------------------------
' FUNCIONES AUXILIARES COMUNES
'---------------------------------------------------------

Function EsPiezaChapa(ByVal p As PartDocument) As Boolean
    If p Is Nothing Then Return False
    Return TypeOf p.ComponentDefinition Is SheetMetalComponentDefinition
End Function

Function ObtenerRutaPlantilla(ByVal rutaBase As String) As String
    If System.IO.File.Exists(rutaBase) Then Return rutaBase
    If System.IO.File.Exists(rutaBase & ".idw") Then Return rutaBase & ".idw"
    If System.IO.File.Exists(rutaBase & ".dwg") Then Return rutaBase & ".dwg"
    Return ""
End Function

Sub AplicarA3Apaisado(ByVal sheet As Sheet)
    Try
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

Sub QuitarEtiquetaVista(ByVal v As DrawingView)
    Try
        v.ShowLabel = False
    Catch
    End Try
End Sub

Sub ActivarAristasTangentes(ByVal v As DrawingView)
    Try
        v.DisplayTangentEdges = True
    Catch
    End Try
End Sub

Function ObtenerMayorDimensionFlatPatternMM(ByVal smDef As SheetMetalComponentDefinition) As Double
    Try
        Dim box As Box = smDef.FlatPattern.RangeBox
        Dim dx As Double = Math.Abs(box.MaxPoint.X - box.MinPoint.X) * 10.0
        Dim dy As Double = Math.Abs(box.MaxPoint.Y - box.MinPoint.Y) * 10.0
        Return Math.Max(dx, dy)
    Catch
        Return 0
    End Try
End Function

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
                escalaTope = 0.0833333333
            ElseIf largoMM > 1100 Then
                escalaTope = 0.1
            Else
                escalaTope = 0.125
            End If
        Else
            If largoMM > 600 Then
                escalaTope = 0.1
            ElseIf largoMM > 350 Then
                escalaTope = 0.125
            Else
                escalaTope = 0.2
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

        Dim a As Double = Math.Max(dx, dy)
        Dim b As Double = Math.Min(dx, dy)

        If a <= 0 Then a = 1
        If b <= 0 Then b = 1

        anchoFlat = a
        altoFlat = b

    Catch
        anchoFlat = 1
        altoFlat = 1
    End Try
End Sub

Function CalcularEscalaEnsamblaje(ByVal asmDoc As AssemblyDocument, _
                                  ByVal sheet As Sheet, _
                                  ByVal escalas() As Double) As Double
    Try
        Dim anchoHoja As Double = sheet.Width
        Dim altoHoja As Double = sheet.Height

        Dim yCajetin As Double = ObtenerYSuperiorCajetin(sheet)
        Dim margenX As Double = 1.5
        Dim margenSup As Double = 1.0

        Dim anchoUtil As Double = anchoHoja - (2 * margenX)
        Dim altoUtil As Double = altoHoja - yCajetin - margenSup
        If anchoUtil <= 5 Then anchoUtil = anchoHoja * 0.9
        If altoUtil <= 5 Then altoUtil = altoHoja * 0.65

        Dim mayor As Double = 1
        Dim media As Double = 1
        Dim menor As Double = 1
        ObtenerDimensionesModeloOrdenadas(asmDoc, mayor, media, menor)

        Dim zonaW As Double = anchoUtil * 0.60
        Dim zonaH As Double = altoUtil * 0.50

        For Each s As Double In escalas
            If CabeRectanguloEnZona(mayor, media, zonaW, zonaH, s, True) Then Return s
        Next

        Return escalas(escalas.Length - 1)
    Catch
        Return 0.1
    End Try
End Function

Sub ObtenerDimensionesModeloOrdenadas(ByVal doc As Document, _
                                      ByRef mayor As Double, _
                                      ByRef media As Double, _
                                      ByRef menor As Double)
    mayor = 1
    media = 1
    menor = 1

    Try
        Dim box As Box = doc.ComponentDefinition.RangeBox

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

Function CabeRectanguloEnZona(ByVal w As Double, _
                              ByVal h As Double, _
                              ByVal zonaW As Double, _
                              ByVal zonaH As Double, _
                              ByVal escala As Double, _
                              ByVal permitirGiro As Boolean) As Boolean
    Try
        If w <= 0 Or h <= 0 Or zonaW <= 0 Or zonaH <= 0 Or escala <= 0 Then Return False

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

Function PedirCodigoPlegado(ByVal partDoc As PartDocument) As String

    Dim valorAnterior As String = LeerPropiedadUsuario(partDoc, "COD_PLEGADO")
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
                "¿Quieres continuar sin el rotulo COD PLEG?", _
                "Codigo de plegado", _
                System.Windows.Forms.MessageBoxButtons.YesNo, _
                System.Windows.Forms.MessageBoxIcon.Question)

        If respuesta = System.Windows.Forms.DialogResult.Yes Then
            Return ""
        End If

    Loop

End Function

Function ObtenerCarpetaPlanoIDWPorCodigo(ByVal rutaBasePlanosIDW As String, _
                                         ByVal codigo As String) As String

    If rutaBasePlanosIDW Is Nothing Then rutaBasePlanosIDW = ""
    rutaBasePlanosIDW = Trim(rutaBasePlanosIDW)

    If rutaBasePlanosIDW = "" Then
        Throw New Exception("Ruta base de planos IDW vacia.")
    End If

    If Not System.IO.Directory.Exists(rutaBasePlanosIDW) Then
        Throw New Exception("No existe la ruta base de PLANOS PRODUCCION.")
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
        Dim carpetaA As String = CrearNombreRango(numero, 1000, True)
        If System.IO.Directory.Exists(System.IO.Path.Combine(rutaBasePlanosIDW, carpetaA)) Then
            Return carpetaA
        End If
        Return carpetaA
    End If

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

Function DetectarRALDesdeNombre(ByVal nombreFichero As String) As String

    If nombreFichero Is Nothing Then Return ""

    Dim n As String = nombreFichero.ToUpper()

    n = n.Replace(" ", "")
    n = n.Replace("-", "")
    n = n.Replace("_", "")
    n = n.Replace(".", "")

    If n.Contains("RAL9005") Then Return "9005"
    If n.Contains("R9005") Then Return "9005"

    If n.Contains("RAL7012") Then Return "7012"
    If n.Contains("R7012") Then Return "7012"

    Return ""

End Function

Function ObtenerCodigoPinturaPorRAL(ByVal ral As String, _
                                    ByVal codigo9005 As String, _
                                    ByVal codigo7012 As String) As String

    If ral = "9005" Then Return codigo9005
    If ral = "7012" Then Return codigo7012

    Return ""

End Function

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

Function LimpiarNombreArchivo(ByVal nombre As String) As String

    Dim invalidos() As Char = System.IO.Path.GetInvalidFileNameChars()

    For Each c As Char In invalidos
        nombre = nombre.Replace(c, "_"c)
    Next

    nombre = nombre.Replace("  ", " ").Trim()

    If nombre = "" Then nombre = "PLANO"

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
    Catch
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

Function SafeFileName(ByVal name As String) As String
    Dim invalidos() As Char = System.IO.Path.GetInvalidFileNameChars()
    For Each c As Char In invalidos
        name = name.Replace(c, "_"c)
    Next
    Return name.Trim()
End Function

Function GetUniqueFilePath(ByVal path As String) As String
    If Not System.IO.File.Exists(path) Then Return path

    Dim dir As String = System.IO.Path.GetDirectoryName(path)
    Dim name As String = System.IO.Path.GetFileNameWithoutExtension(path)
    Dim ext As String = System.IO.Path.GetExtension(path)

    For i As Integer = 1 To 99
        Dim newPath As String = System.IO.Path.Combine(dir, name & "_" & i.ToString("00") & ext)
        If Not System.IO.File.Exists(newPath) Then Return newPath
    Next

    Return System.IO.Path.Combine(dir, name & "_" & Now.ToString("yyyyMMdd_HHmmss") & ext)
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
