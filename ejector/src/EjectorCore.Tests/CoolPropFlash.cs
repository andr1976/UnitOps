using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using EjectorCore;

namespace EjectorCore.Tests;

/// <summary>
/// IFlash on the CoolProp shared library (C API, P/Invoke), mirroring the Python
/// reference's Thermo class (ejector1d.py) call for call, so the C# engine can be
/// regression-tested against the Python model on identical thermodynamics.
/// Test-only; the CAPE-OPEN unit uses the PME's property package instead.
/// The DLL is found through the COOLPROP_DLL environment variable or a sibling
/// CoolProp checkout (../CoolProp/build_shared/Release/CoolProp.dll next to the repo).
/// </summary>
public sealed class CoolPropFlash : IFlash, IDisposable
{
    private static readonly string? DllPath = FindDll();
    private static bool _resolverSet;
    private static readonly object Lock = new();

    public static bool Available => DllPath != null;

    private static string? FindDll()
    {
        var env = Environment.GetEnvironmentVariable("COOLPROP_DLL");
        if (!string.IsNullOrEmpty(env) && File.Exists(env)) return env;
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, ".git")))
            {
                var p = Path.Combine(dir.Parent!.FullName, "CoolProp", "build_shared", "Release", "CoolProp.dll");
                return File.Exists(p) ? p : null;
            }
            dir = dir.Parent;
        }
        return null;
    }

    private static void EnsureResolver()
    {
        lock (Lock)
        {
            if (_resolverSet) return;
            NativeLibrary.SetDllImportResolver(typeof(CoolPropFlash).Assembly, (name, asm, path) =>
                name == "CoolProp" && DllPath != null ? NativeLibrary.Load(DllPath) : IntPtr.Zero);
            _resolverSet = true;
        }
    }

    // ---- C API (CoolPropLib.h); C "long" is 32-bit on Windows
    [DllImport("CoolProp", CallingConvention = CallingConvention.Cdecl)]
    private static extern int AbstractState_factory(string backend, string fluids, ref int errcode, byte[] msg, int len);
    [DllImport("CoolProp", CallingConvention = CallingConvention.Cdecl)]
    private static extern void AbstractState_free(int handle, ref int errcode, byte[] msg, int len);
    [DllImport("CoolProp", CallingConvention = CallingConvention.Cdecl)]
    private static extern void AbstractState_set_fractions(int handle, double[] f, int n, ref int errcode, byte[] msg, int len);
    [DllImport("CoolProp", CallingConvention = CallingConvention.Cdecl)]
    private static extern void AbstractState_update(int handle, int pair, double v1, double v2, ref int errcode, byte[] msg, int len);
    [DllImport("CoolProp", CallingConvention = CallingConvention.Cdecl)]
    private static extern double AbstractState_keyed_output(int handle, int param, ref int errcode, byte[] msg, int len);
    [DllImport("CoolProp", CallingConvention = CallingConvention.Cdecl)]
    private static extern double AbstractState_keyed_output_satState(int handle, string state, int param, ref int errcode, byte[] msg, int len);
    [DllImport("CoolProp", CallingConvention = CallingConvention.Cdecl)]
    private static extern int get_param_index(string param);
    [DllImport("CoolProp", CallingConvention = CallingConvention.Cdecl)]
    private static extern int get_input_pair_index(string pair);
    [DllImport("CoolProp", CallingConvention = CallingConvention.Cdecl)]
    private static extern double Props1SI(string fluid, string output);

    private readonly int _h;
    private readonly byte[] _msg = new byte[1024];
    private readonly int _iT, _iS, _iD, _iQ, _iH, _iPhase, _iM;
    private readonly int _pairHP, _pairPS, _pairPT, _pairPQ;
    private double[]? _z;

    public CoolPropFlash(string fluid, string backend = "HEOS")
    {
        if (!Available) throw new InvalidOperationException("CoolProp.dll not found (set COOLPROP_DLL)");
        EnsureResolver();
        int err = 0;
        _h = AbstractState_factory(backend, fluid, ref err, _msg, _msg.Length);
        Check(err);
        IsMixture = fluid.Contains('&');
        _iT = get_param_index("T"); _iS = get_param_index("Smass"); _iD = get_param_index("Dmass");
        _iQ = get_param_index("Q"); _iH = get_param_index("Hmass"); _iPhase = get_param_index("Phase");
        _iM = get_param_index("molar_mass");
        _pairHP = get_input_pair_index("HmassP_INPUTS"); _pairPS = get_input_pair_index("PSmass_INPUTS");
        _pairPT = get_input_pair_index("PT_INPUTS"); _pairPQ = get_input_pair_index("PQ_INPUTS");
        TriplePointPressure = IsMixture ? 0.0 : Props1SI(fluid, "ptriple");
    }

    public bool IsMixture { get; }
    public double TriplePointPressure { get; }

    private void Check(int err)
    {
        if (err == 0) return;
        int n = Array.IndexOf(_msg, (byte)0);
        throw new FlashException(Encoding.ASCII.GetString(_msg, 0, n < 0 ? _msg.Length : n));
    }

    private void SetZ(double[]? z)
    {
        if (!IsMixture) return;
        if (z == null) throw new ArgumentException("mixture flash needs a composition z");
        if (_z != null && _z.SequenceEqual(z)) return;
        int err = 0;
        AbstractState_set_fractions(_h, z, z.Length, ref err, _msg, _msg.Length);
        Check(err);
        _z = (double[])z.Clone();
    }

    private double Out(int param)
    {
        int err = 0;
        double v = AbstractState_keyed_output(_h, param, ref err, _msg, _msg.Length);
        Check(err);
        return v;
    }

    private void Update(int pair, double v1, double v2)
    {
        int err = 0;
        AbstractState_update(_h, pair, v1, v2, ref err, _msg, _msg.Length);
        Check(err);
    }

    private FluidState State(double P, double h, double[]? z)
    {
        var st = new FluidState { P = P, H = h, T = Out(_iT), S = Out(_iS), Rho = Out(_iD), Z = z };
        double qm = Out(_iQ);
        if (qm > 0.0 && qm < 1.0)
        {
            int err = 0;
            st.RhoL = AbstractState_keyed_output_satState(_h, "liquid", _iD, ref err, _msg, _msg.Length);
            Check(err);
            st.RhoV = AbstractState_keyed_output_satState(_h, "gas", _iD, ref err, _msg, _msg.Length);
            Check(err);
            double v = 1 / st.Rho, vl = 1 / st.RhoL, vv = 1 / st.RhoV;
            st.Q = (v - vl) / (vv - vl);                     // mass vapour fraction
        }
        else if (qm == 0.0 || qm == 1.0)
        {
            st.Q = qm;
        }
        else
        {
            st.Q = (int)Out(_iPhase) == 0 ? -1.0 : 2.0;      // iphase_liquid = 0
        }
        return st;
    }

    public FluidState PH(double P, double h, double[]? z) { SetZ(z); Update(_pairHP, h, P); return State(P, h, z); }
    public FluidState PS(double P, double s, double[]? z) { SetZ(z); Update(_pairPS, P, s); return State(P, Out(_iH), z); }
    public FluidState PT(double P, double T, double[]? z) { SetZ(z); Update(_pairPT, P, T); return State(P, Out(_iH), z); }
    public FluidState PQ(double P, double q, double[]? z) { SetZ(z); Update(_pairPQ, P, q); return State(P, Out(_iH), z); }
    public double MolarMass(double[]? z) { SetZ(z); return Out(_iM); }

    public void Dispose()
    {
        int err = 0;
        AbstractState_free(_h, ref err, _msg, _msg.Length);
    }
}

/// <summary>[Fact] that is skipped when the CoolProp DLL is not available.</summary>
public sealed class CoolPropFactAttribute : FactAttribute
{
    public CoolPropFactAttribute()
    {
        if (!CoolPropFlash.Available) Skip = "CoolProp.dll not found (set COOLPROP_DLL)";
    }
}

/// <summary>[Theory] that is skipped when the CoolProp DLL is not available.</summary>
public sealed class CoolPropTheoryAttribute : TheoryAttribute
{
    public CoolPropTheoryAttribute()
    {
        if (!CoolPropFlash.Available) Skip = "CoolProp.dll not found (set COOLPROP_DLL)";
    }
}
