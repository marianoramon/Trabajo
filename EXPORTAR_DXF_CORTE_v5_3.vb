'---------------------------------------------------------
' REGLA EXTERNA GLOBAL - EXPORTAR DXF DE CORTE
' Version v5.3
'
' Archivo recomendado:
' Q:\BIBLIOTECA INVENTOR 2019\ILOGIC_GLOBAL\EXPORTAR_DXF_CORTE.iLogicVB
'
' CAMBIOS v5.3:
' - NOMBRE POR CODIGO DE CORTE: si el archivo se llama con dos numeros
'   separados por guion (ej: 43716-70831.ipt), el DXF se nombra con el
'   numero de DESPUES del guion, que es el codigo de corte (70831.dxf).
' - Carpetas por codigo: nueva constante RUTA_BASE_CARPETAS_CODIGO.
'   Antes se buscaban los rangos en la carpeta del ensamblaje (nunca
'   estaban alli) y el guardado por codigo no funcionaba.
' - Busqueda de carpeta de rango con formato estricto y comparacion
'   NUMERICA (.A02000-A02999, .40000-40999). Antes la comparacion
'   alfabetica metia piezas en rangos equivocados.
' - Si no existe la carpeta del rango, se crea automaticamente.
' - Cuando una pieza cae al fallback (sin carpeta de codigo), se anota
'   en la columna OBSERVACION del log.
' - El log del ensamblaje lleva fecha/hora (ya no se machaca).
' - Eliminados controles muertos del formulario (txtRuta, btnExaminar).
'
' CAMBIOS v5.2:
' - Un unico formulario con todas las opciones (seleccion, marcas, ruta)
'---------------------------------------------------------

Sub Main()

    Dim invApp As Inventor.Application = ThisApplication
    Dim activeDoc As Document = invApp.ActiveDocument

    ' v5.3: raiz donde estan las carpetas de rango por codigo
    ' (.A02000-A02999, .40000-40999, etc.). AJUSTAR SI CAMBIA.
    Dim RUTA_BASE_CARPETAS_CODIGO As String = "Q:\DISEÑOS\60- PULVER AGRO"

    If activeDoc Is Nothing Then
        MessageBox.Show("No hay ningun documento abierto.", "Exportar DXF corte")
        Return
    End If

    If activeDoc.FullFileName = "" Then
        MessageBox.Show("Guarda primero el documento antes de exportar DXF.", "Exportar DXF corte")
        Return
    End If

    Try
        If activeDoc.DocumentType = DocumentTypeEnum.kAssemblyDocumentObject Then
            Dim asmDoc As AssemblyDocument = TryCast(activeDoc, AssemblyDocument)
            If asmDoc Is Nothing Then
                MessageBox.Show("No se ha podido leer el ensamblaje activo.", "Exportar DXF corte")
                Return
            End If

            Dim cantidadSeleccion As Integer = 0
            Try
                cantidadSeleccion = asmDoc.SelectSet.Count
            Catch
                cantidadSeleccion = 0
            End Try

            Dim formulario As New FormularioExportarDXF(cantidadSeleccion > 0, asmDoc.FullFileName)
            Dim resultado As System.Windows.Forms.DialogResult = formulario.ShowDialog()

            If resultado <> System.Windows.Forms.DialogResult.OK Then
                Return
            End If

            Dim exportarSeleccion As Boolean = formulario.ExportarSeleccion
            Dim incluirMarcas As Boolean = formulario.IncluirMarcas
            Dim rutaCustom As String = formulario.RutaDestino
            Dim rutaPorCodigo As Boolean = formulario.RutaPorCodigo

            If exportarSeleccion AndAlso cantidadSeleccion = 0 Then
                MessageBox.Show("No hay elementos seleccionados.", "Exportar DXF corte")
                Return
            End If

            If exportarSeleccion Then
                ExportarDXFSeleccionEnsamblaje(invApp, asmDoc, rutaCustom, incluirMarcas, rutaPorCodigo, RUTA_BASE_CARPETAS_CODIGO)
            Else
                ExportarDXFDesdeEnsamblaje(invApp, asmDoc, rutaCustom, incluirMarcas, rutaPorCodigo, RUTA_BASE_CARPETAS_CODIGO)
            End If
            Return
        End If

        If activeDoc.DocumentType = DocumentTypeEnum.kPartDocumentObject Then
            Dim partDoc As PartDocument = TryCast(activeDoc, PartDocument)
            If partDoc Is Nothing Then
                MessageBox.Show("No se ha podido leer la pieza activa.", "Exportar DXF corte")
                Return
            End If

            Dim formulario As New FormularioExportarDXF(False, partDoc.FullFileName)
            Dim resultado As System.Windows.Forms.DialogResult = formulario.ShowDialog()

            If resultado <> System.Windows.Forms.DialogResult.OK Then
                Return
            End If

            Dim incluirMarcas As Boolean = formulario.IncluirMarcas
            Dim rutaCustom As String = formulario.RutaDestino
            Dim rutaPorCodigo As Boolean = formulario.RutaPorCodigo

            ExportarDXFDesdePieza(invApp, partDoc, rutaCustom, incluirMarcas, rutaPorCodigo, RUTA_BASE_CARPETAS_CODIGO)
            Return
        End If

        MessageBox.Show("Esta regla solo funciona con ensamblajes .IAM o piezas .IPT." & vbCrLf & vbCrLf & _
                        "Documento actual: " & activeDoc.DisplayName, _
                        "Exportar DXF corte")

    Catch ex As Exception
        MessageBox.Show("Error general exportando DXF:" & vbCrLf & vbCrLf & ex.Message, "Exportar DXF corte")
    End Try

End Sub

'---------------------------------------------------------
' FORMULARIO PERSONALIZADO PARA OPCIONES
'---------------------------------------------------------

