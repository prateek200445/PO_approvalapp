// =============================================================================
// INVOICE-RULE COPY — do not compile next to original.
// Replace FrmSmallBagBailForDespatch.cs after review. Search tag: INVOICE-RULE
// THIS FILE: look up Small Bags WIP, minus Small Bag Godown, plus Despatch Godown (kg).
//
// LINE MAP (this copy):
//   298-300 after bale insert: minus Small Bag Godown, plus Despatch (Small Bags WIP)
//   864 helper InvoiceRule_MoveWipToDespatch
// =============================================================================
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using DataGridViewAutoFilter;

namespace ERP
{
    public partial class FrmSmallBagBailForDespatch : Form
    {
        bool isdoubleclick = false;
        public int transid = 0;
        public System.Windows.Forms.StatusBarPanel pfrm_sbp1 = null;
        private filter.filterData filterData;
        bool isDelete = false;
        DataSet dataset1Roll = new DataSet();
        DataView Ro;
        public FrmSmallBagBailForDespatch()
        {
            InitializeComponent();
            Form_Load();
        }

        private void Form_Load()
        {
            try
            {
                filterData = new filter.filterData(dataGridView1);

                dateLoomEntry.Format = DateTimePickerFormat.Custom;
                dateLoomEntry.CustomFormat = Tools.ToolsCommonFunction.ERPDateSettings();
                dateLoomEntry.MaxDate = DateTime.Now;
                dateLoomEntry.MinDate = ProductionCommonFunction.ERPBackDateSettings();

                if (Database.OpenConnection(Utility.MaterialConnectionString))
                {
                    Database.myreader = Database.GetExecuteReaderCommand("select name from factoryinfo order by name");
                    while (Database.myreader.Read())
                    {
                        comboCompanyName.Items.Add(Database.myreader[0].ToString());
                    }
                    Database.myreader.Close();
                    comboCompanyName.Text = frmDefaultVale.CompanyName;

                    comboPartyName1.Items.Clear();
                    comboPartyName1.Text = "";
                    comboPartyName.Text = "";
                    Database.myreader = Database.GetExecuteReaderCommand("select distinct(PartyName) from SmallBagBaillingEntry where CompanyName='" + comboCompanyName.Text + "' ");
                    while (Database.myreader.Read())
                    {
                        comboPartyName.Items.Add(Database.myreader[0].ToString());
                        comboPartyName1.Items.Add(Database.myreader[0].ToString());
                    }
                    Database.Closeconnection();
                }

                comboInternalCompny.DataSource = ProductionCommonFunction.GetInternalVendor();
                comboInternalCompny.DisplayMember = "firmname";
                comboInternalCompny.ValueMember = "firmname";

                buttonDelete.Enabled = false;
                BindGrid_data();
                BindGrid();


            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
            }
        }

        private void FrmMISRollforDespatchExcelFormat_Load(object sender, EventArgs e)
        {
        }
        private void BindGrid_data()
        {
            try
            {

                if (Database.OpenConnection(Utility.MaterialConnectionString))
                {
                    //string strsql = "select SysDate,PartyName,MarketingOrdNo,v.BuyerOrderNo,sb.BailNo, PCS as BailPcs,Weight as Bailwt,sb.PalatWt,sb.ProductionSmallBagEntryID as Transid from SmallBagBaillingEntry"
                    //    + " sb left join Despatch..vw_ProductionCompAndMarkInvNo v on v.itemno = sb.MarketingOrdNo and v.buyername = sb.PartyName and"
                    //    + " v.ProductionCompanyname = sb.CompanyName where rollno not in (select BailNo from SmallBagBailForDespatch where CompanyName='" + comboCompanyName.Text + "' and"
                    //+ " PartyName='" + comboPartyName.Text + "' and Deppt='Small Bag') and sb.CompanyName='" + comboCompanyName.Text + "' and PartyName='" + comboPartyName.Text + "' ";

                    string strsql = " Exec sp_Prod_SB_DispatchBail  N'" + comboCompanyName.Text + "','" + dateLoomEntry.Value.ToString("yyyy-MM-dd") + "','" + comboPartyName.Text + "'";
                    Database.myreader = Database.GetExecuteReaderCommand(strsql);
                    DataTable dt = new DataTable();
                    dt.Load(Database.myreader);
                    BindingSource datasource = new BindingSource(dt, null);
                    dataGridView1.Rows.Clear();
                    DataTable dtTmp = new DataTable();
                    dtTmp = dt;

                    for (int i = 0; i < dtTmp.Rows.Count; i++)
                    {
                        dataGridView1.Rows.Add(
                            false,
                            (
                            (DateTime)dtTmp.Rows[i]["SysDate"]).ToString("MM/dd/yyyy"),
                            Convert.ToString(dtTmp.Rows[i]["PartyName"]),
                            Convert.ToString(dtTmp.Rows[i]["MarketingOrdNo"]),
                            Convert.ToString(dtTmp.Rows[i]["BuyerOrderNo"]),
                            Convert.ToString(dtTmp.Rows[i]["BailNo"]),
                            Convert.ToString(dtTmp.Rows[i]["BailPcs"]),
                            Convert.ToString(dtTmp.Rows[i]["BailWt"]),
                            Convert.ToString(dtTmp.Rows[i]["PalatWt"]),
                            Convert.ToString(dtTmp.Rows[i]["transId"]),
                            Convert.ToString(dtTmp.Rows[i]["BagSize"]),
                            Convert.ToString(dtTmp.Rows[i]["Remark"]),
                            Convert.ToString(dtTmp.Rows[i]["InternalBailNo"])
                            );
                    }
                    //dataGridView1.DataSource = dtTmp.Copy();
                    for (int i = 0; i <= dataGridView1.Columns.Count - 1; i++)
                    {
                        if (i != 0)
                        {
                            if (dataGridView1.Columns[i].Name == "BailPcs" || dataGridView1.Columns[i].Name == "BailWt")
                                ERP.FormatStyle.RowReadOnly(dataGridView1.Columns[i], false);
                            else
                                ERP.FormatStyle.RowReadOnly(dataGridView1.Columns[i], true);
                        }
                    }


                }
            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
            }

        }

