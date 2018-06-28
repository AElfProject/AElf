using System;
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

        [SmartContractFunction("${this}.Initialize", new string[]{}, new []{"${this}.Balances"})]
        public bool Initialize(Hash account, ulong qty)
        {
            Console.WriteLine("Initialize");
            Balances.SetValue(account, qty);
            return true;
        }

        public void SleepMilliseconds(int milliSeconds)
        {
            // Used to test timeout
            Api.Sleep(milliSeconds);
        }

        public string NoAction()
        {
            // Don't delete, this is needed to test placeholder transactions
            var str = "NoAction";
            Console.WriteLine("NoAction");
            return str;
        }
        
        [SmartContractFunction("${this}.Transfer", new string[]{}, new []{"${this}.Balances", "${this}.TransactionStartTimes", "${this}.TransactionEndTimes"})]
        public bool Transfer(Hash from, Hash to, ulong qty)
        {
            // This is for testing batched transaction sequence
            TransactionStartTimes.SetValue(Api.GetTransaction().GetHash(), Now());

            var fromBal = Balances.GetValue(from);

            var toBal = Balances.GetValue(to);

            Api.Assert(fromBal > qty);
            
            var newFromBal = fromBal - qty;
            var newToBal = toBal + qty;
            Balances.SetValue(from, newFromBal);
            Balances.SetValue(to, newToBal);

            // This is for testing batched transaction sequence
            TransactionEndTimes.SetValue(Api.GetTransaction().GetHash(), Now());
            return true;
        }

        [SmartContractFunction("${this}.GetBalance", new string[]{}, new []{"${this}.Balances"})]
        public ulong GetBalance(Hash account)
        {
            var b = Balances.GetValue(account);
            //Console.WriteLine(b);
            return b;
        }

        [SmartContractFunction("${this}.GetTransactionStartTime", new string[]{}, new []{"${this}.TransactionStartTimes"})]
        public string GetTransactionStartTime(Hash transactionHash)
        {
            var startTime = TransactionStartTimes.GetValue(transactionHash);
            return startTime;
        }

        [SmartContractFunction("${this}.GetTransactionEndTime", new string[]{}, new []{"${this}.TransactionEndTimes"})]
        public string GetTransactionEndTime(Hash transactionHash)
        {
            var endTime = TransactionEndTimes.GetValue(transactionHash);
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