using System;
using System.Linq;
using AElf.Sdk.CSharp;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace AElf.CSharp.CodeOps.Patchers.Module
{
    public class ExecutionObserverInjector : IPatcher<ModuleDefinition>
    {
        public void Patch(ModuleDefinition module)
        {
            // Check if already injected, do not double inject
            if (module.Types.Select(t => t.Name).Contains(nameof(ExecutionObserverProxy)))
                return;
            
            // ReSharper disable once IdentifierTypo
            var nmspace = module.Types.Single(m => m.BaseType is TypeDefinition).Namespace;

            var (counterProxy, observerField) = ConstructCounterProxy(module, nmspace);

            var proxySetObserverMethod = ConstructProxySetObserverMethod(module, observerField);
            var proxyCountMethod = ConstructProxyCountMethod(module, observerField);
            var proxyGetUsageMethod = ConstructProxyGetUsageMethod(module, observerField);
            
            counterProxy.Methods.Add(proxySetObserverMethod);
            counterProxy.Methods.Add(proxyCountMethod);
            counterProxy.Methods.Add(proxyGetUsageMethod);

            // Patch the types
            foreach (var typ in module.Types)
            {
                PatchType(typ, proxyCountMethod);
            }

            module.Types.Add(counterProxy);
        }

        private (TypeDefinition, FieldDefinition) ConstructCounterProxy(ModuleDefinition module, string nmspace)
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

            return (observerType, observerField);
        }

        private MethodDefinition ConstructProxyCountMethod(ModuleDefinition module, FieldReference observerField)
        {
            var countMethod = new MethodDefinition(
                nameof(ExecutionObserverProxy.Count), 
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static, 
                module.ImportReference(typeof(void))
            );
            
            countMethod.Body.Variables.Add(new VariableDefinition(module.ImportReference(typeof(bool))));
            var il = countMethod.Body.GetILProcessor();

            var ret = il.Create(OpCodes.Ret);
            
            #if DEBUG
            il.Emit(OpCodes.Ldsfld, observerField);
            il.Emit(OpCodes.Call, module.ImportReference(typeof(ExecutionObserverDebugger).
                GetMethod(nameof(ExecutionObserverDebugger.Test), new []{ typeof(IExecutionObserver) })));
            #endif
            il.Emit(OpCodes.Ldsfld, observerField);
            il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Cgt_Un);
            il.Emit(OpCodes.Stloc_0);
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Brfalse_S, ret); // Do not call if not initialized
            il.Emit(OpCodes.Ldsfld, observerField);
            il.Emit(OpCodes.Callvirt, module.ImportReference(
                typeof(IExecutionObserver).GetMethod(nameof(IExecutionObserver.Count))));
            il.Append(ret);

            return countMethod;
        }

        private MethodDefinition ConstructProxyGetUsageMethod(ModuleDefinition module, FieldReference observerField)
        {
            var getUsageMethod = new MethodDefinition(
                nameof(ExecutionObserverProxy.GetUsage), 
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static, 
                module.ImportReference(typeof(int))
            );
            
            getUsageMethod.Body.Variables.Add(new VariableDefinition(module.ImportReference(typeof(bool))));
            getUsageMethod.Body.Variables.Add(new VariableDefinition(module.ImportReference(typeof(int))));
            
            var il = getUsageMethod.Body.GetILProcessor();
            
            var setZero = il.Create(OpCodes.Ldc_I4_0);
            var loadFirstVar = il.Create(OpCodes.Ldloc_1);
            
            #if DEBUG
            il.Emit(OpCodes.Ldsfld, observerField);
            il.Emit(OpCodes.Call, module.ImportReference(typeof(ExecutionObserverDebugger).
                GetMethod(nameof(ExecutionObserverDebugger.Test), new []{ typeof(IExecutionObserver) })));
            #endif
            il.Emit(OpCodes.Ldsfld, observerField);
            il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Cgt_Un);
            il.Emit(OpCodes.Stloc_0);
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Brfalse_S, setZero);
            il.Emit(OpCodes.Ldsfld, observerField);
            
            il.Emit(OpCodes.Callvirt, module.ImportReference(
                typeof(IExecutionObserver).GetMethod(nameof(IExecutionObserver.GetUsage))));
            il.Emit(OpCodes.Stloc_1);
            il.Emit(OpCodes.Br_S, loadFirstVar);
            il.Append(setZero);
            il.Emit(OpCodes.Stloc_1);
            il.Emit(OpCodes.Br_S, loadFirstVar);
            il.Append(loadFirstVar);
            il.Emit(OpCodes.Ret);

            return getUsageMethod;
        }

        private MethodDefinition ConstructProxySetObserverMethod(ModuleDefinition module, FieldReference observerField)
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
            #if DEBUG
            il.Emit(OpCodes.Ldsfld, observerField);
            il.Emit(OpCodes.Call, module.ImportReference(typeof(ExecutionObserverDebugger).
                GetMethod(nameof(ExecutionObserverDebugger.Test), new []{ typeof(IExecutionObserver) })));
            #endif
            il.Emit(OpCodes.Ret);

            return setObserverMethod;
        }

        private void PatchType(TypeDefinition typ, MethodReference counterMethodRef)
        {
            // Patch the methods in the type
            foreach (var method in typ.Methods)
            {
                PatchMethodsWithCounter(method, counterMethodRef);
            }

            // Patch if there is any nested type within the type
            foreach (var nestedType in typ.NestedTypes)
            {
                PatchType(nestedType, counterMethodRef);
            }
        }

        private void PatchMethodsWithCounter(MethodDefinition method, MethodReference counterMethodRef)
        {
            if (!method.HasBody)
                return;

            var il = method.Body.GetILProcessor();

            // Insert before every branching instruction
            var branchingInstructions = method.Body.Instructions.Where(i => 
                Consts.JumpingOps.Contains(i.OpCode)).ToList();

            il.Body.SimplifyMacros();
            il.InsertBefore(method.Body.Instructions.First(), il.Create(OpCodes.Call, counterMethodRef));
            foreach (var instruction in branchingInstructions)
            {
                il.InsertBefore(instruction, il.Create(OpCodes.Call, counterMethodRef));
            }
            il.Body.OptimizeMacros();
        }
    }
}
