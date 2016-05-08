using Keeper.Warm;
using Keeper.Warm.Prolog;
using System;
using System.IO;
using System.Linq;

namespace Keeper.Warm
{
    public static class Program
    {
        // Included Prolog file is the classic Greek Gods program, here
        // duplicated from Dan Tobin
        // (see http://www.norsemathology.org/wiki/index.php?title=Dan_Tobin%27s_Prolog_Project)

        public static void Main(string[] args)
        {
            try
            {
                var testHost = new Host();

                var rules = Parser.ParseFile(File.ReadAllText(".\\Program.plg"));

                foreach (var rule in rules)
                {
                    testHost.AddRule(rule);
                }
                
                bool isRunning = true;

                do
                {
                    Console.Write("?");
                    string line = Console.ReadLine();

                    if(string.IsNullOrEmpty(line))
                    {
                        isRunning = false;
                    }
                    else
                    {
                        try
                        {
                            var query = Parser.ParseQuery(line);

                            RunQuery(testHost, query);
                        }
                        catch(Exception ex)
                        {
                            Console.WriteLine("Exception thrown: {0}", ex.Message);
                        }
                    }

                } while (isRunning);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception thrown: {0}", ex.Message);

                Console.ReadLine();
            }
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

            Console.WriteLine("no");

            Console.WriteLine();
        }
    }
}