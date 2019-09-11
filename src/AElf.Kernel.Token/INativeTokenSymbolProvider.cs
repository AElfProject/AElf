using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.Token
{
    public interface INativeTokenSymbolProvider
    {
        string GetNativeTokenSymbol();
    }

    public class DefaultNativeTokenSymbolProvider : INativeTokenSymbolProvider
    {
        public string GetNativeTokenSymbol()
        {
            return "ELF";
        }
    }
}