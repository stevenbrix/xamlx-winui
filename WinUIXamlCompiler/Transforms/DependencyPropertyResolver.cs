using System.Collections.Generic;
using System.Linq;
using XamlX;
using XamlX.Ast;
using XamlX.Emit;
using XamlX.IL;
using XamlX.Transform;
using XamlX.TypeSystem;
using WinUIXamlCompiler.Ast;

namespace WinUIXamlCompiler.Transforms
{
    class DependencyPropertyResolver : IXamlAstTransformer
    {
        public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
        {
            if (node is XamlAstClrProperty prop)
            {
                IXamlProperty dependencyPropertyProp = prop.DeclaringType.Properties
                    .FirstOrDefault(p => p.Getter?.IsStatic == true &&
                        p.Name == prop.Name + "Property" &&
                        p.PropertyType == context.GetWinUITypes().DependencyProperty);
                if (dependencyPropertyProp is null)
                {
                    return node;
                }
                return new DependencyProperty(prop, dependencyPropertyProp, context.GetWinUITypes());
            }
            return node;
        }
    }
}