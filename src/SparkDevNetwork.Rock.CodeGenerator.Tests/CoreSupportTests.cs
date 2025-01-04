namespace SparkDevNetwork.Rock.CodeGenerator.Tests;

public class CoreSupportTests
{
    [Fact]
    public void GetDomainFolderName_DoesNotModifyLowercaseDomain()
    {
        var domain = CoreSupport.GetDomainFolderName( "group" );

        Assert.Equal( "group", domain );
    }

    [Fact]
    public void GetDomainFolderName_DoesNotModifyMixedcaseDomain()
    {
        var domain = CoreSupport.GetDomainFolderName( "Group" );

        Assert.Equal( "Group", domain );
    }

    [Fact]
    public void GetDomainFolderName_DoesNotModifyTwoLetterDomain()
    {
        var domain = CoreSupport.GetDomainFolderName( "GR" );

        Assert.Equal( "GR", domain );
    }

    [Fact]
    public void GetDomainFolderName_UpdatesUppercaseDomain()
    {
        var domain = CoreSupport.GetDomainFolderName( "GROUP" );

        Assert.Equal( "Group", domain );
    }
}
