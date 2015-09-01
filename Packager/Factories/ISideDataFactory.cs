﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Packager.Models.FileModels;
using Packager.Models.OutputModels;
using Packager.Models.PodMetadataModels;

namespace Packager.Factories
{
    public interface ISideDataFactory
    {
        SideData[] Generate(ConsolidatedPodMetadata podMetadata, IEnumerable<ObjectFileModel> filesToProcess);
    }
}