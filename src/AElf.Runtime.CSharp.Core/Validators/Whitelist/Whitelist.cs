using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AElf.Runtime.CSharp.Validators.Whitelist
{
    public class Whitelist
    {
        private readonly IDictionary<string, NamespaceRule> _namespaces = new Dictionary<string, NamespaceRule>();

        public IReadOnlyDictionary<string, NamespaceRule> NameSpaces =>
            (IReadOnlyDictionary<string, NamespaceRule>) _namespaces;

        public Whitelist Namespace(string name, Permission permission,
            Action<NamespaceRule> namespaceRules = null)
        {
            var rule = new NamespaceRule(name, permission);

            _namespaces[name] = rule;

            namespaceRules?.Invoke(rule);

            return this;
        }

        public IEnumerable<ValidationResult> Validate(ModuleDefinition module)
        {
            var results = new List<ValidationResult>();

            foreach (var typeDef in module.Types)
            {
                results.AddRange(Validate(typeDef));
            }

            return results;
        }

        private IEnumerable<ValidationResult> Validate(TypeDefinition type)
        {
            var results = new List<ValidationResult>();
            
            foreach (var method in type.Methods)
            {
                if (!method.HasBody)
                    continue;

                foreach (var instruction in method.Body.Instructions)
                {
                    results.AddRange(Validate(method, instruction));
                }
            }

            return results;
        }

        private IEnumerable<ValidationResult> Validate(MethodDefinition method, Instruction instruction)
        {
            if (!(instruction.Operand is MemberReference reference))
                return Enumerable.Empty<ValidationResult>();

            if (reference is MethodReference methodReference)
            {
                var results = new List<ValidationResult>();
                results.AddRange(ValidateReference(method, methodReference.DeclaringType, methodReference.Name));
                results.AddRange(ValidateReference(method, methodReference.ReturnType));
                return results;
            }

            if (reference is FieldReference fieldReference)
            {
                var results = new List<ValidationResult>();
                results.AddRange(ValidateReference(method, fieldReference.DeclaringType, fieldReference.Name));
                results.AddRange(ValidateReference(method, fieldReference.FieldType));
                return results;
            }

            if (reference is TypeReference typeReference)
            {
                return ValidateReference(method, typeReference);
            }

            return Enumerable.Empty<ValidationResult>();
        }

        private IEnumerable<ValidationResult> ValidateReference(MethodDefinition method, TypeReference type,
            string member = null)
        {
            var results = new List<ValidationResult>();

            if (type.IsGenericParameter)
                return results;

            // Dig deeper by calling ValidateReference until reaching base type
            if (type.IsByReference)
            {
                results.AddRange(ValidateReference(method, type.GetElementType()));
                return results;
            }

            if (type is GenericInstanceType generic)
            {
                results.AddRange(ValidateReference(method, generic.ElementType));

                foreach (var argument in generic.GenericArguments)
                {
                    results.AddRange(ValidateReference(method, argument));
                }

                return results;
            }

            // If the type is an array, then validate the element type of the array
            if (type.IsArray)
            {
                results.AddRange(ValidateReference(method, type.GetElementType()));
            }
            
            // Reached the most base type, now we can validate against the whitelist
            results.AddRange(ValidateWhitelist(method, type, member));

            return results;
        }

        private IEnumerable<ValidationResult> ValidateWhitelist(MethodDefinition method, TypeReference type, string member = null)
        {
            // Allow own defined types
            if (type is TypeDefinition)
            {
                yield break;
            }

            // Filter in the whitelist whether there is any rule
            var result = Search(type, member);

            // Return a validation result if search result is negative
            switch (result)
            {
                case WhitelistSearchResult.DeniedNamespace:
                    var ns = string.IsNullOrWhiteSpace(type.Namespace) ? @"""" : type.Namespace;
                    yield return new WhitelistValidationResult($"{ns} is not allowed.");
                    break;
                    
                case WhitelistSearchResult.DeniedType:
                    yield return new WhitelistValidationResult($"{type.Name} in {type.Namespace} is not allowed.");
                    break;
                
                
                case WhitelistSearchResult.DeniedMember:
                    yield return new WhitelistValidationResult($"{member} in {type.FullName} is not allowed.");
                    break;
            }
        }

        private WhitelistSearchResult Search(TypeReference type, string member = null)
        {
            var error = Enumerable.Empty<WhitelistValidationResult>();
            
            // Fail if there is no rule for the namespace
            if (!_namespaces.TryGetValue(type.Namespace, out var namespaceRule))
            {
                if (_namespaces.Where(ns => ns.Value.Permission == Permission.Allowed && !ns.Value.Types.Any())
                                .Any(ns => type.Namespace.StartsWith(ns.Key)))
                    return WhitelistSearchResult.Allowed;
                
                return WhitelistSearchResult.DeniedNamespace;
            }
            
            // Fail if the type is not defined in the namespace 
            if (!namespaceRule.Types.TryGetValue(type.Name, out var typeRule) || 
                (typeRule.Permission == Permission.Denied && !typeRule.Members.Any()))
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
                return typeRule.Permission == Permission.Allowed
                    ? WhitelistSearchResult.Allowed
                    : WhitelistSearchResult.DeniedMember;
            }

            return memberRule.Permission == Permission.Allowed
                ? WhitelistSearchResult.Allowed
                : WhitelistSearchResult.DeniedMember;
        }
    }
    
    internal enum WhitelistSearchResult
    {
        Allowed,
        DeniedNamespace,
        DeniedType,
        DeniedMember,
    }
    
    public class WhitelistValidationResult : ValidationResult
    {
        public WhitelistValidationResult(string message) : base(message)
        {
        }
    }
}
