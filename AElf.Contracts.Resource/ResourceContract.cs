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
            ResourceTypes = Enum.GetValues(typeof(UserResourceKey.Types.ResourceType))
                .Cast<UserResourceKey.Types.ResourceType>().Select(x => x.ToString()).ToList();
        }

        internal static void AssertCorrectResourceType(string resourceType)
        {
            Api.Assert(ResourceTypes.Contains(resourceType), "Incorrect resource type.");
        }

        internal static UserResourceKey.Types.ResourceType ParseResourceType(string resourceType)
        {
            AssertCorrectResourceType(resourceType);
            return (UserResourceKey.Types.ResourceType) Enum.Parse(typeof(UserResourceKey.Types.ResourceType),
                resourceType, ignoreCase: true);
        }

        #endregion static 

        #region Fields

        internal Map<StringValue, ConnectorPair> ConnectorPairs = new Map<StringValue, ConnectorPair>("ConnectorPairs");
        internal MapToUInt64<UserResourceKey> UserResources = new MapToUInt64<UserResourceKey>("UserResources");
        internal BoolField Initialized = new BoolField("Initialized");
        internal PbField<Address> ElfTokenAddress = new PbField<Address>("ElfTokenAddress");

        #endregion Fields

        #region Helpers

        private ElfTokenShim ElfToken => new ElfTokenShim(ElfTokenAddress);

        #endregion Helpers

        #region Views

        [View]
        public byte[] GetElfTokenAddress()
        {
            return ElfTokenAddress.GetValue().DumpByteArray();
        }

        [View]
        public ulong GetUserBalance(Address address, string resourceType)
        {
            var urk = new UserResourceKey()
            {
                Address = address,
                Type = ParseResourceType(resourceType)
            };
            return UserResources[urk];
        }

        [View]
        public ulong GetExchangeBalance(string resourceType)
        {
            AssertCorrectResourceType(resourceType);
            var rt = new StringValue() {Value = resourceType};
            return ConnectorPairs[rt].ResBalance;
        }

        #endregion Views

        #region Actions

        public void Initialize(Address elfTokenAddress)
        {
            var i = Initialized.GetValue();
            Api.Assert(!i, $"Already initialized {i}.");
            ElfTokenAddress.SetValue(elfTokenAddress);
            foreach (var resourceType in ResourceTypes)
            {
                var rt = new StringValue() {Value = resourceType};
                var c = ConnectorPairs[rt];
                c.ElfBalance = 1000000;
                ConnectorPairs[rt] = c;
            }

            Initialized.SetValue(true);
        }

        public void AdjustResourceCap(string resourceType, ulong newCap)
        {
            // TODO: Limit the permission to delegate nodes' multisig
            AssertCorrectResourceType(resourceType);
            var rt = new StringValue() {Value = resourceType};
            var connector = ConnectorPairs[rt];
            connector.ResBalance = newCap;
            ConnectorPairs[rt] = connector;
        }

        public void BuyResource(string resourceType, ulong paidElf)
        {
            AssertCorrectResourceType(resourceType);
            var payout = this.BuyResourceFromExchange(resourceType, paidElf);
            var urk = new UserResourceKey()
            {
                Address = Api.GetTransaction().From,
                Type = ParseResourceType(resourceType)
            };
            UserResources[urk] = UserResources[urk].Add(payout);
            ElfToken.TransferByUser(Api.GetContractAddress(), paidElf);
        }

        public void SellResource(string resourceType, ulong resToSell)
        {
            var bal = GetUserBalance(Api.GetTransaction().From, resourceType);
            Api.Assert(bal >= resToSell, "Insufficient balance.");
            AssertCorrectResourceType(resourceType);
            var elfToReceive = this.SellResourceToExchange(resourceType, resToSell);
            var urk = new UserResourceKey()
            {
                Address = Api.GetTransaction().From,
                Type = ParseResourceType(resourceType)
            };
            UserResources[urk] = UserResources[urk].Sub(resToSell);
            ElfToken.TransferByContract(Api.GetTransaction().From, elfToReceive);
        }

        #endregion Actions
    }
}