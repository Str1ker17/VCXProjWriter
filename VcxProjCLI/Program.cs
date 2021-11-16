using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using CrosspathLib;
using VcxProjLib;

namespace VcxProjCLI {
    internal class Program {
        internal static Configuration config = Configuration.Default();

        private static String TakeArg(String[] args, ref int idx) {
            if ((idx + 1) >= args.Length) {
                if (idx < args.Length) {
                    throw new ApplicationException($"option '{args[idx]}' requires an argument");
                }
                throw new ApplicationException("TakeArg failed");
            }

            ++idx;
            return args[idx];
        }

        private static void ShowHelp() {
            Console.WriteLine( "VCXProjWriter v0.2.0");
            Console.WriteLine( "Copyleft (c) Str1ker, 2020-2021");
            Console.WriteLine( );
            Console.WriteLine( "Usage:");
            Console.WriteLine( "-s, --substitute-path BEFORE=AFTER");
            Console.WriteLine( "        replace all source/header paths that start with BEFORE to AFTER.");
            Console.WriteLine( "        You definitely want this when editing Linux project on Windows.");
            Console.WriteLine( "-o, --output-dir DIR");
            Console.WriteLine($"        put solution to DIR directory. Default is '{config.Outdir}'.");
            Console.WriteLine( "-f, --file FILE");
            Console.WriteLine($"        read compilation database from FILE.'");
            Console.WriteLine( "        The compilation database file should conform to compiledb \"arguments\" format.");
            Console.WriteLine( "        More about it: https://github.com/nickdiego/compiledb");
            Console.WriteLine( "-r, --remote");
            Console.WriteLine( "        connect to remote system via SSH and collect information");
            Console.WriteLine( "        from compiler(s) for more precise project generation");
            Console.WriteLine( "-D, --define NAME=VALUE");
            Console.WriteLine( "        define C preprocessor macro and override all occurences by this value.");
            Console.WriteLine( "        This is useful if build system put too much uniqueness to it.");
            Console.WriteLine( "--exclude-compiler COMPILER");
            Console.WriteLine( "        do not create anything for sources built with COMPILER.");
            Console.WriteLine( "        COMPILER can be an absolute or relative path.");
            Console.WriteLine( "--relax-include-dirs-order");
            Console.WriteLine( "        allow to group together files with the same include");
            Console.WriteLine( "        directories list but different order of them");
            Console.WriteLine( "--debug");
            Console.WriteLine( "        print lots of debugging information.");
            Console.WriteLine( "--help");
            Console.WriteLine( "        print this help message.");

            Environment.Exit(0);
        }

        private static void ProcessArgs(String[] args) {
            for (int idx = 0; idx < args.Length; idx++) {
                switch (args[idx]) {
                    case "-s":
                    case "--substitute-path":
                        String[] subst = TakeArg(args, ref idx).Split(new[] {'='}, 2);
                        if (subst.Length != 2) {
                            throw new ApplicationException("--substitute-path requires next arg to be like '/path/before=/path/after'");
                        }
                        AbsoluteCrosspath[] pair = new AbsoluteCrosspath[2];
                        for (int i = 0; i < 2; ++i) {
                            try {
                                pair[i] = Crosspath.FromString(subst[i]) as AbsoluteCrosspath;
                            }
                            catch (Exception e) {
                                throw new PolymorphismException($"{e.GetType()}: '{subst[i]}' is not a valid absolute path");
                            }
                        }
                        config.Substitutions.Add(new Tuple<AbsoluteCrosspath, AbsoluteCrosspath>(pair[0], pair[1]));
                        break;

                    case "-b":
                    case "--base-dir":
                        config.BaseDir = AbsoluteCrosspath.FromString(TakeArg(args, ref idx));
                        break;

#if PATH_BASED_FILTERING
                    case "--include":
                        config.IncludeFilesFrom.Add(Crosspath.FromString(TakeArg(args, ref idx)));
                        break;

                    case "--exclude":
                        config.ExcludeFilesFrom.Add(Crosspath.FromString(TakeArg(args, ref idx)));
                        break;
#endif

                    case "-o":
                    case "--output-dir":
                        config.Outdir = TakeArg(args, ref idx);
                        break;

                    case "-f":
                    case "--file":
                        config.InputFile = TakeArg(args, ref idx);
                        break;

                    case "-r":
                    case "--remote":
                        RemoteHost remote = RemoteHost.Parse(TakeArg(args, ref idx));
                        config.AssignRemote(remote);
                        break;

                    case "-D":
                    case "--define":
                        config.OverrideDefines.Add(new Define(TakeArg(args, ref idx)));
                        break;

                    case "--exclude-compiler":
                        config.ExcludeCompilers.Add(Crosspath.FromString(TakeArg(args, ref idx)));
                        break;

                    case "--relax-include-dirs-order":
                        config.RelaxIncludeDirsOrder = true;
                        break;

                    case "--randomize-outdir":
                        config.Outdir = Path.Combine(config.Outdir, new Random().Next().ToString("x8"));
                        break;

                    case "--open-now":
                        config.OpenSolution = true;
                        break;

                    case "--debug":
                        Logger.Level = LogLevel.Debug;
                        break;

                    case "--help":
                        ShowHelp();
                        return;

                    default:
                        throw new Exception($"Unknown command line parameter '{args[idx]}");
                }
            }
        }

