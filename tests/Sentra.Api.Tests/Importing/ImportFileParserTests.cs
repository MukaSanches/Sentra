using System.Text;
using ClosedXML.Excel;
using Sentra.Api.Endpoints;

namespace Sentra.Api.Tests.Importing;

public sealed class ImportFileParserTests
{
    [Fact]
    public async Task Csv_ParsesBrazilianHeadersAndQuotedValues()
    {
        const string csv =
            "Nome;Telefone;Unidade;Vínculo\n" +
            "\"Maria da Silva\";+5511999999999;101;proprietária\n";

        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var rows = await ImportEndpoints.ReadCsvAsync(stream, CancellationToken.None);

        var row = Assert.Single(rows);
        Assert.Equal("Maria da Silva", row["nome"]);
        Assert.Equal("+5511999999999", row["telefone"]);
        Assert.Equal("101", row["unidade"]);
        Assert.Equal("proprietária", row["vinculo"]);
    }

    [Fact]
    public void Xlsx_ParsesFirstWorksheetWithoutOfficeInstalled()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Moradores");
        sheet.Cell(1, 1).Value = "Nome";
        sheet.Cell(1, 2).Value = "Telefone";
        sheet.Cell(1, 3).Value = "Unidade";
        sheet.Cell(2, 1).Value = "João Souza";
        sheet.Cell(2, 2).Value = "+5511988888888";
        sheet.Cell(2, 3).Value = "202";

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        var rows = ImportEndpoints.ReadXlsx(stream);

        var row = Assert.Single(rows);
        Assert.Equal("João Souza", row["nome"]);
        Assert.Equal("+5511988888888", row["telefone"]);
        Assert.Equal("202", row["unidade"]);
    }

    [Theory]
    [InlineData("Vínculo", "vinculo")]
    [InlineData("Display_Name", "displayname")]
    [InlineData("Identificador da Unidade", "identificadordaunidade")]
    public void HeaderNormalization_IsDeterministic(string input, string expected)
        => Assert.Equal(expected, ImportEndpoints.NormalizeHeader(input));
}
