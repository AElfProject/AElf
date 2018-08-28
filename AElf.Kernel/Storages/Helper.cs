using System;
using AElf.Common.Extensions;
using Google.Protobuf;

// ReSharper disable InconsistentNaming
namespace AElf.Kernel.Storages
{
    public static class Helper
    {
        public static string GetKeyString(this Hash hash, uint type)
        {
            return new Key
            {
                Type = type,
                Value = ByteString.CopyFrom(hash.GetHashBytes()),
                HashType = (uint) hash.HashType
            }.ToByteArray().ToHex();
        }
    }
    
    // ReSharper disable UnusedMember.Global
    public enum Types
    {
        UInt64Value = 0,
        Hash,
        BlockBody,
        BlockHeader,
        Chain,
        Change,
        SmartContractRegistration,
        TransactionResult,
        Transaction,
        FunctionMetadata,
        SerializedCallGraph,
        SideChain,
        WorldState,
        Miners,
        BlockProducer,
        Round,
        AElfDPoSInformation,
        Int32Value,
        StringValue,
        Timestamp,
        SInt32Value
    }
}