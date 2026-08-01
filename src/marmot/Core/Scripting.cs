using System.Diagnostics;
using Marmot.Backend.Projects;
using Marmot.Backend.Resources;

namespace Marmot;

public static class Scripting {

    public static async Task GenerateSource(Project project) {

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
                            <PublishAot>true</PublishAot>
                            <AssemblyName>{project.Folder}</AssemblyName>
                            <StartupObject>Marmot.Backend.Player.{project.Pascal}Entry</StartupObject>
                            <RootNamespace>{project.Pascal}</RootNamespace>
                            <ImplicitUsings>enable</ImplicitUsings>
                            <TargetFramework>net10.0</TargetFramework>
                            <InvariantGlobalization>true</InvariantGlobalization>
                            <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
                        </PropertyGroup>

                        <ItemGroup>
                            <Reference Include="marmot">
                                <HintPath>{Path.Join(AppContext.BaseDirectory, "lib/marmot.dll")}</HintPath>
                            </Reference>
                            <Compile Include="{project.SrcPath}/**/*.cs" />
                            <Compile Include="{project.SrcGenPath}/{project.Pascal}Entry.cs" Visible="false" />
                        </ItemGroup>
                        
                        <ItemGroup Condition="'$(includeLibs)' == 'true'">
                            <Reference Include="Raylib-cs">
                                <HintPath>/mnt/secondary/Projects/Marmot/build/lib/Raylib-cs.dll</HintPath>
                            </Reference>
                        </ItemGroup>

                        <Import Project="Sdk.targets" Sdk="Microsoft.NET.Sdk" />

                    </Project>
                    """;

        await File.WriteAllTextAsync(project.SrcProjPath, proj);
    }

    private static async Task GenerateEntry(Project project) {

        var entry = $$"""
                      namespace Marmot.Backend.Player;
                      
                      public static class {{project.Pascal}}Entry {
                      
                          public static async Task Main(string[] args)
                              => await Player.Ignite(new {{project.Pascal}}.{{project.Pascal}}());
                      }
                      """;

        await File.WriteAllTextAsync(project.SrcEntryPath, entry);
    }

    private static async Task EnsureTemplate(Project project) {

        var templatePath = Path.Join(project.SrcPath, project.Pascal + ".cs");

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

    internal static async Task Build(Project project, string cmd = "publish", bool release = true, bool includeLibs = true) {

        await GenerateSource(project);

        var releaseArgs = release ? "-r linux-x64 -c release" : "";
        var args = $"{cmd} {releaseArgs} -p:includeLibs={includeLibs}";

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