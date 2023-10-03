namespace Terrajobst.ApiCatalog;

public record struct MarkupPartDiff(DiffKind Kind, MarkupPart Part)
{
    public static IEnumerable<MarkupPartDiff> Create(Markup oldMarkup, Markup newMarkup)
    {
        if (oldMarkup is null && newMarkup is null)
            return Enumerable.Empty<MarkupPartDiff>();

        if (oldMarkup is null)
            return newMarkup.Parts.Select(p => new MarkupPartDiff(DiffKind.Added, p));

        if (newMarkup is null)
            return newMarkup.Parts.Select(p => new MarkupPartDiff(DiffKind.Removed, p));

        return DiffUtil.Diff(oldMarkup, newMarkup);
    }
}
