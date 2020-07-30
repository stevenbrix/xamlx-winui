using XamlX.Emit;
using XamlX.IL;
using XamlX.Transform;
using XamlX.TypeSystem;

namespace WinUIXamlCompiler
{
    class WellKnownWinUITypes
    {
        public IXamlAssembly WinUIControlsAssembly { get; }
        public IXamlType XamlDirect { get; }
        public IXamlType IXamlDirectObject { get; }
        public IXamlType XamlTypeIndex { get; }
        public IXamlType XamlPropertyIndex { get; }
        public IXamlType XamlEventIndex { get; }
        public IXamlType BindingBase { get; }
        public IXamlType DependencyProperty { get; }
        public IXamlType DependencyObject { get; }

        public WellKnownWinUITypes(TransformerConfiguration cfg)
        {
            WinUIControlsAssembly = cfg.TypeSystem.FindAssembly("Microsoft.WinUI"); // TODO: Make configurable for C++
            XamlDirect = cfg.TypeSystem.FindType("Microsoft.UI.Xaml.Core.Direct.XamlDirect");
            IXamlDirectObject = cfg.TypeSystem.FindType("Microsoft.UI.Xaml.Core.Direct.IXamlDirectObject");
            XamlTypeIndex = cfg.TypeSystem.FindType("Microsoft.UI.Xaml.Core.Direct.XamlTypeIndex");
            XamlPropertyIndex = cfg.TypeSystem.FindType("Microsoft.UI.Xaml.Core.Direct.XamlPropertyIndex");
            XamlEventIndex = cfg.TypeSystem.FindType("Microsoft.UI.Xaml.Core.Direct.XamlEventIndex");
            BindingBase = cfg.TypeSystem.FindType("Microsoft.UI.Xaml.Data.BindingBase");
            DependencyProperty = cfg.TypeSystem.FindType("Microsoft.UI.Xaml.DependencyProperty");
        }
    }
}