using AElf.Types;

namespace AElf.Kernel.SmartContract.Events;

public class LogEventContextData
{
    public Block Block { get; set; }
    public LogEvent LogEvent { get; set; }
}