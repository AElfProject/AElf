using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AElf.Contracts.Association;
using AElf.Contracts.Configuration;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.CrossChain;
using AElf.Contracts.Economic;
using AElf.Contracts.Election;
using AElf.Contracts.Genesis;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Parliament;
using AElf.Contracts.Profit;
using AElf.Contracts.Referendum;
using AElf.Contracts.TestContract.TransactionFees;
using AElf.Contracts.TokenConverter;
using AElf.Contracts.Treasury;
using AElf.CSharp.CodeOps.Validators;
using AElf.CSharp.CodeOps.Validators.Assembly;
using AElf.CSharp.CodeOps.Validators.Method;
using AElf.Runtime.CSharp.Tests.BadContract;
using Shouldly;
using Xunit;

namespace AElf.CSharp.CodeOps
{
    public class ContractAuditorFixture : IDisposable
    {
        private ContractAuditor _auditor;
        private readonly RequiredAcsDto _requiredAcs;

        public ContractAuditorFixture()
        {
            _auditor = new ContractAuditor(null, null);
            _requiredAcs = new RequiredAcsDto
            {
                AcsList = new [] {"acs1", "acs8"}.ToList(), 
                RequireAll = false
            };
        }

        public void Audit(byte[] code)
        {
            _auditor.Audit(code, _requiredAcs, false);
        }

        public void Dispose()
        {
            _auditor = null;
        }
    }

    public class ContractAuditorTests : CSharpCodeOpsTestBase, IClassFixture<ContractAuditorFixture>
    {
        private readonly ContractAuditorFixture _auditorFixture;
        private const string ContractDllDir = "../../../contracts/";

        public ContractAuditorTests(ContractAuditorFixture auditorFixture)
        {
            // Use fixture to instantiate auditor only once
            _auditorFixture = auditorFixture;
        }

        #region Positive Cases
        
        [Theory]
        [InlineData(typeof(AssociationContract))]
        [InlineData(typeof(ConfigurationContract))]
        [InlineData(typeof(AEDPoSContract))]
        [InlineData(typeof(CrossChainContract))]
        [InlineData(typeof(EconomicContract))]
        [InlineData(typeof(ElectionContract))]
        [InlineData(typeof(BasicContractZero))]
        [InlineData(typeof(TokenContract))]
        [InlineData(typeof(ParliamentContract))]
        [InlineData(typeof(ProfitContract))]
        [InlineData(typeof(ReferendumContract))]
        [InlineData(typeof(TokenConverterContract))]
        [InlineData(typeof(TreasuryContract))]
        public void CheckSystemContracts_AllShouldPass(Type contractType)
        {
            Should.NotThrow(()=>_auditorFixture.Audit(ReadPatchedContractCode(contractType)));
        }

        [Fact]
        public void ContractPatcher_Test()
        {
            var code = ReadContractCode(typeof(TokenContract));
            var updateCode = ContractPatcher.Patch(code);
            code.ShouldNotBe(updateCode);
            var exception = Record.Exception(() => _auditorFixture.Audit(updateCode));
            exception.ShouldBeNull();
        }

        #endregion

        #region Negative Cases

