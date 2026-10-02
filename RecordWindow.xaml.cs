using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using LibVLCSharp.Shared;

namespace CursorCapturer;

public partial class RecordWindow : Window
{
  private const int HotkeyId = 1;

  private const int WmHotkey = 0x0312;

  private const uint VkF8 = 0x77;

  [DllImport("user32.dll")]
  private static extern bool RegisterHotKey(nint window, int id, uint modifiers, uint virtualKey);

  [DllImport("user32.dll")]
  private static extern bool UnregisterHotKey(nint window, int id);

  [DllImport("user32.dll")]
  private static extern bool GetCursorPos(out POINT point);

  [StructLayout(LayoutKind.Sequential)]
  private struct POINT
  {
    public int X;

    public int Y;
  }

  [DllImport("winmm.dll")]
  private static extern uint timeBeginPeriod(uint milliseconds);

  [DllImport("winmm.dll")]
  private static extern uint timeEndPeriod(uint milliseconds);

  private readonly string _videoPath;

  private readonly Recording _recording = new Recording();

  private readonly MediaPlayer _player;

  private readonly Media _media;

  private readonly Stopwatch _clock = Stopwatch.StartNew();

  private nint _windowHandle;

  private bool _playbackStarted;

  private bool _paused;

  private bool _finished;

  private bool _awaitingFirstPlayingEvent = true;

  private Thread? _keyframerThread;

  private volatile bool _keyframerRunning;

  private long _lastVlcTime = -1;

  private double _lastVlcStampMs;

  private double _lastTimeMs;

  public RecordWindow(string videoPath)
  {
    InitializeComponent();

    _videoPath = videoPath;
    _player = new MediaPlayer(App.LibVLC);
    _media = new Media(App.LibVLC, new Uri(videoPath));
  }

  private void OnWindowSourceInitialized(object? sender, EventArgs e)
  {
    _windowHandle = new WindowInteropHelper(this).Handle;
    HwndSource.FromHwnd(_windowHandle)?.AddHook(WindowMessageHook);
    RegisterHotKey(_windowHandle, HotkeyId, 0, VkF8);

    VideoView.MediaPlayer = _player;
  }

  private nint WindowMessageHook(nint window, int message, nint wParam, nint lParam, ref bool handled)
  {
    if (message == WmHotkey && wParam.ToInt32() == HotkeyId)
    {
      StartPlayback();
      handled = true;
    }

    return nint.Zero;
  }

  private async void OnWindowLoadedAsync(object sender, RoutedEventArgs e)
  {
    await _media.Parse(MediaParseOptions.ParseLocal);

    if (!GetVideoFPS(out double fps))
    {
      StatusText.Text = "";
      MessageBox.Show("This file has no video track.", Title, MessageBoxButton.OK, MessageBoxImage.Error);
      return;
    }

    _recording.Fps = fps;
    _recording.Video = _videoPath;

    _player.Playing += OnPlayerPlaying;
    _player.EndReached += OnPlayerEndReached;

    _player.Play(_media);

    StatusText.Text = "Press F8 to start!";
  }

  private bool GetVideoFPS(out double fps)
  {
    foreach (MediaTrack track in _media.Tracks)
    {
      if (track.TrackType == TrackType.Video)
      {
        VideoTrack videoTrack = track.Data.Video;
        fps = (double)videoTrack.FrameRateNum / videoTrack.FrameRateDen;
        return true;
      }
    }

    fps = 0;
    return false;
  }

  private void OnPlayerPlaying(object? sender, EventArgs e)
  {
    if (!_awaitingFirstPlayingEvent)
    {
      return;
    }

    _awaitingFirstPlayingEvent = false;

    Dispatcher.InvokeAsync(PauseOnFirstFrame);
  }

  private void PauseOnFirstFrame()
  {
    _player.SetPause(true);
  }

  private void OnPlayerEndReached(object? sender, EventArgs e)
  {
    Dispatcher.InvokeAsync(Finish);
  }

  private void StartPlayback()
  {
    if (_playbackStarted)
    {
      return;
    }

    _playbackStarted = true;
    _player.Time = 0;
    _player.SetPause(false);

    _lastVlcTime = -1;
    _lastTimeMs = 0;
    _lastVlcStampMs = _clock.Elapsed.TotalMilliseconds;

    StartKeyframer();
    StatusText.Text = "Recording";
    UpdatePlayPauseButton();
  }

  private void OnPlayPauseClick(object sender, RoutedEventArgs e)
  {
    if (!_playbackStarted)
    {
      StartPlayback();
      return;
    }

    _paused = !_paused;
    _player.SetPause(_paused);

    _lastVlcStampMs = _clock.Elapsed.TotalMilliseconds;
    UpdatePlayPauseButton();
  }

  private void UpdatePlayPauseButton()
  {
    PlayPauseButton.Content = _paused ? "Play" : "Pause";
  }

  private void StartKeyframer()
  {
    _keyframerRunning = true;
    _keyframerThread = new Thread(RunKeyframer)
    {
      IsBackground = true,
      Name = "Cursor keyframer"
    };
    _keyframerThread.Start();
  }

  private void RunKeyframer()
  {
    _ = timeBeginPeriod(1);

    try
    {
      while (_keyframerRunning)
      {
        if (!_paused)
        {
          RecordCursorSample();
        }

        Thread.Sleep(1);
      }
    }
    finally
    {
      _ = timeEndPeriod(1);
    }
  }

  private void RecordCursorSample()
  {
    if (GetCursorPos(out POINT point))
    {
      double timeMs = GetCurrentTimeMs();
      Keyframe sample = new Keyframe
      {
        Frame = (int)Math.Round(timeMs / 1000.0 * _recording.Fps),
        X = point.X,
        Y = point.Y
      };

      List<Keyframe> keyframes = _recording.Keyframes;
      if (keyframes.Count > 0 && keyframes[^1].Frame == sample.Frame)
      {
        keyframes[^1] = sample;
      }
      else
      {
        keyframes.Add(sample);
      }
    }
  }

  private double GetCurrentTimeMs()
  {
    long vlcTime = _player.Time;
    double now = _clock.Elapsed.TotalMilliseconds;

    if (vlcTime != _lastVlcTime)
    {
      _lastVlcTime = vlcTime;
      _lastVlcStampMs = now;
    }

    _lastTimeMs = Math.Max(_lastTimeMs, vlcTime + (now - _lastVlcStampMs));
    return _lastTimeMs;
  }

  private void Finish()
  {
    StopKeyframer();

    if (_finished)
    {
      return;
    }

    _finished = true;

    CompleteWindow completeWindow = new CompleteWindow(_recording);
    completeWindow.Show();
    Close();
  }

  private void StopKeyframer()
  {
    _keyframerRunning = false;
    _keyframerThread?.Join();
  }

  private void OnWindowClosed(object? sender, EventArgs e)
  {
    StopKeyframer();
    UnregisterHotKey(_windowHandle, HotkeyId);
    VideoView.MediaPlayer = null;

    ThreadPool.QueueUserWorkItem(_ =>
    {
      _player.Stop();
      _player.Dispose();
      _media.Dispose();
    });
  }
}
