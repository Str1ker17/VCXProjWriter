using System;
using System.Collections.Generic;
using CrosspathLib;

namespace VcxProjLib {
    public class ProjectFile {
        protected static DefineNameOnlyComparer defineNameOnlyComparerInstance = new DefineNameOnlyComparer();

        public AbsoluteCrosspath FilePath { get; }
        public Compiler Compiler { get; }
        public HashSet<AbsoluteCrosspath> IncludeDirectories { get; }
        public HashSet<Define> Defines { get; }

        // generate compiledb with --full-path for this to work
        public ProjectFile(AbsoluteCrosspath filePath, Compiler compiler) {
            IncludeDirectories = new HashSet<AbsoluteCrosspath>();
            Compiler = compiler;
            Defines = new HashSet<Define>(defineNameOnlyComparerInstance);
            FilePath = filePath;
        }

        public Boolean AddIncludeDir(AbsoluteCrosspath includeDir) {
            return IncludeDirectories.Add(includeDir);
        }

        public Boolean Define(String defineString) {
            Define newDefine = new Define(defineString);
            if (Defines.Contains(newDefine)) {
                Console.WriteLine("[!] warning: '{0}' redefined to '{1}'", newDefine.Name, newDefine.Value);
                Defines.Remove(newDefine);
            }

            return Defines.Add(newDefine);
        }

        public Boolean Undefine(String undefineString) {
            return Defines.Remove(new Define(undefineString, VcxProjLib.Define.DefaultValue));
        }

        public void DumpData() {
            foreach (AbsoluteCrosspath inc in IncludeDirectories) {
                Console.WriteLine("-I{0}", inc);
            }

            foreach (Define def in Defines) {
                if (def.Value == "")
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
            Int64 hashIn = string.Empty.GetHashCode();
            foreach (var inc in IncludeDirectories) {
                hashIn += inc.GetHashCode();
            }

            foreach (var def in Defines) {
                hashIn += def.Name.GetHashCode() + def.Value.GetHashCode();
            }

            return hashIn;
        }

        protected class DefineNameOnlyComparer : IEqualityComparer<Define> {
            public Boolean Equals(Define x, Define y) {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(x, null)) return false;
                if (ReferenceEquals(y, null)) return false;
                if (x.GetType() != y.GetType()) return false;
                return x.Name == y.Name;
            }

            public Int32 GetHashCode(Define obj) {
                return obj.Name.GetHashCode();
            }
        }
    }
}