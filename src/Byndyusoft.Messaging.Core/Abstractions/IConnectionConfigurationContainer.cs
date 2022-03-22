using EasyNetQ;

namespace Byndyusoft.Messaging.Abstractions
{
    public interface IConnectionConfigurationContainer
    {
        ConnectionConfiguration ConnectionConfiguration { get; }
    }
}