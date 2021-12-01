using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace GitTfs.Core
{
    public class GitTfsGatedCheckinException : GitTfsException
    {
        public string ShelvesetName { get; }
        public ReadOnlyCollection<KeyValuePair<string, Uri>> AffectedBuildDefinitions { get; set; }
        public string CheckInTicket { get; }

        public GitTfsGatedCheckinException(string shelvesetName, ReadOnlyCollection<KeyValuePair<string, Uri>> affectedBuildDefinitions, string checkInTicket)
            : base("Gated checkin detected!")
        {
            ShelvesetName = shelvesetName;
            AffectedBuildDefinitions = affectedBuildDefinitions;
            CheckInTicket = checkInTicket;
        }
    }
}