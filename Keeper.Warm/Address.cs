namespace Keeper.Warm
{
    public struct Address
    {
        private int value;

        public Address(int value)
        {
            this.value = value;
        }

        public Address(AddressType type, int pointer)
        {
            this.value = ((int)type << 24) | (pointer & 0x00FFFFFF);
        }

        public AddressType Type
        {
            get
            {
                return (AddressType)(this.value >> 24);
            }
        }

        public int Pointer
        {
            get
            {
                int result = this.value & 0x00FFFFFF;

                return result >= 0x00800000
                    ? result + unchecked((int)0xFF000000)
                    : result;
            }
        }

        public int Value
        {
            get
            {
                return this.value;
            }
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
