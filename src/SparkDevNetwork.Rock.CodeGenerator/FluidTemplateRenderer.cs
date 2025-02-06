using System.Collections.Generic;
using System.Threading.Tasks;

using Fluid;
using Fluid.Values;

namespace SparkDevNetwork.Rock.CodeGenerator
{
    /// <summary>
    /// Implementation of a template renderer using the Fluid engine.
    /// </summary>
    public class FluidTemplateRenderer : ITemplateRenderer
    {
        #region Fields

        /// <summary>
        /// The parser to use when rendering templates.
        /// </summary>
        private readonly FluidParser _parser = new FluidParser();

        /// <summary>
        /// The options that describe how the template should be rendered.
        /// </summary>
        private readonly TemplateOptions _options;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new instance of <see cref="FluidTemplateRenderer"/>. 
        /// </summary>
        public FluidTemplateRenderer()
        {
            _options = new TemplateOptions
            {
                MemberAccessStrategy = new UnsafeMemberAccessStrategy()
            };

            _options.Filters.AddFilter( "CamelCase", FilterCamelCase );
            _options.Filters.AddFilter( "Downcase", Fluid.Filters.StringFilters.Downcase );
            _options.Filters.AddFilter( "Join", Fluid.Filters.ArrayFilters.Join );
            _options.Filters.AddFilter( "Pluralize", FilterPluralize );
            _options.Filters.AddFilter( "SplitCase", FilterSplitCase );
        }

        #endregion

        #region Methods

        /// <inheritdoc/>
        public string Render( string source, Dictionary<string, object> mergeFields )
        {
            var template = _parser.Parse( source );
            var context = new TemplateContext( mergeFields, _options );

            return template.Render( context );
        }

        #endregion

        #region Filters

        private static ValueTask<FluidValue> FilterSplitCase( FluidValue source, FilterArguments arguments, TemplateContext context )
        {
            return new StringValue( source.ToStringValue().SplitCase() );
        }

        private static ValueTask<FluidValue> FilterCamelCase( FluidValue source, FilterArguments arguments, TemplateContext context )
        {
            return new StringValue( source.ToStringValue().ToCamelCase() );
        }

        private static ValueTask<FluidValue> FilterPluralize( FluidValue source, FilterArguments arguments, TemplateContext context )
        {
            return new StringValue( Vocabulary.Default.Pluralize( source.ToStringValue(), false ) );
        }

        #endregion
    }
}
