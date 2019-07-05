using System;
using System.Collections;

namespace AElf.Contracts.TokenConverter
{
    public class BancorHelper
    {
        /// <summary>
        /// Get token return:
        ///    Return = (1 - (fromConnectorBalance / (fromConnectorBalance + paidAmount)) ^(fromConnectorWeight/toConnectorWeight))*toConnectorBalance
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
            {
                throw new InvalidValueException("Connector balance needs to be a positive number.");
            }

            if (paidAmount <= 0)
            {
                throw new InvalidValueException("Amount needs to be a positive number.");
            }

            decimal bf = fromConnectorBalance;
            decimal wf = fromConnectorWeight;
            decimal bt = toConnectorBalance;
            decimal wt = toConnectorWeight;
            decimal a = paidAmount;
            if (wf == wt)
            {
                // if both weights are the same, the formula can be reduced
                return (long) (bt * a / (bf + a));
            }

            decimal x = bf / (bf + a);
            decimal y = wf / wt;
            return (long) (bt * (Decimal.One - Exp(y * Ln(x))));
        }

        /// <summary>
        /// Get amount of token to pay:
        ///    amountToPay = ((toConnectorBalance / (toConnectorBalance - amountToReceive)) ^(toConnectorWeight/fromConnectorWeight) - 1)*fromConnectorBalance
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
            {
                throw new InvalidValueException("Connector balance needs to be a positive number.");
            }

            if (amountToReceive <= 0)
            {
                throw new InvalidValueException("Amount needs to be a positive number.");
            }

            decimal bf = fromConnectorBalance;
            decimal wf = fromConnectorWeight;
            decimal bt = toConnectorBalance;
            decimal wt = toConnectorWeight;
            decimal a = amountToReceive;
            if (wf == wt)
            {
                // if both weights are the same, the formula can be reduced
                return (long) (bf * a / (bt - a));
            }

            decimal x = bt / (bt - a);
            decimal y = wt / wf;
            return (long) (bf * (Exp(y * Ln(x)) - Decimal.One));
        }

        #region Exponential Helpers

        static readonly uint _LOOPS = 20; // Max = 20

        private static long[] Fact =
        {
            1L,
            1L * 2,
            1L * 2 * 3,
            1L * 2 * 3 * 4,
            1L * 2 * 3 * 4 * 5,
            1L * 2 * 3 * 4 * 5 * 6,
            1L * 2 * 3 * 4 * 5 * 6 * 7,
            1L * 2 * 3 * 4 * 5 * 6 * 7 * 8,
            1L * 2 * 3 * 4 * 5 * 6 * 7 * 8 * 9,
            1L * 2 * 3 * 4 * 5 * 6 * 7 * 8 * 9 * 10,
            1L * 2 * 3 * 4 * 5 * 6 * 7 * 8 * 9 * 10 * 11,
            1L * 2 * 3 * 4 * 5 * 6 * 7 * 8 * 9 * 10 * 11 * 12,
            1L * 2 * 3 * 4 * 5 * 6 * 7 * 8 * 9 * 10 * 11 * 12 * 13,
            1L * 2 * 3 * 4 * 5 * 6 * 7 * 8 * 9 * 10 * 11 * 12 * 13 * 14,
            1L * 2 * 3 * 4 * 5 * 6 * 7 * 8 * 9 * 10 * 11 * 12 * 13 * 14 * 15,
            1L * 2 * 3 * 4 * 5 * 6 * 7 * 8 * 9 * 10 * 11 * 12 * 13 * 14 * 15 * 16,
            1L * 2 * 3 * 4 * 5 * 6 * 7 * 8 * 9 * 10 * 11 * 12 * 13 * 14 * 15 * 16 * 17,
            1L * 2 * 3 * 4 * 5 * 6 * 7 * 8 * 9 * 10 * 11 * 12 * 13 * 14 * 15 * 16 * 17 * 18,
            1L * 2 * 3 * 4 * 5 * 6 * 7 * 8 * 9 * 10 * 11 * 12 * 13 * 14 * 15 * 16 * 17 * 18 * 19,
            1L * 2 * 3 * 4 * 5 * 6 * 7 * 8 * 9 * 10 * 11 * 12 * 13 * 14 * 15 * 16 * 17 * 18 * 19 * 20,
            //14197454024290336768L, //1L * 2 * 3 * 4 * 5 * 6 * 7 * 8 * 9 * 10 * 11 * 12 * 13 * 14 * 15 * 16 * 17 * 18 * 19 * 20 * 21,        // NOTE: Overflow during compilation
            //17196083355034583040L, //1L * 2 * 3 * 4 * 5 * 6 * 7 * 8 * 9 * 10 * 11 * 12 * 13 * 14 * 15 * 16 * 17 * 18 * 19 * 20 * 21 * 22    // NOTE: Overflow during compilation
        };

        // http://www.daimi.au.dk/~ivan/FastExpproject.pdf
        // Left to Right Binary Exponentiation
        public static decimal Pow(decimal x, uint y)
        {
            if (y == 1)
                return x;

            decimal A = 1m;
            BitArray e = new BitArray(BitConverter.GetBytes(y));
            int t = e.Count;

            for (int i = t - 1; i >= 0; --i)
            {
                A *= A;
                if (e[i] == true)
                {
                    A *= x;
                }
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
            decimal x = 1 - a;
            if (Math.Abs(x) >= 1)
                throw new Exception("must be 0 < a < 2");

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

            uint iteration = _LOOPS;
            decimal result = 1;
            while (iteration > 0)
            {
                //uint fatorial = Factorial(iteration);
                var fatorial = Fact[iteration - 1];
                result += (Pow(y, iteration) / fatorial);
                iteration--;
            }

            return result;
        }

        #endregion Exponential Helpers
    }
}