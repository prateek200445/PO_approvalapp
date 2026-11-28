namespace ERP
{
    partial class frmpackingitemsnew
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmpackingitemsnew));
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.contno = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.qtyvalue = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.qtyunits = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.ColVarSep = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.qtyvalue2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.qtyunits2 = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.porno = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.sizevalue = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.sizeunit = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.totqty = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.qtyinuints3 = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.colnetwt = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.rate = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.amount = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.label1 = new System.Windows.Forms.Label();
            this.comboinvoiceno = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.comboitemcode = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.textpackinggoods = new System.Windows.Forms.TextBox();
            this.btn_save = new System.Windows.Forms.Button();
            this.btnUpdate = new System.Windows.Forms.Button();
            this.label4 = new System.Windows.Forms.Label();
            this.textNetWt = new System.Windows.Forms.TextBox();
            this.dataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewComboBoxColumn1 = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.dataGridViewTextBoxColumn3 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewComboBoxColumn2 = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.dataGridViewTextBoxColumn4 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn5 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewComboBoxColumn3 = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.dataGridViewTextBoxColumn6 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewComboBoxColumn4 = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.dataGridViewTextBoxColumn7 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn8 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn9 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.comboCompanyName = new System.Windows.Forms.ComboBox();
            this.label66 = new System.Windows.Forms.Label();
            this.btnItemDetails = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.SuspendLayout();
            // 
            // dataGridView1
            // 
            this.dataGridView1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.dataGridView1.BackgroundColor = System.Drawing.Color.WhiteSmoke;
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.contno,
            this.qtyvalue,
            this.qtyunits,
            this.ColVarSep,
            this.qtyvalue2,
            this.qtyunits2,
            this.porno,
            this.sizevalue,
            this.sizeunit,
            this.totqty,
            this.qtyinuints3,
            this.colnetwt,
            this.rate,
            this.amount});
            this.dataGridView1.Location = new System.Drawing.Point(21, 90);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.Size = new System.Drawing.Size(938, 380);
            this.dataGridView1.TabIndex = 5;
            this.dataGridView1.CurrentCellChanged += new System.EventHandler(this.dataGridView1_CurrentCellChanged);
            this.dataGridView1.EditingControlShowing += new System.Windows.Forms.DataGridViewEditingControlShowingEventHandler(this.dataGridView1_EditingControlShowing);
            // 
            // contno
            // 
            this.contno.HeaderText = "Container No.";
            this.contno.Name = "contno";
            // 
            // qtyvalue
            // 
            this.qtyvalue.HeaderText = "Qty Value";
            this.qtyvalue.Name = "qtyvalue";
            // 
            // qtyunits
            // 
            this.qtyunits.HeaderText = "Qty(in units)";
            this.qtyunits.Name = "qtyunits";
            // 
            // ColVarSep
            // 
            this.ColVarSep.HeaderText = "VarSep";
            this.ColVarSep.Name = "ColVarSep";
            this.ColVarSep.Width = 50;
            // 
            // qtyvalue2
            // 
            this.qtyvalue2.HeaderText = "Qty Value";
            this.qtyvalue2.Name = "qtyvalue2";
            // 
            // qtyunits2
            // 
            this.qtyunits2.HeaderText = "Qty(in Units)";
            this.qtyunits2.Name = "qtyunits2";
            // 
            // porno
            // 
            this.porno.HeaderText = "Por No.";
            this.porno.Name = "porno";
            // 
            // sizevalue
            // 
            this.sizevalue.HeaderText = "Size in";
            this.sizevalue.Name = "sizevalue";
            // 
            // sizeunit
            // 
            this.sizeunit.HeaderText = "size(in Units)";
            this.sizeunit.Name = "sizeunit";
            // 
            // totqty
            // 
            this.totqty.HeaderText = "Quantity";
            this.totqty.Name = "totqty";
            // 
            // qtyinuints3
            // 
            this.qtyinuints3.HeaderText = "Qty(in Units)";
            this.qtyinuints3.Name = "qtyinuints3";
            // 
            // colnetwt
            // 
            this.colnetwt.HeaderText = "Net Weight(kgs)";
            this.colnetwt.Name = "colnetwt";
            // 
            // rate
            // 
            this.rate.HeaderText = "Rate";
            this.rate.Name = "rate";
            // 
            // amount
            // 
            this.amount.HeaderText = "Amount";
            this.amount.Name = "amount";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(29, 43);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(65, 13);
            this.label1.TabIndex = 6;
            this.label1.Text = "Invoice No.:";
            // 
            // comboinvoiceno
            // 
            this.comboinvoiceno.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboinvoiceno.FormattingEnabled = true;
            this.comboinvoiceno.Location = new System.Drawing.Point(126, 38);
            this.comboinvoiceno.Name = "comboinvoiceno";
            this.comboinvoiceno.Size = new System.Drawing.Size(263, 21);
            this.comboinvoiceno.TabIndex = 1;
            this.comboinvoiceno.SelectedIndexChanged += new System.EventHandler(this.comboinvoiceno_SelectedIndexChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(29, 68);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(92, 13);
            this.label2.TabIndex = 8;
            this.label2.Text = "Invoice SubCode:";
            // 
            // comboitemcode
            // 
            this.comboitemcode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboitemcode.FormattingEnabled = true;
            this.comboitemcode.Location = new System.Drawing.Point(126, 64);
            this.comboitemcode.Name = "comboitemcode";
            this.comboitemcode.Size = new System.Drawing.Size(263, 21);
            this.comboitemcode.TabIndex = 2;
            this.comboitemcode.SelectedIndexChanged += new System.EventHandler(this.comboitemcode_SelectedIndexChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(567, 26);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(69, 26);
            this.label3.TabIndex = 10;
            this.label3.Text = "No. & Packing\r\nof Goods:\r\n";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // textpackinggoods
            // 
            this.textpackinggoods.Enabled = false;
            this.textpackinggoods.Location = new System.Drawing.Point(639, 22);
            this.textpackinggoods.Multiline = true;
            this.textpackinggoods.Name = "textpackinggoods";
            this.textpackinggoods.Size = new System.Drawing.Size(320, 58);
            this.textpackinggoods.TabIndex = 4;
            // 
            // btn_save
            // 
            this.btn_save.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btn_save.Image = ((System.Drawing.Image)(resources.GetObject("btn_save.Image")));
            this.btn_save.Location = new System.Drawing.Point(410, 487);
            this.btn_save.Name = "btn_save";
            this.btn_save.Size = new System.Drawing.Size(75, 30);
            this.btn_save.TabIndex = 6;
            this.btn_save.UseVisualStyleBackColor = true;
            this.btn_save.Click += new System.EventHandler(this.btn_save_Click);
            // 
            // btnUpdate
            // 
            this.btnUpdate.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnUpdate.Image = ((System.Drawing.Image)(resources.GetObject("btnUpdate.Image")));
            this.btnUpdate.Location = new System.Drawing.Point(491, 487);
            this.btnUpdate.Name = "btnUpdate";
            this.btnUpdate.Size = new System.Drawing.Size(85, 30);
            this.btnUpdate.TabIndex = 7;
            this.btnUpdate.UseVisualStyleBackColor = true;
            this.btnUpdate.Click += new System.EventHandler(this.btnUpdate_Click);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(407, 68);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(38, 13);
            this.label4.TabIndex = 14;
            this.label4.Text = "NetWt";
            // 
            // textNetWt
            // 
            this.textNetWt.Enabled = false;
            this.textNetWt.Location = new System.Drawing.Point(454, 64);
            this.textNetWt.Name = "textNetWt";
            this.textNetWt.Size = new System.Drawing.Size(97, 20);
            this.textNetWt.TabIndex = 3;
            // 
            // dataGridViewTextBoxColumn1
            // 
            this.dataGridViewTextBoxColumn1.HeaderText = "Container No.";
            this.dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
            // 
            // dataGridViewTextBoxColumn2
            // 
            this.dataGridViewTextBoxColumn2.HeaderText = "Qty Value";
            this.dataGridViewTextBoxColumn2.Name = "dataGridViewTextBoxColumn2";
            // 
            // dataGridViewComboBoxColumn1
            // 
            this.dataGridViewComboBoxColumn1.HeaderText = "Qty(in units)";
            this.dataGridViewComboBoxColumn1.Name = "dataGridViewComboBoxColumn1";
            // 
            // dataGridViewTextBoxColumn3
            // 
            this.dataGridViewTextBoxColumn3.HeaderText = "Qty Value";
            this.dataGridViewTextBoxColumn3.Name = "dataGridViewTextBoxColumn3";
            // 
            // dataGridViewComboBoxColumn2
            // 
            this.dataGridViewComboBoxColumn2.HeaderText = "Qty(in Units)";
            this.dataGridViewComboBoxColumn2.Name = "dataGridViewComboBoxColumn2";
            // 
            // dataGridViewTextBoxColumn4
            // 
            this.dataGridViewTextBoxColumn4.HeaderText = "Por No.";
            this.dataGridViewTextBoxColumn4.Name = "dataGridViewTextBoxColumn4";
            // 
            // dataGridViewTextBoxColumn5
            // 
            this.dataGridViewTextBoxColumn5.HeaderText = "Size in";
            this.dataGridViewTextBoxColumn5.Name = "dataGridViewTextBoxColumn5";
            // 
            // dataGridViewComboBoxColumn3
            // 
            this.dataGridViewComboBoxColumn3.HeaderText = "size(in Units)";
            this.dataGridViewComboBoxColumn3.Name = "dataGridViewComboBoxColumn3";
            // 
            // dataGridViewTextBoxColumn6
            // 
            this.dataGridViewTextBoxColumn6.HeaderText = "Quantity";
            this.dataGridViewTextBoxColumn6.Name = "dataGridViewTextBoxColumn6";
            // 
            // dataGridViewComboBoxColumn4
            // 
            this.dataGridViewComboBoxColumn4.HeaderText = "Qty(in Units)";
            this.dataGridViewComboBoxColumn4.Name = "dataGridViewComboBoxColumn4";
            // 
            // dataGridViewTextBoxColumn7
            // 
            this.dataGridViewTextBoxColumn7.HeaderText = "Net Weight(kgs)";
            this.dataGridViewTextBoxColumn7.Name = "dataGridViewTextBoxColumn7";
            // 
            // dataGridViewTextBoxColumn8
            // 
            this.dataGridViewTextBoxColumn8.HeaderText = "Rate";
            this.dataGridViewTextBoxColumn8.Name = "dataGridViewTextBoxColumn8";
            // 
            // dataGridViewTextBoxColumn9
            // 
            this.dataGridViewTextBoxColumn9.HeaderText = "Amount";
            this.dataGridViewTextBoxColumn9.Name = "dataGridViewTextBoxColumn9";
            // 
            // comboCompanyName
            // 
            this.comboCompanyName.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboCompanyName.Enabled = false;
            this.comboCompanyName.FormattingEnabled = true;
            this.comboCompanyName.Location = new System.Drawing.Point(126, 12);
            this.comboCompanyName.Name = "comboCompanyName";
            this.comboCompanyName.Size = new System.Drawing.Size(263, 21);
            this.comboCompanyName.TabIndex = 0;
            this.comboCompanyName.SelectedIndexChanged += new System.EventHandler(this.comboCompanyName_SelectedIndexChanged);
            // 
            // label66
            // 
            this.label66.AutoSize = true;
            this.label66.Location = new System.Drawing.Point(29, 16);
            this.label66.Name = "label66";
            this.label66.Size = new System.Drawing.Size(85, 13);
            this.label66.TabIndex = 136;
            this.label66.Text = "Company Name:";
            // 
            // btnItemDetails
            // 
            this.btnItemDetails.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnItemDetails.BackColor = System.Drawing.Color.LightSteelBlue;
            this.btnItemDetails.Font = new System.Drawing.Font("Arial Narrow", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnItemDetails.Location = new System.Drawing.Point(582, 487);
            this.btnItemDetails.Name = "btnItemDetails";
            this.btnItemDetails.Size = new System.Drawing.Size(110, 30);
            this.btnItemDetails.TabIndex = 418;
            this.btnItemDetails.Text = "Load Packing List";
            this.btnItemDetails.UseVisualStyleBackColor = false;
            this.btnItemDetails.Click += new System.EventHandler(this.btnItemDetails_Click);
            // 
            // frmpackingitemsnew
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.WhiteSmoke;
            this.ClientSize = new System.Drawing.Size(985, 529);
            this.Controls.Add(this.btnItemDetails);
            this.Controls.Add(this.comboCompanyName);
            this.Controls.Add(this.label66);
            this.Controls.Add(this.textNetWt);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.btnUpdate);
            this.Controls.Add(this.btn_save);
            this.Controls.Add(this.textpackinggoods);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.comboitemcode);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.comboinvoiceno);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.dataGridView1);
            this.Name = "frmpackingitemsnew";
            this.Text = "Packingitemsnew";
            this.Load += new System.EventHandler(this.frmpackingitemsnew_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox comboinvoiceno;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox comboitemcode;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox textpackinggoods;
        private System.Windows.Forms.Button btn_save;
        private System.Windows.Forms.Button btnUpdate;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox textNetWt;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn2;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn3;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn4;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn5;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn6;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn7;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn8;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn9;
        private System.Windows.Forms.DataGridViewComboBoxColumn dataGridViewComboBoxColumn1;
        private System.Windows.Forms.DataGridViewComboBoxColumn dataGridViewComboBoxColumn2;
        private System.Windows.Forms.DataGridViewComboBoxColumn dataGridViewComboBoxColumn3;
        private System.Windows.Forms.DataGridViewComboBoxColumn dataGridViewComboBoxColumn4;
        private System.Windows.Forms.ComboBox comboCompanyName;
        private System.Windows.Forms.Label label66;
        private System.Windows.Forms.DataGridViewTextBoxColumn contno;
        private System.Windows.Forms.DataGridViewTextBoxColumn qtyvalue;
        private System.Windows.Forms.DataGridViewComboBoxColumn qtyunits;
        private System.Windows.Forms.DataGridViewTextBoxColumn ColVarSep;
        private System.Windows.Forms.DataGridViewTextBoxColumn qtyvalue2;
        private System.Windows.Forms.DataGridViewComboBoxColumn qtyunits2;
        private System.Windows.Forms.DataGridViewTextBoxColumn porno;
        private System.Windows.Forms.DataGridViewTextBoxColumn sizevalue;
        private System.Windows.Forms.DataGridViewComboBoxColumn sizeunit;
        private System.Windows.Forms.DataGridViewTextBoxColumn totqty;
        private System.Windows.Forms.DataGridViewComboBoxColumn qtyinuints3;
        private System.Windows.Forms.DataGridViewTextBoxColumn colnetwt;
        private System.Windows.Forms.DataGridViewTextBoxColumn rate;
        private System.Windows.Forms.DataGridViewTextBoxColumn amount;
        private System.Windows.Forms.Button btnItemDetails;
    }
}