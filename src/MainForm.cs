using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace TiaOpennessLibraryInspectorDemo
{
    internal sealed class MainForm : Form
    {
        private readonly TiaLibraryInspector _inspector;
        private readonly ComboBox _processes = new ComboBox();
        private readonly Button _refreshButton = new Button();
        private readonly Button _inspectButton = new Button();
        private readonly TextBox _searchBox = new TextBox();
        private readonly ComboBox _classificationFilter = new ComboBox();
        private readonly Button _exportButton = new Button();
        private readonly DataGridView _grid = new DataGridView();
        private readonly Label _countLabel = new Label();
        private readonly Label _statusLabel = new Label();
        private List<LibraryTypeRecord> _allRecords = new List<LibraryTypeRecord>();

        public MainForm(TiaLibraryInspector inspector)
        {
            _inspector = inspector ?? throw new ArgumentNullException(nameof(inspector));
            Text = "TIA Openness Library Inspector — Public Demo (TIA Portal " + TiaVersion.Name + ")";
            MinimumSize = new Size(980, 620);
            Size = new Size(1280, 760);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 9F);

            BuildInterface();
            ConfigureGrid();
            Shown += delegate { RefreshProcesses(); };
        }

        private void BuildInterface()
        {
            BackColor = Color.FromArgb(245, 247, 250);

            var title = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                ForeColor = Color.FromArgb(20, 62, 103),
                Text = "TIA Openness Library Inspector"
            };
            var subtitle = new Label
            {
                AutoSize = true,
                ForeColor = Color.FromArgb(80, 96, 112),
                Text = "Read-only inspection of Project Library type and version metadata"
            };

            _processes.DropDownStyle = ComboBoxStyle.DropDownList;
            _processes.Width = 530;
            _refreshButton.Text = "Refresh Processes";
            _refreshButton.AutoSize = true;
            _refreshButton.Click += delegate { RefreshProcesses(); };
            _inspectButton.Text = "Attach && Inspect";
            _inspectButton.AutoSize = true;
            _inspectButton.BackColor = Color.FromArgb(30, 102, 76);
            _inspectButton.ForeColor = Color.White;
            _inspectButton.FlatStyle = FlatStyle.Flat;
            _inspectButton.Click += delegate { AttachAndInspect(); };

            var processRow = new FlowLayoutPanel
            {
                AutoSize = true,
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Padding = new Padding(0, 4, 0, 4)
            };
            processRow.Controls.Add(new Label
            {
                AutoSize = true,
                Padding = new Padding(0, 7, 4, 0),
                Text = "Running TIA Portal:"
            });
            processRow.Controls.Add(_processes);
            processRow.Controls.Add(_refreshButton);
            processRow.Controls.Add(_inspectButton);

            _searchBox.Width = 410;
            _searchBox.TextChanged += delegate { ApplyFilter(); };
            _classificationFilter.DropDownStyle = ComboBoxStyle.DropDownList;
            _classificationFilter.Items.AddRange(new object[]
            {
                InspectionLogic.AllFilter,
                InspectionLogic.UnifiedFilter,
                InspectionLogic.OtherFilter
            });
            _classificationFilter.SelectedIndex = 0;
            _classificationFilter.SelectedIndexChanged += delegate { ApplyFilter(); };
            _exportButton.Text = "Export Filtered CSV";
            _exportButton.AutoSize = true;
            _exportButton.Enabled = false;
            _exportButton.Click += delegate { ExportCsv(); };
            _countLabel.AutoSize = true;
            _countLabel.Padding = new Padding(8, 7, 0, 0);
            _countLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            _countLabel.ForeColor = Color.FromArgb(23, 79, 134);

            var filterRow = new FlowLayoutPanel
            {
                AutoSize = true,
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Padding = new Padding(0, 4, 0, 4)
            };
            filterRow.Controls.Add(new Label
            {
                AutoSize = true,
                Padding = new Padding(0, 7, 4, 0),
                Text = "Search:"
            });
            filterRow.Controls.Add(_searchBox);
            filterRow.Controls.Add(_classificationFilter);
            filterRow.Controls.Add(_exportButton);
            filterRow.Controls.Add(_countLabel);

            _statusLabel.AutoSize = true;
            _statusLabel.ForeColor = Color.FromArgb(80, 96, 112);
            _statusLabel.Text = $"Select a running TIA Portal {TiaVersion.Name} process with an open project or Multiuser local session.";

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 6,
                Padding = new Padding(16),
                BackColor = BackColor
            };
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.Controls.Add(title, 0, 0);
            layout.Controls.Add(subtitle, 0, 1);
            layout.Controls.Add(processRow, 0, 2);
            layout.Controls.Add(filterRow, 0, 3);
            layout.Controls.Add(_grid, 0, 4);
            layout.Controls.Add(_statusLabel, 0, 5);
            Controls.Add(layout);
        }

        private void ConfigureGrid()
        {
            _grid.Dock = DockStyle.Fill;
            _grid.ReadOnly = true;
            _grid.AllowUserToAddRows = false;
            _grid.AllowUserToDeleteRows = false;
            _grid.AllowUserToOrderColumns = true;
            _grid.AutoGenerateColumns = false;
            _grid.BackgroundColor = Color.White;
            _grid.BorderStyle = BorderStyle.FixedSingle;
            _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _grid.MultiSelect = false;
            _grid.RowHeadersVisible = false;
            _grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(242, 247, 251);
            _grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(23, 79, 134);
            _grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            _grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            _grid.EnableHeadersVisualStyles = false;

            AddColumn("Name", "Name", 170);
            AddColumn("Version", "Version", 85);
            AddColumn("Library Path", "LibraryPath", 220);
            AddColumn("Category", "Category", 250);
            AddColumn("State", "State", 95);
            AddColumn("Type GUID", "TypeGuid", 220);
            AddColumn("Version GUID", "VersionGuid", 220);
            AddColumn("Classification", "Classification", 190);
        }

        private void AddColumn(string title, string property, int width)
        {
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = title,
                DataPropertyName = property,
                Width = width,
                SortMode = DataGridViewColumnSortMode.Automatic
            });
        }

        private void RefreshProcesses()
        {
            SetBusy(true, "Reading running TIA Portal processes…");
            try
            {
                IReadOnlyList<TiaProcessInfo> processes = _inspector.ListRunningProcesses();
                _processes.Items.Clear();
                foreach (TiaProcessInfo process in processes)
                {
                    _processes.Items.Add(process);
                }

                if (_processes.Items.Count > 0)
                {
                    _processes.SelectedIndex = 0;
                    _statusLabel.Text = $"Found {_processes.Items.Count} running TIA Portal process(es).";
                }
                else
                {
                    _statusLabel.Text = "No running TIA Portal process was found.";
                }
            }
            catch (Exception exception)
            {
                ShowError("Could not list TIA Portal processes", exception);
            }
            finally
            {
                SetBusy(false, null);
            }
        }

        private void AttachAndInspect()
        {
            var process = _processes.SelectedItem as TiaProcessInfo;
            if (process == null)
            {
                MessageBox.Show(
                    this,
                    "Select a running TIA Portal process first.",
                    "Nothing selected",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            SetBusy(true, "Attaching read-only and inspecting Project Library types…");
            try
            {
                InspectionResult result = _inspector.AttachAndInspect(process);
                _allRecords = result.Records;
                ApplyFilter();
                string warningText = result.Warnings.Count == 0
                    ? "No metadata warnings."
                    : $"{result.Warnings.Count} metadata warning(s); unavailable fields were left blank.";
                _statusLabel.Text =
                    $"Read-only scan complete for project '{result.ProjectName}'. {warningText}";
                _exportButton.Enabled = _allRecords.Count > 0;
            }
            catch (Exception exception)
            {
                ShowError("TIA Portal inspection failed", exception);
            }
            finally
            {
                SetBusy(false, null);
            }
        }

        private void ApplyFilter()
        {
            string selectedFilter = Convert.ToString(_classificationFilter.SelectedItem)
                ?? InspectionLogic.AllFilter;
            IReadOnlyList<LibraryTypeRecord> visible = InspectionLogic.Filter(
                _allRecords,
                _searchBox.Text,
                selectedFilter);
            _grid.DataSource = visible.ToList();
            _countLabel.Text =
                $"{visible.Count} visible / {_allRecords.Count} rows / " +
                $"{InspectionLogic.CountUniqueTypes(_allRecords)} types";
        }

        private void ExportCsv()
        {
            var rows = _grid.DataSource as IEnumerable<LibraryTypeRecord>;
            List<LibraryTypeRecord> visible = rows == null
                ? new List<LibraryTypeRecord>()
                : rows.ToList();
            if (visible.Count == 0)
            {
                MessageBox.Show(
                    this,
                    "There are no visible rows to export.",
                    "Nothing to export",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            using (var dialog = new SaveFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv",
                FileName = "tia_library_inspection.csv",
                AddExtension = true,
                DefaultExt = "csv",
                OverwritePrompt = true
            })
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                File.WriteAllText(
                    dialog.FileName,
                    InspectionLogic.ToCsv(visible),
                    new UTF8Encoding(true));
                _statusLabel.Text = $"Exported {visible.Count} filtered row(s) to CSV.";
            }
        }

        private void SetBusy(bool busy, string message)
        {
            UseWaitCursor = busy;
            _refreshButton.Enabled = !busy;
            _inspectButton.Enabled = !busy;
            if (!string.IsNullOrWhiteSpace(message))
            {
                _statusLabel.Text = message;
                _statusLabel.Refresh();
            }
        }

        private void ShowError(string title, Exception exception)
        {
            _statusLabel.Text = title + ".";
            MessageBox.Show(
                this,
                exception.Message,
                title,
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}
