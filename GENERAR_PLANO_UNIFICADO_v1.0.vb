'=================================================================
' REGLA UNIFICADA DE GENERACION DE PLANOS METALPLAK v1.0
'=================================================================
' Unifica en una sola regla la generacion de:
'   - PLANO_PLEGADO (v10.18)
'   - PLANO_PINTURA_RAL (v1.20)
'   - PLANO_SOLDADURA (v1.0)
'   - DESPIECE_VISUAL (v1.0)
'
' USO:
'   1. Ejecutar la regla desde un documento abierto (IPT para plegado, IAM para otros)
'   2. Seleccionar tipo de plano en el dialogo
'   3. Seleccionar plantilla (Metalplak Standard o Alternative)
'   4. Para PLEGADO: opcion de generar JPG de corte automaticamente
'
' NOTA: Requiere que los modelos tengan las plantillas disponibles en:
'   - Metalplak: Q:\BIBLIOTECA INVENTOR 2019\PLANTILLAS 2019\PLANO METALPLAK V19 -LOGO NUEVO
'   - Soldadura: Q:\BIBLIOTECA INVENTOR 2019\PLANTILLAS 2019\PLANO PULVER 2019
'   - Despiece: Q:\BIBLIOTECA INVENTOR 2019\PLANTILLAS 2019\DESPIECE
'=================================================================

Sub Main()

    Dim invApp As Inventor.Application = ThisApplication

    ' Mostrar dialogo de seleccion
    Dim dlg As New FormularioSeleccionPlano()

    If dlg.ShowDialog() = System.Windows.Forms.DialogResult.OK Then

        Dim tipoPlano As String = dlg.TipoPlanoSeleccionado
        Dim plantilla As String = dlg.PlantillaSeleccionada
        Dim generarJPG As Boolean = dlg.GenerarJPG

        Try
            Select Case tipoPlano
                Case "PLEGADO"
                    GenerarPlanoPlegado(invApp, plantilla, generarJPG)
                Case "PINTURA"
                    GenerarPlanoPintura(invApp, plantilla)
                Case "SOLDADURA"
                    GenerarPlanoSoldadura(invApp, plantilla)
                Case "DESPIECE"
                    GenerarDespiece(invApp, plantilla)
            End Select
        Catch ex As Exception
            MessageBox.Show("Error al generar el plano:" & vbCrLf & vbCrLf & ex.Message, "Error")
        End Try

    End If

End Sub

'=================================================================
' DIALOGO DE SELECCION DE TIPO DE PLANO
'=================================================================

