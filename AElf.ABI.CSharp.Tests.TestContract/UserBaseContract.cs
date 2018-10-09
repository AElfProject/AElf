using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Types;
using AElf.Sdk.CSharp;
using AElf.Sdk.CSharp.Types;
using AElf.Types.CSharp;
using AElf.Common;

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
