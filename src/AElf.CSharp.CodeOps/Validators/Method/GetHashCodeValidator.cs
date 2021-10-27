using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Volo.Abp.DependencyInjection;

namespace AElf.CSharp.CodeOps.Validators.Method
{
    public class GetHashCodeValidator : IValidator<MethodDefinition>, ITransientDependency
    {
        public bool SystemContactIgnored => false;

        public IEnumerable<ValidationResult> Validate(MethodDefinition method, CancellationToken ct)
        {
            if (ct.IsCancellationRequested)
                throw new ContractAuditTimeoutException();
            
            if (!method.HasBody)
                return Enumerable.Empty<ValidationResult>();
            
            return method.Name == nameof(GetHashCode) ? 
                ValidateGetHashCodeMethod(method) : ValidateRegularMethod(method);
        }
        
        private IEnumerable<ValidationResult> ValidateRegularMethod(MethodDefinition method)
        {
            var errors = new List<ValidationResult>();

            // Do not allow calls to GetHashCode method from non GetHashCode methods in contract
            foreach (var instruction in method.Body.Instructions)
            {
                if (!(instruction.Operand is MethodReference accessedMethod)) continue;

                if (accessedMethod.Name == nameof(GetHashCode))
                {
                    errors.Add(new GetHashCodeValidationResult(
                            "It is not allowed to access GetHashCode method from a non GetHashCode method.")
                        .WithInfo(method.Name, method.DeclaringType.Namespace, method.DeclaringType.Name, method.Name));
                }
            }

            return errors;
        }

        private IEnumerable<ValidationResult> ValidateGetHashCodeMethod(MethodDefinition method)
        {
            var errors = new List<ValidationResult>();
            
            foreach (var instruction in method.Body.Instructions)
            {
                GetHashCodeValidationResult error = null;
                
                switch (instruction.Operand)
                {
                    case MethodReference accessedMethod:
                        var methodDefinition = accessedMethod.Resolve();
                        
                        if (!(IsGetHashCodeCall(methodDefinition)
                              || IsFieldGetterCall(methodDefinition)
                              || IsGetLengthCall(methodDefinition)
                              || IsExecutionObserverCall(methodDefinition)
                              || IsInequalityOperatorCall(methodDefinition)))
                        {
                            error = new GetHashCodeValidationResult(
                                    $"It is not allowed to access {accessedMethod.Name} method within GetHashCode method.");
                        }
                        
                        break;
                    
                    case FieldReference accessedField:
                        if (instruction.OpCode != OpCodes.Ldfld) // Only allow ldfld, do not allow the rest with fields
                            error = new GetHashCodeValidationResult(
                                $"It is not allowed to set {accessedField.Name} field within GetHashCode method.");
                        break;
                }

                if (error != null)
                {
                    errors.Add(error.WithInfo(method.Name, 
                        method.DeclaringType.Namespace, method.DeclaringType.Name, method.Name));
                }
            }

            return errors;
        }

        private bool IsInequalityOperatorCall(MethodDefinition method)
        {
            if (!(method.Name == "op_Inequality" 
                  && method.Parameters.Count == 2 
                  && method.ReturnType.FullName == typeof(bool).FullName))
                return false;

            // Both input parameters should be of declaring type
            return method.Parameters.All(p => p.ParameterType == method.DeclaringType);
        }

        private bool IsFieldGetterCall(MethodDefinition method)
        {
            return method.Name.StartsWith("get_") && !method.HasParameters && method.HasBody;
        }

        private bool IsGetLengthCall(MethodDefinition method)
        {
            // Allowed for 2 types only
            return (method.DeclaringType.FullName == "System.String" || method.DeclaringType.FullName == "Google.Protobuf.ByteString") 
                   && method.Name == "get_Length" && !method.HasParameters && method.ReturnType.FullName == typeof(int).FullName;
        }

        private bool IsExecutionObserverCall(MethodDefinition method)
        {
            return method.DeclaringType.Name == nameof(ExecutionObserverProxy)
                   && (method.Name == nameof(ExecutionObserverProxy.CallCount) || method.Name == nameof(ExecutionObserverProxy.BranchCount)) 
                   && method.ReturnType.FullName == "System.Void"
                   && !method.HasParameters;
        }

        private bool IsGetHashCodeCall(MethodDefinition method)
        {
            return method.Name == nameof(GetHashCode) && !method.HasParameters;
        }
    }
    
    public class GetHashCodeValidationResult : ValidationResult
    {
        public GetHashCodeValidationResult(string message) : base(message)
        {
        }
    }
}