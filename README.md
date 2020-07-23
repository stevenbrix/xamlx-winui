# XamlX - WinUI

This repo contains the source code for the 2020 Hackathon project to bring the [XamlX](https://github.com/kekekeks/XamlX) compiler to WinUI3.

## What is XamlX?

Taken from the XamlX (linked above) readme, XamlX is a:

> General purpose pluggable XAML compiler with no runtime dependencies. Currently being used by Avalonia project as the XAML engine. 
The compiler isn't tied to Avalonia in any way, shape or form and can be used for any purposes by configuring XamlLanguageTypeMappings to match the needs of your particular framework. Further customization can be done by AST manipulations, see examples of those in Avaloni repository.

One of the key differences between XamlX and the way that WPF and WinUI have serialized XAML files, is that they generate Intermediate Language (IL) instructions,
rather than a binary format reprenstation of the markup (BAML/XBF). This enables support for any .NET language, and doesn't require individual work for C#, VB, or F#.

### Debugging Features of XamlX

XamlX has some really cool debugging features, the below images are curtosy of [a tweet from Jumar Macato](https://twitter.com/jumarmacato/status/1182645002180026369) (Twitter: @jumarmacatoto)

#### Xaml Breakpoints!
![](docs\xaml-breakpoint.png)

#### Inline XamlParse Exceptions!
![](docs\xaml-exception.jfif)

## Getting Started

How to debug the current Avalonia Compiler:
- Debug [Unit tests in Avalonia.Markup.Xaml](https://github.com/AvaloniaUI/Avalonia/blob/master/tests/Avalonia.Markup.Xaml.UnitTests/Xaml/XamlIlTests.cs)

## Goals

- Enable compilation of WinUI3 applications using a XamlX based compiler, similar to the [Avalonia Compiler](https://github.com/AvaloniaUI/Avalonia/blob/master/src/Markup/Avalonia.Markup.Xaml.Loader/CompilerExtensions/AvaloniaXamlIlCompiler.cs)
- Add support for emitting C++ backend
- Ensure support for WinUI Specific language features:
   - (P0) `x:Bind`
   - (P0) `x:Load`
   - (P1) `x:Phase`
- Xaml Direct transforms and a special emitter for method calls?
- Add support for new collection syntax being added:

```xml
<Grid ColumnDefinitions="*,*.Auto"/>
```

## Project Plan

Parallelizable work:
- C++/WinRT backend
- Adding support for WinUI
- xaml direct transforms
- standalone exe

### Standalone exe
Make a standalone exe for easy testing and inner loop.

### C++ Backend
- Emitters all the basic logic for the nodes
- Follow pattern for `IL` folder here: https://github.com/kekekeks/XamlX/tree/master/src/XamlX
- Can use Cecil which has support .winmd for local types
    - Property on CecilReader called `ApplyWindowsRuntimeProjections` - set this to `false`.

Emitters are templated on two types: 

- **Result Type** - valid vs invalid. Emitter returns invalid result if it doesn't emit anything. Chain of responsiblity, so if one emitter can't do it, moves on to the next one.
- **Base emitter** - as close to the primitive instructions as needed.

Node Emitter's call methods on base emitter to emit the node. Base emitter is passed to the node.

### Xaml Direct transform
Assigned to: Jeremy

### WinUI Support
- Copying Avalonia compiler
- Adding WinUI specific language features

## License

This project is licensed with the [MIT license](LICENSE).

## Related Projects

You should take a look at these related projects:

- [XamlX](https://github.com/kekekeks/XamlX)
- [Avalonia](https://github.com/AvaloniaUI/Avalonia)