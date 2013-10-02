using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility.Providers;
using Inedo.BuildMaster.Extensibility.Providers.SourceControl;
using Inedo.BuildMaster.Files;
using Inedo.BuildMaster.Web;

namespace Inedo.BuildMasterExtensions.Rational
{
    /// <summary>
    /// Provides functionality for getting files, browsing folders, and applying labels in Rational ClearCase.
    /// </summary>
    [ProviderProperties(
        "Rational ClearCase",
        "Provides functionality for getting files, browsing folders, and applying labels in Rational ClearCase.",
        RequiresTransparentProxy = true)]
    [CustomEditor(typeof(ClearCaseProviderEditor))]
    public sealed class ClearCaseProvider : SourceControlProviderBase, ILabelingProvider
    {
        /// <summary>
        /// Regular expression used to match output of the LS command.
        /// </summary>
        private static readonly Regex LsOutputRegex = new Regex(@"((?:\S+\s)+)\s+(\S+)@@\S*", RegexOptions.Compiled | RegexOptions.Singleline);

        /// <summary>
        /// Initializes a new instance of the <see cref="ClearCaseProvider"/> class.
        /// </summary>
        public ClearCaseProvider()
        {
        }

        /// <summary>
        /// When implemented in a derived class, gets the char that's used by the
        /// provider to separate directories/files in a path string
        /// </summary>
        public override char DirectorySeparator
        {
            get { return '\\'; }
        }
        /// <summary>
        /// Gets or sets the full path to the cleartool.exe file.
        /// </summary>
        [Persistent]
        public string ExePath { get; set; }
        /// <summary>
        /// Gets or sets the directory containing the VOB view to use.
        /// </summary>
        [Persistent]
        public string ViewPath { get; set; }
        /// <summary>
        /// Gets or sets the name of the branch to work against.
        /// </summary>
        /// <remarks>
        /// A null or empty value will work against LATEST.
        /// </remarks>
        [Persistent]
        public string BranchName { get; set; }

        /// <summary>
        /// When implemented in a derived class, retrieves the latest version of
        /// the source code from the provider's sourcePath into the target path.
        /// </summary>
        /// <param name="sourcePath">Provider source path.</param>
        /// <param name="targetPath">Target file path.</param>
        /// <remarks>
        /// Passing a null or empty string to <paramref name="targetPath"/> will cause this
        /// method to only update the current view. No files will be copied.
        /// </remarks>
        public override void GetLatest(string sourcePath, string targetPath)
        {
            GetFiles(sourcePath, targetPath, null);
        }

        public override DirectoryEntryInfo GetDirectoryEntryInfo(string sourcePath)
        {
            // Return list of VOB's as top-level.
            if (string.IsNullOrEmpty(sourcePath))
            {
                var vobEntries = new List<DirectoryEntryInfo>();

                foreach (var vob in GetVobs())
                    vobEntries.Add(new DirectoryEntryInfo(vob, vob, null, null));

                return new DirectoryEntryInfo(string.Empty, string.Empty, vobEntries.ToArray(), null);
            }

            var vobPath = new VobPath(sourcePath);

            // Load the specified VOB.
            SetConfigSpec(null, null, vobPath.Vob);

            return GetDirectoryEntryInfo(vobPath);
        }
        public override byte[] GetFileContents(string filePath)
        {
            if (string.IsNullOrEmpty("filePath"))
                throw new ArgumentNullException("filePath");

            var vobPath = new VobPath(filePath);
            
            // Load the specified VOB.
            SetConfigSpec(null, null, vobPath.Vob);
            var localVobPath = Path.Combine(this.ViewPath, vobPath.Vob);

            ClearToolPath(localVobPath, "update", vobPath.Path);

            var localPath = Path.Combine(localVobPath, vobPath.Path);
            return File.ReadAllBytes(localPath);
        }
        public override bool IsAvailable()
        {
            return true;
        }
        public override void ValidateConnection()
        {
            if (string.IsNullOrEmpty(this.ExePath))
                throw new NotAvailableException("Executable path is required.");
            if (string.IsNullOrEmpty(this.ViewPath))
                throw new NotAvailableException("View path is required.");

            if (!File.Exists(this.ExePath))
                throw new NotAvailableException(string.Format("The file '{0}' either does not exist or BuildMaster does not have permission to access it.", this.ExePath));
            if (!Directory.Exists(this.ViewPath))
                throw new NotAvailableException(string.Format("The view path '{0}' either does not exist or BuildMaster does not have permission to access it.", this.ViewPath));

            try
            {
                ClearTool("hostinfo");
                ClearTool("lsvob", "-short");
            }
            catch (Exception ex)
            {
                throw new NotAvailableException(ex.Message, ex);
            }
        }
        public void ApplyLabel(string label, string sourcePath)
        {
            if (string.IsNullOrEmpty(label))
                throw new ArgumentNullException("label");

            // Make sure the view is up to date.
            GetLatest(sourcePath, null);

            var vobPath = new VobPath(sourcePath);
            var localPath = Path.Combine(this.ViewPath, vobPath.ToString());

            ClearToolPath(localPath, "mklbtype", "-nc", label);
            ClearToolPath(localPath, "mklabel", "-recurse", label, ".");
        }
        public void GetLabeled(string label, string sourcePath, string targetPath)
        {
            GetFiles(sourcePath, targetPath, label);
        }
        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return "Provides functionality for getting files, browsing folders, and applying labels in Rational ClearCase.";
        }

