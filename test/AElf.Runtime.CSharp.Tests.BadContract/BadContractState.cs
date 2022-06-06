using System;
using AElf.Sdk.CSharp.State;

namespace AElf.Runtime.CSharp.Tests.BadContract;

public class BadContractState : ContractState
{
    public SingletonState<int> CurrentRandom;

    public SingletonState<DateTime> CurrentTime;

    public SingletonState<DateTime> CurrentTimeToday;

    public SingletonState<DateTime> CurrentTimeUtc;
    public SingletonState<double> Double;

    public SingletonState<float> Float;

    public int i;
}