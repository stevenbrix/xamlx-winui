using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using XamlX.Ast;
using XamlX.Compiler;
using XamlX.Emit;
using XamlX.Transform;

namespace WinUIXamlCompiler
{
    static class WinUIXamlCompilerExtensions
    {
        public static void AddWinUIPhases<TBackendCompiler, TEmitResult>(this XamlCompiler<TBackendCompiler, TEmitResult> compiler)
            where TEmitResult : IXamlEmitResult
        {
            void InsertAfter<T>(params IXamlAstTransformer[] t)
                => compiler.Transformers.InsertRange(compiler.Transformers.FindIndex(x => x is T) + 1, t);

            void InsertBefore<T>(params IXamlAstTransformer[] t)
                => compiler.Transformers.InsertRange(compiler.Transformers.FindIndex(x => x is T), t);
        }

    }
}
