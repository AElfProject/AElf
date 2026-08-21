using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Volo.Abp.DependencyInjection;

namespace AElf.CSharp.CodeOps.Validators.Method;

/// <summary>
/// Denies the dynamic dispatch / dynamic code-loading API surface.
///
/// The whitelist validator only inspects statically-typed member references and cannot see through
/// reflection: a contract that calls <c>Type.GetType("System.Reflection.Assembly")</c> followed by
/// <c>Type.InvokeMember("Load", ...)</c> reaches <c>Assembly.Load(byte[])</c> without ever emitting a
/// static reference to a forbidden type (the type and member names are plain string literals). The
/// second-stage assembly loaded this way is never audited and runs with full trust inside the node
/// process, which is enough to walk the host object graph by reflection and exfiltrate the node's
/// signing key.
///
/// This validator closes that hole by rejecting the reflection-invocation, activation and
/// assembly-loading methods by (declaring type, method name), independent of the whitelist. It is
/// intentionally narrow: it targets only the methods that enable dynamic dispatch or dynamic code
/// loading, so legitimate contract code (including <c>typeof(...)</c>, which lowers to
/// <c>Type.GetTypeFromHandle</c>) is unaffected.
/// </summary>
public class ReflectionValidator : IValidator<MethodDefinition>, ITransientDependency
{
    public bool SystemContactIgnored => false;

    // Methods on System.Type that perform dynamic member lookup or invocation.
    // Note: typeof(x) => Type.GetTypeFromHandle, and obj.GetType() => Object.GetType, neither of which
    // is listed here, so both remain allowed.
    private static readonly HashSet<string> TypeReflectionMethods = new()
    {
        "GetType", // the static Type.GetType(string) overloads (instance Object.GetType has a different declaring type)
        "InvokeMember",
        "GetMethod", "GetMethods",
        "GetField", "GetFields",
        "GetProperty", "GetProperties",
        "GetConstructor", "GetConstructors",
        "GetMember", "GetMembers",
        "GetEvent", "GetEvents",
        "GetNestedType", "GetNestedTypes",
        "GetInterface", "GetInterfaceMap",
        "MakeGenericType", "MakeArrayType", "MakePointerType", "MakeByRefType",
        "GetTypeFromProgID", "GetTypeFromCLSID"
    };

    // Fully-qualified declaring types whose *every* method is a dynamic dispatch / load / marshal /
    // codegen primitive. Any call into these is rejected.
    private static readonly HashSet<string> BannedDeclaringTypes = new()
    {
        "System.Activator",
        "System.AppDomain",
        "System.Reflection.Assembly",
        "System.Reflection.MethodBase",
        "System.Reflection.MethodInfo",
        "System.Reflection.ConstructorInfo",
        "System.Reflection.FieldInfo",
        "System.Reflection.PropertyInfo",
        "System.Reflection.EventInfo",
        "System.Reflection.MemberInfo",
        "System.Reflection.Module",
        // IReflect is the reflection-dispatch interface System.Type implements: a cast
        // ((IReflect)typeof(X)).InvokeMember(...) reaches the same dynamic dispatch as
        // Type.InvokeMember while declaring the call on a type this validator did not match.
        "System.Reflection.IReflect",
        // Custom binders steer overload resolution for dynamic invocation.
        "System.Reflection.Binder",
        // The COM-facing interface System.Type also implements; it exposes InvokeMember too.
        "System.Runtime.InteropServices._Type",
        "System.Reflection.Emit.ILGenerator",
        "System.Reflection.Emit.MethodBuilder",
        "System.Reflection.Emit.TypeBuilder",
        "System.Reflection.Emit.AssemblyBuilder",
        "System.Reflection.Emit.DynamicMethod",
        "System.Runtime.Loader.AssemblyLoadContext",
        "System.Runtime.InteropServices.Marshal",
        "System.Runtime.InteropServices.NativeLibrary",
        "System.Runtime.CompilerServices.RuntimeHelpers" // GetUninitializedObject etc. (InitializeArray is on this
                                                          // type but is whitelisted elsewhere and handled by
                                                          // ArrayValidator; see MethodIsBanned).
    };

    // Specific (declaringType, method) pairs to ban without banning the whole declaring type.
    private static readonly HashSet<string> BannedMethods = new()
    {
        "System.Delegate::DynamicInvoke",
        "System.Delegate::CreateDelegate" // dynamic delegate construction = dynamic dispatch
    };

    // Expression-tree compilation is runtime code generation. Compile()/CompileToMethod() may be
    // declared on LambdaExpression or on the generic Expression`1<TDelegate>, so match by namespace.
    private static bool IsExpressionCompile(MethodReference called)
    {
        return (called.Name == "Compile" || called.Name == "CompileToMethod")
               && (called.DeclaringType?.FullName?.StartsWith("System.Linq.Expressions.") ?? false);
    }

    // RuntimeHelpers members that ARE allowed (used by hardcoded array initialization).
    private static readonly HashSet<string> AllowedRuntimeHelpersMembers = new()
    {
        "InitializeArray"
    };

    public IEnumerable<ValidationResult> Validate(MethodDefinition method, CancellationToken ct)
    {
        if (ct.IsCancellationRequested)
            throw new ContractAuditTimeoutException();

        if (!method.HasBody)
            return Enumerable.Empty<ValidationResult>();

        var errors = new List<ValidationResult>();

        foreach (var instruction in method.Body.Instructions)
        {
            if (!(instruction.Operand is MethodReference called))
                continue;

            if (!MethodIsBanned(called))
                continue;

            errors.Add(new ReflectionValidationResult(
                    $"Usage of reflection/dynamic code API is not allowed: {called.DeclaringType?.FullName}.{called.Name}")
                .WithInfo(method.Name, method.DeclaringType.Namespace, method.DeclaringType.Name, called.Name));
        }

        return errors;
    }

    private static bool MethodIsBanned(MethodReference called)
    {
        var declaringType = called.DeclaringType;
        if (declaringType == null)
            return false;

        var declaringFullName = declaringType.FullName;

        // RuntimeHelpers: allow only the explicitly-permitted members (e.g. InitializeArray), ban the rest.
        if (declaringFullName == "System.Runtime.CompilerServices.RuntimeHelpers")
            return !AllowedRuntimeHelpersMembers.Contains(called.Name);

        if (BannedDeclaringTypes.Contains(declaringFullName))
            return true;

        if (declaringFullName == "System.Type" && TypeReflectionMethods.Contains(called.Name))
            return true;

        if (BannedMethods.Contains($"{declaringFullName}::{called.Name}"))
            return true;

        if (IsExpressionCompile(called))
            return true;

        return false;
    }
}

public class ReflectionValidationResult : ValidationResult
{
    public ReflectionValidationResult(string message) : base(message)
    {
    }
}
