using System;
using System.Collections.Generic;
using System.Linq;
using Bn254.Net;
using Nethereum.Util;

namespace ZkWasmVerifier;

public class AggregatorLib
{
    private static readonly UInt256 PMod =
        "21888242871839275222246405745257275088696311157297823662689037894645226208583";

    public static readonly UInt256 QMod =
        "21888242871839275222246405745257275088548364400416034343698204186575808495617";


    public static readonly UInt256 q_mod =
        "21888242871839275222246405745257275088548364400416034343698204186575808495617";

    public static UInt256 HashInstances(UInt256[] absorbing)
    {
        var hash = Sha3Keccack.Current.CalculateHash(absorbing.SelectMany(x => x.ToBigEndianBytes()).ToArray());
        return hash.ToUInt256() % QMod;
    }

    public static void CheckOnCurve(UInt256 x, UInt256 y)
    {
        // y^2 = x^3 + 3
        if (x.IsZero()) return;
        if (y.IsZero()) return;
        UInt256 l = y * y % PMod;
        UInt256 r = x * x % PMod;
        r = r * x % PMod;
        r = r + 3 % PMod;
        if (l != r)
        {
            throw new NotOnCurveException("Not on curve");
        }
    }

    private static (bool, UInt256 x, UInt256 y) TryMul(UInt256 x1, UInt256 y1, UInt256 s)
    {
        try
        {
            var (x, y) = Bn254.Net.Bn254.Mul(x1, y1, s);
            return (true, x, y);
        }
        catch (Exception e)
        {
            return (false, 0, 0);
        }
    }

    private static (bool, UInt256 x, UInt256 y) TryAdd(UInt256 x1, UInt256 y1, UInt256 x2, UInt256 y2)
    {
        try
        {
            var (x, y) = Bn254.Net.Bn254.Add(x1, y1, x2, y2);
            return (true, x, y);
        }
        catch (Exception e)
        {
            return (false, 0, 0);
        }
    }

    public static bool Pairing(UInt256[] input)
    {
        var elements = new List<(UInt256, UInt256, UInt256, UInt256, UInt256, UInt256)>();
        for (var i = 0; 6 * i < input.Length; i++)
        {
            var (x1, y1, x2, y2, x3, y3) = (
                input[6 * i], input[6 * i + 1],
                input[6 * i + 2], input[6 * i + 3],
                input[6 * i + 4], input[6 * i + 5]
            );
            elements.Add((x1, y1, x2, y2, x3, y3));
        }

        return Bn254.Net.Bn254.Pairing(elements.ToArray());
    }

    public static void Msm(UInt256[] input, UInt256 offset, UInt256 count)
    {
        if (count == 0)
        {
            input[offset] = 0;
            input[offset + 1] = 0;
            return;
        }

        var ret = false;
        var start = offset + count * 3 - 3;
        {
            var (ret0, x, y) = TryMul(input[start], input[start + 1], input[start + 2]);
            ret = ret0;
            input[start] = x;
            input[start + 1] = y;
        }
        // Require(ret);

        while (start != offset)
        {
            start -= 3;
            {
                var (ret0, x, y) = TryMul(input[start], input[start + 1], input[start + 2]);
                ret = ret0;
                input[start + 1] = x;
                input[start + 2] = y;
            }
            {
                var (ret0, x, y) = TryAdd(input[start + 1], input[start + 2], input[start + 3], input[start + 4]);
                ret = ret0;
                input[start] = x;
                input[start + 1] = y;
            }
        }
    }

    public static void EccMul(UInt256[] input, UInt256 offset)
    {
        if (input[offset + 2] == 1)
        {
            return;
        }

        Msm(input, offset, 1);
    }

    public static void EccMulAdd(UInt256[] input, UInt256 offset)
    {
        var ret = false;
        var p1 = offset;
        var p2 = p1 + 2;
        {
            var (ret0, x, y) = TryMul(input[p2], input[p2 + 1], input[p2 + 2]);
            ret = ret0;
            input[p2] = x;
            input[p2 + 1] = y;
        }
        {
            var (ret0, x, y) = TryAdd(input[p1], input[p1 + 1], input[p1 + 2], input[p1 + 3]);
            ret = ret0;
            input[p1] = x;
            input[p1 + 1] = y;
        }
    }

    // function ecc_mul_add(
    //     uint256[] memory input,
    // uint256 offset
    // ) internal view {
    //     bool ret = false;
    //     uint256 p1 = offset * 0x20 + 0x20;
    //     uint256 p2 = p1 + 0x40;
    //
    //     assembly {
    //         ret := staticcall(
    //             gas(),
    //             7,
    //             add(input, p2),
    //             0x60,
    //             add(input, p2),
    //             0x40
    //         )
    //     }
    //     require(ret);
    //
    //     assembly {
    //         ret := staticcall(
    //             gas(),
    //             6,
    //             add(input, p1),
    //             0x80,
    //             add(input, p1),
    //             0x40
    //         )
    //     }
    //     require(ret);
    // }

    public static UInt256 FrPow(UInt256 a, UInt256 power)
    {
        var result = Bn254.Net.Bn254.ModExp(a.ToBigEndianBytes(), power.ToBigEndianBytes(), QMod.ToBigEndianBytes());
        return new UInt256(result);
    }

    public static UInt256 FrDiv(UInt256 a, UInt256 b, UInt256 aux)
    {
        var r = b * aux % q_mod;
        if (a != r)
        {
            throw new Exception("div fail");
        }

        if (b.IsZero())
        {
            throw new Exception("div zero");
        }

        return aux % q_mod;
    }

    // function fr_div(uint256 a, uint256 b, uint256 aux) internal pure returns (uint256) {
    //     uint256 r = mulmod(b, aux, q_mod);
    //     require(a == r, "div fail");
    //     require(b != 0, "div zero");
    //     return aux % q_mod;
    // }

    // function fr_pow(uint256 a, uint256 power) internal view returns (uint256) {
    //     uint256[6] memory input;
    //     uint256[1] memory result;
    //     bool ret;
    //
    //     input[0] = 32;
    //     input[1] = 32;
    //     input[2] = 32;
    //     input[3] = a;
    //     input[4] = power;
    //     input[5] = q_mod;
    //
    //     assembly {
    //         ret := staticcall(gas(), 0x05, input, 0xc0, result, 0x20)
    //     }
    //     require(ret);
    //
    //     return result[0];
    // }
}