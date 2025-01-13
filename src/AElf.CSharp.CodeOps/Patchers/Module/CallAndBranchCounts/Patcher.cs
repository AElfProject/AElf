using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace AElf.CSharp.CodeOps.Patchers.Module.CallAndBranchCounts;

public class Patcher : IPatcher<ModuleDefinition>
{
    public bool SystemContactIgnored => true;

    public void Patch(ModuleDefinition module)
    {
        // Check if already injected, do not double inject
        if (module.Types.Select(t => t.Name).Contains(nameof(ExecutionObserverProxy)))
            return;

        // ReSharper disable once IdentifierTypo
        var nmspace = module.Types.Single(m => m.BaseType is TypeDefinition).Namespace;

        var proxyBuilder = new Patch(module, nmspace);

        foreach (var method in module.GetAllTypes().SelectMany(t => t.Methods))
        {
            new MethodPatcher(method, proxyBuilder).DoPatch();
        }

        module.Types.Add(proxyBuilder.ObserverType);
    }
}

internal class MethodPatcher
{
    private readonly Patch _proxy;
    private readonly MethodDefinition _method;
    private bool _alreadyPatched = false;
    private List<Instruction> _allBranchingInstructions;

    internal MethodPatcher(MethodDefinition method, Patch proxy)
    {
        _method = method;
        _proxy = proxy;
    }

    private List<Instruction> AllBranchingInstructions
    {
        get
        {
            if (_allBranchingInstructions == null)
            {
                _allBranchingInstructions = _method.Body.Instructions
                    .Where(i => Constants.JumpingOpCodes.Contains(i.OpCode)).ToList();
            }

            return _allBranchingInstructions;
        }
    }

    public void DoPatch()
    {
        if (!_method.HasBody) return;
        if (_alreadyPatched) return;
        var processor = _method.Body.GetILProcessor();
        processor.Body.SimplifyMacros();
        InsertCallCountAtBeginningOfMethodBody(processor);
        InsertBranchCountForAllBranches(processor);
        processor.Body.OptimizeMacros();
        _alreadyPatched = true;
    }

    private void InsertCallCountAtBeginningOfMethodBody(ILProcessor processor)
    {
        var callCallCountMethod = processor.Create(OpCodes.Call, _proxy.CallCountMethod);
        processor.InsertBefore(_method.Body.Instructions.First(), callCallCountMethod);
    }

    private void InsertBranchCountForAllBranches(ILProcessor processor)
    {
        static bool IsValidInstruction(Instruction instruction)
        {
            var targetInstruction = (Instruction) instruction.Operand;
            return targetInstruction.Offset < instruction.Offset; // What does this mean?
        }

        foreach (var instruction in AllBranchingInstructions.Where(IsValidInstruction))
        {
            var jumpingDestination = (Instruction) instruction.Operand;
            var callBranchCountMethod = processor.Create(OpCodes.Call, _proxy.BranchCountMethod);
            processor.InsertBefore(jumpingDestination, callBranchCountMethod);
            instruction.Operand = callBranchCountMethod;
        }
    }
}