using System;
using AElf.Common;
using AElf.Sdk.CSharp;

namespace AElf.Contracts.Resource
{
    public partial class ResourceContract: CSharpSmartContract<ResourceContractState>
    {
        private static readonly decimal FeeRate = new decimal(5, 0, 0, false, 3);
        
        #region Views

        [View]
        public string GetElfTokenAddress()
        {
            return State.TokenContract.Value.GetFormatted();
        }

        [View]
        public string GetFeeAddress()
        {
            return State.FeeAddress.Value.GetFormatted();
        }

        [View]
        public string GetResourceControllerAddress()
        {
            return State.ResourceControllerAddress.Value.GetFormatted();
        }

        /// <summary>
        /// Query the converter details, i.e. the resource balance, the elf balance, the weights,
        /// which are the parameters that determine the current price of the resource using Bancor Formula.
        /// </summary>
        /// <param name="resourceType">The type of the resource.</param>
        /// <returns>The json representation of the converter.</returns>
        [View]
        public string GetConverter(string resourceType)
        {
            return State.Converters[GetConverterKey(resourceType)].ToString();
        }

        /// <summary>
        /// Query the resource balance of a particular user.
        /// </summary>
        /// <param name="address">The address of the user to query balance for.</param>
        /// <param name="resourceType">The type of the resource to query for.</param>
        /// <returns></returns>
        [View]
        public ulong GetUserBalance(Address address, string resourceType)
        {
            var urk = new UserResourceKey(address, ParseResourceType(resourceType));
            return State.UserBalances[urk];
        }

        /// <summary>
        /// Query the locked balance of a user.
        /// </summary>
        /// <param name="address">The address of the user.</param>
        /// <param name="resourceType">The type of the locked resource to query for.</param>
        /// <returns></returns>
        [View]
        public ulong GetUserLockedBalance(Address address, string resourceType)
        {
            var urk = new UserResourceKey(address, ParseResourceType(resourceType));
            return State.LockedUserResources[urk];
        }

        /// <summary>
        /// Query the balance of a resource held by the exchange.
        /// </summary>
        /// <param name="resourceType">The type of the resource.</param>
        /// <returns>The balance held by the exchange.</returns>
        [View]
        public ulong GetExchangeBalance(string resourceType)
        {
            return State.Converters[GetConverterKey(resourceType)].ResBalance;
        }

        /// <summary>
        /// Query the native ELF token balance registered in the converter for a particular resource type..
        /// </summary>
        /// <param name="resourceType">The type of the resource</param>
        /// <returns>The balance of ELF held in the converter.</returns>
        [View]
        public ulong GetElfBalance(string resourceType)
        {
            return State.Converters[GetConverterKey(resourceType)].ElfBalance;
        }

        #endregion Views

        #region Actions

        /// <summary>
        /// Initialize the contract information.
        /// </summary>
        /// <param name="elfTokenAddress">The address of the native ELF token used to trade resource.</param>
        /// <param name="feeAddress">The address that receives the exchange fee.</param>
        /// <param name="resourceControllerAddress">The address of the resource controller who is in charge of issuing
        /// new resources.</param>
        public void Initialize(Address elfTokenAddress, Address feeAddress, Address resourceControllerAddress)
        {
            var initialized = State.Initialized.Value;
            Assert(!initialized, "Already initialized.");
            State.TokenContract.Value = elfTokenAddress;
            State.FeeAddress.Value = feeAddress;
            State.ResourceControllerAddress.Value = resourceControllerAddress;
            foreach (var resourceType in _resourceTypes)
            {
                var rt = GetConverterKey(resourceType);
                State.Converters[rt] = new Converter()
                {
                    ElfBalance = 1000000,
                    ElfWeight = 500000, // Denominated by 1,000,000
                    ResBalance = 1000000,
                    ResWeight = 500000, // Denominated by 1,000,000
                    ResourceType = ParseResourceType(resourceType)
                };
            }

            State.Initialized.Value = true;
        }

        /// <summary>
        /// Issue new resource by resource controller.
        /// </summary>
        /// <param name="resourceType">The type of resource to issue.</param>
        /// <param name="delta">The new amount to issue.</param>
        public void IssueResource(string resourceType, ulong delta)
        {
            Assert(State.ResourceControllerAddress.Value == Context.Sender,
                "Only resource controller is allowed to perform this action.");
            AssertCorrectResourceType(resourceType);
            var rt = GetConverterKey(resourceType);
            var cvt = State.Converters[rt];
            cvt.ResBalance = cvt.ResBalance.Add(delta);
            State.Converters[rt] = cvt;
            Context.FireEvent(new ResourceIssued()
            {
                ResourceType = Standardized(resourceType),
                IssuedAmount = delta
            });
        }

