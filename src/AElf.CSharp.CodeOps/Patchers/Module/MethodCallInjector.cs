using System.Collections.Generic;
using System.Linq;
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
            var patchedMethods = new Dictionary<IInstructionInjector, MethodDefinition>();
            foreach (var instructionInjector in _instructionInjectors)
            {
                var methodDefinitionToInject = instructionInjector.PatchMethodReference(module);
                patchedMethods.Add(instructionInjector, methodDefinitionToInject);
            }

            // Patch the types
            foreach (var typ in module.Types)
            {
                PatchType(typ, module, patchedMethods);
            }
        }

        private void PatchType(TypeDefinition typ, ModuleDefinition moduleDefinition,
            Dictionary<IInstructionInjector, MethodDefinition> patchedMethods)
        {
            // Patch the methods in the type
            foreach (var method in typ.Methods)
            {
                PatchMethod(moduleDefinition, method, patchedMethods);
            }

            // Patch if there is any nested type within the type
            foreach (var nestedType in typ.NestedTypes)
            {
                PatchType(nestedType, moduleDefinition, patchedMethods);
            }
        }

        private void PatchMethod(ModuleDefinition moduleDefinition, MethodDefinition methodDefinition,
            Dictionary<IInstructionInjector, MethodDefinition> patchedMethods)
        {
            if (!methodDefinition.HasBody)
                return;

            var ilProcessor = methodDefinition.Body.GetILProcessor();

            foreach (var instructionInjector in _instructionInjectors)
            {
                var methodDefinitionToInject = patchedMethods[instructionInjector];
                
                foreach (var instruction in methodDefinition.Body.Instructions.Where(instruction =>
                    instructionInjector.IdentifyInstruction(moduleDefinition, instruction)).ToList())
                {
                    instructionInjector.InjectInstruction(ilProcessor, instruction, methodDefinitionToInject);
                }
            }
        }
    }
}