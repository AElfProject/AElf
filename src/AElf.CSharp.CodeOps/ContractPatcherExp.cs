using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
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
        private static readonly HashSet<OpCode> JumpingOps = new HashSet<OpCode>
        {
            //OpCodes.Call,
            //OpCodes.Calli,
            //OpCodes.Callvirt,
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

            // ReSharper disable once IdentifierTypo
            var nmspace = contractAsmDef.MainModule.Types.Single(m => m.BaseType is TypeDefinition).Namespace;

            var (counterType, instanceField) = ConstructCounterType(contractAsmDef.MainModule, nmspace);

            var countMethod = ConstructCountMethod(mainModule, instanceField, 100000);
            var resetMethod = ConstructResetMethod(mainModule, instanceField);

            counterType.Methods.Add(countMethod);
            counterType.Methods.Add(resetMethod);

            // Check if already injected
            if (!mainModule.Types.Select(t => t.Name).Contains(counterType.Name))
            {
                // Patch the types
                foreach (var typ in contractAsmDef.MainModule.Types)
                {
                    PatchType(contractAsmDef, typ, countMethod);
                }

                PatchEntryPointsWithReset(mainModule, resetMethod);
            
                mainModule.Types.Add(counterType);
            }

            var newCode = new MemoryStream();
            contractAsmDef.Write(newCode);
            return newCode.ToArray();
        }

        private static (TypeDefinition, FieldDefinition) ConstructCounterType(ModuleDefinition module, string nmspace)
        {
            var counterType = new TypeDefinition(
                nmspace, "InternalInstructionCounter",
                TypeAttributes.Sealed | TypeAttributes.Abstract | TypeAttributes.Public | TypeAttributes.Class,
                module.ImportReference(typeof(object))
            );
            
            var counterField = new FieldDefinition(
                "_counter",
                FieldAttributes.Private | FieldAttributes.Static, 
                module.ImportReference(typeof(int)
                )
            );
            
            // Counter field should be thread static (at least for the test cases)
            counterField.CustomAttributes.Add(new CustomAttribute(
                module.ImportReference(typeof(ThreadStaticAttribute).GetConstructor(new Type[]{}))));

            counterType.Fields.Add(counterField);
            
            var constructor = new MethodDefinition(
                ".cctor", MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.RTSpecialName | MethodAttributes.SpecialName | MethodAttributes.Static,
                module.ImportReference(typeof(void))
            );
            
            var il = constructor.Body.GetILProcessor();
            il.Emit(OpCodes.Nop);
            il.Emit(OpCodes.Ldc_I4_0); // Set default value of counter to 0
            il.Emit(OpCodes.Stsfld, counterField);
            il.Emit(OpCodes.Ret);
            
            counterType.Methods.Add(constructor);
            
            return (counterType, counterField);
        }

        private static MethodDefinition ConstructCountMethod(ModuleDefinition module, FieldDefinition counterField, int threshold)
        {
            var counterMethod = new MethodDefinition(
                "Count", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static, 
                module.ImportReference(typeof(void))
            );
            
            counterMethod.Body.Variables.Add(new VariableDefinition(module.ImportReference(typeof(bool))));
            var il = counterMethod.Body.GetILProcessor();
            
            var ret = il.Create(OpCodes.Ret);
            il.Emit(OpCodes.Ldsfld, counterField);
            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Ldc_I4_1);
            il.Emit(OpCodes.Add_Ovf);
            il.Emit(OpCodes.Stsfld, counterField);
            il.Emit(OpCodes.Ldc_I4, threshold);
            il.Emit(OpCodes.Ceq);
            il.Emit(OpCodes.Stloc_0); //  Variable 0
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Brfalse_S, ret);
            il.Emit(OpCodes.Ldstr, "Contract exceeded maximum calls or allowed loops.");
            //il.Emit(OpCodes.Ldsflda, instanceField);
            //il.Emit(OpCodes.Call, mainModule.ImportReference(typeof(int).GetMethod("ToString", new Type[]{})));
            //il.Emit(OpCodes.Call,mainModule.ImportReference(typeof(string).GetMethod("Concat", new []{typeof(string), typeof(string)})));
            il.Emit(OpCodes.Newobj, module.ImportReference(typeof(RuntimeBranchingThresholdExceededException).GetConstructor(new []{typeof(string)})));
            il.Emit(OpCodes.Throw);
            il.Append(ret);

            return counterMethod;
        }
        
        private static MethodDefinition ConstructResetMethod(ModuleDefinition module, FieldDefinition counterField)
        {
            var resetMethod = new MethodDefinition(
                "Reset", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
                module.ImportReference(typeof(void))
            );
            
            var il = resetMethod.Body.GetILProcessor();
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Stsfld, counterField);
            il.Emit(OpCodes.Ret);
            return resetMethod;
        }

        private static void PatchType(AssemblyDefinition contractAsmDef, TypeDefinition typ, MethodReference counterMethodRef)
        {
            // Patch the methods in the type
            foreach (var method in typ.Methods)
            {
                PatchMethodsWithCounter(method, counterMethodRef);
            }

            // Patch if there is any nested type within the type
            foreach (var nestedType in typ.NestedTypes)
            {
                PatchType(contractAsmDef, nestedType, counterMethodRef);
            }
        }

        private static void PatchMethodsWithCounter(MethodDefinition method, MethodReference counterMethodRef)
        {
            if (!method.HasBody)
                return;

            var il = method.Body.GetILProcessor();

            var branchingInstructions = method.Body.Instructions.Where(i => JumpingOps.Contains(i.OpCode)).ToList();

            il.Body.SimplifyMacros();
            foreach (var instruction in branchingInstructions)
            {
                il.InsertBefore(instruction, il.Create(OpCodes.Call, counterMethodRef));
            }
            il.Body.OptimizeMacros();
        }

        private static void PatchEntryPointsWithReset(ModuleDefinition module, MethodReference resetMethodRef)
        {
            var contractImplementation = module.Types.Where(t => t.BaseType is TypeDefinition).SingleOrDefault(IsContractImplementation);

            foreach (var method in contractImplementation.Methods.Where(m => m.IsPublic))
            {
                var il = method.Body.GetILProcessor();
                
                // Insert reset call as the first instruction
                il.InsertBefore(method.Body.Instructions[0], Instruction.Create(OpCodes.Call, resetMethodRef));
            }
        }

        private static bool IsContractImplementation(TypeDefinition typeDefinition)
        {
            while (true)
            {
                var baseType = typeDefinition.BaseType.Resolve();
                
                if (baseType.BaseType == null) // Reached the type before object type (the most base type)
                {
                    return typeDefinition.FullName == typeof(CSharpSmartContract).FullName;
                }

                typeDefinition = baseType;
            }
        }
    }
}
