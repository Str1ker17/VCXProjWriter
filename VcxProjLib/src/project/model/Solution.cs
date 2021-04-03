using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CrosspathLib;
using Newtonsoft.Json;

namespace VcxProjLib {
    public class Solution {
        protected static readonly Dictionary<IncludeDirectoryType, String> IncludeParam = 
            new Dictionary<IncludeDirectoryType, String> {
                {IncludeDirectoryType.Generic, "-I"}
              , {IncludeDirectoryType.Quote, "-iquote"}
              , {IncludeDirectoryType.System, "-isystem"}
              , {IncludeDirectoryType.DirAfter, "-idirafter"}
            };

        /// <summary>
        /// The unique identifier of solution in terms of VS.
        /// </summary>
        protected Guid selfGuid;

        /// <summary>
        /// Solution is a set of projects.
        /// </summary>
        protected Dictionary<Int64, Project> projects;

        // these containers have 2 targets:
        //   1. avoid duplicates;
        //   2. help in batch processing, like rebasing the whole solution from one directory to another.
        // regarding include directories:
        // since we can construct the same absolute path from different combination of relative path
        // and working directory, the 'String' is reconstructed abspath, which is unique.
        // TODO: use collection comparator instead to distinguish between same/different dirs
        // remember that solutionIncludeDirectories is provided for convenience of rebasing
        // but it does NOT need to be sorted and/or preserve order. go to Project for this
        protected HashSet<ProjectFile> solutionFiles;
        protected HashSet<Compiler> solutionCompilers;
        protected Dictionary<String, IncludeDirectory> solutionIncludeDirectories;
        protected HashSet<Guid> acquiredGuids;

        protected Solution() {
            projects = new Dictionary<Int64, Project>();
            solutionFiles = new HashSet<ProjectFile>();
            solutionCompilers = new HashSet<Compiler>();
            solutionIncludeDirectories = new Dictionary<String, IncludeDirectory>();
            acquiredGuids = new HashSet<Guid>();
            selfGuid = AllocateGuid();
        }

        protected Guid AllocateGuid() {
            Guid guid;
            while (true) {
                guid = Guid.NewGuid();
                if (!acquiredGuids.Contains(guid)) {
                    // lol how I forgot this
                    acquiredGuids.Add(guid);
                    break;
                }
            }

            return guid;
        }

        public static Solution CreateSolutionFromCompileDB(String filename) {
            Solution sln = new Solution();
            sln.ParseCompileDB(filename);
            return sln;
        }

        /// <summary>
        /// Can be called multiple times.
        /// </summary>
        /// <param name="before">Old base (e.g. Unix path where compiled)</param>
        /// <param name="after">New base (e.g. Windows path where edited)</param>
        public void Rebase(AbsoluteCrosspath before, AbsoluteCrosspath after) {
            // move project files to another location
            foreach (ProjectFile projectFile in solutionFiles) {
                projectFile.FilePath.Rebase(before, after);
            }

            // move include directories to another location
            foreach (KeyValuePair<String, IncludeDirectory> includeDirPair in solutionIncludeDirectories) {
                includeDirPair.Value.Rebase(before, after);
            }
            
            //foreach (var project in Projects) {
            //    foreach (AbsoluteCrosspath includeDir in project.Value.IncludeDirectories) {
            //        includeDir.Rebase(before, after);
            //    }
            //}
        }

