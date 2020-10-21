namespace System
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public sealed class GitVersionAttribute : Attribute
    {
        public string CommitId { get; }

        public GitVersionAttribute(string commit)
        {
            CommitId = commit;
        }
    }
}
