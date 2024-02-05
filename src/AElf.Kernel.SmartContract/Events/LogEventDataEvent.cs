using AElf.Types;

namespace AElf.Kernel.SmartContract.Events;

public class LogEventDataEvent
{
    public Block Block { get; set; }
    public LogEvent LogEvent { get; set; }
}