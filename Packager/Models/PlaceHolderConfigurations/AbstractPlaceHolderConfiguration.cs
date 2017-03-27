using System.Collections.Generic;
using System.Linq;
using Common.Models;
using Packager.Extensions;
using Packager.Factories;
using Packager.Models.FileModels;

namespace Packager.Models.PlaceHolderConfigurations
{
    public abstract class AbstractPlaceHolderConfiguration : IPlaceHolderConfiguration
    {
        private FileModelFactory.UsageApplications Application { get; }
        private List<FileUsages> RequiredUsages { get; }

        protected AbstractPlaceHolderConfiguration(List<FileUsages> requiredUsages, FileModelFactory.UsageApplications application)
        {
            Application = application;
            RequiredUsages = requiredUsages;
        }

        public List<AbstractFile> GetPlaceHoldersToAdd(List<AbstractFile> fileModels)
        {
            var sequencesRequiringPlaceHolders = GetSequencesRequiringPlaceHolders(fileModels);
            if (sequencesRequiringPlaceHolders.Any() == false)
            {
                return new List<AbstractFile>();
            }

            var projectCode = GetProjectCode(fileModels);
            var barcode = GetBarCode(fileModels);

            var result = new List<AbstractFile>();
            return sequencesRequiringPlaceHolders
                .Aggregate(result, (current, sequence) =>
                    current.Concat(
                            GetPlaceHolders(projectCode, barcode, sequence))
                        .ToList());
        }

        private IEnumerable<AbstractFile> GetPlaceHolders(string projectCode, string barcode, int sequence)
        {
            var template = new PlaceHolderFile(projectCode, barcode, sequence);
            return RequiredUsages.Select(usage => GetPlaceHolder(template, usage))
                .ToList();
        }

        private List<int> GetSequencesRequiringPlaceHolders(IEnumerable<AbstractFile> fileModels)
        {
            return fileModels.GroupBy(fileModel => fileModel.SequenceIndicator)
                .Where(grouping => AnyRequiredUsagesPresent(grouping) == false)
                .Select(groupting => groupting.Key)
                .ToList();
        }

        private bool AnyRequiredUsagesPresent(IGrouping<int, AbstractFile> grouping)
        {
            return RequiredUsages.Any(
                usage => grouping.Any(fileModel => fileModel.FileUsage == usage));
        }

        private static string GetProjectCode(IEnumerable<AbstractFile> fileModels)
        {
            var firstPresFile = fileModels.FirstOrDefault(f => f.IsPreservationVersion());
            return firstPresFile?.ProjectCode;
        }

        private static string GetBarCode(IEnumerable<AbstractFile> fileModels)
        {
            var firstPresFile = fileModels.FirstOrDefault(f => f.IsPreservationVersion());
            return firstPresFile?.BarCode;
        }

        private AbstractFile GetPlaceHolder(PlaceHolderFile template, FileUsages usage)
        {
            return FileModelFactory.ConvertTo(template, usage, Application);
        }
    }
}