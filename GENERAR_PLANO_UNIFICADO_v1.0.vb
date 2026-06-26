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
    Dim CENTRO_TRABAJO As String = "PINTURA"
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

    CrearPlanoMetalplakPintura(invApp, tg, RUTA_PLANTILLA_METALPLAK, modelDoc, ral, codigoPintura, NOMBRE_CARPETA_SALIDA, CENTRO_TRABAJO, ESCALAS)

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
        "3. Puntear las uniones principales en sus extremos." & vbCrLf & _
        "4. Verificar cotas generales del conjunto antes de soldar."

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

    CrearPlanoSoldaduraImpl(invApp, tg, asmDoc, RUTA_PLANTILLA_BASE, SECUENCIA_SOLDADURA, CENTRO_TRABAJO, ESCALAS)

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

'=================================================================
' IMPLEMENTACIONES PINTURA - COMPLETA
'=================================================================

Sub CrearPlanoMetalplakPintura(ByVal invApp As Inventor.Application, _
                                ByVal tg As TransientGeometry, _
                                ByVal rutaPlantilla As String, _
                                ByVal modelDoc As Document, _
                                ByVal ral As String, _
                                ByVal codigoPintura As String, _
                                ByVal nombreCarpetaSalida As String, _
                                ByVal centroTrabajo As String, _
                                ByVal escalas() As Double)

    Dim codigoModelo As String = LeerPropiedad(modelDoc, "Design Tracking Properties", "Part Number")
    Dim codAlmacen As String = LeerPropiedad(modelDoc, "Design Tracking Properties", "Stock Number")
    Dim descripcion As String = LeerPropiedad(modelDoc, "Design Tracking Properties", "Description")
    Dim proyecto As String = LeerPropiedad(modelDoc, "Design Tracking Properties", "Project")
    Dim disenador As String = ObtenerAutorActual(invApp)

    Dim revision As String = LeerPropiedad(modelDoc, "Inventor Summary Information", "Revision Number")
    If revision = "" Then revision = LeerPropiedad(modelDoc, "Summary Information", "Revision Number")
    If revision = "" Then revision = LeerPropiedad(modelDoc, "Design Tracking Properties", "Revision Number")
    If revision = "" Then revision = "00"

    Dim nombreModelo As String = System.IO.Path.GetFileNameWithoutExtension(modelDoc.FullFileName)
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

    Dim carpetaModelo As String = System.IO.Path.GetDirectoryName(modelDoc.FullFileName)
    If carpetaModelo.ToUpper().StartsWith("H:") Then
        carpetaModelo = "Q:" & carpetaModelo.Substring(2)
    End If

    Dim carpetaSalida As String = System.IO.Path.Combine(carpetaModelo, nombreCarpetaSalida)
    If Not System.IO.Directory.Exists(carpetaSalida) Then
        System.IO.Directory.CreateDirectory(carpetaSalida)
    End If

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

    Dim drawingDoc As DrawingDocument = CrearPlanoMetalplakSeguro(invApp, rutaPlantilla)

    If drawingDoc Is Nothing Then
        Throw New Exception("No se ha podido crear el plano con la plantilla Metalplak.")
    End If

    ForzarUnidadesMilimetros(drawingDoc)
    drawingDoc.SaveAs(rutaPlano, False)

    Dim sheet As Sheet = drawingDoc.ActiveSheet
    If sheet Is Nothing Then
        Throw New Exception("El plano no tiene hoja activa.")
    End If

    Dim ancho As Double = sheet.Width
    Dim alto As Double = sheet.Height

    Dim escalaPrincipal As Double = CalcularEscalaA4Maxima(modelDoc, sheet, escalas)
    Dim escalaIso As Double = escalaPrincipal

    If escalaPrincipal <= 0 Then escalaPrincipal = 0.2
    If escalaIso <= 0 Then escalaIso = escalaPrincipal

    Dim escalaTexto As String = FormatearEscala(escalaPrincipal)

    Dim pPrincipal As Point2d = tg.CreatePoint2d(ancho * 0.25, alto * 0.73)
    Dim pLateral As Point2d = tg.CreatePoint2d(ancho * 0.64, alto * 0.73)
    Dim pSuperior As Point2d = tg.CreatePoint2d(ancho * 0.25, alto * 0.47)
    Dim pIso As Point2d = tg.CreatePoint2d(ancho * 0.64, alto * 0.46)

    Dim pSimboloRAL As Point2d = CalcularPuntoAcabadoRAL(sheet, tg)

    Dim vPrincipal As DrawingView = CrearVistaBaseSegura(invApp, sheet, modelDoc, pPrincipal, escalaPrincipal, ViewOrientationTypeEnum.kFrontViewOrientation)

    If vPrincipal Is Nothing Then
        Throw New Exception("No se ha podido crear la vista principal.")
    End If

    vPrincipal.Name = "VISTA PRINCIPAL"
    QuitarEtiquetaVista(vPrincipal)
    ActivarAristasTangentes(vPrincipal)

    AcotarVistaBasica(sheet, tg, vPrincipal, True, True, 0.8)

    Try
        Dim vLateral As DrawingView = sheet.DrawingViews.AddProjectedView(vPrincipal, pLateral, DrawingViewStyleEnum.kHiddenLineRemovedDrawingViewStyle)
        vLateral.Name = "VISTA LATERAL"
        QuitarEtiquetaVista(vLateral)
        ActivarAristasTangentes(vLateral)
        AcotarVistaBasica(sheet, tg, vLateral, True, False, 0.8)
    Catch
    End Try

    Try
        Dim vSuperior As DrawingView = sheet.DrawingViews.AddProjectedView(vPrincipal, pSuperior, DrawingViewStyleEnum.kHiddenLineRemovedDrawingViewStyle)
        vSuperior.Name = "VISTA SUPERIOR"
        QuitarEtiquetaVista(vSuperior)
        ActivarAristasTangentes(vSuperior)
    Catch
    End Try

    Try
        Dim vIso As DrawingView = CrearVistaBaseSegura(invApp, sheet, modelDoc, pIso, escalaIso, ViewOrientationTypeEnum.kIsoTopRightViewOrientation)
        If vIso IsNot Nothing Then
            vIso.Name = "ISOMETRICA"
            QuitarEtiquetaVista(vIso)
            ActivarAristasTangentes(vIso)
        End If
    Catch
    End Try

    BorrarAcabadosRAL(sheet)
    InsertarAcabadoRALSeguro(drawingDoc, sheet, tg, nombreSimboloRAL, textoAcabado, pSimboloRAL)

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
    EscribirPropiedadUsuario(drawingDoc, "ARTICULO FINAL", articuloFinalCajetin)
    EscribirPropiedadUsuario(drawingDoc, "ARTÍCULO FINAL", articuloFinalCajetin)
    EscribirPropiedadUsuario(drawingDoc, "ARTICULO_FINAL", articuloFinalCajetin)
    EscribirPropiedadUsuario(drawingDoc, "ARTÍCULO_FINAL", articuloFinalCajetin)
    EscribirPropiedadUsuario(drawingDoc, "CODIGO_PIEZA_CAJETIN", codigoPiezaCajetin)
    EscribirPropiedadUsuario(drawingDoc, "<Nº DE PIEZA>", codigoPiezaCajetin)
    EscribirPropiedadUsuario(drawingDoc, "Nº DE PIEZA", codigoPiezaCajetin)
    EscribirPropiedadUsuario(drawingDoc, "NUMERO DE PIEZA", codigoPiezaCajetin)
    EscribirPropiedadUsuario(drawingDoc, "CENTRO", centroTrabajo)
    EscribirPropiedadUsuario(drawingDoc, "REFERENCIA CLIENTE", referenciaClienteCajetin)
    EscribirPropiedadUsuario(drawingDoc, "REFERENCIA_CLIENTE", referenciaClienteCajetin)
    EscribirPropiedadUsuario(drawingDoc, "CODIGO CLIENTE", referenciaClienteCajetin)
    EscribirPropiedadUsuario(drawingDoc, "CÓDIGO CLIENTE", referenciaClienteCajetin)
    EscribirPropiedadUsuario(drawingDoc, "COD_CLIENTE", referenciaClienteCajetin)

    EscribirPropiedad(drawingDoc, "Design Tracking Properties", "Part Number", codigoPiezaCajetin)
    EscribirPropiedad(drawingDoc, "Design Tracking Properties", "Stock Number", codigoPiezaCajetin)
    EscribirPropiedad(drawingDoc, "Design Tracking Properties", "Description", descripcionPlano)
    EscribirPropiedad(drawingDoc, "Design Tracking Properties", "Project", proyecto)
    EscribirPropiedad(drawingDoc, "Design Tracking Properties", "Designer", disenador)
    EscribirPropiedad(drawingDoc, "Inventor Summary Information", "Revision Number", revision)
    EscribirPropiedad(drawingDoc, "Summary Information", "Revision Number", revision)

    RellenarPromptsCajetinPintura(sheet, articuloCajetin, articuloFinalCajetin, centroTrabajo, escalaTexto, referenciaClienteCajetin, descripcionPlano, textoAcabado)

    drawingDoc.Update2(True)
    drawingDoc.Save()

    ExportarPDF(invApp, drawingDoc, rutaPDF)
    ExportarDWF(invApp, drawingDoc, rutaDWF)

    drawingDoc.Activate()

