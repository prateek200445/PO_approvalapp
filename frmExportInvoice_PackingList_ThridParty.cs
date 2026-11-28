using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using System.Collections;
using System.Data.SqlClient;

namespace ERP
{
    public partial class frmExportInvoice_PackingList_ThridParty : Form
    {
        private DataSet ds = new DataSet("myDs");
        private DataTable dt = new DataTable("myDT");
        bool IsInit = false;

        string CompanyCode = "";
        public int TotalCount = 0;
        public double UpdateGrossWt = 0;
        public double TotalUOM = 0;
        public double UpdatedTotalAmt = 0;
        ArrayList ArrListSionNo = new ArrayList();
        ArrayList ArrListImportItem = new ArrayList();
        ArrayList ArrListImportItemUnit = new ArrayList();
        ArrayList ArrListImportItemWt = new ArrayList();
        ArrayList ArrItemName = new ArrayList();
        ArrayList ArrUOM = new ArrayList();
        ArrayList ArrOrderQty = new ArrayList();
        ArrayList ArrUnitPrice = new ArrayList();
        ArrayList ArrCommodityRemarks = new ArrayList();
        ArrayList ArrListDeptt = new ArrayList();
        string ExportITCHSNo = "";

        double Friegth = 0;
        double TCS = 0;
        bool SameCommodity = false;

        public frmExportInvoice_PackingList_ThridParty()
        {
            InitializeComponent();
            Form_Load();
        }

        private void Form_Load()
        {
            foreach (DataGridViewColumn column in dataGridView1.Columns)
            {
                column.SortMode = DataGridViewColumnSortMode.NotSortable;
            }
            comboType.SelectedIndex = 0;
            dateTimePickerDateTimeRemoval.Format = DateTimePickerFormat.Custom;
            dateTimePickerDateTimeRemoval.CustomFormat = DespatchCommonFunction.ERPDateSettings();
            dateTimePickerInvoice.Format = DateTimePickerFormat.Custom;
            dateTimePickerInvoice.CustomFormat = DespatchCommonFunction.ERPDateSettings();
            dateTimePickerInvDate.Format = DateTimePickerFormat.Custom;
            dateTimePickerInvDate.CustomFormat = DespatchCommonFunction.ERPDateSettings();
            dateTimePickerDespatchDate.Format = DateTimePickerFormat.Custom;
            dateTimePickerDespatchDate.CustomFormat = DespatchCommonFunction.ERPDateSettings();
            dateTimeReferance.Format = DateTimePickerFormat.Custom;
            dateTimeReferance.CustomFormat = DespatchCommonFunction.ERPDateSettings();
            datetimeBLdate.Format = DateTimePickerFormat.Custom;
            datetimeBLdate.CustomFormat = DespatchCommonFunction.ERPDateSettings();
            dateTimeBuyerOrderNo.Format = DateTimePickerFormat.Custom;
            dateTimeBuyerOrderNo.CustomFormat = DespatchCommonFunction.ERPDateSettings();
            btnUpdate.Enabled = false;
            GettingAdvLic();
            comboTypeInvoice.SelectedIndex = 0;
            comboTaxApp.SelectedIndex = 0;
            comboModeTransport.SelectedIndex = 3;
            string strsql = "";
            if (Database.OpenConnection(Utility.DespacthConnectionString))
            {

                DespatchCommonFunction.GetCompanyList(comboCompanyName);
                comboCompanyName.Text = frmDefaultVale.CompanyName;
                DespatchCommonFunction.GetDepttName(comboDeptt);
                DespatchCommonFunction.GetTransportMaster(comboTransporter);
                if (comboTransporter.Items.Count > 0)
                    comboTransporter.SelectedIndex = 0;

                strsql = "select DisPortName from dischargeport order by DisPortName";
                DespatchCommonFunction.Loadcombo(combo_discharge, strsql, "DisPortName", "DisPortName", Utility.DespacthConnectionString);
                strsql = "select PortCode,loadportname from loadingport order by PortCode";
                DespatchCommonFunction.Loadcombo(combo_loading, strsql, "loadportname", "PortCode", Utility.DespacthConnectionString);
                //strsql = "Select distinct CompanyName from CompanyMaster  order by CompanyName";

                strsql = "Select distinct CompanyName from CompanyMaster where (invoicecategory = 'Export'  or invoicecategory = '*') " +
                                          " and isnull(IsFreeze,'no')='no' and (('" + dateTimePickerInvDate.Value.ToString("yyyy-MM-dd") +
                                          "' between fromdate and todate) or (fromdate <= '" + dateTimePickerInvDate.Value.ToString("yyyy-MM-dd") +
                                          "' and todate is null ))  order by CompanyName";
                DespatchCommonFunction.Loadcombo(comboBuyerName, strsql, "CompanyName", "CompanyName", Utility.DespacthConnectionString);
                strsql = "Select Distinct(vehicalNo) as vehicalNo from DomesticInvoice order by vehicalNo";
                DespatchCommonFunction.Loadcombo(comboVehicalNo, strsql, "vehicalNo", "vehicalNo", Utility.DespacthConnectionString);
                strsql = "select distinct paymenttermname from MaterialProcessing..PaymentTermsMaster where Type='Sales' and isdelflag=0 order by paymenttermname";
                DespatchCommonFunction.Loadcombo(textTermsofPayment, strsql, "paymenttermname", "paymenttermname", Utility.DespacthConnectionString);
                DespatchCommonFunction.GetCurrencyCombo(comboCurrency, Utility.DespacthConnectionString);

                Database.myreader = Database.GetExecuteReaderCommand("select * from MaterialProcessing..excisemaster");
                if (Database.myreader.Read())
                {
                    textCVDPerc.Text = Database.myreader[0].ToString();
                    textExDuty.Text = Database.myreader[0].ToString();
                    textEduCess.Text = Database.myreader[1].ToString();
                    textSecHiEdu.Text = Database.myreader[2].ToString();
                }
                Database.myreader.Close();
                Database.myreader = Database.GetExecuteReaderCommand("select importErsCode from MaterialProcessing..factoryInfo where name='"
                    + comboCompanyName.Text + "'");
                if (Database.myreader.Read())
                {
                    textIE_code.Text = Database.myreader[0].ToString();
                }
                Database.myreader.Close();

                strsql = "Select Distinct(StateName) as StateName from StateMaster WITH(NOLOCK)  ";
                DespatchCommonFunction.Loadcombo(comboState, strsql, "StateName", "StateName", Utility.DespacthConnectionString);

                strsql = "Select EPCGLicNo from EPCGMaster where companyname = '" + comboCompanyName.Text + "' order by EPCGLicNo";
                DespatchCommonFunction.Loadcombo(comboEPCGLicense, strsql, "EPCGLicNo", "EPCGLicNo", Utility.DespacthConnectionString);
                if (comboEPCGLicense.Items.Count > 0)
                    comboEPCGLicense.SelectedIndex = 0;

                string EPCGNo = "";
                Database.myreader = Database.GetExecuteReaderCommand("Select * from EPCGMaster where companyname = '"
                + comboCompanyName.Text + "' and setDefault = 'yes' order by EPCGLicNo");
                if (Database.myreader.Read())
                    EPCGNo = Database.myreader[0].ToString();
                Database.myreader.Close();

                //comboEPCGNo_SelectedIndexChanged.Text = "3730000120";

                Database.myreader = Database.GetExecuteReaderCommand("Select * from UTBond where companyname = '"
                    + comboCompanyName.Text + "' order by BondDate");
                while (Database.myreader.Read())
                {
                    textUT1BondNo.Text = Database.myreader[1].ToString();
                }
                Database.myreader.Close();

                DespatchCommonFunction.GetBuyerName(comboThirdPartyName, Utility.DespacthConnectionString, dateTimePickerInvDate.Value);

                Database.Closeconnection();


            }

            //groupAdvanceLic.Visible = false;
            //groupEPCG.Visible = false;
            groupDirectExport.Visible = false;
            //GettingAdvLic();
            comboTaxApp.SelectedIndex = 1;
            //if (comboBuyerName.Items.Count > 0)
            //    comboBuyerName.SelectedIndex = 0;
            //if (comboCompanyName.Items.Count > 0)
            //    comboCompanyName.SelectedIndex = 0;
            comboCurrency.SelectedIndex = 0;
            IsInit = false;

            textARESuffix.Text = frmDefaultVale.Year;
            fillData();

            if (comboCompanyName.Text == "Plastene India Limited (Unit -II)")
            {
                //checkImport.Checked = false;  //18-11-2010
                checkSelfSealing.Checked = false;
                // checkImport.Enabled = false;
                textAREDetail.Text = "";
                textAREDetails1.Text = "";
                textPackages.Text = "";
            }
        }




        void fillData()
        {

            if (Database.OpenConnection(Utility.DespacthConnectionString))
            {
                Database.myadapter = Database.GetAdapterCommand("Select CommodityName from Commodity where companyname = '"
                    + comboCompanyName.Text + "' order by CommodityName");
                DataTable dt = new DataTable();
                Database.myadapter.Fill(dt);
                DCommodity.DataSource = dt;
                DCommodity.ValueMember = dt.Columns[0].ColumnName.ToString();
                DCommodity.DisplayMember = dt.Columns[0].ColumnName.ToString();
                combocommodity.DataSource = dt;
                combocommodity.DisplayMember = dt.Columns[0].ColumnName.ToString();
                combocommodity.ValueMember = dt.Columns[0].ColumnName.ToString();

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
                dt = new DataTable();
                Database.myadapter.Fill(dt);

                DUOM.DataSource = dt;
                DUOM.ValueMember = dt.Columns[0].ColumnName.ToString();
                DUOM.DisplayMember = dt.Columns[0].ColumnName.ToString();

                DPKgUnit.DataSource = dt;
                DPKgUnit.ValueMember = dt.Columns[0].ColumnName.ToString();
                DPKgUnit.DisplayMember = dt.Columns[0].ColumnName.ToString();
            }

        }



        private void frmExportInvoice_Load(object sender, EventArgs e)
        {


        }

