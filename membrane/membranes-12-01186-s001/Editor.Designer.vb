'This File was designed by the institue of chemical, environmental and bioscience engineering
'by the authors Kouessan Aziaba, Bahram Haddadi-Sisakht, Christian Jordan and Michael Harasek
'This File is based on the source code of various DWSIM unitoperations developed by Daniel Wagner Oliveira de Medeiros


<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class Editor

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()>
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        Me.Label1 = New System.Windows.Forms.Label()
        Me.cbmembranemode = New System.Windows.Forms.ComboBox()
        Me.GroupBox1 = New System.Windows.Forms.GroupBox()
        Me.btnDisconnectEnergy = New System.Windows.Forms.Button()
        Me.btnDisconnectOutlet2 = New System.Windows.Forms.Button()
        Me.btnDisconnectOutlet1 = New System.Windows.Forms.Button()
        Me.btnCreateAndConnectEnergy = New System.Windows.Forms.Button()
        Me.btnCreateAndConnectOutlet2 = New System.Windows.Forms.Button()
        Me.btnCreateAndConnectOutlet1 = New System.Windows.Forms.Button()
        Me.btnDisconnect1 = New System.Windows.Forms.Button()
        Me.btnCreateAndConnectInlet1 = New System.Windows.Forms.Button()
        Me.Label16 = New System.Windows.Forms.Label()
        Me.cbEnergy = New System.Windows.Forms.ComboBox()
        Me.Label7 = New System.Windows.Forms.Label()
        Me.cbOutlet2 = New System.Windows.Forms.ComboBox()
        Me.cbOutlet1 = New System.Windows.Forms.ComboBox()
        Me.Label2 = New System.Windows.Forms.Label()
        Me.cbInlet1 = New System.Windows.Forms.ComboBox()
        Me.Label8 = New System.Windows.Forms.Label()
        Me.GroupBox5 = New System.Windows.Forms.GroupBox()
        Me.chkActive = New System.Windows.Forms.CheckBox()
        Me.lblTag = New System.Windows.Forms.TextBox()
        Me.lblConnectedTo = New System.Windows.Forms.Label()
        Me.lblStatus = New System.Windows.Forms.Label()
        Me.Label13 = New System.Windows.Forms.Label()
        Me.Label12 = New System.Windows.Forms.Label()
        Me.Label11 = New System.Windows.Forms.Label()
        Me.GroupBox4 = New System.Windows.Forms.GroupBox()
        Me.rtbAnnotations = New Extended.Windows.Forms.RichTextBoxExtended()
        Me.ppres = New System.Windows.Forms.Label()
        Me.GroupBox2 = New System.Windows.Forms.GroupBox()
        Me.Tbsc = New System.Windows.Forms.TextBox()
        Me.Label9 = New System.Windows.Forms.Label()
        Me.Tbchamber = New System.Windows.Forms.TextBox()
        Me.Label6 = New System.Windows.Forms.Label()
        Me.Label5 = New System.Windows.Forms.Label()
        Me.Label4 = New System.Windows.Forms.Label()
        Me.Label3 = New System.Windows.Forms.Label()
        Me.TbFiLe = New System.Windows.Forms.TextBox()
        Me.TbIdfib = New System.Windows.Forms.TextBox()
        Me.TbNfb = New System.Windows.Forms.TextBox()
        Me.Tbpp = New System.Windows.Forms.TextBox()
        Me.fiblen = New System.Windows.Forms.Label()
        Me.Idfi = New System.Windows.Forms.Label()
        Me.nfib = New System.Windows.Forms.Label()
        Me.lblFlowmode = New System.Windows.Forms.Label()
        Me.cbFlowMode = New System.Windows.Forms.ComboBox()
        Me.TabControl1 = New System.Windows.Forms.TabControl()
        Me.TabPageParams = New System.Windows.Forms.TabPage()
        Me.TabInclude = New System.Windows.Forms.TabPage()
        Me.ListViewCompounds = New System.Windows.Forms.ListView()
        Me.TabPagePermeances = New System.Windows.Forms.TabPage()
        Me.gridPermeance = New System.Windows.Forms.DataGridView()
        Me.Compound = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.Permeance = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.TabControl2 = New System.Windows.Forms.TabControl()
        Me.Results = New System.Windows.Forms.TabPage()
        Me.gridResults = New System.Windows.Forms.DataGridView()
        Me.DataGridViewTextBoxColumn1 = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.DataGridViewTextBoxColumn2 = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.TabPage2 = New System.Windows.Forms.TabPage()
        Me.GroupBox1.SuspendLayout()
        Me.GroupBox5.SuspendLayout()
        Me.GroupBox4.SuspendLayout()
        Me.GroupBox2.SuspendLayout()
        Me.TabControl1.SuspendLayout()
        Me.TabPageParams.SuspendLayout()
        Me.TabInclude.SuspendLayout()
        Me.TabPagePermeances.SuspendLayout()
        CType(Me.gridPermeance, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.TabControl2.SuspendLayout()
        Me.Results.SuspendLayout()
        CType(Me.gridResults, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.TabPage2.SuspendLayout()
        Me.SuspendLayout()
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Location = New System.Drawing.Point(9, 22)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(84, 13)
        Me.Label1.TabIndex = 0
        Me.Label1.Text = "Membrane Type"
        '
        'cbmembranemode
        '
        Me.cbmembranemode.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cbmembranemode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cbmembranemode.FormattingEnabled = True
        Me.cbmembranemode.Items.AddRange(New Object() {"Gas Permeation", "Pervaporation", "Steam Permeation"})
        Me.cbmembranemode.Location = New System.Drawing.Point(193, 19)
        Me.cbmembranemode.Name = "cbmembranemode"
        Me.cbmembranemode.Size = New System.Drawing.Size(158, 21)
        Me.cbmembranemode.TabIndex = 1
        '
        'GroupBox1
        '
        Me.GroupBox1.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.GroupBox1.Controls.Add(Me.btnDisconnectEnergy)
        Me.GroupBox1.Controls.Add(Me.btnDisconnectOutlet2)
        Me.GroupBox1.Controls.Add(Me.btnDisconnectOutlet1)
        Me.GroupBox1.Controls.Add(Me.btnCreateAndConnectEnergy)
        Me.GroupBox1.Controls.Add(Me.btnCreateAndConnectOutlet2)
        Me.GroupBox1.Controls.Add(Me.btnCreateAndConnectOutlet1)
        Me.GroupBox1.Controls.Add(Me.btnDisconnect1)
        Me.GroupBox1.Controls.Add(Me.btnCreateAndConnectInlet1)
        Me.GroupBox1.Controls.Add(Me.Label16)
        Me.GroupBox1.Controls.Add(Me.cbEnergy)
        Me.GroupBox1.Controls.Add(Me.Label7)
        Me.GroupBox1.Controls.Add(Me.cbOutlet2)
        Me.GroupBox1.Controls.Add(Me.cbOutlet1)
        Me.GroupBox1.Controls.Add(Me.Label2)
        Me.GroupBox1.Controls.Add(Me.cbInlet1)
        Me.GroupBox1.Controls.Add(Me.Label8)
        Me.GroupBox1.Location = New System.Drawing.Point(12, 116)
        Me.GroupBox1.Name = "GroupBox1"
        Me.GroupBox1.Size = New System.Drawing.Size(389, 139)
        Me.GroupBox1.TabIndex = 2
        Me.GroupBox1.TabStop = False
        Me.GroupBox1.Text = "Connections"
        '
        'btnDisconnectEnergy
        '
        Me.btnDisconnectEnergy.BackgroundImage = Global.DWSIM.UnitOperations.Membrane.My.Resources.Resources.disconnect
        Me.btnDisconnectEnergy.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center
        Me.btnDisconnectEnergy.Location = New System.Drawing.Point(358, 99)
        Me.btnDisconnectEnergy.Name = "btnDisconnectEnergy"
        Me.btnDisconnectEnergy.Size = New System.Drawing.Size(23, 21)
        Me.btnDisconnectEnergy.TabIndex = 42
        Me.btnDisconnectEnergy.UseVisualStyleBackColor = True
        '
        'btnDisconnectOutlet2
        '
        Me.btnDisconnectOutlet2.BackgroundImage = Global.DWSIM.UnitOperations.Membrane.My.Resources.Resources.disconnect
        Me.btnDisconnectOutlet2.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center
        Me.btnDisconnectOutlet2.Location = New System.Drawing.Point(358, 72)
        Me.btnDisconnectOutlet2.Name = "btnDisconnectOutlet2"
        Me.btnDisconnectOutlet2.Size = New System.Drawing.Size(23, 21)
        Me.btnDisconnectOutlet2.TabIndex = 41
        Me.btnDisconnectOutlet2.UseVisualStyleBackColor = True
        '
        'btnDisconnectOutlet1
        '
        Me.btnDisconnectOutlet1.BackgroundImage = Global.DWSIM.UnitOperations.Membrane.My.Resources.Resources.disconnect
        Me.btnDisconnectOutlet1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center
        Me.btnDisconnectOutlet1.Location = New System.Drawing.Point(358, 45)
        Me.btnDisconnectOutlet1.Name = "btnDisconnectOutlet1"
        Me.btnDisconnectOutlet1.Size = New System.Drawing.Size(23, 21)
        Me.btnDisconnectOutlet1.TabIndex = 40
        Me.btnDisconnectOutlet1.UseVisualStyleBackColor = True
        '
        'btnCreateAndConnectEnergy
        '
        Me.btnCreateAndConnectEnergy.BackgroundImage = Global.DWSIM.UnitOperations.Membrane.My.Resources.Resources.bullet_lightning
        Me.btnCreateAndConnectEnergy.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom
        Me.btnCreateAndConnectEnergy.Location = New System.Drawing.Point(330, 99)
        Me.btnCreateAndConnectEnergy.Name = "btnCreateAndConnectEnergy"
        Me.btnCreateAndConnectEnergy.Size = New System.Drawing.Size(23, 21)
        Me.btnCreateAndConnectEnergy.TabIndex = 39
        Me.btnCreateAndConnectEnergy.UseVisualStyleBackColor = True
        '
        'btnCreateAndConnectOutlet2
        '
        Me.btnCreateAndConnectOutlet2.BackgroundImage = Global.DWSIM.UnitOperations.Membrane.My.Resources.Resources.bullet_lightning
        Me.btnCreateAndConnectOutlet2.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom
        Me.btnCreateAndConnectOutlet2.Location = New System.Drawing.Point(330, 72)
        Me.btnCreateAndConnectOutlet2.Name = "btnCreateAndConnectOutlet2"
        Me.btnCreateAndConnectOutlet2.Size = New System.Drawing.Size(23, 21)
        Me.btnCreateAndConnectOutlet2.TabIndex = 38
        Me.btnCreateAndConnectOutlet2.UseVisualStyleBackColor = True
        '
        'btnCreateAndConnectOutlet1
        '
        Me.btnCreateAndConnectOutlet1.BackgroundImage = Global.DWSIM.UnitOperations.Membrane.My.Resources.Resources.bullet_lightning
        Me.btnCreateAndConnectOutlet1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom
        Me.btnCreateAndConnectOutlet1.Location = New System.Drawing.Point(330, 45)
        Me.btnCreateAndConnectOutlet1.Name = "btnCreateAndConnectOutlet1"
        Me.btnCreateAndConnectOutlet1.Size = New System.Drawing.Size(23, 21)
        Me.btnCreateAndConnectOutlet1.TabIndex = 37
        Me.btnCreateAndConnectOutlet1.UseVisualStyleBackColor = True
        '
        'btnDisconnect1
        '
        Me.btnDisconnect1.BackgroundImage = Global.DWSIM.UnitOperations.Membrane.My.Resources.Resources.disconnect
        Me.btnDisconnect1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center
        Me.btnDisconnect1.Location = New System.Drawing.Point(358, 18)
        Me.btnDisconnect1.Name = "btnDisconnect1"
        Me.btnDisconnect1.Size = New System.Drawing.Size(23, 21)
        Me.btnDisconnect1.TabIndex = 30
        Me.btnDisconnect1.UseVisualStyleBackColor = True
        '
        'btnCreateAndConnectInlet1
        '
        Me.btnCreateAndConnectInlet1.BackgroundImage = Global.DWSIM.UnitOperations.Membrane.My.Resources.Resources.bullet_lightning
        Me.btnCreateAndConnectInlet1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom
        Me.btnCreateAndConnectInlet1.Location = New System.Drawing.Point(330, 18)
        Me.btnCreateAndConnectInlet1.Name = "btnCreateAndConnectInlet1"
        Me.btnCreateAndConnectInlet1.Size = New System.Drawing.Size(23, 21)
        Me.btnCreateAndConnectInlet1.TabIndex = 29
        Me.btnCreateAndConnectInlet1.UseVisualStyleBackColor = True
        '
        'Label16
        '
        Me.Label16.AutoSize = True
        Me.Label16.ImeMode = System.Windows.Forms.ImeMode.NoControl
        Me.Label16.Location = New System.Drawing.Point(9, 103)
        Me.Label16.Name = "Label16"
        Me.Label16.Size = New System.Drawing.Size(76, 13)
        Me.Label16.TabIndex = 28
        Me.Label16.Text = "Energy Stream"
        '
        'cbEnergy
        '
        Me.cbEnergy.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cbEnergy.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cbEnergy.FormattingEnabled = True
        Me.cbEnergy.Location = New System.Drawing.Point(144, 99)
        Me.cbEnergy.Name = "cbEnergy"
        Me.cbEnergy.Size = New System.Drawing.Size(178, 21)
        Me.cbEnergy.TabIndex = 27
        '
        'Label7
        '
        Me.Label7.AutoSize = True
        Me.Label7.ImeMode = System.Windows.Forms.ImeMode.NoControl
        Me.Label7.Location = New System.Drawing.Point(9, 79)
        Me.Label7.Name = "Label7"
        Me.Label7.Size = New System.Drawing.Size(90, 13)
        Me.Label7.TabIndex = 13
        Me.Label7.Text = "Retentate Stream"
        '
        'cbOutlet2
        '
        Me.cbOutlet2.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cbOutlet2.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cbOutlet2.FormattingEnabled = True
        Me.cbOutlet2.Location = New System.Drawing.Point(144, 72)
        Me.cbOutlet2.Name = "cbOutlet2"
        Me.cbOutlet2.Size = New System.Drawing.Size(178, 21)
        Me.cbOutlet2.TabIndex = 8
        '
        'cbOutlet1
        '
        Me.cbOutlet1.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cbOutlet1.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cbOutlet1.FormattingEnabled = True
        Me.cbOutlet1.Location = New System.Drawing.Point(144, 45)
        Me.cbOutlet1.Name = "cbOutlet1"
        Me.cbOutlet1.Size = New System.Drawing.Size(178, 21)
        Me.cbOutlet1.TabIndex = 3
        '
        'Label2
        '
        Me.Label2.AutoSize = True
        Me.Label2.ImeMode = System.Windows.Forms.ImeMode.NoControl
        Me.Label2.Location = New System.Drawing.Point(9, 52)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(88, 13)
        Me.Label2.TabIndex = 2
        Me.Label2.Text = "Permeate Stream"
        '
        'cbInlet1
        '
        Me.cbInlet1.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cbInlet1.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cbInlet1.FormattingEnabled = True
        Me.cbInlet1.Location = New System.Drawing.Point(144, 18)
        Me.cbInlet1.Name = "cbInlet1"
        Me.cbInlet1.Size = New System.Drawing.Size(178, 21)
        Me.cbInlet1.TabIndex = 1
        '
        'Label8
        '
        Me.Label8.AutoSize = True
        Me.Label8.ImeMode = System.Windows.Forms.ImeMode.NoControl
        Me.Label8.Location = New System.Drawing.Point(9, 26)
        Me.Label8.Name = "Label8"
        Me.Label8.Size = New System.Drawing.Size(67, 13)
        Me.Label8.TabIndex = 0
        Me.Label8.Text = "Feed Stream"
        '
        'GroupBox5
        '
        Me.GroupBox5.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.GroupBox5.Controls.Add(Me.chkActive)
        Me.GroupBox5.Controls.Add(Me.lblTag)
        Me.GroupBox5.Controls.Add(Me.lblConnectedTo)
        Me.GroupBox5.Controls.Add(Me.lblStatus)
        Me.GroupBox5.Controls.Add(Me.Label13)
        Me.GroupBox5.Controls.Add(Me.Label12)
        Me.GroupBox5.Controls.Add(Me.Label11)
        Me.GroupBox5.Location = New System.Drawing.Point(12, 12)
        Me.GroupBox5.Name = "GroupBox5"
        Me.GroupBox5.Size = New System.Drawing.Size(389, 98)
        Me.GroupBox5.TabIndex = 5
        Me.GroupBox5.TabStop = False
        Me.GroupBox5.Text = "General"
        '
        'chkActive
        '
        Me.chkActive.AutoSize = True
        Me.chkActive.CheckAlign = System.Drawing.ContentAlignment.MiddleRight
        Me.chkActive.FlatAppearance.CheckedBackColor = System.Drawing.Color.Lime
        Me.chkActive.Location = New System.Drawing.Point(358, 47)
        Me.chkActive.Name = "chkActive"
        Me.chkActive.Size = New System.Drawing.Size(15, 14)
        Me.chkActive.TabIndex = 26
        Me.chkActive.UseVisualStyleBackColor = True
        '
        'lblTag
        '
        Me.lblTag.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.lblTag.Location = New System.Drawing.Point(144, 19)
        Me.lblTag.Name = "lblTag"
        Me.lblTag.Size = New System.Drawing.Size(178, 20)
        Me.lblTag.TabIndex = 25
        '
        'lblConnectedTo
        '
        Me.lblConnectedTo.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.lblConnectedTo.ImeMode = System.Windows.Forms.ImeMode.NoControl
        Me.lblConnectedTo.Location = New System.Drawing.Point(141, 72)
        Me.lblConnectedTo.Name = "lblConnectedTo"
        Me.lblConnectedTo.Size = New System.Drawing.Size(230, 18)
        Me.lblConnectedTo.TabIndex = 20
        Me.lblConnectedTo.Text = "LinkedTo"
        '
        'lblStatus
        '
        Me.lblStatus.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.lblStatus.ImeMode = System.Windows.Forms.ImeMode.NoControl
        Me.lblStatus.Location = New System.Drawing.Point(141, 47)
        Me.lblStatus.Name = "lblStatus"
        Me.lblStatus.Size = New System.Drawing.Size(230, 18)
        Me.lblStatus.TabIndex = 19
        Me.lblStatus.Text = "Status"
        '
        'Label13
        '
        Me.Label13.AutoSize = True
        Me.Label13.ImeMode = System.Windows.Forms.ImeMode.NoControl
        Me.Label13.Location = New System.Drawing.Point(9, 72)
        Me.Label13.Name = "Label13"
        Me.Label13.Size = New System.Drawing.Size(51, 13)
        Me.Label13.TabIndex = 17
        Me.Label13.Text = "Linked to"
        '
        'Label12
        '
        Me.Label12.AutoSize = True
        Me.Label12.ImeMode = System.Windows.Forms.ImeMode.NoControl
        Me.Label12.Location = New System.Drawing.Point(9, 47)
        Me.Label12.Name = "Label12"
        Me.Label12.Size = New System.Drawing.Size(37, 13)
        Me.Label12.TabIndex = 16
        Me.Label12.Text = "Status"
        '
        'Label11
        '
        Me.Label11.AutoSize = True
        Me.Label11.ImeMode = System.Windows.Forms.ImeMode.NoControl
        Me.Label11.Location = New System.Drawing.Point(9, 22)
        Me.Label11.Name = "Label11"
        Me.Label11.Size = New System.Drawing.Size(69, 13)
        Me.Label11.TabIndex = 14
        Me.Label11.Text = "Object Name"
        '
        'GroupBox4
        '
        Me.GroupBox4.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.GroupBox4.Controls.Add(Me.rtbAnnotations)
        Me.GroupBox4.Location = New System.Drawing.Point(6, 3)
        Me.GroupBox4.Name = "GroupBox4"
        Me.GroupBox4.Size = New System.Drawing.Size(369, 218)
        Me.GroupBox4.TabIndex = 8
        Me.GroupBox4.TabStop = False
        Me.GroupBox4.Text = "Notes"
        '
        'rtbAnnotations
        '
        Me.rtbAnnotations.Dock = System.Windows.Forms.DockStyle.Fill
        Me.rtbAnnotations.Location = New System.Drawing.Point(3, 16)
        Me.rtbAnnotations.Name = "rtbAnnotations"
        Me.rtbAnnotations.Rtf = "{\rtf1\ansi\ansicpg1252\deff0\deflang1046{\fonttbl{\f0\fnil Microsoft Sans Serif;" &
    "}}" & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "\viewkind4\uc1\pard\f0\fs17\par" & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "}" & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10)
        Me.rtbAnnotations.ShowRedo = False
        Me.rtbAnnotations.ShowUndo = False
        Me.rtbAnnotations.Size = New System.Drawing.Size(363, 199)
        Me.rtbAnnotations.TabIndex = 0
        Me.rtbAnnotations.ToolbarVisible = False
        '
        'ppres
        '
        Me.ppres.AutoSize = True
        Me.ppres.ImeMode = System.Windows.Forms.ImeMode.NoControl
        Me.ppres.Location = New System.Drawing.Point(9, 78)
        Me.ppres.Name = "ppres"
        Me.ppres.Size = New System.Drawing.Size(96, 13)
        Me.ppres.TabIndex = 14
        Me.ppres.Text = "Permeate Pressure"
        '
        'GroupBox2
        '
        Me.GroupBox2.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.GroupBox2.Controls.Add(Me.Tbsc)
        Me.GroupBox2.Controls.Add(Me.Label9)
        Me.GroupBox2.Controls.Add(Me.Tbchamber)
        Me.GroupBox2.Controls.Add(Me.Label6)
        Me.GroupBox2.Controls.Add(Me.Label5)
        Me.GroupBox2.Controls.Add(Me.Label4)
        Me.GroupBox2.Controls.Add(Me.Label3)
        Me.GroupBox2.Controls.Add(Me.TbFiLe)
        Me.GroupBox2.Controls.Add(Me.TbIdfib)
        Me.GroupBox2.Controls.Add(Me.TbNfb)
        Me.GroupBox2.Controls.Add(Me.Tbpp)
        Me.GroupBox2.Controls.Add(Me.fiblen)
        Me.GroupBox2.Controls.Add(Me.Idfi)
        Me.GroupBox2.Controls.Add(Me.nfib)
        Me.GroupBox2.Controls.Add(Me.ppres)
        Me.GroupBox2.Controls.Add(Me.lblFlowmode)
        Me.GroupBox2.Controls.Add(Me.cbFlowMode)
        Me.GroupBox2.Controls.Add(Me.Label1)
        Me.GroupBox2.Controls.Add(Me.cbmembranemode)
        Me.GroupBox2.Location = New System.Drawing.Point(8, 3)
        Me.GroupBox2.Name = "GroupBox2"
        Me.GroupBox2.Size = New System.Drawing.Size(367, 251)
        Me.GroupBox2.TabIndex = 6
        Me.GroupBox2.TabStop = False
        Me.GroupBox2.Text = "Calculation Parameters"
        '
        'Tbsc
        '
        Me.Tbsc.Location = New System.Drawing.Point(192, 205)
        Me.Tbsc.Name = "Tbsc"
        Me.Tbsc.Size = New System.Drawing.Size(118, 20)
        Me.Tbsc.TabIndex = 28
        Me.Tbsc.TextAlign = System.Windows.Forms.HorizontalAlignment.Right
        '
        'Label9
        '
        Me.Label9.AutoSize = True
        Me.Label9.ImeMode = System.Windows.Forms.ImeMode.NoControl
        Me.Label9.Location = New System.Drawing.Point(9, 208)
        Me.Label9.Name = "Label9"
        Me.Label9.Size = New System.Drawing.Size(81, 13)
        Me.Label9.TabIndex = 27
        Me.Label9.Text = "Initial Stage-Cut"
        '
        'Tbchamber
        '
        Me.Tbchamber.Location = New System.Drawing.Point(192, 179)
        Me.Tbchamber.Name = "Tbchamber"
        Me.Tbchamber.Size = New System.Drawing.Size(118, 20)
        Me.Tbchamber.TabIndex = 26
        Me.Tbchamber.TextAlign = System.Windows.Forms.HorizontalAlignment.Right
        '
        'Label6
        '
        Me.Label6.AutoSize = True
        Me.Label6.ImeMode = System.Windows.Forms.ImeMode.NoControl
        Me.Label6.Location = New System.Drawing.Point(10, 182)
        Me.Label6.Name = "Label6"
        Me.Label6.Size = New System.Drawing.Size(29, 13)
        Me.Label6.TabIndex = 25
        Me.Label6.Text = "Cells"
        '
        'Label5
        '
        Me.Label5.AutoSize = True
        Me.Label5.ImeMode = System.Windows.Forms.ImeMode.NoControl
        Me.Label5.Location = New System.Drawing.Point(327, 127)
        Me.Label5.Name = "Label5"
        Me.Label5.Size = New System.Drawing.Size(15, 13)
        Me.Label5.TabIndex = 24
        Me.Label5.Text = "m"
        '
        'Label4
        '
        Me.Label4.AutoSize = True
        Me.Label4.ImeMode = System.Windows.Forms.ImeMode.NoControl
        Me.Label4.Location = New System.Drawing.Point(327, 101)
        Me.Label4.Name = "Label4"
        Me.Label4.Size = New System.Drawing.Size(15, 13)
        Me.Label4.TabIndex = 23
        Me.Label4.Text = "m"
        '
        'Label3
        '
        Me.Label3.AutoSize = True
        Me.Label3.ImeMode = System.Windows.Forms.ImeMode.NoControl
        Me.Label3.Location = New System.Drawing.Point(327, 75)
        Me.Label3.Name = "Label3"
        Me.Label3.Size = New System.Drawing.Size(20, 13)
        Me.Label3.TabIndex = 22
        Me.Label3.Text = "Pa"
        '
        'TbFiLe
        '
        Me.TbFiLe.Location = New System.Drawing.Point(192, 127)
        Me.TbFiLe.Name = "TbFiLe"
        Me.TbFiLe.Size = New System.Drawing.Size(118, 20)
        Me.TbFiLe.TabIndex = 21
        Me.TbFiLe.TextAlign = System.Windows.Forms.HorizontalAlignment.Right
        '
        'TbIdfib
        '
        Me.TbIdfib.Location = New System.Drawing.Point(192, 101)
        Me.TbIdfib.Name = "TbIdfib"
        Me.TbIdfib.Size = New System.Drawing.Size(118, 20)
        Me.TbIdfib.TabIndex = 20
        Me.TbIdfib.TextAlign = System.Windows.Forms.HorizontalAlignment.Right
        '
        'TbNfb
        '
        Me.TbNfb.Location = New System.Drawing.Point(192, 153)
        Me.TbNfb.Name = "TbNfb"
        Me.TbNfb.Size = New System.Drawing.Size(118, 20)
        Me.TbNfb.TabIndex = 19
        Me.TbNfb.TextAlign = System.Windows.Forms.HorizontalAlignment.Right
        '
        'Tbpp
        '
        Me.Tbpp.Location = New System.Drawing.Point(192, 75)
        Me.Tbpp.Name = "Tbpp"
        Me.Tbpp.Size = New System.Drawing.Size(118, 20)
        Me.Tbpp.TabIndex = 18
        Me.Tbpp.TextAlign = System.Windows.Forms.HorizontalAlignment.Right
        '
        'fiblen
        '
        Me.fiblen.AutoSize = True
        Me.fiblen.ImeMode = System.Windows.Forms.ImeMode.NoControl
        Me.fiblen.Location = New System.Drawing.Point(11, 130)
        Me.fiblen.Name = "fiblen"
        Me.fiblen.Size = New System.Drawing.Size(62, 13)
        Me.fiblen.TabIndex = 17
        Me.fiblen.Text = "Fiber length"
        '
        'Idfi
        '
        Me.Idfi.AutoSize = True
        Me.Idfi.ImeMode = System.Windows.Forms.ImeMode.NoControl
        Me.Idfi.Location = New System.Drawing.Point(9, 101)
        Me.Idfi.Name = "Idfi"
        Me.Idfi.Size = New System.Drawing.Size(49, 13)
        Me.Idfi.TabIndex = 16
        Me.Idfi.Text = "ID Fibers"
        '
        'nfib
        '
        Me.nfib.AutoSize = True
        Me.nfib.ImeMode = System.Windows.Forms.ImeMode.NoControl
        Me.nfib.Location = New System.Drawing.Point(9, 156)
        Me.nfib.Name = "nfib"
        Me.nfib.Size = New System.Drawing.Size(56, 13)
        Me.nfib.TabIndex = 15
        Me.nfib.Text = "No° Fibers"
        '
        'lblFlowmode
        '
        Me.lblFlowmode.AutoSize = True
        Me.lblFlowmode.Location = New System.Drawing.Point(9, 49)
        Me.lblFlowmode.Name = "lblFlowmode"
        Me.lblFlowmode.Size = New System.Drawing.Size(59, 13)
        Me.lblFlowmode.TabIndex = 2
        Me.lblFlowmode.Text = "Flow Mode"
        '
        'cbFlowMode
        '
        Me.cbFlowMode.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cbFlowMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cbFlowMode.FormattingEnabled = True
        Me.cbFlowMode.Items.AddRange(New Object() {"Counter-Current", "Co-Current", "Cross Flow"})
        Me.cbFlowMode.Location = New System.Drawing.Point(193, 46)
        Me.cbFlowMode.Name = "cbFlowMode"
        Me.cbFlowMode.Size = New System.Drawing.Size(158, 21)
        Me.cbFlowMode.TabIndex = 3
        '
        'TabControl1
        '
        Me.TabControl1.Controls.Add(Me.TabPageParams)
        Me.TabControl1.Controls.Add(Me.TabInclude)
        Me.TabControl1.Controls.Add(Me.TabPagePermeances)
        Me.TabControl1.Location = New System.Drawing.Point(12, 261)
        Me.TabControl1.Name = "TabControl1"
        Me.TabControl1.SelectedIndex = 0
        Me.TabControl1.Size = New System.Drawing.Size(386, 283)
        Me.TabControl1.TabIndex = 16
        '
        'TabPageParams
        '
        Me.TabPageParams.Controls.Add(Me.GroupBox2)
        Me.TabPageParams.Location = New System.Drawing.Point(4, 22)
        Me.TabPageParams.Name = "TabPageParams"
        Me.TabPageParams.Padding = New System.Windows.Forms.Padding(3)
        Me.TabPageParams.Size = New System.Drawing.Size(378, 257)
        Me.TabPageParams.TabIndex = 0
        Me.TabPageParams.Text = "Parameters"
        Me.TabPageParams.UseVisualStyleBackColor = True
        '
        'TabInclude
        '
        Me.TabInclude.Controls.Add(Me.ListViewCompounds)
        Me.TabInclude.Location = New System.Drawing.Point(4, 22)
        Me.TabInclude.Name = "TabInclude"
        Me.TabInclude.Padding = New System.Windows.Forms.Padding(3)
        Me.TabInclude.Size = New System.Drawing.Size(378, 257)
        Me.TabInclude.TabIndex = 2
        Me.TabInclude.Text = "Compounds"
        Me.TabInclude.UseVisualStyleBackColor = True
        '
        'ListViewCompounds
        '
        Me.ListViewCompounds.CheckBoxes = True
        Me.ListViewCompounds.HideSelection = False
        Me.ListViewCompounds.Location = New System.Drawing.Point(0, 0)
        Me.ListViewCompounds.Name = "ListViewCompounds"
        Me.ListViewCompounds.Size = New System.Drawing.Size(378, 257)
        Me.ListViewCompounds.TabIndex = 0
        Me.ListViewCompounds.UseCompatibleStateImageBehavior = False
        Me.ListViewCompounds.View = System.Windows.Forms.View.List
        '
        'TabPagePermeances
        '
        Me.TabPagePermeances.Controls.Add(Me.gridPermeance)
        Me.TabPagePermeances.Location = New System.Drawing.Point(4, 22)
        Me.TabPagePermeances.Name = "TabPagePermeances"
        Me.TabPagePermeances.Padding = New System.Windows.Forms.Padding(3)
        Me.TabPagePermeances.Size = New System.Drawing.Size(378, 257)
        Me.TabPagePermeances.TabIndex = 1
        Me.TabPagePermeances.Text = "Permeances"
        Me.TabPagePermeances.UseVisualStyleBackColor = True
        '
        'gridPermeance
        '
        Me.gridPermeance.AllowUserToAddRows = False
        Me.gridPermeance.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill
        Me.gridPermeance.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize
        Me.gridPermeance.Columns.AddRange(New System.Windows.Forms.DataGridViewColumn() {Me.Compound, Me.Permeance})
        Me.gridPermeance.Location = New System.Drawing.Point(3, 6)
        Me.gridPermeance.Name = "gridPermeance"
        Me.gridPermeance.RowHeadersVisible = False
        Me.gridPermeance.Size = New System.Drawing.Size(372, 248)
        Me.gridPermeance.TabIndex = 0
        '
        'Compound
        '
        Me.Compound.HeaderText = "Compund"
        Me.Compound.Name = "Compound"
        '
        'Permeance
        '
        Me.Permeance.HeaderText = "Permeance [SI]"
        Me.Permeance.Name = "Permeance"
        '
        'TabControl2
        '
        Me.TabControl2.Controls.Add(Me.Results)
        Me.TabControl2.Controls.Add(Me.TabPage2)
        Me.TabControl2.Location = New System.Drawing.Point(12, 559)
        Me.TabControl2.Name = "TabControl2"
        Me.TabControl2.SelectedIndex = 0
        Me.TabControl2.Size = New System.Drawing.Size(389, 253)
        Me.TabControl2.TabIndex = 17
        '
        'Results
        '
        Me.Results.Controls.Add(Me.gridResults)
        Me.Results.Location = New System.Drawing.Point(4, 22)
        Me.Results.Name = "Results"
        Me.Results.Padding = New System.Windows.Forms.Padding(3)
        Me.Results.Size = New System.Drawing.Size(381, 227)
        Me.Results.TabIndex = 0
        Me.Results.Text = "Results"
        Me.Results.UseVisualStyleBackColor = True
        '
        'gridResults
        '
        Me.gridResults.AllowUserToAddRows = False
        Me.gridResults.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill
        Me.gridResults.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize
        Me.gridResults.Columns.AddRange(New System.Windows.Forms.DataGridViewColumn() {Me.DataGridViewTextBoxColumn1, Me.DataGridViewTextBoxColumn2})
        Me.gridResults.Location = New System.Drawing.Point(3, 3)
        Me.gridResults.Name = "gridResults"
        Me.gridResults.RowHeadersVisible = False
        Me.gridResults.Size = New System.Drawing.Size(375, 221)
        Me.gridResults.TabIndex = 18
        '
        'DataGridViewTextBoxColumn1
        '
        Me.DataGridViewTextBoxColumn1.HeaderText = "Result"
        Me.DataGridViewTextBoxColumn1.Name = "DataGridViewTextBoxColumn1"
        '
        'DataGridViewTextBoxColumn2
        '
        Me.DataGridViewTextBoxColumn2.HeaderText = "Value"
        Me.DataGridViewTextBoxColumn2.Name = "DataGridViewTextBoxColumn2"
        '
        'TabPage2
        '
        Me.TabPage2.Controls.Add(Me.GroupBox4)
        Me.TabPage2.Location = New System.Drawing.Point(4, 22)
        Me.TabPage2.Name = "TabPage2"
        Me.TabPage2.Padding = New System.Windows.Forms.Padding(3)
        Me.TabPage2.Size = New System.Drawing.Size(381, 227)
        Me.TabPage2.TabIndex = 1
        Me.TabPage2.Text = "Notes"
        Me.TabPage2.UseVisualStyleBackColor = True
        '
        'Editor
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.AutoScroll = True
        Me.ClientSize = New System.Drawing.Size(413, 839)
        Me.Controls.Add(Me.TabControl2)
        Me.Controls.Add(Me.TabControl1)
        Me.Controls.Add(Me.GroupBox5)
        Me.Controls.Add(Me.GroupBox1)
        Me.Font = New System.Drawing.Font("Microsoft Sans Serif", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Name = "Editor"
        Me.ShowHint = WeifenLuo.WinFormsUI.Docking.DockState.DockLeft
        Me.Text = "Editor"
        Me.GroupBox1.ResumeLayout(False)
        Me.GroupBox1.PerformLayout()
        Me.GroupBox5.ResumeLayout(False)
        Me.GroupBox5.PerformLayout()
        Me.GroupBox4.ResumeLayout(False)
        Me.GroupBox2.ResumeLayout(False)
        Me.GroupBox2.PerformLayout()
        Me.TabControl1.ResumeLayout(False)
        Me.TabPageParams.ResumeLayout(False)
        Me.TabInclude.ResumeLayout(False)
        Me.TabPagePermeances.ResumeLayout(False)
        CType(Me.gridPermeance, System.ComponentModel.ISupportInitialize).EndInit()
        Me.TabControl2.ResumeLayout(False)
        Me.Results.ResumeLayout(False)
        CType(Me.gridResults, System.ComponentModel.ISupportInitialize).EndInit()
        Me.TabPage2.ResumeLayout(False)
        Me.ResumeLayout(False)

    End Sub

    Friend WithEvents Label1 As Windows.Forms.Label
    Friend WithEvents cbmembranemode As Windows.Forms.ComboBox
    Public WithEvents GroupBox1 As Windows.Forms.GroupBox
    Public WithEvents Label16 As Windows.Forms.Label
    Public WithEvents cbEnergy As Windows.Forms.ComboBox
    Public WithEvents Label7 As Windows.Forms.Label
    Public WithEvents cbOutlet2 As Windows.Forms.ComboBox
    Public WithEvents cbOutlet1 As Windows.Forms.ComboBox
    Public WithEvents Label2 As Windows.Forms.Label
    Public WithEvents cbInlet1 As Windows.Forms.ComboBox
    Public WithEvents Label8 As Windows.Forms.Label
    Public WithEvents GroupBox5 As Windows.Forms.GroupBox
    Public WithEvents lblTag As Windows.Forms.TextBox
    Public WithEvents lblStatus As Windows.Forms.Label
    Public WithEvents Label12 As Windows.Forms.Label
    Public WithEvents Label11 As Windows.Forms.Label
    Public WithEvents GroupBox4 As Windows.Forms.GroupBox
    Public WithEvents rtbAnnotations As Extended.Windows.Forms.RichTextBoxExtended
    Public WithEvents ppres As Windows.Forms.Label
    Public WithEvents GroupBox2 As Windows.Forms.GroupBox
    Friend WithEvents chkActive As Windows.Forms.CheckBox
    Friend WithEvents btnDisconnect1 As Windows.Forms.Button
    Friend WithEvents btnCreateAndConnectInlet1 As Windows.Forms.Button
    Friend WithEvents btnDisconnectEnergy As Windows.Forms.Button
    Friend WithEvents btnDisconnectOutlet2 As Windows.Forms.Button
    Friend WithEvents btnDisconnectOutlet1 As Windows.Forms.Button
    Friend WithEvents btnCreateAndConnectEnergy As Windows.Forms.Button
    Friend WithEvents btnCreateAndConnectOutlet2 As Windows.Forms.Button
    Friend WithEvents btnCreateAndConnectOutlet1 As Windows.Forms.Button
    Friend WithEvents lblFlowmode As Windows.Forms.Label
    Friend WithEvents cbFlowMode As Windows.Forms.ComboBox
    Friend WithEvents TabControl1 As Windows.Forms.TabControl
    Friend WithEvents TabPageParams As Windows.Forms.TabPage
    Friend WithEvents TabPagePermeances As Windows.Forms.TabPage
    Friend WithEvents gridPermeance As Windows.Forms.DataGridView
    Friend WithEvents TabInclude As Windows.Forms.TabPage
    Friend WithEvents ListViewCompounds As Windows.Forms.ListView
    Public WithEvents nfib As Windows.Forms.Label
    Public WithEvents fiblen As Windows.Forms.Label
    Public WithEvents Idfi As Windows.Forms.Label
    Friend WithEvents Tbpp As Windows.Forms.TextBox
    Friend WithEvents TbFiLe As Windows.Forms.TextBox
    Friend WithEvents TbIdfib As Windows.Forms.TextBox
    Friend WithEvents TbNfb As Windows.Forms.TextBox
    Public WithEvents Label3 As Windows.Forms.Label
    Public WithEvents Label5 As Windows.Forms.Label
    Public WithEvents Label4 As Windows.Forms.Label
    Friend WithEvents Tbchamber As Windows.Forms.TextBox
    Public WithEvents Label6 As Windows.Forms.Label
    Friend WithEvents Tbsc As Windows.Forms.TextBox
    Public WithEvents Label9 As Windows.Forms.Label
    Public WithEvents lblConnectedTo As Windows.Forms.Label
    Public WithEvents Label13 As Windows.Forms.Label
    Friend WithEvents TabControl2 As Windows.Forms.TabControl
    Friend WithEvents Results As Windows.Forms.TabPage
    Friend WithEvents TabPage2 As Windows.Forms.TabPage
    Friend WithEvents Compound As Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents Permeance As Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents gridResults As Windows.Forms.DataGridView
    Friend WithEvents DataGridViewTextBoxColumn1 As Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents DataGridViewTextBoxColumn2 As Windows.Forms.DataGridViewTextBoxColumn
End Class