Public Class FormularioSeleccionPlano
    Inherits System.Windows.Forms.Form

    Public Property TipoPlanoSeleccionado As String = ""
    Public Property PlantillaSeleccionada As String = "Metalplak Standard"
    Public Property GenerarJPG As Boolean = False

    Public Sub New()
        Me.Text = "Generar Plano"
        Me.Width = 450
        Me.Height = 400
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False

        ' Etiqueta titulo
        Dim lblTitulo As New System.Windows.Forms.Label()
        lblTitulo.Text = "Selecciona el tipo de plano a generar:"
        lblTitulo.Left = 20
        lblTitulo.Top = 20
        lblTitulo.Width = 400
        lblTitulo.Height = 25
        Me.Controls.Add(lblTitulo)

        ' GroupBox Tipo de Plano
        Dim gbTipoPlano As New System.Windows.Forms.GroupBox()
        gbTipoPlano.Text = "Tipo de plano"
        gbTipoPlano.Left = 20
        gbTipoPlano.Top = 55
        gbTipoPlano.Width = 400
        gbTipoPlano.Height = 130

        Dim rbPlegado As New System.Windows.Forms.RadioButton()
        rbPlegado.Text = "Plegado (Chapa)"
        rbPlegado.Left = 30
        rbPlegado.Top = 30
        rbPlegado.Width = 200
        rbPlegado.Checked = True
        rbPlegado.Tag = "PLEGADO"
        AddHandler rbPlegado.CheckedChanged, Sub() ActualizarOpciones()
        gbTipoPlano.Controls.Add(rbPlegado)

        Dim rbPintura As New System.Windows.Forms.RadioButton()
        rbPintura.Text = "Pintura (RAL)"
        rbPintura.Left = 30
        rbPintura.Top = 60
        rbPintura.Width = 200
        rbPintura.Tag = "PINTURA"
        gbTipoPlano.Controls.Add(rbPintura)

        Dim rbSoldadura As New System.Windows.Forms.RadioButton()
        rbSoldadura.Text = "Soldadura"
        rbSoldadura.Left = 30
        rbSoldadura.Top = 90
        rbSoldadura.Width = 200
        rbSoldadura.Tag = "SOLDADURA"
        gbTipoPlano.Controls.Add(rbSoldadura)

        Dim rbDespiece As New System.Windows.Forms.RadioButton()
        rbDespiece.Text = "Despiece Visual"
        rbDespiece.Left = 30
        rbDespiece.Top = 120
        rbDespiece.Width = 200
        rbDespiece.Tag = "DESPIECE"
        gbTipoPlano.Controls.Add(rbDespiece)

        Me.Controls.Add(gbTipoPlano)

        ' GroupBox Plantilla
        Dim gbPlantilla As New System.Windows.Forms.GroupBox()
        gbPlantilla.Text = "Plantilla"
        gbPlantilla.Left = 20
        gbPlantilla.Top = 200
        gbPlantilla.Width = 400
        gbPlantilla.Height = 80

        Dim rbPlantilla1 As New System.Windows.Forms.RadioButton()
        rbPlantilla1.Text = "Metalplak Standard (Recomendado)"
        rbPlantilla1.Left = 30
        rbPlantilla1.Top = 30
        rbPlantilla1.Width = 300
        rbPlantilla1.Checked = True
        rbPlantilla1.Tag = "Standard"
        gbPlantilla.Controls.Add(rbPlantilla1)

        Dim rbPlantilla2 As New System.Windows.Forms.RadioButton()
        rbPlantilla2.Text = "Metalplak Alternativa"
        rbPlantilla2.Left = 30
        rbPlantilla2.Top = 60
        rbPlantilla2.Width = 300
        rbPlantilla2.Tag = "Alternative"
        gbPlantilla.Controls.Add(rbPlantilla2)

        Me.Controls.Add(gbPlantilla)

        ' CheckBox para JPG (visible solo si Plegado esta seleccionado)
        Dim chkJPG As New System.Windows.Forms.CheckBox()
        chkJPG.Text = "Generar JPG de corte automaticamente"
        chkJPG.Left = 20
        chkJPG.Top = 295
        chkJPG.Width = 400
        chkJPG.Height = 25
        chkJPG.Tag = "chkJPG"
        chkJPG.Visible = True
        Me.Controls.Add(chkJPG)

        ' Botones
        Dim btnOK As New System.Windows.Forms.Button()
        btnOK.Text = "Generar"
        btnOK.Left = 240
        btnOK.Top = 335
        btnOK.Width = 90
        btnOK.Height = 30
        AddHandler btnOK.Click, Sub(s, e) BtnOK_Click(s, e, rbPlegado, rbPintura, rbSoldadura, rbDespiece, rbPlantilla1, rbPlantilla2, chkJPG)
        Me.Controls.Add(btnOK)

        Dim btnCancel As New System.Windows.Forms.Button()
        btnCancel.Text = "Cancelar"
        btnCancel.Left = 340
        btnCancel.Top = 335
        btnCancel.Width = 80
        btnCancel.Height = 30
        btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel
        Me.Controls.Add(btnCancel)

    End Sub

    Private Sub ActualizarOpciones()
        ' Actualizacion dinamica si es necesaria
    End Sub

    Private Sub BtnOK_Click(s As Object, e As System.EventArgs,
                           rbPlegado As System.Windows.Forms.RadioButton,
                           rbPintura As System.Windows.Forms.RadioButton,
                           rbSoldadura As System.Windows.Forms.RadioButton,
                           rbDespiece As System.Windows.Forms.RadioButton,
                           rbPlantilla1 As System.Windows.Forms.RadioButton,
                           rbPlantilla2 As System.Windows.Forms.RadioButton,
                           chkJPG As System.Windows.Forms.CheckBox)

        ' Determinar tipo de plano seleccionado
        If rbPlegado.Checked Then
            TipoPlanoSeleccionado = "PLEGADO"
        ElseIf rbPintura.Checked Then
            TipoPlanoSeleccionado = "PINTURA"
        ElseIf rbSoldadura.Checked Then
            TipoPlanoSeleccionado = "SOLDADURA"
        ElseIf rbDespiece.Checked Then
            TipoPlanoSeleccionado = "DESPIECE"
        End If

        ' Determinar plantilla
        If rbPlantilla1.Checked Then
            PlantillaSeleccionada = "Metalplak Standard"
        ElseIf rbPlantilla2.Checked Then
            PlantillaSeleccionada = "Metalplak Alternative"
        End If

        ' Generar JPG solo si es Plegado y esta marcado
        GenerarJPG = (TipoPlanoSeleccionado = "PLEGADO") AndAlso chkJPG.Checked

        Me.DialogResult = System.Windows.Forms.DialogResult.OK
        Me.Close()

    End Sub

