namespace TiaOpennessLibraryInspectorDemo
{
    /// <summary>
    /// TIA Portal version the build targets (TiaPortalVersion in the project file).
    /// V20 ships one Siemens.Engineering.dll in PublicAPI\V20; V21 splits it into
    /// Siemens.Engineering.Base.dll and friends in PublicAPI\V21\net48.
    /// </summary>
    internal static class TiaVersion
    {
#if TIA_V20
        public const string Name = "V20";
        public const string PublicApiEnvironmentVariable = "TIA_PORTAL_V20_PUBLIC_API";
        public const string MainAssemblyFile = "Siemens.Engineering.dll";
        public static readonly string[] PublicApiRelativePath = { "Siemens", "Automation", "Portal V20", "PublicAPI", "V20" };
#else
        public const string Name = "V21";
        public const string PublicApiEnvironmentVariable = "TIA_PORTAL_V21_PUBLIC_API";
        public const string MainAssemblyFile = "Siemens.Engineering.Base.dll";
        public static readonly string[] PublicApiRelativePath = { "Siemens", "Automation", "Portal V21", "PublicAPI", "V21", "net48" };
#endif
    }
}
