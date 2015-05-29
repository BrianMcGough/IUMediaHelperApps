﻿using System;
using System.IO;
using Packager.Extensions;

namespace Packager.Models
{
    public class FileModel
    {
        public FileModel()
        {
        }

        public FileModel(FileModel original, string newFileUse, string newExension)
        {
            ProjectCode = original.ProjectCode;
            Extension = original.Extension;
            BarCode = original.BarCode;
            FileUse = newFileUse;
            Extension = newExension;
            OriginalFileName = ToFileName();
            SequenceIndicator = original.SequenceIndicator;
        }

        public FileModel(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            OriginalFileName = Path.GetFileName(path);
            Extension = Path.GetExtension(path).ToDefaultIfEmpty();

            var parts = Path.GetFileNameWithoutExtension(path)
                .ToDefaultIfEmpty()
                .ToLowerInvariant()
                .Split(new[] {'_'}, StringSplitOptions.RemoveEmptyEntries);

            ProjectCode = parts.FromIndex(0, string.Empty).ToLowerInvariant();
            BarCode = parts.FromIndex(1, string.Empty).ToLowerInvariant();
            SequenceIndicator = parts.FromIndex(2, string.Empty).ToLowerInvariant();
            FileUse = parts.FromIndex(3, string.Empty).ToLowerInvariant();
        }

        public string OriginalFileName { get; set; }
        public string ProjectCode { get; set; }
        public string BarCode { get; set; }
        public string SequenceIndicator { get; set; }
        public string FileUse { get; set; }
        public string Extension { get; set; }

        public bool IsValidForGrouping()
        {
            if (string.IsNullOrWhiteSpace(ProjectCode))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(BarCode))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(Extension))
            {
                return false;
            }

            return true;
        }

        public bool IsValidForProcessing()
        {
            if (!IsValidForGrouping())
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(SequenceIndicator))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(FileUse))
            {
                return false;
            }

            return true;
        }

        public string ToFileName()
        {
            return string.Format("{0}_{1}_{2}_{3}{4}", ProjectCode, BarCode, SequenceIndicator, FileUse, Extension);
        }

        public bool BelongsToProject(string projectCode)
        {
            return ProjectCode.Equals(projectCode, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}