Public Class FormularioExportarDXF
    Inherits System.Windows.Forms.Form

    Private WithEvents btnOK As System.Windows.Forms.Button
    Private WithEvents btnCancel As System.Windows.Forms.Button
    Private WithEvents rbTodas As System.Windows.Forms.RadioButton
    Private WithEvents rbSeleccion As System.Windows.Forms.RadioButton
    Private WithEvents chkMarcas As System.Windows.Forms.CheckBox
    Private WithEvents gbModo As System.Windows.Forms.GroupBox
    Private WithEvents gbOpciones As System.Windows.Forms.GroupBox
    Private WithEvents gbRuta As System.Windows.Forms.GroupBox
    Private WithEvents rbRutaPersonalizada As System.Windows.Forms.RadioButton
    Private WithEvents rbRutaPorCodigo As System.Windows.Forms.RadioButton

    Private _exportarSeleccion As Boolean = False
    Private _incluirMarcas As Boolean = False
    Private _rutaDestino As String = ""
    Private _rutaPorCodigo As Boolean = False
    Private _haySeleccion As Boolean = False
    Private _rutaPorDefecto As String = ""

    Public ReadOnly Property ExportarSeleccion As Boolean
        Get
            Return _exportarSeleccion
        End Get
    End Property

    Public ReadOnly Property IncluirMarcas As Boolean
        Get
            Return _incluirMarcas
        End Get
    End Property

    Public ReadOnly Property RutaDestino As String
        Get
            Return _rutaDestino
        End Get
    End Property

    Public ReadOnly Property RutaPorCodigo As Boolean
        Get
            Return _rutaPorCodigo
        End Get
    End Property

    Public Sub New(haySeleccion As Boolean, rutaDocumento As String)
        MyBase.New()

        _haySeleccion = haySeleccion
        _rutaPorDefecto = System.IO.Path.GetDirectoryName(rutaDocumento)

        InitializeComponent()
        ConfigurarFormulario()
    End Sub

    Private Sub InitializeComponent()

        Me.Text = "Exportar DXF de Corte - Opciones"
        Me.Width = 500
        Me.Height = 420
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog

        ' GroupBox Modo
        gbModo = New System.Windows.Forms.GroupBox()
        gbModo.Text = "1. Seleccionar piezas a exportar"
        gbModo.Left = 15
        gbModo.Top = 15
        gbModo.Width = 450
        gbModo.Height = 90

        rbTodas = New System.Windows.Forms.RadioButton()
        rbTodas.Text = "Todas las piezas del ensamblaje (desde BOM Solo piezas)"
        rbTodas.Left = 20
        rbTodas.Top = 30
        rbTodas.AutoSize = True
        rbTodas.Checked = True

        rbSeleccion = New System.Windows.Forms.RadioButton()
        rbSeleccion.Text = "Solo los elementos seleccionados"
        rbSeleccion.Left = 20
        rbSeleccion.Top = 55
        rbSeleccion.AutoSize = True
        rbSeleccion.Enabled = _haySeleccion

        gbModo.Controls.Add(rbTodas)
        gbModo.Controls.Add(rbSeleccion)

        ' GroupBox Opciones
        gbOpciones = New System.Windows.Forms.GroupBox()
        gbOpciones.Text = "2. Opciones de exportacion"
        gbOpciones.Left = 15
        gbOpciones.Top = 115
        gbOpciones.Width = 450
        gbOpciones.Height = 80

        chkMarcas = New System.Windows.Forms.CheckBox()
        chkMarcas.Text = "Incluir marcas de plegado (3 marcas de 10 mm, margen 2 mm)"
        chkMarcas.Left = 20
        chkMarcas.Top = 30
        chkMarcas.AutoSize = True
        chkMarcas.Checked = False

        gbOpciones.Controls.Add(chkMarcas)

        ' GroupBox Ruta de salida
        gbRuta = New System.Windows.Forms.GroupBox()
        gbRuta.Text = "3. Ubicacion de archivos DXF"
        gbRuta.Left = 15
        gbRuta.Top = 210
        gbRuta.Width = 450
        gbRuta.Height = 110

        rbRutaPorCodigo = New System.Windows.Forms.RadioButton()
        rbRutaPorCodigo.Text = "Guardar en carpetas por codigo de pieza"
        rbRutaPorCodigo.Left = 20
        rbRutaPorCodigo.Top = 30
        rbRutaPorCodigo.AutoSize = True
        rbRutaPorCodigo.Checked = True

        Dim lblPorCodigo As New System.Windows.Forms.Label()
        lblPorCodigo.Text = "  (.A02000-A02999, .40000-40999, etc. Se crean si no existen)"
        lblPorCodigo.Left = 40
        lblPorCodigo.Top = 55
        lblPorCodigo.AutoSize = True

        rbRutaPersonalizada = New System.Windows.Forms.RadioButton()
        rbRutaPersonalizada.Text = "Guardar en carpeta del ensamblaje (01_DXF_CORTE)"
        rbRutaPersonalizada.Left = 20
        rbRutaPersonalizada.Top = 75
        rbRutaPersonalizada.AutoSize = True

        gbRuta.Controls.Add(rbRutaPorCodigo)
        gbRuta.Controls.Add(lblPorCodigo)
        gbRuta.Controls.Add(rbRutaPersonalizada)

        ' Botones
        btnOK = New System.Windows.Forms.Button()
        btnOK.Text = "Exportar"
        btnOK.DialogResult = System.Windows.Forms.DialogResult.OK
        btnOK.Left = 310
        btnOK.Top = 335
        btnOK.Width = 70
        btnOK.Height = 25

        btnCancel = New System.Windows.Forms.Button()
        btnCancel.Text = "Cancelar"
        btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel
        btnCancel.Left = 390
        btnCancel.Top = 335
        btnCancel.Width = 75
        btnCancel.Height = 25

        Me.Controls.Add(gbModo)
        Me.Controls.Add(gbOpciones)
        Me.Controls.Add(gbRuta)
        Me.Controls.Add(btnOK)
        Me.Controls.Add(btnCancel)

        Me.AcceptButton = btnOK
        Me.CancelButton = btnCancel

    End Sub

    Private Sub ConfigurarFormulario()
        If Not _haySeleccion Then
            rbSeleccion.Enabled = False
        End If
    End Sub

    Private Sub btnOK_Click(sender As Object, e As EventArgs) Handles btnOK.Click

        _exportarSeleccion = rbSeleccion.Checked
        _incluirMarcas = chkMarcas.Checked
        _rutaPorCodigo = rbRutaPorCodigo.Checked

        ' La ruta del documento se usa solo para el modo 01_DXF_CORTE
        ' y para el log. El modo por codigo usa RUTA_BASE_CARPETAS_CODIGO.
        _rutaDestino = _rutaPorDefecto

        If Not System.IO.Directory.Exists(_rutaDestino) Then
            MessageBox.Show("La carpeta del documento no existe:" & vbCrLf & _rutaDestino, "Error de ruta")
            Return
        End If

        Me.DialogResult = System.Windows.Forms.DialogResult.OK
        Me.Close()

    End Sub

End Class

'---------------------------------------------------------
' EXPORTAR DESDE ENSAMBLAJE
'---------------------------------------------------------

Sub ExportarDXFDesdeEnsamblaje(ByVal invApp As Inventor.Application, _
                               ByVal asmDoc As AssemblyDocument, _
                               ByVal rutaCustom As String, _
                               ByVal incluirMarcasPlegado As Boolean, _
                               ByVal rutaPorCodigo As Boolean, _
                               ByVal rutaBaseCodigos As String)

    ' v5.3: la ruta del documento se redirige a Q: si viene de H:.
    rutaCustom = RedirigirRutaHaQ(rutaCustom)

    Dim asmName As String = System.IO.Path.GetFileNameWithoutExtension(asmDoc.FullFileName)

    Dim outputFolder As String = System.IO.Path.Combine(rutaCustom, "01_DXF_CORTE")
    CrearCarpetaSiNoExiste(outputFolder)

    ' v5.3: log con fecha/hora para no machacar exportaciones anteriores.
    Dim fechaLog As String = System.DateTime.Now.ToString("yyyyMMdd_HHmmss")
    Dim logPath As String = System.IO.Path.Combine( _
        outputFolder, _
        "00_LOG_EXPORT_DXF_" & SafeFileName(asmName) & "_" & fechaLog & ".csv")

    Try
        asmDoc.Update()
    Catch
    End Try

    Dim bom As BOM = asmDoc.ComponentDefinition.BOM
    bom.StructuredViewEnabled = True
    bom.StructuredViewFirstLevelOnly = False
    bom.PartsOnlyViewEnabled = True

    Dim partsOnlyView As BOMView = Nothing

    For Each v As BOMView In bom.BOMViews
        Try
            If v.ViewType = BOMViewTypeEnum.kPartsOnlyBOMViewType Then
                partsOnlyView = v
                Exit For
            End If
        Catch
        End Try
    Next

    If partsOnlyView Is Nothing Then
        MessageBox.Show("No se ha encontrado la vista BOM Solo piezas.", "Exportar DXF corte")
        Return
    End If

    Dim usedFileNames As New System.Collections.Generic.Dictionary(Of String, Integer)(System.StringComparer.OrdinalIgnoreCase)

    Dim totalRows As Integer = 0
    Dim exportedCount As Integer = 0
    Dim skippedCount As Integer = 0
    Dim errorCount As Integer = 0
    Dim noCodeCount As Integer = 0
    Dim duplicateCount As Integer = 0

    Dim utf8Bom As New System.Text.UTF8Encoding(True)

    Using sw As New System.IO.StreamWriter(logPath, False, utf8Bom)

        EscribirCabeceraLog(sw)

        For Each row As BOMRow In partsOnlyView.BOMRows

            totalRows += 1

            Dim itemText As String = ""
            Dim qty As String = "1"
            Dim compDef As ComponentDefinition = Nothing
            Dim modelDoc As Document = Nothing

            Try
                itemText = row.ItemNumber.ToString()
            Catch
                itemText = totalRows.ToString()
            End Try

            Try
                qty = row.TotalQuantity.ToString()
            Catch
                Try
                    qty = row.ItemQuantity.ToString()
                Catch
                    qty = "1"
                End Try
            End Try

            If qty Is Nothing Or qty = "" Then qty = "1"

            Try
                compDef = row.ComponentDefinitions.Item(1)
                modelDoc = compDef.Document
            Catch ex As Exception
                errorCount += 1
                sw.WriteLine(MakeCsvLine(itemText, "", "", qty, "", "", "", "", "ERROR", "No se pudo leer la fila de BOM: " & ex.Message))
                Continue For
            End Try

            ProcesarDocumentoParaDXF(modelDoc, itemText, qty, outputFolder, usedFileNames, sw, exportedCount, skippedCount, errorCount, noCodeCount, duplicateCount, incluirMarcasPlegado, rutaPorCodigo, rutaBaseCodigos)

        Next

    End Using

    MessageBox.Show("Exportacion DXF finalizada desde ensamblaje." & vbCrLf & vbCrLf & _
                    "Ensamblaje:" & vbCrLf & asmDoc.DisplayName & vbCrLf & vbCrLf & _
                    "Carpeta DXF:" & vbCrLf & outputFolder & vbCrLf & vbCrLf & _
                    "Marcas de plegado: " & If(incluirMarcasPlegado, "SI, solo en piezas plegadas", "NO") & vbCrLf & _
                    "DXF exportados: " & exportedCount & vbCrLf & _
                    "Piezas omitidas: " & skippedCount & vbCrLf & _
                    "Errores: " & errorCount & vbCrLf & _
                    "Sin codigo: " & noCodeCount & vbCrLf & _
                    "Duplicados: " & duplicateCount & vbCrLf & vbCrLf & _
                    "Log:" & vbCrLf & logPath, _
                    "Exportar DXF corte")

