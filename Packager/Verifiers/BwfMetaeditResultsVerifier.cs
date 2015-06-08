﻿using System.Collections.Generic;
using System.IO;
using Packager.Extensions;
using Packager.Observers;

namespace Packager.Verifiers
{
    public class BwfMetaEditResultsVerifier : IVerifier
    {
        public BwfMetaEditResultsVerifier(string output, List<string> targetPaths, List<IObserver> observers)
        {
            Output = output;
            TargetPaths = targetPaths;
            Observers = observers;
        }

        private string Output { get; set; }
        private List<string> TargetPaths { get; set; }
        private List<IObserver> Observers { get; set; }

        public bool Verify()
        {
            var hasError = false;
            foreach (var path in TargetPaths)
            {
                var fileName = Path.GetFileName(path);
                if (IsModifiedOrNothingToDo(Output, path))
                {
                    Observers.Log("Successfully added metadata to {0}", fileName);
                }
                else
                {
                    Observers.Log("Could not add metadata to {0}", fileName);
                    hasError = true;
                }
            }

            return hasError == false;
        }

        private static bool IsModifiedOrNothingToDo(string output, string path)
        {
            var modified = string.Format("{0}: is modified", path);
            var nothingToDo = string.Format("{0}: nothing to do", path);
            return output.Contains(modified) || output.Contains(nothingToDo);
        }
    }
}