﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using System.Xml.Linq;
using NAudio.Utils;
using NAudio.Wave;
using WavConvert4Amiga.Properties;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;


namespace WavConvert4Amiga
{

    public partial class MainForm : Form
    {
        private const string VERSION = "1.4a";
        [DllImport("user32.dll")]
        private static extern IntPtr LoadCursorFromFile(string lpFileName);
        private SystemAudioRecorder audioRecorder;
        private WaveformProcessor waveformProcessor;
        private RecordedAudioData recordedAudioData;
        private RecordingIndicator recordingIndicator;
        private List<string> currentEffects = new List<string>();
        private List<(int start, int end)> currentCutRegions = new List<(int start, int end)>();
        private AudioEffectsProcessor audioEffects;
        private ComboBox comboBoxPTNote;
        private CheckBox checkBoxNTSC;
        private CheckBox checkBox16BitWAV;
        private WaveOut waveOut;
        private WaveFormat originalFormat; // Store original format
        private MemoryStream audioStream;
        private RawSourceWaveStream waveStream;
        private WaveformViewer waveformViewer;
        private Button btnZoomIn;
        private Button btnZoomOut;
        private Button btnPreviewLoop;
        private Button btnCut;
        private List<(int start, int end)> cutRegions = new List<(int start, int end)>();
        private Button btnUndo;
        private Button btnRedo;
        private Button btnRecordSystemSound;
        private Button btnRecordMicrophone;
        private Button btnStopRecording;
        private HScrollBar hScrollBar;
        private ComboBox comboBoxMicrophone;
        // Add this field to store the current PCM data
        private byte[] currentPcmData;
        private string lastLoadedFilePath; // Store the path of the last loaded file
        private bool isPlaying = false;
        private int currentPreviewStart = -1;
        private int currentPreviewEnd = -1;
        private string tempSourceFile; // Store path to temp file
        private int originalSampleRate; // Store original format
        private byte[] originalPcmData;
        private readonly object playbackLock = new object();
        private static int previewCounter = 0;
        private IWaveProvider currentWaveProvider;
        private TrackBar trackBarAmplify;
        private Label labelAmplify;
        private float amplificationFactor = 1.0f;
        private float previousAmplificationFactor = 1.0f;  // Add this as a class field
       // Store original data as floating-point intermediate values
        private float[] originalFloatData = null; // Holds the original unprocessed data
        private float[] workingFloatData = null; // Holds the current state after effects and adjustments
        private Dictionary<string, Cursor> customCursors = new Dictionary<string, Cursor>();
        private Font retroFont;
      //  private Stack<byte[]> undoStack = new Stack<byte[]>();
      //  private Stack<byte[]> redoStack = new Stack<byte[]>();
        private Stack<AudioState> undoStack = new Stack<AudioState>();
        private Stack<AudioState> redoStack = new Stack<AudioState>();
        private const int MAX_UNDO_STEPS = 20; // Limit memory usage
        private bool isRecorded = false;


        private Dictionary<string, (int pal, int ntsc)> ptNoteToHz = new Dictionary<string, (int pal, int ntsc)>()
        {
             // Octave 1
                {"C-1", (4144, 4182)}, {"C#1", (4390, 4430)}, {"D-1", (4655, 4698)}, {"D#1", (4926, 4972)},
                {"E-1", (5231, 5280)}, {"F-1", (5542, 5593)}, {"F#1", (5872, 5926)}, {"G-1", (6223, 6280)},
                {"G#1", (6593, 6653)}, {"A-1", (6982, 7046)}, {"A#1", (7389, 7457)}, {"B-1", (7830, 7902)},
                // Octave 2
                {"C-2", (8287, 8363)}, {"C#2", (8779, 8860)}, {"D-2", (9309, 9395)}, {"D#2", (9852, 9943)},
                {"E-2", (10463, 10559)}, {"F-2", (11084, 11186)}, {"F#2", (11745, 11853)}, {"G-2", (12445, 12560)},
                {"G#2", (13185, 13307)}, {"A-2", (13964, 14093)}, {"A#2", (14779, 14915)}, {"B-2", (15694, 15839)},
                // Octave 3
                {"C-3", (16574, 16727)}, {"C#3", (17559, 17721)}, {"D-3", (18668, 18840)}, {"D#3", (19705, 19886)},
                {"E-3", (20864, 21056)}, {"F-3", (22168, 22372)}, {"F#3", (23489, 23706)}, {"G-3", (24803, 25032)},
                {"G#3", (26273, 26515)}, {"A-3", (27928, 28185)}, {"A#3", (29557, 29830)}, {"B-3", (31388, 31677)}
         };

        private AudioState CreateCurrentState()
        {
            string selectedRate = comboBoxSampleRate.Text;
            string sampleRateString = new string(selectedRate.TakeWhile(char.IsDigit).ToArray());
            int sampleRate = int.TryParse(sampleRateString, out int rate) ? rate : 8363;

            return new AudioState(
                currentPcmData,
                sampleRate,
                currentCutRegions.ToList(),
                amplificationFactor,
                currentEffects.ToList()
            );
        }

        public MainForm()
        {
            InitializeComponent();
            this.Text = $"WAVConvert4Amiga v{VERSION}";
            InitializeLoadPanel();
            // Create the checkerboard background
            waveformProcessor = new WaveformProcessor();
            CreateCheckerboardBackground();
            InitializeRecordingIndicator();
            // First create all controls
            // Initialize UI
            InitializeWaveformControls();
            InitializeAmplificationControls();  // This creates trackBarAmplify
            InitializeEffectsPanel();
            audioRecorder = new SystemAudioRecorder();
            InitializeRecordingButtons();
            InitializePTNoteComboBox();
            InitializeCheckboxes();
            // Then style everything
            StyleLabels();
            StyleCheckbox(checkBoxEnable8SVX);
            StyleCheckbox(checkBoxLowPass);
            StyleCheckbox(checkBoxAutoConvert);
            StyleCheckbox(checkBoxMoveOriginal);
            StyleCheckbox(checkBox16BitWAV);
            StyleCheckbox(checkBoxNTSC);
            StyleTrackBar();  // Now the trackbar exists when we try to style it

            // Apply retro styling to the main form
            retroFont = FontManager.GetMainFont();
            this.Font = retroFont;

            // Convert existing buttons to RetroButtons
            var controls = this.Controls.Cast<Control>().ToList();
            foreach (Control control in controls)
            {
                if (control is Button oldButton)
                {
                    RetroButton newButton = new RetroButton();
                    newButton.Location = oldButton.Location;
                    newButton.Size = oldButton.Size;
                    newButton.Text = oldButton.Text;
                    newButton.Name = oldButton.Name;
                    newButton.Click += (s, e) => oldButton.PerformClick();

                    this.Controls.Remove(oldButton);
                    this.Controls.Add(newButton);
                }
                else if (control is Panel panel)
                {
                    AddBevelToPanel(panel);

                    // Also convert buttons inside panels
                    var panelControls = panel.Controls.Cast<Control>().ToList();
                    foreach (Control panelControl in panelControls)
                    {
                        if (panelControl is Button oldPanelButton)
                        {
                            RetroButton newPanelButton = new RetroButton();
                            newPanelButton.Location = oldPanelButton.Location;
                            newPanelButton.Size = oldPanelButton.Size;
                            newPanelButton.Text = oldPanelButton.Text;
                            newPanelButton.Name = oldPanelButton.Name;
                            newPanelButton.Click += (s, e) => oldPanelButton.PerformClick();

                            panel.Controls.Remove(oldPanelButton);
                            panel.Controls.Add(newPanelButton);
                        }
                    }
                }
            }

            // Style the waveform panel
            panelWaveform.BackColor = Color.FromArgb(60, 70, 100);
            AddBevelToPanel(panelWaveform);

            // Style the combo box
            comboBoxSampleRate.BackColor = Color.Black;
            comboBoxSampleRate.ForeColor = Color.FromArgb(255, 215, 0);
            comboBoxSampleRate.FlatStyle = FlatStyle.Flat;

            // Style the list box
            listBoxFiles.BackColor = Color.Black;
            listBoxFiles.ForeColor = Color.FromArgb(255, 215, 0);
            listBoxFiles.BorderStyle = BorderStyle.Fixed3D;

            // Style the waveform panel specifically
            panelWaveform.BackColor = Color.FromArgb(60, 70, 100);  // Darker blue for contrast
            AddBevelToPanel(panelWaveform);

            // Style the combo box
            comboBoxSampleRate.BackColor = Color.Black;
            comboBoxSampleRate.ForeColor = Color.FromArgb(255, 215, 0); // Gold text
            comboBoxSampleRate.FlatStyle = FlatStyle.Flat;

            // Style the list box
            listBoxFiles.BackColor = Color.Black;
            listBoxFiles.ForeColor = Color.FromArgb(255, 215, 0); // Gold text
            listBoxFiles.BorderStyle = BorderStyle.Fixed3D;
            InitializeListBox();
            InitializeComboBox();
            InitializeCursors();
            //InitializeAmplificationControls();
            // Set minimum size to prevent controls from being cut off
            // Adjust these values based on your actual layout needs
            this.MinimumSize = new Size(800, 600);
            BackColor = Color.FromArgb(80, 90, 120); // Darker blue-grey
            ForeColor = Color.White;
            // Set panel colors
            panel1.BackColor = Color.FromArgb(180, 190, 210);  // Lighter blue-grey for panels
            panelWaveform.BackColor = Color.Black;  // Waveform area should be black
            ApplyAmigaStyle(this.Controls);
            comboBoxSampleRate.Leave += ComboBoxSampleRate_Leave;
            waveformViewer.LoopPointsChanged += OnLoopPointsChanged;
            checkBoxLowPass.CheckedChanged += checkBoxLowPass_CheckedChanged;

            // Set ListBox colors if you have any
            if (listBoxFiles != null)
            {
                listBoxFiles.BackColor = Color.Black;
                listBoxFiles.ForeColor = Color.FromArgb(180, 190, 210);
                listBoxFiles.Font = FontManager.GetMainFont(9f);
            }

            // Set ComboBox colors
            if (comboBoxSampleRate != null)
            {
                comboBoxSampleRate.BackColor = Color.Black;
                comboBoxSampleRate.ForeColor = Color.FromArgb(180, 190, 210);
                comboBoxSampleRate.Font = FontManager.GetMainFont(9f, FontStyle.Regular);
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            panel1.AllowDrop = true;
            panel1.DragEnter += panel1_DragEnter;
            panel1.DragDrop += panel1_DragDrop;

            // Populate the ComboBox with common values
            comboBoxSampleRate.Items.Add("150Hz - BitCrushed+++");
            comboBoxSampleRate.Items.Add("250Hz - BitCrushed++");
            comboBoxSampleRate.Items.Add("500Hz - BitCrushed+");
            comboBoxSampleRate.Items.Add("1000Hz - BitCrushed");
            comboBoxSampleRate.Items.Add("4143Hz - Half-Rate");
            comboBoxSampleRate.Items.Add("8287Hz - PAL Middle - C");
            comboBoxSampleRate.Items.Add("8363Hz - NTSC Middle - C");
            comboBoxSampleRate.Items.Add("22050Hz - HQ Already Tuned");
            comboBoxSampleRate.Items.Add("28836Hz - Maximum Quality - PAL");
            comboBoxSampleRate.Items.Add("29101Hz - Maximum Quality - NTSC");
            // Allow manual entry by setting DropDownStyle to DropDown
            comboBoxSampleRate.DropDownStyle = ComboBoxStyle.DropDown;

            // Optionally select a default value
            comboBoxSampleRate.SelectedIndex = 5; // Select the first item by default
        }

        private void InitializeCursors()
        {
            try
            {
                // Create temporary directory for cursor files
                string tempDir = Path.Combine(Path.GetTempPath(), "WavConvert4Amiga");
                if (!Directory.Exists(tempDir))
                    Directory.CreateDirectory(tempDir);

                // Save cursor files
                string normalPath = Path.Combine(tempDir, "normal.cur");
                string busyPath = Path.Combine(tempDir, "busy.cur");
                string handPath = Path.Combine(tempDir, "hand.cur");

                File.WriteAllBytes(normalPath, Properties.Resources.amiga_wb1_mini_normal);
                File.WriteAllBytes(busyPath, Properties.Resources.amiga_wb1_mini_busy);
                File.WriteAllBytes(handPath, Properties.Resources.amiga_wb1_mini_hand);

                // Load cursors using Windows API
                IntPtr normalHandle = LoadCursorFromFile(normalPath);
                IntPtr busyHandle = LoadCursorFromFile(busyPath);
                IntPtr handHandle = LoadCursorFromFile(handPath);

                if (normalHandle != IntPtr.Zero && busyHandle != IntPtr.Zero)
                {
                    customCursors["normal"] = new Cursor(normalHandle);
                    customCursors["busy"] = new Cursor(busyHandle);
                    customCursors["hand"] = new Cursor(handHandle);

                    // Set initial cursor
                    this.Cursor = customCursors["normal"];
                }
                else
                {
                    throw new Exception("Failed to load cursor handles");
                }

                // Clean up temp files
                try
                {
                    File.Delete(normalPath);
                    File.Delete(busyPath);
                    File.Delete(handPath);
                    Directory.Delete(tempDir);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load cursors: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Cursor = Cursors.Default;
            }
        }
        private void InitializeRecordingIndicator()
        {
            recordingIndicator = new RecordingIndicator();
            recordingIndicator.Size = new Size(200, 40);
            recordingIndicator.Location = new Point(this.Width - 220, 10);
            recordingIndicator.Visible = false;
            recordingIndicator.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            recordingIndicator.BringToFront(); // Make sure it's on top
            this.Controls.Add(recordingIndicator);
        }
        private void InitializeCheckboxes()
        {
            // Set up initial handling of checkbox state changes
            checkBoxEnable8SVX.CheckedChanged += (s, e) =>
            {
                // If 8SVX is checked, uncheck 16-bit WAV
                if (checkBoxEnable8SVX.Checked && checkBox16BitWAV.Checked)
                {
                    checkBox16BitWAV.Checked = false;
                }
            };

            checkBox16BitWAV.CheckedChanged += (s, e) =>
            {
                // If 16-bit WAV is checked, uncheck 8SVX
                if (checkBox16BitWAV.Checked && checkBoxEnable8SVX.Checked)
                {
                    checkBoxEnable8SVX.Checked = false;
                }
            };
        }

        private void SetCustomCursor(string cursorType)
        {
            if (customCursors.ContainsKey(cursorType))
            {
                this.Cursor = customCursors[cursorType];
            }
        }

        private void InitializeLoadPanel()
        {
            panel1.Cursor = Cursors.Hand;
            panel1.Click += LoadPanel_Click;
        }
        private void LoadPanel_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "All Supported Files|*.wav;*.8svx;*.iff|WAV files (*.wav)|*.wav|IFF/8SVX files (*.8svx;*.iff)|*.8svx;*.iff|ST Sample Files|*.*|All files (*.*)|*.*";
                openFileDialog.FilterIndex = 1;
                openFileDialog.Multiselect = true;
                openFileDialog.Title = "Select Audio Files";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    SetCustomCursor("busy");
                    try
                    {
                        foreach (string filePath in openFileDialog.FileNames)
                        {
                            StopPreview();
                            trackBarAmplify.Value = 100;
                            ProcessWaveFile(filePath);
                        }
                    }
                    finally
                    {
                        SetCustomCursor("normal");
                    }
                }
            }
        }



