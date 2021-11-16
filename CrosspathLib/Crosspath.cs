using System;
using System.Collections;
using System.Collections.Generic;

namespace CrosspathLib {
    public enum CrosspathOrigin {
        Absolute
      , Relative
    }

    public enum CrosspathFlavor {
        Windows
      , Unix
    }

    public abstract class Crosspath : IEnumerable<String> {
        public String SourceString { get; private set; }
        public CrosspathOrigin Origin { get; private set; }
        public CrosspathFlavor Flavor { get; protected set; }
        public Char WindowsRootDrive { get; protected set; }
        public String LastEntry { get { return directories.Last.Value; } }

        internal LinkedList<String> directories;

        // we have to process all possible conditions:
        // relative and absolute paths;
        // Windows and Unix paths.
        // also we need some flavor-agnostic internal format to store paths

        protected static int _serial_seq = 1;
        protected static List<Crosspath> allObjects = new List<Crosspath>();
        protected static HashSet<Crosspath> allObjectsSet = new HashSet<Crosspath>();
        protected int serial;
        /// <summary>
        /// This constructor is hidden from the outside of classes
        /// </summary>
        protected Crosspath() {
            serial = _serial_seq;
            ++_serial_seq;
            allObjects.Add(this);
        }

        /// <summary>
        /// Creates a copy of Crosspath instance.
        /// </summary>
        /// <param name="xpath">Source instance.</param>
        protected Crosspath(Crosspath xpath) : this() {
            SourceString = xpath.SourceString;
            Origin = xpath.Origin;
            Flavor = xpath.Flavor;
            WindowsRootDrive = xpath.WindowsRootDrive;

            directories = new LinkedList<String>(xpath.directories);
        }

        protected static void DetectParams(String path, out CrosspathOrigin origin, out CrosspathFlavor flavor, out Char rootDrive) {
            // Windows supports both / and \
            // while Unix supports only / but uses \ for escaping
            rootDrive = 'A';
            if (path.Length >= 1 && path[0] == '/') {
                flavor = CrosspathFlavor.Unix;
                origin = CrosspathOrigin.Absolute;
                return;
            }

            if (path.Length >= 2 && path[1] == ':') {
                flavor = CrosspathFlavor.Windows;
                origin = CrosspathOrigin.Absolute;
                rootDrive = path[0];
                return;
            }

            // TODO: support also UNC paths, like \\192.168.0.1\share

            origin = CrosspathOrigin.Relative;

            // fast check
            foreach (Char c in path) {
                if (c == '\\') {
                    // assume for now that Unix paths do not contain backslashes
                    flavor = CrosspathFlavor.Windows;
                    return;
                }
            }

            // keep it simple
            flavor = CrosspathFlavor.Unix;
        }

        /// <summary>
        /// Create a generic Crosspath object, which is actually an
        /// AbsoluteCrosspath or RelativeCrosspath depending on the input string.
        /// </summary>
        /// <param name="path">Any path string.</param>
        /// <returns>Crosspath object.</returns>
        public static Crosspath FromString(String path) {
            DetectParams(path, out CrosspathOrigin origin, out CrosspathFlavor flavor, out Char rootDrive);
            Crosspath xpath;

            switch (origin) {
                case CrosspathOrigin.Absolute:
                    xpath = AbsoluteCrosspath.CreateInstance();
                    break;
                case CrosspathOrigin.Relative:
                    xpath = RelativeCrosspath.CreateInstance();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            xpath.Origin = origin;
            xpath.Flavor = flavor;
            xpath.WindowsRootDrive = rootDrive;
            xpath.SourceString = path;
            xpath.directories = new LinkedList<String>();

            // push directories
            String[] parts;
            if (flavor == CrosspathFlavor.Windows) {
                if (origin == CrosspathOrigin.Absolute) {
                    path = path.Substring(2);
                }

                parts = path.Split(new[] {'/', '\\'}, StringSplitOptions.RemoveEmptyEntries);
            }
            else /* if (Flavor == CrosspathFlavor.FlavorUnix) */ {
                parts = path.Split(new[] {'/'}, StringSplitOptions.RemoveEmptyEntries);
            }

            foreach (String part in parts) {
                // TODO: check `part` for validity
                xpath.Chdir(part);
            }

            return xpath;
        }

        /// <summary>
        /// Change directory for a single named path component.
        /// </summary>
        /// <param name="dir">A single dir component.</param>
        protected void Chdir(String dir) {
            if (dir == ".")
                return;
            if (dir == "..") {
                if (directories.Count == 0) {
                    if (Origin == CrosspathOrigin.Absolute) {
                        throw new CrosspathLibException("attempt to go out of absolute path");
                    }

                    // DONE: handle out-of-tree paths more precisely
                    // allow putting ".." to the beginning of relative paths
                    directories.AddLast(dir);
                    return;
                }

                if (directories.Last.Value == "..") {
                    if (Origin == CrosspathOrigin.Relative) {
                        directories.AddLast(dir);
                        return;
                    }
                }

                // this is the main code branch
                directories.RemoveLast();
                return;
            }

            directories.AddLast(dir);
        }

        public Crosspath Append(RelativeCrosspath part) {
            foreach (String dir in part.directories) {
                Chdir(dir);
            }

            return this;
        }

        /// <summary>
        /// Removes last entry if exists and returns self.
        /// This is useful to get containing directory.
        /// </summary>
        /// <returns>Modified self object</returns>
        public virtual Crosspath ToContainingDirectory() {
            if (directories.Count > 0) {
                directories.RemoveLast();
            }

            return this;
        }

        public IEnumerator<String> GetEnumerator() {
            return directories.GetEnumerator();
        }
        
        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        /// <summary>
        /// A generic API to get an absolute path of any Crosspath object.
        /// </summary>
        /// <returns>Absolute string; otherwise exception.</returns>
        public abstract String ToAbsolutizedString();

        public override String ToString() {
            return $"(generic crosspath of {directories.Count} dirs)";
        }

        /// <summary>
        /// This is tricky. Enabling AbsoluteCrosspath and RelativeCrosspath to be equal
        /// limits count of internals that still remain different.
        /// </summary>
        /// <returns></returns>
        public override Int32 GetHashCode() {
            //return ToAbsolutizedString().GetHashCode();
            return Flavor.GetHashCode() + LastEntry.GetHashCode();
        }

        /// <summary>
        /// This is to find out which _paths_ are identical in filesystem.
        /// This is the base equality comparer; to compare AbsoluteCrosspath and RelativeCrosspath,
        /// they must implement it ourselves, or `Origin' field break it.
        /// WARNING: this override leads to situation when multiple Crosspath instances are
        /// equal, despite of being constructed from different working directories.
        /// </summary>
        /// <param name="obj">Object to compare with.</param>
        /// <returns>True if objects represent the same path in filesystem; false otherwise.</returns>
        public override Boolean Equals(Object obj) {
            if (!(obj is Crosspath crosspath)) {
                return false;
            }

            return this.Origin == crosspath.Origin && this.Flavor == crosspath.Flavor &&
                   this.WindowsRootDrive == crosspath.WindowsRootDrive &&
                   LinkedListEquality.Equals<String>(this.directories, crosspath.directories);
        }
    }
}
