using System;
using System.Buffers.Binary;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace AElf.Types
{
    [StructLayout(LayoutKind.Explicit)]
    public readonly struct UInt256Value : IComparable, IComparable<UInt256Value>, IInteger<UInt256Value>
    {
        [FieldOffset(0)] public readonly ulong U0;
        [FieldOffset(8)] public readonly ulong U1;
        [FieldOffset(16)] public readonly ulong U2;
        [FieldOffset(24)] public readonly ulong U3;

        private uint r0 => (uint) U0;
        private uint r1 => (uint) (U0 >> 32);
        private uint r2 => (uint) U1;
        private uint r3 => (uint) (U1 >> 32);

        public static readonly UInt256Value Zero = new UInt256Value(0ul);
        public static readonly UInt256Value One = new UInt256Value(1ul);

        public static readonly UInt256Value MinValue = Zero;
        public static readonly UInt256Value MaxValue = ~Zero;
        public static readonly UInt256Value UInt128MaxValue = new UInt256Value(ulong.MaxValue, ulong.MaxValue, 0, 0);
        public bool IsZero => (U0 | U1 | U2 | U3) == 0;
        public bool IsOne => ((U0 ^ 1UL) | U1 | U2 | U3) == 0;
        public bool IsZeroOrOne => ((U0 >> 1) | U1 | U2 | U3) == 0;
        public UInt256Value ZeroValue => Zero;
        public bool IsUint64 => (U1 | U2 | U3) == 0;
        public (ulong value, bool overflow) UlongWithOverflow => (U0, (U1 | U2 | U3) != 0);

        #region Ctor

        public UInt256Value(ulong u0 = 0, ulong u1 = 0, ulong u2 = 0, ulong u3 = 0)
        {
            U0 = u0;
            U1 = u1;
            U2 = u2;
            U3 = u3;
        }

        public UInt256Value(uint r0, uint r1, uint r2, uint r3, uint r4, uint r5, uint r6, uint r7)
        {
            U0 = (ulong) r1 << 32 | r0;
            U1 = (ulong) r3 << 32 | r2;
            U2 = (ulong) r5 << 32 | r4;
            U3 = (ulong) r7 << 32 | r6;
        }

        public UInt256Value(ReadOnlySpan<ulong> data, bool isBigEndian = false)
        {
            if (isBigEndian)
            {
                U3 = data[0];
                U2 = data[1];
                U1 = data[2];
                U0 = data[3];
            }
            else
            {
                U0 = data[0];
                U1 = data[1];
                U2 = data[2];
                U3 = data[3];
            }
        }

        #endregion

        #region Add

        public static void Add(in UInt256Value a, in UInt256Value b, out UInt256Value res)
        {
            var carry = 0ul;
            AddWithCarry(a.U0, b.U0, ref carry, out var res1);
            AddWithCarry(a.U1, b.U1, ref carry, out var res2);
            AddWithCarry(a.U2, b.U2, ref carry, out var res3);
            AddWithCarry(a.U3, b.U3, ref carry, out var res4);
            res = new UInt256Value(res1, res2, res3, res4);
        }

        public void Add(in UInt256Value a, out UInt256Value res) => Add(this, a, out res);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void AddWithCarry(ulong x, ulong y, ref ulong carry, out ulong sum)
        {
            sum = x + y + carry;
            carry = ((x & y) | ((x | y) & ~sum)) >> 63;
        }

        #endregion

        #region Sub

        public static void Sub(in UInt256Value a, in UInt256Value b, out UInt256Value res)
        {
            var borrow = 0ul;
            SubtractWithBorrow(a.U0, b.U0, ref borrow, out ulong res0);
            SubtractWithBorrow(a.U1, b.U1, ref borrow, out ulong res1);
            SubtractWithBorrow(a.U2, b.U2, ref borrow, out ulong res2);
            SubtractWithBorrow(a.U3, b.U3, ref borrow, out ulong res3);
            res = new UInt256Value(res0, res1, res2, res3);
        }

        public void Sub(in UInt256Value b, out UInt256Value res) => Sub(this, b, out res);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SubtractWithBorrow(ulong a, ulong b, ref ulong borrow, out ulong res)
        {
            res = a - b - borrow;
            borrow = ((~a & b) | ~(a ^ b) & res) >> 63;
        }

        public static bool SubtractUnderflow(in UInt256Value a, in UInt256Value b, out UInt256Value res)
        {
            ulong borrow = 0;
            SubtractWithBorrow(a[0], b[0], ref borrow, out ulong z0);
            SubtractWithBorrow(a[1], b[1], ref borrow, out ulong z1);
            SubtractWithBorrow(a[2], b[2], ref borrow, out ulong z2);
            SubtractWithBorrow(a[3], b[3], ref borrow, out ulong z3);
            res = new UInt256Value(z0, z1, z2, z3);
            return borrow != 0;
        }

        #endregion

        #region Mul

        public static void Mul(in UInt256Value x, in UInt256Value y, out UInt256Value res)
        {
            ref var rx = ref Unsafe.As<UInt256Value, ulong>(ref Unsafe.AsRef(in x));
            ref var ry = ref Unsafe.As<UInt256Value, ulong>(ref Unsafe.AsRef(in y));

            ulong carry;
            ulong res1, res2, res3, r0, r1, r2, r3;

            (carry, r0) = Multiply64(rx, ry);
            UmulHop(carry, Unsafe.Add(ref rx, 1), ry, out carry, out res1);
            UmulHop(carry, Unsafe.Add(ref rx, 2), ry, out carry, out res2);
            res3 = Unsafe.Add(ref rx, 3) * ry + carry;

            UmulHop(res1, rx, Unsafe.Add(ref ry, 1), out carry, out r1);
            UmulStep(res2, Unsafe.Add(ref rx, 1), Unsafe.Add(ref ry, 1), carry, out carry, out res2);
            res3 = res3 + Unsafe.Add(ref rx, 2) * Unsafe.Add(ref ry, 1) + carry;

            UmulHop(res2, rx, Unsafe.Add(ref ry, 2), out carry, out r2);
            res3 = res3 + Unsafe.Add(ref rx, 1) * Unsafe.Add(ref ry, 2) + carry;

            r3 = res3 + rx * Unsafe.Add(ref ry, 3);

            res = new UInt256Value(r0, r1, r2, r3);
        }

        public void Mul(in UInt256Value a, out UInt256Value res) => Mul(this, a, out res);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void UmulHop(ulong z, ulong x, ulong y, out ulong high, out ulong low)
        {
            (high, low) = Multiply64(x, y);
            ulong carry = 0ul;
            AddWithCarry(low, z, ref carry, out low);
            AddWithCarry(high, 0, ref carry, out high);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void UmulStep(ulong z, ulong x, ulong y, ulong carry, out ulong high, out ulong low)
        {
            (high, low) = Multiply64(x, y);
            ulong c = 0;
            AddWithCarry(low, carry, ref c, out low);
            AddWithCarry(high, 0, ref c, out high);
            c = 0;
            AddWithCarry(low, z, ref c, out low);
            AddWithCarry(high, 0, ref c, out high);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static (ulong high, ulong low) Multiply64(ulong a, ulong b)
        {
            ulong a0 = (uint) a;
            var a1 = a >> 32;
            var b0 = (uint) b;
            var b1 = b >> 32;
            var carry = a0 * b0;
            var r0 = (uint) carry;
            carry = (carry >> 32) + a0 * b1;
            var r2 = carry >> 32;
            carry = (uint) carry + a1 * b0;
            var low = carry << 32 | r0;
            var high = (carry >> 32) + r2 + a1 * b1;
            return (high, low);
        }

        #endregion

        #region Div

        public static void Div(in UInt256Value x, in UInt256Value y, out UInt256Value res)
        {
            if (y.IsZero || y > x)
            {
                res = Zero;
                return;
            }

            if (x == y)
            {
                res = One;
                return;
            }

            if (x.IsUint64)
            {
                res = new UInt256Value(x.U0 / y.U0, 0ul, 0ul, 0ul);
                return;
            }

            res = default; // initialize with zeros
            const int length = 4;
            Udivrem(ref Unsafe.As<UInt256Value, ulong>(ref res),
                ref Unsafe.As<UInt256Value, ulong>(ref Unsafe.AsRef(in x)), length, y, out UInt256Value _);
        }

        public void Div(in UInt256Value a, out UInt256Value res) => Div(this, a, out res);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Udivrem(ref ulong quot, ref ulong u, int length, in UInt256Value d, out UInt256Value rem)
        {
            int dLen = 0;
            int shift = 0;
            if (d.U3 != 0)
            {
                dLen = 4;
                shift = LeadingZeros(d.U3);
            }
            else if (d.U2 != 0)
            {
                dLen = 3;
                shift = LeadingZeros(d.U2);
            }
            else if (d.U1 != 0)
            {
                dLen = 2;
                shift = LeadingZeros(d.U1);
            }
            else if (d.U0 != 0)
            {
                dLen = 1;
                shift = LeadingZeros(d.U0);
            }

            var uLen = 0;
            for (var i = length - 1; i >= 0; i--)
            {
                if (Unsafe.Add(ref u, i) != 0)
                {
                    uLen = i + 1;
                    break;
                }
            }

            Span<ulong> un = stackalloc ulong[uLen + 1];
            un[uLen] = Rsh(Unsafe.Add(ref u, uLen - 1), 64 - shift);
            for (int i = uLen - 1; i > 0; i--)
            {
                un[i] = Lsh(Unsafe.Add(ref u, i), shift) | Rsh(Unsafe.Add(ref u, i - 1), 64 - shift);
            }

            un[0] = Lsh(u, shift);

            // TODO: Skip the highest word of numerator if not significant.

            if (dLen == 1)
            {
                var dnn0 = Lsh(d.U0, shift);
                ulong r = UdivremBy1(ref quot, un, dnn0);
                r = Rsh(r, shift);
                rem = new UInt256Value(r, 0ul, 0ul, 0ul);
                return;
            }

            ulong dn0 = Lsh(d.U0, shift);
            ulong dn1 = 0;
            ulong dn2 = 0;
            ulong dn3 = 0;
            switch (dLen)
            {
                case 4:
                    dn3 = Lsh(d.U3, shift) | Rsh(d.U2, 64 - shift);
                    goto case 3;
                case 3:
                    dn2 = Lsh(d.U2, shift) | Rsh(d.U1, 64 - shift);
                    goto case 2;
                case 2:
                    dn1 = Lsh(d.U1, shift) | Rsh(d.U0, 64 - shift);
                    break;
            }

            Span<ulong> dnS = stackalloc ulong[4] {dn0, dn1, dn2, dn3};
            dnS = dnS.Slice(0, dLen);

            UdivremKnuth(ref quot, un, dnS);

            ulong rem0 = 0, rem1 = 0, rem2 = 0, rem3 = 0;
            switch (dLen)
            {
                case 1:
                    rem0 = Rsh(un[dLen - 1], shift);
                    goto r0;
                case 2:
                    rem1 = Rsh(un[dLen - 1], shift);
                    goto r1;
                case 3:
                    rem2 = Rsh(un[dLen - 1], shift);
                    goto r2;
                case 4:
                    rem3 = Rsh(un[dLen - 1], shift);
                    goto r3;
            }

            r3:
            rem2 = Rsh(un[2], shift) | Lsh(un[3], 64 - shift);
            r2:
            rem1 = Rsh(un[1], shift) | Lsh(un[2], 64 - shift);
            r1:
            rem0 = Rsh(un[0], shift) | Lsh(un[1], 64 - shift);
            r0:

            rem = new UInt256Value(rem0, rem1, rem2, rem3);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int LeadingZeros(ulong x) => 64 - Len64(x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Len64(ulong x)
        {
            int n = 0;
            if (x >= (1ul << 32))
            {
                x >>= 32;
                n = 32;
            }

            if (x >= (1ul << 16))
            {
                x >>= 16;
                n += 16;
            }

            if (x >= (1ul << 8))
            {
                x >>= 8;
                n += 8;
            }

            return n + len8tab[x];
        }

        private static readonly byte[] len8tab =
        {
            0x00, 0x01, 0x02, 0x02, 0x03, 0x03, 0x03, 0x03, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04,
            0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05,
            0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06,
            0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06,
            0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07,
            0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07,
            0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07,
            0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07,
            0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08,
            0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08,
            0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08,
            0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08,
            0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08,
            0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08,
            0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08,
            0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08,
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ulong Lsh(ulong a, int n)
        {
            var n1 = n >> 1;
            var n2 = n - n1;
            return (a << n1) << n2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ulong Rsh(ulong a, int n)
        {
            var n1 = n >> 1;
            var n2 = n - n1;
            return (a >> n1) >> n2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong UdivremBy1(ref ulong quot, Span<ulong> u, ulong d)
        {
            var reciprocal = Reciprocal2by1(d);
            ulong rem;
            rem = u[u.Length - 1]; // Set the top word as remainder.
            for (int j = u.Length - 2; j >= 0; j--)
            {
                (Unsafe.Add(ref quot, j), rem) = Udivrem2by1(rem, u[j], d, reciprocal);
            }

            return rem;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong Reciprocal2by1(ulong d)
        {
            var (reciprocal, _) = Div64(~d, ~((ulong) 0), d);
            return reciprocal;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static (ulong quot, ulong rem) Udivrem2by1(ulong uh, ulong ul, ulong d, ulong reciprocal)
        {
            (ulong qh, ulong ql) = Multiply64(reciprocal, uh);
            ulong carry = 0;
            AddWithCarry(ql, ul, ref carry, out ql);
            AddWithCarry(qh, uh, ref carry, out qh);
            qh++;

            var r = ul - qh * d;

            if (r > ql)
            {
                qh--;
                r += d;
            }

            if (r >= d)
            {
                qh++;
                r -= d;
            }

            return (qh, r);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static (ulong quo, ulong rem) Div64(ulong hi, ulong lo, ulong y)
        {
            const ulong two32 = ((ulong) 1) << 32;
            const ulong mask32 = two32 - 1;
            if (y == 0)
            {
                throw new DivideByZeroException("y == 0");
            }

            if (y <= hi)
            {
                throw new OverflowException("y <= hi");
            }

            var s = LeadingZeros(y);
            y <<= s;

            ulong yn1 = y >> 32;
            ulong yn0 = y & mask32;
            ulong un32 = Lsh(hi, s) | Rsh(lo, (64 - s));
            ulong un10 = Lsh(lo, s);
            ulong un1 = un10 >> 32;
            ulong un0 = un10 & mask32;
            ulong q1 = un32 / yn1;
            ulong rhat = un32 - q1 * yn1;

            for (; q1 >= two32 || q1 * yn0 > two32 * rhat + un1;)
            {
                q1--;
                rhat += yn1;
                if (rhat >= two32)
                {
                    break;
                }
            }

            ulong un21 = un32 * two32 + un1 - q1 * y;
            ulong q0 = un21 / yn1;
            rhat = un21 - q0 * yn1;

            for (; q0 >= two32 || q0 * yn0 > two32 * rhat + un0;)
            {
                q0--;
                rhat += yn1;
                if (rhat >= two32)
                {
                    break;
                }
            }

            return (q1 * two32 + q0, Rsh((un21 * two32 + un0 - q0 * y), s));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void UdivremKnuth(ref ulong quot, Span<ulong> u, Span<ulong> d)
        {
            var dh = d[d.Length - 1];
            var dl = d[d.Length - 2];
            var reciprocal = Reciprocal2by1(dh);

            for (int j = u.Length - d.Length - 1; j >= 0; j--)
            {
                var U2 = u[j + d.Length];
                var U1 = u[j + d.Length - 1];
                var U0 = u[j + d.Length - 2];

                ulong qhat, rhat;
                if (U2 >= dh)
                {
                    qhat = ~((ulong) 0);
                    // TODO: Add "qhat one to big" adjustment (not needed for correctness, but helps avoiding "add back" case).
                }
                else
                {
                    (qhat, rhat) = Udivrem2by1(U2, U1, dh, reciprocal);
                    (ulong ph, ulong pl) = Multiply64(qhat, dl);
                    if (ph > rhat || (ph == rhat && pl > U0))
                    {
                        qhat--;
                        // TODO: Add "qhat one to big" adjustment (not needed for correctness, but helps avoiding "add back" case).
                    }
                }

                // Multiply and subtract.
                var borrow = SubMulTo(u.Slice(j), d, qhat);
                u[j + d.Length] = U2 - borrow;
                if (U2 < borrow)
                {
                    // Too much subtracted, add back.
                    qhat--;
                    u[j + d.Length] += AddTo(u.Slice(j), d);
                }

                Unsafe.Add(ref quot, j) = qhat; // Store quotient digit.
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong AddTo(Span<ulong> x, Span<ulong> y)
        {
            ulong carry = 0;
            for (int i = 0; i < y.Length; i++)
            {
                AddWithCarry(x[i], y[i], ref carry, out x[i]);
            }

            return carry;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong SubMulTo(Span<ulong> x, Span<ulong> y, ulong multiplier)
        {
            ulong borrow = 0;
            for (int i = 0; i < y.Length; i++)
            {
                ulong s = 0, borrow1 = 0;
                SubtractWithBorrow(x[i], borrow, ref borrow1, out s);
                (ulong ph, ulong pl) = Multiply64(y[i], multiplier);
                ulong t = 0, borrow2 = 0;
                SubtractWithBorrow(s, pl, ref borrow2, out t);
                x[i] = t;
                borrow = ph + borrow1 + borrow2;
            }

            return borrow;
        }

        #endregion

        #region Mod

        public static void Mod(in UInt256Value x, in UInt256Value y, out UInt256Value res)
        {
            if (x.IsZero || y.IsZeroOrOne)
            {
                res = Zero;
                return;
            }

            switch (x.CompareTo(y))
            {
                case -1:
                    res = x;
                    return;
                case 0:
                    res = Zero;
                    return;
            }

            if (x.IsUint64)
            {
                res = new UInt256Value(x.U0 % y.U0, 0ul, 0ul, 0ul);
                return;
            }

            const int length = 4;
            Span<ulong> quot = stackalloc ulong[length];
            Udivrem(ref MemoryMarshal.GetReference(quot), ref Unsafe.As<UInt256Value, ulong>(ref Unsafe.AsRef(in x)),
                length, y, out res);
        }

        public void Mod(in UInt256Value m, out UInt256Value res) => Mod(this, m, out res);

        #endregion

        #region Operators

        #region Or

        public static void Or(in UInt256Value a, in UInt256Value b, out UInt256Value res)
        {
            res = new UInt256Value(a.U0 | b.U0, a.U1 | b.U1, a.U2 | b.U2, a.U3 | b.U3);
        }

        public static UInt256Value operator |(in UInt256Value a, in UInt256Value b)
        {
            Or(a, b, out UInt256Value res);
            return res;
        }

        #endregion

        #region And

        public static void And(in UInt256Value a, in UInt256Value b, out UInt256Value res)
        {
            res = new UInt256Value(a.U0 & b.U0, a.U1 & b.U1, a.U2 & b.U2, a.U3 & b.U3);
        }

        public static UInt256Value operator &(in UInt256Value a, in UInt256Value b)
        {
            And(a, b, out var res);
            return res;
        }

        #endregion

        #region Xor

        public static void Xor(in UInt256Value a, in UInt256Value b, out UInt256Value res)
        {
            res = new UInt256Value(a.U0 ^ b.U0, a.U1 ^ b.U1, a.U2 ^ b.U2, a.U3 ^ b.U3);
        }

        public static UInt256Value operator ^(in UInt256Value a, in UInt256Value b)
        {
            Xor(a, b, out UInt256Value res);
            return res;
        }

        #endregion

        #region Not

        public static void Not(in UInt256Value a, out UInt256Value res)
        {
            ulong U0 = ~a.U0;
            ulong U1 = ~a.U1;
            ulong U2 = ~a.U2;
            ulong U3 = ~a.U3;
            res = new UInt256Value(U0, U1, U2, U3);
        }

        public static UInt256Value operator ~(in UInt256Value a)
        {
            Not(in a, out var res);
            return res;
        }

        #endregion

        #region + - * /

        public static UInt256Value operator +(in UInt256Value a, in UInt256Value b)
        {
            Add(in a, in b, out var res);
            return res;
        }

        public static UInt256Value operator ++(UInt256Value a)
        {
            Add(in a, new UInt256Value(1, 0, 0, 0), out var res);
            return res;
        }

        public static UInt256Value operator -(in UInt256Value a, in UInt256Value b)
        {
            if (SubtractUnderflow(in a, in b, out var c))
            {
                throw new ArithmeticException($"Underflow in subtraction {a} - {b}");
            }

            return c;
        }

        public static UInt256Value operator *(UInt256Value a, UInt256Value b)
        {
            Mul(in a, in b, out var c);
            return c;
        }

        public static UInt256Value operator /(UInt256Value a, UInt256Value b)
        {
            Div(in a, in b, out var c);
            return c;
        }

        #endregion

        public static bool operator ==(in UInt256Value a, in UInt256Value b) => a.Equals(b);

        public static bool operator !=(in UInt256Value a, in UInt256Value b) => !(a == b);

        public static bool operator <(in UInt256Value a, in UInt256Value b)
        {
            return LessThan(in a, in b);
        }

        public static bool operator >(in UInt256Value a, in UInt256Value b)
        {
            return LessThan(in a, in b);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool LessThan(in UInt256Value a, in UInt256Value b)
        {
            if (a.U3 != b.U3)
                return a.U3 < b.U3;
            if (a.U2 != b.U2)
                return a.U2 < b.U2;
            if (a.U1 != b.U1)
                return a.U1 < b.U1;
            return a.U0 < b.U0;
        }

        #endregion

        public ulong this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0:
                        return U0;
                    case 1:
                        return U1;
                    case 2:
                        return U2;
                    case 3:
                        return U3;
                }

                throw new IndexOutOfRangeException();
            }
        }

        public int CompareTo(object obj)
        {
            if (obj is UInt256Value value)
                return CompareTo(value);

            throw new InvalidOperationException();
        }

        public int CompareTo(UInt256Value other)
        {
            if (this < other)
            {
                return -1;
            }

            return Equals(other) ? 0 : 1;
        }

        public static explicit operator ulong(UInt256Value a)
        {
            if (a.U1 > 0 || a.U2 > 0 || a.U3 > 0)
            {
                throw new OverflowException("Cannot convert UInt256Value to ulong.");
            }

            return a.U0;
        }

        public static implicit operator UInt256Value(ulong value) => new UInt256Value(value);

        public static explicit operator BigInteger(UInt256Value value)
        {
            Span<byte> bytes = stackalloc byte[32];
            BinaryPrimitives.WriteUInt64LittleEndian(bytes.Slice(0, 8), value.U0);
            BinaryPrimitives.WriteUInt64LittleEndian(bytes.Slice(8, 8), value.U1);
            BinaryPrimitives.WriteUInt64LittleEndian(bytes.Slice(16, 8), value.U2);
            BinaryPrimitives.WriteUInt64LittleEndian(bytes.Slice(24, 8), value.U3);
            return new BigInteger(bytes.ToArray());
        }

        public override string ToString()
        {
            return ((BigInteger) this).ToString();
        }
    }
}