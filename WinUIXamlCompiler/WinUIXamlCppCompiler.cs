using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WinUIXamlCompiler.Emitters.Cpp;
using WinUIXamlCompiler.Emitters.IL;
using XamlX;
using XamlX.Ast;
using XamlX.Compiler;
using XamlX.Emit;
using XamlX.IL;
using XamlX.Parsers;
using XamlX.Transform;
using XamlX.Transform.Transformers;
using XamlX.TypeSystem;

namespace WinUIXamlCompiler
{
    class WinUIXamlCppCompiler : XamlImperativeCompiler<CppEmitter, CppNodeEmitResult>
    {
        public WinUIXamlCppCompiler(TransformerConfiguration configuration, XamlLanguageEmitMappings<CppEmitter, CppNodeEmitResult> emitMappings)
        : base(configuration, emitMappings, true)
        {
            this.AddWinUIPhases();

            Emitters.Add(new XamlDirectConversionEmitter());
            Emitters.Add(new XamlDirectNewObjectEmitter());
            Emitters.Add(new XamlDirectSetterEmitter());
            Emitters.Add(new XamlDirectAdderSetterEmitter());
            Emitters.Add(new XamlDirectEventSetterEmitter());
        }

        protected override void CompileBuild(IFileSource fileSource, IXamlAstValueNode rootInstance, Func<string, IXamlType, IXamlTypeBuilder<CppEmitter>> createSubType, CppEmitter codeGen, XamlRuntimeContext<CppEmitter, CppNodeEmitResult> context, IXamlMethod compiledPopulate)
        {
            throw new NotImplementedException();
        }

        protected override void CompilePopulate(IFileSource fileSource, IXamlAstManipulationNode manipulation, Func<string, IXamlType, IXamlTypeBuilder<CppEmitter>> createSubType, CppEmitter codeGen, XamlRuntimeContext<CppEmitter, CppNodeEmitResult> context)
        {
            throw new NotImplementedException();
        }

        protected override XamlRuntimeContext<CppEmitter, CppNodeEmitResult> CreateRuntimeContext(XamlDocument doc, IXamlType contextType, IXamlTypeBuilder<CppEmitter> namespaceInfoBuilder, string baseUri, IXamlType rootType)
        {
            throw new NotImplementedException();
        }

        protected override XamlEmitContext<CppEmitter, CppNodeEmitResult> InitCodeGen(IFileSource file, Func<string, IXamlType, IXamlTypeBuilder<CppEmitter>> createSubType, CppEmitter codeGen, XamlRuntimeContext<CppEmitter, CppNodeEmitResult> context, bool needContextLocal)
        {
            throw new NotImplementedException();
        }
    }
}
