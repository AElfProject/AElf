using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Google.Protobuf.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Volo.Abp.DependencyInjection;

namespace AElf.CSharp.CodeOps.Validators.Method
{
    public class DescriptorAccessValidator : IValidator<MethodDefinition>, ITransientDependency
    {
        public bool SystemContactIgnored => false;

        public IEnumerable<ValidationResult> Validate(MethodDefinition method, CancellationToken ct)
        {
            if (ct.IsCancellationRequested)
                throw new ContractAuditTimeoutException();
            
            if (!method.HasBody)
                return Enumerable.Empty<ValidationResult>();

            // If there is any non-constructor method accessing a FileDescriptor type field to set,
            // then this is a problem. FileDescriptor field should only be set in constructor.
            // If there is any access to set a FileDescriptor type field from a constructor outside of its declaring class, 
            // then it is a problem too
            var instructions = method.Body.Instructions
                .Where(i => i.OpCode == OpCodes.Stsfld &&
                            i.Operand is FieldDefinition field &&
                            field.FieldType.FullName == typeof(FileDescriptor).FullName && 
                            (!method.IsConstructor || field.DeclaringType != method.DeclaringType)).ToArray();
            
            if (instructions.Any())
            {
                return instructions.Select(i => new DescriptorAccessValidationResult(
                        "It is not allowed to set FileDescriptor type static field outside of its declaring type's constructor.")
                        .WithInfo(method.Name, method.DeclaringType.Namespace, method.DeclaringType.Name, null)); 
            }

            return Enumerable.Empty<ValidationResult>();
        }
    }
    
    public class DescriptorAccessValidationResult : ValidationResult
    {
        public DescriptorAccessValidationResult(string message) : base(message)
        {
        }
    }
}