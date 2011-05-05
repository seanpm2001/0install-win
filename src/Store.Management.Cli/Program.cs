﻿/*
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
using System.Collections.Generic;
using System.IO;
using Common;
using Common.Collections;
using Common.Storage;
using Common.Tasks;
using Common.Utils;
using NDesk.Options;
using ZeroInstall.Model;
using ZeroInstall.Store.Implementation.Archive;
using ZeroInstall.Store.Management.Cli.Properties;
using ZeroInstall.Store.Implementation;

namespace ZeroInstall.Store.Management.Cli
{
    #region Enumerations
    /// <summary>
    /// An errorlevel is returned to the original caller after the application terminates, to indicate success or the reason for failure.
    /// </summary>
    public enum ErrorLevel
    {
        ///<summary>Everything is OK.</summary>
        OK = 0,

        /// <summary>The user canceled the operation.</summary>
        UserCanceled = 1,

        /// <summary>The arguments passed on the command-line were not valid.</summary>
        InvalidArguments = 2,

        /// <summary>An unknown or not supported feature was requested.</summary>
        NotSupported = 3,

        /// <summary>An IO error occurred.</summary>
        IOError = 10,

        /// <summary>A requested implementation could not be found or could not be launched.</summary>
        ImplementationError = 15,

        /// <summary>A manifest digest for an implementation did not match the expected value.</summary>
        DigestMismatch = 20,
    }
    #endregion

    /// <summary>
    /// Launches a command-line tool for managing caches of Zero Install implementations.
    /// </summary>
    public static class Program
    {
        private static IStore _store;

        #region Startup
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static int Main(string[] args)
        {
#if !DEBUG
            // Prevent launch during update and allow instance detection
            string mutexName = AppMutex.GenerateName(Locations.InstallBase);
            if (AppMutex.Probe(mutexName + "-update")) return 99;
            AppMutex.Create(mutexName);
            AppMutex.Create("Zero Install");
#endif

            // Automatically show help for missing args
            if (args.Length == 0) args = new[] {"--help"};

            IList<string> restArgs;
            try { restArgs = ParseArgs(args); }
            #region Error handling
            catch (ArgumentException ex)
            {
                Log.Error(ex.Message);
                return (int)ErrorLevel.InvalidArguments;
            }
            #endregion

            if (restArgs.Count == 0) return (int)ErrorLevel.OK;

            try
            {
                _store = StoreProvider.CreateDefault();
                return (int)ExecuteArgs(restArgs, new CliTaskHandler());
            }
            #region Error handling
            catch (UserCancelException)
            {
                return (int)ErrorLevel.UserCanceled;
            }
            catch (ArgumentException ex)
            {
                Log.Error(ex.Message);
                return (int)ErrorLevel.InvalidArguments;
            }
            catch (IOException ex)
            {
                Log.Error(ex.Message);
                return (int)ErrorLevel.IOError;
            }
            catch (UnauthorizedAccessException ex)
            {
                Log.Error(ex.Message);
                return (int)ErrorLevel.IOError;
            }
            catch (InvalidOperationException ex)
            {
                Log.Error(ex.Message);
                return (int)ErrorLevel.IOError;
            }
            catch (ImplementationNotFoundException ex)
            {
                Log.Error(ex.Message);
                return (int)ErrorLevel.ImplementationError;
            }
            catch (ImplementationAlreadyInStoreException ex)
            {
                Log.Error(ex.Message);
                return (int)ErrorLevel.ImplementationError;
            }
            catch (DigestMismatchException ex)
            {
                // ToDo: Display manifest diff
                Log.Error(ex.Message);
                return (int)ErrorLevel.DigestMismatch;
            }
            #endregion
        }
        #endregion

        #region Parse
        /// <summary>
        /// Parses command-line arguments.
        /// </summary>
        /// <param name="args">The command-line arguments to be parsed.</param>
        /// <returns>Any unparsed commands left over.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="args"/> contains unknown options.</exception>
        private static IList<string> ParseArgs(IEnumerable<string> args)
        {
            #region Sanity checks
            if (args == null) throw new ArgumentNullException("args");
            #endregion

            #region Define options
            var options = new OptionSet
            {
                // Mode selection
                {"V|version", Resources.OptionVersion, unused => Console.WriteLine(AppInfo.Name + " " + AppInfo.Version + (Locations.IsPortable ? " - Portable mode\n" : "\n") + AppInfo.Copyright + "\n" + Resources.LicenseInfo)},

                // Documentation
                {"man", Resources.OptionMan, unused => PrintManual()}
            };
            #endregion

            #region Help text
            options.Add("h|help|?", Resources.OptionHelp, unused =>
            {
                PrintUsage();
                Console.WriteLine(Resources.Options);
                options.WriteOptionDescriptions(Console.Out);
            });
            #endregion

            // Parse the arguments and call the hooked handlers
            return options.Parse(args);
        }
        #endregion

        #region Help
        private static void PrintUsage()
        {
            const string usage = "{0}\t{1}\n\t{2}\n\t{3}\n\t{4}\n\t{5}\n\t{5}\n\t{6}\n\t{7}\n\t{8}\n\t{9}\n";
            Console.WriteLine(usage, Resources.Usage, Resources.UsageAdd, Resources.UsageAudit, Resources.UsageCopy, Resources.UsageFind, Resources.UsageList, Resources.UsageManifest, Resources.UsageOptimize, Resources.UsageRemove, Resources.UsageVerify);
        }

        private static void PrintManual()
        {
            // ToDo: Add flow formatting for better readability on console
            Console.WriteLine("\n");
            Console.WriteLine("ADD\n\n" + Resources.DetailsAdd + "\n\n\n");
            Console.WriteLine("AUDIT\n\n" + Resources.DetailsAudit + "\n\n\n");
            Console.WriteLine("COPY\n\n" + Resources.DetailsCopy + "\n\n\n");
            Console.WriteLine("FIND\n\n" + Resources.DetailsFind + "\n\n\n");
            Console.WriteLine("LIST\n\n" + Resources.DetailsList + "\n\n\n");
            Console.WriteLine("MANAGE\n\n" + Resources.DetailsManage + "\n\n\n");
            Console.WriteLine("MANIFEST\n\n" + Resources.DetailsManifest + "\n\n\n");
            Console.WriteLine("OPTMISE\n\n" + Resources.DetailsOptimise + "\n\n\n");
            Console.WriteLine("REMOVE\n\n" + Resources.DetailsRemove + "\n\n\n");
            Console.WriteLine("VERIFY\n\n" + Resources.DetailsVerify + "\n");
        }
        #endregion

        #region Execute
        /// <summary>
        /// Executes the commands specified by the command-line arguments.
        /// </summary>
        /// <param name="args">The command-line arguments that were not parsed as options.</param>
        /// <param name="handler">A callback object used when the the user needs to be asked any questions or informed about progress.</param>
        /// <returns>The error level to return when the process ends.</returns>
        /// <exception cref="UserCancelException">Thrown if an IO task was canceled.</exception>
        /// <exception cref="ArgumentException">Thrown if the number of arguments passed in <paramref name="args"/> is incorrect.</exception>
        /// <exception cref="NotSupportedException">Thrown if the archive type is unknown or not supported.</exception>
        /// <exception cref="IOException">Thrown if a problem occurred while creating a directory.</exception>
        /// <exception cref="UnauthorizedAccessException">Thrown if creating a directory is not permitted.</exception>
        /// <exception cref="ImplementationNotFoundException">Thrown if no implementation matching the <see cref="ManifestDigest"/> could be found in this store.</exception>
        /// <exception cref="ImplementationAlreadyInStoreException">Thrown if there is already an <see cref="Model.Implementation"/> with the specified <see cref="ManifestDigest"/> in the store.</exception>
        /// <exception cref="DigestMismatchException">Thrown if the archive/directory content doesn't match the <see cref="ManifestDigest"/>.</exception>
        private static ErrorLevel ExecuteArgs(IList<string> args, ITaskHandler handler)
        {
            switch (args[0])
            {
                case "add":
                    return Add(args, handler);

                case "audit":
                    return Audit(args, handler);

                case "copy":
                    Copy(args, handler);
                    return ErrorLevel.OK;

                case "find":
                    Find(args);
                    return ErrorLevel.OK;

                case "remove":
                    Remove(args, handler);
                    return ErrorLevel.OK;

                case "list":
                    List(args);
                    return ErrorLevel.OK;

                case "manage":
                    // ToDo: Automatically switch to GTK# on Linux
                    ProcessUtils.LaunchHelperAssembly("0store-win", null);
                    return ErrorLevel.OK;

                case "manifest":
                    GenerateManifest(args, handler);
                    return ErrorLevel.OK;

                case "optimise":
                    Optimise(args, handler);
                    return ErrorLevel.OK;

                case "verify":
                    Verify(args, handler);
                    return ErrorLevel.OK;

                default:
                    Log.Error(Resources.UnknownMode);
                    return ErrorLevel.NotSupported;
            }
        }
        #endregion

        //--------------------//

        #region Execute helpers
        private static ErrorLevel Add(IList<string> args, ITaskHandler handler)
        {
            if (args.Count < 3 || args.Count > 4) throw new ArgumentException(string.Format(Resources.WrongNoArguments, Resources.UsageAdd));
            var manifestDigest = new ManifestDigest(args[1]);
            string path = args[2];
            string subDir = (args.Count == 4) ? args[3] : null;

            if (Directory.Exists(path)) _store.AddDirectory(path, manifestDigest, handler);
            else if (File.Exists(path)) _store.AddArchive(new ArchiveFileInfo {Path = path, SubDir = subDir}, manifestDigest, handler);
            else
            {
                Log.Error(string.Format(Resources.NoSuchFileOrDirectory, path));
                return ErrorLevel.IOError;
            }
            return ErrorLevel.OK;
        }

        private static void Copy(IList<string> args, ITaskHandler handler)
        {
            if (args.Count < 2 || args.Count > 3) throw new ArgumentException(string.Format(Resources.WrongNoArguments, Resources.UsageCopy));

            IStore store = (args.Count == 3) ? new DirectoryStore(args[2]) : _store;
            store.AddDirectory(args[1], new ManifestDigest(Path.GetFileName(args[1])), handler);
        }

        private static void Find(IList<string> args)
        {
            if (args.Count != 2) throw new ArgumentException(string.Format(Resources.WrongNoArguments, Resources.UsageFind));

            Console.WriteLine(_store.GetPath(new ManifestDigest(args[1])));
        }

        private static void Remove(IList<string> args, ITaskHandler handler)
        {
            if (args.Count < 2) throw new ArgumentException(string.Format(Resources.WrongNoArguments, Resources.UsageRemove));

            for (int i = 1; i < args.Count; i++)
            {
                _store.Remove(new ManifestDigest(args[i]), handler);
                Log.Info(string.Format(Resources.SuccessfullyRemoved, args[i]));
            }
        }

        private static void List(IList<string> args)
        {
            if (args.Count != 1) throw new ArgumentException(string.Format(Resources.WrongNoArguments, Resources.UsageList));

            foreach (ManifestDigest digest in _store.ListAll())
                Console.WriteLine(digest.BestDigest);
        }

        private static void Optimise(IList<string> args, ITaskHandler handler)
        {
            if (args.Count == 1) _store.Optimise(handler);
            else
            {
                for (int i = 1; i < args.Count; i++)
                    new DirectoryStore(args[i]).Optimise(handler);
            }
        }

        private static void Verify(IList<string> args, ITaskHandler handler)
        {
            if (args.Count < 2) throw new ArgumentException(string.Format(Resources.WrongNoArguments, Resources.UsageVerify));
            for (int i = 1; i < args.Count; i++)
            {
                if (Directory.Exists(args[i]))
                { // Verify an arbitrary directory
                    DirectoryStore.VerifyDirectory(args[i], new ManifestDigest(Path.GetFileName(args[i])), handler);
                }
                else
                { // Verify a directory inside the default store
                    _store.Verify(new ManifestDigest(args[i]), handler);
                }
                Console.WriteLine(Resources.StoreEntryOK);
            }
        }
        #endregion

        #region Audit
        private static ErrorLevel Audit(IList<string> args, ITaskHandler handler)
        {
            ErrorLevel result = ErrorLevel.OK;
            if (args.Count == 1) AuditStore(_store, handler);
            else
            {
                for (int i = 1; i < args.Count; i++)
                {
                    ErrorLevel tempResult = AuditStore(new DirectoryStore(args[i]), handler);
                    if (tempResult > result) result = tempResult;
                }
            }
            return result;
        }

        /// <summary>
        /// Recalculates the digests for all entries in the store and ensures they are correct. Prints any problems to the console.
        /// </summary>
        /// <param name="store">The store to be audited.</param>
        /// <param name="handler">A callback object used when the the user needs to be informed about progress.</param>
        /// <returns>The error level to return when the process ends.</returns>
        /// <exception cref="IOException">Thrown if a directory in the store could not be processed.</exception>
        /// <exception cref="UnauthorizedAccessException">Thrown if read access to the store is not permitted.</exception>
        private static ErrorLevel AuditStore(IStore store, ITaskHandler handler)
        {
            var problems = store.Audit(handler);
            if (problems == null)
            {
                Log.Error(Resources.NoAuditSupport);
                return ErrorLevel.InvalidArguments;
            }

            bool hasProblems = false;
            foreach (var problem in problems)
            {
                Console.WriteLine(problem.Message);
                Console.WriteLine();
                hasProblems = true;
            }
            if (hasProblems)
            {
                Log.Warn(Resources.AuditErrors);
                return ErrorLevel.DigestMismatch;
            }

            Log.Info(Resources.AuditPass);
            return ErrorLevel.OK;
        }
        #endregion

        #region Manifest
        /// <summary>
        /// Prints the manifest for a directory (listing every file and directory in the tree) to the console.
        /// After the manifest, the last line gives the digest of the manifest itself. 
        /// </summary>
        /// <param name="args">The command-line arguments that were not parsed as options.</param>
        /// <param name="handler">A callback object used when the the user needs to be informed about progress.</param>
        /// <exception cref="IOException">Thrown if a directory could not be processed.</exception>
        private static void GenerateManifest(IList<string> args, ITaskHandler handler)
        {
            if (args.Count < 2 || args.Count > 3) throw new ArgumentException(string.Format(Resources.WrongNoArguments, Resources.UsageManifest));

            // Determine manifest format
            ManifestFormat format;
            if (args.Count == 3) format = ManifestFormat.FromPrefix(args[2]);
            else
            {
                try
                {
                    // Try to extract the algorithm from the directory name
                    format = ManifestFormat.FromPrefix(StringUtils.GetLeftPartAtFirstOccurrence(Path.GetFileName(Path.GetFullPath(args[1])), '='));
                }
                catch (ArgumentException)
                {
                    // Default to the best available algorithm
                    format = EnumerableUtils.GetFirst(ManifestFormat.Recommended);
                }
            }

            string path = args[1];
            if (!Directory.Exists(path)) throw new DirectoryNotFoundException(string.Format(Resources.DirectoryNotFound, path));

            var manifest = Manifest.Generate(path, format, handler, path);
            Console.Write(manifest);
            Console.WriteLine(manifest.CalculateDigest());
        }
        #endregion
    }
}
