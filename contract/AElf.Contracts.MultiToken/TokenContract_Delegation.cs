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
                if (value > 0)
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
        if (State.DelegateesMap[input.DelegatorAddress] == null)
        {
            return new Empty();
        }

        if (!State.DelegateesMap[input.DelegatorAddress].Delegatees.ContainsKey(Context.Sender.ToBase58()))
        {
            return new Empty();
        }

        var delegatees = State.DelegateesMap[input.DelegatorAddress];
        delegatees.Delegatees.Remove(Context.Sender.ToBase58());
        State.DelegateesMap[input.DelegatorAddress] = delegatees;

        Context.Fire(new TransactionFeeDelegationCancelled
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
        if (State.DelegateesMap[Context.Sender] == null)
        {
            return new Empty();
        }

        if (!State.DelegateesMap[Context.Sender].Delegatees.ContainsKey(input.DelegateeAddress.ToBase58()))
        {
            return new Empty();
        }

        var delegatees = State.DelegateesMap[Context.Sender];
        delegatees.Delegatees.Remove(input.DelegateeAddress.ToBase58());
        State.DelegateesMap[Context.Sender] = delegatees;

        Context.Fire(new TransactionFeeDelegationCancelled
        {
            Caller = Context.Sender,
            Delegatee = input.DelegateeAddress,
            Delegator = Context.Sender
        });
        return new Empty();
    }

    public override TransactionFeeDelegations GetDelegatorAllowance(GetDelegatorAllowanceInput input)
    {
        if (State.DelegateesMap[input.DelegatorAddress] == null)
        {
            return new TransactionFeeDelegations();
        }
        var feeDelegatees = State.DelegateesMap[input.DelegatorAddress];
        var delegatees = feeDelegatees.Delegatees;
        return delegatees.ContainsKey(input.DelegateeAddress.ToBase58())
            ? delegatees[input.DelegateeAddress.ToBase58()]
            : new TransactionFeeDelegations();
    }
}