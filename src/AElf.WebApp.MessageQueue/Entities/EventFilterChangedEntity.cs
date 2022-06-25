using System;
using System.Collections.Generic;
using AElf.WebApp.MessageQueue.Enums;

namespace AElf.WebApp.MessageQueue.Entities
{
    public class EventFilterChangedEntity : IEventFilterEntity<Guid>
    {
        public EventFilterChangedEntity(Guid id)
        {
            Id = id;
            Status = EventFilterStatus.Stopped;
        }

        public EventFilterOperate OperateType { get; set; }
        public Guid Id { get; }
        public List<EventDetail> EventDetails { get; set; }
        public long FromHeight { get; set; }
        public long ToHeight { get; set; }
        public long CurrentHeight { get; set; }
        public EventFilterStatus Status { get; set; }
    }
}