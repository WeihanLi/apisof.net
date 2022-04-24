﻿using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Terrajobst.ApiCatalog.Tests;

public class PlatformContextTests
{
    [Fact]
    public async Task PlatformContext_AllowList()
    {
        var source = @"
            using System.Runtime.Versioning;
            namespace System
            {
                [SupportedOSPlatform(""ios"")]
                [SupportedOSPlatform(""tvos"")]
                public class TheClass { }
            }
        ";

        var expectedPlatforms = @"
            For the selected framework the API is only supported on these platforms:
            - iOS
            - Mac Catalyst
            - tvOS
        ";

        var catalog = await new FluentCatalogBuilder()
            .AddFramework("net45", fx =>
                fx.AddAssembly("System.Runtime", GetOperatingSystemWithImpliedPlatform())
                    .AddAssembly("System.TheAssembly", source))
            .BuildAsync();

        var context = PlatformContext.Create(catalog, "net45");
        var api = Assert.Single(catalog.GetAllApis().Where(a => a.GetFullName() == "System.TheClass"));
        var annotation = context.GetPlatformAnnotation(api);

        Assert.Equal(expectedPlatforms.Unindent(), annotation.ToString().Trim());
    }

    [Fact]
    public async Task PlatformContext_AllowList_RemoveImpliedPlatform()
    {
        var source = @"
            using System.Runtime.Versioning;
            namespace System
            {
                [SupportedOSPlatform(""ios"")]
                [SupportedOSPlatform(""tvos"")]
                [UnsupportedOSPlatform(""maccatalyst"")]
                public class TheClass { }
            }
        ";

        var expectedPlatforms = @"
            For the selected framework the API is only supported on these platforms:
            - iOS
            - tvOS
        ";

        var catalog = await new FluentCatalogBuilder()
            .AddFramework("net45", fx =>
                fx.AddAssembly("System.Runtime", GetOperatingSystemWithImpliedPlatform())
                  .AddAssembly("System.TheAssembly", source))
            .BuildAsync();

        var context = PlatformContext.Create(catalog, "net45");
        var api = Assert.Single(catalog.GetAllApis().Where(a => a.GetFullName() == "System.TheClass"));
        var annotation = context.GetPlatformAnnotation(api);

        Assert.Equal(expectedPlatforms.Unindent(), annotation.ToString().Trim());
    }

    [Fact]
    public async Task PlatformContext_DenyList()
    {
        var source = @"
            using System.Runtime.Versioning;
            namespace System
            {
                [UnsupportedOSPlatform(""ios"")]
                [UnsupportedOSPlatform(""tvos"")]
                public class TheClass { }
            }
        ";

        var expectedPlatforms = @"
            For the selected framework the API is supported on any platform except for:
            - iOS
            - Mac Catalyst
            - tvOS
        ";

        var catalog = await new FluentCatalogBuilder()
            .AddFramework("net45", fx =>
                fx.AddAssembly("System.Runtime", GetOperatingSystemWithImpliedPlatform())
                    .AddAssembly("System.TheAssembly", source))
            .BuildAsync();

        var context = PlatformContext.Create(catalog, "net45");
        var api = Assert.Single(catalog.GetAllApis().Where(a => a.GetFullName() == "System.TheClass"));
        var annotation = context.GetPlatformAnnotation(api);

        Assert.Equal(expectedPlatforms.Unindent(), annotation.ToString().Trim());
    }

    [Fact]
    public async Task PlatformContext_DenyList_AddImpliedPlatform()
    {
        var source = @"
            using System.Runtime.Versioning;
            namespace System
            {
                [SupportedOSPlatform(""maccatalyst"")]
                [UnsupportedOSPlatform(""ios"")]
                [UnsupportedOSPlatform(""tvos"")]
                public class TheClass { }
            }
        ";

        var expectedPlatforms = @"
            For the selected framework the API is supported on any platform except for:
            - iOS
            - tvOS
        ";

        var catalog = await new FluentCatalogBuilder()
            .AddFramework("net45", fx =>
                fx.AddAssembly("System.Runtime", GetOperatingSystemWithImpliedPlatform())
                  .AddAssembly("System.TheAssembly", source))
            .BuildAsync();

        var context = PlatformContext.Create(catalog, "net45");
        var api = Assert.Single(catalog.GetAllApis().Where(a => a.GetFullName() == "System.TheClass"));
        var annotation = context.GetPlatformAnnotation(api);

        Assert.Equal(expectedPlatforms.Unindent(), annotation.ToString().Trim());
    }

    [Fact]
    public async Task PlatformContext_Assembly()
    {
        var source = @"
            using System.Runtime.Versioning;
            [assembly: UnsupportedOSPlatform(""ios"")]
            namespace System
            {
                public class TheClass { }
            }
        ";

        var expectedPlatforms = @"
            For the selected framework the API is supported on any platform except for:
            - iOS
        ";

        var catalog = await new FluentCatalogBuilder()
            .AddFramework("net45", fx =>
                fx.AddAssembly("System.TheAssembly", source))
            .BuildAsync();

        var context = PlatformContext.Create(catalog, "net45");
        var api = Assert.Single(catalog.GetAllApis().Where(a => a.GetFullName() == "System.TheClass"));
        var annotation = context.GetPlatformAnnotation(api);

        Assert.Equal(expectedPlatforms.Unindent(), annotation.ToString().Trim());
    }

