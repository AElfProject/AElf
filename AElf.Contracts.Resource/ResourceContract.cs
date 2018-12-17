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

        #endregion static 

        #region Fields

        internal Map<StringValue, ConnectorPair> ConnectorPairs = new Map<StringValue, ConnectorPair>("ConnectorPairs");
        internal MapToUInt64<UserResourceKey> UserResources = new MapToUInt64<UserResourceKey>("UserResources");
        internal MapToUInt64<UserResourceKey> LockedUserResources = new MapToUInt64<UserResourceKey>("LockedUserResources");
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

        [View]
        public ulong GetElfBalance(string resourceType)
        {
            AssertCorrectResourceType(resourceType);
            var rt = new StringValue() {Value = resourceType};
            return ConnectorPairs[rt].ElfBalance;
        }

        #endregion Views

        #region Actions

        public void Initialize(Address elfTokenAddress)
        {
            var initialized = Initialized.GetValue();
            Api.Assert(!initialized, $"Already initialized.");
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
                Address = Api.GetFromAddress(),
                Type = ParseResourceType(resourceType)
            };
            UserResources[urk] = UserResources[urk].Add(payout);
            ElfToken.TransferByUser(Api.GetContractAddress(), paidElf);
        }

        public void SellResource(string resourceType, ulong resToSell)
        {
            var bal = GetUserBalance(Api.GetFromAddress(), resourceType);
            Api.Assert(bal >= resToSell, $"Insufficient {resourceType.ToUpper()} balance.");
            AssertCorrectResourceType(resourceType);
            var elfToReceive = this.SellResourceToExchange(resourceType, resToSell);
            var urk = new UserResourceKey()
            {
                Address = Api.GetFromAddress(),
                Type = ParseResourceType(resourceType)
            };
            UserResources[urk] = UserResources[urk].Sub(resToSell);
            ElfToken.TransferByContract(Api.GetFromAddress(), elfToReceive);
        }

        public void LockResource(Address to, ulong amount, string resourceType)
        {
            AssertCorrectResourceType(resourceType);
            var urkFrom = new UserResourceKey
            {
                Address = Api.GetFromAddress(),
                Type = (ResourceType) Enum.Parse(typeof(ResourceType),
                    resourceType)
            };
            UserResources[urkFrom] = UserResources[urkFrom].Sub(amount);
            var lurkTo = new UserResourceKey
            {
                Address = to,
                Type = (ResourceType) Enum.Parse(typeof(ResourceType),
                    resourceType)
            };
            LockedUserResources[lurkTo] = UserResources[lurkTo].Add(amount);
        }
        
        public void WithdrawResource(Address to, ulong amount, string resourceType)
        {
            AssertCorrectResourceType(resourceType);
            var urkFrom = new UserResourceKey
            {
                Address = Api.GetFromAddress(),
                Type = (ResourceType) Enum.Parse(typeof(ResourceType),
                    resourceType)
            };
            UserResources[urkFrom] = UserResources[urkFrom].Add(amount);
            var lurkTo = new UserResourceKey
            {
                Address = to,
                Type = (ResourceType) Enum.Parse(typeof(ResourceType),
                    resourceType)
            };
            LockedUserResources[lurkTo] = UserResources[lurkTo].Sub(amount);
        }
        
        #endregion Actions
    }
}