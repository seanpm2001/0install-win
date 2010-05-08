﻿/*
 * Copyright 2010 Bastian Eicher
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

using System.Security.Cryptography;
using Common.Helpers;
using IO = System.IO;
using NUnit.Framework;

namespace ZeroInstall.Store.Implementation
{
    /// <summary>
    /// Contains test methods for <see cref="Manifest"/>.
    /// </summary>
    [TestFixture]
    public class ManifestTest
    {
        /// <summary>
        /// Ensures that <see cref="Manifest"/> is correctly generated using the old format and SHA1 algorithm, serialized and deserialized.
        /// </summary>
        [Test]
        public void TestSaveLoadOld()
        {
            Manifest manifest1, manifest2;
            string tempFile = null, tempDir = null;
            try
            {
                // Create a test directory to create a manifest for
                tempDir = FileHelper.GetTempDirectory();
                IO.File.WriteAllText(IO.Path.Combine(tempDir, "file1"), @"content1");
                IO.File.WriteAllText(IO.Path.Combine(tempDir, "file2"), @"content2");
                string subDir = IO.Path.Combine(tempDir, @"subdir");
                IO.Directory.CreateDirectory(subDir);
                IO.File.WriteAllText(IO.Path.Combine(subDir, "file"), @"content");

                // Generate manifest, write it to a file and read the file again
                tempFile = IO.Path.GetTempFileName();
                manifest1 = Manifest.Generate(tempDir, ManifestFormat.Sha1Old);
                manifest1.Save(tempFile);
                manifest2 = Manifest.Load(tempFile, ManifestFormat.Sha1Old);
            }
            finally
            { // Clean up
                if (tempFile != null) IO.File.Delete(tempFile);
                if (tempDir != null) IO.Directory.Delete(tempDir, true);
            }

            // Ensure data stayed the same
            Assert.AreEqual(manifest1, manifest2);
        }

        /// <summary>
        /// Ensures that <see cref="Manifest"/> is correctly generated using the new format and SHA1 algorithm, serialized and deserialized.
        /// </summary>
        [Test]
        public void TestSaveLoadNew()
        {
            Manifest manifest1, manifest2;
            string tempFile = null, tempDir = null;
            try
            {
                // Create a test directory to create a manifest for
                tempDir = FileHelper.GetTempDirectory();
                IO.File.WriteAllText(IO.Path.Combine(tempDir, "file1"), @"content1");
                IO.File.WriteAllText(IO.Path.Combine(tempDir, "file2"), @"content2");
                string subDir = IO.Path.Combine(tempDir, @"subdir");
                IO.Directory.CreateDirectory(subDir);
                IO.File.WriteAllText(IO.Path.Combine(subDir, "file"), @"content");

                // Generate manifest, write it to a file and read the file again
                tempFile = IO.Path.GetTempFileName();
                manifest1 = Manifest.Generate(tempDir, ManifestFormat.Sha1New);
                manifest1.Save(tempFile);
                manifest2 = Manifest.Load(tempFile, ManifestFormat.Sha1New);
            }
            finally
            { // Clean up
                if (tempFile != null) IO.File.Delete(tempFile);
                if (tempDir != null) IO.Directory.Delete(tempDir, true);
            }

            // Ensure data stayed the same
            Assert.AreEqual(manifest1, manifest2);
        }
    }
}
