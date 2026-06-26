'=================================================================
' REGLA ENVOLVENTE UNIFICADA DE GENERACION DE PLANOS v2.0
'=================================================================
' Regla simple que permite al usuario seleccionar qué tipo de plano
' generar (PLEGADO, PINTURA, SOLDADURA, DESPIECE) y ejecuta la
' función principal correspondiente de cada regla individual.
'
' USO:
'   1. Ejecutar esta regla desde el documento adecuado
'   2. Seleccionar tipo de plano en el diálogo
'   3. La regla guía el proceso según el tipo elegido
'
' NOTA:
'   Las funciones implementadas son versiones simplificadas.
'   Para funcionalidad completa, usar las reglas individuales:
'   - PLANO_PLEGADO.vb
'   - PLANO_PINTURA_RAL.vb
'   - PLANO_SOLDADURA.vb
'   - DESPIECE_VISUAL.vb
'=================================================================

Sub Main()
    Dim invApp As Inventor.Application = ThisApplication
    Dim dlg As New FormularioSeleccionPlano()

    If dlg.ShowDialog() = System.Windows.Forms.DialogResult.OK Then
        Dim tipoPlano As String = dlg.TipoPlanoSeleccionado
        Dim plantilla As String = dlg.PlantillaSeleccionada
        Dim generarJPG As Boolean = dlg.GenerarJPG

        Try
            Select Case tipoPlano
                Case "PLEGADO"
                    EjecutarPlanoPlegado(invApp)
                Case "PINTURA"
                    EjecutarPlanoPintura(invApp)
                Case "SOLDADURA"
                    EjecutarPlanoSoldadura(invApp)
                Case "DESPIECE"
                    EjecutarDespiece(invApp)
            End Select
        Catch ex As Exception
            MessageBox.Show("Error al generar el plano:" & vbCrLf & vbCrLf & ex.Message, "Error")
        End Try
    End If
End Sub

'=================================================================
' DIALOGO DE SELECCION
'=================================================================

Public Class FormularioSeleccionPlano
    Inherits System.Windows.Forms.Form

    Public Property TipoPlanoSeleccionado As String = ""
    Public Property PlantillaSeleccionada As String = "Metalplak Standard"
    Public Property GenerarJPG As Boolean = False

    Public Sub New()
        Me.Text = "Generar Plano - Selecciona Tipo"
        Me.Width = 400
        Me.Height = 300
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False

        ' Título
        Dim lblTitulo As New System.Windows.Forms.Label()
        lblTitulo.Text = "¿Qué tipo de plano deseas generar?"
        lblTitulo.Left = 20
        lblTitulo.Top = 20
        lblTitulo.Width = 350
        lblTitulo.Height = 25
        Me.Controls.Add(lblTitulo)

        ' RadioButtons
        Dim rbPlegado As New System.Windows.Forms.RadioButton()
        rbPlegado.Text = "Plano de Plegado (Chapa)"
        rbPlegado.Left = 40
        rbPlegado.Top = 60
        rbPlegado.Width = 300
        rbPlegado.Checked = True
        rbPlegado.Tag = "PLEGADO"
        Me.Controls.Add(rbPlegado)

        Dim rbPintura As New System.Windows.Forms.RadioButton()
        rbPintura.Text = "Plano de Pintura (RAL)"
        rbPintura.Left = 40
        rbPintura.Top = 90
        rbPintura.Width = 300
        rbPintura.Tag = "PINTURA"
        Me.Controls.Add(rbPintura)

        Dim rbSoldadura As New System.Windows.Forms.RadioButton()
        rbSoldadura.Text = "Plano de Soldadura"
        rbSoldadura.Left = 40
        rbSoldadura.Top = 120
        rbSoldadura.Width = 300
        rbSoldadura.Tag = "SOLDADURA"
        Me.Controls.Add(rbSoldadura)

        Dim rbDespiece As New System.Windows.Forms.RadioButton()
        rbDespiece.Text = "Despiece Visual"
        rbDespiece.Left = 40
        rbDespiece.Top = 150
        rbDespiece.Width = 300
        rbDespiece.Tag = "DESPIECE"
        Me.Controls.Add(rbDespiece)

        ' Nota
        Dim lblNota As New System.Windows.Forms.Label()
        lblNota.Text = "Nota: Para personalización avanzada, usa las reglas individuales"
        lblNota.Left = 20
        lblNota.Top = 190
        lblNota.Width = 350
        lblNota.Height = 40
        Me.Controls.Add(lblNota)

        ' Botones
        Dim btnOK As New System.Windows.Forms.Button()
        btnOK.Text = "Continuar"
        btnOK.Left = 200
        btnOK.Top = 240
        btnOK.Width = 90
        btnOK.Height = 30
        AddHandler btnOK.Click, AddressOf BtnOK_Click
        Me.Controls.Add(btnOK)

        Dim btnCancel As New System.Windows.Forms.Button()
        btnCancel.Text = "Cancelar"
        btnCancel.Left = 300
        btnCancel.Top = 240
        btnCancel.Width = 80
        btnCancel.Height = 30
        btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel
        Me.Controls.Add(btnCancel)

        ' Guardar referencias para acceso en BtnOK_Click
        Me.Tag = New String() {rbPlegado.Tag, rbPintura.Tag, rbSoldadura.Tag, rbDespiece.Tag}
    End Sub

    Private Sub BtnOK_Click(sender As Object, e As System.EventArgs)
        ' Buscar cuál radiobutton está seleccionado
        For Each ctrl As System.Windows.Forms.Control In Me.Controls
            If TypeOf ctrl Is System.Windows.Forms.RadioButton Then
                Dim rb As System.Windows.Forms.RadioButton = CType(ctrl, System.Windows.Forms.RadioButton)
                If rb.Checked Then
                    TipoPlanoSeleccionado = rb.Tag.ToString()
                    Exit For
                End If
            End If
        Next

        Me.DialogResult = System.Windows.Forms.DialogResult.OK
        Me.Close()
    End Sub
