'This File was designed by the institue of chemical, environmental and bioscience engineering
'by the authors Kouessan Aziaba, Bahram Haddadi-Sisakht, Christian Jordan and Michael Harasek
'This File is based on the source code of various DWSIM unitoperations developed by Daniel Wagner Oliveira de Medeiros


Imports System.Drawing
Imports System.Windows.Forms
Imports DWSIM.Interfaces.Enums.GraphicObjects
Imports WeifenLuo.WinFormsUI.Docking
Imports su = DWSIM.SharedClasses.SystemsOfUnits
Imports Eto.Drawing
Imports DWSIM.ExtensionMethods



Public Class Editor

    Inherits WeifenLuo.WinFormsUI.Docking.DockContent

    Public Property HObject As Membrane

    Public Loaded As Boolean = False

    Dim units As SharedClasses.SystemsOfUnits.Units
    Dim nf, nff As String


    Private Sub Editor_Load(sender As Object, e As EventArgs) Handles MyBase.Load

        UpdateInfo()

    End Sub

    Sub UpdateInfo()

        nf = HObject.FlowSheet.FlowsheetOptions.NumberFormat
        nff = HObject.FlowSheet.FlowsheetOptions.FractionNumberFormat

        Loaded = False

        With HObject

            chkActive.Checked = .GraphicObject.Active

            Me.Text = .GraphicObject.Tag & " (" & .GetDisplayName() & ")"

            lblTag.Text = .GraphicObject.Tag
            If .Calculated Then
                lblStatus.Text = .FlowSheet.GetTranslatedString("Calculado") & " (" & .LastUpdated.ToString & ")"
                lblStatus.ForeColor = System.Drawing.Color.Blue
            Else
                If Not .GraphicObject.Active Then
                    lblStatus.Text = .FlowSheet.GetTranslatedString("Inativo")
                    lblStatus.ForeColor = System.Drawing.Color.Gray
                ElseIf .ErrorMessage <> "" Then
                    lblStatus.Text = .FlowSheet.GetTranslatedString("Erro")
                    lblStatus.ForeColor = System.Drawing.Color.Red
                Else
                    lblStatus.Text = .FlowSheet.GetTranslatedString("NoCalculado")
                    lblStatus.ForeColor = System.Drawing.Color.Black
                End If
            End If

            lblConnectedTo.Text = ""

            If .IsSpecAttached Then lblConnectedTo.Text = .FlowSheet.SimulationObjects(.AttachedSpecId).GraphicObject.Tag
            If .IsAdjustAttached Then lblConnectedTo.Text = .FlowSheet.SimulationObjects(.AttachedAdjustId).GraphicObject.Tag

            Dim mslist As String() = .FlowSheet.GraphicObjects.Values.Where(Function(x) x.ObjectType = ObjectType.MaterialStream).Select(Function(m) m.Tag).ToArray


            cbInlet1.Items.Clear()
            cbInlet1.Items.AddRange(mslist)


            cbOutlet1.Items.Clear()
            cbOutlet2.Items.Clear()

            cbOutlet1.Items.AddRange(mslist)
            cbOutlet2.Items.AddRange(mslist)


            If Not .GetInletMaterialStream(0) Is Nothing Then cbInlet1.SelectedItem = .GetInletMaterialStream(0).GraphicObject.Tag

            If Not .GetOutletMaterialStream(0) Is Nothing Then cbOutlet1.SelectedItem = .GetOutletMaterialStream(0).GraphicObject.Tag
            If Not .GetOutletMaterialStream(1) Is Nothing Then cbOutlet2.SelectedItem = .GetOutletMaterialStream(1).GraphicObject.Tag

            Dim eslist As String() = .FlowSheet.SimulationObjects.Values.Where(Function(x) x.GraphicObject.ObjectType = ObjectType.EnergyStream).Select(Function(m) m.GraphicObject.Tag).ToArray


            cbEnergy.Items.Clear()
            cbEnergy.Items.AddRange(eslist)

            If .GraphicObject.InputConnectors(1).IsAttached Then cbEnergy.SelectedItem = .GraphicObject.InputConnectors(1).AttachedConnector.AttachedFrom.Tag

            'Membrane and FlowModes
            Select Case HObject.CalcMode
                Case Membrane.MembraneMode.Gaspermeation
                    cbmembranemode.SelectedIndex = 0
                Case Membrane.MembraneMode.Pervaporation
                    cbmembranemode.SelectedIndex = 1
                Case Membrane.MembraneMode.SteamPermeation
                    cbmembranemode.SelectedIndex = 2
            End Select

            Select Case HObject.FlowMode
                Case Membrane.FlowDirection.CounterCurrent
                    cbFlowMode.SelectedIndex = 0
                Case Membrane.FlowDirection.CoCurrent
                    cbFlowMode.SelectedIndex = 1
                Case Membrane.FlowDirection.Crossflow
                    cbFlowMode.SelectedIndex = 2
            End Select

            'Pressure Permeate
            Tbpp.Text = DirectCast(HObject, Membrane).PermeatePressure

            'Membrane Area
            TbFiLe.Text = DirectCast(HObject, Membrane).FiberLength
            TbIdfib.Text = DirectCast(HObject, Membrane).InnerDiameterFibers
            TbNfb.Text = DirectCast(HObject, Membrane).NumberFibers

            'Membrane Cells
            Tbchamber.Text = DirectCast(HObject, Membrane).Chambers

            'Init Stage-Cut
            Tbsc.Text = DirectCast(HObject, Membrane).StageCut

            If TypeOf HObject Is Membrane Then

                '    'key compounds

                ListViewCompounds.Items.Clear()
                For Each comp In HObject.FlowSheet.SelectedCompounds.Values
                    Dim lvi As New ListViewItem()
                    With lvi
                        .Text = comp.Name
                        .Tag = comp.Name
                        .Name = comp.Name
                    End With
                    ListViewCompounds.Items.Add(lvi)
                Next
                Me.ListViewCompounds.SelectedItems.Clear()

                For Each lvi As ListViewItem In Me.ListViewCompounds.Items
                    If DirectCast(HObject, Membrane).ActiveComponents.Contains(lvi.Tag) Then lvi.Checked = True
                Next



                'Composition

                gridPermeance.Columns(0).ReadOnly = True
                gridPermeance.Rows.Clear()
                gridPermeance.Columns(1).CellTemplate.Style.Format = nff


                For Each cp As String In DirectCast(HObject, Membrane).ActiveComponents
                    If HObject.Permeances.ContainsKey(cp) Then
                        gridPermeance.Rows.Add(New Object() {cp, HObject.Permeances.Item(cp)})
                    Else
                        DirectCast(HObject, Membrane).Permeances.Add(cp, 0)
                        gridPermeance.Rows.Add(New Object() {cp, HObject.Permeances.Item(cp)})
                    End If
                Next

                For Each cp As String In DirectCast(HObject, Membrane).ActiveComponents
                    If HObject.Permeances.ContainsKey(cp) = False Then
                        DirectCast(HObject, Membrane).Permeances.Remove(cp)
                    End If
                Next


                'Results

                gridResults.ReadOnly = True
                gridResults.Rows.Clear()
                gridResults.Columns(1).CellTemplate.Style.Format = nff

                gridResults.Rows.Add(New Object() {"Area", .Area})

            End If

        End With


        'HObject.FlowSheet.ShowMessage(HObject.Permeances.Keys.ToList.ToString, Interfaces.IFlowsheet.MessageType.Other)


        Loaded = True

    End Sub


    'Sub PopulateCompGrid(grid As DataGridView, complist As List(Of Interfaces.ICompound), amounttype As String)

    '    grid.ReadOnly = True
    '    grid.Rows.Clear()
    '    grid.Columns(1).CellTemplate.Style.Format = nff

    '    For Each comp In complist
    '        grid.Rows.Add(New Object() {comp.Name, DirectCast(HObject, Membrane).Permeances.Item(comp.Name)})
    '    Next

    'End Sub

    Private Sub ComboBox1_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cbmembranemode.SelectedIndexChanged, cbFlowMode.SelectedIndexChanged

        If Loaded Then

            If sender Is cbmembranemode Then

                UpdateProps(cbmembranemode)

            ElseIf sender Is cbFlowMode Then

                UpdateProps(cbFlowMode)

            End If

            HObject.FlowSheet.RequestCalculation()

        End If


    End Sub


    Private Sub listviewcompounds_itemchecked(ByVal sender As Object, ByVal e As System.Windows.Forms.ItemCheckedEventArgs) Handles ListViewCompounds.ItemChecked
        If Loaded Then

            Dim comp = e.Item.Text

            If e.Item.Checked Then
                HObject.ActiveComponents.Add(comp)
            Else
                HObject.ActiveComponents.Remove(comp)
            End If

            HObject.NumActComponents = HObject.ActiveComponents.Count

        End If
        'HObject.FlowSheet.RequestCalculation()
    End Sub
    Private Sub btnDisconnect1_Click(sender As Object, e As EventArgs) Handles btnDisconnect1.Click
        If cbInlet1.SelectedItem IsNot Nothing Then
            HObject.FlowSheet.DisconnectObjects(HObject.GraphicObject.InputConnectors(0).AttachedConnector.AttachedFrom, HObject.GraphicObject)
            cbInlet1.SelectedItem = Nothing
        End If
    End Sub

    Private Sub btnDisconnectOutlet1_Click(sender As Object, e As EventArgs) Handles btnDisconnectOutlet1.Click
        If cbOutlet1.SelectedItem IsNot Nothing Then
            HObject.FlowSheet.DisconnectObjects(HObject.GraphicObject, HObject.GraphicObject.OutputConnectors(0).AttachedConnector.AttachedTo)
            cbOutlet1.SelectedItem = Nothing
        End If
    End Sub

    Private Sub btnDisconnectOutlet2_Click(sender As Object, e As EventArgs) Handles btnDisconnectOutlet2.Click
        If cbOutlet1.SelectedItem IsNot Nothing Then
            HObject.FlowSheet.DisconnectObjects(HObject.GraphicObject, HObject.GraphicObject.OutputConnectors(1).AttachedConnector.AttachedTo)
            cbOutlet1.SelectedItem = Nothing
        End If
    End Sub

    Private Sub btnDisconnectEnergy_Click(sender As Object, e As EventArgs) Handles btnDisconnectEnergy.Click
        If cbEnergy.SelectedItem IsNot Nothing Then
            HObject.FlowSheet.DisconnectObjects(HObject.GraphicObject.InputConnectors(1).AttachedConnector.AttachedFrom, HObject.GraphicObject)
            cbEnergy.SelectedItem = Nothing
            HObject.FlowSheet.UpdateInterface()
        End If
    End Sub

    Sub HandleInletConnections(sender As Object, e As EventArgs) Handles cbInlet1.SelectedIndexChanged
        If Loaded Then UpdateInletConnection(sender)
    End Sub

    Sub HandleOutletConnections(sender As Object, e As EventArgs) Handles cbOutlet1.SelectedIndexChanged, cbOutlet2.SelectedIndexChanged
        If Loaded Then UpdateOutletConnection(sender)
    End Sub



    Sub UpdateInletConnection(cb As ComboBox)

        Dim text As String = cb.Text

        If text <> "" Then

            Dim index As Integer = Convert.ToInt32(cb.Name.Substring(cb.Name.Length - 1)) - 1

            Dim gobj = HObject.GraphicObject
            Dim flowsheet = HObject.FlowSheet

            If flowsheet.GetFlowsheetSimulationObject(text).GraphicObject.OutputConnectors(0).IsAttached Then
                MessageBox.Show(flowsheet.GetTranslatedString("Todasasconexespossve"), flowsheet.GetTranslatedString("Erro"), MessageBoxButtons.OK, MessageBoxIcon.Error)
            Else
                If gobj.InputConnectors(index).IsAttached Then flowsheet.DisconnectObjects(gobj.InputConnectors(index).AttachedConnector.AttachedFrom, gobj)
                Try
                    flowsheet.ConnectObjects(flowsheet.GetFlowsheetSimulationObject(text).GraphicObject, gobj, 0, index)
                Catch ex As Exception
                    MessageBox.Show(ex.Message, flowsheet.GetTranslatedString("Erro"), MessageBoxButtons.OK, MessageBoxIcon.Error)
                End Try
            End If
            UpdateInfo()

        End If

    End Sub

    Sub UpdateOutletConnection(cb As ComboBox)

        Dim text As String = cb.Text

        If text <> "" Then

            Dim index As Integer = Convert.ToInt32(cb.Name.Substring(cb.Name.Length - 1)) - 1

            Dim gobj = HObject.GraphicObject
            Dim flowsheet = HObject.FlowSheet

            If flowsheet.GetFlowsheetSimulationObject(text).GraphicObject.InputConnectors(0).IsAttached Then
                MessageBox.Show(flowsheet.GetTranslatedString("Todasasconexespossve"), flowsheet.GetTranslatedString("Erro"), MessageBoxButtons.OK, MessageBoxIcon.Error)
            Else
                If gobj.OutputConnectors(index).IsAttached Then flowsheet.DisconnectObjects(gobj, gobj.OutputConnectors(index).AttachedConnector.AttachedTo)
                Try
                    flowsheet.ConnectObjects(gobj, flowsheet.GetFlowsheetSimulationObject(text).GraphicObject, index, 0)
                Catch ex As Exception
                    MessageBox.Show(ex.Message, flowsheet.GetTranslatedString("Erro"), MessageBoxButtons.OK, MessageBoxIcon.Error)
                End Try
            End If
            UpdateInfo()

        End If

    End Sub


    Private Sub cbEnergy_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cbEnergy.SelectedIndexChanged

        If Loaded Then

            Dim text As String = cbEnergy.Text

            If text <> "" Then

                Dim index As Integer = 1

                Dim gobj = HObject.GraphicObject
                Dim flowsheet = HObject.FlowSheet

                If flowsheet.GetFlowsheetSimulationObject(text).GraphicObject.OutputConnectors(0).IsAttached Then
                    MessageBox.Show(flowsheet.GetTranslatedString("Todasasconexespossve"), flowsheet.GetTranslatedString("Erro"), MessageBoxButtons.OK, MessageBoxIcon.Error)
                    Exit Sub
                End If
                If gobj.InputConnectors(index).IsAttached Then flowsheet.DisconnectObjects(gobj.InputConnectors(index).AttachedConnector.AttachedFrom, gobj)
                flowsheet.ConnectObjects(flowsheet.GetFlowsheetSimulationObject(text).GraphicObject, gobj, 0, index)

            End If

        End If

    End Sub

    Private Sub rtbAnnotations_RtfChanged(sender As Object, e As EventArgs) Handles rtbAnnotations.RtfChanged
        If Loaded Then HObject.Annotation = rtbAnnotations.Rtf
    End Sub



    Private Sub chkActive_CheckedChanged(sender As Object, e As EventArgs) Handles chkActive.CheckedChanged
        If Loaded Then
            HObject.GraphicObject.Active = chkActive.Checked
            HObject.FlowSheet.UpdateInterface()
            UpdateInfo()
        End If
    End Sub

    Private Sub tb_TextChanged(sender As Object, e As EventArgs) Handles TbFiLe.TextChanged, TbIdfib.TextChanged, TbNfb.TextChanged, Tbpp.TextChanged, Tbchamber.TextChanged, Tbsc.TextChanged

        Dim tbox = DirectCast(sender, TextBox)

        If tbox.Text.IsValidDoubleExpression Then
            tbox.ForeColor = System.Drawing.Color.Blue
        Else
            tbox.ForeColor = System.Drawing.Color.Red
        End If

    End Sub

    Private Sub TextBoxKeyDown(sender As Object, e As KeyEventArgs) Handles TbFiLe.KeyDown, TbIdfib.KeyDown, TbNfb.KeyDown, Tbpp.KeyDown, Tbchamber.KeyDown, Tbsc.KeyDown


        If e.KeyCode = Keys.Enter And Loaded And DirectCast(sender, TextBox).ForeColor = System.Drawing.Color.Blue Then

            UpdateProps(sender)

            DirectCast(sender, TextBox).SelectAll()

        End If

    End Sub

    Sub UpdateProps(sender As Object)

        If sender Is cbmembranemode Then

            Select Case cbmembranemode.SelectedIndex
                Case 0
                    DirectCast(HObject, Membrane).CalcMode = Membrane.MembraneMode.Gaspermeation
                Case 1
                    DirectCast(HObject, Membrane).CalcMode = Membrane.MembraneMode.Pervaporation
                Case 2
                    DirectCast(HObject, Membrane).CalcMode = Membrane.MembraneMode.SteamPermeation

            End Select

        ElseIf sender Is cbFlowMode Then

            Select Case cbFlowMode.SelectedIndex
                Case 0
                    DirectCast(HObject, Membrane).FlowMode = Membrane.FlowDirection.CounterCurrent
                Case 1
                    DirectCast(HObject, Membrane).FlowMode = Membrane.FlowDirection.CoCurrent
                Case 2
                    DirectCast(HObject, Membrane).FlowMode = Membrane.FlowDirection.Crossflow

            End Select

        End If

        If sender Is TbFiLe Then DirectCast(HObject, Membrane).FiberLength = TbFiLe.Text.ParseExpressionToDouble
        If sender Is TbIdfib Then DirectCast(HObject, Membrane).InnerDiameterFibers = TbIdfib.Text.ParseExpressionToDouble
        If sender Is TbNfb Then DirectCast(HObject, Membrane).NumberFibers = TbNfb.Text.ParseExpressionToDouble
        If sender Is Tbpp Then DirectCast(HObject, Membrane).PermeatePressure = Tbpp.Text.ParseExpressionToDouble
        If sender Is Tbchamber Then DirectCast(HObject, Membrane).Chambers = Tbchamber.Text.ParseExpressionToDouble
        If sender Is Tbsc Then DirectCast(HObject, Membrane).StageCut = Tbsc.Text.ParseExpressionToDouble

        RequestCalc()

    End Sub

    Private Sub btnCreateAndConnectInlet1_Click(sender As Object, e As EventArgs) Handles btnCreateAndConnectInlet1.Click, btnCreateAndConnectOutlet1.Click, btnCreateAndConnectOutlet2.Click, btnCreateAndConnectEnergy.Click

        Dim sgobj = HObject.GraphicObject
        Dim fs = HObject.FlowSheet

        If sender Is btnCreateAndConnectInlet1 Then

            Dim obj = fs.AddObject(ObjectType.MaterialStream, sgobj.InputConnectors(0).Position.X - 50, sgobj.InputConnectors(0).Position.Y - 10, "")

            If sgobj.InputConnectors(0).IsAttached Then fs.DisconnectObjects(sgobj.InputConnectors(0).AttachedConnector.AttachedFrom, sgobj)
            fs.ConnectObjects(obj.GraphicObject, sgobj, 0, 0)

        ElseIf sender Is btnCreateAndConnectOutlet1 Then

            Dim obj = fs.AddObject(ObjectType.MaterialStream, sgobj.OutputConnectors(0).Position.X + 30, sgobj.OutputConnectors(0).Position.Y - 10, "")

            If sgobj.OutputConnectors(0).IsAttached Then fs.DisconnectObjects(sgobj, sgobj.OutputConnectors(0).AttachedConnector.AttachedTo)
            fs.ConnectObjects(sgobj, obj.GraphicObject, 0, 0)

        ElseIf sender Is btnCreateAndConnectOutlet2 Then

            Dim obj = fs.AddObject(ObjectType.MaterialStream, sgobj.OutputConnectors(1).Position.X + 20, sgobj.OutputConnectors(1).Position.Y - 10, "")

            If sgobj.OutputConnectors(1).IsAttached Then fs.DisconnectObjects(sgobj, sgobj.OutputConnectors(1).AttachedConnector.AttachedTo)
            fs.ConnectObjects(sgobj, obj.GraphicObject, 1, 0)

        ElseIf sender Is btnCreateAndConnectEnergy Then


            If TypeOf HObject Is Membrane Then
                Dim obj = fs.AddObject(ObjectType.EnergyStream, sgobj.InputConnectors(1).Position.X + 30, sgobj.InputConnectors(1).Position.Y + 30, "")


                If sgobj.InputConnectors(1).IsAttached Then fs.DisconnectObjects(sgobj, sgobj.InputConnectors(1).AttachedConnector.AttachedTo)
                fs.ConnectObjects(sgobj, obj.GraphicObject, 1, 0)

            End If
        End If

        HObject.FlowSheet.UpdateInterface()
        UpdateInfo()

    End Sub


    Private Sub lblTag_KeyPress(sender As Object, e As KeyEventArgs) Handles lblTag.KeyUp

        If e.KeyCode = Keys.Enter Then

            If Loaded Then HObject.GraphicObject.Tag = lblTag.Text
            If Loaded Then HObject.FlowSheet.UpdateOpenEditForms()
            Me.Text = HObject.GraphicObject.Tag & " (" & HObject.GetDisplayName() & ")"
            DirectCast(HObject.FlowSheet, Interfaces.IFlowsheetGUI).UpdateInterface()

        End If

    End Sub

    Sub RequestCalc()

        HObject.FlowSheet.RequestCalculation(HObject)

    End Sub


    Private Sub UpdatePermeances()
        If Loaded Then
            HObject.Permeances.Clear()
            For Each row As DataGridViewRow In gridPermeance.Rows
                Try
                    HObject.Permeances.Add(row.Cells(0).Value, row.Cells(1).Value)
                    If Not HObject.AllPermeances.ContainsKey(row.Cells(0).Value) Then
                        HObject.AllPermeances.Add(row.Cells(0).Value, row.Cells(1).Value)
                    End If
                Catch ex As Exception
                    MessageBox.Show(ex.Message)
                End Try
            Next
        End If
        'UpdateInfo()
    End Sub




    Private Sub gridPermeance_CellValueChanged(sender As Object, e As DataGridViewCellEventArgs) Handles gridPermeance.CellValueChanged


        'If Loaded Then

        '    Try

        '        'Dim value As Double = gridPermeance.Rows(e.RowIndex).Cells(1).Value

        '        'Dim comp = gridPermeance.Rows(e.RowIndex).Cells(0).Value

        '        'HObject.CompoundNames(comp) = value

        '        DirectCast(HObject, Membrane).Permeances.Item(gridPermeance.Rows(e.RowIndex).Cells(0).Value) = gridPermeance.Rows(e.RowIndex).Cells(1).Value


        '    Catch ex As Exception

        '        MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)

        '    End Try

        'End If


        UpdatePermeances()

    End Sub

    Private Sub ListViewCompounds_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ListViewCompounds.SelectedIndexChanged
        UpdateInfo()
    End Sub

    Private Sub gridPermeance_CellContentClick(sender As Object, e As DataGridViewCellEventArgs) Handles gridPermeance.CellContentClick

        If e.ColumnIndex = 2 Then
            For i = 0 To gridPermeance.RowCount - 1
                If i <> e.RowIndex Then
                    gridPermeance.Rows(i).Cells(2).Value = False
                    gridPermeance.Rows(i).Cells(1).ReadOnly = False
                    gridPermeance.Rows(i).Cells(2).ReadOnly = False
                    gridPermeance.Rows(i).Cells(1).Style.BackColor = Nothing
                Else
                    'gridPermeance.Rows(i).Cells(2).Value = True
                    gridPermeance.Rows(i).Cells(1).ReadOnly = True
                    gridPermeance.Rows(i).Cells(2).ReadOnly = True
                End If
            Next
        End If
        UpdatePermeances()
        'UpdateInfo()
    End Sub



End Class