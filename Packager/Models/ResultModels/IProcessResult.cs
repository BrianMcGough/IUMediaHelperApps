using System.Diagnostics;
using Packager.Utilities;
using Packager.Utilities.Process;

namespace Packager.Models.ResultModels
{
    public interface IProcessResult
    {
        int ExitCode { get; set; }

        IOutputBuffer StandardOutput { get; }

        IOutputBuffer StandardError { get; }
        
        ProcessStartInfo StartInfo { get; set; }
    }
}