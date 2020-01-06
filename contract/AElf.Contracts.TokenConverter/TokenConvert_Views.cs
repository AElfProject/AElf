using AElf.Contracts.MultiToken;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.TokenConverter
{
    public partial class TokenConverterContract
    {
        public override Address GetTokenContractAddress(Empty input)
        {
            return State.TokenContract.Value;
        }

        public override Address GetFeeReceiverAddress(Empty input)
        {
            return State.FeeReceiverAddress.Value;
        }

        public override StringValue GetFeeRate(Empty input)
        {
            return new StringValue()
            {
                Value = State.FeeRate.Value
            };
        }

        public override Address GetManagerAddress(Empty input)
        {
            return State.ManagerAddress.Value;
        }

        public override TokenSymbol GetBaseTokenSymbol(Empty input)
        {
            return new TokenSymbol()
            {
                Symbol = State.BaseTokenSymbol.Value
            };
        }

        /// <summary>
        /// Query the connector details.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override Connector GetConnector(TokenSymbol input)
        {
            return State.Connectors[input.Symbol];
        }

        public override DepositInfo GetNeededDeposit(ToBeConnectedTokenInfo input)
        {
            Assert(IsValidSymbol(input.TokenSymbol), "Invalid symbol.");

            var toConnector = State.Connectors[input.TokenSymbol];
            Assert(toConnector != null, "Can't find to connector.");
            Assert(!string.IsNullOrEmpty(toConnector.RelatedSymbol), "can't find related symbol'");
            var fromConnector = State.Connectors[toConnector.RelatedSymbol];
            Assert(fromConnector != null, "Can't find from connector.");
            var tokenInfo = State.TokenContract.GetTokenInfo.Call(
                new GetTokenInfoInput
                {
                    Symbol = input.TokenSymbol,
                });
            var balance = State.TokenContract.GetBalance.Call(
                new GetBalanceInput
                {
                    Owner = Context.Self,
                    Symbol = input.TokenSymbol
                }).Balance;
            var amountOutOfTokenConvert = tokenInfo.TotalSupply - balance - input.AmountToTokenConvert;
            long needDeposit = 0;
            if (amountOutOfTokenConvert > 0)
            {
                var fb = fromConnector.VirtualBalance;
                var tb = toConnector.IsVirtualBalanceEnabled
                    ? toConnector.VirtualBalance.Add(tokenInfo.TotalSupply)
                    : tokenInfo.TotalSupply;
                needDeposit =
                    BancorHelper.GetAmountToPayFromReturn(fb, GetWeight(fromConnector), 
                        tb, GetWeight(toConnector), amountOutOfTokenConvert);
            }
            return new DepositInfo
            {
                NeedAmount = needDeposit,
                AmountOutOfTokenConvert = amountOutOfTokenConvert
            };
        }
        
        public override Int64Value GetDepositConnectorBalance(StringValue symbolInput)
        {
            var connector = State.Connectors[symbolInput.Value];
            Assert(connector != null && !connector.IsDepositAccount, $"token symbol is invalid");
            var ntSymbol = connector.RelatedSymbol;
            return new Int64Value
            {
                Value = State.Connectors[ntSymbol].VirtualBalance + State.DepositBalance[ntSymbol]
            };
        }
    }
}