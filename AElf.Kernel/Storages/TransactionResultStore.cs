using System.Threading.Tasks;
using AElf.Database;
using AElf.Kernel.Extensions;
 using Google.Protobuf;
 
 namespace AElf.Kernel.Storages
 {
     public class TransactionResultStore : ITransactionResultStore
     {
         private readonly IKeyValueDatabase _keyValueDatabase;
 
         public TransactionResultStore(IKeyValueDatabase keyValueDatabase)
         {
             _keyValueDatabase = keyValueDatabase;
         }
 
         public async Task InsertAsync(TransactionResult result)
         {
             Hash hash = result.CalculateHash();
             await _keyValueDatabase.SetAsync(hash.Value.ToBase64(), result.Serialize());
         }
 
         public async Task<TransactionResult> GetAsync(Hash hash)
         {
             var txResultBytes = await _keyValueDatabase.GetAsync(hash.Value.ToBase64(), typeof(TransactionResult));
             return TransactionResult.Parser.ParseFrom(txResultBytes);
         }
     }
 }