End Class

'=================================================================
' FUNCIONES DE GENERACION DE PLANOS (ROUTERS)
'=================================================================

Sub GenerarPlanoPlegado(ByVal invApp As Inventor.Application, ByVal plantilla As String, ByVal generarJPG As Boolean)

    Dim tg As TransientGeometry = invApp.TransientGeometry
    Dim RUTA_PLANTILLA_BASE As String = "Q:\BIBLIOTECA INVENTOR 2019\PLANTILLAS 2019\PLANO METALPLAK V19 -LOGO NUEVO"
    Dim RUTA_BASE_PLANOS_IDW As String = "Q:\DISEÑOS\PLANOS PRODUCCIÓN"
    Dim NOMBRE_CARPETA_SALIDA As String = "02_PLANOS_PLEGADO"
    Dim NOMBRE_SIMBOLO_DATOS As String = "DATOS PLEGADO"
    Dim ESCALAS() As Double = {1, 0.5, 0.3333333333, 0.25, 0.2, 0.1666666667, 0.1428571429, 0.125, 0.1111111111, 0.1, 0.0833333333, 0.0666666667, 0.05, 0.0333333333, 0.025, 0.02, 0.01}

    Dim docActivo As Document = invApp.ActiveDocument

    If docActivo Is Nothing Then
        Throw New Exception("No hay ningun documento activo.")
    End If

    If docActivo.DocumentType <> DocumentTypeEnum.kPartDocumentObject Then
        Throw New Exception("Plegado requiere un documento IPT (pieza de chapa). Abre el archivo .IPT.")
    End If

    Dim partDoc As PartDocument = TryCast(docActivo, PartDocument)

    If partDoc Is Nothing Then
        Throw New Exception("No se ha podido leer la pieza activa.")
    End If

    If partDoc.FullFileName = "" Then
        Throw New Exception("Guarda primero la pieza antes de generar el plano.")
    End If

    If Not EsPiezaChapa(partDoc) Then
        Throw New Exception("La pieza activa no es de chapa. Revisa que el subtipo sea Chapa.")
    End If

    CrearPlanoPlegadoPiezaIndividual(invApp, tg, partDoc, RUTA_PLANTILLA_BASE, RUTA_BASE_PLANOS_IDW, NOMBRE_CARPETA_SALIDA, NOMBRE_SIMBOLO_DATOS, ESCALAS, False)

    MessageBox.Show("Plano de plegado generado correctamente. El plano queda abierto para revision.", "Exito")

    ' TODO: Si generarJPG, ejecutar la regla de JPG crop aqui
    ' If generarJPG Then
    '     GenerarJPGCorte(invApp, invApp.ActiveDocument)
    ' End If

End Sub