        private void btnSave_Click(object sender, EventArgs e)
        {

            int VenCnt = DespatchCommonFunction.CustomerLedgerName(comboBuyerName.Text, comboCompanyName.Text);
            if (VenCnt == 0)
            {
                MessageBox.Show("No Account Ledger Found for this customer.\nPlease Ask Account Dept to create Ledger for this customer:- [" + comboBuyerName.Text + "]");
                return;
            }
            if (combo_loading.Text.Length == 0)
            {
                MessageBox.Show("Please Select Port Code");
                combo_loading.Focus();
                return;
            }
            if (!combo_loading.Text.Contains(combo_loading.Text))
            {
                MessageBox.Show("Please Select Port Code");
                combo_loading.Focus();
                return;
            }
            if (comboContract1.Text == "")
                MessageBox.Show("Please select proper nature of contract");
            else if (!(textTermsofPayment.Items.Contains(textTermsofPayment.Text.Trim())))
                MessageBox.Show("Please select proper payment terms or enter payment term in payment master");
            else if (textgrosswt.Text == "")
                MessageBox.Show("Please enter value in GrossWt");
            else if (texttarewt.Text == "")
                MessageBox.Show("Please enter value in TareWt");
            else if (textpaletwt.Text == "")
                MessageBox.Show("Please enter value in Pallet Wt");
            else if (combocommodity.Text == "")
                MessageBox.Show("Please select Commodity");

            //else if (comboModeTransport.Text == "")
            //    MessageBox.Show("Please Select Mode of Transit");
            else if (textPackagingDetail.Text == "")
                MessageBox.Show("Please enter packaging detail");
            //else if (textplace_delivery.Text == "")
            //    MessageBox.Show("Please enter Delivery Place ");
            //else if (combo_loading.Text == "")
            //    MessageBox.Show("Please enter loading port");
            //else if (combo_discharge.Text == "")
            //    MessageBox.Show("Please enter Discharge port");
            else
            {

                bool IsValidBuyerName = false;
                bool IsNotContract = false;
                bool IsSpecialDuty = false;
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

                if (comboContract1.Text == "CIF" || comboContract1.Text == "CF")
                {
                    if (Convert.ToDouble(txtSeaFreAmt.Text) != 0)
                        IsNotContract = true;
                }
                else if (comboContract1.Text == "DDU")
                {
                    if (Convert.ToDouble(textFrieght.Text) != 0)
                        IsNotContract = true;
                }
                else
                    IsNotContract = true;


                if (IsSpecialDuty)
                    MessageBox.Show("Put Proper Special Duty Remarks");
                if (!IsNotContract)
                    MessageBox.Show("Put Proper value in frieght amount");
                else if (Convert.ToDouble(textgrosswt.Text) < Convert.ToDouble(textntwt.Text))
                {
                    MessageBox.Show("Gross weight can not be less than net weight");
                    textgrosswt.Text = "0";
                }
                else
                {
                    calc1();
                    double total = 0;
                    bool IsWt = false;
                    double GrossWts = 0;
                    double TempTotal = 0;
                    string InvNo = "";
                    int ARENo = 0;
                    string commodity = "";
                    try
                    {
                        if (Database.OpenConnection(Utility.DespacthConnectionString))
                        {
                            textEntryNo.Text = GetEntryNo(textARESuffix.Text, comboCompanyName.Text, Convert.ToDateTime(dateTimePickerInvDate.Value));
                            Database.Closeconnection();
                        }
                        if (textEntryNo.Text == "")
                            MessageBox.Show("Please Enter value in Entry Number");
                        else
                        {
                            Database.OpenConnection(Utility.DespacthConnectionString);
                            Database.myreader = Database.GetExecuteReaderCommand("Select ARENo,InvNo from packinglist where yr = '"
                                        + textARESuffix.Text + "' and companyname = '" + comboCompanyName.Text + "' order by InvNo desc");
                            if (Database.myreader.Read())
                            {
                                ARENo = Convert.ToInt32(Database.myreader["ARENo"].ToString());
                            }
                            Database.myreader.Close();
                            if (ARENo == 0)
                                ARENo = 001;
                            else
                                ARENo++;

                            if (textSpDuty1Amt.Text == "")
                                textSpDuty1Amt.Text = "0";
                            if (textSpDuty2Amt.Text == "")
                                textSpDuty2Amt.Text = "0";
                            if (textSpDuty3Amt.Text == "")
                                textSpDuty3Amt.Text = "0";
                            //string iscommercial = "";
                            //string commercialinvoice = "";




                            //total = Math.Round(total, 0);
                            total = Math.Round(total, 0, MidpointRounding.AwayFromZero);
                            double temptotal = total;
                            //total = Math.Round(total, 0);

                            //double Educess2 = 0;
                            //double HiEducess2 = 0;

                            double Excise = 0;
                            double Educess = 0;
                            double HiEducess = 0;
                            //double Vat = 0;
                            //double AddVat = 0;



                            double spduty1 = Convert.ToDouble(textSpDuty1Amt.Text);
                            double spduty2 = Convert.ToDouble(textSpDuty2Amt.Text);
                            double spduty3 = Convert.ToDouble(textSpDuty3Amt.Text);


                            bool IsAppEpCG = false;
                            TotalCount = 0;
                            if (dataGridView1.Rows.Count - 1 > 0)
                            {
                                TotalCount++;

                            }
                            if (CheckApplicabilityEPCG_PendingLimit(comboEPCGLicense.Text, Convert.ToDouble(textGrdTotal.Text)))
                                IsAppEpCG = true;

                            bool IsAppAdvLicense = true;

                            if (CheckApplicabilityAdvLic_PendingLimit(comboAdvance.Text, Convert.ToDouble(textgrosswt.Text)))
                                IsAppAdvLicense = true;


                            bool IsWrongDate = false;
                            if (textBuyerOrderNo.Text != "")
                            {
                                if (dateTimeBuyerOrderNo.Text == "01/01/1900" || dateTimeBuyerOrderNo.Text == "")
                                    IsWrongDate = true;
                            }
                            if (!IsAppEpCG && chkEPCGLIC.Checked)
                                MessageBox.Show("Limit of EPCG is exceeded beyond Licesene Limit");
                            if (!IsAppAdvLicense && chkAdvLic.Checked)
                                MessageBox.Show("Limit of Advance Limit is exceeded beyond Licesene Limit");
                            if (comboDeptt.Text == "")
                                MessageBox.Show("Please enter Value in DepttName");
                            else if (TotalCount <= 0)
                                MessageBox.Show("Please Select Items from Item Information");
                            else if (IsWt)
                                MessageBox.Show("Net Weight should be lesser than Gross Weight");
                            else if (IsWrongDate)
                                MessageBox.Show("Buyer Order Date is not Correct Format");
                            else if (TotalCount > 0)
                            {

                                GetCommodity_Theding();
                                if (Database.OpenConnection(Utility.DespacthConnectionString))
                                {


                                    int InvoiceSeries = 0;
                                    //textInvNo.Text = GenerateInvno(comboCompanyName.Text, textARESuffix.Text, ref InvoiceSeries);

                                    if (DateTime.Now < Convert.ToDateTime("2017-07-01 00:00:00"))
                                    {
                                        textInvNo.Text = GenerateInvno(comboCompanyName.Text, textARESuffix.Text, ref InvoiceSeries);
                                    }
                                    else
                                    {
                                        textInvNo.Text = DespatchCommonFunction.GenerateInvno(comboCompanyName.Text, textARESuffix.Text, ref InvoiceSeries);
                                    }

                                    InvNo = textInvNo.Text;
                                    string ConsigneeCode = "";
                                    Database.myreader = Database.GetExecuteReaderCommand("select ConsigneeCode from Consignee where consigneename = '"
                                     + comboConsigneeName.Text + "' and companycode ='" + CompanyCode + "' and city = ' " + comboConsigneeCity.Text + "'");
                                    if (Database.myreader.Read())
                                        ConsigneeCode = Database.myreader[0].ToString();
                                    Database.myreader.Close();

                                    //string InsuranceApp = "";
                                    //string Insurance = "";

                                    int DespatchNo = 0; //Convert.ToInt32(InvNo.Substring(1));



                                    string st2 = "";

                                    string buyerorderdate = "";
                                    if (textBuyerOrderNo.Text == "")
                                        buyerorderdate = System.DateTime.Now.ToString("MM/dd/yyyy");//dr[5] = System.DateTime.Now.ToString("MM/dd/yyyy");
                                    else
                                        buyerorderdate = dateTimeBuyerOrderNo.Value.ToString(DespatchCommonFunction.ERPDBDateSettings());
                                    string EpCGNO = "";
                                    //string EPCGNODate = "";
                                    string Advlicenseno = "";
                                    //string AdvlicensenoDate = "";
                                    string Utibond = "";
                                    //string UtiBondDate = "";
                                    string isepcg = "no";
                                    string isadvlic = "no";
                                    string isimport = "no";
                                    string iscenvat = "no";
                                    string isDifa = "no";
                                    string DifaNo = "";

                                    if (chkAdvLic.Checked)
                                    {
                                        isadvlic = "yes";
                                        Advlicenseno = comboAdvance.Text;
                                    }
                                    else
                                    {
                                        isadvlic = "no";
                                        Advlicenseno = "";
                                    }

                                    if (chkEPCGLIC.Checked)
                                    {
                                        isepcg = "yes";
                                        EpCGNO = comboEPCGLicense.Text;
                                        // EPCGNODate = dateTimePickerEPCGDate.Value.ToString();
                                    }
                                    else
                                    {
                                        isepcg = "no";
                                        EpCGNO = "";
                                    }

                                    if (chkDFIA.Checked)
                                    {
                                        isDifa = "yes";

                                        // EPCGNODate = dateTimePickerEPCGDate.Value.ToString();
                                    }
                                    else
                                    {
                                        isDifa = "yes";

                                    }
                                    if (comboExportUnder.SelectedIndex == 1) //UTBond
                                        Utibond = textUT1BondNo.Text;
                                    else
                                        Utibond = "";


                                    string totalamtword = "";
                                    string exciseamtword = "";
                                    if (comboTaxApp.SelectedIndex == 0)
                                    {
                                        totalamtword = Utility.IntegerToWords(Convert.ToInt64(TempTotal));
                                        exciseamtword = "NIL";
                                    }

                                    string utbonddetails = "";

                                    string billofexportdate = "";
                                    string utbondexpirydate = "";



                                    if (checkImport.Checked)
                                        isimport = "yes";
                                    else
                                        isimport = "no";

                                    if (checkCENVAT.Checked)
                                        iscenvat = textCENVATNo.Text;
                                    else
                                        iscenvat = "NIL";
                                    string isdirectexport = "";
                                    string packinglistno = "";
                                    string contno = "";
                                    string bottlesealno = "";
                                    string packages = "";
                                    if (checkDirectExport.Checked)
                                    {
                                        isdirectexport = "yes";
                                        packinglistno = textPackingListNo.Text.ToUpper();
                                        contno = textContNo.Text.ToUpper();
                                        bottlesealno = textBottleSealNo.Text.ToUpper();
                                        packages = textPackages.Text.ToUpper();
                                    }
                                    else
                                    {
                                        isdirectexport = "no";

                                        packages = textPackages.Text.ToUpper();
                                    }
                                    //need to check before giving 
                                    Int64 exciseamt = Convert.ToInt64(Excise + Educess + HiEducess);
                                    string s = comboModeTransport.Text;
                                    string st = "";
                                    for (int i = 0; i < comboModeTransport.Items.Count; i++)
                                    {
                                        if (s != comboModeTransport.Items[i].ToString())
                                            st += comboModeTransport.Items[i].ToString() + "/";
                                    }
                                    int t = st.LastIndexOf('/');
                                    st = st.Remove(t);

                                    ArrayList arrItem = new ArrayList();
                                    arrItem.Add("[By air");
                                    arrItem.Add("/");
                                    arrItem.Add("sea");
                                    arrItem.Add("/");
                                    arrItem.Add("Parcel");
                                    arrItem.Add("/");
                                    arrItem.Add("Land");
                                    arrItem.Add("/");
                                    arrItem.Add("Post");
                                    arrItem.Add("/and]");
                                    string st1 = "";
                                    for (int i = 0; i < arrItem.Count; i++)
                                    {
                                        if (s.ToLower() != arrItem[i].ToString().ToLower())
                                            st1 += arrItem[i].ToString();
                                        else
                                            ++i;
                                    }
                                    string iscenvattaken = "";
                                    string rg23A = "";
                                    if (checkCENVAT.Checked)
                                    {
                                        iscenvattaken = "yes";
                                        rg23A = textRG23A.Text;
                                    }
                                    else
                                        iscenvat = "no";
                                    string istrading = "";
                                    string isdfia = "";
                                    //if (checkTrading.Checked)
                                    //    istrading = "Trading Invoice";
                                    //if (checkDFIA.Checked)
                                    //    isdfia = "yes";
                                    //else
                                    isdfia = "no";
                                    bool isentry = false;
                                    string exciseamtinword = "";
                                    string temptotalinword = "";
                                    string temptotalRSInWords = "";
                                    string temptotalFOBinword = "";
                                    //   temptotalFOBinword = Utility.IntegerToWords(Convert.ToInt64(TextFOB.Text));
                                    // temptotalinword = Utility.IntegerToWords(Convert.ToInt64(TempTotal));
                                    //exciseamtinword = Utility.IntegerToWords(exciseamt);
                                    //temptotalRSInWords = Utility.IntegerToWords(Convert.ToInt64(textAmntInRS.Text));
                                    DialogResult drs = MessageBox.Show("Do you want to generate " + comboTypeInvoice.Text.ToUpper()
                                     + Environment.NewLine + " Do you want to generate  Invoice for " + comboCompanyName.Text.ToUpper()
                                     + Environment.NewLine + st2, "Generate Export Invoice"
                                                     , MessageBoxButtons.YesNo, MessageBoxIcon.Information);

                                    if (drs.CompareTo(DialogResult.Yes) == 0)
                                    {

                                        if (Database.OpenConnection(Utility.DespacthConnectionString))
                                        {

                                            Database.BeginTransaction();
                                            //bill leading no as total unit of messurement
                                            //BILL _LEADING_DT AS DATE OF ISSUE
                                            //DOC_DT AS DATE TIME OF REMOVAL
                                            //Authorised as specialDutyremarks
                                            //totalCBM as FOB Rate 
                                            //bankdetail As Rate in RS
                                            //AmountInRupeesWords as transPorter Name
                                            //accounthead as Vehicle NO
                                            //doc_cred_no as LR No  LRNO
                                            if (Database.GetExecuteNonQueryCommand("INSERT INTO [Despatch].[dbo].[Packinglist] ([IE_code],[Invoice_no],[Invoice_dt],[Buyers_od_no],[Buyers_od_dt] "
                                                    + " ,[country_origin] ,[country_desti],[Buyers],[terms_payement],"
                                                    + " [pre_carrige],[place_receipt],[vessl_flteno],[port_loading],[port_discharge],[place_delivery] "
                                                    + ",[Hc_containerno],[line_seal],[cusom_seal_no],[gross_wt],[tare_wt],[pallet_wt],[tot_net_wt],[yr],[remarks],[consignee],[cons_city] "
                                                    + " ,[currancyconversion],[currancy],[cur_value] "
                                                    + " ,[deptt],[CommercialAmount],[CompanyName],[Notifier],[ContType],[AmtWord],[TotalAmt],[HTSCODE],[commodity] "
                                                    + " ,[EpcgNo],[advno],[utibondno],[isepcg],[isadvlic],[beingexport] ,[isdfia],[isimport] "
                                                    + " ,[exciseper],[educessper],[hieducessper] ,[areno],[FOBValue],[FrieghtAmount],[Insurance],[Commision],[Discount],SpDutyAmt1 ,[Deduction],[DutyForegone]" //[Other]
                                                    + ",[AMountinRupees],[TransportName],[typeofshipment],[NatureofContract],[Authorised],SpDutyRemarks1 "
                                                    + " ,[BCDPERC],[CVDPerc],[EDCessPerc],[HEDCessPerc],[EDuCEssPerc],[HeducessPerc],[AddVatPerc] "
                                                    + "  ,[BCD],[CVD],[EDCess],[HEDCess],[EDuCEssoel2],[Heducess],[AddVatOel2] "
                                                    + " ,[OpeningBalance],[DutyDebited] ,[ClosingBalance],[deliveryterms],[packagingdetial] ,[cons_ntbyer] ,[modeoftransit]"
                                                    + " ,reference,remarks2,invno,totalCBM,bankdetail,bill_leading_dt,doc_dt,bill_leading_no,VehicalNo,LRNO,FreightRemarks,FreightRemarks2,"
                                                    + " FreightAmt2,InlandHaulageRem,InlandHaulageAmt,OtherDuty,OtherDutyAmt,DiscountRem,SpDutyRemarks2,SpDutyAmt2,SpDutyRemarks3,SpDutyAmt3,ThirdPartyName,manucheckbox,IsthirdParty,thirdparty,ADCODE,PortCode)   VALUES  ('"
                                                    + textIE_code.Text + "','" + InvNo + "','" + dateTimePickerInvDate.Value.ToString(DespatchCommonFunction.ERPDBDateSettings()) + "','" + textBuyerOrderNo.Text + "','" + dateTimeBuyerOrderNo.Value.ToString(DespatchCommonFunction.ERPDBDateSettings())
                                                    + "','INDIA','" + textDestinationPlace.Text + "','" + comboBuyerName.Text + "','"
                                                    + textTermsofPayment.Text
                                                    + "','" + textprecarriage.Text + "','" + textreceipt_precarrier.Text + "','" + textvessel_no.Text + "','" + combo_loading.Text + "','"
                                                    + combo_discharge.Text + "','" + textplace_delivery.Text
                                                    + "','" + texthc_containerno.Text + "','" + textline_Seal.Text + "','" + textcustom_sealno.Text + "'," + textgrosswt.Text + "," + texttarewt.Text
                                                    + "," + textpaletwt.Text
                                                    + "," + textntwt.Text + ",'" + textARESuffix.Text + "','" + textRemarks.Text + "','" + comboConsigneeName.Text + "','" + comboConsigneeCity.Text
                                                    + "'," + textCurrencyExchangeRate.Text
                                                    + ",'" + comboCurrency.Text + "','" + comboCurrencyValue.Text + "','" + comboDeptt.Text + "'," + TextFOB.Text + ",'" + comboCompanyName.Text
                                                    + "','" + textNotifier.Text + "','" + comboContType.Text + "','" + temptotalFOBinword + "'," + textGrdTotal.Text + ",'" + textHeading.Text  //TextFOB.Text
                                                    + "','" + combocommodity.Text + "','"
                                                    + EpCGNO + "','" + Advlicenseno
                                                    + "','" + Utibond + "','" + isepcg + "','" + isadvlic + "','" + comboExportUnder.Text + "','" + isdfia + "','" + isimport + "',"
                                                    + 0 + "," + 0 + "," + 0 + ",'" + ARENo + "'," + TextFOB.Text + "," + textFrieght.Text + "," + textinsuranceAmnt.Text + ", 0 ,"
                                                    + textDiscount.Text + "," + textSpDuty1Amt.Text
                                                    + ",0," + exciseamt + "," + Math.Round(Convert.ToDouble(textAmntInRS.Text), 2) + ",'" + comboTransporter.Text + "','" + comboTypeOfShipment.Text + "','" + comboContract1.Text + "','AUTHORISED SIGNATORY','"
                                                    + textSpDutyRemarks1.Text + "',"
                                                    + textBCDPerc.Text + "," + textCVDPerc.Text + "," + textEDCessPerc.Text + "," + textHEDcessPerc.Text + "," + textEDUCESSPerc.Text
                                                    + "," + textHEDUCESSPerc.Text + "," + textAddVatPerc.Text + ","
                                                    + textBCD.Text + "," + textCVD.Text + "," + textEDCESS.Text + "," + textHEDCESS.Text + "," + textEDUCESSOEL2.Text
                                                    + "," + textHEDUCESS.Text + "," + textAddVat2.Text + "," + textOpeningBalance.Text + "," + textDutyDebited.Text + "," + textClosingBalance.Text + ",'" + textTermsofDelivery.Text + "','" + textPackagingDetail.Text + "','" + textConsignee.Text + "','" + comboModeofTransit.Text + "','"
                                                    + textReferanceNo.Text + " Date :" + dateTimeReferance.Value.ToString(DespatchCommonFunction.ERPDBDateSettings()) + "','" + textRemarks2.Text + "'," + InvoiceSeries + "," + textFobRate.Text
                                                    + ",'" + textRateInRs.Text + "','" + datetimeBLdate.Value.ToString(DespatchCommonFunction.ERPDBDateSettings()) + "','" + dateTimePickerDateTimeRemoval.Value.ToString(DespatchCommonFunction.ERPDBDateSettings()) + "','" + txtbillofloading.Text + "','"
                                                    + comboVehicalNo.Text + "','" + textLrNo.Text + "','"
                                                    + TxtFreightRem.Text + "','" + txtSeaFrieghtRem.Text + "'," + Convert.ToDouble(txtSeaFreAmt.Text) + ",'"
                                                    + TxtInLandHauRem.Text + "'," + Convert.ToDouble(TxtHaulageAmt.Text) + ",'" + TxtOtherRem.Text + "',"
                                                    + Convert.ToDouble(TxtOthersDiscAmt.Text) + ",'" + txtDiscountRem.Text + "','" + textSpDutyRemark2.Text + "',"
                                                    + textSpDuty2Amt.Text + ",'" + textSpDutyRemarks3.Text + "'," + textSpDuty3Amt.Text + ",'" + comboThirdPartyName.Text
                                                    + "',1,'yes','" + comboCompanyName.Text + "','" + TxtADCODE.Text + "','" + combo_loading.SelectedValue + "')"))
                                                isentry = true;

                                            //FreightRemarks2,FreightAmt2,InlandHaulageRem,InlandHaulageAmt,OtherDuty,OtherDutyAmt
                                            textInvNo.Text = InvNo;

                                            SetEntryNo(textARESuffix.Text, comboCompanyName.Text, Convert.ToDateTime(dateTimePickerInvDate.Text));

                                            // Database.Closeconnection();
                                            Database.GetExecuteNonQueryCommand("Update  PackingListCreditDuty set refinvno='" + textInvNo.Text + "' where (refinvno is null or refinvno='') and companyname='"
                                                + comboCompanyName.Text + "' and date <'" + dateTimePickerInvDate.Value + "'");

                                            for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                                            {
                                                if (dataGridView1.Rows[i].Cells[1].FormattedValue.ToString() != "")
                                                // if (Database.OpenConnection(Utility.DespacthConnectionString))
                                                {
                                                    double Currency = GetCurrencyConversion();
                                                    //double GPM = 0;
                                                    //double GSM = 0;
                                                    //double Reqwt = 0;
                                                    //double NetWt = Convert.ToDouble(dataGridView1.Rows[i].Cells[10].Value.ToString());
                                                    //double OrderQty = Convert.ToDouble(dataGridView1.Rows[i].Cells[7].Value.ToString());

                                                    Database.myadapter = Database.GetAdapterCommand("select * from Packingitems");
                                                    DataSet dataset1 = new DataSet();
                                                    Database.myadapter.Fill(dataset1, "Packingitems");

                                                    //insert data in database 
                                                    DataRow dr = dataset1.Tables[0].NewRow();
                                                    dr[0] = InvNo;


                                                    if (dataGridView1.Rows[i].Cells[1].FormattedValue.ToString() != "")
                                                        dr[1] = dataGridView1.Rows[i].Cells[1].Value.ToString();

                                                    if (dataGridView1.Rows[i].Cells[2].FormattedValue.ToString() != "")
                                                        dr[2] = dataGridView1.Rows[i].Cells[2].Value.ToString();

                                                    if (dataGridView1.Rows[i].Cells[4].FormattedValue.ToString() != "")
                                                        dr[3] = dataGridView1.Rows[i].Cells[4].FormattedValue.ToString();

                                                    if (dataGridView1.Rows[i].Cells[7].FormattedValue.ToString() != "")
                                                        dr[4] = dataGridView1.Rows[i].Cells[7].Value.ToString();

                                                    if (dataGridView1.Rows[i].Cells[6].FormattedValue.ToString() != "")
                                                        dr[5] = dataGridView1.Rows[i].Cells[6].Value.ToString();

                                                    if (dataGridView1.Rows[i].Cells[10].FormattedValue.ToString() != "")
                                                        dr[6] = dataGridView1.Rows[i].Cells[10].Value.ToString();
                                                    //if (dataGridView1.Rows[i].Cells[0].FormattedValue.ToString() != "")
                                                    //    dr[7] = dataGridView1.Rows[i].Cells[0].Value.ToString();
                                                    dr[7] = InvNo;


                                                    if (dataGridView1.Rows[i].Cells[8].FormattedValue.ToString() != "")
                                                        dr[8] = dataGridView1.Rows[i].Cells[8].Value.ToString();

                                                    dr[9] = comboCurrencyValue.Text;
                                                    //if (dataGridView1.Rows[i].Cells[11].FormattedValue.ToString() != "")
                                                    if (dataGridView1.Rows[i].Cells[9].FormattedValue.ToString() != "")
                                                        dr[10] = dataGridView1.Rows[i].Cells[9].FormattedValue.ToString();


                                                    if (dataGridView1.Rows[i].Cells[14].FormattedValue.ToString() != "")
                                                        dr[11] = dataGridView1.Rows[i].Cells[14].FormattedValue.ToString();

                                                    if (dataGridView1.Rows[i].Cells[13].FormattedValue.ToString() != "")
                                                        dr[12] = dataGridView1.Rows[i].Cells[13].FormattedValue.ToString();


                                                    dr["ItemDeptt"] = dataGridView1.Rows[i].Cells[DtDeptt.Index].FormattedValue.ToString();  //commodity
                                                    dr["Commodity"] = dataGridView1.Rows[i].Cells[DCommodity.Index].FormattedValue.ToString();  //commodity
                                                    dr["HSNCode"] = dataGridView1.Rows[i].Cells[colHSNCode.Index].FormattedValue.ToString();
                                                    dr["CGSTrate"] = dataGridView1.Rows[i].Cells[ColCGSTRate.Index].FormattedValue.ToString();
                                                    dr["CGSTamt"] = dataGridView1.Rows[i].Cells[colCGSTAmt.Index].FormattedValue.ToString();
                                                    dr["SGSTRate"] = dataGridView1.Rows[i].Cells[ColSGSTRate.Index].FormattedValue.ToString();
                                                    dr["SGSTAmt"] = dataGridView1.Rows[i].Cells[colSGSTAmt.Index].FormattedValue.ToString();
                                                    dr["IGSTrate"] = dataGridView1.Rows[i].Cells[colIGSTRate.Index].FormattedValue.ToString();
                                                    dr["IGSTAmt"] = dataGridView1.Rows[i].Cells[ColIGSTAmt.Index].FormattedValue.ToString();


                                                    dataset1.Tables[0].Rows.Add(dr);
                                                    Database.mycmdBuilder = Database.GetCmdBuilderCommand(Database.myadapter);
                                                    int x = Database.myadapter.Update(dataset1.Tables[0]);

                                                    //  Database.Closeconnection();
                                                    if (checkImport.Checked)
                                                        InsertValueInExportCntMaterial(InvNo, textARESuffix.Text);
                                                    if (chkAdvLic.Checked)
                                                    {
                                                        GrossWts = 0;

                                                        GrossWts += Convert.ToDouble(textntwt.Text);// textgrosswt.Text);

                                                        DespatchCommonFunction.UpdateAdvLic_PendingLimit(comboCompanyName.Text, comboAdvance.Text, GrossWts);
                                                    }

                                                }
                                            }
                                            if (isentry)
                                            {
                                                Database.CommitTransaction();
                                                MessageBox.Show("Invoice No " + InvNo + " and ARENo = "
                                                    + ARENo.ToString() + " is generated succesfully");
                                                Database.Closeconnection();

                                            }
                                        }
                                        //IsUpdate = false;
                                        TotalCount = 0;
                                        textAddVat.Text = "0";
                                        textLrNo.Text = "";
                                        textBuyerOrderNo.Text = "";
                                        //     textDelChallanNo.Text = "";
                                        textDesPatchNo.Text = "";
                                        textEduCess.Text = "0";
                                        textExDuty.Text = "12.5";
                                        textInvNo.Text = "";
                                        textTermsofDelivery.Text = "";
                                        textSecHiEdu.Text = "1";
                                        textTermsofPayment.Text = "";
                                        textVat.Text = "0";
                                        textGrdTotal.Text = "";

                                        btnUpdate.Enabled = false;
                                        btnSave.Enabled = true;

                                        dataGridView1.Rows.Clear();
                                        //   checkTrading.Checked = false;

                                        Utility.UserInformation("Save", "Export Invoice " + InvNo.ToString() + " is saved successfully");

                                        //FrmDispatchReportPIL frm = new FrmDispatchReportPIL();
                                        //frm.Show();
                                    }
                                }
                            }
                        }
                    }

                    catch (Exception ex)
                    {

                        ErrorMessageBox.Show(ex);
                        Database.RollBackTransaction();

                    }
                }
            }
        }

