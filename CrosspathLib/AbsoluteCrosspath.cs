using System;
using System.Collections.Generic;
using System.Text;

namespace CrosspathLib {
    public class AbsoluteCrosspath : Crosspath {
        internal static AbsoluteCrosspath CreateInstance() {
            return new AbsoluteCrosspath();
        }

        /// <summary>
        /// Only for internal usage
        /// </summary>
        protected AbsoluteCrosspath() {
        }

        /// <summary>
        /// Creates a copy of instance.
        /// </summary>
        /// <param name="source"></param>
        public AbsoluteCrosspath(AbsoluteCrosspath source) : base(source) {
        }

        /// <summary>
        /// Returns full (absolute) path as a string.
        /// </summary>
        /// <returns></returns>
        public override String ToString() {
            if (Directories.Count == 0) {
                return "/";
            }

            StringBuilder sb = new StringBuilder();
            switch (Flavor) {
                case CrosspathFlavor.Windows:
                    sb.Append(WindowsRootDrive);
                    sb.Append(':');
                    break;
                case CrosspathFlavor.Unix:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            foreach (String dir in Directories) {
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

                sb.Append(dir);
            }

            return sb.ToString();
        }

        /// <summary>
        /// This call is identical to AbsoluteCrosspath.ToString(). It was placed here to support type-agnostic absolute path extraction.
        /// </summary>
        /// <returns></returns>
        public override String ToAbsolutizedString() {
            return ToString();
        }

        /// <summary>
        /// Replaces the beginning of path.
        /// </summary>
        /// <param name="oldBase"></param>
        /// <param name="newBase"></param>
        /// <returns></returns>
        public AbsoluteCrosspath Rebase(AbsoluteCrosspath oldBase, AbsoluteCrosspath newBase) {
            if (Directories.Count < oldBase.Directories.Count) {
                return this;
            }

            if (Directories.Count == 0) {
            }

            Boolean cantRebase = false;
            var myIter = Directories.GetEnumerator();
            var theirsIter = oldBase.Directories.GetEnumerator();
            for (int idx = 0; idx < oldBase.Directories.Count; idx++) {
                myIter.MoveNext();
                theirsIter.MoveNext();
                if (myIter.Current != theirsIter.Current) {
                    cantRebase = true;
                    break;
                }
            }

            myIter.Dispose();
            theirsIter.Dispose();

            if (cantRebase) {
                return this;
            }

            // rebasing
            if (Directories.Count == oldBase.Directories.Count) {
                // full rebase, replacing the whole path
                Directories = new LinkedList<String>(newBase.Directories);
            }
            else {
                for (int idx = 0; idx < oldBase.Directories.Count; idx++) {
                    Directories.RemoveFirst();
                }

                LinkedListNode<String> lln = Directories.First;

                foreach (String dir in newBase.Directories) {
                    Directories.AddBefore(lln, dir);
                }
            }

            // rerooting
            if (newBase.Flavor == CrosspathFlavor.Windows) {
                WindowsRootDrive = newBase.WindowsRootDrive;
            }

            Flavor = newBase.Flavor;

            return this;
        }
    }
}