        private void GetFiles(string sourcePath, string targetPath, string label)
        {
            if (!string.IsNullOrEmpty(targetPath))
                Directory.CreateDirectory(targetPath);

            if (string.IsNullOrEmpty(sourcePath))
            {
                // Load all VOB's.
                SetConfigSpec(this.BranchName, label, GetVobs().ToArray());

                ClearToolPath(this.ViewPath, "update", ".");

                if (!string.IsNullOrEmpty(targetPath))
                    Util.Files.CopyFiles(this.ViewPath, targetPath);
                return;
            }

            var vobPath = new VobPath(sourcePath);

            // Load the specified VOB.
            SetConfigSpec(this.BranchName, label, vobPath.Vob);
            var localVobPath = Path.Combine(this.ViewPath, vobPath.Vob);

            ClearToolPath(localVobPath, "update", Util.CoalesceStr(vobPath.Path, "."));

            if (!string.IsNullOrEmpty(targetPath))
            {
                var localPath = Path.Combine(localVobPath, vobPath.Path);
                Util.Files.CopyFiles(localPath, targetPath);
            }
        }
        /// <summary>
        /// Parses a line of text returned by the ClearCase ls command.
        /// </summary>
        /// <param name="line">Line of text to parse.</param>
        /// <param name="vob">Name of the VOB which the command was run against.</param>
        /// <returns>FileEntryInfo or DirectoryEntryInfo instance containing the parsed line.</returns>
        private SystemEntryInfo ParseLine(string line, string vob)
        {
            var match = LsOutputRegex.Match(line);
            if (match == null)
                return null;

            if (match.Groups.Count != 3 || !match.Groups[1].Success || !match.Groups[2].Success)
                return null;

            var vobPath = new VobPath(vob, match.Groups[2].Value);
            var fileName = Path.GetFileName(vobPath.Path);

            if (match.Groups[1].Value.Contains("directory"))
                return new DirectoryEntryInfo(fileName, vobPath.ToString(), null, null);
            else
                return new FileEntryInfo(fileName, vobPath.ToString());
        }
        /// <summary>
        /// Deletes ClearCase .updt log files from the view directory.
        /// </summary>
        private void DeleteLogs()
        {
            if (string.IsNullOrEmpty(this.ViewPath))
                return;

            try
            {
                var logFiles = Directory.GetFiles(this.ViewPath, "*.updt");
                foreach (var logFile in logFiles)
                {
                    try
                    {
                        File.Delete(logFile);
                    }
                    catch
                    {
                    }
                }
            }
            catch
            {
            }
        }
        /// <summary>
        /// Returns a DirectoryEntryInfo from a specified VOB.
        /// </summary>
        /// <param name="vobPath">Name of the VOB.</param>
        /// <returns>DirectoryEntryInfo from the specified VOB.</returns>
        private DirectoryEntryInfo GetDirectoryEntryInfo(VobPath vobPath)
        {
            var localVobPath = Path.Combine(this.ViewPath, vobPath.Vob);

            Directory.CreateDirectory(localVobPath);
            var results = ClearToolPath(localVobPath, "ls", "-long", vobPath.Path);

            var directories = new List<DirectoryEntryInfo>();
            var files = new List<FileEntryInfo>();

            foreach (var line in results)
            {
                var entry = ParseLine(line, vobPath.Vob);
                if (entry.Name != "lost+found")
                {
                    var dir = entry as DirectoryEntryInfo;
                    if (dir != null)
                        directories.Add(dir);

                    var file = entry as FileEntryInfo;
                    if (file != null)
                        files.Add(file);
                }
            }

            var entryPath = vobPath.ToString();

            return new DirectoryEntryInfo(Path.GetFileName(entryPath), entryPath, directories.ToArray(), files.ToArray());
        }
        /// <summary>
        /// Sets the ClearCase configspec for the current view.
        /// </summary>
        /// <param name="branch">The name of the branch to view or null for main.</param>
        /// <param name="label">The label to view or null for latest.</param>
        /// <param name="vobs">The VOB's to load.</param>
        private void SetConfigSpec(string branch, string label, params string[] vobs)
        {
            var configSpecFileName = Path.GetTempFileName();

            try
            {
                using (var configSpecStream = new StreamWriter(configSpecFileName))
                {
                    configSpecStream.WriteLine("element * CHECKEDOUT");
                    configSpecStream.WriteLine("element * /{0}/{1}", Util.CoalesceStr(branch, "main"), Util.CoalesceStr(label, "LATEST"));
                    if (vobs != null)
                    {
                        foreach (var vob in vobs)
                            configSpecStream.WriteLine("load \\{0}", vob);
                    }
                }

                ClearToolPath(this.ViewPath, "setcs", "-force", configSpecFileName);
            }
            finally
            {
                try
                {
                    File.Delete(configSpecFileName);
                }
                catch
                {
                }
            }
        }
        /// <summary>
        /// Returns a list of ClearCase VOB's.
        /// </summary>
        /// <returns>List of ClearCase VOB's.</returns>
        private List<string> GetVobs()
        {
            var vobs = ClearTool("lsvob", "-short");
            for (int i = 0; i < vobs.Count; i++)
                vobs[i] = vobs[i].Trim('\\', '/');

            return vobs;
        }

