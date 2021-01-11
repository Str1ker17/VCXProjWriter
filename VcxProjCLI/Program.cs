using System;
using System.Collections.Generic;
using System.IO;
using CrosspathLib;
using VcxProjLib;

namespace VcxProjCLI {
    class Program {
        static void Main(String[] args) {
            String file = "compile_commands.json";
            String outdir = "output";
            List<Tuple<String, String>> substitutions = new List<Tuple<String, String>>();

            try {
                for (int idx = 0; idx < args.Length; idx++) {
                    switch (args[idx]) {
                        case "-s":
                        case "--substitute-path":
                            String[] subst = args[idx + 1].Split(new[] {'='}, 2);
                            if (subst.Length != 2) {
                                throw new Exception(
                                        "--substitute-path requires next arg to be like '/path/before=/path/after'");
                            }

                            substitutions.Add(new Tuple<String, String>(subst[0], subst[1]));
                            ++idx;
                            break;
                        case "-o":
                        case "--output-dir":
                            ++idx;
                            break;
                        case "-f":
                        case "--file":
                            ++idx;
                            break;
                        case "--debug":
                            Solution.DebugOutput = true;
                            break;
                        case "--help":
                            Console.WriteLine("VCXProjWriter v0.1");
                            Console.WriteLine("Copyleft (c) Str1ker, 2020-2021");
                            Console.WriteLine();
                            Console.WriteLine("Usage:");
                            Console.WriteLine("-s, --substitute-path BEFORE=AFTER");
                            Console.WriteLine("        replace all source/header paths using rule.");
                            Console.WriteLine(
                                    "        You definitely want this when editing Linux project on Windows.");
                            Console.WriteLine("-o, --output-dir DIR");
                            Console.WriteLine("        put solution to DIR directory. Default is '{0}'.", outdir);
                            Console.WriteLine("-f, --file FILE");
                            Console.WriteLine(
                                    String.Format("        read compilation database from FILE. Default is '{0}'"
                                          , file));
                            Console.WriteLine(
                                    "        The compilation database file should conform to compiledb \"arguments\" format.");
                            Console.WriteLine("        More about it: https://github.com/nickdiego/compiledb");
                            Console.WriteLine("--debug");
                            Console.WriteLine("        print lots of debugging information.");
                            Console.WriteLine("--help");
                            Console.WriteLine("        print this help message.");
                            return;
                        default:
                            throw new Exception(String.Format("Unknown command line parameter '{0}", args[idx]));
                    }
                }



                //substitutions.Add(new Tuple<String, String>(@"/local/store/bin-src/qemu", @"D:\Workspace\Sources\qemu"));
                //substitutions.Add(new Tuple<String, String>(@"/sonic/src/sonic-swss", @"D:\Workspace\Sources\sonic-buildimage\src\sonic-swss"));

                Solution sln = Solution.CreateSolutionFromCompileDB(file);
                foreach (Tuple<String, String> substitution in substitutions) {
                    //sln.SubstitutePath(substitution.Item1, substitution.Item2);
                    sln.Rebase(Crosspath.FromString(substitution.Item1) as AbsoluteCrosspath
                          , Crosspath.FromString(substitution.Item2) as AbsoluteCrosspath);
                }

                try {
                    Directory.Delete("output", true);
                }
                catch {
                    // ignored
                }

                sln.WriteToDirectory("output");
            }
            catch (Exception e) {
                Console.WriteLine(String.Format("[x] {0}", e.Message));
                Console.WriteLine();
                Console.WriteLine("Stack trace for debugging purposes:");
                Console.WriteLine(String.Format("{0}", e.StackTrace));
                Console.ReadLine();
            }
        }
    }
}