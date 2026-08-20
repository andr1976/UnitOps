using System;
using CAPEOPEN;
using MembraneCore.Models;
using Xunit;

namespace Membrane.CapeOpen.Tests
{
    /// <summary>
    /// Headless end-to-end test of the CAPE-OPEN adapter: connect ports, configure parameters, run the
    /// Validate→Calculate lifecycle against mock Material Objects, and confirm the outlets receive exactly
    /// what the validated <see cref="CrossFlowModel"/> produces. No PME/COFE required.
    /// </summary>
    public class AdapterIntegrationTests
    {
        private static readonly string[] Ids = { "ammonia", "hydrogen", "nitrogen" };
        private static readonly double[] Feed = { 0.45, 0.25, 0.30 };
        private static readonly double[] Perm = { 2.63e-10, 8.35e-11, 1.72e-11 };
        private const double Tk = 323.15, Pr = 1.0e6, Pp = 1.3e5, Nf = 1.0, Area = 200.0;

        private static ICapeParameter Param(object collection, string name)
            => (ICapeParameter)((ICapeCollection)collection).Item(name);

        [Fact]
        public void FullLifecycle_DelegatesToCrossFlowModel()
        {
            var feed = MockMaterialObject.Feed(Ids, Tk, Pr, Nf, Feed);
            var retentate = new MockMaterialObject(Ids);
            var permeate = new MockMaterialObject(Ids);

            var unit = new MembraneUnitOperation();
            unit.Initialize();

            // Connect the three material ports.
            var ports = (ICapeCollection)unit.ports;
            ((ICapeUnitPort)ports.Item("Feed")).Connect(feed);
            ((ICapeUnitPort)ports.Item("Retentate")).Connect(retentate);
            ((ICapeUnitPort)ports.Item("Permeate")).Connect(permeate);

            // Operating parameters.
            Param(unit.parameters, "PermeatePressure").value = Pp;
            Param(unit.parameters, "MembraneArea").value = Area;

            // First Validate discovers the compounds and creates per-compound permeance parameters
            // (still zero → invalid), which is expected.
            string msg = "";
            bool first = unit.Validate(ref msg);
            Assert.False(first);
            Assert.Equal(CapeValidationStatus.CAPE_INVALID, unit.ValStatus);

            // Configure permeances, then validate for real.
            Param(unit.parameters, "Permeance_ammonia").value = Perm[0];
            Param(unit.parameters, "Permeance_hydrogen").value = Perm[1];
            Param(unit.parameters, "Permeance_nitrogen").value = Perm[2];

            bool ok = unit.Validate(ref msg);
            Assert.True(ok, msg);
            Assert.Equal(CapeValidationStatus.CAPE_VALID, unit.ValStatus);

            unit.Calculate();

            // Reference result straight from the physics core.
            var expected = CrossFlowModel.SolveByArea(Feed, Nf, Perm, Pr, Pp, Area);

            // Permeate stream captured by the mock.
            var permFrac = permeate.Get("fraction");
            for (int i = 0; i < 3; i++)
                Assert.Equal(expected.PermeateComposition[i], permFrac[i], 6);
            Assert.Equal(Nf * expected.StageCut, permeate.Get("totalflow")[0], 6);
            Assert.Equal(Pp, permeate.Get("pressure")[0], 3);
            Assert.Equal(Tk, permeate.Get("temperature")[0], 6);

            // Retentate stream.
            var retFrac = retentate.Get("fraction");
            for (int i = 0; i < 3; i++)
                Assert.Equal(expected.RetentateComposition[i], retFrac[i], 6);
            Assert.Equal(Nf * (1.0 - expected.StageCut), retentate.Get("totalflow")[0], 6);
            Assert.Equal(Pr, retentate.Get("pressure")[0], 3);

            // Each outlet must have been flashed exactly once.
            Assert.Equal(1, permeate.CalcEquilibriumCalls);
            Assert.Equal(1, retentate.CalcEquilibriumCalls);

            // Output parameter reports the stage cut.
            double stageCut = Convert.ToDouble(Param(unit.parameters, "StageCut").value);
            Assert.Equal(expected.StageCut, stageCut, 6);

            // Overall mass balance closes.
            for (int i = 0; i < 3; i++)
            {
                double inMol = Nf * Feed[i];
                double outMol = permeate.Get("totalflow")[0] * permFrac[i] + retentate.Get("totalflow")[0] * retFrac[i];
                Assert.Equal(inMol, outMol, 6);
            }

            // Position-dependent profiles are published as plottable array output parameters (100 points).
            // Per the Parameter CI errata the array value is a SAFEARRAY(VARIANT) -> object[] of boxed doubles.
            var pos = Arr(Param(unit.parameters, "Profile_Position").value);
            Assert.Equal(100, pos.Length);
            Assert.Equal(0.0, pos[0], 6);
            Assert.Equal(1.0, pos[99], 6);
            var retProfile = Arr(Param(unit.parameters, "Profile_Retentate_ammonia").value);
            Assert.Equal(100, retProfile.Length);
            Assert.True(retProfile[99] < retProfile[0], "fast gas depletes along the retentate profile");
            var scProfile = Arr(Param(unit.parameters, "Profile_StageCut").value);
            Assert.True(scProfile[99] > scProfile[0], "stage cut rises along position");
            Assert.Equal(expected.StageCut, scProfile[99], 2);
        }

