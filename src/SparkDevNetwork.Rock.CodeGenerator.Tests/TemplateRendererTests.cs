namespace SparkDevNetwork.Rock.CodeGenerator.Tests;

public class TemplateRendererTests
{
    [Theory]
    [InlineData( typeof( FluidTemplateRenderer ) )]
    public void SupportsCamelCaseFilter( Type rendererType )
    {
        var renderer = ( ITemplateRenderer? ) Activator.CreateInstance( rendererType );
        var mergeFields = new Dictionary<string, object>();

        Assert.NotNull( renderer );

        var text = renderer.Render( "{{ 'DefinedValue' | CamelCase }}", mergeFields );

        Assert.Equal( "definedValue", text );
    }

    [Theory]
    [InlineData( typeof( FluidTemplateRenderer ) )]
    public void SupportsDowncaseFilter( Type rendererType )
    {
        var renderer = ( ITemplateRenderer? ) Activator.CreateInstance( rendererType );
        var mergeFields = new Dictionary<string, object>();

        Assert.NotNull( renderer );

        var text = renderer.Render( "{{ 'DefinedValue' | Downcase }}", mergeFields );

        Assert.Equal( "definedvalue", text );
    }

    [Theory]
    [InlineData( typeof( FluidTemplateRenderer ) )]
    public void SupportsJoinFilter( Type rendererType )
    {
        var renderer = ( ITemplateRenderer? ) Activator.CreateInstance( rendererType );
        var mergeFields = new Dictionary<string, object>
        {
            ["Items"] = new List<string> { "one", "two", "three" }
        };

        Assert.NotNull( renderer );

        var text = renderer.Render( "{{ Items | Join:', ' }}", mergeFields );

        Assert.Equal( "one, two, three", text );
    }

    [Theory]
    [InlineData( typeof( FluidTemplateRenderer ) )]
    public void SupportsPluralizeFilter( Type rendererType )
    {
        var renderer = ( ITemplateRenderer? ) Activator.CreateInstance( rendererType );
        var mergeFields = new Dictionary<string, object>();

        Assert.NotNull( renderer );

        var text = renderer.Render( "{{ 'Campus' | Pluralize }}", mergeFields );

        Assert.Equal( "Campuses", text );
    }

    [Theory]
    [InlineData( typeof( FluidTemplateRenderer ) )]
    public void SupportsSplitCaseFilter( Type rendererType )
    {
        var renderer = ( ITemplateRenderer? ) Activator.CreateInstance( rendererType );
        var mergeFields = new Dictionary<string, object>();

        Assert.NotNull( renderer );

        var text = renderer.Render( "{{ 'DefinedValue' | SplitCase }}", mergeFields );

        Assert.Equal( "Defined Value", text );
    }
}
