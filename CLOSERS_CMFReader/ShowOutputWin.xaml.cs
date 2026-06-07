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

        public ShowOutputWin(string motion, List<string> CMF_files, int Datanum)
        {
            InitializeComponent();
            Unpack_file = CMF_files;
            cmf_motion = motion;
            progressbar_motion.Minimum = 1;
            progressbar_motion.Maximum = Datanum;
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

                                        Application.Current.Dispatcher.Invoke(() =>
                                        {
                                            ShowProcessingText.AppendText(fileName + "......finished" + Environment.NewLine);
                                            ShowProcessingText.ScrollToEnd();
                                        });
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

                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                processbar_processing();
                            });
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

                                    Application.Current.Dispatcher.Invoke(() =>
                                    {
                                        ShowProcessingText.AppendText(entryName + "......finished" + Environment.NewLine);
                                        ShowProcessingText.ScrollToEnd();
                                    });
                                }

                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    processbar_processing();
                                });
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

                Close_btn.IsEnabled = true;
            }
        }

        public void processbar_processing()
        {
            progressbar_motion.Value++;
        }
    }
}