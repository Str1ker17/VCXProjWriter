using System;
using System.Collections.Generic;
using CrosspathLib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;

namespace CrosspathUnitTests {
    [TestClass]
    public class CrosspathLibTest {
        [TestMethod]
        public void Polymorphism() {
            Crosspath cpath = Crosspath.FromString(@"/");
            AbsoluteCrosspath unused = cpath as AbsoluteCrosspath;
            RelativeCrosspath unused1 = cpath as RelativeCrosspath;
        }

        [TestMethod]
        public void DynamicReturn() {
            Crosspath cpath = Crosspath.FromString(@"/");
            if (cpath.Origin == CrosspathOrigin.Absolute) {
                Logger.LogMessage(((AbsoluteCrosspath) cpath).ToString());
            }
            else {
                Assert.Fail("it was absolute");
            }

            Crosspath cpath2 = Crosspath.FromString(@".");
            if (cpath2.Origin == CrosspathOrigin.Relative) {
                Logger.LogMessage(((RelativeCrosspath) cpath2).ToString());
            }
            else {
                Assert.Fail("it was relative");
            }
        }

        [TestMethod]
        public void UnixRoot() {
            Crosspath cpath = Crosspath.FromString(@"/");
            Assert.IsInstanceOfType(cpath, typeof(AbsoluteCrosspath));
            Assert.AreEqual(CrosspathFlavor.Unix, cpath.Flavor);
            Assert.AreEqual(CrosspathOrigin.Absolute, cpath.Origin);
            Assert.AreEqual(@"/", cpath.ToAbsolutizedString());
        }

        // I don't think this test is significant.
        /*
        [TestMethod]
        public void WindowsMinifiedRootDrive() {
            Crosspath cpath = Crosspath.FromString(@"\");
            Assert.IsInstanceOfType(cpath, typeof(AbsoluteCrosspath));
            Assert.AreEqual(CrosspathFlavor.Windows, cpath.Flavor);
            Assert.AreEqual(CrosspathOrigin.Absolute, cpath.Origin);
        }
        */

        [TestMethod]
        public void EmptyPathParse() {
            Crosspath cpath = Crosspath.FromString(@"");
            Assert.IsInstanceOfType(cpath, typeof(RelativeCrosspath));
            Assert.AreEqual(CrosspathFlavor.Unix, cpath.Flavor);
            Assert.AreEqual(CrosspathOrigin.Relative, cpath.Origin);
        }

        [TestMethod]
        public void EmptyPathProcess() {
            Crosspath cpath = Crosspath.FromString(@"");
            Assert.IsInstanceOfType(cpath, typeof(RelativeCrosspath));
            Assert.AreEqual(CrosspathFlavor.Unix, cpath.Flavor);
            Assert.AreEqual(CrosspathOrigin.Relative, cpath.Origin);
            try {
                cpath.ToAbsolutizedString();
                Assert.Fail("should fail");
            }
            catch (PolymorphismException) {
            }
        }

        [TestMethod]
        public void TestMethod3() {
            Crosspath cpath = Crosspath.FromString(@"/local/store/qemu");
            Assert.IsInstanceOfType(cpath, typeof(AbsoluteCrosspath));
            Assert.AreEqual(CrosspathFlavor.Unix, cpath.Flavor);
            Assert.AreEqual(CrosspathOrigin.Absolute, cpath.Origin);
            Assert.AreEqual(@"/local/store/qemu", ((AbsoluteCrosspath) cpath).ToString());
            // DONE: substitute (rebase) absolute paths
            Assert.AreEqual(@"D:\Projects\local\store\qemu"
              , ((AbsoluteCrosspath) cpath).Rebase(AbsoluteCrosspath.FromString("/")
                  , AbsoluteCrosspath.FromString(@"D:\Projects")).ToString());
        }

        [TestMethod]
        public void RelativeWithGarbage1() {
            Crosspath cpath = Crosspath.FromString(@"qemu/src/../inc");
            Assert.IsInstanceOfType(cpath, typeof(RelativeCrosspath));
            Assert.AreEqual(CrosspathFlavor.Unix, cpath.Flavor);
            Assert.AreEqual(CrosspathOrigin.Relative, cpath.Origin);
            Assert.AreEqual(@"qemu/inc", ((RelativeCrosspath) cpath).ToString());
        }

