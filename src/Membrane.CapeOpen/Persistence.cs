using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace Membrane.CapeOpen
{
    /// <summary>
    /// COM <c>IPersistStreamInit</c> (not exposed by the BCL). The PME calls these to persist the unit's
    /// configuration inside the flowsheet file. Methods that return an HRESULT use <see cref="PreserveSigAttribute"/>.
    /// </summary>
    [ComImport]
    [Guid("7FD52380-4E07-101B-AE2D-08002B2EC713")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IPersistStreamInit
    {
        void GetClassID(out Guid pClassID);

        [PreserveSig] int IsDirty();

        void Load(IStream pStm);

        void Save(IStream pStm, [MarshalAs(UnmanagedType.Bool)] bool fClearDirty);

        void GetSizeMax(out long pcbSize);

        void InitNew();
    }

    /// <summary>Helpers to read/write a whole byte buffer through a COM <see cref="IStream"/>.</summary>
    internal static class StreamPersistence
    {
        public static void WriteAll(IStream stm, byte[] data)
        {
            // Write a 4-byte length prefix then the payload, so Load knows how much to read.
            byte[] len = BitConverter.GetBytes(data.Length);
            stm.Write(len, len.Length, IntPtr.Zero);
            if (data.Length > 0)
                stm.Write(data, data.Length, IntPtr.Zero);
        }

        public static byte[] ReadAll(IStream stm)
        {
            byte[] len = new byte[4];
            int got = ReadExact(stm, len, 4);
            if (got < 4) return Array.Empty<byte>();
            int n = BitConverter.ToInt32(len, 0);
            if (n <= 0) return Array.Empty<byte>();
            byte[] buf = new byte[n];
            ReadExact(stm, buf, n);
            return buf;
        }

        private static int ReadExact(IStream stm, byte[] buffer, int count)
        {
            // IStream.Read reads into buffer; use the pcbRead out param to track progress.
            IntPtr pRead = Marshal.AllocHGlobal(sizeof(int));
            try
            {
                int total = 0;
                while (total < count)
                {
                    byte[] tmp = total == 0 ? buffer : new byte[count - total];
                    stm.Read(tmp, count - total, pRead);
                    int read = Marshal.ReadInt32(pRead);
                    if (read <= 0) break;
                    if (total != 0) Array.Copy(tmp, 0, buffer, total, read);
                    total += read;
                }
                return total;
            }
            finally { Marshal.FreeHGlobal(pRead); }
        }
    }
}
