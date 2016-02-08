namespace Packager.Models.FileModels
{
    public class AccessFile : AbstractFile
    {
        private const string FileUseValue = "access";
        private const string FullFileUseValue = "Access File Version";
        private const string ExtensionValue = ".mp4";

        public AccessFile(AbstractFile original) : 
            base(original, FileUseValue, FullFileUseValue, ExtensionValue)
        {
        }
    }
}