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
                if (prop.DeclaringType.Assembly == context.GetWinUITypes().WinUIControlsAssembly)
                {
                    IXamlField propertyIndexMaybe = context.GetWinUITypes().XamlPropertyIndex.Fields.FirstOrDefault(f => f.Name == $"{prop.DeclaringType.Name}_{prop.Name}");
                    if (propertyIndexMaybe != null)
                    {
                        prop.Setters.Insert(0, new XamlDirectSetter(context.GetWinUITypes(), prop.Getter.ReturnType, prop.DeclaringType, propertyIndexMaybe));
                        foreach (var adder in XamlTransformHelpers.FindPossibleAdders(context, prop.Getter.ReturnType))
                        {
                            if (adder.Parameters.Count == 1)
                            {
                                prop.Setters.Add(new XamlDirectAdderSetter(context.GetWinUITypes(), adder.Parameters[0], prop.DeclaringType, propertyIndexMaybe));
                            }
                        }
                        return prop;
                    }
                    IXamlField eventIndexMaybe = context.GetWinUITypes().XamlEventIndex.Fields.FirstOrDefault(f => f.Name == $"{prop.DeclaringType.Name}_{prop.Name}");
                    if (eventIndexMaybe != null)
                    {
                        prop.Setters.Insert(0, new XamlDirectEventSetter(context.GetWinUITypes(), prop.Setters[0].Parameters[0], prop.DeclaringType, eventIndexMaybe));
                        return prop;
                    }
                }
            }
            return node;
        }
    }
}