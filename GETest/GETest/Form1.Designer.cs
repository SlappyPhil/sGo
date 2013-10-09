namespace GETest
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
            this.geckoWebBrowser1 = new Skybound.Gecko.GeckoWebBrowser();
            this.tb_Gestures = new System.Windows.Forms.TextBox();
            this.lbl_Gestures = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // geckoWebBrowser1
            // 
            this.geckoWebBrowser1.Dock = System.Windows.Forms.DockStyle.Right;
            this.geckoWebBrowser1.Location = new System.Drawing.Point(184, 0);
            this.geckoWebBrowser1.Name = "geckoWebBrowser1";
            this.geckoWebBrowser1.Size = new System.Drawing.Size(700, 502);
            this.geckoWebBrowser1.TabIndex = 0;
            this.geckoWebBrowser1.Click += new System.EventHandler(this.geckoWebBrowser1_Click);
            this.geckoWebBrowser1.KeyDown += new System.Windows.Forms.KeyEventHandler(this.geckoWebBrowser1_KeyDown);
            this.geckoWebBrowser1.MouseUp += new System.Windows.Forms.MouseEventHandler(this.geckoWebBrowser1_MouseUp);
            // 
            // tb_Gestures
            // 
            this.tb_Gestures.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.tb_Gestures.Location = new System.Drawing.Point(0, 295);
            this.tb_Gestures.Multiline = true;
            this.tb_Gestures.Name = "tb_Gestures";
            this.tb_Gestures.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.tb_Gestures.Size = new System.Drawing.Size(184, 207);
            this.tb_Gestures.TabIndex = 2;
            this.tb_Gestures.Enter += new System.EventHandler(this.tb_Gestures_Enter);
            // 
            // lbl_Gestures
            // 
            this.lbl_Gestures.AutoSize = true;
            this.lbl_Gestures.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.lbl_Gestures.Location = new System.Drawing.Point(0, 282);
            this.lbl_Gestures.Name = "lbl_Gestures";
            this.lbl_Gestures.Padding = new System.Windows.Forms.Padding(0, 0, 104, 0);
            this.lbl_Gestures.Size = new System.Drawing.Size(184, 13);
            this.lbl_Gestures.TabIndex = 3;
            this.lbl_Gestures.Text = "Gestures Used:";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(884, 502);
            this.Controls.Add(this.lbl_Gestures);
            this.Controls.Add(this.tb_Gestures);
            this.Controls.Add(this.geckoWebBrowser1);
            this.IsMdiContainer = true;
            this.Name = "MainForm";
            this.Text = "sGo";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Skybound.Gecko.GeckoWebBrowser geckoWebBrowser1;
        private System.Windows.Forms.TextBox tb_Gestures;
        private System.Windows.Forms.Label lbl_Gestures;
    }
}

