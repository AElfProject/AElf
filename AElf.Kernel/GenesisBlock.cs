using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Extensions;
using AElf.Kernel.KernelAccount;

namespace AElf.Kernel
{
    public class GenesisBlock : IBlock
    {
        private readonly BlockHeader _blockHeader = new BlockHeader(Hash<IBlock>.Zero);
        private readonly BlockBody _blockBody = new BlockBody();
        private AccountZero _accountZero;
        private readonly WorldState _worldState;

        public GenesisBlock(WorldState worldState, AccountZero accountZero)
        {
            _worldState = worldState;
            _accountZero = accountZero;
        }

        /// <summary>
        /// Returns the block hash.
        /// </summary>
        /// <returns>The hash.</returns>
        public IHash GetHash()
        {
            return new Hash<IBlock>(this.CalculateHash());
        }

        /// <summary>
        /// Deploy contracts in Genesis block
        /// </summary>
        /// <param name="smartContractRegistrations"></param>
        public void DeployContracts(IEnumerable<SmartContractRegistration> smartContractRegistrations)
        {
            _accountZero.DeployContractsInGenesinGenesisBlock(smartContractRegistrations);
        }

        public IBlockHeader GetHeader()
        {
            return _blockHeader;
        }

        public IBlockBody GetBody()
        {
            return _blockBody;
        }

        // No Transaction in GenesisBlock
        public bool AddTransaction(ITransaction tx)
        {
            throw new System.NotImplementedException("No Transaction in GenesisBlock");
        }
    }
}