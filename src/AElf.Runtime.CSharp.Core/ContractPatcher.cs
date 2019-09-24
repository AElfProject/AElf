using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using AElf.Sdk.CSharp;
using Mono.Cecil.Cil;
using AssemblyDefinition = Mono.Cecil.AssemblyDefinition;
using TypeDefinition = Mono.Cecil.TypeDefinition;

namespace AElf.Runtime.CSharp
{
    public class ContractPatcher
    {
        private static readonly Dictionary<string, string> TargetMethods = new Dictionary<string, string>
        {
            {"System.String::Concat", nameof(AElfString)},
            // May add System.String::Format later
        };

        public static byte[] Patch(byte[] code)
        {
            var contractAsmDef = AssemblyDefinition.ReadAssembly(new MemoryStream(code));

            //TODO: Get type reference from specific version of AElf.Sdk dependency
            //var nameRefSdk = contractAsmDef.MainModule.AssemblyReferences.Single(r => r.Name == "AElf.Sdk");
            //var refSdk = AssemblyDefinition.ReadAssembly(nameRefSdk.FullName);

            //TODO: Import reference for all types mapped in TargetMethods
            var aelfStr = contractAsmDef.MainModule.ImportReference(typeof(AElfString));

            var aelfStrDef = aelfStr.Resolve();
            
            foreach (var typ in contractAsmDef.MainModule.Types)
            {
                PatchType(contractAsmDef, typ, aelfStrDef);
            }

            var newCode = new MemoryStream();
            contractAsmDef.Write(newCode);
            return newCode.ToArray();
        }

        private static void PatchType(AssemblyDefinition contractAsmDef, TypeDefinition typ, TypeDefinition sdkTypeDef)
        {
            foreach (var method in typ.Methods)
            {
                PatchMethod(contractAsmDef, method, sdkTypeDef);
            }

            foreach (var nestedType in typ.NestedTypes)
            {
                PatchType(contractAsmDef, nestedType, sdkTypeDef);
            }
        }

        private static void PatchMethod(AssemblyDefinition contractAsmDef, MethodDefinition method, TypeDefinition sdkTypeDef)
        {
            if (!method.HasBody)
                return;
            
            var ilProcessor = method.Body.GetILProcessor();
                    
            var instructionsToReplace = method.Body.Instructions.Where(i => 
                    i.OpCode.Code == Code.Call && TargetMethods.Any(m => ((MethodReference) i.Operand).FullName.Contains(m.Key)))
                .ToList();

            foreach (var instruction in instructionsToReplace)
            {
                var sysMethodRef = (MethodReference) instruction.Operand;
                var newMethodRef = contractAsmDef.MainModule.ImportReference(GetSdkMethodReference(sdkTypeDef, sysMethodRef));

                ilProcessor.InsertBefore(instruction, ilProcessor.Create(OpCodes.Call, newMethodRef));
                ilProcessor.Remove(instruction);
            }
        }

        private static MethodReference GetSdkMethodReference(TypeDefinition aelfStrDef, MethodReference methodRef)
        {
            // Find the right method that has the same set of parameters and return type
            var methodDefinition = aelfStrDef.Methods.Single(
                m => m.ReturnType.FullName == methodRef.ReturnType.FullName && // Return type
                     m.FullName.Split("::")[1] == methodRef.FullName.Split("::")[1] // Parameters
            );

            return methodDefinition;
        }
    }
}
