using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrosspathLib;

namespace VcxProjLib {
    enum FileSystemEntryType {
        File,
        Directory,
    };

    enum ProjectEntryType {
        SourceFile,
        HeaderFile,
        IncludeDirectory,
    }

    class FileSystemEntry : AbsoluteCrosspath {

    }
}