        /*
        [TestMethod]
        public void TestMethod5() {
            Crosspath cpath = new Crosspath(@"source.c");
            Assert.AreEqual(CrosspathFlavor.Unix, cpath.Flavor);
            Assert.AreEqual(CrosspathOrigin.Relative, cpath.Origin);
            try {
                cpath.Absolute.Get();
                Assert.Fail("accessing .Absolute should throw an exception");
            }
            catch (NotSupportedException e) {
                Assert.AreEqual("use GetBasedOn() instead", e.Message);
            }

            cpath.Relative.SetWorkingDirectory(new Crosspath("/"));
            Assert.AreEqual("/source.c", cpath.Relative.Absolutized().Absolute.Get());
        }
        */

        [TestMethod]
        public void TryCompileDBEntry() {
            String file = "source.c";
            String directory = "/path/to/file";

            // from Solution.cs
            Crosspath xpath = Crosspath.FromString(file);
            Crosspath xdir = Crosspath.FromString(directory);
            if (xpath is RelativeCrosspath relPath) {
                relPath.SetWorkingDirectory(xdir as AbsoluteCrosspath);
            }

            Assert.IsInstanceOfType(xpath, typeof(RelativeCrosspath));
            Assert.AreEqual(CrosspathOrigin.Relative, xpath.Origin);
            Assert.AreEqual(CrosspathFlavor.Unix, xpath.Flavor);
            Assert.IsInstanceOfType(xdir, typeof(AbsoluteCrosspath));
            Assert.AreEqual(CrosspathOrigin.Absolute, xdir.Origin);
            Assert.AreEqual(CrosspathFlavor.Unix, xdir.Flavor);

            Assert.AreEqual(@"/path/to/file/source.c", ((RelativeCrosspath) xpath).Absolutized().ToString());
        }

        /// <summary>
        /// Tests Crosspath.GetHashCode() and Crosspath.Equals() to be used in HashSet.
        /// </summary>
        [TestMethod]
        public void HashDefaultEquality() {
            HashSet<Crosspath> hs = new HashSet<Crosspath>();
            hs.Add(Crosspath.FromString("/usr/share/vcxprojwriter"));
            hs.Add(Crosspath.FromString("/usr/share/vcxprojwriter"));
            Assert.AreEqual(1, hs.Count);
        }

        [TestMethod]
        public void HashDiversedEquality() {
            HashSet<Crosspath> hs = new HashSet<Crosspath> {
                Crosspath.FromString("/tmp/VCXProjWriter")
              , AbsoluteCrosspath.FromString("/tmp/VCXProjWriter1")
              , RelativeCrosspath.FromString("tmp/VCXProjWriter")
            };
            RelativeCrosspath relativeCrosspath = RelativeCrosspath.FromString("VCXProjWriter");
            relativeCrosspath.SetWorkingDirectory(AbsoluteCrosspath.FromString("/tmp"));
            Boolean has_added = hs.Add(relativeCrosspath);
            Assert.AreEqual(false, has_added);
            Assert.AreEqual(3, hs.Count);
        }

        [TestMethod]
        public void RebaseFromUnixToWindowsTestFile() {
            Crosspath xpath = Crosspath.FromString("/local/store/bin-src/qemu/hw/display/trace.c");
            AbsoluteCrosspath before = Crosspath.FromString("/local/store/bin-src/qemu") as AbsoluteCrosspath;
            AbsoluteCrosspath after = Crosspath.FromString(@"D:\Workspace\Source\qemu") as AbsoluteCrosspath;
            AbsoluteCrosspath apath = ((AbsoluteCrosspath) xpath).Rebase(before, after);
            Assert.AreEqual(CrosspathFlavor.Windows, apath.Flavor);
            Assert.AreEqual('D', apath.WindowsRootDrive);
            Assert.AreEqual(@"D:\Workspace\Source\qemu\hw\display\trace.c", apath.ToString());
            Assert.AreEqual(@"D:\Workspace\Source\qemu\hw\display\trace.c", apath.ToAbsolutizedString());
        }

        [TestMethod]
        public void RebaseFromUnixToWindowsTestFull() {
            Crosspath xpath = Crosspath.FromString("/local/store/bin-src/qemu");
            AbsoluteCrosspath before = Crosspath.FromString("/local/store/bin-src/qemu") as AbsoluteCrosspath;
            AbsoluteCrosspath after = Crosspath.FromString(@"D:\Workspace\Source\qemu") as AbsoluteCrosspath;
            AbsoluteCrosspath apath = ((AbsoluteCrosspath) xpath).Rebase(before, after);
            Assert.AreEqual(CrosspathFlavor.Windows, apath.Flavor);
            Assert.AreEqual('D', apath.WindowsRootDrive);
            Assert.AreEqual(@"D:\Workspace\Source\qemu", apath.ToString());
            Assert.AreEqual(@"D:\Workspace\Source\qemu", apath.ToAbsolutizedString());
        }

