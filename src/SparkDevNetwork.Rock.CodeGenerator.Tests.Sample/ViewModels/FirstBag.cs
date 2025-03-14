using SparkDevNetwork.Rock.CodeGenerator.Tests.Sample.Enums;
using SparkDevNetwork.Rock.CodeGenerator.Tests.Sample.Enums.Codes;
using SparkDevNetwork.Rock.CodeGenerator.Tests.Sample.ViewModels.SubFolder;

namespace SparkDevNetwork.Rock.CodeGenerator.Tests.Sample.ViewModels;

public class FirstBag
{
    public int Id { get; set; }

    public Status Status { get; set; }

    public ErrorType Error { get; set; }

    public Response ResponseCode { get; set; }

    public ThirdBag? State { get; set; }

    public List<ThirdBag>? States { get; set; }

    public Guid GuidValue { get; set; }

    public Guid? OptionalGuidValue { get; set; }
}
