using System.Runtime.InteropServices;

namespace Keeper.Warm
{
    [StructLayout(LayoutKind.Explicit)]
    internal struct Word
    {
        [FieldOffset(0)]
        public long Int64;

        [FieldOffset(0)]
        public int Int32;

        [FieldOffset(0)]
        public Address Address;
    }
}