End Sub

'---------------------------------------------------------
' EXPORTAR SOLO LA SELECCION ACTUAL DEL ENSAMBLAJE
'---------------------------------------------------------

Sub ExportarDXFSeleccionEnsamblaje(ByVal invApp As Inventor.Application, _
                                  ByVal asmDoc As AssemblyDocument, _
                                  ByVal rutaCustom As String, _
                                  ByVal incluirMarcasPlegado As Boolean, _
                                  ByVal rutaPorCodigo As Boolean, _
                                  ByVal rutaBaseCodigos As String)

    rutaCustom = RedirigirRutaHaQ(rutaCustom)

    Dim documentos As New System.Collections.Generic.Dictionary(Of String, Document)( _
        System.StringComparer.OrdinalIgnoreCase)

    Dim cantidades As New System.Collections.Generic.Dictionary(Of String, Integer)( _
        System.StringComparer.OrdinalIgnoreCase)

    Dim seleccionValida As Integer = 0
    Dim seleccionNoValida As Integer = 0
    Dim seleccionSuprimida As Integer = 0

    For Each objetoSeleccionado As Object In asmDoc.SelectSet

        Dim occ As ComponentOccurrence = Nothing

        Try
            occ = TryCast(objetoSeleccionado, ComponentOccurrence)
        Catch
            occ = Nothing
        End Try

        If occ Is Nothing Then
            seleccionNoValida += 1
            Continue For
        End If

        Try
            If occ.Suppressed Then
                seleccionSuprimida += 1
                Continue For
            End If
        Catch
        End Try

        seleccionValida += 1
        RecogerPiezasDesdeOcurrencia(occ, documentos, cantidades, seleccionSuprimida)

    Next

    If seleccionValida = 0 Then
        MessageBox.Show( _
            "La seleccion actual no contiene componentes validos." & vbCrLf & vbCrLf & _
            "Selecciona piezas o subconjuntos en el navegador o en la ventana grafica del ensamblaje.", _
            "Exportar DXF seleccion")
        Return
    End If

    If documentos.Count = 0 Then
        MessageBox.Show( _
            "No se han encontrado piezas dentro de la seleccion." & vbCrLf & vbCrLf & _
            "Elementos no validos: " & seleccionNoValida.ToString() & vbCrLf & _
            "Ocurrencias suprimidas: " & seleccionSuprimida.ToString(), _
            "Exportar DXF seleccion")
        Return
    End If

    Dim asmName As String = System.IO.Path.GetFileNameWithoutExtension(asmDoc.FullFileName)

    Dim outputFolder As String = System.IO.Path.Combine(rutaCustom, "01_DXF_CORTE")
    CrearCarpetaSiNoExiste(outputFolder)

    Dim fechaLog As String = System.DateTime.Now.ToString("yyyyMMdd_HHmmss")
    Dim logPath As String = System.IO.Path.Combine( _
        outputFolder, _
        "00_LOG_EXPORT_DXF_SELECCION_" & SafeFileName(asmName) & "_" & fechaLog & ".csv")

    Dim usedFileNames As New System.Collections.Generic.Dictionary(Of String, Integer)( _
        System.StringComparer.OrdinalIgnoreCase)

    Dim exportedCount As Integer = 0
    Dim skippedCount As Integer = 0
    Dim errorCount As Integer = 0
    Dim noCodeCount As Integer = 0
    Dim duplicateCount As Integer = 0
    Dim totalProcesadas As Integer = 0

    Dim utf8Bom As New System.Text.UTF8Encoding(True)

    Using sw As New System.IO.StreamWriter(logPath, False, utf8Bom)

        EscribirCabeceraLog(sw)

        For Each rutaDocumento As String In documentos.Keys

            totalProcesadas += 1

            Dim modelDoc As Document = documentos(rutaDocumento)
            Dim qty As String = "1"

            If cantidades.ContainsKey(rutaDocumento) Then
                qty = cantidades(rutaDocumento).ToString()
            End If

            ProcesarDocumentoParaDXF( _
                modelDoc, _
                "SEL-" & totalProcesadas.ToString(), _
                qty, _
                outputFolder, _
                usedFileNames, _
                sw, _
                exportedCount, _
                skippedCount, _
                errorCount, _
                noCodeCount, _
                duplicateCount, _
                incluirMarcasPlegado, _
                rutaPorCodigo, _
                rutaBaseCodigos)

        Next

    End Using

    MessageBox.Show( _
        "Exportacion DXF de la seleccion finalizada." & vbCrLf & vbCrLf & _
        "Ensamblaje:" & vbCrLf & asmDoc.DisplayName & vbCrLf & vbCrLf & _
        "Componentes seleccionados validos: " & seleccionValida.ToString() & vbCrLf & _
        "Piezas distintas encontradas: " & documentos.Count.ToString() & vbCrLf & _
        "Marcas de plegado: " & If(incluirMarcasPlegado, "SI, solo en piezas plegadas", "NO") & vbCrLf & _
        "DXF exportados: " & exportedCount.ToString() & vbCrLf & _
        "Piezas omitidas: " & skippedCount.ToString() & vbCrLf & _
        "Errores: " & errorCount.ToString() & vbCrLf & _
        "Sin codigo: " & noCodeCount.ToString() & vbCrLf & _
        "Duplicados de codigo: " & duplicateCount.ToString() & vbCrLf & _
        "Seleccion no valida: " & seleccionNoValida.ToString() & vbCrLf & _
        "Ocurrencias suprimidas: " & seleccionSuprimida.ToString() & vbCrLf & vbCrLf & _
        "Carpeta DXF:" & vbCrLf & outputFolder & vbCrLf & vbCrLf & _
        "Log:" & vbCrLf & logPath, _
        "Exportar DXF seleccion")

End Sub

'---------------------------------------------------------
' RECORRER UNA OCURRENCIA SELECCIONADA
'---------------------------------------------------------

Sub RecogerPiezasDesdeOcurrencia( _
        ByVal occ As ComponentOccurrence, _
        ByVal documentos As System.Collections.Generic.Dictionary(Of String, Document), _
        ByVal cantidades As System.Collections.Generic.Dictionary(Of String, Integer), _
        ByRef seleccionSuprimida As Integer)

    If occ Is Nothing Then Exit Sub

    Try
        If occ.Suppressed Then
            seleccionSuprimida += 1
            Exit Sub
        End If
    Catch
    End Try

    Dim docOcc As Document = Nothing

    Try
        docOcc = occ.Definition.Document
    Catch
        docOcc = Nothing
    End Try

    If docOcc Is Nothing Then Exit Sub

    If docOcc.DocumentType = DocumentTypeEnum.kPartDocumentObject Then

        Dim clave As String = ""

        Try
            clave = docOcc.FullFileName
        Catch
            clave = ""
        End Try

        If clave Is Nothing OrElse clave.Trim() = "" Then
            clave = docOcc.DisplayName
        End If

        If Not documentos.ContainsKey(clave) Then
            documentos.Add(clave, docOcc)
            cantidades.Add(clave, 1)
        Else
            cantidades(clave) = cantidades(clave) + 1
        End If

        Exit Sub

    End If

    If docOcc.DocumentType = DocumentTypeEnum.kAssemblyDocumentObject Then

        Try
            For Each subOcc As ComponentOccurrence In occ.SubOccurrences
                RecogerPiezasDesdeOcurrencia( _
                    subOcc, documentos, cantidades, seleccionSuprimida)
            Next
        Catch
        End Try

    End If

