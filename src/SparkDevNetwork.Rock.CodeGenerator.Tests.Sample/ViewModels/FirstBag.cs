namespace SparkDevNetwork.Rock.CodeGenerator.Tests.Sample.ViewModels;

using Enums;
using Enums.Codes;

using SparkDevNetwork.Rock.CodeGenerator.Tests.Sample.ViewModels.SubFolder;

public class FirstBag
{
    public int Id { get; set; }

    public Status Status { get; set; }

    public ErrorType Error { get; set; }

    public Response ResponseCode { get; set; }

    public ThirdBag? State { get; set; }
}
