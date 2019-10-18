using System.Threading.Tasks;

namespace AElf.Blockchains.SideChain
{
    public interface IPrimaryTokenSymbolDiscoveryService
    {
        Task<string> GetPrimaryTokenSymbol();
    }
}