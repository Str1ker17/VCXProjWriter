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
        private String _inputFile;
        public String InputFile {
            get { return _inputFile; }
            set {
                // TODO: be more restrictive
                _inputFile = value;
            }
        }

        private String _outdir;
        public String Outdir {
            get { return _outdir; }
            set {
                _outdir = value;
            }
        }

        public List<Tuple<AbsoluteCrosspath, AbsoluteCrosspath>> Substitutions { get; private set; }
        public RemoteHost Remote { get; private set; }

        // encapsulation sucks sometimes
        public AbsoluteCrosspath BaseDir;
        public List<Crosspath> IncludeFilesFrom;
        public List<Crosspath> ExcludeFilesFrom;

        protected Configuration() {
            Substitutions = new List<Tuple<AbsoluteCrosspath, AbsoluteCrosspath>>();
            Remote = null;
        }

        public static Configuration Default() {
            return new Configuration {
                    InputFile = "compile_commands.json"
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