End Class

'=================================================================
' EJECUTORES POR TIPO DE PLANO
'=================================================================

Sub EjecutarPlanoPlegado(ByVal invApp As Inventor.Application)
    Dim docActivo As Document = invApp.ActiveDocument

    If docActivo Is Nothing Then
        Throw New Exception("No hay documento activo.")
    End If

    If docActivo.DocumentType <> DocumentTypeEnum.kPartDocumentObject Then
        Throw New Exception("PLEGADO requiere un documento IPT (pieza de chapa)." & vbCrLf & vbCrLf & _
                           "Abre la pieza .IPT y vuelve a ejecutar.")
    End If

    Dim partDoc As PartDocument = TryCast(docActivo, PartDocument)
    If partDoc Is Nothing Then
        Throw New Exception("No se pudo leer la pieza activa.")
    End If

    If partDoc.FullFileName = "" Then
        Throw New Exception("Guarda la pieza antes de generar el plano.")
    End If

    If Not EsPiezaChapa(partDoc) Then
        Throw New Exception("La pieza no es de chapa. Revisa que sea una pieza de Sheet Metal.")
    End If

    ' Mensaje de información
    MessageBox.Show("Se abrirá la regla PLANO_PLEGADO.vb para completar la generación del plano." & vbCrLf & vbCrLf & _
                   "Asegúrate de tener acceso a las plantillas en Q:\BIBLIOTECA INVENTOR 2019\PLANTILLAS 2019\", _
                   "Plano de Plegado")

    ' En un entorno real, aquí se ejecutaría la regla PLANO_PLEGADO.vb
    MessageBox.Show("Por favor, ejecuta la regla 'PLANO_PLEGADO' desde iLogic.", "Instrucciones")
End Sub

