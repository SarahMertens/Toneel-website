using System.IO.Compression;
using System.Xml.Linq;
using Toneel.Core.Export;

namespace Toneel.Core.Tests;

public class ReservationExportTests
{
    private static readonly XNamespace Main =
        "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

    /// <summary>The sheet as a dictionary: "A1" → "Totaal bedrag: €48".</summary>
    private static Dictionary<string, string> Cells(ExportFile file)
    {
        using var zip = new ZipArchive(new MemoryStream(file.Content), ZipArchiveMode.Read);
        using var stream = zip.GetEntry("xl/worksheets/sheet1.xml")!.Open();

        return XDocument.Load(stream)
            .Descendants(Main + "c")
            .ToDictionary(cell => cell.Attribute("r")!.Value, cell => cell.Value);
    }

    [Fact]
    public void Names_the_file_after_the_company()
    {
        var file = ReservationExport.ToExcel([], Sample.Production);

        Assert.Equal("Toneelgroep_Test.xlsx", file.FileName);
        Assert.Equal(ReservationExport.XlsxContentType, file.ContentType);
    }

    [Fact]
    public void Puts_the_total_on_top_and_numbers_the_rows_oldest_first()
    {
        var cells = Cells(ReservationExport.ToExcel(
            [
                Sample.Reservation("Jef", "De Smet", Sample.Sunday, seats: 4, addedMinutesAgo: 5),
                Sample.Reservation("Lotte", "Vermeulen", Sample.Saturday, seats: 2, addedMinutesAgo: 60),
            ],
            Sample.Production));

        Assert.Equal("Totaal bedrag: €48", cells["A1"]);

        Assert.Equal("Nummer", cells["A3"]);
        Assert.Equal("Aantal", cells["F3"]);

        Assert.Equal("001", cells["A4"]);
        Assert.Equal("24 januari 2026", cells["B4"]);
        Assert.Equal("Zaal Noord", cells["C4"]);
        Assert.Equal("Vermeulen", cells["D4"]);
        Assert.Equal("Lotte", cells["E4"]);
        Assert.Equal("2", cells["F4"]);

        Assert.Equal("002", cells["A5"]);
        Assert.Equal("De Smet", cells["D5"]);
    }

    [Fact]
    public void Lists_seats_per_date_below_the_reservations()
    {
        var cells = Cells(ReservationExport.ToExcel(
            [
                Sample.Reservation(date: Sample.Sunday, seats: 4),
                Sample.Reservation("Nora", undated: true, seats: 3),
            ],
            Sample.Production));

        // Two reservations end on row 5; two blank rows follow.
        Assert.Equal("Aantal personen per datum:", cells["A8"]);
        Assert.Equal("25 januari 2026", cells["A9"]);
        Assert.Equal("4", cells["B9"]);
        Assert.Equal("Nog niet gekozen", cells["A10"]);
        Assert.Equal("3", cells["B10"]);
        Assert.Equal("Nog niet gekozen", cells["B5"]);
    }

    [Fact]
    public void An_empty_list_still_gives_a_valid_sheet_with_headers()
    {
        var cells = Cells(ReservationExport.ToExcel([], Sample.Production));

        Assert.Equal("Totaal bedrag: €0", cells["A1"]);
        Assert.Equal("Naam", cells["D3"]);
        Assert.False(cells.ContainsKey("A4"));
    }
}
