using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AElf.Contracts.AssociationAuth;
using AElf.Contracts.Election;
using AElf.Contracts.Genesis;
using AElf.Contracts.MultiToken;
using AElf.Contracts.ParliamentAuth;
using AElf.Contracts.Profit;
using AElf.Contracts.ReferendumAuth;
using AElf.Contracts.Resource.FeeReceiver;
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
        
        #region Positive Cases
        
        [Fact]
        public void CheckDefaultContracts_AllShouldPass()
        {
            // TODO: Add other contracts in contract security test once contract dependencies are simplified.
            var contracts = new[]
            {
                //typeof(AssociationAuthContract).Module.ToString(),
                //typeof(ElectionContract).Module.ToString(), // Failing due to TimeSpan / FloatOps
                typeof(BasicContractZero).Module.ToString(),
                typeof(TokenContract).Module.ToString(),
                //typeof(ParliamentAuthContract).Module.ToString(),
                //typeof(ProfitContract).Module.ToString(),
                //typeof(ReferendumAuthContract).Module.ToString(),
                //typeof(FeeReceiverContract).Module.ToString(),
                typeof(TestContract.TestContract).Module.ToString(),
            };

            // Load the DLL's from contracts folder to prevent codecov injection
            foreach (var contract in contracts)
            {
                var contractDllPath = _contractDllDir + contract;
                
                Should.NotThrow(()=>_auditor.Audit(ReadCode(contractDllPath), false));
            }
        }
        
        #endregion
        
        #region Negative Cases

        [Fact]
        public void CheckBadContract_ForRandomUsage()
        {
            LookFor(_findings, 
                    "UpdateStateWithRandom", 
                    i => i.Namespace == "System" && i.Type == "Random")
                .ShouldNotBeNull();
        }
        
        [Fact]
        public void CheckBadContract_ForDateTimeUtcNowUsage()
        {
            LookFor(_findings, 
                    "UpdateStateWithCurrentTime", 
                    i => i.Namespace == "System" && i.Type == "DateTime" && i.Member == "get_UtcNow")
                .ShouldNotBeNull();
        }
        
        [Fact]
        public void CheckBadContract_ForDateTimeNowUsage()
        {
            LookFor(_findings, 
                    "UpdateStateWithCurrentTime",
                    i => i.Namespace == "System" && i.Type == "DateTime" && i.Member == "get_Now")
                .ShouldNotBeNull();
        }
        
        [Fact]
        public void CheckBadContract_ForDateTimeTodayUsage()
        {
            LookFor(_findings, 
                    "UpdateStateWithCurrentTime",
                    i => i.Namespace == "System" && i.Type == "DateTime" && i.Member == "get_Today")
                .ShouldNotBeNull();
        }

        [Fact]
        public void CheckBadContract_ForDoubleTypeUsage()
        {
            LookFor(_findings, 
                    "UpdateDoubleState",
                    i => i.Namespace == "System" && i.Type == "Double")
                .ShouldNotBeNull();
        }
        
        [Fact]
        public void CheckBadContract_ForFloatTypeUsage()
        {
            // http://docs.microsoft.com/en-us/dotnet/api/system.single
            LookFor(_findings, 
                    "UpdateFloatState",
                    i => i.Namespace == "System" && i.Type == "Single") 
                .ShouldNotBeNull();
        }
        
        [Fact]
        public void CheckBadContract_ForDiskOpsUsage()
        {
            LookFor(_findings, 
                    "WriteFileToNode",
                    i => i.Namespace == "System.IO")
                .ShouldNotBeNull();
        }

        [Fact]
        public void CheckBadContract_ForDeniedMemberUseInNestedClass()
        {
            LookFor(_findings, 
                    "UseDeniedMemberInNestedClass",
                    i => i.Namespace == "System" && i.Type == "DateTime" && i.Member == "get_Now")
                .ShouldNotBeNull();
        }
        
        [Fact]
        public void CheckBadContract_ForDeniedMemberUseInSeparateClass()
        {
            LookFor(_findings, 
                    "UseDeniedMemberInSeparateClass",
                    i => i.Namespace == "System" && i.Type == "DateTime" && i.Member == "get_Now")
                .ShouldNotBeNull();
        }
        
        [Fact]
        public void CheckBadContract_ForFloatOperations()
        {
            _findings.FirstOrDefault(f => f.GetType() == typeof(FloatOpsValidationResult))
                .ShouldNotBeNull();
        }
        
        #endregion

        #region Test Helpers

        private Info LookFor(IEnumerable<ValidationResult>  findings, string referencingMethod, Func<Info, bool> criteria)
        {
            return findings.Select(f => f.Info).FirstOrDefault(i => i.ReferencingMethod == referencingMethod && criteria(i));
        }

        private byte[] ReadCode(string path)
        {
            return File.Exists(path) ? File.ReadAllBytes(path) : throw new FileNotFoundException("Contract DLL cannot be found. " + path);
        }
        
        #endregion
    }
}
