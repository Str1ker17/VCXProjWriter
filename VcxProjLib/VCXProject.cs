using System;

namespace VcxProjLib {
    public abstract class VCXProject {
        protected Guid ProjectGuid;

        protected VCXProject() {
            this.ProjectGuid = Guid.NewGuid();
        }
    }
}
