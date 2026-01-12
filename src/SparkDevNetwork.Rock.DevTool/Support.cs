using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

using Semver;

namespace SparkDevNetwork.Rock.DevTool;

/// <summary>
/// Various support elements.
/// </summary>
class Support
{
    /// <summary>
    /// The current version of the tool.
    /// </summary>
    public static readonly SemVersion CurrentToolVersion;

    /// <summary>
    /// Initializes static members of the <see cref="Support"/> class.
    /// </summary>
    static Support()
    {
        var versionInfo = typeof( Support )
            .Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>();

        var version = SemVersion.Parse( versionInfo!.InformationalVersion, SemVersionStyles.Any );

        CurrentToolVersion = new SemVersion( version.Major, version.Minor, version.Patch );
    }

    /// <summary>
    /// The default serializer options that outputs with indentation.
    /// </summary>
    public static JsonSerializerOptions SerializerOptions { get; } = new JsonSerializerOptions
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
}