End Sub

'---------------------------------------------------------
' EXPORTAR DESDE PIEZA SUELTA
'---------------------------------------------------------

Sub ExportarDXFDesdePieza(ByVal invApp As Inventor.Application, _
                          ByVal partDoc As PartDocument, _
                          ByVal rutaCustom As String, _
                          ByVal incluirMarcasPlegado As Boolean, _
                          ByVal rutaPorCodigo As Boolean, _
                          ByVal rutaBaseCodigos As String)

    rutaCustom = RedirigirRutaHaQ(rutaCustom)

    Dim partName As String = System.IO.Path.GetFileNameWithoutExtension(partDoc.FullFileName)

    Dim outputFolder As String = System.IO.Path.Combine(rutaCustom, "01_DXF_CORTE")
    CrearCarpetaSiNoExiste(outputFolder)

    Dim fechaLog As String = System.DateTime.Now.ToString("yyyyMMdd_HHmmss")
    Dim logPath As String = System.IO.Path.Combine( _
        outputFolder, _
        "00_LOG_EXPORT_DXF_" & SafeFileName(partName) & "_" & fechaLog & ".csv")

    Dim usedFileNames As New System.Collections.Generic.Dictionary(Of String, Integer)(System.StringComparer.OrdinalIgnoreCase)

    Dim exportedCount As Integer = 0
    Dim skippedCount As Integer = 0
    Dim errorCount As Integer = 0
    Dim noCodeCount As Integer = 0
    Dim duplicateCount As Integer = 0

    Dim utf8Bom As New System.Text.UTF8Encoding(True)

    Using sw As New System.IO.StreamWriter(logPath, False, utf8Bom)

        EscribirCabeceraLog(sw)
        ProcesarDocumentoParaDXF(partDoc, "1", "1", outputFolder, usedFileNames, sw, exportedCount, skippedCount, errorCount, noCodeCount, duplicateCount, incluirMarcasPlegado, rutaPorCodigo, rutaBaseCodigos)

    End Using

    MessageBox.Show("Exportacion DXF finalizada desde pieza suelta." & vbCrLf & vbCrLf & _
                    "Pieza:" & vbCrLf & partDoc.DisplayName & vbCrLf & vbCrLf & _
                    "Carpeta DXF:" & vbCrLf & outputFolder & vbCrLf & vbCrLf & _
                    "Marcas de plegado: " & If(incluirMarcasPlegado, "SI", "NO") & vbCrLf & _
                    "DXF exportados: " & exportedCount & vbCrLf & _
                    "Piezas omitidas: " & skippedCount & vbCrLf & _
                    "Errores: " & errorCount & vbCrLf & _
                    "Sin codigo: " & noCodeCount & vbCrLf & vbCrLf & _
                    "Log:" & vbCrLf & logPath, _
                    "Exportar DXF corte")

End Sub

'---------------------------------------------------------
' PROCESAR DOCUMENTO PARA DXF
'---------------------------------------------------------