End Sub

'=================================================================
' IMPLEMENTACIONES PRINCIPALES - PLEGADO
'=================================================================

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

    If Not smDef.HasFlatPattern Then
        Throw New Exception("La pieza sigue sin tener desarrollo plano despues de intentar desplegarla.")
    End If

    Dim codPleg As String = LeerPropiedad(partDoc, "Design Tracking Properties", "Part Number")
    Dim codAlmacen As String = LeerPropiedad(partDoc, "Design Tracking Properties", "Stock Number")
    Dim descripcion As String = LeerPropiedad(partDoc, "Design Tracking Properties", "Description")
    Dim proyecto As String = LeerPropiedad(partDoc, "Design Tracking Properties", "Project")
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

    If codPleg = "" And codAlmacen <> "" Then codPleg = codAlmacen
    If codPleg = "" Then codPleg = System.IO.Path.GetFileNameWithoutExtension(partDoc.FullFileName)
    If codCorte = "" Then codCorte = codAlmacen
    If codCorte = "" Then codCorte = codPleg
    If espesorTexto = "" Then espesorTexto = ObtenerEspesorChapa(partDoc)
    If descripcion = "" Then descripcion = System.IO.Path.GetFileNameWithoutExtension(partDoc.FullFileName)

    Dim codigoPlegadoManual As String = PedirCodigoPlegado(partDoc)

    If codigoPlegadoManual <> "" Then
        EscribirPropiedadUsuario(partDoc, "COD_PLEGADO", codigoPlegadoManual)
    End If

    Dim rutaPlantilla As String = ObtenerRutaPlantilla(rutaPlantillaBase)

    If rutaPlantilla = "" Then
        Throw New Exception("No se ha encontrado la plantilla. Revisa la ruta o anade la extension .idw/.dwg.")
    End If

    If Not System.IO.File.Exists(rutaPlantilla) Then
        Throw New Exception("No existe la plantilla: " & rutaPlantilla)
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

    Dim foldedOptions As NameValueMap = invApp.TransientObjects.CreateNameValueMap()
    foldedOptions.Add("SheetMetalFoldedModel", True)

    Dim escalaPrincipal As Double = CalcularEscalaLongitudinalPlegado(smDef, sheet, piezaLargaA3, escalasDisponibles)
    Dim escalaDesarrollo As Double = escalaPrincipal
    Dim escalaIso As Double = escalaPrincipal
    Dim escalaExtremo As Double = CalcularEscalaDetalleExtremo(partDoc, sheet, escalasDisponibles)

    Dim orientacionPrincipal As ViewOrientationTypeEnum
    Dim orientacionSecundaria As ViewOrientationTypeEnum
    Dim orientacionExtremo As ViewOrientationTypeEnum
    ObtenerOrientacionesPiezaLarga(partDoc, orientacionPrincipal, orientacionSecundaria, orientacionExtremo)

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

        Dim margenIzq As Double = 2.2
        Dim margenDer As Double = 2.0
        Dim margenSup As Double = 1.6
        Dim sep As Double = 1.2

        Dim yCajetinSup As Double = ObtenerYSuperiorCajetin(sheet)
        Dim yZonaInferior As Double = yCajetinSup + 1.2

        pDesarrollo = tg.CreatePoint2d(ancho * 0.32, alto * 0.78)

        Dim vDesarrollo As DrawingView = sheet.DrawingViews.AddBaseView( _
            partDoc, _
            pDesarrollo, _
            escalaDesarrollo, _
            ViewOrientationTypeEnum.kDefaultViewOrientation, _
            DrawingViewStyleEnum.kHiddenLineRemovedDrawingViewStyle, _
            "", _
            Nothing, _
            flatOptions)

        vDesarrollo.Name = "DESARROLLO"
        QuitarEtiquetaVista(vDesarrollo)
        ActivarAristasTangentes(vDesarrollo)
        GirarVistaHorizontalSiNecesario(vDesarrollo)

        Dim xCentroDes As Double = margenIzq + (vDesarrollo.Width / 2.0)
        Dim yCentroDes As Double = alto - margenSup - (vDesarrollo.Height / 2.0)
        vDesarrollo.Position = tg.CreatePoint2d(xCentroDes, yCentroDes)

        AcotarVistaBasica(sheet, tg, vDesarrollo, True, True, 0.8)

        Dim xSimbolo As Double = ancho - margenDer - 3.0
        Dim ySimbolo As Double = alto - margenSup - 1.0

        If xSimbolo > ancho - margenDer - 3.0 Then
            xSimbolo = ancho - margenDer - 3.0
        End If

        If ySimbolo > alto - margenSup Then
            ySimbolo = alto - margenSup - 0.5
        End If

        pSimbolo = tg.CreatePoint2d(xSimbolo, ySimbolo)

        InsertarSimboloDatosPlegado(drawingDoc, sheet, nombreSimboloDatos, pSimbolo, matriz, codCorte, espesorTexto)

        If codigoPlegadoManual <> "" Then
            Dim pCodigoPlegado As Point2d = tg.CreatePoint2d(pSimbolo.X, pSimbolo.Y + 2.2)
            InsertarRotuloCodigoPlegado(sheet, tg, pCodigoPlegado, codigoPlegadoManual)
        End If

        Dim yDebajoDesarrollo As Double = vDesarrollo.Position.Y - (vDesarrollo.Height / 2.0) - 1.4
        Dim altoZonaInferior As Double = yDebajoDesarrollo - yZonaInferior

        If altoZonaInferior < 8 Then
            yDebajoDesarrollo = alto * 0.56
            altoZonaInferior = yDebajoDesarrollo - yZonaInferior
        End If

        pPrincipal = tg.CreatePoint2d(ancho * 0.24, yDebajoDesarrollo - 1.0)

        Dim vPrincipal As DrawingView = sheet.DrawingViews.AddBaseView( _
            partDoc, _
            pPrincipal, _
            escalaPrincipal, _
            orientacionPrincipal, _
            DrawingViewStyleEnum.kHiddenLineRemovedDrawingViewStyle, _
            "", _
            Nothing, _
            foldedOptions)

        vPrincipal.Name = "VISTA PRINCIPAL"
        QuitarEtiquetaVista(vPrincipal)
        ActivarAristasTangentes(vPrincipal)
        GirarVistaHorizontalSiNecesario(vPrincipal)

        Dim xCentroPrincipal As Double = margenIzq + (vPrincipal.Width / 2.0)
        Dim yCentroPrincipal As Double = yDebajoDesarrollo - (vPrincipal.Height / 2.0)
        vPrincipal.Position = tg.CreatePoint2d(xCentroPrincipal, yCentroPrincipal)

        AcotarVistaBasica(sheet, tg, vPrincipal, True, True, 0.8)

        Dim vLateral As DrawingView = Nothing

        Try
            pLateral = tg.CreatePoint2d(ancho * 0.87, alto * 0.49)

            vLateral = sheet.DrawingViews.AddBaseView( _
                partDoc, _
                pLateral, _
                escalaExtremo, _
                orientacionExtremo, _
                DrawingViewStyleEnum.kHiddenLineRemovedDrawingViewStyle, _
                "", _
                Nothing, _
                foldedOptions)

            vLateral.Name = "DETALLE A"
            MostrarEtiquetaVista(vLateral)
            ActivarAristasTangentes(vLateral)

            vLateral.Position = pLateral

            AcotarVistaBasica(sheet, tg, vLateral, True, True, 0.8, True)
            AcotarPlegados90ExteriorExteriorEnSeccion(sheet, tg, vLateral, 2.0, 0.8, 5.0, 1.0, 0.8, 14)

        Catch
        End Try

        Try
            Dim ySuperiorVista As Double = vPrincipal.Position.Y - (vPrincipal.Height / 2.0) - 1.2

            Dim vSuperior As DrawingView = sheet.DrawingViews.AddBaseView( _
                partDoc, _
                tg.CreatePoint2d(vPrincipal.Position.X, ySuperiorVista), _
                escalaPrincipal, _
                orientacionSecundaria, _
                DrawingViewStyleEnum.kHiddenLineRemovedDrawingViewStyle, _
                "", _
                Nothing, _
                foldedOptions)

            vSuperior.Name = "VISTA SUPERIOR"
            QuitarEtiquetaVista(vSuperior)
            ActivarAristasTangentes(vSuperior)
            GirarVistaHorizontalSiNecesario(vSuperior)

            Dim yCentroSuperior As Double = ySuperiorVista - (vSuperior.Height / 2.0)
            vSuperior.Position = tg.CreatePoint2d(vPrincipal.Position.X, yCentroSuperior)

            AcotarVistaBasica(sheet, tg, vSuperior, False, True, 0.8)

        Catch
        End Try

        Try
            pIso = tg.CreatePoint2d(ancho * 0.23, 5.0)

            Dim vIso As DrawingView = sheet.DrawingViews.AddBaseView( _
                partDoc, _
                pIso, _
                escalaIso, _
                ViewOrientationTypeEnum.kIsoTopRightViewOrientation, _
                DrawingViewStyleEnum.kHiddenLineRemovedDrawingViewStyle, _
                "", _
                Nothing, _
                foldedOptions)

            vIso.Name = "ISOMETRICA"
            QuitarEtiquetaVista(vIso)
            ActivarAristasTangentes(vIso)
            GirarVistaHorizontalSiNecesario(vIso)
            vIso.Position = tg.CreatePoint2d(ancho * 0.23, 1.2 + (vIso.Height / 2.0))

            Dim vIsoOpuesta As DrawingView = sheet.DrawingViews.AddBaseView( _
                partDoc, _
                tg.CreatePoint2d(ancho * 0.47, 4.5), _
                escalaIso, _
                ViewOrientationTypeEnum.kIsoBottomLeftViewOrientation, _
                DrawingViewStyleEnum.kHiddenLineRemovedDrawingViewStyle, _
                "", _
                Nothing, _
                foldedOptions)

            vIsoOpuesta.Name = "ISOMETRICA OPUESTA"
            QuitarEtiquetaVista(vIsoOpuesta)
            ActivarAristasTangentes(vIsoOpuesta)
            GirarVistaHorizontalSiNecesario(vIsoOpuesta)
            vIsoOpuesta.Position = tg.CreatePoint2d(ancho * 0.47, 0.8 + (vIsoOpuesta.Height / 2.0))

        Catch
        End Try

    Else

        Dim yCajetinSupA4 As Double = ObtenerYSuperiorCajetin(sheet)

        Dim xColumnaIzquierda As Double = ancho * 0.30
        Dim xColumnaDerecha As Double = ancho * 0.73

        Dim yFilaSuperior As Double = alto * 0.835
        Dim yFilaCentral As Double = alto * 0.585
        Dim yFilaInferior As Double = yCajetinSupA4 + ((alto * 0.43 - yCajetinSupA4) / 2.0)

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

        Dim orientacionPrincipalA4 As ViewOrientationTypeEnum = ViewOrientationTypeEnum.kFrontViewOrientation
        Dim orientacionLateralA4 As ViewOrientationTypeEnum = ViewOrientationTypeEnum.kRightViewOrientation
        Dim orientacionSuperiorA4 As ViewOrientationTypeEnum = ViewOrientationTypeEnum.kTopViewOrientation

        Dim escalaProvisional As Double = 0.2

        Dim vPrincipal As DrawingView = sheet.DrawingViews.AddBaseView( _
            partDoc, _
            tg.CreatePoint2d(xColumnaIzquierda, yFilaSuperior), _
            escalaProvisional, _
            orientacionPrincipalA4, _
            DrawingViewStyleEnum.kHiddenLineRemovedDrawingViewStyle, _
            "", _
            Nothing, _
            foldedOptions)

        vPrincipal.Name = "VISTA PRINCIPAL"
        QuitarEtiquetaVista(vPrincipal)
        ActivarAristasTangentes(vPrincipal)

        Dim vLateral As DrawingView = sheet.DrawingViews.AddBaseView( _
            partDoc, _
            tg.CreatePoint2d(xColumnaDerecha, yFilaSuperior), _
            escalaProvisional, _
            orientacionLateralA4, _
            DrawingViewStyleEnum.kHiddenLineRemovedDrawingViewStyle, _
            "", _
            Nothing, _
            foldedOptions)

        vLateral.Name = "VISTA LATERAL"
        QuitarEtiquetaVista(vLateral)
        ActivarAristasTangentes(vLateral)

        Dim vSuperior As DrawingView = sheet.DrawingViews.AddBaseView( _
            partDoc, _
            tg.CreatePoint2d(xColumnaIzquierda, yFilaCentral), _
            escalaProvisional, _
            orientacionSuperiorA4, _
            DrawingViewStyleEnum.kHiddenLineRemovedDrawingViewStyle, _
            "", _
            Nothing, _
            foldedOptions)

        vSuperior.Name = "VISTA SUPERIOR"
        QuitarEtiquetaVista(vSuperior)
        ActivarAristasTangentes(vSuperior)

        Dim vIso As DrawingView = sheet.DrawingViews.AddBaseView( _
            partDoc, _
            tg.CreatePoint2d(xColumnaDerecha, yFilaCentral), _
            escalaProvisional, _
            ViewOrientationTypeEnum.kIsoTopRightViewOrientation, _
            DrawingViewStyleEnum.kHiddenLineRemovedDrawingViewStyle, _
            "", _
            Nothing, _
            foldedOptions)

        vIso.Name = "ISOMETRICA"
        QuitarEtiquetaVista(vIso)
        ActivarAristasTangentes(vIso)

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

        Dim escalaPrincipalCalculada As Double = CalcularEscalaComunVistasCreadas( _
            vPrincipal, zonaPrincipalW, zonaPrincipalH, _
            vLateral, zonaLateralW, zonaLateralH, _
            vSuperior, zonaSuperiorW, zonaSuperiorH, _
            escalasDisponibles)

        Dim escalaIsoCalculada As Double = CalcularEscalaMaximaVistaCreada( _
            vIso, zonaIsoW, zonaIsoH, escalasDisponibles)

        Dim escalaDesarrolloCalculada As Double = CalcularEscalaMaximaVistaCreada( _
            vDesarrollo, zonaDesarrolloW, zonaDesarrolloH, escalasDisponibles)

        If escalaIsoCalculada > escalaPrincipalCalculada Then
            escalaIsoCalculada = escalaPrincipalCalculada
        End If

        escalaPrincipal = escalaPrincipalCalculada
        escalaIso = escalaIsoCalculada
        escalaDesarrollo = escalaDesarrolloCalculada
        escalaTexto = FormatearEscala(escalaPrincipal)

        vPrincipal.Scale = escalaPrincipal
        vLateral.Scale = escalaPrincipal
        vSuperior.Scale = escalaPrincipal
        vIso.Scale = escalaIso
        vDesarrollo.Scale = escalaDesarrollo

        drawingDoc.Update2(True)

        vPrincipal.Position = tg.CreatePoint2d(xColumnaIzquierda, yFilaSuperior)
        vLateral.Position = tg.CreatePoint2d(xColumnaDerecha, yFilaSuperior)
        vSuperior.Position = tg.CreatePoint2d(xColumnaIzquierda, yFilaCentral)
        vIso.Position = tg.CreatePoint2d(xColumnaDerecha, yFilaCentral - 0.1)
        vDesarrollo.Position = tg.CreatePoint2d(xColumnaIzquierda, yFilaInferior)

        AcotarVistaBasica(sheet, tg, vPrincipal, True, True, 0.75)
        AcotarVistaBasica(sheet, tg, vLateral, True, True, 0.75)
        AcotarVistaBasica(sheet, tg, vSuperior, False, True, 0.75)
        AcotarVistaBasica(sheet, tg, vDesarrollo, True, True, 0.75)

        Dim xSimboloA4 As Double = Math.Min(ancho - 3.3, vDesarrollo.Position.X + (vDesarrollo.Width / 2.0) + 3.2)
        Dim ySimboloA4 As Double = vDesarrollo.Position.Y + 0.2

        pSimbolo = tg.CreatePoint2d(xSimboloA4, ySimboloA4)

        InsertarSimboloDatosPlegado(drawingDoc, sheet, nombreSimboloDatos, pSimbolo, matriz, codCorte, espesorTexto)

        If codigoPlegadoManual <> "" Then
            Dim pCodigoPlegadoA4 As Point2d = tg.CreatePoint2d(pSimbolo.X, pSimbolo.Y + 2.2)
            InsertarRotuloCodigoPlegado(sheet, tg, pCodigoPlegadoA4, codigoPlegadoManual)
        End If

    End If

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

    RellenarPromptsCajetin(sheet, codPleg, centroTrabajo, escalaTexto, referenciaCliente)

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

    Dim nombreBase As String = LimpiarNombreArchivo(codPleg)
    If nombreBase.Length > 130 Then nombreBase = nombreBase.Substring(0, 130)

    Dim extensionPlano As String = System.IO.Path.GetExtension(rutaPlantilla).ToLower()
    If extensionPlano <> ".idw" And extensionPlano <> ".dwg" Then extensionPlano = ".idw"

    Dim rutaPlano As String = System.IO.Path.Combine(carpetaSalida, nombreBase & extensionPlano)
    Dim rutaPDF As String = System.IO.Path.Combine(carpetaSalida, nombreBase & ".pdf")
    Dim rutaDWF As String = System.IO.Path.Combine(carpetaSalida, nombreBase & ".dwf")

    drawingDoc.SaveAs(rutaPlano, False)
    ExportarPDF(invApp, drawingDoc, rutaPDF)
    ExportarDWF(invApp, drawingDoc, rutaDWF)

    drawingDoc.Activate()

