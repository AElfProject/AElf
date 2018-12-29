using System.Collections.Generic;
using System.Linq;
using AElf.Common;
using AElf.Kernel;
using AElf.Sdk.CSharp;
using AElf.Sdk.CSharp.Types;
using Google.Protobuf.WellKnownTypes;
using Api = AElf.Sdk.CSharp.Api;
using Enum = System.Enum;

namespace AElf.Contracts.Resource
{
    public class ResourceContract : CSharpSmartContract
    {
        #region static

        internal static List<string> ResourceTypes;

        static ResourceContract()
        {
            ResourceTypes = Enum.GetValues(typeof(ResourceType))
                .Cast<ResourceType>().Select(x => x.ToString().ToUpper()).ToList();
        }

        internal static void AssertCorrectResourceType(string resourceType)
        {
            Api.Assert(ResourceTypes.Contains(resourceType.ToUpper()), "Incorrect resource type.");
        }

        internal static ResourceType ParseResourceType(string resourceType)
        {
            AssertCorrectResourceType(resourceType);
            return (ResourceType) Enum.Parse(typeof(ResourceType),
                resourceType, ignoreCase: true);
        }

        internal static StringValue GetConverterKey(string resourceType)
        {
            AssertCorrectResourceType(resourceType);
            return new StringValue() {Value = resourceType.ToUpper()};
        }

        #endregion static 

        #region Fields

        internal Map<StringValue, Converter> Converters = new Map<StringValue, Converter>("Converters");
        internal MapToUInt64<UserResourceKey> UserBalances = new MapToUInt64<UserResourceKey>("UserBalances");

        internal MapToUInt64<UserResourceKey> LockedUserResources =
            new MapToUInt64<UserResourceKey>("LockedUserResources");

        internal BoolField Initialized = new BoolField("Initialized");
        internal PbField<Address> ElfTokenAddress = new PbField<Address>("ElfTokenAddress");
        internal PbField<Address> FeeAddress = new PbField<Address>("FeeAddress");
        internal PbField<Address> ResourceControllerAddress = new PbField<Address>("ResourceControllerAddress");
        internal static readonly decimal FeeRate = new decimal(5, 0, 0, false, 3);

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
        public string GetFeeAddress()
        {
            return FeeAddress.GetValue().GetFormatted();
        }

        [View]
        public string GetResourceControllerAddress()
        {
            return ResourceControllerAddress.GetValue().GetFormatted();
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
            return Converters[GetConverterKey(resourceType)].ToString();
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
            return UserBalances[urk];
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
            return LockedUserResources[urk];
        }

        /// <summary>
        /// Query the balance of a resource held by the exchange.
        /// </summary>
        /// <param name="resourceType">The type of the resource.</param>
        /// <returns>The balance held by the exchange.</returns>
        [View]
        public ulong GetExchangeBalance(string resourceType)
        {
            return Converters[GetConverterKey(resourceType)].ResBalance;
        }

        /// <summary>
        /// Query the native ELF token balance registered in the converter for a particular resource type..
        /// </summary>
        /// <param name="resourceType">The type of the resource</param>
        /// <returns>The balance of ELF held in the converter.</returns>
        [View]
        public ulong GetElfBalance(string resourceType)
        {
            return Converters[GetConverterKey(resourceType)].ElfBalance;
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
            var initialized = Initialized.GetValue();
            Api.Assert(!initialized, "Already initialized.");
            ElfTokenAddress.SetValue(elfTokenAddress);
            FeeAddress.SetValue(feeAddress);
            ResourceControllerAddress.SetValue(resourceControllerAddress);
            foreach (var resourceType in ResourceTypes)
            {
                var rt = GetConverterKey(resourceType);
                Converters[rt] = new Converter()
                {
                    ElfBalance = 1000000,
                    ElfWeight = 500000, // Denominated by 1,000,000
                    ResBalance = 1000000,
                    ResWeight = 500000, // Denominated by 1,000,000
                    ResourceType = ParseResourceType(resourceType)
                };
            }

            Initialized.SetValue(true);
        }

