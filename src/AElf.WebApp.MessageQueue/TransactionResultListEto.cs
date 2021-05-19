using System.Collections.Generic;

namespace AElf.WebApp.MessageQueue
{
    public class TransactionResultListEto
    {
        public Dictionary<string, TransactionResultEto> TransactionResults { get; set; }
        public long StartBlockNumber { get; set; }
        public long EndBlockNumber { get; set; }
    }

    public class TransactionResultEto
    {
        public string TransactionId { get; set; }

        public string Status { get; set; }

        public LogEventEto[] Logs { get; set; }

        public string ReturnValue { get; set; }

        public string Error { get; set; }
    }

    public class LogEventEto
    {
        public string Address { get; set; }

        public string Name { get; set; }

        public string[] Indexed { get; set; }

        public string NonIndexed { get; set; }
    }
}