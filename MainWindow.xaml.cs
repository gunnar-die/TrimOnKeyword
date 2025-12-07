#nullable enable
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using WinForms = System.Windows.Forms; // FolderBrowserDialog

namespace TrimOnKeyword
{
    public class PreviewItem
    {
        public bool IsSelected { get; set; }
        public string CurrentName { get; set; } = string.Empty;
        public string NewName { get; set; } = string.Empty;
        public string CurrentPath { get; set; } = string.Empty;
        public string NewPath { get; set; } = string.Empty;
    }

    public partial class MainWindow : Window
    {
        public ObservableCollection<PreviewItem> PreviewItems { get; } = new();
        private string _folder = string.Empty;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            SetGoEnabled(false);
        }

        // ===== Title bar handlers =====
        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }
        private void BtnMin_Click(object? sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void BtnMax_Click(object? sender, RoutedEventArgs e) =>
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        private void BtnClose_Click(object? sender, RoutedEventArgs e) => Close();

        // ===== App logic =====
        private void BtnPick_Click(object sender, RoutedEventArgs e)
        {
            using var fbd = new WinForms.FolderBrowserDialog { ShowNewFolderButton = false };
            if (fbd.ShowDialog() == WinForms.DialogResult.OK)
            {
                _folder = fbd.SelectedPath;
                LblFolder.Text = _folder;
            }
        }

        private async void BtnScan_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInputs(out var folder, out var keyword)) return;
            await BuildPreviewAsync(folder, keyword);
            SetGoEnabled(PreviewItems.Any(p => p.IsSelected && !string.Equals(p.CurrentPath, p.NewPath, StringComparison.Ordinal)));
        }

        private void BtnSelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in PreviewItems) item.IsSelected = true;
            GridPreview.Items.Refresh();
            SetGoEnabled(PreviewItems.Any(p => p.IsSelected && !string.Equals(p.CurrentPath, p.NewPath, StringComparison.Ordinal)));
        }

        private void BtnDeselectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in PreviewItems) item.IsSelected = false;
            GridPreview.Items.Refresh();
            SetGoEnabled(false);
        }

        private async void BtnGo_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInputs(out var folder, out var keyword)) return;

            var toRename = PreviewItems
                .Where(p => p.IsSelected && !string.Equals(p.CurrentPath, p.NewPath, StringComparison.Ordinal))
                .ToList();

            if (toRename.Count == 0)
            {
                MessageBox.Show(this, "Nothing selected to rename.", "Done",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (MessageBox.Show(this, $"Proceed to rename {toRename.Count} files?", "Confirm",
                                MessageBoxButton.OKCancel, MessageBoxImage.Question) != MessageBoxResult.OK)
                return;

            int ok = 0, skipped = 0, errors = 0;
            Prog.Value = 0;
            int total = toRename.Count;
            int i = 0;

            await Task.Run(() =>
            {
                foreach (var item in toRename)
                {
                    try
                    {
                        var dir = Path.GetDirectoryName(item.NewPath)!;
                        Directory.CreateDirectory(dir);
                        File.Move(item.CurrentPath, item.NewPath);
                        ok++;
                    }
                    catch (IOException ioex)
                    {
                        item.NewName += $"  (skipped: {ioex.Message})";
                        skipped++;
                    }
                    catch (Exception ex)
                    {
                        item.NewName += $"  (error: {ex.Message})";
                        errors++;
                    }
                    finally
                    {
                        i++;
                        Dispatcher.Invoke(() => Prog.Value = (100.0 * i) / total);
                        Dispatcher.Invoke(() => GridPreview.Items.Refresh());
                    }
                }
            });

            MessageBox.Show(this, $"Done. Renamed: {ok}, Skipped: {skipped}, Errors: {errors}",
                "Result", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SetGoEnabled(bool enabled) => BtnGo.IsEnabled = enabled;

        private bool ValidateInputs(out string folder, out string keyword)
        {
            folder = _folder;
            keyword = TxtKeyword.Text ?? string.Empty;

            if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
            {
                MessageBox.Show(this, "Pick a folder first.", "Missing folder",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            if (string.IsNullOrWhiteSpace(keyword))
            {
                MessageBox.Show(this, "Enter a keyword.", "Missing keyword",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        private async Task BuildPreviewAsync(string folder, string keyword)
        {
            PreviewItems.Clear();
            Prog.Value = 0;
            SetGoEnabled(false);

            var cmp = (ChkCase.IsChecked == true) ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            var files = Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories).ToList();
            int total = files.Count;

            if (total == 0)
            {
                PreviewItems.Add(new PreviewItem { CurrentName = "(no files)" });
                return;
            }

            await Task.Run(() =>
            {
                for (int j = 0; j < files.Count; j++)
                {
                    var file = files[j];
                    string dir = Path.GetDirectoryName(file)!;
                    string name = Path.GetFileNameWithoutExtension(file);
                    string ext = Path.GetExtension(file);

                    int idx = name.IndexOf(keyword, cmp);
                    if (idx >= 0)
                    {
                        string trimmedBase = TrimTrailingSeparators(name.Substring(0, idx));
                        if (!string.IsNullOrWhiteSpace(trimmedBase))
                        {
                            string target = Path.Combine(dir, trimmedBase + ext);
                            string unique = GetUniqueTargetPath(target);

                            var item = new PreviewItem
                            {
                                IsSelected = true,
                                CurrentName = Path.GetFileName(file),
                                NewName = Path.GetFileName(unique),
                                CurrentPath = file,
                                NewPath = unique
                            };
                            Dispatcher.Invoke(() => PreviewItems.Add(item));
                        }
                    }

                    Dispatcher.Invoke(() => Prog.Value = (100.0 * (j + 1)) / total);
                }
            });

            if (PreviewItems.Count == 0)
                PreviewItems.Add(new PreviewItem { CurrentName = "(no matches)" });
        }

        private static string TrimTrailingSeparators(string s) => s.TrimEnd('.', '-', '_', ' ');
        private static string GetUniqueTargetPath(string desiredPath)
        {
            if (!File.Exists(desiredPath)) return desiredPath;
            string dir = Path.GetDirectoryName(desiredPath)!;
            string fn = Path.GetFileNameWithoutExtension(desiredPath);
            string ext = Path.GetExtension(desiredPath);
            int i = 1;
            string candidate;
            do
            {
                candidate = Path.Combine(dir, $"{fn} ({i}){ext}");
                i++;
            } while (File.Exists(candidate));
            return candidate;
        }
    }
}
