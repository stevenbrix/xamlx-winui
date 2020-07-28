using System.Linq;
using XamlX.Ast;
using XamlX.Emit;
using XamlX.IL;
using XamlX.Transform;
using XamlX.TypeSystem;

namespace WinUIXamlCompiler.Transforms
{
    class XamlDirectTransformer : IXamlAstTransformer
    {
        public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
        {
            if (node is XamlValueWithManipulationNode valWithManip &&
                valWithManip.Value is XamlAstNewClrObjectNode newObj &&
                newObj.Arguments.Count == 0)
            {
                IXamlType objType = newObj.Type.GetClrType();
                if (objType.Namespace != "Microsoft.UI.Xaml.Controls" || objType.Assembly != context.GetWinUITypes().WinUIControlsAssembly)
                {
                    // Only built-in controls have XamlDirect support.
                    return node;
                }
                IXamlField typeIndex = context.GetWinUITypes().XamlTypeIndex.GetAllFields().FirstOrDefault(fld => fld.Name == objType.Name);
                if (typeIndex is null)
                {
                    // We didn't find a matching type index, so we can't use XamlDirect.
                    return node;
                }
                var constructedObjNode = new XamlDirectNewObjectNode(newObj, context.GetWinUITypes().IXamlDirectObject, typeIndex);
                // TODO: Transform properties and events to use XamlDirect when possible.

                valWithManip.Value = new XamlObjectFromDirectObjectNode(constructedObjNode, newObj.Type);
            }
            return node;
        }
    }

    class XamlDirectNewObjectNode : XamlAstNode, IXamlAstValueNode
    {
        public XamlDirectNewObjectNode(IXamlLineInfo lineInfo, IXamlType iXamlDirectObject, IXamlField typeIndex)
            :base (lineInfo)
        {
            Type = new XamlAstClrTypeReference(lineInfo, iXamlDirectObject, false);
        }

        public IXamlAstTypeReference Type { get; }
    }

    class XamlObjectFromDirectObjectNode : XamlValueWithSideEffectNodeBase
    {
        public XamlObjectFromDirectObjectNode(IXamlAstValueNode value, IXamlAstTypeReference targetType)
            : base(value, value)
        {
            Type = targetType;
        }

        public override IXamlAstTypeReference Type { get; }
    }

    class XamlDirectObjectFromObjectNode : XamlValueWithSideEffectNodeBase
    {
        public XamlDirectObjectFromObjectNode( IXamlAstValueNode value, IXamlType iXamlDirectObject)
            : base(value, value)
        {
            Type = new XamlAstClrTypeReference(value, iXamlDirectObject, false);
        }

        public override IXamlAstTypeReference Type { get; }
    }
}