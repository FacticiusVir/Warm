namespace Keeper.Warm
{
    public class MethodToken
    {
        public MethodToken(int arity)
        {
            this.Arity = arity;
        }

        public int Arity
        {
            get;
            private set;
        }
    }
}