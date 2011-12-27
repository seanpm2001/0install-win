/*
 * Copyright 2010-2011 Bastian Eicher
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser Public License for more details.
 * 
 * You should have received a copy of the GNU Lesser Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.IO;
using System.Text;
using NUnit.Framework;
using Moq;
using ZeroInstall.Model;

namespace ZeroInstall.Store.Feeds
{
    /// <summary>
    /// Contains test methods for <see cref="FeedUtils"/>.
    /// </summary>
    [TestFixture]
    public class FeedUtilsTest
    {
        /// <summary>
        /// Ensures <see cref="FeedUtils.GetFeeds"/> correctly loads <see cref="Feed"/>s from an <see cref="IFeedCache"/>, skipping any exceptions.
        /// </summary>
        [Test]
        public void TestGetFeeds()
        {
            var feed1 = FeedTest.CreateTestFeed();
            var feed3 = FeedTest.CreateTestFeed();
            feed3.Uri = new Uri("http://0install.de/feeds/test/test3.xml");

            var cacheMock = new Mock<IFeedCache>(MockBehavior.Strict);
            cacheMock.Setup(x => x.ListAll()).Returns(new[] {"http://0install.de/feeds/test/test1.xml", "http://0install.de/feeds/test/test2.xml", "http://0install.de/feeds/test/test3.xml"});
            cacheMock.Setup(x => x.GetFeed("http://0install.de/feeds/test/test1.xml")).Returns(feed1);
            cacheMock.Setup(x => x.GetFeed("http://0install.de/feeds/test/test2.xml")).Throws(new IOException("Fake IO exception for testing"));
            cacheMock.Setup(x => x.GetFeed("http://0install.de/feeds/test/test3.xml")).Returns(feed3);

            CollectionAssert.AreEqual(new[] {feed1, feed3}, FeedUtils.GetFeeds(cacheMock.Object));
        }

        private const string FeedText = "Feed data\n";
        private readonly byte[] _feedBytes = Encoding.UTF8.GetBytes(FeedText);
        private const string SignatureBlockStart = "<!-- Base64 Signature\n";
        private static readonly byte[] _signatureBytes = Encoding.UTF8.GetBytes("Signature data");
        private static readonly string _signatureBase64 = Convert.ToBase64String(_signatureBytes).Insert(10, "\n");
        private const string SignatureBlockEnd = "\n\n-->\n";

        /// <summary>
        /// Ensures that <see cref="FeedUtils.GetSignatures"/> correctly separates an XML signature block from a signed feed.
        /// </summary>
        [Test]
        public void TestGetSignatures()
        {
            var openPgpMock = new Mock<IOpenPgp>(MockBehavior.Strict);
            var result = new OpenPgpSignature[] {new ValidSignature("123", new DateTime(2000, 1, 1))};
            openPgpMock.Setup(x => x.Verify(_feedBytes, _signatureBytes)).Returns(result);

            string input = FeedText + SignatureBlockStart + _signatureBase64 + SignatureBlockEnd;
            CollectionAssert.AreEqual(result, FeedUtils.GetSignatures(openPgpMock.Object, Encoding.UTF8.GetBytes(input)));

            openPgpMock.Verify();
        }

        /// <summary>
        /// Ensures that <see cref="FeedUtils.GetSignatures"/> throws a <see cref="SignatureException"/> if the signature block does not start in a new line.
        /// </summary>
        [Test]
        public void TestGetSignaturesMissingNewLine()
        {
            string input = "Feed without newline" + SignatureBlockStart + _signatureBase64 + SignatureBlockEnd;
            Assert.Throws<SignatureException>(() => FeedUtils.GetSignatures(new Mock<IOpenPgp>().Object, Encoding.UTF8.GetBytes(input)));
        }

        /// <summary>
        /// Ensures that <see cref="FeedUtils.GetSignatures" /> throws a <see cref="SignatureException"/> if the signature contains non-base 64 characters.
        /// </summary>
        [Test]
        public void TestGetSignaturesInvalidChars()
        {
            const string input = FeedText + SignatureBlockStart + "*!?#" + SignatureBlockEnd;
            Assert.Throws<SignatureException>(() => FeedUtils.GetSignatures(new Mock<IOpenPgp>().Object, Encoding.UTF8.GetBytes(input)));
        }

        /// <summary>
        /// Ensures that <see cref="FeedUtils.GetSignatures" /> throws a <see cref="SignatureException"/> if the correct signature end is missing.
        /// </summary>
        [Test]
        public void TestGetSignaturesMissingEnd()
        {
            string input = FeedText + SignatureBlockStart + _signatureBase64;
            Assert.Throws<SignatureException>(() => FeedUtils.GetSignatures(new Mock<IOpenPgp>().Object, Encoding.UTF8.GetBytes(input)));
        }

        /// <summary>
        /// Ensures that <see cref="FeedUtils.GetSignatures" /> throws a <see cref="SignatureException"/> if there is additional data after the signature block.
        /// </summary>
        [Test]
        public void TestGetSignaturesDataAfterSignature()
        {
            string input = FeedText + SignatureBlockStart + _signatureBase64 + SignatureBlockEnd + "more data";
            Assert.Throws<SignatureException>(() => FeedUtils.GetSignatures(new Mock<IOpenPgp>().Object, Encoding.UTF8.GetBytes(input)));
        }
    }
}