        [TestMethod]
        public void GetContainingDirectory() {
            String filename1 = "lzcintrin.h";
            Crosspath xpath1 = Crosspath.FromString(filename1);
            xpath1.ToContainingDirectory();
            Assert.AreEqual(".", xpath1.ToString());

            String filename2 = "/lzcintrin.h";
            Crosspath xpath2 = Crosspath.FromString(filename2);
            xpath2.ToContainingDirectory();
            Assert.AreEqual("/", xpath2.ToString());

            String filename3 = "someinc/lzcintrin.h";
            Crosspath xpath3 = Crosspath.FromString(filename3);
            xpath3.ToContainingDirectory();
            Assert.AreEqual("someinc", xpath3.ToString());

            String filename4 = "/path/to/lzcintrin.h";
            Crosspath xpath4 = Crosspath.FromString(filename4);
            xpath4.ToContainingDirectory();
            Assert.AreEqual("/path/to", xpath4.ToString());
        }

        [TestMethod]
        public void AppendedTest() {
            AbsoluteCrosspath xLocalIncludeDirectory = AbsoluteCrosspath.GetCurrentDirectory();
            RelativeCrosspath xRelPath = RelativeCrosspath.FromString("lzcintrin.h");
            AbsoluteCrosspath xPath = xLocalIncludeDirectory.Appended(xRelPath);
            Assert.AreEqual(xPath.ToString(), xLocalIncludeDirectory + @"\" + xRelPath);
        }

        [TestMethod]
        public void RelativizeTest1() {
            AbsoluteCrosspath xIncludeDirectory = AbsoluteCrosspath.FromString("/local/store/bin-src/qemu");
            AbsoluteCrosspath xFile = AbsoluteCrosspath.FromString("/local/store/bin-src/qemu/hw/mips/serial.c");
            RelativeCrosspath relPath = xFile.Relativized(xIncludeDirectory);
            Assert.AreEqual("hw/mips/serial.c", relPath.ToString());
        }

        [TestMethod]
        public void RelativizeTest2() {
            AbsoluteCrosspath xIncludeDirectory = AbsoluteCrosspath.FromString("/local/store/bin-src/qemu");
            AbsoluteCrosspath xFile = AbsoluteCrosspath.FromString("/local/store/fast/bin-src/ccache/ccache.c");
            RelativeCrosspath relPath = xFile.Relativized(xIncludeDirectory);
            Assert.AreEqual("../../fast/bin-src/ccache/ccache.c", relPath.ToString());
        }

        [TestMethod]
        public void RelativizeTest3() {
            AbsoluteCrosspath xIncludeDirectory = AbsoluteCrosspath.FromString("/local/store/bin-src/qemu");
            AbsoluteCrosspath xFile = AbsoluteCrosspath.FromString("/local/store/fast/bin-src/ccache/ccache.c");
            try {
                RelativeCrosspath unused = xFile.Relativized(xIncludeDirectory, true);
                Assert.Fail("should fail");
            }
            catch (CrosspathLibException) {
            }
        }

        [TestMethod]
        public void RelativizeTestWin1() {
            AbsoluteCrosspath xIncludeDirectory = AbsoluteCrosspath.FromString(@"C:\Windows\system32\config");
            AbsoluteCrosspath xFile = AbsoluteCrosspath.FromString(@"C:\Program Files (x86)\Common Files\Microsoft");
            RelativeCrosspath relPath = xFile.Relativized(xIncludeDirectory);
            Assert.AreEqual(@"..\..\..\Program Files (x86)\Common Files\Microsoft", relPath.ToString());
        }

        [TestMethod]
        public void RelativizeTestWin2() {
            AbsoluteCrosspath xIncludeDirectory = AbsoluteCrosspath.FromString(@"C:\Windows\system32\config");
            AbsoluteCrosspath xFile = AbsoluteCrosspath.FromString(@"D:\Games\Call of Duty 2");
            try {
                RelativeCrosspath unused = xFile.Relativized(xIncludeDirectory);
                Assert.Fail("should fail");
            }
            catch {
                // ignored
            }
        }
    }
}