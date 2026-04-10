# WavConvert4Amiga

A Windows utility designed to convert WAV/MP3 files for optimal use with Amiga computers and music trackers. Features an intuitive interface for sample rate conversion, waveform editing, and ProTracker note tuning.

![WavConvert4Amiga GUI v1.2](wc4a-gui-1.2.jpeg?raw=true "Title")
## Features

### Audio Processing
- Convert WAV files to 8-bit mono format at any sample rate
- Load MP3 files and decode them into the same editable workflow as WAV files
- Support for both PAL and NTSC frequencies
- ProTracker note frequency conversion (C-1 to B-3)
- Chipify Mono effect (single-note chip-style resynthesis with envelope following)
- Chipify Deluxe effect (frame pitch-tracking with chip wave selection and resynthesis)
- Built-in low-pass filter option
- Adjustable amplification control
- 8SVX file format support
- 8SVX loop points support

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
- Drag and drop WAV/MP3 file support
- Auto-convert option for batch processing
- Original file preservation option
- Save as WAV or 8SVX format
- Save loop selections separately

## Usage

### Basic Operation
1. Drag and drop a WAV or MP3 file onto the interface
2. Select desired sample rate or ProTracker note
3. Adjust amplification if needed
4. Enable low-pass filter if desired
5. Click "Convert Current" or enable "Auto Convert"

### Setting Loop Points
1. Click in the waveform to set start point
2. Click again to set end point
3. Loop will preview automatically
4. Save loop as using either "Save Loop" or "Save Loop Points (8SVX)"

### Recording Audio
1. Choose "Record System" or "Record Mic"
2. Click "Stop Recording" when finished
3. Process the recorded audio as needed

### AudioFX
1. Pick any AudioFX one sample is loaded or recorded.
2. Click Convert Current or set loop points to save sample/section with effects

### Future AudioFX Ideas (for next release)
- **Bitcrusher / downsample:** Lo-fi grit control with variable sample-rate reduction and quantization depth.
- **Distortion / overdrive:** Soft-clip and hard-clip options for drums, basses, and aggressive leads.
- **Chorus / ensemble:** Slight delayed detuned copies to thicken single-cycle and sustained samples.
- **Flanger / phaser:** Swept comb/notch movement for classic retro movement sounds.
- **Tremolo / auto-pan (mono tremolo mode):** Rhythmic amplitude modulation for pulse-like textures.
- **Compressor / limiter:** Simple dynamics control to make converted 8-bit samples punchier and more consistent.
- **Noise gate / denoise:** Remove low-level hiss or room noise from recordings before conversion.
- **Transient shaper:** Emphasize attack on drums/percussion so they cut through in tracker mixes.
- **Band-pass “telephone” EQ:** Useful for voice and FX coloration with minimal CPU-heavy processing.
- **Reverse + fade-in/out helpers:** Quick one-click sample design tools for transitions and impacts.
- **ADSR-style volume envelope apply:** Bake attack/decay/release directly into one-shot samples.
- **Stereo-to-mono blend modes:** Mid/side-inspired collapse choices to reduce phase cancellation when importing stereo material.


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
### v1.4
- Workflow bugs should now be resolved.

### v1.3
- Amiga ST-xx Sample format added
- You can now load 8svx and original Amiga St-xx sample formats directly in to wavconvert.

### v1.2
- Added AudioFX Engine
- Fixed More Bugs
- Icon for program added

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
