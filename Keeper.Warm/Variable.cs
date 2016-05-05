namespace Keeper.MakeSomething
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