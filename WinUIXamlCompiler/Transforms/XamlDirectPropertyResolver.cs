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
    class XamlDirectPropertyResolver : IXamlAstTransformer
    {
        public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
        {
            if (node is XamlAstClrProperty prop)
            {
                if (prop.DeclaringType.Assembly == context.GetWinUITypes().WinUIControlsAssembly &&
                    prop.DeclaringType.Namespace == "Microsoft.UI.Xaml.Controls")
                {
                    IXamlField propertyIndexMaybe = context.GetWinUITypes().XamlPropertyIndex.Fields.FirstOrDefault(f => f.Name == $"{prop.DeclaringType.Name}_{prop.Name}");
                    prop.Setters.Insert(0, new XamlDirectSetter(context.GetWinUITypes(), context.Configuration.WellKnownTypes.String, prop.Getter.ReturnType, prop.DeclaringType, propertyIndexMaybe));
                    foreach (var adder in XamlTransformHelpers.FindPossibleAdders(context, prop.Getter.ReturnType))
                    {
                        if (adder.Parameters.Count == 1)
                        {
                            prop.Setters.Add(new XamlDirectAdderSetter(context.GetWinUITypes(), adder.Parameters[0], prop.DeclaringType, propertyIndexMaybe));
                        }
                    }
                }
            }
            return node;
        }
    }
}