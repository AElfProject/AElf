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

namespace AElf.Runtime.CSharp.Tests
{
    public class TestContract : CSharpSmartContract
    {
        [SmartContractFieldData("${this}.Balances", DataAccessMode.AccountSpecific)]
        public MapToUInt64<Hash> Balances = new MapToUInt64<Hash>("Balances");
        
        [SmartContractFieldData("${this}.TransactionStartTimes", DataAccessMode.AccountSpecific)]
        public MapToString<Hash> TransactionStartTimes = new MapToString<Hash>("TransactionStartTimes");
        
        [SmartContractFieldData("${this}.TransactionEndTimes", DataAccessMode.AccountSpecific)]
        public MapToString<Hash> TransactionEndTimes = new MapToString<Hash>("TransactionEndTimes");

        [SmartContractFunction("${this}.InitializeAsync", new string[]{}, new []{"${this}.Balances"})]
        public async Task<bool> InitializeAsync(Hash account, ulong qty)
        {
            await Balances.SetValueAsync(account, qty);
            return true;
        }

        [SmartContractFunction("${this}.Transfer", new string[]{}, new []{"${this}.Balances", "${this}.TransactionStartTimes", "${this}.TransactionEndTimes"})]
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

        [SmartContractFunction("${this}.GetBalance", new string[]{}, new []{"${this}.Balances"})]
        public async Task<ulong> GetBalance(Hash account)
        {
            return await Balances.GetValueAsync(account);
        }

        [SmartContractFunction("${this}.GetTransactionStartTime", new string[]{}, new []{"${this}.TransactionStartTimes"})]
        public async Task<string> GetTransactionStartTime(Hash transactionHash)
        {
            var startTime = await TransactionStartTimes.GetValueAsync(transactionHash);
            return startTime;
        }

        [SmartContractFunction("${this}.GetTransactionEndTime", new string[]{}, new []{"${this}.TransactionEndTimes"})]
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