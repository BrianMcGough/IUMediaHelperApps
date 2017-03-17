﻿using System;
using System.Globalization;
using System.IO;
using Packager.Extensions;

namespace Packager.Models.FileModels
{
    public abstract class AbstractFile
    {
        private string _projectCode;

        protected AbstractFile()
        {
        }

        protected AbstractFile(AbstractFile original, string fileUse, string fullFileUse, string extension)
        {
            BarCode = original.BarCode;
            SequenceIndicator = original.SequenceIndicator;
            ProjectCode = original.ProjectCode;
            Extension = extension;
            FileUse = fileUse;
            FullFileUse = fullFileUse;
            Filename = NormalizeFilename();
            PlaceHolder = original.PlaceHolder;
            OriginalFileName = IsSameAs(original)
                ? original.OriginalFileName
                : Filename;
        }

        public abstract int Precedence { get; }

        public string OriginalFileName { get; protected set; }

        public string ProjectCode
        {
            get
            {
                return _projectCode.IsNotSet()
                    ? string.Empty
                    : _projectCode.ToUpperInvariant();
            }
            protected set { _projectCode = value; }
        }

        public int SequenceIndicator { get; protected set; }

        public string FileUse { get; protected set; }
        public string FullFileUse { get; protected set; }
        public string Checksum { get; set; }
        public string BarCode { get; protected set; }
        public string Extension { get; protected set; }
        public string Filename { get; protected set; }
        public bool PlaceHolder { get; protected set; }
        
        public string GetFolderName()
        {
            return $"{ProjectCode}_{BarCode}";
        }

        public bool BelongsToProject(string projectCode)
        {
            return ProjectCode.Equals(projectCode, StringComparison.InvariantCultureIgnoreCase);
        }

        private string NormalizeFilename()
        {
            var parts = new[]
            {
                ProjectCode,
                BarCode,
                SequenceIndicator.ToString("D2", CultureInfo.InvariantCulture),
                FileUse
            };

            return string.Join("_", parts) + Extension;
        }

        protected static int GetSequenceIndicator(string value)
        {
            int result;
            return int.TryParse(value, out result)
                ? result
                : 0;
        }

        public virtual bool IsValid()
        {
            if (ProjectCode.IsNotSet())
            {
                return false;
            }

            if (BarCode.IsNotSet())
            {
                return false;
            }

            if (Extension.IsNotSet())
            {
                return false;
            }

            if (SequenceIndicator < 1)
            {
                return false;
            }

            if (FileUse.IsNotSet())
            {
                return false;
            }

            return true;
        }

        public bool IsSameAs(AbstractFile model)
        {
            if (!model.ProjectCode.Equals(ProjectCode, StringComparison.InvariantCultureIgnoreCase))
            {
                return false;
            }

            if (!model.BarCode.Equals(BarCode, StringComparison.InvariantCultureIgnoreCase))
            {
                return false;
            }

            if (model.SequenceIndicator != SequenceIndicator)
            {
                return false;
            }

            if (!model.FileUse.Equals(FileUse, StringComparison.InvariantCultureIgnoreCase))
            {
                return false;
            }

            if (!model.Extension.Equals(Extension, StringComparison.InvariantCultureIgnoreCase))
            {
                return false;
            }

            return true;
        }

        public string ToFrameMd5Filename()
        {
            return $"{Filename}.framemd5";
        }

        public string GetOriginalFolderName()
        {
            return Path.Combine(GetFolderName(), "Originals");
        }
    }
}