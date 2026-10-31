using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Data.SqlClient;
using System.Collections;
using System.Globalization;
using System.Drawing.Imaging;
using System.Reflection;
using iTextSharp.text.pdf;
using iTextSharp.text;
using iTextSharp.text.html;
using iTextSharp.text.xml;
using System.Data.OleDb;

using System.IO;

namespace ERP
{
    public partial class frmLedgerSummary_Voucher : ERPTabPage
    {
        #region Variables

        public static DataSet dataset1 = new DataSet();
        public static bool isexecute = false;
        private filter.filterData filterData;
        double totalAmntForPrint = 0;

        CultureInfo us = new CultureInfo("en-US");
        CultureInfo IN = new CultureInfo("gu-IN");

        //following arraylist are fom deleteing row from datagridview

        List<string> col1 = new List<string>();
        List<string> col2 = new List<string>();
        List<string> col3 = new List<string>();
        List<string> col4 = new List<string>();
        List<string> col5 = new List<string>();
        List<string> col6 = new List<string>();
        List<string> col7 = new List<string>();
        List<string> col8 = new List<string>();
        List<string> col9 = new List<string>();
        List<string> col10 = new List<string>();
        List<int> index = new List<int>();


        //following code is for open all the details of datagridview row
        private bool isExpanded = false;
        private int showDetails = 0;
        string[] Ledger = new string[0];
        bool isDelete = false;

        AccountVoucherDetails avd;
        DeleteRestoreRows drr;

        public static string ldgrname = "";
        public static DateTime dtfrom = Utility.nullDate;
        public static DateTime dtTo = Utility.nullDate;
        public static string Company = "";

        DataTable dtledger = new DataTable();
        #endregion

        private string SelectedLedgerName = "";

        #region Main Functions
        public frmLedgerSummary_Voucher()
        {
            InitializeComponent();
            new ComboFilter(comboLedgerName);
            Form_Load();
        }

        private void Form_Load()
        {
            #region Help Message
            this.HelpTitle = "Accounts Ledger";
            this.HelpMessage = @"A ledger is a record of accounts. 
If we want to see any partiular ledger we use accounts ledger.
If any particular company is having bill or not this information we get from this ledger.
If that company not having a bill we can create it from:
Go to Accounts < Creation 

These accounts are recorded separately showing their beginning/ending balance.

The ledger also sums up the total of every account which is transferred into the balance sheet and income statement.
There are 3 different kinds of ledgers that deal with book-keeping. Ledgers include:

Sales ledger, which deals mostly with the accounts receivable account. 
This ledger consists of the financial transactions made by customers to the business.
Purchase ledger is a ledger that goes hand and hand with the Accounts Payable account.
This is the purchasing transaction a company does.
General ledger representing the original 5 main accounts: assets, liabilities, equity, income, and expenses.";
            #endregion;

            #region Init
            new dateTimePickerRange(dateTimePicker1, dateTimePicker2);
            avd = new AccountVoucherDetails(dataGridView1, comboCompanyName, comboLedgerName, dateTimePicker1, dateTimePicker2, 2, 3, 4, 0, 1, 5, 6, -1, 1, 2);

            dataGridView1.ContextMenuStrip = AccountCommonFunction.AccountContextMenu.GetCommonVoucherContext(avd);
            //dataGridView1.ContextMenuStrip = AccountCommonFunction.ColumnsHide.GetColumns(dataGridView1.ContextMenuStrip, dataGridView1);
            drr = new DeleteRestoreRows(dataGridView1);

            drr.BeforeDelete += (dgr, e) =>
            {
                if (dgr.Cells[1].Style.Padding.Left > 0)
                    e.Cancel = true;
                else if (dgr.Index < dgr.DataGridView.RowCount - 1 && dgr.DataGridView.Rows[dgr.Index + 1].Cells[1].Style.Padding.Left > 0)
                {
                    avd.ExpandDetails(dgr.Index);
                }
            };

            drr.onDelete += (dgr) =>
            {
                double dr = 0;
                double cr = 0;
                double balance = 0;
                double oBalance = 0;
                for (int i = 0; i < dataGridView1.Rows.Count; i++)
                {
                    if (dataGridView1.Rows[i].Cells[0].FormattedValue.ToString().Length == 0) continue;
                    double cDR = Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[5].Value);
                    double cCR = Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[6].Value);
                    balance = cDR - cCR + balance;

                    if (balance < 0)
                    {
                        dataGridView1.Rows[i].Cells[7].Value = -balance;
                        dataGridView1.Rows[i].Cells[8].Value = "Cr";
                    }
                    else
                    {
                        dataGridView1.Rows[i].Cells[7].Value = balance;
                        dataGridView1.Rows[i].Cells[8].Value = "Dr";
                    }

                    if (i == 0 && Convert.ToString(dataGridView1.Rows[0].Cells[1].FormattedValue).Equals("Opening Balance"))
                    {
                        oBalance = cDR - cCR;
                        cDR = 0;
                        cCR = 0;
                    }

                    dr += cDR;
                    cr += cCR;
                }
                textDebit.Text = dr.ToString("#,##0.00");
                textCredit.Text = cr.ToString("#,##0.00");
                textOpeningBalance.Text = oBalance.ToString("#,##0.00");

                textClosingBalance.Text = balance.ToString("#,##0.00");
            };
            drr.onRestore += (dgr) =>
            {
                double dr = 0;
                double cr = 0;
                double balance = 0;
                double oBalance = 0;
                for (int i = 0; i < dataGridView1.Rows.Count; i++)
                {
                    if (dataGridView1.Rows[i].Cells[0].FormattedValue.ToString().Length == 0) continue;
                    double cDR = Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[5].Value);
                    double cCR = Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[6].Value);
                    balance = cDR - cCR + balance;

                    if (balance < 0)
                    {
                        dataGridView1.Rows[i].Cells[7].Value = -balance;
                        dataGridView1.Rows[i].Cells[8].Value = "Cr";
                    }
                    else
                    {
                        dataGridView1.Rows[i].Cells[7].Value = balance;
                        dataGridView1.Rows[i].Cells[8].Value = "Dr";
                    }

                    if (i == 0 && Convert.ToString(dataGridView1.Rows[0].Cells[1].FormattedValue).Equals("Opening Balance"))
                    {
                        oBalance = cDR - cCR;
                        cDR = 0;
                        cCR = 0;
                    }

