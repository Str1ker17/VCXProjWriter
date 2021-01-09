using System;
using System.Text;

namespace CrosspathLib {
    public class RelativeCrosspath : Crosspath {
        internal static RelativeCrosspath CreateInstance() {
            return new RelativeCrosspath();
        }

        protected RelativeCrosspath() { }

        public AbsoluteCrosspath WorkingDirectory { get; private set; }

        public RelativeCrosspath(RelativeCrosspath source) : base(source) {
            this.WorkingDirectory = new AbsoluteCrosspath(WorkingDirectory);
        }

        public void SetWorkingDirectory(AbsoluteCrosspath workdir) {
            //if (workdir.Origin != CrosspathOrigin.Absolute) {
            //    throw new ArgumentOutOfRangeException(nameof(workdir), "should be absolute");
            //}

            this.WorkingDirectory = workdir;
        }

        public AbsoluteCrosspath Absolutized() {
            // assume that:
            // - self is relative
            // - working directory is absolute
            return new AbsoluteCrosspath(WorkingDirectory).Append(this) as AbsoluteCrosspath;
            
        }

        public override String ToString() {
            // filter out .. and .
            if (directories.Count == 0) {
                return string.Empty;
            }

            // we need to inverse stack before output
            StringBuilder sb = new StringBuilder();
            foreach (String dir in directories) {
                sb.Append(dir);
                switch (Flavor) {
                    case CrosspathFlavor.Windows:
                        sb.Append('\\');
                        break;
                    case CrosspathFlavor.Unix:
                        sb.Append('/');
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            // cut trailing (back)slash
            return sb.ToString(0, sb.Length - 1);
        }

        public override String ToAbsolutizedString() {
            if (WorkingDirectory is null) {
                throw new CrosspathLibPolymorphismException("attempt to absolutize RelativePath without a WorkingDirectory");
            }

            return (new AbsoluteCrosspath(WorkingDirectory).Append(this) as AbsoluteCrosspath).ToString();
        }
    }
}