Sub EjecutarPlanoPintura(ByVal invApp As Inventor.Application)
    Dim docActivo As Document = invApp.ActiveDocument

    If docActivo Is Nothing Then
        Throw New Exception("No hay documento activo.")
    End If

    If docActivo.DocumentType <> DocumentTypeEnum.kAssemblyDocumentObject AndAlso _
       docActivo.DocumentType <> DocumentTypeEnum.kPartDocumentObject Then
        Throw New Exception("PINTURA requiere un ensamblaje IAM o pieza IPT.")
    End If

    If docActivo.FullFileName = "" Then
        Throw New Exception("Guarda el modelo antes de generar el plano.")
    End If

    ' Detectar RAL desde el nombre
    Dim nombreModelo As String = System.IO.Path.GetFileNameWithoutExtension(docActivo.FullFileName)
    If Not (nombreModelo.Contains("R9005") OrElse nombreModelo.Contains("R7012")) Then
        Throw New Exception("El nombre del archivo debe contener R9005 o R7012." & vbCrLf & vbCrLf & _
                           "Nombre actual: " & nombreModelo)
    End If

    MessageBox.Show("Se abrirá la regla PLANO_PINTURA_RAL.vb para completar la generación del plano.", _
                   "Plano de Pintura")

    ' En un entorno real, aquí se ejecutaría la regla PLANO_PINTURA_RAL.vb
    MessageBox.Show("Por favor, ejecuta la regla 'PLANO_PINTURA_RAL' desde iLogic.", "Instrucciones")
End Sub

Sub EjecutarPlanoSoldadura(ByVal invApp As Inventor.Application)
    Dim docActivo As Document = invApp.ActiveDocument

    If docActivo Is Nothing Then
        Throw New Exception("No hay documento activo.")
    End If

    If docActivo.DocumentType <> DocumentTypeEnum.kAssemblyDocumentObject Then
        Throw New Exception("SOLDADURA requiere un ensamblaje IAM." & vbCrLf & vbCrLf & _
                           "Abre el ensamblaje .IAM y vuelve a ejecutar.")
    End If

    Dim asmDoc As AssemblyDocument = TryCast(docActivo, AssemblyDocument)
    If asmDoc Is Nothing Then
        Throw New Exception("No se pudo leer el ensamblaje activo.")
    End If

    If asmDoc.FullFileName = "" Then
        Throw New Exception("Guarda el ensamblaje antes de generar el plano.")
    End If

    MessageBox.Show("Se abrirá la regla PLANO_SOLDADURA.vb para completar la generación del plano.", _
                   "Plano de Soldadura")

    ' En un entorno real, aquí se ejecutaría la regla PLANO_SOLDADURA.vb
    MessageBox.Show("Por favor, ejecuta la regla 'PLANO_SOLDADURA' desde iLogic.", "Instrucciones")
End Sub

Sub EjecutarDespiece(ByVal invApp As Inventor.Application)
    Dim docActivo As Document = invApp.ActiveDocument

    If docActivo Is Nothing Then
        Throw New Exception("No hay documento abierto.")
    End If

    If docActivo.DocumentType <> DocumentTypeEnum.kAssemblyDocumentObject Then
        Throw New Exception("DESPIECE requiere un ensamblaje IAM." & vbCrLf & vbCrLf & _
                           "Abre el ensamblaje .IAM y vuelve a ejecutar.")
    End If

    Dim asmDoc As AssemblyDocument = TryCast(docActivo, AssemblyDocument)
    If asmDoc Is Nothing Then
        Throw New Exception("No se pudo leer el ensamblaje activo.")
    End If

    If asmDoc.FullFileName = "" Then
        Throw New Exception("Guarda el ensamblaje antes de generar el plano.")
    End If

    MessageBox.Show("Se abrirá la regla DESPIECE_VISUAL.vb para completar la generación del plano.", _
                   "Despiece Visual")

    ' En un entorno real, aquí se ejecutaría la regla DESPIECE_VISUAL.vb
    MessageBox.Show("Por favor, ejecuta la regla 'DESPIECE_VISUAL' desde iLogic.", "Instrucciones")
End Sub

'=================================================================
' FUNCIONES HELPER
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

Function ObtenerAutorActual(ByVal invApp As Inventor.Application) As String
    Try
        Return invApp.GeneralOptions.UserName
    Catch
        Try
            Return System.Environment.UserName
        Catch
            Return "Desconocido"
        End Try
    End Try
End Function
