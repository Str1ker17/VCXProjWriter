using System;

namespace VcxProjLib {
    public class Crosspath {
        protected String m_path = String.Empty;

        protected Crosspath(String path) {
            m_path = path;
        }

        public static Crosspath CreateFromUnixDir(String dir) {
            return new Crosspath(dir);
        }
    }
}