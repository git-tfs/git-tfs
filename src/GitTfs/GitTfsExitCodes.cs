namespace GitTfs
{
    /// <summary>
    ///     Collection of exit codes used by git-tfs.
    /// </summary>
    /// <remarks>
    ///     For consistency across all running environments, both various
    ///     Windows - shells (powershell.exe, cmd.exe) and UNIX - like environments
    ///     such as bash (MinGW), sh or zsh avoid using negative exit status codes
    ///     or codes 255 or higher.
    ///
    ///     Some running environments might either modulo exit codes with 256 or clamp
    ///     them to interval [0, 255].
    ///
    ///     For more information:
    ///     http://www.gnu.org/software/libc/manual/html_node/Exit-Status.html
    ///     http://tldp.org/LDP/abs/html/exitcodes.html
    /// </remarks>
    public static class GitTfsExitCodes
    {
        public const int OK = 0;
        public const int Help = 1;
        public const int InvalidArguments = 2;
        public const int ForceRequired = 3;
        public const int SomeDataCouldNotHaveBeenRetrieved = 4;
        public const int ExceptionThrown = Byte.MaxValue - 1;

        //Verify
        public const int VerifyPathCaseMismatch = 100;
        public const int VerifyFileMissing = 101;
        public const int VerifyContentMismatch = 102;
    }
}