        private void InitializeComboBox()
        {
            comboBoxSampleRate.ForeColor = Color.FromArgb(255, 215, 0); // Gold color
            comboBoxSampleRate.BackColor = Color.Black;
            comboBoxSampleRate.Font = FontManager.GetMainFont(9f);
            comboBoxSampleRate.DrawMode = DrawMode.OwnerDrawFixed;
            comboBoxSampleRate.DrawItem += ComboBoxSampleRate_DrawItem;
        }

        private void ComboBoxSampleRate_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                this.ActiveControl = null;
                ProcessSampleRateChange();
            }
        }

        private void ComboBoxSampleRate_Leave(object sender, EventArgs e)
        {
            ProcessSampleRateChange();
        }

        private void ComboBoxSampleRate_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;

            e.DrawBackground();
            using (var brush = new SolidBrush(Color.FromArgb(255, 215, 0))) // Gold color
            {
                e.Graphics.DrawString(comboBoxSampleRate.Items[e.Index].ToString(),
                    e.Font, brush, e.Bounds);
            }
            e.DrawFocusRectangle();
        }

        private void InitializePTNoteComboBox()
        {
            // Create and configure the PT Note ComboBox
            comboBoxPTNote = new ComboBox();
            comboBoxPTNote.Location = new Point(comboBoxSampleRate.Right + 50, comboBoxSampleRate.Top);
            comboBoxPTNote.Width = 100;
            comboBoxPTNote.DropDownStyle = ComboBoxStyle.DropDown;
            comboBoxPTNote.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            comboBoxPTNote.AutoCompleteSource = AutoCompleteSource.ListItems;

            // Style to match existing controls
            comboBoxPTNote.BackColor = Color.Black;
            comboBoxPTNote.ForeColor = Color.FromArgb(255, 215, 0);
            comboBoxPTNote.Font = FontManager.GetMainFont(9f);
            comboBoxPTNote.DrawMode = DrawMode.OwnerDrawFixed;
            comboBoxPTNote.DrawItem += ComboBoxPTNote_DrawItem;

            // Add all notes to the combo box
            foreach (var note in ptNoteToHz.Keys.OrderBy(x => x))
            {
                comboBoxPTNote.Items.Add(note);
            }

            // Create and configure NTSC checkbox
            checkBoxNTSC = new CheckBox();
            checkBoxNTSC.Text = "NTSC";
            checkBoxNTSC.Location = new Point(comboBoxPTNote.Right + 20, comboBoxPTNote.Top + 2);
            checkBoxNTSC.AutoSize = true;
            StyleCheckbox(checkBoxNTSC); // Use existing checkbox styling
            checkBoxNTSC.CheckedChanged += CheckBoxNTSC_CheckedChanged;

            // Handle selection change
            comboBoxPTNote.SelectedIndexChanged += ComboBoxPTNote_SelectedIndexChanged;
            comboBoxPTNote.KeyDown += ComboBoxPTNote_KeyDown;
            comboBoxPTNote.Leave += ComboBoxPTNote_Leave;

            // Add label with adjusted spacing
            Label labelPTNote = new Label();
            labelPTNote.Text = "PT Note";
            labelPTNote.AutoSize = true;
            labelPTNote.Location = new Point(comboBoxPTNote.Left - 45, label1.Top); // Aligned with "Sample Rate" label
            labelPTNote.ForeColor = Color.FromArgb(255, 215, 0);
            labelPTNote.BackColor = Color.Transparent;

            // Add controls to form
            this.Controls.Add(labelPTNote);
            this.Controls.Add(comboBoxPTNote);
            this.Controls.Add(checkBoxNTSC);
        }

        private void ComboBoxPTNote_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;

            e.DrawBackground();
            using (var brush = new SolidBrush(Color.FromArgb(255, 215, 0))) // Gold color
            {
                e.Graphics.DrawString(comboBoxPTNote.Items[e.Index].ToString(),
                    e.Font, brush, e.Bounds);
            }
            e.DrawFocusRectangle();
        }

        private void CheckBoxNTSC_CheckedChanged(object sender, EventArgs e)
        {
            // Update the Hz value if a note is selected
            if (comboBoxPTNote.SelectedItem != null)
            {
                UpdateSampleRateFromNote(comboBoxPTNote.SelectedItem.ToString());
            }
        }

        private void ComboBoxPTNote_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBoxPTNote.SelectedItem == null) return;
            UpdateSampleRateFromNote(comboBoxPTNote.SelectedItem.ToString());
        }

        private void UpdateSampleRateFromNote(string selectedNote)
        {
            if (ptNoteToHz.TryGetValue(selectedNote, out var rates))
            {
                int hz = checkBoxNTSC.Checked ? rates.ntsc : rates.pal;
                comboBoxSampleRate.Text = hz.ToString() + "Hz";
                ProcessSampleRateChange();
                AddToListBox($"Note {selectedNote} - {(checkBoxNTSC.Checked ? "NTSC" : "PAL")} {hz}Hz");
            }
        }

        private void ComboBoxPTNote_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                string input = comboBoxPTNote.Text.ToUpper();
                if (ptNoteToHz.ContainsKey(input))
                {
                    comboBoxPTNote.SelectedItem = input;
                }
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void ComboBoxPTNote_Leave(object sender, EventArgs e)
        {
            string input = comboBoxPTNote.Text.ToUpper();
            if (ptNoteToHz.ContainsKey(input))
            {
                comboBoxPTNote.SelectedItem = input;
            }
            else
            {
                // If invalid input, revert to previous selection
                if (comboBoxPTNote.SelectedItem != null)
                {
                    comboBoxPTNote.Text = comboBoxPTNote.SelectedItem.ToString();
                }
            }
        }

        private void InitializeListBox()
        {
            // Set gold color and monospace font
            listBoxFiles.ForeColor = Color.FromArgb(255, 215, 0); // Gold color
            listBoxFiles.BackColor = Color.Black;
            listBoxFiles.Font = FontManager.GetMainFont();

            // Override the default add method to always scroll to last item
            listBoxFiles.DrawMode = DrawMode.OwnerDrawFixed;
            listBoxFiles.DrawItem += ListBoxFiles_DrawItem;
        }

        private void AddToListBox(string text)
        {
            listBoxFiles.Items.Add(text);
            listBoxFiles.SelectedIndex = listBoxFiles.Items.Count - 1;
            listBoxFiles.TopIndex = listBoxFiles.Items.Count - 1;

            // Force a UI update
            listBoxFiles.Refresh();
            Application.DoEvents();
        }

        private void ListBoxFiles_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;

            e.DrawBackground();
            using (var brush = new SolidBrush(Color.FromArgb(255, 215, 0))) // Gold color
            {
                e.Graphics.DrawString(listBoxFiles.Items[e.Index].ToString(),
                    e.Font, brush, e.Bounds);
            }
            e.DrawFocusRectangle();
        }

        private void InitializeWaveformControls()
        {
            // Initialize the waveform panel
            panelWaveform.Height = 350;
            panelWaveform.Visible = true;

            // Create a flow layout panel for all buttons at the top
            FlowLayoutPanel controlPanel = new FlowLayoutPanel();
            controlPanel.Dock = DockStyle.Top;
            controlPanel.Height = 35; // Increased height for buttons
            controlPanel.Padding = new Padding(5);
            panelWaveform.Controls.Add(controlPanel);
            InitializeEditButtons(controlPanel);

            // Common button size
            Size buttonSize = new Size(100, 25);

            // Add Clear Button
            Button btnClearWaveform = new RetroButton();
            btnClearWaveform.Text = "Clear";
            btnClearWaveform.Size = new Size(100, 25);
            btnClearWaveform.Click += BtnClearWaveform_Click;
            controlPanel.Controls.Add(btnClearWaveform);

            //Zoom Buttons
            btnZoomIn = new RetroButton();
            btnZoomIn.Text = "Zoom In";
            btnZoomIn.Size = new Size(100, 25);
            btnZoomIn.Click += BtnZoomIn_Click;
            controlPanel.Controls.Add(btnZoomIn);

            btnZoomOut = new RetroButton();
            btnZoomOut.Text = "Zoom Out";
            btnZoomOut.Size = new Size(100, 25);
            btnZoomOut.Click += BtnZoomOut_Click;
            controlPanel.Controls.Add(btnZoomOut);

            // Add Save Loop Points (8SVX) button
            Button btnSaveLoop8SVX = new RetroButton();
            btnSaveLoop8SVX.Text = "Save Loop Points (8SVX)";
            btnSaveLoop8SVX.Size = new Size(160, 25); // Wider for longer text
            btnSaveLoop8SVX.Click += BtnSaveLoop8SVX_Click;
            controlPanel.Controls.Add(btnSaveLoop8SVX);

            // Add Save Loop button
            Button btnSaveLoop = new RetroButton();
            btnSaveLoop.Text = "Save Loop";
            btnSaveLoop.Size = buttonSize;
            btnSaveLoop.Click += BtnSaveLoop_Click;
            controlPanel.Controls.Add(btnSaveLoop);

            // Add Preview button
            btnPreviewLoop = new RetroButton();
            btnPreviewLoop.Text = "Preview";
            btnPreviewLoop.Size = buttonSize;
            btnPreviewLoop.Click += BtnPreviewLoop_Click;
            controlPanel.Controls.Add(btnPreviewLoop);

            // Initialize the waveform viewer AFTER the control panel
            waveformViewer = new WaveformViewer();
            waveformViewer.Dock = DockStyle.Fill;
            waveformViewer.LoopPointsChanged += OnLoopPointsChanged;
            panelWaveform.Controls.Add(waveformViewer);

            // Add scroll bar
            hScrollBar = new HScrollBar();
            hScrollBar.Dock = DockStyle.Bottom;
            hScrollBar.Height = 20;
            hScrollBar.Scroll += HScrollBar_Scroll;
            panelWaveform.Controls.Add(hScrollBar);

            // Make sure waveform viewer is set up to display data
            if (currentPcmData != null)
            {
                waveformViewer.SetAudioData(currentPcmData);
            }
        }

        private void BtnZoomIn_Click(object sender, EventArgs e)
        {
            waveformViewer.ZoomIn();
        }

        private void BtnZoomOut_Click(object sender, EventArgs e)
        {
            waveformViewer.ZoomOut();
        }

        private void BtnClearWaveform_Click(object sender, EventArgs e)
        {
            // Stop any ongoing playback
            StopPreview();

            // Clear all state
            ClearAllState();

            // Clear the waveform data
            currentPcmData = null;
            originalPcmData = null;

            // Clear the waveform viewer
            if (waveformViewer != null)
            {
                waveformViewer.Clear(); // Ensure your `WaveformViewer` class has a Clear method
            }

            // Clear the undo/redo stacks
            undoStack.Clear();
            redoStack.Clear();

            // Reset audio-related variables
            isRecorded = false;
            lastLoadedFilePath = null;
            originalFormat = null;

            // Update UI state
            UpdateEditButtonStates();

            // Add feedback to the list box or status bar
            AddToListBox("Waveform and PCM data cleared.");
        }

        private void HScrollBar_Scroll(object sender, ScrollEventArgs e)
        {
            waveformViewer.ScrollTo(e.NewValue);
        }

        private void BtnCut_Click(object sender, EventArgs e)
        {
            if (currentPcmData == null) return;

            var (start, end) = waveformViewer.GetLoopPoints();
            if (start < 0 || end < 0 || start >= end)
            {
                MessageBox.Show("Please set valid loop points first.", "Invalid Selection",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Store original length for logging
            int originalLength = currentPcmData.Length;

            // IMPORTANT: Store current state in undo stack BEFORE making changes
            PushUndo(currentPcmData);
            redoStack.Clear();

            // Create new array without the cut section
            byte[] newData = new byte[currentPcmData.Length - (end - start)];

            // Copy data before cut point
            Array.Copy(currentPcmData, 0, newData, 0, start);

            // Copy data after cut point  
            Array.Copy(currentPcmData, end, newData, start, currentPcmData.Length - end);

            // Update current data
            currentPcmData = newData;

            // Add cut marker for counting/logging
            currentCutRegions.Add((start, end));

            // Update waveform display with the new cut data
            waveformViewer.SetAudioData(currentPcmData);
            UpdateEditButtonStates();

            // Clear loop points after cut (since the cut region is gone)
            waveformViewer.ClearLoopPoints();

            if (isPlaying)
            {
                StopPreview();
            }

            AddToListBox($"Cut applied. Audio length: {originalLength} → {currentPcmData.Length}. Total cuts: {currentCutRegions.Count}");
        }


        private void BtnUndo_Click(object sender, EventArgs e)
        {
            if (undoStack.Count == 0) return;

            // Store current complete state in redo stack before undoing
            PushRedo(currentPcmData);

            // Restore previous complete state
            var previousState = undoStack.Pop();

            // Restore all the state variables
            currentPcmData = new byte[previousState.AudioData.Length];
            Array.Copy(previousState.AudioData, currentPcmData, previousState.AudioData.Length);

            // IMPORTANT: Restore the effects and cuts lists
            currentEffects = previousState.AppliedEffects.ToList();
            currentCutRegions = previousState.CutRegions.ToList();
            amplificationFactor = previousState.AmplificationFactor;

            // CRITICAL FIX: Update sample rate in UI to match the restored state
            comboBoxSampleRate.Text = $"{previousState.SampleRate}Hz";

            // Update amplification UI
            trackBarAmplify.Value = (int)(amplificationFactor * 100);
            labelAmplify.Text = $"Amplify: {trackBarAmplify.Value}%";

            // Update waveform display
            waveformViewer.SetAudioData(currentPcmData);
            waveformViewer.SetSampleRate(previousState.SampleRate);

            // Update UI state
            UpdateEditButtonStates();

            // If preview was playing, stop it
            if (isPlaying)
            {
                StopPreview();
            }

            AddToListBox($"Undo: Restored state at {previousState.SampleRate}Hz with {currentEffects.Count} effects, {currentCutRegions.Count} cuts");
        }

        private void BtnPreviewLoop_Click(object sender, EventArgs e)
        {
            if (isPlaying)
            {
                StopPreview();
            }
            else
            {
                if (currentPcmData == null)
                {
                    MessageBox.Show("Please load a file first.", "No File Loaded", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var (start, end) = waveformViewer.GetLoopPoints();
                if (start < 0 || end < 0)
                {
                    // If no loop points set, preview entire sample
                    StartPreview(0, currentPcmData.Length);
                }
                else
                {
                    StartPreview(start, end);
                }
            }
        }

        private byte[] ApplyAllModifications(byte[] sourceData, int targetSampleRate,
                                   List<(int start, int end)> cuts = null,
                                   float amplification = 1.0f,
                                   List<string> effects = null)
        {
            if (sourceData == null) return null;

            byte[] result = sourceData;

            try
            {
                // Step 1: Resample to target rate (only if source is original data)
                if (originalFormat != null && sourceData == originalPcmData && originalFormat.SampleRate != targetSampleRate)
                {
                    using (var sourceMs = new MemoryStream())
                    {
                        using (var writer = new WaveFileWriter(sourceMs, originalFormat))
                        {
                            writer.Write(result, 0, result.Length);
                            writer.Flush();
                            sourceMs.Position = 0;

                            using (var reader = new WaveFileReader(sourceMs))
                            using (var resampler = new MediaFoundationResampler(reader, new WaveFormat(targetSampleRate, 8, 1)))
                            {
                                resampler.ResamplerQuality = 60;
                                result = GetPCMData(resampler);
                            }
                        }
                    }
                }

                // Step 2: Apply cuts (only if working from original data)
                if (cuts != null && cuts.Count > 0 && sourceData == originalPcmData)
                {
                    // Calculate cut positions based on resampling ratio
                    double ratio = originalFormat != null ? (double)targetSampleRate / originalFormat.SampleRate : 1.0;

                    foreach (var cut in cuts.OrderByDescending(c => c.start))
                    {
                        int scaledStart = (int)(cut.start * ratio);
                        int scaledEnd = (int)(cut.end * ratio);

                        // Ensure cuts are within bounds
                        scaledStart = Math.Max(0, Math.Min(scaledStart, result.Length));
                        scaledEnd = Math.Max(scaledStart, Math.Min(scaledEnd, result.Length));

                        if (scaledEnd > scaledStart)
                        {
                            byte[] newData = new byte[result.Length - (scaledEnd - scaledStart)];
                            Array.Copy(result, 0, newData, 0, scaledStart);
                            Array.Copy(result, scaledEnd, newData, scaledStart, result.Length - scaledEnd);
                            result = newData;
                        }
                    }
                }

                // Step 3: Apply amplification
                if (amplification != 1.0f)
                {
                    result = waveformProcessor.ApplyAmplification(result, amplification);
                }

                // Step 4: Apply effects
                if (effects != null)
                {
                    foreach (string effect in effects)
                    {
                        switch (effect)
                        {
                            case "lowpass":
                                if (checkBoxLowPass.Checked)
                                {
                                    float cutoffFrequency = targetSampleRate * 0.45f;
                                    result = waveformProcessor.ApplyLowPassFilter(result, targetSampleRate, cutoffFrequency);
                                }
                                break;
                            case "underwater":
                                result = audioEffects.ApplyUnderwaterEffect(result, targetSampleRate);
                                break;
                            case "robot":
                                result = audioEffects.ApplyRobotEffect(result, targetSampleRate);
                                break;
                            case "highpitch":
                                result = audioEffects.ApplyPitchShift(result, targetSampleRate, 1.5f);
                                break;
                            case "lowpitch":
                                result = audioEffects.ApplyPitchShift(result, targetSampleRate, 0.75f);
                                break;
                            case "echo":
                                result = audioEffects.ApplyEchoEffect(result, targetSampleRate);
                                break;
                            case "vocal":
                                result = audioEffects.ApplyVocalRemoval(result, targetSampleRate);
                                break;
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                AddToListBox($"Error applying modifications: {ex.Message}");
                return sourceData;
            }
        }

        private void DisableControlsDuringRecording()
        {

            foreach (Control control in panelWaveform.Controls)
            {
                control.Enabled = false;
            }
            // Disable top controls
            checkBoxEnable8SVX.Enabled = false;
            checkBoxAutoConvert.Enabled = false;
            checkBoxMoveOriginal.Enabled = false;
            checkBoxLowPass.Enabled = false;
            listBoxFiles.Enabled = false;

            // Disable panels and their contents
            panel1.AllowDrop = false;
            panel1.Enabled = false;
            panelWaveform.Enabled = false;

            // Explicitly disable specific buttons
            btnRecordSystemSound.Enabled = false;
            btnRecordMicrophone.Enabled = false;
            btnManualConvert.Enabled = false;
            btnZoomIn.Enabled = false;
            btnZoomOut.Enabled = false;
            btnPreviewLoop.Enabled = false;
            btnCut.Enabled = false;
            btnUndo.Enabled = false;
            btnRedo.Enabled = false;

            // Disable interactive controls
            comboBoxSampleRate.Enabled = false;
            comboBoxPTNote.Enabled = false;
            checkBoxNTSC.Enabled = false;
            trackBarAmplify.Enabled = false;

            // Disable effects panel
            panelBottom.Controls.OfType<Panel>().Where(p => p != btnStopRecording.Parent).ToList()
                .ForEach(p => p.Enabled = false);

            // Ensure stop button stays enabled
            btnStopRecording.Enabled = true;
            btnStopRecording.Invalidate();

        }
        private void EnableAllControls()
        {
            foreach (Control control in panelWaveform.Controls)
            {
                control.Enabled = true;
            }

            // Disable top controls
            checkBoxEnable8SVX.Enabled = true;
            checkBoxAutoConvert.Enabled = true;
            checkBoxMoveOriginal.Enabled = true;
            checkBoxLowPass.Enabled = true;
            listBoxFiles.Enabled = true;

            // Disable panels and their contents
            panel1.AllowDrop = true;
            panel1.Enabled = true;
            panelWaveform.Enabled = true;

            // Explicitly enable specific buttons
            btnRecordSystemSound.Enabled = true;
            btnRecordMicrophone.Enabled = true;
            btnManualConvert.Enabled = true;
            btnZoomIn.Enabled = true;
            btnZoomOut.Enabled = true;
            btnPreviewLoop.Enabled = true;
            btnCut.Enabled = true;
            btnUndo.Enabled = true;
            btnRedo.Enabled = true;

            // Disable interactive controls
            comboBoxSampleRate.Enabled = true;
            comboBoxPTNote.Enabled = true;
            checkBoxNTSC.Enabled = true;
            trackBarAmplify.Enabled = true;

            // Disable effects panel
            panelBottom.Controls.OfType<Panel>().Where(p => p != btnStopRecording.Parent).ToList()
                .ForEach(p => p.Enabled = true);

            // Ensure stop button stays enabled
            btnStopRecording.Enabled = false;
            btnStopRecording.Invalidate();
        }

        private void InitializeRecordingButtons()
        {
            audioRecorder = new SystemAudioRecorder();

            // Create a panel for the recording controls
            Panel recordingPanel = new Panel
            {
                Location = new Point(10, 10),
                Size = new Size(325, 160),
                BackColor = Color.FromArgb(180, 190, 210)
            };
            AddBevelToPanel(recordingPanel);

            // Record System Sound button
            btnRecordSystemSound = new RetroButton
            {
                Text = "Record System",
                Size = new Size(150, 30),
                Location = new Point(10, 10)
            };
            btnRecordSystemSound.Click += (s, e) =>
            {
                StopPreview();
                ClearAllState();
                try
                {
                    int sampleRate = GetSelectedSampleRate();
                    audioRecorder.StartRecordingSystemSound(sampleRate);
                    AddToListBox($"Recording system sound at {sampleRate} Hz...");
                    isRecorded = true;

                    // Disable all controls except stop recording
                    DisableControlsDuringRecording();
                    btnStopRecording.Enabled = true;


                    // Show and start recording indicator
                    recordingIndicator.RecordingType = "system";
                    recordingIndicator.Visible = true;
                    recordingIndicator.StartBlinking();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to start recording: {ex.Message}", "Recording Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            recordingPanel.Controls.Add(btnRecordSystemSound);

            // Record Microphone button
            btnRecordMicrophone = new RetroButton
            {
                Text = "Record Mic",
                Size = new Size(150, 30),
                Location = new Point(170, 10)
            };

            // Add Microphone Selection ComboBox
            Label labelMic = new Label
            {
                Text = "Microphone:",
                Location = new Point(10, 90),
                AutoSize = true,
                ForeColor = Color.Black,
                Font = FontManager.GetMainFont(9f)
            };
            recordingPanel.Controls.Add(labelMic);
          
            comboBoxMicrophone = new ComboBox
            {
                Location = new Point(100, 88),
                Size = new Size(215, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.Black,
                ForeColor = Color.FromArgb(255, 215, 0),
                Font = FontManager.GetMainFont(9f)
            };
            // Style the combo box
            comboBoxMicrophone.DrawMode = DrawMode.OwnerDrawFixed;
            comboBoxMicrophone.DrawItem += (s, e) =>
            {
                e.DrawBackground();
                if (e.Index >= 0)
                {
                    using (var brush = new SolidBrush(Color.FromArgb(255, 215, 0)))
                    {
                        e.Graphics.DrawString(comboBoxMicrophone.Items[e.Index].ToString(),
                            e.Font, brush, e.Bounds);
                    }
                }
                e.DrawFocusRectangle();
            };

            // Populate microphone list
            var mics = SystemAudioRecorder.GetAvailableMicrophones();
            foreach (var mic in mics)
            {
                comboBoxMicrophone.Items.Add(mic.ProductName);
            }

            if (comboBoxMicrophone.Items.Count > 0)
            {
                comboBoxMicrophone.SelectedIndex = 0;
            }
            recordingPanel.Controls.Add(comboBoxMicrophone);

            btnRecordMicrophone.Click += (s, e) =>
            {
                StopPreview();
                ClearAllState();
                try
                {
                    if (comboBoxMicrophone.SelectedIndex < 0)
                    {
                        MessageBox.Show("Please select a microphone.", "No Microphone Selected",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    int sampleRate = GetSelectedSampleRate();
                    int selectedDeviceNumber = comboBoxMicrophone.SelectedIndex;

                    // Verify the device is still valid
                    if (!SystemAudioRecorder.IsValidDeviceNumber(selectedDeviceNumber))
                    {
                        MessageBox.Show("Selected microphone is no longer available.",
                            "Device Unavailable", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        RefreshMicrophoneList(); // Helper method to refresh the list
                        return;
                    }

                    audioRecorder.StartRecordingMicrophone(sampleRate, selectedDeviceNumber);
                    AddToListBox($"Recording microphone ({comboBoxMicrophone.Text}) at {sampleRate} Hz...");
                    isRecorded = true;

                    // Disable all controls except stop recording
                    DisableControlsDuringRecording();
                    btnStopRecording.Enabled = true;

                    // Show and start recording indicator
                    recordingIndicator.RecordingType = "microphone";
                    recordingIndicator.Visible = true;
                    recordingIndicator.StartBlinking();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to start recording: {ex.Message}", "Recording Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            recordingPanel.Controls.Add(btnRecordMicrophone);

            // Stop Recording button
            btnStopRecording = new RetroButton
            {
                Text = "Stop Recording",
                Size = new Size(150, 30),
                Location = new Point(10, 120),
                Enabled = false
            };
            btnStopRecording.Click += async (s, e) =>
            {
                try
                {
                    btnStopRecording.Enabled = false;
                    AddToListBox("Stopping recording...");

                    await audioRecorder.StopRecording();

                    // Stop and hide recording indicator
                    recordingIndicator.StopBlinking();
                    recordingIndicator.Visible = false;

                    if (audioRecorder.RecordedData != null)
                    {
                        currentPcmData = audioRecorder.RecordedData;
                        originalPcmData = new byte[currentPcmData.Length];
                        Debug.WriteLine($"Updating currentPcmData. Source length: {currentPcmData.Length}");
                        Array.Copy(currentPcmData, originalPcmData, currentPcmData.Length);

                        // Store the original format
                        originalFormat = audioRecorder.CapturedFormat;

                        // Create proper WaveFormat for recorded data if needed
                        if (originalFormat == null)
                        {
                            string selectedSampleRate = comboBoxSampleRate.Text;
                            string sampleRateString = new string(selectedSampleRate.TakeWhile(char.IsDigit).ToArray());
                            int sampleRate = int.TryParse(sampleRateString, out int rate) ? rate : 44100;
                            originalFormat = new WaveFormat(sampleRate, 16, 1);
                        }

                        this.Invoke(new Action(() =>
                        {
                            waveformViewer.Clear();
                            ProcessWithCurrentSampleRate();
                            waveformViewer.SetAudioData(currentPcmData);
                            StoreInitialState();
                        }));

                        AddToListBox($"Recorded {currentPcmData.Length} bytes of audio data");
                    }
                    else
                    {
                        AddToListBox("No valid data recorded");
                        isRecorded = false;
                    }
                }
                catch (Exception ex)
                {
                    AddToListBox($"Error stopping recording: {ex.Message}");
                    MessageBox.Show($"Error stopping recording: {ex.Message}", "Recording Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    isRecorded = false;
                }
                finally
                {
                    // Re-enable all controls
                    EnableAllControls();
                    btnStopRecording.Enabled = false;
                }
            };
            recordingPanel.Controls.Add(btnStopRecording);

            // Add the recording panel to the bottom panel
            panelBottom.Controls.Add(recordingPanel);
        }

        // Add this helper method to refresh the microphone list
        private void RefreshMicrophoneList()
        {
            comboBoxMicrophone.Items.Clear();
            var mics = SystemAudioRecorder.GetAvailableMicrophones();
            foreach (var mic in mics)
            {
                comboBoxMicrophone.Items.Add(mic.ProductName);
            }

            if (comboBoxMicrophone.Items.Count > 0)
            {
                comboBoxMicrophone.SelectedIndex = 0;
            }
        }
        private void PushUndo(byte[] data)
        {
            // Store the complete current state including effects and cuts
            var state = new AudioState(
                data,
                GetSelectedSampleRate(),
                currentCutRegions.ToList(),
                amplificationFactor,
                currentEffects.ToList()
            );

            undoStack.Push(state);

            // Limit stack size
            if (undoStack.Count > MAX_UNDO_STEPS)
            {
                var tempStack = new Stack<AudioState>();
                for (int i = 0; i < MAX_UNDO_STEPS; i++)
                {
                    tempStack.Push(undoStack.Pop());
                }
                undoStack = tempStack;
            }
        }

        private void BtnRedo_Click(object sender, EventArgs e)
        {
            if (redoStack.Count == 0) return;

            // Store current complete state in undo stack
            PushUndo(currentPcmData);

            // Restore redo state
            var redoState = redoStack.Pop();

            // Restore all the state variables
            currentPcmData = new byte[redoState.AudioData.Length];
            Array.Copy(redoState.AudioData, currentPcmData, redoState.AudioData.Length);

            // IMPORTANT: Restore the effects and cuts lists
            currentEffects = redoState.AppliedEffects.ToList();
            currentCutRegions = redoState.CutRegions.ToList();
            amplificationFactor = redoState.AmplificationFactor;

            // CRITICAL FIX: Update sample rate in UI to match the restored state
            comboBoxSampleRate.Text = $"{redoState.SampleRate}Hz";

            // Update amplification UI
            trackBarAmplify.Value = (int)(amplificationFactor * 100);
            labelAmplify.Text = $"Amplify: {trackBarAmplify.Value}%";

            // Update waveform display
            waveformViewer.SetAudioData(currentPcmData);
            waveformViewer.SetSampleRate(redoState.SampleRate);

            // Update UI state
            UpdateEditButtonStates();

            // If preview was playing, stop it
            if (isPlaying)
            {
                StopPreview();
            }

            AddToListBox($"Redo: Restored state at {redoState.SampleRate}Hz with {currentEffects.Count} effects, {currentCutRegions.Count} cuts");
        }

        private void PushRedo(byte[] data)
        {
            // Store the complete current state including effects and cuts
            var state = new AudioState(
                data,
                GetSelectedSampleRate(),
                currentCutRegions.ToList(),
                amplificationFactor,
                currentEffects.ToList()
            );

            redoStack.Push(state);

            // Limit stack size
            if (redoStack.Count > MAX_UNDO_STEPS)
            {
                var tempStack = new Stack<AudioState>();
                for (int i = 0; i < MAX_UNDO_STEPS; i++)
                {
                    tempStack.Push(redoStack.Pop());
                }
                redoStack = tempStack;
            }
        }

        private void ProcessRecordedAudio()
        {
            isRecorded = true;
            try
            {
                if (audioRecorder.RecordedData == null || audioRecorder.RecordedData.Length == 0)
                {
                    AddToListBox("No valid recording data available");
                    return;
                }

                // Store high quality original data
                originalPcmData = audioRecorder.RecordedData;
                originalFormat = audioRecorder.CapturedFormat;
                originalSampleRate = originalFormat.SampleRate;

                // Get selected sample rate for initial conversion
                int targetSampleRate = GetSelectedSampleRate();

                // Create the working copy at the target rate
                using (var sourceMs = new MemoryStream())
                {
                    using (var writer = new WaveFileWriter(sourceMs, originalFormat))
                    {
                        writer.Write(originalPcmData, 0, originalPcmData.Length);
                        writer.Flush();
                        sourceMs.Position = 0;

                        using (var reader = new WaveFileReader(sourceMs))
                        using (var resampler = new MediaFoundationResampler(reader, new WaveFormat(targetSampleRate, 8, 1)))
                        {
                            resampler.ResamplerQuality = 60;
                            currentPcmData = GetPCMData(resampler);
                        }
                    }
                }

                // Update display with the resampled data
                waveformViewer.SetAudioData(currentPcmData);

                AddToListBox($"Recorded {originalPcmData.Length} bytes at {originalSampleRate}Hz");
                AddToListBox($"Converted to {currentPcmData.Length} bytes at {targetSampleRate}Hz");

                // Enable UI controls
                trackBarAmplify.Enabled = true;
                btnManualConvert.Enabled = true;
            }
            catch (Exception ex)
            {
                isRecorded = false;
                AddToListBox($"Error processing recorded audio: {ex.Message}");
                MessageBox.Show($"Error processing recorded audio: {ex.Message}", "Processing Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateRecordingButtons(bool isRecording)
        {
            btnRecordSystemSound.Enabled = !isRecording;
            btnRecordMicrophone.Enabled = !isRecording;
            btnStopRecording.Enabled = isRecording;
        }

        private void OnLoopPointsChanged(object sender, (int start, int end) loopPoints)
        {
            // If loop points were cleared, stop playback
            if (loopPoints.start < 0 || loopPoints.end < 0)
            {
                StopPreview();
            }
            // If both points are set and valid
            else if (loopPoints.start < loopPoints.end)
            {
                if (isPlaying)
                {
                    // Update existing preview if already playing
                    UpdatePreviewLoopPoints(loopPoints.start, loopPoints.end);
                }
                else
                {
                    // Auto-start preview for new loop points
                    StartPreview(loopPoints.start, loopPoints.end);
                }
            }

            // Enable/disable cut button based on loop point validity
            btnCut.Enabled = currentPcmData != null &&
                             loopPoints.start >= 0 &&
                             loopPoints.end >= 0 &&
                             loopPoints.start < loopPoints.end;
        }

        private void UpdatePreviewLoopPoints(int start, int end)
        {
            try
            {
                if (currentPcmData == null || start < 0 || end > currentPcmData.Length || start >= end)
                {
                    return;
                }

                string selectedSampleRate = comboBoxSampleRate.Text;
                string sampleRateString = new string(selectedSampleRate.TakeWhile(char.IsDigit).ToArray());
                if (!int.TryParse(sampleRateString, out int targetSampleRate) || targetSampleRate <= 0)
                {
                    return;
                }

                // Extract the new section to play
                byte[] sectionToPlay = new byte[end - start];
                Array.Copy(currentPcmData, start, sectionToPlay, 0, end - start);

                if (checkBoxLowPass.Checked)
                {
                    float cutoffFrequency = targetSampleRate * 0.45f;
                    sectionToPlay = waveformProcessor.ApplyLowPassFilter(sectionToPlay, targetSampleRate, cutoffFrequency);
                }

                // Clean up old streams
                if (audioStream != null)
                {
                    audioStream.Dispose();
                }
                if (waveStream != null)
                {
                    waveStream.Dispose();
                }

                // Create new streams with the updated section
                audioStream = new MemoryStream(sectionToPlay);
                var waveFormat = new WaveFormat(targetSampleRate, 8, 1);
                waveStream = new RawSourceWaveStream(audioStream, waveFormat);

                // Create new looping provider
                var loopProvider = new LoopingWaveProvider(waveStream, 0, sectionToPlay.Length);

                // Update the wave out
                var oldWaveOut = waveOut;
                waveOut = new WaveOut();
                waveOut.Init(loopProvider);
                waveOut.PlaybackStopped += OnPlaybackStopped;

                // Start the new playback
                waveOut.Play();

                // Clean up the old waveOut after starting the new one to minimize gap
                if (oldWaveOut != null)
                {
                    oldWaveOut.Stop();
                    oldWaveOut.Dispose();
                }

                currentWaveProvider = loopProvider;
                currentPreviewStart = start;
                currentPreviewEnd = end;

                // Update waveform viewer
                waveformViewer.SetSampleRate(targetSampleRate);
                waveformViewer.SetPlayheadPosition(start);
                waveformViewer.StartPlayback();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating preview: {ex.Message}");
            }
        }
        private void StartPreview(int start, int end)
        {
            try
            {
                if (currentPcmData == null || start < 0 || end > currentPcmData.Length || start >= end)
                {
                    StopPreview();
                    return;
                }

                StopPreview();

                string selectedSampleRate = comboBoxSampleRate.Text;
                string sampleRateString = new string(selectedSampleRate.TakeWhile(char.IsDigit).ToArray());
                if (!int.TryParse(sampleRateString, out int targetSampleRate) || targetSampleRate <= 0)
                {
                    MessageBox.Show("Invalid sample rate selected.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Extract the section to play
                byte[] sectionToPlay = new byte[end - start];
                Array.Copy(currentPcmData, start, sectionToPlay, 0, end - start);

                if (checkBoxLowPass.Checked)
                {
                    float cutoffFrequency = targetSampleRate * 0.45f;
                    sectionToPlay = waveformProcessor.ApplyLowPassFilter(sectionToPlay, targetSampleRate, cutoffFrequency);
                }

                audioStream = new MemoryStream(sectionToPlay);
                var waveFormat = new WaveFormat(targetSampleRate, 8, 1);
                waveStream = new RawSourceWaveStream(audioStream, waveFormat);

                // Create the looping provider
                var loopProvider = new LoopingWaveProvider(waveStream, 0, sectionToPlay.Length);
                currentWaveProvider = loopProvider;

                waveOut = new WaveOut();
                waveOut.Init(loopProvider);
                waveOut.PlaybackStopped += OnPlaybackStopped;

                isPlaying = true;
                currentPreviewStart = start;
                currentPreviewEnd = end;

                // Configure waveform viewer for playback
                waveformViewer.SetSampleRate(targetSampleRate);
                waveformViewer.SetPlayheadPosition(start);  // Use new method to set initial position
                waveformViewer.StartPlayback();

                waveOut.Play();
                btnPreviewLoop.Text = "Stop";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error previewing audio: {ex.Message}", "Preview Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                StopPreview();
            }
        }

        private void StopPreview(object sender = null, EventArgs e = null)
        {
            if (waveOut != null)
            {
                try
                {
                    waveOut.Stop();
                    waveOut.Dispose();
                    waveOut = null;
                }
                catch (Exception) { }
            }

            if (waveStream != null)
            {
                try
                {
                    waveStream.Dispose();
                    waveStream = null;
                }
                catch (Exception) { }
            }

            if (audioStream != null)
            {
                try
                {
                    audioStream.Dispose();
                    audioStream = null;
                }
                catch (Exception) { }
            }

            // Stop playhead animation and reset position
            if (waveformViewer != null)
            {
                waveformViewer.StopPlayheadAnimation();
            }

            isPlaying = false;
            btnPreviewLoop.Text = "Preview";
            currentPreviewStart = -1;
            currentPreviewEnd = -1;
        }

        private void InitializeEditButtons(FlowLayoutPanel controlPanel)
        {
            // Cut button
            btnCut = new RetroButton();
            btnCut.Text = "Cut";
            btnCut.Size = new Size(100, 25);
            btnCut.Click += BtnCut_Click;
            btnCut.Enabled = false; // Disabled until loop points are set
            controlPanel.Controls.Add(btnCut);

            // Undo button
            btnUndo = new RetroButton();
            btnUndo.Text = "Undo";
            btnUndo.Size = new Size(100, 25);
            btnUndo.Click += BtnUndo_Click;
            btnUndo.Enabled = false;
            controlPanel.Controls.Add(btnUndo);

            // Redo button
            btnRedo = new RetroButton();
            btnRedo.Text = "Redo";
            btnRedo.Size = new Size(100, 25);
            btnRedo.Click += BtnRedo_Click;
            btnRedo.Enabled = false;
            controlPanel.Controls.Add(btnRedo);
        }

        private void UpdateEditButtonStates()
        {
            btnUndo.Enabled = undoStack.Count > 0;
            btnRedo.Enabled = redoStack.Count > 0;
            btnCut.Enabled = currentPcmData != null;
        }

        private void InitializeAmplificationControls()
        {
            // Create panel for amplification
            Panel amplifyPanel = new Panel();
            amplifyPanel.Height = 30;
            amplifyPanel.Dock = DockStyle.Bottom;
            amplifyPanel.BackColor = Color.FromArgb(180, 190, 210); // Match Amiga style

            // Create label
            labelAmplify = new Label();
            labelAmplify.Text = "Amplify: 100%";
            labelAmplify.AutoSize = true;
            labelAmplify.ForeColor = Color.FromArgb(255, 215, 0); // Gold color
            labelAmplify.Location = new Point(5, 5);
            amplifyPanel.Controls.Add(labelAmplify);

            // Create trackbar
            trackBarAmplify = new TrackBar();
            trackBarAmplify.Minimum = 100;
            trackBarAmplify.Maximum = 500;
            trackBarAmplify.Value = 100;
            trackBarAmplify.TickFrequency = 50;
            trackBarAmplify.LargeChange = 50;
            trackBarAmplify.SmallChange = 10;
            trackBarAmplify.Width = 200;
            trackBarAmplify.Height = 30;
            trackBarAmplify.Location = new Point(labelAmplify.Right + 5, 0);

            // Important: Reconnect the ValueChanged event
            trackBarAmplify.ValueChanged += (s, e) =>
            {
                amplificationFactor = trackBarAmplify.Value / 100.0f;
                labelAmplify.Text = $"Amplify: {trackBarAmplify.Value}%";

                //Need to check preview data here and try to process this first if we have some.
                if (currentPcmData != null)
                {

                    // Create undo point
                    byte[] prevData = new byte[currentPcmData.Length];
                    Array.Copy(currentPcmData, prevData, currentPcmData.Length);
                    PushUndo(prevData);

                    // Store current playback state
                    bool wasPlaying = isPlaying;
                    var (oldStart, oldEnd) = waveformViewer.GetLoopPoints();

                    // Stop any current playback
                    if (wasPlaying)
                    {
                        StopPreview();
                    }

                    // Amplify and convert the PCM data

                    currentPcmData = AmplifyAndConvert(currentPcmData, amplificationFactor);
                    waveformViewer.SetAudioData(currentPcmData);

                    if (oldStart >= 0 && oldEnd >= 0)
                    {
                        waveformViewer.RestoreLoopPoints(oldStart, oldEnd);
                    }

                    // Resume playback if we were playing
                    if (wasPlaying && oldStart >= 0 && oldEnd >= 0)
                    {
                        StartPreview(oldStart, oldEnd);
                    }

                } else if (originalPcmData != null && originalPcmData.Length > 0)
                {
                    // Store current playback state
                    bool wasPlaying = isPlaying;
                    var (oldStart, oldEnd) = waveformViewer.GetLoopPoints();

                    // Stop any current playback
                    if (wasPlaying)
                    {
                        StopPreview();
                    }

                    // Amplify and convert the PCM data
                    currentPcmData = AmplifyAndConvert(originalPcmData, amplificationFactor);

                    // Update waveform display
                    //ProcessSampleRateChange();
                    //Best way to do this  - wasn't working in v1.0 but now it is
                    waveformViewer.SetAudioData(currentPcmData);

                    if (oldStart >= 0 && oldEnd >= 0)
                    {
                        waveformViewer.RestoreLoopPoints(oldStart, oldEnd);
                    }

                    // Resume playback if we were playing
                    if (wasPlaying && oldStart >= 0 && oldEnd >= 0)
                    {
                        StartPreview(oldStart, oldEnd);
                    }
                }
            };

            amplifyPanel.Controls.Add(trackBarAmplify);
            panelWaveform.Controls.Add(amplifyPanel);
        } // Add this method to apply amplification to audio data

        private byte[] AmplifyAndConvert(byte[] pcmData, float amplificationFactor)
        {
            if (originalPcmData == null) return pcmData;

            // Always start from original data and apply all current modifications
            int targetSampleRate = GetSelectedSampleRate();

            return ApplyAllModifications(
                originalPcmData,
                targetSampleRate,
                currentCutRegions,
                amplificationFactor,
                currentEffects
            );
        }
        private static byte Clamp(byte value, byte min, byte max)
        {
            return (byte)Math.Max(min, Math.Min(max, value));
        }

        private void InitializeEffectsPanel()
        {
            audioEffects = new AudioEffectsProcessor(cursorType => SetCustomCursor(cursorType));

            // Create effects panel
            Panel effectsPanel = new Panel
            {
                Location = new Point(panelBottom.Width - 300, 10),
                Size = new Size(280, 200),
                BackColor = Color.FromArgb(180, 190, 210)
            };
            AddBevelToPanel(effectsPanel);

            // Create label
            Label labelEffects = new Label
            {
                Text = "Sound Effects",
                Location = new Point(0, 0),
                AutoSize = true,
                ForeColor = Color.FromArgb(255, 215, 0)
            };
            effectsPanel.Controls.Add(labelEffects);

            // Create effect buttons
            var buttonY = 30;
            CreateEffectButton("Underwater Effect", new Point(10, buttonY), effectsPanel, ApplyUnderwaterEffect);
            CreateEffectButton("Robot Voice", new Point(150, buttonY), effectsPanel, ApplyRobotEffect);

            buttonY += 30;
            CreateEffectButton("High Pitch", new Point(10, buttonY), effectsPanel, ApplyHighPitchEffect);
            CreateEffectButton("Low Pitch", new Point(150, buttonY), effectsPanel, ApplyLowPitchEffect);

            buttonY += 30;
            CreateEffectButton("Echo Effect", new Point(10, buttonY), effectsPanel, ApplyEchoEffect);
            CreateEffectButton("Vocal Remove", new Point(150, buttonY), effectsPanel, ApplyVocalRemovalEffect);

            buttonY += 30;
            CreateEffectButton("Reset Effects", new Point(10, buttonY), effectsPanel, ResetEffects);
            panelBottom.Controls.Add(effectsPanel);
        }

        private void CreateEffectButton(string text, Point location, Panel parent, EventHandler clickHandler)
        {
            RetroButton button = new RetroButton
            {
                Text = text,
                Location = location,
                Size = new Size(120, 30)
            };
            button.Click += clickHandler;
            parent.Controls.Add(button);
        }

        // Effect handlers
        private void ApplyEffect(Func<byte[], int, byte[]> effectFunction, string effectName)
        {
            if (currentPcmData == null) return;
            AddToListBox($"Applying {effectName}...");

            try
            {
                // Store playback state
                bool wasPlaying = isPlaying;
                var (oldLoopStart, oldLoopEnd) = waveformViewer.GetLoopPoints();
                float oldLength = currentPcmData.Length;  // NEW: Store original length

                // Stop playback temporarily
                if (wasPlaying)
                {
                    StopPreview();
                }

                // Create a copy of current data for undo
                byte[] prevData = new byte[currentPcmData.Length];
                Array.Copy(currentPcmData, prevData, currentPcmData.Length);
                PushUndo(prevData);

                // Apply effect
                currentPcmData = effectFunction(currentPcmData, GetSelectedSampleRate());
                waveformViewer.SetAudioData(currentPcmData);

                // Clear redo stack and update UI
                redoStack.Clear();
                UpdateEditButtonStates();

                // Restore loop points if they existed
                // Adjust and restore loop points if they existed
                if (oldLoopStart >= 0 && oldLoopEnd >= 0)
                {
                    float ratio = currentPcmData.Length / oldLength;  // NEW: Calculate scaling ratio
                    int newLoopStart = (int)(oldLoopStart * ratio);  // NEW: Scale start point
                    int newLoopEnd = (int)(oldLoopEnd * ratio);      // NEW: Scale end point
                    waveformViewer.RestoreLoopPoints(newLoopStart, newLoopEnd);
                }

                // Restore playback if it was playing
                if (wasPlaying)
                {
                    if (oldLoopStart >= 0 && oldLoopEnd >= 0)
                    {
                        float ratio = currentPcmData.Length / oldLength;
                        int newLoopStart = (int)(oldLoopStart * ratio);
                        int newLoopEnd = (int)(oldLoopEnd * ratio);
                        StartPreview(newLoopStart, newLoopEnd);
                    }
                    else
                    {
                        StartPreview(0, currentPcmData.Length);
                    }
                }

                AddToListBox($"{effectName} applied");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error applying effect: {ex.Message}", "Effect Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ApplyTrackedEffect(string effectName, Func<byte[]> effectFunction)
        {
            if (currentPcmData == null) return;

            AddToListBox($"Applying {effectName}...");

            try
            {
                bool wasPlaying = isPlaying;
                var (oldLoopStart, oldLoopEnd) = waveformViewer.GetLoopPoints();

                // Store the original audio length BEFORE applying effect
                int originalLength = currentPcmData.Length;

                if (wasPlaying) StopPreview();

                // Create undo point BEFORE modifying anything
                PushUndo(currentPcmData);

                // Add effect to current effects list
                currentEffects.Add(effectName);

                // Apply effect directly to current data
                int targetSampleRate = GetSelectedSampleRate();

                switch (effectName)
                {
                    case "underwater":
                        currentPcmData = audioEffects.ApplyUnderwaterEffect(currentPcmData, targetSampleRate);
                        break;
                    case "robot":
                        currentPcmData = audioEffects.ApplyRobotEffect(currentPcmData, targetSampleRate);
                        break;
                    case "highpitch":
                        currentPcmData = audioEffects.ApplyPitchShift(currentPcmData, targetSampleRate, 1.5f);
                        break;
                    case "lowpitch":
                        currentPcmData = audioEffects.ApplyPitchShift(currentPcmData, targetSampleRate, 0.75f);
                        break;
                    case "echo":
                        currentPcmData = audioEffects.ApplyEchoEffect(currentPcmData, targetSampleRate);
                        break;
                    case "vocal":
                        currentPcmData = audioEffects.ApplyVocalRemoval(currentPcmData, targetSampleRate);
                        break;
                }

                waveformViewer.SetAudioData(currentPcmData);
                redoStack.Clear();
                UpdateEditButtonStates();

                // NEW: Scale loop points if the audio length changed
                if (oldLoopStart >= 0 && oldLoopEnd >= 0)
                {
                    int newLength = currentPcmData.Length;

                    if (newLength != originalLength && originalLength > 0)
                    {
                        // Calculate the scaling ratio
                        float ratio = (float)newLength / originalLength;

                        // Scale the loop points
                        int newLoopStart = (int)(oldLoopStart * ratio);
                        int newLoopEnd = (int)(oldLoopEnd * ratio);

                        // Ensure they're within bounds
                        newLoopStart = Math.Max(0, Math.Min(newLoopStart, newLength - 1));
                        newLoopEnd = Math.Max(newLoopStart + 1, Math.Min(newLoopEnd, newLength));

                        // Restore scaled loop points
                        waveformViewer.RestoreLoopPoints(newLoopStart, newLoopEnd);

                        AddToListBox($"Loop points scaled from {oldLoopStart}-{oldLoopEnd} to {newLoopStart}-{newLoopEnd}");

                        // Resume playback with new loop points if it was playing
                        if (wasPlaying)
                        {
                            StartPreview(newLoopStart, newLoopEnd);
                        }
                    }
                    else
                    {
                        // No length change - restore original loop points
                        waveformViewer.RestoreLoopPoints(oldLoopStart, oldLoopEnd);
                        if (wasPlaying) StartPreview(oldLoopStart, oldLoopEnd);
                    }
                }

                AddToListBox($"{effectName} applied. Audio length: {originalLength} → {currentPcmData.Length}. Total effects: {currentEffects.Count}");
            }
            catch (Exception ex)
            {
                currentEffects.Remove(effectName);
                MessageBox.Show($"Error applying effect: {ex.Message}", "Effect Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ApplyVocalRemovalEffect(object sender, EventArgs e)
        {
            ApplyTrackedEffect("vocal", () => audioEffects.ApplyVocalRemoval(currentPcmData, GetSelectedSampleRate()));
        }

        private void ApplyUnderwaterEffect(object sender, EventArgs e)
        {
            ApplyTrackedEffect("underwater", () => audioEffects.ApplyUnderwaterEffect(currentPcmData, GetSelectedSampleRate()));
        }

        private void ApplyRobotEffect(object sender, EventArgs e)
        {
            ApplyTrackedEffect("robot", () => audioEffects.ApplyRobotEffect(currentPcmData, GetSelectedSampleRate()));
        }

        private void ApplyHighPitchEffect(object sender, EventArgs e)
        {
            ApplyTrackedEffect("highpitch", () => audioEffects.ApplyPitchShift(currentPcmData, GetSelectedSampleRate(), 1.5f));
        }

        private void ApplyLowPitchEffect(object sender, EventArgs e)
        {
            ApplyTrackedEffect("lowpitch", () => audioEffects.ApplyPitchShift(currentPcmData, GetSelectedSampleRate(), 0.75f));
        }

        private void ApplyEchoEffect(object sender, EventArgs e)
        {
            ApplyTrackedEffect("echo", () => audioEffects.ApplyEchoEffect(currentPcmData, GetSelectedSampleRate()));
        }

        private void ResetEffects(object sender, EventArgs e)
        {
            if (originalPcmData == null) return;

            AddToListBox("Resetting effects...");
            SetCustomCursor("busy");

            try
            {
                // Store playback state and original length
                bool wasPlaying = isPlaying;
                var (oldLoopStart, oldLoopEnd) = waveformViewer.GetLoopPoints();
                int originalLength = currentPcmData.Length;

                if (wasPlaying) StopPreview();

                // Create undo point
                PushUndo(currentPcmData);

                // Clear effects but keep cuts and amplification
                currentEffects.Clear();

                // Regenerate audio with current cuts and amplification but no effects
                int targetSampleRate = GetSelectedSampleRate();
                currentPcmData = ApplyAllModifications(
                    originalPcmData,
                    targetSampleRate,
                    currentCutRegions,
                    amplificationFactor,
                    currentEffects
                );

                waveformViewer.SetAudioData(currentPcmData);
                redoStack.Clear();
                UpdateEditButtonStates();

                // Scale loop points if length changed
                if (oldLoopStart >= 0 && oldLoopEnd >= 0)
                {
                    int newLength = currentPcmData.Length;

                    if (newLength != originalLength && originalLength > 0)
                    {
                        float ratio = (float)newLength / originalLength;
                        int newLoopStart = (int)(oldLoopStart * ratio);
                        int newLoopEnd = (int)(oldLoopEnd * ratio);

                        newLoopStart = Math.Max(0, Math.Min(newLoopStart, newLength - 1));
                        newLoopEnd = Math.Max(newLoopStart + 1, Math.Min(newLoopEnd, newLength));

                        waveformViewer.RestoreLoopPoints(newLoopStart, newLoopEnd);

                        if (wasPlaying) StartPreview(newLoopStart, newLoopEnd);
                    }
                    else
                    {
                        waveformViewer.RestoreLoopPoints(oldLoopStart, oldLoopEnd);
                        if (wasPlaying) StartPreview(oldLoopStart, oldLoopEnd);
                    }
                }

                AddToListBox($"Effects reset. Audio length: {originalLength} → {currentPcmData.Length} (cuts and amplification preserved)");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error resetting effects: {ex.Message}", "Reset Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetCustomCursor("normal");
            }
        }

        private void ProcessSampleRateChange()
        {
            StopPreview();
            if (originalPcmData == null && !isRecorded) return;

            try
            {
                int targetSampleRate = GetSelectedSampleRate();
                AddToListBox($"Converting to {targetSampleRate}Hz...");

                // Create undo point BEFORE changing sample rate
                if (currentPcmData != null && undoStack.Count > 0)
                {
                    PushUndo(currentPcmData);
                }

                // ALWAYS start from original data for proper resampling (preserves pitch/speed)
                // But we need to handle cuts differently since they can't be applied to original positions

                // Step 1: Resample original data to target rate
                byte[] resampledOriginal;
                using (var sourceMs = new MemoryStream())
                {
                    using (var writer = new WaveFileWriter(sourceMs, originalFormat))
                    {
                        writer.Write(originalPcmData, 0, originalPcmData.Length);
                        writer.Flush();
                        sourceMs.Position = 0;

                        using (var reader = new WaveFileReader(sourceMs))
                        using (var resampler = new MediaFoundationResampler(reader, new WaveFormat(targetSampleRate, 8, 1)))
                        {
                            resampler.ResamplerQuality = 60;
                            resampledOriginal = GetPCMData(resampler);
                        }
                    }
                }

                // Step 2: If we have cuts, we need to re-apply them at the new sample rate
                byte[] result = resampledOriginal;

                if (currentCutRegions.Count > 0)
                {
                    AddToListBox($"Re-applying {currentCutRegions.Count} cuts at new sample rate...");

                    // Calculate the scaling ratio for cut positions
                    double ratio = originalFormat != null ? (double)targetSampleRate / originalFormat.SampleRate : 1.0;

                    // Apply cuts in reverse order (from end to start) to maintain correct positions
                    var scaledCuts = currentCutRegions
                        .Select(cut => (
                            start: (int)(cut.start * ratio),
                            end: (int)(cut.end * ratio)
                        ))
                        .OrderByDescending(cut => cut.start)
                        .ToList();

                    foreach (var cut in scaledCuts)
                    {
                        int scaledStart = Math.Max(0, Math.Min(cut.start, result.Length));
                        int scaledEnd = Math.Max(scaledStart, Math.Min(cut.end, result.Length));

                        if (scaledEnd > scaledStart)
                        {
                            byte[] newData = new byte[result.Length - (scaledEnd - scaledStart)];
                            Array.Copy(result, 0, newData, 0, scaledStart);
                            Array.Copy(result, scaledEnd, newData, scaledStart, result.Length - scaledEnd);
                            result = newData;
                        }
                    }
                }

                // Step 3: Apply amplification if not default
                if (amplificationFactor != 1.0f)
                {
                    result = waveformProcessor.ApplyAmplification(result, amplificationFactor);
                }

                // Step 4: Apply effects
                foreach (string effect in currentEffects)
                {
                    switch (effect)
                    {
                        case "lowpass":
                            if (checkBoxLowPass.Checked)
                            {
                                float cutoffFrequency = targetSampleRate * 0.45f;
                                result = waveformProcessor.ApplyLowPassFilter(result, targetSampleRate, cutoffFrequency);
                            }
                            break;
                        case "underwater":
                            result = audioEffects.ApplyUnderwaterEffect(result, targetSampleRate);
                            break;
                        case "robot":
                            result = audioEffects.ApplyRobotEffect(result, targetSampleRate);
                            break;
                        case "highpitch":
                            result = audioEffects.ApplyPitchShift(result, targetSampleRate, 1.5f);
                            break;
                        case "lowpitch":
                            result = audioEffects.ApplyPitchShift(result, targetSampleRate, 0.75f);
                            break;
                        case "echo":
                            result = audioEffects.ApplyEchoEffect(result, targetSampleRate);
                            break;
                        case "vocal":
                            result = audioEffects.ApplyVocalRemoval(result, targetSampleRate);
                            break;
                    }
                }

                // Update current data
                currentPcmData = result;

                // Update display
                waveformViewer.SetAudioData(currentPcmData);
                waveformViewer.SetSampleRate(targetSampleRate);

                AddToListBox($"Conversion to {targetSampleRate}Hz complete. Preserved {currentCutRegions.Count} cuts, {currentEffects.Count} effects");
            }
            catch (Exception ex)
            {
                AddToListBox($"Error resampling audio: {ex.Message}");
            }
        }

        // Helper method to determine the current sample rate of the audio
        private int GetCurrentAudioSampleRate()
        {
            // Try to parse the current sample rate from the UI
            string selectedRate = comboBoxSampleRate.Text;
            string sampleRateString = new string(selectedRate.TakeWhile(char.IsDigit).ToArray());

            if (int.TryParse(sampleRateString, out int rate) && rate > 0)
            {
                return rate;
            }

            // Fallback to original format if available
            return originalFormat?.SampleRate ?? 8363;
        }

        private int GetSelectedSampleRate()
        {
            string selectedSampleRate = comboBoxSampleRate.Text;
            string sampleRateString = new string(selectedSampleRate.TakeWhile(char.IsDigit).ToArray());
            return int.TryParse(sampleRateString, out int sampleRate) ? sampleRate : 44100; // Default to 44100 Hz if parsing fails
        }
        private void OnPlaybackStopped(object sender, StoppedEventArgs e)
        {
            if (isPlaying)
            {
                BeginInvoke(new Action(() =>
                {
                    // If we have loop points, restart playback
                    if (currentPreviewStart >= 0 && currentPreviewEnd >= 0)
                    {
                        waveOut?.Play();
                        waveformViewer.SetPlayheadPosition(currentPreviewStart);
                    }
                    else
                    {
                        // For full file preview, restart from beginning
                        waveOut?.Play();
                        waveformViewer.SetPlayheadPosition(0);
                    }
                }));
            }
        }

        private void panel1_DragEnter(object sender, DragEventArgs e)
        {
            SetCustomCursor("hand");
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void panel1_DragDrop(object sender, DragEventArgs e)
        {
            SetCustomCursor("busy");
            try
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                foreach (string file in files)
                {
                    StopPreview();
                    trackBarAmplify.Value = 100;
                    ProcessWaveFile(file);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error processing file: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetCustomCursor("normal");
            }
        }

        private void ProcessWaveFile(string filePath)
        {
            SetCustomCursor("busy");
            try
            {
                ClearAllState();
               // undoStack.Clear();
               // redoStack.Clear();
               // UpdateEditButtonStates();
                lastLoadedFilePath = filePath;

                string extension = Path.GetExtension(filePath).ToLower();

                if (string.IsNullOrEmpty(extension))
                {
                    // Only process files with no extension using STSampleLoader
                    if (STSampleLoader.IsSTSample(filePath))
                    {
                        var info = STSampleLoader.LoadSTSample(filePath);
                        originalPcmData = info.AudioData;
                        originalFormat = new WaveFormat(info.SampleRate, 8, 1);
                        originalSampleRate = info.SampleRate;
                    }
                    else
                    {
                        throw new InvalidOperationException("Unsupported file format for files without extension");
                    }
                }
                else if (extension == ".8svx" || extension == ".iff")
                {
                    // Handle 8SVX and IFF files
                    using (var reader = new BinaryReader(File.OpenRead(filePath)))
                    {
                        string formType = new string(reader.ReadChars(4));
                        if (formType != "FORM")
                            throw new InvalidDataException("Not a valid IFF file");

                        reader.BaseStream.Seek(4, SeekOrigin.Current);
                        string fileType = new string(reader.ReadChars(4));

                        if (fileType != "8SVX")
                            throw new InvalidDataException("Unsupported IFF format - only 8SVX is supported");

                        reader.BaseStream.Seek(0, SeekOrigin.Begin);
                        var info = SVXLoader.Load8SVXFile(filePath);
                        originalPcmData = info.AudioData;
                        originalFormat = new WaveFormat(info.SampleRate, 8, 1);
                        originalSampleRate = info.SampleRate;
                    }
                }
                else if (extension == ".wav")
                {
                    // Handle WAV files
                    using (var reader = new WaveFileReader(filePath))
                    {
                        originalFormat = reader.WaveFormat;
                        originalSampleRate = reader.WaveFormat.SampleRate;
                        originalPcmData = new byte[reader.Length];
                        reader.Read(originalPcmData, 0, originalPcmData.Length);
                    }
                }
                else
                {
                    throw new InvalidOperationException("Unsupported file format");
                }

                // Process for target sample rate
                int targetSampleRate = GetSelectedSampleRate();
                using (var sourceMs = new MemoryStream())
                {
                    using (var writer = new WaveFileWriter(sourceMs, originalFormat))
                    {
                        writer.Write(originalPcmData, 0, originalPcmData.Length);
                        writer.Flush();
                        sourceMs.Position = 0;

                        using (var reader = new WaveFileReader(sourceMs))
                        using (var resampler = new MediaFoundationResampler(reader, new WaveFormat(targetSampleRate, 8, 1)))
                        {
                            resampler.ResamplerQuality = 60;
                            currentPcmData = GetPCMData(resampler);
                        }
                    }
                }

                if (currentPcmData != null)
                {
                    waveformViewer.SetAudioData(currentPcmData);
                    waveformViewer.Invalidate();
                    isRecorded = false;

                    // NOW store the initial state - this should be AFTER currentPcmData is set
                    StoreInitialState();
                }

                AddToListBox($"Loaded file: {Path.GetFileName(filePath)}");
                AddToListBox($"Original sample rate: {originalSampleRate}Hz");
                AddToListBox($"Converted to: {targetSampleRate}Hz");

                if (checkBoxAutoConvert.Checked)
                {
                    ProcessWithCurrentSampleRate();
                    if (checkBoxMoveOriginal.Checked)
                    {
                        MoveOriginalFile(filePath);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error processing file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetCustomCursor("normal");
            }
        }

        private void StoreInitialState()
        {
            if (currentPcmData != null && originalFormat != null)
            {
                // Clear all modification lists
                currentEffects.Clear();
                currentCutRegions.Clear();
                amplificationFactor = 1.0f;
                trackBarAmplify.Value = 100;
                labelAmplify.Text = "Amplify: 100%";

                // Clear existing undo/redo stacks
                undoStack.Clear();
                redoStack.Clear();

                // Store this as the initial state WITH the original sample rate
                var initialState = new AudioState(
                    currentPcmData,
                    originalFormat.SampleRate, // Use ORIGINAL sample rate, not current UI selection
                    currentCutRegions.ToList(),
                    amplificationFactor,
                    currentEffects.ToList()
                );

                undoStack.Push(initialState);
                UpdateEditButtonStates();

                AddToListBox($"Initial clean state stored (Original: {originalFormat.SampleRate}Hz)");
            }
        }

        private void MoveOriginalFile(string filePath)
        {
            try
            {
                string originalFolder = Path.Combine(Path.GetDirectoryName(filePath), "original");
                if (!Directory.Exists(originalFolder))
                {
                    Directory.CreateDirectory(originalFolder);
                }
                string destinationPath = Path.Combine(originalFolder, Path.GetFileName(filePath));
                File.Move(filePath, destinationPath);
                AddToListBox($"Original file moved to: {destinationPath}");
            }
            catch (Exception ex)
            {
                AddToListBox($"Error moving original file: {ex.Message}");
            }
        }

        private void comboBoxSampleRate_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selectedRate = comboBoxSampleRate.Text;
            string sampleRateString = new string(selectedRate.TakeWhile(char.IsDigit).ToArray());

            if (int.TryParse(sampleRateString, out int rate))
            {
                AddToListBox($"Changing sample rate to {rate}Hz...");
            }
            // Process the sample rate change 
            // Stop any current playback and processing
            StopPreview();
            ProcessSampleRateChange();
        }

        private byte[] LoadWaveFile(string filePath)
        {
            const int CHUNK_SIZE = 512 * 1024; // Process in 512KB chunks to avoid ACM limitations
            using (var reader = new WaveFileReader(filePath))
            {
                // Store original format for reference
                originalFormat = reader.WaveFormat;
                originalSampleRate = reader.WaveFormat.SampleRate;

                // If already in correct format, just read the data
                if (reader.WaveFormat.BitsPerSample == 8 && reader.WaveFormat.Channels == 1)
                {
                    byte[] buffer = new byte[reader.Length];
                    int totalBytesRead = 0;
                    while (totalBytesRead < reader.Length)
                    {
                        int bytesRead = reader.Read(buffer, totalBytesRead,
                            Math.Min(CHUNK_SIZE, (int)(reader.Length - totalBytesRead)));
                        if (bytesRead == 0) break;
                        totalBytesRead += bytesRead;
                    }
                    // Add file info to listbox
                    AddToListBox($"Loaded file: {Path.GetFileName(filePath)}");
                    AddToListBox($"Original sample rate: {originalSampleRate}Hz");
                    AddToListBox($"Original format: {originalFormat}");
                    isRecorded = false;
                    return buffer;
                }
                else
                {
                    // For conversion, process in chunks
                    using (var memoryStream = new MemoryStream())
                    {
                        // int offset = 0;
                        // Adjust chunk size to be a multiple of block align
                        int adjustedChunkSize = CHUNK_SIZE - (CHUNK_SIZE % reader.WaveFormat.BlockAlign);
                        byte[] inputBuffer = new byte[adjustedChunkSize];

                        while (true)
                        {
                            // Read a chunk from the original file
                            MemoryStream chunkStream = new MemoryStream();
                            int bytesToRead = Math.Min(adjustedChunkSize, (int)(reader.Length - reader.Position));
                            // Ensure bytesToRead is a multiple of block align
                            bytesToRead = bytesToRead - (bytesToRead % reader.WaveFormat.BlockAlign);

                            if (bytesToRead == 0) break;

                            int bytesRead = reader.Read(inputBuffer, 0, bytesToRead);
                            if (bytesRead == 0) break;

                            chunkStream.Write(inputBuffer, 0, bytesRead);
                            chunkStream.Position = 0;

                            if (reader.WaveFormat.BitsPerSample > 16)
                            {
                                //24-bit Samples and higher handled here instead
                                try
                                {
                                    // First convert to 16-bit stereo as an intermediate step
                                    using (var chunkReader = new RawSourceWaveStream(chunkStream, reader.WaveFormat))
                                    using (var resampler = new MediaFoundationResampler(
                                        chunkReader,
                                        new WaveFormat(reader.WaveFormat.SampleRate, 16, reader.WaveFormat.Channels)))
                                    {
                                        resampler.ResamplerQuality = 60; // High quality conversion
                                        byte[] tempBuffer = new byte[adjustedChunkSize];
                                        //int bytesRead;

                                        while ((bytesRead = resampler.Read(tempBuffer, 0, tempBuffer.Length)) > 0)
                                        {
                                            // Now convert from 16-bit stereo to 8-bit mono
                                            using (var tempStream = new MemoryStream(tempBuffer, 0, bytesRead))
                                            using (var sixteenBitReader = new RawSourceWaveStream(tempStream,
                                                new WaveFormat(reader.WaveFormat.SampleRate, 16, reader.WaveFormat.Channels)))
                                            using (var finalResampler = new MediaFoundationResampler(
                                                sixteenBitReader,
                                                new WaveFormat(reader.WaveFormat.SampleRate, 8, 1)))
                                            {
                                                byte[] convertedBuffer = new byte[adjustedChunkSize];
                                                int convertedBytesRead;

                                                while ((convertedBytesRead = finalResampler.Read(convertedBuffer, 0, convertedBuffer.Length)) > 0)
                                                {
                                                    memoryStream.Write(convertedBuffer, 0, convertedBytesRead);
                                                    isRecorded = false;

                                                }
                                            }
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    AddToListBox($"Error converting 24-bit audio: {ex.Message}");
                                    throw;
                                }
                            }

                            else
                            {
                                // Create a reader and converter for this chunk
                                using (var chunkReader = new RawSourceWaveStream(chunkStream, reader.WaveFormat))



                                using (var conversionStream = new WaveFormatConversionStream(
                                new WaveFormat(reader.WaveFormat.SampleRate, 8, 1), chunkReader))
                                {
                                    byte[] convertedBuffer = new byte[adjustedChunkSize];
                                    int convertedBytesRead;
                                    while ((convertedBytesRead = conversionStream.Read(convertedBuffer, 0, convertedBuffer.Length)) > 0)
                                    {
                                        memoryStream.Write(convertedBuffer, 0, convertedBytesRead);
                                        isRecorded = false;

                                    }
                                }
                            }
                        }
                        // Add file info to listbox
                        AddToListBox($"Loaded file: {Path.GetFileName(filePath)}");
                        AddToListBox($"Original sample rate: {originalSampleRate}Hz");
                        AddToListBox($"Original format: {originalFormat}");
                        isRecorded = false;
                        return memoryStream.ToArray();


                    }
                }
            }
        }
        private void ProcessWithCurrentSampleRate(object sender = null)
        {
            StopPreview();
            try
            {
                string selectedSampleRate = comboBoxSampleRate.Text;
                string sampleRateString = new string(selectedSampleRate.TakeWhile(char.IsDigit).ToArray());
                if (!int.TryParse(sampleRateString, out int targetSampleRate) || targetSampleRate <= 0)
                {
                    MessageBox.Show("Invalid sample rate.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                byte[] pcmData;
                var (oldStart, oldEnd) = waveformViewer.GetLoopPoints();

                if (!string.IsNullOrEmpty(lastLoadedFilePath) && !isRecorded)
                {
                    string ext = Path.GetExtension(lastLoadedFilePath).ToLower();
                    if (ext == ".8svx")
                    {
                        var svxInfo = SVXLoader.Load8SVXFile(lastLoadedFilePath);
                        using (var sourceMs = new MemoryStream())
                        {
                            var format = new WaveFormat(svxInfo.SampleRate, 8, 1);
                            using (var writer = new WaveFileWriter(sourceMs, format))
                            {
                                writer.Write(svxInfo.AudioData, 0, svxInfo.AudioData.Length);
                                writer.Flush();
                                sourceMs.Position = 0;

                                using (var reader = new WaveFileReader(sourceMs))
                                using (var resampler = new MediaFoundationResampler(reader, new WaveFormat(targetSampleRate, 8, 1)))
                                {
                                    resampler.ResamplerQuality = 60;
                                    pcmData = GetPCMData(resampler);
                                }
                            }
                        }
                    }
                    else
                    {
                        using (var reader = new WaveFileReader(lastLoadedFilePath))
                        {
                            using (var resampler = new MediaFoundationResampler(reader, new WaveFormat(targetSampleRate, 8, 1)))
                            {
                                resampler.ResamplerQuality = 60;
                                pcmData = GetPCMData(resampler);
                            }
                        }
                    }
                }
                else if (isRecorded && originalPcmData != null)
                {
                    using (var sourceMs = new MemoryStream())
                    {
                        // Write original data to memory stream
                        var writer = new WaveFileWriter(sourceMs, originalFormat);
                        writer.Write(originalPcmData, 0, originalPcmData.Length);
                        writer.Flush();
                        sourceMs.Position = 0;

                        using (var reader = new WaveFileReader(sourceMs))
                        using (var resampler = new MediaFoundationResampler(reader, new WaveFormat(targetSampleRate, 8, 1)))
                        {
                            resampler.ResamplerQuality = 60;
                            pcmData = GetPCMData(resampler);
                        }
                    }
                }
                else
                {
                    return;
                }

                // Apply effects after resampling
                if (amplificationFactor != 1.0f)
                {
                    pcmData = waveformProcessor.ApplyAmplification(pcmData, amplificationFactor);
                }

                if (checkBoxLowPass.Checked)
                {
                    float cutoffFrequency = targetSampleRate * 0.45f;
                    pcmData = waveformProcessor.ApplyLowPassFilter(pcmData, targetSampleRate, cutoffFrequency);
                }

                // Update current data
                Debug.WriteLine($"Updating currentPcmData. Source length: {currentPcmData.Length}");
                currentPcmData = pcmData;

                Debug.WriteLine($"Updating currentPcmData. Source length: {currentPcmData.Length}");


                // Force waveform display update
                if (waveformViewer != null)
                {
                    waveformViewer.SetAudioData(currentPcmData);
                    waveformViewer.Invalidate();
                    Application.DoEvents(); // Force immediate update
                }

                // Restore loop points if they existed
                if (oldStart >= 0 && oldEnd >= 0)
                {
                    // Calculate new positions based on sample rate change
                    double ratio = (double)targetSampleRate / originalSampleRate;
                    int newStart = (int)(oldStart * ratio);
                    int newEnd = (int)(oldEnd * ratio);

                    waveformViewer.RestoreLoopPoints(newStart, newEnd);
                }

                // Handle auto-save if requested
                if (!isRecorded && !string.IsNullOrEmpty(lastLoadedFilePath) &&
                    (checkBoxAutoConvert.Checked || (sender != null && sender.GetType() == typeof(Button))))
                {
                    SaveProcessedFile(pcmData, lastLoadedFilePath, targetSampleRate);
                }

                AddToListBox($"Resampled to {targetSampleRate}Hz");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error processing audio: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Helper method to reliably get PCM data from a wave provider
        private byte[] GetPCMData(IWaveProvider provider)
        {
            using (var outStream = new MemoryStream())
            {
                byte[] buffer = new byte[4096];
                int read;
                while ((read = provider.Read(buffer, 0, buffer.Length)) > 0)
                {
                    outStream.Write(buffer, 0, read);
                }
                return outStream.ToArray();
            }
        }

        private void checkBoxLowPass_CheckedChanged(object sender, EventArgs e)
        {
            if (isPlaying)
            {
                // Get current loop points and preserve playback
                var (start, end) = waveformViewer.GetLoopPoints();
                if (start >= 0 && end >= 0)
                {
                    StartPreview(start, end);  // This will reprocess the audio with/without filter
                }
                else
                {
                    StartPreview(0, currentPcmData.Length);
                }
            }
        }

        private void SaveAs8SVX(byte[] pcmData, string output8SVXFile, int sampleRate)
        {
            // Get loop points if set
            var (loopStart, loopEnd) = waveformViewer.GetLoopPoints();
            int loopLength = (loopEnd > loopStart) ? loopEnd - loopStart : 0;

            // Convert unsigned PCM (0-255) to signed (-128 to 127)
            byte[] signedPcm = new byte[pcmData.Length];
            for (int i = 0; i < pcmData.Length; i++)
            {
                signedPcm[i] = (byte)(pcmData[i] - 128);
            }

            using (var writer = new BinaryWriter(File.Open(output8SVXFile, FileMode.Create)))
            {
                // Calculate total size including all chunks
                int formSize = 4 + // "8SVX"
                              (8 + 20) + // VHDR chunk
                              (8 + 32) + // ANNO chunk
                              (8 + 4) + // CHAN chunk
                              (8 + signedPcm.Length + (signedPcm.Length % 2)); // BODY chunk with padding

                // FORM Chunk
                writer.Write("FORM".ToCharArray());
                writer.Write(BitConverter.GetBytes(formSize).Reverse().ToArray());
                writer.Write("8SVX".ToCharArray());

                // VHDR Chunk
                writer.Write("VHDR".ToCharArray());
                writer.Write(BitConverter.GetBytes(20).Reverse().ToArray()); // Chunk size
                writer.Write(BitConverter.GetBytes(signedPcm.Length).Reverse().ToArray()); // OneShotHiSamples
                writer.Write(BitConverter.GetBytes(loopLength).Reverse().ToArray()); // RepeatHiSamples
                writer.Write(BitConverter.GetBytes(loopLength).Reverse().ToArray()); // SamplesPerHiCycle
                // Convert sample rate to big-endian bytes
                byte[] sampleRateBytes = BitConverter.GetBytes((ushort)sampleRate).Reverse().ToArray();
                writer.Write(sampleRateBytes); // Sample rate - 2 bytes
                writer.Write((byte)1); // Octaves
                writer.Write((byte)0); // Compression
                writer.Write(new byte[] { 0x00, 0x01, 0x00, 0x00 }); // Volume

                // ANNO Chunk
                writer.Write("ANNO".ToCharArray());
                writer.Write(BitConverter.GetBytes(32).Reverse().ToArray());
                writer.Write(Encoding.ASCII.GetBytes("File created by Sound Exchange  ".PadRight(32)));

                // CHAN Chunk
                writer.Write("CHAN".ToCharArray());
                writer.Write(BitConverter.GetBytes(4).Reverse().ToArray());
                writer.Write(BitConverter.GetBytes(2).Reverse().ToArray());

                // BODY Chunk
                writer.Write("BODY".ToCharArray());
                writer.Write(BitConverter.GetBytes(signedPcm.Length).Reverse().ToArray());
                writer.Write(signedPcm);

                // Add padding byte if needed
                if (signedPcm.Length % 2 != 0)
                {
                    writer.Write((byte)0);
                }
            }
        }
        private void ClearAllState()
        {
            // Clear all modification tracking
            currentEffects.Clear();
            currentCutRegions.Clear();
            amplificationFactor = 1.0f;

            // Reset UI elements
            trackBarAmplify.Value = 100;
            labelAmplify.Text = "Amplify: 100%";

            // Clear undo/redo stacks
            undoStack.Clear();
            redoStack.Clear();
            UpdateEditButtonStates();

            // Clear waveform display
            waveformViewer.Clear();

            // Reset audio data
            currentPcmData = null;
            originalPcmData = null;
            originalFormat = null;

            // Reset flags
            isRecorded = false;
            lastLoadedFilePath = null;

            AddToListBox("State cleared for new session");
        }

        private void SaveProcessedFile(byte[] pcmData, string originalFilePath, int sampleRate)
        {
            try
            {
                SetCustomCursor("busy");
                string directory = Path.GetDirectoryName(originalFilePath);
                string fileName = Path.GetFileNameWithoutExtension(originalFilePath);
                string outputPath;

                if (checkBoxEnable8SVX.Checked)
                {
                    outputPath = Path.Combine(directory, $"{fileName}_{sampleRate}Hz.8svx");
                    SaveAs8SVX(pcmData, outputPath, sampleRate);
                }
                else
                {
                    // For WAV, check if 16-bit is requested
                    outputPath = Path.Combine(directory, $"{fileName}_{sampleRate}Hz.wav");
                    bool use16Bit = checkBox16BitWAV?.Checked ?? false;

                    // Create WAV file with appropriate bit depth
                    WaveFormat format;
                    if (use16Bit)
                    {
                        format = new WaveFormat(sampleRate, 16, 1);
                        // Convert 8-bit PCM data to 16-bit
                        using (var writer = new WaveFileWriter(outputPath, format))
                        {
                            // Convert 8-bit unsigned to 16-bit signed
                            short[] samples16Bit = new short[pcmData.Length];
                            for (int i = 0; i < pcmData.Length; i++)
                            {
                                // Convert 8-bit (0-255) to 16-bit (-32768 to 32767)
                                samples16Bit[i] = (short)((pcmData[i] - 128) * 256);
                            }

                            byte[] buffer = new byte[samples16Bit.Length * 2];
                            Buffer.BlockCopy(samples16Bit, 0, buffer, 0, buffer.Length);
                            writer.Write(buffer, 0, buffer.Length);
                        }
                    }
                    else
                    {
                        // Existing 8-bit code
                        format = new WaveFormat(sampleRate, 8, 1);
                        using (var writer = new WaveFileWriter(outputPath, format))
                        {
                            writer.Write(pcmData, 0, pcmData.Length);
                        }
                    }
                }

                    // Check if file exists
                    if (File.Exists(outputPath))
                {
                    var result = MessageBox.Show(
                        $"File {Path.GetFileName(outputPath)} already exists. Overwrite?",
                        "Confirm Overwrite",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (result == DialogResult.No)
                    {
                        return;
                    }
                }

                AddToListBox($"Saved: {Path.GetFileName(outputPath)}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetCustomCursor("normal");
            }
        }

        private void BtnSaveLoop_Click(object sender, EventArgs e)
        {
            if (currentPcmData == null)
            {
                MessageBox.Show("Please load a file first.", "No File Loaded", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var (start, end) = waveformViewer.GetLoopPoints();
            if (start < 0 || end < 0)
            {
                MessageBox.Show("Please set loop points first.", "No Loop Points", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SaveFileDialog saveDialog = new SaveFileDialog();
            saveDialog.Filter = "WAV files (*.wav)|*.wav|8SVX files (*.8svx)|*.8svx|All files (*.*)|*.*";
            saveDialog.FilterIndex = 1;

            if (!string.IsNullOrEmpty(lastLoadedFilePath))
            {
                saveDialog.InitialDirectory = Path.GetDirectoryName(lastLoadedFilePath);
                saveDialog.FileName = Path.GetFileNameWithoutExtension(lastLoadedFilePath) + "_loop.wav";
            }

            if (saveDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    // Extract just the loop section
                    int loopLength = end - start;
                    byte[] loopData = new byte[loopLength];
                    Array.Copy(currentPcmData, start, loopData, 0, loopLength);

                    string selectedSampleRate = comboBoxSampleRate.Text;
                    string sampleRateString = new string(selectedSampleRate.TakeWhile(char.IsDigit).ToArray());
                    int sampleRate = int.Parse(sampleRateString);

                    string extension = Path.GetExtension(saveDialog.FileName).ToLower();
                    if (extension == ".8svx")
                    {
                        SaveAs8SVX(loopData, saveDialog.FileName, sampleRate);
                        AddToListBox($"Saved loop section as 8SVX: {Path.GetFileName(saveDialog.FileName)}");
                    }
                    else
                    {
                        // Save as WAV file - check if 16-bit is enabled
                        bool use16Bit = checkBox16BitWAV.Checked;

                        if (use16Bit)
                        {
                            // Create 16-bit WAV
                            var format = new WaveFormat(sampleRate, 16, 1);
                            using (var writer = new WaveFileWriter(saveDialog.FileName, format))
                            {
                                // Convert 8-bit unsigned to 16-bit signed
                                short[] samples16Bit = new short[loopData.Length];
                                for (int i = 0; i < loopData.Length; i++)
                                {
                                    // Scale 8-bit range (0-255) to 16-bit range (-32768 to 32767)
                                    samples16Bit[i] = (short)((loopData[i] - 128) * 256);
                                }

                                byte[] buffer = new byte[samples16Bit.Length * 2];
                                Buffer.BlockCopy(samples16Bit, 0, buffer, 0, buffer.Length);
                                writer.Write(buffer, 0, buffer.Length);
                            }
                            AddToListBox($"Saved loop section as 16-bit WAV: {Path.GetFileName(saveDialog.FileName)}");
                        }
                        else
                        {
                            // Save as 8-bit WAV (original code)
                            var format = new WaveFormat(sampleRate, 8, 1);
                            using (var writer = new WaveFileWriter(saveDialog.FileName, format))
                            {
                                writer.Write(loopData, 0, loopData.Length);
                            }
                            AddToListBox($"Saved loop section as 8-bit WAV: {Path.GetFileName(saveDialog.FileName)}");
                        }
                    }

                    MessageBox.Show("Loop section saved successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving loop: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void BtnSaveLoop8SVX_Click(object sender, EventArgs e)
        {
            if (currentPcmData == null)
            {
                MessageBox.Show("Please load a file first.", "No File Loaded", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var (start, end) = waveformViewer.GetLoopPoints();
            if (start < 0 || end < 0)
            {
                MessageBox.Show("Please set loop points first.", "No Loop Points", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SaveFileDialog saveDialog = new SaveFileDialog();
            saveDialog.Filter = "8SVX files (*.8svx)|*.8svx|All files (*.*)|*.*";
            saveDialog.FilterIndex = 1;

            if (!string.IsNullOrEmpty(lastLoadedFilePath))
            {
                saveDialog.InitialDirectory = Path.GetDirectoryName(lastLoadedFilePath);
                saveDialog.FileName = Path.GetFileNameWithoutExtension(lastLoadedFilePath) + ".8svx";
            }

            if (saveDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    string selectedSampleRate = comboBoxSampleRate.Text;
                    string sampleRateString = new string(selectedSampleRate.TakeWhile(char.IsDigit).ToArray());
                    int sampleRate = int.Parse(sampleRateString);

                    SaveAs8SVX(currentPcmData, saveDialog.FileName, sampleRate);
                    AddToListBox($"Saved 8SVX with loop points: {saveDialog.FileName}");
                    MessageBox.Show("File saved with loop points.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Control)
            {
                switch (e.KeyCode)
                {
                    case Keys.Z:
                        if (btnUndo.Enabled) BtnUndo_Click(this, EventArgs.Empty);
                        break;
                    case Keys.Y:
                        if (btnRedo.Enabled) BtnRedo_Click(this, EventArgs.Empty);
                        break;
                    case Keys.X:
                        if (btnCut.Enabled) BtnCut_Click(this, EventArgs.Empty);
                        break;
                    case Keys.Space:
                        if (btnPreviewLoop.Enabled) BtnPreviewLoop_Click(this, EventArgs.Empty);
                        break;
                }
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);

            // Clean up temp file
            if (!string.IsNullOrEmpty(tempSourceFile) && File.Exists(tempSourceFile))
            {
                try
                {
                    File.Delete(tempSourceFile);
                }
                catch { /* Ignore cleanup errors on exit */ }
            }

            // Clean up audio resources
            StopPreview();
        }

        private void ApplyAmigaStyle(Control.ControlCollection controls)
        {
            foreach (Control control in controls)
            {
                if (control is Button button)
                {
                    button.BackColor = Color.FromArgb(180, 190, 210);
                    button.FlatStyle = FlatStyle.Flat;
                    button.FlatAppearance.BorderColor = Color.FromArgb(100, 100, 100);
                    button.Font = FontManager.GetMainFont(9f);
                }
                else if (control is Panel panel)
                {
                    panel.BackColor = Color.FromArgb(180, 190, 210);
                    ApplyAmigaStyle(panel.Controls);
                }
                else if (control is ListBox listBox)
                {
                    listBox.BackColor = Color.Black;
                    listBox.ForeColor = Color.FromArgb(180, 190, 210);
                    listBox.Font = FontManager.GetMainFont(9f);
                }
                else if (control is ComboBox comboBox)
                {
                    comboBox.BackColor = Color.Black;
                    comboBox.ForeColor = Color.FromArgb(180, 190, 210);
                    comboBox.Font = FontManager.GetMainFont(9f);
                }
            }
        }

        private void CreateCheckerboardBackground()
        {
            try
            {
                int tileSize = 32;  // Reduced from 64 to 16
                Bitmap checkerTile = new Bitmap(tileSize, tileSize);
                using (Graphics g = Graphics.FromImage(checkerTile))
                {
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None; // For sharp edges

                    // Base background
                    using (SolidBrush baseBrush = new SolidBrush(Color.FromArgb(90, 100, 140)))
                    {
                        g.FillRectangle(baseBrush, 0, 0, tileSize, tileSize);
                    }

                    // Grid lines
                    using (Pen gridPen = new Pen(Color.FromArgb(128, 74, 85, 128), 1))
                    {
                        // Horizontal lines
                        for (int y = 4; y < tileSize; y += 4)
                        {
                            g.DrawLine(gridPen, 0, y, tileSize, y);
                        }
                        // Vertical lines
                        for (int x = 4; x < tileSize; x += 4)
                        {
                            g.DrawLine(gridPen, x, 0, x, tileSize);
                        }
                    }

                    // Corner squares
                    using (SolidBrush cornerBrush = new SolidBrush(Color.FromArgb(100, 110, 150)))
                    {
                        g.FillRectangle(cornerBrush, 0, 0, 4, 4);
                        g.FillRectangle(cornerBrush, 12, 0, 4, 4);
                        g.FillRectangle(cornerBrush, 0, 12, 4, 4);
                        g.FillRectangle(cornerBrush, 12, 12, 4, 4);
                    }

                    // Inner squares
                    using (SolidBrush accentBrush = new SolidBrush(Color.FromArgb(132, 146, 183)))
                    {
                        g.FillRectangle(accentBrush, 5, 5, 2, 2);
                        g.FillRectangle(accentBrush, 9, 5, 2, 2);
                        g.FillRectangle(accentBrush, 5, 9, 2, 2);
                        g.FillRectangle(accentBrush, 9, 9, 2, 2);
                    }

                    // Center square
                    using (SolidBrush centerBrush = new SolidBrush(Color.FromArgb(74, 85, 128)))
                    {
                        g.FillRectangle(centerBrush, 7, 7, 2, 2);
                    }

                    // Corner brackets
                    using (SolidBrush bracketBrush = new SolidBrush(Color.FromArgb(154, 163, 196)))
                    {
                        // Top-left corner
                        g.FillRectangle(bracketBrush, 0, 0, 2, 1);
                        g.FillRectangle(bracketBrush, 0, 0, 1, 2);

                        // Top-right corner
                        g.FillRectangle(bracketBrush, 14, 0, 2, 1);
                        g.FillRectangle(bracketBrush, 15, 0, 1, 2);

                        // Bottom-left corner
                        g.FillRectangle(bracketBrush, 0, 15, 2, 1);
                        g.FillRectangle(bracketBrush, 0, 14, 1, 2);

                        // Bottom-right corner
                        g.FillRectangle(bracketBrush, 14, 15, 2, 1);
                        g.FillRectangle(bracketBrush, 15, 14, 1, 2);
                    }
                }

                // Set the background
                Image oldBackground = this.BackgroundImage;
                this.BackgroundImage = checkerTile;
                this.BackgroundImageLayout = ImageLayout.Tile;

                if (oldBackground != null)
                {
                    oldBackground.Dispose();
                }
            }
            catch (Exception ex)
            {
                this.BackgroundImage = null;
                this.BackColor = Color.FromArgb(90, 100, 140);
                Debug.WriteLine($"Error creating background: {ex.Message}");
            }
        }

        private void AddBevelToPanel(Panel panel)
        {
            panel.Paint += (s, e) =>
            {
                ControlPaint.DrawBorder3D(e.Graphics, panel.ClientRectangle, Border3DStyle.Raised);
            };
        }

        private void StyleCheckbox(CheckBox checkbox)
        {
            checkbox.FlatStyle = FlatStyle.Flat;
            checkbox.BackColor = Color.FromArgb(60, 70, 100); // Dark blue like in the screenshot
            checkbox.ForeColor = Color.FromArgb(255, 215, 0); // Gold color
            checkbox.UseVisualStyleBackColor = false;
            // Set bold font using your existing FontManager
            checkbox.Font = FontManager.GetMainFont(9f, FontStyle.Bold);
        }

        private void StyleLabels()
        {
            foreach (Control control in this.Controls)
            {
                if (control is Label label)
                {
                    label.ForeColor = Color.FromArgb(255, 215, 0); // Gold text
                    label.Font = FontManager.GetMainFont(9f, FontStyle.Bold);
                    label.BackColor = Color.Transparent;
                }
            }
        }

        private void StyleTrackBar()
        {
            if (trackBarAmplify != null)
            {
                trackBarAmplify.BackColor = Color.FromArgb(180, 190, 210);
                trackBarAmplify.TickStyle = TickStyle.Both;
                trackBarAmplify.TickFrequency = 50;
            }
        }

        private void btnManualConvert_Click_1(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(lastLoadedFilePath) && !isRecorded)
            {
                MessageBox.Show("Please load a file or record audio first.", "No Audio Data", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                if (isRecorded)
                {
                    // For recorded audio, just save the currentPcmData as it's already processed
                    SaveFileDialog saveDialog = new SaveFileDialog();
                    saveDialog.Filter = "WAV files (*.wav)|*.wav|8SVX files (*.8svx)|*.8svx|All files (*.*)|*.*";
                    saveDialog.FilterIndex = 1;

                    if (saveDialog.ShowDialog() == DialogResult.OK)
                    {
                        string selectedSampleRate = comboBoxSampleRate.Text;
                        string sampleRateString = new string(selectedSampleRate.TakeWhile(char.IsDigit).ToArray());
                        int sampleRate = int.TryParse(sampleRateString, out int rate) ? rate : 8363;

                        if (Path.GetExtension(saveDialog.FileName).ToLower() == ".8svx")
                        {
                            SaveAs8SVX(currentPcmData, saveDialog.FileName, sampleRate);
                            AddToListBox($"Saved recorded audio as 8SVX: {Path.GetFileName(saveDialog.FileName)}");
                        }
                        else
                        {
                            // Save as WAV
                            var format = new WaveFormat(sampleRate, 8, 1);
                            using (var writer = new WaveFileWriter(saveDialog.FileName, format))
                            {
                                writer.Write(currentPcmData, 0, currentPcmData.Length);
                            }
                            AddToListBox($"Saved recorded audio as WAV: {Path.GetFileName(saveDialog.FileName)}");
                        }
                    }
                }
                else
                {
                    // For loaded WAV files, use the existing process
                    ProcessWithCurrentSampleRate(sender);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving file: {ex.Message}", "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Custom cursor handling for Drag Panel Area
        private void panel1_MouseEnter(object sender, EventArgs e)
        {
            SetCustomCursor("hand");
        }

        private void panel1_MouseLeave(object sender, EventArgs e)
        {
            SetCustomCursor("normal");
        }

    }
    }

