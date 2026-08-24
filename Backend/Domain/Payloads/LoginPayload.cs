namespace Domain.Payloads;

public record class LoginPayload(string UserName, string Password);