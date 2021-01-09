using System;
using System.Collections.Generic;
using Microsoft.Build.Logging;
using VcxProjLib.CrosspathLib;

namespace VcxProjLib {
    public class ProjectFile {
        protected static DefineNameComparer DefineNameComparerInstance = new DefineNameComparer();

        public Crosspath FilePath { get; private set; }
        public HashSet<String> IncludeDirectories { get; private set; }
        public HashSet<Define> Defines { get; private set; }

        public ProjectFile(Crosspath filePath) {
            IncludeDirectories = new HashSet<String>();
            Defines = new HashSet<Define>(DefineNameComparerInstance);
            FilePath = new Crosspath(filePath);
        }

        public bool AddIncludeDir(String includeDir) {
            return IncludeDirectories.Add(includeDir);
        }

        public bool Define(String defineString) {
            Define newDefine = new Define(defineString);
            if (Defines.Contains(newDefine)) {
                Console.WriteLine("[!] warning: '{0}' redefined to '{1}'", newDefine.Name, newDefine.Value);
                Defines.Remove(newDefine);
            }

            return Defines.Add(newDefine);
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
            Int64 hashIn = String.Empty.GetHashCode();
            foreach (var inc in IncludeDirectories) {
                hashIn += inc.GetHashCode();
            }

            foreach (var def in Defines) {
                hashIn += def.Name.GetHashCode() + def.Value.GetHashCode();
            }

            return hashIn;
        }

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
    }
}