        /// <summary>
        /// Runs the cleartool.exe command-line program.
        /// </summary>
        /// <param name="command">The command to pass to cleartool.</param>
        /// <param name="args">The arguments to pass to cleartool.</param>
        /// <returns>Output lines produced by cleartool.</returns>
        private List<string> ClearTool(string command, params string[] args)
        {
            return ClearToolPath(null, command, args);
        }
        /// <summary>
        /// Runs the cleartool.exe command-line program in a specified working directory.
        /// </summary>
        /// <param name="workingDirectory">The directory to run cleartool.exe in.</param>
        /// <param name="command">The command to pass to cleartool.</param>
        /// <param name="args">The arguments to pass to cleartool.</param>
        /// <returns>Output lines produced by cleartool.</returns>
        private List<string> ClearToolPath(string workingDirectory, string command, params string[] args)
        {
            try
            {
                var argBuffer = new StringBuilder(command);
                argBuffer.Append(' ');

                foreach (var arg in args)
                    argBuffer.AppendFormat("\"{0}\" ", arg);

                var startInfo = new ProcessStartInfo(this.ExePath, argBuffer.ToString())
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                if (!string.IsNullOrEmpty(workingDirectory))
                    startInfo.WorkingDirectory = workingDirectory;

                var process = new Process()
                {
                    StartInfo = startInfo
                };

                this.LogProcessExecution(startInfo);

                process.Start();

                var lines = new List<string>();
                string line;

                while (!process.HasExited)
                {
                    line = process.StandardOutput.ReadLine();
                    if (line != null)
                    {
                        if (!string.IsNullOrEmpty(line))
                            lines.Add(line);
                    }
                    else
                        Thread.Sleep(5);
                }

                if (process.ExitCode != 0)
                {
                    var errorMessage = string.Join("", lines.ToArray()) + process.StandardError.ReadToEnd().Replace("\r", "").Replace("\n", "");
                    throw new InvalidOperationException(errorMessage);
                }

                while ((line = process.StandardOutput.ReadLine()) != null)
                {
                    if (!string.IsNullOrEmpty(line))
                        lines.Add(line);
                }

                return lines;
            }
            finally
            {
                DeleteLogs();
            }
        }

        #region Private VobPath Class
        /// <summary>
        /// Contains a VOB name and an optional path within it.
        /// </summary>
        private sealed class VobPath
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="VobPath"/> class.
            /// </summary>
            /// <param name="sourcePath">The source path.</param>
            public VobPath(string sourcePath)
            {
                if (string.IsNullOrEmpty(sourcePath))
                {
                    this.Vob = string.Empty;
                    this.Path = string.Empty;
                }
                else
                {
                    // Otherwise grab first element as VOB and the rest as the path.
                    var sourcePathComponents = sourcePath.Split(new[] { '\\', '/' }, 2, StringSplitOptions.RemoveEmptyEntries);

                    this.Vob = sourcePathComponents[0];
                    if (sourcePathComponents.Length > 1)
                        this.Path = sourcePathComponents[1];
                    else
                        this.Path = string.Empty;
                }
            }
            /// <summary>
            /// Initializes a new instance of the <see cref="VobPath"/> class.
            /// </summary>
            /// <param name="vob">The VOB name.</param>
            /// <param name="path">The path inside the VOB.</param>
            public VobPath(string vob, string path)
            {
                this.Vob = vob;
                this.Path = path;
            }

            /// <summary>
            /// Gets the VOB name.
            /// </summary>
            public string Vob { get; private set; }
            /// <summary>
            /// Gets the path inside the VOB.
            /// </summary>
            public string Path { get; private set; }

            /// <summary>
            /// Returns a <see cref="System.String"/> that represents this instance.
            /// </summary>
            /// <returns>
            /// A <see cref="System.String"/> that represents this instance.
            /// </returns>
            public override string ToString()
            {
                if (string.IsNullOrEmpty(this.Path))
                    return this.Vob;
                else
                    return this.Vob + "\\" + this.Path;
            }
        }
        #endregion
    }
}
