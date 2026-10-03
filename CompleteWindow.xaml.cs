using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using Microsoft.Win32;

namespace CursorCapturer;

public partial class CompleteWindow : Window
{
  private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
  {
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
  };

  private readonly Recording _recording;

  public CompleteWindow(Recording recording)
  {
    InitializeComponent();
    _recording = recording;

    var assembly = Assembly.GetExecutingAssembly();
    Title = $"{assembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product} v{assembly.GetName().Version?.ToString(3) ?? "0.0.0"}";
  }

  private void OnSaveClick(object sender, RoutedEventArgs e)
  {
    SaveFileDialog dialog = new SaveFileDialog
    {
      Filter = "JSON|*.json",
      FileName = "cursordata.json"
    };

    if (dialog.ShowDialog() != true)
    {
      return;
    }

    string json = JsonSerializer.Serialize(_recording, JsonOptions);
    File.WriteAllText(dialog.FileName, json);
  }

  private void OnRecordAgainClick(object sender, RoutedEventArgs e)
  {
    RecordWindow recordWindow = new RecordWindow(_recording.Video);
    recordWindow.Show();
    Close();
  }
}
