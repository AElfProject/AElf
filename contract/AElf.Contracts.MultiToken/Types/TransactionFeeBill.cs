using System.Linq;
using AElf.Sdk.CSharp;

namespace AElf.Contracts.MultiToken
{
    public partial class TransactionFeeBill
    {
        public static TransactionFeeBill operator +(TransactionFeeBill bill1, TransactionFeeBill bill2)
        {
            var result = new TransactionFeeBill();
            foreach (var symbol in bill1.TokenToAmount.Keys.Union(bill2.TokenToAmount.Keys))
            {
                var amountInBill1 = bill1.TokenToAmount.ContainsKey(symbol) ? bill1.TokenToAmount[symbol] : 0;
                var amountInBill2 = bill2.TokenToAmount.ContainsKey(symbol) ? bill1.TokenToAmount[symbol] : 0;
                result.TokenToAmount.Add(symbol, amountInBill1.Add(amountInBill2));
            }

            return result;
        }
    }
}