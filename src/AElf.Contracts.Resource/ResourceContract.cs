using System;
using AElf.Common;
using AElf.Contracts.MultiToken.Messages;
using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Resource
{
    public partial class ResourceContract: ResourceContractContainer.ResourceContractBase
    {
        private static readonly decimal FeeRate = new decimal(5, 0, 0, false, 3);
        
        #region Views

        public override Address GetElfTokenAddress(Empty input)
        {
            return State.TokenContract.Value;
        }

        public override Address GetFeeAddress(Empty input)
        {
            return State.FeeAddress.Value;
        }

        public override Address GetResourceControllerAddress(Empty input)
        {
            return State.ResourceControllerAddress.Value;
        }

        /// <summary>
        /// Query the converter details, i.e. the resource balance, the elf balance, the weights,
        /// which are the parameters that determine the current price of the resource using Bancor Formula.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override Converter GetConverter(ResourceId input)
        {
            return State.Converters[GetConverterKey(input.Type.ToString())];
        }

        /// <summary>
        /// Query the resource balance of a particular user.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override SInt64Value GetUserBalance(UserResourceId input)
        {
            var urk = new UserResourceKey(input.Address, ParseResourceType(input.Type.ToString()));
            return new SInt64Value() {Value = State.UserBalances[urk]};
        }

        /// <summary>
        /// Query the locked balance of a user.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override SInt64Value GetUserLockedBalance(UserResourceId input)
        {
            var urk = new UserResourceKey(input.Address, ParseResourceType(input.Type.ToString()));
            return new SInt64Value() {Value = State.LockedUserResources[urk]};
        }

        /// <summary>
        /// Query the balance of a resource held by the exchange.
        /// </summary>
        /// <param name="input"></param>
        /// <returns>The balance held by the exchange.</returns>
        public override SInt64Value GetExchangeBalance(ResourceId input)
        {
            return new SInt64Value() {Value = State.Converters[GetConverterKey(input.Type.ToString())].ResBalance};
        }

        /// <summary>
        /// Query the native ELF token balance registered in the converter for a particular resource type.
        /// </summary>
        /// <param name="input"></param>
        /// <returns>The balance of ELF held in the converter.</returns>
        public override SInt64Value GetElfBalance(ResourceId input)
        {
            return new SInt64Value() {Value = State.Converters[GetConverterKey(input.Type.ToString())].ElfBalance};
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
            var initialized = State.Initialized.Value;
            Assert(!initialized, "Already initialized.");
            State.TokenContract.Value = input.ElfTokenAddress;
            State.FeeAddress.Value = input.FeeAddress;
            State.ResourceControllerAddress.Value = input.ResourceControllerAddress;
            foreach (var resourceType in _resourceTypes)
            {
                var rt = GetConverterKey(resourceType);
                State.Converters[rt] = new Converter()
                {
                    ElfBalance = 1000000,
                    ElfWeight = 500000, // Denominated by 1,000,000
                    ResBalance = 1000000,
                    ResWeight = 500000, // Denominated by 1,000,000
                    Type = ParseResourceType(resourceType)
                };
            }

            State.Initialized.Value = true;
            return new Empty();
        }

        /// <summary>
        /// Issue new resource by resource controller.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override Empty IssueResource(ResourceAmount input)
        {
            var resourceType = input.Type.ToString();
            var delta = input.Amount;
            Assert(State.ResourceControllerAddress.Value == Context.Sender,
                "Only resource controller is allowed to perform this action.");
            AssertCorrectResourceType(resourceType);
            var rt = GetConverterKey(resourceType);
            var cvt = State.Converters[rt];
            cvt.ResBalance = cvt.ResBalance.Add(delta);
            State.Converters[rt] = cvt;
            return new Empty();
        }

        /// <summary>
        /// Buy resource from the Bancor Converter.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override Empty BuyResource(ResourceAmount input)
        {
            var resourceType = input.Type.ToString();
            var paidElf = input.Amount;
            AssertCorrectResourceType(resourceType);
            var fees = (long) (paidElf * FeeRate);
            var elfForRes = paidElf.Sub(fees);
            var payout = this.BuyResourceFromExchange(resourceType, elfForRes);
            var urk = new UserResourceKey(Context.Sender, ParseResourceType(resourceType));
            State.UserBalances[urk] = State.UserBalances[urk].Add(payout);

            if (elfForRes > 0)
            {
                State.TokenContract.TransferFrom.Send(new TransferFromInput
                {
                    From = Context.Sender,
                    To = Context.Self,
                    Amount = elfForRes,
                    Symbol = "ELF",
                    Memo = $"Buying {resourceType.ToUpper()} with {paidElf} elf tokens."
                });
            }

            if (fees > 0)
            {
                State.TokenContract.TransferFrom.Send(new TransferFromInput
                {
                    From = Context.Sender,
                    To = State.FeeAddress.Value,
                    Amount = fees,
                    Symbol = "ELF",
                    Memo = $"Charged {fees} fees for buying {resourceType.ToUpper()}"
                });
            }

            return new Empty();
        }

        /// <summary>
        /// Sell resource to the Bancor Converter.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override Empty SellResource(ResourceAmount input)
        {
            var resourceType = input.Type.ToString();
            var resToSell = input.Amount;
            var bal = GetUserBalance(new UserResourceId() {Address = Context.Sender, Type = input.Type}).Value;
            Assert(bal >= resToSell, $"Insufficient {resourceType.ToUpper()} balance.");
            AssertCorrectResourceType(resourceType);
            var elfToReceive = this.SellResourceToExchange(resourceType, resToSell);
            var fees = (long) (elfToReceive * FeeRate);
            var urk = new UserResourceKey(Context.Sender, ParseResourceType(resourceType));
            State.UserBalances[urk] = State.UserBalances[urk].Sub(resToSell);
            
            var amount = elfToReceive.Sub(fees);
            if (amount > 0)
            {
                State.TokenContract.Transfer.Send(new TransferInput
                {
                    To = Context.Sender,
                    Amount = amount,
                    Symbol = "ELF",
                    Memo = $"Selling {resToSell} {resourceType.ToUpper()}s"
                });
            }

            if (fees > 0)
            {
                State.TokenContract.Transfer.Send(new TransferInput
                {
                    To = State.FeeAddress.Value,
                    Symbol = "ELF",
                    Amount = fees,
                    Memo = $"Charged {fees} fees for selling {resourceType.ToUpper()}s"
                });
            }

            return new Empty();
        }

        /// <summary>
        /// Lock resource for chain creation.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override Empty LockResource(ResourceAmount input)
        {
            var resourceType = input.Type.ToString();
            var amount = input.Amount;
            // Transfer from user to resource controller
            var rt = ParseResourceType(resourceType);
            Transfer(Context.Sender, State.ResourceControllerAddress.Value, amount, rt);

            // Increase locked amount
            var key = new UserResourceKey(Context.Sender, rt);
            State.LockedUserResources[key] = State.LockedUserResources[key].Add(amount);
            return new Empty();
        }

        /// <summary>
        /// Unlock resource for a user. This action is restricted to resource controller only.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override Empty UnlockResource(UserResourceAmount input)
        {
            var resourceType = input.Type.ToString();
            var userAddress = input.User;
            var amount = input.Amount;
            var rca = State.ResourceControllerAddress.Value;
            Assert(Context.Sender == rca, "Only the resource controller can perform this action.");

            // Transfer from resource controller to user
            var rt = ParseResourceType(resourceType);
            Transfer(rca, userAddress, amount, rt);

            // Reduce locked amount
            var key = new UserResourceKey(userAddress, rt);
            State.LockedUserResources[key] = State.LockedUserResources[key].Sub(amount);
            return new Empty();
        }

        #endregion Actions
    }
}