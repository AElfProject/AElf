using System.Linq;
using AElf.CSharp.CodeOps.Instructions;
using AElf.Kernel.CodeCheck;
using Mono.Cecil;

namespace AElf.CSharp.CodeOps.Patchers.Module;

public class StateWrittenSizeLimitMethodInjector : IPatcher<ModuleDefinition>
{
    private readonly IStateWrittenInstructionInjector _instructionInjector;

    public StateWrittenSizeLimitMethodInjector(IStateWrittenInstructionInjector instructionInjector)
    {
        _instructionInjector = instructionInjector;
    }

    public bool SystemContactIgnored => true;

    public void Patch(ModuleDefinition module)
    {
        // Patch the types
        foreach (var typ in module.Types)
        {
            PatchType(typ, module);
        }
    }

    private void PatchType(TypeDefinition typ, ModuleDefinition moduleDefinition)
    {
        // Patch the methods in the type
        foreach (var method in typ.Methods)
        {
            PatchMethod(moduleDefinition, method);
        }

        // Patch if there is any nested type within the type
        foreach (var nestedType in typ.NestedTypes)
        {
            PatchType(nestedType, moduleDefinition);
        }
    }

    private void PatchMethod(ModuleDefinition moduleDefinition, MethodDefinition methodDefinition)
    {
        if (!methodDefinition.HasBody)
            return;

        var ilProcessor = methodDefinition.Body.GetILProcessor();

        var instructionsRequiringInjection =
            methodDefinition.Body.Instructions.Where(_instructionInjector.IdentifyInstruction).ToList();
        /*
         TODO: Comment out this first before we handle it properly, see https://github.com/AElfProject/AElf/issues/3389
         var isNotContractImplementation = !methodDefinition.DeclaringType.IsContractImplementation();
        if (instructionsRequiringInjection.Count > 0 && isNotContractImplementation)
        {
            // TODO: https://github.com/AElfProject/AElf/issues/3387
            throw new InvalidCodeException("Updating state in non-contract class.");
        }
        */
        foreach (var instruction in instructionsRequiringInjection)
        {
            _instructionInjector.InjectInstruction(ilProcessor, instruction, moduleDefinition);
        }
    }
}