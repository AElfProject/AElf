using AElf.Sdk.CSharp;
using AElf.Sdk.CSharp.Types;
using AElf.Common;

// ReSharper disable once CheckNamespace
namespace AElf.ABI.CSharp.Tests
{
    public class UserBaseContract : CSharpSmartContract, IStandard
    {
        private UInt32Field _totalSupply = new UInt32Field("_totalSupply");
        private MapToUInt32<Address> _balances = new MapToUInt32<Address>("_balances");

        [View]
        public uint GetTotalSupply()
        {
            return 100;
        }

        [View]
        public uint GetBalanceOf(Address account)
        {
            var task = _balances.GetValueAsync(account);
            task.Wait();
            return task.Result;
        }
        public bool Transfer(Address to, uint value)
        {
            return true;
        }
    }
}
