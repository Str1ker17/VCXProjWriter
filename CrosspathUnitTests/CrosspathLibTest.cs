using System;
using System.Collections.Generic;
using CrosspathLib;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrosspathUnitTests {
    [TestClass]
    public class CrosspathLibTest {
        [TestMethod]
        public void Polymorphism() {
            Crosspath cpath = Crosspath.FromString(@"/");
            AbsoluteCrosspath apath = cpath as AbsoluteCrosspath;
            RelativeCrosspath rpath = cpath as RelativeCrosspath;
        }

        [TestMethod]
        public void DynamicReturn() {
            Crosspath cpath = Crosspath.FromString(@"/");
            if (cpath.Origin == CrosspathOrigin.Absolute) {
                (cpath as AbsoluteCrosspath).ToString();
            }
            else {
                Assert.Fail("it was absolute");
            }

            Crosspath cpath2 = Crosspath.FromString(@".");
            if (cpath2.Origin == CrosspathOrigin.Relative) {
                (cpath2 as RelativeCrosspath).ToString();
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

        // I don't this this test is significant.
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

        /*
        [TestMethod]
        public void TestMethod3() {
            Crosspath cpath = Crosspath.FromString(@"/local/store/qemu");
            Assert.IsInstanceOfType(cpath, typeof(AbsoluteCrosspath));
            Assert.AreEqual(CrosspathFlavor.Unix, cpath.Flavor);
            Assert.AreEqual(CrosspathOrigin.Absolute, cpath.Origin);
            Assert.AreEqual(@"/local/store/qemu", (cpath as AbsoluteCrosspath).Get(CrosspathFlavor.Unix));
            Assert.AreEqual(@"C:\local\store\qemu", cpath.Absolute.Get(CrosspathFlavor.Windows));
            // TODO: substitute absolute paths
            //Assert.AreEqual(@"D:\projects\local\store\qemu"
            //      , ((AbsoluteCrosspath) cpath).GetBasedOn(CrosspathFlavor.Windows, @"D:\Projects"));
        }
        */

        [TestMethod]
        public void RelativeWithGarbage1() {
            Crosspath cpath = Crosspath.FromString(@"qemu/src/../inc");
            Assert.IsInstanceOfType(cpath, typeof(RelativeCrosspath));
            Assert.AreEqual(CrosspathFlavor.Unix, cpath.Flavor);
            Assert.AreEqual(CrosspathOrigin.Relative, cpath.Origin);
            Assert.AreEqual(@"qemu/src/../inc", (cpath as RelativeCrosspath).ToString());
            //Assert.AreEqual(@"/local/store/qemu", ((AbsoluteCrosspath) cpath).Get(CrosspathFlavor.Unix));
            //Assert.AreEqual(@"C:\local\store\qemu", ((AbsoluteCrosspath) cpath).Get(CrosspathFlavor.Windows));
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

            Assert.AreEqual(@"/path/to/file/source.c", (xpath as RelativeCrosspath).Absolutized().ToString());
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
        public void RebaseFromUnixToWindowsTestFile() {
            Crosspath xpath = Crosspath.FromString("/local/store/bin-src/qemu/hw/display/trace.c");
            AbsoluteCrosspath before = Crosspath.FromString("/local/store/bin-src/qemu") as AbsoluteCrosspath;
            AbsoluteCrosspath after = Crosspath.FromString(@"D:\Workspace\Source\qemu") as AbsoluteCrosspath;
            AbsoluteCrosspath apath = (xpath as AbsoluteCrosspath).Rebase(before, after);
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
            AbsoluteCrosspath apath = (xpath as AbsoluteCrosspath).Rebase(before, after);
            Assert.AreEqual(CrosspathFlavor.Windows, apath.Flavor);
            Assert.AreEqual('D', apath.WindowsRootDrive);
            Assert.AreEqual(@"D:\Workspace\Source\qemu", apath.ToString());
            Assert.AreEqual(@"D:\Workspace\Source\qemu", apath.ToAbsolutizedString());
        }
    }
}