using System;
using System.IO;
using System.IO.Compression;
using CrosspathLib;

namespace VcxProjLib {
    public class IncludeDirectory : AbsoluteCrosspath {
        public IncludeDirectoryType Type { get; }
        public String ShortName { get; }

        protected static int serial = 1;

        public IncludeDirectory(AbsoluteCrosspath path, IncludeDirectoryType type) : base(path) {
            Type = type;
            ShortName = $"{serial:D4}";
            ++serial;
        }

        public void RebaseToLocal(RemoteHost remote, AbsoluteCrosspath localXpath) {
            if (Flavor == CrosspathFlavor.Windows) {
                return;
            }

            int xtractBufSize = 32768;
            Byte[] zipExtractBuf = new Byte[xtractBufSize];

            String remoteFilename = $"/tmp/{ShortName}.zip";
            String localDirectoryPattern = $@"include\{ShortName}";
            String localFilenamePattern = $"{ShortName}.zip";

            AbsoluteCrosspath xLocalIncludeDirectory = localXpath.Appended(RelativeCrosspath.FromString(localDirectoryPattern));
            String localIncludeDirectory = xLocalIncludeDirectory.ToString();
            String localFilename = localXpath.Appended(RelativeCrosspath.FromString(localFilenamePattern)).ToString();
            if (remote.Execute($"pushd {this} && zip -9 -r -q {remoteFilename} . && popd", out String result) != RemoteHost.Success) {
                Logger.WriteLine(LogLevel.Error, result);
                return;
            }

            Directory.CreateDirectory(localXpath.ToString());
            remote.DownloadFile(remoteFilename, localFilename);
            Directory.CreateDirectory(localIncludeDirectory);

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
            }

            // if extraction went ok, we can remove files
            remote.Execute($"rm {remoteFilename}", out String _);
            File.Delete(localFilename);

            // since we definitely have a local copy of include directory, rebase on it
            this.Rebase(this, AbsoluteCrosspath.FromString(localIncludeDirectory));
        }
    }
}
