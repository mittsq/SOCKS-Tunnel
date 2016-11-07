﻿using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace SOCKS_Tunnel {
    public partial class Form1 : Form {
        string PlinkPath = Path.Combine(Path.GetTempPath(), "plink.exe");
        byte tries = 0;

        public Form1() {
            InitializeComponent();

            Shown += delegate { (new Thread(CheckForPlinkUpdates)).Start(); };
            button1.Click += Button1_Click;
        }

        private void Button1_Click(object sender, EventArgs e) {
            new Thread(() => {
                string arguments = "-ssh -v -N "
                    + "-l " + textBox4.Text
                    + " -pw " + textBox5.Text
                    + " -P " + numericUpDown1.Value.ToString()
                    + " -D " + numericUpDown2.Value.ToString()
                    + " " + textBox2.Text;
                Process p = new Process();
                p.EnableRaisingEvents = true;
                p.StartInfo.Arguments = arguments;
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.RedirectStandardError = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                p.StartInfo.FileName = PlinkPath;
                p.StartInfo.UseShellExecute = false;
                p.OutputDataReceived += (s, _e) => AddLogMessage(_e.Data);
                p.ErrorDataReceived += (s, _e) => AddLogMessage(_e.Data);
                p.Start();
                p.BeginOutputReadLine();
                p.BeginErrorReadLine();
                p.WaitForExit();
            }).Start();
        }

        public void AddLogMessage(string message) {
            textBox1.Invoke((MethodInvoker)delegate {
                textBox1.AppendText(message + Environment.NewLine);
            });
        }

        public void CheckForPlinkUpdates() {
            AddLogMessage("Checking for binary updates ...");
            WebClient wc = new WebClient();
            if (File.Exists(PlinkPath)) {
                if (tries++ > 3) {
                    AddLogMessage("Attempts to update the binary failed. Will use existing binary." + Environment.NewLine);
                    button1.Invoke((MethodInvoker)delegate { button1.Enabled = true; });
                    return;
                }
                AddLogMessage("Existing binary found.");
                string LocalMD5 = string.Empty;
                AddLogMessage("Generating MD5 checksum of existing binary ...");
                using (var cs = MD5.Create()) {
                    using (var stream = File.OpenRead(PlinkPath)) {
                        LocalMD5 = BitConverter.ToString(cs.ComputeHash(stream)).Replace("-", "‌​").ToLower().ToString().Trim();
                        AddLogMessage("   " + LocalMD5);
                    }
                }
                AddLogMessage("Fetching MD5 checksum of online binary ...");
                string RemoteMD5 = Regex.Match(wc.DownloadString(@"https://the.earth.li/~sgtatham/putty/0.67/md5sums"), @"(.*?)[ ]*x86\/plink\.exe").Groups[1].Value.ToString().Trim();
                AddLogMessage("   " + RemoteMD5);
                if (LocalMD5 == RemoteMD5) {
                    AddLogMessage("No updates found." + Environment.NewLine);
                    button1.Invoke((MethodInvoker)delegate { button1.Enabled = true; });
                    return;
                }
                AddLogMessage("Update available. Downloading ...");
            } else { AddLogMessage("Binary not present. Downloading ..."); }
            wc.DownloadFile(@"https://the.earth.li/~sgtatham/putty/latest/x86/plink.exe", PlinkPath);
            CheckForPlinkUpdates();
        }
    }
}
