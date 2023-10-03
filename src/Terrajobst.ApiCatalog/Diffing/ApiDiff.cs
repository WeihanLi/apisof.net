namespace Terrajobst.ApiCatalog;

// TODO: Integrate into the web site
//       We should have diff page where one can select frameworks, with a quick link for latest - 1 vs latest
//
// TODO: Add tests

public sealed class ApiDiff
{
    public static IEnumerable<ApiDiff> Create(ApiCatalogModel catalog, DiffOptions diffOptions)
    {
        return catalog.RootApis
                      .Order()
                      .Select(a => Create(a, diffOptions))
                      .Where(d => d is not null);
    }

    public static ApiDiff Create(ApiModel api, DiffOptions diffOptions)
    {
        var oldDeclaration = diffOptions.Old.Resolve(api);
        var newDeclaration = diffOptions.New.Resolve(api);

        if (oldDeclaration is null && newDeclaration is null)
            return null;

        var children = new List<ApiDiff>(api.Children.Count);

        foreach (var child in api.Children.OrderBy(c => c))
        {
            var childDiff = Create(child, diffOptions);
            if (childDiff is not null)
                children.Add(childDiff);
        }

        var kind = DiffKind.Unchanged;

        if (oldDeclaration is null)
        {
            if (!diffOptions.IncludeAdded)
                return null;

            kind = DiffKind.Added;
        }
        else if (newDeclaration is null)
        {
            if (!diffOptions.IncludeRemoved)
                return null;

            kind = DiffKind.Removed;
        }
        else if (oldDeclaration.Value.GetMyMarkupId() != newDeclaration.Value.GetMyMarkupId())
        {
            if (!diffOptions.IncludeChanged)
                return null;

            kind = DiffKind.Changed;
        }

        if (kind == DiffKind.Unchanged && !children.Any(c => c.Kind != DiffKind.Unchanged))
            return null;

        return new ApiDiff(kind, oldDeclaration, newDeclaration, children.ToArray());
    }

    private readonly DiffKind _kind;
    private readonly ApiModel _api;
    private readonly int _oldDeclarationId;
    private readonly int _newDeclarationId;
    private readonly IReadOnlyList<ApiDiff> _children;

    private ApiDiff(DiffKind kind,
                    ApiDeclarationModel? old,
                    ApiDeclarationModel? @new,
                    IReadOnlyList<ApiDiff> children)
    {
        _kind = kind;
        _api = (old?.Api ?? @new?.Api).Value;
        _oldDeclarationId = old?.Id ?? -1;
        _newDeclarationId = @new?.Id ?? -1;
        _children = children;
    }

    public ApiDeclarationModel Representative => New ?? Old!.Value;
    public DiffKind Kind => _kind;
    public ApiModel Api => _api;
    public ApiDeclarationModel? Old => _oldDeclarationId == -1 ? null : new ApiDeclarationModel(_api, _oldDeclarationId);
    public ApiDeclarationModel? New => _newDeclarationId == -1 ? null : new ApiDeclarationModel(_api, _newDeclarationId);
    public IReadOnlyList<ApiDiff> Children => _children;

    public IEnumerable<MarkupPartDiff> GetMarkupDiff()
    {
        return MarkupPartDiff.Create(Old?.GetMyMarkup(), New?.GetMyMarkup());
    }
}