Sub GenerarPlanoPintura(ByVal invApp As Inventor.Application, ByVal plantilla As String)

    Dim tg As TransientGeometry = invApp.TransientGeometry
    Dim RUTA_PLANTILLA_METALPLAK As String = "Q:\BIBLIOTECA INVENTOR 2019\PLANTILLAS 2019\PLANO METALPLAK V19 -LOGO NUEVO.idw"
    Dim NOMBRE_CARPETA_SALIDA As String = "03_PLANOS_PINTURA"
    Dim CODIGO_RAL_9005 As String = "AP02554"
    Dim CODIGO_RAL_7012 As String = "AP02645"
    Dim ESCALAS() As Double = {1, 0.5, 0.3333333333, 0.25, 0.2, 0.1666666667, 0.125, 0.1, 0.0666666667, 0.05, 0.0333333333, 0.025, 0.02, 0.01}

    Dim modelDoc As Document = invApp.ActiveDocument

    If modelDoc Is Nothing Then
        Throw New Exception("No hay ningun documento activo.")
    End If

    If modelDoc.DocumentType <> DocumentTypeEnum.kAssemblyDocumentObject AndAlso _
       modelDoc.DocumentType <> DocumentTypeEnum.kPartDocumentObject Then
        Throw New Exception("Pintura requiere un ensamblaje IAM o pieza IPT.")
    End If

    If modelDoc.FullFileName = "" Then
        Throw New Exception("Guarda primero el modelo antes de generar el plano de pintura.")
    End If

    Dim nombreModelo As String = System.IO.Path.GetFileNameWithoutExtension(modelDoc.FullFileName)
    Dim ral As String = DetectarRALDesdeNombre(nombreModelo)

    If ral = "" Then
        Throw New Exception("No se ha detectado RAL en el nombre del fichero." & vbCrLf & vbCrLf & _
                            "El nombre debe contener R9005 o R7012.")
    End If

    Dim codigoPintura As String = ObtenerCodigoPinturaPorRAL(ral, CODIGO_RAL_9005, CODIGO_RAL_7012)

    If codigoPintura = "" Then
        Throw New Exception("RAL detectado pero sin codigo de pintura configurado: RAL " & ral)
    End If

    CrearPlanoMetalplakSeguro(invApp, RUTA_PLANTILLA_METALPLAK, modelDoc, ral, codigoPintura, NOMBRE_CARPETA_SALIDA, ESCALAS)

    MessageBox.Show("Plano de pintura generado correctamente.", "Exito")

End Sub

Sub GenerarPlanoSoldadura(ByVal invApp As Inventor.Application, ByVal plantilla As String)

    Dim tg As TransientGeometry = invApp.TransientGeometry
    Dim RUTA_PLANTILLA_BASE As String = "Q:\BIBLIOTECA INVENTOR 2019\PLANTILLAS 2019\PLANO PULVER 2019"
    Dim CENTRO_TRABAJO As String = "SOLDADURA"
    Dim ESCALAS() As Double = {1, 0.5, 0.3333333333, 0.25, 0.2, 0.1666666667, 0.1428571429, 0.125, 0.1111111111, 0.1, 0.0833333333, 0.0666666667, 0.05, 0.0333333333, 0.025, 0.02, 0.01}

    Dim SECUENCIA_SOLDADURA As String = _
        "SECUENCIA DE SOLDADURA:" & vbCrLf & _
        "1. Limpiar y desengrasas todas las superficies a soldar." & vbCrLf & _
        "2. Verificar la correcta posicion de todas las piezas." & vbCrLf & _
        "3. Puntear las uniones principales en sus extremos."

    Dim docActivo As Document = invApp.ActiveDocument

    If docActivo Is Nothing Then
        Throw New Exception("No hay ningun documento activo.")
    End If

    If docActivo.DocumentType <> DocumentTypeEnum.kAssemblyDocumentObject Then
        Throw New Exception("Soldadura requiere un ensamblaje IAM.")
    End If

    Dim asmDoc As AssemblyDocument = TryCast(docActivo, AssemblyDocument)

    If asmDoc Is Nothing Then
        Throw New Exception("No se ha podido leer el ensamblaje activo.")
    End If

    If asmDoc.FullFileName = "" Then
        Throw New Exception("Guarda primero el ensamblaje antes de generar el plano.")
    End If

    CrearPlanoSoldadura(invApp, tg, asmDoc, RUTA_PLANTILLA_BASE, SECUENCIA_SOLDADURA, CENTRO_TRABAJO, ESCALAS)

    MessageBox.Show("Plano de soldadura generado. Revisa la colocacion de globos y la lista de piezas.", "Exito")

End Sub

Sub GenerarDespiece(ByVal invApp As Inventor.Application, ByVal plantilla As String)

    Dim tg As TransientGeometry = invApp.TransientGeometry
    Dim templatePath As String = "Q:\BIBLIOTECA INVENTOR 2019\PLANTILLAS 2019\DESPIECE.idw"

    Dim activeDoc As Document = invApp.ActiveDocument

    If activeDoc Is Nothing Then
        Throw New Exception("No hay ningun documento abierto.")
    End If

    If activeDoc.DocumentType <> DocumentTypeEnum.kAssemblyDocumentObject Then
        Throw New Exception("Despiece requiere un ensamblaje IAM.")
    End If

    Dim asmDoc As AssemblyDocument = TryCast(activeDoc, AssemblyDocument)

    If asmDoc Is Nothing Then
        Throw New Exception("No se ha podido leer el ensamblaje activo.")
    End If

    If asmDoc.FullFileName = "" Then
        Throw New Exception("Guarda primero el ensamblaje antes de generar el plano.")
    End If

    If Not System.IO.File.Exists(templatePath) Then
        Throw New Exception("No se encuentra la plantilla:" & vbCrLf & templatePath)
    End If

    CrearDespieceVisual(invApp, tg, asmDoc, templatePath)

    MessageBox.Show("Despiece visual generado correctamente.", "Exito")

