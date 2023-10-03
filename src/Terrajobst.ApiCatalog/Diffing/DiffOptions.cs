using NuGet.Frameworks;

namespace Terrajobst.ApiCatalog;

public sealed class DiffOptions
{
    public bool IncludeAdded { get; init; } = true;
    public bool IncludeRemoved { get; init; } = true;
    public bool IncludeChanged { get; init; } = true;
    //public bool IncludeUnchangedNamespaceMembers { get; init; } = true;
    //public bool IncludeUnchangedTypeMembers { get; init; } = true;
    public required DeclarationResolver Old { get; init; }
    public required DeclarationResolver New { get; init; }
}
