using System.Collections.Generic;
using System.Linq;
using XamlX;
using XamlX.Ast;
using XamlX.Emit;
using XamlX.IL;
using XamlX.Transform;
using XamlX.TypeSystem;
using WinUIXamlCompiler.Transforms;
using WinUIXamlCompiler.Ast;

namespace WinUIXamlCompiler.Emitters.IL
{
    class XamlDirectConversionEmitter : IXamlAstNodeEmitter<IXamlILEmitter, XamlILNodeEmitResult>
    {
        public XamlILNodeEmitResult Emit(IXamlAstNode node, XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> context, IXamlILEmitter codeGen)
        {
            if (node is XamlObjectFromDirectObjectNode objNode)
            {
                IXamlType xamlDirectType = context.GetWinUITypes().XamlDirect;
                codeGen
                    .EmitCall(xamlDirectType.GetMethod(new FindMethodMethodSignature("GetDefault", xamlDirectType) { IsStatic = true }));
                    
                context.Emit(objNode.Value, codeGen, context.GetWinUITypes().IXamlDirectObject);
                codeGen
                    .EmitCall(xamlDirectType.GetMethod(new FindMethodMethodSignature("GetObject", context.Configuration.WellKnownTypes.Object, context.GetWinUITypes().IXamlDirectObject)))
                    .Castclass(objNode.Type.GetClrType());
                return XamlILNodeEmitResult.Type(0, objNode.Type.GetClrType());
            }
            else if (node is XamlDirectObjectFromObjectNode directObjNode)
            {
                IXamlType xamlDirectType = context.GetWinUITypes().XamlDirect;
                codeGen
                    .EmitCall(xamlDirectType.GetMethod(new FindMethodMethodSignature("GetDefault", xamlDirectType) { IsStatic = true }));
                    
                context.Emit(directObjNode.Value, codeGen, context.Configuration.WellKnownTypes.Object);
                codeGen
                    .EmitCall(xamlDirectType.GetMethod(new FindMethodMethodSignature("GetXamlDirectObject", context.GetWinUITypes().IXamlDirectObject, context.Configuration.WellKnownTypes.Object)));
                return XamlILNodeEmitResult.Type(0, context.GetWinUITypes().IXamlDirectObject);
            }
            return null;
        }
    }

    class XamlDirectNewObjectEmitter : IXamlAstNodeEmitter<IXamlILEmitter, XamlILNodeEmitResult>
    {
        public XamlILNodeEmitResult Emit(IXamlAstNode node, XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> context, IXamlILEmitter codeGen)
        {
            if (node is XamlDirectNewObjectNode objNode)
            {
                IXamlType xamlDirectType = context.GetWinUITypes().XamlDirect;
                codeGen
                    .EmitCall(xamlDirectType.GetMethod(new FindMethodMethodSignature("GetDefault", xamlDirectType) { IsStatic = true }))
                    .Ldsfld(objNode.TypeIndex)
                    .EmitCall(xamlDirectType.GetMethod(new FindMethodMethodSignature("CreateInstance", context.GetWinUITypes().IXamlDirectObject, context.GetWinUITypes().XamlTypeIndex)));
                return XamlILNodeEmitResult.Type(0, context.GetWinUITypes().IXamlDirectObject);
            }
            return null;
        }
    }

    class XamlDirectSetterEmitter : IXamlPropertySetterEmitter<IXamlILEmitter>
    {
        public bool EmitCall(IXamlPropertySetter setter, IXamlILEmitter emitter)
        {
            if (setter is XamlDirectSetter xdirect)
            {
                var paramType = setter.Parameters[0];
                var expectedParameters = new [] { xdirect.WinUITypes.IXamlDirectObject, xdirect.WinUITypes.XamlPropertyIndex, paramType};
                
                IXamlType xamlDirectType = xdirect.WinUITypes.XamlDirect;
                var setterMethod = xamlDirectType
                    .FindMethod(m => !m.IsStatic &&
                        m.Name.StartsWith("Set") &&
                        m.ReturnType == emitter.TypeSystem.FindType("System.Void") &&
                        m.Parameters.SequenceEqual(expectedParameters));

                using (var objLocal = emitter.LocalsPool.GetLocal(xdirect.WinUITypes.IXamlDirectObject))
                using (var valLocal = emitter.LocalsPool.GetLocal(paramType))
                {
                    emitter
                        .Stloc(valLocal.Local)
                        .Stloc(objLocal.Local)
                        .EmitCall(xamlDirectType.GetMethod(new FindMethodMethodSignature("GetDefault", xamlDirectType) { IsStatic = true }))
                        .Ldloc(objLocal.Local)
                        .Ldsfld(xdirect.PropertyIndex)
                        .Ldloc(valLocal.Local)
                        .EmitCall(setterMethod);
                }
                return true;
            }
            return false;
        }
    }

