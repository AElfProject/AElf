using System;
using AElf.Common;
using AElf.Sdk.CSharp;
using AElf.Sdk.CSharp.Types;

namespace AElf.Contracts.Resource
{
    /// <summary>
    /// This contracts controls how the fees collected from resource trading can be used. Current rule says
    /// 50% of the fees will be burned and the other 50% distributed to the foundation.
    /// </summary>
    public class FeeReceiverContract : CSharpSmartContract
    {
        #region Fields

        internal BoolField Initialized = new BoolField("Initialized");
        internal PbField<Address> ElfTokenAddress = new PbField<Address>("ElfTokenAddress");
        internal PbField<Address> FoundationAddress = new PbField<Address>("FoundationAddress");
        internal UInt64Field OwedToFoundation = new UInt64Field("OwedToFoundation");

        #endregion Fields

        #region Helpers

        private ElfTokenShim ElfToken => new ElfTokenShim(ElfTokenAddress);

        #endregion Helpers

        #region Views

        [View]
        public string GetElfTokenAddress()
        {
            return ElfTokenAddress.GetValue().GetFormatted();
        }

        [View]
        public string GetFoundationAddress()
        {
            return FoundationAddress.GetValue().GetFormatted();
        }

        [View]
        public ulong GetOwedToFoundation()
        {
            return OwedToFoundation.GetValue();
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
            var initialized = Initialized.GetValue();
            Api.Assert(!initialized, "Already initialized.");
            ElfTokenAddress.SetValue(elfTokenAddress);
            FoundationAddress.SetValue(foundationAddress);
            Initialized.SetValue(true);
        }

        /// <summary>
        /// Withdraw specific amount owed to the foundation. Only foundation can perform this action.
        /// </summary>
        /// <param name="amount"></param>
        public void Withdraw(ulong amount)
        {
            Api.Assert(Api.GetFromAddress() == FoundationAddress.GetValue(), "Only foundation can withdraw token.");
            var owed = OwedToFoundation.GetValue();
            Api.Assert(owed >= amount, "Too much to withdraw.");
            ElfToken.TransferByContract(FoundationAddress.GetValue(), amount);
            OwedToFoundation.SetValue(owed.Sub(amount));
        }

        /// <summary>
        /// Withdraw all amount owed to the foundation. Only foundation can perform this action.
        /// </summary>
        public void WithdrawAll()
        {
            var owed = OwedToFoundation.GetValue();
            Withdraw(owed);
        }

        /// <summary>
        /// Burn half of raw tokens and set the other half as owed to foundation. Anyone can perform this action.
        /// </summary>
        public void Burn()
        {
            var bal = ElfToken.BalanceOf(Api.GetContractAddress());
            var owed = OwedToFoundation.GetValue();
            var preBurnAmount = bal.Sub(owed);
            var half = preBurnAmount.Div(2);
            ElfToken.Burn(half);
            owed = owed.Add(half);
            OwedToFoundation.SetValue(owed);
        }

        #endregion Actions
    }
}