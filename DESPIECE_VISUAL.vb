Sub Main()

    Dim invApp As Inventor.Application = ThisApplication
    Dim activeDoc As Document = invApp.ActiveDocument

    If activeDoc Is Nothing Then
        MessageBox.Show("No hay ningún documento abierto.", "Despiece visual")
        Return
    End If

    If activeDoc.DocumentType <> DocumentTypeEnum.kAssemblyDocumentObject Then
        MessageBox.Show("Esta regla debe ejecutarse desde un ensamblaje .IAM." & vbCrLf & vbCrLf & _
                        "Documento actual: " & activeDoc.DisplayName, _
                        "Despiece visual")
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

    Dim tg As TransientGeometry = invApp.TransientGeometry

    '---------------------------------------------------------
    ' RUTA DE PLANTILLA
    ' CAMBIA ESTA RUTA POR TU PLANTILLA REAL
    ' Tiene que terminar en .idw
    '---------------------------------------------------------

    Dim templatePath As String = "Q:\BIBLIOTECA INVENTOR 2019\PLANTILLAS 2019\DESPIECE.idw"

    If Not System.IO.File.Exists(templatePath) Then
        MessageBox.Show("No se encuentra la plantilla:" & vbCrLf & templatePath & vbCrLf & vbCrLf & _
                        "La ruta debe terminar en un archivo .idw.", _
                        "Despiece")
        Return
    End If

    '---------------------------------------------------------
    ' CARPETA DE SALIDA
    '---------------------------------------------------------

    Dim asmFolder As String = System.IO.Path.GetDirectoryName(asmDoc.FullFileName)
    Dim asmName As String = System.IO.Path.GetFileNameWithoutExtension(asmDoc.FullFileName)

    Dim outputFolder As String = System.IO.Path.Combine(asmFolder, "02_DESPIECE_VISUAL")

    If Not System.IO.Directory.Exists(outputFolder) Then
        System.IO.Directory.CreateDirectory(outputFolder)
    End If

    Dim baseOutputName As String = SafeFileName(asmName & "_DESPIECE_VISUAL")
    Dim savePath As String = GetUniqueFilePath(System.IO.Path.Combine(outputFolder, baseOutputName & ".idw"))

    '---------------------------------------------------------
    ' ACTUALIZAR ENSAMBLAJE
    '---------------------------------------------------------

    Try
        asmDoc.Update()
    Catch
    End Try

    '---------------------------------------------------------
    ' CREAR DIBUJO DESDE PLANTILLA
    '---------------------------------------------------------

    Dim drawDoc As DrawingDocument
    drawDoc = invApp.Documents.Add(DocumentTypeEnum.kDrawingDocumentObject, templatePath, True)

    Dim sheet As Sheet = drawDoc.Sheets.Item(1)
    sheet.Activate()

    ' Si la plantilla tiene más hojas, borrar las sobrantes
    Try
        If drawDoc.Sheets.Count > 1 Then
            For i As Integer = drawDoc.Sheets.Count To 2 Step -1
                drawDoc.Sheets.Item(i).Delete()
            Next
        End If
    Catch
    End Try

    '---------------------------------------------------------
    ' ACTIVAR BOM SOLO PIEZAS
    '---------------------------------------------------------

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
        MessageBox.Show("No se ha encontrado la vista BOM Solo piezas.", "Despiece visual")
        Return
    End If

    '---------------------------------------------------------
    ' RECOGER PIEZAS IPT VÁLIDAS
    '---------------------------------------------------------

    Dim validRows As New System.Collections.Generic.List(Of BOMRow)

    For Each bomRow As BOMRow In partsOnlyView.BOMRows

        Try
            Dim compDef As ComponentDefinition = BOMRow.ComponentDefinitions.Item(1)
            Dim modelDoc As Document = compDef.Document

            If modelDoc.DocumentType = DocumentTypeEnum.kPartDocumentObject Then
                validRows.Add(BOMRow)
            End If

        Catch
            ' Si una fila da error, se omite
        End Try

    Next

    If validRows.Count = 0 Then
        MessageBox.Show("No se han encontrado piezas IPT válidas en la BOM.", "Despiece visual")
        Return
    End If

    Dim totalParts As Integer = validRows.Count

    '---------------------------------------------------------
    ' CONFIGURACIÓN DE HOJA
    ' Inventor trabaja en centímetros en planos
    '---------------------------------------------------------

    ' Márgenes útiles
    Dim marginLeft As Double = 2.0
    Dim marginRight As Double = 2.0
    Dim marginTop As Double = 1.6

    ' Margen inferior para no invadir cajetín
    Dim marginBottom As Double = 6.3

    ' Título superior
    Dim showTitle As Boolean = True
    Dim titleText As String = "DESPIECE VISUAL"

    ' Factor de ocupación de vista dentro de cada celda
    Dim viewWidthFactor As Double = 0.88
    Dim viewHeightFactor As Double = 0.62

    ' Posición vertical de vista y texto dentro de celda
    Dim viewYOffsetFactor As Double = 0.13
    Dim labelYOffsetFactor As Double = 0.34

    ' Límite para agrandar piezas pequeñas
    Dim maxEnlargeFactor As Double = 1.60

    '---------------------------------------------------------
    ' CALCULAR ESPACIO ÚTIL
    '---------------------------------------------------------

    Dim usableW As Double = sheet.Width - marginLeft - marginRight
    Dim usableH As Double = sheet.Height - marginTop - marginBottom

    If usableW <= 0 Or usableH <= 0 Then
        MessageBox.Show("El espacio útil de la hoja es insuficiente. Revisa márgenes.", "Despiece visual")
        Return
    End If

    '---------------------------------------------------------
    ' CALCULAR MEJOR CUADRÍCULA PARA 1 SOLA HOJA
    '---------------------------------------------------------

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

    '---------------------------------------------------------
    ' TAMAÑO DE TEXTO REDUCIDO PARA UNA SOLA HOJA
    '
    ' Inventor trabaja en cm:
    ' 0.13 = 1,3 mm aprox.
    ' 0.16 = 1,6 mm aprox.
    ' 0.18 = 1,8 mm aprox.
    ' 0.22 = 2,2 mm aprox.
    '---------------------------------------------------------

    Dim labelTextHeight As Double = cellH * 0.045

    ' Límites reducidos para hoja única
    If labelTextHeight > 0.22 Then labelTextHeight = 0.22
    If labelTextHeight < 0.13 Then labelTextHeight = 0.13

    Dim titleTextHeight As Double = 0.14

    '---------------------------------------------------------
    ' CREAR / CONFIGURAR ESTILOS DE TEXTO
    '---------------------------------------------------------

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

    '---------------------------------------------------------
    ' TÍTULO SUPERIOR EN UNA SOLA LÍNEA
    '---------------------------------------------------------

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

    '---------------------------------------------------------
    ' COLOCAR TODAS LAS PIEZAS EN LA MISMA HOJA
    '---------------------------------------------------------

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

        '-----------------------------------------------------
        ' LEER NÚMERO DE PIEZA
        '-----------------------------------------------------

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

        ' Limpiar caracteres conflictivos
        partNumber = Replace(partNumber, "&", "Y")
        partNumber = Replace(partNumber, "<", "")
        partNumber = Replace(partNumber, ">", "")

        '-----------------------------------------------------
        ' LEER CANTIDAD
        '-----------------------------------------------------

        Dim qty As String = ""

        Try
            qty = bomRow.TotalQuantity.ToString()
        Catch
            qty = "1"
        End Try

        If qty Is Nothing Or qty = "" Then qty = "1"

        '-----------------------------------------------------
        ' POSICIÓN EN CUADRÍCULA
        '-----------------------------------------------------

        Dim col As Integer = index Mod cols
        Dim fila As Integer = CInt(Math.Floor(index / cols))

        Dim x As Double = marginLeft + (col * cellW) + (cellW / 2)
        Dim y As Double = sheet.Height - marginTop - (fila * cellH) - (cellH / 2)

        '-----------------------------------------------------
        ' CREAR VISTA ISOMÉTRICA
        '-----------------------------------------------------

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

        '-----------------------------------------------------
        ' ESCALA AUTOMÁTICA DE VISTA
        '-----------------------------------------------------

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

        '-----------------------------------------------------
        ' ETIQUETA DE PIEZA
        '-----------------------------------------------------

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

    '---------------------------------------------------------
    ' ACTUALIZAR Y GUARDAR PLANO
    '---------------------------------------------------------

    Try
        drawDoc.Update()
    Catch
    End Try

    Try
        drawDoc.SaveAs(savePath, False)
    Catch ex As Exception
        MessageBox.Show("El plano se ha generado, pero no se ha podido guardar automáticamente." & vbCrLf & vbCrLf & _
                        "Motivo: " & ex.Message, _
                        "Despiece visual")
        Return
    End Try

    MessageBox.Show("Despiece visual generado en UNA SOLA HOJA." & vbCrLf & vbCrLf & _
                    "Ensamblaje: " & asmDoc.DisplayName & vbCrLf & _
                    "Piezas generadas: " & totalParts & vbCrLf & _
                    "Columnas: " & cols & vbCrLf & _
                    "Filas: " & rows & vbCrLf & _
                    "Tamaño texto etiqueta: " & Math.Round(labelTextHeight * 10, 1) & " mm aprox." & vbCrLf & vbCrLf & _
                    "Plano guardado en:" & vbCrLf & savePath, _
                    "Despiece visual")

End Sub