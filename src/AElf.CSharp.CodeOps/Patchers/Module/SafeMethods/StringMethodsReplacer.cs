using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using AElf.Sdk.CSharp;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace AElf.CSharp.CodeOps.Patchers.Module.SafeMethods;

public class StringMethodsReplacer : IPatcher<ModuleDefinition>
{
    private static readonly string Sdk = "AElf.Sdk.CSharp";

    public static readonly IReadOnlyDictionary<string, string> MethodCallReplacements = new Dictionary<string, string>
    {
        {"System.String::Concat", $"{Sdk}.{nameof(AElfString)}"},
        // May add System.String::Format later
    };

    private List<string> _sdkTypes;

    internal List<string> SdkTypes
    {
        get
        {
            if (_sdkTypes == null)
            {
                _sdkTypes = MethodCallReplacements.Select(kv => kv.Value).Distinct().ToList();
            }

            return _sdkTypes;
        }
    }

    private AssemblyDefinition _refSdk;

    internal AssemblyDefinition RefSdk
    {
        get
        {
            if (_refSdk == null)
            {
                _refSdk = AssemblyDefinition.ReadAssembly(typeof(AElfString).Assembly.Location);
            }

            return _refSdk;
        }
    }

    public bool SystemContactIgnored => false;

    public void Patch(ModuleDefinition module)
    {
        new ModulePatcher(module, this).DoPatch();
    }
}

internal class ModulePatcher
{
    private readonly ModuleDefinition _module;
    private readonly Dictionary<string, TypeDefinition> _sdkTypes;

    internal ModulePatcher(ModuleDefinition module, StringMethodsReplacer replacer)
    {
        _module = module;
        _sdkTypes = replacer.SdkTypes
            .Select(t => module.ImportReference(replacer.RefSdk.MainModule.GetType(t)).Resolve())
            .ToDictionary(t => t.FullName);
    }

    public void DoPatch()
    {
        foreach (var method in _module.GetAllTypes().SelectMany(t => t.Methods))
        {
            PatchMethod(method, _sdkTypes);
        }
    }

    private void PatchMethod(MethodDefinition method, Dictionary<string, TypeDefinition> sdkTypeDefs)
    {
        if (!method.HasBody)
            return;

        var processor = method.Body.GetILProcessor();
        processor.Body.SimplifyMacros();

        bool MethodCallNeedsReplacement(Instruction instruction)
        {
            return instruction.OpCode.Code == Code.Call &&
                   StringMethodsReplacer.MethodCallReplacements.ContainsKey(
                       ((MethodReference) instruction.Operand).MemberFullName()
                   );
        }

        // Note: We have to convert it to List before iterating, otherwise it will be problematic when we iterate while rewriting
        var instructions = method.Body.Instructions.Where(MethodCallNeedsReplacement).ToList();
        foreach (var instruction in instructions)
        {
            var oldMethodRef = (MethodReference) instruction.Operand;
            var newMethodRef = method.Module.ImportReference(GetNewMethodReference(oldMethodRef));
            var newMethodCall = processor.Create(OpCodes.Call, newMethodRef);

            processor.Replace(instruction, newMethodCall);
        }
        processor.Body.OptimizeMacros();
    }

    private MethodReference GetNewMethodReference(MethodReference oldMethodReference)
    {
        var oldMethodName = $"{oldMethodReference.DeclaringType}::{oldMethodReference.Name}";
        var newType = StringMethodsReplacer.MethodCallReplacements[oldMethodName];
        var methodWithSameSignatureInNewType = _sdkTypes[newType].Methods.Single(
            m => m.FullNameWithoutDeclaringType() == oldMethodReference.FullNameWithoutDeclaringType()
        );

        return methodWithSameSignatureInNewType;
    }
}