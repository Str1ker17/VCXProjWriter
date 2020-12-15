using System;
using System.Collections.Generic;

namespace VcxProjLib {
    public class Project {
        protected class DefineExactComparer : IEqualityComparer<Define> {
            public bool Equals(Define x, Define y) {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(x, null)) return false;
                if (ReferenceEquals(y, null)) return false;
                if (x.GetType() != y.GetType()) return false;
                return x.Name == y.Name && x.Value == y.Value;
            }

            public int GetHashCode(Define obj) {
                return obj.Name.GetHashCode();
            }
        }

        /// <summary>
        /// Not only extra include directories, but system too.
        /// PRESERVE ORDER
        /// </summary>
        public HashSet<String> IncludeDirectories { get; private set; }

        /// <summary>
        /// Undefines form defines, removing item from then.
        /// Does not require to preserve order.
        /// </summary>
        public HashSet<Define> Defines { get; private set; }

        public Dictionary<String, ProjectFile> ProjectFiles { get; private set; }


        public Project(HashSet<String> includeDirectories, HashSet<Define> defines) {
            // create copies, not references
            IncludeDirectories = new HashSet<String>(includeDirectories);
            Defines = new HashSet<Define>(defines, new DefineExactComparer());
            // initialize an empty set
            ProjectFiles = new Dictionary<String, ProjectFile>();
        }

        public bool AddProjectFile(ProjectFile pf) {
            if (ProjectFiles.ContainsKey(pf.FilePath)) {
                return false;
            }
            ProjectFiles.Add(pf.FilePath, pf);
            return true;
        }

        public bool TestWhetherProjectFileBelongs(ProjectFile pf) {
            return IncludeDirectories.SetEquals(pf.IncludeDirectories) && Defines.SetEquals(pf.Defines);
        }
    }
}