namespace XUIEditor
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            propertyGrid1 = new PropertyGrid();
            treeView1 = new TreeView();
            btnLoad = new Button();
            btnSave = new Button();
            imageList1 = new ImageList(components);
            chbk_ignoreShow = new CheckBox();
            SuspendLayout();
            // 
            // propertyGrid1
            // 
            propertyGrid1.BackColor = SystemColors.Control;
            propertyGrid1.Location = new Point(270, 50);
            propertyGrid1.Name = "propertyGrid1";
            propertyGrid1.PropertySort = PropertySort.NoSort;
            propertyGrid1.Size = new Size(290, 600);
            propertyGrid1.TabIndex = 1;
            propertyGrid1.PropertyValueChanged += propertyGrid1_PropertyValueChanged;
            // 
            // treeView1
            // 
            treeView1.BorderStyle = BorderStyle.None;
            treeView1.CheckBoxes = true;
            treeView1.Location = new Point(10, 50);
            treeView1.Name = "treeView1";
            treeView1.ShowLines = false;
            treeView1.Size = new Size(250, 600);
            treeView1.TabIndex = 2;
            treeView1.AfterCheck += treeView1_AfterCheck;
            treeView1.AfterSelect += treeView1_AfterSelect;
            // 
            // btnLoad
            // 
            btnLoad.Location = new Point(10, 10);
            btnLoad.Name = "btnLoad";
            btnLoad.Size = new Size(100, 23);
            btnLoad.TabIndex = 3;
            btnLoad.Text = "Load Folder";
            btnLoad.UseVisualStyleBackColor = true;
            btnLoad.Click += btnLoad_Click;
            // 
            // btnSave
            // 
            btnSave.Enabled = false;
            btnSave.Location = new Point(120, 10);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(100, 23);
            btnSave.TabIndex = 4;
            btnSave.Text = "Save";
            btnSave.UseVisualStyleBackColor = true;
            btnSave.Click += btnSave_Click;
            // 
            // imageList1
            // 
            imageList1.ColorDepth = ColorDepth.Depth32Bit;
            imageList1.ImageSize = new Size(64, 64);
            imageList1.TransparentColor = Color.Transparent;
            // 
            // chbk_ignoreShow
            // 
            chbk_ignoreShow.AutoSize = true;
            chbk_ignoreShow.Location = new Point(235, 12);
            chbk_ignoreShow.Name = "chbk_ignoreShow";
            chbk_ignoreShow.Size = new Size(150, 19);
            chbk_ignoreShow.TabIndex = 5;
            chbk_ignoreShow.Text = "Ignore \"Show\" property";
            chbk_ignoreShow.UseVisualStyleBackColor = true;
            chbk_ignoreShow.CheckedChanged += chbk_ignoreShow_CheckedChanged;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(572, 661);
            Controls.Add(chbk_ignoreShow);
            Controls.Add(propertyGrid1);
            Controls.Add(treeView1);
            Controls.Add(btnLoad);
            Controls.Add(btnSave);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            Name = "MainForm";
            Text = "S4 League - XUI Editor by Dekirai";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.PropertyGrid propertyGrid1;
        private System.Windows.Forms.TreeView treeView1;
        private System.Windows.Forms.Button btnLoad;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.ImageList imageList1;
        private CheckBox chbk_ignoreShow;
    }
}
