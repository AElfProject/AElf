using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Types;
using AElf.Sdk.CSharp;
using AElf.Sdk.CSharp.Types;
using AElf.Types.CSharp;

namespace AElf.ABI.CSharp.Tests
{
    public class UserBaseContract : CSharpSmartContract, IStandard
    {
        private UInt32Field _totalSupply = new UInt32Field("_totalSupply");
        private MapToUInt32<Hash> _balances = new MapToUInt32<Hash>("_balances");

        public override async Task InvokeAsync()
        {
            await Task.CompletedTask;
        }

        public uint GetTotalSupply()
        {
            return 100;
        }

        public uint GetBalanceOf(Hash account)
        {
            var task = _balances.GetValueAsync(account);
            task.Wait();
            return task.Result;
        }
        public bool Transfer(Hash to, uint value)
        {
            return true;
        }
    }
}
