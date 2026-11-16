namespace ERP
{
    partial class FrmWareHouseTransferNew
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmWareHouseTransferNew));
            this.label6 = new System.Windows.Forms.Label();
            this.dateTimePicker1 = new System.Windows.Forms.DateTimePicker();
            this.comboWareHouseTo = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.comboWareHouseFrom = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.comboCompanyName = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.Column1 = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.editRollToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.selectCheckRollsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.btnTransfer = new System.Windows.Forms.Button();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.dateTimePicker2 = new System.Windows.Forms.DateTimePicker();
            this.button1 = new System.Windows.Forms.Button();
            this.label7 = new System.Windows.Forms.Label();
            this.ddlPartyName = new System.Windows.Forms.ComboBox();
            this.label8 = new System.Windows.Forms.Label();
            this.ddlPurpose = new System.Windows.Forms.ComboBox();
            this.label9 = new System.Windows.Forms.Label();
            this.ddlType = new System.Windows.Forms.ComboBox();
            this.label10 = new System.Windows.Forms.Label();
            this.txtInvNo = new System.Windows.Forms.TextBox();
            this.label11 = new System.Windows.Forms.Label();
            this.txtRemarks = new System.Windows.Forms.TextBox();
            this.label12 = new System.Windows.Forms.Label();
            this.dateTransfer = new System.Windows.Forms.DateTimePicker();
            this.textBox2 = new System.Windows.Forms.TextBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.comboRollForm = new System.Windows.Forms.ComboBox();
            this.lblInfo = new System.Windows.Forms.Label();
            this.button2 = new System.Windows.Forms.Button();
            this.label13 = new System.Windows.Forms.Label();
            this.textRollNo = new System.Windows.Forms.TextBox();
            this.label14 = new System.Windows.Forms.Label();
            this.cmbStatus = new System.Windows.Forms.ComboBox();
            this.button4 = new System.Windows.Forms.Button();
            this.btnDelete = new System.Windows.Forms.Button();
            this.TxtMultSel = new System.Windows.Forms.TextBox();
            this.cachedCrMISRollForDispatchDisplay1 = new production.CachedCrMISRollForDispatchDisplay();
            this.chkNM = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.contextMenuStrip1.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(551, 57);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(52, 13);
            this.label6.TabIndex = 71;
            this.label6.Text = "To Date :";
            // 
            // dateTimePicker1
            // 
            this.dateTimePicker1.Location = new System.Drawing.Point(603, 54);
            this.dateTimePicker1.Name = "dateTimePicker1";
            this.dateTimePicker1.Size = new System.Drawing.Size(87, 20);
            this.dateTimePicker1.TabIndex = 64;
            // 
            // comboWareHouseTo
            // 
            this.comboWareHouseTo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboWareHouseTo.FormattingEnabled = true;
            this.comboWareHouseTo.Items.AddRange(new object[] {
            "FROM ITEM MASTER"});
            this.comboWareHouseTo.Location = new System.Drawing.Point(460, 80);
            this.comboWareHouseTo.Name = "comboWareHouseTo";
            this.comboWareHouseTo.Size = new System.Drawing.Size(202, 21);
            this.comboWareHouseTo.TabIndex = 67;
            this.comboWareHouseTo.SelectedIndexChanged += new System.EventHandler(this.comboWareHouseTo_SelectedIndexChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(343, 84);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(115, 13);
            this.label3.TabIndex = 70;
            this.label3.Text = "To Warehouse Name :";
            // 
            // comboWareHouseFrom
            // 
            this.comboWareHouseFrom.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboWareHouseFrom.FormattingEnabled = true;
            this.comboWareHouseFrom.Location = new System.Drawing.Point(140, 82);
            this.comboWareHouseFrom.Name = "comboWareHouseFrom";
            this.comboWareHouseFrom.Size = new System.Drawing.Size(202, 21);
            this.comboWareHouseFrom.TabIndex = 66;
            this.comboWareHouseFrom.SelectedIndexChanged += new System.EventHandler(this.comboWareHouseFrom_SelectedIndexChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(17, 85);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(125, 13);
            this.label2.TabIndex = 69;
            this.label2.Text = "From Warehouse Name :";
            // 
            // comboCompanyName
            // 
            this.comboCompanyName.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboCompanyName.Enabled = false;
            this.comboCompanyName.FormattingEnabled = true;
            this.comboCompanyName.Location = new System.Drawing.Point(140, 54);
            this.comboCompanyName.Name = "comboCompanyName";
            this.comboCompanyName.Size = new System.Drawing.Size(249, 21);
            this.comboCompanyName.TabIndex = 65;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(53, 56);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(88, 13);
            this.label1.TabIndex = 68;
            this.label1.Text = "Company Name :";
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
            this.dataGridView1.Location = new System.Drawing.Point(18, 106);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.Size = new System.Drawing.Size(1190, 268);
            this.dataGridView1.TabIndex = 72;
            this.dataGridView1.CellFormatting += new System.Windows.Forms.DataGridViewCellFormattingEventHandler(this.dataGridView1_CellFormatting);
            this.dataGridView1.CellValueChanged += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridView1_CellValueChanged);
            // 
            // Column1
            // 
            this.Column1.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCells;
            this.Column1.HeaderText = "#";
            this.Column1.Name = "Column1";
            this.Column1.TrueValue = "";
            this.Column1.Width = 21;
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.editRollToolStripMenuItem,
            this.selectCheckRollsToolStripMenuItem});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(162, 48);
            this.contextMenuStrip1.Text = "Edit Roll";
            // 
            // editRollToolStripMenuItem
            // 
            this.editRollToolStripMenuItem.Name = "editRollToolStripMenuItem";
            this.editRollToolStripMenuItem.Size = new System.Drawing.Size(161, 22);
            this.editRollToolStripMenuItem.Text = "Edit Roll";
            this.editRollToolStripMenuItem.Click += new System.EventHandler(this.editRollToolStripMenuItem_Click);
            // 
            // selectCheckRollsToolStripMenuItem
            // 
            this.selectCheckRollsToolStripMenuItem.Name = "selectCheckRollsToolStripMenuItem";
            this.selectCheckRollsToolStripMenuItem.Size = new System.Drawing.Size(161, 22);
            this.selectCheckRollsToolStripMenuItem.Text = "Select/Check Rolls";
            this.selectCheckRollsToolStripMenuItem.Click += new System.EventHandler(this.selectCheckRollsToolStripMenuItem_Click);
            // 
            // btnTransfer
            // 
            this.btnTransfer.Image = ((System.Drawing.Image)(resources.GetObject("btnTransfer.Image")));
            this.btnTransfer.Location = new System.Drawing.Point(1013, 75);
            this.btnTransfer.Name = "btnTransfer";
            this.btnTransfer.Size = new System.Drawing.Size(73, 30);
            this.btnTransfer.TabIndex = 73;
            this.btnTransfer.Text = "Transfer";
            this.btnTransfer.UseVisualStyleBackColor = true;
            this.btnTransfer.Click += new System.EventHandler(this.btnTransfer_Click);
            // 
            // label4
            // 
            this.label4.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 6.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.Location = new System.Drawing.Point(18, 379);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(350, 12);
            this.label4.TabIndex = 74;
            this.label4.Text = "* For Outside Roll: Party Roll Number will be display in Marking Column of above " +
                "grid";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(395, 57);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(62, 13);
            this.label5.TabIndex = 76;
            this.label5.Text = "From Date :";
            // 
            // dateTimePicker2
            // 
            this.dateTimePicker2.Location = new System.Drawing.Point(463, 54);
            this.dateTimePicker2.Name = "dateTimePicker2";
            this.dateTimePicker2.Size = new System.Drawing.Size(89, 20);
            this.dateTimePicker2.TabIndex = 75;
            // 
            // button1
            // 
            this.button1.Image = ((System.Drawing.Image)(resources.GetObject("button1.Image")));
            this.button1.Location = new System.Drawing.Point(793, 77);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(30, 27);
            this.button1.TabIndex = 77;
            this.button1.Text = "Ok";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // label7
            // 
            this.label7.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(15, 400);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(68, 13);
            this.label7.TabIndex = 70;
            this.label7.Text = "Party Name :";
            this.label7.Visible = false;
            // 
            // ddlPartyName
            // 
            this.ddlPartyName.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.ddlPartyName.FormattingEnabled = true;
            this.ddlPartyName.Location = new System.Drawing.Point(89, 396);
            this.ddlPartyName.Name = "ddlPartyName";
            this.ddlPartyName.Size = new System.Drawing.Size(242, 21);
            this.ddlPartyName.TabIndex = 66;
            this.ddlPartyName.Visible = false;
            // 
            // label8
            // 
            this.label8.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(333, 400);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(52, 13);
            this.label8.TabIndex = 70;
            this.label8.Text = "Purpose :";
            this.label8.Visible = false;
            // 
            // ddlPurpose
            // 
            this.ddlPurpose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.ddlPurpose.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.ddlPurpose.FormattingEnabled = true;
            this.ddlPurpose.Items.AddRange(new object[] {
            "Sales",
            "Going for Job Work",
            "Received for Job Work",
            "Export"});
            this.ddlPurpose.Location = new System.Drawing.Point(388, 396);
            this.ddlPurpose.Name = "ddlPurpose";
            this.ddlPurpose.Size = new System.Drawing.Size(128, 21);
            this.ddlPurpose.TabIndex = 66;
            this.ddlPurpose.Visible = false;
            // 
            // label9
            // 
            this.label9.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(516, 400);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(37, 13);
            this.label9.TabIndex = 70;
            this.label9.Text = "Type :";
            this.label9.Visible = false;
            // 
            // ddlType
            // 
            this.ddlType.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.ddlType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.ddlType.FormattingEnabled = true;
            this.ddlType.Items.AddRange(new object[] {
            "Laminated",
            "UnLaminated",
            "Webbing",
            "Liner"});
            this.ddlType.Location = new System.Drawing.Point(557, 396);
            this.ddlType.Name = "ddlType";
            this.ddlType.Size = new System.Drawing.Size(75, 21);
            this.ddlType.TabIndex = 66;
            this.ddlType.Visible = false;
            // 
            // label10
            // 
            this.label10.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(634, 400);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(48, 13);
            this.label10.TabIndex = 70;
            this.label10.Text = "Inv. No :";
            this.label10.Visible = false;
            // 
            // txtInvNo
            // 
            this.txtInvNo.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.txtInvNo.Location = new System.Drawing.Point(684, 396);
            this.txtInvNo.Name = "txtInvNo";
            this.txtInvNo.Size = new System.Drawing.Size(100, 20);
            this.txtInvNo.TabIndex = 78;
            // 
            // label11
            // 
            this.label11.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(785, 400);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(55, 13);
            this.label11.TabIndex = 70;
            this.label11.Text = "Remarks :";
            this.label11.Visible = false;
            // 
            // txtRemarks
            // 
            this.txtRemarks.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.txtRemarks.Location = new System.Drawing.Point(842, 382);
            this.txtRemarks.Multiline = true;
            this.txtRemarks.Name = "txtRemarks";
            this.txtRemarks.Size = new System.Drawing.Size(167, 35);
            this.txtRemarks.TabIndex = 79;
            this.txtRemarks.Visible = false;
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(823, 84);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(78, 13);
            this.label12.TabIndex = 71;
            this.label12.Text = "Transfer Date :";
            // 
            // dateTransfer
            // 
            this.dateTransfer.Location = new System.Drawing.Point(907, 81);
            this.dateTransfer.Name = "dateTransfer";
            this.dateTransfer.Size = new System.Drawing.Size(99, 20);
            this.dateTransfer.TabIndex = 64;
            // 
            // textBox2
            // 
            this.textBox2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.textBox2.BackColor = System.Drawing.SystemColors.GradientActiveCaption;
            this.textBox2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox2.Enabled = false;
            this.textBox2.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox2.ForeColor = System.Drawing.Color.Black;
            this.textBox2.Location = new System.Drawing.Point(12, 12);
            this.textBox2.Name = "textBox2";
            this.textBox2.Size = new System.Drawing.Size(1196, 22);
            this.textBox2.TabIndex = 419;
            this.textBox2.TabStop = false;
            this.textBox2.Text = "Roll Transfer";
            this.textBox2.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.comboRollForm);
            this.groupBox1.Controls.Add(this.lblInfo);
            this.groupBox1.Controls.Add(this.button2);
            this.groupBox1.Controls.Add(this.label13);
            this.groupBox1.Controls.Add(this.textRollNo);
            this.groupBox1.Location = new System.Drawing.Point(727, 37);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(303, 38);
            this.groupBox1.TabIndex = 420;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Search Roll";
            // 
            // comboRollForm
            // 
            this.comboRollForm.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboRollForm.FormattingEnabled = true;
            this.comboRollForm.Items.AddRange(new object[] {
            "In",
            "Out"});
            this.comboRollForm.Location = new System.Drawing.Point(161, 12);
            this.comboRollForm.Name = "comboRollForm";
            this.comboRollForm.Size = new System.Drawing.Size(46, 21);
            this.comboRollForm.TabIndex = 421;
            // 
            // lblInfo
            // 
            this.lblInfo.AutoSize = true;
            this.lblInfo.Location = new System.Drawing.Point(269, 16);
            this.lblInfo.Name = "lblInfo";
            this.lblInfo.Size = new System.Drawing.Size(30, 13);
            this.lblInfo.TabIndex = 422;
            this.lblInfo.Text = "(info)";
            this.lblInfo.Click += new System.EventHandler(this.lblInfo_Click);
            // 
            // button2
            // 
            this.button2.Image = ((System.Drawing.Image)(resources.GetObject("button2.Image")));
            this.button2.Location = new System.Drawing.Point(211, 9);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(57, 27);
            this.button2.TabIndex = 421;
            this.button2.Text = "Search";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(6, 19);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(45, 13);
            this.label13.TabIndex = 421;
            this.label13.Text = "Roll No:";
            // 
            // textRollNo
            // 
            this.textRollNo.Location = new System.Drawing.Point(52, 14);
            this.textRollNo.Name = "textRollNo";
            this.textRollNo.Size = new System.Drawing.Size(103, 20);
            this.textRollNo.TabIndex = 0;
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Location = new System.Drawing.Point(664, 85);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(43, 13);
            this.label14.TabIndex = 71;
            this.label14.Text = "Status :";
            // 
            // cmbStatus
            // 
            this.cmbStatus.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbStatus.FormattingEnabled = true;
            this.cmbStatus.Items.AddRange(new object[] {
            "Loom",
            "NeedleLoom"});
            this.cmbStatus.Location = new System.Drawing.Point(709, 80);
            this.cmbStatus.Name = "cmbStatus";
            this.cmbStatus.Size = new System.Drawing.Size(81, 21);
            this.cmbStatus.TabIndex = 67;
            this.cmbStatus.SelectedIndexChanged += new System.EventHandler(this.cmbStatus_SelectedIndexChanged);
            // 
            // button4
            // 
            this.button4.Image = ((System.Drawing.Image)(resources.GetObject("button4.Image")));
            this.button4.Location = new System.Drawing.Point(1036, 43);
            this.button4.Name = "button4";
            this.button4.Size = new System.Drawing.Size(80, 30);
            this.button4.TabIndex = 73;
            this.button4.Text = "Swap Party";
            this.button4.UseVisualStyleBackColor = true;
            this.button4.Click += new System.EventHandler(this.button3_Click);
            // 
            // btnDelete
            // 
            this.btnDelete.Image = ((System.Drawing.Image)(resources.GetObject("btnDelete.Image")));
            this.btnDelete.Location = new System.Drawing.Point(1092, 74);
            this.btnDelete.Name = "btnDelete";
            this.btnDelete.Size = new System.Drawing.Size(115, 30);
            this.btnDelete.TabIndex = 421;
            this.btnDelete.Text = "Set As Non-Moving";
            this.btnDelete.UseVisualStyleBackColor = true;
            this.btnDelete.Click += new System.EventHandler(this.btnDelete_Click);
            // 
            // TxtMultSel
            // 
            this.TxtMultSel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.TxtMultSel.Location = new System.Drawing.Point(1012, 382);
            this.TxtMultSel.MaxLength = 0;
            this.TxtMultSel.Multiline = true;
            this.TxtMultSel.Name = "TxtMultSel";
            this.TxtMultSel.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.TxtMultSel.Size = new System.Drawing.Size(196, 35);
            this.TxtMultSel.TabIndex = 422;
            this.TxtMultSel.Text = "Search Rolls and Press Enter ";
            this.TxtMultSel.KeyDown += new System.Windows.Forms.KeyEventHandler(this.TxtMultSel_KeyDown);
            // 
            // chkNM
            // 
            this.chkNM.AutoSize = true;
            this.chkNM.Location = new System.Drawing.Point(1123, 49);
            this.chkNM.Name = "chkNM";
            this.chkNM.Size = new System.Drawing.Size(79, 17);
            this.chkNM.TabIndex = 423;
            this.chkNM.Text = "Show N.M.";
            this.chkNM.UseVisualStyleBackColor = true;
            // 
            // FrmWareHouseTransferNew
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1220, 422);
            this.Controls.Add(this.chkNM);
            this.Controls.Add(this.TxtMultSel);
            this.Controls.Add(this.btnDelete);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.textBox2);
            this.Controls.Add(this.txtRemarks);
            this.Controls.Add(this.txtInvNo);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.dateTimePicker2);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.button4);
            this.Controls.Add(this.btnTransfer);
            this.Controls.Add(this.dataGridView1);
            this.Controls.Add(this.label12);
            this.Controls.Add(this.label14);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.dateTransfer);
            this.Controls.Add(this.dateTimePicker1);
            this.Controls.Add(this.cmbStatus);
            this.Controls.Add(this.comboWareHouseTo);
            this.Controls.Add(this.label11);
            this.Controls.Add(this.label10);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.ddlType);
            this.Controls.Add(this.ddlPurpose);
            this.Controls.Add(this.ddlPartyName);
            this.Controls.Add(this.comboWareHouseFrom);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.comboCompanyName);
            this.Controls.Add(this.label1);
            this.Name = "FrmWareHouseTransferNew";
            this.Text = "Roll Transfer Entry";
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.contextMenuStrip1.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.DateTimePicker dateTimePicker1;
        private System.Windows.Forms.ComboBox comboWareHouseTo;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ComboBox comboWareHouseFrom;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox comboCompanyName;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.Button btnTransfer;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.DateTimePicker dateTimePicker2;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.ComboBox ddlPartyName;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.ComboBox ddlPurpose;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.ComboBox ddlType;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.TextBox txtInvNo;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.TextBox txtRemarks;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.DateTimePicker dateTransfer;
        private System.Windows.Forms.TextBox textBox2;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.TextBox textRollNo;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Label lblInfo;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.ComboBox cmbStatus;
        private System.Windows.Forms.Button button4;
        private System.Windows.Forms.ComboBox comboRollForm;
        private System.Windows.Forms.Button btnDelete;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem editRollToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem selectCheckRollsToolStripMenuItem;
        private System.Windows.Forms.TextBox TxtMultSel;
        private System.Windows.Forms.DataGridViewCheckBoxColumn Column1;
        private production.CachedCrMISRollForDispatchDisplay cachedCrMISRollForDispatchDisplay1;
        private System.Windows.Forms.CheckBox chkNM;
    }
}