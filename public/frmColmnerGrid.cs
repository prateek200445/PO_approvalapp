using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
//using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Data.SqlClient;
using System.IO;
using System.Globalization;
using DataGridViewAutoFilter;
namespace ERP
{
    public partial class frmColmnerGrid : Form
    {
        #region Variables

        CultureInfo us = new CultureInfo("en-US");
        CultureInfo IN = new CultureInfo("gu-IN");
        //NumberFormatInfo CustomNumberFormat = (NumberFormatInfo)NumberFormatInfo.CurrentInfo.Clone();
        List<ColumnerGrid> cGrid = new List<ColumnerGrid>();
        Assending ase = new Assending();
        public DateTime dTo = Utility.nullDate;
        public DateTime dFrom = Utility.nullDate;
        public string CompanyName = "";
        public string LedgerName = "";

        public string[] DeletedDate;
        public string[] DeletedVoucherNo;
        public string[] DeletedVoucherRef;
        public string[] DeletedVoucherType;
        DataGridViewRow[] DeletedRows = new DataGridViewRow[0];
        AccountVoucherDetails avd = null;
        AccountVoucherDetails avd2 = null;

        private bool RefNoandDateFilled = false;
        private bool ItemQtyandValueFilled = false;
        private bool NarrationFilled = false;

        private DataTable ColumnerDataTable = null;

        DataGridViewRow dra;
        filter.filterData filterData;

        Dictionary<string, string> table_map = new Dictionary<string, string>();

        Database db = new Database();
        private struct ColumnerGrid
        {
            public string VoucherType;
            public string BillNo;
            public string RefNo;
            public string PurchaseLadger;
            public string SubType;
            public string LedgerTo;
            public string CompanyName;
            public string GSTNo;
            public string TINNo;
            public string CSTNo;
            public string PANNo;
            public string[] LedgerFrom;
            public double Amount;
            public double[] SubAmount;
            public DateTime Date;

            public ColumnerGrid(string vType, string bNo, string rNo, string pLadger, string sType, string lTo, string cName, string[] lFrom, double amount, double[] sAmount, DateTime d, string gstNo, string tinNo, string cstNo, string panNo)
            {
                VoucherType = vType;
                BillNo = bNo;
                RefNo = rNo;
                PurchaseLadger = pLadger;
                SubType = sType;
                LedgerTo = lTo;
                CompanyName = cName;
                LedgerFrom = lFrom;
                Amount = amount;
                SubAmount = sAmount;
                Date = d;
                GSTNo = gstNo;
                TINNo = tinNo;
                CSTNo = cstNo;
                PANNo = panNo;
            }
        }

        //enum VoucherType : int {Contra=0, CreditNote=1, DebitNote=2, Journal=3, Payment=4, Purchase=5, Receipt=6, Sales=7};
        #endregion

        #region Main Functions
        public frmColmnerGrid()
        {
            InitializeComponent();
            Form_Load();
        }

        public frmColmnerGrid(DateTime dateFrom, DateTime dateTo, string companyName, string ledgerName, string[] deletedDate, string[] deletedVoucherNo, string[] deletedVoucherRef, string[] deletedVoucherType)
        {
            InitializeComponent();

            dFrom = dateFrom;
            dTo = dateTo;
            CompanyName = comboCompanyName.Text;
            LedgerName = ledgerName;

            DeletedDate = deletedDate;
            DeletedVoucherNo = deletedVoucherNo;
            DeletedVoucherRef = deletedVoucherRef;
            DeletedVoucherType = deletedVoucherType;

            Form_Load();
        }

        public frmColmnerGrid(DateTime dateFrom, DateTime dateTo, string companyName, string ledgerName, DataGridViewRow[] deletedRows)
        {
            InitializeComponent();

            dFrom = dateFrom;
            dTo = dateTo;
            comboCompanyName.Text = companyName;
            CompanyName = companyName;
            LedgerName = ledgerName;

            DeletedRows = deletedRows;

            Form_Load();
        }

        private void Form_Load()
        {
            #region Initilize
            new dateTimePickerRange(dateTimePicker1, dateTimePicker2);
            new ComboFilter(comboLedgerName);
            avd = new AccountVoucherDetails(dataGridView1, comboCompanyName, comboLedgerName, dateTimePicker1, dateTimePicker2, 6, 1, 3, 2, 7, -1, -1, 15, 5, 6);
            avd.IsInventoryOnly = true;
            avd.QtyColumnIndex = 13;
            avd.QtyAmountColumnIndex = 14;

            avd2 = new AccountVoucherDetails(dataGridView2, comboCompanyName, comboLedgerName, dateTimePicker1, dateTimePicker2, 2, 3, 3, 0, 1, -1, -1, 7, 1, 2);


            DeleteRestoreRows drr = new DeleteRestoreRows(dataGridView1);

            drr.BeforeDelete += (dgr, e) =>
                {
                    if (dgr.Cells[1].Style.Padding.Left > 0)
                        e.Cancel = true;
                    else if (dgr.Index < dgr.DataGridView.RowCount - 1 && dgr.DataGridView.Rows[dgr.Index + 1].Cells[1].Style.Padding.Left > 0)
                    {
                        avd.ExpandDetails(dgr.Index);
                    }

                    if (dgr.Index == dataGridView1.RowCount - 1) e.Cancel = true;
                };

            drr.onDelete += (dgr) =>
            {
                dataGridView1.Rows[dataGridView1.RowCount - 1].Cells[10].Value = Utility.SafeConvertToDouble(dataGridView1.Rows[dataGridView1.RowCount - 1].Cells[10].Value) - Utility.SafeConvertToDouble(dgr.Cells[10].Value);
                for (int i = 12; i < dataGridView1.ColumnCount; i++)
                {
                    dataGridView1.Rows[dataGridView1.RowCount - 1].Cells[i].Value = Utility.SafeConvertToDouble(dataGridView1.Rows[dataGridView1.RowCount - 1].Cells[i].Value) - Utility.SafeConvertToDouble(dgr.Cells[i].Value);
                }
            };

            drr.onRestore += (dgr) =>
            {
                dataGridView1.Rows[dataGridView1.RowCount - 1].Cells[10].Value = Utility.SafeConvertToDouble(dataGridView1.Rows[dataGridView1.RowCount - 1].Cells[10].Value) + Utility.SafeConvertToDouble(dgr.Cells[10].Value);
                for (int i = 12; i < dataGridView1.ColumnCount; i++)
                {
                    dataGridView1.Rows[dataGridView1.RowCount - 1].Cells[i].Value = Utility.SafeConvertToDouble(dataGridView1.Rows[dataGridView1.RowCount - 1].Cells[i].Value) + Utility.SafeConvertToDouble(dgr.Cells[i].Value);
                }
            };

            //dataGridView1.Columns["ColumnAmount"].DefaultCellStyle = FormatStyle.Number2Digit(); // CustomNumberFormat;

            /////////////////////
            DataGridView dgv = dataGridView2;
            dra = (DataGridViewRow)dgv.RowTemplate.Clone();
            DataGridViewCell[] dataGridViewCells = new DataGridViewCell[dgv.Columns.Count];
            for (int i = 0; i < dgv.Columns.Count; i++) { dataGridViewCells[i] = (DataGridViewCell)dgv.Columns[i].CellTemplate.Clone(); }
            dra.Cells.AddRange(dataGridViewCells);

            dataGridView2.Columns[ColumnQty.Index].DefaultCellStyle = FormatStyle.NumberFormat(6, 3);
            dataGridView2.Columns[ColumnAmount2.Index].DefaultCellStyle = FormatStyle.Number2Digit();

            filterData = new filter.filterData(dataGridView2);
            filterData = new filter.filterData(dgrSummary);
            filterData = new filter.filterData(dataGridView1);
            filterData = new filter.filterData(dgsalesformat);



            table_map.Add("sales", "SalesVoucher");
            table_map.Add("purchase", "PurchaseVoucher");

            checkBoxRef.Checked = true;
            checkBoxInventory.Checked = false;
            checkBoxNarration.Checked = false;
            checkBoxPartyInfo.Checked = true;

            #endregion

            comboCompanyName.Items.Clear();

            AccountCommonFunction.BindComboCompany(comboCompanyName);
            comboBoxCurrency.Text = "Rs";
            //if (Database.OpenConnection(Utility.MaterialConnectionString))
            //{
            //    Database.myreader = Database.GetExecuteReaderCommand("Select name from factoryinfo order by Name");
            //    while (Database.myreader.Read())
            //        comboCompanyName.Items.Add(Database.myreader[0].ToString());
            //    Database.myreader.Close();
            //}
            comboCompanyName.Enabled = true;
            comboCompanyName.Text = frmDefaultVale.CompanyName;
            Database.Closeconnection();

            if (LedgerName.Length != 0) comboLedgerName.Text = LedgerName;

            if (dFrom == Utility.nullDate || dTo == Utility.nullDate || comboCompanyName.Text.Length == 0)
            {
                dateTimePicker1.Value = DateTime.Now.AddMonths(-1);
                comboCompanyName.Text = frmDefaultVale.CompanyName;
                DeletedDate = new string[0];
            }
            else
            {
                dateTimePicker1.Value = dFrom;
                dateTimePicker2.Value = dTo;
                LoadData(dFrom, dTo, "");
            }
        }

