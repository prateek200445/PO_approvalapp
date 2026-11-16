namespace ERP
{
    partial class FrmReceipt_Warehouse
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmReceipt_Warehouse));
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.Column1 = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.selectCheckItemToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.button1 = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.ComboCmpny = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.combowarehouse = new System.Windows.Forms.ComboBox();
            this.button4 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.textBox2 = new System.Windows.Forms.TextBox();
            this.chkSelect = new System.Windows.Forms.CheckBox();
            this.btn_update_warehouse = new System.Windows.Forms.Button();
            this.combo_change_warehouse = new System.Windows.Forms.ComboBox();
            this.lbl_change_warehouse = new System.Windows.Forms.Label();
            this.comboInternalReq = new System.Windows.Forms.ComboBox();
            this.label4 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.contextMenuStrip1.SuspendLayout();
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
            this.Column1});
            this.dataGridView1.ContextMenuStrip = this.contextMenuStrip1;
            this.dataGridView1.Location = new System.Drawing.Point(12, 85);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.Size = new System.Drawing.Size(1033, 280);
            this.dataGridView1.TabIndex = 6;
            this.dataGridView1.CurrentCellDirtyStateChanged += new System.EventHandler(this.dataGridView1_CurrentCellDirtyStateChanged);
            this.dataGridView1.SelectionChanged += new System.EventHandler(this.dataGridView1_SelectionChanged);
            // 
            // Column1
            // 
            this.Column1.HeaderText = "Check";
            this.Column1.Name = "Column1";
            this.Column1.Width = 80;
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.selectCheckItemToolStripMenuItem});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(171, 26);
            // 
            // selectCheckItemToolStripMenuItem
            // 
            this.selectCheckItemToolStripMenuItem.Name = "selectCheckItemToolStripMenuItem";
            this.selectCheckItemToolStripMenuItem.Size = new System.Drawing.Size(170, 22);
            this.selectCheckItemToolStripMenuItem.Text = "Select/Check Item";
            this.selectCheckItemToolStripMenuItem.Click += new System.EventHandler(this.selectCheckItemToolStripMenuItem_Click_1);
            // 
            // button1
            // 
            this.button1.Image = ((System.Drawing.Image)(resources.GetObject("button1.Image")));
            this.button1.Location = new System.Drawing.Point(982, 41);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(60, 30);
            this.button1.TabIndex = 8;
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(98, 50);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(88, 13);
            this.label1.TabIndex = 9;
            this.label1.Text = "Company Name :";
            // 
            // ComboCmpny
            // 
            this.ComboCmpny.FormattingEnabled = true;
            this.ComboCmpny.Location = new System.Drawing.Point(192, 47);
            this.ComboCmpny.Name = "ComboCmpny";
            this.ComboCmpny.Size = new System.Drawing.Size(292, 21);
            this.ComboCmpny.TabIndex = 7;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(490, 33);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(73, 13);
            this.label2.TabIndex = 10;
            this.label2.Text = "Ware House :";
            // 
            // combowarehouse
            // 
            this.combowarehouse.FormattingEnabled = true;
            this.combowarehouse.Location = new System.Drawing.Point(492, 47);
            this.combowarehouse.Name = "combowarehouse";
            this.combowarehouse.Size = new System.Drawing.Size(223, 21);
            this.combowarehouse.TabIndex = 11;
            this.combowarehouse.SelectedIndexChanged += new System.EventHandler(this.combowarehouse_SelectedIndexChanged);
            // 
            // button4
            // 
            this.button4.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.button4.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button4.Image = ((System.Drawing.Image)(resources.GetObject("button4.Image")));
            this.button4.Location = new System.Drawing.Point(12, 375);
            this.button4.Name = "button4";
            this.button4.Size = new System.Drawing.Size(115, 30);
            this.button4.TabIndex = 12;
            this.button4.Text = "Accept";
            this.button4.UseVisualStyleBackColor = true;
            this.button4.Click += new System.EventHandler(this.button4_Click);
            // 
            // button2
            // 
            this.button2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.button2.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button2.Location = new System.Drawing.Point(141, 374);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(119, 31);
            this.button2.TabIndex = 13;
            this.button2.Text = "Reject";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // textBox2
            // 
            this.textBox2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBox2.BackColor = System.Drawing.SystemColors.GradientActiveCaption;
            this.textBox2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox2.Enabled = false;
            this.textBox2.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox2.ForeColor = System.Drawing.Color.Black;
            this.textBox2.Location = new System.Drawing.Point(9, 8);
            this.textBox2.Name = "textBox2";
            this.textBox2.Size = new System.Drawing.Size(1033, 25);
            this.textBox2.TabIndex = 420;
            this.textBox2.TabStop = false;
            this.textBox2.Text = "RAW MATERIAL RECEIPT";
            this.textBox2.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // chkSelect
            // 
            this.chkSelect.AutoSize = true;
            this.chkSelect.Location = new System.Drawing.Point(24, 65);
            this.chkSelect.Name = "chkSelect";
            this.chkSelect.Size = new System.Drawing.Size(125, 17);
            this.chkSelect.TabIndex = 421;
            this.chkSelect.Text = "Select / UnSelect All";
            this.chkSelect.UseVisualStyleBackColor = true;
            this.chkSelect.CheckedChanged += new System.EventHandler(this.chkSelect_CheckedChanged);
            // 
            // btn_update_warehouse
            // 
            this.btn_update_warehouse.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.btn_update_warehouse.Enabled = false;
            this.btn_update_warehouse.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btn_update_warehouse.Image = ((System.Drawing.Image)(resources.GetObject("btn_update_warehouse.Image")));
            this.btn_update_warehouse.Location = new System.Drawing.Point(661, 375);
            this.btn_update_warehouse.Name = "btn_update_warehouse";
            this.btn_update_warehouse.Size = new System.Drawing.Size(91, 30);
            this.btn_update_warehouse.TabIndex = 430;
            this.btn_update_warehouse.Text = "Update";
            this.btn_update_warehouse.UseVisualStyleBackColor = true;
            this.btn_update_warehouse.Click += new System.EventHandler(this.btn_update_warehouse_Click);
            // 
            // combo_change_warehouse
            // 
            this.combo_change_warehouse.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.combo_change_warehouse.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.combo_change_warehouse.FormattingEnabled = true;
            this.combo_change_warehouse.Location = new System.Drawing.Point(401, 380);
            this.combo_change_warehouse.Name = "combo_change_warehouse";
            this.combo_change_warehouse.Size = new System.Drawing.Size(252, 21);
            this.combo_change_warehouse.TabIndex = 429;
            // 
            // lbl_change_warehouse
            // 
            this.lbl_change_warehouse.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.lbl_change_warehouse.AutoSize = true;
            this.lbl_change_warehouse.Location = new System.Drawing.Point(288, 384);
            this.lbl_change_warehouse.Name = "lbl_change_warehouse";
            this.lbl_change_warehouse.Size = new System.Drawing.Size(108, 13);
            this.lbl_change_warehouse.TabIndex = 428;
            this.lbl_change_warehouse.Text = "Change Warehouse :";
            // 
            // comboInternalReq
            // 
            this.comboInternalReq.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboInternalReq.FormattingEnabled = true;
            this.comboInternalReq.Items.AddRange(new object[] {
            "FROM ITEM MASTER"});
            this.comboInternalReq.Location = new System.Drawing.Point(721, 50);
            this.comboInternalReq.Name = "comboInternalReq";
            this.comboInternalReq.Size = new System.Drawing.Size(242, 21);
            this.comboInternalReq.TabIndex = 451;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(718, 36);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(92, 13);
            this.label4.TabIndex = 452;
            this.label4.Text = "Interal Requistion:";
            // 
            // FrmReceipt_Warehouse
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.WhiteSmoke;
            this.ClientSize = new System.Drawing.Size(1057, 415);
            this.Controls.Add(this.comboInternalReq);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.btn_update_warehouse);
            this.Controls.Add(this.combo_change_warehouse);
            this.Controls.Add(this.lbl_change_warehouse);
            this.Controls.Add(this.chkSelect);
            this.Controls.Add(this.textBox2);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.button4);
            this.Controls.Add(this.combowarehouse);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.ComboCmpny);
            this.Controls.Add(this.dataGridView1);
            this.Name = "FrmReceipt_Warehouse";
            this.Text = "Receipt";
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.contextMenuStrip1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.DataGridViewCheckBoxColumn Column1;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox ComboCmpny;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox combowarehouse;
        private System.Windows.Forms.Button button4;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.TextBox textBox2;
        private System.Windows.Forms.CheckBox chkSelect;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem selectCheckItemToolStripMenuItem;
        private System.Windows.Forms.Button btn_update_warehouse;
        private System.Windows.Forms.ComboBox combo_change_warehouse;
        private System.Windows.Forms.Label lbl_change_warehouse;
        private System.Windows.Forms.ComboBox comboInternalReq;
        private System.Windows.Forms.Label label4;
    }
}