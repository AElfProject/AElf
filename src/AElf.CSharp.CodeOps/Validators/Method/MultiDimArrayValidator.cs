using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Volo.Abp.DependencyInjection;

namespace AElf.CSharp.CodeOps.Validators.Method
{
    public class MultiDimArrayValidator : IValidator<MethodDefinition>, ITransientDependency
    {
        public bool SystemContactIgnored => false;

        public IEnumerable<ValidationResult> Validate(MethodDefinition method, CancellationToken ct)
        {
            if (ct.IsCancellationRequested)
                throw new ContractAuditTimeoutException();
            
            if (!method.HasBody)
                return Enumerable.Empty<ValidationResult>();
            
            var errors = new List<ValidationResult>();
            
            foreach (var instruction in method.Body.Instructions)
            {
                if (instruction.OpCode.Code == Code.Newarr)
                {
                    var typeReference = (TypeReference) instruction.Operand;

                    if (typeReference.IsArray)
                    {
                        errors.Add(
                            new MultiDimArrayValidationResult($"{method.Name} contains multi dimension array declaration.")
                                    .WithInfo(method.Name, method.DeclaringType.Namespace, method.DeclaringType.Name, null));
                    }
                }
            }

            return errors;
        }
    }

    public class MultiDimArrayValidationResult : ValidationResult
    {
        public MultiDimArrayValidationResult(string message) : base(message)
        {
        }
    }
}