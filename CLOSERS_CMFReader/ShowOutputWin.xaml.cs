using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Win32;
using CLOSERS_CMFReader.WinForms;
using CLOSERS_CMFReader.Classes;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Windows.Threading;
using Path = System.IO.Path;

namespace CLOSERS_CMFReader
{
    /// <summary>
    /// ShowOutputWin.xaml 的互動邏輯
    /// </summary>
    public partial class ShowOutputWin : Window
    {
        private List<string> Unpack_file;
        private List<string> UnpackSelect_file = new List<string>();
        private string cmf_motion;

        private int totalFiles = 0;
        private int filesProcessed = 0;
        private ConcurrentQueue<string> logQueue = new ConcurrentQueue<string>();
        private DispatcherTimer uiTimer;

        public ShowOutputWin(string motion, List<string> CMF_files, int Datanum)
        {
            InitializeComponent();
            Unpack_file = CMF_files;
            cmf_motion = motion;
            progressbar_motion.Minimum = 0;
            progressbar_motion.Maximum = Datanum;
            progressbar_motion.Value = 0;
            totalFiles = Datanum;
            filesProcessed = 0;
            UpdateProgressText();
            if (motion == "Read")
            {
                Cancle_btn.IsEnabled = false;
                OK_btn.IsEnabled = false;
                Close_btn.IsEnabled = false;
                Browse_btn.IsEnabled = false;
                PathTextbox.IsEnabled = false;
            }
            else if (motion == "UnpackingSelect")
            {
                foreach (string output_file in CMF_files)
                {
                    string[] data_file = null;
                    data_file = output_file.Split(',');
                    UnpackSelect_file.Add(data_file[0]);

                }
            }
        }

        private void Close_btn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Browse_btn_Click(object sender, RoutedEventArgs e)
        {
            using (WpfFolderBrowserDialogEx folderBrowse = new WpfFolderBrowserDialogEx()
            {
                Title = "Select destination folder"
            })
                if (folderBrowse.ShowDialog(this) == true)
                {
                    PathTextbox.Text = folderBrowse.FileName;
                }
        }

        private void Cancle_btn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private async void OK_btn_Click(object sender, RoutedEventArgs e)
        {
            Cancle_btn.IsEnabled = false;
            OK_btn.IsEnabled = false;
            Close_btn.IsEnabled = false;
            if (string.IsNullOrEmpty(PathTextbox.Text))
            {
                MessageBox.Show("Please input destination path.");
                Cancle_btn.IsEnabled = true;
                OK_btn.IsEnabled = true;
                Close_btn.IsEnabled = true;
            }
            else
            {
                string destFolder = PathTextbox.Text;

                progressbar_motion.Minimum = 0;
                progressbar_motion.Maximum = totalFiles;
                progressbar_motion.Value = 0;
                filesProcessed = 0;

                StartProgressTimer();

                await Task.Run(() =>
                {
                    if (cmf_motion == "Unpacking")
                    {
                        foreach (string output_file in Unpack_file)
                        {
                            try
                            {
                                using (var localArchive = new CMFFile(output_file))
                                {
                                    localArchive.BeginRead();
                                    for (int i = 0; i < localArchive.FileCount; i++)
                                    {
                                        var entry = localArchive.Entries[i];
                                        string fileName = new Classes.File(entry).Name;
                                        string targetPath = Path.Combine(destFolder, fileName);

                                        var dirName = Path.GetDirectoryName(targetPath);
                                        if (!string.IsNullOrEmpty(dirName))
                                        {
                                            Directory.CreateDirectory(dirName);
                                        }

                                        localArchive.ExtractEntry(entry, targetPath);

                                        logQueue.Enqueue(fileName);
                                        System.Threading.Interlocked.Increment(ref filesProcessed);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    MessageBox.Show(this, $"Error extracting archive '{output_file}':\n" + ex.Message, "Error");
                                });
                            }
                        }
                    }
                    else if (cmf_motion == "UnpackingSelect")
                    {
                        CMFFile localArchive = null;
                        string predata_file = "";
                        try
                        {
                            foreach (string output_file in Unpack_file)
                            {
                                string[] data_file = output_file.Split(',');
                                string cmfPath = data_file[0];
                                string entryName = data_file[1];

                                if (cmfPath != predata_file)
                                {
                                    if (localArchive != null)
                                    {
                                        localArchive.Dispose();
                                    }
                                    localArchive = new CMFFile(cmfPath);
                                    localArchive.BeginRead();
                                }
                                predata_file = cmfPath;

                                var entry = localArchive[entryName];
                                if (entry != null)
                                {
                                    string targetPath = Path.Combine(destFolder, entryName);
                                    var dirName = Path.GetDirectoryName(targetPath);
                                    if (!string.IsNullOrEmpty(dirName))
                                    {
                                        Directory.CreateDirectory(dirName);
                                    }

                                    localArchive.ExtractEntry(entry, targetPath);

                                    logQueue.Enqueue(entryName);
                                }

                                System.Threading.Interlocked.Increment(ref filesProcessed);
                            }
                        }
                        catch (Exception ex)
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                MessageBox.Show(this, "Error during selective extraction:\n" + ex.Message, "Error");
                            });
                        }
                        finally
                        {
                            if (localArchive != null)
                            {
                                localArchive.Dispose();
                            }
                        }
                    }
                });

                StopProgressTimer();
                Close_btn.IsEnabled = true;
            }
        }

        public void processbar_processing()
        {
            filesProcessed++;
            progressbar_motion.Value = filesProcessed;
            UpdateProgressText();
        }

        private void StartProgressTimer()
        {
            uiTimer = new DispatcherTimer();
            uiTimer.Interval = TimeSpan.FromMilliseconds(100);
            uiTimer.Tick += UiTimer_Tick;
            uiTimer.Start();
        }

        private void StopProgressTimer()
        {
            if (uiTimer != null)
            {
                uiTimer.Stop();
                uiTimer = null;
            }
            FlushProgress();
        }

        private void UiTimer_Tick(object sender, EventArgs e)
        {
            FlushProgress();
        }

        private void FlushProgress()
        {
            progressbar_motion.Value = filesProcessed;
            UpdateProgressText();

            if (!logQueue.IsEmpty)
            {
                StringBuilder sb = new StringBuilder();
                while (logQueue.TryDequeue(out string logLine))
                {
                    sb.AppendLine(logLine + "......finished");
                }

                if (sb.Length > 0)
                {
                    ShowProcessingText.AppendText(sb.ToString());
                    ShowProcessingText.ScrollToEnd();

                    while (ShowProcessingText.Document.Blocks.Count > 500)
                    {
                        ShowProcessingText.Document.Blocks.Remove(ShowProcessingText.Document.Blocks.FirstBlock);
                    }
                }
            }
        }

        private void UpdateProgressText()
        {
            double percentage = totalFiles > 0 ? ((double)filesProcessed / totalFiles) * 100 : 0;
            int filesLeft = Math.Max(0, totalFiles - filesProcessed);
            ProcessingLabel.Content = $"Processing: {percentage:0}% ({filesProcessed} file done, {filesLeft} file left)";
        }
    }
}