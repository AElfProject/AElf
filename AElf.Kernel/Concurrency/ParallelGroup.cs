using System;
using System.Collections.Generic;
using System.Linq;

namespace AElf.Kernel.Concurrency
{
    public class ParallelGroup : IParallelGroup
    {
        private readonly Dictionary<Hash, List<ITransaction>> _accountTxsDict;
        private readonly bool _batched; //TODO: is this bool flag really needed?
        private readonly List<IBatch> _batches;
        

        public ParallelGroup()
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

        public List<Hash> GetSenderList()
        {
            return _accountTxsDict.Keys.ToList();
        }
        
        public List<IBatch> Batches => _batches;
    }
}