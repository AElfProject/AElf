using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Volo.Abp.DependencyInjection;

namespace AElf.CSharp.CodeOps.Validators.Module;

public class ObserverProxyValidator : IValidator<ModuleDefinition>, ITransientDependency
{
    private readonly TypeDefinition _counterProxyTypeRef;
    private TypeDefinition _injProxyType;
    private MethodDefinition _injProxySetObserver;
    private MethodDefinition _injProxyBranchCount;
    private MethodDefinition _injProxyCallCount;

    public ObserverProxyValidator()
    {
        // Module is only used to construct the type
        var module = AssemblyDefinition.ReadAssembly(typeof(ExecutionObserverProxy).Assembly.Location).MainModule;
        _counterProxyTypeRef =
            new AElf.CSharp.CodeOps.Patchers.Module.CallAndBranchCounts.Patch(module, "AElf.Reference").ObserverType;
    }

    public bool SystemContactIgnored => true;

    public IEnumerable<ValidationResult> Validate(ModuleDefinition module, CancellationToken ct)
    {
        var errors = new List<ValidationResult>();

        // Get proxy type reference
        _injProxyType = module.Types.SingleOrDefault(t => t.Name == nameof(ExecutionObserverProxy));

        if (_injProxyType == null)
            return new List<ValidationResult>
            {
                new ObserverProxyValidationResult("Could not find execution observer proxy in contract.")
            };

        CheckObserverProxyIsNotTampered(errors);

        // Get references for proxy methods
        _injProxySetObserver =
            _injProxyType.Methods.SingleOrDefault(m => m.Name == nameof(ExecutionObserverProxy.SetObserver));
        _injProxyBranchCount =
            _injProxyType.Methods.SingleOrDefault(m => m.Name == nameof(ExecutionObserverProxy.BranchCount));
        _injProxyCallCount =
            _injProxyType.Methods.SingleOrDefault(m => m.Name == nameof(ExecutionObserverProxy.CallCount));

        foreach (var typ in module.Types)
        {
            CheckCallsFromTypes(errors, typ, ct);
        }

        return errors;
    }

    private void CheckObserverProxyIsNotTampered(List<ValidationResult> errors)
    {
        if (!_injProxyType.HasSameFields(_counterProxyTypeRef))
        {
            errors.Add(new ObserverProxyValidationResult(_injProxyType.Name + " type has different fields."));
        }

        foreach (var refMethod in _counterProxyTypeRef.Methods)
        {
            var injMethod = _injProxyType.Methods.SingleOrDefault(m => m.Name == refMethod.Name);

            if (injMethod == null)
            {
                errors.Add(new ObserverProxyValidationResult(refMethod.Name +
                                                             " is not implemented in observer proxy."));
            }

            if (!injMethod.HasSameBody(refMethod))
            {
                var contractMethodBody =
                    string.Join("\n", injMethod?.Body.Instructions.Select(i => i.ToString()).ToArray());
                var referenceMethodBody =
                    string.Join("\n", refMethod?.Body.Instructions.Select(i => i.ToString()).ToArray());

                errors.Add(new ObserverProxyValidationResult(
                    $"{refMethod.Name} proxy method body is tampered.\n" +
                    $"Injected Contract: \n{contractMethodBody}\n\n" +
                    $"Reference:\n{referenceMethodBody}"));
            }

            if (!injMethod.HasSameParameters(refMethod))
            {
                errors.Add(new ObserverProxyValidationResult(refMethod.Name +
                                                             " proxy method accepts different parameters."));
            }
        }

        if (_injProxyType.Methods.Count != _counterProxyTypeRef.Methods.Count)
            errors.Add(new ObserverProxyValidationResult("Observer type contains unusual number of methods."));
    }

    private void CheckCallsFromTypes(List<ValidationResult> errors, TypeDefinition typ, CancellationToken ct)
    {
        if (typ == _injProxyType) // Do not need to validate calls from the injected proxy
            return;

        // Patch the methods in the type
        foreach (var method in typ.Methods.Where(m => !m.IsConstructor))
        {
            CheckCallsFromMethods(errors, method, ct);
        }

        // Patch if there is any nested type within the type
        foreach (var nestedType in typ.NestedTypes)
        {
            CheckCallsFromTypes(errors, nestedType, ct);
        }
    }

    private bool IsFollowedByCallToBranchCount(Instruction instruction)
    {
        if (instruction == null)
        {
            return false;
        }
        if (instruction.OpCode == OpCodes.Nop)
            return instruction.Next != null && IsFollowedByCallToBranchCount(instruction.Next);
        return instruction.OpCode == OpCodes.Call &&
               instruction.Operand == _injProxyBranchCount;
    }

    private void CheckCallsFromMethods(List<ValidationResult> errors, MethodDefinition method, CancellationToken ct)
    {
        if (ct.IsCancellationRequested)
            throw new ContractAuditTimeoutException();

        if (!method.HasBody)
            return;

        // First instruction should be a call to proxy call count method
        var firstInstruction = method.Body.Instructions.First();
        if (!(firstInstruction.OpCode == OpCodes.Call && firstInstruction.Operand == _injProxyCallCount))
            errors.Add(new ObserverProxyValidationResult($"Missing execution observer call count call detected. " +
                                                         $"[{method.DeclaringType.Name} > {method.Name}]"));

        // Should be a call placed before each branching opcode
        foreach (var instruction in method.Body.Instructions)
        {
            if (Constants.JumpingOpCodes.Contains(instruction.OpCode)
                && instruction.Operand is Instruction targetInstruction
                && targetInstruction.Offset < instruction.Offset)
            {
                var targetIsCallToBranchCount = IsFollowedByCallToBranchCount(targetInstruction);
                // Note: instructionAfterTargetIsCallToBranchCount is added for backward-compatibility, the call was
                //       previously injected not at the target position but after the target position.
                var instructionAfterTargetIsCallToBranchCount = IsFollowedByCallToBranchCount(targetInstruction.Next);
                if (!targetIsCallToBranchCount && !instructionAfterTargetIsCallToBranchCount)
                {
                    errors.Add(new ObserverProxyValidationResult(
                        "Missing execution observer branch count call detected. " +
                        $"[{method.DeclaringType.Name} > {method.Name}]"));
                }
            }

            // Calling SetObserver method within contract is a breach
            if (instruction.OpCode == OpCodes.Call && instruction.Operand == _injProxySetObserver)
            {
                errors.Add(new ObserverProxyValidationResult(
                    $"Proxy initialize call detected from within the contract. " +
                    $"[{method.DeclaringType.Name} > {method.Name}]"));
            }
        }
    }
}

public class ObserverProxyValidationResult : ValidationResult
{
    public ObserverProxyValidationResult(string message) : base(message)
    {
    }
}