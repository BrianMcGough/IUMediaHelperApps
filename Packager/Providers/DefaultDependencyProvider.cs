﻿using System;
using Packager.Deserializers;
using Packager.Factories;
using Packager.Models;
using Packager.Models.PodMetadataModels;
using Packager.Models.SettingsModels;
using Packager.Observers;
using Packager.Utilities.Bext;
using Packager.Utilities.Email;
using Packager.Utilities.FileSystem;
using Packager.Utilities.Hashing;
using Packager.Utilities.Process;
using Packager.Utilities.Xml;
using Packager.Validators;
using Packager.Validators.Attributes;
using Packager.Verifiers;
using RestSharp;
using RestSharp.Authenticators;

namespace Packager.Providers
{
    public class DefaultDependencyProvider : IDependencyProvider
    {
        public DefaultDependencyProvider(IProgramSettings programSettings)
        {
            Hasher = new Hasher(programSettings.ProcessingDirectory);
            XmlExporter = new XmlExporter();
            DirectoryProvider = new DirectoryProvider();
            FileProvider = new FileProvider();
            ProcessRunner = new ProcessRunner();
            ProgramSettings = programSettings;
            IngestDataFactory = new IngestDataFactory();
            SideDataFactory = new SideDataFactory(IngestDataFactory);
            MetadataGenerator = new CarrierDataFactory(SideDataFactory);
            SystemInfoProvider = new SystemInfoProvider(programSettings.LogDirectoryName);
            Observers = new ObserverCollection();
            AudioMetadataFactory = new EmbeddedAudioMetadataFactory();
            VideoMetadataFactory = new EmbeddedVideoMetadataFactory();
            MetaEditRunner = new BwfMetaEditRunner(ProcessRunner, programSettings.BwfMetaEditPath,
                programSettings.ProcessingDirectory);
            BextProcessor = new BextProcessor(MetaEditRunner, Observers, new BwfMetaEditResultsVerifier());
            AudioFFMPEGRunner = new AudioFFMPEGRunner(ProgramSettings, ProcessRunner, Observers, FileProvider, Hasher);
            VideoFFMPEGRunner = new VideoFFMPEGRunner(ProgramSettings, ProcessRunner, Observers, FileProvider, Hasher);
            EmailSender = new EmailSender(FileProvider, ProgramSettings.SmtpServer);
            LookupsProvider = new AppConfigLookupsProvider();
            ImportableFactory = new ImportableFactory(LookupsProvider);
            ValidatorCollection = new StandardValidatorCollection
            {
                new ValueRequiredValidator(),
                new DirectoryExistsValidator(DirectoryProvider),
                new FileExistsValidator(FileProvider),
                new UriValidator(),
                new MembersValidator()
            };
            MetadataProvider = new PodMetadataProvider(GetRestClient(ProgramSettings, FileProvider, ImportableFactory), Observers,
                ValidatorCollection);
            SuccessFolderCleaner = new SuccessFolderCleaner(DirectoryProvider, programSettings.SuccessDirectoryName,
                new TimeSpan(programSettings.DeleteSuccessfulObjectsAfterDays, 0, 0, 0), Observers);
            FFProbeRunner = new FFProbeRunner(ProgramSettings, ProcessRunner, FileProvider, Observers);
        }

        [ValidateObject]
        public ISystemInfoProvider SystemInfoProvider { get; }

        [ValidateObject]
        public IHasher Hasher { get; }

        [ValidateObject]
        public IXmlExporter XmlExporter { get; }

        [ValidateObject]
        public IDirectoryProvider DirectoryProvider { get; }

        [ValidateObject]
        public IFileProvider FileProvider { get; }

        [ValidateObject]
        public IProcessRunner ProcessRunner { get; }

        [ValidateObject]
        public ICarrierDataFactory MetadataGenerator { get; }

        [ValidateObject]
        public IProgramSettings ProgramSettings { get; }

        public IImportableFactory ImportableFactory { get; }

        [ValidateObject]
        public IObserverCollection Observers { get; }

        [ValidateObject]
        public IPodMetadataProvider MetadataProvider { get; }

        [ValidateObject]
        public IBextProcessor BextProcessor { get; }

        [ValidateObject]
        public IFFMPEGRunner AudioFFMPEGRunner { get; }

        public IFFMPEGRunner VideoFFMPEGRunner { get; }

        [ValidateObject]
        public IEmailSender EmailSender { get; }

        [ValidateObject]
        public ISideDataFactory SideDataFactory { get; }

        [ValidateObject]
        public IIngestDataFactory IngestDataFactory { get; }

        public IValidatorCollection ValidatorCollection { get; }
        public ISuccessFolderCleaner SuccessFolderCleaner { get; }
        public IEmbeddedMetadataFactory<AudioPodMetadata> AudioMetadataFactory { get; }
        public IEmbeddedMetadataFactory<VideoPodMetadata> VideoMetadataFactory { get; }

        [ValidateObject]
        public ILookupsProvider LookupsProvider { get; }

        public IFFProbeRunner FFProbeRunner { get; }

        public IBwfMetaEditRunner MetaEditRunner { get; }

        private static IRestClient GetRestClient(IProgramSettings programSettings, IFileProvider fileProvider, IImportableFactory factory)
        {
            var podAuth = fileProvider.Deserialize<PodAuth>(programSettings.PodAuthFilePath);
            var result = new RestClient(programSettings.WebServiceUrl)
            {
                Authenticator = new HttpBasicAuthenticator(podAuth.UserName, podAuth.Password)
            };

            result.AddHandler("application/xml", new PodResultDeserializer(factory));
            return result;
        }
    }
}