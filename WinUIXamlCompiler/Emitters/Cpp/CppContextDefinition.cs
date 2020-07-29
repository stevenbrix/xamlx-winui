using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using XamlX.Emit;
using XamlX.Transform;
using XamlX.TypeSystem;

namespace WinUIXamlCompiler.Emitters.Cpp
{
    
    static class XamlRuntimeContextDefintion
    {
        public const string RootObjectFieldName = "RootObject";
        public const string IntermediateRootObjectFieldName = "IntermediateRoot";
        public const string ParentListFieldName = "ParentsStack";
        public const string ProvideTargetObjectName = "ProvideTargetObject";
        public const string ProvideTargetPropertyName = "ProvideTargetProperty";
        public const string PushParentMethodName = "PushParent";
        public const string PopParentMethodName = "PopParent";
    }

    class CppContextDefinition
    {
        private readonly IXamlField ParentListField;
        private readonly IXamlField _parentServiceProviderField;
        private readonly IXamlField _innerServiceProviderField;
        private readonly IXamlField PropertyTargetObject;
        private readonly IXamlField PropertyTargetProperty;

        private IXamlConstructor Constructor { get; set; }

        public static IXamlType GenerateContextClass(IXamlTypeBuilder<CppEmitter> builder,
            IXamlTypeSystem typeSystem, XamlLanguageTypeMappings mappings,
            XamlLanguageEmitMappings<CppEmitter, CppNodeEmitResult> emitMappings,
            CppDocument document)
        {
            return new CppContextDefinition(builder, typeSystem, mappings, emitMappings, document, Array.Empty<IXamlType>()).ContextType;
        }

        public List<Action> CreateCallbacks = new List<Action>();

        public const int BaseUriArg = 0;
        public const int StaticProvidersArg = 1;
        public IXamlType ContextType;
        
