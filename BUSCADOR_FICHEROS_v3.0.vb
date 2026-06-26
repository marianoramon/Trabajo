' =====================================================================
'  BUSCADOR DE FICHEROS EN LA RED (con INDICE) - v3.0 MEJORADA
' =====================================================================
'
'  Nuevas caracteristicas v3.0:
'    - Buscar por descripcion de pieza (Descripcion)
'    - Ordenar resultados clickeando en encabezados
'    - Boton "Actualizar indice" para reindexar sin perder datos
'    - Soporta nombre de fichero + Nº pieza + Descripcion
'
'  Modos de indexado:
'    - Rapido: solo nombre
'    - Completo: nombre + Nº de pieza + Descripcion (lee iProperties)
'
' =====================================================================

Sub Main()
    Dim frm As New BuscadorFicherosForm(ThisApplication)
    frm.ShowDialog()
End Sub


Public Class BuscadorFicherosForm
    Inherits System.Windows.Forms.Form

    Private Const RUTA_DEFECTO As String = "P:\"
    Private Const RUTA_RED As String = "\\192.168.10.217\DISEÑO"
    Private Const RUTA_INDICES As String = "P:\000 MARIANADAS\INDEX"
    Private Const MAX_RESULTADOS As Integer = 2000

    Private oApp As Inventor.Application

    Private txtRuta As System.Windows.Forms.TextBox
    Private btnExaminar As System.Windows.Forms.Button
    Private cboUnidad As System.Windows.Forms.ComboBox
    Private txtIndice As System.Windows.Forms.TextBox
    Private btnExaminarIndice As System.Windows.Forms.Button
    Private chkPartNumber As System.Windows.Forms.CheckBox
    Private btnIndexar As System.Windows.Forms.Button
    Private btnActualizar As System.Windows.Forms.Button
    Private lblIndiceInfo As System.Windows.Forms.Label
    Private txtCodigo As System.Windows.Forms.TextBox
    Private rdoTodos As System.Windows.Forms.RadioButton
    Private rdoPiezas As System.Windows.Forms.RadioButton
    Private rdoEnsamblajes As System.Windows.Forms.RadioButton
    Private rdoPlanos As System.Windows.Forms.RadioButton
    Private btnBuscar As System.Windows.Forms.Button
    Private btnCancelar As System.Windows.Forms.Button
    Private btnAbrir As System.Windows.Forms.Button
    Private btnAbrirCarpeta As System.Windows.Forms.Button
    Private lstResultados As System.Windows.Forms.ListView
    Private lblEstado As System.Windows.Forms.Label

    Private hiloTrabajo As System.Threading.Thread
    Private cancelar As Boolean = False
    Private _cuentaIndexada As Integer = 0
    Private _progreso As Integer = 0
    Private _nPiezas As Integer = 0
    Private _nEnsam As Integer = 0
    Private _nPlanos As Integer = 0
    Private _errorTrabajo As String = ""

    Private _indice As System.Collections.Generic.List(Of String())
    Private _indiceFirma As String = ""
    Private _ordenActual As Integer = 0
    Private _ascendente As Boolean = True

    Public Sub New(app As Inventor.Application)
        oApp = app
        InicializarComponentes()
    End Sub

    Private Function CarpetaConfig() As String
        Return System.IO.Path.Combine( _
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments), _
            "BuscadorInventor")
    End Function

    Private Sub InicializarComponentes()
        Me.Text = "Buscar ficheros en la red (con indice) v3.0"
        Me.Width = 950
        Me.Height = 750
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen

        Dim anchTLR As System.Windows.Forms.AnchorStyles = _
            System.Windows.Forms.AnchorStyles.Top _
            Or System.Windows.Forms.AnchorStyles.Left _
            Or System.Windows.Forms.AnchorStyles.Right
        Dim anchTR As System.Windows.Forms.AnchorStyles = _
            System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right

        ' --- Fila 1: carpeta a indexar ---
        Me.Controls.Add(Etiqueta("Carpeta a indexar:", 12, 15, 130))
        txtRuta = New System.Windows.Forms.TextBox()
        txtRuta.Left = 145 : txtRuta.Top = 12 : txtRuta.Width = 490 : txtRuta.Anchor = anchTLR
        Me.Controls.Add(txtRuta)
        btnExaminar = Boton("Examinar...", 660, 10, 115, 26, anchTR)
        AddHandler btnExaminar.Click, AddressOf OnExaminarRuta
        Me.Controls.Add(btnExaminar)

        ' --- Fila 2: unidad rapida ---
        Me.Controls.Add(Etiqueta("Unidad:", 12, 47, 130))
        cboUnidad = New System.Windows.Forms.ComboBox()
        cboUnidad.Left = 145 : cboUnidad.Top = 44 : cboUnidad.Width = 280
        cboUnidad.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        cboUnidad.Items.Add("Ruta de red (" & RUTA_RED & ")")
        Try
            For Each unidad As String In System.Environment.GetLogicalDrives()
                cboUnidad.Items.Add(unidad)
            Next
        Catch
        End Try
        AddHandler cboUnidad.SelectedIndexChanged, AddressOf OnUnidadChanged
        Me.Controls.Add(cboUnidad)

        ' --- Fila 3: carpeta del indice ---
        Me.Controls.Add(Etiqueta("Carpeta del indice:", 12, 81, 130))
        txtIndice = New System.Windows.Forms.TextBox()
        txtIndice.Left = 145 : txtIndice.Top = 78 : txtIndice.Width = 490 : txtIndice.Anchor = anchTLR
        AddHandler txtIndice.TextChanged, AddressOf OnIndiceCarpetaChanged
        Me.Controls.Add(txtIndice)
        btnExaminarIndice = Boton("Examinar...", 660, 76, 115, 26, anchTR)
        AddHandler btnExaminarIndice.Click, AddressOf OnExaminarIndice
        Me.Controls.Add(btnExaminarIndice)

        ' --- Fila 4: opciones de indexado ---
        chkPartNumber = New System.Windows.Forms.CheckBox()
        chkPartNumber.Text = "Indexar tambien Nº de pieza y descripcion (mas lento)"
        chkPartNumber.Left = 145 : chkPartNumber.Top = 110 : chkPartNumber.Width = 350
        Me.Controls.Add(chkPartNumber)

        ' --- Fila 5: indexar + actualizar + info ---
        btnIndexar = Boton("Indexar ahora", 145, 138, 120, 28, _
            System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left)
        AddHandler btnIndexar.Click, AddressOf OnIndexar
        Me.Controls.Add(btnIndexar)

        btnActualizar = Boton("Actualizar", 275, 138, 120, 28, _
            System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left)
        AddHandler btnActualizar.Click, AddressOf OnActualizar
        Me.Controls.Add(btnActualizar)

        lblIndiceInfo = Etiqueta("", 405, 143, 370)
        Me.Controls.Add(lblIndiceInfo)

        ' --- Fila 6: codigo + buscar ---
        Me.Controls.Add(Etiqueta("Codigo a buscar:", 12, 179, 130))
        txtCodigo = New System.Windows.Forms.TextBox()
        txtCodigo.Left = 145 : txtCodigo.Top = 176 : txtCodigo.Width = 490 : txtCodigo.Anchor = anchTLR
        AddHandler txtCodigo.KeyDown, AddressOf OnCodigoKeyDown
        Me.Controls.Add(txtCodigo)
        btnBuscar = Boton("Buscar", 660, 174, 115, 26, anchTR)
        AddHandler btnBuscar.Click, AddressOf OnBuscar
        Me.Controls.Add(btnBuscar)

        ' --- Fila 7: filtro por tipo ---
        rdoTodos = New System.Windows.Forms.RadioButton()
        rdoTodos.Text = "Todos" : rdoTodos.Checked = True
        rdoTodos.Left = 145 : rdoTodos.Top = 208 : rdoTodos.Width = 70
        Me.Controls.Add(rdoTodos)
        rdoPiezas = New System.Windows.Forms.RadioButton()
        rdoPiezas.Text = "Piezas (.ipt)"
        rdoPiezas.Left = 220 : rdoPiezas.Top = 208 : rdoPiezas.Width = 110
        Me.Controls.Add(rdoPiezas)
        rdoEnsamblajes = New System.Windows.Forms.RadioButton()
        rdoEnsamblajes.Text = "Ensamblajes (.iam)"
        rdoEnsamblajes.Left = 335 : rdoEnsamblajes.Top = 208 : rdoEnsamblajes.Width = 150
        Me.Controls.Add(rdoEnsamblajes)
        rdoPlanos = New System.Windows.Forms.RadioButton()
        rdoPlanos.Text = "Planos (.idw)"
        rdoPlanos.Left = 490 : rdoPlanos.Top = 208 : rdoPlanos.Width = 160
        Me.Controls.Add(rdoPlanos)

        ' --- Lista de resultados (ordenable) ---
        lstResultados = New System.Windows.Forms.ListView()
        lstResultados.Left = 12 : lstResultados.Top = 238
        lstResultados.Width = 920 : lstResultados.Height = 340
        lstResultados.View = System.Windows.Forms.View.Details
        lstResultados.FullRowSelect = True
        lstResultados.MultiSelect = False
        lstResultados.Anchor = System.Windows.Forms.AnchorStyles.Top _
            Or System.Windows.Forms.AnchorStyles.Bottom _
            Or System.Windows.Forms.AnchorStyles.Left _
            Or System.Windows.Forms.AnchorStyles.Right
        lstResultados.Columns.Add("Nombre", 180)
        lstResultados.Columns.Add("Nº pieza", 90)
        lstResultados.Columns.Add("Descripcion", 180)
        lstResultados.Columns.Add("Tipo", 60)
        lstResultados.Columns.Add("Modificado", 115)
        lstResultados.Columns.Add("Ruta", 290)
        AddHandler lstResultados.DoubleClick, AddressOf OnAbrir
        AddHandler lstResultados.ColumnClick, AddressOf OnColumnClick
        Me.Controls.Add(lstResultados)

        ' --- Estado + botones ---
        lblEstado = Etiqueta("Listo.", 12, 594, 400)
        lblEstado.Anchor = System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left
        Me.Controls.Add(lblEstado)
        btnCancelar = Boton("Cancelar", 318, 589, 100, 30, _
            System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right)
        btnCancelar.Enabled = False
        AddHandler btnCancelar.Click, AddressOf OnCancelar
        Me.Controls.Add(btnCancelar)
        btnAbrirCarpeta = Boton("Abrir carpeta", 425, 589, 160, 30, _
            System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right)
        AddHandler btnAbrirCarpeta.Click, AddressOf OnAbrirCarpeta
        Me.Controls.Add(btnAbrirCarpeta)
        btnAbrir = Boton("Abrir en Inventor", 592, 589, 183, 30, _
            System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right)
        AddHandler btnAbrir.Click, AddressOf OnAbrir
        Me.Controls.Add(btnAbrir)

        Me.AcceptButton = btnBuscar

        CargarAjustes()
        ActualizarInfoIndice()
    End Sub

    ' --- Helpers de creacion de controles ---
    Private Function Etiqueta(texto As String, x As Integer, y As Integer, w As Integer) _
            As System.Windows.Forms.Label
        Dim l As New System.Windows.Forms.Label()
        l.Text = texto : l.Left = x : l.Top = y : l.Width = w
        Return l
    End Function

    Private Function Boton(texto As String, x As Integer, y As Integer, w As Integer, _
            h As Integer, anchor As System.Windows.Forms.AnchorStyles) As System.Windows.Forms.Button
        Dim b As New System.Windows.Forms.Button()
        b.Text = texto : b.Left = x : b.Top = y : b.Width = w : b.Height = h : b.Anchor = anchor
        Return b
    End Function

    ' --- Ajustes (recordar rutas y opciones) ---
    Private Function ArchivoAjustes() As String
        Return System.IO.Path.Combine(CarpetaConfig(), "ajustes.txt")
    End Function

    Private Sub CargarAjustes()
        Dim ruta As String = RUTA_DEFECTO
        Dim indice As String = RUTA_INDICES
        Dim pn As Boolean = False
        Try
            Dim a As String = ArchivoAjustes()
            If System.IO.File.Exists(a) Then
                Dim ls() As String = System.IO.File.ReadAllLines(a)
                If ls.Length >= 1 AndAlso ls(0).Trim() <> "" Then ruta = ls(0).Trim()
                If ls.Length >= 2 AndAlso ls(1).Trim() <> "" Then
                    Dim carpetaGuardada As String = ls(1).Trim()
                    If System.IO.Directory.Exists(carpetaGuardada) Then
                        indice = carpetaGuardada
                    End If
                End If
                If ls.Length >= 3 Then pn = (ls(2).Trim() = "1")
            End If
        Catch
        End Try
        txtRuta.Text = ruta
        txtIndice.Text = indice
        chkPartNumber.Checked = pn
    End Sub

    Private Sub GuardarAjustes()
        Try
            System.IO.Directory.CreateDirectory(CarpetaConfig())
            System.IO.File.WriteAllText(ArchivoAjustes(), _
                txtRuta.Text.Trim() & vbCrLf & txtIndice.Text.Trim() & vbCrLf & _
                If(chkPartNumber.Checked, "1", "0") & vbCrLf)
        Catch
        End Try
    End Sub

    Private Function CarpetaIndice() As String
        Dim carpeta As String = txtIndice.Text.Trim()
        If carpeta = "" Then carpeta = RUTA_INDICES
        Return carpeta
    End Function

    Private Function Sanitizar(s As String) As String
        Dim r As String = s
        For Each c As Char In System.IO.Path.GetInvalidFileNameChars()
            r = r.Replace(c, "_"c)
        Next
        r = r.Replace(" ", "_")
        If r.Length > 80 Then r = r.Substring(0, 80)
        If r = "" Then r = "raiz"
        Return r
    End Function

    Private Function ArchivoIndiceActual() As String
        Return System.IO.Path.Combine(CarpetaIndice(), _
            "IndiceBuscador_" & Sanitizar(txtRuta.Text.Trim()) & ".txt")
    End Function

    Private Function ListarArchivosIndice() As System.Collections.Generic.List(Of String)
        Dim res As New System.Collections.Generic.List(Of String)
        Try
            Dim carpeta As String = CarpetaIndice()
            If System.IO.Directory.Exists(carpeta) Then
                For Each f As String In System.IO.Directory.GetFiles(carpeta, "IndiceBuscador*.txt")
                    res.Add(f)
                Next
            End If
        Catch
        End Try
        Return res
    End Function

    Private Function RaizDeIndice(archivo As String) As String
        Try
            Using sr As New System.IO.StreamReader(archivo)
                Dim primera As String = sr.ReadLine()
                If primera IsNot Nothing AndAlso primera.StartsWith("#") Then
                    Dim p() As String = primera.Split(CChar(vbTab))
                    If p.Length >= 3 Then Return p(2)
                End If
            End Using
        Catch
        End Try
        Return System.IO.Path.GetFileNameWithoutExtension(archivo)
    End Function

    Private Sub ActualizarInfoIndice()
        Try
            Dim archivos = ListarArchivosIndice()
            If archivos.Count = 0 Then
                lblIndiceInfo.Text = "Sin indice. Pulsa 'Indexar ahora'."
                Return
            End If
            Dim ultima As Date = Date.MinValue
            Dim raices As New System.Collections.Generic.List(Of String)
            For Each a As String In archivos
                Dim f As Date = System.IO.File.GetLastWriteTime(a)
                If f > ultima Then ultima = f
                raices.Add(RaizDeIndice(a))
            Next
            lblIndiceInfo.Text = archivos.Count.ToString() & " indice(s): " & _
                String.Join(", ", raices.ToArray()) & "  (ult. " & ultima.ToString("yyyy-MM-dd HH:mm") & ")"
        Catch
            lblIndiceInfo.Text = ""
        End Try
    End Sub

    ' --- Eventos de rutas ---
    Private Sub OnExaminarRuta(sender As Object, e As System.EventArgs)
        Dim r As String = ElegirCarpeta(txtRuta.Text)
        If r <> "" Then txtRuta.Text = r
    End Sub

    Private Sub OnExaminarIndice(sender As Object, e As System.EventArgs)
        Dim r As String = ElegirCarpeta(txtIndice.Text)
        If r <> "" Then txtIndice.Text = r
    End Sub

    Private Function ElegirCarpeta(inicial As String) As String
        Dim dlg As New System.Windows.Forms.FolderBrowserDialog()
        dlg.Description = "Elige la carpeta"
        Try
            If System.IO.Directory.Exists(inicial.Trim()) Then dlg.SelectedPath = inicial.Trim()
        Catch
        End Try
        If dlg.ShowDialog() = System.Windows.Forms.DialogResult.OK Then Return dlg.SelectedPath
        Return ""
    End Function

    Private Sub OnUnidadChanged(sender As Object, e As System.EventArgs)
        If cboUnidad.SelectedIndex = 0 Then
            txtRuta.Text = RUTA_RED
        ElseIf cboUnidad.SelectedIndex > 0 Then
            txtRuta.Text = cboUnidad.SelectedItem.ToString()
        End If
    End Sub

    Private Sub OnIndiceCarpetaChanged(sender As Object, e As System.EventArgs)
        ActualizarInfoIndice()
    End Sub

    Private Sub OnCodigoKeyDown(sender As Object, e As System.Windows.Forms.KeyEventArgs)
        If e.KeyCode = System.Windows.Forms.Keys.Enter Then
            OnBuscar(sender, e)
            e.SuppressKeyPress = True
        End If
    End Sub

    ' ================= INDEXACION =================
    Private Sub OnIndexar(sender As Object, e As System.EventArgs)
        EjecutarIndexacion(False)
    End Sub

    Private Sub OnActualizar(sender As Object, e As System.EventArgs)
        EjecutarIndexacion(True)
    End Sub

    Private Sub EjecutarIndexacion(esActualizacion As Boolean)
        If hiloTrabajo IsNot Nothing AndAlso hiloTrabajo.IsAlive Then Return

        Dim ruta As String = txtRuta.Text.Trim()
        If ruta = "" OrElse Not System.IO.Directory.Exists(ruta) Then
            System.Windows.Forms.MessageBox.Show("La carpeta a indexar no existe:" & vbCrLf & ruta, _
                "Indexar", System.Windows.Forms.MessageBoxButtons.OK, _
                System.Windows.Forms.MessageBoxIcon.Warning)
            Return
        End If

        Dim carpetaIndice As String = txtIndice.Text.Trim()
        If carpetaIndice = "" Then carpetaIndice = CarpetaConfig()
        Try
            System.IO.Directory.CreateDirectory(carpetaIndice)
        Catch ex As System.Exception
            System.Windows.Forms.MessageBox.Show("No se pudo usar la carpeta del indice:" & vbCrLf & _
                ex.Message, "Indexar", System.Windows.Forms.MessageBoxButtons.OK, _
                System.Windows.Forms.MessageBoxIcon.Error)
            Return
        End Try

        GuardarAjustes()
        cancelar = False
        btnIndexar.Enabled = False
        btnActualizar.Enabled = False
        btnBuscar.Enabled = False
        btnCancelar.Enabled = True
        Me.Cursor = System.Windows.Forms.Cursors.WaitCursor

        Dim archivo As String = ArchivoIndiceActual()
        Dim modo As String = If(esActualizacion, "Actualizando", "Indexando")

        If chkPartNumber.Checked Then
            lblEstado.Text = modo & " con Nº de pieza y descripcion en segundo plano..."
            hiloTrabajo = New System.Threading.Thread(Sub() EjecutarIndexadoCompleto(ruta, archivo, esActualizacion))
        Else
            lblEstado.Text = modo & " en segundo plano: " & ruta & " ..."
            hiloTrabajo = New System.Threading.Thread(Sub() EjecutarIndexadoRapido(ruta, archivo, esActualizacion))
        End If
        hiloTrabajo.IsBackground = True
        Try
            hiloTrabajo.SetApartmentState(System.Threading.ApartmentState.STA)
        Catch
        End Try
        hiloTrabajo.Start()
    End Sub

    Private Function CargarIndiceDiccionario(archivo As String) _
            As System.Collections.Generic.Dictionary(Of String, String())
        Dim d As New System.Collections.Generic.Dictionary(Of String, String())( _
            System.StringComparer.OrdinalIgnoreCase)
        Try
            If System.IO.File.Exists(archivo) Then
                For Each l As String In System.IO.File.ReadAllLines(archivo)
                    If l = "" OrElse l.StartsWith("#") Then Continue For
                    Dim p() As String = l.Split(CChar(vbTab))
                    If p.Length < 2 Then Continue For
                    Dim pn As String = ""
                    Dim desc As String = ""
                    If p.Length >= 3 Then pn = p(2)
                    If p.Length >= 4 Then desc = p(3)
                    d(p(0)) = New String() {p(1), pn, desc}
                Next
            End If
        Catch
        End Try
        Return d
    End Function

    Private Sub EscribirEntrada(sw As System.IO.StreamWriter, ruta As String, _
            ticks As Long, pn As String, desc As String)
        Dim ext As String = System.IO.Path.GetExtension(ruta).ToLower()
        If ext = ".ipt" Then
            _nPiezas += 1
        ElseIf ext = ".iam" Then
            _nEnsam += 1
        ElseIf ext = ".idw" Then
            _nPlanos += 1
        End If
        Dim pnLimpio As String = pn.Replace(vbTab, " ").Replace(vbCr, " ").Replace(vbLf, " ")
        Dim descLimpio As String = desc.Replace(vbTab, " ").Replace(vbCr, " ").Replace(vbLf, " ")
        sw.WriteLine(ruta & vbTab & ticks.ToString() & vbTab & pnLimpio & vbTab & descLimpio)
    End Sub

    Private Sub EjecutarIndexadoRapido(ruta As String, archivo As String, esActualizacion As Boolean)
        _cuentaIndexada = 0
        _progreso = 0
        _nPiezas = 0
        _nEnsam = 0
        _nPlanos = 0
        _errorTrabajo = ""

        Dim viejo = CargarIndiceDiccionario(archivo)
        Dim archivoTemporal As String = archivo & ".tmp"

        Try
            If System.IO.File.Exists(archivoTemporal) Then System.IO.File.Delete(archivoTemporal)

            Using sw As New System.IO.StreamWriter(archivoTemporal, False, New System.Text.UTF8Encoding(False))
                sw.WriteLine("#INDICE" & vbTab & System.DateTime.Now.ToString("yyyy-MM-dd HH:mm") & vbTab & ruta)
                _cuentaIndexada = IndexarRapido(ruta, sw, viejo)
            End Using

            If cancelar Then
                Try
                    If System.IO.File.Exists(archivoTemporal) Then System.IO.File.Delete(archivoTemporal)
                Catch
                End Try
            Else
                ReemplazarIndiceDeFormaSegura(archivoTemporal, archivo)
            End If

        Catch ex As System.Exception
            _errorTrabajo = ex.Message
            Try
                If System.IO.File.Exists(archivoTemporal) Then System.IO.File.Delete(archivoTemporal)
            Catch
            End Try
        End Try

        _indice = Nothing
        _indiceFirma = ""

        Try
            If Me.IsHandleCreated AndAlso Not Me.IsDisposed Then
                Me.Invoke(New System.Action(AddressOf FinIndexado))
            End If
        Catch
        End Try
    End Sub

    Private Function IndexarRapido(carpeta As String, sw As System.IO.StreamWriter, _
            viejo As System.Collections.Generic.Dictionary(Of String, String())) As Integer
        If cancelar Then Return 0
        Dim n As Integer = 0
        Dim ficheros() As String = Nothing
        Try
            ficheros = System.IO.Directory.GetFiles(carpeta)
        Catch
            Return 0
        End Try
        For Each f As String In ficheros
            If cancelar Then Return n
            Dim ext As String = System.IO.Path.GetExtension(f).ToLower()
            If EsIndexable(ext) Then
                Dim ticks As Long = 0
                Try
                    ticks = System.IO.File.GetLastWriteTime(f).Ticks
                Catch
                End Try
                Dim pn As String = ""
                Dim desc As String = ""
                Dim prev() As String = Nothing
                If viejo.TryGetValue(f, prev) AndAlso prev(0) = ticks.ToString() Then
                    pn = prev(1)
                    desc = prev(2)
                End If
                EscribirEntrada(sw, f, ticks, pn, desc)
                n += 1
                _progreso += 1
                If (_progreso Mod 200) = 0 Then ActualizarProgresoAsync()
            End If
        Next
        Dim subs() As String = Nothing
        Try
            subs = System.IO.Directory.GetDirectories(carpeta)
        Catch
            Return n
        End Try
        For Each sc As String In subs
            If EsCarpetaExcluida(sc) Then Continue For
            n += IndexarRapido(sc, sw, viejo)
            If cancelar Then Return n
        Next
        Return n
    End Function

    Private Sub ActualizarProgresoAsync()
        Try
            If Me.IsHandleCreated AndAlso Not Me.IsDisposed Then
                Me.BeginInvoke(New System.Action(AddressOf MostrarProgreso))
            End If
        Catch
        End Try
    End Sub

    Private Sub MostrarProgreso()
        lblEstado.Text = "Indexando... " & _progreso.ToString() & " ficheros"
    End Sub

    Private Sub EjecutarIndexadoCompleto(ruta As String, archivo As String, esActualizacion As Boolean)
        _cuentaIndexada = 0
        _progreso = 0
        _nPiezas = 0
        _nEnsam = 0
        _nPlanos = 0
        _errorTrabajo = ""

        Dim viejo = CargarIndiceDiccionario(archivo)
        Dim archivoTemporal As String = archivo & ".tmp"

        Dim oApr As Inventor.ApprenticeServerComponent = Nothing
        Try
            oApr = New Inventor.ApprenticeServerComponent()
        Catch
            oApr = Nothing
        End Try

        Try
            If System.IO.File.Exists(archivoTemporal) Then System.IO.File.Delete(archivoTemporal)

            Using sw As New System.IO.StreamWriter(archivoTemporal, False, New System.Text.UTF8Encoding(False))
                sw.WriteLine("#INDICE" & vbTab & System.DateTime.Now.ToString("yyyy-MM-dd HH:mm") & vbTab & ruta & vbTab & "PN+DESC")
                _cuentaIndexada = IndexarCompleto(ruta, sw, viejo, oApr)
            End Using

            If cancelar Then
                Try
                    If System.IO.File.Exists(archivoTemporal) Then System.IO.File.Delete(archivoTemporal)
                Catch
                End Try
            Else
                ReemplazarIndiceDeFormaSegura(archivoTemporal, archivo)
            End If

        Catch ex As System.Exception
            _errorTrabajo = ex.Message
            Try
                If System.IO.File.Exists(archivoTemporal) Then System.IO.File.Delete(archivoTemporal)
            Catch
            End Try
        End Try

        oApr = Nothing
        _indice = Nothing
        _indiceFirma = ""

        Try
            If Me.IsHandleCreated AndAlso Not Me.IsDisposed Then
                Me.Invoke(New System.Action(AddressOf FinIndexado))
            End If
        Catch
        End Try
    End Sub

    Private Function IndexarCompleto(carpeta As String, sw As System.IO.StreamWriter, _
            viejo As System.Collections.Generic.Dictionary(Of String, String()), _
            oApr As Inventor.ApprenticeServerComponent) As Integer
        If cancelar Then Return 0
        Dim n As Integer = 0
        Dim ficheros() As String = Nothing
        Try
            ficheros = System.IO.Directory.GetFiles(carpeta)
        Catch
            Return 0
        End Try
        For Each f As String In ficheros
            If cancelar Then Return n
            Dim ext As String = System.IO.Path.GetExtension(f).ToLower()
            If EsIndexable(ext) Then
                Dim ticks As Long = 0
                Try
                    ticks = System.IO.File.GetLastWriteTime(f).Ticks
                Catch
                End Try

                Dim pn As String = ""
                Dim desc As String = ""
                Dim prev() As String = Nothing
                If viejo.TryGetValue(f, prev) AndAlso prev(0) = ticks.ToString() Then
                    pn = prev(1)
                    desc = prev(2)
                ElseIf oApr IsNot Nothing Then
                    Dim datos() As String = LeerPropiedadesPieza(oApr, f)
                    pn = datos(0)
                    desc = datos(1)
                End If

                EscribirEntrada(sw, f, ticks, pn, desc)
                n += 1
                _progreso += 1
                If (_progreso Mod 20) = 0 Then ActualizarProgresoAsync()
            End If
        Next
        Dim subs() As String = Nothing
        Try
            subs = System.IO.Directory.GetDirectories(carpeta)
        Catch
            Return n
        End Try
        For Each sc As String In subs
            If EsCarpetaExcluida(sc) Then Continue For
            n += IndexarCompleto(sc, sw, viejo, oApr)
            If cancelar Then Return n
        Next
        Return n
    End Function

    Private Function LeerPropiedadesPieza(oApr As Inventor.ApprenticeServerComponent, ruta As String) As String()
        Dim pn As String = ""
        Dim desc As String = ""
        Try
            Dim oDoc As Inventor.ApprenticeServerDocument = oApr.Open(ruta)
            Try
                Try
                    Dim v As Object = oDoc.PropertySets.Item("Design Tracking Properties").Item("Part Number").Value
                    If v IsNot Nothing Then pn = v.ToString()
                Catch
                End Try
                Try
                    Dim v As Object = oDoc.PropertySets.Item("Design Tracking Properties").Item("Description").Value
                    If v IsNot Nothing Then desc = v.ToString()
                Catch
                End Try
            Catch
            End Try
            Try
                oApr.Close()
            Catch
            End Try
        Catch
            Try
                oApr.Close()
            Catch
            End Try
        End Try
        Return New String() {pn, desc}
    End Function

    Private Sub FinIndexado()
        Me.Cursor = System.Windows.Forms.Cursors.Default
        btnIndexar.Enabled = True
        btnActualizar.Enabled = True
        btnBuscar.Enabled = True
        btnCancelar.Enabled = False
        ActualizarInfoIndice()

        If _errorTrabajo <> "" Then
            lblEstado.Text = "Error al indexar: " & _errorTrabajo
        ElseIf cancelar Then
            lblEstado.Text = "Indexacion cancelada (" & _cuentaIndexada.ToString() & " ficheros)."
        Else
            lblEstado.Text = "Indice creado: " & _cuentaIndexada.ToString() & " ficheros (" & _
                _nPiezas.ToString() & " piezas, " & _nEnsam.ToString() & " ensamblajes, " & _
                _nPlanos.ToString() & " planos)."
        End If
    End Sub

    ' ================= BUSQUEDA (sobre el indice) =================
    Private Function ExtensionesActivas() As System.Collections.Generic.List(Of String)
        Dim exts As New System.Collections.Generic.List(Of String)
        If rdoPiezas.Checked Then
            exts.Add(".ipt")
        ElseIf rdoEnsamblajes.Checked Then
            exts.Add(".iam")
        ElseIf rdoPlanos.Checked Then
            exts.Add(".idw")
        Else
            exts.Add(".ipt")
            exts.Add(".iam")
            exts.Add(".idw")
        End If
        Return exts
    End Function

    Private Function EsIndexable(ext As String) As Boolean
        Return ext = ".ipt" OrElse ext = ".iam" OrElse ext = ".idw"
    End Function

    Private Function EsCarpetaExcluida(carpeta As String) As Boolean
        Dim nombre As String = System.IO.Path.GetFileName(carpeta.TrimEnd("\"c))
        Return String.Equals(nombre, "OldVersions", System.StringComparison.OrdinalIgnoreCase)
    End Function

    Private Function EsCopiaSeguridad(ruta As String) As Boolean
        Return ruta.ToLower().Contains("\oldversions\")
    End Function

    Private Function FirmaIndices(archivos As System.Collections.Generic.List(Of String)) As String
        Dim sb As New System.Text.StringBuilder()
        For Each a As String In archivos
            sb.Append(a).Append("|")
            Try
                sb.Append(System.IO.File.GetLastWriteTime(a).Ticks)
            Catch
            End Try
            sb.Append(";")
        Next
        Return sb.ToString()
    End Function

    Private Function CargarIndices(archivos As System.Collections.Generic.List(Of String)) As Boolean
        Dim lista As New System.Collections.Generic.List(Of String())
        Dim vistos As New System.Collections.Generic.HashSet(Of String)( _
            System.StringComparer.OrdinalIgnoreCase)
        Dim errores As New System.Collections.Generic.List(Of String)
        Dim indicesLeidos As Integer = 0

        Try
            Dim firma As String = FirmaIndices(archivos)
            If _indice IsNot Nothing AndAlso _indiceFirma = firma Then Return True

            For Each archivo As String In archivos
                Try
                    If Not System.IO.File.Exists(archivo) Then
                        errores.Add(System.IO.Path.GetFileName(archivo) & ": no existe")
                        Continue For
                    End If

                    Dim lineas() As String = LeerLineasIndiceConReintento(archivo, 5, 250)

                    For Each l As String In lineas
                        If String.IsNullOrWhiteSpace(l) OrElse l.StartsWith("#") Then Continue For

                        Dim p() As String = l.Split(CChar(vbTab))
                        If p.Length < 2 Then Continue For

                        Dim ruta As String = p(0).Trim()
                        If ruta = "" Then Continue For
                        If vistos.Contains(ruta) Then Continue For

                        Dim ticks As Long
                        If Not Long.TryParse(p(1).Trim(), ticks) Then Continue For

                        vistos.Add(ruta)

                        Dim nombre As String = System.IO.Path.GetFileNameWithoutExtension(ruta).ToLowerInvariant()
                        Dim ext As String = System.IO.Path.GetExtension(ruta).ToLowerInvariant()
                        Dim pnLower As String = ""
                        Dim descLower As String = ""
                        If p.Length >= 3 Then pnLower = p(2).Trim().ToLowerInvariant()
                        If p.Length >= 4 Then descLower = p(3).Trim().ToLowerInvariant()

                        lista.Add(New String() {ruta, nombre, ext, ticks.ToString(), pnLower, descLower})
                    Next

                    indicesLeidos += 1

                Catch exArchivo As System.Exception
                    errores.Add(System.IO.Path.GetFileName(archivo) & ": " & exArchivo.Message)
                End Try
            Next

            If indicesLeidos = 0 Then
                _errorTrabajo = "No se pudo leer ningun indice."
                Return False
            End If

            _indice = lista
            _indiceFirma = firma
            Return True

        Catch ex As System.Exception
            _errorTrabajo = ex.Message
            Return False
        End Try
    End Function

    Private Function LeerLineasIndiceConReintento(archivo As String, _
                                                   intentos As Integer, _
                                                   esperaMs As Integer) As String()
        Dim ultimoError As System.Exception = Nothing

        For i As Integer = 1 To intentos
            Try
                Using fs As New System.IO.FileStream(archivo, _
                                                    System.IO.FileMode.Open, _
                                                    System.IO.FileAccess.Read, _
                                                    System.IO.FileShare.ReadWrite)
                    Using sr As New System.IO.StreamReader(fs, System.Text.Encoding.UTF8, True)
                        Dim contenido As String = sr.ReadToEnd()
                        Return contenido.Replace(vbCrLf, vbLf).Replace(vbCr, vbLf).Split(CChar(vbLf))
                    End Using
                End Using
            Catch ex As System.Exception
                ultimoError = ex
                System.Threading.Thread.Sleep(esperaMs)
            End Try
        Next

        If ultimoError IsNot Nothing Then Throw ultimoError
        Return New String() {}
    End Function

    Private Sub ReemplazarIndiceDeFormaSegura(archivoTemporal As String, archivoFinal As String)
        Dim carpeta As String = System.IO.Path.GetDirectoryName(archivoFinal)
        If carpeta <> "" AndAlso Not System.IO.Directory.Exists(carpeta) Then
            System.IO.Directory.CreateDirectory(carpeta)
        End If

        Dim copiaSeguridad As String = archivoFinal & ".bak"

        Try
            If System.IO.File.Exists(copiaSeguridad) Then System.IO.File.Delete(copiaSeguridad)
            If System.IO.File.Exists(archivoFinal) Then
                System.IO.File.Replace(archivoTemporal, archivoFinal, copiaSeguridad, True)
                Try
                    If System.IO.File.Exists(copiaSeguridad) Then System.IO.File.Delete(copiaSeguridad)
                Catch
                End Try
            Else
                System.IO.File.Move(archivoTemporal, archivoFinal)
            End If
        Catch
            If System.IO.File.Exists(archivoFinal) Then System.IO.File.Delete(archivoFinal)
            System.IO.File.Move(archivoTemporal, archivoFinal)
        End Try
    End Sub

    Private Sub OnBuscar(sender As Object, e As System.EventArgs)
        Dim codigo As String = txtCodigo.Text.Trim()
        If codigo = "" Then
            System.Windows.Forms.MessageBox.Show("Escribe un codigo para buscar.", "Buscador", _
                System.Windows.Forms.MessageBoxButtons.OK, _
                System.Windows.Forms.MessageBoxIcon.Information)
            Return
        End If

        Dim archivos = ListarArchivosIndice()
        If archivos.Count = 0 Then
            Dim resp As System.Windows.Forms.DialogResult = System.Windows.Forms.MessageBox.Show( _
                "No hay ningun indice en esa carpeta." & vbCrLf & vbCrLf & "¿Indexar ahora?", _
                "Buscador", System.Windows.Forms.MessageBoxButtons.YesNo, _
                System.Windows.Forms.MessageBoxIcon.Question)
            If resp = System.Windows.Forms.DialogResult.Yes Then OnIndexar(sender, e)
            Return
        End If

        lblEstado.Text = "Buscando en " & archivos.Count.ToString() & " indice(s)..."
        Me.Cursor = System.Windows.Forms.Cursors.WaitCursor
        Me.Refresh()

        If Not CargarIndices(archivos) Then
            Me.Cursor = System.Windows.Forms.Cursors.Default
            System.Windows.Forms.MessageBox.Show("No se pudieron leer los indices.", _
                "Buscador", System.Windows.Forms.MessageBoxButtons.OK, _
                System.Windows.Forms.MessageBoxIcon.Error)
            Return
        End If

        Dim exts As System.Collections.Generic.List(Of String) = ExtensionesActivas()
        Dim cod As String = codigo.ToLower()
        Dim encontrados As New System.Collections.Generic.List(Of String())
        Dim truncado As Boolean = False

        For Each entrada As String() In _indice
            If EsCopiaSeguridad(entrada(0)) Then Continue For
            If exts.Contains(entrada(2)) Then
                Dim coincide As Boolean = entrada(1).Contains(cod)  ' nombre
                If Not coincide AndAlso entrada(4) <> "" Then coincide = entrada(4).Contains(cod)  ' Nº pieza
                If Not coincide AndAlso entrada(5) <> "" Then coincide = entrada(5).Contains(cod)  ' Descripcion
                If coincide Then
                    encontrados.Add(entrada)
                    If encontrados.Count >= MAX_RESULTADOS Then
                        truncado = True
                        Exit For
                    End If
                End If
            End If
        Next

        encontrados.Sort(Function(a, b) CLng(b(3)).CompareTo(CLng(a(3))))

        lstResultados.Items.Clear()
        For Each entrada As String() In encontrados
            Dim ruta As String = entrada(0)
            Dim item As New System.Windows.Forms.ListViewItem(System.IO.Path.GetFileName(ruta))
            item.SubItems.Add(entrada(4))  ' Nº pieza
            item.SubItems.Add(entrada(5))  ' Descripcion
            item.SubItems.Add(entrada(2).ToUpper().Replace(".", ""))  ' Tipo
            Dim fechaStr As String = ""
            Try
                fechaStr = New System.DateTime(CLng(entrada(3))).ToString("yyyy-MM-dd HH:mm")
            Catch
            End Try
            item.SubItems.Add(fechaStr)  ' Fecha
            item.SubItems.Add(ruta)  ' Ruta
            item.Tag = ruta
            lstResultados.Items.Add(item)
        Next

        Me.Cursor = System.Windows.Forms.Cursors.Default
        Dim msg As String = encontrados.Count.ToString() & " resultado(s) para """ & codigo & """."
        If truncado Then msg &= " (limitado a " & MAX_RESULTADOS & ")"
        lblEstado.Text = msg

        If lstResultados.Items.Count > 0 Then
            lstResultados.Items(0).Selected = True
            lstResultados.Select()
        End If
    End Sub

    Private Sub OnColumnClick(sender As Object, e As System.Windows.Forms.ColumnClickEventArgs)
        If e.Column = _ordenActual Then
            _ascendente = Not _ascendente
        Else
            _ordenActual = e.Column
            _ascendente = True
        End If

        ' Reordenar la lista
        Dim sorter As New ColumnSorter(_ordenActual, _ascendente)
        lstResultados.ListViewItemSorter = sorter
        lstResultados.Sort()
    End Sub

    Private Sub OnCancelar(sender As Object, e As System.EventArgs)
        cancelar = True
        lblEstado.Text = "Cancelando..."
        btnCancelar.Enabled = False
    End Sub

    Private Function RutaSeleccionada() As String
        If lstResultados.SelectedItems.Count = 0 Then
            System.Windows.Forms.MessageBox.Show("Selecciona un fichero de la lista.", "Buscador", _
                System.Windows.Forms.MessageBoxButtons.OK, _
                System.Windows.Forms.MessageBoxIcon.Information)
            Return ""
        End If
        Dim ruta As String = lstResultados.SelectedItems(0).Tag.ToString()
        If Not System.IO.File.Exists(ruta) Then
            System.Windows.Forms.MessageBox.Show("El fichero ya no existe (reindexar):" & vbCrLf & ruta, _
                "Buscador", System.Windows.Forms.MessageBoxButtons.OK, _
                System.Windows.Forms.MessageBoxIcon.Warning)
            Return ""
        End If
        Return ruta
    End Function

    Private Sub OnAbrir(sender As Object, e As System.EventArgs)
        Dim ruta As String = RutaSeleccionada()
        If ruta = "" Then Return
        Try
            oApp.Documents.Open(ruta, True)
            Me.Close()
        Catch ex As System.Exception
            System.Windows.Forms.MessageBox.Show("No se pudo abrir el fichero:" & vbCrLf & ruta & _
                vbCrLf & vbCrLf & ex.Message, "Error al abrir", _
                System.Windows.Forms.MessageBoxButtons.OK, _
                System.Windows.Forms.MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub OnAbrirCarpeta(sender As Object, e As System.EventArgs)
        Dim ruta As String = RutaSeleccionada()
        If ruta = "" Then Return
        Try
            System.Diagnostics.Process.Start("explorer.exe", "/select,""" & ruta & """")
        Catch ex As System.Exception
            System.Windows.Forms.MessageBox.Show("No se pudo abrir el Explorador:" & vbCrLf & _
                ex.Message, "Abrir carpeta", System.Windows.Forms.MessageBoxButtons.OK, _
                System.Windows.Forms.MessageBoxIcon.Error)
        End Try
    End Sub

End Class


' ================= ORDENADOR DE COLUMNAS =================
Public Class ColumnSorter
    Implements System.Collections.IComparer

    Private _columnIndex As Integer
    Private _ascendente As Boolean

    Public Sub New(columnIndex As Integer, ascendente As Boolean)
        _columnIndex = columnIndex
        _ascendente = ascendente
    End Sub

    Public Function Compare(x As Object, y As Object) As Integer Implements System.Collections.IComparer.Compare
        Dim itemX As System.Windows.Forms.ListViewItem = TryCast(x, System.Windows.Forms.ListViewItem)
        Dim itemY As System.Windows.Forms.ListViewItem = TryCast(y, System.Windows.Forms.ListViewItem)

        If itemX Is Nothing OrElse itemY Is Nothing Then Return 0

        Dim textoX As String = ""
        Dim textoY As String = ""

        If _columnIndex < itemX.SubItems.Count Then textoX = itemX.SubItems(_columnIndex).Text
        If _columnIndex < itemY.SubItems.Count Then textoY = itemY.SubItems(_columnIndex).Text

        Dim resultado As Integer

        ' Columnas 0, 4, 5 son texto. Columna 1, 2, 3 son numéricas si es posible
        If _columnIndex = 4 Then  ' Fecha
            Dim fechaX As DateTime = DateTime.MinValue
            Dim fechaY As DateTime = DateTime.MinValue
            DateTime.TryParse(textoX, fechaX)
            DateTime.TryParse(textoY, fechaY)
            resultado = fechaX.CompareTo(fechaY)
        Else
            resultado = String.Compare(textoX, textoY, True)
        End If

        If Not _ascendente Then resultado = -resultado
        Return resultado
    End Function

End Class
