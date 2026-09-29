namespace Infrastructure.Common;

// Email y telefono/celular obligatorios al CREAR un Cliente o Proveedor desde el CRUD de
// administracion (ClienteRepository.CreateCliente / ProveedorRepository.CrearProveedor). No
// aplica a los alta automaticos inline (venta con cliente nuevo, compra con proveedor por RUC,
// pedido web) -- esos construyen la entidad directo, sin pasar por estos metodos, porque ahi no
// siempre hay contacto disponible en el momento (ej. SUNAT no devuelve email/telefono del RUC).
public static class ValidacionContacto
{
    // telefonos: uno o mas campos de telefono (Proveedor tiene Telefono y Celular); basta con que
    // uno venga con dato.
    public static string? Validar(string? email, params string?[] telefonos)
    {
        if (string.IsNullOrWhiteSpace(email)) return "El email es obligatorio";
        if (!EsEmailValido(email)) return "El email no es válido";
        if (telefonos.All(string.IsNullOrWhiteSpace)) return "El teléfono/celular es obligatorio";
        return null;
    }

    private static bool EsEmailValido(string email)
    {
        try { return new System.Net.Mail.MailAddress(email.Trim()).Address == email.Trim(); }
        catch (FormatException) { return false; }
    }
}
