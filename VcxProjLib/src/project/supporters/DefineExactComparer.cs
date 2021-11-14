using System;
using System.Collections.Generic;

namespace VcxProjLib {
    public class DefineExactComparer : IEqualityComparer<Define> {
        public Boolean Equals(Define x, Define y) {
            if (ReferenceEquals(x, y)) return true;
            if (ReferenceEquals(x, null)) return false;
            if (ReferenceEquals(y, null)) return false;
            if (x.GetType() != y.GetType()) return false;
            return x.Name == y.Name && x.Value == y.Value;
        }

        /// <summary>
        /// This method is intended only to speed up comparation.
        /// If two objects have identical hash codes, then Equals() is called.
        /// </summary>
        /// <param name="obj">Define object to calculate hash code</param>
        /// <returns></returns>
        public Int32 GetHashCode(Define obj) {
            return obj.Name.GetHashCode() ^ obj.Value.GetHashCode();
        }

        /* Implement Singleton pattern. */
        protected DefineExactComparer() {
        }

        protected static DefineExactComparer singletonInstance = new DefineExactComparer();

        public static DefineExactComparer Instance {
            get { return singletonInstance; }
        }
    }
}