﻿using Avalonia.Data;
using ReactiveUI;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MangaJaNaiConverterGui.ViewModels
{
    [DataContract]
    public class MainWindowViewModel : ViewModelBase
    {
        private static readonly List<string> IMAGE_EXTENSIONS = new() { ".png", ".jpg", ".jpeg", ".webp", ".bmp" };
        private static readonly List<string> ARCHIVE_EXTENSIONS = new() { ".zip", ".cbz", ".rar", ".cbr"};

        public MainWindowViewModel() 
        {
            var g1 = this.WhenAnyValue
            (
                x => x.InputFilePath,
                x => x.OutputFilename,
                x => x.InputFolderPath,
                x => x.OutputFolderPath
            );

            var g2 = this.WhenAnyValue
            (
                x => x.SelectedTabIndex,
                x => x.UpscaleImages,
                x => x.UpscaleArchives,
                x => x.OverwriteExistingFiles,
                x => x.WebpSelected,
                x => x.PngSelected,
                x => x.JpegSelected
            );

            g1.CombineLatest(g2).Subscribe(x =>
            {
                Validate();
            });
        }

        private CancellationTokenSource? _cancellationTokenSource;
        private Process? _runningProcess = null;

        private int _selectedTabIndex;
        [DataMember]
        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set
            {
                if (_selectedTabIndex != value)
                {
                    this.RaiseAndSetIfChanged(ref _selectedTabIndex, value);
                    this.RaisePropertyChanged(nameof(InputStatusText));
                    
                }
            }
        }

        private string _inputFilePath = string.Empty;
        [DataMember]
        public string InputFilePath
        {
            get => _inputFilePath;
            set
            {
                this.RaiseAndSetIfChanged(ref _inputFilePath, value);
                this.RaisePropertyChanged(nameof(InputStatusText));

                if (string.IsNullOrEmpty(value))
                {
                    throw new DataValidationException("Input File is required.");
                }
            }
        }

        private string _inputFolderPath = string.Empty;
        [DataMember]
        public string InputFolderPath
        {
            get => _inputFolderPath;
            set
            {
                this.RaiseAndSetIfChanged(ref _inputFolderPath, value);
                this.RaisePropertyChanged(nameof(InputStatusText));
            }
        }
        /*
        private string _outputFilePath = string.Empty;
        [DataMember]
        public string OutputFilePath
        {
            get => _outputFilePath;
            set => this.RaiseAndSetIfChanged(ref _outputFilePath, value);
        }*/

        private string _outputFilename = "%filename%-mangajanai";
        [DataMember]
        public string OutputFilename
        {
            get => _outputFilename;
            set => this.RaiseAndSetIfChanged(ref _outputFilename, value);
        }

        private string _outputFolderPath = string.Empty;
        [DataMember]
        public string OutputFolderPath
        {
            get => _outputFolderPath;
            set => this.RaiseAndSetIfChanged(ref _outputFolderPath, value);
        }

        private bool _overwriteExistingFiles = false;
        [DataMember]
        public bool OverwriteExistingFiles
        {
            get => _overwriteExistingFiles;
            set => this.RaiseAndSetIfChanged(ref _overwriteExistingFiles, value);
        }

        private bool _upscaleImages = false;
        [DataMember]
        public bool UpscaleImages
        {
            get => _upscaleImages;
            set => this.RaiseAndSetIfChanged(ref _upscaleImages, value);
        }

        private bool _upscaleArchives = true;
        [DataMember]
        public bool UpscaleArchives
        {
            get => _upscaleArchives;
            set => this.RaiseAndSetIfChanged(ref _upscaleArchives, value);
        }

        private bool _autoAdjustLevels = false;
        [DataMember]
        public bool AutoAdjustLevels
        {
            get => _autoAdjustLevels;
            set => this.RaiseAndSetIfChanged(ref _autoAdjustLevels, value);
        }

        private string _grayscaleModelFilePath = string.Empty;
        [DataMember]
        public string GrayscaleModelFilePath
        {
            get => _grayscaleModelFilePath;
            set => this.RaiseAndSetIfChanged(ref _grayscaleModelFilePath, value);
        }

        private string _colorModelFilePath = string.Empty;
        [DataMember]
        public string ColorModelFilePath
        {
            get => _colorModelFilePath;
            set => this.RaiseAndSetIfChanged(ref _colorModelFilePath, value);
        }

        private string _resizeHeightBeforeUpscale = 0.ToString();
        [DataMember]
        public string ResizeHeightBeforeUpscale
        {
            get => _resizeHeightBeforeUpscale;
            set => this.RaiseAndSetIfChanged(ref _resizeHeightBeforeUpscale, value);
        }

        private string _resizeFactorBeforeUpscale = 100.ToString();
        [DataMember]
        public string ResizeFactorBeforeUpscale
        {
            get => _resizeFactorBeforeUpscale;
            set => this.RaiseAndSetIfChanged(ref _resizeFactorBeforeUpscale, value);
        }

        private string _resizeHeightAfterUpscale = 0.ToString();
        [DataMember]
        public string ResizeHeightAfterUpscale
        {
            get => _resizeHeightAfterUpscale;
            set => this.RaiseAndSetIfChanged(ref _resizeHeightAfterUpscale, value);
        }

        private string _resizeFactorAfterUpscale = 100.ToString();
        [DataMember]
        public string ResizeFactorAfterUpscale
        {
            get => _resizeFactorAfterUpscale;
            set => this.RaiseAndSetIfChanged(ref _resizeFactorAfterUpscale, value);
        }

        private bool _webpSelected = true;
        [DataMember]
        public bool WebpSelected
        {
            get => _webpSelected;
            set
            {
                this.RaiseAndSetIfChanged(ref _webpSelected, value);
                this.RaisePropertyChanged(nameof(ShowUseLosslessCompression));
                this.RaisePropertyChanged(nameof(ShowLossyCompressionQuality));
            }
        }

        private bool _pngSelected = false;
        [DataMember]
        public bool PngSelected
        {
            get => _pngSelected;
            set
            {
                this.RaiseAndSetIfChanged(ref _pngSelected, value);
            }
        }

        private bool _jpegSelected = false;
        [DataMember]
        public bool JpegSelected
        {
            get => _jpegSelected;
            set
            {
                this.RaiseAndSetIfChanged(ref _jpegSelected, value);
                this.RaisePropertyChanged(nameof(ShowLossyCompressionQuality));
            }
        }

        private string ImageFormat => WebpSelected ? "webp" : PngSelected ? "png" : "jpg";

        public bool ShowUseLosslessCompression => WebpSelected;

        private bool _useLosslessCompression = false;
        [DataMember]
        public bool UseLosslessCompression
        {
            get => _useLosslessCompression;
            set
            {
                this.RaiseAndSetIfChanged(ref _useLosslessCompression, value);
                this.RaisePropertyChanged(nameof(ShowLossyCompressionQuality));
            }
        }

        public bool ShowLossyCompressionQuality => JpegSelected || (WebpSelected && !UseLosslessCompression);

        private string _lossyCompressionQuality = 80.ToString();
        [DataMember]
        public string LossyCompressionQuality
        {
            get => _lossyCompressionQuality;
            set => this.RaiseAndSetIfChanged(ref _lossyCompressionQuality, value);
        }

        private bool _showLossySettings = true;
        [DataMember]
        public bool ShowLossySettings
        {
            get => _showLossySettings;
            set => this.RaiseAndSetIfChanged(ref _showLossySettings, value);
        }

        private bool _valid = false;
        [IgnoreDataMember]
        public bool Valid
        {
            get => _valid;
            set
            {
                this.RaiseAndSetIfChanged(ref _valid, value);
                this.RaisePropertyChanged(nameof(UpscaleEnabled));
                this.RaisePropertyChanged(nameof(LeftStatus));
            }
        }

        private bool _upscaling = false;
        [IgnoreDataMember]
        public bool Upscaling
        {
            get => _upscaling;
            set
            {
                this.RaiseAndSetIfChanged(ref _upscaling, value);
                this.RaisePropertyChanged(nameof(UpscaleEnabled));
                this.RaisePropertyChanged(nameof(LeftStatus));
            }
        }

        private string _validationText = string.Empty;
        public string ValidationText
        {
            get => _validationText;
            set
            {
                this.RaiseAndSetIfChanged(ref _validationText, value);
                this.RaisePropertyChanged(nameof(LeftStatus));
            }
        }

        public string ConsoleText => string.Join("\n", ConsoleQueue);

        private static readonly int CONSOLE_QUEUE_CAPACITY = 1000;

        private ConcurrentQueue<string> _consoleQueue = new();
        public ConcurrentQueue<string> ConsoleQueue
        {
            get => this._consoleQueue;
            set
            {
                this.RaiseAndSetIfChanged(ref _consoleQueue, value);
                this.RaisePropertyChanged(nameof(ConsoleText));
            }
        }

        private bool _showConsole = false;
        public bool ShowConsole
        {
            get => _showConsole;
            set => this.RaiseAndSetIfChanged(ref _showConsole, value);
        }

        private string _inputStatusText = string.Empty;
        public string InputStatusText
        {
            get => _inputStatusText;
            set
            {
                this.RaiseAndSetIfChanged(ref _inputStatusText, value);
                this.RaisePropertyChanged(nameof(LeftStatus));
            }
        }

        public string LeftStatus => !Valid ? ValidationText.Replace("\n", " ") : $"{InputStatusText} selected for upscaling.";

        private int _progressCurrentFile = 0;
        public int ProgressCurrentFile
        {
            get => _progressCurrentFile;
            set => this.RaiseAndSetIfChanged(ref _progressCurrentFile, value);
        }

        private int _progressTotalFiles = 0;
        public int ProgressTotalFiles
        {
            get => _progressTotalFiles;
            set => this.RaiseAndSetIfChanged(ref _progressTotalFiles, value);
        }

        private int _progressCurrentFileInCurrentArchive = 0;
        public int ProgressCurrentFileInArchive
        {
            get => _progressCurrentFileInCurrentArchive;
            set => this.RaiseAndSetIfChanged(ref _progressCurrentFileInCurrentArchive, value);
        }

        private int _progressTotalFilesInCurrentArchive = 0;
        public int ProgressTotalFilesInCurrentArchive
        {
            get => _progressTotalFilesInCurrentArchive;
            set => this.RaiseAndSetIfChanged(ref _progressTotalFilesInCurrentArchive, value);
        }

        private bool _showArchiveProgressBar = false;
        public bool ShowArchiveProgressBar
        {
            get => _showArchiveProgressBar;
            set => this.RaiseAndSetIfChanged(ref _showArchiveProgressBar, value);
        }

        public bool UpscaleEnabled => Valid && !Upscaling;

        public async Task RunUpscale()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            var ct = _cancellationTokenSource.Token;

            var task = Task.Run(async () =>
            {
                ct.ThrowIfCancellationRequested();
                ConsoleQueueClear();
                Upscaling = true;
                ProgressCurrentFile = 0;
                ProgressCurrentFileInArchive = 0;
                ShowArchiveProgressBar = false;

                var flags = new StringBuilder();
                if (UpscaleArchives)
                {
                    flags.Append("--upscale-archives ");
                }
                if (UpscaleImages)
                {
                    flags.Append("--upscale-images ");
                }
                if (OverwriteExistingFiles)
                {
                    flags.Append("--overwrite-existing-files ");
                }
                if (AutoAdjustLevels)
                {
                    flags.Append("--auto-adjust-levels ");
                }
                if (UseLosslessCompression)
                {
                    flags.Append("--use-lossless-compression ");
                }

                var inputArgs = $"--input-file-path \"{InputFilePath}\" ";

                if (SelectedTabIndex == 1)
                {
                    inputArgs = $"--input-folder-path \"{InputFolderPath}\" ";
                }

                var grayscaleModelFilePath = string.IsNullOrWhiteSpace(GrayscaleModelFilePath) ? GrayscaleModelFilePath : Path.GetFullPath(GrayscaleModelFilePath);
                var colorModelFilePath = string.IsNullOrWhiteSpace(ColorModelFilePath) ? ColorModelFilePath : Path.GetFullPath(ColorModelFilePath);

                var cmd = $@".\python\python.exe "".\backend\src\runmangajanaiconverterguiupscale.py"" {inputArgs} --output-folder-path ""{OutputFolderPath}"" --output-filename ""{OutputFilename}"" --resize-height-before-upscale {ResizeHeightBeforeUpscale} --resize-factor-before-upscale {ResizeFactorBeforeUpscale} --grayscale-model-path ""{grayscaleModelFilePath}"" --color-model-path ""{colorModelFilePath}"" --image-format {ImageFormat} --lossy-compression-quality {LossyCompressionQuality} --resize-height-after-upscale {ResizeHeightAfterUpscale} --resize-factor-after-upscale {ResizeFactorAfterUpscale} {flags}";
                ConsoleQueueEnqueue($"Upscaling with command: {cmd}");
                await RunCommand($@" /C {cmd}");

                Valid = true;
            }, ct);

            try
            {
                await task;
                Validate();
            }
            catch (OperationCanceledException e)
            {
                Console.WriteLine($"{nameof(OperationCanceledException)} thrown with message: {e.Message}");
                Upscaling = false;
            }
            finally
            {
                _cancellationTokenSource.Dispose();
                Upscaling = false;
            }
        }

        public void CancelUpscale()
        {
            try
            {
                _cancellationTokenSource?.Cancel();
                if (_runningProcess != null && !_runningProcess.HasExited)
                {
                    // Kill the process
                    _runningProcess.Kill(true);
                    _runningProcess = null; // Clear the reference to the terminated process
                }
                Validate();
            }
            catch { }
        }

        public void SetWebpSelected()
        {
            WebpSelected = true;
            PngSelected = false;
            JpegSelected = false;
        }

        public void SetPngSelected()
        {
            PngSelected = true;
            WebpSelected = false;
            JpegSelected = false;
        }

        public void SetJpegSelected()
        {
            JpegSelected = true;
            WebpSelected = false;
            PngSelected = false;
        }

        private void CheckInputs()
        {
            if (Valid && !Upscaling)
            {
                var overwriteText = OverwriteExistingFiles ? "overwritten" : "skipped";

                // input file
                if (SelectedTabIndex == 0)
                {
                    StringBuilder status = new();
                    var skipFiles = 0;

                    

                    if (IMAGE_EXTENSIONS.Any(x => InputFilePath.ToLower().EndsWith(x))) 
                    {
                        var outputFilePath = Path.ChangeExtension(
                                                Path.Join(
                                                    Path.GetFullPath(OutputFolderPath), 
                                                    OutputFilename.Replace("%filename%", Path.GetFileNameWithoutExtension(InputFilePath))), 
                                                ImageFormat);
                        if (File.Exists(outputFilePath)) {
                            status.Append($" (1 image already exists and will be {overwriteText})");
                            if (!OverwriteExistingFiles)
                            {
                                skipFiles++;
                            }
                        }
                    }
                    else if (ARCHIVE_EXTENSIONS.Any(x => InputFilePath.ToLower().EndsWith(x)))
                    {
                        var outputFilePath = Path.ChangeExtension(
                                                Path.Join(
                                                    Path.GetFullPath(OutputFolderPath),
                                                    OutputFilename.Replace("%filename%", Path.GetFileNameWithoutExtension(InputFilePath))),
                                                "cbz");
                        if (File.Exists(outputFilePath))
                        {
                            status.Append($" (1 archive already exists and will be {overwriteText})");
                            if (!OverwriteExistingFiles)
                            {
                                skipFiles++;
                            }
                        }
                    }
                    else
                    {
                        // TODO ???
                    }

                    var s = skipFiles > 0 ? "s" : "";
                    if (IMAGE_EXTENSIONS.Any(x => InputFilePath.ToLower().EndsWith(x)))
                    {
                        status.Insert(0, $"{1 - skipFiles} image{s}");
                    }
                    else if (ARCHIVE_EXTENSIONS.Any(x => InputFilePath.ToLower().EndsWith(x)))
                    {
                        status.Insert(0, $"{1 - skipFiles} archive{s}");
                    }
                    else
                    {
                        status.Insert(0, "0 files");
                    }

                    InputStatusText = status.ToString();
                    ProgressCurrentFile = 0;
                    ProgressTotalFiles = 1 - skipFiles;
                    ProgressCurrentFileInArchive = 0;
                    ProgressTotalFilesInCurrentArchive = 0;
                    ShowArchiveProgressBar = false;
                }
                else  // input folder
                {
                    List<string> statuses = new();
                    var existImageCount = 0;
                    var existArchiveCount = 0;
                    var totalFileCount = 0;

                    if (UpscaleImages)
                    {
                        var images = Directory.EnumerateFiles(InputFolderPath, "*.*", SearchOption.AllDirectories)
                            .Where(file => IMAGE_EXTENSIONS.Any(ext => file.ToLower().EndsWith(ext)));
                        var imagesCount = 0;

                        foreach (var inputImagePath in images)
                        {
                            var outputImagePath = Path.ChangeExtension(
                                                    Path.Join(
                                                        Path.GetFullPath(OutputFolderPath),
                                                        OutputFilename.Replace("%filename%", Path.GetFileNameWithoutExtension(inputImagePath))),
                                                    ImageFormat);
                            // if out file exists, exist count ++
                            // if overwrite image OR out file doesn't exist, count image++
                            var fileExists = File.Exists(outputImagePath);

                            if (fileExists)
                            {
                                existImageCount++;
                            }

                            if (!fileExists || OverwriteExistingFiles)
                            {
                                imagesCount++;
                            }
                        }

                        var imageS = imagesCount == 1 ? "" : "s";
                        var existImageS = existImageCount == 1 ? "" : "s";

                        statuses.Add($"{imagesCount} image{imageS} ({existImageCount} image{existImageS} already exist and will be {overwriteText})");
                        totalFileCount += imagesCount;
                    }
                    if (UpscaleArchives)
                    {
                        var archives = Directory.EnumerateFiles(InputFolderPath, "*.*", SearchOption.AllDirectories)
                            .Where(file => ARCHIVE_EXTENSIONS.Any(ext => file.ToLower().EndsWith(ext)));
                        var archivesCount = 0;

                        foreach (var inputArchivePath in archives)
                        {
                            var outputArchivePath = Path.ChangeExtension(
                                                        Path.Join(
                                                            Path.GetFullPath(OutputFolderPath),
                                                            OutputFilename.Replace("%filename%", Path.GetFileNameWithoutExtension(inputArchivePath))),
                                                        "cbz");
                            var fileExists = File.Exists(outputArchivePath); 

                            if (fileExists)
                            {
                                existArchiveCount++;
                            }

                            if (!fileExists || OverwriteExistingFiles)
                            {
                                archivesCount++;
                            }
                        }

                        var archiveS = archivesCount == 1 ? "" : "s";
                        var existArchiveS = existArchiveCount == 1 ? "" : "s";
                        statuses.Add($"{archivesCount} archive{archiveS} ({existArchiveCount} archive{existArchiveS} already exist and will be {overwriteText})");
                        totalFileCount += archivesCount;
                    }

                    if (!UpscaleArchives && !UpscaleImages)
                    {
                        InputStatusText = "0 files";
                    }
                    else
                    {
                        InputStatusText = $"{string.Join(" and ", statuses)}";
                    }

                    ProgressCurrentFile = 0;
                    ProgressTotalFiles = totalFileCount;
                    ProgressCurrentFileInArchive = 0;
                    ProgressTotalFilesInCurrentArchive = 0;
                    ShowArchiveProgressBar = false;

                }
            }
        }

        public void Validate()
        {
            var valid = true;
            var validationText = new List<string>();
            if (SelectedTabIndex == 0)
            {

                if (string.IsNullOrWhiteSpace(InputFilePath))
                {
                    valid = false;
                    validationText.Add("Input File is required.");
                }
                else if (!File.Exists(InputFilePath))
                {
                    valid = false;
                    validationText.Add("Input File does not exist.");
                }

            }
            else
            {
                if (string.IsNullOrWhiteSpace(InputFolderPath))
                {
                    valid = false;
                    validationText.Add("Input Folder is required.");
                }
                else if (!Directory.Exists(InputFolderPath))
                {
                    valid = false;
                    validationText.Add("Input Folder does not exist.");
                }
            }

            if (string.IsNullOrWhiteSpace(OutputFilename))
            {
                valid = false;
                validationText.Add("Output Filename is required.");
            }

            if (string.IsNullOrWhiteSpace(OutputFolderPath))
            {
                valid = false;
                validationText.Add("Output Folder is required.");
            }

            Valid = valid;
            CheckInputs();
            if (ProgressTotalFiles == 0)
            {
                Valid = false;
                validationText.Add($"{InputStatusText} selected for upscaling. At least one file must be selected.");
            }
            ValidationText = string.Join("\n", validationText);
        }

        public async Task RunCommand(string command)
        {
            // Create a new process to run the CMD command
            using (var process = new Process())
            {
                _runningProcess = process;
                process.StartInfo.FileName = "cmd.exe";
                process.StartInfo.Arguments = command;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.WorkingDirectory = Path.GetFullPath(@".\chaiNNer");
                process.StartInfo.StandardOutputEncoding = Encoding.UTF8;
                process.StartInfo.StandardErrorEncoding = Encoding.UTF8;

                // Create a StreamWriter to write the output to a log file
                using (var outputFile = new StreamWriter("error.log", append: true))
                {
                    process.ErrorDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            outputFile.WriteLine(e.Data); // Write the output to the log file
                            ConsoleQueueEnqueue(e.Data);
                        }
                    };

                    process.OutputDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            if (e.Data.StartsWith("PROGRESS="))
                            {
                                if (e.Data.Contains("_zip_image"))
                                {
                                    ShowArchiveProgressBar = true;
                                    ProgressCurrentFileInArchive++;
                                }
                                else
                                {
                                    ProgressCurrentFile++;
                                }
                            }
                            else if (e.Data.StartsWith("TOTALZIP="))
                            {
                                if (int.TryParse(e.Data.Replace("TOTALZIP=", ""), out var total))
                                {
                                    ShowArchiveProgressBar = true;
                                    ProgressCurrentFileInArchive = 0;
                                    ProgressTotalFilesInCurrentArchive = total;
                                }
                            }
                            else
                            {
                                outputFile.WriteLine(e.Data); // Write the output to the log file
                                ConsoleQueueEnqueue(e.Data);
                                Debug.WriteLine(e.Data);
                            }
                        }
                    };

                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine(); // Start asynchronous reading of the output
                    await process.WaitForExitAsync();
                }
                
            }
        }

        private void ConsoleQueueClear()
        {
            ConsoleQueue.Clear();
            this.RaisePropertyChanged(nameof(ConsoleText));
        }

        private void ConsoleQueueEnqueue(string value)
        {
            while (ConsoleQueue.Count > CONSOLE_QUEUE_CAPACITY)
            {
                ConsoleQueue.TryDequeue(out var _);
            }
            ConsoleQueue.Enqueue(value);
            this.RaisePropertyChanged(nameof(ConsoleText));
        }
    }
}