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

        public static Word Increment(Word value)
        {
            return new Word { Int64 = value.Int64 + 1 };
        }

        public static Word Add(Word a, Word b)
        {
            return new Word { Int64 = a.Int64 + b.Int64 };
        }

        public static Word And(Word a, Word b)
        {
            return new Word { Int64 = a.Int64 & b.Int64 };
        }

        public static Word Or(Word a, Word b)
        {
            return new Word { Int64 = a.Int64 | b.Int64 };
        }

        public static Word ShiftLeft(Word a, Word b)
        {
            return new Word { Int64 = a.Int64 << b.Int32 };
        }

        public static Word ShiftRight(Word a, Word b)
        {
            return new Word { Int64 = a.Int64 >> b.Int32 };
        }
    }
}