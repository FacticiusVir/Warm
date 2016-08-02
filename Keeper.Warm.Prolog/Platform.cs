namespace Keeper.Warm.Prolog
{
    public class Platform
    {
        public static readonly MethodToken SetTag = new MethodToken(2, 1);

        private Machine machine;

        public Platform()
        {
            this.machine = new Machine();

            this.machine.DefineMethod(SetTag, new Opcode[]
            {

            }, null, 0);
        }
    }
}
