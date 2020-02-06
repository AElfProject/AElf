using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AElf.Sdk.CSharp;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace AElf.CSharp.CodeOps
{
    public static class Extensions
    {
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

        public static IEnumerable<FieldDefinition> GetStaticFields(this TypeDefinition type)
        {
            // Get static fields from type
            var fields = type.Fields.Where(f => 
                f.IsPublic && 
                f.IsStatic && 
                !(f.IsInitOnly || f.HasConstant)
                ).ToList();

            // Get static fields from nested types 
            fields.AddRange(type.NestedTypes.SelectMany(GetStaticFields));

            return fields;
        }

        public static IEnumerable<FieldDefinition> GetContractFields(this ModuleDefinition module)
        {
            // Get contract implementations
            var contractImplementation = module.Types.Where(IsContractImplementation).Single();

            // Get fields in contract implementation
            return contractImplementation.GetFields();
        }

        private static IEnumerable<FieldDefinition> GetFields(this TypeDefinition type)
        {
            var fields = type.Fields.Where(f => !f.HasConstant).ToList();

            if (type.BaseType is TypeDefinition baseType)
            {
                fields.AddRange(baseType.GetFields());
            }

            return fields;
        }

        private static GenericInstanceType FindGenericInstanceType(TypeDefinition type)
        {
            while (true)
            {
                switch (type.BaseType)
                {
                    case null:
                        return null;
                    case GenericInstanceType genericInstanceType:
                        return genericInstanceType;
                    default:
                        type = type.BaseType.Resolve();
                        continue;
                }
            }
        }

        public static bool IsContractImplementation(this TypeDefinition type)
        {
            var baseGenericInstanceType = FindGenericInstanceType(type);

            if (baseGenericInstanceType == null)
                return false;

            var elementType = baseGenericInstanceType.ElementType.Resolve();

            var baseType = GetBaseType(elementType);
            
            return baseType.Interfaces.Any(i => i.InterfaceType.FullName == typeof(ISmartContract).FullName);
        }

        private static TypeDefinition GetBaseType(this TypeDefinition type)
        {
            while (true)
            {
                if (type.BaseType == null || type.BaseType.FullName == typeof(object).FullName) return type;
                type = type.BaseType.Resolve();
            }
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