End Sub

'=================================================================
' FUNCIONES PRINCIPALES DE PLANOS (COPIADAS DE LAS REGLAS ORIGINALES)
'=================================================================

' Aqui irian todas las funciones CrearPlanoPlegadoPiezaIndividual, CrearPlanoSoldadura, etc.
' Estas son muy grandes (miles de lineas), por lo que se han movido a archivos separados
' durante la fase de desarrollo.
'
' Para la implementacion final, copiar todas las funciones de:
' - PLANO_PLEGADO.vb (lineas 64 en adelante)
' - PLANO_PINTURA_RAL.vb (lineas 146 en adelante)
' - PLANO_SOLDADURA.vb (lineas 80 en adelante)
' - DESPIECE_VISUAL.vb (lineas 80 en adelante)

' PLACEHOLDER: Las funciones principales se agregaran en la siguiente etapa
' Sub CrearPlanoPlegadoPiezaIndividual(...)
' Sub CrearPlanoMetalplakSeguro(...)
' Sub CrearPlanoSoldadura(...)
' Sub CrearDespieceVisual(...)

'=================================================================
' FUNCIONES HELPER COMUNES
'=================================================================

Function EsPiezaChapa(ByVal partDoc As PartDocument) As Boolean
    Try
        Dim comp As SheetMetalComponentDefinition = TryCast(partDoc.ComponentDefinition, SheetMetalComponentDefinition)
        Return comp IsNot Nothing
    Catch
        Return False
    End Try
End Function

Function LeerPropiedad(ByVal doc As Document, ByVal setName As String, ByVal propName As String) As String
    Try
        If setName = "Design Tracking Properties" Then
            Return doc.PropertySets.Item("Design Tracking Properties").Item(propName).Value.ToString()
        ElseIf setName = "Inventor Summary Information" Then
            Return doc.PropertySets.Item("Inventor Summary Information").Item(propName).Value.ToString()
        ElseIf setName = "Summary Information" Then
            Return doc.PropertySets.Item("Summary Information").Item(propName).Value.ToString()
        End If
    Catch
    End Try
    Return ""
End Function

Function LeerPropiedadUsuario(ByVal partDoc As PartDocument, ByVal propName As String) As String
    Try
        Return partDoc.PropertySets.Item("User Defined Properties").Item(propName).Value.ToString()
    Catch
        Return ""
    End Try
End Function

Sub EscribirPropiedadUsuario(ByVal partDoc As PartDocument, ByVal propName As String, ByVal value As String)
    Try
        Dim userProps As PropertySet = partDoc.PropertySets.Item("User Defined Properties")
        userProps.Item(propName).Value = value
    Catch
        ' Si no existe, crear la propiedad
        Try
            Dim userProps As PropertySet = partDoc.PropertySets.Item("User Defined Properties")
            userProps.Add(value, propName)
        Catch
        End Try
    End Try
End Sub

Function ObtenerAutorActual(ByVal invApp As Inventor.Application) As String
    Try
        ' Intenta obtener del usuario actual de Inventor
        Return invApp.GeneralOptions.UserName
    Catch
        ' Si falla, usa el usuario de Windows
        Try
            Return System.Environment.UserName
        Catch
            Return "Desconocido"
        End Try
    End Try
End Function

Function ObtenerEspesorChapa(ByVal partDoc As PartDocument) As String
    Try
        Dim smDef As SheetMetalComponentDefinition = TryCast(partDoc.ComponentDefinition, SheetMetalComponentDefinition)
        If smDef Is Nothing Then Return ""

        Dim thickness As Double = smDef.SheetMetalStyle.ThicknessValue
        Return Math.Round(thickness * 10, 2).ToString() & " mm"
    Catch
        Return ""
    End Try
