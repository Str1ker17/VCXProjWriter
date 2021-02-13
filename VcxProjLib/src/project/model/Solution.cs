using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CrosspathLib;
using Newtonsoft.Json;

namespace VcxProjLib {
    public class Solution {
        protected Dictionary<Int64, Project> projects;
        protected HashSet<ProjectFile> solutionFiles;
        protected HashSet<Compiler> solutionCompilers;
        // since we can construct the same absolute path from different combination of relative path
        // and working directory, the 'String' is reconstructed abspath, which is unique.
        // TODO: use collection comparator instead to distinguish between same/different dirs
        // remember that solutionIncludeDirectories is provided for convenience of rebasing
        // but it does NOT need to be sorted and/or preserve order. go to Project for this
        protected Dictionary<String, AbsoluteCrosspath> solutionIncludeDirectories;
        protected Dictionary<IncludeDirectoryType, String> includeParam;
        protected HashSet<Guid> acquiredGuids;
        protected Guid selfGuid;

        protected Solution() {
            projects = new Dictionary<Int64, Project>();
            solutionFiles = new HashSet<ProjectFile>();
            solutionCompilers = new HashSet<Compiler>();
            solutionIncludeDirectories = new Dictionary<String, AbsoluteCrosspath>();
            includeParam = new Dictionary<IncludeDirectoryType, String> {
                    {IncludeDirectoryType.Generic, "-I"}
                  , {IncludeDirectoryType.Quote, "-iquote"}
                  , {IncludeDirectoryType.System, "-isystem"}
                  , {IncludeDirectoryType.DirAfter, "-idirafter"}
            };
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
            foreach (KeyValuePair<String, AbsoluteCrosspath> includeDirPair in solutionIncludeDirectories) {
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
                String compilerPath = entry.arguments[0];
                Compiler compiler = null;
                foreach (Compiler solutionCompiler in solutionCompilers) {
                    if (solutionCompiler.ExePath == compilerPath) {
                        // already registered
                        compiler = solutionCompiler;
                    }
                }

                if (compiler == null) {
                    compiler = new Compiler(compilerPath);
                    solutionCompilers.Add(compiler);
                }

                if (solutionCompilers.Add(compiler)) {
                    Logger.WriteLine(LogLevel.Info, $"New compiler '{compilerPath}'");
                }

                // get full file path
                AbsoluteCrosspath xpath;
                Crosspath tmpXpath = Crosspath.FromString(entry.file);
                if (tmpXpath is RelativeCrosspath relXpath) {
                    relXpath.SetWorkingDirectory(Crosspath.FromString(entry.directory) as AbsoluteCrosspath);
                    xpath = relXpath.Absolutized();
                }
                else {
                    xpath = tmpXpath as AbsoluteCrosspath;
                }

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
                    foreach (IncludeDirectoryType idk in includeParam.Keys) {
                        if (arg.StartsWith(includeParam[idk])) {
                            idt = idk;
                            includeDirStr = arg.Substring(includeParam[idk].Length);
                            break;
                        }
                    }

                    if (idt != IncludeDirectoryType.Null) {
                        if (includeDirStr == "") {
                            // this is a bit hacky
                            if (entry.arguments[i + 1][0] == '-') {
                                // the next arg is an option itself; this is a build system failure so skip
                                continue;
                            }

                            includeDirStr = entry.arguments[i + 1];
                            ++i;
                        }

                        // DONE: determine more accurately which include is local and which is remote
                        // this is done with the use of Solution.Rebase() so we customly point to local files
                        AbsoluteCrosspath includeDir;
                        Crosspath includeDirTmp = Crosspath.FromString(includeDirStr);
                        if (includeDirTmp is RelativeCrosspath relIncludeDirEx) {
                            relIncludeDirEx.SetWorkingDirectory(Crosspath.FromString(entry.directory) as AbsoluteCrosspath);
                            includeDir = relIncludeDirEx.Absolutized();
                        }
                        else {
                            includeDir = includeDirTmp as AbsoluteCrosspath;
                        }

                        String includeDirStrReconstructed = includeDir.ToString();
                        if (!solutionIncludeDirectories.ContainsKey(includeDirStrReconstructed)) {
                            solutionIncludeDirectories.Add(includeDirStrReconstructed, includeDir);
                            Logger.WriteLine(LogLevel.Debug, $"New include directory '{includeDirStrReconstructed}'");
                        }
                        else {
                            // if this include directory is already known, then drop current object and get old reference
                            includeDir = solutionIncludeDirectories[includeDirStrReconstructed];
                        }

                        // relax if we're adding the same include directory twice
                        pf.AddIncludeDir(includeDir);

                        continue;
                    }

                    // defines:
                    // -D
                    if (arg.StartsWith("-D")) {
                        String defineString = arg.Substring("-D".Length);
                        if (defineString == "") {
                            if (entry.arguments[i + 1][0] == '-') {
                                // the next arg is an option itself; this is a build system failure so skip
                                continue;
                            }

                            defineString = entry.arguments[i + 1];
                            ++i;
                        }

                        pf.Define(defineString);
                        Logger.WriteLine(LogLevel.Trace, $"[i] Added -D{defineString}");
                        continue;
                    }

                    // undefines:
                    // -U
                    if (arg.StartsWith("-U")) {
                        String undefineString = arg.Substring("-U".Length);
                        if (undefineString == "") {
                            if (entry.arguments[i + 1][0] == '-') {
                                // the next arg is an option itself; this is a build system failure so skip
                                continue;
                            }

                            undefineString = entry.arguments[i + 1];
                            ++i;
                        }

                        pf.Undefine(undefineString);
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
                    projects.Add(projectHash, new Project(AllocateGuid(), $"Project_{projectSerial:D4}", compiler, pf.IncludeDirectories, pf.Defines));
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
                        Logger.WriteLine(LogLevel.Trace, $"# > {file.Value.FilePath}");
                    }

                    using (var enumerator = projectKvp.Value.ProjectFiles.GetEnumerator()) {
                        enumerator.MoveNext();
                        enumerator.Current.Value.DumpData();
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

        public void DownloadExtraInfoFromRemote(RemoteHost remote, String dir) {
            String pwd = Directory.GetCurrentDirectory();
            Directory.CreateDirectory(dir);
            Directory.SetCurrentDirectory(dir);
            foreach (Compiler compiler in solutionCompilers) {
                compiler.DownloadAdditionalInfo(remote);
            }
            Directory.SetCurrentDirectory(pwd);
        }

        // TODO: these WriteLine calls break encapsulation
        public void CheckForTotalRebase() {
            foreach (KeyValuePair<String, AbsoluteCrosspath> includeDirPair in solutionIncludeDirectories) {
                if (includeDirPair.Value.Flavor == CrosspathFlavor.Unix) {
                    Logger.WriteLine(LogLevel.Warning, $"the remote include directory '{includeDirPair.Value}' was not rebased");
                }
                else if (includeDirPair.Value.Flavor == CrosspathFlavor.Windows) {
                    if (!Directory.Exists(includeDirPair.Value.ToString())) {
                        Logger.WriteLine(LogLevel.Warning, $"directory '{includeDirPair.Value}' does not exist after rebase");
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

            foreach (Compiler compiler in solutionCompilers) {
                compiler.WriteToFile();
            }

            foreach (Project projectKvp in projects.Values) {
                // remember there will also be .vcxproj.filters
                projectKvp.WriteToFile();
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