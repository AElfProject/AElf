using System;
using AElf.Common;
using AElf.Contracts.MultiToken.Messages;
using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Resource.FeeReceiver
{
    /// <summary>
    /// This contracts controls how the fees collected from resource trading can be used. Current rule says
    /// 50% of the fees will be burned and the other 50% distributed to the foundation.
    /// </summary>
    public class FeeReceiverContract: FeeReceiverContractContainer.FeeReceiverContractBase
    {
        #region Views

        public override Address GetElfTokenAddress(Empty input)
        {
            return State.TokenContract.Value;
        }

        public override Address GetFoundationAddress(Empty input)
        {
            return State.FoundationAddress.Value;
        }

        public override SInt64Value GetOwedToFoundation(Empty input)
        {
            return new SInt64Value() {Value = State.OwedToFoundation.Value};
        }

        #endregion Views
        
        #region Actions

        /// <summary>
        /// Initializes this contract.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override Empty Initialize(InitializeInput input)
        {
            var initialized = State.Initialized.Value;
            Assert(!initialized, "Already initialized.");
            State.TokenContract.Value = input.ElfTokenAddress;
            State.FoundationAddress.Value = input.FoundationAddress;
            State.Initialized.Value = true;
            return new Empty();
        }

        /// <summary>
        /// Withdraw specific amount owed to the foundation. Only foundation can perform this action.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override Empty Withdraw(SInt64Value input)
        {
            var amount = input.Value;
            Assert(Context.Sender == State.FoundationAddress.Value, "Only foundation can withdraw token.");
            var owed = State.OwedToFoundation.Value;
            Assert(owed >= amount, "Too much to withdraw.");
            if (amount > 0)
            {
                State.TokenContract.Transfer.Send(new TransferInput
                {
                    To = State.FoundationAddress.Value,
                    Amount = amount,
                    Symbol = "ELF"
                });
            }
            State.OwedToFoundation.Value = owed.Sub(amount);
            return new Empty();
        }

        /// <summary>
        /// Withdraw all amount owed to the foundation. Only foundation can perform this action.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override Empty WithdrawAll(Empty input)
        {
            var owed = State.OwedToFoundation.Value;
            return Withdraw(new SInt64Value() {Value = owed});
        }

        /// <summary>
        /// Burn half of raw tokens and set the other half as owed to foundation. Anyone can perform this action.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override Empty Burn(Empty input)
        {
            var bal = State.TokenContract.GetBalance.Call(new GetBalanceInput
            {
                Owner = Context.Self,
                Symbol = "ELF"
            }).Balance;
            var owed = State.OwedToFoundation.Value;
            var preBurnAmount = bal.Sub(owed);
            var half = preBurnAmount.Div(2);
            if (half > 0)
            {
                State.TokenContract.Burn.Send(new BurnInput
                {
                    Symbol = "ELF",
                    Amount = half
                });
            }
            owed = owed.Add(half);
            State.OwedToFoundation.Value = owed;
            return new Empty();
        }

        #endregion Actions
    }
}