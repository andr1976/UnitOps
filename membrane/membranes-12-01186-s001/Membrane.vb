'This File was designed by the institue of chemical, environmental and bioscience engineering
'by the authors Kouessan Aziaba, Bahram Haddadi-Sisakht, Christian Jordan and Michael Harasek
'This File is based on the source code of various DWSIM unitoperations developed by Daniel Wagner Oliveira de Medeiros



Imports System.Math
Imports DWSIM.Thermodynamics
Imports DWSIM.Thermodynamics.BaseClasses
Imports DWSIM.Thermodynamics.Streams
Imports DWSIM.Thermodynamics.PropertyPackages
Imports DWSIM.Interfaces
Imports DWSIM.Interfaces.Enums
Imports DWSIM.Interfaces.Enums.GraphicObjects
Imports DWSIM.FlowsheetBase
Imports SkiaSharp.Views.Desktop.Extensions
Imports DWSIM.Drawing.SkiaSharp.GraphicObjects
Imports DWSIM.DrawingTools.Point
Imports DWSIM.ExtensionMethods
Imports DWSIM.UnitOperations.UnitOperations.Auxiliary
Imports DWSIM.UnitOperations.UnitOperations
Imports DWSIM.SharedClasses
Imports DWSIM.Thermodynamics.PropertyPackages.PropertyPackage



