using System.Threading.Tasks;
using AElf.Database;
using AElf.Kernel.Types;

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
             var key = trKey.GetKeyString(TypeName.TnTransactionResult);
             await _keyValueDatabase.SetAsync(key, result.Serialize());
         }
 
         public async Task<TransactionResult> GetAsync(Hash trKey)
         {
             var key = trKey.GetKeyString(TypeName.TnTransactionResult);
             var txResultBytes = await _keyValueDatabase.GetAsync(key, typeof(TransactionResult));
             return txResultBytes == null ? null : TransactionResult.Parser.ParseFrom(txResultBytes);
         }
     }
 }