using System;
using System.Collections.Generic;

namespace VcxProjLib {
    public class CompilerPossiblyRelativePathComparer : IEqualityComparer<Compiler> {
        public Boolean Equals(Compiler x, Compiler y) {
            if (ReferenceEquals(x, y)) return true;
            if (ReferenceEquals(x, null)) return false;
            if (ReferenceEquals(y, null)) return false;
            if (x.GetType() != y.GetType()) return false;
            return x.ExePath.ToString() == y.ExePath.ToString();
        }

        /// <summary>
        /// This method is intended only to speed up comparation.
        /// If two objects have identical hash codes, then Equals() is called.
        /// </summary>
        /// <param name="obj">Define object to calculate hash code</param>
        /// <returns></returns>
        public Int32 GetHashCode(Compiler obj) {
            return obj.ExePath.ToString().GetHashCode();
        }
        
        /* Implement Singleton pattern. */
        protected CompilerPossiblyRelativePathComparer() { }

        protected static CompilerPossiblyRelativePathComparer singletonInstance = new CompilerPossiblyRelativePathComparer();

        public static CompilerPossiblyRelativePathComparer Instance {
            get {
                return singletonInstance;
            }
        }
    }
}