Sub ProcesarDocumentoParaDXF(ByVal modelDoc As Document, _
                             ByVal itemText As String, _
                             ByVal qty As String, _
                             ByVal outputFolder As String, _
                             ByVal usedFileNames As System.Collections.Generic.Dictionary(Of String, Integer), _
                             ByVal sw As System.IO.StreamWriter, _
                             ByRef exportedCount As Integer, _
                             ByRef skippedCount As Integer, _
                             ByRef errorCount As Integer, _
                             ByRef noCodeCount As Integer, _
                             ByRef duplicateCount As Integer, _
                             ByVal incluirMarcasPlegado As Boolean, _
                             ByVal rutaPorCodigo As Boolean, _
                             ByVal rutaBaseCodigos As String)

    Dim partNumber As String = ""
    Dim description As String = ""
    Dim material As String = ""
    Dim thickness As String = ""
    Dim sourceFile As String = ""
    Dim dxfPath As String = ""
    Dim estado As String = ""
    Dim observacion As String = ""

    Try
        If modelDoc Is Nothing Then
            estado = "ERROR"
            observacion = "Documento de modelo no disponible."
            errorCount += 1
            sw.WriteLine(MakeCsvLine(itemText, partNumber, description, qty, material, thickness, sourceFile, dxfPath, estado, observacion))
            Exit Sub
        End If

        sourceFile = modelDoc.DisplayName

        Try
            If modelDoc.FullFileName <> "" Then sourceFile = modelDoc.FullFileName
        Catch
        End Try

        If modelDoc.DocumentType <> DocumentTypeEnum.kPartDocumentObject Then
            estado = "OMITIDA"
            observacion = "No es pieza IPT."
            skippedCount += 1
            sw.WriteLine(MakeCsvLine(itemText, partNumber, description, qty, material, thickness, sourceFile, dxfPath, estado, observacion))
            Exit Sub
        End If

        Dim partDoc As PartDocument = TryCast(modelDoc, PartDocument)

        If partDoc Is Nothing Then
            estado = "OMITIDA"
            observacion = "No se pudo convertir a PartDocument."
            skippedCount += 1
            sw.WriteLine(MakeCsvLine(itemText, partNumber, description, qty, material, thickness, sourceFile, dxfPath, estado, observacion))
            Exit Sub
        End If

        partNumber = GetProp(modelDoc, "Design Tracking Properties", "Part Number")
        description = GetProp(modelDoc, "Design Tracking Properties", "Description")
        material = GetMaterialName(partDoc)

        If partNumber Is Nothing Then partNumber = ""
        partNumber = Trim(partNumber)

        If partNumber = "" Then
            partNumber = System.IO.Path.GetFileNameWithoutExtension(modelDoc.FullFileName)
            If partNumber = "" Then partNumber = modelDoc.DisplayName.Replace(".ipt", "").Replace(".IPT", "")
            noCodeCount += 1
            observacion = AddObs(observacion, "Sin numero de pieza. Se usa nombre de archivo.")
        End If

        Dim smDef As SheetMetalComponentDefinition = Nothing

        Try
            smDef = TryCast(partDoc.ComponentDefinition, SheetMetalComponentDefinition)
        Catch
            smDef = Nothing
        End Try

        If smDef Is Nothing Then
            estado = "OMITIDA"
            observacion = AddObs(observacion, "La pieza no es chapa metalica.")
            skippedCount += 1
            sw.WriteLine(MakeCsvLine(itemText, partNumber, description, qty, material, thickness, sourceFile, dxfPath, estado, observacion))
            Exit Sub
        End If

        '-----------------------------------------------------
        ' v5.3: CODIGO DE CORTE DESDE EL NOMBRE DEL ARCHIVO
        '-----------------------------------------------------
        ' Si el nombre del archivo (o el numero de pieza) tiene el
        ' formato "43716-70831" (dos numeros separados por guion),
        ' el DXF se nombra con el numero de DESPUES del guion, que
        ' es el codigo de corte: 70831.dxf
        Dim codigoParaDXF As String = partNumber

        Dim nombreArchivoSinExt As String = ""
        Try
            nombreArchivoSinExt = System.IO.Path.GetFileNameWithoutExtension(modelDoc.FullFileName)
        Catch
            nombreArchivoSinExt = ""
        End Try

        Dim codigoCorte As String = ObtenerCodigoCorteDesdeNombre(nombreArchivoSinExt)
        If codigoCorte = "" Then codigoCorte = ObtenerCodigoCorteDesdeNombre(partNumber)

        If codigoCorte <> "" Then
            codigoParaDXF = codigoCorte
            observacion = AddObs(observacion, _
                "Nombre tipo XXXXX-YYYYY: el DXF se nombra con el codigo de corte " & codigoCorte & ".")
        End If

        ' Si se selecciona ruta por codigo, buscar/crear la carpeta de rango
        ' en la RAIZ configurada (no en la carpeta del ensamblaje).
        Dim carpetaFinal As String = outputFolder
        If rutaPorCodigo Then
            Dim carpetaCodigo As String = BuscarCarpetaPorCodigo(codigoParaDXF, rutaBaseCodigos)
            If carpetaCodigo <> "" Then
                carpetaFinal = System.IO.Path.Combine(carpetaCodigo, "01_DXF_CORTE")
                CrearCarpetaSiNoExiste(carpetaFinal)
            Else
                observacion = AddObs(observacion, _
                    "SIN CARPETA DE CODIGO: no se pudo determinar carpeta de rango en " & _
                    rutaBaseCodigos & ". Se guarda en " & outputFolder & ".")
            End If
        End If

        thickness = GetSheetMetalThicknessMm(smDef)

        Try
            If Not smDef.HasFlatPattern Then
                smDef.Unfold()
            End If

            Try
                smDef.FlatPattern.ExitEdit()
            Catch
            End Try

        Catch ex As Exception
            estado = "ERROR"
            observacion = AddObs(observacion, "No se pudo crear/actualizar el desarrollo: " & ex.Message)
            errorCount += 1
            sw.WriteLine(MakeCsvLine(itemText, partNumber, description, qty, material, thickness, sourceFile, dxfPath, estado, observacion))
            Exit Sub
        End Try

        Dim cleanCode As String = SafeFileName(codigoParaDXF)
        If cleanCode = "" Then cleanCode = "SIN_CODIGO_" & itemText

        Dim baseFileName As String = cleanCode & ".dxf"
        Dim dxfFileName As String = baseFileName

        If usedFileNames.ContainsKey(baseFileName) Then
            usedFileNames(baseFileName) = usedFileNames(baseFileName) + 1
            dxfFileName = cleanCode & "_DUP" & usedFileNames(baseFileName).ToString() & ".dxf"
            duplicateCount += 1
            observacion = AddObs(observacion, "Codigo duplicado. Se anade sufijo DUP.")
        Else
            usedFileNames.Add(baseFileName, 1)
        End If

        dxfPath = System.IO.Path.Combine(carpetaFinal, dxfFileName)

        Dim piezaTienePlegados As Boolean = DocumentoTienePlegados(partDoc)
        Dim aplicarMarcas As Boolean = incluirMarcasPlegado AndAlso piezaTienePlegados

        Try
            If aplicarMarcas Then

                Dim rutaTemporal As String = dxfPath & ".tmp_completo.dxf"

                Dim opcionesTemporales As String = _
                    "FLAT PATTERN DXF?" & _
                    "AcadVersion=2004" & _
                    "&OuterProfileLayer=IV_OUTER_PROFILE" & _
                    "&InteriorProfilesLayer=IV_INTERIOR_PROFILES" & _
                    "&BendLayer=IV_BEND" & _
                    "&BendUpLayer=IV_BEND_UP" & _
                    "&BendDownLayer=IV_BEND_DOWN" & _
                    "&InvisibleLayers=IV_TANGENT;IV_TOOL_CENTER;IV_TOOL_CENTER_UP;IV_TOOL_CENTER_DOWN;IV_ARC_CENTERS;IV_FEATURE_PROFILES;IV_FEATURE_PROFILES_UP;IV_FEATURE_PROFILES_DOWN;IV_UNCONSUMED_SKETCHES"

                If System.IO.File.Exists(rutaTemporal) Then
                    System.IO.File.Delete(rutaTemporal)
                End If

                smDef.DataIO.WriteDataToFile(opcionesTemporales, rutaTemporal)

                Dim detalleConversion As String = ""

                If ConvertirDXFInventorAR12ConMarcas( _
                        rutaTemporal, _
                        dxfPath, _
                        10.0, _
                        2.0, _
                        detalleConversion) Then

                    observacion = AddObs(observacion, _
                        "DXF R12 generado con 3 marcas de 10 mm por plegado y margen de 2 mm.")

                Else

                    Dim opcionesRespaldo As String = _
                        "FLAT PATTERN DXF?" & _
                        "AcadVersion=2004" & _
                        "&OuterProfileLayer=IV_OUTER_PROFILE" & _
                        "&InteriorProfilesLayer=IV_INTERIOR_PROFILES" & _
                        "&InvisibleLayers=IV_TANGENT;IV_BEND;IV_BEND_UP;IV_BEND_DOWN;IV_TOOL_CENTER;IV_TOOL_CENTER_UP;IV_TOOL_CENTER_DOWN;IV_ARC_CENTERS;IV_FEATURE_PROFILES;IV_FEATURE_PROFILES_UP;IV_FEATURE_PROFILES_DOWN;IV_UNCONSUMED_SKETCHES"

                    smDef.DataIO.WriteDataToFile(opcionesRespaldo, dxfPath)

                    observacion = AddObs(observacion, _
                        "No se pudieron generar las marcas. Se exporto DXF limpio de respaldo. Detalle: " & detalleConversion)

                End If

                Try
                    If System.IO.File.Exists(rutaTemporal) Then
                        System.IO.File.Delete(rutaTemporal)
                    End If
                Catch
                End Try

            Else

                Dim opcionesLimpias As String = _
                    "FLAT PATTERN DXF?" & _
                    "AcadVersion=2004" & _
                    "&OuterProfileLayer=IV_OUTER_PROFILE" & _
                    "&InteriorProfilesLayer=IV_INTERIOR_PROFILES" & _
                    "&InvisibleLayers=IV_TANGENT;IV_BEND;IV_BEND_UP;IV_BEND_DOWN;IV_TOOL_CENTER;IV_TOOL_CENTER_UP;IV_TOOL_CENTER_DOWN;IV_ARC_CENTERS;IV_FEATURE_PROFILES;IV_FEATURE_PROFILES_UP;IV_FEATURE_PROFILES_DOWN;IV_UNCONSUMED_SKETCHES"

                smDef.DataIO.WriteDataToFile(opcionesLimpias, dxfPath)

                If piezaTienePlegados Then
                    observacion = AddObs(observacion, "DXF generado sin marcas de plegado.")
                Else
                    observacion = AddObs(observacion, "Pieza plana de corte: DXF generado sin marcas.")
                End If

            End If

            estado = "EXPORTADO"
            exportedCount += 1
            observacion = AddObs(observacion, "DXF generado correctamente.")

        Catch ex As Exception
            estado = "ERROR"
            observacion = AddObs(observacion, "Error exportando DXF: " & ex.Message)
            errorCount += 1
        End Try

        sw.WriteLine(MakeCsvLine(itemText, partNumber, description, qty, material, thickness, sourceFile, dxfPath, estado, observacion))

    Catch ex As Exception
        estado = "ERROR"
        observacion = AddObs(observacion, "Error inesperado procesando documento: " & ex.Message)
        errorCount += 1
        sw.WriteLine(MakeCsvLine(itemText, partNumber, description, qty, material, thickness, sourceFile, dxfPath, estado, observacion))
    End Try

End Sub

'---------------------------------------------------------
' v5.3: CODIGO DE CORTE DESDE NOMBRE "XXXXX-YYYYY"
'---------------------------------------------------------
' Si el texto es exactamente dos numeros separados por guion
' (con espacios opcionales alrededor del guion), devuelve el
' segundo numero (el codigo de corte). Si no, devuelve "".
' Ejemplos:
'   "43716-70831"    -> "70831"
'   "43716 - 70831"  -> "70831"
'   "43716-43714"    -> "43714"
'   "A02893"         -> ""  (no cambia nada)
'   "40563 - PILAR"  -> ""  (tiene texto, no aplica)

Function ObtenerCodigoCorteDesdeNombre(ByVal nombre As String) As String

    Try
        If nombre Is Nothing Then Return ""

        nombre = Trim(nombre)
        If nombre = "" Then Return ""

        Dim m As System.Text.RegularExpressions.Match = _
            System.Text.RegularExpressions.Regex.Match( _
                nombre, "^(\d+)\s*-\s*(\d+)$")

        If Not m.Success Then Return ""

        Return m.Groups(2).Value

    Catch
        Return ""
    End Try

