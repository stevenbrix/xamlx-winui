using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using XamlX;
using XamlX.Ast;
using XamlX.Emit;
using XamlX.IL;
using XamlX.Parsers;
using XamlX.Transform;
using XamlX.Transform.Transformers;
using XamlX.TypeSystem;

namespace WinUIXamlCompiler
{
    class WinUIXamlILCompiler : XamlILCompiler
    {
        public WinUIXamlILCompiler(TransformerConfiguration configuration, XamlLanguageEmitMappings<IXamlILEmitter, XamlILNodeEmitResult> emitMappings) : base(configuration, emitMappings, true)
        {
        }
    }
}
