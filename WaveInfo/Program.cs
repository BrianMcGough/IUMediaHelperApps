﻿using System;
using System.IO;
using System.Text;

namespace WaveInfo
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            var returnCode = 0;
            var builder = new StringBuilder();
            try
            {
                var waveFile = WaveFileFactory.OpenWaveFile(args[0]);

                Console.Clear();

                WriteFileOverView(builder, waveFile);
                OutputLine(builder);
                WriteDataSizeReport(builder, waveFile);
                OutputLine(builder);
                WriteChunkOverview(builder, waveFile);
                OutputLine(builder);
                WriteChunkReports(builder, waveFile);
                WriteNotes(builder);
                OutputLine(builder);
                WriteReport(args[0], builder);
            }
            catch (Exception e)
            {
                OutputLine(builder, "An {0} exception occurred while attempting to analyze ths file: {1}",
                    e.GetType(), e.Message);
                returnCode = -1;
            }


            Console.WriteLine();
            Console.WriteLine("Press [return] to exit");
            Console.ReadLine();
            return returnCode;
        }

        private static string ToFixedLength(object value)
        {
            return $"{value,-20}";
        }

        private static void WriteReport(string originalPath, StringBuilder builder)
        {
            var info = new FileInfo(originalPath);

            var path = $"{originalPath}.{info.LastWriteTime:yyyy-MM-dd_hh-mm-ss-tt}.report.txt";
            File.WriteAllText(path, builder.ToString());
        }

        private static void OutputLine(StringBuilder builder)
        {
            builder.AppendLine();
            Console.WriteLine();
        }

        private static void WriteChunkReports(StringBuilder builder, WaveFile wavefile)
        {
            foreach (var chunk in wavefile.Chunks)
            {
                OutputLine(builder,chunk.GetReport());
            }
        }

        private static void OutputLine(StringBuilder builder, string baseText, params object[] args)
        {
            var text = string.Format(baseText, args);
            builder.AppendLine(text);
            Console.WriteLine(text);
        }


        private static void WriteFileOverView(StringBuilder builder, WaveFile file)
        {
            OutputLine(builder, "Filename            {0}", file.FileName);
            OutputLine(builder);
            OutputLine(builder, "Size (Windows)      {0}", file.FileSize);

            var riffChunk = file.GetChunk<RiffChunk>();
            if (riffChunk == null)
            {
                OutputLine(builder, "** no riff chunk present!");
                return;
            }

            OutputLine(builder, "Size ({0})         {1}", riffChunk.Id, NormalizeReportedSize(riffChunk.ReportedSize));
            if (IsMaxSize(riffChunk.Size) && riffChunk.Size + 8 != file.FileSize)
            {
                OutputLine(builder, $"                    [not correct: should be {file.FileSize} - 8]");
            }

            var ds64Chunk = file.GetChunk<Ds64Chunk>();
            if (ds64Chunk == null)
            {
                return;
            }

            OutputLine(builder, "Size (ds64)         {0}", ds64Chunk.RiffSize);
            if (Convert.ToInt64(ds64Chunk.RiffSize) + 8 != file.FileSize)
            {
                OutputLine(builder, $"                    [not correct: should be {file.FileSize} - 8]");
            }
        }

        private static void WriteDataSizeReport(StringBuilder builder, WaveFile file)
        {
            var dataChunk = file.GetChunk<DataChunk>();
            if (dataChunk == null)
            {
                OutputLine(builder, "** no data chunk present!");
                return;
            }
            OutputLine(builder, "Data size (data)     {0}", NormalizeReportedSize(dataChunk.ReportedSize));

            var ds64Chunk = file.GetChunk<Ds64Chunk>();
            if (ds64Chunk != null)
            {
                OutputLine(builder, "Data size (ds64)     {0}", ds64Chunk.DataSize);
            }

            OutputLine(builder, "Data MD5 Hash        {0}", dataChunk.Md5Hash);
        }

        private static string NormalizeReportedSize(uint value)
        {
            return IsMaxSize(value) ? "0xFFFFFFFF [1]" : value.ToString();
        }

        private static bool IsMaxSize(uint value)
        {
            return value == 0xFFFFFFFF;
        }

        private static string GetSizeReportText(AbstractChunk chunk)
        {
            var baseformat = chunk is ListChunk
                ? "{0} [3]"
                : "{0}";

            if (chunk.Size == chunk.ReportedSize)
            {
                return string.Format(baseformat, chunk.Size);
            }

            if (IsMaxSize(chunk.ReportedSize))
            {
                return string.Format(baseformat, "0xFFFFFFFF [1]");
            }

            return string.Format(baseformat, $"{chunk.Size} ({chunk.ReportedSize}) [2]");
        }

        private static void WriteChunkOverview(StringBuilder builder, WaveFile file)
        {
            OutputLine(builder, $"{ToFixedLength("Header")}{ToFixedLength("Position")}{ToFixedLength("Size")}");
            OutputLine(builder,
                $"{ToFixedLength(new string('-', 18))}{ToFixedLength(ToFixedLength(new string('-', 18)))}{ToFixedLength(ToFixedLength(new string('-', 18)))}");
            foreach (var chunk in file.Chunks)
            {
                OutputLine(builder, "{0,-20}{1,-20}{2,-20}", chunk.Id, chunk.Offset, GetSizeReportText(chunk));
            }
        }

        private static void WriteNotes(StringBuilder builder)
        {
            if (!NotesPresent(builder))
            {
                return;
            }

            OutputLine(builder, "Notes: ");
            OutputLine(builder);
            OutputLine(builder, "[1]   0xFFFFFFFF indicates that the actual value is in the ds64 chunk.");
            OutputLine(builder, "[2]   The wave standard specifies that sizes should be even numbers.");
            OutputLine(builder, "      In this case, we have to add 1 to the size in order to read the");
            OutputLine(builder, "      chunk correcly.");
            OutputLine(builder, "[3]   The size of the LIST chunk is the sum of the size of its sub-chunks.");
        }

        private static bool NotesPresent(StringBuilder builder)
        {
            return NotePresent(builder, "1") ||
                   NotePresent(builder, "2") ||
                   NotePresent(builder, "3");
        }

        private static bool NotePresent(StringBuilder builder, string note)
        {
            return builder.ToString().Contains($"[{note}]");
        }
    }
}