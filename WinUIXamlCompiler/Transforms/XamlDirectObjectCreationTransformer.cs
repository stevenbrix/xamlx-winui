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
    class XamlDirectObjectCreationTransformer : IXamlAstTransformer
    {
        public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
        {
            if (node is XamlValueWithManipulationNode valWithManip &&
                TryGetNewObjectNodeForXamlDirect(valWithManip, out var newObj) &&
                newObj.Arguments.Count == 0)
            {
                if (!(valWithManip.Manipulation is XamlObjectInitializationNode init) ||
                    !(init.Manipulation is XamlManipulationGroupNode manipGrp))
                {
                    throw new XamlParseException(
                        "Unable to find the object initialization node inside object creation.", node);
                }

                IXamlType objType = newObj.Type.GetClrType();
                if (objType.Assembly != context.GetWinUITypes().WinUIControlsAssembly)
                {
                    // Only built-in controls have XamlDirect support.
                    // Remove the XamlDirect setters since XamlDirect isn't worth using once the object
                    // is created and return.
                    RemoveXamlDirectSettersFromAssignments(manipGrp);
                    return node;
                }
                IXamlField typeIndex = context.GetWinUITypes().XamlTypeIndex.GetAllFields().FirstOrDefault(fld => fld.Name == objType.Name);
                if (typeIndex is null)
                {
                    // We didn't find a matching type index, so we can't use XamlDirect.
                    // Remove the XamlDirect setters since XamlDirect isn't worth using once the object
                    // is created and return.
                    RemoveXamlDirectSettersFromAssignments(manipGrp);

                    return node;
                }
                IXamlAstValueNode constructedObjNode = new XamlDirectNewObjectNode(newObj, context.GetWinUITypes().IXamlDirectObject, typeIndex);
                var xamlDirectAssignmentsNode = new XamlManipulationGroupNode(constructedObjNode);
                var standardAssignmentsNode = new XamlManipulationGroupNode(constructedObjNode);

                ExtractXamlDirectAssignments(manipGrp, xamlDirectAssignmentsNode, standardAssignmentsNode);

                if (xamlDirectAssignmentsNode.Children.Count != 0)
                {
                    constructedObjNode = new XamlValueWithManipulationNode(constructedObjNode, constructedObjNode, xamlDirectAssignmentsNode);
                }

                if (standardAssignmentsNode.Children.Count == 0)
                {
                    node = new XamlObjectFromDirectObjectNode(constructedObjNode, newObj.Type);
                }
                else
                {
                    valWithManip.Value = new XamlObjectFromDirectObjectNode(constructedObjNode, newObj.Type);
                    init.Manipulation = standardAssignmentsNode;
                }
            }
            return node;

            static void ExtractXamlDirectAssignments(XamlManipulationGroupNode manipGrp, XamlManipulationGroupNode xamlDirectAssignmentsNode, XamlManipulationGroupNode standardAssignmentsNode)
            {
                foreach (var manip in manipGrp.Children)
                {
                    if (manip is XamlManipulationGroupNode nestedGrp)
                    {
                        ExtractXamlDirectAssignments(nestedGrp, xamlDirectAssignmentsNode, standardAssignmentsNode);
                    }
                    else if (manip is XamlPropertyAssignmentNode assign)
                    {
                        var xamlDirectSetters = assign.PossibleSetters.OfType<IXamlDirectSetter>().ToList();
                        if (xamlDirectSetters.Count != 0)
                        {
                            assign.PossibleSetters.Clear();
                            assign.PossibleSetters.AddRange(xamlDirectSetters);
                            xamlDirectAssignmentsNode.Children.Add(assign);
                        }
                        else
                        {
                            standardAssignmentsNode.Children.Add(assign);
                        }
                    }
                    else
                    {
                        standardAssignmentsNode.Children.Add(manip);
                    }
                }
            }
        }

        private static bool TryGetNewObjectNodeForXamlDirect(XamlValueWithManipulationNode valWithManip, out XamlAstNewClrObjectNode newObj)
        {
            switch(valWithManip.Value)
            {
                case XamlDeferredContentInitializeIntermediateRootNode root:
                    newObj = root.Value as XamlAstNewClrObjectNode;
                    return !(newObj is null);
                case XamlAstNewClrObjectNode newObjNode:
                    newObj = newObjNode;
                    return true;
                default:
                    newObj = null;
                    return false;
            }
        }

        private static void RemoveXamlDirectSettersFromAssignments(XamlManipulationGroupNode manipGrp)
        {
            foreach (var manip in manipGrp.Children)
            {
                if (manip is XamlPropertyAssignmentNode assign)
                {
                    for (int i = 0; i < assign.PossibleSetters.Count;)
                    {
                        if (assign.PossibleSetters[i] is IXamlDirectSetter)
                        {
                            assign.PossibleSetters.RemoveAt(i);
                            continue;
                        }
                        i++;
                    }
                }
            }
        }
    }

    class XamlDirectNewObjectNode : XamlAstNode, IXamlAstValueNode
    {
        public XamlDirectNewObjectNode(IXamlLineInfo lineInfo, IXamlType iXamlDirectObject, IXamlField typeIndex)
            :base (lineInfo)
        {
            Type = new XamlAstClrTypeReference(lineInfo, iXamlDirectObject, false);
            TypeIndex = typeIndex;
        }

        public IXamlAstTypeReference Type { get; }
        public IXamlField TypeIndex { get; }
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
        public XamlDirectObjectFromObjectNode(IXamlAstValueNode value, IXamlType iXamlDirectObject)
            : base(value, value)
        {
            Type = new XamlAstClrTypeReference(value, iXamlDirectObject, false);
        }

        public override IXamlAstTypeReference Type { get; }
    }
}