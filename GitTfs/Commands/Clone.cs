namespace Sep.Git.Tfs.Commands
{
    public class Clone : GitTfsCommand
    {
        [OptDef(OptValType.ValueReq)]
        [ShortOptionName('r')]
        public int? revision { get; set; }

        private InitOptions initOptions;
        private FcOptions fcOptions;
        private RemoteOptions remoteOptions;

        public IEnumerable<ParseHelper> ExtraOptions
        {
            get
            {
                return from options in new [] { initOptions, fcOptions, remoteOptions }
                       select new PropertyThingParseHelper(options);
            }
        }
    }
}
