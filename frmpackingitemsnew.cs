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
    public partial class frmpackingitemsnew : Form
    {
        string _Invno = "";
        string _despackinglist = "";
        public frmpackingitemsnew()
        {
            InitializeComponent();
            Form_Load();
        }
        public frmpackingitemsnew(string InvNo, double netwt, string despackinglist)
        {
            InitializeComponent();
            _Invno = InvNo;
            _despackinglist = despackinglist;
            Form_Load();
            textNetWt.Text = netwt.ToString();
        }
        public frmpackingitemsnew(string InvNo)
        {
            InitializeComponent();
            _Invno = InvNo;
            Form_Load();
        }
        private void Form_Load()
        {
            fillData();
            if (Database.OpenConnection(Utility.DespacthConnectionString))
            {
                DespatchCommonFunction.GetCompanyList(comboCompanyName);
                Database.myreader.Close();
            }
            comboCompanyName.Text = frmDefaultVale.CompanyName;
        }

        void fillData()
        {

            //if (Database.OpenConnection(Utility.MaterialConnectionString))
            {
                //OleDbDataAdapter adapter1 = new OleDbDataAdapter("select BillD.ProductName,BillD.ProductID,BillD.Saletype,BillD.InQty,BillD.SBtype,BillD.Dis1,BillD.Type,BillD.InPrice from BillDetails BillD", database);
                Database.myadapter = Database.GetAdapterCommand("Select unitname from MaterialProcessing.dbo.Unit order by unitname");
                DataTable dt = new DataTable();
                Database.myadapter.Fill(dt);
                qtyunits.DataSource = dt;
                qtyunits.ValueMember = dt.Columns[0].ColumnName.ToString();
                qtyunits.DisplayMember = dt.Columns[0].ColumnName.ToString();
                qtyunits2.DataSource = dt;
                qtyunits2.ValueMember = dt.Columns[0].ColumnName.ToString();
                qtyunits2.DisplayMember = dt.Columns[0].ColumnName.ToString();
                qtyinuints3.DataSource = dt;
                qtyinuints3.ValueMember = dt.Columns[0].ColumnName.ToString();
                qtyinuints3.DisplayMember = dt.Columns[0].ColumnName.ToString();
                sizeunit.DataSource = dt;
                sizeunit.ValueMember = dt.Columns[0].ColumnName.ToString();
                sizeunit.DisplayMember = dt.Columns[0].ColumnName.ToString();
            }
        }
        private void btn_save_Click(object sender, EventArgs e)
        {
            try
            {

                double TotalNetWt = 0;
                for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                {

                    if (dataGridView1.Rows[i].Cells[qtyvalue2.Index].Value.ToString() != "" && dataGridView1.Rows[i].Cells[qtyvalue.Index].Value.ToString() != "" &&
                           dataGridView1.Rows[i].Cells[qtyunits2.Index].Value.ToString() != "")
                    {
                        TotalNetWt += Convert.ToDouble(dataGridView1.Rows[i].Cells[colnetwt.Index].Value.ToString());
                    }
                }

                TotalNetWt = Math.Round(TotalNetWt, 3);

                if (TotalNetWt != Convert.ToDouble(textNetWt.Text) && Convert.ToDouble(textNetWt.Text) != 0)
                    MessageBox.Show("Net wt defined in Packing List Should be matched with summation of Packing Items");
                else
                {
                    int x = 0;
                    double Total = 0;
                    if (Database.OpenConnection(Utility.DespacthConnectionString))
                    {
                        for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                        {

                            if (dataGridView1.Rows[i].Cells[qtyvalue2.Index].Value.ToString() != "" && dataGridView1.Rows[i].Cells[qtyvalue.Index].Value.ToString() != "" &&
                                   dataGridView1.Rows[i].Cells[qtyunits2.Index].Value.ToString() != "")
                            {
                                // int InvoiceSeries = 0;
                                string invSubNo = comboitemcode.Text + "/0";

                                ArrayList arrpur = new ArrayList();
                                int MaxValue = 0;
                                Database.myreader = Database.GetExecuteReaderCommand("Select srno from subPackingitem where InvsubNo ='"
                                                        + comboitemcode.Text + "'");
                                while (Database.myreader.Read())
                                    arrpur.Add(Database.myreader[0].ToString());
                                Database.myreader.Close();
                                ArrayList arr = new ArrayList();
                                for (int j = 0; j < arrpur.Count; j++)
                                    arr.Add(arrpur[j].ToString().Substring(arrpur[j].ToString().LastIndexOf('/') + 1));

                                for (int k = 0; k < arr.Count; k++)
                                {
                                    if (MaxValue < Convert.ToInt32(arr[k]))
                                        MaxValue = Convert.ToInt32(arr[k]);
                                }
                                if (MaxValue == 0)
                                    MaxValue = 1;
                                else
                                    MaxValue = MaxValue + 1;

                                invSubNo = comboitemcode.Text + "/" + MaxValue.ToString();
                                Total += Convert.ToDouble(dataGridView1.Rows[i].Cells[totqty.Index].Value.ToString());
                                Database.myadapter = Database.GetAdapterCommand("select * from subpackingitem");
                                DataSet dataset1 = new DataSet();
                                Database.myadapter.Fill(dataset1, "subpackingitem");
                                DataRow dr;
                                dr = dataset1.Tables[0].NewRow();

                                dr[0] = invSubNo;

                                if (dataGridView1.Rows[i].Cells[contno.Index].FormattedValue.ToString() != "")
                                    dr[1] = dataGridView1.Rows[i].Cells[contno.Index].Value.ToString();

                                if (dataGridView1.Rows[i].Cells[qtyvalue.Index].FormattedValue.ToString() != "")
                                    dr[2] = dataGridView1.Rows[i].Cells[qtyvalue.Index].Value.ToString();

                                if (dataGridView1.Rows[i].Cells[qtyunits.Index].FormattedValue.ToString() != "")
                                    dr[3] = dataGridView1.Rows[i].Cells[qtyunits.Index].Value.ToString();

                                if (dataGridView1.Rows[i].Cells[qtyvalue2.Index].FormattedValue.ToString() != "")
                                    dr[4] = dataGridView1.Rows[i].Cells[qtyvalue2.Index].Value.ToString();

                                if (dataGridView1.Rows[i].Cells[qtyunits2.Index].FormattedValue.ToString() != "")
                                    dr[5] = dataGridView1.Rows[i].Cells[qtyunits2.Index].Value.ToString();

                                if (dataGridView1.Rows[i].Cells[porno.Index].FormattedValue.ToString() != "")
                                    dr[6] = dataGridView1.Rows[i].Cells[porno.Index].Value.ToString();

                                if (dataGridView1.Rows[i].Cells[sizevalue.Index].FormattedValue.ToString() != "")
                                    dr[7] = dataGridView1.Rows[i].Cells[sizevalue.Index].Value.ToString();

                                if (dataGridView1.Rows[i].Cells[sizeunit.Index].FormattedValue.ToString() != "")
                                    dr[8] = dataGridView1.Rows[i].Cells[sizeunit.Index].Value.ToString();

                                if (dataGridView1.Rows[i].Cells[totqty.Index].FormattedValue.ToString() != "")
                                    dr[9] = dataGridView1.Rows[i].Cells[totqty.Index].Value.ToString();

                                if (dataGridView1.Rows[i].Cells[qtyinuints3.Index].FormattedValue.ToString() != "")
                                    dr[10] = dataGridView1.Rows[i].Cells[qtyinuints3.Index].Value.ToString();

                                if (dataGridView1.Rows[i].Cells[colnetwt.Index].FormattedValue.ToString() != "")
                                    dr[11] = dataGridView1.Rows[i].Cells[colnetwt.Index].Value.ToString();

                                dr[12] = comboitemcode.Text;
                                if (dataGridView1.Rows[i].Cells[rate.Index].FormattedValue.ToString() != "")
                                    dr[13] = dataGridView1.Rows[i].Cells[rate.Index].Value.ToString();

                                if (dataGridView1.Rows[i].Cells[amount.Index].FormattedValue.ToString() != "")
                                    dr[14] = dataGridView1.Rows[i].Cells[amount.Index].Value.ToString();

                                if (dataGridView1.Rows[i].Cells[ColVarSep.Index].FormattedValue.ToString() != "")
                                    dr[15] = Convert.ToString(dataGridView1.Rows[i].Cells[ColVarSep.Index].Value);

                                dataset1.Tables[0].Rows.Add(dr);
                                Database.mycmdBuilder = Database.GetCmdBuilderCommand(Database.myadapter);
                                x = Database.myadapter.Update(dataset1.Tables[0]);
                            }

                        }
                        double avgRate = 0;
                        double TotalAmt = 0;
                        double netwt = 0;
                        Database.myreader = Database.GetExecuteReaderCommand("SELECT  sum(Amount)/sum(qunt_unit) Rate, sum(amount) amount ,sum(net_wt_kgs) Netwt FROM subpackingitem where invSubNo='" +
                            comboitemcode.Text + "'");
                        if (Database.myreader.Read())
                        {
                            avgRate = Utility.SafeConvertToDouble(Database.myreader[0].ToString());
                            TotalAmt = Utility.SafeConvertToDouble(Database.myreader[1].ToString());
                            netwt = Utility.SafeConvertToDouble(Database.myreader[2].ToString());
                        }
                        Database.myreader.Close();


                        Database.GetExecuteNonQueryCommand("update packingitems set totalqty = " + Total
                         + ",rate=" + avgRate + ",Amount=" + TotalAmt + " ,net_wt=" + netwt + " where invsubno = '" + comboitemcode.Text + "'");
                        Database.Closeconnection();
                        if (x > 0)
                        {
                            MessageBox.Show("Packing details add successfully");
                            dataGridView1.Rows.Clear();
                            btn_save.Enabled = false;
                            btnUpdate.Enabled = true;
                        }
                    }
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
                double TotalNetWt = 0;
                for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                {

                    if (dataGridView1.Rows[i].Cells[qtyvalue2.Index].Value.ToString() != "" && dataGridView1.Rows[i].Cells[qtyvalue.Index].Value.ToString() != "" &&
                           dataGridView1.Rows[i].Cells[qtyunits2.Index].Value.ToString() != "")
                    {
                        TotalNetWt += Convert.ToDouble(dataGridView1.Rows[i].Cells[colnetwt.Index].Value.ToString());
                    }
                }

                TotalNetWt = Math.Round(TotalNetWt, 3);
                if (TotalNetWt != Convert.ToDouble(textNetWt.Text))
                {
                    DialogResult drs = MessageBox.Show("Net wt defined in Packing List Should be matched with summation of Packing Items ? Do you Still wants to Continue with Updating ?", "Update PackingList"
                                    , MessageBoxButtons.YesNo, MessageBoxIcon.Information);

                    if (drs.CompareTo(DialogResult.No) == 0)
                    {
                        return;
                    }
                }


                //if (TotalNetWt != Convert.ToDouble(textNetWt.Text))
                //    MessageBox.Show("Net wt defined in Packing List Should be matched with summation of Packing Items");
                //else
                {
                    int x = 0;
                    double Total = 0;
                    if (Database.OpenConnection(Utility.DespacthConnectionString))
                    {
                        Database.GetExecuteNonQueryCommand("delete from  subPackingitem  where InvsubNo = '" + comboitemcode.Text + "'");
                        for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                        {

                            if (dataGridView1.Rows[i].Cells[qtyvalue2.Index].Value.ToString() != "" && dataGridView1.Rows[i].Cells[qtyvalue.Index].Value.ToString() != "" &&
                                   dataGridView1.Rows[i].Cells[qtyunits2.Index].Value.ToString() != "")
                            {
                                //int InvoiceSeries = 0;
                                string invSubNo = comboitemcode.Text + "/0";

                                ArrayList arrpur = new ArrayList();
                                int MaxValue = 0;
                                Database.myreader = Database.GetExecuteReaderCommand("Select srno from subPackingitem where InvsubNo ='"
                                                        + comboitemcode.Text + "'");
                                while (Database.myreader.Read())
                                    arrpur.Add(Database.myreader[0].ToString());
                                Database.myreader.Close();
                                ArrayList arr = new ArrayList();
                                for (int j = 0; j < arrpur.Count; j++)
                                    arr.Add(arrpur[j].ToString().Substring(arrpur[j].ToString().LastIndexOf('/') + 1));

                                for (int k = 0; k < arr.Count; k++)
                                {
                                    if (MaxValue < Convert.ToInt32(arr[k]))
                                        MaxValue = Convert.ToInt32(arr[k]);
                                }
                                if (MaxValue == 0)
                                    MaxValue = 1;
                                else
                                    MaxValue = MaxValue + 1;




                                //Database.myreader = Database.GetExecuteReaderCommand("select srno from subPackingitem where InvsubNo = '"
                                // + comboitemcode.Text + "' order by srno");
                                //while (Database.myreader.Read())
                                //    invSubNo = Database.myreader[0].ToString();
                                //Database.myreader.Close();

                                //if (invSubNo != "")
                                //{
                                //    string[] invTempSubNo = invSubNo.Split('/');
                                //    InvoiceSeries = Convert.ToInt32(invTempSubNo[5]);
                                //}
                                //InvoiceSeries = InvoiceSeries + 1;
                                invSubNo = comboitemcode.Text + "/" + MaxValue.ToString();

                                Total += Convert.ToDouble(dataGridView1.Rows[i].Cells[totqty.Index].Value.ToString());

                                Database.myadapter = Database.GetAdapterCommand("select * from subpackingitem");
                                DataSet dataset1 = new DataSet();
                                Database.myadapter.Fill(dataset1, "subpackingitem");
                                DataRow dr;
                                dr = dataset1.Tables[0].NewRow();


                                dr[0] = invSubNo;
                                if (dataGridView1.Rows[i].Cells[contno.Index].FormattedValue.ToString() != "")
                                    dr[1] = dataGridView1.Rows[i].Cells[contno.Index].Value.ToString();

                                if (dataGridView1.Rows[i].Cells[qtyvalue.Index].FormattedValue.ToString() != "")
                                    dr[2] = dataGridView1.Rows[i].Cells[qtyvalue.Index].Value.ToString();

                                if (dataGridView1.Rows[i].Cells[qtyunits.Index].FormattedValue.ToString() != "")
                                    dr[3] = dataGridView1.Rows[i].Cells[qtyunits.Index].Value.ToString();

                                if (dataGridView1.Rows[i].Cells[qtyvalue2.Index].FormattedValue.ToString() != "")
                                    dr[4] = dataGridView1.Rows[i].Cells[qtyvalue2.Index].Value.ToString();

                                if (dataGridView1.Rows[i].Cells[qtyunits2.Index].FormattedValue.ToString() != "")
                                    dr[5] = dataGridView1.Rows[i].Cells[qtyunits2.Index].Value.ToString();

                                if (dataGridView1.Rows[i].Cells[porno.Index].FormattedValue.ToString() != "")
                                    dr[6] = dataGridView1.Rows[i].Cells[porno.Index].Value.ToString();

                                if (dataGridView1.Rows[i].Cells[sizevalue.Index].FormattedValue.ToString() != "")
                                    dr[7] = dataGridView1.Rows[i].Cells[sizevalue.Index].Value.ToString();

                                if (dataGridView1.Rows[i].Cells[sizeunit.Index].FormattedValue.ToString() != "")
                                    dr[8] = dataGridView1.Rows[i].Cells[sizeunit.Index].Value.ToString();

                                if (dataGridView1.Rows[i].Cells[totqty.Index].FormattedValue.ToString() != "")
                                    dr[9] = dataGridView1.Rows[i].Cells[totqty.Index].Value.ToString();

                                if (dataGridView1.Rows[i].Cells[qtyinuints3.Index].FormattedValue.ToString() != "")
                                    dr[10] = dataGridView1.Rows[i].Cells[qtyinuints3.Index].Value.ToString();

                                if (dataGridView1.Rows[i].Cells[colnetwt.Index].FormattedValue.ToString() != "")
                                    dr[11] = dataGridView1.Rows[i].Cells[colnetwt.Index].Value.ToString();

                                dr[12] = comboitemcode.Text;
                                if (dataGridView1.Rows[i].Cells[rate.Index].FormattedValue.ToString() != "")
                                    dr[13] = dataGridView1.Rows[i].Cells[rate.Index].Value.ToString();

                                if (dataGridView1.Rows[i].Cells[amount.Index].FormattedValue.ToString() != "")
                                    dr[14] = dataGridView1.Rows[i].Cells[amount.Index].Value.ToString();

                                if (dataGridView1.Rows[i].Cells[ColVarSep.Index].FormattedValue.ToString() != "")
                                    dr[15] = Convert.ToString(dataGridView1.Rows[i].Cells[ColVarSep.Index].Value);


                                dataset1.Tables[0].Rows.Add(dr);
                                Database.mycmdBuilder = Database.GetCmdBuilderCommand(Database.myadapter);
                                x = Database.myadapter.Update(dataset1.Tables[0]);
                            }

                        }
                        double avgRate = 0;
                        double TotalAmt = 0;
                        double netwt = 0;
                        Database.myreader = Database.GetExecuteReaderCommand("SELECT  sum(Amount)/sum(qunt_unit) as Rate, sum(amount) amount ,sum(net_wt_kgs) Netwt FROM subpackingitem where invSubNo='" +
                            comboitemcode.Text + "'");
                        if (Database.myreader.Read())
                        {
                            avgRate = Utility.SafeConvertToDouble(Database.myreader[0].ToString());
                            TotalAmt = Utility.SafeConvertToDouble(Database.myreader[1].ToString());
                            netwt = Utility.SafeConvertToDouble(Database.myreader[2].ToString());
                        }
                        Database.myreader.Close();


                        Database.GetExecuteNonQueryCommand("update packingitems set totalqty = " + Total
                         + ",rate=" + avgRate + ",Amount=" + TotalAmt + " ,net_wt=" + netwt + " where invsubno = '" + comboitemcode.Text + "'");


                        Database.Closeconnection();
                        if (x > 0)
                            MessageBox.Show("Packing details updated successfully");
                    }
                }
            }

            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
            }
        }

        private void frmpackingitemsnew_Load(object sender, EventArgs e)
        {
        }

        private void comboinvoiceno_SelectedIndexChanged(object sender, EventArgs e)
        {
            comboitemcode.Items.Clear();
            comboitemcode.Text = "";


            if (Database.OpenConnection(Utility.DespacthConnectionString))
            {
                string strsql = "";
                strsql = "select InvSubNo from packingitems where invno = '" + comboinvoiceno.Text + " 'order by InvSubNo";
                DespatchCommonFunction.Loadcombo(comboitemcode, strsql, "InvSubNo", "InvSubNo", Utility.DespacthConnectionString);
                Database.Closeconnection();
            }
            if (comboitemcode.Items.Count > 0)
                comboitemcode.SelectedIndex = 0;
        }

        private void comboitemcode_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (Database.OpenConnection(Utility.DespacthConnectionString))
                {
                    textpackinggoods.Text = "";
                    textNetWt.Text = "";

                    Database.myreader = Database.GetExecuteReaderCommand("select kind_of_packages,Net_Wt from packingitems where invsubno = '" +
                                     comboitemcode.Text + " ' order by InvSubNo");
                    while (Database.myreader.Read())
                    {
                        textpackinggoods.Text = Database.myreader[0].ToString();
                        textNetWt.Text = Database.myreader[1].ToString();
                    }
                    Database.myreader.Close();

                    int i = 0;
                    dataGridView1.Rows.Clear();

                    object obj = Database.GetExecuteScalarCommand_return("select count(*) from subpackingitem where invsubno = '" + comboitemcode.Text + "'");
                    int count = Convert.ToInt32(obj);
                    if (count > 0)
                        dataGridView1.Rows.Add(count);

                    Database.myreader = Database.GetExecuteReaderCommand("select * from subpackingitem where invsubno = '" +
                                  comboitemcode.Text + "'");
                    while (Database.myreader.Read())
                    {
                        dataGridView1.Rows[i].Cells[contno.Index].Value = Database.myreader[1].ToString();
                        dataGridView1.Rows[i].Cells[qtyvalue.Index].Value = Database.myreader[2].ToString();
                        dataGridView1.Rows[i].Cells[qtyunits.Index].Value = Database.myreader[3].ToString();
                        dataGridView1.Rows[i].Cells[qtyvalue2.Index].Value = Database.myreader[4].ToString();
                        dataGridView1.Rows[i].Cells[qtyunits2.Index].Value = Database.myreader[5].ToString();
                        dataGridView1.Rows[i].Cells[porno.Index].Value = Database.myreader[6].ToString();
                        dataGridView1.Rows[i].Cells[sizevalue.Index].Value = Database.myreader[7].ToString();
                        dataGridView1.Rows[i].Cells[sizeunit.Index].Value = Database.myreader[8].ToString();
                        dataGridView1.Rows[i].Cells[totqty.Index].Value = Database.myreader[9].ToString();
                        dataGridView1.Rows[i].Cells[qtyinuints3.Index].Value = Database.myreader[10].ToString();
                        dataGridView1.Rows[i].Cells[colnetwt.Index].Value = Database.myreader[11].ToString();
                        dataGridView1.Rows[i].Cells[rate.Index].Value = Database.myreader[13].ToString();
                        dataGridView1.Rows[i].Cells[amount.Index].Value = Database.myreader[14].ToString();
                        dataGridView1.Rows[i].Cells[ColVarSep.Index].Value = Database.myreader[15].ToString();

                        btn_save.Enabled = false;
                        btnUpdate.Enabled = true;
                        i++;

                    }
                    Database.myreader.Close();
                    if (i == 0)
                    {
                        btn_save.Enabled = true;
                        btnUpdate.Enabled = false;
                    }

                    Database.Closeconnection();
                }
            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
            }
        }

        private void dataGridView1_CurrentCellChanged(object sender, EventArgs e)
        {
            try
            {
                if (dataGridView1.CurrentCell.ColumnIndex == rate.Index || dataGridView1.CurrentCell.ColumnIndex == totqty.Index)
                {
                    if (dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[qtyvalue.Index].FormattedValue.ToString().Trim() != ""
                         && dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[qtyvalue2.Index].FormattedValue.ToString().Trim() != "")
                    {
                        dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[amount.Index].Value =
                            Convert.ToDouble(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[totqty.Index].Value.ToString()) *
                            Convert.ToDouble(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[rate.Index].Value.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                ex.ToString();
            }

        }

        private void dataGridView1_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            if (e.Control is TextBox)
            {
                TextBox tb = e.Control as TextBox;
                tb.KeyPress -= new KeyPressEventHandler(tb_KeyPress);
                if (this.dataGridView1.CurrentCell.ColumnIndex == qtyvalue.Index
                    || this.dataGridView1.CurrentCell.ColumnIndex == qtyvalue2.Index
                    || this.dataGridView1.CurrentCell.ColumnIndex == totqty.Index)
                    tb.KeyPress += new KeyPressEventHandler(tb_KeyPress);
            }
        }

        void tb_KeyPress(object sender, KeyPressEventArgs e)
        {

            if ((!(Char.IsDigit(e.KeyChar))) && (!(e.KeyChar == '\b')) && (!(e.KeyChar == '.')))
                e.Handled = true;
        }

        private void comboCompanyName_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (Database.OpenConnection(Utility.DespacthConnectionString))
            {
                string strsql = "";
                if (_Invno.Length > 0)
                {
                    strsql = "select Invoice_No from packinglist where companyname = '" +
                        comboCompanyName.Text + "' and Invoice_No='" + _Invno + "' order by Invoice_dt desc,invno desc";
                }
                else
                {
                    strsql = "select Invoice_No from packinglist where companyname = '" +
                        comboCompanyName.Text + "' order by Invoice_dt desc,invno desc";
                }
                DespatchCommonFunction.Loadcombo(comboinvoiceno, strsql, "Invoice_No", "Invoice_No", Utility.DespacthConnectionString);
                Database.Closeconnection();
            }

            if (comboinvoiceno.Items.Count > 0)
                comboinvoiceno.SelectedIndex = 0;
        }

        private void btnItemDetails_Click(object sender, EventArgs e)
        {
            fillDataLoad();
        }

        void fillDataLoad()
        {

            try
            {
                //OleDbDataAdapter adapter1 = new OleDbDataAdapter("select BillD.ProductName,BillD.ProductID,BillD.Saletype,BillD.InQty,BillD.SBtype,BillD.Dis1,BillD.Type,BillD.InPrice from BillDetails BillD", database);
                Database.myadapter = Database.GetAdapterCommand("Select unitname from MaterialProcessing.dbo.Unit order by unitname");
                DataTable dt = new DataTable();
                Database.myadapter.Fill(dt);
                qtyunits.DataSource = dt;
                qtyunits.ValueMember = dt.Columns[0].ColumnName.ToString();
                qtyunits.DisplayMember = dt.Columns[0].ColumnName.ToString();
                qtyunits2.DataSource = dt;
                qtyunits2.ValueMember = dt.Columns[0].ColumnName.ToString();
                qtyunits2.DisplayMember = dt.Columns[0].ColumnName.ToString();
                qtyinuints3.DataSource = dt;
                qtyinuints3.ValueMember = dt.Columns[0].ColumnName.ToString();
                qtyinuints3.DisplayMember = dt.Columns[0].ColumnName.ToString();
                sizeunit.DataSource = dt;
                sizeunit.ValueMember = dt.Columns[0].ColumnName.ToString();
                sizeunit.DisplayMember = dt.Columns[0].ColumnName.ToString();
                if (_despackinglist.Length > 0)
                {
                    dataGridView1.Rows.Clear();
                    using (System.Data.SqlClient.SqlConnection con = new System.Data.SqlClient.SqlConnection(Utility.MaterialConnectionString))
                    {
                        con.Open();
                        string strsql = "SELECT  PONO ,Size ,COUNT(*) bailCount,SUM([No. of PCS.]) PCS ,SUM (A.Weight) Netwt FROM vw_Prod_DespatchPackingList A WHERE A.PACKINGLISTNO in ('" 
                            + _despackinglist.Replace(",", "','") + "') GROUP BY PONO ,Size ";
                        System.Data.SqlClient.SqlCommand command = new System.Data.SqlClient.SqlCommand(strsql, con);
                        System.Data.SqlClient.SqlDataReader reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            string scontno = "01 To " + Convert.ToString(reader["bailCount"]);
                            string sqtyvalue = Convert.ToString(reader["bailCount"]);
                            string sqtyunits = "Set";
                            string sColVarSep = "X";
                            string sqtyvalue2 = Convert.ToString(Utility.SafeConvertToDouble( reader["PCS"]) / Utility.SafeConvertToDouble(reader["bailCount"]));
                            string sqtyunits2 = "Pcs";
                            string sporno = Convert.ToString(reader["PONO"]);
                            string ssizevalue = Convert.ToString(reader["Size"]);
                            string ssizeunit = "Pcs";
                            string stotqty = Convert.ToString(reader["PCS"]);
                            string sqtyinuints3 = "Pcs";
                            string scolnetwt = Convert.ToString(reader["Netwt"]);
                            string sRate = "0";
                            string samount = "0";
                            dataGridView1.Rows.Add(
                                scontno, sqtyvalue, sqtyunits, sColVarSep, sqtyvalue2, sqtyunits2, sporno,
                                ssizevalue, ssizeunit, stotqty, sqtyinuints3, scolnetwt, sRate, samount);


                        }
                        reader.Close();
                    }
                }
            }
            catch (Exception ex)
            {

            }

        }
    }
}