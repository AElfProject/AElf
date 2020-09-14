using System;
using System.Globalization;
using System.Linq;
using AElf.Standards.ACS1;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.TokenConverter
{
    public partial class TokenConverterContract : TokenConverterContractImplContainer.TokenConverterContractImplBase
    {
        private const string NtTokenPrefix = "nt";

        #region Actions

        /// <summary>
        /// Initialize the contract information.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override Empty Initialize(InitializeInput input)
        {
            Assert(IsValidBaseSymbol(input.BaseTokenSymbol), $"Base token symbol is invalid. {input.BaseTokenSymbol}");
            Assert(State.TokenContract.Value == null, "Already initialized.");
            State.TokenContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);
            State.FeeReceiverAddress.Value =
                Context.GetContractAddressByName(SmartContractConstants.TreasuryContractSystemName);
            State.BaseTokenSymbol.Value = !string.IsNullOrEmpty(input.BaseTokenSymbol)
                ? input.BaseTokenSymbol
                : Context.Variables.NativeSymbol;
            var feeRate = AssertedDecimal(input.FeeRate);
            Assert(IsBetweenZeroAndOne(feeRate), "Fee rate has to be a decimal between 0 and 1.");
            State.FeeRate.Value = feeRate.ToString(CultureInfo.InvariantCulture);
            foreach (var connector in input.Connectors)
            {
                if (connector.IsDepositAccount)
                {
                    Assert(!string.IsNullOrEmpty(connector.Symbol), "Invalid connector symbol.");
                    AssertValidConnectorWeight(connector);
                }
                else
                {
                    Assert(IsValidSymbol(connector.Symbol), "Invalid symbol.");
                    AssertValidConnectorWeight(connector);
                }

                State.Connectors[connector.Symbol] = connector;
            }

            return new Empty();
        }

        public override Empty UpdateConnector(UpdateConnectorInput input)
        {
            AssertPerformedByConnectorController();
            Assert(!string.IsNullOrEmpty(input.Symbol), "input symbol can not be empty'");
            var targetConnector = State.Connectors[input.Symbol];
            Assert(targetConnector != null, "Can not find target connector.");
            Assert(!targetConnector.IsPurchaseEnabled, "connector can not be updated because it has been activated");
            if (!string.IsNullOrEmpty(input.Weight))
            {
                var weight = AssertedDecimal(input.Weight);
                Assert(IsBetweenZeroAndOne(weight), "Connector Shares has to be a decimal between 0 and 1.");
                targetConnector.Weight = input.Weight.ToString(CultureInfo.InvariantCulture);
            }

            if (targetConnector.IsDepositAccount && input.VirtualBalance > 0)
                targetConnector.VirtualBalance = input.VirtualBalance;
            State.Connectors[input.Symbol] = targetConnector;
            return new Empty();
        }


        public override Empty AddPairConnector(PairConnectorParam input)
        {
            AssertPerformedByConnectorController();
            Assert(!string.IsNullOrEmpty(input.ResourceConnectorSymbol),
                "resource token symbol should not be empty");
            var nativeConnectorSymbol = NtTokenPrefix.Append(input.ResourceConnectorSymbol);
            Assert(State.Connectors[input.ResourceConnectorSymbol] == null,
                "resource token symbol has existed");
            var resourceConnector = new Connector
            {
                Symbol = input.ResourceConnectorSymbol,
                IsPurchaseEnabled = false,
                RelatedSymbol = nativeConnectorSymbol,
                Weight = input.ResourceWeight
            };
            Assert(IsValidSymbol(resourceConnector.Symbol), "Invalid symbol.");
            AssertValidConnectorWeight(resourceConnector);
            var nativeTokenToResourceConnector = new Connector
            {
                Symbol = nativeConnectorSymbol,
                VirtualBalance = input.NativeVirtualBalance,
                IsVirtualBalanceEnabled = true,
                IsPurchaseEnabled = false,
                RelatedSymbol = input.ResourceConnectorSymbol,
                Weight = input.NativeWeight,
                IsDepositAccount = true
            };
            AssertValidConnectorWeight(nativeTokenToResourceConnector);
            State.Connectors[resourceConnector.Symbol] = resourceConnector;
            State.Connectors[nativeTokenToResourceConnector.Symbol] = nativeTokenToResourceConnector;
            return new Empty();
        }

        public override Empty Buy(BuyInput input)
        {
            var toConnector = State.Connectors[input.Symbol];
            if (toConnector == null)
            {
                throw new AssertionException("To connector not found during buying.");
            }

            Assert(toConnector.IsPurchaseEnabled, "Purchase not enabled.");
            var fromConnector = State.Connectors[toConnector.RelatedSymbol];
            var payAmount = BancorHelper.GetAmountToPayFromReturn(
                GetSelfBalance(fromConnector), GetWeight(fromConnector),
                GetSelfBalance(toConnector), GetWeight(toConnector),
                input.Amount);
            AdjustPayAmount(input.Symbol, ref payAmount);
            var fee = Convert.ToInt64(payAmount * GetFeeRate());
            Assert(fee > 0, $"Purchase token not enough: {input.Symbol}");

            var amountToPayPlusFee = payAmount.Add(fee);
            Assert(input.PayLimit == 0 || amountToPayPlusFee <= input.PayLimit, "Price not good.");

            // Pay fee
            if (fee > 0)
            {
                HandleFee(fee);
            }

            // Transfer base token
            State.TokenContract.TransferFrom.Send(
                new TransferFromInput
                {
                    Symbol = State.BaseTokenSymbol.Value,
                    From = Context.Sender,
                    To = Context.Self,
                    Amount = payAmount,
                });
            State.DepositBalance[fromConnector.Symbol] = State.DepositBalance[fromConnector.Symbol].Add(payAmount);
            // Transfer bought token
            State.TokenContract.Transfer.Send(
                new TransferInput
                {
                    Symbol = input.Symbol,
                    To = Context.Sender,
                    Amount = input.Amount
                });

            Context.Fire(new TokenBought
            {
                Symbol = input.Symbol,
                BoughtAmount = input.Amount,
                BaseAmount = payAmount,
                FeeAmount = fee
            });
            return new Empty();
        }

        public override Empty Sell(SellInput input)
        {
            var fromConnector = State.Connectors[input.Symbol];
            if (fromConnector == null)
            {
                throw new AssertionException("From connector not found during selling.");
            }

            Assert(fromConnector.IsPurchaseEnabled, "Purchase not enabled.");
            var toConnector = State.Connectors[fromConnector.RelatedSymbol];
            var receiveAmount = BancorHelper.GetReturnFromPaid(
                GetSelfBalance(fromConnector), GetWeight(fromConnector),
                GetSelfBalance(toConnector), GetWeight(toConnector),
                input.Amount
            );
            AdjustReceiveAmount(input.Symbol, ref receiveAmount);
            var fee = Convert.ToInt64(receiveAmount * GetFeeRate());
            var treasuryContractAddress = Context.GetContractAddressByName(SmartContractConstants.TreasuryContractSystemName);

            if (Context.Sender == treasuryContractAddress)
            {
                fee = 0;
            }
            else
            {
                Assert(fee > 0, $"Sell token not enough: {input.Symbol}");
            }

            var amountToReceiveLessFee = receiveAmount.Sub(fee);
            Assert(input.ReceiveLimit == 0 || amountToReceiveLessFee >= input.ReceiveLimit, "Price not good.");

            // Pay fee
            if (fee > 0)
            {
                HandleFee(fee);
            }

            // Transfer base token
            State.TokenContract.Transfer.Send(
                new TransferInput
                {
                    Symbol = State.BaseTokenSymbol.Value,
                    To = Context.Sender,
                    Amount = receiveAmount
                });
            State.DepositBalance[toConnector.Symbol] =
                State.DepositBalance[toConnector.Symbol].Sub(receiveAmount);
            // Transfer sold token
            State.TokenContract.TransferFrom.Send(
                new TransferFromInput
                {
                    Symbol = input.Symbol,
                    From = Context.Sender,
                    To = Context.Self,
                    Amount = input.Amount
                });
            Context.Fire(new TokenSold
            {
                Symbol = input.Symbol,
                SoldAmount = input.Amount,
                BaseAmount = receiveAmount,
                FeeAmount = fee
            });
            return new Empty();
        }

        private void HandleFee(long fee)
        {
            var donateFee = fee.Div(2);
            var burnFee = fee.Sub(donateFee);

            // Transfer to fee receiver.
            State.TokenContract.TransferFrom.Send(
                new TransferFromInput
                {
                    Symbol = State.BaseTokenSymbol.Value,
                    From = Context.Sender,
                    To = State.FeeReceiverAddress.Value,
                    Amount = donateFee
                });

            // Transfer to self contract then burn
            State.TokenContract.TransferFrom.Send(
                new TransferFromInput
                {
                    Symbol = State.BaseTokenSymbol.Value,
                    From = Context.Sender,
                    To = Context.Self,
                    Amount = burnFee
                });
            State.TokenContract.Burn.Send(
                new BurnInput
                {
                    Symbol = State.BaseTokenSymbol.Value,
                    Amount = burnFee
                });
        }

        public override Empty SetFeeRate(StringValue input)
        {
            AssertPerformedByConnectorController();
            var feeRate = AssertedDecimal(input.Value);
            Assert(IsBetweenZeroAndOne(feeRate), "Fee rate has to be a decimal between 0 and 1.");
            State.FeeRate.Value = feeRate.ToString(CultureInfo.InvariantCulture);
            return new Empty();
        }

        public override Empty EnableConnector(ToBeConnectedTokenInfo input)
        {
            var fromConnector = State.Connectors[input.TokenSymbol];
            if (fromConnector == null)
            {
                throw new AssertionException("[EnableConnector]From connector not found during enable connector.");
            }
            Assert(!fromConnector.IsDepositAccount, "From connector is deposit account.");
            var toConnector = State.Connectors[fromConnector.RelatedSymbol];
            if (toConnector == null)
            {
                throw new AssertionException("[EnableConnector]To connector not found during enable connector.");
            }
            var needDeposit = GetNeededDeposit(input);
            if (needDeposit.NeedAmount > 0)
            {
                State.TokenContract.TransferFrom.Send(
                    new TransferFromInput
                    {
                        Symbol = State.BaseTokenSymbol.Value,
                        From = Context.Sender,
                        To = Context.Self,
                        Amount = needDeposit.NeedAmount,
                    });
            }

            if (input.AmountToTokenConvert > 0)
            {
                State.TokenContract.TransferFrom.Send(
                    new TransferFromInput
                    {
                        Symbol = input.TokenSymbol,
                        From = Context.Sender,
                        To = Context.Self,
                        Amount = input.AmountToTokenConvert
                    });
            }

            State.DepositBalance[toConnector.Symbol] = needDeposit.NeedAmount;
            toConnector.IsPurchaseEnabled = true;
            fromConnector.IsPurchaseEnabled = true;
            return new Empty();
        }

        public override Empty ChangeConnectorController(AuthorityInfo input)
        {
            AssertPerformedByConnectorController();
            Assert(CheckOrganizationExist(input), "New Controller organization does not exist.");
            State.ConnectorController.Value = input;
            return new Empty();
        }

        #endregion Actions

        #region Helpers

        private decimal AssertedDecimal(string number)
        {
            Assert(decimal.TryParse(number, out var decimalNumber), $@"Invalid decimal ""{number}""");
            return decimalNumber;
        }

        private static bool IsBetweenZeroAndOne(decimal number)
        {
            return number > decimal.Zero && number < decimal.One;
        }

        private static bool IsValidSymbol(string symbol)
        {
            return symbol.Length > 0 &&
                   symbol.All(c => c >= 'A' && c <= 'Z');
        }

        private static bool IsValidBaseSymbol(string symbol)
        {
            return string.IsNullOrEmpty(symbol) || IsValidSymbol(symbol);
        }

        private decimal GetFeeRate()
        {
            return decimal.Parse(State.FeeRate.Value);
        }

        private long GetSelfBalance(Connector connector)
        {
            long realBalance;
            if (connector.IsDepositAccount)
            {
                realBalance = State.DepositBalance[connector.Symbol];
            }
            else
            {
                realBalance = State.TokenContract.GetBalance.Call(
                    new GetBalanceInput()
                    {
                        Owner = Context.Self,
                        Symbol = connector.Symbol
                    }).Balance;
            }

            if (connector.IsVirtualBalanceEnabled)
            {
                return connector.VirtualBalance.Add(realBalance);
            }

            return realBalance;
        }

        private decimal GetWeight(Connector connector)
        {
            return decimal.Parse(connector.Weight);
        }

        private void AssertPerformedByConnectorController()
        {
            if (State.ConnectorController.Value == null)
            {
                State.ConnectorController.Value = GetDefaultConnectorController();
            }

            Assert(Context.Sender == State.ConnectorController.Value.OwnerAddress,
                "Only manager can perform this action.");
        }

        private AuthorityInfo GetDefaultConnectorController()
        {
            if (State.ParliamentContract.Value == null)
            {
                State.ParliamentContract.Value =
                    Context.GetContractAddressByName(SmartContractConstants.ParliamentContractSystemName);
            }

            return new AuthorityInfo
            {
                ContractAddress = State.ParliamentContract.Value,
                OwnerAddress = State.ParliamentContract.GetDefaultOrganizationAddress.Call(new Empty())
            };
        }

        private void AssertValidConnectorWeight(Connector connector)
        {
            var weight = AssertedDecimal(connector.Weight);
            Assert(IsBetweenZeroAndOne(weight), "Connector Shares has to be a decimal between 0 and 1.");
            connector.Weight = weight.ToString(CultureInfo.InvariantCulture);
        }

        private void AdjustPayAmount(string symbol, ref long payAmount)
        {
            var tradeInformation = State.TradeInformation[symbol] ?? new TradeInformation();
            if (tradeInformation.BuyTimes >= tradeInformation.SellTimes)
            {
                tradeInformation.PrepareAmount = tradeInformation.PrepareAmount.Add(1);
                payAmount = payAmount.Add(1); // avoid buying multiple times and selling all one time
            }

            tradeInformation.BuyTimes = tradeInformation.BuyTimes.Add(1);
            State.TradeInformation[symbol] = tradeInformation;
        }

        private void AdjustReceiveAmount(string symbol, ref long receiveAmount)
        {
            var tradeInformation = State.TradeInformation[symbol] ?? new TradeInformation();
            if (tradeInformation.PrepareAmount > 0 && tradeInformation.SellTimes > tradeInformation.BuyTimes)
            {
                tradeInformation.PrepareAmount = tradeInformation.PrepareAmount.Sub(1);
                receiveAmount = receiveAmount.Add(1);
            }

            tradeInformation.SellTimes = tradeInformation.SellTimes.Add(1);
            State.TradeInformation[symbol] = tradeInformation;
        }

        #endregion
    }
}