using AElf.Kernel.Token.Infrastructure;
using Shouldly;
using Xunit;

namespace AElf.Kernel.Token.Test
{
    public partial class KernelTokenTest : KernelTokenTestBase
    {
        private readonly IPrimaryTokenSymbolProvider _primaryTokenSymbolProvider;
        private readonly IPrimaryTokenSymbolService _primaryTokenSymbolService;

        public KernelTokenTest()
        {
            _primaryTokenSymbolProvider = GetRequiredService<IPrimaryTokenSymbolProvider>();
            _primaryTokenSymbolService = GetRequiredService<IPrimaryTokenSymbolService>();
        }

        [Fact]
        public void PrimaryTokenSymbolProvider_Test()
        {
            var primaryToken = "ELF";
            _primaryTokenSymbolProvider.SetPrimaryTokenSymbol(primaryToken);
            var storedToken = _primaryTokenSymbolProvider.GetPrimaryTokenSymbol();
            storedToken.ShouldBe(primaryToken);

            var newToken = "ALICE";
            _primaryTokenSymbolProvider.SetPrimaryTokenSymbol(newToken);
            storedToken = _primaryTokenSymbolProvider.GetPrimaryTokenSymbol();
            storedToken.ShouldBe(newToken);
        }
    }
}