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
using AElf.CSharp.CodeOps.Validators.Module;
using AElf.Runtime.CSharp.Tests.BadContract;
using Mono.Cecil.Cil;
using Shouldly;
using Xunit;
using MethodDefinition = Mono.Cecil.MethodDefinition;

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
                AcsList = new[] {"acs1", "acs8"}.ToList(),
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
            Should.NotThrow(() => _auditorFixture.Audit(ReadPatchedContractCode(contractType)));
        }

        //[Fact]
        public void ContractPatcher_Test()
        {
            var code = ReadContractCode(typeof(TokenContract));
            var updateCode = ContractPatcher.Patch(code);
            code.ShouldNotBe(updateCode);
            var exception = Record.Exception(() => _auditorFixture.Audit(updateCode));
            exception.ShouldBeNull();
        }

        #endregion

        [Fact]
        public void PatchTest()
        {
            ContractPatcher.Patch(ReadContractCode(typeof(AEDPoSContract)));
        }

        #region Negative Cases

        [Fact]
        public void CheckBadContract_ForFindings()
        {
            var findings = Should.Throw<InvalidCodeException>(
                    () => _auditorFixture.Audit(ReadContractCode(typeof(BadContract))))
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

            // FileDescriptor type field not allowed to be set outside of its declaring type constructor
            findings
                .FirstOrDefault(f => f is DescriptorAccessValidationResult &&
                                     f.Info.Type == "BadCase1" && f.Info.ReferencingMethod == "SetFileDescriptor")
                .ShouldNotBeNull();

            // Initialization value not allowed for resettable members, in regular type
            findings
                .FirstOrDefault(f => f is ContractStructureValidationResult &&
                                     f.Info.Type == "BadCase2" && f.Info.ReferencingMethod == ".cctor" &&
                                     f.Info.Member == "Number").ShouldNotBeNull();

            // Initialization value not allowed for resettable members, in contract
            findings
                .FirstOrDefault(f => f is ContractStructureValidationResult &&
                                     f.Info.Type == "BadContract" && f.Info.ReferencingMethod == ".ctor" &&
                                     f.Info.Member == "i").ShouldNotBeNull();

            // A type that is not allowed as a static readonly field
            findings.FirstOrDefault(f => f is ContractStructureValidationResult &&
                                         f.Info.Type == "BadCase3" && f.Info.Member == "field").ShouldNotBeNull();

            // Not allowed as readonly if a type has a field with its own type and has instance field
            findings.FirstOrDefault(f => f is ContractStructureValidationResult &&
                                         f.Info.Type == "BadCase4" && f.Info.Member == "field").ShouldNotBeNull();

            // Non primitive type is not allowed as an argument in ReadOnly GenericInstanceType type in static readonly fields
            findings.FirstOrDefault(f => f is ContractStructureValidationResult &&
                                         f.Info.Type == "BadCase5" && f.Info.Member == "collection").ShouldNotBeNull();

            // A type that is not allowed to be used as a field in contract state
            findings
                .FirstOrDefault(f => f is ContractStructureValidationResult &&
                                     f.Info.Type == "BadContractState" && f.Info.Member == "i").ShouldNotBeNull();
        }

        [Fact]
        public void CheckResetField_ForMissingReset()
        {
            var code = ReadContractCode(typeof(BadContract));
            var module = Mono.Cecil.ModuleDefinition.ReadModule(new MemoryStream(code));
            var resetFieldsMethod = module.Types
                .Single(t => t.IsContractImplementation())
                .Methods.Single(m => m.Name == "ResetFields");

            RemoveInstruction(resetFieldsMethod, i => i.OpCode == OpCodes.Stfld); // Remove field reset
            RemoveInstruction(resetFieldsMethod, i =>
                i.OpCode == OpCodes.Call && // Remove call to other type's ResetFields
                i.Operand is MethodDefinition method &&
                method.Name == "ResetFields");

            var tamperedContract = new MemoryStream();
            module.Write(tamperedContract);

            var findings = Should.Throw<InvalidCodeException>(
                    () => _auditorFixture.Audit(tamperedContract.ToArray()))
                .Findings;

            findings.FirstOrDefault(f => f is ResetFieldsValidationResult &&
                                         f.Message.Contains("missing reset for certain fields")).ShouldNotBeNull();

            findings.FirstOrDefault(f => f is ResetFieldsValidationResult &&
                                         f.Message.Contains("missing certain method calls")).ShouldNotBeNull();
        }

        [Fact]
        public void CheckPatchAudit_ForUncheckedMathOpcodes()
        {
            // Here, we use any contract that contains unchecked math OpCode even with "Check for arithmetic overflow"
            // checked in the project. If first section of below test case fails, need to create another contract  
            // that iterates an array with foreach loop.
            var contractCode = ReadContractCode(typeof(TransactionFeesContract));

            var findings = Should.Throw<InvalidCodeException>(
                    () => _auditorFixture.Audit(contractCode))
                .Findings;

            findings.FirstOrDefault(f => f is UncheckedMathValidationResult)
                .ShouldNotBeNull();

            // After patching, all unchecked arithmetic OpCodes should be cleared.
            Should.NotThrow(() => _auditorFixture.Audit(ContractPatcher.Patch(contractCode)));
        }

        #endregion

        #region Test Helpers

        Info LookFor(IEnumerable<ValidationResult> findings, string referencingMethod, Func<Info, bool> criteria)
        {
            return findings.Select(f => f.Info)
                .FirstOrDefault(i => i != null && i.ReferencingMethod == referencingMethod && criteria(i));
        }


        void RemoveInstruction(MethodDefinition method, Func<Instruction, bool> where)
        {
            var il = method.Body.GetILProcessor();

            il.Remove(method.Body.Instructions.First(where));
        }

        #endregion
    }
}