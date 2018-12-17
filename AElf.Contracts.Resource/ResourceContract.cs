using System.Collections.Generic;
using System.Linq;
using AElf.Common;
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

        #endregion static 

        #region Fields

        internal Map<StringValue, Converter> Converters = new Map<StringValue, Converter>("Converters");
        internal MapToUInt64<UserResourceKey> UserBalances = new MapToUInt64<UserResourceKey>("UserBalances");
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

        [View]
        public string GetConverter(string resourceType)
        {
            AssertCorrectResourceType(resourceType);
            return Converters[new StringValue(){Value = resourceType}].ToString();
        }

        [View]
        public ulong GetUserBalance(Address address, string resourceType)
        {
            var urk = new UserResourceKey()
            {
                Address = address,
                Type = ParseResourceType(resourceType)
            };
            return UserBalances[urk];
        }

        [View]
        public ulong GetExchangeBalance(string resourceType)
        {
            AssertCorrectResourceType(resourceType);
            var rt = new StringValue() {Value = resourceType};
            return Converters[rt].ResBalance;
        }

        [View]
        public ulong GetElfBalance(string resourceType)
        {
            AssertCorrectResourceType(resourceType);
            var rt = new StringValue() {Value = resourceType};
            return Converters[rt].ElfBalance;
        }

        #endregion Views

        #region Actions

        public void Initialize(Address elfTokenAddress, Address feeAddress, Address resourceControllerAddress)
        {
            var initialized = Initialized.GetValue();
            Api.Assert(!initialized, "Already initialized.");
            ElfTokenAddress.SetValue(elfTokenAddress);
            FeeAddress.SetValue(feeAddress);
            ResourceControllerAddress.SetValue(resourceControllerAddress);
            foreach (var resourceType in ResourceTypes)
            {
                var rt = new StringValue() {Value = resourceType};
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

        public void IssueResource(string resourceType, ulong delta)
        {
            checked
            {
                Api.Assert(ResourceControllerAddress.GetValue() == Api.GetTransaction().From,
                    "Only resource controller is allowed to perform this action.");
                AssertCorrectResourceType(resourceType);
                var rt = new StringValue() {Value = resourceType};
                var cvt = Converters[rt];
                cvt.ResBalance += delta;
                Converters[rt] = cvt;
            }
        }

        public void BuyResource(string resourceType, ulong paidElf)
        {
            AssertCorrectResourceType(resourceType);
            var fees = (ulong) (paidElf * FeeRate);
            var elfForRes = paidElf - fees;
            var payout = this.BuyResourceFromExchange(resourceType, elfForRes);
            var urk = new UserResourceKey()
            {
                Address = Api.GetTransaction().From,
                Type = ParseResourceType(resourceType)
            };
            UserBalances[urk] = UserBalances[urk].Add(payout);
            ElfToken.TransferByUser(Api.GetContractAddress(), elfForRes);
            ElfToken.TransferByUser(FeeAddress.GetValue(), fees);
        }

        public void SellResource(string resourceType, ulong resToSell)
        {
            var bal = GetUserBalance(Api.GetTransaction().From, resourceType);
            Api.Assert(bal >= resToSell, $"Insufficient {resourceType.ToUpper()} balance.");
            AssertCorrectResourceType(resourceType);
            var elfToReceive = this.SellResourceToExchange(resourceType, resToSell);
            var fees = (ulong) (elfToReceive * FeeRate);
            var urk = new UserResourceKey()
            {
                Address = Api.GetTransaction().From,
                Type = ParseResourceType(resourceType)
            };
            UserBalances[urk] = UserBalances[urk].Sub(resToSell);
            ElfToken.TransferByContract(Api.GetTransaction().From, elfToReceive - fees);
            ElfToken.TransferByContract(FeeAddress.GetValue(), fees);
        }

        #endregion Actions
    }
}