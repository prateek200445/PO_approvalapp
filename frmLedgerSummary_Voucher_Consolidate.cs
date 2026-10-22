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

namespace ERP
{
    public partial class frmLedgerSummary_Voucher_Consolidate : Form
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

        #endregion

        #region Main Functions
        public frmLedgerSummary_Voucher_Consolidate()
        {
            InitializeComponent();
            new ComboFilter(comboLedgerName);
            Form_Load();
        }

        private void Form_Load()
        {
            #region Init

            new dateTimePickerRange(dateTimePicker1, dateTimePicker2);
            avd = new AccountVoucherDetails(dataGridView1, 0, comboLedgerName, dateTimePicker1, dateTimePicker2, 3, 4, 5, 1, 2, 6, 7, -1, 2, 3);

            dataGridView1.ContextMenuStrip = AccountCommonFunction.AccountContextMenu.GetCommonVoucherContext(avd);

            drr = new DeleteRestoreRows(dataGridView1);

            drr.BeforeDelete += (dgr, e) =>
                {
                    if (dgr.Cells[2].Style.Padding.Left > 0)
                        e.Cancel = true;
                    else if (dgr.Index < dgr.DataGridView.RowCount - 1 && dgr.DataGridView.Rows[dgr.Index + 1].Cells[2].Style.Padding.Left > 0)
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

                int drIndex = 6;
                int crIndex = 7;

                if (comboBoxCurrency.SelectedIndex != 0)
                {
                    drIndex = 9;
                    crIndex = 10;
                }

                for (int i = 0; i < dataGridView1.Rows.Count; i++)
                {
                    if (dataGridView1.Rows[i].Cells[0].FormattedValue.ToString().Length == 0) continue;
                    double cDR = Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[drIndex].Value);
                    double cCR = Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[crIndex].Value);
                    balance = cDR - cCR + balance;

                    dataGridView1.Rows[i].Cells[12].Value = balance;

                    if (i == 0 && Convert.ToString(dataGridView1.Rows[0].Cells[2].FormattedValue).Equals("Opening Balance"))
                    {
                        oBalance = cDR - cCR;
                        cDR = 0;
                        cCR = 0;
                    }

                    dr += cDR;
                    cr += cCR;
                }
                textDebit.Text = dr.ToString("#,##0.00", IN);
                textCredit.Text = cr.ToString("#,##0.00", IN);
                textOpeningBalance.Text = oBalance.ToString("#,##0.00", IN);

