using SparkDevNetwork.Rock.CodeGenerator.Tests.Sample.ViewModels.SubFolder;

namespace SparkDevNetwork.Rock.CodeGenerator.Tests.Sample.ViewModels;

public class SecondBag
{
    public Guid Guid { get; set; }

    public FirstBag? Item { get; set; }

    public ThirdBag? State { get; set; }
}
