using System;
using XamlX.Emit;
using XamlX.Transform;
using XamlX.TypeSystem;

namespace WinUIXamlCompiler
{
    internal class WinUIXamlLanguage
    {
        internal static (XamlLanguageTypeMappings, XamlLanguageEmitMappings<TBackendEmitter, TEmitResult>) Configure<TBackendEmitter, TEmitResult>(CecilTypeSystem typeSystem)
        where TEmitResult : IXamlEmitResult
        {
            throw new NotImplementedException();
        }
    }
}