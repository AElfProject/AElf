using System;

namespace AElf.Sdk.CSharp
{
    public class Event
    {
        private string _topic;
        // TODO: Add list of serializables
        public Event(string topic)
        {
            _topic = topic;
        }
    }
}
