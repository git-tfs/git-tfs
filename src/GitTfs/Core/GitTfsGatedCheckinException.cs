using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Sep.Git.Tfs.Core
{
    public class GitTfsGatedCheckinException : GitTfsException
    {
        public string ShelvesetName { get; set; }
        public ReadOnlyCollection<KeyValuePair<string, Uri>> AffectedBuildDefinitions { get; set; }
        public string CheckInTicket { get; set; }

        public GitTfsGatedCheckinException(string shelvesetName, ReadOnlyCollection<KeyValuePair<string, Uri>> affectedBuildDefinitions, string checkInTicket)
            : base("Gated checkin detected!")
        {
            ShelvesetName = shelvesetName;
            AffectedBuildDefinitions = affectedBuildDefinitions;
            CheckInTicket = checkInTicket;
        }
    }
}