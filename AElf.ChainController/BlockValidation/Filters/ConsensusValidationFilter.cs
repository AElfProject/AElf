using System.Linq;
using System.Threading.Tasks;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Common;
using AElf.Configuration;
using AElf.Configuration.Config.Chain;
using AElf.Kernel.Consensus;
using AElf.SmartContract;
using AElf.Types.CSharp;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using NLog;

// ReSharper disable once CheckNamespace
namespace AElf.ChainController
{
    // ReSharper disable InconsistentNaming
    public class ConsensusBlockValidationFilter
    {
        private readonly ISmartContractService _smartContractService;
        private readonly ILogger _logger;

        private Address ConsensusContractAddress =>
            ContractHelpers.GetConsensusContractAddress(Hash.LoadBase58(ChainConfig.Instance.ChainId));

        public ConsensusBlockValidationFilter(ISmartContractService smartContractService)
        {
            _smartContractService = smartContractService;
            
            _logger = LogManager.GetLogger(nameof(ConsensusBlockValidationFilter));
        }

        public async Task<BlockValidationResult> ValidateBlockAsync(IBlock block)
        {
            if (NodeConfig.Instance.ConsensusKind == ConsensusKind.AElfDPoS)
            {
                return await DPoSValidation(block);
            }

            if (NodeConfig.Instance.ConsensusKind == ConsensusKind.AElfPoW)
            {
                return await PoWValidation(block);
            }

            return BlockValidationResult.NotImplementConsensus;
        }

        private async Task<BlockValidationResult> DPoSValidation(IBlock block)
        {
            // If the height of chain is 1, no need to check consensus validation
            if (block.Header.Index < GlobalConfig.GenesisBlockHeight + 2)
            {
                return BlockValidationResult.Success;
            }

            var updateTx =
                block.Body.TransactionList.Where(t => t.MethodName == ConsensusBehavior.NextRound.ToString())
                    .ToList();
            if (updateTx.Count > 0)
            {
                if (updateTx.Count > 1)
                {
                    return BlockValidationResult.IncorrectConsensusTransaction;
                }
            }

            //Formulate an Executive and execute a transaction of checking time slot of this block producer
            TransactionTrace trace;
            var executive = await _smartContractService.GetExecutiveAsync(ConsensusContractAddress,
                Hash.LoadBase58(ChainConfig.Instance.ChainId));
            try
            {
                var tx = GetTransactionToValidateBlock(block.GetAbstract());
                if (tx == null)
                {
                    return BlockValidationResult.FailedToCheckConsensusInvalidation;
                }

                var tc = new TransactionContext
                {
                    Transaction = tx
                };
                await executive.SetTransactionContext(tc).Apply();
                trace = tc.Trace;
            }
            finally
            {
                _smartContractService.PutExecutiveAsync(ConsensusContractAddress, executive).Wait();
            }
            
            //If failed to execute the transaction of checking time slot
            if (!trace.StdErr.IsNullOrEmpty())
            {
                _logger.Trace("Failed to execute tx Validation: " + trace.StdErr);
                return BlockValidationResult.FailedToCheckConsensusInvalidation;
            }

            var result = Int32Value.Parser.ParseFrom(trace.RetVal.ToByteArray()).Value;

            switch (result)
            {
                case 1:
                    return BlockValidationResult.NotMiner;
                case 2:
                    return BlockValidationResult.InvalidTimeSlot;
                case 3:
                    return BlockValidationResult.SameWithCurrentRound;
                case 11:
                    return BlockValidationResult.ParseProblem;
                default:
                    return BlockValidationResult.Success;
            }
        }

        private async Task<BlockValidationResult> PoWValidation(IBlock block)
        {
            return BlockValidationResult.Success;
        }

        private Transaction GetTransactionToValidateBlock(BlockAbstract blockAbstract)
        {
            var tx = new Transaction
            {
                From = Address.Parse(NodeConfig.Instance.NodeAccount),
                To = ConsensusContractAddress,
                IncrementId = 0,
                MethodName = "ValidateBlock",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(blockAbstract))
            };

            var signer = new ECSigner();
            var signature = signer.Sign(NodeConfig.Instance.ECKeyPair, tx.GetHash().DumpByteArray());
            tx.Sigs.Add(ByteString.CopyFrom(signature.SigBytes)); 

            return tx;
        }
    }
}