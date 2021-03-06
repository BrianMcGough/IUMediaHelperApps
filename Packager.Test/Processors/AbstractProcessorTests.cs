﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Models;
using NSubstitute;
using NUnit.Framework;
using Packager.Extensions;
using Packager.Factories;
using Packager.Factories.FFMPEGArguments;
using Packager.Models.EmbeddedMetadataModels;
using Packager.Models.FileModels;
using Packager.Models.PodMetadataModels;
using Packager.Models.ResultModels;
using Packager.Models.SettingsModels;
using Packager.Observers;
using Packager.Processors;
using Packager.Providers;
using Packager.Utilities.Bext;
using Packager.Utilities.Hashing;
using Packager.Utilities.Images;
using Packager.Utilities.ProcessRunners;
using Packager.Utilities.Xml;

namespace Packager.Test.Processors
{
    public abstract class AbstractProcessorTests
    {
        protected const string ProjectCode = "MDPI";
        protected const string Barcode = "4890764553278906";
        protected const string InputDirectory = "work";
        protected const string DropBoxRoot = "dropbox";
        protected const string ErrorDirectory = "error";
        protected const string ProcessingRoot = "processing";
        protected const string DigitizingEntity = "test digitizing entity";

        protected IProgramSettings ProgramSettings { get; set; }
        //protected IDependencyProvider DependencyProvider { get; set; }
        protected IDirectoryProvider DirectoryProvider { get; set; }
        protected IFileProvider FileProvider { get; set; }
        protected IHasher Hasher { get; set; }
        protected IProcessRunner ProcessRunner { get; set; }
        protected IXmlExporter XmlExporter { get; set; }
        protected IObserverCollection Observers { get; set; }
        protected IProcessor Processor { get; set; }
        protected IPodMetadataProvider MetadataProvider { get; set; }
        protected ICarrierDataFactory<AudioPodMetadata> AudioCarrierDataFactory { get; set; }
        protected ICarrierDataFactory<VideoPodMetadata> VideoCarrierDataFactory { get; set; }
        protected IBextProcessor BextProcessor { get; set; }
        protected IFFMPEGRunner FFMPEGRunner { get; set; }
        protected IFFMPEGArgumentsFactory FFMPEGArgumentsFactory { get; set; }
        protected IFFProbeRunner FFProbeRunner { get; set; }
        protected ILabelImageImporter ImageProcessor { get; set; }
        protected IPlaceHolderFactory PlaceHolderFactory { get; set; }

        protected string ExpectedProcessingDirectory => Path.Combine(ProcessingRoot, ExpectedObjectFolderName);
        protected string ExpectedOriginalsDirectory => Path.Combine(ExpectedProcessingDirectory, "Originals");
        protected string ExpectedObjectFolderName { get; set; }

        protected IEmbeddedMetadataFactory<AudioPodMetadata> AudioMetadataFactory { get; set; }
        protected IEmbeddedMetadataFactory<VideoPodMetadata> VideoMetadataFactory { get; set; }

        protected string PreservationFileName { get; set; }
        protected string PreservationIntermediateFileName { get; set; }
        protected string ProductionFileName { get; set; }
        protected string AccessFileName { get; set; }

        protected AbstractFile PresAbstractFile { get; set; }
        protected AbstractFile ProdAbstractFile { get; set; }
        protected AbstractFile PresIntAbstractFile { get; set; }
        protected AbstractFile AccessAbstractFile { get; set; }

        protected string XmlManifestFileName { get; set; }

        public DurationResult Result { get; set; }

        protected List<AbstractFile> ModelList { get; set; }

        protected abstract void DoCustomSetup();

        protected IGrouping<string, AbstractFile> GetGrouping(IEnumerable<AbstractFile> models)
        {
            return models.GroupBy(m => m.BarCode).First();
        }

