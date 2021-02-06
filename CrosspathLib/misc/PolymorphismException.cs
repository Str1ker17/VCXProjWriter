using System;

namespace CrosspathLib {
    public class PolymorphismException : Exception {
        public PolymorphismException(String message) : base(message) {
        }
    }
}