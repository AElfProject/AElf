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

        public bool Initialize(Hash account, ulong qty)
        {
            Balances.SetValue(account, qty);
            return true;
        }

        public bool Transfer(Hash from, Hash to, ulong qty)
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

        public ulong GetBalance(Hash account)
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