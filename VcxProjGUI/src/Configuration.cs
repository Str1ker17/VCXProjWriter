using System;
using System.Collections.Generic;
using CrosspathLib;
using VcxProjLib;
using Newtonsoft.Json;

namespace VcxProjGUI {
    [Serializable]
    internal class Configuration {
        private String _file;
        public String File {
            get { return _file; }
            set {
                // TODO: be more restrictive
                _file = value;
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

        protected Configuration() {
            Substitutions = new List<Tuple<AbsoluteCrosspath, AbsoluteCrosspath>>();
            Remote = null;
        }

        public static Configuration Default() {
            return new Configuration {
                    File = "compile_commands.json"
                  , Outdir = "output"
            };
        }

        public static Configuration LoadFromFile(String filename) {
            return null;
        }

        public void SaveToFile(String filename) {

        }
    }
}