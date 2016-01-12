﻿using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Packager.Factories;
using Packager.Models.EmbeddedMetadataModels;
using Packager.Models.FileModels;
using Packager.Models.PodMetadataModels;

namespace Packager.Test.Factories.EmbeddedVideoMetadataFactoryTests
{
    [TestFixture]
    public abstract class EmbeddedVideoMetadataFactoryTests
    {
        [SetUp]
        public void BeforeEach()
        {
            PresProvenance = GetFileProvenance(PresFilename);
            MezzProvenance = GetFileProvenance(MezzFilename);
            PresModel = new ObjectFileModel(PresFilename);
            MezzModel = new ObjectFileModel(MezzFilename);
            Instances = new List<ObjectFileModel> {PresModel, MezzModel};
            Metadata = new VideoPodMetadata
            {
                DigitizingEntity = DigitizingEntity,
                Unit = Unit,
                CallNumber = CallNumber,
                Format = "Record",
                Title = Title,
                FileProvenances = new List<AbstractDigitalFile> {PresProvenance, MezzProvenance}
            };

            DoCustomSetup();

            Result =
                new EmbeddedVideoMetadataFactory().Generate(Instances, ModelToUseForTesting, Metadata) as AbstractEmbeddedVideoMetadata;
        }

        private const string PresFilename = "MDPI_4890764553278906_01_pres.mkv";
        private const string MezzFilename = "MDPI_4890764553278906_01_mezz.mov";
        private const string DigitizingEntity = "Test digitizing entity";
        private const string Unit = "Test unit";
        private const string CallNumber = "AB1243";
        private const string Title = "Test title";

        public string FilenameToUseForTesting { get; set; }
        public ObjectFileModel ModelToUseForTesting { get; set; }
        private AbstractEmbeddedVideoMetadata Result { get; set; }
        private ObjectFileModel PresModel { get; set; }
        private ObjectFileModel MezzModel { get; set; }
        private DigitalVideoFile PresProvenance { get; set; }
        private DigitalVideoFile MezzProvenance { get; set; }
        private VideoPodMetadata Metadata { get; set; }

        private List<ObjectFileModel> Instances { get; set; }

        protected virtual void DoCustomSetup()
        {
        }

        private DigitalVideoFile GetFileProvenance(string filename)
        {
            return new DigitalVideoFile
            {
                Comment = "Comment",
                CreatedBy = "Created by",
                DateDigitized = new DateTime(2015, 8, 1, 1, 2, 3),
                Filename = filename,
                SignalChain = GetSignalChain()
            };
        }

        private static List<Device> GetSignalChain()
        {
            var result = new List<Device>
            {
                new Device
                {
                    DeviceType = "extraction workstation",
                    Model = "ew model",
                    Manufacturer = "ew manufacturer",
                    SerialNumber = "ew serial number"
                },
                new Device
                {
                    DeviceType = "player",
                    Model = "Player model",
                    Manufacturer = "Player manufacturer",
                    SerialNumber = "Player serial number"
                },
                new Device
                {
                    DeviceType = "ad",
                    Model = "Ad model",
                    Manufacturer = "Ad manufacturer",
                    SerialNumber = "Ad serial number"
                }
            };

            return result;
        }

        [Test]
        public void DescriptionShouldBeCorrect()
        {
            var expected = $"{Unit}. {CallNumber}. File use: {ModelToUseForTesting.FullFileUse}. {Path.GetFileNameWithoutExtension(FilenameToUseForTesting)}";
            Assert.That(Result.Description, Is.EqualTo(expected));
        }

        [Test]
        public void TitleShouldBeCorrect()
        {
            Assert.That(Result.Title, Is.EqualTo(Metadata.Title));
        }

        [Test]
        public void MasteredDateShouldBeCorrect()
        {
            Assert.That(Result.MasteredDate, Is.EqualTo("2015-08-01"));
        }

        [Test]
        public void CommentShouldBeCorrect()
        {
            Assert.That(Result.Comment, Is.EqualTo($"File created by {Metadata.DigitizingEntity}"));
        }

        public class WhenModelIsPreservationMaster : EmbeddedVideoMetadataFactoryTests
        {
            protected override void DoCustomSetup()
            {
                base.DoCustomSetup();
                FilenameToUseForTesting = PresFilename;
                ModelToUseForTesting = PresModel;
            }

            [Test]
            public void ResultShouldBeCorrectType()
            {
                Assert.That(Result is EmbeddedVideoPreservationMetadata);
            }
        }

        public class WhenModelIsMezzanine : EmbeddedVideoMetadataFactoryTests
        {
            protected override void DoCustomSetup()
            {
                base.DoCustomSetup();
                FilenameToUseForTesting = MezzFilename;
                ModelToUseForTesting = MezzModel;
            }

            [Test]
            public void ResultShouldBeCorrectType()
            {
                Assert.That(Result is EmbeddedVideoMezzanineMetadata);
            }
        }
    }
}