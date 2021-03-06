﻿using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Common.Models;
using NSubstitute;
using NUnit.Framework;
using Packager.Factories;
using Packager.Models.PodMetadataModels;

namespace Packager.Test.Models.MetadataModels.PodMetadataTests
{
    [TestFixture]
    public abstract class AbstractPodMetadataTests
    {
        [SetUp]
        public virtual void BeforeEach()
        {
            Factory = Substitute.For<IImportableFactory>();
            Element = new XElement("pod");

            Factory.ToBooleanValue(Element, "success").Returns(true);
            Factory.ToStringValue(Element, "message").Returns("test message");

            Factory.ToStringValue(Element, "data/damage").Returns("damage value");
            Factory.ToStringValue(Element, "data/preservation_problems")
                .Returns("preservation problems value");
            Factory.ToDateTimeValue(Element, "data/baking_date")
                .Returns(new DateTime(2016, 1, 1));
            Factory.ToStringValue(Element, "data/mdpi_barcode").Returns("barcode value");
            Factory.ToStringValue(Element, "data/call_number").Returns("call number value");

            Factory.ToStringValue(Element, "data/cleaning_comment").Returns("cleaning comment value");
            Factory.ToDateTimeValue(Element, "data/cleaning_date")
                .Returns(new DateTime(2016, 2, 1));
            Factory.ToStringValue(Element, "data/repaired").Returns("No"); //todo: fix this so it's a factory method and is more easily mockable

            Factory.ToStringValue(Element, "data/title").Returns("title value");
            Factory.ToStringValue(Element, "data/unit").Returns("unit value");
            Factory.ToStringValue(Element, "data/comments").Returns("comments value");
            Factory.ToStringValue(Element, "data/playback_speed").Returns("speed value");
            Factory.ToStringValue(Element, "data/file_iarl").Returns("file iarl value");
            Factory.ToStringValue(Element, "data/file_icmt").Returns("file icmt value");
            Factory.ToStringValue(Element, "data/file_bext").Returns("file bext value");
        }

        private IMediaFormat FormatValue { get; set; }
        private IImportableFactory Factory { get; set; }
        private XElement Element { get; set; }
        private AbstractPodMetadata Instance { get; set; }
        
        public class WhenImportingOpenReelAudioPodMetadata : AbstractPodMetadataTests
        {
         
            private AudioPodMetadata AudioPodMetadata => Instance as AudioPodMetadata;

            private List<DigitalAudioFile> MockProvenances { get; set; }

            [SetUp]
            public override void BeforeEach()
            {
                base.BeforeEach();

                MockProvenances = new List<DigitalAudioFile>
                {
                    new DigitalAudioFile { Filename = "test1"},
                    new DigitalAudioFile { Filename = "test2"}
                };

                // need to make factory produce correct value for format
                FormatValue = MediaFormats.OpenReelAudioTape;
                Factory.ToStringValue(Element, "data/format").Returns(MediaFormats.OpenReelAudioTape.Key);
                Factory.ToStringValue(Element, "data/sound_field").Returns("sound field value");
                Factory.ToStringValue(Element, "data/tape_thickness").Returns("tape thickness value");
                Factory.ToStringValue(Element, "data/track_configuration").Returns("track configuration value");
                Factory.ToStringValue(Element, "data/tape_stock_brand").Returns("brand value");
                Factory.ToStringValue(Element, "data/directions_recorded").Returns("directions recorded value");

                Factory.ToObjectList<DigitalAudioFile>(Element,
                    "data/digital_files/digital_file_provenance")
                    .Returns(MockProvenances);

                Instance = new AudioPodMetadata();
                Instance.ImportFromXml(Element, Factory);
            }
            
            [Test]
            public void ItShouldCallFactoryToResolveSoundField()
            {
                Factory.Received().ToStringValue(Element, "data/sound_field");
            }

            [Test]
            public void ItShouldSetSoundFieldCorrectly()
            {
                Assert.That(AudioPodMetadata.SoundField, Is.EqualTo("sound field value"));
            }

