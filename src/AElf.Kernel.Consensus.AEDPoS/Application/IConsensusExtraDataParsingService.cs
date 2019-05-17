using System;
using System.Threading.Tasks;
using AElf.Contracts.Consensus.AEDPoS;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.Consensus.AEDPoS.Application
{
    internal interface IConsensusExtraDataParsingService
    {
        BytesValue ParseHeaderExtraData(byte[] consensusTriggerInformation);
    }
}