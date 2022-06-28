using System;
using System.Collections.Generic;
using Volo.Abp.EventBus;

namespace AElf.WebApp.MessageQueue;

[EventName("AElf.WebMessage.BlockMessageEto")]
public class BlockMessageEto
{
    public int ChainId { get; set; }
    public long Height { get; set; }
    public DateTime BlockTime { get; set; }
    public string BlockHash { get; set; }
    public List<TransactionMessageEto> TransactionMessageList { get;} = new List<TransactionMessageEto>();
    public string Bloom { get; set; }
}

public class TransactionMessageEto
{
    public string TransactionId { get; set; }
    public string Status { get; set; }
    public LogEventEto[] Logs { get; set; }
    public string Bloom { get; set; }
    public string ReturnValue { get; set; }
    public string Error { get; set; }
    public string MethodName { get; set; }
    public string FromAddress { get; set; }
    public string ToAddress { get; set; }
}

public class LogEventEto
{
    public string Address { get; set; }

    public string Name { get; set; }

    public string[] Indexed { get; set; }

    public string NonIndexed { get; set; }
}