        // Array parameter value is a SAFEARRAY(VARIANT): an object[] of boxed doubles.
        private static double[] Arr(object v)
        {
            if (v is double[] d) return d;
            var o = (object[])v;
            var a = new double[o.Length];
            for (int i = 0; i < o.Length; i++) a[i] = System.Convert.ToDouble(o[i]);
            return a;
        }

        [Fact]
        public void SpecMode_StageCut_ComputesRequiredArea_AndTogglesModes()
        {
            var unit = new MembraneUnitOperation();
            unit.Initialize();
            var ports = (ICapeCollection)unit.ports;
            ((ICapeUnitPort)ports.Item("Feed")).Connect(MockMaterialObject.Feed(Ids, Tk, Pr, Nf, Feed));
            ((ICapeUnitPort)ports.Item("Retentate")).Connect(new MockMaterialObject(Ids));
            ((ICapeUnitPort)ports.Item("Permeate")).Connect(new MockMaterialObject(Ids));

            Param(unit.parameters, "PermeatePressure").value = Pp;
            Param(unit.parameters, "SpecMode").value = "StageCut";   // design mode
            string msg = "";
            unit.Validate(ref msg);                                  // creates permeance params + applies modes
            Param(unit.parameters, "Permeance_ammonia").value = Perm[0];
            Param(unit.parameters, "Permeance_hydrogen").value = Perm[1];
            Param(unit.parameters, "Permeance_nitrogen").value = Perm[2];

            // In design mode StageCut is now an INPUT target; MembraneArea is a computed OUTPUT.
            Assert.Equal(CapeParamMode.CAPE_INPUT, Param(unit.parameters, "StageCut").Mode);
            Assert.Equal(CapeParamMode.CAPE_OUTPUT, Param(unit.parameters, "MembraneArea").Mode);

            const double target = 0.35;
            Param(unit.parameters, "StageCut").value = target;
            Assert.True(unit.Validate(ref msg), msg);
            unit.Calculate();

            double areaOut = Convert.ToDouble(Param(unit.parameters, "MembraneArea").value);
            Assert.True(areaOut > 0, "required area computed");

            // The computed area must reproduce the target stage cut.
            var check = MembraneCore.Models.CrossFlowModel.SolveByArea(Feed, Nf, Perm, Pr, Pp, areaOut);
            Assert.Equal(target, check.StageCut, 3);
        }

