using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using AElf.Sdk.CSharp;
using AElf.Sdk.CSharp.State;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Volo.Abp.DependencyInjection;

namespace AElf.CSharp.CodeOps.Instructions;

public interface IStateWrittenInstructionInjector
{
    bool IdentifyInstruction(Instruction instruction);

    void InjectInstruction(ILProcessor ilProcessor, Instruction originInstruction,
        ModuleDefinition moduleDefinition);

    bool ValidateInstruction(ModuleDefinition moduleDefinition, Instruction instruction);
}

public class StateWrittenInstructionInjector : IStateWrittenInstructionInjector, ITransientDependency
{
    private static readonly ReadOnlyDictionary<string, HashSet<string>> MethodCallsIdentifications =
        new ReadOnlyDictionary<string, HashSet<string>>(
            new Dictionary<string, HashSet<string>>
            {
                {typeof(SingletonState).FullName, new HashSet<string> {"set_Value"}},
                {typeof(ReadonlyState).FullName, new HashSet<string> {"set_Value"}},
                {typeof(MappedState).FullName, new HashSet<string> {"set_Item", "Set"}}
            });

    private static readonly HashSet<string> PrimitiveTypes = new HashSet<string>
    {
        typeof(int).FullName, typeof(uint).FullName,
        typeof(long).FullName, typeof(ulong).FullName,
        typeof(bool).FullName
    };

    public bool IdentifyInstruction(Instruction instruction)
    {
        if (instruction.OpCode != OpCodes.Callvirt)
            return false;
        var methodReference = (MethodReference) instruction.Operand;
        var declaringType = methodReference.DeclaringType.Resolve();
        if (declaringType == null || !declaringType.HasGenericParameters)
            return false;

        var baseTypeFullName = declaringType.BaseType?.FullName;
        if (baseTypeFullName == null ||
            !MethodCallsIdentifications.TryGetValue(baseTypeFullName, out var methodNames) || 
            !(methodReference.DeclaringType is GenericInstanceType genericType))
            return false;
        var argumentType = genericType.GenericArguments.Last().Resolve();
        if (argumentType.IsEnum)
            return false;
            
        var contains = PrimitiveTypes.Contains(argumentType.FullName);
        return !contains && methodNames.Contains(methodReference.Name);
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
            moduleDefinition.ImportReference(typeof(CSharpSmartContractAbstract).GetProperty("Context")
                .GetMethod)); // get_Context
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

        MethodReference stateSizeLimitInstruction;
        try
        {
            var previousIsCast = instruction.Previous?.OpCode == OpCodes.Castclass;
            var expectedToBeValidateStateSize = previousIsCast ? instruction.Previous.Previous : instruction.Previous;
            stateSizeLimitInstruction = (MethodReference) expectedToBeValidateStateSize?.Operand;
        }
        catch (InvalidCastException)
        {
            return false;
        }
            
        var result = !string.IsNullOrEmpty(stateSizeLimitInstruction?.FullName) &&
                     methodDefinition.FullName == stateSizeLimitInstruction.FullName;

        return result;
    }
}