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

using System;
using System.Diagnostics;
using System.IO;
using Common.Utils;
using NUnit.Framework;
using NUnit.Mocks;
using ZeroInstall.Injector.Solver;
using ZeroInstall.Model;
using ZeroInstall.Store.Implementation;

namespace ZeroInstall.Injector
{
    /// <summary>
    /// Contains test methods for <see cref="Executor"/>.
    /// </summary>
    [TestFixture]
    public class ExecutorTest
    {
        #region Constants
        private const string Test1Path = "test1 path", Test2Path = "test2 path";
        #endregion

        #region Shared
        private DynamicMock _storeMock;

        private IStore TestStore
        {
            get { return (IStore)_storeMock.MockInstance; }
        }

        [SetUp]
        public void SetUp()
        {
            // Prepare mock objects that will be injected with methods in the tests
            _storeMock = new DynamicMock("MockStore", typeof(IStore));
        }

        [TearDown]
        public void TearDown()
        {
            // Ensure no method calls were left out
            _storeMock.Verify();
        }
        #endregion

        /// <summary>
        /// Ensures the <see cref="Executor"/> constructor throws the correct exceptions.
        /// </summary>
        [Test]
        public void TestExceptions()
        {
            Assert.Throws<ArgumentException>(() => new Executor(new Selections(), TestStore), "Empty selections should be rejected");

            var selections = SelectionsTest.CreateTestSelections();
            selections.Commands[1].WorkingDir = new WorkingDir();
            _storeMock.SetReturnValue("GetPath", "test path");
            var executor = new Executor(selections, TestStore);
            Assert.Throws<CommandException>(() => executor.GetStartInfo(new string[0]), "Multiple WorkingDir changes should be rejected");
        }

        private void PrepareStoreMock(Selections selections)
        {
            _storeMock.ExpectAndReturn("GetPath", Test1Path, selections.Implementations[0].ManifestDigest); // First/inner command for command-line
            _storeMock.ExpectAndReturn("GetPath", Test2Path, selections.Implementations[1].ManifestDigest); // Second/outer/runner command for command-line
            _storeMock.ExpectAndReturn("GetPath", Test1Path, selections.Implementations[0].ManifestDigest); // Self-binding for first implementation
            _storeMock.ExpectAndReturn("GetPath", Test2Path, selections.Implementations[1].ManifestDigest); // Binding for dependency from first to second implementation
            _storeMock.ExpectAndReturn("GetPath", Test2Path, selections.Implementations[1].ManifestDigest); // Self-binding for second implementation
            _storeMock.ExpectAndReturn("GetPath", Test1Path, selections.Implementations[0].ManifestDigest); // Binding for command dependency from second to first implementation
            _storeMock.ExpectAndReturn("GetPath", Test2Path, selections.Implementations[1].ManifestDigest); // Self-binding for second/outer/runner command
            _storeMock.ExpectAndReturn("GetPath", Test1Path, selections.Implementations[0].ManifestDigest); // Working dir for first/inner command
        }
        
        private static void CheckEnvironment(ProcessStartInfo startInfo)
        {
            Assert.AreEqual("default" + Path.PathSeparator + Test1Path, startInfo.EnvironmentVariables["TEST1_PATH_SELF"], "Should append implementation path");
            Assert.AreEqual("test1", startInfo.EnvironmentVariables["TEST1_VALUE"], "Should directly set value");
            Assert.AreEqual(Test2Path + Path.PathSeparator + "default", startInfo.EnvironmentVariables["TEST2_PATH_SELF"], "Should prepend implementation path");
            Assert.AreEqual("test2", startInfo.EnvironmentVariables["TEST2_VALUE"], "Should directly set value");
            Assert.AreEqual("default" + Path.PathSeparator + Path.Combine(Test2Path, "sub"), startInfo.EnvironmentVariables["TEST2_PATH_SUB_DEP"], "Should append implementation sub-path");
            Assert.AreEqual(Test1Path, startInfo.EnvironmentVariables["TEST1_PATH_COMMAND_DEP"], "Should set implementation path");
            Assert.AreEqual(Test2Path, startInfo.EnvironmentVariables["TEST2_PATH_RUNNER_SELF"], "Should set implementation path");
            Assert.AreEqual(Path.Combine(Test1Path, "bin"), startInfo.WorkingDirectory, "Should set implementation path");
        }

        /// <summary>
        /// Ensures <see cref="Executor.GetStartInfo"/> handles complex <see cref="Selections"/>.
        /// </summary>
        [Test]
        public void TestGetStartInfo()
        {
            var selections = SelectionsTest.CreateTestSelections();

            PrepareStoreMock(selections);

            var executor = new Executor(selections, TestStore);
            var startInfo = executor.GetStartInfo(new[] {"--custom"});
            Assert.AreEqual(
                Path.Combine(Test2Path, StringUtils.UnifySlashes(selections.Commands[1].Path)),
                startInfo.FileName,
                "Should combine runner implementation directory with runner command path");
            Assert.AreEqual(
                selections.Commands[1].Arguments[0] + " \"" + selections.Commands[0].Runner.Arguments[0] + "\" \"" + Path.Combine(Test1Path, StringUtils.UnifySlashes(selections.Commands[0].Path)) + "\" " + selections.Commands[0].Arguments[0] + " --custom",
                startInfo.Arguments,
                "Should combine core and additional runner arguments with application implementation directory, command path and arguments");
            
            CheckEnvironment(startInfo);
        }

