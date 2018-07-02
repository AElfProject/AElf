using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Cryptography.ECDSA;
using AElf.Kernel.Node.Config;
using AElf.Kernel.Services;
using AElf.Kernel.Types;
using AElf.Types.CSharp;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using ServiceStack;

namespace AElf.Kernel.BlockValidationFilters
{
    public class ConsensusBlockValidationFilter: IBlockValidationFilter
    {
        private readonly ISmartContractService _smartContractService;

        public ConsensusBlockValidationFilter(ISmartContractService smartContractService)
        {
            _smartContractService = smartContractService;
        }

        public async Task<ValidationError> ValidateBlockAsync(IBlock block, IChainContext context, ECKeyPair keyPair)
        {
            if (block.Header.Index < 2)
            {
                return ValidationError.Success;
            }
            
            var uncompressedPrivKey = block.Header.P.ToByteArray();
            var recipientKeyPair = ECKeyPair.FromPublicKey(uncompressedPrivKey);
            var contractAccountHash = new Hash(context.ChainId.CalculateHashWith("__SmartContractZero__")).ToAccount();
            var executive = await _smartContractService.GetExecutiveAsync(contractAccountHash, context.ChainId);
            var tx = GetTxToVerifyBlockProducer(contractAccountHash, keyPair, recipientKeyPair.GetAddress().ToHex());
            var tc = new TransactionContext
            {
                Transaction = tx
            };
            executive.SetTransactionContext(tc).Apply(true).Wait();
            
            var trace = tc.Trace;
            if (!trace.StdErr.IsNullOrEmpty())
            {
                return ValidationError.InvalidBlock;
            }

            return BoolValue.Parser.ParseFrom(trace.RetVal.ToByteArray()).Value
                ? ValidationError.Success
                : ValidationError.InvalidBlock;
        }

        private ITransaction GetTxToVerifyBlockProducer(Hash contractAccountHash, ECKeyPair keyPair, string recepientAddress)
        {
            var tx = new Transaction
            {
                From = keyPair.GetAddress(),
                To = contractAccountHash,
                IncrementId = 0,
                MethodName = "BlockProducerVerification",
                P = ByteString.CopyFrom(keyPair.PublicKey.Q.GetEncoded()),
                Params = ByteString.CopyFrom(ParamsPacker.Pack(new StringValue {Value = recepientAddress}))
            };
            
            var signer = new ECSigner();
            var signature = signer.Sign(keyPair, tx.GetHash().GetHashBytes());

            // Update the signature
            tx.R = ByteString.CopyFrom(signature.R);
            tx.S = ByteString.CopyFrom(signature.S);

            return tx;
        }
    }
}