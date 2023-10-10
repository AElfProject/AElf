using Epoche;

namespace AElf.Runtime.WebAssembly.Extensions;

public static class StringExtensions
{
    public static string ToSelector(this string functionName, bool prefixOx = false)
    {
        return Keccak256.ComputeEthereumFunctionSelector(functionName, prefixOx);
    }
}