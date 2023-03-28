using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AElf.CSharp.CodeOps.Patchers.Module;
using Google.Protobuf.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Volo.Abp.DependencyInjection;

namespace AElf.CSharp.CodeOps.Validators.Module;

public class ResetFieldsValidator : IValidator<ModuleDefinition>, ITransientDependency
{
    public bool SystemContactIgnored => false;

    public IEnumerable<ValidationResult> Validate(ModuleDefinition module, CancellationToken ct)
    {
        var checker = new ExecutionObserverProxyChecker(module);
        var errors = module.Types
            .Where(t => t.Name != nameof(ExecutionObserverProxy))
            .SelectMany(t => ValidateResetFieldsMethod(checker, t, t.IsContractImplementation(), ct))
            .ToList();

        return errors;
    }

    private IEnumerable<ValidationResult> ValidateResetFieldsMethod(ExecutionObserverProxyChecker checker,
        TypeDefinition type, bool isContractImplementation, CancellationToken ct)
    {
        if (ct.IsCancellationRequested)
            throw new ContractAuditTimeoutException();

        var errors = new List<ValidationResult>();

        // Validate nested types (Do not consider any nested types as contract implementation)
        errors.AddRange(type.NestedTypes.SelectMany(t =>
            ValidateResetFieldsMethod(checker, t, false, ct)));

        var fieldsToReset = new List<FieldDefinition>();

        var methodsToCall = type.NestedTypes
            .Select(t => t.Methods.SingleOrDefault(m =>
                m.Name == nameof(ResetFieldsMethodInjector.ResetFields)))
            .Where(t => t != null)
            .ToList();

        var resetFieldsMethod =
            type.Methods.SingleOrDefault(m => m.Name == nameof(ResetFieldsMethodInjector.ResetFields));

        if (isContractImplementation)
        {
            // All fields including static ones and also base class instance fields should be reset
            fieldsToReset.AddRange(type.GetAllFields(f => !(f.IsInitOnly || f.HasConstant)));

            // Contract implementation's ResetFields method should be calling all other types' ResetFields methods
            methodsToCall.AddRange(type.Module.Types
                .Where(t => t != type)
                .Select(t => t.Methods.SingleOrDefault(m =>
                    m.Name == nameof(ResetFieldsMethodInjector.ResetFields)))
                .Where(t => t != null));

            // Ensure contract implementation's ResetFields method is accessible from CSharpSmartContractProxy
            if (resetFieldsMethod != null && !resetFieldsMethod.IsPublic)
            {
                errors.Add(new ResetFieldsValidationResult(
                        $"ResetFields method of contract implementation is not public.")
                    .WithInfo(resetFieldsMethod.Name, type.Namespace, type.Name, null));
            }
        }
        else
        {
            fieldsToReset.AddRange(type.Fields.Where(
                    f => checker.IsObserverFieldThatRequiresResetting(f)
                         || f.IsFuncTypeFieldInGeneratedClass()
                )
            );
        }

        if (resetFieldsMethod == null)
        {
            // If there is no ResetFields method but there are fields to clear or methods to call
            // then the type is missing ResetFields method
            if (fieldsToReset.Count > 0 || methodsToCall.Count > 0)
            {
                errors.Add(new ResetFieldsValidationResult(
                        $"{type.Name} type is missing ResetFields method.")
                    .WithInfo(type.Name, type.Namespace, type.Name, null));
            }
        }
        else
        {
            // Validate whether all fields are reset and other ResetFields methods are called
            // fieldsToReset and methodsToCall variables are supposed to be cleared inside below method
            errors.AddRange(ValidateMethodInstructions(resetFieldsMethod, fieldsToReset, methodsToCall));

            // If there are remaining fields or methods not reset, then add error for those
            if (fieldsToReset.Count > 0)
            {
                errors.Add(new ResetFieldsValidationResult(
                        $"ResetFields method is missing reset for certain fields " +
                        $"{string.Join(", ", fieldsToReset.Select(f => f.Name).ToArray())}")
                    .WithInfo(resetFieldsMethod.Name, type.Namespace, type.Name, null));
            }

            if (methodsToCall.Count > 0)
            {
                errors.Add(new ResetFieldsValidationResult(
                        $"ResetFields method is missing certain method calls " +
                        $"{string.Join(", ", methodsToCall.Select(m => m.DeclaringType + "::" + m.Name).ToArray())}")
                    .WithInfo(resetFieldsMethod.Name, type.Namespace, type.Name, null));
            }
        }

        return errors;
    }

    private IEnumerable<ValidationResult> ValidateMethodInstructions(MethodDefinition resetFieldsMethod,
        List<FieldDefinition> fieldsToReset,
        List<MethodDefinition> methodsToCall)
    {
        var errors = new List<ValidationResult>();

        foreach (var instruction in resetFieldsMethod.Body.Instructions)
        {
            // Skip first instruction in ResetFields method, this is supposed to be CallCount call
            // and validation of this is handled in ObserverProxyValidator
            if (instruction.Offset == 0 &&
                instruction.OpCode == OpCodes.Call &&
                instruction.Operand is MethodReference methodRef &&
                methodRef.Name == nameof(ExecutionObserverProxy.CallCount))
                continue;

            // If ret, assume end of the method and exit the loop
            if (instruction.OpCode == OpCodes.Ret)
                break;

            // Skip if using "this" to set an instance field
            if (instruction.OpCode == OpCodes.Ldarg_0)
                continue;

            // Skip if loading 0 or null to stack
            if (instruction.OpCode == OpCodes.Ldc_I4_0 || instruction.OpCode == OpCodes.Conv_I8 || instruction.OpCode == OpCodes.Ldnull) // Default value
                continue;

            if (instruction.OpCode == OpCodes.Ldfld && instruction.Operand != null &&
                instruction.Operand is FieldReference fieldReference)
            {
                if (fieldReference.Name == "Zero" && fieldReference.DeclaringType.FullName == "System.Decimal")
                {
                    continue;
                }
            }
            
            // If setting a field
            if ((instruction.OpCode == OpCodes.Stfld || instruction.OpCode == OpCodes.Stsfld) &&
                instruction.Operand is FieldDefinition field)
            {
                if (fieldsToReset.Remove(field))
                {
                    continue;
                }
            }

            // If calling a method
            if (instruction.OpCode == OpCodes.Call && instruction.Operand is MethodDefinition method)
            {
                if (methodsToCall.Remove(method))
                {
                    continue;
                }
            }

            // If reached here, then something is wrong
            errors.Add(new ResetFieldsValidationResult(
                    $"Unexpected instruction {instruction} found in ResetFields method.")
                .WithInfo(resetFieldsMethod.Name, resetFieldsMethod.DeclaringType.Namespace,
                    resetFieldsMethod.DeclaringType.Name, null));
        }

        return errors;
    }
}

public class ResetFieldsValidationResult : ValidationResult
{
    public ResetFieldsValidationResult(string message) : base(message)
    {
    }
}