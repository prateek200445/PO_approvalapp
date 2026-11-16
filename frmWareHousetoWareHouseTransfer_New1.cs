using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Collections;

namespace ERP
{

    public partial class frmWareHousetoWareHouseTransfer_New1 : Form
    {
        int GroupSrNo = 0;

        private filter.filterData filterData;
        DataTable dtYarnDespatch;
        public frmWareHousetoWareHouseTransfer_New1()
        {
            InitializeComponent();
            Form_Load();
            //dataGridView1.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(229, 218, 251);

        }
        //This overloaded function is called from Production report. To load Data from reporting purpose
        public frmWareHousetoWareHouseTransfer_New1(string Qry, string warehouse)
        {
            InitializeComponent();
            Form_Load();
            fillData(Qry);
            comboWareHouseFrom.Text = warehouse;

            if (comboWareHouseFrom.SelectedIndex > -1)
                btnSave.Enabled = true;
            else
                btnSave.Enabled = false;
        }

        private void Form_Load()
        {
            //dateSysdate.CustomFormat = "yyyy-MM-dd";
            dateSysdate.CustomFormat = Tools.ToolsCommonFunction.ERPDateSettings();
            dateSysdate.Format = DateTimePickerFormat.Custom;
            dateSysdate.MaxDate = DateTime.Now;
            dateSysdate.MinDate = ProductionCommonFunction.ERPBackDateSettings();
            filterData = new filter.filterData(dataGridView1);

            dateTimePicker2.Value = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            dateTimePicker2.MaxDate = DateTime.Now;

            comboCompanyName.DataSource = ProductionCommonFunction.GetCompanyList();
            comboCompanyName.DisplayMember = ProductionCommonVariables.FieldName.CompanyName;
            comboCompanyName.ValueMember = ProductionCommonVariables.FieldName.IndentCode;

            comboCompanyName.Text = frmDefaultVale.CompanyName;

            if (FrmMain.IsProductionadmin)
            {
                comboWareHouseFrom.DataSource = ProductionCommonFunction.GetWareHouseList(comboCompanyName.Text);
                comboWareHouseFrom.DisplayMember = ProductionCommonVariables.FieldName.WareHouse;
                comboWareHouseFrom.ValueMember = ProductionCommonVariables.FieldName.SrNo;
            }
            else
            {
                comboWareHouseFrom.DataSource = ProductionCommonFunction.GetWareHouse_userWise();
                comboWareHouseFrom.DisplayMember = ProductionCommonVariables.FieldName.WareHouse;
                comboWareHouseFrom.ValueMember = ProductionCommonVariables.FieldName.SrNo;

                comboWareHouseTo.DataSource = ProductionCommonFunction.GetWareHouseList(comboCompanyName.Text);
                comboWareHouseTo.DisplayMember = ProductionCommonVariables.FieldName.WareHouse;
                comboWareHouseTo.ValueMember = ProductionCommonVariables.FieldName.SrNo;

            }
            if (comboWareHouseFrom.Items.Count > 0)
                comboWareHouseFrom.SelectedIndex = -1;
            btnUpdate.Enabled = false;

            if (FrmMain.IsAdmin)
                BtnDelete.Visible = true;

        }
        private void frmWareHousetoWareHouseTransfer_Load(object sender, EventArgs e)
        {
            Form_Load();
        }

        void fillData(string Qry)
        {
            if (Database.OpenConnection(Utility.MaterialConnectionString))
            {

                try
                {

                    dataGridView1.Rows.Clear();
                    string where = "";

                    if (chkStkInHnd.Checked != true)
                    {
                        where = " and ROUND(warehouse.stkinhand,2) >=1";
                    }
                    if (Qry.Length == 0)
                    {
                        string st = "";
                        if (FrmMain.UserName.ToLower() == "erp")
                        {
                            st = "select warehouse.Itemcode,warehouse.ItemName,warehouse.GroupName,0 as [Qty],'.' as [Remarks],ROUND(warehouse.stkinhand,2) AS stkinhand,ROUND(warehouse.stkinhand,2) as [RemainingQty],misgrupitem from " +
                                    " warehouse inner join item on item.itemcode = warehouse.itemcode and item.companyname = warehouse.companyname where warehouse.companyname = '" + comboCompanyName.Text + "' and warehouse.warehousename = '" +
                                    comboWareHouseFrom.Text + "'  order by warehouse.Itemname";
                        }
                        else
                        {
                            st = "select warehouse.Itemcode,warehouse.ItemName,warehouse.GroupName,0 as [Qty],'.' as [Remarks],ROUND(warehouse.stkinhand,2) AS stkinhand,ROUND(warehouse.stkinhand,2) as [RemainingQty],misgrupitem from " +
                                   " warehouse inner join item on item.itemcode = warehouse.itemcode and item.companyname = warehouse.companyname where warehouse.companyname = '" + comboCompanyName.Text + "' and warehouse.warehousename = '" +
                                   comboWareHouseFrom.Text + "' " + where +
                                   " and (('" + dateSysdate.Value.ToString("yyyy-MM-dd") + "' BETWEEN FROMDATE AND TODATE) OR (FROMDATE <='" +
                                   dateSysdate.Value.ToString("yyyy-MM-dd") + "' AND TODATE IS NULL))   order by warehouse.Itemname";

                        }
                        Database.myadapter = Database.GetAdapterCommand(st);
                    }
                    else
                    {
                        Database.myadapter = Database.GetAdapterCommand(Qry + " " + where + " order by Itemname");
                    }
                    DataTable dt = new DataTable();
                    Database.myadapter.Fill(dt);

                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        dataGridView1.Rows.Add(false,
                        dt.Rows[i][0],
                        dt.Rows[i][1],
                        dt.Rows[i][2],
                        dt.Rows[i][3],
                        dt.Rows[i][4],
                        dt.Rows[i][5],
                        dt.Rows[i][6],
                        dt.Rows[i][7]
                        );
                    }
                }

                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }
        }