''' <summary>
''' Membrane Model
''' </summary>





<System.Serializable()> Public Class Membrane


    Inherits UnitOperations.UnitOpBaseClass

    Implements DWSIM.Interfaces.IExternalUnitOperation


    Public m_ResStageCut As Dictionary(Of String, Double)



    Private Property UOName As String = "Membrane"
    Private Property UODEscription As String = "Membrane Unit Operation"

    Dim N0 As New Dictionary(Of String, Double)
    Dim Colums, Rows As Integer

    Public Overrides Property ComponentName As String = UOName

    Public Overrides Property ComponentDescription As String = UODEscription

    Public Overrides Property ObjectClass As SimulationObjectClass = SimulationObjectClass.Separators

    Public Property Permeances As New Dictionary(Of String, Double)

    Public Property AllPermeances As New Dictionary(Of String, Double)

    Public Property PermeatePressure As Double = 100000.0

    Public Property NumberFibers As Double = 100.0

    Public Property InnerDiameterFibers As Double = 0.01

    Public Property FiberLength As Double = 0.1

    Public Property Chambers As Double = 1

    Public Property StageCut As Double = 0.2

    Public Property Components As New List(Of String)

    Public Property ActiveComponents As New List(Of String)

    Public Property NumActComponents As New Integer



    'tells DWSIM that this UO is compatible with mobile versions, (so do we need this?)
    Public Overrides ReadOnly Property MobileCompatible As Boolean = False

    Private ReadOnly Property IExternalUnitOperation_Name As String Implements Interfaces.IExternalUnitOperation.Name
        Get
            Return UOName
        End Get
    End Property

    Public ReadOnly Property Description As String Implements Interfaces.IExternalUnitOperation.Description
        Get
            Return UODEscription
        End Get
    End Property

    Public ReadOnly Property Prefix As String Implements Interfaces.IExternalUnitOperation.Prefix
        Get
            Return "MBRN-"
        End Get
    End Property


    Public Sub New(ByVal Name As String, ByVal Description As String)

        MyBase.CreateNew()
        Me.ComponentName = Name
        Me.ComponentDescription = Description
        Me.ActiveComponents = New List(Of String)
        Me.m_ResStageCut = New Dictionary(Of String, Double)
        Permeances = New Dictionary(Of String, Double)
        AllPermeances = New Dictionary(Of String, Double)
        ReDim CompoundTable(0, 0)


    End Sub



    Public Sub New()

        MyBase.New()
        'Permeances = New Dictionary(Of String, Double)
        'PermeatePressure = New Double


    End Sub


    <NonSerialized> <Xml.Serialization.XmlIgnore> Private editwindow As Editor

    Public Enum MembraneMode

        Gaspermeation = 0
        Pervaporation = 1
        SteamPermeation = 2


    End Enum

    Public Enum FlowDirection
        CounterCurrent = 0
        CoCurrent = 1
        Crossflow = 2
    End Enum

    Protected m_cmode As MembraneMode = MembraneMode.Gaspermeation
    Protected m_fmode As FlowDirection = FlowDirection.CounterCurrent
    Protected m_Area As Nullable(Of Double) = 1.0#
    'Protected m_sc As List(Of Double) = 


    Private _Compound_Table As Double(,) = New Double(,) {}
    Private _initialestimates As New List(Of Double)



    Public Overrides Sub DisplayEditForm()

        If editwindow Is Nothing Then

            editwindow = New Editor() With {.HObject = Me}
            editwindow.ShowHint = GlobalSettings.Settings.DefaultEditFormLocation
            editwindow.Tag = "ObjectEditor"
            Me.FlowSheet.DisplayForm(editwindow)


        Else

            If editwindow.IsDisposed Then
                editwindow = New Editor() With {.HObject = Me}
                editwindow.ShowHint = GlobalSettings.Settings.DefaultEditFormLocation
                editwindow.Tag = "ObjectEditor"
                Me.FlowSheet.DisplayForm(editwindow)
            Else
                editwindow.Activate()
            End If
        End If

        FlowSheet.DisplayForm(editwindow)

    End Sub

    Public Overrides Sub UpdateEditForm()

        If editwindow IsNot Nothing Then

            If editwindow.InvokeRequired Then

                editwindow.Invoke(Sub()
                                      editwindow?.UpdateInfo()
                                  End Sub)
            Else
                editwindow?.UpdateInfo()
            End If

        End If

    End Sub

    Public Overrides Sub CloseEditForm()

        'editwindow?.Close()

        If editwindow IsNot Nothing Then
            If Not editwindow.IsDisposed Then
                editwindow.Close()
                editwindow = Nothing
            End If
        End If

    End Sub

    Public Overrides Function GetDisplayName() As String
        Return UOName
    End Function

    Public Overrides Function GetDisplayDescription() As String
        Return UODEscription
    End Function

    Public Overrides Function GetIconBitmap() As Object
        Return My.Resources.TU_Signet_HD
    End Function


    'returns a new instance of membrane, using XML cloning
    Public Overrides Function CloneXML() As Object

        Dim objdata = XMLSerializer.XMLSerializer.Serialize(Me)
        Dim newmembrane As New Membrane()
        newmembrane.LoadData(objdata)

        Return newmembrane

    End Function

    'retuens a new instance of membrane, using JSON cloning
    Public Overrides Function CloneJSON() As Object

        Dim jsonstring = Newtonsoft.Json.JsonConvert.SerializeObject(Me)
        Dim newmembrane = Newtonsoft.Json.JsonConvert.DeserializeObject(Of Membrane)(jsonstring)

        Return newmembrane

    End Function


    'return a new instance of this UO
    Public Function ReturnInstance(typename As String) As Object Implements Interfaces.IExternalUnitOperation.ReturnInstance
        Return New Membrane()
    End Function

    Private Image As SkiaSharp.SKImage

    'this function draws the object on the flowsheet
    Public Sub Draw(g As Object) Implements Interfaces.IExternalUnitOperation.Draw

        Dim canvas = DirectCast(g, SkiaSharp.SKCanvas)

        If Image Is Nothing Then
            Using bitmap = My.Resources.TU_Signet_HD.ToSKBitmap()
                Image = SkiaSharp.SKImage.FromBitmap(bitmap)
            End Using
        End If

        Dim x = Me.GraphicObject.X
        Dim y = Me.GraphicObject.Y
        Dim w = Me.GraphicObject.Width
        Dim h = Me.GraphicObject.Height

        Using p As New SkiaSharp.SKPaint With {.FilterQuality = SkiaSharp.SKFilterQuality.High}
            canvas.DrawImage(Image, New SkiaSharp.SKRect(GraphicObject.X, GraphicObject.Y, GraphicObject.X + GraphicObject.Width, GraphicObject.Y + GraphicObject.Height), p)
        End Using


    End Sub


    Public Sub CreateConnectors() Implements IExternalUnitOperation.CreateConnectors

        Dim w, h, x, y As Double
        w = GraphicObject.Width
        h = GraphicObject.Height
        x = GraphicObject.X
        y = GraphicObject.Y

        Dim Port1 As New ConnectionPoint

        Port1.Position = New Point(x, y - h / 2)
        Port1.Type = ConType.ConIn
        Port1.Direction = ConDir.Right

        Dim Port2 As New ConnectionPoint
        Port2.Position = New Point(x + w / 2, y + h)
        Port2.Type = ConType.ConIn
        Port2.Direction = ConDir.Up
        Port2.Type = ConType.ConEn

        Dim Port3 As New ConnectionPoint
        Port3.Position = New Point(x + w, y + h * (1 / 3))
        Port3.Type = ConType.ConOut
        Port3.Direction = ConDir.Right


        Dim Port4 As New ConnectionPoint
        Port4.Position = New Point(x + w, y + h * (2 / 3))
        Port4.Type = ConType.ConOut
        Port4.Direction = ConDir.Right


        With GraphicObject.InputConnectors
            If .Count = 2 Then
                .Item(0).Position = New Point(x, y + h / 2)
                .Item(1).Position = New Point(x + w / 2, y + h)
            Else
                .Add(Port1)
                .Add(Port2)
            End If
            .Item(0).ConnectorName = "Feed"
            .Item(1).ConnectorName = "Heat Inlet"
        End With

        With GraphicObject.OutputConnectors
            If .Count = 2 Then
                .Item(0).Position = New Point(x + w, y + h * (1 / 3))
                .Item(1).Position = New Point(x + w, y + h * (2 / 3))
            Else
                .Add(Port3)
                .Add(Port4)
            End If
            .Item(0).ConnectorName = "Permeate"
            .Item(1).ConnectorName = "Retentate"
        End With

        Me.GraphicObject.EnergyConnector.Active = False

    End Sub


    Public Sub PopulateEditorPanel(container As Object) Implements Interfaces.IExternalUnitOperation.PopulateEditorPanel
        Throw New NotImplementedException()
    End Sub

    Public Property CompoundTable() As Double(,)

        Get
            Return _Compound_Table
        End Get
        Set(ByVal value As Double(,))
            _Compound_Table = value
        End Set

    End Property




    Public Function ToFlowSheet(value As Double()) As String
        Dim Line As String
        Line = ""
        For i = 0 To (value.Length - 1)
            Line = Line + "[" + CStr(value(i)) + "] "
        Next


        FlowSheet.ShowMessage(Line, IFlowsheet.MessageType.Other)
    End Function

    Public Function RefreshStream(Stream As MaterialStream, Temperature As Double, Pressure As Double, MolarFraction As Double(), MolarFlow As Double()) As MaterialStream

        Stream.ClearAllProps()
        Stream.SetMolarFlow(SumY(MolarFlow))
        Stream.SetOverallComposition(MolarFlow.ToArray)
        Stream.SetTemperature(Temperature)
        Stream.SetPressure(Pressure)
        Stream.SetFlashSpec("PT")
        Me.PropertyPackage.CurrentMaterialStream = Stream
        Stream.Calculate()
        Stream.Validate()

        Return Stream

    End Function

    Public Function InitStream() As MaterialStream

        Dim Value As MaterialStream
        Value = New MaterialStream()
        Me.FlowSheet.AddCompoundsToMaterialStream(Value)
        Value.SetFlowsheet(FlowSheet)
        Value.PropertyPackage = Me.PropertyPackage
        Value.SpecType = StreamSpec.Temperature_and_Pressure
        Value.ClearAllProps()

        Return Value

    End Function


    Public Function InitStageCut(PropEStageCut As Double, Compounds As Integer) As Double()
        Dim Value(Compounds - 1) As Double
        For I = 0 To Compounds - 1

            Value(I) = PropEStageCut

        Next

        Return Value

    End Function


    Public Function InitMolarFractionxr(d As Double()) As Double()

        Dim value(d.Length - 1) As Double
        For i = 0 To value.Length - 1
            value(i) = d(i)
        Next

        Return value

    End Function


    Public Function InitMolarFractionyi(Stream As MaterialStream, d As Double(), Perm As Double()) As Double()

        Select Case CalcMode
            Case Membrane.MembraneMode.Gaspermeation
                Dim value(d.Length - 1) As Double
                For i = 0 To value.Length - 1
                    value(i) = d(i)
                Next

                Return value

            Case Membrane.MembraneMode.Pervaporation
                Dim FluxesInits(Stream.GetNumCompounds - 1) As Double
                For I = 0 To (Stream.GetNumCompounds - 1)
                    FluxesInits(I) = d(I) * Perm(I)
                Next

                Dim Value(Stream.GetNumCompounds - 1) As Double
                For I = 0 To (Stream.GetNumCompounds - 1)
                    Value(I) = FluxesInits(I) / (SumY(FluxesInits))
                Next

                Return Value
            Case Membrane.MembraneMode.SteamPermeation
                Dim FluxesInits(Stream.GetNumCompounds - 1) As Double
                For I = 0 To (Stream.GetNumCompounds - 1)
                    FluxesInits(I) = d(I) * Perm(I)
                Next

                Dim Value(Stream.GetNumCompounds - 1) As Double
                For I = 0 To (Stream.GetNumCompounds - 1)
                    Value(I) = FluxesInits(I) / (SumY(FluxesInits))
                Next

                Return Value
        End Select



    End Function


    Public Function InitMolarFractionyp(d As Double()) As Double()

        Dim value(d.Length - 1) As Double
        For i = 0 To value.Length - 1
            value(i) = d(i)
        Next

        Return value

    End Function

    Public Function PassMolarComposition(MolFrac As Double()) As Double()

        Dim Value(MolFrac.Length - 1) As Double

        For I = 0 To MolFrac.Length - 1

            Value(I) = MolFrac(I)

        Next

        Return Value

    End Function



    Public Function PartialPressure(Stream As MaterialStream) As Double()
        'get pressure and molarfrac from stream, apply Law of Dalton

        Dim molarFraction As Double() = Stream.GetPhaseComposition(0)
        Dim Pressure As Double = Stream.GetPressure()
        Dim Value(molarFraction.Length - 1) As Double

        For i = 0 To molarFraction.Length - 1
            Value(i) = Pressure * molarFraction(i)
        Next

        Return Value


    End Function


    Public Function PartialPressure(MolFrac As Double(), Pressure As Double) As Double()

        Dim Value(MolFrac.Length - 1) As Double
        For I = 0 To MolFrac.Length - 1

            Value(I) = Pressure * MolFrac(I)

        Next

        Return Value

    End Function

    Public Function BinaryAlpha(Perm As Double()) As Double()

        Dim Value(1) As Double
        Value(0) = Perm(0) / Perm(1)
        Value(1) = Perm(1) / Perm(0)

        Return Value

    End Function

    Public Function GetStreamVaporPressure(Stream As MaterialStream)

        Dim value(Stream.GetNumCompounds - 1) As Double
        Dim Temperature As Double
        Temperature = Stream.GetTemperature()
        Dim Ids(Stream.GetNumComponents - 1) As String

        For I = 0 To (Stream.GetNumComponents - 1)
            Ids(I) = Stream.ComponentIds(I).ToString
            value(I) = Stream.PropertyPackage.AUX_PVAPi(Stream.PropertyPackage.CurrentMaterialStream.Phases(0).Compounds(Ids(I)).ConstantProperties.Name, Stream.GetTemperature())
        Next

        'value = Stream.GetTDependentProperty(Stream.GetTDependentPropList(4), Temperature, Stream.ComponentIds, )

        Return value

    End Function

    Public Function ActivityCoefficient(Stream As MaterialStream)

        Dim Value(Stream.GetNumCompounds - 1) As Double
        For I = 0 To (Stream.GetNumCompounds - 1)
            'Value(I) = Stream.GetProp("activityCoefficient", "Liquid", "UNDEFINED", "Mixture", "Mass")
            Value(I) = Stream.Phases(3).Compounds(Stream.ComponentIds(I)).ActivityCoeff.GetValueOrDefault
        Next

        Return Value

    End Function


    Public Function FugacityCoefficient(Stream As MaterialStream)

        Dim Value(Stream.GetNumCompounds - 1) As Double
        For I = 0 To (Stream.GetNumCompounds - 1)
            Value(I) = Stream.Phases(2).Compounds(Stream.ComponentIds(I)).FugacityCoeff.GetValueOrDefault
        Next

        Return Value

    End Function


    Public Function CalcPartialPressure(Stream As MaterialStream, MolarFraction As Double())

        Dim Value(Stream.GetNumCompounds - 1) As Double
        Dim ACL1(Stream.GetNumCompounds - 1) As Double
        Dim FCV(Stream.GetNumCompounds - 1) As Double
        Dim VaporP(Stream.GetNumCompounds - 1) As Double

        FCV = FugacityCoefficient(Stream)
        ACL1 = ActivityCoefficient(Stream)
        VaporP = GetStreamVaporPressure(Stream)

        ToFlowSheet(FCV)

        For I = 0 To (Stream.GetNumCompounds - 1)
            Value(I) = ((ACL1(I) * MolarFraction(I) * VaporP(I))) '/ FCV(I)               
        Next

        Return Value

    End Function


    Public Function CalcEffectiveVaporMolarFlow(Stream As MaterialStream)

        Dim Value(Stream.GetNumCompounds - 1) As Double



        Value = Stream.GetProp("flow", Stream.Phases(2).ToString, Nothing, "Mixture", "mole")

        Return Value

    End Function


    Public Function calcEffectiveMolarFlow(Stream As MaterialStream)

        Dim Value(Stream.GetNumCompounds - 1), MolarFraction(Stream.GetNumCompounds - 1), MolarComponentFlow(Stream.GetNumCompounds - 1) As Double
        Dim MolarFlow As Double = Stream.GetMolarFlow
        MolarFraction = Stream.GetPhaseComposition(0)

        Select Case CalcMode
            Case Membrane.MembraneMode.Gaspermeation

                For i = 0 To (Stream.GetNumCompounds - 1)
                    MolarComponentFlow(i) = MolarFraction(i) * MolarFlow
                Next

                For I = 0 To (Stream.GetNumCompounds - 1)

                    Value(I) = MolarComponentFlow(I)

                Next
                Return Value
            Case Membrane.MembraneMode.Pervaporation
                Value = Stream.GetProp("flow", Stream.Phases(0).ToString, Nothing, "Mixture", "mole")
                Return Value
            Case Membrane.MembraneMode.SteamPermeation
                Value = Stream.GetProp("flow", Stream.Phases(0).ToString, Nothing, "Mixture", "mole")
                Return Value
        End Select


        'Value = Stream.GetProp("flow", Stream.Phases(0).ToString, Nothing, "Mixture", "mole")
        'Return Value
    End Function



    Public Function PressureRatio(Stream As MaterialStream, OutputPressure As Double) As Double()

        Select Case CalcMode
            Case Membrane.MembraneMode.Gaspermeation
                Dim PressureVector(Stream.GetNumComponents - 1) As Double
                For I = 0 To (Stream.GetNumCompounds - 1)
                    PressureVector(I) = Stream.GetPressure() / OutputPressure
                Next
                Return PressureVector
            Case Membrane.MembraneMode.Pervaporation
                Dim PressureVector(Stream.GetNumComponents - 1) As Double
                Dim StreamVaporPressure(Stream.GetNumCompounds - 1) As Double
                StreamVaporPressure = GetStreamVaporPressure(Stream)
                For I = 0 To (Stream.GetNumCompounds - 1)
                    PressureVector(I) = StreamVaporPressure(I) / OutputPressure
                Next
                Return PressureVector
            Case Membrane.MembraneMode.SteamPermeation
                Dim PressureVector(Stream.GetNumComponents - 1) As Double
                For I = 0 To (Stream.GetNumCompounds - 1)
                    PressureVector(I) = GetStreamVaporPressure(Stream)(I) / OutputPressure
                Next
                Return PressureVector
        End Select

        Return Nothing

    End Function


    Public Function yiRetentateEndBinary(RetMolFrac As Double(), IdealSel As Double(), ratio As Double())
        Dim A(RetMolFrac.Length - 1), B(RetMolFrac.Length - 1), C(RetMolFrac.Length - 1), Value(RetMolFrac.Length - 1) As Double
        For I = 0 To RetMolFrac.Length - 1
            A(I) = IdealSel(I) - 1
            B(I) = ((IdealSel(I) - 1) * ((ratio(I) * RetMolFrac(I)) + 1)) + ratio(I)
            C(I) = IdealSel(I) * ratio(I) * RetMolFrac(I)
            Value(I) = (B(I) - Sqrt((B(I) * B(I)) - (4 * A(I) * C(I)))) / (2 * A(I))
        Next
    End Function

    Public Function yiRetentateMultiInitds1(RetMolFrac As Double(), Perm As Double()) As Double()

        Dim FluxesInit(RetMolFrac.Length - 1) As Double
        For I = 0 To RetMolFrac.Length - 1

            FluxesInit(I) = RetMolFrac(I) * Perm(I)

        Next

        Dim Value(RetMolFrac.Length - 1) As Double
        For I = 0 To RetMolFrac.Length - 1

            Value(I) = FluxesInit(I) / AbsSumY(FluxesInit)

        Next


    End Function

    Public Function yiRetentateMultids1(RetMolFrac As Double(), MolFracLastIt As Double(), ratio As Double(), Perm As Double()) As Double()
        Dim A(RetMolFrac.Length - 1), B(RetMolFrac.Length - 1), C(RetMolFrac.Length - 1), Value(RetMolFrac.Length - 1) As Double
        For I = 0 To RetMolFrac.Length - 1
            A(I) = Perm(I)
            B(I) = 0

            For J = 0 To RetMolFrac.Length - 1
                If I = J Then

                    B(I) -= Perm(J) * ((ratio(J) * RetMolFrac(J)) + 1)

                Else

                    B(I) += Perm(J) * (MolFracLastIt(J) - (ratio(J) * RetMolFrac(J)))

                End If

            Next
            C(I) = Perm(I) * ratio(I) * RetMolFrac(I)
            Value(I) = (B(I) + Sqrt((B(I) * B(I)) + (4 * A(I) * C(I)))) / (2 * A(I))
        Next

        Return Value
    End Function

    Public Function yiRetentateMultids2(RetMolFrac As Double(), MolFracLastIt As Double(), ratio As Double(), Perm As Double()) As Double()


        'ToFlowSheet(Perm)

        Dim A(RetMolFrac.Length - 1), B(RetMolFrac.Length - 1), C(RetMolFrac.Length - 1), Value(RetMolFrac.Length - 1), ValueRaw(RetMolFrac.Length - 1) As Double

        For I = 0 To RetMolFrac.Length - 1


            'FlowSheet.ShowMessage(Perm(I).ToString, IFlowsheet.MessageType.Other)
            A(I) = Perm(I) * RetMolFrac(I) * ratio(I) * (SumY(MolFracLastIt) - MolFracLastIt(I))
            B(I) = 0



            For J = 0 To RetMolFrac.Length - 1
                If J = I Then

                    B(I) += 0

                Else

                    B(I) += Perm(J) * ((RetMolFrac(J) * ratio(J)) - MolFracLastIt(J))

                End If

            Next
            C(I) = Perm(I) * (SumY(MolFracLastIt) - MolFracLastIt(I))
            ValueRaw(I) = A(I) / (B(I) + C(I))
        Next

        'ToFlowSheet(A)
        'ToFlowSheet(B)
        'ToFlowSheet(C)

        Dim SumRaw As Double = SumY(ValueRaw)

        For I = 0 To RetMolFrac.Length - 1
            Value(I) = ValueRaw(I) / SumRaw
        Next

        Return Value


    End Function


    Public Function yfMultiInitCocurrent(FeedMolFrac As Double(), Perm As Double()) As Double()

        Dim FluxesInit(FeedMolFrac.Length - 1) As Double
        For I = 0 To FeedMolFrac.Length - 1

            FluxesInit(I) = FeedMolFrac(I) * Perm(I)

        Next

        Dim Value(FeedMolFrac.Length - 1) As Double
        For I = 0 To FeedMolFrac.Length - 1

            Value(I) = FluxesInit(I) / AbsSumY(FluxesInit)

        Next

        Return Value

    End Function


    Public Function yfMultiCocurrent(FeedMolFrac As Double(), MolFracLastIt As Double(), ratio As Double(), Perm As Double()) As Double()

        Dim A(FeedMolFrac.Length - 1), B(FeedMolFrac.Length - 1), C(FeedMolFrac.Length - 1), Value(FeedMolFrac.Length - 1), ValueRaw(FeedMolFrac.Length - 1) As Double

        For I = 0 To FeedMolFrac.Length - 1
            A(I) = Perm(I) * FeedMolFrac(I) * ratio(I) * (SumY(MolFracLastIt) - MolFracLastIt(I))
            B(I) = 0

            For J = 0 To FeedMolFrac.Length - 1
                If J = I Then

                    B(I) += 0

                Else

                    B(I) += Perm(J) * ((FeedMolFrac(J) * ratio(J)) - MolFracLastIt(J))

                End If

            Next
            C(I) = Perm(I) * (SumY(MolFracLastIt) - MolFracLastIt(I))
            ValueRaw(I) = A(I) / (B(I) + C(I))
        Next

        'ToFlowSheet(A)

        Dim SumRaw As Double = SumY(ValueRaw)

        For I = 0 To FeedMolFrac.Length - 1
            Value(I) = ValueRaw(I) / SumRaw
        Next

        Return Value


    End Function


    Public Function yfMultiCross(FeedMolFrac As Double(), RetMolFrac As Double(), MolFracLastIt As Double(), ratio As Double(), Perm As Double()) As Double()

        Dim A(FeedMolFrac.Length - 1), B(FeedMolFrac.Length - 1), C(FeedMolFrac.Length - 1), X(FeedMolFrac.Length - 1), Value(FeedMolFrac.Length - 1), ValueRaw(FeedMolFrac.Length - 1) As Double



        For I = 0 To FeedMolFrac.Length - 1
            X(I) = (FeedMolFrac(I) + RetMolFrac(I)) / 2
        Next

        For I = 0 To FeedMolFrac.Length - 1
            A(I) = Perm(I) * FeedMolFrac(I) * ratio(I) * (SumY(MolFracLastIt) - MolFracLastIt(I))
            B(I) = 0

            For J = 0 To FeedMolFrac.Length - 1
                If J = I Then

                    B(I) += 0

                Else

                    B(I) += Perm(J) * ((X(J) * ratio(J)) - MolFracLastIt(J))

                End If

            Next
            C(I) = Perm(I) * (SumY(MolFracLastIt) - MolFracLastIt(I))
            ValueRaw(I) = A(I) / (B(I) + C(I))
        Next

        'ToFlowSheet(A)

        Dim SumRaw As Double = SumY(ValueRaw)

        For I = 0 To FeedMolFrac.Length - 1
            Value(I) = ValueRaw(I) / SumRaw
        Next

        Return Value


    End Function


    Public Function xrRetentateMultiInit(FeedMolFlux As Double(), RetMolFrac As Double(), PressureF As Double, Pressurep As Double, Perm As Double()) As Double()

        Dim yLightInit(FeedMolFlux.Length - 1) As Double
        For I = 0 To FeedMolFlux.Length - 1

            yLightInit(I) = FeedMolFlux(I) * Perm(I)
        Next

        'Dim yperm1(FeedMolFrac.Length - 1) As Double
        'Dim pyperm(FeedMolFrac.Length - 1) As Double
        'For I = 0 To FeedMolFrac.Length - 1

        '    yperm1(I) = yLightInit(I) / AbsSumY(yLightInit)
        '    pyperm(I) = yperm1(I) * Pressurep
        'Next

        'Dim DrivFor(FeedMolFrac.Length - 1) As Double
        'For I = 0 To FeedMolFrac.Length - 1


        '    'DrivFor((I) = FeedMolFrac(I) * Perm(I)
        '    DrivFor(I) = Perm(I) * ((((FeedMolFrac(I) - RetMolFrac(I)) / Log(FeedMolFrac(I) / RetMolFrac(I))) * PressureF) - pyperm(I))
        'Next



        'Dim yPermGuess(FeedMolFrac.Length - 1) As Double
        'For I = 0 To FeedMolFrac.Length - 1

        '    yPermGuess(I) = DrivFor(I) / AbsSumY(DrivFor)

        'Next

        Dim retFlux(FeedMolFlux.Length - 1) As Double
        For I = 0 To FeedMolFlux.Length - 1

            retFlux(I) = FeedMolFlux(I) - yLightInit(I)

        Next

        Dim value(FeedMolFlux.Length - 1) As Double
        For I = 0 To FeedMolFlux.Length - 1

            value(I) = retFlux(I) / AbsSumY(retFlux)

        Next

        Return value

    End Function


    Public Function GetCp(Stream As MaterialStream)

        Dim Value(Stream.GetNumCompounds - 1) As Double
        Dim Temperature As Double = Stream.GetTemperature()

        For I = 0 To (Stream.GetNumCompounds - 1)
            Value(I) = Stream.PropertyPackage.AUX_LIQ_Cpi(Stream.Phases(0).Compounds(Stream.ComponentIds(I)).ConstantProperties, Temperature) * Stream.Phases(0).Compounds(Stream.ComponentIds(I)).ConstantProperties.Molar_Weight
        Next

        Return Value

    End Function

    Public Function GetHv(Stream As MaterialStream)
        Dim Value(Stream.GetNumCompounds) As Double
        Dim Temperature As Double = Stream.GetTemperature()

        For I = 0 To (Stream.GetNumCompounds - 1)
            Value(I) = Stream.PropertyPackage.AUX_HVAPi(Stream.ComponentIds(I), Temperature) * Stream.Phases(0).Compounds(Stream.ComponentIds(I)).ConstantProperties.Molar_Weight
        Next

        Return Value

    End Function

    Public Function GetDewPoint(Stream As MaterialStream)

        Dim Value As Double

        Value = Stream.Phases(0).Properties.dewTemperature.GetValueOrDefault

        Return Value

    End Function


    Public Overrides Function LoadData(data As List(Of XElement)) As Boolean

        If Permeances Is Nothing Then Permeances = New Dictionary(Of String, Double)
        'If ResStageCuts Is Nothing Then ResStageCuts = New Dictionary(Of String, Double)

        XMLSerializer.XMLSerializer.Deserialize(Me, data)

        Dim ci As Globalization.CultureInfo = Globalization.CultureInfo.InvariantCulture


        Me.Permeances.Clear()
        For Each xel As XElement In (From xel2 As XElement In data Select xel2 Where xel2.Name = "Permeances").Elements.ToList
            If Not Permeances.ContainsKey(xel.@Key) Then Me.Permeances.Add(xel.@Key, Double.Parse(xel.@Value, ci))
        Next


        Return True
    End Function

    'Public Overrides Function LoadData(data As System.Collections.Generic.List(Of System.Xml.Linq.XElement)) As Boolean

    '    XMLSerializer.XMLSerializer.Deserialize(Me, data)

    '    Return True

    'End Function

    Public Overrides Function savedata() As List(Of XElement)

        Dim elements As List(Of System.Xml.Linq.XElement) = MyBase.SaveData()
        Dim ci As Globalization.CultureInfo = Globalization.CultureInfo.InvariantCulture

        With elements

            .Add(New XElement("permeances"))
            For Each p In Permeances
                .Item(.Count - 1).Add(New XElement("variable", New XAttribute("key", p.Key), New XAttribute("value", p.Value.ToString(ci))))
            Next

            '.Add(New XElement("resstagecuts"))
            'For Each s In ResStageCuts
            '    .Item(.Count - 1).Add(New XElement("resstagecut", New XAttribute("key", s.Key), New XAttribute("value", s.Value.ToString(ci))))
            'Next

        End With

        Return elements

    End Function


    'Public Overrides Function SaveData() As System.Collections.Generic.List(Of System.Xml.Linq.XElement)

    '    Return XMLSerializer.XMLSerializer.Serialize(Me)

    'End Function

    Public Property CalcMode() As MembraneMode
        Get
            Return m_cmode
        End Get
        Set(ByVal value As MembraneMode)
            m_cmode = value
        End Set
    End Property

    Public Property FlowMode() As FlowDirection
        Get
            Return m_fmode
        End Get
        Set(ByVal value As FlowDirection)
            m_fmode = value
        End Set
    End Property

    Public Property Area() As Nullable(Of Double)
        Get
            Return m_Area
        End Get
        Set(ByVal value As Nullable(Of Double))
            m_Area = value
        End Set
    End Property


    <Xml.Serialization.XmlIgnore()> Public ReadOnly Property ResStageCuts() As Dictionary(Of String, Double)
        Get
            Return Me.m_ResStageCut
        End Get
    End Property

    Public Overrides Function GetPropertyValue(ByVal prop As String, Optional ByVal su As Interfaces.IUnitsOfMeasure = Nothing) As Object


        If su Is Nothing Then su = New SystemsOfUnits.SI()
        Dim val0 As Object = MyBase.GetPropertyValue(prop, su)

        If Not val0 Is Nothing Then
            Return val0

        Else

            Dim value As Double
            Dim cv As New SystemsOfUnits.Converter

            If prop.Equals("Permeate Pressure") Then
                Return SystemsOfUnits.Converter.ConvertFromSI(su.pressure, Me.PermeatePressure)
            ElseIf prop.Equals("Inner diameter of fibers") Then
                Return SystemsOfUnits.Converter.ConvertFromSI(su.diameter, Me.InnerDiameterFibers)
            ElseIf prop.Equals("Fiber length") Then
                Return SystemsOfUnits.Converter.ConvertFromSI(su.distance, Me.FiberLength)
            ElseIf prop.Equals("Number of fibers") Then
                Return Me.NumberFibers
            ElseIf prop.Equals("Number of chambers") Then
                Return Me.Chambers


            ElseIf prop.Equals("Calculation Mode") Then


                'Select Case prop
                'Case "Calculation Mode"
                Select Case CalcMode
                    Case MembraneMode.Gaspermeation
                        Return "Gas Permeation"
                    Case MembraneMode.Pervaporation
                        Return "Pervaporation"
                    Case MembraneMode.SteamPermeation
                        Return "Steam Permeation"
                End Select

            ElseIf prop.Equals("Flow Mode") Then
                'Case "Flow Mode"
                Select Case FlowMode
                    Case FlowDirection.CoCurrent
                        Return "Co-Current"
                    Case FlowDirection.CounterCurrent
                        Return "Counter-Current"
                    Case FlowDirection.Crossflow
                        Return "Crossflow"
                End Select

            ElseIf prop.Equals("Participating Components") Then


                '  Case "Participating Components"
                value = Me.NumActComponents
                'End Select


            End If

            If Me.Permeances.ContainsKey(prop) Then Return Me.Permeances(prop)
            '    Return Nothing
        End If

    End Function

    Public Overloads Overrides Function GetProperties(ByVal proptype As Interfaces.Enums.PropertyType) As String()

        Dim i As Integer = 0
        Dim proplist As New ArrayList
        Dim basecol = MyBase.GetProperties(proptype)
        If basecol.Length > 0 Then proplist.AddRange(basecol)
        Select Case proptype
            'Case PropertyType.RW
            '    'For i = 0 To 4
            '    '    proplist.Add("PROP_MB_" + CStr(i))
            '    'Next
            '    proplist.Add("Permeate Pressure")
            '    proplist.Add("Inner diameter of fiber")
            '    proplist.Add("fiber length")
            '    proplist.Add("Number of fibers")
            '    proplist.Add("Number of chambers")
            Case PropertyType.WR
                'For i = 0 To 4
                '    proplist.Add("PROP_MB_" + CStr(i))
                'Next
                proplist.Add("Permeate Pressure")
                proplist.Add("Inner diameter of fiber")
                proplist.Add("fiber length")
                proplist.Add("Number of fibers")
                proplist.Add("Number of chambers")
            Case PropertyType.ALL
                'For i = 0 To 4
                '    proplist.Add("PROP_MB_" + CStr(i))
                'Next
                'proplist.Add("Permeate Pressure")
                'proplist.Add("Inner diameter of fiber")
                'proplist.Add("fiber length")
                'proplist.Add("Number of fibers")
                'proplist.Add("Number of chambers")
                proplist.Add("Calculation Mode")
                proplist.Add("Flow Mode")
                proplist.Add("Participating Components")
                For Each item In Permeances
                    proplist.Add(item.Key + ": Permeance")
                Next
        End Select

        Return proplist.ToArray(GetType(System.String))
        proplist = Nothing


    End Function


    Public Overrides Function GetPropertyUnit(ByVal prop As String, Optional ByVal su As Interfaces.IUnitsOfMeasure = Nothing) As String
        Dim u0 As String = MyBase.GetPropertyUnit(prop, su)

        If u0 <> "NF" Then
            Return u0
        Else
            If su Is Nothing Then su = New SystemsOfUnits.SI
            Dim cv As New SystemsOfUnits.Converter
            Dim value As String = ""


            If prop.Equals("Permeate Pressure") Then
                Return su.pressure
            ElseIf prop.Equals("Inner diameter of fibers") Then
                Return su.diameter
            ElseIf prop.Equals("Fiber length") Then
                Return su.distance
            ElseIf prop.Equals("Number of fibers") Then
                Return ""
            ElseIf prop.Equals("Number of chambers") Then
                Return ""
            ElseIf prop.Equals("Calculation Mode") Then
                Select Case CalcMode
                    Case MembraneMode.Gaspermeation
                        Return ""
                    Case MembraneMode.Pervaporation
                        Return ""
                    Case MembraneMode.SteamPermeation
                        Return ""
                End Select
            ElseIf prop.Equals("Flow Mode") Then
                Select Case FlowMode
                    Case FlowDirection.CoCurrent
                        Return ""
                    Case FlowDirection.CounterCurrent
                        Return ""
                    Case FlowDirection.Crossflow
                        Return ""
                End Select
            ElseIf prop.Equals("Participating Components") Then
                Return ""
            End If

        End If



        'If prop.Contains("_") Then

        '        Try

        '            Dim propidx As Integer = Convert.ToInt32(prop.Split("_")(2))

        '            Select Case propidx

        '                Case 0
        '                    'PROP_MB_0	Permeate Pressure
        '                    value = su.pressure
        '                Case 1
        '                    'PROP_MB_1	Inner diameter of fibers
        '                    value = su.diameter
        '                Case 2
        '                    'PROP_MB_2	Fiber Length
        '                    value = su.distance
        '                Case 3
        '                    'PROP_MB_3	Number of Fibers
        '                    value = ""
        '                Case 4
        '                    'PROP_MB_4	Number of Chambers
        '                    value = ""
        '            End Select


        '            Return value

        '        Catch ex As Exception

        '            Return ""

        '        End Try

        '    Else

        '        Select Case prop
        '            Case "Calculation Mode"
        '                Return ""
        '            Case "Flow Mode"
        '                Return ""
        '            Case "Participating Components"
        '                value = ""
        '            Case Else
        '                If prop.Contains("Conversion") Then value = "%"
        '        End Select

        '    End If

        '    Return value

        'End If

    End Function


    Public Overrides Function SetPropertyValue(ByVal prop As String, ByVal propval As Object, Optional ByVal su As Interfaces.IUnitsOfMeasure = Nothing) As Boolean

        If MyBase.SetPropertyValue(prop, propval, su) Then Return True

        If su Is Nothing Then su = New SystemsOfUnits.SI
        Dim cv As New SystemsOfUnits.Converter
        'Dim propidx As Integer = Convert.ToInt32(prop.Split("_")(2))

        'Select Case propidx

        '    Case 0
        '        'PROP_MB_0	Permeate Pressure
        '        Me.PermeatePressure = SystemsOfUnits.Converter.ConvertToSI(su.pressure, propval)
        '    Case 1
        '        'PROP_MB_1	Inner diameter of fibers
        '        Me.InnerDiameterFibers = SystemsOfUnits.Converter.ConvertToSI(su.diameter, propval)
        '    Case 2
        '        'PROP_MB_2	Fiber Length
        '        Me.FiberLength = SystemsOfUnits.Converter.ConvertToSI(su.distance, propval)
        '    Case 3
        '        'PROP_MB_3	Number of Fibers
        '        Me.NumberFibers = propval
        '    Case 4
        '        'PROP_MB_4	Number of Chambers
        '        Me.Chambers = propval
        'End Select


        If prop.Equals("Pressure Pressure") Then
            Me.PermeatePressure = SystemsOfUnits.Converter.ConvertToSI(su.pressure, propval)
        ElseIf prop.Equals("Inner diameter of fibers") Then
            Me.InnerDiameterFibers = SystemsOfUnits.Converter.ConvertToSI(su.diameter, propval)
        ElseIf prop.Equals("Fiber length") Then
            Me.FiberLength = SystemsOfUnits.Converter.ConvertToSI(su.distance, propval)
        ElseIf prop.Equals("Number of fibers") Then
            Me.NumberFibers = propval
        ElseIf prop.Equals("Number of chambers") Then
            Me.Chambers = propval
            'ElseIf prop.Equals("Calculation Mode") Then
            '    Select Case CalcMode
            '        Case MembraneMode.Gaspermeation
            '            Return ""
            '        Case MembraneMode.Pervaporation
            '            Return ""
            '        Case MembraneMode.SteamPermeation
            '            Return ""
            '    End Select
            'ElseIf prop.Equals("Flow Mode") Then
            '    Select Case FlowMode
            '        Case FlowDirection.CoCurrent
            '            Return ""
            '        Case FlowDirection.CounterCurrent
            '            Return ""
            '        Case FlowDirection.Crossflow
            '            Return ""
            '    End Select
            'ElseIf prop.Equals("Participating Components") Then
            '    Return ""
        End If

        If Me.Permeances.ContainsKey(prop) Then Me.Permeances(prop) = propval
        'Return Nothing

        Return True

    End Function

    Public Overrides Sub Calculate(Optional args As Object = Nothing)
        'Components = FlowSheet.SelectedCompounds.

        Components.Clear()
        For Each comp As String In FlowSheet.SelectedCompounds.Keys

            'Dim compound = FlowSheet.SelectedCompounds(comp)
            Components.Add(comp)

        Next


        Dim IDIndex As Integer = 0

        Dim IDs As Dictionary(Of Integer, String) 'use this to include or exclude compounds
        If IDs Is Nothing Then IDs = New Dictionary(Of Integer, String)
        For Each comp As KeyValuePair(Of String, ICompoundConstantProperties) In FlowSheet.SelectedCompounds

            IDs.Add(IDIndex, comp.Key)
            'FlowSheet.ShowMessage(IDIndex.ToString + "    " + comp.Key.ToString, IFlowsheet.MessageType.Other)
            IDIndex += 1

        Next

        Dim Permeance(Permeances.Count - 1) As Double
        Dim Pm_index As Integer = 0



        If Me.ResStageCuts Is Nothing Then Me.m_ResStageCut = New Dictionary(Of String, Double)

        'Me.ResStageCuts.Clear()
        'For Each s As String In Me.ComponentIDs
        '    Me.ResStageCuts.Add(s, 0)
        'Next

        'FlowSheet.ShowMessage(Permeances.Count.ToString, IFlowsheet.MessageType.Other)

        For Each pm As KeyValuePair(Of String, Double) In Permeances

            'FlowSheet.ShowMessage(pm.ToString + ":  " + Permeances.Item(pm.Key).ToString, IFlowsheet.MessageType.Other)

            'If ComponentIDs.Contains(pm.Key) Then

            Permeance(Pm_index) = Permeances.Item(pm.Key)

            Pm_index += 1
            'End If

            'FlowSheet.ShowMessage(Permeance(Pm_index).ToString, IFlowsheet.MessageType.Other)

        Next

        'outputpressure must be read in
        Dim pp = PermeatePressure     'Pa

        'ToFlowSheet(Permeance)


        Dim deviation As Double = 1000.0
        Dim count As Integer = 0


        'Validate unitop status.
        'Me.Validate()

        'Check if somme streams are already connected


        If GetInletMaterialStream(0) Is Nothing Then
            Throw New Exception("No stream connected to inlet gas port")
        End If

        If GetOutletMaterialStream(0) Is Nothing Then
            Throw New Exception("No stream connected to inlet water port")
        End If

        If GetOutletMaterialStream(1) Is Nothing Then

            Throw New Exception("No stream connected to outlet gas port")
        End If


        'Initialize streams {1x input material stream, 2x output Material stream, 1x Virtual Stream}

        Dim Feed, PermStream, RetStream, VirtFeed As DWSIM.Thermodynamics.Streams.MaterialStream

        'For Each es As KeyValuePair(Of String, Double) In ResStageCuts
        '    If GetExtraPropertyValue(es.Key) Is Nothing Then
        '        AddExtraProperty("Stage-Cut: " + es.Key, es.Value)
        '    End If


        'Next

        Feed = Me.GetInletMaterialStream(0)
        'VirtFeed = Me.GetInletMaterialStream(0)

        VirtFeed = InitStream()
        VirtFeed.Assign(Feed)

        Feed.Validate()
        VirtFeed.Validate()

        Dim esout = GetInletEnergyStream(1)


        'Get the Amount of total (active and non active) Compounds and also the number of Phases

        Dim NumCompounds, NumPhases As Integer
        NumCompounds = Feed.GetNumCompounds
        NumPhases = Feed.GetNumPhases



        'Get Values from the Input Stream that we need
        Dim Tf, Pf, mTotal, nTotal, hf, sf As Double
        Dim xf(NumCompounds - 1), xfInit(NumCompounds - 1), nf(NumCompounds - 1), pxf(NumCompounds - 1), wf(NumCompounds - 1) As Double

        Tf = Feed.GetTemperature()                                'K
        Pf = Feed.GetPressure()                                   'Pa
        mTotal = Feed.GetMassFlow()                               'kg/s
        nTotal = Feed.GetMolarFlow()                              'mol/s
        hf = Feed.GetMassEnthalpy()                               'kJ/kg
        sf = Feed.GetMassEntropy()                                'kJ/kg

        xf = Feed.GetPhaseComposition(0)                          '-
        xfInit = Feed.GetPhaseComposition(0)                      '-

        For i = 0 To (NumCompounds - 1)
            nf(i) = xf(i) * nTotal
        Next

        wf = Feed.MoleFractionsToMassFractions(nf)



        VirtFeed = RefreshStream(VirtFeed, 400, Feed.GetPressure(), xf, nf)

        'FlowSheet.ShowMessage(Feed.ToString, IFlowsheet.MessageType.Other)
        'FlowSheet.ShowMessage(VirtFeed.ToString, IFlowsheet.MessageType.Other)




        'Output Values initials must be Defined here

        Dim Tr, Tp, qtp, qtr As Double
        Dim xr, yi, yp, pxr, pyp, pyi, wp, wr, cp, sc0, Hp, Hr, Hv, Ratio As Double()
        Dim NEff(NumCompounds - 1), NEffVap(NumCompounds - 1), qip(NumCompounds - 1), qi0(NumCompounds - 1), qir(NumCompounds - 1), sc(NumCompounds - 1) As Double
        Dim Threads, Cells As Integer


        Dim PermeateTotal As Double = 0
        Dim RetentateTotal As Double = 0

        Dim yp_acuml(NumCompounds - 1) As Double

        Ratio = PressureRatio(Feed, pp)
        xr = InitMolarFractionxr(xf)
        yi = InitMolarFractionyi(Feed, xf, Permeance)
        yp = InitMolarFractionyp(xf)
        pyi = PartialPressure(yi, pp)
        pyp = PartialPressure(yp, pp)
        Dim P As Double = Me.GetInletMaterialStream(0).Phases(0).Properties.pressure.GetValueOrDefault
        Area = InnerDiameterFibers * Math.PI * FiberLength * NumberFibers
        Cells = 0
        Threads = 0

        'ToFlowSheet(Ratio)

        NEff = calcEffectiveMolarFlow(Feed)


        sc = InitStageCut(StageCut, NumCompounds)
        Dim sc_index As Integer = 0

        For I = 0 To NumCompounds - 1
            qip(I) = sc(I) * NEff(I)
        Next
        qtp = SumY(qip)

        'For Each comp In Async String In CompoundIDs

        'ToFlowSheet(xr)
        'xr = xrRetentateMultiInit(xf, xf, Pf, pp, Permeance)
        'ToFlowSheet(xf)
        'ToFlowSheet(xr)


        'If CalcMode = FlowDirection.Crossflow Then

        'End If
        'ToFlowSheet(xr)


        'FlowSheet.ShowMessage(IDs.Item(I).ToString, IFlowsheet.MessageType.Other)

        'ToFlowSheet(Permeance)

        Select Case CalcMode
            Case Membrane.MembraneMode.Gaspermeation
                'complete calculation routine of Gas Permeation

                FlowSheet.ShowMessage("Calculate Gas Permeation", IFlowsheet.MessageType.Other)

                pxf = PartialPressure(Feed)
                pxr = PartialPressure(xr, Pf)



                For K = 0 To Chambers - 1

                    While (deviation >= 0.00001) And (count < 1000)


                        If FlowMode = FlowDirection.CoCurrent Then

                            yi = yfMultiCocurrent(xf, yi, Ratio, Permeance)
                            pyi = PartialPressure(yi, pp)

                            For I = 0 To NumCompounds - 1

                                qi0(I) = qip(I)

                            Next

                            For I = 0 To NumCompounds - 1

                                If ActiveComponents.Contains(IDs.Item(I)) Then

                                    sc(I) = Permeance(I) * (Area / Chambers) * ((pxf(I) - pyi(I))) / NEff(I) '- (pxr(I) - pyp(I))
                                Else
                                    sc(I) = 0
                                End If

                            Next

                        ElseIf FlowMode = FlowDirection.CounterCurrent Then

                            yi = yiRetentateMultids2(xr, yi, Ratio, Permeance)
                            pyi = PartialPressure(yi, pp)

                            For I = 0 To NumCompounds - 1

                                qi0(I) = qip(I)

                            Next

                            For I = 0 To NumCompounds - 1

                                If ActiveComponents.Contains(IDs.Item(I)) Then

                                    sc(I) = (((Permeance(I) * (Area / Chambers))) * (((pxf(I) - pyp(I)) - (pxr(I) - pyi(I))) / (Log((pxf(I) - pyp(I)) / (pxr(I) - pyi(I)))))) / NEff(I)
                                Else
                                    sc(I) = 0
                                End If
                            Next
                            'ToFlowSheet(sc)
                        ElseIf FlowMode = FlowDirection.Crossflow Then

                        End If

                        'yi = yiRetentateMultids2(xr, yi, Ratio, Permeance)
                        'pyi = PartialPressure(yi, pp)


                        'For I = 0 To NumCompounds - 1

                        '    qi0(I) = qip(I)

                        'Next

                        'For I = 0 To NumCompounds - 1

                        '    sc(I) = (((Permeance(I) * (Area / Chambers))) * (((pxf(I) - pyp(I)) - (pxr(I) - pyi(I))) / (Log((pxf(I) - pyp(I)) / (pxr(I) - pyi(I)))))) / NEff(I)

                        'Next

                        For I = 0 To NumCompounds - 1

                            qip(I) = sc(I) * NEff(I)

                        Next

                        qtp = SumY(qip)

                        For I = 0 To NumCompounds - 1

                            yp(I) = qip(I) / qtp

                        Next

                        pyp = PartialPressure(yp, pp)
                        qtr = 0

                        For I = 0 To NumCompounds - 1

                            qir(I) = NEff(I) - qip(I)

                        Next

                        qtr = SumY(qir)


                        For I = 0 To NumCompounds - 1

                            xr(I) = qir(I) / qtr
                            pxr(I) = Pf * xr(I)

                        Next

                        deviation = 0

                        For I = 0 To NumCompounds - 1

                            deviation = deviation + Abs(qip(I) - qi0(I))

                        Next

                        count = count + 1

                    End While

                    PermeateTotal = PermeateTotal + qtp
                    RetentateTotal = qtr

                    sc = InitStageCut(StageCut, NumCompounds)

                    For I = 0 To NumCompounds - 1

                        NEff(I) = qir(I)

                    Next

                    For I = 0 To NumCompounds - 1

                        yp_acuml(I) = yp_acuml(I) + qip(I)

                    Next

                    Cells = Cells + 1

                    If Cells < Chambers Then


                        xf = PassMolarComposition(xr)
                        yp = InitMolarFractionyp(xf)
                        yi = InitMolarFractionyi(Feed, xf, Permeance)
                        xr = InitMolarFractionyp(xf)
                        pxf = PartialPressure(xf, Pf)
                        pxr = PartialPressure(xr, Pf)
                        pyp = PartialPressure(xfInit, pp)
                        pyi = PartialPressure(xfInit, pp)

                        deviation = 1000.0
                        count = 0

                        For I = 0 To NumCompounds - 1

                            qir(I) = 0
                            qi0(I) = 0
                            qip(I) = sc(I) * NEff(I)




                        Next

                        qtp = SumY(qip)

                    End If



                Next

                For I = 0 To NumCompounds - 1

                    yp(I) = (yp_acuml(I) / PermeateTotal)

                Next



                PermStream = Me.GetOutletMaterialStream(0)
                RetStream = Me.GetOutletMaterialStream(1)

                PermStream.SetMolarFlow(PermeateTotal)
                PermStream.SetOverallComposition(yp.ToArray)
                PermStream.SetTemperature(Tf)
                PermStream.SetPressure(pp)
                PermStream.SetFlashSpec("PT")
                Me.PropertyPackage.CurrentMaterialStream = PermStream
                PermStream.Calculate()
                PermStream.Validate()


                RetStream.SetMolarFlow(RetentateTotal)
                RetStream.SetOverallComposition(xr.ToArray)
                RetStream.SetTemperature(Tf)
                RetStream.SetPressure(Pf)
                RetStream.SetFlashSpec("PT")
                Me.PropertyPackage.CurrentMaterialStream = RetStream
                RetStream.Calculate()
                RetStream.Validate()


            Case Membrane.MembraneMode.Pervaporation
                'complete calculation routune of Pervaporation

                FlowSheet.ShowMessage("Calculate Pervaporation", IFlowsheet.MessageType.Other)

                pxf = CalcPartialPressure(Feed, xf)
                pxr = CalcPartialPressure(Feed, xr)

                'ToFlowSheet(pxf)
                'FlowSheet.ShowMessage("show pxr:", IFlowsheet.MessageType.Other)
                'ToFlowSheet(pxr)

                For L = 0 To Chambers - 1

                    While (deviation >= 0.00001) And (count < 1000)


                        If FlowMode = FlowDirection.CoCurrent Then

                            yi = yfMultiCocurrent(xf, yi, Ratio, Permeance)
                            pyi = PartialPressure(yi, pp)

                            For I = 0 To NumCompounds - 1

                                qi0(I) = qip(I)

                            Next

                            For I = 0 To NumCompounds - 1

                                If ActiveComponents.Contains(IDs.Item(I)) Then

                                    sc(I) = Permeance(I) * (Area / Chambers) * ((pxf(I) - pyi(I))) / NEff(I) '- (pxr(I) - pyp(I))
                                Else
                                    sc(I) = 0
                                End If

                            Next


                        ElseIf FlowMode = FlowDirection.CounterCurrent Then


                            yi = yiRetentateMultids2(xr, yi, Ratio, Permeance)
                            pyi = PartialPressure(yi, pp)

                            'ToFlowSheet(yi)


                            For I = 0 To NumCompounds - 1

                                qi0(I) = qip(I)

                            Next

                            For I = 0 To NumCompounds - 1

                                If ActiveComponents.Contains(IDs.Item(I)) Then

                                    sc(I) = (((Permeance(I) * (Area / Chambers))) * (((pxf(I) - pyp(I)) - (pxr(I) - pyi(I))) / (Log((pxf(I) - pyp(I)) / (pxr(I) - pyi(I)))))) / NEff(I)
                                Else
                                    sc(I) = 0
                                End If
                            Next

                        ElseIf FlowMode = FlowDirection.Crossflow Then

                            yi = yfMultiCross(xf, xr, yi, Ratio, Permeance)
                            pyi = PartialPressure(yi, pp)

                            'ToFlowSheet(yi)


                            For I = 0 To NumCompounds - 1

                                qi0(I) = qip(I)

                            Next

                            For I = 0 To NumCompounds - 1

                                If ActiveComponents.Contains(IDs.Item(I)) Then

                                    'sc(I) = Permeance(I) * (Area / Chambers) * ((pxf(I) - pyi(I))) / NEff(I)
                                    sc(I) = (((Permeance(I) * (Area / Chambers))) * (((pxf(I) - pyp(I)) - (pxr(I) - pyi(I))) / (Log((pxf(I) - pyp(I)) / (pxr(I) - pyi(I)))))) / NEff(I)
                                    'sc(I) = Permeance(I) * (Area / Chambers) * ((((xf(I) - xr(I)) / Log(xf(I) / xr(I))) * Pf) - pyp(I)) / NEff(I)

                                Else
                                    sc(I) = 0
                                End If
                            Next

                        End If



                        For I = 0 To NumCompounds - 1

                            qip(I) = sc(I) * NEff(I)

                        Next

                        qtp = SumY(qip)

                        For I = 0 To NumCompounds - 1

                            yp(I) = qip(I) / qtp

                        Next

                        pyp = PartialPressure(yp, pp)
                        qtr = 0

                        For I = 0 To NumCompounds - 1

                            qir(I) = NEff(I) - qip(I)

                        Next

                        qtr = SumY(qir)


                        For I = 0 To NumCompounds - 1

                            xr(I) = qir(I) / qtr
                        Next
                        VirtFeed = RefreshStream(VirtFeed, Tf, P, xf, NEff)
                        pxr = CalcPartialPressure(VirtFeed, xr)

                        deviation = 0

                        For I = 0 To NumCompounds - 1

                            deviation = deviation + Abs(qip(I) - qi0(I))

                        Next

                        count = count + 1

                    End While

                    PermeateTotal = PermeateTotal + qtp
                    RetentateTotal = qtr

                    sc = InitStageCut(StageCut, NumCompounds)

                    For I = 0 To NumCompounds - 1

                        NEff(I) = qir(I)

                    Next

                    For I = 0 To NumCompounds - 1

                        yp_acuml(I) = yp_acuml(I) + qip(I)

                    Next

                    Cells = Cells + 1

                    If Cells < Chambers Then

                        xf = PassMolarComposition(xr)
                        yp = InitMolarFractionyp(xf)
                        yi = InitMolarFractionyi(Feed, xf, Permeance)
                        xr = InitMolarFractionyp(xf)
                        pxf = PartialPressure(xf, Pf)
                        pxr = PartialPressure(xr, Pf)
                        pyp = PartialPressure(xfInit, pp)
                        pyi = PartialPressure(xfInit, pp)

                        deviation = 1000.0
                        count = 0

                        For I = 0 To NumCompounds - 1

                            qir(I) = 0
                            qi0(I) = 0
                            qip(I) = sc(I) * NEff(I)


                        Next

                        qtp = SumY(qip)

                    End If
                    VirtFeed = RefreshStream(VirtFeed, Tf, P, xf, NEff)


                Next



                For I = 0 To NumCompounds - 1

                    yp(I) = (yp_acuml(I) / PermeateTotal)

                Next

                Tp = GetDewPoint(Feed)
                cp = GetCp(Feed)
                Hv = GetHv(Feed)

                Dim K(2) As Double

                For I = 0 To Feed.GetNumCompounds - 1
                    K(0) += (xf(I) * cp(I))
                Next

                For I = 0 To Feed.GetNumCompounds - 1
                    K(1) += (xf(I) * cp(I))
                Next

                For I = 0 To Feed.GetNumCompounds - 1
                    K(2) += (xf(I) * cp(I))
                Next

                Tr = (((Tp * PermeateTotal * K(1)) - (Tf * nTotal * K(0)) + (PermeateTotal * K(2))) / ((PermeateTotal * K(1)) - (nTotal * K(0))))


                PermStream = Me.GetOutletMaterialStream(0)
                RetStream = Me.GetOutletMaterialStream(1)

                PermStream.SetMolarFlow(PermeateTotal)
                PermStream.SetOverallComposition(yp.ToArray)
                PermStream.SetTemperature(Tf)
                PermStream.SetPressure(pp)
                PermStream.SetFlashSpec("PT")
                Me.PropertyPackage.CurrentMaterialStream = PermStream
                PermStream.Calculate()
                PermStream.Validate()


                RetStream.SetMolarFlow(RetentateTotal)
                RetStream.SetOverallComposition(xr.ToArray)
                RetStream.SetTemperature(Tf)
                RetStream.SetPressure(Pf)
                RetStream.SetFlashSpec("PT")
                Me.PropertyPackage.CurrentMaterialStream = RetStream
                RetStream.Calculate()
                RetStream.Validate()

            Case Membrane.MembraneMode.SteamPermeation
                'complete calculation routune of Steam Permeation

                pxf = CalcPartialPressure(Feed, xf)
                pxr = CalcPartialPressure(Feed, xr)

                NEffVap = CalcEffectiveVaporMolarFlow(Feed)

                For L = 0 To Chambers - 1

                    While (deviation >= 0.00001) And (count < 1000)

                        yi = yiRetentateMultids2(xr, yi, Ratio, Permeance)
                        pyi = PartialPressure(yi, pp)


                        For I = 0 To NumCompounds - 1

                            qi0(I) = qip(I)

                        Next



                        For I = 0 To NumCompounds - 1
                            'N_eff_vap
                            sc(I) = (((Permeance(I) * (Area / Chambers))) * (((pxf(I) - pyp(I)) - (pxr(I) - pyi(I))) / (Log((pxf(I) - pyp(I)) / (pxr(I) - pyi(I)))))) / NEffVap(I)

                        Next

                        For I = 0 To NumCompounds - 1

                            qip(I) = sc(I) * NEffVap(I)

                        Next

                        qtp = SumY(qip)

                        For I = 0 To NumCompounds - 1

                            yp(I) = qip(I) / qtp

                        Next

                        pyp = PartialPressure(yp, pp)
                        qtr = 0

                        For I = 0 To NumCompounds - 1

                            qir(I) = NEff(I) - qip(I)

                        Next

                        qtr = SumY(qir)


                        For I = 0 To NumCompounds - 1

                            xr(I) = qir(I) / qtr
                        Next
                        VirtFeed = RefreshStream(VirtFeed, Tf, P, xf, NEff)
                        pxr = CalcPartialPressure(VirtFeed, xr)

                        deviation = 0

                        For I = 0 To NumCompounds - 1

                            deviation = deviation + Abs(qip(I) - qi0(I))

                        Next

                        count = count + 1

                    End While




                    PermeateTotal = PermeateTotal + qtp
                    RetentateTotal = qtr

                    sc = InitStageCut(StageCut, NumCompounds)
                    NEffVap = CalcEffectiveVaporMolarFlow(VirtFeed)

                    For I = 0 To NumCompounds - 1

                        NEff(I) = qir(I)

                    Next

                    For I = 0 To NumCompounds - 1

                        yp_acuml(I) = yp_acuml(I) + qip(I)

                    Next

                    Cells = Cells + 1

                    If Cells < Chambers Then

                        xf = PassMolarComposition(xr)
                        yp = InitMolarFractionyp(xf)
                        yi = InitMolarFractionyi(Feed, xf, Permeance)
                        xr = InitMolarFractionyp(xf)
                        pxf = PartialPressure(xf, Pf)
                        pxr = PartialPressure(xr, Pf)
                        pyp = PartialPressure(xfInit, pp)
                        pyi = PartialPressure(xfInit, pp)

                        deviation = 1000.0
                        count = 0

                        For I = 0 To NumCompounds - 1

                            qir(I) = 0
                            qi0(I) = 0
                            qip(I) = sc(I) * NEff(I)


                        Next

                        qtp = SumY(qip)

                    End If
                    VirtFeed = RefreshStream(VirtFeed, Tf, P, xf, NEff)


                Next

                For I = 0 To NumCompounds - 1

                    yp(I) = (yp_acuml(I) / PermeateTotal)

                Next
                Tp = GetDewPoint(Feed)
                cp = GetCp(Feed)
                Hv = GetHv(Feed)

                Dim K(2) As Double

                For I = 0 To Feed.GetNumCompounds - 1
                    K(0) += (xf(I) * cp(I))
                Next

                For I = 0 To Feed.GetNumCompounds - 1
                    K(1) += (xf(I) * cp(I))
                Next

                For I = 0 To Feed.GetNumCompounds - 1
                    K(2) += (xf(I) * cp(I))
                Next

                Tr = (((Tp * PermeateTotal * K(1)) - (Tf * nTotal * K(0)) + (PermeateTotal * K(2))) / ((PermeateTotal * K(1)) - (nTotal * K(0))))



                PermStream = Me.GetOutletMaterialStream(0)
                RetStream = Me.GetOutletMaterialStream(1)

                PermStream.SetMolarFlow(PermeateTotal)
                PermStream.SetOverallComposition(yp.ToArray)
                PermStream.SetTemperature(Tf)
                PermStream.SetPressure(pp)
                PermStream.SetFlashSpec("PT")
                Me.PropertyPackage.CurrentMaterialStream = PermStream
                PermStream.Calculate()
                PermStream.Validate()


                RetStream.SetMolarFlow(RetentateTotal)
                RetStream.SetOverallComposition(xr.ToArray)
                RetStream.SetTemperature(Tf)
                RetStream.SetPressure(Pf)
                RetStream.SetFlashSpec("PT")
                Me.PropertyPackage.CurrentMaterialStream = RetStream
                RetStream.Calculate()
                RetStream.Validate()
        End Select

    End Sub
End Class
