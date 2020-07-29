using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WinUIXamlCompiler.Emitters.Cpp;
using XamlX;
using XamlX.Ast;
using XamlX.Compiler;
using XamlX.Emit;
using XamlX.IL;
using XamlX.Parsers;
using XamlX.Transform;
using XamlX.TypeSystem;

namespace WinUIXamlCompiler
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var cppCommand = new Command("cpp", "Compile Xaml to C++/WinRT")
            {
                new Option<DirectoryInfo>("-o", "The output directory for the generated C++ code.") { IsRequired = true }
            };

            var ilCommand = new Command("il", "Compile Xaml to IL and embed in a .NET assembly")
            {
                new Option<string>("-i", "The input .NET assembly") { IsRequired = true },
                new Option<string>("-o", "The ouput path for the updated .NET assembly.") { IsRequired = true }
            };

            var xamlFilesArgument = new Option<string[]>(
                "--xaml",
                "The input xaml files.")
                {
                    IsRequired = true
                };

            xamlFilesArgument.AddAlias("-x");

            var referencesArgument = new Option<string[]>(
                "--reference",
                "The reference metadata files (.NET assemblies or WinMD files).")
                {
                    IsRequired = true
                };
            referencesArgument.AddAlias("-r");

            var rootCommand = new RootCommand
            {
                cppCommand,
                ilCommand,
                xamlFilesArgument,
                referencesArgument
            };

            rootCommand.Description = "WinUI Xaml Compiler";
            
            // Note that the parameters of the handler method are matched according to the names of the options
            rootCommand.Handler = CommandHandler.Create<string[], string[]>((xaml, reference) =>
            {
                Console.WriteLine("A Xaml compliation target such as cpp or il must be specified.");
            });

            ilCommand.Handler = CommandHandler.Create<string[], string[], string, string>((xaml, reference, i, o) => CompileIL(xaml, reference, i, o));

            // Parse the incoming args and invoke the handler
            await rootCommand.InvokeAsync(args);
        }

        private static void CompileIL(string[] xamlFiles, string[] references, string inputAssembly, string outputAssembly)
        {
            var typeSystem = new CecilTypeSystem(references.Concat(new[] { inputAssembly }), inputAssembly);
            var asm = typeSystem.TargetAssemblyDefinition;

            var (xamlLanguage, emitConfig) = WinUIXamlLanguage.Configure<IXamlILEmitter, XamlILNodeEmitResult>(typeSystem);
            var compilerConfig = new TransformerConfiguration(typeSystem,
                typeSystem.TargetAssembly,
                xamlLanguage,
                GetXmlnsNamespacesIL(typeSystem, xamlLanguage));


            var contextDef = new TypeDefinition("CompiledWinUIXaml", "XamlIlContext",
                TypeAttributes.Class, asm.MainModule.TypeSystem.Object);
            asm.MainModule.Types.Add(contextDef);

            var contextClass = XamlILContextDefinition.GenerateContextClass(typeSystem.CreateTypeBuilder(contextDef), typeSystem,
                xamlLanguage, emitConfig);

            var compiler = new WinUIXamlILCompiler(compilerConfig, emitConfig) { EnableIlVerification = true };
            var typeDef = new TypeDefinition("CompiledWinUIXaml", "CompiledXaml", TypeAttributes.Class, asm.MainModule.TypeSystem.Object);

            asm.MainModule.Types.Add(typeDef);
            var builder = typeSystem.CreateTypeBuilder(typeDef);

            CompileXaml(xamlFiles, typeSystem, compilerConfig, contextClass, compiler, builder);

            Directory.CreateDirectory(Path.GetDirectoryName(outputAssembly));

            asm.Write(outputAssembly, new WriterParameters
            {
                WriteSymbols = asm.MainModule.HasSymbols
            });
        }
        private static void CompileCpp(string[] xamlFiles, string[] references, string outputFolder)
        {
            var typeSystem = new CecilTypeSystem(references, null);
            var asm = typeSystem.TargetAssemblyDefinition;

            var (xamlLanguage, emitConfig) = WinUIXamlLanguage.Configure<CppEmitter, CppNodeEmitResult>(typeSystem);
            var compilerConfig = new TransformerConfiguration(typeSystem,
                typeSystem.TargetAssembly,
                xamlLanguage,
                GetXmlnsNamespacesCpp(typeSystem, xamlLanguage));

            var contextDocument = new CppDocument();

            var contextDef = new TypeDefinition("CompiledWinUIXaml", "XamlIlContext",
                TypeAttributes.Class, asm.MainModule.TypeSystem.Object);
            asm.MainModule.Types.Add(contextDef);

            var contextClass = CppContextDefinition.GenerateContextClass(typeSystem.CreateTypeBuilder(contextDef), typeSystem,
                xamlLanguage, emitConfig);

            var compiler = new WinUIXamlCppCompiler(compilerConfig, emitConfig);
            CompileXaml(xamlFiles, typeSystem, compilerConfig, contextClass, compiler, null /* provide a type builder for C++ */);
        }

        private static XamlXmlnsMappings GetXmlnsNamespacesCpp(CecilTypeSystem typeSystem, XamlLanguageTypeMappings xamlLanguage)
        {
            var mappings = XamlXmlnsMappings.Resolve(typeSystem, xamlLanguage);
            mappings.Namespaces.Add("http://schemas.microsoft.com/winfx/2006/xaml/presentation", new List<(IXamlAssembly asm, string ns)>
            {
                // TODO: default namespace mappings.
            });
            return mappings;
        }

        private static XamlXmlnsMappings GetXmlnsNamespacesIL(CecilTypeSystem typeSystem, XamlLanguageTypeMappings xamlLanguage)
        {
            var mappings = XamlXmlnsMappings.Resolve(typeSystem, xamlLanguage);
            var winUIAssembly = typeSystem.FindAssembly("Microsoft.WinUI");
            mappings.Namespaces.Add("http://schemas.microsoft.com/winfx/2006/xaml/presentation", new List<(IXamlAssembly asm, string ns)>
            {
                (winUIAssembly, "Microsoft.UI"),
                (winUIAssembly, "Microsoft.UI.Xaml"),
                (winUIAssembly, "Microsoft.UI.Xaml.Automation"),
                (winUIAssembly, "Microsoft.UI.Xaml.Automation.Peers"),
                (winUIAssembly, "Microsoft.UI.Xaml.Automation.Provider"),
                (winUIAssembly, "Microsoft.UI.Xaml.Automation.Text"),
                (winUIAssembly, "Microsoft.UI.Xaml.Controls"),
                (winUIAssembly, "Microsoft.UI.Xaml.Controls.Primitives"),
                (winUIAssembly, "Microsoft.UI.Xaml.Data"),
                (winUIAssembly, "Microsoft.UI.Xaml.Documents"),
                (winUIAssembly, "Microsoft.UI.Xaml.Input"),
                (winUIAssembly, "Microsoft.UI.Xaml.Interop"),
                (winUIAssembly, "Microsoft.UI.Xaml.Markup"),
                (winUIAssembly, "Microsoft.UI.Xaml.Media"),
                (winUIAssembly, "Microsoft.UI.Xaml.Media.Animation"),
                (winUIAssembly, "Microsoft.UI.Xaml.Media.Imaging"),
                (winUIAssembly, "Microsoft.UI.Xaml.Media.Media3D"),
                (winUIAssembly, "Microsoft.UI.Xaml.Navigation"),
                (winUIAssembly, "Microsoft.UI.Xaml.Resources"),
                (winUIAssembly, "Microsoft.UI.Xaml.Shapes"),
                (winUIAssembly, "Microsoft.UI.Xaml.Threading"),
                (winUIAssembly, "Microsoft.UI.Text"),
            });
            return mappings;
        }

        private static void CompileXaml<TBackendEmitter, TEmitResult>(string[] xamlFiles, CecilTypeSystem typeSystem, TransformerConfiguration compilerConfig, IXamlType contextClass, XamlImperativeCompiler<TBackendEmitter, TEmitResult> compiler, IXamlTypeBuilder<TBackendEmitter> builder)
            where TEmitResult : IXamlEmitResult
        {
            foreach (var xamlFile in xamlFiles)
            {
                var res = new XamlFileSource(Path.GetFullPath(xamlFile), File.ReadAllBytes(xamlFile));

                // StreamReader is needed here to handle BOM
                var xaml = new StreamReader(new MemoryStream(res.FileContents)).ReadToEnd();
                var parsed = XDocumentXamlParser.Parse(xaml);

                var initialRoot = (XamlAstObjectNode)parsed.Root;

                var classDirective = initialRoot.Children.OfType<XamlAstXmlDirective>()
                    .FirstOrDefault(d => d.Namespace == XamlNamespaces.Xaml2006 && d.Name == "Class");
                IXamlType classType = null;
                if (classDirective != null)
                {
                    if (classDirective.Values.Count != 1 || !(classDirective.Values[0] is XamlAstTextNode tn))
                        throw new XamlParseException("x:Class should have a string value", classDirective);
                    classType = typeSystem.TargetAssembly.FindType(tn.Text);
                    if (classType == null)
                        throw new XamlParseException($"Unable to find type `{tn.Text}`", classDirective);
                    OverrideRootType(parsed,
                        new XamlAstClrTypeReference(classDirective, classType, false));
                    initialRoot.Children.Remove(classDirective);
                }


                compiler.Transform(parsed);
                var populateName = classType == null ? "Populate_" + Path.GetFileNameWithoutExtension(xamlFile) : "_XamlIlPopulate";
                var buildName = classType == null ? "Build_" + Path.GetFileNameWithoutExtension(xamlFile) : null;

                var classTypeDefinition =
                    classType == null ? null : typeSystem.GetTypeReference(classType).Resolve();

                compiler.Compile(parsed, contextClass,
                    compiler.DefinePopulateMethod(builder, parsed, populateName,
                        false),
                    buildName == null ? null : compiler.DefineBuildMethod(builder, parsed, buildName, true),
                    null,
                    (closureName, closureBaseType) =>
                        builder.DefineSubType(closureBaseType, closureName, false),
                    xamlFile, res
                );
            }
        }

        private static void OverrideRootType(XamlDocument doc, IXamlAstTypeReference newType)
        {
            var root = (XamlAstObjectNode)doc.Root;
            var oldType = root.Type;
            if (oldType.Equals(newType))
                return;

            root.Type = newType;
            foreach (var child in root.Children.OfType<XamlAstXamlPropertyValueNode>())
            {
                if (child.Property is XamlAstNamePropertyReference prop)
                {
                    if (prop.DeclaringType.Equals(oldType))
                        prop.DeclaringType = newType;
                    if (prop.TargetType.Equals(oldType))
                        prop.TargetType = newType;
                }
            }
        }
    }
}
