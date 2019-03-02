using System;
using AElf.Common;
using AElf.Sdk.CSharp;

namespace AElf.Contracts.Resource.FeeReceiver
{
    /// <summary>
    /// This contracts controls how the fees collected from resource trading can be used. Current rule says
    /// 50% of the fees will be burned and the other 50% distributed to the foundation.
    /// </summary>
    public class FeeReceiverContract: CSharpSmartContract<FeeReceiverContractState>
    {
        #region Views

        [View]
        public string GetElfTokenAddress()
        {
            return State.TokenContract.Value.GetFormatted();
        }

        [View]
        public string GetFoundationAddress()
        {
            return State.FoundationAddress.Value.GetFormatted();
        }

        [View]
        public ulong GetOwedToFoundation()
        {
            return State.OwedToFoundation.Value;
        }

        #endregion Views
        
        #region Actions

        /// <summary>
        /// Initializes this contract.
        /// </summary>
        /// <param name="elfTokenAddress">The address of the native ELF token.</param>
        /// <param name="foundationAddress">The address of the foundation.</param>
        public void Initialize(Address elfTokenAddress, Address foundationAddress)
        {
            var initialized = State.Initialized.Value;
            Assert(!initialized, "Already initialized.");
            State.TokenContract.Value = elfTokenAddress;
            State.FoundationAddress.Value = foundationAddress;
            State.Initialized.Value = true;
        }

        /// <summary>
        /// Withdraw specific amount owed to the foundation. Only foundation can perform this action.
        /// </summary>
        /// <param name="amount"></param>
        public void Withdraw(ulong amount)
        {
            Assert(Context.Sender == State.FoundationAddress.Value, "Only foundation can withdraw token.");
            var owed = State.OwedToFoundation.Value;
            Assert(owed >= amount, "Too much to withdraw.");
            State.TokenContract.Transfer(State.FoundationAddress.Value, amount);
            State.OwedToFoundation.Value = owed.Sub(amount);
        }

        /// <summary>
        /// Withdraw all amount owed to the foundation. Only foundation can perform this action.
        /// </summary>
        public void WithdrawAll()
        {
            var owed = State.OwedToFoundation.Value;
            Withdraw(owed);
        }

        /// <summary>
        /// Burn half of raw tokens and set the other half as owed to foundation. Anyone can perform this action.
        /// </summary>
        public void Burn()
        {
            var bal = State.TokenContract.BalanceOf(Context.Self);
            var owed = State.OwedToFoundation.Value;
            var preBurnAmount = bal.Sub(owed);
            var half = preBurnAmount.Div(2);
            State.TokenContract.Burn(half);
            owed = owed.Add(half);
            State.OwedToFoundation.Value = owed;
        }

        #endregion Actions
    }
}