using System.Threading.Tasks;
using AElf.Kernel.Token.Infrastructure;

namespace AElf.Kernel.Token
{
    public interface IPrimaryTokenSymbolService
    {
        void SetPrimaryTokenSymbol(string symbol);
        Task<string> GetPrimaryTokenSymbol();
    }

    public class DefaultPrimaryTokenSymbolService : IPrimaryTokenSymbolService
    {
        private readonly IPrimaryTokenSymbolProvider _primaryTokenSymbolProvider;

        public DefaultPrimaryTokenSymbolService(IPrimaryTokenSymbolProvider primaryTokenSymbolProvider)
        {
            _primaryTokenSymbolProvider = primaryTokenSymbolProvider;
        }

        public void SetPrimaryTokenSymbol(string symbol)
        {
            _primaryTokenSymbolProvider.SetPrimaryTokenSymbol(symbol);
        }

        public Task<string> GetPrimaryTokenSymbol()
        {
            return _primaryTokenSymbolProvider.GetPrimaryTokenSymbol();
        }
    }
}