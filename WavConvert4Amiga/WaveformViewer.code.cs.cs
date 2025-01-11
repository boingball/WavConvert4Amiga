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
        }

        public void SetAudioData(byte[] data)
        {
            audioData = data;
            loopStart = -1;
            loopEnd = -1;

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
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (audioData == null) return;

            int clickedSample = XToSample(e.X);

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
                loopStart = clickedSample;
                loopEnd = -1;
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

            int centerY = (int)(this.Height * 0.55);//Move to 55% from top
            float yScale = (Height / 2) / 128f;

            using (var pen = new Pen(Color.Blue))
            {
                // Calculate visible portion based on zoom
                int visibleSamples = (int)(audioData.Length / zoomFactor);
                int startSample = 0; // Can be adjusted for scrolling later

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
                        minSample = Math.Min(minSample, audioData[i]);
                        maxSample = Math.Max(maxSample, audioData[i]);
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

            // Draw loop points
            if (loopStart >= 0)
            {
                using (var pen = new Pen(Color.Red, 2))
                {
                    int x = (int)((long)loopStart * Width / (audioData.Length / zoomFactor));
                    e.Graphics.DrawLine(pen, x, 0, x, Height);
                }
            }

            if (loopEnd >= 0)
            {
                using (var pen = new Pen(Color.Red, 2))
                {
                    int x = (int)((long)loopEnd * Width / (audioData.Length / zoomFactor));
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
            zoomFactor = Math.Min(zoomFactor * ZOOM_STEP, MAX_ZOOM);
            Invalidate();
        }

        public void ZoomOut()
        {
            zoomFactor = Math.Max(zoomFactor / ZOOM_STEP, MIN_ZOOM);
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
            scrollOffset = Math.Max(0, offset);
            Invalidate();
        }
        private int XToSample(int x)
        {
            if (audioData == null || Width == 0) return 0;
            return (int)((long)x * audioData.Length / Width);
        }
        private int SampleToX(int sample)
        {
            if (audioData == null || audioData.Length == 0) return 0;
            return (int)((long)sample * Width / audioData.Length);
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