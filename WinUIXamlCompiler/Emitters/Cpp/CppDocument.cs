
using System;
using System.Collections.Generic;
using XamlX.TypeSystem;

namespace WinUIXamlCompiler.Emitters.Cpp
{
    class CppDocument
    {
        private List<string> Includes { get; } = new List<string>();
        private List<CppClass> Classes { get; } = new List<CppClass>();

        public void WriteToFile(string destinationPath)
        {
            
        }

        public void AddInclude(string include)
        {
            Includes.Add(include);
        }

        public IXamlTypeBuilder<CppEmitter> DefineClass(string name)
        {
            throw new NotImplementedException();
        }

        public IXamlType GetCppType(string name)
        {
            throw new NotImplementedException();
        }

        public void Emit(string cpp)
        {
            throw new NotImplementedException();
        }
    }
}