namespace Keeper.Warm
{
    internal struct Address
    {
        private long value;

        public Address(long value)
        {
            this.value = value;
        }

        public Address(AddressType type, long pointer)
        {
            this.value = ((long)type << 56) | (pointer & 0x00FFFFFF);
        }

        public AddressType Type
        {
            get
            {
                return (AddressType)(this.value >> 56);
            }
        }

        public long Pointer
        {
            get
            {
                long result = this.value & 0x00FFFFFFFFFFFFFF;

                return result >= 0x00FFFFFFFFFFFFFF
                    ? result + unchecked((long)0xFF00000000000000)
                    : result;
            }
        }

        public long Value
        {
            get
            {
                return this.value;
            }
        }

        public static Address operator +(Address baseAddress, long offset)
        {
            return new Address(baseAddress.Type, baseAddress.Pointer + offset);
        }

        public static Address operator -(Address baseAddress, long offset)
        {
            return new Address(baseAddress.Type, baseAddress.Pointer - offset);
        }

        public static Address operator +(Address baseAddress, int offset)
        {
            return new Address(baseAddress.Type, baseAddress.Pointer + offset);
        }

        public static Address operator -(Address baseAddress, int offset)
        {
            return new Address(baseAddress.Type, baseAddress.Pointer - offset);
        }

        public static Address operator ++(Address baseAddress)
        {
            return baseAddress + 1;
        }

        public static bool operator ==(Address left, Address right)
        {
            return left.value == right.value;
        }

        public static bool operator !=(Address left, Address right)
        {
            return left.value != right.value;
        }

        public static bool operator <(Address left, Address right)
        {
            return left.value < right.value;
        }

        public static bool operator >(Address left, Address right)
        {
            return left.value > right.value;
        }

        public override bool Equals(object obj)
        {
            return obj is Address
                && ((Address)obj) == this;
        }

        public override int GetHashCode()
        {
            return this.value.GetHashCode();
        }

        public override string ToString()
        {
            return string.Format("{0} {1}", this.Type, this.Pointer);
        }
    }
}
