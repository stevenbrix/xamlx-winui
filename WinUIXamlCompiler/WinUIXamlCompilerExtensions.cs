using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WinUIXamlCompiler.Transforms;
using XamlX.Ast;
using XamlX.Compiler;
using XamlX.Emit;
using XamlX.Transform;
using XamlX.Transform.Transformers;

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

            InsertAfter<NewObjectTransformer>(new XamlDirectTransformer());
        }

        public static WellKnownWinUITypes GetWinUITypes(this AstTransformationContext ctx)
        {
            if (ctx.TryGetItem<WellKnownWinUITypes>(out var rv))
                return rv;
            ctx.SetItem(rv = new WellKnownWinUITypes(ctx.Configuration));
            return rv;
        }
        
        public static WellKnownWinUITypes GetWinUITypes<TBackendCompiler, TEmitResult>(this XamlEmitContext<TBackendCompiler, TEmitResult> ctx)
                    where TEmitResult : IXamlEmitResult
        {
            if (ctx.TryGetItem<WellKnownWinUITypes>(out var rv))
                return rv;
            ctx.SetItem(rv = new WellKnownWinUITypes(ctx.Configuration));
            return rv;
        }
    }
}
