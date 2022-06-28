namespace AElf.WebApp.MessageQueue.Dtos;

public class SyncInformationDto
{
    public long CurrentHeight { get; set; }
    public string State { get; set; }
}