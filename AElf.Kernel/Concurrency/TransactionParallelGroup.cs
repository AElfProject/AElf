using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;

namespace AElf.Kernel.Concurrency
{
    public class TransactionParallelGroup : ITransactionParallelGroup
    {
        private readonly Dictionary<Hash, List<ITransaction>> _accountTxsDict;
        private readonly bool _batched;
        private List<IBatch> _batches;
        private dynamic _accountListOrderedByTxSize;
        

        public TransactionParallelGroup()
        {
            _batched = false;
            _accountTxsDict = new Dictionary<Hash, List<ITransaction>>();
            _batches = new List<IBatch>();
        }

        public int GetSenderCount()
        {
            return _accountTxsDict.Count;
        }

        /// <summary>
        /// Get the tx list in this group, txList is empty if the sender is not found in this group
        /// </summary>
        /// <param name="sender"></param>
        /// <returns></returns>
        public List<ITransaction> GetAccountTxList(Hash sender)
        {
            if (!_accountTxsDict.TryGetValue(sender, out var accountTxList))
            {
                accountTxList = new List<ITransaction>();
            }
            
            return accountTxList;
        }

        /// <summary>
        /// Add new accountTxList into the group before start batching
        /// </summary>
        /// <returns>return true if succeed, false if this group is already batching</returns>
        public void AddAccountTxList(KeyValuePair<Hash, List<ITransaction>> kvPair)
        {
            if (!_batched)
            {
                _accountTxsDict.Add(kvPair.Key, kvPair.Value);
                
                for (int i = 0; i < kvPair.Value.Count; i++)
                {
                    IBatch batch = _batches.ElementAtOrDefault(i);
                    if (batch == null)
                    {
                        batch = new Batch();
                        _batches.Add(batch);
                    }
                    
                    batch.AddTransaction(kvPair.Value.ElementAt(i));
                }
            }
            else
            {
                throw new Exception("When try to add accountTxList to parallelGroup, the group already start batching");
            }
        }

        /// <summary>
        /// Return first tx's sender account, used to determine whether another sender account should merge into this group.
        /// </summary>
        /// <returns></returns>
        public Hash GetOneAccountInGroup()
        {
            if (_accountTxsDict.Count > 0)
            {
                return _accountTxsDict.First().Key;
            }
            else
            {
                return Hash.Zero;
            }
        }

        public List<Hash> GetSenderList()
        {
            return _accountTxsDict.Keys.ToList();
        }

        public List<ITransaction> GetNextUnScheduledTxBatch()
        {
            throw new NotImplementedException();
            var transactionBatch = new List<Transaction>();
            if (!_batched)
            {
                _accountListOrderedByTxSize = from txLists in _accountTxsDict
                    orderby txLists.Value.Count descending
                    select txLists;
            }
        }
        
        
    }
}