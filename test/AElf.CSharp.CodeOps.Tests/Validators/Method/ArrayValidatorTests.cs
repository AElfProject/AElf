using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AElf.Runtime.CSharp.Tests.BadContract;
using Mono.Cecil;
using Shouldly;
using Xunit;

namespace AElf.CSharp.CodeOps.Validators.Method
{
    public class ArrayValidatorTests : CSharpCodeOpsTestBase
    {
        [Fact]
        public void Validate_Test()
        {
            var badContractModule = GetContractModule(typeof(BadContract));
            var validationResults = new List<ValidationResult>();
            
            var cts = new CancellationTokenSource();
            cts.Cancel();
            Assert.Throws<ContractAuditTimeoutException>(() => Validate(badContractModule, validationResults, cts.Token));

            Validate(badContractModule, validationResults, CancellationToken.None);
            
            validationResults.ShouldContain(r=>r.Message.Contains("Array size can not be larger than 40960 bytes"));
            validationResults.ShouldContain(r=>r.Message.Contains("Array size is too large that causes overflow when estimating memory usage"));
            validationResults.ShouldContain(r=>r.Message.Contains("Array size can not be larger than 5 elements"));
            validationResults.ShouldContain(r=>r.Message.Contains("Array of System.Int32[] type is not allowed."));
            validationResults.ShouldContain(r=>r.Message.Contains("Array size could not be identified for System.Int32"));
        }

        private void Validate(ModuleDefinition moduleDefinition, List<ValidationResult> validationResults,
            CancellationToken token)
        {
            var validator = new ArrayValidator();
            foreach (var type in moduleDefinition.Types)
            {
                foreach (var method in type.Methods)
                {
                    validationResults.AddRange(validator.Validate(method, token));
                }
            }
        }
    }
}