using System;
using System.IO;
using Leayal.Closers.CMF;

class Program
{
    static void Main(string[] args)
    {
        try
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "CMF_Test_Output_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            Console.WriteLine($"Temporary output directory: {tempDir}");

            // Test DAT0.CMF (ver 3)
            string dat0Path = @"E:\Closers\DAT\DAT0\DAT0.CMF";
            Console.WriteLine($"\n--- Testing old CMF: {dat0Path} ---");
            using (var archive = CMFArchive.Read(dat0Path))
            {
                Console.WriteLine($"File Count: {archive.FileCount}");
                for (int i = 0; i < Math.Min(3, archive.FileCount); i++)
                {
                    var entry = archive.Entries[i];
                    Console.WriteLine($"Entry {i}: {entry.FileName}");
                    Console.WriteLine($"  Compressed: {entry.IsCompressed}, Encrypted: {entry.IsEncrypted}");
                    Console.WriteLine($"  Unpacked Size: {entry.UnpackedSize}, Compressed Size: {entry.CompressedSize}");
                    
                    string destPath = Path.Combine(tempDir, "DAT0_" + entry.FileName);
                    archive.ExtractEntry(entry, destPath);
                    
                    if (File.Exists(destPath))
                    {
                        var info = new FileInfo(destPath);
                        Console.WriteLine($"  Extracted successfully: {info.Length} bytes");
                        // Read first few bytes to check signature
                        byte[] sample = new byte[8];
                        using (var fs = File.OpenRead(destPath))
                        {
                            fs.Read(sample, 0, sample.Length);
                        }
                        Console.WriteLine($"  Sample bytes: {BitConverter.ToString(sample)}");
                    }
                    else
                    {
                        Console.WriteLine("  Extraction failed: file does not exist.");
                    }
                }
            }

            // Test DAT17.CMF (ver 9)
            string dat17Path = @"E:\Closers\DAT\DAT0\DAT17.CMF";
            Console.WriteLine($"\n--- Testing new CMF: {dat17Path} ---");
            using (var archive = CMFArchive.Read(dat17Path))
            {
                Console.WriteLine($"File Count: {archive.FileCount}");
                for (int i = 0; i < Math.Min(3, archive.FileCount); i++)
                {
                    var entry = archive.Entries[i];
                    Console.WriteLine($"Entry {i}: {entry.FileName}");
                    Console.WriteLine($"  Compressed: {entry.IsCompressed}, Encrypted: {entry.IsEncrypted}");
                    Console.WriteLine($"  Unpacked Size: {entry.UnpackedSize}, Compressed Size: {entry.CompressedSize}");
                    
                    string destPath = Path.Combine(tempDir, "DAT17_" + entry.FileName);
                    archive.ExtractEntry(entry, destPath);
                    
                    if (File.Exists(destPath))
                    {
                        var info = new FileInfo(destPath);
                        Console.WriteLine($"  Extracted successfully: {info.Length} bytes");
                        byte[] sample = new byte[8];
                        using (var fs = File.OpenRead(destPath))
                        {
                            fs.Read(sample, 0, sample.Length);
                        }
                        Console.WriteLine($"  Sample bytes: {BitConverter.ToString(sample)}");
                    }
                    else
                    {
                        Console.WriteLine("  Extraction failed: file does not exist.");
                    }
                }
            }

            // Clean up
            Directory.Delete(tempDir, true);
            Console.WriteLine("\nTests completed successfully and cleaned up.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error occurred: {ex}");
        }
    }
}
