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
    public partial class FrmBillWiseHighAgstRefReport : ERPTabPage
    {
        #region Variables
        double totalAmntForPrint = 0;
        public static DateTime datefrom = Utility.nullDate;
        public static DateTime dateto = Utility.nullDate;
        public static bool isbillwiseReport = false;
        public static string companyname = "";
        public static string ledgername = "";
        public static string Currency = "";
        public static string Representative = "";
        CultureInfo us = new CultureInfo("en-US");
        CultureInfo IN = new CultureInfo("gu-IN");

        DataGridViewRow dra = null;

        private bool Expanded = false;

        #endregion

        #region Main Function
        public FrmBillWiseHighAgstRefReport()
        {
            InitializeComponent();
            Form_Load();
        }

        private void Form_Load()
        {
            new ComboFilter(comboLedgerName);
            new filter.filterData(dataGridView1);
            new filter.filterData(dgTotalCrDr);

            dra = (DataGridViewRow)dataGridView1.RowTemplate.Clone();
            DataGridViewCell[] dataGridViewCells = new DataGridViewCell[dataGridView1.Columns.Count];
            for (int i = 0; i < dataGridView1.Columns.Count; i++) { dataGridViewCells[i] = (DataGridViewCell)dataGridView1.Columns[i].CellTemplate.Clone(); }
            dra.Cells.AddRange(dataGridViewCells);

            try
            {

                AccountCommonFunction.BindComboCompanyMulti(comboCompanyName, true);



                dateTimePicker1.Value = datefrom;
                dateTimePicker2.Value = dateto;
                comboCompanyName.Text = companyname;
                comboLedgerName.Text = ledgername;

                BindGrid();
                datefrom = Utility.nullDate;
                dateto = Utility.nullDate;
                isbillwiseReport = false;
                isbillwiseReport = false;

                dataGridView1.Focus();
            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
            }
        }

        #endregion

        #region Functions
        void BindGrid()
        {
            DataSet dataset1 = new DataSet();
            try
            {

                if (Database.OpenConnection_Adapter(Utility.MaterialConnectionString))
                {
                    Database.mycommand.Parameters.Clear();
                    Database.mycommand.CommandText = "SP_Ledger_OutstandnigMax";
                    Database.mycommand.CommandType = CommandType.StoredProcedure;
                    Database.mycommand.Connection = Database.myconn;
                    Database.mycommand.Parameters.AddWithValue("@FROMDATE", dateTimePicker1.Value.ToString("yyyy-MM-dd"));
                    Database.mycommand.Parameters.AddWithValue("@TODATE", dateTimePicker2.Value.ToString("yyyy-MM-dd"));
                    Database.mycommand.Parameters.AddWithValue("@companyname", (comboCompanyName.Text == "* (All)" ? "" : comboCompanyName.Text));
                    Database.mycommand.Parameters.AddWithValue("@LedgerName", comboLedgerName.Text);


                    Database.myadapter = Database.GetAdapterCommand_SqlCommand();

                    Database.myadapter.Fill(dataset1, " SP_Ledger_OutstandnigMax '" + dateTimePicker1.Value.ToString("yyyy-MM-dd") + "','" +
                        dateTimePicker2.Value.ToString("yyyy-MM-dd") + "',"
                    + (comboCompanyName.Text == "* (All)" ? "" : comboCompanyName.Text) + "','" + comboLedgerName.Text + "'");

                    dataGridView1.Rows.Clear();

                    List<DataGridViewRow> dRows = new List<DataGridViewRow>();

                    for (int i = 0; i < dataset1.Tables[0].Rows.Count; i++)
                    {
                        DataGridViewRow dr = (DataGridViewRow)dra.Clone();
                        dr.Cells[0].Value = dataset1.Tables[0].Rows[i]["LedgerName"];
                        dr.Cells[1].Value = dataset1.Tables[0].Rows[i]["OutStand"];
                        dr.Cells[2].Value = Convert.ToDateTime(dataset1.Tables[0].Rows[i]["Date"]).ToString("dd-MMM-yyyy");

                        dRows.Add(dr);
                    }
                    dataGridView1.Rows.AddRange(dRows.ToArray());
                }

                //if (comboCompanyName.SelectedIndex == 0)
                //    dataGridView1.Columns[0].Visible = true;
                //else
                //    dataGridView1.Columns[0].Visible = false;
            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
            }
        }

        private void ViewSummary()
        {
            double totalsumCr = 0;
            double totalsumDr = 0;
            foreach (DataGridViewRow row in this.dataGridView1.Rows)
            {
                if (Utility.SafeConvertToDouble(row.Cells[5].Value) >= 0)
                    totalsumDr += Utility.SafeConvertToDouble(row.Cells[5].Value);
                else
                    totalsumCr += Utility.SafeConvertToDouble(row.Cells[5].Value);
            }


        }

        private void UpdateRowClone()
        {
            DataGridView dgv = dataGridView1;
            dra = (DataGridViewRow)dgv.RowTemplate.Clone();
            DataGridViewCell[] dataGridViewCells = new DataGridViewCell[dgv.Columns.Count];
            for (int i = 0; i < dgv.Columns.Count; i++) { dataGridViewCells[i] = (DataGridViewCell)dgv.Columns[i].CellTemplate.Clone(); }
            dra.Cells.AddRange(dataGridViewCells);

            //dra.Cells[0].Style.Padding = new Padding(16, 0, 0, 0);

            //dra.DefaultCellStyle.ForeColor = Color.Green;
        }

        private int ExpandDetails(int rIndex)
        {
            DataGridView dgv = dataGridView1;
            int rCount = dgv.RowCount;
            if (dgv.AllowUserToAddRows) rCount--;
            if (rIndex < 0 || rIndex >= rCount) return 0;

            if (dgv.Rows[rIndex].Cells[3].Style.Padding.Left > 0) return 0;      // return if Current Row is Details Row

            if (rIndex < rCount - 1 && dgv.Rows[rIndex + 1].Cells[3].Style.Padding.Left > 0)
            {
                // Removing Rows
                int i = 0;
                while (rIndex < dgv.RowCount - 1 && dgv.Rows[rIndex + 1].Cells[3].Style.Padding.Left > 0)
                {
                    dgv.Rows.RemoveAt(rIndex + 1);
                    i--;
                }
                return i;
            }
            else
            {
                UpdateRowClone();

                string BillNo = Convert.ToString(dgv.Rows[rIndex].Cells[3].Value);
                //if (BillNo.Length == 0) return 0;

                //object oDate = dateTimePicker2.Value;
                //if (oDate == null || !(oDate is DateTime)) return 0;

                try
                {
                    string companyname = comboCompanyName.Text;
                    if (comboCompanyName.SelectedIndex == 0)
                        companyname = dataGridView1.Rows[rIndex].Cells[0].Value.ToString();

                    string LedgerName = comboLedgerName.Text;
                    if (comboLedgerName.Text.Length == 0)
                        LedgerName = dataGridView1.Rows[rIndex].Cells[2].Value.ToString();

                    DataFormat.BillTrack billTrack = new DataFormat.BillTrack(companyname, LedgerName, BillNo, dateTimePicker2.Value);

                    Font NormalFont = new Font(dataGridView1.Font, FontStyle.Regular);
                    int i = 0;
                    foreach (DataFormat.BillTrack.BillDetail billDetail in billTrack.billDetails)
                    {
                        dataGridView1.Rows.Insert(rIndex + i + 1,
                            null, billDetail.VoucherNo,
                            billDetail.VoucherType,
                            billDetail.VoucherDate,
                            billDetail.VoucherNo,
                            billDetail.Amount);

                        // show foreign amount if available
                        if (billDetail.ExcRate != 1 && billDetail.ExcRate != 0)
                        {
                            dataGridView1.Rows[rIndex + i + 1].Cells[6].Value = billDetail.Amount / billDetail.ExcRate;
                            dataGridView1.Rows[rIndex + i + 1].Cells[6].Style = FormatStyle.AccountCurrencyFormatWithCurrency(billDetail.Currency);
                        }

                        dataGridView1.Rows[rIndex + i + 1].Cells[2].Style.Padding = new System.Windows.Forms.Padding(16, 0, 0, 0);
                        dataGridView1.Rows[rIndex + i + 1].DefaultCellStyle.ForeColor = Color.Green;
                        dataGridView1.Rows[rIndex + i + 1].DefaultCellStyle.Font = NormalFont;
                        i++;
                    }

                    if (billTrack.ForexDetail.Amount != 0)
                    {
                        dataGridView1.Rows.Insert(rIndex + i + 1,
                            null, billTrack.ForexDetail.VoucherNo,
                            billTrack.ForexDetail.VoucherType,
                            billTrack.ForexDetail.VoucherDate,
                            billTrack.ForexDetail.VoucherNo,
                            billTrack.ForexDetail.Amount);
                        dataGridView1.Rows[rIndex + i + 1].Cells[2].Style.Padding = new System.Windows.Forms.Padding(16, 0, 0, 0);
                        dataGridView1.Rows[rIndex + i + 1].DefaultCellStyle.ForeColor = Color.Green;
                        dataGridView1.Rows[rIndex + i + 1].DefaultCellStyle.Font = NormalFont;
                        i++;
                    }

                    return i;
                }
                catch (Exception ex)
                {
                    ErrorMessageBox.Show(ex);
                }

                return 0;

                //if (!Database.OpenConnection(Utility.MaterialConnectionString)) return 0;

                //List<DataGridViewRow> dgvr = new List<DataGridViewRow>();
                //DataGridViewRow dr;

                //Database.myreader = Database.GetExecuteReaderCommand(string.Format("SELECT VoucherType, Date, StoreInwardNo, BillAmount, Type, SupplierInvNo FROM vw_AllAgstRefBillWise_Detailed WHERE SupplierName='{0}' AND CompanyName='{1}' AND SupplierInvNo='{2}'and date<='{3}' ORDER BY Date ASC", comboLedgerName.Text, comboCompanyName.Text, BillNo, ((DateTime)oDate).ToString("yyyy-MM-dd")));

                //Font NormalFont = new Font(dataGridView1.Font, FontStyle.Regular);
                //int i = 0;
                //double dSum = 0;
                //while (Database.myreader.Read())
                //{
                //    double d = Utility.SafeConvertToDouble(Database.myreader["BillAmount"]);
                //    if (Convert.ToString(Database.myreader["Type"]).ToLower() == "cr") d = -d;
                //    dSum += d;
                //    dataGridView1.Rows.Insert(rIndex + i + 1,
                //        null, Database.myreader["SupplierInvNo"],
                //        Database.myreader["VoucherType"],
                //        Database.myreader["Date"],
                //        Database.myreader["StoreInwardNo"],
                //        d);
                //    dataGridView1.Rows[rIndex + i + 1].Cells[2].Style.Padding = new System.Windows.Forms.Padding(16, 0, 0, 0);
                //    dataGridView1.Rows[rIndex + i + 1].DefaultCellStyle.ForeColor = Color.Green;
                //    dataGridView1.Rows[rIndex + i + 1].DefaultCellStyle.Font = NormalFont;
                //    i++;
                //}
                //double dForex = Utility.SafeConvertToDouble(dgv.Rows[rIndex].Cells[4].Value)-dSum;
                //if (dForex != 0)
                //{
                //    dataGridView1.Rows.Insert(rIndex + i + 1,
                //        null, null,
                //        "Forex Gain/Loss",
                //        null,
                //        null,
                //        dForex);
                //    dataGridView1.Rows[rIndex + i + 1].Cells[2].Style.Padding = new System.Windows.Forms.Padding(16, 0, 0, 0);
                //    dataGridView1.Rows[rIndex + i + 1].DefaultCellStyle.ForeColor = Color.Green;
                //    dataGridView1.Rows[rIndex + i + 1].DefaultCellStyle.Font = NormalFont;
                //}
                //Database.Closeconnection();
                //return i;
            }
        }

        private void OpenEntryForm(int rIndex)
        {
            DataGridView dgv = dataGridView1;
            int DetailsColumnIndex = 2;
            int voucherTypeIndex = 2;
            int DateColumnIndex = 3;
            int voucherNoIndex = 1;
            int voucherRefIndex = 4;
            DateTimePicker dt1 = dateTimePicker1;
            DateTimePicker dt2 = dateTimePicker2;

            int rCount = dgv.RowCount;
            if (dgv.AllowUserToAddRows) rCount--;
            if (rIndex < 0 || rIndex >= rCount) return;

            if (dgv.Rows[rIndex].Cells[DetailsColumnIndex].Style.Padding.Left == 0)
            {
                return;
            }

            if (string.Compare(dgv.Rows[rIndex].Cells[DetailsColumnIndex].FormattedValue.ToString().Trim(), "Forex Gain/Loss", true) == 0)
            {
                return;
                //FrmForexGainLoss.isbillwiseReport = true;
                //FrmForexGainLoss.ledgername = comboLedgerName.Text;
                //FrmForexGainLoss.companyname = comboCompanyName.Text;
                //FrmForexGainLoss.dateForm = dt1.Value;
                //FrmForexGainLoss.dateto = dt2.Value;
                //TabClass.OpenForm(new FrmForexGainLoss());
            }
            else if (Convert.ToString(dgv.Rows[rIndex].Cells[voucherTypeIndex].Value) == "Other Sales")
            {
                object oDate = dgv.Rows[rIndex].Cells[DateColumnIndex].Value;
                if (oDate == null || !(oDate is DateTime)) return;


                frmSalesVoucher_Blank.InvNo = Convert.ToString(dgv.Rows[rIndex].Cells[voucherNoIndex].Value).Trim();
                frmSalesVoucher_Blank.InvDate = (DateTime)oDate;
                frmSalesVoucher_Blank.InvType = Convert.ToString(dgv.Rows[rIndex].Cells[voucherTypeIndex].Value).Trim();
                frmSalesVoucher_Blank.Invyear = frmDefaultVale.Year;
                frmSalesVoucher_Blank.CompanyName = comboCompanyName.Text;
                frmSalesVoucher_Blank.IsSalesRegister = true;
                TabClass.OpenForm(new frmSalesVoucher_Blank());
            }
            else
            {
                string voucherType = Convert.ToString(dgv.Rows[rIndex].Cells[voucherTypeIndex].Value);
                if (voucherType.Length == 0) return;

                object oDate = dgv.Rows[rIndex].Cells[DateColumnIndex].Value;
                if (oDate == null || !(oDate is DateTime)) return;

                if (!Database.OpenConnection(Utility.MaterialConnectionString)) return;

                string narration = "";
                List<DataGridViewRow> dgvr = new List<DataGridViewRow>();
                DataGridViewRow dr;

                string type = "";
                switch (Convert.ToString(Database.GetExecuteScalarCommand_return(string.Format("SELECT TOP 1 groupname FROM vouchertype WHERE vouchertypename = '{0}'", voucherType))).ToLower())
                {
                    case "receipt":
                        frmReceiptMulti.ReceiptNo = Convert.ToString(dgv.Rows[rIndex].Cells[voucherNoIndex].Value).Trim();
                        frmReceiptMulti.IsReceipt = true;
                        frmReceiptMulti.Company = comboCompanyName.Text;
                        frmReceiptMulti.currentDate = (DateTime)oDate;
                        TabClass.OpenForm(new frmReceiptMulti());
                        break;
                    case "journal":
                        frmJounral.JournalNo = Convert.ToString(dgv.Rows[rIndex].Cells[voucherNoIndex].Value).Trim();
                        frmJounral.IsJournal = true;
                        frmJounral.Company = comboCompanyName.Text;
                        frmJounral.currentDate = (DateTime)oDate;
                        TabClass.OpenForm(new frmJounral());
                        break;
                    case "purchase":
                        string pono = "";
                        Database.OpenConnection(Utility.MaterialConnectionString);
                        Database.myreader = Database.GetExecuteReaderCommand("select Storeinwardno From purchasevoucher where GeneralPurchaseNo = '"
                         + Convert.ToString(dgv.Rows[rIndex].Cells[voucherRefIndex].Value).Trim() + "' and sysdate = '"
                        + ((DateTime)oDate).ToString("yyyy-MM-dd") + "'");

                        if (Database.myreader.Read())
                            pono = Database.myreader[0].ToString();
                        Database.myreader.Close();
                        if (frmPurchaseVoucher.PurchaseNo.Contains("OTH"))
                        {
                            frmPurchaseVoucher_Blank.PurchaseNo = pono;
                            frmPurchaseVoucher_Blank.IsPurchase = true;
                            frmPurchaseVoucher_Blank.Company = comboCompanyName.Text;
                            frmPurchaseVoucher_Blank.currentDate = (DateTime)oDate;
                            TabClass.OpenForm(new frmPurchaseVoucher_Blank());
                        }
                        else
                        {
                            frmPurchaseVoucher.PurchaseNo = pono;
                            frmPurchaseVoucher.IsPurchase = true;
                            frmPurchaseVoucher.Company = comboCompanyName.Text;
                            frmPurchaseVoucher.currentDate = (DateTime)oDate;
                            TabClass.OpenForm(new frmPurchaseVoucher());
                        }
                        break;
                    case "payment":
                        frmPaymentMulti.PaymentNo = Convert.ToString(dgv.Rows[rIndex].Cells[voucherNoIndex].Value).Trim();
                        frmPaymentMulti.companyname = comboCompanyName.Text;
                        frmPaymentMulti.IsPayment = true;
                        frmPaymentMulti.currentDate = (DateTime)oDate;
                        TabClass.OpenForm(new frmPaymentMulti());
                        break;
                    case "sales":
                        //frmSalesVoucher.InvNo = Convert.ToString(dgv.Rows[rIndex].Cells[voucherNoIndex].Value).Trim();
                        //frmSalesVoucher.InvDate = (DateTime)oDate;
                        //frmSalesVoucher.InvType = Convert.ToString(dgv.Rows[rIndex].Cells[voucherTypeIndex].Value).Trim();
                        //frmSalesVoucher.CompanyName = comboCompanyName.Text;
                        //frmSalesVoucher.IsSalesRegister = true;
                        TabClass.OpenForm(new frmSalesVoucher(true,
                            false,
                            Convert.ToString(dgv.Rows[rIndex].Cells[voucherNoIndex].Value),
                            Convert.ToString(dgv.Rows[rIndex].Cells[voucherTypeIndex].Value),
                            Utility.YRfromDate((DateTime)oDate),
                            (DateTime)oDate,
                            comboCompanyName.Text));
                        break;
                    case "debitnote":
                        Database.OpenConnection(Utility.MaterialConnectionString);
                        Database.myreader = Database.GetExecuteReaderCommand("select DebitType from debitnote where Debitnotenumber = '"
                            + Convert.ToString(dgv.Rows[rIndex].Cells[voucherNoIndex].Value) + "'");
                        if (Database.myreader.Read())
                            type = Database.myreader[0].ToString();
                        Database.myreader.Close();

                        if (type == "Provisional")
                        {
                            frmProvisionalDebitNote.DebitNo = Convert.ToString(dgv.Rows[rIndex].Cells[voucherNoIndex].Value).Trim();
                            frmProvisionalDebitNote.IsDebit = true;
                            frmProvisionalDebitNote.Company = comboCompanyName.Text;
                            frmProvisionalDebitNote.currentDate = (DateTime)oDate;
                            TabClass.OpenForm(new frmProvisionalDebitNote());
                        }
                        else
                        {
                            FrmDebitNote.DebitNo = Convert.ToString(dgv.Rows[rIndex].Cells[voucherNoIndex].Value).Trim();
                            FrmDebitNote.IsDebit = true;
                            FrmDebitNote.Company = comboCompanyName.Text;
                            FrmDebitNote.currentDate = (DateTime)oDate;
                            TabClass.OpenForm(new FrmDebitNote());
                        }
                        break;
                    case "creditnote":
                        Database.OpenConnection(Utility.MaterialConnectionString);
                        Database.myreader = Database.GetExecuteReaderCommand("select CreditType from creditnote where Creditnotenumber = '"
                            + Convert.ToString(dgv.Rows[rIndex].Cells[voucherNoIndex].Value) + "'");
                        if (Database.myreader.Read())
                            type = Database.myreader[0].ToString();
                        Database.myreader.Close();

                        if (type == "Provisional")
                        {
                            frmProvisionalCreditNote.CreditNo = Convert.ToString(dgv.Rows[rIndex].Cells[voucherNoIndex].Value).Trim();
                            frmProvisionalCreditNote.IsDebit = true;
                            frmProvisionalCreditNote.Company = comboCompanyName.Text;
                            frmProvisionalCreditNote.currentDate = (DateTime)oDate;
                            TabClass.OpenForm(new frmProvisionalCreditNote());
                        }
                        else
                        {
                            FrmCreditNote.CreditNo = Convert.ToString(dgv.Rows[rIndex].Cells[voucherNoIndex].Value).Trim();
                            FrmCreditNote.IsDebit = true;
                            FrmCreditNote.Company = comboCompanyName.Text;
                            FrmCreditNote.currentDate = (DateTime)oDate;
                            TabClass.OpenForm(new FrmCreditNote());
                        }
                        break;
                    case "contra":
                        frmContra.ContraNo = Convert.ToString(dgv.Rows[rIndex].Cells[voucherNoIndex].Value).Trim();
                        frmContra.IsContra = true;
                        frmContra.CompanyName = comboCompanyName.Text;
                        frmContra.contraDate = (DateTime)oDate;
                        TabClass.OpenForm(new frmContra());
                        break;
                    default:
                        break;
                }
            }
        }

        private void TotalCrDr()
        {
            DataSet dataset1 = new DataSet();
            try
            {
                string Currency1 = "Rs.";
                if (Database.OpenConnection_Adapter(Utility.MaterialConnectionString))
                {
                    Database.mycommand.Parameters.Clear();
                    Database.mycommand.CommandText = "sp_rep_total_Cr_dr";
                    Database.mycommand.CommandType = CommandType.StoredProcedure;
                    Database.mycommand.Connection = Database.myconn;
                    Database.mycommand.Parameters.AddWithValue("@DateTo", dateTimePicker2.Value);
                    Database.mycommand.Parameters.AddWithValue("@CompanyName", (comboCompanyName.Text == "* (All)" ? "" : comboCompanyName.Text));
                    Database.mycommand.Parameters.AddWithValue("@LedgerName", comboLedgerName.Text);
                    Database.mycommand.Parameters.AddWithValue("@RepresentiveName", "");
                    Database.mycommand.Parameters.AddWithValue("@DateFrom", dateTimePicker1.Value);
                    Database.myadapter = Database.GetAdapterCommand_SqlCommand();

                    Database.myadapter.Fill(dataset1, " sp_rep_total_Cr_dr '" + (comboCompanyName.Text == "* (All)" ? "" : comboCompanyName.Text) + "','"
                    + comboLedgerName.Text + "','" + dateTimePicker1.Value + "','" + dateTimePicker2.Value + "',' '");

                    dgTotalCrDr.Rows.Clear();

                    List<DataGridViewRow> dRows = new List<DataGridViewRow>();
                    DataGridViewRow dra1 = null;
                    dra1 = (DataGridViewRow)dgTotalCrDr.RowTemplate.Clone();
                    DataGridViewCell[] dataGridViewCells = new DataGridViewCell[dgTotalCrDr.Columns.Count];
                    for (int i = 0; i < dgTotalCrDr.Columns.Count; i++) { dataGridViewCells[i] = (DataGridViewCell)dgTotalCrDr.Columns[i].CellTemplate.Clone(); }
                    dra1.Cells.AddRange(dataGridViewCells);


                    for (int i = 0; i < dataset1.Tables[0].Rows.Count; i++)
                    {
                        DataGridViewRow dr = (DataGridViewRow)dra1.Clone();
                        dr.Cells[0].Value = dataset1.Tables[0].Rows[i]["CompanyName"];
                        dr.Cells[1].Value = dataset1.Tables[0].Rows[i]["LedgerName"];

                        double dp = Utility.SafeConvertToDouble(dataset1.Tables[0].Rows[i]["TotalCredit"]);
                        double db = Utility.SafeConvertToDouble(dataset1.Tables[0].Rows[i]["TotalDebit"]);

                        dr.Cells[2].Value = -1 * dp;
                        dr.Cells[3].Value = db;
                        dRows.Add(dr);
                    }
                    dgTotalCrDr.Rows.AddRange(dRows.ToArray());
                }
                dgTotalCrDr.Visible = true;
                dgTotalCrDr.BringToFront();
                double totalsumCr = 0;
                double totalsumDr = 0;
                if (dataset1.Tables[0].Rows.Count > 0)
                {
                    totalsumCr = Convert.ToDouble(dataset1.Tables[0].Compute("sum(TotalCredit)", ""));
                    totalsumDr = Convert.ToDouble(dataset1.Tables[0].Compute("sum(TotalDebit)", ""));
                }


            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
            }
        }


        #endregion

        #region Events
        private void FrmBillWiseAgstRefReport_Load(object sender, EventArgs e)
        {
        }



        private void comboCompanyName_SelectedIndexChanged(object sender, EventArgs e)
        {


        }

        private void dataGridView1_DoubleClick(object sender, EventArgs e)
        {
        }

        private void dataGridView1_KeyDown(object sender, KeyEventArgs e)
        {
            if (dataGridView1.CurrentCell == null) return;
            if (e.KeyCode == Keys.Back || e.KeyCode == Keys.Delete)
            {
                try
                {
                    ViewSummary();
                    ViewSummary();
                }
                catch (Exception ex)
                {
                    ErrorMessageBox.Show(ex);
                }
            }

            if (e.KeyCode == Keys.F1 && e.Shift)
            {
                ExpandDetails(dataGridView1.CurrentCell.RowIndex);
                e.SuppressKeyPress = true;
            }
            if (e.KeyCode == Keys.Enter)
            {
                if (dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[1].Style.Padding.Left == 0)
                {
                    ExpandDetails(dataGridView1.CurrentCell.RowIndex);
                    e.SuppressKeyPress = true;
                }
                else
                {
                    OpenEntryForm(dataGridView1.CurrentCell.RowIndex);
                }
            }
        }



        private void dataGridView1_Resize(object sender, EventArgs e)
        {
            //dataGridView1.Columns[2].Width = dataGridView1.Width - 880;
        }

        private void refrenceTransactionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (dataGridView1.CurrentCell == null
                || dataGridView1.CurrentCell.RowIndex == -1)
                return;


        }

        private void ledgerSummaryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TabClass.OpenForm(new frmLedgerSummary_Voucher(), null, string.Format("comboCompanyName={0};comboLedgerName={1};dateTimePicker1={2};dateTimePicker2={3}:focus=button1;run=button1", comboCompanyName.Text, comboLedgerName.Text, dateTimePicker2.Value.ToString("yyyy-MM-dd"), dateTimePicker2.Value.ToString("yyyy-MM-dd")));
        }



        #endregion

        private void LoadCompanyCombo(object sender, EventArgs e)
        {
            comboLedgerName.Items.Clear();
            comboLedgerName.Text = "";
            if (Database.OpenConnection(Utility.MaterialConnectionString))
            {

                comboLedgerName.Items.Clear();
                string strsql = "Select distinct ledgername from ledgermaster where companyname in('" + comboCompanyName.Text.Replace(",", "','") + "')";
                if (comboBoxCategory.Text.Length > 0)
                    strsql += " and  Category ='" + comboBoxCategory.Text + "'";
                strsql += " order by LedgerName ";

                Database.myreader = Database.GetExecuteReaderCommand(strsql);
                while (Database.myreader.Read())
                    comboLedgerName.Items.Add(Database.myreader[0].ToString());
                Database.myreader.Close();


                Database.Closeconnection();
            }

            Database.OpenConnection(Utility.MaterialConnectionString);


        }

        private void comboCompanyName_SelectedValueChanged(object sender, EventArgs e)
        {

        }

        private void comboCompanyName_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            //LoadCompanyCombo(sender, e);
        }

        private void comboCompanyName_Leave(object sender, EventArgs e)
        {
            LoadCompanyCombo(sender, e);
        }

        private void btnAnanlysys_Click(object sender, EventArgs e)
        {
            try
            {
                dataGridView1.Visible = true;
                dataGridView1.BringToFront();
                BindGrid();
                dgTotalCrDr.Visible = false;
                dgTotalCrDr.BringToFront();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Problem with Analysys" + ex.Message);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {

        }

        private void comboBoxCategory_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                string sql = "Select distinct LedgerName from LedgerMaster where Category ='" + comboBoxCategory.Text
                    + "' and companyname in ('" + comboCompanyName.Text.Replace(",", "','") + "') order by LedgerName asc";
                if (Database.OpenConnection(Utility.MaterialConnectionString))
                {
                    DataTable dt = Database.GetDataTable(sql);
                    comboLedgerName.Items.Clear();
                    comboLedgerName.Text = "";                    
                    for (int i = 0; i <= dt.Rows.Count - 1; i++)
                    {                        
                        comboLedgerName.Items.Add(dt.Rows[i]["LedgerName"].ToString());                        
                    }
                    
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Problem with Selection of Category :" + ex.Message);
            }
        }
    }
}
