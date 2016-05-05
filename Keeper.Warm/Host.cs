using Keeper.MakeSomething;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Keeper.Warm
{
    public class Host
    {
        private Machine machine = new Machine();
        private Compiler compiler = new Compiler();
        private Dictionary<RuleLabel, RuleData> ruleLookup = new Dictionary<RuleLabel, RuleData>();
        private Dictionary<FunctorDescriptor, int> functorLookup = new Dictionary<FunctorDescriptor, int>();
        private int topOfCodePointer = 128;

        private int LookupRulePointer(string name, int arity)
        {
            RuleLabel label = new RuleLabel()
            {
                Name = name,
                Arity = arity
            };

            RuleData result;

            if (!this.ruleLookup.TryGetValue(label, out result))
            {
                this.machine.SetCode(new Opcode[]
                    {
                        Opcode.Fail,
                        Opcode.Nop,
                        Opcode.Nop,
                        Opcode.Nop
                    }, this.topOfCodePointer);

                result = new RuleData()
                {
                    IsDefined = false,
                    FirstThunkPointer = this.topOfCodePointer,
                    LastThunkPointer = this.topOfCodePointer
                };

                this.topOfCodePointer += 4;

                this.ruleLookup[label] = result;
            }

            return result.FirstThunkPointer;
        }

        public void AddRule(Rule rule)
        {
            RuleLabel label = new RuleLabel()
            {
                Name = rule.Head.Header.Token,
                Arity = rule.Head.Terms.Count()
            };

            int rulePointer = this.topOfCodePointer;

            int compiledLength = CompileRule(rule);

            this.topOfCodePointer += compiledLength;

            RuleData ruleData;

            if (!this.ruleLookup.TryGetValue(label, out ruleData))
            {
                machine.SetCode(new Opcode[]
                {
                    Opcode.Nop,
                    Opcode.Nop,
                    Opcode.BranchAbsolute,
                    (Opcode)rulePointer
                }, this.topOfCodePointer);

                ruleData = new RuleData
                {
                    IsDefined = true,
                    FirstThunkPointer = this.topOfCodePointer,
                    LastThunkPointer = this.topOfCodePointer
                };

                this.topOfCodePointer += 4;

                this.ruleLookup[label] = ruleData;
            }
            else if (ruleData.IsDefined)
            {
                machine.SetCode(new Opcode[]
                {
                    Opcode.Nop,
                    Opcode.Nop,
                    Opcode.BranchAbsolute,
                    (Opcode)rulePointer
                }, this.topOfCodePointer);

                machine.SetCode(new[]
                {
                    Opcode.ChoicePoint,
                    (Opcode)this.topOfCodePointer,
                }, ruleData.LastThunkPointer);

                ruleData.LastThunkPointer = this.topOfCodePointer;

                this.topOfCodePointer += 4;
            }
            else
            {
                machine.SetCode(new Opcode[]
                {
                    Opcode.Nop,
                    Opcode.Nop,
                    Opcode.BranchAlways,
                    (Opcode)rulePointer
                }, ruleData.FirstThunkPointer);

                ruleData.IsDefined = true;
            }
        }

        private int CompileRule(Rule rule)
        {
            var generator = new CodeGenerator(rule.Head.Terms.Count(), this.LookupRulePointer, this.LookupFunctor);

            this.compiler.CompileRule(rule, generator);

            var compiledRule = generator.Generate().Cast<Opcode>().ToArray();

            machine.SetCode(compiledRule, this.topOfCodePointer);

            return compiledRule.Length;
        }

        public QueryResult Query(params CompoundTerm[] goals)
        {
            return this.Query((IEnumerable<CompoundTerm>)goals);
        }

        public QueryResult Query(IEnumerable<CompoundTerm> goals)
        {
            var variables = GetVariables(goals).Distinct().Reverse().ToArray();

            var queryRule = new Rule(new CompoundTerm("_query", variables), goals.ToArray());

            int queryCodeLength = this.CompileRule(queryRule);

            bool success = this.machine.Start(this.topOfCodePointer, variables.Count());

            var result = new QueryResult(success, variables, this.machine);

            return result;
        }

        private IEnumerable<Variable> GetVariables(IEnumerable<CompoundTerm> goals)
        {
            var subCompoundTerms = new List<CompoundTerm>();

            foreach (var goal in goals)
            {
                foreach (var term in goal.Terms)
                {
                    var termAsVariable = term as Variable;

                    if (termAsVariable != null)
                    {
                        yield return termAsVariable;
                    }
                    else
                    {
                        var termAsCompound = term as CompoundTerm;

                        if (termAsCompound != null)
                        {
                            subCompoundTerms.Add(termAsCompound);
                        }
                    }
                }
            }

            if (subCompoundTerms.Any())
            {
                foreach (var variable in GetVariables(subCompoundTerms))
                {
                    yield return variable;
                }
            }
        }

        private int LookupFunctor(FunctorDescriptor functor)
        {
            int result;

            if (!this.functorLookup.TryGetValue(functor, out result))
            {
                result = this.machine.AddFunctor(functor);

                this.functorLookup[functor] = result;
            }

            return result;
        }

        private struct RuleLabel
            : IEquatable<RuleLabel>
        {
            public string Name;
            public int Arity;

            public bool Equals(RuleLabel other)
            {
                return this.Name == other.Name
                    && this.Arity == other.Arity;
            }
        }

        private class RuleData
        {
            public bool IsDefined;
            public int FirstThunkPointer;
            public int LastThunkPointer;
        }
    }
}
