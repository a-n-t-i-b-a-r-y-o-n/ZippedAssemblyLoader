# Zipped Assembly Loader
_Load .NET Assemblies from Zip files_

This library allows loading .NET Assembly objects from compressed Zip files.

Managed .NET Assemblies are loaded into memory directly from the archive, while native libraries are extracted to a
temporary file, loaded into memory, then deleted.

Loading an Assembly from an archive registers the archive file for library resolution with the `AppDomain`, allowing
runtime dependency resolution to use the archive as a source of libraries as the application runs.

## Usage

Assemblies can be loaded either by their filename or `AssemblyName`, similar to using an `AssemblyLoadContext`:
```csharp
// By filename
Assembly? foo = ZipAssemblyLoadContext.Load("FooLib.dll", "/path/to/Bar.zip");

// By AssemblyName
Assembly? bar = ZipAssemblyLoadContext.Load(new AssemblyName("FooLib"), "/path/to/Bar.zip");
```

If all dependencies have been loaded from an archive, or you wish to avoid loading further ones from it, the archive
file can be unregistered as a source of libraries:
```csharp
// Unregister a particular archive
ZipAssemblyLoadContext.UnregisterArchive("/path/to/Foo.zip");

// Unregister all registered archives
ZipAssemblyLoadContext.UnregisterAllArchives();
```