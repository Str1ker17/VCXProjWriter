using System;
using System.IO;
using System.IO.Compression;
using CrosspathLib;

namespace VcxProjLib {
    public class IncludeDirectory : AbsoluteCrosspath, IComparable<IncludeDirectory> {
        public IncludeDirectoryType Type { get; }
        public String ShortName { get; }

        protected Boolean autoDownloaded = false;

        protected static UInt32 serial = 1;

        public IncludeDirectory(AbsoluteCrosspath path, IncludeDirectoryType type) : base(path) {
            Type = type;
            ShortName = $"{serial:D4}";
            ++serial;
        }

        protected static Int32 ConvertTypeToInt(IncludeDirectoryType type) {
            switch (type) {
                case IncludeDirectoryType.Quote: return 10;
                case IncludeDirectoryType.Generic: return 20;
                case IncludeDirectoryType.System: return 30;
                case IncludeDirectoryType.DirAfter: return 40;

                default: {
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="other"></param>
        /// <returns>-1 if this is lower than other; 0 if they are equal; 1 otherwise</returns>
        public Int32 CompareTo(IncludeDirectory other) {
            return ConvertTypeToInt(this.Type) - ConvertTypeToInt(other.Type);
        }

#if AGGREGATE_XPATH
        public override String ToString() {
            return xpath.ToString();
        }

        public void Rebase(AbsoluteCrosspath before, AbsoluteCrosspath after) {
            xpath.Rebase(before, after);
        }

        public CrosspathFlavor Flavor {
            get { return xpath.Flavor; }
        }
#endif

        public String GetLocalProjectPath(AbsoluteCrosspath solutionDir) {
            if (autoDownloaded) {
                return $@"$(SolutionDir)\{this.Relativized(solutionDir, true)}";
            }

            return this.ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="remote">Remote host to download from</param>
        /// <param name="localXpath">Local directory for remote include directories, e.g. D:\Project1\remote\192.168.0.1.</param>
        public void RebaseToLocal(RemoteHost remote, AbsoluteCrosspath localXpath) {
            if (this.Flavor == CrosspathFlavor.Windows) {
                return;
            }

            int xtractBufSize = 65536;
            Byte[] zipExtractBuf = new Byte[xtractBufSize];

            String remoteFilename = $"/tmp/{ShortName}.zip";
            String localDirectoryPattern = $@"include\{ShortName}";
            String localFilenamePattern = $"{ShortName}.zip";

            AbsoluteCrosspath xLocalIncludeDirectory = localXpath.Appended(RelativeCrosspath.FromString(localDirectoryPattern));
            String localIncludeDirectory = xLocalIncludeDirectory.ToString();
            String localFilename = localXpath.Appended(RelativeCrosspath.FromString(localFilenamePattern)).ToString();
            Logger.WriteLine(LogLevel.Info, $"Rolling ${this} into {localFilenamePattern}...");
            if (remote.Execute($"pushd {this} && zip -1 -r -q {remoteFilename} . && popd", out String result) != RemoteHost.Success) {
                Logger.WriteLine(LogLevel.Error, result);
                return;
            }

            Directory.CreateDirectory(localIncludeDirectory);
            remote.DownloadFile(remoteFilename, localFilename);
            File.WriteAllText(xLocalIncludeDirectory.Appended(RelativeCrosspath.FromString($@"..\{ShortName}_origin.txt")).ToString(), this.ToString());

            // not working bcz of NTFS case & special names restrictions. extract manually.
            //ZipFile.ExtractToDirectory(localFilename, localDirectory, Encoding.UTF8);

            using (FileStream fs = new FileStream(localFilename, FileMode.Open)) {
                ZipArchive za = new ZipArchive(fs, ZipArchiveMode.Read);
                foreach (ZipArchiveEntry zaEntry in za.Entries) {
                    RelativeCrosspath xRelPath = RelativeCrosspath.FromString(zaEntry.FullName);
                    AbsoluteCrosspath xPath = xLocalIncludeDirectory.Appended(xRelPath);
                    if (zaEntry.FullName.EndsWith("/")) {
                        // packed directory
                        continue;
                    }

                    String path = xPath.ToString();
                    if (File.Exists(path)) {
                        Logger.WriteLine(LogLevel.Warning, $"file '{zaEntry.FullName}' already exists as '{path}' - case problems?");
                        continue;
                    }

                    // hax, remove leading / from path
                    if (xRelPath.ToString() == Compiler.RemoteTempFile.Substring(1)) {
                        continue;
                    }

                    String dirname = new AbsoluteCrosspath(xPath).ToContainingDirectory().ToString();
                    try {
                        Directory.CreateDirectory(dirname);
                        // packed file
                        using (FileStream xfs = new FileStream(path, FileMode.CreateNew)) {
                            using (Stream zas = zaEntry.Open()) {
                                while (true) {
                                    int len = zas.Read(zipExtractBuf, 0, xtractBufSize);
                                    if (len == 0) {
                                        // EOF
                                        break;
                                    }

                                    xfs.Write(zipExtractBuf, 0, len);
                                }
                            }
                        }
                    }
                    catch (Exception e) {
                        Logger.WriteLine(LogLevel.Error, $"Could not extract '${zaEntry.FullName}': ${e.Message}");
                    }
                }
            }

            // if extraction went ok, we can remove files
            remote.Execute($"rm {remoteFilename}", out String _);
            File.Delete(localFilename);

            // since we definitely have a local copy of include directory, rebase on it
            this.Rebase(this, xLocalIncludeDirectory);
            autoDownloaded = true;
        }
    }
}