        /// <summary>
        /// Issue new resource by resource controller.
        /// </summary>
        /// <param name="resourceType">The type of resource to issue.</param>
        /// <param name="delta">The new amount to issue.</param>
        public void IssueResource(string resourceType, ulong delta)
        {
            Api.Assert(ResourceControllerAddress.GetValue() == Api.GetFromAddress(),
                "Only resource controller is allowed to perform this action.");
            AssertCorrectResourceType(resourceType);
            var rt = GetConverterKey(resourceType);
            var cvt = Converters[rt];
            cvt.ResBalance = cvt.ResBalance.Add(delta);
            Converters[rt] = cvt;
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
            var urk = new UserResourceKey(Api.GetFromAddress(), ParseResourceType(resourceType));
            UserBalances[urk] = UserBalances[urk].Add(payout);
            ElfToken.TransferByUser(Api.GetContractAddress(), elfForRes);
            ElfToken.TransferByUser(FeeAddress.GetValue(), fees);
        }

        /// <summary>
        /// Sell resource to the Bancor Converter.
        /// </summary>
        /// <param name="resourceType">The type of the resource to sell.</param>
        /// <param name="resToSell">The amount of the resource to sell. The returned ELF token amount
        /// will be determined by the Bancor Formula and the converter parameters at the time of execution.</param>
        public void SellResource(string resourceType, ulong resToSell)
        {
            var bal = GetUserBalance(Api.GetFromAddress(), resourceType);
            Api.Assert(bal >= resToSell, $"Insufficient {resourceType.ToUpper()} balance.");
            AssertCorrectResourceType(resourceType);
            var elfToReceive = this.SellResourceToExchange(resourceType, resToSell);
            var fees = (ulong) (elfToReceive * FeeRate);
            var urk = new UserResourceKey(Api.GetFromAddress(), ParseResourceType(resourceType));
            UserBalances[urk] = UserBalances[urk].Sub(resToSell);
            ElfToken.TransferByContract(Api.GetFromAddress(), elfToReceive.Sub(fees));
            ElfToken.TransferByContract(FeeAddress.GetValue(), fees);
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
            Transfer(Api.GetFromAddress(), ResourceControllerAddress.GetValue(), amount, rt);

            // Increase locked amount
            var key = new UserResourceKey(Api.GetFromAddress(), rt);
            LockedUserResources[key] = LockedUserResources[key].Add(amount);
        }

        /// <summary>
        /// Unlock resource for a user. This action is restricted to resource controller only.
        /// </summary>
        /// <param name="userAddress">The address of the user to unlock the resource for.</param>
        /// <param name="amount">The amount of resource to unlock.</param>
        /// <param name="resourceType">The type of the resource to unlock.</param>
        public void UnlockResource(Address userAddress, ulong amount, string resourceType)
        {
            var rca = ResourceControllerAddress.GetValue();
            Api.Assert(Api.GetFromAddress() == rca, "Only the resource controller can perform this action.");

            // Transfer from resource controller to user
            var rt = ParseResourceType(resourceType);
            Transfer(rca, userAddress, amount, rt);

            // Reduce locked amount
            var key = new UserResourceKey(Api.GetFromAddress(), rt);
            LockedUserResources[key] = LockedUserResources[key].Sub(amount);
        }

        #endregion Actions

        #region Helpers

        private void Transfer(Address from, Address to, ulong amount, ResourceType rt)
        {
            var fromKey = new UserResourceKey(from, rt);
            var toKey = new UserResourceKey(to, rt);
            var fromBal = UserBalances[fromKey];
            var toBal = UserBalances[toKey];
            fromBal = fromBal.Sub(amount);
            toBal = toBal.Add(amount);
            UserBalances[fromKey] = fromBal;
            UserBalances[toKey] = toBal;
        }

        #endregion
    }
}