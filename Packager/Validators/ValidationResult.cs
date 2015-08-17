namespace Packager.Validators
{
    public class ValidationResult
    {
        public ValidationResult(string baseIssue, params object[] args)
            : this(false, string.Format(baseIssue, args))
        {
        }

        private ValidationResult(bool success, string issue)
        {
            Issue = issue;
            Result = success;
        }

        public static ValidationResult Success
        {
            get { return new ValidationResult(true, ""); }
        }

        public bool Result { get; set; }
        public string Issue { get; set; }
    }
}