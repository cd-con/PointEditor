﻿using System.IO.Compression;
using System.Net;

const string DOWNLOAD_URL = "https://github.com/cd-con/PointEditor/releases/latest/download/PointEditor.zip";
const string DOWNLOAD_TARGET = "Update.zip";
string EXTRACT_TARGET = Directory.GetCurrentDirectory();


Console.WriteLine("Killing process... (waiting 3s)");
Thread.Sleep(3000);
Console.WriteLine("Downloading update...");

using (var client = new WebClient())
    client.DownloadFile(DOWNLOAD_URL, DOWNLOAD_TARGET);

Console.WriteLine("Updating...");
ZipFile.ExtractToDirectory(DOWNLOAD_TARGET, EXTRACT_TARGET, true);
File.Delete(DOWNLOAD_TARGET);
Console.WriteLine("Finished!");
