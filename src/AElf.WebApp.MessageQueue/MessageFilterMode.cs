namespace AElf.WebApp.MessageQueue;

public enum MessageFilterMode
{
    OnlyTo,
    OnlyFrom,
    OnlyEventName,
    ToAndEventName,
    FromAndEventName,
    FromAndTo,
    All
}