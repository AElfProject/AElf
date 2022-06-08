using System.Collections.Generic;

namespace AElf.WebApp.MessageQueue;

public class EventHandleOptions
{
    public string Connection { get; set; }
    public string ParallelHandleQueue { get; set; }
    public List<ParallelHandleEvent> ParallelHandleEventInfo { get; set; }
}

public class ParallelHandleEvent
{
    public string ContractName { get; set; }
    public List<string> EventNames { get; set; }
}