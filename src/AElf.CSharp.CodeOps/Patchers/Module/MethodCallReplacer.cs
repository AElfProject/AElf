using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AElf.Sdk.CSharp;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AElf.CSharp.CodeOps.Patchers.Module;

public class MethodCallReplacer : IPatcher<ModuleDefinition>
{
    private static readonly string Sdk = "AElf.Sdk.CSharp";
        
    private static readonly Dictionary<string, string> MethodCallsToReplace = new Dictionary<string, string>
    {
        {"System.String::Concat", $"{Sdk}.{nameof(AElfString)}"},
        // May add System.String::Format later
    };
        
    // Replace unchecked math OpCodes with checked OpCodes (overflow throws exception)
    private static readonly Dictionary<OpCode, OpCode> OpCodesToReplace = new Dictionary<OpCode, OpCode>()
    {
        {OpCodes.Add, OpCodes.Add_Ovf},
        {OpCodes.Sub, OpCodes.Sub_Ovf},
        {OpCodes.Mul, OpCodes.Mul_Ovf}
    };
        
    public bool SystemContactIgnored => false;

    public void Patch(ModuleDefinition module)
    {
        // May cache all versions not to keep reloading for every contract deployment
        var refSdk = AssemblyDefinition.ReadAssembly(Assembly.GetAssembly(typeof(AElfString)).Location);

        // Get the type definitions mapped for target methods from SDK
        var sdkTypes = MethodCallsToReplace.Select(kv => kv.Value).Distinct();
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
                i.OpCode.Code == Code.Call && MethodCallsToReplace.Any(m => ((MethodReference) i.Operand).FullName.Contains(m.Key)))
            .ToList();

        foreach (var instruction in methodCallsToReplace)
        {
            var sysMethodRef = (MethodReference) instruction.Operand;
            var newMethodRef = method.Module.ImportReference(GetSdkMethodReference(sdkTypeDefs, sysMethodRef));

            ilProcessor.Replace(instruction, ilProcessor.Create(OpCodes.Call, newMethodRef));
        }

        var opCodesToReplace = method.Body.Instructions.Where(i => OpCodesToReplace.Keys.Contains(i.OpCode)).ToList();
        foreach (var instruction in opCodesToReplace)
        {
            ilProcessor.Replace(instruction, ilProcessor.Create(OpCodesToReplace[instruction.OpCode]));
        }
    }

    private MethodReference GetSdkMethodReference(Dictionary<string, TypeDefinition> sdkTypeDefs, MethodReference methodRef)
    {
        // Find the right method that has the same set of parameters and return type
        var replaceFrom = MethodCallsToReplace[$"{methodRef.DeclaringType}::{methodRef.Name}"];
        var methodDefinition = sdkTypeDefs[replaceFrom].Methods.Single(
            m => m.ReturnType.FullName == methodRef.ReturnType.FullName && // Return type
                 m.FullName.Split(new [] {"::"}, StringSplitOptions.None)[1] == 
                 methodRef.FullName.Split(new [] {"::"}, StringSplitOptions.None)[1] // Method Name & Parameters
        );

        return methodDefinition;
    }
}