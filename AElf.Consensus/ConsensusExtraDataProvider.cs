using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Execution.Execution;
using AElf.Kernel;
using AElf.Kernel.BlockService;
using AElf.Kernel.Managers;
using AElf.SmartContract;
using AElf.Types.CSharp;
using Google.Protobuf;

namespace AElf.Consensus
{
    public class ConsensusExtraDataProvider : IBlockExtraDataProvider
    {
        private readonly IConsensusService _consensusService;
        
        public ConsensusExtraDataProvider(IConsensusService consensusService)
        {
            _consensusService = consensusService;
        }
        
        public Task FillExtraData(Block block)
        {
            if (block.Header.BlockExtraData == null)
            {
                block.Header.BlockExtraData = new BlockExtraData();
            }

            var consensusInformation = _consensusService.GetNewConsensusInformation().ToByteArray();

            block.Header.BlockExtraData.ConsensusInformation = ByteString.CopyFrom(consensusInformation);

            return Task.CompletedTask;
        }


    }
}