using System;
using System.Collections.Generic;
using CrosspathLib;

namespace VcxProjLib {
    public class ProjectFile {
        /// <summary>
        /// Project file is a source file, which has a path.
        /// </summary>
        public AbsoluteCrosspath FilePath { get; }

        // these properties are only valuable when grouping project files; do we need them inside the ProjectFile?
        public Compiler Compiler { get; }
        public IncludeDirectoryList IncludeDirectories { get; }
        public Dictionary<String, Define> Defines { get; }
        public HashSet<Define> SetOfDefines { get; }

        // generate compiledb with '--full-path' for this to work
        public ProjectFile(AbsoluteCrosspath filePath, Compiler compiler) {
            IncludeDirectories = new IncludeDirectoryList();
            Compiler = compiler;
            Defines = new Dictionary<String, Define>();
            SetOfDefines = new HashSet<Define>(Define.ExactComparer);
            FilePath = filePath;
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
                hashIn += inc.GetHashCode();
            }

            foreach (Define def in Defines.Values) {
                hashIn += ((Int64)(def.Name.GetHashCode() + def.Value.GetHashCode())) << 32;
            }

            return hashIn;
        }

        public override String ToString() {
            return $"{Compiler.ExePath} {FilePath}";
        }
    }
}