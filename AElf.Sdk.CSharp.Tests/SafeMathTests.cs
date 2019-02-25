using System;
using AElf.Sdk.CSharp;
using Xunit;
using Shouldly;

namespace AElf.Sdk.CSharp.Tests
{
    public class SafeMathTests
    {
        [Fact]
        public void Int_Test()
        {
            5.Mul(6).ShouldBe(30);
            Should.Throw<OverflowException>(() => { 5.Mul(int.MaxValue); });

            10.Div(2).ShouldBe(5);
            Should.Throw<DivideByZeroException>(() => { 5.Div(0); });

            10.Sub(5).ShouldBe(5);
            Should.Throw<OverflowException>(() => { int.MaxValue.Sub(-5); });

            10.Add(5).ShouldBe(15);
            Should.Throw<OverflowException>(() => { int.MaxValue.Add(8); });
        }
    }
}