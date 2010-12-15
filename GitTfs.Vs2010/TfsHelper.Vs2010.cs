using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.Win32;
using Sep.Git.Tfs.Core.TfsInterop;
using Sep.Git.Tfs.Vs2010;

namespace Sep.Git.Tfs.VsCommon
{
    public partial class TfsHelper : ITfsHelper
    {
        private TfsTeamProjectCollection server;

        public string TfsClientLibraryVersion
        {
            get { return typeof(TfsTeamProjectCollection).Assembly.GetName().Version.ToString() + " (MS)"; }
        }

        private void UpdateServer()
        {
            if (string.IsNullOrEmpty(Url))
            {
                server = null;
            }
            else
            {
                server = new TfsTeamProjectCollection(new Uri(Url), new UICredentialsProvider());
                server.EnsureAuthenticated();
            }
        }

        private TfsTeamProjectCollection Server
        {
            get
            {
                return server;
            }
        }

        public bool ShowCheckinDialog(IWorkspace workspace, IPendingChange[] pendingChanges, 
            IEnumerable<IWorkItemCheckedInfo> checkedInfos, string checkinComment)
        {
            return ShowCheckinDialog(_bridge.Unwrap<Workspace>(workspace),
                                     pendingChanges.Select(p => _bridge.Unwrap<PendingChange>(p)).ToArray(),
                                     checkedInfos.Select(c => _bridge.Unwrap<WorkItemCheckedInfo>(c)).ToArray(),
                                     checkinComment);
        }

        private static bool ShowCheckinDialog(Workspace workspace, PendingChange[] pendingChanges, 
            WorkItemCheckedInfo[] checkedInfos, string checkinComment)
        {
            var result = true;

            using (var parentForm = new ParentForm())
            {
                parentForm.Show();

                dynamic dialog = new ReflectionProxy(GetCheckinDialogType(), workspace.VersionControlServer);

                int dialogResult = dialog.Show(parentForm.Handle, workspace, pendingChanges, pendingChanges,
                                               checkinComment, null, null, checkedInfos);

                if (dialogResult <= 0)
                {
                    result = false;
                }

                parentForm.Close();
            }

            return result;
        }

        private const string DialogAssemblyName = "Microsoft.TeamFoundation.VersionControl.ControlAdapter";

        private static Type GetCheckinDialogType()
        {
            return GetDialogAssembly().GetType(DialogAssemblyName + ".CheckinDialog");
        }

        private static Assembly GetDialogAssembly()
        {
            return Assembly.LoadFrom(GetDialogAssemblyPath());
        }

        private static string GetDialogAssemblyPath()
        {
            return Path.Combine(GetVs2010InstallDir(), "PrivateAssemblies", DialogAssemblyName + ".dll");
        }

        private static string GetVs2010InstallDir()
        {
            return Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\VisualStudio\10.0").GetValue("InstallDir").ToString();
        }
    }
}