            [Test]
            public void ItShouldCallFactoryToResolveTapeThickness()
            {
                Factory.Received().ToStringValue(Element, "data/tape_thickness");
            }

            [Test]
            public void ItShouldSetTapeThicknessCorrectly()
            {
                Assert.That(AudioPodMetadata.TapeThickness, Is.EqualTo("tape thickness value"));
            }

            [Test]
            public void ItShouldCallFactoryToResolveTrackConfiguration()
            {
                Factory.Received().ToStringValue(Element, "data/track_configuration");
            }

            [Test]
            public void ItShouldSetTrackConfigurationCorrectly()
            {
                Assert.That(AudioPodMetadata.TrackConfiguration, Is.EqualTo("track configuration value"));
            }

            [Test]
            public void ItShouldUseCorrectPathToResolveBrand()
            {
                Factory.Received().ToStringValue(Element, "data/tape_stock_brand");
            }

            [Test]
            public void ItShouldSetBrandCorrectly()
            {
                Assert.That(AudioPodMetadata.Brand, Is.EqualTo("brand value"));
            }

            [Test]
            public void ItShouldUseCorrectPathToResolveDirectionsRecorded()
            {
                Factory.Received().ToStringValue(Element, "data/directions_recorded");
            }

            [Test]
            public void ItShouldSetDirectionsRecordedCorrectly()
            {
                Assert.That(AudioPodMetadata.DirectionsRecorded, Is.EqualTo("directions recorded value"));
            }

            [Test]
            public void ItShouldCallFactoryCorrectlyToImportFileProvenances()
            {
                Factory.Received().ToObjectList<DigitalAudioFile>(Element, "data/digital_files/digital_file_provenance");
            }

            [Test]
            public void ItShouldSetProvenancesCorrectly()
            {
                Assert.That(Instance.FileProvenances, Is.EquivalentTo(MockProvenances));
            }
        }

        public class WhenImportingLacquerDiscAudioPodMetadata : AbstractPodMetadataTests
        {
            private AudioPodMetadata AudioPodMetadata => Instance as AudioPodMetadata;
            private List<DigitalAudioFile> MockProvenances { get; set; }

            [SetUp]
            public override void BeforeEach()
            {
                base.BeforeEach();

                MockProvenances = new List<DigitalAudioFile>
                {
                    new DigitalAudioFile { Filename = "test1"},
                    new DigitalAudioFile { Filename = "test2"}
                };

                // need to make factory produce correct value for format
                FormatValue = MediaFormats.LacquerDisc;
                Factory.ToStringValue(Element, "data/format").Returns(MediaFormats.LacquerDisc.Key);
                
                Factory.ToStringValue(Element, "data/speed").Returns("speed value");
                Factory.ToStringValue(Element, "data/sound_field").Returns("sound field value");

                Factory.ToObjectList<DigitalAudioFile>(Element,
                  "data/digital_files/digital_file_provenance")
                  .Returns(MockProvenances);

                Instance = new AudioPodMetadata();
                Instance.ImportFromXml(Element, Factory);
            }

            [Test]
            public void ItShouldCallFactoryToResolveSoundField()
            {
                Factory.Received().ToStringValue(Element, "data/sound_field");
            }

            [Test]
            public void SoundfieldShouldHaveCorrectValue()
            {
                Assert.That(AudioPodMetadata.SoundField, Is.EqualTo("sound field value"));
            }

            [Test]
            public void ItShouldCallFactoryCorrectlyToImportFileProvenances()
            {
                Factory.Received().ToObjectList<DigitalAudioFile>(Element, "data/digital_files/digital_file_provenance");
            }

            [Test]
            public void ItShouldSetProvenancesCorrectly()
            {
                Assert.That(Instance.FileProvenances, Is.EquivalentTo(MockProvenances));
            }
        }
        
        public class WhenImportingVideoPodMetadata : AbstractPodMetadataTests
        {
            private VideoPodMetadata VideoPodMetadata => Instance as VideoPodMetadata;

            private List<DigitalVideoFile> MockProvenances { get; set; }

