using System.Collections.Generic;
using System.Linq;
using XamlX;
using XamlX.Ast;
using XamlX.Emit;
using XamlX.IL;
using XamlX.Transform;
using XamlX.TypeSystem;

namespace WinUIXamlCompiler.Ast
{
    interface IXamlDirectSetter : IXamlPropertySetter
    {
        void ChangeEmitSetterType(IXamlType newType);
    }

    class XamlDirectSetter : IXamlPropertySetter, IXamlDirectSetter
    {
        public XamlDirectSetter(
            WellKnownWinUITypes winUITypes,
            IXamlType propertyType,
            IXamlType targetType,
            IXamlField propertyIndex)
        {
            Parameters = new[] { propertyType };
            WinUITypes = winUITypes;
            TargetType = targetType;
            PropertyIndex = propertyIndex;
        }

        public WellKnownWinUITypes WinUITypes { get; }
        public IXamlType TargetType { get; }
        public IXamlField PropertyIndex { get; }
        public PropertySetterBinderParameters BinderParameters { get; } = new PropertySetterBinderParameters();

        public IReadOnlyList<IXamlType> Parameters { get; private set; }

        public void ChangeEmitSetterType(IXamlType setterType)
        {
            Parameters = new [] { setterType };
        }
    }

    class XamlDirectAdderSetter : IXamlPropertySetter, IXamlDirectSetter
    {
        public XamlDirectAdderSetter(WellKnownWinUITypes winUITypes,
            IXamlType propertyType,
            IXamlType targetType,
            IXamlField propertyIndex)
        {
            Parameters = new[] { propertyType };
            WinUITypes = winUITypes;
            TargetType = targetType;
            PropertyIndex = propertyIndex;
        }

        public WellKnownWinUITypes WinUITypes { get; }
        public IXamlType TargetType { get; }
        public IXamlField PropertyIndex { get; }
        public PropertySetterBinderParameters BinderParameters { get; } = new PropertySetterBinderParameters { AllowMultiple = true };

        public IReadOnlyList<IXamlType> Parameters { get; private set; }

        public void ChangeEmitSetterType(IXamlType setterType)
        {
            Parameters = new [] { setterType };
        }
    }
}