        private CppContextDefinition(IXamlTypeBuilder<CppEmitter> parentBuilder,
            IXamlTypeSystem typeSystem, XamlLanguageTypeMappings mappings,
            XamlLanguageEmitMappings<CppEmitter, CppNodeEmitResult> emitMappings,
            CppDocument document,
            IXamlType[] staticProviderTypes)
        {
            var so = typeSystem.GetType("System.Object");
            var builder = parentBuilder.DefineSubType(so, "Context", true);
            
            builder.DefineGenericParameters(new[]
            {
                new KeyValuePair<string, XamlGenericParameterConstraint>("TTarget",
                    new XamlGenericParameterConstraint
                    {
                        IsClass = true
                    })
            });
            var rootObjectField = builder.DefineField(builder.GenericParameters[0], "RootObject", true, false);
            var intermediateRootObjectField = builder.DefineField(so, XamlRuntimeContextDefintion.IntermediateRootObjectFieldName, true, false);
            _parentServiceProviderField = builder.DefineField(mappings.ServiceProvider, "_sp", false, false);
            if (mappings.InnerServiceProviderFactoryMethod != null)
                _innerServiceProviderField = builder.DefineField(mappings.ServiceProvider, "_innerSp", false, false);

            var staticProvidersField = builder.DefineField(document.GetCppType($"std::tuple<{string.Join(", ", staticProviderTypes.Select(t => t.FullName.Replace(".", "::")))}>"),
                "_staticProviders", false, false);
            
            var systemType = typeSystem.GetType("Windows.UI.Xaml.Interop.TypeName");
            var systemUri = typeSystem.GetType("Windows.Foundation.Uri");
            var systemString = typeSystem.GetType("System.String");
            var getServiceInterfaceMethod = mappings.ServiceProvider.FindMethod("GetService", so, false, systemType);

            var ownServices = new List<IXamlType>();
            var ctorCallbacks = new List<Action<CppEmitter>>();
            
            if (mappings.RootObjectProvider != null)
            {
                builder.AddInterfaceImplementation(mappings.RootObjectProvider);
                var rootGen = ImplementInterfacePropertyGetter(builder, mappings.RootObjectProvider, XamlRuntimeContextDefintion.RootObjectFieldName)
                    .Generator;
                var parentRootProvider = rootGen.DefineLocal(mappings.RootObjectProvider);
                rootGen
                    .Emit($"if({rootObjectField.Name} != nullptr) return {rootObjectField.Name};\n")
                    .Emit($"if ({_parentServiceProviderField.Name} == nullptr) return nullptr;\n")
                    .Emit($"auto parentProvider = ")
                    .Cast(mappings.RootObjectProvider)
                    .Emit($"{_parentServiceProviderField.Name}.GetService")
                    .OpenParen()
                    .TypeInfo(mappings.RootObjectProvider)
                    .CloseParen()
                    .StatementEnd()
                    .Emit("if (parentProvider == nullptr) return nullptr;\n")
                    .Emit("return parentProvider.RootObject();");

                if (mappings.RootObjectProviderIntermediateRootPropertyName != null)
                    ImplementInterfacePropertyGetter(builder, mappings.RootObjectProvider, mappings.RootObjectProviderIntermediateRootPropertyName)
                        .Generator
                        .Return()
                        .ThisField(intermediateRootObjectField)
                        .StatementEnd();

                ownServices.Add(mappings.RootObjectProvider);
            }

            if (mappings.ParentStackProvider != null)
            {
                builder.AddInterfaceImplementation(mappings.ParentStackProvider);
                var objectListType = document.GetCppType("winrt::single_threaded_vector<winrt::IInspectable>");
                ParentListField = builder.DefineField(objectListType, XamlRuntimeContextDefintion.ParentListFieldName, true, false);

                var enumerator = EmitParentEnumerable(typeSystem, parentBuilder, mappings);
                CreateCallbacks.Add(enumerator.createCallback);
                var parentStackEnumerableField = builder.DefineField(
                    typeSystem.GetType("Windows.Foundation.Collections.IIterable`1").MakeGenericType(new[]{so}),
                    "_parentStackEnumerable", false, false);

                ImplementInterfacePropertyGetter(builder, mappings.ParentStackProvider, "Parents")
                    .Generator.Return().ThisField(parentStackEnumerableField).StatementEnd();
                
                ctorCallbacks.Add(g => g
                    .ThisField(ParentListField)
                    .Assign()
                    .Emit("{}")
                    .StatementEnd()
                    .ThisField(parentStackEnumerableField)
                    .Assign()
                    .TypeName(enumerator.type)
                    .OpenParen()
                    .ThisField(ParentListField)
                    .ArgDelmiter()
                    .ThisField(_parentServiceProviderField)
                    .CloseParen()
                    .StatementEnd());
                ownServices.Add(mappings.ParentStackProvider);
            }

            ownServices.Add(EmitTypeDescriptorContextStub(typeSystem, builder, mappings));

            if (mappings.ProvideValueTarget != null)
            {
                builder.AddInterfaceImplementation(mappings.ProvideValueTarget);
                PropertyTargetObject = builder.DefineField(so, XamlRuntimeContextDefintion.ProvideTargetObjectName, true, false);
                PropertyTargetProperty = builder.DefineField(so, XamlRuntimeContextDefintion.ProvideTargetPropertyName, true, false);
                ImplementInterfacePropertyGetter(builder, mappings.ProvideValueTarget, "TargetObject")
                    .Generator.Return().ThisField(PropertyTargetObject).StatementEnd();
                ImplementInterfacePropertyGetter(builder, mappings.ProvideValueTarget, "TargetProperty")
                    .Generator.Return().ThisField(PropertyTargetProperty).StatementEnd();
                ownServices.Add(mappings.ProvideValueTarget);
            }

            IXamlField baseUriField = null;
            if (mappings.UriContextProvider != null)
            {
                baseUriField = builder.DefineField(systemUri, "_baseUri", false, false);
                builder.AddInterfaceImplementation(mappings.UriContextProvider);
                var getter = builder.DefineMethod(systemUri, new IXamlType[0], "get_BaseUri", true, false, true);
                var setter = builder.DefineMethod(typeSystem.GetType("System.Void"), new[] {systemUri},
                    "set_BaseUri", true, false, true);

                getter.Generator
                    .Return().ThisField(baseUriField).StatementEnd();

                setter.Generator
                    .ThisField(baseUriField).Assign().ArgName(0).StatementEnd();
                builder.DefineProperty(systemUri, "BaseUri", setter, getter);
                
                    
                ownServices.Add(mappings.UriContextProvider);    
            }
            
            builder.AddInterfaceImplementation(mappings.ServiceProvider);
            var getServiceMethod = builder.DefineMethod(so,
                new[] {systemType},
                "GetService", true, false, true);

            ownServices = ownServices.Where(s => s != null).ToList();


            if (_innerServiceProviderField != null)
            {
                var next = getServiceMethod.Generator.DefineLabel("noInner");
                var innerResult = getServiceMethod.Generator.DefineLocal(so);
                getServiceMethod.Generator
                    .Emit("if")
                    .OpenParen()
                    .ThisField(_innerServiceProviderField)
                    .NotEquals()
                    .Nullptr()
                    .CloseParen()
                    .Emit("{\n")
                    .Local(innerResult)
                    .Assign()
                    .ThisField(_innerServiceProviderField)
                    .Emit(".")
                    .MethodName(getServiceInterfaceMethod)
                    .OpenParen()
                    .ArgName(0)
                    .CloseParen()
                    .StatementEnd()
                    .Emit("if")
                    .OpenParen()
                    .Local(innerResult)
                    .NotEquals()
                    .Nullptr()
                    .CloseParen()
                    .Return()
                    .Local(innerResult)
                    .StatementEnd()
                    .Emit("}\n");

            }
            var compare = systemType.FindMethod("Equals", typeSystem.GetType("System.Boolean"),
                false, systemType);
            var isAssignableFrom = systemType.FindMethod("IsAssignableFrom", typeSystem.GetType("System.Boolean"),
                false, systemType);
            var fromHandle = systemType.Methods.First(m => m.Name == "GetTypeFromHandle");
            var getTypeFromObject = so.Methods.First(m => m.Name == "GetType" && m.Parameters.Count == 0);
            if (ownServices.Count != 0)
            {
                getServiceMethod.Generator
                    .Emit("if")
                    .OpenParen();
                for (var c = 0; c < ownServices.Count; c++)
                {
                    if (c != 0)
                    {
                        getServiceMethod.Generator.Emit("||");
                    }
                    getServiceMethod.Generator
                        .ArgName(0)
                        .Equals()
                        .TypeInfo(ownServices[c]);
                }
                getServiceMethod.Generator
                    .CloseParen()
                    .Return()
                    .Emit("this")
                    .StatementEnd();
            }

            var staticProviderLocal = getServiceMethod.Generator.DefineLocal(typeSystem.FindType("System.Object"));

            getServiceMethod.Generator
                .Local(staticProviderLocal)
                .Assign()
                .Emit("get_element_by_type_name")
                .OpenParen()
                .ThisField(staticProvidersField)
                .ArgDelmiter()
                .ArgName(0)
                .CloseParen()
                .StatementEnd();             

            var noParentProvider = getServiceMethod.Generator.DefineLabel();
            getServiceMethod.Generator
                .LdThisFld(_parentServiceProviderField)
                .Brfalse(noParentProvider)
                .LdThisFld(_parentServiceProviderField)
                .Ldarg(1)
                .EmitCall(getServiceInterfaceMethod)
                .Emit(OpCodes.Ret)
                .MarkLabel(noParentProvider)
                .Ldnull()
                .Ret();

            var ctor = builder.DefineConstructor(false, 
                mappings.ServiceProvider,
                staticProvidersField.FieldType,
                systemString);
            ctor.Generator
                .Emit(OpCodes.Ldarg_0)
                .Emit(OpCodes.Call, so.Constructors.First())
                .Emit(OpCodes.Ldarg_0)
                .Emit(OpCodes.Ldarg_1)
                .Emit(OpCodes.Stfld, _parentServiceProviderField)
                .Emit(OpCodes.Ldarg_0)
                .Emit(OpCodes.Ldarg_2)
                .Emit(OpCodes.Stfld, staticProvidersField);
            if (baseUriField != null)
            {
                var noUri = ctor.Generator.DefineLabel();
                ctor.Generator
                    .Emit(OpCodes.Ldarg_3)
                    .Brfalse(noUri)
                    .Emit(OpCodes.Ldarg_0)
                    .Emit(OpCodes.Ldarg_3)
                    .Newobj(systemUri.FindConstructor(new List<IXamlType>
                    {
                        typeSystem.GetType("System.String")
                    }))
                    .Emit(OpCodes.Stfld, baseUriField)
                    .MarkLabel(noUri);
            }

            foreach (var feature in ctorCallbacks)
                feature(ctor.Generator);

            emitMappings.ContextTypeBuilderCallback?.Invoke(builder, ctor.Generator);
            
            // We are calling this last to ensure that our own services are ready
            if (_innerServiceProviderField != null)
                ctor.Generator
                    // _innerSp = InnerServiceProviderFactory(this)
                    .Ldarg_0()
                    .Ldarg_0()
                    .EmitCall(mappings.InnerServiceProviderFactoryMethod)
                    .Stfld(_innerServiceProviderField);
                    
            ctor.Generator.Emit(OpCodes.Ret);

            Constructor = ctor;
            CreateCallbacks.Add(() => { parentBuilder.CreateType(); });
            
            if (ParentListField != null)
            {
                EmitPushPopParent(builder, typeSystem);
            }
            
            CreateAllTypes();
            ContextType = builder.CreateType();
        }

