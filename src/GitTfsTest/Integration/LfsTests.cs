//
// used CloneTests.cs as a template

using GitTfs.Core.TfsInterop;
using Xunit;
using Xunit.Abstractions;
using System.Diagnostics;

namespace GitTfs.Test.Integration
{
    //NOTE: All timestamps in these tests must specify a time zone. If they don't, the local time zone will be used in the DateTime,
    //      but the commit timestamp will use the ToUniversalTime() version of the DateTime.
    //      This will cause the hashes to differ on computers in different time zones.
    public class LfsTests : BaseTest, IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly IntegrationHelper h;

        public LfsTests(ITestOutputHelper output)
        {
            _output = output;
            h = new IntegrationHelper();
            _output.WriteLine("Repository in folder: " + h.Workdir);
        }

        public void Dispose() => h.Dispose();

        private readonly string[] RefsInNewClone = new[] { "HEAD", "refs/heads/master", "refs/remotes/tfs/default" };

        /// <summary>
        /// Verify repo layout.
        /// The tree verifies the correctness of the filenames and contents.
        /// The commit verifies the correctness of the commit message, author, and date, too.
        /// </summary>
        /// <param name="repodir">The repo dir.</param>
        /// <param name="refs">Refs to inspect</param>
        /// <param name="commit">(optional) The expected commit sha.</param>
        /// <param name="tree">(optional) The expected tree sha.</param>
        private void AssertNewClone(string repodir, string[] refs, string commit = null, string tree = null)
        {
            const string format = "{0}: {1} / {2}";
            var expected = string.Join("\n", refs.Select(gitref => string.Format(format, gitref, commit, tree)));
            var actual = string.Join("\n", refs.Select(gitref =>
            {
                var actualCommit = h.RevParseCommit(repodir, gitref);
                return string.Format(format, gitref,
                    commit == null || actualCommit == null ? null : actualCommit.Sha,
                    tree == null || actualCommit == null ? null : actualCommit.Tree.Sha);
            }));
            Assert.Equal(expected, actual);
        }

        /// <summary>
        /// Generate LFS pointer file contents into a string for a given (rather large) file.
        /// It will run the command 'git-lfs pointer' on the given file and capture stdout
        /// </summary>
        /// <param name="repodir">The repo dir.</param>
        /// <param name="relFilePAth">path of the file relative to the repository root</param>
        /// <returns>The so-called LFS pointer file, containing the unique oid identifying the actual binary file</returns>
        private string GetLfsPointer(string repodir, string relFilePAth)
        {
            Process process = new Process();
            process.StartInfo.FileName = "git-lfs";
            process.StartInfo.Arguments = $"pointer --file={relFilePAth}";
            process.StartInfo.WorkingDirectory = h.Repository(repodir).Info.WorkingDirectory;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.Start();

            StreamReader sr = process.StandardOutput;
            string output = sr.ReadToEnd();
            process.WaitForExit();
            return output;
        }