    class XamlDirectAdderSetterEmitter : IXamlPropertySetterEmitter<IXamlILEmitter>
    {
        public bool EmitCall(IXamlPropertySetter setter, IXamlILEmitter emitter)
        {
            if (setter is XamlDirectAdderSetter xdirect)
            {
                var paramType = setter.Parameters[0];
                
                IXamlType xamlDirectType = xdirect.WinUITypes.XamlDirect;
                var getCollectionMethod = xamlDirectType.GetMethod(
                    new FindMethodMethodSignature("GetXamlDirectObjectProperty",
                        xdirect.WinUITypes.IXamlDirectObject,
                        xdirect.WinUITypes.IXamlDirectObject,
                        xdirect.WinUITypes.XamlPropertyIndex));
                var addToCollection = xamlDirectType.GetMethod(
                    new FindMethodMethodSignature("AddToCollection",
                        emitter.TypeSystem.GetType("System.Void"),
                        xdirect.WinUITypes.IXamlDirectObject,
                        xdirect.WinUITypes.IXamlDirectObject));

                using (var objLocal = emitter.LocalsPool.GetLocal(xdirect.WinUITypes.IXamlDirectObject))
                using (var valLocal = emitter.LocalsPool.GetLocal(paramType))
                {
                    emitter
                        .Stloc(valLocal.Local)
                        .Stloc(objLocal.Local)
                        .EmitCall(xamlDirectType.GetMethod(new FindMethodMethodSignature("GetDefault", xamlDirectType) { IsStatic = true }))
                        .Dup();

                    emitter
                        .Ldloc(objLocal.Local)
                        .Ldsfld(xdirect.PropertyIndex)
                        .EmitCall(getCollectionMethod);

                    if (paramType != xdirect.WinUITypes.IXamlDirectObject)
                    {
                        emitter
                            .EmitCall(xamlDirectType.GetMethod(new FindMethodMethodSignature("GetDefault", xamlDirectType) { IsStatic = true }))
                            .Ldloc(valLocal.Local)
                            .EmitCall(xamlDirectType.FindMethod(m => m.Name == "GetXamlDirectObject"));
                    }
                    else
                    {
                        emitter.Ldloc(valLocal.Local);
                    }

                    emitter
                        .EmitCall(addToCollection);
                }
                return true;
            }
            return false;
        }
    }
    class XamlDirectEventSetterEmitter : IXamlPropertySetterEmitter<IXamlILEmitter>
    {
        public bool EmitCall(IXamlPropertySetter setter, IXamlILEmitter emitter)
        {
            if (setter is XamlDirectEventSetter xdirect)
            {
                var paramType = setter.Parameters[0];
                var expectedParameters = new [] { xdirect.WinUITypes.IXamlDirectObject, xdirect.WinUITypes.XamlPropertyIndex, paramType};
                
                IXamlType xamlDirectType = xdirect.WinUITypes.XamlDirect;
                var setterMethod = xamlDirectType.GetMethod(
                    new FindMethodMethodSignature("AddEventHandler",
                        emitter.TypeSystem.GetType("System.Void"),
                        xdirect.WinUITypes.IXamlDirectObject,
                        xdirect.WinUITypes.XamlEventIndex,
                        emitter.TypeSystem.GetType("System.Object")));

                using (var objLocal = emitter.LocalsPool.GetLocal(xdirect.WinUITypes.IXamlDirectObject))
                using (var valLocal = emitter.LocalsPool.GetLocal(paramType))
                {
                    emitter
                        .Stloc(valLocal.Local)
                        .Stloc(objLocal.Local)
                        .EmitCall(xamlDirectType.GetMethod(new FindMethodMethodSignature("GetDefault", xamlDirectType) { IsStatic = true }))
                        .Ldloc(objLocal.Local)
                        .Ldsfld(xdirect.EventIndex)
                        .Ldloc(valLocal.Local)
                        .EmitCall(setterMethod);
                }
                return true;
            }
            return false;
        }
    }
}