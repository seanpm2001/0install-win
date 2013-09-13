﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.18051
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace ZeroInstall.Capture.Properties {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    public class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("ZeroInstall.Capture.Properties.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Capture directory initialized..
        /// </summary>
        public static string CaptureDirInitialized {
            get {
                return ResourceManager.GetString("CaptureDirInitialized", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The directory &apos;{0}&apos; is not empty..
        /// </summary>
        public static string DirectoryNotEmpty {
            get {
                return ResourceManager.GetString("DirectoryNotEmpty", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Detected &apos;{0}&apos; as installation directory..
        /// </summary>
        public static string InstallationDirDetected {
            get {
                return ResourceManager.GetString("InstallationDirDetected", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Installation data collected.
        ///Please edit the newly created &apos;feed.xml&apos; to fit your needs..
        /// </summary>
        public static string InstallDataCollected {
            get {
                return ResourceManager.GetString("InstallDataCollected", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Invalid capabilities registry path: {0}.
        /// </summary>
        public static string InvalidCapabilitiesRegistryPath {
            get {
                return ResourceManager.GetString("InvalidCapabilitiesRegistryPath", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to No 32-bit %ProgramFiles% directory found..
        /// </summary>
        public static string MissingProgramFiles32Bit {
            get {
                return ResourceManager.GetString("MissingProgramFiles32Bit", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Multiple new default programs were detected in the registry. Handling them all, but the last one may take precedence in  some cases..
        /// </summary>
        public static string MultipleDefaultProgramsDetected {
            get {
                return ResourceManager.GetString("MultipleDefaultProgramsDetected", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Multiple installation directories were detected. Choosing first by default..
        /// </summary>
        public static string MultipleInstallationDirsDetected {
            get {
                return ResourceManager.GetString("MultipleInstallationDirsDetected", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Multiple new application registrations were detected in the registry. Choosing first by default..
        /// </summary>
        public static string MultipleRegisteredAppsDetected {
            get {
                return ResourceManager.GetString("MultipleRegisteredAppsDetected", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to No installation directory was detected..
        /// </summary>
        public static string NoInstallationDirDetected {
            get {
                return ResourceManager.GetString("NoInstallationDirDetected", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The directory &apos;{0}&apos; is not a capture directory. Please use &apos;0capture init&apos;..
        /// </summary>
        public static string NotCaptureDirectory {
            get {
                return ResourceManager.GetString("NotCaptureDirectory", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to This method is currently only available on Windows..
        /// </summary>
        public static string OnlyAvailableOnWindows {
            get {
                return ResourceManager.GetString("OnlyAvailableOnWindows", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Post-installation snapshot created..
        /// </summary>
        public static string PostInstallSnapshotCreated {
            get {
                return ResourceManager.GetString("PostInstallSnapshotCreated", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Pre-installation snapshot created..
        /// </summary>
        public static string PreInstallSnapshotCreated {
            get {
                return ResourceManager.GetString("PreInstallSnapshotCreated", resourceCulture);
            }
        }
    }
}
