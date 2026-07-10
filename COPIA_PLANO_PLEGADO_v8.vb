Sub Main()

    '---------------------------------------------------------
    ' COPIA_PLANO_PLEGADO v8.7 - ENSAMBLAJE + IPROPERTIES FIABLES
    '
    ' Novedades v8.7 (CORRECCION DEFINITIVA IPROPERTIES EN LA COPIA):
    '  - El COD DE PLEGADO se asigna y GUARDA en la pieza origen ANTES
    '    de copiar el archivo. La copia nace ya con la propiedad dentro.
    '  - Antes la copia se hacia primero: si el IDW ya existia, la regla
    '    salia sin asignar codigo y la copia quedaba SIN la propiedad.
    '  - Verificacion doble: se relee la propiedad en el origen tras
    '    guardar, y en la copia tras abrirla (con respaldo de escritura
    '    directa + guardado si faltara).
    '  - Se cierra cualquier version antigua de la copia en memoria
    '    antes de abrir la nueva (evitaba ver la propiedad recien copiada).
    '
    ' Novedades v8.6:
    '  - Busqueda con comodin del CSV de duplicados: si no esta en la
    '    ruta configurada, se busca DUPLICADOS*.csv en esa carpeta, en
    '    su carpeta padre y en la subcarpeta AUDITORIA.
    '  - Si el CSV de control ya esta sincronizado (codigos 1119-1166
    '    marcados UTILIZADO), se puede dejar RUTA_CSV_DUPLICADOS_PLEGADO
    '    vacia y la regla funciona solo con el CSV de control.
    '
    ' Novedades v8.5:
    '  - La carpeta de destino de la pieza se elige SOLO entre carpetas
    '    con formato estricto de rango (.A02000-A02999, .40000-40999).
    '    Antes carpetas como "bh 1618 sueltas-40405" podian colarse.
    '  - Si no existe la carpeta del rango, se crea automaticamente:
    '    A03000 -> .A03000-A03999
    '
    ' Novedades v8.4:
    '  - Si el CSV de duplicados no se puede leer, la regla SE DETIENE
    '    con aviso en lugar de asignar codigos ya gastados en silencio.
    '  - El COD DE PLEGADO ya grabado en una pieza se valida contra
    '    DUPLICADOS_PL: si pertenece a otro articulo (ej: 1119 es de
    '    A02777), se ignora y se asigna un codigo nuevo.
    '  - Autocorreccion del CSV de control: las filas UTILIZADO con un
    '    articulo distinto al dueno real segun DUPLICADOS_PL se
    '    restauran automaticamente (marca SYNC DUPLICADOS).
    '
    ' Novedades v8.3:
    '  - Los codigos NO se asignan de forma consecutiva a ciegas:
    '    se cruza el CSV de control con DUPLICADOS_PL.csv. Los codigos
    '    que alli figuran como NO DISPONIBLE (ya gastados en la
    '    resolucion de duplicados) NUNCA se asignan a piezas nuevas.
    '  - Esas filas se sincronizan automaticamente en el CSV de control
    '    (pasan a UTILIZADO con su articulo y la marca SYNC DUPLICADOS).
    '  - Si el articulo ya tenia codigo reservado en DUPLICADOS_PL,
    '    se reutiliza ese codigo.
    '  - CORRECCION: el COD DE PLEGADO se escribe y GUARDA explicitamente
    '    en la copia del IPT, con aviso en pantalla si el guardado falla
    '    (antes fallaba en silencio y la copia quedaba sin la propiedad).
    '
    ' Novedades v8.2:
    '  - CODIGO DE PLEGADO AUTOMATICO: la regla toma el primer codigo
    '    DISPONIBLE del CSV CONTROL_CODIGOS_PLEGADO, lo marca como
    '    UTILIZADO (articulo, ruta IPT, fecha, usuario) y guarda el CSV.
    '  - Si el articulo ya tiene codigo (iProperty o fila del CSV),
    '    se reutiliza en lugar de gastar uno nuevo.
    '  - Lectura/escritura del CSV con 5 reintentos (evita el error
    '    "being used by another process" si esta abierto en Excel).
    '  - Ya no hace falta teclear el codigo a mano en planos nuevos.
    '
    ' Novedades v8.1:
    '  - Los planos del ensamblaje se guardan en la subcarpeta
    '    02_PLANOS_PLEGADO creada dentro de la carpeta del ensamblaje.
    '  - En modo ensamblaje NO se exporta PDF: solo IDW + DWF.
    '  - Los solidos (piezas que no son de chapa) quedan explicitamente
    '    excluidos de la generacion de planos.
    '
    ' Novedades v8.0:
    '  - MODO ENSAMBLAJE: si la regla se ejecuta desde un .IAM, detecta
    '    todas las piezas de chapa referenciadas y ofrece generar los
    '    planos de TODAS o elegir SOLO UNA de la lista.
    '    Los IDW/PDF/DWF se guardan en LA MISMA CARPETA del ensamblaje.
    '  - CORRECCION IPROPERTIES: la pieza se guarda (partDoc.Save) despues
    '    de escribir MATRIZ y COD DE PLEGADO, para que las iProperties
    '    queden en el archivo y no solo en memoria.
    '  - EscribirPropiedad ahora crea la propiedad si no existe en el set,
    '    en lugar de fallar en silencio.
    '  - El COD DE PLEGADO se escribe tambien en la pieza copiada.
    '
    ' Comportamiento v7.1 conservado en modo pieza individual:
    '  - Si el IDW YA EXISTE: NO se machaca; se abre para edicion manual.
    '  - Si el IDW NO EXISTE: se crea el plano completo A4.
    '---------------------------------------------------------

    Dim invApp As Inventor.Application = ThisApplication
    Dim tg As TransientGeometry = invApp.TransientGeometry

    Dim RUTA_PLANTILLA_BASE As String = _
        "Q:\BIBLIOTECA INVENTOR 2019\PLANTILLAS 2019\PLANO METALPLAK V19 -LOGO NUEVO"

    Dim RUTA_BASE_PLANOS_IDW As String = _
        "Q:\DISEÑOS\PLANOS PRODUCCIÓN"

    Dim RUTA_BASE_PIEZAS As String = _
        "Q:\DISEÑOS\60- PULVER AGRO"

    Dim NOMBRE_CARPETA_SALIDA As String = "02_PLANOS_PLEGADO"
    Dim NOMBRE_SIMBOLO_DATOS As String = "DATOS PLEGADO PULVER"
    Dim NOMBRE_SIMBOLO_POSICION As String = "OJO POSICIÓN"

    Dim NOMBRE_REGLA_IPROPERTIES As String = "IPROPERTIES"

    ' v8.2: CSV de control de codigos de plegado.
    ' Formato: COD_PLEGADO;ESTADO;COD_ARTICULO;RUTA_IPT;FECHA;USUARIO
    ' La regla toma automaticamente el primer codigo DISPONIBLE,
    ' lo marca como UTILIZADO y guarda el CSV.
    Dim RUTA_CSV_CONTROL_PLEGADO As String = _
        "Q:\MARIANO\00_CONTROL_PLEGADO\CONTROL_CODIGOS_PLEGADO.csv"

    ' v8.3: CSV de resolucion de duplicados.
    ' Formato: COD_PL;ARTICULO;RESOLUCION;COD_ANTERIOR;COD_NUEVO;ESTADO
    ' Los COD_NUEVO con estado NO DISPONIBLE ya estan gastados y
    ' NUNCA deben asignarse a piezas nuevas, aunque el CSV de control
    ' los siga mostrando como DISPONIBLE.
    Dim RUTA_CSV_DUPLICADOS_PLEGADO As String = _
        "Q:\MARIANO\00_CONTROL_PLEGADO\AUDITORIA\DUPLICADOS_PL.csv"

    Dim ESCALAS() As Double = { _
        1.0, _
        0.5, _
        0.25, _
        0.2, _
        0.1666666667, _
        0.125, _
        0.1, _
        0.05}

    Dim AUMENTAR_ESCALA_UN_PASO As Boolean = False

    Dim docActivo As Document = invApp.ActiveDocument

    If docActivo Is Nothing Then
        MessageBox.Show("No hay ningún documento activo.", "Copia y plano de plegado")
        Exit Sub
    End If

    '---------------------------------------------------------
    ' v8.0: MODO ENSAMBLAJE
    '---------------------------------------------------------
    If docActivo.DocumentType = DocumentTypeEnum.kAssemblyDocumentObject Then

        Dim asmDoc As AssemblyDocument = TryCast(docActivo, AssemblyDocument)

        If asmDoc Is Nothing Then
            MessageBox.Show("No se ha podido leer el ensamblaje activo.", "Planos de plegado del ensamblaje")
            Exit Sub
        End If

        ProcesarEnsamblajePlegado( _
            invApp, tg, asmDoc, _
            RUTA_PLANTILLA_BASE, _
            NOMBRE_SIMBOLO_DATOS, _
            NOMBRE_SIMBOLO_POSICION, _
            NOMBRE_REGLA_IPROPERTIES, _
            RUTA_CSV_CONTROL_PLEGADO, _
            RUTA_CSV_DUPLICADOS_PLEGADO, _
            ESCALAS)

        Exit Sub

    End If

    If docActivo.DocumentType <> DocumentTypeEnum.kPartDocumentObject Then
        MessageBox.Show( _
            "Ejecuta la regla desde una pieza .IPT de chapa o desde un ensamblaje .IAM.", _
            "Copia y plano de plegado")
        Exit Sub
    End If

    Dim piezaOrigen As PartDocument = TryCast(docActivo, PartDocument)

    If piezaOrigen Is Nothing Then
        MessageBox.Show("No se ha podido leer la pieza activa.", "Copia y plano de plegado")
        Exit Sub
    End If

    If piezaOrigen.FullFileName = "" Then
        MessageBox.Show("Guarda primero la pieza.", "Copia y plano de plegado")
        Exit Sub
    End If

    If Not EsPiezaChapa(piezaOrigen) Then
        MessageBox.Show( _
            "La pieza activa no es de chapa.", _
            "Copia y plano de plegado")
        Exit Sub
    End If

    Dim codigoPieza As String = _
        LeerPropiedad(piezaOrigen, "Design Tracking Properties", "Part Number")

    If codigoPieza = "" Then
        codigoPieza = _
            System.IO.Path.GetFileNameWithoutExtension(piezaOrigen.FullFileName)
    End If

    codigoPieza = NormalizarCodigoParaRango(codigoPieza)

    If codigoPieza = "" Then
        MessageBox.Show( _
            "No se ha podido determinar el código de la pieza.", _
            "Copia y plano de plegado")
        Exit Sub
    End If

    '---------------------------------------------------------
    ' v8.7: PASO 1 - ASIGNAR COD DE PLEGADO AL ORIGEN *ANTES* DE COPIAR
    '---------------------------------------------------------
    ' CAUSA DEL FALLO ANTERIOR: la copia se hacia ANTES de asignar el
    ' codigo. Si el IDW ya existia, la regla salia sin asignar nada y
    ' la copia quedaba en disco SIN el COD DE PLEGADO para siempre.
    ' Ahora el codigo se asigna y GUARDA en el origen primero, de modo
    ' que la copia del archivo ya nace con la propiedad dentro,
    ' en TODOS los caminos posibles (plano nuevo o plano existente).
    Dim codigoPlegadoAsignado As String = ""

    Try
        codigoPlegadoAsignado = AsignarCodigoPlegadoAutomatico( _
            invApp, _
            piezaOrigen, _
            RUTA_CSV_CONTROL_PLEGADO, _
            RUTA_CSV_DUPLICADOS_PLEGADO, _
            NOMBRE_REGLA_IPROPERTIES)
    Catch ex As Exception
        MessageBox.Show( _
            "No se ha podido asignar el COD DE PLEGADO automatico." & _
            vbCrLf & vbCrLf & ex.Message, _
            "Código de plegado")
        Exit Sub
    End Try

    If Trim(codigoPlegadoAsignado) = "" Then
        MessageBox.Show( _
            "No se ha podido obtener un COD DE PLEGADO." & vbCrLf & vbCrLf & _
            "Revisa que el CSV de control tenga códigos DISPONIBLES:" & vbCrLf & _
            RUTA_CSV_CONTROL_PLEGADO, _
            "Código de plegado")
        Exit Sub
    End If

    '---------------------------------------------------------
    ' v8.7: PASO 2 - GRABAR Y GUARDAR EL ORIGEN (VERIFICADO)
    '---------------------------------------------------------
    Try
        EscribirPropiedadUsuario(piezaOrigen, "COD DE PLEGADO", codigoPlegadoAsignado)

        PrepararDesarrolloPlanoEnPiezaOrigen(piezaOrigen)
        piezaOrigen.Update2(True)
        piezaOrigen.Save()

        ' Verificacion: la propiedad debe poder releerse tras guardar.
        If Trim(LeerPropiedadUsuario(piezaOrigen, "COD DE PLEGADO")) = "" Then
            Throw New Exception("La propiedad se escribio pero no se puede releer.")
        End If
    Catch ex As Exception
        MessageBox.Show( _
            "El COD DE PLEGADO " & codigoPlegadoAsignado & " se ha reservado en el CSV, " & _
            "pero NO se ha podido guardar en la pieza origen." & vbCrLf & vbCrLf & _
            "Detalle: " & ex.Message & vbCrLf & vbCrLf & _
            "Comprueba que la pieza no sea de solo lectura y vuelve a ejecutar. " & _
            "El codigo reservado se reutilizara (no se gasta otro).", _
            "Código de plegado")
        Exit Sub
    End Try

    '---------------------------------------------------------
    ' v8.7: PASO 3 - COPIAR IPT (YA CON EL CODIGO DENTRO)
    '---------------------------------------------------------

    Dim carpetaPieza As String = _
        BuscarCarpetaPiezaPorCodigo(RUTA_BASE_PIEZAS, codigoPieza)

    If carpetaPieza = "" Then
        MessageBox.Show( _
            "No se ha encontrado la carpeta de rango de la pieza." & vbCrLf & vbCrLf & _
            "Código: " & codigoPieza & vbCrLf & _
            "Raíz: " & RUTA_BASE_PIEZAS, _
            "Copia y plano de plegado")
        Exit Sub
    End If

    Dim rutaPiezaDestino As String = _
        System.IO.Path.Combine(carpetaPieza, codigoPieza & ".ipt")

    If Not CopiarPiezaConConfirmacionIntegrada( _
        invApp, _
        piezaOrigen.FullFileName, _
        rutaPiezaDestino) Then

        Exit Sub
    End If

    Dim partDoc As PartDocument = Nothing

    If String.Equals( _
        piezaOrigen.FullFileName, _
        rutaPiezaDestino, _
        StringComparison.OrdinalIgnoreCase) Then

        partDoc = piezaOrigen
    Else
        ' Cerrar cualquier version antigua de la copia que Inventor
        ' tuviera en memoria (sin propiedad) antes de abrir la nueva.
        CerrarDocumentoPorRutaIntegrada(invApp, rutaPiezaDestino)
        partDoc = ObtenerOAbrirPiezaIntegrada(invApp, rutaPiezaDestino)
    End If

    If partDoc Is Nothing Then
        MessageBox.Show( _
            "La pieza se ha copiado, pero no se ha podido abrir:" & vbCrLf & vbCrLf & _
            rutaPiezaDestino, _
            "Copia y plano de plegado")
        Exit Sub
    End If

    '---------------------------------------------------------
    ' v8.7: PASO 4 - VERIFICAR EL CODIGO EN LA COPIA (EN DISCO)
    '---------------------------------------------------------
    Try
        If Trim(LeerPropiedadUsuario(partDoc, "COD DE PLEGADO")) = "" Then
            ' Respaldo: escribir y guardar directamente en la copia.
            EscribirPropiedadUsuario(partDoc, "COD DE PLEGADO", codigoPlegadoAsignado)
            partDoc.Save()

            If Trim(LeerPropiedadUsuario(partDoc, "COD DE PLEGADO")) = "" Then
                Throw New Exception("La propiedad no se puede releer tras guardar.")
            End If
        End If
    Catch ex As Exception
        MessageBox.Show( _
            "AVISO: no se ha podido guardar el COD DE PLEGADO en la copia de la pieza:" & vbCrLf & _
            rutaPiezaDestino & vbCrLf & vbCrLf & _
            "Detalle: " & ex.Message & vbCrLf & vbCrLf & _
            "El plano se generara igualmente con el codigo " & codigoPlegadoAsignado & ", " & _
            "pero revisa las iProperties de la copia (archivo bloqueado o de solo lectura).", _
            "Código de plegado - copia")
    End Try

    '---------------------------------------------------------
    ' CALCULAR RUTAS FINALES
    '---------------------------------------------------------

    Dim carpetaPlano As String = _
        ObtenerCarpetaPlanoIDWPorCodigo(RUTA_BASE_PLANOS_IDW, codigoPieza)

    If Not System.IO.Directory.Exists(carpetaPlano) Then
        System.IO.Directory.CreateDirectory(carpetaPlano)
    End If

    Dim rutaIDW As String = _
        System.IO.Path.Combine(carpetaPlano, codigoPieza & ".idw")

    Dim rutaDWF As String = _
        System.IO.Path.Combine(carpetaPlano, codigoPieza & ".dwf")

    Dim rutaPDF As String = _
        System.IO.Path.Combine(carpetaPlano, codigoPieza & ".pdf")

    '---------------------------------------------------------
    ' SI EL PLANO EXISTE, SE ABRE PARA EDITAR (NO SE MACHACA)
    '---------------------------------------------------------
    ' v8.7: este camino ya deja la copia CON su COD DE PLEGADO,
    ' porque el codigo se asigna y guarda antes de copiar.

    If System.IO.File.Exists(rutaIDW) Then

        Dim planoExistente As DrawingDocument = _
            AbrirPlanoExistenteParaEdicion(invApp, rutaIDW)

        If planoExistente Is Nothing Then
            MessageBox.Show( _
                "El plano ya existe pero no se ha podido abrir:" & vbCrLf & vbCrLf & _
                rutaIDW & vbCrLf & vbCrLf & _
                "Comprueba que el archivo no esté bloqueado o abierto por otro usuario.", _
                "Copia y plano de plegado")
            Exit Sub
        End If

        MessageBox.Show( _
            "La pieza se ha copiado correctamente (con COD DE PLEGADO " & codigoPlegadoAsignado & "):" & vbCrLf & _
            rutaPiezaDestino & vbCrLf & vbCrLf & _
            "El plano YA EXISTÍA y NO se ha regenerado:" & vbCrLf & rutaIDW & vbCrLf & vbCrLf & _
            "Se ha abierto para que puedas editarlo y modificarlo manualmente." & vbCrLf & _
            "Recuerda reexportar PDF/DWF si haces cambios.", _
            "Plano existente abierto para edición")

        Exit Sub

    End If

    '---------------------------------------------------------
    ' EL IDW NO EXISTE -> LIMPIAR PDF/DWF HUERFANOS
    '---------------------------------------------------------

    Try
        If System.IO.File.Exists(rutaDWF) Then System.IO.File.Delete(rutaDWF)
        If System.IO.File.Exists(rutaPDF) Then System.IO.File.Delete(rutaPDF)
    Catch ex As Exception
        MessageBox.Show( _
            "Existen un PDF o DWF antiguos que no se pueden eliminar:" & vbCrLf & vbCrLf & _
            ex.Message & vbCrLf & vbCrLf & _
            "Ciérralos o desbloquéalos y vuelve a ejecutar la regla.", _
            "Copia y plano de plegado")
        Exit Sub
    End Try

    '---------------------------------------------------------
    ' CREAR PLANO DIRECTAMENTE
    '---------------------------------------------------------

    Try
        CrearPlanoPlegadoPiezaIndividual( _
            invApp, _
            tg, _
            partDoc, _
            RUTA_PLANTILLA_BASE, _
            RUTA_BASE_PLANOS_IDW, _
            NOMBRE_CARPETA_SALIDA, _
            NOMBRE_SIMBOLO_DATOS, _
            NOMBRE_SIMBOLO_POSICION, _
            ESCALAS, _
            AUMENTAR_ESCALA_UN_PASO, _
            codigoPlegadoAsignado, _
            "", _
            False)

        MessageBox.Show( _
            "Proceso finalizado correctamente." & vbCrLf & vbCrLf & _
            "Pieza:" & vbCrLf & rutaPiezaDestino & vbCrLf & vbCrLf & _
            "COD DE PLEGADO: " & codigoPlegadoAsignado & vbCrLf & vbCrLf & _
            "Plano:" & vbCrLf & rutaIDW & vbCrLf & vbCrLf & _
            "DWF:" & vbCrLf & rutaDWF & vbCrLf & vbCrLf & _
            "El plano queda abierto para revisión.", _
            "Copia y plano de plegado")

    Catch ex As Exception
        MessageBox.Show( _
            "No se ha podido crear el plano de plegado:" & vbCrLf & vbCrLf & _
            ex.Message, _
            "Copia y plano de plegado")
    End Try

