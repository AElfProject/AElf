using System;
using System.Collections.Generic;
using System.Linq;

namespace AElf.Kernel
{
    [Serializable]
    public class BlockBody : IBlockBody
    {
        private List<ITransaction> _transactions { get; set; } = new List<ITransaction>();
        private MerkleTree<IAccount> _stateMerkleTreeRightChild = new MerkleTree<IAccount>();

        public int TransactionsCount
        {
            get
            {
                return _transactions.Count;
            }
        }

        public BlockBody() { }

        public IQueryable<ITransaction> GetTransactions()
        {
            return _transactions.AsQueryable();
        }

        public MerkleTree<IAccount> GetChangedWorldState()
        {
            return _stateMerkleTreeRightChild;
        }

        public bool AddTransaction(ITransaction tx)
        {
            //Avoid duplication of addition.
            if (_transactions.Exists(t => t.GetHash() == tx.GetHash()))
            {
                return false;
            }
            _transactions.Add(tx);
            return true;
        }

        /// <summary>
        /// For one account, just update once its state.
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        public bool AddState(IAccount account)
        {
            var hash = new Hash<IAccount>(this.GetSHA256Hash());
            if (null == _stateMerkleTreeRightChild.FindLeaf(hash))
            {
                _stateMerkleTreeRightChild.AddNode(hash);
                return true;
            }
            return false;
        }
    }
}