        [Fact]
        public void EnergyMode_Adiabatic_CoolsPermeateByJouleThomson_RetentateStaysAtFeed()
        {
            var feed = MockMaterialObject.Feed(Ids, Tk, Pr, Nf, Feed);
            var retentate = new MockMaterialObject(Ids);
            var permeate = new MockMaterialObject(Ids);

            var unit = new MembraneUnitOperation();
            unit.Initialize();
            var ports = (ICapeCollection)unit.ports;
            ((ICapeUnitPort)ports.Item("Feed")).Connect(feed);
            ((ICapeUnitPort)ports.Item("Retentate")).Connect(retentate);
            ((ICapeUnitPort)ports.Item("Permeate")).Connect(permeate);

            Param(unit.parameters, "PermeatePressure").value = Pp;
            Param(unit.parameters, "MembraneArea").value = Area;
            Param(unit.parameters, "EnergyMode").value = "Adiabatic";
            string msg = "";
            unit.Validate(ref msg);
            Param(unit.parameters, "Permeance_ammonia").value = Perm[0];
            Param(unit.parameters, "Permeance_hydrogen").value = Perm[1];
            Param(unit.parameters, "Permeance_nitrogen").value = Perm[2];
            Assert.True(unit.Validate(ref msg), msg);
            unit.Calculate();

            // The mock enthalpy is a linear Joule-Thomson fluid (mu=1e-6 K/Pa): isenthalpic expansion
            // Pr->Pp cools the permeate by mu*(Pp-Pr); the constant-pressure retentate stays at feed T.
            double expectedPermT = Tk + 1.0e-6 * (Pp - Pr);
            Assert.Equal(expectedPermT, Convert.ToDouble(Param(unit.parameters, "PermeateTemperature").value), 2);
            Assert.Equal(Tk, Convert.ToDouble(Param(unit.parameters, "RetentateTemperature").value), 2);

            // The outlet streams were written at those temperatures.
            Assert.Equal(expectedPermT, permeate.Get("temperature")[0], 2);
            Assert.Equal(Tk, retentate.Get("temperature")[0], 2);

            // Permeate temperature profile is populated and cooler than the feed downstream.
            var permTprofile = Arr(Param(unit.parameters, "Profile_PermeateTemperature").value);
            Assert.Equal(100, permTprofile.Length);
            Assert.True(permTprofile[99] < Tk, "permeate profile cools along position");
        }

        [Fact]
        public void DrivingForce_Fugacity_ReducesStageCut_VersusPartialPressure()
        {
            double SolveStageCut(string mode)
            {
                var feed = MockMaterialObject.Feed(Ids, Tk, Pr, Nf, Feed);
                feed.Fugacity = new[] { 0.80, 0.90, 0.95 };   // real-gas φ < 1 at high pressure
                var unit = new MembraneUnitOperation();
                unit.Initialize();
                var ports = (ICapeCollection)unit.ports;
                ((ICapeUnitPort)ports.Item("Feed")).Connect(feed);
                ((ICapeUnitPort)ports.Item("Retentate")).Connect(new MockMaterialObject(Ids));
                ((ICapeUnitPort)ports.Item("Permeate")).Connect(new MockMaterialObject(Ids));
                Param(unit.parameters, "PermeatePressure").value = Pp;
                Param(unit.parameters, "MembraneArea").value = Area;
                Param(unit.parameters, "DrivingForce").value = mode;
                string msg = "";
                unit.Validate(ref msg);
                Param(unit.parameters, "Permeance_ammonia").value = Perm[0];
                Param(unit.parameters, "Permeance_hydrogen").value = Perm[1];
                Param(unit.parameters, "Permeance_nitrogen").value = Perm[2];
                Assert.True(unit.Validate(ref msg), msg);
                unit.Calculate();
                return Convert.ToDouble(Param(unit.parameters, "StageCut").value);
            }

            double ig = SolveStageCut("PartialPressure");
            double rg = SolveStageCut("Fugacity");
            Assert.True(ig > 0 && rg > 0);
            Assert.True(rg < ig, $"fugacity (real-gas) stage cut {rg} should be below partial-pressure {ig}");
        }

