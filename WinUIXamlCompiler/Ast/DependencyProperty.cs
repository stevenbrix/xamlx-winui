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
    class DependencyProperty : XamlAstClrProperty
    {
        private readonly IXamlProperty dependencyProperty;

        public DependencyProperty(XamlAstClrProperty original, IXamlProperty dependencyPropertyProp,
            WellKnownWinUITypes types)
            :base(original, original.Name, original.DeclaringType, original.Getter, original.Setters)
        {
            dependencyProperty = dependencyPropertyProp;
            Setters.Insert(0, new BindingSetter(types, original.DeclaringType, dependencyPropertyProp));
        }
    }

    abstract class DependencyPropertyCustomSetter : IXamlPropertySetter
    {
        protected readonly WellKnownWinUITypes Types;
        protected readonly IXamlProperty DependencyProperty;

        public DependencyPropertyCustomSetter(WellKnownWinUITypes types,
            IXamlType declaringType,
            IXamlProperty dependencyProperty)
        {
            Types = types;
            DependencyProperty = dependencyProperty;
            TargetType = declaringType;
        }

        public IXamlType TargetType { get; }

        public PropertySetterBinderParameters BinderParameters { get; } = new PropertySetterBinderParameters
        {
            AllowXNull = false
        };

        public IReadOnlyList<IXamlType> Parameters { get; set; }
    }

    class BindingSetter : DependencyPropertyCustomSetter
    {
        public BindingSetter(WellKnownWinUITypes types,
            IXamlType declaringType,
            IXamlProperty dependencyProperty) : base(types, declaringType, dependencyProperty)
        {
            Parameters = new[] {types.BindingBase};
        }
    }
}