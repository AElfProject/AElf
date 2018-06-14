using System;
using System.Text;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Concurrency.Metadata;
using AElf.Kernel.Extensions;
using AElf.Sdk.CSharp;
using AElf.Sdk.CSharp.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using CSharpSmartContract = AElf.Sdk.CSharp.CSharpSmartContract;
using Api = AElf.Sdk.CSharp.Api;

namespace AElf.Contracts.Examples
{
    public class SimpleTokenContract : CSharpSmartContract
    {
        [SmartContractFieldData("${this}.Balances", DataAccessMode.AccountSpecific)]
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
            //await (Task<object>)member.Invoke(this, parameters);
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
            Api.Return(new UInt64Value() { Value = balBytes.ToUInt64() });
            return balBytes.ToUInt64();
        }

        public async Task<object> GetTransactionStartTime(Hash transactionHash)
        {
            var startTime = await TransactionStartTimes.GetValue(transactionHash);
            Api.Return(new BytesValue() { Value = ByteString.CopyFrom(startTime) });
            return startTime;
        }

        public async Task<object> GetTransactionEndTime(Hash transactionHash)
        {
            var endTime = await TransactionEndTimes.GetValue(transactionHash);
            Api.Return(new BytesValue() { Value = ByteString.CopyFrom(endTime) });
            return endTime;
        }

        private byte[] Now()
        {
            var dtStr = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffffff");
            return Encoding.UTF8.GetBytes(dtStr);
        }
    }
}