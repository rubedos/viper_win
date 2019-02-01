namespace ImageStreamWinForms
{
  partial class Form1
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
      this.label1 = new System.Windows.Forms.Label();
      this.uriTextBox = new System.Windows.Forms.TextBox();
      this.button1 = new System.Windows.Forms.Button();
      this.imageBox = new System.Windows.Forms.PictureBox();
      ((System.ComponentModel.ISupportInitialize)(this.imageBox)).BeginInit();
      this.SuspendLayout();
      // 
      // label1
      // 
      this.label1.AutoSize = true;
      this.label1.Location = new System.Drawing.Point(13, 13);
      this.label1.Name = "label1";
      this.label1.Size = new System.Drawing.Size(107, 25);
      this.label1.TabIndex = 0;
      this.label1.Text = "VIPER Uri";
      // 
      // uriTextBox
      // 
      this.uriTextBox.Location = new System.Drawing.Point(182, 6);
      this.uriTextBox.Name = "uriTextBox";
      this.uriTextBox.Size = new System.Drawing.Size(354, 31);
      this.uriTextBox.TabIndex = 1;
      this.uriTextBox.Text = "http://192.168.1.143:11311";
      // 
      // button1
      // 
      this.button1.Location = new System.Drawing.Point(543, 4);
      this.button1.Name = "button1";
      this.button1.Size = new System.Drawing.Size(143, 35);
      this.button1.TabIndex = 2;
      this.button1.Text = "Connect";
      this.button1.UseVisualStyleBackColor = true;
      this.button1.Click += new System.EventHandler(this.Connect_Click);
      // 
      // imageBox
      // 
      this.imageBox.Location = new System.Drawing.Point(13, 55);
      this.imageBox.Name = "imageBox";
      this.imageBox.Size = new System.Drawing.Size(1280, 720);
      this.imageBox.TabIndex = 3;
      this.imageBox.TabStop = false;
      // 
      // Form1
      // 
      this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 25F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.ClientSize = new System.Drawing.Size(1305, 777);
      this.Controls.Add(this.imageBox);
      this.Controls.Add(this.button1);
      this.Controls.Add(this.uriTextBox);
      this.Controls.Add(this.label1);
      this.Name = "Form1";
      this.Text = "Form1";
      this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.Form1_FormClosed);
      ((System.ComponentModel.ISupportInitialize)(this.imageBox)).EndInit();
      this.ResumeLayout(false);
      this.PerformLayout();

    }

    #endregion

    private System.Windows.Forms.Label label1;
    private System.Windows.Forms.TextBox uriTextBox;
    private System.Windows.Forms.Button button1;
    private System.Windows.Forms.PictureBox imageBox;
  }
}

