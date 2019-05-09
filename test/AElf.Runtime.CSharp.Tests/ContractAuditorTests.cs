using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AElf.Contracts.Genesis;
using AElf.Contracts.MultiToken;
using AElf.Runtime.CSharp.Validators;
using AElf.Runtime.CSharp.Validators.Method;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace AElf.Runtime.CSharp.Tests
{
    public class ContractAuditorTests : CSharpRuntimeTestBase
    {
        private ContractAuditor _auditor;
        private IEnumerable<ValidationResult> _findings;
        private readonly string _contractDllDir = "../../../contracts/";

        public ContractAuditorTests(ITestOutputHelper testOutputHelper)
        {
            _auditor = new ContractAuditor();
            
            Should.Throw<InvalidCodeException>(() =>
            {
                try
                {
                    _auditor.Audit(ReadCode(_contractDllDir + typeof(BadContract.BadContract).Module),
                        false);
                }
                catch (InvalidCodeException ex)
                {
                    _findings = ex.Findings;
                    throw ex;
                }
            });
        }

        [Fact]
        public void CheckBadContract_ForRandomUsage()
        {
            LookFor(_findings, i => i.Namespace == "System" && i.Type == "Random")
                .ShouldNotBeNull();
        }
        
        [Fact]
        public void CheckBadContract_ForUtcNowUsage()
        {
            LookFor(_findings, i => i.Namespace == "System" && i.Type == "DateTime" && i.Member == "get_UtcNow")
                .ShouldNotBeNull();
        }        
        
        [Fact]
        public void CheckBadContract_ForDoubleType()
        {
            LookFor(_findings, i => i.Namespace == "System" && i.Type == "Double")
                .ShouldNotBeNull();
        }
        
        [Fact]
        public void CheckBadContract_ForDiskOperations()
        {
            LookFor(_findings, i => i.Namespace == "System.IO")
                .ShouldNotBeNull();
        }
        
        [Fact]
        public void CheckBadContract_ForFloatOperations()
        {
            _findings.FirstOrDefault(f => f.GetType() == typeof(FloatOpsValidationResult))
                .ShouldNotBeNull();
        }

        [Fact]
        public void CodeCheck_DefaultContracts()
        {
            // TODO: Add other contracts in contract security test once contract dependencies are simplified.
            var contracts = new[]
            {
                typeof(TestContract.TestContract).Module.ToString(),
                typeof(BasicContractZero).Module.ToString(),
                typeof(TokenContract).Module.ToString(),
            };

            // Load the DLL's from contracts folder to prevent codecov injection
            foreach (var contract in contracts)
            {
                var contractDllPath = _contractDllDir + contract;
                
                Should.NotThrow(()=>_auditor.Audit(ReadCode(contractDllPath), false));
            }
        }

        private Info LookFor(IEnumerable<ValidationResult>  findings, Func<Info, bool> criteria)
        {
            return findings.Select(f => f.Info).Where(criteria).FirstOrDefault();
        }

        private byte[] ReadCode(string path)
        {
            return File.Exists(path) ? File.ReadAllBytes(path) : throw new FileNotFoundException("Contract DLL cannot be found. " + path);
        }
    }
}
