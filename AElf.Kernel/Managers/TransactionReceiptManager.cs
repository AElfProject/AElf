using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Database;
using AElf.Kernel.Storages;
using AElf.Kernel.Types;
using Google.Protobuf;

namespace AElf.Kernel.Managers
{
    public class TransactionReceiptManager : ITransactionReceiptManager
    {
        private readonly IKeyValueDatabase _database;

        public TransactionReceiptManager(IKeyValueDatabase database)
        {
            _database = database;
        }

        private static string GetKey(Hash txId)
        {
            return $"{GlobalConfig.TransactionReceiptPrefix}{txId.DumpHex()}";
        }

        public async Task AddOrUpdateReceiptAsync(TransactionReceipt receipt)
        {
            await _database.SetAsync(GetKey(receipt.TransactionId), receipt.ToByteArray());
        }

        public async Task AddOrUpdateReceiptsAsync(IEnumerable<TransactionReceipt> receipts)
        {
            var dict = receipts.ToDictionary(r => GetKey(r.TransactionId), r => r.ToByteArray());
            await _database.PipelineSetAsync(dict);
        }

        public async Task<TransactionReceipt> GetReceiptAsync(Hash txId)
        {
            var res = await _database.GetAsync(GetKey(txId));
            return res?.Deserialize<TransactionReceipt>();
        }
    }
}