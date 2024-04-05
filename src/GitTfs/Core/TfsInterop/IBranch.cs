using GitTfs.Core.BranchVisitors;

namespace GitTfs.Core.TfsInterop
{
    public interface IBranchObject
    {
        string Path { get; }
        string ParentPath { get; }
        bool IsRoot { get; }
    }

    public class BranchTree
    {
        public BranchTree(IBranchObject branch)
            : this(branch, new List<BranchTree>())
        {
        }

        public BranchTree(IBranchObject branch, IEnumerable<BranchTree> childBranches)
            : this(branch, childBranches.ToList())
        {
        }

        public BranchTree(IBranchObject branch, List<BranchTree> childBranches)
        {
            if (childBranches == null)
                throw new ArgumentNullException("childBranches");
            Branch = branch;
            ChildBranches = childBranches;
        }

        public IBranchObject Branch { get; private set; }

        public List<BranchTree> ChildBranches { get; private set; }

        public string Path => Branch.Path;
        public string ParentPath => Branch.ParentPath;
        public bool IsRoot => Branch.IsRoot;

        public override string ToString() => $"{Path} [{ChildBranches.Count} children]";
    }

    public static class BranchExtensions
    {
        /// <summary>Find the root branch that match the path given in <paramref name="remoteTfsPath" />.</summary>
        /// <param name="tfs">The <see cref="ITfsHelper" /> to use.</param>
        /// <param name="remoteTfsPath">The TFS path to search for.</param>
        /// <param name="searchExactPath">If <paramref name="remoteTfsPath" /> must match exact, or if it is ok if
        ///     <paramref name="remoteTfsPath" /> is a part of the path.</param>
        /// <returns>The root branch that match the search criteria or null if nothing was found.</returns>
        /// <remarks>
        ///     If the root branch matching the search criteria was not found, it try to search by all deleted branches to.
        ///     This can usable if the user want to clone a deleted branch.
        /// </remarks>
        public static BranchTree GetRootTfsBranchForRemotePath(this ITfsHelper tfs, string remoteTfsPath, bool searchExactPath = true)
            => GetRootTfsBranchForRemotePath(tfs, remoteTfsPath, searchExactPath, false) ??
               GetRootTfsBranchForRemotePath(tfs, remoteTfsPath, searchExactPath, true);

        private static BranchTree GetRootTfsBranchForRemotePath(this ITfsHelper tfs, string remoteTfsPath, bool searchExactPath, bool searchDeletedBranches)
        {
            var branches = tfs.GetBranches(searchDeletedBranches);
            var branchTrees = branches.Aggregate(new Dictionary<string, BranchTree>(StringComparer.OrdinalIgnoreCase), (dict, branch) => dict.Tap(d => d.Add(branch.Path, new BranchTree(branch))));
            foreach (var branch in branchTrees.Values)
            {
                if (!branch.IsRoot)
                {
                    //in some strange cases there might be a branch which is not marked as IsRoot
                    //but the parent for this branch is missing.
                    if (branchTrees.ContainsKey(branch.ParentPath))
                        branchTrees[branch.ParentPath].ChildBranches.Add(branch);
                }
            }
            var roots = branchTrees.Values.Where(b => b.IsRoot);
            return roots.FirstOrDefault(b =>
            {
                var visitor = new BranchTreeContainsPathVisitor(remoteTfsPath, searchExactPath);
                b.AcceptVisitor(visitor);
                return visitor.Found;
            });
        }

        public static void AcceptVisitor(this BranchTree branch, IBranchTreeVisitor treeVisitor, int level = 0)
        {
            treeVisitor.Visit(branch, level);
            foreach (var childBranch in branch.ChildBranches)
            {
                childBranch.AcceptVisitor(treeVisitor, level + 1);
            }
        }

        public static IEnumerable<BranchTree> GetAllChildren(this BranchTree branch)
        {
            if (branch == null) return Enumerable.Empty<BranchTree>();

            var childrenBranches = new List<BranchTree>(branch.ChildBranches);
            foreach (var childBranch in branch.ChildBranches)
            {
                childrenBranches.AddRange(childBranch.GetAllChildren());
            }
            return childrenBranches;
        }

        public static IEnumerable<BranchTree> GetAllChildrenOfBranch(this BranchTree branch, string tfsPath)
        {
            if (branch == null) return Enumerable.Empty<BranchTree>();

            if (string.Compare(branch.Path, tfsPath, StringComparison.InvariantCultureIgnoreCase) == 0)
                return branch.GetAllChildren();

            var childrenBranches = new List<BranchTree>();
            foreach (var childBranch in branch.ChildBranches)
            {
                childrenBranches.AddRange(GetAllChildrenOfBranch(childBranch, tfsPath));
            }
            return childrenBranches;
        }
    }
}