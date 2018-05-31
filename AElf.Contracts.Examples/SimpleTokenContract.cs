using System;
using System.Text;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Extensions;
using AElf.Sdk.CSharp;
using AElf.Sdk.CSharp.Types;
using CSharpSmartContract = AElf.Sdk.CSharp.CSharpSmartContract;

namespace AElf.Contracts.Examples
{
    public class SimpleTokenContract : CSharpSmartContract
    {
        public Map Balances = new Map("Balances");
        public Map TransactionStartTimes = new Map("TransactionStartTimes");
        public Map TransactionEndTimes = new Map("TransactionEndTimes");

        public async Task<object> InitializeAsync(Hash account, ulong qty)
        {
            await Balances.SetValueAsync(account, qty.ToBytes());
            return null;
        }

        public override async Task InvokeAsync()
        {
            var tx = Api.GetTransaction();

            var methodname = tx.MethodName;
            var type = GetType();
            var member = type.GetMethod(methodname);
            // params array
            var parameters = Parameters.Parser.ParseFrom(tx.Params).Params.Select(p => p.Value()).ToArray();

            // invoke
            await (Task<object>)member.Invoke(this, parameters);
        }

        public async Task<object> Transfer(Hash from, Hash to, ulong qty)
        {
            // This is for testing batched transaction sequence
            await TransactionStartTimes.SetValueAsync(Api.GetTransaction().GetHash(), Now());

            var fromBalBytes = await Balances.GetValue(from);
            var fromBal = fromBalBytes.ToUInt64();
            var toBalBytes = await Balances.GetValue(to);
            var toBal = toBalBytes.ToUInt64();
            var newFromBal = fromBal - qty;
            var newToBal = toBal + qty;
            await Balances.SetValueAsync(from, newFromBal.ToBytes());
            await Balances.SetValueAsync(to, newToBal.ToBytes());

            // This is for testing batched transaction sequence
            await TransactionEndTimes.SetValueAsync(Api.GetTransaction().GetHash(), Now());
            return null;
        }

        public async Task<object> GetBalance(Hash account)
        {
            var balBytes = await Balances.GetValue(account);
            Api.LogToResult(balBytes);
            return balBytes.ToUInt64();
        }

        public async Task<object> GetTransactionStartTime(Hash transactionHash)
        {
            var startTime = await TransactionStartTimes.GetValue(transactionHash);
            Api.LogToResult(startTime);
            return null;
        }

        public async Task<object> GetTransactionEndTime(Hash transactionHash)
        {
            var endTime = await TransactionEndTimes.GetValue(transactionHash);
            Api.LogToResult(endTime);
            return null;
        }

        private byte[] Now()
        {
            var dtStr = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffffff");
            return Encoding.ASCII.GetBytes(dtStr);
        }
    }
}