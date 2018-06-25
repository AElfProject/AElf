using System;
using System.Text;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Concurrency.Metadata;
using AElf.Sdk.CSharp;
using AElf.Sdk.CSharp.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using CSharpSmartContract = AElf.Sdk.CSharp.CSharpSmartContract;
using Api = AElf.Sdk.CSharp.Api;

namespace AElf.Kernel.Tests.Concurrency.Metadata.TestContracts
{
    public class SimpleTokenContract : CSharpSmartContract
    {
        [SmartContractFieldData("${this}.Balances", DataAccessMode.AccountSpecific)]
        public MapToUInt64<Hash> Balances = new MapToUInt64<Hash>("Balances");

        public MapToString<Hash> TransactionStartTimes = new MapToString<Hash>("TransactionStartTimes");
        public MapToString<Hash> TransactionEndTimes = new MapToString<Hash>("TransactionEndTimes");

        public async Task<bool> InitializeAsync(Hash account, ulong qty)
        {
            await Balances.SetValueAsync(account, qty);
            return true;
        }

        public override async Task InvokeAsync()
        {

            // Not needed anymore. Keep here to comply with interface.

            await Task.CompletedTask;
        }

        public async Task<bool> Transfer(Hash from, Hash to, ulong qty)
        {
            // This is for testing batched transaction sequence
            await TransactionStartTimes.SetValueAsync(Api.GetTransaction().GetHash(), Now());

            var fromBal = await Balances.GetValueAsync(from);

            var toBal = await Balances.GetValueAsync(to);

            var newFromBal = fromBal - qty;
            var newToBal = toBal + qty;
            await Balances.SetValueAsync(from, newFromBal);
            await Balances.SetValueAsync(to, newToBal);

            // This is for testing batched transaction sequence
            await TransactionEndTimes.SetValueAsync(Api.GetTransaction().GetHash(), Now());
            return true;
        }

        public async Task<ulong> GetBalance(Hash account)
        {
            return await Balances.GetValueAsync(account);
        }

        public async Task<string> GetTransactionStartTime(Hash transactionHash)
        {
            var startTime = await TransactionStartTimes.GetValueAsync(transactionHash);
            return startTime;
        }

        public async Task<string> GetTransactionEndTime(Hash transactionHash)
        {
            var endTime = await TransactionEndTimes.GetValueAsync(transactionHash);
            return endTime;
        }

        private string Now()
        {
            var dtStr = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffffff");
            return dtStr;
        }
    }
}