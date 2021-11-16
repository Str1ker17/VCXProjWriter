using System;
using System.Collections.Generic;
using CrosspathLib;

namespace VcxProjLib {
    public class ProjectFile {
        /// <summary>
        /// Project file is a source file, which has a path.
        /// </summary>
        public AbsoluteCrosspath FilePath { get; }

        /// <summary>
        /// Project files are created in context of solution, so there's a link to owner.
        /// </summary>
        protected Solution OwnerSolution { get; }

        public RelativeCrosspath ProjectFolder { get; }

        // these properties are only valuable when grouping project files; do we need them inside the ProjectFile?
        public CompilerInstance CompilerOfFile { get; }
        public IncludeDirectoryList IncludeDirectories { get; }
        public Boolean DoNotUseStandardIncludeDirectories { get; }
        public Dictionary<String, Define> Defines { get; }
        public HashSet<Define> SetOfDefines { get; }
        public HashSet<AbsoluteCrosspath> ForceIncludes { get; }
        
        public ProjectFile(Solution sln, AbsoluteCrosspath filePath, CompilerInstance compilerInstance) {
            CompilerOfFile = compilerInstance;
            IncludeDirectories = new IncludeDirectoryList();
            DoNotUseStandardIncludeDirectories = false;
            Defines = new Dictionary<String, Define>();
            SetOfDefines = new HashSet<Define>(DefineExactComparer.Instance);
            ForceIncludes = new HashSet<AbsoluteCrosspath>();
            FilePath = filePath;
            OwnerSolution = sln;
            if (sln.config.BaseDir != null) {
                ProjectFolder = RelativeCrosspath.CreateRelativePath(filePath, sln.config.BaseDir, true).ToContainingDirectory() as RelativeCrosspath;
            }
        }

        public void AddIncludeDir(IncludeDirectory includeDir) {
            // TODO: preserve include directories order
            IncludeDirectories.AddIncludeDirectory(includeDir);
        }

        /// <summary>
        /// Apply a preprocessor define to a single source file.
        /// </summary>
        /// <param name="defineString">The whole define string without "-D",
        /// for instance, "_FORTIFY_SOURCE=2" or "FEATURE_WANTED".</param>
        public void SetCppDefine(String defineString) {
            Define newDefine = new Define(defineString);
            if (Defines.ContainsKey(newDefine.Name)) {
                Define oldDefine = Defines[newDefine.Name];
                if (oldDefine.Value != newDefine.Value) {
                    Logger.WriteLine(LogLevel.Warning, $"'{newDefine.Name}' redefined from '' to '{newDefine.Value}'");
                }
                SetOfDefines.Remove(oldDefine);
            }

            Defines[newDefine.Name] = newDefine;
            SetOfDefines.Add(newDefine);
        }

        public void UnsetCppDefine(String undefineString) {
            if (!Defines.ContainsKey(undefineString)) {
                return;
            }

            Define defineForRemoval = Defines[undefineString];
            SetOfDefines.Remove(defineForRemoval);
            Defines.Remove(undefineString);
        }

        public void DumpData() {
            foreach (IncludeDirectory inc in IncludeDirectories) {
                Console.WriteLine("-I{0}", inc);
            }

            foreach (Define def in Defines.Values) {
                if (def.Value == Define.DefaultValue)
                    Console.WriteLine("-D{0}", def.Name);
                else
                    Console.WriteLine("-D{0}={1}", def.Name, def.Value);
            }
        }

        /// <summary>
        /// This method is intented to calculate somewhat integer value to group project files faster.
        /// This time this is a very bad hash function
        /// </summary>
        /// <returns></returns>
        public Int64 HashProjectID() {
            Int64 hashIn = String.Empty.GetHashCode();
            foreach (IncludeDirectory inc in IncludeDirectories) {
                // Crosspath.GetHashCode() is too weak now. Do it ourselves.
                hashIn += inc.ToString().GetHashCode();
            }

            hashIn += CompilerOfFile.GetHashCode();

            foreach (Define def in Defines.Values) {
                hashIn += ((Int64)(def.Name.GetHashCode() + def.Value.GetHashCode())) << 32;
            }

            foreach (AbsoluteCrosspath forceInclude in ForceIncludes) {
                hashIn += ((Int64)forceInclude.ToString().GetHashCode()) << 21;
            }

            return hashIn;
        }

