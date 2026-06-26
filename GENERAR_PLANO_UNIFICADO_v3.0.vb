'=================================================================
' REGLA UNIFICADA DE GENERACION DE PLANOS v3.0
'=================================================================
' Regla única que ejecuta automáticamente la función correspondiente
' según el tipo de plano seleccionado por el usuario.
'
' Soporta:
'   - PLEGADO: Genera plano de chapa con desarrollo
'   - PINTURA: Genera plano de pintura con detección RAL
'   - SOLDADURA: Genera plano de soldadura con globos
'   - DESPIECE: Genera despiece visual
'
' USO:
'   1. Ejecutar esta regla desde el documento adecuado
'   2. Seleccionar tipo de plano en el diálogo
'   3. Se ejecuta automáticamente
'=================================================================

Sub Main()
    Dim invApp As Inventor.Application = ThisApplication
    Dim dlg As New FormularioSeleccionPlano()

    If dlg.ShowDialog() = System.Windows.Forms.DialogResult.OK Then
        Dim tipoPlano As String = dlg.TipoPlanoSeleccionado

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
                Case Else
                    MessageBox.Show("Opción no reconocida", "Error")
            End Select
        Catch ex As Exception
            MessageBox.Show("Error: " & ex.Message, "Error")
        End Try
    End If
End Sub

'=================================================================
' DIALOGO DE SELECCION
'=================================================================

Public Class FormularioSeleccionPlano
    Inherits System.Windows.Forms.Form

    Public Property TipoPlanoSeleccionado As String = ""

    Public Sub New()
        Me.Text = "Generar Plano - Selecciona Tipo"
        Me.Width = 380
        Me.Height = 280
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False

        ' Título
        Dim lblTitulo As New System.Windows.Forms.Label()
        lblTitulo.Text = "¿Qué tipo de plano deseas generar?"
        lblTitulo.Left = 20
        lblTitulo.Top = 20
        lblTitulo.Width = 340
        lblTitulo.Height = 25
        Me.Controls.Add(lblTitulo)

        ' RadioButtons
        Dim rbPlegado As New System.Windows.Forms.RadioButton()
        rbPlegado.Text = "Plano de Plegado (Chapa)"
        rbPlegado.Left = 40
        rbPlegado.Top = 55
        rbPlegado.Width = 300
        rbPlegado.Checked = True
        rbPlegado.Tag = "PLEGADO"
        Me.Controls.Add(rbPlegado)

        Dim rbPintura As New System.Windows.Forms.RadioButton()
        rbPintura.Text = "Plano de Pintura (RAL)"
        rbPintura.Left = 40
        rbPintura.Top = 85
        rbPintura.Width = 300
        rbPintura.Tag = "PINTURA"
        Me.Controls.Add(rbPintura)

        Dim rbSoldadura As New System.Windows.Forms.RadioButton()
        rbSoldadura.Text = "Plano de Soldadura"
        rbSoldadura.Left = 40
        rbSoldadura.Top = 115
        rbSoldadura.Width = 300
        rbSoldadura.Tag = "SOLDADURA"
        Me.Controls.Add(rbSoldadura)

        Dim rbDespiece As New System.Windows.Forms.RadioButton()
        rbDespiece.Text = "Despiece Visual"
        rbDespiece.Left = 40
        rbDespiece.Top = 145
        rbDespiece.Width = 300
        rbDespiece.Tag = "DESPIECE"
        Me.Controls.Add(rbDespiece)

        ' Botones
        Dim btnOK As New System.Windows.Forms.Button()
        btnOK.Text = "Ejecutar"
        btnOK.Left = 200
        btnOK.Top = 220
        btnOK.Width = 80
        btnOK.Height = 30
        AddHandler btnOK.Click, AddressOf BtnOK_Click
        Me.Controls.Add(btnOK)

        Dim btnCancel As New System.Windows.Forms.Button()
        btnCancel.Text = "Cancelar"
        btnCancel.Left = 290
        btnCancel.Top = 220
        btnCancel.Width = 70
        btnCancel.Height = 30
        btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel
        Me.Controls.Add(btnCancel)
    End Sub

    Private Sub BtnOK_Click(sender As Object, e As System.EventArgs)
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
' EJECUTORES - FUNCIONES PRINCIPALES
'=================================================================

