namespace AElf.WebApp.MessageQueue.Dtos;

public class SetWorkerInput
{
    public int? Period { get; set; }
    public int? BlockCountPerPeriod { get; set; }
    
    public int? ParallelCount { get; set; }
}