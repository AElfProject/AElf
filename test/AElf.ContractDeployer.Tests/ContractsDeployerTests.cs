using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AElf.Kernel.CodeCheck.Infrastructure;
using AElf.Kernel.SmartContract;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace AElf.ContractDeployer;

public class ContractsDeployerTests : ContractDeployerTestBase
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly IContractPatcher _contractPatcher;
    private readonly IContractAuditor _contractAuditor;

    public ContractsDeployerTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _contractPatcher = GetRequiredService<IContractPatcher>();
        _contractAuditor = GetRequiredService<IContractAuditor>();
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

    [Fact]
    public void Test1()
    {

        var code1 = GetContactCode("AElf.Contracts.TokenConverter");
        var code2 = GetContactCode("AElf.Contracts.MultiToken");

        _testOutputHelper.WriteLine(DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss-fff"));
        var patchedCode = _contractPatcher.Patch(code2, false);
        _contractAuditor.Audit(patchedCode,
            new RequiredAcs { AcsList = new List<string>(), RequireAll = false }, false);
        _testOutputHelper.WriteLine(DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss-fff"));
    }

    [Fact]
    public void Test()
    {
        
        var code1 = GetContactCode("AElf.Contracts.TokenConverter");
        var code2 = GetContactCode("AElf.Contracts.MultiToken");

        Parallel.For(0, 1, i =>
        {
            try
            {
                var code = i % 2 == 0 ? code1 : code1;
                var patchedCode = _contractPatcher.Patch(code, false);
                _contractAuditor.Audit(patchedCode, new RequiredAcs{AcsList = new List<string>(), RequireAll = false},false);
                _testOutputHelper.WriteLine(i.ToString());
            }
            catch (Exception e)
            {
                _testOutputHelper.WriteLine("Failed: " + i);
                throw;
            }
        });
        
        _testOutputHelper.WriteLine("=================");

        // for (int i = 0; i < 10; i++)
        // {
        //     try
        //     {
        //         var patchedCode = _contractPatcher.Patch(GetContactCode("AElf.Contracts.TokenConverter"), false);
        //         _contractAuditor.Audit(patchedCode, new RequiredAcs{AcsList = new List<string>(), RequireAll = false},false);
        //         _testOutputHelper.WriteLine(i.ToString());
        //     }
        //     catch (Exception e)
        //     {
        //         _testOutputHelper.WriteLine("Failed: "+i.ToString());Data = ListDictionaryInternal 
        //     }
        // }
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
        var patchedCode = _contractPatcher.Patch(GetContactCode(name), false);
        var path = Path.Combine(Environment.CurrentDirectory, name + ".dll.patched");
        File.WriteAllBytes(path, patchedCode);
    }
}