        [Fact]
        public void CheckBadContract_ForFindings()
        {
            var findings = Should.Throw<InvalidCodeException>(
                ()=>_auditorFixture.Audit(ReadContractCode(typeof(BadContract))))
                .Findings;
            
            // Should have identified that ACS1 or ACS8 is not there
            findings.FirstOrDefault(f => f is AcsValidationResult).ShouldNotBeNull();
            
            // Random usage
            LookFor(findings,
                    "UpdateStateWithRandom",
                    i => i.Namespace == "System" && i.Type == "Random")
                .ShouldNotBeNull();

            // DateTime UtcNow usage
            LookFor(findings,
                    "UpdateStateWithCurrentTime",
                    i => i.Namespace == "System" && i.Type == "DateTime" && i.Member == "get_UtcNow")
                .ShouldNotBeNull();

            // DateTime Now usage
            LookFor(findings,
                    "UpdateStateWithCurrentTime",
                    i => i.Namespace == "System" && i.Type == "DateTime" && i.Member == "get_Now")
                .ShouldNotBeNull();

            // DateTime Today usage
            LookFor(findings,
                    "UpdateStateWithCurrentTime",
                    i => i.Namespace == "System" && i.Type == "DateTime" && i.Member == "get_Today")
                .ShouldNotBeNull();

            // Double type usage
            LookFor(findings,
                    "UpdateDoubleState",
                    i => i.Namespace == "System" && i.Type == "Double")
                .ShouldNotBeNull();

            // Float type usage
            LookFor(findings,
                    "UpdateFloatState",
                    i => i.Namespace == "System" && i.Type == "Single")
                .ShouldNotBeNull();

            // Disk Ops usage
            LookFor(findings,
                    "WriteFileToNode",
                    i => i.Namespace == "System.IO")
                .ShouldNotBeNull();

            // String constructor usage
            LookFor(findings,
                    "InitLargeStringDynamic",
                    i => i.Namespace == "System" && i.Type == "String" && i.Member == ".ctor")
                .ShouldNotBeNull();

            // Denied member use in nested class
            LookFor(findings,
                    "UseDeniedMemberInNestedClass",
                    i => i.Namespace == "System" && i.Type == "DateTime" && i.Member == "get_Now")
                .ShouldNotBeNull();

            // Denied member use in separate class
            LookFor(findings,
                    "UseDeniedMemberInSeparateClass",
                    i => i.Namespace == "System" && i.Type == "DateTime" && i.Member == "get_Now")
                .ShouldNotBeNull();

            // Large array initialization
            findings.FirstOrDefault(f => f is ArrayValidationResult && f.Info.ReferencingMethod == "InitLargeArray")
                .ShouldNotBeNull();

            // Float operations
            findings.FirstOrDefault(f => f is FloatOpsValidationResult)
                .ShouldNotBeNull();

            var getHashCodeFindings = findings.Where(f => f is GetHashCodeValidationResult).ToList();
            LookFor(getHashCodeFindings, "TestGetHashCodeCall", f => f != null).ShouldNotBeNull();
            LookFor(getHashCodeFindings, "GetHashCode", f => f != null).ShouldNotBeNull();
        }

        [Fact]
        public void CheckPatchAudit_ForUncheckedMathOpcodes()
        {
            // Here, we use any contract that contains unchecked math OpCode even with "Check for arithmetic overflow"
            // checked in the project. If first section of below test case fails, need to create another contract  
            // that iterates an array with foreach loop.
            var contractCode = ReadCode(ContractDllDir + typeof(TransactionFeesContract).Module);
            
            var findings = Should.Throw<InvalidCodeException>(
                    ()=>_auditorFixture.Audit(contractCode))
                .Findings;
            
            findings.FirstOrDefault(f => f is UncheckedMathValidationResult)
                .ShouldNotBeNull();
            
            // After patching, all unchecked arithmetic OpCodes should be cleared.
            Should.NotThrow(() => _auditorFixture.Audit(ContractPatcher.Patch(contractCode)));
        }

        #endregion

        #region Test Helpers

        byte[] ReadCode(string path)
        {
            return File.Exists(path)
                ? File.ReadAllBytes(path)
                : throw new FileNotFoundException("Contract DLL cannot be found. " + path);
        }

        Info LookFor(IEnumerable<ValidationResult> findings, string referencingMethod, Func<Info, bool> criteria)
        {
            return findings.Select(f => f.Info)
                .FirstOrDefault(i => i != null && i.ReferencingMethod == referencingMethod && criteria(i));
        }

        byte[] ReadContractCode(Type contractType)
        {
            return ReadCode(ContractDllDir + contractType.Module);
        }

        byte[] ReadPatchedContractCode(Type contractType)
        {
            return ReadCode(ContractDllDir + contractType.Module + ".patched");
        }

        #endregion
    }
}