        #endregion

        #region Functions

        private void LoadData1(DateTime DateFrom, DateTime DateTo, string VoucherType)
        {
            //dataGridView1.Rows.Clear();
            filterData.clearFilter();
            filterData.switchOnOff(true);
            dgsalesformat.Visible = false;
            dataGridView1.Visible = true;
            dataGridView2.Visible = false;
            dgrSummary.Visible = false;
            dgItemWiseSumm.Visible = false;
            splitContainer1.Visible = false;

            string ledgerName = null;
            string type = null;
            LedgerName = comboLedgerName.Text;

            if (VoucherType.Length > 0) type = VoucherType;
            if (LedgerName.Length > 0) ledgerName = LedgerName;

            if (Database.OpenConnection(Utility.MaterialConnectionString))
            {



                ColumnerDataTable = Database.GetDataTable("sp_ColumnerPivot_All", CommandType.StoredProcedure,
                               new Database.Parameter("@DateFrom", DateFrom.ToString("yyyy-MM-dd 00:00:00")),
                               new Database.Parameter("@DateTo", DateTo.ToString("yyyy-MM-dd 23:59:59")),
                               new Database.Parameter("@CompanyName", comboCompanyName.Text),
                               new Database.Parameter("@LedgerName", ledgerName),
                               new Database.Parameter("@VoucherType", null)
                               );



                DataTable dataTable = ColumnerDataTable;

                if (dataTable.Rows.Count == 0)
                {
                    MessageBox.Show("No data found");
                    return;
                }

                if (cbExportToExcel.Checked == true)
                {
                    if (dataTable.Columns.Count > 500)
                        MessageBox.Show("This record can't show directly. This will export in excel", "Columner", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    if (!checkBoxRef.Checked)
                    {
                        dataTable.Columns.Remove("RefNo");
                        dataTable.Columns.Remove("RefDate");
                    }
                    if (!checkBoxInventory.Checked)
                    {
                        dataTable.Columns.Remove("Qty");
                        dataTable.Columns.Remove("Value");
                    }
                    if (!checkBoxPartyInfo.Checked)
                    {
                        dataTable.Columns.Remove("GSTNo");
                        dataTable.Columns.Remove("TINNo");
                        dataTable.Columns.Remove("CSTNo");
                        dataTable.Columns.Remove("PanNo");
                    }
                    if (!checkBoxNarration.Checked)
                    {
                        dataTable.Columns.Remove("Narration");
                    }

                    Export.ToFile(dataTable);
                    return;
                }

                dataGridView1.DataSource = dataTable;

                // visible or not
                dataGridView1.Columns[3].Visible = dataGridView1.Columns[4].Visible = checkBoxRef.Checked;
                dataGridView1.Columns[13].Visible = dataGridView1.Columns[14].Visible = checkBoxInventory.Checked;
                dataGridView1.Columns[12].Visible = checkBoxNarration.Checked;
                dataGridView1.Columns[8].Visible = dataGridView1.Columns[9].Visible =
                dataGridView1.Columns[10].Visible = dataGridView1.Columns[11].Visible
                    = checkBoxPartyInfo.Checked;

                //dataGridView1.Columns[15].DefaultCellStyle = FormatStyle.AccountCurrencyFormat();

                //for (int i = 16; i < dataGridView1.ColumnCount; i++)
                //{
                //    dataGridView1.Columns[i].DefaultCellStyle = FormatStyle.AccountCurrencyFormat();
                //}

                return;

                int FreezedColumnCount = 16;
                int DynamicFirstColumnIndexDataTable = 16;
                int DynamicFirstColumnIndexGrid = 16;

                while (dataGridView1.Columns.Count > FreezedColumnCount)
                {
                    dataGridView1.Columns.RemoveAt(FreezedColumnCount);
                }

                // Insert Dynamic Column
                for (int i = DynamicFirstColumnIndexDataTable; i < dataTable.Columns.Count; i++)
                {
                    dataGridView1.Columns.Add(dataTable.Columns[i].ColumnName, dataTable.Columns[i].ColumnName);
                }

                for (int i = DynamicFirstColumnIndexGrid; i < dataGridView1.Columns.Count; i++)
                {
                    dataGridView1.Columns[i].DefaultCellStyle = FormatStyle.Number2Digit();
                }


                DateTime dStart = DateTime.Now;

                Tools.DrawingControl.SuspendDrawing(dataGridView1);

                try
                {
                    // Insert Rows
                    for (int i = 0, rIndex = 0; i < dataTable.Rows.Count; i++)
                    {

                        bool isDeleted = false;
                        for (int j = 0; j < DeletedRows.Length; j++)
                        {
                            if (String.Compare((string)dataTable.Rows[i].ItemArray[1], Convert.ToString(DeletedRows[j].Cells[3].Value), true) == 0            // Match Voucher No
                                && String.Compare(((string)dataTable.Rows[i].ItemArray[3]).Trim(), Convert.ToString(DeletedRows[j].Cells[4].Value).Trim(), true) == 0     // Match Voucher Ref
                                && String.Compare((AccountCommonFunction.SafeConvertToDate(dataTable.Rows[i].ItemArray[2])).ToString("dd/MM/yyyy"), (AccountCommonFunction.SafeConvertToDate(DeletedRows[j].Cells[0].Value)).ToString("dd/MM/yyyy"), true) == 0     // Match Date
                                && String.Compare((string)dataTable.Rows[i].ItemArray[6], Convert.ToString(DeletedRows[j].Cells[2].Value), true) == 0)    // Match Voucher Type
                            {
                                isDeleted = true;
                                break;
                            }
                        }
                        if (isDeleted) continue;

                        dataGridView1.Rows.Add(
                            dataTable.Rows[i].ItemArray[0], // VoucherType
                            dataTable.Rows[i].ItemArray[1], // VoucherNo
                            dataTable.Rows[i].ItemArray[2], // VoucherDate
                            dataTable.Rows[i].ItemArray[3], // RefNo
                            dataTable.Rows[i].ItemArray[4], // RefDate
                            dataTable.Rows[i].ItemArray[5], // Ledger
                            dataTable.Rows[i].ItemArray[6], // SubVoucherType
                            dataTable.Rows[i].ItemArray[7], // Party
                            dataTable.Rows[i].ItemArray[8], // GSTNo
                            dataTable.Rows[i].ItemArray[9], // TINNo
                            dataTable.Rows[i].ItemArray[10], // CSTNo
                            dataTable.Rows[i].ItemArray[11], // PanNo
                            dataTable.Rows[i].ItemArray[12], // Narration
                            dataTable.Rows[i].ItemArray[13], // Qty
                            dataTable.Rows[i].ItemArray[14], // Value
                            dataTable.Rows[i].ItemArray[15] // GrossAmount
                            );

                        for (int j = DynamicFirstColumnIndexDataTable; j < dataTable.Columns.Count; j++)
                        {
                            //if (dataTable.Rows[i].ItemArray[j] is DBNull) continue;
                            dataGridView1.Rows[rIndex].Cells[DynamicFirstColumnIndexGrid + (j - DynamicFirstColumnIndexDataTable)].Value = dataTable.Rows[i].ItemArray[j];
                        }

                        rIndex++;
                    }

                    // Sum Row
                    int TotalAmountRowIndex = dataGridView1.Rows.Count;
                    dataGridView1.Rows.Add();

                    dataGridView1.Rows[TotalAmountRowIndex].DefaultCellStyle.Font = new Font(dataGridView1.Font, FontStyle.Bold);
                    dataGridView1.Rows[TotalAmountRowIndex].Cells[0].Value = "Total";
                    dataGridView1.Rows[TotalAmountRowIndex].Cells[15].Value = Utility.SafeConvertToDouble(dataTable.Compute("SUM(GrossAmount)", ""));
                    dataGridView1.Rows[TotalAmountRowIndex].Cells[13].Value = Utility.SafeConvertToDouble(dataTable.Compute("SUM(Qty)", ""));
                    dataGridView1.Rows[TotalAmountRowIndex].Cells[14].Value = Utility.SafeConvertToDouble(dataTable.Compute("SUM(Value)", ""));

                    for (int i = DynamicFirstColumnIndexGrid; i < dataGridView1.Columns.Count; i++)
                    {
                        dataGridView1.Rows[TotalAmountRowIndex].Cells[i].Value = Utility.SafeConvertToDouble(
                            dataTable.Compute("Sum([" + dataGridView1.Columns[i].HeaderText + "])", "")
                        );
                    }
                }
                catch (Exception ex)
                {
                    ErrorMessageBox.Show(ex);
                }

                Tools.DrawingControl.ResumeDrawing(dataGridView1);

                System.Diagnostics.Debug.WriteLine((DateTime.Now - dStart).Milliseconds);

                return;

                //SqlParameter[] p = new SqlParameter[]{
                //new SqlParameter("@dateFrom",DateFrom.ToString("yyyy-MM-dd")),
                //new SqlParameter("@dateto", DateTo.ToString("yyyy-MM-dd")),
                //new SqlParameter("@Companyname", comboCompanyName.Text),
                //new SqlParameter("@ledgername", ledgerName),
                //new SqlParameter("@type", type)
                //};

                //using (DataTable dataTable2 = db.getdata(p, "sp_AllLdegerForColumnerNW", "Material"))
                //{
                //    dataTable2.DefaultView.Sort = "RCPT, PAYMENTNO";
                //    using (DataTable dataTable = dataTable2.DefaultView.ToTable())
                //    {

                //        cGrid.Clear();
                //        string vType = "", BillNo = "", RefNo = "", PurchaseLadger = "", SubType = "", LedgerTo = "", gstNo = "", tinNo = "", cstNo = "", panNo = "";
                //        List<string> LedgerFrom = new List<string>();
                //        double Amount = 0;
                //        List<double> SubAmount = new List<double>();
                //        DateTime Date = DateTime.Now;

                //        bool isInitialized = false;

                //        for (int i = 0; i < dataTable.Rows.Count; i++)
                //        {
                //            bool isDeleted = false;
                //            for (int j = 0; j < DeletedRows.Length; j++)
                //            {
                //                if (String.Compare((string)dataTable.Rows[i].ItemArray[1], Convert.ToString(DeletedRows[j].Cells[3].Value), true) == 0            // Match Voucher No
                //                    && String.Compare(((string)dataTable.Rows[i].ItemArray[2]).Trim(), Convert.ToString(DeletedRows[j].Cells[4].Value).Trim(), true) == 0     // Match Voucher Ref
                //                    && String.Compare(((DateTime)dataTable.Rows[i].ItemArray[10]).ToString("dd/MM/yyyy"), ((DateTime)DeletedRows[j].Cells[0].Value).ToString("dd/MM/yyyy"), true) == 0     // Match Date
                //                    && String.Compare((string)dataTable.Rows[i].ItemArray[4], Convert.ToString(DeletedRows[j].Cells[2].Value), true) == 0)    // Match Voucher Type
                //                    //if (String.Compare((string)dataTable.Rows[i].ItemArray[1], DeletedVoucherNo[j], true) == 0            // Match Voucher No
                //                    //&& String.Compare(((string)dataTable.Rows[i].ItemArray[2]).Trim(), DeletedVoucherRef[j].Trim(), true) == 0     // Match Voucher Ref
                //                    //&& String.Compare(((DateTime)dataTable.Rows[i].ItemArray[10]).ToString("dd/MM/yyyy"), DeletedDate[j], true) == 0     // Match Date
                //                    //&& String.Compare((string)dataTable.Rows[i].ItemArray[4], DeletedVoucherType[j], true) == 0)    // Match Voucher Type
                //                {
                //                    isDeleted = true;
                //                    break;
                //                }
                //            }
                //            if (isDeleted) continue;

                //            if (String.Compare((string)dataTable.Rows[i].ItemArray[1], BillNo, true) == 0
                //                && String.Compare((string)dataTable.Rows[i].ItemArray[0], vType, true) == 0
                //                 && String.Compare((string)dataTable.Rows[i].ItemArray[2], RefNo, true) == 0
                //                && String.Compare((string)dataTable.Rows[i].ItemArray[6], LedgerTo, true) == 0)
                //            //&& String.Compare((string)dataTable.Rows[i].ItemArray[11], CompanyName, true) == 0)   // Future Use
                //            {
                //                LedgerFrom.Add((string)dataTable.Rows[i].ItemArray[7]);
                //                SubAmount.Add((double)dataTable.Rows[i].ItemArray[9]);
                //            }
                //            else
                //            {
                //                if (isInitialized)
                //                    cGrid.Add(new ColumnerGrid(vType, BillNo, RefNo, PurchaseLadger, SubType, LedgerTo, comboCompanyName.Text, LedgerFrom.ToArray(), Amount, SubAmount.ToArray(), Date, gstNo, tinNo, cstNo, panNo));

                //                vType = (string)dataTable.Rows[i].ItemArray[0];
                //                BillNo = (string)dataTable.Rows[i].ItemArray[1];
                //                RefNo = (string)dataTable.Rows[i].ItemArray[2];
                //                PurchaseLadger = Convert.ToString(dataTable.Rows[i].ItemArray[3]);
                //                SubType = (string)dataTable.Rows[i].ItemArray[4];
                //                LedgerTo = (string)dataTable.Rows[i].ItemArray[6];
                //                Amount = (double)dataTable.Rows[i].ItemArray[8];
                //                Date = (DateTime)dataTable.Rows[i].ItemArray[10];
                //                comboCompanyName.Text = (string)dataTable.Rows[i].ItemArray[11];
                //                gstNo = (string)dataTable.Rows[i].ItemArray[12];
                //                tinNo = (string)dataTable.Rows[i].ItemArray[13];
                //                cstNo = (string)dataTable.Rows[i].ItemArray[14];
                //                panNo = (string)dataTable.Rows[i].ItemArray[15];

                //                LedgerFrom.Clear();
                //                SubAmount.Clear();
                //                LedgerFrom.Add((string)dataTable.Rows[i].ItemArray[7]);
                //                SubAmount.Add((double)dataTable.Rows[i].ItemArray[9]);

                //                isInitialized = true;
                //            }
                //        }

                //        if (dataTable.Rows.Count > 0)
                //        {
                //            cGrid.Add(new ColumnerGrid(vType, BillNo, RefNo, PurchaseLadger, SubType, LedgerTo, comboCompanyName.Text, LedgerFrom.ToArray(), Amount, SubAmount.ToArray(), Date, gstNo, tinNo, cstNo, panNo));

                //            LedgerFrom.Clear();
                //            SubAmount.Clear();
                //        }
                //    }
                //}
                Database.myconn.Close();
            }

            BindData(VoucherType);
        }

        private void LoadData(DateTime DateFrom, DateTime DateTo, string VoucherType)
        {
            //dataGridView1.Rows.Clear();
            filterData.clearFilter();
            filterData.switchOnOff(true);
            dgsalesformat.Visible = false;
            dataGridView1.Visible = true;
            dataGridView2.Visible = false;
            dgrSummary.Visible = false;
            dgItemWiseSumm.Visible = false;
            splitContainer1.Visible = false;

            dataGridView1.BringToFront();
            dataGridView1.Focus();

            string ledgerName = null;
            string type = null;
            LedgerName = comboLedgerName.Text;

            if (VoucherType.Length > 0) type = VoucherType;
            if (LedgerName.Length > 0) ledgerName = LedgerName;

            if (Database.OpenConnection(Utility.MaterialConnectionString))
            {
                //ColumnerDataTable = db.getdata(new SqlParameter[] {
                //                                           new SqlParameter("@DateFrom", DateFrom),
                //                                           new SqlParameter("@DateTo", DateTo),
                //                                           new SqlParameter("@CompanyName", comboCompanyName.Text),
                //                                           new SqlParameter("@LedgerName", ledgerName),
                //                                           new SqlParameter("@VoucherType", type)
                //                                        }, "sp_ColumnerPivot", "Material");

                if (type == "Credit Note JV" || type == "Debit Note JV")
                {
                    ColumnerDataTable = Database.GetDataTable("sp_ColumnerPivot", CommandType.StoredProcedure,
                                   new Database.Parameter("@DateFrom", DateFrom.ToString("yyyy-MM-dd 00:00:00")),
                                   new Database.Parameter("@DateTo", DateTo.ToString("yyyy-MM-dd 23:59:59")),
                                   new Database.Parameter("@CompanyName", comboCompanyName.Text),
                                   new Database.Parameter("@LedgerName", ledgerName),
                                   new Database.Parameter("@VoucherType", "journal"),
                                   new Database.Parameter("@AddVoucherType", type),
                                   new Database.Parameter("@currType", comboBoxCurrency.Text)
                                   );
                }
                else if (type == "Job Work")
                {
                    ColumnerDataTable = Database.GetDataTable("SP_VIEW_JOBWORK", CommandType.StoredProcedure,
                                   new Database.Parameter("@DateFrom", DateFrom.ToString("yyyy-MM-dd 00:00:00")),
                                   new Database.Parameter("@DateTo", DateTo.ToString("yyyy-MM-dd 23:59:59")),
                                   new Database.Parameter("@CompanyName", comboCompanyName.Text)
                                   );
                }
                else
                {
                    ColumnerDataTable = Database.GetDataTable("sp_ColumnerPivot", CommandType.StoredProcedure,
                                   new Database.Parameter("@DateFrom", DateFrom.ToString("yyyy-MM-dd 00:00:00")),
                                   new Database.Parameter("@DateTo", DateTo.ToString("yyyy-MM-dd 23:59:59")),
                                   new Database.Parameter("@CompanyName", comboCompanyName.Text),
                                   new Database.Parameter("@LedgerName", ledgerName),
                                   new Database.Parameter("@VoucherType", type),
                                   new Database.Parameter("@currType", comboBoxCurrency.Text),
                                   new Database.Parameter("@BlnInterCompany", (checkBox1.Checked == true ? 1 : 0))
                                   );

                }

                DataTable dataTable = ColumnerDataTable;

                if (dataTable.Rows.Count == 0)
                {
                    MessageBox.Show("No data found");
                    return;
                }
                if (type == "Job Work")
                {
                    dgsalesformat.DataSource = dataTable;
                    dgsalesformat.Visible = true;
                    dataGridView1.Visible = false;
                    dataGridView2.Visible = false;
                    dgrSummary.Visible = false;
                    dgItemWiseSumm.Visible = false;
                    return;
                }

                if (cbExportToExcel.Checked == true)
                {
                    if (dataTable.Columns.Count > 500)
                        MessageBox.Show("This record can't show directly. This will export in excel", "Columner", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    if (!checkBoxRef.Checked)
                    {
                        dataTable.Columns.Remove("RefNo");
                        dataTable.Columns.Remove("RefDate");
                    }
                    if (!checkBoxInventory.Checked)
                    {
                        dataTable.Columns.Remove("Qty");
                        dataTable.Columns.Remove("Value");
                    }
                    if (!checkBoxPartyInfo.Checked)
                    {
                        dataTable.Columns.Remove("GSTNo");
                        dataTable.Columns.Remove("TINNo");
                        dataTable.Columns.Remove("CSTNo");
                        dataTable.Columns.Remove("PanNo");
                    }
                    if (!checkBoxNarration.Checked)
                    {
                        dataTable.Columns.Remove("Narration");
                    }

                    Export.ToFile(dataTable);
                    return;
                }

                dataGridView1.DataSource = dataTable;

                // visible or not
                dataGridView1.Columns[3].Visible = dataGridView1.Columns[4].Visible = checkBoxRef.Checked;
                dataGridView1.Columns[14].Visible = checkBoxNarration.Checked;
                //dataGridView1.Columns[12].Visible = checkBoxNarration.Checked;
                dataGridView1.Columns[8].Visible = dataGridView1.Columns[9].Visible =
                dataGridView1.Columns[10].Visible = dataGridView1.Columns[11].Visible = dataGridView1.Columns[12].Visible =
                dataGridView1.Columns[13].Visible = checkBoxPartyInfo.Checked;

                dataGridView1.Columns[15].DefaultCellStyle = FormatStyle.AccountCurrencyFormat();

                for (int i = 16; i < dataGridView1.ColumnCount; i++)
                {
                    dataGridView1.Columns[i].DefaultCellStyle = FormatStyle.AccountCurrencyFormat();
                }

                return;

                int FreezedColumnCount = 16;
                int DynamicFirstColumnIndexDataTable = 16;
                int DynamicFirstColumnIndexGrid = 16;

                while (dataGridView1.Columns.Count > FreezedColumnCount)
                {
                    dataGridView1.Columns.RemoveAt(FreezedColumnCount);
                }

                // Insert Dynamic Column
                for (int i = DynamicFirstColumnIndexDataTable; i < dataTable.Columns.Count; i++)
                {
                    dataGridView1.Columns.Add(dataTable.Columns[i].ColumnName, dataTable.Columns[i].ColumnName);
                }

                for (int i = DynamicFirstColumnIndexGrid; i < dataGridView1.Columns.Count; i++)
                {
                    dataGridView1.Columns[i].DefaultCellStyle = FormatStyle.Number2Digit();
                }


                DateTime dStart = DateTime.Now;

                Tools.DrawingControl.SuspendDrawing(dataGridView1);

                try
                {
                    // Insert Rows
                    for (int i = 0, rIndex = 0; i < dataTable.Rows.Count; i++)
                    {

                        bool isDeleted = false;
                        for (int j = 0; j < DeletedRows.Length; j++)
                        {
                            if (String.Compare((string)dataTable.Rows[i].ItemArray[1], Convert.ToString(DeletedRows[j].Cells[3].Value), true) == 0            // Match Voucher No
                                && String.Compare(((string)dataTable.Rows[i].ItemArray[3]).Trim(), Convert.ToString(DeletedRows[j].Cells[4].Value).Trim(), true) == 0     // Match Voucher Ref
                                && String.Compare((AccountCommonFunction.SafeConvertToDate(dataTable.Rows[i].ItemArray[2])).ToString("dd/MM/yyyy"), (AccountCommonFunction.SafeConvertToDate(DeletedRows[j].Cells[0].Value)).ToString("dd/MM/yyyy"), true) == 0     // Match Date
                                && String.Compare((string)dataTable.Rows[i].ItemArray[6], Convert.ToString(DeletedRows[j].Cells[2].Value), true) == 0)    // Match Voucher Type
                            {
                                isDeleted = true;
                                break;
                            }
                        }
                        if (isDeleted) continue;

                        dataGridView1.Rows.Add(
                            dataTable.Rows[i].ItemArray[0], // VoucherType
                            dataTable.Rows[i].ItemArray[1], // VoucherNo
                            dataTable.Rows[i].ItemArray[2], // VoucherDate
                            dataTable.Rows[i].ItemArray[3], // RefNo
                            dataTable.Rows[i].ItemArray[4], // RefDate
                            dataTable.Rows[i].ItemArray[5], // Ledger
                            dataTable.Rows[i].ItemArray[6], // SubVoucherType
                            dataTable.Rows[i].ItemArray[7], // Party
                            dataTable.Rows[i].ItemArray[8], // GSTNo
                            dataTable.Rows[i].ItemArray[9], // TINNo
                            dataTable.Rows[i].ItemArray[10], // CSTNo
                            dataTable.Rows[i].ItemArray[11], // PanNo
                            dataTable.Rows[i].ItemArray[12], // Narration
                            dataTable.Rows[i].ItemArray[13], // Qty
                            dataTable.Rows[i].ItemArray[14], // Value
                            dataTable.Rows[i].ItemArray[15] // GrossAmount
                            );

                        for (int j = DynamicFirstColumnIndexDataTable; j < dataTable.Columns.Count; j++)
                        {
                            //if (dataTable.Rows[i].ItemArray[j] is DBNull) continue;
                            dataGridView1.Rows[rIndex].Cells[DynamicFirstColumnIndexGrid + (j - DynamicFirstColumnIndexDataTable)].Value = dataTable.Rows[i].ItemArray[j];
                        }

                        rIndex++;
                    }

                    // Sum Row
                    int TotalAmountRowIndex = dataGridView1.Rows.Count;
                    dataGridView1.Rows.Add();

                    dataGridView1.Rows[TotalAmountRowIndex].DefaultCellStyle.Font = new Font(dataGridView1.Font, FontStyle.Bold);
                    dataGridView1.Rows[TotalAmountRowIndex].Cells[0].Value = "Total";
                    dataGridView1.Rows[TotalAmountRowIndex].Cells[15].Value = Utility.SafeConvertToDouble(dataTable.Compute("SUM(GrossAmount)", ""));
                    dataGridView1.Rows[TotalAmountRowIndex].Cells[13].Value = Utility.SafeConvertToDouble(dataTable.Compute("SUM(Qty)", ""));
                    dataGridView1.Rows[TotalAmountRowIndex].Cells[14].Value = Utility.SafeConvertToDouble(dataTable.Compute("SUM(Value)", ""));

                    for (int i = DynamicFirstColumnIndexGrid; i < dataGridView1.Columns.Count; i++)
                    {
                        dataGridView1.Rows[TotalAmountRowIndex].Cells[i].Value = Utility.SafeConvertToDouble(
                            dataTable.Compute("Sum([" + dataGridView1.Columns[i].HeaderText + "])", "")
                        );
                    }
                }
                catch (Exception ex)
                {
                    ErrorMessageBox.Show(ex);
                }

                Tools.DrawingControl.ResumeDrawing(dataGridView1);

                System.Diagnostics.Debug.WriteLine((DateTime.Now - dStart).Milliseconds);

                return;

                //SqlParameter[] p = new SqlParameter[]{
                //new SqlParameter("@dateFrom",DateFrom.ToString("yyyy-MM-dd")),
                //new SqlParameter("@dateto", DateTo.ToString("yyyy-MM-dd")),
                //new SqlParameter("@Companyname", comboCompanyName.Text),
                //new SqlParameter("@ledgername", ledgerName),
                //new SqlParameter("@type", type)
                //};

                //using (DataTable dataTable2 = db.getdata(p, "sp_AllLdegerForColumnerNW", "Material"))
                //{
                //    dataTable2.DefaultView.Sort = "RCPT, PAYMENTNO";
                //    using (DataTable dataTable = dataTable2.DefaultView.ToTable())
                //    {

                //        cGrid.Clear();
                //        string vType = "", BillNo = "", RefNo = "", PurchaseLadger = "", SubType = "", LedgerTo = "", gstNo = "", tinNo = "", cstNo = "", panNo = "";
                //        List<string> LedgerFrom = new List<string>();
                //        double Amount = 0;
                //        List<double> SubAmount = new List<double>();
                //        DateTime Date = DateTime.Now;

                //        bool isInitialized = false;

                //        for (int i = 0; i < dataTable.Rows.Count; i++)
                //        {
                //            bool isDeleted = false;
                //            for (int j = 0; j < DeletedRows.Length; j++)
                //            {
                //                if (String.Compare((string)dataTable.Rows[i].ItemArray[1], Convert.ToString(DeletedRows[j].Cells[3].Value), true) == 0            // Match Voucher No
                //                    && String.Compare(((string)dataTable.Rows[i].ItemArray[2]).Trim(), Convert.ToString(DeletedRows[j].Cells[4].Value).Trim(), true) == 0     // Match Voucher Ref
                //                    && String.Compare(((DateTime)dataTable.Rows[i].ItemArray[10]).ToString("dd/MM/yyyy"), ((DateTime)DeletedRows[j].Cells[0].Value).ToString("dd/MM/yyyy"), true) == 0     // Match Date
                //                    && String.Compare((string)dataTable.Rows[i].ItemArray[4], Convert.ToString(DeletedRows[j].Cells[2].Value), true) == 0)    // Match Voucher Type
                //                    //if (String.Compare((string)dataTable.Rows[i].ItemArray[1], DeletedVoucherNo[j], true) == 0            // Match Voucher No
                //                    //&& String.Compare(((string)dataTable.Rows[i].ItemArray[2]).Trim(), DeletedVoucherRef[j].Trim(), true) == 0     // Match Voucher Ref
                //                    //&& String.Compare(((DateTime)dataTable.Rows[i].ItemArray[10]).ToString("dd/MM/yyyy"), DeletedDate[j], true) == 0     // Match Date
                //                    //&& String.Compare((string)dataTable.Rows[i].ItemArray[4], DeletedVoucherType[j], true) == 0)    // Match Voucher Type
                //                {
                //                    isDeleted = true;
                //                    break;
                //                }
                //            }
                //            if (isDeleted) continue;

                //            if (String.Compare((string)dataTable.Rows[i].ItemArray[1], BillNo, true) == 0
                //                && String.Compare((string)dataTable.Rows[i].ItemArray[0], vType, true) == 0
                //                 && String.Compare((string)dataTable.Rows[i].ItemArray[2], RefNo, true) == 0
                //                && String.Compare((string)dataTable.Rows[i].ItemArray[6], LedgerTo, true) == 0)
                //            //&& String.Compare((string)dataTable.Rows[i].ItemArray[11], CompanyName, true) == 0)   // Future Use
                //            {
                //                LedgerFrom.Add((string)dataTable.Rows[i].ItemArray[7]);
                //                SubAmount.Add((double)dataTable.Rows[i].ItemArray[9]);
                //            }
                //            else
                //            {
                //                if (isInitialized)
                //                    cGrid.Add(new ColumnerGrid(vType, BillNo, RefNo, PurchaseLadger, SubType, LedgerTo, comboCompanyName.Text, LedgerFrom.ToArray(), Amount, SubAmount.ToArray(), Date, gstNo, tinNo, cstNo, panNo));

                //                vType = (string)dataTable.Rows[i].ItemArray[0];
                //                BillNo = (string)dataTable.Rows[i].ItemArray[1];
                //                RefNo = (string)dataTable.Rows[i].ItemArray[2];
                //                PurchaseLadger = Convert.ToString(dataTable.Rows[i].ItemArray[3]);
                //                SubType = (string)dataTable.Rows[i].ItemArray[4];
                //                LedgerTo = (string)dataTable.Rows[i].ItemArray[6];
                //                Amount = (double)dataTable.Rows[i].ItemArray[8];
                //                Date = (DateTime)dataTable.Rows[i].ItemArray[10];
                //                comboCompanyName.Text = (string)dataTable.Rows[i].ItemArray[11];
                //                gstNo = (string)dataTable.Rows[i].ItemArray[12];
                //                tinNo = (string)dataTable.Rows[i].ItemArray[13];
                //                cstNo = (string)dataTable.Rows[i].ItemArray[14];
                //                panNo = (string)dataTable.Rows[i].ItemArray[15];

                //                LedgerFrom.Clear();
                //                SubAmount.Clear();
                //                LedgerFrom.Add((string)dataTable.Rows[i].ItemArray[7]);
                //                SubAmount.Add((double)dataTable.Rows[i].ItemArray[9]);

                //                isInitialized = true;
                //            }
                //        }

                //        if (dataTable.Rows.Count > 0)
                //        {
                //            cGrid.Add(new ColumnerGrid(vType, BillNo, RefNo, PurchaseLadger, SubType, LedgerTo, comboCompanyName.Text, LedgerFrom.ToArray(), Amount, SubAmount.ToArray(), Date, gstNo, tinNo, cstNo, panNo));

                //            LedgerFrom.Clear();
                //            SubAmount.Clear();
                //        }
                //    }
                //}
                Database.myconn.Close();
            }

            BindData(VoucherType);
        }

        private void BindData(string VoucherType)
        {
            Tools.DrawingControl.SuspendDrawing(dataGridView1);

            List<double> amountTotal = new List<double>();
            amountTotal.Add(0);

            //dataGridView1.Rows.Clear();
            dataGridView1.DataSource = null;

            dataGridView2.Rows.Clear();

            while (dataGridView1.Columns.Count > 12)
            {
                dataGridView1.Columns.RemoveAt(12);
            }

            DataGridViewCellStyle dataGridViewCellStyle1 = FormatStyle.Number2Digit();

            int rCount = 0;
            for (int i = 0; i < cGrid.Count; i++)
            {
                if (VoucherType.Length > 0 && VoucherType != cGrid[i].VoucherType.ToLower()) continue;

                dataGridView1.Rows.Add();
                rCount++;

                dataGridView1.Rows[i].Cells[0].Value = cGrid[i].VoucherType;
                dataGridView1.Rows[i].Cells[1].Value = cGrid[i].BillNo;
                dataGridView1.Rows[i].Cells[2].Value = cGrid[i].RefNo;
                dataGridView1.Rows[i].Cells[3].Value = cGrid[i].PurchaseLadger;
                dataGridView1.Rows[i].Cells[4].Value = cGrid[i].SubType;
                dataGridView1.Rows[i].Cells[5].Value = cGrid[i].LedgerTo;
                dataGridView1.Rows[i].Cells[6].Value = cGrid[i].GSTNo;
                dataGridView1.Rows[i].Cells[7].Value = cGrid[i].TINNo;
                dataGridView1.Rows[i].Cells[8].Value = cGrid[i].CSTNo;
                dataGridView1.Rows[i].Cells[9].Value = cGrid[i].PANNo;
                dataGridView1.Rows[i].Cells[10].Value = cGrid[i].Amount;
                dataGridView1.Rows[i].Cells[11].Value = cGrid[i].Date;

                amountTotal[0] += cGrid[i].Amount;

                for (int j = 0; j < cGrid[i].LedgerFrom.Length; j++)
                {
                    bool insertRequired = true;
                    int k = 12;
                    int l = dataGridView1.ColumnCount - 1;
                    for (; l >= k; l--)
                    {
                        if (String.Compare(dataGridView1.Columns[l].HeaderText, cGrid[i].LedgerFrom[j], true) == 0)
                        {
                            dataGridView1.Rows[i].Cells[l].Value = cGrid[i].SubAmount[j];
                            amountTotal[l - 11] += cGrid[i].SubAmount[j];
                            insertRequired = false;
                            break;
                        }
                    }
                    //for (; k < dataGridView1.Columns.Count; k++)
                    //{
                    //    if (String.Compare(dataGridView1.Columns[k].HeaderText, cGrid[i].LedgerFrom[j]) == 0)
                    //    {
                    //        dataGridView1.Rows[i].Cells[k].Value = cGrid[i].SubAmount[j]; //.ToString("#,##0.00", IN); //ToString("0,0", CultureInfo.InvariantCulture);//.ToString("0.00");
                    //        amountTotal[k - 11] += cGrid[i].SubAmount[j];
                    //        insertRequired = false;
                    //        break;
                    //    }
                    //}
                    k = dataGridView1.ColumnCount;
                    if (insertRequired)
                    {
                        dataGridView1.Columns.Add("Column" + k.ToString(), cGrid[i].LedgerFrom[j]);
                        dataGridView1.Rows[i].Cells[k].Value = cGrid[i].SubAmount[j];
                        dataGridView1.Columns[k].DefaultCellStyle = dataGridViewCellStyle1;
                        amountTotal.Add(0);
                        amountTotal[k - 11] += cGrid[i].SubAmount[j];
                    }
                }
            }

            dataGridView1.Rows.Add();
            dataGridView1.Rows[rCount].DefaultCellStyle.Font = new Font(dataGridView1.Font, FontStyle.Bold);
            dataGridView1.Rows[rCount].Cells[0].Value = "Total";
            dataGridView1.Rows[rCount].Cells[10].Value = amountTotal[0];
            //for (int j = 1; j < amountTotal.Count; j++)
            //{
            //    dataGridView1.Rows[rCount].Cells[11 + j].Value = amountTotal[j].ToString("0.00");
            //}

            for (int j = 12; j < dataGridView1.Columns.Count; j++)
            {
                double a = 0;
                for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                {
                    object cValue = dataGridView1.Rows[i].Cells[j].Value;
                    if (cValue != null)
                    {
                        a += (double)cValue;
                        //a += Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[j].FormattedValue.ToString());
                    }
                }
                dataGridView1.Rows[dataGridView1.Rows.Count - 1].Cells[j].Value = a; //.ToString("0.00");
            }

            // ase.Order(dataGridView1);

            Tools.DrawingControl.ResumeDrawing(dataGridView1);
        }

        #endregion

        #region Events

        private void frmColmnerGrid_Load(object sender, EventArgs e)
        {
        }

        private void dataGridView1_KeyDown(object sender, KeyEventArgs e)
        {
            return;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (combvoucherType.Text.ToLower() == "sales" || combvoucherType.Text.ToLower() == "purchase")
            {
                if (MessageBox.Show("This Report will take some time to generate.\nDo you still wants to generate Report?", "Columner Grid",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Information) != System.Windows.Forms.DialogResult.Yes)
                    return; // cancel
            }

            if (combvoucherType.Text.Length == 0)
            {
                LoadData1(dateTimePicker1.Value, dateTimePicker2.Value, combvoucherType.Text);
                //MessageBox.Show("Select voucher type first!");
                return;
            }
            LoadData(dateTimePicker1.Value, dateTimePicker2.Value, combvoucherType.Text);
        }

        private void frmColmnerGrid_KeyUp(object sender, KeyEventArgs e)
        {
            //if (e.KeyCode == Keys.Escape)
            //{
            //    this.Close();
            //    this.Dispose();
            //}
        }

        private void button1_Click(object sender, EventArgs e)
        {

        }

        private void comboCompanyName_SelectedIndexChanged(object sender, EventArgs e)
        {
            comboLedgerName.Items.Clear();
            comboLedgerName.Text = "";

            comboLedgerName.Items.Add("");
            if (Database.OpenConnection(Utility.MaterialConnectionString))
            {
                Database.myreader = Database.GetExecuteReaderCommand("select distinct(LedgerName) from Ledgermaster where companyname = '"
                     + comboCompanyName.Text + "' order by ledgername");
                while (Database.myreader.Read())
                {
                    comboLedgerName.Items.Add(Database.myreader[0].ToString());
                }
                Database.myreader.Close();
                Database.Closeconnection();
            }
            comboLedgerName.SelectedIndex = 0;
        }


        private void combvoucherType_SelectedIndexChanged(object sender, EventArgs e)
        {
            if ((sender as ComboBox).Text == "sales" || (sender as ComboBox).Text == "purchase")
            {
                buttonItemWise.Enabled = true;
                btnSummary.Enabled = true;
                btnSummary.Visible = true;
                checkBox1.Visible = true;
                checkBox2.Visible = true;
                checkBox3.Visible = false;
                chkOnlyInter.Visible = true;
                btnPivot.Visible = true;
                if ((sender as ComboBox).Text == "sales")
                {
                    btnSalesFormat.Visible = true;
                    checkBox3.Visible = true;
                }
                else
                    btnSalesFormat.Visible = false;
            }
            else if ((sender as ComboBox).Text == "")
            {
                btnSummary.Enabled = true;
                btnSummary.Visible = true;
                checkBox1.Visible = false;
                checkBox2.Visible = false;
                checkBox3.Visible = false;
                chkOnlyInter.Visible = false;
                btnPivot.Visible = false;
            }
            else
            {
                btnSalesFormat.Visible = false;
                buttonItemWise.Enabled = false;
                btnSummary.Enabled = false;
                btnSummary.Visible = false;
                checkBox1.Visible = false;
                checkBox2.Visible = false;
                checkBox3.Visible = false;
                chkOnlyInter.Visible = false;
                btnPivot.Visible = false;
            }
        }

        private void buttonItemWise_Click(object sender, EventArgs e)
        {
            filterData.clearFilter();
            filterData.switchOnOff(true);
            dataGridView2.Rows.Clear();
            //dataGridView1.Rows.Clear();
            dataGridView1.DataSource = null;
            dataGridView2.Visible = true;
            dataGridView1.Visible = false;
            dgrSummary.Visible = false;
            dgItemWiseSumm.Visible = false;
            dgsalesformat.Visible = false;
            splitContainer1.Visible = false;
            if (combvoucherType.Text.Length == 0) return;
            if (!Database.OpenConnection(Utility.MaterialConnectionString)) return;
            string vouchertypecondition = "";
            if (combvoucherType.Text.ToString().ToLower() == "sales")
            {
                vouchertypecondition = string.Format(" (SELECT DISTINCT VoucherType FROM  SalesVoucher  WHERE (VoucherType not like '%Return%' "
                    + (checkBox3.Checked == true ? " AND  VoucherType!='Job Invoice'" : "")
                    + ") and Invdate BETWEEN '{0:yyyy-MM-dd}' AND '{1:yyyy-MM-dd}'  union all SELECT DISTINCT VoucherType FROM  PurchaseVoucher WHERE VoucherType like '%Return%' and SysDate BETWEEN '{0:yyyy-MM-dd}' AND '{1:yyyy-MM-dd}') ", dateTimePicker1.Value, dateTimePicker2.Value);
            }
            else if (combvoucherType.Text.ToString().ToLower() == "purchase")
            {
                vouchertypecondition = string.Format(" (SELECT DISTINCT VoucherType FROM  PurchaseVoucher WHERE VoucherType not like '%Return%' and SysDate BETWEEN '{0:yyyy-MM-dd}' AND '{1:yyyy-MM-dd}' union all SELECT DISTINCT VoucherType FROM  SalesVoucher   WHERE VoucherType like '%Return%' and InvDate BETWEEN '{0:yyyy-MM-dd}' AND '{1:yyyy-MM-dd}') ", dateTimePicker1.Value, dateTimePicker2.Value);
            }
            string strcondition = "";
            if (checkBox1.Checked && checkBox2.Checked)
            {
                strcondition = "and isnull(vw_ItemLedgerTransaction.IsInterCompany,'no') = 'no'";
            }
            else if ((checkBox1.Checked == false) && (checkBox2.Checked))
            {
                strcondition = " and vw_ItemLedgerTransaction.InterUnit=0";
            }
            else if ((checkBox1.Checked == true) && (checkBox2.Checked == false))
            {
                strcondition = " and (isnull(vw_ItemLedgerTransaction.IsInterCompany,'no') = 'no' OR vw_ItemLedgerTransaction.InterUnit=1)";
            }

            if (chkOnlyInter.Checked)
            {
                strcondition = " and (isnull(vw_ItemLedgerTransaction.IsInterCompany,'no') = 'yes' and vw_ItemLedgerTransaction.InterUnit!=1)";
            }

            string query = string.Format("SELECT distinct SysDate, spLedger, SupplierName,InvoiceNo,Invdate, VoucherType, VoucherNo,ItemCode, itemMasterName as ItemName,MainGroup, ItemGroupname, SubGroupName,ItemDesc,  case when VoucherType in ('Sales Return','Purchase Return') then  (InwardQty+OutwardQty) else ABS(InwardQty-OutwardQty) end 'Qty', case when VoucherType in ('Sales Return','Purchase Return') then  InwardValue+OutwardValue else ABS(InwardValue-OutwardValue) end 'Amount',case when VoucherType in ('Sales Return','Purchase Return') then  -BillAmount else BillAmount end BillAmount,RowNum,CGSTPer,CGSTAmount,SGSTPer,SGSTAmount,IGSTPer,IGSTAmount,freigtGST,TCS,TotalGST,NewGSTNo,HSNCODE,PER AS UNIT,currencyValue ,ExchangeRate ,ItemName as AccItemGrop ,Netwt,country  FROM vw_ItemLedgerTransaction WHERE CompanyName='{0}' AND SysDate BETWEEN '{2:yyyy-MM-dd}' AND '{3:yyyy-MM-dd}' AND VoucherType IN ({5}) {4} "
            + (strcondition.Length > 0 ? strcondition : "") + " order by SysDate,SupplierName,VoucherNo,rownum", comboCompanyName.Text, table_map[combvoucherType.Text], dateTimePicker1.Value, dateTimePicker2.Value, (comboLedgerName.Text.Length == 0 ? "" : "AND SupplierName='" + comboLedgerName.Text + "'"), vouchertypecondition);

            List<DataGridViewRow> dgvr = new List<DataGridViewRow>();
            ColAcGrupName.HeaderText = combvoucherType.Text + " Group Name";
            double Qty = 0;
            double Amount = 0;
            double q = 0;
            double v = 0;
            double b = 0;
            double cgstamt = 0;
            double sgstamt = 0;
            double igstamt = 0;
            double Freightgstamt = 0;
            double Totalgstamt = 0;
            double BillAmt = 0;
            double tcs = 0;

            Database.GetExecuteReaderCommand(query);
            while (Database.myreader.Read())
            {
                DataGridViewRow dr = (DataGridViewRow)dra.Clone();
                dr.Cells[ColumnDate2.Index].Value = Database.myreader["SysDate"];
                dr.Cells[ColumnParticular.Index].Value = Database.myreader["SupplierName"];
                dr.Cells[ColumnVoucherType.Index].Value = Database.myreader["VoucherType"];
                dr.Cells[ColumnVoucherNo.Index].Value = Database.myreader["VoucherNo"];

                dr.Cells[ColInvoiceNo.Index].Value = Database.myreader["InvoiceNo"];
                dr.Cells[ColInvDate.Index].Value = Database.myreader["Invdate"];

                dr.Cells[ColumnLedger.Index].Value = Database.myreader["spLedger"];
                dr.Cells[ColItemDesc.Index].Value = Database.myreader["ItemDesc"];
                dr.Cells[ColumnItemName.Index].Value = Database.myreader["ItemName"];


                dr.Cells[ColitemDeptt.Index].Value = Database.myreader["MainGroup"];
                dr.Cells[ColItemGrp.Index].Value = Database.myreader["ItemGroupName"];
                dr.Cells[ColItemSubGrp.Index].Value = Database.myreader["SubGroupName"];
                //dr.Cells[ColumnItemName.Index].Value = Database.myreader["itemdescother"]; 

                dr.Cells[ColBillAmt.Index].Value = Database.myreader["BillAmount"];

                q = Utility.SafeConvertToDouble(Database.myreader["Qty"]);
                v = Utility.SafeConvertToDouble(Database.myreader["Amount"]);
                b = Utility.SafeConvertToDouble(Database.myreader["BillAmount"]);

                dr.Cells[ColumnQty.Index].Value = q;
                dr.Cells[ColumnAmount2.Index].Value = v;

                dr.Cells[colCGSTRate.Index].Value = Database.myreader["CGSTPer"];
                dr.Cells[colCGSTAmt.Index].Value = Database.myreader["CGSTAmount"];
                dr.Cells[ColSGSTRate.Index].Value = Database.myreader["SGSTPer"];
                dr.Cells[ColSGSTAmt.Index].Value = Database.myreader["SGSTAmount"];
                dr.Cells[colIGSTrate.Index].Value = Database.myreader["IGSTPer"];
                dr.Cells[ColIGSTAmt.Index].Value = Database.myreader["IGSTAmount"];
                dr.Cells[colFreightGST.Index].Value = Database.myreader["freigtGST"];
                dr.Cells[colTotalGST.Index].Value = Database.myreader["TotalGST"];
                dr.Cells[colTcs.Index].Value = Database.myreader["TCS"];
                dr.Cells[colGSTIn.Index].Value = Database.myreader["NewGSTNo"];
                dr.Cells[ColHSnCode.Index].Value = Database.myreader["HSNCOde"];
                dr.Cells[colUnit.Index].Value = Database.myreader["unit"];

                dr.Cells[ColCur.Index].Value = Database.myreader["currencyValue"];
                dr.Cells[ColExRate.Index].Value = Database.myreader["ExchangeRate"];
                dr.Cells[ColAcGrupName.Index].Value = Database.myreader["AccItemGrop"];
                dr.Cells[ColNetwt.Index].Value = Database.myreader["Netwt"];

                dr.Cells[DtCountry.Index].Value = Database.myreader["Country"];

                Qty += q;
                Amount += v;
                BillAmt += b;
                cgstamt += Utility.SafeConvertToDouble(Database.myreader["CGSTAmount"]);
                sgstamt += Utility.SafeConvertToDouble(Database.myreader["SGSTAmount"]);
                igstamt += Utility.SafeConvertToDouble(Database.myreader["IGSTAmount"]);
                Freightgstamt += Utility.SafeConvertToDouble(Database.myreader["freigtGST"]);
                Totalgstamt += Utility.SafeConvertToDouble(Database.myreader["TotalGST"]);

                tcs += Utility.SafeConvertToDouble(Database.myreader["tcs"]);

                dgvr.Add(dr);
            }
            Database.myreader.Close();

            DataGridViewRow dr2 = (DataGridViewRow)dra.Clone();
            dr2.Cells[ColumnQty.Index].Value = Math.Round(Qty, FrmMain.ERPQtyDigit);
            dr2.Cells[ColumnAmount2.Index].Value = Math.Round(Amount, FrmMain.ERPAmoutnDigit);
            dr2.Cells[ColBillAmt.Index].Value = Math.Round(BillAmt, FrmMain.ERPAmoutnDigit);
            dr2.Cells[colCGSTAmt.Index].Value = Math.Round(cgstamt, FrmMain.ERPAmoutnDigit);
            dr2.Cells[ColSGSTAmt.Index].Value = Math.Round(sgstamt, FrmMain.ERPAmoutnDigit);
            dr2.Cells[ColIGSTAmt.Index].Value = Math.Round(igstamt, FrmMain.ERPAmoutnDigit);
            dr2.Cells[colFreightGST.Index].Value = Math.Round(Freightgstamt, FrmMain.ERPAmoutnDigit);
            dr2.Cells[colTotalGST.Index].Value = Math.Round(Totalgstamt, FrmMain.ERPAmoutnDigit);
            dr2.Cells[colTcs.Index].Value = Math.Round(tcs, FrmMain.ERPAmoutnDigit);
            dr2.DefaultCellStyle.Font = new Font(dataGridView2.Font, FontStyle.Bold);
            dgvr.Add(dr2);

            dataGridView2.Rows.AddRange(dgvr.ToArray());
            CustomizeGrid(dataGridView2);
            dataGridView2.BringToFront();
            dataGridView2.Focus();

        }

        #endregion

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (dataGridView1.Columns.Count == 0) return;
            // RefNo & RefDate
            dataGridView1.Columns[3].Visible = dataGridView1.Columns[4].Visible = checkBoxRef.Checked;
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            if (dataGridView1.Columns.Count == 0) return;
            // Qty & Value
            dataGridView1.Columns[13].Visible = dataGridView1.Columns[14].Visible = checkBoxInventory.Checked;
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            if (dataGridView1.Columns.Count == 0) return;
            // Narration
            dataGridView1.Columns[12].Visible = checkBoxNarration.Checked;
        }

        private void checkBox4_CheckedChanged(object sender, EventArgs e)
        {
            if (dataGridView1.Columns.Count == 0) return;
            // Party Info
            dataGridView1.Columns[8].Visible = dataGridView1.Columns[9].Visible =
            dataGridView1.Columns[10].Visible = dataGridView1.Columns[11].Visible
                = checkBoxPartyInfo.Checked;
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            if (avd.ExpandAll()) (sender as Button).Text = "Hide All";
            else (sender as Button).Text = "Show All";
        }

        private void btnSummary_Click(object sender, EventArgs e)
        {
            try
            {
                filterData.clearFilter();
                filterData.switchOnOff(true);
                dgsalesformat.Visible = false;
                dgrSummary.Visible = true;
                dgItemWiseSumm.Visible = true;
                dataGridView1.Visible = false;
                dataGridView2.Visible = false;
                dgrSummary.DataSource = null;
                dgItemWiseSumm.DataSource = null;
                splitContainer1.Visible = true;
                DataSet dt = new DataSet();
                if (Database.OpenConnection(Utility.MaterialConnectionString))
                {
                    if (combvoucherType.Text == "sales")
                    {
                        dt = Database.GetDataSet("SP_COLUMN_SUMMARY", CommandType.StoredProcedure,
                                       new Database.Parameter("@fromdate", dateTimePicker1.Value.ToString("yyyy-MM-dd")),
                                       new Database.Parameter("@ToDate", dateTimePicker2.Value.ToString("yyyy-MM-dd")),
                                       new Database.Parameter("@CompanyName", comboCompanyName.Text),
                                       new Database.Parameter("@BlnInterCompany", (checkBox1.Checked == true ? 1 : 0)),
                                       new Database.Parameter("@BlnInterUnit", (checkBox2.Checked == true ? 1 : 0)),
                                       new Database.Parameter("@blnJobWokInv", (checkBox3.Checked == true ? 1 : 0))

                                        );
                    }
                    else if (combvoucherType.Text == "purchase")
                    {
                        dt = Database.GetDataSet("SP_COLUMN_SUMMARY_PUR", CommandType.StoredProcedure,
                                       new Database.Parameter("@fromdate", dateTimePicker1.Value.ToString("yyyy-MM-dd")),
                                       new Database.Parameter("@ToDate", dateTimePicker2.Value.ToString("yyyy-MM-dd")),
                                       new Database.Parameter("@CompanyName", comboCompanyName.Text),
                                       new Database.Parameter("@BlnInterCompany", (checkBox1.Checked == true ? 1 : 0)),
                                       new Database.Parameter("@BlnInterUnit", (checkBox2.Checked == true ? 1 : 0))
                                        );
                    }
                    else if (combvoucherType.Text == "")
                    {

                        dt = Database.GetDataSet("sp_Columner_All", CommandType.StoredProcedure,
                                   new Database.Parameter("@DateFrom", dateTimePicker1.Value.ToString("yyyy-MM-dd")),
                                   new Database.Parameter("@DateTo", dateTimePicker2.Value.ToString("yyyy-MM-dd")),
                                   new Database.Parameter("@CompanyName", comboCompanyName.Text),
                                   new Database.Parameter("@LedgerName", null),
                                   new Database.Parameter("@VoucherType", null),
                                   new Database.Parameter("@currType", null));

                        dgsalesformat.Visible = true;
                        dgsalesformat.DataSource = null;
                        dgsalesformat.DataSource = dt.Tables[0];
                        dgrSummary.Visible = false;
                        dgItemWiseSumm.Visible = false;
                        return;
                    }
                    dgrSummary.DataSource = dt.Tables[0];
                    //dgItemWiseSumm.DataSource = dt.Tables[1];

                    BindingSource datasource = new BindingSource(dt.Tables[1], null);
                    dgItemWiseSumm.DataSource = datasource;

                    dgItemWiseSumm.Columns[3].DefaultCellStyle.Alignment = DataGridViewContentAlignment.BottomRight;
                    dgItemWiseSumm.Columns[4].DefaultCellStyle.Alignment = DataGridViewContentAlignment.BottomRight;
                    dgItemWiseSumm.Columns[5].DefaultCellStyle.Alignment = DataGridViewContentAlignment.BottomRight;

                    // Add the AutoFilter header cell to each column.
                    foreach (DataGridViewColumn col in dgItemWiseSumm.Columns)
                    {
                        col.HeaderCell = new
                            DataGridViewAutoFilterColumnHeaderCell(col.HeaderCell);
                    }
                    dgrSummary.Focus();
                    dgItemWiseSumm.BringToFront();
                    CustomizeGrid(dgrSummary);
                    CustomizeGrid(dgItemWiseSumm);
                    dgrSummary.BringToFront();
                    dgItemWiseSumm.BringToFront();
                    dgrSummary.Focus();

                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);


            }
        }

        private void CustomizeGrid(DataGridView grd)
        {
            foreach (DataGridViewColumn col in grd.Columns)
            {
                try
                {
                    if (col.Name.ToLower().Contains("date"))
                    {
                        grd.Columns[col.Index].DefaultCellStyle.Format = mmCommonFunction.ERPDateSettings();
                    }
                    else
                    {
                        double result;
                        string v1 = Convert.ToString(grd[col.Index, 0].Value);
                        if (double.TryParse(v1, out result))
                        {
                            grd.Columns[col.Index].DefaultCellStyle.Alignment = DataGridViewContentAlignment.BottomRight;
                        }
                    }
                }
                catch (Exception ex)
                {

                }
            }
        }

        private void btnSalesFormat_Click(object sender, EventArgs e)
        {
            try
            {
                filterData.clearFilter();
                filterData.switchOnOff(true);
                dgsalesformat.Visible = true;
                dgrSummary.Visible = false;
                dgItemWiseSumm.Visible = false;
                dataGridView1.Visible = false;
                dataGridView2.Visible = false;
                dgrSummary.DataSource = null;
                dgItemWiseSumm.DataSource = null;
                splitContainer1.Visible = false;

                DataSet dt = new DataSet();
                if (Database.OpenConnection(Utility.MaterialConnectionString))
                {
                    dt = Database.GetDataSet("select * from vw_SalesVoucher_OEL6 where companyname='" + comboCompanyName.Text +
                        "' and Invdate between '" + dateTimePicker1.Value.ToString("yyyy-MM-dd") + "' and '" +
                        dateTimePicker2.Value.ToString("yyyy-MM-dd") + "'");

                    dgsalesformat.DataSource = dt.Tables[0];
                    foreach (DataGridViewColumn col in dgItemWiseSumm.Columns)
                    {
                        col.HeaderCell = new
                            DataGridViewAutoFilterColumnHeaderCell(col.HeaderCell);
                    }
                }
                dgsalesformat.Visible = true;
                CustomizeGrid(dgsalesformat);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);


            }
        }

