using System.Collections.Generic;
using System.IO;
using System.Linq;
using AElf.Kernel.CodeCheck;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using MethodAttributes = Mono.Cecil.MethodAttributes;

namespace AElf.CSharp.CodeOps.Patchers.Module;

public partial class ResetFieldsMethodInjector : IPatcher<ModuleDefinition>
{
    public bool SystemContactIgnored => false;

    public void Patch(ModuleDefinition module)
    {
        var checker = new ExecutionObserverProxyChecker(module);
        /*
         TODO: Comment out this first before we handle it properly, see https://github.com/AElfProject/AElf/issues/3389
         var disallowedFields = module.GetAllTypes().SelectMany(t => t.Fields).Where(f => f.IsDisallowedStaticField())
            .Where(f => !checker.IsObserverFieldThatRequiresResetting(f)).ToList();
        if (!disallowedFields.IsNullOrEmpty())
        {
            throw new InvalidCodeException("Static field is not allowed in user code.");
        }
        */
        foreach (var type in module.Types.Where(t => !t.IsContractImplementation()))
        {
            InjectTypeWithResetFields(checker, module, type);
            AddCallToNestedClassResetFields(type);
        }

        InjectContractWithResetFields(checker, module);
    }

    private bool InjectTypeWithResetFields(ExecutionObserverProxyChecker checker, ModuleDefinition module,
        TypeDefinition type)
    {
        var callToNestedResetNeeded = false;

        // Inject for nested types first
        foreach (var nestedType in type.NestedTypes)
        {
            callToNestedResetNeeded |= InjectTypeWithResetFields(checker, module, nestedType);
        }

        // Get static non-initonly, non-constant fields
        var fields = type.Fields.Where(
            f => 
            checker.IsObserverFieldThatRequiresResetting(f) ||
            f.IsFuncTypeFieldInGeneratedClass()
        ).ToList();

        callToNestedResetNeeded |= fields.Any();

        if (callToNestedResetNeeded)
        {
            // Inject ResetFields method in type, so that nested calls can be added later
            type.Methods.Add(ConstructResetFieldsMethod(module, fields, false));
        }

        return callToNestedResetNeeded;
    }

    private void AddCallToNestedClassResetFields(TypeDefinition type)
    {
        var parentClassResetMethod = type.Methods.FirstOrDefault(m => m.Name == nameof(ResetFields));

        if (parentClassResetMethod == null)
            return;

        var il = parentClassResetMethod.Body.GetILProcessor();

        var ret = il.Body.Instructions.Last();

        // Get reference of the methods to nested types' reset fields methods
        var toBeCalledFromParent = type.NestedTypes
            .Where(t => t.Methods.Any(m => m.Name == nameof(ResetFields)))
            .Select(t => t.Methods.Single(m => m.Name == nameof(ResetFields)));

        foreach (var nestedResetMethod in toBeCalledFromParent)
        {
            // Add a call to nested class' ResetFields method
            il.InsertBefore(ret, il.Create(OpCodes.Call, nestedResetMethod));
        }

        // Do the same for nested types (add call to their nested types' ResetMethod)
        foreach (var nestedType in type.NestedTypes)
        {
            AddCallToNestedClassResetFields(nestedType);
        }
    }

    private void InjectContractWithResetFields(ExecutionObserverProxyChecker checker, ModuleDefinition module)
    {
        var contractImplementation = module.Types.Single(t => t.IsContractImplementation());

        foreach (var nestedType in contractImplementation.NestedTypes)
        {
            InjectTypeWithResetFields(checker, module, nestedType);
            AddCallToNestedClassResetFields(nestedType);
        }

        var otherTypes = module.Types.Where(t => !t.IsContractImplementation()).ToList();

        // Add contract's nested types as well
        otherTypes.AddRange(contractImplementation.NestedTypes);

        // Get all fields, including instance fields from the base type of the inherited contract type
        var resetFieldsMethod = ConstructResetFieldsMethod(module,
            contractImplementation.GetAllFields(f => !(f.IsInitOnly || f.HasConstant)), true);

        var il = resetFieldsMethod.Body.GetILProcessor();

        var ret = il.Body.Instructions.Last();

        // Call other types' reset fields method from contract implementation reset method
        foreach (var resetMethodRef in otherTypes.Select(type =>
                     type.Methods.FirstOrDefault(m =>
                         m.Name == nameof(ResetFields))).Where(resetMethodRef => resetMethodRef != null))
        {
            il.InsertBefore(ret, il.Create(OpCodes.Call, resetMethodRef));
        }

        contractImplementation.Methods.Add(resetFieldsMethod);
    }

    private MethodDefinition ConstructResetFieldsMethod(ModuleDefinition module, IEnumerable<FieldDefinition> fields,
        bool instanceMethod)
    {
        var attributes = instanceMethod
            ? MethodAttributes.Public | MethodAttributes.HideBySig
            : MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static;

        var resetFieldsMethod = new MethodDefinition(
            nameof(ResetFields),
            attributes,
            module.ImportReference(typeof(void))
        );

        resetFieldsMethod.Body.Variables.Add(new VariableDefinition(module.ImportReference(typeof(bool))));
        var il = resetFieldsMethod.Body.GetILProcessor();

        foreach (var field in fields)
        {
            if (instanceMethod && !field.IsStatic)
                il.Emit(OpCodes.Ldarg_0); // this
            LoadDefaultValue(il, field);
            il.Emit(field.IsStatic ? OpCodes.Stsfld : OpCodes.Stfld, field);
        }

        il.Emit(OpCodes.Ret);

        return resetFieldsMethod;
    }

    private static void LoadDefaultValue(ILProcessor il, FieldReference field)
    {
        if (field.FieldType.FullName == "System.Int64" || field.FieldType.FullName == "System.UInt64")
        {
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Conv_I8);
        }
        else if (field.FieldType.FullName == "System.Decimal")
        {
            il.Emit(OpCodes.Ldfld, field.DeclaringType.Module.ImportReference(DecimalZeroField));
        }
        else if (field.FieldType.IsValueType)
        {
            il.Emit(OpCodes.Ldc_I4_0);
        }
        else
        {
            il.Emit(OpCodes.Ldnull);
        }
    }

    public static void ResetFields()
    {
        // This method is to use its name only
    }
}

public partial class ResetFieldsMethodInjector
{
    private static readonly FieldDefinition DecimalZeroField = null;

    static ResetFieldsMethodInjector()
    {
        var systemRuntime = Basic.Reference.Assemblies.Net60.Resources.SystemRuntime;

        using var stream = new MemoryStream(systemRuntime);

        DecimalZeroField = AssemblyDefinition.ReadAssembly(stream)
            .MainModule.GetAllTypes().Single(t => t.FullName == "System.Decimal")
            .Fields.Single(f => f.Name == "Zero");
    }
}