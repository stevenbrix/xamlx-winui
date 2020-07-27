﻿using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
                new Argument<DirectoryInfo>("-o", "The output directory for the generated C++ code.")
            };

            var ilCommand = new Command("il", "Compile Xaml to IL and embed in a .NET assembly")
            {
                new Argument<FileInfo>("-i", "The input .NET assembly"),
                new Argument<FileInfo>("-o", "The ouput path for the updated .NET assembly.")
            };

            var xamlFilesArgument = new Argument<string[]>(
                "--xaml",
                "The input xaml files.");

            xamlFilesArgument.AddAlias("-x");

            var referencesArgument = new Argument<string[]>(
                "--reference",
                "The reference metadata files (.NET assemblies or WinMD files).");
            referencesArgument.AddAlias("-r");

            // Create a root command with some options
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
                XamlXmlnsMappings.Resolve(typeSystem, xamlLanguage));


            var contextDef = new TypeDefinition("CompiledAvaloniaXaml", "XamlIlContext",
                TypeAttributes.Class, asm.MainModule.TypeSystem.Object);
            asm.MainModule.Types.Add(contextDef);

            var contextClass = XamlILContextDefinition.GenerateContextClass(typeSystem.CreateTypeBuilder(contextDef), typeSystem,
                xamlLanguage, emitConfig);

            var compiler = new WinUIXamlILCompiler(compilerConfig, emitConfig) { EnableIlVerification = true };
            var typeDef = new TypeDefinition("CompiledWinUIXaml", "CompiledXaml", TypeAttributes.Class, asm.MainModule.TypeSystem.Object);

            asm.MainModule.Types.Add(typeDef);
            var builder = typeSystem.CreateTypeBuilder(typeDef);

            CompileXaml(xamlFiles, typeSystem, compilerConfig, contextClass, compiler, builder);

            asm.Write(outputAssembly, new WriterParameters
            {
                WriteSymbols = asm.MainModule.HasSymbols
            });
        }
        private static void CompileCpp(string[] xamlFiles, string[] references, string outputFolder)
        {
            var typeSystem = new CecilTypeSystem(references, null);
            var asm = typeSystem.TargetAssemblyDefinition;

            var (xamlLanguage, emitConfig) = WinUIXamlLanguage.Configure<IXamlILEmitter, XamlILNodeEmitResult>(typeSystem);
            var compilerConfig = new TransformerConfiguration(typeSystem,
                typeSystem.TargetAssembly,
                xamlLanguage,
                GetXmlnsNamespacesCpp(typeSystem, xamlLanguage));


            var contextDef = new TypeDefinition("CompiledAvaloniaXaml", "XamlIlContext",
                TypeAttributes.Class, asm.MainModule.TypeSystem.Object);
            asm.MainModule.Types.Add(contextDef);

            var contextClass = XamlILContextDefinition.GenerateContextClass(typeSystem.CreateTypeBuilder(contextDef), typeSystem,
                xamlLanguage, emitConfig);

            var compiler = new WinUIXamlILCompiler(compilerConfig, emitConfig) { EnableIlVerification = true };
            CompileXaml(xamlFiles, typeSystem, compilerConfig, contextClass, compiler, null /* provider a type builder for C++ */);
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
                (winUIAssembly, "Windows.UI"),
                (winUIAssembly, "Windows.UI.Xaml"),
                (winUIAssembly, "Windows.UI.Xaml.Automation"),
                (winUIAssembly, "Windows.UI.Xaml.Automation.Peers"),
                (winUIAssembly, "Windows.UI.Xaml.Automation.Provider"),
                (winUIAssembly, "Windows.UI.Xaml.Automation.Text"),
                (winUIAssembly, "Windows.UI.Xaml.Controls"),
                (winUIAssembly, "Windows.UI.Xaml.Controls.Primitives"),
                (winUIAssembly, "Windows.UI.Xaml.Data"),
                (winUIAssembly, "Windows.UI.Xaml.Documents"),
                (winUIAssembly, "Windows.UI.Xaml.Input"),
                (winUIAssembly, "Windows.UI.Xaml.Interop"),
                (winUIAssembly, "Windows.UI.Xaml.Markup"),
                (winUIAssembly, "Windows.UI.Xaml.Media"),
                (winUIAssembly, "Windows.UI.Xaml.Media.Animation"),
                (winUIAssembly, "Windows.UI.Xaml.Media.Imaging"),
                (winUIAssembly, "Windows.UI.Xaml.Media.Media3D"),
                (winUIAssembly, "Windows.UI.Xaml.Navigation"),
                (winUIAssembly, "Windows.UI.Xaml.Resources"),
                (winUIAssembly, "Windows.UI.Xaml.Shapes"),
                (winUIAssembly, "Windows.UI.Xaml.Threading"),
                (winUIAssembly, "Windows.UI.Text"),
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
                    builder.DefineSubType(compilerConfig.WellKnownTypes.Object, "NamespaceInfo_" + Path.GetFileNameWithoutExtension(xamlFile),
                        true),
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