        public void CreateAllTypes()
        {
            foreach (var cb in CreateCallbacks)
                cb();
        }

        private void EmitPushPopParent(IXamlTypeBuilder<CppEmitter> builder, IXamlTypeSystem ts)
        {
            var @void = ts.GetType("System.Void");
            var so = ts.GetType("System.Object");
            var  objectListType = ts.GetType("System.Collections.Generic.List`1")
                .MakeGenericType(new[] {so});
            
            var pushParentGenerator =
                builder.DefineMethod(@void, new[] {so}, XamlRuntimeContextDefintion.PushParentMethodName, true, false, false)
                .Generator;

            pushParentGenerator.LdThisFld(ParentListField)
                .Ldarg(1)
                .EmitCall(objectListType.FindMethod("Add", @void,
                    false, so));

            if (PropertyTargetObject != null)
            {
                pushParentGenerator.Ldarg_0()
                    .Ldarg(1)
                    .Stfld(PropertyTargetObject)
                    .Ret();
            }
            
            var pop = builder.DefineMethod(@void, new IXamlType[0], XamlRuntimeContextDefintion.PopParentMethodName, true, false, false)
                .Generator;

            var idx = pop.DefineLocal(ts.GetType("System.Int32"));
            pop
                // var idx = _parents.Count - 1;
                .LdThisFld(ParentListField)
                .EmitCall(objectListType.FindMethod(m => m.Name == "get_Count"))
                .Ldc_I4(1).Emit(OpCodes.Sub).Stloc(idx);
                // this.PropertyTargetObject = _parents[idx];
            if (PropertyTargetObject != null)
            {
                pop
                    .Ldarg_0()
                    .LdThisFld(ParentListField)
                    .Ldloc(idx)
                    .EmitCall(objectListType.FindMethod(m => m.Name == "get_Item"))
                    .Stfld(PropertyTargetObject);
            }
                // _parents.RemoveAt(idx);
            pop
                .LdThisFld(ParentListField)
                .Ldloc(idx).EmitCall(objectListType.FindMethod(m => m.Name == "RemoveAt"))
                .Ret();

        }
        
