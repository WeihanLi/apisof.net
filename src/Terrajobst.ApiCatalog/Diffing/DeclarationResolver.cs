using NuGet.Frameworks;

namespace Terrajobst.ApiCatalog;

public abstract class DeclarationResolver
{
    public abstract ApiDeclarationModel? Resolve(ApiModel api);

    public static DeclarationResolver Null { get; } = new NullDeclarationResolver();

    public static DeclarationResolver ForFramework(ApiAvailabilityContext context, NuGetFramework framework)
    {
        return new FrameworkDeclarationResolver(context, framework);
    }

    public static DeclarationResolver ForPackage(ApiAvailabilityContext context, PackageModel package, NuGetFramework framework)
    {
        return new PackageDeclarationResolver(context, package, framework);
    }

    private sealed class NullDeclarationResolver : DeclarationResolver
    {
        public override ApiDeclarationModel? Resolve(ApiModel api)
        {
            return null;
        }
    }

    private sealed class FrameworkDeclarationResolver : DeclarationResolver
    {
        private readonly ApiAvailabilityContext _context;
        private readonly NuGetFramework _framework;

        public FrameworkDeclarationResolver(ApiAvailabilityContext context, NuGetFramework framework)
        {
            _context = context;
            _framework = framework;
        }

        public override ApiDeclarationModel? Resolve(ApiModel api)
        {
            var availability = _context.GetAvailability(api, _framework);
            return availability?.Declaration;
        }
    }

    private sealed class PackageDeclarationResolver : DeclarationResolver
    {
        private readonly ApiAvailabilityContext _context;
        private readonly PackageModel _package;
        private readonly NuGetFramework _framework;

        public PackageDeclarationResolver(ApiAvailabilityContext context, PackageModel package, NuGetFramework framework)
        {
            _context = context;
            _package = package;
            _framework = framework;
        }

        public override ApiDeclarationModel? Resolve(ApiModel api)
        {
            var availability = _context.GetAvailability(api, _framework, _package);
            if (availability is not null && !availability.IsInBox)
                return availability?.Declaration;

            return null;
        }
    }
}
