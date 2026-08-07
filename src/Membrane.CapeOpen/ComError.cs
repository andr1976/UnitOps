using System;
using System.Runtime.InteropServices;

namespace Membrane.CapeOpen
{
    /// <summary>
    /// Maps failures to CAPE-OPEN error HRESULTs (spec §8). Throwing a <see cref="COMException"/> with the
    /// right HRESULT is how a .NET COM server signals a specific CAPE error to the PME.
    /// </summary>
    internal static class ComError
    {
        // CapeErrorInterfaceHR values (spec §8).
        public const int ECapeUnknownHR = unchecked((int)0x80040501);
        public const int ECapeBadCOParameterHR = unchecked((int)0x80040504);
        public const int ECapeBadArgumentHR = unchecked((int)0x80040505);
        public const int ECapeInvalidArgumentHR = unchecked((int)0x80040506);
        public const int ECapeOutOfBoundsHR = unchecked((int)0x80040507);
        public const int ECapeNoImplHR = unchecked((int)0x80040509);
        public const int ECapeLimitedImplHR = unchecked((int)0x8004050A);
        public const int ECapeSolvingErrorHR = unchecked((int)0x80040510);
        public const int ECapeBadInvOrderHR = unchecked((int)0x80040511);

        public static CapeError Unknown(string msg) => Make(ECapeUnknownHR, "ECapeUnknown", msg);
        public static CapeError BadCOParameter(string msg) => Make(ECapeBadCOParameterHR, "ECapeBadCOParameter", msg);
        public static CapeError BadArgument(string msg) => Make(ECapeBadArgumentHR, "ECapeBadArgument", msg);
        public static CapeError InvalidArgument(string msg) => Make(ECapeInvalidArgumentHR, "ECapeInvalidArgument", msg);
        public static CapeError OutOfBounds(string msg) => Make(ECapeOutOfBoundsHR, "ECapeOutOfBounds", msg);
        public static CapeError NoImpl(string msg) => Make(ECapeNoImplHR, "ECapeNoImpl", msg);
        public static CapeError SolvingError(string msg) => Make(ECapeSolvingErrorHR, "ECapeSolvingError", msg);
        public static CapeError BadInvOrder(string msg) => Make(ECapeBadInvOrderHR, "ECapeBadInvOrder", msg);

        private static CapeError Make(int hr, string name, string msg)
        {
            Diagnostics.Log($"ERROR {name} 0x{hr:X8}: {msg}");
            return new CapeError(hr, name, msg);
        }
    }

    /// <summary>Lightweight file logger for debugging inside a PME (which may swallow error detail).</summary>
    internal static class Diagnostics
    {
        private static readonly object Gate = new object();
        private static string? _path;

        internal static bool Enabled { get; set; } = true;

        private static string Path
        {
            get
            {
                if (_path == null)
                {
                    string dir = System.IO.Path.GetDirectoryName(
                        System.Reflection.Assembly.GetExecutingAssembly().Location) ?? ".";
                    _path = System.IO.Path.Combine(dir, "membrane_capeopen.log");
                }
                return _path;
            }
        }

        internal static void Log(string message)
        {
            if (!Enabled) return;
            try
            {
                lock (Gate)
                    System.IO.File.AppendAllText(Path, $"{DateTime.Now:HH:mm:ss.fff}  {message}{Environment.NewLine}");
            }
            catch { /* never let logging throw across the COM boundary */ }
        }
    }
}
