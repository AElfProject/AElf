namespace AElf.WebApp.MessageQueue.Entities
{
    public interface IEventBasicInfo
    {
        public string Name { get; set; }
        public string Address { get; set; }
    }
}