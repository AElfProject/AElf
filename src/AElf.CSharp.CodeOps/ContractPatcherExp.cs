using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace AElf.CSharp.CodeOps
{
    public class ContractPatcherExp
    {
        private static readonly HashSet<OpCode> JumpingOps = new HashSet<OpCode>
        {
            OpCodes.Call,
            OpCodes.Calli,
            OpCodes.Callvirt,
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
            
            var counterType = new TypeDefinition(
                nmspace, "InternalInstructionCounter",
                TypeAttributes.Sealed | TypeAttributes.Abstract | TypeAttributes.Public | TypeAttributes.Class,
                mainModule.ImportReference(typeof(object))
            );
            
            var instanceField = new FieldDefinition(
                "_counter",
                FieldAttributes.Private | FieldAttributes.Static,
                mainModule.ImportReference(typeof(int))
            );
            
            counterType.Fields.Add(instanceField);
            
            var constructor = new MethodDefinition(
                ".cctor", MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.RTSpecialName | MethodAttributes.SpecialName | MethodAttributes.Static,
                mainModule.ImportReference(typeof(void))
            );

            ILProcessor il;
            il = constructor.Body.GetILProcessor();
            il.Emit(OpCodes.Nop);
            il.Emit(OpCodes.Ldc_I4_0); // Set default value of counter to 0
            il.Emit(OpCodes.Stsfld, instanceField);
            il.Emit(OpCodes.Ret);

            var countMethod = ConstructCountMethod(mainModule, instanceField, 1000);

            counterType.Methods.Add(constructor);
            counterType.Methods.Add(countMethod);

            // Check if already injected
            if (!mainModule.Types.Select(t => t.Name).Contains(counterType.Name))
            {
                // Patch the types
                foreach (var typ in contractAsmDef.MainModule.Types)
                {
                    PatchType(contractAsmDef, typ, countMethod);
                }
            
                mainModule.Types.Add(counterType);
            }

            var newCode = new MemoryStream();
            contractAsmDef.Write(newCode);
            return newCode.ToArray();
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
            il.Emit(OpCodes.Nop);
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
            il.Emit(OpCodes.Newobj, module.ImportReference(typeof(Exception).GetConstructor(new []{typeof(string)})));
            il.Emit(OpCodes.Throw);
            il.Append(ret);

            return counterMethod;
        }

        private static void PatchType(AssemblyDefinition contractAsmDef, TypeDefinition typ, MethodReference counterMethodRef)
        {
            // Patch the methods in the type
            foreach (var method in typ.Methods)
            {
                PatchMethod(method, counterMethodRef);
            }

            // Patch if there is any nested type within the type
            foreach (var nestedType in typ.NestedTypes)
            {
                PatchType(contractAsmDef, nestedType, counterMethodRef);
            }
        }

        private static void PatchMethod(MethodDefinition method, MethodReference counterMethodRef)
        {
            if (!method.HasBody)
                return;

            var ilProcessor = method.Body.GetILProcessor();

            var branchingInstructions = method.Body.Instructions.Where(i => JumpingOps.Contains(i.OpCode)).ToList();

            ilProcessor.Body.SimplifyMacros();
            foreach (var instruction in branchingInstructions)
            {
                ilProcessor.InsertBefore(instruction, ilProcessor.Create(OpCodes.Call, counterMethodRef));
            }
            ilProcessor.Body.OptimizeMacros();
        }
    }
}
