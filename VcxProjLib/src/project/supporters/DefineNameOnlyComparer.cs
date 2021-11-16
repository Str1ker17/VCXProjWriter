using System;
using System.Collections.Generic;

namespace VcxProjLib {
    public class DefineNameOnlyComparer : IEqualityComparer<Define> {
        public Boolean Equals(Define x, Define y) {
            if (ReferenceEquals(x, y)) return true;
            if (ReferenceEquals(x, null)) return false;
            if (ReferenceEquals(y, null)) return false;
            if (x.GetType() != y.GetType()) return false;
            return x.Name == y.Name;
        }

        /// <summary>
        /// This method is intended only to speed up comparation.
        /// If two objects have identical hash codes, then Equals() is called.
        /// </summary>
        /// <param name="obj">Define object to calculate hash code</param>
        /// <returns></returns>
        public Int32 GetHashCode(Define obj) {
            return obj.Name.GetHashCode();
        }

        /* Implement Singleton pattern. */
        protected DefineNameOnlyComparer() {
        }

        protected static DefineNameOnlyComparer singletonInstance = new DefineNameOnlyComparer();

        public static DefineNameOnlyComparer Instance {
            get { return singletonInstance; }
        }
    }
}