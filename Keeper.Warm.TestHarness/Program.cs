using Keeper.Warm;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Keeper.Warm
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                var machine = new Machine();

                var derefToken = new MethodToken(1, 1);

                machine.DefineMethod(derefToken, new Opcode[]
                {
                }, null);

                var testMethodToken = new MethodToken(0, 0);

                machine.DefineMethod(testMethodToken, new Opcode[]
                {
                    Opcode.LoadLocalAddress,
                    0,
                    Opcode.Call,
                    0
                }, new[] { derefToken }, 1);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception thrown: {0}", ex.Message);

                Console.ReadLine();
            }
        }
    }
}