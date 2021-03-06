using System;
using System.Linq;
using System.Reflection;
using Packager.Attributes;
using Packager.Exceptions;
using Packager.Extensions;
using Packager.Utilities.Bext;
using Packager.Utilities.ProcessRunners;

namespace Packager.Models.EmbeddedMetadataModels
{
    public class EmbeddedAudioMetadata : AbstractEmbeddedMetadata
    {
        [BextField(BextFields.Description, 256)]
        public string Description { get; set; }

        [BextField(BextFields.Originator, 32)]
        public string Originator { get; set; }

        [BextField(BextFields.OriginatorReference, 32)]
        public string OriginatorReference { get; set; }

        [BextField(BextFields.OriginationDate, 10)]
        public string OriginationDate { get; set; }

        [BextField(BextFields.OriginationTime, 8)]
        public string OriginationTime { get; set; }

        [BextField(BextFields.TimeReference)]
        public string TimeReference { get; set; }

        [BextField(BextFields.UMID)]
        public string UMID { get; set; }

        [BextField(BextFields.History)]
        public string CodingHistory { get; set; }

        [BextField(BextFields.IARL)]
        public string IARL { get; set; }

        [BextField(BextFields.IART)]
        public string IART { get; set; }

        [BextField(BextFields.ICMS)]
        public string ICMS { get; set; }

        [BextField(BextFields.ICMT)]
        public string ICMT { get; set; }

        [BextField(BextFields.ICOP)]
        public string ICOP { get; set; }

        [BextField(BextFields.ICRD)]
        public string ICRD { get; set; }

        [BextField(BextFields.IENG)]
        public string IENG { get; set; }

        [BextField(BextFields.IGNR)]
        public string IGNR { get; set; }

        [BextField(BextFields.IKEY)]
        public string IKEY { get; set; }

        [BextField(BextFields.IMED)]
        public string IMED { get; set; }

        [BextField(BextFields.INAM)]
        public string INAM { get; set; }

        [BextField(BextFields.IPRD)]
        public string IPRD { get; set; }

        [BextField(BextFields.ISBJ)]
        public string ISBJ { get; set; }

        [BextField(BextFields.ISFT)]
        public string ISFT { get; set; }

        [BextField(BextFields.ISRC)]
        public string ISRC { get; set; }

        [BextField(BextFields.ISRF)]
        public string ISRF { get; set; }

        [BextField(BextFields.ITCH)]
        public string ITCH { get; set; }

        public override ArgumentBuilder AsArguments()
        {
            var arguments = new ArgumentBuilder();

            foreach (var info in GetType().GetProperties()
                .Select(
                    p =>
                        new Tuple<string, BextFieldAttribute>(GetValueFromField(this, p),
                            p.GetCustomAttribute<BextFieldAttribute>()))
                .Where(t => t.Item2 != null && t.Item1.IsSet()))
            {
                if (info.Item2.ValueWithinLengthLimit(info.Item1) == false)
                {
                    throw new EmbeddedMetadataException(
                        "Value for bext field {0} ('{1}') exceeds maximum length ({2})", info.Item2.Field, info.Item1,
                        info.Item2.MaxLength);
                }

                arguments.Add(
                    $"-metadata {info.Item2.GetFFMPEGArgument()}={info.Item1.NormalizeForCommandLine().ToQuoted()}");
            }

            return arguments;
        }

        private static string GetValueFromField(EmbeddedAudioMetadata core, PropertyInfo info)
        {
            return info.GetValue(core).ToDefaultIfEmpty();
        }
    }
}