using System.Runtime.InteropServices;

namespace Core.Common
{
    [StructLayout(LayoutKind.Sequential, Size = 1)]
    public readonly struct Unit
    {
        public static readonly Unit Value = default;
    }
}
