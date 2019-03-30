using System;
using System.Linq;
using AElf.Common;
using AElf.Contracts.MultiToken.Messages;
using AElf.Kernel;
using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.TokenConverter
{
    public class TokenConverterContract : TokenConverterContractContainer.TokenConverterContractBase
    {
        #region Views

        public override Address GetTokenContractAddress(Empty input)
        {
            return State.TokenContract.Value;
        }

        public override Address GetFeeReceiverAddress(Empty input)
        {
            return State.FeeReceiverAddress.Value;
        }

        /// <summary>
        /// Query the connector details.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override Connector GetConnector(TokenId input)
        {
            return State.Connectors[input.Symbol];
        }

        #endregion Views

        #region Actions

        /// <summary>
        /// Initialize the contract information.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override Empty Initialize(InitializeInput input)
        {
            Assert(input.TokenContractAddress != null, "Token contract address required.");
            Assert(input.FeeReceiverAddress != null, "Fee receiver address required.");
            Assert(IsValidSymbol(input.BaseTokenSymbol), "Base token symbol is invalid.");
            Assert(input.MaxWeight > 0, "Invalid MaxWeight.");
            Assert(State.TokenContract.Value == null, "Already initialized.");
            State.TokenContract.Value = input.TokenContractAddress;
            State.FeeReceiverAddress.Value = input.FeeReceiverAddress;
            State.BaseTokenSymbol.Value = input.BaseTokenSymbol;
            State.MaxWeight.Value = input.MaxWeight;
            State.Manager.Value = input.Manager;
            var index = State.ConnectorCount.Value;
            foreach (var connector in input.Connectors)
            {
                Assert(IsValidSymbol(connector.Symbol), "Invalid symbol.");
                State.ConnectorSymbols[index] = connector.Symbol;
                State.Connectors[connector.Symbol] = connector;
                index = index.Add(1);
            }

            State.ConnectorCount.Value = index;
            return new Empty();
        }

        public override Empty SetConnector(Connector input)
        {
            Assert(Context.Sender == State.Manager.Value, "Only manager can perform this action.");
            var existing = State.Connectors[input.Symbol];
            if (existing == null)
            {
                State.ConnectorSymbols[State.ConnectorCount.Value] = input.Symbol;
                State.ConnectorCount.Value = State.ConnectorCount.Value.Add(1);
            }

            State.Connectors[input.Symbol] = input;
            return new Empty();
        }


        public override Empty Buy(BuyInput input)
        {
            Assert(IsValidSymbol(input.Symbol), "Invalid symbol.");
            var fromConnector = State.Connectors[State.BaseTokenSymbol.Value];
            var toConnector = State.Connectors[input.Symbol];
            Assert(toConnector != null, "Can't find connector.");
            var amountToPay = BancorHelpers.GetAmountToPayFromReturn(
                GetSelfBalance(fromConnector), GetWeight(fromConnector),
                GetSelfBalance(toConnector), GetWeight(toConnector),
                input.Amount);
            var fee = Convert.ToInt64(amountToPay * GetFeeRate());

            var amountToPayPlusFee = amountToPay.Add(fee);
            Assert(input.PayLimit == 0 || amountToPayPlusFee <= input.PayLimit, "Price not good.");

            // Pay fee
            if (fee > 0)
            {
                State.TokenContract.TransferFrom.Send(
                    new TransferFromInput()
                    {
                        Symbol = State.BaseTokenSymbol.Value,
                        From = Context.Sender,
                        To = State.FeeReceiverAddress.Value,
                        Amount = fee
                    });
            }

            // Transafer base token
            State.TokenContract.TransferFrom.Send(
                new TransferFromInput()
                {
                    Symbol = State.BaseTokenSymbol.Value,
                    From = Context.Sender,
                    To = Context.Self,
                    Amount = amountToPay
                });

            // Transafer bought token
            State.TokenContract.Transfer.Send(
                new TransferInput()
                {
                    Symbol = input.Symbol,
                    To = Context.Sender,
                    Amount = input.Amount
                });
            return new Empty();
        }


        public override Empty Sell(SellInput input)
        {
            Assert(IsValidSymbol(input.Symbol), "Invalid symbol.");
            var fromConnector = State.Connectors[input.Symbol];
            Assert(fromConnector != null, "Can't find connector.");
            var toConnector = State.Connectors[State.BaseTokenSymbol.Value];
            var amountToReceive = BancorHelpers.GetReturnFromPaid(
                GetSelfBalance(fromConnector), GetWeight(fromConnector),
                GetSelfBalance(toConnector), GetWeight(toConnector),
                input.Amount
            );

            var fee = Convert.ToInt64(amountToReceive * GetFeeRate());

            var amountToReceiveLessFee = amountToReceive.Sub(fee);
            Assert(input.ReceiveLimit == 0 || amountToReceiveLessFee >= input.ReceiveLimit, "Price not good.");

            // Pay fee
            if (fee > 0)
            {
                State.TokenContract.TransferFrom.Send(
                    new TransferFromInput()
                    {
                        Symbol = State.BaseTokenSymbol.Value,
                        From = Context.Sender,
                        To = State.FeeReceiverAddress.Value,
                        Amount = fee
                    });
            }

            // Transafer base token
            State.TokenContract.TransferFrom.Send(
                new TransferFromInput()
                {
                    Symbol = State.BaseTokenSymbol.Value,
                    From = Context.Sender,
                    To = Context.Self,
                    Amount = amountToReceiveLessFee
                });

            // Transafer sold token
            State.TokenContract.Transfer.Send(
                new TransferInput()
                {
                    Symbol = input.Symbol,
                    To = Context.Sender,
                    Amount = input.Amount
                });
            return new Empty();
        }

        #endregion Actions

        #region Helpers

        private static bool IsValidSymbol(string symbol)
        {
            return symbol.Length > 0 && symbol.All(c => c >= 'A' && c <= 'Z');
        }

        private decimal GetFeeRate()
        {
            return new decimal(State.FeeRateNumerator.Value) / new decimal(State.FeeRateDenominator.Value);
        }


        private long GetSelfBalance(Connector connector)
        {
            var realBalance = State.TokenContract.GetBalance.Call(
                new GetBalanceInput()
                {
                    Owner = Context.Self,
                    Symbol = connector.Symbol
                }).Balance;
            if (connector.IsVirtualBalanceEnabled)
            {
                return connector.VirtualBalance + realBalance;
            }

            return realBalance;
        }

        private decimal GetWeight(Connector connector)
        {
            return new decimal(connector.Weight) / new decimal(State.MaxWeight.Value);
        }

        #endregion
    }
}