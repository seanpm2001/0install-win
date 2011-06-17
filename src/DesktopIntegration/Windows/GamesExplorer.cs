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

using Capabilities = ZeroInstall.Model.Capabilities;
using AccessPoints = ZeroInstall.DesktopIntegration.AccessPoints;

namespace ZeroInstall.DesktopIntegration.Windows
{
    /// <summary>
    /// Contains control logic for applying <see cref="Capabilities.GamesExplorer"/> on Windows systems.
    /// </summary>
    public static class GamesExplorer
    {
        #region Constants
        /// <summary>The HKLM registry key for registering applications in the Windows Games Explorer.</summary>
        public const string RegKeyMachineGames = @"SOFTWARE\Microsoft\Windows\CurrentVersion\GameUX\Games";
        #endregion
    }
}
