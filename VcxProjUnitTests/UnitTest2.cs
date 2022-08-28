using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using CrosspathLib;
using VcxProjLib;

namespace VcxProjUnitTests
{
    [TestClass]
    public class UnitTest2
    {
        [TestMethod]
        public void CompareProjectFiles() {
            Solution sln = new Solution(Configuration.Default());
            Compiler c = new Compiler(Crosspath.FromString("gcc"));
            CompilerInstance ci1 = new CompilerInstance(c, new List<String>(new[] {"-c"}));
            ProjectFile pf1 = new ProjectFile(sln, AbsoluteCrosspath.FromString("/tmp/file1.c"), ci1);
            ProjectFile pf2 = new ProjectFile(sln, AbsoluteCrosspath.FromString("/tmp/file2.c"), ci1);
            ProjectFile pf3 = new ProjectFile(sln, AbsoluteCrosspath.FromString("/tmp/file1.c"), ci1);

            Assert.AreEqual(pf1, pf1);
            Assert.AreNotEqual(pf1, pf2);
            Assert.AreEqual(pf1, pf3);
            Assert.AreNotEqual(pf2, pf3);

            // this is different compiler instance!
            CompilerInstance ci2 = new CompilerInstance(c, new List<String>(new[] {"-c"}));
            ProjectFile pf4 = new ProjectFile(sln, AbsoluteCrosspath.FromString("/tmp/file1.c"), ci2);
            Assert.AreNotEqual(pf1, pf4);
        }

        [TestMethod]
        public void CompareCompilerInstances() {
            RelativeCrosspath gccRel = RelativeCrosspath.FromString("gcc");
            Compiler c1 = new Compiler(gccRel);
            CompilerInstance ci1 = new CompilerInstance(c1, new List<String>(new[] {"-c"}));
            CompilerInstance ci2 = new CompilerInstance(c1, new List<String>(new[] {"-O2"}));
            CompilerInstance ci3 = new CompilerInstance(c1, new List<String>(new[] {"-c"}));
            CompilerInstance ci4 = new CompilerInstance(c1, new List<String>());

            Assert.AreEqual(ci1, ci1);
            Assert.AreNotEqual(ci1, ci2);
            Assert.AreEqual(ci1, ci3);
            Assert.AreEqual(ci1, ci4);

            AbsoluteCrosspath gccAbs = AbsoluteCrosspath.FromString("/usr/bin/gcc");
            Compiler c2 = new Compiler(gccAbs);
            CompilerInstance ci5 = new CompilerInstance(c2, new List<String>(new[] {""}));

            Assert.AreNotEqual(ci1, ci5);

            // advanced case. check if extracting additional info helps to reduce project number
            gccRel.SetWorkingDirectory(AbsoluteCrosspath.FromString("/usr/bin"));

            Assert.AreEqual(ci1, ci5);
        }

        [TestMethod]
        public void CompareCompilers() {
            Compiler c1 = new Compiler(Crosspath.FromString("gcc"));
            Compiler c2 = new Compiler(Crosspath.FromString("mips-linux-gcc"));
            Compiler c3 = new Compiler(Crosspath.FromString("gcc"));
            Compiler c4 = new Compiler(Crosspath.FromString("bin/gcc"));
            Compiler c5 = new Compiler(Crosspath.FromString("/usr/bin/gcc"));

            Assert.AreNotEqual(c1, c2);
            Assert.AreEqual(c1, c3);
            Assert.AreNotEqual(c2, c3);
            Assert.AreNotEqual(c1, c4);
            Assert.AreNotEqual(c1, c5);

            // advanced case. check if extracting additional info helps to reduce project number
            ((RelativeCrosspath)c1.ExePath).SetWorkingDirectory(AbsoluteCrosspath.FromString("/usr/bin"));
            Assert.AreEqual(c1, c5);
        }
    }
}
