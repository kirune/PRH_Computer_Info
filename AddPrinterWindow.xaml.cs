using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Diagnostics;
using System.Printing;
using System.Collections.Generic;
using System.Text.Json;

namespace HelpCard
{
    public class PrinterInfo
    {
        public string FullName { get; set; }      // \\PrintServer\PrinterName
        public string DisplayName { get; set; }   // PrinterName
    }

    public partial class AddPrinterWindow : Window
    {
        public string ComputerName { get; private set; }        // UNC for mapping
        public string DisplayPrinterName { get; private set; }  // Friendly for UI
        public bool SetAsDefault { get; private set; }

        private List<PrinterInfo> allPrinters;
        private bool offDomain = false;

        private static readonly string CacheDir =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                         "PRH", "HelpCard");
        private static readonly string CacheFile = Path.Combine(CacheDir, "printers.json");
        private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(24);

        public AddPrinterWindow()
        {
            InitializeComponent();

            allPrinters = LoadPrinterList().ToList();

            if (offDomain)
            {
                PrinterComboBox.ItemsSource = new List<PrinterInfo>();
                PrinterComboBox.IsEnabled = false;
                InstructionText.Text = "Not on a domain — no printers available";
            }
            else if (!allPrinters.Any())
            {
                PrinterComboBox.ItemsSource = new List<PrinterInfo>();
                PrinterComboBox.IsEnabled = false;
                InstructionText.Text = "No printers available to add";
            }
            else
            {
                PrinterComboBox.ItemsSource = allPrinters;
                PrinterComboBox.IsEnabled = true;
                InstructionText.Text = "Enter or select a printer:";
            }
        }

        // --- Cache + fetch logic ---
        private IEnumerable<PrinterInfo> LoadPrinterList()
        {
            // 0. Bail if not domain joined
            bool isDomainJoined = !string.IsNullOrEmpty(Environment.UserDomainName)
                                  && !Environment.UserDomainName.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase);

            if (!isDomainJoined)
            {
                offDomain = true;
                return new List<PrinterInfo>();
            }

            // 1. Try cache
            if (File.Exists(CacheFile))
            {
                var age = DateTime.Now - File.GetLastWriteTime(CacheFile);
                if (age < CacheTtl)
                {
                    try
                    {
                        string json = File.ReadAllText(CacheFile);
                        var cached = JsonSerializer.Deserialize<List<string>>(json);
                        if (cached != null && cached.Any())
                        {
                            return cached.Select(p => new PrinterInfo
                            {
                                FullName = p,
                                DisplayName = p.Contains("\\") ? p.Split('\\').Last() : p
                            }).ToList();
                        }
                    }
                    catch { /* fall back to server */ }
                }
            }

            // 2. Query print server
            List<PrinterInfo> printers = new List<PrinterInfo>();
            try
            {
                var server = new PrintServer(@"\\PrintServer");
                var queues = server.GetPrintQueues(new[]
                {
                    EnumeratedPrintQueueTypes.Shared
                });

                printers = queues.Select(q => new PrinterInfo
                {
                    FullName = q.FullName,
                    DisplayName = q.FullName.Contains("\\") ? q.FullName.Split('\\').Last() : q.FullName
                }).ToList();

                // Save FullNames to cache
                Directory.CreateDirectory(CacheDir);
                string json = JsonSerializer.Serialize(printers.Select(p => p.FullName));
                File.WriteAllText(CacheFile, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to query printers from PrintServer: {ex.Message}",
                                "Printer Query Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            return printers;
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            if (offDomain || !allPrinters.Any())
            {
                this.DialogResult = false;
                this.Close();
                return;
            }

            if (PrinterComboBox.SelectedItem is not PrinterInfo selected || string.IsNullOrEmpty(selected.FullName))
            {
                MessageBox.Show("Please select a valid printer from the list.",
                                "Invalid Printer",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                return;
            }

            ComputerName = selected.FullName;         // full UNC
            DisplayPrinterName = selected.DisplayName; // friendly
            SetAsDefault = SetDefaultCheckBox.IsChecked == true;

            try
            {
                // 1. Install printer
                using (var mapProc = Process.Start(new ProcessStartInfo
                {
                    FileName = "rundll32.exe",
                    Arguments = $"printui.dll,PrintUIEntry /in /n \"{ComputerName}\"",
                    CreateNoWindow = true,
                    UseShellExecute = false
                }))
                {
                    mapProc?.WaitForExit();
                }

                // 2. Optionally set as default
                if (SetAsDefault)
                {
                    using (var defProc = Process.Start(new ProcessStartInfo
                    {
                        FileName = "rundll32.exe",
                        Arguments = $"printui.dll,PrintUIEntry /y /n \"{ComputerName}\"",
                        CreateNoWindow = true,
                        UseShellExecute = false
                    }))
                    {
                        defProc?.WaitForExit();
                    }
                }

                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to map printer: {ex.Message}",
                                "Error",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void PrinterComboBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!offDomain && allPrinters.Any())
                FilterPrinters();
        }

        private void PrinterComboBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (!offDomain && allPrinters.Any())
                FilterPrinters();
        }

        private void FilterPrinters()
        {
            string input = PrinterComboBox.Text ?? string.Empty;

            // Grab inner editable TextBox to manage caret
            var innerBox = (TextBox)PrinterComboBox.Template.FindName("PART_EditableTextBox", PrinterComboBox);
            int caretIndex = innerBox?.CaretIndex ?? input.Length;

            IEnumerable<PrinterInfo> filtered;
            if (string.IsNullOrWhiteSpace(input))
            {
                filtered = allPrinters;
            }
            else
            {
                filtered = allPrinters
                    .Where(p => p.DisplayName.IndexOf(input, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();
            }

            PrinterComboBox.ItemsSource = filtered;
            PrinterComboBox.Text = input;

            if (innerBox != null)
                innerBox.CaretIndex = caretIndex;

            PrinterComboBox.IsDropDownOpen = filtered.Any();
        }
    }
}
