using System.Linq;
using AElf.Sdk.CSharp;

namespace AElf.Contracts.MultiToken
{
    public partial class TransactionFeeBill
    {
        public static TransactionFeeBill operator +(TransactionFeeBill bill1, TransactionFeeBill bill2)
        {
            var result = new TransactionFeeBill();
            foreach (var symbol in bill1.FeesMap.Keys.Union(bill2.FeesMap.Keys))
            {
                var amountInBill1 = bill1.FeesMap.ContainsKey(symbol) ? bill1.FeesMap[symbol] : 0;
                var amountInBill2 = bill2.FeesMap.ContainsKey(symbol) ? bill2.FeesMap[symbol] : 0;
                result.FeesMap.Add(symbol, amountInBill1.Add(amountInBill2));
            }

            return result;
        }
    }
}