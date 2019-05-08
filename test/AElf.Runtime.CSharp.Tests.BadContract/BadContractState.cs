using System;
using AElf.Sdk.CSharp.State;

namespace AElf.Runtime.CSharp.Tests.BadContract
{
    public class BadContractState : ContractState
    {
        public SingletonState<double> Double;

        public SingletonState<float> Float;

        public SingletonState<DateTime> CurrentTime;

        public SingletonState<Int32> CurrentRandom;
    }
}
