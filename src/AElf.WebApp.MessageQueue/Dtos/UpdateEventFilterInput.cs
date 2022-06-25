using System;
using System.Collections.Generic;
using AElf.WebApp.MessageQueue.Entities;

namespace AElf.WebApp.MessageQueue.Dtos
{
    public class UpdateEventFilterInput
    {
        public Guid Id { get; set; }
        public List<EventDetail> EventDetails { get; set; }
        public long FromHeight { get; set; }
        public long ToHeight { get; set; }
    }
}