using System;
using System.Collections.Generic;
using System.Text;
using XamlX.TypeSystem;

namespace WinUIXamlCompiler
{
    class XamlFileSource : IFileSource
    {
        public XamlFileSource(string path, byte[] contents)
        {
            FilePath = path;
            FileContents = contents;
        }

        public string FilePath { get; }

        public byte[] FileContents { get; }
    }
}
