using AElf.Contracts.MultiToken;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.AEPoW
{
    public partial class AEPoWContract
    {
        public override Empty CoinBase(Address input)
        {
            if (State.TokenContract.Value == null)
            {
                State.TokenContract.Value =
                    Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);
            }

            State.TokenContract.Transfer.Send(new TransferInput
            {
                To = input,
                Amount = GetCurrentCoinBaseAmount(),
                Symbol = Context.Variables.NativeSymbol
            });

            return new Empty();
        }

        // TODO: Calculated based on blockchain start time.
        private long GetCurrentCoinBaseAmount()
        {
            return 0;
        }
    }
}