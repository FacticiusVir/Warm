using Keeper.Warm;
using Keeper.Warm.Prolog;
using System;
using System.Linq;

namespace Keeper.Warm
{
    public static class Program
    {
        // This is the classic Greek Gods program, here duplicated from Dan Tobin (see http://www.norsemathology.org/wiki/index.php?title=Dan_Tobin%27s_Prolog_Project)
        private static string testFile = @"
parent(cronus,hestia).
parent(cronus,pluto).
parent(cronus,poseidon).
parent(cronus,zeus).
parent(cronus,hera).
parent(cronus,demeter).
parent(rhea, hestia).
parent(rhea, pluto).
parent(rhea, poseidon).
parent(rhea, zeus).
parent(rhea, hera).
parent(rhea, demeter).

parent(zeus, athena).

parent(zeus, ares).
parent(zeus, hebe).
parent(zeus, hephaestus).
parent(hera, ares).
parent(hera, hebe).
parent(hera, hephaestus).

parent(zeus, persephone).
parent(demeter, persephone).


male(cronus).
male(pluto).
male(poseidon).
male(zeus).
male(ares).
male(hephaestus).
female(rhea).
female(hestia).
female(hera).
female(demeter).
female(athena).
female(hebe).
female(persephone).


isFather(X, Y):-
	male(X),
	parent(X, Y).

isMother(X, Y):-
	female(X),
	parent(X, Y).

isDaughter(X, Y):-
	female(X),
	parent(Y, X).

isSon(X, Y):-
	male(X),
	parent(Y, X).

isAncestor(X, Y):-
	parent(X, Y).
isAncestor(X, Y):-
	parent(X, T),
	parent(T, Y).";

        public static void Main(string[] args)
        {
            var testHost = new Host();

            var rules = Parser.ParseFile(testFile);

            foreach(var rule in rules)
            {
                testHost.AddRule(rule);
            }

            RunQuery(testHost, Parser.ParseQuery("isAncestor(Ancestor, Descendant)"));

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

            Console.WriteLine("no");

            Console.WriteLine();
        }
    }
}