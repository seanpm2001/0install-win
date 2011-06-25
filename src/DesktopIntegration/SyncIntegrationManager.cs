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
using System.Net;
using Common;
using Common.Storage;
using Common.Tasks;
using Common.Utils;
using ICSharpCode.SharpZipLib.Zip;
using ZeroInstall.DesktopIntegration.AccessPoints;
using ZeroInstall.Model;
using Capabilities = ZeroInstall.Model.Capabilities;

namespace ZeroInstall.DesktopIntegration
{
    /// <summary>
    /// Synchronizes the <see cref="AppList"/> with other computers.
    /// </summary>
    public class SyncIntegrationManager : IntegrationManager
    {
        #region Constants
        private const string AppListLastSyncSuffix = ".last-sync";
        #endregion

        #region Variables
        /// <summary>The base URL of the sync server.</summary>
        private readonly Uri _syncServer;

        /// <summary>The username to authenticate with against the <see cref="_syncServer"/>.</summary>
        private readonly string _username;

        /// <summary>The password to authenticate with against the <see cref="_syncServer"/>.</summary>
        private readonly string _password;

        /// <summary>The local key used to encrypt data before sending it to the <see cref="_syncServer"/>.</summary>
        private readonly string _cryptoKey;

        /// <summary>The storage location of the <see cref="AppList"/> file.</summary>
        private readonly AppList _appListLastSync;
        #endregion

        #region Constructor
        /// <summary>
        /// Creates a new sync manager.
        /// </summary>
        /// <param name="systemWide">Apply operations system-wide instead of just for the current user.</param>
        /// <param name="syncServer">The base URL of the sync server.</param>
        /// <param name="username">The username to authenticate with against the <paramref name="syncServer"/>.</param>
        /// <param name="password">The password to authenticate with against the <paramref name="syncServer"/>.</param>
        /// <param name="cryptoKey">The local key used to encrypt data before sending it to the <paramref name="syncServer"/>.</param>
        /// <exception cref="IOException">Thrown if a problem occurs while accessing the <see cref="AppList"/> file.</exception>
        /// <exception cref="UnauthorizedAccessException">Thrown if read or write access to the <see cref="AppList"/> file is not permitted.</exception>
        /// <exception cref="InvalidDataException">Thrown if a problem occurs while deserializing the XML data.</exception>
        public SyncIntegrationManager(bool systemWide, Uri syncServer, string username, string password, string cryptoKey)
            : base(systemWide)
        {
            #region Sanity checks
            if (syncServer == null) throw new ArgumentNullException("syncServer");
            if (string.IsNullOrEmpty(username)) throw new ArgumentNullException("username");
            if (password == null) throw new ArgumentNullException("password");
            if (cryptoKey == null) throw new ArgumentNullException("cryptoKey");
            #endregion

            _syncServer = syncServer;
            _username = username;
            _password = password;
            _cryptoKey = cryptoKey;

            if (File.Exists(AppListPath + AppListLastSyncSuffix)) _appListLastSync = AppList.Load(AppListPath + AppListLastSyncSuffix);
            else
            {
                _appListLastSync = new AppList();
                _appListLastSync.Save(AppListPath + AppListLastSyncSuffix);
            }
        }
        #endregion

        //--------------------//

        #region Sync
        /// <summary>
        /// Synchronize the <see cref="AppList"/> with the sync server and (un)apply <see cref="AccessPoint"/>s accordingly.
        /// </summary>
        /// <param name="feedRetreiver">Callback method used to retreive additional <see cref="Feed"/>s on demand.</param>
        /// <param name="handler">A callback object used when the the user is to be informed about the progress of long-running operations such as uploads and downloads.</param>
        /// <exception cref="UserCancelException">Thrown if the user canceled the task.</exception>
        /// <exception cref="IOException">Thrown if a problem occurs while writing to the filesystem or registry.</exception>
        /// <exception cref="InvalidDataException">Thrown if a problem occurred while deserializing the XML data or if the specified crypto key was wrong.</exception>
        /// <exception cref="WebException">Thrown if a problem occured while downloading additional data (such as icons).</exception>
        /// <exception cref="UnauthorizedAccessException">Thrown if write access to the filesystem or registry is not permitted.</exception>
        public void Sync(Converter<string, Feed> feedRetreiver, ITaskHandler handler)
        {
            #region Sanity checks
            if (feedRetreiver == null) throw new ArgumentNullException("feedRetreiver");
            if (handler == null) throw new ArgumentNullException("handler");
            #endregion

            var appListUri = new Uri(_syncServer, new Uri("app-list", UriKind.Relative));
            var webClient = new WebClient {Credentials = new NetworkCredential(_username, _password)};

            // Download and merge the current AppList from the server
            // ToDo: Use ITask to allow for cancellation
            var appListData = webClient.DownloadData(appListUri);
            if (appListData.Length > 0)
            {
                try
                {
                    var newData = XmlStorage.FromZip<AppList>(new MemoryStream(appListData), _cryptoKey, null);
                    MergeNewData(newData, feedRetreiver, handler);
                }
                #region Error handling
                catch (ZipException ex)
                { // Wrap exception since only certain exception types are allowed
                    throw new InvalidDataException(ex.Message, ex);
                }
                #endregion
            }

            // Upload the encrypted AppList back to the server
            var memoryStream = new MemoryStream();
            XmlStorage.ToZip(memoryStream, AppList, _cryptoKey, null);
            // ToDo: Use ITask to allow for cancellation
            webClient.UploadData(appListUri, "PUT", memoryStream.ToArray());

            // Save reference point for sync
            AppList.Save(AppListPath + AppListLastSyncSuffix);
        }

