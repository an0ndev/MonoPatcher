using System;

class MonoPatcher {
	static void Main (string[] args) {
		Console.WriteLine ("works!");
        if (args.Length != 3) {
            Console.WriteLine ("usage: mono MonoPatcher.exe [input assembly file] [patch directory] [output assembly file]");
        }
        string inputAssembly = args [0];
        string patchDirectory = args [1];
        string outputAssembly = args [2];
        Console.WriteLine (args.Length);
        foreach (string arg in args) {
            Console.WriteLine (arg);
        }
	}
}
