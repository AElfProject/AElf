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
        var feeDelegatees = State.DelegateesMap[input.DelegatorAddress] ?? new TransactionFeeDelegatees();
        var delegatees = feeDelegatees.Delegatees;
        if (!delegatees.ContainsKey(Context.Sender.ToBase58()))
        {
            if (delegatees.Count() >= 128)
            {
                return new SetTransactionFeeDelegationsOutput()
                {
                    Success = false
                };
            }
            delegatees.Add(Context.Sender.ToBase58(), new TransactionFeeDelegations());
            foreach (var (key, value) in parsedData)
            {
                if (value >= 0)
                {
                    AssertValidToken(key, value);
                    delegatees[Context.Sender.ToBase58()].Delegations.Add(key,value);
                }
            }
            State.DelegateesMap[input.DelegatorAddress] = feeDelegatees;
            Context.Fire(new TransactionFeeDelegationAdded()
            {
                Caller = Context.Sender,
                Delegatee = Context.Sender,
                Delegator = input.DelegatorAddress
            });
        }
        else
        {
            //normally set
            var delegations = delegatees[Context.Sender.ToBase58()].Delegations;
            foreach (var (key, value) in parsedData)
            {
                if (value <= 0 && delegations.ContainsKey(key))
                {
                    delegations.Remove(key);
                }
                else if(value > 0)
                {
                    AssertValidToken(key, value);
                    delegations[key] = value;
                }
            }
            State.DelegateesMap[input.DelegatorAddress] = feeDelegatees;
            if (delegatees[Context.Sender.ToBase58()].Delegations.Count != 0)
                return new SetTransactionFeeDelegationsOutput()
                {
                    Success = true
                };
            State.DelegateesMap[input.DelegatorAddress].Delegatees.Remove(Context.Sender.ToBase58());
            Context.Fire(new TransactionFeeDelegationCancelled()
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
        
        var feeDelegatees = State.DelegateesMap[input.DelegatorAddress] ?? new TransactionFeeDelegatees();
        var delegatees = feeDelegatees.Delegatees;
        return delegatees.ContainsKey(input.DelegateeAddress.ToBase58())?delegatees[input.DelegateeAddress.ToBase58()]: new TransactionFeeDelegations();

    }

}