        private void dataGridView1_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            try
            {
                if (dataGridView1.IsCurrentCellDirty)
                {
                    dataGridView1.CommitEdit(DataGridViewDataErrorContexts.Commit);

                    string itemcodes = "WIP00005,WIP00006,WIP00007,WIP00008,WIP00023,WIP00024,WIP00025,WIP00026,WIP00032,WIP00033,WIP00034";

                    string itemcode = Convert.ToString(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[DtItemCode.Index].Value);
                    if (itemcodes.Contains(itemcode))
                    {
                        dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells["Chk"].Value = false;
                        dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells["dtQty"].Value = "0";
                        return;
                    }


                    if (dataGridView1.CurrentRow.Cells.IndexOf(dataGridView1.CurrentCell) == 4)
                    {
                        DataGridViewTextBoxCell Col1 = (DataGridViewTextBoxCell)dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells["dtQty"];
                        DataGridViewTextBoxCell Col2 = (DataGridViewTextBoxCell)dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells["DtStock"];
                        DataGridViewTextBoxCell Col3 = (DataGridViewTextBoxCell)dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells["dtRemainQty"];

                        Col3.Value = Math.Round(Convert.ToDouble(Utility.ConverttoInt(Col2.FormattedValue.ToString())), 2) -
                                     Math.Round(Convert.ToDouble(Utility.ConverttoInt(Col1.FormattedValue.ToString())), 2);

                        if ((bool)dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells["Chk"].Value == false && Convert.ToDouble(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells["DtStock"].Value) > 0)
                        {
                            dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells["Chk"].Value = true;
                        }
                        else if ((bool)dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells["Chk"].Value == true && Convert.ToDouble(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells["DtStock"].Value) == 0)
                        {
                            dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells["Chk"].Value = false;
                        }

                        if (Convert.ToDouble(Col3.Value) < 0)
                        {
                            dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells["dtRemainQty"].Style.ForeColor = Color.Red;
                        }
                        else
                        {
                            dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells["dtRemainQty"].Style.ForeColor = Color.Black;
                        }
                    }

                }

            }
            catch (Exception ex)
            {
                ex.ToString();
            }
        }

        private void fillgridwithReq()
        {
            comboInternalReq.Items.Clear();
            comboInternalReq.Text = "";

            Database.OpenConnection(Utility.MaterialConnectionString);
            Database.myreader = Database.GetExecuteReaderCommand(" select code from warehousePurchaseReq where towarehouse = '"
                 + comboWareHouseFrom.Text + "' and isapprove = 'yes' and companyname = '" + comboCompanyName.Text + "' and fromwarehouse = '"
                 + comboWareHouseTo.Text + "'");
            while (Database.myreader.Read())
                comboInternalReq.Items.Add(Database.myreader[0].ToString());
            Database.myreader.Close();

            Database.Closeconnection();

            if (comboInternalReq.Items.Count > 0)
                comboInternalReq.SelectedIndex = 0;

        }

        private void comboWareHouseFrom_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                fillData("");
                comboWareHouseTo.DataSource = null;
                comboWareHouseTo.DataSource = ProductionCommonFunction.GetWareHouse_frmWareHouse(comboWareHouseFrom.Text, comboCompanyName.Text);
                comboWareHouseTo.DisplayMember = ProductionCommonVariables.FieldName.WareHouse;
                comboWareHouseTo.ValueMember = ProductionCommonVariables.FieldName.SrNo;
                if (comboWareHouseFrom.Text == "Inhouse Liner Bag Godown")
                {
                    dataGridView1.Columns["BtnColumn"].Visible = true;
                    dataGridView1.Columns["dtQty"].ReadOnly = true;
                }
                else
                {
                    dataGridView1.Columns["BtnColumn"].Visible = false;
                    dataGridView1.Columns["dtQty"].ReadOnly = false;
                }

                fillgridwithReq();

            }
            catch (Exception ex)
            {
                ex.ToString();
            }

        }

        private void button3_Click(object sender, EventArgs e)
        {
            btnSave.Enabled = true;
            btnUpdate.Enabled = false;
            dataGridView1.Rows.Clear();
            ddlPartyName.Text = "";
            ddlPurpose.Text = "";
            txtInvNo.Text = "";
            txtRemarks.Text = "";
            ddlPurpose.SelectedIndex = -1;
            TxtPackingList.Text = "";
            //dataGridView1.Columns.Remove("ID");
        }
        public void Validations()
        {
            //try
            //{
            double stkinhand = 0;
            if (comboWareHouseTo.SelectedIndex == -1)
            {
                throw new ERP.ERPErrorException("Please Select ToWarehouse Name.");
            }
            if (!Utility.CheckDateFrize_Production(dateSysdate.Value.ToString("yyyy-MM-dd"), comboCompanyName.Text))
            {
                throw new ERP.ERPErrorException("Entry on this Date is Freezed. So, You Can't do Entry.");
            }
            if (comboWareHouseFrom.Text == comboWareHouseTo.Text)
            {
                throw new ERP.ERPErrorException("FromGodown and ToGodown should not be same");
            }
            if (comboWareHouseFrom.Text.Length == 0 || comboWareHouseTo.Text.Length == 0)
            {
                throw new ERP.ERPErrorException("Please Select FromGodown/ToGodown");
            }
            if (Database.OpenConnection(Utility.MaterialConnectionString))
            {
                for (int i = 0; i < dataGridView1.Rows.Count; i++)
                {
                    if ((bool)dataGridView1.Rows[i].Cells[0].Value)
                    {
                        Database.myreader = Database.GetExecuteReaderCommand("select stkinhand from warehouse  where companyname = '"
                        + comboCompanyName.Text + "' and warehousename = '" + comboWareHouseFrom.Text
                        + "' and itemcode = '" + dataGridView1["DtItemCode", i].Value + "'");
                        if (Database.myreader.Read())
                            stkinhand = Convert.ToDouble(Database.myreader["stkinhand"].ToString());
                        Database.myreader.Close();
                        stkinhand = stkinhand - Convert.ToDouble(dataGridView1["dtQty", i].Value);
                        if (Math.Round(stkinhand, 2) < 0.0)
                        {
                            throw new ERP.ERPErrorException("'" + dataGridView1["dtItemName", i].Value + "' has stock less from Warehouse,so cant issue ");
                            // return false;
                        }
                    }
                }
            }

            bool ischek = false;

            for (int i = 0; i < dataGridView1.Rows.Count; i++)
            {
                //if (ischek)
                //    break;
                if (Convert.ToString(dataGridView1.Rows[i].Cells[0].FormattedValue).Trim() == "True")
                {
                    if (Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells["dtQty"].FormattedValue) > 0)
                        ischek = true;
                    else
                        throw new ERP.ERPErrorException("Please Check any one Checkbox to Proceed. And also enter proper qty to transfer.");
                }
            }
            if (ischek == false)
                throw new ERP.ERPErrorException("Please Check any one Checkbox to Proceed. And also enter proper qty to transfer.");


        }

        private void button1_Click(object sender, EventArgs e)
        {
            string PackingListNo = "";
            double TotalYarn = 0;
            try
            {
                //if (
                Validations();
                // )

                if (comboWareHouseTo.Text == "Despatch Godown")
                {

                    if (ddlPartyName.Text.Length == 0 || ddlPartyName.Text == "")
                    {
                        MessageBox.Show("Please Select Party Name.");
                        return;
                    }
                    if (ddlPurpose.SelectedIndex == -1)
                    {
                        MessageBox.Show("Please Select Purpose Name.");
                        return;
                    }

                    if (txtInvNo.Text.Trim().Length == 0)
                    {
                        MessageBox.Show("Please Write Invoice Number.");
                        return;
                    }
                    //Generate Auto Number For PackingList...
                    TxtPackingList.Text = "";
                    PackingListNo = ProductionCommonFunction.GeneratePackingListInvno(comboCompanyName.Text);
                    TxtPackingList.Text = PackingListNo;

                    if ((comboWareHouseFrom.Text == "Multi Filament Godown" || comboWareHouseFrom.Text == "TFO Godown"))
                    {

                        for (int i = 0; i <= dataGridView1.Rows.Count - 1; i++)
                        {
                            if ((bool)dataGridView1.Rows[i].Cells[0].Value == true && Utility.SafeConvertToDouble(dataGridView1["dtQty", i].FormattedValue.ToString()) > 0)
                            {
                                if (dataGridView1.Rows[i].Cells[DtGroupName.Index].Value.ToString().ToUpper().Contains("Yarn".ToUpper()))
                                {
                                    TotalYarn += Utility.SafeConvertToDouble(dataGridView1["dtQty", i].FormattedValue.ToString());
                                }
                            }
                        }
                        if (TotalYarn > 0)
                        {
                            dtYarnDespatch = new DataTable();
                            dtYarnDespatch.Columns.Clear();
                            dtYarnDespatch.Columns.Add("InvNo");
                            dtYarnDespatch.Columns.Add("Invdate");
                            dtYarnDespatch.Columns.Add("PartyName");
                            dtYarnDespatch.Columns.Add("sysdate");
                            dtYarnDespatch.Columns.Add("MS");
                            dtYarnDespatch.Columns.Add("BoxNo");
                            dtYarnDespatch.Columns.Add("Deneir");
                            dtYarnDespatch.Columns.Add("BoxWt");
                            dtYarnDespatch.Columns.Add("BobbinWt");
                            dtYarnDespatch.Columns.Add("NosOfBobbin");
                            dtYarnDespatch.Columns.Add("Grosswt");
                            dtYarnDespatch.Columns.Add("TrWt");
                            dtYarnDespatch.Columns.Add("Netwt");
                            dtYarnDespatch.Columns.Add("CompanyName");
                            dtYarnDespatch.Columns.Add("Colors");
                            //Open Dialog for saving Packing List as Rolls
                            using (FrmYarnDespatch f = new FrmYarnDespatch(comboCompanyName.Text, (string)dateSysdate.Value.ToString("yyyy-MM-dd"), TotalYarn, ddlPartyName.Text, txtInvNo.Text, (string)dateSysdate.Value.ToString("yyyy-MM-dd"), ref dtYarnDespatch))
                            {
                                if (f.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                                {
                                    if (dtYarnDespatch.Rows.Count - 1 < 0)
                                    {
                                        return;
                                    }
                                }

                            }
                        }
                    }
                }

                {
                    try
                    {
                        if ((comboWareHouseFrom.Text == "Multi Filament Godown" || comboWareHouseFrom.Text == "TFO Godown") &&
                            comboCompanyName.Text == "Plastene India Limited " && comboWareHouseTo.Text == "Despatch Godown")
                        {
                            if (dtYarnDespatch.Rows.Count - 1 < 0 && TotalYarn > 0)
                            {
                                MessageBox.Show("Please Fill Yarn Despatch details properly and Click on OK Button");
                                return;
                            }
                        }
                        DataTable itemDt = new DataTable();
                        Database.OpenConnection(Utility.MaterialConnectionString);
                        Database.BeginTransaction();
                        //by Raj 08.12.2021 due to generated same Number if not in begin tran re-chek for PackingListno
                        if (comboWareHouseTo.Text == "Despatch Godown")
                        {
                            PackingListNo = ProductionCommonFunction.GeneratePackingListInvno(comboCompanyName.Text);
                            TxtPackingList.Text = PackingListNo;
                        }
                        //End by raj 08.12.2021
                        int GroupId = (int)Database.ExecuteScalar("select max(groupid)+1 from warehousetowarehouse");

                        bool isavailinwarehouse = false;
                        bool inward = false;
                        for (int i = 0; i < dataGridView1.Rows.Count; i++)
                        {
                            if ((bool)dataGridView1.Rows[i].Cells[0].Value == true && Utility.SafeConvertToDouble(dataGridView1["dtQty", i].FormattedValue.ToString()) > 0)
                            {
                                int srno = (int)Database.ExecuteScalar("select max(transid)+1 from warehousetowarehouse  ");
                                isavailinwarehouse = false;
                                isavailinwarehouse = false;

                                //Database.myreader = Database.GetExecuteReaderCommand("select stkinhand from warehouse  where companyname = '"
                                // + comboCompanyName.Text + "' and warehousename = '" + comboWareHouseFrom.Text
                                // + "' and itemcode = '" + dataGridView1[1, i].Value + "' and stkinhand>0");
                                //if (Database.myreader.Read())
                                //    isavailinwarehouse = true;
                                //Database.myreader.Close();
                                //if (isavailinwarehouse)
                                //{
                                Database.GetExecuteNonQueryCommand("insert into warehousetowarehouse  (CompanyName,FromWareHouse,ToWareHouse,ItemCode,ItemName,Qty,Remarks,GroupId,transid,sysdate,InternalReq) values('"
                                 + comboCompanyName.Text + "','" + comboWareHouseFrom.Text + "','"
                                 + comboWareHouseTo.Text + "','" + dataGridView1["DtItemCode", i].FormattedValue + "','"
                                + dataGridView1["dtItemName", i].FormattedValue + "','" + dataGridView1["dtQty", i].FormattedValue + "','"
                                + dataGridView1["dtRemarks", i].FormattedValue + "'," + GroupId + "," + srno
                                + ",'" + dateSysdate.Value.ToString("yyyy-MM-dd") + "','" + comboInternalReq.Text + "')");


                                if (comboWareHouseTo.Text == "Stock Adjustment Entry")
                                {
                                    int transid = Convert.ToInt32(Database.GetExecuteNonQueryCommand_retrun("select max(transid) from warehousetowarehouse"));
                                    ProductionCommonFunction.InsertStockJournal(transid, dateSysdate.Value, comboCompanyName.Text, "Consumption", dataGridView1["dtItemName", i].FormattedValue.ToString(),
                                        Convert.ToDouble(dataGridView1["dtQty", i].FormattedValue), 0, 0, "KGS", frmDefaultVale.Year, transid, srno, dataGridView1["DtItemCode", i].FormattedValue.ToString(), "..", "StockAdj");
                                    //Change transid to Srno of current record so we have track for which record we are inserting stock jv
                                }
                                if (comboWareHouseTo.Text == "Despatch Godown")
                                {
                                    int transid = Convert.ToInt32(Database.GetExecuteNonQueryCommand_retrun("select max(transid) from warehousetowarehouse"));
                                    ProductionCommonFunction.InsertStockJournal(transid, dateSysdate.Value, comboCompanyName.Text, "Sales", dataGridView1["dtItemName", i].FormattedValue.ToString(),
                                        Convert.ToDouble(dataGridView1["dtQty", i].FormattedValue), 0, 0, "KGS", frmDefaultVale.Year, transid, srno, dataGridView1["DtItemCode", i].FormattedValue.ToString(), "..", comboWareHouseFrom.Text);
                                    //Change transid to Srno of current record so we have track for which record we are inserting stock jv
                                }


                                bool IsAvail = false;
                                Database.myreader = Database.GetExecuteReaderCommand("select * from  warehouse where itemcode = '"
                                + dataGridView1[1, i].Value.ToString() + "' and warehousename = '"
                                + comboWareHouseTo.Text + "' and companyname = '" + comboCompanyName.Text + "'");
                                if (Database.myreader.Read())
                                {
                                    IsAvail = true;
                                }
                                Database.myreader.Close();

                                if (!IsAvail)
                                {

                                    //                   if (MessageBox.Show("Item with code '" + dataGridView1["DtItemCode", i].Value.ToString() + "' does not exists in '" + comboWareHouseTo.Text + "' do you want to create it?"
                                    //, "Warehouse Transfer", MessageBoxButtons.YesNo, MessageBoxIcon.Information) != System.Windows.Forms.DialogResult.Yes)
                                    //                       return;

                                    string InsertString = "";
                                    string subgroup2 = "";
                                    itemDt = Database.GetDataTable("select * from item where ItemCode = '" + dataGridView1[1, i].Value.ToString() + "' and CompanyName = '" + comboCompanyName.Text + "'");

                                    itemDt.Rows[0][4] = "0";
                                    itemDt.Rows[0][5] = "0";
                                    itemDt.Rows[0][6] = "0";
                                    itemDt.Rows[0][8] = "0";
                                    itemDt.Rows[0][12] = "0";
                                    itemDt.Rows[0][13] = "0";
                                    itemDt.Rows[0][14] = "0";
                                    itemDt.Rows[0][18] = "0";
                                    subgroup2 = Convert.ToString(itemDt.Rows[0]["DtlSubGroupName"]);
                                    for (int j = 0; j <= 19; j++)
                                    {
                                        InsertString += "'" + itemDt.Rows[0][j].ToString() + "',";
                                    }

                                    if (InsertString != "")
                                        InsertString = InsertString.Substring(0, InsertString.Length - 1);

                                    if (Database.GetExecuteNonQueryCommand("insert into warehouse values(" + InsertString + ",'" + comboWareHouseTo.Text + "','" + subgroup2 + "')"))
                                    {
                                        //Database.GetExecuteNonQueryCommand("update  warehouse set stkinhand = " + dataGridView1["dtQty", i].Value + " where itemcode = '" + itemDt.Rows[0][0].ToString() + "' "
                                        //+ " and warehousename = '" + comboWareHouseTo.Text + "' and companyname = '" + comboCompanyName.Text + "'");

                                        Database.GetExecuteNonQueryCommand("update  warehouse set stkinhand = round(stkinhand,2) - " + dataGridView1["dtQty", i].Value + " where itemcode = '" + itemDt.Rows[0][0].ToString() + "' "
                                        + " and warehousename = '" + comboWareHouseFrom.Text + "' and companyname = '" + comboCompanyName.Text + "'");

                                        string itemname = ProductionCommonFunction.GetItemName(itemDt.Rows[0][0].ToString());
                                        ProductionCommonFunction.WareHouseStoreoutwards(itemDt.Rows[0][0].ToString(), itemname,
                                                  Convert.ToDouble(dataGridView1["dtQty", i].Value), dateSysdate.Value, comboCompanyName.Text, comboWareHouseFrom.Text, "");

                                    }
                                    else
                                    {
                                        MessageBox.Show("Record not saved.");
                                    }
                                }
                                else
                                {

                                    ProductionCommonFunction.WareHouseStoreoutwards(dataGridView1["DtItemCode", i].Value.ToString(), dataGridView1["DtItemName", i].Value.ToString(),
                                       Convert.ToDouble(dataGridView1["dtQty", i].Value), dateSysdate.Value, comboCompanyName.Text, comboWareHouseFrom.Text, "");

                                    Database.GetExecuteNonQueryCommand("update warehouse set stkinhand = round(stkinhand,2) - "
                                    + Convert.ToDouble(dataGridView1["dtQty", i].Value) + " where companyname = '"
                                    + comboCompanyName.Text + "' and warehousename = '" + comboWareHouseFrom.Text
                                    + "' and itemcode = '" + dataGridView1["DtItemCode", i].Value + "'");

                                    //Database.GetExecuteNonQueryCommand("update warehouse set stkinhand = round(stkinhand,2) + "
                                    //+ Convert.ToDouble(dataGridView1["dtQty", i].Value) + " where companyname = '"
                                    //+ comboCompanyName.Text + "' and warehousename = '" + comboWareHouseTo.Text
                                    //+ "' and itemcode = '" + dataGridView1["DtItemCode", i].Value + "'");
                                }


                                if (comboWareHouseFrom.Text == "Inhouse Liner Bag Godown")
                                {
                                    if (FrmShowGrid.historyTable.Rows.Count > 0)
                                    {
                                        for (int k = 0; k < FrmShowGrid.historyTable.Rows.Count; k++)
                                        {
                                            string strsql = "insert into InhouseLinerBagHistory (itemcode,itemname,qty,transid,remainingqty,sysdate) values ('" + FrmShowGrid.historyTable.Rows[k][0].ToString() + "','" +
                                            FrmShowGrid.historyTable.Rows[k][1].ToString() + "'," + Convert.ToDecimal(FrmShowGrid.historyTable.Rows[k][2]) + "," +
                                            Convert.ToInt32(FrmShowGrid.historyTable.Rows[k][3]) + "," + Convert.ToDecimal(FrmShowGrid.historyTable.Rows[k][2]) + ",'" + FrmShowGrid.historyTable.Rows[k]["sysdate"] + "')";
                                            Database.GetExecuteNonQueryCommand(strsql);
                                        }
                                    }
                                }

                                //Database.GetExecuteNonQueryCommand("Insert into prod_transaction(CompanyName,Qty,FromGodown," +
                                // " ToGodown,ItemName,Sysdate,ItemCode) Values('" + comboCompanyName.Text + "','" + dataGridView1[3, i].FormattedValue + "'," +
                                // "'" + comboWareHouseFrom.Text + "','" + comboWareHouseTo.Text + "','" + dataGridView1[2, i].FormattedValue + "'"
                                // + ",'" + dateTimePicker1.Value.ToString("yyyy-MM-dd") + "','" + dataGridView1[1, i].FormattedValue + "')");

                                if (comboWareHouseTo.Text == "Despatch Godown")
                                {

                                    if (comboWareHouseFrom.Text == "Inhouse Liner Bag Godown")
                                    {
                                        Database.GetExecuteNonQueryCommand("Insert into TapepackingEntry (ItemCode, ItemName, Weight, PCS, Sysdate, InvoiceNo, Remarks, CompanyName, " +
                                            " GroupID, PartyName, Purpose ,PACKINGLISTNO) values ('" + dataGridView1["DtItemCode", i].FormattedValue + "','" + dataGridView1["dtItemName", i].FormattedValue + "', " +
                                            "" + dataGridView1["dtQty", i].FormattedValue + ",0,'" + dateSysdate.Value.ToString("yyyy-MM-dd") + "','" + txtInvNo.Text + "','" + txtRemarks.Text + "','" + comboCompanyName.Text + "'," + GroupId + "," +
                                            " '" + ddlPartyName.Text + "','" + ddlPurpose.Text + "','" + PackingListNo + "')");
                                    }
                                    else
                                    {
                                        //Boolean istapedespalready = false;
                                        //Database.myreader = Database.GetExecuteReaderCommand("select ItemCode from TapepackingEntry where ItemCode = '"
                                        //+ dataGridView1["DtItemCode", i].Value + "'");
                                        //if (Database.myreader.Read())
                                        //    istapedespalready = true;
                                        //Database.myreader.Close();
                                        //if (!istapedespalready)
                                        {
                                            Database.GetExecuteNonQueryCommand("Insert into TapepackingEntry (ItemCode, ItemName, Weight, PCS, Sysdate, InvoiceNo, Remarks, CompanyName, " +
                                                " GroupID, PartyName, Purpose,PACKINGLISTNO) values ('" + dataGridView1["DtItemCode", i].FormattedValue + "','" + dataGridView1["dtItemName", i].FormattedValue + "', " +
                                            "" + dataGridView1["dtQty", i].FormattedValue + ",0,'" + dateSysdate.Value.ToString("yyyy-MM-dd") + "','" + txtInvNo.Text + "','" + txtRemarks.Text + "','" + comboCompanyName.Text + "'," + GroupId + "," +
                                            " '" + ddlPartyName.Text + "','" + ddlPurpose.Text + "','" + PackingListNo + "')");
                                        }
                                    }
                                }

                                inward = true;
                                //}
                                //else
                                //    MessageBox.Show("'" + dataGridView1[2, i].Value + "' is not in FromWarehouse,so first transfer it from Item Transfer than issue ");
                            }

                        }
                        // BindGrid();
                        if (inward)
                        {
                            if (comboWareHouseTo.Text == "Despatch Godown")
                            {
                                if ((comboWareHouseFrom.Text == "Multi Filament Godown" || comboWareHouseFrom.Text == "TFO Godown") && comboCompanyName.Text == "Plastene India Limited ")
                                {
                                    for (int i = 0; i <= dtYarnDespatch.Rows.Count - 1; i++)
                                    {
                                        string strsql = "INSERT INTO MIS_YarnDespatch(InvNo,Invdate,PartyName,sysdate,MS,BoxNo,Deneir,BoxWt,BobbinWt,NosOfBobbin,Grosswt,TrWt,Netwt,CompanyName,Colors,PackingListNo) VALUES('" +
                                           dtYarnDespatch.Rows[i]["InvNo"] + "','" +
                                           dtYarnDespatch.Rows[i]["Invdate"] + "','" +
                                           dtYarnDespatch.Rows[i]["PartyName"] + "','" +
                                           dtYarnDespatch.Rows[i]["sysdate"] + "','" +
                                           dtYarnDespatch.Rows[i]["MS"] + "','" +
                                           dtYarnDespatch.Rows[i]["BoxNo"] + "'," +
                                           Utility.SafeConvertToDouble(dtYarnDespatch.Rows[i]["Deneir"]) + "," +
                                           Utility.SafeConvertToDouble(dtYarnDespatch.Rows[i]["BoxWt"]) + "," +
                                           Utility.SafeConvertToDouble(dtYarnDespatch.Rows[i]["BobbinWt"]) + "," +
                                           Utility.SafeConvertToDouble(dtYarnDespatch.Rows[i]["NosOfBobbin"]) + "," +
                                           Utility.SafeConvertToDouble(dtYarnDespatch.Rows[i]["Grosswt"]) + "," +
                                           Utility.SafeConvertToDouble(dtYarnDespatch.Rows[i]["TrWt"]) + "," +
                                           Utility.SafeConvertToDouble(dtYarnDespatch.Rows[i]["Netwt"]) + ",'" +
                                           dtYarnDespatch.Rows[i]["CompanyName"] + "','" +
                                           dtYarnDespatch.Rows[i]["Colors"] + "','" + TxtPackingList.Text + "')";
                                        Database.GetExecuteNonQueryCommand(strsql);

                                    }
                                }
                            }
                            Database.CommitTransaction();
                            Database.Closeconnection();
                            if (TxtPackingList.Text.Length > 0)
                                Utility.UserInformation("Save ", "Material Transfered  " + comboWareHouseFrom.Text + " to  " + comboWareHouseTo.Text + " is Saved for " + comboCompanyName.Text + " with Packinglistno : " + TxtPackingList.Text);
                            else
                                Utility.UserInformation("Save ", "Material Transfered  " + comboWareHouseFrom.Text + " to  " + comboWareHouseTo.Text + " is Saved for " + comboCompanyName.Text);
                            MessageBox.Show(" Ware House to Ware House Entry has been done");
                            dataGridView1.Rows.Clear();
                            dtYarnDespatch = new DataTable();
                            dtYarnDespatch.Rows.Clear();
                            dtYarnDespatch.Columns.Clear();
                        }
                    }
                    catch (Exception ex1)
                    {
                        Database.RollBackTransaction();
                    }
                }
            }
            catch (Exception ex)
            {

                ErrorMessageBox.Show(ex);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                Validations();
                {
                    try
                    {
                        Database.OpenConnection(Utility.MaterialConnectionString);
                        Database.BeginTransaction();

                        string companyname = "";
                        string Fromwarehouse = "";
                        string Towarehouse = "";
                        ArrayList ArrItemcode = new ArrayList();
                        ArrayList ArrItemName = new ArrayList();
                        ArrayList ArrQty = new ArrayList();
                        ArrayList ArrRemarks = new ArrayList();
                        ArrayList Arrtransid = new ArrayList();

                        Database.myreader = Database.GetExecuteReaderCommand("Select * from warehousetowarehouse where groupid = " + GroupSrNo + " and transid not in " +
                            " (select transid from warehouseinwards where companyname='" + comboCompanyName.Text + "' and status='" + Towarehouse + "' )");
                        while (Database.myreader.Read())
                        {
                            companyname = Database.myreader[0].ToString();
                            Fromwarehouse = Database.myreader[1].ToString();
                            Towarehouse = Database.myreader[2].ToString();
                            ArrItemcode.Add(Database.myreader[3].ToString());
                            ArrItemName.Add(Database.myreader[4].ToString());
                            ArrQty.Add(Database.myreader[5].ToString());
                            ArrRemarks.Add(Database.myreader[6].ToString());

                        }
                        Database.myreader.Close();

                        for (int i = 0; i < ArrItemcode.Count; i++)
                        {

                            string itemname = ProductionCommonFunction.GetItemName(ArrItemcode[i].ToString());
                            ProductionCommonFunction.WareHouseStoreinwards(ArrItemcode[i].ToString(), itemname,
                                      Convert.ToDouble(ArrQty[i].ToString()), dateSysdate.Value, comboCompanyName.Text, Fromwarehouse, Towarehouse);


                            Database.GetExecuteNonQueryCommand("update warehouse set stkinhand = stkinhand + "
                          + ArrQty[i].ToString() + " where companyname = '"
                          + companyname + "' and warehousename = '" + Fromwarehouse
                          + "' and itemcode = '" + ArrItemcode[i].ToString() + "'");

                            ProductionCommonFunction.WareHouseStoreoutwards(ArrItemcode[i].ToString(), itemname,
                                      Convert.ToDouble(ArrQty[i].ToString()), dateSysdate.Value, comboCompanyName.Text, Towarehouse, Fromwarehouse);


                            Database.GetExecuteNonQueryCommand("update warehouse set stkinhand = stkinhand - "
                             + ArrQty[i].ToString() + " where companyname = '"
                             + companyname + "' and warehousename = '" + Towarehouse
                             + "' and itemcode = '" + ArrItemcode[i].ToString() + "'");
                        }

                        Database.GetExecuteNonQueryCommand("delete from warehousetowarehouse where groupid = " + GroupSrNo + " and transid not in " +
                            " (select transid from warehouseinwards where companyname='" + comboCompanyName.Text + "' and status='ToWareHouse' )");

                        bool isavailinwarehouse = false;
                        bool isavaitransid = false;
                        bool inward = false;
                        double stkinhand = 0;
                        for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                        {
                            isavaitransid = false;
                            Database.myreader = Database.GetExecuteReaderCommand("select transid from warehouseinwards  where companyname = '"
                                                + comboCompanyName.Text + "' and status= 'ToWareHouse' and transid = '" + dataGridView1[5, i].Value + "'");
                            if (Database.myreader.Read())
                                isavaitransid = true;
                            Database.myreader.Close();
                            if (!isavaitransid)
                            {
                                isavailinwarehouse = false;
                                int srno = 0;
                                Database.myreader = Database.GetExecuteReaderCommand("select transid from warehousetowarehouse order by transid desc");
                                if (Database.myreader.Read())
                                    srno = Convert.ToInt32(Database.myreader[0].ToString());
                                Database.myreader.Close();
                                srno = srno + 1;
                                Database.myreader = Database.GetExecuteReaderCommand("select stkinhand from warehouse  where companyname = '"
                                + comboCompanyName.Text + "' and warehousename = '" + comboWareHouseFrom.Text
                                + "' and itemcode = '" + dataGridView1[0, i].Value + "'");
                                if (Database.myreader.Read())
                                    isavailinwarehouse = true;
                                Database.myreader.Close();

                                if (isavailinwarehouse)
                                {
                                    Database.myreader = Database.GetExecuteReaderCommand("select stkinhand from warehouse  where companyname = '"
                                     + comboCompanyName.Text + "' and warehousename = '" + comboWareHouseFrom.Text
                                     + "' and itemcode = '" + dataGridView1["DtItemCode", i].Value + "'");
                                    if (Database.myreader.Read())
                                        stkinhand = Convert.ToDouble(Database.myreader["stkinhand"].ToString());
                                    Database.myreader.Close();
                                    stkinhand = stkinhand - Convert.ToDouble(dataGridView1["dtQty", i].Value);
                                    if (stkinhand >= 0)
                                    {
                                        if (dataGridView1["dtRemarks", i].FormattedValue.ToString() != "")
                                            dataGridView1["dtRemarks", i].Value = ".";

                                        Database.GetExecuteNonQueryCommand("insert into warehousetowarehouse (CompanyName,FromWareHouse,ToWareHouse,ItemCode,ItemName,Qty,Remarks,GroupId,transid,sysdate,InternalReq) values('"
                                        + comboCompanyName.Text + "','" + comboWareHouseFrom.Text + "','"
                                        + comboWareHouseTo.Text + "','" + dataGridView1["DtItemCode", i].Value + "','"
                                        + dataGridView1["dtItemName", i].Value + "','" + dataGridView1["dtQty", i].Value + "','"
                                        + dataGridView1["dtRemarks", i].Value + "'," + GroupSrNo + "," + srno
                                        + ",'" + dateSysdate.Value.ToString("yyyy-MM-dd") + "','" + comboInternalReq.Text + "')");


                                        ProductionCommonFunction.WareHouseStoreoutwards(dataGridView1["DtItemCode", i].Value.ToString(),
                                              dataGridView1["DtItemName", i].Value.ToString(), Convert.ToDouble(dataGridView1["dtQty", i].Value),
                                              dateSysdate.Value, comboCompanyName.Text, comboWareHouseFrom.Text, comboWareHouseTo.Text);

                                        Database.GetExecuteNonQueryCommand("update warehouse set stkinhand = stkinhand - "
                                         + Convert.ToDouble(dataGridView1["dtQty", i].Value) + " where companyname = '"
                                         + comboCompanyName.Text + "' and warehousename = '" + comboWareHouseFrom.Text
                                         + "' and itemcode = '" + dataGridView1["DtItemCode", i].Value + "'");

                                        //Database.GetExecuteNonQueryCommand("update warehouse set stkinhand = stkinhand + "
                                        // + Convert.ToDouble(dataGridView1[2, i].Value) + " where companyname = '"
                                        // + comboCompanyName.Text + "' and warehousename = '" + comboWareHouseTo.Text
                                        // + "' and itemcode = '" + dataGridView1[0, i].Value + "'");

                                        inward = true;
                                    }
                                    else
                                        MessageBox.Show("'" + dataGridView1["dtItemName", i].Value + "' has stock less in from Warehouse,so cant issue ");
                                }

                                else
                                    MessageBox.Show("'" + dataGridView1["dtItemName", i].Value + "' is not in FromWarehouse,so first transfer it from Item Transfer than issue ");
                            }
                        }
                        Database.CommitTransaction();
                        Database.Closeconnection();
                        // BindGrid();
                        if (inward)
                        {
                            MessageBox.Show(" Ware House to Ware House Entry has been done and updated ");
                            dataGridView1.Rows.Clear();

                        }
                    }
                    catch (Exception ex1)
                    {
                        Database.RollBackTransaction();
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
            }
        }


        //private void dataGridView2_MouseDoubleClick(object sender, MouseEventArgs e)
        //{

        //    try
        //    {

        //        GroupSrNo = 0;
        //        btnSave.Enabled = false;
        //        btnUpdate.Enabled = true;

        //        //comboWareHouseFrom.Text = dataGridView2[1, dataGridView2.CurrentRow.Index].Value.ToString();
        //        comboWareHouseTo.Text = dataGridView2[2, dataGridView2.CurrentRow.Index].Value.ToString();
        //        GroupSrNo = Convert.ToInt32(dataGridView2[7, dataGridView2.CurrentRow.Index].Value.ToString());
        //        dateTimePicker1.Value = (DateTime)dataGridView2[9, dataGridView2.CurrentRow.Index].Value;

        //        dataGridView1.Rows.Clear();

        //        Database.OpenConnection(Utility.MaterialConnectionString);
        //        object j = Database.GetExecuteScalarCommand_return("select count(*) from warehousetowarehouse where groupid = "
        //             + GroupSrNo);
        //        int TotalCount = Convert.ToInt32(j);


        //        if (TotalCount >= 0)
        //        {
        //            //dataGridView1.Columns.Add("ID", "Transid");
        //            dataGridView1.Rows.Add(TotalCount);

        //            int i = 0;
        //            Database.myreader = Database.GetExecuteReaderCommand("select a.itemcode,a.itemname,qty,remarks,b.StkInHand,b.StkInHand as [Remaining Qty] from warehousetowarehouse a " +
        //                                " left join WareHouse b on a.CompanyName=b.CompanyName and a.FromWareHouse=b.WareHouseName and a.ItemCode=b.ItemCode " +
        //                                " where a.GroupId=" + GroupSrNo);
        //            while (Database.myreader.Read())
        //            {
        //                dataGridView1[1, i].Value = Database.myreader[0].ToString();
        //                dataGridView1[2, i].Value = Database.myreader[1].ToString();
        //                dataGridView1[3, i].Value = Database.myreader[2].ToString();
        //                dataGridView1[4, i].Value = Database.myreader[3].ToString();
        //                dataGridView1[5, i].Value = Database.myreader[4].ToString();
        //                dataGridView1[6, i].Value = Database.myreader[5].ToString();
        //                i++;
        //            }
        //            Database.myreader.Close();
        //        }

        //    }
        //    catch (Exception ex)
        //    {
        //        ErrorMessageBox.Show(ex);
        //    }
        //}


        private void comboWareHouseTo_SelectedIndexChanged(object sender, EventArgs e)
        {
            //fillData();
            if (comboWareHouseTo.Text == "Despatch Godown")
            {
                ddlPartyName.Visible = true;
                ddlPurpose.Visible = true;
                txtRemarks.Visible = true;
                txtInvNo.Visible = true;
                label7.Visible = true;
                label8.Visible = true;
                label10.Visible = true;
                label11.Visible = true;

                //  ddlPartyName.DataSource = ProductionCommonFunction.GetDespatchCompanyList();// ProductionCommonFunction.GetInternalVendor();

                DataTable dtInternal = ProductionCommonFunction.GetDespatchCompanyList();
                dtInternal.Rows.Add("Export Godown");
                ddlPartyName.DataSource = dtInternal;
                ddlPartyName.ValueMember = "firmname";
                ddlPartyName.DisplayMember = "firmname";

                label14.Visible = true;
                TxtPackingList.Text = "";
                TxtPackingList.Visible = true;

            }
            else
            {
                ddlPartyName.Visible = false;
                ddlPurpose.Visible = false;
                txtRemarks.Visible = false;
                txtInvNo.Visible = false;
                label7.Visible = false;
                label8.Visible = false;
                label10.Visible = false;
                label11.Visible = false;
                label14.Visible = false;
                TxtPackingList.Visible = false;
                TxtPackingList.Text = "";
            }
            fillgridwithReq();



        }

        private void dataGridView1_CurrentCellChanged(object sender, EventArgs e)
        {
            if (dataGridView1.CurrentCell == null) return;
            try
            {
                if (dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[1].FormattedValue.ToString().Trim() != "")
                {

                    if (Database.OpenConnection(Utility.MaterialConnectionString))
                    {
                        Database.myreader = Database.GetExecuteReaderCommand("select stkinhand from warehouse where ItemCode = '"
                        + dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells["DtItemCode"].FormattedValue.ToString() + "' and warehousename = '"
                        + comboWareHouseFrom.Text + "' and companyname = '" + comboCompanyName.Text + "'");
                        if (Database.myreader.Read())
                        {
                            dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells["DtStock"].Value = Math.Round(Convert.ToDouble(Database.myreader["stkinhand"]), 2);
                        }
                        Database.myreader.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                ex.ToString();
            }
        }


        private void dataGridView1_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            e.ThrowException = false;
        }

        private void btnok_Click(object sender, EventArgs e)
        {

            string qry = string.Format("select WW.CompanyName,FromWareHouse,ToWareHouse,WW.ItemCode,WW.ItemName,sum(Qty) as qty,WW.Remarks,WW.sysdate  ,TAP.InvoiceNo ,tap.PackingListNo,tap.PartyName,WW.GroupId,WW.transid from WarehousetoWareHouse WW LEFT JOIN TapepackingEntry tap with(nolock) on tap.ItemCode=ww.ItemCode and tap.GroupID=ww.GroupId  where WW.sysdate >= '{0:yyyy-MM-dd}'  " +
            "  and WW.CompanyName= '{1}'  " +
            "  group by GROUPING sets((WW.CompanyName,FromWareHouse,ToWareHouse,WW.ItemCode,WW.ItemName,WW.Remarks,WW.GroupId,WW.transid,WW.sysdate,TAP.InvoiceNo,tap.PackingListNo,tap.PartyName),())", dateTimePicker2.Value, comboCompanyName.Text);
            //   string qry = string.Format("select * from WarehousetoWareHouse where sysdate >= '{0:yyyy-MM-dd}' and CompanyName='{1}'", dateTimePicker2.Value, comboCompanyName.Text);
            string qry1 = string.Format("select ww.CompanyName, ww.FromWareHouse, ww.ToWareHouse, ww.ItemCode, ww.ItemName, SUM(ww.Qty) AS QTY,ww.Remarks, ww.GroupId, ww.transid, ww.sysdate, ww.Rate,tap.InvoiceNo,tap.PartyName,tap.Purpose from WarehousetoWareHouse ww inner join TapepackingEntry tap with(nolock) on tap.ItemCode=ww.ItemCode and tap.GroupID=ww.GroupId where ww.sysdate >= '{0:yyyy-MM-dd}' " +
            " and ww.CompanyName='{1}' " +
            " GROUP BY GROUPING sets ((ww.CompanyName, ww.FromWareHouse, ww.ToWareHouse, ww.ItemCode, ww.ItemName,  ww.Remarks, ww.GroupId, ww.transid, ww.sysdate, ww.Rate,tap.InvoiceNo,tap.PartyName,tap.Purpose), ())", dateTimePicker2.Value, comboCompanyName.Text);


            if (comboWareHouseFrom.Text == "Inhouse Liner Bag Godown")
                new FrmShowGrid(qry1, "Materail Transfer History", true).ShowDialog();
            else
                TabClass.OpenForm(new FrmShowGrid(qry, "Materail Transfer History"));//.ShowDialog();//new FrmShowGrid(qry, "Materail Transfer History").ShowDialog();
            // dataGridView2.DataSource = dataTable;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            fillData("");
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            try
            {
                bool isChecked = false;
                string strItemCode = "";
                for (int i = 0; i < dataGridView1.Rows.Count; i++)
                {
                    if ((bool)dataGridView1.Rows[i].Cells[0].Value)
                    {
                        isChecked = true;
                        strItemCode += "'" + dataGridView1.Rows[i].Cells["DtItemCode"].Value.ToString() + "',";
                    }
                }
                if (!isChecked)
                    throw new ERP.ERPErrorException("Please select any item to delete.");

                if (strItemCode.EndsWith(","))
                    strItemCode = strItemCode.Remove(strItemCode.Length - 1, 1);
                //Database.BeginTransaction();
                Database.ExecuteNonQuery("delete from warehouse where itemcode in (" + strItemCode + ") and CompanyName='" + comboCompanyName.Text + "' and WareHouseName ='" + comboWareHouseFrom.Text + "'");
                //Database.CommitTransaction();
                strItemCode = strItemCode.Replace("'", "");
                Utility.UserInformation("Delete", "Itemcodes " + strItemCode + "  are deleted from the WareHouse " + comboWareHouseFrom.Text + " for " + comboCompanyName.Text);
                fillData("");
            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
            }
        }


        private void selectCheckRollsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (dataGridView1.CurrentCell == null
              || dataGridView1.CurrentCell.RowIndex == -1)
                return;
            ProductionCommonFunction.CheckSelectedRows_Datagridview(ref dataGridView1, dataGridView1.CurrentCell.ColumnIndex);
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                var senderGrid = (DataGridView)sender;
                if (senderGrid.Columns[e.ColumnIndex] is DataGridViewButtonColumn &&
                    e.RowIndex >= 0)
                {

                    bool chk = Convert.ToBoolean(dataGridView1.Rows[e.RowIndex].Cells["Chk"].EditedFormattedValue);
                    if (chk == false)
                    {
                        MessageBox.Show("Please select a Row to get the quantity");
                        return;
                    }

                    string itemCode = dataGridView1.Rows[e.RowIndex].Cells["DtItemCode"].Value.ToString();
                    string qry = string.Format("select (WTW.CompanyName),(WTW.FromWareHouse),(WTW.ToWareHouse),(WTW.ItemCode),(WTW.ItemName),(WTW.Qty)-isnull(his.qty ,0) as qty,(WTW.Remarks),(WTW.GroupId),(WTW.transid),(WTW.sysdate),(WTW.rate),(WTW.Qty)-isnull(his.qty ,0) as [RemQty] from WarehousetoWareHouse WTW left join (select itemcode,transid,sum(qty) qty from InhouseLinerBagHistory group by itemcode,transid) HIS on WTW.transid = HIS.transid where WTW.sysdate <= '{0:yyyy-MM-dd}' and WTW.CompanyName='{1}' and WTW.ItemCode='{2}' and WTW.ToWareHouse = 'Inhouse Liner Bag Godown' and (WTW.Qty)- isnull(his.qty ,0)> 0.99", dateSysdate.Value, comboCompanyName.Text, itemCode);
                    new FrmShowGrid(qry, "Materail Transfer History", true).ShowDialog();

                    if (FrmShowGrid.sumOfReturnQty != 0)
                    {
                        dataGridView1.Rows[e.RowIndex].Cells["dtQty"].Value = FrmShowGrid.sumOfReturnQty.ToString();

                        DataGridViewTextBoxCell Col1 = (DataGridViewTextBoxCell)dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells["dtQty"];
                        DataGridViewTextBoxCell Col2 = (DataGridViewTextBoxCell)dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells["DtStock"];
                        DataGridViewTextBoxCell Col3 = (DataGridViewTextBoxCell)dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells["dtRemainQty"];

                        Col3.Value = Math.Round(Convert.ToDouble(Utility.ConverttoInt(Col2.FormattedValue.ToString())), 2) -
                                     Math.Round(Convert.ToDouble(Utility.ConverttoInt(Col1.FormattedValue.ToString())), 2);
                    }
                }
            }
            catch (Exception exObj)
            {
                ErrorMessageBox.Show(exObj);
            }
            finally
            {

            }
        }

        private void comboInternalReq_SelectedIndexChanged(object sender, EventArgs e)
        {
            dataGridView1.Rows.Clear();

            Database.OpenConnection(Utility.MaterialConnectionString);

            string query = "select * from (select warehousePurchaseReq.itemcode,warehousePurchaseReq.itemdesc,groupname,warehousePurchaseReq.qty,'.' as [Remarks],ROUND(stkinhand,2) AS stkinhand,ROUND(stkinhand,2) as [RemainingQty],code,isapprove, warehousePurchaseReq.companyname " +
                            " from warehousePurchaseReq inner join warehouse on warehouse.itemcode = warehousePurchaseReq.itemcode " +
                            "and warehouse.companyname = warehousePurchaseReq.companyname and warehousePurchaseReq.towarehouse = warehouse.warehousename ) f where not exists ( " +
                            "   select itemcode from warehousetowarehouse l where f.Code = l.InternalReq and f.ItemCode = l.ItemCode )";


            Database.myadapter = Database.GetAdapterCommand(query + "  and code = '"
                + comboInternalReq.Text + "' and isapprove = 'yes' and f.companyname = '" + comboCompanyName.Text + "'");



            DataTable dt = new DataTable();
            Database.myadapter.Fill(dt);

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                dataGridView1.Rows.Add(false,
                dt.Rows[i][0],
                dt.Rows[i][1],
                dt.Rows[i][2],
                dt.Rows[i][3],
                dt.Rows[i][4],
                dt.Rows[i][5],
                dt.Rows[i][6]
                );
            }


            Database.Closeconnection();
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            try
            {
                if (Database.OpenConnection(Utility.MaterialConnectionString))
                {
                    if (Database.GetExecuteNonQueryCommand("update Tapepackingentry set InvoiceNo = '" + txtInvNo.Text
                        + "',partyname = '" + ddlPartyName.Text + "' where PackingListNo = '" + textpktno.Text + "' and companyname = '" + comboCompanyName.Text +"'"))

                        MessageBox.Show("Update Tapepackingentry sucessfully");

                    if (Database.GetExecuteNonQueryCommand("update MISRollforDespatch set Invno = '" + txtInvNo.Text + "',partyname = '" + ddlPartyName.Text + 
                        "' where PackingListNo = '" + textpktno.Text + "' and companyname = '" + comboCompanyName.Text + "'"))
                        MessageBox.Show("Update MISRollforDespatch sucessfully");
                    
                    Database.Closeconnection();
                    
                    Utility.UserInformation("Update", "Invoice no changed for packing list no " + textpktno.Text);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void textpktno_Leave(object sender, EventArgs e)
        {
            try
            {
                if (Database.OpenConnection(Utility.MaterialConnectionString))
                {
                    Database.myreader = Database.GetExecuteReaderCommand("select InvoiceNo,partyname from Tapepackingentry  where PackingListNo = '"
                         + textpktno.Text + "' and companyname = '" + comboCompanyName.Text + "'");
                    if(Database.myreader.Read())
                    {
                        ddlPartyName.Text = Database.myreader[1].ToString();
                        txtInvNo.Text = Database.myreader[0].ToString();
                    }
                    Database.myreader.Close();


                    Database.myreader = Database.GetExecuteReaderCommand("select Invno,partyname from MISRollforDespatch  where PackingListNo = '"
                        + textpktno.Text + "' and companyname = '" + comboCompanyName.Text + "'");

                    if (Database.myreader.Read())
                    {
                        ddlPartyName.Text = Database.myreader[1].ToString();
                        txtInvNo.Text = Database.myreader[0].ToString();
                    }
                    Database.myreader.Close();

                    Database.Closeconnection(); 
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }
    }
}