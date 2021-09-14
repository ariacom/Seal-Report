using System;
using System.IO;
using System.Linq;

namespace RazorEngine.Templating
{
    /// <summary>
    /// A TemplateManager loading templates from embedded resources.
    /// </summary>
    public class EmbeddedResourceTemplateManager : ITemplateManager
    {
        /// <summary>
        /// Initializes a new TemplateManager.
        /// </summary>
        /// <param name="rootType">The type from the assembly that contains embedded resources that will act as a root type for Assembly.GetManifestResourceStream() calls.</param>
        public EmbeddedResourceTemplateManager(Type rootType)
        {
            if (rootType == null)
                throw new ArgumentNullException(nameof(rootType));

            this.RootType = rootType;
        }

        /// <summary>
        /// The type from the assembly that contains embedded resources
        /// </summary>
        public Type RootType { get; }

        /// <summary>
        /// Resolve the given key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public ITemplateSource Resolve(ITemplateKey key)
        {
            Stream stream = null;
            var name = key.Name + ".cshtml";

            try
            {
                stream = this.RootType.Assembly.GetManifestResourceStream(this.RootType, name);

                // Assemblies built using .NET Core CLI have their embedded resources named by:
                // {assembly name}.{folder structure (separated by periods)}.{name}
                //
                // ie. Test.RazorEngine.Core.Templating.Templates.Layout.cshtml where:
                // assembly name:    Test.RazorEngine.Core
                // folder structure: Templating.Templates
                // name:             Layout.cshtml
                //
                // If the namespace of the RootType does not use the generated default namespace
                // then it would be unable to resolve the resource manifest scoped by that RootType.
                if (stream == null)
                {
                    var matching = this.RootType.Assembly.GetManifestResourceNames()
                        .Where(x => x.EndsWith(name, StringComparison.Ordinal))
                        .ToArray();

                    if (matching.Length == 0)
                    {
                        throw new TemplateLoadingException(string.Format("Couldn't load resource '{0}.{1}.cshtml' from assembly {2}", this.RootType.Namespace, key.Name, this.RootType.Assembly.FullName));
                    }
                    else if (matching.Length > 1)
                    {
                        throw new TemplateLoadingException(string.Format(
                            "Could not resolve template scoped to root type {0}. Found multiple templates ending with {0} in assembly {2}!"
                            + "  Please specify a fully-qualified template name. Matching templates:  {3}",
                            this.RootType.Namespace, name, this.RootType.Assembly.FullName, string.Join("; ", matching))
                        );
                    }
                    else
                    {
                        stream = RootType.Assembly.GetManifestResourceStream(matching[0]);
                    }
                }

                using (var reader = new StreamReader(stream))
                {
                    return new LoadedTemplateSource(reader.ReadToEnd());
                }
            }
            finally
            {
                if (stream != null)
                {
                    stream.Dispose();
                }
            }
        }

        /// <summary>
        /// Get the given key.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="resolveType"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public ITemplateKey GetKey(string name, ResolveType resolveType, ITemplateKey context)
        {
            return new NameOnlyTemplateKey(name, resolveType, context);
        }

        /// <summary>
        /// Throws NotSupportedException.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="source"></param>
        public void AddDynamic(ITemplateKey key, ITemplateSource source)
        {
            throw new NotSupportedException("Adding templates dynamically is not supported. This Manager only supports embedded resources.");
        }
    }
}
