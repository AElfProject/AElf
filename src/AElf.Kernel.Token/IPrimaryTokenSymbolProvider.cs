using System.Threading.Tasks;

namespace AElf.Kernel.Token
{
    /// <summary>
    /// Primary token symbol is the token symbol used in validating tx sender's balance.
    /// </summary>
    public interface IPrimaryTokenSymbolProvider
    {
        void SetPrimaryTokenSymbol(string symbol);
        Task<string> GetPrimaryTokenSymbol();
    }

    public class DefaultPrimaryTokenSymbolProvider : IPrimaryTokenSymbolProvider
    {
        public void SetPrimaryTokenSymbol(string symbol)
        {
        }

        public Task<string> GetPrimaryTokenSymbol()
        {
            return Task.FromResult("ELF");
        }
    }
}