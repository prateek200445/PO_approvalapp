namespace ERP
{
    partial class frmPrintDomesticPrice
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
            this.components = new System.ComponentModel.Container();
            this.label1 = new System.Windows.Forms.Label();
            this.crystalReportViewer1 = new CrystalDecisions.Windows.Forms.CrystalReportViewer();
            this.button1 = new System.Windows.Forms.Button();
            this.comboInvoice = new System.Windows.Forms.ComboBox();
            this.label33 = new System.Windows.Forms.Label();
            this.btnPrint = new System.Windows.Forms.Button();
            this.comboCompanyName = new System.Windows.Forms.ComboBox();
            this.label66 = new System.Windows.Forms.Label();
            this.button2 = new System.Windows.Forms.Button();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.comboyr = new System.Windows.Forms.ComboBox();
            this.button3 = new System.Windows.Forms.Button();
            this.cmbinv2 = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.button4 = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(330, 19);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(62, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Invoice No.";
            // 
            // crystalReportViewer1
            // 
            this.crystalReportViewer1.ActiveViewIndex = -1;
            this.crystalReportViewer1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.crystalReportViewer1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.crystalReportViewer1.Cursor = System.Windows.Forms.Cursors.Default;
            this.crystalReportViewer1.Location = new System.Drawing.Point(12, 74);
            this.crystalReportViewer1.Name = "crystalReportViewer1";
            this.crystalReportViewer1.SelectionFormula = "";
            this.crystalReportViewer1.ShowExportButton = false;
            this.crystalReportViewer1.Size = new System.Drawing.Size(996, 545);
            this.crystalReportViewer1.TabIndex = 2;
            this.crystalReportViewer1.ToolPanelView = CrystalDecisions.Windows.Forms.ToolPanelViewType.None;
            this.crystalReportViewer1.ViewTimeSelectionFormula = "";
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(806, 14);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 3;
            this.button1.Text = "OK";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // comboInvoice
            // 
            this.comboInvoice.FormattingEnabled = true;
            this.comboInvoice.Location = new System.Drawing.Point(398, 15);
            this.comboInvoice.Name = "comboInvoice";
            this.comboInvoice.Size = new System.Drawing.Size(108, 21);
            this.comboInvoice.TabIndex = 4;
            // 
            // label33
            // 
            this.label33.AutoSize = true;
            this.label33.Location = new System.Drawing.Point(670, 19);
            this.label33.Name = "label33";
            this.label33.Size = new System.Drawing.Size(33, 13);
            this.label33.TabIndex = 5;
            this.label33.Text = "Suffix";
            // 
            // btnPrint
            // 
            this.btnPrint.Location = new System.Drawing.Point(42, 75);
            this.btnPrint.Name = "btnPrint";
            this.btnPrint.Size = new System.Drawing.Size(26, 27);
            this.btnPrint.TabIndex = 7;
            this.btnPrint.Text = "P";
            this.toolTip1.SetToolTip(this.btnPrint, "Print");
            this.btnPrint.UseVisualStyleBackColor = true;
            this.btnPrint.Click += new System.EventHandler(this.btnPrint_Click);
            // 
            // comboCompanyName
            // 
            this.comboCompanyName.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboCompanyName.FormattingEnabled = true;
            this.comboCompanyName.Location = new System.Drawing.Point(110, 15);
            this.comboCompanyName.Name = "comboCompanyName";
            this.comboCompanyName.Size = new System.Drawing.Size(211, 21);
            this.comboCompanyName.TabIndex = 135;
            this.comboCompanyName.SelectedIndexChanged += new System.EventHandler(this.comboCompanyName_SelectedIndexChanged);
            // 
            // label66
            // 
            this.label66.AutoSize = true;
            this.label66.Location = new System.Drawing.Point(16, 19);
            this.label66.Name = "label66";
            this.label66.Size = new System.Drawing.Size(85, 13);
            this.label66.TabIndex = 136;
            this.label66.Text = "Company Name:";
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(15, 75);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(24, 27);
            this.button2.TabIndex = 137;
            this.button2.Text = "E";
            this.toolTip1.SetToolTip(this.button2, "Exprot To PFD");
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click_2);
            // 
            // toolTip1
            // 
            this.toolTip1.UseAnimation = false;
            this.toolTip1.UseFading = false;
            // 
            // comboyr
            // 
            this.comboyr.FormattingEnabled = true;
            this.comboyr.Location = new System.Drawing.Point(709, 15);
            this.comboyr.Name = "comboyr";
            this.comboyr.Size = new System.Drawing.Size(86, 21);
            this.comboyr.TabIndex = 138;
            this.comboyr.SelectedIndexChanged += new System.EventHandler(this.comboyr_SelectedIndexChanged);
            // 
            // button3
            // 
            this.button3.Location = new System.Drawing.Point(887, 14);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(97, 23);
            this.button3.TabIndex = 139;
            this.button3.Text = "Third Party ARE";
            this.button3.UseVisualStyleBackColor = true;
            this.button3.Click += new System.EventHandler(this.button3_Click_1);
            // 
            // cmbinv2
            // 
            this.cmbinv2.FormattingEnabled = true;
            this.cmbinv2.Location = new System.Drawing.Point(556, 15);
            this.cmbinv2.Name = "cmbinv2";
            this.cmbinv2.Size = new System.Drawing.Size(108, 21);
            this.cmbinv2.TabIndex = 141;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(519, 19);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(20, 13);
            this.label2.TabIndex = 140;
            this.label2.Text = "To";
            // 
            // button4
            // 
            this.button4.Location = new System.Drawing.Point(806, 43);
            this.button4.Name = "button4";
            this.button4.Size = new System.Drawing.Size(178, 23);
            this.button4.TabIndex = 142;
            this.button4.Text = "Mutilple Print";
            this.button4.UseVisualStyleBackColor = true;
            this.button4.Click += new System.EventHandler(this.button4_Click);
            // 
            // frmPrintDomesticPrice
            // 
            this.AcceptButton = this.button1;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScroll = true;
            this.BackColor = System.Drawing.Color.WhiteSmoke;
            this.ClientSize = new System.Drawing.Size(1020, 641);
            this.Controls.Add(this.button4);
            this.Controls.Add(this.cmbinv2);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.button3);
            this.Controls.Add(this.comboyr);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.comboCompanyName);
            this.Controls.Add(this.label66);
            this.Controls.Add(this.btnPrint);
            this.Controls.Add(this.label33);
            this.Controls.Add(this.comboInvoice);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.crystalReportViewer1);
            this.Controls.Add(this.label1);
            this.Name = "frmPrintDomesticPrice";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Print DomesticPrice";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.Load += new System.EventHandler(this.frmPrintDomesticPrice_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private CrystalDecisions.Windows.Forms.CrystalReportViewer crystalReportViewer1;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.ComboBox comboInvoice;
        private System.Windows.Forms.Label label33;
        private System.Windows.Forms.Button btnPrint;
        private System.Windows.Forms.ComboBox comboCompanyName;
        private System.Windows.Forms.Label label66;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.ComboBox comboyr;
        private System.Windows.Forms.Button button3;
        private System.Windows.Forms.ComboBox cmbinv2;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button button4;
    }
}