        private void InsertJV(string Invno, double IncentiveAmount, int Srno = 0)
        {
            try
            {
                if (Database.OpenConnection(Utility.DespacthConnectionString))
                {

                    int JVNo = Convert.ToInt16(Database.GetExecuteNonQueryCommand_retrun("select JournalNo from Materialprocessing.dbo.JournalItem where billno ='" + Invno
                        + "' and CompanyName = '" + comboCompanyName.Text + "' and yr = '" + textARESuffix.Text + "'"));
                    Database.BeginTransaction();
                    Database.mycommand = new System.Data.SqlClient.SqlCommand();
                    Database.mycommand.Parameters.Clear();
                    Database.mycommand.CommandType = CommandType.StoredProcedure;
                    Database.mycommand.Connection = Database.myconn;
                    Database.mycommand.Transaction = Database.mytransaction;
                    Database.mycommand.CommandText = "SP_Insert_Update_ACJournal";


                    Database.mycommand.Parameters.AddWithValue("@CompanyName", comboCompanyName.Text);
                    Database.mycommand.Parameters.AddWithValue("@year", textARESuffix.Text);
                    Database.mycommand.Parameters.AddWithValue("@sysdate", DateTime.Now);
                    Database.mycommand.Parameters.AddWithValue("@IncentiveAmt", IncentiveAmount);

                    Database.mycommand.Parameters.AddWithValue("@username", FrmMain.UserName);
                    Database.mycommand.Parameters.AddWithValue("@InvNo", Invno);
                    Database.mycommand.Parameters.AddWithValue("@InvDate", dateTimePickerInvoice.Text);
                    Database.mycommand.Parameters.AddWithValue("@UpdateJVNo", JVNo);

                    if (JVNo == 0)
                        Database.mycommand.Parameters.AddWithValue("@Srno", 0);
                    else
                        Database.mycommand.Parameters.AddWithValue("@Srno", Srno);

                    Database.mycommand.ExecuteNonQuery();

                    Database.CommitTransaction();

                }
            }
            catch (Exception ex)
            {
                Database.RollBackTransaction();
                MessageBox.Show(ex.ToString());
            }
        }


        private void GetCommodity_Theding()
        {
            string THeading = "";
            textHeading.Text = "";
            ArrayList ArrTHeading = new ArrayList();
            for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
            {
                if (dataGridView1.Rows[i].Cells[3].FormattedValue.ToString() != "" &&
                   dataGridView1.Rows[i].Cells[6].FormattedValue.ToString() != "" &&
                   dataGridView1.Rows[i].Cells[7].FormattedValue.ToString() != "" &&
                   dataGridView1.Rows[i].Cells[8].FormattedValue.ToString() != "")
                {
                    if (Database.OpenConnection(Utility.DespacthConnectionString))
                    {
                        Database.myreader = Database.GetExecuteReaderCommand("select THeading from commodity where CommodityName ='"
                              + dataGridView1.Rows[i].Cells[3].Value.ToString() + "'");
                        while (Database.myreader.Read())
                        {
                            ArrTHeading.Add(Database.myreader[0].ToString());
                        }
                        Database.myreader.Close();
                        Database.Closeconnection();
                    }
                }
            }
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
        }

        private double GetCurrencyConversion()
        {
            double CurrencyConversion = 1;
            if (comboCurrency.SelectedIndex == 0) //INR
                CurrencyConversion = 1;
            else if (comboCurrency.SelectedIndex == 1) //GBP
            {
                Database.myreader = Database.GetExecuteReaderCommand("select Pound from currency order by sysdate desc");
                if (Database.myreader.Read())
                    CurrencyConversion = Convert.ToDouble(Database.myreader[0].ToString());
                Database.myreader.Close();
            }
            else if (comboCurrency.SelectedIndex == 2) //EURO
            {
                Database.myreader = Database.GetExecuteReaderCommand("select EURO from currency order by sysdate desc");
                if (Database.myreader.Read())
                    CurrencyConversion = Convert.ToDouble(Database.myreader[0].ToString());
                Database.myreader.Close();
            }
            else if (comboCurrency.SelectedIndex == 2) //Dollar
            {
                Database.myreader = Database.GetExecuteReaderCommand("select Dollar from currency order by sysdate desc");
                if (Database.myreader.Read())
                    CurrencyConversion = Convert.ToDouble(Database.myreader[0].ToString());
                Database.myreader.Close();
            }
            return CurrencyConversion;

        }

        private string GetEntryNo(string yr, string companyname, DateTime dt)
        {
            string EntryNo = "";
            int entry = 0;


            for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
            {
                if (dataGridView1.Rows[i].Cells[4].FormattedValue.ToString() != "" &&
                   dataGridView1.Rows[i].Cells[6].FormattedValue.ToString() != "" &&
                   dataGridView1.Rows[i].Cells[7].FormattedValue.ToString() != "" &&
                   dataGridView1.Rows[i].Cells[8].FormattedValue.ToString() != "")
                {
                    entry = 0;
                    Database.myreader = Database.GetExecuteReaderCommand("select entryno from entryno where commodity ='"
                     + dataGridView1.Rows[i].Cells[3].Value.ToString() + "' and yr = '" + yr + "' and companyname = '"
                     + companyname + "' and Mnth = " + dt.Month + " and MnthYr = " + dt.Year + " order by entryno");
                    while (Database.myreader.Read())
                        entry = Convert.ToInt32(Database.myreader[0].ToString());
                    Database.myreader.Close();
                    entry = entry + 1;

                    EntryNo += entry.ToString() + "/";

                }
            }
            int temp = EntryNo.LastIndexOf("/");
            EntryNo = EntryNo.Remove(temp);
            return EntryNo;
        }


        private string SetEntryNo(string yr, string companyname, DateTime dt)
        {
            string EntryNo = "";
            int entry = 0;

            for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
            {
                if (dataGridView1.Rows[i].Cells[4].FormattedValue.ToString() != "" &&
                   dataGridView1.Rows[i].Cells[6].FormattedValue.ToString() != "" &&
                   dataGridView1.Rows[i].Cells[7].FormattedValue.ToString() != "" &&
                   dataGridView1.Rows[i].Cells[8].FormattedValue.ToString() != "")
                {
                    entry = 0;
                    Database.myreader = Database.GetExecuteReaderCommand("select entryno from entryno where commodity ='"
                     + dataGridView1.Rows[i].Cells[3].Value.ToString() + "' and yr = '" + yr + "' and companyname = '"
                     + companyname + "' and Mnth = " + dt.Month + " and MnthYr = " + dt.Year + " order by entryno");
                    while (Database.myreader.Read())
                        entry = Convert.ToInt32(Database.myreader[0].ToString());
                    Database.myreader.Close();
                    entry = entry + 1;


                    Database.GetExecuteNonQueryCommand("insert into entryno values(" + entry + ",'" +
                         comboCompanyName.Text + "','" + textARESuffix.Text
                          + "','" + dataGridView1.Rows[i].Cells[3].Value.ToString() + "'," + dt.Month + "," + dt.Year
                          + ",'" + dt + "','" + textInvNo.Text + "')");
                    EntryNo = entry.ToString() + "/";
                }
            }
            int temp = EntryNo.LastIndexOf("/");
            EntryNo = EntryNo.Remove(temp);
            return EntryNo;
        }


        private double Calculation(int QtyIndex, int AmountIndex, int TotalIndex)
        {
            TotalCount = 0;
            double total = 0;
            double temptotal = 0;
            string CommodityName = "";
            double dblCGSTamt = 0;
            double dblSGSTAmt = 0;
            double dblIGSTAmt = 0;
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

                if (textSpDuty1Amt.Text == "")
                    textSpDuty1Amt.Text = "0";
                if (textSpDuty2Amt.Text == "")
                    textSpDuty2Amt.Text = "0";
                if (textSpDuty3Amt.Text == "")
                    textSpDuty3Amt.Text = "0";


                for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                {
                    if (dataGridView1.Rows[i].Cells[QtyIndex].FormattedValue.ToString() != "" &&
                      dataGridView1.Rows[i].Cells[3].FormattedValue.ToString() != "" &&
                      dataGridView1.Rows[i].Cells[7].FormattedValue.ToString() != "" &&
                      dataGridView1.Rows[i].Cells[8].FormattedValue.ToString() != "" &&
                      dataGridView1.Rows[i].Cells[AmountIndex].FormattedValue.ToString() != "")
                    {
                        if (i == 0)
                            CommodityName = dataGridView1.Rows[0].Cells[3].Value.ToString();

                        if (CommodityName != dataGridView1.Rows[0].Cells[3].Value.ToString())
                            SameCommodity = true;

                        Convert.ToDouble(dataGridView1.Rows[i].Cells[1].Value.ToString());
                        TotalCount = TotalCount + 1;

                        double NetWt = Convert.ToDouble(dataGridView1.Rows[i].Cells[10].Value.ToString());

                        dataGridView1.Rows[i].Cells[TotalIndex].Value = Convert.ToString(Convert.ToDouble(dataGridView1.Rows[i].Cells[QtyIndex].Value.ToString())
                                   * Convert.ToDouble(dataGridView1.Rows[i].Cells[AmountIndex].Value.ToString()));

                        //colCGSTrate colCGSTamt  colSGSTRate   ColSGSTAmt
                        if (Convert.ToDouble(dataGridView1.Rows[i].Cells[ColCGSTRate.Index].Value) > 0)
                        {

                            dataGridView1.Rows[i].Cells[colCGSTAmt.Index].Value =
                                Math.Round(Convert.ToDouble(dataGridView1.Rows[i].Cells[DTotal.Index].Value.ToString()) *
                                Convert.ToDouble(dataGridView1.Rows[i].Cells[ColCGSTRate.Index].Value.ToString()) / 100, 2);
                            dblCGSTamt += Convert.ToDouble(dataGridView1.Rows[i].Cells[colCGSTAmt.Index].Value);

                        }
                        if (Convert.ToDouble(dataGridView1.Rows[i].Cells[ColSGSTRate.Index].Value) > 0)
                        {

                            dataGridView1.Rows[i].Cells[colSGSTAmt.Index].Value =
                                Math.Round(Convert.ToDouble(dataGridView1.Rows[i].Cells[DTotal.Index].Value.ToString()) *
                                Convert.ToDouble(dataGridView1.Rows[i].Cells[ColSGSTRate.Index].Value.ToString()) / 100, 2);
                            dblSGSTAmt += Convert.ToDouble(dataGridView1.Rows[i].Cells[colSGSTAmt.Index].Value);

                        }
                        if (Convert.ToDouble(dataGridView1.Rows[i].Cells[colIGSTRate.Index].Value) > 0)
                        {

                            dataGridView1.Rows[i].Cells[ColIGSTAmt.Index].Value =
                                Math.Round(Convert.ToDouble(dataGridView1.Rows[i].Cells[DTotal.Index].Value.ToString()) *
                                Convert.ToDouble(dataGridView1.Rows[i].Cells[colIGSTRate.Index].Value.ToString()) / 100, 2);
                            dblIGSTAmt += Convert.ToDouble(dataGridView1.Rows[i].Cells[ColIGSTAmt.Index].Value);

                        }

                        total += Convert.ToDouble(dataGridView1.Rows[i].Cells[TotalIndex].Value);
                        total = Math.Round(total, 0, MidpointRounding.AwayFromZero);
                    }
                }

                temptotal = total + dblCGSTamt + dblIGSTAmt + dblSGSTAmt;
                total = Math.Round(total + dblCGSTamt + dblIGSTAmt + dblSGSTAmt, 0);

                double Excise = 0;
                double Educess = 0;
                double HiEducess = 0;
                double Vat = 0;
                double AddVat = 0;


                if (comboTaxApp.SelectedIndex == 1)
                    total = total + Educess + HiEducess + Vat + AddVat + Excise;


                Friegth = Convert.ToDouble(textFrieght.Text);
                total = total + Friegth;

                TCS = Convert.ToDouble(textDiscount.Text);
                TCS = (total * TCS) / 100;
                TCS = Math.Round(TCS, 0);


                double spduty1 = Convert.ToDouble(textSpDuty1Amt.Text);
                double spduty2 = Convert.ToDouble(textSpDuty2Amt.Text);
                double spduty3 = Convert.ToDouble(textSpDuty3Amt.Text);

                total = total + TCS + spduty1 + spduty2 + spduty3;


                total = Math.Round(total, 0);

                textGrdTotal.Text = total.ToString();


            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
            }
            return temptotal;
        }