        protected void ParseCompileDB(String filename) {
            // hope compiledb is not so large to eat all the memory
            String compiledbRaw = File.ReadAllText(filename);
            List<CompileDBEntry> entries = JsonConvert.DeserializeObject<List<CompileDBEntry>>(compiledbRaw);
            Int32 projectSerial = 1;
            foreach (CompileDBEntry entry in entries) {
                // get compiler path
                // we can't assume whether compilerPath is absolute or relative;
                // it's possible to enforce compiledb's '--full-path' but I prefer to stay somewhat
                // backwards-compatible.
                String compilerPath = entry.arguments[0];
                AbsoluteCrosspath workingDir = AbsoluteCrosspath.FromString(entry.directory);
                Compiler compiler = null;
                foreach (Compiler solutionCompiler in solutionCompilers) {
                    if (solutionCompiler.ExePath == compilerPath) {
                        // already registered
                        compiler = solutionCompiler;
                    }
                }

                if (compiler == null) {
                    compiler = new Compiler(compilerPath);
                    if (solutionCompilers.Add(compiler)) {
                        Logger.WriteLine(LogLevel.Info, $"New compiler '{compilerPath}'");
                    }
                }

                // get full file path
                AbsoluteCrosspath xpath = AbsoluteCrosspath.FromString(entry.file, workingDir);
                ProjectFile pf = new ProjectFile(xpath, compiler);
                Logger.WriteLine(LogLevel.Trace, $"===== file {xpath} =====");

                // parse arguments to obtain all -I, -D, -U
                // start from 1 to skip compiler name
                for (Int32 i = 1; i < entry.arguments.Count; i++) {
                    String arg = entry.arguments[i];
                    // includes:
                    // -I
                    // -iquote
                    // -isystem
                    // -idirafter
                    // ..., see https://gcc.gnu.org/onlinedocs/gcc/Directory-Options.html
                    // TODO: also process -nostdinc to control whether system include dirs should be added or not.
                    // TODO: preserve priority between -I, -iquote and other include dir types
                    IncludeDirectoryType idt = IncludeDirectoryType.Null;
                    String includeDirStr = String.Empty;
                    foreach (IncludeDirectoryType idk in IncludeParam.Keys) {
                        if (arg.StartsWith(IncludeParam[idk])) {
                            idt = idk;
                            // first try to cut current arg to process form of "-I/path/to/include"
                            includeDirStr = arg.Substring(IncludeParam[idk].Length);
                            // if it gave no info, take the whole next arg to process form of "-I /path/to/include"
                            if (includeDirStr.Length == 0) {
                                includeDirStr = entry.arguments[i + 1];
                                ++i;
                            }
                            break;
                        }
                    }

                    if (idt != IncludeDirectoryType.Null) {
                        // DONE: determine more accurately which include is local and which is remote
                        // this is done with the use of Solution.Rebase() so we customly point to local files
                        AbsoluteCrosspath includeDirPath = AbsoluteCrosspath.FromString(includeDirStr, workingDir);
                        String includeDirStrReconstructed = includeDirPath.ToString();
                        IncludeDirectory includeDir;
                        if (!solutionIncludeDirectories.ContainsKey(includeDirStrReconstructed)) {
                            includeDir = new IncludeDirectory(includeDirPath, idt);
                            solutionIncludeDirectories.Add(includeDirStrReconstructed, includeDir);
                            Logger.WriteLine(LogLevel.Debug, $"New include directory '{includeDirStrReconstructed}'");
                        }
                        else {
                            // if this include directory is already known, then drop current object and get old reference
                            includeDir = solutionIncludeDirectories[includeDirStrReconstructed];
                            Logger.WriteLine(LogLevel.Trace, $"Reusing include directory '{includeDirStrReconstructed}'");
                        }

                        // relax if we're adding the same include directory twice
                        // TODO: rewrite this using PriorityQueue to preserver order
                        pf.AddIncludeDir(includeDir);

                        continue;
                    }

                    // defines:
                    // -D
                    if (arg.StartsWith("-D")) {
                        // first try to cut current arg to process form of "-DLINUX"
                        String defineString = arg.Substring("-D".Length);
                        // if it gave no info, take the whole next arg to process form of "-D LINUX"
                        if (defineString.Length == 0) {
                            defineString = entry.arguments[i + 1];
                            ++i;
                        }

                        pf.SetCppDefine(defineString);
                        Logger.WriteLine(LogLevel.Trace, $"[i] Added -D{defineString}");
                        continue;
                    }

                    // undefines:
                    // -U
                    if (arg.StartsWith("-U")) {
                        String undefineString = arg.Substring("-U".Length);
                        if (undefineString.Length == 0) {
                            undefineString = entry.arguments[i + 1];
                            ++i;
                        }

                        pf.UnsetCppDefine(undefineString);
                        Logger.WriteLine(LogLevel.Trace, $"[i] Added -U{undefineString}");
                        // ReSharper disable once RedundantJumpStatement
                        continue;
                    }
                }

                if (!solutionFiles.Add(pf)) {
                    throw new ApplicationException("[x] Attempt to add the same ProjectFile multiple times");
                }

                // add to project files now?
                //pf.DumpData();
                Int64 projectHash = pf.HashProjectID();
                if (!projects.ContainsKey(projectHash)) {
                    projects.Add(projectHash, new Project(AllocateGuid(), $"Project_{projectSerial:D4}", compiler, pf.IncludeDirectories, pf.SetOfDefines));
                    ++projectSerial;
                }

                // add file to project
                if (!projects[projectHash].TestWhetherProjectFileBelongs(pf)) {
                    throw new ApplicationException(
                            $"[x] Could not add '{pf.FilePath}' to project '{projectHash}' - hash function error");
                }

                if (!projects[projectHash].AddProjectFile(pf)) {
                    throw new ApplicationException(
                            $"[x] Could not add '{pf.FilePath}' to project '{projectHash}' - already exists");
                }
            }

            Logger.WriteLine(LogLevel.Info, $"[i] Created a solution of {projects.Count} projects from {solutionFiles.Count} files");

            if (Logger.Level == LogLevel.Trace) {
                foreach (var projectKvp in projects) {
                    Logger.WriteLine(LogLevel.Trace, $"# Project ID {projectKvp.Key}");
                    foreach (var file in projectKvp.Value.ProjectFiles) {
                        Logger.WriteLine(LogLevel.Trace, $"# > {file.FilePath}");
                    }

                    using (var enumerator = projectKvp.Value.ProjectFiles.GetEnumerator()) {
                        enumerator.MoveNext();
                        enumerator.Current.DumpData();
                    }

                    Logger.WriteLine(LogLevel.Trace, "============================================");
                }
            }

            // put a breakpoint here
            //Console.Read();
        }

