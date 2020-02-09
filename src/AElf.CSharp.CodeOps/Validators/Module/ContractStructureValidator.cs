using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
            var errors = new List<ValidationResult>();
            
            if (IsStateImplementation(type))
            {
                return ValidateContractStateType(type);
            }
            
            errors.AddRange(IsContractImplementation(type) ? ValidateContractType(type) : ValidateRegularType(type));
            errors.AddRange(type.NestedTypes.SelectMany(ValidateType));

            return errors;
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
            // Allow readonly only if the type is primitive type
            // Allow constants
            // Allow any other types if not readonly or constant (since ResetFields can reset later on)
            return type.Fields
                .Where(f => 
                    !((f.Constant != null || f.IsInitOnly) && 
                      (Constants.PrimitiveTypes.Contains(FieldTypeFullName(f)) || 
                       _allowedStaticFieldInitOnlyTypes.Contains(FieldTypeFullName(f))))) // Treat contract field as static field
                .Where(f => f.Constant != null || f.IsInitOnly)
                .Select(f => 
                    new ContractStructureValidatorResult("Only primitive types are allowed in readonly fields in contract implementation.")
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
            var fieldTypeFullName = FieldTypeFullName(field);

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

        private string FieldTypeFullName(FieldDefinition field)
        {
            return field.FieldType is GenericInstanceType genericInstance
                ? genericInstance.ElementType.FullName // TypeXyz<T> then element type full name will be TypeXyz`1
                : field.FieldType.FullName;
        }

        private bool IsBadStateField(FieldDefinition field)
        {
            if (field.FieldType is GenericInstanceType genericInstanceType)
            {
                return !_allowedStateTypes.Contains(genericInstanceType.ElementType.FullName);
            }

            if (_allowedStateTypes.Contains(field.FieldType.FullName))
                return false;

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
        
        private readonly HashSet<string> _allowedStaticFieldInitOnlyTypes = new HashSet<string>
        {
            typeof(Marshaller<>).FullName,
            typeof(Method<,>).FullName,
            typeof(MessageParser<>).FullName,
            typeof(FieldCodec<>).FullName,
            typeof(MapField<,>.Codec).FullName,
            typeof(ReadOnlyCollection<>).FullName,
            typeof(IReadOnlyDictionary<,>).FullName
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
            typeof(BoolState).FullName,
            typeof(Int32State).FullName,
            typeof(UInt32State).FullName,
            typeof(Int64State).FullName,
            typeof(UInt64State).FullName,
            typeof(StringState).FullName,
            typeof(BytesState).FullName,
            
            // Complex state types
            typeof(ReadonlyState<>).FullName,
            typeof(SingletonState<>).FullName,
            typeof(MappedState<,>).FullName,
            typeof(MappedState<,,>).FullName,
            typeof(MappedState<,,,>).FullName,
            typeof(MappedState<,,,,>).FullName,
            typeof(MethodReference<,>).FullName,
            typeof(ProtobufState<>).FullName,
        };
    }
    
    public class ContractStructureValidatorResult : ValidationResult
    {
        public ContractStructureValidatorResult(string message) : base(message)
        {
        }
    }
}