using System.Net;
using System.Runtime.Serialization;
namespace Domain.Models
{
    public class ErrorHandler : Exception
    {
        [IgnoreDataMember]
        public HttpStatusCode Code { get; }
        [DataMember(Name = "message")]
        public override string Message { get; }
        public object? ExceptionData { get; }
        public int InternalResponse { get; set; }
        public int Status { get; set; }
        public ErrorHandler(HttpStatusCode code, string message, object data = null, int internalResponse = 100, int status = 400)
        {
            Code = code;
            Message = message;
            ExceptionData = data;
            InternalResponse = internalResponse;
            Status = status;
        }
    }

}