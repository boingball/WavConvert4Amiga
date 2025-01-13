using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace WavConvert4Amiga
{
    public partial class WaveformViewer : UserControl
    {
        private byte[] audioData;
        private int loopStart = -1;
        private int loopEnd = -1;
        private float zoomFactor = 1.0f;
        private float currentZoom = 1.0f;
        private int scrollOffset = 0;
        private bool isDraggingLoop = false;
        private bool isDraggingStart = false;
        private Color waveformColor = Color.DodgerBlue;
        private Color backgroundColor = Color.Black;
        private Color loopMarkerColor = Color.Red;
        private Color playheadColor = Color.FromArgb(144, 238, 144); // Light green color
        private int currentPlayPosition = -1; // Current playback position
        private Timer playheadTimer;
        private int scrollPosition = 0;
        private int maxScroll = 0;

        public event EventHandler<(int start, int end)> LoopPointsChanged;
        private bool isDraggingEnd = false;
        private const int DRAG_THRESHOLD = 5;
        private const float ZOOM_STEP = 1.5f;
        private const float MAX_ZOOM = 10.0f;
        private const float MIN_ZOOM = 1.0f;

        public WaveformViewer()
        {
            InitializeComponent();
            this.DoubleBuffered = true;
            this.BackColor = backgroundColor;
            this.MinimumSize = new Size(200, 100);

            this.MouseDown += WaveformViewer_MouseDown;
            this.MouseMove += WaveformViewer_MouseMove;
            this.MouseUp += WaveformViewer_MouseUp;

            // Initialize playhead timer
            playheadTimer = new Timer();
            playheadTimer.Interval = 16; // ~60 FPS update rate
            playheadTimer.Tick += PlayheadTimer_Tick;
        }

        // Add method to update playhead position
        public void UpdatePlayPosition(int samplePosition)
        {
            if (currentPlayPosition != samplePosition)
            {
                currentPlayPosition = samplePosition;
                this.Invalidate(); // Trigger redraw
            }
        }

        // Start playhead animation
        public void StartPlayheadAnimation()
        {
            playheadTimer.Start();
        }

        // Stop playhead animation
        public void StopPlayheadAnimation()
        {
            playheadTimer.Stop();
            currentPlayPosition = -1;
            this.Invalidate();
        }

        private DateTime playbackStartTime;
        private int sampleRate = 8000; // Default sample rate

        public void SetSampleRate(int rate)
        {
            sampleRate = rate;
        }

        private void PlayheadTimer_Tick(object sender, EventArgs e)
        {
            if (currentPlayPosition >= 0)
            {
                // Calculate elapsed time since playback started
                TimeSpan elapsed = DateTime.Now - playbackStartTime;

                // Calculate sample position based on time and sample rate
                int samplesElapsed = (int)(elapsed.TotalSeconds * sampleRate);

                if (loopStart >= 0 && loopEnd >= 0)
                {
                    // Loop mode - calculate position within loop region
                    int loopLength = loopEnd - loopStart;
                    if (loopLength > 0)
                    {
                        // Calculate position within loop
                        int loopedPosition = samplesElapsed % loopLength;
                        currentPlayPosition = loopStart + loopedPosition;
                    }
                }
                else
                {
                    // Full file preview mode
                    currentPlayPosition = samplesElapsed;
                    if (audioData != null && currentPlayPosition >= audioData.Length)
                    {
                        // Reset to beginning when reaching the end in full file mode
                        currentPlayPosition = 0;
                        playbackStartTime = DateTime.Now;
                    }
                }

                // Ensure position stays within valid range
                if (audioData != null)
                {
                    currentPlayPosition = Math.Max(0, Math.Min(currentPlayPosition, audioData.Length - 1));
                }

                this.Invalidate();
            }
        }

        public void StartPlayback()
        {
            playbackStartTime = DateTime.Now;
            playheadTimer.Start();

            // If we have loop points, ensure we start from loop start
            if (loopStart >= 0 && loopEnd >= 0)
            {
                currentPlayPosition = loopStart;
            }
            else
            {
                currentPlayPosition = 0;
            }
        }

        // Add this method to explicitly set playhead position
        public void SetPlayheadPosition(int position)
        {
            if (position >= 0 && audioData != null && position < audioData.Length)
            {
                currentPlayPosition = position;
                playbackStartTime = DateTime.Now.AddSeconds(-((double)position / sampleRate));
                this.Invalidate();
            }
        }

        public void SetAudioData(byte[] data)
        {
            audioData = data;
            loopStart = -1;
            loopEnd = -1;
            scrollPosition = 0;

            // Calculate maximum scroll value based on zoom
            UpdateScrollParameters();

            if (InvokeRequired)
            {
                BeginInvoke(new Action(() =>
                {
                    Invalidate();
                    Update();
                }));
            }
            else
            {
                Invalidate();
                Update();
            }
        }

        // update scroll parameters
        private void UpdateScrollParameters()
        {
            if (audioData == null || audioData.Length == 0)
            {
                maxScroll = 0;
                return;
            }

            // Calculate how many samples are visible at current zoom
            int visibleSamples = (int)(Width * (audioData.Length / (float)Width) / zoomFactor);

            // Maximum scroll is total samples minus visible samples
            maxScroll = Math.Max(0, audioData.Length - visibleSamples);

            // Update scrollbar on parent form if it exists
            if (Parent != null)
            {
                var scrollBar = Parent.Controls.OfType<HScrollBar>().FirstOrDefault();
                if (scrollBar != null)
                {
                    scrollBar.Minimum = 0;
                    scrollBar.Maximum = maxScroll + scrollBar.LargeChange;
                    scrollBar.Value = Math.Min(scrollBar.Value, maxScroll);
                }
            }
        }

        public (int start, int end) GetLoopPoints()
        {
            return (loopStart, loopEnd);
        }
        private void UpdateLoopPoints(int start, int end)
        {
            loopStart = start;
            loopEnd = end;
            LoopPointsChanged?.Invoke(this, (start, end));
            Invalidate(); // Redraw to show new loop points
        }

        public void ClearLoopPoints()
        {
            loopStart = -1;
            loopEnd = -1;
            LoopPointsChanged?.Invoke(this, (-1, -1));
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (audioData == null) return;

            int clickedSample = XToSample(e.X);

            // Right click to clear loop points
            if (e.Button == MouseButtons.Right)
            {
                ClearLoopPoints();
                return;
            }

            // If we're near either loop point, start dragging it
            if (loopStart >= 0 && Math.Abs(SampleToX(loopStart) - e.X) <= DRAG_THRESHOLD)
            {
                isDraggingStart = true;
                return;
            }

            if (loopEnd >= 0 && Math.Abs(SampleToX(loopEnd) - e.X) <= DRAG_THRESHOLD)
            {
                isDraggingEnd = true;
                return;
            }

            // If Control key is held, clear existing points before setting new ones
            if (ModifierKeys == Keys.Control)
            {
                ClearLoopPoints();
            }

            // If we're not dragging, set new points
            if (loopStart == -1)
            {
                loopStart = clickedSample;
            }
            else if (loopEnd == -1)
            {
                loopEnd = clickedSample;
                if (loopEnd < loopStart)
                {
                    int temp = loopStart;
                    loopStart = loopEnd;
                    loopEnd = temp;
                }
                LoopPointsChanged?.Invoke(this, (loopStart, loopEnd));
            }
            else
            {
                // Start new loop points
                ClearLoopPoints();
                loopStart = clickedSample;
            }
            Invalidate();
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (audioData == null || (e.Button != MouseButtons.Left)) return;

            int newSample = XToSample(Math.Max(0, Math.Min(e.X, Width)));

            if (isDraggingStart)
            {
                if (newSample != loopStart && (loopEnd == -1 || newSample < loopEnd))
                {
                    loopStart = newSample;
                    if (loopEnd != -1)
                    {
                        LoopPointsChanged?.Invoke(this, (loopStart, loopEnd));
                    }
                    Invalidate();
                }
            }
            else if (isDraggingEnd)
            {
                if (newSample != loopEnd && newSample > loopStart)
                {
                    loopEnd = newSample;
                    LoopPointsChanged?.Invoke(this, (loopStart, loopEnd));
                    Invalidate();
                }
            }
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (audioData == null || audioData.Length == 0) return;

            e.Graphics.Clear(Color.Black);

            int centerY = (int)(this.Height * 0.55);
            float yScale = (Height / 2) / 128f;

            using (var pen = new Pen(Color.Blue))
            {
                // Calculate visible portion based on zoom and scroll
                int visibleSamples = (int)(audioData.Length / zoomFactor);
                int startSample = scrollPosition;

                for (int x = 0; x < Width; x++)
                {
                    int sampleIndex = startSample + (int)((long)x * visibleSamples / Width);
                    int nextIndex = startSample + (int)((long)(x + 1) * visibleSamples / Width);
                    nextIndex = Math.Min(nextIndex, audioData.Length);

                    if (sampleIndex >= audioData.Length) break;

                    byte minSample = 255;
                    byte maxSample = 0;

                    for (int i = sampleIndex; i < nextIndex; i++)
                    {
                        if (i < audioData.Length)
                        {
                            minSample = Math.Min(minSample, audioData[i]);
                            maxSample = Math.Max(maxSample, audioData[i]);
                        }
                    }

                    int minY = centerY + (int)((minSample - 128) * yScale);
                    int maxY = centerY + (int)((maxSample - 128) * yScale);

                    if (maxY - minY < 4)
                    {
                        int mid = (maxY + minY) / 2;
                        minY = mid - 2;
                        maxY = mid + 2;
                    }

                    e.Graphics.DrawLine(pen, x, minY, x, maxY);
                }
            }

            // Draw loop points adjusted for scroll position
            if (loopStart >= 0)
            {
                using (var pen = new Pen(Color.Red, 2))
                {
                    int x = (int)((long)(loopStart - scrollPosition) * Width / (audioData.Length / zoomFactor));
                    if (x >= 0 && x < Width)
                        e.Graphics.DrawLine(pen, x, 0, x, Height);
                }
            }

            if (loopEnd >= 0)
            {
                using (var pen = new Pen(Color.Red, 2))
                {
                    int x = (int)((long)(loopEnd - scrollPosition) * Width / (audioData.Length / zoomFactor));
                    if (x >= 0 && x < Width)
                        e.Graphics.DrawLine(pen, x, 0, x, Height);
                }
            }

            // Draw playhead adjusted for scroll position
            if (currentPlayPosition >= 0)
            {
                using (var pen = new Pen(playheadColor, 2))
                {
                    int x = (int)((long)(currentPlayPosition - scrollPosition) * Width / (audioData.Length / zoomFactor));
                    if (x >= 0 && x < Width)
                        e.Graphics.DrawLine(pen, x, 0, x, Height);
                }
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            isDraggingStart = false;
            isDraggingEnd = false;
            base.OnMouseUp(e);
        }
        private void WaveformViewer_MouseDown(object sender, MouseEventArgs e)
        {
            if (audioData == null) return;

            float samplesPerPixel = (audioData.Length / (float)this.Width) / zoomFactor;
            int samplePos = (int)(e.X * samplesPerPixel) + (int)(scrollOffset * samplesPerPixel);

            if (loopStart >= 0 && Math.Abs((loopStart - scrollOffset * samplesPerPixel) / samplesPerPixel - e.X) < 5)
            {
                isDraggingLoop = true;
                isDraggingStart = true;
            }
            else if (loopEnd >= 0 && Math.Abs((loopEnd - scrollOffset * samplesPerPixel) / samplesPerPixel - e.X) < 5)
            {
                isDraggingLoop = true;
                isDraggingStart = false;
            }
            else
            {
                if (ModifierKeys == Keys.Control)
                {
                    if (loopStart < 0 || ModifierKeys == (Keys.Control | Keys.Shift))
                    {
                        loopStart = samplePos;
                    }
                    else
                    {
                        loopEnd = samplePos;
                    }
                    Invalidate();
                }
            }
        }

        public void UpdateDisplay(byte[] data)
        {
            audioData = data;
            Invalidate();
        }

        private void WaveformViewer_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDraggingLoop && audioData != null)
            {
                float samplesPerPixel = (audioData.Length / (float)this.Width) / zoomFactor;
                int samplePos = (int)(e.X * samplesPerPixel) + (int)(scrollOffset * samplesPerPixel);

                if (isDraggingStart)
                {
                    loopStart = Math.Min(Math.Max(0, samplePos), audioData.Length - 1);
                }
                else
                {
                    loopEnd = Math.Min(Math.Max(0, samplePos), audioData.Length - 1);
                }
                Invalidate();
            }
        }

        private void WaveformViewer_MouseUp(object sender, MouseEventArgs e)
        {
            isDraggingLoop = false;
        }

        public void ZoomIn()
        {
            float oldZoom = zoomFactor;
            zoomFactor = Math.Min(zoomFactor * ZOOM_STEP, MAX_ZOOM);

            // Adjust scroll position to keep the center point centered
            if (oldZoom != zoomFactor)
            {
                float centerPoint = scrollPosition + (Width / 2f) * (audioData.Length / Width) / oldZoom;
                UpdateScrollParameters();
                scrollPosition = (int)(centerPoint - (Width / 2f) * (audioData.Length / Width) / zoomFactor);
                scrollPosition = Math.Max(0, Math.Min(scrollPosition, maxScroll));
            }

            Invalidate();
        }

        public void ZoomOut()
        {
            float oldZoom = zoomFactor;
            zoomFactor = Math.Max(zoomFactor / ZOOM_STEP, MIN_ZOOM);

            // Adjust scroll position to keep the center point centered
            if (oldZoom != zoomFactor)
            {
                float centerPoint = scrollPosition + (Width / 2f) * (audioData.Length / Width) / oldZoom;
                UpdateScrollParameters();
                scrollPosition = (int)(centerPoint - (Width / 2f) * (audioData.Length / Width) / zoomFactor);
                scrollPosition = Math.Max(0, Math.Min(scrollPosition, maxScroll));
            }

            Invalidate();
        }

        public float GetZoomLevel()
        {
            return currentZoom;
        }

        public void SetZoomLevel(float zoom)
        {
            currentZoom = Math.Max(MIN_ZOOM, Math.Min(MAX_ZOOM, zoom));
            Invalidate();
        }

        public void Clear()
        {
            audioData = null;
            loopStart = -1;
            loopEnd = -1;
          //  CutRegions.Clear();  // Clear cut regions when clearing the viewer
            Invalidate();
        }

        public void ScrollTo(int offset)
        {
            scrollPosition = Math.Max(0, Math.Min(offset, maxScroll));
            Invalidate();
        }

        // New version with zoom and scroll handling
        private int XToSample(int x)
        {
            if (audioData == null || Width == 0) return 0;

            // NEW: Calculate samples per pixel at current zoom level
            float samplesPerPixel = (audioData.Length / (float)Width) / zoomFactor;

            // NEW: Convert x coordinate to sample index, accounting for scroll position
            int sampleIndex = (int)(scrollPosition + (x * samplesPerPixel));

            // NEW: Clamp to valid range
            return Math.Max(0, Math.Min(sampleIndex, audioData.Length - 1));
        }

        private int SampleToX(int sample)
        {
            if (audioData == null || audioData.Length == 0) return 0;

            // NEW: Calculate pixels per sample at current zoom level
            float pixelsPerSample = (Width * zoomFactor) / (float)audioData.Length;

            // NEW: Convert sample index to x coordinate, accounting for scroll position
            return (int)((sample - scrollPosition) * pixelsPerSample);
        }
        public void RestoreLoopPoints(int start, int end)
        {
            if (audioData == null) return;

            // Ensure points are within valid range for new data
            start = Math.Max(0, Math.Min(start, audioData.Length - 1));
            end = Math.Max(start + 1, Math.Min(end, audioData.Length));

            loopStart = start;
            loopEnd = end;
            Invalidate();
        }
    }
}