        private bool WeightDifferences(int NetWtIndex, int GrossWtIndex)
        {
            bool IsDiffApp = false;
            try
            {
                for (int i = 0; i < TotalCount; i++)
                {
                    if (dataGridView1.Rows[i].Cells[NetWtIndex].FormattedValue.ToString() != ""
                        && dataGridView1.Rows[i].Cells[GrossWtIndex].FormattedValue.ToString() != "")
                    {
                        if (Convert.ToDouble(dataGridView1.Rows[i].Cells[GrossWtIndex].Value.ToString())
                            < Convert.ToDouble(dataGridView1.Rows[i].Cells[NetWtIndex].Value.ToString()))
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
            //Calculation(7, 8, 9);
            try
            {

                if (comboContract1.Text == "")
                {
                    MessageBox.Show("Please select proper nature of contract.");
                }
                else
                {
                    double total = 0;
                    double fobvalue = 0;
                    bool isallow = true;
                    double ntwt = 0;
                    double grwt = 0;
                    double UOMwt = 0;
                    int totalcount = 0;


                    for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                    {
                        if (dataGridView1.Rows[i].Cells[1].FormattedValue.ToString() == "")
                        {
                            total =
                            totalcount++;
                            MessageBox.Show("Please put proper item information ");
                            isallow = false;
                            break;
                        }

                        if (dataGridView1.Rows[i].Cells[8].FormattedValue.ToString() != "")
                        {
                            total = total + Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[DTotal.Index].FormattedValue.ToString());
                        }
                        dataGridView1.Rows[i].Cells[ColIGSTAmt.Index].Value =
                                    Math.Round(Utility.SafeConvertToDouble(Convert.ToDouble(dataGridView1.Rows[i].Cells[colIGSTRate.Index].Value.ToString())
                                    * Convert.ToDouble(dataGridView1.Rows[i].Cells[DTotal.Index].Value.ToString()) / 100), 2);

                    }
                    textGrdTotal.Text = "0";
                    textinsuranceAmnt.Text = "0";
                    double InstRate = Convert.ToDouble(textInsurancePerc.Text);
                    //textGrdTotal.Text = TextFOB.Text;
                    if (isallow)
                    {
                        if (comboContract1.Text == "CF")
                        {
                            textinsuranceAmnt.Text = "0";
                            if (Convert.ToDouble(textFrieght.Text) != 0)
                            {
                                fobvalue = total + Convert.ToDouble(textFrieght.Text);
                            }
                            else if (Convert.ToDouble(txtSeaFreAmt.Text) != 0)
                            {
                                MessageBox.Show("Please put proper value in fireght Amount");
                            }

                        }
                        else if (comboContract1.Text == "CI")
                        {
                            textFrieght.Text = "0";
                            textinsuranceAmnt.Text = Convert.ToString(Math.Round((total * InstRate / 100), 2));
                            fobvalue = total + Convert.ToDouble(textinsuranceAmnt.Text);
                        }
                        else if (comboContract1.Text == "FOB")
                        {
                            fobvalue = total;
                            textinsuranceAmnt.Text = "0";
                            textFrieght.Text = "0";

                        }
                        else if (comboContract1.Text == "DDU")
                        {
                            textinsuranceAmnt.Text = Convert.ToString(Math.Round((total * InstRate / 100), 2));
                            fobvalue = total + Convert.ToDouble(textFrieght.Text) + Convert.ToDouble(textinsuranceAmnt.Text);
                        }
                        else if (comboContract1.Text == "DDP")
                        {
                            fobvalue = total + Convert.ToDouble(textFrieght.Text);
                        }
                        if (textDiscount.Text != "")
                            fobvalue = fobvalue - Convert.ToDouble(textDiscount.Text);

                        if (textSpDuty1Amt.Text != "")
                            fobvalue = fobvalue + Convert.ToDouble(textSpDuty1Amt.Text);

                        if (textSpDuty2Amt.Text != "")
                            fobvalue = fobvalue + Convert.ToDouble(textSpDuty2Amt.Text);

                        if (textSpDuty3Amt.Text != "")
                            fobvalue = fobvalue + Convert.ToDouble(textSpDuty3Amt.Text);


                        TextFOB.Text = fobvalue.ToString();
                        textFobRate.Text = (fobvalue / UOMwt).ToString();


                        textntwt.Text = ntwt.ToString();
                        // textgrosswt.Text = grwt.ToString();
                        texttarewt.Text = (Convert.ToDouble(textgrosswt.Text) - Convert.ToDouble(textntwt.Text)).ToString();
                        textAmntInRS.Text = Math.Round((fobvalue * Convert.ToDouble(textCurrencyExchangeRate.Text)), 2).ToString();
                        textRateInRs.Text = Math.Round((Convert.ToDouble(textAmntInRS.Text) / UOMwt), 2).ToString();
                        calc1();
                        //    textFOBValue.BackColor = Color.Red;
                    }
                }

            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
            }

        }

        void clearForRead()
        {
            textAddVat.Text = "0";
            textLrNo.Text = "";
            textBuyerOrderNo.Text = "";
            textReferanceNo.Text = "";
            textDesPatchNo.Text = "";
            textEduCess.Text = "0";
            textExDuty.Text = "12.5";

            textTermsofDelivery.Text = "";
            textSecHiEdu.Text = "0";
            textTermsofPayment.Text = "";
            textVat.Text = "0";

            textAddVatPerc.Text = "0";
            textFrieght.Text = "0";
            textDiscount.Text = "0";
            textntwt.Text = "0";
            textBCD.Text = "0";
            textBCDPerc.Text = "0";
            textCVD.Text = "0";
            textCVDPerc.Text = "0";
            textEDCESS.Text = "0";
            textEDCessPerc.Text = "0";
            textEduCess.Text = "0";
            textEDUCESSOEL2.Text = "0";
            textEDUCESSPerc.Text = "0";
            //textEduAmt.Text = "0";
            textDiscount.Text = "0";
            //textExciseAmt.Text = "0";
            TextFOB.Text = "0";
            textAmntInRS.Text = "0";
            textAddVatPerc.Text = "0";
            //textAddVatAmt.Text = "0";
            textAddVat.Text = "0";
            textAddVat2.Text = "0";
            textRemarks.Text = "";

            textRemarks2.Text = "";
            textplace_delivery.Text = "";
            textprecarriage.Text = "";
            textreceipt_precarrier.Text = "";
            textvessel_no.Text = "";
            textSpDutyRemarks1.Text = "";
            textNotifier.Text = "";
            textline_Seal.Text = "";
            textIE_code.Text = "";
            txtDiscountRem.Text = "";
            txtSeaFrieghtRem.Text = "";
            txtSeaFreAmt.Text = "0";
            TxtInLandHauRem.Text = "";
            TxtHaulageAmt.Text = "0";
            TxtOtherRem.Text = "";
            TxtOthersDiscAmt.Text = "0";

            txtFOBV.Text = "0";
            textRateInRs.Text = "0";
            textFobRate.Text = "0";
            TextFOB.Text = "0";
            textAmntInRS.Text = "0";

        }
        private void textInvNo_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            try
            {
                clearForRead();

                string companyname = "";
                bool iscoomercial = false;
                string commercialinvno = "";
                bool IsCheckIsurance = false;
                if (Database.OpenConnection(Utility.DespacthConnectionString))
                {
                    string currency = "";
                    string consignee = "";
                    string buyername = "";
                    //    comboAdvLicenseNo.Items.Clear();
                    string Currency = "";
                    string BeingExport = "";
                    string AdvLicenseNo = "";
                    string EPCGNo = "";
                    string currencyExchngeRate = "";

                    bool IsDirectExport = false;
                    bool IsEPCGNo = false;
                    bool ismarketing = false;

                    {
                        //bill leading no as total unit of messurement
                        //BILL _LEADING_DT AS DATE OF ISSUE
                        //DOC_DT AS DATE TIME OF REMOVAL
                        //Authorised as specialDutyremarks
                        //totalCBM as FOB Rate 
                        //bankdetail As Rate in RS
                        //AmountInRupeesWords as transPorter Name
                        //accounthead as Vehicle NO
                        //doc_cred_no as LR No
                        Database.myreader = Database.GetExecuteReaderCommand("select [IE_code],[Invoice_no],[Invoice_dt],[Buyers_od_no],[Buyers_od_dt]  "
                            + " ,[country_desti],[Buyers],[terms_payement], "
                            + " [pre_carrige],[place_receipt],[vessl_flteno],[port_loading],[port_discharge],[place_delivery] , "
                            + " [Hc_containerno],[line_seal],[cusom_seal_no],[gross_wt],[tare_wt],[pallet_wt],[tot_net_wt],[yr], "
                            + " [remarks],[consignee],[cons_city]  ,[currancyconversion],[currancy],[cur_value]  ,[deptt],[CommercialAmount], "
                            + " [Notifier],[ContType],[HTSCODE],[commodity], "
                            + " [FrieghtAmount],[Insurance],[Discount],[Other],[AMountinRupees], "
                            + " [typeofshipment],[NatureofContract],[BCDPERC],[CVDPerc],[EDCessPerc],[HEDCessPerc],[EDuCEssPerc],[HeducessPerc],[AddVatPerc] "
                            + " ,[BCD],[CVD],[EDCess],[HEDCess],[EDuCEssoel2],[Heducess],[AddVatOel2], [deliveryterms] "
                            + " ,[packagingdetial] ,[cons_ntbyer] ,[modeoftransit],reference,remarks2,Authorised,beingExport,utibondno,bill_leading_dt,doc_dt,"
                            + " doc_cred_no, accounthead, amountInRupeesWords,FreightRemarks,FreightRemarks2,FreightAmt2,InlandHaulageRem,InlandHaulageAmt,OtherDuty,"
                            + " OtherDutyAmt,DiscountRem,SpDutyRemarks2,SpDutyAmt2,SpDutyRemarks3,SpDutyAmt3,IsthirdParty,ThirdPartyName,SpDutyRemarks1,SpDutyAmt1 ,VehicalNo,TransportName,LRNO,Bill_leading_no,ADCODE,PortCode"
                            + " from [Packinglist] where Invoice_No = '"
                            + textInvNo.Text + "' ");
                        if (Database.myreader.Read())
                        {

                            if (Convert.ToString(Database.myreader["IsthirdParty"]) == "no")
                            {
                                MessageBox.Show("Please Open This Invoice in Commercial Invoice");
                                clearForRead();
                                return;
                            }
                            btnSave.Enabled = false;
                            btnUpdate.Enabled = true;
                            texthc_containerno.Text = Database.myreader["Hc_containerno"].ToString();
                            textIE_code.Text = Database.myreader["IE_code"].ToString();
                            dateTimePickerInvDate.Value = Convert.ToDateTime(Database.myreader["Invoice_dt"]);
                            textBuyerOrderNo.Text = Database.myreader["Buyers_od_no"].ToString();
                            dateTimeBuyerOrderNo.Value = Convert.ToDateTime(Database.myreader["Buyers_od_dt"].ToString());
                            textplace_delivery.Text = Database.myreader["country_desti"].ToString();
                            buyername = Database.myreader["Buyers"].ToString();
                            textTermsofPayment.Text = Database.myreader["terms_payement"].ToString();
                            textprecarriage.Text = Database.myreader["pre_carrige"].ToString();
                            textreceipt_precarrier.Text = Database.myreader["place_receipt"].ToString();
                            textvessel_no.Text = Database.myreader["vessl_flteno"].ToString();
                            combo_loading.Text = Database.myreader["port_loading"].ToString();
                            combo_loading.SelectedValue = Convert.ToString(Database.myreader["PortCode"]);
                            combo_discharge.Text = Database.myreader["port_discharge"].ToString();
                            textplace_delivery.Text = Database.myreader["place_delivery"].ToString();
                            textContNo.Text = Database.myreader["Hc_containerno"].ToString();
                            textline_Seal.Text = Database.myreader["line_seal"].ToString();
                            textcustom_sealno.Text = Database.myreader["cusom_seal_no"].ToString();
                            textgrosswt.Text = Database.myreader["gross_wt"].ToString();
                            textpaletwt.Text = Database.myreader["pallet_wt"].ToString();
                            textntwt.Text = Database.myreader["tot_net_wt"].ToString();
                            texttarewt.Text = Database.myreader["tare_wt"].ToString();
                            textARESuffix.Text = Database.myreader["yr"].ToString();
                            textRemarks.Text = Database.myreader["remarks"].ToString();
                            datetimeBLdate.Value = Convert.ToDateTime(Database.myreader["Bill_leading_dt"].ToString());
                            consignee = Database.myreader["consignee"].ToString();
                            comboConsigneeCity.Text = Database.myreader["cons_city"].ToString();
                            currencyExchngeRate = Database.myreader["currancyconversion"].ToString();
                            currency = Database.myreader["currancy"].ToString();
                            // comboCurrencyValue.Text = Database.myreader["cur_value"].ToString();
                            comboDeptt.Text = Database.myreader["deptt"].ToString();
                            comboDeptt.Text = Database.myreader["deptt"].ToString();
                            TextFOB.Text = Database.myreader["CommercialAmount"].ToString();
                            textNotifier.Text = Database.myreader["Notifier"].ToString();
                            comboContType.Text = Database.myreader["ContType"].ToString();
                            textHeading.Text = Database.myreader["HTSCODE"].ToString();
                            combocommodity.Text = Database.myreader["commodity"].ToString();
                            textFrieght.Text = Database.myreader["FrieghtAmount"].ToString();
                            textinsuranceAmnt.Text = Database.myreader["Insurance"].ToString();
                            textDiscount.Text = Database.myreader["Discount"].ToString();
                            textSpDuty1Amt.Text = Database.myreader["SpDutyAmt1"].ToString();
                            textAmntInRS.Text = Database.myreader["AMountinRupees"].ToString();
                            comboTypeOfShipment.Text = Database.myreader["typeofshipment"].ToString();
                            comboContract1.Text = Database.myreader["NatureofContract"].ToString();
                            txtbillofloading.Text = Database.myreader["Bill_leading_no"].ToString();
                            textBCDPerc.Text = Database.myreader["BCDPERC"].ToString();
                            textCVDPerc.Text = Database.myreader["CVDPerc"].ToString();
                            textEDCESS.Text = Database.myreader["EDCessPerc"].ToString();
                            textHEDcessPerc.Text = Database.myreader["HEDCessPerc"].ToString();
                            textEDUCESSPerc.Text = Database.myreader["EDuCEssPerc"].ToString();
                            textHEDUCESSPerc.Text = Database.myreader["HeducessPerc"].ToString();
                            textAddVatPerc.Text = Database.myreader["AddVatPerc"].ToString();

                            textBCD.Text = Database.myreader["BCD"].ToString();
                            textCVD.Text = Database.myreader["CVD"].ToString();
                            textEDCESS.Text = Database.myreader["EDCess"].ToString();
                            textHEDUCESS.Text = Database.myreader["HEDCess"].ToString();
                            textEDUCESSOEL2.Text = Database.myreader["EDuCEssoel2"].ToString();
                            textHEDUCESS.Text = Database.myreader["Heducess"].ToString();
                            //    textAddVatAmt.Text = Database.myreader["AddVatOel2"].ToString();
                            TxtADCODE.Text = Database.myreader["ADCODE"].ToString();


                            textTermsofDelivery.Text = Database.myreader["deliveryterms"].ToString();
                            textPackagingDetail.Text = Database.myreader["packagingdetial"].ToString();
                            textConsignee.Text = Database.myreader["cons_ntbyer"].ToString();
                            comboModeofTransit.Text = Database.myreader["modeoftransit"].ToString();
                            textReferanceNo.Text = Database.myreader["reference"].ToString();
                            textRemarks2.Text = Database.myreader["Remarks2"].ToString();
                            textSpDutyRemarks1.Text = Database.myreader["SpDutyRemarks1"].ToString();

                            BeingExport = Database.myreader["beingExport"].ToString();
                            textUT1BondNo.Text = Database.myreader["utibondno"].ToString();
                            //   dateTimePickerInvDate.Text = Database.myreader["bill_leading_dt"].ToString();
                            dateTimePickerDateTimeRemoval.Value = Convert.ToDateTime(Database.myreader["doc_dt"].ToString());

                            textLrNo.Text = Database.myreader["LRNO"].ToString();
                            comboVehicalNo.Text = Database.myreader["VehicalNo"].ToString();
                            comboTransporter.Text = Database.myreader["TransportName"].ToString();

                            TxtFreightRem.Text = Database.myreader["FreightRemarks"].ToString();

                            txtSeaFrieghtRem.Text = Convert.ToString(Database.myreader["FreightRemarks2"]);
                            txtSeaFreAmt.Text = Convert.ToString(Database.myreader["FreightAmt2"]);
                            if (txtSeaFreAmt.Text.Length == 0)
                                txtSeaFreAmt.Text = "0";
                            TxtInLandHauRem.Text = Convert.ToString(Database.myreader["InlandHaulageRem"]);
                            TxtHaulageAmt.Text = Convert.ToString(Database.myreader["InlandHaulageAmt"]);
                            if (TxtHaulageAmt.Text.Length == 0)
                                TxtHaulageAmt.Text = "0";
                            TxtOtherRem.Text = Convert.ToString(Database.myreader["OtherDuty"]);
                            TxtOthersDiscAmt.Text = Convert.ToString(Database.myreader["OtherDutyAmt"]);
                            if (TxtOthersDiscAmt.Text.Length == 0)
                                TxtOthersDiscAmt.Text = "0";
                            txtDiscountRem.Text = Convert.ToString(Database.myreader["DiscountRem"]);

                            textSpDutyRemark2.Text = Convert.ToString(Database.myreader["SpDutyRemarks2"]);
                            textSpDutyRemarks3.Text = Convert.ToString(Database.myreader["SpDutyRemarks3"]);
                            textSpDuty2Amt.Text = Convert.ToString(Database.myreader["SpDutyAmt2"]);
                            textSpDuty3Amt.Text = Convert.ToString(Database.myreader["SpDutyAmt3"]);
                            //
                            comboThirdPartyName.Text = Convert.ToString(Database.myreader["ThirdPartyName"]);

                        }
                        Database.myreader.Close();
                        comboExportUnder.Text = BeingExport;
                        if (!comboBuyerName.Items.Contains(buyername))
                            comboBuyerName.Items.Add(buyername);

                        comboBuyerName.Text = buyername;
                        comboCurrency.Text = currency;
                        comboConsigneeName.Text = consignee;
                        textCurrencyExchangeRate.Text = currencyExchngeRate;


                        dataGridView1.Rows.Clear();
                        Database.OpenConnection(Utility.DespacthConnectionString);
                        object j = Database.GetExecuteScalarCommand_return("select count(*) from [Packingitems] where InvsubNo = '"
                            + textInvNo.Text + "' ");
                        TotalCount = Convert.ToInt32(j);

                        if (TotalCount > 0)
                            dataGridView1.Rows.Add(TotalCount);

                        int i = 0;
                        Database.myreader = Database.GetExecuteReaderCommand("select * from Packingitems where InvsubNo ='"
                                           + textInvNo.Text + "'");

                        while (Database.myreader.Read())
                        {
                            dataGridView1.Rows[i].Cells[0].Value = Database.myreader[7].ToString();

                            dataGridView1.Rows[i].Cells[1].Value = Database.myreader[1].ToString();
                            dataGridView1.Rows[i].Cells[2].Value = Database.myreader[2].ToString();
                            dataGridView1.Rows[i].Cells[3].Value = combocommodity.Text;
                            dataGridView1.Rows[i].Cells[4].Value = Database.myreader[3].ToString();
                            dataGridView1.Rows[i].Cells[5].Value = Database.myreader[13].ToString();
                            dataGridView1.Rows[i].Cells[6].Value = Database.myreader[5].ToString();
                            dataGridView1.Rows[i].Cells[7].Value = Database.myreader[4].ToString();
                            dataGridView1.Rows[i].Cells[8].Value = Database.myreader[8].ToString();
                            dataGridView1.Rows[i].Cells[9].Value = Database.myreader[10].ToString();
                            dataGridView1.Rows[i].Cells[10].Value = Database.myreader[6].ToString();
                            dataGridView1.Rows[i].Cells[13].Value = Database.myreader[11].ToString();
                            dataGridView1.Rows[i].Cells[14].Value = Database.myreader[12].ToString();

                            string commodity = Convert.ToString(Database.myreader["Commodity"]);


                            dataGridView1.Rows[i].Cells[DCommodity.Index].Value = (commodity.Length > 0 ? commodity : combocommodity.Text);
                            dataGridView1.Rows[i].Cells[colHSNCode.Index].Value = Convert.ToString(Database.myreader["HSNCode"]);
                            dataGridView1.Rows[i].Cells[ColCGSTRate.Index].Value = Convert.ToString(Database.myreader["CGSTrate"]);
                            dataGridView1.Rows[i].Cells[colCGSTAmt.Index].Value = Convert.ToString(Database.myreader["CGSTamt"]);
                            dataGridView1.Rows[i].Cells[ColSGSTRate.Index].Value = Convert.ToString(Database.myreader["SGSTRate"]);
                            dataGridView1.Rows[i].Cells[colSGSTAmt.Index].Value = Convert.ToString(Database.myreader["SGSTAmt"]);
                            dataGridView1.Rows[i].Cells[colIGSTRate.Index].Value = Convert.ToString(Database.myreader["IGSTrate"]);
                            dataGridView1.Rows[i].Cells[ColIGSTAmt.Index].Value = Convert.ToString(Database.myreader["IGSTAmt"]);


                            i++;
                        }
                        Database.myreader.Close();
                        Database.Closeconnection();
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
            }

        }

        private void btnInitliaze_Click(object sender, EventArgs e)
        {
            //  checkTrading.Checked = false;
            //IsUpdate = false;
            TotalCount = 0;
            textAddVat.Text = "0";
            textLrNo.Text = "";
            textBuyerOrderNo.Text = "";
            textReferanceNo.Text = "";
            textDesPatchNo.Text = "";
            textEduCess.Text = "0";
            textExDuty.Text = "12.5";
            textInvNo.Text = "";
            textTermsofDelivery.Text = "";
            textSecHiEdu.Text = "0";
            textTermsofPayment.Text = "";
            textVat.Text = "0";
            //checkCommercialInvoice.Checked = false;
            clearForRead();
            dataGridView1.Rows.Clear();
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

            btnUpdate.Enabled = false;
            btnSave.Enabled = true;

        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            //if (!FrmMain.IsUpdateComInvoice)
            //    MessageBox.Show("You have no right to update export Invoice");
            int VenCnt = DespatchCommonFunction.CustomerLedgerName(comboBuyerName.Text, comboCompanyName.Text);
            if (VenCnt == 0)
            {
                MessageBox.Show("No Account Ledger Found for this customer.\nPlease Ask Account Dept to create Ledger for this customer:- [" + comboBuyerName.Text + "]");
                return;
            }
            if (combo_loading.Text.Length == 0)
            {
                MessageBox.Show("Please Select Port Code");
                combo_loading.Focus();
                return;
            }
            if (!combo_loading.Text.Contains(combo_loading.Text))
            {
                MessageBox.Show("Please Select Port Code");
                combo_loading.Focus();
                return;
            }

            if (!(textTermsofPayment.Items.Contains(textTermsofPayment.Text.Trim())))
                MessageBox.Show("Please select proper payment terms or enter payment term in payment master");
            else if (textgrosswt.Text == "")
                MessageBox.Show("Please enter value in GrossWt");
            else if (texttarewt.Text == "")
                MessageBox.Show("Please enter value in TareWt");
            else if (textpaletwt.Text == "")
                MessageBox.Show("Please enter value in Pallet Wt");
            else if (combocommodity.Text == "")
                MessageBox.Show("Please select Commodity");
            else if (comboModeTransport.Text == "")
                MessageBox.Show("Please Select Mode of Transit");
            else if (textPackagingDetail.Text == "")
                MessageBox.Show("Please enter packaging detail");
            //else if (textplace_delivery.Text == "")
            //    MessageBox.Show("Please enter Delivery Place ");
            //else if (combo_loading.Text == "")
            //    MessageBox.Show("Please enter loading port");
            //else if (combo_discharge.Text == "")
            //    MessageBox.Show("Please enter Discharge port");
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
                bool IsWrongDate = false;


                //if (textBuyerOrderNo.Text != "")
                //{
                //    if (dateTimeBuyerOrderNo.Text == "01/01/1900" || dateTimeBuyerOrderNo.Text == "")
                //        IsWrongDate = true;
                //}

                if (!IsValidBuyerName)
                    MessageBox.Show(" Invalid Buyer Name");
                //else if (IsWrongDate)
                //    MessageBox.Show("Buyer Order Date is not Correct Format");
                else
                {
                    if (!FrmMain.IsUpdateExportInvoice)
                        MessageBox.Show("You do not have right to update Export Invoice");
                    else
                    {
                        double total = 0;
                        double ChangeGrossWt = 0;
                        bool IsWt = false;

                        try
                        {
                            calc1();
                            // total = Calculation(7, 8, 9);
                            //IsWt = WeightDifferences(10, 11);
                            string InsuranceApp = "";
                            string Insurance = "";

                            if (textSpDuty1Amt.Text == "")
                                textSpDuty1Amt.Text = "0";
                            if (textSpDuty2Amt.Text == "")
                                textSpDuty2Amt.Text = "0";
                            if (textSpDuty3Amt.Text == "")
                                textSpDuty3Amt.Text = "0";

                            string iscommercial = "";
                            string commercialinvoice = "";

                            // total = Math.Round(total, 0);


                            double Excise = 0;
                            double Educess = 0;
                            double HiEducess = 0;
                            double Vat = 0;
                            double AddVat = 0;

                            Friegth = Convert.ToDouble(textFrieght.Text);

                            double TempTotal = 0;
                            string commodity = "";

                            for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                            {
                                if (dataGridView1.Rows[i].Cells[0].FormattedValue.ToString() != "" &&
                                    dataGridView1.Rows[i].Cells[1].FormattedValue.ToString() != "" &&
                                    dataGridView1.Rows[i].Cells[4].FormattedValue.ToString() != "" &&
                                    dataGridView1.Rows[i].Cells[6].FormattedValue.ToString() != "")
                                {

                                    TempTotal += Convert.ToDouble(dataGridView1.Rows[i].Cells[9].Value.ToString());
                                    TempTotal = Math.Round(TempTotal, 0);
                                    commodity += dataGridView1.Rows[i].Cells[3].Value.ToString() + ",";
                                }
                            }

                            bool IsAlreadyPrint = false;
                            Database.OpenConnection(Utility.DespacthConnectionString);
                            Database.myreader = Database.GetExecuteReaderCommand("select * from PrintCommercialExTaxInvoice where invoice_no = '"
                                + textInvNo.Text + "' ");
                            if (Database.myreader.Read())
                                IsAlreadyPrint = true;
                            Database.myreader.Close();

                            if (IsAlreadyPrint)
                                MessageBox.Show("Following Invoice No is already Printed you cannot change Invoice");
                            else if (TotalCount > 0)
                            {
                                if (Database.OpenConnection(Utility.DespacthConnectionString))
                                {
                                    DialogResult drs = MessageBox.Show("Do you want to update Export Invoice", "Updation Export Invoice"
                                                     , MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                                    if (drs.CompareTo(DialogResult.Yes) == 0)
                                    {
                                        Database.BeginTransaction();
                                        double DouTotal = Convert.ToDouble(textGrdTotal.Text);
                                        DouTotal = Math.Round(DouTotal, 0);
                                        Int64 Totals = Convert.ToInt64(DouTotal);
                                        //Update EPCG And Advacnce License
                                        //string EPCGNo = "";
                                        //string AdvNo = "";
                                        //double Amount = 0;
                                        //double GrossWt = 0;


                                        //string isdirectexport = "";
                                        //string packinglistno = "";
                                        //string contno = "";
                                        //string bottlesealno = "";
                                        //string packages = "";


                                        string EpCGNO = "";
                                        //string EPCGNODate = "";
                                        string Advlicenseno = "";
                                        //string AdvlicensenoDate = "";
                                        string Utibond = "";
                                        //string UtiBondDate = "";
                                        string isepcg = "no";
                                        string isadvlic = "no";
                                        string isimport = "no";
                                        //string iscenvat = "no";
                                        string isdfia = "no";

                                        if (comboExportUnder.SelectedIndex == 1) //UTBond
                                            Utibond = textUT1BondNo.Text;
                                        else
                                            Utibond = "";
                                        textAmntInRS.Text = Math.Round(Convert.ToDouble(textAmntInRS.Text), 2).ToString();

                                        Database.GetExecuteNonQueryCommand("Update Packinglist set IE_code='" + textIE_code.Text + "',Invoice_dt='" + dateTimePickerInvDate.Value.ToString(DespatchCommonFunction.ERPDBDateSettings())
                                            + "',Buyers_od_no='" + textBuyerOrderNo.Text + "' ,Buyers_od_dt='" + dateTimeBuyerOrderNo.Value.ToString(DespatchCommonFunction.ERPDBDateSettings()) + "',country_desti='"
                                            + textDestinationPlace.Text + "', Buyers='" + comboBuyerName.Text + "',terms_payement='"
                                            + textTermsofPayment.Text + "',pre_carrige='" + textprecarriage.Text + "',place_receipt='" + textreceipt_precarrier.Text + "',vessl_flteno='"
                                            + textvessel_no.Text + "',port_loading='" + combo_loading.Text + "',port_discharge='" + combo_discharge.Text + "',place_delivery='"
                                            + textplace_delivery.Text + "',Hc_containerno='" + texthc_containerno.Text + "',line_seal='" + textline_Seal.Text + "',cusom_seal_no='"
                                            + textcustom_sealno.Text + "',gross_wt=" + textgrosswt.Text + ",tare_wt=" + texttarewt.Text + ",pallet_wt=" + textpaletwt.Text + ",tot_net_wt=" + textntwt.Text
                                            + ",yr='" + textARESuffix.Text + "',remarks='" + textRemarks.Text + "',consignee='" + comboConsigneeName.Text + "',cons_city='" + comboConsigneeCity.Text
                                            + "', currancyconversion= " + textCurrencyExchangeRate.Text + ",currancy='" + comboCurrency.Text + "',cur_value='" + comboCurrencyValue.Text + "', deptt='"
                                            + comboDeptt.Text + "',CommercialAmount=" + TextFOB.Text + ",Notifier='" + textNotifier.Text + "',ContType='" + comboContType.Text + "',TotalAmt=" + textGrdTotal.Text + ",HTSCODE='"
                                            + textHeading.Text + "',commodity='" + combocommodity.Text + "',EpcgNo='" + EpCGNO + "',advno='" + Advlicenseno + "',utibondno='" + Utibond + "',isepcg='" + isepcg + "',isadvlic='"
                                            + isadvlic + "',beingexport='" + comboExportUnder.Text + "',isdfia='" + isdfia + "',isimport='" + isimport + "',exciseper=0,educessper=0,hieducessper=0,FOBValue="
                                            + TextFOB.Text + ",FrieghtAmount=" + textFrieght.Text + ",Insurance=" + textinsuranceAmnt.Text + ",Commision=0,Discount=" + textDiscount.Text + ",SpDutyAmt1="
                                            + textSpDuty1Amt.Text + ",Deduction=0,Dutydebited='" + textDutyDebited.Text + "',AMountinRupees=" + Math.Round(Convert.ToDouble(textAmntInRS.Text), 2) + ",typeofshipment='" + comboTypeOfShipment.Text
                                            + "',NatureofContract='" + comboContract1.Text + "',Authorised='AUTHORISED SIGNATORY',SpDutyRemarks1='" + textSpDutyRemarks1.Text + "',BCDPERC=" + textBCDPerc.Text + ",CVDPerc=" + textCVDPerc.Text
                                            + ",EDCessPerc=" + textEDCessPerc.Text + ",HEDCessPerc=" + textHEDcessPerc.Text + ",EDuCEssPerc=" + textEDUCESSPerc.Text + ",HeducessPerc="
                                            + textHEDUCESSPerc.Text + ",AddVatPerc=" + textAddVatPerc.Text + ",BCD=" + textBCD.Text + ",CVD=" + textCVD.Text + ",EDCess=" + textEDCESS.Text
                                            + ",HEDCess=" + textHEDCESS.Text + ",EDuCEssoel2=" + textEDUCESSOEL2.Text + ",Heducess=" + textHEDUCESS.Text + ",AddVatOel2=" + textAddVat2.Text + ",packagingdetial='"
                                            + textPackagingDetail.Text + "',deliveryterms='" + textTermsofDelivery.Text + "',cons_ntbyer='" + textConsignee.Text + "',modeoftransit='" + comboModeofTransit.Text + "',reference='"
                                            + textReferanceNo.Text + " ',remarks2='" + textRemarks2.Text + "',totalCBM=" + textFobRate.Text + ",bankdetail='" + textRateInRs.Text
                                            + "',[OpeningBalance]=" + textOpeningBalance.Text + " ,bill_leading_dt='" + datetimeBLdate.Value.ToString(DespatchCommonFunction.ERPDBDateSettings())
                                            + "',doc_dt='" + dateTimePickerDateTimeRemoval.Value.ToString(DespatchCommonFunction.ERPDBDateSettings()) + "',bill_leading_no='" + txtbillofloading.Text + "',[ClosingBalance]="
                                            + textClosingBalance.Text + ",LRNO='" + textLrNo.Text + "',VehicalNo='" + comboVehicalNo.Text + "'"
                                            + ",TransportName='" + comboTransporter.Text + "',FreightRemarks='" + TxtFreightRem.Text + "',FreightRemarks2='" + txtSeaFrieghtRem.Text + "',FreightAmt2=" + Convert.ToDouble(txtSeaFreAmt.Text) + ","
                                            + " InlandHaulageRem ='" + TxtInLandHauRem.Text + "',InlandHaulageAmt=" + Convert.ToDouble(TxtHaulageAmt.Text) + ",OtherDuty='"
                                            + TxtOtherRem.Text + "',OtherDutyAmt=" + Convert.ToDouble(TxtOthersDiscAmt.Text) + ",DiscountRem='" + txtDiscountRem.Text + "'"
                                            + ",SpDutyRemarks2='" + textSpDutyRemark2.Text + "',SpDutyAmt2=" + textSpDuty2Amt.Text + ",SpDutyRemarks3='" + textSpDutyRemarks3.Text + "',SpDutyAmt3=" + textSpDuty3Amt.Text
                                            + ",IsthirdParty='yes' ,ThirdPartyName='" + comboThirdPartyName.Text + "',manucheckbox=1,ADCODE='" + TxtADCODE.Text + "',PortCode='" + combo_loading.SelectedValue + "' where invoice_no='" + textInvNo.Text + "'");


                                        Database.GetExecuteNonQueryCommand("Update  PackingListCreditDuty set refinvno='" + textInvNo.Text + "' where (refinvno is null or refinvno='') and companyname='"
                                              + comboCompanyName.Text + "' and date <'" + dateTimePickerInvDate.Value.ToString(DespatchCommonFunction.ERPDBDateSettings()) + "'");

                                        Database.GetExecuteNonQueryCommand("delete from packingitems  where Invsubno ='" + textInvNo.Text + "'");
                                        bool isEntry = false;
                                        TotalCount = dataGridView1.Rows.Count - 1;
                                        for (int i = 0; i < TotalCount; i++)
                                        {
                                            if (dataGridView1.Rows[i].Cells[1].FormattedValue.ToString() != "")
                                            // if (Database.OpenConnection(Utility.DespacthConnectionString))
                                            {
                                                double Currency = GetCurrencyConversion();
                                                //double GPM = 0;
                                                //double GSM = 0;
                                                //double Reqwt = 0;
                                                //double NetWt = Convert.ToDouble(dataGridView1.Rows[i].Cells[10].Value.ToString());
                                                //double OrderQty = Convert.ToDouble(dataGridView1.Rows[i].Cells[7].Value.ToString());

                                                Database.myadapter = Database.GetAdapterCommand("select * from Packingitems");
                                                DataSet dataset1 = new DataSet();
                                                Database.myadapter.Fill(dataset1, "Packingitems");

                                                //insert data in database 
                                                DataRow dr = dataset1.Tables[0].NewRow();
                                                dr[0] = textInvNo.Text;

                                                if (dataGridView1.Rows[i].Cells[1].FormattedValue.ToString() != "")
                                                    dr[1] = dataGridView1.Rows[i].Cells[1].Value.ToString();

                                                if (dataGridView1.Rows[i].Cells[2].FormattedValue.ToString() != "")
                                                    dr[2] = dataGridView1.Rows[i].Cells[2].Value.ToString();

                                                if (dataGridView1.Rows[i].Cells[4].FormattedValue.ToString() != "")
                                                    dr[3] = dataGridView1.Rows[i].Cells[4].FormattedValue.ToString();

                                                if (dataGridView1.Rows[i].Cells[7].FormattedValue.ToString() != "")
                                                    dr[4] = dataGridView1.Rows[i].Cells[7].Value.ToString();

                                                if (dataGridView1.Rows[i].Cells[6].FormattedValue.ToString() != "")
                                                    dr[5] = dataGridView1.Rows[i].Cells[6].Value.ToString();

                                                if (dataGridView1.Rows[i].Cells[10].FormattedValue.ToString() != "")
                                                    dr[6] = dataGridView1.Rows[i].Cells[10].Value.ToString();
                                                if (dataGridView1.Rows[i].Cells[0].FormattedValue.ToString() != "")
                                                    dr[7] = dataGridView1.Rows[i].Cells[0].Value.ToString();



                                                if (dataGridView1.Rows[i].Cells[8].FormattedValue.ToString() != "")
                                                    dr[8] = dataGridView1.Rows[i].Cells[8].Value.ToString();

                                                dr[9] = comboCurrencyValue.Text;
                                                //if (dataGridView1.Rows[i].Cells[11].FormattedValue.ToString() != "")
                                                if (dataGridView1.Rows[i].Cells[9].FormattedValue.ToString() != "")
                                                    dr[10] = dataGridView1.Rows[i].Cells[9].FormattedValue.ToString();


                                                if (dataGridView1.Rows[i].Cells[14].FormattedValue.ToString() != "")
                                                    dr[11] = dataGridView1.Rows[i].Cells[14].FormattedValue.ToString();

                                                if (dataGridView1.Rows[i].Cells[13].FormattedValue.ToString() != "")
                                                    dr[12] = dataGridView1.Rows[i].Cells[13].FormattedValue.ToString();



                                                dr["ItemDeptt"] = dataGridView1.Rows[i].Cells[DtDeptt.Index].FormattedValue.ToString();  //commodity
                                                dr["Commodity"] = dataGridView1.Rows[i].Cells[DCommodity.Index].FormattedValue.ToString();  //commodity
                                                dr["HSNCode"] = dataGridView1.Rows[i].Cells[colHSNCode.Index].FormattedValue.ToString();
                                                dr["CGSTrate"] = dataGridView1.Rows[i].Cells[ColCGSTRate.Index].FormattedValue.ToString();
                                                dr["CGSTamt"] = dataGridView1.Rows[i].Cells[colCGSTAmt.Index].FormattedValue.ToString();
                                                dr["SGSTRate"] = dataGridView1.Rows[i].Cells[ColSGSTRate.Index].FormattedValue.ToString();
                                                dr["SGSTAmt"] = dataGridView1.Rows[i].Cells[colSGSTAmt.Index].FormattedValue.ToString();
                                                dr["IGSTrate"] = dataGridView1.Rows[i].Cells[colIGSTRate.Index].FormattedValue.ToString();
                                                dr["IGSTAmt"] = dataGridView1.Rows[i].Cells[ColIGSTAmt.Index].FormattedValue.ToString();


                                                dataset1.Tables[0].Rows.Add(dr);
                                                Database.mycmdBuilder = Database.GetCmdBuilderCommand(Database.myadapter);
                                                int x = Database.myadapter.Update(dataset1.Tables[0]);
                                                isEntry = true;
                                            }
                                        }

                                        Database.CommitTransaction();
                                        if (isEntry)
                                            MessageBox.Show("Invoice No " + textInvNo.Text + " is updated succesfully");

                                        //Update Pending Limit from Advance License
                                        if (TotalCount == 1)
                                        {
                                            if (checkImport.Checked)
                                            {
                                                bool IsInv = false;
                                                //   if (Database.OpenConnection(Utility.DespacthConnectionString))
                                                {
                                                    Database.myreader = Database.GetExecuteReaderCommand("select * from ExportContMaterial where ExportInvNo = '"
                                                      + textInvNo.Text + "' and yr ='" + textARESuffix.Text + "' and companyname = '" + comboCompanyName.Text + "'");
                                                    if (Database.myreader.Read())
                                                        IsInv = true;
                                                    Database.myreader.Close();
                                                    //  Database.Closeconnection();
                                                }
                                                if (IsInv)
                                                    Update_ValueInExportCntMaterial(textInvNo.Text, textARESuffix.Text);
                                                else
                                                    InsertValueInExportCntMaterial(textInvNo.Text, textARESuffix.Text);
                                            }
                                        }
                                        Utility.UserInformation("Update", "Export Invoice " + textInvNo.Text + " is updated successfully");

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
                }
            }
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            //frmPrintCustomReport_OEL2 frm = new frmPrintCustomReport_OEL2();
            //frm.Show();
            TabClass.OpenForm(new frmPrintCustomReport_OEL2());
        }

        private void GettingAdvLic()
        {
            if (Database.OpenConnection(Utility.DespacthConnectionString))
            {
                SqlDataReader myreader = Database.GetExecuteReaderCommand("select * from advLicense");
                while (myreader.Read())
                    comboAdvance.Items.Add(myreader["AdvLicenseNo"].ToString());
                myreader.Close();

                if (comboAdvance.Items.Count > 0)
                    comboAdvance.SelectedIndex = 0;
                Database.Closeconnection();
            }
        }

        //private void UpdateAdvLic_PendingLimit(string LicenseNo, double Limit)
        //{
        //    if (Database.OpenConnection(Utility.DespacthConnectionString))
        //    {
        //        double PendingLimit = 0;
        //        double TotalLimit = 0;
        //        Database.myreader = Database.GetExecuteReaderCommand("select * from advLicense where AdvLicenseNo = '"
        //                + LicenseNo + "' and companyname = '" + comboCompanyName.Text + "'");
        //        if (Database.myreader.Read())
        //        {
        //            PendingLimit = Convert.ToDouble(Database.myreader["PendingLimit"].ToString());
        //            TotalLimit = Convert.ToDouble(Database.myreader["Limit"].ToString());
        //        }

        //        Database.myreader.Close();

        //        Limit = Limit / 1000;


        //        if ((PendingLimit - Limit) >= 0)
        //        {
        //            Database.GetExecuteNonQueryCommand("update advLicense set PendingLimit = " + (PendingLimit - Limit)
        //             + " where AdvLicenseNo = '" + LicenseNo + "' and companyname = '" + comboCompanyName.Text + "'");
        //        }
        //        //   Database.Closeconnection();
        //    }
        //}




        private bool CheckApplicabilityEPCG_PendingLimit(string LicenseNo, double LimitRs)
        {
            if (Database.OpenConnection(Utility.DespacthConnectionString))
            {
                double PendingLimit = 0;
                double Limits = 0;
                Database.myreader = Database.GetExecuteReaderCommand("select * from EPCGMaster where EPCGLicNo = '"
                        + LicenseNo + "' and companyname = '" + comboCompanyName.Text + "'");
                if (Database.myreader.Read())
                {
                    PendingLimit = Convert.ToDouble(Database.myreader["PendingLimit"].ToString());
                    Limits = Convert.ToDouble(Database.myreader["Limit"].ToString());
                }
                Database.myreader.Close();
                Database.Closeconnection();

                return true;

            }
            else
                return false;
        }


        private bool CheckApplicabilityAdvLic_PendingLimit(string LicenseNo, double Limit)
        {
            if (Database.OpenConnection(Utility.DespacthConnectionString))
            {
                double PendingLimit = 0;
                double Limits = 0;
                Database.myreader = Database.GetExecuteReaderCommand("select * from advLicense where AdvLicenseNo = '"
                        + LicenseNo + "' and companyname = '" + comboCompanyName.Text + "'");
                if (Database.myreader.Read())
                {
                    PendingLimit = Convert.ToDouble(Database.myreader["PendingLimit"].ToString());
                    Limits = Convert.ToDouble(Database.myreader["Limit"].ToString());
                }
                Database.myreader.Close();
                Database.Closeconnection();

                return true;

            }
            else
                return false;
        }


        private void CalculateMaterialWt()
        {
            if (Database.OpenConnection(Utility.DespacthConnectionString))
            {

                double NetWt = 0;

                NetWt += Convert.ToDouble(textntwt.Text);
                bool IsMatch = false;
                int IndexNo = 0;
                ArrListSionNo.Clear();
                ArrListImportItem.Clear();
                ArrListImportItemWt.Clear();
                ArrListImportItemUnit.Clear();

                Database.myreader = Database.GetExecuteReaderCommand("select * from AdvLicItem where ExportItem = '"
                                                         + combocommodity.Text
                                                         + "' and advlicense = '" + comboAdvance.Text
                                                         + "' and companyname = '" + comboCompanyName.Text + "'");
                while (Database.myreader.Read())
                {
                    IsMatch = false;
                    IndexNo = 0;
                    ExportITCHSNo = Database.myreader[0].ToString();

                    for (int j = 0; j < ArrListImportItem.Count; j++)
                    {
                        if (ArrListImportItem[j].ToString() == Database.myreader[4].ToString())
                        {
                            IsMatch = true;
                            IndexNo = j;
                            break;
                        }
                    }

                    if (IsMatch)
                    {
                        double ratio = Convert.ToDouble(Database.myreader[5].ToString());
                        double InputAllowed = Convert.ToDouble(Database.myreader[6].ToString());
                        double TotalWt = (ratio * InputAllowed * NetWt) / 100;
                        double PreviousWt = Convert.ToDouble(ArrListImportItemWt[IndexNo]);
                        double TotalWts = TotalWt + PreviousWt;
                        ArrListImportItemWt.RemoveAt(IndexNo);
                        ArrListImportItemWt.Insert(IndexNo, TotalWts);
                    }
                    else
                    {
                        ArrListSionNo.Add(Database.myreader[1].ToString());
                        ArrListImportItem.Add(Database.myreader[4].ToString());
                        ArrListImportItemUnit.Add("KGS");
                        double ratio = Convert.ToDouble(Database.myreader[5].ToString());
                        double InputAllowed = Convert.ToDouble(Database.myreader[6].ToString());
                        double TotalWt = (ratio * InputAllowed * NetWt) / 100;
                        ArrListImportItemWt.Add(TotalWt);
                    }
                }
                Database.myreader.Close();
                Database.Closeconnection();
            }

        }

        private void Update_ValueInExportCntMaterial(string InvNo, string yr)
        {
            //CalculateMaterialWt();
            if (Database.OpenConnection(Utility.DespacthConnectionString))
            {
                int srno = 0;

                Database.GetExecuteNonQueryCommand("delete from ExportContMaterial where ExportInvNo = '" + InvNo
                    + "' and yr = '" + yr + "' and companyname = '" + comboCompanyName.Text + "'");
                Database.myreader = Database.GetExecuteReaderCommand("select SrNo from ExportContMaterial order by srno");
                while (Database.myreader.Read())
                    srno = Convert.ToInt32(Database.myreader[0].ToString());
                Database.myreader.Close();

                Database.myadapter = Database.GetAdapterCommand("select * from ExportContMaterial");
                DataSet dataset1 = new DataSet();
                Database.myadapter.Fill(dataset1);

                for (int i = 0; i < ArrListSionNo.Count; i++)
                {
                    srno = srno + 1;
                    DataRow dr = dataset1.Tables[0].NewRow();
                    dr[0] = ArrListImportItem[i].ToString();
                    dr[1] = ArrListSionNo[i].ToString();
                    dr[2] = ArrListImportItemUnit[i].ToString();
                    dr[3] = ArrListImportItemWt[i].ToString();
                    dr[4] = InvNo;
                    dr[5] = srno;
                    dr[6] = ExportITCHSNo;
                    dr[7] = yr;
                    dr[8] = comboCompanyName.Text;

                    dataset1.Tables[0].Rows.Add(dr);
                    Database.mycmdBuilder = Database.GetCmdBuilderCommand(Database.myadapter);
                    int x = Database.myadapter.Update(dataset1.Tables[0]);
                }

                Database.Closeconnection();
            }
        }

        private void comboCommodity_SelectedIndexChanged(object sender, EventArgs e)
        {

        }



        private void textEntryNo_KeyPress(object sender, KeyPressEventArgs e)
        {
            Utility.AllowDecimalOnly(e);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            comboBuyerName.Items.Clear();


            if (Database.OpenConnection(Utility.DespacthConnectionString))
            {
                Database.myreader = Database.GetExecuteReaderCommand("Select * from CompanyMaster order by CompanyName");
                while (Database.myreader.Read())
                    comboBuyerName.Items.Add(Database.myreader[0].ToString());
                Database.myreader.Close();
                Database.Closeconnection();
            }
            if (comboBuyerName.Items.Count > 0)
                comboBuyerName.SelectedIndex = 0;

        }

        private void comboCompName_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {

                if (Database.OpenConnection(Utility.DespacthConnectionString))
                {
                    Database.myreader = Database.GetExecuteReaderCommand("select terms,companycode from companymaster where companyname = '"
                     + comboBuyerName.Text + "'");
                    if (Database.myreader.Read())
                    {
                        textTermsofPayment.Text = Database.myreader[0].ToString();
                        CompanyCode = Database.myreader[1].ToString();
                    }
                    Database.myreader.Close();

                    comboConsigneeName.Items.Clear();
                    comboConsigneeName.Text = "";
                    comboConsigneeCity.Items.Clear();
                    comboConsigneeCity.Text = "";
                    textConsignee.Text = "";

                    Database.myreader = Database.GetExecuteReaderCommand("select Distinct(Consigneename) from consignee where  companyCode = '"
                        + CompanyCode + "'");
                    while (Database.myreader.Read())
                        comboConsigneeName.Items.Add(Database.myreader[0].ToString());
                    Database.myreader.Close();
                    Database.Closeconnection();
                    if (comboConsigneeName.Items.Count > 0)
                        comboConsigneeName.SelectedIndex = 0;
                }

            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
            }
        }

        private void textInvNo_TextChanged(object sender, EventArgs e)
        {

        }

        private void comboTaxApp_SelectedIndexChanged(object sender, EventArgs e)
        {

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

        private void dataGrid1_CurrentCellChanged(object sender, EventArgs e)
        {
            if (!IsInit)
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


                    if (dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[2].FormattedValue.ToString() != "" &&
                        dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[3].FormattedValue.ToString() != "" &&
                        dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[7].FormattedValue.ToString() != "" &&
                        dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[6].FormattedValue.ToString() != "" &&
                        dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[8].FormattedValue.ToString() != "")
                    {
                        dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[9].Value = Convert.ToString(Convert.ToDouble(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[7].Value.ToString())
                            * Convert.ToDouble(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[8].Value.ToString()));
                        total += Convert.ToDouble(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[9].Value.ToString());
                    }

                    double NetWt = Convert.ToDouble(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[10].FormattedValue.ToString());

                }

                catch (Exception ex)
                {
                    ErrorMessageBox.Show(ex);
                }
            }
        }

        private void InsertValueInExportCntMaterial(string InvNo, string yr)
        {
            CalculateMaterialWt();
            if (Database.OpenConnection(Utility.DespacthConnectionString))
            {
                int srno = 0;
                Database.myreader = Database.GetExecuteReaderCommand("select top 1 SrNo from CommercialContMaterial order by srno desc");
                if (Database.myreader.Read())
                    srno = Convert.ToInt32(Database.myreader[0].ToString());
                Database.myreader.Close();

                Database.myadapter = Database.GetAdapterCommand("select * from CommercialContMaterial");
                DataSet dataset1 = new DataSet();
                Database.myadapter.Fill(dataset1);

                for (int i = 0; i < ArrListSionNo.Count; i++)
                {
                    srno = srno + 1;
                    DataRow dr = dataset1.Tables[0].NewRow();
                    dr[0] = ArrListImportItem[i].ToString();
                    dr[1] = ArrListSionNo[i].ToString();
                    dr[2] = ArrListImportItemUnit[i].ToString();
                    dr[3] = ArrListImportItemWt[i].ToString();
                    dr[4] = InvNo;
                    dr[5] = srno;
                    dr[6] = ExportITCHSNo;
                    dr[7] = yr;
                    dr[8] = comboCompanyName.Text;

                    dataset1.Tables[0].Rows.Add(dr);
                    Database.mycmdBuilder = Database.GetCmdBuilderCommand(Database.myadapter);
                    int x = Database.myadapter.Update(dataset1.Tables[0]);
                }

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
                    string strsql = "select distinct City from Consignee where consigneename = '" + comboConsigneeName.Text + "' and companycode ='" + CompanyCode + "'";
                    DespatchCommonFunction.Loadcombo(comboConsigneeCity, strsql, "City", "City", Utility.DespacthConnectionString);
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
                    Database.Closeconnection();


                }
            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
            }

        }

        private void checkCENVAT_CheckedChanged(object sender, EventArgs e)
        {
            if (checkCENVAT.Checked)
                groupCenvat.Visible = true;
            else
                groupCenvat.Visible = false;
        }

        private void dataGridView1_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {

        }

        void tb_KeyPress(object sender, KeyPressEventArgs e)
        {

            if ((!(Char.IsDigit(e.KeyChar))) && (!(e.KeyChar == '\b')) && (!(e.KeyChar == '.')))
                e.Handled = true;
        }


        private void dataGridView1_CurrentCellChanged(object sender, EventArgs e)
        {
            try
            {
                if (dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[7].FormattedValue.ToString().Trim() != ""
                     && dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[8].FormattedValue.ToString().Trim() != "")
                {
                    dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[9].Value =
                     Convert.ToDouble(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[7].Value.ToString()) *
                     Convert.ToDouble(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[8].Value.ToString());
                }



            }
            catch (Exception ex)
            {
                ex.ToString();
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void textItem_MouseClick(object sender, MouseEventArgs e)
        {
            MessageBox.Show("click");
        }


        private void textInvNo_MouseClick(object sender, MouseEventArgs e)
        {

        }

        private void checkSelfSealing_CheckedChanged(object sender, EventArgs e)
        {
            if (checkSelfSealing.Checked)
            {
                if (comboCompanyName.Text == "Plastene India Limited (Unit -II)")
                {
                    if (checkDirectExport.Checked)
                    {
                        textAREDetail.Text = "CERTIFICATE  OF  SELF SEALING &  SELF CERTIFY SELF SEALING CERTIFECATION NO.CONTAINER MADE FOR EXPORT PERMISSION NO.VIII/48-345/SS/ MP&SEZ/11-12 DT:02.08.2011";
                        textAREDetails1.Text = "This is certify that the description and value of the Goods covered by this invoice & ARE-1 have been checked by me & goods have been packed and Sealed under my supervision ";
                        textPackages.Text = "self supervision";
                    }
                    else
                    {
                        textAREDetail.Text = "CERTIFICATE  OF  SELF SEALING &  SELF CERTIFY";
                        textAREDetails1.Text = "This is certify that the description and value of the Goods covered by this invoice & ARE-1 have been checked by me & goods have been packed and Sealed under my supervision ";
                        textPackages.Text = "self supervision";
                    }
                }
                else
                {
                    textAREDetail.Text = "CERTIFICATE  OF  SELF SEALING &  SELF CERTIFY";
                    textAREDetails1.Text = "This is certify that the description and value of the Goods covered by this invoice & ARE-1 have been checked by me & goods have been packed and Sealed under my supervision ";
                    textPackages.Text = "self supervision";
                }
            }
            else
            {
                textAREDetail.Text = "";
                textAREDetails1.Text = "";
                textPackages.Text = "";
            }
        }


        private void button2_Click(object sender, EventArgs e)
        {
            calc1();
        }
        void calc1()
        {
            try
            {
                if (comboContract1.Text == "")
                {
                    MessageBox.Show("Please Select proper nature of contract");
                }
                else
                {
                    double total = 0;
                    double fobvalue = 0;
                    bool isallow = true;
                    double ntwt = 0;
                    double grwt = 0;
                    double UOMwt = 0;

                    for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                    {
                        if (dataGridView1.Rows[i].Cells[3].FormattedValue.ToString() == "")
                        {
                            MessageBox.Show("Please Select proper itemdeptt ");
                            isallow = false;
                            break;

                        }

                        if (dataGridView1.Rows[i].Cells[9].FormattedValue.ToString() != "")
                        {
                            total = total + Convert.ToDouble(dataGridView1.Rows[i].Cells[9].FormattedValue);
                            ntwt += Convert.ToDouble(dataGridView1.Rows[i].Cells[10].FormattedValue);
                            //grwt += Convert.ToDouble(dataGridView1.Rows[i].Cells[11].FormattedValue);
                            UOMwt += Convert.ToDouble(dataGridView1.Rows[i].Cells[7].FormattedValue);
                        }
                    }
                    TotalUOM = UOMwt;

                    if (isallow)
                    {

                        if (comboContract1.Text == "CIF")
                        {
                            textinsuranceAmnt.Text = Convert.ToString(Math.Round((total * Convert.ToDouble(textInsurancePerc.Text) / 100), 2));
                            fobvalue = total + Convert.ToDouble(textFrieght.Text);//+ Convert.ToDouble(textinsuranceAmnt.Text)
                        }
                        else if (comboContract1.Text == "CF")
                        {
                            textinsuranceAmnt.Text = "0";
                            fobvalue = total + Convert.ToDouble(textFrieght.Text);
                        }
                        else if (comboContract1.Text == "CI")
                        {
                            textFrieght.Text = "0";
                            textinsuranceAmnt.Text = Convert.ToString(Math.Round((total * Convert.ToDouble(textInsurancePerc.Text) / 100), 2));
                            fobvalue = total + Convert.ToDouble(textinsuranceAmnt.Text);
                        }
                        else if (comboContract1.Text == "FOB")
                        {
                            fobvalue = total;// +Convert.ToDouble(textFrieght.Text);
                        }
                        else if (comboContract1.Text == "DDU")
                        {
                            textinsuranceAmnt.Text = Convert.ToString(Math.Round((total * Convert.ToDouble(textInsurancePerc.Text) / 100), 2));
                            fobvalue = total + Convert.ToDouble(textFrieght.Text) - Convert.ToDouble(textinsuranceAmnt.Text);
                        }
                        else if (comboContract1.Text == "DDP")
                        {
                            fobvalue = total + Convert.ToDouble(textFrieght.Text) + +Convert.ToDouble(textinsuranceAmnt.Text);
                        }

                        if (textDiscount.Text != "")
                            fobvalue = fobvalue - Convert.ToDouble(textDiscount.Text);

                        if (textSpDuty1Amt.Text != "")
                            fobvalue = fobvalue + Convert.ToDouble(textSpDuty1Amt.Text);

                        if (textSpDuty2Amt.Text != "")
                            fobvalue = fobvalue + Convert.ToDouble(textSpDuty2Amt.Text);

                        if (textSpDuty3Amt.Text != "")
                            fobvalue = fobvalue + Convert.ToDouble(textSpDuty3Amt.Text);


                        textGrdTotal.Text = total.ToString();// fobvalue.ToString();
                        textFobRate.Text = Math.Round((fobvalue / UOMwt), 2).ToString();
                        textntwt.Text = ntwt.ToString();
                        if (textgrosswt.Text != "")
                            texttarewt.Text = (Convert.ToDouble(textgrosswt.Text) - Convert.ToDouble(textntwt.Text)).ToString();
                        if (comboContract1.Text == "DDU")
                        {
                            TextFOB.Text = Convert.ToString(Convert.ToDouble(fobvalue) - Convert.ToDouble(txtSeaFreAmt.Text) -
                                                Convert.ToDouble(TxtHaulageAmt.Text));

                            //textgrosswt.Text = grwt.ToString();
                            //texttarewt.Text = (grwt - ntwt).ToString();

                            //textAmntInRS.Text = (Math.Round(((Convert.ToDouble(TextFOB.Text) - Convert.ToDouble(textFrieght.Text)) * Convert.ToDouble(textCurrencyExchangeRate.Text)), 2)).ToString();
                            textAmntInRS.Text = (Math.Round(((Convert.ToDouble(TextFOB.Text)) * Convert.ToDouble(textCurrencyExchangeRate.Text)), 2)).ToString();
                            textAmntInRS.Text = (Math.Round(Convert.ToDouble(textAmntInRS.Text), 0)).ToString();
                        }
                        else if (comboContract1.Text == "DDP")
                        {
                            TextFOB.Text = Convert.ToString(Convert.ToDouble(fobvalue) - Convert.ToDouble(textFrieght.Text) -
                                                Convert.ToDouble(textinsuranceAmnt.Text) - Convert.ToDouble(txtSeaFreAmt.Text) -
                                                Convert.ToDouble(TxtHaulageAmt.Text) - Convert.ToDouble(TxtOthersDiscAmt.Text));

                            //textAmntInRS.Text = (Math.Round(((Convert.ToDouble(TextFOB.Text) - Convert.ToDouble(textFrieght.Text)) * Convert.ToDouble(textCurrencyExchangeRate.Text)), 2)).ToString();
                            textAmntInRS.Text = (Math.Round(((Convert.ToDouble(TextFOB.Text)) * Convert.ToDouble(textCurrencyExchangeRate.Text)), 2)).ToString();
                            textAmntInRS.Text = (Math.Round(Convert.ToDouble(textAmntInRS.Text), 0)).ToString();
                        }
                        else if (comboContract1.Text == "CIF")
                        {
                            TextFOB.Text = "";
                            TextFOB.Text = (Convert.ToDouble(fobvalue) - Convert.ToDouble(txtSeaFreAmt.Text) - Convert.ToDouble(textinsuranceAmnt.Text)).ToString();
                            textAmntInRS.Text = (Math.Round((Convert.ToDouble(TextFOB.Text) * Convert.ToDouble(textCurrencyExchangeRate.Text)), 2)).ToString();
                            textAmntInRS.Text = (Math.Round(Convert.ToDouble(textAmntInRS.Text), 0)).ToString();
                        }
                        else
                        {
                            TextFOB.Text = "";
                            //TextFOB.Text = (Convert.ToDouble(fobvalue) - Convert.ToDouble(txtSeaFreAmt.Text)).ToString();
                            TextFOB.Text = (Convert.ToDouble(fobvalue) - Convert.ToDouble(txtSeaFreAmt.Text) - Convert.ToDouble(TxtHaulageAmt.Text) - Convert.ToDouble(TxtOthersDiscAmt.Text)).ToString();
                            textAmntInRS.Text = (Math.Round((Convert.ToDouble(TextFOB.Text) * Convert.ToDouble(textCurrencyExchangeRate.Text)), 2)).ToString();
                            textAmntInRS.Text = (Math.Round(Convert.ToDouble(textAmntInRS.Text), 0)).ToString();
                        }
                        textRateInRs.Text = (Math.Round((Convert.ToDouble(textAmntInRS.Text) / UOMwt), 2)).ToString();
                        txtFOBV.Text = TextFOB.Text;
                        double TempAmnt = Convert.ToDouble(textAmntInRS.Text.ToString());
                        Calc(TempAmnt);

                        //    textFOBValue.BackColor = Color.Red;
                    }
                }

            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
            }

        }

        void Calc(double Amount)
        {
            try
            {
                double pendinglimit = 0;
                double BCD = Math.Round((Convert.ToDouble(textBCDPerc.Text) * Amount) / 100, 0);
                double CVD = Math.Round((Convert.ToDouble(textCVDPerc.Text) * (Amount + BCD)) / 100, 0);
                double EDCESS = Math.Round((Convert.ToDouble(textEDCessPerc.Text) * CVD) / 100, 0);
                double HEDCESS = Math.Round((Convert.ToDouble(textHEDcessPerc.Text) * CVD) / 100, 0);
                double EDUCESSOEL2 = Math.Round((Convert.ToDouble(textEDUCESSPerc.Text) * (BCD + CVD + EDCESS + HEDCESS)) / 100, 0);
                double HEDUCESS = Math.Round((Convert.ToDouble(textHEDUCESSPerc.Text) * (BCD + CVD + EDCESS + HEDCESS)) / 100, 0);
                double ADDVAT = Math.Round((Convert.ToDouble(textAddVatPerc.Text) * (BCD + CVD + EDCESS + HEDCESS + EDUCESSOEL2 + HEDUCESS + Amount)) / 100, 0);
                textBCD.Text = Convert.ToString(BCD);
                textCVD.Text = Convert.ToString(CVD);
                textEDCESS.Text = Convert.ToString(EDCESS);
                textHEDCESS.Text = Convert.ToString(HEDCESS);
                textEDUCESSOEL2.Text = Convert.ToString(EDUCESSOEL2);
                textHEDUCESS.Text = Convert.ToString(HEDUCESS);
                textAddVat2.Text = Convert.ToString(ADDVAT);
                string Utibond = "";

                if (comboExportUnder.SelectedIndex == 1) //UTBond
                    Utibond = textUT1BondNo.Text;
                else
                    Utibond = "";
                if (Utibond != "")
                {
                    if (Database.OpenConnection(Utility.DespacthConnectionString))
                    {
                        DateTime dtBondDt = Convert.ToDateTime("2013-03-31 00:00:00.000");

                        Database.myreader = Database.GetExecuteReaderCommand("select pendinglimit,BondDate from UTbond where BondNo = '"
                        + Utibond + "' and expirydate >= '"
                        + dateTimePickerInvoice.Value + "' and companyname = '"
                        + comboCompanyName.Text + "'");
                        if (Database.myreader.Read())
                        {
                            textOpeningBalance.Text = Database.myreader[0].ToString();
                            dtBondDt = Convert.ToDateTime(Database.myreader[1]);
                        }
                        Database.myreader.Close();
                        string Condition = "";
                        if (dtBondDt < Convert.ToDateTime("2013-03-31 00:00:00.000"))
                        {
                            Condition = "2013-03-31 00:00:00.000";
                        }
                        else
                        {
                            Condition = dtBondDt.ToString("yyyy-MM-dd 00:00:00.000");
                        }

                        if (textInvNo.Text.Length == 0)
                        {
                            Database.myreader = Database.GetExecuteReaderCommand("select  isnull(sum(dutydebited),0) from packinglist where utibondno='" + Utibond + "' and   invoice_dt>'" + Condition + "'  and companyname='" + comboCompanyName.Text + "' and  (Invoice_no<'" + textInvNo.Text + "' or isnull(Invoice_no,'')<>'') "); //Invoice_no!='" + textInvNo.Text + "'"
                            if (Database.myreader.Read())
                            {
                                textOpeningBalance.Text = (Convert.ToDouble(textOpeningBalance.Text.ToString()) - Convert.ToDouble(Database.myreader[0].ToString())).ToString();
                            }
                            Database.myreader.Close();

                            Database.myreader = Database.GetExecuteReaderCommand("select  isnull(sum(creditamount),0) from PackingListCreditDuty where utbondno='" + Utibond + "' and  date>'" + Condition + "' and companyname='" + comboCompanyName.Text + "' and  (refinvno <'" + textInvNo.Text + "' or isnull(refinvno ,'')<>'')"); //refinvno !='" + textInvNo.Text + "'"
                            if (Database.myreader.Read())
                            {
                                textOpeningBalance.Text = (Convert.ToDouble(textOpeningBalance.Text.ToString()) + Convert.ToDouble(Database.myreader[0].ToString())).ToString();
                            }
                            Database.myreader.Close();
                        }
                        else
                        {
                            Database.myreader = Database.GetExecuteReaderCommand("select  isnull(sum(dutydebited),0) from packinglist where utibondno='" + Utibond + "' and   invoice_dt>'" + Condition + "'  and companyname='" + comboCompanyName.Text + "' and  Invoice_no!='" + textInvNo.Text + "'"); //
                            if (Database.myreader.Read())
                            {
                                textOpeningBalance.Text = (Convert.ToDouble(textOpeningBalance.Text.ToString()) - Convert.ToDouble(Database.myreader[0].ToString())).ToString();
                            }
                            Database.myreader.Close();

                            Database.myreader = Database.GetExecuteReaderCommand("select  isnull(sum(creditamount),0) from PackingListCreditDuty where utbondno='" + Utibond + "' and  date>'" + Condition + "' and companyname='" + comboCompanyName.Text + "' and  refinvno !='" + textInvNo.Text + "'"); //refinvno !='" + textInvNo.Text + "'"
                            if (Database.myreader.Read())
                            {
                                textOpeningBalance.Text = (Convert.ToDouble(textOpeningBalance.Text.ToString()) + Convert.ToDouble(Database.myreader[0].ToString())).ToString();
                            }
                            Database.myreader.Close();
                        }

                        pendinglimit = Convert.ToDouble(textDutyDebited.Text);
                        textDutyDebited.Text = Convert.ToString(BCD + CVD + EDCESS + HEDCESS + EDUCESSOEL2 + HEDUCESS + ADDVAT);

                        textClosingBalance.Text = (Convert.ToDouble(textOpeningBalance.Text.ToString()) - Convert.ToDouble(textDutyDebited.Text)).ToString();
                    }

                }
            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
            }
        }
        public string GenerateInvno(string companyname, string yr, ref  int InvoiceSeries)
        {
            //int srno = 0;
            string Invno = "";
            //string temp = "";
            if (Database.OpenConnection(Utility.DespacthConnectionString))
            {

                Database.myreader = Database.GetExecuteReaderCommand("Select dispatchcode from factoryinfo where name = '"
                  + companyname + "'");
                if (Database.myreader.Read())
                    Invno = Database.myreader[0].ToString();
                Database.myreader.Close();

                Database.myreader = Database.GetExecuteReaderCommand("Select invno from packinglist where companyname = '"
                  + companyname + "' and yr = '" + textARESuffix.Text + "'  order by invno desc");
                if (Database.myreader.Read())
                    InvoiceSeries = Convert.ToInt32(Database.myreader[0].ToString());
                Database.myreader.Close();
                InvoiceSeries = InvoiceSeries + 1;
                Invno = Invno + "/EXP/" + InvoiceSeries.ToString() + "/" + yr;

            }
            return Invno;
        }

        private void textCurrencyExchangeRate_Leave(object sender, EventArgs e)
        {
            calc1();
        }

        private void textgrosswt_Leave(object sender, EventArgs e)
        {
            if (Convert.ToDouble(textgrosswt.Text) < Convert.ToDouble(textntwt.Text))
            {
                MessageBox.Show("Gross weight can not be less than net weight");
                textgrosswt.Text = "0";
            }
            else
                texttarewt.Text = (Convert.ToDouble(textgrosswt.Text) - Convert.ToDouble(textntwt.Text)).ToString();
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboExportUnder.Text == "UT-1 Bond No")
            {
                Database.OpenConnection(Utility.DespacthConnectionString);
                Database.myreader = Database.GetExecuteReaderCommand("Select * from UTBond where companyname = '"
                    + comboCompanyName.Text + "' order by BondDate");
                while (Database.myreader.Read())
                {
                    textUT1BondNo.Text = Database.myreader[1].ToString();
                }
                Database.myreader.Close();
            }
        }

        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            TabClass.OpenForm(new frmPrintCommercaiReport_OEL2());
        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            TabClass.OpenForm(new frmPrintPackingList_OEL2());
        }

        private void toolStripButton4_Click(object sender, EventArgs e)
        {
            //FrmSelfSealingDeclaration2_OEL2 frm = new FrmSelfSealingDeclaration2_OEL2();
            //frm.Show();
            TabClass.OpenForm(new FrmSelfSealingDeclaration_OEL2());
        }

        private void toolStripButton5_Click(object sender, EventArgs e)
        {
            //FrmSDFDecl_oel2 frm = new FrmSDFDecl_oel2();
            //frm.Show();
            TabClass.OpenForm(new FrmSDFDecl_oel2());
        }

        private void toolStripButton6_Click(object sender, EventArgs e)
        {
            TabClass.OpenForm(new FrmPrintAre1OEL2());
        }

        private void toolStripButton7_Click(object sender, EventArgs e)
        {
            TabClass.OpenForm(new FrmSelfSealingDeclaration_OEL2());
        }



        private void dataGridView1_CellLeave(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (e.ColumnIndex == DCommodity.Index && e.RowIndex >= 0)
                {
                    //DCommodity DCommodity
                    string Commodity = Convert.ToString(dataGridView1.Rows[e.RowIndex].Cells[DCommodity.Index].Value);
                    string THeading = "";
                    Database.OpenConnection(Utility.DespacthConnectionString);
                    Database.myreader = Database.GetExecuteReaderCommand("Select top 1 THeading,NCGSTRate,NSGSTRate,NIGSTRate from CommodityHSNGST where companyname = '"
                        + comboCompanyName.Text + "' and CommodityName='" + Commodity + "' ");
                    if (Database.myreader.Read())
                    {
                        THeading = Database.myreader[0].ToString();
                        dataGridView1.CurrentRow.Cells[colHSNCode.Index].Value = THeading;
                        dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[colHSNCode.Index].Value = THeading;

                        if (comboState.Text.ToUpper() == "Gujarat".ToUpper() && textplace_delivery.Text.Contains("KASEZ") == false)
                        {
                            dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[ColCGSTRate.Index].Value = Database.myreader["NCGSTRate"].ToString();
                            dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[ColSGSTRate.Index].Value = Database.myreader["NSGSTRate"].ToString();
                            dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[colIGSTRate.Index].Value = 0;
                        }
                        else
                        {
                            dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[ColCGSTRate.Index].Value = 0;
                            dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[ColSGSTRate.Index].Value = 0;
                            dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[colIGSTRate.Index].Value = Database.myreader["NIGSTRate"].ToString();
                        }

                    }
                    Database.myreader.Close();
                    if (dateTimePickerInvDate.Value < Convert.ToDateTime("2017-07-01 00:00:00"))
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
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show("dataGridView1_CellValueChanged :" + ex.Message);
            }
        }

        private void comboThirdPartyName_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                textConsignee.Text = "";
                comboConsigneeCity.Items.Clear();
                comboConsigneeCity.Text = "";

                using (System.Data.SqlClient.SqlConnection conn = new System.Data.SqlClient.SqlConnection(Utility.DespacthConnectionString))
                {

                    conn.Open();
                    string strsql = "select consignee,terms,Destination,typeofInvoice,Companycode from companymaster where "
                                  + " companyname = '" + comboThirdPartyName.Text + "'";
                    System.Data.SqlClient.SqlCommand command = new System.Data.SqlClient.SqlCommand(strsql, conn);
                    System.Data.SqlClient.SqlDataReader reader = command.ExecuteReader();

                    while (reader.Read())
                    {
                        textConsignee.Text = reader[0].ToString();
                        CompanyCode = reader[4].ToString();
                    }
                    reader.Close();

                    comboConsigneeName.Items.Clear();
                    comboConsigneeName.Text = "";

                    strsql = "select Distinct(Consigneename) as Consigneename from consignee where  companyCode = '" + CompanyCode + "'";

                    command = new System.Data.SqlClient.SqlCommand(strsql, conn);
                    reader = command.ExecuteReader();

                    while (reader.Read())
                    {
                        comboConsigneeName.Items.Add(Convert.ToString(reader[0]));
                    }
                    reader.Close();

                    if (comboConsigneeName.Items.Count > 0)
                        comboConsigneeName.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            try
            {
                if (textInvNo.Text.Length == 0) return;
                if (Database.OpenConnection(Utility.DespacthConnectionString))
                {

                    if (!DespatchCommonFunction.IsEwayGenerated(comboCompanyName.Text, textARESuffix.Text, textInvNo.Text))
                    {
                        Database.GetExecuteNonQueryCommand("INSERT INTO EWayBillMaster(CompanyName,InvNo,Yr,EWayNo) VALUES('" +
                            comboCompanyName.Text + "','" + textInvNo.Text + "','" + textARESuffix.Text + "','" + TxtEwayNo.Text + "')");
                        MessageBox.Show("E-Way Bill No update for Inv No :" + textInvNo.Text);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Problem with Updating E-Way Bill No");
            }
        }

        private void dataGridView1_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (dataGridView1.CurrentCell.ColumnIndex == DCommodity.Index)
            {
                if (Database.OpenConnection(Utility.DespacthConnectionString))
                {
                    string Commodity = Convert.ToString(dataGridView1.Rows[dataGridView1.CurrentRow.Index].Cells[3].Value);
                    string THeading = "";
                    Database.OpenConnection(Utility.DespacthConnectionString);
                    Database.myreader = Database.GetExecuteReaderCommand("Select top 1 THeading,NCGSTRate,NSGSTRate,NIGSTRate from Commodity where companyname = '"
                        + comboCompanyName.Text + "' and CommodityName='" + Commodity + "' ");
                    if (Database.myreader.Read())
                    {
                        THeading = Database.myreader[0].ToString();
                        dataGridView1.CurrentRow.Cells[colHSNCode.Index].Value = THeading;
                        dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[colHSNCode.Index].Value = THeading;
                        dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[colIGSTRate.Index].Value = Utility.SafeConvertToDouble(Database.myreader["NIGSTRate"]);
                    }
                    Database.myreader.Close();
                    combocommodity.Text = "";
                    combocommodity.Text = Commodity;


                }
            }
            else if (dataGridView1.CurrentCell.ColumnIndex == DUnitPrice.Index || dataGridView1.CurrentCell.ColumnIndex == DOrderQty.Index ||
                dataGridView1.CurrentCell.ColumnIndex == colIGSTRate.Index)
            {
                dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[DTotal.Index].Value =
                      Convert.ToString(Utility.SafeConvertToDouble(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[DOrderQty.Index].Value)
                  * Utility.SafeConvertToDouble(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[DUnitPrice.Index].Value));


                dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[ColIGSTAmt.Index].Value =
                    Math.Round(Utility.SafeConvertToDouble(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[DTotal.Index].Value) *
                    Utility.SafeConvertToDouble(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[colIGSTRate.Index].Value) / 100, 2);
            }
        }

        private void dateTimePickerInvDate_ValueChanged(object sender, EventArgs e)
        {
            try
            {
                if (Database.OpenConnection(Utility.DespacthConnectionString))
                {
                    string strsql = "Select distinct companyname from vw_CompanyMaster where   " +
                          "   isnull(IsFreeze,'no')='no' and (('" + dateTimePickerInvDate.Value.ToString("yyyy-MM-dd") +
                           "' between fromdate and todate) or (fromdate <= '" +
                           dateTimePickerInvDate.Value.ToString("yyyy-MM-dd") + "' and todate is null ))  order by CompanyName";
                    DespatchCommonFunction.Loadcombo(comboBuyerName, strsql, "CompanyName", "CompanyName", Utility.DespacthConnectionString);
                }
            }
            catch (Exception ex)
            {
                ex.Message.ToString();
            }
        }

    }
}
