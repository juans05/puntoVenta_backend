using System.Globalization;

namespace Application.Helper
{
    public static class DateTimeHelper
    {
        private static readonly DateTime DateTimeUnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static TimeSpan Tz = new TimeSpan(-3, 0, 0);

        public static DateTimeOffset Now => DateTimeOffset.UtcNow.ToOffset(Tz);

        public static long GetUnixTimeMilliseconds(DateTime value)
        {
            return (long)(value - DateTimeUnixEpoch).TotalMilliseconds; //este 258500 es un valor casi exacto para encajar con el timestamp de react ya que hay un desface
        }

        public static string ToUnixTime(this DateTime dateTime)
        {
            DateTimeOffset dto = new DateTimeOffset(dateTime.ToUniversalTime());
            return dto.ToUnixTimeSeconds().ToString();
        }


        public static long GetUnixTime(int minutes) => DateTimeOffset.UtcNow.AddMinutes(minutes).ToUnixTimeSeconds();

        public static string DateToSpanish(int value, int desface)
        {
            return DateTime.UtcNow.AddHours(-5).AddMinutes(value)
                                               .AddSeconds(desface)//esto depende de la hora donde esta el servidor
                                               .ToString("dd/MMMM/yyyy HH:mm:ss", new CultureInfo("es-ES"));
        }

        public static string Conversion(DateTime refreshToken, int desface)
        {
            return refreshToken//aqui ya no se agrega el -5 porque viene con la hora correcta
                               .AddSeconds(desface)
                               .ToString("dd/MMMM/yyyy HH:mm:ss", new CultureInfo("es-ES"));
        }

        /// <summary>
        /// Hora local según la zona horaria del país (multi-país).
        /// Si el país no existe o no tiene TimeZone, usa la zona por defecto (-5, Perú).
        /// </summary>
        public static DateTime LocalNow(int? paisId = null)
        {
            var timezone = ResolveTimeZone(paisId);
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timezone);
        }

        /// <summary>
        /// Hora local desde una marca de tiempo UTC (multi-país).
        /// </summary>
        public static DateTime FromUtc(DateTime utc, int? paisId = null)
        {
            var timezone = ResolveTimeZone(paisId);
            return utc.Kind == DateTimeKind.Utc
                ? TimeZoneInfo.ConvertTimeFromUtc(utc, timezone)
                : TimeZoneInfo.ConvertTimeBySystemTimeZoneId(utc, timezone.Id);
        }

        private static TimeZoneInfo ResolveTimeZone(int? paisId)
        {
            // Caché estático simple por paisId; la fuente de verdad es el catálogo Pais (BD).
            // En un request real la zona se compensa desde TenantContext/ConfiguracionFiscal.
            if (paisId.HasValue && PaisTimeZones.TryGetValue(paisId.Value, out var tz))
            {
                try { return TimeZoneInfo.FindSystemTimeZoneById(tz); }
                catch (TimeZoneNotFoundException) { /* fallback */ }
            }

            return TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time"); // UTC-5 (Perú)
        }

        private static readonly Dictionary<int, string> PaisTimeZones = new()
        {
            [604] = "SA Pacific Standard Time",        // Perú (UTC-5)
            [484] = "Central Standard Time (Mexico)",   // México (UTC-6)
            [170] = "SA Pacific Standard Time",         // Colombia (UTC-5)
            [218] = "SA Pacific Standard Time",         // Ecuador (UTC-5)
        };
    }
}
