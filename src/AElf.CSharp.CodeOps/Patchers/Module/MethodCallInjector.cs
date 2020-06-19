using System.Collections.Generic;
using System.Linq;
using AElf.CSharp.CodeOps.Instructions;
using Mono.Cecil;

namespace AElf.CSharp.CodeOps.Patchers.Module
{
    public class MethodCallInjector : IPatcher<ModuleDefinition>
    {
        private readonly List<IInstructionInjector> _instructionInjectors;

        public MethodCallInjector(IEnumerable<IInstructionInjector> instructionInjectors)
        {
            _instructionInjectors = instructionInjectors.ToList();
        }

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

            foreach (var instructionInjector in _instructionInjectors)
            {
                foreach (var instruction in methodDefinition.Body.Instructions.Where(instruction =>
                    instructionInjector.IdentifyInstruction(instruction)).ToList())
                {
                    instructionInjector.InjectInstruction(ilProcessor, instruction, moduleDefinition);
                }
            }
        }
    }
}