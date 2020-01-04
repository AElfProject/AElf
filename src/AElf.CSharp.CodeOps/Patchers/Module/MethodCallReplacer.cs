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
        
        private static readonly ReplaceRules MethodCallsToReplace = new ReplaceRules()
            .CallsTo("System.String::Concat", i => i
                .Replace($"{Sdk}.{nameof(AElfString)}"))
            .CallsTo("System.Object::GetHashCode", i => i
                .Replace($"{Sdk}.{nameof(AElfString)}", typeof(string)));

        // Replace unchecked math OpCodes with checked OpCodes (overflow throws exception)
        private static readonly Dictionary<OpCode, OpCode> OpCodesToReplace = new Dictionary<OpCode, OpCode>()
        {
            {OpCodes.Add, OpCodes.Add_Ovf},
            {OpCodes.Sub, OpCodes.Sub_Ovf},
            {OpCodes.Mul, OpCodes.Mul_Ovf}
        };

        public void Patch(ModuleDefinition module)
        {
            // Get the specific version of the SDK referenced by the contract
            var nameRefSdk = module.AssemblyReferences.Single(r => r.Name == Sdk);
            
            // May cache all versions not to keep reloading for every contract deployment
            var refSdk = AssemblyDefinition.ReadAssembly(Assembly.Load(nameRefSdk.FullName).Location);

            // Get the type definitions mapped for target methods from SDK
            var sdkTypes = MethodCallsToReplace.MethodCalls.SelectMany(kv => 
                kv.Value.InstanceTypes.Values).Distinct();
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
                    
            var methodCallsToReplace = method.Body.Instructions.Where(i => 
                    (i.OpCode.Code == Code.Call || i.OpCode.Code == Code.Callvirt) && 
                    MethodCallsToReplace.MethodCalls.Any(m => 
                        ((MethodReference) i.Operand).FullName.Contains(m.Key) && 
                        m.Value.InstanceTypes.Keys.Contains(i.GetInstanceTypeInStack(method)?.ToString() ?? "ALL")
                        )
                    )
                .ToList();

            foreach (var instruction in methodCallsToReplace)
            {
                var sysMethodRef = (MethodReference) instruction.Operand;
                var newMethodRef = method.Module.ImportReference(GetSdkMethodReference(sdkTypeDefs, sysMethodRef, instruction.GetInstanceTypeInStack(method)?.ToString()));

                ilProcessor.Replace(instruction, ilProcessor.Create(OpCodes.Call, newMethodRef));
            }

            var opCodesToReplace = method.Body.Instructions.Where(i => OpCodesToReplace.Keys.Contains(i.OpCode)).ToList();
            foreach (var instruction in opCodesToReplace)
            {
                ilProcessor.Replace(instruction, ilProcessor.Create(OpCodesToReplace[instruction.OpCode]));
            }
        }

        private MethodReference GetSdkMethodReference(Dictionary<string, TypeDefinition> sdkTypeDefs, MethodReference methodRef, string instanceType)
        {
            // Find the right method that has the same set of parameters and return type
            var replaceInfos = MethodCallsToReplace.MethodCalls[$"{methodRef.DeclaringType}::{methodRef.Name}"];

            var replaceFromType = "";
            if (replaceInfos.InstanceTypes.Keys.Count() == 1 && replaceInfos.InstanceTypes.First().Key == "ALL")
                replaceFromType = replaceInfos.InstanceTypes["ALL"];
            else
                replaceFromType = replaceInfos.InstanceTypes[instanceType];

            MethodDefinition methodDefinition;
            if (methodRef.HasParameters)
            {
                methodDefinition = sdkTypeDefs[replaceFromType].Methods.Single(
                    m => m.ReturnType.FullName == methodRef.ReturnType.FullName && // Return type
                         m.FullName.Split(new [] {"::"}, StringSplitOptions.None)[1] == 
                         methodRef.FullName.Split(new [] {"::"}, StringSplitOptions.None)[1] // Method Name & Parameters
                );
            }
            else
            {
                // Instance will be input parameter for the method in SDK
                methodDefinition = sdkTypeDefs[replaceFromType].Methods.Single(
                    m => m.ReturnType.FullName == methodRef.ReturnType.FullName && // Return type
                         m.FullName.Split(new [] {"::"}, StringSplitOptions.None)[1] == 
                         $"{methodRef.Name}({instanceType})" // Method Name & Parameters
                );
            }

            return methodDefinition;
        }
    }

    public class ReplaceRules
    {
        private readonly IDictionary<string, ReplaceInfo> _methodCalls = new Dictionary<string, ReplaceInfo>();
        
        public IReadOnlyDictionary<string, ReplaceInfo> MethodCalls => (IReadOnlyDictionary<string, ReplaceInfo>) _methodCalls;

        public ReplaceRules CallsTo(string methodName, Action<ReplaceInfo> replaceInfo = null)
        {
            var info = new ReplaceInfo(methodName);

            _methodCalls[methodName] = info;
            
            replaceInfo?.Invoke(info);

            return this;
        }
    }

    public class ReplaceInfo
    {
        public string Name { get; }

        private readonly IDictionary<string, string> _instanceTypes = new Dictionary<string, string>();

        public IReadOnlyDictionary<string, string> InstanceTypes => (IReadOnlyDictionary<string, string>) _instanceTypes;

        public ReplaceInfo(string name)
        {
            Name = name;
        }

        public ReplaceInfo Replace(string withMethodsFrom, Type onlyForInstanceOf = null)
        {
            if (onlyForInstanceOf != null)
                _instanceTypes[onlyForInstanceOf.ToString()] = withMethodsFrom;
            else
                _instanceTypes["ALL"] = withMethodsFrom;

            return this;
        }
    }
}
