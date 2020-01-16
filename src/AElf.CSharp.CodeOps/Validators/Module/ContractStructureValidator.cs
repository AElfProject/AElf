using System.Collections.Generic;
using System.Linq;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp.State;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Google.Protobuf.Reflection;
using Mono.Cecil;

namespace AElf.CSharp.CodeOps.Validators.Module
{
    public class ContractStructureValidator : IValidator<ModuleDefinition>
    {
        private readonly HashSet<string> _allowedStaticFieldTypes = new HashSet<string>
        {
            typeof(Marshaller<>).FullName,
            typeof(Method<,>).FullName,
            typeof(MessageParser<>).FullName,
            typeof(FileDescriptor).FullName,
            typeof(FieldCodec<>).FullName,
            typeof(MapField<,>.Codec).FullName,
        };

        public ContractStructureValidator()
        {
            // Convert full names to Mono.Cecil compatible full names
            foreach (var typeName in _allowedStaticFieldTypes.Where(typeName => typeName.Contains("+")).ToList())
            {
                _allowedStaticFieldTypes.Remove(typeName);
                _allowedStaticFieldTypes.Add(typeName.Replace("+", "/"));
            }
        }

        public IEnumerable<ValidationResult> Validate(ModuleDefinition module)
        {
            var errors = new List<ValidationResult>();
            
            errors.AddRange(ValidateStructure(module));
            errors.AddRange(module.Types.SelectMany(ValidateType));
            errors.AddRange(module.Types.SelectMany(t => t.NestedTypes).SelectMany(ValidateType));

            return errors;
        }

        private IEnumerable<ValidationResult> ValidateStructure(ModuleDefinition module)
        {
            var contractImplementation = module.Types.Where(IsContractImplementation).Single();

            var contractBase = module.Types.SelectMany(t => t.NestedTypes.Where(IsContractImplementation)).Single();
            
            var contractState = contractBase.BaseType is GenericInstanceType genericType
                ? genericType.GenericArguments.Single()
                : null;
                
            return Enumerable.Empty<ValidationResult>();
        }

        private IEnumerable<ValidationResult> ValidateType(TypeDefinition type)
        {
            if (IsStateImplementation(type))
            {
                return ValidateContractStateType(type);
            }
            
            return IsContractImplementation(type) ? 
                ValidateContractType(type) : ValidateRegularType(type);
        }

        private IEnumerable<ValidationResult> ValidateContractStateType(TypeDefinition type)
        {
            // Only allow MappedState or ContractReferenceState fields
            
            return Enumerable.Empty<ValidationResult>();
        }

        private IEnumerable<ValidationResult> ValidateContractType(TypeDefinition type)
        {
            // Do not allow any non-constant field in contract implementation
            if (type.Fields.Any(f => f.Constant == null))
                return new List<ValidationResult>
                {
                    new ContractStructureValidatorResult("Non-constant fields are not allowed in contract implementation.")
                };
            
            return Enumerable.Empty<ValidationResult>();
        }

        private IEnumerable<ValidationResult> ValidateRegularType(TypeDefinition type)
        {
            var errors = new List<ValidationResult>();

            var staticFields = type.Fields.Where(f => f.IsStatic);

            var badFields = staticFields.Where(f =>
            {
                if (f.FieldType is GenericInstanceType genericInstance)
                {
                    // Should be one of the allowed types
                    return !_allowedStaticFieldTypes.Contains(genericInstance.ElementType.FullName);
                }

                // Then it should be a constant (and it has to be a primitive type if constant)
                return f.Constant == null; 
            }).ToList();

            if (badFields.Any())
            {
                return badFields.Select(f => new ContractStructureValidatorResult(
                        $"{f.FieldType.FullName} type is not allowed to be used as static field in regular types in contract."));
            }

            return Enumerable.Empty<ValidationResult>();
        }

        private GenericInstanceType FindGenericInstanceType(TypeDefinition type)
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

        private bool IsContractImplementation(TypeDefinition type)
        {
            var baseGenericInstanceType = FindGenericInstanceType(type);

            if (baseGenericInstanceType == null)
                return false;

            var elementType = baseGenericInstanceType.ElementType.Resolve();

            var baseType = GetBaseType(elementType);
            
            return baseType.Interfaces.Any(i => i.InterfaceType.FullName == typeof(ISmartContract).FullName);
        }

        private bool IsStateImplementation(TypeDefinition type)
        {
            return GetBaseType(type).FullName == typeof(StateBase).FullName;
        }

        private TypeDefinition GetBaseType(TypeDefinition type)
        {
            while (true)
            {
                if (type.BaseType == null || type.BaseType.FullName == typeof(object).FullName) return type;
                type = type.BaseType.Resolve();
            }
        }
    }
    
    public class ContractStructureValidatorResult : ValidationResult
    {
        public ContractStructureValidatorResult(string message) : base(message)
        {
        }
    }
}