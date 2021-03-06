﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using Packager.Exceptions;
using Packager.Extensions;
using Packager.Factories;
using Packager.Models.EmbeddedMetadataModels;
using Packager.Models.FileModels;
using Packager.Models.ResultModels;
using Packager.Models.SettingsModels;
using Packager.Observers;
using Packager.Providers;
using Packager.Test.Mocks;
using Packager.Utilities.Hashing;
using Packager.Utilities.ProcessRunners;
using Packager.Validators.Attributes;

namespace Packager.Test.Utilities
{
    [TestFixture]
    public class FFMPEGRunnerTests
    {
        [SetUp]
        public virtual async Task BeforeEach()
        {
            ProgramSettings = Substitute.For<IProgramSettings>();
            ProgramSettings.FFMPEGPath.Returns(FFMPEGPath);
            ProgramSettings.ProcessingDirectory.Returns(BaseProcessingDirectory);
            ProgramSettings.FFMPEGAudioAccessArguments.Returns(AudioAccessArguments);
            ProgramSettings.FFMPEGAudioProductionArguments.Returns(AudioProductionArguments);

            OutputBuffer = Substitute.For<IOutputBuffer>();
            OutputBuffer.GetContent().Returns(StandardErrorOutput);

            ProcessRunnerResult = Substitute.For<IProcessResult>();
            ProcessRunnerResult.ExitCode.Returns(0);
            ProcessRunnerResult.StandardError.Returns(OutputBuffer);

            MasterFileModel = FileModelFactory.GetModel(MasterFileName);

            ProcessRunner = Substitute.For<IProcessRunner>();

            Observers = Substitute.For<IObserverCollection>();
            FileProvider = Substitute.For<IFileProvider>();

            Hasher = Substitute.For<IHasher>();
            Hasher.Hash(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(x => Task.FromResult($"{Path.GetFileName(x.Arg<string>())} hash value"));

            Metadata = MockBextMetadata.Get();

            
            Runner = new FFMPEGRunner(ProgramSettings, FileProvider, Hasher, Observers, ProcessRunner);

            DoCustomSetup();
        }
        
        private const string FFMPEGPath = "ffmpeg.exe";
        private const string AudioProductionArguments = "production arguments";
        private const string AudioAccessArguments = "production arguments";
        private const string BaseProcessingDirectory = "base";
        private const string MasterFileName = "MDPI_123456789_01_pres.wav";

        private const string StandardErrorOutput = "Standard error output";
        private AbstractFile MasterFileModel { get; set; }

        private IOutputBuffer OutputBuffer { get; set; }
        private IProcessRunner ProcessRunner { get; set; }
        private IObserverCollection Observers { get; set; }
        private IFileProvider FileProvider { get; set; }
        private IFFMPEGRunner Runner { get; set; }
        private AbstractFile Result { get; set; }
        private IProcessResult ProcessRunnerResult { get; set; }
        private IProgramSettings ProgramSettings { get; set; }
        private EmbeddedAudioMetadata Metadata { get; set; }

        private IHasher Hasher { get; set; }


        protected virtual void DoCustomSetup()
        {
        }
        
        public class WhenNormalizingOriginals : FFMPEGRunnerTests
        {
            public override async Task BeforeEach()
            {
                await base.BeforeEach();

                ProductionFileModel = FileModelFactory.GetModel(ProductionFileName);

                Originals = new List<AbstractFile>
                {
                    ProductionFileModel,
                    PreservationFileModel
                };

                // configure file provider to assert original should exist
                FileProvider.FileExists(Path.Combine(BaseProcessingDirectory,
                    ProductionFileModel.GetOriginalFolderName(), ProductionFileName)).Returns(true);
                FileProvider.FileExists(Path.Combine(BaseProcessingDirectory,
                    PreservationFileModel.GetOriginalFolderName(), MasterFileName)).Returns(true);

                // configure file provider to assert normalized versions do not exist
                FileProvider.FileDoesNotExist(Path.Combine(BaseProcessingDirectory, ProductionFileModel.GetFolderName(),
                    ProductionFileName)).Returns(true);
                FileProvider.FileDoesNotExist(Path.Combine(BaseProcessingDirectory,
                    PreservationFileModel.GetFolderName(), MasterFileName)).Returns(true);
            }

            private List<AbstractFile> Originals { get; set; }

            private const string ProductionFileName = "MDPI_123456789_01_prod.wav";

            private AbstractFile ProductionFileModel { get; set; }
            private AbstractFile PreservationFileModel => MasterFileModel;


            public class WhenThingsGoWell : WhenNormalizingOriginals
            {
                public override async Task BeforeEach()
                {
                    await base.BeforeEach();

                    ProcessRunner.Run(Arg.Any<ProcessStartInfo>(), Arg.Any<CancellationToken>()).ReturnsForAnyArgs(x =>
                    {
                        ProcessRunnerResult.StartInfo.Returns(x.Arg<ProcessStartInfo>());
                        return Task.FromResult(ProcessRunnerResult);
                    });

                    var arguments = new ArgumentBuilder("base arguments").AddArguments(Metadata.AsArguments());
                    await Runner.Normalize(PreservationFileModel, arguments, CancellationToken.None);
                }

                private ProcessStartInfo StartInfo { get; set; }

                protected override void DoCustomSetup()
                {
                    base.DoCustomSetup();
                    ProcessRunner.Run(Arg.Do<ProcessStartInfo>(arg => StartInfo = arg), CancellationToken.None);
                }
                
                [Test]
                public void ArgsShouldEndWithCorrectOutputFile()
                {
                    var normalizedPath =
                        Path.Combine(BaseProcessingDirectory, PreservationFileModel.GetFolderName(),
                            PreservationFileModel.Filename).ToQuoted();
                    Assert.That(StartInfo.Arguments.EndsWith(normalizedPath));
                }
                
                [Test]
                public void ArgsShouldIncludeBaseArguments()
                {
                    Assert.That(StartInfo.Arguments.Contains("base arguments"));
                }

                [Test]
                public void ArgsShouldIncludeMetadataCommands()
                {
                    foreach (var argument in Metadata.AsArguments())
                    {
                        Assert.That(StartInfo.Arguments.Contains(argument), $"arguments should contain {argument}");
                    }
                }

                [Test]
                public void ArgsShouldStartwithCorrectInputFile()
                {
                    var originalPath =
                        Path.Combine(BaseProcessingDirectory, PreservationFileModel.GetOriginalFolderName(),
                            PreservationFileModel.Filename).ToQuoted();
                    Assert.That(StartInfo.Arguments.StartsWith($"-i {originalPath}"));
                }

                [Test]
                public void ItShouldCallProcessRunnerWithCorrectUtilityPath()
                {
                    Assert.That(StartInfo.FileName, Is.EqualTo(FFMPEGPath));
                }

                [Test]
                public void ItShouldEndSection()
                {
                    Observers.Received()
                        .EndSection(Arg.Any<string>(), $"{PreservationFileModel.Filename} normalized successfully");
                }

                [Test]
                public void ItShouldOpenSection()
                {
                    Observers.Received().BeginSection("Normalizing {0}", PreservationFileModel.Filename);
                }

                [Test]
                public void ItShouldSetCreateNoWindowToTrue()
                {
                    Assert.That(StartInfo.CreateNoWindow, Is.True);
                }

                [Test]
                public void ItShouldSetRedirectsCorrectly()
                {
                    Assert.That(StartInfo.RedirectStandardError, Is.True);
                    Assert.That(StartInfo.RedirectStandardOutput, Is.True);
                }

                [Test]
                public void ItShouldSetUseShellExecuteToFalse()
                {
                    Assert.That(StartInfo.UseShellExecute, Is.False);
                }

                [Test]
                public void ItShouldVerifyThatAllNormalizedVersionDoesNotExist()
                {
                    var path = Path.Combine(BaseProcessingDirectory, PreservationFileModel.GetFolderName(),
                        PreservationFileModel.Filename);
                    FileProvider.Received().FileExists(path);
                }

                [Test]
                public void ItShouldVerifyThatOriginalExists()
                {
                    var path = Path.Combine(BaseProcessingDirectory, PreservationFileModel.GetOriginalFolderName(),
                        PreservationFileModel.Filename);
                    FileProvider.Received().FileDoesNotExist(path);
                }
            }

            public class WhenThingsGoWrong : WhenNormalizingOriginals
            {
                public override async Task BeforeEach()
                {
                    await base.BeforeEach();

                    Originals = new List<AbstractFile> {PreservationFileModel};

                    // make file provider report that original does not exist
                    FileProvider.FileDoesNotExist(Path.Combine(BaseProcessingDirectory,
                        PreservationFileModel.GetOriginalFolderName(), MasterFileName)).Returns(true);

                    ProcessRunner.Run(Arg.Any<ProcessStartInfo>(), Arg.Any<CancellationToken>()).ReturnsForAnyArgs(x =>
                    {
                        ProcessRunnerResult.StartInfo.Returns(x.Arg<ProcessStartInfo>());
                        return Task.FromResult(ProcessRunnerResult);
                    });

                    var issue =
                        Assert.ThrowsAsync<LoggedException>(async () => await Runner.Normalize(PreservationFileModel, null, CancellationToken.None));
                    Assert.That(issue, Is.Not.Null);
                    Assert.That(issue.InnerException, Is.TypeOf<FileNotFoundException>());
                }

                [Test]
                public void ItShouldCloseSectionCorrectly()
                {
                    Observers.Received().EndSection(Arg.Any<string>());
                }

                [Test]
                public void ItShouldLogIssue()
                {
                    Observers.Received()
                        .LogProcessingIssue(Arg.Any<FileNotFoundException>(), PreservationFileModel.BarCode);
                }
            }
        }

        public class WhenVerifyingNormalizedVersions : FFMPEGRunnerTests
        {
            public override async Task BeforeEach()
            {
                await base.BeforeEach();

                ProductionFileModel = FileModelFactory.GetModel(ProductionFileName);

                Originals = new List<AbstractFile>
                {
                    ProductionFileModel,
                    PreservationFileModel
                };

                // configure file provider to assert original should exist
                FileProvider.FileDoesNotExist(Path.Combine(BaseProcessingDirectory,
                    ProductionFileModel.GetOriginalFolderName(), ProductionFileName)).Returns(false);
                FileProvider.FileDoesNotExist(Path.Combine(BaseProcessingDirectory,
                    PreservationFileModel.GetOriginalFolderName(), MasterFileName)).Returns(false);

                // configure file provider to assert normalized versions exist
                FileProvider.FileDoesNotExist(Path.Combine(BaseProcessingDirectory, ProductionFileModel.GetFolderName(),
                    ProductionFileName)).Returns(false);
                FileProvider.FileDoesNotExist(Path.Combine(BaseProcessingDirectory,
                    PreservationFileModel.GetFolderName(), MasterFileName)).Returns(false);

                // configure file provider to assert original framemd5 files exists
                FileProvider.FileDoesNotExist(Path.Combine(BaseProcessingDirectory,
                    ProductionFileModel.GetOriginalFolderName(), ProductionFileModel.ToFrameMd5Filename()))
                    .Returns(false);
                FileProvider.FileDoesNotExist(Path.Combine(BaseProcessingDirectory,
                    PreservationFileModel.GetOriginalFolderName(), PreservationFileModel.ToFrameMd5Filename()))
                    .Returns(false);

                // configure file provider to assert normalized framemd5 files exists
                FileProvider.FileDoesNotExist(Path.Combine(BaseProcessingDirectory, ProductionFileModel.GetFolderName(),
                    ProductionFileModel.ToFrameMd5Filename())).Returns(false);
                FileProvider.FileDoesNotExist(Path.Combine(BaseProcessingDirectory,
                    PreservationFileModel.GetFolderName(), PreservationFileModel.ToFrameMd5Filename())).Returns(false);
            }

            public List<AbstractFile> Originals { get; set; }

            private const string ProductionFileName = "MDPI_123456789_01_prod.wav";

            private AbstractFile ProductionFileModel { get; set; }
            private AbstractFile PreservationFileModel => MasterFileModel;

            public class WhenThingsGoWell : WhenVerifyingNormalizedVersions
            {
                public override async Task BeforeEach()
                {
                    base.BeforeEach();

                    ProcessRunner.Run(Arg.Any<ProcessStartInfo>(), Arg.Any<CancellationToken>()).ReturnsForAnyArgs(x =>
                    {
                        ProcessRunnerResult.StartInfo.Returns(x.Arg<ProcessStartInfo>());
                        return Task.FromResult(ProcessRunnerResult);
                    });

                    await Runner.Verify(Originals, CancellationToken.None);
                }

                [Test]
                public void ItShouldCallFileProviderToTestThatFilesExist()
                {
                    foreach (var model in Originals)
                    {
                        FileProvider.Received()
                            .FileDoesNotExist(Path.Combine(BaseProcessingDirectory, model.GetOriginalFolderName(),
                                model.Filename));
                        FileProvider.Received()
                            .FileDoesNotExist(Path.Combine(BaseProcessingDirectory, model.GetFolderName(),
                                model.Filename));
                    }
                }

                [Test]
                public void ItShouldCallProcessRunnerCorrectlyForNormalizedVersions()
                {
                    foreach (var model in Originals)
                    {
                        var frameMd5Path = Path.Combine(BaseProcessingDirectory, model.GetFolderName(),
                            model.ToFrameMd5Filename());
                        var targetPath = Path.Combine(BaseProcessingDirectory, model.GetFolderName(), model.Filename);

                        var expectedArgs = $"-y -i {targetPath} -map 0 -f framemd5 {frameMd5Path}";
                        ProcessRunner.Received().Run(Arg.Is<ProcessStartInfo>(i => i.Arguments.Equals(expectedArgs)), Arg.Any<CancellationToken>());
                    }
                }

                [Test]
                public void ItShouldCallProcessRunnerCorrectlyForOriginalVersions()
                {
                    foreach (var model in Originals)
                    {
                        var frameMd5Path = Path.Combine(BaseProcessingDirectory, model.GetOriginalFolderName(),
                            model.ToFrameMd5Filename());
                        var targetPath = Path.Combine(BaseProcessingDirectory, model.GetOriginalFolderName(),
                            model.Filename);

                        var expectedArgs = $"-y -i {targetPath} -map 0 -f framemd5 {frameMd5Path}";
                        ProcessRunner.Received().Run(Arg.Is<ProcessStartInfo>(i => i.Arguments.Equals(expectedArgs)), Arg.Any<CancellationToken>());
                    }
                }

                [Test]
                public void ItShouldCloseSectionsCorrectly()
                {
                    foreach (var model in Originals)
                    {
                        Observers.Received()
                            .EndSection(Arg.Any<string>(), $"{model.Filename} (original) hashed successfully");
                        Observers.Received()
                            .EndSection(Arg.Any<string>(), $"{model.Filename} (normalized) hashed successfully");
                    }
                }

                [Test]
                public void ItShouldCloseValidatingSection()
                {
                    foreach (var model in Originals)
                    {
                        Observers.Received()
                            .EndSection(Arg.Any<string>(), $"{model.Filename} (normalized) validated successfully");
                    }
                }

                [Test]
                public void ItShouldLogNormalizedFrameMd5Hash()
                {
                    foreach (var model in Originals)
                    {
                        Observers.Received()
                            .Log("normalized framemd5 hash: {0}", $"{model.ToFrameMd5Filename()} hash value");
                    }
                }

                [Test]
                public void ItShouldLogOriginalFrameMd5Hash()
                {
                    foreach (var model in Originals)
                    {
                        Observers.Received()
                            .Log("original framemd5 hash: {0}", $"{model.ToFrameMd5Filename()} hash value");
                    }
                }

                [Test]
                public void ItShouldOpenSectionsCorrectly()
                {
                    foreach (var model in Originals)
                    {
                        Observers.Received().BeginSection("Hashing {0}", $"{model.Filename} (original)");
                        Observers.Received().BeginSection("Hashing {0}", $"{model.Filename} (normalized)");
                    }
                }

                [Test]
                public void ItShouldOpenValidatingSection()
                {
                    foreach (var model in Originals)
                    {
                        Observers.Received().BeginSection("Validating {0} (normalized)", model.Filename);
                    }
                }

                [Test]
                public void ItShouldVerifyFrameMd5HashesExists()
                {
                    foreach (var model in Originals)
                    {
                        var originalFrameMd5Path = Path.Combine(BaseProcessingDirectory, model.GetOriginalFolderName(),
                            model.ToFrameMd5Filename());
                        var normalizedFrameMd5Path = Path.Combine(BaseProcessingDirectory, model.GetFolderName(),
                            model.ToFrameMd5Filename());
                        FileProvider.Received().FileDoesNotExist(originalFrameMd5Path);
                        FileProvider.Received().FileDoesNotExist(normalizedFrameMd5Path);
                    }
                }

                [Test]
                public void ItShouldVerifyFrameMd5HashesForNormalizedVersions()
                {
                    foreach (var model in Originals)
                    {
                        var normalizedFrameMd5Path = Path.Combine(BaseProcessingDirectory, model.GetFolderName(),
                            model.ToFrameMd5Filename());
                        Hasher.Received().Hash(normalizedFrameMd5Path, Arg.Any<CancellationToken>());
                    }
                }

                [Test]
                public void ItShouldVerifyFrameMd5HashesForOriginalVersions()
                {
                    foreach (var model in Originals)
                    {
                        var originalFrameMd5Path = Path.Combine(BaseProcessingDirectory, model.GetOriginalFolderName(),
                            model.ToFrameMd5Filename());
                        Hasher.Received().Hash(originalFrameMd5Path, Arg.Any<CancellationToken>());
                    }
                }
            }

            public class WhenHashesAreNotEqual : WhenVerifyingNormalizedVersions
            {
                public override async Task BeforeEach()
                {
                    await base.BeforeEach();

                    Originals = new List<AbstractFile> {PreservationFileModel};

                    // make hasher return different values
                    Hasher.Hash(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(x => Task.FromResult(x.Arg<string>()));

                    ProcessRunner.Run(Arg.Any<ProcessStartInfo>(), Arg.Any<CancellationToken>()).ReturnsForAnyArgs(x =>
                    {
                        ProcessRunnerResult.StartInfo.Returns(x.Arg<ProcessStartInfo>());
                        return Task.FromResult(ProcessRunnerResult);
                    });

                    var issue = Assert.ThrowsAsync<LoggedException>(async () => await Runner.Verify(Originals, CancellationToken.None));
                    Assert.That(issue, Is.Not.Null);
                    Assert.That(issue.InnerException, Is.TypeOf<NormalizeOriginalException>());
                }

                [Test]
                public void ItShouldCloseSectionCorrectly()
                {
                    Observers.Received().EndSection(Arg.Any<string>());
                }

                [Test]
                public void ItShouldLogExceptionCorrectly()
                {
                    Observers.Received()
                        .LogProcessingIssue(Arg.Any<NormalizeOriginalException>(), PreservationFileModel.BarCode);
                }
            }

            public class WhenThingsGoWrong : WhenVerifyingNormalizedVersions
            {
                public override async Task BeforeEach()
                {
                    await base.BeforeEach();

                    Originals = new List<AbstractFile> {PreservationFileModel};

                    // make file provider report that file does not exist
                    FileProvider.FileDoesNotExist(Path.Combine(BaseProcessingDirectory,
                        PreservationFileModel.GetOriginalFolderName(), MasterFileName)).Returns(true);

                    ProcessRunner.Run(Arg.Any<ProcessStartInfo>(), Arg.Any<CancellationToken>()).ReturnsForAnyArgs(x =>
                    {
                        ProcessRunnerResult.StartInfo.Returns(x.Arg<ProcessStartInfo>());
                        return Task.FromResult(ProcessRunnerResult);
                    });

                    var issue = Assert.ThrowsAsync<LoggedException>(async () => await Runner.Verify(Originals, CancellationToken.None));
                    Assert.That(issue, Is.Not.Null);
                    Assert.That(issue.InnerException, Is.TypeOf<FileNotFoundException>());
                }

                [Test]
                public void ItShouldCloseSectionCorrectly()
                {
                    Observers.Received().EndSection(Arg.Any<string>());
                }

                [Test]
                public void ItShouldLogIssueCorrectly()
                {
                    Observers.Received()
                        .LogProcessingIssue(Arg.Any<FileNotFoundException>(), PreservationFileModel.BarCode);
                }
            }
        }
        
        public class WhenGeneratingDerivatives : FFMPEGRunnerTests
        {
            private AbstractFile DerivativeFileModel { get; set; }

            public class WhenGeneratingAccessDerivatives : WhenGeneratingDerivatives
            {
                public override async Task BeforeEach()
                {
                    await base.BeforeEach();
                    DerivativeFileModel = new AccessFile(new ProductionFile( MasterFileModel));

                    ProcessRunner.Run(Arg.Any<ProcessStartInfo>(), Arg.Any<CancellationToken>()).ReturnsForAnyArgs(x =>
                    {
                        ProcessRunnerResult.StartInfo.Returns(x.Arg<ProcessStartInfo>());
                        return Task.FromResult(ProcessRunnerResult);
                    });

                    Result = await Runner.CreateAccessDerivative(new ProductionFile(MasterFileModel), 
                        new ArgumentBuilder(AudioAccessArguments), Enumerable.Empty<string>(), CancellationToken.None);
                }

                private const string AccessDerivativeFileName = "MDPI_123456789_01_access.mp4";
                private const string ProductionFileName = "MDPI_123456789_01_prod.wav";
                private ProcessStartInfo StartInfo { get; set; }

                protected override void DoCustomSetup()
                {
                    base.DoCustomSetup();
                    FileProvider.FileExists(null).ReturnsForAnyArgs(false);
                    ProcessRunner.Run(Arg.Do<ProcessStartInfo>(arg => StartInfo = arg), Arg.Any<CancellationToken>());
                }

                [Test]
                public void ArgumentsShouldContainGlobalArguments()
                {
                    Assert.That(StartInfo.Arguments.Contains(AudioAccessArguments));
                }

                [Test]
                public void ArgumentsShouldEndWithOutputFile()
                {
                    var expected = Path.Combine(BaseProcessingDirectory,
                        MasterFileModel.GetFolderName(), AccessDerivativeFileName).ToQuoted();
                    Assert.That(StartInfo.Arguments.EndsWith(expected));
                }

                [Test]
                public void ArgumentsShouldStartWithInputFile()
                {
                    var expected = Path.Combine(BaseProcessingDirectory, MasterFileModel.GetFolderName(), ProductionFileName)
                            .ToQuoted();
                    Assert.That(StartInfo.Arguments.StartsWith($"-i {expected}"));
                }

                [Test]
                public void ItShouldCallProcessRunnerWithCorrectUtilityPath()
                {
                    Assert.That(StartInfo.FileName, Is.EqualTo(FFMPEGPath));
                }

                [Test]
                public void ItShouldLogProcessResultOutput()
                {
                    Observers.Received().Log(StandardErrorOutput);
                }

                [Test]
                public void ItShouldSetCreateNoWindowToTrue()
                {
                    Assert.That(StartInfo.CreateNoWindow, Is.True);
                }

                [Test]
                public void ItShouldSetRedirectsCorrectly()
                {
                    Assert.That(StartInfo.RedirectStandardError, Is.True);
                    Assert.That(StartInfo.RedirectStandardOutput, Is.True);
                }

                [Test]
                public void ItShouldSetUseShellExecuteToFalse()
                {
                    Assert.That(StartInfo.UseShellExecute, Is.False);
                }
            }

            public class WhenGeneratingProductionDerivatives : WhenGeneratingDerivatives
            {
                public override async Task BeforeEach()
                {
                    await base.BeforeEach();
                    DerivativeFileModel = new ProductionFile(MasterFileModel);
                }

                private const string ProdDerivativeFileName = "MDPI_123456789_01_prod.wav";

                public class WhenThingsGoWell : WhenGeneratingProductionDerivatives
                {
                    public override async Task BeforeEach()
                    {
                        await base.BeforeEach();

                        ProcessRunner.Run(Arg.Any<ProcessStartInfo>(), Arg.Any<CancellationToken>()).ReturnsForAnyArgs(x =>
                        {
                            ProcessRunnerResult.StartInfo.Returns(x.Arg<ProcessStartInfo>());
                            return Task.FromResult(ProcessRunnerResult);
                        });

                        var arguments = new ArgumentBuilder(AudioProductionArguments).AddArguments(Metadata.AsArguments());
                        Result = await Runner.CreateProdOrMezzDerivative(MasterFileModel, DerivativeFileModel, arguments, Enumerable.Empty<string>(), CancellationToken.None);
                    }

                    public class WhenDerivativeAlreadyExists : WhenThingsGoWell
                    {
                        protected override void DoCustomSetup()
                        {
                            base.DoCustomSetup();
                            FileProvider.FileExists(null).ReturnsForAnyArgs(true);
                        }

                        [Test]
                        public void ItShouldCloseSectionCorrectly()
                        {
                            Observers.Received()
                                .EndSection(Arg.Any<string>(),
                                    $"Generate {DerivativeFileModel.FileUsage.FullFileUse} skipped - already exists: {DerivativeFileModel.Filename}");
                        }

                        [Test]
                        public void ItShouldLogThatFileAlreadyExists()
                        {
                            Observers.Received()
                                .Log("{0} already exists. Will not generate derivative", DerivativeFileModel.FileUsage.FullFileUse);
                        }

                        [Test]
                        public void ItShouldNotCallProcessRunner()
                        {
                            ProcessRunner.DidNotReceive().Run(Arg.Any<ProcessStartInfo>(), Arg.Any<CancellationToken>());
                        }
                    }

                    public class WhenDerivativeDoesNotExist : WhenThingsGoWell
                    {
                        private ProcessStartInfo StartInfo { get; set; }

                        protected override void DoCustomSetup()
                        {
                            base.DoCustomSetup();
                            FileProvider.FileExists(null).ReturnsForAnyArgs(false);
                            ProcessRunner.Run(Arg.Do<ProcessStartInfo>(arg => StartInfo = arg), Arg.Any<CancellationToken>());
                        }

                        [Test]
                        public void ArgumentsShouldContainGlobalArguments()
                        {
                            Assert.That(StartInfo.Arguments.Contains(AudioProductionArguments));
                        }

                        [Test]
                        public void ArgumentsShouldContainMetadata()
                        {
                            Assert.That(StartInfo.Arguments.Contains(Metadata.AsArguments().ToString()));
                        }

                        [Test]
                        public void ArgumentsShouldEndWithOutputFile()
                        {
                            var expected =
                                Path.Combine(BaseProcessingDirectory, MasterFileModel.GetFolderName(),
                                    ProdDerivativeFileName)
                                    .ToQuoted();
                            Assert.That(StartInfo.Arguments.EndsWith(expected));
                        }

                        [Test]
                        public void ArgumentsShouldStartWithInputFile()
                        {
                            var expected = Path.Combine(BaseProcessingDirectory, MasterFileModel.GetFolderName(), MasterFileName)
                                    .ToQuoted();
                            Assert.That(StartInfo.Arguments.StartsWith($"-i {expected}"));
                        }

                        [Test]
                        public void ItShouldCallEndSectionCorrectly()
                        {
                            Observers.Received()
                                .EndSection(Arg.Any<string>(),
                                    $"{DerivativeFileModel.FileUsage.FullFileUse} generated successfully: {DerivativeFileModel.Filename}");
                        }

                        [Test]
                        public void ItShouldCallProcessRunnerWithCorrectUtilityPath()
                        {
                            Assert.That(StartInfo.FileName, Is.EqualTo(FFMPEGPath));
                        }

                        [Test]
                        public void ItShouldIncludeMetadataArguments()
                        {
                            foreach (var argument in Metadata.AsArguments())
                            {
                                Assert.That(StartInfo.Arguments.Contains(argument),
                                    $"argument {argument} should be present");
                            }
                        }

                        [Test]
                        public void ItShouldLogProcessResultOutput()
                        {
                            Observers.Received().Log(StandardErrorOutput);
                        }

                        [Test]
                        public void ItShouldSetCreateNoWindowToTrue()
                        {
                            Assert.That(StartInfo.CreateNoWindow, Is.True);
                        }

                        [Test]
                        public void ItShouldSetRedirectsCorrectly()
                        {
                            Assert.That(StartInfo.RedirectStandardError, Is.True);
                            Assert.That(StartInfo.RedirectStandardOutput, Is.True);
                        }

                        [Test]
                        public void ItShouldSetUseShellExecuteToFalse()
                        {
                            Assert.That(StartInfo.UseShellExecute, Is.False);
                        }

                        [Test]
                        public void ItShouldTestThatOriginalExists()
                        {
                            var path = Path.Combine(BaseProcessingDirectory, MasterFileModel.GetFolderName(),
                                MasterFileName);
                            FileProvider.Received().FileDoesNotExist(path);
                        }

                        [Test]
                        public void ItShouldTestThatTargetDoesNotExists()
                        {
                            var path = Path.Combine(BaseProcessingDirectory, MasterFileModel.GetFolderName(),
                                new ProductionFile(MasterFileModel).Filename);

                            FileProvider.Received().FileExists(path);
                        }
                    }

                    [Test]
                    public void ItShouldCallBeginSectionCorrectly()
                    {
                        Observers.Received()
                            .BeginSection("Generating {0}: {1}", DerivativeFileModel.FileUsage.FullFileUse,
                                DerivativeFileModel.Filename);
                    }


                    [Test]
                    public void ItShouldReturnCorrectResult()
                    {
                        Assert.That(DerivativeFileModel.IsSameAs(Result));
                    }
                }
            }

            public class WhenThingsGoWrong : WhenGeneratingDerivatives
            {
                public override async Task BeforeEach()
                {
                    await base.BeforeEach();
                    DerivativeFileModel = new ProductionFile(MasterFileModel);
                }

                private LoggedException FinalException { get; set; }

                public class WhenProcessRunnerReturnsFailResult : WhenThingsGoWrong
                {
                    public override async Task BeforeEach()
                    {
                        await base.BeforeEach();
                        ProcessRunnerResult.ExitCode.Returns(-1);
                        ProcessRunner.Run(Arg.Any<ProcessStartInfo>(), Arg.Any<CancellationToken>()).ReturnsForAnyArgs(x => 
                            {
                                ProcessRunnerResult.StartInfo.Returns(x.Arg<ProcessStartInfo>());
                                return Task.FromResult(ProcessRunnerResult);
                            });

                        FinalException =
                            Assert.ThrowsAsync<LoggedException>(
                                async () =>
                                    await
                                        Runner.CreateProdOrMezzDerivative(MasterFileModel, DerivativeFileModel, new ArgumentBuilder(), Enumerable.Empty<string>(),CancellationToken.None));
                    }

                    [Test]
                    public void FinalExceptionShouldBeCorrect()
                    {
                        var innerException = FinalException.InnerException as GenerateDerivativeException;
                        Assert.That(innerException, Is.Not.Null);
                        Assert.That(innerException.Message, Is.EqualTo($"Could not generate derivative: {-1}"));
                    }
                }

                public class WhenExceptionsOccurr : WhenThingsGoWrong
                {
                    public override async Task BeforeEach()
                    {
                        await base.BeforeEach();
                        DerivativeFileModel = new ProductionFile(MasterFileModel);
                        Exception = new Exception("testing");
                        ProcessRunner.WhenForAnyArgs(x => x.Run(null, CancellationToken.None)).Do(x => throw Exception);
                        FinalException = Assert.ThrowsAsync<LoggedException>(async () => await
                            Runner.CreateProdOrMezzDerivative(MasterFileModel, DerivativeFileModel, new ArgumentBuilder(), Enumerable.Empty<string>(), CancellationToken.None));
                    }

                    private Exception Exception { get; set; }

                    [Test]
                    public void ItShouldCloseExceptionCorrectly()
                    {
                        Observers.Received().EndSection(Arg.Any<string>());
                    }

                    [Test]
                    public void ItShouldLogIssue()
                    {
                        Observers.Received().LogProcessingIssue(Exception, MasterFileModel.BarCode);
                    }

                    [Test]
                    public void ItShouldThrowCorrectLoggedException()
                    {
                        Assert.That(FinalException, Is.Not.Null);
                        Assert.That(FinalException.InnerException.Equals(Exception));
                    }
                }
            }
        }


        [Test]
        public void FFMPEGPathPropertyShouldHaveValidateAttribute()
        {
            var info = typeof (FFMPEGRunner).GetProperty("FFMPEGPath");
            Assert.That(info.GetCustomAttribute<ValidateFileAttribute>(), Is.Not.Null);
        }
    }
}