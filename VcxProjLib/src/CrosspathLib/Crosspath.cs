using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace VcxProjLib.CrosspathLib {
    public enum CrosspathOrigin {
        Absolute
      , Relative
    }

    public enum CrosspathFlavor {
        Windows
      , Unix
    }

    public partial class Crosspath {
        public String SourceString { get; private set; }
        public CrosspathOrigin Origin { get; private set; }
        public CrosspathFlavor Flavor { get; private set; }
        public char WindowsRootDrive { get; private set; } = 'A';
        
        protected List<String> directories = new List<String>();

        private AbsoluteCrosspath _absolute;
        private RelativeCrosspath _relative;

        public AbsoluteCrosspath Absolute {
            get {
                if (!IsAbsolute()) {
                    throw new NotSupportedException("access to 'Absolute' property of relative crosspath is forbidden");
                }
                return _absolute;
            }
        }

        public RelativeCrosspath Relative {
            get {
                if (IsAbsolute()) {
                    throw new NotSupportedException("access to 'Relative' property of absolute crosspath is forbidden");
                }
                return _relative;
            }
        }

        // we have to process all possible conditions:
        // relative and absolute paths;
        // Windows and Unix paths.
        // also we need some flavor-agnostic internal format to store paths

        protected Crosspath() { }

        protected void InitializeInners() {
            this._absolute = new AbsoluteCrosspath(this);
            this._relative = new RelativeCrosspath(this);
        }

        /// <summary>
        /// Creates a copy of Crosspath instance.
        /// </summary>
        /// <param name="xpath">Source instance.</param>
        public Crosspath(Crosspath xpath) {
            InitializeInners();

            this.SourceString = xpath.SourceString;
            this.Origin = xpath.Origin;
            this.Flavor = xpath.Flavor;
            this.WindowsRootDrive = xpath.WindowsRootDrive;
            
            this.directories = new List<string>(xpath.directories);
        }

        public Crosspath(String path) {
            InitializeInners();
            SourceString = path;

            // Windows supports both / and \
            // while Unix supports only / but uses \ for escaping
            do {
                if (path.Length >= 1 && path[0] == '/') {
                    this.Flavor = CrosspathFlavor.Unix;
                    this.Origin = CrosspathOrigin.Absolute;
                    break;
                }

                if (path.Length >= 2 && path[1] == ':') {
                    this.Flavor = CrosspathFlavor.Windows;
                    this.Origin = CrosspathOrigin.Absolute;
                    this.WindowsRootDrive = path[0];
                    break;
                }

                this.Origin = CrosspathOrigin.Relative;

                // fast check
                //bool has_forward_slash = false;
                for (int pos = 0; pos < path.Length; ++pos) {
                    if (path[pos] == '\\') {
                        // assume for now that Unix paths do not contain backslashes
                        this.Flavor = CrosspathFlavor.Windows;
                        goto detection_end;
                    }

                    //if (path[pos] == '/') {
                    //    has_forward_slash = true;
                    //}
                }

                //if (has_forward_slash) {
                //    this.Flavor = CrosspathFlavor.FlavorUnix;
                //}
                //else {
                //    this.Flavor = CrosspathFlavor.Compatible;
                //}
                // keep it simple instead
                this.Flavor = CrosspathFlavor.Unix;
            } while (false);

            detection_end:
            String[] parts;
            if (Flavor == CrosspathFlavor.Windows) {
                if (Origin == CrosspathOrigin.Absolute) {
                    path = path.Substring(2);
                }

                parts = path.Split(new[] {'/', '\\'}, StringSplitOptions.RemoveEmptyEntries);
            }
            else /* if (Flavor == CrosspathFlavor.FlavorUnix) */ {
                parts = path.Split(new[] {'/'}, StringSplitOptions.RemoveEmptyEntries);
            }

            foreach (String part in parts) {
                // TODO: check `part` for validity
                directories.Add(part);
            }
        }

        public override String ToString() {
            // beautify SourceString using current Origin and Flavor property values
            //return SourceString; // FIXME
            switch (Origin) {
                case CrosspathOrigin.Absolute:
                    return this._absolute.ToString();
                case CrosspathOrigin.Relative:
                    return this._relative.ToString();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public bool IsAbsolute() {
            return this.Origin == CrosspathOrigin.Absolute;
        }

        public String ToAbsolutizedString() {
            if (this.IsAbsolute()) {
                return this.Absolute.ToString();
            }

            return this.Relative.Absolutized().Absolute.ToString();
        }

        public Crosspath Append(Crosspath part) {
            if (part.IsAbsolute()) {
                throw new ArgumentOutOfRangeException(nameof(part), "appended part should be relative");
            }

            foreach (String dir in part.directories) {
                this.directories.Add(dir);
            }

            return this;
        }

        // https://docs.microsoft.com/en-us/previous-versions/visualstudio/visual-studio-2012/ms173120(v=vs.110)?redirectedfrom=MSDN

        public partial class AbsoluteCrosspath { }

        public partial class RelativeCrosspath { }
    }
}