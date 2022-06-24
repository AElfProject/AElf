using System.Collections.Generic;
using AElf.WebApp.MessageQueue.Enums;

namespace AElf.WebApp.MessageQueue.Entities
{
    public interface IEventFilterEntity<TKey>
    {
        public TKey Id { get; }
        public List<EventDetail> EventDetails { get; set; }
        public long FromHeight { get; set; }
        public long ToHeight { get; set; }
        public long CurrentHeight { get; set; }
        public EventFilterStatus Status { get; set; }
    }
}