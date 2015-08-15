namespace RealMOL
{
    partial class Form_Main
    {
        /// <summary>
        /// Variable del diseñador requerida.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Limpiar los recursos que se estén utilizando.
        /// </summary>
        /// <param name="disposing">true si los recursos administrados se deben desechar; false en caso contrario.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Código generado por el Diseñador de Windows Forms

        /// <summary>
        /// Método necesario para admitir el Diseñador. No se puede modificar
        /// el contenido del método con el editor de código.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.pictureBox_Logo = new System.Windows.Forms.PictureBox();
            this.label_Description = new System.Windows.Forms.Label();
            this.button_Launch = new System.Windows.Forms.Button();
            this.label_Detected = new System.Windows.Forms.Label();
            this.button_LoadDevices = new System.Windows.Forms.Button();
            this.timer_ControlPyMOL = new System.Windows.Forms.Timer(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox_Logo)).BeginInit();
            this.SuspendLayout();
            // 
            // pictureBox_Logo
            // 
            this.pictureBox_Logo.Location = new System.Drawing.Point(12, 12);
            this.pictureBox_Logo.Name = "pictureBox_Logo";
            this.pictureBox_Logo.Size = new System.Drawing.Size(128, 128);
            this.pictureBox_Logo.TabIndex = 0;
            this.pictureBox_Logo.TabStop = false;
            // 
            // label_Description
            // 
            this.label_Description.Font = new System.Drawing.Font("Segoe UI Semibold", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label_Description.Location = new System.Drawing.Point(146, 12);
            this.label_Description.Name = "label_Description";
            this.label_Description.Size = new System.Drawing.Size(226, 34);
            this.label_Description.TabIndex = 1;
            this.label_Description.Text = "Aplicación NUI con realidad virtual para visualización molecular";
            this.label_Description.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // button_Launch
            // 
            this.button_Launch.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.button_Launch.Location = new System.Drawing.Point(262, 169);
            this.button_Launch.Name = "button_Launch";
            this.button_Launch.Size = new System.Drawing.Size(110, 30);
            this.button_Launch.TabIndex = 2;
            this.button_Launch.Text = "¡Iniciar!";
            this.button_Launch.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.button_Launch.UseVisualStyleBackColor = true;
            this.button_Launch.Click += new System.EventHandler(this.button_Launch_Click);
            // 
            // label_Detected
            // 
            this.label_Detected.Location = new System.Drawing.Point(146, 56);
            this.label_Detected.Name = "label_Detected";
            this.label_Detected.Size = new System.Drawing.Size(226, 110);
            this.label_Detected.TabIndex = 3;
            this.label_Detected.Text = "Dispositivos detectados:";
            // 
            // button_LoadDevices
            // 
            this.button_LoadDevices.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.button_LoadDevices.Location = new System.Drawing.Point(96, 169);
            this.button_LoadDevices.Name = "button_LoadDevices";
            this.button_LoadDevices.Size = new System.Drawing.Size(160, 30);
            this.button_LoadDevices.TabIndex = 4;
            this.button_LoadDevices.Text = "Cargar dispositivos";
            this.button_LoadDevices.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.button_LoadDevices.UseVisualStyleBackColor = true;
            this.button_LoadDevices.Click += new System.EventHandler(this.button_LoadDevices_Click);
            // 
            // timer_ControlPyMOL
            // 
            this.timer_ControlPyMOL.Interval = 40;
            this.timer_ControlPyMOL.Tick += new System.EventHandler(this.timer_ControlPyMOL_Tick);
            // 
            // Form_Main
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(384, 211);
            this.Controls.Add(this.button_LoadDevices);
            this.Controls.Add(this.label_Detected);
            this.Controls.Add(this.button_Launch);
            this.Controls.Add(this.label_Description);
            this.Controls.Add(this.pictureBox_Logo);
            this.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.MaximizeBox = false;
            this.Name = "Form_Main";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "RealMOL";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form_Main_FormClosing);
            this.Load += new System.EventHandler(this.Form_Main_Load);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox_Logo)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.PictureBox pictureBox_Logo;
        private System.Windows.Forms.Label label_Description;
        private System.Windows.Forms.Button button_Launch;
        private System.Windows.Forms.Label label_Detected;
        private System.Windows.Forms.Button button_LoadDevices;
        private System.Windows.Forms.Timer timer_ControlPyMOL;
    }
}

