using System;
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
        // Readonly static fields
        private readonly HashSet<string> _allowedStaticFieldInitOnlyTypes = new HashSet<string>
        {
            typeof(Marshaller<>).FullName,
            typeof(Method<,>).FullName,
            typeof(MessageParser<>).FullName,
            typeof(FieldCodec<>).FullName,
            typeof(MapField<,>.Codec).FullName,
        };
        
        private readonly HashSet<string> _allowedStaticFieldTypes = new HashSet<string>
        {
            // Required for Linq in contracts (TODO: Discuss the risk of allowing Func as static public field)
            typeof(Func<>).FullName,
            typeof(Func<,>).FullName,
            typeof(Func<,,>).FullName,
        };
        
        private readonly HashSet<string> _allowedStateTypes = new HashSet<string>
        {
            typeof(MappedState<,>).FullName,
            typeof(MappedState<,,>).FullName,
            typeof(MappedState<,,,>).FullName,
            typeof(MappedState<,,,,>).FullName,
            typeof(MethodReference<,>).FullName,
        };

        public ContractStructureValidator()
        {
            // Convert full names to Mono.Cecil compatible full names
            foreach (var typeName in _allowedStaticFieldInitOnlyTypes.Where(typeName => typeName.Contains("+")).ToList())
            {
                _allowedStaticFieldInitOnlyTypes.Remove(typeName);
                _allowedStaticFieldInitOnlyTypes.Add(typeName.Replace("+", "/"));
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
            
            // Do some other checks here to restrict more
                
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
            // Only allow MappedState, ContractReferenceState or MethodReference fields
            var badFields = type.Fields.Where(IsBadStateField).ToList();
            
            if (badFields.Any())
            {
                return badFields.Select(f => 
                    new ContractStructureValidatorResult(
                            $"{f.FieldType.FullName} type is not allowed as a field in contract state.")
                        .WithInfo(f.Name, type.Namespace, type.Name, f.Name));
            }
            
            return Enumerable.Empty<ValidationResult>();
        }

        private IEnumerable<ValidationResult> ValidateContractType(TypeDefinition type)
        { 
            // Allow only constant or readonly primitive type fields in contract implementation
            return type.Fields
                .Where(f => 
                    !(f.Constant != null || (f.IsInitOnly && Constants.PrimitiveTypes.Contains(f.FieldType.FullName))))
                .Select(f => 
                    new ContractStructureValidatorResult("Non-constant or non-readonly fields are not allowed in contract implementation.")
                    .WithInfo(f.Name, type.Namespace, type.Name, f.Name));
        }

        private IEnumerable<ValidationResult> ValidateRegularType(TypeDefinition type)
        {
            // Skip if ExecutionObserver (validated in a separate validator)
            if (type.Name == typeof(ExecutionObserverProxy).Name)
                return Enumerable.Empty<ValidationResult>();

            var staticFields = type.Fields.Where(f => f.IsStatic);
            var badFields = staticFields.Where(IsBadField).ToList();

            if (badFields.Any())
            {
                return badFields.Select(f => 
                    new ContractStructureValidatorResult(
                        $"{f.FieldType.FullName} type is not allowed to be used as static field in regular types in contract.")
                    .WithInfo(f.Name, type.Namespace, type.Name, f.Name));
            }

            return Enumerable.Empty<ValidationResult>();
        }

        private bool IsBadField(FieldDefinition field)
        {
            var fieldTypeFullName = field.FieldType is GenericInstanceType genericInstance
                ? genericInstance.ElementType.FullName // TypeXyz<T> then element type full name will be TypeXyz`1
                : field.FieldType.FullName;

            // Only allowed types or primitive types if InitOnly field
            if (field.IsInitOnly 
                && (_allowedStaticFieldInitOnlyTypes.Contains(fieldTypeFullName) 
                    || Constants.PrimitiveTypes.Contains(fieldTypeFullName)))
            {
                return false;
            }
            
            // Required for Linq in contracts
            if (_allowedStaticFieldTypes.Contains(fieldTypeFullName))
            {
                return false;
            }

            // Only allow FileDescriptor field as non InitOnly field
            if (fieldTypeFullName == typeof(FileDescriptor).FullName)
                return false;
            
            // Allow field type that is type of its declaring type (required for protobuf generated code)
            if (fieldTypeFullName == field.DeclaringType.FullName)
                return false;
                
            // Then it should be a constant (and it has to be a primitive type if constant)
            return field.Constant == null;
        }

        private bool IsBadStateField(FieldDefinition field)
        {
            if (field.FieldType is GenericInstanceType genericInstanceType)
            {
                return !_allowedStateTypes.Contains(genericInstanceType.ElementType.FullName);
            }
            
            // If not ContractReferenceState then it is not allowed
            return field.FieldType.Resolve().BaseType.FullName != typeof(ContractReferenceState).FullName;
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