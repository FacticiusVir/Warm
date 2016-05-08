using Sprache;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Keeper.Warm.Prolog
{
    public static class Parser
    {
        private static Parser<AtomInfo> AtomParser = from name in (from first in Parse.Lower
                                                                   from tail in Parse.LetterOrDigit.Many()
                                                                   select new[] { first }.Concat(tail)).Text().Token()
                                                     select new AtomInfo(name);

        private static Parser<VariableInfo> VariableParser = from name in (from first in Parse.Upper
                                                                           from tail in Parse.LetterOrDigit.Many()
                                                                           select new[] { first }.Concat(tail)).Text().Token()
                                                             select new VariableInfo(name);

        private static Parser<CompoundTermInfo> CompoundTermParserRef = Parse.Ref(() => CompoundTermParser);

        private static Parser<ITermInfo> TermParser = ((Parser<ITermInfo>)CompoundTermParserRef).Or(AtomParser).Or(VariableParser);

        private static Parser<CompoundTermInfo> CompoundTermParser = from header in AtomParser
                                                                     from open in Parse.Char('(')
                                                                     from terms in TermParser.DelimitedBy(Parse.Char(','))
                                                                     from close in Parse.Char(')')
                                                                     select new CompoundTermInfo(header, terms);

        private static Parser<Rule> FactParser = from head in CompoundTermParser.Token()
                                                 from end in Parse.Char('.')
                                                 select CreateRule(head);

        private static Parser<Rule> PredicateParser = from head in CompoundTermParser.Token()
                                                      from bind in Parse.String(":-").Token()
                                                      from goals in CompoundTermParser.DelimitedBy(Parse.Char(','))
                                                      from end in Parse.Char('.')
                                                      select CreateRule(head, goals);

        private static Rule CreateRule(CompoundTermInfo headInfo, IEnumerable<CompoundTermInfo> goals = null)
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
                return new Rule(head, goals.Select(x => (CompoundTerm)x.CreateTerm(resolve)).ToArray());
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
            var goalInfo = CompoundTermParser.DelimitedBy(Parse.Char(',')).Parse(input);

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

            return goalInfo.Select(x => (CompoundTerm)x.CreateTerm(resolve)).ToArray();
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

        private interface ITermInfo
        {
            ITerm CreateTerm(Func<string, Variable> resolve);
        }
    }
}