        private void buttonInitialize_Click(object sender, EventArgs e)
        {
            try
            {
                isdoubleclick = true;


                buttonDelete.Enabled = false;
                buttonSave.Enabled = true;

                dateLoomEntry.Enabled = true;
                isdoubleclick = false;

                BtnUpdate.Enabled = false;
                buttonDelete.Enabled = false;
                buttonSave.Enabled = false;
                comboPartyName1.Visible = true;
                comboPartyName.Visible = false;
                btnload.Visible = false;
                TxtPackingList.Text = "";
                textInvno.Text = "";
                dataGridView1.Rows.Clear();

                dateLoomEntry.Format = DateTimePickerFormat.Custom;
                //dateLoomEntry.CustomFormat = "MM/dd/yyyy";
                dateLoomEntry.CustomFormat = Tools.ToolsCommonFunction.ERPDateSettings();
                dateLoomEntry.MaxDate = DateTime.Now;
                dateLoomEntry.MinDate = ProductionCommonFunction.ERPBackDateSettings();
                dateLoomEntry.Value = Convert.ToDateTime(DateTime.Now.ToShortDateString());
                buttonSave.Enabled = true;
                checkBox1.Checked = false;
                comboInternalCompny.Text = "";
            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
            }
        }
        public bool Validations()
        {
            try
            {
                if (textInvno.Text == "")
                {
                    MessageBox.Show("Please enter Inv No..");
                    return false;
                }

                //Database.OpenConnection(Utility.MaterialConnectionString);
                //for (int i = 0; i < dataGridView3.Rows.Count - 1; i++)
                //{
                //    if (Convert.ToString(dataGridView3[0, i].FormattedValue) == "")
                //    {
                //        MessageBox.Show("Please enter roll no.");
                //        return false;
                //    }
                //    if (Convert.ToString(dataGridView3[1, i].FormattedValue) == "")
                //    {
                //        MessageBox.Show("Please enter PCS value.");
                //        return false;
                //    }
                //    if (Convert.ToString(dataGridView3[2, i].FormattedValue) == "")
                //    {
                //        MessageBox.Show("Please enter Weight value.");
                //        return false;
                //    }
                //    if (Convert.ToString(dataGridView3[3, i].FormattedValue) == "")
                //    {
                //        MessageBox.Show("Please enter Party name.");
                //        return false;
                //    }
                //    if (Convert.ToString(dataGridView3[4, i].FormattedValue) == "")
                //    {
                //        MessageBox.Show("Please enter Marketing ord No.");
                //        return false;
                //    }

                //    Database.myreader = Database.GetExecuteReaderCommand("select BailNo from SmallBagBailForDespatch where BailNo = '"
                //            + dataGridView3[0, i].FormattedValue + "' and Deppt='Small Bag'");
                //    DataTable dt = new DataTable();
                //    dt.Load(Database.myreader);
                //    if (dt.Rows.Count > 0)
                //    {
                //        MessageBox.Show("You all ready done entry for this Roll No : " + dataGridView3[0, i].FormattedValue + "");
                //        return false;
                //    }
                //}
                return true;
            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
                return false;
            }
        }

