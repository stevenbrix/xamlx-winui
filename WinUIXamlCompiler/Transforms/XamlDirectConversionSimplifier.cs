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
                foreach (var setter in assign.PossibleSetters)
                {
                    IXamlDirectSetter directSetter = (IXamlDirectSetter)setter;
                    
                    // Up to this point, the type of the setter parameter matches the actual property type
                    // so that the compiler correctly resolves the setter as a possible setter.
                    // For the emit phase, we need to change the setter type to match the actual type
                    // that the emitted code expects.
                    // Here we change the setter type from a DependencyObject-derived type to IXamlDirectObject
                    // so that the emitters know that the actual expected parameter type is IXamlDirectObject.
                    // Additionally, if we are setting a property that is type object with a DependencyObject-derived object,
                    // we need to use the IXamlDirectObject setter.
                    if (!(directSetter is XamlDirectEventSetter))
                    {
                        if (context.GetWinUITypes().DependencyObject.IsAssignableFrom(directSetter.Parameters[0]))
                        {
                            directSetter.ChangeEmitSetterType(context.GetWinUITypes().IXamlDirectObject);
                        }
                        else if (directSetter.Parameters[0] == context.Configuration.WellKnownTypes.Object)
                        {
                            if (context.GetWinUITypes().DependencyObject.IsAssignableFrom(assign.Values[0].Type.GetClrType()))
                            {
                                directSetter.ChangeEmitSetterType(context.GetWinUITypes().IXamlDirectObject);
                            }
                        }
                    }
                }

                for (int i = 0; i < assign.Values.Count; i++)
                {
                    if (assign.Values[i] is XamlObjectFromDirectObjectNode objFromDirect)
                    {
                        assign.Values[i] = objFromDirect.Value;
                    }
                    else if (assign.Values[i] is XamlValueWithManipulationNode || assign.PossibleSetters[0].Parameters[0] == context.GetWinUITypes().IXamlDirectObject)
                    {
                        assign.Values[i] = new XamlDirectObjectFromObjectNode(assign.Values[0], context.GetWinUITypes().IXamlDirectObject);
                    }
                }

            }
            return node;
        }
    }
}