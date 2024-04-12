using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using AElf.Sdk.CSharp.State;
using Google.Protobuf.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace AElf.CSharp.CodeOps;

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
            operandStr = field.Name;
        else if (instruction.Operand is Instruction ins) // Is probably branching to another instruction
            operandStr = ins.OpCode.ToString() + ins.Operand; // May restrict to branching to ret only
        else
            operandStr = instruction.Operand?.ToString();

        return instruction.OpCode + operandStr;
    }

    public static IEnumerable<FieldDefinition> GetResetableStaticFields(this TypeDefinition type)
    {
        // Get static fields from type
        var fields = type.Fields.Where(f =>
            f.IsPublic &&
            f.IsStatic &&
            !(f.IsInitOnly || f.HasConstant)
        ).ToList();

        // Get static fields from nested types 
        fields.AddRange(type.NestedTypes.SelectMany(GetResetableStaticFields));

        return fields;
    }

    public static bool IsDisallowedStaticField(this FieldDefinition field)
    {
        var isStatic = field.IsStatic;
        var isNotReadonly = !field.IsInitOnly;
        var isNotConstant = !field.HasConstant;
        var isNotInProtobufGeneratedType = field.FieldType.FullName != typeof(FileDescriptor).FullName &&
                                           field.FieldType.FullName != typeof(MessageDescriptor).FullName;
        var isNotFunctionFieldInGeneratedClass = !field.IsFuncTypeFieldInGeneratedClass();
        return new[]
        {
            isStatic, isNotReadonly, isNotConstant, isNotInProtobufGeneratedType, isNotFunctionFieldInGeneratedClass
        }.All(x => x);
    }

    /// <summary>
    /// <c>Func</c> type static fields are generated for LINQ operations. We should allow them and not reset them.
    /// </summary>
    /// <param name="field"></param>
    /// <returns></returns>
    public static bool IsFuncTypeFieldInGeneratedClass(this FieldDefinition field)
    {
        if (field.DeclaringType.Name != "<>c")
        {
            return false;
        }

        if (field.FieldType is GenericInstanceType genericInstance)
        {
            return Constants.FuncTypes.Contains(genericInstance.ElementType.FullName);
        }

        return false;
    }

    public static IEnumerable<FieldDefinition> GetAllFields(this TypeDefinition type,
        Func<FieldDefinition, bool> condition)
    {
        var fields = type.Fields.Where(condition).ToList();

        if (type.BaseType is TypeDefinition baseType) fields.AddRange(baseType.GetAllFields(condition));

        return fields;
    }

    private static GenericInstanceType FindGenericInstanceType(TypeDefinition type)
    {
        var maxInheritance = Constants.MaxInheritanceThreshold;
        while (true)
        {
            if (maxInheritance-- == 0)
                throw new MaxInheritanceExceededException();

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

    public static bool IsStateImplementation(this TypeDefinition type)
    {
        return GetBaseType(type).FullName == typeof(StateBase).FullName;
    }

    private static TypeDefinition GetBaseType(this TypeDefinition type)
    {
        var maxInheritance = Constants.MaxInheritanceThreshold;
        while (true)
        {
            if (maxInheritance-- == 0)
                throw new MaxInheritanceExceededException();

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

    public static Type FindExecutionObserverProxyType(this Assembly assembly)
    {
        return assembly.GetTypes().SingleOrDefault(t => t.Name == nameof(ExecutionObserverProxy));
    }

    // Borrowed from https://github.com/jbevain/cecil/blob/7b8ee049a151204997eecf587c69acc2f67c8405/Mono.Cecil/MemberReference.cs#L82-L88
    public static string MemberFullName(this MemberReference self)
    {
        if (self.DeclaringType == null)
            return self.Name;

        return self.DeclaringType.FullName + "::" + self.Name;
    }

    // Borrowed from https://github.com/jbevain/cecil/blob/7b8ee049a151204997eecf587c69acc2f67c8405/Mono.Cecil/MethodReference.cs#L105-L114
    public static string FullNameWithoutDeclaringType(this MethodReference self)
    {
        var builder = new StringBuilder();
        builder.Append(self.ReturnType.FullName)
            .Append(" ")
            .Append(self.Name);
        self.MethodSignatureFullName(builder);
        return builder.ToString();
    }

    // Borrowed from https://github.com/jbevain/cecil/blob/7b8ee049a151204997eecf587c69acc2f67c8405/Mono.Cecil/IMethodSignature.cs#L36-L55
    public static void MethodSignatureFullName(this IMethodSignature self, StringBuilder builder)
    {
        builder.Append("(");

        if (self.HasParameters)
        {
            var parameters = self.Parameters;
            for (int i = 0; i < parameters.Count; i++)
            {
                var parameter = parameters[i];
                if (i > 0)
                    builder.Append(",");

                if (parameter.ParameterType.IsSentinel)
                    builder.Append("...,");

                builder.Append(parameter.ParameterType.FullName);
            }
        }

        builder.Append(")");
    }
}