using System;
using System.Collections.Generic;
using System.IO;
using CrosspathLib;
using Newtonsoft.Json;

namespace VcxProjLib {
    /// <summary>
    /// This is NOT Visual Studio Solution/Project configuration.
    /// This is configuration of project generation.
    /// </summary>
    [Serializable]
    public class Configuration {
        public String InputFile { get; set; }
        public String Outdir { get; set; }
        
        public Boolean RandomizeOutdir { get; set; } = false;
        public Boolean OpenSolution { get; set; } = false;

        public List<Tuple<AbsoluteCrosspath, AbsoluteCrosspath>> Substitutions { get; private set; }
        public RemoteHost Remote { get; private set; }

        public Boolean RelaxIncludeDirsOrder { get; set; }

        // encapsulation sucks sometimes

        /// <summary>
        /// The (remote) directory in which most source files will be looked up for project structure.
        /// The rest will be placed to the special "External sources" folder.
        /// </summary>
        public AbsoluteCrosspath BaseDir;

        public List<Crosspath> IncludeFilesFrom;
        public List<Crosspath> ExcludeFilesFrom;
        public HashSet<Define> OverrideDefines = new HashSet<Define>(DefineNameOnlyComparer.Instance);
        public HashSet<Crosspath> IncludeCompilers;
        public HashSet<Crosspath> ExcludeCompilers = new HashSet<Crosspath>();

        protected Configuration() {
            Substitutions = new List<Tuple<AbsoluteCrosspath, AbsoluteCrosspath>>();
            Remote = null;
        }

        public static Configuration Default() {
            return new Configuration {
                    InputFile = null
                  , Outdir = "output"
            };
        }

        public static Configuration LoadFromFile(String filename) {
            return JsonConvert.DeserializeObject<Configuration>(File.ReadAllText(filename));
        }

        public void SaveToFile(String filename) {
            File.WriteAllText(filename, JsonConvert.SerializeObject(this, Formatting.Indented));
        }

        public void AssignRemote(RemoteHost remote) {
            Remote = remote;
        }
    }
}