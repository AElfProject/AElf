using System;
using System.Collections.Generic;
using System.Linq;

namespace AElf.Kernel.Concurrency
{
    public class TransactionParallelGroup
    {
        protected Dictionary<Hash, List<Transaction>> _accountTxsDict;
        private int _currentScheduleBatch;
        private dynamic _accountListOrderedByTxSize;
        

        public TransactionParallelGroup()
        {
            _currentScheduleBatch = 0;
            _accountTxsDict = new Dictionary<Hash, List<Transaction>>();
        }

        public int Count => _accountTxsDict.Count;

        /// <summary>
        /// Get the tx list in this group, txList is empty if the sender is not found in this group
        /// </summary>
        /// <param name="sender"></param>
        /// <returns></returns>
        public List<Transaction> GetAccountTxList(Hash sender)
        {
            if (!_accountTxsDict.TryGetValue(sender, out var accountTxList))
            {
                accountTxList = new List<Transaction>();
            }

            return accountTxList;
        }

        /// <summary>
        /// Add new accountTxList into the group before start batching
        /// </summary>
        /// <param name="account"></param>
        /// <param name="txsSentByAccount"></param>
        /// <returns>return true if succeed, false if this group is already batching</returns>
        public bool AddAccountTxList(KeyValuePair<Hash, List<Transaction>> kvPair)
        {
            if (_currentScheduleBatch == 0)
            {
                _accountTxsDict.Add(kvPair.Key, kvPair.Value);
                return true;
            }
            else
            {
                return false;
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

        public List<Transaction> GetNextUnScheduledTxBatch()
        {
            throw new NotImplementedException();
            var transactionBatch = new List<Transaction>();
            if (_currentScheduleBatch == 0)
            {
                _accountListOrderedByTxSize = from txLists in _accountTxsDict
                    orderby txLists.Value.Count descending
                    select txLists;
            }

            _currentScheduleBatch++;

            foreach (Hash account in _accountListOrderedByTxSize)
            {
                if (_accountTxsDict[account].Count <= _currentScheduleBatch)
                {
                }
            }
        }
        
        
    }
}