using System.Reflection;

namespace ZippedAssemblyLoader.Tests;

public class FileLoadTests
{
    private static void Reset()
    {
        ZippedAssemblyLoadContext.UnregisterAllArchives();
    }

    /// <summary>
    /// Test that loading a completely empty zip fails at the _zip_ part
    /// </summary>
    [Fact]
    public void LoadDll_WithEmptyFile_ThrowsBadData()
    {
        // Arrange
        Reset();
        
        const string archive = "Zip_Empty.zip";
        Assembly? assembly = null;

        // Act
        var action = () => assembly = ZippedAssemblyLoadContext.Load("FooLib.dll", archive);

        // Assert
        Assert.Throws<InvalidDataException>(action);
        Assert.Null(assembly);
    }

    /// <summary>
    /// Test loading a DLL from different zip archives
    /// </summary>
    /// <param name="zipFilename">Name of the test zip file</param>
    [Theory]
    [InlineData("Zip_SingleDll_Flat.zip")]
    public void LoadDll_WithSafeZip_LoadsDll(string zipFilename)
    {
        // Assume
        Assert.True(File.Exists(zipFilename));

        // Arrange
        Reset();
        
        // Act
        var assembly = ZippedAssemblyLoadContext.Load("ZippedAssemblyLoader.TestAssembly1.dll", zipFilename);
        
        // Assert
        Assert.NotNull(assembly);
        Assert.NotEmpty(assembly.GetExportedTypes());
    }
}