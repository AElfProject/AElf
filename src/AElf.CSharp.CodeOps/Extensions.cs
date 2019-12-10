using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using AElf.Sdk.CSharp;

namespace AElf.CSharp.CodeOps
{
    public static class Extensions
    {
        public static bool IsContractImplementation(this TypeDefinition typeDefinition)
        {
            while (true)
            {
                var baseType = typeDefinition.BaseType.Resolve();
                
                if (baseType.BaseType == null) // Reached the type before object type (the most base type)
                {
                    return typeDefinition.FullName == typeof(CSharpSmartContract).FullName;
                }

                typeDefinition = baseType;
            }
        }

        public static bool HasSameBody(this MethodDefinition sourceMethod, MethodDefinition targetMethod)
        {
            // Exclude nop opcodes (compile in debug mode adds nop to be able to place breakpoint, ignore those)
            var sourceMethodBodyInstructions = sourceMethod.Body.Instructions.Where(i => i.OpCode != OpCodes.Nop).ToArray();
            var targetMethodBodyInstructions = targetMethod.Body.Instructions.Where(i => i.OpCode != OpCodes.Nop).ToArray();

            // Compare method body
            for (var i = 0; i < sourceMethodBodyInstructions.Count(); i++)
            {
                if (sourceMethodBodyInstructions[i].ToComparableString() == targetMethodBodyInstructions[i].ToComparableString())
                    continue;

                return false;
            }

            return true;
        }

        private static string ToComparableString(this Instruction instruction)
        {
            string operandStr;
            
            if (instruction.Operand is FieldDefinition field)
            {
                operandStr = field.Name;
            }
            else if (instruction.Operand is Instruction ins) // Is probably branching to another instruction
            {
                operandStr = ins.OpCode.ToString() + ins.Operand; // May restrict to branching to ret only
            }
            else
            {
                operandStr = instruction.Operand?.ToString();
            }

            return instruction.OpCode + operandStr;
        }

        public static bool HasSameParameters(this MethodDefinition sourceMethod, MethodDefinition targetMethod)
        {
            return sourceMethod.Parameters.Count == targetMethod.Parameters.Count && 
                   sourceMethod.Parameters.All(sp => 
                       targetMethod.Parameters.SingleOrDefault(tp => 
                           tp.Name == sp.Name && tp.ParameterType.FullName == sp.ParameterType.FullName) != null);
        }

        public static bool HasSameFields(this TypeDefinition sourceType, TypeDefinition targetType)
        {
            return sourceType.Fields.Count == targetType.Fields.Count && 
                   sourceType.Fields.All(sp => 
                       targetType.Fields.SingleOrDefault(tp => 
                           tp.Name == sp.Name && tp.FieldType.FullName == sp.FieldType.FullName) != null);
        }
    }
}