End Function

Function PedirCodigoPlegado(ByVal partDoc As PartDocument) As String
    Dim codigoActual As String = LeerPropiedadUsuario(partDoc, "COD_PLEGADO")
    ' NOTA: Aqui se necesitaria un InputBox con valor inicial
    ' Por ahora retorna vacio - necesita implementarse con dialogo
    Return ""
End Function

Function ObtenerRutaPlantilla(ByVal rutaBase As String) As String
    If System.IO.File.Exists(rutaBase & ".idw") Then
        Return rutaBase & ".idw"
    ElseIf System.IO.File.Exists(rutaBase & ".dwg") Then
        Return rutaBase & ".dwg"
    ElseIf System.IO.File.Exists(rutaBase) Then
        Return rutaBase
    End If
    Return ""
End Function

Function ObtenerMayorDimensionFlatPatternMM(ByVal smDef As SheetMetalComponentDefinition) As Double
    Try
        If Not smDef.HasFlatPattern Then
            smDef.Unfold()
            smDef.FlatPattern.ExitEdit()
        End If

        Dim flatPart As PartDocument = smDef.FlatPattern.ParentDocument
        Dim boundBox As Box = flatPart.ComponentDefinition.RangeBox

        Dim width As Double = (boundBox.MaxPoint.X - boundBox.MinPoint.X) * 10
        Dim height As Double = (boundBox.MaxPoint.Y - boundBox.MinPoint.Y) * 10

        Return Math.Max(width, height)
    Catch
        Return 0
    End Try
End Function

Sub AplicarA3Apaisado(ByVal sheet As Sheet)
    Try
        sheet.Orientation = SheetOrientationEnum.kLandscapeOrientation
        ' Cambiar tamaño a A3
        ' Nota: Los tamaños de hoja pueden variar segun la configuracion de Inventor
    Catch
    End Try
End Sub

Function ObtenerCarpetaPlanoIDWPorCodigo(ByVal rutaBase As String, ByVal codigo As String) As String
    ' Extraer los numeros iniciales del codigo (ej: A02871 -> 02)
    Dim codigoNumerico As String = ""
    For Each c As Char In codigo
        If Char.IsDigit(c) Then
            codigoNumerico = codigoNumerico & c
        End If
    Next

    If codigoNumerico.Length >= 2 Then
        Dim rango As String = codigoNumerico.Substring(0, 2) & "000-" & codigoNumerico.Substring(0, 2) & "999 IDW"
        Dim carpetaRango As String = System.IO.Path.Combine(rutaBase, rango)

        If System.IO.Directory.Exists(carpetaRango) Then
            Return carpetaRango
        End If
    End If

    ' Fallback: usar en la raiz
    Return rutaBase
End Function

Function DetectarRALDesdeNombre(ByVal nombre As String) As String
    If nombre.Contains("R9005") Then
        Return "9005"
    ElseIf nombre.Contains("R7012") Then
        Return "7012"
    End If
    Return ""
End Function

Function ObtenerCodigoPinturaPorRAL(ByVal ral As String, ByVal codigo9005 As String, ByVal codigo7012 As String) As String
    If ral = "9005" Then
        Return codigo9005
    ElseIf ral = "7012" Then
        Return codigo7012
    End If
    Return ""
End Function

Function ObtenerCodigoNombreFichero(ByVal modelDoc As Document, ByVal codigo As String, ByVal codAlmacen As String, ByVal nombreModelo As String) As String
    If codigo <> "" Then Return codigo
    If codAlmacen <> "" Then Return codAlmacen
    Return nombreModelo
End Function

Function ObtenerDescripcionNombreFichero(ByVal modelDoc As Document, ByVal descripcion As String, ByVal nombreModelo As String) As String
    If descripcion <> "" Then Return descripcion
    Return nombreModelo
End Function

Function CrearNombreBaseDesdeIProperties(ByVal codigo As String, ByVal descripcion As String) As String
    Return codigo & "_" & descripcion
End Function

Function ObtenerCodigoPiezaModelo(ByVal modelDoc As Document, ByVal codigo As String, ByVal codAlmacen As String, ByVal nombreModelo As String) As String
    If codigo <> "" Then Return codigo
    If codAlmacen <> "" Then Return codAlmacen
    Return nombreModelo
End Function

