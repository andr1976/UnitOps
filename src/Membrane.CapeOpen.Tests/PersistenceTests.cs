using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using CAPEOPEN;
using Xunit;

namespace Membrane.CapeOpen.Tests
{
    /// <summary>
    /// Validates that a computed unit's state (permeances, stage cut, AND the position profiles) survives an
    /// IPersistStreamInit Save → Load round-trip — i.e. propagates from COFE's solve "worker" instance to the
    /// displayed instance. This is the fix for "profiles show zeros after a whole-flowsheet solve".
    /// </summary>
    public class PersistenceTests
    {
        [DllImport("ole32.dll")]
        private static extern int CreateStreamOnHGlobal(IntPtr hGlobal, bool fDeleteOnRelease, out IStream ppstm);

        private static readonly string[] Ids = { "ammonia", "hydrogen", "nitrogen" };
        private static readonly double[] Feed = { 0.45, 0.25, 0.30 };
        private static readonly double[] Perm = { 2.63e-10, 8.35e-11, 1.72e-11 };
        private const double Tk = 323.15, Pr = 1.0e6, Pp = 1.3e5, Nf = 1.0, Area = 200.0;

        private static ICapeParameter P(object coll, string name) => (ICapeParameter)((ICapeCollection)coll).Item(name);
        private static double[] Arr(object v)
        {
            if (v is double[] d) return d;
            var o = (object[])v; var a = new double[o.Length];
            for (int i = 0; i < o.Length; i++) a[i] = Convert.ToDouble(o[i]);
            return a;
        }

        private static MembraneUnitOperation SolvedUnit()
        {
            var u = new MembraneUnitOperation();
            u.Initialize();
            var ports = (ICapeCollection)u.ports;
            ((ICapeUnitPort)ports.Item("Feed")).Connect(MockMaterialObject.Feed(Ids, Tk, Pr, Nf, Feed));
            ((ICapeUnitPort)ports.Item("Retentate")).Connect(new MockMaterialObject(Ids));
            ((ICapeUnitPort)ports.Item("Permeate")).Connect(new MockMaterialObject(Ids));
            P(u.parameters, "PermeatePressure").value = Pp;
            P(u.parameters, "MembraneArea").value = Area;
            string msg = "";
            u.Validate(ref msg);                       // creates per-compound params
            P(u.parameters, "Permeance_ammonia").value = Perm[0];
            P(u.parameters, "Permeance_hydrogen").value = Perm[1];
            P(u.parameters, "Permeance_nitrogen").value = Perm[2];
            u.Validate(ref msg);
            u.Calculate();
            return u;
        }

        [Fact]
        public void SaveLoad_RestoresStageCutAndProfiles()
        {
            var src = SolvedUnit();
            double stageCutSrc = Convert.ToDouble(P(src.parameters, "StageCut").value);
            var posSrc = Arr(P(src.parameters, "Profile_Position").value);
            var retSrc = Arr(P(src.parameters, "Profile_Retentate_ammonia").value);
            Assert.True(stageCutSrc > 0, "precondition: source solved");
            Assert.True(retSrc[0] != retSrc[99], "precondition: source profile populated");

            // Save the solved ("worker") unit to an in-memory stream.
            CreateStreamOnHGlobal(IntPtr.Zero, true, out IStream stm);
            var srcPersist = (IPersistStreamInit)src;
            srcPersist.Save(stm, false);
            stm.Seek(0, 0, IntPtr.Zero);

            // Fresh ("display") instance: Initialize + Load, no Calculate.
            var dst = new MembraneUnitOperation();
            dst.Initialize();
            ((IPersistStreamInit)dst).Load(stm);

            // Fixed profiles restore immediately (position, stage cut) — no compound discovery needed.
            Assert.Equal(stageCutSrc, Convert.ToDouble(P(dst.parameters, "StageCut").value), 6);
            var posDst = Arr(P(dst.parameters, "Profile_Position").value);
            Assert.Equal(1.0, posDst[99], 6);
            Assert.Equal(posSrc[99], posDst[99], 6);

            // Per-compound profiles restore once the compounds are discovered (Validate on the display unit).
            var ports = (ICapeCollection)dst.ports;
            ((ICapeUnitPort)ports.Item("Feed")).Connect(MockMaterialObject.Feed(Ids, Tk, Pr, Nf, Feed));
            ((ICapeUnitPort)ports.Item("Retentate")).Connect(new MockMaterialObject(Ids));
            ((ICapeUnitPort)ports.Item("Permeate")).Connect(new MockMaterialObject(Ids));
            string msg = "";
            dst.Validate(ref msg);

            var retDst = Arr(P(dst.parameters, "Profile_Retentate_ammonia").value);
            Assert.Equal(retSrc.Length, retDst.Length);
            for (int k = 0; k < retSrc.Length; k++)
                Assert.Equal(retSrc[k], retDst[k], 6);

            // Collected (cumulative) permeate profile round-trips too.
            var collSrc = Arr(P(src.parameters, "Profile_PermeateCollected_ammonia").value);
            var collDst = Arr(P(dst.parameters, "Profile_PermeateCollected_ammonia").value);
            Assert.True(collSrc[99] > 0.0, "precondition: collected profile populated");
            for (int k = 0; k < collSrc.Length; k++)
                Assert.Equal(collSrc[k], collDst[k], 6);

            // Permeances restored too.
            Assert.Equal(Perm[0], Convert.ToDouble(P(dst.parameters, "Permeance_ammonia").value), 15);
        }
    }
}
