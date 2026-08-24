using System.Text;

namespace Domain.Utils;

public static class DecimalExtensions
{
    static string[] unidades = { "CERO", "UN", "DOS", "TRES", "CUATRO", "CINCO", "SEIS", "SIETE", "OCHO", "NUEVE" };
    static string[] especiales = { "DIEZ", "ONCE", "DOCE", "TRECE", "CATORCE", "QUINCE", "DIECISÉIS", "DIECISIETE", "DIECIOCHO", "DIECINUEVE" };
    static string[] decenas = { "", "DIEZ", "VEINTE", "TREINTA", "CUARENTA", "CINCUENTA", "SESENTA", "SETENTA", "OCHENTA", "NOVENTA" };
    static string[] centenas = { "", "CIENTO", "DOSCIENTOS", "TRESCIENTOS", "CUATROCIENTOS", "QUINIENTOS", "SEISCIENTOS", "SETECIENTOS", "OCHOCIENTOS", "NOVECIENTOS" };

    public static string ConvertirNumeroALetras(decimal numero)
    {
        int parteEntera = (int)numero;
        int parteDecimal = (int)((numero - parteEntera) * 100);

        StringBuilder resultado = new StringBuilder();

        resultado.Append("SON "); // Agregar "SON" al inicio

        if (parteEntera == 0)
        {
            resultado.Append("CERO");
        }
        else
        {
            resultado.Append(ConstruirParteEntera(parteEntera));
        }

        if (parteDecimal > 0)
        {
            resultado.Append(" CON ");
            resultado.Append(ConstruirParteDecimal(parteDecimal));
            resultado.Append("/100");
        }
        else
        {
            resultado.Append(" CON 00/100"); // Agregar "00/100" si la parte decimal es cero
        }

        resultado.Append(" SOLES"); // Agregar "SOLES" al final

        return resultado.ToString();
    }

    static string ConstruirParteEntera(int numero)
    {
        StringBuilder parteEntera = new StringBuilder();

        if (numero >= 100)
        {
            int centena = numero / 100;
            parteEntera.Append(centenas[centena]);
            numero %= 100;

            if (numero > 0)
            {
                parteEntera.Append(" ");
            }
        }

        if (numero >= 20)
        {
            int decena = numero / 10;
            parteEntera.Append(decenas[decena]);
            numero %= 10;

            if (numero > 0)
            {
                parteEntera.Append(" Y ");
            }
        }

        if (numero > 0)
        {
            if (numero < 10)
            {
                parteEntera.Append(unidades[numero]);
            }
            else if (numero < 20)
            {
                parteEntera.Append(especiales[numero - 10]);
            }
        }

        return parteEntera.ToString();
    }

    static string ConstruirParteDecimal(int numero)
    {
        if (numero < 10)
        {
            return unidades[numero];
        }
        else if (numero < 20)
        {
            return especiales[numero - 10];
        }
        else
        {
            int decena = numero / 10;
            int unidad = numero % 10;
            return $"{decenas[decena]} Y {unidades[unidad]}";
        }
    }
}