End Function

'---------------------------------------------------------
' DETECCION DE PLEGADOS
'---------------------------------------------------------

Function DocumentoTienePlegados(ByVal modelDoc As Document) As Boolean

    Try
        If modelDoc Is Nothing Then Return False
        If modelDoc.DocumentType <> DocumentTypeEnum.kPartDocumentObject Then Return False

        Dim partDoc As PartDocument = TryCast(modelDoc, PartDocument)
        If partDoc Is Nothing Then Return False

        Dim smDef As SheetMetalComponentDefinition = _
            TryCast(partDoc.ComponentDefinition, SheetMetalComponentDefinition)

        If smDef Is Nothing Then Return False

        If Not smDef.HasFlatPattern Then
            Try
                smDef.Unfold()
                Try
                    smDef.FlatPattern.ExitEdit()
                Catch
                End Try
            Catch
                Return False
            End Try
        End If

        Try
            Dim flatPatternObj As Object = smDef.FlatPattern
            Dim resultadosObj As Object = Microsoft.VisualBasic.Interaction.CallByName( _
                flatPatternObj, _
                "FlatBendResults", _
                Microsoft.VisualBasic.CallType.Get)

            If resultadosObj Is Nothing Then Return False

            Dim cantidadObj As Object = Microsoft.VisualBasic.Interaction.CallByName( _
                resultadosObj, _
                "Count", _
                Microsoft.VisualBasic.CallType.Get)

            If cantidadObj Is Nothing Then Return False

            Return CInt(cantidadObj) > 0

        Catch
            Return False
        End Try

    Catch
        Return False
    End Try

End Function

'---------------------------------------------------------
' CONVERTIR DXF COMPLETO DE INVENTOR A DXF R12 LIMPIO CON MARCAS
'---------------------------------------------------------

Function ConvertirDXFInventorAR12ConMarcas( _
    ByVal rutaOrigen As String, _
    ByVal rutaDestino As String, _
    ByVal longitudMarcaMm As Double, _
    ByVal margenBordeMm As Double, _
    ByRef detalle As String) As Boolean

    detalle = ""

    Try
        If Not System.IO.File.Exists(rutaOrigen) Then
            detalle = "No existe el DXF temporal."
            Return False
        End If

        Dim lineas() As String = System.IO.File.ReadAllLines( _
            rutaOrigen, _
            System.Text.Encoding.Default)

        If lineas.Length < 2 Then
            detalle = "El DXF temporal esta vacio."
            Return False
        End If

        If (lineas.Length Mod 2) <> 0 Then
            detalle = "El DXF temporal no tiene pares codigo/valor completos."
            Return False
        End If

        Dim entidades As New System.Collections.Generic.List(Of DXFEntidadSimple)
        Dim i As Integer = 0
        Dim dentroEntidades As Boolean = False

        While i <= lineas.Length - 2

            Dim codigo As String = lineas(i).Trim()
            Dim valor As String = lineas(i + 1).Trim()

            If codigo = "0" AndAlso valor.ToUpperInvariant() = "SECTION" Then

                If i + 3 <= lineas.Length - 1 AndAlso _
                   lineas(i + 2).Trim() = "2" AndAlso _
                   lineas(i + 3).Trim().ToUpperInvariant() = "ENTITIES" Then

                    dentroEntidades = True
                    i += 4
                    Continue While
                End If

            End If

            If dentroEntidades AndAlso codigo = "0" AndAlso valor.ToUpperInvariant() = "ENDSEC" Then
                Exit While
            End If

            If dentroEntidades AndAlso codigo = "0" Then

                Dim tipoEntidad As String = valor.ToUpperInvariant()
                Dim grupos As New System.Collections.Generic.Dictionary(Of String, String)

                i += 2

                While i <= lineas.Length - 2 AndAlso lineas(i).Trim() <> "0"
                    Dim codGrupo As String = lineas(i).Trim()
                    Dim valorGrupo As String = lineas(i + 1).Trim()

                    If Not grupos.ContainsKey(codGrupo) Then
                        grupos.Add(codGrupo, valorGrupo)
                    End If

                    i += 2
                End While

                Dim capa As String = ObtenerGrupoDXF(grupos, "8").ToUpperInvariant()

                If capa = "IV_OUTER_PROFILE" OrElse _
                   capa = "IV_INTERIOR_PROFILES" Then

                    AgregarEntidadPerfilR12(tipoEntidad, grupos, capa, entidades)

                ElseIf capa = "IV_BEND" OrElse _
                       capa = "IV_BEND_UP" OrElse _
                       capa = "IV_BEND_DOWN" Then

                    If tipoEntidad = "LINE" Then
                        AgregarMarcasPlegadoR12( _
                            grupos, _
                            capa, _
                            longitudMarcaMm, _
                            margenBordeMm, _
                            entidades)
                    End If

                End If

                Continue While

            End If

            i += 2

        End While

        If entidades.Count = 0 Then
            detalle = "No se encontro geometria valida en las capas de corte."
            Return False
        End If

        EscribirDXFR12(rutaDestino, entidades)

        detalle = "OK"
        Return True

    Catch ex As System.Exception
        detalle = ex.Message
        Return False
    End Try

End Function

Public Class DXFEntidadSimple
    Public Tipo As String
    Public Capa As String

    Public X1 As Double
    Public Y1 As Double
    Public Z1 As Double

    Public X2 As Double
    Public Y2 As Double
    Public Z2 As Double

    Public CentroX As Double
    Public CentroY As Double
    Public CentroZ As Double

    Public Radio As Double
    Public AnguloInicio As Double
    Public AnguloFin As Double
End Class

Sub AgregarEntidadPerfilR12( _
    ByVal tipoEntidad As String, _
    ByVal grupos As System.Collections.Generic.Dictionary(Of String, String), _
    ByVal capa As String, _
    ByVal entidades As System.Collections.Generic.List(Of DXFEntidadSimple))

    Try
        If tipoEntidad = "LINE" Then

            Dim e As New DXFEntidadSimple
            e.Tipo = "LINE"
            e.Capa = capa

            e.X1 = LeerDoubleGrupoDXF(grupos, "10")
            e.Y1 = LeerDoubleGrupoDXF(grupos, "20")
            e.Z1 = LeerDoubleGrupoDXF(grupos, "30")

            e.X2 = LeerDoubleGrupoDXF(grupos, "11")
            e.Y2 = LeerDoubleGrupoDXF(grupos, "21")
            e.Z2 = LeerDoubleGrupoDXF(grupos, "31")

            entidades.Add(e)
            Exit Sub
        End If

        If tipoEntidad = "ARC" Then

            Dim e As New DXFEntidadSimple
            e.Tipo = "ARC"
            e.Capa = capa

            e.CentroX = LeerDoubleGrupoDXF(grupos, "10")
            e.CentroY = LeerDoubleGrupoDXF(grupos, "20")
            e.CentroZ = LeerDoubleGrupoDXF(grupos, "30")

            e.Radio = LeerDoubleGrupoDXF(grupos, "40")
            e.AnguloInicio = LeerDoubleGrupoDXF(grupos, "50")
            e.AnguloFin = LeerDoubleGrupoDXF(grupos, "51")

            entidades.Add(e)
            Exit Sub
        End If

        If tipoEntidad = "CIRCLE" Then

            Dim e As New DXFEntidadSimple
            e.Tipo = "CIRCLE"
            e.Capa = capa

            e.CentroX = LeerDoubleGrupoDXF(grupos, "10")
            e.CentroY = LeerDoubleGrupoDXF(grupos, "20")
            e.CentroZ = LeerDoubleGrupoDXF(grupos, "30")

            e.Radio = LeerDoubleGrupoDXF(grupos, "40")

            entidades.Add(e)
            Exit Sub
        End If

    Catch
    End Try

End Sub

