using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WinUIXamlCompiler.Emitters.IL;
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
            this.AddWinUIPhases();

            Emitters.Add(new XamlDirectConversionEmitter());
            Emitters.Add(new XamlDirectNewObjectEmitter());
            Emitters.Add(new XamlDirectSetterEmitter());
            Emitters.Add(new XamlDirectAdderSetterEmitter());
            Emitters.Add(new XamlDirectEventSetterEmitter());
        }
    }
}
