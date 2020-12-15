using System;
using System.Collections.Generic;

namespace VcxProjLib {
    public class ProjectFile {
        protected class DefineNameComparer : IEqualityComparer<Define> {
            public bool Equals(Define x, Define y) {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(x, null)) return false;
                if (ReferenceEquals(y, null)) return false;
                if (x.GetType() != y.GetType()) return false;
                return x.Name == y.Name;
            }

            public int GetHashCode(Define obj) {
                return obj.Name.GetHashCode();
            }
        }

        protected DefineNameComparer DefineNameComparerInstance;

        public String FilePath { get; private set; }
        public HashSet<String> IncludeDirectories { get; private set; }
        public HashSet<Define> Defines { get; private set; }

        public ProjectFile(String filePath) {
            IncludeDirectories = new HashSet<String>();
            DefineNameComparerInstance = new DefineNameComparer();
            Defines = new HashSet<Define>(DefineNameComparerInstance);
            FilePath = filePath;
        }

        public bool AddIncludeDir(String includeDir) {
            return IncludeDirectories.Add(includeDir);
        }

        public bool Define(String defineString) {
            return Defines.Add(new Define(defineString));
        }

        public bool Undefine(String undefineString) {
            return Defines.Remove(new Define(undefineString, VcxProjLib.Define.DefaultValue));
        }

        public void DumpData() {
            foreach (String inc in IncludeDirectories) {
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
            
            var hashIn = String.Empty.GetHashCode();
            foreach (var inc in IncludeDirectories) {
                hashIn += inc.GetHashCode();
            }

            foreach (var def in Defines) {
                hashIn += def.Name.GetHashCode() + def.Value.GetHashCode();
            }

            return hashIn;
        }
    }
}