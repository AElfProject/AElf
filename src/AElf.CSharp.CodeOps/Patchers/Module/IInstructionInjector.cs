using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using AElf.Sdk.CSharp;
using AElf.Sdk.CSharp.State;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Volo.Abp.DependencyInjection;
using MethodAttributes = Mono.Cecil.MethodAttributes;
using ParameterAttributes = Mono.Cecil.ParameterAttributes;
using TypeAttributes = Mono.Cecil.TypeAttributes;

namespace AElf.CSharp.CodeOps.Patchers.Module
{
    public interface IInstructionInjector
    {
        bool IdentifyInstruction(ModuleDefinition moduleDefinition, Instruction instruction);

        MethodDefinition PatchMethodReference(ModuleDefinition moduleDefinition);

        void InjectInstruction(ILProcessor ilProcessor, Instruction originInstruction,
            MethodDefinition moduleDefinition);
    }

    public class ContractStateInstructionInjector : IInstructionInjector, ITransientDependency
    {
        private static readonly ReadOnlyDictionary<string, string> MethodCallsIdentifications =
            new ReadOnlyDictionary<string, string>(
                new Dictionary<string, string>
                {
                    {typeof(SingletonState).FullName, "set_Value"},
                    {typeof(ReadonlyState).FullName, "set_Value"},
                    {typeof(MappedState).FullName, "set_Item"}
                });


        public bool IdentifyInstruction(ModuleDefinition moduleDefinition, Instruction instruction)
        {
            if (instruction.OpCode != OpCodes.Callvirt)
                return false;
            var methodReference = (MethodReference) instruction.Operand;
            var baseTypeFullName = methodReference.DeclaringType.Resolve()?.BaseType?.FullName;
            if (baseTypeFullName == null || !MethodCallsIdentifications.TryGetValue(baseTypeFullName, out var methodName))
                return false;
            return methodReference.Name == methodName;
        }

        public MethodDefinition PatchMethodReference(ModuleDefinition moduleDefinition)
        {
            return ConstructStateSizeLimitMethod(moduleDefinition);
        }

        public void InjectInstruction(ILProcessor ilProcessor, Instruction originInstruction,
            MethodDefinition methodDefinition)
        {
            ilProcessor.Body.SimplifyMacros();
            // ilProcessor.
            // var instruction = originInstruction.Previous;
            ilProcessor.InsertBefore(originInstruction, ilProcessor.Create(OpCodes.Call, methodDefinition));
            // ilProcessor.InsertBefore(originInstruction, instruction); // load the value
            ilProcessor.Body.OptimizeMacros();
        }

        private MethodDefinition ConstructStateSizeLimitMethod(ModuleDefinition moduleDefinition)
        {
            var nmspace = moduleDefinition.Types.Single(m => m.BaseType is TypeDefinition).Namespace;

            var stateSizeLimitType = new TypeDefinition(
                nmspace, nameof(StateRestrictionProxy),
                TypeAttributes.Sealed | TypeAttributes.Public | TypeAttributes.Class,
                moduleDefinition.ImportReference(typeof(object))
            );

            var stateSizeLimitMethod = new MethodDefinition(
                nameof(StateRestrictionProxy.LimitStateSize),
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
                moduleDefinition.ImportReference(typeof(object))
            );

            stateSizeLimitMethod.Parameters.Add(new ParameterDefinition("obj",
                ParameterAttributes.In, moduleDefinition.ImportReference(typeof(object))));
            
            // comparision result
            stateSizeLimitMethod.Body.Variables.Add(
                new VariableDefinition(moduleDefinition.ImportReference(typeof(bool))));
            
            // return value
            stateSizeLimitMethod.Body.Variables.Add(
                new VariableDefinition(moduleDefinition.ImportReference(typeof(object))));
            
            var il = stateSizeLimitMethod.Body.GetILProcessor();


            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, moduleDefinition.ImportReference(
                typeof(SerializationHelper).GetMethod(nameof(SerializationHelper.Serialize))));
            il.Emit(OpCodes.Ldlen); // length
            il.Emit(OpCodes.Conv_I4); // convert to int32
            il.Emit(OpCodes.Ldc_I4, 128 * 1024); // push int32
            il.Emit(OpCodes.Cgt); // compare 
            il.Emit(OpCodes.Stloc_0); // pop from the top

            il.Emit(OpCodes.Ldloc_0); // load comparision value
            // var ret = il.Create(OpCodes.Ret);
            var returnStatement = il.Create(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Brfalse_S, returnStatement); // return

            il.Emit(OpCodes.Ldstr, "State size limited.");
            il.Emit(OpCodes.Newobj, moduleDefinition.ImportReference(
                typeof(AssertionException).GetConstructor(
                    BindingFlags.Instance | BindingFlags.Public, null,
                    CallingConventions.HasThis, new[] {typeof(string)}, null)));
            il.Emit(OpCodes.Throw);
            
            il.Append(returnStatement);
            il.Emit(OpCodes.Stloc_1); // pop from the top
            var loadReturnValueInstruction = il.Create(OpCodes.Ldloc_1);
            il.Emit(OpCodes.Br_S, loadReturnValueInstruction); // return
            
            il.Append(loadReturnValueInstruction);
            il.Emit(OpCodes.Ret);

            stateSizeLimitType.Methods.Add(stateSizeLimitMethod);
            moduleDefinition.Types.Add(stateSizeLimitType);

            return stateSizeLimitMethod;
        }
    }
}