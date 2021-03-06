using System.Linq;
using ofreact;
using YamlDotNet.RepresentationModel;

namespace osu.Framework.Declarative.Yaml
{
    /// <summary>
    /// Resolves an element prop using attributes that implement <see cref="IPropResolver"/>.
    /// </summary>
    public class AttributePropResolver : IPropResolver
    {
        public IPropProvider Resolve(ComponentBuilderContext context, ElementBuilder element, PropTypeInfo prop, YamlNode node)
            => prop.GetAttributes()
                   .OfType<IPropResolver>()
                   .Select(r => r.Resolve(context, element, prop, node))
                   .FirstOrDefault(p => p != null);
    }
}