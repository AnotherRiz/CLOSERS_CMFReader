using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using Leayal.Closers.CMF;
using CLOSERS_CMFReader.Classes;
using System.IO;

namespace CLOSERS_CMFReader
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public CMFFile archive;
        public List<string> CMF_files = new List<string>();
        public string Unpacking_file;
        public List<UnpackList> items = new List<UnpackList>();
        public List<string> SelectCMF_file = new List<string>();
        private string motion;

        public MainWindow()
        {
            InitializeComponent();
            UnpackCMF_btn.IsEnabled = false;
            UnpackCMFRecord_btn.IsEnabled = false;
            CMFListView.ItemsSource = items;
        }

        private void UnpackCMF_btn_Click(object sender, RoutedEventArgs e)
        {
            archive = null;
            if (CMFListView.SelectedItems.Count > 0)
            {
                motion = "UnpackingSelect";
                for (int i = 0; i < CMFListView.SelectedItems.Count; i++)
                {
                    UnpackList SelectCMF = (UnpackList)CMFListView.SelectedItems[i];
                    SelectCMF_file.Add(SelectCMF.File + "," + SelectCMF.Name);
                }
                ShowOutputWin OutputWindows = new ShowOutputWin(motion, SelectCMF_file, SelectCMF_file.Count);
                OutputWindows.Show();
            }
            else
            {
                motion = "Unpacking";
                ShowOutputWin OutputWindows = new ShowOutputWin(motion, CMF_files, items.Count);
                OutputWindows.Show();
            }
        }

        private void PackFile_btn_Click(object sender, RoutedEventArgs e)
        {
            //ShowOutputWin OutputWindows = new ShowOutputWin();
            //OutputWindows.Show();
        }

        private async void ReadCMF_btn_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ReadCMF = new OpenFileDialog
            {
                Title = "Select a file to open",
                RestoreDirectory = true,
                DefaultExt = "cmf",
                CheckPathExists = true,
                CheckFileExists = true,
                Filter = "Closers CMF|*.cmf",
                Multiselect = true
            };

            if (ReadCMF.ShowDialog() == true)
            {
                await LoadCMFFilesAsync(ReadCMF.FileNames);
            }
        }

        private async void ReadFolder_btn_Click(object sender, RoutedEventArgs e)
        {
            using (var folderBrowse = new WinForms.WpfFolderBrowserDialogEx { Title = "Select folder to scan CMF files" })
            {
                if (folderBrowse.ShowDialog(this) == true)
                {
                    string selectedPath = folderBrowse.FileName;
                    if (Directory.Exists(selectedPath))
                    {
                        var cmfFiles = await Task.Run(() => 
                            Directory.GetFiles(selectedPath, "*.cmf", SearchOption.AllDirectories)
                        );

                        if (cmfFiles.Length == 0)
                        {
                            MessageBox.Show(this, "No .cmf files found in the selected folder.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                            return;
                        }

                        await LoadCMFFilesAsync(cmfFiles);
                    }
                }
            }
        }

        private async Task LoadCMFFilesAsync(IEnumerable<string> filePaths)
        {
            ReadCMF_btn.IsEnabled = false;
            ReadFolder_btn.IsEnabled = false;
            UnpackCMF_btn.IsEnabled = true;
            UnpackCMFRecord_btn.IsEnabled = true;

            var filesList = filePaths.ToList();
            if (filesList.Count == 0)
            {
                ReadCMF_btn.IsEnabled = true;
                ReadFolder_btn.IsEnabled = true;
                return;
            }

            ShowOutputWin OutputWindows = new ShowOutputWin("Read", CMF_files, filesList.Count);
            OutputWindows.Show();

            var newItems = await Task.Run(() =>
            {
                var tempItems = new List<UnpackList>();
                foreach (string strFilename in filesList)
                {
                    try
                    {
                        using (var tempArchive = new CMFFile(strFilename))
                        {
                            tempArchive.BeginRead();
                            for (int i = 0; i < tempArchive.FileCount; i++)
                            {
                                var entry = tempArchive.Entries[i];
                                var fileHelper = new Classes.File(entry);
                                tempItems.Add(new UnpackList()
                                {
                                    File = strFilename,
                                    Name = fileHelper.Name,
                                    Size = fileHelper.SizeInString,
                                    Type = fileHelper.Type,
                                    RawSize = fileHelper.Size ?? 0
                                });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            MessageBox.Show(this, $"Error while opening '{strFilename}'\n" + ex.ToString(), "Error");
                        });
                    }

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        OutputWindows.processbar_processing();
                    });
                }
                return tempItems;
            });

            CMF_files.AddRange(filesList);
            items.AddRange(newItems);
            CMFList_Update();
            UpdateTotalSize();

            OutputWindows.Close();
            ReadCMF_btn.IsEnabled = true;
            ReadFolder_btn.IsEnabled = true;
        }

        private void UpdateTotalSize()
        {
            long totalBytes = 0;
            foreach (var item in items)
            {
                totalBytes += item.RawSize;
            }

            var byteSizeHelper = new ByteSize(totalBytes);
            TotalSizeLabel.Content = $"Total Extracted Size: {byteSizeHelper.ToString()}";
        }

        void CMFList_Update()
        {
            CMFListView.ItemsSource = null;
            CMFListView.ItemsSource = items;
        }

        private void UnpackClear_btn_Click(object sender, RoutedEventArgs e)
        {
            archive = null;
            CMF_files.Clear();
            items.Clear();
            ReadCMF_btn.IsEnabled = true;
            ReadFolder_btn.IsEnabled = true;
            UnpackCMF_btn.IsEnabled = false;
            UnpackCMFRecord_btn.IsEnabled = false;
            CMFListView.ItemsSource = null;
            Unpacking_file = "";
            UpdateTotalSize();
        }

        private void UnpackCMFRecord_btn_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveRecordDialog = new SaveFileDialog();
            saveRecordDialog.Filter = "Text files(*.txt)|*.txt|All files(*.*)|*.*";
            if (saveRecordDialog.ShowDialog() == true)
            {
                string name = saveRecordDialog.FileName;

                TextWriter sw = new StreamWriter(saveRecordDialog.FileName.ToString());

                for (int i = 0; i < items.Count; i++)
                {
                    sw.Write(items[i].File + " | " + items[i].Name);
                    sw.WriteLine("");
                }
                sw.Close();
            }
        }
    }
}