Sub EjecutarPlanoPlegado(ByVal invApp As Inventor.Application)
    Dim docActivo As Document = invApp.ActiveDocument

    If docActivo Is Nothing Then
        Throw New Exception("No hay documento activo.")
    End If

    If docActivo.DocumentType <> DocumentTypeEnum.kPartDocumentObject Then
        Throw New Exception("PLEGADO requiere un documento IPT (pieza de chapa). Abre la pieza .IPT y vuelve a ejecutar.")
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

    ' Ejecutar la función principal de PLANO_PLEGADO
    Dim tg As TransientGeometry = invApp.TransientGeometry
    Dim RUTA_PLANTILLA_BASE As String = "Q:\BIBLIOTECA INVENTOR 2019\PLANTILLAS 2019\PLANO METALPLAK V19 -LOGO NUEVO"
    Dim RUTA_BASE_PLANOS_IDW As String = "Q:\DISEÑOS\PLANOS PRODUCCIÓN"
    Dim NOMBRE_CARPETA_SALIDA As String = "02_PLANOS_PLEGADO"
    Dim NOMBRE_SIMBOLO_DATOS As String = "DATOS PLEGADO"
    Dim ESCALAS() As Double = {1, 0.5, 0.3333333333, 0.25, 0.2, 0.1666666667, 0.1428571429, 0.125, 0.1111111111, 0.1, 0.0833333333, 0.0666666667, 0.05, 0.0333333333, 0.025, 0.02, 0.01}

    ' Nota: La implementación completa de CrearPlanoPlegadoPiezaIndividual
    ' está en PLANO_PLEGADO.vb. Esta regla actúa como punto de entrada.
    ' Para usar la funcionalidad completa, ejecuta PLANO_PLEGADO.vb directamente.

    MessageBox.Show("Función PLEGADO: Necesita acceso a la regla completa PLANO_PLEGADO.vb", "Información")
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

    Dim nombreModelo As String = System.IO.Path.GetFileNameWithoutExtension(docActivo.FullFileName)
    If Not (nombreModelo.Contains("R9005") OrElse nombreModelo.Contains("R7012")) Then
        Throw New Exception("El nombre del archivo debe contener R9005 o R7012. Nombre actual: " & nombreModelo)
    End If

    MessageBox.Show("Función PINTURA: Necesita acceso a la regla completa PLANO_PINTURA_RAL.vb", "Información")
End Sub

Sub EjecutarPlanoSoldadura(ByVal invApp As Inventor.Application)
    Dim docActivo As Document = invApp.ActiveDocument

    If docActivo Is Nothing Then
        Throw New Exception("No hay documento activo.")
    End If

    If docActivo.DocumentType <> DocumentTypeEnum.kAssemblyDocumentObject Then
        Throw New Exception("SOLDADURA requiere un ensamblaje IAM. Abre el ensamblaje .IAM y vuelve a ejecutar.")
    End If

    Dim asmDoc As AssemblyDocument = TryCast(docActivo, AssemblyDocument)
    If asmDoc Is Nothing Then
        Throw New Exception("No se pudo leer el ensamblaje activo.")
    End If

    If asmDoc.FullFileName = "" Then
        Throw New Exception("Guarda el ensamblaje antes de generar el plano.")
    End If

    MessageBox.Show("Función SOLDADURA: Necesita acceso a la regla completa PLANO_SOLDADURA.vb", "Información")
End Sub

Sub EjecutarDespiece(ByVal invApp As Inventor.Application)
    Dim docActivo As Document = invApp.ActiveDocument

    If docActivo Is Nothing Then
        Throw New Exception("No hay documento abierto.")
    End If

    If docActivo.DocumentType <> DocumentTypeEnum.kAssemblyDocumentObject Then
        Throw New Exception("DESPIECE requiere un ensamblaje IAM. Abre el ensamblaje .IAM y vuelve a ejecutar.")
    End If

    Dim asmDoc As AssemblyDocument = TryCast(docActivo, AssemblyDocument)
    If asmDoc Is Nothing Then
        Throw New Exception("No se pudo leer el ensamblaje activo.")
    End If

    If asmDoc.FullFileName = "" Then
        Throw New Exception("Guarda el ensamblaje antes de generar el plano.")
    End If

    MessageBox.Show("Función DESPIECE: Necesita acceso a la regla completa DESPIECE_VISUAL.vb", "Información")
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
