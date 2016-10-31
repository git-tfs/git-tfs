using System;
using System.Diagnostics;
using WixSharp;
using System.IO;

namespace GitTfs.Setup
{
    class Program
    {
        static void TxtToRtf(string inputFile, string outputFile)
        {
            var lines = System.IO.File.ReadAllLines(inputFile);

            using (var f = new StreamWriter(outputFile))
            {
                f.WriteLine(@"{\rtf1\ansi\deff0{\fonttbl{\f0\fnil\fcharset0 Arial;}}\nowwrap\fs18");
                foreach (var l in lines)
                {
                    f.WriteLine(l);
                    f.WriteLine(@"\line ");
                }
                f.WriteLine(@"}");
            }
        }

        static void Main()
        {
            string ProjectName;
            Guid GUID;
            string Conf;
#if DEBUG
            var IsDebug = true;
#else
            var IsDebug = false;
#endif
            if (IsDebug)
            {
                ProjectName = "GitTfsDebug";
                GUID = new Guid("6FE30B47-2577-43AD-9095-1861BA25889B");
                Conf = "Debug";
            }
            else
            {
                ProjectName = "GitTfs";
                GUID = new Guid("98823CC2-0C2E-4CF7-B5ED-EA2DD26559BF");
                Conf = "Release";
            }
            var SrcDir = @"..\GitTfs\bin\" + Conf;
            var project = new Project(ProjectName,
                             new Dir(@"%ProgramFiles%\GitTfs",
                                 new Files("*.*", f => !f.EndsWith(".pdb")
                                                    && !f.EndsWith(".xml")
                                                    && !f.EndsWith(".rtf"))),
                             new EnvironmentVariable("PATH", "[INSTALLDIR]") { Part = EnvVarPart.last });


            project.UI = WUI.WixUI_InstallDir;
            project.GUID = GUID;
            project.SourceBaseDir = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, SrcDir));

            FileVersionInfo fileinfo =
                FileVersionInfo.GetVersionInfo(Path.Combine(project.SourceBaseDir, "git-tfs.exe"));
            project.Version = new Version(fileinfo.FileMajorPart, fileinfo.FileMinorPart, fileinfo.FileBuildPart, fileinfo.FilePrivatePart);
            project.OutFileName = ProjectName + "-" + project.Version.ToString();

            TxtToRtf(Path.Combine(project.SourceBaseDir, "LICENSE"), Path.Combine(project.SourceBaseDir, "LICENSE.rtf"));
            project.LicenceFile = @"LICENSE.rtf";

            project.ControlPanelInfo.Manufacturer = "SEP";
            //project.ControlPanelInfo.ProductIcon = "GitTfs.ico";
            project.ControlPanelInfo.Comments = "A Git/TFS bridge, similar to git-svn";
            project.ControlPanelInfo.HelpLink = "http://git-tfs.com/";
            project.ControlPanelInfo.UrlUpdateInfo = "https://github.com/git-tfs/git-tfs/releases";

            project.MajorUpgrade = new MajorUpgrade
            {
                Schedule = UpgradeSchedule.afterInstallInitialize,
                DowngradeErrorMessage = "A later version of GitTfs is already installed. Setup will now exit."
            };

            project.BuildMsi();
        }
    }
}
