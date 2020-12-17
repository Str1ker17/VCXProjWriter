using System;

namespace VcxProjLib {
    public abstract class VCXProject {
        protected Guid ProjectGuid { get; private set; }

        protected VCXProject() {
            this.ProjectGuid = Guid.NewGuid();
        }
    }
}