                textClosingBalance.Text = balance.ToString("#,##0.00", IN);
            };
            drr.onRestore += (dgr) =>
            {
                double dr = 0;
                double cr = 0;
                double balance = 0;
                double oBalance = 0;

                int drIndex = 6;
                int crIndex = 7;

                if (comboBoxCurrency.SelectedIndex != 0)
                {
                    drIndex = 9;
                    crIndex = 10;
                }

                for (int i = 0; i < dataGridView1.Rows.Count; i++)
                {
                    if (dataGridView1.Rows[i].Cells[0].FormattedValue.ToString().Length == 0) continue;
                    double cDR = Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[drIndex].Value);
                    double cCR = Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[crIndex].Value);
                    balance = cDR - cCR + balance;

                    dataGridView1.Rows[i].Cells[12].Value = balance;

                    if (i == 0 && Convert.ToString(dataGridView1.Rows[0].Cells[2].FormattedValue).Equals("Opening Balance"))
                    {
                        oBalance = cDR - cCR;
                        cDR = 0;
                        cCR = 0;
                    }

                    dr += cDR;
                    cr += cCR;
                }
                textDebit.Text = dr.ToString("#,##0.00", IN);
                textCredit.Text = cr.ToString("#,##0.00", IN);
                textOpeningBalance.Text = oBalance.ToString("#,##0.00", IN);

                textClosingBalance.Text = balance.ToString("#,##0.00", IN);
            };

            comboBoxCurrency.SelectedIndex = 0;


            dataGridView1.Columns[ColumnDebit.Index].DefaultCellStyle =
              dataGridView1.Columns[ColumnCredit.Index].DefaultCellStyle =
              dataGridView1.Columns[ColumnfDebit.Index].DefaultCellStyle =
              dataGridView1.Columns[ColumnfCredit.Index].DefaultCellStyle =
              FormatStyle.Number2Digit();// FormatStyle.AccountCurrencyFormat();

            dataGridView1.Columns[ColumnClosing.Index].DefaultCellStyle = FormatStyle.AccountCurrencyFormat();
            dataGridView1.Columns[ClosingFC.Index].DefaultCellStyle = FormatStyle.AccountCurrencyFormat();

            dataGridView1.Columns[6].Width =
                dataGridView1.Columns[7].Width =
                dataGridView1.Columns[9].Width =
                dataGridView1.Columns[10].Width = 120;

            dataGridView1.Columns[8].Width = 50;
            dataGridView1.Columns[11].Width = 50;
            dataGridView1.Columns[11].DefaultCellStyle = FormatStyle.Number2Digit();
            dataGridView1.Columns[DtInterest.Index].DefaultCellStyle = FormatStyle.Number2Digit();
            //dataGridView1.Columns[ClosingFC.Index].DefaultCellStyle = FormatStyle.Number2Digit();

            #endregion

            dateTimePicker1.Format = DateTimePickerFormat.Custom;
            dateTimePicker1.CustomFormat = Tools.ToolsCommonVariable.Setting.DateFormat;

            dateTimePicker2.Format = DateTimePickerFormat.Custom;
            dateTimePicker2.CustomFormat = Tools.ToolsCommonVariable.Setting.DateFormat;

            dateTimePicker1.Value = AccountCommonFunction.FinancialYearStart();

            AccountCommonFunction.BindComboCompanyWithGC(comboCompanyName);

            comboCompanyName.Text = "C-" + frmDefaultVale.CompanyName;
        }

        #endregion

        #region Functions

        private void ShowLedgerSummary()
        {

            //ArrayList ArrledgerName = new ArrayList();

            //Database.OpenConnection(Utility.MaterialConnectionString);
            //Database.myreader = Database.GetExecuteReaderCommand("Select ledgername from templedger1");
            //while (Database.myreader.Read())
            //    ArrledgerName.Add(Database.myreader[0].ToString());
            //Database.myreader.Close();
            //Database.Closeconnection();

            //for (int i = 0; i < ArrledgerName.Count; i++)
            //{
            //    comboLedgerName.Text = ArrledgerName[i].ToString();

            //filterData.clearFilter();
            dataGridView1.Rows.Clear();

            #region Old Code
            //            string query_openingbalance = @"
            //select sum(" + (comboBoxCurrency.SelectedIndex==0?"amount":"amount/ExcRate") + @")
            //from vw_LedgerSummary as vw
            //where vw.LedgerName='{0}' and vw.CompanyName='{1}' and vw.Date < '{2:yyyy-MM-dd}'";

            //            if (comboBoxCurrency.SelectedIndex != 0)
            //                query_openingbalance += string.Format(" and vw.Currency='{0}'", comboBoxCurrency.Text);

            //            string query_transaction = string.Format(@"exec sp_LedgerSummary_Final '{0}', '{1:yyyy-MM-dd}', '{2:yyyy-MM-dd}', '{3}', {4}", comboCompanyName.Text, dateTimePicker1.Value, dateTimePicker2.Value, comboLedgerName.Text, comboBoxCurrency.SelectedIndex==0?"NULL":string.Format("'{0}'", comboBoxCurrency.Text));

            //            Database.OpenConnection(Utility.MaterialConnectionString);
            //            Database.GetExecuteReaderCommand(string.Format(query_transaction, comboLedgerName.Text, comboCompanyName.Text, dateTimePicker1.Value, dateTimePicker2.Value));

            //            DataTable dataTable = new DataTable();
            //            dataTable.Load(Database.myreader);

            //            double OpeningBalance = Utility.SafeConvertToDouble(Database.GetExecuteScalarCommand_return(string.Format(query_openingbalance, comboLedgerName.Text, comboCompanyName.Text, dateTimePicker1.Value)));

            //            if (comboBoxCurrency.SelectedIndex != 0)
            //            {
            //                OpeningBalance += AccountCommonFunction.SafeConvertToDouble(Database.ExecuteScalar(@"
            //select SUM(Amount/ExcRate)
            //from vw_BillWiseTransaction vw
            //where vw.LedgerName=@LedgerName
            //	and vw.CompanyName=@CompanyName and vw.VoucherDate < @Date
            //	and vw.Currency=@Currency
            //    and vw.VoucherType in ('OpeningPayble', 'OpeningReceivable')
            //",
            //    new Database.Parameter("@LedgerName", comboLedgerName.Text),
            //    new Database.Parameter("@CompanyName", comboCompanyName.Text),
            //    new Database.Parameter("@Date", dateTimePicker1.Value.ToString("yyyy-MM-dd")),
            //    new Database.Parameter("@Currency", comboBoxCurrency.Text)
            //    ));
            //            }

            //            double Debit = 0;
            //            double fDebit = 0;
            //            double Credit = 0;
            //            double fCredit = 0;
            //            double ClosingBalance = 0;
            //            double fClosingBalance = 0;

            //            ClosingBalance += OpeningBalance;
            //            fClosingBalance += OpeningBalance;

            //            dataGridView1.Rows.Add(
            //                dateTimePicker1.Value.AddDays(-1),
            //                "Opening Balance",
            //                "",
            //                "",
            //                "",
            //                OpeningBalance > 0 ? OpeningBalance : 0,
            //                OpeningBalance <= 0 ? OpeningBalance : 0,
            //                (comboBoxCurrency.SelectedIndex==0?"Rs.":comboBoxCurrency.Text),
            //                OpeningBalance > 0 ? OpeningBalance : 0,
            //                OpeningBalance <= 0 ? OpeningBalance : 0,
            //                1,
            //                (comboBoxCurrency.SelectedIndex==0?ClosingBalance:fClosingBalance)
            //                );

            //            for (int i = 0; i < dataTable.Rows.Count; i++)
            //            {
            //                double a = Utility.SafeConvertToDouble(dataTable.Rows[i].ItemArray[5]);
            //                double excRate = Utility.SafeConvertToDouble(dataTable.Rows[i].ItemArray[7]);
            //                if (a > 0)
            //                {
            //                    Debit += a;
            //                    fDebit += a / excRate;
            //                }
            //                else
            //                {
            //                    Credit += -a;
            //                    fCredit += -a / excRate;
            //                }

            //                ClosingBalance += a;
            //                fClosingBalance += a / excRate;

            //                dataGridView1.Rows.Add(
            //                    dataTable.Rows[i].ItemArray[0],
            //                    dataTable.Rows[i].ItemArray[1],
            //                    dataTable.Rows[i].ItemArray[2],
            //                    dataTable.Rows[i].ItemArray[3],
            //                    dataTable.Rows[i].ItemArray[4],
            //                    (double)dataTable.Rows[i].ItemArray[5] > 0 ? dataTable.Rows[i].ItemArray[5] : 0,
            //                    (double)dataTable.Rows[i].ItemArray[5] <= 0 ? dataTable.Rows[i].ItemArray[5] : 0,
            //                    dataTable.Rows[i].ItemArray[6],
            //                    (double)dataTable.Rows[i].ItemArray[5] > 0 ? (double)dataTable.Rows[i].ItemArray[5] / (double)dataTable.Rows[i].ItemArray[7] : 0,
            //                    (double)dataTable.Rows[i].ItemArray[5] <= 0 ? (double)dataTable.Rows[i].ItemArray[5] / (double)dataTable.Rows[i].ItemArray[7] : 0,
            //                    dataTable.Rows[i].ItemArray[7],
            //                    (comboBoxCurrency.SelectedIndex==0?ClosingBalance:fClosingBalance)
            //                    );
            //            }

            #endregion

            //string query_transaction = string.Format(@"exec sp_LedgerSummary_Final '{0}', '{1:yyyy-MM-dd}', '{2:yyyy-MM-dd}', '{3}', {4}", comboCompanyName.Text, dateTimePicker1.Value, dateTimePicker2.Value, comboLedgerName.Text, comboBoxCurrency.SelectedIndex == 0 ? "NULL" : string.Format("'{0}'", comboBoxCurrency.Text));

            Database.OpenConnection(Utility.MaterialConnectionString);

            string v = Convert.ToString(comboCompanyName.SelectedValue);

            int CompanyType = 0;
            string CompanyName = "";
            int CompanyId = 0;

            if (v.Length < 2)
                throw new ERPErrorException("Comapny Required");

            switch (v[0])
            {
                case 'A':
                    CompanyType = 0;
                    break;
                case 'G':
                    CompanyType = 1;
                    CompanyName = v.Substring(2);
                    break;
                case 'C':
                    CompanyType = 2;
                    CompanyId = AccountCommonFunction.SafeConvertToInt(v.Substring(2));
                    break;
            }

            DataTable dataTable = Database.GetDataTable("sp_ac_LedgerSummary_BankRecoDate", CommandType.StoredProcedure,
                new Database.Parameter("@CompanyType", CompanyType),			// 0 all, 1 group company, 2 company
                new Database.Parameter("@CompanyName", CompanyName),            // Group Company Name
                new Database.Parameter("@CompanyId", CompanyId),				// Company Id
                new Database.Parameter("@LedgerName", comboLedgerName.Text),
                new Database.Parameter("@Currency", comboBoxCurrency.SelectedIndex == 0 ? null : comboBoxCurrency.Text),
                new Database.Parameter("@DateFrom", dateTimePicker1.Value),
                new Database.Parameter("@DateTo", dateTimePicker2.Value),
                new Database.Parameter("@InterestCal", Utility.SafeConvertToDouble(textROI.Text))
                );

            double OpeningBalance = 0;
            double fOpeningBalance = 0;
            double Debit = 0;
            double fDebit = 0;
            double Credit = 0;
            double fCredit = 0;
            double ClosingBalance = 0;
            double fClosingBalance = 0;

            double OpendingnDr = 0;
            double OpendingnCr = 0;

            double OpendingnFDr = 0;
            double OpendingnFCr = 0;

            if (dataTable.Rows.Count > 0)
            {
                DataRow dr = dataTable.Rows[0];
                if (Convert.ToString(dr["VoucherType"]).ToString() == "z")
                    OpeningBalance = AccountCommonFunction.SafeConvertToDouble(dr["amount"]);

                if (comboBoxCurrency.SelectedIndex != 0)
                {
                    double excRate = AccountCommonFunction.SafeConvertToDouble(dr["ExcRate"]);
                    if (excRate != 0)
                        fOpeningBalance = AccountCommonFunction.SafeConvertToDouble(dr["amount"]) / excRate;
                }
                if (fOpeningBalance == 0)
                {
                    OpendingnFDr = AccountCommonFunction.SafeConvertToDouble(dr["FDebit"]);
                    OpendingnFCr = AccountCommonFunction.SafeConvertToDouble(dr["FCredit"]);
                }
                else
                {
                    if (fOpeningBalance >= 0)
                        OpendingnFDr = fOpeningBalance;
                    else
                        OpendingnFCr = fOpeningBalance;
                }

            }


            if (OpeningBalance >= 0)
                OpendingnDr = OpeningBalance;
            else
                OpendingnCr = OpeningBalance;

            //if (fOpeningBalance >= 0)
            //    OpendingnFDr = fOpeningBalance;
            //else
            //    OpendingnFCr = fOpeningBalance;



            double openingnew = OpeningBalance;
            int semicount = 0;

            foreach (DataRow dr in dataTable.Rows)
            {

                double a = AccountCommonFunction.SafeConvertToDouble(dr["amount"]);
                double excRate = AccountCommonFunction.SafeConvertToDouble(dr["ExcRate"]);
                if (a > 0)
                {
                    Debit += a;
                    if (excRate != 0)
                        fDebit += a / excRate;
                }
                else
                {
                    Credit += -a;
                    openingnew = openingnew + a;
                    if (excRate != 0)
                        fCredit += -a / excRate;
                    if (openingnew < 0 && a != 0)
                    {
                        semicount = semicount + 1;
                        #region Adjust Opening against Receipt with specific tenor
                        //if (Utility.MaterialConnectionString.Contains("5115"))
                        //{
                        //    if (semicount > 1)
                        //    {
                        //        if (dr["VoucherType"].ToString() == "Bank Payment")
                        //        {

                        //            int paymentransid = 0;
                        //            Database.myreader = Database.GetExecuteReaderCommand("select transid from  Payment where companyname = '" +
                        //                dr["companyname"].ToString() + "' and PaymentNo =" + dr["voucherno"].ToString() + " and LedgerTo = '" + comboLedgerName.Text + "' and paymentdate = '"
                        //                + Convert.ToDateTime(Convert.ToDateTime(dr["Date"]).ToShortDateString()).ToShortDateString() + "'");
                        //            if (Database.myreader.Read())
                        //                paymentransid = Convert.ToInt32(Database.myreader[0].ToString());
                        //            Database.myreader.Close();


                        //            Database.GetExecuteNonQueryCommand("update Payment set ledgerto = 'OSWAL EXTRUSION LTD-KASEZ'  where companyname = '" +
                        //                dr["companyname"].ToString() + "' and PaymentNo =" + dr["voucherno"].ToString() + " and LedgerTo = '" + comboLedgerName.Text + "' and paymentdate = '"
                        //                + Convert.ToDateTime(dr["Date"]).ToShortDateString() + "'");


                        //            Database.GetExecuteNonQueryCommand("update PaymentAgstRef set partyname = 'OSWAL EXTRUSION LTD-KASEZ'  where companyname = '" +
                        //                dr["companyname"].ToString() + "' and PaymentNo =" + dr["voucherno"].ToString() + " and partyname = '" + comboLedgerName.Text + "' and paymenttransid = "
                        //                + paymentransid);

                        //        }
                        //        if (dr["VoucherType"].ToString() == "Bank Receipt")
                        //        {

                        //            int paymentransid = 0;
                        //            Database.myreader = Database.GetExecuteReaderCommand("select transid from  Paymentreceipt where companyname = '" +
                        //                dr["companyname"].ToString() + "' and PaymentNo =" + dr["voucherno"].ToString() + " and LedgerTo = '" + comboLedgerName.Text + "' and paymentdate = '"
                        //                + Convert.ToDateTime(dr["Date"]).ToShortDateString() + "'");
                        //            if (Database.myreader.Read())
                        //                paymentransid = Convert.ToInt32(Database.myreader[0].ToString());
                        //            Database.myreader.Close();


                        //            Database.GetExecuteNonQueryCommand("update Paymentreceipt set ledgerto = 'OSWAL EXTRUSION LTD-KASEZ'  where companyname = '" +
                        //             dr["companyname"].ToString() + "' and PaymentNo =" + dr["voucherno"].ToString() + " and LedgerTo = '" + comboLedgerName.Text + "' and paymentdate = '"
                        //             + Convert.ToDateTime(dr["Date"]).ToShortDateString() + "'");

                        //            Database.GetExecuteNonQueryCommand("update PaymentReceiptAgstRef set partyname = 'OSWAL EXTRUSION LTD-KASEZ'  where companyname = '" +
                        //                dr["companyname"].ToString() + "' and PaymentNo =" + dr["voucherno"].ToString() + " and partyname = '" + comboLedgerName.Text + "' and receipttransid = "
                        //                + paymentransid);


                        //        }
                        //        if (dr["VoucherType"].ToString() == "Journal")
                        //        {

                        //            int paymentransid = 0;
                        //            Database.myreader = Database.GetExecuteReaderCommand("select transid from  Journal where companyname = '" +
                        //                dr["companyname"].ToString() + "' and journalno =" + dr["voucherno"].ToString() + " and partyname = '" + comboLedgerName.Text + "' and sysdate = '"
                        //                + Convert.ToDateTime(dr["Date"]).ToShortDateString() + "'");
                        //            if (Database.myreader.Read())
                        //                paymentransid = Convert.ToInt32(Database.myreader[0].ToString());
                        //            Database.myreader.Close();


                        //            Database.GetExecuteNonQueryCommand("update Journal set partyname = 'OSWAL EXTRUSION LTD-KASEZ',CrPartyName =  'OSWAL EXTRUSION LTD-KASEZ'  where companyname = '" +
                        //             dr["companyname"].ToString() + "' and journalno =" + dr["voucherno"].ToString() + " and partyname = '" + comboLedgerName.Text + "' and sysdate = '"
                        //             + Convert.ToDateTime(dr["Date"]).ToShortDateString() + "'");

                        //            Database.GetExecuteNonQueryCommand("update Journalitem set partyname = 'OSWAL EXTRUSION LTD-KASEZ'  where companyname = '" +
                        //                dr["companyname"].ToString() + "' and journalno =" + dr["voucherno"].ToString() + " and partyname = '" + comboLedgerName.Text + "' and journaltransid = "
                        //                + paymentransid);

                        //        }

                        //    }
                        //}
                        #endregion
                    }
                }

                ClosingBalance += a;

                if (excRate != 0)
                {
                    if (excRate != 1)
                        fClosingBalance += a / excRate;
                    else
                        fClosingBalance += AccountCommonFunction.SafeConvertToDouble(dr["fClosingBalance"]);
                }

                string sCurrency = Convert.ToString(dr["Currency"]);

                if (sCurrency.ToLower() == "rs")
                    sCurrency = "Rs.";

                object ofDebit = 0;
                object ofCredit = 0;

                if (excRate != 0)
                {
                    ofDebit = a > 0 ? a / excRate : 0;
                    ofCredit = a <= 0 ? a / excRate : 0;
                }

                if (sCurrency == "Rs.")
                {
                    if (dr["VoucherType"].ToString().ToLower() == "z")
                    {
                        ofDebit = OpendingnFDr; //null;
                        ofCredit = OpendingnFCr;// null;
                    }
                    else
                    {
                        ofDebit = null;
                        ofCredit = null;
                    }
                }

                DateTime dt1 = Convert.ToDateTime(dateTimePicker2.Value);
                DateTime dt2 = Convert.ToDateTime(Convert.ToDateTime(dr["Date"]).ToShortDateString());

                #region as per JInal

                //////int Days = (dt1 - dt2).Days;
                //////Days = Days + 1;
                ////int Days = Convert.ToInt32(dr["DaysG"]);
                ////double interest = 0;
                //////if (Debit > 0)
                //////    interest = (Debit * Days * 12.95) / 36500;
                //////else
                //////    interest = -(Credit * Days * 12.95) / 36500;

                //////if (textROI.Text == "")
                //////    textROI.Text = "0";
                //////interest = (a * Days * Convert.ToDouble(textROI.Text)) / 36500;

                ////interest = Utility.SafeConvertToDouble(dr["InterstCal"]);
                #endregion

                int Days = (dt1 - dt2).Days;
                Days = Days + 1;

                double interest = 0;
                //if (Debit > 0)
                //    interest = (Debit * Days * 12.95) / 36500;
                //else
                //    interest = -(Credit * Days * 12.95) / 36500;

                if (textROI.Text == "")
                    textROI.Text = "0";
                interest = (a * Days * Convert.ToDouble(textROI.Text)) / 36500;



                dataGridView1.Rows.Add(
                    dr["CompanyName"],
                    Convert.ToDateTime(dr["Date"]),
                    dr["LedgerName"],
                    dr["VoucherType"],
                    dr["voucherno"].ToString(),
                    dr["VoucherRef"],
                     a > 0 ? a : 0,
                    a <= 0 ? Math.Abs(a) : 0,
                    sCurrency == "Rs." ? null : sCurrency,
                    ofDebit,
                    Math.Abs(AccountCommonFunction.SafeConvertToDouble(ofCredit)),
                    (excRate == 0 || Math.Round(a / excRate, 2) == 0 ? 0 : excRate),
                    ClosingBalance,
                    (comboBoxCurrency.SelectedIndex == 0 ? AccountCommonFunction.SafeConvertToDouble(dr["fClosingBalance"]) : fClosingBalance), Days, interest
                    );
                //(comboBoxCurrency.SelectedIndex == 0 ? ClosingBalance : fClosingBalance)

                if (dr == dataTable.Rows[dataTable.Rows.Count - 1] && Convert.ToString(dr["LedgerName"]).ToLower() == "unadujsted forex gain/loss")
                {
                    DataGridViewRow drr = dataGridView1.Rows[dataGridView1.Rows.Count - 1];

                    drr.DefaultCellStyle.ForeColor = Color.Blue;
                    drr.DefaultCellStyle.Font = new System.Drawing.Font("Tahoma", 8.25F, FontStyle.Bold | FontStyle.Italic, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                }

                if (dataTable.Columns.Contains("ApprovalStatus"))
                {
                    if (dr["ApprovalStatus"].ToString() == "0")
                    {
                        dataGridView1.Rows[dataGridView1.Rows.Count - 1].DefaultCellStyle.BackColor = System.Drawing.Color.Orange;
                    }
                    if (dr["ApprovalStatus"].ToString() == "-1")
                    {
                        dataGridView1.Rows[dataGridView1.Rows.Count - 1].DefaultCellStyle.BackColor = System.Drawing.Color.Tomato;
                        dataGridView1.Rows[dataGridView1.Rows.Count - 1].DefaultCellStyle.ForeColor = System.Drawing.Color.White;
                    }
                }

            }


            Debit = Debit - Math.Abs(OpendingnDr);
            Credit = Credit - Math.Abs(OpendingnCr);
            fDebit = fDebit - Math.Abs(OpendingnFDr);
            fCredit = fCredit - Math.Abs(OpendingnFCr);

            textOpeningBalance.Text = comboBoxCurrency.SelectedIndex == 0 ? OpeningBalance.ToString("#,##0.00", IN) : fOpeningBalance.ToString(" #,##0.00 " + comboBoxCurrency.Text, IN);
            textDebit.Text = comboBoxCurrency.SelectedIndex == 0 ? (Debit).ToString("#,##0.00", IN) : (fDebit).ToString("#,##0.00 " + comboBoxCurrency.Text, IN);
            textCredit.Text = comboBoxCurrency.SelectedIndex == 0 ? (Credit).ToString("#,##0.00", IN) : (fCredit).ToString("#,##0.00 " + comboBoxCurrency.Text, IN);
            textClosingBalance.Text = comboBoxCurrency.SelectedIndex == 0 ? (OpeningBalance + Debit - Credit).ToString("#,##0.00", IN) : (fOpeningBalance + fDebit - fCredit).ToString("#,##0.00 " + comboBoxCurrency.Text, IN);

            //   }
        }

        private string GetCompanyName()
        {
            string v = Convert.ToString(comboCompanyName.SelectedValue);

            int CompanyType = 0;
            string CompanyName = "";
            int CompanyId = 0;

            if (v.Length < 2)
                throw new ERPErrorException("Comapny Required");

            switch (v[0])
            {
                case 'A':
                    CompanyType = 0;
                    break;
                case 'G':
                    CompanyType = 1;
                    CompanyName = v.Substring(2);
                    break;
                case 'C':
                    CompanyType = 2;
                    CompanyId = AccountCommonFunction.SafeConvertToInt(v.Substring(2));
                    break;
            }

            Database.OpenConnection(Utility.MaterialConnectionString);

            DataTable dt = Database.GetDataTable(@"
select CompanyName
from ledgermaster l with(Nolock)
inner join FactoryInfo fi with(Nolock) on l.CompanyName=fi.Name
where LedgerName=@LedgerName
and fi.srno=CASE WHEN @CompanyType=2 THEN @CompanyId ELSE fi.srno END
and fi.GroupName=CASE WHEN @CompanyType=1 THEN @CompanyName ELSE fi.GroupName END
",
                new Database.Parameter("@LedgerName", comboLedgerName.Text),
                new Database.Parameter("@CompanyType", CompanyType),
                new Database.Parameter("@CompanyId", CompanyId),
                new Database.Parameter("@CompanyName", CompanyName)
                );

            List<string> d = new List<string>();
            foreach (DataRow dr in dt.Rows)
            {
                d.Add(Convert.ToString(dr["CompanyName"]));
            }

            return ChooseOption.ChooseString(d);
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
                comboLedgerName.DataSource = AccountCommonFunction.GetLedgerListGC(Convert.ToString(comboCompanyName.SelectedValue));
                comboLedgerName.ValueMember = AccountCommonVariable.FieldName.LedgerId;
                comboLedgerName.DisplayMember = AccountCommonVariable.FieldName.LedgerName;
                comboLedgerName.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                ex.Message.ToString();
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            string CompanyName = this.GetCompanyName();

            if (String.IsNullOrEmpty(CompanyName))
                return;

            (new frmContactInfo(comboLedgerName.Text, CompanyName)).ShowDialog();
        }
        private void button5_ShowAll(object sender, EventArgs e)
        {
            if (avd.ExpandAll()) (sender as Button).Text = "Hide All";
            else (sender as Button).Text = "Show All";
        }

        private void button7_Click(object sender, EventArgs e)
        {
            string CompanyName = this.GetCompanyName();

            if (String.IsNullOrEmpty(CompanyName))
                return;

            TabClass.OpenForm(new FrmLedgerSummaryMnthWise(CompanyName, comboLedgerName.Text, dateTimePicker1.Value, dateTimePicker2.Value));
        }

        private void button8_Click(object sender, EventArgs e)
        {
            string CompanyName = this.GetCompanyName();

            if (String.IsNullOrEmpty(CompanyName))
                return;

            (new frmAddUpdateLedger(CompanyName, comboLedgerName.Text)).ShowDialog();
        }

        private void button10_Click(object sender, EventArgs e)
        {
            string CompanyName = this.GetCompanyName();

            if (String.IsNullOrEmpty(CompanyName))
                return;

            DateTime DateTo = dateTimePicker2.Value;
            FrmBillWiseAgstRefReport.isbillwiseReport = true;
            FrmBillWiseAgstRefReport.dateto = DateTo;
            FrmBillWiseAgstRefReport.companyname = CompanyName;
            FrmBillWiseAgstRefReport.ledgername = comboLedgerName.Text;
            FrmBillWiseAgstRefReport.Currency = comboBoxCurrency.Text;
            TabClass.OpenForm(new FrmBillWiseAgstRefReport());
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            contextMenuStrip1.Show(toolStrip1, new Point(0, toolStrip1.Height - 5));
        }

        private void printLedgerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            frmPrintLedgerSummary.ledgername = comboLedgerName.Text;
            frmPrintLedgerSummary.cmpnyname = comboCompanyName.Text;
            TabClass.OpenForm(new frmPrintLedgerSummary());
        }

        private void columnerReportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (comboLedgerName.Text.Length == 0)
            {
                MessageBox.Show("Select Proper Ledger Name");
                return;
            }

            frmColmnerGrid frm = new frmColmnerGrid(dateTimePicker1.Value, dateTimePicker2.Value,
                comboCompanyName.Text, comboLedgerName.Text, drr.DeletedRows);
            TabClass.OpenForm(frm);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            filterData = new filter.filterData(dataGridView1);
            ShowLedgerSummary();
            Dtdays.Visible = chkInt.Checked;
            DtInterest.Visible = chkInt.Checked;
            ParticularsNameChange(); // added by manish on 22nd aug 2022
        }

        private void ParticularsNameChange()
        {
            for (int i = 0; i < dataGridView1.Rows.Count ; i++)
            {
                if(dataGridView1.Rows[i].Cells[2].Value.ToString() == comboLedgerName.Text)
                {
                    Database.OpenConnection(Utility.MaterialConnectionString);
                    Database.myreader = Database.GetExecuteReaderCommand("SELECT case when isnull(Debit,0) > 0 then crpartyname else drpartyname end FROM Journal WITH(NOLOCK) WHERE   JournalNo= "
                         + dataGridView1.Rows[i].Cells[4].Value.ToString() + "and  COMPANYNAME='" + dataGridView1.Rows[i].Cells[0].Value.ToString()
                         + "' AND sysdate= '" + dataGridView1.Rows[i].Cells[1].Value.ToString() + "'  AND 	PartyName = '" + comboLedgerName.Text + "'");
                    if (Database.myreader.Read())
                        dataGridView1.Rows[i].Cells[2].Value = Database.myreader[0].ToString();
                    Database.myreader.Close();
                    Database.Closeconnection();

                }

            }
        }

        private void chknarration_CheckedChanged(object sender, EventArgs e)
        {
            avd.isNarrationOnly = (sender as CheckBox).Checked;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            //if ((sender as CheckBox).Checked)
            //{
            //    dataGridView1.Columns[6].Visible =
            //        dataGridView1.Columns[7].Visible = false;

            //    dataGridView1.Columns[8].Visible =
            //        dataGridView1.Columns[9].Visible =
            //        dataGridView1.Columns[10].Visible =
            //        dataGridView1.Columns[11].Visible = true;

            //    dataGridView1.Columns[2].Width = dataGridView1.Width - 1000;
            //}
            //else
            //{
            //    dataGridView1.Columns[6].Visible =
            //        dataGridView1.Columns[7].Visible = true;

            //    dataGridView1.Columns[8].Visible =
            //        dataGridView1.Columns[9].Visible =
            //        dataGridView1.Columns[10].Visible =
            //        dataGridView1.Columns[11].Visible = false;
            //    dataGridView1.Columns[2].Width = dataGridView1.Width - 900;
            //}
        }

        private void dataGridView1_Resize(object sender, EventArgs e)
        {
            //if (checkBox1.Checked)
            //{
            //    dataGridView1.Columns[2].Width = dataGridView1.Width - 1000;
            //}
            //else
            //{
            //    dataGridView1.Columns[2].Width = dataGridView1.Width - 900;
            //}
        }

        #endregion

        private void ledgerConfirmationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (Database.OpenConnection(Utility.MaterialConnectionString))
                {
                    int CompanyType = 0;
                    string companyname = "";
                    string v = Convert.ToString(comboCompanyName.SelectedValue);
                    int CompanyId = AccountCommonFunction.SafeConvertToInt(v.Substring(2));
                    switch (v[0])
                    {

                        case 'A':
                            CompanyType = 0;
                            return;
                        case 'G':
                            CompanyType = 1;
                            companyname = v.Substring(2);
                            Database.GetExecuteNonQueryCommand("Delete from LedgerSummaryBalTemp where ledgername = '" + comboLedgerName.Text
                                + "' and username='" + FrmMain.UserName + "'");
                            break;
                        case 'C':
                            CompanyType = 2;
                            CompanyId = AccountCommonFunction.SafeConvertToInt(v.Substring(2));
                            companyname = Convert.ToString(comboCompanyName.Text).Substring(2);
                            Database.GetExecuteNonQueryCommand("Delete from LedgerSummaryBalTemp where ledgername = '" + comboLedgerName.Text + "' and companyname='"
                                     + companyname + "' and username='" + FrmMain.UserName + "'");
                            break;

                    }

                    if (Database.GetExecuteNonQueryCommand("insert into LedgerSummaryBalTemp values('" + companyname
                 + "','" + comboLedgerName.Text + "'," + Utility.SafeConvertToDouble(textOpeningBalance.Text.Replace(comboBoxCurrency.Text, ""))
                 + "," + Convert.ToDouble(textClosingBalance.Text.ToString().Replace(comboBoxCurrency.Text, "")) + ",'" + FrmMain.UserName + "','"
                 + dateTimePicker1.Value.ToString("MM/dd/yyyy") + "','"
                 + dateTimePicker2.Value.ToString("MM/dd/yyyy")
                 + "'," + Convert.ToDouble(textDebit.Text.Replace(comboBoxCurrency.Text, "")) + "," + Convert.ToDouble(textCredit.Text.Replace(comboBoxCurrency.Text, "")) + ")"))
                    {
                        //frmPrintLedgerSummary.dtLedgerReport = dt;
                        //frmPrintLedgerSummary.ledgername = comboLedgerName.Text;
                        //frmPrintLedgerSummary.cmpnyname = comboCompanyName.Text;

                        TabClass.OpenForm(new frmConfirmationLedgerSummary(companyname, comboLedgerName.Text, dateTimePicker1.Value, dateTimePicker2.Value, comboBoxCurrency.Text, CompanyType));
                        //TabClass.OpenForm(new frmPrintLedgerSummary());
                    }
                }

            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);

            }
        }

        private void chkInt_CheckedChanged(object sender, EventArgs e)
        {
            Dtdays.Visible = chkInt.Checked;
            DtInterest.Visible = chkInt.Checked;
        }

        private void dataGridView1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            try
            {
                if (dataGridView1.CurrentCell.ColumnIndex == dataGridView1.Columns["NosAttch"].Index)
                {
                    int aat = Convert.ToInt32(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[colAttach.Index].Value);
                    if (aat > 0)
                    {
                        string yr = Convert.ToString(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells["yr"].Value);
                        string voucherno = Convert.ToString(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells["VoucherNo"].Value);
                        string RefType = Convert.ToString(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells["RefType"].Value);
                        string companyname = Convert.ToString(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells["companyname"].Value);
                        Tools.DMSManager.OpenReference(RefType, string.Format("{0}/{1}/" + RefType + "/{2}",
                           AccountCommonFunction.GetCompanyCode(companyname),
                           yr, voucherno));
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Problem with Open Attachment" + ex.Message);
            }
        }

        private void btnSummary_Click(object sender, EventArgs e)
        {

        }

        private void summaryBreakupToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (comboLedgerName.Text.Length == 0)
                {
                    MessageBox.Show("Please Select Ledger Name");
                    return;
                }
                string v = Convert.ToString(comboCompanyName.SelectedValue);

                int CompanyType = 0;
                string CompanyName = "";
                int CompanyId = 0;

                if (v.Length < 2)
                    throw new ERPErrorException("Comapny Required");

                switch (v[0])
                {
                    case 'A':
                        CompanyType = 0;
                        break;
                    case 'G':
                        CompanyType = 1;
                        CompanyName = v.Substring(2);
                        break;
                    case 'C':
                        CompanyType = 2;
                        CompanyId = AccountCommonFunction.SafeConvertToInt(v.Substring(2));
                        break;
                }

                //DataTable dataTable = Database.GetDataTable(
                //    "Select * from [fn_Ledger_Closing](" + CompanyType + ",'" + CompanyName + "'," + CompanyId + ",'" +
                //    comboLedgerName.Text + "','" + dateTimePicker2.Value + "'");

                frmExportExcel.ConnectionString = Utility.MaterialConnectionString;
                frmExportExcel.CommandString = "Select * from [dbo].[fn_Ledger_Closing](" + CompanyType + ",'" + CompanyName + "'," + CompanyId + ",'" +
                     comboLedgerName.Text + "','" + dateTimePicker1.Value.ToString("yyyy-MM-dd") + "','" + dateTimePicker2.Value.ToString("yyyy-MM-dd") + "')";
                frmExportExcel.TableName = "fn_Ledger_Closing";

                TabClass.OpenForm(new frmExportExcel(), sender);



            }
            catch (Exception ex)
            {
                MessageBox.Show("Problem with Summary Report" + ex.Message);
            }
        }

        private void menuAccount1_Load(object sender, EventArgs e)
        {

        }

    }
}