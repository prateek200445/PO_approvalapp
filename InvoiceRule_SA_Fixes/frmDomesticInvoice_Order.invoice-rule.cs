// =============================================================================
// INVOICE-RULE COPY — do not compile next to original.
// Replace frmDomesticInvoice_Order.cs after review. Search tag: INVOICE-RULE
//
// THIS FILE: MINUS Despatch after MappPackingList insert. On Update PLUS old map then MINUS new.
// Item.StkInHand not touched.
//
// LINE MAP (this copy):
//   1052-1053 Save minus Despatch after map
//   2147 / 2302 Update remember old packing list
//   2725-2732 Update plus old, rewrite map, minus new
//   4150 helper InvoiceRule_AdjustDespatchGodown
// =============================================================================
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
    public partial class frmDomesticInvoice_Order : Form
    {
        DataTable dtGroupName;
        DataTable dtSubGroupName;
        ComboBox comboBox;
        bool isupdate = false;
        DataTable dtIMaster;
        bool isagstMarketing = false;
        private DataSet ds = new DataSet("myDs");
        private DataTable dt = new DataTable("myDT");
        string CompanyCode = "";

        bool isupdatefororder = false;

        ArrayList ArrCommodity = new ArrayList();

        public int TotalCount = 0;
        double Excise = 0;
        double Educess = 0;
        double HiEduCess = 0;
        double AddVat = 0;
        double Vat = 0;
        double Friegth = 0;
        double TCS = 0;

        public frmDomesticInvoice_Order()
        {
            InitializeComponent();
            Form_Load();
        }

        private void Form_Load()
        {

            try
            {
                groupAvailBenefit.Visible = false;
                checkIsPurchaseReturn.Checked = false;
                if (DateTime.Now >= Convert.ToDateTime("2019-01-01"))
                    checkIsPurchaseReturn.Visible = false;
                else
                    checkIsPurchaseReturn.Visible = true;

                //DialogResult drs = MessageBox.Show("Do you want to Avail the Benefit 39-A", "Avail Benefit 39-A"
                //    , MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                //if (drs.CompareTo(DialogResult.Yes) == 0)
                //    checkAvailBenefit.Checked = true;

                dateTimePickerDateTimeRemoval.Format = DateTimePickerFormat.Custom;
                dateTimePickerDateTimeRemoval.CustomFormat = DespatchCommonFunction.ERPDateSettings();

                dateTimePickerInvoice.Format = DateTimePickerFormat.Custom;
                dateTimePickerInvoice.CustomFormat = DespatchCommonFunction.ERPDateSettings();

                dateTimePickerDespatchDate.Format = DateTimePickerFormat.Custom;
                dateTimePickerDespatchDate.CustomFormat = DespatchCommonFunction.ERPDateSettings();

                dateTimePickerChallanDate.Format = DateTimePickerFormat.Custom;
                dateTimePickerChallanDate.CustomFormat = DespatchCommonFunction.ERPDateSettings();

                dateTimePickerVehicalDate.Format = DateTimePickerFormat.Custom;
                dateTimePickerVehicalDate.CustomFormat = DespatchCommonFunction.ERPDateSettings();

                dateTimePickerLRNo.Format = DateTimePickerFormat.Custom;
                dateTimePickerLRNo.CustomFormat = DespatchCommonFunction.ERPDateSettings();

                dateTimePickerInvDate.Format = DateTimePickerFormat.Custom;
                dateTimePickerInvDate.CustomFormat = DespatchCommonFunction.ERPDateSettings();

                dateTimeBuyerOrderDate.Format = DateTimePickerFormat.Custom;
                dateTimeBuyerOrderDate.CustomFormat = DespatchCommonFunction.ERPDateSettings();

                btnUpdate.Enabled = false;

                comboTypeInvoice.SelectedIndex = 0;
                if (Database.OpenConnection(Utility.DespacthConnectionString))
                {
                    DespatchCommonFunction.GetCompanyList(comboCompanyName);
                    comboCompanyName.Text = frmDefaultVale.CompanyName;

                    DespatchCommonFunction.GetDepttName(comboDeptt);
                    string strsql = "";
                    ////strsql = "Select * from vw_CompanyMaster where (invoicecategory = 'Domestic'  or invoicecategory = '*') order by CompanyName";
                    //strsql = "Select companyname from vw_CompanyMaster where (invoicecategory = 'Domestic'  or invoicecategory = '*')  " +
                    //      " and isnull(IsFreeze,'no')='no' and (('" + dateTimePickerInvDate.Value.ToString("yyyy-MM-dd") +
                    //       "' between fromdate and todate) or (fromdate <= '" +
                    //       dateTimePickerInvDate.Value.ToString("yyyy-MM-dd") + "' and todate is null ))  order by CompanyName";

                    //DespatchCommonFunction.Loadcombo(comboBuyerName, strsql, "CompanyName", "CompanyName", Utility.DespacthConnectionString);

                    strsql = "";
                    strsql = "select distinct paymenttermname from MaterialProcessing..PaymentTermsMaster where Type='Sales' and isdelflag=0 order by paymenttermname";
                    DespatchCommonFunction.Loadcombo(textTermsofPayment, strsql, "paymenttermname", "paymenttermname", Utility.DespacthConnectionString);

                    Database.myreader = Database.GetExecuteReaderCommand("select * from MaterialProcessing..excisemaster");
                    if (Database.myreader.Read())
                    {
                        textExDuty.Text = Database.myreader[0].ToString();
                        textEduCess.Text = Database.myreader[1].ToString();
                        textSecHiEdu.Text = Database.myreader[2].ToString();
                    }
                    Database.myreader.Close();
                    DespatchCommonFunction.GetTransportMaster(comboTranporterName);
                    strsql = "";
                    strsql = "Select Distinct(vehicalNo) as vehicalNo from DomesticInvoice with (NOLOCK) order by vehicalNo";
                    DespatchCommonFunction.Loadcombo(comboVehicalNo, strsql, "vehicalNo", "vehicalNo", Utility.DespacthConnectionString);
                    strsql = "";
                    strsql = "select currency from currencytype ";
                    DespatchCommonFunction.Loadcombo(comboCurrency, strsql, "currency", "currency", Utility.DespacthConnectionString);

                    strsql = "";
                    strsql = "Select Distinct(Destination) as Destination from DomesticInvoice   with (NOLOCK) order by Destination";
                    DespatchCommonFunction.Loadcombo(comboDestination, strsql, "Destination", "Destination", Utility.DespacthConnectionString);

                    strsql = "Select Distinct(StateName) as StateName from StateMaster WITH(NOLOCK)  ";
                    DespatchCommonFunction.Loadcombo(comboState, strsql, "StateName", "StateName", Utility.DespacthConnectionString);

                    Database.Closeconnection();
                }
                if (comboBuyerName.Items.Count > 0)
                    comboBuyerName.SelectedIndex = 0;

                if (comboTranporterName.Items.Count > 0)
                    comboTranporterName.SelectedIndex = 0;

                if (comboCurrency.Items.Count > 0)
                    comboCurrency.SelectedIndex = 0;

                //comboType.SelectedIndex = 0;

                comboExicseApp.SelectedIndex = 0;
                comboVatApp.SelectedIndex = 0;

                groupInsurance.Visible = false;

                textSuffix.Text = frmDefaultVale.Year;

                fillData();


                string strsql1 = "";
                strsql1 = "select LedgerName from ledgermaster where isinvoice = 'yes' and companyname = '"
                    + comboCompanyName.Text + "'";
                DespatchCommonFunction.Loadcombo(comboAccountHead, strsql1, "LedgerName", "LedgerName", Utility.MaterialConnectionString);
                Database.Closeconnection();

                dispfrm.Visible = false;
                dispfrm_add.Visible = false;
                combodispfrm.Visible = false;
                textdispfrm_add.Visible = false;

            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
            }
        }


        void fillData(bool blnupdate = false)
        {

            if (Database.OpenConnection(Utility.DespacthConnectionString))
            {
                //OleDbDataAdapter adapter1 = new OleDbDataAdapter("select BillD.ProductName,BillD.ProductID,BillD.Saletype,BillD.InQty,BillD.SBtype,BillD.Dis1,BillD.Type,BillD.InPrice from BillDetails BillD", database);
                if (blnupdate)
                {
                    Database.myadapter = Database.GetAdapterCommand("Select CommodityName from Commodity where companyname = '" + comboCompanyName.Text
                        + "'  order by CommodityName");
                }
                else
                {
                    Database.myadapter = Database.GetAdapterCommand("Select CommodityName from Commodity where companyname = '" + comboCompanyName.Text
                        + "' and (('" + dateTimePickerInvDate.Value.ToString("yyyy-MM-dd")
                        + "' between nFromDate and nToDate) or (nFromDate <= '"
                        + dateTimePickerInvDate.Value.ToString("yyyy-MM-dd") + "' and nToDate is null )) "
                        + " order by CommodityName");
                }
                DataTable dt = new DataTable();
                Database.myadapter.Fill(dt);
                DCommodity.DataSource = dt;
                DCommodity.ValueMember = dt.Columns[0].ColumnName.ToString();
                DCommodity.DisplayMember = dt.Columns[0].ColumnName.ToString();

                Database.myadapter = Database.GetAdapterCommand("select distinct(DepttName) from itemdeptt order by depttname");
                DataTable dt1 = new DataTable();

                Database.myadapter.Fill(dt1);

                DtDeptt.DataSource = dt1;
                DtDeptt.ValueMember = dt1.Columns[0].ColumnName.ToString();
                DtDeptt.DisplayMember = dt1.Columns[0].ColumnName.ToString();
            }

            //if (Database.OpenConnection(Utility.MaterialConnectionString))
            {

                Database.myadapter = Database.GetAdapterCommand("Select UnitName from MaterialProcessing.dbo.Unit order by UnitName");
                DataTable dt = new DataTable();
                Database.myadapter.Fill(dt);

                DUOM.DataSource = dt;
                DUOM.ValueMember = dt.Columns[0].ColumnName.ToString();
                DUOM.DisplayMember = dt.Columns[0].ColumnName.ToString();

                #region ITem Master Added 22.06.2020
                dtIMaster = new DataTable();
                dtIMaster = DespatchCommonFunction.GetItemMaster(dateTimePickerInvDate.Value.ToString("yyyy-MM-dd"), comboCompanyName.Text);
                colItemCode.DataSource = dtIMaster;
                colItemCode.ValueMember = dtIMaster.Columns[0].ColumnName.ToString();
                colItemCode.DisplayMember = dtIMaster.Columns[1].ColumnName.ToString();

                DataTable dtdeptt = DespatchCommonFunction.GetItemMainGrp();
                colMainGrp.DataSource = dtdeptt;
                colMainGrp.ValueMember = dtdeptt.Columns[0].ColumnName.ToString();
                colMainGrp.DisplayMember = dtdeptt.Columns[0].ColumnName.ToString();

                dtGroupName = DespatchCommonFunction.GetItemGroupName();
                dtSubGroupName = DespatchCommonFunction.GetItemSubGroupName();

                colGroupName.DataSource = dtGroupName;
                colGroupName.ValueMember = dtGroupName.Columns[1].ColumnName.ToString();
                colGroupName.DisplayMember = dtGroupName.Columns[1].ColumnName.ToString();

                colSubGrp.DataSource = dtSubGroupName;
                colSubGrp.ValueMember = dtSubGroupName.Columns[2].ColumnName.ToString();
                colSubGrp.DisplayMember = dtSubGroupName.Columns[2].ColumnName.ToString();
                #endregion

            }

        }

        private void frmDomesticInvoice_Load(object sender, EventArgs e)
        {
        }

        private bool MatchCommodity_Theding(int count)
        {
            string THeading = "";
            ArrayList ArrTHeading = new ArrayList();

            for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
            {
                if (dataGridView1.Rows[i].Cells[1].FormattedValue.ToString() != "" &&
                   dataGridView1.Rows[i].Cells[3].FormattedValue.ToString() != "" &&
                   dataGridView1.Rows[i].Cells[4].FormattedValue.ToString() != "" &&
                   dataGridView1.Rows[i].Cells[5].FormattedValue.ToString() != "")
                {
                    if (Database.OpenConnection(Utility.DespacthConnectionString))
                    {
                        Database.myreader = Database.GetExecuteReaderCommand("select THeading from commodity where CommodityName ='"
                              + dataGridView1.Rows[i].Cells[1].Value.ToString() + "' and companyname = '"
                              + comboCompanyName.Text + "'");
                        while (Database.myreader.Read())
                            ArrTHeading.Add(Database.myreader[0].ToString());
                        Database.myreader.Close();
                        // Database.Closeconnection();
                    }
                }
            }
            if (ArrTHeading.Count > 0)
                THeading = ArrTHeading[0].ToString();
            for (int i = 1; i < ArrTHeading.Count; i++)
            {
                if (ArrTHeading[0].ToString() != ArrTHeading[i].ToString())
                {
                    if (!THeading.Contains(ArrTHeading[i].ToString()))
                        THeading += "/" + ArrTHeading[i].ToString();
                }
            }
            textHeading.Text = THeading;
            return true;
        }


        private void UpdateInsurancePolicy(double TotalAmt, bool CheckInsurence)
        {
            if (CheckInsurence)
            {
                TotalAmt = (TotalAmt * 110) / 100;
                TotalAmt = Math.Round(TotalAmt, 2);
                Database.GetExecuteNonQueryCommand("update insurancepolicy set pendinglimit = Pendinglimit - " + TotalAmt
                  + " where policyno ='" + textInsurance.Text + "' and companyname = '" + comboCompanyName.Text + "'");
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            #region 19-08-2020 lock as per accounting voucher type
            if (!Utility.CheckAccountVoucherDateFreeze(dateTimePickerInvDate.Value, comboCompanyName.Text, "sales"))
            {
                MessageBox.Show(AccountCommonVariable.ErrMsg.DATE_FREEZE + "\nPlease check Invoice Date");
                return;
            }
            if (!Utility.CheckAccountVoucherDateFreeze(dateTimePickerInvoice.Value, comboCompanyName.Text, "sales"))
            {
                MessageBox.Show(AccountCommonVariable.ErrMsg.DATE_FREEZE + "\nPlease check date and Time of Invoice");
                return;
            }
            if (!Utility.CheckAccountVoucherDateFreeze(dateTimePickerDespatchDate.Value, comboCompanyName.Text, "sales"))
            {
                MessageBox.Show(AccountCommonVariable.ErrMsg.DATE_FREEZE + "\nPlease check despatch date");
                return;
            }
            if (!Utility.CheckAccountVoucherDateFreeze(dateTimePickerDateTimeRemoval.Value, comboCompanyName.Text, "sales"))
            {
                MessageBox.Show(AccountCommonVariable.ErrMsg.DATE_FREEZE + "\nPlease check date and Time of Removal");
                return;
            }
            #endregion

            int VenCnt = DespatchCommonFunction.CustomerLedgerName(comboBuyerName.Text, comboCompanyName.Text);
            if (VenCnt == 0)
            {
                MessageBox.Show("No Account Ledger Found for this customer.\nPlease Ask Account Dept to create Ledger for this customer:- [" + comboBuyerName.Text + "]");
                return;
            }

            if (Database.OpenConnection(Utility.DespacthConnectionString))
            {
                if (dateTimePickerInvDate.Value >= Convert.ToDateTime("2017-07-01 00:00:00"))
                {
                    string newgstno = Convert.ToString(Database.GetExecuteNonQueryCommand_retrun("select isnull(NewGSTNo,'') as NewGSTNo from CompanyMaster where CompanyName='"
                        + comboBuyerName.Text + "'"));
                    if (newgstno.Length == 0)
                    {
                        MessageBox.Show("Please Enter GSTIN No for This Customer");
                        return;
                    }
                    if (newgstno == DespatchCommonFunction.CompanyGSTNo(comboCompanyName.Text))
                    {
                        MessageBox.Show("Domestic invoice will not geneated for SAME GSTIN Company.\nPlease Created Delivery Challan for branch transfer");
                        return;
                    }
                    for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                    {
                        string hsncode = Convert.ToString(dataGridView1.Rows[i].Cells[colHSNCode.Index].Value);
                        if (hsncode == "")
                        {
                            MessageBox.Show("Please Enter HSN Code for Item :" + dataGridView1.Rows[i].Cells[DCommodity.Index].Value);
                            return;
                        }
                        if ((hsncode.Length < 6) && (dateTimePickerInvDate.Value >= Convert.ToDateTime("2021-04-01")))
                        {
                            MessageBox.Show("Please Enter 6 Digit HSN Code for Item :" + dataGridView1.Rows[i].Cells[DCommodity.Index].Value);
                            return;
                        }

                        string itemcode = Convert.ToString(dataGridView1.Rows[i].Cells[colItemCode.Index].Value);
                        if (itemcode.Length == 0)
                        {
                            MessageBox.Show("Please Select Item ");
                            return;
                        }
                        if (dataGridView1.Rows[i].Cells[DCommodity.Index].FormattedValue.ToString() == "")
                        {
                            MessageBox.Show("Please Select Proper Commodity ");
                            return;
                        }
                        if (dataGridView1.Rows[i].Cells[DUOM.Index].FormattedValue.ToString() == "")
                        {
                            MessageBox.Show("Please Select Unit ");
                            return;
                        }
                        if (dataGridView1.Rows[i].Cells[DOrderQty.Index].FormattedValue.ToString() == "")
                        {
                            MessageBox.Show("Please Enter Order Qty");
                            return;
                        }
                        if (dataGridView1.Rows[i].Cells[DNetWt.Index].FormattedValue.ToString() == "")
                        {
                            MessageBox.Show("Please Enter Netwt");
                            return;
                        }
                        if (dataGridView1.Rows[i].Cells[DUnitPrice.Index].FormattedValue.ToString() == "")
                        {
                            MessageBox.Show("Please Enter Unit Price ");
                            return;
                        }
                        if (dataGridView1.Rows[i].Cells[DUOM.Index].FormattedValue.ToString().ToLower() == "kgs")
                        {
                            if (dataGridView1.Rows[i].Cells[DOrderQty.Index].FormattedValue.ToString() !=
                                dataGridView1.Rows[i].Cells[DNetWt.Index].FormattedValue.ToString())
                            {
                                MessageBox.Show("Please Enter Proper Netwt ");
                                return;
                            }
                        }
                    }
                }
            }

            bool blntans = false;
            string InvType = "";
            bool isMultilayer = false;

            if (comboDeptt.Text == "")
            {
                MessageBox.Show("Please enter Value in DepttName");
                return;
            }

            //if (comboDeptt.Text == "Multilayer")
            //    isMultilayer = true;
            //if (comboDeptt.Text == "Multilayer")
            //    isMultilayer = true;

            DateTime invdatetemc = new DateTime();
            DateTime invdatetemc2 = new DateTime();
            isupdatefororder = false;
            if (!FrmMain.IsCreateDomesticInvoice)
                MessageBox.Show("You have no right to save Domestic Invoice ");
            else if (textBoxLRNo.Text == "")
                MessageBox.Show("Please enter Value in LRNo");
            else if (comboState.Text == "")
                MessageBox.Show("Please select State ");
            else if (!(textTermsofPayment.Items.Contains(textTermsofPayment.Text.Trim())))
                MessageBox.Show("Please select proper payment terms or enter payment term in payment master");
            else
            {
                #region PackingList
                string despackinglist = "";
                if (isMultilayer == false)
                {
                    if (DespatchCommonFunction.AllowMappingPackinglist(comboCompanyName.Text) > 0)
                    {
                        //check with Packing List
                        double TotalNetwt = 0;
                        for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                        {
                            //DNetWt
                            if (Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[DNetWt.Index].Value.ToString()) > 0)
                            {
                                TotalNetwt += Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[DNetWt.Index].Value.ToString());
                            }
                            else
                            {
                                TotalNetwt += Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[DOrderQty.Index].Value.ToString());
                            }
                        }
                        if (!DespatchCommonFunction.GetPackingListInvoiceNo2(comboCompanyName.Text, comboBuyerName.Text, TotalNetwt, dataGridView1, colItemCode.Name, DNetWt.Name, ref despackinglist))
                        {
                            return;
                        }
                    }
                }
                #endregion
                invdatetemc2 = invdatetemc;
                if (Database.OpenConnection(Utility.DespacthConnectionString))
                {
                    Database.myreader = Database.GetExecuteReaderCommand("Select InvNo,CAST(FLOOR(CAST(InvDateTime AS float)) AS datetime) from DomesticInvoice   where suffix = '"
                            + textSuffix.Text + "' and companyname = '" + comboCompanyName.Text + "' order by InvNo desc");
                    if (Database.myreader.Read())
                        invdatetemc = Convert.ToDateTime(Database.myreader[1].ToString());
                    Database.myreader.Close();
                    Database.Closeconnection();
                }
                //Database.myreader = Database.GetExecuteReaderCommand("Select InvNo");
                if (invdatetemc <= invdatetemc2)
                    invdatetemc = System.DateTime.Now;


                invdatetemc = invdatetemc.AddDays(-1);
                if (invdatetemc >= Convert.ToDateTime(dateTimePickerInvDate.Value))
                {
                    MessageBox.Show("Please change Invoice date ");
                }
                else
                {

                    string agstmarketing = "";
                    if (isagstMarketing)
                        agstmarketing = "yes";
                    else
                        agstmarketing = "no";
                    double total = 0;
                    bool IsWt = false;
                    bool IsBlank = false;
                    try
                    {

                        for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                        {
                            if (dataGridView1.Rows[i].Cells[DNoofPkg.Index].FormattedValue.ToString() != ""
                                && dataGridView1.Rows[i].Cells[DtDeptt.Index].FormattedValue.ToString() != ""
                                && dataGridView1.Rows[i].Cells[DUOM.Index].FormattedValue.ToString() != ""
                                && dataGridView1.Rows[i].Cells[DOrderQty.Index].FormattedValue.ToString() != ""
                                && dataGridView1.Rows[i].Cells[DUnitPrice.Index].FormattedValue.ToString() != ""
                                && dataGridView1.Rows[i].Cells[DTotal.Index].FormattedValue.ToString() != ""
                                && dataGridView1.Rows[i].Cells[DNetWt.Index].FormattedValue.ToString() != ""
                                && dataGridView1.Rows[i].Cells[DGrossWt.Index].FormattedValue.ToString() != "")
                            {

                                double t = Convert.ToDouble(dataGridView1.Rows[i].Cells[DOrderQty.Index].Value.ToString());
                                t = Convert.ToDouble(dataGridView1.Rows[i].Cells[DUnitPrice.Index].Value.ToString());
                                t = Convert.ToDouble(dataGridView1.Rows[i].Cells[DTotal.Index].Value.ToString());
                                t = Convert.ToDouble(dataGridView1.Rows[i].Cells[DNetWt.Index].Value.ToString());
                                t = Convert.ToDouble(dataGridView1.Rows[i].Cells[DGrossWt.Index].Value.ToString());

                                if (dataGridView1.Rows[i].Cells[DOrderQty.Index].FormattedValue.ToString() == "")
                                {
                                    MessageBox.Show("Please enter value in Qty");
                                    IsBlank = true;
                                }
                                if (dataGridView1.Rows[i].Cells[DUnitPrice.Index].FormattedValue.ToString() == "")
                                {
                                    MessageBox.Show("Please enter value in Rate");
                                    IsBlank = true;
                                }
                                if (dataGridView1.Rows[i].Cells[DTotal.Index].FormattedValue.ToString() == "")
                                {
                                    MessageBox.Show("Please enter value in Amount");
                                    IsBlank = true;
                                }
                                if (dataGridView1.Rows[i].Cells[DNetWt.Index].FormattedValue.ToString() == "")
                                {
                                    MessageBox.Show("Please enter value in NetWt");
                                    IsBlank = true;
                                }
                                if (dataGridView1.Rows[i].Cells[DGrossWt.Index].FormattedValue.ToString() == "")
                                {
                                    MessageBox.Show("Please enter value in Grosswt");
                                    IsBlank = true;
                                }


                            }
                        }
                        if (OrderQuantity())
                        {
                            MessageBox.Show("Inv Qty is greater than Pending Qty");
                            IsBlank = true;
                        }
                        if (!IsBlank)
                        {
                            bool isSave = false;
                            Int32 InvNo = 0;

                            if (dateTimePickerInvDate.Value > Convert.ToDateTime("2017-06-30 00:00:00"))
                            {
                                total = CalculationGST();
                            }
                            else
                            {
                                total = Calculation();
                            }

                            textGrdTotal.Text = total.ToString();
                            IsWt = WeightDifferences();
                            bool IsValidBuyerName = false;
                            if (Database.OpenConnection(Utility.DespacthConnectionString))
                            {
                                Database.myreader = Database.GetExecuteReaderCommand("Select * from CompanyMaster where CompanyName = '"
                                     + comboBuyerName.Text + "'");
                                if (Database.myreader.Read())
                                    IsValidBuyerName = true;
                                Database.myreader.Close();
                                Database.Closeconnection();
                            }
                            if (!IsValidBuyerName)
                                MessageBox.Show(" Invalid Buyer Name");
                            else if (comboTypeInvoice.Text == "")
                                MessageBox.Show("Type of Invoice Cannot Be Blank Please select TAX/RETAIL Invoice");
                            else if (TotalCount <= 0)
                                MessageBox.Show("Please at leaset Fill One Item Details in Item Information");
                            else if (comboDeptt.Text == "")
                                MessageBox.Show("Please enter Value in DepttName");
                            else if (textBoxLRNo.Text == "")
                                MessageBox.Show("Please enter Value in LRNo");
                            else if (IsWt)
                                MessageBox.Show("Net Weight should be lesser than Gross Weight");
                            else if (!MatchCommodity_Theding(10))
                                MessageBox.Show("Please select proper Commodity,THeading of Commodity would be require same",
                                         "THeading should be macth", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            else
                            {
                                if (Database.OpenConnection(Utility.DespacthConnectionString))
                                {

                                    if (dateTimePickerInvDate.Value < Convert.ToDateTime("2017-07-01 00:00:00"))
                                    {
                                        Database.myreader = Database.GetExecuteReaderCommand("Select Isnull(MAX(InvNo),0) as InvNo from DomesticInvoice where suffix = '"
                                            + textSuffix.Text + "' and companyname = '" + comboCompanyName.Text + "' ");
                                        if (Database.myreader.Read())
                                            InvNo = Convert.ToInt32(Database.myreader["InvNo"].ToString());
                                        Database.myreader.Close();
                                        InvNo = InvNo + 1;
                                    }
                                    else
                                    {

                                        //Database.myreader = Database.GetExecuteReaderCommand("Select Isnull(MAX(InvNo),0) as InvNo from DomesticInvoice where suffix = '"
                                        //    + textSuffix.Text + "' and companyname = '" + comboCompanyName.Text + "'");
                                        //if (Database.myreader.Read())
                                        //    InvNo = Convert.ToInt32(Database.myreader["InvNo"].ToString());
                                        //Database.myreader.Close();

                                        int InvoiceSeries = 0;
                                        InvType = DespatchCommonFunction.GenerateDomesticInvno(comboCompanyName.Text, textSuffix.Text, ref InvoiceSeries);//prefixdespatchcode + "/JOB/" + textyear.Text + "/" + InvNo.ToString();
                                        InvNo = InvoiceSeries;
                                    }

                                    //Database.myreader = Database.GetExecuteReaderCommand("Select isnull(max(InvNo),0) as InvNo from DomesticInvoice    where suffix = '"
                                    //    + textSuffix.Text + "' and companyname = '" + comboCompanyName.Text + "' ");
                                    //if (Database.myreader.Read())
                                    //    InvNo = Convert.ToInt32(Database.myreader["InvNo"].ToString());
                                    //Database.myreader.Close();
                                    //InvNo = InvNo + 1;

                                    string ConsigneeCode = "";
                                    Database.myreader = Database.GetExecuteReaderCommand("select ConsigneeCode from Consignee where consigneename = '"
                                         + comboConsigneeName.Text + "' and companycode ='" + CompanyCode
                                         + "' and city = '" + comboConsigneeCity.Text + "'");
                                    if (Database.myreader.Read())
                                        ConsigneeCode = Database.myreader[0].ToString();
                                    Database.myreader.Close();


                                    string st = "";
                                    if (checkAvailBenefit.Checked)
                                        st = "You have availed benefit 39-A";
                                    else
                                        st = "you have not availed benefit 39-A";

                                    DialogResult drs = MessageBox.Show("Do you want to generate " + comboTypeInvoice.Text.ToUpper()
                                         + Environment.NewLine + " Do you want to generate  Invoice for " + comboCompanyName.Text.ToUpper()
                                         + Environment.NewLine + st, "Generate Domestic Invoice"
                                                     , MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                                    if (drs.CompareTo(DialogResult.Yes) == 0)
                                    {
                                        blntans = false;
                                        Database.BeginTransaction();
                                        blntans = true;



                                        string BuyerDate = "";
                                        string ExciseApplicable = "";
                                        string InsuranceApp = "";
                                        string Insurance = "";
                                        string UtBondDetails = "";

                                        if (dateTimeBuyerOrderDate.Text == "" || textBuyerOrderNo.Text == "" || textBuyerOrderNo.Text == "N/A")
                                            BuyerDate = "";//dr[5] = System.DateTime.Now.Date.ToString("MM/dd/yyyy");
                                        else
                                            BuyerDate = dateTimeBuyerOrderDate.Value.ToString(DespatchCommonFunction.ERPDBDateSettings());

                                        if (comboExicseApp.SelectedIndex == 0)
                                            ExciseApplicable = "NIL";

                                        else
                                        {
                                            ExciseApplicable = Utility.IntegerToWords(Convert.ToInt64(Convert.ToDouble(Excise + Educess + HiEduCess)));
                                            textExciseNIlRemarks.Text = "";
                                        }

                                        if (comboVatApp.SelectedIndex == 0)
                                        {
                                        }
                                        else
                                        {
                                            textVatNilRmarks.Text = "";
                                        }

                                        if (checkInsurance.Checked)
                                        {
                                            InsuranceApp = "yes";
                                            Insurance = textInsurance.Text;
                                        }
                                        else
                                            InsuranceApp = "no";

                                        if (checkAvailBenefit.Checked)
                                            UtBondDetails = textUTBondDetails.Text;
                                        string isfreghtinclude = "";
                                        if (checkFreight.Checked)
                                            isfreghtinclude = "yes";
                                        else
                                            isfreghtinclude = "no";

                                        string ispurchasereturn = "";
                                        if (checkIsPurchaseReturn.Checked)
                                            ispurchasereturn = "yes";
                                        else
                                            ispurchasereturn = "no";

                                        if (BuyerDate == "")
                                        {
                                            if (Database.GetExecuteNonQueryCommand("insert into DomesticInvoice(SalesOrderNo,InvNo,InvDate,TermsOfPayment,BuyerOrderNo,BuyerDate,DespatchNo,DespatchDate,VehicalNo,Destination,TermsOfDelivery,BuyerName,ExDuty,Educess,SecHiEduCess,vat,Addvat,ExciseAmt,EduCessAmt,SecHiEduCessAmt,VatAmt,AddVatAmt,TotalQty,TotalAmt,TotalAmtWord,InvDateTime,RemDateTime,TypeOfInvoice,DelChallanNo,DelChallanDate,LRNo,LRDate,LoginName,ExciseAmtWord,TransportName,Consignee,Heading,Insurance,Typevat,Remarks,CheckInsurance,Excisetype,VatType,Frieght,TCSPer,TCSAmt,Suffix,Deptt,Header,CompanyName,UTBond,Currency,Currencyvalue,PLA,RG23,ConsigneeCode,SupplementaryInvoice,AgainstInvNo,IsCancel,SpDuty1,SpDuty1Amt,SpDuty2,SpDuty2Amt,SpDuty3,SpDuty3Amt,AgainstInvDate,AccountHead,notifier,freightremarks,IsMArketing,ISFREIGHTINCLUDE,ExciseNilRemarks,VatNIlRemarks,State,ispurchasereturn,MobileNo,IsGSTAllowDuty1,IsGSTAllowDuty2,IsGSTAllowDuty3,Invtype,isdispfrm,dispfrm,dispfrm_add) values('',"
                                                                + InvNo + ",'" + dateTimePickerInvDate.Value.ToString(DespatchCommonFunction.ERPDBDateSettings())
                                                                + "','" + textTermsofPayment.Text
                                                                + "','" + textBuyerOrderNo.Text
                                                                + "',null,'" + InvNo
                                                                + "','" + dateTimePickerDespatchDate.Value.ToString(DespatchCommonFunction.ERPDBDateSettings())
                                                                + "','" + comboVehicalNo.Text.ToUpper()
                                                                + "','" + comboDestination.Text.ToUpper()
                                                                + "','" + textTermsOfDelivery.Text
                                                                + "','" + comboBuyerName.Text
                                                                + "'," + textExDuty.Text
                                                                + "," + textEduCess.Text
                                                                + "," + textSecHiEdu.Text
                                                                + "," + textVat.Text
                                                                + "," + textAddVat.Text
                                                                + "," + Excise
                                                                + "," + Educess
                                                                + "," + HiEduCess
                                                                + "," + Vat
                                                                + "," + AddVat
                                                                + ",0," + textGrdTotal.Text
                                                                + ",'" + Utility.IntegerToWords(Convert.ToInt64(Convert.ToDouble(textGrdTotal.Text)))
                                                                + "','" + dateTimePickerInvoice.Value.ToString(DespatchCommonFunction.ERPDBDateSettings())
                                                                + "','" + dateTimePickerDateTimeRemoval.Value.ToString(DespatchCommonFunction.ERPDBDateSettings())
                                                                + "','" + comboTypeInvoice.Text
                                                                + "','" + textDelChallanNo.Text
                                                                + "','" + dateTimePickerChallanDate.Value.ToString(DespatchCommonFunction.ERPDBDateSettings())
                                                                + "','" + textBoxLRNo.Text
                                                                + "','" + dateTimePickerLRNo.Value.ToString(DespatchCommonFunction.ERPDBDateSettings())
                                                                + "','" + FrmMain.UserName
                                                                + "','" + ExciseApplicable
                                                                + "','" + comboTranporterName.Text
                                                                + "','" + textConsignee.Text
                                                                + "','" + textHeading.Text
                                                                + "','" + Insurance
                                                                + "','" + comboType.Text
                                                                + "','" + textRemarks.Text
                                                                + "','" + InsuranceApp
                                                                + "','" + comboExicseApp.Text
                                                                + "','" + comboVatApp.Text
                                                                + "'," + textFrieght.Text
                                                                + "," + textTCS.Text
                                                                + "," + TCS
                                                                + ",'" + textSuffix.Text
                                                                + "','" + comboDeptt.Text
                                                                + "','','" + comboCompanyName.Text
                                                                + "','" + UtBondDetails
                                                                + "','" + comboCurrency.Text
                                                                + "','" + comboCurrencyValue.Text
                                                                + "','" + textPLA.Text
                                                                + "','" + textRG23A.Text
                                                                + "','" + ConsigneeCode
                                                                + "',null,'','','" + textSpDutyRemarks1.Text
                                                                + "'," + textSpDuty1Amt.Text
                                                                + ",'" + textSpDutyRemark2.Text
                                                                + "'," + textSpDuty2Amt.Text
                                                                + ",'" + textSpDutyRemarks3.Text
                                                                + "'," + textSpDuty3Amt.Text
                                                                + ",'','" + comboAccountHead.Text + "','" + textNotifier.Text + "','"
                                                                + textFreightRemarks.Text + "','" + agstmarketing + "','"
                                                                + isfreghtinclude + "','" + textExciseNIlRemarks.Text + "','" + textVatNilRmarks.Text + "','"
                                                                + comboState.Text + "','"
                                                                + ispurchasereturn + "','" + textMobile.Text + "','"
                                                                + (chkSpDu1.Checked == true ? "yes" : "no") + "','" + (chkSpDu2.Checked == true ? "yes" : "no") + "','" + (chkSpDu3.Checked == true ? "yes" : "no") + "','',"
                                                                + (chk_dispfrm.Checked ? "1" : "0") + "," 
                                                                + "'" + combodispfrm.Text + "',"
                                                                + "'" + textdispfrm_add.Text + "')"))
                                                isSave = true;
                                        }
                                        else
                                        {
                                            if (Database.GetExecuteNonQueryCommand("insert into DomesticInvoice(SalesOrderNo,InvNo,InvDate,TermsOfPayment,BuyerOrderNo,BuyerDate,DespatchNo,DespatchDate,VehicalNo,Destination,TermsOfDelivery,BuyerName,ExDuty,Educess,SecHiEduCess,vat,Addvat,ExciseAmt,EduCessAmt,SecHiEduCessAmt,VatAmt,AddVatAmt,TotalQty,TotalAmt,TotalAmtWord,InvDateTime,RemDateTime,TypeOfInvoice,DelChallanNo,DelChallanDate,LRNo,LRDate,LoginName,ExciseAmtWord,TransportName,Consignee,Heading,Insurance,Typevat,Remarks,CheckInsurance,Excisetype,VatType,Frieght,TCSPer,TCSAmt,Suffix,Deptt,Header,CompanyName,UTBond,Currency,Currencyvalue,PLA,RG23,ConsigneeCode,SupplementaryInvoice,AgainstInvNo,IsCancel,SpDuty1,SpDuty1Amt,SpDuty2,SpDuty2Amt,SpDuty3,SpDuty3Amt,AgainstInvDate,AccountHead,notifier,freightremarks,IsMArketing,ISFREIGHTINCLUDE,ExciseNilRemarks,VatNIlRemarks,State,ispurchasereturn,MobileNo,IsGSTAllowDuty1,IsGSTAllowDuty2,IsGSTAllowDuty3,Invtype,isdispfrm,dispfrm,dispfrm_add) values('',"
                                                                + InvNo + ",'" + dateTimePickerInvDate.Value.ToString(DespatchCommonFunction.ERPDBDateSettings())
                                                                + "','" + textTermsofPayment.Text
                                                                + "','" + textBuyerOrderNo.Text
                                                                + "','" + BuyerDate
                                                                + "','" + InvNo
                                                                + "','" + dateTimePickerDespatchDate.Value.ToString(DespatchCommonFunction.ERPDBDateSettings())
                                                                + "','" + comboVehicalNo.Text.ToUpper()
                                                                + "','" + comboDestination.Text.ToUpper()
                                                                + "','" + textTermsOfDelivery.Text
                                                                + "','" + comboBuyerName.Text
                                                                + "'," + textExDuty.Text
                                                                + "," + textEduCess.Text
                                                                + "," + textSecHiEdu.Text
                                                                + "," + textVat.Text
                                                                + "," + textAddVat.Text
                                                                + "," + Excise
                                                                + "," + Educess
                                                                + "," + HiEduCess
                                                                + "," + Vat
                                                                + "," + AddVat
                                                                + ",0," + textGrdTotal.Text
                                                                + ",'" + Utility.IntegerToWords(Convert.ToInt64(Convert.ToDouble(textGrdTotal.Text)))
                                                                + "','" + dateTimePickerInvoice.Value.ToString(DespatchCommonFunction.ERPDBDateSettings())
                                                                + "','" + dateTimePickerDateTimeRemoval.Value.ToString(DespatchCommonFunction.ERPDBDateSettings())
                                                                + "','" + comboTypeInvoice.Text
                                                                + "','" + textDelChallanNo.Text
                                                                + "','" + dateTimePickerChallanDate.Value.ToString(DespatchCommonFunction.ERPDBDateSettings())
                                                                + "','" + textBoxLRNo.Text
                                                                + "','" + dateTimePickerLRNo.Value.ToString(DespatchCommonFunction.ERPDBDateSettings())
                                                                + "','" + FrmMain.UserName
                                                                + "','" + ExciseApplicable
                                                                + "','" + comboTranporterName.Text
                                                                + "','" + textConsignee.Text
                                                                + "','" + textHeading.Text
                                                                + "','" + Insurance
                                                                + "','" + comboType.Text
                                                                + "','" + textRemarks.Text
                                                                + "','" + InsuranceApp
                                                                + "','" + comboExicseApp.Text
                                                                + "','" + comboVatApp.Text
                                                                + "'," + textFrieght.Text
                                                                + "," + textTCS.Text
                                                                + "," + TCS
                                                                + ",'" + textSuffix.Text
                                                                + "','" + comboDeptt.Text
                                                                + "','','" + comboCompanyName.Text
                                                                + "','" + UtBondDetails
                                                                + "','" + comboCurrency.Text
                                                                + "','" + comboCurrencyValue.Text
                                                                + "','" + textPLA.Text
                                                                + "','" + textRG23A.Text
                                                                + "','" + ConsigneeCode
                                                                + "',null,'','','" + textSpDutyRemarks1.Text
                                                                + "'," + textSpDuty1Amt.Text
                                                                + ",'" + textSpDutyRemark2.Text
                                                                + "'," + textSpDuty2Amt.Text
                                                                + ",'" + textSpDutyRemarks3.Text
                                                                + "'," + textSpDuty3Amt.Text
                                                                + ",'','" + comboAccountHead.Text + "','" + textNotifier.Text + "','"
                                                                + textFreightRemarks.Text + "','" + agstmarketing + "','"
                                                                + isfreghtinclude + "','" + textExciseNIlRemarks.Text + "','"
                                                                + textVatNilRmarks.Text + "','" + comboState.Text + "','"
                                                                + ispurchasereturn + "','" + textMobile.Text + "','"
                                                                + (chkSpDu1.Checked == true ? "yes" : "no") + "','" + (chkSpDu2.Checked == true ? "yes" : "no")
                                                                + "','" + (chkSpDu3.Checked == true ? "yes" : "no") + "','',"
                                                                + (chk_dispfrm.Checked ? "1" : "0") + ","
                                                                + "'" + combodispfrm.Text + "',"
                                                                + "'" + textdispfrm_add.Text + "')"))
                                                isSave = true;

                                        }

                                        UpdateInsurancePolicy(Convert.ToDouble(textGrdTotal.Text), checkInsurance.Checked);

                                        for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                                        {
                                            if (dataGridView1.Rows[i].Cells[DCommodity.Index].FormattedValue.ToString() != "" &&
                                                dataGridView1.Rows[i].Cells[DtDeptt.Index].FormattedValue.ToString() != "" &&
                                                dataGridView1.Rows[i].Cells[DUOM.Index].FormattedValue.ToString() != "" &&
                                                dataGridView1.Rows[i].Cells[DOrderQty.Index].FormattedValue.ToString() != "" &&
                                                dataGridView1.Rows[i].Cells[DUnitPrice.Index].FormattedValue.ToString() != "")
                                            {

                                                string GroupName = "a";
                                                bool IsAvailInDatabase = false;
                                                string Commodity = "";
                                                int TotalCommodity = 0;
                                                Database.myreader = Database.GetExecuteReaderCommand("select isnull(count(Distinct(commodity)),0) from DomesticInvItem where InvNo ="
                                                                        + InvNo + " and yr = '" + textSuffix.Text + "' and companyname = '" + comboCompanyName.Text + "'");
                                                if (Database.myreader.Read())
                                                {
                                                    TotalCommodity = Convert.ToInt32(Database.myreader[0].ToString());
                                                }
                                                Database.myreader.Close();
                                                Database.myreader = Database.GetExecuteReaderCommand("select groupcommodity from DomesticInvItem where InvNo ="
                                                              + InvNo + " and commodity = '" + dataGridView1.Rows[i].Cells[DCommodity.Index].Value.ToString() + "' and yr = '"
                                                              + textSuffix.Text + "' and companyname = '" + comboCompanyName.Text + "'");
                                                if (Database.myreader.Read())
                                                {
                                                    GroupName = Database.myreader[0].ToString();
                                                    Commodity = "";
                                                    IsAvailInDatabase = true;
                                                }
                                                Database.myreader.Close();

                                                if (TotalCommodity == 1 && !IsAvailInDatabase)
                                                    GroupName = "b";
                                                if (TotalCommodity == 2 && !IsAvailInDatabase)
                                                    GroupName = "c";
                                                if (TotalCommodity == 3 && !IsAvailInDatabase)
                                                    GroupName = "d";
                                                if (TotalCommodity == 4 && !IsAvailInDatabase)
                                                    GroupName = "e";

                                                if (!IsAvailInDatabase)
                                                    Commodity = dataGridView1[1, i].ToString();

                                                if (dataGridView1.Rows[i].Cells[DNoofPkg.Index].FormattedValue.ToString() == "")
                                                    dataGridView1.Rows[i].Cells[DNoofPkg.Index].Value = ".";

                                                if (dataGridView1.Rows[i].Cells[DItemDesc.Index].FormattedValue.ToString() == "")
                                                    dataGridView1.Rows[i].Cells[DItemDesc.Index].Value = ".";

                                                if (dataGridView1.Rows[i].Cells[DOrderQty.Index].FormattedValue.ToString() == "")
                                                    dataGridView1.Rows[i].Cells[DOrderQty.Index].Value = ".";

                                                if (dataGridView1.Rows[i].Cells[DUnitPrice.Index].FormattedValue.ToString() == "")
                                                    dataGridView1.Rows[i].Cells[DUnitPrice.Index].Value = ".";

                                                if (dataGridView1.Rows[i].Cells[DUOM.Index].FormattedValue.ToString() == "")
                                                    dataGridView1.Rows[i].Cells[DUOM.Index].Value = ".";

                                                if (dataGridView1.Rows[i].Cells[DTotal.Index].FormattedValue.ToString() == "")
                                                    dataGridView1.Rows[i].Cells[DTotal.Index].Value = ".";

                                                if (dataGridView1.Rows[i].Cells[DNetWt.Index].FormattedValue.ToString() == "")
                                                    dataGridView1.Rows[i].Cells[DNetWt.Index].Value.ToString();
                                                if (dataGridView1.Rows[i].Cells[DGrossWt.Index].FormattedValue.ToString() == "")
                                                    dataGridView1.Rows[i].Cells[DGrossWt.Index].Value.ToString();

                                                if (dataGridView1.Rows[i].Cells[DNonDisplayUnit.Index].FormattedValue.ToString() == "")
                                                    dataGridView1.Rows[i].Cells[DNonDisplayUnit.Index].Value = ".";

                                                double NonDisplayQty = 0;
                                                if (dataGridView1.Rows[i].Cells[DUOM.Index].FormattedValue.ToString() ==
                                                        dataGridView1.Rows[i].Cells[DNonDisplayUnit.Index].FormattedValue.ToString())
                                                    NonDisplayQty = Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[DOrderQty.Index].Value.ToString());
                                                else if (dataGridView1.Rows[i].Cells[DNonDisplayQty.Index].FormattedValue.ToString() == "")
                                                    NonDisplayQty = 0;
                                                else
                                                    NonDisplayQty = Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[DNonDisplayQty.Index].Value.ToString());

                                                if (dataGridView1.Rows[i].Cells[DQtyRemarks.Index].FormattedValue.ToString() == "")
                                                    dataGridView1.Rows[i].Cells[DQtyRemarks.Index].Value = ".";

                                                if (dataGridView1.Rows[i].Cells[DCommodityRemarks.Index].FormattedValue.ToString() == "")
                                                    dataGridView1.Rows[i].Cells[DCommodityRemarks.Index].Value = ".";

                                                if (isagstMarketing)
                                                {
                                                    string STRQL = "insert into DomesticInvItem(InvNo,NoofPkg,ItemDesc,Qty,Rate,Unit,Amount,NetWt,GrossWt,SalesOrderNo,LoginName,Commodity,SrNo,GroupCommodity,NonDisplayUnit,NonDisplayQty,QtyRemarks,Yr,Companyname,CommodityRemarks,Desc1,Desc2,BagsMRP,size,weekno,LotNo,Colour,Meter,GPM,GSM,WBWeight,ReqWtperBag,ReqWtTotal,DispatchWtperBagGr,WtTolperBagGr,WtExtrapershortKg,WtDiffbetPLWB,AssesableValue,OrderNO,itemDeptt,HSNCode,CGSTrate,CGSTamt,SGSTRate,SGSTAmt,IGSTRate,IGSTAmt,Itemcode) values('"
                                                            + InvNo + "','" + dataGridView1.Rows[i].Cells[DNoofPkg.Index].Value
                                                            + "','" + dataGridView1.Rows[i].Cells[DItemDesc.Index].Value
                                                            + "','" + dataGridView1.Rows[i].Cells[DOrderQty.Index].Value
                                                            + "','" + dataGridView1.Rows[i].Cells[DUnitPrice.Index].Value
                                                            + "','" + dataGridView1.Rows[i].Cells[DUOM.Index].Value
                                                            + "','" + dataGridView1.Rows[i].Cells[DTotal.Index].Value
                                                            + "','" + dataGridView1.Rows[i].Cells[DNetWt.Index].Value
                                                            + "','" + dataGridView1.Rows[i].Cells[DGrossWt.Index].Value + "','"
                                                            + "','" + FrmMain.UserName
                                                            + "','" + dataGridView1.Rows[i].Cells[DCommodity.Index].Value.ToString()
                                                            + "','" + Convert.ToString((i + 1))
                                                            + "','" + GroupName
                                                            + "','" + dataGridView1.Rows[i].Cells[DNonDisplayUnit.Index].Value.ToString()
                                                            + "','" + NonDisplayQty.ToString()
                                                            + "','" + dataGridView1.Rows[i].Cells[DQtyRemarks.Index].Value.ToString()
                                                            + "','" + textSuffix.Text
                                                            + "','" + comboCompanyName.Text
                                                            + "','" + dataGridView1.Rows[i].Cells[DCommodityRemarks.Index].Value.ToString()
                                                            + "',null,null,null,null,null,null,null,null,null,null,null,"
                                                            + "null,null,null,null,null,null,null" + ",'" + dataGridView1.Rows[i].Cells[OrderNo.Index].Value.ToString()
                                                            + "','" + dataGridView1.Rows[i].Cells[DtDeptt.Index].Value.ToString()
                                                            + "','" + dataGridView1.Rows[i].Cells[colHSNCode.Index].Value.ToString()
                                                            + "'," + Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[colCGSTRate.Index].Value)
                                                            + "," + Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[ColCGSTAmt.Index].Value)
                                                            + "," + Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[ColSGSTRate.Index].Value)
                                                            + "," + Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[colSGSTAmt.Index].Value)
                                                            + "," + Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[ColIGSTRate.Index].Value)
                                                            + "," + Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[colIGSTAmt.Index].Value)
                                                            + ",'" + Convert.ToString(dataGridView1.Rows[i].Cells[colItemCode.Index].Value) + "')";

                                                    Database.GetExecuteNonQueryCommand(STRQL);
                                                }
                                                else
                                                {
                                                    Database.GetExecuteNonQueryCommand("insert into DomesticInvItem(InvNo,NoofPkg,ItemDesc,Qty,Rate,Unit,Amount,NetWt,GrossWt,SalesOrderNo,LoginName,Commodity,SrNo,GroupCommodity,NonDisplayUnit,NonDisplayQty,QtyRemarks,Yr,Companyname,CommodityRemarks,Desc1,Desc2,BagsMRP,size,weekno,LotNo,Colour,Meter,GPM,GSM,WBWeight,ReqWtperBag,ReqWtTotal,DispatchWtperBagGr,WtTolperBagGr,WtExtrapershortKg,WtDiffbetPLWB,AssesableValue,OrderNO,itemDeptt,HSNCode,CGSTrate,CGSTamt,SGSTRate,SGSTAmt,IGSTRate,IGSTAmt,itemcode) values('"
                                                            + InvNo + "','" + dataGridView1.Rows[i].Cells[DNoofPkg.Index].Value
                                                            + "','" + dataGridView1.Rows[i].Cells[DItemDesc.Index].Value
                                                            + "','" + dataGridView1.Rows[i].Cells[DOrderQty.Index].Value
                                                            + "','" + dataGridView1.Rows[i].Cells[DUnitPrice.Index].Value
                                                            + "','" + dataGridView1.Rows[i].Cells[DUOM.Index].Value
                                                            + "','" + dataGridView1.Rows[i].Cells[DTotal.Index].Value
                                                            + "','" + dataGridView1.Rows[i].Cells[DNetWt.Index].Value
                                                            + "','" + dataGridView1.Rows[i].Cells[DGrossWt.Index].Value + "','"
                                                            + "','" + FrmMain.UserName
                                                            + "','" + dataGridView1.Rows[i].Cells[DCommodity.Index].Value.ToString()
                                                            + "','" + Convert.ToString((i + 1))
                                                            + "','" + GroupName
                                                            + "','" + dataGridView1.Rows[i].Cells[DNonDisplayUnit.Index].Value.ToString()
                                                            + "','" + NonDisplayQty.ToString()
                                                            + "','" + dataGridView1.Rows[i].Cells[DQtyRemarks.Index].Value.ToString()
                                                            + "','" + textSuffix.Text
                                                            + "','" + comboCompanyName.Text
                                                            + "','" + dataGridView1.Rows[i].Cells[DCommodityRemarks.Index].Value.ToString()
                                                            + "',null,null,null,null,null,null,null,null,null,null,null,"
                                                            + "null,null,null,null,null,null,null"
                                                            + ",'" + dataGridView1.Rows[i].Cells[DtDeptt.Index].Value.ToString()
                                                            + "','" + dataGridView1.Rows[i].Cells[colHSNCode.Index].Value.ToString()
                                                            + "'," + dataGridView1.Rows[i].Cells[colCGSTRate.Index].Value.ToString()
                                                            + "," + dataGridView1.Rows[i].Cells[ColCGSTAmt.Index].Value.ToString()
                                                           + "," + Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[ColSGSTRate.Index].Value)
                                                            + "," + Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[colSGSTAmt.Index].Value)
                                                            + "," + Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[ColIGSTRate.Index].Value)
                                                            + "," + Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[colIGSTAmt.Index].Value)
                                                            + ",'" + Convert.ToString(dataGridView1.Rows[i].Cells[colItemCode.Index].Value) + "')");
                                                }


                                            }
                                        }
                                        /// Iniitalize 
                                        TotalCount = 0;
                                        textAddVat.Text = "";
                                        textBoxLRNo.Text = "";
                                        textBuyerOrderNo.Text = "";
                                        textDelChallanNo.Text = "";
                                        textDesPatchNo.Text = "";
                                        comboDestination.Text = "";
                                        textEduCess.Text = "";
                                        textExDuty.Text = "";
                                        textInvNo.Text = "";
                                        textTermsOfDelivery.Text = "";
                                        textSecHiEdu.Text = "";
                                        textTermsofPayment.Text = "";
                                        textVat.Text = "";
                                        comboVehicalNo.Text = "";
                                        textGrdTotal.Text = "";
                                        textMobile.Text = "";
                                        checkInsurance.Checked = false;
                                        checkFreight.Checked = false;
                                        btnUpdate.Enabled = false;
                                        btnSave.Enabled = true;
                                        dataGridView1.Rows.Clear();
                                        Database.CommitTransaction();
                                        blntans = false;
                                        if (isSave)
                                            MessageBox.Show("Domestic Invoice " + InvNo + " is generated succesfully");

                                        Utility.UserInformation("Save", "Domestic Invoice " + InvNo.ToString() + " is saved successfully");

                                        #region PackingList
                                        if (DespatchCommonFunction.AllowMappingPackinglist(comboCompanyName.Text) > 0)
                                        {
                                            if (Database.OpenConnection(Utility.DespacthConnectionString))
                                            {
                                                string strgstInv = Convert.ToString(Database.GetExecuteNonQueryCommand_retrun("SELECT GSTInvNo FROM Vw_DomesticInvoice WHERE InvNo=" +
                                                InvNo + " and companyname='" + comboCompanyName.Text + "' and ARESuffix='" + textSuffix.Text + "'"));

                                                Database.GetExecuteNonQueryCommand("INSERT INTO MappPackingList (CompanyName,InvNo,Yr,PackingListNo) VALUES('" +
                                                comboCompanyName.Text + "','" + strgstInv + "','" + textSuffix.Text + "','" + despackinglist + "')");
                                                // INVOICE-RULE: packing list mapped = sold, take kg off Despatch board.
                                                InvoiceRule_AdjustDespatchGodown(comboCompanyName.Text, despackinglist, true);
                                            }
                                        }
                                        #endregion

                                        FrmDispatchReportPIL frm = new FrmDispatchReportPIL();
                                        frm.Show();
                                        fillData();
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if (blntans)
                            Database.RollBackTransaction();

                        ErrorMessageBox.Show(ex);
                    }
                }
            }
        }

        private double Calculation()
        {
            double total = 0;
            TotalCount = 0;
            double dblCGSTamt = 0;
            double dblSGSTAmt = 0;
            double dblIGSTAmt = 0;
            try
            {
                if (Database.OpenConnection(Utility.DespacthConnectionString))
                {
                    double itemfreight = 0;
                    double totalfreight = 0;
                    bool FreightAtDespatchTime = false;
                    for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                    {
                        itemfreight = 0;
                        Database.myreader = Database.GetExecuteReaderCommand("select FreightAtDespatchTime from vw_MarketingInvoice   where itemNO = '"
                            + dataGridView1.Rows[i].Cells[OrderNo.Index].Value.ToString() + "' ");
                        if (Database.myreader.Read())
                        {
                            if (Database.myreader[0].ToString() == "yes")
                                FreightAtDespatchTime = true;
                        }
                        Database.myreader.Close();
                        if (!FreightAtDespatchTime)
                        {
                            Database.myreader = Database.GetExecuteReaderCommand("select FreightperUnit from Marketinginvitem with (NOLOCK) where ItemNo = '"
                                + dataGridView1.Rows[i].Cells[OrderNo.Index].Value.ToString() + "'  ");
                            if (Database.myreader.Read())
                            {
                                itemfreight = Convert.ToDouble(Database.myreader[0].ToString());

                            }
                            Database.myreader.Close();
                        }
                        Database.myreader = Database.GetExecuteReaderCommand("select Frieght,FreightAtDespatchTime from Multilayerworkorder with (NOLOCK) where workordernumber = '"
                            + dataGridView1.Rows[i].Cells[OrderNo.Index].Value.ToString() + "'");
                        if (Database.myreader.Read())
                        {
                            if (Database.myreader[1].ToString() == "yes")
                                FreightAtDespatchTime = true;
                            if (!FreightAtDespatchTime)
                                itemfreight = Convert.ToDouble(Database.myreader[0].ToString());

                        }
                        Database.myreader.Close();

                        Database.myreader = Database.GetExecuteReaderCommand("select Frieght,FreightAtDespatchTime from BOPPworkorder with (NOLOCK) where workordernumber = '"
                            + dataGridView1.Rows[i].Cells[OrderNo.Index].Value.ToString() + "'");
                        if (Database.myreader.Read())
                        {
                            if (Database.myreader[1].ToString() == "yes")
                                FreightAtDespatchTime = true;
                            if (!FreightAtDespatchTime)
                                itemfreight = Convert.ToDouble(Database.myreader[0].ToString());

                        }
                        Database.myreader.Close();
                        if (!FreightAtDespatchTime)
                            totalfreight += itemfreight * Convert.ToDouble(dataGridView1.Rows[i].Cells[DOrderQty.Index].Value.ToString());
                    }
                    if (!FreightAtDespatchTime)
                        textFrieght.Text = totalfreight.ToString();
                }
                if (textExDuty.Text == "")
                {
                    textExDuty.Text = "0";
                    textEduCess.Text = "0";
                    textSecHiEdu.Text = "0";
                    if (textVat.Text == "")
                        textVat.Text = "0";
                    if (textAddVat.Text == "")
                        textAddVat.Text = "0";
                }
                if (textSpDuty1Amt.Text == "")
                    textSpDuty1Amt.Text = "0";
                if (textSpDuty2Amt.Text == "")
                    textSpDuty2Amt.Text = "0";
                if (textSpDuty3Amt.Text == "")
                    textSpDuty3Amt.Text = "0";


                //&& dataGrid1[i, 2].ToString() != ""
                for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                {
                    if (dataGridView1.Rows[i].Cells[DCommodity.Index].FormattedValue.ToString() != "" &&
                     dataGridView1.Rows[i].Cells[DUOM.Index].FormattedValue.ToString() != "" &&
                     dataGridView1.Rows[i].Cells[DtDeptt.Index].FormattedValue.ToString() != "" &&
                       dataGridView1.Rows[i].Cells[DOrderQty.Index].FormattedValue.ToString() != "" &&
                    dataGridView1.Rows[i].Cells[DUnitPrice.Index].FormattedValue.ToString() != "")
                    {

                        ///ADD NEW LINE FOR ERPQtyDigit & ERPQtyDigit
                        ///Change by nikhil

                        dataGridView1.Rows[i].Cells[DOrderQty.Index].Value =
                        Math.Round(Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[DOrderQty.Index].Value), FrmMain.ERPQtyDigit);


                        dataGridView1.Rows[i].Cells[DUnitPrice.Index].Value =
                        Math.Round(Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[DUnitPrice.Index].Value), FrmMain.ERPRateDigit);




                        //dataGridView1.Rows[i].Cells[DTotal.Index].Value =
                        //Math.Round(Convert.ToDouble(dataGridView1.Rows[i].Cells[DOrderQty.Index].Value.ToString()) *
                        // Convert.ToDouble(dataGridView1.Rows[i].Cells[DUnitPrice.Index].Value.ToString()), 2);

                        ///ADD ERPAmoutnDigit
                        ///Change by nikhil
                        dataGridView1.Rows[i].Cells[DTotal.Index].Value =
                        Math.Round(Convert.ToDouble(dataGridView1.Rows[i].Cells[DOrderQty.Index].Value.ToString()) *
                        Convert.ToDouble(dataGridView1.Rows[i].Cells[DUnitPrice.Index].Value.ToString()), FrmMain.ERPAmoutnDigit);

                        total += Convert.ToDouble(dataGridView1.Rows[i].Cells[DTotal.Index].Value.ToString());
                        //total = Math.Round(total, 0, MidpointRounding.AwayFromZero);
                        ///ADD ERPAmoutnDigit
                        ///Change by nikhil
                        total = Math.Round(total, FrmMain.ERPAmoutnDigit, MidpointRounding.AwayFromZero);
                        TotalCount += 1;
                    }
                    //colCGSTrate colCGSTamt  colSGSTRate   ColSGSTAmt

                    {

                        {
                            //dataGridView1.Rows[i].Cells[ColCGSTAmt.Index].Value =
                            //    Math.Round(Convert.ToDouble(dataGridView1.Rows[i].Cells[DTotal.Index].Value.ToString()) *
                            //    Convert.ToDouble(dataGridView1.Rows[i].Cells[colCGSTRate.Index].Value.ToString()) / 100, 2);

                            ///ADD ERPAmoutnDigit
                            ///Change by nikhil
                            dataGridView1.Rows[i].Cells[ColCGSTAmt.Index].Value =
                         Math.Round(Convert.ToDouble(dataGridView1.Rows[i].Cells[DTotal.Index].Value.ToString()) *
                         Convert.ToDouble(dataGridView1.Rows[i].Cells[colCGSTRate.Index].Value.ToString()) / 100, FrmMain.ERPAmoutnDigit);

                            dblCGSTamt += Convert.ToDouble(dataGridView1.Rows[i].Cells[ColCGSTAmt.Index].Value);
                        }
                    }

                    {

                        {
                            //dataGridView1.Rows[i].Cells[colSGSTAmt.Index].Value =
                            //    Math.Round(Convert.ToDouble(dataGridView1.Rows[i].Cells[DTotal.Index].Value.ToString()) *
                            //    Convert.ToDouble(dataGridView1.Rows[i].Cells[ColSGSTRate.Index].Value.ToString()) / 100, 2);

                            ///ADD ERPAmoutnDigit
                            ///Change by nikhil
                            dataGridView1.Rows[i].Cells[colSGSTAmt.Index].Value =
                           Math.Round(Convert.ToDouble(dataGridView1.Rows[i].Cells[DTotal.Index].Value.ToString()) *
                           Convert.ToDouble(dataGridView1.Rows[i].Cells[ColSGSTRate.Index].Value.ToString()) / 100, FrmMain.ERPAmoutnDigit);

                            dblSGSTAmt += Convert.ToDouble(dataGridView1.Rows[i].Cells[ColCGSTAmt.Index].Value);
                        }
                    }

                    {

                        {
                            //dataGridView1.Rows[i].Cells[colIGSTAmt.Index].Value =
                            //    Math.Round(Convert.ToDouble(dataGridView1.Rows[i].Cells[DTotal.Index].Value.ToString()) *
                            //    Convert.ToDouble(dataGridView1.Rows[i].Cells[ColIGSTRate.Index].Value.ToString()) / 100, 2);


                            ///ADD ERPAmoutnDigit
                            ///Change by nikhil
                            dataGridView1.Rows[i].Cells[colIGSTAmt.Index].Value =
                            Math.Round(Convert.ToDouble(dataGridView1.Rows[i].Cells[DTotal.Index].Value.ToString()) *
                            Convert.ToDouble(dataGridView1.Rows[i].Cells[ColIGSTRate.Index].Value.ToString()) / 100, FrmMain.ERPAmoutnDigit);

                            dblIGSTAmt += Convert.ToDouble(dataGridView1.Rows[i].Cells[colIGSTAmt.Index].Value);
                        }
                    }
                }

                //total = Math.Round(total + dblSGSTAmt + dblCGSTamt + dblIGSTAmt, 0);
                ///ADD ERPAmoutnDigit
                ///Change by nikhil
                total = Math.Round(total + dblSGSTAmt + dblCGSTamt + dblIGSTAmt, FrmMain.ERPAmoutnDigit);

                Excise = (total * Convert.ToDouble(textExDuty.Text)) / 100;
                Excise = Math.Round(Excise, 0);

                Educess = ((Convert.ToDouble(textEduCess.Text) * Excise) / 100);
                HiEduCess = ((Convert.ToDouble(textSecHiEdu.Text) * Excise) / 100);
                Educess = Math.Round(Educess, 0);
                HiEduCess = Math.Round(HiEduCess, 0);
                if (checkFreight.Checked)
                {
                    Friegth = Convert.ToDouble(textFrieght.Text);
                    total = total + Friegth;
                }
                if (comboExicseApp.SelectedIndex == 0) // Excise nil
                {
                    Vat = (total * Convert.ToDouble(textVat.Text)) / 100;
                    AddVat = (total * Convert.ToDouble(textAddVat.Text)) / 100;
                }
                else if (comboExicseApp.SelectedIndex == 1)
                {
                    Vat = ((total + Excise + Educess + HiEduCess) * Convert.ToDouble(textVat.Text)) / 100;
                    AddVat = ((total + Excise + Educess + HiEduCess) * Convert.ToDouble(textAddVat.Text)) / 100;
                }
                Vat = Math.Round(Vat, 0);
                AddVat = Math.Round(AddVat, 0);


                if (comboVatApp.SelectedIndex == 0 && comboExicseApp.SelectedIndex == 1) //Vat Nil
                    total = total + Educess + HiEduCess + Excise;
                else if (comboExicseApp.SelectedIndex == 0 && comboVatApp.SelectedIndex == 1) //Excise Nil
                    total = total + Vat + AddVat;
                else if (comboExicseApp.SelectedIndex == 1 && comboVatApp.SelectedIndex == 1)
                    total = total + Educess + HiEduCess + Vat + AddVat + Excise;

                if (!checkFreight.Checked)
                {
                    Friegth = Convert.ToDouble(textFrieght.Text);
                    total = total + Friegth;
                }
                TCS = Convert.ToDouble(textTCS.Text);
                TCS = (total * TCS) / 100;
                //TCS = Math.Round(TCS, 0);
                ///ADD ERPAmoutnDigit
                ///Change by nikhil

                TCS = Math.Round(TCS, FrmMain.ERPAmoutnDigit);

                double spduty1 = Convert.ToDouble(textSpDuty1Amt.Text);
                double spduty2 = Convert.ToDouble(textSpDuty2Amt.Text);
                double spduty3 = Convert.ToDouble(textSpDuty3Amt.Text);

                total = total + TCS + spduty1 + spduty2 + spduty3;

                //total = Math.Round(total, 0);
                ///ADD ERPAmoutnDigit
                ///Change by nikhil
                total = Math.Round(total, FrmMain.ERPAmoutnDigit);
                textGrdTotal.Text = total.ToString();
            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
            }
            return total;
        }


        private double CalculationGST()
        {
            CheckGST();

            double total = 0;
            TotalCount = 0;
            double dblCGSTamt = 0;
            double dblSGSTAmt = 0;
            double dblIGSTAmt = 0;
            try
            {
                if (Database.OpenConnection(Utility.DespacthConnectionString))
                {
                    double itemfreight = 0;
                    double totalfreight = 0;
                    bool FreightAtDespatchTime = false;
                    for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                    {
                        itemfreight = 0;
                        Database.myreader = Database.GetExecuteReaderCommand("select FreightAtDespatchTime from vw_MarketingInvoice   where itemNO = '"
                            + dataGridView1.Rows[i].Cells[OrderNo.Index].Value.ToString() + "' ");
                        if (Database.myreader.Read())
                        {
                            if (Database.myreader[0].ToString() == "yes")
                                FreightAtDespatchTime = true;
                        }
                        Database.myreader.Close();
                        if (!FreightAtDespatchTime)
                        {
                            Database.myreader = Database.GetExecuteReaderCommand("select FreightperUnit from Marketinginvitem with (NOLOCK) where ItemNo = '"
                                + dataGridView1.Rows[i].Cells[OrderNo.Index].Value.ToString() + "'  ");
                            if (Database.myreader.Read())
                            {
                                itemfreight = Convert.ToDouble(Database.myreader[0].ToString());

                            }
                            Database.myreader.Close();
                        }
                        Database.myreader = Database.GetExecuteReaderCommand("select Frieght,FreightAtDespatchTime from Multilayerworkorder with (NOLOCK) where workordernumber = '"
                            + dataGridView1.Rows[i].Cells[OrderNo.Index].Value.ToString() + "'");
                        if (Database.myreader.Read())
                        {
                            if (Database.myreader[1].ToString() == "yes")
                                FreightAtDespatchTime = true;
                            if (!FreightAtDespatchTime)
                                itemfreight = Convert.ToDouble(Database.myreader[0].ToString());

                        }
                        Database.myreader.Close();

                        Database.myreader = Database.GetExecuteReaderCommand("select Frieght,FreightAtDespatchTime from BOPPworkorder with (NOLOCK) where workordernumber = '"
                            + dataGridView1.Rows[i].Cells[OrderNo.Index].Value.ToString() + "'");
                        if (Database.myreader.Read())
                        {
                            if (Database.myreader[1].ToString() == "yes")
                                FreightAtDespatchTime = true;
                            if (!FreightAtDespatchTime)
                                itemfreight = Convert.ToDouble(Database.myreader[0].ToString());

                        }
                        Database.myreader.Close();
                        if (!FreightAtDespatchTime)
                            totalfreight += itemfreight * Convert.ToDouble(dataGridView1.Rows[i].Cells[DOrderQty.Index].Value.ToString());



                    }
                    if (!FreightAtDespatchTime)
                        textFrieght.Text = totalfreight.ToString();
                }

                if (textSpDuty1Amt.Text == "")
                    textSpDuty1Amt.Text = "0";
                if (textSpDuty2Amt.Text == "")
                    textSpDuty2Amt.Text = "0";
                if (textSpDuty3Amt.Text == "")
                    textSpDuty3Amt.Text = "0";

                if (checkFreight.Checked)
                {
                    Friegth = Convert.ToDouble(textFrieght.Text);
                    total = total + Friegth;
                }
                //&& dataGrid1[i, 2].ToString() != ""
                for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                {
                    if (dataGridView1.Rows[i].Cells[DCommodity.Index].FormattedValue.ToString() != "" &&
                     dataGridView1.Rows[i].Cells[DUOM.Index].FormattedValue.ToString() != "" &&
                     dataGridView1.Rows[i].Cells[DtDeptt.Index].FormattedValue.ToString() != "" &&
                       dataGridView1.Rows[i].Cells[DOrderQty.Index].FormattedValue.ToString() != "" &&
                    dataGridView1.Rows[i].Cells[DUnitPrice.Index].FormattedValue.ToString() != "")
                    {
                        ///ADD NEW LINE FOR ERPQtyDigit & ERPQtyDigit
                        ///Change by nikhil

                        dataGridView1.Rows[i].Cells[DOrderQty.Index].Value =
                        Math.Round(Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[DOrderQty.Index].Value), FrmMain.ERPQtyDigit);


                        dataGridView1.Rows[i].Cells[DUnitPrice.Index].Value =
                        Math.Round(Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[DUnitPrice.Index].Value), FrmMain.ERPRateDigit);


                        //dataGridView1.Rows[i].Cells[DTotal.Index].Value =
                        //Math.Round(Convert.ToDouble(dataGridView1.Rows[i].Cells[DOrderQty.Index].Value.ToString()) *
                        // Convert.ToDouble(dataGridView1.Rows[i].Cells[DUnitPrice.Index].Value.ToString()), 2);
                        ///ADD ERPAmoutnDigit
                        ///Change by nikhil


                        dataGridView1.Rows[i].Cells[DTotal.Index].Value =
                        Math.Round(Convert.ToDouble(dataGridView1.Rows[i].Cells[DOrderQty.Index].Value.ToString()) *
                        Convert.ToDouble(dataGridView1.Rows[i].Cells[DUnitPrice.Index].Value.ToString()), FrmMain.ERPAmoutnDigit);



                        total += Convert.ToDouble(dataGridView1.Rows[i].Cells[DTotal.Index].Value.ToString());
                        //total = Math.Round(total, 0, MidpointRounding.AwayFromZero);
                        TotalCount += 1;
                    }
                    //colCGSTrate colCGSTamt  colSGSTRate   ColSGSTAmt


                    //dataGridView1.Rows[i].Cells[ColCGSTAmt.Index].Value =
                    //    Math.Round(Convert.ToDouble(dataGridView1.Rows[i].Cells[DTotal.Index].Value.ToString()) *
                    //    Convert.ToDouble(dataGridView1.Rows[i].Cells[colCGSTRate.Index].Value.ToString()) / 100, 2);
                    ///ADD ERPAmoutnDigit
                    ///Change by nikhil

                    dataGridView1.Rows[i].Cells[ColCGSTAmt.Index].Value =
                    Math.Round(Convert.ToDouble(dataGridView1.Rows[i].Cells[DTotal.Index].Value.ToString()) *
                    Convert.ToDouble(dataGridView1.Rows[i].Cells[colCGSTRate.Index].Value.ToString()) / 100, FrmMain.ERPAmoutnDigit);

                    dblCGSTamt += Convert.ToDouble(dataGridView1.Rows[i].Cells[ColCGSTAmt.Index].Value);


                    //dataGridView1.Rows[i].Cells[colSGSTAmt.Index].Value =
                    //    Math.Round(Convert.ToDouble(dataGridView1.Rows[i].Cells[DTotal.Index].Value.ToString()) *
                    //    Convert.ToDouble(dataGridView1.Rows[i].Cells[ColSGSTRate.Index].Value.ToString()) / 100, 2);


                    ///ADD ERPAmoutnDigit
                    ///Change by nikhil
                    dataGridView1.Rows[i].Cells[colSGSTAmt.Index].Value =
                    Math.Round(Convert.ToDouble(dataGridView1.Rows[i].Cells[DTotal.Index].Value.ToString()) *
                    Convert.ToDouble(dataGridView1.Rows[i].Cells[ColSGSTRate.Index].Value.ToString()) / 100, FrmMain.ERPAmoutnDigit);

                    dblSGSTAmt += Convert.ToDouble(dataGridView1.Rows[i].Cells[colSGSTAmt.Index].Value);



                    //dataGridView1.Rows[i].Cells[colIGSTAmt.Index].Value =
                    //Math.Round(Convert.ToDouble(dataGridView1.Rows[i].Cells[DTotal.Index].Value.ToString()) *
                    //Convert.ToDouble(dataGridView1.Rows[i].Cells[ColIGSTRate.Index].Value.ToString()) / 100, 2);

                    ///ADD ERPAmoutnDigit
                    ///Change by nikhil
                    dataGridView1.Rows[i].Cells[colIGSTAmt.Index].Value =
                    Math.Round(Convert.ToDouble(dataGridView1.Rows[i].Cells[DTotal.Index].Value.ToString()) *
                    Convert.ToDouble(dataGridView1.Rows[i].Cells[ColIGSTRate.Index].Value.ToString()) / 100, FrmMain.ERPAmoutnDigit);

                    dblIGSTAmt += Convert.ToDouble(dataGridView1.Rows[i].Cells[colIGSTAmt.Index].Value);


                }
                double findmaxrate = 0;
                foreach (DataGridViewRow oItem in dataGridView1.Rows)
                {
                    if (Utility.SafeConvertToDouble(oItem.Cells[ColSGSTRate.Index].Value) > findmaxrate)
                    {
                        findmaxrate = Convert.ToDouble(oItem.Cells[ColSGSTRate.Index].Value.ToString());
                    }
                    if (Utility.SafeConvertToDouble(oItem.Cells[colCGSTRate.Index].Value) > findmaxrate)
                    {
                        findmaxrate = Convert.ToDouble(oItem.Cells[colCGSTRate.Index].Value.ToString());
                    }
                    if (Utility.SafeConvertToDouble(oItem.Cells[ColIGSTRate.Index].Value) > findmaxrate)
                    {
                        findmaxrate = Convert.ToDouble(oItem.Cells[ColIGSTRate.Index].Value.ToString());
                    }
                }
                //if (checkFreight.Checked && dblIGSTAmt > 0)
                //{
                //    dblIGSTAmt += Math.Round((Friegth) * Convert.ToDouble(findmaxrate) / 100, 2);
                //}
                //if (checkFreight.Checked && dblSGSTAmt > 0)
                //{
                //    dblSGSTAmt += Math.Round((Friegth) * Convert.ToDouble(findmaxrate) / 100, 2);
                //}
                //if (checkFreight.Checked && dblSGSTAmt > 0)
                //{
                //    dblCGSTamt += Math.Round((Friegth) * Convert.ToDouble(findmaxrate) / 100, 2);
                //}

                ///ADD ERPAmoutnDigit
                ///Change by nikhil
                if (checkFreight.Checked && dblIGSTAmt > 0)
                {
                    dblIGSTAmt += Math.Round((Friegth) * Convert.ToDouble(findmaxrate) / 100, FrmMain.ERPAmoutnDigit);
                }
                if (checkFreight.Checked && dblSGSTAmt > 0)
                {
                    dblSGSTAmt += Math.Round((Friegth) * Convert.ToDouble(findmaxrate) / 100, FrmMain.ERPAmoutnDigit);
                }
                if (checkFreight.Checked && dblSGSTAmt > 0)
                {
                    dblCGSTamt += Math.Round((Friegth) * Convert.ToDouble(findmaxrate) / 100, FrmMain.ERPAmoutnDigit);
                }



                //if (chkSpDu1.Checked)
                //{
                //    if (dblCGSTamt > 0)
                //        dblCGSTamt += Math.Round((Utility.SafeConvertToDouble(textSpDuty1Amt.Text)) * Convert.ToDouble(findmaxrate) / 100, 2);
                //    if (dblSGSTAmt > 0)
                //        dblSGSTAmt += Math.Round((Utility.SafeConvertToDouble(textSpDuty1Amt.Text)) * Convert.ToDouble(findmaxrate) / 100, 2);
                //    if (dblIGSTAmt > 0)
                //        dblIGSTAmt += Math.Round((Utility.SafeConvertToDouble(textSpDuty1Amt.Text)) * Convert.ToDouble(findmaxrate) / 100, 2);
                //}

                ///ADD ERPAmoutnDigit
                ///Change by nikhil
                if (chkSpDu1.Checked)
                {
                    if (dblCGSTamt > 0)
                        dblCGSTamt += Math.Round((Utility.SafeConvertToDouble(textSpDuty1Amt.Text)) * Convert.ToDouble(findmaxrate) / 100, FrmMain.ERPAmoutnDigit);
                    if (dblSGSTAmt > 0)
                        dblSGSTAmt += Math.Round((Utility.SafeConvertToDouble(textSpDuty1Amt.Text)) * Convert.ToDouble(findmaxrate) / 100, FrmMain.ERPAmoutnDigit);
                    if (dblIGSTAmt > 0)
                        dblIGSTAmt += Math.Round((Utility.SafeConvertToDouble(textSpDuty1Amt.Text)) * Convert.ToDouble(findmaxrate) / 100, FrmMain.ERPAmoutnDigit);
                }


                //if (chkSpDu2.Checked)
                //{
                //    if (dblCGSTamt > 0)
                //        dblCGSTamt += Math.Round((Utility.SafeConvertToDouble(textSpDuty2Amt.Text)) * Convert.ToDouble(findmaxrate) / 100, 2);
                //    if (dblSGSTAmt > 0)
                //        dblSGSTAmt += Math.Round((Utility.SafeConvertToDouble(textSpDuty2Amt.Text)) * Convert.ToDouble(findmaxrate) / 100, 2);
                //    if (dblIGSTAmt > 0)
                //        dblIGSTAmt += Math.Round((Utility.SafeConvertToDouble(textSpDuty2Amt.Text)) * Convert.ToDouble(findmaxrate) / 100, 2);
                //}


                ///ADD ERPAmoutnDigit
                ///Change by nikhil
                if (chkSpDu2.Checked)
                {
                    if (dblCGSTamt > 0)
                        dblCGSTamt += Math.Round((Utility.SafeConvertToDouble(textSpDuty2Amt.Text)) * Convert.ToDouble(findmaxrate) / 100, FrmMain.ERPAmoutnDigit);
                    if (dblSGSTAmt > 0)
                        dblSGSTAmt += Math.Round((Utility.SafeConvertToDouble(textSpDuty2Amt.Text)) * Convert.ToDouble(findmaxrate) / 100, FrmMain.ERPAmoutnDigit);
                    if (dblIGSTAmt > 0)
                        dblIGSTAmt += Math.Round((Utility.SafeConvertToDouble(textSpDuty2Amt.Text)) * Convert.ToDouble(findmaxrate) / 100, FrmMain.ERPAmoutnDigit);
                }

                //if (chkSpDu3.Checked)
                //{
                //    if (dblCGSTamt > 0)
                //        dblCGSTamt += Math.Round((Utility.SafeConvertToDouble(textSpDuty3Amt.Text)) * Convert.ToDouble(findmaxrate) / 100, 2);
                //    if (dblSGSTAmt > 0)
                //        dblSGSTAmt += Math.Round((Utility.SafeConvertToDouble(textSpDuty3Amt.Text)) * Convert.ToDouble(findmaxrate) / 100, 2);
                //    if (dblIGSTAmt > 0)
                //        dblIGSTAmt += Math.Round((Utility.SafeConvertToDouble(textSpDuty3Amt.Text)) * Convert.ToDouble(findmaxrate) / 100, 2);
                //}



                ///ADD ERPAmoutnDigit
                ///Change by nikhil
                if (chkSpDu3.Checked)
                {
                    if (dblCGSTamt > 0)
                        dblCGSTamt += Math.Round((Utility.SafeConvertToDouble(textSpDuty3Amt.Text)) * Convert.ToDouble(findmaxrate) / 100, FrmMain.ERPAmoutnDigit);
                    if (dblSGSTAmt > 0)
                        dblSGSTAmt += Math.Round((Utility.SafeConvertToDouble(textSpDuty3Amt.Text)) * Convert.ToDouble(findmaxrate) / 100, FrmMain.ERPAmoutnDigit);
                    if (dblIGSTAmt > 0)
                        dblIGSTAmt += Math.Round((Utility.SafeConvertToDouble(textSpDuty3Amt.Text)) * Convert.ToDouble(findmaxrate) / 100, FrmMain.ERPAmoutnDigit);
                }


                //total = Math.Round(total + dblSGSTAmt + dblCGSTamt + dblIGSTAmt, 2);


                ///ADD ERPAmoutnDigit
                ///Change by nikhil
                total = Math.Round(total + dblSGSTAmt + dblCGSTamt + dblIGSTAmt, FrmMain.ERPAmoutnDigit);





                if (comboVatApp.SelectedIndex == 0 && comboExicseApp.SelectedIndex == 1) //Vat Nil
                    total = total + Educess + HiEduCess + Excise;
                else if (comboExicseApp.SelectedIndex == 0 && comboVatApp.SelectedIndex == 1) //Excise Nil
                    total = total + Vat + AddVat;
                else if (comboExicseApp.SelectedIndex == 1 && comboVatApp.SelectedIndex == 1)
                    total = total + Educess + HiEduCess + Vat + AddVat + Excise;

                if (!checkFreight.Checked)
                {
                    Friegth = Convert.ToDouble(textFrieght.Text);
                    total = total + Friegth;
                }


                double spduty1 = Convert.ToDouble(textSpDuty1Amt.Text);
                double spduty2 = Convert.ToDouble(textSpDuty2Amt.Text);
                double spduty3 = Convert.ToDouble(textSpDuty3Amt.Text);
                total = total + spduty1 + spduty2 + spduty3;

                TCS = Convert.ToDouble(textTCS.Text);
                TCS = (total * TCS) / 100;
                //TCS = Math.Round(TCS, 0);

                ///ADD ERPAmoutnDigit
                ///Change by nikhil
                TCS = Math.Round(TCS, FrmMain.ERPAmoutnDigit);
                total = total + TCS;

                //total = Math.Round(total, 2);

                ///ADD ERPAmoutnDigit
                ///Change by nikhil
                total = Math.Round(total, FrmMain.ERPAmoutnDigit);
                textGrdTotal.Text = total.ToString();
            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
            }
            return total;
        }

        private bool WeightDifferences()
        {
            bool IsDiffApp = false;
            try
            {
                for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                {
                    if (dataGridView1[DNetWt.Index, i].ToString() != "" && dataGridView1[DGrossWt.Index, i].ToString() != "")
                    {
                        if (Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[DGrossWt.Index].Value.ToString()) <
                            Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[DNetWt.Index].Value.ToString()))
                        {
                            IsDiffApp = true;
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
            }
            return IsDiffApp;
        }

        private void btnCalculation_Click(object sender, EventArgs e)
        {

            if (dateTimePickerInvDate.Value > Convert.ToDateTime("2017-06-30 00:00:00"))
            {

                CalculationGST();
            }
            else
            {
                Calculation();
            }
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {


            //frmPrintDomesticPrice frm = new frmPrintDomesticPrice();
            //frm.ShowDialog();
            DateTime gstdate = Convert.ToDateTime("2017-07-01 00:00:00");
            if (dateTimePickerInvDate.Value >= gstdate)
                TabClass.OpenForm(new frmPrintDomesticPrice_GST());
            else
                TabClass.OpenForm(new frmPrintDomesticPrice());

        }

        private void textInvNo_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            try
            {
                if (Database.OpenConnection(Utility.DespacthConnectionString))
                {
                    isupdate = true;
                    string CompanyName = "";
                    string Currency = "";
                    bool IsCheckIsurance = false;
                    string state = "";
                    string consigneecode = "";


                    Database.myreader = Database.GetExecuteReaderCommand("select IsMarketing from domesticinvoice with (NOLOCK) where invno = "
                    + textInvNo.Text + " and companyName = '" + comboCompanyName.Text + "' and suffix = '" + textSuffix.Text + "' and ismarketing = 'yes'");
                    if (Database.myreader.Read())
                        isagstMarketing = true;
                    Database.myreader.Close();
                    //if (!ismarketing)
                    //    MessageBox.Show("Please Update InvNo in Domestic  Invoice ");
                    //else
                    //{
                    fillData(true);
                    Database.OpenConnection(Utility.DespacthConnectionString);
                    Database.myreader = Database.GetExecuteReaderCommand("select * from DomesticInvoice with (NOLOCK) where InvNo = " +
                                                                        textInvNo.Text + " and suffix ='" + textSuffix.Text + "' and companyname = '"
                                                                        + comboCompanyName.Text + "'");
                    if (Database.myreader.Read())
                    {
                        isupdatefororder = true;
                        btnSave.Enabled = false;
                        btnUpdate.Enabled = true;

                        textBoxLRNo.Text = Database.myreader["LRno"].ToString();
                        textDelChallanNo.Text = Database.myreader["DelChallanNo"].ToString();
                        dateTimePickerLRNo.Value = Convert.ToDateTime(Database.myreader["LRDate"].ToString());
                        textExDuty.Text = Database.myreader[12].ToString();
                        comboExicseApp.Text = Database.myreader[41].ToString();
                        comboVatApp.Text = Database.myreader[42].ToString();
                        dateTimePickerChallanDate.Value = Convert.ToDateTime(Database.myreader["DelChallanDate"].ToString());
                        textFrieght.Text = Database.myreader[43].ToString();
                        textTCS.Text = Database.myreader[44].ToString();
                        textMobile.Text = Database.myreader["MobileNo"].ToString();
                        dateTimePickerInvDate.Value = Convert.ToDateTime(Database.myreader[2].ToString());
                        textTermsofPayment.Text = Database.myreader[3].ToString();
                        textBuyerOrderNo.Text = Database.myreader[4].ToString();

                        if (textBuyerOrderNo.Text != "")
                            dateTimeBuyerOrderDate.Value = Convert.ToDateTime(Database.myreader[5].ToString());

                        textDesPatchNo.Text = Database.myreader[6].ToString();
                        dateTimePickerDespatchDate.Value = Convert.ToDateTime(Database.myreader[7].ToString());
                        comboVehicalNo.Text = Database.myreader[8].ToString();
                        comboDestination.Text = Database.myreader[9].ToString();
                        textTermsOfDelivery.Text = Database.myreader[10].ToString();
                        CompanyName = Database.myreader[11].ToString();
                        textEduCess.Text = Database.myreader[13].ToString();
                        textSecHiEdu.Text = Database.myreader[14].ToString();
                        textVat.Text = Database.myreader[15].ToString();
                        textAddVat.Text = Database.myreader[16].ToString();
                        textGrdTotal.Text = Database.myreader[23].ToString();
                        dateTimePickerInvoice.Value = Convert.ToDateTime(Database.myreader[25].ToString());
                        dateTimePickerDateTimeRemoval.Value = Convert.ToDateTime(Database.myreader[26].ToString());
                        comboTypeInvoice.Text = Database.myreader[27].ToString();
                        textRemarks.Text = Database.myreader["Remarks"].ToString();
                        if (Database.myreader[40].ToString() == "yes")
                            IsCheckIsurance = true;

                        comboDeptt.Text = Database.myreader[47].ToString();
                        comboType.Text = Database.myreader["TypeVat"].ToString();

                        if (Database.myreader["UTBond"].ToString() != "")
                            checkAvailBenefit.Checked = true;
                        Currency = Database.myreader["Currency"].ToString();

                        textSpDutyRemarks1.Text = Database.myreader[59].ToString();
                        textSpDuty1Amt.Text = Database.myreader[60].ToString();
                        textSpDutyRemark2.Text = Database.myreader[61].ToString();
                        textSpDuty2Amt.Text = Database.myreader[62].ToString();
                        textSpDutyRemarks3.Text = Database.myreader[63].ToString();
                        textSpDuty3Amt.Text = Database.myreader[64].ToString();


                        comboTranporterName.Text = Database.myreader["transportname"].ToString();
                        textHeading.Text = Database.myreader["Heading"].ToString();
                        textConsignee.Text = Database.myreader["Consignee"].ToString();

                        comboAccountHead.Text = Database.myreader["AccountHead"].ToString();
                        textFreightRemarks.Text = Database.myreader["FREIGHTREMARKS"].ToString();
                        textNotifier.Text = Database.myreader["nOTIFIER"].ToString();
                        consigneecode = Database.myreader["ConsigneeCode"].ToString();

                        textVatNilRmarks.Text = Database.myreader["VatNilRemarks"].ToString();
                        textExciseNIlRemarks.Text = Database.myreader["ExciseNilRemarks"].ToString();
                        if (Database.myreader[70].ToString() == "yes")
                            checkFreight.Checked = true;
                        else
                            checkFreight.Checked = false;
                        state = Database.myreader["state"].ToString();
                        if (Database.myreader["ispurchasereturn"].ToString() == "yes")
                            checkIsPurchaseReturn.Checked = true;
                        else
                            checkIsPurchaseReturn.Checked = false;

                        if (Convert.ToString(Database.myreader["IsGSTAllowDuty1"]) == "yes")
                            chkSpDu1.Checked = true;
                        if (Convert.ToString(Database.myreader["IsGSTAllowDuty2"]) == "yes")
                            chkSpDu2.Checked = true;
                        if (Convert.ToString(Database.myreader["IsGSTAllowDuty3"]) == "yes")
                            chkSpDu3.Checked = true;

                        chk_dispfrm.Checked = false;
                        if (Convert.ToString(Database.myreader["isdispfrm"]) == "1")
                        {
                            chk_dispfrm.Checked = true;
                            dispfrm.Visible = true;
                            combodispfrm.Visible = true;
                            combodispfrm.Text = Database.myreader["dispfrm"].ToString();
                            dispfrm_add.Visible = true;
                            textdispfrm_add.Visible = true;
                            textdispfrm_add.Text = Database.myreader["dispfrm_add"].ToString();
                        }

                    }
                    Database.myreader.Close();
                    Database.Closeconnection();

                    comboCurrency.Text = Currency;
                    if (IsCheckIsurance)
                        checkInsurance.Checked = true;
                    else
                        checkInsurance.Checked = false;

                    if (!comboBuyerName.Items.Contains(CompanyName))
                        comboBuyerName.Items.Add(CompanyName);

                    comboBuyerName.Text = CompanyName;

                    comboState.Text = state;
                    if (isagstMarketing)
                        BuyerNameSelection();
                    Database.OpenConnection(Utility.DespacthConnectionString);
                    string consigneename = "";
                    string consigneecity = "";
                    string consigneeadd = "";

                    Database.myreader = Database.GetExecuteReaderCommand("select consigneename,City,ConsigneeAdd from consignee where "
                          + " Consigneecode = '" + consigneecode + "'");
                    if (Database.myreader.Read())
                    {
                        consigneename = Database.myreader[0].ToString();
                        consigneecity = Database.myreader[1].ToString();
                        consigneeadd = Database.myreader[2].ToString();
                    }
                    Database.myreader.Close();
                    Database.Closeconnection();

                    comboConsigneeName.Text = consigneename;
                    comboConsigneeCity.Text = consigneecity;
                    textConsignee.Text = consigneeadd;


                    dataGridView1.Rows.Clear();

                    Database.OpenConnection(Utility.DespacthConnectionString);
                    object j = Database.GetExecuteScalarCommand_return("select count(*) from DomesticInvItem with (NOLOCK) where InvNo = "
                            + textInvNo.Text + " and yr = '" + textSuffix.Text + "' and companyname = '"
                            + comboCompanyName.Text + "'");
                    TotalCount = Convert.ToInt32(j);


                    if (TotalCount > 0)
                        dataGridView1.Rows.Add(TotalCount);


                    //ArrayList ArrSrNo = new ArrayList();
                    //ArrayList ArrItemName = new ArrayList();
                    //ArrayList ArrUnit = new ArrayList();
                    //ArrayList ArrQty = new ArrayList();
                    //ArrayList ArrRate = new ArrayList();
                    //ArrayList ArrNetWt = new ArrayList();
                    //ArrayList ArrGrossWt = new ArrayList();
                    //ArrayList ArrNoOfPkg = new ArrayList();
                    //ArrayList ArrTotal = new ArrayList();
                    //ArrayList ArrCommodity = new ArrayList();
                    //ArrayList ArrNonDisplayQty = new ArrayList();
                    ArrayList ArrGroupCommodity = new ArrayList();
                    ArrayList ArrDeptt = new ArrayList();
                    //ArrayList ArrQtyRemarks = new ArrayList();
                    //ArrayList ArrCommodityRemarks = new ArrayList();
                    ArrayList ArrOrderNo = new ArrayList();
                    int k = 0;
                    Database.myreader = Database.GetExecuteReaderCommand("select * from DomesticInvItem with (NOLOCK) where InvNo = " +
                                                    textInvNo.Text + " and yr = '" + textSuffix.Text + "' and companyname = '"
                            + comboCompanyName.Text + "'");
                    while (Database.myreader.Read())
                    {

                        //ArrSrNo.Add(Database.myreader["SrNo"].ToString());
                        ArrCommodity.Add(Database.myreader["Commodity"].ToString());
                        //ArrItemName.Add(Database.myreader["ItemDesc"].ToString());
                        //ArrUnit.Add(Database.myreader["Unit"].ToString());
                        //ArrQty.Add(Database.myreader["Qty"].ToString());
                        //ArrRate.Add(Database.myreader["Rate"].ToString());
                        //ArrNetWt.Add(Database.myreader["NetWt"].ToString());
                        //ArrGrossWt.Add(Database.myreader["GrossWt"].ToString());
                        //ArrNoOfPkg.Add(Database.myreader["NoofPkg"].ToString());
                        //ArrTotal.Add(Database.myreader["Amount"].ToString());
                        //ArrNonDisplayQty.Add(Database.myreader["NonDisplayQty"].ToString());
                        ArrGroupCommodity.Add(Database.myreader["GroupCommodity"].ToString());
                        //ArrQtyRemarks.Add(Database.myreader["QtyRemarks"].ToString());
                        //ArrCommodityRemarks.Add(Database.myreader["CommodityRemarks"].ToString());
                        //ArrOrderNo.Add(Database.myreader["OrderNo"].ToString());
                        dataGridView1.Rows[k].Cells[DNoofPkg.Index].Value = Database.myreader["NoofPkg"].ToString();
                        //dataGridView1.Rows[k].Cells[1].Value = Database.myreader["Commodity"].ToString();
                        dataGridView1.Rows[k].Cells[DItemDesc.Index].Value = Database.myreader["ItemDesc"].ToString();
                        dataGridView1.Rows[k].Cells[DUOM.Index].Value = Database.myreader["Unit"].ToString();
                        dataGridView1.Rows[k].Cells[DOrderQty.Index].Value = Database.myreader["Qty"].ToString();
                        dataGridView1.Rows[k].Cells[DUnitPrice.Index].Value = Database.myreader["Rate"].ToString();
                        dataGridView1.Rows[k].Cells[DTotal.Index].Value = Database.myreader["Amount"].ToString();
                        dataGridView1.Rows[k].Cells[DNetWt.Index].Value = Database.myreader["NetWt"].ToString();
                        dataGridView1.Rows[k].Cells[DGrossWt.Index].Value = Database.myreader["GrossWt"].ToString();
                        dataGridView1.Rows[k].Cells[DNonDisplayQty.Index].Value = Database.myreader["NonDisplayQty"].ToString();
                        dataGridView1.Rows[k].Cells[DNonDisplayUnit.Index].Value = "KGS";
                        //dataGridView1.Rows[k].Cells[11].Value = Database.myreader["GroupCommodity"].ToString();
                        dataGridView1.Rows[k].Cells[DQtyRemarks.Index].Value = Database.myreader["QtyRemarks"].ToString();
                        dataGridView1.Rows[k].Cells[DCommodityRemarks.Index].Value = Database.myreader["CommodityRemarks"].ToString();
                        dataGridView1.Rows[k].Cells[OrderNo.Index].Value = Database.myreader["OrderNO"].ToString();

                        dataGridView1.Rows[k].Cells[colHSNCode.Index].Value = Convert.ToString(Database.myreader["HSNCode"]);
                        dataGridView1.Rows[k].Cells[colCGSTRate.Index].Value = Utility.SafeConvertToDouble(Database.myreader["CGSTrate"]);
                        dataGridView1.Rows[k].Cells[ColCGSTAmt.Index].Value = Utility.SafeConvertToDouble(Database.myreader["CGSTamt"]);
                        dataGridView1.Rows[k].Cells[ColSGSTRate.Index].Value = Utility.SafeConvertToDouble(Database.myreader["SGSTRate"]);
                        dataGridView1.Rows[k].Cells[colSGSTAmt.Index].Value = Utility.SafeConvertToDouble(Database.myreader["SGSTAmt"]);
                        dataGridView1.Rows[k].Cells[ColIGSTRate.Index].Value = Utility.SafeConvertToDouble(Database.myreader["IGSTrate"]);
                        dataGridView1.Rows[k].Cells[colIGSTAmt.Index].Value = Utility.SafeConvertToDouble(Database.myreader["IGSTAmt"]);
                        dataGridView1.Rows[k].Cells[colMainGrp.Index].Value = DespatchCommonFunction.GetDeptt(Database.myreader["ItemCode"].ToString());
                        string grp = "";
                        string subgrp = "";
                        DespatchCommonFunction.GetItemGropuName(Database.myreader["ItemCode"].ToString(), comboCompanyName.Text.ToString(), ref grp, ref subgrp);
                        dataGridView1.Rows[k].Cells[colGroupName.Index].Value = grp;
                        dataGridView1.Rows[k].Cells[colSubGrp.Index].Value = subgrp;
                        dataGridView1.Rows[k].Cells[colMainGrp.Index].Value = DespatchCommonFunction.GetDeptt(Database.myreader["ItemCode"].ToString());
                        dataGridView1.Rows[k].Cells[colItemCode.Index].Value = Convert.ToString(Database.myreader["ItemCode"]);
                        ArrOrderNo.Add(Database.myreader["OrderNo"].ToString());
                        ArrDeptt.Add(Database.myreader["ItemDeptt"].ToString());
                        k++;
                    }
                    Database.myreader.Close();

                    for (int i = 0; i < ArrOrderNo.Count; i++)
                    {
                        Database.myreader = Database.GetExecuteReaderCommand("select distinct(Invno) from MarketingInvitem with (NOLOCK) where ItemNO = '"
                        + ArrOrderNo[i].ToString() + "' union all select distinct(workordernumber) from Multilayerworkorder with (NOLOCK) where workordernumber = '"
                        + ArrOrderNo[i].ToString() + "' union all select distinct(workordernumber) from BOPPworkorder with (NOLOCK) where workordernumber = '"
                        + ArrOrderNo[i].ToString() + "'");
                        if (Database.myreader.Read())
                        {
                            for (int n = 0; n < checkedListBox1.Items.Count; n++)
                            {
                                if (Database.myreader[0].ToString() == checkedListBox1.Items[n].ToString())
                                {
                                    checkedListBox1.SetItemChecked(n, true);

                                }
                                else
                                {
                                    if (checkedListBox1.Items.Contains(Database.myreader[0].ToString()))
                                    {
                                    }
                                    else
                                        checkedListBox1.Items.Add(Database.myreader[0].ToString());

                                }
                            }
                        }
                        Database.myreader.Close();
                    }



                    for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                    {
                        if (ArrCommodity[i].ToString() == "")
                        {
                            string CommodityName = "";

                            Database.myreader = Database.GetExecuteReaderCommand("select * from DomesticInvItem with (NOLOCK) where InvNo = "
                                                               + textInvNo.Text + " and  GroupCommodity = '"
                                                               + ArrGroupCommodity[i].ToString() + "'"
                                                               + " and yr = '" + textSuffix.Text + "' and companyname = '"
                            + comboCompanyName.Text + "' and commodity != ''");
                            if (Database.myreader.Read())
                            {
                                CommodityName = Database.myreader["Commodity"].ToString();
                                ArrCommodity.RemoveAt(i);
                                ArrCommodity.Insert(i, CommodityName);

                            }
                            Database.myreader.Close();
                        }
                    }


                    for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                    {
                        dataGridView1.Rows[i].Cells[DCommodity.Index].Value = ArrCommodity[i].ToString();
                        dataGridView1.Rows[i].Cells[DtDeptt.Index].Value = ArrDeptt[i].ToString();
                    }

                    if (Database.OpenConnection(Utility.DespacthConnectionString))
                    {
                        DataSet dataset1 = new DataSet();
                        if (comboCompanyName.Text == "Oswal Extrusion Limited (Unit-II)")
                        {
                            Database.myadapter = Database.GetAdapterCommand("select distinct GSTInvNo from Vw_DomesticInvoiceOEL2 where  InvNo = "
                                                    + textInvNo.Text + " and suffix = '" + textSuffix.Text + "' and  companyname = '"
                                                                     + comboCompanyName.Text + "'  ");
                            Database.myadapter.Fill(dataset1, "Vw_DomesticInvoiceOEL2");
                        }
                        else
                        {
                            Database.myadapter = Database.GetAdapterCommand("select distinct GSTInvNo from Vw_DomesticInvoice where  InvNo = "
                                                    + textInvNo.Text + " and suffix = '" + textSuffix.Text + "' and  companyname = '"
                                                                     + comboCompanyName.Text + "'  ");
                            Database.myadapter.Fill(dataset1, "Vw_DomesticInvoice");
                        }


                        string strgstInv = Convert.ToString(dataset1.Tables[0].Rows[0]["GSTInvNo"]);

                        TxtEwayNo.Text = Convert.ToString(Database.GetExecuteNonQueryCommand_retrun("Select EWayNo from EWayBillMaster where companyname='"
                            + comboCompanyName.Text + "' and yr='" + textSuffix.Text + "' and  invno='" + strgstInv + "'"));
                        if (TxtEwayNo.Text.Length == 0)
                            TxtEwayNo.Enabled = true;
                        else
                            TxtEwayNo.Enabled = false;
                    }

                    Database.Closeconnection();
                    isupdate = false;
                }
            }
            catch (Exception ex)
            {
                isupdate = false;
                ErrorMessageBox.Show(ex);
            }
        }

        private void btnInitliaze_Click(object sender, EventArgs e)
        {
            try
            {
                textNotifier.Text = "";
                textFreightRemarks.Text = "";
                TotalCount = 0;
                textAddVat.Text = "1";
                textBoxLRNo.Text = "";
                textBuyerOrderNo.Text = "";
                textDelChallanNo.Text = "";
                textDesPatchNo.Text = "";
                comboDestination.Text = "";
                textEduCess.Text = "0";
                textExDuty.Text = "12.5";
                textInvNo.Text = "";
                textTermsOfDelivery.Text = "";
                textSecHiEdu.Text = "0";
                textTermsofPayment.Text = "";
                textVat.Text = "4";
                comboVehicalNo.Text = "";
                textMobile.Text = "";
                checkInsurance.Checked = false;
                checkFreight.Checked = false;
                comboBuyerName.Text = "";
                btnUpdate.Enabled = false;
                btnSave.Enabled = true;
                dataGridView1.Rows.Clear();
                checkedListBox1.Items.Clear();
                isagstMarketing = false;
                checkIsPurchaseReturn.Checked = false;
                if (DateTime.Now >= Convert.ToDateTime("2019-01-01"))
                    checkIsPurchaseReturn.Visible = false;
                else
                    checkIsPurchaseReturn.Visible = true;

                if (Database.OpenConnection(Utility.MaterialConnectionString))
                {
                    Database.myreader = Database.GetExecuteReaderCommand("select * from MaterialProcessing..excisemaster");
                    if (Database.myreader.Read())
                    {
                        textExDuty.Text = Database.myreader[0].ToString();
                        textEduCess.Text = Database.myreader[1].ToString();
                        textSecHiEdu.Text = Database.myreader[2].ToString();
                    }
                    Database.myreader.Close();
                }
                fillData(false);

            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
            }
        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            #region 19-08-2020 lock as per accounting voucher type
            if (!Utility.CheckAccountVoucherDateFreeze(dateTimePickerInvDate.Value, comboCompanyName.Text, "sales"))
            {
                MessageBox.Show(AccountCommonVariable.ErrMsg.DATE_FREEZE + "\nPlease check Invoice Date");
                return;
            }
            if (!Utility.CheckAccountVoucherDateFreeze(dateTimePickerInvoice.Value, comboCompanyName.Text, "sales"))
            {
                MessageBox.Show(AccountCommonVariable.ErrMsg.DATE_FREEZE + "\nPlease check date and Time of Invoice");
                return;
            }
            if (!Utility.CheckAccountVoucherDateFreeze(dateTimePickerDespatchDate.Value, comboCompanyName.Text, "sales"))
            {
                MessageBox.Show(AccountCommonVariable.ErrMsg.DATE_FREEZE + "\nPlease check despatch date");
                return;
            }
            if (!Utility.CheckAccountVoucherDateFreeze(dateTimePickerDateTimeRemoval.Value, comboCompanyName.Text, "sales"))
            {
                MessageBox.Show(AccountCommonVariable.ErrMsg.DATE_FREEZE + "\nPlease check date and Time of Removal");
                return;
            }
            #endregion

            string desPackinglist = "";
            string desPackinglistBeforeUpdate = ""; // INVOICE-RULE: packing list already on this invoice
            if (DespatchCommonFunction.IsAccountingDone(comboCompanyName.Text, textInvNo.Text, textSuffix.Text))
            {
                MessageBox.Show("Accounting Entry is done for the selected Invoice.\nPlease Contact Account Administrator");
                return;
            }

            int VenCnt = DespatchCommonFunction.CustomerLedgerName(comboBuyerName.Text, comboCompanyName.Text);
            if (VenCnt == 0)
            {
                MessageBox.Show("No Account Ledger Found for this customer.\nPlease Ask Account Dept to create Ledger for this customer:- [" + comboBuyerName.Text + "]");
                return;
            }
            if (Database.OpenConnection(Utility.DespacthConnectionString))
            {
                if (dateTimePickerInvDate.Value >= Convert.ToDateTime("2017-07-01 00:00:00"))
                {
                    string newgstno = Convert.ToString(Database.GetExecuteNonQueryCommand_retrun("select isnull(NewGSTNo,'') as NewGSTNo from CompanyMaster where CompanyName='"
                        + comboBuyerName.Text + "'"));
                    if (newgstno.Length == 0)
                    {
                        MessageBox.Show("Please Enter GSTIN No for This Customer");
                        return;
                    }
                    if (newgstno == DespatchCommonFunction.CompanyGSTNo(comboCompanyName.Text))
                    {
                        MessageBox.Show("Domestic invoice will not geneated for SAME GSTIN Company.\nPlease Created Delivery Challan for branch transfer");
                        return;
                    }
                    for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                    {
                        string hsncode = Convert.ToString(dataGridView1.Rows[i].Cells[colHSNCode.Index].Value);
                        if (hsncode == "")
                        {
                            MessageBox.Show("Please Enter HSN Code for Item :" + dataGridView1.Rows[i].Cells[DCommodity.Index].Value);
                            return;
                        }
                        if ((hsncode.Length < 6) && (dateTimePickerInvDate.Value >= Convert.ToDateTime("2021-04-01")))
                        {
                            MessageBox.Show("Please Enter 6 Digit HSN Code for Item :" + dataGridView1.Rows[i].Cells[DCommodity.Index].Value);
                            return;
                        }
                        string itemcode = Convert.ToString(dataGridView1.Rows[i].Cells[colItemCode.Index].Value);
                        if (itemcode.Length == 0)
                        {
                            MessageBox.Show("Please Select Item ");
                            return;
                        }
                        if (dataGridView1.Rows[i].Cells[DCommodity.Index].FormattedValue.ToString() == "")
                        {
                            MessageBox.Show("Please Select Proper Commodity ");
                            return;
                        }
                        if (dataGridView1.Rows[i].Cells[DUOM.Index].FormattedValue.ToString() == "")
                        {
                            MessageBox.Show("Please Select Unit ");
                            return;
                        }
                        if (dataGridView1.Rows[i].Cells[DOrderQty.Index].FormattedValue.ToString() == "")
                        {
                            MessageBox.Show("Please Enter Order Qty");
                            return;
                        }
                        if (dataGridView1.Rows[i].Cells[DNetWt.Index].FormattedValue.ToString() == "")
                        {
                            MessageBox.Show("Please Enter Netwt");
                            return;
                        }
                        if (dataGridView1.Rows[i].Cells[DUnitPrice.Index].FormattedValue.ToString() == "")
                        {
                            MessageBox.Show("Please Enter Unit Price ");
                            return;
                        }
                        if (dataGridView1.Rows[i].Cells[DUOM.Index].FormattedValue.ToString().ToLower() == "kgs")
                        {
                            if (dataGridView1.Rows[i].Cells[DOrderQty.Index].FormattedValue.ToString() !=
                                dataGridView1.Rows[i].Cells[DNetWt.Index].FormattedValue.ToString())
                            {
                                MessageBox.Show("Please Enter Proper Netwt ");
                                return;
                            }
                        }
                    }
                }
            }

            isupdatefororder = true;

            bool isMultilayer = false;

            if (comboDeptt.Text == "")
            {
                MessageBox.Show("Please enter Value in DepttName");
                return;
            }

            //if (comboDeptt.Text == "Multilayer")
            //    isMultilayer = true;

            if (!FrmMain.IsUpdateDomesticInvoice)
                MessageBox.Show("You have no right to update Domestic Invoice");
            else if (textBoxLRNo.Text == "")
                MessageBox.Show("Please enter Value in LRNo");
            else if (comboState.Text == "")
                MessageBox.Show("Please select State ");
            else if (!(textTermsofPayment.Items.Contains(textTermsofPayment.Text.Trim())))
                MessageBox.Show("Please select proper payment terms or enter payment term in payment master");
            else if (OrderQuantity())
            {
                MessageBox.Show("Inv Qty is greater than Pending Qty");
            }
            else
            {
                bool IsValidBuyerName = false;
                if (Database.OpenConnection(Utility.DespacthConnectionString))
                {
                    Database.myreader = Database.GetExecuteReaderCommand("Select * from CompanyMaster where CompanyName = '"
                         + comboBuyerName.Text + "'");
                    if (Database.myreader.Read())
                        IsValidBuyerName = true;
                    Database.myreader.Close();
                    Database.Closeconnection();
                }
                if (!IsValidBuyerName)
                    MessageBox.Show(" Invalid Buyer Name");
                else if (!FrmMain.IsUpdateDomesticInvoice)
                    MessageBox.Show("You do not have right to update Domestic Invoice");
                else
                {
                    #region PackingList Check
                    if (isMultilayer == false)
                    {
                        if (DespatchCommonFunction.AllowMappingPackinglist(comboCompanyName.Text) > 0)
                        {
                            //check with Packing List
                            double TotalNetwt = 0;
                            for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                            {
                                //DNetWt
                                if (Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[DNetWt.Index].Value.ToString()) > 0)
                                {
                                    TotalNetwt += Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[DNetWt.Index].Value.ToString());
                                }
                                else
                                {
                                    TotalNetwt += Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[DOrderQty.Index].Value.ToString());
                                }
                            }

                            if (DespatchCommonFunction.AllowMappingPackinglist(comboCompanyName.Text) > 0)
                            {
                                string strgstInv = Convert.ToString(Database.GetExecuteNonQueryCommand_retrun("SELECT GSTInvNo FROM Vw_DomesticInvoice WHERE InvNo=" +
                                                    textInvNo.Text + " and companyname='" + comboCompanyName.Text + "' and ARESuffix='" + textSuffix.Text + "'"));

                                desPackinglist = Convert.ToString(Database.GetExecuteNonQueryCommand_retrun("SELECT DISTINCT TOP 1 dbo.DistinctList (MappPackingList.PackingListNo ,',') PackingListNo FROM MappPackingList WHERE companyname='" + comboCompanyName.Text + "' AND InvNo='" + strgstInv + "' AND Yr='" + textSuffix.Text + "'"));
                                desPackinglistBeforeUpdate = desPackinglist; // INVOICE-RULE: remember old map to plus Despatch back

                                if (desPackinglist == "")
                                {
                                    if (!DespatchCommonFunction.GetPackingListInvoiceNo2(comboCompanyName.Text, comboBuyerName.Text, TotalNetwt, dataGridView1, colItemCode.Name, DNetWt.Name, ref desPackinglist))
                                    {
                                        return;
                                    }
                                }
                                else
                                {

                                    if (Database.OpenConnection(Utility.MaterialConnectionString))
                                    {
                                        string[] split = desPackinglist.Split(',');
                                        double totalpackinglist = 0;
                                        for (int i = 0; i < split.Length; i++)
                                        {
                                            string strsql = "select sum(isnull(Netwt,0)) Netwt from vw_PackingListAll where companyname='" + comboCompanyName.Text + "'"
                                                //+ " and  yr='" + textSuffix.Text + "' and "
                                                + " AND (PACKINGLISTNO ='" + split[i] + "' or invno='" + split[i] + "') ";
                                            Database.myreader = Database.GetExecuteReaderCommand(strsql);
                                            if (Database.myreader.Read())
                                            {
                                                totalpackinglist += Utility.SafeConvertToDouble(Database.myreader[0].ToString());
                                            }
                                            Database.myreader.Close();
                                            Database.Closeconnection();
                                        }
                                        if (Math.Round(Utility.SafeConvertToDouble(TotalNetwt),FrmMain.ERPQtyDigit) != Math.Round(Utility.SafeConvertToDouble(totalpackinglist),FrmMain.ERPQtyDigit))
                                        {
                                            MessageBox.Show("PackingList and Invoice Qty not match");
                                            return;
                                        }
                                        #region check for item
                                        DataTable dt = Database.GetDataTable("Select * from [vw_PackingListAll_dtl] where PACKINGLISTNO in ('" + desPackinglist.Replace(",", "','") + "')");
                                        for (int i = 0; i < this.dataGridView1.Rows.Count - 1; i++)
                                        {
                                            string griditemcode = Convert.ToString(dataGridView1.Rows[i].Cells["colItemCode"].Value);
                                            dt.DefaultView.RowFilter = "Itemcode='" + griditemcode + "'";
                                            DataTable dv = dt.DefaultView.ToTable();
                                            if (dv.Rows.Count == 0)
                                            {
                                                MessageBox.Show("Packing List Item is not match ");
                                                return;
                                            }
                                        }
                                        #endregion
                                    }
                                }
                            }
                        }
                    }
                    #endregion


                    double total = 0;
                    bool IsWt = false;
                    try
                    {
                        bool IsAlreadyPrint = false;
                        Database.OpenConnection(Utility.DespacthConnectionString);
                        Database.myreader = Database.GetExecuteReaderCommand("select * from printdomesticinvoice where InvNo = "
                             + textInvNo.Text
                             + " and yr  = '" + textSuffix.Text + "' and companyname ='"
                             + comboCompanyName.Text + "'");
                        if (Database.myreader.Read())
                            IsAlreadyPrint = true;
                        Database.myreader.Close();



                        if (dateTimePickerInvDate.Value > Convert.ToDateTime("2017-06-30 00:00:00"))
                        {
                            total = CalculationGST();
                        }
                        else
                        {
                            total = Calculation();
                        }

                        textGrdTotal.Text = total.ToString();
                        IsWt = WeightDifferences();

                        if (IsAlreadyPrint)
                            MessageBox.Show("Following  Invoice is already Printed so that you cannot change Domestic Invoice");
                        else if (TotalCount < 0)
                            MessageBox.Show("Please Enter Item Item Information");
                        else if (IsWt)
                            MessageBox.Show("Net Weight should be lesser than Gross Weight");
                        else if (!MatchCommodity_Theding(TotalCount))
                            MessageBox.Show("Please select proper Commodity,THeading of Commodity would be require same",
                                     "THeading should be macth", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        else
                        {
                            if (Database.OpenConnection(Utility.DespacthConnectionString))
                            {
                                DialogResult drs = MessageBox.Show("Do you want to update Domestic Invoice", "Updation Domestic Invoice"
                                                 , MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                                if (drs.CompareTo(DialogResult.Yes) == 0)
                                {
                                    Database.BeginTransaction();

                                    double TotalAmt = 0;
                                    string CheckInsurane = "";
                                    Database.myreader = Database.GetExecuteReaderCommand("select TotalAmt,CheckInsurance from domesticinvoice where Invno = "
                                        + textInvNo.Text + " and suffix = '" + textSuffix.Text
                                        + "' and companyname = '" + comboCompanyName.Text + "'");
                                    if (Database.myreader.Read())
                                    {
                                        TotalAmt = Convert.ToDouble(Database.myreader[0].ToString());
                                        CheckInsurane = Database.myreader[1].ToString();
                                    }
                                    Database.myreader.Close();
                                    string isfreghtinclude = "";
                                    if (checkFreight.Checked)
                                        isfreghtinclude = "yes";
                                    else
                                        isfreghtinclude = "no";
                                    string InsuranceApp = "";
                                    string Insurance = "";

                                    if (checkInsurance.Checked)
                                    {
                                        InsuranceApp = "yes";
                                        Insurance = textInsurance.Text;
                                    }
                                    else
                                        InsuranceApp = "no";

                                    string ispurchasereturn = "";
                                    if (checkIsPurchaseReturn.Checked)
                                        ispurchasereturn = "yes";
                                    else
                                        ispurchasereturn = "no";

                                    if (checkInsurance.Checked == false && CheckInsurane == "yes")
                                        UpdateInsurancePolicy(-TotalAmt, true);
                                    else if (checkInsurance.Checked == true && CheckInsurane == "no")
                                        UpdateInsurancePolicy(total, true);
                                    else if (checkInsurance.Checked)
                                        UpdateInsurancePolicy(total - TotalAmt, true);
                                    string ConsigneeCode = "";
                                    Database.myreader = Database.GetExecuteReaderCommand("select ConsigneeCode from Consignee where consigneename = '"
                                         + comboConsigneeName.Text + "' and companycode ='" + CompanyCode
                                         + "' and city = '" + comboConsigneeCity.Text + "'");
                                    if (Database.myreader.Read())
                                        ConsigneeCode = Database.myreader[0].ToString();
                                    Database.myreader.Close();


                                    Database.GetExecuteNonQueryCommand("Update DomesticInvoice set  Buyername = '" + comboBuyerName.Text + "',"
                                                                               + " TermsofPayment ='" + textTermsofPayment.Text + "',"
                                                                               + " DespatchNo = '" + textDesPatchNo.Text + "',"
                                                                               + " DespatchDate = '" + dateTimePickerDespatchDate.Value.ToString(DespatchCommonFunction.ERPDBDateSettings()) + "',"
                                                                               + " VehicalNo = '" + comboVehicalNo.Text.ToUpper() + "',"
                                                                               + " Destination = '" + comboDestination.Text.ToUpper() + "',"
                                                                               + " TermsofDelivery = '" + textTermsOfDelivery.Text + "',"
                                                                               + " Exduty = " + textExDuty.Text + ","
                                                                               + " Educess = " + textEduCess.Text + ","
                                                                               + " SecHiEduCess = " + textSecHiEdu.Text + ","
                                                                               + " vat = " + textVat.Text + ","
                                                                               + " Addvat = " + textAddVat.Text + ","
                                                                               + " ExciseAmt =" + Excise.ToString() + ","
                                                                               + " EduCessAmt = " + Educess.ToString() + ","
                                                                               + " SecHiEduCessAmt = " + HiEduCess.ToString() + ","
                                                                               + " VatAmt = " + Vat.ToString() + ","
                                                                               + " AddVatAmt =" + AddVat.ToString() + ","
                                                                               + " TotalAmt = " + textGrdTotal.Text + ","
                                                                               + " RemDateTime = '" + dateTimePickerDateTimeRemoval.Value.ToString(DespatchCommonFunction.ERPDBDateSettings()) + "',"
                                                                               + " DelChallanNo = '" + textDelChallanNo.Text + "',"
                                                                               + " DelChallanDate = '" + dateTimePickerChallanDate.Value.ToString(DespatchCommonFunction.ERPDBDateSettings()) + "',"
                                                                               + " LRNo = '" + textBoxLRNo.Text + "',"
                                                                               + " LRDate = '" + dateTimePickerLRNo.Value.ToString(DespatchCommonFunction.ERPDBDateSettings()) + "',"
                                                                               + " TotalAmtWord = '" + Utility.IntegerToWords(Convert.ToInt64(Convert.ToDouble(textGrdTotal.Text))) + "',"
                                                                               + " ExciseAmtWord = '" + Utility.IntegerToWords(Convert.ToInt64(Excise + Educess + HiEduCess)) + "',"
                                                                               + " Consignee = '" + textConsignee.Text + "',"
                                                                               + " TransportName = '" + comboTranporterName.Text + "',"
                                                                               + " Heading = '" + textHeading.Text + "',"
                                                                               + " Insurance = '" + Insurance + "',"
                                                                               + " CheckInsurance = '" + InsuranceApp + "',"
                                                                               + " Typevat = '" + comboType.Text + "',"
                                                                               + " Remarks = '" + textRemarks.Text + "',"
                                                                               + " Frieght = " + textFrieght.Text + ","
                                                                               + " TCSPer = " + textTCS.Text + ","
                                                                               + " TCSAmt = " + TCS
                                                                               + " ,Deptt = '" + comboDeptt.Text + "',"
                                                                               + "TypeOfInvoice = '" + comboTypeInvoice.Text + "',"
                                                                               + "Currency = '" + comboCurrency.Text + "',"
                                                                               + "Currencyvalue = '" + comboCurrencyValue.Text + "'"
                                                                               + ",AccountHead = '" + comboAccountHead.Text + "',Notifier = '"
                                                                               + textNotifier.Text + "',FREIGHTREMARKS = '" + textFreightRemarks.Text + "',isfreightinclude = '" + isfreghtinclude + "'"
                                                                               + ",ExciseNilRemarks = '"
                                                                               + textExciseNIlRemarks.Text + "',VatNilRemarks = '"
                                                                               + textVatNilRmarks.Text + "',state = '"
                                                                               + comboState.Text + "' ,ispurchasereturn = '"
                                                                               + ispurchasereturn + "',mobileno='" + textMobile.Text + "',"
                                                                               + "isdispfrm = " + (chk_dispfrm.Checked ? "1" : "0") + ","
                                                                               + "dispfrm = '" + combodispfrm.Text + "',"
                                                                               + "dispfrm_add = '" + textdispfrm_add.Text + "'"
                                                                               + " where Invno =" + textInvNo.Text + " and suffix = '"
                                                                               + textSuffix.Text + "' and companyname = '"
                                                                               + comboCompanyName.Text + "'");

                                    Database.GetExecuteNonQueryCommand("Update DomesticInvoice set IsGSTAllowDuty1 ='" + (chkSpDu1.Checked == true ? "yes" : "no")
                                        + "',IsGSTAllowDuty2='" + (chkSpDu2.Checked == true ? "yes" : "no") + "',IsGSTAllowDuty3='" + (chkSpDu3.Checked == true ? "yes" : "no")
                                        + "' where Invno =" + textInvNo.Text + " and suffix = '"
                                        + textSuffix.Text + "' and companyname = '"
                                        + comboCompanyName.Text + "'");

                                    if (comboExicseApp.SelectedIndex == 0) //Excise Nil
                                        Database.GetExecuteNonQueryCommand("Update DomesticInvoice set ExciseAmtWord = 'NIL' where Invno = "
                                            + textInvNo.Text + " and suffix = '" + textSuffix.Text + "' and companyname = '" + comboCompanyName.Text + "'");
                                    if (textBuyerOrderNo.Text != "")
                                    {
                                        Database.GetExecuteNonQueryCommand("update domesticinvoice set BuyerOrderNo = '" + textBuyerOrderNo.Text + "',"
                                                                            + "BuyerDate = '" + dateTimeBuyerOrderDate.Value.ToString() + "'"
                                                                            + " where Invno =" + textInvNo.Text + " and suffix = '"
                                                                            + textSuffix.Text + "' and companyname = '"
                                                                            + comboCompanyName.Text + "'");
                                    }
                                    else
                                    {
                                        Database.GetExecuteNonQueryCommand("update domesticinvoice set BuyerOrderNo = '" + textBuyerOrderNo.Text + "',"
                                                                           + "BuyerDate = null"
                                                                           + " where Invno =" + textInvNo.Text + " and suffix = '"
                                                                           + textSuffix.Text + "' and companyname = '"
                                                                           + comboCompanyName.Text + "'");
                                    }


                                    Database.GetExecuteNonQueryCommand("Update DomesticInvoice set spDuty1 = '" + textSpDutyRemarks1.Text
                                              + "',SpDuty1Amt = " + textSpDuty1Amt.Text
                                              + ",SpDuty2 = '" + textSpDutyRemark2.Text
                                              + "',SpDuty2Amt = " + textSpDuty2Amt.Text
                                               + ",SpDuty3 = '" + textSpDutyRemarks3.Text
                                                + "',spDuty3Amt =  " + textSpDuty3Amt.Text
                                              + ",consigneecode = '" + ConsigneeCode + "' where Invno = "
                                            + textInvNo.Text + " and suffix = '" + textSuffix.Text + "' and companyname = '" + comboCompanyName.Text + "'");


                                    //if (Database.OpenConnection(Utility.DespacthConnectionString))
                                    //{
                                    Database.GetExecuteNonQueryCommand("delete from DomesticInvItem where InvNo = "
                                        + textInvNo.Text + " and yr = '" + textSuffix.Text + "' and companyname = '"
                                        + comboCompanyName.Text + "'");
                                    //    Database.Closeconnection();
                                    //}

                                    bool IsExecute = false;
                                    for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                                    {

                                        if (dataGridView1[DItemDesc.Index, i].ToString() != "" &&
                                            dataGridView1[DUnitPrice.Index, i].ToString() != ""
                                            && dataGridView1[DTotal.Index, i].ToString() != ""
                                            && dataGridView1[DNetWt.Index, i].ToString() != "")
                                        {
                                            //if (Database.OpenConnection(Utility.DespacthConnectionString))
                                            //{
                                            string GroupName = "a";
                                            bool IsAvailInDatabase = false;
                                            string Commodity = "";
                                            int TotalCommodity = 0;
                                            Database.myreader = Database.GetExecuteReaderCommand("select isnull(count(Distinct(commodity)),0) from DomesticInvItem where InvNo ="
                                                                         + textInvNo.Text + " and yr = '" + textSuffix.Text + "' and companyname = '" + comboCompanyName.Text + "'");
                                            if (Database.myreader.Read())
                                            {
                                                TotalCommodity = Convert.ToInt32(Database.myreader[0].ToString());
                                            }
                                            Database.myreader.Close();

                                            Database.myreader = Database.GetExecuteReaderCommand("select groupcommodity from DomesticInvItem where InvNo ="
                                                          + textInvNo.Text + " and commodity = '" + dataGridView1.Rows[i].Cells[DCommodity.Index].Value.ToString() + "' and yr = '"
                                                          + textSuffix.Text + "' and companyname = '" + comboCompanyName.Text + "'");
                                            if (Database.myreader.Read())
                                            {
                                                GroupName = Database.myreader[0].ToString();
                                                Commodity = "";
                                                IsAvailInDatabase = true;
                                            }
                                            Database.myreader.Close();

                                            if (TotalCommodity == 1 && !IsAvailInDatabase)
                                                GroupName = "b";
                                            if (TotalCommodity == 2 && !IsAvailInDatabase)
                                                GroupName = "c";
                                            if (TotalCommodity == 3 && !IsAvailInDatabase)
                                                GroupName = "d";
                                            if (TotalCommodity == 4 && !IsAvailInDatabase)
                                                GroupName = "e";

                                            if (!IsAvailInDatabase)
                                                Commodity = dataGridView1[DCommodity.Index, i].ToString();


                                            if (dataGridView1.Rows[i].Cells[DNoofPkg.Index].FormattedValue.ToString() == "")
                                                dataGridView1.Rows[i].Cells[DNoofPkg.Index].Value = ".";

                                            if (dataGridView1.Rows[i].Cells[DItemDesc.Index].FormattedValue.ToString() == "")
                                                dataGridView1.Rows[i].Cells[DItemDesc.Index].Value = ".";

                                            if (dataGridView1.Rows[i].Cells[DOrderQty.Index].FormattedValue.ToString() == "")
                                                dataGridView1.Rows[i].Cells[DOrderQty.Index].Value = ".";

                                            if (dataGridView1.Rows[i].Cells[DUnitPrice.Index].FormattedValue.ToString() == "")
                                                dataGridView1.Rows[i].Cells[DUnitPrice.Index].Value = ".";

                                            if (dataGridView1.Rows[i].Cells[DUOM.Index].FormattedValue.ToString() == "")
                                                dataGridView1.Rows[i].Cells[DUOM.Index].Value = ".";

                                            if (dataGridView1.Rows[i].Cells[DTotal.Index].FormattedValue.ToString() == "")
                                                dataGridView1.Rows[i].Cells[DTotal.Index].Value = "0";

                                            if (dataGridView1.Rows[i].Cells[DNetWt.Index].FormattedValue.ToString() == "")
                                                dataGridView1.Rows[i].Cells[DNetWt.Index].Value.ToString();
                                            if (dataGridView1.Rows[i].Cells[DGrossWt.Index].FormattedValue.ToString() == "")
                                                dataGridView1.Rows[i].Cells[DGrossWt.Index].Value.ToString();

                                            if (dataGridView1.Rows[i].Cells[DNonDisplayUnit.Index].FormattedValue.ToString() == "")
                                                dataGridView1.Rows[i].Cells[DNonDisplayUnit.Index].Value = ".";


                                            double NonDisplayQty = 0;
                                            if (dataGridView1.Rows[i].Cells[DUOM.Index].FormattedValue.ToString() ==
                                                    dataGridView1.Rows[i].Cells[DNonDisplayUnit.Index].FormattedValue.ToString())
                                                NonDisplayQty = Convert.ToDouble(dataGridView1.Rows[i].Cells[DOrderQty.Index].Value.ToString());
                                            else if (dataGridView1.Rows[i].Cells[DNonDisplayQty.Index].FormattedValue.ToString() == "")
                                                NonDisplayQty = 0;
                                            else
                                                NonDisplayQty = Convert.ToDouble(dataGridView1.Rows[i].Cells[DNonDisplayQty.Index].Value.ToString());

                                            if (dataGridView1.Rows[i].Cells[DQtyRemarks.Index].FormattedValue.ToString() == "")
                                                dataGridView1.Rows[i].Cells[DQtyRemarks.Index].Value = ".";

                                            if (dataGridView1.Rows[i].Cells[DCommodityRemarks.Index].FormattedValue.ToString() == "")
                                                dataGridView1.Rows[i].Cells[DCommodityRemarks.Index].Value = ".";

                                            if (isagstMarketing)
                                            {
                                                if (Database.GetExecuteNonQueryCommand("insert into DomesticInvItem(InvNo,NoofPkg,ItemDesc,Qty,Rate,Unit,Amount,NetWt,GrossWt,SalesOrderNo,LoginName,Commodity,SrNo,GroupCommodity,NonDisplayUnit,NonDisplayQty,QtyRemarks,Yr,Companyname,CommodityRemarks,Desc1,Desc2,BagsMRP,size,weekno,LotNo,Colour,Meter,GPM,GSM,WBWeight,ReqWtperBag,ReqWtTotal,DispatchWtperBagGr,WtTolperBagGr,WtExtrapershortKg,WtDiffbetPLWB,AssesableValue,OrderNO,itemDeptt,HSNCode,CGSTrate,CGSTamt,SGSTRate,SGSTAmt,IGSTRate,IGSTAmt,ItemCode) values('"
                                                            + textInvNo.Text + "','" + dataGridView1.Rows[i].Cells[DNoofPkg.Index].Value
                                                            + "','" + dataGridView1.Rows[i].Cells[DItemDesc.Index].Value
                                                            + "','" + dataGridView1.Rows[i].Cells[DOrderQty.Index].Value
                                                            + "','" + dataGridView1.Rows[i].Cells[DUnitPrice.Index].Value
                                                            + "','" + dataGridView1.Rows[i].Cells[DUOM.Index].Value
                                                            + "','" + dataGridView1.Rows[i].Cells[DTotal.Index].Value
                                                            + "','" + dataGridView1.Rows[i].Cells[DNetWt.Index].Value
                                                            + "','" + dataGridView1.Rows[i].Cells[DGrossWt.Index].Value + "','"
                                                            + "','" + FrmMain.UserName
                                                            + "','" + dataGridView1.Rows[i].Cells[DCommodity.Index].Value.ToString()
                                                            + "','" + Convert.ToString((i + 1))
                                                            + "','" + GroupName
                                                            + "','" + dataGridView1.Rows[i].Cells[DNonDisplayUnit.Index].Value.ToString()
                                                            + "','" + NonDisplayQty.ToString()
                                                            + "','" + dataGridView1.Rows[i].Cells[DQtyRemarks.Index].Value.ToString()
                                                            + "','" + textSuffix.Text
                                                            + "','" + comboCompanyName.Text
                                                            + "','" + dataGridView1.Rows[i].Cells[DCommodityRemarks.Index].Value.ToString()
                                                            + "',null,null,null,null,null,null,null,null,null,null,null,"
                                                            + "null,null,null,null,null,null,null" + ",'" + dataGridView1.Rows[i].Cells[OrderNo.Index].Value.ToString()
                                                            + "','" + dataGridView1.Rows[i].Cells[DtDeptt.Index].Value.ToString()
                                                            + "','" + dataGridView1.Rows[i].Cells[colHSNCode.Index].Value.ToString()
                                                            + "'," + Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[colCGSTRate.Index].Value)
                                                            + "," + Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[ColCGSTAmt.Index].Value)
                                                            + "," + Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[ColSGSTRate.Index].Value)
                                                            + "," + Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[colSGSTAmt.Index].Value)
                                                            + "," + Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[ColIGSTRate.Index].Value)
                                                            + "," + Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[colIGSTAmt.Index].Value)
                                                            + ",'" + Convert.ToString(dataGridView1.Rows[i].Cells[colItemCode.Index].Value) + "')"))

                                                    IsExecute = true;
                                            }
                                            else
                                            {
                                                if (Database.GetExecuteNonQueryCommand("insert into DomesticInvItem(InvNo,NoofPkg,ItemDesc,Qty,Rate,Unit,Amount,NetWt,GrossWt,SalesOrderNo,LoginName,Commodity,SrNo,GroupCommodity,NonDisplayUnit,NonDisplayQty,QtyRemarks,Yr,Companyname,CommodityRemarks,Desc1,Desc2,BagsMRP,size,weekno,LotNo,Colour,Meter,GPM,GSM,WBWeight,ReqWtperBag,ReqWtTotal,DispatchWtperBagGr,WtTolperBagGr,WtExtrapershortKg,WtDiffbetPLWB,AssesableValue,OrderNO,itemDeptt,HSNCode,CGSTrate,CGSTamt,SGSTRate,SGSTAmt,IGSTRate,IGSTAmt,ItemCode) values('"
                                                    + textInvNo.Text + "','" + dataGridView1.Rows[i].Cells[DNoofPkg.Index].Value
                                                    + "','" + dataGridView1.Rows[i].Cells[DItemDesc.Index].Value
                                                    + "','" + dataGridView1.Rows[i].Cells[DOrderQty.Index].Value
                                                    + "','" + dataGridView1.Rows[i].Cells[DUnitPrice.Index].Value
                                                    + "','" + dataGridView1.Rows[i].Cells[DUOM.Index].Value
                                                    + "','" + dataGridView1.Rows[i].Cells[DTotal.Index].Value
                                                    + "','" + dataGridView1.Rows[i].Cells[DNetWt.Index].Value
                                                    + "','" + dataGridView1.Rows[i].Cells[DGrossWt.Index].Value + "','"
                                                    + "','" + FrmMain.UserName
                                                    + "','" + dataGridView1.Rows[i].Cells[DCommodity.Index].Value.ToString()
                                                    + "','" + Convert.ToString((i + 1))
                                                    + "','" + GroupName
                                                    + "','" + dataGridView1.Rows[i].Cells[DNonDisplayUnit.Index].Value.ToString()
                                                    + "','" + NonDisplayQty.ToString()
                                                    + "','" + dataGridView1.Rows[i].Cells[DQtyRemarks.Index].Value.ToString()
                                                    + "','" + textSuffix.Text
                                                    + "','" + comboCompanyName.Text
                                                    + "','" + dataGridView1.Rows[i].Cells[DCommodityRemarks.Index].Value.ToString()
                                                    + "',null,null,null,null,null,null,null,null,null,null,null,"
                                                    + "null,null,null,null,null,null,null" + ",'','" + dataGridView1.Rows[i].Cells[DtDeptt.Index].Value.ToString()
                                                    + "','" + dataGridView1.Rows[i].Cells[colHSNCode.Index].Value.ToString()
                                                    + "'," + Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[colCGSTRate.Index].Value)
                                                    + "," + Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[ColCGSTAmt.Index].Value)
                                                    + "," + Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[ColSGSTRate.Index].Value)
                                                    + "," + Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[colSGSTAmt.Index].Value)
                                                    + "," + Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[ColIGSTRate.Index].Value)
                                                    + "," + Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[colIGSTAmt.Index].Value)
                                                    + ",'" + Convert.ToString(dataGridView1.Rows[i].Cells[colItemCode.Index].Value) + "')"))
                                                    IsExecute = true;
                                            }
                                        }

                                    }//end of forlop grid
                                    Database.CommitTransaction();

                                    if (IsExecute)
                                        MessageBox.Show("Domestic Invoice No " + textInvNo.Text + " is succesfully updated");

                                    Utility.UserInformation("Update", "Domestic Invoice " + textInvNo.Text + " is updated successfully");

                                    if (DespatchCommonFunction.AllowMappingPackinglist(comboCompanyName.Text) > 0)
                                    {
                                        if (Database.OpenConnection(Utility.DespacthConnectionString))
                                        {
                                            string strgstInv = Convert.ToString(Database.GetExecuteNonQueryCommand_retrun("SELECT GSTInvNo FROM Vw_DomesticInvoice WHERE InvNo=" +
                                            textInvNo.Text + " and companyname='" + comboCompanyName.Text + "' and ARESuffix='" + textSuffix.Text + "'"));

                                            // INVOICE-RULE: plus old packing list back, then minus the list now on the invoice.
                                            InvoiceRule_AdjustDespatchGodown(comboCompanyName.Text, desPackinglistBeforeUpdate, false);
                                            Database.GetExecuteNonQueryCommand("DELETE FROM MappPackingList WHERE CompanyName='" + comboCompanyName.Text + "' AND InvNo='" +
                                                strgstInv + "' AND YR='" + textSuffix.Text + "'");

                                            Database.GetExecuteNonQueryCommand("INSERT INTO MappPackingList (CompanyName,InvNo,Yr,PackingListNo) VALUES('" +
                                            comboCompanyName.Text + "','" + strgstInv + "','" + textSuffix.Text + "','" + desPackinglist + "')");
                                            InvoiceRule_AdjustDespatchGodown(comboCompanyName.Text, desPackinglist, true);
                                        }
                                    }
                                }
                                //}
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Database.RollBackTransaction();
                        ErrorMessageBox.Show(ex);
                    }
                }
            }
        }

        private void comboType_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (dateTimePickerInvDate.Value > Convert.ToDateTime("2017-07-01"))
                return;

            if (comboType.SelectedIndex == 0) //VAT
            {
                labelAddVat.Visible = true;
                textAddVat.Visible = true;
                labelVATCST.Text = " VAT %";
                textVat.Text = "4";
                textAddVat.Text = "1";

                if (textRemarks.Text.Contains("SALES AGAINST C FORM"))
                {
                    textRemarks.Text = textRemarks.Text.Replace(" SALES AGAINST C FORM", "");
                }
            }
            else if (comboType.SelectedIndex == 1) //CST
            {
                DialogResult dr = MessageBox.Show("Do you want Sales Against 'C' form", "Against C Form", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                if (dr.CompareTo(DialogResult.Yes) == 0)
                {
                    if (!textRemarks.Text.Contains(" SALES AGAINST C FORM"))
                        textRemarks.Text += " SALES AGAINST C FORM";
                }
                labelAddVat.Visible = false;
                textAddVat.Visible = false;
                labelVATCST.Text = " CST %";
                textVat.Text = "2";
                textAddVat.Text = "0";
            }
        }

        private void comboCompanyName_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                textConsignee.Text = "";
                comboState.Text = "";
                comboTypeInvoice.Text = "";
                checkedListBox1.Items.Clear();
                dataGridView1.Rows.Clear();
                if (Database.OpenConnection(Utility.DespacthConnectionString))
                {
                    string state = "";
                    Database.myreader = Database.GetExecuteReaderCommand("select consignee,terms,Destination,typeofInvoice,Companycode,STATE from companymaster where "
                                  + " companyname = '" + comboBuyerName.Text + "'");
                    if (Database.myreader.Read())
                    {
                        textConsignee.Text = Database.myreader[0].ToString();
                        textTermsofPayment.Text = Database.myreader[1].ToString();
                        comboDestination.Text = Database.myreader[2].ToString();
                        // comboTypeInvoice.Text = Database.myreader[3].ToString();
                        CompanyCode = Database.myreader[4].ToString();
                        state = Convert.ToString(Database.myreader["State"]);
                    }
                    Database.myreader.Close();

                    Database.myreader = Database.GetExecuteReaderCommand("select MarketingInvno from MarketingInvoice with(NOLOCK) where buyerName = '"
                    + comboBuyerName.Text + "' and MarketingInvoice.companyname = '" + comboCompanyName.Text + "' and   MarketingInvno not in " +
                    "  (select orderno from freezeMarketingOrder)" +
                    " union all" +
                    "    select WorkOrderNumber from MultiLayerWorkOrder with(NOLOCK)  where billingname = '"
                      + comboBuyerName.Text + "'  and   WorkOrderNumber not in " +
                    "  (select orderno from freezeMarketingOrder)   " +
                    " union all" +
                    "    select WorkOrderNo from vw_MultiLayerLinerItem with(NOLOCK) where Party = '"
                      + comboBuyerName.Text + "' and   WorkOrderNo not in " +
                    "  (select orderno from freezeMarketingOrder)  union all" +
                    "    select WorkOrderNumber from BOppWorkOrder with(NOLOCK) where billingname = '"
                      + comboBuyerName.Text + "' and   WorkOrderNumber not in " +
                    "  (select orderno from freezeMarketingOrder)");
                    if (Database.myreader.Read())
                    {
                        DialogResult drs = MessageBox.Show("Do you want to generate Invoice No against Marketing Order No " + comboCompanyName.Text.ToUpper()
                                      + Environment.NewLine + " Do you want to generate  Invoice for " + comboBuyerName.Text.ToUpper()
                                      + Environment.NewLine, "Generate Domestic Invoice Against Order No"
                                                  , MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                        if (drs.CompareTo(DialogResult.Yes) == 0)
                        {
                            isagstMarketing = true;
                        }

                    }
                    Database.myreader.Close();

                    if (isagstMarketing)
                    {

                        string strsql = "select distinct(OrderNo) from  " +
                                             "(select vw_Marketinginvoice.MarketingInvno'OrderNo',ItemNo,  " +
                                             " vw_Marketinginvoice.Qty - isnull(sum(Vw_DomesticInvoice.qty),0)'Qty',   " +
                                             " vw_Marketinginvoice.companyname,vw_Marketinginvoice.BuyerName  from vw_Marketinginvoice  with (nolock) " +
                                             " left join Vw_DomesticInvoice    " +
                                             " on vw_Marketinginvoice.ItemNo = orderno and (Vw_DomesticInvoice.iscancel = '' or vw_domesticinvoice.iscancel is NULL)  " +
                                             " where vw_Marketinginvoice.MarketingInvno not in (select orderno from freezeMarketingOrder)  and " +
                                             " vw_Marketinginvoice.buyername = '" + comboBuyerName.Text
                                             + "'  and vw_Marketinginvoice.companyname='" + comboCompanyName.Text + "'" +
                                             " group by vw_Marketinginvoice.MarketingInvno ,vw_Marketinginvoice.ItemNO ,   " +
                                             " vw_Marketinginvoice.companyname,vw_Marketinginvoice.BuyerName ,vw_Marketinginvoice.Qty  " +
                                             " having vw_Marketinginvoice.Qty - isnull(sum(Vw_DomesticInvoice.qty),0) > 0 )as tmp   " +
                                             " order by OrderNo";

                        Database.myreader = Database.GetExecuteReaderCommand(strsql);
                        while (Database.myreader.Read())
                            checkedListBox1.Items.Add(Database.myreader[0].ToString());
                        Database.myreader.Close();

                        strsql = "";
                        strsql = "select workordernumber from  " +
                                                " (select workordernumber,  " +
                                                " multilayerworkorder.Qty - isnull(sum(Vw_DomesticInvoice.qty),0)'Qty'  " +
                                                " ,multilayerworkorder.billingname from multilayerworkorder  with(NOLOCK) " +
                                                " Left join Vw_DomesticInvoice   with (nolock)  " +
                                                " on workordernumber = orderno  " +
                                                " where workordernumber not in (select orderno from freezeMarketingOrder) and billingname = '"
                                                + comboBuyerName.Text + "' and factoryinfo = '" + comboCompanyName.Text + "' and (Vw_DomesticInvoice.iscancel = '' or vw_domesticinvoice.iscancel is NULL)" +
                                                " group by workordernumber,multilayerworkorder.billingname,multilayerworkorder.Qty  " +
                                                " having ((0.05 * multilayerworkorder.Qty) + multilayerworkorder.Qty) >= isnull(sum(Vw_DomesticInvoice.qty),0)  ) as tmp   " +
                                                " order by workordernumber";

                        Database.myreader = Database.GetExecuteReaderCommand(strsql);
                        while (Database.myreader.Read())
                            checkedListBox1.Items.Add(Database.myreader[0].ToString());
                        Database.myreader.Close();
                        if (comboCompanyName.Text.Trim() == "Plastene India Limited")
                        {
                            strsql = "";
                            strsql = "select workorderno from  " +
                                        " (select workorderno,  " +
                                        " vw_MultiLayerLinerItem.TotalNetWt - isnull(sum(Vw_DomesticInvoice.qty),0) 'Qty'  " +
                                        " ,vw_MultiLayerLinerItem.Party from vw_MultiLayerLinerItem  with (nolock) " +
                                        " Left join Vw_DomesticInvoice   " +
                                        " on workorderno = orderno  and " +
                                        " vw_MultiLayerLinerItem.commodity = Vw_DomesticInvoice.commodity and " +
                                        " vw_MultiLayerLinerItem.Particular = Vw_DomesticInvoice.ItemDesc" +
                                        " where workorderno not in (select orderno from freezeMarketingOrder) and Party = '"
                                        + comboBuyerName.Text + "' and (Vw_DomesticInvoice.iscancel = '' or vw_domesticinvoice.iscancel is NULL)" +
                                        " group by workorderno,vw_MultiLayerLinerItem.Party,vw_MultiLayerLinerItem.TotalNetWt  " +
                                        " having ((0.05 * vw_MultiLayerLinerItem.TotalNetWt) + vw_MultiLayerLinerItem.TotalNetWt)    >= isnull(sum(Vw_DomesticInvoice.qty),0)  ) as tmp   " +
                                        " order by workorderno";

                            Database.myreader = Database.GetExecuteReaderCommand(strsql);
                            while (Database.myreader.Read())
                                checkedListBox1.Items.Add(Database.myreader[0].ToString());
                            Database.myreader.Close();

                            strsql = "";
                            strsql = "select workordernumber from  " +
                                               " (select workordernumber,  " +
                                               " BOPPWorkOrder.Qty - isnull(sum(Vw_DomesticInvoice.qty),0)'Qty'  " +
                                               " ,BOPPWorkOrder.billingname from BOPPWorkOrder with(NOLOCK) " +
                                               " Left join Vw_DomesticInvoice  with (nolock) " +
                                               " on workordernumber = orderno  " +
                                               " where workordernumber not in (select orderno from freezeMarketingOrder) and billingname = '"
                                               + comboBuyerName.Text + "' and (Vw_DomesticInvoice.iscancel = '' or vw_domesticinvoice.iscancel is NULL)" +
                                               " group by workordernumber,BOPPWorkOrder.billingname,BOPPWorkOrder.Qty  " +
                                               " having ((0.05 * BOPPWorkOrder.Qty) + BOPPWorkOrder.Qty) >= isnull(sum(Vw_DomesticInvoice.qty),0)  ) as tmp   " +
                                               " order by workordernumber";
                            Database.myreader = Database.GetExecuteReaderCommand(strsql);
                            while (Database.myreader.Read())
                                checkedListBox1.Items.Add(Database.myreader[0].ToString());
                            Database.myreader.Close();
                        }
                    }

                    comboConsigneeName.Items.Clear();
                    comboConsigneeName.Text = "";

                    Database.myreader = Database.GetExecuteReaderCommand("select Distinct(Consigneename) from consignee where  companyCode = '"
                        + CompanyCode + "'");
                    while (Database.myreader.Read())
                        comboConsigneeName.Items.Add(Database.myreader[0].ToString());
                    Database.myreader.Close();

                    Database.Closeconnection();

                    TypeInvoice();

                    if (comboConsigneeName.Items.Count > 0)
                        comboConsigneeName.SelectedIndex = 0;
                    comboState.Text = state;
                }
            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
            }

        }

        private void dataGrid1_CurrentCellChanged(object sender, EventArgs e)
        {

            double total = 0;
            try
            {
                if (textExDuty.Text == "")
                {
                    textExDuty.Text = "0";
                    textEduCess.Text = "0";
                    textSecHiEdu.Text = "0";
                    if (textVat.Text == "")
                        textVat.Text = "0";
                    if (textAddVat.Text == "")
                        textAddVat.Text = "0";
                }

                if (dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[DNoofPkg.Index].FormattedValue.ToString().Trim() != ""
                     && dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[DCommodity.Index].FormattedValue.ToString().Trim() != ""
                     && dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[DUOM.Index].FormattedValue.ToString().Trim() != ""
                     && dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[DOrderQty.Index].FormattedValue.ToString().Trim() != "" &&
                     dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[DUnitPrice.Index].FormattedValue.ToString().Trim() != "")
                {
                    dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[DTotal.Index].Value =
                        Convert.ToDouble(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[DOrderQty.Index].Value.ToString()) *
                        Convert.ToDouble(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[DUnitPrice.Index].Value.ToString());
                    total += Convert.ToDouble(dataGridView1[DTotal.Index, dataGridView1.CurrentCell.RowIndex]);

                }



            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
            }
        }

        private void checkInsurance_CheckedChanged(object sender, EventArgs e)
        {
            if (checkInsurance.Checked == true)
            {
                groupInsurance.Visible = true;
                if (Database.OpenConnection(Utility.DespacthConnectionString))
                {
                    Database.myreader = Database.GetExecuteReaderCommand("Select top 1 policyno from InsurancePolicy where companyname = '"
                        + comboCompanyName.Text + "' order by srno desc");
                    if (Database.myreader.Read())
                        textInsurance.Text = Database.myreader[0].ToString();
                    Database.myreader.Close();
                    //  Database.Closeconnection();
                }
            }
            else
            {
                groupInsurance.Visible = false;
                textInsurance.Text = "";
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            comboBuyerName.Items.Clear();
            comboBuyerName.Text = "";
            comboTranporterName.Items.Clear();
            comboTranporterName.Text = "";

            if (Database.OpenConnection(Utility.DespacthConnectionString))
            {
                Database.myreader = Database.GetExecuteReaderCommand("Select * from CompanyMaster order by CompanyName");
                while (Database.myreader.Read())
                    comboBuyerName.Items.Add(Database.myreader[0].ToString());
                Database.myreader.Close();

                Database.myreader = Database.GetExecuteReaderCommand("select Compname from transportmaster order by compname");
                while (Database.myreader.Read())
                    comboTranporterName.Items.Add(Database.myreader[0].ToString());
                Database.myreader.Close();


                Database.Closeconnection();
            }
            //if (comboBuyerName.Items.Count > 0)
            //    comboBuyerName.SelectedIndex = 0;

        }

        private void textInvNo_MouseDown(object sender, MouseEventArgs e)
        {

        }

        private void checkAvailBenefit_CheckedChanged(object sender, EventArgs e)
        {
            groupAvailBenefit.Visible = false;
            if (checkAvailBenefit.Checked)
                groupAvailBenefit.Visible = true;
            else
                groupAvailBenefit.Visible = false;

        }

        private void comboCurrency_SelectedIndexChanged(object sender, EventArgs e)
        {
            comboCurrencyValue.Items.Clear();
            comboCurrencyValue.Text = "";
            if (Database.OpenConnection(Utility.DespacthConnectionString))
            {
                Database.myreader = Database.GetExecuteReaderCommand("select currencyvalue from currencytype where currency = '" + comboCurrency.Text + "'");
                if (Database.myreader.Read())
                {
                    comboCurrencyValue.Items.Add(Database.myreader[0].ToString());
                    comboCurrencyValue.SelectedIndex = 0;
                }
                Database.myreader.Close();
                Database.Closeconnection();
            }
        }


        private void comboConsigneeName_SelectedIndexChanged_1(object sender, EventArgs e)
        {
            try
            {
                if (Database.OpenConnection(Utility.DespacthConnectionString))
                {
                    comboConsigneeCity.Items.Clear();
                    comboConsigneeCity.Text = "";

                    Database.myreader = Database.GetExecuteReaderCommand("select City from Consignee where consigneename = '"
                       + comboConsigneeName.Text + "' and companycode ='" + CompanyCode + "'");
                    while (Database.myreader.Read())
                        comboConsigneeCity.Items.Add(Database.myreader[0].ToString());
                    Database.myreader.Close();


                    if (comboConsigneeCity.Items.Count > 0)
                        comboConsigneeCity.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
            }
        }

        private void comboConsigneeCity_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (Database.OpenConnection(Utility.DespacthConnectionString))
                {
                    Database.myreader = Database.GetExecuteReaderCommand("select ConsigneeAdd from Consignee where consigneename = '"
                       + comboConsigneeName.Text + "' and companycode ='" + CompanyCode + "' and city = '" + comboConsigneeCity.Text + "'");
                    if (Database.myreader.Read())
                        textConsignee.Text = Database.myreader[0].ToString();
                    Database.myreader.Close();


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
                if (dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[DOrderQty.Index].FormattedValue.ToString().Trim() != ""
                     && dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[DUnitPrice.Index].FormattedValue.ToString().Trim() != "")
                {
                    dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[DTotal.Index].Value =
                        Convert.ToDouble(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[DUnitPrice.Index].Value.ToString()) *
                        Convert.ToDouble(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[DOrderQty.Index].Value.ToString());
                }

            }
            catch (Exception ex)
            {
                ex.ToString();
            }
        }

        private void dataGridView1_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            try
            {
                if (e.Control is TextBox)
                {
                    TextBox tb = e.Control as TextBox;
                    tb.KeyPress -= new KeyPressEventHandler(tb_KeyPress);
                    if (this.dataGridView1.CurrentCell.ColumnIndex == DOrderQty.Index
                        || this.dataGridView1.CurrentCell.ColumnIndex == DNetWt.Index
                        || this.dataGridView1.CurrentCell.ColumnIndex == DUnitPrice.Index
                        || this.dataGridView1.CurrentCell.ColumnIndex == DTotal.Index)
                        tb.KeyPress += new KeyPressEventHandler(tb_KeyPress);
                }

                if (dataGridView1.CurrentCell.ColumnIndex == DCommodity.Index) // For Commodity ComboBox
                {
                    comboBox = e.Control as ComboBox;
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

        void tb_KeyPress(object sender, KeyPressEventArgs e)
        {

            if ((!(Char.IsDigit(e.KeyChar))) && (!(e.KeyChar == '\b')) && (!(e.KeyChar == '.')))
                e.Handled = true;
        }


        void comboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {

                if (dataGridView1.CurrentCell.ColumnIndex == DCommodity.Index)
                {
                    textTCS.Text = "0";
                    ComboBox cb = ((ComboBox)sender);
                    Database.OpenConnection(Utility.DespacthConnectionString);
                    Database.myreader = Database.GetExecuteReaderCommand("select THeading,Avail39a from commodity where CommodityName ='"
                                      + cb.Text + "' and companyname = '" + comboCompanyName.Text + "'");
                    while (Database.myreader.Read())
                    {
                        textHeading.Text = Database.myreader[0].ToString();
                        if (Database.myreader[1].ToString() == "yes")
                            checkAvailBenefit.Checked = true;
                        else
                            checkAvailBenefit.Checked = false;
                    }
                    Database.myreader.Close();
                    #region check TCS Calculation 28.09.2020
                    textTCS.Text = DespatchCommonFunction.LoadTCSLedger(comboCompanyName.Text, comboBuyerName.Text, cb.Text, Convert.ToDateTime(dateTimePickerDespatchDate.Value)).ToString(); ;
                    //Database.myreader = Database.GetExecuteReaderCommand("select TotalTCS  from tcsfill where Commodity ='"
                    //                  + cb.Text + "' and companyname = '" + comboCompanyName.Text + "' and (('"
                    //                  + dateTimePickerDespatchDate.Value + "' between fromdate and todate) or ('"
                    //                  + dateTimePickerDespatchDate.Value + "' >= fromdate and todate is null))");
                    //while (Database.myreader.Read())
                    //    textTCS.Text = Database.myreader[0].ToString();
                    //Database.myreader.Close();
                    #endregion
                    Database.Closeconnection();
                }



            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
            }
        }


        private void checkedListBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                dataGridView1.Rows.Clear();
                textFrieght.Text = "0";
                if (Database.OpenConnection(Utility.DespacthConnectionString))
                {
                    string BuyerOrderNo = "";
                    string consigneecode = "";
                    ArrayList ArrOrderNo = new ArrayList();
                    ArrOrderNo.Clear();
                    bool isfound = false;
                    for (int i = 0; i < checkedListBox1.CheckedItems.Count; i++)
                        ArrOrderNo.Add(checkedListBox1.CheckedItems[i].ToString());
                    int k = 0;
                    double freight = 0;

                    for (int j = 0; j < ArrOrderNo.Count; j++)
                    {

                        Database.myreader = Database.GetExecuteReaderCommand("select * from MarketingInvoice with (NOLOCK) where MarketingInvNo = '"
                        + ArrOrderNo[j].ToString() + "'");
                        if (Database.myreader.Read())
                        {
                            if (j > 0)
                            {
                                BuyerOrderNo = BuyerOrderNo + "," + Database.myreader["BuyerOrderNo"].ToString();

                            }
                            else
                            {
                                BuyerOrderNo = Database.myreader["BuyerOrderNo"].ToString();

                            }

                            textTermsOfDelivery.Text = Database.myreader[58].ToString();
                            textTermsofPayment.Text = Database.myreader[3].ToString();
                            dateTimeBuyerOrderDate.Value = Convert.ToDateTime(Database.myreader[5].ToString());
                            comboDeptt.Text = Database.myreader[34].ToString();
                            textExDuty.Text = Database.myreader[8].ToString();
                            textEduCess.Text = Database.myreader[9].ToString();
                            textSecHiEdu.Text = Database.myreader[10].ToString();
                            if (Database.myreader["typevat"].ToString() != "")
                            {
                                comboType.Text = Database.myreader["typevat"].ToString();
                                textVat.Text = Database.myreader[11].ToString();
                                textAddVat.Text = Database.myreader[12].ToString();

                            }

                            textTCS.Text = Database.myreader["TCSPer"].ToString();
                            textRemarks.Text = Database.myreader["Remarks"].ToString();
                            textFreightRemarks.Text = Database.myreader["FreightREmarks"].ToString();
                            consigneecode = Database.myreader["consigneecode"].ToString();
                            textSpDutyRemarks1.Text = Database.myreader[46].ToString();
                            textSpDutyRemark2.Text = Database.myreader[48].ToString();
                            textSpDutyRemarks3.Text = Database.myreader[50].ToString();
                            textSpDuty1Amt.Text = Database.myreader[47].ToString();
                            textSpDuty2Amt.Text = Database.myreader[49].ToString();
                            textSpDuty3Amt.Text = Database.myreader[51].ToString();
                            comboExicseApp.Text = Database.myreader[28].ToString();
                            comboVatApp.Text = Database.myreader[29].ToString();
                            textVatNilRmarks.Text = Database.myreader[63].ToString();
                            textExciseNIlRemarks.Text = Database.myreader[62].ToString();
                            if (Database.myreader[61].ToString() == "yes")
                                checkFreight.Checked = true;
                            else
                                checkFreight.Checked = false;

                            isfound = true;
                        }
                        Database.myreader.Close();


                        double itemfreight = 0;
                        if (isfound)
                        {
                            Database.myreader = Database.GetExecuteReaderCommand("select NoofPkg,Commodity,ItemDesc,Unit,QTY,Rate,ItemDeptt,FreightperUnit,ItemNo,Itemcode from  " +
                                           "(select vw_Marketinginvoice.MarketingInvno'OrderNo',  " +
                                           " vw_Marketinginvoice.Qty - isnull(sum(Vw_DomesticInvoice.qty),0)'Qty',   " +
                                           " vw_Marketinginvoice.companyname,vw_Marketinginvoice.BuyerName ,  " +
                                            " vw_Marketinginvoice.NoofPKG,vw_Marketinginvoice.ItemDesc,vw_Marketinginvoice.Rate,  " +
                                            " vw_Marketinginvoice.Unit,vw_Marketinginvoice.COmmodity,vw_Marketinginvoice.ItemDeptt,vw_Marketinginvoice.FreightperUnit,ItemNo,MktItemCode as Itemcode  from vw_Marketinginvoice  " +
                                           " left join Vw_DomesticInvoice    " +
                                           " on vw_Marketinginvoice.ItemNO = orderno  and (Vw_DomesticInvoice.iscancel = '' or vw_domesticinvoice.iscancel is NULL)    " +
                                           " where vw_Marketinginvoice.MarketingInvno not in (select orderno from freezeMarketingOrder)  and " +
                                           " vw_Marketinginvoice.buyername = '" + comboBuyerName.Text + "'" +
                                           " group by vw_Marketinginvoice.MarketingInvno ,vw_Marketinginvoice.ItemDesc ,   " +
                                           " vw_Marketinginvoice.companyname,vw_Marketinginvoice.BuyerName ,  " +
                                            " vw_Marketinginvoice.NoofPKG,vw_Marketinginvoice.ItemDesc,vw_Marketinginvoice.Rate,  " +
                                            " vw_Marketinginvoice.Unit,vw_Marketinginvoice.COmmodity, " +
                                            " vw_Marketinginvoice.ItemDeptt,vw_Marketinginvoice.FreightperUnit,ItemNo    " +
                                           " ,vw_Marketinginvoice.Qty,MktItemCode having vw_Marketinginvoice.Qty - isnull(sum(Vw_DomesticInvoice.qty),0) >0 )as tmp   " +
                                           " where orderNo = '"
                           + ArrOrderNo[j].ToString() + "'");
                            // Database.myreader = Database.GetExecuteReaderCommand("select * from MarketingInvitem where InvNo = '"
                            //+ ArrOrderNo[j].ToString() + "'");
                            while (Database.myreader.Read())
                            {
                                dataGridView1.Rows.Add();
                                dataGridView1[DNoofPkg.Index, k].Value = Database.myreader[0].ToString();
                                if (!DCommodity.Items.Contains(Database.myreader[1].ToString()))
                                {
                                    DataTable dt = (DataTable)DCommodity.DataSource;
                                    dt.Rows.Add(Database.myreader[1].ToString());
                                    DCommodity.DataSource = dt.DefaultView.ToTable(true, "CommodityName");

                                }
                                dataGridView1[DCommodity.Index, k].Value = Database.myreader[1].ToString();

                                dataGridView1[DItemDesc.Index, k].Value = Database.myreader[2].ToString();
                                dataGridView1[DUOM.Index, k].Value = Database.myreader[3].ToString();
                                dataGridView1[DOrderQty.Index, k].Value = Database.myreader[4].ToString();
                                dataGridView1[DUnitPrice.Index, k].Value = Database.myreader[5].ToString();
                                dataGridView1[DtDeptt.Index, k].Value = Database.myreader[6].ToString();
                                dataGridView1[OrderNo.Index, k].Value = Database.myreader[8].ToString();

                                string grp = "";
                                string subgrp = "";
                                DespatchCommonFunction.GetItemGropuName(Database.myreader["ItemCode"].ToString(), comboCompanyName.Text.ToString(), ref grp, ref subgrp);
                                dataGridView1.Rows[k].Cells[colGroupName.Index].Value = grp;
                                dataGridView1.Rows[k].Cells[colSubGrp.Index].Value = subgrp;
                                dataGridView1.Rows[k].Cells[colMainGrp.Index].Value = DespatchCommonFunction.GetDeptt(Database.myreader["ItemCode"].ToString());
                                dataGridView1.Rows[k].Cells[colItemCode.Index].Value = Convert.ToString(Database.myreader["ItemCode"]);

                                dataGridView1[colItemCode.Index, k].Value = Convert.ToString(Database.myreader["Itemcode"].ToString());

                                freight = freight + Convert.ToDouble(Database.myreader[7].ToString()) * Convert.ToDouble(Database.myreader[4].ToString());
                                k++;
                            }
                            Database.myreader.Close();
                        }
                        if (comboCompanyName.Text == "Plastene India Limited " || comboCompanyName.Text == "Oswal Extrusion Limited -(Varsana Unit)")
                        {
                            if (!isfound)
                            {

                                Database.myreader = Database.GetExecuteReaderCommand("select * from MultiLayerworkOrder with (NOLOCK) where WorkOrderNumber = '"
                                + ArrOrderNo[j].ToString() + "'");
                                if (Database.myreader.Read())
                                {
                                    if (j > 0)
                                        BuyerOrderNo = BuyerOrderNo + "," + Database.myreader["BuyerOrderNo"].ToString();
                                    else
                                        BuyerOrderNo = Database.myreader["BuyerOrderNo"].ToString();

                                    textTermsOfDelivery.Text = Database.myreader[106].ToString();
                                    textTermsofPayment.Text = Database.myreader[116].ToString();
                                    comboDeptt.Text = "Multilayer";
                                    consigneecode = Database.myreader["consigneecode"].ToString();
                                    dateTimeBuyerOrderDate.Value = Convert.ToDateTime(Database.myreader[108].ToString());
                                    itemfreight = Convert.ToDouble(Database.myreader["frieght"].ToString());
                                    textFreightRemarks.Text = Database.myreader["FrightREmarks"].ToString();
                                    textExDuty.Text = Database.myreader[35].ToString();
                                    textEduCess.Text = Database.myreader[36].ToString();
                                    textSecHiEdu.Text = Database.myreader[37].ToString();
                                    comboExicseApp.Text = Database.myreader[112].ToString();
                                    comboVatApp.Text = Database.myreader[114].ToString();
                                    textVatNilRmarks.Text = Database.myreader[15].ToString();
                                    textExciseNIlRemarks.Text = Database.myreader[113].ToString();
                                    if (Database.myreader["taxtype"].ToString() != "")
                                    {
                                        comboType.Text = Database.myreader["taxtype"].ToString();
                                        textVat.Text = Database.myreader[39].ToString();
                                        textAddVat.Text = Database.myreader[40].ToString();

                                    }
                                }
                                Database.myreader.Close();



                                Database.myreader = Database.GetExecuteReaderCommand("select Commodity,QtyUnit,Qty,Basic,JobName from  " +
                                                " (select workordernumber,  " +
                                                " multilayerworkorder.Qty - isnull(sum(domesticinvitem.qty),0)'Qty'  " +
                                                " ,multilayerworkorder.billingname ,multilayerworkorder.commodity,  " +
                                                " multilayerworkorder.QtyUnit,multilayerworkorder.Basic,JobName from multilayerworkorder  with (NOLOCK) " +
                                                " Left join domesticinvitem   with (NOLOCK)  " +
                                                " on workordernumber = orderno  " +
                                                " where workordernumber not in (select orderno from freezeMarketingOrder) " +
                                                " group by workordernumber,multilayerworkorder.billingname,multilayerworkorder.commodity,  " +
                                                " multilayerworkorder.QtyUnit,multilayerworkorder.Basic,JobName  " +
                                                " ,multilayerworkorder.Qty )as tmp   " +
                                                " where workordernumber = '" + ArrOrderNo[j].ToString() + "'");
                                // Database.myreader = Database.GetExecuteReaderCommand("select * from MultiLayerworkOrder where WorkOrderNumber = '"
                                //+ ArrOrderNo[j].ToString() + "'");
                                if (Database.myreader.Read())
                                {
                                    dataGridView1.Rows.Add();
                                    dataGridView1[DCommodity.Index, k].Value = Database.myreader[0].ToString();
                                    dataGridView1[DtDeptt.Index, k].Value = "Multilayer";
                                    dataGridView1[DUOM.Index, k].Value = Database.myreader[1].ToString();
                                    dataGridView1[DOrderQty.Index, k].Value = Database.myreader[2].ToString();
                                    dataGridView1[DUnitPrice.Index, k].Value = Database.myreader[3].ToString();
                                    dataGridView1[DItemDesc.Index, k].Value = Database.myreader[4].ToString();
                                    dataGridView1[OrderNo.Index, k].Value = ArrOrderNo[j].ToString();
                                    freight += itemfreight * Convert.ToDouble(dataGridView1[DOrderQty.Index, k].Value);
                                    k++;
                                }
                                Database.myreader.Close();

                                Database.myreader = Database.GetExecuteReaderCommand("select commodity,Rate,totalnetWt,Particular from Multilayerlineritem  with (NOLOCK) where  workorderno = '" + ArrOrderNo[j].ToString() + "'");
                                while (Database.myreader.Read())
                                {
                                    dataGridView1.Rows.Add();
                                    dataGridView1[DCommodity.Index, k].Value = Database.myreader[0].ToString();
                                    dataGridView1[DUnitPrice.Index, k].Value = Database.myreader[1].ToString();
                                    dataGridView1[DUOM.Index, k].Value = "KGS";
                                    dataGridView1[DOrderQty.Index, k].Value = Database.myreader[2].ToString();
                                    dataGridView1[DtDeptt.Index, k].Value = "Multilayer";
                                    dataGridView1[DItemDesc.Index, k].Value = Database.myreader[3].ToString();
                                    k++;
                                }
                                Database.myreader.Close();

                                Database.myreader = Database.GetExecuteReaderCommand("select * from BOPPworkOrder  with (NOLOCK) where WorkOrderNumber = '"
                            + ArrOrderNo[j].ToString() + "'");
                                if (Database.myreader.Read())
                                {
                                    if (j > 0)
                                        BuyerOrderNo = BuyerOrderNo + "," + Database.myreader["BuyerOrderNo"].ToString();
                                    else
                                        BuyerOrderNo = Database.myreader["BuyerOrderNo"].ToString();

                                    textTermsOfDelivery.Text = Database.myreader["deliveryTerms"].ToString();
                                    textTermsofPayment.Text = Database.myreader[73].ToString();
                                    comboDeptt.Text = "BOPP";
                                    dateTimeBuyerOrderDate.Value = Convert.ToDateTime(Database.myreader["BuyerOrderDate"].ToString());
                                    itemfreight = Convert.ToDouble(Database.myreader["frieght"].ToString());
                                    textFreightRemarks.Text = Database.myreader["FrightREmarks"].ToString();
                                    textExDuty.Text = Database.myreader[9].ToString();
                                    textEduCess.Text = Database.myreader[10].ToString();
                                    textSecHiEdu.Text = Database.myreader[11].ToString();
                                    if (Database.myreader["taxtype"].ToString() != "")
                                    {
                                        comboType.Text = Database.myreader["taxtype"].ToString();
                                        textVat.Text = Database.myreader[13].ToString();
                                        textAddVat.Text = Database.myreader[14].ToString();

                                    }
                                }
                                Database.myreader.Close();
                                Database.myreader = Database.GetExecuteReaderCommand("select Commodity,QtyUnit,Qty,Basic,JobName from  " +
                                                " (select workordernumber,  " +
                                                " BoPPworkorder.Qty - isnull(sum(domesticinvitem.qty),0)'Qty'  " +
                                                " ,BoPPworkorder.billingname ,BoPPworkorder.commodity,  " +
                                                " BoPPworkorder.QtyUnit,BoPPworkorder.Basic,JobName from BoPPworkorder  with (NOLOCK)  " +
                                                " Left join domesticinvitem    with (NOLOCK) " +
                                                " on workordernumber = orderno  " +
                                                " where workordernumber not in (select orderno from freezeMarketingOrder) " +
                                                " group by workordernumber,BoPPworkorder.billingname,BoPPworkorder.commodity,  " +
                                                " BoPPworkorder.QtyUnit,BoPPworkorder.Basic,JobName  " +
                                                " ,BoPPworkorder.Qty )as tmp   " +
                                                " where workordernumber = '" + ArrOrderNo[j].ToString() + "'");
                                // Database.myreader = Database.GetExecuteReaderCommand("select * from MultiLayerworkOrder where WorkOrderNumber = '"
                                //+ ArrOrderNo[j].ToString() + "'");
                                if (Database.myreader.Read())
                                {
                                    dataGridView1.Rows.Add();
                                    dataGridView1[DCommodity.Index, k].Value = Database.myreader[0].ToString();
                                    dataGridView1[DtDeptt.Index, k].Value = "BOPP";
                                    dataGridView1[DUOM.Index, k].Value = Database.myreader[1].ToString();
                                    dataGridView1[DOrderQty.Index, k].Value = Database.myreader[2].ToString();
                                    dataGridView1[DUnitPrice.Index, k].Value = Database.myreader[3].ToString();
                                    dataGridView1[DItemDesc.Index, k].Value = Database.myreader[4].ToString();
                                    dataGridView1[OrderNo.Index, k].Value = ArrOrderNo[j].ToString();
                                    freight += itemfreight * Convert.ToDouble(dataGridView1[DOrderQty.Index, k].Value);
                                    k++;
                                }
                                Database.myreader.Close();
                            }
                        }
                    }


                    Database.OpenConnection(Utility.DespacthConnectionString);
                    string consigneename = "";
                    string consigneecity = "";
                    string consigneeadd = "";

                    Database.myreader = Database.GetExecuteReaderCommand("select consigneename,City,ConsigneeAdd from consignee where "
                          + " Consigneecode = '" + consigneecode + "'");
                    if (Database.myreader.Read())
                    {
                        consigneename = Database.myreader[0].ToString();
                        consigneecity = Database.myreader[1].ToString();
                        consigneeadd = Database.myreader[2].ToString();
                    }
                    Database.myreader.Close();
                    Database.Closeconnection();

                    comboConsigneeName.Text = consigneename;
                    comboConsigneeCity.Text = consigneecity;
                    textConsignee.Text = consigneeadd;
                    textFrieght.Text = Convert.ToString(freight);
                    textBuyerOrderNo.Text = BuyerOrderNo;
                    if (dataGridView1.Rows.Count > 1)
                        MatchCommodity_Theding(1);
                }
            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
            }
        }
        void BuyerNameSelection()
        {
            checkedListBox1.Items.Clear();
            dataGridView1.Rows.Clear();
            if (Database.OpenConnection(Utility.DespacthConnectionString))
            {
                Database.myreader = Database.GetExecuteReaderCommand("select MarketingInvno from MarketingInvoice  with (NOLOCK) where buyerName = '"
                   + comboBuyerName.Text + "' and companyname = '" + comboCompanyName.Text + "' order by InvNo");
                while (Database.myreader.Read())
                    checkedListBox1.Items.Add(Database.myreader[0].ToString());
                Database.myreader.Close();

                Database.myreader = Database.GetExecuteReaderCommand("select WorkOrderNumber from MultiLayerWorkOrder  with (NOLOCK) where companyname = '"
                + comboBuyerName.Text + "' order by WOrkOrderNo ");
                while (Database.myreader.Read())
                    checkedListBox1.Items.Add(Database.myreader[0].ToString());
                Database.myreader.Close();

            }
        }

        private void comboExicseApp_SelectedIndexChanged(object sender, EventArgs e)
        {

            if (dateTimePickerInvDate.Value >= Convert.ToDateTime("2017-07-01 00:00:00"))
                return;
            if (comboExicseApp.SelectedIndex == 0)
            {
                groupExciseNil.Visible = true;
                textExciseNIlRemarks.Text = "";
                textExciseNIlRemarks.Text = "Against CT3 Form";
            }
            else
            {
                groupExciseNil.Visible = false;
                textExciseNIlRemarks.Text = "";
            }
        }

        private void comboVatApp_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (dateTimePickerInvDate.Value >= Convert.ToDateTime("2017-07-01 00:00:00"))
                return;
            if (comboVatApp.SelectedIndex == 0)
            {
                groupVATNil.Visible = true;
                textVatNilRmarks.Text = "";
                textVatNilRmarks.Text = "Against H Form";
                textVat.Text = "0";
                textAddVat.Text = "0";
                if (!textRemarks.Text.Contains(textVatNilRmarks.Text))
                    textRemarks.Text += textVatNilRmarks.Text;
            }
            else
            {
                groupVATNil.Visible = false;
                textVatNilRmarks.Text = "";
                if (textRemarks.Text.Contains("AGAINST H FORM"))
                {
                    textRemarks.Text = textRemarks.Text.Replace("AGAINST H FORM", "");
                }
            }
        }

        private void comboState_SelectedIndexChanged(object sender, EventArgs e)
        {
            TypeInvoice();
        }
        void TypeInvoice()
        {
            try
            {
                if (comboState.Text == "Gujarat")
                {
                    string gstno = "";
                    string tinno = "";
                    bool ISAFTINNO = false;
                    if (Database.OpenConnection(Utility.DespacthConnectionString))
                    {
                        Database.myreader = Database.GetExecuteReaderCommand("select Vatno,GSTNo,isaftinno from companymaster where companyname = '" + comboBuyerName.Text + "'");
                        if (Database.myreader.Read())
                        {
                            tinno = Database.myreader[0].ToString();
                            gstno = Database.myreader[1].ToString();
                            if (Database.myreader["isaftinno"].ToString() == "yes")
                                ISAFTINNO = true;
                        }
                        Database.myreader.Close();
                        if (dateTimePickerInvDate.Value < Convert.ToDateTime("2017-07-01"))
                        {
                            if (ISAFTINNO || tinno.Length >= 11 || gstno.Length >= 11)
                                comboTypeInvoice.Text = "Tax Invoice";
                            else
                                comboTypeInvoice.Text = "Retail Invoice";
                        }
                        else
                        {
                            comboTypeInvoice.Text = "Tax Invoice";
                        }

                    }
                }

                else
                {
                    if (dateTimePickerInvDate.Value < Convert.ToDateTime("2017-07-01"))
                        comboTypeInvoice.Text = "Retail Invoice";
                    else
                        comboTypeInvoice.Text = "Tax Invoice";
                }
            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
            }
        }

        private bool OrderQuantity()
        {
            try
            {
                bool ispending = false;
                if (Database.OpenConnection(Utility.DespacthConnectionString))
                {
                    double orderqty = 0;
                    double invqty = 0;
                    bool isorderqty = false;
                    double pending = 0;

                    for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                    {
                        orderqty = 0;
                        invqty = 0;
                        isorderqty = false;
                        ispending = false;
                        pending = 0;
                        Database.myreader = Database.GetExecuteReaderCommand("select qty from vw_Marketinginvoice where itemno = '"
                        + dataGridView1.Rows[i].Cells[OrderNo.Index].FormattedValue.ToString() + "'");
                        if (Database.myreader.Read())
                        {
                            orderqty = Convert.ToDouble(Database.myreader[0].ToString());
                            isorderqty = true;
                        }
                        Database.myreader.Close();

                        if (!isorderqty)
                        {
                            Database.myreader = Database.GetExecuteReaderCommand("select Qty from MultiLayerworkOrder  with (NOLOCK) where WorkOrderNumber = '"
                            + dataGridView1.Rows[i].Cells[OrderNo.Index].FormattedValue.ToString() + "'");
                            if (Database.myreader.Read())
                            {
                                orderqty = Convert.ToDouble(Database.myreader[0].ToString());
                                isorderqty = true;
                            }
                            Database.myreader.Close();
                        }
                        if (!isorderqty)
                        {
                            Database.myreader = Database.GetExecuteReaderCommand("select totalnetwt from Multilayerlineritem  with (NOLOCK) where workorderno = '"
                            + dataGridView1.Rows[i].Cells[OrderNo.Index].FormattedValue.ToString() + "'");
                            if (Database.myreader.Read())
                            {
                                orderqty = Convert.ToDouble(Database.myreader[0].ToString());
                                isorderqty = true;
                            }
                            Database.myreader.Close();
                        }
                        if (!isorderqty)
                        {
                            Database.myreader = Database.GetExecuteReaderCommand("select Qty from BOPPWorkorder  with (NOLOCK) where WorkOrderNumber = '"
                            + dataGridView1.Rows[i].Cells[OrderNo.Index].FormattedValue.ToString() + "'");
                            if (Database.myreader.Read())
                            {
                                orderqty = Convert.ToDouble(Database.myreader[0].ToString());
                                isorderqty = true;
                            }
                            Database.myreader.Close();
                        }
                        if (!isupdatefororder)
                        {
                            Database.myreader = Database.GetExecuteReaderCommand("select isnull(sum(qty),0)  from vw_domesticinvoice where orderno = '"
                            + dataGridView1.Rows[i].Cells[OrderNo.Index].FormattedValue.ToString() + "' and (iscancel = '' or iscancel is null)  ");
                            if (Database.myreader.Read())
                            {
                                invqty = Convert.ToDouble(Database.myreader[0].ToString());
                            }
                            Database.myreader.Close();
                        }
                        else
                        {
                            Database.myreader = Database.GetExecuteReaderCommand("select isnull(sum(qty),0)  from vw_domesticinvoice where orderno = '"
                           + dataGridView1.Rows[i].Cells[OrderNo.Index].FormattedValue.ToString() + "'  and (iscancel = '' or iscancel is null) and  " +
                            " invno not in (select invno from vw_domesticinvoice where invno = '" + textInvNo.Text + "' and suffix = '"
                            + textSuffix.Text + "')");
                            if (Database.myreader.Read())
                            {
                                invqty = Convert.ToDouble(Database.myreader[0].ToString());
                            }
                            Database.myreader.Close();
                        }

                        pending = (orderqty + (orderqty * .10)) - (invqty + Convert.ToDouble(dataGridView1.Rows[i].Cells[DOrderQty.Index].FormattedValue.ToString()));
                        if (pending < 0)
                        {
                            ispending = true;

                        }
                        else
                            ispending = false;

                        if (ispending)
                            break;
                    }
                }
                return ispending;

            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
                return true;

            }
        }



        private void dataGridView1_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            try
            {
                #region
                if (!isupdate)
                {
                    if (dataGridView1.IsCurrentCellDirty)
                    {

                        dataGridView1.CommitEdit(DataGridViewDataErrorContexts.Commit);
                        if (dataGridView1.CurrentRow.Cells.IndexOf(dataGridView1.CurrentCell) == DCommodity.Index)
                        {

                            if (Database.OpenConnection(Utility.DespacthConnectionString))
                            {
                                string Commodity = Convert.ToString(dataGridView1.Rows[dataGridView1.CurrentRow.Index].Cells[DCommodity.Index].Value);
                                string THeading = "";
                                Database.OpenConnection(Utility.DespacthConnectionString);
                                string strsql = "Select top 1 THeading,NCGSTRate,NSGSTRate,NIGSTRate from CommodityHSNGST where companyname = '" + comboCompanyName.Text
                                    + "' and CommodityName='" + Commodity + "' and (('" + dateTimePickerInvDate.Value.ToString("yyyy-MM-dd")
                                    + "' between nFromDate and nToDate) or (nFromDate <= '"
                                    + dateTimePickerInvDate.Value.ToString("yyyy-MM-dd") + "' and nToDate is null ))";

                                Database.myreader = Database.GetExecuteReaderCommand(strsql);
                                //"Select top 1 THeading,NCGSTRate,NSGSTRate,NIGSTRate from Commodity where companyname = '"                                    + comboCompanyName.Text + "' and CommodityName='" + Commodity + "' and todate is null "
                                if (Database.myreader.Read())
                                {
                                    THeading = Database.myreader[0].ToString();
                                    THeading = Database.myreader["THeading"].ToString();
                                    if (dateTimePickerInvDate.Value.Date >= Convert.ToDateTime("2021-04-01") && THeading.Length > 4)
                                        dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[colHSNCode.Index].Value = THeading;
                                    else
                                        dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[colHSNCode.Index].Value = THeading.Substring(0, 4);

                                    if (comboState.Text.ToUpper() == "Gujarat".ToUpper())
                                    {
                                        dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[colCGSTRate.Index].Value = Database.myreader["NCGSTRate"].ToString();
                                        dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[ColSGSTRate.Index].Value = Database.myreader["NSGSTRate"].ToString();
                                        dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[ColIGSTRate.Index].Value = 0;
                                    }
                                    else
                                    {

                                        dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[colCGSTRate.Index].Value = 0;
                                        dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[ColSGSTRate.Index].Value = 0;
                                        dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[ColIGSTRate.Index].Value = Database.myreader["NIGSTRate"].ToString();
                                    }

                                    //dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[colCGSTRate.Index].Value = Database.myreader["NCGSTRate"].ToString();
                                    //dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[ColSGSTRate.Index].Value = Database.myreader["NSGSTRate"].ToString();
                                    //dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[ColIGSTRate.Index].Value = Database.myreader["NIGSTRate"].ToString();
                                }
                                Database.myreader.Close();
                                if (DateTime.Now < Convert.ToDateTime("2017-07-01"))
                                {
                                    if (THeading.Length > 0)
                                    {
                                        //Commodify_ExciseRate
                                        Database.myreader = Database.GetExecuteReaderCommand("Select top 1 ExicseDuty from Commodify_ExciseRate where THeading='" + THeading +
                                            "' and convert(char,cast('" + dateTimePickerInvDate.Value.ToString("yyyy-MM-dd") + " 00:00:00' as date),102) >= convert(char,Fromdate,102)");

                                        if (Database.myreader.Read())
                                        {
                                            textExDuty.Text = Database.myreader[0].ToString();
                                            Database.myreader.Close();
                                        }
                                        else
                                        {
                                            Database.myreader = Database.GetExecuteReaderCommand("select * from MaterialProcessing..excisemaster");
                                            if (Database.myreader.Read())
                                            {
                                                textExDuty.Text = Database.myreader[0].ToString();
                                            }
                                            Database.myreader.Close();
                                        }
                                    }
                                    else
                                    {
                                        Database.myreader = Database.GetExecuteReaderCommand("select * from MaterialProcessing..excisemaster");
                                        if (Database.myreader.Read())
                                        {
                                            textExDuty.Text = Database.myreader[0].ToString();
                                        }
                                        Database.myreader.Close();
                                    }
                                }
                                Database.Closeconnection();
                            }
                        }
                        else if (dataGridView1.CurrentRow.Cells.IndexOf(dataGridView1.CurrentCell) == colMainGrp.Index)
                        {
                            string deptt = Convert.ToString(dataGridView1.Rows[dataGridView1.CurrentRow.Index].Cells[colMainGrp.Index].Value);

                            DataGridViewComboBoxCell ColGRP = (DataGridViewComboBoxCell)dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[colGroupName.Index];
                            dtGroupName.DefaultView.RowFilter = "Depttname='" + deptt + "'";
                            if (dtGroupName.DefaultView.ToTable(true, "GROUPNAME").Rows.Count > 0)
                            {
                                ColGRP.DataSource = dtGroupName.DefaultView.ToTable(true, "GROUPNAME");
                                ColGRP.ValueMember = dtGroupName.Columns["GROUPNAME"].ColumnName.ToString();
                                ColGRP.DisplayMember = dtGroupName.Columns["GROUPNAME"].ColumnName.ToString();
                            }
                            DataTable dt = new DataTable();
                            dt = dtIMaster.Copy();
                            DataGridViewComboBoxCell Col11 = (DataGridViewComboBoxCell)dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[colItemCode.Index];
                            dt.DefaultView.RowFilter = "Deptt='" + deptt + "'";
                            Col11.DataSource = dt.DefaultView.ToTable();
                            Col11.ValueMember = dt.Columns[0].ColumnName.ToString();
                            Col11.DisplayMember = dt.Columns[1].ColumnName.ToString();
                        }
                        else if (dataGridView1.CurrentRow.Cells.IndexOf(dataGridView1.CurrentCell) == colGroupName.Index)
                        {
                            string deptt = Convert.ToString(dataGridView1.Rows[dataGridView1.CurrentRow.Index].Cells[colMainGrp.Index].Value);
                            string groupname = Convert.ToString(dataGridView1.Rows[dataGridView1.CurrentRow.Index].Cells[colGroupName.Index].Value);
                            DataTable dt = new DataTable();
                            dt = dtIMaster.Copy();

                            DataGridViewComboBoxCell ColSubGRP = (DataGridViewComboBoxCell)dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[colSubGrp.Index];
                            dtSubGroupName.DefaultView.RowFilter = "DepartmentName='" + deptt + "' and GroupName='" + groupname + "'";
                            if (dtSubGroupName.DefaultView.ToTable(true, "SubGroupName").Rows.Count > 0)
                            {
                                ColSubGRP.DataSource = dtSubGroupName.DefaultView.ToTable(true, "SubGroupName");
                                ColSubGRP.ValueMember = dtSubGroupName.Columns["SubGroupName"].ColumnName.ToString();
                                ColSubGRP.DisplayMember = dtSubGroupName.Columns["SubGroupName"].ColumnName.ToString();
                            }

                            DataGridViewComboBoxCell Col11 = (DataGridViewComboBoxCell)dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[colItemCode.Index];
                            dt.DefaultView.RowFilter = "Deptt='" + deptt + "' and GroupName='" + groupname + "'";
                            if (dt.DefaultView.ToTable(true, "ItemCode").Rows.Count > 0)
                            {
                                Col11.DataSource = dt.DefaultView.ToTable();
                                Col11.ValueMember = dt.Columns[0].ColumnName.ToString();
                                Col11.DisplayMember = dt.Columns[1].ColumnName.ToString();
                            }
                        }
                        else if (dataGridView1.CurrentRow.Cells.IndexOf(dataGridView1.CurrentCell) == colSubGrp.Index)
                        {
                            string deptt = Convert.ToString(dataGridView1.Rows[dataGridView1.CurrentRow.Index].Cells[colMainGrp.Index].Value);
                            string groupname = Convert.ToString(dataGridView1.Rows[dataGridView1.CurrentRow.Index].Cells[colGroupName.Index].Value);
                            string subGrp = Convert.ToString(dataGridView1.Rows[dataGridView1.CurrentRow.Index].Cells[colSubGrp.Index].Value);

                            DataTable dt = Database.GetDataTable("Select distinct Itemcode,Itemname from MaterialProcessing..item where Deptt='" + deptt
                                + "' and GroupName='" + groupname + "' and SubGroupName='" + subGrp + "' and companyname='" + comboCompanyName.Text + "'");
                            DataGridViewComboBoxCell Col11 = (DataGridViewComboBoxCell)dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[colItemCode.Index];
                            Col11.DataSource = dt.DefaultView.ToTable();
                            Col11.ValueMember = dt.Columns[0].ColumnName.ToString();
                            Col11.DisplayMember = dt.Columns[1].ColumnName.ToString();

                        }
                        else if (dataGridView1.CurrentRow.Cells.IndexOf(dataGridView1.CurrentCell) == colItemCode.Index)
                        {
                            if (textInvNo.Text.Length == 0)
                            {
                                string itemcode = Convert.ToString(dataGridView1.Rows[dataGridView1.CurrentRow.Index].Cells[colItemCode.Index].Value);

                                string strsql = "SELECT  distinct Split.a.value('.', 'VARCHAR(200)') AS Commodity from " +
                                    " (select CAST ('<M>' + REPLACE(REPLACE(i.Commodity,'&','&#38;'), ',', '</M><M>') + '</M>' AS XML) AS Commodity from MaterialProcessing..Item i " +
                                    " where i.itemcode='" + itemcode + "' and i.CompanyName='" + comboCompanyName.Text + "' and isnull(i.Commodity,'')<>'') AS A " +
                                    " CROSS APPLY Commodity.nodes ('/M') AS Split(a) order by 1 ";

                                DataTable dt = Database.GetDataTable(strsql);

                                DataGridViewComboBoxCell ColCommo = (DataGridViewComboBoxCell)dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[DCommodity.Index];
                                ColCommo.DataSource = dt;
                                ColCommo.ValueMember = dt.Columns[0].ColumnName.ToString();
                                ColCommo.DisplayMember = dt.Columns[0].ColumnName.ToString();
                                string itemdesc = Convert.ToString(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[DItemDesc.Index].Value);
                                if (itemdesc.Length == 0)
                                    dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[DItemDesc.Index].Value = Convert.ToString(dataGridView1.Rows[dataGridView1.CurrentRow.Index].Cells[colItemCode.Index].FormattedValue);
                            }
                        }
                    }
                }
                #endregion

            }
            catch (Exception ex)
            {
                ex.ToString();
            }
        }

        private void CheckGST()
        {
            try
            {
                if (Database.OpenConnection(Utility.DespacthConnectionString))
                {
                    for (int i = 0; i <= dataGridView1.Rows.Count - 1; i++)
                    {
                        string Commodity = Convert.ToString(dataGridView1.Rows[i].Cells[DCommodity.Index].Value);
                        if (Commodity.Length == 0)
                            continue;

                        string THeading = "";
                        Database.OpenConnection(Utility.DespacthConnectionString);
                        Database.myreader = Database.GetExecuteReaderCommand("Select top 1 THeading,NCGSTRate,NSGSTRate,NIGSTRate from CommodityHSNGST where companyname = '"
                            + comboCompanyName.Text + "' and CommodityName='" + Commodity + "' and (('" + dateTimePickerInvDate.Value.ToString("yyyy-MM-dd")
                        + "' between nFromDate and nToDate) or (nFromDate <= '"
                        + dateTimePickerInvDate.Value.ToString("yyyy-MM-dd") + "' and nToDate is null )) ");
                        if (Database.myreader.Read())
                        {
                            THeading = Database.myreader[0].ToString();
                            THeading = Database.myreader["THeading"].ToString();
                            if (dateTimePickerInvDate.Value.Date >= Convert.ToDateTime("2021-04-01"))
                                dataGridView1.Rows[i].Cells[colHSNCode.Index].Value = THeading;
                            else
                                dataGridView1.Rows[i].Cells[colHSNCode.Index].Value = THeading.Substring(0, 4);


                            // comment by manish 30th May 2024
                            //if (comboState.Text.ToUpper() == "Gujarat".ToUpper() && comboDestination.Text.ToUpper().Contains("KASEZ") == false)
                            //{
                            //    dataGridView1.Rows[i].Cells[colCGSTRate.Index].Value = Database.myreader["NCGSTRate"].ToString();
                            //    dataGridView1.Rows[i].Cells[ColSGSTRate.Index].Value = Database.myreader["NSGSTRate"].ToString();
                            //    dataGridView1.Rows[i].Cells[ColIGSTRate.Index].Value = 0;
                            //}
                            //else
                            //{

                            //    dataGridView1.Rows[i].Cells[colCGSTRate.Index].Value = 0;
                            //    dataGridView1.Rows[i].Cells[ColSGSTRate.Index].Value = 0;
                            //    dataGridView1.Rows[i].Cells[ColIGSTRate.Index].Value = Database.myreader["NIGSTRate"].ToString();
                            //}
                        }

                    }
                }
            }
            catch (Exception ex)
            {

            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                if (textInvNo.Text.Length == 0) return;
                if (Database.OpenConnection(Utility.DespacthConnectionString))
                {
                    string strgstInv = Convert.ToString(Database.GetExecuteNonQueryCommand_retrun("SELECT GSTInvNo FROM Vw_DomesticInvoice WHERE InvNo=" +
                            textInvNo.Text + " and companyname='" + comboCompanyName.Text + "' and ARESuffix='" + textSuffix.Text + "'"));
                    if (!DespatchCommonFunction.IsEwayGenerated(comboCompanyName.Text, textSuffix.Text, strgstInv))
                    {
                        Database.GetExecuteNonQueryCommand("INSERT INTO EWayBillMaster(CompanyName,InvNo,Yr,EWayNo) VALUES('" +
                            comboCompanyName.Text + "','" + strgstInv + "','" + textSuffix.Text + "','" + TxtEwayNo.Text + "')");
                        MessageBox.Show("E-Way Bill No update for Inv No :" + strgstInv);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Problem with Updating E-Way Bill No :" + ex.Message);
            }
        }

        private void tsbtnAttachment_Click(object sender, EventArgs e)
        {
            if (this.textInvNo.Text.Length > 0)
            {
                Tools.DMSManager.OpenReference("DS_DI", string.Format("{0}/{1}/DS_DI/{2}",
                    AccountCommonFunction.GetCompanyCode(this.comboCompanyName.Text),
                    this.textSuffix.Text,
                    this.textInvNo.Text
                    ));
            }
        }

        private void dateTimePickerInvDate_ValueChanged(object sender, EventArgs e)
        {
            DespatchCommonFunction.LoadCustomerUnlock(comboBuyerName, comboCompanyName.Text, dateTimePickerInvDate.Value);
        }

        private void comboCompanyName_SelectedIndexChanged_1(object sender, EventArgs e)
        {
            DespatchCommonFunction.LoadCustomerUnlock(comboBuyerName, comboCompanyName.Text, dateTimePickerInvDate.Value);
        }

        private void dataGridView1_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (comboBox != null)
                {
                    // Here we will remove the subscription for selected index changed
                    comboBox.SelectedIndexChanged -= new EventHandler(comboBox_SelectedIndexChanged);
                }
            }
            catch (Exception Ex)
            {
                Ex.Message.ToString();
            }
        }

        private void dataGridView1_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void chk_dispfrm_CheckedChanged(object sender, EventArgs e)
        {
            bool isChecked = chk_dispfrm.Checked;

            dispfrm.Visible = isChecked;
            dispfrm_add.Visible = isChecked;
            combodispfrm.Visible = isChecked;
            textdispfrm_add.Visible = isChecked;

            // Load only when checked
            if (!isChecked)
            {
                combodispfrm.Text = "";
                textdispfrm_add.Text = "";
                return;
            }

            combodispfrm.Items.Clear();

            if (Database.OpenConnection(Utility.MaterialConnectionString))
            {
                try
                {
                    Database.myreader = Database.GetExecuteReaderCommand(
                        "SELECT DISTINCT FirmName FROM Vendor ORDER BY FirmName");

                    while (Database.myreader.Read())
                    {
                        combodispfrm.Items.Add(
                            Database.myreader["FirmName"].ToString());
                    }
                }
                finally
                {
                    if (Database.myreader != null)
                        Database.myreader.Close();

                    Database.Closeconnection();
                }
            }
        }

        private void combodispfrm_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (Database.OpenConnection(Utility.MaterialConnectionString))
            {
                Database.myreader = Database.GetExecuteReaderCommand("select Address from Vendor where FirmName = '" + combodispfrm.Text + "'");
                while (Database.myreader.Read())
                    textdispfrm_add.Text = Database.myreader["Address"].ToString();
                Database.myreader.Close();
            }
            Database.Closeconnection();
        }

        private void comboConsigneeName_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {

                if (Database.OpenConnection(Utility.DespacthConnectionString))
                {
                    comboConsigneeCity.Items.Clear();
                    comboConsigneeCity.Text = "";
                    string strsql = "select City from Consignee where consigneename = '" + comboConsigneeName.Text + "' and companycode ='" + CompanyCode + "'";
                    DespatchCommonFunction.Loadcombo(comboConsigneeCity, strsql, "city", "city", Utility.DespacthConnectionString);
                    if (comboConsigneeCity.Items.Count > 0)
                        comboConsigneeCity.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
            }
        }

        private void comboConsigneeCity_SelectedIndexChanged_1(object sender, EventArgs e)
        {
            try
            {
                using (System.Data.SqlClient.SqlConnection con = new System.Data.SqlClient.SqlConnection(Utility.DespacthConnectionString))
                {
                    con.Open();
                    string strsql = "select ConsigneeAdd from Consignee where consigneename = '"
                       + comboConsigneeName.Text + "' and companycode ='" + CompanyCode + "' and city = '" + comboConsigneeCity.Text + "'";
                    System.Data.SqlClient.SqlCommand cmd = new System.Data.SqlClient.SqlCommand(strsql, con);
                    System.Data.SqlClient.SqlDataReader reader = cmd.ExecuteReader();
                    if (reader.Read())
                        textConsignee.Text = reader[0].ToString();
                    reader.Close();
                }

            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
            }
        }

        // INVOICE-RULE helper: packing-list kg on Despatch Godown only (WareHouse.StkInHand).
        private void InvoiceRule_AdjustDespatchGodown(string company, string packingListCsv, bool removeFromDespatch)
        {
            if (packingListCsv == null || packingListCsv.Trim() == "" || company == null || company.Trim() == "")
                return;
            string inList = packingListCsv.Replace(",", "','");
            DataTable dt = null;
            if (Database.OpenConnection(Utility.DespacthConnectionString))
                dt = Database.GetDataTable("Select Itemcode, sum(isnull(Netwt,0)) Netwt from vw_PackingListAll_dtl with (nolock) where PACKINGLISTNO in ('" + inList + "') group by Itemcode");
            if (dt == null || dt.Rows.Count == 0)
                return;
            if (!Database.OpenConnection(Utility.MaterialConnectionString))
                return;
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                string itemCode = Convert.ToString(dt.Rows[i]["Itemcode"]);
                double netwt = Utility.SafeConvertToDouble(dt.Rows[i]["Netwt"]);
                if (itemCode == null || itemCode.Trim() == "" || netwt == 0)
                    continue;
                if (removeFromDespatch)
                    Database.GetExecuteNonQueryCommand("Update Warehouse set stkinHand=case when Round(stkinHand - " + netwt + ",2)<0 then 0 else Round(stkinHand - " + netwt + ",2) end where CompanyName='" + company + "' and WarehouseName='Despatch Godown' and ItemCode='" + itemCode + "'");
                else
                {
                    int exists = Convert.ToInt32(Database.GetExecuteNonQueryCommand_retrun("Select count(*) from Warehouse where itemcode='" + itemCode + "' and companyname='" + company + "' and warehousename='Despatch Godown'"));
                    if (exists == 0)
                        Database.GetExecuteNonQueryCommand("Insert into warehouse (ItemCode,ItemName,GroupName,Deptt,Minlevel,Maxlevel,ReOrder,Unit,StkInHand,TypeVendor,NameVendor,EmailVendor,QtyOrder,MnthlyConsum,Rate,CompanyName,LoginName,Dummy1,OpeningStock,SubGroupName,WareHouseName) select ItemCode,ItemName,GroupName,Deptt,Minlevel,Maxlevel,ReOrder,Unit,0 StkInHand,TypeVendor,NameVendor,EmailVendor,QtyOrder,MnthlyConsum,Rate,CompanyName,LoginName,Dummy1,OpeningStock,SubGroupName,'Despatch Godown' from item where itemcode='" + itemCode + "' and companyname='" + company + "'");
                    Database.GetExecuteNonQueryCommand("Update Warehouse set stkinHand=Round(stkinHand + " + netwt + ",2) where CompanyName='" + company + "' and WarehouseName='Despatch Godown' and ItemCode='" + itemCode + "'");
                }
            }
        }
    }

}