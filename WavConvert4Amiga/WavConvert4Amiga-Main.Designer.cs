namespace WavConvert4Amiga
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.label1 = new System.Windows.Forms.Label();
            this.panel1 = new System.Windows.Forms.Panel();
            this.label2 = new System.Windows.Forms.Label();
            this.listBoxFiles = new System.Windows.Forms.ListBox();
            this.comboBoxSampleRate = new System.Windows.Forms.ComboBox();
            this.checkBoxEnable8SVX = new System.Windows.Forms.CheckBox();
            this.checkBoxMoveOriginal = new System.Windows.Forms.CheckBox();
            this.panelWaveform = new System.Windows.Forms.Panel();
            this.checkBoxLowPass = new System.Windows.Forms.CheckBox();
            this.checkBoxAutoConvert = new System.Windows.Forms.CheckBox();
            this.btnManualConvert = new System.Windows.Forms.Button();
            this.panelBottom = new System.Windows.Forms.Panel();
            this.dataGridViewQueue = new System.Windows.Forms.DataGridView();
            this.btnQueueAddFiles = new System.Windows.Forms.Button();
            this.btnQueueStart = new System.Windows.Forms.Button();
            this.btnQueueStop = new System.Windows.Forms.Button();
            this.btnQueueClearCompleted = new System.Windows.Forms.Button();
            this.checkBox16BitWAV = new System.Windows.Forms.CheckBox();
            this.panel1.SuspendLayout();
            this.panelBottom.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewQueue)).BeginInit();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(19, 11);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(86, 16);
            this.label1.TabIndex = 1;
            this.label1.Text = "Sample Rate";
            // 
            // panel1
            // 
            this.panel1.AllowDrop = true;
            this.panel1.BackgroundImage = global::WavConvert4Amiga.Properties.Resources.WC4A_icon;
            this.panel1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.panel1.Controls.Add(this.label2);
            this.panel1.Location = new System.Drawing.Point(1013, 526);
            this.panel1.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(478, 324);
            this.panel1.TabIndex = 2;
            this.panel1.DragDrop += new System.Windows.Forms.DragEventHandler(this.panel1_DragDrop);
            this.panel1.DragEnter += new System.Windows.Forms.DragEventHandler(this.panel1_DragEnter);
            this.panel1.MouseEnter += new System.EventHandler(this.panel1_MouseEnter);
            this.panel1.MouseLeave += new System.EventHandler(this.panel1_MouseLeave);
            this.panel1.MouseHover += new System.EventHandler(this.panel1_MouseEnter);
            this.panel1.MouseMove += new System.Windows.Forms.MouseEventHandler(this.panel1_MouseEnter);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Minecraft Ten", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(7, 0);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(422, 24);
            this.label2.TabIndex = 0;
            this.label2.Text = "Drop WAV Files Here / Click to Load";
            this.label2.MouseEnter += new System.EventHandler(this.panel1_MouseEnter);
            this.label2.MouseLeave += new System.EventHandler(this.panel1_MouseLeave);
            this.label2.MouseHover += new System.EventHandler(this.panel1_MouseEnter);
            this.label2.MouseMove += new System.Windows.Forms.MouseEventHandler(this.panel1_MouseEnter);
            // 
            // listBoxFiles
            // 
            this.listBoxFiles.FormattingEnabled = true;
            this.listBoxFiles.ItemHeight = 16;
            this.listBoxFiles.Location = new System.Drawing.Point(22, 526);
            this.listBoxFiles.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.listBoxFiles.Name = "listBoxFiles";
            this.listBoxFiles.Size = new System.Drawing.Size(1069, 64);
            this.listBoxFiles.TabIndex = 4;
            // 
            // comboBoxSampleRate
            // 
            this.comboBoxSampleRate.FormattingEnabled = true;
            this.comboBoxSampleRate.Location = new System.Drawing.Point(115, 7);
            this.comboBoxSampleRate.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.comboBoxSampleRate.Name = "comboBoxSampleRate";
            this.comboBoxSampleRate.Size = new System.Drawing.Size(260, 24);
            this.comboBoxSampleRate.TabIndex = 4;
            this.comboBoxSampleRate.SelectedValueChanged += new System.EventHandler(this.comboBoxSampleRate_SelectedIndexChanged);
            // 
            // checkBoxEnable8SVX
            // 
            this.checkBoxEnable8SVX.AutoSize = true;
            this.checkBoxEnable8SVX.BackColor = System.Drawing.SystemColors.Control;
            this.checkBoxEnable8SVX.Location = new System.Drawing.Point(927, 9);
            this.checkBoxEnable8SVX.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.checkBoxEnable8SVX.Name = "checkBoxEnable8SVX";
            this.checkBoxEnable8SVX.Size = new System.Drawing.Size(62, 20);
            this.checkBoxEnable8SVX.TabIndex = 5;
            this.checkBoxEnable8SVX.Text = "8SVX";
            this.checkBoxEnable8SVX.UseVisualStyleBackColor = true;
            // 
            // checkBoxMoveOriginal
            // 
            this.checkBoxMoveOriginal.AutoSize = true;
            this.checkBoxMoveOriginal.Location = new System.Drawing.Point(1025, 50);
            this.checkBoxMoveOriginal.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.checkBoxMoveOriginal.Name = "checkBoxMoveOriginal";
            this.checkBoxMoveOriginal.Size = new System.Drawing.Size(137, 20);
            this.checkBoxMoveOriginal.TabIndex = 6;
            this.checkBoxMoveOriginal.Text = "Move Original File";
            this.checkBoxMoveOriginal.UseVisualStyleBackColor = true;
            // 
            // panelWaveform
            // 
            this.panelWaveform.Location = new System.Drawing.Point(22, 76);
            this.panelWaveform.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.panelWaveform.Name = "panelWaveform";
            this.panelWaveform.Size = new System.Drawing.Size(1469, 443);
            this.panelWaveform.TabIndex = 8;
            this.panelWaveform.Visible = false;
            // 
            // checkBoxLowPass
            // 
            this.checkBoxLowPass.AutoSize = true;
            this.checkBoxLowPass.BackColor = System.Drawing.SystemColors.Control;
            this.checkBoxLowPass.Location = new System.Drawing.Point(675, 10);
            this.checkBoxLowPass.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.checkBoxLowPass.Name = "checkBoxLowPass";
            this.checkBoxLowPass.Size = new System.Drawing.Size(119, 20);
            this.checkBoxLowPass.TabIndex = 9;
            this.checkBoxLowPass.Text = "Low Pass Filter";
            this.checkBoxLowPass.UseVisualStyleBackColor = false;
            // 
            // checkBoxAutoConvert
            // 
            this.checkBoxAutoConvert.AutoSize = true;
            this.checkBoxAutoConvert.BackColor = System.Drawing.SystemColors.Control;
            this.checkBoxAutoConvert.Location = new System.Drawing.Point(906, 50);
            this.checkBoxAutoConvert.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.checkBoxAutoConvert.Name = "checkBoxAutoConvert";
            this.checkBoxAutoConvert.Size = new System.Drawing.Size(105, 20);
            this.checkBoxAutoConvert.TabIndex = 10;
            this.checkBoxAutoConvert.Text = "Auto Convert";
            this.checkBoxAutoConvert.UseVisualStyleBackColor = true;
            // 
            // btnManualConvert
            // 
            this.btnManualConvert.Location = new System.Drawing.Point(22, 42);
            this.btnManualConvert.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btnManualConvert.Name = "btnManualConvert";
            this.btnManualConvert.Size = new System.Drawing.Size(191, 27);
            this.btnManualConvert.TabIndex = 11;
            this.btnManualConvert.Text = "Convert Current";
            this.btnManualConvert.UseVisualStyleBackColor = true;
            this.btnManualConvert.Click += new System.EventHandler(this.btnManualConvert_Click_1);
            // 
            // panelBottom
            // 
            this.panelBottom.Controls.Add(this.dataGridViewQueue);
            this.panelBottom.Location = new System.Drawing.Point(26, 788);
            this.panelBottom.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.panelBottom.Name = "panelBottom";
            this.panelBottom.Size = new System.Drawing.Size(950, 220);
            this.panelBottom.TabIndex = 12;
            // 
            // dataGridViewQueue
            // 
            this.dataGridViewQueue.AllowUserToAddRows = false;
            this.dataGridViewQueue.AllowUserToDeleteRows = false;
            this.dataGridViewQueue.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridViewQueue.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dataGridViewQueue.Location = new System.Drawing.Point(25, 728);
            this.dataGridViewQueue.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.dataGridViewQueue.Name = "dataGridViewQueue";
            this.dataGridViewQueue.ReadOnly = true;
            this.dataGridViewQueue.RowHeadersVisible = false;
            this.dataGridViewQueue.RowHeadersWidth = 62;
            this.dataGridViewQueue.RowTemplate.Height = 28;
            this.dataGridViewQueue.Size = new System.Drawing.Size(1069, 54);
            this.dataGridViewQueue.TabIndex = 0;
            // 
            // btnQueueAddFiles
            // 
            this.btnQueueAddFiles.Location = new System.Drawing.Point(246, 52);
            this.btnQueueAddFiles.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btnQueueAddFiles.Name = "btnQueueAddFiles";
            this.btnQueueAddFiles.Size = new System.Drawing.Size(145, 34);
            this.btnQueueAddFiles.TabIndex = 12;
            this.btnQueueAddFiles.Text = "Add Files...";
            this.btnQueueAddFiles.UseVisualStyleBackColor = true;
            this.btnQueueAddFiles.Click += new System.EventHandler(this.btnQueueAddFiles_Click);
            // 
            // btnQueueStart
            // 
            this.btnQueueStart.Location = new System.Drawing.Point(397, 52);
            this.btnQueueStart.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btnQueueStart.Name = "btnQueueStart";
            this.btnQueueStart.Size = new System.Drawing.Size(145, 34);
            this.btnQueueStart.TabIndex = 13;
            this.btnQueueStart.Text = "Start Queue";
            this.btnQueueStart.UseVisualStyleBackColor = true;
            this.btnQueueStart.Click += new System.EventHandler(this.btnQueueStart_Click);
            // 
            // btnQueueStop
            // 
            this.btnQueueStop.Location = new System.Drawing.Point(548, 52);
            this.btnQueueStop.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btnQueueStop.Name = "btnQueueStop";
            this.btnQueueStop.Size = new System.Drawing.Size(145, 34);
            this.btnQueueStop.TabIndex = 14;
            this.btnQueueStop.Text = "Pause/Stop";
            this.btnQueueStop.UseVisualStyleBackColor = true;
            this.btnQueueStop.Click += new System.EventHandler(this.btnQueueStop_Click);
            // 
            // btnQueueClearCompleted
            // 
            this.btnQueueClearCompleted.Location = new System.Drawing.Point(699, 52);
            this.btnQueueClearCompleted.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btnQueueClearCompleted.Name = "btnQueueClearCompleted";
            this.btnQueueClearCompleted.Size = new System.Drawing.Size(170, 34);
            this.btnQueueClearCompleted.TabIndex = 15;
            this.btnQueueClearCompleted.Text = "Clear Completed";
            this.btnQueueClearCompleted.UseVisualStyleBackColor = true;
            this.btnQueueClearCompleted.Click += new System.EventHandler(this.btnQueueClearCompleted_Click);
            // 
            // checkBox16BitWAV
            // 
            this.checkBox16BitWAV.AutoSize = true;
            this.checkBox16BitWAV.Location = new System.Drawing.Point(826, 9);
            this.checkBox16BitWAV.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.checkBox16BitWAV.Name = "checkBox16BitWAV";
            this.checkBox16BitWAV.Size = new System.Drawing.Size(95, 20);
            this.checkBox16BitWAV.TabIndex = 11;
            this.checkBox16BitWAV.Text = "16-bit WAV";
            this.checkBox16BitWAV.UseVisualStyleBackColor = true;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.ClientSize = new System.Drawing.Size(1507, 844);
            this.Controls.Add(this.btnQueueStart);
            this.Controls.Add(this.panelBottom);
            this.Controls.Add(this.dataGridViewQueue);
            this.Controls.Add(this.btnQueueClearCompleted);
            this.Controls.Add(this.btnQueueStop);
            this.Controls.Add(this.btnQueueStart);
            this.Controls.Add(this.btnQueueAddFiles);
            this.Controls.Add(this.btnManualConvert);
            this.Controls.Add(this.panelWaveform);
            this.Controls.Add(this.listBoxFiles);
            this.Controls.Add(this.comboBoxSampleRate);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.checkBoxMoveOriginal);
            this.Controls.Add(this.checkBoxAutoConvert);
            this.Controls.Add(this.checkBoxLowPass);
            this.Controls.Add(this.checkBoxEnable8SVX);
            this.Controls.Add(this.checkBox16BitWAV);
            this.DoubleBuffered = true;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.Text = "SoundConvert4Amiga";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.panelBottom.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewQueue)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.ListBox listBoxFiles;
        private System.Windows.Forms.ComboBox comboBoxSampleRate;
        private System.Windows.Forms.CheckBox checkBoxEnable8SVX;
        private System.Windows.Forms.CheckBox checkBoxMoveOriginal;
        private System.Windows.Forms.Panel panelWaveform;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.CheckBox checkBoxLowPass;
        private System.Windows.Forms.CheckBox checkBoxAutoConvert;
        private System.Windows.Forms.Button btnManualConvert;
        private System.Windows.Forms.Panel panelBottom;
        private System.Windows.Forms.DataGridView dataGridViewQueue;
        private System.Windows.Forms.Button btnQueueAddFiles;
        private System.Windows.Forms.Button btnQueueStart;
        private System.Windows.Forms.Button btnQueueStop;
        private System.Windows.Forms.Button btnQueueClearCompleted;
    }
}
