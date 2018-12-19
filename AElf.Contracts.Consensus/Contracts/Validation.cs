using AElf.Kernel;
using AElf.Sdk.CSharp;

namespace AElf.Contracts.Consensus.Contracts
{
    // ReSharper disable UnusedMember.Global
    // ReSharper disable InconsistentNaming
    public class Validation
    {
        private readonly DataCollection _collection;

        public Validation(DataCollection collection)
        {
            _collection = collection;
        }

        public BlockValidationResult ValidateBlock(BlockAbstract blockAbstract)
        {
            var minersList = Api.GetCurrentMiners();
            if (!minersList.Contains(blockAbstract.MinerPublicKey))
            {
                return BlockValidationResult.NotMiner;
            }

            return BlockValidationResult.Success;
        }
    }
}