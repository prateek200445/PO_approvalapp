using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using DataGridViewAutoFilter;
using System.Collections;

namespace ERP
{
    public partial class FrmSubsidiaryChallanMulti : Form
    {
        int SrNo = 0;
        DataTable dtIMaster;
        string val = "";
        bool flag = false;
        bool flagupdate = true;

        public FrmSubsidiaryChallanMulti()
        {
            InitializeComponent();
            Form_Load();
        }

        private void Form_Load()
        {
            if (Database.OpenConnection(Utility.DespacthConnectionString))
            {
                DespatchCommonFunction.GetCompanyList(comboProcessorName);
                //string strsql = ("select Companyname from companymaster order by companyname");
                //string strsql = "Select distinct companyname from CompanyMaster where   " +
                //          "   isnull(IsFreeze,'no')='no' and " +                          
                //          "  todate is null  order by CompanyName";

                //DespatchCommonFunction.Loadcombo(comboSubsidiaryBuyer, strsql, "Companyname", "Companyname", Utility.DespacthConnectionString);                
                Database.Closeconnection();

            }
            textYear.Text = frmDefaultVale.Year;
            comboProcessorName.Text = frmDefaultVale.CompanyName;

            fillData();

            BindGrid();
        }


        void fillData()
        {

            if (Database.OpenConnection(Utility.DespacthConnectionString))
            {
                Database.myadapter = Database.GetAdapterCommand("Select CommodityName from Commodity where companyname = '"
                    + comboProcessorName.Text + "' and nToDate is null order by CommodityName");
                DataTable dt = new DataTable();
                Database.myadapter.Fill(dt);
                DtCommodity.DataSource = dt;
                DtCommodity.ValueMember = dt.Columns[0].ColumnName.ToString();
                DtCommodity.DisplayMember = dt.Columns[0].ColumnName.ToString();

            }

            //if (Database.OpenConnection(Utility.MaterialConnectionString))
            {

                Database.myadapter = Database.GetAdapterCommand("Select UnitName from MaterialProcessing.dbo.Unit order by UnitName");
                DataTable dt = new DataTable();
                Database.myadapter.Fill(dt);

                DtUnit.DataSource = dt;
                DtUnit.ValueMember = dt.Columns[0].ColumnName.ToString();
                DtUnit.DisplayMember = dt.Columns[0].ColumnName.ToString();


                #region ITem Master Added 22.06.2020
                dtIMaster = new DataTable();
                dtIMaster = DespatchCommonFunction.GetItemMaster(Tools.ToolsCommonFunction.StartYear(textYear.Text, "Fromdate").ToString("yyyy-MM-dd"), frmDefaultVale.CompanyName);
                colItemCode.DataSource = dtIMaster;
                colItemCode.ValueMember = dtIMaster.Columns[0].ColumnName.ToString();
                colItemCode.DisplayMember = dtIMaster.Columns[1].ColumnName.ToString();
                DataTable dtdeptt = DespatchCommonFunction.GetItemMainGrp();
                colMainGrp.DataSource = dtdeptt;
                colMainGrp.ValueMember = dtdeptt.Columns[0].ColumnName.ToString();
                colMainGrp.DisplayMember = dtdeptt.Columns[0].ColumnName.ToString();
                #endregion

            }

        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            try
            {
                if (IsValidate())
                {
                    bool IsAvail = false;
                    ArrayList arr = new ArrayList();
                    if (Database.OpenConnection(Utility.DespacthConnectionString))
                    {
                        for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                        {
                            Database.myreader = Database.GetExecuteReaderCommand("select year from subsidiarychallanItemMulti WITH (NOLOCK)  where subsidiaryBuyer = '"
                                                   + comboSubsidiaryBuyer.Text + "' and year = '" + textYear.Text
                                                   + "' and MainChallanNo = '"
                                                   + dataGridView1.Rows[i].Cells[Column6.Index].Value.ToString() + "' and ProcessorName = '"
                                                   + comboProcessorName.Text + "'");
                            if (Database.myreader.Read())
                            {
                                IsAvail = true;
                                break;
                            }
                            Database.myreader.Close();
                        }
                        Database.Closeconnection();
                    }
                    if (IsAvail)
                        MessageBox.Show("Main Challan No is already in Subsidary Order List");
                    else if (dataGridView1.Rows.Count - 1 == 0)
                        MessageBox.Show("Please mention at least One Item");
                    else if (textNatureofProcessing.Text == "")
                        MessageBox.Show("Please enter value in Nature of Processing");

                    else
                    {
                        for (int i = 0; i < dataGridView1.Rows.Count; i++)
                        {
                            string val1 = dataGridView1.Rows[i].Cells[Column6.Index].FormattedValue.ToString();

                            int count = 1;
                            for (int j = 0; j < i; j++)
                            {
                                if (dataGridView1.Rows[j].Cells[Column6.Index].FormattedValue.ToString().Trim() == dataGridView1.Rows[i].Cells[Column6.Index].FormattedValue.ToString().Trim())
                                {
                                    count++;
                                }
                            }
                            if (count == 1)
                                val = val1;
                            else
                                val = val1 + "-" + count;
                            arr.Add(val);

                        }
                        DialogResult drs = MessageBox.Show("Do you want to generate Subsidary Challan Order "
                                                            , "Generate Order"
                                          , MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                        if (drs.CompareTo(DialogResult.Yes) == 0)
                        {
                            if (Database.OpenConnection(Utility.DespacthConnectionString))
                            {
                                Database.BeginTransaction();
                                int srno = 0;
                                Database.myreader = Database.GetExecuteReaderCommand("select top 1 srno from subsidiarychallanMulti order by srno desc");
                                if (Database.myreader.Read())
                                    srno = Convert.ToInt32(Database.myreader[0].ToString());
                                Database.myreader.Close();
                                srno = srno + 1;
                                Database.GetExecuteNonQueryCommand("insert into subsidiarychallanMulti values('"
                                         + comboProcessorName.Text + "','" + textProcessorAdd.Text
                                         + "','" + comboSubsidiaryBuyer.Text + "','" + textSubsidiaryBuyerAdd.Text
                                         + "','" + textNatureofProcessing.Text + "','" +
                                         textYear.Text + "'," + srno + ")");

                                for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                                {
                                    Database.GetExecuteNonQueryCommand("insert into subsidiarychallanitemMulti values('"
                                     + textYear.Text + "','" + comboProcessorName.Text
                                     + "','" + comboSubsidiaryBuyer.Text + "','"
                                     + Convert.ToString(arr[i]).Trim() + "','"
                                     + Convert.ToDateTime(dataGridView1.Rows[i].Cells[Column7.Index].Value).ToString("MM/dd/yyyy") + "','"
                                     + Convert.ToString(dataGridView1.Rows[i].Cells[DtCommodity.Index].Value) + "','"
                                     + Convert.ToString(dataGridView1.Rows[i].Cells[Column8.Index].Value) + "','"
                                     + Convert.ToString(dataGridView1.Rows[i].Cells[Column2.Index].Value) + "','"
                                     + Convert.ToString(dataGridView1.Rows[i].Cells[DtUnit.Index].Value) + "','"
                                     + Convert.ToString(dataGridView1.Rows[i].Cells[Column5.Index].Value) + "',"
                                    + srno + ",'" + Convert.ToString(dataGridView1.Rows[i].Cells[colItemCode.Index].Value) + "',0)");
                                }
                                MessageBox.Show("Saved Successfully");
                                Database.CommitTransaction();
                                ClearAll();
                            }
                        }
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

        private void FrmSubsidiaryChallanMulti_Load(object sender, EventArgs e)
        {
        }

        private void dataGridView1_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            try
            {
                if (dataGridView1.CurrentCell.ColumnIndex == DtCommodity.Index) // For Commodity ComboBox
                {
                    ComboBox comboBox = e.Control as ComboBox;
                    comboBox.SelectedIndexChanged += new EventHandler(comboBox_SelectedIndexChanged);
                }
                if (this.dataGridView1.CurrentCellAddress.X == colItemCode.Index)
                {
                    ComboBox cb = e.Control as ComboBox;
                    if (cb != null)
                    {
                        cb.DropDownStyle = ComboBoxStyle.DropDown;
                        //cb.AutoCompleteMode = AutoCompleteMode.Suggest;
                        new ComboFilter(cb, true);
                    }
                }
            }
            catch (Exception ex)
            {
            }
        }

        void comboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            ComboBox cb = ((ComboBox)sender);
            try
            {
                if (dataGridView1.CurrentCell.ColumnIndex == DtCommodity.Index) // For Commodity ComboBox
                {
                    Database.OpenConnection(Utility.DespacthConnectionString);
                    Database.myreader = Database.GetExecuteReaderCommand("select THeading from commodity WITH (NOLOCK) where CommodityName ='"
                              + cb.Text + "' and companyname = '" + comboProcessorName.Text + "'");
                    if (Database.myreader.Read())
                        dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[Column8.Index].Value = Database.myreader[0].ToString();
                    Database.myreader.Close();
                }
            }
            catch (Exception ex)
            {
                ex.ToString();
            }
        }

        private void comboProcessorName_SelectedIndexChanged(object sender, EventArgs e)
        {

            if (Database.OpenConnection(Utility.DespacthConnectionString))
            {
                string strsql = "Select companyname from companymaster where   " +
                   "   isnull(IsFreeze,'no')='no' and (('" + DateTime.Now.ToString("yyyy-MM-dd") +
                   "' between fromdate and todate) or (fromdate <= '" + DateTime.Now.ToString("yyyy-MM-dd") +
                   "' and todate is null ))  and CompanyCode not in " +
                   " ( select LCompanyCode from MaterialProcessing.dbo.LedgerMaster where (Todate is not null or IsAccClose='yes' ) and  companyname='" + comboProcessorName.Text +
                   "'  and isnull(LCompanyCode,'')<>'') order by companyname";
                DespatchCommonFunction.LoadCustomerUnlock(comboSubsidiaryBuyer, comboProcessorName.Text, DateTime.Now);

                Database.myreader = Database.GetExecuteReaderCommand("select address from factoryinfo where name = '"
                     + comboProcessorName.Text + "'");
                if (Database.myreader.Read())
                    textProcessorAdd.Text = Database.myreader[0].ToString();
                Database.myreader.Close();
                Database.Closeconnection();
            }
        }

        private void BindGrid()
        {
            try
            {
                if (Database.OpenConnection_Adapter(Utility.DespacthConnectionString))
                {
                    Database.myadapter = Database.GetAdapterCommand("select * from vw_subsidiaryChallanItem where year = '" + textYear.Text + "' and processorname = '" +
                       comboProcessorName.Text + "' order by challansrno desc");
                    DataSet dataset1 = new DataSet();
                    Database.myadapter.Fill(dataset1, "vw_subsidiaryChallanItem");
                    BindingSource datasource = new BindingSource(dataset1.Tables[0], null);
                    dataGridView2.DataSource = datasource;
                    foreach (DataGridViewColumn col in dataGridView2.Columns)
                    {
                        col.HeaderCell = new
                            DataGridViewAutoFilterColumnHeaderCell(col.HeaderCell);
                    }
                    dataGridView1.Columns[2].Width = 300;
                }
            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
            }
        }

        private void comboSubsidiaryBuyer_SelectedIndexChanged(object sender, EventArgs e)
        {
            textSubsidiaryBuyerAdd.Text = "";
            if (Database.OpenConnection(Utility.DespacthConnectionString))
            {
                Database.myreader = Database.GetExecuteReaderCommand("select address,city,state,country from companymaster where companyname = '"
                     + comboSubsidiaryBuyer.Text + "'");
                while (Database.myreader.Read())
                {
                    textSubsidiaryBuyerAdd.Text = Database.myreader[0].ToString();
                    textSubsidiaryBuyerAdd.Text += "," + Database.myreader[1].ToString();
                    textSubsidiaryBuyerAdd.Text += "," + Database.myreader[2].ToString();
                    textSubsidiaryBuyerAdd.Text += "," + Database.myreader[3].ToString();
                }
                Database.myreader.Close();
                Database.Closeconnection();
            }
        }

        private void dataGridView2_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void dataGridView2_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            try
            {

                btnSave.Enabled = false;
                btnUpdate.Enabled = true;
                textYear.Text = Convert.ToString(dataGridView2.Rows[dataGridView2.CurrentCell.RowIndex].Cells[0].Value);
                comboProcessorName.Text = Convert.ToString(dataGridView2.Rows[dataGridView2.CurrentCell.RowIndex].Cells[1].Value);
                comboSubsidiaryBuyer.Text = Convert.ToString(dataGridView2.Rows[dataGridView2.CurrentCell.RowIndex].Cells[2].Value);

                textProcessorAdd.Text = Convert.ToString(dataGridView2.Rows[dataGridView2.CurrentCell.RowIndex].Cells[11].Value);
                textSubsidiaryBuyerAdd.Text = Convert.ToString(dataGridView2.Rows[dataGridView2.CurrentCell.RowIndex].Cells[12].Value);
                textNatureofProcessing.Text = Convert.ToString(dataGridView2.Rows[dataGridView2.CurrentCell.RowIndex].Cells[13].Value);

                int GroupSrNo = Convert.ToInt32(dataGridView2.Rows[dataGridView2.CurrentCell.RowIndex].Cells[10].Value);
                dataGridView1.Rows.Clear();
                Database.OpenConnection(Utility.DespacthConnectionString);
                object j = Database.GetExecuteScalarCommand_return("select count(*) from subsidiarychallanitemMulti where ChallanSrno = "
                     + GroupSrNo);
                int TotalCount = Convert.ToInt32(j);
                SrNo = GroupSrNo;

                if (TotalCount > 0)
                {
                    dataGridView1.Rows.Add(TotalCount);
                    int i = 0;
                    Database.myreader = Database.GetExecuteReaderCommand("select MainChallanNo,MainChallanDate,CommodityName"
                        + ",Theading,OrderQty,QtyUnit,BalancedQty,Itemcode from subsidiarychallanitemMulti where ChallanSrno = "
                    + GroupSrNo);
                    while (Database.myreader.Read())
                    {
                        dataGridView1.Rows[i].Cells[Column6.Index].Value = Database.myreader[0].ToString();
                        dataGridView1.Rows[i].Cells[Column7.Index].Value = Database.myreader[1].ToString();
                        dataGridView1.Rows[i].Cells[DtCommodity.Index].Value = Database.myreader[2].ToString();
                        dataGridView1.Rows[i].Cells[Column8.Index].Value = Database.myreader[3].ToString();
                        dataGridView1.Rows[i].Cells[Column2.Index].Value = Database.myreader[4].ToString();
                        dataGridView1.Rows[i].Cells[DtUnit.Index].Value = Database.myreader[5].ToString();
                        dataGridView1.Rows[i].Cells[Column5.Index].Value = Math.Round( Utility.SafeConvertToDouble( Database.myreader[6].ToString()), FrmMain.ERPQtyDigit) ;
                        dataGridView1.Rows[i].Cells[colItemCode.Index].Value = Database.myreader["ItemCode"].ToString();
                        dataGridView1.Rows[i].Cells[colMainGrp.Index].Value = DespatchCommonFunction.GetDeptt(Database.myreader["ItemCode"].ToString());
                        //
                        i++;
                    }
                    Database.myreader.Close();
                }
            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
            }
        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            try
            {
                bool IsAvail = false;
                if (flagupdate == true)
                {
                    if (IsValidate())
                    {
                        if (Database.OpenConnection(Utility.DespacthConnectionString))
                        {
                            for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                            {
                                Database.myreader = Database.GetExecuteReaderCommand("select * from subsidiarychallanItemMulti WITH (NOLOCK) where subsidiaryBuyer = '"
                                                       + comboSubsidiaryBuyer.Text + "' and year = '" + textYear.Text
                                                       + "' and MainChallanNo = '"
                                                       + dataGridView1.Rows[i].Cells[Column6.Index].Value.ToString() + "' and ProcessorName = '"
                                                       + comboProcessorName.Text + "' and Challansrno != " + SrNo);
                                if (Database.myreader.Read())
                                {
                                    IsAvail = true;
                                    break;
                                }
                                Database.myreader.Close();
                            }
                            Database.Closeconnection();
                        }
                        if (IsAvail)
                            MessageBox.Show("Main Challan No is already in Subsidary Order List");
                        else if (dataGridView1.Rows.Count - 1 == 0)
                            MessageBox.Show("Please mention at least One Item");
                        else if (textNatureofProcessing.Text == "")
                            MessageBox.Show("Please enter value in Nature of Processing");

                        else
                        {

                            DialogResult drs = MessageBox.Show("Do you want to update Subsidary Challan Order "
                                                                , "update Order"
                                              , MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                            if (drs.CompareTo(DialogResult.Yes) == 0)
                            {
                                if (Database.OpenConnection(Utility.DespacthConnectionString))
                                {
                                    Database.BeginTransaction();

                                    Database.GetExecuteNonQueryCommand("delete from subsidiarychallanMulti where srno = "
                                     + SrNo);

                                    Database.GetExecuteNonQueryCommand("delete from subsidiarychallanitemMulti where challansrno = "
                                                                 + SrNo);

                                    Database.GetExecuteNonQueryCommand("insert into subsidiarychallanMulti values('"
                                     + comboProcessorName.Text + "','" + textProcessorAdd.Text
                                     + "','" + comboSubsidiaryBuyer.Text + "','" + textSubsidiaryBuyerAdd.Text
                                     + "','" + textNatureofProcessing.Text + "','" +
                                     textYear.Text + "'," + SrNo + ")");

                                    for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                                    {
                                        Database.GetExecuteNonQueryCommand("insert into subsidiarychallanitemMulti values('"
                                         + textYear.Text + "','" + comboProcessorName.Text
                                         + "','" + comboSubsidiaryBuyer.Text + "','"
                                         + dataGridView1.Rows[i].Cells[Column6.Index].Value.ToString().Trim() + "','"
                                         + Convert.ToDateTime(dataGridView1.Rows[i].Cells[Column7.Index].Value).ToString("MM/dd/yyyy") + "','"
                                         + Convert.ToString(dataGridView1.Rows[i].Cells[DtCommodity.Index].Value) + "','"
                                         + Convert.ToString(dataGridView1.Rows[i].Cells[Column8.Index].Value) + "','"
                                         + Convert.ToString(dataGridView1.Rows[i].Cells[Column2.Index].Value) + "','"
                                         + Convert.ToString(dataGridView1.Rows[i].Cells[DtUnit.Index].Value) + "','"
                                         + Convert.ToString(dataGridView1.Rows[i].Cells[Column5.Index].Value) + "',"
                                            + SrNo + ",'" + Convert.ToString(dataGridView1.Rows[i].Cells[colItemCode.Index].Value) + "',0)");
                                    }
                                    MessageBox.Show("Updated Successfully");
                                    Database.CommitTransaction();
                                    ClearAll();
                                }
                            }
                        }
                        BindGrid();
                    }
                    else
                    {
                        MessageBox.Show("Order quantity can not less then dispatch quantity");
                    }
                }
                else
                {
                    MessageBox.Show("Order quantity can not less then dispatch quantity");
                }
            }
            catch (Exception ex)
            {
                Database.RollBackTransaction();
                ErrorMessageBox.Show(ex);
            }

        }

        private void dataGridView1_CurrentCellChanged(object sender, EventArgs e)
        {
            try
            {
                if (dataGridView1.CurrentCell != null)
                {
                    if (dataGridView1.CurrentCell.ColumnIndex >= DtCommodity.Index) // For Commodity ComboBox
                    {
                        DataGridViewTextBoxCell cell = (DataGridViewTextBoxCell)dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[Column2.Index];
                        DataGridViewTextBoxCell cell2 = (DataGridViewTextBoxCell)dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[Column5.Index];
                        if (btnSave.Enabled == true)
                        {
                            cell2.Value = Convert.ToString(cell.Value);
                        }
                        else
                        {
                            DataTable dt = new DataTable();
                            double count = 0;
                            double total = 0;
                            DataGridViewTextBoxCell chno = (DataGridViewTextBoxCell)dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[Column6.Index];
                            dt = Database.GetDataTable("select * from SubsidiaryChallanIssueItemMulti where MainChallanNo = '" +
                                chno.FormattedValue + "' and Year = '" + textYear.Text + "' and ProcessorName = '" + comboProcessorName.Text +
                                "' and SubsidiaryBuyer='" + comboSubsidiaryBuyer.Text + "' AND subsidiaryyear='" + textYear.Text + "'");
                            //dt.Load(Database.myreader);
                            if (dt.Rows.Count > 0)
                            {
                                for (int i = 0; i < dt.Rows.Count; i++)
                                {
                                    count += Convert.ToDouble(dt.Rows[i]["OrderQty"]);
                                }
                                total = Convert.ToDouble(cell.FormattedValue) - count;
                                cell2.Value = total;
                                if (total < 0)
                                {
                                    flagupdate = false;
                                }
                                else
                                {
                                    cell2.Value = total;
                                    flagupdate = true;
                                }
                            }
                            else
                            {
                                cell2.Value = Convert.ToString(cell.Value);
                            }
                        }
                    }
                    else if (dataGridView1.CurrentRow.Cells.IndexOf(dataGridView1.CurrentCell) == colMainGrp.Index)
                    {
                        string deptt = Convert.ToString(dataGridView1.Rows[dataGridView1.CurrentRow.Index].Cells[colMainGrp.Index].Value);
                        DataTable dt = new DataTable();
                        dt = dtIMaster.Copy();
                        DataGridViewComboBoxCell Col11 = (DataGridViewComboBoxCell)dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[colItemCode.Index];
                        dt.DefaultView.RowFilter = "Deptt='" + deptt + "'";
                        if (dt.DefaultView.ToTable(true, "ItemCode").Rows.Count > 0)
                        {
                            Col11.DataSource = dt.DefaultView.ToTable();
                            Col11.ValueMember = dt.Columns[0].ColumnName.ToString();
                            Col11.DisplayMember = dt.Columns[1].ColumnName.ToString();
                        }
                    }
                    else if (dataGridView1.CurrentRow.Cells.IndexOf(dataGridView1.CurrentCell) == colItemCode.Index)
                    {
                        string itemcode = Convert.ToString(dataGridView1.Rows[dataGridView1.CurrentRow.Index].Cells[colItemCode.Index].Value);
                        string strsql = "SELECT  distinct Split.a.value('.', 'VARCHAR(200)') AS Commodity from " +
                            " (select CAST ('<M>' + REPLACE(REPLACE(i.Commodity,'&','&#38;'), ',', '</M><M>') + '</M>' AS XML) AS Commodity from MaterialProcessing..Item i " +
                            " where i.itemcode='" + itemcode + "' and i.CompanyName='" + comboProcessorName.Text + "' and isnull(i.Commodity,'')<>'') AS A " +
                            " CROSS APPLY Commodity.nodes ('/M') AS Split(a) order by 1 ";

                        DataTable dt = Database.GetDataTable(strsql);
                        DataGridViewComboBoxCell colComm = (DataGridViewComboBoxCell)dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[DtCommodity.Index];
                        colComm.DataSource = dt;
                        colComm.ValueMember = dt.Columns[0].ColumnName.ToString();
                        colComm.DisplayMember = dt.Columns[0].ColumnName.ToString();
                    }
                }

            }
            catch (Exception)
            {

                throw;
            }
        }

        private void btnInitliaze_Click(object sender, EventArgs e)
        {
            try
            {
                ClearAll();
                BindGrid();
                fillData();
            }
            catch (Exception)
            {

                throw;
            }
        }

        private void ClearAll()
        {
            try
            {
                dataGridView1.Rows.Clear();
                comboSubsidiaryBuyer.Text = string.Empty;
                textSubsidiaryBuyerAdd.Text = string.Empty;
                textNatureofProcessing.Text = string.Empty;
                btnUpdate.Enabled = false;
                btnSave.Enabled = true;

            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
            }
        }

        public bool IsValidate()
        {
            try
            {
                DataTable dt = new DataTable();
                double count = 0;
                double total = 0;

                if (comboSubsidiaryBuyer.Text == "")
                {
                    MessageBox.Show("Please select subsidary buyer.");
                    return false;
                }

                for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                {
                    if (Convert.ToString(dataGridView1.Rows[i].Cells[Column6.Index].Value).Trim() == "")
                    {
                        MessageBox.Show("Please enter mainchallan no.");
                        return false;
                    }
                    if (Convert.ToString(dataGridView1.Rows[i].Cells[DtCommodity.Index].Value).Trim() == "")
                    {
                        MessageBox.Show("Please select commodity name.");
                        return false;
                    }
                    if (Convert.ToString(dataGridView1.Rows[i].Cells[DtUnit.Index].Value).Trim() == "")
                    {
                        MessageBox.Show("Please select unit.");
                        return false;
                    }

                    string itemcode = Convert.ToString(dataGridView1.Rows[i].Cells[colItemCode.Index].Value);
                    if (itemcode.Length == 0)
                    {
                        MessageBox.Show("Please Select Item ");
                        return false;
                    }
                }
                if (Database.OpenConnection(Utility.DespacthConnectionString))

                    # region for duplicate Main Challanno and MainChallandate added by Manish on 23rd April 2024
                    for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                    {
                        bool isavail = false;
                        string chno = Convert.ToString(dataGridView1.Rows[i].Cells[Column6.Index].Value).Trim();


                        string st = " select * from subsidiarychallanitemMulti where mainchallanno = '" +
                            chno + "' and mainchallandate ='" + Convert.ToDateTime(dataGridView1.Rows[i].Cells[Column7.Index].Value).ToString("MM/dd/yyyy")
                            + "' and SubsidiaryBuyer ='" + comboSubsidiaryBuyer.Text + "' ProcessorName = '" + comboProcessorName.Text + "'";
                        Database.myreader = Database.GetExecuteReaderCommand("select * from subsidiarychallanitemMulti where mainchallanno = '" +
                            chno + "' and mainchallandate ='" + Convert.ToDateTime(dataGridView1.Rows[i].Cells[Column7.Index].Value).ToString("MM/dd/yyyy")
                            + "' and SubsidiaryBuyer ='" + comboSubsidiaryBuyer.Text + "' and ProcessorName = '" + comboProcessorName.Text + "'");
                        if (Database.myreader.Read())
                        {
                            isavail = true;
                        }
                        Database.myreader.Close();
                        if (isavail)
                            return false;
                    }
                     # endregion

                    for (int j = 0; j < dataGridView1.Rows.Count; j++)
                    {
                        string chno = Convert.ToString(dataGridView1.Rows[j].Cells[Column6.Index].Value).Trim(); 
                        double cell = Convert.ToDouble(dataGridView1.Rows[j].Cells[Column2.Index].Value);
                        if (chno != "")
                        {
                            //Database.myreader = Database.GetExecuteReaderCommand();
                            dt = Database.GetDataTable("select * from SubsidiaryChallanIssueItemMulti where MainChallanNo = '" + chno + "' and Year = '"
                                + textYear.Text + "' and ProcessorName = '" + comboProcessorName.Text + "' and SubsidiaryBuyer='" + comboSubsidiaryBuyer.Text
                                + "' AND subsidiaryyear='" + textYear.Text + "'");

                            //dt.Load(Database.myreader);
                            if (dt.Rows.Count > 0)
                            {
                                for (int i = 0; i < dt.Rows.Count; i++)
                                {
                                    count += Convert.ToDouble(dt.Rows[i]["OrderQty"]);
                                }
                                total = Convert.ToDouble(cell) - count;
                                dataGridView1.Rows[j].Cells[Column5.Index].Value = Convert.ToString(total);
                                if (total < 0)
                                {
                                    return false;
                                }
                            }
                            else
                            {
                                dataGridView1.Rows[j].Cells[Column5.Index].Value = Convert.ToString(cell);
                            }
                            //Database.myreader.Close();
                        }

                    }
                Database.Closeconnection();

                return true;

            }
            catch (Exception)
            {

                throw;
            }
        }

    }
}