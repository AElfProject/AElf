using System;
using System.IO;
using AElf.Contracts.Genesis;
using AElf.Contracts.MultiToken;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace AElf.Runtime.CSharp.Tests
{
    public class ContractAuditorTests : CSharpRuntimeTestBase
    {
        private readonly string _contractDllDirectory = AppDomain.CurrentDomain.BaseDirectory;
        private ContractAuditor _auditor;

        public ContractAuditorTests(ITestOutputHelper testOutputHelper)
        {
            _auditor = new ContractAuditor();
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
                var contractDllPath = _contractDllDirectory + contract;

                Should.NotThrow(()=>_auditor.Audit(ReadCode(contractDllPath), false));
            }
        }

        private byte[] ReadCode(string path)
        {
            return File.Exists(path) ? File.ReadAllBytes(path) : throw new FileNotFoundException("Contract DLL cannot be found. " + path);
        }
    }
}
