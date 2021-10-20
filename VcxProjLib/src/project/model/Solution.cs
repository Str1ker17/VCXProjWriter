﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CrosspathLib;
using Newtonsoft.Json;

namespace VcxProjLib {
    public class Solution {
        public static InternalConfiguration internalConfiguration = new InternalConfiguration {
            RelaxIncludeDirsOrder = false
        };

        /// <summary>
        /// The unique identifier of solution in terms of VS.
        /// </summary>
        protected Guid selfGuid;

        /// <summary>
        /// Solution is a set of projects.
        /// </summary>
        protected Dictionary<Int64, Project> projects;

        /// <summary>
        /// The (remote) directory in which most source files will be looked up for project structure.
        /// The rest will be placed to the special "External sources" folder.
        /// </summary>
        public AbsoluteCrosspath BaseDir { get; }

        // these containers have 2 targets:
        //   1. avoid duplicates;
        //   2. help in batch processing, like rebasing the whole solution from one directory to another.
        // regarding include directories:
        // since we can construct the same absolute path from different combination of relative path
        // and working directory, the 'String' is reconstructed abspath, which is unique.
        // TODO: use collection comparator instead to distinguish between same/different dirs
        // remember that solutionIncludeDirectories is provided for convenience of rebasing
        // but it does NOT need to be sorted and/or preserve order. go to Project for this
        // force includes are tracked as solution files too!
        protected HashSet<ProjectFile> solutionFiles;
        protected HashSet<Compiler> solutionCompilers;
        protected Dictionary<String, IncludeDirectory> solutionIncludeDirectories;

        protected static HashSet<Guid> acquiredGuids = new HashSet<Guid>();

        public Solution(Configuration config) {
            projects = new Dictionary<Int64, Project>();
            solutionFiles = new HashSet<ProjectFile>();
            solutionCompilers = new HashSet<Compiler>();
            solutionIncludeDirectories = new Dictionary<String, IncludeDirectory>();
            selfGuid = AllocateGuid();

            this.BaseDir = config.BaseDir;
        }

        public static Guid AllocateGuid() {
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

            if (BaseDir != null) {
                BaseDir.Rebase(before, after);
            }
        }

        public ProjectFile TrackFile(ProjectFile pf, Boolean allowDuplicate = false) {
            if (!solutionFiles.Add(pf)) {
                if (!allowDuplicate) {
                    throw new ApplicationException("[x] Attempt to add the same ProjectFile multiple times");
                }
            }

            return pf;
        }

        public IncludeDirectory TrackIncludeDirectory(AbsoluteCrosspath includeDirPath, IncludeDirectoryType idt) {
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

            return includeDir;
        }

        public void ParseCompileDB(String filename) {
            // hope compiledb is not so large to eat all the memory
            String compiledbRaw = File.ReadAllText(filename);
            List<CompileDBEntry> entries = JsonConvert.DeserializeObject<List<CompileDBEntry>>(compiledbRaw);
            foreach (CompileDBEntry entry in entries) {
                // get full file path
                AbsoluteCrosspath workingDir = AbsoluteCrosspath.FromString(entry.directory);
                AbsoluteCrosspath xpath = AbsoluteCrosspath.FromString(entry.file, workingDir);

                // TODO: filter out entries using include and exclude lists

                // get compiler path
                // we can't guess whether compilerPath is absolute or relative;
                // it's possible to enforce compiledb's '--full-path' but I prefer to stay somewhat
                // backwards-compatible.
                Crosspath compilerPath = Crosspath.FromString(entry.arguments[0]);
                Compiler compiler = null;
                foreach (Compiler solutionCompiler in solutionCompilers) {
                    if (solutionCompiler.ExePath.Equals(compilerPath)) {
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


                ProjectFile pf = new ProjectFile(this, xpath, compiler);
                Logger.WriteLine(LogLevel.Trace, $"===== file {xpath} =====");

                pf.AddInfoFromCommandLine(workingDir, entry.arguments);

                TrackFile(pf);

                // add to project files now?
                //pf.DumpData();
                Int64 projectHash = pf.HashProjectID();
                if (!projects.ContainsKey(projectHash)) {
                    projects.Add(projectHash, new Project(AllocateGuid(), compiler, pf.IncludeDirectories, pf.SetOfDefines, new HashSet<AbsoluteCrosspath>()));
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
            File.WriteAllText(SolutionStructure.ForcedIncludes.SolutionPostCompat, templates.SolutionCompat);

            // regarding compiler_compat.h:
            // generate one file per compiler and reference them from projects
            // see Compiler.WriteToFile() instead

            File.WriteAllBytes(SolutionStructure.SolutionPropsFilename, templates.SolutionProps);

            // write .sln itself
            // using simple text generator
            using (StreamWriter sw = new StreamWriter(SolutionStructure.SolutionFilename, false, Encoding.UTF8)) {
                sw.WriteLine("");
                sw.WriteLine("Microsoft Visual Studio Solution File, Format Version 12.00");
                sw.WriteLine("# Visual Studio Version 16");
                sw.WriteLine("VisualStudioVersion = 16.0.31613.86");
                sw.WriteLine("MinimumVisualStudioVersion = 10.0.40219.1");
                foreach (Project project in projects.Values) {
                    // this magic GUID is "Windows (Visual C++)" project type
                    sw.WriteLine($"Project(\"{{8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942}}\") = \"{project.Name}\", \"{project.Filename}\", \"{{{project.Guid.ToString().ToUpper()}}}\"");
                    sw.WriteLine("EndProject");
                }

                // this magic GUID is "Solution Folder"
                sw.WriteLine($"Project(\"{{2150E333-8FDC-42A3-9474-1A3956D46DE8}}\") = \"Compat files\", \"Compat files\", \"{{{AllocateGuid()}}}\"");
                sw.WriteLine("\tProjectSection(SolutionItems) = preProject");
                sw.WriteLine("\t\tsolution_compat.h = solution_compat.h");
                sw.WriteLine("\t\tsolution_post_compiler_compat.h = solution_post_compiler_compat.h");
                sw.WriteLine("\tEndProjectSection");
                sw.WriteLine("EndProject");

                sw.WriteLine("Global");
                sw.WriteLine("GlobalSection(SolutionConfigurationPlatforms) = preSolution");
                foreach (String cfg in SolutionStructure.SolutionConfigurations) {
                    sw.WriteLine($"\t\t{cfg}|{SolutionStructure.SolutionPlatformName} = {cfg}|{SolutionStructure.SolutionPlatformName}");
                }

                sw.WriteLine("EndGlobalSection");
                sw.WriteLine("GlobalSection(ProjectConfigurationPlatforms) = postSolution");
                String[] cfgStages = { "ActiveCfg", "Build.0", "Deploy.0" };
                foreach (Project project in projects.Values) {
                    foreach (String cfg in SolutionStructure.SolutionConfigurations) {
                        foreach (String cfgStage in cfgStages) {
                            sw.WriteLine($"\t\t{{{project.Guid.ToString().ToUpper()}}}.{cfg}|{SolutionStructure.SolutionPlatformName}.{cfgStage} = {cfg}|{project.Compiler.VSPlatform}");
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