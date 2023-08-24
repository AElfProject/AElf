using AElf.Types;
using Google.Protobuf;

namespace AElf.Runtime.WebAssembly.Tests;

public static class WebAssemblyRuntimeTestConstants
{
    public static Address Alice = new()
    {
        Value = ByteString.CopyFrom(
            1, 1, 1, 1, 1, 1, 1, 1,
            1, 1, 1, 1, 1, 1, 1, 1,
            1, 1, 1, 1, 1, 1, 1, 1,
            1, 1, 1, 1, 1, 1, 1, 1)
    };

    public static Address Bob = new()
    {
        Value = ByteString.CopyFrom(
            2, 2, 2, 2, 2, 2, 2, 2,
            2, 2, 2, 2, 2, 2, 2, 2,
            2, 2, 2, 2, 2, 2, 2, 2,
            2, 2, 2, 2, 2, 2, 2, 2)
    };

    public static Address Charlie = new()
    {
        Value = ByteString.CopyFrom(
            3, 3, 3, 3, 3, 3, 3, 3,
            3, 3, 3, 3, 3, 3, 3, 3,
            3, 3, 3, 3, 3, 3, 3, 3,
            3, 3, 3, 3, 3, 3, 3, 3)
    };

    public static Address Django = new()
    {
        Value = ByteString.CopyFrom(
            4, 4, 4, 4, 4, 4, 4, 4,
            4, 4, 4, 4, 4, 4, 4, 4,
            4, 4, 4, 4, 4, 4, 4, 4,
            4, 4, 4, 4, 4, 4, 4, 4)
    };

    public static byte[] CallReturnData = { 0xDE, 0xAD, 0xBE, 0xEF };
}