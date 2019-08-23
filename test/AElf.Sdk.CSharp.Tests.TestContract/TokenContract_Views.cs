using System.Linq;
using AElf.Types;

namespace AElf.Sdk.CSharp.Tests.TestContract
{
    public partial class TokenContract
    {
        [View]
        public string Symbol()
        {
            return State.TokenInfo.Symbol.Value;
        }

        [View]
        public string TokenName()
        {
            return State.TokenInfo.TokenName.Value;
        }

        [View]
        public ulong TotalSupply()
        {
            return State.TokenInfo.TotalSupply.Value;
        }

        [View]
        public uint Decimals()
        {
            return State.TokenInfo.Decimals.Value;
        }

        [View]
        public ulong BalanceOf(Address owner)
        {
            return State.Balances[owner];
        }

        [View]
        public ulong Allowance(Address owner, Address spender)
        {
            return State.Allowances[owner][spender];
        }


        [View]
        public Hash GetVirtualAddressHash(int n)
        {
            return
                Hash.FromRawBytes(Context.Sender.Value.Concat(n.DumpByteArray()).ToArray().ComputeHash());
        }

        [View]
        public Address GetVirtualAddress(int n)
        {
            return Context.ConvertVirtualAddressToContractAddress(GetVirtualAddressHash(n));
        }
    }
}