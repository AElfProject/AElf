using System;
using System.Collections.Generic;
using System.IO;
using AElf.Kernel.CodeCheck.Infrastructure;
using Shouldly;
using Xunit;

namespace AElf.ContractDeployer
{
    public class ContractsDeployerTests : ContractDeployerTestBase
    {
        private readonly IContractPatcher _contractPatcher;

        public ContractsDeployerTests()
        {
            _contractPatcher = GetRequiredService<IContractPatcher>();
        }

        [Fact]
        public void GetContractCodes_WithoutContractManifest_Test()
        {
            Assert.Throws<NoContractDllFoundInManifestException>(() =>
                ContractsDeployer.GetContractCodes<ContractDeployerModule>());
        }
        
        [Fact]
        public void GetContractCodes_WithContractManifest_Test()
        {
            var codes = ContractsDeployer.GetContractCodes<ContractDeployerTestAElfModule>();
            
            CheckResult(codes);
        }
        
        [Fact]
        public void GetContractCodes_ContractFolder_Test()
        {
            var folder = "/NotExist";
            var codes = ContractsDeployer.GetContractCodes<ContractDeployerTestAElfModule>(folder);

            CheckResult(codes);

            folder = Environment.CurrentDirectory;
            codes = ContractsDeployer.GetContractCodes<ContractDeployerTestAElfModule>(folder);
            
            CheckResult(codes);
        }
        
        [Fact]
        public void GetContractCodes_PatchedContract_Test()
        {
            PatchContractCode("AElf.Contracts.MultiToken");
            PatchContractCode("AElf.Contracts.TokenConverter");

            var codes = ContractsDeployer.GetContractCodes<ContractDeployerTestAElfModule>(isPatched: true);

            CheckResult(codes);
        }

        private void CheckResult(IReadOnlyDictionary<string, byte[]> codes)
        {
            codes.Count.ShouldBe(2);
            codes["AElf.Contracts.MultiToken"].ShouldBe(GetContactCode("AElf.Contracts.MultiToken"));
            codes["AElf.Contracts.TokenConverter"].ShouldBe(GetContactCode("AElf.Contracts.TokenConverter"));
        }

        private byte[] GetContactCode(string name)
        {
            var path = Path.Combine(Environment.CurrentDirectory, name + ".dll");
            return File.ReadAllBytes(path);
        }

        private void PatchContractCode(string name)
        {
            var patchedCode = _contractPatcher.Patch(GetContactCode(name), true);
            var path = Path.Combine(Environment.CurrentDirectory, name + ".dll.patched");
            File.WriteAllBytes(path, patchedCode);
        }
    }
}