        private IXamlMethodBuilder<CppEmitter> ImplementInterfacePropertyGetter(IXamlTypeBuilder<CppEmitter> builder ,
            IXamlType type, string name)
        {
            var prefix = type.Namespace + "." + type.Name + ".";
            var originalGetter = type.FindMethod(m => m.Name == "get_" + name);
            var gen = builder.DefineMethod(originalGetter.ReturnType, new IXamlType[0],
                prefix + "get_" + name, false, false,
                true, originalGetter);
            builder.DefineProperty(originalGetter.ReturnType,prefix+ name, null, gen);
            return gen;
        }
        
        IXamlType EmitTypeDescriptorContextStub(IXamlTypeSystem typeSystem, IXamlTypeBuilder<CppEmitter> builder,
            XamlLanguageTypeMappings mappings)
        {
            if (mappings.TypeDescriptorContext == null)
                return null;
            var tdc = mappings.TypeDescriptorContext;
            var tdcPrefix = tdc.Namespace + "." + tdc.Name+".";

            builder.AddInterfaceImplementation(mappings.TypeDescriptorContext);
            void PropertyStub(string name) => ImplementInterfacePropertyGetter(builder, tdc, name).Generator.Return().Nullptr().StatementEnd();
            PropertyStub("Container");
            PropertyStub("Instance");
            PropertyStub("PropertyDescriptor");

            void MethodStub(string name)
            {
                var original = tdc.FindMethod(m => m.Name == name);
                builder.DefineMethod(original.ReturnType, original.Parameters, tdcPrefix + name, 
                        false, false, true,
                        original)
                    .Generator
                    .Throw("E_NOTIMPL");

            }
            MethodStub("OnComponentChanging");
            MethodStub("OnComponentChanged");

            return mappings.TypeDescriptorContext;
        }
        