        private static void Main(String[] args) {
#if !DEBUG
            try {
#endif
                ProcessArgs(args);
                if (config.InputFile == null) {
                    ShowHelp();
                }

                Stopwatch sw = new Stopwatch();

                sw.Start();
                Solution sln = new Solution(config);
                sln.ParseCompileDB(config.InputFile);
                sw.Stop();
                Int64 parseAndGroup = sw.ElapsedMilliseconds;

                sw.Reset();
                sw.Start();
                if (config.Remote != null) {
                    sln.RetrieveExtraInfoFromRemote(config.Remote);
                }
                sw.Stop();
                Int64 remoteInfo = sw.ElapsedMilliseconds;

                sln.FilterOutEntries();

                sw.Reset();
                sw.Start();
                foreach (Tuple<AbsoluteCrosspath, AbsoluteCrosspath> substitution in config.Substitutions) {
                    sln.Rebase(substitution.Item1, substitution.Item2);
                }
                sw.Stop();
                Int64 rebase = sw.ElapsedMilliseconds;

                // DONE: also check for:
                //   - project file accessibility
                //   - include dirs accessibility
                List<IncludeDirectory> remoteNotRebased = new List<IncludeDirectory>();
                sln.CheckForTotalRebase(ref remoteNotRebased);

                try {
                    Directory.Delete(config.Outdir, true);
                }
                catch {
                    // ignored
                }

                if (config.Remote != null) {
                    sln.DownloadCompilerIncludeDirectoriesFromRemote(config.Remote, config.Outdir);
                    foreach (IncludeDirectory includeDirectory in remoteNotRebased) {
                        includeDirectory.RebaseToLocal(config.Remote
                              , AbsoluteCrosspath.GetCurrentDirectory().Append(RelativeCrosspath.FromString(config.Outdir))
                                                                       .Append(RelativeCrosspath.FromString(String.Format(SolutionStructure.RemoteIncludePath, config.Remote.Host))));
                    }
                }

                sw.Reset();
                sw.Start();
                sln.WriteToDirectory(config.Outdir);
                sw.Stop();
                Int64 write = sw.ElapsedMilliseconds;

                Console.WriteLine();
                Console.WriteLine($"Elapsed time: parseAndGroup = {parseAndGroup} ms, remoteInfo = {remoteInfo} ms" 
                                + $", rebase = {rebase} ms, write = {write} ms.");

                if (config.OpenSolution) {
                    Process.Start(new ProcessStartInfo(Path.Combine(config.Outdir, SolutionStructure.SolutionFilename)) { UseShellExecute = true });
                }
#if !DEBUG
            }
            catch (Exception e) {
                Console.WriteLine($"[x] {e.Message}");
                Console.WriteLine();
                Console.WriteLine("Stack trace for debugging purposes:");
                Console.WriteLine($"{e.StackTrace}");
                Console.WriteLine("Press Enter to continue...");
                Console.ReadLine();
            }
#endif
        }
    }
}