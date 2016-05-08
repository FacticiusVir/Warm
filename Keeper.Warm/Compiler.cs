using Keeper.Warm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Keeper.Warm
{
    public class Compiler
    {
        private struct StructureArgument
        {
            public ITerm Term;
            public Opcode GetAddress;
            public Local LocalOperand;
            public ArgumentType Type;
        }

        private enum ArgumentType
        {
            Variable,
            Value,
            Constant,
            List,
            EmptyList
        }

        public void CompileRule(Rule rule, ICodeGenerator generator)
        {
            string bodyString =
                rule.Goals.Any()
                ? " if " + string.Join(", ", rule.Goals.Select(x => x.ToString()))
                : "";

            Console.WriteLine(rule.Head + bodyString);

            var head = rule.Head;

            var structurePointerLocal = generator.DefineLocal();

            var derefAddressLocal = generator.DefineLocal();
            var resolvedArgumentLocal = generator.DefineLocal();
            var functorAddressLocal = generator.DefineLocal();

            var subTerms = new List<StructureArgument>();

            for (int argumentIndex = 0; argumentIndex < rule.Head.Terms.Count(); argumentIndex++)
            {
                var argument = rule.Head.Terms.ElementAt(argumentIndex);

                subTerms.Add(new StructureArgument
                {
                    Term = argument,
                    GetAddress = Opcode.LoadArgumentAddressBase | (Opcode)argumentIndex
                });
            }

            var assignedVariables = new Dictionary<Variable, Local>();

            while (subTerms.Any())
            {
                var termsToIterate = subTerms.ToArray();

                subTerms.Clear();

                foreach (var argument in termsToIterate)
                {
                    var compoundTerm = argument.Term as CompoundTerm;

                    if (compoundTerm != null)
                    {
                        var strLabel = generator.DefineLabel();
                        var failLabel = generator.DefineLabel();
                        var continueLabel = generator.DefineLabel();

                        var argumentFunctor = new FunctorDescriptor(compoundTerm);

                        Console.WriteLine("GetStructure {0}, {1}", argumentFunctor, argument.LocalOperand != null ? argument.LocalOperand.Index : ((int)argument.GetAddress & 8));

                        var subTermsToPopulate = new List<StructureArgument>();

                        foreach (var subTerm in compoundTerm.Terms)
                        {
                            subTermsToPopulate.Add(EvaluateSubTerm(generator, subTerms, assignedVariables, subTerm));
                        }

                        if (argument.LocalOperand == null)
                        {
                            generator.Emit(argument.GetAddress);
                        }
                        else
                        {
                            generator.Emit(argument.GetAddress, argument.LocalOperand);
                        }
                        generator.Emit(Opcode.Deref);
                        generator.Emit(Opcode.StoreLocal, derefAddressLocal);
                        generator.Emit(Opcode.LoadLocal, derefAddressLocal);
                        generator.Emit(Opcode.Load);
                        generator.Emit(Opcode.StoreLocal, resolvedArgumentLocal);
                        generator.Emit(Opcode.LoadLocal, resolvedArgumentLocal);
                        generator.Emit(Opcode.GetTag);
                        generator.Emit(Opcode.LoadConstant, (int)Tag.Ref);
                        generator.Emit(Opcode.BranchNotEqual, strLabel);
                        generator.Emit(Opcode.LoadGlobalRegisterH);
                        generator.Emit(Opcode.Duplicate);
                        generator.Emit(Opcode.Increment);
                        generator.Emit(Opcode.ApplyTagStr);
                        generator.Emit(Opcode.Store);
                        generator.Emit(Opcode.LoadGlobalRegisterH);
                        generator.Emit(Opcode.Increment);
                        generator.Emit(Opcode.LoadConstant, argumentFunctor);
                        generator.Emit(Opcode.ApplyTagFun);
                        generator.Emit(Opcode.Store);
                        generator.Emit(Opcode.LoadLocal, derefAddressLocal);
                        generator.Emit(Opcode.LoadGlobalRegisterH);
                        generator.Emit(Opcode.Bind);
                        generator.Emit(Opcode.LoadGlobalRegisterH);
                        generator.Emit(Opcode.LoadConstant2);
                        generator.Emit(Opcode.Add);
                        generator.Emit(Opcode.StoreGlobalRegisterH);

                        foreach (var subTerm in subTermsToPopulate)
                        {
                            EmitUnifyWrite(generator, subTerm);
                        }

                        generator.Emit(Opcode.BranchAlways, continueLabel);
                        generator.MarkLabel(strLabel);
                        generator.Emit(Opcode.LoadLocal, resolvedArgumentLocal);
                        generator.Emit(Opcode.GetTag);
                        generator.Emit(Opcode.LoadConstant, (int)Tag.Str);
                        generator.Emit(Opcode.BranchNotEqual, failLabel);
                        generator.Emit(Opcode.LoadLocal, resolvedArgumentLocal);
                        generator.Emit(Opcode.GetAddress);
                        generator.Emit(Opcode.StoreLocal, functorAddressLocal);
                        generator.Emit(Opcode.LoadLocal, functorAddressLocal);
                        generator.Emit(Opcode.Load);
                        generator.Emit(Opcode.LoadConstant, argumentFunctor);
                        generator.Emit(Opcode.ApplyTagFun);
                        generator.Emit(Opcode.BranchNotEqual, failLabel);
                        generator.Emit(Opcode.LoadLocal, functorAddressLocal);
                        generator.Emit(Opcode.Increment);
                        generator.Emit(Opcode.StoreLocal, structurePointerLocal);

                        foreach (var subTerm in subTermsToPopulate)
                        {
                            EmitUnifyRead(generator, structurePointerLocal, derefAddressLocal, resolvedArgumentLocal, failLabel, subTerm);
                        }

                        generator.Emit(Opcode.BranchAlways, continueLabel);
                        generator.MarkLabel(failLabel);
                        generator.Emit(Opcode.Fail);
                        generator.MarkLabel(continueLabel);
                    }
                    else
                    {
                        var variableTerm = argument.Term as Variable;

                        if (variableTerm != null)
                        {
                            Local variableLocal;

                            if (!assignedVariables.TryGetValue(variableTerm, out variableLocal))
                            {
                                variableLocal = generator.DefineLocal();

                                Console.WriteLine("GetVariable {0} {1}", variableTerm.Name, variableLocal.Index);

                                assignedVariables.Add(variableTerm, variableLocal);

                                generator.Emit(argument.GetAddress);
                                generator.Emit(Opcode.Load);
                                generator.Emit(Opcode.StoreLocal, variableLocal);
                            }
                            else
                            {
                                Console.WriteLine("GetValue {0} {1}", variableTerm.Name, variableLocal.Index);

                                generator.Emit(argument.GetAddress);
                                generator.Emit(Opcode.LoadLocalAddress, variableLocal);
                                generator.Emit(Opcode.Unify);
                            }
                        }
                        else
                        {
                            var subTermAsConstant = (argument.Term as Atom);

                            if (subTermAsConstant != null)
                            {
                                Console.WriteLine("GetConstant {0}", subTermAsConstant.Token);

                                var conLabel = generator.DefineLabel();
                                var continueLabel = generator.DefineLabel();

                                generator.Emit(argument.GetAddress);
                                generator.Emit(Opcode.Deref);
                                generator.Emit(Opcode.StoreLocal, derefAddressLocal);
                                generator.Emit(Opcode.LoadLocal, derefAddressLocal);
                                generator.Emit(Opcode.Load);
                                generator.Emit(Opcode.StoreLocal, resolvedArgumentLocal);
                                generator.Emit(Opcode.LoadLocal, resolvedArgumentLocal);
                                generator.Emit(Opcode.GetTag);
                                generator.Emit(Opcode.LoadConstant, (int)Tag.Ref);
                                generator.Emit(Opcode.BranchNotEqual, conLabel);
                                generator.Emit(Opcode.LoadLocal, derefAddressLocal);
                                generator.Emit(Opcode.LoadConstant, new FunctorDescriptor(subTermAsConstant));
                                generator.Emit(Opcode.ApplyTagCon);
                                generator.Emit(Opcode.Store);
                                generator.Emit(Opcode.BranchAlways, continueLabel);
                                generator.MarkLabel(conLabel);
                                generator.Emit(Opcode.LoadLocal, resolvedArgumentLocal);
                                generator.Emit(Opcode.LoadConstant, new FunctorDescriptor(subTermAsConstant));
                                generator.Emit(Opcode.ApplyTagCon);
                                generator.Emit(Opcode.BranchEqual, continueLabel);
                                generator.Emit(Opcode.Fail);
                                generator.MarkLabel(continueLabel);
                            }
                            else
                            {
                                var subTermAsList = (argument.Term as ListPair);

                                Console.WriteLine("GetList {0}", argument.LocalOperand != null ? argument.LocalOperand.Index : ((int)argument.GetAddress & 8));

                                var headArgument = EvaluateSubTerm(generator, subTerms, assignedVariables, subTermAsList.Head);
                                var tailArgument = EvaluateSubTerm(generator, subTerms, assignedVariables, subTermAsList.Tail);

                                var lisLabel = generator.DefineLabel();
                                var failLabel = generator.DefineLabel();
                                var continueLabel = generator.DefineLabel();

                                if (argument.LocalOperand == null)
                                {
                                    generator.Emit(argument.GetAddress);
                                }
                                else
                                {
                                    generator.Emit(argument.GetAddress, argument.LocalOperand);
                                }
                                generator.Emit(Opcode.Deref);
                                generator.Emit(Opcode.StoreLocal, derefAddressLocal);
                                generator.Emit(Opcode.LoadLocal, derefAddressLocal);
                                generator.Emit(Opcode.Load);
                                generator.Emit(Opcode.StoreLocal, resolvedArgumentLocal);
                                generator.Emit(Opcode.LoadLocal, resolvedArgumentLocal);
                                generator.Emit(Opcode.GetTag);
                                generator.Emit(Opcode.LoadConstant, (int)Tag.Ref);
                                generator.Emit(Opcode.BranchNotEqual, lisLabel);
                                generator.Emit(Opcode.LoadGlobalRegisterH);
                                generator.Emit(Opcode.Duplicate);
                                generator.Emit(Opcode.Increment);
                                generator.Emit(Opcode.ApplyTagLis);
                                generator.Emit(Opcode.Store);
                                generator.Emit(Opcode.LoadGlobalRegisterH);
                                generator.Emit(Opcode.LoadLocal, derefAddressLocal);
                                generator.Emit(Opcode.Bind);
                                generator.Emit(Opcode.LoadGlobalRegisterH);
                                generator.Emit(Opcode.Increment);
                                generator.Emit(Opcode.StoreGlobalRegisterH);

                                EmitUnifyWrite(generator, headArgument);
                                EmitUnifyWrite(generator, tailArgument);

                                generator.Emit(Opcode.BranchAlways, continueLabel);
                                generator.MarkLabel(lisLabel);
                                generator.Emit(Opcode.LoadLocal, resolvedArgumentLocal);
                                generator.Emit(Opcode.GetTag);
                                generator.Emit(Opcode.LoadConstant, (int)Tag.Lis);
                                generator.Emit(Opcode.BranchNotEqual, failLabel);
                                generator.Emit(Opcode.LoadLocal, resolvedArgumentLocal);
                                generator.Emit(Opcode.GetAddress);
                                generator.Emit(Opcode.StoreLocal, structurePointerLocal);

                                EmitUnifyRead(generator, structurePointerLocal, derefAddressLocal, resolvedArgumentLocal, failLabel, headArgument);
                                EmitUnifyRead(generator, structurePointerLocal, derefAddressLocal, resolvedArgumentLocal, failLabel, tailArgument);

                                generator.Emit(Opcode.BranchAlways, continueLabel);
                                generator.MarkLabel(failLabel);
                                generator.Emit(Opcode.Fail);
                                generator.MarkLabel(continueLabel);
                            }
                        }
                    }
                }
            }

            var termLookup = new Dictionary<ITerm, Local>();

            var setVariables = new List<Variable>();

            foreach (var variablePair in assignedVariables)
            {
                termLookup.Add(variablePair.Key, variablePair.Value);
                setVariables.Add(variablePair.Key);
            }

            foreach (var goal in rule.Goals)
            {
                var termStack = new Stack<ITerm>();
                var newTerms = new List<ITerm>(goal.Terms);

                while (newTerms.Any())
                {
                    var temp = newTerms.ToArray();

                    newTerms.Clear();

                    foreach (var term in temp)
                    {
                        termStack.Push(term);

                        var compoundTerm = term as CompoundTerm;

                        if (compoundTerm != null)
                        {
                            newTerms.AddRange(compoundTerm.Terms
                                                            .Where(x => x is CompoundTerm || x is Variable)
                                                            .Reverse());
                        }
                    }
                }

                foreach (var term in termStack)
                {
                    var variable = term as Variable;

                    if (variable != null)
                    {
                        EmitSetVariableValue(generator, termLookup, setVariables, term, variable);
                    }
                    else
                    {
                        var compoundTerm = term as CompoundTerm;

                        if (compoundTerm != null)
                        {
                            var termLocal = generator.DefineLocal();
                            termLookup.Add(compoundTerm, termLocal);

                            Console.WriteLine("SetStructure {0} {1}", new FunctorDescriptor(compoundTerm), termLocal.Index);

                            generator.Emit(Opcode.LoadGlobalRegisterH);
                            generator.Emit(Opcode.Increment);
                            generator.Emit(Opcode.ApplyTagStr);
                            generator.Emit(Opcode.StoreLocal, termLocal);
                            generator.Emit(Opcode.LoadGlobalRegisterH);
                            generator.Emit(Opcode.LoadLocal, termLocal);
                            generator.Emit(Opcode.Store);
                            generator.Emit(Opcode.LoadGlobalRegisterH);
                            generator.Emit(Opcode.Increment);
                            generator.Emit(Opcode.LoadConstant,
                                            new FunctorDescriptor(compoundTerm));
                            generator.Emit(Opcode.ApplyTagFun);
                            generator.Emit(Opcode.Store);
                            generator.Emit(Opcode.LoadGlobalRegisterH);
                            generator.Emit(Opcode.Increment);
                            generator.Emit(Opcode.Increment);
                            generator.Emit(Opcode.StoreGlobalRegisterH);

                            foreach (var subTerm in compoundTerm.Terms)
                            {
                                if (subTerm is Variable)
                                {
                                    EmitSetVariableValue(generator, termLookup, setVariables, subTerm, subTerm as Variable);
                                }
                                else if (subTerm is Atom)
                                {
                                    var constantTerm = subTerm as Atom;

                                    Console.WriteLine("SetConstant {0}", constantTerm.Token);

                                    generator.Emit(Opcode.LoadGlobalRegisterH);
                                    generator.Emit(Opcode.LoadConstant, new FunctorDescriptor(constantTerm));
                                    generator.Emit(Opcode.ApplyTagCon);
                                    generator.Emit(Opcode.Store);
                                    generator.Emit(Opcode.LoadGlobalRegisterH);
                                    generator.Emit(Opcode.Increment);
                                    generator.Emit(Opcode.StoreGlobalRegisterH);
                                }
                                else
                                {
                                    EmitSetValue(generator, termLookup[subTerm]);
                                }
                            }
                        }
                        else
                        {
                            var constantTerm = term as Atom;

                            if (constantTerm != null)
                            {
                                var termLocal = generator.DefineLocal();
                                termLookup.Add(constantTerm, termLocal);

                                Console.WriteLine("SetConstant {0} {1}", constantTerm.Token, termLocal.Index);

                                generator.Emit(Opcode.LoadConstant, new FunctorDescriptor(constantTerm));
                                generator.Emit(Opcode.ApplyTagCon);
                                generator.Emit(Opcode.StoreLocal, termLocal);
                            }
                            else
                            {
                                var listTerm = term as ListPair;

                                if (listTerm != null)
                                {
                                    var termLocal = generator.DefineLocal();
                                    termLookup.Add(listTerm, termLocal);

                                    Console.WriteLine("SetList {0}", termLocal.Index);

                                    generator.Emit(Opcode.LoadGlobalRegisterH);
                                    generator.Emit(Opcode.ApplyTagLis);
                                    generator.Emit(Opcode.StoreLocal, termLocal);
                                }
                                else
                                {
                                    var emptyListTerm = term as EmptyList;

                                    if (emptyListTerm != null)
                                    {
                                        var termLocal = generator.DefineLocal();
                                        termLookup.Add(emptyListTerm, termLocal);

                                        Console.WriteLine("SetConstant [] {0}", termLocal.Index);

                                        generator.Emit(Opcode.LoadConstant, new FunctorDescriptor(emptyListTerm));
                                        generator.Emit(Opcode.ApplyTagCon);
                                        generator.Emit(Opcode.StoreLocal, termLocal);
                                    }
                                    else
                                    {
                                        throw new NotSupportedException();
                                    }
                                }
                            }
                        }
                    }
                }

                foreach (var argument in goal.Terms.Reverse())
                {
                    var argumentLocal = termLookup[argument];

                    Console.WriteLine("PutArgument {0}", argumentLocal.Index);

                    generator.Emit(Opcode.LoadLocal, argumentLocal);
                }

                Console.WriteLine("Call {0}", new FunctorDescriptor(goal));
                generator.Emit(Opcode.Call, new FunctorDescriptor(goal));
            }

            Console.WriteLine();
        }

        private static void EmitUnifyRead(ICodeGenerator generator, Local structurePointerLocal, Local derefAddressLocal, Local resolvedArgumentLocal, Label failLabel, StructureArgument subTerm)
        {
            switch (subTerm.Type)
            {
                case ArgumentType.Value:
                    Console.WriteLine("UnifyValue {0} (read mode)", subTerm.LocalOperand.Index);

                    generator.Emit(Opcode.LoadLocal, structurePointerLocal);
                    generator.Emit(Opcode.LoadLocalAddress, subTerm.LocalOperand);
                    generator.Emit(Opcode.Unify);
                    generator.Emit(Opcode.LoadLocal, structurePointerLocal);
                    generator.Emit(Opcode.Increment);
                    generator.Emit(Opcode.StoreLocal, structurePointerLocal);
                    break;
                case ArgumentType.Variable:
                    Console.WriteLine("UnifyVariable {0} (read mode)", subTerm.LocalOperand.Index);

                    generator.Emit(Opcode.LoadLocal, structurePointerLocal);
                    generator.Emit(Opcode.Load);
                    generator.Emit(Opcode.StoreLocal, subTerm.LocalOperand);
                    generator.Emit(Opcode.LoadLocal, structurePointerLocal);
                    generator.Emit(Opcode.Increment);
                    generator.Emit(Opcode.StoreLocal, structurePointerLocal);
                    break;
                case ArgumentType.Constant:
                    var subTermAsConstant = (subTerm.Term as Atom);

                    Console.WriteLine("UnifyConstant {0} (read mode)", subTermAsConstant.Token);

                    var conLabel = generator.DefineLabel();
                    var constantContinueLabel = generator.DefineLabel();

                    generator.Emit(Opcode.LoadLocal, structurePointerLocal);
                    generator.Emit(Opcode.Deref);
                    generator.Emit(Opcode.StoreLocal, derefAddressLocal);
                    generator.Emit(Opcode.LoadLocal, derefAddressLocal);
                    generator.Emit(Opcode.Load);
                    generator.Emit(Opcode.StoreLocal, resolvedArgumentLocal);
                    generator.Emit(Opcode.LoadLocal, resolvedArgumentLocal);
                    generator.Emit(Opcode.GetTag);
                    generator.Emit(Opcode.LoadConstant, (int)Tag.Ref);
                    generator.Emit(Opcode.BranchNotEqual, conLabel);
                    generator.Emit(Opcode.LoadLocal, derefAddressLocal);
                    generator.Emit(Opcode.LoadConstant, new FunctorDescriptor(subTermAsConstant));
                    generator.Emit(Opcode.ApplyTagCon);
                    generator.Emit(Opcode.Store);
                    generator.Emit(Opcode.BranchAlways, constantContinueLabel);
                    generator.MarkLabel(conLabel);
                    generator.Emit(Opcode.LoadLocal, resolvedArgumentLocal);
                    generator.Emit(Opcode.LoadConstant, new FunctorDescriptor(subTermAsConstant));
                    generator.Emit(Opcode.ApplyTagCon);
                    generator.Emit(Opcode.BranchNotEqual, failLabel);
                    generator.MarkLabel(constantContinueLabel);
                    generator.Emit(Opcode.LoadLocal, structurePointerLocal);
                    generator.Emit(Opcode.Increment);
                    generator.Emit(Opcode.StoreLocal, structurePointerLocal);
                    break;
                case ArgumentType.EmptyList:
                    Console.WriteLine("UnifyConstant [] (read mode)");

                    var elConLabel = generator.DefineLabel();
                    var elConstantContinueLabel = generator.DefineLabel();

                    generator.Emit(Opcode.LoadLocal, structurePointerLocal);
                    generator.Emit(Opcode.Deref);
                    generator.Emit(Opcode.StoreLocal, derefAddressLocal);
                    generator.Emit(Opcode.LoadLocal, derefAddressLocal);
                    generator.Emit(Opcode.Load);
                    generator.Emit(Opcode.StoreLocal, resolvedArgumentLocal);
                    generator.Emit(Opcode.LoadLocal, resolvedArgumentLocal);
                    generator.Emit(Opcode.GetTag);
                    generator.Emit(Opcode.LoadConstant, (int)Tag.Ref);
                    generator.Emit(Opcode.BranchNotEqual, elConLabel);
                    generator.Emit(Opcode.LoadLocal, derefAddressLocal);
                    generator.Emit(Opcode.LoadConstant, new FunctorDescriptor((EmptyList)subTerm.Term));
                    generator.Emit(Opcode.ApplyTagCon);
                    generator.Emit(Opcode.Store);
                    generator.Emit(Opcode.BranchAlways, elConstantContinueLabel);
                    generator.MarkLabel(elConLabel);
                    generator.Emit(Opcode.LoadLocal, resolvedArgumentLocal);
                    generator.Emit(Opcode.LoadConstant, new FunctorDescriptor((EmptyList)subTerm.Term));
                    generator.Emit(Opcode.ApplyTagCon);
                    generator.Emit(Opcode.BranchNotEqual, failLabel);
                    generator.MarkLabel(elConstantContinueLabel);
                    generator.Emit(Opcode.LoadLocal, structurePointerLocal);
                    generator.Emit(Opcode.Increment);
                    generator.Emit(Opcode.StoreLocal, structurePointerLocal);
                    break;
            }
        }

        private static void EmitUnifyWrite(ICodeGenerator generator, StructureArgument subTerm)
        {
            switch (subTerm.Type)
            {
                case ArgumentType.Value:
                    Console.WriteLine("UnifyValue {0} (write mode)", subTerm.LocalOperand.Index);

                    generator.Emit(Opcode.LoadGlobalRegisterH);
                    generator.Emit(Opcode.LoadLocal, subTerm.LocalOperand);
                    generator.Emit(Opcode.Store);
                    generator.Emit(Opcode.LoadGlobalRegisterH);
                    generator.Emit(Opcode.Increment);
                    generator.Emit(Opcode.StoreGlobalRegisterH);
                    break;
                case ArgumentType.Variable:
                    Console.WriteLine("UnifyVariable {0} (write mode)", subTerm.LocalOperand.Index);

                    generator.Emit(Opcode.LoadGlobalRegisterH);
                    generator.Emit(Opcode.Duplicate);
                    generator.Emit(Opcode.ApplyTagRef);
                    generator.Emit(Opcode.Store);
                    generator.Emit(Opcode.LoadGlobalRegisterH);
                    generator.Emit(Opcode.Load);
                    generator.Emit(Opcode.StoreLocal, subTerm.LocalOperand);
                    generator.Emit(Opcode.LoadGlobalRegisterH);
                    generator.Emit(Opcode.Increment);
                    generator.Emit(Opcode.StoreGlobalRegisterH);
                    break;
                case ArgumentType.Constant:
                    var subTermAsConstant = subTerm.Term as Atom;

                    Console.WriteLine("UnifyConstant {0} (write mode)", subTermAsConstant.Token);

                    generator.Emit(Opcode.LoadGlobalRegisterH);
                    generator.Emit(Opcode.LoadConstant, new FunctorDescriptor(subTermAsConstant));
                    generator.Emit(Opcode.ApplyTagCon);
                    generator.Emit(Opcode.Store);
                    generator.Emit(Opcode.LoadGlobalRegisterH);
                    generator.Emit(Opcode.Increment);
                    generator.Emit(Opcode.StoreGlobalRegisterH);
                    break;
                case ArgumentType.EmptyList:
                    Console.WriteLine("UnifyConstant [] (write mode)");

                    generator.Emit(Opcode.LoadGlobalRegisterH);
                    generator.Emit(Opcode.LoadConstant, new FunctorDescriptor((EmptyList)subTerm.Term));
                    generator.Emit(Opcode.ApplyTagCon);
                    generator.Emit(Opcode.Store);
                    generator.Emit(Opcode.LoadGlobalRegisterH);
                    generator.Emit(Opcode.Increment);
                    generator.Emit(Opcode.StoreGlobalRegisterH);
                    break;
            }
        }

        private static StructureArgument EvaluateSubTerm(ICodeGenerator generator, List<StructureArgument> subTerms, Dictionary<Variable, Local> assignedVariables, ITerm subTerm)
        {
            var subTermAsVariable = subTerm as Variable;
            var compoundSubterm = subTerm as CompoundTerm;
            var subTermAsList = subTerm as ListPair;

            StructureArgument? newArgument = null;

            if (subTermAsVariable != null || compoundSubterm != null || subTermAsList != null)
            {
                Local subTermLocal = null;

                bool isAssigned = subTermAsVariable != null && assignedVariables.TryGetValue(subTermAsVariable, out subTermLocal);

                if (!isAssigned)
                {
                    subTermLocal = generator.DefineLocal();

                    if (subTermAsVariable != null)
                    {
                        assignedVariables.Add(subTermAsVariable, subTermLocal);
                    }
                }

                newArgument = new StructureArgument
                {
                    Term = subTerm,
                    GetAddress = Opcode.LoadLocalAddress,
                    LocalOperand = subTermLocal,
                    Type = isAssigned
                            ? ArgumentType.Value
                            : ArgumentType.Variable
                };

                if (compoundSubterm != null || subTermAsList != null)
                {
                    subTerms.Add(newArgument.Value);
                }
            }
            else
            {
                var subTermAsConstant = subTerm as Atom;

                if (subTermAsConstant != null)
                {
                    newArgument = new StructureArgument
                    {
                        Term = subTerm,
                        Type = ArgumentType.Constant
                    };
                }
                else
                {
                    var subTermAsEmptyList = subTerm as EmptyList;

                    if (subTermAsEmptyList != null)
                    {
                        newArgument = new StructureArgument
                        {
                            Term = subTerm,
                            Type = ArgumentType.EmptyList
                        };
                    }
                }
            }

            if (newArgument.HasValue)
            {
                return newArgument.Value;
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        private static void EmitSetVariableValue(ICodeGenerator generator, Dictionary<ITerm, Local> termLookup, List<Variable> setVariables, ITerm term, Variable variable)
        {
            if (!setVariables.Contains(variable))
            {
                var termLocal = generator.DefineLocal();
                termLookup.Add(variable, termLocal);

                Console.WriteLine("SetVariable {0} {1}", variable.Name, termLocal.Index);

                generator.Emit(Opcode.LoadGlobalRegisterH);
                generator.Emit(Opcode.Duplicate);
                generator.Emit(Opcode.ApplyTagRef);
                generator.Emit(Opcode.Store);
                generator.Emit(Opcode.LoadGlobalRegisterH);
                generator.Emit(Opcode.Load);
                generator.Emit(Opcode.StoreLocal, termLocal);
                generator.Emit(Opcode.LoadGlobalRegisterH);
                generator.Emit(Opcode.Increment);
                generator.Emit(Opcode.StoreGlobalRegisterH);

                setVariables.Add(variable);
            }
            else
            {
                EmitSetValue(generator, termLookup[term]);
            }
        }

        private static void EmitSetValue(ICodeGenerator generator, Local termLocal)
        {
            Console.WriteLine("SetValue {0}", termLocal.Index);

            generator.Emit(Opcode.LoadGlobalRegisterH);
            generator.Emit(Opcode.LoadLocal, termLocal);
            generator.Emit(Opcode.Store);
            generator.Emit(Opcode.LoadGlobalRegisterH);
            generator.Emit(Opcode.Increment);
            generator.Emit(Opcode.StoreGlobalRegisterH);
        }
    }
}