Function ResolverRutaLibre(ByVal rutaSugerida As String) As String
    If Not System.IO.File.Exists(rutaSugerida) Then
        Return rutaSugerida
    End If

    Dim base As String = System.IO.Path.GetFileNameWithoutExtension(rutaSugerida)
    Dim ext As String = System.IO.Path.GetExtension(rutaSugerida)
    Dim carpeta As String = System.IO.Path.GetDirectoryName(rutaSugerida)

    Dim contador As Integer = 1
    Do While System.IO.File.Exists(System.IO.Path.Combine(carpeta, base & "_" & contador & ext))
        contador += 1
    Loop

    Return System.IO.Path.Combine(carpeta, base & "_" & contador & ext)
End Function

Function SafeFileName(ByVal nombre As String) As String
    Dim invalidChars As Char() = System.IO.Path.GetInvalidFileNameChars()
    Dim resultado As String = nombre
    For Each c As Char In invalidChars
        resultado = resultado.Replace(c, "_"c)
    Next
    Return resultado
End Function

Function GetUniqueFilePath(ByVal suggestedPath As String) As String
    If Not System.IO.File.Exists(suggestedPath) Then
        Return suggestedPath
    End If

    Dim base As String = System.IO.Path.GetFileNameWithoutExtension(suggestedPath)
    Dim ext As String = System.IO.Path.GetExtension(suggestedPath)
    Dim folder As String = System.IO.Path.GetDirectoryName(suggestedPath)

    Dim counter As Integer = 1
    Do While System.IO.File.Exists(System.IO.Path.Combine(folder, base & "_" & counter & ext))
        counter += 1
    Loop

    Return System.IO.Path.Combine(folder, base & "_" & counter & ext)
End Function

' ===== FUNCIONES PLACEHOLDER =====
' Las siguientes funciones son stubs que necesitan las implementaciones reales
' de los archivos originales:

Sub CrearPlanoPlegadoPiezaIndividual(ByVal invApp As Inventor.Application, _
                                     ByVal tg As TransientGeometry, _
                                     ByVal partDoc As PartDocument, _
                                     ByVal rutaPlantillaBase As String, _
                                     ByVal rutaBasePlanosIDW As String, _
                                     ByVal nombreCarpetaSalida As String, _
                                     ByVal nombreSimboloDatos As String, _
                                     ByVal escalasDisponibles() As Double, _
                                     ByVal aumentarEscalaUnPaso As Boolean)
    ' PLACEHOLDER: Copiar desde PLANO_PLEGADO.vb lineas 64+
    Throw New Exception("CrearPlanoPlegadoPiezaIndividual no implementada - pendiente integrar desde PLANO_PLEGADO.vb")
End Sub

Sub CrearPlanoMetalplakSeguro(ByVal invApp As Inventor.Application, _
                              ByVal rutaPlantilla As String, _
                              ByVal modelDoc As Document, _
                              ByVal ral As String, _
                              ByVal codigoPintura As String, _
                              ByVal nombreCarpetaSalida As String, _
                              ByVal escalas() As Double)
    ' PLACEHOLDER: Copiar desde PLANO_PINTURA_RAL.vb lineas 146+
    Throw New Exception("CrearPlanoMetalplakSeguro no implementada - pendiente integrar desde PLANO_PINTURA_RAL.vb")
End Sub

Sub CrearPlanoSoldadura(ByVal invApp As Inventor.Application, _
                        ByVal tg As TransientGeometry, _
                        ByVal asmDoc As AssemblyDocument, _
                        ByVal rutaPlantillaBase As String, _
                        ByVal secuenciaSoldadura As String, _
                        ByVal centroTrabajo As String, _
                        ByVal escalas() As Double)
    ' PLACEHOLDER: Copiar desde PLANO_SOLDADURA.vb lineas 80+
    Throw New Exception("CrearPlanoSoldadura no implementada - pendiente integrar desde PLANO_SOLDADURA.vb")
End Sub

Sub CrearDespieceVisual(ByVal invApp As Inventor.Application, _
                        ByVal tg As TransientGeometry, _
                        ByVal asmDoc As AssemblyDocument, _
                        ByVal templatePath As String)
    ' PLACEHOLDER: Copiar desde DESPIECE_VISUAL.vb lineas 80+
    Throw New Exception("CrearDespieceVisual no implementada - pendiente integrar desde DESPIECE_VISUAL.vb")
End Sub
