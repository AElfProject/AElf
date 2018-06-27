using System;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel.Concurrency.Metadata;
using AElf.Sdk.CSharp.Types;
using CSharpSmartContract = AElf.Sdk.CSharp.CSharpSmartContract;
using Api = AElf.Sdk.CSharp.Api;

namespace AElf.Kernel.CSharp.Tests
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
            Console.WriteLine("Initialize");
            await Balances.SetValueAsync(account, qty);
            return true;
        }
        
        
        public override async Task InvokeAsync()
        {

            // Not needed anymore. Keep here to comply with interface.

            await Task.CompletedTask;
        }

        public void SleepMilliseconds(int milliSeconds)
        {
            // Used to test timeout
            Thread.Sleep(milliSeconds);
        }

        public string NoAction()
        {
            // Don't delete, this is needed to test placeholder transactions
            var str = "NoAction";
            Console.WriteLine("NoAction");
            return str;
        }
        
        [SmartContractFunction("${this}.Transfer", new string[]{}, new []{"${this}.Balances", "${this}.TransactionStartTimes", "${this}.TransactionEndTimes"})]
        public async Task<bool> Transfer(Hash from, Hash to, ulong qty)
        {
            // This is for testing batched transaction sequence
            await TransactionStartTimes.SetValueAsync(Api.GetTransaction().GetHash(), Now());

            var fromBal = await Balances.GetValueAsync(from);

            var toBal = await Balances.GetValueAsync(to);

            Api.Assert(fromBal > qty);
            
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
            var b = await Balances.GetValueAsync(account);
            //Console.WriteLine(b);
            return b;
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

        public void Print(string name)
        {
            Console.WriteLine("Hello, {0}", name);
        }
    }
}