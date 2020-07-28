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
        void PrepareForEmit();
    }

    class XamlDirectSetter : IXamlPropertySetter, IXamlDirectSetter
    {
        private readonly IXamlType _stringType;

        public XamlDirectSetter(
            WellKnownWinUITypes winUITypes,
            IXamlType stringType,
            IXamlType propertyType,
            IXamlType targetType,
            IXamlField propertyIndex)
        {
            Parameters = new[] { propertyType };
            WinUITypes = winUITypes;
            this._stringType = stringType;
            TargetType = targetType;
            PropertyIndex = propertyIndex;
        }

        public WellKnownWinUITypes WinUITypes { get; }
        public IXamlType TargetType { get; }
        public IXamlField PropertyIndex { get; }
        public PropertySetterBinderParameters BinderParameters { get; } = new PropertySetterBinderParameters();

        public IReadOnlyList<IXamlType> Parameters { get; private set; }

        public void PrepareForEmit()
        {
            if (!Parameters[0].IsValueType && Parameters[0] != _stringType)
            {
                Parameters = new [] { WinUITypes.IXamlDirectObject };
            }
        }
    }

    class XamlDirectAdderSetter : IXamlPropertySetter, IXamlDirectSetter
    {
        public XamlDirectAdderSetter(WellKnownWinUITypes winUITypes, IXamlType propertyType,
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

        public void PrepareForEmit()
        {
            // For the emitter we update the parameter type to the actual type
            // the value will be.
            Parameters = new [] { WinUITypes.IXamlDirectObject };
        }
    }
}