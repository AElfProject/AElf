using System;
using System.Runtime.CompilerServices;
using Acs2;
using AElf.Contracts.MultiToken.Messages;
using AElf.Sdk.CSharp;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Org.BouncyCastle.Crypto.Engines;

namespace AElf.Contracts.MultiToken
{
    public partial class TokenContract
    {
        public override GetMetadataOutput GetMetadata(GetMetadataInput input)
        {
            switch (input.Method)
            {
                case nameof(Transfer):
                {
                    var p = input.Parameter.Unpack<TransferInput>();
                    return new GetMetadataOutput()
                    {
                        Resources = {Context.Self.ToByteString(), p.To.ToByteString()}
                    };
                }
                case nameof(Lock):
                {
                    var p = input.Parameter.Unpack<LockInput>();
                    return new GetMetadataOutput()
                    {
                        Resources = {Context.Self.ToByteString(), p.To.ToByteString()}
                    };
                }
                default:
                    throw new AssertionException($"invalid method: {input.Method}");
            }
        }
    }
}