            [SetUp]
            public override void BeforeEach()
            {
                base.BeforeEach();

                MockProvenances = new List<DigitalVideoFile>
                {
                    new DigitalVideoFile { Filename = "test1"},
                    new DigitalVideoFile { Filename = "test2"}
                };

                // need to make factory produce correct value for format
                FormatValue = MediaFormats.EightMillimeterVideo;
                Factory.ToStringValue(Element, "data/format").Returns(MediaFormats.EightMillimeterVideo.Key);
                Factory.ToStringValue(Element, "data/image_format").Returns("image format value");
                Factory.ToStringValue(Element, "data/recording_standard").Returns("recording standard value");
               
                Factory.ToObjectList<DigitalVideoFile>(Element,
                  "data/digital_files/digital_file_provenance")
                  .Returns(MockProvenances);

                Instance = new VideoPodMetadata();
                Instance.ImportFromXml(Element, Factory);
            }

            [Test]
            public void ItShouldCallFactoryCorrectlyToResolveImageFormat()
            {
                Factory.Received().ToStringValue(Element, "data/image_format");
            }

            [Test]
            public void ItShouldSetImageFormatCorrectly()
            {
                Assert.That(VideoPodMetadata.ImageFormat, Is.EqualTo("image format value"));
            }

            [Test]
            public void ItShouldCallFactoryCorrectlyToResolveRecordingStandard()
            {
                Factory.Received().ToStringValue(Element, "data/recording_standard");
            }

            [Test]
            public void ItShouldSetRecordingStandardCorrectly()
            {
                Assert.That(VideoPodMetadata.RecordingStandard, Is.EqualTo("recording standard value"));
            }

            [Test]
            public void ItShouldCallFactoryCorrectlyToImportFileProvenances()
            {
                Factory.Received().ToObjectList<DigitalVideoFile>(Element, "data/digital_files/digital_file_provenance");
            }

            [Test]
            public void ItShouldSetProvenancesCorrectly()
            {
                Assert.That(Instance.FileProvenances, Is.EquivalentTo(MockProvenances));
            }

         
        }

        [Test]
        public void ItShouldCallFactoryCorrectlyToPreservationProblems()
        {
            Factory.Received().ToStringValue(Element, "data/preservation_problems");
        }

        [Test]
        public void ItShouldSetPreservationProblemsCorrectly()
        {
            Assert.That(Instance.PreservationProblems, Is.EqualTo("preservation problems value"));
        }

        [Test]
        public void ItShouldCallFactoryCorrectlyToResolveDamage()
        {
            Factory.Received().ToStringValue(Element, "data/damage");
        }

        [Test]
        public void ItShouldSetDamageCorrectly()
        {
            Assert.That(Instance.Damage, Is.EqualTo("damage value"));
        }

        [Test]
        public void ItShouldUseCorrectPathToResolveBakingDate()
        {
            Factory.Received().ToDateTimeValue(Element, "data/baking_date");
        }

        [Test]
        public void ItShouldSetBakingDateCorrectly()
        {
            Assert.That(Instance.BakingDate, Is.EqualTo(new DateTime(2016,1,1)));
        }

        [Test]
        public void ItShouldUseCorrectPathToResolveBarcode()
        {
            Factory.Received().ToStringValue(Element, "data/mdpi_barcode");
        }

        [Test]
        public void ItShouldSetBarcodeCorrectly()
        {
            Assert.That(Instance.Barcode, Is.EqualTo("barcode value"));
        }

        [Test]
        public void ItShouldUseCorrectPathToResolveCallNumber()
        {
            Factory.Received().ToStringValue(Element, "data/call_number");
        }

        [Test]
        public void ItShouldSetCallNumberCorrectly()
        {
            Assert.That(Instance.CallNumber, Is.EqualTo("call number value"));
        }

        [Test]
        public void ItShouldUseCorrectPathToResolveCleaningComment()
        {
            Factory.Received().ToStringValue(Element, "data/cleaning_comment");
        }

