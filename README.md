# WavConvert4Amiga

A Windows utility designed to convert WAV files for optimal use with Amiga computers and music trackers. Features an intuitive interface for sample rate conversion, waveform editing, and ProTracker note tuning.

## Features

### Audio Processing
- Convert WAV files to 8-bit mono format at any sample rate
- Support for both PAL and NTSC frequencies
- ProTracker note frequency conversion (C-1 to B-3)
- Built-in low-pass filter option
- Adjustable amplification control
- 8SVX file format support

### Waveform Editor
- Visual waveform display with zoom controls
- Set and adjust loop points
- Cut unwanted sections
- Preview audio with loop points
- Undo/Redo support

### Recording Features
- Record system audio directly
- Record from microphone
- Real-time sample rate conversion
- Visual feedback during recording

### File Handling
- Drag and drop WAV file support
- Auto-convert option for batch processing
- Original file preservation option
- Save as WAV or 8SVX format
- Save loop selections separately

## Usage

### Basic Operation
1. Drag and drop a WAV file onto the interface
2. Select desired sample rate or ProTracker note
3. Adjust amplification if needed
4. Enable low-pass filter if desired
5. Click "Convert Current" or enable "Auto Convert"

### Setting Loop Points
1. Click in the waveform to set start point
2. Click again to set end point
3. Use "Preview" to test the loop
4. Save loop using either "Save Loop" or "Save Loop Points (8SVX)"

### Recording Audio
1. Choose "Record System" or "Record Mic"
2. Set desired sample rate before recording
3. Click "Stop Recording" when finished
4. Process the recorded audio as needed

### Keyboard Shortcuts
- `Ctrl+Z`: Undo
- `Ctrl+Y`: Redo
- `Ctrl+X`: Cut
- `Ctrl+Space`: Preview/Stop Preview

## System Requirements
- Windows operating system
- .NET Framework 4.7.2
- DirectX/Windows Media Foundation

## Building
The project can be built using Visual Studio 2019 or later. Required NuGet packages:
```
- NAudio (and related packages)
- CSCore
- Microsoft.Win32.Registry
```

## License
MIT License

## Acknowledgments
- NAudio library for audio processing
- Amiga Developer Community for format specifications
- ProTracker documentation for frequency tables

## Version History
### v1.1
- Fixed Bugs
- UI Changes
- Play back head shown in green
  
### v1.0
- Initial release
- Complete waveform editing functionality
- ProTracker note conversion support
- Recording capabilities
- 8SVX file format support

## Contributing
Feel free to submit issues and pull requests.