        [SetUp]
        public virtual async Task BeforeEach()
        {
            ProgramSettings = Substitute.For<IProgramSettings>();
            ProgramSettings.ProjectCode.Returns(ProjectCode);
            ProgramSettings.InputDirectory.Returns(InputDirectory);
            ProgramSettings.DropBoxDirectoryName.Returns(DropBoxRoot);
            ProgramSettings.ErrorDirectoryName.Returns(ErrorDirectory);
            ProgramSettings.ProcessingDirectory.Returns(ProcessingRoot);
            ProgramSettings.DigitizingEntity.Returns(DigitizingEntity);

            AudioMetadataFactory = Substitute.For<IEmbeddedMetadataFactory<AudioPodMetadata>>();
            AudioMetadataFactory.Generate(Arg.Any<IEnumerable<AbstractFile>>(), Arg.Any<AbstractFile>(), Arg.Any<AudioPodMetadata>())
                .Returns(new EmbeddedAudioMetadata());

            VideoMetadataFactory = Substitute.For<IEmbeddedMetadataFactory<VideoPodMetadata>>();
            VideoMetadataFactory.Generate(Arg.Any<IEnumerable<AbstractFile>>(),
                Arg.Is<AbstractFile>(a => a.IsMezzanineVersion()), Arg.Any<VideoPodMetadata>())
                .Returns(new EmbeddedVideoMezzanineMetadata());
            VideoMetadataFactory.Generate(Arg.Any<IEnumerable<AbstractFile>>(),
                Arg.Is<AbstractFile>(a => a.IsPreservationVersion() || a.IsPreservationIntermediateVersion()),
                Arg.Any<VideoPodMetadata>())
                .Returns(new EmbeddedVideoPreservationMetadata());

            DirectoryProvider = Substitute.For<IDirectoryProvider>();
            FileProvider = Substitute.For<IFileProvider>();
            Hasher = Substitute.For<IHasher>();
            ProcessRunner = Substitute.For<IProcessRunner>();
            XmlExporter = Substitute.For<IXmlExporter>();
            Observers = Substitute.For<IObserverCollection>();
            MetadataProvider = Substitute.For<IPodMetadataProvider>();
            MetadataProvider.AdjustMediaFormat(Arg.Any<AudioPodMetadata>(), Arg.Any<List<AbstractFile>>()).Returns(a => a.Arg<AudioPodMetadata>());
            MetadataProvider.AdjustMediaFormat(Arg.Any<VideoPodMetadata>(), Arg.Any<List<AbstractFile>>()).Returns(a => a.Arg<VideoPodMetadata>());
            MetadataProvider.AdjustDigitalProvenanceData(Arg.Any<AudioPodMetadata>(), Arg.Any<List<AbstractFile>>()).Returns(a => a.Arg<AudioPodMetadata>());
            MetadataProvider.AdjustDigitalProvenanceData(Arg.Any<VideoPodMetadata>(), Arg.Any<List<AbstractFile>>()).Returns(a => a.Arg<VideoPodMetadata>());
           
            AudioCarrierDataFactory = Substitute.For<ICarrierDataFactory<AudioPodMetadata>>();
            VideoCarrierDataFactory = Substitute.For<ICarrierDataFactory<VideoPodMetadata>>();
            BextProcessor = Substitute.For<IBextProcessor>();

            FFMPEGRunner = Substitute.For<IFFMPEGRunner>();
            FFMPEGRunner.CreateAccessDerivative(Arg.Any<AbstractFile>(), Arg.Any<ArgumentBuilder>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
                .Returns(x => Task.FromResult((AbstractFile)new AccessFile(x.Arg<AbstractFile>())));
            FFMPEGRunner.CreateProdOrMezzDerivative(Arg.Any<AbstractFile>(), Arg.Any<AbstractFile>(), Arg.Any<ArgumentBuilder>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
                .Returns(x => Task.FromResult(x.ArgAt<AbstractFile>(1)));

            FFProbeRunner = Substitute.For<IFFProbeRunner>();
            FFProbeRunner.GenerateQualityControlFile(Arg.Any<AbstractFile>(), Arg.Any<CancellationToken>())
                .Returns(a => Task.FromResult(new QualityControlFile(a.Arg<AbstractFile>())));

            ImageProcessor = Substitute.For<ILabelImageImporter>();
            ImageProcessor.ImportMediaImages(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(new List<AbstractFile>()));

            PlaceHolderFactory = Substitute.For<IPlaceHolderFactory>();
            PlaceHolderFactory.GetPlaceHoldersToAdd(Arg.Any<IMediaFormat>(), Arg.Any<List<AbstractFile>>())
                .Returns(new List<AbstractFile>());

            FFMPEGArgumentsFactory = Substitute.For<IFFMPEGArgumentsFactory>();

            DoCustomSetup();

            Result = await Processor.ProcessObject(GetGrouping(ModelList), CancellationToken.None);
        }
    }
}