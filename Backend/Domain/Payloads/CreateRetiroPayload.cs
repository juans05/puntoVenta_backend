namespace Domain.Payloads;

public record CreateRetiroPayload(int CajaId, decimal Monto, string Motivo);
