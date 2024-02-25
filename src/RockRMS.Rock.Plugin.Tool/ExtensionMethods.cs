namespace RockRMS.Rock.Plugin.Tool;

static class ExtensionMethods
{
    /// <summary>
    /// Clones the options into a new options object.
    /// </summary>
    /// <param name="options">The options to be cloned.</param>
    /// <returns>A new instance of <see cref="CreateCommandOptions"/> that has the same values.</returns>
    public static CreateCommandOptions Clone( this CreateCommandOptions options )
    {
        return new CreateCommandOptions
        {
            Organization = options.Organization,
            OrganizationCode = options.OrganizationCode,
            PluginName = options.PluginName,
            RockVersion = options.RockVersion,
            RockWebPath = options.RockWebPath,
            Obsidian = options.Obsidian,
            Copy = options.Copy
        };
    }
}