End Sub


'---------------------------------------------------------
' v8.0: PROCESADO COMPLETO DE UN ENSAMBLAJE
'---------------------------------------------------------
' Detecta todas las piezas de chapa unicas referenciadas por el
' ensamblaje. Pregunta si se generan TODAS o SOLO UNA (lista).
' Los planos se guardan en la MISMA CARPETA del ensamblaje.
' Si un IDW ya existe en esa carpeta, la pieza se omite (no se machaca).

Sub ProcesarEnsamblajePlegado( _
    ByVal invApp As Inventor.Application, _
    ByVal tg As TransientGeometry, _
    ByVal asmDoc As AssemblyDocument, _
    ByVal rutaPlantillaBase As String, _
    ByVal nombreSimboloDatos As String, _
    ByVal nombreSimboloPosicion As String, _
    ByVal nombreReglaIProperties As String, _
    ByVal rutaCSVControlPlegado As String, _
    ByVal rutaCSVDuplicadosPlegado As String, _
    ByVal escalasDisponibles() As Double)

    If asmDoc.FullFileName = "" Then
        MessageBox.Show("Guarda primero el ensamblaje.", "Planos de plegado del ensamblaje")
        Exit Sub
    End If

    Dim carpetaEnsamblaje As String = _
        System.IO.Path.GetDirectoryName(asmDoc.FullFileName)

    ' v8.1: los planos van a la subcarpeta 02_PLANOS_PLEGADO dentro
    ' de la carpeta del ensamblaje. Se crea si no existe.
    Dim carpetaSalidaPlanos As String = _
        System.IO.Path.Combine(carpetaEnsamblaje, "02_PLANOS_PLEGADO")

    Try
        If Not System.IO.Directory.Exists(carpetaSalidaPlanos) Then
            System.IO.Directory.CreateDirectory(carpetaSalidaPlanos)
        End If
    Catch ex As Exception
        MessageBox.Show( _
            "No se ha podido crear la carpeta de salida:" & vbCrLf & _
            carpetaSalidaPlanos & vbCrLf & vbCrLf & ex.Message, _
            "Planos de plegado del ensamblaje")
        Exit Sub
    End Try

    '-----------------------------------------------------
    ' 1) RECOLECTAR PIEZAS DE CHAPA UNICAS
    '-----------------------------------------------------
    ' Solo entran piezas de CHAPA. Los solidos normales (piezas .IPT
    ' que no son SheetMetal) quedan excluidos de la generacion de planos.
    Dim piezas As New System.Collections.Generic.List(Of PartDocument)
    Dim codigos As New System.Collections.Generic.List(Of String)
    Dim rutasVistas As New System.Collections.Generic.List(Of String)

    For Each refDoc As Document In asmDoc.AllReferencedDocuments
        Try
            If refDoc.DocumentType <> DocumentTypeEnum.kPartDocumentObject Then Continue For

            Dim p As PartDocument = TryCast(refDoc, PartDocument)
            If p Is Nothing Then Continue For
            If p.FullFileName = "" Then Continue For

            ' v8.1: EXCLUIR SOLIDOS. Solo las piezas de chapa
            ' (SheetMetalComponentDefinition) generan plano.
            If Not EsPiezaChapa(p) Then Continue For

            Dim clave As String = p.FullFileName.ToUpperInvariant()
            If rutasVistas.Contains(clave) Then Continue For
            rutasVistas.Add(clave)

            Dim codigo As String = _
                LeerPropiedad(p, "Design Tracking Properties", "Part Number")

            If codigo = "" Then
                codigo = System.IO.Path.GetFileNameWithoutExtension(p.FullFileName)
            End If

            codigo = NormalizarCodigoParaRango(codigo)
            If codigo = "" Then Continue For

            piezas.Add(p)
            codigos.Add(codigo)
        Catch
        End Try
    Next

    If piezas.Count = 0 Then
        MessageBox.Show( _
            "El ensamblaje no referencia ninguna pieza de chapa.", _
            "Planos de plegado del ensamblaje")
        Exit Sub
    End If

    '-----------------------------------------------------
    ' 2) ELEGIR: TODAS O SOLO UNA
    '-----------------------------------------------------
    Dim respuesta As System.Windows.Forms.DialogResult = _
        System.Windows.Forms.MessageBox.Show( _
            "Se han detectado " & piezas.Count.ToString() & " piezas de chapa en el ensamblaje." & vbCrLf & vbCrLf & _
            "SÍ  = Generar los planos de TODAS las piezas" & vbCrLf & _
            "NO  = Elegir UNA sola pieza de la lista" & vbCrLf & _
            "CANCELAR = Salir sin hacer nada" & vbCrLf & vbCrLf & _
            "Los planos (IDW + DWF, sin PDF) se guardarán en:" & vbCrLf & carpetaSalidaPlanos, _
            "Planos de plegado del ensamblaje", _
            System.Windows.Forms.MessageBoxButtons.YesNoCancel, _
            System.Windows.Forms.MessageBoxIcon.Question)

    Dim indicesSeleccionados As New System.Collections.Generic.List(Of Integer)

    If respuesta = System.Windows.Forms.DialogResult.Cancel Then
        Exit Sub
    ElseIf respuesta = System.Windows.Forms.DialogResult.Yes Then
        For i As Integer = 0 To piezas.Count - 1
            indicesSeleccionados.Add(i)
        Next
    Else
        ' Elegir una sola pieza de la lista.
        Dim elegido As String = InputListBox( _
            "Elige la pieza de chapa para generar su plano:", _
            codigos, _
            codigos(0), _
            "Planos de plegado del ensamblaje", _
            "Piezas de chapa")

        If elegido Is Nothing OrElse Trim(CStr(elegido)) = "" Then Exit Sub

        Dim indiceElegido As Integer = -1
        For i As Integer = 0 To codigos.Count - 1
            If String.Equals(codigos(i), CStr(elegido), StringComparison.OrdinalIgnoreCase) Then
                indiceElegido = i
                Exit For
            End If
        Next

        If indiceElegido < 0 Then Exit Sub
        indicesSeleccionados.Add(indiceElegido)
    End If

    '-----------------------------------------------------
    ' 3) GENERAR LOS PLANOS EN 02_PLANOS_PLEGADO
    '-----------------------------------------------------
    ' v8.1: salida IDW + DWF (sin PDF) en la subcarpeta del ensamblaje.
    Dim generadas As New System.Collections.Generic.List(Of String)
    Dim existentes As New System.Collections.Generic.List(Of String)
    Dim fallidas As New System.Collections.Generic.List(Of String)

    Dim cerrarTrasGuardar As Boolean = (indicesSeleccionados.Count > 1)

    For Each idx As Integer In indicesSeleccionados

        Dim p As PartDocument = piezas(idx)
        Dim codigo As String = codigos(idx)

        Dim rutaIDW As String = _
            System.IO.Path.Combine(carpetaSalidaPlanos, codigo & ".idw")

        ' No machacar planos existentes (criterio v7.1).
        If System.IO.File.Exists(rutaIDW) Then
            existentes.Add(codigo)
            Continue For
        End If

        ' v8.2: COD DE PLEGADO AUTOMATICO. Orden: iProperty existente >
        ' codigo ya asignado al articulo en el CSV > primer DISPONIBLE
        ' del CSV (se marca UTILIZADO y se guarda) > regla IPROPERTIES.
        ' Solo si todo falla se pide a mano.
        Dim codPlegado As String = ""

        Try
            codPlegado = AsignarCodigoPlegadoAutomatico( _
                invApp, p, rutaCSVControlPlegado, _
                rutaCSVDuplicadosPlegado, nombreReglaIProperties)
        Catch
            codPlegado = ""
        End Try

        If codPlegado = "" Then
            codPlegado = Trim(InputBox( _
                "COD DE PLEGADO para la pieza " & codigo & vbCrLf & _
                "(dejar vacío = omitir esta pieza):", _
                "Código de plegado", ""))
        End If

        If codPlegado = "" Then
            fallidas.Add(codigo & " (sin COD DE PLEGADO)")
            Continue For
        End If

        Try
            CrearPlanoPlegadoPiezaIndividual( _
                invApp, _
                tg, _
                p, _
                rutaPlantillaBase, _
                carpetaSalidaPlanos, _
                "", _
                nombreSimboloDatos, _
                nombreSimboloPosicion, _
                escalasDisponibles, _
                False, _
                codPlegado, _
                carpetaSalidaPlanos, _
                cerrarTrasGuardar, _
                False)

            generadas.Add(codigo)
        Catch ex As Exception
            fallidas.Add(codigo & " (" & ex.Message & ")")
        End Try
    Next

    '-----------------------------------------------------
    ' 4) RESUMEN FINAL
    '-----------------------------------------------------
    Dim resumen As String = _
        "Planos de plegado del ensamblaje terminados." & vbCrLf & _
        "Carpeta: " & carpetaSalidaPlanos & vbCrLf & _
        "Formatos: IDW + DWF (sin PDF)" & vbCrLf & vbCrLf & _
        "GENERADOS (" & generadas.Count.ToString() & "):" & vbCrLf & _
        ResumirListaCodigos(generadas) & vbCrLf & vbCrLf & _
        "YA EXISTÍAN - NO tocados (" & existentes.Count.ToString() & "):" & vbCrLf & _
        ResumirListaCodigos(existentes) & vbCrLf & vbCrLf & _
        "FALLIDOS / OMITIDOS (" & fallidas.Count.ToString() & "):" & vbCrLf & _
        ResumirListaCodigos(fallidas)

    MessageBox.Show(resumen, "Planos de plegado del ensamblaje")

End Sub


Function ResumirListaCodigos( _
    ByVal lista As System.Collections.Generic.List(Of String)) As String

    If lista Is Nothing OrElse lista.Count = 0 Then Return "  (ninguno)"

    Dim salida As String = ""
    For Each c As String In lista
        If salida <> "" Then salida &= vbCrLf
        salida &= "  - " & c
    Next

    Return salida

End Function


'---------------------------------------------------------
' v8.2: CODIGO DE PLEGADO AUTOMATICO DESDE CSV DE CONTROL
'---------------------------------------------------------
' CSV: COD_PLEGADO;ESTADO;COD_ARTICULO;RUTA_IPT;FECHA;USUARIO
'
' Orden de asignacion:
'   1. iProperty COD DE PLEGADO ya presente en la pieza
'      (se sincroniza el CSV si esa fila seguia DISPONIBLE).
'   2. Codigo ya asignado a este articulo en el CSV (se reutiliza).
'   3. Primer codigo DISPONIBLE del CSV: se marca UTILIZADO con
'      articulo, ruta, fecha y usuario, y se guarda el CSV.
'   4. Regla externa IPROPERTIES como ultimo recurso.
'
' El CSV se lee/escribe con reintentos por si Excel lo tiene abierto.

Function AsignarCodigoPlegadoAutomatico( _
    ByVal invApp As Inventor.Application, _
    ByVal partDoc As PartDocument, _
    ByVal rutaCSV As String, _
    ByVal rutaCSVDuplicados As String, _
    ByVal nombreReglaIProperties As String) As String

    If partDoc Is Nothing Then Return ""

    Dim codArticulo As String = _
        LeerPropiedad(partDoc, "Design Tracking Properties", "Part Number")

    If codArticulo = "" Then
        codArticulo = System.IO.Path.GetFileNameWithoutExtension(partDoc.FullFileName)
    End If

    codArticulo = NormalizarCodigoParaRango(codArticulo)

    Dim rutaIPT As String = partDoc.FullFileName

    ' 1) iProperty ya presente en la pieza.
    ' v8.4: el codigo existente se VALIDA contra DUPLICADOS_PL antes de
    ' reutilizarlo. Si ese codigo pertenece a OTRO articulo (ej: una
    ' prueba anterior grabo el 1119 que es de A02777), NO se reutiliza:
    ' se sigue al paso 2/3 para tomar un codigo nuevo del CSV.
    Dim codigoExistente As String = _
        Trim(LeerPropiedadUsuario(partDoc, "COD DE PLEGADO"))

    If codigoExistente <> "" Then

        Dim dupNoDisp As New System.Collections.Generic.Dictionary(Of String, String)
        Dim dupPorArt As New System.Collections.Generic.Dictionary(Of String, String)
        Dim duplicadosCargados As Boolean = _
            CargarDuplicadosNoDisponibles(rutaCSVDuplicados, dupNoDisp, dupPorArt)

        Dim codigoEnConflicto As Boolean = False

        If duplicadosCargados AndAlso dupNoDisp.ContainsKey(codigoExistente) Then
            Dim duenoDuplicado As String = dupNoDisp(codigoExistente)
            If duenoDuplicado <> "" AndAlso _
               duenoDuplicado <> codArticulo.ToUpperInvariant() Then
                codigoEnConflicto = True
            End If
        End If

        If Not codigoEnConflicto Then
            ' Sincronizar el CSV por si esa fila seguia como DISPONIBLE.
            Try
                MarcarCodigoUtilizadoEnCSV(rutaCSV, codigoExistente, codArticulo, rutaIPT)
            Catch
            End Try

            ' Asegurar que la propiedad queda GUARDADA en el archivo.
            Try
                EscribirPropiedadUsuario(partDoc, "COD DE PLEGADO", codigoExistente)
                If partDoc.FullFileName <> "" AndAlso partDoc.Dirty Then
                    partDoc.Save()
                End If
            Catch
            End Try

            Return codigoExistente
        End If
        ' Con conflicto: se ignora el codigo grabado y se pide uno nuevo.
    End If

    ' 2) y 3) Buscar en el CSV: reutilizar el del articulo (control o
    ' duplicados) o tomar el primer DISPONIBLE realmente libre.
    ' v8.4: los errores del CSV ya NO se tragan en silencio; suben al
    ' llamador para que el usuario vea el motivo real.
    Dim codigoCSV As String = TomarCodigoPlegadoDeCSV( _
        rutaCSV, rutaCSVDuplicados, codArticulo, rutaIPT)

    If codigoCSV <> "" Then
        ' Guardar el codigo en la pieza.
        EscribirPropiedadUsuario(partDoc, "COD DE PLEGADO", codigoCSV)
        Try
            If partDoc.FullFileName <> "" Then partDoc.Save()
        Catch
        End Try
        Return codigoCSV
    End If

    ' 4) Ultimo recurso: la regla externa IPROPERTIES.
    Try
        Return EjecutarIPropertiesModoCodigoPlegado( _
            invApp, partDoc, nombreReglaIProperties)
    Catch
        Return ""
    End Try

End Function