End Sub
'=================================================================
' IMPLEMENTACIONES SOLDADURA - COMPLETA
'=================================================================

Sub CrearPlanoSoldaduraImpl(ByVal invApp As Inventor.Application, _
                           ByVal tg As TransientGeometry, _
                           ByVal asmDoc As AssemblyDocument, _
                           ByVal rutaPlantillaBase As String, _
                           ByVal secuenciaSoldadura As String, _
                           ByVal centroTrabajo As String, _
                           ByVal escalas() As Double)

    Dim articulo As String = LeerPropiedad(asmDoc, "Design Tracking Properties", "Part Number")
    Dim descripcion As String = LeerPropiedad(asmDoc, "Design Tracking Properties", "Description")
    Dim proyecto As String = LeerPropiedad(asmDoc, "Design Tracking Properties", "Project")
    Dim disenador As String = LeerPropiedad(asmDoc, "Design Tracking Properties", "Designer")
    Dim material As String = LeerPropiedad(asmDoc, "Design Tracking Properties", "Material")
    Dim revision As String = LeerPropiedad(asmDoc, "Inventor Summary Information", "Revision Number")
    If revision = "" Then revision = LeerPropiedad(asmDoc, "Summary Information", "Revision Number")

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

    Dim mayorDimMM As Double = ObtenerMayorDimensionModeloMM(asmDoc)
    If mayorDimMM > 800 Then
        AplicarA3Apaisado(sheet)
    End If

    Dim ancho As Double = sheet.Width
    Dim alto As Double = sheet.Height

    Dim escala As Double = CalcularEscalaEnsamblaje(asmDoc, sheet, escalas)
    Dim escalaTexto As String = FormatearEscala(escala)

    Dim pPrincipal As Point2d = tg.CreatePoint2d(ancho * 0.30, alto * 0.66)

    Dim vPrincipal As DrawingView = sheet.DrawingViews.AddBaseView( _
        asmDoc, pPrincipal, escala, _
        ViewOrientationTypeEnum.kFrontViewOrientation, _
        DrawingViewStyleEnum.kHiddenLineRemovedDrawingViewStyle, _
        "", Nothing, Nothing)

    vPrincipal.Name = "PRINCIPAL"
    QuitarEtiquetaVista(vPrincipal)
    ActivarAristasTangentes(vPrincipal)

    Try
        Dim pSup As Point2d = tg.CreatePoint2d(vPrincipal.Position.X, vPrincipal.Position.Y - (vPrincipal.Height / 2.0) - 4.0)
        Dim vSup As DrawingView = sheet.DrawingViews.AddProjectedView(vPrincipal, pSup, DrawingViewStyleEnum.kHiddenLineRemovedDrawingViewStyle)
        vSup.Name = "SUPERIOR"
        QuitarEtiquetaVista(vSup)
        ActivarAristasTangentes(vSup)
    Catch
    End Try

    Try
        Dim pLat As Point2d = tg.CreatePoint2d(vPrincipal.Position.X + (vPrincipal.Width / 2.0) + 5.0, vPrincipal.Position.Y)
        Dim vLat As DrawingView = sheet.DrawingViews.AddProjectedView(vPrincipal, pLat, DrawingViewStyleEnum.kHiddenLineRemovedDrawingViewStyle)
        vLat.Name = "LATERAL"
        QuitarEtiquetaVista(vLat)
        ActivarAristasTangentes(vLat)
    Catch
    End Try

    Dim vIso As DrawingView = Nothing
    Try
        Dim pIso As Point2d = tg.CreatePoint2d(ancho * 0.72, alto * 0.32)
        vIso = sheet.DrawingViews.AddBaseView( _
            asmDoc, pIso, escala, _
            ViewOrientationTypeEnum.kIsoTopRightViewOrientation, _
            DrawingViewStyleEnum.kHiddenLineRemovedDrawingViewStyle, _
            "", Nothing, Nothing)
        vIso.Name = "ISOMETRICA"
        QuitarEtiquetaVista(vIso)
        ActivarAristasTangentes(vIso)
    Catch
    End Try

    drawingDoc.Update2(True)

    Try
        Dim pBOM As Point2d = tg.CreatePoint2d(ancho * 0.72, alto * 0.95)
        Dim oPL As PartsList = sheet.PartsLists.Add(vPrincipal, pBOM)
    Catch
    End Try

    Try
        Dim vGlobos As DrawingView = vIso
        If vGlobos Is Nothing Then vGlobos = vPrincipal
        PonerGlobosAutomaticos(invApp, tg, sheet, vGlobos, asmDoc)
    Catch
    End Try

    Try
        Dim pNota As Point2d = tg.CreatePoint2d(1.5, alto - 0.8)
        sheet.DrawingNotes.GeneralNotes.AddFitted(pNota, "SOLDADURA: Revisar especificaciones de proceso.")
    Catch
    End Try

    Try
        PonerSecuenciaSoldadura(sheet, tg, secuenciaSoldadura)
    Catch
    End Try

    EscribirPropiedadUsuario(drawingDoc, "ARTICULO", articulo)
    EscribirPropiedadUsuario(drawingDoc, "ARTÍCULO", articulo)
    EscribirPropiedadUsuario(drawingDoc, "DESCRIPCION", descripcion)
    EscribirPropiedadUsuario(drawingDoc, "DESCRIPCIÓN", descripcion)
    EscribirPropiedadUsuario(drawingDoc, "ESCALA", escalaTexto)
    EscribirPropiedadUsuario(drawingDoc, "CENTRO", centroTrabajo)
    EscribirPropiedadUsuario(drawingDoc, "MATERIAL", material)
    EscribirPropiedadUsuario(drawingDoc, "PROYECTO", proyecto)
    EscribirPropiedadUsuario(drawingDoc, "DISENADOR", disenador)

    EscribirPropiedad(drawingDoc, "Design Tracking Properties", "Part Number", articulo)
    EscribirPropiedad(drawingDoc, "Design Tracking Properties", "Description", descripcion)
    EscribirPropiedad(drawingDoc, "Design Tracking Properties", "Project", proyecto)
    EscribirPropiedad(drawingDoc, "Design Tracking Properties", "Designer", disenador)
    EscribirPropiedad(drawingDoc, "Inventor Summary Information", "Revision Number", revision)
    EscribirPropiedad(drawingDoc, "Summary Information", "Revision Number", revision)

    RellenarPromptsCajetin(sheet, articulo, descripcion, centroTrabajo, escalaTexto)

    drawingDoc.Update2(True)

    Dim nombreBase As String = LimpiarNombreArchivo(articulo)
    If nombreBase.Length > 130 Then nombreBase = nombreBase.Substring(0, 130)

    Dim extensionPlano As String = System.IO.Path.GetExtension(rutaPlantilla).ToLower()
    If extensionPlano <> ".idw" And extensionPlano <> ".dwg" Then extensionPlano = ".idw"

    Dim rutaPlano As String = System.IO.Path.Combine(carpetaSalida, nombreBase & extensionPlano)
    Dim rutaPDF As String = System.IO.Path.Combine(carpetaSalida, nombreBase & ".pdf")
    Dim rutaDWF As String = System.IO.Path.Combine(carpetaSalida, nombreBase & ".dwf")

    drawingDoc.SaveAs(rutaPlano, False)
    ExportarPDF(invApp, drawingDoc, rutaPDF)
    ExportarDWF(invApp, drawingDoc, rutaDWF)

    drawingDoc.Activate()

