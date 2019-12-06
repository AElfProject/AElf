using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using FieldAttributes = Mono.Cecil.FieldAttributes;
using MethodAttributes = Mono.Cecil.MethodAttributes;
using TypeAttributes = Mono.Cecil.TypeAttributes;

namespace AElf.CSharp.CodeOps
{
    public static class ContractPatcherExp
    {
        public static readonly HashSet<OpCode> JumpingOps = new HashSet<OpCode>
        {
            OpCodes.Beq,
            OpCodes.Beq_S,
            OpCodes.Bge,
            OpCodes.Bge_S,
            OpCodes.Bge_Un,
            OpCodes.Bge_Un_S,
            OpCodes.Bgt,
            OpCodes.Bgt_S,
            OpCodes.Ble,
            OpCodes.Ble_S,
            OpCodes.Ble_Un,
            OpCodes.Blt,
            OpCodes.Bne_Un,
            OpCodes.Bne_Un_S,
            OpCodes.Br,
            OpCodes.Brfalse,
            OpCodes.Brfalse_S,
            OpCodes.Brtrue,
            OpCodes.Brtrue_S,
            OpCodes.Br_S
        };
        
        public static byte[] Patch(byte[] code)
        {
            var contractAsmDef = AssemblyDefinition.ReadAssembly(new MemoryStream(code));

            var mainModule = contractAsmDef.MainModule;

            #if DEBUG
            
            #endif

            // ReSharper disable once IdentifierTypo
            var nmspace = contractAsmDef.MainModule.Types.Single(m => m.BaseType is TypeDefinition).Namespace;

            var (counterProxy, observerField) = ConstructCounterProxy(contractAsmDef.MainModule, nmspace);

            var initializeMethod = ConstructInitializeMethod(mainModule, observerField);
            var proxyCountMethod = ConstructProxyCountMethod(mainModule, observerField);
            
            counterProxy.Methods.Add(initializeMethod);
            counterProxy.Methods.Add(proxyCountMethod);

            // Check if already injected
            if (!mainModule.Types.Select(t => t.Name).Contains(counterProxy.Name))
            {
                // Patch the types
                foreach (var typ in contractAsmDef.MainModule.Types)
                {
                    PatchType(typ, proxyCountMethod);
                }

                mainModule.Types.Add(counterProxy);
            }

            var newCode = new MemoryStream();
            contractAsmDef.Write(newCode);
            return newCode.ToArray();
        }

        private static (TypeDefinition, FieldDefinition) ConstructCounterProxy(ModuleDefinition module, string nmspace)
        {
            var counterType = new TypeDefinition(
                nmspace, nameof(ExecutionObserverProxy),
                TypeAttributes.Sealed | TypeAttributes.Abstract | TypeAttributes.Public | TypeAttributes.Class,
                module.ImportReference(typeof(object))
            );
            
            var counterField = new FieldDefinition(
                "_observer",
                FieldAttributes.Private | FieldAttributes.Static, 
                module.ImportReference(typeof(IExecutionObserver)
                )
            );
            
            // Counter field should be thread static (at least for the test cases)
            counterField.CustomAttributes.Add(new CustomAttribute(
                module.ImportReference(typeof(ThreadStaticAttribute).GetConstructor(new Type[]{}))));

            counterType.Fields.Add(counterField);

            return (counterType, counterField);
        }

        private static MethodDefinition ConstructProxyCountMethod(ModuleDefinition module, FieldReference observerField)
        {
            var counterMethod = new MethodDefinition(
                nameof(ExecutionObserverProxy.Count), 
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static, 
                module.ImportReference(typeof(void))
            );
            
            counterMethod.Body.Variables.Add(new VariableDefinition(module.ImportReference(typeof(bool))));
            var il = counterMethod.Body.GetILProcessor();

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

            return counterMethod;
        }
        
        private static MethodDefinition ConstructInitializeMethod(ModuleDefinition module, FieldReference observerField)
        {
            var initializeMethod = new MethodDefinition(
                nameof(ExecutionObserverProxy.Initialize), 
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static, 
                module.ImportReference(typeof(void))
            );

            initializeMethod.Parameters.Add(new ParameterDefinition("observer", 
                ParameterAttributes.In, module.ImportReference(typeof(IExecutionObserver))));
            
            var il = initializeMethod.Body.GetILProcessor();
            
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Stsfld, observerField);
            #if DEBUG
            il.Emit(OpCodes.Ldsfld, observerField);
            il.Emit(OpCodes.Call, module.ImportReference(typeof(ExecutionObserverDebugger).
                GetMethod(nameof(ExecutionObserverDebugger.Test), new []{ typeof(IExecutionObserver) })));
            #endif
            il.Emit(OpCodes.Ret);

            return initializeMethod;
        }

        private static void PatchType(TypeDefinition typ, MethodReference counterMethodRef)
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

        private static void PatchMethodsWithCounter(MethodDefinition method, MethodReference counterMethodRef)
        {
            if (!method.HasBody)
                return;

            var il = method.Body.GetILProcessor();

            // Insert before every branching instruction
            var branchingInstructions = method.Body.Instructions.Where(i => JumpingOps.Contains(i.OpCode)).ToList();
            il.Body.SimplifyMacros();
            foreach (var instruction in branchingInstructions)
            {
                il.InsertBefore(instruction, il.Create(OpCodes.Call, counterMethodRef));
            }
            il.Body.OptimizeMacros();
        }
    }
}
