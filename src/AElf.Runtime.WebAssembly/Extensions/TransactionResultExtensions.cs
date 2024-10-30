using System.Text;
using AElf.Types;

namespace AElf.Runtime.WebAssembly.Extensions;

public static class TransactionResultExtensions
{
    public static List<string> GetRuntimeLogs(this TransactionResult transactionResult)
    {
        var logs = transactionResult.Logs.Where(l => l.Name == "RuntimeLog");
        return logs.Select(l => Encoding.UTF8.GetString(l.NonIndexed.ToByteArray())).ToList();
    }

    public static List<string> GetPrints(this TransactionResult transactionResult)
    {
        var prints = transactionResult.Logs.Where(l => l.Name == "Print");
        return prints.Select(p => Encoding.UTF8.GetString(p.NonIndexed.ToByteArray())).ToList();
    }

    public static List<string> GetErrorMessages(this TransactionResult transactionResult)
    {
        var errors = transactionResult.Logs.Where(l => l.Name == "ErrorMessage");
        return errors.Select(p => Encoding.UTF8.GetString(p.NonIndexed.ToByteArray())).ToList();
    }

    public static List<string> GetDebugMessages(this TransactionResult transactionResult)
    {
        var debugs = transactionResult.Logs.Where(l => l.Name == "DebugMessage");
        return debugs.Select(p => Encoding.UTF8.GetString(p.NonIndexed.ToByteArray())).ToList();
    }
}