' v8.3: Devuelve el codigo asignado a este articulo si ya existe
' (en el CSV de control O en DUPLICADOS_PL), o toma el primer
' DISPONIBLE realmente libre. Los codigos que DUPLICADOS_PL marca
' como NO DISPONIBLE nunca se asignan: se sincronizan en el CSV de
' control como UTILIZADO con su articulo y se salta al siguiente.
Function TomarCodigoPlegadoDeCSV( _
    ByVal rutaCSV As String, _
    ByVal rutaCSVDuplicados As String, _
    ByVal codArticulo As String, _
    ByVal rutaIPT As String) As String

    If Trim(rutaCSV) = "" Then Return ""
    If Not System.IO.File.Exists(rutaCSV) Then
        Throw New Exception("No existe el CSV de control: " & rutaCSV)
    End If

    ' Codigos ya gastados segun DUPLICADOS_PL (COD_NUEVO;NO DISPONIBLE).
    ' v8.4: si el CSV de duplicados esta configurado pero NO se puede
    ' cargar, la regla se DETIENE. Continuar en silencio provocaba que
    ' se asignaran codigos ya gastados (ej: 1119 de A02777).
    Dim codigosNoDisponibles As New System.Collections.Generic.Dictionary(Of String, String)
    Dim codigoPorArticuloDup As New System.Collections.Generic.Dictionary(Of String, String)

    If Trim(rutaCSVDuplicados) <> "" Then
        If Not CargarDuplicadosNoDisponibles( _
            rutaCSVDuplicados, codigosNoDisponibles, codigoPorArticuloDup) Then

            Throw New Exception( _
                "No se ha podido leer el CSV de duplicados:" & vbCrLf & _
                rutaCSVDuplicados & vbCrLf & vbCrLf & _
                "Sin ese archivo no se puede comprobar que codigos ya estan " & _
                "gastados (1112-1166) y NO se asigna ningun codigo nuevo." & vbCrLf & _
                "Revisa la ruta o deja la constante RUTA_CSV_DUPLICADOS_PLEGADO " & _
                "vacia si ya no quieres usar ese control.")
        End If
    End If

    Dim articuloClave As String = codArticulo.ToUpperInvariant()

    Dim lineas() As String = LeerLineasCSVConReintentos(rutaCSV)
    If lineas Is Nothing OrElse lineas.Length < 2 Then Return ""

    Dim csvModificado As Boolean = False
    Dim indiceDisponible As Integer = -1
    Dim codigoAsignado As String = ""

    ' Codigo ya reservado a este articulo por la resolucion de duplicados.
    Dim codigoReservadoDup As String = ""
    If codigoPorArticuloDup.ContainsKey(articuloClave) Then
        codigoReservadoDup = codigoPorArticuloDup(articuloClave)
    End If

    For i As Integer = 1 To lineas.Length - 1
        Dim campos() As String = lineas(i).Split(";"c)
        If campos.Length < 3 Then Continue For

        Dim codigoFila As String = Trim(campos(0))
        Dim estado As String = Trim(campos(1)).ToUpperInvariant()
        Dim articuloFila As String = Trim(campos(2)).ToUpperInvariant()

        ' a) El articulo ya tiene codigo en el CSV de control.
        ' v8.4: solo se reutiliza si DUPLICADOS_PL no dice que ese codigo
        ' pertenece a OTRO articulo (fila contaminada por una prueba).
        If codigoAsignado = "" AndAlso _
           estado = "UTILIZADO" AndAlso _
           articuloFila <> "" AndAlso _
           articuloFila = articuloClave Then

            If Not (codigosNoDisponibles.ContainsKey(codigoFila) AndAlso _
                    codigosNoDisponibles(codigoFila) <> articuloClave) Then
                codigoAsignado = codigoFila
            End If
        End If

        ' NOTA v8.4: las filas UTILIZADO cuyo articulo difiere del dueno
        ' segun DUPLICADOS_PL (ej: 1112-1118) NO se tocan: son dobles
        ' reservas historicas que debe resolver el usuario a mano.

        ' b) El articulo tiene codigo reservado por DUPLICADOS_PL:
        '    se reutiliza y se sincroniza su fila si seguia DISPONIBLE.
        If codigoAsignado = "" AndAlso _
           codigoReservadoDup <> "" AndAlso _
           codigoFila = codigoReservadoDup Then

            If estado <> "UTILIZADO" Then
                lineas(i) = _
                    codigoFila & ";UTILIZADO;" & codArticulo & ";" & rutaIPT & ";" & _
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & ";" & _
                    ObtenerUsuarioWindowsActual()
                csvModificado = True
            End If

            codigoAsignado = codigoFila
        End If

        ' c) Fila DISPONIBLE pero el codigo ya esta gastado segun
        '    DUPLICADOS_PL: NO se asigna. Se sincroniza como UTILIZADO
        '    con el articulo del duplicado y se sigue buscando.
        If estado = "DISPONIBLE" AndAlso _
           codigosNoDisponibles.ContainsKey(codigoFila) Then

            lineas(i) = _
                codigoFila & ";UTILIZADO;" & _
                codigosNoDisponibles(codigoFila) & ";;" & _
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & ";" & _
                ObtenerUsuarioWindowsActual() & " (SYNC DUPLICADOS)"
            csvModificado = True
            Continue For
        End If

        ' d) Primer DISPONIBLE realmente libre.
        If indiceDisponible < 0 AndAlso estado = "DISPONIBLE" Then
            indiceDisponible = i
        End If
    Next

    ' Consumir el primer DISPONIBLE libre si el articulo no tenia codigo.
    If codigoAsignado = "" Then

        If indiceDisponible < 0 Then
            ' Guardar las sincronizaciones aunque no haya codigo libre.
            If csvModificado Then EscribirLineasCSVConReintentos(rutaCSV, lineas)
            Throw New Exception( _
                "El CSV de control no tiene codigos DISPONIBLES libres." & vbCrLf & rutaCSV)
        End If

        Dim camposFila() As String = lineas(indiceDisponible).Split(";"c)
        codigoAsignado = Trim(camposFila(0))

        lineas(indiceDisponible) = _
            codigoAsignado & ";UTILIZADO;" & _
            codArticulo & ";" & _
            rutaIPT & ";" & _
            DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & ";" & _
            ObtenerUsuarioWindowsActual()
        csvModificado = True
    End If

    ' Una sola escritura con la asignacion + todas las sincronizaciones.
    If csvModificado Then
        EscribirLineasCSVConReintentos(rutaCSV, lineas)
    End If

    Return codigoAsignado

End Function


' v8.4: Lee DUPLICADOS_PL.csv y devuelve:
'   codigosNoDisponibles: COD_NUEVO -> ARTICULO (filas NO DISPONIBLE)
'   codigoPorArticulo:    ARTICULO  -> COD_NUEVO (para reutilizar)
' Devuelve True si el archivo se cargo correctamente; False si no se
' encuentra o no se puede leer (el llamador decide si es error fatal).
Function CargarDuplicadosNoDisponibles( _
    ByVal rutaCSVDuplicados As String, _
    ByRef codigosNoDisponibles As System.Collections.Generic.Dictionary(Of String, String), _
    ByRef codigoPorArticulo As System.Collections.Generic.Dictionary(Of String, String)) As Boolean

    Try
        If Trim(rutaCSVDuplicados) = "" Then Return False

        ' Busqueda de respaldo: si no esta en la ruta configurada, se
        ' prueba en la carpeta padre y en la subcarpeta AUDITORIA.
        If Not System.IO.File.Exists(rutaCSVDuplicados) Then
            Dim nombre As String = System.IO.Path.GetFileName(rutaCSVDuplicados)
            Dim carpeta As String = System.IO.Path.GetDirectoryName(rutaCSVDuplicados)

            Dim candidatoPadre As String = System.IO.Path.Combine( _
                System.IO.Path.GetDirectoryName(carpeta), nombre)
            Dim candidatoAuditoria As String = System.IO.Path.Combine( _
                System.IO.Path.Combine(carpeta, "AUDITORIA"), nombre)

            If System.IO.File.Exists(candidatoPadre) Then
                rutaCSVDuplicados = candidatoPadre
            ElseIf System.IO.File.Exists(candidatoAuditoria) Then
                rutaCSVDuplicados = candidatoAuditoria
            Else
                ' v8.6: busqueda final con comodin DUPLICADOS*.csv en la
                ' carpeta configurada, su padre y la subcarpeta AUDITORIA.
                Dim encontrado As String = ""
                Dim carpetasBusqueda() As String = { _
                    carpeta, _
                    System.IO.Path.GetDirectoryName(carpeta), _
                    System.IO.Path.Combine(carpeta, "AUDITORIA")}

                For Each dirBusqueda As String In carpetasBusqueda
                    Try
                        If dirBusqueda <> "" AndAlso _
                           System.IO.Directory.Exists(dirBusqueda) Then

                            Dim coincidencias() As String = _
                                System.IO.Directory.GetFiles( _
                                    dirBusqueda, "DUPLICADOS*.csv")

                            If coincidencias.Length > 0 Then
                                encontrado = coincidencias(0)
                                Exit For
                            End If
                        End If
                    Catch
                    End Try
                Next

                If encontrado = "" Then Return False
                rutaCSVDuplicados = encontrado
            End If
        End If

        Dim lineas() As String = LeerLineasCSVConReintentos(rutaCSVDuplicados)
        If lineas Is Nothing OrElse lineas.Length < 2 Then Return True

        ' Formato: COD_PL;ARTICULO;RESOLUCION;COD_ANTERIOR;COD_NUEVO;ESTADO
        For i As Integer = 1 To lineas.Length - 1
            Try
                Dim campos() As String = lineas(i).Split(";"c)
                If campos.Length < 6 Then Continue For

                Dim articulo As String = Trim(campos(1)).ToUpperInvariant()
                Dim codNuevo As String = Trim(campos(4))
                Dim estado As String = Trim(campos(5)).ToUpperInvariant()

                If codNuevo = "" Then Continue For

                If estado.Contains("NO DISPONIBLE") Then

                    If Not codigosNoDisponibles.ContainsKey(codNuevo) Then
                        codigosNoDisponibles.Add(codNuevo, articulo)
                    End If

                    If articulo <> "" AndAlso _
                       Not codigoPorArticulo.ContainsKey(articulo) Then
                        codigoPorArticulo.Add(articulo, codNuevo)
                    End If
                End If
            Catch
            End Try
        Next

        Return True

    Catch
        ' v8.4: el fallo de lectura se comunica al llamador, que decide
        ' si es un error fatal (asignacion de codigo nuevo) o tolerable.
        Return False
    End Try

End Function


' Marca como UTILIZADO un codigo concreto (sincronizacion cuando la
' pieza ya traia el codigo en sus iProperties).
Sub MarcarCodigoUtilizadoEnCSV( _
    ByVal rutaCSV As String, _
    ByVal codigo As String, _
    ByVal codArticulo As String, _
    ByVal rutaIPT As String)

    If Trim(rutaCSV) = "" Then Exit Sub
    If Trim(codigo) = "" Then Exit Sub
    If Not System.IO.File.Exists(rutaCSV) Then Exit Sub

    Dim lineas() As String = LeerLineasCSVConReintentos(rutaCSV)
    If lineas Is Nothing OrElse lineas.Length < 2 Then Exit Sub

    Dim cambiado As Boolean = False

    For i As Integer = 1 To lineas.Length - 1
        Dim campos() As String = lineas(i).Split(";"c)
        If campos.Length < 2 Then Continue For

        If Trim(campos(0)) = Trim(codigo) Then

            Dim estado As String = Trim(campos(1)).ToUpperInvariant()

            ' Solo se escribe si la fila seguia DISPONIBLE o sin articulo.
            Dim articuloFila As String = ""
            If campos.Length >= 3 Then articuloFila = Trim(campos(2))

            If estado <> "UTILIZADO" OrElse articuloFila = "" Then
                lineas(i) = _
                    Trim(codigo) & ";UTILIZADO;" & _
                    codArticulo & ";" & _
                    rutaIPT & ";" & _
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & ";" & _
                    ObtenerUsuarioWindowsActual()
                cambiado = True
            End If

            Exit For
        End If
    Next

    If cambiado Then
        EscribirLineasCSVConReintentos(rutaCSV, lineas)
    End If

End Sub


' Lectura con reintentos: evita el error "being used by another process"
' cuando el CSV esta abierto en Excel u otra sesion lo esta escribiendo.
Function LeerLineasCSVConReintentos(ByVal rutaCSV As String) As String()

    Dim ultimoError As String = ""

    For intento As Integer = 1 To 5
        Try
            Return System.IO.File.ReadAllLines( _
                rutaCSV, System.Text.Encoding.Default)
        Catch ex As Exception
            ultimoError = ex.Message
            System.Threading.Thread.Sleep(500)
        End Try
    Next

    Throw New Exception( _
        "No se ha podido LEER el CSV de control tras 5 intentos." & vbCrLf & _
        "Cierra el archivo en Excel y vuelve a ejecutar la regla." & vbCrLf & _
        rutaCSV & vbCrLf & vbCrLf & ultimoError)

End Function


Sub EscribirLineasCSVConReintentos( _
    ByVal rutaCSV As String, _
    ByVal lineas() As String)

    Dim ultimoError As String = ""

    For intento As Integer = 1 To 5
        Try
            System.IO.File.WriteAllLines( _
                rutaCSV, lineas, System.Text.Encoding.Default)
            Exit Sub
        Catch ex As Exception
            ultimoError = ex.Message
            System.Threading.Thread.Sleep(500)
        End Try
    Next

    Throw New Exception( _
        "No se ha podido GUARDAR el CSV de control tras 5 intentos." & vbCrLf & _
        "El codigo se ha asignado a la pieza pero el CSV NO se ha actualizado." & vbCrLf & _
        "Cierra el archivo en Excel y actualiza la fila a mano, o vuelve a ejecutar." & vbCrLf & _
        rutaCSV & vbCrLf & vbCrLf & ultimoError)

End Sub


'---------------------------------------------------------
' APERTURA DE PLANO EXISTENTE PARA EDICION
'---------------------------------------------------------

Function AbrirPlanoExistenteParaEdicion( _
    ByVal invApp As Inventor.Application, _
    ByVal rutaIDW As String) As DrawingDocument

    Try
        For Each doc As Document In invApp.Documents
            Try
                If doc.DocumentType = DocumentTypeEnum.kDrawingDocumentObject AndAlso _
                   String.Equals(doc.FullFileName, rutaIDW, StringComparison.OrdinalIgnoreCase) Then

                    Dim yaAbierto As DrawingDocument = TryCast(doc, DrawingDocument)

                    If yaAbierto IsNot Nothing Then
                        Try
                            yaAbierto.Activate()
                        Catch
                        End Try
                        Return yaAbierto
                    End If
                End If
            Catch
            End Try
        Next

        Dim plano As DrawingDocument = _
            TryCast(invApp.Documents.Open(rutaIDW, True), DrawingDocument)

        If plano Is Nothing Then Return Nothing

        Try
            plano.Activate()
        Catch
        End Try

        Return plano

    Catch
        Return Nothing
    End Try

End Function


'---------------------------------------------------------
' EJECUCION DE IPROPERTIES EN MODO ASIGNACION
'---------------------------------------------------------

Function EjecutarIPropertiesModoCodigoPlegado( _
    ByVal invApp As Inventor.Application, _
    ByVal partDoc As PartDocument, _
    ByVal nombreRegla As String) As String

    If invApp Is Nothing Then
        Throw New Exception("No existe la aplicación de Inventor.")
    End If

    If partDoc Is Nothing Then
        Throw New Exception("No existe la pieza sobre la que asignar el código.")
    End If

    If Trim(nombreRegla) = "" Then
        Throw New Exception("El nombre de la regla IPROPERTIES está vacío.")
    End If

    Const NOMBRE_BANDERA As String = "__MODO_ASIGNAR_COD_PLEGADO"

    EscribirPropiedadUsuario(partDoc, NOMBRE_BANDERA, "ASIGNAR")

    If UCase(Trim(LeerPropiedadUsuario(partDoc, NOMBRE_BANDERA))) <> "ASIGNAR" Then
        Throw New Exception( _
            "No se ha podido preparar el modo de asignación dentro del IPT.")
    End If

    Dim argumentos As NameValueMap = _
        invApp.TransientObjects.CreateNameValueMap()

    argumentos.Add("ASIGNAR_COD_PLEGADO", True)

    Dim resultado As Integer = -999

    Try
        resultado = iLogicVb.Automation.RunExternalRuleWithArguments( _
            partDoc, _
            nombreRegla, _
            argumentos)
    Finally
        EliminarPropiedadUsuario(partDoc, NOMBRE_BANDERA)
    End Try

    If resultado <> 0 Then
        Throw New Exception( _
            "La regla externa '" & nombreRegla & _
            "' devolvió el código de error " & resultado.ToString() & ".")
    End If

    Dim codigo As String = _
        Trim(LeerPropiedadUsuario(partDoc, "COD DE PLEGADO"))

    If codigo = "" Then
        Throw New Exception( _
            "La regla IPROPERTIES no entró en modo de asignación o no pudo escribir " & _
            "la iProperty 'COD DE PLEGADO'.")
    End If

    Return codigo

End Function


'---------------------------------------------------------
' COPIA Y APERTURA DE LA PIEZA
'---------------------------------------------------------

' v8.5: SOLO acepta carpetas con formato estricto de rango:
'   .A02000-A02999 / A02000-A02999   (codigos con prefijo A)
'   .40000-40999   / 40000-40999     (codigos numericos)
' Antes cualquier carpeta con dos numeros y una letra A en el nombre
' (ej: "bh 1618 sueltas-40405") podia colarse como carpeta de rango.
' Si no existe la carpeta del rango, SE CREA (bloques de 1000,
' con punto inicial como las existentes: .A03000-A03999).
Function BuscarCarpetaPiezaPorCodigo( _
    ByVal raiz As String, _
    ByVal codigo As String) As String

    Try
        If Not System.IO.Directory.Exists(raiz) Then Return ""

        Dim esA As Boolean = codigo.ToUpperInvariant().StartsWith("A")
        Dim numero As Integer = ObtenerParteNumericaCodigoIntegrada(codigo)

        Dim mejor As String = ""
        Dim mejorAmplitud As Integer = Integer.MaxValue

        Dim patron As String
        If esA Then
            patron = "^\.?A0*(\d+)\s*-\s*A0*(\d+)$"
        Else
            patron = "^\.?0*(\d+)\s*-\s*0*(\d+)$"
        End If

        For Each carpeta As String In System.IO.Directory.GetDirectories(raiz)

            Dim nombre As String = _
                System.IO.Path.GetFileName(carpeta).Trim().ToUpperInvariant()

            Dim m As System.Text.RegularExpressions.Match = _
                System.Text.RegularExpressions.Regex.Match(nombre, patron)

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

        ' No existe carpeta de rango para este codigo: se crea.
        ' A03000 -> .A03000-A03999 ; 43500 -> .43000-43999
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

        Dim rutaNueva As String = System.IO.Path.Combine(raiz, nombreNuevo)

        If Not System.IO.Directory.Exists(rutaNueva) Then
            System.IO.Directory.CreateDirectory(rutaNueva)
        End If

        Return rutaNueva

    Catch
        Return ""
    End Try

