using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace TiaOpennessLibraryInspectorDemo
{
    internal sealed class LibraryTypeRecord
    {
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string LibraryPath { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string TypeGuid { get; set; } = string.Empty;
        public string VersionGuid { get; set; } = string.Empty;
        public string Classification { get; set; } = string.Empty;
    }

    internal sealed class InspectionResult
    {
        public string ProjectName { get; set; } = string.Empty;
        public List<LibraryTypeRecord> Records { get; } = new List<LibraryTypeRecord>();
        public List<string> Warnings { get; } = new List<string>();
    }

    internal static class InspectionLogic
    {
        public const string AllFilter = "All types";
        public const string UnifiedFilter = "Unified candidates";
        public const string OtherFilter = "Other / unknown";

        public static string ClassifyRuntimeTypes(
            string libraryRuntimeType,
            string typeObjectRuntimeType)
        {
            string evidence = (libraryRuntimeType + " " + typeObjectRuntimeType).ToLowerInvariant();
            bool unified = evidence.Contains("hmiunified") || evidence.Contains("winccunified");
            bool faceplate = evidence.Contains("faceplate");

            if (unified && faceplate)
            {
                return "Unified faceplate candidate";
            }

            if (unified)
            {
                return "Unified type candidate";
            }

            return "Other / unknown";
        }

        public static IReadOnlyList<LibraryTypeRecord> Filter(
            IEnumerable<LibraryTypeRecord> records,
            string searchText,
            string classificationFilter)
        {
            string search = (searchText ?? string.Empty).Trim();
            IEnumerable<LibraryTypeRecord> query = records ?? Enumerable.Empty<LibraryTypeRecord>();

            if (search.Length > 0)
            {
                query = query.Where(record =>
                    Contains(record.Name, search) ||
                    Contains(record.Version, search) ||
                    Contains(record.LibraryPath, search) ||
                    Contains(record.Category, search) ||
                    Contains(record.Classification, search));
            }

            if (string.Equals(classificationFilter, UnifiedFilter, StringComparison.Ordinal))
            {
                query = query.Where(record =>
                    record.Classification.StartsWith("Unified", StringComparison.Ordinal));
            }
            else if (string.Equals(classificationFilter, OtherFilter, StringComparison.Ordinal))
            {
                query = query.Where(record =>
                    !record.Classification.StartsWith("Unified", StringComparison.Ordinal));
            }

            return query
                .OrderBy(record => record.LibraryPath, StringComparer.OrdinalIgnoreCase)
                .ThenBy(record => record.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(record => record.Version, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public static int CountUniqueTypes(IEnumerable<LibraryTypeRecord> records)
        {
            return (records ?? Enumerable.Empty<LibraryTypeRecord>())
                .Select(record => string.IsNullOrWhiteSpace(record.TypeGuid)
                    ? record.LibraryPath + "\u001f" + record.Name
                    : record.TypeGuid)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();
        }

        public static string CombineLibraryPath(string parent, string child)
        {
            string left = (parent ?? string.Empty).Trim().TrimEnd('/');
            string right = (child ?? string.Empty).Trim().Trim('/');
            if (left.Length == 0)
            {
                return right;
            }

            return right.Length == 0 ? left : left + "/" + right;
        }

        public static string ToCsv(IEnumerable<LibraryTypeRecord> records)
        {
            var text = new StringBuilder();
            text.AppendLine(
                "Name,Version,Library Path,Category,State,Type GUID,Version GUID,Classification");

            foreach (LibraryTypeRecord record in records ?? Enumerable.Empty<LibraryTypeRecord>())
            {
                text.AppendLine(string.Join(",", new[]
                {
                    CsvEscape(record.Name),
                    CsvEscape(record.Version),
                    CsvEscape(record.LibraryPath),
                    CsvEscape(record.Category),
                    CsvEscape(record.State),
                    CsvEscape(record.TypeGuid),
                    CsvEscape(record.VersionGuid),
                    CsvEscape(record.Classification)
                }));
            }

            return text.ToString();
        }

        public static string CsvEscape(object value)
        {
            string text = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
            if (text.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0)
            {
                return text;
            }

            return "\"" + text.Replace("\"", "\"\"") + "\"";
        }

        private static bool Contains(string value, string search)
        {
            return (value ?? string.Empty).IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
