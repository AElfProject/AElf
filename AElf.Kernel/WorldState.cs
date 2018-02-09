using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Extensions;
using AElf.Kernel.KernelAccount;
using AElf.Kernel.Merkle;

namespace AElf.Kernel
{
    public class WorldState : IWorldState
    {
        private Dictionary<IAccount, IAccountDataProvider> _accountDataProviders;

        private BinaryMerkleTree<IHash> _merkleTree;
        private List<IDataProvider> _dataProviders;

        private static WorldState _instance = null;
        private static readonly object SyncObject = new object();
        
        public static WorldState Instance
        {
            get
            {
                lock (SyncObject)
                {
                    return _instance ?? (_instance = new WorldState());
                }
            }
        }

        private WorldState()
        {
            _accountDataProviders = new Dictionary<IAccount, IAccountDataProvider>();
            _merkleTree = new BinaryMerkleTree<IHash>();
            _dataProviders = new List<IDataProvider>();
        }

        /// <summary>
        /// If the given account address included in the world state, return the instance,
        /// otherwise create a new account data provider and return.
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        public IAccountDataProvider GetAccountDataProviderByAccount(IAccount account)
        {
            return _accountDataProviders.TryGetValue(account, out var accountDataProvider)
                ? accountDataProvider
                : AddAccountDataProvider(account);
        }

        public Task<IHash<IMerkleTree<IHash>>> GetWorldStateMerkleTreeRootAsync()
        {
            return Task.FromResult(_merkleTree.ComputeRootHash());
        }

        private IAccountDataProvider AddAccountDataProvider(IAccount account)
        {
            var accountDataProvider = new AccountDataProvider(account);
            //Add the address to dict.
            _accountDataProviders[account] = accountDataProvider;
            
            AddDataProvider(accountDataProvider.GetDataProvider());
            
            return accountDataProvider;
        }

        public void AddDataProvider(IDataProvider dataProvider)
        {
            _dataProviders.Add(dataProvider);
            //Add the hash of account data provider to merkle tree as a node.
            _merkleTree.AddNode(new Hash<IHash>(dataProvider.CalculateHash()));
        }

        public void UpdateDataProvider(IDataProvider oldDataProvider, IDataProvider newDataProvider)
        {
            var order = _dataProviders.IndexOf(oldDataProvider);
            _dataProviders[order] = newDataProvider;
            _merkleTree.UpdateNode(new Hash<IHash>(oldDataProvider.CalculateHash()), 
                new Hash<IHash>(newDataProvider.CalculateHash()));
        }
    }
}
