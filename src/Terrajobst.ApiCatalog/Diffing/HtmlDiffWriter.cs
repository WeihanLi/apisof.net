using System.Text.Encodings.Web;

namespace Terrajobst.ApiCatalog;

public sealed class HtmlDiffWriter : DiffWriter
{
    private readonly TextWriter _writer;
    private readonly bool _includeDocument;

    private DiffKind _lineDiff;
    private DiffKind _spanDiff;

    public HtmlDiffWriter(TextWriter writer, bool includeDocument)
    {
        _writer = writer;
        _includeDocument = includeDocument;

        if (_includeDocument)
        {
            var header = """
                <html>
                <head>
                <style>
                .a  {background-color: #acf1ac; }
                .r  {background-color: #f5afaf; }
                .c  {background-color: #fdfdaa; }
                .k { color: blue; }
                .p { color: black; }
                .s { color: #a31515; }
                .n { color: #3876c2; }
                a {
                    color: black;
                    text-decoration: none;
                }
                a:hover {
                    color: blue;
                    text-decoration: underline;
                }
                code div {
                    white-space: pre;
                }
                </style>
                </head>
                <body>
                <code>
                """;

            _writer.WriteLine(header);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_includeDocument)
                _writer.WriteLine("</code></body></html>");
        }

        base.Dispose(disposing);
    }

    public override int Indent { get; set; }

    public override void StartDiffLine(DiffKind kind)
    {
        var cssClass = kind switch
        {
            DiffKind.Added => "a",
            DiffKind.Removed => "r",
            DiffKind.Changed => "c",
            DiffKind.Unchanged or
            _ => null
        };

        _writer.Write($"<div");
        if (cssClass is not null)
            _writer.Write($" class=\"{cssClass}\"");
        _writer.Write(">");
        _writer.Write(new string(' ', Indent * 4));
        _lineDiff = kind;
    }

    public override void EndDiffLine()
    {
        _writer.Write("</div>");
        _writer.WriteLine();
        _lineDiff = DiffKind.Unchanged;
    }

    public override void StartDiffSpan(DiffKind kind)
    {
        if (_lineDiff != DiffKind.Changed)
            return;

        var cssClass = kind switch
        {
            DiffKind.Added => "a",
            DiffKind.Removed => "r",
            DiffKind.Changed or
            DiffKind.Unchanged or
            _ => null
        };

        if (cssClass is null)
            return;

        _spanDiff = kind;
        _writer.Write($"<span class=\"{cssClass}\">");
    }

    public override void EndDiffSpan()
    {
        if (_lineDiff != DiffKind.Changed)
            return;

        if (_spanDiff != DiffKind.Unchanged)
        {
            _writer.Write("</span>");
            _spanDiff = DiffKind.Unchanged;
        }
    }

    public override void Write(MarkupPart part)
    {
        if (part.Kind == MarkupPartKind.Whitespace && part.Text is "\n" or "\r" or "\r\n")
        {
            var lineDiff = _lineDiff;
            EndDiffLine();
            StartDiffLine(lineDiff);
            return;
        }

        var cssClass = part.Kind switch
        {
            MarkupPartKind.LiteralString => "s",
            MarkupPartKind.LiteralNumber => "n",
            MarkupPartKind.Keyword => "k",
            MarkupPartKind.Whitespace => null,
            MarkupPartKind.Punctuation => "p",
            MarkupPartKind.Reference => null,
            _ => null
        };

        if (cssClass is not null)
            _writer.Write($"<span class=\"{cssClass}\">");

        if (part.Reference is not null)
            _writer.Write($"<a href=\"https://apisof.net/catalog/{part.Reference}\">");

        _writer.Write(HtmlEncoder.Default.Encode(part.Text));

        if (part.Reference is not null)
            _writer.Write($"</a>");

        if (cssClass is not null)
            _writer.Write("</span>");
    }
}