        // for SolutionFiles, only paths are necessary
#if FALSE
        public override int GetHashCode() {
            Int64 bigHash = HashProjectID();
            Int32 loPart = (Int32)(bigHash & 0xffffffff);
            Int32 hiPart = (Int32)(bigHash >> 32);
            return loPart ^ hiPart;
        }

        /// <summary>
        /// FIXME
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj) {
            if (obj == null) return false;
            Boolean eq = ((ProjectFile) obj).HashProjectID() == this.HashProjectID();
            return eq;
        }
#endif

        public override int GetHashCode() {
            return FilePath.GetHashCode();
        }

#if FALSE
        public override bool Equals(object obj) {
            if (obj == null) return false;
            Boolean eq = ((ProjectFile)obj).FilePath.Equals(this.FilePath);
            return eq;
        }
#endif

        public override String ToString() {
            return $"{CompilerOfFile.BaseCompiler.ExePath} {CompilerOfFile} {FilePath}";
        }

        protected static String TakeArg(List<String> args, ref int idx) {
            if ((idx + 1) >= args.Count) {
                if (idx < args.Count) {
                    throw new ApplicationException($"option '{args[idx]}' requires an argument");
                }
                throw new ApplicationException("TakeArg failed");
            }

            ++idx;
            return args[idx];
        }

        protected static bool TakeParamValue(List<String> args, ref int idx, String param, out String value) {
            if (!(args[idx].StartsWith(param))) {
                value = null;
                return false;
            }

            // first try to cut current arg to process form of "-DLINUX"
            String defineString = args[idx].Substring(param.Length);
            // if it gave no info, take the whole next arg to process form of "-D LINUX"
            if (defineString.Length == 0) {
                defineString = TakeArg(args, ref idx);
            }

            value = defineString;
            return true;
        }

        public void AddInfoFromCommandLine(AbsoluteCrosspath workingDir, List<String> args) {
            // parse arguments to obtain all -I, -D, -U
            // start from 1 to skip compiler name
            for (Int32 i = 1; i < args.Count; i++) {
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
                foreach (IncludeDirectoryType idk in IncludeDirectory.IncludeParam.Keys) {
                    if (TakeParamValue(args, ref i, IncludeDirectory.IncludeParam[idk], out includeDirStr)) {
                        idt = idk;
                        break;
                    }
                }

                if (idt != IncludeDirectoryType.Null) {
                    // DONE: determine more accurately which include is local and which is remote
                    // this is done with the use of Solution.Rebase() so we customly point to local files
                    AbsoluteCrosspath includeDirPath = AbsoluteCrosspath.FromString(includeDirStr, workingDir);
                    IncludeDirectory includeDir = OwnerSolution.TrackIncludeDirectory(includeDirPath, idt);

                    // relax if we're adding the same include directory twice
                    // TODO: rewrite this using PriorityQueue to preserver order
                    this.AddIncludeDir(includeDir);

                    continue;
                }

                // defines:
                // -D
                if (TakeParamValue(args, ref i, "-D", out String defineString)) {
                    this.SetCppDefine(defineString);
                    Logger.WriteLine(LogLevel.Trace, $"[i] Added -D{defineString}");
                    continue;
                }

                // undefines:
                // -U
                if (TakeParamValue(args, ref i, "-U", out String undefineString)) {
                    this.UnsetCppDefine(undefineString);
                    Logger.WriteLine(LogLevel.Trace, $"[i] Added -U{undefineString}");
                    continue;
                }

                if (TakeParamValue(args, ref i, "-include", out String forceInclude)) {
                    AbsoluteCrosspath forceIncludePath = AbsoluteCrosspath.FromString(forceInclude, workingDir);
                    ProjectFile forceIncludeProjectFile = this.OwnerSolution.TrackFile(new ProjectFile(OwnerSolution, forceIncludePath, this.CompilerOfFile), true);
                    this.ForceIncludes.Add(forceIncludeProjectFile.FilePath);
                    // ReSharper disable once RedundantJumpStatement
                    continue;
                }
            }
        }
    }
}