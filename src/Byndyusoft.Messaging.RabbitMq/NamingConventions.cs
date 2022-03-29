using EasyNetQ;

namespace Byndyusoft.Messaging.RabbitMq
{
    internal class NamingConventions : Conventions
    {
        public NamingConventions(ITypeNameSerializer typeNameSerializer) : base(typeNameSerializer)
        {
            ErrorQueueNamingConvention = messageInfo => $"{messageInfo.Queue}.error";
        }
    }
}