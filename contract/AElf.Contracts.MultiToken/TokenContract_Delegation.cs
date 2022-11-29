using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Standards.ACS1;
using AElf.Standards.ACS10;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using AElf.Contracts.MultiToken;

namespace AElf.Contracts.MultiToken;

public partial class TokenContract
{
     
    public override SetTransactionFeeDelegationsOutput SetTransactionFeeDelegations(
        SetTransactionFeeDelegationsInput input)
    {
        var parsedData = input.Delegations;
        foreach (var d in parsedData)
        {
            if (d.Value < 0)
                parsedData[d.Key] = 0;
        }
        var feeDelegatees = State.DelegateesMap[input.DelegatorAddress] ?? new TransactionFeeDelegatees();
        var delegatees = feeDelegatees.Delegatees;
        if (!delegatees.ContainsKey(Context.Sender.ToBase58()))
        {
            delegatees.Add(Context.Sender.ToBase58(), new TransactionFeeDelegations());
        }
        if(delegatees[Context.Sender.ToBase58()].Delegations.Count != 0)
        {
            if (input.Delegations.First(x => x.Value > 0).Key == null)
            {
                State.DelegateesMap[input.DelegatorAddress].Delegatees.Remove(Context.Sender.ToBase58());
                Context.Fire(new TransactionFeeDelegationCancelled()
                {
                    Caller = Context.Sender,
                    Delegatee = Context.Sender,
                    Delegator = input.DelegatorAddress
                });
            }
            State.DelegateesMap[input.DelegatorAddress].Delegatees[Context.Sender.ToBase58()] = new TransactionFeeDelegations()
            {
                Delegations = { input.Delegations}
            };
        }
        else
        {
            Assert(input.Delegations.First(x => x.Value > 0).Key != null, "Invalid input");
            Assert(delegatees.Count() < 128, "delegate count reach limit");
            delegatees[Context.Sender.ToBase58()].Delegations.Add(input.Delegations);
            Context.Fire(new TransactionFeeDelegationAdded()
            {
                Caller = Context.Sender,
                Delegatee = Context.Sender,
                Delegator = input.DelegatorAddress
            });

        }

        return new SetTransactionFeeDelegationsOutput()
        {
            Success = true
        };
    }
    
    
    
    public override Empty RemoveTransactionFeeDelegator(
        RemoveTransactionFeeDelegatorInput input)
    {

        var Delegatees = State.DelegateesMap[input.DelegatorAddress].Delegatees;
        Assert(Delegatees[Context.Sender.ToString()] != null, "Invalid input");
        State.DelegateesMap[input.DelegatorAddress].Delegatees.Remove(Context.Sender.ToString());
        Context.Fire(new TransactionFeeDelegationCancelled()
        {
            Caller = Context.Sender,
            Delegatee = Context.Sender,
            Delegator = input.DelegatorAddress
        });
        return new Empty();
    }
    
    public override Empty RemoveTransactionFeeDelegatee(
        RemoveTransactionFeeDelegateeInput input)
    {
        var Delegatees = State.DelegateesMap[Context.Sender].Delegatees;
        Assert(Delegatees[input.DelegateeAddress.ToString()] != null, "Invalid input");
        State.DelegateesMap[Context.Sender].Delegatees.Remove(input.DelegateeAddress.ToString());
        Context.Fire(new TransactionFeeDelegationCancelled()
        {
            Caller = Context.Sender,
            Delegatee = input.DelegateeAddress,
            Delegator = Context.Sender
        });
        return new Empty();
    }

    public override TransactionFeeDelegations GetDelegatorAllowance(GetDelegatorAllowanceInput input)
    {
        var Delegatees = State.DelegateesMap[input.DelegateeAddress].Delegatees;
        return new TransactionFeeDelegations()
        {
            Delegations = {Delegatees[input.DelegatorAddress.ToString()].Delegations}
        };

    }

}