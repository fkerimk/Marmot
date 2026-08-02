using System.Diagnostics;
using Marmot.Backend.Projects;
using static System.IO.Path;

namespace Marmot.Backend.Scripting;

public static class ScriptingManager {

    public static string BuildPlatform { get; private set; } = "win-x64";

    internal static async Task GenerateSource(Project project) {

        Directory.CreateDirectory(project.SrcPath);
        Directory.CreateDirectory(project.SrcGenPath);

        await GenerateCsproj(project);
        await GenerateEntry(project);

        await EnsureTemplate(project);
    }

    private static async Task GenerateCsproj(Project project) {

        var proj = $"""
                    <Project>

                        <PropertyGroup>
                            <OutPath>{project.SrcGenPath}</OutPath>
                            <OutputPath>$(OutPath)/obj/Debug</OutputPath>
                            <PublishDir>$(OutPath)/build</PublishDir>
                            <RestoreOutputPath>$(OutPath)/obj</RestoreOutputPath>
                            <BaseIntermediateOutputPath>$(OutPath)/obj</BaseIntermediateOutputPath>
                            <MSBuildProjectExtensionsPath>$(OutPath)/obj</MSBuildProjectExtensionsPath>
                        </PropertyGroup>

                        <Import Project="Sdk.props" Sdk="Microsoft.NET.Sdk" />

                        <PropertyGroup>
                            <Nullable>enable</Nullable>
                            <OutputType>EXE</OutputType>
                            <AssemblyName>{project.Folder}</AssemblyName>
                            <RootNamespace>{project.Pascal}</RootNamespace>
                            <StartupObject>Marmot.Backend.Player.{project.Pascal}Entry</StartupObject>
                            <ImplicitUsings>enable</ImplicitUsings>
                            <TargetFramework>net10.0</TargetFramework>
                            <InvariantGlobalization>true</InvariantGlobalization>
                            <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
                        </PropertyGroup>
                        
                        <PropertyGroup Condition="'$(Configuration)' == 'Release'">
                            <SelfContained>true</SelfContained>
                            <PublishTrimmed>false</PublishTrimmed> 
                            <PublishSingleFile>true</PublishSingleFile>
                            <EnableCompressionInSingleFile>true</EnableCompressionInSingleFile>
                            <ErrorOnDuplicatePublishOutputFiles>false</ErrorOnDuplicatePublishOutputFiles>
                            <IncludeNativeLibrariesForSelfExtract>true</IncludeNativeLibrariesForSelfExtract>
                        </PropertyGroup>

                        <ItemGroup>
                            <Reference Include="marmot">
                                <HintPath>{PathM.LibPath("marmot.dll")}</HintPath>
                            </Reference>
                            <Compile Include="{project.SrcPath}/**/*.cs" />
                            <Compile Include="{project.SrcGenPath}/{project.Pascal}Entry.cs" Visible="false" />
                        </ItemGroup>
                        
                        <ItemGroup Condition="'$(includeLibs)' == 'true'">
                            <PackageReference Include="Raylib-cs" Version="8.0.0" />
                        </ItemGroup>
                        
                        <Target Name="CustomRaylibBuild" AfterTargets="Build">
                            <Copy SourceFiles="{PathM.LibPath("libraylib.so")}"
                                  DestinationFiles="$(OutDir)runtimes/linux-x64/native/libraylib.so"
                                  SkipUnchangedFiles="false"
                                  Condition="Exists('$(OutDir)runtimes/linux-x64/native/libraylib.so')" />
                            <Copy SourceFiles="{PathM.LibPath("libraylib.dll")}"
                                  DestinationFiles="$(OutDir)runtimes/win-x64/native/raylib.dll"
                                  SkipUnchangedFiles="false"
                                  Condition="Exists('$(OutDir)runtimes/win-x64/native/raylib.dll')" />
                        </Target>
                            
                        <Target Name="FixRaylibBundleConflict" BeforeTargets="GenerateSingleFileBundle">
                            <ItemGroup>
                                <FilesToBundle Remove="@(FilesToBundle)" Condition="'%(FileName)' == 'raylib' or '%(FileName)' == 'libraylib'" />
                                <FilesToBundle Include="{PathM.LibPath("libraylib.so")}" Condition="'$(RuntimeIdentifier)' == 'linux-x64'">
                                    <RelativePath>libraylib.so</RelativePath>
                                </FilesToBundle>
                                <FilesToBundle Include="{PathM.LibPath("libraylib.dll")}" Condition="'$(RuntimeIdentifier)' == 'win-x64'">
                                    <RelativePath>raylib.dll</RelativePath>
                                </FilesToBundle>
                            </ItemGroup>
                        </Target>
                        
                        <Import Project="Sdk.targets" Sdk="Microsoft.NET.Sdk" />

                    </Project>
                    """;

        await File.WriteAllTextAsync(project.SrcProjPath, proj);
    }

    private static async Task GenerateEntry(Project project) {

        var entry = $$"""
                      namespace Marmot.Backend.Player;
                      
                      public static class {{project.Pascal}}Entry {
                      
                          #if DEBUG
                          private const bool DebugMode = true;
                          #else
                          private const bool DebugMode = false;
                          #endif
                      
                          public static async Task Main(string[] args)
                              => await Player.Ignite(new {{project.Pascal}}.{{project.Pascal}}(), DebugMode);
                      }
                      """;

        await File.WriteAllTextAsync(project.SrcEntryPath, entry);
    }

    private static async Task EnsureTemplate(Project project) {

        var templatePath = Join(project.SrcPath, project.Pascal + ".cs");

        if (File.Exists(templatePath)) return;

        var template = $$"""
                         using Marmot;
                         
                         namespace {{project.Pascal}};
                         
                         public class {{project.Pascal}} : Game {
                         
                             public override void Init() {
                         
                                 Console.WriteLine("Hello Marmot!");
                             }
                         
                             public override void Loop() {
                         
                         
                             }
                         
                             public override void Exit() {
                         
                         
                             }
                         }
                         """;

        await File.WriteAllTextAsync(templatePath, template);
    }

    internal static async Task Build(Project project, string cmd = "publish", bool release = true) {

        await GenerateSource(project);

        var platformArgs = $"-r {BuildPlatform}";
        var releaseArgs = release ? $"{platformArgs} -c release" : "-c debug";
        var args = $"{cmd} {releaseArgs} -p:includeLibs=true";

        var startInfo = new ProcessStartInfo {

            FileName = "dotnet",
            Arguments = args,
            WorkingDirectory = project.SrcPath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);

        if (process != null) {

            process.OutputDataReceived += (_, e) => { Console.WriteLine(e.Data); };
            process.ErrorDataReceived += (_, e) => { Console.Error.WriteLine(e.Data); };

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync();
            var exitCode = process.ExitCode;
        }
    }
}