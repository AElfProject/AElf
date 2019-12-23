using System.Linq;
using AElf.Sdk.CSharp;

namespace AElf.Contracts.Configuration
{
    public partial class ResourceTokenAmount
    {
        public static ResourceTokenAmount operator +(ResourceTokenAmount amount1, ResourceTokenAmount amount2)
        {
            var result = new ResourceTokenAmount();
            foreach (var symbol in amount1.Value.Keys.Union(amount2.Value.Keys))
            {
                var amountInBill1 = amount1.Value.ContainsKey(symbol) ? amount1.Value[symbol] : 0;
                var amountInBill2 = amount2.Value.ContainsKey(symbol) ? amount2.Value[symbol] : 0;
                result.Value.Add(symbol, amountInBill1.Add(amountInBill2));
            }

            return result;
        }

        public static ResourceTokenAmount operator -(ResourceTokenAmount amount1, ResourceTokenAmount amount2)
        {
            var result = new ResourceTokenAmount();
            foreach (var symbol in amount1.Value.Keys.Union(amount2.Value.Keys))
            {
                var amountInBill1 = amount1.Value.ContainsKey(symbol) ? amount1.Value[symbol] : 0;
                var amountInBill2 = amount2.Value.ContainsKey(symbol) ? amount2.Value[symbol] : 0;
                result.Value.Add(symbol, amountInBill1.Sub(amountInBill2));
            }

            return result;
        }
    }
}