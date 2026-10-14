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
    public partial class frmDomesticInvoice : Form
    {
        ComboBox comboBox;
        public bool isupdate = false;
        DataTable dtIMaster;
        DataTable dtGroupName;
        DataTable dtSubGroupName;
        private DataSet ds = new DataSet("myDs");
        private DataTable dt = new DataTable("myDT");
        AutoCompleteStringCollection DataItems = new AutoCompleteStringCollection();
        string CompanyCode = "";
        ArrayList ArrCommodity = new ArrayList();
        public int TotalCount = 0;
        double Excise = 0;
        double Educess = 0;
        double HiEduCess = 0;
        double AddVat = 0;
        double Vat = 0;
        double Friegth = 0;
        double TCS = 0;

        //FOR oel2
        public double oelbasic = 0;
        public double CVD = 0;
        public double educess1 = 0;
        public double shecess1 = 0;
        public double educess2 = 0;
        public double shecess2 = 0;
        public double educess3 = 0;
        public double shecess3 = 0;
        public double AddDuty = 0;


        DateTime dtUT1Bond;
        DateTime dtUT1Expiry;

        public frmDomesticInvoice()
        {
            InitializeComponent();
            Form_Load();
        }

        private void Form_Load()
        {

            try
            {
                groupAvailBenefit.Visible = false;
                //DialogResult drs = MessageBox.Show("Do you want to Avail the Benefit 39-A", "Avail Benefit 39-A"
                //    , MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                //if (drs.CompareTo(DialogResult.Yes) == 0)
                //    checkAvailBenefit.Checked = true;
                dateTimePickerInvDate.Value = DateTime.Now;
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
                string strsql = "";
                checkIsPurchaseReturn.Checked = false;
                if (DateTime.Now >= Convert.ToDateTime("2019-01-01"))
                    checkIsPurchaseReturn.Visible = false;
                else
                    checkIsPurchaseReturn.Visible = true;

                comboTypeInvoice.SelectedIndex = 0;
                if (Database.OpenConnection(Utility.DespacthConnectionString))
                {
                    DespatchCommonFunction.GetCompanyList(comboCompanyName);
                    comboCompanyName.Text = frmDefaultVale.CompanyName;
                    DespatchCommonFunction.GetDepttName(comboDeptt);

                    strsql = "select distinct paymenttermname from MaterialProcessing..PaymentTermsMaster where Type='Sales' and isdelflag=0 order by paymenttermname";
                    DespatchCommonFunction.Loadcombo(textTermsofPayment, strsql, "paymenttermname", "paymenttermname", Utility.DespacthConnectionString);

                    DespatchCommonFunction.GetTransportMaster(comboTranporterName);

                    strsql = "Select Distinct(vehicalNo) as vehicalNo from DomesticInvoice WITH(NOLOCK)  order by vehicalNo";
                    DespatchCommonFunction.Loadcombo(comboVehicalNo, strsql, "vehicalNo", "vehicalNo", Utility.DespacthConnectionString);
                    DespatchCommonFunction.GetCurrencyCombo(comboCurrency, Utility.DespacthConnectionString);

                    strsql = "Select Distinct(Destination) as Destination from DomesticInvoice WITH(NOLOCK)  order by Destination";
                    DespatchCommonFunction.Loadcombo(comboDestination, strsql, "Destination", "Destination", Utility.DespacthConnectionString);

                    Database.myreader = Database.GetExecuteReaderCommand("select * from MaterialProcessing..excisemaster");
                    if (Database.myreader.Read())
                    {
                        textExDuty.Text = Database.myreader[0].ToString();
                        textEduCess.Text = Database.myreader[1].ToString();
                        textSecHiEdu.Text = Database.myreader[2].ToString();
                        textOEL2CVD.Text = Database.myreader[0].ToString();
                        texToel2Cess1.Text = Database.myreader[1].ToString();
                        texToel2SHECess1.Text = Database.myreader[2].ToString();
                    }
                    Database.myreader.Close();

                    strsql = "Select Distinct(StateName) as StateName from StateMaster WITH(NOLOCK)  ";
                    DespatchCommonFunction.Loadcombo(comboState, strsql, "StateName", "StateName", Utility.DespacthConnectionString);


                    Database.Closeconnection();
                }
                //if (comboBuyerName.Items.Count > 0)
                //    comboBuyerName.SelectedIndex = 0;

                if (comboTranporterName.Items.Count > 0)
                    comboTranporterName.SelectedIndex = 0;

                //if (comboCompanyName.Items.Count > 0)
                //    comboCompanyName.SelectedIndex = 1;

                if (comboCurrency.Items.Count > 0)
                    comboCurrency.SelectedIndex = 0;

                //comboType.SelectedIndex = 0;

                comboExicseApp.SelectedIndex = 0;
                comboVatApp.SelectedIndex = 0;

                groupInsurance.Visible = false;

                textSuffix.Text = frmDefaultVale.Year;

                fillData();

                if (Database.OpenConnection(Utility.MaterialConnectionString))
                {
                    strsql = "";
                    strsql = "select LedgerName from ledgermaster where isinvoice = 'yes' and companyname = '" + comboCompanyName.Text + "' ORDER BY LedgerName ";//and Typeofinvoice = 'Domestic';
                    DespatchCommonFunction.Loadcombo(comboAccountHead, strsql, "LedgerName", "LedgerName", Utility.MaterialConnectionString);
                    Database.Closeconnection();
                }

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


        void fillData(Boolean blnupdate = false)
        {

            if (Database.OpenConnection(Utility.DespacthConnectionString))
            {
                if (blnupdate)
                {
                    Database.myadapter = Database.GetAdapterCommand("Select DISTINCT CommodityName from Commodity where companyname = '" + comboCompanyName.Text
                        + "'  order by CommodityName");
                }
                else
                {
                    Database.myadapter = Database.GetAdapterCommand("Select DISTINCT CommodityName from Commodity where companyname = '" + comboCompanyName.Text
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

            }

            //if (Database.OpenConnection(Utility.MaterialConnectionString))
            {

                Database.myadapter = Database.GetAdapterCommand("Select DISTINCT UnitName from MaterialProcessing.dbo.Unit order by UnitName");
                DataTable dt = new DataTable();
                Database.myadapter.Fill(dt);

                DUOM.DataSource = dt;
                DUOM.ValueMember = dt.Columns[0].ColumnName.ToString();
                DUOM.DisplayMember = dt.Columns[0].ColumnName.ToString();

                #region ITem Master Added
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
                if (dataGridView1.Rows[i].Cells[DCommodity.Index].FormattedValue.ToString() != "" &&
                   dataGridView1.Rows[i].Cells[DUOM.Index].FormattedValue.ToString() != "" &&
                   dataGridView1.Rows[i].Cells[DOrderQty.Index].FormattedValue.ToString() != "" &&
                   dataGridView1.Rows[i].Cells[DUnitPrice.Index].FormattedValue.ToString() != "")
                {
                    if (Database.OpenConnection(Utility.DespacthConnectionString))
                    {
                        Database.myreader = Database.GetExecuteReaderCommand("select THeading from commodity where CommodityName ='"
                              + dataGridView1.Rows[i].Cells[DCommodity.Index].Value.ToString() + "' and companyname = '"
                              + comboCompanyName.Text + "'");
                        while (Database.myreader.Read())
                            ArrTHeading.Add(Database.myreader[0].ToString());
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
            DateTime invdatetemc = new DateTime();
            DateTime invdatetemc2 = new DateTime();
            bool isMultilayer = false;
            string InvType = "";

            string MerchantInvoice = "";
            if (checkMerchantExport.Checked)
                MerchantInvoice = "yes";


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
                        if (hsncode.Length == 0)
                        {
                            MessageBox.Show("Please Enter HSN Code for Item :" + dataGridView1.Rows[i].Cells[DCommodity.Index].Value);
                            return;
                        }
                        if ((hsncode.Length < 6) && (dateTimePickerInvDate.Value >= Convert.ToDateTime("2021-04-01")))
                        {
                            MessageBox.Show("Please Enter 6 digit HSN Code for Item :" + dataGridView1.Rows[i].Cells[DCommodity.Index].Value);
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
            if (comboDeptt.Text == "")
            {
                MessageBox.Show("Please enter Value in DepttName");
                return;
            }

            //if (comboDeptt.Text == "Multilayer")
            //    isMultilayer = true;

            if (!FrmMain.IsCreateDomesticInvoice)
                MessageBox.Show("You have no right to save Domestic Invoice ");
            else if (textBoxLRNo.Text == "")
                MessageBox.Show("Please enter Value in LRNo");
            else if (comboState.Text == "")
                MessageBox.Show("Please select State ");
            else if (!(textTermsofPayment.Items.Contains(textTermsofPayment.Text.Trim())))
                MessageBox.Show("Please select proper payment terms or enter payment term in payment master");
            else if (textBuyerOrderNo.Text.Length == 0)
                MessageBox.Show("Please Enter Proper Buyer Order No");
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
                if (Database.OpenConnection(Utility.DespacthConnectionString))
                {
                    if (dateTimePickerInvDate.Value >= Convert.ToDateTime("2017-07-01 00:00:00"))
                    {
                        Database.myreader = Database.GetExecuteReaderCommand("Select InvNo,CAST(FLOOR(CAST(InvDateTime AS float)) AS datetime) from DomesticInvoice where suffix = '"
                               + textSuffix.Text + "' and companyname = '" + comboCompanyName.Text + "' order by InvNo desc");
                        if (Database.myreader.Read())
                            invdatetemc = Convert.ToDateTime(Database.myreader[1].ToString());
                        Database.myreader.Close();
                    }
                    else
                    {
                        Database.myreader = Database.GetExecuteReaderCommand("Select InvNo,CAST(FLOOR(CAST(InvDateTime AS float)) AS datetime) from DomesticInvoice where suffix = '"
                                + textSuffix.Text + "' and companyname = '" + comboCompanyName.Text + "' order by InvNo desc");
                        if (Database.myreader.Read())
                            invdatetemc = Convert.ToDateTime(Database.myreader[1].ToString());
                        Database.myreader.Close();
                    }
                    Database.Closeconnection();
                }

                if (invdatetemc <= invdatetemc2)
                    invdatetemc = System.DateTime.Now;

                invdatetemc = invdatetemc.AddDays(-1);
                if (invdatetemc >= Convert.ToDateTime(dateTimePickerInvDate.Value.ToString()))
                {
                    MessageBox.Show("Please change Invoice date ");
                }
                else
                {
                    double total = 0;
                    bool IsWt = false;
                    bool IsBlank = false;
                    try
                    {

                        for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                        {
                            if (dataGridView1.Rows[i].Cells[DCommodity.Index].FormattedValue.ToString() != ""
                                && dataGridView1.Rows[i].Cells[DtDeptt.Index].FormattedValue.ToString() != ""
                                && dataGridView1.Rows[i].Cells[DUOM.Index].FormattedValue.ToString() != ""
                                && dataGridView1.Rows[i].Cells[DOrderQty.Index].FormattedValue.ToString() != ""
                                && dataGridView1.Rows[i].Cells[DUnitPrice.Index].FormattedValue.ToString() != "")
                            {

                                double t = Convert.ToDouble(dataGridView1.Rows[i].Cells[DOrderQty.Index].Value.ToString());
                                t = Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[DUnitPrice.Index].Value.ToString());
                                t = Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[DTotal.Index].Value.ToString());
                                t = Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[DNetWt.Index].Value.ToString());
                                t = Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[DGrossWt.Index].Value.ToString());

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
                        if (!IsBlank)
                        {
                            Int32 InvNo = 0;
                            //total = Calculation();
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
                                Database.myreader = Database.GetExecuteReaderCommand("Select CompanyName from CompanyMaster where CompanyName = '"
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
                                        if (checkMerchantExport.Checked == false)
                                        {
                                            int InvoiceSeries = 0;
                                            InvType = DespatchCommonFunction.GenerateDomesticInvno(comboCompanyName.Text, textSuffix.Text, ref InvoiceSeries);//prefixdespatchcode + "/JOB/" + textyear.Text + "/" + InvNo.ToString();
                                            InvNo = InvoiceSeries;
                                        }
                                        else
                                        {
                                            int InvoiceSeries = 0;
                                            InvType = DespatchCommonFunction.GenerateMechantExportInvno(comboCompanyName.Text, textSuffix.Text, ref InvoiceSeries);//prefixdespatchcode + "/JOB/" + textyear.Text + "/" + InvNo.ToString();
                                            InvNo = InvoiceSeries;
                                     
                                        }
                                    }


                                    string ConsigneeCode = "";
                                    if (Database.OpenConnection(Utility.DespacthConnectionString))
                                    {
                                        Database.myreader = Database.GetExecuteReaderCommand("select ConsigneeCode from Consignee where consigneename = '"
                                             + comboConsigneeName.Text + "' and companycode ='" + CompanyCode
                                             + "' and city = '" + comboConsigneeCity.Text + "'");
                                        if (Database.myreader.Read())
                                            ConsigneeCode = Database.myreader[0].ToString();
                                        Database.myreader.Close();
                                    }

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
                                        Database.OpenConnection(Utility.DespacthConnectionString);//12.03.2022
                                        Database.BeginTransaction();


                                        string BuyerDate = "";
                                        string ExciseApplicable = "";
                                        string InsuranceApp = "";
                                        string Insurance = "";
                                        string UtBondDetails = "";
                                        bool isSave = false;

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

                                        #region OEL2
                                        if (frmDefaultVale.CompanyName == "Oswal Extrusion Limited (Unit-II)")
                                        {
                                            Database.GetExecuteNonQueryCommand("insert into DomesticInvoiceoel2 values('"
                                                + InvNo + "','"
                                                + textSuffix.Text + "','" + frmDefaultVale.CompanyName + "','"
                                                + textOel2Basic.Text + "','" + textOEL2CVD.Text + "','"
                                                + texToel2Cess1.Text + "','" + texToel2Cess2.Text
                                                + "','" + texToel2Cess3.Text + "','" + texToel2SHECess1.Text
                                                + "','" + texToel2SHECess2.Text + "','" + texToel2SHECess3.Text
                                                + "','" + oelbasic + "','" + CVD + "','" + educess1 + "','" + educess2
                                                + "','" + educess3 + "','" + shecess1 + "','" + shecess2 + "','" + shecess3 + "', '" + TextOEL2AddDuty.Text + "','" + AddDuty + "')");
                                            // Add field Addduty,TextOEL2AddDuty in insert query also add both filed in databse For Qry for OEL2 with Inv no 21 and 23 by KUSHAL
                                            //MessageBox.Show("Domestic Invoice " + InvNo + " is generated succesfully");
                                        }
                                        #endregion
                                        #region DomesticInvoice
                                        if (BuyerDate == "")
                                        {
                                            if (Database.GetExecuteNonQueryCommand("insert into DomesticInvoice(SalesOrderNo,InvNo,InvDate,TermsOfPayment,BuyerOrderNo,BuyerDate,DespatchNo,DespatchDate,VehicalNo,Destination,TermsOfDelivery,BuyerName,ExDuty,Educess,SecHiEduCess,vat,Addvat,ExciseAmt,EduCessAmt,SecHiEduCessAmt,VatAmt,AddVatAmt,TotalQty,TotalAmt,TotalAmtWord,InvDateTime,RemDateTime,TypeOfInvoice,DelChallanNo,DelChallanDate,LRNo,LRDate,LoginName,ExciseAmtWord,TransportName,Consignee,Heading,Insurance,Typevat,Remarks,CheckInsurance,Excisetype,VatType,Frieght,TCSPer,TCSAmt,Suffix,Deptt,Header,CompanyName,UTBond,Currency,Currencyvalue,PLA,RG23,ConsigneeCode,SupplementaryInvoice,AgainstInvNo,IsCancel,SpDuty1,SpDuty1Amt,SpDuty2,SpDuty2Amt,SpDuty3,SpDuty3Amt,AgainstInvDate,AccountHead,notifier,freightremarks,IsMArketing,ISFREIGHTINCLUDE,ExciseNilRemarks,VatNIlRemarks,State,ispurchasereturn,MobileNo,InvType,IsGSTAllowDuty1,IsGSTAllowDuty2,IsGSTAllowDuty3,IsNIllRated,MerchantExport,Notification,AdvanceLicense,isdispfrm,dispfrm,dispfrm_add) values('',"
                                                        + InvNo + ",'" + dateTimePickerInvDate.Value.ToString(DespatchCommonFunction.ERPDBDateSettings())
                                                        + "','" + textTermsofPayment.Text
                                                        + "','" + textBuyerOrderNo.Text
                                                        + "',null,'" + InvNo
                                                        + "','" + dateTimePickerDespatchDate.Value.ToString(DespatchCommonFunction.ERPDBDateSettings())
                                                        + "','" + comboVehicalNo.Text.ToUpper()
                                                        + "','" + comboDestination.Text.ToUpper()
                                                        + "','" + textTermsOfDelivery
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
                                                       + "','" + ((chkDirectExport.Checked) ? "yes" : "no")
                                                        + "','" + textRG23A.Text
                                                        + "','" + ConsigneeCode
                                                        + "',null,'','','" + textSpDutyRemarks1.Text
                                                        + "'," + textSpDuty1Amt.Text
                                                        + ",'" + textSpDutyRemark2.Text
                                                        + "'," + textSpDuty2Amt.Text
                                                        + ",'" + textSpDutyRemarks3.Text
                                                        + "'," + textSpDuty3Amt.Text
                                                        + ",'','" + comboAccountHead.Text + "','" + textNotifier.Text + "','"
                                                        + textFreightRemarks.Text + "','no','" + isfreghtinclude + "','"
                                                        + textExciseNIlRemarks.Text + "','" + textVatNilRmarks.Text + "','"
                                                        + comboState.Text + "','"
                                                        + ispurchasereturn + "','" + textMobile.Text + "','"
                                                        + InvType + "','" + (chkSpDu1.Checked == true ? "yes" : "no") + "','"
                                                        + (chkSpDu2.Checked == true ? "yes" : "no") + "','"
                                                        + (chkSpDu3.Checked == true ? "yes" : "no") + "','"
                                                        + (chknill.Checked == true ? "yes" : "no") + "','"
                                                        + (checkMerchantExport.Checked == true ? "yes" : "no") + "','"
                                                        + textNotification.Text + "','"
                                                        + comboAdvLicenseNo.Text + "',"
                                                        + (chk_dispfrm.Checked ? "1" : "0") + ","
                                                        + "'" + combodispfrm.Text + "',"
                                                        + "'" + textdispfrm_add.Text + "')")) ;
                                                isSave = true;
                                        }
                                        else
                                        {
                                            if (Database.GetExecuteNonQueryCommand("insert into DomesticInvoice(SalesOrderNo,InvNo,InvDate,TermsOfPayment,BuyerOrderNo,BuyerDate,DespatchNo,DespatchDate,VehicalNo,Destination,TermsOfDelivery,BuyerName,ExDuty,Educess,SecHiEduCess,vat,Addvat,ExciseAmt,EduCessAmt,SecHiEduCessAmt,VatAmt,AddVatAmt,TotalQty,TotalAmt,TotalAmtWord,InvDateTime,RemDateTime,TypeOfInvoice,DelChallanNo,DelChallanDate,LRNo,LRDate,LoginName,ExciseAmtWord,TransportName,Consignee,Heading,Insurance,Typevat,Remarks,CheckInsurance,Excisetype,VatType,Frieght,TCSPer,TCSAmt,Suffix,Deptt,Header,CompanyName,UTBond,Currency,Currencyvalue,PLA,RG23,ConsigneeCode,SupplementaryInvoice,AgainstInvNo,IsCancel,SpDuty1,SpDuty1Amt,SpDuty2,SpDuty2Amt,SpDuty3,SpDuty3Amt,AgainstInvDate,AccountHead,notifier,freightremarks,IsMArketing,ISFREIGHTINCLUDE,ExciseNilRemarks,VatNIlRemarks,State,ispurchasereturn, MobileNo,InvType,IsGSTAllowDuty1,IsGSTAllowDuty2,IsGSTAllowDuty3,IsNIllRated,MerchantExport,Notification,AdvanceLicense,isdispfrm,dispfrm,dispfrm_add) values('',"
                                                        + InvNo + ",'" + dateTimePickerInvDate.Value.ToString(DespatchCommonFunction.ERPDBDateSettings())
                                                        + "','" + textTermsofPayment.Text
                                                        + "','" + textBuyerOrderNo.Text
                                                        + "','" + dateTimeBuyerOrderDate.Value.ToString(DespatchCommonFunction.ERPDBDateSettings())
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
                                                        + "','" + ((chkDirectExport.Checked) ? "yes" : "no")
                                                        + "','" + textRG23A.Text
                                                        + "','" + ConsigneeCode
                                                        + "',null,'','','" + textSpDutyRemarks1.Text
                                                        + "'," + textSpDuty1Amt.Text
                                                        + ",'" + textSpDutyRemark2.Text
                                                        + "'," + textSpDuty2Amt.Text
                                                        + ",'" + textSpDutyRemarks3.Text
                                                        + "'," + textSpDuty3Amt.Text
                                                        + ",'','" + comboAccountHead.Text + "','" + textNotifier.Text + "','"
                                                        + textFreightRemarks.Text + "','no','" + isfreghtinclude + "','"
                                                        + textExciseNIlRemarks.Text + "','" + textVatNilRmarks.Text + "','"
                                                        + comboState.Text + "','" + ispurchasereturn + "','" + textMobile.Text + "','"
                                                        + InvType + "','" + (chkSpDu1.Checked == true ? "yes" : "no") + "','"
                                                        + (chkSpDu2.Checked == true ? "yes" : "no") + "','"
                                                        + (chkSpDu3.Checked == true ? "yes" : "no") + "','"
                                                        + (chknill.Checked == true ? "yes" : "no") + "','"
                                                        + (checkMerchantExport.Checked == true ? "yes" : "no") + "','"
                                                        + textNotification.Text + "','"
                                                        + comboAdvLicenseNo.Text + "',"
                                                        + (chk_dispfrm.Checked ? "1" : "0") + ","
                                                        + "'" + combodispfrm.Text + "',"
                                                        + "'" + textdispfrm_add.Text + "')")) ;
                                                isSave = true;
                                        }
                                        #endregion
                                        #region DomesticInvItem
                                        UpdateInsurancePolicy(Convert.ToDouble(textGrdTotal.Text), checkInsurance.Checked);
                                        for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                                        {
                                            // by raj 08.12.2021 NO NEEDS TO CHECK CONDITION FOR COMM,DEPT,ORDER,UNITTOTAL AS WE WILL INSERT ALL  DETAILS.
                                            //if (
                                            //        dataGridView1.Rows[i].Cells[DCommodity.Index].FormattedValue.ToString() != "" &&
                                            //        dataGridView1.Rows[i].Cells[DtDeptt.Index].FormattedValue.ToString() != "" &&
                                            //        dataGridView1.Rows[i].Cells[DOrderQty.Index].FormattedValue.ToString() != "" &&
                                            //        dataGridView1.Rows[i].Cells[DUnitPrice.Index].FormattedValue.ToString() != "" &&
                                            //        dataGridView1.Rows[i].Cells[DTotal.Index].FormattedValue.ToString() != ""
                                            //    )
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
                                                              + InvNo + " and commodity = '" + dataGridView1.Rows[i].Cells[1].Value.ToString() + "' and yr = '"
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
                                                    dataGridView1.Rows[i].Cells[DNoofPkg.Index].Value = "";

                                                if (dataGridView1.Rows[i].Cells[DItemDesc.Index].FormattedValue.ToString() == "")
                                                    dataGridView1.Rows[i].Cells[DItemDesc.Index].Value = "";

                                                if (dataGridView1.Rows[i].Cells[DOrderQty.Index].FormattedValue.ToString() == "")
                                                    dataGridView1.Rows[i].Cells[DOrderQty.Index].Value = "";

                                                if (dataGridView1.Rows[i].Cells[DUnitPrice.Index].FormattedValue.ToString() == "")
                                                    dataGridView1.Rows[i].Cells[DUnitPrice.Index].Value = "";

                                                if (dataGridView1.Rows[i].Cells[DUOM.Index].FormattedValue.ToString() == "")
                                                    dataGridView1.Rows[i].Cells[DUOM.Index].Value = "";

                                                if (dataGridView1.Rows[i].Cells[DTotal.Index].FormattedValue.ToString() == "")
                                                    dataGridView1.Rows[i].Cells[DTotal.Index].Value = "";

                                                if (dataGridView1.Rows[i].Cells[DNetWt.Index].FormattedValue.ToString() == "")
                                                    dataGridView1.Rows[i].Cells[DNetWt.Index].Value.ToString();
                                                if (dataGridView1.Rows[i].Cells[DGrossWt.Index].FormattedValue.ToString() == "")
                                                    dataGridView1.Rows[i].Cells[DGrossWt.Index].Value.ToString();

                                                if (dataGridView1.Rows[i].Cells[DNonDisplayUnit.Index].FormattedValue.ToString() == "")
                                                    dataGridView1.Rows[i].Cells[DNonDisplayUnit.Index].Value = "";


                                                double NonDisplayQty = 0;
                                                if (dataGridView1.Rows[i].Cells[DUOM.Index].FormattedValue.ToString() ==
                                                        dataGridView1.Rows[i].Cells[DNonDisplayUnit.Index].FormattedValue.ToString())
                                                    NonDisplayQty = Convert.ToDouble(dataGridView1.Rows[i].Cells[DOrderQty.Index].Value);
                                                else if (dataGridView1.Rows[i].Cells[DNonDisplayQty.Index].FormattedValue.ToString() == "")
                                                    NonDisplayQty = 0;
                                                else
                                                    NonDisplayQty = Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[DNonDisplayQty.Index].Value);

                                                if (dataGridView1.Rows[i].Cells[DQtyRemarks.Index].FormattedValue.ToString() == "")
                                                    dataGridView1.Rows[i].Cells[DQtyRemarks.Index].Value = "";

                                                if (dataGridView1.Rows[i].Cells[DCommodityRemarks.Index].FormattedValue.ToString() == "")
                                                    dataGridView1.Rows[i].Cells[DCommodityRemarks.Index].Value = "";


                                                Database.GetExecuteNonQueryCommand("insert into DomesticInvItem(InvNo,NoofPkg,ItemDesc,Qty,Rate,Unit,Amount,NetWt,GrossWt,SalesOrderNo,LoginName,Commodity,SrNo,GroupCommodity,NonDisplayUnit,NonDisplayQty,QtyRemarks,Yr,Companyname,CommodityRemarks,Desc1,Desc2,BagsMRP,size,weekno,LotNo,Colour,Meter,GPM,GSM,WBWeight,ReqWtperBag,ReqWtTotal,DispatchWtperBagGr,WtTolperBagGr,WtExtrapershortKg,WtDiffbetPLWB,AssesableValue,OrderNO,itemDeptt,HSNCode,CGSTrate,CGSTamt,SGSTRate,SGSTAmt,IGSTRate,IGSTAmt,Itemcode,MerchantExport) values('"
                                                    + InvNo + "','" + dataGridView1.Rows[i].Cells[DNoofPkg.Index].Value
                                                    + "','" + Convert.ToString(dataGridView1.Rows[i].Cells[DItemDesc.Index].Value)
                                                    + "','" + Convert.ToString(dataGridView1.Rows[i].Cells[DOrderQty.Index].Value)
                                                    + "','" + Convert.ToString(dataGridView1.Rows[i].Cells[DUnitPrice.Index].Value)
                                                    + "','" + Convert.ToString(dataGridView1.Rows[i].Cells[DUOM.Index].Value)
                                                    + "','" + Convert.ToString(dataGridView1.Rows[i].Cells[DTotal.Index].Value)
                                                    + "','" + Convert.ToString(dataGridView1.Rows[i].Cells[DNetWt.Index].Value)
                                                    + "','" + Convert.ToString(dataGridView1.Rows[i].Cells[DGrossWt.Index].Value) + "','"
                                                    + "','" + FrmMain.UserName
                                                    + "','" + Convert.ToString(dataGridView1.Rows[i].Cells[DCommodity.Index].Value)
                                                    + "','" + Convert.ToString((i + 1))
                                                    + "','" + GroupName
                                                    + "','" + Convert.ToString(dataGridView1.Rows[i].Cells[DNonDisplayUnit.Index].Value)
                                                    + "','" + Convert.ToString(NonDisplayQty)
                                                    + "','" + Convert.ToString(dataGridView1.Rows[i].Cells[DQtyRemarks.Index].Value)
                                                    + "','" + textSuffix.Text
                                                    + "','" + comboCompanyName.Text
                                                    + "','" + Convert.ToString(dataGridView1.Rows[i].Cells[DCommodityRemarks.Index].Value)
                                                    + "',null,null,null,null,null,null,null,null,null,null,null,"
                                                    + "null,null,null,null,null,null,null,'','"
                                                    + Convert.ToString(dataGridView1.Rows[i].Cells[DtDeptt.Index].Value) + "','"
                                                    + Convert.ToString(dataGridView1.Rows[i].Cells[colHSNCode.Index].Value) + "',"
                                                    + Convert.ToDouble(dataGridView1.Rows[i].Cells[colCGSTrate.Index].Value) + ","
                                                    + Convert.ToDouble(dataGridView1.Rows[i].Cells[colCGSTamt.Index].Value) + ","
                                                    + Convert.ToDouble(dataGridView1.Rows[i].Cells[colSGSTRate.Index].Value) + ","
                                                    + Convert.ToDouble(dataGridView1.Rows[i].Cells[ColSGSTAmt.Index].Value) + ","
                                                    + Convert.ToDouble(dataGridView1.Rows[i].Cells[colIGSTRate.Index].Value) + ","
                                                    + Convert.ToDouble(dataGridView1.Rows[i].Cells[colIGSTAmt.Index].Value) + ",'"
                                                    + Convert.ToString(dataGridView1.Rows[i].Cells[colItemCode.Index].Value) +"','"
                                                    + (checkMerchantExport.Checked == true ? "yes" : "no") 
                                                    + "')");


                                            }
                                        }
                                        #endregion
                                        #region DirectExport/ Domestic ARE1
                                        if (chkDirectExport.Checked)
                                        {
                                            Database.GetExecuteNonQueryCommand("insert into domesticARE1  (InvNo,CompanyName,Year,AdvNo,UT1BondNo) values('"
                                                + InvNo + "','" + frmDefaultVale.CompanyName + "','" + textSuffix.Text + "','"
                                                + txtAdvanceLicNo.Text + "','" + comboUT1Bond.Text + "')");

                                        }
                                        #endregion

                                        Database.CommitTransaction();
                                        Utility.UserInformation("Save", "Domestic Invoice " + InvNo.ToString() + " is saved successfully");
                                        if (isSave)
                                            MessageBox.Show("Domestic Invoice " + InvNo + " is generated succesfully");

                                        #region PackingList
                                        if (DespatchCommonFunction.AllowMappingPackinglist(comboCompanyName.Text) > 0)
                                        {
                                            if (Database.OpenConnection(Utility.DespacthConnectionString))
                                            {
                                                string strgstInv = Convert.ToString(Database.GetExecuteNonQueryCommand_retrun("SELECT GSTInvNo FROM Vw_DomesticInvoice WHERE InvNo=" +
                                                InvNo + " and companyname='" + comboCompanyName.Text + "' and ARESuffix='" + textSuffix.Text + "'"));

                                                Database.GetExecuteNonQueryCommand("INSERT INTO MappPackingList (CompanyName,InvNo,Yr,PackingListNo) VALUES('" +
                                                comboCompanyName.Text + "','" + strgstInv + "','" + textSuffix.Text + "','" + despackinglist + "')");
                                            }
                                        }
                                        #endregion


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
                                        btnUpdate.Enabled = false;
                                        btnSave.Enabled = true;
                                        checkFreight.Checked = false;
                                        chknill.Checked = false;
                                        dataGridView1.Rows.Clear();
                                        checkMerchantExport.Checked = false;
                                        //FrmDispatchReportPIL frm = new FrmDispatchReportPIL();
                                        //frm.Show();
                                        fillData(false);
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
            }
        }

        private double Calculation()
        {
            double total = 0;
            TotalCount = 0;

            try
            {
                if (frmDefaultVale.CompanyName == "Oswal Extrusion Limited (Unit-II)")
                {
                    textExDuty.Text = "0";
                    textEduCess.Text = "0";
                    textSecHiEdu.Text = "0";
                    for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                    {
                        if (dataGridView1.Rows[i].Cells[DCommodity.Index].FormattedValue.ToString() != "" &&
                         dataGridView1.Rows[i].Cells[DtDeptt.Index].FormattedValue.ToString() != "" &&
                         dataGridView1.Rows[i].Cells[DUOM.Index].FormattedValue.ToString() != "" &&
                           dataGridView1.Rows[i].Cells[DOrderQty.Index].FormattedValue.ToString() != "" &&
                        dataGridView1.Rows[i].Cells[DUnitPrice.Index].FormattedValue.ToString() != "")
                        {

                            ///ADD NEW LINE FOR ERPQtyDigit & RateDigit
                            ///Change by nikhil

                            dataGridView1.Rows[i].Cells[DOrderQty.Index].Value =
                            Math.Round(Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[DOrderQty.Index].Value), FrmMain.ERPQtyDigit);


                            dataGridView1.Rows[i].Cells[DUnitPrice.Index].Value =
                            Math.Round(Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[DUnitPrice.Index].Value), FrmMain.ERPRateDigit);


                            //dataGridView1.Rows[i].Cells[DTotal.Index].Value =
                            // Convert.ToDouble(dataGridView1.Rows[i].Cells[DOrderQty.Index].Value.ToString()) *
                            // Convert.ToDouble(dataGridView1.Rows[i].Cells[DUnitPrice.Index].Value.ToString());

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


                    }
                    total = Math.Round(total, 0);
                    double tempc = 0;
                    double tempsum1 = 0;
                    double tempsum2 = 0;

                    oelbasic = (total * Convert.ToDouble(textOel2Basic.Text)) / 100;
                    oelbasic = Math.Round(oelbasic, 0);                                 //b

                    tempc = total + oelbasic;
                    CVD = (tempc * Convert.ToDouble(textOEL2CVD.Text)) / 100;
                    CVD = Math.Round(CVD, 0);                                           //d

                    educess1 = ((CVD + oelbasic) * Convert.ToDouble(texToel2Cess1.Text)) / 100;
                    educess1 = Math.Round(educess1, 0);                                 //e

                    shecess1 = ((CVD + oelbasic) * Convert.ToDouble(texToel2SHECess1.Text)) / 100;
                    shecess1 = Math.Round(shecess1, 0);                                    //f

                    tempsum1 = oelbasic + CVD + educess1 + shecess1;

                    // Add field For Qry for OEL2 with Inv no 21 and 23 by KUSHAL
                    AddDuty = ((total + tempsum1) * Convert.ToDouble(TextOEL2AddDuty.Text)) / 100;
                    AddDuty = Math.Round(AddDuty, 0);
                    /////
                    tempsum1 = oelbasic + CVD + educess1 + shecess1 + AddDuty;

                    educess2 = (tempsum1 * Convert.ToDouble(texToel2Cess2.Text)) / 100;
                    educess2 = Math.Round(educess2, 0);                                 //g

                    shecess2 = (tempsum1 * Convert.ToDouble(texToel2SHECess2.Text)) / 100;
                    shecess2 = Math.Round(shecess2, 0);                                  //h

                    tempsum2 = tempsum1 + educess2 + shecess2;                              //i

                    educess3 = (tempsum2 * Convert.ToDouble(texToel2Cess3.Text)) / 100;
                    educess3 = Math.Round(educess3, 0);
                    shecess3 = (tempsum2 * Convert.ToDouble(texToel2SHECess3.Text)) / 100;
                    shecess3 = Math.Round(shecess3, 0);


                    total = total + tempsum2 + educess3 + shecess3;


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
                        //total = tempsum2 + educess3 + shecess3;
                        Vat = ((total) * Convert.ToDouble(textVat.Text)) / 100;
                        AddVat = ((total) * Convert.ToDouble(textAddVat.Text)) / 100;
                    }
                    Vat = Math.Round(Vat, 0);
                    AddVat = Math.Round(AddVat, 0);


                    //if (comboDeptt.Text != "Wastage")
                    //    total = total + tempsum2 + educess3 + shecess3;

                    //  total = total + tempsum2 + educess3 + shecess3;
                    total = total + Vat + AddVat;
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
                else
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


                    //&& dataGrid1[i, 2].ToString() != ""
                    for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                    {
                        if (dataGridView1.Rows[i].Cells[DCommodity.Index].FormattedValue.ToString() != "" &&
                           dataGridView1.Rows[i].Cells[DOrderQty.Index].FormattedValue.ToString() != "" &&
                        dataGridView1.Rows[i].Cells[DUnitPrice.Index].FormattedValue.ToString() != "")
                        {


                            ///ADD NEW LINE FOR ERPQtyDigit & RateDigit
                            ///Change by nikhil

                            dataGridView1.Rows[i].Cells[DOrderQty.Index].Value =
                            Math.Round(Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[DOrderQty.Index].Value), FrmMain.ERPQtyDigit);


                            dataGridView1.Rows[i].Cells[DUnitPrice.Index].Value =
                            Math.Round(Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[DUnitPrice.Index].Value), FrmMain.ERPRateDigit);



                            //dataGridView1.Rows[i].Cells[DTotal.Index].Value =
                            // Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[DOrderQty.Index].Value) *
                            // Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[DUnitPrice.Index].Value);

                            ///ADD ERPAmoutnDigit
                            ///Change by nikhil
                            dataGridView1.Rows[i].Cells[DTotal.Index].Value =
                             Math.Round(Convert.ToDouble(dataGridView1.Rows[i].Cells[DOrderQty.Index].Value) *
                              Convert.ToDouble(dataGridView1.Rows[i].Cells[DUnitPrice.Index].Value), FrmMain.ERPAmoutnDigit);


                            //dataGridView1.Rows[i].Cells[DTotal.Index].Value =
                            //Convert.ToDouble(dataGridView1.Rows[i].Cells[DOrderQty.Index].Value) *
                            // Convert.ToDouble(dataGridView1.Rows[i].Cells[DUnitPrice.Index].Value);

                            total += Convert.ToDouble(dataGridView1.Rows[i].Cells[DTotal.Index].Value);
                            //total = Math.Round(total, 0, MidpointRounding.AwayFromZero);
                            ///ADD ERPAmoutnDigit
                            ///Change by nikhil
                            total = Math.Round(total, FrmMain.ERPAmoutnDigit, MidpointRounding.AwayFromZero);
                            TotalCount += 1;
                        }


                    }

                    total = Math.Round(total, 0);
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

                    //total = total + TCS + spduty1 + spduty2 + spduty3;

                    //total = Math.Round(total, 0);
                    ///ADD ERPAmoutnDigit
                    ///Change by nikhil
                    total = Math.Round(total, FrmMain.ERPAmoutnDigit);
                    textGrdTotal.Text = total.ToString();
                }
            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
            }
            return total;
        }

        private double CalculationGST()
        {

            double total = 0;
            TotalCount = 0;
            double dblCGSTamt = 0;
            double dblSGSTAmt = 0;
            double dblIGSTAmt = 0;
            double tempc = 0;
            double tempsum1 = 0;
            double tempsum2 = 0;
            double temptotal = 0;



            //FOR oel2
            double localoelbasic = 0;
            double localCVD = 0;
            double localeducess1 = 0;
            double localshecess1 = 0;
            double localeducess2 = 0;
            double localshecess2 = 0;
            double localeducess3 = 0;
            double localshecess3 = 0;
            double localAddDuty = 0;


            try
            {
                CheckGST();
                if (frmDefaultVale.CompanyName == "Oswal Extrusion Limited (Unit-II)")
                {
                    textExDuty.Text = "0";
                    textEduCess.Text = "0";
                    textSecHiEdu.Text = "0";
                    for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                    {
                        if (dataGridView1.Rows[i].Cells[DCommodity.Index].FormattedValue.ToString() != "" &&
                         dataGridView1.Rows[i].Cells[DtDeptt.Index].FormattedValue.ToString() != "" &&
                         dataGridView1.Rows[i].Cells[DUOM.Index].FormattedValue.ToString() != "" &&
                           dataGridView1.Rows[i].Cells[DOrderQty.Index].FormattedValue.ToString() != "" &&
                        dataGridView1.Rows[i].Cells[DUnitPrice.Index].FormattedValue.ToString() != "")
                        {

                            ///ADD NEW LINE FOR ERPQtyDigit & RateDigit
                            ///Change by nikhil

                            dataGridView1.Rows[i].Cells[DOrderQty.Index].Value =
                            Math.Round(Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[DOrderQty.Index].Value), FrmMain.ERPQtyDigit);


                            dataGridView1.Rows[i].Cells[DUnitPrice.Index].Value =
                            Math.Round(Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[DUnitPrice.Index].Value), FrmMain.ERPRateDigit);

                            //temptotal = Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[DOrderQty.Index].Value) *
                            // Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[DUnitPrice.Index].Value);
                            ///ADD ERPAmoutnDigit
                            ///Change by nikhil

                            temptotal = Math.Round(Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[DOrderQty.Index].Value) *
                            Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[DUnitPrice.Index].Value), FrmMain.ERPAmoutnDigit);

                            //dataGridView1.Rows[i].Cells[DTotal.Index].Value =
                            // Convert.ToDouble(dataGridView1.Rows[i].Cells[DOrderQty.Index].Value.ToString()) *
                            // Convert.ToDouble(dataGridView1.Rows[i].Cells[DUnitPrice.Index].Value.ToString());

                            total += Utility.SafeConvertToDouble(temptotal);
                            // total = Math.Round(total, 0, MidpointRounding.AwayFromZero);

                            ///ADD ERPAmoutnDigit
                            ///Change by nikhil
                            total = Math.Round(total, FrmMain.ERPAmoutnDigit, MidpointRounding.AwayFromZero);

                            oelbasic = (temptotal * Utility.SafeConvertToDouble(textOel2Basic.Text)) / 100;
                            oelbasic = Math.Round(oelbasic, 0);                                 //b
                            localoelbasic += oelbasic;

                            //tempc = Utility.SafeConvertToDouble( dataGridView1.Rows[i].Cells[DTotal.Index].Value) + oelbasic;
                            CVD = (oelbasic * Utility.SafeConvertToDouble(textOEL2CVD.Text)) / 100;
                            CVD = Math.Round(CVD, 0);                                           //d
                            localCVD += CVD;

                            educess1 = ((CVD + oelbasic) * Utility.SafeConvertToDouble(texToel2Cess1.Text)) / 100;
                            educess1 = Math.Round(educess1, 0);                                 //e
                            localeducess1 += educess1;

                            shecess1 = ((CVD + oelbasic) * Utility.SafeConvertToDouble(texToel2SHECess1.Text)) / 100;
                            shecess1 = Math.Round(shecess1, 0);                                    //f
                            localshecess1 += shecess1;

                            tempsum1 = oelbasic + CVD + educess1 + shecess1;

                            // Add field For Qry for OEL2 with Inv no 21 and 23 by KUSHAL
                            AddDuty = ((total + tempsum1) * Utility.SafeConvertToDouble(TextOEL2AddDuty.Text)) / 100;
                            AddDuty = Math.Round(AddDuty, 0);
                            localAddDuty += AddDuty;

                            /////
                            tempsum1 = oelbasic + CVD + educess1 + shecess1 + AddDuty;

                            educess2 = (tempsum1 * Utility.SafeConvertToDouble(texToel2Cess2.Text)) / 100;
                            educess2 = Math.Round(educess2, 0);                                 //g
                            localeducess2 += educess2;

                            shecess2 = (tempsum1 * Utility.SafeConvertToDouble(texToel2SHECess2.Text)) / 100;
                            shecess2 = Math.Round(shecess2, 0);                                  //h
                            localshecess2 += shecess2;

                            tempsum2 = tempsum1 + educess2 + shecess2;                              //i

                            educess3 = (tempsum2 * Utility.SafeConvertToDouble(texToel2Cess3.Text)) / 100;
                            educess3 = Math.Round(educess3, 0);

                            localeducess3 += educess3;

                            shecess3 = (tempsum2 * Utility.SafeConvertToDouble(texToel2SHECess3.Text)) / 100;
                            shecess3 = Math.Round(shecess3, 0);
                            localshecess3 += shecess3;

                            dataGridView1.Rows[i].Cells[DTotal.Index].Value = temptotal + tempsum2 + educess3 + shecess3;
                            total = total + tempsum2 + educess3 + shecess3;
                            TotalCount += 1;
                        }

                        //colCGSTrate colCGSTamt  colSGSTRate   ColSGSTAmt

                        //dataGridView1.Rows[i].Cells[colCGSTamt.Index].Value =
                        //Math.Round(Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[DTotal.Index].Value) *
                        //Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[colCGSTrate.Index].Value) / 100, 2);

                        ///ADD ERPAmoutnDigit
                        ///Change by nikhil

                        dataGridView1.Rows[i].Cells[colCGSTamt.Index].Value =
                        Math.Round(Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[DTotal.Index].Value) *
                        Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[colCGSTrate.Index].Value) / 100, FrmMain.ERPAmoutnDigit);

                        dblCGSTamt += Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[colCGSTamt.Index].Value);


                        //dataGridView1.Rows[i].Cells[ColSGSTAmt.Index].Value =
                        //Math.Round(Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[DTotal.Index].Value) *
                        //Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[colSGSTRate.Index].Value) / 100, 2);

                        ///ADD ERPAmoutnDigit
                        ///Change by nikhil
                        dataGridView1.Rows[i].Cells[ColSGSTAmt.Index].Value =
                        Math.Round(Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[DTotal.Index].Value) *
                        Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[colSGSTRate.Index].Value) / 100, FrmMain.ERPAmoutnDigit);

                        dblSGSTAmt += Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[ColSGSTAmt.Index].Value);

                        //dataGridView1.Rows[i].Cells[colIGSTAmt.Index].Value =
                        //Math.Round(Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[DTotal.Index].Value) *
                        //Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[colIGSTRate.Index].Value) / 100, 2);

                        ///ADD ERPAmoutnDigit
                        ///Change by nikhil
                        dataGridView1.Rows[i].Cells[colIGSTAmt.Index].Value =
                        Math.Round(Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[DTotal.Index].Value) *
                        Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[colIGSTRate.Index].Value) / 100, FrmMain.ERPAmoutnDigit);

                        dblIGSTAmt += Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[colIGSTAmt.Index].Value);

                    }
                    //total = Math.Round(total + dblSGSTAmt + dblCGSTamt + dblIGSTAmt, 2);

                    ///ADD ERPAmoutnDigit
                    ///Change by nikhil
                    total = Math.Round(total + dblSGSTAmt + dblCGSTamt + dblIGSTAmt, FrmMain.ERPAmoutnDigit);
                    if (checkFreight.Checked)
                    {
                        Friegth = Convert.ToDouble(textFrieght.Text);
                        total = total + Friegth;
                    }

                    total = total + Vat + AddVat;
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
                    TCS = Math.Round(TCS, 0);

                    total = total + TCS;

                    //total = Math.Round(total, 0);
                    ///ADD ERPAmoutnDigit
                    ///Change by nikhil
                    total = Math.Round(total, FrmMain.ERPAmoutnDigit);
                    textGrdTotal.Text = total.ToString();

                    oelbasic = localoelbasic;
                    CVD = localCVD;
                    educess1 = localeducess1;
                    shecess1 = localshecess1;
                    educess2 = localeducess2;
                    shecess2 = localshecess2;
                    educess3 = localeducess3;
                    shecess3 = localshecess3;
                    AddDuty = localAddDuty;
                }
                else
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


                    //&& dataGrid1[i, 2].ToString() != ""
                    for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                    {
                        if (dataGridView1.Rows[i].Cells[DCommodity.Index].FormattedValue.ToString() != "" &&
                           dataGridView1.Rows[i].Cells[DOrderQty.Index].FormattedValue.ToString() != "" &&
                        dataGridView1.Rows[i].Cells[DUnitPrice.Index].FormattedValue.ToString() != "")
                        {

                            ///ADD NEW LINE FOR ERPQtyDigit & RateDigit
                            ///Change by nikhil

                            dataGridView1.Rows[i].Cells[DOrderQty.Index].Value =
                            Math.Round(Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[DOrderQty.Index].Value), FrmMain.ERPQtyDigit);


                            dataGridView1.Rows[i].Cells[DUnitPrice.Index].Value =
                            Math.Round(Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[DUnitPrice.Index].Value), FrmMain.ERPRateDigit);



                            //dataGridView1.Rows[i].Cells[DTotal.Index].Value =
                            // Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[DOrderQty.Index].Value) *
                            // Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[DUnitPrice.Index].Value);

                            ///ADD ERPAmoutnDigit
                            ///Change by nikhil
                            dataGridView1.Rows[i].Cells[DTotal.Index].Value =
                            Math.Round(Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[DOrderQty.Index].Value) *
                            Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[DUnitPrice.Index].Value), FrmMain.ERPAmoutnDigit);

                            total += Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[DTotal.Index].Value);
                            //total = Math.Round(total, 0, MidpointRounding.AwayFromZero);
                            TotalCount += 1;
                        }
                        //colCGSTrate colCGSTamt  colSGSTRate   ColSGSTAmt
                        double dbltotal = Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[DTotal.Index].Value);
                        double dblCGSTrate = Convert.ToDouble(dataGridView1.Rows[i].Cells[colCGSTrate.Index].Value);

                        //  dataGridView1.Rows[i].Cells[colCGSTamt.Index].Value = Math.Round((dbltotal) * dblCGSTrate / 100, 2);
                        ///ADD ERPAmoutnDigit
                        ///Change by nikhil

                        dataGridView1.Rows[i].Cells[colCGSTamt.Index].Value = Math.Round((dbltotal) * dblCGSTrate / 100, FrmMain.ERPAmoutnDigit);
                        dblCGSTamt += Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[colCGSTamt.Index].Value);


                        double dblSGSTRate = Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[colSGSTRate.Index].Value);
                        //dataGridView1.Rows[i].Cells[ColSGSTAmt.Index].Value =
                        //    Math.Round((dbltotal) * dblSGSTRate / 100, 2);

                        ///ADD ERPAmoutnDigit
                        ///Change by nikhil
                        dataGridView1.Rows[i].Cells[ColSGSTAmt.Index].Value =
                        Math.Round((dbltotal) * dblSGSTRate / 100, FrmMain.ERPAmoutnDigit);

                        dblSGSTAmt += Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[ColSGSTAmt.Index].Value);

                        double dblIGSTRate = Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[colIGSTRate.Index].Value);
                        //dataGridView1.Rows[i].Cells[colIGSTAmt.Index].Value =
                        //    Math.Round((dbltotal) * dblIGSTRate / 100, 2);

                        ///ADD ERPAmoutnDigit
                        ///Change by nikhil
                        dataGridView1.Rows[i].Cells[colIGSTAmt.Index].Value =
                        Math.Round((dbltotal) * dblIGSTRate / 100, FrmMain.ERPAmoutnDigit);

                        dblIGSTAmt += Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[colIGSTAmt.Index].Value);

                    }

                    Friegth = Convert.ToDouble(textFrieght.Text);
                    total = total + Friegth;

                    double findmaxrate = 0;
                    foreach (DataGridViewRow oItem in dataGridView1.Rows)
                    {
                        if (Utility.SafeConvertToDouble(oItem.Cells[colSGSTRate.Index].Value) > findmaxrate)
                        {
                            findmaxrate = Convert.ToDouble(oItem.Cells[colSGSTRate.Index].Value);
                        }
                        if (Utility.SafeConvertToDouble(oItem.Cells[colCGSTrate.Index].Value) > findmaxrate)
                        {
                            findmaxrate = Convert.ToDouble(oItem.Cells[colCGSTrate.Index].Value);
                        }
                        if (Utility.SafeConvertToDouble(oItem.Cells[colIGSTRate.Index].Value) > findmaxrate)
                        {
                            findmaxrate = Convert.ToDouble(oItem.Cells[colIGSTRate.Index].Value);
                        }
                    }

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
                    if (checkFreight.Checked && dblCGSTamt > 0)
                    {
                        dblCGSTamt += Math.Round((Friegth) * Convert.ToDouble(findmaxrate) / 100, FrmMain.ERPAmoutnDigit);
                    }

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


                    //total = Math.Round(total + dblSGSTAmt + dblCGSTamt + dblIGSTAmt, 0);

                    ///ADD ERPAmoutnDigit
                    ///Change by nikhil
                    total = Math.Round(total + dblSGSTAmt + dblCGSTamt + dblIGSTAmt, FrmMain.ERPAmoutnDigit);

                    double spduty1 = Utility.SafeConvertToDouble(textSpDuty1Amt.Text);
                    double spduty2 = Utility.SafeConvertToDouble(textSpDuty2Amt.Text);
                    double spduty3 = Utility.SafeConvertToDouble(textSpDuty3Amt.Text);
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
                        if (Convert.ToDouble(dataGridView1.Rows[i].Cells[DGrossWt.Index].Value.ToString()) <
                            Convert.ToDouble(dataGridView1.Rows[i].Cells[DNetWt.Index].Value.ToString()))
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
        private void CheckGST()
        {
            try
            {
                if (Database.OpenConnection(Utility.DespacthConnectionString))
                {
                    string Nsatate = Convert.ToString(Database.GetExecuteNonQueryCommand_retrun("SELECT distinct NState FROM  FactoryInfo where name='" + comboCompanyName.Text + "'"));

                    for (int i = 0; i <= dataGridView1.Rows.Count - 1; i++)
                    {
                        string Commodity = Convert.ToString(dataGridView1.Rows[i].Cells[DCommodity.Index].Value);
                        if (Commodity.Length == 0)
                            continue;

                        string THeading = "";
                        Database.OpenConnection(Utility.DespacthConnectionString);
                        Database.myreader = Database.GetExecuteReaderCommand("Select top 1 THeading,NCGSTRate,NSGSTRate,NIGSTRate from CommodityHSNGST where companyname = '"
                            + comboCompanyName.Text + "' AND  CommodityName='" + Commodity + "' and (('" + dateTimePickerInvDate.Value.ToString("yyyy-MM-dd")
                                    + "' between nFromDate and nToDate) or (nFromDate <= '"
                                    + dateTimePickerInvDate.Value.ToString("yyyy-MM-dd") + "' and nToDate is null ))");
                        if (Database.myreader.Read())
                        {
                            THeading = Database.myreader[0].ToString();
                            THeading = Database.myreader["THeading"].ToString();

                            if (dateTimePickerInvDate.Value.Date >= Convert.ToDateTime("2021-04-01"))
                                dataGridView1.Rows[i].Cells[colHSNCode.Index].Value = THeading;
                            else
                                dataGridView1.Rows[i].Cells[colHSNCode.Index].Value = THeading.Substring(0, 4);

                            if (checkMerchantExport.Checked == false)
                            {
                                if (comboState.Text.ToUpper() == Nsatate.ToUpper())
                                {
                                    dataGridView1.Rows[i].Cells[colCGSTrate.Index].Value = Database.myreader["NCGSTRate"].ToString();
                                    dataGridView1.Rows[i].Cells[colSGSTRate.Index].Value = Database.myreader["NSGSTRate"].ToString();
                                    dataGridView1.Rows[i].Cells[colIGSTRate.Index].Value = 0;
                                }
                                else
                                {
                                    dataGridView1.Rows[i].Cells[colCGSTrate.Index].Value = 0;
                                    dataGridView1.Rows[i].Cells[colSGSTRate.Index].Value = 0;
                                    dataGridView1.Rows[i].Cells[colIGSTRate.Index].Value = Database.myreader["NIGSTRate"].ToString();
                                }
                            }
                        }

                    }
                }
            }
            catch (Exception ex)
            {

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
                isupdate = true;
                string consigneecode = "";
                if (Database.OpenConnection(Utility.DespacthConnectionString))
                {
                    string MerchantCondition = " and MerchantExport ='";
                    if (checkMerchantExport.Checked)
                        MerchantCondition += "yes" + "'";
                    else
                        MerchantCondition = " and isnull(MerchantExport,'no') = 'no'";
                    string CompanyName = "";
                    string Currency = "";
                    bool IsCheckIsurance = false;
                    bool ismarketing = false;
                    string state = "";
                    Database.myreader = Database.GetExecuteReaderCommand("select IsMarketing from domesticinvoice where invno = "
                    + textInvNo.Text + " and companyName = '" + comboCompanyName.Text + "' and suffix = '" + textSuffix.Text + "' and ismarketing = 'yes'");
                    if (Database.myreader.Read())
                        ismarketing = true;
                    Database.myreader.Close();
                    if (ismarketing)
                        MessageBox.Show("Please Update InvNo in Domestic  Invoice against Order No");
                    else
                    {
                        Database.myreader = Database.GetExecuteReaderCommand("select * from DomesticInvoice  with (NOLOCK) where InvNo = " +
                                                                            textInvNo.Text + " and suffix ='" + textSuffix.Text + "' and companyname = '"
                                                                            + comboCompanyName.Text + "'" + MerchantCondition);
                        if (Database.myreader.Read())
                        {
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
                            dateTimePickerInvDate.Value = Convert.ToDateTime(Database.myreader[2].ToString());
                            textTermsofPayment.Text = Database.myreader[3].ToString();
                            textBuyerOrderNo.Text = Database.myreader[4].ToString();
                            if (textBuyerOrderNo.Text != "")
                                dateTimeBuyerOrderDate.Value = Convert.ToDateTime(Database.myreader[5].ToString());
                            comboTypeInvoice.Text = Database.myreader[27].ToString();
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
                            textMobile.Text = Database.myreader["MobileNo"].ToString();
                            textVatNilRmarks.Text = Database.myreader["VatNilRemarks"].ToString();
                            textExciseNIlRemarks.Text = Database.myreader["ExciseNilRemarks"].ToString();
                            if (Database.myreader[70].ToString() == "yes")
                                checkFreight.Checked = true;
                            else
                                checkFreight.Checked = false;
                            state = Database.myreader["State"].ToString();
                            if (Database.myreader["ispurchasereturn"].ToString() == "yes")
                                checkIsPurchaseReturn.Checked = true;
                            else
                                checkIsPurchaseReturn.Checked = false;

                            if (Database.myreader["Pla"].ToString() == "yes")
                                chkDirectExport.Checked = true;
                            else
                                chkDirectExport.Checked = false;

                            chkSpDu1.Checked = false;
                            chkSpDu2.Checked = false;
                            chkSpDu3.Checked = false;
                            chknill.Checked = false;

                            if (Convert.ToString(Database.myreader["IsGSTAllowDuty1"]) == "yes")
                                chkSpDu1.Checked = true;
                            if (Convert.ToString(Database.myreader["IsGSTAllowDuty2"]) == "yes")
                                chkSpDu2.Checked = true;
                            if (Convert.ToString(Database.myreader["IsGSTAllowDuty3"]) == "yes")
                                chkSpDu3.Checked = true;

                            if (Convert.ToString(Database.myreader["IsNIllRated"]) == "yes")
                                chknill.Checked = true;

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

                        Database.myreader = Database.GetExecuteReaderCommand("select * from DOMESTICINVOICEOEL2 with (NOLOCK)  where InvoiceNumber = " +
                                                                            textInvNo.Text + " and sufix ='" + textSuffix.Text + "' and companyname = '"
                                                                            + comboCompanyName.Text + "'");
                        if (Database.myreader.Read())
                        {
                            textOel2Basic.Text = Database.myreader["Basic"].ToString();
                            textOEL2CVD.Text = Database.myreader["cvd"].ToString();

                            texToel2Cess1.Text = Database.myreader["EduCess1"].ToString();
                            texToel2Cess2.Text = Database.myreader["EduCess2"].ToString();
                            texToel2Cess3.Text = Database.myreader["EduCess3"].ToString();
                            texToel2SHECess1.Text = Database.myreader["HiEduCess1"].ToString();
                            texToel2SHECess2.Text = Database.myreader["HiEduCess2"].ToString();
                            texToel2SHECess3.Text = Database.myreader["HiEduCess3"].ToString();
                            TextOEL2AddDuty.Text = Utility.ConverttoInt(Convert.ToString(Database.myreader["AddDuty"])); // Add field For Qry for OEL2 by KUSHAL
                        }

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
                        fillData(true);
                        Database.OpenConnection(Utility.DespacthConnectionString);
                        object j = Database.GetExecuteScalarCommand_return("select count(*) from DomesticInvItem  with (NOLOCK) where InvNo = "
                                + textInvNo.Text + " and yr = '" + textSuffix.Text + "' and companyname = '"
                                + comboCompanyName.Text + "'");
                        TotalCount = Convert.ToInt32(j);


                        if (TotalCount > 0)
                            dataGridView1.Rows.Add(TotalCount);


                        ArrayList ArrSrNo = new ArrayList();
                        ArrayList ArrItemName = new ArrayList();
                        ArrayList ArrUnit = new ArrayList();
                        ArrayList ArrQty = new ArrayList();
                        ArrayList ArrRate = new ArrayList();
                        ArrayList ArrNetWt = new ArrayList();
                        ArrayList ArrGrossWt = new ArrayList();
                        ArrayList ArrNoOfPkg = new ArrayList();
                        ArrayList ArrTotal = new ArrayList();
                        ArrayList ArrCommodity = new ArrayList();
                        ArrayList ArrNonDisplayQty = new ArrayList();
                        ArrayList ArrGroupCommodity = new ArrayList();
                        ArrayList ArrQtyRemarks = new ArrayList();
                        ArrayList ArrCommodityRemarks = new ArrayList();
                        ArrayList ArrDeptt = new ArrayList();

                        int k = 0;
                        Database.OpenConnection(Utility.DespacthConnectionString);
                        Database.myreader = Database.GetExecuteReaderCommand(
                            "select * from DomesticInvItem with (NOLOCK)  where InvNo = " + textInvNo.Text + " and yr = '" + textSuffix.Text +
                            "' and companyname = '" + comboCompanyName.Text + "'" + MerchantCondition);
                        while (Database.myreader.Read())
                        {
                            //ArrSrNo.Add(Database.myreader["SrNo"].ToString());
                            ArrCommodity.Add(Database.myreader["Commodity"].ToString());
                            ArrItemName.Add(Database.myreader["ItemCode"].ToString());
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
                            dataGridView1.Rows[k].Cells[colHSNCode.Index].Value = Convert.ToString(Database.myreader["HSNCode"]);
                            dataGridView1.Rows[k].Cells[colCGSTrate.Index].Value = Convert.ToString(Database.myreader["CGSTrate"]);
                            dataGridView1.Rows[k].Cells[colCGSTamt.Index].Value = Convert.ToString(Database.myreader["CGSTamt"]);
                            dataGridView1.Rows[k].Cells[colSGSTRate.Index].Value = Convert.ToString(Database.myreader["SGSTRate"]);
                            dataGridView1.Rows[k].Cells[ColSGSTAmt.Index].Value = Convert.ToString(Database.myreader["SGSTAmt"]);
                            dataGridView1.Rows[k].Cells[colIGSTRate.Index].Value = Convert.ToString(Database.myreader["IGSTRate"]);
                            dataGridView1.Rows[k].Cells[colIGSTAmt.Index].Value = Convert.ToString(Database.myreader["IGSTAmt"]);
                            string grp = "";
                            string subgrp = "";
                            DespatchCommonFunction.GetItemGropuName(Database.myreader["ItemCode"].ToString(), comboCompanyName.Text.ToString(), ref grp, ref subgrp);
                            dataGridView1.Rows[k].Cells[colGroupName.Index].Value = grp;
                            dataGridView1.Rows[k].Cells[colSubGrp.Index].Value = subgrp;
                            dataGridView1.Rows[k].Cells[colMainGrp.Index].Value = DespatchCommonFunction.GetDeptt(Database.myreader["ItemCode"].ToString());
                            dataGridView1.Rows[k].Cells[colItemCode.Index].Value = Convert.ToString(Database.myreader["ItemCode"]);

                            // dataGridView1.Rows[k].Cells[14].Value = Database.myreader["OrderNO"].ToString();
                            //ArrOrderNo.Add(Database.myreader["OrderNo"].ToString());
                            ArrDeptt.Add(Database.myreader["ItemDeptt"].ToString());
                            k++;
                        }
                        Database.myreader.Close();
                        for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                        {
                            dataGridView1.Rows[i].Cells[DCommodity.Index].Value = ArrCommodity[i].ToString();
                            dataGridView1.Rows[i].Cells[DtDeptt.Index].Value = ArrDeptt[i].ToString();
                            DataGridViewComboBoxColumn Col2 = (DataGridViewComboBoxColumn)dataGridView1.Columns[DtDeptt.Index];
                            if (Col2.Items.Count != 0) Col2.Items.Clear();
                            Database.myreader = Database.GetExecuteReaderCommand("select distinct(depttname) from itemdeptt inner join commodity on commodity.deptt = itemdeptt.groupname where  companyname = '"
                            + comboCompanyName.Text + "' and commodityname = '"
                            + dataGridView1.Rows[i].Cells[DCommodity.Index].Value + "' order by depttname   ");
                            while (Database.myreader.Read())
                                Col2.Items.Add(Database.myreader[0].ToString());
                            Database.myreader.Close();

                            for (int m = 0; m <= i; m++)
                            {
                                isupdate = false;
                                if (Col2.Items.Contains(dataGridView1.Rows[m].Cells[DItemDesc.Index].Value.ToString()) == false)
                                    Col2.Items.Add(dataGridView1.Rows[m].Cells[DItemDesc.Index].Value.ToString());
                            }

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
                        if (dateTimePickerInvDate.Value > Convert.ToDateTime("2017-06-30 00:00:00"))
                        {
                            CalculationGST();
                        }
                        else
                        {
                            Calculation();
                        }
                        Database.Closeconnection();
                    }
                }




            }
            catch (Exception ex)
            {
                ex.ToString();
                //ErrorMessageBox.Show(ex);
            }
        }

        private void btnInitliaze_Click(object sender, EventArgs e)
        {
            try
            {
                textNotifier.Text = "";
                textFreightRemarks.Text = "";
                TotalCount = 0;

                textBoxLRNo.Text = "";
                textBuyerOrderNo.Text = "";
                textDelChallanNo.Text = "";
                textDesPatchNo.Text = "";
                comboDestination.Text = "";
                textEduCess.Text = "0";
                texToel2Cess2.Text = "0";
                texToel2SHECess2.Text = "0";
                TextOEL2AddDuty.Text = "0";
                checkIsPurchaseReturn.Checked = false;
                if (DateTime.Now >= Convert.ToDateTime("2019-01-01"))
                    checkIsPurchaseReturn.Visible = false;
                else
                    checkIsPurchaseReturn.Visible = true;

                if (DateTime.Now >= Convert.ToDateTime("2017-07-01"))
                    textExDuty.Text = "0";
                else
                    textExDuty.Text = "12.5";
                textInvNo.Text = "";
                textTermsOfDelivery.Text = "";
                textSecHiEdu.Text = "0";
                textTermsofPayment.Text = "";
                if (DateTime.Now >= Convert.ToDateTime("2017-07-01"))
                {
                    textVat.Text = "0";
                    textAddVat.Text = "0";
                    comboExicseApp.Text = "NIL";
                    comboVatApp.Text = "NIL";
                }
                else
                {
                    textVat.Text = "4";
                    textAddVat.Text = "1";
                }
                comboVehicalNo.Text = "";
                textMobile.Text = "";
                checkInsurance.Checked = false;
                checkFreight.Checked = false;
                btnUpdate.Enabled = false;
                btnSave.Enabled = true;
                dataGridView1.Rows.Clear();
                checkIsPurchaseReturn.Checked = false;
                chkDirectExport.Checked = false;
                if (Database.OpenConnection(Utility.MaterialConnectionString))
                {
                    Database.myreader = Database.GetExecuteReaderCommand("select * from MaterialProcessing..excisemaster ORDER BY FROMDATE DESC");
                    if (Database.myreader.Read())
                    {
                        textExDuty.Text = Database.myreader[0].ToString();
                        textEduCess.Text = Database.myreader[1].ToString();
                        textSecHiEdu.Text = Database.myreader[2].ToString();
                    }
                    Database.myreader.Close();
                }
                fillData();

            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
            }
        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            string desPackinglist = "";
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

            if (checkMerchantExport.Checked == false) // added by manish 2nd Feb 2025
            {
                if (DespatchCommonFunction.IsAccountingDone(comboCompanyName.Text, textInvNo.Text, textSuffix.Text))
                {
                    MessageBox.Show("Accounting Entry is done for the selected Invoice.\nPlease Contact Account Administrator");
                    return;
                }
            } // end added

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
            bool isMultilayer = false;
            //if (comboDeptt.Text == "Multilayer")
            //    isMultilayer = true;

            if (!FrmMain.IsUpdateDomesticInvoice)
                MessageBox.Show("You have no right to update Domestic Invoice");
            else if (textBoxLRNo.Text == "")
                MessageBox.Show("Please enter Value in LRNo");
            else if (comboState.Text == "")
                MessageBox.Show("Please select State");
            else if (!(textTermsofPayment.Items.Contains(textTermsofPayment.Text.Trim())))
                MessageBox.Show("Please select proper payment terms or enter payment term in payment master");
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
                    #region PackingList

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
                            TotalNetwt = Math.Round(TotalNetwt, 2);

                            string strgstInv = Convert.ToString(Database.GetExecuteNonQueryCommand_retrun("SELECT GSTInvNo FROM Vw_DomesticInvoice WHERE InvNo=" +
                                                textInvNo.Text + " and companyname='" + comboCompanyName.Text + "' and ARESuffix='" + textSuffix.Text + "'"));
                            if (strgstInv.Length > 0)
                            {
                                desPackinglist = Convert.ToString(Database.GetExecuteNonQueryCommand_retrun("SELECT DISTINCT TOP 1 dbo.DistinctList (MappPackingList.PackingListNo ,',') PackingListNo FROM MappPackingList WHERE companyname='"
                                    + comboCompanyName.Text + "' AND InvNo='" + strgstInv + "' AND Yr='" + textSuffix.Text + "'"));
                            }
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
                                            //+ "' and  yr='" + textSuffix.Text + "'  "
                                            + " and (PACKINGLISTNO ='" + split[i] + "' or invno='" + split[i] + "') ";
                                        Database.myreader = Database.GetExecuteReaderCommand(strsql);
                                        if (Database.myreader.Read())
                                        {
                                            totalpackinglist += Utility.SafeConvertToDouble(Database.myreader[0].ToString());
                                        }
                                        Database.myreader.Close();
                                        Database.Closeconnection();
                                        totalpackinglist = Math.Round(totalpackinglist, FrmMain.ERPQtyDigit);

                                    }
                                    if (Utility.SafeConvertToDouble(Math.Round(TotalNetwt, 2)) != Utility.SafeConvertToDouble(Math.Round(totalpackinglist, 2)))
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
                        string isfreightinclude = "";
                        if (checkFreight.Checked)
                            isfreightinclude = "yes";
                        else
                            isfreightinclude = "no";
                        string ispurchasereturn = "";
                        if (checkIsPurchaseReturn.Checked)
                            ispurchasereturn = "yes";
                        else
                            ispurchasereturn = "no";
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
                                    Database.OpenConnection(Utility.DespacthConnectionString);
                                    Database.BeginTransaction();
                                    string MerchantCondition = " and MerchantExport ='";
                                    if (checkMerchantExport.Checked)
                                        MerchantCondition += "yes" + "'";
                                    else
                                        MerchantCondition = " and isnull(MerchantExport,'no') = 'no'";

                                    double TotalAmt = 0;
                                    string CheckInsurane = "";
                                    Database.myreader = Database.GetExecuteReaderCommand("select TotalAmt,CheckInsurance from domesticinvoice where Invno = "
                                        + textInvNo.Text + " and suffix = '" + textSuffix.Text
                                        + "' and companyname = '" + comboCompanyName.Text + "'" + MerchantCondition);
                                    if (Database.myreader.Read())
                                    {
                                        TotalAmt = Convert.ToDouble(Database.myreader[0].ToString());
                                        CheckInsurane = Database.myreader[1].ToString();
                                    }
                                    Database.myreader.Close();
                                    string InsuranceApp = "";
                                    string Insurance = "";
                                    if (checkInsurance.Checked)
                                    {
                                        InsuranceApp = "yes";
                                        Insurance = textInsurance.Text;
                                    }
                                    else
                                        InsuranceApp = "no";

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
                                                                               + " CheckInsurance = '" + InsuranceApp + "',"
                                                                               + " Insurance = '" + Insurance + "',"
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
                                                                               + textNotifier.Text + "',FREIGHTREMARKS = '" + textFreightRemarks.Text
                                                                               + "', isfreightinclude = '" + isfreightinclude + "'"
                                                                               + ", state = '" + comboState.Text + "',ispurchasereturn = '"
                                                                               + ispurchasereturn + "',mobileno='" + textMobile.Text + "',"
                                                                               + "isdispfrm = " + (chk_dispfrm.Checked ? "1" : "0") + ","
                                                                               + "dispfrm = '" + combodispfrm.Text + "',"
                                                                               + "dispfrm_add = '" + textdispfrm_add.Text + "'"
                                                                               + " where Invno =" + textInvNo.Text + " and suffix = '"
                                                                               + textSuffix.Text + "' and companyname = '"
                                                                               + comboCompanyName.Text + "'" + MerchantCondition);

                                    Database.GetExecuteNonQueryCommand("Update DomesticInvoice set IsGSTAllowDuty1 ='" + (chkSpDu1.Checked == true ? "yes" : "no")
                                      + "',IsGSTAllowDuty2='" + (chkSpDu2.Checked == true ? "yes" : "no")
                                      + "',IsGSTAllowDuty3='" + (chkSpDu3.Checked == true ? "yes" : "no")
                                      + "',IsNIllRated='" + (chknill.Checked == true ? "yes" : "no")
                                      + "' where Invno =" + textInvNo.Text + " and suffix = '"
                                      + textSuffix.Text + "' and companyname = '"
                                      + comboCompanyName.Text + "'" + MerchantCondition);

                                   
                                    Database.GetExecuteNonQueryCommand("Update DomesticInvoice set MerchantExport ='" + (checkMerchantExport.Checked == true ? "yes" : "no")
                                  + "',Notification='" + textNotification.Text + "',AdvanceLicense='" + comboAdvLicenseNo.Text
                                  + "' where Invno =" + textInvNo.Text + " and suffix = '"
                                  + textSuffix.Text + "' and companyname = '"
                                  + comboCompanyName.Text + "'" + MerchantCondition);


                                    if (comboExicseApp.SelectedIndex == 0) //Excise Nil
                                        Database.GetExecuteNonQueryCommand("Update DomesticInvoice set ExciseAmtWord = 'NIL' where Invno = "
                                            + textInvNo.Text + " and suffix = '" + textSuffix.Text + "' and companyname = '" + comboCompanyName.Text + "'" + MerchantCondition);
                                    if (textBuyerOrderNo.Text != "")
                                    {
                                        Database.GetExecuteNonQueryCommand("update domesticinvoice set BuyerOrderNo = '" + textBuyerOrderNo.Text + "',"
                                                                            + "BuyerDate = '" + dateTimeBuyerOrderDate.Value.ToString(DespatchCommonFunction.ERPDBDateSettings()) + "'"
                                                                            + " where Invno =" + textInvNo.Text + " and suffix = '"
                                                                            + textSuffix.Text + "' and companyname = '"
                                                                            + comboCompanyName.Text + "'" + MerchantCondition);
                                    }
                                    else
                                    {
                                        Database.GetExecuteNonQueryCommand("update domesticinvoice set BuyerOrderNo = '" + textBuyerOrderNo.Text + "',"
                                                                           + "BuyerDate = null"
                                                                           + " where Invno =" + textInvNo.Text + " and suffix = '"
                                                                           + textSuffix.Text + "' and companyname = '"
                                                                           + comboCompanyName.Text + "'" + MerchantCondition);
                                    }


                                    Database.GetExecuteNonQueryCommand("Update DomesticInvoice set spDuty1 = '" + textSpDutyRemarks1.Text
                                            + "',SpDuty1Amt = " + textSpDuty1Amt.Text
                                            + ",SpDuty2 = '" + textSpDutyRemark2.Text
                                            + "',SpDuty2Amt = " + textSpDuty2Amt.Text
                                            + ",SpDuty3 = '" + textSpDutyRemarks3.Text
                                            + "',spDuty3Amt =  " + textSpDuty3Amt.Text
                                            + ",consigneecode = '" + ConsigneeCode + "' where Invno = "
                                            + textInvNo.Text + " and suffix = '" + textSuffix.Text + "' and companyname = '"
                                            + comboCompanyName.Text + "'" + MerchantCondition);


                                    //  if (Database.OpenConnection(Utility.DespacthConnectionString))
                                    {
                                        Database.GetExecuteNonQueryCommand("delete from DomesticInvItem where InvNo = "
                                            + textInvNo.Text + " and yr = '" + textSuffix.Text + "' and companyname = '"
                                            + comboCompanyName.Text + "'" + MerchantCondition);
                                        // Database.Closeconnection();
                                    }

                                    //  if (Database.OpenConnection(Utility.DespacthConnectionString))
                                    {
                                        Database.GetExecuteNonQueryCommand("delete from DOMESTICINVOICEOEL2 where InvoiceNumber = " +
                                                                          textInvNo.Text + " and sufix ='" + textSuffix.Text + "' and companyname = '"
                                                                          + comboCompanyName.Text + "'");


                                    }
                                    if (frmDefaultVale.CompanyName == "Oswal Extrusion Limited (Unit-II)")
                                    {
                                        Database.GetExecuteNonQueryCommand("insert into DomesticInvoiceoel2 values('"
                                            + textInvNo.Text + "','"
                                      + textSuffix.Text + "','" + frmDefaultVale.CompanyName + "','"
                                       + textOel2Basic.Text + "','" + textOEL2CVD.Text + "','"
                                       + texToel2Cess1.Text + "','" + texToel2Cess2.Text
                                       + "','" + texToel2Cess3.Text + "','" + texToel2SHECess1.Text
                                       + "','" + texToel2SHECess2.Text + "','" + texToel2SHECess3.Text
                                       + "','" + oelbasic + "','" + CVD + "','" + educess1 + "','" + educess2
                                       + "','" + educess3 + "','" + shecess1 + "','" + shecess2 + "','" + shecess3 + "','" + TextOEL2AddDuty.Text + "','" + AddDuty + "')");
                                        // Add field Addduty,TextOEL2AddDuty in insert query also add both filed in databse For Qry for OEL2 with Inv no 21 and 23 by KUSHAL

                                        //MessageBox.Show("Domestic Invoice " + InvNo + " is generated succesfully");


                                    }
                                    // Database.Closeconnection();
                                    bool IsExecute = false;
                                    for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                                    {

                                        if (dataGridView1[DItemDesc.Index, i].ToString() != "" &&
                                            dataGridView1[DUnitPrice.Index, i].ToString() != "" &&
                                            dataGridView1[DtDeptt.Index, i].ToString() != ""
                                            && dataGridView1[DTotal.Index, i].ToString() != ""
                                            && dataGridView1[DNetWt.Index, i].ToString() != "")
                                        {
                                            //if (Database.OpenConnection(Utility.DespacthConnectionString))
                                            {
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
                                                    NonDisplayQty = Convert.ToDouble(dataGridView1.Rows[i].Cells[DOrderQty.Index].Value.ToString());
                                                else if (dataGridView1.Rows[i].Cells[DNonDisplayQty.Index].FormattedValue.ToString() == "")
                                                    NonDisplayQty = 0;
                                                else
                                                    NonDisplayQty = Convert.ToDouble(dataGridView1.Rows[i].Cells[DNonDisplayQty.Index].Value.ToString());

                                                if (dataGridView1.Rows[i].Cells[DQtyRemarks.Index].FormattedValue.ToString() == "")
                                                    dataGridView1.Rows[i].Cells[DQtyRemarks.Index].Value = ".";

                                                if (dataGridView1.Rows[i].Cells[DCommodityRemarks.Index].FormattedValue.ToString() == "")
                                                    dataGridView1.Rows[i].Cells[DCommodityRemarks.Index].Value = ".";


                                                if (Database.GetExecuteNonQueryCommand("insert into DomesticInvItem(InvNo,NoofPkg,ItemDesc,Qty,Rate,Unit,Amount,NetWt,GrossWt,SalesOrderNo,LoginName,Commodity,SrNo,GroupCommodity,NonDisplayUnit,NonDisplayQty,QtyRemarks,Yr,Companyname,CommodityRemarks,Desc1,Desc2,BagsMRP,size,weekno,LotNo,Colour,Meter,GPM,GSM,WBWeight,ReqWtperBag,ReqWtTotal,DispatchWtperBagGr,WtTolperBagGr,WtExtrapershortKg,WtDiffbetPLWB,AssesableValue,OrderNO,itemDeptt ,HSNCode,CGSTrate,CGSTamt,SGSTRate,SGSTAmt,IGSTRate,IGSTAmt,Itemcode,MerchantExport) values('"
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
                                                            + "null,null,null,null,null,null,null,'','" + dataGridView1.Rows[i].Cells[DtDeptt.Index].Value.ToString() + "','"
                                                            + Convert.ToString(dataGridView1.Rows[i].Cells[colHSNCode.Index].Value) + "',"
                                                            + Convert.ToDouble(dataGridView1.Rows[i].Cells[colCGSTrate.Index].Value) + ","
                                                            + Convert.ToDouble(dataGridView1.Rows[i].Cells[colCGSTamt.Index].Value) + ","
                                                            + Convert.ToDouble(dataGridView1.Rows[i].Cells[colSGSTRate.Index].Value) + ","
                                                            + Convert.ToDouble(dataGridView1.Rows[i].Cells[ColSGSTAmt.Index].Value) + ","
                                                            + Convert.ToDouble(dataGridView1.Rows[i].Cells[colIGSTRate.Index].Value) + ","
                                                            + Convert.ToDouble(dataGridView1.Rows[i].Cells[colIGSTAmt.Index].Value) + ",'"
                                                            + Convert.ToString(dataGridView1.Rows[i].Cells[colItemCode.Index].Value) + "','"
                                                            + (checkMerchantExport.Checked == true ? "yes" : "no") 
                                                            + "')"))
                                                    IsExecute = true;

                                            }
                                        }

                                    }//end of forlop grid

                                    Database.CommitTransaction();
                                    Database.Closeconnection();
                                    if (IsExecute)
                                        MessageBox.Show("Domestic Invoice No " + textInvNo.Text + " is succesfully updated");

                                    Utility.UserInformation("Update", "Domestic Invoice " + textInvNo.Text + " is updated successfully");

                                    #region PackingList
                                    if (DespatchCommonFunction.AllowMappingPackinglist(comboCompanyName.Text) > 0)
                                    {
                                        if (Database.OpenConnection(Utility.DespacthConnectionString))
                                        {
                                            string strgstInv = Convert.ToString(Database.GetExecuteNonQueryCommand_retrun("SELECT GSTInvNo FROM Vw_DomesticInvoice WHERE InvNo=" +
                                            textInvNo.Text + " and companyname='" + comboCompanyName.Text + "' and ARESuffix='" + textSuffix.Text + "'"));

                                            Database.GetExecuteNonQueryCommand("DELETE FROM MappPackingList WHERE CompanyName='" + comboCompanyName.Text + "' AND InvNo='" +
                                                strgstInv + "' AND YR='" + textSuffix.Text + "'");
                                            Database.GetExecuteNonQueryCommand("INSERT INTO MappPackingList (CompanyName,InvNo,Yr,PackingListNo) VALUES('" +
                                            comboCompanyName.Text + "','" + strgstInv + "','" + textSuffix.Text + "','" + desPackinglist + "')");
                                        }
                                    }
                                    #endregion

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

        private void comboType_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (dateTimePickerInvDate.Value > Convert.ToDateTime("2017-07-01"))
                return;

            if (comboType.SelectedIndex == 0) //VAT
            {
                labelAddVat.Visible = true;
                textAddVat.Visible = true;
                if (DateTime.Now >= Convert.ToDateTime("2017-07-01"))
                    return;

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
                if (DateTime.Now >= Convert.ToDateTime("2017-07-01"))
                    return;
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
                textConsignee.Text = "";
                comboConsigneeName.Items.Clear();
                using (System.Data.SqlClient.SqlConnection con = new System.Data.SqlClient.SqlConnection(Utility.DespacthConnectionString))
                {
                    con.Open();
                    string state = "";
                    string strsql = "select consignee,terms,Destination,typeofInvoice,Companycode,state from companymaster where "
                                  + " companyname = '" + comboBuyerName.Text.Replace("'", "''") + "'";
                    System.Data.SqlClient.SqlCommand command = new System.Data.SqlClient.SqlCommand(strsql, con);
                    System.Data.SqlClient.SqlDataReader reader = command.ExecuteReader();

                    while (reader.Read())
                    {
                        textConsignee.Text = Convert.ToString(reader[0]);
                        textTermsofPayment.Text = Convert.ToString(reader[1]);
                        comboDestination.Text = Convert.ToString(reader[2]);
                        CompanyCode = Convert.ToString(reader[4]);
                        state = Convert.ToString(reader["state"]);
                    }

                    comboConsigneeName.Items.Clear();
                    comboConsigneeName.Text = "";
                    strsql = "select Distinct(Consigneename) as Consigneename from consignee where  companyCode = '" + CompanyCode + "'";
                    DespatchCommonFunction.Loadcombo(comboConsigneeName, strsql, "Consigneename", "Consigneename", Utility.DespacthConnectionString);
                    comboConsigneeName.Text = "";
                    comboConsigneeName.SelectedIndex = -1;
                    Database.Closeconnection();
                    TypeInvoice();
                    if (comboConsigneeName.Items.Count > 0)
                        comboConsigneeName.SelectedIndex = 0;
                    comboState.Text = state;
                }

                ComboInvoiceNo.DataSource = null;
                //ComboInvoiceNo.DataSource = DespatchCommonFunction.GetPackingListInvoiceNo(comboCompanyName.Text, comboBuyerName.Text);
                //ComboInvoiceNo.DisplayMember = "Invno";
                //ComboInvoiceNo.ValueMember = "Invno";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Problem with loading Packing List :" + ex.Message);
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
                     && dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[colHSNCode.Index].FormattedValue.ToString().Trim() != "" &&
                     dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[DOrderQty.Index].FormattedValue.ToString().Trim() != "")
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
                    Database.Closeconnection();
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
                DespatchCommonFunction.GetBuyerName(comboBuyerName, Utility.DespacthConnectionString, dateTimePickerInvDate.Value);
                DespatchCommonFunction.GetTransportMaster(comboTranporterName);
                Database.Closeconnection();
            }

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

        private void comboConsigneeCity_SelectedIndexChanged(object sender, EventArgs e)
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


        private void dataGridView1_CurrentCellChanged(object sender, EventArgs e)
        {
            try
            {
                if (dataGridView1.CurrentCell.ColumnIndex == DOrderQty.Index ||
                    dataGridView1.CurrentCell.ColumnIndex == DUnitPrice.Index ||
                    dataGridView1.CurrentCell.ColumnIndex == DTotal.Index ||
                    dataGridView1.CurrentCell.ColumnIndex == colCGSTrate.Index ||
                    dataGridView1.CurrentCell.ColumnIndex == colSGSTRate.Index ||
                    dataGridView1.CurrentCell.ColumnIndex == colIGSTRate.Index
                    )
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
            }
            catch (Exception ex)
            {

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
                    if (this.dataGridView1.CurrentCell.ColumnIndex == DOrderQty.Index //6
                        || this.dataGridView1.CurrentCell.ColumnIndex == DNetWt.Index //9
                        || this.dataGridView1.CurrentCell.ColumnIndex == DUnitPrice.Index //7
                        || this.dataGridView1.CurrentCell.ColumnIndex == DTotal.Index) //8
                        tb.KeyPress += new KeyPressEventHandler(tb_KeyPress);
                }

                if (dataGridView1.CurrentCell.ColumnIndex == DCommodity.Index) // For Commodity ComboBox
                {
                    //comboBox = e.Control as ComboBox;
                    //comboBox.SelectedIndexChanged += new EventHandler(comboBox_SelectedIndexChanged);
                    //dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[colHSNCode.Index].Value = textHeading.Text;
                }
                if (dataGridView1.CurrentCell.ColumnIndex == DItemDesc.Index) // For Commodity ComboBox
                {
                    if (e.Control is DataGridViewTextBoxEditingControl)
                    {
                        var tec = (e.Control as DataGridViewTextBoxEditingControl);
                        tec.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
                        tec.AutoCompleteSource = AutoCompleteSource.CustomSource;
                        tec.AutoCompleteCustomSource = DataItems;
                    }
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
            { }
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
                textTCS.Text = "0";
                ComboBox cb = ((ComboBox)sender);

                Database.OpenConnection(Utility.DespacthConnectionString);
                Database.myreader = Database.GetExecuteReaderCommand("select * from CommodityHSNGST where CommodityName ='"
                        + cb.Text + "' and companyname = '" + comboCompanyName.Text + "' and (('" + dateTimePickerInvDate.Value.ToString("yyyy-MM-dd")
                        + "' between nFromDate and nToDate) or (nFromDate <= '"
                        + dateTimePickerInvDate.Value.ToString("yyyy-MM-dd") + "' and nToDate is null )) order by CommodityName");
                while (Database.myreader.Read())
                {
                    textHeading.Text = Database.myreader["THeading"].ToString();
                    if (Database.myreader["THeading"].ToString().Length > 4)
                    {
                        if (dateTimePickerInvDate.Value.Date >= Convert.ToDateTime("2021-04-01"))
                            dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[colHSNCode.Index].Value = Database.myreader["THeading"].ToString().Substring(0, 6);
                        else
                            dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[colHSNCode.Index].Value = Database.myreader["THeading"].ToString().Substring(0, 4);
                    }
                    else
                        dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[colHSNCode.Index].Value = Database.myreader["THeading"].ToString();
                    if (comboState.Text.ToUpper() == "Gujarat".ToUpper())
                    {
                        dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[colCGSTrate.Index].Value = Database.myreader["NCGSTRate"].ToString();
                        dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[colSGSTRate.Index].Value = Database.myreader["NSGSTRate"].ToString();
                        dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[colIGSTRate.Index].Value = 0;
                    }
                    else
                    {
                        dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[colCGSTrate.Index].Value = 0;
                        dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[colSGSTRate.Index].Value = 0;
                        dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[colIGSTRate.Index].Value = Database.myreader["NIGSTRate"].ToString();
                    }
                    if (Database.myreader["Avail39a"].ToString() == "yes")
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
                //Database.Closeconnection();
                #endregion
            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
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

        private void dataGridView1_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            try
            {
                #region
                if (dataGridView1.IsCurrentCellDirty)
                {
                    dataGridView1.CommitEdit(DataGridViewDataErrorContexts.Commit);
                    return;
                }
                //if (!isupdate)
                {
                    if (dataGridView1.IsCurrentCellDirty)
                    {

                        dataGridView1.CommitEdit(DataGridViewDataErrorContexts.Commit);
                        if (dataGridView1.CurrentRow.Cells.IndexOf(dataGridView1.CurrentCell) == DCommodity.Index)
                        {
                            DataGridViewComboBoxColumn Col2 = (DataGridViewComboBoxColumn)dataGridView1.Columns[DtDeptt.Index];

                            if (Col2.Items.Count != 0) Col2.Items.Clear();

                            if (Database.OpenConnection(Utility.DespacthConnectionString))
                            {
                                string commoditylist = "";
                                for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                                {
                                    commoditylist += "'" + dataGridView1.Rows[i].Cells[DCommodity.Index].Value + "',";
                                }
                                if (commoditylist.Length > 0)
                                    commoditylist = commoditylist.Remove(commoditylist.Length - 1, 1);

                                Database.myreader = Database.GetExecuteReaderCommand("select distinct(depttname) from itemdeptt inner join commodity on commodity.deptt = itemdeptt.groupname where  companyname = '"
                                + comboCompanyName.Text + "' and commodityname in ("
                                + commoditylist + ") order by depttname   ");
                                while (Database.myreader.Read())
                                    Col2.Items.Add(Database.myreader[0].ToString());
                                Database.myreader.Close();
                                //for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                                //{
                                //    string strva = Convert.ToString(dataGridView1.Rows[i].Cells[DItemDesc.Index].Value);
                                //    if (Col2.Items.Contains(strva) == false)
                                //        Col2.Items.Add(strva);
                                //}

                                string Commodity = Convert.ToString(dataGridView1.Rows[dataGridView1.CurrentRow.Index].Cells[DCommodity.Index].Value);
                                string THeading = "";
                                Database.OpenConnection(Utility.DespacthConnectionString);
                                Database.myreader = Database.GetExecuteReaderCommand("Select top 1 * from CommodityHSNGST where companyname = '"
                                    + comboCompanyName.Text + "' and CommodityName='" + Commodity + "' and (('" + dateTimePickerInvDate.Value.ToString("yyyy-MM-dd")
                                    + "' between nFromDate and nToDate) or (nFromDate <= '"
                                    + dateTimePickerInvDate.Value.ToString("yyyy-MM-dd") + "' and nToDate is null ))");
                                if (Database.myreader.Read())
                                {
                                    THeading = Database.myreader["THeading"].ToString();
                                    if (THeading.Length > 3)
                                    {
                                        if (dateTimePickerInvDate.Value.Date >= Convert.ToDateTime("2021-04-01"))
                                            dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[colHSNCode.Index].Value = THeading;
                                        else
                                            dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[colHSNCode.Index].Value = THeading.Substring(0, 4);
                                    }
                                    if (comboState.Text.ToUpper() == "Gujarat".ToUpper())
                                    {
                                        dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[colCGSTrate.Index].Value = Database.myreader["NCGSTRate"].ToString();
                                        dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[colSGSTRate.Index].Value = Database.myreader["NSGSTRate"].ToString();
                                        dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[colIGSTRate.Index].Value = 0;
                                    }
                                    else
                                    {

                                        dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[colCGSTrate.Index].Value = 0;
                                        dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[colSGSTRate.Index].Value = 0;
                                        dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[colIGSTRate.Index].Value = Database.myreader["NIGSTRate"].ToString();
                                    }
                                    if (Database.myreader["Avail39a"].ToString() == "yes")
                                        checkAvailBenefit.Checked = true;
                                    else
                                        checkAvailBenefit.Checked = false;
                                }
                                Database.myreader.Close();
                                #region check TCS Calculation 28.09.2020
                                textTCS.Text = DespatchCommonFunction.LoadTCSLedger(comboCompanyName.Text, comboBuyerName.Text, Commodity, Convert.ToDateTime(dateTimePickerDespatchDate.Value)).ToString();

                                #endregion
                                Database.Closeconnection();
                                DataItems = new AutoCompleteStringCollection();
                                DataItems = DespatchCommonFunction.GetItemList(comboCompanyName.Text, Commodity);
                            }

                        }
                        else if (dataGridView1.CurrentRow.Cells.IndexOf(dataGridView1.CurrentCell) == colMainGrp.Index)
                        {
                            string deptt = Convert.ToString(dataGridView1.Rows[dataGridView1.CurrentRow.Index].Cells[colMainGrp.Index].Value);
                            DataTable dt = new DataTable();
                            dt = dtIMaster.Copy();

                            DataGridViewComboBoxCell ColGRP = (DataGridViewComboBoxCell)dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[colGroupName.Index];
                            dtGroupName.DefaultView.RowFilter = "Depttname='" + deptt + "'";
                            if (dtGroupName.DefaultView.ToTable(true, "GROUPNAME").Rows.Count > 0)
                            {
                                ColGRP.DataSource = dtGroupName.DefaultView.ToTable(true, "GROUPNAME");
                                ColGRP.ValueMember = dtGroupName.Columns["GROUPNAME"].ColumnName.ToString();
                                ColGRP.DisplayMember = dtGroupName.Columns["GROUPNAME"].ColumnName.ToString();
                            }
                            DataGridViewComboBoxCell Col11 = (DataGridViewComboBoxCell)dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[colItemCode.Index];
                            dt.DefaultView.RowFilter = "Deptt='" + deptt + "'";
                            if (dt.DefaultView.ToTable(true, "ItemCode").Rows.Count > 0)
                            {
                                Col11.DataSource = dt.DefaultView.ToTable();
                                Col11.ValueMember = dt.Columns[0].ColumnName.ToString();
                                Col11.DisplayMember = dt.Columns[1].ColumnName.ToString();
                            }
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
                                DataGridViewComboBoxCell colComm = (DataGridViewComboBoxCell)dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[DCommodity.Index];
                                colComm.DataSource = dt;
                                colComm.ValueMember = dt.Columns[0].ColumnName.ToString();
                                colComm.DisplayMember = dt.Columns[0].ColumnName.ToString();

                                if (Convert.ToString(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[DItemDesc.Index].Value).Length == 0)
                                    dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[DItemDesc.Index].Value = Convert.ToString(dataGridView1.Rows[dataGridView1.CurrentRow.Index].Cells[colItemCode.Index].FormattedValue);
                            }
                        }
                        else if (dataGridView1.CurrentRow.Cells.IndexOf(dataGridView1.CurrentCell) == DOrderQty.Index)
                        {
                            double OrderQty = Utility.SafeConvertToDouble(dataGridView1.Rows[dataGridView1.CurrentRow.Index].Cells[DOrderQty.Index].Value);
                            if (Convert.ToString(dataGridView1.Rows[dataGridView1.CurrentRow.Index].Cells[DUOM.Index].Value).ToUpper() == "KGS".ToUpper())
                            {
                                dataGridView1.Rows[dataGridView1.CurrentRow.Index].Cells[DNetWt.Index].Value = OrderQty;
                                dataGridView1.Rows[dataGridView1.CurrentRow.Index].Cells[DGrossWt.Index].Value = OrderQty;
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
                    string NewGSTNo = "";
                    using (System.Data.SqlClient.SqlConnection con = new System.Data.SqlClient.SqlConnection(Utility.DespacthConnectionString))
                    {
                        con.Open();
                        string strsql = "select Vatno,GSTNo,isaftinno,NewGSTNo from companymaster where companyname = '" + comboBuyerName.Text + "'";
                        System.Data.SqlClient.SqlCommand command = new System.Data.SqlClient.SqlCommand(strsql, con);
                        System.Data.SqlClient.SqlDataReader reader = command.ExecuteReader();
                        if (reader.Read())
                        {
                            tinno = reader[0].ToString();
                            gstno = reader[1].ToString();
                            NewGSTNo = Convert.ToString(reader["NewGSTNo"]);
                            if (reader["isaftinno"].ToString() == "yes")
                                ISAFTINNO = true;
                        }
                        if (dateTimePickerInvDate.Value < Convert.ToDateTime("2017-07-01"))
                        {
                            if (ISAFTINNO || tinno.Length >= 11 || gstno.Length >= 11 || NewGSTNo.Length > 12)
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



        private void chkDirectExport_CheckedChanged(object sender, EventArgs e)
        {
            panelDiirectExport.Visible = false;
            comboUT1Bond.Items.Clear();
            if (!chkDirectExport.Checked)
                return;

            panelDiirectExport.Visible = true;
            Database.myreader = Database.GetExecuteReaderCommand("Select BondNo,BondDate,ExpiryDate from Despatch..UTBond where companyname = '" +
                      comboCompanyName.Text + "'order by BondDate");
            while (Database.myreader.Read())
            {
                comboUT1Bond.Items.Add(Database.myreader["BondNo"].ToString());
                dtUT1Bond = Convert.ToDateTime(Database.myreader["BondDate"].ToString());
                dtUT1Expiry = Convert.ToDateTime(Database.myreader["ExpiryDate"].ToString());
            }
            Database.myreader.Close();
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
                        MessageBox.Show("E-Way Bill No Update for Inv :" + strgstInv);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Problem with Updating E-Way Bill No");
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
            catch (Exception ex)
            {
                ex.Message.ToString();
            }
        }

        private void dataGridView1_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            #region


            if (dataGridView1.IsCurrentCellDirty)
            {

                dataGridView1.CommitEdit(DataGridViewDataErrorContexts.Commit);
                if (dataGridView1.CurrentRow.Cells.IndexOf(dataGridView1.CurrentCell) == DCommodity.Index)
                {
                    DataGridViewComboBoxColumn Col2 = (DataGridViewComboBoxColumn)dataGridView1.Columns[DtDeptt.Index];

                    if (Col2.Items.Count != 0) Col2.Items.Clear();

                    if (Database.OpenConnection(Utility.DespacthConnectionString))
                    {
                        string commoditylist = "";
                        for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                        {
                            commoditylist += "'" + dataGridView1.Rows[i].Cells[DCommodity.Index].Value + "',";
                        }
                        if (commoditylist.Length > 0)
                            commoditylist = commoditylist.Remove(commoditylist.Length - 1, 1);

                        Database.myreader = Database.GetExecuteReaderCommand("select distinct(depttname) from itemdeptt inner join commodity on commodity.deptt = itemdeptt.groupname where  companyname = '"
                        + comboCompanyName.Text + "' and commodityname in ("
                        + commoditylist + ") order by depttname   ");
                        while (Database.myreader.Read())
                            Col2.Items.Add(Database.myreader[0].ToString());
                        Database.myreader.Close();
                        //for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                        //{
                        //    string strva = Convert.ToString(dataGridView1.Rows[i].Cells[DItemDesc.Index].Value);
                        //    if (Col2.Items.Contains(strva) == false)
                        //        Col2.Items.Add(strva);
                        //}

                        string Commodity = Convert.ToString(dataGridView1.Rows[dataGridView1.CurrentRow.Index].Cells[DCommodity.Index].Value);
                        string THeading = "";
                        Database.OpenConnection(Utility.DespacthConnectionString);
                        Database.myreader = Database.GetExecuteReaderCommand("Select top 1 * from CommodityHSNGST where companyname = '"
                            + comboCompanyName.Text + "' and CommodityName='" + Commodity + "' and (('" + dateTimePickerInvDate.Value.ToString("yyyy-MM-dd")
                            + "' between nFromDate and nToDate) or (nFromDate <= '"
                            + dateTimePickerInvDate.Value.ToString("yyyy-MM-dd") + "' and nToDate is null ))");
                        if (Database.myreader.Read())
                        {
                            THeading = Database.myreader["THeading"].ToString();
                            if (THeading.Length > 3)
                            {
                                if (dateTimePickerInvDate.Value.Date >= Convert.ToDateTime("2021-04-01"))
                                    dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[colHSNCode.Index].Value = THeading;
                                else
                                    dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[colHSNCode.Index].Value = THeading.Substring(0, 4);
                            }
                            if (comboState.Text.ToUpper() == "Gujarat".ToUpper())
                            {
                                dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[colCGSTrate.Index].Value = Database.myreader["NCGSTRate"].ToString();
                                dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[colSGSTRate.Index].Value = Database.myreader["NSGSTRate"].ToString();
                                dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[colIGSTRate.Index].Value = 0;
                            }
                            else
                            {

                                dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[colCGSTrate.Index].Value = 0;
                                dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[colSGSTRate.Index].Value = 0;
                                dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[colIGSTRate.Index].Value = Database.myreader["NIGSTRate"].ToString();
                            }
                            if (Database.myreader["Avail39a"].ToString() == "yes")
                                checkAvailBenefit.Checked = true;
                            else
                                checkAvailBenefit.Checked = false;
                        }
                        Database.myreader.Close();
                        #region check TCS Calculation 28.09.2020
                        textTCS.Text = DespatchCommonFunction.LoadTCSLedger(comboCompanyName.Text, comboBuyerName.Text, Commodity, Convert.ToDateTime(dateTimePickerDespatchDate.Value)).ToString();

                        #endregion
                        Database.Closeconnection();
                        DataItems = new AutoCompleteStringCollection();
                        DataItems = DespatchCommonFunction.GetItemList(comboCompanyName.Text, Commodity);
                    }

                }
                else if (dataGridView1.CurrentRow.Cells.IndexOf(dataGridView1.CurrentCell) == colMainGrp.Index)
                {
                    string deptt = Convert.ToString(dataGridView1.Rows[dataGridView1.CurrentRow.Index].Cells[colMainGrp.Index].Value);
                    DataTable dt = new DataTable();
                    dt = dtIMaster.Copy();

                    DataGridViewComboBoxCell ColGRP = (DataGridViewComboBoxCell)dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[colGroupName.Index];
                    dtGroupName.DefaultView.RowFilter = "Depttname='" + deptt + "'";
                    if (dtGroupName.DefaultView.ToTable(true, "GROUPNAME").Rows.Count > 0)
                    {
                        ColGRP.DataSource = dtGroupName.DefaultView.ToTable(true, "GROUPNAME");
                        ColGRP.ValueMember = dtGroupName.Columns["GROUPNAME"].ColumnName.ToString();
                        ColGRP.DisplayMember = dtGroupName.Columns["GROUPNAME"].ColumnName.ToString();
                    }
                    DataGridViewComboBoxCell Col11 = (DataGridViewComboBoxCell)dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[colItemCode.Index];
                    dt.DefaultView.RowFilter = "Deptt='" + deptt + "'";
                    if (dt.DefaultView.ToTable(true, "ItemCode").Rows.Count > 0)
                    {
                        Col11.DataSource = dt.DefaultView.ToTable();
                        Col11.ValueMember = dt.Columns[0].ColumnName.ToString();
                        Col11.DisplayMember = dt.Columns[1].ColumnName.ToString();
                    }
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
                        DataGridViewComboBoxCell colComm = (DataGridViewComboBoxCell)dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[DCommodity.Index];
                        colComm.DataSource = dt;
                        colComm.ValueMember = dt.Columns[0].ColumnName.ToString();
                        colComm.DisplayMember = dt.Columns[0].ColumnName.ToString();

                        if (Convert.ToString(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[DItemDesc.Index].Value).Length == 0)
                            dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[DItemDesc.Index].Value = Convert.ToString(dataGridView1.Rows[dataGridView1.CurrentRow.Index].Cells[colItemCode.Index].FormattedValue);
                    }
                }
                else if (dataGridView1.CurrentRow.Cells.IndexOf(dataGridView1.CurrentCell) == DOrderQty.Index)
                {
                    double OrderQty = Utility.SafeConvertToDouble(dataGridView1.Rows[dataGridView1.CurrentRow.Index].Cells[DOrderQty.Index].Value);
                    if (Convert.ToString(dataGridView1.Rows[dataGridView1.CurrentRow.Index].Cells[DUOM.Index].Value).ToUpper() == "KGS".ToUpper())
                    {
                        dataGridView1.Rows[dataGridView1.CurrentRow.Index].Cells[DNetWt.Index].Value = OrderQty;
                        dataGridView1.Rows[dataGridView1.CurrentRow.Index].Cells[DGrossWt.Index].Value = OrderQty;
                    }
                }
            }

            #endregion
        }

        private void checkMerchantExport_CheckedChanged(object sender, EventArgs e)
        {

            try
            {
                if (checkMerchantExport.Checked)
                {

                    if (dataGridView1.Rows.Count > 1)
                    {
                        double GSTPer = 0;
                        string commoditylist = "";
                        for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                        {
                            commoditylist += "'" + dataGridView1.Rows[i].Cells[DCommodity.Index].Value + "',";
                            GSTPer = Convert.ToDouble(dataGridView1.Rows[i].Cells[colCGSTrate.Index].Value)
                                   + Convert.ToDouble(dataGridView1.Rows[i].Cells[colSGSTRate.Index].Value)
                                   + Convert.ToDouble(dataGridView1.Rows[i].Cells[colIGSTRate.Index].Value);
                        }
                        if (commoditylist.Length > 0)
                            commoditylist = commoditylist.Remove(commoditylist.Length - 1, 1);


                        string state = "";
                        if (Database.OpenConnection(Utility.MaterialConnectionString))
                        {
                            Database.myreader = Database.GetExecuteReaderCommand("select state from ledgermaster where ledgername = '"
                                + comboBuyerName.Text + "'");
                            if (Database.myreader.Read())
                                state = Database.myreader[0].ToString();
                            Database.myreader.Close();
                            Database.Closeconnection();
                        }
                        //if (GSTPer == 0.1 && comboDestination.Text.ToLower() == "gujarat")
                        //    textNotification.Text = "Supply Under Notification Number 40/2017 to Merchant Exporter";
                        //else if (GSTPer == 0.1 && comboDestination.Text.ToLower() != "gujarat")
                        //    textNotification.Text = "Supply Under Notification Number 41/2017 to Merchant Exporter";
                        //else
                        //    textNotification.Text = "Supply to Merchant Exporter";

                        if (GSTPer == 0.1 && state.ToLower() == "gujarat")
                            textNotification.Text = "Supply Under Notification Number 40/2017 to Merchant Exporter";
                        else if (GSTPer == 0.1 && state.ToLower() != "gujarat")
                            textNotification.Text = "Supply Under Notification Number 41/2017 to Merchant Exporter";
                        else
                            textNotification.Text = "Supply to Merchant Exporter";

                        if (Database.OpenConnection(Utility.DespacthConnectionString))
                        {
                            comboAdvLicenseNo.Items.Clear();
                            comboAdvLicenseNo.Text = "";
                            string strsql = "";
                            strsql = "select advlicenseno from advlicense where companyname = '" + comboCompanyName.Text + "' and commodity = " + commoditylist
                                   + " AND setdefault = 'yes' and isnull(isclose,0)=0 ";
                            Database.myreader = Database.GetExecuteReaderCommand(strsql);
                            if (Database.myreader.Read())
                                comboAdvLicenseNo.Items.Add(Database.myreader[0].ToString());
                            Database.myreader.Close();
                            Database.Closeconnection();
                        }
                    }
                }
                else
                {
                    textNotification.Text = "";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
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
    }
}