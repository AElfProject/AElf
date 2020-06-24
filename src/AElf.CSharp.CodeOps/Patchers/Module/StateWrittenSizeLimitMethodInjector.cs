using System.Linq;
using AElf.CSharp.CodeOps.Instructions;
using Mono.Cecil;

namespace AElf.CSharp.CodeOps.Patchers.Module
{
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

            foreach (var instruction in methodDefinition.Body.Instructions.Where(instruction =>
                _instructionInjector.IdentifyInstruction(instruction)).ToList())
            {
                _instructionInjector.InjectInstruction(ilProcessor, instruction, moduleDefinition);
            }
        }
    }
}