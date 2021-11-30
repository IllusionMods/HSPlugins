using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using DirectXTexNet;
using KKImageConverter.Properties;
using ModernWpf;
using SharpGL;
using SharpGL.Enumerations;
using SharpGL.Shaders;
using SharpGL.WPF;
using Image = DirectXTexNet.Image;
using Point = System.Drawing.Point;

namespace KKImageConverter.ViewModels
{
    public class FileData
    {
        public string FullPath { get; set; }
        public string Name { get; set; }
    }

    public class MainWindowViewModel : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        }
        #endregion

        #region Types
        private enum TextureFilterType
        {
            Point,
            Bilinear
        }
        private enum ResizeType
        {
            PowerOfTwo = 0,
            Exact = 1
        }

        private enum ResizePowerOfTwoType
        {
            ToLower = 0,
            ToUpper,
            ToNearest
        }

        private enum OutputFormats
        {
            PNG,
            DDS
        }

        private enum DDSCompressionSpeed
        {
            Slow,
            Fast
        }

        public enum ColorCorrectionWorkflow
        {
            Gamma,
            Linear
        }
        #endregion

        #region Fields
        private OpenGL gl;
        private bool openGLInitialized = false;
        private readonly float[] vertices =
        {
            -1f,  1f, 0f,    0f, 0f,
            -1f, -1f, 0f,    0f, 1f,
             1f, -1f, 0f,    1f, 1f,
             1f,  1f, 0f,    1f, 0f
        };
        private readonly float[] verticesRendering =
        {
            -1f,  1f, 0f,    0f, 1f,
            -1f, -1f, 0f,    0f, 0f,
             1f, -1f, 0f,    1f, 0f,
             1f,  1f, 0f,    1f, 1f
        };
        private uint vbo;
        private uint vao;
        private uint vboRendering;
        private uint vaoRendering;
        private uint currentTextureID = 0;
        private ScratchImage currentScratchImage;
        private readonly ShaderProgram colorCorrectionProgram = new ShaderProgram();
        private readonly ShaderProgram renderProgram = new ShaderProgram();
        #endregion

        #region Properties
        public bool UsingDarkTheme
        {
            get { return Settings.Default.UsingDarkTheme; }
            set
            {
                Settings.Default.UsingDarkTheme = value;
                Settings.Default.Save();
                this.OnPropertyChanged();
                ThemeManager.Current.ApplicationTheme = value ? ApplicationTheme.Dark : ApplicationTheme.Light;
            }
        }
        public bool UsingLightTheme
        {
            get { return !Settings.Default.UsingDarkTheme; }
            set
            {
                Settings.Default.UsingDarkTheme = !value;
                Settings.Default.Save();
                this.OnPropertyChanged();
                ThemeManager.Current.ApplicationTheme = value ? ApplicationTheme.Light : ApplicationTheme.Dark;
            }
        }
        public ObservableCollection<FileData> Files { get; } = new ObservableCollection<FileData>();
        private FileData selectedFile;
        public FileData SelectedFile
        {
            get { return this.selectedFile; }
            set
            {
                this.selectedFile = value;
                this.OnPropertyChanged();
                this.RefreshDisplayedTexture();
            }
        }
        public bool ColorCorrectionEnabled
        {
            get { return Settings.Default.ColorCorrectionEnabled; }
            set
            {
                Settings.Default.ColorCorrectionEnabled = value;
                Settings.Default.Save();
                this.OnPropertyChanged();
            }
        }
        public List<string> ColorCorrectionWorkflowOptions { get { return Enum.GetNames(typeof(ColorCorrectionWorkflow)).ToList(); } }
        public int SelectedColorCorrectionWorkflowIndex
        {
            get { return Settings.Default.SelectedColorCorrectionWorkflowIndex; }
            set
            {
                Settings.Default.SelectedColorCorrectionWorkflowIndex = value;
                Settings.Default.Save();
                this.OnPropertyChanged();
                this.OnPropertyChanged(nameof(this.LinearWorkflow));
                this.OnPropertyChanged(nameof(this.GammaWorkflow));
            }
        }
        public bool LinearWorkflow { get { return (ColorCorrectionWorkflow)this.SelectedColorCorrectionWorkflowIndex == ColorCorrectionWorkflow.Linear; } }
        public bool GammaWorkflow { get { return (ColorCorrectionWorkflow)this.SelectedColorCorrectionWorkflowIndex == ColorCorrectionWorkflow.Gamma; } }
        private float colorCorrectionAmount = 1f;
        public float ColorCorrectionAmount
        {
            get { return this.colorCorrectionAmount; }
            set
            {
                this.colorCorrectionAmount = this.Clamp(value, 0f, 1f);
                this.OnPropertyChanged();
            }
        }
        public bool ResizeEnabled
        {
            get { return Settings.Default.ResizeEnabled; }
            set
            {
                Settings.Default.ResizeEnabled = value;
                Settings.Default.Save();
                this.OnPropertyChanged();
                this.RefreshCurrentSize();
                if (value == false)
                    this.SelectedTextureFilterIndex = (int)TextureFilterType.Point;
            }
        }
        public List<string> TextureFilterOptions { get { return Enum.GetNames(typeof(TextureFilterType)).ToList(); } }
        public int SelectedTextureFilterIndex
        {
            get { return Settings.Default.SelectedTextureFilterIndex; }
            set
            {
                if (Settings.Default.SelectedTextureFilterIndex != value)
                {
                    Settings.Default.SelectedTextureFilterIndex = value;
                    Settings.Default.Save();
                    this.OnPropertyChanged();
                    this.RefreshTextureFilter();
                }
            }
        }
        public List<string> ResizeOptions
        {
            get
            {
                return new List<string>()
                {
                    "To Power of 2",
                    "Exact Dimensions"
                };
            }
        }
        public int SelectedResizeOptionIndex
        {
            get { return Settings.Default.SelectedResizeOptionIndex; }
            set
            {
                Settings.Default.SelectedResizeOptionIndex = value;
                Settings.Default.Save();
                this.OnPropertyChanged();
                this.OnPropertyChanged(nameof(this.ResizeOptionPowerOfTwo));
                this.OnPropertyChanged(nameof(this.ResizeOptionExact));
                this.RefreshCurrentSize();
            }
        }
        public bool ResizeOptionPowerOfTwo { get { return (ResizeType)this.SelectedResizeOptionIndex == ResizeType.PowerOfTwo; } }
        public bool ResizeOptionExact { get { return (ResizeType)this.SelectedResizeOptionIndex == ResizeType.Exact; } }
        public List<string> ResizePowerOfTwoOptions
        {
            get
            {
                return new List<string>()
                {
                    "To Lower",
                    "To Upper",
                    "To Nearest"
                };
            }
        }
        public int SelectedResizePowerOfTwoOptionIndex
        {
            get { return Settings.Default.SelectedResizePowerOfTwoOptionIndex; }
            set
            {
                Settings.Default.SelectedResizePowerOfTwoOptionIndex = value;
                Settings.Default.Save();
                this.OnPropertyChanged();
                this.OnPropertyChanged(nameof(this.ResizePowerOfTwoOptionToNearest));
                this.RefreshCurrentSize();
            }
        }
        public bool ResizePowerOfTwoOptionToNearest { get { return (ResizePowerOfTwoType)this.SelectedResizePowerOfTwoOptionIndex == ResizePowerOfTwoType.ToNearest; } }
        public int ResizeExactWidth
        {
            get { return Settings.Default.ResizeExactWidth; }
            set
            {
                Settings.Default.ResizeExactWidth = value;
                Settings.Default.Save();
                this.OnPropertyChanged();
                this.RefreshCurrentSize();
            }
        }
        public int ResizeExactHeight
        {
            get { return Settings.Default.ResizeExactHeight; }
            set
            {
                Settings.Default.ResizeExactHeight = value;
                Settings.Default.Save();
                this.OnPropertyChanged();
                this.RefreshCurrentSize();
            }
        }
        private Point currentSize;
        public Point CurrentSize
        {
            get { return this.currentSize; }
            set
            {
                this.currentSize = value;
                this.OnPropertyChanged();
                this.RefreshVertices();
            }
        }
        public bool RenameEnabled
        {
            get { return Settings.Default.RenameEnabled; }
            set
            {
                Settings.Default.RenameEnabled = value;
                Settings.Default.Save();
                this.OnPropertyChanged();
                this.RefreshCurrentName();
            }
        }
        public string Prefix
        {
            get { return Settings.Default.Prefix; }
            set
            {
                Settings.Default.Prefix = value;
                Settings.Default.Save();
                this.OnPropertyChanged();
                this.RefreshCurrentName();
            }
        }
        public string Suffix
        {
            get { return Settings.Default.Suffix; }
            set
            {
                Settings.Default.Suffix = value;
                Settings.Default.Save();
                this.OnPropertyChanged();
                this.RefreshCurrentName();
            }
        }
        private string currentName;
        public string CurrentName
        {
            get { return this.currentName; }
            set
            {
                this.currentName = value;
                this.OnPropertyChanged();
            }
        }
        public List<string> OutputFormatsOptions
        {
            get
            {
                return new List<string>()
                {
                    "PNG",
                    "DDS (BC7)"
                };
            }
        }
        public int SelectedOutputFormatIndex
        {
            get { return Settings.Default.SelectedOutputFormatIndex; }
            set
            {
                Settings.Default.SelectedOutputFormatIndex = value;
                Settings.Default.Save();
                this.OnPropertyChanged();
                this.OnPropertyChanged(nameof(this.OutputFormatDDS));
                this.RefreshCurrentName();
            }
        }
        public bool OutputFormatDDS { get { return (OutputFormats)this.SelectedOutputFormatIndex == OutputFormats.DDS; } }
        public string[] DDSCompressionSpeedOptions { get { return Enum.GetNames(typeof(DDSCompressionSpeed)); } }
        public int SelectedDDSCompressionSpeedIndex
        {
            get { return Settings.Default.SelectedDDSCompressionSpeedIndex; }
            set
            {
                Settings.Default.SelectedDDSCompressionSpeedIndex = value;
                Settings.Default.Save();
                this.OnPropertyChanged();
            }
        }
        public string OutputFolder
        {
            get { return Settings.Default.OutputFolder; }
            set
            {
                Settings.Default.OutputFolder = value;
                Settings.Default.Save();
                this.OnPropertyChanged();
            }
        }
        private bool showR = true;
        public bool ShowR
        {
            get { return this.showR; }
            set
            {
                this.showR = value;
                this.OnPropertyChanged();
            }
        }
        private bool showG = true;
        public bool ShowG
        {
            get { return this.showG; }
            set
            {
                this.showG = value;
                this.OnPropertyChanged();
            }
        }
        private bool showB = true;
        public bool ShowB
        {
            get { return this.showB; }
            set
            {
                this.showB = value;
                this.OnPropertyChanged();
            }
        }
        private bool showA = true;
        public bool ShowA
        {
            get { return this.showA; }
            set
            {
                this.showA = value;
                this.OnPropertyChanged();
            }
        }
        private bool showChecker = true;
        public bool ShowChecker
        {
            get { return this.showChecker; }
            set
            {
                this.showChecker = value;
                this.OnPropertyChanged();
            }
        }
        private bool isConverting = false;
        public bool IsConverting
        {
            get { return this.isConverting; }
            set
            {
                this.isConverting = value;
                this.OnPropertyChanged();
                this.OnPropertyChanged(nameof(this.IsNotConverting));
            }
        }
        public bool IsNotConverting
        {
            get { return !this.IsConverting; }
            set
            {
                this.IsConverting = !value;
                this.OnPropertyChanged();
            }
        }
        private float progress = 0f;
        public float Progress
        {
            get { return this.progress; }
            set
            {
                this.progress = value;
                this.OnPropertyChanged();
            }
        }
        #endregion

        #region Commands
        public ICommand AddFilesCommand { get; }
        public ICommand AddFoldersCommand { get; }
        public ICommand DeleteSelectedFileCommand { get; }
        public ICommand RemoveAllCommand { get; }
        public ICommand BrowseOutputCommand { get; }
        public ICommand ConvertCommand { get; }
        public ICommand OpenGLDrawCommand { get; }
        public ICommand OpenGLResizedCommand { get; }
        public ICommand FileDropCommand { get; }
        #endregion

        #region Constructor
        public MainWindowViewModel()
        {
            this.AddFilesCommand = new RelayCommand<object>(this.ExecuteAddFiles);
            this.AddFoldersCommand = new RelayCommand<object>(this.ExecuteAddFolders);
            this.RemoveAllCommand = new RelayCommand<object>(this.ExecuteRemoveAll);
            this.DeleteSelectedFileCommand = new RelayCommand<object>(this.ExecuteDeleteSelectedFile);
            this.BrowseOutputCommand = new RelayCommand<object>(this.ExecuteBrowseOutput);
            this.ConvertCommand = new RelayCommand<object>(this.ExecuteConvert);
            this.OpenGLDrawCommand = new RelayCommand<OpenGLControl>(this.ExecuteOpenGLDraw);
            this.OpenGLResizedCommand = new RelayCommand<OpenGLControl>(this.ExecuteOpenGLResized);
            this.FileDropCommand = new RelayCommand<DragEventArgs>(this.ExecuteFileDrop);
        }
        #endregion

        #region Methods
        private void TryAddFile(string path)
        {
            string extension = Path.GetExtension(path);
            if (extension != null &&
                (extension.Equals(".png", StringComparison.OrdinalIgnoreCase) ||
                 extension.Equals(".gif", StringComparison.OrdinalIgnoreCase) ||
                 extension.Equals(".tiff", StringComparison.OrdinalIgnoreCase) ||
                 extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
                 extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                 extension.Equals(".dds", StringComparison.OrdinalIgnoreCase) ||
                 extension.Equals(".bmp", StringComparison.OrdinalIgnoreCase) ||
                 extension.Equals(".tga", StringComparison.OrdinalIgnoreCase)))
            {
                string fullPath = Path.GetFullPath(path);
                if (this.Files.Any(f => f.FullPath == fullPath) == false)
                    this.Files.Add(new FileData()
                    {
                        FullPath = fullPath,
                        Name = Path.GetFileName(fullPath)
                    });
            }
        }

        private void LoadFile(string path, bool handleError = true)
        {
            if (this.currentTextureID != 0)
            {
                this.gl.DeleteTextures(1, new[] { this.currentTextureID });
                if (this.currentScratchImage != null)
                    this.currentScratchImage.Dispose();
                this.currentScratchImage = null;
                this.currentTextureID = 0;
                this.gl.BindTexture(OpenGL.GL_TEXTURE_2D, 0);
            }

            try
            {
                if (path != null)
                {
                    string extension = Path.GetExtension(path).ToLower();
                    switch (extension)
                    {
                        case ".dds":
                            this.currentScratchImage = TexHelper.Instance.LoadFromDDSFile(path, DDS_FLAGS.NONE).Decompress(DXGI_FORMAT.R8G8B8A8_UNORM);
                            break;
                        case ".tga":
                            this.currentScratchImage = TexHelper.Instance.LoadFromTGAFile(path);
                            break;
                        case ".bmp":
                        case ".png":
                        case ".gif":
                        case ".tiff":
                        case ".jpg":
                        case ".jpeg":
                            this.currentScratchImage = TexHelper.Instance.LoadFromWICFile(path, WIC_FLAGS.FORCE_RGB | WIC_FLAGS.IGNORE_SRGB);
                            break;
                    }

                    TexMetadata metadata = this.currentScratchImage.GetMetadata();
                    if (metadata.Format != DXGI_FORMAT.R8G8B8A8_UNORM)
                    {
                        this.currentScratchImage = this.currentScratchImage.Convert(DXGI_FORMAT.R8G8B8A8_UNORM, TEX_FILTER_FLAGS.DEFAULT, 0f);
                        metadata = this.currentScratchImage.GetMetadata();
                    }

                    Image im = this.currentScratchImage.GetImage(0);

                    uint[] idContainer = new uint[1];
                    this.gl.GenTextures(1, idContainer);
                    this.currentTextureID = idContainer[0];
                    this.gl.BindTexture(OpenGL.GL_TEXTURE_2D, this.currentTextureID);
                    this.gl.TexImage2D(OpenGL.GL_TEXTURE_2D, 0, OpenGL.GL_RGBA, metadata.Width, metadata.Height, 0, OpenGL.GL_RGBA, OpenGL.GL_UNSIGNED_BYTE, im.Pixels);
                    this.gl.Build2DMipmaps(OpenGL.GL_TEXTURE_2D, OpenGL.GL_RGBA, metadata.Width, metadata.Height, OpenGL.GL_RGBA, OpenGL.GL_UNSIGNED_BYTE, im.Pixels);
                    this.gl.TexParameter(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_WRAP_S, OpenGL.GL_CLAMP_TO_EDGE);
                    this.gl.TexParameter(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_WRAP_T, OpenGL.GL_CLAMP_TO_EDGE);
                    this.RefreshTextureFilter();
                }
            }
            catch (Exception e)
            {
                if (this.currentTextureID != 0)
                    this.gl.DeleteTextures(1, new[] { this.currentTextureID });
                if (this.currentScratchImage != null)
                    this.currentScratchImage.Dispose();
                this.currentScratchImage = null;
                this.currentTextureID = 0;
                this.gl.BindTexture(OpenGL.GL_TEXTURE_2D, 0);

                if (handleError)
                    MessageBox.Show("An error happened, please report the following to the developer:\n" + e, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                else
                    throw;
            }
            finally
            {
                this.RefreshCurrentSize();
                this.RefreshCurrentName();
            }
        }

        private void RefreshTextureFilter()
        {
            if (this.currentTextureID != 0)
            {
                this.gl.BindTexture(OpenGL.GL_TEXTURE_2D, this.currentTextureID);
                switch ((TextureFilterType)this.SelectedTextureFilterIndex)
                {
                    case TextureFilterType.Point:
                        this.gl.TexParameter(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_MAG_FILTER, OpenGL.GL_NEAREST);
                        this.gl.TexParameter(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_MIN_FILTER, OpenGL.GL_NEAREST_MIPMAP_LINEAR);
                        break;
                    case TextureFilterType.Bilinear:
                        this.gl.TexParameter(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_MAG_FILTER, OpenGL.GL_LINEAR);
                        this.gl.TexParameter(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_MIN_FILTER, OpenGL.GL_LINEAR_MIPMAP_LINEAR);
                        break;
                }
            }
        }

        private void ReloadCurrentFile()
        {
            this.RefreshDisplayedTexture();
        }

        private void RefreshDisplayedTexture()
        {
            if (this.selectedFile != null)
                this.LoadFile(this.SelectedFile.FullPath);
            else
                this.LoadFile(null);
        }

        private void RefreshCurrentSize()
        {
            if (this.currentScratchImage != null)
            {
                if (this.ResizeEnabled)
                    this.CurrentSize = this.CalculateResized(new Point(this.currentScratchImage.GetMetadata().Width, this.currentScratchImage.GetMetadata().Height));
                else
                    this.CurrentSize = new Point(this.currentScratchImage.GetMetadata().Width, this.currentScratchImage.GetMetadata().Height);
            }
            else
                this.CurrentSize = new Point(0, 0);
            if (this.gl != null)
            {
                this.colorCorrectionProgram.Bind(this.gl);
                this.gl.Uniform2(this.colorCorrectionProgram.GetUniformLocation(this.gl, "texSize"), (float)this.CurrentSize.X, (float)this.CurrentSize.Y);
                this.colorCorrectionProgram.Unbind(this.gl);
            }
        }

        private Point CalculateResized(Point size)
        {
            Point newSize = size;
            if (this.ResizeEnabled)
            {
                switch ((ResizeType)this.SelectedResizeOptionIndex)
                {
                    case ResizeType.PowerOfTwo:
                        switch ((ResizePowerOfTwoType)this.SelectedResizePowerOfTwoOptionIndex)
                        {
                            case ResizePowerOfTwoType.ToLower:
                                if (this.IsPowerOfTwo(newSize.X) == false)
                                    newSize.X = this.Clamp(this.GetPrevPowerOfTwo(newSize.X), 32, 8192);
                                if (this.IsPowerOfTwo(newSize.Y) == false)
                                    newSize.Y = this.Clamp(this.GetPrevPowerOfTwo(newSize.Y), 32, 8192);
                                break;
                            case ResizePowerOfTwoType.ToUpper:
                                if (this.IsPowerOfTwo(newSize.X) == false)
                                    newSize.X = this.Clamp(this.GetNextPowerOfTwo(newSize.X), 32, 8192);
                                if (this.IsPowerOfTwo(newSize.Y) == false)
                                    newSize.Y = this.Clamp(this.GetNextPowerOfTwo(newSize.Y), 32, 8192);
                                break;
                            case ResizePowerOfTwoType.ToNearest:
                                if (this.IsPowerOfTwo(newSize.X) == false)
                                {
                                    int next = this.Clamp(this.GetNextPowerOfTwo(newSize.X), 32, 8192);
                                    int prev = this.Clamp(this.GetPrevPowerOfTwo(newSize.X), 32, 8192);
                                    if (next - newSize.X < newSize.X - prev)
                                        newSize.X = next;
                                    else
                                        newSize.X = prev;
                                }
                                if (this.IsPowerOfTwo(newSize.Y) == false)
                                {
                                    int next = this.Clamp(this.GetNextPowerOfTwo(newSize.Y), 32, 8192);
                                    int prev = this.Clamp(this.GetPrevPowerOfTwo(newSize.Y), 32, 8192);
                                    if (next - newSize.Y < newSize.Y - prev)
                                        newSize.Y = next;
                                    else
                                        newSize.Y = prev;
                                }
                                break;
                        }
                        break;
                    case ResizeType.Exact:
                        newSize.X = this.ResizeExactWidth;
                        newSize.Y = this.ResizeExactHeight;
                        break;
                }
            }
            return newSize;
        }

        private void RefreshCurrentName()
        {
            if (this.selectedFile != null)
            {
                if (this.RenameEnabled)
                    this.CurrentName = this.CalculateRenamed(this.selectedFile.Name);
                else
                    this.CurrentName = "";
            }
            else
                this.CurrentName = "";
        }

        private string CalculateRenamed(string name)
        {
            return $"{this.Prefix}{Path.GetFileNameWithoutExtension(name)}{this.Suffix}.{((OutputFormats)this.SelectedOutputFormatIndex).ToString().ToLower()}";
        }

        private int GetNextPowerOfTwo(int v)
        {
            v--;
            v |= v >> 1;
            v |= v >> 2;
            v |= v >> 4;
            v |= v >> 8;
            v |= v >> 16;
            v++;
            return v;
        }

        private int GetPrevPowerOfTwo(int v)
        {
            return this.GetNextPowerOfTwo(v) >> 1;
        }

        private bool IsPowerOfTwo(int x)
        {
            return (x & (x - 1)) == 0;
        }

        private int Clamp(int v, int min, int max)
        {
            if (v < min)
                return min;
            if (v > max)
                return max;
            return v;
        }

        private float Clamp(float v, float min, float max)
        {
            if (v < min)
                return min;
            if (v > max)
                return max;
            return v;
        }

        private void ExecuteAddFiles(object o)
        {
            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog
            {
                Multiselect = true,
                Filter = "Supported formats|*.png;*.gif;*.tiff;*.jpg;*.jpeg;*.dds;*.tga;*.bmp"
            };
            if (dialog.ShowDialog() == true)
            {
                foreach (string fileName in dialog.FileNames)
                    this.TryAddFile(fileName);
            }
        }

        private void ExecuteAddFolders(object o)
        {
            System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                foreach (string fileName in Directory.GetFiles(dialog.SelectedPath))
                    this.TryAddFile(fileName);
            }
        }

        private void ExecuteFileDrop(DragEventArgs args)
        {
            if (args.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] draggedFiles = (string[])args.Data.GetData(DataFormats.FileDrop);
                foreach (string file in draggedFiles)
                {
                    this.TryAddFile(file);
                }
            }
        }

        private void ExecuteDeleteSelectedFile(object o)
        {
            if (this.selectedFile != null)
                this.Files.Remove(this.selectedFile);
        }
        private void ExecuteRemoveAll(object o)
        {
            this.Files.Clear();
        }

        private void ExecuteBrowseOutput(object o)
        {
            System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                this.OutputFolder = dialog.SelectedPath;
        }

        private async void ExecuteConvert(object o)
        {
            if (Directory.Exists(this.OutputFolder) == false)
                Directory.CreateDirectory(this.OutputFolder);

            this.IsConverting = true;
            StringBuilder finalReport = new StringBuilder();
            finalReport.AppendLine($"Conversion task started at {DateTime.Now}");
            bool error = false;

            for (int i = 0; i < this.Files.Count; i++)
            {
                FileData file = this.Files[i];
                finalReport.AppendLine($"Processing file \"{file.FullPath}\"...");
                try
                {
                    this.LoadFile(file.FullPath, false);
                    await Task.Delay(100);
                    ScratchImage image = this.RenderCurrentToTexture();

                    string name = this.CalculateRenamed(file.Name);
                    switch ((OutputFormats)this.SelectedOutputFormatIndex)
                    {
                        case OutputFormats.PNG:
                            image.SaveToWICFile(0, WIC_FLAGS.NONE, TexHelper.Instance.GetWICCodec(WICCodecs.PNG), Path.Combine(this.OutputFolder, name));
                            break;
                        case OutputFormats.DDS:
                            image = image.GenerateMipMaps(TEX_FILTER_FLAGS.CUBIC, 0);
                            switch ((DDSCompressionSpeed)this.SelectedDDSCompressionSpeedIndex)
                            {
                                case DDSCompressionSpeed.Slow:
                                    image = image.Compress(DXGI_FORMAT.BC7_UNORM, TEX_COMPRESS_FLAGS.PARALLEL, 0f);
                                    break;
                                case DDSCompressionSpeed.Fast:
                                    image = image.Compress(DXGI_FORMAT.BC7_UNORM, TEX_COMPRESS_FLAGS.BC7_QUICK | TEX_COMPRESS_FLAGS.PARALLEL, 0f);
                                    break;
                            }
                            image.SaveToDDSFile(DDS_FLAGS.NONE, Path.Combine(this.OutputFolder, name));
                            break;
                    }
                    this.Progress = (float)i / (this.Files.Count - 1);
                    finalReport.AppendLine("OK");
                }
                catch (Exception e)
                {
                    error = true;
                    finalReport.AppendLine("Error happened:\n" + e);
                }
            }
            if (error)
            {
                File.WriteAllText("report.txt", finalReport.ToString());
                MessageBox.Show("An error happened during conversion, check report.txt for more details.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
                MessageBox.Show("Conversion done, everything went well.", "Done", MessageBoxButton.OK, MessageBoxImage.Information);
            this.Progress = 0f;
            this.ReloadCurrentFile();
            this.IsConverting = false;
        }

        private ScratchImage RenderCurrentToTexture()
        {
            //Create framebuffer
            uint[] b = new uint[1];
            this.gl.GenFramebuffersEXT(1, b);
            uint framebufferId = b[0];
            this.gl.BindFramebufferEXT(OpenGL.GL_FRAMEBUFFER_EXT, framebufferId);

            Point size = this.CalculateResized(new Point(this.currentScratchImage.GetMetadata().Width, this.currentScratchImage.GetMetadata().Height));
            this.gl.GenTextures(1, b);
            uint renderedTextureId = b[0];
            this.gl.BindTexture(OpenGL.GL_TEXTURE_2D, renderedTextureId);
            switch ((TextureFilterType)this.SelectedTextureFilterIndex)
            {
                case TextureFilterType.Point:
                    this.gl.TexParameter(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_MAG_FILTER, OpenGL.GL_NEAREST);
                    this.gl.TexParameter(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_MIN_FILTER, OpenGL.GL_NEAREST_MIPMAP_LINEAR);
                    break;
                case TextureFilterType.Bilinear:
                    this.gl.TexParameter(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_MAG_FILTER, OpenGL.GL_LINEAR);
                    this.gl.TexParameter(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_MIN_FILTER, OpenGL.GL_LINEAR_MIPMAP_LINEAR);
                    break;
            }
            this.gl.TexParameter(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_WRAP_S, OpenGL.GL_CLAMP_TO_EDGE);
            this.gl.TexParameter(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_WRAP_T, OpenGL.GL_CLAMP_TO_EDGE);
            this.gl.TexImage2D(OpenGL.GL_TEXTURE_2D, 0, OpenGL.GL_RGBA, size.X, size.Y, 0, OpenGL.GL_RGBA, OpenGL.GL_UNSIGNED_BYTE, IntPtr.Zero);

            this.gl.FramebufferTexture(OpenGL.GL_FRAMEBUFFER_EXT, OpenGL.GL_COLOR_ATTACHMENT0_EXT, renderedTextureId, 0);
            this.gl.DrawBuffers(1, new[] { OpenGL.GL_COLOR_ATTACHMENT0_EXT });
            if (this.gl.CheckFramebufferStatusEXT(OpenGL.GL_FRAMEBUFFER_EXT) != OpenGL.GL_FRAMEBUFFER_COMPLETE_EXT)
                throw new Exception("Couldn't create OpenGL Framebuffer.");

            //Prepare
            int[] cachedViewport = new int[4];
            this.gl.GetInteger(GetTarget.Viewport, cachedViewport);
            this.gl.Viewport(0, 0, size.X, size.Y);

            //Render to texture
            this.gl.Clear(OpenGL.GL_COLOR_BUFFER_BIT | OpenGL.GL_DEPTH_BUFFER_BIT);
            this.gl.LoadIdentity();

            this.gl.BindFramebufferEXT(OpenGL.GL_FRAMEBUFFER_EXT, framebufferId);
            this.renderProgram.Bind(this.gl);
            this.gl.Uniform1(this.renderProgram.GetUniformLocation(this.gl, "doColorCorrection"), this.ColorCorrectionEnabled ? 1 : 0);
            this.gl.Uniform1(this.renderProgram.GetUniformLocation(this.gl, "convertToLinear"), (ColorCorrectionWorkflow)this.SelectedColorCorrectionWorkflowIndex == ColorCorrectionWorkflow.Linear ? 1 : 0);
            this.gl.Uniform1(this.renderProgram.GetUniformLocation(this.gl, "correctionAmount"), this.ColorCorrectionAmount);
            this.gl.BindTexture(OpenGL.GL_TEXTURE_2D, this.currentTextureID);
            this.gl.BindVertexArray(this.vaoRendering);
            this.gl.DrawArrays(OpenGL.GL_TRIANGLE_FAN, 0, 4);
            this.renderProgram.Unbind(this.gl);

            //Get pixels
            byte[] pixels = new byte[size.X * size.Y * 4];

            this.gl.BindTexture(OpenGL.GL_TEXTURE_2D, renderedTextureId);
            this.gl.ReadPixels(0, 0, size.X, size.Y, OpenGL.GL_RGBA, OpenGL.GL_UNSIGNED_BYTE, pixels);

            //Cleanup
            this.gl.DeleteTextures(1, new[] { renderedTextureId });
            this.gl.BindTexture(OpenGL.GL_TEXTURE_2D, 0);

            this.gl.DeleteFramebuffersEXT(1, new[] { framebufferId });
            this.gl.BindFramebufferEXT(OpenGL.GL_FRAMEBUFFER_EXT, 0);

            this.gl.Viewport(cachedViewport[0], cachedViewport[1], cachedViewport[2], cachedViewport[3]);

            DXGI_FORMAT format = DXGI_FORMAT.R8G8B8A8_UNORM;
            ScratchImage image = TexHelper.Instance.Initialize2D(format, size.X, size.Y, 1, 0, CP_FLAGS.NONE);
            Image im = image.GetImage(0);
            unsafe
            {
                byte* pixelPtr = (byte*)im.Pixels.ToPointer();
                foreach (byte p in pixels)
                {
                    *pixelPtr = p;
                    ++pixelPtr;
                }
            }
            return image;
        }

        private void OpenGLInitialized(OpenGLControl openGl)
        {
            this.gl = openGl.OpenGL;
            if (this.openGLInitialized)
                return;
            this.openGLInitialized = true;

            uint[] buffer = new uint[1];
            {
                this.gl.GenVertexArrays(1, buffer);
                this.vao = buffer[0];
                this.gl.BindVertexArray(this.vao);

                this.gl.GenBuffers(1, buffer);
                this.vbo = buffer[0];
                this.gl.BindBuffer(OpenGL.GL_ARRAY_BUFFER, this.vbo);
                this.gl.BufferData(OpenGL.GL_ARRAY_BUFFER, this.vertices, OpenGL.GL_DYNAMIC_DRAW);
                this.gl.VertexAttribPointer(0, 3, OpenGL.GL_FLOAT, false, 5 * sizeof(float), IntPtr.Zero);
                this.gl.EnableVertexAttribArray(0);

                this.gl.VertexAttribPointer(1, 2, OpenGL.GL_FLOAT, false, 5 * sizeof(float), new IntPtr(3 * sizeof(float)));
                this.gl.EnableVertexAttribArray(1);
            }

            {
                this.gl.GenVertexArrays(1, buffer);
                this.vaoRendering = buffer[0];
                this.gl.BindVertexArray(this.vaoRendering);

                this.gl.GenBuffers(1, buffer);
                this.vboRendering = buffer[0];
                this.gl.BindBuffer(OpenGL.GL_ARRAY_BUFFER, this.vboRendering);
                this.gl.BufferData(OpenGL.GL_ARRAY_BUFFER, this.verticesRendering, OpenGL.GL_STATIC_DRAW);
                this.gl.VertexAttribPointer(0, 3, OpenGL.GL_FLOAT, false, 5 * sizeof(float), IntPtr.Zero);
                this.gl.EnableVertexAttribArray(0);

                this.gl.VertexAttribPointer(1, 2, OpenGL.GL_FLOAT, false, 5 * sizeof(float), new IntPtr(3 * sizeof(float)));
                this.gl.EnableVertexAttribArray(1);
            }

            this.gl.ClearColor(0f, 0f, 0f, 1f);
            this.gl.Enable(OpenGL.GL_TEXTURE_2D);
            this.ExecuteOpenGLResized(openGl);

            this.colorCorrectionProgram.Create(this.gl, Encoding.ASCII.GetString(Resources.ColorCorrectionVert), Encoding.ASCII.GetString(Resources.ColorCorrectionFrag), null);
            this.renderProgram.Create(this.gl, Encoding.ASCII.GetString(Resources.RenderVert), Encoding.ASCII.GetString(Resources.RenderFrag), null);
        }

        private void ExecuteOpenGLDraw(OpenGLControl openGl)
        {
            this.OpenGLInitialized(openGl);
            this.gl.Clear(OpenGL.GL_COLOR_BUFFER_BIT | OpenGL.GL_DEPTH_BUFFER_BIT);
            this.gl.LoadIdentity();

            this.colorCorrectionProgram.Bind(this.gl);
            this.gl.Uniform4(this.colorCorrectionProgram.GetUniformLocation(this.gl, "showChannels"), this.showR ? 1 : 0, this.showG ? 1 : 0, this.showB ? 1 : 0, this.showA ? 1 : 0);
            this.gl.Uniform1(this.colorCorrectionProgram.GetUniformLocation(this.gl, "showChecker"), this.showChecker ? 1 : 0);
            this.gl.Uniform1(this.colorCorrectionProgram.GetUniformLocation(this.gl, "doColorCorrection"), this.ColorCorrectionEnabled ? 1 : 0);
            this.gl.Uniform1(this.colorCorrectionProgram.GetUniformLocation(this.gl, "convertToLinear"), (ColorCorrectionWorkflow)this.SelectedColorCorrectionWorkflowIndex == ColorCorrectionWorkflow.Linear ? 1 : 0);
            this.gl.Uniform1(this.colorCorrectionProgram.GetUniformLocation(this.gl, "correctionAmount"), this.ColorCorrectionAmount);
            this.gl.BindTexture(OpenGL.GL_TEXTURE_2D, this.currentTextureID);
            this.gl.BindVertexArray(this.vao);
            this.gl.DrawArrays(OpenGL.GL_TRIANGLE_FAN, 0, 4);
            this.colorCorrectionProgram.Unbind(this.gl);

#if DEBUG
            this.CheckErrors();
#endif
        }

        private void ExecuteOpenGLResized(OpenGLControl openGl)
        {
            this.OpenGLInitialized(openGl);
            this.gl.MatrixMode(MatrixMode.Projection);
            this.gl.LoadIdentity();
            this.gl.Ortho2D(0, openGl.OpenGL.RenderContextProvider.Width, openGl.OpenGL.RenderContextProvider.Height, 0);
            this.RefreshVertices();
        }

        private void CheckErrors()
        {
            uint err;
            while ((err = this.gl.GetError()) != OpenGL.GL_NO_ERROR)
                Console.WriteLine(this.gl.GetErrorDescription(err));
        }

        //Todo make that shit better
        private void RefreshVertices()
        {
            if (this.gl == null)
                return;
            double widthPercentage = 1f;
            double heightPercentage = 1f;
            if (this.currentScratchImage != null)
            {
                double maxDimension = Math.Max(this.currentSize.X, this.currentSize.Y);
                widthPercentage = this.currentSize.X / maxDimension;
                heightPercentage = this.currentSize.Y / maxDimension;
            }

            double min = Math.Min(this.gl.RenderContextProvider.Width, this.gl.RenderContextProvider.Height);
            widthPercentage = min * widthPercentage / this.gl.RenderContextProvider.Width;
            heightPercentage = min * heightPercentage / this.gl.RenderContextProvider.Height;
            double max = Math.Max(widthPercentage, heightPercentage);
            widthPercentage /= max;
            heightPercentage /= max;

            this.vertices[0] = (float)-widthPercentage;
            this.vertices[1] = (float)heightPercentage;

            this.vertices[5] = (float)-widthPercentage;
            this.vertices[6] = (float)-heightPercentage;

            this.vertices[10] = (float)widthPercentage;
            this.vertices[11] = (float)-heightPercentage;

            this.vertices[15] = (float)widthPercentage;
            this.vertices[16] = (float)heightPercentage;

            this.gl.BindBuffer(OpenGL.GL_ARRAY_BUFFER, this.vbo);
            this.gl.BufferData(OpenGL.GL_ARRAY_BUFFER, this.vertices, OpenGL.GL_DYNAMIC_DRAW);
        }

        #endregion
    }
}
