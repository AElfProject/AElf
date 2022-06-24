using System;
using System.Collections.Generic;
using AElf.WebApp.MessageQueue.Enums;

namespace AElf.WebApp.MessageQueue.Entities
{
    public class EventFilterEntity : IEventFilterEntity<Guid>
    {
        public EventFilterEntity(Guid id)
        {
            Id = id;
            Status = EventFilterStatus.Stopped;
        }

        public EventFilterEntity(IEventFilterEntity<Guid> eventFilterEntity)
        {
            Id = eventFilterEntity.Id;
            FromEventFilterChanged(eventFilterEntity);
        }

        public Guid Id { get; }
        public List<EventDetail> EventDetails { get; set; }
        public long FromHeight { get; set; }
        public long ToHeight { get; set; }
        public long CurrentHeight { get; set; }
        public EventFilterStatus Status { get; set; }

        public void FromEventFilterChanged(IEventFilterEntity<Guid> eventFilterEntity)
        {
            EventDetails = eventFilterEntity.EventDetails;
            FromHeight = eventFilterEntity.FromHeight;
            ToHeight = eventFilterEntity.ToHeight;
            CurrentHeight = eventFilterEntity.CurrentHeight;
            Status = eventFilterEntity.Status;
        }

        public string GetTopic()
        {
            return $"{Id}-{FromHeight}-{ToHeight}";
        }
    }
}