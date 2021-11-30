using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using KKImageConverter.Properties;
using ModernWpf;

namespace KKImageConverter
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void App_OnStartup(object sender, StartupEventArgs e)
        {
            if (string.IsNullOrEmpty(Settings.Default.OutputFolder))
            {
                Settings.Default.OutputFolder = Path.Combine(Directory.GetCurrentDirectory(), "Output").Replace('/', '\\');
                Settings.Default.Save();
            }
            ThemeManager.Current.ApplicationTheme = Settings.Default.UsingDarkTheme ? ApplicationTheme.Dark : ApplicationTheme.Light;
        }
    }
}