        private void buttonSave_Click(object sender, EventArgs e)
        {
            string PackingListNo = "";
            TxtPackingList.Text = "";

            try
            {
                if (Validations())
                {
                    bool isentry = false;
                    double invoiceRuleKg = 0;
                    DialogResult drs;
                    string internalcompanyname = "";
                    if (checkBox1.Checked)
                    {
                        internalcompanyname = comboInternalCompny.Text;

                        drs = MessageBox.Show("Do you want to save Despatch entry to " + internalcompanyname + " ?"
                              , "Despatch Entry", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                    }
                    else
                    {
                        drs = MessageBox.Show("Do you want to save Despatch entry ?"
                                , "Despatch Entry", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                    }
                    if (drs.CompareTo(DialogResult.No) == 0)
                        return;

                    if (Database.OpenConnection(Utility.MaterialConnectionString))
                    {
                        Database.BeginTransaction();
                        PackingListNo = ProductionCommonFunction.GeneratePackingListInvno(comboCompanyName.Text); //by Raj 08.12.2021 due to generated same Number if not in begin tran re-chek for PackingListno
                        TxtPackingList.Text = PackingListNo;
                        for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                        {
                            if ((bool)dataGridView1.Rows[i].Cells["Column1"].FormattedValue == true)
                            {
                                if (Database.GetExecuteNonQueryCommand("insert into SmallBagBailForDespatch(BailNo,CompanyName,PartyName,MarketingOrdNo,PCS,Weight,SysDate,Remarks,InvNo,Deppt,PalatWt,MainRemarks,BuyerOrdNo,PACKINGLISTNO,ReceivingCompany) values('"
                                          + dataGridView1.Rows[i].Cells[BailNo.Index].Value + "','"
                                          + comboCompanyName.Text + "','" + dataGridView1.Rows[i].Cells[PartyName.Index].Value + "','"
                                          + dataGridView1.Rows[i].Cells[MarketingOrdNo.Index].Value + "','"
                                          + Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[BailPcs.Index].Value) + "',"
                                          + Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[BailWt.Index].Value) + ",'"
                                          + dateLoomEntry.Value.ToString("yyyy-MM-dd") + "','"
                                          + TextRemarks.Text + "','" + textInvno.Text + "','Small Bag',"
                                          + Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[PalatWt.Index].Value) + ",'"
                                          + TextRemarks.Text + "','" + dataGridView1.Rows[i].Cells[BuyerOrdNo.Index].Value
                                          + "','" + PackingListNo + "','" + internalcompanyname + "')"))
                                {
                                    isentry = true;
                                    invoiceRuleKg += Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[BailWt.Index].Value);
                                }
                            }
                        }
                        // INVOICE-RULE: bale kg sit on Despatch board as Small Bags WIP until packing list is mapped.
                        if (isentry)
                            InvoiceRule_MoveWipToDespatch(comboCompanyName.Text, "Small Bag Godown", "Small Bags WIP", invoiceRuleKg);
                    }
                    if (isentry)
                    {
                        Database.CommitTransaction();
                        Database.Closeconnection();
                        Utility.UserInformation("Save ", "Smallbag Despatch Entry " + dateLoomEntry.Value.ToString("yyyy-MM-dd") + "is Saved for Partyname " + comboPartyName.Text);
                        MessageBox.Show("Data Saved Succesfully");
                        dataGridView1.Rows.Clear();
                        textInvno.Text = "";
                        BindGrid_data();
                        BindGrid();
                    }
                    BindGrid();
                }

            }
            catch (Exception ex)
            {
                Database.RollBackTransaction();
                ErrorMessageBox.Show(ex);
            }
        }



        private void buttonDelete_Click(object sender, EventArgs e)
        {
            try
            {
                if (Database.OpenConnection(Utility.DespacthConnectionString))
                {
                    int intrec = Convert.ToInt32(Database.GetExecuteNonQueryCommand_retrun("select count(*) from MappPackingList where PACKINGLISTNO like '%" + TxtPackingList.Text + "%'"));
                    if (intrec > 0)
                    {
                        MessageBox.Show("Invoice is already Generated.\nYou can't delete this Record");
                        return;
                    }
                }
                if (Database.OpenConnection(Utility.MaterialConnectionString))
                {
                    if (Database.GetExecuteNonQueryCommand("delete from SmallBagBailForDespatch where Deppt='Small Bag' and transid = " + transid + ""))
                        MessageBox.Show("Data Deleted Succesfully");

                    Database.Closeconnection();
                    isdoubleclick = true;
                    dataGridView1.Rows.Clear();
                    isdoubleclick = false;
                    Utility.UserInformation("Delete ", "Bail Entry " + transid + "is Delete for Bail No " + dataGridView1[1, 0].Value);

                }
                BindGrid();

            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
            }
        }

        private void comboPartyName_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!BtnUpdate.Enabled)
                BindGrid_data();
        }
        private void BindGrid()
        {
            if (Database.OpenConnection_Adapter(Utility.MaterialConnectionString))
            {
                DateTime currentdatetime = dateLoomEntry.Value;
                DateTime firstdate = new DateTime(currentdatetime.Year, currentdatetime.Month, 1);
                Database.myadapter = Database.GetAdapterCommand("select * from SmallBagBailForDespatch where CompanyName = '"
                    + comboCompanyName.Text + "' and SysDate >= '" + firstdate.ToString("yyyy-MM-dd") + "' and Deppt='Small Bag'   order by TransId desc");
                DataSet dataset1 = new DataSet();
                Database.myadapter.Fill(dataset1, "SmallBagBailForDespatch");

                BindingSource datasource = new BindingSource(dataset1.Tables[0], null);
                dataGridView2.DataSource = datasource;

                // Add the AutoFilter header cell to each column.
                foreach (DataGridViewColumn col in dataGridView2.Columns)
                {
                    col.HeaderCell = new
                        DataGridViewAutoFilterColumnHeaderCell(col.HeaderCell);
                }
            }
        }

        private void dateLoomEntry_ValueChanged(object sender, EventArgs e)
        {
            BindGrid_data();
            BindGrid();
            isdoubleclick = true;
            dataGridView1.Rows.Clear();
            isdoubleclick = false;
        }



        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            BindGrid_data();
            isdoubleclick = true;
            dataGridView1.Rows.Clear();
            isdoubleclick = false;
        }

        private void dataGridView3_SelectionChanged(object sender, EventArgs e)
        {

        }

        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            try
            {
                double sum = 0;
                pfrm_sbp1.Text = "";

                foreach (DataGridViewRow row in this.dataGridView1.Rows)
                {
                    foreach (DataGridViewColumn col in this.dataGridView1.Columns)
                    {
                        if (dataGridView1[col.Index, row.Index].Selected)
                            try
                            {
                                sum += Convert.ToDouble(dataGridView1[col.Index, row.Index].Value);
                            }
                            catch (Exception ex)
                            {
                                ex.ToString();
                            }
                    }
                }
                pfrm_sbp1.Text = " Sum : " + sum.ToString();

            }
            catch (Exception ex)
            {
                ex.ToString();
            }
        }

        public void BindDrop()
        {
            DataTable dt = new DataTable();
            dt = dataset1Roll.Tables[0];
            Ro = new DataView(dt);
            comboRollNo.DataSource = Ro;
            comboRollNo.DisplayMember = "RollNo";
        }

        private void comboRollNo_KeyUp(object sender, KeyEventArgs e)
        {
            if (comboRollNo.Text.Length < 3) return;

            if ((e.KeyCode >= Keys.A && e.KeyCode <= Keys.Z) || (e.KeyCode >= Keys.D0 && e.KeyCode <= Keys.D9) || (e.KeyCode >= Keys.NumPad0 && e.KeyCode <= Keys.NumPad9) || e.KeyCode == Keys.Back || e.KeyCode == Keys.Delete)
            {
                if (e.KeyCode == Keys.Back || e.KeyCode == Keys.Delete)
                    isDelete = true;

                FilterComboBox();
            }
        }
        public void FilterComboBox()
        {
            try
            {
                if (!isDelete)
                {
                    string SearchFor = comboRollNo.Text;
                    Ro.RowFilter = string.Concat("rollno LIKE '%", SearchFor, "%'");
                    comboRollNo.DataSource = Ro;
                    comboRollNo.DisplayMember = "RollNo";

                }
                else
                {

                    DataTable dt = new DataTable();
                    dt = dataset1Roll.Tables[0];
                    Ro = new DataView(dt);
                    comboRollNo.DataSource = Ro;
                    comboRollNo.DisplayMember = "RollNo";

                }
                comboRollNo.Height = 30;

                isDelete = false;
            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                for (int j = 0; j < dataGridView1.Rows.Count; j++)
                {
                    if ((Convert.ToString(dataGridView1.Rows[j].Cells[0].Value)) == (Convert.ToString(comboRollNo.Text)))
                    {
                        dataGridView1.Rows[j].Selected = true;
                        dataGridView1.FirstDisplayedScrollingRowIndex = j;
                    }
                    else
                    {
                        dataGridView1.Rows[j].Selected = false;
                    }
                }

                BindDrop();
                return;

            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
            }
        }

        private void selectCheckBailToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (dataGridView1.CurrentCell == null
                         || dataGridView1.CurrentCell.RowIndex == -1)
                return;

            Tools.ToolsCommonFunction.CheckSelectedRows_Datagridview(ref dataGridView1, dataGridView1.CurrentCell.ColumnIndex);
        }

        private void comboPartyName1_Leave(object sender, EventArgs e)
        {
            try
            {

                if (Database.OpenConnection(Utility.MaterialConnectionString))
                {
                    if (dataGridView1.Rows.Count - 1 > 0)
                    {
                        DialogResult drs = MessageBox.Show("List is already avaialble below. Do you want to Clear the list? Once You click on Yes then all Selected Filers will be Remove and you have to again select Bail list"
                                    , "Despatch Entry", MessageBoxButtons.YesNo, MessageBoxIcon.Information);

                        if (drs.CompareTo(DialogResult.No) == 0)
                        {
                            return;
                        }
                    }
                    dataGridView1.Rows.Clear();

                    for (int j = 0; j <= comboPartyName1.CheckedItems.Count - 1; j++)
                    {
                        string strsql = " Exec sp_Prod_SB_DispatchBail  N'" + comboCompanyName.Text + "','" + dateLoomEntry.Value.ToString("yyyy-MM-dd") + "','" + comboPartyName1.CheckedItems[j].ToString() + "'";
                        Database.myreader = Database.GetExecuteReaderCommand(strsql);
                        DataTable dt = new DataTable();
                        dt.Load(Database.myreader);
                        BindingSource datasource = new BindingSource(dt, null);

                        DataTable dtTmp = new DataTable();
                        dtTmp = dt;

                        for (int i = 0; i < dtTmp.Rows.Count; i++)
                        {
                            dataGridView1.Rows.Add(
                                false,
                                (
                                (DateTime)dtTmp.Rows[i]["SysDate"]).ToString("MM/dd/yyyy"),
                                Convert.ToString(dtTmp.Rows[i]["PartyName"]),
                                Convert.ToString(dtTmp.Rows[i]["MarketingOrdNo"]),
                                Convert.ToString(dtTmp.Rows[i]["BuyerOrderNo"]),
                                Convert.ToString(dtTmp.Rows[i]["BailNo"]),
                                Convert.ToString(dtTmp.Rows[i]["BailPcs"]),
                                Convert.ToString(dtTmp.Rows[i]["BailWt"]),
                                Convert.ToString(dtTmp.Rows[i]["PalatWt"]),
                                Convert.ToString(dtTmp.Rows[i]["transId"]),
                                Convert.ToString(dtTmp.Rows[i]["BagSize"]),
                                Convert.ToString(dtTmp.Rows[i]["Remark"]),
                                Convert.ToString(dtTmp.Rows[i]["InternalBailNo"])
                                );
                        }
                    }

                    //dataGridView1.DataSource = dtTmp.Copy();
                    for (int i = 0; i <= dataGridView1.Columns.Count - 1; i++)
                    {
                        if (i != 0)
                        {
                            // dataGridView1.Columns[i].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
                            //dataGridView1.Columns[i].Resizable = DataGridViewAutoSizeColumnMode.;
                            if (dataGridView1.Columns[i].Name == "BailPcs" || dataGridView1.Columns[i].Name == "BailWt")
                                ERP.FormatStyle.RowReadOnly(dataGridView1.Columns[i], false);
                            else
                                ERP.FormatStyle.RowReadOnly(dataGridView1.Columns[i], true);
                        }
                    }


                }
            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
            }
        }

        private void dataGridView2_DoubleClick(object sender, EventArgs e)
        {
            isdoubleclick = true;
            BtnUpdate.Enabled = true;
            buttonDelete.Enabled = true;
            buttonSave.Enabled = false;
            comboPartyName1.Visible = false;
            comboPartyName.Visible = true;
            btnload.Visible = true;

            if (dataGridView1.Rows.Count > 1)
                dataGridView1.Rows.Clear();
            try
            {
                dateLoomEntry.Value = Convert.ToDateTime(dataGridView2["sysdate", dataGridView2.CurrentRow.Index].FormattedValue);

                dataGridView1.Rows[0].Cells["sysDate"].Value = Convert.ToString(dataGridView2["sysdate", dataGridView2.CurrentRow.Index].FormattedValue);
                dataGridView1.Rows[0].Cells["PartyName"].Value = Convert.ToString(dataGridView2["PartyName", dataGridView2.CurrentRow.Index].FormattedValue);
                dataGridView1.Rows[0].Cells["MarketingOrdNo"].Value = Convert.ToString(dataGridView2["MarketingOrdNo", dataGridView2.CurrentRow.Index].FormattedValue);
                dataGridView1.Rows[0].Cells["BuyerOrdNo"].Value = Convert.ToString(dataGridView2["BuyerOrdNo", dataGridView2.CurrentRow.Index].FormattedValue);
                dataGridView1.Rows[0].Cells["BailNo"].Value = Convert.ToString(dataGridView2["BailNo", dataGridView2.CurrentRow.Index].FormattedValue);
                dataGridView1["BailPcs", 0].Value = Convert.ToString(dataGridView2["pcs", dataGridView2.CurrentRow.Index].FormattedValue);
                dataGridView1["BailWt", 0].Value = Convert.ToString(dataGridView2["Weight", dataGridView2.CurrentRow.Index].FormattedValue);
                dataGridView1["PalatWt", 0].Value = 0;
                dataGridView1.Rows[0].Cells["Id"].Value = 0;


                transid = Convert.ToInt32(dataGridView2["transid", dataGridView2.CurrentRow.Index].FormattedValue.ToString());
                comboPartyName.Text = Convert.ToString(dataGridView2["PartyName", dataGridView2.CurrentRow.Index].FormattedValue);
                textInvno.Text = Convert.ToString(dataGridView2["InvNo", dataGridView2.CurrentRow.Index].FormattedValue);
                TextRemarks.Text = Convert.ToString(dataGridView2["Remarks", dataGridView2.CurrentRow.Index].FormattedValue);
                TxtPackingList.Text = Convert.ToString(dataGridView2["PACKINGLISTNO", dataGridView2.CurrentRow.Index].FormattedValue);
                if (Convert.ToString(dataGridView2["ReceivingCompany", dataGridView2.CurrentRow.Index].FormattedValue).Length > 0)
                {
                    comboInternalCompny.Text = Convert.ToString(dataGridView2["ReceivingCompany", dataGridView2.CurrentRow.Index].FormattedValue);
                    checkBox1.Checked = true;
                }
                else
                {
                    comboInternalCompny.Text = "";
                    checkBox1.Checked = false;
                }

            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
            }
        }

        private void BtnUpdate_Click(object sender, EventArgs e)
        {
            string PackingListNo = "";
            PackingListNo = TxtPackingList.Text;

            if (Database.OpenConnection(Utility.DespacthConnectionString))
            {
                int intrec = Convert.ToInt32(Database.GetExecuteNonQueryCommand_retrun("select count(*) from MappPackingList where PACKINGLISTNO like '%" + TxtPackingList.Text + "%'"));
                if (intrec > 0)
                {
                    MessageBox.Show("Invoice is already Generated.\nYou can't delete this Record");
                    return;
                }
            }
            
            //TxtPackingList.Text = PackingListNo;
            try
            {
                if (Validations())
                {
                    bool isentry = false;
                    if (Database.OpenConnection(Utility.MaterialConnectionString))
                    {
                        Database.BeginTransaction();
                        for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                        {
                            if ((bool)dataGridView1.Rows[i].Cells["Column1"].FormattedValue == true)
                            {
                                Database.GetExecuteNonQueryCommand(" Delete from  SmallBagBailForDespatch where transid=" + transid);

                                if (Database.GetExecuteNonQueryCommand("insert into SmallBagBailForDespatch(BailNo,CompanyName,PartyName,MarketingOrdNo,PCS,Weight,SysDate,Remarks,InvNo,Deppt,PalatWt,MainRemarks,BuyerOrdNo,PACKINGLISTNO) values('"
                                          + dataGridView1.Rows[i].Cells[BailNo.Index].Value + "','"
                                          + comboCompanyName.Text + "','" + dataGridView1.Rows[i].Cells[PartyName.Index].Value + "','"
                                          + dataGridView1.Rows[i].Cells[MarketingOrdNo.Index].Value + "','"
                                          + Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[BailPcs.Index].Value) + "',"
                                          + Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[BailWt.Index].Value) + ",'"
                                          + dateLoomEntry.Value.ToString("yyyy-MM-dd") + "','"
                                          + TextRemarks.Text + "','" + textInvno.Text + "','Small Bag',"
                                          + Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[PalatWt.Index].Value) + ",'"
                                          + TextRemarks.Text + "','" + dataGridView1.Rows[i].Cells[BuyerOrdNo.Index].Value + "','" + PackingListNo + "')"))
                                {
                                    isentry = true;
                                    string internalcompanyname = "";
                                    if (checkBox1.Checked)
                                    {
                                        internalcompanyname = comboInternalCompny.Text;
                                        DialogResult drs = MessageBox.Show("Do you want to save Despatch entry to " + internalcompanyname + " ?"
                                        , "Despatch Entry", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                                        if (drs.CompareTo(DialogResult.Yes) == 0)
                                        {
                                            Database.GetExecuteNonQueryCommand("UPDATE from SmallBagBailForDespatch where ReceivingCompany='" + internalcompanyname + "' "
                                                + " where PackingListNo='" + TxtPackingList.Text + "'");
                                        }
                                    }
                                }
                            }
                        }
                        if (checkBox1.Checked)
                        {
                            string internalcompanyname = comboInternalCompny.Text;
                            DialogResult drs = MessageBox.Show("Do you want to save Despatch entry to " + internalcompanyname + " ?"
                            , "Despatch Entry", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                            if (drs.CompareTo(DialogResult.Yes) == 0)
                            {
                                Database.GetExecuteNonQueryCommand("UPDATE from SmallBagBailForDespatch where ReceivingCompany='" + internalcompanyname + "' "
                                    + " where PackingListNo='" + TxtPackingList.Text + "'");
                            }
                        }
                    }
                    if (isentry)
                    {
                        Database.CommitTransaction();
                        Database.Closeconnection();
                        Utility.UserInformation("Update", "Smallbag Despatch Entry " + dateLoomEntry.Value.ToString("yyyy-MM-dd") + "is Saved for Partyname " + comboPartyName.Text);
                        MessageBox.Show("Data Saved Succesfully");
                        dataGridView1.Rows.Clear();
                        textInvno.Text = "";
                        BindGrid_data();
                        BindGrid();
                    }
                    BindGrid();
                }

            }
            catch (Exception ex)
            {
                Database.RollBackTransaction();
                ErrorMessageBox.Show(ex);
            }
        }

        private void btnload_Click(object sender, EventArgs e)
        {
            if ((DateTime.Now - dateLoomEntry.Value).TotalDays > 7)
            {
                MessageBox.Show("Despatch date is more than 7 days.Updation in packing list is not allowed");
                return;
            }
            if (dataGridView1.Rows.Count > 0)
            {
                DialogResult drs = MessageBox.Show("Load More Bails in Already Despatch Entry"
                            , "Despatch Entry", MessageBoxButtons.YesNo, MessageBoxIcon.Information);

                if (drs.CompareTo(DialogResult.No) == 0)
                {
                    return;
                }
            }

            if (Database.OpenConnection(Utility.MaterialConnectionString))
            {
                //string strsql = "select SysDate,PartyName,MarketingOrdNo,v.BuyerOrderNo,sb.BailNo, PCS as BailPcs,Weight as Bailwt,sb.PalatWt,sb.ProductionSmallBagEntryID as Transid from SmallBagBaillingEntry"
                //    + " sb left join Despatch..vw_ProductionCompAndMarkInvNo v on v.itemno = sb.MarketingOrdNo and v.buyername = sb.PartyName and"
                //    + " v.ProductionCompanyname = sb.CompanyName where rollno not in (select BailNo from SmallBagBailForDespatch where CompanyName='" + comboCompanyName.Text + "' and"
                //+ " PartyName='" + comboPartyName.Text + "' and Deppt='Small Bag') and sb.CompanyName='" + comboCompanyName.Text + "' and PartyName='" + comboPartyName.Text + "' ";

                string strsql = " Exec sp_Prod_SB_DispatchBail  N'" + comboCompanyName.Text + "','" + dateLoomEntry.Value.ToString("yyyy-MM-dd") + "','" + comboPartyName.Text + "'";
                Database.myreader = Database.GetExecuteReaderCommand(strsql);
                DataTable dt = new DataTable();
                dt.Load(Database.myreader);
                BindingSource datasource = new BindingSource(dt, null);
                //dataGridView1.Rows.Clear();
                DataTable dtTmp = new DataTable();
                dtTmp = dt;

                for (int i = 0; i < dtTmp.Rows.Count; i++)
                {
                    dataGridView1.Rows.Add(
                        false,
                        (
                        (DateTime)dtTmp.Rows[i]["SysDate"]).ToString("MM/dd/yyyy"),
                        Convert.ToString(dtTmp.Rows[i]["PartyName"]),
                        Convert.ToString(dtTmp.Rows[i]["MarketingOrdNo"]),
                        Convert.ToString(dtTmp.Rows[i]["BuyerOrderNo"]),
                        Convert.ToString(dtTmp.Rows[i]["BailNo"]),
                        Convert.ToString(dtTmp.Rows[i]["BailPcs"]),
                        Convert.ToString(dtTmp.Rows[i]["BailWt"]),
                        Convert.ToString(dtTmp.Rows[i]["PalatWt"]),
                        Convert.ToString(dtTmp.Rows[i]["transId"]),
                        Convert.ToString(dtTmp.Rows[i]["BagSize"]),
                        Convert.ToString(dtTmp.Rows[i]["Remark"]),
                        Convert.ToString(dtTmp.Rows[i]["InternalBailNo"])
                        );
                    dataGridView1.Rows[dataGridView1.Rows.Count - 2].DefaultCellStyle.BackColor = Color.GreenYellow;
                }
                //dataGridView1.DataSource = dtTmp.Copy();
                for (int i = 0; i <= dataGridView1.Columns.Count - 1; i++)
                {
                    if (i != 0)
                    {

                        if (dataGridView1.Columns[i].Name == "PCS" || dataGridView1.Columns[i].Name == "Weight")
                            ERP.FormatStyle.RowReadOnly(dataGridView1.Columns[i], false);
                        else
                            ERP.FormatStyle.RowReadOnly(dataGridView1.Columns[i], true);
                    }
                }
            }

        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            btninup.Visible = checkBox1.Checked;
            comboInternalCompny.Visible = checkBox1.Checked;
        }

        private void btninup_Click(object sender, EventArgs e)
        {
            try
            {
                if (Database.OpenConnection(Utility.DespacthConnectionString))
                {
                    int intrec = Convert.ToInt32(Database.GetExecuteNonQueryCommand_retrun("select count(*) from MappPackingList where PACKINGLISTNO like '%" + TxtPackingList.Text + "%'"));
                    if (intrec > 0)
                    {
                        MessageBox.Show("Invoice is already Generated.\nYou can't delete this Record");
                        return;
                    }
                }
                if (checkBox1.Checked)
                {
                    if (Database.OpenConnection(Utility.MaterialConnectionString))
                    {
                        string internalcompanyname = comboInternalCompny.Text;
                        DialogResult drs = MessageBox.Show("Do you want to save Despatch entry to " + internalcompanyname + " ?"
                        , "Despatch Entry", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                        if (drs.CompareTo(DialogResult.Yes) == 0)
                        {
                            Database.GetExecuteNonQueryCommand("UPDATE SmallBagBailForDespatch set  ReceivingCompany='" + internalcompanyname + "' "
                                + " where PackingListNo='" + TxtPackingList.Text + "'");
                            BindGrid_data();
                            BindGrid();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Problem with Internal Despatch Name selection :" + ex.Message);
            }
        }

        private void comboPartyName1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        // INVOICE-RULE: minus WIP kg from Small Bag Godown, plus same WIP kg on Despatch Godown.
        private void InvoiceRule_MoveWipToDespatch(string company, string fromGodown, string wipItemName, double kg)
        {
            if (kg == 0 || company == null || company.Trim() == "")
                return;
            string itemCode = "";
            Database.myreader = Database.GetExecuteReaderCommand("select top 1 isnull(ItemCode,'') as itemcode from item with(nolock) where todate is null and CompanyName='"
                + company + "' and itemname='" + wipItemName + "' order by fromdate desc ");
            if (Database.myreader.Read())
                itemCode = Database.myreader[0].ToString();
            Database.myreader.Close();
            if (itemCode == null || itemCode.Trim() == "")
                return;
            Database.GetExecuteNonQueryCommand("Update Warehouse set stkinHand=case when Round(stkinHand - " + kg + ",2)<0 then 0 else Round(stkinHand - " + kg + ",2) end where CompanyName='"
                + company + "' and WarehouseName='" + fromGodown + "' and ItemCode='" + itemCode + "'");
            int exists = Convert.ToInt32(Database.GetExecuteNonQueryCommand_retrun("Select count(*) from Warehouse where itemcode='"
                + itemCode + "' and companyname='" + company + "' and warehousename='Despatch Godown'"));
            if (exists == 0)
            {
                Database.GetExecuteNonQueryCommand("Insert into warehouse (ItemCode,ItemName,GroupName,Deptt,Minlevel,Maxlevel,ReOrder,Unit,StkInHand,TypeVendor,NameVendor,EmailVendor,QtyOrder,MnthlyConsum,Rate,CompanyName,LoginName,Dummy1,OpeningStock,SubGroupName,WareHouseName) "
                    + "select ItemCode,ItemName,GroupName,Deptt,Minlevel,Maxlevel,ReOrder,Unit,0 StkInHand,TypeVendor,NameVendor,EmailVendor,QtyOrder,MnthlyConsum,Rate,CompanyName,LoginName,Dummy1,OpeningStock,SubGroupName,'Despatch Godown' from item where itemcode='"
                    + itemCode + "' and companyname='" + company + "'");
            }
            Database.GetExecuteNonQueryCommand("Update Warehouse set stkinHand=Round(stkinHand + " + kg + ",2) where CompanyName='"
                + company + "' and WarehouseName='Despatch Godown' and ItemCode='" + itemCode + "'");
        }
    }
}





