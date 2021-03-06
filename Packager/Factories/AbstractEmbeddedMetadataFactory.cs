using System;
using System.Collections.Generic;
using System.Linq;
using Packager.Exceptions;
using Packager.Extensions;
using Packager.Models.EmbeddedMetadataModels;
using Packager.Models.FileModels;
using Packager.Models.PodMetadataModels;
using Packager.Models.SettingsModels;

namespace Packager.Factories
{
    public abstract class AbstractEmbeddedMetadataFactory<T> : IEmbeddedMetadataFactory<T> where T : AbstractPodMetadata
    {
        protected AbstractEmbeddedMetadataFactory(IProgramSettings programSettings)
        {
            DigitizingEntity = programSettings.DigitizingEntity;
        }

        private string DigitizingEntity { get; }

        public AbstractEmbeddedMetadata Generate(IEnumerable<AbstractFile> models, AbstractFile target, T metadata)
        {
            var provenance = GetProvenance(metadata, models, target);
            return Generate(target, provenance, metadata, DigitizingEntity);
        }

        protected abstract AbstractEmbeddedMetadata Generate(AbstractFile model, AbstractDigitalFile provenance, T metadata, string digitizingEntity);
        
        protected static string GetDateString(DateTime? date, string format, string defaultValue)
        {
            return date.HasValue == false ? defaultValue : date.Value.ToString(format);
        }

        private static AbstractDigitalFile GetProvenance(AbstractPodMetadata podMetadata,
            IEnumerable<AbstractFile> instances, AbstractFile model)
        {
            var sequenceInstances = instances.Where(m => m.SequenceIndicator.Equals(model.SequenceIndicator));
            var sequenceMaster = sequenceInstances.GetPreservationOrIntermediateModel();
            if (sequenceMaster == null)
            {
                throw new EmbeddedMetadataException(
                    "No corresponding preservation or preservation-intermediate master present for {0}", model.Filename);
            }

            var defaultProvenance = GetProvenance(podMetadata, sequenceMaster);
            if (defaultProvenance == null)
            {
                throw new EmbeddedMetadataException("No digital file provenance in metadata for {0}",
                    sequenceMaster.Filename);
            }

            return GetProvenance(podMetadata, model, defaultProvenance);
        }

        private static AbstractDigitalFile GetProvenance(AbstractPodMetadata podMetadata, AbstractFile model,
            AbstractDigitalFile defaultValue = null)
        {
            var result =
                podMetadata.FileProvenances.SingleOrDefault(
                    dfp => model.IsSameAs(FileModelFactory.GetModel(dfp.Filename)));
            return result ?? defaultValue;
        }
    }
}