using System;
using System.IO;
using System.Text;
using Leayal.Closers.CMF;

class Program
{
    static void Main(string[] args)
    {
        try
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "CMF_Script_Test_Output_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            Console.WriteLine($"Temporary output directory: {tempDir}");

            // Test DAT115088.CMF (SCRIPT)
            string scriptCmfPath = @"E:\Closers\DAT\SCRIPT\DAT115088.CMF";
            Console.WriteLine($"\n--- Testing Script CMF: {scriptCmfPath} ---");
            using (var archive = CMFArchive.Read(scriptCmfPath))
            {
                Console.WriteLine($"File Count: {archive.FileCount}");
                for (int i = 0; i < Math.Min(2, archive.FileCount); i++)
                {
                    var entry = archive.Entries[i];
                    Console.WriteLine($"Entry {i}: {entry.FileName}");
                    Console.WriteLine($"  Compressed: {entry.IsCompressed}, Encrypted: {entry.IsEncrypted}");
                    Console.WriteLine($"  Unpacked Size: {entry.UnpackedSize}, Compressed Size: {entry.CompressedSize}");
                    
                    string destPath = Path.Combine(tempDir, entry.FileName);
                    archive.ExtractEntry(entry, destPath);
                    
                    if (File.Exists(destPath))
                    {
                        var info = new FileInfo(destPath);
                        Console.WriteLine($"  Extracted successfully: {info.Length} bytes");
                        // Read first few bytes to check if it's the raw base64 string
                        byte[] sample = new byte[32];
                        using (var fs = File.OpenRead(destPath))
                        {
                            fs.Read(sample, 0, sample.Length);
                        }
                        string sampleStr = Encoding.ASCII.GetString(sample);
                        Console.WriteLine($"  Sample string: {sampleStr}");
                    }
                    else
                    {
                        Console.WriteLine("  Extraction failed: file does not exist.");
                    }
                }
            }

            // Clean up
            Directory.Delete(tempDir, true);
            Console.WriteLine("\nScript extraction tests completed successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error occurred: {ex}");
        }
    }
}
