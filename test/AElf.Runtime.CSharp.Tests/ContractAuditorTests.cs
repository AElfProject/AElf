using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AElf.Contracts.AssociationAuth;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.CrossChain;
using AElf.Contracts.Election;
using AElf.Contracts.Genesis;
using AElf.Contracts.MultiToken;
using AElf.Contracts.ParliamentAuth;
using AElf.Contracts.Profit;
using AElf.Contracts.ReferendumAuth;
using AElf.Contracts.Resource.FeeReceiver;
using AElf.Contracts.TokenConverter;
using AElf.Runtime.CSharp.Helper;
using AElf.Runtime.CSharp.Validators;
using AElf.Runtime.CSharp.Validators.Method;
using Mono.Cecil.Cil;
using Shouldly;
using Xunit;
using ValidationResult = AElf.Runtime.CSharp.Validators.ValidationResult;

namespace AElf.Runtime.CSharp.Tests
{
    public class ContractAuditorFixture : IDisposable
    {
        private ContractAuditor _auditor;

        public ContractAuditorFixture()
        {
            _auditor = new ContractAuditor(null, null);
        }

        public void Audit(byte[] code)
        {
            _auditor.Audit(code, false);
        }

        public void Dispose()
        {
            _auditor = null;
        }
    }

    public class ContractAuditorTests : CSharpRuntimeTestBase, IClassFixture<ContractAuditorFixture>
    {
        private readonly ContractAuditorFixture _auditorFixture;
        private readonly string _contractDllDir = "../../../contracts/";
        private readonly Type[] _contracts = {
            typeof(AssociationAuthContract),
            typeof(AEDPoSContract),
            typeof(CrossChainContract),
            typeof(ElectionContract),
            typeof(BasicContractZero),
            typeof(TokenContract),
            typeof(ParliamentAuthContract),
            typeof(ProfitContract),
            typeof(ReferendumAuthContract),
            typeof(FeeReceiverContract),
            typeof(TokenConverterContract),
            typeof(TestContract.TestContract),
        };

        public ContractAuditorTests(ContractAuditorFixture auditorFixture)
        {
            // Use fixture to instantiate auditor only once
            _auditorFixture = auditorFixture;
        }
        
        #region Positive Cases
        
        [Fact]
        public void CheckSystemContracts_AllShouldPass()
        {
            // Load the DLL's from contracts folder to prevent codecov injection
            foreach (var contractPath in _contracts.Select(c => _contractDllDir + c.Module.ToString()))
            {
                Should.NotThrow(()=>_auditorFixture.Audit(ReadCode(contractPath)));
            }
        }

        #endregion
        
        #region Negative Cases

        [Fact]
        public void CheckBadContract_ForFindings()
        {
            var findings = Should.Throw<InvalidCodeException>(
                ()=>_auditorFixture.Audit(ReadCode(_contractDllDir + typeof(BadContract.BadContract).Module)))
                .Findings;
            
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
        }
        
        [Fact]
        public void CheckILVerifier_IsFunctional()
        { 
            const string dummyCode =  @"using System;

                                        public class DummyClass
                                        {
                                            public int SimpleAdd()
                                            {
                                                var a = 2;
                                                var b = 3;
                                                return a + b;
                                            }
                                        }";
            
            var validAssembly = new MemoryStream();
            var dummyAssembly = AssemblyCompiler.Compile("DummyLib", dummyCode);
            dummyAssembly.Write(validAssembly);

            var typ = dummyAssembly.MainModule.GetType("DummyClass");

            var testMethod = typ.Methods.FirstOrDefault(m => m.Name == "SimpleAdd");

            if (testMethod != null)
            {
                var processor = testMethod.Body.GetILProcessor();
                
                // Break IL codes by injecting a line that loads string to stack while adding 2 integers
                processor.Body.Instructions.Insert(2, processor.Create(OpCodes.Ldstr, "AElf"));
            }
            
            var invalidAssembly = new MemoryStream();

            dummyAssembly.Write(invalidAssembly);
            
            // Ensure contract auditor doesn't throw any exception
            Should.NotThrow(()=>_auditorFixture.Audit(validAssembly.ToArray()));
            
            // Ensure ILVerifier is doing its job
            Should.Throw<InvalidCodeException>(()=>_auditorFixture.Audit(invalidAssembly.ToArray()))
                .Findings.FirstOrDefault(f => f is ILVerifierResult).ShouldNotBeNull();
        }

        #endregion
        
        #region Test Helpers
        
        byte[] ReadCode(string path)
        {
            return File.Exists(path) ? File.ReadAllBytes(path) : throw new FileNotFoundException("Contract DLL cannot be found. " + path);
        }
        
        Info LookFor(IEnumerable<ValidationResult> findings, string referencingMethod, Func<Info, bool> criteria)
        {
            return findings.Select(f => f.Info).FirstOrDefault(i => i != null && i.ReferencingMethod == referencingMethod && criteria(i));
        }
        
        #endregion
    }
}
