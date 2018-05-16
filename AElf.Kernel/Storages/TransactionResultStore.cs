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
 
         public async Task InsertAsync(Hash trKey, TransactionResult result)
         {
             await _keyValueDatabase.SetAsync(trKey.Value.ToBase64(), result.Serialize());
         }
 
         public async Task<TransactionResult> GetAsync(Hash hash)
         {
             var txResultBytes = await _keyValueDatabase.GetAsync(hash.Value.ToBase64(), typeof(TransactionResult));
             return txResultBytes == null ? null : TransactionResult.Parser.ParseFrom(txResultBytes);
         }
     }
 }