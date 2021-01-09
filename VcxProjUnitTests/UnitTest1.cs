using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using VcxProjLib;
using VcxProjLib.CrosspathLib;

namespace VcxProjUnitTests {
    [TestClass]
    public class UnitTest1 {
        /*
        [TestMethod]
        public void Polymorphism() {
            Crosspath cpath = new Crosspath(@"/");
            AbsoluteCrosspath apath = cpath as AbsoluteCrosspath;
            RelativeCrosspath rpath = cpath as RelativeCrosspath;
        }
        */

        /*
        [TestMethod]
        public void DynamicReturn() {
            Crosspath cpath = new Crosspath(@"/");
            if (cpath.Origin == CrosspathOrigin.Absolute) {
                cpath.GetAbsolute().ToString();
            }
            else {
                Assert.Fail("it was absolute");
            }

            Crosspath cpath2 = new Crosspath(@".");
            if (cpath2.Origin == CrosspathOrigin.Relative) {
                cpath2.GetRelative().ToString();
            }
            else {
                Assert.Fail("it was relative");
            }
        }
        */

        [TestMethod]
        public void TestMethod1() {
            Crosspath cpath = new Crosspath(@"/");
            Assert.AreEqual(CrosspathFlavor.Unix, cpath.Flavor);
            Assert.AreEqual(CrosspathOrigin.Absolute, cpath.Origin);
        }

        [TestMethod]
        public void TestMethod2() {
            try {
                Crosspath cpath = new Crosspath(@"\");
                Assert.Fail();
            }
            catch (ArgumentException) {
            }
            catch {
                Assert.Fail("should be ArgumentException");
            }
        }

        [TestMethod]
        public void TestMethod3() {
            Crosspath cpath = new Crosspath(@"/local/store/qemu");
            Assert.AreEqual(CrosspathFlavor.Unix, cpath.Flavor);
            Assert.AreEqual(CrosspathOrigin.Absolute, cpath.Origin);
            Assert.AreEqual(@"/local/store/qemu", cpath.Absolute.Get(CrosspathFlavor.Unix));
            Assert.AreEqual(@"C:\local\store\qemu", cpath.Absolute.Get(CrosspathFlavor.Windows));
            // TODO: substitute absolute paths
            //Assert.AreEqual(@"D:\projects\local\store\qemu"
            //      , ((AbsoluteCrosspath) cpath).GetBasedOn(CrosspathFlavor.Windows, @"D:\Projects"));
        }

        [TestMethod]
        public void TestMethod4() {
            Crosspath cpath = new Crosspath(@"qemu/src/../inc");
            Assert.AreEqual(CrosspathFlavor.Unix, cpath.Flavor);
            Assert.AreEqual(CrosspathOrigin.Relative, cpath.Origin);
            //Assert.AreEqual(@"/local/store/qemu", ((AbsoluteCrosspath) cpath).Get(CrosspathFlavor.Unix));
            //Assert.AreEqual(@"C:\local\store\qemu", ((AbsoluteCrosspath) cpath).Get(CrosspathFlavor.Windows));
        }

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

        [TestMethod]
        public void TryCompileDBEntry() {
            CompileDBEntry entry = new CompileDBEntry {file = "source.c", directory = "/path/to/file"};

            // from Solution.cs
            Crosspath xpath = new Crosspath(entry.file);
            Crosspath xdir = new Crosspath(entry.directory);
            if (xpath.Origin == CrosspathOrigin.Relative) {
                xpath.Relative.SetWorkingDirectory(xdir);
            }

            Assert.AreEqual(CrosspathOrigin.Relative, xpath.Origin);
            Assert.AreEqual(CrosspathFlavor.Unix, xpath.Flavor);
            Assert.AreEqual(CrosspathOrigin.Absolute, xdir.Origin);
            Assert.AreEqual(CrosspathFlavor.Unix, xdir.Flavor);

            Assert.AreEqual(@"/path/to/file/source.c", xpath.ToAbsolutizedString());
        }
    }
}