End Function


Function ObtenerParteNumericaCodigoIntegrada(ByVal codigo As String) As Integer

    Try
        Dim limpio As String = codigo.Trim().ToUpperInvariant()

        If limpio.StartsWith("A") Then limpio = limpio.Substring(1)

        Dim numero As Integer = 0
        Integer.TryParse(limpio, numero)

        Return numero

    Catch
        Return 0
    End Try

End Function


Function ExtraerLimitesCarpetaIntegrada(ByVal nombre As String) As Integer()

    Try
        Dim coincidencias As System.Text.RegularExpressions.MatchCollection = _
            System.Text.RegularExpressions.Regex.Matches(nombre, "\d+")

        If coincidencias.Count < 2 Then Return Nothing

        Dim primero As Integer = 0
        Dim segundo As Integer = 0

        If Not Integer.TryParse(coincidencias(0).Value, primero) Then Return Nothing
        If Not Integer.TryParse(coincidencias(1).Value, segundo) Then Return Nothing

        Return New Integer() {primero, segundo}

    Catch
        Return Nothing
    End Try

End Function


Function CopiarPiezaConConfirmacionIntegrada( _
    ByVal invApp As Inventor.Application, _
    ByVal origen As String, _
    ByVal destino As String) As Boolean

    Try
        If String.Equals( _
            origen, _
            destino, _
            StringComparison.OrdinalIgnoreCase) Then

            Return True
        End If

        If System.IO.File.Exists(destino) Then

            Dim respuesta As System.Windows.Forms.DialogResult = _
                System.Windows.Forms.MessageBox.Show( _
                    "Ya existe la pieza de destino:" & vbCrLf & vbCrLf & _
                    destino & vbCrLf & vbCrLf & _
                    "¿Quieres reemplazarla?", _
                    "Confirmar reemplazo de pieza", _
                    System.Windows.Forms.MessageBoxButtons.YesNoCancel, _
                    System.Windows.Forms.MessageBoxIcon.Warning)

            If respuesta <> System.Windows.Forms.DialogResult.Yes Then
                Return False
            End If

            CerrarDocumentoPorRutaIntegrada(invApp, destino)
            System.IO.File.Delete(destino)
        End If

        System.IO.File.Copy(origen, destino, True)

        Return True

    Catch ex As Exception
        MessageBox.Show( _
            "No se ha podido copiar la pieza." & vbCrLf & vbCrLf & _
            ex.Message, _
            "Copia y plano de plegado")

        Return False
    End Try

End Function


Function ObtenerOAbrirPiezaIntegrada( _
    ByVal invApp As Inventor.Application, _
    ByVal ruta As String) As PartDocument

    Try
        For Each doc As Document In invApp.Documents
            Try
                If doc.DocumentType = DocumentTypeEnum.kPartDocumentObject AndAlso _
                   String.Equals(doc.FullFileName, ruta, StringComparison.OrdinalIgnoreCase) Then

                    Return TryCast(doc, PartDocument)
                End If
            Catch
            End Try
        Next

        Return TryCast(invApp.Documents.Open(ruta, True), PartDocument)

    Catch
        Return Nothing
    End Try

End Function


Sub CerrarDocumentoPorRutaIntegrada( _
    ByVal invApp As Inventor.Application, _
    ByVal ruta As String)

    Try
        For i As Integer = invApp.Documents.Count To 1 Step -1
            Dim doc As Document = invApp.Documents.Item(i)

            Try
                If String.Equals( _
                    doc.FullFileName, _
                    ruta, _
                    StringComparison.OrdinalIgnoreCase) Then

                    doc.Close(True)
                    Exit Sub
                End If
            Catch
            End Try
        Next
    Catch
    End Try

End Sub


'---------------------------------------------------------
' PREPARAR DESARROLLO EN LA PIEZA ORIGEN
'---------------------------------------------------------
Sub PrepararDesarrolloPlanoEnPiezaOrigen(ByVal partDoc As PartDocument)

    If partDoc Is Nothing Then
        Throw New Exception("No existe la pieza origen.")
    End If

    Dim smDef As SheetMetalComponentDefinition = _
        TryCast(partDoc.ComponentDefinition, SheetMetalComponentDefinition)

    If smDef Is Nothing Then
        Throw New Exception("La pieza origen no es una pieza de chapa válida.")
    End If

    If Not smDef.HasFlatPattern Then
        smDef.Unfold()

        Try
            smDef.FlatPattern.ExitEdit()
        Catch
        End Try
    End If

    If Not smDef.HasFlatPattern Then
        Throw New Exception("No se ha podido generar el desarrollo plano de la pieza origen.")
    End If

    partDoc.Update2(True)

End Sub


'---------------------------------------------------------
' FUNCION PRINCIPAL
'---------------------------------------------------------
' v8.0:
'  - carpetaSalidaForzada: si se indica, el IDW/PDF/DWF se guardan ahi
'    (modo ensamblaje) en lugar de la carpeta de rango.
'  - cerrarTrasGuardar: True cierra el plano tras exportar (modo lote).
'  - La pieza se guarda tras escribir MATRIZ y COD DE PLEGADO para que
'    las iProperties queden en el archivo.

Sub CrearPlanoPlegadoPiezaIndividual(ByVal invApp As Inventor.Application, _
                                     ByVal tg As TransientGeometry, _
                                     ByVal partDoc As PartDocument, _
                                     ByVal rutaPlantillaBase As String, _
                                     ByVal rutaBasePlanosIDW As String, _
                                     ByVal nombreCarpetaSalida As String, _
                                     ByVal nombreSimboloDatos As String, _
                                     ByVal nombreSimboloPosicion As String, _
                                     ByVal escalasDisponibles() As Double, _
                                     ByVal aumentarEscalaUnPaso As Boolean, _
                                     ByVal codigoPlegadoAsignado As String, _
                                     Optional ByVal carpetaSalidaForzada As String = "", _
                                     Optional ByVal cerrarTrasGuardar As Boolean = False, _
                                     Optional ByVal exportarPDFTambien As Boolean = True)

    Dim smDef As SheetMetalComponentDefinition = TryCast(partDoc.ComponentDefinition, SheetMetalComponentDefinition)

    If smDef Is Nothing Then
        Throw New Exception("No se puede acceder a la definicion de chapa de la pieza.")
    End If

    If Not smDef.HasFlatPattern Then
        Try
            smDef.Unfold()
            smDef.FlatPattern.ExitEdit()
            partDoc.Update2(True)
        Catch ex As Exception
            Throw New Exception("La pieza no se puede desplegar automaticamente. Detalle: " & ex.Message)
        End Try
    End If

    If Not smDef.HasFlatPattern Then
        Throw New Exception("La pieza sigue sin tener desarrollo plano despues de intentar desplegarla.")
    End If

    '---------------------------------------------------------
    ' LEER iPROPERTIES DE LA PIEZA
    '---------------------------------------------------------

    Dim codPleg As String = LeerPropiedad(partDoc, "Design Tracking Properties", "Part Number")
    Dim codAlmacen As String = LeerPropiedad(partDoc, "Design Tracking Properties", "Stock Number")
    Dim descripcion As String = LeerPropiedad(partDoc, "Design Tracking Properties", "Description")
    Dim proyecto As String = LeerPropiedad(partDoc, "Design Tracking Properties", "Project")
    Dim disenador As String = ObtenerUsuarioWindowsActual()
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

    Dim codPlegadoReal As String = Trim(codigoPlegadoAsignado)

    If codPlegadoReal = "" Then
        codPlegadoReal = Trim(LeerPropiedadUsuario(partDoc, "COD DE PLEGADO"))
    End If

    If codPlegadoReal = "" Then
        Throw New Exception( _
            "La pieza no tiene informado el campo 'COD DE PLEGADO'.")
    End If

    ' Forzar matriz correcta según el espesor real de chapa.
    Dim matrizCalculada As String = ObtenerMatrizPorEspesor(partDoc)

    If matrizCalculada <> "" Then
        matriz = matrizCalculada
        EscribirPropiedadUsuario(partDoc, "MATRIZ", matriz)
    End If

    ' v8.0: asegurar que el COD DE PLEGADO queda tambien en la pieza
    ' con la que se genera el plano (antes solo estaba en la origen).
    EscribirPropiedadUsuario(partDoc, "COD DE PLEGADO", codPlegadoReal)

    ' v8.0 CORRECCION IPROPERTIES: guardar la pieza para que MATRIZ y
    ' COD DE PLEGADO queden en el archivo IPT y no solo en memoria.
    ' Antes el IPT copiado quedaba en disco SIN estas propiedades.
    Try
        If partDoc.FullFileName <> "" Then
            partDoc.Save()
        End If
    Catch
        ' Si la pieza esta bloqueada o es de solo lectura no se bloquea
        ' la generacion del plano: las propiedades siguen en memoria.
    End Try

    If codPleg = "" And codAlmacen <> "" Then codPleg = codAlmacen
    If codPleg = "" Then codPleg = System.IO.Path.GetFileNameWithoutExtension(partDoc.FullFileName)
    If codCorte = "" Then codCorte = codAlmacen
    If codCorte = "" Then codCorte = codPleg
    If espesorTexto = "" Then espesorTexto = ObtenerEspesorChapa(partDoc)
    If descripcion = "" Then descripcion = System.IO.Path.GetFileNameWithoutExtension(partDoc.FullFileName)

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

    ' v8.0: en modo ensamblaje la carpeta de salida es la del propio IAM.
    Dim carpetaSalida As String

    If Trim(carpetaSalidaForzada) <> "" Then
        carpetaSalida = carpetaSalidaForzada
    Else
        carpetaSalida = ObtenerCarpetaPlanoIDWPorCodigo(rutaBasePlanosIDW, codPleg)
    End If

    If Not System.IO.Directory.Exists(carpetaSalida) Then
        System.IO.Directory.CreateDirectory(carpetaSalida)
    End If

    '---------------------------------------------------------
    ' CREAR PLANO
    '---------------------------------------------------------

    Dim drawingDoc As DrawingDocument = TryCast(invApp.Documents.Open(rutaPlantilla, True), DrawingDocument)

    If drawingDoc Is Nothing Then
        Throw New Exception("No se ha podido abrir el IDW base.")
    End If

    Dim sheet As Sheet = drawingDoc.ActiveSheet

    BorrarRotulosCodigoPlegadoExistentes(sheet)
    BorrarTodasLasVistasExistentes(sheet)

    AplicarA4VerticalObligatorio(drawingDoc, sheet)

    sheet = drawingDoc.ActiveSheet

    Dim mayorDimensionPiezaMM As Double = ObtenerMayorDimensionFlatPatternMM(smDef)
    Dim piezaLargaA3 As Boolean = False

    Dim ancho As Double = sheet.Width
    Dim alto As Double = sheet.Height
    Dim hojaVertical As Boolean = alto > ancho

    BorrarSimbolosExistentes(sheet, nombreSimboloDatos)
    BorrarSimbolosExistentes(sheet, nombreSimboloPosicion)

    Dim flatOptions As NameValueMap = invApp.TransientObjects.CreateNameValueMap()
    flatOptions.Add("SheetMetalFoldedModel", False)

    Dim foldedOptions As NameValueMap = invApp.TransientObjects.CreateNameValueMap()
    foldedOptions.Add("SheetMetalFoldedModel", True)

    '---------------------------------------------------------
    ' SELECCION REAL DE ORIENTACION, ESCALA Y DISTRIBUCION
    '---------------------------------------------------------

    Dim escalaProvisional As Double = _
        CalcularEscalaInicialSegunLongitudA4(mayorDimensionPiezaMM, escalasDisponibles)

    Dim escalaPrincipal As Double = escalaProvisional
    Dim escalaDesarrollo As Double = escalaPrincipal
    Dim escalaIso As Double = escalaPrincipal
    Dim escalaTexto As String = ""
    Dim centroTrabajo As String = "PLEGADO"
    Dim referenciaCliente As String = codPleg
    Dim pSimbolo As Point2d = Nothing

    Dim orientacionPrincipalPlano As ViewOrientationTypeEnum = _
        SeleccionarOrientacionPrincipalReal( _
            drawingDoc, sheet, partDoc, tg, foldedOptions)

    '---------------------------------------------------------
    ' 1) VISTA PRINCIPAL REAL
    '---------------------------------------------------------
    Dim vPrincipal As DrawingView = sheet.DrawingViews.AddBaseView( _
        partDoc, _
        tg.CreatePoint2d(ancho * 0.28, alto * 0.72), _
        escalaProvisional, _
        orientacionPrincipalPlano, _
        DrawingViewStyleEnum.kHiddenLineRemovedDrawingViewStyle, _
        "", _
        Nothing, _
        foldedOptions)

    vPrincipal.Name = "VISTA PRINCIPAL"
    QuitarEtiquetaVista(vPrincipal)
    ActivarAristasTangentes(vPrincipal)

    ForzarOrientacionHorizontalVistaBase(drawingDoc, vPrincipal, "VISTA PRINCIPAL")

    drawingDoc.Update2(True)

    '---------------------------------------------------------
    ' 2) VISTAS PROYECTADAS DESDE LA PRINCIPAL
    '---------------------------------------------------------
    Dim vLateral As DrawingView = sheet.DrawingViews.AddProjectedView( _
        vPrincipal, _
        tg.CreatePoint2d(vPrincipal.Position.X + Math.Max(5.0, vPrincipal.Width + 2.0), vPrincipal.Position.Y), _
        DrawingViewStyleEnum.kHiddenLineRemovedDrawingViewStyle)

    vLateral.Name = "VISTA LATERAL"
    QuitarEtiquetaVista(vLateral)
    ActivarAristasTangentes(vLateral)

    Dim vSuperior As DrawingView = sheet.DrawingViews.AddProjectedView( _
        vPrincipal, _
        tg.CreatePoint2d(vPrincipal.Position.X, vPrincipal.Position.Y - Math.Max(4.0, vPrincipal.Height + 2.0)), _
        DrawingViewStyleEnum.kHiddenLineRemovedDrawingViewStyle)

    vSuperior.Name = "VISTA SUPERIOR"
    QuitarEtiquetaVista(vSuperior)
    ActivarAristasTangentes(vSuperior)

    Dim vIso As DrawingView = sheet.DrawingViews.AddBaseView( _
        partDoc, _
        tg.CreatePoint2d( _
            vPrincipal.Position.X + Math.Max(5.0, vPrincipal.Width + 2.0), _
            vPrincipal.Position.Y - Math.Max(4.0, vPrincipal.Height + 2.0)), _
        escalaProvisional, _
        ViewOrientationTypeEnum.kIsoTopRightViewOrientation, _
        DrawingViewStyleEnum.kHiddenLineRemovedDrawingViewStyle, _
        "", _
        Nothing, _
        foldedOptions)

    vIso.Name = "ISOMETRICA"
    QuitarEtiquetaVista(vIso)
    ActivarAristasTangentes(vIso)

    '---------------------------------------------------------
    ' 3) DESARROLLO
    '---------------------------------------------------------
    Dim vDesarrollo As DrawingView = sheet.DrawingViews.AddBaseView( _
        partDoc, _
        tg.CreatePoint2d(ancho * 0.28, alto * 0.30), _
        escalaProvisional, _
        ViewOrientationTypeEnum.kDefaultViewOrientation, _
        DrawingViewStyleEnum.kHiddenLineRemovedDrawingViewStyle, _
        "", _
        Nothing, _
        flatOptions)

    vDesarrollo.Name = "DESARROLLO"
    QuitarEtiquetaVista(vDesarrollo)
    ActivarAristasTangentes(vDesarrollo)

    ForzarOrientacionHorizontalVistaBase(drawingDoc, vDesarrollo, "DESARROLLO")

    drawingDoc.Update2(True)
    If vPrincipal.Width < vPrincipal.Height Then
        Throw New Exception("VISTA PRINCIPAL no ha quedado horizontal y no se puede aplicar el patron unico.")
    End If
    If vDesarrollo.Width < vDesarrollo.Height Then
        Throw New Exception("DESARROLLO no ha quedado horizontal y no se puede aplicar el patron unico.")
    End If

    VincularEscalaVistaProyectada(vLateral)
    VincularEscalaVistaProyectada(vSuperior)
    OcultarEtiquetasTodasLasVistas(sheet)
    drawingDoc.Update2(True)

    ValidarVistasProyectadasDependientes( _
        vPrincipal, vLateral, vSuperior, vIso)

    '---------------------------------------------------------
    ' 4) MAYOR ESCALA NORMALIZADA REALMENTE VALIDA
    '---------------------------------------------------------
    SeleccionarEscalaComunYDistribuirA4Robusto( _
        drawingDoc, sheet, tg, _
        vDesarrollo, vPrincipal, vLateral, vSuperior, vIso, _
        escalasDisponibles, False, escalaPrincipal, pSimbolo)

    escalaDesarrollo = escalaPrincipal
    escalaIso = escalaPrincipal
    escalaTexto = FormatearEscala(escalaPrincipal)

    If escalaPrincipal < 0.05 Then
        Throw New Exception( _
            "La pieza no cabe correctamente en A4 a una escala minima de 1:20." & vbCrLf & _
            "El plano no se guarda para evitar un resultado inutil para taller.")
    End If

    '---------------------------------------------------------
    ' 5) ACOTACION CONTROLADA
    '---------------------------------------------------------
    Dim cotasCreadas As Integer = 0

    Dim posicionesCotas As New System.Collections.Generic.List(Of Point2d)

    cotasCreadas += AcotarVistaBasicaControlada(sheet, tg, vPrincipal, True, True, 0.70, posicionesCotas)
    cotasCreadas += AcotarVistaBasicaControlada(sheet, tg, vLateral, True, True, 0.70, posicionesCotas)
    cotasCreadas += AcotarVistaBasicaControlada(sheet, tg, vSuperior, False, True, 0.70, posicionesCotas)
    cotasCreadas += AcotarVistaBasicaControlada(sheet, tg, vDesarrollo, True, True, 0.70, posicionesCotas)

    If cotasCreadas < 4 Then
        Throw New Exception( _
            "La acotacion automatica no ha creado suficientes cotas." & vbCrLf & _
            "Cotas creadas: " & cotasCreadas.ToString() & vbCrLf & _
            "El plano no se guarda incompleto.")
    End If

    AcotarPlegados90ExteriorExteriorEnSeccion( _
        sheet, tg, vLateral, 1.40, 0.75, 3.0, 0.15, 0.30, 6)

    '---------------------------------------------------------
    ' 6) SIMBOLO COD PLEGADO PULVER (con el codigo asignado)
    '---------------------------------------------------------
    Dim articuloCajetin As String = NormalizarCodigoParaRango(codPleg)
    referenciaCliente = articuloCajetin

    Dim codigoIncluidoEnSimbolo As Boolean = _
        InsertarSimboloDatosPlegado( _
            drawingDoc, _
            sheet, _
            nombreSimboloDatos, _
            pSimbolo, _
            matriz, _
            codCorte, _
            espesorTexto, _
            codPlegadoReal)

    If Not codigoIncluidoEnSimbolo Then
        Dim pCodigoPlegado As Point2d = _
            tg.CreatePoint2d( _
                pSimbolo.X, _
                Math.Min(alto - 0.8, pSimbolo.Y + 2.1))

        InsertarRotuloCodigoPlegado( _
            sheet, _
            pCodigoPlegado, _
            codPlegadoReal)
    End If

    '---------------------------------------------------------
    ' 7) SIMBOLO OJO POSICION
    '---------------------------------------------------------
    Dim tienePosicion As Boolean = DetectarPiezaConPosicion(partDoc, smDef)

    If tienePosicion Then
        InsertarSimboloPosicion( _
            drawingDoc, sheet, tg, nombreSimboloPosicion, pSimbolo, _
            vDesarrollo, vPrincipal, vLateral, vSuperior, vIso)
        EscribirPropiedadUsuario(drawingDoc, "POSICION", "SI")
    Else
        EscribirPropiedadUsuario(drawingDoc, "POSICION", "NO")
    End If

    OcultarEtiquetasTodasLasVistas(sheet)
    AplicarYVerificarEscalaComunTodasLasVistas( _
        drawingDoc, escalaPrincipal, vDesarrollo, vPrincipal, vLateral, vSuperior, vIso)
    drawingDoc.Update2(True)

    '---------------------------------------------------------
    ' ESCRIBIR PROPIEDADES EN EL PLANO
    '---------------------------------------------------------

    EscribirPropiedadUsuario(drawingDoc, "COD_ALMACEN", codAlmacen)
    EscribirPropiedadUsuario(drawingDoc, "CORTE", codCorte)
    EscribirPropiedadUsuario(drawingDoc, "ESPESOR", espesorTexto)
    EscribirPropiedadUsuario(drawingDoc, "MATRIZ", matriz)
    EscribirPropiedadUsuario(drawingDoc, "COD DE PLEGADO", codPlegadoReal)
    EscribirPropiedadUsuario(drawingDoc, "MATERIAL", material)
    EscribirPropiedadUsuario(drawingDoc, "VERSION_REGLA_PLEGADO", "8.7-ENSAMBLAJE")
    EscribirPropiedadUsuario(drawingDoc, "PROYECTO", proyecto)
    EscribirPropiedadUsuario(drawingDoc, "DISENADOR", disenador)
    EscribirPropiedadUsuario(drawingDoc, "ESTADO_DISENO", estadoDiseno)
    EscribirPropiedadUsuario(drawingDoc, "REVISADO_POR", revisadoPor)
    EscribirPropiedadUsuario(drawingDoc, "APROBADO_FABRICACION_POR", aprobadoFabPor)
    EscribirPropiedadUsuario(drawingDoc, "ESCALA", escalaTexto)
    EscribirPropiedadUsuario(drawingDoc, "ARTICULO", articuloCajetin)
    EscribirPropiedadUsuario(drawingDoc, "ARTÍCULO", articuloCajetin)
    EscribirPropiedadUsuario(drawingDoc, "ARTICULO FINAL", articuloCajetin)
    EscribirPropiedadUsuario(drawingDoc, "ARTÍCULO FINAL", articuloCajetin)
    EscribirPropiedadUsuario(drawingDoc, "ARTICULO_FINAL", articuloCajetin)
    EscribirPropiedadUsuario(drawingDoc, "Nº DE PIEZA", articuloCajetin)
    EscribirPropiedadUsuario(drawingDoc, "NUMERO DE PIEZA", articuloCajetin)
    EscribirPropiedadUsuario(drawingDoc, "NÚMERO DE PIEZA", articuloCajetin)
    EscribirPropiedadUsuario(drawingDoc, "CENTRO", centroTrabajo)
    EscribirPropiedadUsuario(drawingDoc, "REFERENCIA CLIENTE", referenciaCliente)
    EscribirPropiedadUsuario(drawingDoc, "REFERENCIA_CLIENTE", referenciaCliente)
    EscribirPropiedadUsuario(drawingDoc, "CODIGO CLIENTE", referenciaCliente)
    EscribirPropiedadUsuario(drawingDoc, "CÓDIGO CLIENTE", referenciaCliente)
    EscribirPropiedadUsuario(drawingDoc, "COD_CLIENTE", articuloCajetin)
    EscribirPropiedadUsuario(drawingDoc, "REF CLIENTE", articuloCajetin)
    EscribirPropiedadUsuario(drawingDoc, "REF_CLIENTE", articuloCajetin)
    EscribirPropiedadUsuario(drawingDoc, "REFERENCIA CLIENTE FINAL", articuloCajetin)

    EscribirPropiedad(drawingDoc, "Design Tracking Properties", "Part Number", articuloCajetin)
    EscribirPropiedad(drawingDoc, "Design Tracking Properties", "Stock Number", articuloCajetin)

    RellenarPromptsCajetin(sheet, articuloCajetin, centroTrabajo, escalaTexto, articuloCajetin)

    EscribirPropiedad(drawingDoc, "Design Tracking Properties", "Part Number", articuloCajetin)
    EscribirPropiedad(drawingDoc, "Design Tracking Properties", "Stock Number", articuloCajetin)
    EscribirPropiedad(drawingDoc, "Design Tracking Properties", "Description", descripcion)
    EscribirPropiedad(drawingDoc, "Design Tracking Properties", "Project", proyecto)
    EscribirPropiedad(drawingDoc, "Design Tracking Properties", "Designer", disenador)
    EscribirPropiedad(drawingDoc, "Inventor Summary Information", "Author", disenador)
    EscribirPropiedad(drawingDoc, "Summary Information", "Author", disenador)
    EscribirPropiedad(drawingDoc, "Inventor Summary Information", "Revision Number", revision)
    EscribirPropiedad(drawingDoc, "Summary Information", "Revision Number", revision)

    drawingDoc.Update2(True)

    '---------------------------------------------------------
    ' GUARDAR Y EXPORTAR
    '---------------------------------------------------------

    Dim nombreBase As String = LimpiarNombreArchivo(codPleg)
    If nombreBase.Length > 130 Then nombreBase = nombreBase.Substring(0, 130)

    Dim extensionPlano As String = System.IO.Path.GetExtension(rutaPlantilla).ToLower()
    If extensionPlano <> ".idw" And extensionPlano <> ".dwg" Then extensionPlano = ".idw"

    Dim rutaPlano As String = System.IO.Path.Combine(carpetaSalida, nombreBase & extensionPlano)
    Dim rutaPDF As String = System.IO.Path.Combine(carpetaSalida, nombreBase & ".pdf")
    Dim rutaDWF As String = System.IO.Path.Combine(carpetaSalida, nombreBase & ".dwf")

    drawingDoc.SaveAs(rutaPlano, False)

    ' v8.1: en modo ensamblaje NO se genera PDF, solo DWF.
    If exportarPDFTambien Then
        ExportarPDF(invApp, drawingDoc, rutaPDF)
    End If

    ExportarDWF(invApp, drawingDoc, rutaDWF)

    ' v8.0: en modo lote se cierra el plano; en modo individual queda abierto.
    If cerrarTrasGuardar Then
        Try
            drawingDoc.Close(True)
        Catch
        End Try
    Else
        drawingDoc.Activate()
    End If

