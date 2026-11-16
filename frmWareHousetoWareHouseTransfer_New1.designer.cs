namespace ERP
{
    partial class frmWareHousetoWareHouseTransfer_New1
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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle13 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle14 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle15 = new System.Windows.Forms.DataGridViewCellStyle();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmWareHousetoWareHouseTransfer_New1));
            this.comboWareHouseTo = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.comboWareHouseFrom = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.comboCompanyName = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.Chk = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.DtItemCode = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dtItemName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.DtGroupName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dtQty = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dtRemarks = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.DtStock = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dtRemainQty = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.DtCategory = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.BtnColumn = new System.Windows.Forms.DataGridViewButtonColumn();
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.selectCheckRollsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.label5 = new System.Windows.Forms.Label();
            this.dateSysdate = new System.Windows.Forms.DateTimePicker();
            this.dataGridViewComboBoxColumn1 = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.dataGridViewComboBoxColumn2 = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.dataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn3 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn4 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dateTimePicker2 = new System.Windows.Forms.DateTimePicker();
            this.label6 = new System.Windows.Forms.Label();
            this.btnok = new System.Windows.Forms.Button();
            this.btnInit = new System.Windows.Forms.Button();
            this.btnUpdate = new System.Windows.Forms.Button();
            this.btnSave = new System.Windows.Forms.Button();
            this.chkStkInHnd = new System.Windows.Forms.CheckBox();
            this.txtRemarks = new System.Windows.Forms.TextBox();
            this.txtInvNo = new System.Windows.Forms.TextBox();
            this.label11 = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.ddlPurpose = new System.Windows.Forms.ComboBox();
            this.ddlPartyName = new System.Windows.Forms.ComboBox();
            this.textBox2 = new System.Windows.Forms.TextBox();
            this.BtnDelete = new System.Windows.Forms.Button();
            this.comboInternalReq = new System.Windows.Forms.ComboBox();
            this.label4 = new System.Windows.Forms.Label();
            this.TxtPackingList = new System.Windows.Forms.TextBox();
            this.label14 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.textpktno = new System.Windows.Forms.TextBox();
            this.button1 = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.contextMenuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // comboWareHouseTo
            // 
            this.comboWareHouseTo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboWareHouseTo.FormattingEnabled = true;
            this.comboWareHouseTo.Items.AddRange(new object[] {
            "FROM ITEM MASTER"});
            this.comboWareHouseTo.Location = new System.Drawing.Point(495, 66);
            this.comboWareHouseTo.Name = "comboWareHouseTo";
            this.comboWareHouseTo.Size = new System.Drawing.Size(228, 21);
            this.comboWareHouseTo.TabIndex = 5;
            this.comboWareHouseTo.SelectedIndexChanged += new System.EventHandler(this.comboWareHouseTo_SelectedIndexChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(378, 70);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(115, 13);
            this.label3.TabIndex = 51;
            this.label3.Text = "To Warehouse Name :";
            // 
            // comboWareHouseFrom
            // 
            this.comboWareHouseFrom.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboWareHouseFrom.FormattingEnabled = true;
            this.comboWareHouseFrom.Location = new System.Drawing.Point(133, 68);
            this.comboWareHouseFrom.Name = "comboWareHouseFrom";
            this.comboWareHouseFrom.Size = new System.Drawing.Size(242, 21);
            this.comboWareHouseFrom.TabIndex = 4;
            this.comboWareHouseFrom.SelectedIndexChanged += new System.EventHandler(this.comboWareHouseFrom_SelectedIndexChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 71);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(125, 13);
            this.label2.TabIndex = 49;
            this.label2.Text = "From Warehouse Name :";
            // 
            // comboCompanyName
            // 
            this.comboCompanyName.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboCompanyName.Enabled = false;
            this.comboCompanyName.FormattingEnabled = true;
            this.comboCompanyName.Location = new System.Drawing.Point(133, 44);
            this.comboCompanyName.Name = "comboCompanyName";
            this.comboCompanyName.Size = new System.Drawing.Size(242, 21);
            this.comboCompanyName.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(42, 46);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(88, 13);
            this.label1.TabIndex = 47;
            this.label1.Text = "Company Name :";
            // 
            // dataGridView1
            // 
            this.dataGridView1.AllowUserToAddRows = false;
            this.dataGridView1.AllowUserToDeleteRows = false;
            this.dataGridView1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.dataGridView1.BackgroundColor = System.Drawing.Color.WhiteSmoke;
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Chk,
            this.DtItemCode,
            this.dtItemName,
            this.DtGroupName,
            this.dtQty,
            this.dtRemarks,
            this.DtStock,
            this.dtRemainQty,
            this.DtCategory,
            this.BtnColumn});
            this.dataGridView1.ContextMenuStrip = this.contextMenuStrip1;
            this.dataGridView1.Location = new System.Drawing.Point(9, 117);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.Size = new System.Drawing.Size(896, 327);
            this.dataGridView1.TabIndex = 6;
            this.dataGridView1.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridView1_CellContentClick);
            this.dataGridView1.CurrentCellChanged += new System.EventHandler(this.dataGridView1_CurrentCellChanged);
            this.dataGridView1.CurrentCellDirtyStateChanged += new System.EventHandler(this.dataGridView1_CurrentCellDirtyStateChanged);
            this.dataGridView1.DataError += new System.Windows.Forms.DataGridViewDataErrorEventHandler(this.dataGridView1_DataError);
            // 
            // Chk
            // 
            this.Chk.HeaderText = "Check";
            this.Chk.Name = "Chk";
            this.Chk.Width = 40;
            // 
            // DtItemCode
            // 
            this.DtItemCode.HeaderText = "ItemCode";
            this.DtItemCode.Name = "DtItemCode";
            this.DtItemCode.ReadOnly = true;
            // 
            // dtItemName
            // 
            this.dtItemName.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCells;
            this.dtItemName.HeaderText = "ItemName";
            this.dtItemName.Name = "dtItemName";
            this.dtItemName.ReadOnly = true;
            this.dtItemName.Width = 80;
            // 
            // DtGroupName
            // 
            this.DtGroupName.HeaderText = "Group Name";
            this.DtGroupName.Name = "DtGroupName";
            this.DtGroupName.ReadOnly = true;
            // 
            // dtQty
            // 
            dataGridViewCellStyle13.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
            this.dtQty.DefaultCellStyle = dataGridViewCellStyle13;
            this.dtQty.HeaderText = "Qty";
            this.dtQty.Name = "dtQty";
            // 
            // dtRemarks
            // 
            this.dtRemarks.HeaderText = "Remarks";
            this.dtRemarks.Name = "dtRemarks";
            this.dtRemarks.Width = 150;
            // 
            // DtStock
            // 
            dataGridViewCellStyle14.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
            this.DtStock.DefaultCellStyle = dataGridViewCellStyle14;
            this.DtStock.HeaderText = "Stock";
            this.DtStock.Name = "DtStock";
            // 
            // dtRemainQty
            // 
            dataGridViewCellStyle15.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
            this.dtRemainQty.DefaultCellStyle = dataGridViewCellStyle15;
            this.dtRemainQty.HeaderText = "Remaining Qty";
            this.dtRemainQty.Name = "dtRemainQty";
            // 
            // DtCategory
            // 
            this.DtCategory.HeaderText = "DtCategory";
            this.DtCategory.Name = "DtCategory";
            // 
            // BtnColumn
            // 
            this.BtnColumn.HeaderText = "Click Here";
            this.BtnColumn.Name = "BtnColumn";
            this.BtnColumn.Text = "Click";
            this.BtnColumn.UseColumnTextForButtonValue = true;
            this.BtnColumn.Visible = false;
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.selectCheckRollsToolStripMenuItem});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(162, 26);
            // 
            // selectCheckRollsToolStripMenuItem
            // 
            this.selectCheckRollsToolStripMenuItem.Name = "selectCheckRollsToolStripMenuItem";
            this.selectCheckRollsToolStripMenuItem.Size = new System.Drawing.Size(161, 22);
            this.selectCheckRollsToolStripMenuItem.Text = "Select/Check Rolls";
            this.selectCheckRollsToolStripMenuItem.Click += new System.EventHandler(this.selectCheckRollsToolStripMenuItem_Click);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(578, 459);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(152, 13);
            this.label5.TabIndex = 59;
            this.label5.Text = "Search saved data From Date:";
            // 
            // dateSysdate
            // 
            this.dateSysdate.Location = new System.Drawing.Point(495, 42);
            this.dateSysdate.Name = "dateSysdate";
            this.dateSysdate.Size = new System.Drawing.Size(139, 20);
            this.dateSysdate.TabIndex = 0;
            // 
            // dataGridViewComboBoxColumn1
            // 
            this.dataGridViewComboBoxColumn1.AutoComplete = false;
            this.dataGridViewComboBoxColumn1.DisplayStyle = System.Windows.Forms.DataGridViewComboBoxDisplayStyle.ComboBox;
            this.dataGridViewComboBoxColumn1.HeaderText = "ItemName";
            this.dataGridViewComboBoxColumn1.Name = "dataGridViewComboBoxColumn1";
            this.dataGridViewComboBoxColumn1.ReadOnly = true;
            this.dataGridViewComboBoxColumn1.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.dataGridViewComboBoxColumn1.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            this.dataGridViewComboBoxColumn1.Width = 300;
            // 
            // dataGridViewComboBoxColumn2
            // 
            this.dataGridViewComboBoxColumn2.HeaderText = "ItemName";
            this.dataGridViewComboBoxColumn2.Name = "dataGridViewComboBoxColumn2";
            this.dataGridViewComboBoxColumn2.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.dataGridViewComboBoxColumn2.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            this.dataGridViewComboBoxColumn2.Width = 300;
            // 
            // dataGridViewTextBoxColumn1
            // 
            this.dataGridViewTextBoxColumn1.HeaderText = "ItemCode";
            this.dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
            this.dataGridViewTextBoxColumn1.ReadOnly = true;
            this.dataGridViewTextBoxColumn1.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.dataGridViewTextBoxColumn1.Width = 130;
            // 
            // dataGridViewTextBoxColumn2
            // 
            this.dataGridViewTextBoxColumn2.HeaderText = "Qty";
            this.dataGridViewTextBoxColumn2.Name = "dataGridViewTextBoxColumn2";
            this.dataGridViewTextBoxColumn2.Width = 200;
            // 
            // dataGridViewTextBoxColumn3
            // 
            this.dataGridViewTextBoxColumn3.HeaderText = "Remarks";
            this.dataGridViewTextBoxColumn3.Name = "dataGridViewTextBoxColumn3";
            this.dataGridViewTextBoxColumn3.ReadOnly = true;
            this.dataGridViewTextBoxColumn3.Width = 200;
            // 
            // dataGridViewTextBoxColumn4
            // 
            this.dataGridViewTextBoxColumn4.HeaderText = "Stock";
            this.dataGridViewTextBoxColumn4.Name = "dataGridViewTextBoxColumn4";
            this.dataGridViewTextBoxColumn4.ReadOnly = true;
            // 
            // dateTimePicker2
            // 
            this.dateTimePicker2.CustomFormat = "dd-MM-yyyy";
            this.dateTimePicker2.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dateTimePicker2.Location = new System.Drawing.Point(734, 455);
            this.dateTimePicker2.Name = "dateTimePicker2";
            this.dateTimePicker2.Size = new System.Drawing.Size(106, 20);
            this.dateTimePicker2.TabIndex = 62;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(454, 46);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(39, 13);
            this.label6.TabIndex = 63;
            this.label6.Text = " Date :";
            // 
            // btnok
            // 
            this.btnok.Image = ((System.Drawing.Image)(resources.GetObject("btnok.Image")));
            this.btnok.Location = new System.Drawing.Point(846, 450);
            this.btnok.Name = "btnok";
            this.btnok.Size = new System.Drawing.Size(60, 30);
            this.btnok.TabIndex = 61;
            this.btnok.UseVisualStyleBackColor = true;
            this.btnok.Click += new System.EventHandler(this.btnok_Click);
            // 
            // btnInit
            // 
            this.btnInit.Image = ((System.Drawing.Image)(resources.GetObject("btnInit.Image")));
            this.btnInit.Location = new System.Drawing.Point(11, 450);
            this.btnInit.Name = "btnInit";
            this.btnInit.Size = new System.Drawing.Size(85, 30);
            this.btnInit.TabIndex = 9;
            this.btnInit.UseVisualStyleBackColor = true;
            this.btnInit.Click += new System.EventHandler(this.button3_Click);
            // 
            // btnUpdate
            // 
            this.btnUpdate.Enabled = false;
            this.btnUpdate.Image = ((System.Drawing.Image)(resources.GetObject("btnUpdate.Image")));
            this.btnUpdate.Location = new System.Drawing.Point(183, 450);
            this.btnUpdate.Name = "btnUpdate";
            this.btnUpdate.Size = new System.Drawing.Size(85, 30);
            this.btnUpdate.TabIndex = 8;
            this.btnUpdate.UseVisualStyleBackColor = true;
            this.btnUpdate.Click += new System.EventHandler(this.button2_Click);
            // 
            // btnSave
            // 
            this.btnSave.Image = ((System.Drawing.Image)(resources.GetObject("btnSave.Image")));
            this.btnSave.Location = new System.Drawing.Point(102, 450);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(75, 30);
            this.btnSave.TabIndex = 7;
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.button1_Click);
            // 
            // chkStkInHnd
            // 
            this.chkStkInHnd.AutoSize = true;
            this.chkStkInHnd.Location = new System.Drawing.Point(734, 66);
            this.chkStkInHnd.Name = "chkStkInHnd";
            this.chkStkInHnd.Size = new System.Drawing.Size(137, 17);
            this.chkStkInHnd.TabIndex = 64;
            this.chkStkInHnd.Text = "Show StockInHand = 0";
            this.chkStkInHnd.UseVisualStyleBackColor = true;
            this.chkStkInHnd.CheckedChanged += new System.EventHandler(this.checkBox1_CheckedChanged);
            // 
            // txtRemarks
            // 
            this.txtRemarks.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.txtRemarks.Location = new System.Drawing.Point(759, 497);
            this.txtRemarks.Multiline = true;
            this.txtRemarks.Name = "txtRemarks";
            this.txtRemarks.Size = new System.Drawing.Size(146, 22);
            this.txtRemarks.TabIndex = 89;
            this.txtRemarks.Visible = false;
            // 
            // txtInvNo
            // 
            this.txtInvNo.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.txtInvNo.Location = new System.Drawing.Point(586, 499);
            this.txtInvNo.Name = "txtInvNo";
            this.txtInvNo.Size = new System.Drawing.Size(100, 20);
            this.txtInvNo.TabIndex = 88;
            this.txtInvNo.Visible = false;
            // 
            // label11
            // 
            this.label11.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(698, 502);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(55, 13);
            this.label11.TabIndex = 85;
            this.label11.Text = "Remarks :";
            this.label11.Visible = false;
            // 
            // label10
            // 
            this.label10.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(535, 502);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(48, 13);
            this.label10.TabIndex = 86;
            this.label10.Text = "Inv. No :";
            this.label10.Visible = false;
            // 
            // label8
            // 
            this.label8.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(331, 502);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(52, 13);
            this.label8.TabIndex = 83;
            this.label8.Text = "Purpose :";
            this.label8.Visible = false;
            // 
            // label7
            // 
            this.label7.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(7, 502);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(68, 13);
            this.label7.TabIndex = 84;
            this.label7.Text = "Party Name :";
            this.label7.Visible = false;
            // 
            // ddlPurpose
            // 
            this.ddlPurpose.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.ddlPurpose.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.ddlPurpose.FormattingEnabled = true;
            this.ddlPurpose.Items.AddRange(new object[] {
            "Sales",
            "Export",
            "Going for Job Work",
            "Received for Job Work",
            "Export"});
            this.ddlPurpose.Location = new System.Drawing.Point(384, 499);
            this.ddlPurpose.Name = "ddlPurpose";
            this.ddlPurpose.Size = new System.Drawing.Size(128, 21);
            this.ddlPurpose.TabIndex = 81;
            this.ddlPurpose.Visible = false;
            // 
            // ddlPartyName
            // 
            this.ddlPartyName.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.ddlPartyName.FormattingEnabled = true;
            this.ddlPartyName.Location = new System.Drawing.Point(76, 499);
            this.ddlPartyName.Name = "ddlPartyName";
            this.ddlPartyName.Size = new System.Drawing.Size(242, 21);
            this.ddlPartyName.TabIndex = 82;
            this.ddlPartyName.Visible = false;
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
            this.textBox2.Location = new System.Drawing.Point(12, 11);
            this.textBox2.Name = "textBox2";
            this.textBox2.Size = new System.Drawing.Size(894, 25);
            this.textBox2.TabIndex = 419;
            this.textBox2.TabStop = false;
            this.textBox2.Text = "RAW MATERIAL TRANSFER ";
            this.textBox2.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // BtnDelete
            // 
            this.BtnDelete.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(192)))), ((int)(((byte)(255)))));
            this.BtnDelete.Location = new System.Drawing.Point(274, 450);
            this.BtnDelete.Name = "BtnDelete";
            this.BtnDelete.Size = new System.Drawing.Size(75, 30);
            this.BtnDelete.TabIndex = 420;
            this.BtnDelete.Text = "Delete";
            this.BtnDelete.UseVisualStyleBackColor = false;
            this.BtnDelete.Visible = false;
            this.BtnDelete.Click += new System.EventHandler(this.BtnDelete_Click);
            // 
            // comboInternalReq
            // 
            this.comboInternalReq.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboInternalReq.FormattingEnabled = true;
            this.comboInternalReq.Items.AddRange(new object[] {
            "FROM ITEM MASTER"});
            this.comboInternalReq.Location = new System.Drawing.Point(133, 93);
            this.comboInternalReq.Name = "comboInternalReq";
            this.comboInternalReq.Size = new System.Drawing.Size(242, 21);
            this.comboInternalReq.TabIndex = 449;
            this.comboInternalReq.SelectedIndexChanged += new System.EventHandler(this.comboInternalReq_SelectedIndexChanged);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(16, 97);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(92, 13);
            this.label4.TabIndex = 450;
            this.label4.Text = "Interal Requistion:";
            // 
            // TxtPackingList
            // 
            this.TxtPackingList.Enabled = false;
            this.TxtPackingList.Location = new System.Drawing.Point(495, 93);
            this.TxtPackingList.Name = "TxtPackingList";
            this.TxtPackingList.Size = new System.Drawing.Size(119, 20);
            this.TxtPackingList.TabIndex = 451;
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F);
            this.label14.Location = new System.Drawing.Point(405, 97);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(88, 13);
            this.label14.TabIndex = 452;
            this.label14.Text = "Packing List No :";
            // 
            // label9
            // 
            this.label9.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(9, 539);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(82, 13);
            this.label9.TabIndex = 453;
            this.label9.Text = "PackingListNo :";
            // 
            // textpktno
            // 
            this.textpktno.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.textpktno.Location = new System.Drawing.Point(97, 536);
            this.textpktno.Name = "textpktno";
            this.textpktno.Size = new System.Drawing.Size(262, 20);
            this.textpktno.TabIndex = 454;
            this.textpktno.Leave += new System.EventHandler(this.textpktno_Leave);
            // 
            // button1
            // 
            this.button1.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.button1.Location = new System.Drawing.Point(365, 534);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(101, 23);
            this.button1.TabIndex = 457;
            this.button1.Text = "Update InvNo";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click_1);
            // 
            // frmWareHousetoWareHouseTransfer_New1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.WhiteSmoke;
            this.ClientSize = new System.Drawing.Size(917, 561);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.textpktno);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.TxtPackingList);
            this.Controls.Add(this.label14);
            this.Controls.Add(this.comboInternalReq);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.BtnDelete);
            this.Controls.Add(this.textBox2);
            this.Controls.Add(this.txtRemarks);
            this.Controls.Add(this.txtInvNo);
            this.Controls.Add(this.label11);
            this.Controls.Add(this.label10);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.ddlPurpose);
            this.Controls.Add(this.ddlPartyName);
            this.Controls.Add(this.chkStkInHnd);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.dateTimePicker2);
            this.Controls.Add(this.btnok);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.dateSysdate);
            this.Controls.Add(this.btnInit);
            this.Controls.Add(this.btnUpdate);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.dataGridView1);
            this.Controls.Add(this.comboWareHouseTo);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.comboWareHouseFrom);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.comboCompanyName);
            this.Controls.Add(this.label1);
            this.Name = "frmWareHousetoWareHouseTransfer_New1";
            this.Text = "WareHouse Transfer";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.Load += new System.EventHandler(this.frmWareHousetoWareHouseTransfer_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.contextMenuStrip1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox comboWareHouseTo;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ComboBox comboWareHouseFrom;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox comboCompanyName;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;
        private System.Windows.Forms.DataGridViewComboBoxColumn dataGridViewComboBoxColumn1;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn2;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn3;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnUpdate;
        private System.Windows.Forms.Button btnInit;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.DateTimePicker dateSysdate;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn4;
        private System.Windows.Forms.DataGridViewComboBoxColumn dataGridViewComboBoxColumn2;
        private System.Windows.Forms.Button btnok;
        private System.Windows.Forms.DateTimePicker dateTimePicker2;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.CheckBox chkStkInHnd;
        private System.Windows.Forms.TextBox txtRemarks;
        private System.Windows.Forms.TextBox txtInvNo;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.ComboBox ddlPurpose;
        private System.Windows.Forms.ComboBox ddlPartyName;
        private System.Windows.Forms.TextBox textBox2;
        private System.Windows.Forms.Button BtnDelete;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem selectCheckRollsToolStripMenuItem;
        private System.Windows.Forms.ComboBox comboInternalReq;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox TxtPackingList;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.DataGridViewCheckBoxColumn Chk;
        private System.Windows.Forms.DataGridViewTextBoxColumn DtItemCode;
        private System.Windows.Forms.DataGridViewTextBoxColumn dtItemName;
        private System.Windows.Forms.DataGridViewTextBoxColumn DtGroupName;
        private System.Windows.Forms.DataGridViewTextBoxColumn dtQty;
        private System.Windows.Forms.DataGridViewTextBoxColumn dtRemarks;
        private System.Windows.Forms.DataGridViewTextBoxColumn DtStock;
        private System.Windows.Forms.DataGridViewTextBoxColumn dtRemainQty;
        private System.Windows.Forms.DataGridViewTextBoxColumn DtCategory;
        private System.Windows.Forms.DataGridViewButtonColumn BtnColumn;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.TextBox textpktno;
        private System.Windows.Forms.Button button1;
    }
}