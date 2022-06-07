using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Linq;
using AElf.Sdk.CSharp;

namespace AElf.Contracts.TokenConverter;

public static class BancorHelper
{
    static BancorHelper()
    {
        Fact = Array.AsReadOnly(Enumerable.Range(0, 20).Select(x => DynFact(x)).ToArray());
    }

    private static long DynFact(long number)
    {
        var fact = number == 0 ? 1 : number;
        for (var i = number - 1; i >= 1; i--) fact *= i;
        return fact;
    }

    /// <summary>
    ///     Get token return:
    ///     Return = (1 - (fromConnectorBalance / (fromConnectorBalance + paidAmount))
    ///     ^(fromConnectorWeight/toConnectorWeight))*toConnectorBalance
    /// </summary>
    /// <param name="fromConnectorBalance"></param>
    /// <param name="fromConnectorWeight"></param>
    /// <param name="toConnectorBalance"></param>
    /// <param name="toConnectorWeight"></param>
    /// <param name="paidAmount"></param>
    /// <returns></returns>
    public static long GetReturnFromPaid(long fromConnectorBalance, decimal fromConnectorWeight,
        long toConnectorBalance, decimal toConnectorWeight, long paidAmount)
    {
        if (fromConnectorBalance <= 0 || toConnectorBalance <= 0)
            throw new InvalidValueException("Connector balance needs to be a positive number.");

        if (paidAmount <= 0) throw new InvalidValueException("Amount needs to be a positive number.");

        decimal bf = fromConnectorBalance;
        var wf = fromConnectorWeight;
        decimal bt = toConnectorBalance;
        var wt = toConnectorWeight;
        decimal a = paidAmount;
        if (wf == wt)
            // if both weights are the same, the formula can be reduced
            return (long)(bt / (bf + a) * a);

        var x = bf / (bf + a);
        var y = wf / wt;
        return (long)(bt * (decimal.One - Exp(y * Ln(x))));
    }

    /// <summary>
    ///     Get amount of token to pay:
    ///     amountToPay = ((toConnectorBalance / (toConnectorBalance - amountToReceive))
    ///     ^(toConnectorWeight/fromConnectorWeight) - 1)*fromConnectorBalance
    /// </summary>
    /// <param name="fromConnectorBalance"></param>
    /// <param name="fromConnectorWeight"></param>
    /// <param name="toConnectorBalance"></param>
    /// <param name="toConnectorWeight"></param>
    /// <param name="amountToReceive"></param>
    /// <returns></returns>
    public static long GetAmountToPayFromReturn(long fromConnectorBalance, decimal fromConnectorWeight,
        long toConnectorBalance, decimal toConnectorWeight, long amountToReceive)
    {
        if (fromConnectorBalance <= 0 || toConnectorBalance <= 0)
            throw new InvalidValueException("Connector balance needs to be a positive number.");

        if (amountToReceive <= 0) throw new InvalidValueException("Amount needs to be a positive number.");

        decimal bf = fromConnectorBalance;
        var wf = fromConnectorWeight;
        decimal bt = toConnectorBalance;
        var wt = toConnectorWeight;
        decimal a = amountToReceive;
        if (wf == wt)
            try
            {
                // if both weights are the same, the formula can be reduced
                return (long)(bf / (bt - a) * a);
            }
            catch
            {
                throw new AssertionException("Insufficient account balance to deposit");
            }

        var x = bt / (bt - a);
        var y = wt / wf;
        return (long)(bf * (Exp(y * Ln(x)) - decimal.One));
    }

    #region Exponential Helpers

    private const int _LOOPS = 20; // Max = 20

    private static readonly ReadOnlyCollection<long> Fact;

    // http://www.daimi.au.dk/~ivan/FastExpproject.pdf
    // Left to Right Binary Exponentiation
    public static decimal Pow(decimal x, uint y)
    {
        if (y == 1)
            return x;

        var A = 1m;
        var e = new BitArray(y.ToBytes(false));
        var t = e.Count;

        for (var i = t - 1; i >= 0; --i)
        {
            A *= A;
            if (e[i]) A *= x;
        }

        return A;
    }

    // http://stackoverflow.com/questions/429165/raising-a-decimal-to-a-power-of-decimal
    // natural logarithm series
    private static decimal Ln(decimal a)
    {
        /*
        ln(a) = log(1-x) = - x - x^2/2 - x^3/3 - ...   (where |x| < 1)
            x: a = 1-x    =>   x = 1-a = 1 - 1.004 = -.004
        */
        var x = 1 - a;
        if (Math.Abs(x) >= 1)
            throw new InvalidValueException("must be 0 < a < 2");

        decimal result = 0;
        uint iteration = _LOOPS;
        while (iteration > 0)
        {
            result -= Pow(x, iteration) / iteration;
            iteration--;
        }

        return result;
    }


    // http://stackoverflow.com/questions/429165/raising-a-decimal-to-a-power-of-decimal
    // power series
    private static decimal Exp(decimal y)
    {
        /*
        exp(y) = 1 + y + y^2/2 + x^3/3! + y^4/4! + y^5/5! + ...
        */

        var iteration = _LOOPS;
        decimal result = 1;
        while (iteration > 0)
        {
            //uint fatorial = Factorial(iteration);
            var fatorial = Fact[iteration - 1];
            result += Pow(y, (uint)iteration) / fatorial;
            iteration--;
        }

        return result;
    }

    #endregion Exponential Helpers
}