        [Fact]
        public void DrivingForce_FugacityLocal_RunsAndDiffersFromConstantFugacity()
        {
            double SolveStageCut(string mode)
            {
                var feed = MockMaterialObject.Feed(Ids, Tk, Pr, Nf, Feed);
                feed.CompositionDependentFugacity = true;   // φ_i = 1 − 0.4·x_i, varies along the module
                var unit = new MembraneUnitOperation();
                unit.Initialize();
                var ports = (ICapeCollection)unit.ports;
                ((ICapeUnitPort)ports.Item("Feed")).Connect(feed);
                ((ICapeUnitPort)ports.Item("Retentate")).Connect(new MockMaterialObject(Ids));
                ((ICapeUnitPort)ports.Item("Permeate")).Connect(new MockMaterialObject(Ids));
                Param(unit.parameters, "PermeatePressure").value = Pp;
                Param(unit.parameters, "MembraneArea").value = Area;
                Param(unit.parameters, "DrivingForce").value = mode;
                string msg = "";
                unit.Validate(ref msg);
                Param(unit.parameters, "Permeance_ammonia").value = Perm[0];
                Param(unit.parameters, "Permeance_hydrogen").value = Perm[1];
                Param(unit.parameters, "Permeance_nitrogen").value = Perm[2];
                Assert.True(unit.Validate(ref msg), msg);
                unit.Calculate();
                return Convert.ToDouble(Param(unit.parameters, "StageCut").value);
            }

            double partial = SolveStageCut("PartialPressure");
            double constFug = SolveStageCut("Fugacity");
            double localFug = SolveStageCut("FugacityLocal");

            Assert.True(localFug > 0 && localFug < 1, "FugacityLocal produced a valid stage cut");
            // φ < 1 lowers the driving force, so both fugacity modes give a smaller stage cut than ideal.
            Assert.True(constFug < partial, $"const-fugacity {constFug} < partial {partial}");
            Assert.True(localFug < partial, $"local-fugacity {localFug} < partial {partial}");
            // Position-dependent φ must actually differ from the single feed-evaluated coefficient.
            Assert.True(System.Math.Abs(localFug - constFug) > 1e-4,
                $"local {localFug} should differ from constant-feed {constFug}");
        }

        [Fact]
        public void Calculate_WithNoPortsConnected_ThrowsSolvingError_WithReadableName()
        {
            var unit = new MembraneUnitOperation();
            unit.Initialize();
            var ex = Assert.Throws<Membrane.CapeOpen.CapeError>(() => unit.Calculate());
            // The error object must expose a readable CAPE-OPEN name/description (so COFE doesn't report
            // "failed to get error name"), and carry the solving-error HRESULT.
            Assert.Equal(ComErrorHr.SolvingError, ex.HResult);
            Assert.Equal("ECapeSolvingError", ((CAPEOPEN.ECapeRoot)ex).name);
            Assert.False(string.IsNullOrEmpty(((CAPEOPEN.ECapeUser)ex).description));
        }

        [Fact]
        public void Terminate_DisconnectsAllPorts()
        {
            var unit = new MembraneUnitOperation();
            unit.Initialize();
            var ports = (ICapeCollection)unit.ports;
            ((ICapeUnitPort)ports.Item("Feed")).Connect(new MockMaterialObject(Ids));
            ((ICapeUnitPort)ports.Item("Retentate")).Connect(new MockMaterialObject(Ids));
            ((ICapeUnitPort)ports.Item("Permeate")).Connect(new MockMaterialObject(Ids));
            Assert.NotNull(((ICapeUnitPort)ports.Item("Feed")).connectedObject);

            unit.Terminate();

            Assert.Null(((ICapeUnitPort)ports.Item("Feed")).connectedObject);
            Assert.Null(((ICapeUnitPort)ports.Item("Retentate")).connectedObject);
            Assert.Null(((ICapeUnitPort)ports.Item("Permeate")).connectedObject);
        }

        [Fact]
        public void Disconnect_ClearsConnection()
        {
            var unit = new MembraneUnitOperation();
            unit.Initialize();
            var feedPort = (ICapeUnitPort)((ICapeCollection)unit.ports).Item("Feed");
            feedPort.Connect(new MockMaterialObject(Ids));
            Assert.NotNull(feedPort.connectedObject);
            feedPort.Disconnect();
            Assert.Null(feedPort.connectedObject);
        }

        [Fact]
        public void Validate_FailsGracefully_WhenPortsNotConnected()
        {
            var unit = new MembraneUnitOperation();
            unit.Initialize();
            string msg = "";
            Assert.False(unit.Validate(ref msg));
            Assert.False(string.IsNullOrEmpty(msg));
            Assert.Equal(CapeValidationStatus.CAPE_INVALID, unit.ValStatus);
        }

