using System.Globalization;
using System.IO.Compression;
using System.Security;
using System.Text;

namespace Toneel.Core.Export;

/// <summary>
/// Writes a single-sheet Excel workbook (.xlsx).
/// </summary>
/// <remarks>
/// An .xlsx file is a zip archive with a handful of XML files. For one plain
/// sheet that is little work, and it keeps a large spreadsheet library out of
/// the download the browser has to fetch.
/// </remarks>
public sealed class XlsxWriter(string sheetName)
{
    private readonly SortedDictionary<int, SortedDictionary<int, object>> _rows = [];
    private readonly List<string> _merges = [];
    private readonly List<double> _columnWidths = [];

    /// <summary>Sets a cell. Rows and columns start at 1; numbers stay numbers in Excel.</summary>
    public XlsxWriter Set(int row, int column, object? value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(row, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(column, 1);

        if (value is null or "") return this;

        if (!_rows.TryGetValue(row, out var cells))
        {
            _rows[row] = cells = [];
        }

        cells[column] = value;
        return this;
    }

    /// <summary>Fills one row, starting at <paramref name="firstColumn"/>.</summary>
    public XlsxWriter SetRow(int row, int firstColumn, params object?[] values)
    {
        for (var i = 0; i < values.Length; i++)
        {
            Set(row, firstColumn + i, values[i]);
        }

        return this;
    }

    public XlsxWriter Merge(int row, int firstColumn, int lastColumn)
    {
        _merges.Add($"{CellName(row, firstColumn)}:{CellName(row, lastColumn)}");
        return this;
    }

    /// <summary>Widths in characters, for column A, B, C, …</summary>
    public XlsxWriter ColumnWidths(params double[] widths)
    {
        _columnWidths.Clear();
        _columnWidths.AddRange(widths);
        return this;
    }

    public byte[] ToArray()
    {
        using var buffer = new MemoryStream();

        using (var zip = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            Add(zip, "[Content_Types].xml", ContentTypes);
            Add(zip, "_rels/.rels", RootRelations);
            Add(zip, "xl/workbook.xml", Workbook());
            Add(zip, "xl/_rels/workbook.xml.rels", WorkbookRelations);
            Add(zip, "xl/worksheets/sheet1.xml", Sheet());
        }

        return buffer.ToArray();
    }

    /// <summary>"A1", "F12", "AA3"</summary>
    public static string CellName(int row, int column)
    {
        var letters = "";

        for (var remaining = column; remaining > 0; remaining = (remaining - 1) / 26)
        {
            letters = (char)('A' + ((remaining - 1) % 26)) + letters;
        }

        return letters + row.ToString(CultureInfo.InvariantCulture);
    }

    private string Sheet()
    {
        var xml = new StringBuilder(
            """<?xml version="1.0" encoding="UTF-8" standalone="yes"?>""" +
            """<worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">""");

        if (_columnWidths.Count > 0)
        {
            xml.Append("<cols>");
            for (var i = 0; i < _columnWidths.Count; i++)
            {
                var width = _columnWidths[i].ToString(CultureInfo.InvariantCulture);
                xml.Append($"""<col min="{i + 1}" max="{i + 1}" width="{width}" customWidth="1"/>""");
            }

            xml.Append("</cols>");
        }

        xml.Append("<sheetData>");
        foreach (var (rowNumber, cells) in _rows)
        {
            xml.Append($"""<row r="{rowNumber}">""");
            foreach (var (column, value) in cells)
            {
                xml.Append(Cell(CellName(rowNumber, column), value));
            }

            xml.Append("</row>");
        }

        xml.Append("</sheetData>");

        if (_merges.Count > 0)
        {
            xml.Append($"""<mergeCells count="{_merges.Count}">""");
            foreach (var range in _merges)
            {
                xml.Append($"""<mergeCell ref="{range}"/>""");
            }

            xml.Append("</mergeCells>");
        }

        return xml.Append("</worksheet>").ToString();
    }

    private static string Cell(string name, object value) => value switch
    {
        sbyte or byte or short or ushort or int or uint or long or ulong
            or float or double or decimal =>
            $"""<c r="{name}"><v>{Convert.ToString(value, CultureInfo.InvariantCulture)}</v></c>""",

        // Inline strings avoid a separate shared-strings file.
        _ => $"""<c r="{name}" t="inlineStr"><is><t xml:space="preserve">{Escape(value.ToString())}</t></is></c>""",
    };

    private string Workbook() =>
        """<?xml version="1.0" encoding="UTF-8" standalone="yes"?>""" +
        """<workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" """ +
        """xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">""" +
        $"""<sheets><sheet name="{Escape(SafeSheetName(sheetName))}" sheetId="1" r:id="rId1"/></sheets>""" +
        "</workbook>";

    /// <summary>Excel allows at most 31 characters and none of <c>[ ] : * ? / \</c>.</summary>
    public static string SafeSheetName(string name)
    {
        var cleaned = new string([.. name.Select(c => @"[]:*?/\".Contains(c) ? '_' : c)]).Trim();

        if (cleaned.Length == 0) return "Blad1";
        return cleaned.Length > 31 ? cleaned[..31] : cleaned;
    }

    private static string Escape(string? text)
    {
        // XML 1.0 has no way to write most control characters; drop them.
        var printable = new string(
            [.. (text ?? "").Where(c => c is '\t' or '\n' or '\r' || !char.IsControl(c))]);

        return SecurityElement.Escape(printable);
    }

    private static void Add(ZipArchive zip, string path, string content)
    {
        using var writer = new StreamWriter(
            zip.CreateEntry(path, CompressionLevel.Optimal).Open(),
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        writer.Write(content);
    }

    private const string ContentTypes =
        """<?xml version="1.0" encoding="UTF-8" standalone="yes"?>""" +
        """<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">""" +
        """<Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>""" +
        """<Default Extension="xml" ContentType="application/xml"/>""" +
        """<Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>""" +
        """<Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>""" +
        "</Types>";

    private const string RootRelations =
        """<?xml version="1.0" encoding="UTF-8" standalone="yes"?>""" +
        """<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">""" +
        """<Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/>""" +
        "</Relationships>";

    private const string WorkbookRelations =
        """<?xml version="1.0" encoding="UTF-8" standalone="yes"?>""" +
        """<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">""" +
        """<Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/>""" +
        "</Relationships>";
}