End Sub


'---------------------------------------------------------
' LIMPIEZA DE ROTULOS COD PLEG
'---------------------------------------------------------

Sub BorrarRotulosCodigoPlegadoExistentes(ByVal sheet As Sheet)

    Try
        If sheet Is Nothing Then Exit Sub

        For i As Integer = sheet.DrawingNotes.GeneralNotes.Count To 1 Step -1

            Dim nota As GeneralNote = sheet.DrawingNotes.GeneralNotes.Item(i)
            Dim contenido As String = ""

            Try
                contenido = nota.Text
            Catch
                Try
                    contenido = nota.FormattedText
                Catch
                    contenido = ""
                End Try
            End Try

            Dim normalizado As String = NormalizarTextoCodigoPlegado(contenido)

            If normalizado.Contains("CODPLEG") OrElse _
               normalizado.Contains("CODIGOPLEGADO") Then

                Try
                    nota.Delete()
                Catch
                End Try
            End If

        Next

        For i As Integer = sheet.SketchedSymbols.Count To 1 Step -1

            Dim simbolo As SketchedSymbol = sheet.SketchedSymbols.Item(i)
            Dim nombre As String = ""

            Try
                nombre = simbolo.Definition.Name
            Catch
                nombre = ""
            End Try

            Dim normalizado As String = NormalizarTextoCodigoPlegado(nombre)

            If normalizado.Contains("CODPLEG") OrElse _
               normalizado.Contains("CODIGOPLEGADO") Then

                Try
                    simbolo.Delete()
                Catch
                End Try
            End If

        Next

    Catch
    End Try

End Sub


Function NormalizarTextoCodigoPlegado(ByVal texto As String) As String

    If texto Is Nothing Then Return ""

    Dim salida As String = texto.ToUpper()

    salida = salida.Replace("Á", "A")
    salida = salida.Replace("É", "E")
    salida = salida.Replace("Í", "I")
    salida = salida.Replace("Ó", "O")
    salida = salida.Replace("Ú", "U")
    salida = salida.Replace("Ü", "U")
    salida = salida.Replace("Ñ", "N")

    salida = salida.Replace(" ", "")
    salida = salida.Replace("_", "")
    salida = salida.Replace("-", "")
    salida = salida.Replace(".", "")
    salida = salida.Replace(":", "")
    salida = salida.Replace("<", "")
    salida = salida.Replace(">", "")
    salida = salida.Replace("/", "")

    Return salida

End Function


'---------------------------------------------------------
' LIMPIEZA DE VISTAS EXISTENTES
'---------------------------------------------------------

Sub BorrarTodasLasVistasExistentes(ByVal sheet As Sheet)

    Try
        If sheet Is Nothing Then Exit Sub

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
                If yTexto > sheet.Height - 0.60 Then
                    yTexto = vista.Position.Y - (vista.Height / 2.0) - offsetActual
                End If

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
                If xTexto > sheet.Width - 0.60 Then
                    xTexto = vista.Position.X - (vista.Width / 2.0) - offsetActual
                End If

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


'---------------------------------------------------------
' SIMBOLO DE BOCETO COD PLEGADO PULVER
'---------------------------------------------------------

Function InsertarSimboloDatosPlegado(ByVal drawingDoc As DrawingDocument, _
                                     ByVal sheet As Sheet, _
                                     ByVal nombreSimbolo As String, _
                                     ByVal punto As Point2d, _
                                     ByVal matriz As String, _
                                     ByVal codCorte As String, _
                                     ByVal espesorTexto As String, _
                                     ByVal codPlegado As String) As Boolean
    Try
        Dim def As SketchedSymbolDefinition = _
            drawingDoc.SketchedSymbolDefinitions.Item(nombreSimbolo)

        Try
            Dim promptsConCodigo(3) As String
            promptsConCodigo(0) = matriz
            promptsConCodigo(1) = codCorte
            promptsConCodigo(2) = espesorTexto
            promptsConCodigo(3) = codPlegado

            sheet.SketchedSymbols.Add(def, punto, 0, 1, promptsConCodigo)
            Return True
        Catch
        End Try

        Try
            Dim promptsActuales(2) As String
            promptsActuales(0) = matriz
            promptsActuales(1) = codCorte
            promptsActuales(2) = espesorTexto

            sheet.SketchedSymbols.Add(def, punto, 0, 1, promptsActuales)
            Return False
        Catch
        End Try

        Try
            sheet.SketchedSymbols.Add(def, punto, 0, 1)
            Return False
        Catch ex As Exception
            Throw New Exception( _
                "No se ha podido insertar el símbolo de boceto '" & _
                nombreSimbolo & "'. Detalle: " & ex.Message)
        End Try

    Catch ex As Exception
        Throw New Exception( _
            "No se ha encontrado o no se ha podido insertar el símbolo de boceto '" & _
            nombreSimbolo & "'. Detalle: " & ex.Message)
    End Try
End Function


Sub InsertarRotuloCodigoPlegado(ByVal sheet As Sheet, _
                                ByVal punto As Point2d, _
                                ByVal codPlegado As String)

    If sheet Is Nothing Then
        Throw New Exception("No existe la hoja para insertar el código de plegado.")
    End If

    If punto Is Nothing Then
        Throw New Exception("No existe el punto de inserción del código de plegado.")
    End If

    codPlegado = Trim(codPlegado)

    If codPlegado = "" Then
        Throw New Exception("El código de plegado está vacío.")
    End If

    Try
        Dim nota As GeneralNote = _
            sheet.DrawingNotes.GeneralNotes.AddFitted( _
                punto, _
                "COD. PLEGADO: " & codPlegado)

        Try
            nota.ShowTextBorder = True
        Catch
        End Try

    Catch ex As Exception
        Throw New Exception( _
            "No se ha podido insertar el rótulo COD. PLEGADO en el plano. " & _
            "Detalle: " & ex.Message)
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
    End Try
End Sub

'---------------------------------------------------------
' VISUALIZACION DE VISTAS
'---------------------------------------------------------

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
' FORMATO A4 VERTICAL FIJO
'---------------------------------------------------------

Sub AplicarA4VerticalObligatorio(ByVal drawingDoc As DrawingDocument, _
                                  ByVal sheet As Sheet)

    If drawingDoc Is Nothing Then
        Throw New Exception("No existe el documento de plano.")
    End If

    If sheet Is Nothing Then
        Throw New Exception("No existe la hoja activa.")
    End If

    Dim errorCambio As String = ""

    Try
        sheet.ChangeSize(DrawingSheetSizeEnum.kA4DrawingSheetSize, True)
    Catch ex As Exception
        errorCambio = ex.Message

        Try
            sheet.Size = DrawingSheetSizeEnum.kA4DrawingSheetSize
        Catch ex2 As Exception
            errorCambio &= vbCrLf & ex2.Message
        End Try
    End Try

    Try
        sheet.Orientation = PageOrientationTypeEnum.kPortraitPageOrientation
    Catch ex As Exception
        errorCambio &= vbCrLf & ex.Message
    End Try

    Try
        drawingDoc.Update2(True)
    Catch
    End Try

    Dim hojaComprobacion As sheet = drawingDoc.ActiveSheet

    If hojaComprobacion Is Nothing Then
        Throw New Exception("No se puede recuperar la hoja después de aplicar A4.")
    End If

    If hojaComprobacion.Width >= hojaComprobacion.Height Then
        Throw New Exception( _
            "Inventor no ha aplicado el formato A4 vertical." & vbCrLf & vbCrLf & _
            "La regla se ha detenido para evitar generar un plano A3 incompleto." & _
            If(errorCambio <> "", vbCrLf & vbCrLf & "Detalle: " & errorCambio, ""))
    End If

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


Function ObtenerNombreCarpetaRangoIDWExistenteOPreferido( _
    ByVal rutaBasePlanosIDW As String, _
    ByVal codigo As String) As String

    If codigo Is Nothing Then Return ""

    codigo = NormalizarCodigoParaRango(codigo)

    If codigo = "" Then Return ""

    Dim esA As Boolean = codigo.ToUpper().StartsWith("A")
    Dim digitos As String = ""

    If esA Then
        digitos = ExtraerDigitosIniciales(codigo.Substring(1))
    Else
        digitos = ExtraerDigitosIniciales(codigo)
    End If

    If digitos = "" Then Return ""

    Dim numero As Integer = 0

    If Not Integer.TryParse(digitos, numero) Then Return ""

    If esA Then
        Return CrearNombreRango(numero, 1000, True)
    End If

    Return CrearNombreRango(numero, 500, False)

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
' USUARIO ACTUAL DE WINDOWS
'---------------------------------------------------------

Function ObtenerUsuarioWindowsActual() As String

    Dim usuario As String = ""

    Try
        usuario = Trim(System.Environment.UserName)
    Catch
        usuario = ""
    End Try

    If usuario = "" Then usuario = "USUARIO"

    Return usuario

End Function


'---------------------------------------------------------
' MATRIZ SEGUN ESPESOR - CRITERIO METALPLAK
'---------------------------------------------------------

Function ObtenerMatrizPorEspesor(ByVal partDoc As PartDocument) As String

    Try
        Dim smDef As SheetMetalComponentDefinition = _
            TryCast(partDoc.ComponentDefinition, SheetMetalComponentDefinition)

        If smDef Is Nothing Then Return ""

        Dim espesorMm As Double = smDef.Thickness.Value * 10.0
        espesorMm = Math.Round(espesorMm, 2)

        If Math.Abs(espesorMm - 1.5) < 0.05 Then Return "V10"
        If Math.Abs(espesorMm - 2.0) < 0.05 Then Return "V16"
        If Math.Abs(espesorMm - 3.0) < 0.05 Then Return "V24"
        If Math.Abs(espesorMm - 4.0) < 0.05 Then Return "V32"
        If Math.Abs(espesorMm - 6.0) < 0.05 Then Return "V32"
        If Math.Abs(espesorMm - 8.0) < 0.05 Then Return "V60"
        If Math.Abs(espesorMm - 10.0) < 0.05 Then Return "V60"

        Return LeerPropiedadUsuario(partDoc, "MATRIZ")

    Catch
        Return ""
    End Try

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