        public void RetrieveExtraInfoFromRemote(RemoteHost remote) {
            foreach (Compiler compiler in solutionCompilers) {
                compiler.ExtractAdditionalInfo(remote);
            }
        }

        public void DownloadCompilerIncludeDirectoriesFromRemote(RemoteHost remote, String dir) {
            String pwd = Directory.GetCurrentDirectory();
            Directory.CreateDirectory(dir);
            Directory.SetCurrentDirectory(dir);
            foreach (Compiler compiler in solutionCompilers) {
                compiler.DownloadStandardIncludeDirectories(remote);
            }
            Directory.SetCurrentDirectory(pwd);
        }

        // TODO: these WriteLine calls break encapsulation
        public void CheckForTotalRebase(ref List<IncludeDirectory> remoteNotRebased) {
            foreach (IncludeDirectory includeDir in solutionIncludeDirectories.Values) {
                if (includeDir.Flavor == CrosspathFlavor.Unix) {
                    Logger.WriteLine(LogLevel.Warning, $"the remote include directory '{includeDir}' was not rebased");
                    if (remoteNotRebased != null) {
                        remoteNotRebased.Add(includeDir);
                    }
                }
                else if (includeDir.Flavor == CrosspathFlavor.Windows) {
                    if (!Directory.Exists(includeDir.ToString())) {
                        Logger.WriteLine(LogLevel.Warning, $"directory '{includeDir}' does not exist after rebase");
                    }
                }
            }

            foreach (ProjectFile pf in solutionFiles) {
                if (pf.FilePath.Flavor == CrosspathFlavor.Unix) {
                    Logger.WriteLine(LogLevel.Warning, $"project file '{pf.FilePath}' was not rebased and won't be accessible");
                }
                else if (pf.FilePath.Flavor == CrosspathFlavor.Windows) {
                    if (!File.Exists(pf.FilePath.ToString())) {
                        Logger.WriteLine(LogLevel.Warning, $"project file '{pf.FilePath}' does not exist after rebase");
                    }
                }
            }
        }

