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
    public partial class Form2Subsidiary_Challan : Form
    {
        int SrNo = 0;
        string val = "";
        bool flagupdate = true;
        public Form2Subsidiary_Challan()
        {
            InitializeComponent();
            Form_Load();
        }

        private void Form_Load()
        {
            dateMainChallanDate.Format = DateTimePickerFormat.Custom;
            dateMainChallanDate.CustomFormat = DespatchCommonFunction.ERPDateSettings();

            if (Database.OpenConnection(Utility.DespacthConnectionString))
            {
                DespatchCommonFunction.GetCompanyList(comboProcessorName);
                //string strsql = "select Companyname from companymaster order by companyname";
                string strsql = "Select companyname from companymaster where   " +
                          "   isnull(IsFreeze,'no')='no' and (('" + dateMainChallanDate.Value.ToString("yyyy-MM-dd") +
                           "' between fromdate and todate) or (fromdate <= '" +
                           dateMainChallanDate.Value.ToString("yyyy-MM-dd") + "' and todate is null ))  order by CompanyName";

                DespatchCommonFunction.Loadcombo(comboSubsidiaryBuyer, strsql, "companyname", "companyname", Utility.DespacthConnectionString);
                strsql = "select unitname  from unit order by unitname";
                DespatchCommonFunction.Loadcombo(comboQtyUnit, strsql, "unitname", "unitname", Utility.DespacthConnectionString);
                Database.Closeconnection();
            }
            textYear.Text = frmDefaultVale.Year;
            comboProcessorName.Text = frmDefaultVale.CompanyName;
            BindGrid();
            btnUpdate.Enabled = false;

        }

        private void BindGrid()
        {
            try
            {
                if (Database.OpenConnection_Adapter(Utility.DespacthConnectionString))
                {
                    Database.myadapter = Database.GetAdapterCommand("select * from SubOrder with (NoLock) where yr = '" + textYear.Text +
                            "' and processorname = '" + comboProcessorName.Text + "' order by srno desc");
                    DataSet dataset1 = new DataSet();
                    Database.myadapter.Fill(dataset1, "SubOrder");
                    BindingSource datasource = new BindingSource(dataset1.Tables[0], null);
                    dataGridView1.DataSource = datasource;
                    // Add the AutoFilter header cell to each column.
                    foreach (DataGridViewColumn col in dataGridView1.Columns)
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

        private void Form2Subsidiary_Challan_Load(object sender, EventArgs e)
        {
        }

        private void comboProcessorName_SelectedIndexChanged(object sender, EventArgs e)
        {

            if (Database.OpenConnection(Utility.DespacthConnectionString))
            {
                Database.myreader = Database.GetExecuteReaderCommand("select address from factoryinfo where name = '"
                     + comboProcessorName.Text + "'");
                if (Database.myreader.Read())
                    textProcessorAdd.Text = Database.myreader[0].ToString();
                Database.myreader.Close();
                Database.Closeconnection();
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

        private void textQtyDispatched_KeyPress(object sender, KeyPressEventArgs e)
        {
            Utility.AllowFloatOnly(e);
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
                        Database.myreader = Database.GetExecuteReaderCommand("select * from SubOrder where subsidaryName = '"
                             + comboSubsidiaryBuyer.Text + "' and yr = '" + textYear.Text
                             + "' and MainChallan = ' " + textMainChallanNo.Text + "'");
                        while (Database.myreader.Read())
                            IsAvail = true;
                        Database.myreader.Close();
                        Database.Closeconnection();
                    }
                    if (IsAvail)
                        MessageBox.Show("Main Challan No is already in Subsidary Order List");
                    else if (textNatureofProcessing.Text == "")
                        MessageBox.Show("Please enter value in Nature of Processing");

                    else
                    {
                        int VenCnt = DespatchCommonFunction.CustomerLedgerName(comboSubsidiaryBuyer.Text, comboProcessorName.Text);
                        if (VenCnt == 0)
                        {
                            MessageBox.Show("No Account Ledger Found for this customer.\nPlease Ask Account Dept to create Ledger for this customer:- [" + comboSubsidiaryBuyer.Text + "]");
                            return;
                        }
                        for (int i = 0; i < dataGridView1.Rows.Count; i++)
                        {
                            string val1 = dataGridView1.Rows[i].Cells[0].FormattedValue.ToString();

                            int count = 1;
                            for (int j = 0; j < i; j++)
                            {
                                if (dataGridView1.Rows[j].Cells[0].FormattedValue.ToString().Trim() == dataGridView1.Rows[i].Cells[0].FormattedValue.ToString().Trim())
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
                                //Database.myreader = Database.GetExecuteReaderCommand("select srno from SubOrder order by srno");
                                Database.myreader = Database.GetExecuteReaderCommand("select max(srno) from SubOrder");
                                if (Database.myreader.Read())
                                    srno = Convert.ToInt32(Database.myreader[0].ToString());
                                Database.myreader.Close();
                                srno = srno + 1;

                                Database.myadapter = Database.GetAdapterCommand("select * from SubOrder");
                                DataSet dataset1 = new DataSet();
                                Database.myadapter.Fill(dataset1, "SubOrder");

                                //insert data in database 
                                DataRow dr;
                                dr = dataset1.Tables[0].NewRow();
                                dr[0] = srno;
                                dr[1] = textYear.Text;
                                dr[2] = comboProcessorName.Text.ToUpper();
                                dr[3] = textProcessorAdd.Text.ToUpper();
                                dr[4] = comboSubsidiaryBuyer.Text.ToUpper();
                                dr[5] = textSubsidiaryBuyerAdd.Text.ToUpper();
                                dr[6] = textMainChallanNo.Text.ToUpper();
                                dr[7] = dateMainChallanDate.Value.ToString(DespatchCommonFunction.ERPDBDateSettings());
                                dr[8] = textNatureofProcessing.Text.ToUpper();
                                dr[9] = textQtyDispatched.Text;
                                dr[10] = textQtyDispatched.Text;
                                dr[11] = comboQtyUnit.Text.ToUpper();


                                dataset1.Tables[0].Rows.Add(dr);

                                Database.mycmdBuilder = Database.GetCmdBuilderCommand(Database.myadapter);
                                int x = Database.myadapter.Update(dataset1.Tables[0]);
                                if (x > 0)
                                {
                                    Database.CommitTransaction();
                                    MessageBox.Show("Subsidary Order No " + textMainChallanNo.Text + " is generated succesfully");
                                    Utility.UserInformation("Save", "Subsidary Order No " + textMainChallanNo.Text + " is saved ");
                                    BindGrid();
                                    ClearAll();
                                }
                            }
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                Database.RollBackTransaction();
                ErrorMessageBox.Show(ex);
            }

        }

        private void ClearAll()
        {
            //dataGridView1.DataSource = null;
            textMainChallanNo.Text = "";
            comboSubsidiaryBuyer.Text = "";
            textSubsidiaryBuyerAdd.Text = "";
            comboQtyUnit.Text = "";
            textNatureofProcessing.Text = "";
            textQtyDispatched.Text = "";
            btnUpdate.Enabled = false;
            btnSave.Enabled = true;
        }
        private void btnInitliaze_Click(object sender, EventArgs e)
        {
            ClearAll();
        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            try
            {
                if (flagupdate == true)
                {
                    if (IsValidate())
                    {
                        int VenCnt = DespatchCommonFunction.CustomerLedgerName(comboSubsidiaryBuyer.Text, comboProcessorName.Text);
                        if (VenCnt == 0)
                        {
                            MessageBox.Show("No Account Ledger Found for this customer.\nPlease Ask Account Dept to create Ledger for this customer:- [" + comboSubsidiaryBuyer.Text + "]");
                            return;
                        }
                        if (Database.OpenConnection(Utility.DespacthConnectionString))
                        {
                            Database.BeginTransaction();
                            Database.GetExecuteNonQueryCommand("Delete from suborder where srno = " + SrNo);
                            Database.myadapter = Database.GetAdapterCommand("select * from SubOrder");
                            DataSet dataset1 = new DataSet();
                            Database.myadapter.Fill(dataset1, "SubOrder");

                            //insert data in database 
                            DataRow dr;
                            dr = dataset1.Tables[0].NewRow();
                            dr[0] = SrNo;
                            dr[1] = textYear.Text;
                            dr[2] = comboProcessorName.Text.ToUpper();
                            dr[3] = textProcessorAdd.Text.ToUpper();
                            dr[4] = comboSubsidiaryBuyer.Text.ToUpper();
                            dr[5] = textSubsidiaryBuyerAdd.Text.ToUpper();
                            dr[6] = textMainChallanNo.Text.ToUpper();
                            dr[7] = dateMainChallanDate.Value.ToString(DespatchCommonFunction.ERPDBDateSettings());
                            dr[8] = textNatureofProcessing.Text.ToUpper();
                            dr[9] = textQtyDispatched.Text;
                            dr[10] = textBalanceQty.Text;
                            dr[11] = comboQtyUnit.Text.ToUpper();


                            dataset1.Tables[0].Rows.Add(dr);

                            Database.mycmdBuilder = Database.GetCmdBuilderCommand(Database.myadapter);
                            int x = Database.myadapter.Update(dataset1.Tables[0]);
                            if (x > 0)
                            {
                                Database.CommitTransaction();
                                MessageBox.Show("Subsidary Order No " + textMainChallanNo.Text + " is updated succesfully");
                                Utility.UserInformation("Update", "Subsidary Order No " + textMainChallanNo.Text + " is updated");
                                BindGrid();
                                ClearAll();
                            }
                        }
                    }
                    else
                    {

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

        private void dataGridView1_DoubleClick(object sender, EventArgs e)
        {
            try
            {
                SrNo = Convert.ToInt32(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[0].Value);

                textYear.Text = Convert.ToString(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[1].Value);
                comboProcessorName.Text = Convert.ToString(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[2].Value);
                textProcessorAdd.Text = Convert.ToString(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[3].Value);
                comboSubsidiaryBuyer.Text = Convert.ToString(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[4].Value);
                textSubsidiaryBuyerAdd.Text = Convert.ToString(Convert.ToString(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[5].Value));
                textMainChallanNo.Text = Convert.ToString(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[6].Value);
                dateMainChallanDate.Value = Convert.ToDateTime(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[7].Value);
                textNatureofProcessing.Text = Convert.ToString(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[8].Value);
                textQtyDispatched.Text = (Convert.ToString(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[9].Value));
                textBalanceQty.Text = (Convert.ToString(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[10].Value));
                comboQtyUnit.Text = Convert.ToString(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[11].Value);
                btnUpdate.Enabled = true;
                btnSave.Enabled = false;
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
                #region 19-08-2020 lock as per accounting voucher type
                if (!Utility.CheckAccountVoucherDateFreeze(dateMainChallanDate.Value, comboProcessorName.Text, "sales"))
                {
                    MessageBox.Show(AccountCommonVariable.ErrMsg.DATE_FREEZE + "\nPlease check Invoice Date");
                    return false;
                }
                #endregion

                DataTable dt = new DataTable();
                double count = 0;
                double total = 0;

                if (comboSubsidiaryBuyer.Text == "")
                {
                    MessageBox.Show("Please select subsidary buyer.");
                    return false;
                }
                if (textMainChallanNo.Text == "")
                {
                    MessageBox.Show("Please enter mainchallan no.");
                    return false;
                }
                if (comboQtyUnit.Text == "")
                {
                    MessageBox.Show("Please select unit.");
                    return false;
                }

                if (Database.OpenConnection(Utility.DespacthConnectionString))
                {

                    Database.myreader = Database.GetExecuteReaderCommand("select * from SubsidiaryChallan where MainChallanNo = '" + textMainChallanNo.Text + "' and Yr = '" + textYear.Text + "' and ProcessorName = '" + comboProcessorName.Text + "' and SubsidiaryBuyer='" + comboSubsidiaryBuyer.Text + "'");
                    dt.Load(Database.myreader);
                    if (dt.Rows.Count > 0)
                    {
                        for (int i = 0; i < dt.Rows.Count; i++)
                        {
                            count += Convert.ToDouble(dt.Rows[i]["QuantiyDispatched"]);
                        }
                        total = Convert.ToDouble(Utility.ConverttoInt(textQtyDispatched.Text)) - count;
                        textBalanceQty.Text = total.ToString();
                        if (total < 0)
                        {
                            MessageBox.Show("Order quantity can not less then dispatch quantity");
                            return false;
                        }
                        else
                        {
                            textBalanceQty.Text = total.ToString();
                        }
                    }
                    else
                    {
                        textBalanceQty.Text = textQtyDispatched.Text;
                    }
                }

                return true;

            }
            catch (Exception)
            {

                throw;
            }
        }

        private void textQtyDispatched_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (btnSave.Enabled == true)
                {
                    textBalanceQty.Text = textQtyDispatched.Text;
                }
                else
                {

                    if (Database.OpenConnection(Utility.DespacthConnectionString))
                    {
                        DataTable dt = new DataTable();
                        double count = 0;
                        double total = 0;

                        Database.myreader = Database.GetExecuteReaderCommand("select * from SubsidiaryChallan where MainChallanNo = '" + textMainChallanNo.Text + "' and Yr = '" + textYear.Text + "' and ProcessorName = '" + comboProcessorName.Text + "' and SubsidiaryBuyer='" + comboSubsidiaryBuyer.Text + "'");
                        dt.Load(Database.myreader);
                        if (dt.Rows.Count > 0)
                        {
                            for (int i = 0; i < dt.Rows.Count; i++)
                            {
                                count += Convert.ToDouble(dt.Rows[i]["QuantiyDispatched"]);
                            }
                            total = Convert.ToDouble(Utility.ConverttoInt(textQtyDispatched.Text)) - count;
                            textBalanceQty.Text = total.ToString();
                            if (total < 0)
                            {
                                flagupdate = false;
                            }
                            else
                            {
                                textBalanceQty.Text = total.ToString();
                                flagupdate = true;
                            }
                        }
                        else
                        {
                            textBalanceQty.Text = textQtyDispatched.Text;
                        }

                    }
                }


            }
            catch (Exception)
            {

                throw;
            }
        }


    }
}