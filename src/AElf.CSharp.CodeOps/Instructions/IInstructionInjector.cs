using System.Collections.Generic;
using System.Collections.ObjectModel;
using AElf.Sdk.CSharp;
using AElf.Sdk.CSharp.State;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Volo.Abp.DependencyInjection;

namespace AElf.CSharp.CodeOps.Instructions
{
    public interface IInstructionInjector
    {
        bool IdentifyInstruction(Instruction instruction);

        void InjectInstruction(ILProcessor ilProcessor, Instruction originInstruction,
            ModuleDefinition moduleDefinition);

        bool ValidateInstruction(ModuleDefinition moduleDefinition, Instruction instruction);
    }

    public class ContractStateInstructionInjector : IInstructionInjector, ITransientDependency
    {
        private static readonly ReadOnlyDictionary<string, List<string>> MethodCallsIdentifications =
            new ReadOnlyDictionary<string, List<string>>(
                new Dictionary<string, List<string>>
                {
                    {typeof(SingletonState).FullName, new List<string> {"set_Value"}},
                    {typeof(ReadonlyState).FullName, new List<string> {"set_Value"}},
                    {typeof(MappedState).FullName, new List<string> {"set_Item", "Set"}}
                });


        public bool IdentifyInstruction(Instruction instruction)
        {
            if (instruction.OpCode != OpCodes.Callvirt)
                return false;
            var methodReference = (MethodReference) instruction.Operand;
            var baseTypeFullName = methodReference.DeclaringType.Resolve()?.BaseType?.FullName;
            if (baseTypeFullName == null ||
                !MethodCallsIdentifications.TryGetValue(baseTypeFullName, out var methodNames))
                return false;
            return methodNames.Contains(methodReference.Name);
        }


        public void InjectInstruction(ILProcessor ilProcessor, Instruction originInstruction,
            ModuleDefinition moduleDefinition)
        {
            ilProcessor.Body.SimplifyMacros();

            var localValCount = ilProcessor.Body.Variables.Count;
            ilProcessor.Body.Variables.Add(new VariableDefinition(moduleDefinition.ImportReference(typeof(object))));

            var stocInstruction =
                ilProcessor.Create(OpCodes.Stloc_S, ilProcessor.Body.Variables[localValCount]); // pop to local val 
            ilProcessor.InsertBefore(originInstruction, stocInstruction);

            var ldThisInstruction = ilProcessor.Create(OpCodes.Ldarg_0); // this
            ilProcessor.InsertAfter(stocInstruction, ldThisInstruction);

            var getContextInstruction = ilProcessor.Create(OpCodes.Call,
                moduleDefinition.ImportReference(typeof(CSharpSmartContract<>).GetProperty("Context").GetMethod)); // get_Context
            ilProcessor.InsertAfter(ldThisInstruction, getContextInstruction);

            var ldlocInstruction =
                ilProcessor.Create(OpCodes.Ldloc_S, ilProcessor.Body.Variables[localValCount]); // load local val
            ilProcessor.InsertAfter(getContextInstruction, ldlocInstruction);

            var callInstruction = ilProcessor.Create(OpCodes.Callvirt, moduleDefinition.ImportReference(
                typeof(CSharpSmartContractContext).GetMethod(nameof(CSharpSmartContractContext.ValidateStateSize))));
            ilProcessor.InsertAfter(ldlocInstruction, callInstruction);

            ilProcessor.Body.OptimizeMacros();
        }

        public bool ValidateInstruction(ModuleDefinition moduleDefinition, Instruction instruction)
        {
            var methodDefinition = moduleDefinition.ImportReference(
                typeof(CSharpSmartContractContext).GetMethod(nameof(CSharpSmartContractContext.ValidateStateSize)));

            var stateSizeLimitInstruction = (MethodReference) instruction.Previous?.Operand;
            var result = !string.IsNullOrEmpty(stateSizeLimitInstruction?.FullName) &&
                         methodDefinition.FullName == stateSizeLimitInstruction.FullName;

            return result;
        }
    }
}