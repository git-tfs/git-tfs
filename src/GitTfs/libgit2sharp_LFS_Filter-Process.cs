//
// Found in a gist by Dedmen Miller <dedmen@dedmen.de> at:
// https://gist.github.com/dedmen/ab740ad9ebfde0403e8223480bef91ae


using LibGit2Sharp;

using System.Diagnostics;
using System.Text;

public class GitPktLine
{
    // Git Pkt-line protocol
    // https://git-scm.com/docs/gitprotocol-common
    // https://github.com/git-lfs/pktline

    //public static FileStream debugLog = new FileStream("p:/log", FileMode.Create);

    private static void WritePacketInt(string data, Stream output)
    {
        // Size
        var packetLength = data.Length + 4 + 1; // + 4byte length, + terminating LF
        var packet = System.Text.Encoding.ASCII.GetBytes(packetLength.ToString("x4"));

        output.Write(packet, 0, packet.Length);
        var dataPacket = System.Text.Encoding.ASCII.GetBytes(data);
        output.Write(dataPacket, 0, dataPacket.Length);
        var terminatorChar = new[] { (byte)'\n' };
        output.Write(terminatorChar, 0, terminatorChar.Length); // Terminating LF //#TODO this is optional.. its probably easier to just omit it. Including it in binary data is error, excluding it in text data is fine

        //{
        //    debugLog.Write(System.Text.Encoding.ASCII.GetBytes(packetLength.ToString("x4")));
        //    debugLog.Write(System.Text.Encoding.ASCII.GetBytes(data));
        //    debugLog.Write(new[] { (byte)'\n' }); // Terminating LF
        //    debugLog.Flush();
        //}
    }

    private static void WritePacketInt(byte[] data, int bufferLength, Stream output)
    {
        // Size
        var packetLength = bufferLength + 4 /*+ 1*/; // + 4byte length, + terminating LF
        var packet = System.Text.Encoding.ASCII.GetBytes(packetLength.ToString("x4"));
        output.Write(packet, 0, packet.Length);
        output.Write(data, 0, bufferLength);
        //output.Write(new[] { (byte)'\n' }); // Terminating LF

        //{
        //    debugLog.Write(System.Text.Encoding.ASCII.GetBytes(packetLength.ToString("x4")));
        //    debugLog.Write(data, 0, bufferLength);
        //    debugLog.Write(new[] { (byte)'\n' }); // Terminating LF
        //    debugLog.Flush();
        //}
    }

    public static void WriteMessage(string message, Stream target)
    {
        using var output = new MemoryStream();
        WritePacketInt(message, output);
        {
            output.Seek(0, SeekOrigin.Begin);
            Trace.TraceInformation(System.Text.Encoding.ASCII.GetString(output.ToArray()));
        }

        output.CopyTo(target);
    }

    public static void WriteMessagePacketList(IEnumerable<string> messages, Stream target)
    {
        using var output = new MemoryStream();
        // List of packets, terminated by a flush

        foreach (var message in messages)
        {
            WritePacketInt(message, output);
        }
        Flush(output);

        output.Seek(0, SeekOrigin.Begin);
        //Trace.TraceInformation(">" + System.Text.Encoding.ASCII.GetString(output.GetBuffer()));
        output.CopyTo(target);
        target.Flush();
    }

    public static void Flush(Stream target)
    {
        byte[] flushTerminator = Encoding.UTF8.GetBytes("0000");
        target.Write(flushTerminator, 0, flushTerminator.Length);
        target.Flush();
    }

    public static void Delim(Stream target)
    {
        byte[] delimiter = Encoding.UTF8.GetBytes("0001");
        target.Write(delimiter, 0, delimiter.Length);
    }

    public static byte[] ReadMessage(Stream source, bool stripNewline)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        byte[] pktLength = new byte[4];

        try
        {
            ReadExactly(source, pktLength, 0, 4);
        }
        catch (EndOfStreamException)
        {
            return Array.Empty<byte>();
        }

        string header = System.Text.Encoding.ASCII.GetString(pktLength);

        int packetLength;

        try
        {
            packetLength = Convert.ToInt32(header, 16);
        }
        catch (FormatException ex)
        {
            throw new InvalidDataException(
                $"Invalid pkt-line header '{header}'",
                ex);
        }

        // flush packet
        if (packetLength == 0)
            return Array.Empty<byte>();

        if (packetLength < 4)
        {
            throw new InvalidDataException(
                $"Invalid pkt-line length {packetLength}");
        }

        int payloadLength = packetLength - 4;

        byte[] payload = ReadExactly(source, payloadLength);

