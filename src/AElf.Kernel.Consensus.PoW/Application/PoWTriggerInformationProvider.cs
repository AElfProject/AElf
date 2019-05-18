using AElf.Kernel.Consensus.Application;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.Consensus.PoW.Application
{
    public class PoWTriggerInformationProvider : ITriggerInformationProvider
    {
        public BytesValue GetTriggerInformationForConsensusCommand()
        {
            return new BytesValue();
        }

        public BytesValue GetTriggerInformationForBlockHeaderExtraData()
        {
            // TODO: Return block hash of the block is generating.
            throw new System.NotImplementedException();
        }

        public BytesValue GetTriggerInformationForConsensusTransactions()
        {
            // TODO: Return nonce.
            throw new System.NotImplementedException();
        }
    }
}