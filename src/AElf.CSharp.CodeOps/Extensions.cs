using System;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace AElf.CSharp.CodeOps
{
    public static class Extensions
    {
        public static TypeReference GetInstanceTypeInStack(this Instruction instruction, MethodDefinition methodDef)
        {
            if (instruction.OpCode == OpCodes.Call)
            {
                // If it is a call instruction, it is a static method call,
                // not an instance method call, so we don't care about the instance type
                return null;
            }

            // Try to static identify the first element in stack during runtime
            var previousInstruction = instruction.Previous;
            var methodVariables = methodDef.Body.Variables;
            var methodParameters = methodDef.Parameters;
            switch (previousInstruction.OpCode.Code.ToString())
            {
                // From input parameters (arguments)
                case nameof(OpCodes.Ldarg_0): // this (loads itself to stack)
                    return methodDef.DeclaringType;
                case nameof(OpCodes.Ldarg_1):
                    return methodParameters[0].ParameterType;
                case nameof(OpCodes.Ldarg_2):
                    return methodParameters[1].ParameterType;
                case nameof(OpCodes.Ldarg_3):
                    return methodParameters[2].ParameterType;
                case nameof(OpCodes.Ldarg):
                    return methodParameters[(short) previousInstruction.Operand - 1].ParameterType;
                
                // From method variables
                case nameof(OpCodes.Ldloc_0):
                    return methodVariables[0].VariableType;
                case nameof(OpCodes.Ldloc_1):
                    return methodVariables[1].VariableType;
                case nameof(OpCodes.Ldloc_2):
                    return methodVariables[2].VariableType;
                case nameof(OpCodes.Ldloc_3):
                    return methodVariables[3].VariableType;
                case nameof(OpCodes.Ldloc):
                    return methodVariables[(short) previousInstruction.Operand].VariableType;
                
                // From field
                case nameof(OpCodes.Ldfld):
                    return ((FieldReference) previousInstruction.Operand).FieldType;

                // From method call
                case nameof(OpCodes.Call):
                case nameof(OpCodes.Callvirt):
                    return ((MethodReference) previousInstruction.Operand).ReturnType;
                
                default:
                    throw new InvalidCodeException("Cannot identify instance type for instance method call.");
            }
        }

        public static bool HasSameBody(this MethodDefinition sourceMethod, MethodDefinition targetMethod)
        {
            // Exclude nop opcodes (compile in debug mode adds nop to be able to place breakpoint, ignore those)
            var sourceMethodBodyInstructions = sourceMethod.Body.Instructions
                .Where(i => i.OpCode != OpCodes.Nop).ToArray();
            var targetMethodBodyInstructions = targetMethod.Body.Instructions
                .Where(i => i.OpCode != OpCodes.Nop).ToArray();

            // Compare method body
            return !sourceMethodBodyInstructions.Where((t, i) => 
                t.ToComparableString() != targetMethodBodyInstructions[i].ToComparableString()).Any();
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
            // Don't mind if injected type method has more parameters since we check the body to be the same
            return sourceMethod.Parameters.Count == targetMethod.Parameters.Count && 
                   sourceMethod.Parameters.All(sp => 
                       targetMethod.Parameters.SingleOrDefault(tp => 
                           tp.Name == sp.Name && tp.ParameterType.FullName == sp.ParameterType.FullName) != null);
        }

        public static bool HasSameFields(this TypeDefinition sourceType, TypeDefinition targetType)
        {
            // Don't mind if injected type has more fields since we check each of the methods' bodies
            return sourceType.Fields.Count == targetType.Fields.Count && 
                   sourceType.Fields.All(sp => 
                       targetType.Fields.SingleOrDefault(tp => 
                           tp.Name == sp.Name && tp.FieldType.FullName == sp.FieldType.FullName) != null);
        }

        public static void RemoveCoverLetInjectedInstructions(this MethodDefinition method)
        {
            if (!method.IsMethodCoverletInjected())
                return;

            var methodInstructions = method.Body.Instructions.ToList();
            var il = method.Body.GetILProcessor();
            il.Body.SimplifyMacros();

            // Update branching instructions if they are pointing to coverlet injected code
            foreach (var instruction in methodInstructions)
            {
                // Skip if not a branching instruction
                if (!Constants.JumpingOpCodes.Contains(instruction.OpCode)) continue;
                
                var targetInstruction = (Instruction) instruction.Operand;

                if (targetInstruction.Next == null) continue; // Probably end of method body
                
                if (targetInstruction.Next.IsCoverletInjectedInstruction())
                {
                    // Point to next
                    il.Replace(instruction, 
                        il.Create(instruction.OpCode, GetNextNonCoverletInstruction(targetInstruction.Next)));
                }
            }

            foreach (var instruction in methodInstructions
                .Where(instruction => instruction.IsCoverletInjectedInstruction()))
            {
                // Remove coverlet injected instructions
                il.Remove(instruction.Previous);
                il.Remove(instruction);
            }
            
            il.Body.OptimizeMacros();
        }
        
        private static void PrintBody(this MethodDefinition method)
        {
            foreach (var instruction in method.Body.Instructions)
            {
                Console.WriteLine($"{instruction.OpCode.ToString()} {instruction.Operand}");
            }
        }

        private static Instruction GetNextNonCoverletInstruction(Instruction instruction)
        {
            // check whether we are at the end of the method body first
            if (instruction.Next == null) return instruction;
            
            // Sometimes coverlet is injecting twice, then next will be its counter value
            var nextLine = instruction.Next;

            // and next after that will be a coverlet call, then skip those 2 as well;
            // if not, just return next line
            return nextLine.Next?.IsCoverletInjectedInstruction() ?? false ? 
                GetNextNonCoverletInstruction(nextLine.Next) 
                : nextLine; // Otherwise, just return next line
        }

        private static bool IsMethodCoverletInjected(this MethodDefinition method)
        {
            return method.Body.Instructions.Any(i => i.IsCoverletInjectedInstruction());
        }

        private static bool IsCoverletInjectedInstruction(this Instruction instruction)
        {
            return instruction.OpCode == OpCodes.Call &&
                   instruction.Operand.ToString().Contains("Coverlet.Core.Instrumentation.Tracker");
        }

        public static Type FindContractType(this Assembly assembly)
        {
            var types = assembly.GetTypes();
            return types.SingleOrDefault(t => typeof(ISmartContract).IsAssignableFrom(t) && !t.IsNested);
        }
        
        public static Type FindContractBaseType(this Assembly assembly)
        {
            var types = assembly.GetTypes();
            return types.SingleOrDefault(t => typeof(ISmartContract).IsAssignableFrom(t) && t.IsNested);
        }

        public static Type FindContractContainer(this Assembly assembly)
        {
            var contractBase = FindContractBaseType(assembly);
            return contractBase.DeclaringType;
        }
        
        public static Type FindExecutionObserverType(this Assembly assembly)
        {
            return assembly.GetTypes().SingleOrDefault(t => t.Name == nameof(ExecutionObserverProxy));
        }
    }
}
