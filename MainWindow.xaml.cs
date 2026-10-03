using System.Reflection;
using System.Windows;
using Microsoft.Win32;

namespace CursorCapturer;

public partial class MainWindow : Window
{
  private const string VideoFileFilter = "Video files|*.mp4;*.mkv;*.avi;*.mov;*.webm|All files|*.*";

  public MainWindow()
  {
    InitializeComponent();

    var assembly = Assembly.GetExecutingAssembly();
    Title = $"{assembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product} v{assembly.GetName().Version?.ToString(3) ?? "0.0.0"}";
  }

  private void OnImportClick(object sender, RoutedEventArgs e)
  {
    OpenFileDialog dialog = new OpenFileDialog
    {
      Filter = VideoFileFilter
    };

    if (dialog.ShowDialog() != true)
    {
      return;
    }

    RecordWindow recordWindow = new RecordWindow(dialog.FileName);
    recordWindow.Show();
    Close();
  }
}
