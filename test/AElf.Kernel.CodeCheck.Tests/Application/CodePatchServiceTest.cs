using System;
using System.Threading.Tasks;
using AElf.Kernel.CodeCheck.Application;
using AElf.Kernel.CodeCheck.Infrastructure;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace AElf.Kernel.CodeCheck.Tests;

public class CodePatchTest : CodeCheckTestBase
{
    private readonly ICodePatchService _codePatchService;
    
    public CodePatchTest()
    {
        _codePatchService = GetRequiredService<ICodePatchService>();
    }

    [Fact]
    public async Task PerformCodePatchAsync_Success_Test()
    {
        var code = new byte[10];
        var result = _codePatchService.PerformCodePatch(code, 0, false, out var patchedCode);
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task PerformCodePatchAsync_UnknownCategory_Test()
    {
        var code = new byte[10];
        var result = _codePatchService.PerformCodePatch(code, 1, false, out var patchedCode);
        result.ShouldBeFalse();
    }
}