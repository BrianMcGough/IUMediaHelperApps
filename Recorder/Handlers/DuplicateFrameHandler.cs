using System.Diagnostics;
using System.Text.RegularExpressions;
using Recorder.ViewModels;

namespace Recorder.Handlers
{
    public class DuplicateFrameHandler:AbstractProcessOutputHandler
    {
        private readonly FrameStatsViewModel _viewModel;
        private readonly Regex _currentFrameExpression = new Regex(@"dup=(\d+)");

        public DuplicateFrameHandler(FrameStatsViewModel viewModel)
        {
            _viewModel = viewModel;
        }

        public override void OnDataReceived(object sender, DataReceivedEventArgs args)
        {
            if (string.IsNullOrWhiteSpace(args.Data))
            {
                return;
            }

            var match = _currentFrameExpression.Match(args.Data);
            if (match.Success == false)
            {
                return;
            }

            if (match.Groups.Count < 2)
            {
                return;
            }

            _viewModel.DuplicateFrames = match.Groups[1].Value.Trim();
        }

        public override void Reset()
        {
            _viewModel.DuplicateFrames = "0";
        }
    }
}