' v8.0 CORRECCION: si la propiedad no existe en el set, se crea.
' Antes fallaba en silencio y la propiedad nunca llegaba al documento.
Sub EscribirPropiedad(ByVal doc As Document, ByVal setName As String, ByVal propName As String, ByVal value As String)
    Try
        Dim ps As PropertySet = doc.PropertySets.Item(setName)
        Try
            Dim p As Inventor.Property = ps.Item(propName)
            p.Value = value
        Catch
            Try
                ps.Add(value, propName)
            Catch
                ' Algunos sets estandar no admiten propiedades nuevas.
            End Try
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


Sub EliminarPropiedadUsuario(ByVal doc As Document, ByVal propName As String)

    If doc Is Nothing Then Return
    If Trim(propName) = "" Then Return

    Try
        Dim ps As PropertySet = _
            doc.PropertySets.Item("Inventor User Defined Properties")

        For i As Integer = ps.Count To 1 Step -1
            Dim p As Inventor.Property = ps.Item(i)

            If String.Equals( _
                Trim(p.Name), _
                Trim(propName), _
                StringComparison.OrdinalIgnoreCase) Then

                p.Delete()
                Exit Sub
            End If
        Next
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
' ANTICOLISION, MANO/POSICION Y OJO POSICION
'---------------------------------------------------------

Function ExisteColisionTextoCota( _
    ByVal posiciones As System.Collections.Generic.List(Of Point2d), _
    ByVal punto As Point2d, _
    ByVal distanciaMinimaCm As Double) As Boolean

    Try
        If posiciones Is Nothing Then Return False
        If punto Is Nothing Then Return False

        For Each p As Point2d In posiciones
            Dim dx As Double = p.X - punto.X
            Dim dy As Double = p.Y - punto.Y
            If Math.Sqrt((dx * dx) + (dy * dy)) < distanciaMinimaCm Then Return True
        Next

        Return False
    Catch
        Return False
    End Try

End Function


Function DetectarPiezaConPosicion( _
    ByVal partDoc As PartDocument, _
    ByVal smDef As SheetMetalComponentDefinition) As Boolean

    Try
        Dim marca As String = _
            (LeerPropiedadUsuario(partDoc, "POSICION") & "|" & _
             LeerPropiedadUsuario(partDoc, "POSICIÓN") & "|" & _
             LeerPropiedadUsuario(partDoc, "MANO")).ToUpper()

        If marca.Contains("NO") Then Return False
        If marca.Contains("SI") OrElse marca.Contains("SÍ") OrElse _
           marca.Contains("X") OrElse marca.Contains("1") Then Return True
    Catch
    End Try

    Return DetectarPosicionPorGeometria(smDef)

End Function


Function DetectarPosicionPorGeometria( _
    ByVal smDef As SheetMetalComponentDefinition) As Boolean

    Try
        If smDef Is Nothing Then Return False
        If Not smDef.HasFlatPattern Then Return False

        Dim fp As FlatPattern = smDef.FlatPattern

        Dim rb As Box = fp.RangeBox
        Dim ext(2) As Double
        ext(0) = Math.Abs(rb.MaxPoint.X - rb.MinPoint.X)
        ext(1) = Math.Abs(rb.MaxPoint.Y - rb.MinPoint.Y)
        ext(2) = Math.Abs(rb.MaxPoint.Z - rb.MinPoint.Z)

        Dim ejeEspesor As Integer = 0
        If ext(1) < ext(ejeEspesor) Then ejeEspesor = 1
        If ext(2) < ext(ejeEspesor) Then ejeEspesor = 2

        Dim ejeU As Integer = -1
        Dim ejeV As Integer = -1
        For k As Integer = 0 To 2
            If k <> ejeEspesor Then
                If ejeU < 0 Then ejeU = k Else ejeV = k
            End If
        Next

        Dim us As New System.Collections.Generic.List(Of Double)
        Dim vs As New System.Collections.Generic.List(Of Double)
        Dim tipos As New System.Collections.Generic.List(Of Integer)

        Dim caraSup As Face = fp.TopFace
        If caraSup Is Nothing Then Return False

        For Each ed As Edge In caraSup.Edges
            Try
                AgregarPuntoPosicion(ed.StartVertex.Point, ejeU, ejeV, 0, us, vs, tipos)
            Catch
            End Try
            Try
                AgregarPuntoPosicion(ed.StopVertex.Point, ejeU, ejeV, 0, us, vs, tipos)
            Catch
            End Try
            Try
                Dim pMin As Double
                Dim pMax As Double
                ed.Evaluator.GetParamExtents(pMin, pMax)
                Dim pMed(0) As Double
                pMed(0) = (pMin + pMax) / 2.0
                Dim xyz(2) As Double
                ed.Evaluator.GetPointAtParam(pMed, xyz)
                AgregarCoordenadaPosicion(xyz(0), xyz(1), xyz(2), ejeU, ejeV, 0, us, vs, tipos)
            Catch
            End Try
        Next

        Dim hayPlegadosArriba As Boolean = False
        Dim hayPlegadosAbajo As Boolean = False

        For Each br As FlatBendResult In fp.FlatBendResults
            Try
                Dim edB As Edge = br.Edge
                Dim tipo As Integer = 2
                If br.IsDirectionUp Then tipo = 1

                If tipo = 1 Then hayPlegadosArriba = True
                If tipo = 2 Then hayPlegadosAbajo = True

                Dim pMinB As Double
                Dim pMaxB As Double
                edB.Evaluator.GetParamExtents(pMinB, pMaxB)
                Dim pMedB(0) As Double
                pMedB(0) = (pMinB + pMaxB) / 2.0
                Dim xyzB(2) As Double
                edB.Evaluator.GetPointAtParam(pMedB, xyzB)
                AgregarCoordenadaPosicion(xyzB(0), xyzB(1), xyzB(2), ejeU, ejeV, tipo, us, vs, tipos)

                Try
                    AgregarPuntoPosicion(edB.StartVertex.Point, ejeU, ejeV, tipo, us, vs, tipos)
                    AgregarPuntoPosicion(edB.StopVertex.Point, ejeU, ejeV, tipo, us, vs, tipos)
                Catch
                End Try
            Catch
            End Try
        Next

        If us.Count < 4 Then Return False

        Dim uMin As Double = us(0)
        Dim uMax As Double = us(0)
        Dim vMin As Double = vs(0)
        Dim vMax As Double = vs(0)

        For i As Integer = 0 To us.Count - 1
            If us(i) < uMin Then uMin = us(i)
            If us(i) > uMax Then uMax = us(i)
            If vs(i) < vMin Then vMin = vs(i)
            If vs(i) > vMax Then vMax = vs(i)
        Next

        Dim cu As Double = (uMin + uMax) / 2.0
        Dim cv As Double = (vMin + vMax) / 2.0

        Dim tol As Double = Math.Max(0.05, Math.Max(uMax - uMin, vMax - vMin) * 0.002)

        If EsConjuntoSimetrico(us, vs, tipos, cu, cv, True, False, False, tol) Then Return False
        If EsConjuntoSimetrico(us, vs, tipos, cu, cv, False, True, False, tol) Then Return False
        If EsConjuntoSimetrico(us, vs, tipos, cu, cv, True, False, True, tol) Then Return False
        If EsConjuntoSimetrico(us, vs, tipos, cu, cv, False, True, True, tol) Then Return False

        Return True

    Catch
        Return False
    End Try

End Function


Sub AgregarPuntoPosicion(ByVal p As Inventor.Point, _
                         ByVal ejeU As Integer, _
                         ByVal ejeV As Integer, _
                         ByVal tipo As Integer, _
                         ByVal us As System.Collections.Generic.List(Of Double), _
                         ByVal vs As System.Collections.Generic.List(Of Double), _
                         ByVal tipos As System.Collections.Generic.List(Of Integer))
    If p Is Nothing Then Exit Sub
    AgregarCoordenadaPosicion(p.X, p.Y, p.Z, ejeU, ejeV, tipo, us, vs, tipos)
End Sub


Sub AgregarCoordenadaPosicion(ByVal x As Double, _
                              ByVal y As Double, _
                              ByVal z As Double, _
                              ByVal ejeU As Integer, _
                              ByVal ejeV As Integer, _
                              ByVal tipo As Integer, _
                              ByVal us As System.Collections.Generic.List(Of Double), _
                              ByVal vs As System.Collections.Generic.List(Of Double), _
                              ByVal tipos As System.Collections.Generic.List(Of Integer))
    Dim c(2) As Double
    c(0) = x
    c(1) = y
    c(2) = z
    us.Add(c(ejeU))
    vs.Add(c(ejeV))
    tipos.Add(tipo)
End Sub


Function EsConjuntoSimetrico( _
    ByVal us As System.Collections.Generic.List(Of Double), _
    ByVal vs As System.Collections.Generic.List(Of Double), _
    ByVal tipos As System.Collections.Generic.List(Of Integer), _
    ByVal cu As Double, _
    ByVal cv As Double, _
    ByVal espejarU As Boolean, _
    ByVal espejarV As Boolean, _
    ByVal invertirSentidos As Boolean, _
    ByVal tol As Double) As Boolean

    Try
        For i As Integer = 0 To us.Count - 1

            Dim uEsp As Double = us(i)
            Dim vEsp As Double = vs(i)
            If espejarU Then uEsp = (2.0 * cu) - us(i)
            If espejarV Then vEsp = (2.0 * cv) - vs(i)

            Dim tipoBuscado As Integer = tipos(i)
            If invertirSentidos Then
                If tipos(i) = 1 Then tipoBuscado = 2
                If tipos(i) = 2 Then tipoBuscado = 1
            End If

            Dim encontrado As Boolean = False
            For j As Integer = 0 To us.Count - 1
                If tipos(j) = tipoBuscado Then
                    If Math.Abs(us(j) - uEsp) <= tol AndAlso Math.Abs(vs(j) - vEsp) <= tol Then
                        encontrado = True
                        Exit For
                    End If
                End If
            Next

            If Not encontrado Then Return False
        Next

        Return True
    Catch
        Return False
    End Try

End Function


Sub InsertarSimboloPosicion(ByVal drawingDoc As DrawingDocument, _
                            ByVal sheet As Sheet, _
                            ByVal tg As TransientGeometry, _
                            ByVal nombreSimboloPosicion As String, _
                            ByVal puntoSimboloDatos As Point2d, _
                            ByVal vDesarrollo As DrawingView, _
                            ByVal vPrincipal As DrawingView, _
                            ByVal vLateral As DrawingView, _
                            ByVal vSuperior As DrawingView, _
                            ByVal vIso As DrawingView)

    Dim anchoOjo As Double = 2.8
    Dim altoOjo As Double = 3.2

    Dim datosIzq As Double = puntoSimboloDatos.X - 2.6
    Dim datosDer As Double = puntoSimboloDatos.X + 2.6
    Dim datosInf As Double = puntoSimboloDatos.Y - 1.8
    Dim datosSup As Double = puntoSimboloDatos.Y + 1.8

    Dim def As SketchedSymbolDefinition = Nothing
    Try
        def = drawingDoc.SketchedSymbolDefinitions.Item(nombreSimboloPosicion)
    Catch
        Dim objetivo As String = NormalizarTextoCodigoPlegado(nombreSimboloPosicion)
        For Each d As SketchedSymbolDefinition In drawingDoc.SketchedSymbolDefinitions
            Try
                If NormalizarTextoCodigoPlegado(d.Name) = objetivo Then
                    def = d
                    Exit For
                End If
            Catch
            End Try
        Next
    End Try

    If def Is Nothing Then
        Throw New Exception( _
            "La pieza tiene posicion de plegado pero no se ha encontrado el " & _
            "simbolo de boceto '" & nombreSimboloPosicion & "' en la plantilla." & vbCrLf & _
            "Anade el simbolo a la plantilla base y vuelve a ejecutar la regla.")
    End If

    Dim candidatos As New System.Collections.Generic.List(Of Point2d)
    candidatos.Add(tg.CreatePoint2d(puntoSimboloDatos.X, datosSup + 0.40 + (altoOjo / 2.0)))
    candidatos.Add(tg.CreatePoint2d(datosIzq - 0.40 - (anchoOjo / 2.0), puntoSimboloDatos.Y))
    candidatos.Add(tg.CreatePoint2d(sheet.Width - 0.45 - (anchoOjo / 2.0), sheet.Height - 0.45 - (altoOjo / 2.0)))

    Dim yGrid As Double = ObtenerYSuperiorCajetin(sheet) + 0.30 + (altoOjo / 2.0)
    While yGrid < sheet.Height - 0.35 - (altoOjo / 2.0)
        Dim xGrid As Double = sheet.Width - 0.40 - (anchoOjo / 2.0)
        While xGrid > 0.40 + (anchoOjo / 2.0)
            candidatos.Add(tg.CreatePoint2d(xGrid, yGrid))
            xGrid -= 1.0
        End While
        yGrid += 1.0
    End While

    Dim vistas() As DrawingView = {vDesarrollo, vPrincipal, vLateral, vSuperior, vIso}
    Dim puntoElegido As Point2d = Nothing

    For Each cand As Point2d In candidatos
        Dim cIzq As Double = cand.X - (anchoOjo / 2.0)
        Dim cDer As Double = cand.X + (anchoOjo / 2.0)
        Dim cInf As Double = cand.Y - (altoOjo / 2.0)
        Dim cSup As Double = cand.Y + (altoOjo / 2.0)

        If cIzq < 0.35 OrElse cDer > sheet.Width - 0.35 Then Continue For
        If cInf < ObtenerYSuperiorCajetin(sheet) + 0.25 OrElse cSup > sheet.Height - 0.30 Then Continue For

        If CajasSolapan(cIzq, cDer, cInf, cSup, datosIzq, datosDer, datosInf, datosSup, 0.10) Then Continue For

        Dim invadeVista As Boolean = False
        For Each v As DrawingView In vistas
            Try
                Dim vIzq As Double = v.Position.X - (v.Width / 2.0) - 0.76
                Dim vDer As Double = v.Position.X + (v.Width / 2.0) + 0.15
                Dim vInf As Double = v.Position.Y - (v.Height / 2.0) - 0.76
                Dim vSup As Double = v.Position.Y + (v.Height / 2.0) + 0.15
                If CajasSolapan(cIzq, cDer, cInf, cSup, vIzq, vDer, vInf, vSup, 0.10) Then
                    invadeVista = True
                    Exit For
                End If
            Catch
            End Try
        Next

        If Not invadeVista Then
            puntoElegido = cand
            Exit For
        End If
    Next

    If puntoElegido Is Nothing Then puntoElegido = candidatos(0)

    Try
        sheet.SketchedSymbols.Add(def, puntoElegido, 0, 1)
    Catch ex As Exception
        Throw New Exception( _
            "No se ha podido insertar el simbolo '" & nombreSimboloPosicion & "'." & vbCrLf & _
            "Detalle: " & ex.Message)
    End Try

End Sub


Function CajasSolapan( _
    ByVal aIzq As Double, _
    ByVal aDer As Double, _
    ByVal aInf As Double, _
    ByVal aSup As Double, _
    ByVal bIzq As Double, _
    ByVal bDer As Double, _
    ByVal bInf As Double, _
    ByVal bSup As Double, _
    ByVal separacion As Double) As Boolean

    If aDer + separacion <= bIzq Then Return False
    If bDer + separacion <= aIzq Then Return False
    If aSup + separacion <= bInf Then Return False
    If bSup + separacion <= aInf Then Return False

    Return True

End Function


'---------------------------------------------------------
' MOTOR DE PLANO v11.1
' Orientacion real, doble patron A4, escala comun verificada,
' acotacion controlada con anticolision.
'---------------------------------------------------------

Function CalcularEscalaInicialSegunLongitudA4( _
    ByVal longitudMayorMM As Double, _
    ByVal escalasDisponibles() As Double) As Double

    Dim anchoObjetivoCm As Double = 13.5

    If longitudMayorMM <= 0 Then Return 0.1

    For Each s As Double In escalasDisponibles
        If s <= 0 Then Continue For
        If s < 0.05 Then Continue For

        Dim longitudEnPlanoCm As Double = (longitudMayorMM / 10.0) * s

        If longitudEnPlanoCm <= anchoObjetivoCm + 0.000001 Then
            Return s
        End If
    Next

    Return 0.05

End Function


Function SeleccionarOrientacionPrincipalReal( _
    ByVal drawingDoc As DrawingDocument, _
    ByVal sheet As Sheet, _
    ByVal partDoc As PartDocument, _
    ByVal tg As TransientGeometry, _
    ByVal foldedOptions As NameValueMap) As ViewOrientationTypeEnum

    Dim candidatos() As ViewOrientationTypeEnum = { _
        ViewOrientationTypeEnum.kFrontViewOrientation, _
        ViewOrientationTypeEnum.kTopViewOrientation, _
        ViewOrientationTypeEnum.kRightViewOrientation}

    Dim mejorOrientacion As ViewOrientationTypeEnum = candidatos(0)
    Dim mejorDimensionMayor As Double = -1
    Dim mejorDimensionMenor As Double = -1
    Dim mejorArea As Double = -1
    Dim escalaPrueba As Double = 0.05

    For i As Integer = 0 To candidatos.Length - 1
        Dim vistaTemp As DrawingView = Nothing

        Try
            vistaTemp = sheet.DrawingViews.AddBaseView( _
                partDoc, _
                tg.CreatePoint2d(sheet.Width / 2.0, sheet.Height / 2.0), _
                escalaPrueba, _
                candidatos(i), _
                DrawingViewStyleEnum.kHiddenLineRemovedDrawingViewStyle, _
                "", _
                Nothing, _
                foldedOptions)

            vistaTemp.Name = "__TMP_ORIENT_" & i.ToString()
            QuitarEtiquetaVista(vistaTemp)
            drawingDoc.Update2(True)

            Dim anchoReal As Double = vistaTemp.Width / escalaPrueba
            Dim altoReal As Double = vistaTemp.Height / escalaPrueba
            Dim dimensionMayor As Double = Math.Max(anchoReal, altoReal)
            Dim dimensionMenor As Double = Math.Min(anchoReal, altoReal)
            Dim area As Double = anchoReal * altoReal

            Dim toleranciaMayor As Double = Math.Max(0.01, mejorDimensionMayor * 0.02)
            Dim elegir As Boolean = False

            If mejorDimensionMayor < 0 OrElse dimensionMayor > mejorDimensionMayor + toleranciaMayor Then
                elegir = True
            ElseIf Math.Abs(dimensionMayor - mejorDimensionMayor) <= toleranciaMayor Then
                If dimensionMenor > mejorDimensionMenor + 0.01 Then
                    elegir = True
                ElseIf Math.Abs(dimensionMenor - mejorDimensionMenor) <= 0.01 AndAlso area > mejorArea Then
                    elegir = True
                End If
            End If

            If elegir Then
                mejorOrientacion = candidatos(i)
                mejorDimensionMayor = dimensionMayor
                mejorDimensionMenor = dimensionMenor
                mejorArea = area
            End If

        Catch ex As Exception
        Finally
            If Not vistaTemp Is Nothing Then
                Try
                    vistaTemp.Delete()
                Catch
                End Try
            End If
        End Try
    Next

    drawingDoc.Update2(True)

    If mejorDimensionMayor <= 0 Then
        Throw New Exception("No se ha podido determinar una vista principal valida del modelo plegado.")
    End If

    Return mejorOrientacion

