using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using XamlX.TypeSystem;

namespace WinUIXamlCompiler.Emitters.Cpp
{
    internal class CppMethod : IXamlMethodBuilder<CppEmitter>
    {
        public CppEmitter Generator => throw new System.NotImplementedException();

        public bool IsPublic => throw new System.NotImplementedException();

        public bool IsStatic => throw new System.NotImplementedException();

        public IXamlType ReturnType => throw new System.NotImplementedException();

        public IReadOnlyList<IXamlType> Parameters => throw new System.NotImplementedException();

        public IXamlType DeclaringType => throw new System.NotImplementedException();

        public IReadOnlyList<IXamlCustomAttribute> CustomAttributes => throw new System.NotImplementedException();

        public string Name => throw new System.NotImplementedException();

        public bool Equals([AllowNull] IXamlMethod other)
        {
            throw new System.NotImplementedException();
        }

        public IXamlMethod MakeGenericMethod(IReadOnlyList<IXamlType> typeArguments)
        {
            throw new System.NotImplementedException();
        }
        
        public void Write(TextWriter writer)
        {
            
        }
    }
}