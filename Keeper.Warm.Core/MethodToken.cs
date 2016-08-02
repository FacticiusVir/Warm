namespace Keeper.Warm
{
    public class MethodToken
    {
        public MethodToken(int arity, int returnValues = 0)
        {
            this.Arity = arity;
            this.ReturnValues = returnValues;
        }

        public int Arity
        {
            get;
            private set;
        }

        public int ReturnValues
        {
            get;
            private set;
        }
    }
}