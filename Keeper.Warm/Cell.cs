namespace Keeper.Warm
{
    public struct Cell
    {
        private int value;

        public Tag Tag
        {
            get
            {
                return (Tag)(this.value >> 28);
            }
        }
        
        public Address Address
        {
            get
            {
                return new Address(this.value & 0x0FFFFFFF);
            }
        }
        
        public int Value
        {
            get
            {
                return this.value;
            }
        }

        public Cell(int value)
        {
            this.value = value;
        }

        public Cell(Tag tag, Address address)
        {
            this.value = ((int)tag << 28) | address.Value;
        }
        
        public override string ToString()
        {
            return string.Format("<{0}, {1}>", this.Tag, this.Address);
        }
    }
}
