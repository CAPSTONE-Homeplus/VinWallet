namespace VinWallet.Domain.Errors
{
    public class ErrorObj
    {
        // Default constructor
        public ErrorObj()
        {
            Timestamp = DateTime.UtcNow;
        }


        public ErrorObj(int code, string message) : this()
        {
            Code = code;
            Message = message;
        }


        public ErrorObj(int code, string message, string description) : this(code, message)
        {
            Description = description;
        }


        public string TraceId { get; set; }


        public int Code { get; set; }

        public string Message { get; set; }


        public string Description { get; set; }

        public DateTime Timestamp { get; set; }




    }
}