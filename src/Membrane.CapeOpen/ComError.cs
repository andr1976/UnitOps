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
        private static bool _bannerWritten;

        // Off by default (no log file, no overhead). Set the env var MEMBRANE_CAPEOPEN_LOG (to any value)
        // to write membrane_capeopen.log next to the DLL for troubleshooting inside a PME.
        internal static bool Enabled { get; set; } =
            !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("MEMBRANE_CAPEOPEN_LOG"));

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
                {
                    if (!_bannerWritten) { _bannerWritten = true; WriteBanner(); }
                    System.IO.File.AppendAllText(Path, $"{DateTime.Now:HH:mm:ss.fff}  {message}{Environment.NewLine}");
                }
            }
            catch { /* never let logging throw across the COM boundary */ }
        }

        /// <summary>One-time-per-process banner: names the host PME, its bitness/PID and the adapter build,
        /// so a shared log file can be read back per session and attributed to COFE vs DWSIM vs Aspen etc.</summary>
        private static void WriteBanner()
        {
            try
            {
                var p = System.Diagnostics.Process.GetCurrentProcess();
                string host;
                try { host = p.MainModule?.FileName ?? p.ProcessName; } catch { host = p.ProcessName; }
                var asm = System.Reflection.Assembly.GetExecutingAssembly();
                var sb = new System.Text.StringBuilder();
                sb.AppendLine(new string('=', 78));
                sb.AppendLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}  ===== SESSION START =====");
                sb.AppendLine($"  HOST     : {host}  (PID {p.Id})");
                sb.AppendLine($"  Bitness  : {(Environment.Is64BitProcess ? "64-bit" : "32-bit")} process");
                sb.AppendLine($"  Adapter  : v{asm.GetName().Version}  @ {asm.Location}");
                sb.AppendLine(new string('=', 78));
                System.IO.File.AppendAllText(Path, sb.ToString());
            }
            catch { /* banner is best-effort */ }
        }
    }
}
