using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Consensus.Application;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AElf.Kernel.Consensus.AEDPoS.Application
{
    // ReSharper disable once InconsistentNaming
    public class AEDPoSExtraDataExtractor : IConsensusExtraDataExtractor
    {
        private readonly IBlockExtraDataService _blockExtraDataService;
        private readonly IConsensusExtraDataNameProvider _consensusExtraDataNameProvider;
        

        public AEDPoSExtraDataExtractor(IBlockExtraDataService blockExtraDataService, 
            IConsensusExtraDataNameProvider consensusExtraDataNameProvider )
        {
            _blockExtraDataService = blockExtraDataService;
            _consensusExtraDataNameProvider = consensusExtraDataNameProvider;
        }

        public ByteString ExtractConsensusExtraData(BlockHeader header)
        {
            var consensusExtraData =
                _blockExtraDataService.GetExtraDataFromBlockHeader(_consensusExtraDataNameProvider.ExtraDataName, header);
            if (consensusExtraData == null)
                return null;

            var headerInformation = AElfConsensusHeaderInformation.Parser.ParseFrom(consensusExtraData);
            consensusExtraData = SetValidate(consensusExtraData);

            // Validate header information
            return headerInformation.SenderPubkey != header.SignerPubkey ? null : consensusExtraData;
        }

        public virtual long GetForkBlockHeight()
        {
            return 0;
        }

        protected virtual ByteString SetValidate(ByteString consensusExtraData)
        {
            var headerInformation = AElfConsensusHeaderInformation.Parser.ParseFrom(consensusExtraData);
            headerInformation.IsValidate = true;
            return headerInformation.ToByteString();
        }
    }

    public class TestForkService : ITestForkService
    {
        private readonly List<IConsensusExtraDataExtractor> _consensusExtraDataExtractors;

        public TestForkService(IServiceContainer<IConsensusExtraDataExtractor> consensusExtraDataExtractors)
        {
            _consensusExtraDataExtractors = consensusExtraDataExtractors
                .OrderByDescending(provider => provider.GetForkBlockHeight()).ToList();
        }

        public ByteString ExtractConsensusExtraData(BlockHeader header)
        {
            var consensusExtraDataExtractor =
                _consensusExtraDataExtractors.First(provider => provider.GetForkBlockHeight() <= header.Height);
            return consensusExtraDataExtractor.ExtractConsensusExtraData(header);
        }
    }
    
    // ReSharper disable once InconsistentNaming
    public class ForkAEDPoSExtraDataExtractor : AEDPoSExtraDataExtractor
    {
        private readonly ChainOptions _chainOptions;
        public ILogger<ForkAEDPoSExtraDataExtractor> Logger { get; set; }
        public ForkAEDPoSExtraDataExtractor(IBlockExtraDataService blockExtraDataService,
            IConsensusExtraDataNameProvider consensusExtraDataNameProvider,IOptionsSnapshot<ChainOptions> chainOptions) : base(blockExtraDataService,
            consensusExtraDataNameProvider)
        {
            _chainOptions = chainOptions.Value;
            
            Logger = new NullLogger<ForkAEDPoSExtraDataExtractor>();
        }

        public override long GetForkBlockHeight()
        {
            return _chainOptions.TestForkBlock4000;
        }

        protected override ByteString SetValidate(ByteString consensusExtraData)
        {
            Logger.LogDebug("SetValidate above fork block");
            var headerInformation = AElfConsensusHeaderInformation.Parser.ParseFrom(consensusExtraData);
            headerInformation.IsValidate = false;
            return headerInformation.ToByteString();
        }
    }
}