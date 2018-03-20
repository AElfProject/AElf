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

        public WorldState()
        {
            _accountDataProviders = new Dictionary<IAccount, IAccountDataProvider>();
            _merkleTree = new BinaryMerkleTree<IHash>();
        }

        /// <summary>
        /// If the given account address included in the world state, return the instance,
        /// otherwise create a new account data provider and return.
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        public IAccountDataProvider GetAccountDataProviderByAccount(IAccount account)
        {
            foreach (var k in _accountDataProviders.Keys)
            {
                if (k.GetAddress().Equals(account.GetAddress()))
                {
                    return _accountDataProviders[k];
                }
            }
            throw new InvalidOperationException("Must add the account data provider before.");
        }

        public Task<IHash<IMerkleTree<IHash>>> GetWorldStateMerkleTreeRootAsync()
        {
            return Task.FromResult(_merkleTree.ComputeRootHash());
        }

        /// <summary>
        /// Add an account data provider,
        /// then add the corresponding data provider to data provider list and merkle tree.
        /// </summary>
        /// <param name="accountDataProviderunt"></param>
        /// <returns></returns>
        public void AddAccountDataProvider(IAccountDataProvider accountDataProviderunt)
        {
            var accountDataProvider = accountDataProviderunt;
            var address = accountDataProviderunt.Context.Address;
            AddDataProvider(new DataProvider(this, address));
            
            //Add the address to dict.
            //_accountDataProviders[new Account(address)] = accountDataProvider;
        }
        
        /// <summary>
        /// Add the data provider to data provider list,
        /// then add its hash value to merkle tree.
        /// </summary>
        /// <param name="dataProvider"></param>
        public void AddDataProvider(IDataProvider dataProvider)
        {
            //Add the hash of account data provider to merkle tree as a node.
            _merkleTree.AddNode(new Hash<IHash>(dataProvider.CalculateHash()));
        }
        
        /// <summary>
        /// Replace a data provider by a new one,
        /// throw a exception when the data provider could not be found.
        /// </summary>
        /// <param name="oldDataProvider"></param>
        /// <param name="newDataProvider"></param>
        public void UpdateDataProvider(IDataProvider oldDataProvider, IDataProvider newDataProvider)
        {
            if (oldDataProvider == null)
            {
                AddDataProvider(newDataProvider);
                return;
            }

            _merkleTree.UpdateNode(new Hash<IHash>(oldDataProvider.CalculateHash()), 
                new Hash<IHash>(newDataProvider.CalculateHash()));
        }
    }
}
