using System;
using System.Collections.Generic;

namespace VcxProjLib {
    /// <summary>
    /// As far as I remember, Newtonsoft.Json requires pattern classes to be public
    /// </summary>
    public class CompileDBEntry {
        // ReSharper disable once InconsistentNaming
        public String directory { get; set; }

        // ReSharper disable once InconsistentNaming
        public List<String> arguments { get; set; }

        // ReSharper disable once InconsistentNaming
        public String file { get; set; }
    }
}
