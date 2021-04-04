using System;

namespace VcxProjLib {
    public class InternalConfiguration {
        protected Boolean relaxIncludeDirsOrder;

        public Boolean RelaxIncludeDirsOrder {
            get { return relaxIncludeDirsOrder; }
            set { relaxIncludeDirsOrder = value; }
        }
    }
}