End Sub

'=================================================================
' IMPLEMENTACIONES DESPIECE - COMPLETA
'=================================================================

Sub CrearDespieceVisualImpl(ByVal invApp As Inventor.Application, _
                           ByVal tg As TransientGeometry, _
                           ByVal asmDoc As AssemblyDocument, _
                           ByVal templatePath As String)

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

    Dim showTitle As Boolean = True
    Dim titleText As String = "DESPIECE VISUAL"

    Dim viewWidthFactor As Double = 0.88
    Dim viewHeightFactor As Double = 0.62

    Dim viewYOffsetFactor As Double = 0.13
    Dim labelYOffsetFactor As Double = 0.34

    Dim maxEnlargeFactor As Double = 1.60

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

    Dim maxViewW As Double = cellW * viewWidthFactor
    Dim maxViewH As Double = cellH * viewHeightFactor

    Dim labelTextHeight As Double = cellH * 0.045
    If labelTextHeight > 0.22 Then labelTextHeight = 0.22
    If labelTextHeight < 0.13 Then labelTextHeight = 0.13

    Dim titleTextHeight As Double = 0.14

    Dim labelStyle As TextStyle = Nothing
    Dim titleStyle As TextStyle = Nothing

    Try
        labelStyle = drawDoc.StylesManager.TextStyles.Item("DESPIECE_1H_ETIQUETA_REDUCIDA")
    Catch
        Try
            labelStyle = drawDoc.StylesManager.TextStyles.Item(1).Copy("DESPIECE_1H_ETIQUETA_REDUCIDA")
        Catch
            labelStyle = Nothing
        End Try
    End Try

    If Not labelStyle Is Nothing Then
        Try
            labelStyle.FontSize = labelTextHeight
            labelStyle.Bold = False
        Catch
        End Try
    End If

    Try
        titleStyle = drawDoc.StylesManager.TextStyles.Item("DESPIECE_1H_TITULO_REDUCIDO")
    Catch
        Try
            titleStyle = drawDoc.StylesManager.TextStyles.Item(1).Copy("DESPIECE_1H_TITULO_REDUCIDO")
        Catch
            titleStyle = Nothing
        End Try
    End Try

    If Not titleStyle Is Nothing Then
        Try
            titleStyle.FontSize = titleTextHeight
            titleStyle.Bold = True
        Catch
        End Try
    End If

    If showTitle = True Then
        Try
            Dim headNote As GeneralNote
            headNote = sheet.DrawingNotes.GeneralNotes.AddFitted( _
                tg.CreatePoint2d(marginLeft + 0.2, sheet.Height - 0.75), _
                titleText)

            If Not titleStyle Is Nothing Then
                headNote.TextStyle = titleStyle
            End If
        Catch
        End Try
    End If

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
        If modelDoc.DocumentType <> DocumentTypeEnum.kPartDocumentObject Then Continue For

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

        partNumber = Replace(partNumber, "&", "Y")
        partNumber = Replace(partNumber, "<", "")
        partNumber = Replace(partNumber, ">", "")

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

        Dim view As DrawingView = Nothing
        Try
            view = sheet.DrawingViews.AddBaseView( _
                modelDoc, _
                tg.CreatePoint2d(x, y + (cellH * viewYOffsetFactor)), _
                1, _
                ViewOrientationTypeEnum.kIsoTopRightViewOrientation, _
                DrawingViewStyleEnum.kHiddenLineRemovedDrawingViewStyle)
        Catch
            Continue For
        End Try

        Try
            Dim scaleByWidth As Double = maxViewW / view.Width
            Dim scaleByHeight As Double = maxViewH / view.Height

            Dim fitScaleFactor As Double = Math.Min(scaleByWidth, scaleByHeight)

            If fitScaleFactor > maxEnlargeFactor Then
                fitScaleFactor = maxEnlargeFactor
            End If

            If fitScaleFactor <= 0 Then
                fitScaleFactor = 1
            End If

            view.Scale = view.Scale * fitScaleFactor
        Catch
        End Try

        Try
            Dim labelText As String = partNumber & " (" & qty & "UD)"

            Dim labelX As Double = x - (cellW * 0.38)
            Dim labelY As Double = y - (cellH * labelYOffsetFactor)

            Dim partNote As GeneralNote
            partNote = sheet.DrawingNotes.GeneralNotes.AddFitted( _
                tg.CreatePoint2d(labelX, labelY), _
                labelText)

            If Not labelStyle Is Nothing Then
                partNote.TextStyle = labelStyle
            End If

        Catch
        End Try

    Next

    Try
        drawDoc.Update()
    Catch
    End Try

    Try
        drawDoc.SaveAs(savePath, False)
    Catch ex As Exception
        Throw New Exception("El plano se ha generado, pero no se ha podido guardar automáticamente: " & ex.Message)
    End Try

