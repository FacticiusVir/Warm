using Sprache;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Keeper.Warm.Prolog
{
    public static class Parser
    {
        private static Parser<ITermInfo> TermParserRef = Parse.Ref(() => TermParser);

        private static Parser<ListInfo> ListParser = from open in Parse.Char('[')
                                                     from items in TermParserRef.DelimitedBy(Parse.Char(',')).Optional()
                                                     from tail in (from bar in Parse.Char('|')
                                                                   from tailTerm in TermParserRef
                                                                   select tailTerm).Optional()
                                                     from close in Parse.Char(']')
                                                     select new ListInfo(items.GetOrElse(Enumerable.Empty<ITermInfo>()), tail.GetOrDefault());

        private static Parser<AtomInfo> AtomParser = from name in (from first in Parse.Lower
                                                                   from tail in Parse.LetterOrDigit.Many()
                                                                   select new[] { first }.Concat(tail)).Text().Token()
                                                     select new AtomInfo(name);

        private static Parser<AtomInfo> NumberParser = from name in (from first in Parse.Digit
                                                                     from tail in Parse.LetterOrDigit.Many()
                                                                     select new[] { first }.Concat(tail)).Text().Token()
                                                       select new AtomInfo("#" + name);

        private static Parser<VariableInfo> VariableParser = from name in (from first in Parse.Upper
                                                                           from tail in Parse.LetterOrDigit.Many()
                                                                           select new[] { first }.Concat(tail)).Text()
                                                             select new VariableInfo(name);

        private static Parser<CompoundTermInfo> CutParser = from symbol in Parse.Char('!').Token()
                                                            select new CompoundTermInfo(new AtomInfo("_cut"), Enumerable.Empty<ITermInfo>());

        private static Parser<CompoundTermInfo> CompoundTermParser = from header in AtomParser
                                                                     from open in Parse.Char('(')
                                                                     from terms in TermParserRef.DelimitedBy(Parse.Char(','))
                                                                     from close in Parse.Char(')')
                                                                     select new CompoundTermInfo(header, terms);

        private static Parser<ITermInfo> TermParser = (((Parser<ITermInfo>)ListParser).Or(CompoundTermParser).Or(AtomParser).Or(VariableParser).Or(NumberParser)).Token();

        private static Parser<Rule> FactParser = from head in CompoundTermParser.Token()
                                                 from end in Parse.Char('.')
                                                 select CreateRule(head);

        private static Parser<IEnumerable<ITermInfo>> GoalParser = ((Parser<ITermInfo>)CompoundTermParser).Or(AtomParser).Or(CutParser).DelimitedBy(Parse.Char(','));

        private static Parser<Rule> PredicateParser = from head in CompoundTermParser.Token()
                                                      from bind in Parse.String(":-").Token()
                                                      from goals in GoalParser
                                                      from end in Parse.Char('.')
                                                      select CreateRule(head, goals);

        private static Rule CreateRule(CompoundTermInfo headInfo, IEnumerable<ITermInfo> goals = null)
        {
            var variableNamespace = new Dictionary<string, Variable>();

            Func<string, Variable> resolve = x =>
            {
                Variable result;

                if (!variableNamespace.TryGetValue(x, out result))
                {
                    result = new Variable(x);
                    variableNamespace.Add(x, result);
                }

                return result;
            };

            var head = (CompoundTerm)headInfo.CreateTerm(resolve);

            if (goals == null)
            {
                return new Rule(head);
            }
            else
            {
                return new Rule(head, CreateGoals(goals, resolve));
            }
        }

        private static Parser<Rule> RuleParser = FactParser.Or(PredicateParser);

        private static Parser<IEnumerable<Rule>> FileParser = from rules in RuleParser.Many().End()
                                                              select rules;

        public static Rule[] ParseFile(string input)
        {
            return FileParser.Parse(input).ToArray();
        }

        public static CompoundTerm[] ParseQuery(string input)
        {
            var goalInfo = (from goals in GoalParser
                            from stop in Parse.Char('.').End()
                            select goals).Parse(input);

            var variableNamespace = new Dictionary<string, Variable>();

            Func<string, Variable> resolve = x =>
            {
                Variable result;

                if (!variableNamespace.TryGetValue(x, out result))
                {
                    result = new Variable(x);
                    variableNamespace.Add(x, result);
                }

                return result;
            };

            return CreateGoals(goalInfo, resolve);
        }

        private static CompoundTerm[] CreateGoals(IEnumerable<ITermInfo> goals, Func<string, Variable> resolve)
        {
            return goals.Select(x =>
            {
                var term = x.CreateTerm(resolve);

                var termAsAtom = term as Atom;

                if (termAsAtom != null)
                {
                    return new CompoundTerm(termAsAtom, Enumerable.Empty<ITerm>());
                }
                else
                {
                    return (CompoundTerm)term;
                }
            }).ToArray();
        }

        private class AtomInfo
            : ITermInfo
        {
            private string name;

            public AtomInfo(string name)
            {
                this.name = name;
            }

            public ITerm CreateTerm(Func<string, Variable> resolve)
            {
                return new Atom(this.name);
            }
        }

        private class VariableInfo
            : ITermInfo
        {
            private string name;

            public VariableInfo(string name)
            {
                this.name = name;
            }

            public ITerm CreateTerm(Func<string, Variable> resolve)
            {
                return resolve(this.name);
            }
        }

        private class CompoundTermInfo
            : ITermInfo
        {
            private ITerm header;
            private IEnumerable<ITermInfo> terms;

            public CompoundTermInfo(AtomInfo header, IEnumerable<ITermInfo> terms)
            {
                this.header = header.CreateTerm(null);
                this.terms = terms;
            }

            public ITerm CreateTerm(Func<string, Variable> resolve)
            {
                return new CompoundTerm((Atom)this.header, this.terms.Select(x => x.CreateTerm(resolve)));
            }
        }

        private class ListInfo
            : ITermInfo
        {
            private IEnumerable<ITermInfo> items;
            private ITermInfo tail;

            public ListInfo(IEnumerable<ITermInfo> items, ITermInfo tail)
            {
                this.items = items;
                this.tail = tail;
            }

            public ITerm CreateTerm(Func<string, Variable> resolve)
            {
                var tailTerm =
                    this.tail != null
                    ? this.tail.CreateTerm(resolve)
                    : new Atom("_emptyList");

                foreach (var item in this.items.Reverse())
                {
                    tailTerm = new CompoundTerm("_list", item.CreateTerm(resolve), tailTerm);
                }

                return tailTerm;
            }
        }

        private interface ITermInfo
        {
            ITerm CreateTerm(Func<string, Variable> resolve);
        }
    }
}