End Function


Sub SeleccionarEscalaComunYDistribuirA4Robusto( _
    ByVal drawingDoc As DrawingDocument, _
    ByVal sheet As Sheet, _
    ByVal tg As TransientGeometry, _
    ByVal vDesarrollo As DrawingView, _
    ByVal vPrincipal As DrawingView, _
    ByVal vLateral As DrawingView, _
    ByVal vSuperior As DrawingView, _
    ByVal vIso As DrawingView, _
    ByVal escalasDisponibles() As Double, _
    ByVal piezaLargaVertical As Boolean, _
    ByRef escalaComun As Double, _
    ByRef puntoSimbolo As Point2d)

    Dim encontrada As Boolean = False
    Dim patronElegido As Integer = 1
    escalaComun = 0

    Dim escalaInicialPermitida As Double = 0
    Try
        escalaInicialPermitida = vPrincipal.Scale
    Catch
        escalaInicialPermitida = 0.1
    End Try

    For Each s As Double In escalasDisponibles
        If s <= 0 Then Continue For
        If s < 0.05 Then Continue For
        If s > escalaInicialPermitida + 0.000001 Then Continue For

        If Not AplicarEscalaComunComprobada( _
            drawingDoc, s, vDesarrollo, vPrincipal, vLateral, vSuperior, vIso) Then
            Continue For
        End If

        Dim patronProbado As Integer = 0
        If Not IntentarDistribuirA4CortoDosPatronesRobusto( _
            drawingDoc, sheet, tg, vDesarrollo, vPrincipal, vLateral, vSuperior, vIso, patronProbado, puntoSimbolo) Then
            Continue For
        End If

        Try
            ValidarVistasProyectadasDependientes(vPrincipal, vLateral, vSuperior, vIso)
        Catch
            Continue For
        End Try

        escalaComun = s
        patronElegido = patronProbado
        encontrada = True
        Exit For
    Next

    If Not encontrada Then
        Throw New Exception( _
            "No se ha encontrado una escala valida para el patron unico A4." & vbCrLf & _
            "Se han probado todas las escalas normalizadas con los patrones de filas y escalonado.")
    End If

    If Not AplicarEscalaComunComprobada( _
        drawingDoc, escalaComun, vDesarrollo, vPrincipal, vLateral, vSuperior, vIso) Then
        Throw New Exception("Inventor no mantiene la escala comun definitiva.")
    End If

    If Not DistribuirA4CortoSegunPatronRobusto( _
        drawingDoc, sheet, tg, vDesarrollo, vPrincipal, vLateral, vSuperior, vIso, patronElegido, puntoSimbolo) Then
        Throw New Exception("No se ha podido reproducir el patron final (patron " & patronElegido.ToString() & ").")
    End If

    drawingDoc.Update2(True)
    OcultarEtiquetasTodasLasVistas(sheet)
    ValidarVistasProyectadasDependientes(vPrincipal, vLateral, vSuperior, vIso)

    If Not ValidarLayoutRobustoA4( _
        sheet, vDesarrollo, vPrincipal, vLateral, vSuperior, vIso, puntoSimbolo) Then
        Throw New Exception("El patron unico final ha dejado de ser valido despues de actualizar Inventor.")
    End If

End Sub


Function AplicarEscalaComunComprobada( _
    ByVal drawingDoc As DrawingDocument, _
    ByVal escala As Double, _
    ByVal vDesarrollo As DrawingView, _
    ByVal vPrincipal As DrawingView, _
    ByVal vLateral As DrawingView, _
    ByVal vSuperior As DrawingView, _
    ByVal vIso As DrawingView) As Boolean

    Try
        If escala <= 0 Then Return False

        vPrincipal.Scale = escala
        vDesarrollo.Scale = escala
        vIso.Scale = escala
        drawingDoc.Update2(True)

        AsegurarEscalaNumericaVistaProyectada(drawingDoc, vLateral, escala, "VISTA LATERAL")
        AsegurarEscalaNumericaVistaProyectada(drawingDoc, vSuperior, escala, "VISTA SUPERIOR")

        drawingDoc.Update2(True)
        ValidarVistasProyectadasDependientes(vPrincipal, vLateral, vSuperior, vIso)

        Dim tol As Double = 0.000001
        If Math.Abs(vPrincipal.Scale - escala) > tol Then Return False
        If Math.Abs(vDesarrollo.Scale - escala) > tol Then Return False
        If Math.Abs(vLateral.Scale - escala) > tol Then Return False
        If Math.Abs(vSuperior.Scale - escala) > tol Then Return False
        If Math.Abs(vIso.Scale - escala) > tol Then Return False

        Return True
    Catch
        Return False
    End Try

End Function


Function IntentarDistribuirA4CortoDosPatronesRobusto( _
    ByVal drawingDoc As DrawingDocument, _
    ByVal sheet As Sheet, _
    ByVal tg As TransientGeometry, _
    ByVal vDesarrollo As DrawingView, _
    ByVal vPrincipal As DrawingView, _
    ByVal vLateral As DrawingView, _
    ByVal vSuperior As DrawingView, _
    ByVal vIso As DrawingView, _
    ByRef patronElegido As Integer, _
    ByRef puntoSimbolo As Point2d) As Boolean

    patronElegido = 0

    If DistribuirA4CortoFilasRobusto( _
        drawingDoc, sheet, tg, vDesarrollo, vPrincipal, vLateral, vSuperior, vIso, puntoSimbolo) Then

        drawingDoc.Update2(True)
        OcultarEtiquetasTodasLasVistas(sheet)

        If ValidarLayoutRobustoA4( _
            sheet, vDesarrollo, vPrincipal, vLateral, vSuperior, vIso, puntoSimbolo) Then
            patronElegido = 1
            Return True
        End If
    End If

    If DistribuirA4CortoEscalonadoRobusto( _
        drawingDoc, sheet, tg, vDesarrollo, vPrincipal, vLateral, vSuperior, vIso, puntoSimbolo) Then

        drawingDoc.Update2(True)
        OcultarEtiquetasTodasLasVistas(sheet)

        If ValidarLayoutRobustoA4( _
            sheet, vDesarrollo, vPrincipal, vLateral, vSuperior, vIso, puntoSimbolo) Then
            patronElegido = 2
            Return True
        End If
    End If

    Return False

End Function


Function DistribuirA4CortoSegunPatronRobusto( _
    ByVal drawingDoc As DrawingDocument, _
    ByVal sheet As Sheet, _
    ByVal tg As TransientGeometry, _
    ByVal vDesarrollo As DrawingView, _
    ByVal vPrincipal As DrawingView, _
    ByVal vLateral As DrawingView, _
    ByVal vSuperior As DrawingView, _
    ByVal vIso As DrawingView, _
    ByVal patron As Integer, _
    ByRef puntoSimbolo As Point2d) As Boolean

    If patron = 2 Then
        Return DistribuirA4CortoEscalonadoRobusto( _
            drawingDoc, sheet, tg, vDesarrollo, vPrincipal, vLateral, vSuperior, vIso, puntoSimbolo)
    End If

    Return DistribuirA4CortoFilasRobusto( _
        drawingDoc, sheet, tg, vDesarrollo, vPrincipal, vLateral, vSuperior, vIso, puntoSimbolo)

End Function


Function DistribuirA4CortoEscalonadoRobusto( _
    ByVal drawingDoc As DrawingDocument, _
    ByVal sheet As Sheet, _
    ByVal tg As TransientGeometry, _
    ByVal vDesarrollo As DrawingView, _
    ByVal vPrincipal As DrawingView, _
    ByVal vLateral As DrawingView, _
    ByVal vSuperior As DrawingView, _
    ByVal vIso As DrawingView, _
    ByRef puntoSimbolo As Point2d) As Boolean

    Try
        Dim margenIzq As Double = 0.48
        Dim margenDer As Double = 0.48
        Dim margenSup As Double = 0.38
        Dim margenCajetin As Double = 0.32
        Dim reservaCotaIzq As Double = 0.72
        Dim reservaCotaInf As Double = 0.72
        Dim separacionH As Double = 0.30
        Dim separacionVMin As Double = 0.22
        Dim anchoSimbolo As Double = 5.2
        Dim altoSimbolo As Double = 3.6

        Dim yMin As Double = ObtenerYSuperiorCajetin(sheet) + margenCajetin
        Dim yMax As Double = sheet.Height - margenSup
        Dim altoUtil As Double = yMax - yMin
        Dim anchoUtil As Double = sheet.Width - margenIzq - margenDer

        Dim altoFila1 As Double = Math.Max(vPrincipal.Height + reservaCotaInf, vLateral.Height + reservaCotaInf)
        Dim altoFila2 As Double = vSuperior.Height + reservaCotaInf
        Dim altoFila3 As Double = vIso.Height + 0.18
        Dim altoFila4 As Double = Math.Max(vDesarrollo.Height + reservaCotaInf, altoSimbolo)

        Dim anchoFila1 As Double = reservaCotaIzq + vPrincipal.Width + separacionH + vLateral.Width + 0.12
        Dim anchoFila2 As Double = reservaCotaIzq + vSuperior.Width + 0.12
        Dim anchoFila3 As Double = vIso.Width + 0.18
        Dim anchoFila4 As Double = reservaCotaIzq + vDesarrollo.Width + separacionH + anchoSimbolo

        Dim anchoNecesario As Double = Math.Max(Math.Max(anchoFila1, anchoFila2), Math.Max(anchoFila3, anchoFila4))
        If anchoNecesario > anchoUtil Then Return False

        Dim altoBase As Double = altoFila1 + altoFila2 + altoFila3 + altoFila4 + (3.0 * separacionVMin)
        If altoBase > altoUtil Then Return False

        Dim extra As Double = altoUtil - altoBase
        Dim margenVerticalExtra As Double = Math.Min(1.20, extra * 0.25)
        Dim separacionV As Double = separacionVMin + ((extra - (2.0 * margenVerticalExtra)) / 3.0)
        If separacionV < separacionVMin Then separacionV = separacionVMin
        If separacionV > 1.60 Then separacionV = 1.60

        Dim yCursor As Double = yMax - margenVerticalExtra

        ' FILA 1: PRINCIPAL + LATERAL
        Dim yFila1 As Double = yCursor - (altoFila1 / 2.0)
        Dim xPrincipal As Double = margenIzq + reservaCotaIzq + (vPrincipal.Width / 2.0)
        Dim xLateral As Double = sheet.Width - margenDer - 0.12 - (vLateral.Width / 2.0)
        vPrincipal.Position = tg.CreatePoint2d(xPrincipal, yFila1)
        vLateral.Position = tg.CreatePoint2d(xLateral, yFila1)

        yCursor -= altoFila1 + separacionV

        ' FILA 2: SUPERIOR SOLA
        Dim yFila2 As Double = yCursor - (altoFila2 / 2.0)
        Dim xSuperior As Double = vPrincipal.Position.X
        vSuperior.Position = tg.CreatePoint2d(xSuperior, yFila2)

        yCursor -= altoFila2 + separacionV

        ' FILA 3: ISOMÉTRICA SOLA A LA DERECHA
        Dim yFila3 As Double = yCursor - (altoFila3 / 2.0)
        Dim xIso As Double = sheet.Width - margenDer - 0.12 - (vIso.Width / 2.0)
        vIso.Position = tg.CreatePoint2d(xIso, yFila3)

        yCursor -= altoFila3 + separacionV

        ' FILA 4: DESARROLLO + DATOS DE PLEGADO
        Dim yFila4 As Double = yCursor - (altoFila4 / 2.0)
        Dim xDesarrollo As Double = margenIzq + reservaCotaIzq + (vDesarrollo.Width / 2.0)
        vDesarrollo.Position = tg.CreatePoint2d(xDesarrollo, yFila4)
        puntoSimbolo = tg.CreatePoint2d( _
            sheet.Width - margenDer - (anchoSimbolo / 2.0), _
            yFila4)

        drawingDoc.Update2(True)
        Return True
    Catch
        Return False
    End Try

End Function


Function DistribuirA4CortoFilasRobusto( _
    ByVal drawingDoc As DrawingDocument, _
    ByVal sheet As Sheet, _
    ByVal tg As TransientGeometry, _
    ByVal vDesarrollo As DrawingView, _
    ByVal vPrincipal As DrawingView, _
    ByVal vLateral As DrawingView, _
    ByVal vSuperior As DrawingView, _
    ByVal vIso As DrawingView, _
    ByRef puntoSimbolo As Point2d) As Boolean

    Try
        If sheet Is Nothing OrElse vDesarrollo Is Nothing OrElse _
           vPrincipal Is Nothing OrElse vLateral Is Nothing OrElse _
           vSuperior Is Nothing OrElse vIso Is Nothing Then Return False

        Dim margenIzq As Double = 0.35
        Dim margenDer As Double = 0.35
        Dim margenSup As Double = 0.70
        Dim margenCajetin As Double = 0.30

        Dim reservaCotaIzq As Double = 0.76
        Dim reservaCotaInf As Double = 0.76
        Dim margenGeometria As Double = 0.10

        Dim separacionH As Double = 0.30
        Dim separacionV As Double = 0.28

        Dim anchoSimbolo As Double = 5.2
        Dim altoSimbolo As Double = 3.6

        Dim xMin As Double = margenIzq
        Dim xMax As Double = sheet.Width - margenDer
        Dim yMin As Double = ObtenerYSuperiorCajetin(sheet) + margenCajetin
        Dim yMax As Double = sheet.Height - margenSup

        If xMax <= xMin OrElse yMax <= yMin Then Return False

        Dim xPrincipal As Double = _
            xMin + reservaCotaIzq + (vPrincipal.Width / 2.0)

        Dim xLateral As Double = _
            xMax - margenGeometria - (vLateral.Width / 2.0)

        Dim principalDer As Double = _
            xPrincipal + (vPrincipal.Width / 2.0) + margenGeometria

        Dim lateralIzq As Double = _
            xLateral - (vLateral.Width / 2.0) - reservaCotaIzq

        If principalDer + separacionH > lateralIzq Then Return False

        Dim semialtoFilaSuperior As Double = Math.Max( _
            vPrincipal.Height / 2.0, _
            vLateral.Height / 2.0)

        Dim yPrincipal As Double = _
            yMax - margenGeometria - semialtoFilaSuperior

        vPrincipal.Position = tg.CreatePoint2d(xPrincipal, yPrincipal)

        vLateral.Position = tg.CreatePoint2d(xLateral, yPrincipal)

        drawingDoc.Update2(True)

        Dim filaSuperiorInf As Double = Math.Min( _
            vPrincipal.Position.Y - (vPrincipal.Height / 2.0) - reservaCotaInf, _
            vLateral.Position.Y - (vLateral.Height / 2.0) - reservaCotaInf)

        Dim semialtoFilaInferior As Double = Math.Max( _
            (vDesarrollo.Height / 2.0) + reservaCotaInf, _
            altoSimbolo / 2.0)

        Dim yFilaInferior As Double = _
            yMin + semialtoFilaInferior + margenGeometria

        Dim xDesarrollo As Double = _
            xMin + reservaCotaIzq + (vDesarrollo.Width / 2.0)

        Dim xSimbolo As Double = _
            xMax - (anchoSimbolo / 2.0)

        Dim desarrolloDer As Double = _
            xDesarrollo + (vDesarrollo.Width / 2.0) + margenGeometria

        Dim simboloIzq As Double = xSimbolo - (anchoSimbolo / 2.0)

        If desarrolloDer + separacionH > simboloIzq Then Return False

        vDesarrollo.Position = tg.CreatePoint2d(xDesarrollo, yFilaInferior)
        puntoSimbolo = tg.CreatePoint2d(xSimbolo, yFilaInferior)

        drawingDoc.Update2(True)

        Dim filaInferiorSup As Double = Math.Max( _
            vDesarrollo.Position.Y + (vDesarrollo.Height / 2.0) + margenGeometria, _
            puntoSimbolo.Y + (altoSimbolo / 2.0))

        Dim bandaSup As Double = filaSuperiorInf - separacionV
        Dim bandaInf As Double = filaInferiorSup + separacionV
        Dim altoBanda As Double = bandaSup - bandaInf

        Dim altoSuperiorTotal As Double = vSuperior.Height + reservaCotaInf + margenGeometria
        If altoSuperiorTotal > altoBanda Then Return False

        Dim ySuperior As Double = _
            ((bandaSup + bandaInf) / 2.0) - (reservaCotaInf / 2.0) + (margenGeometria / 2.0)

        vSuperior.Position = tg.CreatePoint2d(xPrincipal, ySuperior)
        drawingDoc.Update2(True)

        Dim superiorInf As Double = _
            vSuperior.Position.Y - (vSuperior.Height / 2.0) - reservaCotaInf

        If superiorInf < filaInferiorSup Then Return False
        If vSuperior.Position.Y + (vSuperior.Height / 2.0) + margenGeometria > bandaSup Then Return False

        Dim bandaIsoSup As Double = _
            vLateral.Position.Y - (vLateral.Height / 2.0) - reservaCotaInf - separacionV
        Dim bandaIsoInf As Double = _
            puntoSimbolo.Y + (altoSimbolo / 2.0) + separacionV

        If (vIso.Height + (2.0 * margenGeometria)) > (bandaIsoSup - bandaIsoInf) Then Return False

        Dim yIso As Double = (bandaIsoSup + bandaIsoInf) / 2.0

        Dim isoSup As Double = _
            yIso + (vIso.Height / 2.0) + margenGeometria

        If isoSup > bandaIsoSup Then Return False

        Dim xIso As Double = _
            xMax - margenGeometria - (vIso.Width / 2.0)

        vIso.Position = tg.CreatePoint2d(xIso, yIso)
        drawingDoc.Update2(True)

        Dim supIzq As Double = vSuperior.Position.X - (vSuperior.Width / 2.0) - reservaCotaIzq
        Dim supDer As Double = vSuperior.Position.X + (vSuperior.Width / 2.0) + margenGeometria
        Dim supInf As Double = vSuperior.Position.Y - (vSuperior.Height / 2.0) - reservaCotaInf
        Dim supSup As Double = vSuperior.Position.Y + (vSuperior.Height / 2.0) + margenGeometria

        Dim isoIzq As Double = vIso.Position.X - (vIso.Width / 2.0) - margenGeometria
        Dim isoDer As Double = vIso.Position.X + (vIso.Width / 2.0) + margenGeometria
        Dim isoInf As Double = vIso.Position.Y - (vIso.Height / 2.0) - margenGeometria
        isoSup = vIso.Position.Y + (vIso.Height / 2.0) + margenGeometria

        If CajasSolapan( _
            supIzq, supDer, supInf, supSup, _
            isoIzq, isoDer, isoInf, isoSup, 0.06) Then Return False

        Return True

    Catch
        Return False
    End Try

