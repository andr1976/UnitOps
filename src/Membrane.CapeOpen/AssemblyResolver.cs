using System;
using System.IO;
using System.Reflection;

namespace Membrane.CapeOpen
{
    /// <summary>
    /// Resolves managed dependencies (e.g. MembraneCore.dll) from this assembly's own directory when the
    /// component is activated inside a PME whose application base is elsewhere (COFE). Without this hook the
    /// CLR probes the PME's directory and fails to find our sibling DLLs. Idempotent; hook once.
    /// </summary>
    internal static class AssemblyResolver
    {
        private static bool _installed;
        private static readonly object Gate = new object();

        internal static void Ensure()
        {
            if (_installed) return;
            lock (Gate)
            {
                if (_installed) return;
                AppDomain.CurrentDomain.AssemblyResolve += Resolve;
                _installed = true;
            }
        }

        private static Assembly? Resolve(object? sender, ResolveEventArgs args)
        {
            try
            {
                string dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? ".";
                string simpleName = new AssemblyName(args.Name).Name ?? string.Empty;
                if (simpleName.Length == 0) return null;

                foreach (var ext in new[] { ".dll", ".exe" })
                {
                    string candidate = Path.Combine(dir, simpleName + ext);
                    if (File.Exists(candidate))
                    {
                        Diagnostics.Log($"AssemblyResolve: {simpleName} -> {candidate}");
                        return Assembly.LoadFrom(candidate);
                    }
                }
            }
            catch (Exception ex) { Diagnostics.Log("AssemblyResolve failed: " + ex.Message); }
            return null;
        }
    }
}
