using System;
using System.Collections;
using System.Linq;
using AElf.Kernel.SmartContract;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace AElf.CSharp.CodeOps.Patchers.Module
{
    public class ExecutionObserverInjector : IPatcher<ModuleDefinition>
    {
        public bool SystemContactIgnored => true;
        
        public void Patch(ModuleDefinition module)
        {
            // Check if already injected, do not double inject
            if (module.Types.Select(t => t.Name).Contains(nameof(ExecutionObserverProxy)))
                return;
            
            // ReSharper disable once IdentifierTypo
            var nmspace = module.Types.Single(m => m.BaseType is TypeDefinition).Namespace;

            var counterProxy = ConstructCounterProxy(module, nmspace);
            
            var proxyBranchCountMethod = counterProxy.Methods.Single(m => m.Name == nameof(ExecutionObserverProxy.BranchCount));
            var proxyCallCountMethod = counterProxy.Methods.Single(m => m.Name == nameof(ExecutionObserverProxy.CallCount));

            // Patch the types
            foreach (var typ in module.Types)
            {
                PatchType(typ, proxyBranchCountMethod, proxyCallCountMethod);
            }

            module.Types.Add(counterProxy);
        }

        public static TypeDefinition ConstructCounterProxy(ModuleDefinition module, string nmspace)
        {
            var observerType = new TypeDefinition(
                nmspace, nameof(ExecutionObserverProxy),
                TypeAttributes.Sealed | TypeAttributes.Abstract | TypeAttributes.Public | TypeAttributes.Class,
                module.ImportReference(typeof(object))
            );
            
            var observerField = new FieldDefinition(
                "_observer",
                FieldAttributes.Private | FieldAttributes.Static, 
                module.ImportReference(typeof(IExecutionObserver)
                )
            );
            
            // Counter field should be thread static (at least for the test cases)
            observerField.CustomAttributes.Add(new CustomAttribute(
                module.ImportReference(typeof(ThreadStaticAttribute).GetConstructor(new Type[]{}))));

            observerType.Fields.Add(observerField);

            observerType.Methods.Add(ConstructProxySetObserverMethod(module, observerField));
            observerType.Methods.Add(ConstructProxyBranchCountMethod(module, observerField));
            observerType.Methods.Add(ConstructProxyCallCountMethod(module, observerField));

            return observerType;
        }

        private static MethodDefinition ConstructProxyBranchCountMethod(ModuleDefinition module, FieldReference observerField)
        {
            var countMethod = new MethodDefinition(
                nameof(ExecutionObserverProxy.BranchCount), 
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static, 
                module.ImportReference(typeof(void))
            );
            
            countMethod.Body.Variables.Add(new VariableDefinition(module.ImportReference(typeof(bool))));
            var il = countMethod.Body.GetILProcessor();

            var ret = il.Create(OpCodes.Ret);

            il.Emit(OpCodes.Ldsfld, observerField);
            il.Emit(OpCodes.Brfalse_S, ret); // Do not call if not initialized
            il.Emit(OpCodes.Ldsfld, observerField);
            il.Emit(OpCodes.Callvirt, module.ImportReference(
                typeof(IExecutionObserver).GetMethod(nameof(IExecutionObserver.BranchCount))));
            il.Append(ret);

            return countMethod;
        }
        
        private static MethodDefinition ConstructProxyCallCountMethod(ModuleDefinition module, FieldReference observerField)
        {
            var countMethod = new MethodDefinition(
                nameof(ExecutionObserverProxy.CallCount), 
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static, 
                module.ImportReference(typeof(void))
            );
            
            countMethod.Body.Variables.Add(new VariableDefinition(module.ImportReference(typeof(bool))));
            var il = countMethod.Body.GetILProcessor();

            var ret = il.Create(OpCodes.Ret);
            
            il.Emit(OpCodes.Ldsfld, observerField);
            il.Emit(OpCodes.Brfalse_S, ret); // Do not call if not initialized
            il.Emit(OpCodes.Ldsfld, observerField);
            il.Emit(OpCodes.Callvirt, module.ImportReference(
                typeof(IExecutionObserver).GetMethod(nameof(IExecutionObserver.CallCount))));
            il.Append(ret);

            return countMethod;
        }

        private static MethodDefinition ConstructProxySetObserverMethod(ModuleDefinition module, FieldReference observerField)
        {
            var setObserverMethod = new MethodDefinition(
                nameof(ExecutionObserverProxy.SetObserver), 
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static, 
                module.ImportReference(typeof(void))
            );

            setObserverMethod.Parameters.Add(new ParameterDefinition("observer", 
                ParameterAttributes.In, module.ImportReference(typeof(IExecutionObserver))));
            
            var il = setObserverMethod.Body.GetILProcessor();
            
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Stsfld, observerField);
            il.Emit(OpCodes.Ret);

            return setObserverMethod;
        }

        private void PatchType(TypeDefinition typ, MethodReference branchCountRef, MethodReference callCountRef)
        {
            // Patch the methods in the type
            foreach (var method in typ.Methods)
            {
                PatchMethodsWithCounter(method, branchCountRef, callCountRef);
            }

            // Patch if there is any nested type within the type
            foreach (var nestedType in typ.NestedTypes)
            {
                PatchType(nestedType, branchCountRef, callCountRef);
            }
        }

        private void PatchMethodsWithCounter(MethodDefinition method, MethodReference branchCountRef, MethodReference callCountRef)
        {
            if (!method.HasBody)
                return;

            var il = method.Body.GetILProcessor();

            // Insert before every branching instruction
            var branchingInstructions = method.Body.Instructions.Where(i => 
                Constants.JumpingOpCodes.Contains(i.OpCode)).ToList();

            il.Body.SimplifyMacros();
            il.InsertBefore(method.Body.Instructions.First(), il.Create(OpCodes.Call, callCountRef));
            foreach (var instruction in branchingInstructions)
            {
                var targetInstruction = (Instruction) instruction.Operand;
                if (targetInstruction.Offset >= instruction.Offset)
                    continue;
                
                il.InsertAfter(targetInstruction, il.Create(OpCodes.Call, branchCountRef));
            }
            
            il.Body.OptimizeMacros();
        }
    }
}
