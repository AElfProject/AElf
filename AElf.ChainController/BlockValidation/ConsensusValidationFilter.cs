using System.Threading.Tasks;
using AElf.Common.Attributes;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Common;
using AElf.SmartContract;
using AElf.Types.CSharp;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using NLog;
using ServiceStack;

// ReSharper disable once CheckNamespace
namespace AElf.ChainController
{
    [LoggerName(nameof(ConsensusBlockValidationFilter))]
    public class ConsensusBlockValidationFilter: IBlockValidationFilter
    {
        private readonly ISmartContractService _smartContractService;
        private readonly ILogger _logger;

        public ConsensusBlockValidationFilter(ISmartContractService smartContractService, ILogger logger)
        {
            _smartContractService = smartContractService;
            _logger = logger;
        }

        public async Task<BlockValidationResult> ValidateBlockAsync(IBlock block, IChainContext context, ECKeyPair keyPair)
        {
            // If the height of chain is 1, no need to check consensus validation
            if (block.Header.Index < 2)
            {
                return BlockValidationResult.Success;
            }
            
            // Get block producer's address from block header
            var uncompressedPrivateKey = block.Header.P.ToByteArray();
            var recipientKeyPair = ECKeyPair.FromPublicKey(uncompressedPrivateKey);
            
            // Get the address of consensus contract
            var contractAccountHash = AddressHelpers.GetSystemContractAddress(context.ChainId, SmartContractType.AElfDPoS.ToString());
            var timestampOfBlock = block.Header.Time;
            
            //Formulate an Executive and execute a transaction of checking time slot of this block producer
            var executive = await _smartContractService.GetExecutiveAsync(contractAccountHash, context.ChainId);
            var tx = GetTxToVerifyBlockProducer(contractAccountHash, keyPair, recipientKeyPair.GetAddress().DumpHex(), timestampOfBlock);
            if (tx == null)
            {
                return BlockValidationResult.FailedToCheckConsensusInvalidation;
            }
            var tc = new TransactionContext
            {
                Transaction = tx
            };
            await executive.SetTransactionContext(tc).Apply();
            var trace = tc.Trace;
            
            //If failed to execute the transaction of checking time slot
            if (!trace.StdErr.IsNullOrEmpty())
            {
                return BlockValidationResult.FailedToCheckConsensusInvalidation;
            }

            return BoolValue.Parser.ParseFrom(trace.RetVal.ToByteArray()).Value
                ? BlockValidationResult.Success
                : BlockValidationResult.InvalidTimeSlot;
        }

        private Transaction GetTxToVerifyBlockProducer(Address contractAccountHash, ECKeyPair keyPair, string recepientAddress, Timestamp timestamp)
        {
            if (contractAccountHash == null || keyPair == null || recepientAddress == null)
            {
                _logger?.Error("Something wrong happened to consensus verification filter.");
                return null;
            }
            
            var tx = new Transaction
            {
                From = keyPair.GetAddress(),
                To = contractAccountHash,
                IncrementId = 0,
                MethodName = "Validation",
                P = ByteString.CopyFrom(keyPair.PublicKey.Q.GetEncoded()),
                Params = ByteString.CopyFrom(ParamsPacker.Pack(
                    new StringValue {Value = recepientAddress.RemoveHexPrefix()}.ToByteArray(), 
                    timestamp.ToByteArray()))
            };
            
            var signer = new ECSigner();
            var signature = signer.Sign(keyPair, tx.GetHash().DumpByteArray());

            // Update the signature
            tx.R = ByteString.CopyFrom(signature.R);
            tx.S = ByteString.CopyFrom(signature.S);

            return tx;
        }
    }
}