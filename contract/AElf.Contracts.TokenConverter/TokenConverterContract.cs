using System;
using System.Globalization;
using System.Linq;
using AElf.Contracts.MultiToken.Messages;
using AElf.Sdk.CSharp;
using AElf.Types;
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
            Assert(State.TokenContract.Value == null, "Already initialized.");
            State.TokenContract.Value = input.TokenContractAddress;
            State.FeeReceiverAddress.Value = input.FeeReceiverAddress;
            State.BaseTokenSymbol.Value = input.BaseTokenSymbol;
            State.ManagerAddress.Value = input.ManagerAddress;
            var feeRate = AssertedDecimal(input.FeeRate);
            Assert(IsBetweenZeroAndOne(feeRate), "Fee rate has to be a decimal between 0 and 1.");
            State.FeeRate.Value = feeRate.ToString(CultureInfo.InvariantCulture);

            var index = State.ConnectorCount.Value;
            foreach (var connector in input.Connectors)
            {
                AssertValidConnectorAndNormalizeWeight(connector);
                State.ConnectorSymbols[index] = connector.Symbol;
                State.Connectors[connector.Symbol] = connector;
                index = index.Add(1);
            }

            State.ConnectorCount.Value = index;
            return new Empty();
        }

        public override Empty SetConnector(Connector input)
        {
            AssertPerformedByManager();
            AssertValidConnectorAndNormalizeWeight(input);
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
            var amountToPay = BancorHelper.GetAmountToPayFromReturn(
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

            // Transfer base token
            State.TokenContract.TransferFrom.Send(
                new TransferFromInput()
                {
                    Symbol = State.BaseTokenSymbol.Value,
                    From = Context.Sender,
                    To = Context.Self,
                    Amount = amountToPay
                });

            // Transfer bought token
            State.TokenContract.Transfer.Send(
                new TransferInput()
                {
                    Symbol = input.Symbol,
                    To = Context.Sender,
                    Amount = input.Amount
                });
            Context.Fire(new TokenBought()
            {
                Symbol = input.Symbol,
                BoughtAmount = input.Amount,
                BaseAmount = amountToPay,
                FeeAmount = fee
            });
            return new Empty();
        }


        public override Empty Sell(SellInput input)
        {
            Assert(IsValidSymbol(input.Symbol), "Invalid symbol.");
            var fromConnector = State.Connectors[input.Symbol];
            Assert(fromConnector != null, "Can't find connector.");
            var toConnector = State.Connectors[State.BaseTokenSymbol.Value];
            var amountToReceive = BancorHelper.GetReturnFromPaid(
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
                State.TokenContract.Transfer.Send(
                    new TransferInput()
                    {
                        Symbol = State.BaseTokenSymbol.Value,
                        To = State.FeeReceiverAddress.Value,
                        Amount = fee
                    });
            }

            // Transfer base token
            State.TokenContract.Transfer.Send(
                new TransferInput()
                {
                    Symbol = State.BaseTokenSymbol.Value,
                    To = Context.Sender,
                    Amount = amountToReceiveLessFee
                });

            // Transfer sold token
            State.TokenContract.TransferFrom.Send(
                new TransferFromInput()
                {
                    Symbol = input.Symbol,
                    From = Context.Sender,
                    To = Context.Self,
                    Amount = input.Amount
                });
            Context.Fire(new TokenSold()
            {
                Symbol = input.Symbol,
                SoldAmount = input.Amount,
                BaseAmount = amountToReceive,
                FeeAmount = fee
            });
            return new Empty();
        }

        public override Empty SetFeeRate(StringValue input)
        {
            AssertPerformedByManager();
            var feeRate = AssertedDecimal(input.Value);
            Assert(IsBetweenZeroAndOne(feeRate), "Fee rate has to be a decimal between 0 and 1.");
            State.FeeRate.Value = feeRate.ToString(CultureInfo.InvariantCulture);
            return new Empty();
        }

        public override Empty SetManagerAddress(Address input)
        {
            AssertPerformedByManager();
            Assert(input != null && input != new Address(), "Input is not a valid address.");
            State.ManagerAddress.Value = input;
            return new Empty();
        }

        #endregion Actions

        #region Helpers

        private static decimal AssertedDecimal(string number)
        {
            try
            {
                return decimal.Parse(number);
            }
            catch (FormatException)
            {
                throw new InvalidValueException($@"Invalid decimal ""{number}""");
            }
        }

        private static bool IsBetweenZeroAndOne(decimal number)
        {
            return number > decimal.Zero && number < decimal.One;
        }

        private static bool IsValidSymbol(string symbol)
        {
            return symbol.Length > 0 && symbol.All(c => c >= 'A' && c <= 'Z');
        }

        private decimal GetFeeRate()
        {
            return decimal.Parse(State.FeeRate.Value);
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
            return decimal.Parse(connector.Weight);
        }

        private void AssertPerformedByManager()
        {
            Assert(Context.Sender == State.ManagerAddress.Value, "Only manager can perform this action.");
        }

        private void AssertValidConnectorAndNormalizeWeight(Connector connector)
        {
            Assert(IsValidSymbol(connector.Symbol), "Invalid symbol.");
            var weight = AssertedDecimal(connector.Weight);
            Assert(IsBetweenZeroAndOne(weight), "Connector weight has to be a decimal between 0 and 1.");
            connector.Weight = weight.ToString(CultureInfo.InvariantCulture);
        }

        #endregion
    }
}