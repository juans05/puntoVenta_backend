using Domain.Entities;
using Domain.Payloads;
using System.Text.Json;

namespace Infrastructure.Repositories
{
    public static class ConfiguracionRentaFactory
    {
        private static readonly JsonSerializerOptions JsonWriteOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        private static readonly JsonSerializerOptions JsonReadOptions = new() { PropertyNameCaseInsensitive = true };

        public static string SerializarTurnos(List<TurnoConfigPayload>? turnos) =>
            JsonSerializer.Serialize(turnos ?? new List<TurnoConfigPayload>(), JsonWriteOptions);

        public static string SerializarTarifas(List<TarifaConfigPayload>? tarifas) =>
            JsonSerializer.Serialize(tarifas ?? new List<TarifaConfigPayload>(), JsonWriteOptions);

        public static string SerializarRecursos(List<RecursoConfigPayload>? recursos) =>
            JsonSerializer.Serialize(recursos ?? new List<RecursoConfigPayload>(), JsonWriteOptions);

        public static List<TurnoConfigPayload> DeserializarTurnos(string json) =>
            string.IsNullOrWhiteSpace(json) ? new List<TurnoConfigPayload>() : JsonSerializer.Deserialize<List<TurnoConfigPayload>>(json, JsonReadOptions) ?? new List<TurnoConfigPayload>();

        public static List<TarifaConfigPayload> DeserializarTarifas(string json) =>
            string.IsNullOrWhiteSpace(json) ? new List<TarifaConfigPayload>() : JsonSerializer.Deserialize<List<TarifaConfigPayload>>(json, JsonReadOptions) ?? new List<TarifaConfigPayload>();

        public static List<RecursoConfigPayload> DeserializarRecursos(string json) =>
            string.IsNullOrWhiteSpace(json) ? new List<RecursoConfigPayload>() : JsonSerializer.Deserialize<List<RecursoConfigPayload>>(json, JsonReadOptions) ?? new List<RecursoConfigPayload>();

        public static ConfiguracionRentaPayload ConfiguracionDefecto()
        {
            var recursos = new List<RecursoConfigPayload>();

            for (var numero = 101; numero <= 131; numero++)
                recursos.Add(new RecursoConfigPayload { Descripcion = numero.ToString(), Zona = 1, Tipo = "cuarto" });

            for (var numero = 201; numero <= 235; numero++)
                recursos.Add(new RecursoConfigPayload { Descripcion = numero.ToString(), Zona = 2, Tipo = "cuarto" });

            return new ConfiguracionRentaPayload
            {
                Tipo = "generico",
                Turnos =
                {
                    new TurnoConfigPayload { Codigo = "M", Nombre = "Primer Turno", HoraInicio = "10:00", HoraFin = "17:00" },
                    new TurnoConfigPayload { Codigo = "T", Nombre = "Segundo Turno", HoraInicio = "17:00", HoraFin = "02:00" },
                },
                Tarifas =
                {
                    new TarifaConfigPayload { Turno = "M", Dias = "1,2,3,4,5,6", Monto = 55 },
                    new TarifaConfigPayload { Turno = "T", Dias = "1,2,3", Monto = 85 },
                    new TarifaConfigPayload { Turno = "T", Dias = "4,5,6", Monto = 105 },
                    new TarifaConfigPayload { Turno = "M", Dias = "0", Monto = 85 },
                },
                Recursos = recursos,
            };
        }

        public static ConfiguracionRentaPayload ToPayload(ConfiguracionRenta configuracion) => new()
        {
            Tipo = configuracion.Tipo,
            Turnos = DeserializarTurnos(configuracion.TurnosJson),
            Tarifas = DeserializarTarifas(configuracion.TarifasJson),
            Recursos = DeserializarRecursos(configuracion.RecursosJson),
        };

        public static object ConfiguracionToDto(ConfiguracionRenta configuracion) =>
            ConfiguracionToDto(ToPayload(configuracion));

        public static object ConfiguracionToDto(ConfiguracionRentaPayload payload) => new
        {
            tipo = payload.Tipo,
            turnos = payload.Turnos.Select(t => new { codigo = t.Codigo, nombre = t.Nombre, horaInicio = t.HoraInicio, horaFin = t.HoraFin }).ToList(),
            tarifas = payload.Tarifas.Select(t => new { turno = t.Turno, dias = t.Dias, monto = t.Monto }).ToList(),
            recursos = payload.Recursos.Select(r => new { descripcion = r.Descripcion, zona = r.Zona, tipo = r.Tipo }).ToList(),
        };
    }
}