        [Fact]
        public void ParameterCollection_IsStableAcrossValidateAndCalculate()
        {
            // CO Unit Operations spec, errata 2.7: the (parameter) collection must stay constant — same object,
            // order, names and directions — after a value change, a (dis)connection or a solve; it may only
            // change during Edit/Initialize/Load. COFE binds information streams to public parameters, so a
            // collection that is replaced or reordered during a solve desyncs those bindings (the reference-
            // counting failure). This test locks the collection identity and order across the solve lifecycle.
            var unit = new MembraneUnitOperation();
            unit.Initialize();
            var ports = (ICapeCollection)unit.ports;
            ((ICapeUnitPort)ports.Item("Feed")).Connect(MockMaterialObject.Feed(Ids, Tk, Pr, Nf, Feed));
            ((ICapeUnitPort)ports.Item("Retentate")).Connect(new MockMaterialObject(Ids));
            ((ICapeUnitPort)ports.Item("Permeate")).Connect(new MockMaterialObject(Ids));

            // The collection object and the fixed parameters exist before any solve (built in the constructor).
            var coll = (ICapeCollection)unit.parameters;
            var permPressure = Param(unit.parameters, "PermeatePressure");
            int fixedCount = coll.Count();
            Assert.Same(permPressure, (ICapeParameter)coll.Item(1));   // first entry

            Param(unit.parameters, "PermeatePressure").value = Pp;
            Param(unit.parameters, "MembraneArea").value = Area;

            // First Validate discovers the compounds and APPENDS permeance params — without replacing the
            // collection object or moving the fixed entries.
            string msg = "";
            unit.Validate(ref msg);
            Assert.Same(coll, (ICapeCollection)unit.parameters);
            Assert.Same(permPressure, Param(unit.parameters, "PermeatePressure"));
            Assert.Same(permPressure, (ICapeParameter)((ICapeCollection)unit.parameters).Item(1));
            Assert.True(coll.Count() > fixedCount, "permeance parameters were appended");
            var permAmmonia = Param(unit.parameters, "Permeance_ammonia");

            // A second Validate and a Calculate must not disturb the collection identity, order or item refs.
            Param(unit.parameters, "Permeance_ammonia").value = Perm[0];
            Param(unit.parameters, "Permeance_hydrogen").value = Perm[1];
            Param(unit.parameters, "Permeance_nitrogen").value = Perm[2];
            Assert.True(unit.Validate(ref msg), msg);
            int countAfterValidate = ((ICapeCollection)unit.parameters).Count();
            unit.Calculate();

            Assert.Same(coll, (ICapeCollection)unit.parameters);
            Assert.Same(permPressure, (ICapeParameter)((ICapeCollection)unit.parameters).Item(1));
            Assert.Same(permAmmonia, Param(unit.parameters, "Permeance_ammonia"));
            Assert.Equal(countAfterValidate, ((ICapeCollection)unit.parameters).Count());
        }

        [Fact]
        public void EnsureEditableParameters_DiscoversCompoundsOnce()
        {
            // Exercises the GUI-free discovery step that Edit() runs before showing its dialog. (Edit() itself
            // shows a modal WinForms dialog and cannot run headlessly; its return value is dialog-driven.)
            var unit = new MembraneUnitOperation();
            unit.Initialize();
            var ports = (ICapeCollection)unit.ports;
            ((ICapeUnitPort)ports.Item("Feed")).Connect(MockMaterialObject.Feed(Ids, Tk, Pr, Nf, Feed));
            ((ICapeUnitPort)ports.Item("Retentate")).Connect(new MockMaterialObject(Ids));
            ((ICapeUnitPort)ports.Item("Permeate")).Connect(new MockMaterialObject(Ids));

            // First call discovers the compounds → collection changed (the PME should re-read).
            Assert.True(unit.EnsureEditableParameters());
            // Second call changes nothing.
            Assert.False(unit.EnsureEditableParameters());
        }
    }

    internal static class ComErrorHr
    {
        public const int BadInvOrder = unchecked((int)0x80040511);
        public const int SolvingError = unchecked((int)0x80040510);
    }
}
