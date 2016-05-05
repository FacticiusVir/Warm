namespace Keeper.MakeSomething
{
    public sealed class Atom
        : ITerm
    {
        public Atom(string token)
        {
            this.Token = token;
        }

        public string Token
        {
            get;
            private set;
        }

        public override string ToString()
        {
            return this.Token;
        }
    }
}
