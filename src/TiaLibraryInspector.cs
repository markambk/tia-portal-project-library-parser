using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Siemens.Engineering;
using Siemens.Engineering.Library.Types;

namespace TiaOpennessLibraryInspectorDemo
{
    internal sealed class TiaProcessInfo
    {
        public TiaProcessInfo(TiaPortalProcess process)
        {
            Process = process ?? throw new ArgumentNullException(nameof(process));
            ProcessId = process.Id;
            Mode = process.Mode.ToString();
            ProjectPath = process.ProjectPath == null
                ? "No project open"
                : process.ProjectPath.FullName;
        }

        public int ProcessId { get; }
        public string Mode { get; }
        public string ProjectPath { get; }
        internal TiaPortalProcess Process { get; }

        public override string ToString()
        {
            string projectName = ProjectPath == "No project open"
                ? ProjectPath
                : Path.GetFileName(ProjectPath);
            return $"PID {ProcessId} · {Mode} · {projectName}";
        }
    }

    internal sealed class TiaLibraryInspector
    {
        public IReadOnlyList<TiaProcessInfo> ListRunningProcesses()
        {
            return TiaPortal.GetProcesses()
                .Select(process => new TiaProcessInfo(process))
                .OrderBy(process => process.ProcessId)
                .ToList();
        }

        public InspectionResult AttachAndInspect(TiaProcessInfo processInfo)
        {
            if (processInfo == null)
            {
                throw new ArgumentNullException(nameof(processInfo));
            }

            using (TiaPortal portal = processInfo.Process.Attach())
            {
                Project project = portal.Projects.FirstOrDefault();
                if (project == null)
                {
                    throw new InvalidOperationException(
                        "The selected TIA Portal process does not have an open project.");
                }

                var result = new InspectionResult { ProjectName = project.Name };
                LibraryTypeSystemFolder root = project.ProjectLibrary.TypeFolder;
                ScanFolder(root, "Project Library/Types", result);
                return result;
            }
        }

        private static void ScanFolder(
            LibraryTypeFolder folder,
            string path,
            InspectionResult result)
        {
            foreach (LibraryType libraryType in folder.Types)
            {
                ReadLibraryType(libraryType, path, result);
            }

            foreach (LibraryTypeUserFolder child in folder.Folders)
            {
                ScanFolder(
                    child,
                    InspectionLogic.CombineLibraryPath(path, child.Name),
                    result);
            }
        }

        private static void ReadLibraryType(
            LibraryType libraryType,
            string path,
            InspectionResult result)
        {
            string libraryRuntimeType = RuntimeTypeName(libraryType);
            string typeGuid = libraryType.Guid.ToString("D");
            bool foundVersion = false;

            try
            {
                foreach (LibraryTypeVersion version in libraryType.Versions)
                {
                    foundVersion = true;
                    string typeObjectRuntimeType = TryReadTypeObjectRuntimeType(
                        version,
                        libraryType.Name,
                        result.Warnings);
                    result.Records.Add(new LibraryTypeRecord
                    {
                        Name = libraryType.Name,
                        Version = version.VersionNumber.ToString(),
                        LibraryPath = path,
                        Category = string.IsNullOrWhiteSpace(typeObjectRuntimeType)
                            ? libraryRuntimeType
                            : typeObjectRuntimeType,
                        State = version.State.ToString(),
                        TypeGuid = typeGuid,
                        VersionGuid = version.Guid.ToString("D"),
                        Classification = InspectionLogic.ClassifyRuntimeTypes(
                            libraryRuntimeType,
                            typeObjectRuntimeType)
                    });
                }
            }
            catch (Exception exception)
            {
                result.Warnings.Add(
                    $"Could not enumerate every version of '{libraryType.Name}': " +
                    exception.GetType().Name);
            }

            if (!foundVersion)
            {
                result.Records.Add(new LibraryTypeRecord
                {
                    Name = libraryType.Name,
                    Version = string.Empty,
                    LibraryPath = path,
                    Category = libraryRuntimeType,
                    State = "No visible versions",
                    TypeGuid = typeGuid,
                    VersionGuid = string.Empty,
                    Classification = InspectionLogic.ClassifyRuntimeTypes(
                        libraryRuntimeType,
                        string.Empty)
                });
            }
        }

        private static string TryReadTypeObjectRuntimeType(
            LibraryTypeVersion version,
            string typeName,
            ICollection<string> warnings)
        {
            try
            {
                LibraryType typeObject = version.TypeObject;
                return typeObject == null ? string.Empty : RuntimeTypeName(typeObject);
            }
            catch (Exception exception)
            {
                warnings.Add(
                    $"Runtime type metadata was unavailable for '{typeName}' " +
                    $"version {version.VersionNumber}: {exception.GetType().Name}");
                return string.Empty;
            }
        }

        private static string RuntimeTypeName(object value)
        {
            Type type = value.GetType();
            return type.FullName ?? type.Name;
        }
    }
}
