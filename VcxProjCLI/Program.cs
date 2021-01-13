using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using CrosspathLib;
using VcxProjLib;

namespace VcxProjCLI {
    internal class Program {
        internal static String File = "compile_commands.json";
        internal static String Outdir = "output";

        internal static List<Tuple<AbsoluteCrosspath, AbsoluteCrosspath>> Substitutions =
                new List<Tuple<AbsoluteCrosspath, AbsoluteCrosspath>>();

        private static void ProcessArgs(String[] args) {
            for (int idx = 0; idx < args.Length; idx++) {
                switch (args[idx]) {
                    case "-s":
                    case "--substitute-path":
                        String[] subst = args[idx + 1].Split(new[] {'='}, 2);
                        if (subst.Length != 2) {
                            throw new Exception("--substitute-path requires next arg to be like '/path/before=/path/after'");
                        }

                        AbsoluteCrosspath before, after;
                        try {
                            before = Crosspath.FromString(subst[0]) as AbsoluteCrosspath;
                        }
                        catch (Exception e) {
                            throw new CrosspathLibPolymorphismException(
                                    $"{e.GetType()}: '{subst[0]}' is not a valid absolute path");
                        }

                        try {
                            after = Crosspath.FromString(subst[1]) as AbsoluteCrosspath;
                        }
                        catch (Exception e) {
                            throw new CrosspathLibPolymorphismException(
                                    $"{e.GetType()}: '{subst[0]}' is not a valid absolute path");
                        }

                        Substitutions.Add(new Tuple<AbsoluteCrosspath, AbsoluteCrosspath>(before, after));
                        ++idx;
                        break;
                    case "-o":
                    case "--output-dir":
                        Outdir = args[idx + 1];
                        ++idx;
                        break;
                    case "-f":
                    case "--file":
                        Outdir = args[idx + 1];
                        ++idx;
                        break;
                    case "--debug":
                        Logger.DebugOutput = true;
                        break;
                    case "--help":
                        Console.WriteLine("VCXProjWriter v0.1.1");
                        Console.WriteLine("Copyleft (c) Str1ker, 2020-2021");
                        Console.WriteLine();
                        Console.WriteLine("Usage:");
                        Console.WriteLine("-s, --substitute-path BEFORE=AFTER");
                        Console.WriteLine("        replace all source/header paths starts with BEFORE using rule.");
                        Console.WriteLine("        You definitely want this when editing Linux project on Windows.");
                        Console.WriteLine("-o, --output-dir DIR");
                        Console.WriteLine("        put solution to DIR directory. Default is '{0}'.", Outdir);
                        Console.WriteLine("-f, --file FILE");
                        Console.WriteLine($"        read compilation database from FILE. Default is '{File}'");
                        Console.WriteLine("        The compilation database file should conform to compiledb \"arguments\" format.");
                        Console.WriteLine("        More about it: https://github.com/nickdiego/compiledb");
                        Console.WriteLine("--debug");
                        Console.WriteLine("        print lots of debugging information.");
                        Console.WriteLine("--help");
                        Console.WriteLine("        print this help message.");
                        return;
                    default:
                        throw new Exception($"Unknown command line parameter '{args[idx]}");
                }
            }
        }

        private static void Main(String[] args) {
            try {
                ProcessArgs(args);

                Stopwatch sw = new Stopwatch();

                sw.Start();
                Solution sln = Solution.CreateSolutionFromCompileDB(File);
                sw.Stop();
                Int64 parseAndGroup = sw.ElapsedMilliseconds;

                sw.Reset();
                sw.Start();
                foreach (Tuple<AbsoluteCrosspath, AbsoluteCrosspath> substitution in Substitutions) {
                    sln.Rebase(substitution.Item1, substitution.Item2);
                }
                sw.Stop();
                Int64 rebase = sw.ElapsedMilliseconds;

                // DONE: also check for:
                //   - project file accessibility
                //   - include dirs accessibility
                sln.CheckForTotalRebase();

                try {
                    Directory.Delete(Outdir, true);
                }
                catch {
                    // ignored
                }

                sw.Reset();
                sw.Start();
                sln.WriteToDirectory(Outdir);
                sw.Stop();
                Int64 write = sw.ElapsedMilliseconds;

                Console.WriteLine();
                Console.WriteLine($"Elapsed time: parseAndGroup = {parseAndGroup} ms, rebase = {rebase} ms, write = {write} ms.");
            }
            catch (Exception e) {
                Console.WriteLine($"[x] {e.Message}");
                Console.WriteLine();
                Console.WriteLine("Stack trace for debugging purposes:");
                Console.WriteLine($"{e.StackTrace}");
                Console.WriteLine("Press Enter to continue...");
                Console.ReadLine();
            }
        }
    }
}