namespace ERP
{
    partial class Form2Subsidiary_Challan
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form2Subsidiary_Challan));
            this.label2 = new System.Windows.Forms.Label();
            this.comboProcessorName = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.comboSubsidiaryBuyer = new System.Windows.Forms.ComboBox();
            this.label4 = new System.Windows.Forms.Label();
            this.textMainChallanNo = new System.Windows.Forms.TextBox();
            this.dateMainChallanDate = new System.Windows.Forms.DateTimePicker();
            this.label5 = new System.Windows.Forms.Label();
            this.textProcessorAdd = new System.Windows.Forms.TextBox();
            this.textSubsidiaryBuyerAdd = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.textQtyDispatched = new System.Windows.Forms.TextBox();
            this.label10 = new System.Windows.Forms.Label();
            this.label11 = new System.Windows.Forms.Label();
            this.comboQtyUnit = new System.Windows.Forms.ComboBox();
            this.textNatureofProcessing = new System.Windows.Forms.TextBox();
            this.textYear = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.btnUpdate = new System.Windows.Forms.Button();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnInitliaze = new System.Windows.Forms.Button();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.textBalanceQty = new System.Windows.Forms.TextBox();
            this.label8 = new System.Windows.Forms.Label();
            this.textBox2 = new System.Windows.Forms.TextBox();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.SuspendLayout();
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(273, 79);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(85, 13);
            this.label2.TabIndex = 1;
            this.label2.Text = "Processor Name";
            // 
            // comboProcessorName
            // 
            this.comboProcessorName.FormattingEnabled = true;
            this.comboProcessorName.Location = new System.Drawing.Point(366, 75);
            this.comboProcessorName.Name = "comboProcessorName";
            this.comboProcessorName.Size = new System.Drawing.Size(200, 21);
            this.comboProcessorName.TabIndex = 1;
            this.comboProcessorName.SelectedIndexChanged += new System.EventHandler(this.comboProcessorName_SelectedIndexChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(585, 79);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(85, 13);
            this.label3.TabIndex = 3;
            this.label3.Text = "Subsidiary Buyer";
            // 
            // comboSubsidiaryBuyer
            // 
            this.comboSubsidiaryBuyer.FormattingEnabled = true;
            this.comboSubsidiaryBuyer.Location = new System.Drawing.Point(676, 75);
            this.comboSubsidiaryBuyer.Name = "comboSubsidiaryBuyer";
            this.comboSubsidiaryBuyer.Size = new System.Drawing.Size(200, 21);
            this.comboSubsidiaryBuyer.TabIndex = 2;
            this.comboSubsidiaryBuyer.SelectedIndexChanged += new System.EventHandler(this.comboSubsidiaryBuyer_SelectedIndexChanged);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(270, 164);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(88, 13);
            this.label4.TabIndex = 5;
            this.label4.Text = "Main Challan No.";
            // 
            // textMainChallanNo
            // 
            this.textMainChallanNo.Location = new System.Drawing.Point(366, 160);
            this.textMainChallanNo.Name = "textMainChallanNo";
            this.textMainChallanNo.Size = new System.Drawing.Size(200, 20);
            this.textMainChallanNo.TabIndex = 5;
            // 
            // dateMainChallanDate
            // 
            this.dateMainChallanDate.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dateMainChallanDate.Location = new System.Drawing.Point(676, 160);
            this.dateMainChallanDate.Name = "dateMainChallanDate";
            this.dateMainChallanDate.Size = new System.Drawing.Size(200, 20);
            this.dateMainChallanDate.TabIndex = 6;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(576, 164);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(94, 13);
            this.label5.TabIndex = 8;
            this.label5.Text = "Main Challan Date";
            // 
            // textProcessorAdd
            // 
            this.textProcessorAdd.Location = new System.Drawing.Point(366, 104);
            this.textProcessorAdd.Multiline = true;
            this.textProcessorAdd.Name = "textProcessorAdd";
            this.textProcessorAdd.Size = new System.Drawing.Size(200, 45);
            this.textProcessorAdd.TabIndex = 3;
            // 
            // textSubsidiaryBuyerAdd
            // 
            this.textSubsidiaryBuyerAdd.Location = new System.Drawing.Point(677, 104);
            this.textSubsidiaryBuyerAdd.Multiline = true;
            this.textSubsidiaryBuyerAdd.Name = "textSubsidiaryBuyerAdd";
            this.textSubsidiaryBuyerAdd.Size = new System.Drawing.Size(200, 45);
            this.textSubsidiaryBuyerAdd.TabIndex = 4;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(280, 252);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(78, 13);
            this.label7.TabIndex = 21;
            this.label7.Text = "Order Quantity ";
            // 
            // textQtyDispatched
            // 
            this.textQtyDispatched.Location = new System.Drawing.Point(366, 248);
            this.textQtyDispatched.Name = "textQtyDispatched";
            this.textQtyDispatched.Size = new System.Drawing.Size(118, 20);
            this.textQtyDispatched.TabIndex = 8;
            this.textQtyDispatched.TextChanged += new System.EventHandler(this.textQtyDispatched_TextChanged);
            this.textQtyDispatched.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.textQtyDispatched_KeyPress);
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(223, 191);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(135, 13);
            this.label10.TabIndex = 23;
            this.label10.Text = "Nature of Processing Done";
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(493, 252);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(45, 13);
            this.label11.TabIndex = 24;
            this.label11.Text = "Qty Unit";
            // 
            // comboQtyUnit
            // 
            this.comboQtyUnit.FormattingEnabled = true;
            this.comboQtyUnit.Location = new System.Drawing.Point(542, 248);
            this.comboQtyUnit.Name = "comboQtyUnit";
            this.comboQtyUnit.Size = new System.Drawing.Size(82, 21);
            this.comboQtyUnit.TabIndex = 9;
            // 
            // textNatureofProcessing
            // 
            this.textNatureofProcessing.Location = new System.Drawing.Point(366, 191);
            this.textNatureofProcessing.Multiline = true;
            this.textNatureofProcessing.Name = "textNatureofProcessing";
            this.textNatureofProcessing.Size = new System.Drawing.Size(511, 47);
            this.textNatureofProcessing.TabIndex = 7;
            // 
            // textYear
            // 
            this.textYear.Location = new System.Drawing.Point(676, 44);
            this.textYear.Name = "textYear";
            this.textYear.Size = new System.Drawing.Size(118, 20);
            this.textYear.TabIndex = 0;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(641, 47);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(29, 13);
            this.label6.TabIndex = 26;
            this.label6.Text = "Year";
            // 
            // btnUpdate
            // 
            this.btnUpdate.Image = ((System.Drawing.Image)(resources.GetObject("btnUpdate.Image")));
            this.btnUpdate.Location = new System.Drawing.Point(677, 283);
            this.btnUpdate.Name = "btnUpdate";
            this.btnUpdate.Size = new System.Drawing.Size(85, 30);
            this.btnUpdate.TabIndex = 12;
            this.btnUpdate.UseVisualStyleBackColor = true;
            this.btnUpdate.Click += new System.EventHandler(this.btnUpdate_Click);
            // 
            // btnSave
            // 
            this.btnSave.Image = ((System.Drawing.Image)(resources.GetObject("btnSave.Image")));
            this.btnSave.Location = new System.Drawing.Point(591, 283);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(75, 30);
            this.btnSave.TabIndex = 11;
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // btnInitliaze
            // 
            this.btnInitliaze.Image = ((System.Drawing.Image)(resources.GetObject("btnInitliaze.Image")));
            this.btnInitliaze.Location = new System.Drawing.Point(495, 283);
            this.btnInitliaze.Name = "btnInitliaze";
            this.btnInitliaze.Size = new System.Drawing.Size(85, 30);
            this.btnInitliaze.TabIndex = 13;
            this.btnInitliaze.UseVisualStyleBackColor = true;
            this.btnInitliaze.Click += new System.EventHandler(this.btnInitliaze_Click);
            // 
            // dataGridView1
            // 
            this.dataGridView1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.dataGridView1.BackgroundColor = System.Drawing.Color.WhiteSmoke;
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Location = new System.Drawing.Point(26, 330);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.Size = new System.Drawing.Size(1191, 179);
            this.dataGridView1.TabIndex = 14;
            this.dataGridView1.DoubleClick += new System.EventHandler(this.dataGridView1_DoubleClick);
            // 
            // textBalanceQty
            // 
            this.textBalanceQty.Location = new System.Drawing.Point(759, 248);
            this.textBalanceQty.Name = "textBalanceQty";
            this.textBalanceQty.Size = new System.Drawing.Size(118, 20);
            this.textBalanceQty.TabIndex = 10;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(631, 252);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(126, 13);
            this.label8.TabIndex = 127;
            this.label8.Text = "Balanced Order Quantity ";
            // 
            // textBox2
            // 
            this.textBox2.BackColor = System.Drawing.SystemColors.GradientActiveCaption;
            this.textBox2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox2.Enabled = false;
            this.textBox2.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox2.Location = new System.Drawing.Point(26, 7);
            this.textBox2.Name = "textBox2";
            this.textBox2.Size = new System.Drawing.Size(1191, 25);
            this.textBox2.TabIndex = 412;
            this.textBox2.TabStop = false;
            this.textBox2.Text = "SUBSIDIARY ORDER";
            this.textBox2.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // Form2Subsidiary_Challan
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.WhiteSmoke;
            this.ClientSize = new System.Drawing.Size(1243, 521);
            this.Controls.Add(this.textBox2);
            this.Controls.Add(this.textBalanceQty);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.dataGridView1);
            this.Controls.Add(this.btnUpdate);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.btnInitliaze);
            this.Controls.Add(this.textYear);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.textNatureofProcessing);
            this.Controls.Add(this.comboQtyUnit);
            this.Controls.Add(this.label11);
            this.Controls.Add(this.label10);
            this.Controls.Add(this.textQtyDispatched);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.textSubsidiaryBuyerAdd);
            this.Controls.Add(this.textProcessorAdd);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.dateMainChallanDate);
            this.Controls.Add(this.textMainChallanNo);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.comboSubsidiaryBuyer);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.comboProcessorName);
            this.Controls.Add(this.label2);
            this.Name = "Form2Subsidiary_Challan";
            this.Text = "Subsidiary Order";
            this.TopMost = true;
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.Load += new System.EventHandler(this.Form2Subsidiary_Challan_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox comboProcessorName;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ComboBox comboSubsidiaryBuyer;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox textMainChallanNo;
        private System.Windows.Forms.DateTimePicker dateMainChallanDate;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox textProcessorAdd;
        private System.Windows.Forms.TextBox textSubsidiaryBuyerAdd;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox textQtyDispatched;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.ComboBox comboQtyUnit;
        private System.Windows.Forms.TextBox textNatureofProcessing;
        private System.Windows.Forms.TextBox textYear;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Button btnUpdate;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnInitliaze;
        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.TextBox textBalanceQty;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TextBox textBox2;
    }
}