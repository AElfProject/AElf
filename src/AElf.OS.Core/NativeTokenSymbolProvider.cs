using AElf.Kernel.Token;
using Microsoft.Extensions.Options;

namespace AElf.OS
{
    public class NativeTokenSymbolProvider : INativeTokenSymbolProvider
    {
        private readonly EconomicOptions _economicOptions;

        public NativeTokenSymbolProvider(IOptionsSnapshot<EconomicOptions> economicOptions)
        {
            _economicOptions = economicOptions.Value;
        }

        public string GetNativeTokenSymbol()
        {
            return _economicOptions.Symbol;
        }
    }
}