End Sub
'=================================================================
' FUNCIONES HELPER CONSOLIDADAS
'=================================================================

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
    End Try
End Sub

Function EsLineaRectaVista(ByVal dc As DrawingCurve) As Boolean
    Try
        If dc Is Nothing Then Return False
        If dc.StartPoint Is Nothing Then Return False
        If dc.EndPoint Is Nothing Then Return False

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

    Try
        If sheet Is Nothing Then Exit Sub
        If codigo Is Nothing OrElse Trim(codigo) = "" Then Exit Sub

        Dim textoFormateado As String = _
            "<StyleOverride FontSize='0.35' Bold='False'>COD PLEG</StyleOverride>" & _
            "<Br/>" & _
            "<StyleOverride FontSize='0.80' Bold='False'>" & _
            EscaparTextoInventor(Trim(codigo)) & _
            "</StyleOverride>"

        Dim nota As GeneralNote = _
            sheet.DrawingNotes.GeneralNotes.AddFitted( _
                punto, _
                textoFormateado)

        Try
            nota.HorizontalJustification = HorizontalTextAlignmentEnum.kAlignTextCenter
        Catch
        End Try

    Catch ex As Exception
        Throw New Exception("No se ha podido insertar el rotulo COD PLEG. Detalle: " & ex.Message)
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