End Function


Function ValidarLayoutRobustoA4( _
    ByVal sheet As Sheet, _
    ByVal vDesarrollo As DrawingView, _
    ByVal vPrincipal As DrawingView, _
    ByVal vLateral As DrawingView, _
    ByVal vSuperior As DrawingView, _
    ByVal vIso As DrawingView, _
    ByVal puntoSimbolo As Point2d) As Boolean

    Try
        Dim xMin As Double = 0.32
        Dim xMax As Double = sheet.Width - 0.32
        Dim yMin As Double = ObtenerYSuperiorCajetin(sheet) + 0.25
        Dim yMax As Double = sheet.Height - 0.25

        If Not VistaDentroLimites(vPrincipal, xMin, xMax, yMin, yMax, 0.76, 0.10, 0.10, 0.76) Then Return False
        If Not VistaDentroLimites(vLateral, xMin, xMax, yMin, yMax, 0.76, 0.10, 0.10, 0.76) Then Return False
        If Not VistaDentroLimites(vSuperior, xMin, xMax, yMin, yMax, 0.76, 0.10, 0.10, 0.76) Then Return False
        If Not VistaDentroLimites(vDesarrollo, xMin, xMax, yMin, yMax, 0.76, 0.10, 0.10, 0.76) Then Return False
        If Not VistaDentroLimites(vIso, xMin, xMax, yMin, yMax, 0.10, 0.10, 0.10, 0.10) Then Return False

        Dim sIzq As Double = puntoSimbolo.X - 2.6
        Dim sDer As Double = puntoSimbolo.X + 2.6
        Dim sInf As Double = puntoSimbolo.Y - 1.8
        Dim sSup As Double = puntoSimbolo.Y + 1.8
        If sIzq < xMin OrElse sDer > xMax OrElse sInf < yMin OrElse sSup > yMax Then Return False

        Dim vistas() As DrawingView = {vPrincipal, vLateral, vSuperior, vIso, vDesarrollo}
        Dim izqs() As Double = {0.76, 0.76, 0.76, 0.10, 0.76}
        Dim ders() As Double = {0.10, 0.10, 0.10, 0.10, 0.10}
        Dim sups() As Double = {0.10, 0.10, 0.10, 0.10, 0.10}
        Dim infs() As Double = {0.76, 0.76, 0.76, 0.10, 0.76}

        For i As Integer = 0 To vistas.Length - 2
            For j As Integer = i + 1 To vistas.Length - 1
                If VistasSolapan( _
                    vistas(i), izqs(i), ders(i), sups(i), infs(i), _
                    vistas(j), izqs(j), ders(j), sups(j), infs(j), 0.06) Then
                    Return False
                End If
            Next
        Next

        For i As Integer = 0 To vistas.Length - 1
            Dim vIzq As Double = vistas(i).Position.X - (vistas(i).Width / 2.0) - izqs(i)
            Dim vDer As Double = vistas(i).Position.X + (vistas(i).Width / 2.0) + ders(i)
            Dim vInf As Double = vistas(i).Position.Y - (vistas(i).Height / 2.0) - infs(i)
            Dim vSup As Double = vistas(i).Position.Y + (vistas(i).Height / 2.0) + sups(i)
            If CajasSolapan(vIzq, vDer, vInf, vSup, sIzq, sDer, sInf, sSup, 0.06) Then Return False
        Next

        Return True
    Catch
        Return False
    End Try

End Function


Sub ValidarVistasProyectadasDependientes( _
    ByVal vPrincipal As DrawingView, _
    ByVal vLateral As DrawingView, _
    ByVal vSuperior As DrawingView, _
    ByVal vIso As DrawingView)

    If vPrincipal Is Nothing Then Throw New Exception("No existe VISTA PRINCIPAL.")
    If vIso Is Nothing Then Throw New Exception("No existe ISOMETRICA.")

    Dim nombres() As String = {"VISTA LATERAL", "VISTA SUPERIOR"}
    Dim vistas() As DrawingView = {vLateral, vSuperior}

    For i As Integer = 0 To vistas.Length - 1
        If vistas(i) Is Nothing Then Throw New Exception("No existe " & nombres(i) & ".")

        Dim padre As DrawingView = Nothing
        Try
            padre = vistas(i).ParentView
        Catch ex As Exception
            Throw New Exception("No se ha podido leer ParentView de " & nombres(i) & ". Detalle: " & ex.Message)
        End Try

        If padre Is Nothing Then
            Throw New Exception(nombres(i) & " es una vista independiente. Debe ser proyectada desde VISTA PRINCIPAL.")
        End If

        If UCase(Trim(padre.Name)) <> UCase(Trim(vPrincipal.Name)) Then
            Throw New Exception(nombres(i) & " no depende de VISTA PRINCIPAL.")
        End If
    Next

    Try
        If Not vLateral.Aligned Then Throw New Exception("VISTA LATERAL ha perdido la alineacion con VISTA PRINCIPAL.")
    Catch ex As Exception
        If ex.Message.Contains("ha perdido") Then Throw
    End Try

    Try
        If Not vSuperior.Aligned Then Throw New Exception("VISTA SUPERIOR ha perdido la alineacion con VISTA PRINCIPAL.")
    Catch ex As Exception
        If ex.Message.Contains("ha perdido") Then Throw
    End Try

End Sub


Function AcotarVistaBasicaControlada( _
    ByVal sheet As Sheet, _
    ByVal tg As TransientGeometry, _
    ByVal vista As DrawingView, _
    ByVal acotarHorizontal As Boolean, _
    ByVal acotarVertical As Boolean, _
    ByVal offsetCm As Double, _
    ByVal posicionesTexto As System.Collections.Generic.List(Of Point2d)) As Integer

    Dim creadas As Integer = 0
    If vista Is Nothing Then Return 0

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

            Dim offsetH As Double = offsetCm
            Dim ptTextoH As Point2d = tg.CreatePoint2d(vista.Position.X, vista.Position.Y - (vista.Height / 2.0) - offsetH)
            Dim intentosH As Integer = 0
            While ExisteColisionTextoCota(posicionesTexto, ptTextoH, 1.1) AndAlso intentosH < 5
                offsetH += 0.70
                ptTextoH = tg.CreatePoint2d(vista.Position.X, vista.Position.Y - (vista.Height / 2.0) - offsetH)
                intentosH += 1
            End While

            sheet.DrawingDimensions.GeneralDimensions.AddLinear(ptTextoH, intentH1, intentH2, DimensionTypeEnum.kHorizontalDimensionType)
            If posicionesTexto IsNot Nothing Then posicionesTexto.Add(ptTextoH)
            creadas += 1
        Catch
        End Try
    End If

    If acotarVertical AndAlso curvaMinY IsNot Nothing AndAlso curvaMaxY IsNot Nothing Then
        Try
            Dim intentV1 As GeometryIntent = sheet.CreateGeometryIntent(curvaMinY, intentoMinY)
            Dim intentV2 As GeometryIntent = sheet.CreateGeometryIntent(curvaMaxY, intentoMaxY)

            Dim offsetV As Double = offsetCm
            Dim ptTextoV As Point2d = tg.CreatePoint2d(vista.Position.X - (vista.Width / 2.0) - offsetV, vista.Position.Y)
            Dim intentosV As Integer = 0
            While ExisteColisionTextoCota(posicionesTexto, ptTextoV, 1.1) AndAlso intentosV < 5
                offsetV += 0.70
                ptTextoV = tg.CreatePoint2d(vista.Position.X - (vista.Width / 2.0) - offsetV, vista.Position.Y)
                intentosV += 1
            End While

            sheet.DrawingDimensions.GeneralDimensions.AddLinear(ptTextoV, intentV1, intentV2, DimensionTypeEnum.kVerticalDimensionType)
            If posicionesTexto IsNot Nothing Then posicionesTexto.Add(ptTextoV)
            creadas += 1
        Catch
        End Try
    End If

    Return creadas

End Function


Sub OcultarEtiquetasTodasLasVistas(ByVal sheet As Sheet)
    If sheet Is Nothing Then Exit Sub

    For Each vista As DrawingView In sheet.DrawingViews
        Try
            vista.ShowLabel = False
        Catch
        End Try
    Next
End Sub


Function VistaDentroLimites( _
    ByVal vista As DrawingView, _
    ByVal xMinPermitido As Double, _
    ByVal xMaxPermitido As Double, _
    ByVal yMinPermitido As Double, _
    ByVal yMaxPermitido As Double, _
    ByVal margenIzq As Double, _
    ByVal margenDer As Double, _
    ByVal margenSup As Double, _
    ByVal margenInf As Double) As Boolean

    If vista Is Nothing Then Return False

    Dim xIzq As Double = vista.Position.X - (vista.Width / 2.0) - margenIzq
    Dim xDer As Double = vista.Position.X + (vista.Width / 2.0) + margenDer
    Dim yInf As Double = vista.Position.Y - (vista.Height / 2.0) - margenInf
    Dim ySup As Double = vista.Position.Y + (vista.Height / 2.0) + margenSup

    Return xIzq >= xMinPermitido AndAlso _
           xDer <= xMaxPermitido AndAlso _
           yInf >= yMinPermitido AndAlso _
           ySup <= yMaxPermitido

End Function


Function VistasSolapan( _
    ByVal vista1 As DrawingView, _
    ByVal izq1 As Double, _
    ByVal der1 As Double, _
    ByVal sup1 As Double, _
    ByVal inf1 As Double, _
    ByVal vista2 As DrawingView, _
    ByVal izq2 As Double, _
    ByVal der2 As Double, _
    ByVal sup2 As Double, _
    ByVal inf2 As Double, _
    ByVal separacion As Double) As Boolean

    If vista1 Is Nothing OrElse vista2 Is Nothing Then Return False

    Dim aIzq As Double = vista1.Position.X - (vista1.Width / 2.0) - izq1
    Dim aDer As Double = vista1.Position.X + (vista1.Width / 2.0) + der1
    Dim aInf As Double = vista1.Position.Y - (vista1.Height / 2.0) - inf1
    Dim aSup As Double = vista1.Position.Y + (vista1.Height / 2.0) + sup1

    Dim bIzq As Double = vista2.Position.X - (vista2.Width / 2.0) - izq2
    Dim bDer As Double = vista2.Position.X + (vista2.Width / 2.0) + der2
    Dim bInf As Double = vista2.Position.Y - (vista2.Height / 2.0) - inf2
    Dim bSup As Double = vista2.Position.Y + (vista2.Height / 2.0) + sup2

    Return CajasSolapan(aIzq, aDer, aInf, aSup, bIzq, bDer, bInf, bSup, separacion)

End Function


Sub VincularEscalaVistaProyectada(ByVal vista As DrawingView)
    If vista Is Nothing Then Exit Sub

    Try
        vista.ScaleFromBase = True
    Catch
    End Try
End Sub


Sub AsegurarEscalaNumericaVistaProyectada( _
    ByVal drawingDoc As DrawingDocument, _
    ByVal vista As DrawingView, _
    ByVal escalaComun As Double, _
    ByVal nombreVista As String)

    If vista Is Nothing Then Throw New Exception("No existe la vista " & nombreVista & ".")
    If escalaComun <= 0 Then Throw New Exception("Escala comun no valida para " & nombreVista & ".")

    Dim tolerancia As Double = 0.000001

    VincularEscalaVistaProyectada(vista)
    drawingDoc.Update2(True)

    Try
        If Math.Abs(vista.Scale - escalaComun) <= tolerancia Then Exit Sub
    Catch
    End Try

    Try
        vista.ScaleFromBase = False
    Catch
    End Try

    Try
        vista.Scale = escalaComun
    Catch ex As Exception
        Throw New Exception( _
            "No se ha podido igualar la escala de " & nombreVista & "." & vbCrLf & _
            "La vista sigue siendo proyectada, pero Inventor rechazo la escala numerica." & vbCrLf & _
            "Detalle: " & ex.Message)
    End Try

    drawingDoc.Update2(True)

    Try
        If Math.Abs(vista.Scale - escalaComun) > tolerancia Then
            Throw New Exception( _
                nombreVista & " mantiene " & FormatearEscala(vista.Scale) & _
                " en lugar de " & FormatearEscala(escalaComun) & ".")
        End If
    Catch ex As Exception
        Throw New Exception("No se ha podido verificar la escala de " & nombreVista & ". Detalle: " & ex.Message)
    End Try
End Sub


Sub AplicarYVerificarEscalaComunTodasLasVistas( _
    ByVal drawingDoc As DrawingDocument, _
    ByVal escalaComun As Double, _
    ByVal vDesarrollo As DrawingView, _
    ByVal vPrincipal As DrawingView, _
    ByVal vLateral As DrawingView, _
    ByVal vSuperior As DrawingView, _
    ByVal vIso As DrawingView)

    If escalaComun <= 0 Then
        Throw New Exception("La escala comun calculada no es valida.")
    End If

    If vPrincipal Is Nothing OrElse vDesarrollo Is Nothing OrElse _
       vLateral Is Nothing OrElse vSuperior Is Nothing OrElse vIso Is Nothing Then
        Throw New Exception("Falta alguna vista y no se puede verificar la escala comun.")
    End If

    Try
        vPrincipal.Scale = escalaComun
        vDesarrollo.Scale = escalaComun
        vIso.Scale = escalaComun
    Catch ex As Exception
        Throw New Exception("No se ha podido aplicar la escala a las vistas base. Detalle: " & ex.Message)
    End Try

    drawingDoc.Update2(True)

    AsegurarEscalaNumericaVistaProyectada(drawingDoc, vLateral, escalaComun, "VISTA LATERAL")
    AsegurarEscalaNumericaVistaProyectada(drawingDoc, vSuperior, escalaComun, "VISTA SUPERIOR")

    drawingDoc.Update2(True)
    ValidarVistasProyectadasDependientes(vPrincipal, vLateral, vSuperior, vIso)

    Dim toleranciaEscala As Double = 0.000001
    Dim nombres() As String = {"VISTA PRINCIPAL", "VISTA LATERAL", "VISTA SUPERIOR", "ISOMETRICA", "DESARROLLO"}
    Dim vistas() As DrawingView = {vPrincipal, vLateral, vSuperior, vIso, vDesarrollo}

    For i As Integer = 0 To vistas.Length - 1
        Dim escalaReal As Double = vistas(i).Scale
        If Math.Abs(escalaReal - escalaComun) > toleranciaEscala Then
            Throw New Exception( _
                "La vista '" & nombres(i) & "' no mantiene la escala comun." & vbCrLf & _
                "Escala requerida: " & FormatearEscala(escalaComun) & vbCrLf & _
                "Escala detectada: " & FormatearEscala(escalaReal))
        End If
    Next

End Sub


Sub ForzarOrientacionHorizontalVistaBase( _
    ByVal drawingDoc As DrawingDocument, _
    ByVal vista As DrawingView, _
    ByVal nombreVista As String)

    If vista Is Nothing Then
        Throw New Exception("No existe la vista " & nombreVista & ".")
    End If

    drawingDoc.Update2(True)

    If vista.Width >= vista.Height Then Exit Sub

    Dim anguloOriginal As Double = 0
    Try
        anguloOriginal = vista.Rotation
    Catch
        anguloOriginal = 0
    End Try

    Dim candidatos() As Double = { _
        anguloOriginal + (Math.PI / 2.0), _
        anguloOriginal - (Math.PI / 2.0), _
        0.0, _
        Math.PI / 2.0, _
        -Math.PI / 2.0}

    For Each angulo As Double In candidatos
        Try
            vista.Rotation = angulo
            drawingDoc.Update2(True)
            If vista.Width >= vista.Height Then Exit Sub
        Catch
        End Try
    Next

    Throw New Exception( _
        "Inventor no ha podido orientar horizontalmente la vista " & nombreVista & "." & vbCrLf & _
        "Ancho final: " & Math.Round(vista.Width, 2).ToString() & " cm" & vbCrLf & _
        "Alto final: " & Math.Round(vista.Height, 2).ToString() & " cm")

End Sub


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
    End Try
End Sub
