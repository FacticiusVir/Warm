namespace Keeper.Warm.Prolog
{
    public sealed class Variable
        : IListTail, ITerm
    {
        public Variable(string name)
        {
            this.Name = name;
        }

        public string Name
        {
            get;
            private set;
        }

        public override string ToString()
        {
            return "$" + this.Name;
        }
    }
}