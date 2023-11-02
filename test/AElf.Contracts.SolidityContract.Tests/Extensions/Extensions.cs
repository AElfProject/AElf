using System;
using System.Collections.Generic;
using System.Text;

namespace AElf.Contracts.SolidityContract.Extensions;

public static class Extensions
{
    public static string IntegrateContracts(this IEnumerable<string> contracts)
    {
        var code = new StringBuilder();
        foreach (var contract in contracts)
        {
            var firstLineIndex = contract.IndexOf(new List<string>
            {
                "\nabstract contract",
                "\ncontract ",
                "\ninterface ",
                "\nlibrary "
            });

            code.Append($"{contract[firstLineIndex..]}\n");
        }

        return code.ToString();
    }

    public static int IndexOf(this string contract, IEnumerable<string> matchList)
    {
        var index = -1;
        foreach (var match in matchList)
        {
            index = contract.IndexOf(match, StringComparison.Ordinal);
            if (index != -1)
            {
                break;
            }
        }

        return index;
    }
}