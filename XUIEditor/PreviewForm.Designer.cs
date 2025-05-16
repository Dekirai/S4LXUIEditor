namespace XUIEditor
{
    partial class PreviewForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PreviewForm));
            previewPanel = new Panel();
            txtLog = new TextBox();
            SuspendLayout();
            // 
            // previewPanel
            // 
            previewPanel.AutoScroll = true;
            previewPanel.Location = new Point(12, 12);
            previewPanel.Name = "previewPanel";
            previewPanel.Size = new Size(1285, 649);
            previewPanel.TabIndex = 0;
            previewPanel.Paint += PreviewPanel_Paint;
            previewPanel.MouseDown += PreviewPanel_MouseDown;
            previewPanel.MouseLeave += PreviewPanel_MouseLeave;
            previewPanel.MouseMove += PreviewPanel_MouseMove;
            previewPanel.MouseUp += PreviewPanel_MouseUp;
            // 
            // txtLog
            // 
            txtLog.Location = new Point(12, 667);
            txtLog.Multiline = true;
            txtLog.Name = "txtLog";
            txtLog.ReadOnly = true;
            txtLog.ScrollBars = ScrollBars.Vertical;
            txtLog.Size = new Size(1285, 117);
            txtLog.TabIndex = 1;
            // 
            // PreviewForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1309, 796);
            Controls.Add(txtLog);
            Controls.Add(previewPanel);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "PreviewForm";
            Text = "XUI Preview";
            KeyDown += PreviewForm_KeyDown;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Panel previewPanel;
        private TextBox txtLog;
    }
}
