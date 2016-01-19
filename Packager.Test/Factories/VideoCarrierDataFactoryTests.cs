﻿using System;
using System.Collections.Generic;
using NSubstitute;
using NUnit.Framework;
using Packager.Factories;
using Packager.Models.FileModels;
using Packager.Models.OutputModels.Carrier;
using Packager.Models.PodMetadataModels;

namespace Packager.Test.Factories
{
    [TestFixture]
    public class VideoCarrierDataFactoryTests
    {
        [SetUp]
        public void BeforeEach()
        {
            ProcessingDirectory = "test folder";

            PreservationSide1FileModel = new ObjectFileModel(PreservationSide1FileName);
            MezzSide1FileModel = new ObjectFileModel(MezzSide1FileName);
            AccessSide1FileModel = new ObjectFileModel(AccessSide1FileName);

            PodMetadata = new VideoPodMetadata
            {
                Barcode = "4890764553278906",
                BakingDate = new DateTime(2015, 05, 01),
                CleaningDate = new DateTime(2015, 05, 01),
                CleaningComment = "Cleaning comment",
                Format = "CD-R",
                CallNumber = "Call number",
                DigitizingEntity = "Test entity",
                Identifier = "1",
                Repaired = "Yes",
                RecordingStandard = "recording standard",
                ImageFormat = "image format"
            };

            SideDataFactory = Substitute.For<ISideDataFactory>();

            FilesToProcess = new List<ObjectFileModel>
            {
                PreservationSide1FileModel,
                MezzSide1FileModel,
                AccessSide1FileModel
            };

            var generator = new CarrierDataFactory(SideDataFactory);
            Result = generator.Generate(PodMetadata, FilesToProcess);
        }

        private const string PreservationSide1FileName = "MDPI_4890764553278906_01_pres.mkv";
        private const string MezzSide1FileName = "MDPI_4890764553278906_01_mezz.mov";
        private const string AccessSide1FileName = "MDPI_4890764553278906_01_access.mp4";

        private ObjectFileModel PreservationSide1FileModel { get; set; }
        private ObjectFileModel MezzSide1FileModel { get; set; }
        private ObjectFileModel AccessSide1FileModel { get; set; }


        private string ProcessingDirectory { get; set; }
        private List<ObjectFileModel> FilesToProcess { get; set; }
        private VideoPodMetadata PodMetadata { get; set; }
        private ISideDataFactory SideDataFactory { get; set; }
        private VideoCarrier Result { get; set; }

        public class WhenSettingBasicProperties : VideoCarrierDataFactoryTests
        {
            [Test]
            public void ItShouldSetBarcodeCorrectly()
            {
                Assert.That(Result.Barcode, Is.EqualTo(PodMetadata.Barcode));
            }

            [Test]
            public void ItShouldSetCarrierTypeCorrectly()
            {
                Assert.That(Result.CarrierType, Is.EqualTo("Video"));
            }

            [Test]
            public void ItShouldSetIdentifierCorrectly()
            {
                Assert.That(Result.Identifier, Is.EqualTo(PodMetadata.CallNumber));
            }

            [Test]
            public void IsShouldSetDefinitionCorrectly()
            {
                Assert.That(Result.Definition, Is.EqualTo("SD"));
            }

            [Test]
            public void IsShouldSetImageFormatCorrectly()
            {
                Assert.That(Result.ImageFormat, Is.EqualTo(PodMetadata.ImageFormat));
            }

            [Test]
            public void IsShouldSetRecordingStandardCorrectly()
            {
                Assert.That(Result.RecordingStandard, Is.EqualTo(PodMetadata.RecordingStandard));
            }
        }

        public class WhenSettingCleaningData : VideoCarrierDataFactoryTests
        {
            [Test]
            public void ItShouldSetCleaningCommentCorrectly()
            {
                Assert.That(Result.Cleaning.Comment, Is.EqualTo(PodMetadata.CleaningComment));
            }

            [Test]
            public void ItShouldSetCleaningDateCorrectly()
            {
                Assert.That(Result.Cleaning.Date, Is.EqualTo(PodMetadata.CleaningDate));
            }

            [Test]
            public void ItShouldSetCleaningObject()
            {
                Assert.That(Result.Cleaning, Is.Not.Null);
            }
        }

        public class WhenSettingBakingData : VideoCarrierDataFactoryTests
        {
            [Test]
            public void ItShouldSetBakingDateCorrectly()
            {
                Assert.That(Result.Baking.Date, Is.EqualTo(PodMetadata.BakingDate));
            }

            [Test]
            public void ItShouldSetBakingObject()
            {
                Assert.That(Result.Baking, Is.Not.Null);
            }
        }
        
        public class WhenSettingPhysicalConditionData : VideoCarrierDataFactoryTests
        {
            [Test]
            public void ItShouldSetDamageCorrectly()
            {
                Assert.That(Result.PhysicalCondition.Damage, Is.EqualTo(PodMetadata.Damage));
            }

            [Test]
            public void ItShouldSetPhysicalConditionObject()
            {
                Assert.That(Result.PhysicalCondition, Is.Not.Null);
            }

            [Test]
            public void ItShouldSetPreservationProblemCorrectly()
            {
                Assert.That(Result.PhysicalCondition.PreservationProblem, Is.EqualTo(PodMetadata.PreservationProblems));
            }
        }

        public class WhenSettingPartsData :VideoCarrierDataFactoryTests
        {
            [Test]
            public void ItShouldCallSideDataFactoryCorrectly()
            {
                SideDataFactory.Received().Generate(PodMetadata, FilesToProcess);
            }

            [Test]
            public void ItShouldSetDigitizingEntityCorrectly()
            {
                Assert.That(Result.Parts.DigitizingEntity, Is.EqualTo(PodMetadata.DigitizingEntity));
            }

            [Test]
            public void ItShouldSetPartsDataObjectCorrectly()
            {
                Assert.That(Result.Parts, Is.Not.Null);
            }
        }
    }
}