Sub AgregarMarcasPlegadoR12( _
    ByVal grupos As System.Collections.Generic.Dictionary(Of String, String), _
    ByVal capa As String, _
    ByVal longitudMarcaMm As Double, _
    ByVal margenBordeMm As Double, _
    ByVal entidades As System.Collections.Generic.List(Of DXFEntidadSimple))

    Try
        Dim x1 As Double = LeerDoubleGrupoDXF(grupos, "10")
        Dim y1 As Double = LeerDoubleGrupoDXF(grupos, "20")
        Dim z1 As Double = LeerDoubleGrupoDXF(grupos, "30")

        Dim x2 As Double = LeerDoubleGrupoDXF(grupos, "11")
        Dim y2 As Double = LeerDoubleGrupoDXF(grupos, "21")
        Dim z2 As Double = LeerDoubleGrupoDXF(grupos, "31")

        Dim dx As Double = x2 - x1
        Dim dy As Double = y2 - y1
        Dim dz As Double = z2 - z1

        Dim longitud As Double = Math.Sqrt((dx * dx) + (dy * dy) + (dz * dz))

        If longitud <= 0.000001 Then Exit Sub

        Dim margen As Double = margenBordeMm / longitud
        Dim marca As Double = Math.Min( _
            longitudMarcaMm, _
            Math.Max(0.0, longitud - (2.0 * margenBordeMm))) / longitud

        If marca <= 0.0 Then Exit Sub

        Dim inicioA As Double = margen
        Dim inicioB As Double = Math.Min(1.0, margen + marca)

        Dim centroA As Double = Math.Max(0.0, 0.5 - (marca / 2.0))
        Dim centroB As Double = Math.Min(1.0, 0.5 + (marca / 2.0))

        Dim finalB As Double = Math.Max(0.0, 1.0 - margen)
        Dim finalA As Double = Math.Max(0.0, finalB - marca)

        AgregarLineaInterpoladaR12( _
            capa, x1, y1, z1, x2, y2, z2, inicioA, inicioB, entidades)

        AgregarLineaInterpoladaR12( _
            capa, x1, y1, z1, x2, y2, z2, centroA, centroB, entidades)

        AgregarLineaInterpoladaR12( _
            capa, x1, y1, z1, x2, y2, z2, finalA, finalB, entidades)

    Catch
    End Try

End Sub

Sub AgregarLineaInterpoladaR12( _
    ByVal capa As String, _
    ByVal x1 As Double, _
    ByVal y1 As Double, _
    ByVal z1 As Double, _
    ByVal x2 As Double, _
    ByVal y2 As Double, _
    ByVal z2 As Double, _
    ByVal tInicio As Double, _
    ByVal tFin As Double, _
    ByVal entidades As System.Collections.Generic.List(Of DXFEntidadSimple))

    If tInicio < 0.0 Then tInicio = 0.0
    If tInicio > 1.0 Then tInicio = 1.0
    If tFin < 0.0 Then tFin = 0.0
    If tFin > 1.0 Then tFin = 1.0

    If tFin <= tInicio Then Exit Sub

    Dim e As New DXFEntidadSimple
    e.Tipo = "LINE"
    e.Capa = capa

    e.X1 = x1 + ((x2 - x1) * tInicio)
    e.Y1 = y1 + ((y2 - y1) * tInicio)
    e.Z1 = z1 + ((z2 - z1) * tInicio)

    e.X2 = x1 + ((x2 - x1) * tFin)
    e.Y2 = y1 + ((y2 - y1) * tFin)
    e.Z2 = z1 + ((z2 - z1) * tFin)

    entidades.Add(e)

End Sub

Function ObtenerGrupoDXF( _
    ByVal grupos As System.Collections.Generic.Dictionary(Of String, String), _
    ByVal codigo As String) As String

    Try
        If grupos.ContainsKey(codigo) Then Return grupos(codigo)
    Catch
    End Try

    Return ""

End Function

Function LeerDoubleGrupoDXF( _
    ByVal grupos As System.Collections.Generic.Dictionary(Of String, String), _
    ByVal codigo As String) As Double

    Try
        Dim texto As String = ObtenerGrupoDXF(grupos, codigo)
        If texto = "" Then Return 0.0

        Dim valor As Double = 0.0

        If Double.TryParse( _
            texto, _
            System.Globalization.NumberStyles.Float, _
            System.Globalization.CultureInfo.InvariantCulture, _
            valor) Then

            Return valor
        End If

    Catch
    End Try

    Return 0.0

End Function

Sub EscribirDXFR12( _
    ByVal rutaDestino As String, _
    ByVal entidades As System.Collections.Generic.List(Of DXFEntidadSimple))

    Dim capas As New System.Collections.Generic.HashSet(Of String)( _
        System.StringComparer.OrdinalIgnoreCase)

    For Each e As DXFEntidadSimple In entidades
        If e.Capa Is Nothing OrElse e.Capa.Trim() = "" Then
            capas.Add("0")
        Else
            capas.Add(e.Capa.Trim())
        End If
    Next

    Dim salida As New System.Collections.Generic.List(Of String)

    AgregarParDXF(salida, 0, "SECTION")
    AgregarParDXF(salida, 2, "HEADER")
    AgregarParDXF(salida, 9, "$ACADVER")
    AgregarParDXF(salida, 1, "AC1009")
    AgregarParDXF(salida, 0, "ENDSEC")

    AgregarParDXF(salida, 0, "SECTION")
    AgregarParDXF(salida, 2, "TABLES")
    AgregarParDXF(salida, 0, "TABLE")
    AgregarParDXF(salida, 2, "LAYER")
    AgregarParDXF(salida, 70, capas.Count.ToString())

    For Each capa As String In capas
        AgregarParDXF(salida, 0, "LAYER")
        AgregarParDXF(salida, 2, capa)
        AgregarParDXF(salida, 70, "0")

        If capa.ToUpperInvariant().Contains("BEND") Then
            AgregarParDXF(salida, 62, "1")
        Else
            AgregarParDXF(salida, 62, "7")
        End If

        AgregarParDXF(salida, 6, "CONTINUOUS")
    Next

    AgregarParDXF(salida, 0, "ENDTAB")
    AgregarParDXF(salida, 0, "ENDSEC")

    AgregarParDXF(salida, 0, "SECTION")
    AgregarParDXF(salida, 2, "ENTITIES")

    For Each e As DXFEntidadSimple In entidades

        If e.Tipo = "LINE" Then

            AgregarParDXF(salida, 0, "LINE")
            AgregarParDXF(salida, 8, e.Capa)

            AgregarParDXF(salida, 10, FormatearNumeroDXF(e.X1))
            AgregarParDXF(salida, 20, FormatearNumeroDXF(e.Y1))
            AgregarParDXF(salida, 30, FormatearNumeroDXF(e.Z1))

            AgregarParDXF(salida, 11, FormatearNumeroDXF(e.X2))
            AgregarParDXF(salida, 21, FormatearNumeroDXF(e.Y2))
            AgregarParDXF(salida, 31, FormatearNumeroDXF(e.Z2))

        ElseIf e.Tipo = "ARC" Then

            AgregarParDXF(salida, 0, "ARC")
            AgregarParDXF(salida, 8, e.Capa)

            AgregarParDXF(salida, 10, FormatearNumeroDXF(e.CentroX))
            AgregarParDXF(salida, 20, FormatearNumeroDXF(e.CentroY))
            AgregarParDXF(salida, 30, FormatearNumeroDXF(e.CentroZ))

            AgregarParDXF(salida, 40, FormatearNumeroDXF(e.Radio))
            AgregarParDXF(salida, 50, FormatearNumeroDXF(e.AnguloInicio))
            AgregarParDXF(salida, 51, FormatearNumeroDXF(e.AnguloFin))

        ElseIf e.Tipo = "CIRCLE" Then

            AgregarParDXF(salida, 0, "CIRCLE")
            AgregarParDXF(salida, 8, e.Capa)

            AgregarParDXF(salida, 10, FormatearNumeroDXF(e.CentroX))
            AgregarParDXF(salida, 20, FormatearNumeroDXF(e.CentroY))
            AgregarParDXF(salida, 30, FormatearNumeroDXF(e.CentroZ))

            AgregarParDXF(salida, 40, FormatearNumeroDXF(e.Radio))

        End If

    Next

    AgregarParDXF(salida, 0, "ENDSEC")
    AgregarParDXF(salida, 0, "EOF")

    Dim contenido As String = String.Join(vbCrLf, salida.ToArray()) & vbCrLf

    System.IO.File.WriteAllText( _
        rutaDestino, _
        contenido, _
        System.Text.Encoding.ASCII)