        (IXamlType type, IXamlConstructor ctor, Action createCallback) EmitParentEnumerable(IXamlTypeSystem typeSystem, IXamlTypeBuilder<CppEmitter> parentBuilder,
            XamlLanguageTypeMappings mappings)
        {
            var so = typeSystem.GetType("System.Object");
            var enumerableBuilder =
                parentBuilder.DefineSubType(typeSystem.GetType("System.Object"), "ParentStackEnumerable", false);
            var enumerableType = typeSystem.GetType("Windows.Foundation.Collections.IIterable`1")
                .MakeGenericType(new[] {typeSystem.GetType("System.Object")});
            enumerableBuilder.AddInterfaceImplementation(enumerableType);

            var enumerableParentList =
                enumerableBuilder.DefineField(ParentListField.FieldType, "_parentList", false, false);
            var enumerableParentProvider =
                enumerableBuilder.DefineField(mappings.ServiceProvider, "_parentSP", false, false);
            
            var enumerableCtor = enumerableBuilder.DefineConstructor(false, enumerableParentList.FieldType, enumerableParentProvider.FieldType);
            enumerableCtor.Generator
                .ThisField(enumerableParentList).Assign().ArgName(0).StatementEnd()
                .ThisField(enumerableParentProvider).Assign().ArgName(1).StatementEnd();


            var enumeratorType = typeSystem.GetType("System.Collections.IEnumerator");
            var enumeratorObjectType = typeSystem.GetType("System.Collections.Generic.IEnumerator`1")
                .MakeGenericType(new[] {so});

            var enumeratorBuilder = enumerableBuilder.DefineSubType(so, "Enumerator", true);
            enumeratorBuilder.AddInterfaceImplementation(enumeratorObjectType);


            
            var state = enumeratorBuilder.DefineField(typeSystem.GetType("System.Int32"), "_state", false, false);
            
            
            var parentList =
                enumeratorBuilder.DefineField(ParentListField.FieldType, "_parentList", false, false);
            var parentProvider =
                enumeratorBuilder.DefineField(mappings.ServiceProvider, "_parentSP", false, false);
            var listType = typeSystem.GetType("System.Collections.Generic.List`1")
                .MakeGenericType(new[] {so});
            var list = enumeratorBuilder.DefineField(listType, "_list", false, false);
            var listIndex = enumeratorBuilder.DefineField(typeSystem.GetType("System.Int32"), "_listIndex", false, false);
            var current = enumeratorBuilder.DefineField(so, "_current", false, false);
            var parentEnumerator = enumeratorBuilder.DefineField(enumeratorObjectType, "_parentEnumerator", false, false);
            var enumeratorCtor = enumeratorBuilder.DefineConstructor(false, parentList.FieldType, parentProvider.FieldType);
                enumeratorCtor.Generator
                .Emit(OpCodes.Ldarg_0)
                .Emit(OpCodes.Call, so.Constructors.First())
                .Emit(OpCodes.Ldarg_0)
                .Emit(OpCodes.Ldarg_1)
                .Emit(OpCodes.Stfld, parentList)
                .Emit(OpCodes.Ldarg_0)
                .Emit(OpCodes.Ldarg_2)
                .Emit(OpCodes.Stfld, parentProvider)
                .Emit(OpCodes.Ret);
            
            var currentGetter = enumeratorBuilder.DefineMethod(so, new IXamlType[0],
                "get_Current", true, false, true);
            enumeratorBuilder.DefineProperty(so, "Current", null, currentGetter);
            currentGetter.Generator
                .Emit(OpCodes.Ldarg_0)
                .Emit(OpCodes.Ldfld, current)
                .Emit(OpCodes.Ret);

            enumeratorBuilder.DefineMethod(typeSystem.FindType("System.Void"), new IXamlType[0], "Reset", true, false,
                    true).Generator
                .Emit(OpCodes.Newobj,
                    typeSystem.FindType("System.NotSupportedException").FindConstructor(new List<IXamlType>()))
                .Emit(OpCodes.Throw);
            
            var disposeGen = enumeratorBuilder.DefineMethod(typeSystem.FindType("System.Void"), new IXamlType[0], 
                "Dispose", true, false, true ).Generator;
            var disposeRet = disposeGen.DefineLabel();
            disposeGen
                .Emit(OpCodes.Ldarg_0)
                .Emit(OpCodes.Ldfld, parentEnumerator)
                .Emit(OpCodes.Brfalse, disposeRet)
                .Emit(OpCodes.Ldarg_0)
                .Emit(OpCodes.Ldfld, parentEnumerator)
                .Emit(OpCodes.Callvirt, typeSystem.GetType("System.IDisposable").FindMethod(m => m.Name == "Dispose"))
                .MarkLabel(disposeRet)
                .Emit(OpCodes.Ret);

            var boolType = typeSystem.GetType("System.Boolean");
            var moveNext = enumeratorBuilder.DefineMethod(boolType, new IXamlType[0],
                "MoveNext", true,
                false, true).Generator;

            
            const int stateInit = 0;
            const int stateSelf = 1;
            const int stateParent = 2;
            const int stateEof = 3;
            
            var checkStateInit = moveNext.DefineLabel();
            var checkStateSelf = moveNext.DefineLabel();
            var checkStateParent = moveNext.DefineLabel();
            var eof = moveNext.DefineLabel();

            moveNext
                .LdThisFld(state).Ldc_I4(stateInit).Beq(checkStateInit)
                .LdThisFld(state).Ldc_I4(stateSelf).Beq(checkStateSelf)
                .LdThisFld(state).Ldc_I4(stateParent).Beq(checkStateParent)
                .Ldc_I4(0).Ret();
            moveNext.MarkLabel(checkStateInit)
                .Ldarg_0().Dup().Dup().Ldfld(parentList).Stfld(list).LdThisFld(list)
                .EmitCall(listType.FindMethod(m => m.Name == "get_Count" && m.Parameters.Count == 0))
                .Ldc_I4(1).Emit(OpCodes.Sub).Stfld(listIndex)
                .Ldarg_0().Ldc_I4(stateSelf).Stfld(state)
                .Br(checkStateSelf);
            var tryParentState = moveNext.DefineLabel();
            var parentProv = moveNext.DefineLocal(mappings.ParentStackProvider);
            moveNext.MarkLabel(checkStateSelf)

                // if(_listIndex<0) goto tryParent
                .LdThisFld(listIndex).Ldc_I4(0).Emit(OpCodes.Blt, tryParentState)
                // _current = _list[_listIndex]
                .Ldarg_0().LdThisFld(list).LdThisFld(listIndex)
                .EmitCall(listType.FindMethod(m => m.Name == "get_Item")).Stfld(current)
                // _listIndex--
                .Ldarg_0().LdThisFld(listIndex).Ldc_I4(1).Emit(OpCodes.Sub).Stfld(listIndex)
                // return true
                .Ldc_I4(1).Ret()
                // tryParent:
                .MarkLabel(tryParentState)
                // if(parent._parentServiceProvider == null) goto eof;
                .LdThisFld(parentProvider).Brfalse(eof)
                // parentProv = (IParentStackProvider)parent.GetService(typeof(IParentStackProvider));
                .LdThisFld(parentProvider).Ldtype(mappings.ParentStackProvider)
                .EmitCall(mappings.ServiceProvider.FindMethod("GetService", so, false,
                    typeSystem.GetType("System.Type")))
                .Emit(OpCodes.Castclass, mappings.ParentStackProvider)
                .Dup().Stloc(parentProv)
                // if(parentProv == null) goto eof
                .Brfalse(eof)
                // _parentEnumerator = parentProv.Parents.GetEnumerator()
                .Ldarg_0().Ldloc(parentProv)
                .EmitCall(mappings.ParentStackProvider.FindMethod(m => m.Name == "get_Parents"))
                .EmitCall(enumerableType.FindMethod(m => m.Name == "GetEnumerator"))
                .Stfld(parentEnumerator)
                .Ldarg_0().Ldc_I4(stateParent).Stfld(state);


            moveNext.MarkLabel(checkStateParent)
                // if(!_parentEnumerator.MoveNext()) goto eof
                .LdThisFld(parentEnumerator).EmitCall(enumeratorType.FindMethod("MoveNext", boolType, false))
                .Brfalse(eof)
                // _current = _parentEnumerator.Current
                .Ldarg_0()
                .LdThisFld(parentEnumerator).EmitCall(enumeratorObjectType.FindMethod("get_Current", so, false))
                .Stfld(current)
                // return true
                .Ldc_I4(1).Ret();
            
            moveNext.MarkLabel(eof)
                .Ldarg_0().Ldc_I4(stateEof).Stfld(state)
                .Ldc_I4(0).Ret();
                
            var createEnumerator = enumerableBuilder.DefineMethod(enumeratorObjectType, new IXamlType[0], "GetEnumerator", true, false,
                    true);
            createEnumerator.Generator
                .LdThisFld(enumerableParentList)
                .LdThisFld(enumerableParentProvider)
                .Emit(OpCodes.Newobj, enumeratorCtor)
                .Emit(OpCodes.Ret);

            enumerableBuilder.DefineMethod(enumeratorType, new IXamlType[0],
                    "System.Collections.IEnumerable.GetEnumerator", false, false, true,
                    typeSystem.GetType("System.Collections.IEnumerable").FindMethod(m => m.Name == "GetEnumerator"))
                .Generator
                .Ldarg_0()
                .EmitCall(createEnumerator)
                .Emit(OpCodes.Ret);



            return (enumeratorBuilder, enumerableCtor, () =>
            {
                enumeratorBuilder.CreateType();
                enumerableBuilder.CreateType();
            });
        }
    }
}
