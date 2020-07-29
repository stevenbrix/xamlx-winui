using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using XamlX.TypeSystem;

namespace WinUIXamlCompiler.Emitters.Cpp
{
    internal class CppClass : IXamlTypeBuilder<CppEmitter>
    {
        public object Id => throw new System.NotImplementedException();

        public string Name => throw new System.NotImplementedException();

        public string Namespace => throw new System.NotImplementedException();

        public string FullName => throw new System.NotImplementedException();

        public IXamlAssembly Assembly => throw new System.NotImplementedException();

        public IReadOnlyList<IXamlProperty> Properties => throw new System.NotImplementedException();

        public IReadOnlyList<IXamlEventInfo> Events => throw new System.NotImplementedException();

        public IReadOnlyList<IXamlField> Fields => throw new System.NotImplementedException();

        public IReadOnlyList<IXamlConstructor> Constructors => throw new System.NotImplementedException();

        public IReadOnlyList<IXamlCustomAttribute> CustomAttributes => throw new System.NotImplementedException();

        public IReadOnlyList<IXamlType> GenericArguments => throw new System.NotImplementedException();

        public IXamlType GenericTypeDefinition => throw new System.NotImplementedException();

        public bool IsArray => throw new System.NotImplementedException();

        public IXamlType ArrayElementType => throw new System.NotImplementedException();

        public IXamlType BaseType => throw new System.NotImplementedException();

        public bool IsValueType => throw new System.NotImplementedException();

        public bool IsEnum => throw new System.NotImplementedException();

        public IReadOnlyList<IXamlType> Interfaces => throw new System.NotImplementedException();

        public bool IsInterface => throw new System.NotImplementedException();

        public IReadOnlyList<IXamlType> GenericParameters => throw new System.NotImplementedException();

        private List<CppMethod> Methods { get; } = new List<CppMethod>();

        IReadOnlyList<IXamlMethod> IXamlType.Methods => throw new System.NotImplementedException();

        public void AddInterfaceImplementation(IXamlType type)
        {
            throw new System.NotImplementedException();
        }

        public IXamlType CreateType()
        {
            throw new System.NotImplementedException();
        }

        public IXamlConstructorBuilder<CppEmitter> DefineConstructor(bool isStatic, params IXamlType[] args)
        {
            throw new System.NotImplementedException();
        }

        public IXamlField DefineField(IXamlType type, string name, bool isPublic, bool isStatic)
        {
            throw new System.NotImplementedException();
        }

        public void DefineGenericParameters(IReadOnlyList<KeyValuePair<string, XamlGenericParameterConstraint>> names)
        {
            throw new System.NotImplementedException();
        }

        public IXamlMethodBuilder<CppEmitter> DefineMethod(IXamlType returnType, IEnumerable<IXamlType> args, string name, bool isPublic, bool isStatic, bool isInterfaceImpl, IXamlMethod overrideMethod = null)
        {
            throw new System.NotImplementedException();
        }

        public IXamlProperty DefineProperty(IXamlType propertyType, string name, IXamlMethod setter, IXamlMethod getter)
        {
            throw new System.NotImplementedException();
        }

        public IXamlTypeBuilder<CppEmitter> DefineSubType(IXamlType baseType, string name, bool isPublic)
        {
            throw new System.NotImplementedException();
        }

        public bool Equals([AllowNull] IXamlType other)
        {
            throw new System.NotImplementedException();
        }

        public IXamlType GetEnumUnderlyingType()
        {
            throw new System.NotImplementedException();
        }

        public bool IsAssignableFrom(IXamlType type)
        {
            throw new System.NotImplementedException();
        }

        public IXamlType MakeArrayType(int dimensions)
        {
            throw new System.NotImplementedException();
        }

        public IXamlType MakeGenericType(IReadOnlyList<IXamlType> typeArguments)
        {
            throw new System.NotImplementedException();
        }

        public void Write(TextWriter writer)
        {
            
        }
    }
}