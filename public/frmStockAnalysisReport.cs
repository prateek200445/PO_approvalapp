using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Globalization;

namespace ERP
{
    public partial class frmStockAnalysisReport : Form
    {
        filter.filterData filterData;
        DataSet dsdsop = new DataSet();
        DataSet dsall = new DataSet();
        public frmStockAnalysisReport()
        {
            InitializeComponent();
            form_Load();
        }

        public void form_Load()
        {
            filterData = new filter.filterData(dataGridView1);
            AccountCommonFunction.BindComboCompanyMulti(comboCompanyName, true);
            comboCompanyName.Text = frmDefaultVale.CompanyName;
        }


        public void FillProductionStock(DateTime dtfrom, DateTime dtto)
        {
            try
            {
                DataTable CompanyList = new DataTable("CompanyList");
                CompanyList.Columns.Add("StringValue");
                for (int j = 0; j < comboCompanyName.CheckedItems.Count; j++)
                {
                    CompanyList.Rows.Add(comboCompanyName.CheckedItems[j].ToString());
                }
                DataTable CompanyName = CompanyList;
                DataSet dsdsop = new DataSet();
                int rept = 0;
                if (radioButton1.Checked)
                    rept = 0;
                if (radioButton2.Checked)
                    rept = 1;
                if (radioButton3.Checked)
                    rept = 2;

                if (Database.OpenConnection(Utility.MaterialConnectionString))
                {
                    if (checkBox1.Checked)
                    {
                        dsdsop = Database.GetDataSet("SP_STOCKANALYSIS_RPT_PROD_CONS ", CommandType.StoredProcedure,
                            new Database.Parameter("@companyname", CompanyName),
                            new Database.Parameter("@DateFrom", dtfrom.ToString("yyyy-MM-dd")),
                            new Database.Parameter("@DateTo", dtto.ToString("yyyy-MM-dd")),
                            new Database.Parameter("@RptType", rept)
                            );
                    }
                    else
                    { 
                        dsdsop = Database.GetDataSet("SP_STOCKANALYSIS_RPT_ALL ", CommandType.StoredProcedure,
                                 new Database.Parameter("@companyname", CompanyName),
                                 new Database.Parameter("@DateFrom", dtfrom.ToString("yyyy-MM-dd")),
                                 new Database.Parameter("@DateTo", dtto.ToString("yyyy-MM-dd")),
                                 new Database.Parameter("@RptType", rept)
                                 );
                    }
                }

                dsall = dsdsop;

                dataGridView1.DataSource = null;

                dataGridView1.DataSource = (checkBox1.Checked == true ? dsdsop.Tables[0] : dsdsop.Tables[1]);
                //if (radioButton1.Checked)
                //    dataGridView1.DataSource = dsdsop.Tables[1];
                //else if (radioButton2.Checked)
                //    dataGridView1.DataSource = dsdsop.Tables[2];
                //else
                //    dataGridView1.DataSource = dsdsop.Tables[3];
                foreach (DataGridViewColumn col in this.dataGridView1.Columns)
                {
                    try
                    {
                        if (col.Name.ToLower().Contains("date"))
                        {
                            dataGridView1.Columns[col.Index].DefaultCellStyle.Format = mmCommonFunction.ERPDateSettings();
                        }
                        else
                        {
                            double result;
                            string v = Convert.ToString(dataGridView1[col.Index, 0].Value);
                            if (double.TryParse(v, out result))
                            {
                                dataGridView1.Columns[col.Index].DefaultCellStyle.Alignment = DataGridViewContentAlignment.BottomRight;
                            }
                        }
                    }
                    catch (Exception ex)
                    {

                    }
                }
                foreach (DataGridViewRow row in this.dataGridView1.Rows)
                {
                    if (Convert.ToString(row.Cells[0].Value).ToString().Contains("Total"))
                    {
                        row.DefaultCellStyle.BackColor = Color.RoyalBlue;
                        row.DefaultCellStyle.ForeColor = Color.White;
                    }
                    else if (Convert.ToString(row.Cells[1].Value).ToString().Contains("Total"))
                    {
                        row.DefaultCellStyle.BackColor = Color.RoyalBlue;
                        row.DefaultCellStyle.ForeColor = Color.White;
                    }
                }

                foreach (DataGridViewRow row in this.dataGridView2.Rows)
                {
                    if (Convert.ToString(row.Cells[0].Value).ToString().Contains("Total"))
                    {
                        row.DefaultCellStyle.BackColor = Color.RoyalBlue;
                        row.DefaultCellStyle.ForeColor = Color.White;
                    }
                    else if (Convert.ToString(row.Cells[1].Value).ToString().Contains("Total"))
                    {
                        row.DefaultCellStyle.BackColor = Color.RoyalBlue;
                        row.DefaultCellStyle.ForeColor = Color.White;
                    }
                }

                if (radioButton1.Checked)
                {
                    dataGridView1.Columns[2].Frozen = true;
                }
                if (radioButton2.Checked)
                {
                    dataGridView1.Columns[3].Frozen = true;
                }
                if (radioButton3.Checked)
                {
                    dataGridView1.Columns[1].Frozen = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            FillProductionStock(dtFrom.Value, dateTimeDateTo.Value);
        }

        private void dataGridView1_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (dataGridView1.CurrentCell == null) return;

                if (e.KeyCode == Keys.Enter)
                {
                    try
                    {
                        //TabClass.OpenForm(new FrmWareHouseTransferNew(Convert.ToDateTime("2010-01-01"), dt.Date, "", comboItemName.Text));
                        string grpname = Convert.ToString(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[2].Value);
                        string itemname = "";
                        string comp = Convert.ToString(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[0].Value);
                        FrmMisStokAnlyDtl.IsClick = true;
                        FrmMisStokAnlyDtl.Date1 = dtFrom.Value;
                        FrmMisStokAnlyDtl.Date2 = dateTimeDateTo.Value;
                        if (radioButton2.Checked)
                            itemname = Convert.ToString(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[3].Value);

                        if (grpname.Contains("Fabric") && dataGridView1.Columns[dataGridView1.CurrentCell.ColumnIndex].Name.Contains("Cl."))
                            TabClass.OpenForm(new FrmWareHouseTransferNew(Convert.ToDateTime("2010-01-01"), dateTimeDateTo.Value, "", grpname));
                        else
                            TabClass.OpenForm(new FrmMisStokAnlyDtl(dsall, grpname, itemname));
                    }
                    catch (Exception ex)
                    {
                        ErrorMessageBox.Show(ex);
                    }
                }
                if (e.KeyCode == Keys.D)
                {
                    try
                    {
                        if (dataGridView1.Rows.Count > 1)
                        {
                            string Category = Convert.ToString(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[0].Value);

                            string subgroupname = Convert.ToString(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[3].Value);

                            DataTable dt = new DataTable();

                            if (Category == "Sales")
                            {
                                if (comboCompanyName.Text.Contains("*"))
                                {
                                    Database.myreader = Database.GetExecuteReaderCommand("select * from vw_Sales_EBIDTA with(nolock) where"
                                     + " invdate between '" + dtFrom.Value.ToString("yyyy-MM-dd")
                                     + "' and '" + dateTimeDateTo.Value.ToString("yyyy-MM-dd") + "' and subgroupname = '" + subgroupname + "' order by invdate desc");
                                    dt.Load(Database.myreader);
                                }
                                else
                                {
                                    string st  = "select * from vw_Sales_EBIDTA with(nolock) where"
                                     + " companyname in('" + comboCompanyName.Text + "') and invdate between '" + dtFrom.Value.ToString("yyyy-MM-dd")
                                     + "' and '" + dateTimeDateTo.Value.ToString("yyyy-MM-dd") + "' and subgroupname = '" + subgroupname + "' order by invdate desc";
                                    
                                    Database.myreader = Database.GetExecuteReaderCommand(st);
                                    dt.Load(Database.myreader);
                                }
                            }
                            else
                            {
                                if (comboCompanyName.Text.Contains("*"))
                                {
                                    Database.myreader = Database.GetExecuteReaderCommand("select * from vw_purchase_EBIDTA with(nolock) where"
                                    + " invdate between '" + dtFrom.Value.ToString("yyyy-MM-dd")
                                    + "' and '" + dateTimeDateTo.Value.ToString("yyyy-MM-dd") + "' and subgroupname = '" + subgroupname + "' order by invdate desc");
                                    dt.Load(Database.myreader);
                                }
                                else
                                {
                                    Database.myreader = Database.GetExecuteReaderCommand("select * from vw_purchase_EBIDTA with(nolock) where"
                                     + " companyname in('" + comboCompanyName.Text + "') and invdate between '" + dtFrom.Value.ToString("yyyy-MM-dd")
                                     + "' and '" + dateTimeDateTo.Value.ToString("yyyy-MM-dd") + "' and subgroupname = '" + subgroupname + "' order by invdate desc");
                                    dt.Load(Database.myreader);
                                }
                            }

                            dataGridView1.DataSource = dt;

                        }
                    }
                    catch (Exception ex)
                    {
                    }
                }


                if (e.KeyCode == Keys.Escape)
                {
                    groupBox1.Visible = false;
                }
                if (e.KeyCode == Keys.E)
                {
                    groupBox1.Visible = true;
                    try
                    {
                        if (dataGridView1.Rows.Count > 1)
                        {
                            string companyname = Convert.ToString(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[0].Value);
                            string subgroupname = Convert.ToString(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[2].Value);

                            DataSet dsdsop = new DataSet();
                            dsdsop = Database.GetDataSet("SP_Expense_EBIDTA_Subgroupname ", CommandType.StoredProcedure,
                                       new Database.Parameter("@companyname", companyname),
                                       new Database.Parameter("@subgroupname", subgroupname),
                                       new Database.Parameter("@DateFrom", dtFrom.Value.ToString("yyyy-MM-dd")),
                                       new Database.Parameter("@DateTo", dateTimeDateTo.Value.ToString("yyyy-MM-dd"))
                                       );

                            dataGridView2.DataSource = null;
                            dataGridView2.DataSource = dsdsop.Tables[0];

                            dataGridView2.Columns[0].Width = 200;
                            dataGridView2.Columns[1].Width = 100;
                            dataGridView2.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
                            foreach (DataGridViewColumn col in this.dataGridView2.Columns)
                            {
                                try
                                {

                                    double result;
                                    string v = Convert.ToString(dataGridView2[col.Index, 0].Value);
                                    if (double.TryParse(v, out result))
                                        dataGridView2.Columns[col.Index].DefaultCellStyle.Alignment = DataGridViewContentAlignment.BottomRight;
                                }
                                catch (Exception ex)
                                {

                                }
                            }
                            for (int i = 0; i < dataGridView2.Rows.Count - 1; i++)
                            {
                                if (dataGridView2.Rows[i].Cells[2].Value.ToString() == "")
                                {
                                    dataGridView2.Rows[i].DefaultCellStyle.Font = new Font(dataGridView2.Font, FontStyle.Bold);

                                    dataGridView2.Rows[i].DefaultCellStyle.BackColor = System.Drawing.Color.GreenYellow;

                                }
                            }
                        }
                            
                    }
                    catch (Exception ex)
                    {
                    }
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show("Problem with dataGridView1_KeyDown :" + ex.Message);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                Database.OpenConnection(Utility.MaterialConnectionString);

                DataTable CompanyList = new DataTable("CompanyList");
                CompanyList.Columns.Add("StringValue");
                for (int j = 0; j < comboCompanyName.CheckedItems.Count; j++)
                {
                    CompanyList.Rows.Add(comboCompanyName.CheckedItems[j].ToString());
                }
                DataTable CompanyName = CompanyList;
                DataSet dsdsop = new DataSet();

                dsdsop = Database.GetDataSet("SP_Sales_EBIDTA", CommandType.StoredProcedure,
                           new Database.Parameter("@companyname", CompanyName),
                           new Database.Parameter("@DateFrom", dtFrom.Value.ToString("yyyy-MM-dd")),
                           new Database.Parameter("@DateTo", dateTimeDateTo.Value.ToString("yyyy-MM-dd"))
                           );

                dataGridView1.DataSource = null;
                dataGridView1.DataSource = dsdsop.Tables[0];

                foreach (DataGridViewColumn col in this.dataGridView1.Columns)
                {
                    try
                    {
                        if (col.Name.ToLower().Contains("date"))
                        {
                            dataGridView1.Columns[col.Index].DefaultCellStyle.Format = mmCommonFunction.ERPDateSettings();
                        }
                        else
                        {
                            double result;
                            string v = Convert.ToString(dataGridView1[col.Index, 0].Value);
                            if (double.TryParse(v, out result))
                            {
                                dataGridView1.Columns[col.Index].DefaultCellStyle.Alignment = DataGridViewContentAlignment.BottomRight;
                            }
                            
                        }
                    }
                          

                    catch (Exception ex)
                    {

                    }
                }

                for (int i= 0;i < dataGridView1.Rows.Count-1; i++)
                {
                    if (dataGridView1.Rows[i].Cells[2].Value.ToString() == "")
                    {
                        dataGridView1.Rows[i].DefaultCellStyle.Font = new Font(dataGridView1.Font, FontStyle.Bold);

                        dataGridView1.Rows[i].DefaultCellStyle.BackColor = System.Drawing.Color.GreenYellow;

                    }
                }
               

                
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void button3_Click(object sender, EventArgs e)
        {
            try
            {
                Database.OpenConnection(Utility.MaterialConnectionString);

                DataTable CompanyList = new DataTable("CompanyList");
                CompanyList.Columns.Add("StringValue");
                for (int j = 0; j < comboCompanyName.CheckedItems.Count; j++)
                {
                    CompanyList.Rows.Add(comboCompanyName.CheckedItems[j].ToString());
                }
                DataTable CompanyName = CompanyList;
                DataSet dsdsop = new DataSet();

                dsdsop = Database.GetDataSet("SP_Purchase_EBIDTA ", CommandType.StoredProcedure,
                           new Database.Parameter("@companyname", CompanyName),
                           new Database.Parameter("@DateFrom", dtFrom.Value.ToString("yyyy-MM-dd")),
                           new Database.Parameter("@DateTo", dateTimeDateTo.Value.ToString("yyyy-MM-dd"))
                           );

                dataGridView1.DataSource = null;
                dataGridView1.DataSource = dsdsop.Tables[0];

                foreach (DataGridViewColumn col in this.dataGridView1.Columns)
                {
                    try
                    {
                        if (col.Name.ToLower().Contains("date"))
                        {
                            dataGridView1.Columns[col.Index].DefaultCellStyle.Format = mmCommonFunction.ERPDateSettings();
                        }
                        else
                        {
                            double result;
                            string v = Convert.ToString(dataGridView1[col.Index, 0].Value);
                            if (double.TryParse(v, out result))
                            {
                                dataGridView1.Columns[col.Index].DefaultCellStyle.Alignment = DataGridViewContentAlignment.BottomRight;
                            }
                        }
                    }
                    catch (Exception ex)
                    {

                    }
                }
                for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                {
                    if (dataGridView1.Rows[i].Cells[2].Value.ToString() == "")
                    {
                        dataGridView1.Rows[i].DefaultCellStyle.Font = new Font(dataGridView1.Font, FontStyle.Bold);

                        dataGridView1.Rows[i].DefaultCellStyle.BackColor = System.Drawing.Color.GreenYellow;

                    }
                }
               
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                if (checkBox2.Checked)
                {
                    for (int i = 0; i < comboCompanyName.Items.Count; i++)
                    {
                        comboCompanyName.SetItemChecked(i, true);
                    }
                }
                else
                {
                    for (int i = 0; i < comboCompanyName.Items.Count; i++)
                    {
                        comboCompanyName.SetItemChecked(i,false);
                    }

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void frmStockAnalysisReport_Load(object sender, EventArgs e)
        {

        }

        private void button4_Click(object sender, EventArgs e)
        {
            try
            {
                Database.OpenConnection(Utility.MaterialConnectionString);

                DataTable CompanyList = new DataTable("CompanyList");
                CompanyList.Columns.Add("StringValue");
                for (int j = 0; j < comboCompanyName.CheckedItems.Count; j++)
                {
                    CompanyList.Rows.Add(comboCompanyName.CheckedItems[j].ToString());
                }
                DataTable CompanyName = CompanyList;
                DataSet dsdsop = new DataSet();

                dsdsop = Database.GetDataSet("SP_Expense_EBIDTA ", CommandType.StoredProcedure,
                           new Database.Parameter("@companyname", CompanyName),
                           new Database.Parameter("@DateFrom", dtFrom.Value.ToString("yyyy-MM-dd")),
                           new Database.Parameter("@DateTo", dateTimeDateTo.Value.ToString("yyyy-MM-dd"))
                           );

                dataGridView1.DataSource = null;
                dataGridView1.DataSource = dsdsop.Tables[0];

                foreach (DataGridViewColumn col in this.dataGridView1.Columns)
                {
                    try
                    {
                        if (col.Name.ToLower().Contains("date"))
                        {
                            dataGridView1.Columns[col.Index].DefaultCellStyle.Format = mmCommonFunction.ERPDateSettings();
                        }
                        else
                        {
                            double result;
                            string v = Convert.ToString(dataGridView1[col.Index, 0].Value);
                            if (double.TryParse(v, out result))
                            {
                                dataGridView1.Columns[col.Index].DefaultCellStyle.Alignment = DataGridViewContentAlignment.BottomRight;
                            }
                        }
                    }
                    catch (Exception ex)
                    {

                    }
                }
                for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                {
                    if (dataGridView1.Rows[i].Cells[2].Value.ToString() == "")
                    {
                        dataGridView1.Rows[i].DefaultCellStyle.Font = new Font(dataGridView1.Font, FontStyle.Bold);

                        dataGridView1.Rows[i].DefaultCellStyle.BackColor = System.Drawing.Color.GreenYellow;

                    }
                }

                dataGridView1.Columns[0].Width = 250;
                dataGridView1.Columns[2].Width = 300;


            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            groupBox1.Visible = false;
        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void button6_Click(object sender, EventArgs e)
        {
            TabClass.OpenForm(new frmprovisionebidta());
        }

        private void button7_Click(object sender, EventArgs e)
        {
            try
            {
                if (dataGridView1.Rows.Count > 1)
                {
                    if (Database.OpenConnection(Utility.MaterialConnectionString))
                    {

                        Database.GetExecuteNonQueryCommand("delete from DiffStock");
                        for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                        {
                            double stock = Convert.ToDouble(dataGridView1.Rows[i].Cells[25].Value)
                                 + Convert.ToDouble(dataGridView1.Rows[i].Cells[27].Value);

                            Database.GetExecuteNonQueryCommand("insert into DiffStock values('" +
                                dataGridView1.Rows[i].Cells[0].FormattedValue.ToString()
                                + "','" + dataGridView1.Rows[i].Cells[3].FormattedValue.ToString() + "'," + stock + ")");
                        }
                        groupBox2.Visible = true;
                        dataGridView3.Visible = true;
                        DataSet dsdsop = new DataSet();

                        dsdsop = Database.GetDataSet("select * from vw_DifferenceStock where companyname ='" + comboCompanyName.Text + "' and Diffstock !=0");

                        dataGridView3.DataSource = null;
                        dataGridView3.DataSource = dsdsop.Tables[0];

                        dataGridView3.RowsDefaultCellStyle.BackColor = System.Drawing.Color.LightBlue;
                        dataGridView3.AlternatingRowsDefaultCellStyle.BackColor = System.Drawing.Color.White;
                
                      //  dataGridView3.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
                
                        foreach (DataGridViewColumn col in this.dataGridView3.Columns)
                        {
                            try
                            {
                                double result;
                                string v = Convert.ToString(dataGridView3[col.Index, 0].Value);
                                if (double.TryParse(v, out result))
                                {
                                    dataGridView3.Columns[col.Index].DefaultCellStyle.Alignment = DataGridViewContentAlignment.BottomRight;
                                }

                            }
                            catch (Exception ex)
                            {

                            }
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void button8_Click(object sender, EventArgs e)
        {
            groupBox2.Visible = false;

        }

        private void button9_Click(object sender, EventArgs e)
        {
            try
            {
                Database.OpenConnection(Utility.MaterialConnectionString);

                DataTable CompanyList = new DataTable("CompanyList");
                CompanyList.Columns.Add("StringValue");
                for (int j = 0; j < comboCompanyName.CheckedItems.Count; j++)
                {
                    CompanyList.Rows.Add(comboCompanyName.CheckedItems[j].ToString());
                }
                DataTable CompanyName = CompanyList;
                DataSet dsdsop = new DataSet();

                dsdsop = Database.GetDataSet("SP_Expense_EBIDTA_NoCategory ", CommandType.StoredProcedure,
                           new Database.Parameter("@companyname", CompanyName),
                           new Database.Parameter("@DateFrom", dtFrom.Value.ToString("yyyy-MM-dd")),
                           new Database.Parameter("@DateTo", dateTimeDateTo.Value.ToString("yyyy-MM-dd"))
                           );

                dataGridView1.DataSource = null;
                dataGridView1.DataSource = dsdsop.Tables[0];

                foreach (DataGridViewColumn col in this.dataGridView1.Columns)
                {
                    try
                    {
                        if (col.Name.ToLower().Contains("date"))
                        {
                            dataGridView1.Columns[col.Index].DefaultCellStyle.Format = mmCommonFunction.ERPDateSettings();
                        }
                        else
                        {
                            double result;
                            string v = Convert.ToString(dataGridView1[col.Index, 0].Value);
                            if (double.TryParse(v, out result))
                            {
                                dataGridView1.Columns[col.Index].DefaultCellStyle.Alignment = DataGridViewContentAlignment.BottomRight;
                            }
                        }
                    }
                    catch (Exception ex)
                    {

                    }
                }
                for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                {
                    if (dataGridView1.Rows[i].Cells[2].Value.ToString() == "")
                    {
                        dataGridView1.Rows[i].DefaultCellStyle.Font = new Font(dataGridView1.Font, FontStyle.Bold);

                        dataGridView1.Rows[i].DefaultCellStyle.BackColor = System.Drawing.Color.GreenYellow;

                    }
                }

                dataGridView1.Columns[0].Width = 250;
                dataGridView1.Columns[2].Width = 300;


            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void button10_Click(object sender, EventArgs e)
        {
            if (FrmMain.UserName == "anil" || FrmMain.UserName == "erp" || FrmMain.UserName == "mani")
                TabClass.OpenForm(new frmStockJV());
            else
                MessageBox.Show("You don't have right to access Stock JV");
        }
    }
}
