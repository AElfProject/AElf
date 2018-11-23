using System;
using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Resource
{
    public static class BancorHelpers
    {
        public static ulong BuyResourceFromExchange(this ResourceContract c, string resourceType, ulong paidElf)
        {
            ResourceContract.AssertCorrectResourceType(resourceType);
            var rt = new StringValue() {Value = resourceType};
            var connector = c.ConnectorPairs[rt];
            var tokensIssued = ToSmartToken(paidElf, connector.ElfBalance);
            var resourcePayout = FromSmartToken(tokensIssued,connector.ResBalance);
            connector.ElfBalance += paidElf;
            connector.ResBalance -= resourcePayout;
            c.ConnectorPairs[rt] = connector;
            return resourcePayout;
        }

        public static ulong SellResourceToExchange(this ResourceContract c, string resourceType, ulong paidRes)
        {
            ResourceContract.AssertCorrectResourceType(resourceType);
            var rt = new StringValue() {Value = resourceType};
            var connector = c.ConnectorPairs[rt];
            var tokensIssued = ToSmartToken(paidRes, connector.ResBalance);
            var elfPayout = FromSmartToken(tokensIssued,connector.ElfBalance);
            connector.ElfBalance -= elfPayout;
            connector.ResBalance += paidRes;
            c.ConnectorPairs[rt] = connector;
            return elfPayout;
        }
        
        private static ulong ToSmartToken(ulong connected, ulong balance)
        {
            var s = 10000000000.0;
            var c = (double) connected;
            var b = (double) balance;
            var w = 0.5;
            var tokensIssued = s * (Math.Pow(1.0 + c / b, w) - 1.0);
            return Convert.ToUInt64(tokensIssued);
        }

        private static ulong FromSmartToken(ulong destroyed, ulong balance)
        {
            var s = 10000000000.0;
            var d = (double) destroyed;
            var b = (double) balance;
            var w = 0.5;
            var payout = b * (Math.Pow(1.0 + d / s, 1.0 / w) - 1.0);
            return Convert.ToUInt64(payout);
        }
    }
}