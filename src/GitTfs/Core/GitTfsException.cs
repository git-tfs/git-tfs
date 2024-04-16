namespace GitTfs.Core
{
    public class GitTfsException : Exception
    {
        public IEnumerable<string> RecommendedSolutions { get; set; }

        public GitTfsException(string message, IEnumerable<string> solutions, Exception e)
            : base(message, e)
        {
            RecommendedSolutions = solutions;
        }

        public GitTfsException(string message, Exception e)
            : base(message, e)
        { }

        public GitTfsException(string message, IEnumerable<string> solutions)
            : base(message)
        {
            RecommendedSolutions = solutions;
        }

        public GitTfsException(string message)
            : base(message)
        { }

        public GitTfsException WithRecommendation(params string[] recommendations)
        {
            if (RecommendedSolutions == null)
                RecommendedSolutions = (IEnumerable<string>)recommendations.Clone();
            else
                RecommendedSolutions = RecommendedSolutions.Concat(recommendations);
            return this;
        }
    }
}