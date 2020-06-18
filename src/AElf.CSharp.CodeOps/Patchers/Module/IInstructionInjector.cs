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

        void InjectInstruction(ILProcessor ilProcessor, Instruction originInstruction, ModuleDefinition moduleDefinition,
            MethodDefinition methodDefinition);
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


        public bool IdentifyInstruction(ModuleDefinition moduleDefinition, Instruction instruction)
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

        public MethodDefinition PatchMethodReference(ModuleDefinition moduleDefinition)
        {
            return ConstructStateSizeLimitMethod(moduleDefinition);
        }

        public void InjectInstruction(ILProcessor ilProcessor, Instruction originInstruction, ModuleDefinition moduleDefinition,
            MethodDefinition methodDefinition)
        {
            ilProcessor.Body.SimplifyMacros();
            // ilProcessor.
            // var instruction = originInstruction.Previous;

            var localValCount = ilProcessor.Body.Variables.Count;
            ilProcessor.Body.Variables.Add(new VariableDefinition(moduleDefinition.ImportReference(typeof(object))));
            
            var stocInstruction = ilProcessor.Create(OpCodes.Stloc_S, ilProcessor.Body.Variables[localValCount]); // pop to local val 
            ilProcessor.InsertBefore(originInstruction, stocInstruction);
            
            var ldThisInstruction = ilProcessor.Create(OpCodes.Ldarg_0); // this
            ilProcessor.InsertAfter(stocInstruction, ldThisInstruction);
            
            var ldlocInstruction = ilProcessor.Create(OpCodes.Ldloc_S, ilProcessor.Body.Variables[localValCount]); // load local val
            ilProcessor.InsertAfter(ldThisInstruction, ldlocInstruction);
            
            var callInstruction = ilProcessor.Create(OpCodes.Call, methodDefinition); // call 
            ilProcessor.InsertAfter(ldlocInstruction, callInstruction);
            
            // ilProcessor.InsertBefore(originInstruction, instruction); // load the value
            ilProcessor.Body.OptimizeMacros();
        }

        private MethodDefinition ConstructStateSizeLimitMethod(ModuleDefinition moduleDefinition)
        {
            var typeDefinition =
                moduleDefinition.Types.Single(m => m.BaseType is TypeDefinition);
            // var nmspace = typeDefinition.Namespace;

            // var stateSizeLimitType = new TypeDefinition(
            //     nmspace, nameof(StateRestrictionProxy),
            //     TypeAttributes.Sealed | TypeAttributes.Public | TypeAttributes.Class,
            //     moduleDefinition.ImportReference(typeof(object))
            // );

            var stateSizeLimitMethod = new MethodDefinition(
                nameof(StateRestrictionProxy.LimitStateSize),
                MethodAttributes.Private | MethodAttributes.HideBySig,
                moduleDefinition.ImportReference(typeof(object))
            );
            
            // parameter 
            stateSizeLimitMethod.Parameters.Add(new ParameterDefinition("obj",
                ParameterAttributes.In, moduleDefinition.ImportReference(typeof(object))));
            
            // return value
            stateSizeLimitMethod.Body.Variables.Add(
                new VariableDefinition(moduleDefinition.ImportReference(typeof(object))));
            
            var il = stateSizeLimitMethod.Body.GetILProcessor();
            
            il.Emit(OpCodes.Ldarg_0); // this
            
            // Context
            il.Emit(OpCodes.Call,
                moduleDefinition.ImportReference(typeof(CSharpSmartContract<>).GetProperty("Context").GetMethod));
            // parameter
            il.Emit(OpCodes.Ldarg_1);
            
            // Context.LimitStateSize
            il.Emit(OpCodes.Callvirt, moduleDefinition.ImportReference(
                typeof(CSharpSmartContractContext).GetMethod(nameof(CSharpSmartContractContext.LimitStateSize))));
            
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Stloc_0); // pop from the top to localVal_0 

            var loadReturnValueInstruction = il.Create(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Br_S, loadReturnValueInstruction); // return

            il.Append(loadReturnValueInstruction);
            il.Emit(OpCodes.Ret);
            
            typeDefinition.Methods.Add(stateSizeLimitMethod);

            // stateSizeLimitMethod.Parameters.Add(new ParameterDefinition("obj",
            //     ParameterAttributes.In, moduleDefinition.ImportReference(typeof(object))));
            //
            // // comparision result
            // stateSizeLimitMethod.Body.Variables.Add(
            //     new VariableDefinition(moduleDefinition.ImportReference(typeof(bool))));
            //
            // // return value
            // stateSizeLimitMethod.Body.Variables.Add(
            //     new VariableDefinition(moduleDefinition.ImportReference(typeof(object))));
            //
            // var il = stateSizeLimitMethod.Body.GetILProcessor();
            //
            //
            // il.Emit(OpCodes.Ldarg_0);
            // il.Emit(OpCodes.Call, moduleDefinition.ImportReference(
            //     typeof(SerializationHelper).GetMethod(nameof(SerializationHelper.Serialize))));
            // il.Emit(OpCodes.Ldlen); // length
            // il.Emit(OpCodes.Conv_I4); // convert to int32
            // il.Emit(OpCodes.Ldc_I4, 128 * 1024); // push int32
            // il.Emit(OpCodes.Cgt); // compare 
            // il.Emit(OpCodes.Stloc_0); // pop from the top
            //
            // il.Emit(OpCodes.Ldloc_0); // load comparision value
            // // var ret = il.Create(OpCodes.Ret);
            // var returnStatement = il.Create(OpCodes.Ldarg_0);
            // il.Emit(OpCodes.Brfalse_S, returnStatement); // return
            //
            // il.Emit(OpCodes.Ldstr, "State size limited.");
            // il.Emit(OpCodes.Newobj, moduleDefinition.ImportReference(
            //     typeof(AssertionException).GetConstructor(
            //         BindingFlags.Instance | BindingFlags.Public, null,
            //         CallingConventions.HasThis, new[] {typeof(string)}, null)));
            // il.Emit(OpCodes.Throw);
            //
            // il.Append(returnStatement);
            // il.Emit(OpCodes.Stloc_1); // pop from the top to local val 
            // var loadReturnValueInstruction = il.Create(OpCodes.Ldloc_1);
            // il.Emit(OpCodes.Br_S, loadReturnValueInstruction); // return
            //
            // il.Append(loadReturnValueInstruction);
            // il.Emit(OpCodes.Ret);
            //
            // stateSizeLimitType.Methods.Add(stateSizeLimitMethod);
            // moduleDefinition.Types.Add(stateSizeLimitType);

            return stateSizeLimitMethod;
        }
    }
}