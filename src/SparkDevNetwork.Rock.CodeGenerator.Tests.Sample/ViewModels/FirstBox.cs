namespace SparkDevNetwork.Rock.CodeGenerator.Tests.Sample.ViewModels;

using SparkDevNetwork.Rock.CodeGenerator.Tests.Sample.ViewModels.SubFolder;

public class FirstBox
{
    public FirstBag? First { get; set; }

    public SecondBox? Second { get; set; }

    public NotValidBag? InvalidBag { get; set; }

    public NotValidEnum InvalidEnum { get; set; }
}
