using Keeper.MakeSomething;
using System;
using System.Linq;

namespace Keeper.Warm
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var testHost = new Host();

            var variableX = new Variable("X");
            var variableY = new Variable("Y");
            var variableZ = new Variable("Z");
            var variableW = new Variable("W");

            testHost.AddRule(new Rule(new CompoundTerm("a", new Atom("a"), new Atom("a"))));
            testHost.AddRule(new Rule(new CompoundTerm("a", new Atom("b"), new Atom("b"))));
            testHost.AddRule(new Rule(new CompoundTerm("a", new Atom("c"), new Atom("c"))));
            testHost.AddRule(new Rule(new CompoundTerm("a", new Atom("c"), new Atom("d"))));
            
            RunQuery(testHost, new CompoundTerm("a", variableX, variableX));
            RunQuery(testHost, new CompoundTerm("a", new Atom("c"), variableX));
            RunQuery(testHost, new CompoundTerm("a", new Atom("c"), variableY), new CompoundTerm("a", variableY, variableZ));

            Console.WriteLine("Done");

            Console.ReadLine();
        }

        private static void RunQuery(Host testHost, params CompoundTerm[] query)
        {
            var results = testHost.Query(query);

            while (results.Success)
            {
                foreach (var variable in results.Variables)
                {
                    Console.WriteLine("{0}: {1}", variable, results.GetVariable(variable));
                }

                if (!results.Variables.Any())
                {
                    Console.WriteLine("yes");
                }

                if (results.Continue())
                {
                    Console.WriteLine("Continue");
                }
            }

            Console.WriteLine();
        }
    }
}