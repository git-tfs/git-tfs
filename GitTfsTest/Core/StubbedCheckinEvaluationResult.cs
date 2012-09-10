using System;
using System.Collections.Generic;
using System.Linq;
using Sep.Git.Tfs.Core.TfsInterop;

namespace Sep.Git.Tfs.Test.Core
{
    public class StubbedCheckinEvaluationResult : ICheckinEvaluationResult
    {
        private HashSet<ICheckinConflict> conflicts;
        private HashSet<ICheckinNoteFailure> noteFailures;
        private HashSet<IPolicyFailure> policyFailures;

        public StubbedCheckinEvaluationResult()
        {
            conflicts = new HashSet<ICheckinConflict>();
            noteFailures = new HashSet<ICheckinNoteFailure>();
            policyFailures = new HashSet<IPolicyFailure>();
        }

        public ICheckinConflict[] Conflicts { get { return conflicts.ToArray(); } }

        public ICheckinNoteFailure[] NoteFailures { get { return noteFailures.ToArray(); } }

        public IPolicyFailure[] PolicyFailures { get { return policyFailures.ToArray(); } }

        public Exception PolicyEvaluationException { get; set; }

        public StubbedCheckinEvaluationResult WithCheckinConflict(string serverItem, string message, bool? resolvable)
        {
            conflicts.Add(new StubbedCheckinConflict(serverItem, message, resolvable ?? false));
            return this;
        }

        public StubbedCheckinEvaluationResult WithNoteFailure(string serverItem, string name, bool? required, int? displayOrder, string message)
        {
            noteFailures.Add(new StubbedCheckinNoteFailure(
                                 serverItem ?? string.Empty,
                                 name ?? string.Empty,
                                 required ?? false,
                                 displayOrder ?? 0,
                                 message ?? string.Empty));
            return this;
        }

        public StubbedCheckinEvaluationResult WithPoilicyFailure(string message)
        {
            policyFailures.Add(new StubbedPolicyFailure { Message = message ?? string.Empty });
            return this;
        }

        public StubbedCheckinEvaluationResult WithException(Exception ex)
        {
            this.PolicyEvaluationException = ex;
            return this;
        }

        public StubbedCheckinEvaluationResult WithException(string message)
        {
            return this.WithException(new Exception(message ?? string.Empty));
        }
    }

    public class StubbedCheckinConflict : ICheckinConflict
    {
        public StubbedCheckinConflict(string serverItem, string message, bool resolvable)
        {
            ServerItem = serverItem;
            Message = message;
            Resolvable = resolvable;
        }

        public string ServerItem { get; set; }

        public string Message { get; set; }

        public bool Resolvable { get; set; }
    }

    public class StubbedCheckinNoteFailure : ICheckinNoteFailure
    {
        public StubbedCheckinNoteFailure(string serverItem, string name, bool required, int displayOrder, string message)
        {
            Definition = new StubbedCheckinNoteFieldDefinition(serverItem, name, required, displayOrder);
            Message = message;
        }

        public ICheckinNoteFieldDefinition Definition { get; set; }

        public string Message { get; set; }
    }

    public class StubbedCheckinNoteFieldDefinition : ICheckinNoteFieldDefinition
    {
        public StubbedCheckinNoteFieldDefinition(string serverItem, string name, bool required, int displayOrder)
        {
            ServerItem = serverItem;
            Name = name;
            Required = required;
            DisplayOrder = displayOrder;
        }

        public string ServerItem { get; set; }

        public string Name { get; set; }

        public bool Required { get; set; }

        public int DisplayOrder { get; set; }
    }

    public class StubbedPolicyFailure : IPolicyFailure
    {
        public string Message { get; set; }
    }
}
