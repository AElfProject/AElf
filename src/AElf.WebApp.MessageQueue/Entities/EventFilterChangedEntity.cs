using System;
using System.Collections.Generic;
using AElf.WebApp.MessageQueue.Enums;

namespace AElf.WebApp.MessageQueue.Entities
{
    public class EventFilterChangedEntity : IEventFilterEntity<Guid>
    {
        public EventFilterChangedEntity(IEventFilterEntity<Guid> eventFilterEntity, bool? isToDelete = null)
        {
            Id = eventFilterEntity.Id;
            EventDetails = eventFilterEntity.EventDetails;
            FromHeight = eventFilterEntity.FromHeight;
            ToHeight = eventFilterEntity.ToHeight;
            CurrentHeight = eventFilterEntity.CurrentHeight;
            Status = EventFilterStatus.Stopped;
            IsToDelete = false;
            if (isToDelete.HasValue)
            {
                IsToDelete = isToDelete.Value;
            }
        }

        public bool IsToDelete { get; set; }
        public Guid Id { get; }
        public List<EventDetail> EventDetails { get; set; }
        public long FromHeight { get; set; }
        public long ToHeight { get; set; }
        public long CurrentHeight { get; set; }
        public EventFilterStatus Status { get; set; }
    }
}