using AElf.Contracts.MultiToken;
using AElf.Standards.ACS2;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.TransferWrapperContract
{
    /// <summary>
    /// The C# implementation of the contract defined in transfer_wrapper_contract.proto that is located in the "protobuf"
    /// folder.
    /// Notice that it inherits from the protobuf generated code. 
    /// </summary>
    public class TransferWrapperContract : TransferWrapperContractContainer.TransferWrapperContractBase
    {
        public override Empty Initialize(Address input)
        {
            Assert(State.TokenContract.Value == null, "Already initialized.");
            State.TokenContract.Value = input;
            return new Empty();
        }

        public override Empty ThroughContractTransfer(ThroughContractTransferInput input)
        {
            Assert(State.TokenContract.Value != null, "Please initialize first.");
            Context.SendVirtualInline(HashHelper.ComputeFrom(Context.Sender),
                State.TokenContract.Value,
                nameof(State.TokenContract.Transfer), new TransferInput
                {
                    To = input.To,
                    Amount = input.Amount,
                    Symbol = input.Symbol,
                    Memo = input.Memo
                }.ToByteString());
            return new Empty();
        }

        public override Empty ContractTransfer(ThroughContractTransferInput input)
        {
            Assert(State.TokenContract.Value != null, "Please initialize first.");
            State.TokenContract.Transfer.Send(new TransferInput
            {
                To = input.To,
                Amount = input.Amount,
                Symbol = input.Symbol,
                Memo = input.Memo
            });
            return new Empty();
        }

        public override Address GetTokenAddress(Empty input)
        {
            return State.TokenContract.Value;
        }

        public override ResourceInfo GetResourceInfo(Transaction txn)
        {
            var args = TransferInput.Parser.ParseFrom(txn.Params);
            var resourceInfo = new ResourceInfo
            {
                WritePaths =
                {
                    GetTokenContractPath("Balances", txn.From.ToString(), args.Symbol),
                    GetTokenContractPath("Balances", args.To.ToString(), args.Symbol),
                },
                ReadPaths =
                {
                    GetTokenContractPath("TokenInfos", args.Symbol)
                }
            };

            return resourceInfo;
        }

        private ScopedStatePath GetTokenContractPath(params string[] parts)
        {
            return new ScopedStatePath
            {
                Address = State.TokenContract.Value,
                Path = new StatePath
                {
                    Parts =
                    {
                        parts
                    }
                }
            };
        }
    }
}