        /// <summary>
        /// Ensures <see cref="Executor.GetStartInfo"/> handles complex <see cref="Selections"/> and <see cref="Executor.Wrapper"/>.
        /// </summary>
        [Test]
        public void TestGetStartInfoWrapper()
        {
            var selections = SelectionsTest.CreateTestSelections();

            PrepareStoreMock(selections);

            var executor = new Executor(selections, TestStore) {Wrapper = "wrapper --wrapper"};
            var startInfo = executor.GetStartInfo(new[] {"--custom"});
            Assert.AreEqual("wrapper", startInfo.FileName);
            Assert.AreEqual(
                "--wrapper \"" + Path.Combine(Test2Path, StringUtils.UnifySlashes(selections.Commands[1].Path)) + "\" " + selections.Commands[1].Arguments[0] + " \"" + selections.Commands[0].Runner.Arguments[0] + "\" \"" + Path.Combine(Test1Path, StringUtils.UnifySlashes(selections.Commands[0].Path)) + "\" " + selections.Commands[0].Arguments[0] + " --custom",
                startInfo.Arguments,
                "Should combine wrapper arguments, runner and application");
            
            CheckEnvironment(startInfo);
        }

        /// <summary>
        /// Ensures <see cref="Executor.GetStartInfo"/> handles complex <see cref="Selections"/> and <see cref="Executor.Main"/> with relative paths.
        /// </summary>
        [Test]
        public void TestGetStartInfoMainRelative()
        {
            var selections = SelectionsTest.CreateTestSelections();

            PrepareStoreMock(selections);

            var executor = new Executor(selections, TestStore) {Main = "main"};
            var startInfo = executor.GetStartInfo(new[] {"--custom"});
            Assert.AreEqual(
                Path.Combine(Test2Path, StringUtils.UnifySlashes(selections.Commands[1].Path)),
                startInfo.FileName,
                "Should combine runner implementation directory with runner command path");
            Assert.AreEqual(
                selections.Commands[1].Arguments[0] + " \"" + selections.Commands[0].Runner.Arguments[0] + "\" \"" + StringUtils.PathCombine(Test1Path, "dir 1", "main") + "\" --custom",
                startInfo.Arguments,
                "Should combine core and additional runner arguments with application implementation directory, command directory and main binary override");
            
            CheckEnvironment(startInfo);
        }

        /// <summary>
        /// Ensures <see cref="Executor.GetStartInfo"/> handles complex <see cref="Selections"/> and <see cref="Executor.Main"/> with absolute paths.
        /// </summary>
        [Test]
        public void TestGetStartInfoMainAbsolute()
        {
            var selections = SelectionsTest.CreateTestSelections();

            PrepareStoreMock(selections);

            var executor = new Executor(selections, TestStore) {Main = "/main"};
            var startInfo = executor.GetStartInfo(new[] {"--custom"});
            Assert.AreEqual(
                Path.Combine(Test2Path, StringUtils.UnifySlashes(selections.Commands[1].Path)),
                startInfo.FileName,
                "Should combine runner implementation directory with runner command path");
            Assert.AreEqual(
                selections.Commands[1].Arguments[0] + " \"" + selections.Commands[0].Runner.Arguments[0] + "\" \"" + StringUtils.PathCombine(Test1Path, "main") + "\" --custom",
                startInfo.Arguments,
                "Should combine core and additional runner arguments with application implementation directory and main binary override");

            CheckEnvironment(startInfo);
        }

        /// <summary>
        /// Ensures <see cref="Executor.GetStartInfo"/> handles <see cref="Selections"/> with <see cref="Command.Path"/>s that are empty.
        /// </summary>
        [Test]
        public void TestGetStartInfoPathlessCommand()
        {
            var selections = SelectionsTest.CreateTestSelections();
            selections.Commands[0].Path = null;

            _storeMock.ExpectAndReturn("GetPath", Test2Path, selections.Implementations[1].ManifestDigest); // Second/outer/runner command for command-line
            _storeMock.ExpectAndReturn("GetPath", Test1Path, selections.Implementations[0].ManifestDigest); // Self-binding for first implementation
            _storeMock.ExpectAndReturn("GetPath", Test2Path, selections.Implementations[1].ManifestDigest); // Binding for dependency from first to second implementation
            _storeMock.ExpectAndReturn("GetPath", Test2Path, selections.Implementations[1].ManifestDigest); // Self-binding for second implementation
            _storeMock.ExpectAndReturn("GetPath", Test1Path, selections.Implementations[0].ManifestDigest); // Binding for command dependency from second to first implementation
            _storeMock.ExpectAndReturn("GetPath", Test2Path, selections.Implementations[1].ManifestDigest); // Self-binding for second/outer/runner command
            _storeMock.ExpectAndReturn("GetPath", Test1Path, selections.Implementations[0].ManifestDigest); // Working dir for first/inner command

            var executor = new Executor(selections, TestStore);
            var startInfo = executor.GetStartInfo(new[] {"--custom"});
            Assert.AreEqual(
                Path.Combine(Test2Path, StringUtils.UnifySlashes(selections.Commands[1].Path)),
                startInfo.FileName);
            Assert.AreEqual(
                selections.Commands[1].Arguments[0] + " \"" + selections.Commands[0].Runner.Arguments[0] + "\" --custom",
                startInfo.Arguments);

            CheckEnvironment(startInfo);
        }
    }
}