        [Test]
        public void ItShouldSetCleaningCommentCorrectly()
        {
            Assert.That(Instance.CleaningComment, Is.EqualTo("cleaning comment value"));
        }

        [Test]
        public void ItShouldUseCorrectPathToResolveCleaningDate()
        {
            Factory.Received().ToDateTimeValue(Element, "data/cleaning_date");
        }

        [Test]
        public void ItShouldSetCleaningDateCorrectly()
        {
            Assert.That(Instance.CleaningDate, Is.EqualTo(new DateTime(2016,2,1)));
        }
        
        [Test]
        public void ItShouldUseCorrectPathToResolveFormet()
        {
            Factory.Received().ToStringValue(Element, "data/format");
        }

        [Test]
        public void ItShouldSetFormatCorrectly()
        {
            Assert.That(Instance.Format, Is.EqualTo(FormatValue));
        }

        [Test]
        public void ItShouldCallFactoryToResolvePlaybackSpeed()
        {
            Factory.Received().ToStringValue(Element, "data/playback_speed");
        }

        [Test]
        public void ItShouldSetPlaybackSpeedCorrectly()
        {
            Assert.That(Instance.PlaybackSpeed, Is.EqualTo("speed value"));
        }

        [Test]
        public void ItShouldUseCorrectPathToResolveRepaired()
        {
            Factory.Received().ToBooleanValue(Element, "data/repaired");
        }

        [Test]
        public void ItShouldSetRepairedCorrectly()
        {
            Assert.That(Instance.Repaired, Is.EqualTo("No"));
        }

        [Test]
        public void ItShouldUseCorrectPathToResolveTitle()
        {
            Factory.Received().ToStringValue(Element, "data/title");
        }

        [Test]
        public void ItShouldSetTitleCorrectly()
        {
            Assert.That(Instance.Title, Is.EqualTo("title value"));
        }

        [Test]
        public void ItShouldUseCorrectPathToResolveUnit()
        {
            Factory.Received().ToStringValue(Element, "data/unit");
        }

        [Test]
        public void ItShouldSetUnitCorrectly()
        {
            Assert.That(Instance.Unit, Is.EqualTo("unit value"));
        }

        [Test]
        public void ItShouldCallFactoryCorrectlyToResolveComments()
        {
            Factory.Received().ToStringValue(Element, "data/comments");
        }

        [Test]
        public void ItShouldSetCommentsCorrectly()
        {
            Assert.That(Instance.Comments, Is.EqualTo("comments value"));
        }

        [Test]
        public void ItShouldUseCorrectPathToResolveMessage()
        {
            Factory.Received().ToStringValue(Element, "message");
        }

        [Test]
        public void SuccessShouldHaveCorrectValue()
        {
            Assert.That(Instance.Success, Is.True);
        }

        [Test]
        public void MessageShouldHaveCorrectValue()
        {
            Assert.That(Instance.Message, Is.EqualTo("test message"));
        }

        [Test]
        public void ItShouldUseCorrectPathToResolveSuccess()
        {
            Factory.Received().ToBooleanValue(Element, "success");
        }

        [Test]
        public void ItShouldUseCorrectPathToResolveIarl()
        {
            Factory.Received().ToStringValue(Element, "data/file_iarl");
        }

        [Test]
        public void ItShouldSetIarlCorrectly()
        {
            Assert.That(Instance.Iarl, Is.EqualTo("file iarl value"));
        }

        [Test]
        public void ItShouldUseCorrectPathToResolveIcmt()
        {
            Factory.Received().ToStringValue(Element, "data/file_icmt");
        }

        [Test]
        public void ItShouldSetIcmtCorrectly()
        {
            Assert.That(Instance.Icmt, Is.EqualTo("file icmt value"));
        }

        [Test]
        public void ItShouldSetBextCorrectly()
        {
            Assert.That(Instance.Bext, Is.EqualTo("file bext value"));
        }

        [Test]
        public void ItShouldUseCorrectPathToResolveBext()
        {
            Factory.Received().ToStringValue(Element, "data/file_bext");
        }

    }
}