' Apertura comun para piezas, planos y ensamblajes. Solo en el hilo de interfaz.
'
' v7.9.2: una ruta vacia ya no aborta la apertura.
'   Inventor puede tener abiertos documentos sin guardar (Pieza1, un plano
'   recien creado...) cuyo FullFileName es "". Al recorrer los documentos
'   visibles, GetFullPath("") lanzaba "The path is empty" y fallaba la
'   apertura de cualquier fichero que ya estuviera cargado en memoria sin
'   ventana, por ejemplo como referencia de un ensamblaje abierto. Por eso
'   fallaba solo con algunos ficheros. RutaRed devuelve ahora "" para rutas
'   vacias o no normalizables, y toda comparacion pasa por MismaRuta, que
'   nunca considera iguales dos rutas vacias.
Public Class AperturaInventorSegura
    Private Shared abriendo As Boolean = False

    Private Declare Unicode Function WNetGetConnection Lib "mpr.dll" Alias "WNetGetConnectionW" ( _
        localName As String, remoteName As System.Text.StringBuilder, ByRef length As Integer) As Integer

    Public Shared ReadOnly Property EnCurso As Boolean
        Get
            Return abriendo
        End Get
    End Property

    ' Ruta completa con la unidad mapeada sustituida por su UNC. Devuelve ""
    ' si la ruta esta vacia o no se puede normalizar; nunca lanza.
    Private Shared Function RutaRed(ruta As String) As String
        If String.IsNullOrWhiteSpace(ruta) Then Return ""
        Dim completa As String = ""
        Try
            completa = System.IO.Path.GetFullPath(ruta.Trim())
        Catch
            Return ""
        End Try
        If completa.Length >= 3 AndAlso completa(1) = ":"c Then
            Try
                Dim largo As Integer = 32768
                Dim buffer As New System.Text.StringBuilder(largo)
                If WNetGetConnection(completa.Substring(0, 2), buffer, largo) = 0 Then
                    Return buffer.ToString().TrimEnd("\"c) & completa.Substring(2)
                End If
            Catch
            End Try
        End If
        Return completa
    End Function

    ' True solo si la ruta de un documento coincide con la ruta buscada. Un
    ' documento sin guardar (ruta vacia) nunca coincide con nada.
    Private Shared Function MismaRuta(rutaDocumento As String, red As String) As Boolean
        If red = "" Then Return False
        Dim otra As String = RutaRed(rutaDocumento)
        If otra = "" Then Return False
        Return String.Equals(otra, red, System.StringComparison.OrdinalIgnoreCase)
    End Function

    Public Shared Sub Abrir(app As Inventor.Application, ruta As String)
        If app Is Nothing Then Throw New System.InvalidOperationException("Inventor no esta disponible.")
        If abriendo Then Throw New System.InvalidOperationException("Ya hay una apertura en curso.")
        abriendo = True
        Dim silencioAnterior As Boolean = False
        Dim restaurarSilencio As Boolean = False
        Dim rutaOriginal As String = ruta
        Dim rutaIntentada As String = ruta
        Dim fase As String = "Preparar ruta"
        Try
            If String.IsNullOrWhiteSpace(ruta) Then
                Throw New System.IO.FileNotFoundException("No se ha indicado ningun fichero.")
            End If
            rutaOriginal = System.IO.Path.GetFullPath(ruta.Trim())
            rutaIntentada = rutaOriginal
            Dim red As String = RutaRed(rutaOriginal)
            If red = "" Then red = rutaOriginal

            Dim existente As Inventor.Document = Nothing
            ' P:\ y su UNC son el mismo archivo; no lo abras como dos documentos.
            fase = "Buscar documento ya cargado"
            For Each doc As Inventor.Document In app.Documents
                Try
                    If MismaRuta(doc.FullFileName, red) Then
                        existente = doc
                        rutaIntentada = doc.FullFileName
                        Exit For
                    End If
                Catch
                    ' Un documento que no deja leer su ruta no es el buscado.
                End Try
            Next
            If existente Is Nothing AndAlso Not System.IO.File.Exists(rutaOriginal) Then
                Throw New System.IO.FileNotFoundException("Archivo no accesible.", rutaOriginal)
            End If
            silencioAnterior = app.SilentOperation
            restaurarSilencio = True
            app.SilentOperation = False

            ' Activa solo si YA tiene ventana. Un documento cargado como referencia
            ' o por el indexador necesita Open(..., True) antes de Activate.
            If existente IsNot Nothing Then
                fase = "Comprobar documento visible"
                Dim visibleEncontrado As Inventor.Document = Nothing
                For Each visible As Inventor.Document In app.Documents.VisibleDocuments
                    Try
                        If MismaRuta(visible.FullFileName, red) Then
                            visibleEncontrado = visible
                            Exit For
                        End If
                    Catch
                        ' Documentos sin guardar o sin ruta legible: se ignoran.
                    End Try
                Next
                If visibleEncontrado IsNot Nothing Then
                    fase = "Activar documento visible"
                    visibleEncontrado.Activate()
                    Return
                End If
            End If

            fase = "Abrir documento visible"
            Dim abierto As Inventor.Document = Nothing
            Try
                abierto = app.Documents.Open(rutaIntentada, True)
            Catch ex As System.Runtime.InteropServices.COMException
                ' Solo reintenta E_FAIL con el alias UNC real de Windows.
                ' No reintenta con otro alias un documento que ya estaba cargado.
                If existente IsNot Nothing OrElse ex.ErrorCode <> -2147467259 OrElse _
                        String.Equals(rutaOriginal, red, System.StringComparison.OrdinalIgnoreCase) OrElse _
                        Not System.IO.File.Exists(red) Then Throw
                rutaIntentada = red
                fase = "Abrir por ruta de red"
                abierto = app.Documents.Open(rutaIntentada, True)
            End Try
            If abierto Is Nothing Then Throw New System.InvalidOperationException("Inventor no devolvio un documento.")
            fase = "Activar documento abierto"
            abierto.Activate()
        Catch ex As System.Exception
            Dim proyecto As String = "No disponible"
            Try
                proyecto = app.DesignProjectManager.ActiveDesignProject.FullFileName
            Catch
            End Try
            Throw New System.InvalidOperationException( _
                "Archivo: " & rutaOriginal & vbCrLf & _
                "Ruta utilizada: " & rutaIntentada & vbCrLf & _
                "Operacion: " & fase & vbCrLf & _
                "Proyecto: " & proyecto & vbCrLf & _
                "Detalle: " & ex.ToString(), ex)
        Finally
            Try
                If restaurarSilencio Then app.SilentOperation = silencioAnterior
            Finally
                abriendo = False
            End Try
        End Try
    End Sub
End Class
