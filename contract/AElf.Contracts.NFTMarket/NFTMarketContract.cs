using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.NFTMarket
{
    public partial class NFTMarketContract : NFTMarketContractContainer.NFTMarketContractBase
    {
        public override Empty Initialize(InitializeInput input)
        {
            State.NFTContract.Value = input.NftContractAddress;
            State.Admin.Value = input.AdminAddress ?? Context.Sender;
            State.ServiceFeeRate.Value = input.ServiceFeeRate == 0 ? DefaultServiceFeeRate : input.ServiceFeeRate;
            State.ServiceFeeReceiver.Value = input.ServiceFeeReceiver ?? State.Admin.Value;
            State.ServiceFee.Value = input.ServiceFee == 0 ? DefaultServiceFeeAmount : input.ServiceFee;
            State.TokenContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);
            return new Empty();
        }

        public override Empty SetServiceFee(SetServiceFeeInput input)
        {
            AssertSenderIsAdmin();
            State.ServiceFeeRate.Value = input.ServiceFeeRate;
            State.ServiceFeeReceiver.Value = input.ServiceFeeReceiver;
            return new Empty();
        }

        public override Empty SetGlobalTokenWhiteList(StringList input)
        {
            AssertSenderIsAdmin();
            State.GlobalTokenWhiteList.Value = input;
            return new Empty();
        }

        private void AssertSenderIsAdmin()
        {
            Assert(State.Admin.Value != null, "Contract not initialized.");
            Assert(Context.Sender == State.Admin.Value, "No permission.");
        }
    }
}