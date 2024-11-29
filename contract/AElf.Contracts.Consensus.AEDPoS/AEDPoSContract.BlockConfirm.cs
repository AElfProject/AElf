using System.Collections.Generic;
using AElf.Contracts.Consensus.AEDPoS.LibConfirmation;
using AElf.Cryptography.Bls;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.AEDPoS;

public partial class AEDPoSContract
{
    public override Empty SyncBlsPubkey(BytesValue input)
    {
        Assert(IsCurrentMiner(Context.Sender).Value, $"{Context.Sender.ToBase58()} is not current miner.");
        State.BlsPubkeyMap[Context.Sender] = input;
        return new Empty();
    }

    public override Empty ConfirmBlock(ConfirmBlockInput input)
    {
        Assert(IsCurrentMiner(Context.Sender).Value, $"{Context.Sender.ToBase58()} is not current miner.");
        var pubkeys = new List<byte[]>();
        foreach (var signedMiner in input.SignedMiners)
        {
            Assert(IsCurrentMiner(signedMiner).Value, $"{signedMiner.ToBase58()} is not current miner.");
            var pubkey = State.BlsPubkeyMap[signedMiner];
            if (pubkey != null)
            {
                pubkeys.Add(pubkey.Value.ToByteArray());
            }
        }

        Assert(pubkeys.Count > GetMinersCount(GetCurrentRoundInformation(new Empty())).Mul(2).Div(3).Add(1),
            "Not enough miners confirmed the block.");

        var aggregatedPubkey = BlsHelper.AggregatePubkeys(pubkeys.ToArray());
        var aggregatedSignature = input.AggregatedSignature.ToByteArray();
        Assert(BlsHelper.VerifySignature(aggregatedSignature, input.ConfirmedBlock.ToByteArray(), aggregatedPubkey),
            "Failed to verify aggregated signature.");

        Context.Fire(new NewBlockConfirmed
        {
            ConfirmedBlock = input.ConfirmedBlock
        });
        return new Empty();
    }

    public override BytesValue GetBlsPubkey(Address input)
    {
        return State.BlsPubkeyMap[input];
    }
}