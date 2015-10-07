﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Packager.Exceptions;
using Packager.Models.BextModels;
using Packager.Models.FileModels;
using Packager.Models.PodMetadataModels;

namespace Packager.Factories
{
    public class BextMetadataFactory : IBextMetadataFactory
    {
        private const string CodingHistoryLine1Format = "A={0},M={1},T={2},\r\n";
        private const string CodingHistoryLine2Format = "A=PCM,F=96000,W=24,M={0},T={1};A/D,\r\n";
        private const string CodingHistoryLine3 = "A=PCM,F=96000,W=24,M=mono,T=Lynx AES16;DIO";

        private readonly List<string> _knownDigitalFormats = new List<string> { "cd-r", "dat" };
        
        public BextMetadata Generate(ObjectFileModel model, DigitalFileProvenance provenance, ConsolidatedPodMetadata metadata)
        {
            var description = GenerateBextDescription(metadata, model);

            return new BextMetadata
            {
                Originator = metadata.DigitizingEntity,
                OriginatorReference = Path.GetFileNameWithoutExtension(model.ToFileName()),
                Description = description,
                ICMT = description,
                IARL = metadata.Unit,
                OriginationDate = GetDateString(provenance.DateDigitized, "yyyy-MM-dd", ""),
                OriginationTime = GetDateString(provenance.DateDigitized, "HH:mm:ss", ""),
                TimeReference = "0",
                ICRD = GetDateString(provenance.DateDigitized, "yyyy-MM-dd", ""),
                INAM = metadata.Title,
                CodingHistory = GenerateCodingHistory(metadata, provenance)
            };
        }


        private static string GetDateString(DateTime? date, string format, string defaultValue)
        {
            return date.HasValue == false ? defaultValue : date.Value.ToString(format);
        }

        private static string GenerateBextDescription(ConsolidatedPodMetadata metadata, ObjectFileModel fileModel)
        {
            return $"{metadata.Unit}. {metadata.CallNumber}. File use: {fileModel.FullFileUse}. {Path.GetFileNameWithoutExtension(fileModel.ToFileName())}";
        }

        private string GetFormatText(ConsolidatedPodMetadata metadata)
        {
            return _knownDigitalFormats.Contains(metadata.Format.ToLowerInvariant())
                ? "DIGITAL"
                : "ANALOGUE";
        }

        private static string GeneratePlayerTextField(ConsolidatedPodMetadata metadata, DigitalFileProvenance provenance)
        {
            if (!provenance.PlayerDevices.Any())
            {
                throw new BextMetadataException("No player data present in metadata");
            }

            var parts = GenerateDevicePartsArray(provenance.PlayerDevices);

            parts.Add(DetermineSpeedUsed(metadata, provenance));
            parts.Add(metadata.Format);

            return string.Join(";", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
        }

        private static List<string> GenerateDevicePartsArray(IEnumerable<Device> devices)
        {
            var parts = new List<string>();

            foreach (var device in devices)
            {
                parts.Add($"{device.Manufacturer} {device.Model}");
                parts.Add($"SN{device.SerialNumber}");
            }

            return parts;
        }

        private static string GenerateAdTextField(DigitalFileProvenance provenance)
        {
            if (!provenance.AdDevices.Any())
            {
                throw new BextMetadataException("No AD data present in metadata");
            }

            var parts = GenerateDevicePartsArray(provenance.AdDevices);
            return string.Join(";", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
        }

        private string GenerateCodingHistory(ConsolidatedPodMetadata metadata, DigitalFileProvenance provenance)
        {
            var builder = new StringBuilder();

            builder.AppendFormat(CodingHistoryLine1Format,
                GetFormatText(metadata),
                metadata.SoundField,
                GeneratePlayerTextField(metadata, provenance));

            builder.AppendFormat(CodingHistoryLine2Format,
                metadata.SoundField,
                GenerateAdTextField(provenance));

            builder.Append(CodingHistoryLine3);

            return builder.ToString();
        }

        // if provenance.speedUsed is present
        // otherwise use metadata.playback speed
        // finally, replace comma delimiters with semi-colons and remove spaces
        private static string DetermineSpeedUsed(ConsolidatedPodMetadata metadata, DigitalFileProvenance provenance)
        {
            var result = string.IsNullOrWhiteSpace(provenance.SpeedUsed)
                ? metadata.PlaybackSpeed
                : provenance.SpeedUsed;

            if (string.IsNullOrWhiteSpace(result))
            {
                return result;
            }

            return result.Replace(",", ";").Replace("; ", ";");
        }
    }
}