End Sub

Sub AgregarParDXF( _
    ByVal salida As System.Collections.Generic.List(Of String), _
    ByVal codigo As Integer, _
    ByVal valor As String)

    salida.Add(codigo.ToString().PadLeft(3))
    salida.Add(valor)

End Sub

Function FormatearNumeroDXF(ByVal valor As Double) As String

    Return valor.ToString( _
        "0.########", _
        System.Globalization.CultureInfo.InvariantCulture)

End Function

'---------------------------------------------------------
' UTILIDADES DE RUTA
'---------------------------------------------------------

Function RedirigirRutaHaQ(ByVal ruta As String) As String
    Try
        If ruta Is Nothing Then Return ""
        If ruta.ToUpper().StartsWith("H:") Then
            Return "Q:" & ruta.Substring(2)
        End If
        Return ruta
    Catch
        Return ruta
    End Try
End Function

Sub CrearCarpetaSiNoExiste(ByVal ruta As String)
    If Not System.IO.Directory.Exists(ruta) Then
        System.IO.Directory.CreateDirectory(ruta)
    End If
End Sub

'---------------------------------------------------------
' v5.3: CARPETA DE RANGO POR CODIGO - FORMATO ESTRICTO
'---------------------------------------------------------
' SOLO acepta carpetas con formato de rango, con punto inicial
' opcional: .A02000-A02999 / A02000-A02999 / .40000-40999 / 40000-40999
' La comparacion es NUMERICA (no alfabetica): A02893 va a
' .A02000-A02999 y nunca a .A0000-A0999.
' Si no existe la carpeta del rango, SE CREA (bloques de 1000).

Function BuscarCarpetaPorCodigo(ByVal partNumber As String, ByVal rutaBase As String) As String

    Try
        If partNumber = "" OrElse rutaBase = "" Then Return ""
        If Not System.IO.Directory.Exists(rutaBase) Then Return ""

        partNumber = Trim(partNumber).ToUpper()

        Dim esA As Boolean = partNumber.StartsWith("A")

        Dim digitos As String = partNumber
        If esA Then digitos = partNumber.Substring(1)

        ' Quedarse solo con los digitos iniciales.
        Dim soloDigitos As String = ""
        For k As Integer = 0 To digitos.Length - 1
            Dim c As String = digitos.Substring(k, 1)
            If c >= "0" AndAlso c <= "9" Then
                soloDigitos &= c
            Else
                Exit For
            End If
        Next

        Dim numero As Integer = 0
        If Not Integer.TryParse(soloDigitos, numero) Then Return ""

        Dim patron As String
        If esA Then
            patron = "^\.?A0*(\d+)\s*-\s*A0*(\d+)$"
        Else
            patron = "^\.?0*(\d+)\s*-\s*0*(\d+)$"
        End If

        Dim mejor As String = ""
        Dim mejorAmplitud As Integer = Integer.MaxValue

        For Each carpeta As String In System.IO.Directory.GetDirectories(rutaBase)

            Dim nombreCarpeta As String = _
                System.IO.Path.GetFileName(carpeta).Trim().ToUpperInvariant()

            Dim m As System.Text.RegularExpressions.Match = _
                System.Text.RegularExpressions.Regex.Match(nombreCarpeta, patron)

            If Not m.Success Then Continue For

            Dim inicio As Integer = 0
            Dim fin As Integer = 0
            If Not Integer.TryParse(m.Groups(1).Value, inicio) Then Continue For
            If Not Integer.TryParse(m.Groups(2).Value, fin) Then Continue For

            If numero >= inicio AndAlso numero <= fin Then
                Dim amplitud As Integer = Math.Abs(fin - inicio)
                If amplitud < mejorAmplitud Then
                    mejorAmplitud = amplitud
                    mejor = carpeta
                End If
            End If

        Next

        If mejor <> "" Then Return mejor

        ' No existe carpeta de rango: se crea (bloques de 1000).
        Dim inicioRango As Integer = (numero \ 1000) * 1000
        Dim finRango As Integer = inicioRango + 999

        Dim nombreNuevo As String
        If esA Then
            nombreNuevo = ".A" & inicioRango.ToString("00000") & _
                          "-A" & finRango.ToString("00000")
        Else
            nombreNuevo = "." & inicioRango.ToString("00000") & _
                          "-" & finRango.ToString("00000")
        End If

        Dim rutaNueva As String = System.IO.Path.Combine(rutaBase, nombreNuevo)

        If Not System.IO.Directory.Exists(rutaNueva) Then
            System.IO.Directory.CreateDirectory(rutaNueva)
        End If

        Return rutaNueva

    Catch
        Return ""
    End Try

End Function

'---------------------------------------------------------
' LOG CSV
'---------------------------------------------------------

Sub EscribirCabeceraLog(ByVal sw As System.IO.StreamWriter)
    sw.WriteLine("ITEM;CODIGO;DESCRIPCION;CANTIDAD;MATERIAL;ESPESOR_MM;ARCHIVO_ORIGEN;DXF_GENERADO;ESTADO;OBSERVACION")
End Sub

Function MakeCsvLine(itemText As String, _
                     partNumber As String, _
                     description As String, _
                     qty As String, _
                     material As String, _
                     thickness As String, _
                     sourceFile As String, _
                     dxfPath As String, _
                     estado As String, _
                     observacion As String) As String

    Dim line As String = ""

    line = line & CsvField(itemText) & ";"
    line = line & CsvField(partNumber) & ";"
    line = line & CsvField(description) & ";"
    line = line & CsvField(qty) & ";"
    line = line & CsvField(material) & ";"
    line = line & CsvField(thickness) & ";"
    line = line & CsvField(sourceFile) & ";"
    line = line & CsvField(dxfPath) & ";"
    line = line & CsvField(estado) & ";"
    line = line & CsvField(observacion)

    Return line

End Function

Function CsvField(value As String) As String

    If value Is Nothing Then value = ""

    value = Replace(value, """", """""")

    Return """" & value & """"

End Function

Function AddObs(currentObs As String, newObs As String) As String

    If currentObs Is Nothing Then currentObs = ""
    If newObs Is Nothing Then newObs = ""

    If Trim(currentObs) = "" Then
        Return newObs
    Else
        Return currentObs & " | " & newObs
    End If

End Function

'---------------------------------------------------------
' PROPIEDADES
'---------------------------------------------------------

Function GetProp(doc As Document, propSetName As String, propName As String) As String

    Try
        Dim value As Object = doc.PropertySets.Item(propSetName).Item(propName).Value

        If value Is Nothing Then
            Return ""
        Else
            Return CStr(value)
        End If

    Catch
        Return ""
    End Try

End Function

Function GetMaterialName(partDoc As PartDocument) As String

    Try
        Return partDoc.ComponentDefinition.Material.Name
    Catch
    End Try

    Try
        Return partDoc.ActiveMaterial.DisplayName
    Catch
    End Try

    Try
        Return GetProp(partDoc, "Design Tracking Properties", "Material")
    Catch
    End Try

    Return ""

End Function

Function GetSheetMetalThicknessMm(smDef As SheetMetalComponentDefinition) As String

    Try
        Dim thicknessCm As Double = smDef.Thickness.Value
        Dim thicknessMm As Double = thicknessCm * 10.0

        Return Math.Round(thicknessMm, 3).ToString(System.Globalization.CultureInfo.InvariantCulture)

    Catch
        Return ""
    End Try

End Function

Function SafeFileName(value As String) As String

    If value Is Nothing Then value = ""

    value = Trim(value)

    Dim invalidChars() As Char = System.IO.Path.GetInvalidFileNameChars()

    For Each ch As Char In invalidChars
        value = value.Replace(ch, "_"c)
    Next

    value = Replace(value, " ", "_")

    While value.Contains("__")
        value = Replace(value, "__", "_")
    End While

    Return value

End Function
