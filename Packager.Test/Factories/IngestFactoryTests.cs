﻿using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Packager.Exceptions;
using Packager.Factories;
using Packager.Models.FileModels;
using Packager.Models.OutputModels;
using Packager.Models.PodMetadataModels;

namespace Packager.Test.Factories
{
    [TestFixture]
    public class IngestFactoryTests
    {
        private const string GoodFileName = "MDPI_4890764553278906_01_pres.wav";
        private const string MissingFileName = "MDPI_111111111111_01_pres.wav";
        private ConsolidatedPodMetadata PodMetadata { get; set; }
        private AbstractFileModel FileModel { get; set; }
        private DigitalFileProvenance Provenance { get; set; }
        private IngestData Result { get; set; }

        private DigitalFileProvenance GenerateFileProvenance(string fileName)
        {
            return new DigitalFileProvenance
            {
                Filename = fileName,
                Comment = "File provenance comment",
                CreatedBy = "Test user",
                DateDigitized = new DateTime(2015, 05, 01),
                SpeedUsed = "7.5 ips",
                SignalChain = GetSignalChain()
            };
        }
        
        private static List<Device> GetSignalChain()
        {
            var result = new List<Device>
            {
                new Device {DeviceType = "extraction workstation", Model = "ew model", Manufacturer = "ew manufacturer", SerialNumber = "ew serial number"},
                new Device {DeviceType = "player", Model = "Player model", Manufacturer = "Player manufacturer", SerialNumber = "Player serial number"},
                new Device {DeviceType = "ad", Model = "Ad model", Manufacturer = "Ad manufacturer", SerialNumber = "Ad serial number"},
            };

            return result;
        }

        public class WhenProvenanceIsMissing : IngestFactoryTests
        {
            [SetUp]
            public void BeforeEach()
            {
                FileModel = new ObjectFileModel(MissingFileName);
                Provenance = GenerateFileProvenance(GoodFileName);

                PodMetadata = new ConsolidatedPodMetadata
                {
                    Format = "CD-R",
                    FileProvenances = new List<DigitalFileProvenance> {Provenance}
                };
            }

            [Test]
            public void ShouldThrowOutputXmlException()
            {
                var factory = new IngestDataFactory();
                Assert.Throws<OutputXmlException>(() => factory.Generate(PodMetadata, FileModel));
            }
        }

        public class WhenProvenanceIsPresent : IngestFactoryTests
        {
            [SetUp]
            public void BeforeEach()
            {
                FileModel = new ObjectFileModel(GoodFileName);
                Provenance = GenerateFileProvenance(GoodFileName);

                PodMetadata = new ConsolidatedPodMetadata
                {
                    Format = "CD-R",
                    FileProvenances = new List<DigitalFileProvenance> {Provenance}
                };

                var factory = new IngestDataFactory();
                Result = factory.Generate(PodMetadata, FileModel);
            }

            private static void AssertDeviceCollectionOk(IReadOnlyList<Device> devices, IReadOnlyList<IngestDevice> ingestDevices)
            {
                Assert.That(devices.Count, Is.EqualTo(ingestDevices.Count));
                for (var i = 0; i < devices.Count; i++)
                {
                    Assert.That(ingestDevices[i].Model, Is.EqualTo(devices[i].Model));
                    Assert.That(ingestDevices[i].SerialNumber, Is.EqualTo(devices[i].SerialNumber));
                    Assert.That(ingestDevices[i].Manufacturer, Is.EqualTo(devices[i].Manufacturer));
                }
            }

            [Test]
            public void ItShouldExtractionWorkstationCorrectly()
            {
                Assert.That(Result.ExtractionWorkstation.SerialNumber, Is.EqualTo(Provenance.ExtractionWorkstation.SerialNumber));
                Assert.That(Result.ExtractionWorkstation.Model, Is.EqualTo(Provenance.ExtractionWorkstation.Model));
                Assert.That(Result.ExtractionWorkstation.Manufacturer, Is.EqualTo(Provenance.ExtractionWorkstation.Manufacturer));
            }

            [Test]
            public void ItShouldPlayerDevicesCorrectly()
            {
                AssertDeviceCollectionOk(Provenance.PlayerDevices.ToArray(), Result.Players);
            }

            [Test]
            public void ItShouldSetAdDevicesCorrectly()
            {
                AssertDeviceCollectionOk(Provenance.AdDevices.ToArray(), Result.AdDevices);
            }

            [Test]
            public void ItShouldSetCommentsCorrectly()
            {
                Assert.That(Result.Comments, Is.EqualTo(Provenance.Comment));
            }

            [Test]
            public void ItShouldSetCreatedByCorrectly()
            {
                Assert.That(Result.CreatedBy, Is.EqualTo(Provenance.CreatedBy));
            }

            [Test]
            public void ItShouldSetDateCorrectly()
            {
                Assert.That(Result.Date, Is.EqualTo(Provenance.DateDigitized.Value.ToString()));
            }

            [Test]
            public void ItShouldSetSpeedUsedCorrectly()
            {
                Assert.That(Result.SpeedUsed, Is.EqualTo(Provenance.SpeedUsed));
            }

            [Test]
            public void ItShouldSetXsiTypeCorrectly()
            {
                Assert.That(Result.XsiType, Is.EqualTo($"{PodMetadata.Format}Ingest"));
            }
        }
    }
}