namespace SparkDevNetwork.Rock.Plugin.Tool.Enums;

/// <summary>
/// The values available for use in the <c>--verbosity</c> argument.
/// </summary>
enum VerbosityOptions
{
    /// <summary>
    /// Tool output will be quiet and only display errors.
    /// </summary>
    q,

    /// <summary>
    /// Tool output will be minimal and only display warnings and errors.
    /// </summary>
    m,

    /// <summary>
    /// Tool output will be normal and display information, warnings and errors.
    /// </summary>
    n,

    /// <summary>
    /// Tool output will contain detailed information which includes normal
    /// information and additional debug information.
    /// </summary>
    d,
}