        /// <summary>
        /// Buy resource from the Bancor Converter.
        /// </summary>
        /// <param name="resourceType">The type of the resource to buy.</param>
        /// <param name="paidElf">The amount of ELF token to pay for the resource. The returned resource amount
        /// will be determined by the Bancor Formula and the converter parameters at the time of execution.</param>
        public void BuyResource(string resourceType, ulong paidElf)
        {
            AssertCorrectResourceType(resourceType);
            var fees = (ulong) (paidElf * FeeRate);
            var elfForRes = paidElf.Sub(fees);
            var payout = this.BuyResourceFromExchange(resourceType, elfForRes);
            var urk = new UserResourceKey(Context.Sender, ParseResourceType(resourceType));
            State.UserBalances[urk] = State.UserBalances[urk].Add(payout);
            State.TokenContract.TransferFrom(Context.Sender, Context.Self, elfForRes);
            State.TokenContract.TransferFrom(Context.Sender, State.FeeAddress.Value, fees);
            Context.FireEvent(new ResourceBought()
            {
                ResourceType = Standardized(resourceType),
                Buyer = Context.Sender,
                PaidElf = paidElf,
                ReceivedResource = payout
            });
        }

        /// <summary>
        /// Sell resource to the Bancor Converter.
        /// </summary>
        /// <param name="resourceType">The type of the resource to sell.</param>
        /// <param name="resToSell">The amount of the resource to sell. The returned ELF token amount
        /// will be determined by the Bancor Formula and the converter parameters at the time of execution.</param>
        public void SellResource(string resourceType, ulong resToSell)
        {
            var bal = GetUserBalance(Context.Sender, resourceType);
            Assert(bal >= resToSell, $"Insufficient {resourceType.ToUpper()} balance.");
            AssertCorrectResourceType(resourceType);
            var elfToReceive = this.SellResourceToExchange(resourceType, resToSell);
            var fees = (ulong) (elfToReceive * FeeRate);
            var urk = new UserResourceKey(Context.Sender, ParseResourceType(resourceType));
            State.UserBalances[urk] = State.UserBalances[urk].Sub(resToSell);
            State.TokenContract.Transfer(Context.Sender, elfToReceive.Sub(fees));
            State.TokenContract.Transfer(State.FeeAddress.Value, fees);
            Context.FireEvent(new ResourceSold()
            {
                ResourceType = Standardized(resourceType),
                Seller = Context.Sender,
                PaidResource = resToSell,
                ReceivedElf = elfToReceive
            });
        }

        /// <summary>
        /// Lock resource for chain creation.
        /// </summary>
        /// <param name="amount">The amount of resource to lock.</param>
        /// <param name="resourceType">The type of the resource to lock.</param>
        public void LockResource(ulong amount, string resourceType)
        {
            // Transfer from user to resource controller
            var rt = ParseResourceType(resourceType);
            Transfer(Context.Sender, State.ResourceControllerAddress.Value, amount, rt);

            // Increase locked amount
            var key = new UserResourceKey(Context.Sender, rt);
            State.LockedUserResources[key] = State.LockedUserResources[key].Add(amount);
            Context.FireEvent(new ResourceLocked()
            {
                ResourceType = Standardized(resourceType),
                User = Context.Sender,
                Amount = amount
            });
        }

        /// <summary>
        /// Unlock resource for a user. This action is restricted to resource controller only.
        /// </summary>
        /// <param name="userAddress">The address of the user to unlock the resource for.</param>
        /// <param name="amount">The amount of resource to unlock.</param>
        /// <param name="resourceType">The type of the resource to unlock.</param>
        public void UnlockResource(Address userAddress, ulong amount, string resourceType)
        {
            var rca = State.ResourceControllerAddress.Value;
            Assert(Context.Sender == rca, "Only the resource controller can perform this action.");

            // Transfer from resource controller to user
            var rt = ParseResourceType(resourceType);
            Transfer(rca, userAddress, amount, rt);

            // Reduce locked amount
            var key = new UserResourceKey(Context.Sender, rt);
            State.LockedUserResources[key] = State.LockedUserResources[key].Sub(amount);
            Context.FireEvent(new ResourceUnlocked()
            {
                ResourceType = Standardized(resourceType),
                User = userAddress,
                Amount = amount
            });
        }

        #endregion Actions
    }
}