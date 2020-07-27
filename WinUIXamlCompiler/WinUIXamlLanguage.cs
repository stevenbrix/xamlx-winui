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
            var langaugeMappings = new XamlLanguageTypeMappings(typeSystem, useDefault: false)
            {
                ServiceProvider = typeSystem.GetType("Microsoft.UI.Xaml.IXamlServiceProvider"),
                ContentAttributes = 
                {
                    typeSystem.GetType("Microsoft.UI.Xaml.Markup.ContentPropertyAttribute"),
                    typeSystem.GetType("Windows.UI.Xaml.Markup.ContentPropertyAttribute"),
                }
            };

            var emitMappings = new XamlLanguageEmitMappings<TBackendEmitter, TEmitResult>();

            return (langaugeMappings, emitMappings);
        }
    }
}