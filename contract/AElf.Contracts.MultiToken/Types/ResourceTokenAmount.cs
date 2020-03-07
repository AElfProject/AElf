using System.Linq;
using AElf.Sdk.CSharp;

namespace AElf.Contracts.MultiToken
{
    public partial class ResourceTokenAmount
    {
        public static ResourceTokenAmount operator +(ResourceTokenAmount amount1, ResourceTokenAmount amount2)
        {
            var result = new ResourceTokenAmount();
            foreach (var symbol in amount1.Value.Keys.Union(amount2.Value.Keys))
            {
                var symbolAmount1 = amount1.Value.ContainsKey(symbol) ? amount1.Value[symbol] : 0;
                var symbolAmount2 = amount2.Value.ContainsKey(symbol) ? amount2.Value[symbol] : 0;
                result.Value.Add(symbol, symbolAmount1.Add(symbolAmount2));
            }

            return result;
        }

        public static ResourceTokenAmount operator -(ResourceTokenAmount amount1, ResourceTokenAmount amount2)
        {
            var result = new ResourceTokenAmount();
            foreach (var symbol in amount1.Value.Keys.Union(amount2.Value.Keys))
            {
                var symbolAmount1 = amount1.Value.ContainsKey(symbol) ? amount1.Value[symbol] : 0;
                var symbolAmount2 = amount2.Value.ContainsKey(symbol) ? amount2.Value[symbol] : 0;
                result.Value.Add(symbol, symbolAmount1.Sub(symbolAmount2));
            }

            return result;
        }
    }
}