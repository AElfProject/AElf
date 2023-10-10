using System.Collections.Generic;

namespace Solang
{
    public class EventABI
    {
        public List<EventArgABI> Args { get; set; }
        public List<string> Docs { get; set; }
        public string Label { get; set; }
    }

    public class EventArgABI
    {
        public List<string> Docs { get; set; }
        public bool Indexed { get; set; }
        public string Label { get; set; }
        public EventTypeABI Type { get; set; }
    }

    public class EventTypeABI
    {
        public List<string> DisplayName { get; set; }
        public int Type { get; set; }
    }
}