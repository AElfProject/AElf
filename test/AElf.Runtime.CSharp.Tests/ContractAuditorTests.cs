using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using AElf.Contracts.AssociationAuth;
using AElf.Contracts.Consensus.DPoS;
using AElf.Contracts.CrossChain;
using AElf.Contracts.Genesis;
using AElf.Contracts.MultiToken;
using Shouldly;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace AElf.Runtime.CSharp.Tests
{
    public class ContractAuditorTests : CSharpRuntimeTestBase
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private ContractAuditor _auditor;

        public ContractAuditorTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
            _auditor = new ContractAuditor();
        }
        
        [Fact]
        public void CodeCheck_TestContract()
        {
            var code = ReadCode(typeof(TokenContract).Assembly.Location);

            Should.NotThrow(()=>_auditor.Audit(code, false));
        }

        [Fact]
        public void CodeCheck_SystemContracts()
        {
            var contracts = new[]
            {
                typeof(BasicContractZero).Module.ToString(),
            };

            foreach (var contract in contracts)
            {
                var contractDllPath = "../../../../../contracts/" + contract;
                
                using (FileStream fs = new FileStream(contractDllPath, FileMode.Open))
                using (BufferedStream bs = new BufferedStream(fs))
                {
                    using (SHA1Managed sha1 = new SHA1Managed())
                    {
                        byte[] hash = sha1.ComputeHash(bs);
                        var formatted = new StringBuilder(2 * hash.Length);
                        foreach (byte b in hash)
                        {
                            formatted.AppendFormat("{0:X2}", b);
                        }
                        
                        _testOutputHelper.WriteLine("DLL Hash: " + formatted);
                        Console.WriteLine("DLL Hash: " + formatted);
                    }
                }
    
                Should.NotThrow(()=>_auditor.Audit(ReadCode(contractDllPath), false));
            }
        }

        private byte[] ReadCode(string path)
        {
            return File.Exists(path) ? File.ReadAllBytes(path) : null;
        }
    }
}