        public void WriteToDirectory(String directory) {
            Directory.CreateDirectory(directory);
            String pwd = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(directory);

            AbsoluteCrosspath solutionDir = AbsoluteCrosspath.GetCurrentDirectory();

            foreach (Compiler compiler in solutionCompilers) {
                compiler.WriteToFile(solutionDir);
            }

            foreach (Project projectKvp in projects.Values) {
                // remember there will also be .vcxproj.filters
                projectKvp.WriteToFile(solutionDir);
            }

            // TODO: add them as "Solution Items"
            File.WriteAllText(SolutionStructure.ForcedIncludes.SolutionCompat, @"#pragma once");
            File.WriteAllText(SolutionStructure.ForcedIncludes.SolutionPostCompat, "#pragma once\n#undef _WIN32\n");
//#undef _MSC_VER
//#undef _MSC_FULL_VER
//#undef _MSC_BUILD
//#undef _MSC_EXTENSIONS
            // regarding compiler_compat.h:
            // generate one file per compiler and reference them from projects
            // see Compiler.WriteToFile() instead

            File.WriteAllBytes(SolutionStructure.SolutionPropsFilename, templates.SolutionProps);

            // write .sln itself
            // using simple text generator
            using (StreamWriter sw = new StreamWriter(SolutionStructure.SolutionFilename, false, Encoding.UTF8)) {
                sw.WriteLine(@"");
                sw.WriteLine(@"Microsoft Visual Studio Solution File, Format Version 12.00");
                sw.WriteLine(@"# Visual Studio Version 16");
                sw.WriteLine(@"VisualStudioVersion = 16.0.30804.86");
                sw.WriteLine(@"MinimumVisualStudioVersion = 10.0.40219.1");
                foreach (Project project in projects.Values) {
                    sw.WriteLine($@"Project(""{{{AllocateGuid()}}}"") = ""{project.Name}"", ""{project.Filename}"", ""{project.Guid}""");
                    sw.WriteLine("EndProject");
                }

                sw.WriteLine(@"Global");
                sw.WriteLine('\t' + @"GlobalSection(SolutionConfigurationPlatforms) = preSolution");
                foreach (String cfg in SolutionStructure.SolutionConfigurations) {
                    sw.WriteLine($"\t\t{cfg} = {cfg}");
                }

                sw.WriteLine(@"EndGlobalSection");
                sw.WriteLine('\t' + @"GlobalSection(ProjectConfigurationPlatforms) = postSolution");
                String[] cfgStages = {"ActiveCfg", "Build.0", "Deploy.0"};
                foreach (Project project in projects.Values) {
                    foreach (String cfg in SolutionStructure.SolutionConfigurations) {
                        foreach (String cfgStage in cfgStages) {
                            sw.WriteLine($"\t\t{{{project.Guid}}}.{cfg}.{cfgStage} = {cfg}");
                        }
                    }
                }

                sw.WriteLine("\tEndGlobalSection");
                sw.WriteLine("\tGlobalSection(SolutionProperties) = preSolution");
                sw.WriteLine("\t\tHideSolutionNode = FALSE");
                sw.WriteLine("\tEndGlobalSection");
                sw.WriteLine("\tGlobalSection(ExtensibilityGlobals) = postSolution");
                sw.WriteLine($"\t\tSolutionGuid = {{{selfGuid}}}");
                sw.WriteLine("\tEndGlobalSection");
                sw.WriteLine("EndGlobal");
                sw.Close();
            }

            Directory.SetCurrentDirectory(pwd);
        }
    }
}