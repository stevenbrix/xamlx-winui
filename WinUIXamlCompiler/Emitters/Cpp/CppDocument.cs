
using System.Collections.Generic;

namespace WinUIXamlCompiler.Emitters.Cpp
{
    class CppDocument
    {
        private List<string> Includes { get; } = new List<string>();
        private List<CppClass> Classes { get; } = new List<CppClass>();

        public void Write(string destinationPath)
        {
            
        }
    }
}