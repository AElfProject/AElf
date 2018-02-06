using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Extensions;
using AElf.Kernel.KernelAccount;
using AElf.Kernel.Merkle;

namespace AElf.Kernel
{
    public class WorldState : IWorldState
    {
        private Dictionary<byte[], IAccountDataProvider> _accountDataProviders;

        // TODO:
        // Figure out how to update the merkle tree node automatically.
        private BinaryMerkleTree<IHash> _merkleTree;

        //private Func<IMerkleNode>

        public WorldState()
        {
            _accountDataProviders = new Dictionary<byte[], IAccountDataProvider>();
            _merkleTree = new BinaryMerkleTree<IHash>();
        }

        /// <summary>
        /// If the given account address included in the world state, return the instance,
        /// otherwise create a new account data provider and return.
        /// </summary>
        /// <param name="accountAddress"></param>
        /// <returns></returns>
        public IAccountDataProvider GetAccountDataProviderByAccountAddress(byte[] accountAddress)
        {
            return _accountDataProviders.TryGetValue(accountAddress, out var accountDataProvider)
                ? accountDataProvider
                : AddAccountDataProvider(accountAddress);
        }

        public Task<IHash<IMerkleTree<IHash>>> GetWorldStateMerkleTreeRootAsync()
        {
            return Task.FromResult(_merkleTree.ComputeRootHash());
        }

        private IAccountDataProvider AddAccountDataProvider(byte[] accountAddress)
        {
            var accountDataProvider = new AccountDataProvider(accountAddress);
            //Add the address to dict.
            _accountDataProviders[accountAddress] = accountDataProvider;
            //Add the hash of account data provider to merkle tree as a node.
            _merkleTree.AddNode(new Hash<IHash>(accountDataProvider.CalculateHash()));

            return accountDataProvider;
        }
    }
}