using System;
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
            remote.Connect();
            remote.Execute("echo -n 1", out String result);
            Assert.AreEqual("1", result);
        }

        [TestMethod]
        public void PrintGCCPredefinedMacrosAtOnce() {
            RemoteHost remote = InitRemoteHost();
            remote.Connect();
            int rv = remote.Execute("gcc -E -dM - < /dev/null", out String result);
            Assert.AreEqual(0, rv);
        }

        [TestMethod]
        public void PrintGCCIncludeDirectoriesAtOnce() {
            RemoteHost remote = InitRemoteHost();
            remote.Connect();
            int rv = remote.Execute("gcc -E -Wp,-v - < /dev/null", out String result);
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
    }
}