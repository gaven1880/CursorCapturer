using System.Windows;
using LibVLCSharp.Shared;

namespace CursorCapturer;

public partial class App : Application
{
  public static LibVLC LibVLC { get; private set; } = null!;

  protected override void OnStartup(StartupEventArgs e)
  {
    Core.Initialize();
    LibVLC = new LibVLC();
    base.OnStartup(e);
  }
}
