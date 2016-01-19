﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.Tools.Test.Utilities;
using Xunit;

namespace Microsoft.DotNet.Tools.Compiler.Tests
{
    public class CompilerTests : TestBase
    {
        private readonly string _testProjectsRoot;

        public CompilerTests()
        {
            _testProjectsRoot = Path.Combine(AppContext.BaseDirectory, "TestAssets", "TestProjects");
        }

        [Fact]
        public void XmlDocumentationFileIsGenerated()
        {
            // create unique directories in the 'temp' folder
            var root = Temp.CreateDirectory();
            root.CopyFile(Path.Combine(_testProjectsRoot, "global.json"));

            var testLibDir = root.CreateDirectory("TestLibrary");
            var sourceTestLibDir = Path.Combine(_testProjectsRoot, "TestLibrary");

            CopyProjectToTempDir(sourceTestLibDir, testLibDir);

            // run compile
            var outputDir = Path.Combine(testLibDir.Path, "bin");
            var testProject = GetProjectPath(testLibDir);
            var buildCommand = new BuildCommand(testProject, output: outputDir, framework: DefaultFramework);
            var result = buildCommand.ExecuteWithCapturedOutput();
            result.Should().Pass();

            // verify the output xml file
            var outputXml = Path.Combine(outputDir, "Debug", DefaultFramework, "TestLibrary.xml");
            Console.WriteLine("OUTPUT XML PATH: " + outputXml);
            Assert.True(File.Exists(outputXml));
            Assert.Contains("Gets the message from the helper", File.ReadAllText(outputXml));
        }

        [Fact]
        public void SatelliteAssemblyIsGeneratedByDotnetBuild()
        {
            // create unique directories in the 'temp' folder
            var root = Temp.CreateDirectory();
            var testLibDir = root.CreateDirectory("TestProjectWithCultureSpecificResource");
            var sourceTestLibDir = Path.Combine(_testProjectsRoot, "TestProjectWithCultureSpecificResource");

            CopyProjectToTempDir(sourceTestLibDir, testLibDir);

            // run compile on a project with resources
            var outputDir = Path.Combine(testLibDir.Path, "bin");
            var testProject = GetProjectPath(testLibDir);
            var buildCmd = new BuildCommand(testProject, output: outputDir, framework: DefaultFramework);
            var result = buildCmd.ExecuteWithCapturedOutput();
            result.Should().Pass();

            var generatedSatelliteAssemblyPath = Path.Combine(
                outputDir,
                "Debug",
                DefaultFramework,
                "fr",
                "TestProjectWithCultureSpecificResource.resources.dll");
            Assert.True(File.Exists(generatedSatelliteAssemblyPath), $"File {generatedSatelliteAssemblyPath} was not found.");
        }

        [Fact]
        public void LibraryWithAnalyzer()
        {            
            var root = Temp.CreateDirectory();
            var testLibDir = root.CreateDirectory("TestLibraryWithAnalyzer");
            var sourceTestLibDir = Path.Combine(_testProjectsRoot, "TestLibraryWithAnalyzer");

            CopyProjectToTempDir(sourceTestLibDir, testLibDir);
            
            // run compile
            var outputDir = Path.Combine(testLibDir.Path, "bin");
            var testProject = GetProjectPath(testLibDir);
            var buildCmd = new BuildCommand(testProject, output: outputDir, framework: DefaultFramework);
            var result = buildCmd.ExecuteWithCapturedOutput();
            result.Should().Pass();
            
            Assert.Contains("CA1018", result.StdErr);
        }

        [Fact]
        public void ContentFilesAreCopied()
        {
            // create unique directories in the 'temp' folder
            var root = Temp.CreateDirectory();
            var testLibDir = root.CreateDirectory("TestAppWithContentPackage");

            // copy projects to the temp dir and restore them
            CopyProjectToTempDir(Path.Combine(_testProjectsRoot, "TestAppWithContentPackage"), testLibDir);
            RunRestore(testLibDir.Path);

            // run compile
            var outputDir = Path.Combine(testLibDir.Path, "bin");
            var testProject = GetProjectPath(testLibDir);
            var buildCommand = new BuildCommand(testProject, output: outputDir);
            var result = buildCommand.ExecuteWithCapturedOutput();
            result.Should().Pass();

            result = Command.Create(Path.Combine(outputDir, "TestAppWithContentPackage.exe"), new string [0])
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute();
            result.Should().Pass();

            // verify the output xml file
            Assert.True(File.Exists(Path.Combine(outputDir, "scripts\\run.cmd")));
            Assert.True(File.Exists(Path.Combine(outputDir, "config.xml")));
            // verify embedded resources
            Assert.True(result.StdOut.Contains("TestAppWithContentPackage.dnf.png"));
            Assert.True(result.StdOut.Contains("TestAppWithContentPackage.ui.png"));
            // verify 'all' language files not included
            Assert.False(result.StdOut.Contains("TestAppWithContentPackage.dnf_all.png"));
            Assert.False(result.StdOut.Contains("TestAppWithContentPackage.ui_all.png"));
            // verify classes
            Assert.True(result.StdOut.Contains("TestAppWithContentPackage.Foo"));
            Assert.True(result.StdOut.Contains("MyNamespace.Util"));
        }

        private void CopyProjectToTempDir(string projectDir, TempDirectory tempDir)
        {
            // copy all the files to temp dir
            foreach (var file in Directory.EnumerateFiles(projectDir))
            {
                tempDir.CopyFile(file);
            }
        }

        private string GetProjectPath(TempDirectory projectDir)
        {
            return Path.Combine(projectDir.Path, "project.json");
        }
    }
}
