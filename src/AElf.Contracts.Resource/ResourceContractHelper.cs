using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AElf.Common;
using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;
using Enum = System.Enum;

namespace AElf.Contracts.Resource
{
    public partial class ResourceContract
    {
        private static readonly List<string> _resourceTypes = Enum.GetValues(typeof(ResourceType))
            .Cast<ResourceType>().Select(x => x.ToString().ToUpper()).ToList();

        private void AssertCorrectResourceType(string resourceType)
        {
            Assert(_resourceTypes.Contains(resourceType.ToUpper()), "Incorrect resource type.");
        }

        private ResourceType ParseResourceType(string resourceType)
        {
            AssertCorrectResourceType(resourceType);
            return (ResourceType) Enum.Parse(typeof(ResourceType),
                resourceType, ignoreCase: true);
        }

        private string Standardized(string resourceType)
        {
            return ParseResourceType(resourceType).ToString();
        }

        private StringValue GetConverterKey(string resourceType)
        {
            AssertCorrectResourceType(resourceType);
            return new StringValue() {Value = resourceType.ToUpper()};
        }

        private void Transfer(Address from, Address to, long amount, ResourceType rt)
        {
            var fromKey = new UserResourceKey(from, rt);
            var toKey = new UserResourceKey(to, rt);
            var fromBal = State.UserBalances[fromKey];
            var toBal = State.UserBalances[toKey];
            fromBal = fromBal.Sub(amount);
            toBal = toBal.Add(amount);
            State.UserBalances[fromKey] = fromBal;
            State.UserBalances[toKey] = toBal;
        }
        
        private static readonly decimal MaxWeight = 1000000m;

        public long BuyResourceFromExchange(string resourceType, long paidElf)
        {
            checked
            {
                AssertCorrectResourceType(resourceType);
                var rt = GetConverterKey(resourceType);
                var cvt = State.Converters[GetConverterKey(resourceType)];
                var resourcePayout = BancorHelpers.CalculateCrossConnectorReturn(
                    cvt.ElfBalance, cvt.ElfWeight,
                    cvt.ResBalance, cvt.ResWeight,
                    paidElf);
                cvt.ElfBalance += paidElf;
                cvt.ResBalance -= resourcePayout;
                State.Converters[rt] = cvt;
                return resourcePayout;
            }
        }

        public long SellResourceToExchange(string resourceType, long paidRes)
        {
            checked
            {
                AssertCorrectResourceType(resourceType);
                var rt = GetConverterKey(resourceType);
                var cvt = State.Converters[rt];
                var elfPayout = BancorHelpers.CalculateCrossConnectorReturn(
                    cvt.ResBalance, cvt.ResWeight,
                    cvt.ElfBalance, cvt.ElfWeight,
                    paidRes);
                cvt.ElfBalance -= elfPayout;
                cvt.ResBalance += paidRes;
                State.Converters[rt] = cvt;
                return elfPayout;
            }
        }
    }
}