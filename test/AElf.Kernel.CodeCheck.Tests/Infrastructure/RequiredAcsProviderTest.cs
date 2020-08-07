using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace AElf.Kernel.CodeCheck.Tests
{
    public partial class CodeCheckTest
    {
        [Fact]
        public async Task GetRequiredAcsInContractsAsync_Test()
        {
            var requireAcsConfiguration = await _requiredAcsProvider.GetRequiredAcsInContractsAsync(null, 0);
            requireAcsConfiguration.RequireAll.ShouldBe(CodeCheckConstant.IsRequireAllAcs);
            requireAcsConfiguration.AcsList.Count.ShouldBe(2);
            requireAcsConfiguration.AcsList.Contains(CodeCheckConstant.Acs1).ShouldBeTrue();
            requireAcsConfiguration.AcsList.Contains(CodeCheckConstant.Acs2).ShouldBeTrue();
        }
    }
}