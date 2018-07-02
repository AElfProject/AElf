﻿using System.Threading.Tasks;
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
             await _keyValueDatabase.SetAsync(trKey.Value.ToByteArray().ToHex(), result.Serialize());
         }
 
         public async Task<TransactionResult> GetAsync(Hash hash)
         {
             var txResultBytes = await _keyValueDatabase.GetAsync(hash.Value.ToByteArray().ToHex(), typeof(TransactionResult));
             return txResultBytes == null ? null : TransactionResult.Parser.ParseFrom(txResultBytes);
         }
     }
 }