        private void btnPivot_Click(object sender, EventArgs e)
        {
            try
            {
                filterData.clearFilter();
                filterData.switchOnOff(true);
                dgsalesformat.Visible = true;
                dgrSummary.Visible = false;
                dgItemWiseSumm.Visible = false;
                dataGridView1.Visible = false;
                dataGridView2.Visible = false;
                dgrSummary.DataSource = null;
                dgItemWiseSumm.DataSource = null;
                splitContainer1.Visible = false;
                DataSet dt = new DataSet();
                if (combvoucherType.Text.Length == 0) return;
                if (!Database.OpenConnection(Utility.MaterialConnectionString)) return;
                string vouchertypecondition = "";
                if (combvoucherType.Text.ToString().ToLower() == "sales")
                {
                    vouchertypecondition = string.Format(" (SELECT DISTINCT VoucherType FROM  SalesVoucher  WHERE (VoucherType not like '%Return%' "
                        + (checkBox3.Checked == true ? " AND  VoucherType!='Job Invoice'" : "")
                        + ") and Invdate BETWEEN '{0:yyyy-MM-dd}' AND '{1:yyyy-MM-dd}'  union all SELECT DISTINCT VoucherType FROM  PurchaseVoucher WHERE VoucherType like '%Return%' and SysDate BETWEEN '{0:yyyy-MM-dd}' AND '{1:yyyy-MM-dd}') ", dateTimePicker1.Value, dateTimePicker2.Value);
                }
                else if (combvoucherType.Text.ToString().ToLower() == "purchase")
                {
                    vouchertypecondition = string.Format(" (SELECT DISTINCT VoucherType FROM  PurchaseVoucher WHERE VoucherType not like '%Return%' and SysDate BETWEEN '{0:yyyy-MM-dd}' AND '{1:yyyy-MM-dd}' union all SELECT DISTINCT VoucherType FROM  SalesVoucher   WHERE VoucherType like '%Return%' and InvDate BETWEEN '{0:yyyy-MM-dd}' AND '{1:yyyy-MM-dd}') ", dateTimePicker1.Value, dateTimePicker2.Value);
                }
                string strcondition = "";
                if (checkBox1.Checked && checkBox2.Checked)
                {
                    strcondition = "and isnull(vw_ItemLedgerTransaction.IsInterCompany,'no') = 'no'";
                }
                else if ((checkBox1.Checked == false) && (checkBox2.Checked))
                {
                    strcondition = " and vw_ItemLedgerTransaction.InterUnit=0";
                }
                else if ((checkBox1.Checked == true) && (checkBox2.Checked == false))
                {
                    strcondition = " and (isnull(vw_ItemLedgerTransaction.IsInterCompany,'no') = 'no' OR vw_ItemLedgerTransaction.InterUnit=1)";
                }

                if (chkOnlyInter.Checked)
                {
                    strcondition = " and (isnull(vw_ItemLedgerTransaction.IsInterCompany,'no') = 'yes' and vw_ItemLedgerTransaction.InterUnit!=1)";
                }

                string query = string.Format("select case when MainGroup is null then 'Total :' else MainGroup end as MainGroup, case when ItemGroupname is null and MainGroup is not null then 'Total :'   else ItemGroupname end ItemGroupname,   ItemName ,round(sum(Qty),3) as Qty,  round(sum(Amount),2) as BasicAmt,case when isnull(sum(Qty),0)=0 then 0 else round(sum(Amount)/sum(Qty),2) end as AvgRate From ( SELECT distinct SysDate, spLedger, SupplierName,InvoiceNo,Invdate, VoucherType, VoucherNo,ItemCode, itemMasterName as ItemName,MainGroup, ItemGroupname, SubGroupName,ItemDesc,  case when VoucherType in ('Sales Return','Purchase Return') then  (InwardQty+OutwardQty) else ABS(InwardQty-OutwardQty) end 'Qty', case when VoucherType in ('Sales Return','Purchase Return') then  InwardValue+OutwardValue else ABS(InwardValue-OutwardValue) end 'Amount',case when VoucherType in ('Sales Return','Purchase Return') then  -BillAmount else BillAmount end BillAmount,RowNum,CGSTPer,CGSTAmount,SGSTPer,SGSTAmount,IGSTPer,IGSTAmount,freigtGST,TCS,TotalGST,NewGSTNo,HSNCODE,PER AS UNIT,currencyValue ,ExchangeRate ,ItemName as AccItemGrop  FROM vw_ItemLedgerTransaction WHERE CompanyName='{0}' AND SysDate BETWEEN '{2:yyyy-MM-dd}' AND '{3:yyyy-MM-dd}' AND VoucherType IN ({5}) {4} "
                + (strcondition.Length > 0 ? strcondition : "") + " ) A group by GROUPING SETS ((MainGroup, ItemGroupname, ItemName),(MainGroup),())", comboCompanyName.Text, table_map[combvoucherType.Text], dateTimePicker1.Value, dateTimePicker2.Value, (comboLedgerName.Text.Length == 0 ? "" : "AND SupplierName='" + comboLedgerName.Text + "'"), vouchertypecondition);


                dgsalesformat.Columns.Clear();
                DataTable dt1 = Database.GetDataTable(query);
                dgsalesformat.DataSource = null;
                dgsalesformat.DataSource = dt1;
                dgsalesformat.BringToFront();
                dgsalesformat.Focus();

                foreach (DataGridViewColumn col in this.dgsalesformat.Columns)
                {
                    try
                    {
                        if (col.Name.ToLower().Contains("date"))
                        {
                            dgsalesformat.Columns[col.Index].DefaultCellStyle.Format = mmCommonFunction.ERPDateSettings();
                        }
                        else
                        {
                            double result;
                            string v = Convert.ToString(dgsalesformat[col.Index, 0].Value);
                            if (double.TryParse(v, out result))
                            {
                                dgsalesformat.Columns[col.Index].DefaultCellStyle.Alignment = DataGridViewContentAlignment.BottomRight;
                                if (col.Name.ToLower().Contains("qty"))                                
                                    dgsalesformat.Columns[col.Index].DefaultCellStyle.Format = "N3";
                                else
                                    dgsalesformat.Columns[col.Index].DefaultCellStyle.Format = "N2";
                            }
                        }
                    }
                    catch (Exception ex)
                    {

                    }
                }

                foreach (DataGridViewRow row in this.dgsalesformat.Rows)
                {
                    if (row.Cells[0].Value.ToString().Contains("Total"))
                    {
                        row.DefaultCellStyle.BackColor = Color.RoyalBlue;
                        row.DefaultCellStyle.ForeColor = Color.White;
                    }
                    else if (row.Cells[1].Value.ToString().Contains("Total"))
                    {
                        row.DefaultCellStyle.BackColor = Color.RoyalBlue;
                        row.DefaultCellStyle.ForeColor = Color.White;
                    }

                }
                 
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}

