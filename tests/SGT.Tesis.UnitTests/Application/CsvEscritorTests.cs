using System.Text;
using SGT.Tesis.Application.Reportes;

namespace SGT.Tesis.UnitTests.Application;

public class CsvEscritorTests
{
    private static string Texto(byte[] bytes) => Encoding.UTF8.GetString(bytes).TrimStart('﻿');

    [Theory]
    [InlineData("simple", ',', "simple")]
    [InlineData("con,coma", ',', "\"con,coma\"")]
    [InlineData("con;punto y coma", ';', "\"con;punto y coma\"")]
    [InlineData("con;punto y coma", ',', "con;punto y coma")]
    [InlineData("dice \"hola\"", ',', "\"dice \"\"hola\"\"\"")]
    [InlineData("dos\nlíneas", ',', "\"dos\nlíneas\"")]
    [InlineData("", ',', "")]
    public void Escapa_las_celdas_segun_RFC_4180(string valor, char separador, string esperado) =>
        Assert.Equal(esperado, CsvEscritor.Celda(valor, separador));

    [Theory]
    [InlineData("=SUMA(A1:A9)")]
    [InlineData("+cmd|' /C calc'!A0")]
    [InlineData("-2+3")]
    [InlineData("@SUM(1+1)")]
    public void Neutraliza_las_formulas_que_un_tercero_pudo_escribir(string valor)
    {
        var celda = CsvEscritor.Celda(valor, ',');

        Assert.StartsWith("'", celda.TrimStart('"'), StringComparison.Ordinal);
    }

    [Fact]
    public void Escribe_UTF8_con_marca_de_orden_y_saltos_CRLF()
    {
        var bytes = CsvEscritor.Escribir([new[] { "Año", "Tesis" }, new[] { "Pérez", "1" }], SeparadorCsv.PuntoYComa);

        Assert.Equal(new byte[] { 0xEF, 0xBB, 0xBF }, bytes[..3]);
        Assert.Equal("Año;Tesis\r\nPérez;1\r\n", Texto(bytes));
    }

    [Fact]
    public void Cada_separador_usa_su_cultura_numerica()
    {
        Assert.Equal("1,5", 1.5.ToString(CsvEscritor.CulturaPara(SeparadorCsv.PuntoYComa)));
        Assert.Equal("1.5", 1.5.ToString(CsvEscritor.CulturaPara(SeparadorCsv.Coma)));
        Assert.Equal(';', CsvEscritor.Caracter(SeparadorCsv.PuntoYComa));
        Assert.Equal(',', CsvEscritor.Caracter(SeparadorCsv.Coma));
    }
}
