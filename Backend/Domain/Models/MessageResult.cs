using System.Runtime.Serialization;

namespace Domain.Models;

[DataContract]
public class MessageResult<T>
{
    [DataMember(Name = "code")]
    public int Code { get; set; }

    [DataMember(Name = "message")]
    public string Message { get; set; }

    [DataMember(Name = "data")]
    public T Data { get; set; }

    [DataMember(Name = "status")]
    public int Status { get; set; }

    public MessageResult(int code, string message, T data, int status)
    {
        Code = code;
        Message = message;
        Data = data;
        Status = status;
    }

    public static MessageResult<T> Of(string message, T data, int? status = 200, int? code = 1) => new(code.Value, message, data, status.Value);

}

