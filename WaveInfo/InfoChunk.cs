﻿using System;
using System.IO;
using System.Text;
using WaveInfo.Extensions;

namespace WaveInfo
{
    public class InfoChunk : AbstractChunk
    {
        public string Text { get; set; }

        public override void ReadChunk(BinaryReader reader)
        {
            base.ReadChunk(reader);

            Text = new string(reader.ReadChars(Convert.ToInt32(Size))).AppendEnd();
        }

        public override string GetReport()
        {
            var builder = new StringBuilder(base.GetReport());
            builder.AppendLine(this.ToColumns("Text"));

            return builder.ToString();
        }
    }
}