Sub RellenarPromptsCajetin(ByVal sheet As Sheet, _
                           ByVal articulo As String, _
                           ByVal descripcion As String, _
                           ByVal centro As String, _
                           ByVal escala As String)
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

                If tu.Contains("DESCRIPCION") Or tu.Contains("DESCRIPCIÓN") Then
                    Try
                        tb.SetPromptResultText(txt, descripcion)
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
            Catch
            End Try
        Next
    Catch
    End Try
End Sub

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

Function CalcularEscalaMaximaVistaCreada(ByVal vista As DrawingView, _
    ByVal anchoZona As Double, _
    ByVal altoZona As Double, _
    ByVal escalasDisponibles() As Double) As Double

    Try
        If vista Is Nothing Then Return 0.1
        If vista.Scale <= 0 Then Return 0.1

        Dim anchoReal As Double = vista.Width / vista.Scale
        Dim altoReal As Double = vista.Height / vista.Scale

        Dim anchoUtil As Double = anchoZona * 0.90
        Dim altoUtil As Double = altoZona * 0.90

        For Each s As Double In escalasDisponibles
            If (anchoReal * s <= anchoUtil) AndAlso (altoReal * s <= altoUtil) Then
                Return s
            End If
        Next

        Return escalasDisponibles(escalasDisponibles.Length - 1)

    Catch
        Return 0.1
    End Try

End Function

