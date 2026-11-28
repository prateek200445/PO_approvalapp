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
    public partial class frmpackinglist : Form
    {
        private Dictionary<string, string> companyMap = new Dictionary<string,string>()
         {
             {"HPBL1", "HCP Plastene Bulkpack Ltd"},
             {"HPBL4", "HCP Plastene Bulkpack Ltd (Unit - IV)"},
             {"PIL1", "Plastene India Limited "},
             {"PIL2", "Plastene India Limited (Unit -II)"},
             {"PILHO", "Plastene India Limited - HO"},
             {"PPL", "Plastene Polyfilms Limited"},
             {"OEL", "Oswal Extrusion Limited"},
             {"KPW1", "K.P. WOVEN PRIVATE LIMITED"},
             {"KPW3", "K.P. WOVEN PRIVATE LIMITED (UNIT-III)"},
             {"HCPE", "HCP ENTERPRISE LIMITED"}
         };

        private Dictionary<string, string> stuffingcompanyMap = new Dictionary<string, string>()
         {
             {"HPBL1", "HCP Plastene Bulkpack Ltd"},
             {"HPBL4", "HCP Plastene Bulkpack Ltd (Unit - IV)"},
             {"PIL1", "Plastene India Limited"},
             {"PIL2", "Plastene India Limited (Unit -II)"},
             {"PILHO", "Plastene India Limited - HO"},
             {"PPL", "Plastene Polyfilms Limited"},
             {"OEL", "Oswal Extrusion Limited"},
             {"KPW1", "K.P. WOVEN PRIVATE LIMITED"},
             {"KPW3", "K.P. WOVEN PRIVATE LIMITED (UNIT-III)"},
             {"HCPE", "HCP ENTERPRISE LIMITED"}
         };

        DataTable dtIMaster;
        ArrayList ArrListSionNo = new ArrayList();
        ArrayList ArrListImportItem = new ArrayList();
        ArrayList ArrListImportItemUnit = new ArrayList();
        ArrayList ArrListImportItemWt = new ArrayList();
        string ExportITCHSNo = "";
        string CompanyCode = "";
        int TotalCount = 0;
        bool BlnSEZ = false;
        public bool isupdate = false;
        double TCS = 0;
        //Change by Rishabh on 25-07-2026 as per instructions by Anil sir for object highlight if validation fails
        private Timer validationTimer = new Timer();

        private Control highlightedControl = null;

        private Color originalColor = SystemColors.Window;

        private int flashCount = 0;
        //Change end

        public frmpackinglist(string invno, string companyname, bool SEZ)
        {
            InitializeComponent();
            BlnSEZ = SEZ;
            Form_Load();

            //Change by Rishabh on 03-11-2025 for new way of getting ADcodes in dropdown from the master copied from Anil sir's master excel.
            //TxtADCODE.AutoCompleteMode = AutoCompleteMode.Suggest;
            //TxtADCODE.AutoCompleteSource = AutoCompleteSource.CustomSource;
            //TxtADCODE.AutoCompleteCustomSource = DespatchCommonFunction.BondAutoComplete(comboCompanyName.Text);
            //Change End

            label116.Visible = BlnSEZ;
            textTCS.Visible = BlnSEZ;
            if (SEZ)
            {
                if (Database.OpenConnection(Utility.DespacthConnectionString))
                {
                    //Change by Rishabh on 03-11-2025 as txtbox replaced by combobox
                    //TxtADCODE.Text = Convert.ToString(Database.GetExecuteNonQueryCommand_retrun("Select ADCODE from Factoryinfo where name='" + comboCompanyName.Text + "'"));
                    comboBox1.Text = Convert.ToString(Database.GetExecuteNonQueryCommand_retrun("Select ADCODE from Factoryinfo where name='" + comboCompanyName.Text + "'"));
                    Database.Closeconnection();
                    //Change End
                }
            }

            comboCompanyName.Text = companyname;
            textinvoiceno.Text = invno;
            DisplayRecord();

        }
        public frmpackinglist()
        {
            InitializeComponent();
            Form_Load();
            label116.Visible = BlnSEZ;
            textTCS.Visible = BlnSEZ;
        }
        public frmpackinglist(bool SEZ)
        {
            InitializeComponent();
            BlnSEZ = SEZ;
            Form_Load();

            //Change by Rishabh on 03-11-2025 for new way of getting ADcodes in dropdown from the master copied from Anil sir's master excel.
            //TxtADCODE.AutoCompleteMode = AutoCompleteMode.Suggest;
            //TxtADCODE.AutoCompleteSource = AutoCompleteSource.CustomSource;
            //TxtADCODE.AutoCompleteCustomSource = DespatchCommonFunction.BondAutoComplete(comboCompanyName.Text);
            //Change End

            label116.Visible = BlnSEZ;
            textTCS.Visible = BlnSEZ;
            if (SEZ)
            {
                if (Database.OpenConnection(Utility.DespacthConnectionString))
                {
                    //Change by Rishabh on 03-11-2025 as txtbox replaced by combobox
                    //TxtADCODE.Text = Convert.ToString(Database.GetExecuteNonQueryCommand_retrun("Select ADCODE from Factoryinfo where name='" + comboCompanyName.Text + "'"));
                    comboBox1.Text = Convert.ToString(Database.GetExecuteNonQueryCommand_retrun("Select ADCODE from Factoryinfo where name='" + comboCompanyName.Text + "'"));
                    Database.Closeconnection();
                    //Change End
                }
                //if (comboCompanyName.Text.Contains("Plastene India Limited "))
                //    TxtADCODE.Text = "020 2616 of Bank of Baroda";
                //else if (comboCompanyName.Text == "Plastene Polyfilms Limited")
                //    TxtADCODE.Text = "00 20438 of State bank of India";
            }
        }

        private void Form_Load()
        {
            groupDetails.Visible = false;
            fillData();

            if (comboCompanyName.Text != "Oswal Extrusion Limited (Unit-II)")
            {
                textBCDPerc.Text = "0";
                textCVDPerc.Text = "0";
                textEDCessPerc.Text = "0";
                textHEDcessPerc.Text = "0";
                textEDUCESSPerc.Text = "0";
                textHEDUCESSPerc.Text = "0";
                textAddVatPerc.Text = "0";
            }
            // comboType.SelectedIndex = 0;

            dateTimePicker_bill.Format = DateTimePickerFormat.Custom;
            dateTimePicker_bill.CustomFormat = DespatchCommonFunction.ERPDateSettings();
            dateTimePicker_bill.Value = DateTime.Now; // .Text = System.DateTime.Now.ToString();

            dateTimeGRNo.Format = DateTimePickerFormat.Custom;
            dateTimeGRNo.CustomFormat = DespatchCommonFunction.ERPDateSettings();

            dateTimePicker_doc.Format = DateTimePickerFormat.Custom;
            dateTimePicker_doc.CustomFormat = DespatchCommonFunction.ERPDateSettings();

            dateTimePicker_invoice.Format = DateTimePickerFormat.Custom;
            dateTimePicker_invoice.CustomFormat = DespatchCommonFunction.ERPDateSettings();
            dateTimePicker_order.Format = DateTimePickerFormat.Custom;
            dateTimePicker_order.CustomFormat = DespatchCommonFunction.ERPDateSettings();
            textSuffix.Text = frmDefaultVale.Year;
            string strsql = "";
            if (Database.OpenConnection(Utility.MaterialConnectionString))
            {
                strsql = "";
                strsql = "select paymenttermname from PaymentTermsMaster where Type='Sales' and isdelflag=0 order by paymenttermname";
                DespatchCommonFunction.Loadcombo(comboPaymentTerms, strsql, "paymenttermname", "paymenttermname", Utility.MaterialConnectionString);
                if (comboPaymentTerms.Items.Count > 0)
                    comboPaymentTerms.SelectedIndex = 0;
            }

            if (Database.OpenConnection(Utility.DespacthConnectionString))
            {

                strsql = "";
                //strsql = "Select distinct CompanyName from CompanyMaster order by CompanyName";
                //strsql = "Select companyname from CompanyMaster where " +
                //          " isnull(IsFreeze,'no')='no' and (('" + dateTimePicker_invoice.Value.ToString("yyyy-MM-dd") +
                //          "' between fromdate and todate) or (fromdate <= '" + dateTimePicker_invoice.Value.ToString("yyyy-MM-dd") +
                //          "' and todate is null ))  order by CompanyName";
                //DespatchCommonFunction.Loadcombo(comboBuyerName, strsql, "CompanyName", "CompanyName", Utility.DespacthConnectionString);
                DespatchCommonFunction.GetCurrencyCombo(comboCurrency, Utility.DespacthConnectionString);
                DespatchCommonFunction.GetCompanyList(comboCompanyName);
                comboCompanyName.Text = frmDefaultVale.CompanyName;
                DespatchCommonFunction.GetCompanyList(combomanufact);
                Database.myreader.Close();

                DespatchCommonFunction.GetDepttName(comboDeptt);
                Database.OpenConnection(Utility.DespacthConnectionString);
                strsql = "";
                strsql = "Select distinct(Depttname) as Depttname from itemdeptt order by Depttname";
                DespatchCommonFunction.Loadcombo(comboDeptt, strsql, "Depttname", "Depttname", Utility.DespacthConnectionString);

                strsql = "";
                strsql = "select DisPortName from dischargeport order by DisPortName";
                DespatchCommonFunction.Loadcombo(combo_discharge, strsql, "DisPortName", "DisPortName", Utility.DespacthConnectionString);


                strsql = "";
                strsql = "select PortCode,loadportname from loadingport order by PortCode";
                DespatchCommonFunction.Loadcombo(combo_loading, strsql, "loadportname", "PortCode", Utility.DespacthConnectionString);

                strsql = "";
                strsql = "select  distinct(country_origin)  as country_origin from packinglist order by country_origin";
                DespatchCommonFunction.Loadcombo(combocountry_origin, strsql, "country_origin", "country_origin", Utility.DespacthConnectionString);

                strsql = "";
                strsql = "select distinct (country_desti) as country_desti from packinglist order by country_desti";
                DespatchCommonFunction.Loadcombo(combocontry_desti, strsql, "country_desti", "country_desti", Utility.DespacthConnectionString);

                strsql = "";
                strsql = "select distinct commodityname from commodity where companyname = '" + comboCompanyName.Text + "' order by commodityname";
                DespatchCommonFunction.Loadcombo(combocommodity, strsql, "commodityname", "commodityname", Utility.DespacthConnectionString);

                strsql = "";
                strsql = "Select EPCGLicNo from EPCGMaster where companyname = '" + comboCompanyName.Text + "' and SetDefault='yes' and isnull(isfreeze,'no')='no' order by EPCGLicNo";
                DespatchCommonFunction.Loadcombo(comboEPCGNo, strsql, "EPCGLicNo", "EPCGLicNo", Utility.DespacthConnectionString);

                DespatchCommonFunction.GetTransportMaster(comboTransporter);

                strsql = "Select Distinct(vehicalNo) as vehicalNo from DomesticInvoice order by vehicalNo";
                DespatchCommonFunction.Loadcombo(comboVehicalNo, strsql, "vehicalNo", "vehicalNo", Utility.DespacthConnectionString);

                Database.myreader = Database.GetExecuteReaderCommand("Select * from UTBond where companyname = '"
                  + comboCompanyName.Text + "' order by BondDate desc");    
                if (Database.myreader.Read())
                    textUT1Bond.Text = Database.myreader[1].ToString();
                Database.myreader.Close();
                Database.Closeconnection();

                //Change by Rishabh on 03-11-2025 for new way of getting ADcodes in dropdown from the master copied from Anil sir's master excel.
                DespatchCommonFunction.GetADCODE(comboBox1);
                DespatchCommonFunction.GetExportBank(comboBox2);
                //Change End
            }

            if (Database.OpenConnection(Utility.MaterialConnectionString))
            {
                strsql = "";
                strsql = "select LedgerName from ledgermaster where isinvoice = 'yes' and companyname = '" + comboCompanyName.Text + "'";
                DespatchCommonFunction.Loadcombo(comboAccountHead, strsql, "LedgerName", "LedgerName", Utility.MaterialConnectionString);
                Database.Closeconnection();
            }

            textSuffix.Text = frmDefaultVale.Year;

            if (comboBuyerName.Items.Count > 0)
                comboBuyerName.SelectedIndex = 0;
            if (comboDeptt.Items.Count > 0)
                comboDeptt.SelectedIndex = 0;

            comboCurrency.SelectedIndex = 0;
            groupmanufact.Visible = false;
            btn_update.Enabled = false;
            if (Database.OpenConnection(Utility.DespacthConnectionString))
            {
                textIE_code.Text = Convert.ToString(Database.GetExecuteNonQueryCommand_retrun("select importerscode from FactoryInfo where name='" + comboCompanyName.Text + "'"));
            }
            if (BlnSEZ)
            {
                textPackagingDetail.Text = "NOTE:PACKING MATERIALS PIPE & CONES ARE RETURNABLE BASIS  otherwise we are not able to clear the pipes.";
            }

            //Change By Rishabh on 25-07-2026 as per instructions by Anil Sir. Import check box checked by default
            checkImport.Checked = true;
        }

        public bool CheckBuyerName(string BuyerName)
        {
            bool IsAvail = false;

            if (Database.OpenConnection(Utility.DespacthConnectionString))
            {
                Database.myreader = Database.GetExecuteReaderCommand("Select * from companymaster where companyname = '"
                  + comboBuyerName.Text + "'");
                if (Database.myreader.Read())
                    IsAvail = true;
                Database.myreader.Close();
                Database.Closeconnection();
            }
            return IsAvail;
        }

        public bool CheckConsignee(string BuyerName, string Consignee)
        {
            bool IsAvail = false;

            if (Database.OpenConnection(Utility.DespacthConnectionString))
            {
                string ClientCode = "";
                Database.myreader = Database.GetExecuteReaderCommand("Select distinct companycode from companymaster where companyname = '"
                  + comboBuyerName.Text + "'");
                if (Database.myreader.Read())
                {
                    ClientCode = Database.myreader[0].ToString();
                }

                Database.myreader = Database.GetExecuteReaderCommand("Select * from Consignee where CompanyCode = '"
                  + ClientCode + "' and ConsigneeName = '" + Consignee + "'");

                if (Database.myreader.Read())
                    IsAvail = true;
                Database.myreader.Close();
                Database.Closeconnection();
            }
            return IsAvail;
        }


        public static int GetPurchasecode(string PrefixCode)
        {
            ArrayList arrpur = new ArrayList();
            int MaxValue = 0;
            if (Database.OpenConnection(Utility.DespacthConnectionString))
            {
                Database.myreader = Database.GetExecuteReaderCommand("Select InvNo from Packinglist where Invoice_No like '"
                                        + PrefixCode + "%'");
                while (Database.myreader.Read())
                    arrpur.Add(Database.myreader[0].ToString());
                Database.myreader.Close();

                ArrayList arr = new ArrayList();

                for (int i = 0; i < arrpur.Count; i++)
                    arr.Add(arrpur[i].ToString().Substring(arrpur[i].ToString().LastIndexOf('/') + 1));

                for (int i = 0; i < arr.Count; i++)
                {
                    if (MaxValue < Convert.ToInt32(arr[i]))
                        MaxValue = Convert.ToInt32(arr[i]);
                }
            }
            return MaxValue;
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

                Database.myreader = Database.GetExecuteReaderCommand("Select  ISNULL(MAX(invno),0) from packinglist where companyname = '"
                  + companyname + "' and yr = '" + textSuffix.Text + "' and isthirdparty='no'  ");//order by invno desc
                if (Database.myreader.Read())
                    InvoiceSeries = Convert.ToInt32(Database.myreader[0].ToString());
                Database.myreader.Close();
                InvoiceSeries = InvoiceSeries + 1;
                Invno = Invno + "/EXP/" + InvoiceSeries.ToString() + "/" + yr;
                Database.Closeconnection();
            }
            return Invno;
        }

        private double Calculation(int QtyIndex, int AmountIndex, int TotalIndex)
        {
            TotalCount = 0;
            double total = 0;
            double temptotal = 0;
            double dblIGSTAmt = 0;
            try
            {

                for (int i = 0; i <= dataGridView1.Rows.Count - 1; i++)
                {
                    if (dataGridView1.Rows[i].Cells[QtyIndex].Value.ToString() != ""
                        && dataGridView1.Rows[i].Cells[AmountIndex].Value.ToString() != "" &&
                        dataGridView1.Rows[i].Cells[KindofPackages.Index].Value.ToString() != ""
                        && dataGridView1.Rows[i].Cells[QtyUnit.Index].Value.ToString() != ""
                        && dataGridView1.Rows[i].Cells[NetWeight.Index].Value.ToString() != "")
                    {
                        TotalCount = TotalCount + 1;
                        dataGridView1.Rows[i].Cells[TotalIndex].Value =
                                       Convert.ToString(Convert.ToDouble(dataGridView1.Rows[i].Cells[QtyIndex].Value.ToString())
                                     * Convert.ToDouble(dataGridView1.Rows[i].Cells[AmountIndex].Value.ToString()));
                        total += Convert.ToDouble(dataGridView1.Rows[i].Cells[TotalIndex].Value);
                    }

                    dataGridView1.Rows[i].Cells[colIGSTAmt.Index].Value =
                                       Convert.ToString(Convert.ToDouble(dataGridView1.Rows[i].Cells[colIGSTRate.Index].Value.ToString())
                                     * Convert.ToDouble(dataGridView1.Rows[i].Cells[TotalIndex].Value.ToString()));
                    dblIGSTAmt += Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[colIGSTAmt.Index].Value);
                }

                temptotal = total + dblIGSTAmt;
                total = Math.Round(total + dblIGSTAmt, 0);
                textFOBValue.Text = Convert.ToString(total);
                if (textFrieght.Text == "")
                    textFrieght.Text = "0";
                if (textInsurance.Text == "")
                    textInsurance.Text = "0";
                if (textCommision.Text == "")
                    textCommision.Text = "0";
                if (textDiscount.Text == "")
                    textDiscount.Text = "0";
                if (textOther.Text == "")
                    textOther.Text = "0";
                if (textDeduction.Text == "")
                    textDeduction.Text = "0";
                if (textDutyForegone.Text == "")
                    textDutyForegone.Text = "0";

            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
            }
            return temptotal;
        }


        private void btn_save_Click(object sender, EventArgs e)
        {
            bool blnTans = false;
            try
            {
                #region 19-08-2020 lock as per accounting voucher type
                if (!Utility.CheckAccountVoucherDateFreeze(dateTimePicker_invoice.Value, comboCompanyName.Text, "sales"))
                {
                    MessageBox.Show(AccountCommonVariable.ErrMsg.DATE_FREEZE + "\nPlease check Invoice Date");
                    return;
                }
                #endregion

                #region check for Shipping Bill No
                if (!this.BlnSEZ)
                {
                    if (Database.OpenConnection(Utility.DespacthConnectionString))
                    {
                        string GSTNO = Convert.ToString(Database.GetExecuteNonQueryCommand_retrun("Select newgstno from companymaster where CompanyName='" + comboBuyerName.Text + "'"));
                        int Reccnt = Convert.ToInt32(Database.GetExecuteNonQueryCommand_retrun("select * from [ShippingBillNos]('" + comboCompanyName.Text + "','" + textSuffix.Text + "','" + GSTNO + "')"));
                        if (Reccnt > 0)
                        {
                            MessageBox.Show("Please Enter Shipping Bill No in Earlier Export Invoice");
                            return;
                        }
                    }
                }
                #endregion
                string manufact = "";
                string manufinvno = "";
                string manufinvdt = "";
                string isepcg = "";
                string epcg = "";
                string isadvance = "";
                string isdfia = "";
                string advance = "";
                string isimport = "";
                string rex = "";
                bool isentry = false;
                bool isitemdeptt = false;
                int InvoiceSeries = 0;
                int menu = 0;
                bool isMultilayer = false;

                if (combo_loading.Text.Length == 0)
                {
                    //MessageBox.Show("Please Select Port Code");
                    //combo_loading.Focus();
                    ShowValidation("Please Select Port Code", combo_loading);
                    return;
                }
                if (!combo_loading.Text.Contains(combo_loading.Text))
                {
                    //MessageBox.Show("Please Select Port Code");
                    //combo_loading.Focus();
                    ShowValidation("Please Select Port Code", combo_loading);
                    return;
                }
                if (comboDeptt.Text == "")
                {
                    //MessageBox.Show("Please enter Value in DepttName");
                    ShowValidation("Please enter Value in DepttName", comboDeptt);
                    return;
                }
                //Change by Rishabh on 03-11-2025 for AD Code check
                if (comboBox1.Text == "")
                {
                    //MessageBox.Show("Please enter Value in AD Code");
                    ShowValidation("Please enter Value in AD Code", comboBox1);
                    return;
                }
                //Change End
                if (textbuyers_orderno.Text.Length < 5)
                {
                    //MessageBox.Show("Please enter Value in Buyer Order No");
                    ShowValidation("Please enter Value in Buyer Order No", textbuyers_orderno);
                    return;
                }
                //if (comboDeptt.Text == "Multilayer")
                //    isMultilayer = true;
                //if (comboCompanyName.Text == "Plastene India Limited (Unit -II)")
                //{
                //    if (textAreNo.Text == "0" || Convert.ToInt16(textAreNo.Text) == 0)
                //    {
                //        MessageBox.Show("Please Enter Proper ARE No");
                //        return;
                //    }
                //}
                if (Database.OpenConnection(Utility.DespacthConnectionString))
                {
                    int VenCnt = DespatchCommonFunction.CustomerLedgerName(comboBuyerName.Text, comboCompanyName.Text);
                    if (VenCnt == 0)
                    {
                        MessageBox.Show("No Account Ledger Found for this customer.\nPlease Ask Account Dept to create Ledger for this customer:- [" + comboBuyerName.Text + "]");
                        return;
                    }
                }
                if (DateTime.Now < Convert.ToDateTime("2017-07-01 00:00:00"))
                {


                    textinvoiceno.Text = GenerateInvno(comboCompanyName.Text, textSuffix.Text, ref InvoiceSeries);
                }
                else
                {

                    if (comboCompanyName.Text == "HCP Plastene Bulkpack Ltd")
                        textinvoiceno.Text = DespatchCommonFunction.GeneratePackingListInvno(comboCompanyName.Text, textSuffix.Text, ref InvoiceSeries);

                    else
                        textinvoiceno.Text = DespatchCommonFunction.GenerateInvno(comboCompanyName.Text, textSuffix.Text, ref InvoiceSeries);
                }
                if (!CheckBuyerName(comboBuyerName.Text))
                    //MessageBox.Show("Please enter valid buyer Name");
                    ShowValidation("Please enter valid buyer Name", comboBuyerName);
                else if (textgrosswt.Text == "")
                    //MessageBox.Show("Please enter value in GrossWt");
                    ShowValidation("Please enter value in GrossWt", textgrosswt);
                else if (texttarewt.Text == "")
                    //MessageBox.Show("Please enter value in TareWt");
                    ShowValidation("Please enter value in TareWt", texttarewt);
                else if (combocommodity.Text == "")
                    //MessageBox.Show("Please Select Commodity");
                    ShowValidation("Please Select Commodity", combocommodity);
                else if (textpaletwt.Text == "")
                    //MessageBox.Show("Please enter value in Pallet Wt");
                    ShowValidation("Please enter value in Pallet Wt", textpaletwt);
                else if (comboContract.Text == "")
                    //MessageBox.Show("Please Select Nature of contract");
                    ShowValidation("Please Select Nature of contract", comboContract);
                else if (comboModeofTransit.Text == "")
                    //MessageBox.Show("Please Select Mode of Transit");
                    ShowValidation("Please Select Mode of Transit", comboModeofTransit);
                else if (textPackagingDetail.Text == "" && BlnSEZ == false)
                    //MessageBox.Show("Please enter packaging detail");
                    ShowValidation("Please enter packaging detail", textPackagingDetail);
                else if (textplace_delivery.Text == "")
                    //MessageBox.Show("Please enter Delivery Place ");
                    ShowValidation("Please enter Delivery Place ", textplace_delivery);
                else if (combo_loading.Text == "" && BlnSEZ == false)
                    //MessageBox.Show("Please enter loading port");
                    ShowValidation("Please enter loading port", combo_loading);
                else if (combo_discharge.Text == "" && BlnSEZ == false)
                    //MessageBox.Show("Please enter Discharge port");
                    ShowValidation("Please enter Discharge port", combo_discharge);
                else if (textbuyers_orderno.Text.Length == 0 && TxtPerformaInvNo.Text.Length == 0)
                    //MessageBox.Show("Please Enter Proper Buyers Order No");
                    ShowValidation("Please Enter Proper Buyers Order No", textbuyers_orderno);

                else
                {
                    //Change by Rishabh on 03-11-2025 for cross checking AD code swift and bank swift
                    string adcode_swift = "";
                    Database.myreader = Database.GetExecuteReaderCommand("select Swift from ExportADCodesMaster where  ADCode = '" + comboBox1.Text + "'");
                    if (Database.myreader.Read())
                        adcode_swift = Database.myreader[0].ToString();
                    Database.myreader.Close();
                    if (adcode_swift != textSwiftCode.Text)
                    {
                        //MessageBox.Show("Selected AD Code and Bank Swift does not match");
                        ShowValidation("Selected AD Code and Bank Swift does not match", textSwiftCode);
                        return;
                    }

                    //Change End

                    double TotalAmt = 0;
                    double NetWt = 0;
                    for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                    {
                        if (dataGridView1.Rows[i].Cells[dtItemDeptt.Index].FormattedValue.ToString() == "")
                        {
                            MessageBox.Show("Please Select Proper itemdeptt");
                            isitemdeptt = true;
                            break;
                        }
                        if (dataGridView1.Rows[i].Cells[dtCommodity.Index].FormattedValue.ToString() == "")
                        {
                            MessageBox.Show("Please Select Proper Commodity");
                            isitemdeptt = true;
                            break;
                        }
                        string THeading = Convert.ToString(dataGridView1.Rows[i].Cells[ColHSNCode.Index].Value);
                        long number1 = 0;
                        bool canConvert = long.TryParse(THeading, out number1);
                        if (!canConvert)
                        {
                            MessageBox.Show("Please Enter Proper HSN Code");
                            return;
                        }

                        string itemcode = Convert.ToString(dataGridView1.Rows[i].Cells[colItemCode.Index].Value);
                        if (itemcode.Length == 0)
                        {
                            MessageBox.Show("Please Select Item ");
                            isitemdeptt = true;
                            break;
                        }
                        if (dataGridView1.Rows[i].Cells[Qty.Index].Value.ToString() != "" &&
                               dataGridView1.Rows[i].Cells[KindofPackages.Index].Value.ToString() != "" &&
                               dataGridView1.Rows[i].Cells[QtyUnit.Index].Value.ToString() != "")
                        {
                            NetWt += Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[NetWeight.Index].Value);
                            Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[Rate.Index].Value);
                            TotalAmt += Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[Amount.Index].Value);
                        }
                    }
                    textntwt.Text = NetWt.ToString();

                    if (!isitemdeptt)
                    {

                        //textFOBValue.Text = Convert.ToString(TotalAmt);
                        if (textFrieght.Text == "")
                            textFrieght.Text = "0";
                        if (textInsurance.Text == "")
                            textInsurance.Text = "0";
                        if (textCommision.Text == "")
                            textCommision.Text = "0";
                        if (textDiscount.Text == "")
                            textDiscount.Text = "0";
                        if (textOther.Text == "")
                            textOther.Text = "0";
                        if (textDeduction.Text == "")
                            textDeduction.Text = "0";

                        if (textDutyForegone.Text == "")
                            textDutyForegone.Text = "0";

                        TotalAmt += Utility.SafeConvertToDouble(textFrieght.Text) + Utility.SafeConvertToDouble(textInsurance.Text)
                          + Utility.SafeConvertToDouble(textCommision.Text) + Utility.SafeConvertToDouble(textDiscount.Text)
                          + Utility.SafeConvertToDouble(textOther.Text) + Utility.SafeConvertToDouble(textDeduction.Text);

                        TotalAmt += Utility.SafeConvertToDouble(textSpDuty1Amt.Text) +
                            Utility.SafeConvertToDouble(textSpDuty2Amt.Text) +
                            Utility.SafeConvertToDouble(textSpDuty3Amt.Text);
                        //Change by Rishabh to incorporate manufacturer invno and invdt
                        if (checkmenufact.Checked)
                        {
                            menu = 1;
                            manufact = combomanufact.Text;
                            manufinvno = textBox1.Text;
                            manufinvdt = dateTimePicker1.Value.ToString(DespatchCommonFunction.ERPDBDateSettings());
                        }
                        if (checkEPCG.Checked)
                        {
                            isepcg = "yes";
                            epcg = comboEPCGNo.Text;
                        }
                        else
                            isepcg = "No";
                        if (checkAdvanceLic.Checked || checkDFIA.Checked)
                        {
                            if (checkDFIA.Checked)
                            {
                                isdfia = "yes";
                                isadvance = "no";
                            }
                            else
                            {
                                isdfia = "no";
                                isadvance = "yes";
                            }
                            advance = comboAdvLicenseNo.Text;
                        }
                        else
                        {
                            isdfia = "No";
                            isadvance = "No";
                        }

                        if (checkImport.Checked)
                            isimport = "yes";
                        else
                            isimport = "no";
                        //int menu = 0;
                        //if (checkmenufact.Checked)
                        //menu = 1;

                        double CommercialAmount = 0;

                        if (comboContract.Text == "CIF")
                            CommercialAmount = Convert.ToDouble(textFOBValue.Text) + Convert.ToDouble(textFrieght.Text)
                                                  + Convert.ToDouble(textInsurance.Text);
                        else if (comboContract.Text == "CF")
                            CommercialAmount = Convert.ToDouble(textFOBValue.Text) + Convert.ToDouble(textFrieght.Text);
                        else if (comboContract.Text == "CI")
                            CommercialAmount = Convert.ToDouble(textFOBValue.Text) + Convert.ToDouble(textInsurance.Text);
                        else if (comboContract.Text == "FOB")
                            CommercialAmount = Convert.ToDouble(textFOBValue.Text);

                        double AMountinRupees = Convert.ToDouble(CommercialAmount) * Convert.ToDouble(textCurrencyExchangeRate.Text);
                        string AmountInRupeesWords = Utility.IntegerToWords(Convert.ToInt64(AMountinRupees));
                        if (comboCompanyName.Text == "Oswal Extrusion Limited (Unit-II)")
                            Calc(AMountinRupees);

                        if (comboCompanyName.Text == "Oswal Extrusion Limited (Unit-II)")
                        {
                            CommercialAmount = TotalAmt;
                            AMountinRupees = CommercialAmount * Convert.ToDouble(textCurrencyExchangeRate.Text);
                            AmountInRupeesWords = Utility.IntegerToWords(Convert.ToInt64(AMountinRupees));
                        }

                        if (AMountinRupees > 20000000)
                        {
                            MessageBox.Show("Pl check Invoice Amount or Currency Conversion , not properly fill as Amount in Rs going more then 20000000");
                        }
                        else
                        {
                            calculate();
                            #region Check Advance Lic Qty
                            if (checkAdvanceLic.Checked || checkDFIA.Checked)
                            {
                                double pendingq = DespatchCommonFunction.PendingAdvLic(comboCompanyName.Text, comboAdvLicenseNo.Text, Utility.SafeConvertToDouble(textntwt.Text));
                                if (pendingq <= 0)
                                {
                                    MessageBox.Show("Export Aginst This Advnace License is not possible.");
                                    return;
                                }
                            }
                            #endregion
                            #region PackingList
                            string despackinglist = "";
                            if (isMultilayer == false)
                            {
                                if (DespatchCommonFunction.AllowMappingPackinglist(comboCompanyName.Text) > 0)
                                {

                                    if (!DespatchCommonFunction.GetPackingListInvoiceNo2(comboCompanyName.Text, comboBuyerName.Text, Utility.SafeConvertToDouble(textntwt.Text), dataGridView1, colItemCode.Name, NetWeight.Name, ref despackinglist))
                                    {
                                        return;
                                    }
                                }
                            }
                            #endregion

                            DialogResult drs = MessageBox.Show("Do you want to generate Packinglist", "Generate PackingList"
                                , MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                            if (drs.CompareTo(DialogResult.Yes) == 0)
                            {
                                if (checkAdvanceLic.Checked)
                                {
                                    txtDrawBackNo.Text = "";
                                    txtDrawBackAmt.Text = "0";
                                }

                                //int InvoiceSeries = 0;
                                if (DateTime.Now < Convert.ToDateTime("2017-07-01 00:00:00"))
                                {

                                    textinvoiceno.Text = GenerateInvno(comboCompanyName.Text, textSuffix.Text, ref InvoiceSeries);
                                }
                                else
                                {
                                    if (comboCompanyName.Text == "HCP Plastene Bulkpack Ltd")
                                        textinvoiceno.Text = DespatchCommonFunction.GeneratePackingListInvno(comboCompanyName.Text, textSuffix.Text, ref InvoiceSeries);
                                    else
                                        textinvoiceno.Text = DespatchCommonFunction.GenerateInvno(comboCompanyName.Text, textSuffix.Text, ref InvoiceSeries);
                                }
                                //textinvoiceno.Text = DespatchCommonFunction.GenerateInvno(comboCompanyName.Text, textSuffix.Text, ref InvoiceSeries);
                                if (Database.OpenConnection(Utility.DespacthConnectionString))
                                {
                                    blnTans = false;
                                    Database.BeginTransaction();
                                    blnTans = true;
                                    if (Database.GetExecuteNonQueryCommand("insert into packinglist(IE_code,Invoice_no,Invoice_dt,Buyers_od_no,Buyers_od_dt,Reference,Bill_leading_no,Bill_leading_dt,Doc_cred_no,Doc_dt,country_origin,country_desti,Buyers,terms_payement,pre_carrige,place_receipt,vessl_flteno,port_loading,port_discharge,place_delivery,Hc_containerno,line_seal,cusom_seal_no,gross_wt,tare_wt,pallet_wt,tot_net_wt,yr,remarks,Remarks2,rg23a,consignee,cons_city,cons_ntbyer,currancy,cur_value,exduty,educess,sechiedu,deptt,CommercialAmount,vat,addvat,tcs,excise,vatapp,CompanyName,thirdparty,manucheckbox,Notifier,ContType,AmtWord,TotalAmt,HTSCODE,TotalCBM,commodity,bankdetail,EpcgNo,advno,utibondno,isepcg,isadvlic,beingexport,isdfia,isimport,currancyconversion,exciseper,educessper,hieducessper,areno,FOBValue,FrieghtAmount,Insurance,Commision,Discount,Other,Deduction,DutyForegone,AMountinRupees,AmountInRupeesWords,GRNo,GRDate,PackingMaterialWt,typeofshipment,NatureofContract,invno,authorised,AccountHead,BCDPERC,CVDPerc,EDCessPerc,HEDCessPerc,EDuCEssPerc,HeducessPerc,AddVatPerc,BCD,CVD,EDCess,HEDCess,EDuCEssoel2,Heducess,AddVatOel2,OpeningBalance,DutyDebited,ClosingBalance,Ismarketing,deliveryterms,ThirdPartyName,IsthirdParty,modeoftransit,packagingdetial,FreightRemarks,FreightRemarks2,FreightAmt2,InlandHaulageRem,InlandHaulageAmt,OtherDuty,OtherDutyAmt,DiscountRem,SpDutyRemarks2,SpDutyAmt2,SpDutyRemarks3,SpDutyAmt3,PerformaInvoiceNo,ADCODE,TransportName,VehicalNo,InvType,LRNO,dIECAAdress,note,BillofExportNo,BillofExportDate,ShippingBillNo,ShippingBDTE,drawbackno,drawbackAmt,PortCode,tcsper,manufinvno,manufdt,contbooking,totalpkg,stuffing_plant,cont_weight,cont_tare_wt,WeightmentDt,Wt_SlipNo) values('"
                                          + textIE_code.Text + "','"
                                          + textinvoiceno.Text + "','"
                                          + dateTimePicker_invoice.Value.ToString(DespatchCommonFunction.ERPDBDateSettings()) + "','"
                                          + textbuyers_orderno.Text + "','"
                                          + dateTimePicker_order.Value.ToString(DespatchCommonFunction.ERPDBDateSettings()) + "','"
                                          + textother_reff.Text.Replace("'", "''") + "','"
                                          + textbill_loading.Text + "','"
                                          + dateTimePicker_bill.Value.ToString(DespatchCommonFunction.ERPDBDateSettings()) + "','"
                                          + textdoc_creditno.Text + "','"
                                          + dateTimePicker_doc.Value.ToString(DespatchCommonFunction.ERPDBDateSettings()) + "','"
                                          + combocountry_origin.Text + "','"
                                          + combocontry_desti.Text + "','"
                                          + comboBuyerName.Text + "','"
                                          + comboPaymentTerms.Text + "','"
                                          + textprecarriage.Text + "','"
                                          + textreceipt_precarrier.Text + "','"
                                          + textvessel_no.Text + "','"
                                          + combo_loading.Text + "','"
                                          + combo_discharge.Text + "','"
                                          + textplace_delivery.Text + "','"
                                          + texthc_containerno.Text + "','"
                                          + textline_Seal.Text + "','"
                                          + textcustom_sealno.Text + "',"
                                          + textgrosswt.Text + ","
                                          + texttarewt.Text + ","
                                          + textpaletwt.Text + ","
                                          + textntwt.Text + ",'"
                                          + textSuffix.Text + "','"
                                          + textRemarks.Text + "','"
                                          + textPLA.Text + "','"
                                          + textRG23A.Text + "','"
                                          + comboConsigneeName.Text + "','"
                                          + comboConsigneeCity.Text + "','"
                                          + textConsignee.Text + "','"
                                          + comboCurrency.Text + "','" +
                                          comboCurrencyValue.Text + "',"
                                          + " 0,0,0,'" + comboDeptt.Text + "'," + CommercialAmount + ",0,0," + TCS + ",'0','0','"
                                          + comboCompanyName.Text + "','"
                                          + manufact + "',"
                                          + menu + ",'"
                                          + textNotifier.Text + "','"
                                          + comboContType.Text.Replace("'", "''") + "','"
                                          + Utility.IntegerToWords(Convert.ToInt64(TotalAmt)) + "',"
                                          + Convert.ToDouble(TotalAmt) + ",'"
                                          + textHtsCode.Text + "',"
                                      + textCBM.Text + ",'"
                                      + combocommodity.Text + "','"
                                      + textBankDetail.Text + "','"
                                        //Change By Rishabh on 03-11-2025 no longer needed
                                        //+ textBankDate.Text + "','"
                                        //Change End
                                      + epcg + "','"
                                      + advance + "','"
                                      + textUT1Bond.Text + "','"
                                      + isepcg + "','"
                                      + isadvance + "','"
                                      + comboBeingExport.Text + "','"
                                      + isdfia + "','"
                                      + isimport + "',"
                                      + textCurrencyExchangeRate.Text + ","
                                      + textExcise.Text + ","
                                      + textEducess.Text + ","
                                       + texthiEducess.Text + ",'"
                                       + textAreNo.Text + "'," +
                                       textFOBValue.Text + ","
                                       + textFrieght.Text + ","
                                       + textInsurance.Text + ","
                                       + textCommision.Text + ","
                                       + textDiscount.Text + ","
                                       + textOther.Text + ","
                                       + textDeduction.Text + ","
                                       + textDutyForegone.Text + ","
                                       + AMountinRupees + ",'"
                                       + AmountInRupeesWords
                                       + "','" + textGRNo.Text
                                       + "','" + dateTimeGRNo.Value.ToString(DespatchCommonFunction.ERPDBDateSettings())
                                       + "'," + textPackingMaterial.Text
                                       + ",'" + comboShipment.Text + "','"
                                       + comboContract.Text + "',"
                                       + InvoiceSeries + ",'" + textAuthorised.Text
                                       + "','" + comboAccountHead.Text + "',"
                                       + textBCDPerc.Text + ","
                                       + textCVDPerc.Text + ","
                                       + textEDCessPerc.Text + ","
                                       + textHEDcessPerc.Text + ","
                                       + textEDUCESSPerc.Text + ","
                                       + textHEDUCESSPerc.Text + ","
                                       + textAddVatPerc.Text + ","
                                       + textBCD.Text + ","
                                       + textCVD.Text + ","
                                       + textEDCESS.Text + ","
                                       + textHEDCESS.Text + ","
                                       + textEDUCESSOEL2.Text + ","
                                       + textHEDUCESS.Text + ","
                                       + textADDVAT.Text + ","
                                       + textOpeningBalance.Text + ","
                                       + textDutyDebited.Text + ","
                                       + textClosingBalance.Text + ",'no','"
                                       + richTextterms_dlivery.Text + "','','no','"
                                       + comboModeofTransit.Text + "','"
                                       + textPackagingDetail.Text + "','','',0,'',0,'',0,'','',0,'',0,'"
                                       + TxtPerformaInvNo.Text + "','"
                                        //Change by Rishabh on 03-11-2025 as txtbox replaced by combobox
                                        //Txt.ADCODE.Text
                                       + comboBox1.Text + "','"
                                        //Change End
                                       + comboTransporter.Text + "','"
                                       + comboVehicalNo.Text + "','" + (BlnSEZ == true ? "SEZ" : "DIRECT") + "','"
                                       + txtlrno.Text + "','" + TxtIECAdd.Text + "','" + comboNote.Text + "','"
                                       + comboBillOfExportNo.Text + "','"
                                       + dateTimeBillofExportDate.Value.ToString(DespatchCommonFunction.ERPDBDateSettings()) + "','"
                                       + txtshpingbill.Text + "'," + (dtShipDt.Checked == false ? "NULL" : "'" + dtShipDt.Value + "'")
                                       + ",'" + txtDrawBackNo.Text + "'," + Utility.SafeConvertToDouble(txtDrawBackAmt.Text)
                                       + ",'" + combo_loading.SelectedValue + "'," + Utility.SafeConvertToDouble(textTCS.Text)
                                       + ",'" + manufinvno + "'"
                                       + ",'" + manufinvdt + "'"
                                        //Change by Rishabh on 10-11-2025 to add two new columns for container booking no, stuffing plant and total no of packages.
                                       + ",'" + contbooking.Text + "'"
                                       + ",'" + totalpkg.Text + "'"
                                       + ",'" + stuffing_plant.Text + "'"
                                       + ",'" + cont_weight.Text + "'"
                                       + ",'" + cont_tare_wt.Text + "'"
                                       + ",'" + WeightmentDt.Value.ToString(DespatchCommonFunction.ERPDBDateSettings()) + "'"
                                       + ",'" + Wt_SlipNo.Text + "')"))
                                        //Change End

                                        isentry = true;
                                    Database.GetExecuteNonQueryCommand("UPDATE PACKINGLIST SET SpDutyRemarks2='" + textSpDutyRemark2.Text + "'," +
                                        " SpDutyAmt2=" + Utility.SafeConvertToDouble(textSpDuty2Amt.Text) + "," +
                                        " SpDutyRemarks3='" + textSpDutyRemarks3.Text + "'," +
                                        " SpDutyAmt3=" + Utility.SafeConvertToDouble(textSpDuty3Amt.Text) + "," +
                                        " SpDutyRemarks1='" + textSpDutyRemarks1.Text + "'," +
                                        " SpDutyAmt1 =" + Utility.SafeConvertToDouble(textSpDuty1Amt.Text) + "," +
                                        " FreightRemarks2='" + txtSeaFrieghtRem.Text + "',FreightAmt2=" + Utility.SafeConvertToDouble(txtSeaFreAmt.Text) + "," +
                                        " InlandHaulageRem='" + TxtInLandHauRem.Text + "',InlandHaulageAmt=" + Utility.SafeConvertToDouble(TxtHaulageAmt.Text) + "," +
                                        " OtherDuty='" + TxtOtherRem.Text + "',OtherDutyAmt=" + Utility.SafeConvertToDouble(TxtOthersDiscAmt.Text) +
                                        " ,IsInvRate=" + (chkRate.Checked ? 1 : 0) +
                                        " WHERE  Invoice_no='" + textinvoiceno.Text + "' AND YR='" + textSuffix.Text + "'");


                                    Database.GetExecuteNonQueryCommand("UPDATE PACKINGLIST SET BeneficiaryAccountNo='" + textBenefiaciaryAccoutNo.Text + "'," +
                                       " Swiftcode='" + textSwiftCode.Text + "'," +
                                       " CorrespondingBank='" + textCorrespondingBank.Text + "'," +
                                      " CorrespondingBankSwift='" + textCorrespondingBankSwift.Text + "', IBANAccountNo='" + textIBANAccoutNo.Text + "'" +
                                      " WHERE  Invoice_no='" + textinvoiceno.Text + "' AND YR='" + textSuffix.Text + "'");

                                    //Change by Rishabh for Rex Declaration
                                    if (checkBox1.Checked)
                                    {
                                        Database.myreader = Database.GetExecuteReaderCommand("select Declaration from RexDeclaration where CompanyName = '" + comboCompanyName.Text + "'");
                                        while (Database.myreader.Read())
                                        {
                                            rex = Database.myreader[0].ToString();
                                        }
                                        Database.myreader.Close();
                                        Database.GetExecuteNonQueryCommand("update packinglist set isRex = 'yes', Remarks = ISNULL(remarks,'') + CASE WHEN Remarks IS NULL THEN '' ELSE CHAR(13) + CHAR(10) END + '" + rex + "' where Invoice_No = '" + textinvoiceno.Text + "' and Yr = '" + textSuffix.Text + "'");
                                    }
                                    //Change end
                                    //have started from here 
                                    for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                                    {

                                        if (dataGridView1.Rows[i].Cells[Qty.Index].Value.ToString() != "" &&
                                               dataGridView1.Rows[i].Cells[KindofPackages.Index].Value.ToString() != "" &&
                                               dataGridView1.Rows[i].Cells[QtyUnit.Index].Value.ToString() != "")
                                        {
                                            InvoiceSeries = 0;
                                            string invSubNo = textinvoiceno.Text + "/0";

                                            if (dataGridView1.Rows[i].Cells[HeaderName.Index].FormattedValue.ToString() == "")
                                                dataGridView1.Rows[i].Cells[HeaderName.Index].Value = ".";
                                            if (dataGridView1.Rows[i].Cells[ContNo.Index].FormattedValue.ToString() == "")
                                                dataGridView1.Rows[i].Cells[ContNo.Index].Value = ".";
                                            if (dataGridView1.Rows[i].Cells[KindofPackages.Index].FormattedValue.ToString() == "")
                                                dataGridView1.Rows[i].Cells[KindofPackages.Index].Value = ".";
                                            if (dataGridView1.Rows[i].Cells[DescriptionofGoods.Index].FormattedValue.ToString() == "")
                                                dataGridView1.Rows[i].Cells[DescriptionofGoods.Index].Value = ".";

                                            Database.myreader = Database.GetExecuteReaderCommand("select InvSubNo from Packingitems where InvNo = '"
                                             + textinvoiceno.Text + "' order by InvSubNo");
                                            while (Database.myreader.Read())
                                                invSubNo = Database.myreader[0].ToString();
                                            Database.myreader.Close();

                                            if (invSubNo != "")
                                            {
                                                string[] invTempSubNo = invSubNo.Split('/');
                                                if (DateTime.Now < Convert.ToDateTime("2017-07-01 00:00:00"))
                                                {
                                                    InvoiceSeries = Convert.ToInt32(invTempSubNo[4]);
                                                }
                                                else
                                                {
                                                    if (DateTime.Now > Convert.ToDateTime("2018-03-31 00:00:00"))
                                                        InvoiceSeries = Convert.ToInt32(invTempSubNo[4]);
                                                    else
                                                        InvoiceSeries = Convert.ToInt32(invTempSubNo[3]);
                                                }
                                            }
                                            InvoiceSeries = InvoiceSeries + 1;
                                            invSubNo = textinvoiceno.Text + "/" + InvoiceSeries.ToString();

                                            Database.GetExecuteNonQueryCommand("insert into packingitems(InvSubNo,container_no,kind_of_packages,Descr_goods,TotalQty,TotalQtyUnit,net_wt,InvNo,Rate,currancy,amount,HeaderItem,OrderNo,ItemDeptt,Commodity,HSNCode,IGSTrate,IGSTAmt,ItemCode) values('"
                                                + invSubNo + "','"
                                                + dataGridView1.Rows[i].Cells[ContNo.Index].Value.ToString() + "','"
                                                + dataGridView1.Rows[i].Cells[KindofPackages.Index].Value.ToString() + "','"
                                                + dataGridView1.Rows[i].Cells[DescriptionofGoods.Index].Value.ToString() + "',"
                                                + dataGridView1.Rows[i].Cells[Qty.Index].Value.ToString() + ",'"
                                                + dataGridView1.Rows[i].Cells[QtyUnit.Index].Value.ToString() + "',"
                                                + dataGridView1.Rows[i].Cells[NetWeight.Index].Value.ToString() + ",'"
                                                + textinvoiceno.Text + "',"
                                                + dataGridView1.Rows[i].Cells[Rate.Index].Value.ToString() + ",'"
                                                + comboCurrency.Text + "',"
                                                + dataGridView1.Rows[i].Cells[Amount.Index].Value.ToString() + ",'"
                                                + dataGridView1.Rows[i].Cells[HeaderName.Index].Value.ToString() + "','','"
                                                + dataGridView1.Rows[i].Cells[dtItemDeptt.Index].Value.ToString() + "','"
                                                + Convert.ToString(dataGridView1.Rows[i].Cells[dtCommodity.Index].Value) + "','"
                                                + Convert.ToString(dataGridView1.Rows[i].Cells[ColHSNCode.Index].Value) + "',"
                                                + Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[colIGSTRate.Index].Value) + ","
                                                + Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[colIGSTAmt.Index].Value) + ",'"
                                                + Convert.ToString(dataGridView1.Rows[i].Cells[colItemCode.Index].Value) + "')");
                                        }
                                    }
                                    if (isentry)
                                    {


                                        Database.CommitTransaction();
                                        blnTans = false;
                                        Utility.UserInformation("Save", "Commercial Invoice No " + textinvoiceno.Text + " is save successfully");


                                        double IncentiveAmount = 0;
                                        if (Database.OpenConnection(Utility.DespacthConnectionString))
                                        {
                                            Database.myreader = Database.GetExecuteReaderCommand("select [Incentive amount] from vw_MESI_Rpt where Invoice_no = '"
                                                + textinvoiceno.Text + "'");
                                            if (Database.myreader.Read())
                                                IncentiveAmount = Convert.ToDouble(Database.myreader[0].ToString());
                                            Database.myreader.Close();
                                        }
                                        MessageBox.Show("Commercial Invoice No " + textinvoiceno.Text
                                            + " is added successfully");

                                        #region PackingList
                                        if (DespatchCommonFunction.AllowMappingPackinglist(comboCompanyName.Text) > 0)
                                        {
                                            if (Database.OpenConnection(Utility.DespacthConnectionString))
                                            {
                                                Database.GetExecuteNonQueryCommand("INSERT INTO MappPackingList (CompanyName,InvNo,Yr,PackingListNo) VALUES('" +
                                                comboCompanyName.Text + "','" + textinvoiceno.Text + "','" + textSuffix.Text + "','" + despackinglist + "')");
                                            }
                                        }
                                        #endregion
                                        //#region InsertMEIS JV
                                        //if (textGRNo.Text != "0")
                                        //    DespatchCommonFunction.InsertJV(textinvoiceno.Text, IncentiveAmount, comboCompanyName.Text, textSuffix.Text, dateTimePicker_invoice.Value, 0);
                                        //#endregion

                                        dataGridView1.Rows.Clear();
                                    }
                                    if (checkImport.Checked)
                                        InsertValueInExportCntMaterial(textinvoiceno.Text, textSuffix.Text);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (blnTans)
                {
                    Database.RollBackTransaction();
                }
                ErrorMessageBox.Show(ex);
            }
        }

        private double GetCurrencyConversion()
        {
            double CurrencyConversion = 1;
            if (comboCurrency.SelectedIndex == 0) //INR
                CurrencyConversion = 1;
            else if (comboCurrency.SelectedIndex == 1) //GBP
            {
                if (Database.OpenConnection(Utility.DespacthConnectionString))
                {
                    Database.myreader = Database.GetExecuteReaderCommand("select Pound from currency order by sysdate desc");
                    if (Database.myreader.Read())
                        CurrencyConversion = Convert.ToDouble(Database.myreader[0].ToString());
                    Database.myreader.Close();
                    Database.Closeconnection();
                }
            }
            else if (comboCurrency.SelectedIndex == 2) //EURO
            {
                if (Database.OpenConnection(Utility.DespacthConnectionString))
                {
                    Database.myreader = Database.GetExecuteReaderCommand("select EURO from currency order by sysdate desc");
                    if (Database.myreader.Read())
                        CurrencyConversion = Convert.ToDouble(Database.myreader[0].ToString());
                    Database.myreader.Close();
                    Database.Closeconnection();
                }
            }
            else if (comboCurrency.SelectedIndex == 2) //Dollar
            {
                if (Database.OpenConnection(Utility.DespacthConnectionString))
                {
                    Database.myreader = Database.GetExecuteReaderCommand("select Dollar from currency order by sysdate desc");
                    if (Database.myreader.Read())
                        CurrencyConversion = Convert.ToDouble(Database.myreader[0].ToString());
                    Database.myreader.Close();
                    Database.Closeconnection();
                }
            }
            return CurrencyConversion;

        }


        private void btn_initialize_Click(object sender, EventArgs e)
        {
            try
            {
                //textAddVat.Text = "1";
                //textEduCess.Text = "0";
                //textExDuty.Text = "8";
                //textSecHiEdu.Text = "0";
                //textVat.Text = "4";
                TxtEwayNo.Text = "";
                TxtEwayNo.Enabled = true;
                textPackagingDetail.Text = "";

                lblDrawback.Visible = false;
                txtDrawBackNo.Visible = false;
                txtDrawBackNo.Text = "0";
                lblDrawbakAmt.Visible = false;
                txtDrawBackAmt.Visible = false;
                txtDrawBackAmt.Text = "0";
                chkRate.Checked = false;
                textIE_code.Text = "";
                textinvoiceno.Text = "";
                dateTimePicker_invoice.Value = DateTime.Now;
                textbuyers_orderno.Text = "";
                dateTimePicker_order.Value = DateTime.Now;
                textother_reff.Text = "";
                textbill_loading.Text = "";
                dateTimePicker_bill.Value = DateTime.Now;
                textdoc_creditno.Text = "";
                dateTimePicker_doc.Value = DateTime.Now;
                combocountry_origin.Text = "";
                combocontry_desti.Text = "";
                comboBuyerName.Text = "";
                richTextterms_dlivery.Text = "";
                textprecarriage.Text = "";
                textreceipt_precarrier.Text = "";
                textvessel_no.Text = "";
                combo_loading.Text = "";
                combo_discharge.Text = "";
                textplace_delivery.Text = "";
                texthc_containerno.Text = "";
                textline_Seal.Text = "";
                textcustom_sealno.Text = "";
                textgrosswt.Text = "";
                texttarewt.Text = "";
                textpaletwt.Text = "";
                textntwt.Text = "";
                textRemarks.Text = "";
                textPLA.Text = "";
                textRG23A.Text = "";
                comboConsigneeName.Text = "";
                comboConsigneeCity.Text = "";
                textConsignee.Text = "";
                comboCurrency.Text = "";
                comboCurrencyValue.Text = "";
                //textExDuty.Text = "";
                //textEduCess.Text = "";
                //textSecHiEdu.Text = "";
                //comboType.Text = "";
                //textFrieght.Text = "";
                //textVat.Text = "";
                //textAddVat.Text = "";
                textTCS.Text = "0";
                //comboExicseApp.Text = "";
                //comboVatApp.Text = "";
                comboCompanyName.Text = "";
                combocommodity.Text = "";
                textCBM.Text = "";
                textHtsCode.Text = "";
                lblDrawback.Visible = false;
                txtDrawBackNo.Visible = false;
                txtDrawBackNo.Text = "0";
                textBCD.Text = "0";
                textCVD.Text = "0";
                textEDCESS.Text = "0";
                textHEDCESS.Text = "0";
                textEDUCESSOEL2.Text = "0";
                textHEDUCESS.Text = "0";
                textADDVAT.Text = "0";
                textOpeningBalance.Text = "0";
                textDutyDebited.Text = "0";
                textClosingBalance.Text = "0";
                btn_save.Enabled = true;
                btn_update.Enabled = false;
                textUT1Bond.Text = "";
                if (!checkAdvanceLic.Checked)
                {
                    lblDrawback.Visible = true;
                    txtDrawBackNo.Visible = true;
                    lblDrawbakAmt.Visible = true;
                    txtDrawBackAmt.Visible = true;
                }
                else
                {
                    lblDrawback.Visible = false;
                    txtDrawBackNo.Visible = false;
                    lblDrawbakAmt.Visible = false;
                    txtDrawBackAmt.Visible = false;
                }
                if (comboCompanyName.Text != "Oswal Extrusion Limited (Unit-II)")
                {
                    textBCDPerc.Text = "0";
                    textCVDPerc.Text = "0";
                    textEDCessPerc.Text = "0";
                    textHEDcessPerc.Text = "0";
                    textEDUCESSPerc.Text = "0";
                    textHEDUCESSPerc.Text = "0";
                    textAddVatPerc.Text = "0";
                }
                if (Database.OpenConnection(Utility.DespacthConnectionString))
                {
                    textIE_code.Text = Convert.ToString(Database.GetExecuteNonQueryCommand_retrun("select importerscode from FactoryInfo where name='" + comboCompanyName.Text + "'"));
                }
                fillData();
            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
            }
        }

        private void textPLA_TextChanged(object sender, EventArgs e)
        {

        }

        private void frmpackinglist_Load(object sender, EventArgs e)
        {

        }

        private void btn_reffresh_Click(object sender, EventArgs e)
        {
            comboBuyerName.Items.Clear();
            comboBuyerName.Text = "";


            if (Database.OpenConnection(Utility.DespacthConnectionString))
            {

                string strsql = "";
                strsql = "Select CompanyName from CompanyMaster order by CompanyName";
                DespatchCommonFunction.Loadcombo(comboBuyerName, strsql, "CompanyName", "CompanyName", Utility.DespacthConnectionString);
                DespatchCommonFunction.GetCurrencyCombo(comboCurrency, Utility.DespacthConnectionString);
                DespatchCommonFunction.GetCompanyList(comboCompanyName);

                strsql = "";
                strsql = "select DisPortName from dischargeport order by DisPortName";
                DespatchCommonFunction.Loadcombo(combo_discharge, strsql, "DisPortName", "DisPortName", Utility.DespacthConnectionString);

                strsql = "";
                strsql = "select loadportname from loadingport order by loadportname";
                DespatchCommonFunction.Loadcombo(combo_loading, strsql, "loadportname", "loadportname", Utility.DespacthConnectionString);

                Database.Closeconnection();

            }


        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }

        private void comboBuyerName_SelectedIndexChanged(object sender, EventArgs e)
        {


            textConsignee.Text = "";
            comboConsigneeCity.Items.Clear();
            comboConsigneeCity.Text = "";
            if (Database.OpenConnection(Utility.DespacthConnectionString))
            {

                Database.myreader = Database.GetExecuteReaderCommand("select consignee,terms,Destination,typeofInvoice,Companycode from companymaster where "
                              + " companyname = '" + comboBuyerName.Text + "'");
                while (Database.myreader.Read())
                {
                    textConsignee.Text = Database.myreader[0].ToString();
                    richTextterms_dlivery.Text = Database.myreader[1].ToString();
                    combocontry_desti.Text = Database.myreader[2].ToString();
                    CompanyCode = Database.myreader[4].ToString();
                }
                Database.myreader.Close();
                comboConsigneeName.Items.Clear();
                comboConsigneeName.Text = "";
                string strsql = "";
                strsql = "select Distinct(Consigneename) as Consigneename from consignee where  companyCode = '" + CompanyCode + "'";
                DespatchCommonFunction.Loadcombo(comboConsigneeName, strsql, "Consigneename", "Consigneename", Utility.DespacthConnectionString);
                if (comboConsigneeName.Items.Count > 0)
                    comboConsigneeName.SelectedIndex = 0;

                //Change By Rishabh on 23-07-2026 to add To Order as Consignee by default
                comboConsigneeName.Items.Add("To Order");
                //Change End
            }

        }

        private void comboConsigneeName_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                //Change By Rishabh on 23-07-2026 to add To Order as Consignee by default
                if (comboConsigneeName.Text == "To Order")
                {
                    comboConsigneeCity.Items.Clear();
                    comboConsigneeCity.Items.Add("");
                    comboConsigneeCity.SelectedIndex = 0;
                }

                else
                {
                    using (System.Data.SqlClient.SqlConnection conn = new System.Data.SqlClient.SqlConnection(Utility.DespacthConnectionString))
                    {
                        conn.Open();
                        string strsql = "";
                        strsql = "select City from Consignee where consigneename = '" + comboConsigneeName.Text + "' and companycode ='" + CompanyCode + "'";
                        System.Data.SqlClient.SqlCommand command = new System.Data.SqlClient.SqlCommand(strsql, conn);
                        System.Data.SqlClient.SqlDataReader reader = command.ExecuteReader();
                        comboConsigneeCity.Items.Clear();
                        comboConsigneeCity.Text = "";
                        while (reader.Read())
                        {
                            comboConsigneeCity.Items.Add(reader["City"]);
                        }
                        comboConsigneeCity.SelectedIndex = 0;
                    }
                }
                //if (Database.OpenConnection(Utility.DespacthConnectionString))
                //{
                //    comboConsigneeCity.Items.Clear();
                //    comboConsigneeCity.Text = "";

                //    string strsql = "";
                //    strsql = "select City from Consignee where consigneename = '" + comboConsigneeName.Text + "' and companycode ='" + CompanyCode + "'";
                //    DespatchCommonFunction.Loadcombo(comboConsigneeCity, strsql, "City", "City", Utility.DespacthConnectionString);
                //    Database.Closeconnection();
                //    if (comboConsigneeCity.Items.Count > 0)
                //        comboConsigneeCity.SelectedIndex = 0;


                //}
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
                //Change By Rishabh on 23-07-2026 to add To Order as Consignee by default
                if (comboConsigneeCity.Text == "")
                {
                    textConsignee.Text = "";
                }

                else 
                {
                    using (System.Data.SqlClient.SqlConnection conn = new System.Data.SqlClient.SqlConnection(Utility.DespacthConnectionString))
                    {
                        conn.Open();
                        string strsql = "select ConsigneeAdd from Consignee where consigneename = '"
                           + comboConsigneeName.Text + "' and companycode ='" + CompanyCode + "' and city = '" + comboConsigneeCity.Text + "'";
                        System.Data.SqlClient.SqlCommand command = new System.Data.SqlClient.SqlCommand(strsql, conn);
                        System.Data.SqlClient.SqlDataReader reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            textConsignee.Text = reader[0].ToString();
                        }
                    }
                }
                //if (Database.OpenConnection(Utility.DespacthConnectionString))
                //{
                //    Database.myreader = Database.GetExecuteReaderCommand();
                //    if (Database.myreader.Read())
                //        textConsignee.Text = Database.myreader[0].ToString();
                //    Database.myreader.Close();
                //    Database.Closeconnection();  
                //}
            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
            }

        }

        private void btn_update_Click(object sender, EventArgs e)
        {

            string desPackinglist = "";
            bool blnTans = false;
            try
            {
                #region 19-08-2020 lock as per accounting voucher type
                if (!Utility.CheckAccountVoucherDateFreeze(dateTimePicker_invoice.Value, comboCompanyName.Text, "sales"))
                {
                    MessageBox.Show(AccountCommonVariable.ErrMsg.DATE_FREEZE + "\nPlease check Invoice Date");
                    return;
                }
                if (DespatchCommonFunction.IsAccountingDone(comboCompanyName.Text, textinvoiceno.Text, textSuffix.Text))
                {
                    MessageBox.Show("Accounting Entry is done for the selected Invoice.\nPlease Contact Account Administrator");
                    return;
                }
                #endregion

                string manufact = "";
                string manufinvno = "";
                string manufinvdt = "";
                int menu = 0;
                string isepcg = "";
                string epcg = "";
                string isadvance = "";
                string isdfia = "";
                string advance = "";
                string isimport = "";
                string rex = "";
                bool isentry = false;
                bool isItemDeptt = false;
                bool isMultilayer = false;
                #region check for Shipping Bill No
                if (!this.BlnSEZ)
                {
                    if (Database.OpenConnection(Utility.DespacthConnectionString))
                    {
                        string GSTNO = Convert.ToString(Database.GetExecuteNonQueryCommand_retrun("Select newgstno from companymaster where CompanyName='" + comboBuyerName.Text + "'"));
                        int Reccnt = Convert.ToInt32(Database.GetExecuteNonQueryCommand_retrun("select * from [ShippingBillNos]('" + comboCompanyName.Text + "','" + textSuffix.Text + "','" + GSTNO + "')"));
                        if (Reccnt > 0)
                        {
                            MessageBox.Show("Please Enter Shipping Bill No in Earlier Export Invoice");
                            return;
                        }
                    }
                }
                #endregion


                if (comboDeptt.Text == "")
                {
                    MessageBox.Show("Please enter Value in DepttName");
                    return;
                }
                //Change by Rishabh on 03-11-2025 for AD Code check
                if (comboBox1.Text == "")
                {
                    MessageBox.Show("Please enter Value in AD Code");
                    return;
                }
                //Change End
                if (combo_loading.Text.Length == 0)
                {
                    MessageBox.Show("Please Select Port of loading");
                    combo_loading.Focus();
                    return;
                }
                if (!combo_loading.Text.Contains(combo_loading.Text))
                {
                    MessageBox.Show("Please Select Port of loading");
                    combo_loading.Focus();
                    return;
                }
                //if (comboDeptt.Text == "Multilayer")
                //    isMultilayer = true;

                if (!CheckBuyerName(comboBuyerName.Text))
                    MessageBox.Show("Please enter valid buyer Name");
                else if (textgrosswt.Text == "")
                    MessageBox.Show("Please enter value in GrossWt");
                else if (texttarewt.Text == "")
                    MessageBox.Show("Please enter value in TareWt");
                else if (textpaletwt.Text == "")
                    MessageBox.Show("Please enter value in Pallet Wt");
                else if (combocommodity.Text == "")
                    MessageBox.Show("Please select Commodity");
                else if (comboContract.Text == "")
                    MessageBox.Show("Please Select Nature of contract");
                else if (comboModeofTransit.Text == "")
                    MessageBox.Show("Please Select Mode of Transit");
                else if (textPackagingDetail.Text == "" && BlnSEZ == false)
                    MessageBox.Show("Please enter packaging detail");
                else if (textplace_delivery.Text == "")
                    MessageBox.Show("Please enter Delivery Place ");
                else if (combo_loading.Text == "" && BlnSEZ == false)
                    MessageBox.Show("Please enter loading port");
                else if (combo_discharge.Text == "" && BlnSEZ == false)
                    MessageBox.Show("Please enter Discharge port");
                else if (textbuyers_orderno.Text.Length == 0 && TxtPerformaInvNo.Text.Length == 0)
                    MessageBox.Show("Please Enter Proper Buyer's Order No");
                
                
                else
                {
                    //Change by Rishabh on 03-11-2025 for cross checking AD code swift and bank swift
                    if (Database.OpenConnection(Utility.DespacthConnectionString))
                    {
                        string adcode_swift = "";
                        Database.myreader = Database.GetExecuteReaderCommand("select Swift from ExportADCodesMaster where  ADCode = '" + comboBox1.Text + "'");
                        if (Database.myreader.Read())
                            adcode_swift = Database.myreader[0].ToString();
                        Database.myreader.Close();
                        if (adcode_swift != textSwiftCode.Text)
                        {
                            MessageBox.Show("Selected AD Code and Bank Swift does not match");
                            return;
                        }
                    }
                    //Change End

                    int VenCnt = DespatchCommonFunction.CustomerLedgerName(comboBuyerName.Text, comboCompanyName.Text);
                    if (VenCnt == 0)
                    {
                        MessageBox.Show("No Account Ledger Found for this customer.\nPlease Ask Account Dept to create Ledger for this customer:- [" + comboBuyerName.Text + "]");
                        return;
                    }
                    double TotalAmt = 0;
                    double NetWt = 0;
                    for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                    {
                        if (dataGridView1.Rows[i].Cells[dtItemDeptt.Index].Value.ToString() == "")
                        {

                            MessageBox.Show("Please Select Proper ItemDeptt");
                            isItemDeptt = true;
                            break;
                        }
                        if (dataGridView1.Rows[i].Cells[dtCommodity.Index].Value.ToString() == "")
                        {

                            MessageBox.Show("Please Select Proper Commodity");
                            isItemDeptt = true;
                            break;
                        }
                        string itemcode = Convert.ToString(dataGridView1.Rows[i].Cells[colItemCode.Index].Value);
                        if (itemcode.Length == 0)
                        {
                            MessageBox.Show("Please Select Item ");
                            isItemDeptt = true;
                            break;
                        }
                        if (dataGridView1.Rows[i].Cells[Qty.Index].Value.ToString() != "" && dataGridView1.Rows[i].Cells[ContNo.Index].Value.ToString() != "" &&
                               dataGridView1.Rows[i].Cells[QtyUnit.Index].Value.ToString() != "")
                        {
                            NetWt += Convert.ToDouble(dataGridView1.Rows[i].Cells[NetWeight.Index].Value.ToString());
                            Convert.ToDouble(dataGridView1.Rows[i].Cells[Rate.Index].Value.ToString());
                            TotalAmt += Convert.ToDouble(dataGridView1.Rows[i].Cells[Amount.Index].Value.ToString());
                        }
                    }
                    textntwt.Text = NetWt.ToString();
                    if (!isItemDeptt)
                    {
                        //textFOBValue.Text = Convert.ToString(TotalAmt);
                        if (textFrieght.Text == "")
                            textFrieght.Text = "0";
                        if (textInsurance.Text == "")
                            textInsurance.Text = "0";
                        if (textCommision.Text == "")
                            textCommision.Text = "0";
                        if (textDiscount.Text == "")
                            textDiscount.Text = "0";
                        if (textOther.Text == "")
                            textOther.Text = "0";
                        if (textDeduction.Text == "")
                            textDeduction.Text = "0";

                        if (textDutyForegone.Text == "")
                            textDutyForegone.Text = "0";
                        if (textCurrencyExchangeRate.Text == "")
                            textCurrencyExchangeRate.Text = "0";
                        calculate();
                        TotalAmt += Convert.ToDouble(textFrieght.Text) + Convert.ToDouble(textInsurance.Text)
                          + Convert.ToDouble(textCommision.Text) + Convert.ToDouble(textDiscount.Text)
                          + Convert.ToDouble(textOther.Text) + Convert.ToDouble(textDeduction.Text);

                        TotalAmt += Utility.SafeConvertToDouble(textSpDuty1Amt.Text) +
                            Utility.SafeConvertToDouble(textSpDuty2Amt.Text) +
                            Utility.SafeConvertToDouble(textSpDuty3Amt.Text);

                        double CommercialAmount = 0;

                        if (comboContract.Text == "CIF")
                            CommercialAmount = Convert.ToDouble(textFOBValue.Text) + Convert.ToDouble(textFrieght.Text)
                                                  + Convert.ToDouble(textInsurance.Text);
                        else if (comboContract.Text == "CF")
                            CommercialAmount = Convert.ToDouble(textFOBValue.Text) + Convert.ToDouble(textFrieght.Text);
                        else if (comboContract.Text == "CI")
                            CommercialAmount = Convert.ToDouble(textFOBValue.Text) + Convert.ToDouble(textInsurance.Text);
                        else if (comboContract.Text == "FOB")
                            CommercialAmount = Convert.ToDouble(textFOBValue.Text);


                        double AMountinRupees = Convert.ToDouble(CommercialAmount) * Convert.ToDouble(textCurrencyExchangeRate.Text);
                        string AmountInRupeesWords = Utility.IntegerToWords(Convert.ToInt64(AMountinRupees));



                        if (comboCompanyName.Text == "Oswal Extrusion Limited (Unit-II)")
                        {
                            CommercialAmount = TotalAmt;
                            AMountinRupees = CommercialAmount * Convert.ToDouble(textCurrencyExchangeRate.Text);
                            AmountInRupeesWords = Utility.IntegerToWords(Convert.ToInt64(AMountinRupees));
                        }
                        if (AMountinRupees > 10000000)
                        {
                            MessageBox.Show("Pl check Invoice Amount or Currency Conversion , not properly fill as Amount in Rs going more then 5000000");
                        }
                        else
                        {

                            calculate();
                            #region Check Advance Lic Qty
                            if (checkAdvanceLic.Checked || checkDFIA.Checked)
                            {
                                double OrgQtyNet = 0;
                                if (Database.OpenConnection(Utility.DespacthConnectionString))
                                {
                                    OrgQtyNet = Utility.SafeConvertToDouble(Database.GetExecuteNonQueryCommand_retrun("select isnull(tot_net_wt,0) from Packinglist where Invoice_no='" + textinvoiceno.Text + "' AND yr='" + textSuffix.Text + "'"));
                                }
                                if (Utility.SafeConvertToDouble(textntwt.Text) != OrgQtyNet)
                                {
                                    double dblqty = OrgQtyNet - Utility.SafeConvertToDouble(textntwt.Text);
                                    double pendingq = DespatchCommonFunction.PendingAdvLic(comboCompanyName.Text, comboAdvLicenseNo.Text, dblqty / 1000);
                                    if (pendingq <= 0)
                                    {
                                        MessageBox.Show("Export Against This Advnace License is not possible.\nPlease Check Total Export Qty");
                                        return;
                                    }
                                }
                            }
                            #endregion
                            #region PackingList
                            if (isMultilayer == false)
                            {
                                if (DespatchCommonFunction.AllowMappingPackinglist(comboCompanyName.Text) > 0)
                                {
                                    //check with Packing List
                                    double TotalNetwt = 0;
                                    TotalNetwt = Utility.SafeConvertToDouble(textntwt.Text);


                                    desPackinglist = Convert.ToString(Database.GetExecuteNonQueryCommand_retrun("SELECT DISTINCT TOP 1 dbo.DistinctList (MappPackingList.PackingListNo ,',') as PackingListNo FROM MappPackingList WHERE companyname='"
                                        + comboCompanyName.Text + "' AND InvNo='" + textinvoiceno.Text + "' AND Yr='" + textSuffix.Text + "'"));

                                    if (desPackinglist == "")
                                    {
                                        if (!DespatchCommonFunction.GetPackingListInvoiceNo2(comboCompanyName.Text, comboBuyerName.Text, TotalNetwt, dataGridView1, colItemCode.Name, NetWeight.Name, ref desPackinglist))
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
                                                string strsql = "select round(sum(isnull(Netwt,0)),2)  Netwt from vw_PackingListAll where companyname='" + comboCompanyName.Text
                                                    + "' and   yr='" + textSuffix.Text + "' and "
                                                    + " (PACKINGLISTNO ='" + split[i] + "' or invno='" + split[i] + "') ";
                                                Database.myreader = Database.GetExecuteReaderCommand(strsql);
                                                if (Database.myreader.Read())
                                                {
                                                    totalpackinglist += Math.Round(Utility.SafeConvertToDouble(Database.myreader[0].ToString()), 2);
                                                }
                                                Database.myreader.Close();
                                                Database.Closeconnection();
                                            }
                                            if (Math.Round(Utility.SafeConvertToDouble(TotalNetwt), 2) != Math.Round(Utility.SafeConvertToDouble(totalpackinglist), 2))
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
                            #endregion

                            DialogResult drs = MessageBox.Show("Do you want to Update Packinglist", "Update PackingList"
                                , MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                            if (drs.CompareTo(DialogResult.Yes) == 0)
                            {
                                if (checkAdvanceLic.Checked)
                                {
                                    txtDrawBackNo.Text = "";
                                    txtDrawBackAmt.Text = "0";
                                }
                                //Change by Rishabh for manufacturer invno and date
                                if (checkmenufact.Checked)
                                {
                                    menu = 1;
                                    manufact = combomanufact.Text;
                                    manufinvno = textBox1.Text;
                                    manufinvdt = dateTimePicker1.Value.ToString(DespatchCommonFunction.ERPDBDateSettings());
                                }
                                //change end
                                if (checkEPCG.Checked)
                                {
                                    isepcg = "yes";
                                    epcg = comboEPCGNo.Text;
                                }
                                else
                                    isepcg = "No";
                                if (checkAdvanceLic.Checked || checkDFIA.Checked)
                                {
                                    if (checkDFIA.Checked)
                                    {
                                        isdfia = "yes";
                                        isadvance = "no";
                                    }
                                    else
                                    {
                                        isdfia = "no";
                                        isadvance = "yes";
                                    }
                                    advance = comboAdvLicenseNo.Text;
                                }
                                else
                                {
                                    isdfia = "No";
                                    isadvance = "No";
                                }

                                if (checkImport.Checked)
                                    isimport = "yes";
                                else
                                    isimport = "no";
                                //int menu = 0;
                                //if (checkmenufact.Checked)
                                //menu = 1;

                                if (Database.OpenConnection(Utility.DespacthConnectionString))
                                {

                                    blnTans = false;
                                    int InvoiceSeries = 0;
                                    Database.BeginTransaction();
                                    blnTans = true;
                                    Database.GetExecuteNonQueryCommand("delete from packinglist where invoice_no ='" + textinvoiceno.Text + "'");
                                    Database.GetExecuteNonQueryCommand("delete from packingitems where invno = '" + textinvoiceno.Text + "'");


                                    string[] invTempSubNo = textinvoiceno.Text.Split('/');
                                    if (textinvoiceno.Text.Contains("HO"))
                                        InvoiceSeries = Convert.ToInt32(invTempSubNo[3]);
                                    else
                                        InvoiceSeries = Convert.ToInt32(invTempSubNo[2]);


                                    if (Database.GetExecuteNonQueryCommand("insert into packinglist(IE_code,Invoice_no,Invoice_dt,Buyers_od_no,Buyers_od_dt,Reference,Bill_leading_no,Bill_leading_dt,Doc_cred_no,Doc_dt,country_origin,country_desti,Buyers,terms_payement,pre_carrige,place_receipt,vessl_flteno,port_loading,port_discharge,place_delivery,Hc_containerno,line_seal,cusom_seal_no,gross_wt,tare_wt,pallet_wt,tot_net_wt,yr,remarks,Remarks2,rg23a,consignee,cons_city,cons_ntbyer,currancy,cur_value,exduty,educess,sechiedu,deptt,CommercialAmount,vat,addvat,tcs,excise,vatapp,CompanyName,thirdparty,manucheckbox,Notifier,ContType,AmtWord,TotalAmt,HTSCODE,TotalCBM,commodity,bankdetail,EpcgNo,advno,utibondno,isepcg,isadvlic,beingexport,isdfia,isimport,currancyconversion,exciseper,educessper,hieducessper,areno,FOBValue,FrieghtAmount,Insurance,Commision,Discount,Other,Deduction,DutyForegone,AMountinRupees,AmountInRupeesWords,GRNo,GRDate,PackingMaterialWt,typeofshipment,NatureofContract,invno,authorised,AccountHead,BCDPERC,CVDPerc,EDCessPerc,HEDCessPerc,EDuCEssPerc,HeducessPerc,AddVatPerc,BCD,CVD,EDCess,HEDCess,EDuCEssoel2,Heducess,AddVatOel2,OpeningBalance,DutyDebited,ClosingBalance,Ismarketing,deliveryterms,ThirdPartyName,IsthirdParty,modeoftransit,packagingdetial,FreightRemarks,FreightRemarks2,FreightAmt2,InlandHaulageRem,InlandHaulageAmt,OtherDuty,OtherDutyAmt,DiscountRem,SpDutyRemarks2,SpDutyAmt2,SpDutyRemarks3,SpDutyAmt3,PerformaInvoiceNo,ADCODE,TransportName,VehicalNo,InvType,LRNO,dIECAAdress,Note,BillofExportNo,BillofExportDate,ShippingBillNo,ShippingBDTE,drawbackno,drawbackAmt,PortCode,tcsper,manufinvno,manufdt,contbooking,totalpkg,stuffing_plant,cont_weight,cont_tare_wt,WeightmentDt,Wt_SlipNo) values('"
                                        + textIE_code.Text + "','"
                                        + textinvoiceno.Text.ToUpper().ToString() + "','"
                                        + dateTimePicker_invoice.Value.ToString(DespatchCommonFunction.ERPDBDateSettings()) + "','"
                                        + textbuyers_orderno.Text + "','"
                                        + dateTimePicker_order.Value.ToString(DespatchCommonFunction.ERPDBDateSettings()) + "','"
                                        + textother_reff.Text.Replace("'", "''") + "','"
                                        + textbill_loading.Text + "','"
                                        + dateTimePicker_bill.Value.ToString(DespatchCommonFunction.ERPDBDateSettings()) + "','"
                                        + textdoc_creditno.Text + "','"
                                        + dateTimePicker_doc.Value.ToString(DespatchCommonFunction.ERPDBDateSettings()) + "','"
                                        + combocountry_origin.Text + "','"
                                        + combocontry_desti.Text + "','"
                                        + comboBuyerName.Text + "','"
                                        + comboPaymentTerms.Text + "','"
                                        + textprecarriage.Text + "','"
                                        + textreceipt_precarrier.Text + "','"
                                        + textvessel_no.Text + "','"
                                        + combo_loading.Text + "','"
                                        + combo_discharge.Text + "','"
                                        + textplace_delivery.Text + "','"
                                        + texthc_containerno.Text + "','"
                                        + textline_Seal.Text + "','"
                                        + textcustom_sealno.Text + "',"
                                        + textgrosswt.Text + ","
                                        + texttarewt.Text + ","
                                        + textpaletwt.Text + ","
                                        + textntwt.Text + ",'"
                                        + textSuffix.Text + "','"
                                        + textRemarks.Text + "','"
                                        + textPLA.Text + "','"
                                        + textRG23A.Text + "','"
                                        + comboConsigneeName.Text + "','"
                                        + comboConsigneeCity.Text + "','"
                                        + textConsignee.Text + "','"
                                        + comboCurrency.Text + "','" +
                                        comboCurrencyValue.Text + "'," +
                                        " 0,0,0,'" + comboDeptt.Text + "'," + CommercialAmount + ",0,0," + TCS + ",'0','0','"
                                        + comboCompanyName.Text + "','"
                                        + manufact + "',"
                                        + menu + ",'"
                                        + textNotifier.Text + "','"
                                        + comboContType.Text.Replace("'", "''") + "','"
                                        + Utility.IntegerToWords(Convert.ToInt64(TotalAmt)) + "',"
                                        + Convert.ToDouble(TotalAmt) + ",'"
                                        + textHtsCode.Text + "',"
                                    + textCBM.Text + ",'"
                                    + combocommodity.Text + "','"
                                    + textBankDetail.Text + "','"
                                        //Change By Rishabh on 03-11-2025 no longer needed
                                        //+ textBankDate.Text + "','"
                                        //Change End
                                    + epcg + "','"
                                    + advance + "','"
                                    + textUT1Bond.Text + "','"
                                    + isepcg + "','"
                                    + isadvance + "','"
                                    + comboBeingExport.Text + "','"
                                    + isdfia + "','"
                                    + isimport + "',"
                                    + textCurrencyExchangeRate.Text + ","
                                    + textExcise.Text + ","
                                    + textEducess.Text + ","
                                     + texthiEducess.Text + ",'"
                                     + textAreNo.Text + "'," +
                                       textFOBValue.Text + ","
                                       + textFrieght.Text + ","
                                       + textInsurance.Text + ","
                                       + textCommision.Text + ","
                                       + textDiscount.Text + ","
                                       + textOther.Text + ","
                                       + textDeduction.Text + ","
                                       + textDutyForegone.Text + ","
                                       + AMountinRupees + ",'"
                                       + AmountInRupeesWords
                                       + "','" + textGRNo.Text
                                       + "','" + dateTimeGRNo.Value.ToString(DespatchCommonFunction.ERPDBDateSettings())
                                       + "'," + textPackingMaterial.Text
                                       + ",'" + comboShipment.Text + "','"
                                       + comboContract.Text
                                       + "'," + InvoiceSeries
                                       + ",'" + textAuthorised.Text
                                       + "','" + comboAccountHead.Text + "',"
                                       + textBCDPerc.Text + ","
                                       + textCVDPerc.Text + ","
                                       + textEDCessPerc.Text + ","
                                       + textHEDcessPerc.Text + ","
                                       + textEDUCESSPerc.Text + ","
                                       + textHEDUCESSPerc.Text + ","
                                        + textAddVatPerc.Text + ","
                                       + textBCD.Text + ","
                                       + textCVD.Text + ","
                                       + textEDCESS.Text + ","
                                       + textHEDCESS.Text + ","
                                       + textEDUCESSOEL2.Text + ","
                                       + textHEDUCESS.Text + ","
                                       + textADDVAT.Text + ","
                                       + textOpeningBalance.Text + ","
                                       + textDutyDebited.Text + ","
                                       + textClosingBalance.Text + ",'no','"
                                       + richTextterms_dlivery.Text + "','','no','"
                                       + comboModeofTransit.Text + "','" + textPackagingDetail.Text + "','','',0,'',0,'',0,'','',0,'',0,'"
                                       + TxtPerformaInvNo.Text + "','"
                                        //Change by Rishabh on 03-11-2025 as txt box replaced by combobox
                                        //TxtADCODE.Text
                                       + comboBox1.Text + "','"
                                        //Change End
                                       + comboTransporter.Text + "','" + comboVehicalNo.Text + "','"
                                       + (BlnSEZ == true ? "SEZ" : "DIRECT") + "','"
                                       + txtlrno.Text + "','" + TxtIECAdd.Text + "','" + comboNote.Text + "','"
                                       + comboBillOfExportNo.Text + "','" + dateTimeBillofExportDate.Value.ToString(DespatchCommonFunction.ERPDBDateSettings()) + "','"
                                       + txtshpingbill.Text + "'," + (dtShipDt.Checked == false ? "NULL" : "'" + dtShipDt.Value + "'")
                                       + ",'" + txtDrawBackNo.Text + "'," + Utility.SafeConvertToDouble(txtDrawBackAmt.Text) + ",'"
                                       + combo_loading.SelectedValue + "'," + Utility.SafeConvertToDouble(textTCS.Text)
                                       + ",'" + manufinvno + "'"
                                       + ",'" + manufinvdt + "'"
                                       //Change by Rishabh on 10-11-2025 to add two new columns for container booking no, stuffing plant and total no of packages.
                                       + ",'" + contbooking.Text + "'"
                                       + ",'" + totalpkg.Text + "'"
                                       + ",'" + stuffing_plant.Text + "'"
                                       + ",'" + cont_weight.Text + "'"
                                       + ",'" + cont_tare_wt.Text + "'"
                                       + ",'" + WeightmentDt.Value.ToString(DespatchCommonFunction.ERPDBDateSettings()) + "'"
                                       + ",'" + Wt_SlipNo.Text + "')"))
                                       //Change End
                                    {
                                        isentry = true;
                                    }
                                    //FreightRemarks2,FreightAmt2,InlandHaulageRem,InlandHaulageAmt,OtherDuty,OtherDutyAmt
                                    Database.GetExecuteNonQueryCommand("UPDATE PACKINGLIST SET SpDutyRemarks2='" + textSpDutyRemark2.Text + "'," +
                                      " SpDutyAmt2=" + Utility.SafeConvertToDouble(textSpDuty2Amt.Text) + "," +
                                      " SpDutyRemarks3='" + textSpDutyRemarks3.Text + "'," +
                                      " SpDutyAmt3=" + Utility.SafeConvertToDouble(textSpDuty3Amt.Text) + "," +
                                      " SpDutyRemarks1='" + textSpDutyRemarks1.Text + "'," +
                                      " SpDutyAmt1 =" + Utility.SafeConvertToDouble(textSpDuty1Amt.Text) + "," +
                                      " FreightRemarks2='" + txtSeaFrieghtRem.Text + "',FreightAmt2=" + Utility.SafeConvertToDouble(txtSeaFreAmt.Text) + "," +
                                      " InlandHaulageRem='" + TxtInLandHauRem.Text + "',InlandHaulageAmt=" + Utility.SafeConvertToDouble(TxtHaulageAmt.Text) + "," +
                                      " OtherDuty='" + TxtOtherRem.Text + "',OtherDutyAmt = " + Utility.SafeConvertToDouble(TxtOthersDiscAmt.Text) +
                                      " ,IsInvRate=" + (chkRate.Checked ? 1 : 0) +
                                      " WHERE  Invoice_no='" + textinvoiceno.Text + "' AND YR='" + textSuffix.Text + "'");


                                    Database.GetExecuteNonQueryCommand("UPDATE PACKINGLIST SET BeneficiaryAccountNo='" + textBenefiaciaryAccoutNo.Text + "'," +
                                       " Swiftcode='" + textSwiftCode.Text + "'," +
                                       " CorrespondingBank='" + textCorrespondingBank.Text + "'," +
                                      " CorrespondingBankSwift='" + textCorrespondingBankSwift.Text + "', IBANAccountNo='" + textIBANAccoutNo.Text + "'" +
                                      " WHERE  Invoice_no='" + textinvoiceno.Text + "' AND YR='" + textSuffix.Text + "'");
                                    //Change by Rishabh for Rex Declaration
                                    if (checkBox1.Checked)
                                    {
                                        Database.myreader = Database.GetExecuteReaderCommand("select Declaration from RexDeclaration where CompanyName = '" + comboCompanyName.Text + "'");
                                        while (Database.myreader.Read())
                                        {
                                            rex = Database.myreader[0].ToString();
                                        }
                                        Database.myreader.Close();
                                        Database.GetExecuteNonQueryCommand("update packinglist set isRex = 'yes', Remarks = ISNULL(remarks,'') + CASE WHEN Remarks IS NULL THEN '' ELSE CHAR(13) + CHAR(10) END + '" + rex + "' where Invoice_No = '" + textinvoiceno.Text + "' and Yr = '" + textSuffix.Text + "'");
                                    }
                                    //Change end
                                    //have started from here 

                                    for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                                    {

                                        if (dataGridView1.Rows[i].Cells[Qty.Index].Value.ToString() != "" &&
                                                dataGridView1.Rows[i].Cells[KindofPackages.Index].Value.ToString() != "" &&
                                                dataGridView1.Rows[i].Cells[QtyUnit.Index].Value.ToString() != "")
                                        {
                                            InvoiceSeries = 0;
                                            string invSubNo = textinvoiceno.Text + "/0";

                                            Database.myreader = Database.GetExecuteReaderCommand("select InvSubNo from Packingitems where InvNo = '"
                                             + textinvoiceno.Text + "' order by InvSubNo");
                                            while (Database.myreader.Read())
                                                invSubNo = Database.myreader[0].ToString();
                                            Database.myreader.Close();

                                            if (invSubNo != "")
                                            {
                                                invTempSubNo = invSubNo.Split('/');
                                                if (DateTime.Now < Convert.ToDateTime("2017-07-01 00:00:00"))
                                                {
                                                    if (invSubNo.Contains("HO"))
                                                        InvoiceSeries = Convert.ToInt32(invTempSubNo[5]);
                                                    else
                                                        InvoiceSeries = Convert.ToInt32(invTempSubNo[4]);
                                                }
                                                else
                                                {
                                                    if (invSubNo.Contains("HO"))
                                                        InvoiceSeries = Convert.ToInt32(invTempSubNo[4]);
                                                    else
                                                    {
                                                        if (textinvoiceno.Text.Contains("17-18"))
                                                            InvoiceSeries = Convert.ToInt32(invTempSubNo[4]);
                                                        else
                                                            if (textSuffix.Text.Contains("17&18"))
                                                                InvoiceSeries = Convert.ToInt32(invTempSubNo[3]);
                                                            else
                                                                InvoiceSeries = Convert.ToInt32(invTempSubNo[4]);
                                                    }
                                                }

                                            }
                                            InvoiceSeries = InvoiceSeries + 1;
                                            invSubNo = textinvoiceno.Text + "/" + InvoiceSeries.ToString();


                                            Database.GetExecuteNonQueryCommand("insert into packingitems(InvSubNo,container_no,kind_of_packages,Descr_goods,TotalQty,TotalQtyUnit,net_wt,InvNo,Rate,currancy,amount,HeaderItem,OrderNo,ItemDeptt,Commodity,HSNCode,IGSTrate,IGSTAmt,ItemCode) values('"
                                               + invSubNo + "','"
                                               + dataGridView1.Rows[i].Cells[ContNo.Index].Value.ToString() + "','"
                                               + dataGridView1.Rows[i].Cells[KindofPackages.Index].Value.ToString() + "','"
                                               + dataGridView1.Rows[i].Cells[DescriptionofGoods.Index].Value.ToString() + "',"
                                               + dataGridView1.Rows[i].Cells[Qty.Index].Value.ToString() + ",'"
                                               + dataGridView1.Rows[i].Cells[QtyUnit.Index].Value.ToString() + "',"
                                               + dataGridView1.Rows[i].Cells[NetWeight.Index].Value.ToString() + ",'"
                                               + textinvoiceno.Text + "',"
                                               + dataGridView1.Rows[i].Cells[Rate.Index].Value.ToString() + ",'"
                                               + comboCurrency.Text + "',"
                                               + dataGridView1.Rows[i].Cells[Amount.Index].Value.ToString() + ",'"
                                               + dataGridView1.Rows[i].Cells[HeaderName.Index].Value.ToString() + "','','"
                                               + dataGridView1.Rows[i].Cells[dtItemDeptt.Index].Value.ToString() + "','"
                                               + Convert.ToString(dataGridView1.Rows[i].Cells[dtCommodity.Index].Value) + "','"
                                               + Convert.ToString(dataGridView1.Rows[i].Cells[ColHSNCode.Index].Value) + "',"
                                               + Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[colIGSTRate.Index].Value) + ","
                                               + Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[colIGSTAmt.Index].Value) + ",'"
                                               + Convert.ToString(dataGridView1.Rows[i].Cells[colItemCode.Index].Value) + "')");

                                            //Database.GetExecuteNonQueryCommand("insert into packingitems(InvSubNo,container_no,kind_of_packages,Descr_goods,TotalQty,TotalQtyUnit,net_wt,InvNo,Rate,currancy,amount,HeaderItem,OrderNo,ItemDeptt,Commodity) values('"
                                            //  + invSubNo + "','"
                                            //  + dataGridView1.Rows[i].Cells[ContNo.Index].Value.ToString() + "','"
                                            //  + dataGridView1.Rows[i].Cells[KindofPackages.Index].Value.ToString() + "','"
                                            //  + dataGridView1.Rows[i].Cells[DescriptionofGoods.Index].Value.ToString() + "',"
                                            //  + dataGridView1.Rows[i].Cells[Qty.Index].Value.ToString() + ",'"
                                            //  + dataGridView1.Rows[i].Cells[QtyUnit.Index].Value.ToString() + "',"
                                            //  + dataGridView1.Rows[i].Cells[NetWeight.Index].Value.ToString() + ",'"
                                            //  + textinvoiceno.Text + "',"
                                            //  + dataGridView1.Rows[i].Cells[Rate.Index].Value.ToString() + ",'"
                                            //  + comboCurrency.Text + "',"
                                            //  + dataGridView1.Rows[i].Cells[Amount.Index].Value.ToString() + ",'"
                                            //  + dataGridView1.Rows[i].Cells[HeaderName.Index].Value.ToString() + "','','"
                                            //  + dataGridView1.Rows[i].Cells[dtItemDeptt.Index].Value.ToString() + "','"
                                            //  + dataGridView1.Rows[i].Cells[dtCommodity.Index].Value + "')");

                                        }
                                    }
                                    Database.CommitTransaction();
                                    blnTans = false;
                                    Utility.UserInformation("Update", "Commercial Invoice No " + textinvoiceno.Text + " is updated successfully");

                                    if (checkImport.Checked)
                                        Update_ValueInExportCntMaterial(textinvoiceno.Text, textSuffix.Text);


                                    double IncentiveAmount = 0;
                                    if (Database.OpenConnection(Utility.DespacthConnectionString))
                                    {
                                        Database.myreader = Database.GetExecuteReaderCommand("select [Incentive amount] from vw_MESI_Rpt where Invoice_no = '"
                                            + textinvoiceno.Text + "'");
                                        if (Database.myreader.Read())
                                            IncentiveAmount = Convert.ToDouble(Database.myreader[0].ToString());
                                        Database.myreader.Close();
                                    }
                                    if (Database.OpenConnection(Utility.DespacthConnectionString))
                                    {
                                        if (textGRNo.Text != "0")
                                            DespatchCommonFunction.InsertJV(textinvoiceno.Text, IncentiveAmount, comboCompanyName.Text, textSuffix.Text, dateTimePicker_invoice.Value, 1);
                                    }
                                    if (DespatchCommonFunction.AllowMappingPackinglist(comboCompanyName.Text) > 0)
                                    {
                                        if (Database.OpenConnection(Utility.DespacthConnectionString))
                                        {
                                            Database.GetExecuteNonQueryCommand("Delete from MappPackingList where InvNo='" + textinvoiceno.Text + "' and yr='"
                                                + textSuffix.Text + "' and CompanyName='" + comboCompanyName.Text + "'");
                                            Database.GetExecuteNonQueryCommand("INSERT INTO MappPackingList (CompanyName,InvNo,Yr,PackingListNo) VALUES('" +
                                            comboCompanyName.Text + "','" + textinvoiceno.Text + "','" + textSuffix.Text + "','" + desPackinglist + "')");
                                        }
                                    }
                                    if (isentry)
                                        MessageBox.Show("Commercial Invoice No " + textinvoiceno.Text
                                            + " is updated successfully");


                                    fillData();
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (blnTans)
                    Database.RollBackTransaction();
                ErrorMessageBox.Show(ex);
            }
        }

        private void checkmenufact_CheckedChanged(object sender, EventArgs e)
        {
            if (checkmenufact.Checked)
                groupmanufact.Visible = true;
            else
                groupmanufact.Visible = false;
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            TabClass.OpenForm(new frmPrintCommercaiReport());
            //frmPrintCommercaiReport frm = new frmPrintCommercaiReport();
            //frm.Show();
        }

        private void textinvoiceno_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            DisplayRecord();
        }
        private void DisplayRecord()
        {
            try
            {

                string Currency = "";
                string BuyerName = "";
                if (Database.OpenConnection(Utility.DespacthConnectionString))
                {
                    bool ismarketing = false;

                    Database.myreader = Database.GetExecuteReaderCommand("select IsMarketing from packinglist where invoice_no = '"
                  + textinvoiceno.Text + "' and ismarketing != 'yes'");
                    if (Database.myreader.Read())
                        ismarketing = true;
                    Database.myreader.Close();
                    if (!ismarketing)
                        MessageBox.Show("Please Update InvNo in Commercial Invoice With Order No ");
                    else
                    {
                        fillData(true);
                        Database.myreader = Database.GetExecuteReaderCommand("select * from packinglist where invoice_no = '"
                          + textinvoiceno.Text + "'");
                        if (Database.myreader.Read())
                            BuyerName = Database.myreader[12].ToString();
                        Database.myreader.Close();
                        if (!comboBuyerName.Items.Contains(BuyerName))
                            comboBuyerName.Items.Add(BuyerName);
                        comboBuyerName.Text = BuyerName;


                        Database.OpenConnection(Utility.DespacthConnectionString);
                        Database.myreader = Database.GetExecuteReaderCommand("select * from packinglist where invoice_no = '"
                          + textinvoiceno.Text + "'");
                        if (Database.myreader.Read())
                        {
                            textIE_code.Text = Database.myreader[0].ToString();
                            dateTimePicker_invoice.Value = (DateTime)Database.myreader[2];
                            textbuyers_orderno.Text = Database.myreader[3].ToString();
                            dateTimePicker_order.Value = (DateTime)Database.myreader[4];
                            textother_reff.Text = Database.myreader[5].ToString();
                            textbill_loading.Text = Database.myreader[6].ToString();

                            //DateTime dt = DateTime.Parse(.ToString());
                            dateTimePicker_bill.Value = (DateTime)Database.myreader[7];

                            comboContType.Text = Database.myreader["conttype"].ToString();
                            textdoc_creditno.Text = Database.myreader[8].ToString();
                            dateTimePicker_doc.Value = (DateTime)Database.myreader[9];
                            combocountry_origin.Text = Database.myreader[10].ToString();
                            combocontry_desti.Text = Database.myreader[11].ToString();
                            comboPaymentTerms.Text = Database.myreader[13].ToString();
                            textprecarriage.Text = Database.myreader[14].ToString();
                            textreceipt_precarrier.Text = Database.myreader[15].ToString();
                            textvessel_no.Text = Database.myreader[16].ToString();
                            combo_loading.Text = Database.myreader[17].ToString();
                            combo_loading.SelectedValue = Convert.ToString(Database.myreader["PortCode"]);
                            combo_discharge.Text = Database.myreader[18].ToString();
                            textplace_delivery.Text = Database.myreader[19].ToString();
                            texthc_containerno.Text = Database.myreader[20].ToString(); ;
                            textline_Seal.Text = Database.myreader[21].ToString();
                            textcustom_sealno.Text = Database.myreader[22].ToString();
                            textgrosswt.Text = Database.myreader[23].ToString();
                            texttarewt.Text = Database.myreader[24].ToString();
                            textpaletwt.Text = Database.myreader[25].ToString();
                            textntwt.Text = Database.myreader[26].ToString();
                            textSuffix.Text = Database.myreader[27].ToString();
                            textRemarks.Text = Database.myreader[28].ToString();
                            textPLA.Text = Database.myreader[29].ToString();
                            textRG23A.Text = Database.myreader[30].ToString();
                            comboConsigneeName.Text = Database.myreader[31].ToString();
                            comboConsigneeCity.Text = Database.myreader[32].ToString();
                            textConsignee.Text = Database.myreader[33].ToString();
                            Currency = Database.myreader[34].ToString();
                            textTCS.Text = Utility.SafeConvertToDouble(Database.myreader["tcsper"]).ToString();
                            TCS = Utility.SafeConvertToDouble(Database.myreader["tcs"]);
                            //textExDuty.Text = Database.myreader[36].ToString();
                            //textEduCess.Text = Database.myreader[37].ToString();
                            //textSecHiEdu.Text = Database.myreader[38].ToString();
                            //comboType.Text = Database.myreader[39].ToString();
                            //textFrieght.Text = Database.myreader[40].ToString();
                            //textVat.Text = Database.myreader[41].ToString();
                            //textAddVat.Text = Database.myreader[42].ToString();
                            //textTCS.Text = Database.myreader[43].ToString();
                            //comboExicseApp.Text = Database.myreader[44].ToString();
                            //comboVatApp.Text = Database.myreader[45].ToString();
                            comboCompanyName.Text = Database.myreader[46].ToString();
                            textHtsCode.Text = Database.myreader[53].ToString();
                            textCBM.Text = Database.myreader[54].ToString();
                            textAuthorised.Text = Database.myreader["Authorised"].ToString();
                            comboDeptt.Text = Database.myreader["Deptt"].ToString();

                            comboAccountHead.Text = Database.myreader["AccountHead"].ToString();
                            textBCDPerc.Text = Database.myreader[89].ToString();
                            textCVDPerc.Text = Database.myreader[90].ToString();
                            textEDCessPerc.Text = Database.myreader[91].ToString();
                            textHEDcessPerc.Text = Database.myreader[92].ToString();
                            textEDUCESSPerc.Text = Database.myreader[93].ToString();
                            textHEDUCESSPerc.Text = Database.myreader[94].ToString();
                            textAddVatPerc.Text = Database.myreader[95].ToString();
                            textBCD.Text = Database.myreader[96].ToString();
                            textCVD.Text = Database.myreader[97].ToString();
                            textEDCESS.Text = Database.myreader[98].ToString();
                            textHEDCESS.Text = Database.myreader[99].ToString();
                            textEDUCESSOEL2.Text = Database.myreader[100].ToString();
                            textHEDUCESS.Text = Database.myreader[101].ToString();
                            textADDVAT.Text = Database.myreader[102].ToString();
                            textOpeningBalance.Text = Database.myreader[103].ToString();
                            textDutyDebited.Text = Database.myreader[104].ToString();
                            textClosingBalance.Text = Database.myreader[105].ToString();
                            richTextterms_dlivery.Text = Database.myreader[107].ToString();
                            comboModeofTransit.Text = Database.myreader[110].ToString();
                            textPackagingDetail.Text = Database.myreader[111].ToString();
                            TxtPerformaInvNo.Text = Database.myreader["PerformaInvoiceNo"].ToString();
                            //Change by Rishabh on 03-11-2025 as txt box replaced by combobox
                            //TxtADCODE.Text = Database.myreader["ADCODE"].ToString();
                            comboBox1.Text = Database.myreader["ADCODE"].ToString();
                            //Change End
                            txtshpingbill.Text = "";
                            txtshpingbill.Text = Convert.ToString(Database.myreader["ShippingBillNo"]);
                            dtShipDt.Checked = false;
                            int israte = Convert.ToInt32(Database.myreader["IsInvrate"]);
                            chkRate.Checked = (israte == 1 ? true : false);
                            if (Convert.ToString(Database.myreader["ShippingBDTE"]) == DBNull.Value.ToString())
                            {
                                dtShipDt.Checked = true;
                                Utility.setDateTimePickerValue(dtShipDt, Database.myreader["ShippingBDTE"]);
                            }


                            textSpDuty1Amt.Text = Database.myreader["SpDutyAmt1"].ToString();
                            textSpDutyRemarks1.Text = Database.myreader["SpDutyRemarks1"].ToString();
                            textSpDutyRemark2.Text = Convert.ToString(Database.myreader["SpDutyRemarks2"]);
                            textSpDutyRemarks3.Text = Convert.ToString(Database.myreader["SpDutyRemarks3"]);
                            textSpDuty2Amt.Text = Convert.ToString(Database.myreader["SpDutyAmt2"]);
                            textSpDuty3Amt.Text = Convert.ToString(Database.myreader["SpDutyAmt3"]);
                            //FOB Value Additionla Values

                            txtDrawBackNo.Text = Convert.ToString(Database.myreader["drawbackno"]);
                            txtDrawBackAmt.Text = Utility.SafeConvertToDouble(Database.myreader["drawbackAmt"]).ToString();

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


                            //End FOB
                            comboVehicalNo.Text = Convert.ToString(Database.myreader["VehicalNo"]);
                            comboTransporter.Text = Convert.ToString(Database.myreader["TransportName"]);
                            comboNote.Text = Convert.ToString(Database.myreader["Note"]);
                            comboBillOfExportNo.Text = Convert.ToString(Database.myreader["BillofExportNo"]);
                            if (Database.myreader["BillofExportDate"].ToString().Length > 0)
                                dateTimeBillofExportDate.Value = Convert.ToDateTime(Database.myreader["BillofExportDate"].ToString());
                            comboVehicalNo.Text = Convert.ToString(Database.myreader["VehicalNo"]);
                            BlnSEZ = false;
                            if (Convert.ToString(Database.myreader["InvType"]) == "SEZ")
                                BlnSEZ = true;
                            txtlrno.Text = Convert.ToString(Database.myreader["LRNO"]);
                            combocommodity.Text = Database.myreader[55].ToString();


                        }
                        Database.myreader.Close();
                        Database.Closeconnection();

                        if (Database.OpenConnection(Utility.DespacthConnectionString))
                        {
                            Database.myreader = Database.GetExecuteReaderCommand("select * from packinglist where invoice_no = '"
                              + textinvoiceno.Text + "'");
                            if (Database.myreader.Read())
                            {
                                textBankDetail.Text = Database.myreader[56].ToString();
                                //Change By Rishabh on 03-11-2025 no longer needed
                                //textBankDate.Text = Database.myreader[57].ToString();
                                //Change End
                                if (Convert.ToBoolean(Database.myreader["manucheckbox"]))
                                {
                                    checkmenufact.Checked = true;
                                    combomanufact.Text = Database.myreader[47].ToString();
                                    textBox1.Text = Database.myreader[153].ToString();
                                    dateTimePicker1.Value = (DateTime)Database.myreader[154];
                                }
                                else
                                    checkmenufact.Checked = false;
                                btn_save.Enabled = false;
                                btn_update.Enabled = true;
                                if (Database.myreader["isepcg"].ToString() == "yes")
                                {
                                    checkEPCG.Checked = true;
                                    comboEPCGNo.Text = Database.myreader["epcgno"].ToString();
                                }

                                comboBeingExport.Text = Database.myreader["beingexport"].ToString();
                                textUT1Bond.Text = Database.myreader["utibondno"].ToString();
                                if (Database.myreader["isimport"].ToString() == "yes")
                                    checkImport.Checked = true;
                                else
                                    checkImport.Checked = false;
                                textCurrencyExchangeRate.Text = Database.myreader["currancyconversion"].ToString();
                                textEducess.Text = Database.myreader["educessper"].ToString();
                                textExcise.Text = Database.myreader["exciseper"].ToString();
                                texthiEducess.Text = Database.myreader["hieducessper"].ToString();
                                textAreNo.Text = Database.myreader["areno"].ToString();
                                textFOBValue.Text = Database.myreader["FOBValue"].ToString();
                                textFrieght.Text = Database.myreader["Frieghtamount"].ToString();
                                textInsurance.Text = Database.myreader["Insurance"].ToString();
                                textCommision.Text = Database.myreader["Commision"].ToString();

                                textDiscount.Text = Database.myreader["Discount"].ToString();
                                textOther.Text = Database.myreader["Other"].ToString();
                                textDeduction.Text = Database.myreader["Deduction"].ToString();
                                textDutyForegone.Text = Database.myreader["DutyForegone"].ToString();
                                textGRNo.Text = Database.myreader["GRNo"].ToString();
                                //dateTimeGRNo.Value = (DateTime)Database.myreader["GRDAte"];
                                dateTimeGRNo.Value = Convert.ToDateTime(Database.myreader["GRDAte"].ToString());
                                textPackingMaterial.Text = Database.myreader["Packingmaterialwt"].ToString();

                                textNotifier.Text = Database.myreader["Notifier"].ToString();
                                comboShipment.Text = Database.myreader["TypeofShipment"].ToString();
                                comboContract.Text = Database.myreader["NatureofContract"].ToString();

                                textBenefiaciaryAccoutNo.Text = Database.myreader["BeneficiaryAccountNo"].ToString();
                                textSwiftCode.Text = Database.myreader["Swiftcode"].ToString();
                                textCorrespondingBank.Text = Database.myreader["CorrespondingBank"].ToString();
                                textCorrespondingBankSwift.Text = Database.myreader["CorrespondingBankSwift"].ToString();
                                textIBANAccoutNo.Text = Database.myreader["IBANAccountNo"].ToString();

                            }

                            if (Database.myreader["isadvlic"].ToString() == "yes")
                            {
                                comboAdvLicenseNo.Text = Database.myreader["advno"].ToString();
                                checkAdvanceLic.Checked = true;
                                checkDFIA.Visible = false;
                                checkDFIA.Checked = false;
                                checkAdvanceLic.Visible = true;
                            }
                            else if (Database.myreader["isdfia"].ToString() == "yes")
                            {
                                comboAdvLicenseNo.Text = Database.myreader["advno"].ToString();
                                checkAdvanceLic.Checked = false;
                                checkDFIA.Visible = true;
                                checkDFIA.Checked = true;
                                checkAdvanceLic.Visible = false;

                            }
                            if (Database.myreader["isRex"].ToString() == "yes")
                            {
                                checkBox1.Checked = true;
                            }
                            contbooking.Text = Database.myreader["contbooking"].ToString();
                            totalpkg.Text = Database.myreader["totalpkg"].ToString();
                            stuffing_plant.Text = Database.myreader["stuffing_plant"].ToString();
                            cont_weight.Text = Database.myreader["cont_weight"].ToString();
                            cont_tare_wt.Text = Database.myreader["cont_tare_wt"].ToString();
                            WeightmentDt.Value = (DateTime)Database.myreader["WeightmentDt"];
                            Wt_SlipNo.Text = Database.myreader["Wt_SlipNo"].ToString();

                            Database.myreader.Close();
                            comboCurrency.Text = Currency;

                            dataGridView1.Rows.Clear();

                            Database.OpenConnection(Utility.DespacthConnectionString);
                            object obj = Database.GetExecuteScalarCommand_return("select Count(*) from packingitems where invno = '" + textinvoiceno.Text + "'");
                            int count = Convert.ToInt32(obj);
                            if (count > 0)
                                dataGridView1.Rows.Add(count);
                            Database.Closeconnection();


                            if (Database.OpenConnection(Utility.DespacthConnectionString))
                            {
                                int i = 0;
                                Database.myreader = Database.GetExecuteReaderCommand("select * from packingitems where invno = '"
                                  + textinvoiceno.Text + "'");
                                while (Database.myreader.Read())
                                {
                                    dataGridView1.Rows[i].Cells[ContNo.Index].Value = Database.myreader[1].ToString();
                                    dataGridView1.Rows[i].Cells[KindofPackages.Index].Value = Database.myreader[2].ToString();
                                    dataGridView1.Rows[i].Cells[DescriptionofGoods.Index].Value = Database.myreader[3].ToString();
                                    dataGridView1.Rows[i].Cells[Qty.Index].Value = Database.myreader[4].ToString();
                                    dataGridView1.Rows[i].Cells[QtyUnit.Index].Value = Database.myreader[5].ToString();
                                    dataGridView1.Rows[i].Cells[NetWeight.Index].Value = Database.myreader[6].ToString();
                                    dataGridView1.Rows[i].Cells[Rate.Index].Value = Database.myreader[8].ToString();
                                    dataGridView1.Rows[i].Cells[Amount.Index].Value = Database.myreader[10].ToString();
                                    dataGridView1.Rows[i].Cells[HeaderName.Index].Value = Database.myreader[11].ToString();
                                    dataGridView1.Rows[i].Cells[dtItemDeptt.Index].Value = Database.myreader[13].ToString();

                                    //combocommodity.Text
                                    string commodity = (Convert.ToString(Database.myreader[14]) == string.Empty ? combocommodity.Text : Convert.ToString(Database.myreader[14]));
                                    if (!dtCommodity.Items.Contains(commodity))
                                    {
                                        DataTable dt = (DataTable)dtCommodity.DataSource;
                                        dt.Rows.Add(commodity);
                                        dtCommodity.DataSource = dt;
                                    }
                                    if (Convert.ToString(Database.myreader[14]) == string.Empty)
                                    {
                                        dataGridView1.Rows[i].Cells[dtCommodity.Index].Value = combocommodity.Text;
                                    }
                                    else
                                    {
                                        dataGridView1.Rows[i].Cells[dtCommodity.Index].Value = Database.myreader[14].ToString();
                                    }

                                    dataGridView1.Rows[i].Cells[ColHSNCode.Index].Value = Database.myreader["HSNCode"].ToString();
                                    dataGridView1.Rows[i].Cells[colIGSTRate.Index].Value = Database.myreader["IGSTrate"].ToString();
                                    dataGridView1.Rows[i].Cells[colIGSTAmt.Index].Value = Database.myreader["IGSTAmt"].ToString();
                                    dataGridView1.Rows[i].Cells[colMainGrp.Index].Value = DespatchCommonFunction.GetDeptt(Database.myreader["ItemCode"].ToString());
                                    dataGridView1.Rows[i].Cells[colItemCode.Index].Value = Convert.ToString(Database.myreader["ItemCode"].ToString());


                                    i++;
                                }
                                Database.myreader.Close();
                                Database.Closeconnection();
                            }
                        }
                    }
                    if (Database.OpenConnection(Utility.DespacthConnectionString))
                    {
                        TxtEwayNo.Text = Convert.ToString(Database.GetExecuteNonQueryCommand_retrun("Select EWayNo from EWayBillMaster where companyname='"
                            + comboCompanyName.Text + "' and yr='" + textSuffix.Text + "' and  invno='" + textinvoiceno.Text + "'"));
                        if (TxtEwayNo.Text.Length == 0)
                            TxtEwayNo.Enabled = true;
                        else
                            TxtEwayNo.Enabled = false;
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
            }
        }
        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            frmPrintInvoicePackingList frm = new frmPrintInvoicePackingList();
            frm.Show();
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

        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            TabClass.OpenForm(new frmPrintPackingList());
        }

        private void dataGrid1_CurrentCellChanged(object sender, EventArgs e)
        {

        }

        private void dataGridView1_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {

            try
            {
                if (e.Control is TextBox)
                {
                    TextBox tb = e.Control as TextBox;
                    tb.KeyPress -= new KeyPressEventHandler(tb_KeyPress);
                    if (
                        this.dataGridView1.CurrentCell.ColumnIndex == Qty.Index
                        || this.dataGridView1.CurrentCell.ColumnIndex == NetWeight.Index
                        || this.dataGridView1.CurrentCell.ColumnIndex == Rate.Index
                        || this.dataGridView1.CurrentCell.ColumnIndex == Amount.Index)
                        tb.KeyPress += new KeyPressEventHandler(tb_KeyPress);
                }

                if (dataGridView1.CurrentCell.ColumnIndex == QtyUnit.Index) // For Qty Unit ComboBox
                {
                    ComboBox comboBox = e.Control as ComboBox;
                    comboBox.SelectedIndexChanged += new EventHandler(comboBox_SelectedIndexChanged);
                }
                if (dataGridView1.CurrentCell.ColumnIndex == KindofPackages.Index || dataGridView1.CurrentCell.ColumnIndex == DescriptionofGoods.Index)
                {
                    TextBox tb = e.Control as TextBox;
                    tb.KeyPress -= new KeyPressEventHandler(tb_KeyPress1);
                    tb.KeyPress += new KeyPressEventHandler(tb_KeyPress1);
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
                ErrorMessageBox.Show(ex);
            }
        }

        void tb_KeyPress1(object sender, KeyPressEventArgs e)
        {
            try
            {
                //|| dataGridView1.CurrentCell.ColumnIndex == 4  || dataGridView1.CurrentCell.ColumnIndex == 5
                if (dataGridView1.CurrentCell.ColumnIndex == KindofPackages.Index || dataGridView1.CurrentCell.ColumnIndex == DescriptionofGoods.Index)
                {
                    Index = dataGridView1.CurrentCell.ColumnIndex;
                    if (dataGridView1[Index, dataGridView1.CurrentRow.Index].Value != null)
                        textDetails.Text = dataGridView1[Index, dataGridView1.CurrentRow.Index].Value.ToString();
                    groupDetails.Focus();
                    groupDetails.Visible = true;
                    textDetails.Focus();
                }
            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
            }
        }
        void tb_KeyPress(object sender, KeyPressEventArgs e)
        {

            if ((!(Char.IsDigit(e.KeyChar))) && (!(e.KeyChar == '\b')) && (!(e.KeyChar == '.')))
                e.Handled = true;
        }


        void comboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            int selectedIndex = ((ComboBox)sender).SelectedIndex;
            //if (selectedIndex >= 0)
            //    MessageBox.Show(Convert.ToString(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[4].Value));
            // dataGridView1[2, dataGridView1.CurrentCell.RowIndex].Value = Convert.ToString(dataGridView1[4, dataGridView1.CurrentCell.RowIndex].Value);
            // if (selectedIndex >= 0)
            //   dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[2].Value = Convert.ToString(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[4].Value);
        }

        private void textntwt_KeyPress(object sender, KeyPressEventArgs e)
        {
            Utility.AllowFloatOnly(e);
        }

        private void texttarewt_KeyPress(object sender, KeyPressEventArgs e)
        {
            Utility.AllowFloatOnly(e);
        }

        private void textpaletwt_KeyPress(object sender, KeyPressEventArgs e)
        {
            Utility.AllowFloatOnly(e);
        }

        private void textgrosswt_KeyPress(object sender, KeyPressEventArgs e)
        {
            Utility.AllowFloatOnly(e);
        }

        private void toolStripButton4_Click(object sender, EventArgs e)
        {
            TabClass.OpenForm(new FrmBlDraft());
        }

        private void toolStripButton5_Click(object sender, EventArgs e)
        {
            TabClass.OpenForm(new FrmSheet1());
        }

        private void toolStripButton6_Click(object sender, EventArgs e)
        {
            TabClass.OpenForm(new FrmBenificiaryCertificate());
        }

        private void toolStripButton7_Click(object sender, EventArgs e)
        {
            TabClass.OpenForm(new FrmBenificiaryCertificate2());
        }

        private void toolStripButton8_Click(object sender, EventArgs e)
        {
            TabClass.OpenForm(new FrmInterModal());
        }

        private void toolStripButton9_Click(object sender, EventArgs e)
        {
            TabClass.OpenForm(new FrmSingleCountry());
        }

        private void toolStripButton10_Click(object sender, EventArgs e)
        {
            TabClass.OpenForm(new FrmPreLoding());
        }

        private void toolStripButton11_Click(object sender, EventArgs e)
        {
            TabClass.OpenForm(new FrmBillofExchange());
        }


        private void InsertValueInExportCntMaterial(string InvNo, string yr)
        {
            CalculateMaterialWt();
            if (Database.OpenConnection(Utility.DespacthConnectionString))
            {
                int srno = 0;
                Database.myreader = Database.GetExecuteReaderCommand("select SrNo from CommercialContMaterial order by srno");
                while (Database.myreader.Read())
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
                                                         + "' and advlicense = '" + comboAdvLicenseNo.Text
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
            CalculateMaterialWt();
            if (Database.OpenConnection(Utility.DespacthConnectionString))
            {
                int srno = 0;

                Database.GetExecuteNonQueryCommand("delete from CommercialContMaterial where ExportInvNo = '" + InvNo
                    + "' and yr = '" + yr + "' and companyname = '" + comboCompanyName.Text + "'");
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

        private void combocommodity_SelectedIndexChanged_1(object sender, EventArgs e)
        {
            string ADVLICENSE = "";
            string isdfia = "";
            comboAdvLicenseNo.Items.Clear();
            comboAdvLicenseNo.Text = "";
            if (Database.OpenConnection(Utility.DespacthConnectionString))
            {
                string strsql = "";
                strsql = "select advlicenseno from advlicense where companyname = '" + comboCompanyName.Text + "' and commodity = '" + combocommodity.Text
                       + "' AND setdefault = 'yes' and isnull(isclose,0)=0 ";

                //if (comboCompanyName.Text.Contains("Plastene Polyfilms Limited"))
                //{
                //    strsql = "select advlicenseno from advlicense where companyname = '" + comboCompanyName.Text + "' and commodity = '" + combocommodity.Text
                //        + "' AND setdefault = 'yes' and isnull(isclose,0)=0 ";
                //}
                //else
                //{
                //if (BlnSEZ)
                //    strsql = "select advlicenseno from advlicense where companyname = '" + comboCompanyName.Text + "' and commodity = '" + combocommodity.Text
                //        + "' AND setdefault = 'yes' and isnull(isclose,0)=0 and isnull(ISSEZ,0)=1";
                //else
                //    strsql = "select advlicenseno from advlicense where companyname = '" + comboCompanyName.Text + "' and commodity = '" + combocommodity.Text
                //    + "' AND setdefault = 'yes' and isnull(isclose,0)=0 and isnull(ISSEZ,0)=0";

                DespatchCommonFunction.Loadcombo(comboAdvLicenseNo, strsql, "advlicenseno", "advlicenseno", Utility.DespacthConnectionString);
                strsql = "";
                if (BlnSEZ)
                    strsql = "select advlicenseno,dfia from advlicense where companyname = '"
                    + comboCompanyName.Text + "' and commodity = '" + combocommodity.Text + "' AND setdefault = 'yes' and isnull(isclose,0)=0 and isnull(ISSEZ,0)=1";
                else
                    strsql = "select advlicenseno,dfia from advlicense where companyname = '"
                + comboCompanyName.Text + "' and commodity = '" + combocommodity.Text + "' AND setdefault = 'yes' and isnull(isclose,0)=0  and isnull(ISSEZ,0)=0";

                Database.myreader = Database.GetExecuteReaderCommand(strsql);
                if (Database.myreader.Read())
                {
                    ADVLICENSE = Database.myreader[0].ToString();
                    isdfia = Database.myreader[1].ToString();
                }
                Database.myreader.Close();
                if (isdfia == "yes")
                {
                    checkDFIA.Checked = true;
                    checkAdvanceLic.Checked = false;
                    checkAdvanceLic.Visible = false;
                    checkDFIA.Visible = true;
                    comboAdvLicenseNo.Text = ADVLICENSE;
                }
                else
                {
                    Database.myreader = Database.GetExecuteReaderCommand("select advlicenseno,dfia from advlicense where companyname = '"
                    + comboCompanyName.Text + "' and commodity = '" + combocommodity.Text + "' AND GETDATE() <= ExpiryDate AND setdefault = 'yes' and isnull(isclose,0)=0 ORDER BY ExpiryDate ASC");
                    if (Database.myreader.Read())
                    {
                        ADVLICENSE = Database.myreader[0].ToString();
                    }
                    Database.myreader.Close();
                    checkAdvanceLic.Checked = true;
                    checkDFIA.Checked = false;
                    checkDFIA.Visible = false;
                    checkAdvanceLic.Checked = true;
                    comboAdvLicenseNo.Text = ADVLICENSE;
                }
                Database.Closeconnection();
            }
        }

        private void toolStripButton12_Click(object sender, EventArgs e)
        {
            if (BlnSEZ)
            {
                TabClass.OpenForm(new frmPrintCustomReport(true));
                //frmPrintCustomReport frm = new frmPrintCustomReport(true);
                //frm.Show();
            }
            else
            {
                TabClass.OpenForm(new frmPrintCustomReport());
                //frmPrintCustomReport frm = new frmPrintCustomReport();
                //frm.Show();
            }

        }

        private void toolStripButton13_Click(object sender, EventArgs e)
        {
            TabClass.OpenForm(new FrmCustomInvoiceDeclaration());
        }

        private void toolStripButton14_Click(object sender, EventArgs e)
        {
            TabClass.OpenForm(new FrmSelfSealingDeclaration());
        }

        private void toolStripButton15_Click(object sender, EventArgs e)
        {
            TabClass.OpenForm(new FrmSDFDecl());
        }

        private void tabPage9_Click(object sender, EventArgs e)
        {

        }
        int Index = 0;
        private void toolStripButton2_Click_1(object sender, EventArgs e)
        {
            TabClass.OpenForm(new frmprintShippingBill());
        }

        private void btnDetailsClose_Click(object sender, EventArgs e)
        {
            groupDetails.Visible = false;
            dataGridView1[Index, dataGridView1.CurrentRow.Index].Value = textDetails.Text;
            textDetails.Text = "";
            dataGridView1.Focus();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            textDetails.Text = "";
            groupDetails.Visible = false;
        }

        private void toolStripButton16_Click(object sender, EventArgs e)
        {
            TabClass.OpenForm(new FrmPrintGRFORM());
        }


        private void comboAdvLicenseNo_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (Database.OpenConnection(Utility.DespacthConnectionString))
                {
                    string isdfia = "";
                    Database.myreader = Database.GetExecuteReaderCommand("select dfia from advlicense where companyname = '"
                    + comboCompanyName.Text + "' and commodity = '" + combocommodity.Text + "' and advlicenseno = '"
                    + comboAdvLicenseNo.Text + "'");
                    if (Database.myreader.Read())
                        isdfia = Database.myreader[0].ToString();
                    Database.myreader.Close();
                    if (isdfia == "yes")
                    {
                        checkDFIA.Checked = true;
                        checkAdvanceLic.Checked = false;
                        checkAdvanceLic.Visible = false;
                        checkDFIA.Visible = true;
                    }
                    else
                    {
                        checkAdvanceLic.Checked = true;
                        checkDFIA.Checked = false;
                        checkDFIA.Visible = false;
                        checkAdvanceLic.Checked = true;
                    }
                    Database.Closeconnection();
                }
            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
            }
        }

        private void toolStripButton17_Click(object sender, EventArgs e)
        {
            TabClass.OpenForm(new FrmPrintAre1OEL2());
        }

        private void BtnCalculation_Click(object sender, EventArgs e)
        {
            try
            {
                double TotalAmt = 0;
                for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                {
                    if (dataGridView1.Rows[i].Cells[3].Value.ToString() == "")
                    {
                        MessageBox.Show("Please Select Proper ItemDeptt");
                        break;
                    }
                    if (dataGridView1.Rows[i].Cells[4].Value.ToString() != "" && dataGridView1.Rows[i].Cells[1].Value.ToString() != "" &&
                                  dataGridView1.Rows[i].Cells[5].Value.ToString() != "")
                    {
                        TotalAmt += Convert.ToDouble(dataGridView1.Rows[i].Cells[8].Value.ToString());
                    }
                }
                TotalAmt += Convert.ToDouble(textFrieght.Text) + Convert.ToDouble(textInsurance.Text)
                      + Convert.ToDouble(textCommision.Text) + Convert.ToDouble(textDiscount.Text)
                      + Convert.ToDouble(textOther.Text) + Convert.ToDouble(textDeduction.Text);

                double AMountinRupees = TotalAmt * Convert.ToDouble(textCurrencyExchangeRate.Text);
                Calc(AMountinRupees);

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
                textADDVAT.Text = Convert.ToString(ADDVAT);


                if (Database.OpenConnection(Utility.DespacthConnectionString))
                {

                    Database.myreader = Database.GetExecuteReaderCommand("select pendinglimit from UTbond where BondNo = '"
                    + textUT1Bond.Text + "' and expirydate >= '"
                    + dateTimePicker_invoice.Value.ToString("yyyy-MM-dd") + "' and companyname = '"
                    + comboCompanyName.Text + "'");
                    if (Database.myreader.Read())
                    {
                        textOpeningBalance.Text = Database.myreader[0].ToString();
                    }
                    Database.myreader.Close();
                    pendinglimit = Convert.ToDouble(textDutyDebited.Text);
                    textDutyDebited.Text = Convert.ToString(BCD + CVD + EDCESS + HEDCESS + EDUCESSOEL2 + HEDUCESS + ADDVAT);
                    textClosingBalance.Text = Convert.ToString(Convert.ToDouble(textOpeningBalance.Text) - Convert.ToDouble(textDutyDebited.Text) + pendinglimit);

                    Database.GetExecuteNonQueryCommand("update UTBOnd set pendinglimit = "
                    + textClosingBalance.Text + " where bondno = '"
                    + textUT1Bond.Text + "' and companyname = '"
                    + comboCompanyName.Text + "'");
                }
            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
            }
        }

        private void tabPage10_Click(object sender, EventArgs e)
        {

        }

        private void comboCompanyName_SelectedIndexChanged(object sender, EventArgs e)
        {
            string strsql = "Select companyname from companymaster where   " +
                   "   isnull(IsFreeze,'no')='no' and (('" + dateTimePicker_invoice.Value.ToString("yyyy-MM-dd") +
                   "' between fromdate and todate) or (fromdate <= '" + dateTimePicker_invoice.Value.ToString("yyyy-MM-dd") +
                   "' and todate is null ))  and CompanyCode not in " +
                   " ( select LCompanyCode from MaterialProcessing.dbo.LedgerMaster where (Todate is not null or IsAccClose='yes' ) and  companyname='" + comboCompanyName.Text +
                   "'  and isnull(LCompanyCode,'')<>'') order by companyname";
            DespatchCommonFunction.LoadCustomerUnlock(comboBuyerName, comboCompanyName.Text, dateTimePicker_invoice.Value);

            if (comboCompanyName.Text == "Plastene India Limited ")
                textIE_code.Text = "3798000344";
            else
                textIE_code.Text = "";

            if (Database.OpenConnection(Utility.DespacthConnectionString))
            {

                Database.myreader = Database.GetExecuteReaderCommand("select importerscode,IECAddress from FactoryInfo where name='"
                    + comboCompanyName.Text + "' ");
                if (Database.myreader.Read())
                {
                    textIE_code.Text = Convert.ToString(Database.myreader[0]);
                    TxtIECAdd.Text = Convert.ToString(Database.myreader[1]);
                }
                //textIE_code.Text = Convert.ToString(Database.GetExecuteNonQueryCommand_retrun("select importerscode from FactoryInfo where name='"
                //    + comboCompanyName.Text + "'"));

                fillData();
                //Database.myadapter = Database.GetAdapterCommand("Select CommodityName from Commodity where companyname = '"
                //    + comboCompanyName.Text + "' order by CommodityName");
                //DataTable dt = new DataTable();
                //Database.myadapter.Fill(dt);
                //dtCommodity.DataSource = dt;
                //dtCommodity.ValueMember = dt.Columns[0].ColumnName.ToString();
                //dtCommodity.DisplayMember = dt.Columns[0].ColumnName.ToString();

            }

        }

        void fillData(Boolean blnupdate = false)
        {

            if (Database.OpenConnection(Utility.DespacthConnectionString))
            {
                if (blnupdate)
                {
                    Database.myadapter = Database.GetAdapterCommand("Select CommodityName from Commodity where companyname = '" + comboCompanyName.Text
                        + "'  order by CommodityName");
                }
                else
                {
                    Database.myadapter = Database.GetAdapterCommand("Select CommodityName from Commodity where companyname = '" + comboCompanyName.Text
                        + "' and (('" + dateTimePicker_invoice.Value.ToString("yyyy-MM-dd")
                        + "' between nFromDate and nToDate) or (nFromDate <= '"
                        + dateTimePicker_invoice.Value.ToString("yyyy-MM-dd") + "' and nToDate is null )) "
                        + " order by CommodityName");
                }
                DataTable dt = new DataTable();
                Database.myadapter.Fill(dt);
                dtCommodity.DataSource = dt;
                dtCommodity.ValueMember = dt.Columns[0].ColumnName.ToString();
                dtCommodity.DisplayMember = dt.Columns[0].ColumnName.ToString();

                Database.myadapter = Database.GetAdapterCommand("Select unitname from MaterialProcessing.dbo.Unit order by unitname");
                dt = new DataTable();
                Database.myadapter.Fill(dt);

                QtyUnit.DataSource = dt;
                QtyUnit.ValueMember = dt.Columns[0].ColumnName.ToString();
                QtyUnit.DisplayMember = dt.Columns[0].ColumnName.ToString();


                Database.myadapter = Database.GetAdapterCommand("select distinct(depttname) from Despatch..itemdeptt order by depttname   ");
                DataTable dt1 = new DataTable();
                Database.myadapter.Fill(dt1);
                dtItemDeptt.DataSource = dt1;
                dtItemDeptt.ValueMember = dt1.Columns[0].ColumnName.ToString();
                dtItemDeptt.DisplayMember = dt1.Columns[0].ColumnName.ToString();

                #region ITem Master Added 22.06.2020
                dtIMaster = new DataTable();
                dtIMaster = DespatchCommonFunction.GetItemMaster(dateTimePicker_invoice.Value.ToString("yyyy-MM-dd"), comboCompanyName.Text);
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
        private void BtnCalc_Click(object sender, EventArgs e)
        {
            calculate();
        }
        private void calculate()
        {
            try
            {

                double total = 0;
                double fobvalue = 0;
                bool isallow = true;
                double dblIGSTAmt = 0;
                double dblnetwt = 0;
                for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                {
                    if (dataGridView1.Rows[i].Cells[dtItemDeptt.Index].FormattedValue.ToString() == "")
                    {
                        MessageBox.Show("Please Select proper itemdeptt ");
                        isallow = false;
                        break;
                    }

                    //dataGridView1.Rows[i].Cells[Rate.Index].Value =
                    //Math.Round(Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[Rate.Index].Value), FrmMain.ERPRateDigit);

                    dataGridView1.Rows[i].Cells[Qty.Index].Value =
                    Math.Round(Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[Qty.Index].Value), FrmMain.ERPQtyDigit);

                    dataGridView1.Rows[i].Cells[NetWeight.Index].Value =
                    Math.Round(Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[NetWeight.Index].Value), FrmMain.ERPQtyDigit);

                    dataGridView1.Rows[i].Cells[Amount.Index].Value =
                        Convert.ToString(Convert.ToDouble(dataGridView1.Rows[i].Cells[Qty.Index].Value)
                    * Convert.ToDouble(dataGridView1.Rows[i].Cells[Rate.Index].Value));

                    dataGridView1.Rows[i].Cells[Amount.Index].Value =
                   Math.Round(Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[Amount.Index].Value), FrmMain.ERPAmoutnDigit);

                    if (dataGridView1.Rows[i].Cells[Amount.Index].FormattedValue.ToString() != "")
                    {
                        total = total + Convert.ToDouble(dataGridView1.Rows[i].Cells[Amount.Index].FormattedValue.ToString());
                    }

                    total = Math.Round(total, FrmMain.ERPAmoutnDigit);
                    ///ADD ERPAmoutnDigit
                    ///Change by nikhil
                    dataGridView1.Rows[i].Cells[colIGSTAmt.Index].Value =
                    Math.Round(Utility.SafeConvertToDouble(Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[colIGSTRate.Index].Value)
                    * Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[Amount.Index].Value) / 100), FrmMain.ERPAmoutnDigit);
                    dblIGSTAmt += Math.Round(Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[colIGSTAmt.Index].Value), FrmMain.ERPAmoutnDigit);

                    dblnetwt += Math.Round(Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[NetWeight.Index].Value), FrmMain.ERPQtyDigit);
                }

                if (isallow)
                {


                    if (comboContract.Text == "FOB")
                        fobvalue = total - Utility.SafeConvertToDouble(txtSeaFreAmt.Text) - Utility.SafeConvertToDouble(TxtHaulageAmt.Text) -
                            Utility.SafeConvertToDouble(TxtOthersDiscAmt.Text);

                    if (textSpDuty1Amt.Text != "")
                        total = total + Utility.SafeConvertToDouble(textSpDuty1Amt.Text);

                    if (textSpDuty2Amt.Text != "")
                        total = total + Utility.SafeConvertToDouble(textSpDuty2Amt.Text);

                    if (textSpDuty3Amt.Text != "")
                        total = total + Utility.SafeConvertToDouble(textSpDuty3Amt.Text);

                    if (comboContract.Text == "CIF")
                    {
                        if (!chkRate.Checked)
                        {
                            //textInsurance.Text = Convert.ToString(Math.Round((total * 0.05 / 100), 2));
                            if (textplace_delivery.Text == "KASEZ")
                            {
                                fobvalue = total - Utility.SafeConvertToDouble(textFrieght.Text);
                            }
                            else
                            {
                                fobvalue = total - Utility.SafeConvertToDouble(textFrieght.Text)
                                                       - Utility.SafeConvertToDouble(textInsurance.Text);
                            }
                        }
                        else
                            fobvalue = total;
                    }
                    else if (comboContract.Text == "CF")
                    {
                        if (!chkRate.Checked)
                            fobvalue = total - Utility.SafeConvertToDouble(textFrieght.Text);
                        else
                            fobvalue = total;
                    }
                    else if (comboContract.Text == "CI")
                    {
                        if (!chkRate.Checked)
                        {
                            //textInsurance.Text = Convert.ToString(Math.Round((total * 0.05 / 100), 2));
                            if (textplace_delivery.Text == "KASEZ")
                                fobvalue = total;
                            else
                                fobvalue = total - Utility.SafeConvertToDouble(textInsurance.Text);
                        }
                        else
                            fobvalue = total;
                    }
                    //    textFOBValue.BackColor = Color.Red;
                }
                //textFOBValue.Text = Math.Round(fobvalue, 2).ToString();
                ///ADD ERPAmoutnDigit
                ///Change by nikhil
                #region Calculate TCS
                if (BlnSEZ)
                {
                    TCS = Convert.ToDouble(textTCS.Text);
                    TCS = (total * TCS) / 100;
                    TCS = Math.Round(TCS, FrmMain.ERPAmoutnDigit);
                    total = total + TCS;
                }
                #endregion

                //Changed by Rishabh for discount effect issue raised by logistics team (Chetan Barot)
                fobvalue = fobvalue - Utility.SafeConvertToDouble(textDiscount.Text);
                //change end

                if (comboContract.Text == "CIF")
                {
                    fobvalue = fobvalue - Utility.SafeConvertToDouble(txtSeaFreAmt.Text) - Utility.SafeConvertToDouble(TxtHaulageAmt.Text) - Utility.SafeConvertToDouble(TxtOthersDiscAmt.Text);
                }
                
                textFOBValue.Text = Math.Round(fobvalue, FrmMain.ERPAmoutnDigit).ToString();
                txtFOBV.Text = textFOBValue.Text;
                textntwt.Text = dblnetwt.ToString();

                //Change by Rishabh on 29-07-2026 for calc Tare and Pallet Wt
                 double grosswt = Convert.ToDouble(textgrosswt.Text);
                 texttarewt.Text = Math.Round((grosswt - dblnetwt), FrmMain.ERPAmoutnDigit).ToString();
                 textpaletwt.Text = Math.Round((grosswt - dblnetwt), FrmMain.ERPAmoutnDigit).ToString();
            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
            }
        }
        private void dataGridView1_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            try
            {
                if (dataGridView1.CurrentCell.ColumnIndex == dtCommodity.Index)
                {
                    if (Database.OpenConnection(Utility.DespacthConnectionString))
                    {
                        string Commodity = Convert.ToString(dataGridView1.Rows[dataGridView1.CurrentRow.Index].Cells[dtCommodity.Index].Value);
                        string THeading = "";
                        Database.OpenConnection(Utility.DespacthConnectionString);
                        Database.myreader = Database.GetExecuteReaderCommand("Select top 1 THeading,NCGSTRate,NSGSTRate,NIGSTRate from Commodity where companyname = '"
                            + comboCompanyName.Text + "' and CommodityName='" + Commodity + "' and (('" + dateTimePicker_invoice.Value.ToString("yyyy-MM-dd")
                            + "' between nFromDate and nToDate) or (nFromDate <= '"
                            + dateTimePicker_invoice.Value.ToString("yyyy-MM-dd") + "' and nToDate is null )) ");
                        if (Database.myreader.Read())
                        {
                            THeading = Database.myreader[0].ToString();
                            dataGridView1.CurrentRow.Cells[ColHSNCode.Index].Value = THeading;
                            dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[ColHSNCode.Index].Value = THeading;
                            dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[colIGSTRate.Index].Value = Database.myreader["NIGSTRate"].ToString();
                        }
                        Database.myreader.Close();
                        combocommodity.Text = "";
                        combocommodity.Text = Commodity;
                        textHtsCode.Text = "";
                        textHtsCode.Text = THeading;
                        double netwt = Utility.SafeConvertToDouble(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[NetWeight.Index].Value);
                        double pendingq = DespatchCommonFunction.PendingAdvLic(comboCompanyName.Text, comboAdvLicenseNo.Text, netwt);
                        dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[ColAdvLicQty.Index].Value = Math.Round(pendingq, FrmMain.ERPQtyDigit);
                        lblAdvLicNo.Text = "Pending Adv Lic Qty : " + Math.Round(pendingq, FrmMain.ERPQtyDigit) + " MT";
                        #region check TCS Calculation 28.09.2020
                        if (BlnSEZ)
                            textTCS.Text = DespatchCommonFunction.LoadTCSLedger(comboCompanyName.Text, comboBuyerName.Text, Commodity, Convert.ToDateTime(dateTimePicker_invoice.Value)).ToString(); ;
                        #endregion
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
                        " where i.itemcode='" + itemcode + "' and i.CompanyName='" + comboCompanyName.Text + "' and isnull(i.Commodity,'')<>'') AS A " +
                        " CROSS APPLY Commodity.nodes ('/M') AS Split(a) order by 1 ";

                    DataTable dt = Database.GetDataTable(strsql);
                    DataGridViewComboBoxCell colComm = (DataGridViewComboBoxCell)dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[dtCommodity.Index];
                    colComm.DataSource = dt;
                    colComm.ValueMember = dt.Columns[0].ColumnName.ToString();
                    colComm.DisplayMember = dt.Columns[0].ColumnName.ToString();
                }
            }
            catch (Exception ex)
            {
                ex.Message.ToString();
            }
        }

        private void toolStripButton19_Click(object sender, EventArgs e)
        {
            TabClass.OpenForm(new FrmSelfSealingDeclaration2());
        }

        private void dataGridView1_CurrentCellChanged(object sender, EventArgs e)
        {
            try
            {
                double total = 0;
                if (dataGridView1.CurrentCell.RowIndex.ToString() == "null")
                    return;

                {
                    dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[Amount.Index].Value =
                            Math.Round(Convert.ToDouble(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[Qty.Index].Value)
                        * Convert.ToDouble(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[Rate.Index].Value), FrmMain.ERPAmoutnDigit);
                    total += Convert.ToDouble(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[Amount.Index].Value);
                }
                dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[colIGSTAmt.Index].Value =
                    Math.Round(Convert.ToDouble(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[Amount.Index].Value) *
                    Convert.ToDouble(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[colIGSTRate.Index].Value) / 100, FrmMain.ERPAmoutnDigit);

                double pendingq = DespatchCommonFunction.PendingAdvLic(comboCompanyName.Text, comboAdvLicenseNo.Text, Utility.SafeConvertToDouble(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[NetWeight.Index].Value));
                dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[ColAdvLicQty.Index].Value = Math.Round(pendingq, FrmMain.ERPQtyDigit);
                lblAdvLicNo.Text = "Pending Adv Lic Qty : " + Math.Round(pendingq, FrmMain.ERPQtyDigit) + " MT";

            }
            catch (Exception ex)
            {
                //ErrorMessageBox.Show(ex);
            }
        }

        private void btnItemDetails_Click(object sender, EventArgs e)
        {
            if (textinvoiceno.Text.Length == 0)
            {
                MessageBox.Show("Please Save the Record First");
                return;
            }
            frmpackingitemsnew frm = new frmpackingitemsnew(textinvoiceno.Text);
            frm.Show();
            DisplayRecord();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            try
            {
                if (textinvoiceno.Text.Length == 0) return;
                if (Database.OpenConnection(Utility.DespacthConnectionString))
                {
                    if (!DespatchCommonFunction.IsEwayGenerated(comboCompanyName.Text, textSuffix.Text, textinvoiceno.Text))
                    {
                        Database.GetExecuteNonQueryCommand("INSERT INTO EWayBillMaster(CompanyName,InvNo,Yr,EWayNo) VALUES('" +
                            comboCompanyName.Text + "','" + textinvoiceno.Text + "','" + textSuffix.Text + "','" + TxtEwayNo.Text + "')");
                        MessageBox.Show("E-Way Bill No update for Inv No :" + textinvoiceno.Text);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Problem with Updating E-Way Bill No : " + ex.Message);
            }
        }

        private void tsbtnAttachment_Click(object sender, EventArgs e)
        {
            if (this.textinvoiceno.Text.Length > 0)
            {
                Tools.DMSManager.OpenReference("DS_EI", string.Format("{0}/{1}/DS_EI/{2}",
                    AccountCommonFunction.GetCompanyCode(this.comboCompanyName.Text),
                    this.textSuffix.Text,
                    this.textinvoiceno.Text
                    ));
            }
        }

        private void checkAdvanceLic_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                if (!checkAdvanceLic.Checked)
                {
                    lblDrawback.Visible = true;
                    txtDrawBackNo.Visible = true;
                    lblDrawbakAmt.Visible = true;
                    txtDrawBackAmt.Visible = true;
                }
                else
                {
                    lblDrawback.Visible = false;
                    txtDrawBackNo.Visible = false;
                    lblDrawbakAmt.Visible = false;
                    txtDrawBackAmt.Visible = false;
                }
            }
            catch (Exception ex)
            {

            }
        }

        private void dateTimePicker_invoice_ValueChanged(object sender, EventArgs e)
        {
            string strsql = "Select companyname from companymaster where   (invoicecategory = 'Export'  or invoicecategory = '*') and " +
                   " isnull(IsFreeze,'no')='no' and (('" + dateTimePicker_invoice.Value.ToString("yyyy-MM-dd") +
                   "' between fromdate and todate) or (fromdate <= '" + dateTimePicker_invoice.Value.ToString("yyyy-MM-dd") +
                   "' and todate is null ))  and CompanyCode not in " +
                   " ( select LCompanyCode from MaterialProcessing.dbo.LedgerMaster where (Todate is not null or IsAccClose='yes' ) and  companyname='" + comboCompanyName.Text +
                   "'  and isnull(LCompanyCode,'')<>'') order by companyname";
            DespatchCommonFunction.LoadCustomerUnlock(comboBuyerName, comboCompanyName.Text, dateTimePicker_invoice.Value);
            //try
            //{
            //    string strsql = "Select companyname from CompanyMaster where " +
            //                  " isnull(IsFreeze,'no')='no' and (('" + dateTimePicker_invoice.Value.ToString("yyyy-MM-dd") +
            //                  "' between fromdate and todate) or (fromdate <= '" + dateTimePicker_invoice.Value.ToString("yyyy-MM-dd") +
            //                  "' and todate is null ))  order by CompanyName";
            //    DespatchCommonFunction.Loadcombo(comboBuyerName, strsql, "CompanyName", "CompanyName", Utility.DespacthConnectionString);
            //}
            //catch (Exception ex)
            //{
            //    ex.Message.ToString();
            //}
        }

        private void dataGridView1_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.KeyCode == Keys.Enter)
                    calculate();
            }
            catch (Exception ex)
            {
                MessageBox.Show("dataGridView1_KeyDown : " + ex.Message);
            }
        }

        private void dataGridView1_CellLeave(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (e.ColumnIndex == Rate.Index || e.ColumnIndex == Qty.Index || e.ColumnIndex == Amount.Index)
                {
                    calculate();
                    //dataGridView1.Rows[e.RowIndex].Cells[Rate.Index].Value =
                    //    Math.Round(Utility.SafeConvertToDouble(dataGridView1.Rows[e.RowIndex].Cells[Rate.Index].Value), FrmMain.ERPRateDigit);

                    dataGridView1.Rows[e.RowIndex].Cells[Qty.Index].Value =
                    Math.Round(Utility.SafeConvertToDouble(dataGridView1.Rows[e.RowIndex].Cells[Qty.Index].Value), FrmMain.ERPQtyDigit);


                    dataGridView1.Rows[e.RowIndex].Cells[NetWeight.Index].Value =
                    Math.Round(Utility.SafeConvertToDouble(dataGridView1.Rows[e.RowIndex].Cells[NetWeight.Index].Value), FrmMain.ERPQtyDigit);

                    dataGridView1.Rows[e.RowIndex].Cells[Amount.Index].Value =
                    Math.Round(Utility.SafeConvertToDouble(dataGridView1.Rows[e.RowIndex].Cells[Amount.Index].Value), FrmMain.ERPAmoutnDigit);

                    double pendingq = DespatchCommonFunction.PendingAdvLic(comboCompanyName.Text, comboAdvLicenseNo.Text, Utility.SafeConvertToDouble(dataGridView1.Rows[e.RowIndex].Cells[NetWeight.Index].Value));
                    dataGridView1.Rows[e.RowIndex].Cells[ColAdvLicQty.Index].Value = Math.Round(pendingq, FrmMain.ERPQtyDigit);
                    lblAdvLicNo.Text = "Pending Adv Lic Qty : " + Math.Round(pendingq, FrmMain.ERPQtyDigit) + " MT";


                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("dataGridView1_CellLeave : " + ex.Message);
            }
        }

        private void dataGridView1_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void comboCurrencyValue_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                textCurrencyExchangeRate.Text = Convert.ToString(AccountCommonFunction.GetExchangeRate(comboCurrency.Text, dateTimePicker_invoice.Value));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void comboContract_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboContract.Text.ToUpper() == "CIF")
            {
                chkRate.Visible = true;
                chkRate.Enabled = true;
            }
            else
            {
                chkRate.Visible = false;
                chkRate.Enabled = false;
            }

        }

        private void label15_Click(object sender, EventArgs e)
        {

        }

        private void label14_Click(object sender, EventArgs e)
        {

        }

        private void label115_Click(object sender, EventArgs e)
        {

        }

        private void tabPage1_Click(object sender, EventArgs e)
        {

        }

        private void label28_Click(object sender, EventArgs e)
        {

        }

        private void label66_Click(object sender, EventArgs e)
        {

        }

        private void combomanufact_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void label101_Click(object sender, EventArgs e)
        {

        }

        private void textCurrencyExchangeRate_TextChanged(object sender, EventArgs e)
        {

        }

        private void label71_Click(object sender, EventArgs e)
        {

        }

        private void label42_Click(object sender, EventArgs e)
        {

        }

        private void textSuffix_TextChanged(object sender, EventArgs e)
        {

        }

        private void label33_Click(object sender, EventArgs e)
        {

        }

        private void tabPage5_Click(object sender, EventArgs e)
        {

        }

        private void label53_Click(object sender, EventArgs e)
        {

        }

        private void label110_Click(object sender, EventArgs e)
        {

        }

        //Change by Rishabh on 03-11-2025, made new combobox and its click event to auto populate bank details after selection of identfier. Bank master taken from Anil sir's master excel.
        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (comboCurrency.Text == "")
                {
                    MessageBox.Show("Please select proper currency first");
                    return;
                }

                else
                {
                    Database.OpenConnection(Utility.DespacthConnectionString);
                    if (comboCurrency.Text == "USD")
                    {
                        Database.myreader = Database.GetExecuteReaderCommand("select BankName,Branch,Swift,AccountNo,Corresponding_USD_Bank,Corresponding_USD_Swift,Corresponding_USD_AcNo from ExportBankMaster where BankDetails = '"
                              + comboBox2.Text + "'");
                        if (Database.myreader.Read())
                        {
                            textBankDetail.Text = Database.myreader["BankName"].ToString() + ", " + Database.myreader["Branch"].ToString();
                            textSwiftCode.Text = Database.myreader["Swift"].ToString();
                            textBenefiaciaryAccoutNo.Text = Database.myreader["AccountNo"].ToString();
                            textCorrespondingBank.Text = Database.myreader["Corresponding_USD_Bank"].ToString();
                            textCorrespondingBankSwift.Text = Database.myreader["Corresponding_USD_Swift"].ToString();
                            textIBANAccoutNo.Text = Database.myreader["Corresponding_USD_AcNo"].ToString();
                        }
                        else
                        {
                            MessageBox.Show("Please select proper bank");
                            return;
                        }
                    }
                    else if (comboCurrency.Text == "EURO")
                    {
                        Database.myreader = Database.GetExecuteReaderCommand("select BankName,Branch,Swift,AccountNo,Corresponding_Euro_Bank,Corresponding_Euro_Swift,Corresponding_Euro_AcNo from ExportBankMaster where BankDetails = '"
                              + comboBox2.Text + "'");
                        if (Database.myreader.Read())
                        {
                            textBankDetail.Text = Database.myreader["BankName"].ToString() + ", " + Database.myreader["Branch"].ToString();
                            textSwiftCode.Text = Database.myreader["Swift"].ToString();
                            textBenefiaciaryAccoutNo.Text = Database.myreader["AccountNo"].ToString();
                            textCorrespondingBank.Text = Database.myreader["Corresponding_Euro_Bank"].ToString();
                            textCorrespondingBankSwift.Text = Database.myreader["Corresponding_Euro_Swift"].ToString();
                            textIBANAccoutNo.Text = Database.myreader["Corresponding_Euro_AcNo"].ToString();
                        }
                        else
                        {
                            MessageBox.Show("Please select proper bank");
                            return;
                        }
                    }
                    else if (comboCurrency.Text == "GBP")
                    {
                        Database.myreader = Database.GetExecuteReaderCommand("select BankName,Branch,Swift,AccountNo,Corresponding_GBP_Bank,Corresponding_GBP_Swift,Corresponding_GBP_AcNo from ExportBankMaster where BankDetails = '"
                              + comboBox2.Text + "'");
                        if (Database.myreader.Read())
                        {
                            textBankDetail.Text = Database.myreader["BankName"].ToString() + ", " + Database.myreader["Branch"].ToString();
                            textSwiftCode.Text = Database.myreader["Swift"].ToString();
                            textBenefiaciaryAccoutNo.Text = Database.myreader["AccountNo"].ToString();
                            textCorrespondingBank.Text = Database.myreader["Corresponding_GBP_Bank"].ToString();
                            textCorrespondingBankSwift.Text = Database.myreader["Corresponding_GBP_Swift"].ToString();
                            textIBANAccoutNo.Text = Database.myreader["Corresponding_GBP_AcNo"].ToString();
                        }
                        else
                        {
                            MessageBox.Show("Please select proper bank");
                            return;
                        }
                    }
                    else
                    {
                        Database.myreader = Database.GetExecuteReaderCommand("select BankName,Branch,Swift,AccountNo from ExportBankMaster where BankDetails = '"
                              + comboBox2.Text + "'");
                        if (Database.myreader.Read())
                        {
                            textBankDetail.Text = Database.myreader["BankName"].ToString() + ", " + Database.myreader["Branch"].ToString();
                            textSwiftCode.Text = Database.myreader["Swift"].ToString();
                            textBenefiaciaryAccoutNo.Text = Database.myreader["AccountNo"].ToString();
                            textCorrespondingBank.Text = "";
                            textCorrespondingBankSwift.Text = "";
                            textIBANAccoutNo.Text = "";
                        }
                        else
                        {
                            MessageBox.Show("Please select proper bank");
                            return;
                        }
                    }
                    Database.myreader.Close();
                    Database.Closeconnection();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        //Change End

        private void FillForm(ExportInvoiceImport invoice)
        {

            //Invoice Number
            //textinvoiceno.Text = invoice.InvoiceNo;

            //Invoice Date
            //dateTimePicker_invoice.Value = invoice.InvoiceDate;

            if (frmDefaultVale.CompanyName != companyMap[invoice.Exporter])
            {
                MessageBox.Show("Exporting company mismatch in ERP and excel. Please select appropriat company in ERP Default Value");
                return;
            }

            //Buyer
            comboBuyerName.Text = invoice.Buyer;

            //Company
            //comboCompanyName.Text = invoice.CompanyCode;

            //Loading Port
            combo_loading.Text = invoice.PortLoading;

            //Discharge Port
            combo_discharge.Text = invoice.PortDischarge;

            //Currency
            comboCurrency.Text = invoice.Currency;

            //Consignee
            comboConsigneeName.Text = invoice.Consignee;

            //Export Under
            if (!string.IsNullOrEmpty(invoice.ExportUnder))
            {
                if (invoice.ExportUnder.Contains("LUT"))
                    comboBeingExport.Text = "LUT Bond No";
                else if (invoice.ExportUnder.Contains("Rebate"))
                    comboBeingExport.Text = "Claim for Rebate";
            }

            //Cont Booking No
            contbooking.Text = invoice.ContBookingNo;

            //Stuffing Company
            if (invoice.StuffingComp == "")
            {
                stuffing_plant.Text = frmDefaultVale.CompanyName;
            }
            else if (stuffingcompanyMap.ContainsKey(invoice.StuffingComp))
            {
                stuffing_plant.Text = stuffingcompanyMap[invoice.StuffingComp];
            }
            else
            {
                MessageBox.Show("Please Check Stuffing Company in Excel file");
                return;
            }

            //Manufaturer
            if (invoice.Manuf_Inv_No != "")
            {
                checkmenufact.Checked = true;
                combomanufact.Text = invoice.Manufacturer;
                textBox1.Text = invoice.Manuf_Inv_No;
                dateTimePicker1.Value = invoice.Manuf_Inv_Dt;
            }

            //Buyer
            if (!CheckBuyerName(invoice.Buyer))
            {
                MessageBox.Show("Buyer mentioned in excel not exists in ERP. Please check.");
                return;
            }

            else
            {
                comboBuyerName.Text = invoice.Buyer;
            }

            //consignee
            if (!CheckConsignee(invoice.Buyer, invoice.Consignee))
            {
                MessageBox.Show("Consignee mentioned in excel not exists in ERP for selected buyer. Please check.");
            }

            else
            {
                comboBuyerName.Text = invoice.Buyer;
            }

            //Notify Party
            textNotifier.Text = invoice.NotifyParty;

            //ADCode
            if (Database.OpenConnection(Utility.DespacthConnectionString))
            {
                bool isADCode = false;
                Database.myreader = Database.GetExecuteReaderCommand("Select * from ExportADCodesmaster where ADCode = '" + invoice.ADCode + "'");
                if (Database.myreader.Read())
                {
                    isADCode = true;
                }

                if (!isADCode)
                    MessageBox.Show("ADCode mentioned in excel does not exist in ERP. Please check master.");
                else
                    comboBox1.Text = invoice.ADCode;
            }

            //Country of Origin
            combocountry_origin.Text = invoice.CountryOrigin;

            //Country of Destination
            combocontry_desti.Text = invoice.CountryDestination;

            //Port of Loading
            combo_loading.Text = invoice.PortLoading;

            //Port of Discharge
            combo_discharge.Text = invoice.PortDischarge;

            //Place of Delivery
            textplace_delivery.Text = invoice.PlaceDelivery;

            //Tersm of Delivery
            richTextterms_dlivery.Text = invoice.ShipmentTerms;
            comboContract.Text = invoice.ShipmentTerms;

            //Payment Terms
            comboPaymentTerms.Text = invoice.PaymentTerms;

            //Currency
            comboCurrency.Text = invoice.Currency;

            //Proforma Inv
            TxtPerformaInvNo.Text = invoice.ProformaInv;

            //Container No
            texthc_containerno.Text = invoice.ContNo;

            //Line Seal
            textline_Seal.Text = invoice.LineSealNo;

            //Shipper Seal No
            textcustom_sealno.Text = invoice.ShipperSealNo;

            //Total Gross Wt
            textgrosswt.Text = invoice.TotGrossWt.ToString();

            //Disount
            if (invoice.Discount > 0)
            {
                txtSeaFrieghtRem.Text = "Discount";
                txtSeaFreAmt.Text = invoice.Discount.ToString();
            }

            //Insurance
            textInsurance.Text = invoice.Insurance.ToString();

            //Frieght
            textFrieght.Text = invoice.Freight.ToString();

            //Add Charges
            textSpDutyRemarks1.Text = invoice.Add_Charges_Description;
            textSpDuty1Amt.Text = invoice.Add_Charges.ToString();

            //RexDeclaration
            if (invoice.RexDeclaration == "Yes")
            {
                checkBox1.Checked = true;
            }

            //Bank selection
            comboBox2.Text = invoice.BankCode;

            //VGM
            cont_weight.Text = invoice.Cont_Max_Permissible_Wt.ToString();
            cont_tare_wt.Text = invoice.Cont_Tare_Wt.ToString();
            Wt_SlipNo.Text = invoice.Weightment_SlipNo;
            WeightmentDt.Value = invoice.Weightment_Dt;

            //Cont Size
            comboContType.Text = "01 x " + invoice.ContSize;

            //Item Details
            FillItemDetails(invoice);

            //Type of Shipment
            comboShipment.Text = "Consignment Export";

            //Mode of Transit
            comboModeofTransit.Text = "Sea";

            //Transport Details
            comboTransporter.Text = invoice.TransporterName;
            comboVehicalNo.Text = invoice.VehicleNo;

            if (invoice.chk_rate_freight.ToLower() == "yes")
            {
                chkRate.Checked = true;
                double checkvalue = Math.Round(Convert.ToDouble(invoice.Add_Charges - (invoice.Freight + invoice.Insurance)));

                if (checkvalue > 0 && invoice.ShipmentTerms.ToUpper() == "CIF")
                {
                    textSpDuty1Amt.Text = checkvalue.ToString();
                }
                else
                {
                    textSpDutyRemarks1.Text = "";
                    textSpDuty1Amt.Text = "0";
                }
            }
        }

        private void FillItemDetails(ExportInvoiceImport invoice)
        {
            //For importing line item details.
            dataGridView1.Rows.Clear();
            //Total No of Packages
            double totalpackage = 0;
            string Deptt = "";

            foreach (ExportInvoiceItem item in invoice.Items)
            {
                int row = dataGridView1.Rows.Add();
                dataGridView1.Rows[row].Cells["ContNo"].Value = "PoNo - " + item.PONO;

                dataGridView1.Rows[row].Cells["KindofPackages"].Value =
                                     item.No_of_Roles_Bales_Pallets;

                totalpackage += Convert.ToDouble(item.No_of_Roles_Bales_Pallets);

                dataGridView1.Rows[row].Cells["DescriptionofGoods"].Value =
                                     item.Description_Size_Details;

                //for commodity and item read
                if(Database.OpenConnection(Utility.MaterialConnectionString))
                {
                    Database.myreader = Database.GetExecuteReaderCommand("Select distinct Deptt,ItemCode from Item where ItemName = '" + invoice.ItemName + "'");
                    if (Database.myreader.Read())
                    {
                        dataGridView1.CurrentCell = dataGridView1.Rows[row].Cells[colMainGrp.Index];
                        dataGridView1.Rows[row].Cells["colMainGrp"].Value = Database.myreader["Deptt"]; 
                        dataGridView1_CurrentCellDirtyStateChanged(dataGridView1, EventArgs.Empty);

                        dataGridView1.CurrentCell = dataGridView1.Rows[row].Cells[colItemCode.Index];
                        dataGridView1.Rows[row].Cells["colItemCode"].Value = Database.myreader["ItemCode"]; 
                        dataGridView1_CurrentCellDirtyStateChanged(dataGridView1, EventArgs.Empty);
                    }
                }

                dataGridView1.CurrentCell = dataGridView1.Rows[row].Cells[dtCommodity.Index];
                dataGridView1.Rows[row].Cells["dtCommodity"].Value = invoice.commodityname; 
                dataGridView1_CurrentCellDirtyStateChanged(dataGridView1, EventArgs.Empty);

                if(Database.OpenConnection(Utility.DespacthConnectionString))
                {
                    Database.myreader = Database.GetExecuteReaderCommand("Select ItemDeptt from ExcelImportItemDeptt where ItemName = '" + invoice.ItemName + "'");
                    if(Database.myreader.Read())
                    {
                        dataGridView1.CurrentCell = dataGridView1.Rows[row].Cells[dtItemDeptt.Index];
                        dataGridView1.Rows[row].Cells["dtItemDeptt"].Value = Database.myreader["ItemDeptt"];
                        dataGridView1_CurrentCellDirtyStateChanged(dataGridView1, EventArgs.Empty);
                        Deptt = Database.myreader["ItemDeptt"].ToString();
                    }
                }


                dataGridView1.Rows[row].Cells["Qty"].Value =
                    item.Qty;

                dataGridView1.Rows[row].Cells["QtyUnit"].Value =
                    item.Unit;

                dataGridView1.Rows[row].Cells["NetWeight"].Value =
                    item.NetWeight * 1000;

                dataGridView1.Rows[row].Cells["Rate"].Value =
                    item.Rate;
            }
            totalpkg.Text = totalpackage.ToString();
            comboDeptt.Text = Deptt;
        }

        private void btnImportExcel_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();

             ofd.Title = "Select Export Invoice Excel";

             ofd.Filter = "Excel Files(*.xls;*.xlsx;*.xlsm)|*.xls;*.xlsx;*.xlsm";

             if (ofd.ShowDialog() != DialogResult.OK)
                 return;

             try
             {
                 Cursor.Current = Cursors.WaitCursor;

                 ExportInvoiceExcelReader reader = new ExportInvoiceExcelReader();

                 ExportInvoiceImport invoice = reader.Read(ofd.FileName);

                 Form_Load();

                 FillForm(invoice);

                 MessageBox.Show("Excel imported successfully.", "Import",
                     MessageBoxButtons.OK, MessageBoxIcon.Information);
             }
             catch (Exception ex)
             {
                 MessageBox.Show(
                     ex.ToString(),
                     "Import Error",
                     MessageBoxButtons.OK,
                     MessageBoxIcon.Error);
             }
             finally
             {
                 Cursor.Current = Cursors.Default;
             }
        }

        //Change by Rishabh on 25-07-2026 as per instructions by Anil sir for object highlight if validation fails
        private void validationTimer_Tick(object sender, EventArgs e)
        {
            if (highlightedControl == null)
            {
                validationTimer.Stop();
                return;
            }

            if (highlightedControl.BackColor == originalColor)
                highlightedControl.BackColor = Color.Yellow;
            else
                highlightedControl.BackColor = originalColor;

            flashCount++;

            if (flashCount >= 6)
            {
                validationTimer.Stop();

                highlightedControl.BackColor = originalColor;

                highlightedControl = null;

                flashCount = 0;
            }
        }

        private void HighlightControl(Control ctrl)
        {
            if (ctrl == null)
                return;

            validationTimer.Stop();

            if (highlightedControl != null)
            {
                highlightedControl.BackColor = originalColor;
            }

            // Find parent TabPage
            Control parent = ctrl.Parent;

            while (parent != null)
            {
                if (parent is TabPage)
                {
                    TabPage tp = (TabPage)parent;

                    if (tp.Parent is TabControl)
                    {
                        ((TabControl)tp.Parent).SelectedTab = tp;
                    }

                    break;
                }

                parent = parent.Parent;
            }

            ctrl.Focus();

            if (ctrl is TextBox)
                ((TextBox)ctrl).SelectAll();

            if (ctrl is ComboBox)
                ((ComboBox)ctrl).DroppedDown = true;

            highlightedControl = ctrl;
            originalColor = ctrl.BackColor;
            flashCount = 0;

            validationTimer.Start();
        }

        private void ShowValidation(string message, Control ctrl)
        {
            MessageBox.Show(message,
                            "Validation",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);

            HighlightControl(ctrl);
        }
        //Change End

    }
}