using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace WavConvert4Amiga
{
    public class PadSlotInfo
    {
        public byte[] AudioData { get; set; }
        public int SampleRate { get; set; }
        public string Name { get; set; }

        public bool HasData => AudioData != null && AudioData.Length > 0;
    }

    public class SamplePadForm : Form
    {
        private readonly Button[] padButtons = new Button[16];
        private readonly char[] keyMap = "1qazxsw23edcvfr4".ToCharArray();
        private readonly Action<int> playSlotAction;
        private readonly Action<int> editSlotAction;
        private readonly Action stopAllAction;
        private readonly bool[] loadedSlots = new bool[16];
        private readonly bool[] playingSlots = new bool[16];

        public SamplePadForm(Action<int> playSlotAction, Action<int> editSlotAction, Action stopAllAction)
        {
            this.playSlotAction = playSlotAction;
            this.editSlotAction = editSlotAction;
            this.stopAllAction = stopAllAction;

            Text = "Sample PAD";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.SizableToolWindow;
            MinimumSize = new Size(360, 360);
            BackColor = Color.FromArgb(180, 190, 210);
            KeyPreview = true;

            var title = new Label
            {
                Text = "PAD 4x4  (Left-click: Play, Right-click: Edit)",
                AutoSize = true,
                Location = new Point(12, 12),
                ForeColor = Color.Black
            };
            Controls.Add(title);

            var stopAllButton = new RetroButton
            {
                Text = "Stop All",
                Size = new Size(90, 24),
                Location = new Point(240, 8),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            stopAllButton.Click += (s, e) => stopAllAction?.Invoke();
            Controls.Add(stopAllButton);

            var table = new TableLayoutPanel
            {
                Location = new Point(12, 36),
                Size = new Size(320, 280),
                ColumnCount = 4,
                RowCount = 4,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.FromArgb(140, 150, 170),
                Padding = new Padding(4)
            };

            for (int i = 0; i < 4; i++)
            {
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
                table.RowStyles.Add(new RowStyle(SizeType.Percent, 25f));
            }

            for (int slot = 0; slot < 16; slot++)
            {
                int capturedSlot = slot;
                var button = new RetroButton
                {
                    Dock = DockStyle.Fill,
                    Margin = new Padding(4),
                    Text = GetDefaultSlotLabel(slot),
                    Tag = slot
                };

                button.MouseDown += (s, e) =>
                {
                    if (e.Button == MouseButtons.Right)
                    {
                        editSlotAction?.Invoke(capturedSlot);
                        return;
                    }

                    if (e.Button == MouseButtons.Left)
                    {
                        TriggerSlot(capturedSlot);
                    }
                };

                padButtons[slot] = button;
                table.Controls.Add(button, slot % 4, slot / 4);
            }

            Controls.Add(table);
            Resize += (s, e) =>
            {
                table.Size = new Size(ClientSize.Width - 24, ClientSize.Height - 48);
            };

            KeyDown += SamplePadForm_KeyDown;
        }

        public void RefreshSlots(PadSlotInfo[] slots)
        {
            for (int i = 0; i < padButtons.Length; i++)
            {
                var slot = slots != null && i < slots.Length ? slots[i] : null;
                bool hasData = slot != null && slot.HasData;
                loadedSlots[i] = hasData;
                string name = hasData ? (slot.Name ?? "Sample") : "(empty)";
                string keyLabel = char.ToUpperInvariant(keyMap[i]).ToString();

                padButtons[i].Text = $"{i + 1} [{keyLabel}]\n{name}";
                ApplyPadVisual(i);
            }
        }

        public void SetPadPlaying(int slot, bool isPlaying)
        {
            if (slot < 0 || slot >= padButtons.Length)
            {
                return;
            }

            playingSlots[slot] = isPlaying;
            ApplyPadVisual(slot);
        }

        private void SamplePadForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control || e.Alt)
            {
                return;
            }

            char keyChar = GetKeyChar(e.KeyCode);
            int slot = Array.FindIndex(keyMap, k => k == keyChar);
            if (slot >= 0)
            {
                TriggerSlot(slot);
                e.Handled = true;
            }
        }

        private static char GetKeyChar(Keys key)
        {
            string text = key.ToString();
            if (text.StartsWith("D") && text.Length == 2 && char.IsDigit(text[1]))
            {
                return char.ToLowerInvariant(text[1]);
            }

            if (text.Length == 1 && char.IsLetterOrDigit(text[0]))
            {
                return char.ToLowerInvariant(text[0]);
            }

            return '\0';
        }

        private void TriggerSlot(int slot)
        {
            playSlotAction?.Invoke(slot);
        }

        private void ApplyPadVisual(int slot)
        {
            bool hasData = loadedSlots[slot];
            bool isPlaying = playingSlots[slot];
            var button = padButtons[slot];

            button.Enabled = hasData;

            if (!hasData)
            {
                button.BackColor = Color.FromArgb(140, 145, 160);
                button.ForeColor = Color.FromArgb(90, 95, 110);
                return;
            }

            button.ForeColor = Color.Black;
            button.BackColor = isPlaying ? Color.FromArgb(255, 215, 0) : Color.FromArgb(210, 220, 240);
        }

        private string GetDefaultSlotLabel(int slot)
        {
            string keyLabel = char.ToUpperInvariant(keyMap[slot]).ToString();
            return $"{slot + 1} [{keyLabel}]\n(empty)";
        }
    }
}
