﻿using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VcxProjLib;

namespace VcxProjUnitTests {
    [TestClass]
    public class VcxProjLibUnitTests {
        private RemoteHost InitRemoteHost() {
            return RemoteHost.Parse(File.ReadAllText(@"D:\VCXProjWriterRemote.txt"));
        }

        [TestMethod]
        public void ConnectRemoteHost() {
            RemoteHost remote = InitRemoteHost();
            remote.PrepareForConnection();
            remote.Execute("echo -n 1", out String result);
            Assert.AreEqual("1", result);
        }

        [TestMethod]
        public void PrintGCCPredefinedMacrosAtOnce() {
            RemoteHost remote = InitRemoteHost();
            remote.PrepareForConnection();
            int rv = remote.Execute("gcc -E -dM - < /dev/null", out String result);
            Assert.AreEqual(0, rv);
        }

        [TestMethod]
        public void PrintGCCIncludeDirectoriesAtOnce() {
            RemoteHost remote = InitRemoteHost();
            remote.PrepareForConnection();
            int rv = remote.Execute("gcc -E -Wp,-v - < /dev/null", out String result);
            Assert.AreEqual(0, rv);
        }

        [TestMethod]
        public void PrintGCCIncludeDirectoriesCppAtOnce() {
            RemoteHost remote = InitRemoteHost();
            remote.PrepareForConnection();
            int rv = remote.Execute("gcc -x c++ -c -Wp,-v - < /dev/null", out String result);
            Assert.AreEqual(0, rv);
        }

        [TestMethod]
        public void RemoteHostParser1() {
            RemoteHost remote1 = RemoteHost.Parse("user@192.168.0.1");
            Assert.AreEqual("user", remote1.Username);
            Assert.AreEqual("", remote1.Password);
            Assert.AreEqual("192.168.0.1", remote1.Host);
            Assert.AreEqual(22, remote1.Port);

            RemoteHost remote2 = RemoteHost.Parse("user:p@sSw0rD@ec2-eu-central-1.amazon.aws.com:2244");
            Assert.AreEqual("user", remote2.Username);
            Assert.AreEqual("p@sSw0rD", remote2.Password);
            Assert.AreEqual("ec2-eu-central-1.amazon.aws.com", remote2.Host);
            Assert.AreEqual(2244, remote2.Port);

            RemoteHost remote3 = RemoteHost.Parse(@"mother_fuck-3r:v3ry_str0ng-#\@255.255.255.255:65535");
            Assert.AreEqual(@"mother_fuck-3r", remote3.Username);
            Assert.AreEqual(@"v3ry_str0ng-#\", remote3.Password);
            Assert.AreEqual("255.255.255.255", remote3.Host);
            Assert.AreEqual(65535, remote3.Port);
        }

        [TestMethod]
        public void DefineParser() {
            Define def1 = new Define("INCLUDE_L3");
            Assert.AreEqual("INCLUDE_L3", def1.Name);
            Assert.AreEqual(Define.DefaultValue, def1.Value);
            Assert.AreEqual("INCLUDE_L3", def1.ToString());

            Define def2 = new Define("INCLUDE_L3=1");
            Assert.AreEqual("INCLUDE_L3", def2.Name);
            Assert.AreEqual("1", def2.Value);
            Assert.AreEqual("INCLUDE_L3=1", def2.ToString());

            Define def3 = new Define("INCLUDE_L3=YES");
            Assert.AreEqual("INCLUDE_L3", def3.Name);
            Assert.AreEqual("YES", def3.Value);
            Assert.AreEqual("INCLUDE_L3=YES", def3.ToString());

            Define def4 = new Define("INCLUDE_L3=\"YES\"");
            Assert.AreEqual("INCLUDE_L3", def4.Name);
            Assert.AreEqual("YES", def4.Value);
            Assert.AreEqual("INCLUDE_L3=YES", def4.ToString());
        }

        [TestMethod]
        public void DefineComparer() {
            Define def1 = new Define("INCLUDE_L3");
            Define def2 = new Define("INCLUDE_L3=1");
            HashSet<Define> hs1 = new HashSet<Define>(new DefineNameOnlyComparer());
            HashSet<Define> hs2 = new HashSet<Define>(new DefineExactComparer());
            hs1.Add(def1);
            hs1.Add(def2);
            hs2.Add(def1);
            hs2.Add(def2);
            Assert.AreEqual(1, hs1.Count);
            Assert.AreEqual(2, hs2.Count);
        }

        [TestMethod]
        public void IncludeDirsOrder() {
            PriorityQueue<IncludeDirectory> includeDirs = new PriorityQueue<IncludeDirectory>();
            Assert.Fail("TODO");
        }
    }
}