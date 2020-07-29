
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
        private StringBuilder builder = new StringBuilder();
        private Dictionary<string, int> localCountPerType = new Dictionary<string, int>();
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
            return local;
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

        public CppEmitter WriteCast(IXamlType type) => Emit(($"({type.FullName})"));

        public CppEmitter WriteEquals() => Emit(" = ");

        public CppEmitter WriteStatementEnd() => Emit(";\n");
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

    class CppNodeEmitResult : IXamlEmitResult
    {
        public IXamlType ReturnType => throw new System.NotImplementedException();

        public bool Valid => true;
    }
}