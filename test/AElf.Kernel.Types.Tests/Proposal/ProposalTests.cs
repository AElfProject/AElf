using System;
using AElf.Common;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Kernel.Types.Tests
{
    public class ProposalTests
    {
        [Fact]
        public void Get_ProposalHash()
        {
            var proposal = new Proposal
            {
                ExpiredTime = Timestamp.FromDateTimeOffset(DateTimeOffset.UtcNow),
                MultiSigAccount = Address.Generate(),
                Name = "proposal test",
                Proposer = Address.Generate(),
                TxnData = ByteString.CopyFrom(Hash.Generate().DumpByteArray()) 
            };
            
            var hash = proposal.GetHash();
            hash.ShouldNotBe(null);
        }
    }
}