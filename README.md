# CursorCapturer
a Windows app (C# / WPF) that captures cursor movement as keyframes into a JSON, and an AviUtl ExEdit2 script that interpolates between the keyframes.

## Usage
- Open the app and click Import video
- Press F8 to start recording
- Move your cursor until the video ends. You can pause/resume to your liking
- Once the video ends, you can save as JSON (format documented below) or re-record

For the best results, make sure your video's size matches your screen size.

## AviUtl ExEdit2 usage
- Drop `CursorCapturer.anm2` into your AviUtl2 Scripts folder.
- Put [json.lua](https://github.com/rxi/json.lua) in the same directory (the script requires it)
- Import the result JSON into "Data"
- Watch your own movement come alive!

## JSON Format
Example:
```json
{
	"fps": 60,
	"keyframes": [
		{ "frame": 0, "x": 123, "y": 456 },
		{ "frame": 1, "x": 789, "y": 123 },
		{ "frame": 2, "x": 456, "y": 789 }
	]
}
```

| Field                               | Description                                |
| ----------------------------------- | ------------------------------------------ |
| `fps`                               | Video's frame rate                         |
| `keyframes`                         | Captured keyframes                         |
| `keyframes[].frame`                 | Frame number for each keyframe (0-indexed) |
| `keyframes[].x` and `keyframes[].y` | Captured position (aligned top-left)       |

## Building
All you need is just Windows and the .NET 10 SDK. Just run:
`dotnet build`