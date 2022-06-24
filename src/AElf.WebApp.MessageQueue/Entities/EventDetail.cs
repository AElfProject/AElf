using System.Collections.Generic;

namespace AElf.WebApp.MessageQueue.Entities
{
    public class EventDetail
    {
        public string Address { get; set; }
        public string ToAddress { get; set; }
        public List<string> Names { get; set; }
    }
}