Function CalcularEscalaComunVistasCreadas(_
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
        Dim e1 As Double = CalcularEscalaMaximaVistaCreada(vista1, anchoZona1, altoZona1, escalasDisponibles)
        Dim e2 As Double = CalcularEscalaMaximaVistaCreada(vista2, anchoZona2, altoZona2, escalasDisponibles)
        Dim e3 As Double = CalcularEscalaMaximaVistaCreada(vista3, anchoZona3, altoZona3, escalasDisponibles)

        Return Math.Min(e1, Math.Min(e2, e3))

    Catch
        Return 0.1
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

Function CalcularEscalaDetalleExtremo(ByVal partDoc As PartDocument, _
                                      ByVal sheet As Sheet, _
                                      ByVal escalasDisponibles() As Double) As Double
    Try
        Dim mayor As Double = 1
        Dim media As Double = 1
        Dim menor As Double = 1
        ObtenerDimensionesModeloOrdenadas(partDoc, mayor, media, menor)

        Dim zona As Double = If(sheet.Width > sheet.Height, 7.0, 6.0)

        For Each s As Double In escalasDisponibles
            If s <= 0.5 + 0.0000001 Then
                If CabeRectanguloEnZona(media, menor, zona, zona, s, True) Then Return s
            End If
        Next

        Return 0.1
    Catch
        Return 0.2
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

Function ObtenerCarpetaPlanoIDWPorCodigo(ByVal rutaBasePlanosIDW As String, _
                                         ByVal codigo As String) As String

    If rutaBasePlanosIDW Is Nothing Then rutaBasePlanosIDW = ""
    rutaBasePlanosIDW = Trim(rutaBasePlanosIDW)

    If rutaBasePlanosIDW = "" Then
        Throw New Exception("Ruta base de planos IDW vacia.")
    End If

    If Not System.IO.Directory.Exists(rutaBasePlanosIDW) Then
        Throw New Exception("No existe la ruta base de PLANOS PRODUCCION: " & rutaBasePlanosIDW)
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

Sub PonerGlobosAutomaticos(ByVal invApp As Inventor.Application, _
                           ByVal tg As TransientGeometry, _
                           ByVal sheet As Sheet, _
                           ByVal vista As DrawingView, _
                           ByVal asmDoc As AssemblyDocument)
    Try
        If vista Is Nothing Then Exit Sub

        Dim oAsmDef As AssemblyComponentDefinition = asmDoc.ComponentDefinition
        Dim occs As ComponentOccurrences = oAsmDef.Occurrences
        If occs.Count <= 0 Then Exit Sub

        Dim ESPACIO As Double = 1.8
        Dim MAX_POR_FILA As Integer = 10
        Dim GAP_VISTA As Double = 2.5

        Dim topViewEdge As Double = vista.Position.Y + (vista.Height / 2.0) + GAP_VISTA

        Dim visibleCount As Integer = 0
        For Each occ As ComponentOccurrence In occs
            Try
                Dim curvas As DrawingCurvesEnumerator = vista.DrawingCurves(occ)
                If curvas IsNot Nothing AndAlso curvas.Count > 0 Then
                    visibleCount += 1
                End If
            Catch
            End Try
        Next

        If visibleCount = 0 Then Exit Sub

        Dim idx As Integer = 0
        For Each occ As ComponentOccurrence In occs
            Try
                Dim curvas As DrawingCurvesEnumerator = vista.DrawingCurves(occ)
                If curvas Is Nothing Then Continue For
                If curvas.Count = 0 Then Continue For

                Dim dc As DrawingCurve = curvas.Item(1)
                Dim gi As GeometryIntent = sheet.CreateGeometryIntent(dc, PointIntentEnum.kMidPointIntent)

                Dim fila As Integer = idx \ MAX_POR_FILA
                Dim col As Integer = idx Mod MAX_POR_FILA
                Dim countEnFila As Integer = Math.Min(MAX_POR_FILA, visibleCount - fila * MAX_POR_FILA)

                Dim startX As Double = vista.Position.X - ((countEnFila - 1) * ESPACIO / 2.0)
                Dim bx As Double = startX + (col * ESPACIO)
                Dim By As Double = topViewEdge + (fila * ESPACIO)

                Dim leader As ObjectCollection = invApp.TransientObjects.CreateObjectCollection()
                leader.Add(tg.CreatePoint2d(bx, By))
                leader.Add(gi)

                sheet.Balloons.Add(leader)
                idx += 1
            Catch
            End Try
        Next
    Catch
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

Function ObtenerMayorDimensionModeloMM(ByVal doc As Document) As Double
    Try
        Dim box As Box = doc.ComponentDefinition.RangeBox
        Dim dx As Double = Math.Abs(box.MaxPoint.X - box.MinPoint.X) * 10.0
        Dim dy As Double = Math.Abs(box.MaxPoint.Y - box.MinPoint.Y) * 10.0
        Dim dz As Double = Math.Abs(box.MaxPoint.Z - box.MinPoint.Z) * 10.0
        Return Math.Max(dx, Math.Max(dy, dz))
    Catch
        Return 0
    End Try
End Function

Sub PonerSecuenciaSoldadura(ByVal sheet As Sheet, _
                            ByVal tg As TransientGeometry, _
                            ByVal secuencia As String)
    Try
        If secuencia = "" Then Exit Sub
        Dim yBase As Double = ObtenerYSuperiorCajetin(sheet) + 0.5
        Dim pSeq As Point2d = tg.CreatePoint2d(1.5, yBase)
        sheet.DrawingNotes.GeneralNotes.AddFitted(pSeq, secuencia)
    Catch
    End Try
End Sub

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

Function LimpiarNombreArchivo(ByVal nombre As String) As String
    Dim invalidos() As Char = System.IO.Path.GetInvalidFileNameChars()
    For Each c As Char In invalidos
        nombre = nombre.Replace(c, "_"c)
    Next
    nombre = nombre.Replace("  ", " ").Trim()
    If nombre = "" Then nombre = "PLANO"
    Return nombre
End Function

Function ObtenerAutorActual(ByVal invApp As Inventor.Application) As String

    Dim autor As String = ""

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

    If autor = "" Then
        Try
            autor = Trim(System.Environment.UserName)
        Catch
            autor = ""
        End Try
    End If

    If autor = "" Then autor = "USUARIO"

    Return autor

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

' ===== FUNCIONES PINTURA HELPER =====

Function LeerPropiedadUsuarioPrioritaria(ByVal doc As Document, _
                                         ByVal nombres() As String) As String

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

    If vUp = "N/A" OrElse vUp = "NA" OrElse vUp = "SIN CODIGO" OrElse vUp = "SIN CÓDIGO" Then Return ""
    If vUp = "SIN DESCRIPCION" OrElse vUp = "SIN DESCRIPCIÓN" Then Return ""

    Return valor

End Function

Function CrearPlanoMetalplakSeguro(ByVal invApp As Inventor.Application, _
                                   ByVal rutaPlantilla As String) As DrawingDocument

    Dim errores As String = ""

    Try
        If rutaPlantilla IsNot Nothing Then
            rutaPlantilla = rutaPlantilla.Trim()
        Else
            rutaPlantilla = ""
        End If

        If rutaPlantilla <> "" AndAlso System.IO.File.Exists(rutaPlantilla) Then
            Dim d0 As DrawingDocument = TryCast(invApp.Documents.Add(DocumentTypeEnum.kDrawingDocumentObject, rutaPlantilla, True), DrawingDocument)
            If d0 IsNot Nothing Then Return d0
        Else
            errores &= "No existe la plantilla Metalplak: " & rutaPlantilla & vbCrLf
        End If
    Catch ex0 As Exception
        errores &= "Plantilla Metalplak por ruta: " & ex0.Message & vbCrLf
    End Try

    Try
        Dim d As DrawingDocument = TryCast(invApp.Documents.Add(DocumentTypeEnum.kDrawingDocumentObject), DrawingDocument)
        If d IsNot Nothing Then Return d
    Catch ex1 As Exception
        errores &= "Plantilla por defecto: " & ex1.Message & vbCrLf
    End Try

    Try
        Dim d2 As DrawingDocument = TryCast(invApp.Documents.Add(DocumentTypeEnum.kDrawingDocumentObject, "", True), DrawingDocument)
        If d2 IsNot Nothing Then Return d2
    Catch ex2 As Exception
        errores &= "Plantilla por defecto con ruta vacia: " & ex2.Message & vbCrLf
    End Try

    Throw New Exception("No se pudo crear el plano." & vbCrLf & errores)

End Function

Sub ForzarUnidadesMilimetros(ByVal drawingDoc As DrawingDocument)

    Try
        drawingDoc.UnitsOfMeasure.LengthUnits = UnitsTypeEnum.kMillimeterLengthUnits
    Catch
    End Try

End Sub

Function CrearVistaBaseSegura(ByVal invApp As Inventor.Application, _
                              ByVal sheet As Sheet, _
                              ByVal modelDoc As Document, _
                              ByVal punto As Point2d, _
                              ByVal escala As Double, _
                              ByVal orientacion As ViewOrientationTypeEnum) As DrawingView

    Dim errores As String = ""

    Try
        Return sheet.DrawingViews.AddBaseView(modelDoc, punto, escala, orientacion, DrawingViewStyleEnum.kHiddenLineRemovedDrawingViewStyle)
    Catch ex1 As Exception
        errores &= "Intento corto: " & ex1.Message & vbCrLf
    End Try

    Try
        Dim opts As NameValueMap = invApp.TransientObjects.CreateNameValueMap()
        Return sheet.DrawingViews.AddBaseView(modelDoc, punto, escala, orientacion, DrawingViewStyleEnum.kHiddenLineRemovedDrawingViewStyle, "", Nothing, opts)
    Catch ex2 As Exception
        errores &= "Intento completo con opts vacio: " & ex2.Message & vbCrLf
    End Try

    Try
        Return sheet.DrawingViews.AddBaseView(modelDoc, punto, escala, ViewOrientationTypeEnum.kDefaultViewOrientation, DrawingViewStyleEnum.kHiddenLineRemovedDrawingViewStyle)
    Catch ex3 As Exception
        errores &= "Intento default corto: " & ex3.Message & vbCrLf
    End Try

    Throw New Exception("No se pudo crear vista base." & vbCrLf & errores)

End Function

Function CalcularPuntoAcabadoRAL(ByVal sheet As Sheet, _
                                 ByVal tg As TransientGeometry) As Point2d

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

Sub InsertarAcabadoRALSeguro(ByVal drawingDoc As DrawingDocument, _
                             ByVal sheet As Sheet, _
                             ByVal tg As TransientGeometry, _
                             ByVal nombreSimbolo As String, _
                             ByVal textoAcabado As String, _
                             ByVal punto As Point2d)

    Dim def As SketchedSymbolDefinition = BuscarDefinicionSimboloRAL(drawingDoc, nombreSimbolo)

    If def IsNot Nothing Then

        Dim errores As String = ""
        Dim escalaSimbolo As Double = 1

        Try
            sheet.SketchedSymbols.Add(def, punto, 0, escalaSimbolo)
            Exit Sub
        Catch ex0 As Exception
            errores &= "Sin prompts: " & ex0.Message & vbCrLf
        End Try

        For n As Integer = 1 To 6
            Try
                Dim prompts(n - 1) As String

                For i As Integer = 0 To n - 1
                    prompts(i) = ""
                Next

                sheet.SketchedSymbols.Add(def, punto, 0, escalaSimbolo, prompts)
                Exit Sub
            Catch exn As Exception
                errores &= n.ToString() & " prompts: " & exn.Message & vbCrLf
            End Try
        Next

        InsertarEtiquetaAcabadoRAL(sheet, tg, textoAcabado, punto)
        Exit Sub

    End If

    InsertarEtiquetaAcabadoRAL(sheet, tg, textoAcabado, punto)

End Sub

Function BuscarDefinicionSimboloRAL(ByVal drawingDoc As DrawingDocument, _
                                    ByVal nombreSolicitado As String) As SketchedSymbolDefinition

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

Sub InsertarEtiquetaAcabadoRAL(ByVal sheet As Sheet, _
                               ByVal tg As TransientGeometry, _
                               ByVal textoAcabado As String, _
                               ByVal punto As Point2d)

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
    texto = texto.Replace(" ", "")
    texto = texto.Replace("-", "")
    texto = texto.Replace("_", "")
    texto = texto.Replace(".", "")

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

Sub RellenarPromptsCajetinPintura(ByVal sheet As Sheet, _
                                  ByVal articulo As String, _
                                  ByVal articuloFinal As String, _
                                  ByVal centro As String, _
                                  ByVal escala As String, _
                                  ByVal referenciaCliente As String, _
                                  ByVal descripcion As String, _
                                  ByVal acabado As String)

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

                ElseIf tu.Contains("REFERENCIACLIENTE") Or _
                       tu.Contains("CODIGOCLIENTE") Or _
                       tu.Contains("CODCLIENTE") Then
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
    If textoNormalizado.Contains("NODEPIEZA") Then Return True
    If textoNormalizado.Contains("NUMERODEPIEZA") Then Return True
    If textoNormalizado.Contains("NRODEPIEZA") Then Return True
    If textoNormalizado.Contains("PARTNUMBER") Then Return True

    Return False

End Function

Function NormalizarTextoCampo(ByVal texto As String) As String

    If texto Is Nothing Then texto = ""

    texto = texto.ToUpper()
    texto = texto.Replace("Á", "A")
    texto = texto.Replace("É", "E")
    texto = texto.Replace("Í", "I")
    texto = texto.Replace("Ó", "O")
    texto = texto.Replace("Ú", "U")
    texto = texto.Replace("Ü", "U")
    texto = texto.Replace("Ñ", "N")

    texto = texto.Replace("º", "")
    texto = texto.Replace("ª", "")
    texto = texto.Replace("°", "")

    texto = texto.Replace(" ", "")
    texto = texto.Replace("_", "")
    texto = texto.Replace("-", "")
    texto = texto.Replace(".", "")
    texto = texto.Replace(":", "")
    texto = texto.Replace("<", "")
    texto = texto.Replace(">", "")
    texto = texto.Replace("/", "")

    Return texto

End Function

Function CalcularEscalaA4Maxima(ByVal modelDoc As Document, _
                              ByVal sheet As Sheet, _
                              ByVal escalasDisponibles() As Double) As Double

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

            Dim cabeLateral As Boolean = (anchoVistaLarga * s <= anchoZonaDerecha) AndAlso _
                                         (altoVistaLarga * s <= altoZonaSuperior)

            Dim cabePrincipal As Boolean = (anchoVistaCorta * s <= anchoZonaIzquierda) AndAlso _
                                           (altoVistaCorta * s <= altoZonaSuperior)

            Dim cabeSuperior As Boolean = (anchoVistaCorta * s <= anchoZonaIzquierda) AndAlso _
                                          (altoVistaLarga * s <= altoZonaInferior)

            Dim cabeIso As Boolean = (anchoVistaLarga * s <= anchoZonaDerecha) AndAlso _
                                     (altoVistaLarga * s <= altoZonaInferior)

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
