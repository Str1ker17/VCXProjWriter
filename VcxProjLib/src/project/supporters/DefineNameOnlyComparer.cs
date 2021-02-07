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

        public Int32 GetHashCode(Define obj) {
            return obj.Name.GetHashCode();
        }
    }
}
