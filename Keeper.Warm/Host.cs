using System;
using System.Collections.Generic;
using System.Linq;

namespace Keeper.Warm
{
    public class Host
    {
        private Machine machine = new Machine();
        private Compiler compiler = new Compiler();
        private Dictionary<RuleLabel, RuleData> ruleLookup = new Dictionary<RuleLabel, RuleData>();
        private Dictionary<FunctorDescriptor, int> functorLookup = new Dictionary<FunctorDescriptor, int>();
        private int topOfCodePointer = 128;

        private const int thunkSize = 4;

        public Host()
        {
            this.AddBuiltin("fail", 0, new Opcode[]
                {
                    Opcode.Fail
                });

            this.AddBuiltin("nonvar", 1, new Opcode[]
                {
                    Opcode.Allocate,
                    (Opcode)0,
                    Opcode.LoadArgumentAddress0,
                    Opcode.Load,
                    Opcode.GetTag,
                    Opcode.LoadConstant,
                    (Opcode)Tag.Ref,
                    Opcode.BranchNotEqual,
                    (Opcode)3,
                    Opcode.Fail,
                    Opcode.Deallocate,
                    (Opcode)0,
                    Opcode.Proceed
                });

            int testCallbackIndex = this.machine.AddCallback(this.Call);

            this.AddBuiltin("call", 1, new Opcode[]
                {
                    Opcode.Allocate,
                    (Opcode)0,
                    Opcode.LoadArgumentAddress0,
                    Opcode.Load,
                    //Opcode.GetLevel,
                    //Opcode.StoreGlobalRegisterB0,
                    Opcode.Callback,
                    (Opcode)testCallbackIndex,
                    Opcode.Pop,
                    Opcode.Deallocate,
                    (Opcode)0,
                    Opcode.Proceed
                });
        }

        private bool Call(Machine machine)
        {
            var goalCell = new Cell(machine.DereferenceAndLoad(machine.StackPointer));

            if (goalCell.Tag != Tag.Str)
            {
                System.Diagnostics.Debug.WriteLine("Not a compound term");

                return false;
            }

            var functorCell = new Cell(machine.LoadFromStore(goalCell.Address));

            var functor = machine.GetFunctor(functorCell.Address.Pointer);

            var ruleLabel = new RuleLabel()
            {
                Name = functor.Name,
                Arity = functor.Arity
            };

            RuleData ruleData;

            if (!this.ruleLookup.TryGetValue(ruleLabel, out ruleData))
            {
                System.Diagnostics.Debug.WriteLine("No recognised rule for " + functor);

                return false;
            }

            for (int termIndex = 0; termIndex < functor.Arity; termIndex++)
            {
                machine.Push(new Cell(Tag.Ref, goalCell.Address + (functor.Arity - termIndex)));
            }

            machine.Call(ruleData.FirstThunkPointer);

            return true;
        }

        private void AddBuiltin(string name, int arity, Opcode[] code)
        {
            var label = new RuleLabel
            {
                Name = name,
                Arity = arity
            };

            int rulePointer = this.topOfCodePointer;

            this.machine.SetCode(code, this.topOfCodePointer);

            this.topOfCodePointer += code.Length;

            this.AddRule(label, rulePointer);
        }

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

                this.topOfCodePointer += thunkSize;

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

            var compiledRule = CompileRule(rule);

            int rulePointer = this.topOfCodePointer;

            machine.SetCode(compiledRule, this.topOfCodePointer);

            this.topOfCodePointer += compiledRule.Length;

            this.AddRule(label, rulePointer);
        }

        private void AddRule(RuleLabel label, int rulePointer)
        {
            RuleData ruleData;

            Console.WriteLine("Add rule {0} @ {1}", label, rulePointer);

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

                this.topOfCodePointer += thunkSize;

                this.ruleLookup[label] = ruleData;

                Console.WriteLine("Define new");
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

                this.topOfCodePointer += thunkSize;

                Console.WriteLine("Chain");
            }
            else
            {
                machine.SetCode(new Opcode[]
                {
                    Opcode.Nop,
                    Opcode.Nop,
                    Opcode.BranchAbsolute,
                    (Opcode)rulePointer
                }, ruleData.FirstThunkPointer);

                ruleData.IsDefined = true;

                Console.WriteLine("Define");
            }

            Console.WriteLine(ruleData);
            Console.WriteLine();
        }

        private Opcode[] CompileRule(Rule rule)
        {
            var generator = new CodeGenerator(rule.Head.Terms.Count(), this.LookupRulePointer, this.LookupFunctor);

            this.compiler.CompileRule(rule, generator);

            return generator.Generate().Cast<Opcode>().ToArray();
        }

        public QueryResult Query(params CompoundTerm[] goals)
        {
            return this.Query((IEnumerable<CompoundTerm>)goals);
        }

        public QueryResult Query(IEnumerable<CompoundTerm> goals)
        {
            var variables = GetVariables(goals).Distinct().ToArray();

            var queryRule = new Rule(new CompoundTerm("_query", variables), goals.ToArray());

            var compiledRule = this.CompileRule(queryRule);

            machine.SetCode(compiledRule, this.topOfCodePointer);

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

            public override string ToString()
            {
                return string.Format("{0}/{1}", this.Name, this.Arity);
            }
        }

        private class RuleData
        {
            public bool IsDefined;
            public int FirstThunkPointer;
            public int LastThunkPointer;

            public override string ToString()
            {
                return string.Format("Defined: {0}, First: {1}, Last: {2}", this.IsDefined, this.FirstThunkPointer, this.LastThunkPointer);
            }
        }
    }
}
