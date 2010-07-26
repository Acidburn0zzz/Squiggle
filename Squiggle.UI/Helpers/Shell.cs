﻿using System;
using System.Diagnostics;
using System.IO;

namespace Squiggle.UI.Helpers
{
    class Shell
    {
        public static bool CreateDirectoryIfNotExists(string path)
        {
            bool success = true;
            try
            {
                if (!Directory.Exists(path))
                {
                    DirectoryInfo dirInfo = Directory.GetParent(path);
                    if (dirInfo != null)
                        success = CreateDirectoryIfNotExists(dirInfo.FullName);
                    if (success)
                        Directory.CreateDirectory(path);
                }
            }
            catch (Exception ex)
            {
                success = false;
                Trace.WriteLine(ex.Message);
            }
            return success;
        }

        public static void ShowInFolder(string filePath)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = "explorer.exe";
            startInfo.Arguments = "/select,\"" + filePath + "\"";

            Process.Start(startInfo);
        }

        public static void OpenFile(string filePath)
        {
            if (File.Exists(filePath))
                Process.Start(new ProcessStartInfo(filePath));
        }

        public static void OpenUrl(string url)
        {
            try
            {
                Process.Start(url);
            }
            catch { }
        }

        public static string GetUniqueFilePath(string folderPath, string originalFileName)
        {
            string extension = System.IO.Path.GetExtension(originalFileName);
            string fileName = System.IO.Path.GetFileNameWithoutExtension(originalFileName);

            string filePath = System.IO.Path.Combine(folderPath, originalFileName);
            for (int i = 1; File.Exists(filePath); i++)
            {
                string temp = String.Format("{0}({1}){2}", fileName, i, extension);
                filePath = System.IO.Path.Combine(folderPath, temp);
            }
            return filePath;
        }
    }
}
