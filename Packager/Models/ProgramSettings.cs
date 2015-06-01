﻿using System.Collections.Specialized;
using System.IO;

namespace Packager.Models
{
    public interface IProgramSettings
    {
        // ReSharper disable once InconsistentNaming
        string BWFMetaEditPath { get; }
        // ReSharper disable once InconsistentNaming
        string FFMPEGPath { get; }
        string InputDirectory { get; }
        string ProcessingDirectory { get; }
        // ReSharper disable once InconsistentNaming
        string FFMPEGAudioMezzanineArguments { get; }
        // ReSharper disable once InconsistentNaming
        string FFMPEGAudioAccessArguments { get; }
        string ProjectCode { get; }
        string DropBoxDirectoryName { get; }
        string DateFormat { get; }
        void Verify();
    }

    public class ProgramSettings : IProgramSettings
    {
        public ProgramSettings(NameValueCollection settings)
        {
            InputDirectory = settings["WhereStaffWorkDirectoryName"];
            ProcessingDirectory = settings["ProcessingDirectoryName"];
            FFMPEGPath = settings["PathToFFMpeg"];
            BWFMetaEditPath = settings["PathToMetaEdit"];
            FFMPEGAudioMezzanineArguments = settings["ffmpegAudioMezzanineArguments"];
            FFMPEGAudioAccessArguments = settings["ffmpegAudioAccessArguments"];
            ProjectCode = settings["ProjectCode"];
            DropBoxDirectoryName = settings["DropBoxDirectoryName"];
        }

        public string BWFMetaEditPath { get; private set; }
        public string FFMPEGPath { get; private set; }
        public string InputDirectory { get; private set; }
        public string ProcessingDirectory { get; private set; }
        public string FFMPEGAudioMezzanineArguments { get; private set; }
        public string FFMPEGAudioAccessArguments { get; private set; }
        public string ProjectCode { get; private set; }
        public string DropBoxDirectoryName { get; private set; }

        public string DateFormat
        {
            get { return "yyyy-MM-dd"; }
        }


        public void Verify()
        {
            if (!Directory.Exists(InputDirectory))
            {
                throw new DirectoryNotFoundException(InputDirectory);
            }

            if (!Directory.Exists(ProcessingDirectory))
            {
                throw new DirectoryNotFoundException(ProcessingDirectory);
            }

            if (!File.Exists(FFMPEGPath))
            {
                throw new FileNotFoundException(FFMPEGPath);
            }

            if (!File.Exists(BWFMetaEditPath))
            {
                throw new FileNotFoundException(BWFMetaEditPath);
            }

            if (!Directory.Exists(DropBoxDirectoryName))
            {
                throw new DirectoryNotFoundException(DropBoxDirectoryName);
            }
        }
    }
}