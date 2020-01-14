using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace AElf.CSharp.CodeOps.Validators.Method
{
    public class GetHashCodeValidator : IValidator<MethodDefinition>
    {
        public IEnumerable<ValidationResult> Validate(MethodDefinition method)
        {
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
                if (!(instruction.Operand is MethodReference accessedMethod)) continue;

                var methodDefinition = accessedMethod.Resolve();
                
                if (!(IsGetHashCodeCall(methodDefinition)
                       || IsFieldGetterCall(methodDefinition)
                       || IsExecutionObserverCall(methodDefinition)
                       || IsInequalityOperatorCall(methodDefinition)))
                {
                    errors.Add(new GetHashCodeValidationResult(
                        $"It is not allowed to access {accessedMethod.Name} method within GetHashCode method.")
                        .WithInfo(method.Name, method.DeclaringType.Namespace, method.DeclaringType.Name, method.Name));
                }
            }

            return errors;
        }

        private bool IsInequalityOperatorCall(MethodDefinition method)
        {
            var parameterTypes = method.Parameters
                .Select(p => p.ParameterType).Distinct().ToList();

            // All parameters should be of declaring type and also same type
            return parameterTypes.Count == 1 && method.DeclaringType == parameterTypes.FirstOrDefault();
        }

        private bool IsFieldGetterCall(MethodDefinition method)
        {
            if (!(method.Name.StartsWith("get_") && !method.HasParameters))
                return false;

            if ((method.DeclaringType.FullName == "System.String" || method.DeclaringType.FullName == "Google.Protobuf.ByteString") 
                && method.Name == "get_Length")
                return true;
            
            // For other type of objects, check whether the field really exists in the declaring type
            var fieldNames = method.DeclaringType.Fields.Select(f => f.Name.ToLowerInvariant());

            return fieldNames.Contains(method.Name.Substring(4).ToLowerInvariant() + "_");
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