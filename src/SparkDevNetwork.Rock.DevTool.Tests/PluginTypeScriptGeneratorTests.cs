using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using SparkDevNetwork.Rock.DevTool.Generators;

namespace SparkDevNetwork.Rock.DevTool.Tests;

[TestClass]
[System.Diagnostics.CodeAnalysis.SuppressMessage( "Performance", "CA1861:Avoid constant arrays as arguments", Justification = "Doesn't make sense for unit tests." )]
public class PluginTypeScriptGeneratorTests
{
    // Simulated directory structure:
    // enums.partial.ts
    // Enums/codes.partial.ts
    // viewModels.d.ts
    // ViewModels/subFolder.d.ts
    // ViewModels/SubFolder/deep.d.ts
    // ViewModels/SubFolder/Deep/superDeep.d.ts
    // ViewModels/SubFolder2/deep2.d.ts

    [TestMethod]
    [DataRow(
        new string[] { "ViewModels" },
        new string[] { "ViewModels", "SubFolder" },
        new string[] { ".", "ViewModels", "subFolder" },
        DisplayName = "ViewModels => ViewModels.SubFolder" )]

    [DataRow(
        new string[] { "ViewModels" },
        new string[] { "ViewModels", "SubFolder", "Deep" },
        new string[] { ".", "ViewModels", "SubFolder", "deep" },
        DisplayName = "ViewModels => ViewModels.SubFolder.Deep" )]

    [DataRow(
        new string[] { "ViewModels", "SubFolder" },
        new string[] { "ViewModels", "SubFolder", "Deep" },
        new string[] { ".", "deep" },
        DisplayName = "ViewModels.SubFolder => ViewModels.SubFolder.Deep" )]

    [DataRow(
        new string[] { "ViewModels", "SubFolder" },
        new string[] { "ViewModels" },
        new string[] { "..", "viewModels" },
        DisplayName = "ViewModels.SubFolder => ViewModels" )]

    [DataRow(
        new string[] { "ViewModels", "SubFolder", "Deep" },
        new string[] { "ViewModels", "SubFolder" },
        new string[] { "..", "subFolder" },
        DisplayName = "ViewModels.SubFolder.Deep => ViewModels.SubFolder" )]

    [DataRow(
        new string[] { "ViewModels", "SubFolder", "Deep" },
        new string[] { "ViewModels" },
        new string[] { "..", "..", "viewModels" },
        DisplayName = "ViewModels.SubFolder.Deep => ViewModels" )]

    [DataRow(
        new string[] { "ViewModels", "SubFolder", "Deep", "SuperDeep" },
        new string[] { "ViewModels", "SubFolder" },
        new string[] { "..", "..", "subFolder" },
        DisplayName = "ViewModels.SubFolder.Deep.SuperDeep => ViewModels.SubFolder" )]

    [DataRow(
        new string[] { "ViewModels", "SubFolder2", "Deep2" },
        new string[] { "ViewModels", "SubFolder", "Deep" },
        new string[] { "..", "SubFolder", "deep" },
        DisplayName = "ViewModels.SubFolder2.Deep2 => ViewModels.SubFolder.Deep" )]

    [DataRow(
        new string[] { "ViewModels" },
        new string[] { "Enums" },
        new string[] { ".", "enums" },
        DisplayName = "ViewModels => Enums" )]

    [DataRow(
        new string[] { "ViewModels" },
        new string[] { "Enums", "Codes" },
        new string[] { ".", "Enums", "codes" },
        DisplayName = "ViewModels => Enums.Codes" )]

    [DataRow(
        new string[] { "ViewModels", "SubFolder" },
        new string[] { "Enums" },
        new string[] { "..", "enums" },
        DisplayName = "ViewModels.SubFolder => Enums" )]

    [DataRow(
        new string[] { "ViewModels", "SubFolder" },
        new string[] { "Enums", "Codes" },
        new string[] { "..", "Enums", "codes" },
        DisplayName = "ViewModels.SubFolder => Enums.Codes" )]
    public void GetPathReferenceComponents( string[] source, string[] target, string[] expected )
    {
        var sourcePathComponents = source.ToList();
        var targetPathComponents = target.ToList();
        var expectedComponents = expected.ToList();

        var actualComponents = PluginTypeScriptGenerator.GetPathReferenceComponents( sourcePathComponents, targetPathComponents );

        if ( expectedComponents.Count != actualComponents.Count )
        {
            throw new AssertFailedException( $"Expected '{string.Join( "/", expectedComponents )}' but got '{string.Join( "/", actualComponents )}'" );
        }

        if ( !expectedComponents.SequenceEqual( actualComponents ) )
        {
            throw new AssertFailedException( $"Expected '{string.Join( "/", expectedComponents )}' but got '{string.Join( "/", actualComponents )}'" );
        }
    }

    [TestMethod]
    public void GetPathReferenceComponents_WithSamePath_ThrowsException()
    {
        List<string> sourcePathComponents = ["ViewModels"];
        List<string> targetPathComponents = ["ViewModels"];

        Assert.ThrowsException<ArgumentException>( () =>
        {
            PluginTypeScriptGenerator.GetPathReferenceComponents( sourcePathComponents, targetPathComponents );
        } );
    }
}
