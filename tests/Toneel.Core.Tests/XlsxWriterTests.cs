using System.IO.Compression;
using System.Xml.Linq;
using Toneel.Core.Export;

namespace Toneel.Core.Tests;

public class XlsxWriterTests
{
    private static readonly XNamespace Main =
        "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

    /// <summary>Opens the generated file the way Excel does: as a zip with XML inside.</summary>
    private static XDocument Part(byte[] xlsx, string path)
    {
        using var zip = new ZipArchive(new MemoryStream(xlsx), ZipArchiveMode.Read);
        var entry = zip.GetEntry(path);
        Assert.NotNull(entry);

        using var stream = entry!.Open();
        return XDocument.Load(stream);
    }

    private static XElement Cell(XDocument sheet, string name) =>
        sheet.Descendants(Main + "c").Single(cell => cell.Attribute("r")!.Value == name);

    [Theory]
    [InlineData(1, 1, "A1")]
    [InlineData(12, 6, "F12")]
    [InlineData(3, 26, "Z3")]
    [InlineData(3, 27, "AA3")]
    [InlineData(1, 52, "AZ1")]
    [InlineData(1, 703, "AAA1")]
    public void Names_cells_like_Excel(int row, int column, string expected)
    {
        Assert.Equal(expected, XlsxWriter.CellName(row, column));
    }

    [Fact]
    public void Contains_every_part_a_workbook_needs()
    {
        var xlsx = new XlsxWriter("Blad").Set(1, 1, "x").ToArray();

        using var zip = new ZipArchive(new MemoryStream(xlsx), ZipArchiveMode.Read);
        var names = string.Join('|', zip.Entries.Select(entry => entry.FullName).Order(StringComparer.Ordinal));

        Assert.Equal(
            "[Content_Types].xml|_rels/.rels|xl/_rels/workbook.xml.rels|xl/workbook.xml|xl/worksheets/sheet1.xml",
            names);
    }

    [Fact]
    public void Writes_text_as_inline_strings_and_numbers_as_numbers()
    {
        var sheet = Part(
            new XlsxWriter("Blad").Set(1, 1, "Naam").Set(1, 2, 12).Set(2, 1, 8.5m).ToArray(),
            "xl/worksheets/sheet1.xml");

        Assert.Equal("inlineStr", Cell(sheet, "A1").Attribute("t")?.Value);
        Assert.Equal("Naam", Cell(sheet, "A1").Value);
        Assert.Null(Cell(sheet, "B1").Attribute("t"));
        Assert.Equal("12", Cell(sheet, "B1").Value);
        Assert.Equal("8.5", Cell(sheet, "A2").Value);
    }

    [Fact]
    public void Keeps_special_characters_intact()
    {
        const string text = "Jan & <Co> \"'t Viooltje\" €8";

        var sheet = Part(new XlsxWriter("Blad").Set(1, 1, text).ToArray(), "xl/worksheets/sheet1.xml");

        Assert.Equal(text, Cell(sheet, "A1").Value);
    }

    [Fact]
    public void Drops_control_characters_that_would_make_the_file_unreadable()
    {
        var sheet = Part(
            new XlsxWriter("Blad").Set(1, 1, "a\u0001b\u0000c").ToArray(),
            "xl/worksheets/sheet1.xml");

        Assert.Equal("abc", Cell(sheet, "A1").Value);
    }

    [Fact]
    public void Skips_empty_values_and_writes_rows_in_order()
    {
        var sheet = Part(
            new XlsxWriter("Blad").SetRow(5, 1, "e", null, "").Set(2, 1, "b").ToArray(),
            "xl/worksheets/sheet1.xml");

        var rows = string.Join(',', sheet.Descendants(Main + "row").Select(row => row.Attribute("r")!.Value));

        Assert.Equal("2,5", rows);
        Assert.Equal(2, sheet.Descendants(Main + "c").Count());
    }

    [Fact]
    public void Writes_merged_cells_and_column_widths()
    {
        var sheet = Part(
            new XlsxWriter("Blad").Set(1, 1, "Titel").Merge(1, 1, 6).ColumnWidths(10, 18.5).ToArray(),
            "xl/worksheets/sheet1.xml");

        Assert.Equal("A1:F1", sheet.Descendants(Main + "mergeCell").Single().Attribute("ref")?.Value);

        var widths = string.Join(',', sheet.Descendants(Main + "col").Select(col => col.Attribute("width")!.Value));
        Assert.Equal("10,18.5", widths);
    }

    [Theory]
    [InlineData("Toneelgroep_De_Ballon", "Toneelgroep_De_Ballon")]
    [InlineData("a/b:c*d?e[f]g\\h", "a_b_c_d_e_f_g_h")]
    [InlineData("", "Blad1")]
    [InlineData("Een naam die veel langer is dan eenendertig tekens", "Een naam die veel langer is dan")]
    public void Makes_the_sheet_name_valid_for_Excel(string name, string expected)
    {
        Assert.Equal(expected, XlsxWriter.SafeSheetName(name));

        var workbook = Part(new XlsxWriter(name).Set(1, 1, "x").ToArray(), "xl/workbook.xml");
        Assert.Equal(expected, workbook.Descendants(Main + "sheet").Single().Attribute("name")?.Value);
    }

    [Fact]
    public void Rejects_rows_and_columns_below_one()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new XlsxWriter("Blad").Set(0, 1, "x"));
        Assert.Throws<ArgumentOutOfRangeException>(() => new XlsxWriter("Blad").Set(1, 0, "x"));
    }
}
