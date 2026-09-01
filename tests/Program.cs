using System;
using System.Collections.Generic;
using System.Linq;
using TiaOpennessLibraryInspectorDemo;

internal static class Program
{
    private static int _passed;
    private static int _failed;

    private static int Main()
    {
        Run("classifies Unified faceplate runtime evidence", () =>
            Equal(
                "Unified faceplate candidate",
                InspectionLogic.ClassifyRuntimeTypes(
                    "LibraryType",
                    "Siemens.Engineering.HmiUnified.FaceplateType")));

        Run("classifies other Unified type evidence", () =>
            Equal(
                "Unified type candidate",
                InspectionLogic.ClassifyRuntimeTypes(
                    "Siemens.Engineering.HmiUnified.Library.ScriptModuleType",
                    "")));

        Run("does not classify a generic name without runtime evidence", () =>
            Equal(
                "Other / unknown",
                InspectionLogic.ClassifyRuntimeTypes("LibraryType", "GenericType")));

        var records = new List<LibraryTypeRecord>
        {
            Record("MotorTile", "1.0.0", "Project Library/Types/HMI", "Unified faceplate candidate", "A"),
            Record("DriveBlock", "2.0.0", "Project Library/Types/PLC", "Other / unknown", "B"),
            Record("MotorTile", "1.1.0", "Project Library/Types/HMI", "Unified faceplate candidate", "A")
        };

        Run("search is case insensitive by name", () =>
            Equal(2, InspectionLogic.Filter(records, "motortile", InspectionLogic.AllFilter).Count));

        Run("search includes library path", () =>
            Equal(1, InspectionLogic.Filter(records, "PLC", InspectionLogic.AllFilter).Count));

        Run("Unified filter keeps candidates", () =>
            Equal(2, InspectionLogic.Filter(records, "", InspectionLogic.UnifiedFilter).Count));

        Run("other filter excludes Unified candidates", () =>
            Equal(1, InspectionLogic.Filter(records, "", InspectionLogic.OtherFilter).Count));

        Run("unique type count ignores version rows", () =>
            Equal(2, InspectionLogic.CountUniqueTypes(records)));

        Run("combines recursive library paths", () =>
            Equal(
                "Project Library/Types/HMI",
                InspectionLogic.CombineLibraryPath("Project Library/Types/", "/HMI")));

        Run("CSV leaves simple values unquoted", () =>
            Equal("MotorTile", InspectionLogic.CsvEscape("MotorTile")));

        Run("CSV quotes commas", () =>
            Equal("\"Motor,Tile\"", InspectionLogic.CsvEscape("Motor,Tile")));

        Run("CSV doubles embedded quotes", () =>
            Equal("\"Motor \"\"Tile\"\"\"", InspectionLogic.CsvEscape("Motor \"Tile\"")));

        Run("CSV quotes line breaks", () =>
            Equal("\"Line 1\nLine 2\"", InspectionLogic.CsvEscape("Line 1\nLine 2")));

        Run("CSV contains public columns and every row", () =>
        {
            string csv = InspectionLogic.ToCsv(records);
            True(csv.StartsWith("Name,Version,Library Path", StringComparison.Ordinal));
            Equal(4, csv.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries).Length);
        });

        Console.WriteLine();
        Console.WriteLine($"Siemens-independent checks: {_passed} passed, {_failed} failed.");
        return _failed == 0 ? 0 : 1;
    }

    private static LibraryTypeRecord Record(
        string name,
        string version,
        string path,
        string classification,
        string guid)
    {
        return new LibraryTypeRecord
        {
            Name = name,
            Version = version,
            LibraryPath = path,
            Category = "Synthetic.RuntimeType",
            State = "Committed",
            TypeGuid = guid,
            VersionGuid = guid + "-" + version,
            Classification = classification
        };
    }

    private static void Run(string name, Action check)
    {
        try
        {
            check();
            _passed++;
            Console.WriteLine("[PASS] " + name);
        }
        catch (Exception exception)
        {
            _failed++;
            Console.WriteLine("[FAIL] " + name + ": " + exception.Message);
        }
    }

    private static void Equal<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"Expected '{expected}', got '{actual}'.");
        }
    }

    private static void True(bool condition)
    {
        if (!condition)
        {
            throw new InvalidOperationException("Expected condition to be true.");
        }
    }
}
