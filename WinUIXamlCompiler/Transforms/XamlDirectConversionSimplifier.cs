using System.Collections.Generic;
using System.Linq;
using WinUIXamlCompiler.Ast;
using XamlX;
using XamlX.Ast;
using XamlX.Emit;
using XamlX.IL;
using XamlX.Transform;
using XamlX.TypeSystem;

namespace WinUIXamlCompiler.Transforms
{
    class XamlDirectConversionSimplifier : IXamlAstTransformer
    {
        public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
        {
            // By the time this transform runs, either all setters will be XamlDirect setters
            // or none will be.
            if (node is XamlPropertyAssignmentNode assign &&
                assign.PossibleSetters[0] is IXamlDirectSetter)
            {
                for (int i = 0; i < assign.Values.Count; i++)
                {
                    if (assign.Values[i] is XamlObjectFromDirectObjectNode objFromDirect)
                    {
                        assign.Values[i] = objFromDirect.Value;
                    }
                    else if (assign.Values[i] is XamlValueWithManipulationNode || assign.PossibleSetters[0] is XamlDirectAdderSetter)
                    {
                        assign.Values[i] = new XamlDirectObjectFromObjectNode(assign.Values[0], context.GetWinUITypes().IXamlDirectObject);
                    }
                }

                foreach (var setter in assign.PossibleSetters)
                {
                    IXamlDirectSetter directSetter = (IXamlDirectSetter)setter;
                    directSetter.PrepareForEmit();
                }
            }
            return node;
        }
    }
}