using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Volo.Abp.DependencyInjection;

namespace AElf.CSharp.CodeOps.Validators.Whitelist;

public class WhitelistValidator : WhitelistValidatorBase, ITransientDependency
{
    public WhitelistValidator(IWhitelistProvider whitelistProvider) : base(whitelistProvider)
    {
    }

    public override bool SystemContactIgnored => true;
}
    
public class SystemContractWhitelistValidator : WhitelistValidatorBase, ITransientDependency
{
    public SystemContractWhitelistValidator(ISystemContractWhitelistProvider whitelistProvider) : base(whitelistProvider)
    {
    }

    public override bool SystemContactIgnored => false;
}
    
public abstract class WhitelistValidatorBase : IValidator<ModuleDefinition>
{
    private readonly IWhitelistProvider _whitelistProvider;

    public WhitelistValidatorBase(IWhitelistProvider whitelistProvider)
    {
        _whitelistProvider = whitelistProvider;
    }

    public abstract bool SystemContactIgnored { get; }

    public IEnumerable<ValidationResult> Validate(ModuleDefinition module, CancellationToken ct)
    {
        var whiteList = _whitelistProvider.GetWhitelist();
        var results = new List<ValidationResult>();
        // Validate assembly references
        foreach (var asmRef in module.AssemblyReferences)
        {
            if (!whiteList.ContainsAssemblyNameReference(asmRef))
                results.Add(new WhitelistValidationResult("Assembly " + asmRef.Name + " is not allowed."));
        }

        // Validate types in the module, at ANY nesting depth. Visiting only top-level types and
        // their immediate NestedTypes leaves depth-2+ types — hand-written, or compiler-generated
        // closures inside a nested helper — completely unscanned: a denied type referenced only
        // from such a type (a reflection dispatch primitive, for instance) was invisible here.
        results.AddRange(module.GetAllTypes().SelectMany(t => Validate(whiteList, t, ct)));

        return results;
    }

    private IEnumerable<ValidationResult> Validate(Whitelist whitelist, TypeDefinition type, CancellationToken ct)
    {
        var results = new List<ValidationResult>();

        foreach (var method in type.Methods)
        {
            if (ct.IsCancellationRequested)
                throw new ContractAuditTimeoutException();

            // Validate the method's own signature. The instruction-operand and local scans below
            // never see the declared signature, so a denied type used only as a parameter or
            // return type (e.g. a reflection handle smuggled between helper methods) was
            // invisible. Contract methods are synchronous protobuf-in/protobuf-out, so this adds
            // no false positives for legitimate contracts.
            results.AddRange(ValidateReference(whitelist, method, method.ReturnType));
            foreach (var parameter in method.Parameters)
                results.AddRange(ValidateReference(whitelist, method, parameter.ParameterType));

            if (!method.HasBody)
                continue;

            // Validate local variable types. The instruction-operand scan does not see local variable
            // declarations, so a denied type used only as a local (e.g. a reflection handle, or an enum
            // whose values fold to integer constants) would otherwise be invisible.
            if (method.Body.HasVariables)
            {
                foreach (var variable in method.Body.Variables)
                {
                    results.AddRange(ValidateReference(whitelist, method, variable.VariableType));
                }
            }

            foreach (var instruction in method.Body.Instructions)
            {
                results.AddRange(Validate(whitelist, method, instruction));
            }
        }

        return results;
    }

    private IEnumerable<ValidationResult> Validate(Whitelist whitelist, MethodDefinition method,
        Instruction instruction)
    {
        if (!(instruction.Operand is MemberReference reference))
            return Enumerable.Empty<ValidationResult>();

        if (reference is MethodReference methodReference)
        {
            var results = new List<ValidationResult>();
            results.AddRange(ValidateReference(whitelist, method, methodReference.DeclaringType,
                methodReference.Name));
            results.AddRange(ValidateReference(whitelist, method, methodReference.ReturnType));
            // Validate parameter types too. Previously only the declaring type, method name and return
            // type were checked, so a denied type used only as a parameter (e.g. BindingFlags on
            // Type.InvokeMember) slipped through. ValidateReference short-circuits for fully-trusted and
            // generic-parameter types, so this only tightens the untrusted surface.
            //
            // Exception: compiler-generated delegate constructors take (object, IntPtr); every lambda
            // in every contract references one, so scanning its parameters flags IntPtr in virtually
            // all existing contracts. This opens no hole: a raw function pointer only enters via
            // ldftn/ldvirtftn, whose method operand is itself validated above, and IntPtr remains
            // denied as a local/return/parameter type everywhere else.
            if (!IsCompilerGeneratedDelegateConstructor(methodReference))
                foreach (var parameter in methodReference.Parameters)
                    results.AddRange(ValidateReference(whitelist, method, parameter.ParameterType));
            // Validate generic arguments of generic method instances.
            if (methodReference is GenericInstanceMethod genericInstanceMethod)
                foreach (var argument in genericInstanceMethod.GenericArguments)
                    results.AddRange(ValidateReference(whitelist, method, argument));
            return results;
        }

        if (reference is FieldReference fieldReference)
        {
            var results = new List<ValidationResult>();
            results.AddRange(
                ValidateReference(whitelist, method, fieldReference.DeclaringType, fieldReference.Name));
            results.AddRange(ValidateReference(whitelist, method, fieldReference.FieldType));
            return results;
        }

        if (reference is TypeReference typeReference)
        {
            return ValidateReference(whitelist, method, typeReference);
        }

        return Enumerable.Empty<ValidationResult>();
    }

