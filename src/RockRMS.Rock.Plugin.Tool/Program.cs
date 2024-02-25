namespace RockRMS.Rock.Plugin.Tool;

public class Program
{
    static async Task<int> Main(string[] args)
    {
        return await new CommandHandler().InvokeAsync(args);
    }
}
