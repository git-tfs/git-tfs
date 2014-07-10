using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.Win32;
using Sep.Git.Tfs.Core;
using Sep.Git.Tfs.Core.TfsInterop;
using Sep.Git.Tfs.Util;
using Sep.Git.Tfs.VsCommon;
using StructureMap;

namespace Sep.Git.Tfs.Vs2010
{
    public class TfsHelper : TfsHelperBase
    {
        private readonly TfsApiBridge _bridge;

        public TfsHelper(TextWriter stdout, TfsApiBridge bridge, IContainer container) : base(stdout, bridge, container)
        {
            _bridge = bridge;
        }

        public override string TfsClientLibraryVersion
        {
            get { return typeof(TfsTeamProjectCollection).Assembly.GetName().Version + " (MS)"; }
        }

        public override void EnsureAuthenticated()
        {
            if (string.IsNullOrEmpty(Url))
            {
                _server = null;
            }
            else
            {
                Uri uri;
                if (!Uri.IsWellFormedUriString(Url, UriKind.Absolute))
                {
                    // maybe it is not an Uri but instance name
                    var servers = RegisteredTfsConnections.GetConfigurationServers();
                    var registered = servers.FirstOrDefault(s => String.Compare(s.Name, Url, StringComparison.OrdinalIgnoreCase) == 0);
                    if (registered == null)
                        throw new GitTfsException("Given tfs name is not correct URI and not found as a registered TFS instance");
                    uri = registered.Uri;
                }
                else
                {
                    uri = new Uri(Url);
                }

                _server = HasCredentials ?
                    new TfsTeamProjectCollection(uri, GetCredential(), new UICredentialsProvider()) :
                    new TfsTeamProjectCollection(uri, new UICredentialsProvider());

                _server.EnsureAuthenticated();
            }
        }

        protected override T GetService<T>()
        {
            if (_server == null) EnsureAuthenticated();
            return (T)_server.GetService(typeof(T));
        }

        protected override string GetAuthenticatedUser()
        {
            return VersionControl.AuthorizedUser;
        }

        private string vsInstallDir;

        protected override string GetVsInstallDir()
        {
            if (vsInstallDir == null)
            {
                vsInstallDir = TryGetRegString(@"Software\Microsoft\VisualStudio\10.0", "InstallDir")
                    ?? TryGetRegString(@"Software\WOW6432Node\Microsoft\VisualStudio\10.0", "InstallDir");
            }
            return vsInstallDir;
        }

        private static string TryGetRegString(string path, string name)
        {
            try
            {
                Trace.WriteLine("Trying to get " + path + "|" + name);
                var key = Registry.LocalMachine.OpenSubKey(path);
                if(key != null)
                {
                    return key.GetValue(name) as string;
                }
            }
            catch(Exception e)
            {
                Trace.WriteLine("Unable to get registry value " + path + "|" + name + ": " + e);
            }
            return null;
        }
    }

    public class ItemDownloadStrategy : IItemDownloadStrategy
    {
        private readonly TfsApiBridge _bridge;

        public ItemDownloadStrategy(TfsApiBridge bridge)
        {
            _bridge = bridge;
        }

        public TemporaryFile DownloadFile(IItem item)
        {
            var temp = new TemporaryFile();
            try
            {
                _bridge.Unwrap<Item>(item).DownloadFile(temp);
                return temp;
            }
            catch (Exception)
            {
                Trace.WriteLine(String.Format("Something went wrong downloading \"{0}\" in changeset {1}", item.ServerItem, item.ChangesetId));
                temp.Dispose();
                throw;
            }
        }
    }
}
