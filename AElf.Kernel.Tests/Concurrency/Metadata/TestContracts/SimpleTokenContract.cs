using System;
using AElf.Sdk.CSharp.Types;
using AElf.Types.CSharp.MetadataAttribute;
using CSharpSmartContract = AElf.Sdk.CSharp.CSharpSmartContract;
using Api = AElf.Sdk.CSharp.Api;
using AElf.Common;

namespace AElf.Kernel.Tests.Concurrency.Metadata.TestContracts
{
    public class SimpleTokenContract : CSharpSmartContract
    {
        [SmartContractFieldData("${this}.Balances", DataAccessMode.AccountSpecific)]
        public MapToUInt64<Address> Balances = new MapToUInt64<Address>("Balances");

        public MapToString<Hash> TransactionStartTimes = new MapToString<Hash>("TransactionStartTimes");
        public MapToString<Hash> TransactionEndTimes = new MapToString<Hash>("TransactionEndTimes");

        public bool Initialize(Address account, ulong qty)
        {
            Balances.SetValue(account, qty);
            return true;
        }

        public bool Transfer(Address from, Address to, ulong qty)
        {
            // This is for testing batched transaction sequence
            TransactionStartTimes.SetValue(Api.GetTransaction().GetHash(), Now());

            var fromBal = Balances.GetValue(from);

            var toBal = Balances.GetValue(to);

            var newFromBal = fromBal - qty;
            var newToBal = toBal + qty;
            Balances.SetValue(from, newFromBal);
            Balances.SetValue(to, newToBal);

            // This is for testing batched transaction sequence
            TransactionEndTimes.SetValue(Api.GetTransaction().GetHash(), Now());
            return true;
        }

        public ulong GetBalance(Address account)
        {
            return Balances.GetValue(account);
        }

        public string GetTransactionStartTime(Hash transactionHash)
        {
            var startTime = TransactionStartTimes.GetValue(transactionHash);
            return startTime;
        }

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
    }
}