                    dr += cDR;
                    cr += cCR;
                }
                textDebit.Text = dr.ToString("#,##0.00");
                textCredit.Text = cr.ToString("#,##0.00");
                textOpeningBalance.Text = oBalance.ToString("#,##0.00");

                textClosingBalance.Text = balance.ToString("#,##0.00");
            };


            dataGridView1.Columns["ColumnDebit"].DefaultCellStyle = FormatStyle.Number2Digit();
            dataGridView1.Columns["ColumnCredit"].DefaultCellStyle = FormatStyle.Number2Digit();
            dataGridView1.Columns["ColumnClosingBal"].DefaultCellStyle = FormatStyle.Number2Digit();
            dataGridView1.Columns["ColumnDate"].DefaultCellStyle.Format = Tools.ToolsCommonVariable.Setting.DateFormat;
            dataGridView1.Columns[ColInterest.Index].DefaultCellStyle = FormatStyle.Number2Digit();



            #endregion


            dateTimePicker1.Value = AccountCommonFunction.FinancialYearStart();

            //comboCompanyName.DataSource = AccountCommonFunction.GetCompanyList();
            //comboCompanyName.DisplayMember = AccountCommonVariable.FieldName.CompanyName;
            //comboCompanyName.ValueMember = AccountCommonVariable.FieldName.CompanyId;

            AccountCommonFunction.BindComboCompany(comboCompanyName);

            comboCompanyName.Text = frmDefaultVale.CompanyName;



            if (isexecute)
            {
                comboCompanyName.Text = Company;
                if (Database.OpenConnection(Utility.MaterialConnectionString))
                {
                    SelectedLedgerName = ldgrname;

                    int ledgerid = Convert.ToInt32(Database.GetExecuteScalarCommand_return("Select SRNO from ledgerMaster with(Nolock) where companyname='" +
                        Company + "' and LedgerName='" + ldgrname + "'"));
                    DataRow[] row = dtledger.Select("Ledgerid=" + ledgerid);
                    if (row != null)
                        ldgrname = Convert.ToString(row[0].ItemArray[0]);

                }
                //SelectedLedgerName = ldgrname;
                comboLedgerName.Text = ldgrname;
                dateTimePicker1.Value = dtfrom;
                dateTimePicker2.Value = dtTo;
                OK();
                isexecute = false;

            }
        }

        #endregion

        #region Functions

        private DataSet GetAllDetatils(string Ledgername)
        {

            DataSet dataset1 = new DataSet();
            try
            {
                DateTime dt1 = dateTimePicker1.Value;
                DateTime dt2 = dateTimePicker2.Value;
                DateTime DateFrom = new DateTime(dt1.Year, dt1.Month, dt1.Day);
                DateTime DateTo = new DateTime(dt2.Year, dt2.Month, dt2.Day);
                DateTime CurrentDateTime = dateTimePicker1.Value;

                DateTime dtdate = Utility.nullDate;
                if (CurrentDateTime.Day == 1)
                {
                    if (CurrentDateTime.Month == 1)
                    {
                        int dayinmonth = System.DateTime.DaysInMonth(CurrentDateTime.Year - 1, 12);
                        dtdate = new DateTime(CurrentDateTime.Year - 1, 12, dayinmonth);
                    }
                    else
                    {
                        int dayinmonth = System.DateTime.DaysInMonth(CurrentDateTime.Year, CurrentDateTime.Month - 1);
                        dtdate = new DateTime(CurrentDateTime.Year, CurrentDateTime.Month - 1, dayinmonth);
                    }
                }
                else
                {
                    int day = Convert.ToInt32(CurrentDateTime.Day - 1);
                    dtdate = new DateTime(CurrentDateTime.Year, CurrentDateTime.Month, day);
                }

                DateTime Startingdate = new DateTime(2012, 4, 1);

                bool isPNLAccount = false;

                var isPNL = Database.GetExecuteScalarCommand_return(string.Format(@"select top 1 IsPnL from
(
select top 1 * from GroupHeadTree((select top 1 Under from LedgerMaster  with(nolock)  where LedgerName='{0}' and CompanyName='{1}'), '{1}') order by id desc
) as t
left join CashVoucherExpenseHead c  with(nolock)  on c.ExpenseHead=t.GroupName and c.CompanyName='{1}'
", SelectedLedgerName, comboCompanyName.Text));

                if (isPNL != null && !(isPNL is DBNull))
                {
                    isPNLAccount = "yes" == ((string)isPNL).ToLower();
                }

                if (isPNLAccount)
                {
                    Startingdate = new DateTime(DateFrom.Year, 4, 1);
                    if (DateFrom.Month >= 0 && DateFrom.Month < 4) Startingdate = Startingdate.AddYears(-1);
                }

                DataSet dataset2 = new DataSet();

                string CompanyName = comboCompanyName.Text;

                //string LedgerName = SelectedLedgerName;

                dataset1 = Database.GetDataSet("sp_LedgerSummary_Voucher", CommandType.StoredProcedure,
                   new Database.Parameter("@DateFrom", DateFrom.ToString("yyyy-MM-dd")),			// 0 all, 1 group company, 2 company
                   new Database.Parameter("@DateTo", DateTo.ToString("yyyy-MM-dd")),            // Group Company Name
                   new Database.Parameter("@companyname", CompanyName),				// Company Id
                   new Database.Parameter("@LedgerName", Ledgername),
                   new Database.Parameter("@Date1", Startingdate.ToString("yyyy-MM-dd")),
                   new Database.Parameter("@Date2", dtdate.ToString("yyyy-MM-dd")),
                   new Database.Parameter("@IsNarration", 0),
                   new Database.Parameter("@IsAgstRef", (checkBox1.Checked == true ? 1 : 0)),
                   new Database.Parameter("@InterestCal", Utility.SafeConvertToDouble(textROI.Text))
                   );

                if (checkBox1.Checked)
                    ColAgstRef.Visible = true;
                else
                    ColAgstRef.Visible = false;

                if (checkBox2.Checked)
                    ColReco.Visible = true;
                else
                    ColReco.Visible = false;

                //Database.myreader = Database.GetExecuteReaderCommand(" sp_LedgerSummary_Voucher'" + DateFrom.ToString("yyyy-MM-dd") + "','" + DateTo.ToString("yyyy-MM-dd") + "','" 
                //    + CompanyName + "','" + LedgerName + "','" + Startingdate.ToString("yyyy-MM-dd") + "','" + dtdate.ToString("yyyy-MM-dd") + "'");
                //DataTable dt = new DataTable();
                //dt.Load(Database.myreader);
                //.Fill(dataset1);
                //dataset1.Tables.Add(dataTable);
                return dataset1;
            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
                return dataset1;
            }

        }

        private void ViewSummary()
        {
            if (Convert.ToDouble(dataGridView1[7, dataGridView1.Rows.Count - 2].Value) > 0)
                textClosingBalance.Text = dataGridView1[7, dataGridView1.Rows.Count - 2].Value.ToString();
            else
                textClosingBalance.Text = dataGridView1[7, dataGridView1.Rows.Count - 2].Value.ToString();

            if (Convert.ToDouble(dataGridView1[5, 0].Value) > 0)
                textOpeningBalance.Text = dataGridView1[5, 0].Value.ToString();
            else
                textOpeningBalance.Text = "-" + dataGridView1[6, 0].Value.ToString();

            double totalDRsum = 0;
            double totalCrsum = 0;

            foreach (DataGridViewRow row in this.dataGridView1.Rows)
            {
                try
                {

                    if (row.Index > 0)
                    {
                        if (dataGridView1[1, row.Index].FormattedValue.ToString().Trim() != "")
                        {
                            totalDRsum += Convert.ToDouble(dataGridView1[5, row.Index].Value);
                            totalCrsum += Convert.ToDouble(dataGridView1[6, row.Index].Value);
                        }
                    }


                }
                catch (Exception ex)
                {
                    ex.ToString();
                    break;
                }

                textDebit.Text = totalDRsum.ToString("#,##0.00", IN);
                textCredit.Text = totalCrsum.ToString("#,##0.00", IN);
            }
        }

        private void OK()
        {
            //filterData.clearFilter();
            dataGridView1.Rows.Clear();
            DateTime CurrentDateTime = dateTimePicker1.Value;

            // Never Used this two varible
            //double precredit = 0;
            //double predebit = 0;

            printLedgerToolStripMenuItem.Enabled = true;
            buttonMultiPrint.Enabled = true;
            col1.Clear();
            col2.Clear();
            col3.Clear();
            col4.Clear();
            col5.Clear();
            col6.Clear();
            col7.Clear();
            col8.Clear();
            col9.Clear();
            col10.Clear();
            index.Clear();
            RunScript();
            FinishScript();
        }

        private void switchFilter()
        {
            if (showDetails > 0)
            {
                for (int i = 0; i < dataGridView1.Columns.Count; i++)
                {
                    dataGridView1.Columns[i].SortMode = DataGridViewColumnSortMode.NotSortable;
                }
            }
            else
            {
                for (int i = 0; i < dataGridView1.Columns.Count; i++)
                {
                    dataGridView1.Columns[i].SortMode = DataGridViewColumnSortMode.Automatic;
                }
            }

        }

        private void RunScript()
        {
            Database.OpenConnection(Utility.MaterialConnectionString);
            int ledgerid = 0;
            bool blnLedgerFound = true;

            string LedgerName = "";
            if (comboLedgerName.InvokeRequired)
            {
                comboLedgerName.Invoke(new MethodInvoker(delegate { LedgerName = SelectedLedgerName; }));
            }
            else
            {
                if (SelectedLedgerName.Length == 0)
                {
                    if (Database.OpenConnection(Utility.MaterialConnectionString))
                    {
                        SelectedLedgerName = Convert.ToString(Database.GetExecuteScalarCommand_return("Select top 1 LedgerName from ledgerMaster with(nolock) where LedgerName='"
                            + comboLedgerName.Text + "' and CompanyName='" + comboCompanyName.Text + "'"));
                    }
                    if (SelectedLedgerName.Length == 0)
                        SelectedLedgerName = comboLedgerName.Text;
                    ledgerid = Convert.ToInt32(Database.GetExecuteScalarCommand_return("Select SRNO from ledgerMaster  with(nolock)  where companyname='" +
                      comboCompanyName.Text + "' and LedgerName='" + SelectedLedgerName + "'"));

                    if (dtledger.Rows.Count > 0)
                    {
                        DataRow[] row = dtledger.Select("Ledgerid=" + ledgerid);
                        if (row != null)
                            comboLedgerName.Text = Convert.ToString(row[0].ItemArray[0]);
                    }
                }
                LedgerName = SelectedLedgerName;
                if (!comboLedgerName.Items.Contains(LedgerName))
                {
                    blnLedgerFound = false;
                }

            }


            if (Convert.ToDouble(textROI.Text) == 0)
            {
                ColInterest.Visible = false;
            }
            else
            {
                ColInterest.Visible = true;
            }
            if (Database.OpenConnection(Utility.MaterialConnectionString))
            {
                if (blnLedgerFound)
                {
                    LedgerName = Convert.ToString(Database.GetExecuteScalarCommand_return("Select LedgerName from ledgerMaster  with(nolock)  where srno="
                        + Convert.ToInt32(comboLedgerName.SelectedValue)));
                }
            }

            dataset1 = GetAllDetatils(LedgerName);

            /// Never used this variable
            //bool isForx = false;
            if (dataset1.Tables.Count > 0)
            {
                int rCount = dataset1.Tables[0].Rows.Count;

                for (int x = 0; x < rCount; x++)
                {
                    if (dataset1.Tables[0].Rows[x].ItemArray[1].ToString().Trim() == "Unadujsted Forex Gain/Loss")
                    {
                        if ((dataset1.Tables[0].Rows[x].ItemArray[5].ToString().Trim() != "" && dataset1.Tables[0].Rows[x].ItemArray[5].ToString().Trim() != "0") ||
                            (dataset1.Tables[0].Rows[x].ItemArray[6].ToString().Trim() != "" && dataset1.Tables[0].Rows[x].ItemArray[6].ToString().Trim() != "0"))
                        {
                            if (dataGridView1.InvokeRequired)
                            {
                                dataGridView1.Invoke(new MethodInvoker(delegate { dataGridView1.Rows.Add(); }));
                            }
                            else
                            {
                                dataGridView1.Rows.Add();
                            }
                            for (int y = 0; y < dataset1.Tables[0].Columns.Count; y++)
                            {
                                if (y == 8) dataGridView1.Rows[x].Cells[9].Value = dataset1.Tables[0].Rows[x].ItemArray[8];
                                else
                                    dataGridView1.Rows[x].Cells[y].Value = dataset1.Tables[0].Rows[x].ItemArray[y];
                            }
                        }
                    }
                    else
                    {
                        if (dataGridView1.InvokeRequired)
                        {
                            dataGridView1.Invoke(new MethodInvoker(delegate { dataGridView1.Rows.Add(); }));
                            dataGridView1.Rows[x].Cells[ColumnDate.Index].Value = dataset1.Tables[0].Rows[x].ItemArray[0];
                            dataGridView1.Rows[x].Cells[Particulars.Index].Value = dataset1.Tables[0].Rows[x].ItemArray[1];
                            dataGridView1.Rows[x].Cells[ColumnVoucherType.Index].Value = dataset1.Tables[0].Rows[x].ItemArray[2];
                            dataGridView1.Rows[x].Cells[ColumnVoucherNo.Index].Value = dataset1.Tables[0].Rows[x].ItemArray[3];
                            dataGridView1.Rows[x].Cells[VoucherRef.Index].Value = dataset1.Tables[0].Rows[x].ItemArray[4];
                            dataGridView1.Rows[x].Cells[ColumnDebit.Index].Value = dataset1.Tables[0].Rows[x].ItemArray[5];
                            dataGridView1.Rows[x].Cells[ColumnCredit.Index].Value = dataset1.Tables[0].Rows[x].ItemArray[6];
                            dataGridView1.Rows[x].Cells[ColumnClosingBal.Index].Value = dataset1.Tables[0].Rows[x].ItemArray[7];
                            dataGridView1.Rows[x].Cells[clType.Index].Value = "";
                            dataGridView1.Rows[x].Cells[Days.Index].Value = dataset1.Tables[0].Rows[x].ItemArray[10];
                            dataGridView1.Rows[x].Cells[ColAgstRef.Index].Value = dataset1.Tables[0].Rows[x].ItemArray[9];
                            dataGridView1.Rows[x].Cells[ColInterest.Index].Value = dataset1.Tables[0].Rows[x].ItemArray[11];
                            //dataGridView1.Rows[x].Cells[colRefT.Index].Value = dataset1.Tables[0].Rows[x].ItemArray[13];
                            //dataGridView1.Rows[x].Cells[colYr.Index].Value = dataset1.Tables[0].Rows[x].ItemArray[15];
                            //dataGridView1.Rows[x].Cells[colAtt.Index].Value = dataset1.Tables[0].Rows[x].ItemArray[18];

                        }
                        else
                        {
                            dataGridView1.Rows.Add();
                            dataGridView1.Rows[x].Cells[ColumnDate.Index].Value = dataset1.Tables[0].Rows[x].ItemArray[0];
                            dataGridView1.Rows[x].Cells[Particulars.Index].Value = dataset1.Tables[0].Rows[x].ItemArray[1];
                            dataGridView1.Rows[x].Cells[ColumnVoucherType.Index].Value = dataset1.Tables[0].Rows[x].ItemArray[2];
                            dataGridView1.Rows[x].Cells[ColumnVoucherNo.Index].Value = dataset1.Tables[0].Rows[x].ItemArray[3];
                            dataGridView1.Rows[x].Cells[VoucherRef.Index].Value = dataset1.Tables[0].Rows[x].ItemArray[4];
                            dataGridView1.Rows[x].Cells[ColumnDebit.Index].Value = dataset1.Tables[0].Rows[x].ItemArray[5];
                            dataGridView1.Rows[x].Cells[ColumnCredit.Index].Value = dataset1.Tables[0].Rows[x].ItemArray[6];
                            dataGridView1.Rows[x].Cells[ColumnClosingBal.Index].Value = dataset1.Tables[0].Rows[x].ItemArray[7];
                            dataGridView1.Rows[x].Cells[clType.Index].Value = "";
                            dataGridView1.Rows[x].Cells[Days.Index].Value = dataset1.Tables[0].Rows[x].ItemArray[10];
                            dataGridView1.Rows[x].Cells[ColAgstRef.Index].Value = dataset1.Tables[0].Rows[x].ItemArray[9];
                            dataGridView1.Rows[x].Cells[ColInterest.Index].Value = dataset1.Tables[0].Rows[x].ItemArray[11];

                            //dataGridView1.Rows[x].Cells[colRefT.Index].Value = dataset1.Tables[0].Rows[x].ItemArray[13];
                            //dataGridView1.Rows[x].Cells[colYr.Index].Value = dataset1.Tables[0].Rows[x].ItemArray[15];
                            //dataGridView1.Rows[x].Cells[colAtt.Index].Value = dataset1.Tables[0].Rows[x].ItemArray[18];


                        }
                        ////double amt = 0;
                        //for (int y = 0; y < dataset1.Tables[0].Columns.Count; y++)
                        //{
                        //    if (y == 8)
                        //    {
                        //        dataGridView1.Rows[x].Cells[9].Value = dataset1.Tables[0].Rows[x].ItemArray[8];
                        //    }
                        //    //else if (y == 5 || y == 6 || y == 7 || y == 8)
                        //    else if (y == 5 || y == 6 || y == 7)
                        //    {
                        //        if (Convert.ToString(dataset1.Tables[0].Rows[x].ItemArray[y]) != "")
                        //            dataGridView1.Rows[x].Cells[y].Value = dataset1.Tables[0].Rows[x].ItemArray[y];
                        //    }
                        //    else
                        //    { dataGridView1.Rows[x].Cells[y].Value = dataset1.Tables[0].Rows[x].ItemArray[y]; }
                        //}
                    }
                    if (checkBox2.Checked)
                    {
                        var isPNL = Database.GetExecuteScalarCommand_return("select count(*) from AC_EtnryReconsile with(Nolock) where CompanyName='" +
                            comboCompanyName.Text + "' and VoucharDate='" + dataset1.Tables[0].Rows[x].ItemArray[0] + "' and VoucherNo='" +
                            dataset1.Tables[0].Rows[x].ItemArray[3] + "' and VoucherType='" + dataset1.Tables[0].Rows[x].ItemArray[2] + "'");
                        {
                            dataGridView1.Rows[x].Cells[ColReco.Index].Value = (Convert.ToInt32(isPNL) == 0 ? false : true);
                            if (Convert.ToInt32(isPNL) == 1)
                                dataGridView1.Rows[x].DefaultCellStyle.BackColor = System.Drawing.Color.GreenYellow;
                        }
                    }

                    if (dataset1.Tables[0].Columns.Count - 1 >= 12)
                    {
                        if (dataset1.Tables[0].Rows[x].ItemArray[12].ToString() == "0")
                        {
                            dataGridView1.Rows[x].DefaultCellStyle.BackColor = System.Drawing.Color.Orange;
                        }
                        if (dataset1.Tables[0].Rows[x].ItemArray[12].ToString() == "-1")
                        {
                            dataGridView1.Rows[x].DefaultCellStyle.BackColor = System.Drawing.Color.Tomato;
                            dataGridView1.Rows[x].DefaultCellStyle.ForeColor = System.Drawing.Color.White;
                        }
                    }
                    //wtDialog.UpdateProgress((int)(x * 100) / rCount);
                }
            }

            System.Drawing.Font newFont = new System.Drawing.Font(dataGridView1.Font, FontStyle.Bold);


            for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
            {
                if (dataGridView1[5, i].FormattedValue.ToString().Trim() == "")
                    dataGridView1[5, i].Value = 0;
                if (dataGridView1[6, i].FormattedValue.ToString().Trim() == "")
                    dataGridView1[6, i].Value = 0;
                if (dataGridView1[1, i].FormattedValue.ToString().Trim() == "Unadujsted Forex Gain/Loss")
                {
                    dataGridView1.Rows[i].DefaultCellStyle.ForeColor = System.Drawing.Color.Blue;
                    dataGridView1.Rows[i].DefaultCellStyle.Font = newFont;
                }

            }

            double dr = 0;
            double cr = 0;
            double balance = 0;
            double oBalance = 0;
            for (int i = 0; i < dataGridView1.Rows.Count; i++)
            {
                if (dataGridView1.Rows[i].Cells[0].FormattedValue.ToString().Length == 0) continue;
                double cDR = Convert.ToDouble(dataGridView1.Rows[i].Cells[5].Value);
                double cCR = Convert.ToDouble(dataGridView1.Rows[i].Cells[6].Value);


                balance = cDR - cCR + balance;

                if (balance < 0)
                {

                    dataGridView1.Rows[i].Cells[7].Value = (-balance);//.ToString("#,##0.00", IN);
                    dataGridView1.Rows[i].Cells[8].Value = "Cr";
                }
                else
                {
                    dataGridView1.Rows[i].Cells[7].Value = balance;//.ToString("#,##0.00", IN);
                    dataGridView1.Rows[i].Cells[8].Value = "Dr";
                }

                if (i == 0 && Convert.ToString(dataGridView1.Rows[0].Cells[1].FormattedValue).Equals("Opening Balance"))
                {
                    oBalance = cDR - cCR;
                    cDR = 0;
                    cCR = 0;
                }

                dr += cDR;
                cr += cCR;
            }
            cr = Math.Round(cr, 2);
            dr = Math.Round(dr, 2);
            balance = Math.Round(balance, 2);
            oBalance = Math.Round(oBalance, 2);


            if (textDebit.InvokeRequired) textDebit.Invoke(new MethodInvoker(delegate { textDebit.Text = dr.ToString("#,##0.00"); }));
            else textDebit.Text = dr.ToString("#,##0.00");
            if (textClosingBalance.InvokeRequired) textCredit.Invoke(new MethodInvoker(delegate { textCredit.Text = cr.ToString("#,##0.00"); }));
            else textCredit.Text = cr.ToString("#,##0.00");

            if (textOpeningBalance.InvokeRequired) textOpeningBalance.Invoke(new MethodInvoker(delegate { textOpeningBalance.Text = oBalance.ToString("#,##0.00"); }));
            else textOpeningBalance.Text = oBalance.ToString("#,##0.00");

            if (textClosingBalance.InvokeRequired) textClosingBalance.Invoke(new MethodInvoker(delegate { textClosingBalance.Text = balance.ToString("#,##0.00"); }));
            else textClosingBalance.Text = balance.ToString("#,##0.00");

            //for (int i = 1; i < dataGridView1.Rows.Count - 1; i++)
            //{
            //    dataGridView1[7, i].Value = Convert.ToString(Math.Round((Convert.ToDouble(dataGridView1[7, i].Value) + Convert.ToDouble(dataGridView1[7, i - 1].Value)), 2));
            //}
        }

        private void FinishScript()
        {
            //ViewSummary();

            if (Convert.ToDouble(dataGridView1[5, 0].Value) > 0)
                textOpeningBalance.Text = Convert.ToDecimal(dataGridView1[5, 0].Value).ToString("#,##0.00", IN);
            else
                textOpeningBalance.Text = "-" + Convert.ToDecimal(dataGridView1[6, 0].Value).ToString("#,##0.00", IN);

            Database.OpenConnection(Utility.MaterialConnectionString);
            Database.myreader = Database.GetExecuteReaderCommand("select openingbalance from ledgermaster with(Nolock) where ledgername = '"
            + SelectedLedgerName + "' and companyname = '" + comboCompanyName.Text + "'");
            if (Database.myreader.Read())
                textStartingBalance.Text = Convert.ToDecimal(Database.myreader[0]).ToString("#,##0.00", IN);

            Database.myreader.Close();
            if (textStartingBalance.Text == "")
                textStartingBalance.Text = "0";
        }

        private void filterComboBox()
        {
            if (!isDelete)
            {
                string cValue = SelectedLedgerName.ToString();

                for (int i = 0; i < comboLedgerName.Items.Count; i++)
                {
                    if (!comboLedgerName.Items[i].ToString().ToLower().Contains(cValue.ToLower()))
                    {
                        comboLedgerName.Items.RemoveAt(i--);
                    }
                }

                // Refresh virtual system selected index
                if (comboLedgerName.Items.Count > 0)
                {
                    comboLedgerName.SelectedIndex = 0;
                    comboLedgerName.SelectedIndex = -1;
                    SelectedLedgerName = cValue;
                }

                int m = SelectedLedgerName.Length;
                comboLedgerName.SelectionStart = m;
                if (comboLedgerName.Items.Count == 1)
                {
                    comboLedgerName.SelectedIndex = 0;
                    comboLedgerName.SelectionStart = m;
                    comboLedgerName.SelectionLength = SelectedLedgerName.Length - m;
                }
            }
            else
            {
                if (SelectedLedgerName.Length == 0)
                {
                    if (comboLedgerName.Items.Count != Ledger.Length)
                    {
                        comboLedgerName.Items.Clear();
                        for (int i = 0; i < Ledger.Length; i++)
                        {
                            comboLedgerName.Items.Add(Ledger[i]);
                        }
                    }
                }
                else
                {
                    comboLedgerName.Items.Clear();
                    for (int i = 0; i < Ledger.Length; i++)
                    {
                        if (Ledger[i].ToLower().Contains(SelectedLedgerName.ToString().ToLower()))
                            comboLedgerName.Items.Add(Ledger[i]);
                    }
                }

                comboLedgerName.SelectionStart = SelectedLedgerName.Length;
            }
            comboLedgerName.Height = 30;

            isDelete = false;
        }

        #endregion

        #region Events

        private void frmLedgerSummary_Load(object sender, EventArgs e)
        {

        }

        private void comboCompanyName_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                Tools.Cache.ClearAllCache();
                Tools.Cache.ClearAllCache();
                comboLedgerName.DataSource = AccountCommonFunction.GetOpenLedgerList(comboCompanyName.Text);
                comboLedgerName.DisplayMember = AccountCommonVariable.FieldName.LedgerName;
                comboLedgerName.ValueMember = AccountCommonVariable.FieldName.LedgerId;
                comboLedgerName.SelectedIndex = -1;
                SelectedLedgerName = "";
            }
            catch (Exception ex)
            {
                ex.Message.ToString();
            }

        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex == -1)
                return;

            //if (e.ColumnIndex != ColReco.Index)
            //{
            //    dataGridView1.ReadOnly = true;
            //}
            //else
            //{
            //    dataGridView1.ReadOnly = false;
            //}
        }

        private void dataGridView1_MouseDoubleClick(object sender, MouseEventArgs e)
        {

        }

        private void dataGridView1_MouseDown(object sender, MouseEventArgs e)
        {
            try
            {
                if (dataGridView1.CurrentCell.ColumnIndex != ColReco.Index)
                {
                    dataGridView1.ReadOnly = true;
                }
                else
                {
                    dataGridView1.ReadOnly = false;
                }
            }
            catch (Exception ex)
            {

            }
        }

        private void dataGridView1_KeyDown(object sender, KeyEventArgs e)
        {
        }

        private void dataGridView2_SelectionChanged(object sender, EventArgs e)
        {

        }


        private void frmLedgerSummary_Voucher_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F5)
            {
            }
            if (e.KeyCode == Keys.Escape)
            {
                if (MessageBox.Show("Do you want Close Ledger Summary "
                           , "Ledger Summary", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    this.Close();
                    this.Dispose();
                }
            }
            if (e.Control && e.KeyCode == Keys.B)
            {
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            //if (comboLedgerName.Items.Contains(SelectedLedgerName))
            {
                (new frmContactInfo(SelectedLedgerName, comboCompanyName.Text)).ShowDialog();
                //frmContactInfo.partyname = SelectedLedgerName;
                ////frm.MdiParent = this;
                //TabClass.OpenForm(new frmContactInfo());
            }
            //else
            //MessageBox.Show("Please Insert proper Ledger Name");

        }

        //the following code is for deleting current row from datagrid and adding last row to grid
        private void dataGridView1_KeyUp(object sender, KeyEventArgs e)
        {
            return;
            if (e.KeyCode != Keys.Delete && e.KeyCode != Keys.E) return;

            try
            {
                if (e.KeyCode == Keys.Delete)
                {
                    #region Delete Row

                    int a = dataGridView1.CurrentCell.RowIndex;
                    if (dataGridView1.Rows.Count > 1 && a == dataGridView1.Rows.Count - 1) dataGridView1.CurrentCell = dataGridView1.Rows[a - 1].Cells[dataGridView1.CurrentCell.ColumnIndex];


                    if (dataGridView1.Rows[a].Cells[0].FormattedValue.ToString() != "")
                    {
                        col1.Add(dataGridView1.Rows[a].Cells[0].Value.ToString());
                        col2.Add(dataGridView1.Rows[a].Cells[1].Value.ToString());
                        col3.Add(dataGridView1.Rows[a].Cells[2].Value.ToString());
                        col4.Add(dataGridView1.Rows[a].Cells[3].Value.ToString());
                        col5.Add(dataGridView1.Rows[a].Cells[4].Value.ToString());
                        col6.Add(dataGridView1.Rows[a].Cells[5].Value.ToString());
                        col7.Add(dataGridView1.Rows[a].Cells[6].Value.ToString());
                        col8.Add(dataGridView1.Rows[a].Cells[7].Value.ToString());
                        col9.Add(dataGridView1.Rows[a].Cells[9].Value.ToString());
                        index.Add(a);

                        dataGridView1.Rows.RemoveAt(a);
                    }

                    double dr = 0;
                    double cr = 0;
                    double balance = 0;
                    double oBalance = 0;
                    for (int i = 0; i < dataGridView1.Rows.Count; i++)
                    {
                        if (dataGridView1.Rows[i].Cells[0].FormattedValue.ToString().Length == 0) continue;
                        double cDR = Convert.ToDouble(dataGridView1.Rows[i].Cells[5].FormattedValue);
                        double cCR = Convert.ToDouble(dataGridView1.Rows[i].Cells[6].FormattedValue);
                        balance = cDR - cCR + balance;

                        //if (balance < 0)
                        //{

                        //    dataGridView1.Rows[i].Cells[7].Value = (-balance).ToString("#,##0.00", IN);
                        //    dataGridView1.Rows[i].Cells[8].Value = "Cr";
                        //}
                        //else
                        //{
                        //    dataGridView1.Rows[i].Cells[7].Value = balance.ToString("#,##0.00", IN);
                        //    dataGridView1.Rows[i].Cells[8].Value = "Dr";
                        //}

                        if (i == 0 && Convert.ToString(dataGridView1.Rows[0].Cells[1].FormattedValue).Equals("Opening Balance"))
                        {
                            oBalance = cDR - cCR;
                            cDR = 0;
                            cCR = 0;
                        }

                        dr += cDR;
                        cr += cCR;
                    }
                    cr = Math.Round(cr, 2);
                    dr = Math.Round(dr, 2);
                    balance = Math.Round(balance, 2);
                    oBalance = Math.Round(oBalance, 2);


                    if (textDebit.InvokeRequired) textDebit.Invoke(new MethodInvoker(delegate { textDebit.Text = dr.ToString("#,##0.00", IN); }));
                    else textDebit.Text = dr.ToString("#,##0.00", IN);
                    if (textClosingBalance.InvokeRequired) textCredit.Invoke(new MethodInvoker(delegate { textCredit.Text = cr.ToString("#,##0.00", IN); }));
                    else textCredit.Text = cr.ToString("#,##0.00", IN);

                    if (textOpeningBalance.InvokeRequired) textOpeningBalance.Invoke(new MethodInvoker(delegate { textOpeningBalance.Text = oBalance.ToString("#,##0.00", IN); }));
                    else textOpeningBalance.Text = oBalance.ToString("#,##0.00", IN);

                    if (textClosingBalance.InvokeRequired) textClosingBalance.Invoke(new MethodInvoker(delegate { textClosingBalance.Text = balance.ToString("#,##0.00", IN); }));
                    else textClosingBalance.Text = balance.ToString("#,##0.00", IN);


                    FinishScript();
                    #endregion
                }
                else if (e.Control && e.KeyCode == Keys.E)
                {
                    #region Insert Row
                    if (col1.Count > 0)
                    {
                        int x = col1.Count - 1;
                        int indx = Convert.ToInt32((index[x].ToString()));
                        dataGridView1.Rows.Insert(indx);
                        dataGridView1.Rows[indx].Cells[0].Value = col1[x];
                        dataGridView1.Rows[indx].Cells[1].Value = col2[x];
                        dataGridView1.Rows[indx].Cells[2].Value = col3[x];
                        dataGridView1.Rows[indx].Cells[3].Value = col4[x];
                        dataGridView1.Rows[indx].Cells[4].Value = col5[x];
                        dataGridView1.Rows[indx].Cells[5].Value = col6[x];
                        dataGridView1.Rows[indx].Cells[6].Value = col7[x];
                        dataGridView1.Rows[indx].Cells[7].Value = col8[x];
                        dataGridView1.Rows[indx].Cells[9].Value = col9[x];

                        col1.RemoveAt(x);
                        col2.RemoveAt(x);
                        col3.RemoveAt(x);
                        col4.RemoveAt(x);
                        col5.RemoveAt(x);
                        col6.RemoveAt(x);
                        col7.RemoveAt(x);
                        col8.RemoveAt(x);
                        col9.RemoveAt(x);
                        index.RemoveAt(x);
                    }

                    else
                    {
                        MessageBox.Show("There is no data to insert");
                        return;
                    }
                    #endregion
                }

                #region Calculation
                if (e.KeyCode == Keys.Delete || (e.Control && e.KeyCode == Keys.E))
                {
                    double dr = 0;
                    double cr = 0;
                    double balance = 0;
                    double oBalance = 0;
                    for (int i = 0; i < dataGridView1.Rows.Count; i++)
                    {
                        if (dataGridView1.Rows[i].Cells[0].FormattedValue.ToString().Length == 0) continue;
                        double cDR = Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[5].FormattedValue.ToString());
                        double cCR = Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[6].FormattedValue.ToString());
                        balance = cDR - cCR + balance;

                        if (balance < 0)
                        {
                            dataGridView1.Rows[i].Cells[7].Value = -balance;
                            dataGridView1.Rows[i].Cells[8].Value = "Cr";
                        }
                        else
                        {
                            dataGridView1.Rows[i].Cells[7].Value = balance;
                            dataGridView1.Rows[i].Cells[8].Value = "Dr";
                        }

                        if (i == 0 && Convert.ToString(dataGridView1.Rows[0].Cells[1].FormattedValue).Equals("Opening Balance"))
                        {
                            oBalance = cDR - cCR;
                            cDR = 0;
                            cCR = 0;
                        }

                        dr += cDR;
                        cr += cCR;
                    }
                    // textClosingBalance.Text = balance.ToString();
                    textDebit.Text = dr.ToString();
                    textCredit.Text = cr.ToString();
                    textOpeningBalance.Text = oBalance.ToString();

                    textClosingBalance.Text = balance.ToString();
                }
                #endregion
            }
            catch (Exception ex)
            {

                ErrorMessageBox.Show(ex);

            }
        }

        private void button5_ShowAll(object sender, EventArgs e)
        {
            if (avd.ExpandAll()) (sender as Button).Text = "Hide All";
            else (sender as Button).Text = "Show All";
        }

        private void label8_Click(object sender, EventArgs e)
        {

        }

        private void button7_Click(object sender, EventArgs e)
        {
            TabClass.OpenForm(new FrmLedgerSummaryMnthWise(comboCompanyName.Text, SelectedLedgerName, dateTimePicker1.Value, dateTimePicker2.Value));
        }

        private void button8_Click(object sender, EventArgs e)
        {
            //frmLedgerMaster.isFromLedger = true;

            //frmLedgerMaster.LedgerName_Summary = SelectedLedgerName;
            //TabClass.OpenForm(new frmLedgerMaster());

            (new frmAddUpdateLedger(comboCompanyName.Text, SelectedLedgerName)).ShowDialog();
        }

        private void textStartingBalance_TextChanged(object sender, EventArgs e)
        {

        }

        private void button9_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Multiple print may take several time\n\nDo you want to continue?", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != System.Windows.Forms.DialogResult.Yes)
                return;

            using (account.Classes.PrintMultiVoucher pmv = new account.Classes.PrintMultiVoucher())
            {

                DataTable dt = pmv.dataTable;

                int rCount = dataGridView1.Rows.Count;
                if (dataGridView1.AllowUserToAddRows)
                    rCount--;

                for (int i = 0; i < rCount; i++)
                {
                    DataGridViewRow dr = dataGridView1.Rows[i];
                    if (!dr.Visible)
                        continue;

                    if (Convert.ToString(dr.Cells[ColumnVoucherType.Index].Value).ToLower().Trim() == "z")
                        continue;

                    dt.Rows.Add(comboCompanyName.Text, dr.Cells[ColumnVoucherNo.Index].Value, dr.Cells[ColumnDate.Index].Value, dr.Cells[ColumnVoucherType.Index].Value);
                }

                pmv.Print();
            }

            //FrmPrintMultiLedger.Ds1 = dataset1;
            //FrmPrintMultiLedger.CompanyName = comboCompanyName.Text;
            //FrmPrintMultiLedger.LedgerName = SelectedLedgerName;
            //FrmPrintMultiLedger.yr = frmDefaultVale.Year;
            //TabClass.OpenForm(new FrmPrintMultiLedger());
        }

        private void button9_Click_1(object sender, EventArgs e)
        {
            bool isbankacc = false;
            if (FrmMain.IsCrbankReco)
            {
                if (Database.OpenConnection(Utility.MaterialConnectionString))
                {
                    Database.myreader = Database.GetExecuteReaderCommand("select LedgerName from ledgermaster with(Nolock) where Category = 'Bank Accounts'  and companyname = '" +
                    comboCompanyName.Text + "' and ledgername = '" + SelectedLedgerName + "' ");
                    if (Database.myreader.Read())
                        isbankacc = true;
                    Database.myreader.Close();

                    if (isbankacc)
                    {
                        FrmBankReconciliation.ledgername = SelectedLedgerName;
                        DateTime DateFrom = dateTimePicker1.Value;
                        DateTime DateTo = dateTimePicker2.Value;

                        FrmBankReconciliation.datefrom = DateFrom;
                        FrmBankReconciliation.dateto = DateTo;
                        FrmBankReconciliation frm = new FrmBankReconciliation();
                        TabClass.OpenForm(new FrmBankReconciliation());
                    }
                }
            }
        }

        private void button10_Click(object sender, EventArgs e)
        {

            DateTime DateTo = dateTimePicker2.Value;
            FrmBillWiseAgstRefReport.isbillwiseReport = true;
            FrmBillWiseAgstRefReport.dateto = DateTo;
            FrmBillWiseAgstRefReport.companyname = comboCompanyName.Text;
            FrmBillWiseAgstRefReport.ledgername = SelectedLedgerName;
            TabClass.OpenForm(new FrmBillWiseAgstRefReport());

        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            contextMenuStrip1.Show(toolStrip1, new Point(0, toolStrip1.Height - 5));
        }

        private void printLedgerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //DataGridView dgv = (DataGridView)dataGridView1;
            //DataTable dt = new DataTable();
            //for (int i = 0; i < dgv.Columns.Count; i++)
            //{
            //    dt.Columns.Add(dgv.Columns[i].Name);
            //}
            //dt.Columns[5].ColumnName = "Debit";
            //dt.Columns[6].ColumnName = "Credit";
            //dt.Columns[7].ColumnName = "ClosingBalance";
            //object[] drValue = new object[dgv.Columns.Count];
            //foreach (DataGridViewRow dr in dgv.Rows)
            //{
            //    if (dr.Visible == false) continue;

            //    for (int i = 0; i < drValue.Length; i++)
            //    {
            //        if (i == 5 || i == 6)
            //        {
            //            if (Convert.ToString(drValue[i]).Length == 0) drValue[i] = 0;
            //        }

            //        if (dr.Cells[0].FormattedValue.ToString().Length == 0 || (i == 5 || i == 6))
            //            drValue[i] = dr.Cells[i].FormattedValue;
            //        else
            //            drValue[i] = "<b>" + dr.Cells[i].FormattedValue.ToString() + "</b>";


            //    }
            //    dt.Rows.Add(drValue);
            //}

            try
            {
                if (Database.OpenConnection(Utility.MaterialConnectionString))
                {
                    Database.GetExecuteNonQueryCommand("Delete from LedgerSummaryBalTemp where ledgername = '" + SelectedLedgerName + "'and companyname='"
                                     + comboCompanyName.Text + "' and username='" + FrmMain.UserName + "'");

                    if (Database.GetExecuteNonQueryCommand("insert into LedgerSummaryBalTemp values('" + comboCompanyName.Text
                 + "','" + SelectedLedgerName + "'," + Utility.SafeConvertToDouble(textOpeningBalance.Text)
                 + "," + Convert.ToDouble(textClosingBalance.Text.ToString()) + ",'" + FrmMain.UserName + "','"
                 + dateTimePicker1.Value.ToString("MM/dd/yyyy") + "','"
                 + dateTimePicker2.Value.ToString("MM/dd/yyyy")
                 + "'," + Convert.ToDouble(textDebit.Text) + "," + Convert.ToDouble(textCredit.Text) + ")"))
                    {
                        //frmPrintLedgerSummary.dtLedgerReport = dt;
                        //frmPrintLedgerSummary.ledgername = SelectedLedgerName;
                        //frmPrintLedgerSummary.cmpnyname = comboCompanyName.Text;

                        TabClass.OpenForm(new frmPrintLedgerSummary(comboCompanyName.Text, SelectedLedgerName, dateTimePicker1.Value, dateTimePicker2.Value));
                        //TabClass.OpenForm(new frmPrintLedgerSummary());
                    }
                }

            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);

            }

            //        TabClass.OpenForm(new frmPrintLedgerSummary(comboCompanyName.Text, SelectedLedgerName, dateTimePicker1.Value, dateTimePicker2.Value));
        }

        private void columnerReportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (SelectedLedgerName.Length == 0)
            {
                MessageBox.Show("Select Proper Ledger Name");
                return;
            }

            //frmColmnerGrid frm = new frmColmnerGrid(dateTimePicker1.Value, dateTimePicker2.Value,
            //    comboCompanyName.Text, SelectedLedgerName,
            //    col1.ToArray(), col4.ToArray(), col5.ToArray(), col3.ToArray());

            frmColmnerGrid frm = new frmColmnerGrid(dateTimePicker1.Value, dateTimePicker2.Value,
                comboCompanyName.Text, SelectedLedgerName, drr.DeletedRows);
            TabClass.OpenForm(frm);
        }

        private void columnerPrintToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //frmPrintLedgerSummary_COLUMNER.ledgername = SelectedLedgerName;
            //frmPrintLedgerSummary_COLUMNER.DateFrom = dateTimePicker1.Value;
            //frmPrintLedgerSummary_COLUMNER.DateTo = dateTimePicker2.Value;

            //frmPrintLedgerSummary_COLUMNER.DeletedDate = col1.ToArray();
            //frmPrintLedgerSummary_COLUMNER.DeletedVoucherNo = col4.ToArray();
            //frmPrintLedgerSummary_COLUMNER.DeletedVoucherRef = col5.ToArray();
            //frmPrintLedgerSummary_COLUMNER.DeletedVoucherType = col3.ToArray();

            //TabClass.OpenForm(new frmPrintLedgerSummary_COLUMNER());
        }

        private void button1_Click(object sender, EventArgs e)
        {
            filterData = new filter.filterData(dataGridView1);
            OK();
            
        }

        private void intercompanyexporttoexcel()
        {

            try
            {
                ArrayList Arrledgername = new ArrayList();
                if (Database.OpenConnection(Utility.MaterialConnectionString)) 
                {
                    Database.myreader = Database.GetExecuteReaderCommand("select ledgername from ledgermaster where companyname = '"
                        + comboCompanyName.Text + "' and IsInterCompany = 'yes'");
                    while (Database.myreader.Read())
                        Arrledgername.Add(Database.myreader[0].ToString());
                    Database.myreader.Close();

                }

                int ExportProgress = 0;
                bool CancelExport = false;

                int RowsCompleted = 0;


                string file = System.IO.Path.GetTempPath() + "ledgername1.xls";
                if (file != null)
                {
                    if (System.IO.File.Exists(file))
                        System.IO.File.Delete(file);
                }
                //export specific data to Excel
                Microsoft.Office.Interop.Excel.Application excel = new Microsoft.Office.Interop.Excel.Application();
                excel.Application.Workbooks.Add(true);

                for (int k = 0; k < Arrledgername.Count; k++)
                {
                    DataSet dt1 = GetAllDetatils(Arrledgername[k].ToString());
                 
                    Microsoft.Office.Interop.Excel.Worksheet sheet = excel.ActiveWorkbook.ActiveSheet as Microsoft.Office.Interop.Excel.Worksheet;

                    sheet.Name = k.ToString() + "-" + Arrledgername[k].ToString().Substring(0,22);

                    DataTable table = dt1.Tables[0];
                    int rCount = table.Rows.Count;
                    int TotalRows = rCount;

                    Microsoft.Office.Interop.Excel.Range r1;

                 
                    
                    sheet.Cells[1, 1] = "In Book of ";
                    sheet.Cells[1, 2] = comboCompanyName.Text;

                    sheet.Cells[2, 1] = "Ledger Name" ;
                    sheet.Cells[2, 2] = Arrledgername[k].ToString();

                    for (int i = 0; i < table.Columns.Count; i++)
                    {
                        sheet.Cells[3, i + 1] = table.Columns[i].ColumnName;
                    }

                    r1 = sheet.get_Range(sheet.Cells[1, 1], sheet.Cells[1, table.Columns.Count]);
                    r1.Font.Bold = true;

                    sheet.Rows.AutoFit();
                    sheet.Columns.AutoFit();

            

                    for (int i = 0; i < table.Rows.Count; i++)
                    {
                        if (CancelExport) break;
                        for (int j = 0; j < table.Columns.Count; j++)
                        {
                            if (table.Rows[i].ItemArray[j] is DBNull)
                                continue;
                            sheet.Cells[i + 4, j + 1] = table.Rows[i].ItemArray[j];
                        }
                        ExportProgress = (i * 100) / (rCount * 1);
                        RowsCompleted = i;
                    }
                    sheet.Rows.AutoFit();
                    sheet.Columns.AutoFit();

            
                    excel.Application.Worksheets.Add(sheet);

                    sheet.Rows.AutoFit();
                    sheet.Columns.AutoFit();

            
                }
                Microsoft.Office.Interop.Excel.Worksheet worksheet = (Microsoft.Office.Interop.Excel.Worksheet)excel.ActiveSheet;
                object oMissing = System.Reflection.Missing.Value;
                object FileFormat = true;
                worksheet.SaveAs(file, FileFormat, oMissing, oMissing, oMissing, oMissing, oMissing, oMissing, oMissing, oMissing);
                excel.Quit();
                System.Diagnostics.Process.Start(file);

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void comboLedgerName_Leave(object sender, EventArgs e)
        {
            try
            {

                if (Database.OpenConnection(Utility.MaterialConnectionString))
                {
                    SelectedLedgerName = Convert.ToString(Database.GetExecuteScalarCommand_return("Select LedgerName from ledgerMaster with(Nolock) where srno="
                        + Convert.ToInt32(comboLedgerName.SelectedValue)));
                }
                button10.Enabled = AccountCommonFunction.isBillwise(SelectedLedgerName, comboCompanyName.Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);

            }
        }

        //private void comboLedgerName_KeyDown(object sender, KeyEventArgs e)
        //{
        //    return;
        //    if (e.KeyCode == Keys.Escape)
        //    {
        //        SelectedLedgerName = "";
        //        return;
        //    }

        //    if (!comboLedgerName.DroppedDown) comboLedgerName.DroppedDown = true;
        //}

        private void timer1_Tick(object sender, EventArgs e)
        {
            filterComboBox();
            timer1.Stop();
        }

        private void chknarration_CheckedChanged(object sender, EventArgs e)
        {
            avd.isNarrationOnly = (sender as CheckBox).Checked;
        }

        private void dataGridView1_Resize(object sender, EventArgs e)
        {
            dataGridView1.Columns[1].Width = dataGridView1.Width - 930;
        }

        #endregion

        private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
        {

        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            try
            {
                if (Database.OpenConnection(Utility.MaterialConnectionString))
                {
                    Database.GetExecuteNonQueryCommand("Delete from LedgerSummaryBalTemp where ledgername = '" + SelectedLedgerName + "'and companyname='"
                                     + comboCompanyName.Text + "' and username='" + FrmMain.UserName + "'");

                    if (Database.GetExecuteNonQueryCommand("insert into LedgerSummaryBalTemp values('" + comboCompanyName.Text
                 + "','" + SelectedLedgerName + "'," + Utility.SafeConvertToDouble(textOpeningBalance.Text)
                 + "," + Convert.ToDouble(textClosingBalance.Text.ToString()) + ",'" + FrmMain.UserName + "','"
                 + dateTimePicker1.Value.ToString("MM/dd/yyyy") + "','"
                 + dateTimePicker2.Value.ToString("MM/dd/yyyy")
                 + "'," + Convert.ToDouble(textDebit.Text) + "," + Convert.ToDouble(textCredit.Text) + ")"))
                    {
                        //frmPrintLedgerSummary.dtLedgerReport = dt;
                        //frmPrintLedgerSummary.ledgername = SelectedLedgerName;
                        //frmPrintLedgerSummary.cmpnyname = comboCompanyName.Text;

                        TabClass.OpenForm(new frmConfirmationLedgerSummary(comboCompanyName.Text, SelectedLedgerName, dateTimePicker1.Value, dateTimePicker2.Value, "*"));
                        //TabClass.OpenForm(new frmPrintLedgerSummary());
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

        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            try
            {
                //if (Math.Round(Utility.SafeConvertToDouble(textBox2.Text.ToString()), 2) != Math.Round(Utility.SafeConvertToDouble(textBox1.Text.ToString()), 2))
                //{
                //    MessageBox.Show("Reco Credit & Debit Amount not match.");
                //    return;
                //}
                string filename = SelectedLedgerName + "_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".pdf";
                //WindowScreenshot(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), filename, ImageFormat.Jpeg);

                if (Database.OpenConnection(Utility.MaterialConnectionString))
                {
                    for (int i = 0; i <= dataGridView1.Rows.Count - 1; i++)
                    {
                        if (Convert.ToBoolean(dataGridView1.Rows[i].Cells[ColReco.Index].Value) == true)
                        {
                            Database.GetExecuteNonQueryCommand_retrun("insert into AC_EtnryReconsile(CreatedDate, LoginUser, CompanyName, VoucharDate,  VoucherType, VoucherNo,LedgerName) values(Getdate(),'" +
                                FrmMain.UserName + "','" + comboCompanyName.Text + "','" + dataGridView1.Rows[i].Cells[ColumnDate.Index].Value + "','"
                                + dataGridView1.Rows[i].Cells[ColumnVoucherType.Index].Value
                                + "','" + dataGridView1.Rows[i].Cells[ColumnVoucherNo.Index].Value + "','" + SelectedLedgerName + "')");
                        }
                    }
                    exportToPDF(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), filename);

                    //exportToBMP(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), filename);

                    MessageBox.Show("Selected Entries are Store.\n\nFor Attachment Please check File at Location :" + Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\PDFs\\" + filename);
                    Tools.DMSManager.OpenReference("GL_RC", string.Format("{0}/{1}/GL_RC/{2}", AccountCommonFunction.GetCompanyCode(comboCompanyName.Text), frmDefaultVale.Year, SelectedLedgerName));



                    if (MessageBox.Show("Do you want Deleted Local Attachment", "Attachment", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == System.Windows.Forms.DialogResult.Yes)
                    {
                        System.IO.File.Delete(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\PDFs\\" + filename);
                    }

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Problem with Reconsile Log Saving :" + ex.Message);
            }
        }
        private void exportToBMP(String filepath, String filename)
        {
            Bitmap bitmap;
            dataGridView1.Dock = DockStyle.None;
            //Resize DataGridView to full height.
            int height = dataGridView1.Height;
            dataGridView1.Height = dataGridView1.RowCount * dataGridView1.RowTemplate.Height;

            //Create a Bitmap and draw the DataGridView on it.
            bitmap = new Bitmap(this.dataGridView1.Width, this.dataGridView1.Height);
            dataGridView1.DrawToBitmap(bitmap, new System.Drawing.Rectangle(0, 0, this.dataGridView1.Width, this.dataGridView1.Height));

            //Resize DataGridView back to original height.
            dataGridView1.Height = height;
            bitmap = new Bitmap(dataGridView1.Bounds.Width, dataGridView1.Bounds.Height);
            dataGridView1.DrawToBitmap(bitmap, dataGridView1.Bounds);
            //Show the Print Preview Dialog.
            dataGridView1.Dock = DockStyle.Fill;


            string folderPath = filepath + "\\PDFs\\";
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            //using (FileStream stream = new FileStream(folderPath + filename, FileMode.Create))

            bitmap.Save(folderPath + filename);

        }
        private void exportToPDF(String filepath, String filename)
        {
            //Creating iTextSharp Table from the DataTable data
            PdfPTable pdfTable = new PdfPTable(dataGridView1.ColumnCount);

            pdfTable.DefaultCell.Padding = 1;

            pdfTable.WidthPercentage = 100;
            pdfTable.HorizontalAlignment = Element.ALIGN_LEFT;
            pdfTable.DefaultCell.BorderWidth = 1;


            //create an array to store column widths
            float[] colWidths = new float[dataGridView1.Columns.Count];


            //Adding Header row
            foreach (DataGridViewColumn column in dataGridView1.Columns)
            {
                PdfPCell cell = new PdfPCell(new Phrase(column.HeaderText));
                cell.BackgroundColor = new iTextSharp.text.Color(240, 240, 240);
                pdfTable.AddCell(cell);
            }

            //pdfTable.SetTotalWidth(colWidths);
            //Adding DataRow

            {

                {
                    foreach (DataGridViewRow row in dataGridView1.Rows)
                    {
                        foreach (DataGridViewCell cell in row.Cells)
                        {
                            if (cell.ColumnIndex == 0)
                            {

                                PdfPCell cell1 = new PdfPCell(new Phrase(Convert.ToDateTime(cell.Value).ToString("dd-MM-yyyy")));
                                if (row.DefaultCellStyle.BackColor.R == 0 && row.DefaultCellStyle.BackColor.G == 0 && row.DefaultCellStyle.BackColor.B == 0)
                                {
                                    cell1.BackgroundColor = new iTextSharp.text.Color(255, 255, 255);
                                }
                                else
                                {
                                    cell1.BackgroundColor = new iTextSharp.text.Color(row.DefaultCellStyle.BackColor.R, row.DefaultCellStyle.BackColor.G, row.DefaultCellStyle.BackColor.B);
                                }
                                pdfTable.AddCell(cell1);
                            }
                            else
                            {

                                PdfPCell cell1 = new PdfPCell(new Phrase(Convert.ToString(cell.Value)));
                                if (row.DefaultCellStyle.BackColor.R == 0 && row.DefaultCellStyle.BackColor.G == 0 && row.DefaultCellStyle.BackColor.B == 0)
                                {
                                    cell1.BackgroundColor = new iTextSharp.text.Color(255, 255, 255);
                                }
                                else
                                {
                                    cell1.BackgroundColor = new iTextSharp.text.Color(row.DefaultCellStyle.BackColor.R, row.DefaultCellStyle.BackColor.G, row.DefaultCellStyle.BackColor.B);
                                }
                                pdfTable.AddCell(cell1);
                            }
                        }
                    }
                }
            }

            //Exporting to PDF
            string folderPath = filepath + "\\PDFs\\";
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            using (FileStream stream = new FileStream(folderPath + filename, FileMode.Create))
            {
                Document pdfDoc = new Document(PageSize.LEDGER, 0f, 0f, 0f, 0f);
                PdfWriter.GetInstance(pdfDoc, stream);
                pdfDoc.Open();
                pdfDoc.Add(pdfTable);
                pdfDoc.Close();
                stream.Close();
            }
        }
        private void WindowScreenshot(String filepath, String filename, ImageFormat format)
        {
            ScreenCapture sc = new ScreenCapture();

            string fullpath = filepath + "\\" + filename;

            //sc.CaptureWindowToFile(this.Handle, fullpath, format);
            sc.CaptureScreenToFile(fullpath, format);
        }

        private void dataGridView1_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (e.RowIndex == -1)
                    return;

                //if (e.ColumnIndex != ColReco.Index)
                //{
                //    dataGridView1.ReadOnly = true;
                //}
                //else
                //{
                //    dataGridView1.ReadOnly = false;
                //}

                if (e.ColumnIndex == ColReco.Index)
                {
                    bool blntrue = Convert.ToBoolean(dataGridView1.Rows[e.RowIndex].Cells[ColReco.Index].Value);
                    if (Convert.ToInt32(dataGridView1.Rows[e.RowIndex].Cells[RecoIn.Index].Value) > 0 && blntrue == false)
                    {
                        if (!FrmMain.IsAdmin)
                        {
                            MessageBox.Show("You are not Authorized to change Reco Entry");
                            dataGridView1.Rows[e.RowIndex].Cells[ColReco.Index].Value = true;
                            return;
                        }

                        if (MessageBox.Show("Reco Is already done for this Record Do you still want to remove Reco", "Reco Msg", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == System.Windows.Forms.DialogResult.No)
                        {
                            dataGridView1.Rows[e.RowIndex].Cells[ColReco.Index].Value = true;
                            return;
                        }
                        else
                        {
                            Database.GetExecuteScalarCommand_return("update AC_EtnryReconsile set Isdel=1 where CompanyName='" +
                                comboCompanyName.Text + "' and VoucharDate='" + dataGridView1.Rows[e.RowIndex].Cells[ColumnDate.Index].Value + "' and VoucherNo='" +
                                dataGridView1.Rows[e.RowIndex].Cells[ColumnVoucherNo.Index].Value + "' and VoucherType='" +
                                dataGridView1.Rows[e.RowIndex].Cells[ColumnVoucherType.Index].Value + "' and isnull(Isdel,0)=0");
                        }
                    }


                    if (blntrue)
                    {

                        dataGridView1.Rows[e.RowIndex].Tag = dataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex].InheritedStyle.BackColor.A + "," + dataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex].InheritedStyle.BackColor.R + "," + dataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex].InheritedStyle.BackColor.G + "," + dataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex].InheritedStyle.BackColor.B;
                        dataGridView1.Rows[e.RowIndex].DefaultCellStyle.BackColor = System.Drawing.Color.GreenYellow;
                    }
                    else
                    {
                        if (dataGridView1.Rows[e.RowIndex].Tag.ToString().Contains("255,255,255"))
                            dataGridView1.Rows[e.RowIndex].DefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(255, 255, 255);
                        else
                            dataGridView1.Rows[e.RowIndex].DefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(229, 218, 251);
                    }

                    {
                        double Credit = 0;
                        double Debit = 0;
                        for (int i = 0; i <= dataGridView1.Rows.Count - 1; i++)
                        {
                            if (Convert.ToBoolean(dataGridView1.Rows[i].Cells[ColReco.Index].Value) == true)
                            {
                                Debit += Math.Round(Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[ColumnDebit.Index].Value.ToString()), 2);
                                Credit += Math.Round(Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[ColumnCredit.Index].Value.ToString()), 2);
                            }
                        }
                        if (Debit == 0 && Credit == 0)
                        {
                            textBox2.Visible = false;
                            textBox1.Visible = false;
                            label13.Visible = false;
                            label12.Visible = false;
                            label14.Visible = false;
                            textBox3.Visible = false;
                        }
                        else
                        {
                            textBox2.Visible = true;
                            textBox1.Visible = true;
                            label13.Visible = true;
                            label12.Visible = true;
                            label14.Visible = true;
                            textBox3.Visible = true;
                        }
                        textBox2.Text = Debit.ToString();
                        textBox1.Text = Credit.ToString();
                        textBox3.Text = Math.Round(Credit - Debit, 2).ToString();
                    }
                }
            }
            catch (Exception ex)
            {

            }
        }

        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (dataGridView1.CurrentCell.ColumnIndex != ColReco.Index)
            {
                dataGridView1.ReadOnly = true;
            }
            else
            {
                dataGridView1.ReadOnly = false;
            }

            if (dataGridView1.CurrentCell.ColumnIndex == colAtt.Index)
            {
                try
                {
                    int aat = Convert.ToInt32(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[colAtt.Index].Value);
                    if (aat > 0)
                    {
                        string yr = Convert.ToString(dataGridView1.Rows[e.RowIndex].Cells[colYr.Index].Value);
                        string voucherno = Convert.ToString(dataGridView1.Rows[e.RowIndex].Cells[ColumnVoucherNo.Index].Value);
                        string RefType = Convert.ToString(dataGridView1.Rows[e.RowIndex].Cells[colRefT.Index].Value);
                        string companyname = Convert.ToString(comboCompanyName.Text);
                        Tools.DMSManager.OpenReference(RefType, string.Format("{0}/{1}/" + RefType + "/{2}",
                           AccountCommonFunction.GetCompanyCode(companyname),
                           yr, voucherno));
                    }

                }
                catch (Exception ex)
                {
                    MessageBox.Show("Problem with Open Attachment" + ex.Message);
                }
            }


        }

        private void attachmentsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Tools.DMSManager.OpenReference("GL_RC", string.Format("{0}/{1}/GL_RC/{2}",
                    AccountCommonFunction.GetCompanyCode(comboCompanyName.Text),
                    frmDefaultVale.Year, SelectedLedgerName));
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            try
            {

                btnSave.Visible = checkBox2.Checked;
                btnSave.Enabled = checkBox2.Checked;

                if (checkBox2.Checked)
                    ColReco.Visible = true;
                else
                    ColReco.Visible = false;

                if (checkBox2.Checked)
                {

                    for (int x = 0; x <= dataGridView1.Rows.Count - 1; x++)
                    {
                        if (Database.OpenConnection(Utility.MaterialConnectionString))
                        {
                            var isPNL = Database.GetExecuteScalarCommand_return("select count(*) from AC_EtnryReconsile with(Nolock) where CompanyName='" +
                                comboCompanyName.Text + "' and VoucharDate='" + dataset1.Tables[0].Rows[x].ItemArray[0] + "' and VoucherNo='" +
                                dataset1.Tables[0].Rows[x].ItemArray[3] + "' and VoucherType='" + dataset1.Tables[0].Rows[x].ItemArray[2] +
                                "' and isnull(Isdel,0)=0 and LedgerName='" + SelectedLedgerName + "'");
                            {
                                dataGridView1.Rows[x].Cells[ColReco.Index].Value = (Convert.ToInt32(isPNL) == 0 ? false : true);
                                if (Convert.ToInt32(isPNL) == 1)
                                    dataGridView1.Rows[x].DefaultCellStyle.BackColor = System.Drawing.Color.GreenYellow;
                                dataGridView1.Rows[x].Cells[RecoIn.Index].Value = Convert.ToInt32(isPNL);

                                isPNL = Database.GetExecuteScalarCommand_return("select distinct top 1 ID from AC_EtnryReconsile with(Nolock) where CompanyName='" +
                                comboCompanyName.Text + "' and VoucharDate='" + dataset1.Tables[0].Rows[x].ItemArray[0] + "' and VoucherNo='" +
                                dataset1.Tables[0].Rows[x].ItemArray[3] + "' and VoucherType='" + dataset1.Tables[0].Rows[x].ItemArray[2] + "' and isnull(Isdel,0)=0");

                                dataGridView1.Rows[x].Cells[ColId.Index].Value = Convert.ToInt32(isPNL);

                            }
                        }
                    }
                }

                if (checkBox2.Checked)
                {
                    textBox2.Visible = false;
                    textBox1.Visible = false;
                    label13.Visible = false;
                    label12.Visible = false;
                    label14.Visible = false;
                    textBox3.Visible = false;
                }
                else
                {
                    textBox2.Visible = true;
                    textBox1.Visible = true;
                    label13.Visible = true;
                    label12.Visible = true;
                    label14.Visible = true;
                    textBox3.Visible = true;
                }


            }
            catch (Exception EX)
            {
            }

        }

        private void printLedgerGridToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (Database.OpenConnection(Utility.MaterialConnectionString))
                {
                    Database.GetExecuteNonQueryCommand("Delete from LedgerSummaryBalTemp where ledgername = '" + SelectedLedgerName + "'and companyname='"
                                     + comboCompanyName.Text + "' and username='" + FrmMain.UserName + "'");

                    if (Database.GetExecuteNonQueryCommand("insert into LedgerSummaryBalTemp values('" + comboCompanyName.Text
                 + "','" + SelectedLedgerName + "'," + Utility.SafeConvertToDouble(textOpeningBalance.Text)
                 + "," + Convert.ToDouble(textClosingBalance.Text.ToString()) + ",'" + FrmMain.UserName + "','"
                 + dateTimePicker1.Value.ToString("MM/dd/yyyy") + "','"
                 + dateTimePicker2.Value.ToString("MM/dd/yyyy")
                 + "'," + Convert.ToDouble(textDebit.Text) + "," + Convert.ToDouble(textCredit.Text) + ")"))
                    {
                        //frmPrintLedgerSummary.dtLedgerReport = dt;
                        //frmPrintLedgerSummary.ledgername = SelectedLedgerName;
                        //frmPrintLedgerSummary.cmpnyname = comboCompanyName.Text;

                        TabClass.OpenForm(new frmPrintLedgerSummary(comboCompanyName.Text, SelectedLedgerName, dateTimePicker1.Value, dateTimePicker2.Value, dataGridView1));
                        //TabClass.OpenForm(new frmPrintLedgerSummary());
                    }
                }

            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);

            }
        }

        private void hideColumnToolStripMenuItem_Click(object sender, EventArgs e)
        {
            dataGridView1.Columns[dataGridView1.CurrentCell.ColumnIndex].Visible = false;
        }

        private void loadClosedLedgersToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                Tools.Cache.ClearAllCache();
                Tools.Cache.ClearAllCache();
                comboLedgerName.DataSource = null;
                dtledger = AccountCommonFunction.GetClosedLedger(comboCompanyName.Text);
                comboLedgerName.DataSource = dtledger;
                comboLedgerName.DisplayMember = AccountCommonVariable.FieldName.LedgerName;
                comboLedgerName.ValueMember = AccountCommonVariable.FieldName.LedgerId;
                comboLedgerName.SelectedIndex = -1;
                SelectedLedgerName = "";
                new ComboFilter(comboLedgerName);
            }
            catch (Exception ex)
            {
                ex.Message.ToString();
            }
        }

        private void loadAllLedgersToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                Tools.Cache.ClearAllCache();
                Tools.Cache.ClearAllCache();
                comboLedgerName.DataSource = null;
                dtledger = new DataTable();
                dtledger = AccountCommonFunction.GetLedgerListwithCondition(comboCompanyName.Text);
                comboLedgerName.DataSource = dtledger;// AccountCommonFunction.GetLedgerList(comboCompanyName.Text);
                comboLedgerName.DisplayMember = AccountCommonVariable.FieldName.LedgerName;
                comboLedgerName.ValueMember = AccountCommonVariable.FieldName.LedgerId;
                comboLedgerName.SelectedIndex = -1;
                SelectedLedgerName = "";
                new ComboFilter(comboLedgerName);
            }
            catch (Exception ex)
            {
                ex.Message.ToString();
            }
        }

        private void loadOpenLedgersToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                Tools.Cache.ClearAllCache();
                Tools.Cache.ClearAllCache();
                comboLedgerName.DataSource = null;
                comboLedgerName.DataSource = AccountCommonFunction.GetOpenLedgerList(comboCompanyName.Text);
                comboLedgerName.DisplayMember = AccountCommonVariable.FieldName.LedgerName;
                comboLedgerName.ValueMember = AccountCommonVariable.FieldName.LedgerId;
                comboLedgerName.SelectedIndex = -1;
                SelectedLedgerName = "";
                new ComboFilter(comboLedgerName);
            }
            catch (Exception ex)
            {
                ex.Message.ToString();
            }
        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            intercompanyexporttoexcel();
        }




    }
}