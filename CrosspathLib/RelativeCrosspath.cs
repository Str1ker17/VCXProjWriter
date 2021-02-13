using System;
using System.Text;

namespace CrosspathLib {
    public class RelativeCrosspath : Crosspath {
        protected internal static RelativeCrosspath CreateInstance() {
            return new RelativeCrosspath();
        }

        protected RelativeCrosspath() {
        }

        public AbsoluteCrosspath WorkingDirectory { get; protected set; }

        /// <summary>
        /// Creates a copy of instance.
        /// </summary>
        /// <param name="source">Source RelativeCrosspath object, which will remain untouched.</param>
        public RelativeCrosspath(RelativeCrosspath source) : base(source) {
            this.WorkingDirectory = source.WorkingDirectory;
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

        public AbsoluteCrosspath Absolutized(AbsoluteCrosspath root) {
            return new AbsoluteCrosspath(root).Append(this) as AbsoluteCrosspath;
        }

        /// <summary>
        /// Creates RelativeCrosspath from string.
        /// If string does not contain a relative path, then throw an exception.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public new static RelativeCrosspath FromString(String path) {
            Crosspath xpath = Crosspath.FromString(path);
            if (!(xpath is RelativeCrosspath)) {
                throw new CrosspathLibException("the path provided is not relative");
            }
            return xpath as RelativeCrosspath;
        }

        public override String ToString() {
            // filter out .. and .
            if (directories.Count == 0) {
                return ".";
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
                throw new PolymorphismException(
                        "attempt to absolutize RelativePath without a WorkingDirectory");
            }

            return (new AbsoluteCrosspath(WorkingDirectory).Append(this) as AbsoluteCrosspath).ToString();
        }
    }
}