    private static bool IsCompilerGeneratedDelegateConstructor(MethodReference methodReference)
    {
        return methodReference.Name == ".ctor"
               && methodReference.Parameters.Count == 2
               && methodReference.Parameters[0].ParameterType.FullName == "System.Object"
               && methodReference.Parameters[1].ParameterType.FullName == "System.IntPtr";
    }

    private IEnumerable<ValidationResult> ValidateReference(Whitelist whitelist, MethodDefinition method,
        TypeReference type,
        string member = null)
    {
        var results = new List<ValidationResult>();

        // If the type is a generic parameter, stop going deeper
        if (type.IsGenericParameter)
            return results;

        // Unwrap container shapes BEFORE applying assembly trust. A fully-trusted generic
        // container (e.g. Google.Protobuf RepeatedField<T> or any System.Collections type) must
        // not launder its type arguments: RepeatedField<Assembly> hid the denied Assembly
        // argument when the trust check ran on the unresolved container first. Trust is applied
        // to leaf/element types only, and generic arguments are always validated.
        if (type.IsByReference)
        {
            results.AddRange(ValidateReference(whitelist, method, type.GetElementType()));
            return results;
        }

        if (type is GenericInstanceType generic)
        {
            results.AddRange(ValidateReference(whitelist, method, generic.ElementType));

            foreach (var argument in generic.GenericArguments)
            {
                results.AddRange(ValidateReference(whitelist, method, argument));
            }

            return results;
        }

        // If the type is an array, then validate the element type of the array
        if (type.IsArray)
        {
            results.AddRange(ValidateReference(whitelist, method, type.GetElementType()));
            return results;
        }

        // If referred type is from a fully trusted assembly, stop going deeper
        if (whitelist.CheckAssemblyFullyTrusted(type.Resolve()?.Module.Assembly.Name))
            return results;

        // Reached the most base type, now we can validate against the whitelist
        results.AddRange(ValidateAgainstWhitelist(whitelist, method, type, member));

        return results;
    }

    private IEnumerable<ValidationResult> ValidateAgainstWhitelist(Whitelist whitelist, MethodDefinition method,
        TypeReference type, string member = null)
    {
        // Allow own defined types
        if (type is TypeDefinition)
        {
            yield break;
        }

        // Filter in the whitelist whether there is any rule
        var result = Search(whitelist, type, member);

        // Return a validation result if search result is negative (any of the denied results)
        switch (result)
        {
            case WhitelistSearchResult.DeniedNamespace:
                var ns = string.IsNullOrWhiteSpace(type.Namespace) ? @"""" : type.Namespace;
                yield return new WhitelistValidationResult($"{ns} is not allowed.")
                    .WithInfo(method.Name, type.Namespace, type.Name, member);
                break;

            case WhitelistSearchResult.DeniedType:
                yield return new WhitelistValidationResult($"{type.Name} in {type.Namespace} is not allowed.")
                    .WithInfo(method.Name, type.Namespace, type.Name, member);
                break;

            case WhitelistSearchResult.DeniedMember:
                yield return new WhitelistValidationResult($"{member} in {type.FullName} is not allowed.")
                    .WithInfo(method.Name, type.Namespace, type.Name, member);
                break;
        }
    }

    private WhitelistSearchResult Search(Whitelist whitelist, TypeReference type, string member = null)
    {
        var typeNs = GetNameSpace(type);

        // Fail if there is no rule for the namespace
        if (!whitelist.TryGetNamespaceRule(typeNs, out var namespaceRule))
        {
            // If no exact match for namespace, check for wildcard matching
            if (whitelist.ContainsWildcardMatchedNamespaceRule(typeNs))
                return WhitelistSearchResult.Allowed;

            return WhitelistSearchResult.DeniedNamespace;
        }

        // Fail if the type is not allowed in the namespace 
        if (!namespaceRule.Types.TryGetValue(type.Name, out var typeRule) ||
            typeRule.Permission == Permission.Denied && !typeRule.Members.Any())
        {
            return namespaceRule.Permission == Permission.Allowed
                ? WhitelistSearchResult.Allowed
                : WhitelistSearchResult.DeniedType;
        }

        if (typeRule.Permission == Permission.Denied && !typeRule.Members.Any())
            return WhitelistSearchResult.DeniedType;

        if (member == null)
            return WhitelistSearchResult.Allowed;

        if (!typeRule.Members.TryGetValue(member, out var memberRule))
        {
            if (!member.StartsWith("get_") && !member.StartsWith("set_"))
                return typeRule.Permission == Permission.Allowed
                    ? WhitelistSearchResult.Allowed
                    : WhitelistSearchResult.DeniedMember;

            // Check without the prefix as well
            member = member.Split(new[] {'_'}, 2)[1];
            if (!typeRule.Members.TryGetValue(member, out memberRule))
            {
                return typeRule.Permission == Permission.Allowed
                    ? WhitelistSearchResult.Allowed
                    : WhitelistSearchResult.DeniedMember;
            }
        }

        return memberRule.Permission == Permission.Allowed
            ? WhitelistSearchResult.Allowed
            : WhitelistSearchResult.DeniedMember;
    }

    private string GetNameSpace(TypeReference type)
    {
        // Below is needed for nested types that are declared in other types, otherwise namespace is null
        return string.IsNullOrEmpty(type.Namespace) && type.DeclaringType != null
            ? GetNameSpace(type.DeclaringType)
            : type.Namespace;
    }
}

internal enum WhitelistSearchResult
{
    Allowed,
    DeniedNamespace,
    DeniedType,
    DeniedMember
}

public class WhitelistValidationResult : ValidationResult
{
    public WhitelistValidationResult(string message) : base(message)
    {
    }
}