        /// <summary>
        /// This test will clone from a TFS repository which contains a .gitattributes file in C2,
        /// validating that git-LFS will be activated when C3, containing files which should be tracked by LFS
        /// is being commited to the git repo
        /// 
        /// NOTE: git-LFS is not yet active before fetching C3, at the time of writing this test, providing an initial
        /// .gittatributes file is not yet supported.
        /// </summary>
        [FactExceptOnUnix]
        public void CloneWithGitattributesInEarlyChangeset()
        {
            // .gittatributes LFS filter entry to track *.png files:
            string contentGitAttributes = "*.png filter=lfs diff=lfs merge=lfs -text\r\n";
            h.SetupFake(r =>
            {
                r.Changeset(1, "Project created from template", DateTime.Parse("2012-01-01 12:12:10 -05:00"))
                    .Change(TfsChangeType.Add, TfsItemType.Folder, "$/MyProject");
                r.Changeset(2, "Second TFS changeset: add a .gitattributes file with an entry for LFS-tracking *.png files", DateTime.Parse("2012-01-02 12:12:12 -05:00"))
                    .Change(TfsChangeType.Add, TfsItemType.File, "$/MyProject/.gitattributes", contentGitAttributes);
                r.Changeset(3, "Third TFS changeset: add two binary, LFS-tracked *.png files", DateTime.Parse("2012-01-02 12:12:15 -05:00"))
                    .ChangeFromResource(TfsChangeType.Add, TfsItemType.File, "$/MyProject/Some_Picture_A.png", h.Workdir, "GitTfs.Test.Integration.LfsCandidates.Some_Picture_A.png")
                    .ChangeFromResource(TfsChangeType.Add, TfsItemType.File, "$/MyProject/Some_Picture_B.png", h.Workdir, "GitTfs.Test.Integration.LfsCandidates.Some_Picture_B.png");
            });

            // cloning implicitly pulls and fetches changesets from TFS into a git repo, so we can say that those scenarios are implicitly tested from a git LFS clean/smudge filter point of view
            h.Run("clone", h.TfsUrl, "$/MyProject", "MyProject");

            h.AssertCommitMessage("MyProject", "HEAD~2", "Project created from template", "", "git-tfs-id: [" + h.TfsUrl + "]$/MyProject;C1");
            //AssertNewClone("MyProject", new[] { "HEAD~2", "refs/heads/master", "refs/remotes/tfs/default" }, commit: "e8801fe3cf9dfdd64f5556d0f2ccb16176fe0661", tree: "4b825dc642cb6eb9a060e54bf8d69288fbee4904");
            h.AssertFileInWorkspace("MyProject", ".gitattributes", contentGitAttributes);
            h.AssertFileInIndex("MyProject", ".gitattributes", contentGitAttributes);

            h.AssertCommitMessage("MyProject", "HEAD~1", "Second TFS changeset: add a .gitattributes file with an entry for LFS-tracking *.png files", "", "git-tfs-id: [" + h.TfsUrl + "]$/MyProject;C2");
            //AssertNewClone("MyProject", new[] { "HEAD~1", "refs/heads/master", "refs/remotes/tfs/default" }, commit: "d05e642173a4d2dfe366b5c193d95ff752cf5bbd", tree: "bf24d73f70153626bdee5c2c1e845604a2848343");
            h.AssertFileInWorkspace("MyProject", "Some_Picture_A.png", File.ReadAllBytes(Path.Combine(h.Workdir, "GitTfs.Test.Integration.LfsCandidates.Some_Picture_A.png")));
            h.AssertFileInIndex("MyProject", "Some_Picture_A.png", GetLfsPointer("MyProject", "Some_Picture_A.png"));

            h.AssertCommitMessage("MyProject", "HEAD", "Third TFS changeset: add two binary, LFS-tracked *.png files", "", "git-tfs-id: [" + h.TfsUrl + "]$/MyProject;C3");
            AssertNewClone("MyProject", new[] { "HEAD", "refs/heads/master", "refs/remotes/tfs/default" }, commit: "75a3d4bc67c78396753c9bd5331cf07d145e0573", tree: "dad2d30e913a4d1491bdd562070aab3eeda77613");
            h.AssertFileInWorkspace("MyProject", "Some_Picture_B.png", File.ReadAllBytes(Path.Combine(h.Workdir, "GitTfs.Test.Integration.LfsCandidates.Some_Picture_B.png")));
            h.AssertFileInIndex("MyProject", "Some_Picture_B.png", GetLfsPointer("MyProject", "Some_Picture_B.png"));
        }
            /// <summary>
            /// This test will clone from a TFS repository using the --gitattributes option,
            /// validating that git-LFS will be activated from the very first changeset.
            /// The .gitattributes file is provided via command line (not from TFS), so LFS
            /// tracking is in effect even for files added in the earliest changesets.
            /// </summary>
            [FactExceptOnUnix]
            public void CloneWithInitialGitattributesOption()
            {
                // Create a .gitattributes file in the workdir to pass via --gitattributes
                string contentGitAttributes = "*.png filter=lfs diff=lfs merge=lfs -text\r\n";
                string gitAttributesFilePath = Path.Combine(h.Workdir, "initial.gitattributes");
                File.WriteAllText(gitAttributesFilePath, contentGitAttributes);

                h.SetupFake(r =>
                {
                    r.Changeset(1, "Project created from template", DateTime.Parse("2012-01-01 12:12:10 -05:00"))
                        .Change(TfsChangeType.Add, TfsItemType.Folder, "$/MyProject");
                    r.Changeset(2, "Second TFS changeset: add two binary *.png files (LFS-tracked via initial --gitattributes)", DateTime.Parse("2012-01-02 12:12:12 -05:00"))
                        .ChangeFromResource(TfsChangeType.Add, TfsItemType.File, "$/MyProject/Some_Picture_A.png", h.Workdir, "GitTfs.Test.Integration.LfsCandidates.Some_Picture_A.png")
                        .ChangeFromResource(TfsChangeType.Add, TfsItemType.File, "$/MyProject/Some_Picture_B.png", h.Workdir, "GitTfs.Test.Integration.LfsCandidates.Some_Picture_B.png");
                });

                // Clone with --gitattributes to enable LFS from the root commit
                h.Run("clone", $"--gitattributes={gitAttributesFilePath}", h.TfsUrl, "$/MyProject", "MyProject");

                // The root commit (HEAD~2) should be the initial .gitattributes commit created by git-tfs
                h.AssertFileInIndex("MyProject", ".gitattributes", contentGitAttributes);

                // The PNG files should be LFS pointer files in the index (clean filter applied)
                h.AssertFileInWorkspace("MyProject", "Some_Picture_A.png", File.ReadAllBytes(Path.Combine(h.Workdir, "GitTfs.Test.Integration.LfsCandidates.Some_Picture_A.png")));
                h.AssertFileInIndex("MyProject", "Some_Picture_A.png", GetLfsPointer("MyProject", "Some_Picture_A.png"));
                h.AssertFileInWorkspace("MyProject", "Some_Picture_B.png", File.ReadAllBytes(Path.Combine(h.Workdir, "GitTfs.Test.Integration.LfsCandidates.Some_Picture_B.png")));
                h.AssertFileInIndex("MyProject", "Some_Picture_B.png", GetLfsPointer("MyProject", "Some_Picture_B.png"));
            }
        }
    }
