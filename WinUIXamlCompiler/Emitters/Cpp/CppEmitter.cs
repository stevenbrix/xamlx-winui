
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using XamlX.Emit;
using XamlX.TypeSystem;

namespace WinUIXamlCompiler.Emitters.Cpp
{
    class CppEmitter : IHasLocalsPool
    {
        private readonly CppMethod method;
        private readonly StringBuilder builder = new StringBuilder();
        private readonly Dictionary<string, int> localCountPerType = new Dictionary<string, int>();
        private readonly Dictionary<string, CppLabel> labels = new Dictionary<string, CppLabel>();
        public CppEmitter(CppMethod cppMethod)
        {
            LocalsPool  = new XamlLocalsPool(t => DefineLocal(t));
            method = cppMethod;
        }

        public XamlLocalsPool LocalsPool { get; }

        public IXamlLocal DefineLocal(IXamlType type)
        {
            CppLocal local;
            if (localCountPerType.TryGetValue(type.Name, out int count))
            {
                local = new CppLocal(type, $"{type.Name}_{count++}");
                localCountPerType[type.Name] = count;
            }
            else
            {
                local = new CppLocal(type, $"{type.Name}_0");
                localCountPerType[type.Name] = 1;
            }
            TypeName(type).Emit(" ").Local(local).StatementEnd();
            return local;
        }
        
        public CppLabel DefineLabel(string name)
        {
            if (labels.ContainsKey(name))
            {
                throw new InvalidOperationException("Cannot specify the same label name multiple times in the same method.");
            }
            var label = new CppLabel(name);
            return label;
        }

        public void Write(TextWriter writer)
        {
            writer.Write(builder);
        }
        
        public CppEmitter Emit(string str)
        {
            // TODO: Add indentation tracking to make code semi-readable.
            // TODO: Add support for emitting #line directives so we can get
            //       nice diagnostics and exception stacks.
            builder.Append(str);
            return this;
        }

        public CppEmitter MemberAccess(IXamlMember member)
        {
            throw new NotImplementedException();
        }

        public CppEmitter ArgName(int argNumber)
        {
            if (argNumber == 0 && !method.IsStatic)
            {
                Emit("this");
            }
            else
            {
                Emit($"arg{argNumber}");
            }
            return this;
        }

        public CppEmitter OpenParen() => Emit("(");

        public CppEmitter CloseParen() => Emit(")");

        public CppEmitter Local(IXamlLocal local) => Emit(((CppLocal)local).Name);

        public CppEmitter Cast(IXamlType type)
            => this
                .OpenParen()
                .TypeName(type)
                .CloseParen();

        public CppEmitter TypeInfo(IXamlType type)
            => this
                .Emit($"winrt::xaml_typename<")
                .TypeName(type)
                .Emit(">()");

        public CppEmitter Assign() => Emit(" = ");
        public CppEmitter Equals() => Emit(" == ");
        public CppEmitter NotEquals() => Emit(" != ");

        public CppEmitter StatementEnd() => Emit(";\n");

        public CppEmitter Return() => Emit("return ");
        public CppEmitter ThisField(IXamlField field) => Emit(field.Name);

        public CppEmitter MethodName(IXamlMethod method) => Emit(method.Name);

        public CppEmitter TypeName(IXamlType type) => Emit(type.Namespace.Replace(".", "::") + $"::{type.Name}");

        public CppEmitter ArgDelmiter() => Emit(", ");

        public CppEmitter Nullptr() => Emit("nullptr");

        public CppEmitter Throw(string hresult) => Emit($"winrt::throw_hresult({hresult});\n");
    }

    class CppLocal : IXamlLocal
    {
        public CppLocal(IXamlType type, string name)
        {
            Type = type;
            Name = name;
        }
        public string Name { get; }
        public IXamlType Type { get; }
    }

    class CppLabel : IXamlLabel
    {
        public CppLabel(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }

    class CppNodeEmitResult : IXamlEmitResult
    {
        public IXamlType ReturnType => throw new System.NotImplementedException();

        public bool Valid => true;
    }
}