        if (stripNewline && payload.Length > 0 && payload[payload.Length - 1] == (byte)'\n')
        {
            Array.Resize(ref payload, payload.Length - 1);
        }

        return payload;
    }

    public static List<string> ReadMessagePacketList(Stream source)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        var messages = new List<string>();

        while (true)
        {
            byte[] msg = ReadMessage(source, true);

            if (msg == null)
            {
                throw new InvalidDataException(
                    "Received null pkt-line message.");
            }

            // flush packet terminates list
            if (msg.Length == 0)
                break;

            messages.Add(System.Text.Encoding.UTF8.GetString(msg));
        }

        return messages;
    }

    public static IEnumerable<byte[]> ReadMessagePacketListBinary(Stream source)
    {
        var result = new List<byte[]>();
        while (true)
        {
            var msg = ReadMessage(source, false);

            if (msg.Length == 0) // flush
                break;

            result.Add(msg);
        }
        return result;
    }

    //! Write all data from input stream out as packets
    public static void WriteStreamData(Stream input, Stream target)
    {
        // All data in 8192 chunks (65kb is max, so we could be bigger), terminated by a flush
        var buffer = new byte[8192];
        var sentLength = 0;
        do
        {
            sentLength = input.Read(buffer, 0, buffer.Length);

            if (sentLength > 0)
                WritePacketInt(buffer, sentLength, target);

            // Trace.TraceInformation($"S> {Encoding.ASCII.GetString(buffer)}");

        } while (sentLength > 0);

        target.Flush();
    }

    // Read multiple packets from input stream, and send all the data to target
    public static void ReadStreamData(Stream input, Stream target)
    {
        var buffers = ReadMessagePacketListBinary(input); //#TODO this will load the complete data into memory. We could instead just fetch chunks and write target per chunk, lowers memory usage

        foreach (var bytes in buffers)
        {
            target.Write(bytes, 0, bytes.Length);
        }
    }

    private static void ReadExactly(Stream source, byte[] buffer, int offset, int count)
    {
        while (count > 0)
        {
            int n = source.Read(buffer, offset, count);

            if (n == 0)
            {
                throw new EndOfStreamException(
                    "Unexpected EOF while reading pkt-line stream.");
            }

            offset += n;
            count -= n;
        }
    }

    private static byte[] ReadExactly(Stream source, int count)
    {
        var buffer = new byte[count];
        ReadExactly(source, buffer, 0, count);
        return buffer;
    }
}

public class LFSFilter : Filter
{
    private Process processFilterP = null;

    public LFSFilter() : base("lfs", new[] { new FilterAttributeEntry("lfs") })
    {
        //Trace.TraceInformation("LFSFilter default constructor");
        // We can start one filter process, and keep using it. Instead of starting/stopping for each file
    }

    protected override void Clean(string path, string root, Stream input, Stream output)
    {
        // The input buffer is only 65536 bytes large, this function will get called repeatedly for the same path, until all data is passed through

        // Run

        // https://github.com/git-lfs/git-lfs/blob/main/commands/command_filter_process.go#L83
        // Payload end is identified by sending a Flush

        // payload data
        GitPktLine.WriteStreamData(input, processFilterP.StandardInput.BaseStream);

        //Trace.TraceInformation($"LFSFilter Clean path    = '{path}', root = '{root}'");
        // After we've sent all data, we'll go to Complete, send a Flush to signify end, and read the results
    }

    protected override void Complete(string path, string root, Stream output)
    {
        // Communicate that we are done transmitting this file
        GitPktLine.Flush(processFilterP.StandardInput.BaseStream);
        // Now we can read outputs
        GitPktLine.ReadStreamData(processFilterP.StandardOutput.BaseStream, output);

        var status2 = GitPktLine.ReadMessagePacketList(processFilterP.StandardOutput.BaseStream); // status=success (Execution has finished)

        // Trace.TraceInformation($"LFSFilter Complete path = '{path}', root = '{root}'");

        output.Flush();
        output.Close();

        if (status2.First() != "status=success")
        {
            throw new Exception($"LFSFilter ReadMessagePacketList returned errors {status2.First()}");
        }
    }

