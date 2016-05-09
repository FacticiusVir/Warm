using Keeper.Warm.Prolog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Keeper.Warm
{
    public static class Program
    {
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

                    if (string.IsNullOrEmpty(line))
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
                        catch (Exception ex)
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
                    var value = results.GetVariable(variable);

                    Console.WriteLine("{0}: {1}", variable, Format(value));
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

        private static string Format(ITerm value)
        {
            var valueAsAtom = value as Atom;

            if (valueAsAtom != null && valueAsAtom.Token == "_emptyList")
            {
                return "[]";
            }
            else
            {
                var valueAsCompound = value as CompoundTerm;

                if (valueAsCompound != null && valueAsCompound.Header.Token == "_list")
                {
                    var items = new List<string>();

                    var item = value;
                    var itemAsCompound = item as CompoundTerm;

                    while (itemAsCompound != null && itemAsCompound.Header.Token == "_list")
                    {
                        items.Add(Format(itemAsCompound.Terms.First()));

                        item = itemAsCompound.Terms.Skip(1).First();
                        itemAsCompound = item as CompoundTerm;
                    }

                    string tailString = "";

                    var itemAsAtom = item as Atom;

                    if (itemAsAtom == null || itemAsAtom.Token != "_emptyList")
                    {
                        tailString = "| " + Format(item);
                    }

                    return string.Format("[{0}{1}]", string.Join(", ", items), tailString);
                }
                else
                {
                    return value.ToString();
                }
            }
        }
    }
}