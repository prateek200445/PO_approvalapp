namespace ERP
{
    partial class FrmSubsidiaryChallanMulti
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
            this.label2 = new System.Windows.Forms.Label();
            this.textNatureofProcessing = new System.Windows.Forms.TextBox();
            this.label10 = new System.Windows.Forms.Label();
            this.textSubsidiaryBuyerAdd = new System.Windows.Forms.TextBox();
            this.textProcessorAdd = new System.Windows.Forms.TextBox();
            this.comboSubsidiaryBuyer = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.comboProcessorName = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.Column6 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column7 = new ERP.CalendarColumn();
            this.colMainGrp = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.colItemCode = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.DtCommodity = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.Column8 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.DtUnit = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.Column5 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.btnUpdate = new System.Windows.Forms.Button();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnInitliaze = new System.Windows.Forms.Button();
            this.dataGridView2 = new System.Windows.Forms.DataGridView();
            this.textYear = new System.Windows.Forms.TextBox();
            this.label13 = new System.Windows.Forms.Label();
            this.dataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewComboBoxColumn1 = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.dataGridViewTextBoxColumn3 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn4 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewComboBoxColumn2 = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.dataGridViewTextBoxColumn5 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn6 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView2)).BeginInit();
            this.SuspendLayout();
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(397, 22);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(210, 17);
            this.label2.TabIndex = 1;
            this.label2.Text = "SUBSIDIARY ORDER MULTI";
            // 
            // textNatureofProcessing
            // 
            this.textNatureofProcessing.Location = new System.Drawing.Point(708, 89);
            this.textNatureofProcessing.Multiline = true;
            this.textNatureofProcessing.Name = "textNatureofProcessing";
            this.textNatureofProcessing.Size = new System.Drawing.Size(286, 33);
            this.textNatureofProcessing.TabIndex = 140;
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(707, 67);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(135, 13);
            this.label10.TabIndex = 142;
            this.label10.Text = "Nature of Processing Done";
            // 
            // textSubsidiaryBuyerAdd
            // 
            this.textSubsidiaryBuyerAdd.Location = new System.Drawing.Point(463, 89);
            this.textSubsidiaryBuyerAdd.Multiline = true;
            this.textSubsidiaryBuyerAdd.Name = "textSubsidiaryBuyerAdd";
            this.textSubsidiaryBuyerAdd.Size = new System.Drawing.Size(232, 45);
            this.textSubsidiaryBuyerAdd.TabIndex = 132;
            // 
            // textProcessorAdd
            // 
            this.textProcessorAdd.Location = new System.Drawing.Point(121, 89);
            this.textProcessorAdd.Multiline = true;
            this.textProcessorAdd.Name = "textProcessorAdd";
            this.textProcessorAdd.Size = new System.Drawing.Size(238, 45);
            this.textProcessorAdd.TabIndex = 129;
            // 
            // comboSubsidiaryBuyer
            // 
            this.comboSubsidiaryBuyer.FormattingEnabled = true;
            this.comboSubsidiaryBuyer.Location = new System.Drawing.Point(462, 62);
            this.comboSubsidiaryBuyer.Name = "comboSubsidiaryBuyer";
            this.comboSubsidiaryBuyer.Size = new System.Drawing.Size(232, 21);
            this.comboSubsidiaryBuyer.TabIndex = 131;
            this.comboSubsidiaryBuyer.SelectedIndexChanged += new System.EventHandler(this.comboSubsidiaryBuyer_SelectedIndexChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(372, 62);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(85, 13);
            this.label3.TabIndex = 133;
            this.label3.Text = "Subsidiary Buyer";
            // 
            // comboProcessorName
            // 
            this.comboProcessorName.Enabled = false;
            this.comboProcessorName.FormattingEnabled = true;
            this.comboProcessorName.Location = new System.Drawing.Point(121, 62);
            this.comboProcessorName.Name = "comboProcessorName";
            this.comboProcessorName.Size = new System.Drawing.Size(238, 21);
            this.comboProcessorName.TabIndex = 128;
            this.comboProcessorName.SelectedIndexChanged += new System.EventHandler(this.comboProcessorName_SelectedIndexChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(26, 64);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(85, 13);
            this.label1.TabIndex = 130;
            this.label1.Text = "Processor Name";
            // 
            // dataGridView1
            // 
            this.dataGridView1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Column6,
            this.Column7,
            this.colMainGrp,
            this.colItemCode,
            this.DtCommodity,
            this.Column8,
            this.Column2,
            this.DtUnit,
            this.Column5});
            this.dataGridView1.Location = new System.Drawing.Point(29, 140);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.Size = new System.Drawing.Size(967, 234);
            this.dataGridView1.TabIndex = 148;
            this.dataGridView1.CurrentCellChanged += new System.EventHandler(this.dataGridView1_CurrentCellChanged);
            this.dataGridView1.EditingControlShowing += new System.Windows.Forms.DataGridViewEditingControlShowingEventHandler(this.dataGridView1_EditingControlShowing);
            // 
            // Column6
            // 
            this.Column6.HeaderText = "Main Challan No.";
            this.Column6.Name = "Column6";
            // 
            // Column7
            // 
            this.Column7.HeaderText = "Main Challan Date";
            this.Column7.Name = "Column7";
            this.Column7.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.Column7.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            // 
            // colMainGrp
            // 
            this.colMainGrp.HeaderText = "Main Group";
            this.colMainGrp.Name = "colMainGrp";
            // 
            // colItemCode
            // 
            this.colItemCode.AutoComplete = false;
            this.colItemCode.HeaderText = "Item Name";
            this.colItemCode.Name = "colItemCode";
            this.colItemCode.Width = 200;
            // 
            // DtCommodity
            // 
            this.DtCommodity.HeaderText = "Commodity Name";
            this.DtCommodity.Name = "DtCommodity";
            this.DtCommodity.Width = 300;
            // 
            // Column8
            // 
            this.Column8.HeaderText = "T-Heading";
            this.Column8.Name = "Column8";
            // 
            // Column2
            // 
            this.Column2.HeaderText = "Order Qty";
            this.Column2.Name = "Column2";
            this.Column2.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.Column2.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // DtUnit
            // 
            this.DtUnit.HeaderText = "Qty Unit";
            this.DtUnit.Name = "DtUnit";
            // 
            // Column5
            // 
            this.Column5.HeaderText = "Balanced Qty";
            this.Column5.Name = "Column5";
            this.Column5.ReadOnly = true;
            // 
            // btnUpdate
            // 
            this.btnUpdate.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnUpdate.Location = new System.Drawing.Point(532, 380);
            this.btnUpdate.Name = "btnUpdate";
            this.btnUpdate.Size = new System.Drawing.Size(75, 32);
            this.btnUpdate.TabIndex = 151;
            this.btnUpdate.Text = "Update";
            this.btnUpdate.UseVisualStyleBackColor = true;
            this.btnUpdate.Click += new System.EventHandler(this.btnUpdate_Click);
            // 
            // btnSave
            // 
            this.btnSave.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnSave.Location = new System.Drawing.Point(448, 380);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(75, 32);
            this.btnSave.TabIndex = 150;
            this.btnSave.Text = "Save";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // btnInitliaze
            // 
            this.btnInitliaze.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnInitliaze.Location = new System.Drawing.Point(363, 380);
            this.btnInitliaze.Name = "btnInitliaze";
            this.btnInitliaze.Size = new System.Drawing.Size(75, 32);
            this.btnInitliaze.TabIndex = 149;
            this.btnInitliaze.Text = "Initliaze";
            this.btnInitliaze.UseVisualStyleBackColor = true;
            this.btnInitliaze.Click += new System.EventHandler(this.btnInitliaze_Click);
            // 
            // dataGridView2
            // 
            this.dataGridView2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.dataGridView2.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView2.Location = new System.Drawing.Point(29, 418);
            this.dataGridView2.Name = "dataGridView2";
            this.dataGridView2.Size = new System.Drawing.Size(965, 169);
            this.dataGridView2.TabIndex = 152;
            this.dataGridView2.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridView2_CellContentClick);
            this.dataGridView2.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.dataGridView2_MouseDoubleClick);
            // 
            // textYear
            // 
            this.textYear.Location = new System.Drawing.Point(888, 40);
            this.textYear.Name = "textYear";
            this.textYear.Size = new System.Drawing.Size(106, 20);
            this.textYear.TabIndex = 153;
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(853, 43);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(29, 13);
            this.label13.TabIndex = 154;
            this.label13.Text = "Year";
            // 
            // dataGridViewTextBoxColumn1
            // 
            this.dataGridViewTextBoxColumn1.HeaderText = "Main Challan No.";
            this.dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
            // 
            // dataGridViewTextBoxColumn2
            // 
            this.dataGridViewTextBoxColumn2.HeaderText = "Main Challan Date";
            this.dataGridViewTextBoxColumn2.Name = "dataGridViewTextBoxColumn2";
            // 
            // dataGridViewComboBoxColumn1
            // 
            this.dataGridViewComboBoxColumn1.HeaderText = "Commodity Name";
            this.dataGridViewComboBoxColumn1.Name = "dataGridViewComboBoxColumn1";
            this.dataGridViewComboBoxColumn1.Width = 300;
            // 
            // dataGridViewTextBoxColumn3
            // 
            this.dataGridViewTextBoxColumn3.HeaderText = "T-Heading";
            this.dataGridViewTextBoxColumn3.Name = "dataGridViewTextBoxColumn3";
            // 
            // dataGridViewTextBoxColumn4
            // 
            this.dataGridViewTextBoxColumn4.HeaderText = "Order Qty";
            this.dataGridViewTextBoxColumn4.Name = "dataGridViewTextBoxColumn4";
            this.dataGridViewTextBoxColumn4.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.dataGridViewTextBoxColumn4.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // dataGridViewComboBoxColumn2
            // 
            this.dataGridViewComboBoxColumn2.HeaderText = "Qty Unit";
            this.dataGridViewComboBoxColumn2.Name = "dataGridViewComboBoxColumn2";
            // 
            // dataGridViewTextBoxColumn5
            // 
            this.dataGridViewTextBoxColumn5.HeaderText = "Used Qty";
            this.dataGridViewTextBoxColumn5.Name = "dataGridViewTextBoxColumn5";
            // 
            // dataGridViewTextBoxColumn6
            // 
            this.dataGridViewTextBoxColumn6.HeaderText = "Balanced Qty";
            this.dataGridViewTextBoxColumn6.Name = "dataGridViewTextBoxColumn6";
            // 
            // FrmSubsidiaryChallanMulti
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScroll = true;
            this.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.ClientSize = new System.Drawing.Size(1020, 599);
            this.Controls.Add(this.textYear);
            this.Controls.Add(this.label13);
            this.Controls.Add(this.dataGridView2);
            this.Controls.Add(this.btnUpdate);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.btnInitliaze);
            this.Controls.Add(this.dataGridView1);
            this.Controls.Add(this.textNatureofProcessing);
            this.Controls.Add(this.label10);
            this.Controls.Add(this.textSubsidiaryBuyerAdd);
            this.Controls.Add(this.textProcessorAdd);
            this.Controls.Add(this.comboSubsidiaryBuyer);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.comboProcessorName);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.label2);
            this.Name = "FrmSubsidiaryChallanMulti";
            this.Text = "Subsidiary Order Multi";
            this.TopMost = true;
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.Load += new System.EventHandler(this.FrmSubsidiaryChallanMulti_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView2)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox textNatureofProcessing;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.TextBox textSubsidiaryBuyerAdd;
        private System.Windows.Forms.TextBox textProcessorAdd;
        private System.Windows.Forms.ComboBox comboSubsidiaryBuyer;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ComboBox comboProcessorName;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.Button btnUpdate;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnInitliaze;
        private System.Windows.Forms.DataGridView dataGridView2;
        private System.Windows.Forms.TextBox textYear;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn2;
        private System.Windows.Forms.DataGridViewComboBoxColumn dataGridViewComboBoxColumn1;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn3;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn4;
        private System.Windows.Forms.DataGridViewComboBoxColumn dataGridViewComboBoxColumn2;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn5;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn6;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column6;
        private CalendarColumn Column7;
        private System.Windows.Forms.DataGridViewComboBoxColumn colMainGrp;
        private System.Windows.Forms.DataGridViewComboBoxColumn colItemCode;
        private System.Windows.Forms.DataGridViewComboBoxColumn DtCommodity;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column8;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column2;
        private System.Windows.Forms.DataGridViewComboBoxColumn DtUnit;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column5;

    }
}