    protected override void Create(string path, string root, FilterMode mode)
    {
        //Trace.TraceInformation($"LFSFilter Create path = '{path}', root = '{root}', mode = '{mode}'");
        Trace.TraceInformation($"LFSFilter Create: applying '{mode}' filter to '{path}'");

        //GitPktLine.debugLog.Dispose();
        //GitPktLine.debugLog = new FileStream($"p:/log{Path.GetFileName(path)}", FileMode.Create);

        if (processFilterP == null)
        {
            try
            {
                // launch git-lfs
                processFilterP = RunLFSProcess(root, $"filter-process", false);

                // Init // https://git-scm.com/docs/long-running-process-protocol

                GitPktLine.WriteMessagePacketList(new[] { "git-filter-client", "version=2" }, processFilterP.StandardInput.BaseStream);
                var serverInit = GitPktLine.ReadMessagePacketList(processFilterP.StandardOutput.BaseStream);


                // capabilities
                GitPktLine.WriteMessagePacketList(new[] { "capability=clean", "capability=smudge" }, processFilterP.StandardInput.BaseStream);
                var supportedCaps = GitPktLine.ReadMessagePacketList(processFilterP.StandardOutput.BaseStream);

                // ready for commands now
            }
            catch (Exception e)
            {
                Trace.TraceInformation(e.Message);
                Trace.TraceInformation(e.StackTrace);
            }
        }

        GitPktLine.WriteMessagePacketList(new[] { mode == FilterMode.Clean ? "command=clean" : "command=smudge", $"pathname={path}" }, processFilterP.StandardInput.BaseStream);
        var status = GitPktLine.ReadMessagePacketList(processFilterP.StandardOutput.BaseStream); // status=success (command was accepted)
    }

    protected override void Initialize()
    {
        base.Initialize();
        Trace.TraceInformation("LFSFilter Initialize");
    }

    protected override void Smudge(string path, string root, Stream input, Stream output)
    {
        // Run
        // The input buffer is only 65536 bytes large, this function will get called repeatedly for the same path, until all data is passed through

        // https://github.com/git-lfs/git-lfs/blob/main/commands/command_filter_process.go#L93

        // payload data
        GitPktLine.WriteStreamData(input, processFilterP.StandardInput.BaseStream);

        //Trace.TraceInformation($"LFSFilter Smudge path    = '{path}', root = '{root}'");
        // After we've sent all data, we'll go to Complete, send a Flush to signify end, and read the results
    }

    private static Process RunLFSProcess(string root, string command, bool bAsyncOutput = true)
    {
        // adjust for the situation when running in a console using the UTF-8 code page 65001
        // like it may happen when invoked from a GitExtensions "script", as suggested by
        // Kalle Olavi Niemitalo here: https://github.com/git-lfs/git-lfs/issues/5831#issuecomment-2244261656
        if (Console.InputEncoding.CodePage == 65001)
        {
            Console.InputEncoding = new UTF8Encoding(false);
        }

        // launch git-lfs
        var process = new Process();
        process.StartInfo.FileName = "git-lfs";
        process.StartInfo.Arguments = command;
        process.StartInfo.WorkingDirectory = root;
        process.StartInfo.RedirectStandardInput = true;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.CreateNoWindow = true;
        process.StartInfo.UseShellExecute = false;

        process.ErrorDataReceived += (sender, args) =>
        {
            if (!string.IsNullOrEmpty(args.Data))
            {
                Trace.TraceInformation($"LFSFilter E: {args.Data}");
            }
        };

        if (bAsyncOutput)
        {
            process.OutputDataReceived += (sender, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                {
                    Trace.TraceInformation($"LFSFilter O: {args.Data}");
                }
            };
        }

        process.EnableRaisingEvents = true;

        process.Start();

        process.BeginErrorReadLine();

        if (bAsyncOutput)
        {
            process.BeginOutputReadLine();
        }
        return process;
    }


    // https://git-scm.com/docs/githooks

    public static void PrePush(string root, IEnumerable<PushUpdate> updates)
    {
        var process = RunLFSProcess(root, $"pre-push origin");

        foreach (var update in updates)
        {
            process.StandardInput.Write($" {update.DestinationRefName} {update.DestinationObjectId} {update.SourceRefName} {update.SourceObjectId}\n");
        }
        process.StandardInput.Flush();
        process.StandardInput.Close();

        process.WaitForExit();
        Trace.TraceInformation($"LFSFilter PrePush root      = {root}");
    }

    public static void PostCheckout(string root, string oldRef, string newRef)
    {
        var process = RunLFSProcess(root, $"post-checkout {oldRef} {newRef} 0");
        process.WaitForExit();
        Trace.TraceInformation($"LFSFilter PostCheckout root = {root}, oldRef = {oldRef}, newRef = {newRef}");
    }

    public static void PostCommit(string root)
    {
        var process = RunLFSProcess(root, "post-commit");
        process.WaitForExit();
        Trace.TraceInformation($"LFSFilter PostCommit root   = {root}");
    }
}
