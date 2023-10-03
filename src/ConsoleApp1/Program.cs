using NuGet.Frameworks;

using Terrajobst.ApiCatalog;

var catalog = await ApiCatalogModel.LoadFromWebAsync();

// Experimental
// DumpExperimental(catalog, net80);

// Diff
DumpDiff(catalog);

static void DumpDiff(ApiCatalogModel catalog)
{
    var net80 = NuGetFramework.Parse("net8.0");

    var relevantPackages = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "Microsoft.AspNetCore.AsyncState",
        "Microsoft.AspNetCore.Diagnostics.Middleware",
        "Microsoft.AspNetCore.HeaderParsing",
        "Microsoft.AspNetCore.Testing",
        "Microsoft.Extensions.AmbientMetadata.Application",
        "Microsoft.Extensions.AsyncState",
        "Microsoft.Extensions.AuditReports",
        "Microsoft.Extensions.Compliance.Abstractions",
        "Microsoft.Extensions.Compliance.Redaction",
        "Microsoft.Extensions.Compliance.Testing",
        "Microsoft.Extensions.DependencyInjection.AutoActivation",
        "Microsoft.Extensions.Diagnostics.ExceptionSummarization",
        "Microsoft.Extensions.Diagnostics.Extra",
        "Microsoft.Extensions.Diagnostics.ExtraAbstractions",
        "Microsoft.Extensions.Diagnostics.HealthChecks.Common",
        "Microsoft.Extensions.Diagnostics.HealthChecks.ResourceUtilization",
        "Microsoft.Extensions.Diagnostics.Probes",
        "Microsoft.Extensions.Diagnostics.ResourceMonitoring",
        "Microsoft.Extensions.Diagnostics.Testing",
        "Microsoft.Extensions.EnumStrings",
        "Microsoft.Extensions.ExtraAnalyzers",
        "Microsoft.Extensions.Hosting.Testing",
        "Microsoft.Extensions.Http.AutoClient",
        "Microsoft.Extensions.Http.Diagnostics",
        "Microsoft.Extensions.Http.Resilience",
        "Microsoft.Extensions.ObjectPool.DependencyInjection",
        "Microsoft.Extensions.Options.Contextual",
        "Microsoft.Extensions.Resilience",
        "Microsoft.Extensions.StaticAnalysis",
        "Microsoft.Extensions.TimeProvider.Testing"
    };

    var context = ApiAvailabilityContext.Create(catalog);

    var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    var diffName = $"r9-packages-for-shipping";
    var directoryPath = Path.Join(userProfile, "Downloads", diffName);
    if (Directory.Exists(directoryPath))
        Directory.Delete(directoryPath, recursive: true);
    Directory.CreateDirectory(directoryPath);

    foreach (var package in catalog.Packages)
    {
        if (!relevantPackages.Contains(package.Name))
            continue;

        var options = new DiffOptions
        {
            IncludeAdded = true,
            IncludeRemoved = true,
            IncludeChanged = false,
            Old = DeclarationResolver.Null,
            New = DeclarationResolver.ForPackage(context, package, net80)
        };

        var filePath = Path.Join(directoryPath, package.Name + ".html");
        using var fileWriter = File.CreateText(filePath);
        using var writer = new HtmlDiffWriter(fileWriter, includeDocument: true);

        foreach (var diff in ApiDiff.Create(catalog, options))
            WalkDiff(writer, diff);
    }
}

static void WalkDiff(DiffWriter writer,
                     ApiDiff? diff)
{
    if (diff is null)
        return;

    writer.StartDiffLine(diff.Kind);

    foreach (var diffPart in diff.GetMarkupDiff())
    {
        writer.StartDiffSpan(diffPart.Kind);
        writer.Write(diffPart.Part);
        writer.EndDiffSpan();
    }

    writer.EndDiffLine();

    var canHaveChildren = diff.Api.Kind == ApiKind.Namespace ||
                              diff.Api.Kind.IsType() && diff.Api.Kind != ApiKind.Delegate;

    if (canHaveChildren)
    {
        var braceDiffKind = diff.Kind == DiffKind.Changed ? DiffKind.Unchanged : diff.Kind;

        writer.StartDiffLine(braceDiffKind);
        writer.Write(new MarkupPart(MarkupPartKind.Punctuation, "{"));
        writer.EndDiffLine();
        writer.Indent++;

        foreach (var child in diff.Children)
            WalkDiff(writer, child);

        writer.Indent--;
        writer.StartDiffLine(braceDiffKind);
        writer.Write(new MarkupPart(MarkupPartKind.Punctuation, "}"));
        writer.EndDiffLine();
    }
}

static void DumpExperimental(ApiCatalogModel catalog, NuGetFramework framework)
{
    var context = ApiAvailabilityContext.Create(catalog);

    foreach (var api in catalog.GetAllApis())
    {
        var availability = context.GetAvailability(api, framework);
        if (availability is null || !availability.IsInBox && availability.PackageFramework != framework)
            continue;

        var experimental = availability.Declaration.GetEffectiveExperimental();
        if (experimental is null)
            continue;

        Console.WriteLine(api.GetFullName());
    }
}

