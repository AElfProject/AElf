using System;
using System.Threading.Tasks;
using AElf.Kernel.CodeCheck.Application;
using AElf.Kernel.CodeCheck.Infrastructure;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace AElf.Kernel.CodeCheck.Tests;

public partial class CodeCheckTest : CodeCheckTestBase
{
    private readonly CodeCheckOptions _codeCheckOptions;
    private readonly ICodeCheckService _codeCheckService;
    private readonly IContractAuditorContainer _contractAuditorContainer;
    private readonly IRequiredAcsProvider _requiredAcsProvider;


    public CodeCheckTest()
    {
        _codeCheckService = GetRequiredService<ICodeCheckService>();
        _codeCheckOptions = GetRequiredService<IOptionsMonitor<CodeCheckOptions>>().CurrentValue;
        _contractAuditorContainer = GetRequiredService<IContractAuditorContainer>();
        _requiredAcsProvider = GetRequiredService<IRequiredAcsProvider>();
    }

    [Fact]
    public async Task PerformCodeCheckAsync_Without_CodeCheckEnable_Test()
    {
        _codeCheckOptions.CodeCheckEnabled = false;
        var result = await _codeCheckService.PerformCodeCheckAsync(null, null, 0, 0, false, false);
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task PerformCodeCheckAsync_With_Invalid_Category_Test()
    {
        var result = await _codeCheckService.PerformCodeCheckAsync(null, null, 0, -1, false,false);
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task PerformCodeCheckAsync_With_Audit_Fail_Test()
    {
        var result = await _codeCheckService.PerformCodeCheckAsync(null, null, 0, CodeCheckConstant.FailAudit, false,false);
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task PerformCodeCheckAsync_Success_Test()
    {
        var result =
            await _codeCheckService.PerformCodeCheckAsync(null, null, 0, CodeCheckConstant.SuccessAudit, false,false);
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task PerformCodePatchAsync_Success_Test()
    {
        var code = new byte[10];
        var patchedCode = await _codeCheckService.PerformCodePatchAsync(code, 0, false);
        patchedCode.ShouldBe(code);
    }

    [Fact]
    public async Task PerformCodePatchAsync_UnknownCategory_Test()
    {
        var code = new byte[10];
        await Assert.ThrowsAsync<Exception>(async () => await _codeCheckService.PerformCodePatchAsync(code, 1, false));
    }
}