using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AElf.Sdk.CSharp;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AElf.CSharp.CodeOps.Patchers.Module
{
    public class MethodCallReplacer : IPatcher<ModuleDefinition>
    {
        private static readonly string Sdk = "AElf.Sdk.CSharp";
        private static readonly Dictionary<string, string> TargetMethods = new Dictionary<string, string>
        {
            {"System.String::Concat", $"{Sdk}.{nameof(AElfString)}"},
            // May add System.String::Format later
        };

        public void Patch(ModuleDefinition module)
        {
            // Get the specific version of the SDK referenced by the contract
            var nameRefSdk = module.AssemblyReferences.Single(r => r.Name == Sdk);
            
            // May cache all versions not to keep reloading for every contract deployment
            var refSdk = AssemblyDefinition.ReadAssembly(Assembly.Load(nameRefSdk.FullName).Location);

            // Get the type definitions mapped for target methods from SDK
            var sdkTypes = TargetMethods.Select(kv => kv.Value).Distinct();
            var sdkTypeDefs = sdkTypes
                .Select(t => module.ImportReference(refSdk.MainModule.GetType(t)).Resolve())
                .ToDictionary(def => def.FullName);

            // Patch the types
            foreach (var typ in module.Types)
            {
                PatchType(typ, sdkTypeDefs);
            }
        }

        private void PatchType(TypeDefinition typ, Dictionary<string, TypeDefinition> sdkTypeDefs)
        {
            // Patch the methods in the type
            foreach (var method in typ.Methods)
            {
                PatchMethod(method, sdkTypeDefs);
            }

            // Patch if there is any nested type within the type
            foreach (var nestedType in typ.NestedTypes)
            {
                PatchType(nestedType, sdkTypeDefs);
            }
        }

        private void PatchMethod(MethodDefinition method, Dictionary<string, TypeDefinition> sdkTypeDefs)
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
                var newMethodRef = method.Module.ImportReference(GetSdkMethodReference(sdkTypeDefs, sysMethodRef));

                ilProcessor.InsertBefore(instruction, ilProcessor.Create(OpCodes.Call, newMethodRef));
                ilProcessor.Remove(instruction);
            }
        }

        private MethodReference GetSdkMethodReference(Dictionary<string, TypeDefinition> sdkTypeDefs, MethodReference methodRef)
        {
            // Find the right method that has the same set of parameters and return type
            var replaceFrom = TargetMethods[$"{methodRef.DeclaringType}::{methodRef.Name}"];
            var methodDefinition = sdkTypeDefs[replaceFrom].Methods.Single(
                m => m.ReturnType.FullName == methodRef.ReturnType.FullName && // Return type
                     m.FullName.Split(new [] {"::"}, StringSplitOptions.None)[1] == 
                     methodRef.FullName.Split(new [] {"::"}, StringSplitOptions.None)[1] // Method Name & Parameters
            );

            return methodDefinition;
        }
    }
}
