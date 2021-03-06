﻿using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Packager.Models.ResultModels;
using Packager.Utilities;
using Packager.Utilities.ProcessRunners;

namespace Packager.Test.Mocks
{
    public static class MockProcessRunner
    {
        public static IProcessRunner Get(IProcessResult bwfMetaEditResult = null, IProcessResult ffmpegResult = null)
        {
            var runner = Substitute.For<IProcessRunner>();

            if (bwfMetaEditResult == null)
            {
                bwfMetaEditResult = Substitute.For<IProcessResult>();
                bwfMetaEditResult.ExitCode.ReturnsForAnyArgs(0);
            }

            if (ffmpegResult == null)
            {
                ffmpegResult = Substitute.For<IProcessResult>();
                ffmpegResult.ExitCode.ReturnsForAnyArgs(0);
            }

            runner.Run(null, CancellationToken.None).ReturnsForAnyArgs(Task.FromResult(bwfMetaEditResult));
            runner.Run(null, CancellationToken.None).ReturnsForAnyArgs(Task.FromResult(ffmpegResult));
            return runner;
        }
    }
}