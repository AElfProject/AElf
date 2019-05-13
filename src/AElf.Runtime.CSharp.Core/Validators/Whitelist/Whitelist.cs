using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AElf.Runtime.CSharp.Validators.Whitelist
{
    public class Whitelist
    {
        private readonly IDictionary<string, Trust> _assemblies = new Dictionary<string, Trust>();
        private readonly IDictionary<string, NamespaceRule> _namespaces = new Dictionary<string, NamespaceRule>();

        public IReadOnlyDictionary<string, NamespaceRule> NameSpaces =>
            (IReadOnlyDictionary<string, NamespaceRule>) _namespaces;

        public Whitelist Assembly(Assembly assembly, Trust trustLevel)
        {
            _assemblies.Add(assembly.FullName, trustLevel);

            return this;
        }

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
            // Validate assembly references
            foreach (var asmRef in module.AssemblyReferences)
            {
                if (!_assemblies.Keys.Contains(asmRef.FullName))
                    results.Add(new WhitelistValidationResult("Assembly " + asmRef.FullName + " is not allowed."));
            }
            
            // Validate types in the module
            results.AddRange(module.Types.SelectMany(Validate));
            
            // Validate nested types
            results.AddRange(module.Types.SelectMany(t => t.NestedTypes).SelectMany(Validate));

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

            // If the type is a generic parameter, stop going deeper
            if (type.IsGenericParameter)
                return results;
            
            // If referred type is from a fully trusted assembly, stop going deeper
            if (_assemblies.Any(asm => asm.Key == type.Resolve()?.Module.Assembly.FullName && 
                                       asm.Value == Trust.Full))
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
                return results;
            }
            
            // Reached the most base type, now we can validate against the whitelist
            results.AddRange(ValidateAgainstWhitelist(method, type, member));

            return results;
        }

        private IEnumerable<ValidationResult> ValidateAgainstWhitelist(MethodDefinition method, TypeReference type, string member = null)
        {
            // Allow own defined types
            if (type is TypeDefinition)
            {
                yield break;
            }

            // Filter in the whitelist whether there is any rule
            var result = Search(type, member);

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

        private WhitelistSearchResult Search(TypeReference type, string member = null)
        {
            var typeNs = GetNameSpace(type);
            
            // Fail if there is no rule for the namespace
            if (!_namespaces.TryGetValue(typeNs, out var namespaceRule))
            {
                // If no exact match for namespace, check for wildcard matching
                if (_namespaces.Where(ns => ns.Value.Permission == Permission.Allowed 
                                            && !ns.Value.Types.Any()
                                            && ns.Key.EndsWith("*"))
                                .Any(ns => typeNs.StartsWith(ns.Key.Replace(".*", "")
                                                                   .Replace("*", ""))))
                    return WhitelistSearchResult.Allowed;
                
                return WhitelistSearchResult.DeniedNamespace;
            }
            
            // Fail if the type is not allowed in the namespace 
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
                if (!member.StartsWith("get_") && !member.StartsWith("set_"))
                    return typeRule.Permission == Permission.Allowed
                        ? WhitelistSearchResult.Allowed
                        : WhitelistSearchResult.DeniedMember;
    
                // Check without the prefix as well
                member = member.Split("_", 2)[1];
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
            return string.IsNullOrEmpty(type.Namespace) && type.DeclaringType != null ? GetNameSpace(type.DeclaringType) : type.Namespace;
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
