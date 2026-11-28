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
    public partial class frmExportInvoice_OEL2 : Form
    {
        private DataSet ds = new DataSet("myDs");
        private DataTable dt = new DataTable("myDT");
        bool IsInit = false;

        string CompanyCode = "";
        public int TotalCount = 0;
        public double UpdateGrossWt = 0;
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

        public frmExportInvoice_OEL2()
        {
            InitializeComponent();
            Form_Load();

        }

        private void Form_Load()
        {
            groupAvailBenefit.Visible = false;
            //DialogResult drs=  MessageBox.Show("Do you want to Avail the Benefit 39-A", "Avail Benefit 39-A"
            //    , MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            //if (drs.CompareTo(DialogResult.Yes) == 0)
            //   checkAvailBenefit.Checked = true;

            comboType.SelectedIndex = 0;

            dateTimePickerDateTimeRemoval.Format = DateTimePickerFormat.Custom;
            dateTimePickerDateTimeRemoval.CustomFormat = DespatchCommonFunction.ERPDateSettings();

            dateTimePickerInvoice.Format = DateTimePickerFormat.Custom;
            dateTimePickerInvoice.CustomFormat = DespatchCommonFunction.ERPDateSettings();

            dateTimePickerDespatchDate.Format = DateTimePickerFormat.Custom;
            dateTimePickerDespatchDate.CustomFormat = DespatchCommonFunction.ERPDateSettings();

            dateTimePickerChallanDate.Format = DateTimePickerFormat.Custom;
            dateTimePickerChallanDate.CustomFormat = DespatchCommonFunction.ERPDateSettings();

            dateTimePickerVehical.Format = DateTimePickerFormat.Custom;
            dateTimePickerVehical.CustomFormat = DespatchCommonFunction.ERPDateSettings();

            dateTimePickerLRNo.Format = DateTimePickerFormat.Custom;
            dateTimePickerLRNo.CustomFormat = DespatchCommonFunction.ERPDateSettings();

            dateTimePickerInvDate.Format = DateTimePickerFormat.Custom;
            dateTimePickerInvDate.CustomFormat = DespatchCommonFunction.ERPDateSettings();

            dateTimePickerEPCGDate.Format = DateTimePickerFormat.Custom;
            dateTimePickerEPCGDate.CustomFormat = DespatchCommonFunction.ERPDateSettings();

            dateTimePickerAdvLicenseDate.Format = DateTimePickerFormat.Custom;
            dateTimePickerAdvLicenseDate.CustomFormat = DespatchCommonFunction.ERPDateSettings();

            dateTimePickerUT1Bond.Format = DateTimePickerFormat.Custom;
            dateTimePickerUT1Bond.CustomFormat = DespatchCommonFunction.ERPDateSettings();

            dateTimeUTBondExpiryDate.Format = DateTimePickerFormat.Custom;
            dateTimeUTBondExpiryDate.CustomFormat = DespatchCommonFunction.ERPDateSettings();

            dateTimePickerExciseDate.Format = DateTimePickerFormat.Custom;
            dateTimePickerExciseDate.CustomFormat = DespatchCommonFunction.ERPDateSettings();

            dateTimeBuyerOrderNo.Format = DateTimePickerFormat.Custom;
            dateTimeBuyerOrderNo.CustomFormat = DespatchCommonFunction.ERPDateSettings();

            dateTimeBillofExportDate.Format = DateTimePickerFormat.Custom;
            dateTimeBillofExportDate.CustomFormat = DespatchCommonFunction.ERPDateSettings();

            btnUpdate.Enabled = false;
            groupUTBond.Visible = false;
            comboBeingExport.SelectedIndex = 0;
            comboTypeInvoice.SelectedIndex = 0;
            comboTaxApp.SelectedIndex = 0;

            comboModeTransport.SelectedIndex = 3;

            if (Database.OpenConnection(Utility.DespacthConnectionString))
            {
                DespatchCommonFunction.GetCompanyList(comboCompanyName);
                comboCompanyName.Text = frmDefaultVale.CompanyName;

                DespatchCommonFunction.GetDepttName(comboDeptt);
                DespatchCommonFunction.GetTransportMaster(comboTransporter);
                if (comboTransporter.Items.Count > 0)
                    comboTransporter.SelectedIndex = 0;
                Database.myreader.Close();
                string strsql = "";
                if (comboCompanyName.Text != "Oswal Extrusion Limited (Unit-II)")
                {
                    //strsql = "Select CompanyName from CompanyMaster where (invoicecategory = 'Export'  or invoicecategory = '*') order by CompanyName";
                    strsql = "Select companyname from CompanyMaster where (invoicecategory = 'Export'  or invoicecategory = '*') " +
                          " and isnull(IsFreeze,'no')='no' and (('" + dateTimePickerInvDate.Value.ToString("yyyy-MM-dd") +
                          "' between fromdate and todate) or (fromdate <= '" + dateTimePickerInvDate.Value.ToString("yyyy-MM-dd") +
                          "' and todate is null ))  order by CompanyName";
                }
                else
                {
                    //strsql = "Select CompanyName from CompanyMaster order by CompanyName";
                    strsql = "Select companyname from CompanyMaster where " +
                          " isnull(IsFreeze,'no')='no' and (('" + dateTimePickerInvDate.Value.ToString("yyyy-MM-dd") +
                          "' between fromdate and todate) or (fromdate <= '" + dateTimePickerInvDate.Value.ToString("yyyy-MM-dd") +
                          "' and todate is null ))  order by CompanyName";
                }
                DespatchCommonFunction.Loadcombo(comboBuyerName, strsql, "CompanyName", "CompanyName", Utility.DespacthConnectionString);

                strsql = "";
                strsql = "Select Distinct(vehicalNo) as vehicalNo from DomesticInvoice order by vehicalNo";
                DespatchCommonFunction.Loadcombo(comboVehicalNo, strsql, "vehicalNo", "vehicalNo", Utility.DespacthConnectionString);


                strsql = "";
                strsql = "select distinct paymenttermname from MaterialProcessing..PaymentTermsMaster where Type='Sales' and isdelflag=0 order by paymenttermname";
                DespatchCommonFunction.Loadcombo(textTermsofPayment, strsql, "paymenttermname", "paymenttermname", Utility.DespacthConnectionString);

                strsql = "";
                strsql = "select currency from currencytype";
                DespatchCommonFunction.Loadcombo(comboCurrency, strsql, "currency", "currency", Utility.DespacthConnectionString);

                strsql = "";
                strsql = "Select Distinct(Destination) as Destination from DomesticInvoice order by Destination";
                DespatchCommonFunction.Loadcombo(comboDestination, strsql, "Destination", "Destination", Utility.DespacthConnectionString);

                Database.myreader = Database.GetExecuteReaderCommand("select * from MaterialProcessing..excisemaster");
                if (Database.myreader.Read())
                {
                    textExDuty.Text = Database.myreader[0].ToString();
                    textEduCess.Text = Database.myreader[1].ToString();
                    textSecHiEdu.Text = Database.myreader[2].ToString();
                }
                Database.myreader.Close();

                strsql = "Select EPCGLicNo from EPCGMaster where companyname = '" + comboCompanyName.Text + "' order by EPCGLicNo";
                DespatchCommonFunction.Loadcombo(comboEPCGNo, strsql, "EPCGLicNo", "EPCGLicNo", Utility.DespacthConnectionString);

                if (comboEPCGNo.Items.Count > 0)
                    comboEPCGNo.SelectedIndex = 0;

                string EPCGNo = "";
                Database.myreader = Database.GetExecuteReaderCommand("Select * from EPCGMaster where companyname = '"
                + comboCompanyName.Text + "' and setDefault = 'yes' order by EPCGLicNo");
                if (Database.myreader.Read())
                    EPCGNo = Database.myreader[0].ToString();
                Database.myreader.Close();


                comboEPCGNo.Text = EPCGNo;
                //comboEPCGNo.Text = "3730000120";

                Database.myreader = Database.GetExecuteReaderCommand("Select * from UTBond where companyname = '"
                    + comboCompanyName.Text + "' order by BondDate");
                while (Database.myreader.Read())
                {
                    textUT1Bond.Text = Database.myreader[1].ToString();
                    dateTimePickerUT1Bond.Value = (DateTime)Database.myreader[2];
                    dateTimeUTBondExpiryDate.Value = (DateTime)Database.myreader[5];
                }
                Database.myreader.Close();

                strsql = "Select Distinct(StateName) as StateName from StateMaster WITH(NOLOCK)  ";
                DespatchCommonFunction.Loadcombo(comboState, strsql, "StateName", "StateName", Utility.DespacthConnectionString);

                Database.Closeconnection();
                //if (comboCommodity.Items.Count > 0)
                //    comboCommodity.SelectedIndex = 0;

            }

            groupAdvanceLic.Visible = false;
            groupEPCG.Visible = false;
            groupDirectExport.Visible = false;
            comboTaxApp.SelectedIndex = 1;
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
            if (Database.OpenConnection(Utility.MaterialConnectionString))
            {
                string strsql = "";
                strsql = "select LedgerName from ledgermaster where isinvoice = 'yes' and companyname = '" + comboCompanyName.Text + "' and Typeofinvoice = 'Export'";
                DespatchCommonFunction.Loadcombo(comboAccountHead, strsql, "LedgerName", "LedgerName", Utility.MaterialConnectionString);
                Database.Closeconnection();
            }
            textPkgsDetails.Text = "";
            string Remarks = "";
            if (Database.OpenConnection(Utility.DespacthConnectionString))
            {
                Database.myreader = Database.GetExecuteReaderCommand("select top 1 remarks from PackingRemarks order by Id Desc");
                if (Database.myreader.Read())
                {
                    Remarks = Database.myreader[0].ToString();
                }
                Database.myreader.Close();
            }
            textPkgsDetails.Text = Remarks;
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

        private void btnSave_Click(object sender, EventArgs e)
        {
            int VenCnt = DespatchCommonFunction.CustomerLedgerName(comboBuyerName.Text, comboCompanyName.Text);
            if (VenCnt == 0)
            {
                MessageBox.Show("No Account Ledger Found for this customer.\nPlease Ask Account Dept to create Ledger for this customer:- [" + comboBuyerName.Text + "]");
                return;
            }


            if (!FrmMain.IsCreateExportInvoice)
                MessageBox.Show("You have no right to save Export Invoice");
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
                else
                {
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
                            textEntryNo.Text = GetEntryNo(textARESuffix.Text, comboCompanyName.Text, Convert.ToDateTime(dateTimePickerInvDate.Text));
                            Database.Closeconnection();
                        }
                        if (textEntryNo.Text == "")
                            MessageBox.Show("Please Enter value in Entry Number");
                        else
                        {

                            total = Calculation(DOrderQty.Index, DUnitPrice.Index, DTotal.Index);
                            IsWt = WeightDifferences(DNetWt.Index, DGrossWt.Index);

                            if (Database.OpenConnection(Utility.DespacthConnectionString) && checkAdvanceLic.Checked && TotalCount <= 1)
                            {
                                Database.myreader = Database.GetExecuteReaderCommand("select * from AdvLicense where commodity ='" +
                                                          dataGridView1.Rows[0].Cells[3].Value.ToString()
                                                          + "' and advlicenseno = '" + comboAdvLicenseNo.Text
                                                          + "' and companyname = '" + comboCompanyName.Text + "'");
                                while (Database.myreader.Read())
                                    dateTimePickerAdvLicenseDate.Value = Convert.ToDateTime(Database.myreader[8].ToString());

                                Database.myreader.Close();
                                Database.Closeconnection();
                            }

                            if (textSpDuty1Amt.Text == "")
                                textSpDuty1Amt.Text = "0";
                            if (textSpDuty2Amt.Text == "")
                                textSpDuty2Amt.Text = "0";
                            if (textSpDuty3Amt.Text == "")
                                textSpDuty3Amt.Text = "0";
                            string iscommercial = "";
                            string commercialinvoice = "";

                            if (checkCommercialInvoice.Checked)
                            {
                                iscommercial = "yes";
                                commercialinvoice = comboCommercialInvoice.Text;
                            }
                            else
                                iscommercial = "no";


                            GrossWts = Convert.ToDouble(dataGridView1.Rows[0].Cells[11].FormattedValue.ToString());

                            //total = Math.Round(total, 0);
                            total = Math.Round(total, 0, MidpointRounding.AwayFromZero);
                            double temptotal = total;
                            //total = Math.Round(total, 0);

                            double Excise = 0;
                            double Educess = 0;
                            double HiEducess = 0;
                            double Vat = 0;
                            //  double CVD = 0;
                            double AddVat = 0;
                            if (checkTrading.Checked)
                            {
                                Excise = Convert.ToDouble(textExciseAmt.Text);
                                Educess = Convert.ToDouble(textEduAmt.Text);
                                HiEducess = Convert.ToDouble(textSecHieduAmt.Text);
                                Vat = Convert.ToDouble(textVatAmt.Text);
                                AddVat = Convert.ToDouble(textAddVatAmt.Text);
                            }
                            else
                            {
                                Excise = (total * Convert.ToDouble(textExDuty.Text)) / 100;
                                if (Convert.ToDouble(textCVD.Text) > 0)
                                {
                                    textTCS.Text = textCVD.Text;
                                    TCS = ((total + Excise) * Convert.ToDouble(textCVD.Text)) / 100;
                                    TCS = Math.Round(TCS, 0);
                                }
                                Excise = Math.Round(Excise, 0);

                                Educess = ((Convert.ToDouble(textEduCess.Text) * (Excise + TCS)) / 100);
                                HiEducess = ((Convert.ToDouble(textSecHiEdu.Text) * (Excise + TCS)) / 100);
                                Educess = Math.Round(Educess, 0);
                                HiEducess = Math.Round(HiEducess, 0);


                                Vat = ((total + Excise + Educess + HiEducess) * Convert.ToDouble(textVat.Text)) / 100;
                                AddVat = ((total + Excise + Educess + HiEducess) * Convert.ToDouble(textAddVat.Text)) / 100;

                                Vat = Math.Round(Vat, 0);
                                AddVat = Math.Round(AddVat, 0);

                            }

                            //if (comboTaxApp.SelectedIndex == 1)
                            //    total = total + Educess + HiEducess + Vat + AddVat + Excise;
                            if (comboCompanyName.Text == "Oswal Extrusion Limited (Unit-II)")
                            {

                                Vat = ((total) * Convert.ToDouble(textVat.Text)) / 100;
                                AddVat = ((total) * Convert.ToDouble(textAddVat.Text)) / 100;

                                Vat = Math.Round(Vat, 0);
                                AddVat = Math.Round(AddVat, 0);
                                total = total + Vat + AddVat;
                                temptotal = Convert.ToDouble(textGrdTotal.Text.ToString());
                            }

                            Friegth = Convert.ToDouble(textFrieght.Text);
                            total = total + Friegth;

                            double spduty1 = Convert.ToDouble(textSpDuty1Amt.Text);
                            double spduty2 = Convert.ToDouble(textSpDuty2Amt.Text);
                            double spduty3 = Convert.ToDouble(textSpDuty3Amt.Text);

                            total = total + spduty1 + spduty2 + spduty3;

                            total = Math.Round(total, 0, MidpointRounding.AwayFromZero);

                            //total = Math.Round(total, 0);
                            textGrdTotal.Text = Convert.ToString(Convert.ToInt64(total));

                            bool IsAppEpCG = false;

                            for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                            {
                                if (dataGridView1.Rows[i].Cells[DNoofPkg.Index].FormattedValue.ToString() != "" &&
                                     dataGridView1.Rows[i].Cells[DPkgQty.Index].FormattedValue.ToString() != "" &&
                                     dataGridView1.Rows[i].Cells[DItemDesc.Index].FormattedValue.ToString() != "" &&
                                     dataGridView1.Rows[i].Cells[DUOM.Index].FormattedValue.ToString() != "")
                                {

                                    IsAppEpCG = false;
                                    TempTotal += Convert.ToDouble(dataGridView1.Rows[i].Cells[DTotal.Index].Value.ToString());
                                    TempTotal = Math.Round(TempTotal, 0);
                                    if (CheckApplicabilityEPCG_PendingLimit(comboEPCGNo.Text, TempTotal))
                                        IsAppEpCG = true;

                                    commodity += dataGridView1.Rows[i].Cells[DCommodity.Index].Value.ToString() + ",";
                                }
                            }

                            bool IsAppAdvLicense = true;
                            if ((checkAdvanceLic.Checked && TotalCount == 1) || (checkAdvanceLic.Checked && !SameCommodity))
                            {
                                if (CheckApplicabilityAdvLic_PendingLimit(comboAdvLicenseNo.Text, GrossWts))
                                    IsAppAdvLicense = true;
                            }

                            bool IsWrongDate = false;
                            if (textBuyerOrderNo.Text != "")
                            {
                                if (dateTimeBuyerOrderNo.Text == "01/01/1900" || dateTimeBuyerOrderNo.Text == "")
                                    IsWrongDate = true;
                            }
                            if (!IsAppEpCG && checkEPCG.Checked)
                                MessageBox.Show("Limit of EPCG is exceeded beyond Licesene Limit");
                            else if (!IsAppAdvLicense && checkAdvanceLic.Checked)
                                MessageBox.Show("Limit of Advance Limit is exceeded beyond Licesene Limit");
                            else if (comboDeptt.Text == "")
                                MessageBox.Show("Please enter Value in DepttName");
                            else if (TotalCount <= 0)
                                MessageBox.Show("Please Select Items from Item Information");
                            else if (IsWt)
                                MessageBox.Show("Net Weight should be lesser than Gross Weight");
                            else if (IsWrongDate)
                                MessageBox.Show("Buyer Order Date is not Correct Format");
                            else if (TotalCount > 0)
                            {
                                MessageBox.Show("You are selecting Commodity " + commodity);
                                GetCommodity_Theding();
                                int DespatchNo = 0;
                                if (Database.OpenConnection(Utility.DespacthConnectionString))
                                {
                                    //change new logic for GST Inmplemnt  for commer inv and exp inv will be have same serries now
                                    if (DateTime.Now < Convert.ToDateTime("2017-07-01 00:00:00"))
                                    {
                                        Database.myreader = Database.GetExecuteReaderCommand("Select * from ExportInvoice where aresuffix = '"
                                            + textARESuffix.Text + "' and companyname = '" + comboCompanyName.Text + "' order by InvNo");
                                        while (Database.myreader.Read())
                                        {
                                            InvNo = Database.myreader["InvNo"].ToString();
                                            ARENo = Convert.ToInt32(Database.myreader["ARENo"].ToString());
                                        }
                                        Database.myreader.Close();
                                        if (InvNo == "")
                                            InvNo = "E00001";
                                        else
                                            Utility.IncreaseValueByone(ref InvNo, 1);
                                        DespatchNo = Convert.ToInt32(InvNo.Substring(1));

                                        ARENo = ARENo + 1;
                                    }
                                    else
                                    {
                                        int InvoiceSeries = 0;
                                        InvNo = DespatchCommonFunction.GenerateInvno(comboCompanyName.Text, textARESuffix.Text, ref InvoiceSeries);

                                        Database.myreader = Database.GetExecuteReaderCommand("Select  top 1 ARENo from ExportInvoice where aresuffix = '"
                                            + textARESuffix.Text + "' and companyname = '" + comboCompanyName.Text + "' order by InvNo desc");
                                        while (Database.myreader.Read())
                                        {
                                            ARENo = Convert.ToInt32(Database.myreader["ARENo"].ToString());
                                        }
                                        Database.myreader.Close();
                                        DespatchNo = Convert.ToInt32(InvoiceSeries);
                                        ARENo = ARENo + 1;
                                    }
                                    string ConsigneeCode = "";
                                    Database.myreader = Database.GetExecuteReaderCommand("select ConsigneeCode from Consignee where consigneename = '"
                                     + comboConsigneeName.Text + "' and companycode ='" + CompanyCode + "' and city = ' " + comboConsigneeCity.Text + "'");
                                    if (Database.myreader.Read())
                                        ConsigneeCode = Database.myreader[0].ToString();
                                    Database.myreader.Close();

                                    string InsuranceApp = "";
                                    string Insurance = "";
                                    if (checkInsurance.Checked)
                                    {
                                        InsuranceApp = "yes";
                                        Insurance = textInsurance.Text;
                                    }
                                    else
                                    {
                                        InsuranceApp = "no";
                                        Insurance = "";
                                    }


                                    string st2 = "";
                                    if (checkAvailBenefit.Checked)
                                        st2 = "You have availed benefit 39-A";
                                    else
                                        st2 = "you have not availed benefit 39-A";

                                    string buyerorderdate = "";
                                    if (textBuyerOrderNo.Text == "")
                                        buyerorderdate = System.DateTime.Now.ToString(DespatchCommonFunction.ERPDBDateSettings());//dr[5] = System.DateTime.Now.ToString("MM/dd/yyyy");
                                    else
                                        buyerorderdate = dateTimeBuyerOrderNo.Value.ToString(DespatchCommonFunction.ERPDBDateSettings());
                                    string EpCGNO = "";
                                    string EPCGNODate = "";
                                    string Advlicenseno = "";
                                    string AdvlicensenoDate = "";
                                    string Utibond = "";
                                    string UtiBondDate = "";
                                    if (checkEPCG.Checked)
                                    {
                                        EpCGNO = comboEPCGNo.Text;
                                        EPCGNODate = dateTimePickerEPCGDate.Value.ToString(DespatchCommonFunction.ERPDBDateSettings());
                                    }
                                    if ((checkAdvanceLic.Checked || checkDFIA.Checked) && TotalCount == 1)
                                    {
                                        Advlicenseno = comboAdvLicenseNo.Text;
                                        AdvlicensenoDate = dateTimePickerAdvLicenseDate.Value.ToString(DespatchCommonFunction.ERPDBDateSettings());
                                    }
                                    if (comboBeingExport.SelectedIndex == 1) //UTBond
                                    {
                                        Utibond = textUT1Bond.Text;
                                        UtiBondDate = dateTimePickerUT1Bond.Value.ToString(DespatchCommonFunction.ERPDBDateSettings());
                                    }
                                    string totalamtword = "";
                                    string exciseamtword = "";
                                    if (comboTaxApp.SelectedIndex == 0)
                                    {
                                        totalamtword = Utility.IntegerToWords(Convert.ToInt64(TempTotal));
                                        exciseamtword = "NIL";
                                    }
                                    else if (comboTaxApp.SelectedIndex == 1)
                                    {
                                        Int64 totals = Convert.ToInt64(textGrdTotal.Text);
                                        Int64 excises = Convert.ToInt64(Excise + Educess + HiEducess);
                                        totalamtword = Utility.IntegerToWords(totals);
                                        exciseamtword = Utility.IntegerToWords(excises);
                                    }
                                    string utbonddetails = "";
                                    if (checkAvailBenefit.Checked) //UTBond
                                        utbonddetails = textUTBondDetails.Text;

                                    string billofexportdate = "";
                                    string utbondexpirydate = "";
                                    string isepcg = "";
                                    string isadvlic = "";
                                    string isimport = "";
                                    string iscenvat = "";
                                    if (comboBillOfExportNo.Text != "")
                                        billofexportdate = dateTimeBillofExportDate.Value.ToString(DespatchCommonFunction.ERPDBDateSettings());
                                    if (comboBeingExport.SelectedIndex == 1)
                                        utbondexpirydate = dateTimeUTBondExpiryDate.Value.ToString(DespatchCommonFunction.ERPDBDateSettings());

                                    if (checkEPCG.Checked)
                                        isepcg = "yes";
                                    else
                                        isepcg = "no";

                                    if (checkAdvanceLic.Checked)
                                        isadvlic = "yes";
                                    else
                                        isadvlic = "no";

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
                                    if (checkTrading.Checked)
                                        istrading = "Trading Invoice";
                                    if (checkDFIA.Checked)
                                        isdfia = "yes";
                                    else
                                        isdfia = "no";
                                    bool isentry = false;
                                    string exciseamtinword = "";
                                    string temptotalinword = "";
                                    temptotalinword = Utility.IntegerToWords(Convert.ToInt64(TempTotal));
                                    exciseamtinword = Utility.IntegerToWords(exciseamt);
                                    DialogResult drs = MessageBox.Show("Do you want to generate " + comboTypeInvoice.Text.ToUpper()
                                     + Environment.NewLine + " Do you want to generate  Invoice for " + comboCompanyName.Text.ToUpper()
                                     + Environment.NewLine + st2, "Generate Export Invoice"
                                                     , MessageBoxButtons.YesNo, MessageBoxIcon.Information);

                                    if (drs.CompareTo(DialogResult.Yes) == 0)
                                    {
                                        if (Database.OpenConnection(Utility.DespacthConnectionString))
                                        {
                                            Database.BeginTransaction();

                                            if (Database.GetExecuteNonQueryCommand("insert into ExportInvoice(SalesOrderNo,InvNo,InvDate,TermsOfPayment,BuyerOrderNo,BuyerDate,DespatchNo,DespatchDate,VehicalNo,Destination,TermsOfDelivery,BuyerName,ExDuty,Educess,SecHiEduCess,vat,Addvat,ExciseAmt,EduCessAmt,SecHiEduCessAmt,VatAmt,AddVatAmt,TotalQty,TotalAmt,InvDateTime,RemDateTime,TypeOfInvoice,DelChallanNo,DelChallanDate,LRNo,LRDate,ARENo,EPCGNo,EPCGDate,AdvNo,AdvDate,UT1BondNo,UT1BondDate,ExciseNo,ExciseDate,LoginName,TotalAmtWord,ExciseAmtWord,TransportName,DutyRemarks,VatRemarks,EntryNo,Note,BeingExport,DetailsExport,BillofExportNo,BillofExportDate,ExpiryDate,EPCGLic,AdvanceLic,ImportMaterial,CenvatAcc,DestPlace,ARESuffix,AppCompName,DirectExport,PackingListNo,ContainerNo,BottleSealNo,PackageNo,Remarks,Frieght,TCSPer,TCSAmt,ExcieseAmtWords,TaxApp,Currency,CurrencyValue,ModeTransport,NoModeTransport,AREDetails,AREDetails1,Modes,Consignee,Deptt,TypeVat,Header,CompanyName,ConsigneeCode,THeading,Supplementarycode,AgainstInvNo,AccessableinWords,IsCenVat,CenvatRg23,IsCancel,SpDuty1,SpDutyAmt,SpDuty2,SpDuty2Amt,SpDuty3,SpDuty3Amt,Notifier,Trading,IsDFIA,AccountHead,PkgsDetails,IsMarketing,iscommercial,commercialInvno,CheckInsurance,Insurance,isDutyDrawBack) values('','"
                                                + InvNo + "','" + dateTimePickerInvDate.Value.ToString(DespatchCommonFunction.ERPDBDateSettings()) + "','"
                                                + textTermsofPayment.Text + "','" + textBuyerOrderNo.Text + "','"
                                                + buyerorderdate + "','" + DespatchNo + "','" + dateTimePickerDespatchDate.Value.ToString(DespatchCommonFunction.ERPDBDateSettings()) + "','"
                                                + comboVehicalNo.Text.ToUpper() + "','" + comboDestination.Text.ToUpper() + "','"
                                                + textTermsofDelivery.Text.ToUpper() + "','" + comboBuyerName.Text.ToUpper() + "',"
                                                + textExDuty.Text + "," + textEduCess.Text + "," + textSecHiEdu.Text + "," + textVat.Text + ","
                                                + textAddVat.Text + "," + Excise + "," + Educess + ","
                                                + HiEducess + ","
                                                + Vat + ","
                                                + AddVat + ",0," + textGrdTotal.Text + ",'"
                                                + dateTimePickerInvoice.Value.ToString(DespatchCommonFunction.ERPDBDateSettings()) + "','"
                                                + dateTimePickerDateTimeRemoval.Value.ToString(DespatchCommonFunction.ERPDBDateSettings()) + "','"
                                                + comboTypeInvoice.Text + "' ,'"
                                                + textDelChallanNo.Text + "','"
                                                + dateTimePickerChallanDate.Value.ToString(DespatchCommonFunction.ERPDBDateSettings()) + "' ,'"
                                                + textBoxLRNo.Text.ToUpper() + "' ,'"
                                                + dateTimePickerLRNo.Value.ToString(DespatchCommonFunction.ERPDBDateSettings()) + "' ,'"
                                                + ARENo + "','" + EpCGNO + "','" + EPCGNODate + "','"
                                                + Advlicenseno + "','" + AdvlicensenoDate + "','" + Utibond + "','"
                                                + UtiBondDate + "','" + textExciseNotification.Text + "','"
                                                + dateTimePickerExciseDate.Value.ToString(DespatchCommonFunction.ERPDBDateSettings()) + "','"
                                                + FrmMain.UserName + "','"
                                                + totalamtword + "','" + exciseamtword + "','"
                                                + comboTransporter.Text + "','"
                                                + textDutyRemarks.Text + "','"
                                                + textVatRemarks.Text + "','"
                                                + textEntryNo.Text + "','"
                                                + comboNote.Text + "','"
                                                + comboBeingExport.Text + "','" + utbonddetails + "','"
                                                + comboBillOfExportNo.Text + "','"
                                                + billofexportdate + "','"
                                                + utbondexpirydate + "','" + isepcg + "','"
                                                + isadvlic + "','" + isimport + "','" + iscenvat + "','"
                                                + textDestinationPlace.Text.ToUpper() + "','"
                                                + textARESuffix.Text + "','"
                                                + textAppCompanyName.Text + "','" + isdirectexport + "','"
                                                + packinglistno + "','" + contno + "','" + bottlesealno + "','"
                                                + packages + "' ,'" + textRemarks.Text + "',"
                                                + textFrieght.Text + ","
                                                + textTCS.Text + ","
                                                + TCS + ",'" + exciseamtinword + "','"
                                                + comboTaxApp.Text + "','"
                                                + comboCurrency.Text + "','"
                                                + comboCurrencyValue.Text + "','"
                                                + comboModeTransport.Text + "','" + st + "','"
                                                + textAREDetail.Text + "','"
                                                + textAREDetails1.Text + "','" + st1 + "','"
                                                + textConsignee.Text.ToUpper() + "' ,'"
                                                + comboDeptt.Text + "','"
                                                + comboType.Text + "','','"
                                                + comboCompanyName.Text + "','"
                                                + ConsigneeCode + "','"
                                                + textHeading.Text + "','','','"
                                                + temptotalinword + "','" + iscenvattaken + "','"
                                                + rg23A + "','','" + textSpDutyRemarks1.Text + "',"
                                                + textSpDuty1Amt.Text + ",'" + textSpDutyRemark2.Text + "'," + textSpDuty2Amt.Text + ",'"
                                                + textSpDutyRemarks3.Text + "'," + textSpDuty3Amt.Text + ",'"
                                                + textNotifier.Text + "','" + istrading + "','"
                                                + isdfia + "','" + comboAccountHead.Text + "','"
                                                + textPkgsDetails.Text + "','no','"
                                                + iscommercial + "','"
                                                + commercialinvoice + "','"
                                                + InsuranceApp + "','"
                                                + Insurance + "','')"))
                                                isentry = true;

                                            Database.GetExecuteNonQueryCommand("update Exportinvoice set billofexportdate = Null  " +
                                            " where BillofExportdate = '1900-01-01 00:00:00.000'");

                                            UpdateInsurancePolicy(Convert.ToDouble(textGrdTotal.Text), checkInsurance.Checked);


                                            textInvNo.Text = InvNo;

                                            SetEntryNo(textARESuffix.Text, comboCompanyName.Text, Convert.ToDateTime(dateTimePickerInvDate.Text));

                                            // Database.Closeconnection();
                                            Database.myadapter = Database.GetAdapterCommand("select  top 1 * from ExportInvItem");
                                            DataSet dataset1 = new DataSet();
                                            Database.myadapter.Fill(dataset1, "ExportInvItem");
                                            for (int i = 0; i < TotalCount; i++)
                                            {
                                                // if (Database.OpenConnection(Utility.DespacthConnectionString))
                                                {
                                                    double Currency = GetCurrencyConversion();
                                                    //double GPM = 0;
                                                    //double GSM = 0;
                                                    //double Reqwt = 0;
                                                    double NetWt = Convert.ToDouble(dataGridView1.Rows[i].Cells[DNetWt.Index].FormattedValue.ToString());
                                                    double OrderQty = Convert.ToDouble(dataGridView1.Rows[i].Cells[DOrderQty.Index].FormattedValue.ToString());

                                                    //insert data in database 
                                                    DataRow dr = dataset1.Tables[0].NewRow();
                                                    dr[0] = InvNo;

                                                    if (dataGridView1.Rows[i].Cells[DNoofPkg.Index].FormattedValue.ToString() != "")
                                                        dr[1] = dataGridView1.Rows[i].Cells[DNoofPkg.Index].FormattedValue.ToString();

                                                    if (dataGridView1.Rows[i].Cells[DItemDesc.Index].FormattedValue.ToString() != "")
                                                        dr[2] = dataGridView1.Rows[i].Cells[DItemDesc.Index].Value.ToString();

                                                    if (dataGridView1.Rows[i].Cells[DOrderQty.Index].FormattedValue.ToString() != "")
                                                        dr[3] = dataGridView1.Rows[i].Cells[DOrderQty.Index].Value.ToString();

                                                    if (dataGridView1.Rows[i].Cells[DUnitPrice.Index].FormattedValue.ToString() != "")
                                                        dr[4] = dataGridView1.Rows[i].Cells[DUnitPrice.Index].Value.ToString();

                                                    if (dataGridView1.Rows[i].Cells[DUOM.Index].FormattedValue.ToString() != "")
                                                        dr[5] = dataGridView1.Rows[i].Cells[DUOM.Index].Value.ToString();

                                                    if (dataGridView1.Rows[i].Cells[DTotal.Index].FormattedValue.ToString() != "")
                                                        dr[6] = dataGridView1.Rows[i].Cells[DTotal.Index].Value.ToString();
                                                    if (dataGridView1.Rows[i].Cells[DNetWt.Index].FormattedValue.ToString() != "")
                                                        dr[7] = dataGridView1.Rows[i].Cells[DNetWt.Index].Value.ToString();
                                                    if (dataGridView1.Rows[i].Cells[DGrossWt.Index].FormattedValue.ToString() != "")
                                                        dr[8] = dataGridView1.Rows[i].Cells[DGrossWt.Index].Value.ToString();

                                                    dr[9] = "";
                                                    dr[10] = FrmMain.UserName;

                                                    dr[11] = dataGridView1.Rows[i].Cells[DCommodity.Index].FormattedValue.ToString(); ; //commodity

                                                    dr[12] = textARESuffix.Text;

                                                    if (dataGridView1.Rows[i].Cells[DTrWt.Index].FormattedValue.ToString() != "")
                                                        dr[13] = dataGridView1.Rows[i].Cells[DTrWt.Index].FormattedValue.ToString();

                                                    if (dataGridView1.Rows[i].Cells[DQtyRemarks.Index].FormattedValue.ToString() != "")
                                                        dr[14] = dataGridView1.Rows[i].Cells[DQtyRemarks.Index].FormattedValue.ToString();



                                                    dr[15] = comboCompanyName.Text;
                                                    if (dataGridView1.Rows[i].Cells[DtCommodityRemarks.Index].FormattedValue.ToString() != "")
                                                        dr[16] = dataGridView1.Rows[i].Cells[DtCommodityRemarks.Index].FormattedValue.ToString().ToUpper();

                                                    if (dataGridView1.Rows[i].Cells[DPkgQty.Index].FormattedValue.ToString() != "")
                                                        dr[17] = dataGridView1.Rows[i].Cells[DPkgQty.Index].FormattedValue.ToString();
                                                    if (dataGridView1.Rows[i].Cells[DPKgUnit.Index].FormattedValue.ToString() != "")
                                                        dr[18] = dataGridView1.Rows[i].Cells[DPKgUnit.Index].FormattedValue.ToString();

                                                    dr[19] = Currency;
                                                    if (dataGridView1.Rows[i].Cells[DtDeptt.Index].FormattedValue.ToString() != "")
                                                        dr[21] = dataGridView1.Rows[i].Cells[DtDeptt.Index].FormattedValue.ToString();

                                                    dr["HSNCode"] = dataGridView1.Rows[i].Cells[ColHSNCode.Index].Value.ToString();
                                                    dr["CGSTrate"] = Convert.ToDouble(dataGridView1.Rows[i].Cells[colCGSTRate.Index].Value);
                                                    dr["CGSTamt"] = Convert.ToDouble(dataGridView1.Rows[i].Cells[colCGSTAmt.Index].Value);
                                                    dr["SGSTRate"] = Convert.ToDouble(dataGridView1.Rows[i].Cells[ColSGSTRate.Index].Value);
                                                    dr["SGSTAmt"] = Convert.ToDouble(dataGridView1.Rows[i].Cells[ColSGSTAMT.Index].Value);
                                                    dr["IGSTrate"] = Convert.ToDouble(dataGridView1.Rows[i].Cells[ColIGSTRate.Index].Value);
                                                    dr["IGSTAmt"] = Convert.ToDouble(dataGridView1.Rows[i].Cells[ColIGSTAmt.Index].Value);


                                                    dataset1.Tables[0].Rows.Add(dr);
                                                    Database.mycmdBuilder = Database.GetCmdBuilderCommand(Database.myadapter);
                                                    int x = Database.myadapter.Update(dataset1.Tables[0]);

                                                    //  Database.Closeconnection();

                                                    if (TotalCount == 1 || !SameCommodity)
                                                    {
                                                        if (checkImport.Checked)
                                                            InsertValueInExportCntMaterial(InvNo, textARESuffix.Text);
                                                        if (checkAdvanceLic.Checked)
                                                        {
                                                            GrossWts = 0;
                                                            for (int j = 0; j < TotalCount; j++)
                                                                GrossWts += Convert.ToDouble(dataGridView1.Rows[j].Cells[DNetWt.Index].Value.ToString());

                                                            DespatchCommonFunction.UpdateAdvLic_PendingLimit(comboCompanyName.Text, comboAdvLicenseNo.Text, GrossWts);
                                                        }
                                                    }

                                                    if (checkEPCG.Checked)
                                                        UpdateEPCG_PendingLimit(comboEPCGNo.Text, Convert.ToDouble(dataGridView1.Rows[i].Cells[DTotal.Index].Value.ToString()) * Currency);
                                                }
                                            }
                                            if (isentry)
                                            {
                                                Database.CommitTransaction();

                                                Database.Closeconnection();
                                                MessageBox.Show("Invoice No " + InvNo + " and ARENo = "
                                                    + ARENo.ToString() + " is generated succesfully");
                                            }
                                        }
                                        //IsUpdate = false;
                                        TotalCount = 0;
                                        textAddVat.Text = "0";
                                        textBoxLRNo.Text = "";
                                        textBuyerOrderNo.Text = "";
                                        textDelChallanNo.Text = "";
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
                                        checkTrading.Checked = false;

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
                        Database.RollBackTransaction();
                        ErrorMessageBox.Show(ex);

                    }
                }
            }
        }

        private void GetCommodity_Theding()
        {
            string THeading = "";
            textHeading.Text = "";
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
                              + dataGridView1.Rows[i].Cells[DCommodity.Index].Value.ToString() + "'");
                        while (Database.myreader.Read())
                        {
                            ArrTHeading.Add(Database.myreader[0].ToString());
                            dataGridView1.Rows[i].Cells[ColHSNCode.Index].Value = Database.myreader[0].ToString();
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
                if (textCVD.Text == "")
                    textCVD.Text = "0";
                if (textSpDuty1Amt.Text == "")
                    textSpDuty1Amt.Text = "0";
                if (textSpDuty2Amt.Text == "")
                    textSpDuty2Amt.Text = "0";
                if (textSpDuty3Amt.Text == "")
                    textSpDuty3Amt.Text = "0";


                for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                {
                    if (dataGridView1.Rows[i].Cells[QtyIndex].FormattedValue.ToString() != "" &&
                            dataGridView1.Rows[i].Cells[DCommodity.Index].FormattedValue.ToString() != "" &&
                            dataGridView1.Rows[i].Cells[DOrderQty.Index].FormattedValue.ToString() != "" &&
                            dataGridView1.Rows[i].Cells[DUnitPrice.Index].FormattedValue.ToString() != "" &&
                            dataGridView1.Rows[i].Cells[AmountIndex].FormattedValue.ToString() != "")
                    {
                        if (i == 0)
                            CommodityName = dataGridView1.Rows[0].Cells[DCommodity.Index].Value.ToString();

                        if (CommodityName != dataGridView1.Rows[0].Cells[DCommodity.Index].Value.ToString())
                            SameCommodity = true;

                        Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[DPkgQty.Index].Value.ToString());
                        TotalCount = TotalCount + 1;

                        double NetWt = Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[DNetWt.Index].Value.ToString());
                        double GrossWt = Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[DGrossWt.Index].Value.ToString());
                        double TrWt = GrossWt - NetWt;

                        dataGridView1.Rows[i].Cells[DTrWt.Index].Value = TrWt.ToString();

                        dataGridView1.Rows[i].Cells[TotalIndex].Value = Convert.ToString(Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[QtyIndex].Value.ToString())
                                   * Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[AmountIndex].Value.ToString()));
                        total += Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[TotalIndex].Value);
                        total = Math.Round(total, 2, MidpointRounding.AwayFromZero);
                    }

                    //colCGSTrate colCGSTamt  colSGSTRate   ColSGSTAmt

                    {

                        {
                            dataGridView1.Rows[i].Cells[colCGSTAmt.Index].Value =
                                Math.Round(Convert.ToDouble(dataGridView1.Rows[i].Cells[DTotal.Index].Value.ToString()) *
                                Convert.ToDouble(dataGridView1.Rows[i].Cells[colCGSTRate.Index].Value.ToString()) / 100, 2);

                            dblCGSTamt += Convert.ToDouble(dataGridView1.Rows[i].Cells[colCGSTAmt.Index].Value);
                        }
                    }

                    {

                        {
                            dataGridView1.Rows[i].Cells[ColSGSTAMT.Index].Value =
                                Math.Round(Convert.ToDouble(dataGridView1.Rows[i].Cells[DTotal.Index].Value.ToString()) *
                                Convert.ToDouble(dataGridView1.Rows[i].Cells[ColSGSTRate.Index].Value.ToString()) / 100, 2);

                            dblSGSTAmt += Convert.ToDouble(dataGridView1.Rows[i].Cells[ColSGSTAMT.Index].Value);
                        }
                    }

                    {

                        {
                            dataGridView1.Rows[i].Cells[ColIGSTAmt.Index].Value =
                                Math.Round(Convert.ToDouble(dataGridView1.Rows[i].Cells[DTotal.Index].Value.ToString()) *
                                Convert.ToDouble(dataGridView1.Rows[i].Cells[ColIGSTRate.Index].Value.ToString()) / 100, 2);

                            dblIGSTAmt += Convert.ToDouble(dataGridView1.Rows[i].Cells[ColIGSTAmt.Index].Value);
                        }
                    }

                }

                temptotal = total + dblCGSTamt + dblIGSTAmt + dblSGSTAmt;
                total = Math.Round(total + dblCGSTamt + dblIGSTAmt + dblSGSTAmt, 0);

                if (dateTimePickerInvDate.Value < Convert.ToDateTime("2017-07-01 00:00:00"))
                {
                    double Excise = 0;
                    double Educess = 0;
                    double HiEducess = 0;
                    double CVD = 0;
                    double Vat = 0;
                    double AddVat = 0;
                    if (checkTrading.Checked)
                    {
                        Excise = Convert.ToDouble(textExciseAmt.Text);
                        Educess = Convert.ToDouble(textEduAmt.Text);
                        HiEducess = Convert.ToDouble(textSecHiEdu.Text);
                        Vat = Convert.ToDouble(textVatAmt.Text);
                        AddVat = Convert.ToDouble(textAddVat.Text);
                    }
                    else
                    {

                        Excise = (total * Convert.ToDouble(textExDuty.Text)) / 100;
                        if (Convert.ToDouble(textCVD.Text) > 0)
                        {
                            textTCS.Text = textCVD.Text;
                            CVD = ((total + Excise) * Convert.ToDouble(textCVD.Text)) / 100;
                            CVD = Math.Round(CVD, 0);
                        }
                        Excise = Math.Round(Excise, 0);

                        Educess = ((Convert.ToDouble(textEduCess.Text) * (Excise + CVD)) / 100);
                        HiEducess = ((Convert.ToDouble(textSecHiEdu.Text) * (Excise + CVD)) / 100);
                        Educess = Math.Round(Educess, 0);
                        HiEducess = Math.Round(HiEducess, 0);


                        Vat = ((total + Excise + Educess + HiEducess) * Convert.ToDouble(textVat.Text)) / 100;
                        AddVat = ((total + Excise + Educess + HiEducess) * Convert.ToDouble(textAddVat.Text)) / 100;

                        Vat = Math.Round(Vat, 0);
                        AddVat = Math.Round(AddVat, 0);

                    }
                    //if (comboTaxApp.SelectedIndex == 1)
                    //    total = total + Educess + HiEducess + Vat + AddVat + Excise;
                    if (comboCompanyName.Text == "Oswal Extrusion Limited (Unit-II)")
                    {

                        Vat = ((total) * Convert.ToDouble(textVat.Text)) / 100;
                        AddVat = ((total) * Convert.ToDouble(textAddVat.Text)) / 100;

                        Vat = Math.Round(Vat, 0);
                        AddVat = Math.Round(AddVat, 0);
                        total = total + Vat + AddVat;
                    }
                }


                Friegth = Convert.ToDouble(textFrieght.Text);
                total = total + Friegth;

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
            Calculation(DOrderQty.Index, DUnitPrice.Index, DTotal.Index);
        }


        private void textInvNo_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            try
            {

                string companyname = "";
                bool iscoomercial = false;
                string commercialinvno = "";
                bool IsCheckIsurance = false;
                if (Database.OpenConnection(Utility.DespacthConnectionString))
                {


                    comboAdvLicenseNo.Items.Clear();
                    string Currency = "";
                    string BeingExport = "";
                    string AdvLicenseNo = "";
                    string EPCGNo = "";

                    bool IsDirectExport = false;
                    bool IsEPCGNo = false;
                    bool ismarketing = false;
                    Database.myreader = Database.GetExecuteReaderCommand("select IsMarketing from ExportInvoice where invno = '"
                    + textInvNo.Text + "' and companyName = '" + comboCompanyName.Text + "' and aresuffix = '" + textARESuffix.Text + "' and ismarketing != 'yes'");
                    if (Database.myreader.Read())
                        ismarketing = true;
                    Database.myreader.Close();
                    if (!ismarketing)
                        MessageBox.Show("Please Update InvNo in Export Invoice Order No ");
                    else
                    {
                        Database.myreader = Database.GetExecuteReaderCommand("select * from ExportInvoice where InvNo = '" +
                                                                           textInvNo.Text + "' and aresuffix = '" + textARESuffix.Text
                                                                           + "' and companyname = '" + comboCompanyName.Text + "'");
                        if (Database.myreader.Read())
                        {
                            //IsUpdate = true;
                            btnSave.Enabled = false;
                            btnUpdate.Enabled = true;
                            dateTimePickerInvDate.Value = Convert.ToDateTime(Database.myreader[2].ToString());
                            textTermsofPayment.Text = Database.myreader[3].ToString();
                            textBuyerOrderNo.Text = Database.myreader[4].ToString();
                            dateTimeBuyerOrderNo.Value = Convert.ToDateTime(Database.myreader[5].ToString());
                            textDesPatchNo.Text = Database.myreader[6].ToString();
                            dateTimePickerDespatchDate.Value = Convert.ToDateTime(Database.myreader[7].ToString());
                            comboVehicalNo.Text = Database.myreader[8].ToString();
                            comboDestination.Text = Database.myreader[9].ToString();
                            textTermsofDelivery.Text = Database.myreader[10].ToString();
                            companyname = Database.myreader[11].ToString();
                            textExDuty.Text = Database.myreader[12].ToString();
                            textEduCess.Text = Database.myreader[13].ToString();
                            textSecHiEdu.Text = Database.myreader[14].ToString();
                            textVat.Text = Database.myreader[15].ToString();
                            textAddVat.Text = Database.myreader[16].ToString();
                            textGrdTotal.Text = Database.myreader[23].ToString();
                            dateTimePickerInvoice.Value = Convert.ToDateTime(Database.myreader[24].ToString());
                            dateTimePickerDateTimeRemoval.Value = Convert.ToDateTime(Database.myreader[25].ToString());
                            comboTypeInvoice.Text = Database.myreader[26].ToString();

                            textDelChallanNo.Text = Database.myreader[27].ToString();
                            dateTimePickerChallanDate.Value = Convert.ToDateTime(Database.myreader[28].ToString());
                            textBoxLRNo.Text = Database.myreader[29].ToString();
                            dateTimePickerLRNo.Value = Convert.ToDateTime(Database.myreader[30].ToString());
                            textARENo.Text = Database.myreader[31].ToString();
                            EPCGNo = Database.myreader[32].ToString();
                            dateTimePickerEPCGDate.Value = Convert.ToDateTime(Database.myreader[33].ToString());
                            AdvLicenseNo = Database.myreader[34].ToString();
                            dateTimePickerAdvLicenseDate.Value = Convert.ToDateTime(Database.myreader[35].ToString());
                            textUT1Bond.Text = Database.myreader[36].ToString();
                            dateTimePickerUT1Bond.Value = Convert.ToDateTime(Database.myreader[37].ToString());
                            textExciseNotification.Text = Database.myreader[38].ToString();
                            dateTimePickerExciseDate.Value = Convert.ToDateTime(Database.myreader[39].ToString());
                            comboTransporter.Text = Database.myreader[43].ToString();
                            textDutyRemarks.Text = Database.myreader[44].ToString();
                            textVatRemarks.Text = Database.myreader[45].ToString();
                            textEntryNo.Text = Database.myreader[46].ToString();

                            comboNote.Text = Database.myreader[47].ToString();
                            BeingExport = Database.myreader[48].ToString();
                            if (Database.myreader[49].ToString() != "")
                            {
                                checkAvailBenefit.Checked = true;
                                textUTBondDetails.Text = Database.myreader[49].ToString();
                            }

                            comboBillOfExportNo.Text = Database.myreader[50].ToString();
                            dateTimeBillofExportDate.Value = Convert.ToDateTime(Database.myreader[51].ToString());
                            if (Database.myreader[105].ToString() == "yes")
                                IsCheckIsurance = true;
                            textHeading.Text = Database.myreader["Theading"].ToString();

                            if (Database.myreader["EPCGLic"].ToString() == "yes")
                                IsEPCGNo = true;
                            else
                                IsEPCGNo = false;

                            if (Database.myreader["AdvanceLic"].ToString() == "yes")
                            {
                                checkAdvanceLic.Checked = true;
                                checkAdvanceLic.Visible = true;
                                checkDFIA.Checked = false;
                                checkDFIA.Visible = false;
                            }
                            else
                                checkAdvanceLic.Checked = false;

                            if (Database.myreader["ImportMaterial"].ToString() == "yes")
                                checkImport.Checked = true;
                            else
                                checkImport.Checked = false;

                            if (Database.myreader["DirectExport"].ToString() == "yes")
                            {
                                IsDirectExport = true;
                                textPackingListNo.Text = Database.myreader["PackingListNo"].ToString();
                                textContNo.Text = Database.myreader["ContainerNo"].ToString();
                                textBottleSealNo.Text = Database.myreader["BottleSealNo"].ToString();
                            }
                            else
                                IsDirectExport = false;

                            textPackages.Text = Database.myreader["PackageNo"].ToString();

                            comboTaxApp.Text = Database.myreader["taxApp"].ToString();
                            Currency = Database.myreader["Currency"].ToString();
                            comboModeTransport.Text = Database.myreader["ModeTransport"].ToString();
                            comboDeptt.Text = Database.myreader["Deptt"].ToString();
                            comboType.Text = Database.myreader["TypeVat"].ToString();
                            textFrieght.Text = Database.myreader["frieght"].ToString();
                            textTCS.Text = Database.myreader["tcsper"].ToString();

                            if (Database.myreader[88].ToString() == "yes")
                            {
                                checkCENVAT.Checked = true;
                                textRG23A.Text = Database.myreader[89].ToString();
                            }


                            textSpDutyRemarks1.Text = Database.myreader[91].ToString();
                            textSpDuty1Amt.Text = Database.myreader[92].ToString();
                            textSpDutyRemark2.Text = Database.myreader[93].ToString();
                            textSpDuty2Amt.Text = Database.myreader[94].ToString();
                            textSpDutyRemarks3.Text = Database.myreader[95].ToString();
                            textSpDuty3Amt.Text = Database.myreader[96].ToString();
                            textNotifier.Text = Database.myreader[97].ToString();

                            textRemarks.Text = Database.myreader["Remarks"].ToString();
                            if (Database.myreader[98].ToString() == "Trading Invoice")
                            {
                                checkTrading.Checked = true;
                                textExciseAmt.Text = Database.myreader["ExciseAmt"].ToString();
                                textEduAmt.Text = Database.myreader["EducessAmt"].ToString();
                                textSecHieduAmt.Text = Database.myreader["SecHiEduCessAMt"].ToString();
                                textVatAmt.Text = Database.myreader["VatAmt"].ToString();
                                textAddVatAmt.Text = Database.myreader["AddVatAmt"].ToString();
                            }
                            else
                                checkTrading.Checked = false;

                            textTermsofPayment.Text = Database.myreader["termsofpayment"].ToString();
                            //textDestinationPlace.Text = Database.myreader["Destination"].ToString();
                            textAppCompanyName.Text = Database.myreader["AppCompName"].ToString();
                            textConsignee.Text = Database.myreader["Consignee"].ToString();

                            textCENVATNo.Text = Database.myreader["CenvatAcc"].ToString();
                            textDestinationPlace.Text = Database.myreader["DestPlace"].ToString();

                            textAREDetail.Text = Database.myreader["AREDetails"].ToString();
                            textAREDetails1.Text = Database.myreader["AREDetails1"].ToString();

                            if (Database.myreader["IsDFIA"].ToString() == "yes")
                            {
                                checkDFIA.Checked = true;
                                checkDFIA.Visible = true;
                                checkAdvanceLic.Visible = false;
                                checkAdvanceLic.Checked = false;
                            }
                            else
                                checkDFIA.Checked = false;

                            comboAccountHead.Text = Database.myreader["AccountHead"].ToString();
                            textPkgsDetails.Text = Database.myreader["PkgsDetails"].ToString();
                            if (Database.myreader["iscommercial"].ToString() == "yes")
                            {
                                iscoomercial = true;
                                commercialinvno = Database.myreader["commercialInvno"].ToString();
                            }
                            textExDuty.Text = Database.myreader[12].ToString();
                            textEduCess.Text = Database.myreader[13].ToString();
                            textSecHiEdu.Text = Database.myreader[14].ToString();

                        }
                        Database.myreader.Close();
                        if (IsCheckIsurance)
                            checkInsurance.Checked = true;
                        else
                            checkInsurance.Checked = false;
                        if (checkAdvanceLic.Checked || checkDFIA.Checked)
                        {
                            comboAdvLicenseNo.Items.Clear();
                            comboAdvLicenseNo.Items.Add(AdvLicenseNo);
                            if (comboAdvLicenseNo.Items.Count > 0)
                                comboAdvLicenseNo.SelectedIndex = 0;
                        }
                        if (iscoomercial)
                        {
                            checkCommercialInvoice.Checked = true;
                            comboCommercialInvoice.Text = commercialinvno;
                        }
                        else
                        {
                            checkCommercialInvoice.Checked = false;
                            comboCommercialInvoice.Text = "";
                        }
                        comboBeingExport.Text = BeingExport;
                        UpdateGrossWt = 0;
                        UpdatedTotalAmt = 0;

                        dataGridView1.Rows.Clear();
                        Database.OpenConnection(Utility.DespacthConnectionString);
                        object j = Database.GetExecuteScalarCommand_return("select count(*) from ExportInvItem where InvNo = '"
                            + textInvNo.Text + "' and yr= '"
                            + textARESuffix.Text + "' and companyname = '" + comboCompanyName.Text + "'");
                        TotalCount = Convert.ToInt32(j);

                        if (TotalCount > 0)
                            dataGridView1.Rows.Add(TotalCount);

                        int i = 0;
                        Database.myreader = Database.GetExecuteReaderCommand("select * from ExportInvItem where InvNo ='"
                                           + textInvNo.Text + "' and yr= '" + textARESuffix.Text
                                           + "' and companyname = '" + comboCompanyName.Text + "'");

                        while (Database.myreader.Read())
                        {

                            dataGridView1.Rows[i].Cells[DNoofPkg.Index].Value = Database.myreader["NoofPkg"].ToString();
                            dataGridView1.Rows[i].Cells[DCommodity.Index].Value = Database.myreader["Commodity"].ToString();
                            dataGridView1.Rows[i].Cells[DItemDesc.Index].Value = Database.myreader["ItemDesc"].ToString();
                            dataGridView1.Rows[i].Cells[DUOM.Index].Value = Database.myreader["Unit"].ToString();
                            dataGridView1.Rows[i].Cells[DOrderQty.Index].Value = Database.myreader["Qty"].ToString();
                            dataGridView1.Rows[i].Cells[DUnitPrice.Index].Value = Database.myreader["Rate"].ToString();
                            dataGridView1.Rows[i].Cells[DTotal.Index].Value = Database.myreader["Amount"].ToString();
                            dataGridView1.Rows[i].Cells[DNetWt.Index].Value = Database.myreader["NetWt"].ToString();
                            dataGridView1.Rows[i].Cells[DGrossWt.Index].Value = Database.myreader["GrossWt"].ToString();
                            dataGridView1.Rows[i].Cells[DTrWt.Index].Value = Database.myreader["TrWt"].ToString();
                            dataGridView1.Rows[i].Cells[DQtyRemarks.Index].Value = Database.myreader["Qtyremarks"].ToString();
                            dataGridView1.Rows[i].Cells[DtCommodityRemarks.Index].Value = Database.myreader["CommodityRemarks"].ToString();
                            dataGridView1.Rows[i].Cells[DPkgQty.Index].Value = Database.myreader["PkgQty"].ToString();
                            dataGridView1.Rows[i].Cells[DPKgUnit.Index].Value = Database.myreader["PkgUnit"].ToString();
                            dataGridView1.Rows[i].Cells[DtDeptt.Index].Value = Database.myreader["ItemDeptt"].ToString();

                            dataGridView1.Rows[i].Cells[ColHSNCode.Index].Value = Database.myreader["HSNCODE"].ToString();
                            dataGridView1.Rows[i].Cells[colCGSTRate.Index].Value = Database.myreader["CGSTRate"].ToString();
                            dataGridView1.Rows[i].Cells[colCGSTAmt.Index].Value = Database.myreader["CGSTAmt"].ToString();
                            dataGridView1.Rows[i].Cells[ColSGSTRate.Index].Value = Database.myreader["SGSTRate"].ToString();
                            dataGridView1.Rows[i].Cells[ColSGSTAMT.Index].Value = Database.myreader["SGSTAMT"].ToString();
                            dataGridView1.Rows[i].Cells[ColIGSTRate.Index].Value = Database.myreader["IGSTRate"].ToString();
                            dataGridView1.Rows[i].Cells[ColIGSTAmt.Index].Value = Database.myreader["IGSTAmt"].ToString();



                            UpdateGrossWt += Convert.ToDouble(Database.myreader["GrossWt"].ToString());
                            UpdatedTotalAmt += Convert.ToDouble(Database.myreader["Amount"].ToString());

                            i++;
                        }
                        Database.myreader.Close();
                        Database.Closeconnection();
                        comboCurrency.Text = Currency;
                        if (!comboBuyerName.Items.Contains(companyname))
                            comboBuyerName.Items.Add(companyname);
                        comboBuyerName.Text = companyname;

                        if (IsEPCGNo)
                            checkEPCG.Checked = true;
                        else
                            checkEPCG.Checked = false;

                        if (IsDirectExport)
                            checkDirectExport.Checked = true;
                        else
                            checkDirectExport.Checked = false;

                        comboEPCGNo.Text = EPCGNo;


                    }
                    textCVD.Text = textTCS.Text;
                }
            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
            }

        }

        private void btnInitliaze_Click(object sender, EventArgs e)
        {
            checkTrading.Checked = false;
            //IsUpdate = false;
            TotalCount = 0;
            textAddVat.Text = "0";
            textBoxLRNo.Text = "";
            textBuyerOrderNo.Text = "";
            textDelChallanNo.Text = "";
            textDesPatchNo.Text = "";
            textEduCess.Text = "0";
            if (DateTime.Now > Convert.ToDateTime("2017-06-30"))
            {
                textExDuty.Text = "0";
                textSecHiEdu.Text = "0";
            }
            else
            {
                textExDuty.Text = "12.5";
                textSecHiEdu.Text = "1";
            }
            textInvNo.Text = "";
            textTermsofDelivery.Text = "";

            textTermsofPayment.Text = "";
            textVat.Text = "0";
            checkCommercialInvoice.Checked = false;
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
            checkCommercialInvoice.Checked = false;
            textPkgsDetails.Text = "";
            string Remarks = "";
            if (Database.OpenConnection(Utility.DespacthConnectionString))
            {
                Database.myreader = Database.GetExecuteReaderCommand("select top 1 remarks from PackingRemarks order by Id Desc");
                if (Database.myreader.Read())
                {
                    Remarks = Database.myreader[0].ToString();
                }
                Database.myreader.Close();
            }
            textPkgsDetails.Text = Remarks;
        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            if (!FrmMain.IsUpdateExportInvoice)
                MessageBox.Show("You have no right to update export Invoice");
            else if (!(textTermsofPayment.Items.Contains(textTermsofPayment.Text.Trim())))
                MessageBox.Show("Please select proper payment terms or enter payment term in payment master");
            else
            {
                if (DespatchCommonFunction.IsAccountingDone(comboCompanyName.Text, textInvNo.Text, textARESuffix.Text))
                {
                    MessageBox.Show("Accounting Entry is done for the selected Invoice.\nPlease Contact Account Administrator");
                    return;
                }

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
                if (textBuyerOrderNo.Text != "")
                {
                    if (dateTimeBuyerOrderNo.Text == "01/01/1900" || dateTimeBuyerOrderNo.Text == "")
                        IsWrongDate = true;
                }

                if (!IsValidBuyerName)
                    MessageBox.Show(" Invalid Buyer Name");
                else if (IsWrongDate)
                    MessageBox.Show("Buyer Order Date is not Correct Format");
                else
                {
                    if (!FrmMain.IsUpdateExportInvoice)
                        MessageBox.Show("You do not have right to update Export Invoice");
                    else
                    {
                        int VenCnt = DespatchCommonFunction.CustomerLedgerName(comboBuyerName.Text, comboCompanyName.Text);
                        if (VenCnt == 0)
                        {
                            MessageBox.Show("No Account Ledger Found for this customer.\nPlease Ask Account Dept to create Ledger for this customer:- [" + comboBuyerName.Text + "]");
                            return;
                        }
                        double total = 0;
                        double ChangeGrossWt = 0;
                        bool IsWt = false;

                        try
                        {
                            total = Calculation(DOrderQty.Index, DUnitPrice.Index, DTotal.Index);
                            IsWt = WeightDifferences(DNetWt.Index, DGrossWt.Index);
                            string InsuranceApp = "";
                            string Insurance = "";
                            if (Database.OpenConnection(Utility.DespacthConnectionString))
                            {
                                string CheckInsurane = "";
                                double TotalAmt = 0;
                                Database.myreader = Database.GetExecuteReaderCommand("select Totalamt,CheckInsurance from Exportinvoice where Invno = '"
                                    + textInvNo.Text + "' and aresuffix = '" + textARESuffix.Text
                                    + "' and companyname = '" + comboCompanyName.Text + "'");
                                if (Database.myreader.Read())
                                {
                                    TotalAmt = Convert.ToDouble(Database.myreader[0].ToString());
                                    CheckInsurane = Database.myreader[1].ToString();
                                }
                                Database.myreader.Close();

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

                                Database.myreader = Database.GetExecuteReaderCommand("Select * from ExportInvItem where InvNo = '"
                                                 + textInvNo.Text + "' and yr = '"
                                                 + textARESuffix.Text + " ' and companyname = '"
                                                 + comboCompanyName.Text + "'");
                                if (Database.myreader.Read())
                                {
                                    UpdateGrossWt = Convert.ToDouble(Database.myreader["GrossWt"].ToString());
                                    UpdatedTotalAmt = Convert.ToDouble(Database.myreader["AMount"].ToString());
                                }
                                Database.myreader.Close();

                                Database.myreader = Database.GetExecuteReaderCommand("select * from AdvLicense where commodity ='" +
                                                      dataGridView1.Rows[0].Cells[DCommodity.Index].Value.ToString() + "' and advlicenseno = '"
                                                      + comboAdvLicenseNo.Text + "' and companyname = '"
                                                      + comboCompanyName.Text + "'");
                                while (Database.myreader.Read())
                                    dateTimePickerAdvLicenseDate.Value = Convert.ToDateTime(Database.myreader[8].ToString());
                                Database.myreader.Close();

                                Database.Closeconnection();
                            }

                            if (textSpDuty1Amt.Text == "")
                                textSpDuty1Amt.Text = "0";
                            if (textSpDuty2Amt.Text == "")
                                textSpDuty2Amt.Text = "0";
                            if (textSpDuty3Amt.Text == "")
                                textSpDuty3Amt.Text = "0";

                            string iscommercial = "";
                            string commercialinvoice = "";

                            if (checkCommercialInvoice.Checked)
                            {
                                iscommercial = "yes";
                                commercialinvoice = comboCommercialInvoice.Text;
                            }
                            else
                                iscommercial = "no";

                            total = Math.Round(total, 0);

                            double temptotal = total;
                            total = Math.Round(total, 0);
                            double Excise = 0;
                            double Educess = 0;
                            double HiEducess = 0;
                            double Vat = 0;
                            double AddVat = 0;
                            if (dateTimePickerInvDate.Value < Convert.ToDateTime("2017-07-01 00:00:00"))
                            {

                                if (checkTrading.Checked)
                                {
                                    Excise = Convert.ToDouble(textExciseAmt.Text);
                                    Educess = Convert.ToDouble(textEduAmt.Text);
                                    HiEducess = Convert.ToDouble(textSecHieduAmt.Text);
                                    Vat = Convert.ToDouble(textVatAmt.Text);
                                    AddVat = Convert.ToDouble(textAddVatAmt.Text);
                                }
                                else
                                {

                                    Excise = (total * Convert.ToDouble(textExDuty.Text)) / 100;
                                    if (Convert.ToDouble(textCVD.Text) > 0)
                                    {
                                        textTCS.Text = textCVD.Text;
                                        TCS = ((total + Excise) * Convert.ToDouble(textCVD.Text)) / 100;
                                        TCS = Math.Round(TCS, 0);
                                    }
                                    Excise = Math.Round(Excise, 0);

                                    Educess = ((Convert.ToDouble(textEduCess.Text) * (Excise + TCS)) / 100);
                                    HiEducess = ((Convert.ToDouble(textSecHiEdu.Text) * (Excise + TCS)) / 100);
                                    Educess = Math.Round(Educess, 0);
                                    HiEducess = Math.Round(HiEducess, 0);


                                    Vat = ((total + Excise + Educess + HiEducess) * Convert.ToDouble(textVat.Text)) / 100;
                                    AddVat = ((total + Excise + Educess + HiEducess) * Convert.ToDouble(textAddVat.Text)) / 100;

                                    Vat = Math.Round(Vat, 0);
                                    AddVat = Math.Round(AddVat, 0);

                                }
                                //if (comboTaxApp.SelectedIndex == 1)
                                //    total = total + Educess + HiEducess + Vat + AddVat + Excise;

                                if (comboCompanyName.Text == "Oswal Extrusion Limited (Unit-II)")
                                {
                                    Vat = ((total) * Convert.ToDouble(textVat.Text)) / 100;
                                    AddVat = ((total) * Convert.ToDouble(textAddVat.Text)) / 100;

                                    Vat = Math.Round(Vat, 0);
                                    AddVat = Math.Round(AddVat, 0);
                                    total = total + Vat + AddVat;
                                    temptotal = Convert.ToDouble(textGrdTotal.Text.ToString());
                                }
                            }


                            Friegth = Convert.ToDouble(textFrieght.Text);
                            total = total + Friegth;

                            double spduty1 = Convert.ToDouble(textSpDuty1Amt.Text);
                            double spduty2 = Convert.ToDouble(textSpDuty2Amt.Text);
                            double spduty3 = Convert.ToDouble(textSpDuty3Amt.Text);

                            total = total + spduty1 + spduty2 + spduty3;

                            total = Math.Round(total, 0, MidpointRounding.AwayFromZero);
                            //total = Math.Round(total, 0);

                            textGrdTotal.Text = total.ToString();

                            double TempTotal = 0;
                            string commodity = "";

                            for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                            {
                                if (dataGridView1.Rows[i].Cells[DNoofPkg.Index].FormattedValue.ToString() != "" &&
                                    dataGridView1.Rows[i].Cells[DPkgQty.Index].FormattedValue.ToString() != "" &&
                                    dataGridView1.Rows[i].Cells[DItemDesc.Index].FormattedValue.ToString() != "" &&
                                    dataGridView1.Rows[i].Cells[DUOM.Index].FormattedValue.ToString() != "")
                                {

                                    TempTotal += Convert.ToDouble(dataGridView1.Rows[i].Cells[DTotal.Index].Value.ToString());
                                    TempTotal = Math.Round(TempTotal, 0);
                                    commodity += dataGridView1.Rows[i].Cells[DCommodity.Index].Value.ToString() + ",";
                                }
                            }

                            bool IsAlreadyPrint = false;
                            Database.OpenConnection(Utility.DespacthConnectionString);
                            Database.myreader = Database.GetExecuteReaderCommand("select * from printExportinvoice where InvNo = '"
                                + textInvNo.Text + "' and yr  = '" + textARESuffix.Text + "' and companyname = '"
                                + comboCompanyName.Text + "'");
                            if (Database.myreader.Read())
                                IsAlreadyPrint = true;
                            Database.myreader.Close();
                            if (IsAlreadyPrint)
                                MessageBox.Show("Following Invoice No is already Printed you cannot change Invoice");
                            else if (TotalCount <= 0)
                                MessageBox.Show("Please Select Items from Item Information");
                            else if (IsWt)
                                MessageBox.Show("Net Weight should be lesser than Gross Weight");
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
                                        string EPCGNo = "";
                                        string AdvNo = "";
                                        double Amount = 0;
                                        double GrossWt = 0;
                                        Database.myreader = Database.GetExecuteReaderCommand("Select EPCGNo,AdvNo from ExportInvoice where Invno ='"
                                                  + textInvNo.Text + "' and aresuffix = '"
                                                  + textARESuffix.Text + " ' and companyname = '"
                                                  + comboCompanyName.Text + "'");
                                        if (Database.myreader.Read())
                                        {
                                            EPCGNo = Database.myreader[0].ToString();
                                            AdvNo = Database.myreader[1].ToString();
                                        }
                                        Database.myreader.Close();

                                        Database.myreader = Database.GetExecuteReaderCommand("Select amount,Currency from ExportInvItem where Invno ='"
                                                  + textInvNo.Text + "' and yr = '"
                                                  + textARESuffix.Text + " ' and companyname = '"
                                                  + comboCompanyName.Text + "'");
                                        while (Database.myreader.Read())
                                            Amount += Convert.ToDouble(Database.myreader[0].ToString()) *
                                                      Convert.ToDouble(Database.myreader[1].ToString());
                                        Database.myreader.Close();
                                        UpdateEPCG_PendingLimit(EPCGNo, -Amount);



                                        //     Database.OpenConnection(Utility.DespacthConnectionString);
                                        Database.myreader = Database.GetExecuteReaderCommand("Select Grosswt from ExportInvItem where Invno ='"
                                               + textInvNo.Text + "' and yr = '"
                                               + textARESuffix.Text + " ' and companyname = '"
                                               + comboCompanyName.Text + "'");
                                        while (Database.myreader.Read())
                                            GrossWt += Convert.ToDouble(Database.myreader[0].ToString());
                                        Database.myreader.Close();
                                        DespatchCommonFunction.UpdateAdvLic_PendingLimit(comboCompanyName.Text, AdvNo, -GrossWt);

                                        //     Database.OpenConnection(Utility.DespacthConnectionString);
                                        Database.GetExecuteNonQueryCommand("Update ExportInvoice set InvDate = '" + dateTimePickerInvDate.Value.ToString(DespatchCommonFunction.ERPDBDateSettings()) + "',"
                                                                                   + " TermsofPayment ='" + textTermsofPayment.Text + "',"
                                                                                   + " DespatchNo = '" + textDesPatchNo.Text + "',"
                                                                                   + " DespatchDate = '" + dateTimePickerDespatchDate.Value.ToString(DespatchCommonFunction.ERPDBDateSettings()) + "',"
                                                                                   + " VehicalNo = '" + comboVehicalNo.Text + "',"
                                                                                   + " Destination = '" + comboDestination.Text + "',"
                                                                                   + " TermsofDelivery = '" + textTermsofDelivery.Text + "',"
                                                                                   + " Exduty = " + textExDuty.Text + ","
                                                                                   + " Educess = " + textEduCess.Text + ","
                                                                                   + " SecHiEduCess = " + textSecHiEdu.Text + ","
                                                                                   + " vat = " + textVat.Text + ","
                                                                                   + " Addvat = " + textAddVat.Text + ","
                                                                                   + " ExciseAmt =" + Excise.ToString() + ","
                                                                                   + " EduCessAmt = " + Educess.ToString() + ","
                                                                                   + " SecHiEduCessAmt = " + HiEducess.ToString() + ","
                                                                                   + " VatAmt = " + Vat.ToString() + ","
                                                                                   + " AddVatAmt =" + AddVat.ToString() + ","
                                                                                   + " TotalAmt = " + textGrdTotal.Text + ","
                                                                                   + " InvDateTime = '" + dateTimePickerInvoice.Value.ToString(DespatchCommonFunction.ERPDBDateSettings()) + "',"
                                                                                   + " RemDateTime = '" + dateTimePickerDateTimeRemoval.Value.ToString(DespatchCommonFunction.ERPDBDateSettings()) + "',"
                                                                                   + " DelChallanNo = '" + textDelChallanNo.Text + "',"
                                                                                   + " DelChallanDate = '" + dateTimePickerChallanDate.Value.ToString(DespatchCommonFunction.ERPDBDateSettings()) + "',"
                                                                                   + " LRNo = '" + textBoxLRNo.Text + "',"
                                                                                   + " LRDate = '" + dateTimePickerLRNo.Value.ToString(DespatchCommonFunction.ERPDBDateSettings()) + "',"
                                                                                   + " EPCGNo = '" + comboEPCGNo.Text + "',"
                                                                                   + " EPCGDate = '" + dateTimePickerEPCGDate.Value.ToString(DespatchCommonFunction.ERPDBDateSettings()) + "',"
                                                                                   + " AdvNo = '" + comboAdvLicenseNo.Text + "',"
                                                                                   + " AdvDate = '" + dateTimePickerAdvLicenseDate.Value.ToString(DespatchCommonFunction.ERPDBDateSettings()) + "',"
                                                                                   + " ExciseNo = '" + textExciseNotification.Text + "',"
                                                                                   + " ExciseDate = '" + dateTimePickerExciseDate.Value.ToString(DespatchCommonFunction.ERPDBDateSettings()) + "', "
                                                                                   + " TotalAmtWord = '" + Utility.IntegerToWords(Totals) + "',"
                                                                                   + " TransportName = '" + comboTransporter.Text + "',"
                                                                                   + " DutyRemarks = '" + textDutyRemarks.Text + "',"
                                                                                   + " VatRemarks = '" + textVatRemarks.Text + "',"
                                                                                   + " EntryNo = '" + textEntryNo.Text + "',"
                                                                                   + " Note = '" + comboNote.Text + "',"
                                                                                   + " CenvatAcc = '" + textCENVATNo.Text + "',"
                                                                                   + " ARESuffix = '" + textARESuffix.Text + "',"
                                                                                   + " AppCompName = '" + textAppCompanyName.Text + "',"
                                                                                   + " DestPlace = '" + textDestinationPlace.Text + "',"
                                                                                   + " Remarks = '" + textRemarks.Text + "',"
                                                                                   + " Frieght = " + textFrieght.Text + ","
                                                                                   + " TCSPer = " + textTCS.Text + ","
                                                                                   + " TCSAmt = " + TCS + ","
                                                                                   + " TypeVat = '" + comboType.Text
                                                                                   + "',BuyerName = '" + comboBuyerName.Text
                                                                                   + "',BuyerOrderno = '" + textBuyerOrderNo.Text
                                                                                   + "',THeading = '" + textHeading.Text
                                                                                   + "',accessableinwords = '" + Utility.IntegerToWords(Convert.ToInt64(TempTotal))
                                                                                   + "',AccountHead = '" + comboAccountHead.Text
                                                                                   + "',PkgsDetails = '" + textPkgsDetails.Text
                                                                                   + "',Notifier = '" + textNotifier.Text
                                                                                   + "',iscommercial = '" + iscommercial + "',commercialInvno = '"
                                                                                   + commercialinvoice + "',CheckInsurance = '"
                                                                                   + InsuranceApp + "',Insurance = '" + Insurance + "'  where Invno ='" + textInvNo.Text + "' and aresuffix = '"
                                                  + textARESuffix.Text + " ' and companyname = '"
                                                  + comboCompanyName.Text + "'");


                                        if (textBuyerOrderNo.Text != "")
                                        {
                                            Database.GetExecuteNonQueryCommand("update ExportInvoice set BuyerOrderNo = '" + textBuyerOrderNo.Text + "',"
                                                                                + "BuyerDate = '" + dateTimeBuyerOrderNo.Value.ToString(DespatchCommonFunction.ERPDBDateSettings()) + "'"
                                                                              + " where Invno ='" + textInvNo.Text + "' and aresuffix = '"
                                               + textARESuffix.Text + " ' and companyname = '" + comboCompanyName.Text + "'");
                                        }
                                        else
                                        {
                                            Database.GetExecuteNonQueryCommand("update ExportInvoice set BuyerOrderNo = '" + textBuyerOrderNo.Text + "',"
                                                                               + " BuyerDate = null"
                                                                             + " where Invno ='" + textInvNo.Text + "' and aresuffix = '"
                                                                             + textARESuffix.Text + " ' and companyname = '" + comboCompanyName.Text + "'");
                                        }

                                        if (checkAvailBenefit.Checked)
                                            Database.GetExecuteNonQueryCommand("Update ExportInvoice set DetailsExport = '"
                                            + textUTBondDetails.Text + "',notifier = '" + textNotifier.Text
                                            + "' where Invno ='" + textInvNo.Text + "' and aresuffix = '"
                                               + textARESuffix.Text + " ' and companyname = '"
                                               + comboCompanyName.Text + "'");
                                        else
                                            Database.GetExecuteNonQueryCommand("Update ExportInvoice set DetailsExport = '' where Invno ='"
                                                + textInvNo.Text + "' and aresuffix = '"
                                                    + textARESuffix.Text + " ' and companyname = '"
                                                    + comboCompanyName.Text + "'");


                                        if (checkImport.Checked)
                                            Database.GetExecuteNonQueryCommand("Update ExportInvoice set ImportMaterial = 'yes' where Invno ='"
                                                + textInvNo.Text + "' and aresuffix = '"
                                                  + textARESuffix.Text + " ' and companyname = '"
                                                  + comboCompanyName.Text + "'");
                                        else
                                            Database.GetExecuteNonQueryCommand("Update ExportInvoice set ImportMaterial = 'no' where Invno ='"
                                                + textInvNo.Text + "' and aresuffix = '"
                                                  + textARESuffix.Text + " ' and companyname = '"
                                                  + comboCompanyName.Text + "'");

                                        if (checkEPCG.Checked)
                                            Database.GetExecuteNonQueryCommand("Update ExportInvoice set EPCGLic = 'yes' where Invno ='"
                                                + textInvNo.Text + "' and aresuffix = '"
                                                  + textARESuffix.Text + " ' and companyname = '"
                                                  + comboCompanyName.Text + "'");
                                        else
                                        {
                                            Database.GetExecuteNonQueryCommand("Update ExportInvoice set EPCGNo = '' where Invno ='"
                                                + textInvNo.Text + "' and aresuffix = '"
                                                  + textARESuffix.Text + " ' and companyname = '"
                                                  + comboCompanyName.Text + "'");

                                            Database.GetExecuteNonQueryCommand("Update ExportInvoice set EPCGLic = 'no' where Invno ='"
                                                + textInvNo.Text + "' and aresuffix = '"
                                                  + textARESuffix.Text + " ' and companyname = '"
                                                  + comboCompanyName.Text + "'");
                                        }

                                        if (checkAdvanceLic.Checked)
                                            Database.GetExecuteNonQueryCommand("Update ExportInvoice set AdvanceLic = 'yes' where Invno ='"
                                                + textInvNo.Text + "' and aresuffix = '"
                                                  + textARESuffix.Text + " ' and companyname = '"
                                                  + comboCompanyName.Text + "'");
                                        else
                                        {

                                            Database.GetExecuteNonQueryCommand("Update ExportInvoice set AdvNo = '' where Invno ='"
                                             + textInvNo.Text + "' and aresuffix = '"
                                               + textARESuffix.Text + " ' and companyname = '"
                                               + comboCompanyName.Text + "'");

                                            Database.GetExecuteNonQueryCommand("Update ExportInvoice set AdvanceLic = 'no' where Invno ='"
                                                + textInvNo.Text + "' and aresuffix = '"
                                                  + textARESuffix.Text + " ' and companyname = '"
                                                  + comboCompanyName.Text + "'");
                                        }

                                        if (checkDFIA.Checked)
                                        {
                                            Database.GetExecuteNonQueryCommand("Update ExportInvoice set IsDFIA = 'yes' where Invno ='"
                                                + textInvNo.Text + "' and aresuffix = '"
                                                  + textARESuffix.Text + " ' and companyname = '"
                                                  + comboCompanyName.Text + "'");
                                            Database.GetExecuteNonQueryCommand("Update ExportInvoice set AdvNo = '" + comboAdvLicenseNo.Text + "' where Invno ='"
                                            + textInvNo.Text + "' and aresuffix = '"
                                              + textARESuffix.Text + " ' and companyname = '"
                                              + comboCompanyName.Text + "'");

                                        }
                                        else
                                        {
                                            if (checkAdvanceLic.Checked == false)
                                            {
                                                Database.GetExecuteNonQueryCommand("Update ExportInvoice set AdvNo = '' where Invno ='"
                                                 + textInvNo.Text + "' and aresuffix = '"
                                                   + textARESuffix.Text + " ' and companyname = '"
                                                   + comboCompanyName.Text + "'");
                                            }
                                            Database.GetExecuteNonQueryCommand("Update ExportInvoice set IsDFIA = 'no' where Invno ='"
                                                + textInvNo.Text + "' and aresuffix = '"
                                                  + textARESuffix.Text + " ' and companyname = '"
                                                  + comboCompanyName.Text + "'");
                                        }

                                        if (checkCENVAT.Checked)
                                            Database.GetExecuteNonQueryCommand("Update ExportInvoice set IsCenvat = 'yes',Cenvatrg23 = '" +
                                                     textRG23A.Text + "' where Invno ='"
                                                   + textInvNo.Text + "' and aresuffix = '"
                                                     + textARESuffix.Text + " ' and companyname = '"
                                                     + comboCompanyName.Text + "'");
                                        else
                                            Database.GetExecuteNonQueryCommand("Update ExportInvoice set IsCenvat = 'no' where Invno ='"
                                                + textInvNo.Text + "' and aresuffix = '"
                                                  + textARESuffix.Text + " ' and companyname = '"
                                                  + comboCompanyName.Text + "'");


                                        if (checkTrading.Checked)
                                            Database.GetExecuteNonQueryCommand("Update ExportInvoice set Trading = 'Trading Invoice' where Invno ='"
                                                   + textInvNo.Text + "' and aresuffix = '"
                                                     + textARESuffix.Text + " ' and companyname = '"
                                                     + comboCompanyName.Text + "'");
                                        else
                                            Database.GetExecuteNonQueryCommand("Update ExportInvoice set Trading = '' where Invno ='"
                                                + textInvNo.Text + "' and aresuffix = '"
                                                  + textARESuffix.Text + " ' and companyname = '"
                                                  + comboCompanyName.Text + "'");


                                        Database.GetExecuteNonQueryCommand("Update ExportInvoice set SpDuty1 = '" + textSpDutyRemarks1.Text
                                              + "',SpDutyAmt = " + textSpDuty1Amt.Text
                                              + ",SpDuty2 = '" + textSpDutyRemark2.Text
                                              + "',SpDuty2Amt = " + textSpDuty2Amt.Text
                                              + ",SpDuty3 = '" + textSpDutyRemarks3.Text
                                              + "',SpDuty3Amt = " + textSpDuty3Amt.Text
                                            // + ",buyerdate = '" + dateTimeBuyerOrderNo.Text
                                              + " where Invno ='"
                                                + textInvNo.Text + "' and aresuffix = '"
                                                  + textARESuffix.Text + " ' and companyname = '"
                                                  + comboCompanyName.Text + "'");


                                        if (comboBeingExport.SelectedIndex == 1) //UtBond
                                        {

                                            string str = "Update ExportInvoice set UT1BondNo = '" + textUT1Bond.Text
                                                + "',BeingExport = '" + comboBeingExport.Text
                                                + "', UT1BondDate = '" + dateTimePickerUT1Bond.Value.ToString(DespatchCommonFunction.ERPDBDateSettings())
                                                + "',ExpiryDate = '" + dateTimeUTBondExpiryDate.Value.ToString(DespatchCommonFunction.ERPDBDateSettings())
                                                + "'where Invno ='" + textInvNo.Text + "' and aresuffix = '"
                                                 + textARESuffix.Text + "' and companyname = '"
                                                 + comboCompanyName.Text + "'";

                                            Database.GetExecuteNonQueryCommand(str);


                                        }
                                        else
                                        {
                                            string str = "Update ExportInvoice set UT1BondNo = '',BeingExport = '" + comboBeingExport.Text
                                                + "',UT1BondDate = null,ExpiryDate = null where Invno ='" + textInvNo.Text + "' and aresuffix = '"
                                                 + textARESuffix.Text + "' and companyname = '"
                                                 + comboCompanyName.Text + "'";

                                            Database.GetExecuteNonQueryCommand(str);

                                        }

                                        if (comboTaxApp.SelectedIndex == 1)
                                        {
                                            Int64 excises = Convert.ToInt64(Excise + Educess + HiEducess);
                                            string ExciseStr = Utility.IntegerToWords(excises);
                                            Database.GetExecuteNonQueryCommand("Update ExportInvoice set ExciseAmtWord = '" +
                                                  ExciseStr + "' where Invno ='" + textInvNo.Text + "' and aresuffix = '"
                                                  + textARESuffix.Text + " ' and companyname = '"
                                                  + comboCompanyName.Text + "'");
                                        }
                                        else
                                        {
                                            Database.GetExecuteNonQueryCommand("Update ExportInvoice set ExciseAmtWord = 'NIL' where Invno ='" + textInvNo.Text + "' and aresuffix = '"
                                                 + textARESuffix.Text + " ' and companyname = '"
                                                 + comboCompanyName.Text + "'");
                                        }


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


                                        Int64 ExciseAmt = Convert.ToInt64(Excise + Educess + HiEducess);
                                        string ExciseString = Utility.IntegerToWords(ExciseAmt);
                                        Database.GetExecuteNonQueryCommand("Update ExportInvoice set ExcieseAmtWords = '" +
                                                            ExciseString + "', Taxapp ='" + comboTaxApp.Text + "',currency = '" + comboCurrency.Text
                                                            + "',Currencyvalue = '" + comboCurrencyValue.Text +
                                                            "',ModeTransport = '" + comboModeTransport.Text +
                                                            "',NoModeTransport = '" + st +
                                                            "',AreDetails = '" + textAREDetail.Text +
                                                            "',Aredetails1 = '" + textAREDetails1.Text +
                                                            "',modes = '" + st1 +
                                                            "',deptt = '" + comboDeptt.Text +
                                                            "',consignee = '" + textConsignee.Text +
                                                            "'where Invno ='" + textInvNo.Text + "' and aresuffix = '"
                                                            + textARESuffix.Text + " ' and companyname = '"
                                                            + comboCompanyName.Text + "'");

                                        if (checkDirectExport.Checked)
                                        {
                                            Database.GetExecuteNonQueryCommand("Update ExportInvoice set PackingListNo = '" +
                                                     textPackingListNo.Text + "',ContainerNo = '" + textContNo.Text
                                                     + "',BottleSealNo = '" + textBottleSealNo.Text + "'"
                                                     + ",DirectExport = 'yes'"
                                                     + " where Invno ='" + textInvNo.Text + "' and aresuffix = '"
                                                  + textARESuffix.Text + " ' and companyname = '"
                                                  + comboCompanyName.Text + "'");
                                        }

                                        Database.GetExecuteNonQueryCommand("Update ExportInvoice set PackageNo = '" +
                                              textPackages.Text + "' where Invno ='" + textInvNo.Text + "' and aresuffix = '"
                                                  + textARESuffix.Text + " ' and companyname = '"
                                                  + comboCompanyName.Text + "'");
                                        if (checkDFIA.Checked)
                                        {
                                            Database.GetExecuteNonQueryCommand("Update ExportInvoice set IsDFIA = 'yes' "
                                              + " where Invno ='" + textInvNo.Text + "' and aresuffix = '"
                                                 + textARESuffix.Text + " ' and companyname = '"
                                                 + comboCompanyName.Text + "'");

                                        }
                                        else
                                            Database.GetExecuteNonQueryCommand("Update ExportInvoice set IsDFIA = 'no'" +
                                              " where Invno ='" + textInvNo.Text + "' and aresuffix = '"
                                                 + textARESuffix.Text + " ' and companyname = '"
                                                 + comboCompanyName.Text + "'");


                                        //   Database.OpenConnection(Utility.DespacthConnectionString);
                                        Database.GetExecuteNonQueryCommand("delete from ExportInvItem where Invno ='" + textInvNo.Text + "' and yr = '"
                                                 + textARESuffix.Text + " ' and companyname = '"
                                                 + comboCompanyName.Text + "'");

                                        double Currency = GetCurrencyConversion();
                                        Database.myadapter = Database.GetAdapterCommand("select * from ExportInvItem WHERE 1=2");
                                        DataSet dataset1 = new DataSet();
                                        Database.myadapter.Fill(dataset1, "ExportInvItem");
                                        for (int i = 0; i < TotalCount; i++)
                                        {
                                            //     if (Database.OpenConnection_Adapter(Utility.DespacthConnectionString))
                                            {


                                                double NetWt = Convert.ToDouble(dataGridView1.Rows[i].Cells[DNetWt.Index].Value.ToString());
                                                double OrderQty = Convert.ToDouble(dataGridView1.Rows[i].Cells[DOrderQty.Index].Value.ToString());



                                                //insert data in database 
                                                DataRow dr = dataset1.Tables[0].NewRow();
                                                dr[0] = textInvNo.Text;

                                                if (dataGridView1.Rows[i].Cells[DNoofPkg.Index].FormattedValue.ToString() != "")
                                                    dr[1] = dataGridView1.Rows[i].Cells[DNoofPkg.Index].Value.ToString();

                                                if (dataGridView1.Rows[i].Cells[DItemDesc.Index].FormattedValue.ToString() != "")
                                                    dr[2] = dataGridView1.Rows[i].Cells[DItemDesc.Index].Value.ToString();

                                                if (dataGridView1.Rows[i].Cells[DOrderQty.Index].FormattedValue.ToString() != "")
                                                    dr[3] = dataGridView1.Rows[i].Cells[DOrderQty.Index].Value.ToString();

                                                if (dataGridView1.Rows[i].Cells[DUnitPrice.Index].FormattedValue.ToString() != "")
                                                    dr[4] = dataGridView1.Rows[i].Cells[DUnitPrice.Index].Value.ToString();

                                                if (dataGridView1.Rows[i].Cells[DUOM.Index].FormattedValue.ToString() != "")
                                                    dr[5] = dataGridView1.Rows[i].Cells[DUOM.Index].Value.ToString();

                                                if (dataGridView1.Rows[i].Cells[DTotal.Index].FormattedValue.ToString() != "")
                                                    dr[6] = dataGridView1.Rows[i].Cells[DTotal.Index].Value.ToString();
                                                if (dataGridView1.Rows[i].Cells[DNetWt.Index].FormattedValue.ToString() != "")
                                                    dr[7] = dataGridView1.Rows[i].Cells[DNetWt.Index].Value.ToString();
                                                if (dataGridView1.Rows[i].Cells[DGrossWt.Index].FormattedValue.ToString() != "")
                                                    dr[8] = dataGridView1.Rows[i].Cells[DGrossWt.Index].Value.ToString();

                                                dr[9] = "";
                                                dr[10] = FrmMain.UserName;

                                                dr[11] = dataGridView1.Rows[i].Cells[DCommodity.Index].Value.ToString(); ; //commodity

                                                dr[12] = textARESuffix.Text;

                                                if (dataGridView1.Rows[i].Cells[DTrWt.Index].FormattedValue.ToString() != "")
                                                    dr[13] = dataGridView1.Rows[i].Cells[DTrWt.Index].Value.ToString();

                                                if (dataGridView1.Rows[i].Cells[DQtyRemarks.Index].FormattedValue.ToString() != "")
                                                    dr[14] = dataGridView1.Rows[i].Cells[DQtyRemarks.Index].Value.ToString();



                                                dr[15] = comboCompanyName.Text;
                                                if (dataGridView1.Rows[i].Cells[DtCommodityRemarks.Index].FormattedValue.ToString() != "")
                                                    dr[16] = dataGridView1.Rows[i].Cells[DtCommodityRemarks.Index].Value.ToString().ToUpper();

                                                if (dataGridView1.Rows[i].Cells[DPkgQty.Index].FormattedValue.ToString() != "")
                                                    dr[17] = dataGridView1.Rows[i].Cells[DPkgQty.Index].Value.ToString();
                                                if (dataGridView1.Rows[i].Cells[DPKgUnit.Index].FormattedValue.ToString() != "")
                                                    dr[18] = dataGridView1.Rows[i].Cells[DPKgUnit.Index].Value.ToString();

                                                dr[19] = Currency;
                                                if (dataGridView1.Rows[i].Cells[DtDeptt.Index].FormattedValue.ToString() != "")
                                                    dr[21] = dataGridView1.Rows[i].Cells[DtDeptt.Index].Value.ToString();

                                                dr["HSNCode"] = dataGridView1.Rows[i].Cells[ColHSNCode.Index].Value.ToString();
                                                dr["CGSTrate"] = Convert.ToDouble(dataGridView1.Rows[i].Cells[colCGSTRate.Index].Value);
                                                dr["CGSTamt"] = Convert.ToDouble(dataGridView1.Rows[i].Cells[colCGSTAmt.Index].Value);
                                                dr["SGSTRate"] = Convert.ToDouble(dataGridView1.Rows[i].Cells[ColSGSTRate.Index].Value);
                                                dr["SGSTAmt"] = Convert.ToDouble(dataGridView1.Rows[i].Cells[ColSGSTAMT.Index].Value);
                                                dr["IGSTrate"] = Convert.ToDouble(dataGridView1.Rows[i].Cells[ColIGSTRate.Index].Value);
                                                dr["IGSTAmt"] = Convert.ToDouble(dataGridView1.Rows[i].Cells[ColIGSTAmt.Index].Value);

                                                dataset1.Tables[0].Rows.Add(dr);
                                                Database.mycmdBuilder = Database.GetCmdBuilderCommand(Database.myadapter);
                                                int x = Database.myadapter.Update(dataset1.Tables[0]);


                                                if (checkEPCG.Checked)
                                                    UpdateEPCG_PendingLimit(comboEPCGNo.Text, Convert.ToDouble(dataGridView1.Rows[i].Cells[DUnitPrice.Index].Value.ToString()) * Currency);
                                            }
                                        }

                                        Database.CommitTransaction();


                                        //Update Pending Limit from Advance License
                                        if (TotalCount == 1)
                                        {
                                            ChangeGrossWt = Convert.ToDouble(dataGridView1.Rows[0].Cells[DNetWt.Index].Value.ToString());
                                            if (checkAdvanceLic.Checked)
                                                DespatchCommonFunction.UpdateAdvLic_PendingLimit(comboCompanyName.Text, comboAdvLicenseNo.Text, ChangeGrossWt);
                                            //UpdateAdvLic_PendingLimit(comboAdvLicenseNo.Text, ChangeGrossWt - UpdateGrossWt);
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
                                        Database.Closeconnection();

                                        Utility.UserInformation("Update", "Export Invoice " + textInvNo.Text + " is updated successfully");
                                        MessageBox.Show("Invoice No " + textInvNo.Text + " is updated succesfully");
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
            //frmPrintExportInvoice frm = new frmPrintExportInvoice();
            //frm.ShowDialog();
            TabClass.OpenForm(new frmPrintExportInvoice());
        }



        //private void UpdateAdvLic_PendingLimit(string LicenseNo, double Limit)
        //{
        //    // if (Database.OpenConnection(Utility.DespacthConnectionString))
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

        //    }
        //}


        private void UpdateEPCG_PendingLimit(string LicenseNo, double LimitRs)
        {

            if (checkEPCG.Checked)
            {
                // if (Database.OpenConnection(Utility.DespacthConnectionString))
                {
                    double PendingLimit = 0;
                    double TotalLimit = 0;
                    Database.myreader = Database.GetExecuteReaderCommand("select * from EPCGMaster where EPCGLicNo = '"
                             + LicenseNo + "' and companyname = '" + comboCompanyName.Text + "'");
                    if (Database.myreader.Read())
                    {
                        PendingLimit = Convert.ToDouble(Database.myreader["PendingLimit"].ToString());
                        TotalLimit = Convert.ToDouble(Database.myreader["Limit"].ToString());
                    }

                    Database.myreader.Close();

                    {
                        Database.GetExecuteNonQueryCommand("update EPCGMaster set PendingLimit = " + (PendingLimit - LimitRs)
                        + " where EPCGLicNo = '" + LicenseNo + "' and companyname = '" + comboCompanyName.Text + "'");
                    }

                }
            }
        }



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
            for (int i = 0; i < TotalCount; i++)
            {

                {

                    double NetWt = 0;
                    for (int j = 0; j < TotalCount; j++)
                        NetWt += Convert.ToDouble(dataGridView1.Rows[j].Cells[10].Value.ToString());




                    ArrListSionNo.Clear();
                    ArrListImportItem.Clear();
                    ArrListImportItemWt.Clear();
                    ArrListImportItemUnit.Clear();

                    Database.myreader = Database.GetExecuteReaderCommand("select * from AdvLicItem where ExportItem = '"
                                                             + dataGridView1.Rows[i].Cells[DCommodity.Index].Value.ToString()
                                                             + "' and advlicense = '" + comboAdvLicenseNo.Text
                                                             + "' and companyname = '" + comboCompanyName.Text + "'");
                    while (Database.myreader.Read())
                    {


                        ExportITCHSNo = Database.myreader[0].ToString();


                        ArrListSionNo.Add(Database.myreader[1].ToString());
                        ArrListImportItem.Add(Database.myreader[4].ToString());
                        ArrListImportItemUnit.Add("KGS");
                        double ratio = Convert.ToDouble(Database.myreader[5].ToString());
                        double InputAllowed = Convert.ToDouble(Database.myreader[6].ToString());
                        double TotalWt = (ratio * InputAllowed * NetWt) / 100;
                        ArrListImportItemWt.Add(TotalWt);
                        // }
                    }
                    Database.myreader.Close();
                    //    Database.Closeconnection();
                }
            }
        }

        private void InsertValueInExportCntMaterial(string InvNo, string yr)
        {
            CalculateMaterialWt();
            // if (Database.OpenConnection(Utility.DespacthConnectionString))
            {
                int srno = 0;
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

                // Database.Closeconnection();
            }
        }


        private void Update_ValueInExportCntMaterial(string InvNo, string yr)
        {
            CalculateMaterialWt();
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

        private void comboBeingExport_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBeingExport.SelectedIndex == 0) // rebate
            {
                groupUTBond.Visible = false;
                textDutyRemarks.Text = "";
                comboTaxApp.SelectedIndex = 1;
            }
            else if (comboBeingExport.SelectedIndex == 1) //UT Bond
            {
                textDutyRemarks.Text = "Duty Foregone";
                groupUTBond.Visible = true;
                if (Database.OpenConnection(Utility.DespacthConnectionString))
                {
                    Database.myreader = Database.GetExecuteReaderCommand("Select * from UTBond where companyname = '" +
                       comboCompanyName.Text + "'order by BondDate");
                    while (Database.myreader.Read())
                    {
                        textUT1Bond.Text = Database.myreader[1].ToString();
                        dateTimePickerUT1Bond.Value = Convert.ToDateTime(Database.myreader[2].ToString());
                        dateTimeUTBondExpiryDate.Value = Convert.ToDateTime(Database.myreader[5].ToString());
                    }
                    Database.myreader.Close();
                    Database.Closeconnection();
                }
                comboTaxApp.SelectedIndex = 0;
            }
        }

        private void textEntryNo_KeyPress(object sender, KeyPressEventArgs e)
        {
            Utility.AllowDecimalOnly(e);
        }

        private void checkEPCG_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                if (checkEPCG.Checked)
                    groupEPCG.Visible = true;
                else
                    groupEPCG.Visible = false;

                if (comboCompanyName.Text.Trim() == "Plastene India Limited" && checkEPCG.Checked)
                {
                    comboEPCGNo.Text = "3730000146";

                }
            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
            }
        }

        private void checkAdvanceLic_CheckedChanged(object sender, EventArgs e)
        {
            if (checkAdvanceLic.Checked || checkDFIA.Checked)
            {
                groupAdvanceLic.Visible = true;
                //if (comboCompanyName.Text.Trim() == "Plastene India Limited")
                checkImport.Checked = true;
            }
            else
            {
                groupAdvanceLic.Visible = false;
                // if (comboCompanyName.Text.Trim() == "Plastene India Limited")
                checkImport.Checked = false;
            }
            if (comboCompanyName.Text == "Oswal Extrusion Limited (Unit-II)")
                comboTaxApp.SelectedIndex = 0;
        }

        private void checkDirectExport_CheckedChanged(object sender, EventArgs e)
        {
            if (checkDirectExport.Checked)
                groupDirectExport.Visible = true;
            else
                groupDirectExport.Visible = false;
        }

        private void checkDirectExport_CheckedChanged_1(object sender, EventArgs e)
        {
            if (comboCompanyName.Text.Trim() == "Plastene India Limited")
            {
                if (checkDirectExport.Checked)
                    comboEPCGNo.Text = "3730000092";
                else
                    comboEPCGNo.Text = "3730000120";
            }

            if (checkDirectExport.Checked)
                groupDirectExport.Visible = true;
            else
                groupDirectExport.Visible = false;

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
                if (comboCompanyName.Text.Trim() == "Plastene India Limited")
                {
                    if (comboBuyerName.Text == "OSWAL EXTRUSION LTD" || comboBuyerName.Text == "BABU INTERNATIONAL"
                        || comboBuyerName.Text == "ANITA EXPORTS," || comboBuyerName.Text == "JINDAL FIBRES")
                        comboEPCGNo.Text = "3730000088";
                    else
                        comboEPCGNo.Text = "3730000120";
                }


                if (Database.OpenConnection(Utility.DespacthConnectionString))
                {
                    Database.myreader = Database.GetExecuteReaderCommand("select terms,companycode,state,Destination from companymaster where companyname = '"
                     + comboBuyerName.Text + "'");
                    if (Database.myreader.Read())
                    {
                        textTermsofPayment.Text = Database.myreader[0].ToString();
                        CompanyCode = Database.myreader[1].ToString();
                        comboState.Text = Convert.ToString(Database.myreader["state"]);
                        comboDestination.Text = Convert.ToString(Database.myreader["Destination"]);
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
                if (comboCompanyName.Text.Trim() == "Plastene India Limited")
                {
                    if (dataGridView1.Rows[0].Cells[DCommodity.Index].FormattedValue.ToString() != "")
                    {
                        if (dataGridView1.Rows[0].Cells[DCommodity.Index].Value.ToString() == "PP WOVEN SACKS WITHOUT LINER")
                        {
                            if (comboBuyerName.Text == "OSWAL EXTRUSION LTD" || comboBuyerName.Text == "BABU INTERNATIONAL"
                                || comboBuyerName.Text == "ANITA EXPORTS," || comboBuyerName.Text == "JINDAL FIBRES")
                                comboAdvLicenseNo.Text = "3710001166";
                            if (checkDirectExport.Checked)
                                comboAdvLicenseNo.Text = "3710001165";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
            }
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


                    if (dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[DPKgUnit.Index].FormattedValue.ToString() != "" &&
                  dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[DCommodity.Index].FormattedValue.ToString() != "" &&
                  dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[DOrderQty.Index].FormattedValue.ToString() != "" &&
                  dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[DUOM.Index].FormattedValue.ToString() != ""
                        && dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[DUnitPrice.Index].FormattedValue.ToString() != "")
                    {
                        dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[DTotal.Index].Value =
                            Convert.ToString(Convert.ToDouble(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[DOrderQty.Index].Value.ToString())
                            * Convert.ToDouble(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[DUnitPrice.Index].Value.ToString()));
                        total += Convert.ToDouble(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[DTotal.Index].Value.ToString());
                    }

                    if (dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[DNetWt.Index].FormattedValue.ToString() != "" &&
                  dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[DGrossWt.Index].FormattedValue.ToString() != "")
                    {
                        double NetWt = Convert.ToDouble(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[DNetWt.Index].Value.ToString());
                        double GrossWt = Convert.ToDouble(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[DGrossWt.Index].Value.ToString());
                        double Trwt = GrossWt - NetWt;
                        dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[DTrWt.Index].Value = Trwt.ToString();
                    }
                }
                catch (Exception ex)
                {
                    ErrorMessageBox.Show(ex);
                }
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


                    string strsql = "select distinct City from Consignee where consigneename = '"
                       + comboConsigneeName.Text + "' and companycode ='" + CompanyCode + "'";
                    DespatchCommonFunction.Loadcombo(comboConsigneeCity, strsql, "City", "City", Utility.DespacthConnectionString);
                    Database.Closeconnection();

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
                    Database.myreader = Database.GetExecuteReaderCommand("select ConsigneeAdd,eccno from Consignee where consigneename = '"
                       + comboConsigneeName.Text + "' and companycode ='" + CompanyCode + "' and city = '" + comboConsigneeCity.Text + "'");
                    if (Database.myreader.Read())
                    {
                        textConsignee.Text = Database.myreader[0].ToString();
                        if (Database.myreader[1].ToString() != "")
                            textConsignee.Text += " ECC No : " + Database.myreader[1].ToString();
                    }
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

        private void comboEPCGNo_SelectedIndexChanged(object sender, EventArgs e)
        {
            Database.OpenConnection(Utility.DespacthConnectionString);
            Database.myreader = Database.GetExecuteReaderCommand("select EPCGDate from epcgmaster where EPCGLicNo = '"
                 + comboEPCGNo.Text + "'");
            if (Database.myreader.Read())
                dateTimePickerEPCGDate.Value = Convert.ToDateTime(Database.myreader[0].ToString());
            Database.myreader.Close();
        }

        private void comboAdvLicenseNo_SelectedIndexChanged(object sender, EventArgs e)
        {
            Database.OpenConnection(Utility.DespacthConnectionString);
            Database.myreader = Database.GetExecuteReaderCommand("select sysdate from advlicense where AdvLicenseNo = '"
                 + comboAdvLicenseNo.Text + "' and companyname = '"
                 + comboCompanyName.Text + "'");
            if (Database.myreader.Read())
                dateTimePickerAdvLicenseDate.Value = Convert.ToDateTime(Database.myreader[0].ToString());
            Database.myreader.Close();

        }

        private void dataGridView1_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            if (e.Control is TextBox)
            {
                TextBox tb = e.Control as TextBox;
                tb.KeyPress -= new KeyPressEventHandler(tb_KeyPress);
                if (this.dataGridView1.CurrentCell.ColumnIndex == DPkgQty.Index
                    || this.dataGridView1.CurrentCell.ColumnIndex == DTotal.Index
                    || this.dataGridView1.CurrentCell.ColumnIndex == DOrderQty.Index
                    || this.dataGridView1.CurrentCell.ColumnIndex == DUnitPrice.Index
                    || this.dataGridView1.CurrentCell.ColumnIndex == DNetWt.Index
                    || this.dataGridView1.CurrentCell.ColumnIndex == DGrossWt.Index
                    || this.dataGridView1.CurrentCell.ColumnIndex == DTrWt.Index)
                    tb.KeyPress += new KeyPressEventHandler(tb_KeyPress);
            }

            if (dataGridView1.CurrentCell.ColumnIndex == DCommodity.Index) // For Commodity ComboBox
            {
                ComboBox comboBox = e.Control as ComboBox;
                comboBox.SelectedIndexChanged += new EventHandler(comboBox_SelectedIndexChanged);
            }

        }

        void tb_KeyPress(object sender, KeyPressEventArgs e)
        {

            if ((!(Char.IsDigit(e.KeyChar))) && (!(e.KeyChar == '\b')) && (!(e.KeyChar == '.')))
                e.Handled = true;
        }


        void comboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            ComboBox cb = ((ComboBox)sender);
            try
            {
                if (dataGridView1.CurrentCell.ColumnIndex == DCommodity.Index) // For Commodity ComboBox
                {
                    if (comboCompanyName.Text.Trim() == "Plastene India Limited")
                    {
                        if (cb.Text == "COLOURED HDPE TARPAULIN WITH U.V. STABILIZER FITTED WITH PP ROPE AND EYELET (GROMET) PACKED IN LDPE FILLING BAGS")
                            comboEPCGNo.Text = "3730000124";
                        else
                            comboEPCGNo.Text = "3730000120";
                    }

                    if (Database.OpenConnection(Utility.DespacthConnectionString))
                    {
                        ArrayList ArrAdvCommodity = new ArrayList();
                        comboAdvLicenseNo.Items.Clear();


                        Database.myreader = Database.GetExecuteReaderCommand("Select * from AdvLicense where Commodity = '"
                             + cb.Text + "' and companyname = '" + comboCompanyName.Text + "' order by commodity");
                        while (Database.myreader.Read())
                        {
                            ArrAdvCommodity.Add(Database.myreader[0].ToString());
                            dateTimePickerAdvLicenseDate.Text = Database.myreader[8].ToString();
                        }
                        Database.myreader.Close();

                        if (ArrAdvCommodity.Count <= 0)
                        {
                            //MessageBox.Show("Advance License No is not available against this Commodity");
                            checkAdvanceLic.Visible = false;
                            checkImport.Visible = false;
                        }
                        else
                        {
                            checkAdvanceLic.Visible = true;
                            checkImport.Visible = true;
                        }


                        string Advlicenseno = "";
                        string DFIA = "";

                        Database.OpenConnection(Utility.DespacthConnectionString);
                        Database.myreader = Database.GetExecuteReaderCommand("Select Advlicenseno,DFIA from AdvLicense where Commodity = '"
                           + cb.Text + "' and companyname = '" + comboCompanyName.Text + "' and Setdefault = 'yes'");
                        if (Database.myreader.Read())
                        {
                            Advlicenseno = Database.myreader[0].ToString();
                            DFIA = Database.myreader[1].ToString();
                        }
                        Database.myreader.Close();




                        for (int i = 0; i < ArrAdvCommodity.Count; i++)
                            comboAdvLicenseNo.Items.Add(ArrAdvCommodity[i].ToString());

                        comboAdvLicenseNo.Text = Advlicenseno;

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
                        Database.Closeconnection();


                        if (DFIA == "yes")
                        {
                            checkAdvanceLic.Visible = false;
                            checkAdvanceLic.Checked = false;
                            checkDFIA.Visible = true;
                        }
                        else
                        {
                            checkAdvanceLic.Visible = true;
                            checkDFIA.Visible = false;

                        }
                    }
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
                     Convert.ToDouble(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[DOrderQty.Index].Value.ToString()) *
                     Convert.ToDouble(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[DUnitPrice.Index].Value.ToString());

                    if (dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[DUOM.Index].FormattedValue.ToString().Trim() == "KG" ||
                        dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[DUOM.Index].FormattedValue.ToString().Trim() == "KGS")
                    {
                        dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[DNetWt.Index].Value =
                            dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[DOrderQty.Index].Value.ToString();
                    }

                }

                if (dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[DNetWt.Index].FormattedValue.ToString().Trim() != ""
                    && dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[DGrossWt.Index].FormattedValue.ToString().Trim() != "")
                {
                    dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[DTrWt.Index].Value =
                      Convert.ToDouble(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[DGrossWt.Index].Value.ToString()) -
                      Convert.ToDouble(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[DNetWt.Index].Value.ToString());

                }
                //colCGSTrate colCGSTamt  colSGSTRate   ColSGSTAmt
                if (Convert.ToDouble(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[colCGSTRate.Index].Value) > 0)
                {
                    if (dataGridView1.CurrentCell.ColumnIndex == colCGSTRate.Index)
                    {
                        dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[colCGSTAmt.Index].Value =
                            Math.Round(Convert.ToDouble(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[DTotal.Index].Value.ToString()) *
                            Convert.ToDouble(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[colCGSTRate.Index].Value.ToString()) / 100, 2);
                    }
                }
                if (Convert.ToDouble(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[ColSGSTRate.Index].Value) > 0)
                {
                    if (dataGridView1.CurrentCell.ColumnIndex == ColSGSTRate.Index)
                    {
                        dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[ColSGSTAMT.Index].Value =
                            Math.Round(Convert.ToDouble(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[DTotal.Index].Value.ToString()) *
                            Convert.ToDouble(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[ColSGSTRate.Index].Value.ToString()) / 100, 2);
                    }
                }
                if (Convert.ToDouble(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[ColIGSTRate.Index].Value) > 0)
                {
                    if (dataGridView1.CurrentCell.ColumnIndex == ColIGSTRate.Index)
                    {
                        dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[ColIGSTAmt.Index].Value =
                            Math.Round(Convert.ToDouble(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[DTotal.Index].Value.ToString()) *
                            Convert.ToDouble(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[ColIGSTRate.Index].Value.ToString()) / 100, 2);
                    }
                }
            }
            catch (Exception ex)
            {
                ex.ToString();
            }
        }


        private void textItem_MouseClick(object sender, MouseEventArgs e)
        {
            MessageBox.Show("click");
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



        private void checkDFIA_CheckedChanged(object sender, EventArgs e)
        {
            if (checkAdvanceLic.Checked || checkDFIA.Checked)
            {
                groupAdvanceLic.Visible = true;
                //if (comboCompanyName.Text.Trim() == "Plastene India Limited")18/11/2010
                checkImport.Checked = true;
            }
            else
            {
                groupAdvanceLic.Visible = false;
                // if (comboCompanyName.Text.Trim() == "Plastene India Limited") //18/11/2010
                checkImport.Checked = false;
            }
        }

        private void checkCommercialInvoice_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                if (checkCommercialInvoice.Checked == true)
                {
                    groupCommercialInvoice.Visible = true;
                    if (Database.OpenConnection(Utility.DespacthConnectionString))
                    {
                        Database.myreader = Database.GetExecuteReaderCommand("Select invoice_no from Packinglist where companyname = '"
                            + comboCompanyName.Text + "' and yr = '"
                            + frmDefaultVale.Year + "' order by invoice_dt desc,invno desc ");
                        while (Database.myreader.Read())
                            comboCommercialInvoice.Items.Add(Database.myreader[0].ToString());
                        Database.myreader.Close();
                        Database.Closeconnection();
                        if (comboCommercialInvoice.Items.Count > 0)
                            comboCommercialInvoice.SelectedIndex = 0;
                    }
                }
                else
                {
                    groupCommercialInvoice.Visible = false;
                    comboCommercialInvoice.Text = "";
                    comboCommercialInvoice.Items.Clear();
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

        private void checkBox1_CheckedChanged_1(object sender, EventArgs e)
        {

        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            //frmARE3 frm = new frmARE3();
            //frm.ShowDialog();
            TabClass.OpenForm(new frmARE3());
        }

        private void dataGridView1_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            try
            {

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
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
                        dataGridView1.CurrentRow.Cells[ColHSNCode.Index].Value = THeading;

                        dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[ColHSNCode.Index].Value = THeading;

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

                        //dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[colCGSTRate.Index].Value = 0;
                        //dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[ColSGSTRate.Index].Value = 0;
                        //dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[ColIGSTRate.Index].Value = Database.myreader["NIGSTRate"].ToString();

                    }
                    Database.myreader.Close();
                    if (THeading.Length > 0)
                    {
                        if (dateTimePickerInvDate.Value < Convert.ToDateTime("2017-07-01 00:00:00"))
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
                    }

                }

            }
            catch (Exception ex)
            {
                MessageBox.Show("dataGridView1_CellValueChanged :" + ex.Message);
            }
        }

        private void button2_Click(object sender, EventArgs e)
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

        private void dateTimePickerInvDate_ValueChanged(object sender, EventArgs e)
        {
            try
            {
                string strsql = "";
                if (comboCompanyName.Text != "Oswal Extrusion Limited (Unit-II)")
                {
                    //strsql = "Select CompanyName from CompanyMaster where (invoicecategory = 'Export'  or invoicecategory = '*') order by CompanyName";
                    strsql = "Select companyname from CompanyMaster where (invoicecategory = 'Export'  or invoicecategory = '*') " +
                          " and isnull(IsFreeze,'no')='no' and (('" + dateTimePickerInvDate.Value.ToString("yyyy-MM-dd") +
                          "' between fromdate and todate) or (fromdate <= '" + dateTimePickerInvDate.Value.ToString("yyyy-MM-dd") +
                          "' and todate is null ))  order by CompanyName";
                }
                else
                {
                    //strsql = "Select CompanyName from CompanyMaster order by CompanyName";
                    strsql = "Select companyname from CompanyMaster where " +
                          " isnull(IsFreeze,'no')='no' and (('" + dateTimePickerInvDate.Value.ToString("yyyy-MM-dd") +
                          "' between fromdate and todate) or (fromdate <= '" + dateTimePickerInvDate.Value.ToString("yyyy-MM-dd") +
                          "' and todate is null ))  order by CompanyName";
                }
                DespatchCommonFunction.Loadcombo(comboBuyerName, strsql, "CompanyName", "CompanyName", Utility.DespacthConnectionString);
            }
            catch (Exception ex)
            {
                ex.Message.ToString();
            }

        }

    }
}