namespace Domain.Common.Utils
{
    public static class Utils
    {
        public static DateTime CurrentDateTime()
        {
            return DateTime.Parse(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
        }
        public static DateTime ConvertDate(String date)
        {
            var culture = System.Globalization.CultureInfo.InvariantCulture;
            return DateTime.Parse(DateTime.ParseExact(date, "dd/MM/yyyy", culture).ToString("yyyy-MM-dd", culture));
        }

        public static string ConvertToDate(string date)
        {
            return Convert.ToDateTime(date).ToString("dd/MM/yyyy HH:mm:ss");
            //return date.ToString("dd/MM/yyyy HH:mm:ss");

        }

        public static class TipoPaciente
        {
            public static string Dni = "1";
            public static string CarnetExtranjeria = "4";
        }

        public static class Genero
        {
            public static string M = "MASCULINO";
            public static string F = "FEMENINO";
        }

        public static class EstadoCivil
        {
            public static string C = "CASADO";
            public static string D = "DIVORCIADO";
            public static string S = "SOLTERO";
            public static string V = "VIUDO";
        }

        public static IEnumerable<(T item, int index)> WithCustomIndex<T>(this IEnumerable<T> source) => source.Select((item, index) => (item, index + 1));

        public static string PeriodoAcortado()
        {
            var mes = DateTime.Now.Month.ToString();
            return mes.PadLeft(2, '0');

        }
        public static string AnioAcortado()
        {
            var anio = DateTime.Now.Year.ToString();
            return anio.Substring(anio.Length - 2, 2);
        }
        public static string Pad(string secuencia, int caracteres)
        {
            return secuencia.PadLeft(caracteres, '0');
        }
    }

}
