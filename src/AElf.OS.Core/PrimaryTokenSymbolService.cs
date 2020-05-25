using System.Threading.Tasks;
using AElf.Kernel.Token;
using Microsoft.Extensions.Options;

namespace AElf.OS
{
    public class PrimaryTokenSymbolService : IPrimaryTokenSymbolService
    {
        private readonly EconomicOptions _economicOptions;

        public PrimaryTokenSymbolService(IOptionsSnapshot<EconomicOptions> economicOptions)
        {
            _economicOptions = economicOptions.Value;
        }

        public void SetPrimaryTokenSymbol(string symbol)
        {
        }

        public Task<string> GetPrimaryTokenSymbol()
        {
            return Task.FromResult(_economicOptions.Symbol);
        }
    }
}