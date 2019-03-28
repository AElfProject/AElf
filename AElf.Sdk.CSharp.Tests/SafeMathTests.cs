﻿using System;
using Shouldly;
using Xunit;

namespace AElf.Sdk.CSharp.Tests
{
    public class SafeMathTests
    {
        [Fact]
        public void Int_And_UInt_Test()
        {
            5.Mul(6).ShouldBe(30);
            (-5).Mul(6).ShouldBe(-30);
            Should.Throw<OverflowException>(() => { 5.Mul(int.MaxValue); });
            Should.Throw<OverflowException>(() => { (-5).Mul(int.MaxValue); });

            10.Div(2).ShouldBe(5);
            10.Div(-2).ShouldBe(-5);
            Should.Throw<DivideByZeroException>(() => { 5.Div(0); });
            Should.Throw<DivideByZeroException>(() => { (-5).Div(0); });

            10.Sub(5).ShouldBe(5);
            10.Sub(-5).ShouldBe(15);
            Should.Throw<OverflowException>(() => { int.MaxValue.Sub(-5); });
            Should.Throw<OverflowException>(() => { uint.MinValue.Sub(5); });

            10.Add(5).ShouldBe(15);
            10.Add(-5).ShouldBe(5);
            Should.Throw<OverflowException>(() => { int.MaxValue.Add(8); });
            Should.Throw<OverflowException>(() => { uint.MaxValue.Add(8); });
        }

        [Fact]
        public void Int64_And_UInt64_Test()
        {
            ulong number1 = 6;
            long number2 = 6;

            number1.Mul(6).ShouldBe(36UL);
            number2.Mul(6).ShouldBe(36L);
            Should.Throw<DivideByZeroException>(() => { number1.Div(0); });
            Should.Throw<DivideByZeroException>(() => { number2.Div(0); });

            number1.Div(2).ShouldBe(3UL);
            number2.Div(-2).ShouldBe(-3L);
            Should.Throw<DivideByZeroException>(() => { 5.Div(0); });
            Should.Throw<DivideByZeroException>(() => { (-5).Div(0); });

            number1.Sub(5).ShouldBe(1UL);
            number2.Sub(5).ShouldBe(1L);
            Should.Throw<OverflowException>(() => { long.MaxValue.Sub(-5); });
            Should.Throw<OverflowException>(() => { ulong.MinValue.Sub(5); });

            number1.Add(5).ShouldBe(11UL);
            number2.Add(5).ShouldBe(11L);
            Should.Throw<OverflowException>(() => { long.MaxValue.Add(8); });
            Should.Throw<OverflowException>(() => { ulong.MaxValue.Add(8); });
        }
    }
}