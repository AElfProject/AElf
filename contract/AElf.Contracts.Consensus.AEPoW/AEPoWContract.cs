using AElf.Contracts.MultiToken;
using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.AEPoW
{
    public partial class AEPoWContract
    {
        public override Empty Initialize(InitializeInput input)
        {
            Assert(input.SupposedProduceMilliseconds > 0, "Invalid input.");
            State.SupposedProduceMilliseconds.Value = input.SupposedProduceMilliseconds;
            return new Empty();
        }

        public override Empty CoinBase(CoinBaseInput input)
        {
            if (Context.CurrentHeight == 2)
            {
                State.BlockchainStartTime.Value = Context.CurrentBlockTime;
                State.CurrentDifficulty.Value = "0";
                if (State.SupposedProduceMilliseconds.Value == 0)
                {
                    State.SupposedProduceMilliseconds.Value = 10_000;
                }

                if (State.CoinBaseTokenSymbol.Value == null)
                {
                    State.CoinBaseTokenSymbol.Value = Context.Variables.NativeSymbol;
                }
            }
            
            // TODO: Make sure there's only one CoinBase tx in one block.

            Assert(State.Records[Context.CurrentHeight] == null,
                $"Block of height {Context.CurrentHeight} already generated.");
            Context.LogDebug(() => $"Record of height {Context.CurrentHeight} set.");
            State.Records[Context.CurrentHeight] = new PoWRecord
            {
                Producer = Context.Sender,
                Timestamp = Context.CurrentBlockTime,
                NonceNumber = input.NonceNumber
            };

            if (State.TokenContract.Value == null)
            {
                State.TokenContract.Value =
                    Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);
            }

            if (CheckCoinBaseTokenExists())
            {
                State.TokenContract.Transfer.Send(new TransferInput
                {
                    To = Context.Sender,
                    Amount = GetCurrentCoinBaseAmount(),
                    Symbol = State.CoinBaseTokenSymbol.Value
                });
            }

            Context.Fire(new NonceUpdated
            {
                NonceNumber = input.NonceNumber,
                BlockHeight = Context.CurrentHeight
            });

            //CalculateAverageProduceMilliseconds();

            return new Empty();
        }

        private bool CheckCoinBaseTokenExists()
        {
            var tokenInfo = State.TokenContract.GetTokenInfo.Call(new GetTokenInfoInput
            {
                Symbol = State.CoinBaseTokenSymbol.Value
            });
            return tokenInfo != null;
        }

        // TODO: Calculated based on blockchain start time.
        private long GetCurrentCoinBaseAmount()
        {
            return 1_00000000;
        }
    }
}