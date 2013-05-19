using System;

namespace Sep.Git.Tfs.Util
{
    [StructureMapSingleton]
    public class Dialog
    {
        public void Say(string message)
        {
            Console.WriteLine(message);
        }

        public string Ask(string question)
        {
            Console.Write(question);
            return Console.ReadLine();
        }
    }
}
