using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp.State;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Google.Protobuf.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Volo.Abp.DependencyInjection;

namespace AElf.CSharp.CodeOps.Validators.Module
{
    public class ContractStructureValidator : IValidator<ModuleDefinition>, ITransientDependency
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

        public bool SystemContactIgnored => false;

        public IEnumerable<ValidationResult> Validate(ModuleDefinition module, CancellationToken ct)
        {
            if (ct.IsCancellationRequested)
                throw new ContractAuditTimeoutException();
            
            var structureError = ValidateStructure(module);

            if (structureError != null)
                return new[] { structureError }.ToList();

            return module.Types.SelectMany(t => ValidateType(t, ct));
        }

        private ValidationResult ValidateStructure(ModuleDefinition module)
        {
            // There should be only one contract base
            var contractBase = module.Types
                .SelectMany(t => t.NestedTypes.Where(nt => nt.IsContractImplementation()))
                .SingleOrDefault();

            if (contractBase == null)
            {
                return new ContractStructureValidationResult("Contract base not found.");
            }

            var contractImplementation = module.Types.SingleOrDefault(t => t.IsContractImplementation());
            if (contractImplementation == null)
            {
                return new ContractStructureValidationResult("Contract implementation not found.");
            }

            if (contractImplementation.BaseType != contractBase)
            {
                return new ContractStructureValidationResult(
                    $"Contract implementation should inherit from {contractBase.Name} " +
                    $"but inherited from {contractImplementation.BaseType.Name}");
            }

            var contractState = contractBase.BaseType is GenericInstanceType genericType
                ? genericType.GenericArguments.SingleOrDefault()
                : null;

            if (contractState == null)
            {
                return new ContractStructureValidationResult("Contract state not found.");
            }

            var currentContractStateBase = ((TypeDefinition) contractState).BaseType.FullName;
            var aelfContractStateBase = typeof(ContractState).FullName;
            if (currentContractStateBase != aelfContractStateBase)
            {
                return new ContractStructureValidationResult(
                    $"Contract state should inherit from {aelfContractStateBase} " + 
                    $"but inherited from {currentContractStateBase}");
            }

            return null;
        }

        private IEnumerable<ValidationResult> ValidateType(TypeDefinition type, CancellationToken ct)
        {
            if (ct.IsCancellationRequested)
                throw new ContractAuditTimeoutException();
            
            var errors = new List<ValidationResult>();
            
            if (type.IsStateImplementation())
            {
                return ValidateContractStateType(type);
            }
            
            errors.AddRange(type.IsContractImplementation() ? ValidateContractType(type) : ValidateRegularType(type));
            errors.AddRange(type.NestedTypes.SelectMany(t => ValidateType(t, ct)));

            return errors;
        }

        private IEnumerable<ValidationResult> ValidateContractStateType(TypeDefinition type)
        {
            // Only allow MappedState, ContractReferenceState or MethodReference fields
            var badFields = type.Fields.Where(IsBadStateField).ToList();
            
            if (badFields.Any())
            {
                return badFields.Select(f => 
                    new ContractStructureValidationResult(
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
            // Simply, do not allow if the field is readonly and not one of the allowed types
            var errors = type.Fields
                .Where(f => f.IsInitOnly && !_allowedStaticFieldInitOnlyTypes.Contains(FieldTypeFullName(f)))
                .Select(f => 
                    new ContractStructureValidationResult("Only primitive types or whitelisted types are allowed as readonly fields in contract implementation.")
                    .WithInfo(f.Name, type.Namespace, type.Name, f.Name)).ToList();

            // Do not allow setting value of any non-constant, non-readonly field in constructor
            foreach (var method in type.Methods.Where(m => m.IsConstructor))
            {
                foreach (var instruction in method.Body.Instructions
                    .Where(i => i.OpCode == OpCodes.Stfld || i.OpCode == OpCodes.Stsfld))
                {
                    if (instruction.Operand is FieldDefinition field && !(field.IsInitOnly || field.HasConstant))
                    {
                        errors.Add(new ContractStructureValidationResult($"Initialization value is not allowed for field {field.Name}.")
                            .WithInfo(method.Name, type.Namespace, type.Name, field.Name));
                    }
                }
            }

            return errors;
        }

        private IEnumerable<ValidationResult> ValidateRegularType(TypeDefinition type)
        {
            // Skip if ExecutionObserverThreshold (validated in a separate validator)
            if (type.Name == typeof(ExecutionObserverProxy).Name)
                return Enumerable.Empty<ValidationResult>();

            var staticFields = type.Fields.Where(f => f.IsStatic);
            var badFields = staticFields.Where(IsBadField).ToList();

            var errors = badFields.Select(f => 
                new ContractStructureValidationResult(
                        $"{f.FieldType.FullName} type is not allowed to be used as static field in regular types in contract.")
                    .WithInfo(f.Name, type.Namespace, type.Name, f.Name))
                .ToList();
            
            foreach (var method in type.Methods.Where(m => m.IsConstructor))
            {
                foreach (var instruction in method.Body.Instructions
                    .Where(i => i.OpCode == OpCodes.Stfld || i.OpCode == OpCodes.Stsfld))
                {
                    if (instruction.Operand is FieldDefinition field && 
                        field.FieldType.FullName != typeof(FileDescriptor).FullName && // Allow FileDescriptor fields
                        field.IsStatic && !(field.IsInitOnly || field.HasConstant))
                    {
                        errors.Add(new ContractStructureValidationResult($"Initialization value is not allowed for field {field.Name}.")
                            .WithInfo(method.Name, type.Namespace, type.Name, field.Name));
                    }
                }
            }

            return errors;
        }

        private bool IsBadField(FieldDefinition field)
        {
            var fieldTypeFullName = FieldTypeFullName(field);
            
            // If constant, which means it is also primitive, then not a bad field
            if (field.HasConstant)
                return false;
            
            // If a field is init only, then only pass allowed types
            if (field.IsInitOnly && 
                (_allowedStaticFieldInitOnlyTypes.Contains(fieldTypeFullName) || 
                 Constants.PrimitiveTypes.Contains(fieldTypeFullName)))
            {
                // If field type is ReadOnly GenericInstanceType, only primitive types are allowed
                // ReadOnlyCollection, ReadOnlyDictionary etc.
                if (field.FieldType is GenericInstanceType fieldType && field.FieldType.Name.Contains("ReadOnly"))
                {
                    return fieldType.GenericArguments.Any(a => !Constants.PrimitiveTypes.Contains(a.FullName));
                }

                return false;
            }

            // If a type has a field that is same type of itself (Seen in nested classes generated due to Linq)
            // Don't allow any instance fields inside the type of the readonly static field
            if (fieldTypeFullName == field.DeclaringType.FullName && field.DeclaringType.Fields.All(f => f.IsStatic))
                return false;

            return field.IsInitOnly;
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
        
        // For example, we need to allow only primitive types in read only collections
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
    
    public class ContractStructureValidationResult : ValidationResult
    {
        public ContractStructureValidationResult(string message) : base(message)
        {
        }
    }
}