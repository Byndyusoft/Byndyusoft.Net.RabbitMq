namespace Byndyusoft.Net.RabbitMq.Tests
{
    public class Mail
    {
    }
    
    public interface IMessage
    {
    }

    public class RawDocument : IMessage
    {
        public int Int { get; set; }
    }

    public class PoorDocument : IMessage
    {
    }

    public class EnrichedDocument : IMessage
    {
        public RawDocument RawDocument { get; set; }
    }
}
