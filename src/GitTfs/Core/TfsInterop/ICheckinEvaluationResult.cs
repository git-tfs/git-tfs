namespace GitTfs.Core.TfsInterop
{
    public interface ICheckinEvaluationResult
    {
        ICheckinConflict[] Conflicts { get; }
        ICheckinNoteFailure[] NoteFailures { get; }
        IPolicyFailure[] PolicyFailures { get; }
        Exception PolicyEvaluationException { get; }
    }

    public interface ICheckinConflict
    {
        string ServerItem { get; }
        string Message { get; }
        bool Resolvable { get; }
    }

    public interface ICheckinNoteFailure
    {
        ICheckinNoteFieldDefinition Definition { get; }
        string Message { get; }
    }

    public interface ICheckinNoteFieldDefinition
    {
        string ServerItem { get; }
        string Name { get; }
        bool Required { get; }
        int DisplayOrder { get; }
    }

    public interface IPolicyFailure
    {
        string Message { get; }
    }

    public class TfsPolicyOverrideInfo
    {
        public string Comment { get; set; }
        public IPolicyFailure[] Failures { get; set; }
    }
}
