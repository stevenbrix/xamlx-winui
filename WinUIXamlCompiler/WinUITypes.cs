using XamlX.Emit;
using XamlX.IL;
using XamlX.Transform;
using XamlX.TypeSystem;

namespace WinUIXamlCompiler
{
    class WellKnownWinUITypes
    {
        public IXamlType XamlDirect { get; }
        public IXamlType IXamlDirectObject { get; }

        public WellKnownWinUITypes(TransformerConfiguration cfg)
        {
            XamlDirect = cfg.TypeSystem.FindType("Microsoft.UI.Xaml.Core.Direct.XamlDirect");
            IXamlDirectObject = cfg.TypeSystem.FindType("Microsoft.UI.Xaml.Core.Direct.IXamlDirectObject");
        }
    }
}