    [Fact]
    public async Task PlatformContext_Type()
    {
        var source = @"
            using System.Runtime.Versioning;
            namespace System
            {
                [UnsupportedOSPlatform(""ios"")]
                public class TheClass
                {
                    public void TheMethod() {}
                }
            }
        ";

        var expectedPlatforms = @"
            For the selected framework the API is supported on any platform except for:
            - iOS
        ";

        var catalog = await new FluentCatalogBuilder()
            .AddFramework("net45", fx =>
                fx.AddAssembly("System.TheAssembly", source))
            .BuildAsync();

        var context = PlatformContext.Create(catalog, "net45");
        var api = Assert.Single(catalog.GetAllApis().Where(a => a.GetFullName() == "System.TheClass.TheMethod()"));
        var annotation = context.GetPlatformAnnotation(api);

        Assert.Equal(expectedPlatforms.Unindent(), annotation.ToString().Trim());
    }

    [Fact]
    public async Task PlatformContext_Property()
    {
        var source = @"
            using System.Runtime.Versioning;
            namespace System
            {
                public class TheClass
                {
                    [SupportedOSPlatform(""ios"")]
                    public int TheProperty => 0;
                }
            }
        ";

        var expectedPlatforms = @"
            For the selected framework the API is only supported on these platforms:
            - iOS
        ";

        var catalog = await new FluentCatalogBuilder()
            .AddFramework("net45", fx =>
                fx.AddAssembly("System.TheAssembly", source))
            .BuildAsync();

        var context = PlatformContext.Create(catalog, "net45");
        var api = Assert.Single(catalog.GetAllApis().Where(a => a.GetFullName() == "System.TheClass.TheProperty.TheProperty.get"));
        var annotation = context.GetPlatformAnnotation(api);

        Assert.Equal(expectedPlatforms.Unindent(), annotation.ToString().Trim());
    }

    [Fact]
    public async Task PlatformContext_Property_Accessor()
    {
        var source = @"
            using System.Runtime.Versioning;
            namespace System
            {
                public class TheClass
                {
                    public int TheProperty
                    {
                        [UnsupportedOSPlatform(""ios"")]
                        get => 0;
                    }
                }
            }
        ";

        var expectedPlatforms = @"
            For the selected framework the API is supported on any platform except for:
            - iOS
        ";

        var catalog = await new FluentCatalogBuilder()
            .AddFramework("net45", fx =>
                fx.AddAssembly("System.TheAssembly", source))
            .BuildAsync();

        var context = PlatformContext.Create(catalog, "net45");
        var api = Assert.Single(catalog.GetAllApis().Where(a => a.GetFullName() == "System.TheClass.TheProperty.TheProperty.get"));
        var annotation = context.GetPlatformAnnotation(api);

        Assert.Equal(expectedPlatforms.Unindent(), annotation.ToString().Trim());
    }

    [Fact]
    public async Task PlatformContext_Event()
    {
        var source = @"
            using System;
            using System.Runtime.Versioning;
            namespace System
            {
                public class TheClass
                {
                    [SupportedOSPlatform(""ios"")]
                    public event EventHandler TheEvent;
                }
            }
        ";

        var expectedPlatforms = @"
            For the selected framework the API is only supported on these platforms:
            - iOS
        ";

        var catalog = await new FluentCatalogBuilder()
            .AddFramework("net45", fx =>
                fx.AddAssembly("System.TheAssembly", source))
            .BuildAsync();

        var context = PlatformContext.Create(catalog, "net45");
        var api = Assert.Single(catalog.GetAllApis().Where(a => a.GetFullName() == "System.TheClass.TheEvent.TheEvent.add"));
        var annotation = context.GetPlatformAnnotation(api);

        Assert.Equal(expectedPlatforms.Unindent(), annotation.ToString().Trim());
    }

    [Fact]
    public async Task PlatformContext_Event_Accessor()
    {
        var source = @"
            using System;
            using System.Runtime.Versioning;
            namespace System
            {
                public class TheClass
                {
                    public event EventHandler TheEvent
                    {
                        [UnsupportedOSPlatform(""ios"")]
                        add { }
                        remove { }
                    }
                }
            }
        ";

        var expectedPlatforms = @"
            For the selected framework the API is supported on any platform except for:
            - iOS
        ";

        var catalog = await new FluentCatalogBuilder()
            .AddFramework("net45", fx =>
                fx.AddAssembly("System.TheAssembly", source))
            .BuildAsync();

        var context = PlatformContext.Create(catalog, "net45");
        var api = Assert.Single(catalog.GetAllApis().Where(a => a.GetFullName() == "System.TheClass.TheEvent.TheEvent.add"));
        var annotation = context.GetPlatformAnnotation(api);

        Assert.Equal(expectedPlatforms.Unindent(), annotation.ToString().Trim());
    }

    private static string GetOperatingSystemWithImpliedPlatform()
    {
        return @"
            using System.Runtime.Versioning;

            namespace System
            {
                public sealed class OperatingSystem
                {
                    [SupportedOSPlatformGuard(""maccatalyst"")]
                    public static bool IsIOS() { return false; }
                }
            }
        ";
    }
}