using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Volo.Abp.EventBus;

namespace AElf.WebApp.MessageQueue;

[EventName("AElf.WebApp.MessageQueue.TransactionResultListEto")]
public class TransactionResultListEto : IBlockMessage
{
    public Dictionary<string, List<TransactionResultEto>> TransactionResults { get; set; }
    public long StartBlockNumber { get; set; }
    public long EndBlockNumber { get; set; }
    public int ChainId { get; set; }
    [JsonIgnore]
    public long Height => StartBlockNumber;
}

public class TransactionResultEto
{
    public string TransactionId { get; set; }
    public string Status { get; set; }
    public LogEventEto[] Logs { get; set; }
    public string ReturnValue { get; set; }
    public string Error { get; set; }
    public string BlockHash { get; set; }
    public long BlockNumber { get; set; }
    public DateTime BlockTime { get; set; }
    public string MethodName { get; set; }
    public string FromAddress { get; set; }
    public string ToAddress { get; set; }
}