        /// <summary>
        /// Merges a new <see cref="AppList"/> with the existing data.
        /// </summary>
        /// <param name="newAppList">The new <see cref="AppList"/> to merge in.</param>
        /// <param name="feedRetreiver">Callback method used to retreive additional <see cref="Feed"/>s on demand.</param>
        /// <param name="handler">A callback object used when the the user is to be informed about the progress of long-running operations such as downloads.</param>
        /// <exception cref="UserCancelException">Thrown if the user canceled the task.</exception>
        /// <exception cref="IOException">Thrown if a problem occurs while writing to the filesystem or registry.</exception>
        /// <exception cref="InvalidOperationException">Thrown if one or more new <see cref="AccessPoint"/> would cause a conflict with the existing <see cref="AccessPoint"/>s in <see cref="AppList"/>.</exception>
        /// <exception cref="WebException">Thrown if a problem occured while downloading additional data (such as icons).</exception>
        /// <exception cref="UnauthorizedAccessException">Thrown if write access to the filesystem or registry is not permitted.</exception>
        /// <exception cref="InvalidDataException">Thrown if one of the <see cref="AccessPoint"/>s or <see cref="Capabilities.Capability"/>s is invalid.</exception>
        /// <remarks>Performs a three-way merge using <see cref="_appListLastSync"/> as base, <see cref="IntegrationManager.AppList"/> as mine and <paramref name="newAppList"/> as theirs.</remarks>
        private void MergeNewData(AppList newAppList, Converter<string, Feed> feedRetreiver, ITaskHandler handler)
        {
            #region Sanity checks
            if (newAppList == null) throw new ArgumentNullException("newAppList");
            if (feedRetreiver == null) throw new ArgumentNullException("feedRetreiver");
            if (handler == null) throw new ArgumentNullException("handler");
            #endregion

            // ToDo: Redesign to allow for conflict resolution within AppEntrys

            var toRemove = new LinkedList<string>();
            var toAdd = new LinkedList<AppEntry>();

            foreach (var existingEntry in AppList.Entries)
            {
                AppEntry foundEntry;
                if (newAppList.Entries.Find(newEntry => newEntry.InterfaceID == existingEntry.InterfaceID, out foundEntry))
                { // In mine and theirs => maybe both changes
                    if (foundEntry.Timestamp > existingEntry.Timestamp)
                    {
                        toRemove.AddLast(existingEntry.InterfaceID);
                        toAdd.AddLast(foundEntry);
                    }
                }
                else if (_appListLastSync.Entries.Find(oldEntry => oldEntry.InterfaceID == existingEntry.InterfaceID, out foundEntry))
                { // In mine, not in theirs, in base => was removed remotely
                    toRemove.AddLast(existingEntry.InterfaceID);
                }
            }

            foreach (var newEntry in newAppList.Entries)
            {
                AppEntry foundEntry;
                if (!AppList.Entries.Find(existingEntry => existingEntry.InterfaceID == newEntry.InterfaceID, out foundEntry))
                {
                    if (!_appListLastSync.Entries.Find(oldEntry => oldEntry.InterfaceID == newEntry.InterfaceID, out foundEntry))
                    { // In theirs, not in mine, not in base => was added remotely
                        toAdd.AddLast(newEntry);
                    }
                }
            }

            foreach (string interfaceID in toRemove)
                RemoveApp(interfaceID);

            foreach (var appEntry in toAdd)
            {
                // Clone the AppEntry without the access points
                var newAppEntry = new AppEntry {InterfaceID = appEntry.InterfaceID, Name = appEntry.Name, AutoUpdate = appEntry.AutoUpdate, Timestamp = FileUtils.ToUnixTime(DateTime.UtcNow)};
                foreach (var capabilityList in appEntry.CapabilityLists)
                    newAppEntry.CapabilityLists.Add(capabilityList.CloneCapabilityList());
                AppList.Entries.Add(newAppEntry);

                // Add the access points
                AddAccessPoints(new InterfaceFeed(appEntry.InterfaceID, feedRetreiver(appEntry.InterfaceID)), appEntry.AccessPoints.Entries, handler);
            }
        }
        #endregion
    }
}
