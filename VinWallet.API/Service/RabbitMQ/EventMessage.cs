namespace HomeClean.API.Service.Implements.RabbitMQ
{
    public class EventMessage
    {
        public string EventType { get; set; }
        public string Data { get; set; }
    }
}
