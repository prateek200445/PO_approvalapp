// =============================================================================
// INVOICE-RULE COPY — do not compile next to original.
// Replace frmChallan5A.cs after review. Search tag: INVOICE-RULE
// Job-work outward: same minus Despatch when packing list is mapped.
//
// LINE MAP (this copy):
//   549-550 Save minus Despatch after map
//   588 / 754 Update remember old packing list
//   923-930 Update plus old, rewrite map, minus new
//   1787 helper InvoiceRule_AdjustDespatchGodown
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
    public partial class frmChallan5A : Form
    {
        ComboBox comboBox;
        double total = 0;
        DataTable dtIMaster;
        bool IsUpdate = false;
        string CompanyCode = "";
        public int TotalCount = 0;
        double Excise = 0;
        double Educess = 0;
        double HiEduCess = 0;
        double AddVat = 0;
        double Vat = 0;

        string IndentCode = "";

        public frmChallan5A()
        {
            InitializeComponent();
            Form_Load();
        }

        private void Form_Load()
        {
            comboType.SelectedIndex = 0;

            dateTimePickerLRNo.Format = DateTimePickerFormat.Custom;
            dateTimePickerLRNo.CustomFormat = DespatchCommonFunction.ERPDateSettings();

            if (Database.OpenConnection(Utility.DespacthConnectionString))
            {
                DespatchCommonFunction.GetCompanyList(comboCompanyName);
                comboCompanyName.Text = frmDefaultVale.CompanyName;
                DespatchCommonFunction.GetTransportMaster(comboTranporterName);
                Database.Closeconnection();
            }


            if (Database.OpenConnection(Utility.DespacthConnectionString))
            {

                string strsql = "";
                //strsql = "Select companyname from CompanyMaster order by companyname";
                //strsql = "Select companyname from CompanyMaster where   " +
                //          " isnull(IsFreeze,'no')='no' and (('" + dateTimePicker1.Value.ToString("yyyy-MM-dd") +
                //          "' between fromdate and todate) or (fromdate <= '" + dateTimePicker1.Value.ToString("yyyy-MM-dd") +
                //          "' and todate is null ))  order by CompanyName";
                //DespatchCommonFunction.Loadcombo(comboBuyerName, strsql, "companyname", "companyname", Utility.DespacthConnectionString);

                //if (comboBuyerName.Items.Count > 0)
                //    comboBuyerName.SelectedIndex = 0;

                strsql = "Select Distinct(StateName) as StateName from StateMaster WITH(NOLOCK)  ";
                DespatchCommonFunction.Loadcombo(comboState, strsql, "StateName", "StateName", Utility.DespacthConnectionString);

                Database.Closeconnection();


            }

            comboCompanyName.Text = frmDefaultVale.CompanyName;
            textSuffix.Text = frmDefaultVale.Year;

            textPrefixCode.Text = IndentCode + "/" + textSuffix.Text + " /JOBWORK/";
            dateTimePicker1.Format = DateTimePickerFormat.Custom;
            dateTimePicker1.CustomFormat = DespatchCommonFunction.ERPDateSettings();

            fillData();

        }

        void fillData(bool blnupdate = false)
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
                        + "' and (('" + dateTimePicker1.Value.ToString("yyyy-MM-dd")
                        + "' between nFromDate and nToDate) or (nFromDate <= '"
                        + dateTimePicker1.Value.ToString("yyyy-MM-dd") + "' and nToDate is null )) "
                        + " order by CommodityName");
                }
                DataTable dt = new DataTable();
                Database.myadapter.Fill(dt);
                Column1.DataSource = dt;
                Column1.ValueMember = dt.Columns[0].ColumnName.ToString();
                Column1.DisplayMember = dt.Columns[0].ColumnName.ToString();

            }


            Database.myadapter = Database.GetAdapterCommand("Select UnitName from MaterialProcessing.dbo.Unit order by UnitName");
            DataTable dt1 = new DataTable();
            Database.myadapter.Fill(dt1);

            Column5.DataSource = dt1;
            Column5.ValueMember = dt1.Columns[0].ColumnName.ToString();
            Column5.DisplayMember = dt1.Columns[0].ColumnName.ToString();

            #region ITem Master Added 22.06.2020
            dtIMaster = new DataTable();
            dtIMaster = DespatchCommonFunction.GetItemMaster(dateTimePicker1.Value.ToString("yyyy-MM-dd"), comboCompanyName.Text);
            ColItemCode.DataSource = dtIMaster;
            ColItemCode.ValueMember = dtIMaster.Columns[0].ColumnName.ToString();
            ColItemCode.DisplayMember = dtIMaster.Columns[1].ColumnName.ToString();

            DataTable dtdeptt = DespatchCommonFunction.GetItemMainGrp();
            colMainGrp.DataSource = dtdeptt;
            colMainGrp.ValueMember = dtdeptt.Columns[0].ColumnName.ToString();
            colMainGrp.DisplayMember = dtdeptt.Columns[0].ColumnName.ToString();
            #endregion


        }


        private void button2_Click(object sender, EventArgs e)
        {
            TxtEwayNo.Text = "";
            TxtEwayNo.Enabled = true;
            dataGridView1.Rows.Clear();
            textAnnexNo.Text = "";
            textVehicalNo.Text = "";
            textDuration.Text = "";
            textNatureProcessing.Text = "";
            textChallanCode.Text = "";
            checkInsurance.Checked = false;
            IsUpdate = false;
            btnOK.Enabled = true;
            btnUpdate.Enabled = false;
            textRemarks.Text = "";
            fillData();
        }

        public void IncreaseValueByone(ref string value, int Startindex)
        {
            string frontstring = value.Substring(0, Startindex);
            string numericstring = value.Substring(Startindex, 3);
            string format = "";
            int i = Convert.ToInt32(numericstring);
            i = i + 1;
            if (i < 10)
                format = "00";
            else if (i < 100)
                format = "0";
            value = frontstring + format + i.ToString();
        }


        private string GenerateChallanCode()
        {
            try
            {
                string PrefixCode = IndentCode + "/" + textSuffix.Text + "/JOBWORK/";
                int IncreaseValue = 0;
                ArrayList arrpur = new ArrayList();
                int MaxValue = 0;
                if (Database.OpenConnection(Utility.DespacthConnectionString))
                {
                    string strsql = "";
                    strsql = "select isnull(max(cast(replace(challanno ,'" + PrefixCode + "','') as int)),0)   from Challan5a where challanno like '" + PrefixCode + "%'  ";
                    Database.myreader = Database.GetExecuteReaderCommand(strsql);
                    if (Database.myreader.Read())
                        MaxValue = Convert.ToInt16(Database.myreader[0].ToString());
                    Database.myreader.Close();

                    //Database.myreader = Database.GetExecuteReaderCommand("select challanno from Challan5a where challanno like'"
                    //         + PrefixCode + "%'");
                    //while (Database.myreader.Read())
                    //    arrpur.Add(Database.myreader[0].ToString());
                    //Database.myreader.Close();

                    //ArrayList arr = new ArrayList();

                    //for (int i = 0; i < arrpur.Count; i++)
                    //    arr.Add(arrpur[i].ToString().Substring(arrpur[i].ToString().LastIndexOf('/') + 1));

                    //for (int i = 0; i < arr.Count; i++)
                    //{
                    //    if (MaxValue < Convert.ToInt32(arr[i]))
                    //        MaxValue = Convert.ToInt32(arr[i]);
                    //}
                }
                if (MaxValue == 0)
                    IncreaseValue = 1;
                else
                    IncreaseValue = MaxValue + 1;
                PrefixCode = PrefixCode + IncreaseValue.ToString();

                return PrefixCode;
            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
                return "";
            }
        }

        private string GetCompanyCode()
        {
            try
            {
                string CompanyCode = "";
                if (Database.OpenConnection(Utility.DespacthConnectionString))
                {
                    Database.myreader = Database.GetExecuteReaderCommand("select CompanyCode from CompanyMaster where CompanyName = '"
                                     + comboBuyerName.Text + "'");
                    if (Database.myreader.Read())
                        CompanyCode = Database.myreader[0].ToString();
                    Database.myreader.Close();
                    //  Database.Closeconnection();
                }
                return CompanyCode;
            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
                return "";
            }
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            bool BlnTarn = false;
            if (!FrmMain.IsCreateJobWork)
                MessageBox.Show("you have no right to access Job Work");
            else
            {
                #region 19-08-2020 lock as per accounting voucher type
                if (!Utility.CheckAccountVoucherDateFreeze(dateTimePicker1.Value, comboCompanyName.Text, "sales"))
                {
                    MessageBox.Show(AccountCommonVariable.ErrMsg.DATE_FREEZE + "\nPlease Check Challan Date");
                    return;
                }
                #endregion

                //Allow as per discussion & email from Arvindji 01-Nov-2019
                //#region Check With GST IN for Challan for same unit GSTIN Number
                //// Varsana Unit
                //if (!comboCompanyName.Text.Contains("Varsana Unit"))
                //{
                //    if (Convert.ToDateTime(dateTimePicker1.Value) >= Convert.ToDateTime("2017-07-01 00:00:00"))
                //    {
                //        if (Database.OpenConnection(Utility.DespacthConnectionString))
                //        {
                //            string BuyerGST = Convert.ToString(Database.GetExecuteNonQueryCommand_retrun("Select NewGSTNo from CompanyMaster where CompanyName='" + comboBuyerName.Text + "'"));
                //            string CompanyGST = Convert.ToString(Database.GetExecuteNonQueryCommand_retrun("Select NewGSTNo from factoryinfo where Name='" + comboCompanyName.Text + "'"));

                //            if (BuyerGST.ToUpper() == CompanyGST.ToUpper())
                //            {
                //                MessageBox.Show("Same Company GST is not send for job work. Plz create Delivery Challan");
                //                return;
                //            }
                //        }
                //    }
                //}
                //#endregion
                string ConsigneeCode = "";

                if (comboBuyerName.Text == "")
                    MessageBox.Show("Please enter any value in Buyer Name");
                else if (textDuration.Text == "")
                    MessageBox.Show("Please enter value in Duration");
                else if (cmbCommodity.Text == "")
                {
                    MessageBox.Show("Please enter value in Nature of Processing Commodity");
                    cmbCommodity.Focus();
                    SendKeys.Send("{F4}");
                }
                else
                {
                    try
                    {
                        bool isMultilayer = false;


                        int VenCnt = DespatchCommonFunction.CustomerLedgerName(comboBuyerName.Text, comboCompanyName.Text);
                        if (VenCnt == 0)
                        {
                            MessageBox.Show("No Account Ledger Found for this customer.\nPlease Ask Account Dept to create Ledger for this customer:- [" + comboBuyerName.Text + "]");
                            return;
                        }
                        #region Itemcode check
                        //ColItemCode
                        for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                        {
                            if (Convert.ToString(dataGridView1.Rows[i].Cells[ColItemCode.Index].Value) == "")
                            {
                                MessageBox.Show("Please Select Item");
                                return;
                            }
                            else
                            {
                                string strsql = "select count(*) from MaterialProcessing.dbo.item where itemcode='" + Convert.ToString(dataGridView1.Rows[i].Cells[ColItemCode.Index].Value)
                                    + "' and companyname='" + comboCompanyName.Text + "' and " +
                                    "(('" + dateTimePicker1.Value.ToString("yyyy-MM-dd") + "' BETWEEN FROMDATE AND TODATE) OR (FROMDATE <='" + dateTimePicker1.Value.ToString("yyyy-MM-dd") + "' AND TODATE IS NULL)) ";
                                int intcnt = Convert.ToInt32(Database.GetExecuteNonQueryCommand_retrun(strsql));
                                if (intcnt == 0)
                                {
                                    MessageBox.Show("Please Enter Proper Item Code for :" +
                                        Convert.ToString(dataGridView1.Rows[i].Cells[ColItemCode.Index].Value.ToString()));
                                    return;
                                }
                            }
                        }
                        #endregion
                        if (dateTimePicker1.Value > Convert.ToDateTime("2017-06-30 00:00:00"))
                        {
                            CalculationGST();
                        }
                        else
                        {
                            Calculation();
                        }
                        string ChallanCode = GenerateChallanCode();
                        string CompanyCode = GetCompanyCode();
                        //int x = 0;
                        string Arrdesc = "";
                        string ArrIden = "";
                        string Arrqty = "";
                        string ArrValue = "";
                        string ArrUnit = "";
                        string ArrTariff = "";
                        int Count = dataGridView1.Rows.Count - 1;

                        for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                        {

                            //if (dataGridView1.Rows[i].Cells[Column1.Index].Value.ToString().ToLower().Contains("Multilayer") ||
                            //   dataGridView1.Rows[i].Cells[Column1.Index].Value.ToString().ToLower().Contains("liner") ||
                            //   dataGridView1.Rows[i].Cells[Column1.Index].Value.ToString().ToLower().Contains("film") ||
                            //   dataGridView1.Rows[i].Cells[Column1.Index].Value.ToString().ToLower().Contains("ld/lldpe bags") ||
                            //   dataGridView1.Rows[i].Cells[Column1.Index].Value.ToString().ToLower().Contains("ld/lldpe film"))
                            //    isMultilayer = true;

                            Convert.ToDouble(dataGridView1.Rows[i].Cells[Column3.Index].Value.ToString());
                            //Convert.ToDouble(dataGridView1.Rows[i].Cells[6].Value.ToString());

                            Arrdesc += dataGridView1.Rows[i].Cells[Column1.Index].Value.ToString() + ",";
                            if (dataGridView1.Rows[i].Cells[Column2.Index].FormattedValue.ToString().Trim() != "")
                                ArrIden += dataGridView1.Rows[i].Cells[Column2.Index].Value.ToString() + ",";
                            if (dataGridView1.Rows[i].Cells[Column3.Index].FormattedValue.ToString().Trim() != "")
                                Arrqty += dataGridView1.Rows[i].Cells[Column3.Index].Value.ToString() + ",";
                            if (dataGridView1.Rows[i].Cells[Column5.Index].FormattedValue.ToString().Trim() != "")
                                ArrUnit += dataGridView1.Rows[i].Cells[Column5.Index].Value.ToString() + ",";
                            if (dataGridView1.Rows[i].Cells[Column6.Index].FormattedValue.ToString().Trim() != "")
                                ArrValue += dataGridView1.Rows[i].Cells[Column6.Index].Value.ToString() + ",";
                            if (dataGridView1.Rows[i].Cells[Column7.Index].FormattedValue.ToString().Trim() != "")
                            {
                                if (dateTimePicker1.Value.Date >= Convert.ToDateTime("2021-04-01"))
                                    ArrTariff += dataGridView1.Rows[i].Cells[Column7.Index].Value.ToString().Substring(0, 6) + ",";
                                else
                                    ArrTariff += (dateTimePicker1.Value < Convert.ToDateTime("2017-07-01") ? dataGridView1.Rows[i].Cells[Column7.Index].Value.ToString() : dataGridView1.Rows[i].Cells[Column7.Index].Value.ToString().Substring(0, 4)) + ",";


                            }
                            Count = Count + 1;

                        }

                        //if (cmbCommodity.Text.ToString().ToLower().Contains("Multilayer") ||
                        //    cmbCommodity.Text.ToString().ToLower().Contains("liner") ||
                        //    cmbCommodity.Text.ToString().ToLower().Contains("film") ||
                        //    cmbCommodity.Text.ToString().ToLower().Contains("ld/lldpe bags") ||
                        //    cmbCommodity.Text.ToString().ToLower().Contains("ld/lldpe film"))
                        //    isMultilayer = true;

                        //if (textNatureProcessing.Text.ToString().ToLower().Contains("Multilayer") ||
                        //    textNatureProcessing.Text.ToString().ToLower().Contains("liner") ||
                        //    textNatureProcessing.Text.ToString().ToLower().Contains("film") ||
                        //    textNatureProcessing.Text.ToString().ToLower().Contains("ld/lldpe bags") ||
                        //    textNatureProcessing.Text.ToString().ToLower().Contains("ld/lldpe film"))
                        //    isMultilayer = true;

                        if (Database.OpenConnection(Utility.DespacthConnectionString))
                        {
                            Database.myreader = Database.GetExecuteReaderCommand("select ConsigneeCode from Consignee where consigneename = '"
                                 + comboConsigneeName.Text + "' and companycode ='" + CompanyCode
                                 + "' and city = '" + comboConsigneeCity.Text + "'");
                            if (Database.myreader.Read())
                                ConsigneeCode = Database.myreader[0].ToString();
                            Database.myreader.Close();
                            Database.Closeconnection();
                        }
                        string InsuranceApp = "";
                        string Insurance = "";
                        if (checkInsurance.Checked)
                        {
                            InsuranceApp = "yes";
                            Insurance = textInsurance.Text;
                        }
                        else
                            InsuranceApp = "no";

                        if (Count == 0)
                            MessageBox.Show("Please fill at least One Item");
                        else
                        {

                            DialogResult drs = MessageBox.Show("Do you want to generate Challan 5A", "Challan 5A", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                            if (drs.CompareTo(DialogResult.Yes) == 0)
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
                                            TotalNetwt += Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[Column3.Index].Value.ToString());
                                        }

                                        if (!DespatchCommonFunction.GetPackingListInvoiceNo2(comboCompanyName.Text, comboBuyerName.Text, TotalNetwt, dataGridView1, ColItemCode.Name, Column3.Name, ref despackinglist))
                                        {
                                            return;
                                        }

                                    }
                                }
                                #endregion
                                if (Database.OpenConnection(Utility.DespacthConnectionString))
                                {
                                    Database.BeginTransaction();
                                    BlnTarn = true;
                                    Database.GetExecuteNonQueryCommand("insert into challan5a (DescGoods,IdenMarks,Qty,value,Tariff,SysDate,NatureProc,Duration,AnnexNo,VehicalNo     ,CompNam,CompCode,LoginName,ChallanNo,NoOfMeter,CompanyName,Yr,unit,ConsigneeCode,TransportName,LRNo,LRDate,excise,educess,HiEducess,Vat,Addvat,ExciseAmt,EduCessAmt,HieducessAmt,VatAmt,AddVatAmt,VatType,TotalAmt,ConsigneeName,ConsigneeCity,Consignee,Header,iscancel,Insurance,checkinsurance,FGCommodity ,EWayBillNo,Remarks ) values('"
                                        + Arrdesc + "','"
                                        + ArrIden + "','"
                                        + Arrqty + "','"
                                        + ArrValue + "','"
                                        + ArrTariff + "','"
                                        + dateTimePicker1.Value.ToString("yyyy-MM-dd HH:mm") + "','"
                                        + textNatureProcessing.Text + "',"
                                        + textDuration.Text + ",'"
                                        + textAnnexNo.Text + "','"
                                        + textVehicalNo.Text + "','"
                                        + comboBuyerName.Text + "','"
                                        + CompanyCode + "','"
                                        + FrmMain.UserName + "','"
                                        + ChallanCode + "','','"
                                        + comboCompanyName.Text + "','"
                                        + textSuffix.Text + "','"
                                        + ArrUnit + "','"
                                        + ConsigneeCode + "','"
                                        + comboTranporterName.Text + "','"
                                        + textBoxLRNo.Text + "','"
                                        + dateTimePickerLRNo.Value.ToString(DespatchCommonFunction.ERPDBDateSettings()) + "',"
                                        + textExDuty.Text + ","
                                        + textEduCess.Text + ","
                                        + textSecHiEdu.Text + ","
                                        + textVat.Text + ","
                                        + textAddVat.Text + ","
                                        + Excise + ","
                                        + Educess + ","
                                        + HiEduCess + ","
                                        + Vat + ","
                                        + AddVat + ",'"
                                        + comboType.Text + "',"
                                        + total + ",'"
                                        + comboConsigneeName.Text + "','"
                                        + comboConsigneeCity.Text + "','"
                                        + textConsignee.Text + "','','','"
                                        + Insurance + "','"
                                        + InsuranceApp + "','"
                                        + cmbCommodity.Text + "','"
                                        + TxtEwayNo.Text + "','"
                                        + textRemarks.Text + "')");
                                    UpdateInsurancePolicy(Convert.ToDouble(total), checkInsurance.Checked);
                                }

                                for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                                {
                                    //Database.GetExecuteNonQueryCommand("insert into ChallanItem(Code,DescGoods,Identification,Qty,QtyDesc,Unit,Valu,TariffClasss,PendingQty,SubCode,CGSTrate,CGSTAmt,SGSTrate,SGSTAmt,IGSTrate,IGSTAmt,ItemCode,UnitRate) values ('"
                                    //  + ChallanCode + "','" 
                                    //  + dataGridView1.Rows[i].Cells[0].Value.ToString() + "','"
                                    //  + dataGridView1.Rows[i].Cells[1].Value.ToString() + "',"
                                    //  + dataGridView1.Rows[i].Cells[2].Value.ToString() + ",'"
                                    //  + dataGridView1.Rows[i].Cells[3].Value.ToString() + "','"
                                    //  + dataGridView1.Rows[i].Cells[4].Value.ToString() + "',"
                                    //  + dataGridView1.Rows[i].Cells[Column6.Index].Value.ToString() + ",'"
                                    //  + dataGridView1.Rows[i].Cells[Column7.Index].Value.ToString() + "',"
                                    //  + dataGridView1.Rows[i].Cells[2].Value.ToString() + ","
                                    //  + dataGridView1.Rows[i].Cells[Srno.Index].Value.ToString() + ","
                                    //  + dataGridView1.Rows[i].Cells[colCGSTPer.Index].Value.ToString() + ","
                                    //  + dataGridView1.Rows[i].Cells[colCGSTAmt.Index].Value.ToString() + ","
                                    //  + dataGridView1.Rows[i].Cells[colSGSTPer.Index].Value.ToString() + ","
                                    //  + dataGridView1.Rows[i].Cells[colSGSTAmt.Index].Value.ToString() + ","
                                    //  + dataGridView1.Rows[i].Cells[ColIGSTPer.Index].Value.ToString() + ","
                                    //  + dataGridView1.Rows[i].Cells[ColIGSTAmt.Index].Value.ToString() + ",'"
                                    //  + Convert.ToString(dataGridView1.Rows[i].Cells[ColItemCode.Index].Value) + "',"
                                    //  + Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[DUnitPrice.Index].Value.ToString()) + ")");
                                    Database.GetExecuteNonQueryCommand("insert into ChallanItem(Code,DescGoods,Identification,Qty,QtyDesc,Unit,Valu,TariffClasss,PendingQty,SubCode,CGSTrate,CGSTAmt,SGSTrate,SGSTAmt,IGSTrate,IGSTAmt,ItemCode,UnitRate) values ('"
                                       + ChallanCode + "','"
                                       + dataGridView1.Rows[i].Cells[Column1.Index].Value.ToString() + "','"
                                       + dataGridView1.Rows[i].Cells[Column2.Index].Value.ToString() + "',"
                                       + dataGridView1.Rows[i].Cells[Column3.Index].Value.ToString() + ",'"
                                       + dataGridView1.Rows[i].Cells[Column4.Index].Value.ToString() + "','"
                                       + dataGridView1.Rows[i].Cells[Column5.Index].Value.ToString() + "',"
                                       + dataGridView1.Rows[i].Cells[Column6.Index].Value.ToString() + ",'"
                                       + dataGridView1.Rows[i].Cells[Column7.Index].Value.ToString() + "',"
                                       + dataGridView1.Rows[i].Cells[Column3.Index].Value.ToString() + ","
                                       + dataGridView1.Rows[i].Cells[Srno.Index].Value.ToString() + ","
                                       + dataGridView1.Rows[i].Cells[colCGSTPer.Index].Value.ToString() + ","
                                       + dataGridView1.Rows[i].Cells[colCGSTAmt.Index].Value.ToString() + ","
                                       + dataGridView1.Rows[i].Cells[colSGSTPer.Index].Value.ToString() + ","
                                       + dataGridView1.Rows[i].Cells[colSGSTAmt.Index].Value.ToString() + ","
                                       + dataGridView1.Rows[i].Cells[ColIGSTPer.Index].Value.ToString() + ","
                                       + dataGridView1.Rows[i].Cells[ColIGSTAmt.Index].Value.ToString() + ",'"
                                       + Convert.ToString(dataGridView1.Rows[i].Cells[ColItemCode.Index].Value) + "',"
                                       + Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[DUnitPrice.Index].Value.ToString()) + ")");
                                }
                                Database.CommitTransaction();
                                BlnTarn = false;
                                #region PackingList
                                if (DespatchCommonFunction.AllowMappingPackinglist(comboCompanyName.Text) > 0)
                                {
                                    if (Database.OpenConnection(Utility.DespacthConnectionString))
                                    {
                                        Database.GetExecuteNonQueryCommand("INSERT INTO MappPackingList (CompanyName,InvNo,Yr,PackingListNo) VALUES('" +
                                        comboCompanyName.Text + "','" + ChallanCode + "','" + textSuffix.Text + "','" + despackinglist + "')");
                                        // INVOICE-RULE: packing list mapped on JW challan = take kg off Despatch board.
                                        InvoiceRule_AdjustDespatchGodown(comboCompanyName.Text, despackinglist, true);
                                    }
                                }
                                #endregion
                                Utility.UserInformation("Save", "Job Work Challan 5A : " + ChallanCode.ToString() + " is saved successfully");
                                Database.Closeconnection();

                                MessageBox.Show("Challan Code " + ChallanCode + " is succesfully added");

                                dataGridView1.Rows.Clear();
                                textAnnexNo.Text = "";
                                textVehicalNo.Text = "";
                                textDuration.Text = "";
                                textNatureProcessing.Text = "";
                                textChallanCode.Text = "";
                                textRemarks.Text = "";
                                IsUpdate = false;
                                btnOK.Enabled = true;
                                btnUpdate.Enabled = false;

                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if (BlnTarn)
                            Database.RollBackTransaction();
                        ErrorMessageBox.Show(ex);
                    }
                }
            }
        }



        private void btnUpdate_Click(object sender, EventArgs e)
        {
            string desPackinglist = "";
            string desPackinglistBeforeUpdate = ""; // INVOICE-RULE
            bool blnbegin = false;
            if (!FrmMain.IsUpdateJobWork)
                MessageBox.Show("you have no right to update Job work");
            else
            {
                #region 19-08-2020 lock as per accounting voucher type
                if (!Utility.CheckAccountVoucherDateFreeze(dateTimePicker1.Value, comboCompanyName.Text, "sales"))
                {
                    MessageBox.Show(AccountCommonVariable.ErrMsg.DATE_FREEZE + "\nPlease Check Challan Date");
                    return;
                }
                #endregion

                bool isMultilayer = false;
                //#region Check With GST IN for Challan for same unit GSTIN Number
                //if (!comboCompanyName.Text.Contains("Varsana Unit"))
                //{
                //    if (Convert.ToDateTime(dateTimePicker1.Value) >= Convert.ToDateTime("2017-07-01 00:00:00"))
                //    {
                //        if (Database.OpenConnection(Utility.DespacthConnectionString))
                //        {
                //            string BuyerGST = Convert.ToString(Database.GetExecuteNonQueryCommand_retrun("Select NewGSTNo from CompanyMaster where CompanyName='" + comboBuyerName.Text + "'"));
                //            string CompanyGST = Convert.ToString(Database.GetExecuteNonQueryCommand_retrun("Select NewGSTNo from factoryinfo where Name='" + comboCompanyName.Text + "'"));

                //            if (BuyerGST.ToUpper() == CompanyGST.ToUpper())
                //            {
                //                MessageBox.Show("Same Company GST is not send for job work. Plz create Delivery Challan");
                //                return;
                //            }
                //        }
                //    }
                //}
                //#endregion


                if (comboBuyerName.Text == "" || textDuration.Text == "")
                    MessageBox.Show("Please enter any value");
                else if (cmbCommodity.Text == "")
                {
                    MessageBox.Show("Please enter value in Nature of Processing Commodity");
                    cmbCommodity.Focus();
                    SendKeys.Send("{F4}");
                }
                else
                {
                    try
                    {
                        int VenCnt = DespatchCommonFunction.CustomerLedgerName(comboBuyerName.Text, comboCompanyName.Text);
                        if (VenCnt == 0)
                        {
                            MessageBox.Show("No Account Ledger Found for this customer.\nPlease Ask Account Dept to create Ledger for this customer:- [" + comboBuyerName.Text + "]");
                            return;
                        }
                        if (IsUpdate)
                        {
                            #region Itemcode check
                            //ColItemCode
                            for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                            {
                                if (Convert.ToString(dataGridView1.Rows[i].Cells[ColItemCode.Index].Value) == "")
                                {
                                    MessageBox.Show("Please Enter Item");
                                    return;
                                }
                                else
                                {
                                    string strsql = "select count(*) from MaterialProcessing.dbo.item where itemcode='" + Convert.ToString(dataGridView1.Rows[i].Cells[ColItemCode.Index].Value)
                                        + "' and companyname='" + comboCompanyName.Text + "' and " +
                                        "(('" + dateTimePicker1.Value.ToString("yyyy-MM-dd") + "' BETWEEN FROMDATE AND TODATE) OR (FROMDATE <='"
                                        + dateTimePicker1.Value.ToString("yyyy-MM-dd") + "' AND TODATE IS NULL)) ";
                                    int intcnt = Convert.ToInt32(Database.GetExecuteNonQueryCommand_retrun(strsql));
                                    if (intcnt == 0)
                                    {
                                        MessageBox.Show("Please Enter Proper Item Code for :"
                                            + Convert.ToString(dataGridView1.Rows[i].Cells[ColItemCode.Index].Value));
                                        return;
                                    }
                                }
                            }
                            #endregion
                            Database.OpenConnection(Utility.DespacthConnectionString);
                            for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                            {

                                //   if (dataGridView1.Rows[i].Cells[Column1.Index].Value.ToString().ToLower().Contains("Multilayer") ||
                                //dataGridView1.Rows[i].Cells[Column1.Index].Value.ToString().ToLower().Contains("liner") ||
                                //dataGridView1.Rows[i].Cells[Column1.Index].Value.ToString().ToLower().Contains("film") ||
                                //dataGridView1.Rows[i].Cells[Column1.Index].Value.ToString().ToLower().Contains("ld/lldpe bags") ||
                                //dataGridView1.Rows[i].Cells[Column1.Index].Value.ToString().ToLower().Contains("ld/lldpe film"))
                                //       isMultilayer = true;

                                int sId = (int)Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells["Srno"].FormattedValue.ToString());
                                if (sId == 0)
                                {
                                    MessageBox.Show("Invalid SubCode", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    return;
                                }
                                object oSum = Database.GetExecuteScalarCommand_return("SELECT SUM(AcceptedQty) FROM MaterialProcessing..Challan5AInward WHERE ChallanSubCode='" + textChallanCode.Text + "/" + dataGridView1.Rows[i].Cells["Srno"].FormattedValue.ToString() + "'");
                                if (oSum == null || oSum is DBNull) continue;
                                if (Convert.ToDouble(oSum) > Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells["Srno"].FormattedValue.ToString()))
                                {
                                    MessageBox.Show("Inward Qty is more then Order Qty", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    return;
                                }
                            }

                            string CompanyCode = GetCompanyCode();
                            string Arrdesc = "";
                            string ArrIden = "";
                            string Arrqty = "";
                            string ArrValue = "";
                            string ArrUnit = "";
                            string ArrTariff = "";
                            int Count = 0;

                            for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                            {


                                Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[Column3.Index].Value.ToString());
                                Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[Column6.Index].Value.ToString());

                                Arrdesc += dataGridView1.Rows[i].Cells[Column1.Index].Value.ToString() + ",";
                                if (dataGridView1.Rows[i].Cells[Column2.Index].FormattedValue.ToString().Trim() != "")
                                    ArrIden += dataGridView1.Rows[i].Cells[Column2.Index].Value.ToString() + ",";
                                if (dataGridView1.Rows[i].Cells[Column3.Index].FormattedValue.ToString().Trim() != "")
                                    Arrqty += dataGridView1.Rows[i].Cells[Column3.Index].Value.ToString() + ",";
                                if (dataGridView1.Rows[i].Cells[Column5.Index].FormattedValue.ToString().Trim() != "")
                                    ArrUnit += dataGridView1.Rows[i].Cells[Column5.Index].Value.ToString() + ",";
                                if (dataGridView1.Rows[i].Cells[Column6.Index].FormattedValue.ToString().Trim() != "")
                                    ArrValue += dataGridView1.Rows[i].Cells[Column6.Index].Value.ToString() + ",";
                                if (dataGridView1.Rows[i].Cells[Column7.Index].FormattedValue.ToString().Trim() != "")
                                    ArrTariff += dataGridView1.Rows[i].Cells[Column7.Index].Value.ToString() + ",";
                                Count = Count + 1;

                            }

                            // if (cmbCommodity.Text.ToString().ToLower().Contains("Multilayer") ||
                            //cmbCommodity.Text.ToString().ToLower().Contains("liner") ||
                            //cmbCommodity.Text.ToString().ToLower().Contains("film") ||
                            //cmbCommodity.Text.ToString().ToLower().Contains("ld/lldpe bags") ||
                            //cmbCommodity.Text.ToString().ToLower().Contains("ld/lldpe film"))
                            //     isMultilayer = true;

                            // if (textNatureProcessing.Text.ToString().ToLower().Contains("Multilayer") ||
                            //     textNatureProcessing.Text.ToString().ToLower().Contains("liner") ||
                            //     textNatureProcessing.Text.ToString().ToLower().Contains("film") ||
                            //     textNatureProcessing.Text.ToString().ToLower().Contains("ld/lldpe bags") ||
                            //     textNatureProcessing.Text.ToString().ToLower().Contains("ld/lldpe film"))
                            //     isMultilayer = true;

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
                                        TotalNetwt += Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[Column3.Index].Value.ToString());
                                    }
                                    desPackinglist = Convert.ToString(Database.GetExecuteNonQueryCommand_retrun("SELECT DISTINCT TOP 1 isnull(PackingListNo,'') FROM MappPackingList WHERE companyname='"
                                        + comboCompanyName.Text + "' AND InvNo='" + textChallanCode.Text + "' AND Yr='" + textSuffix.Text + "'"));
                                    desPackinglistBeforeUpdate = desPackinglist; // INVOICE-RULE

                                    if (desPackinglist == "")
                                    {
                                        if (!DespatchCommonFunction.GetPackingListInvoiceNo2(comboCompanyName.Text, comboBuyerName.Text, TotalNetwt, dataGridView1, ColItemCode.Name, Column3.Name, ref desPackinglist))
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
                                                string strsql = "select isnull(sum(isnull(Netwt,0)),0) Netwt from vw_PackingListAll where companyname='" + comboCompanyName.Text
                                                    + "'  and yr='" + textSuffix.Text + "' and "
                                                    + " (PACKINGLISTNO ='" + split[i] + "' or invno='" + split[i] + "')";
                                                Database.myreader = Database.GetExecuteReaderCommand(strsql);
                                                if (Database.myreader.Read())
                                                {
                                                    totalpackinglist += Convert.ToDouble(Database.myreader[0].ToString());
                                                }
                                                Database.myreader.Close();
                                                Database.Closeconnection();
                                            }
                                            if (Utility.SafeConvertToDouble(TotalNetwt) != Utility.SafeConvertToDouble(totalpackinglist))
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

                            if (Count == 0)
                                MessageBox.Show("Please fill at least One Item");
                            else
                            {
                                if (Database.OpenConnection(Utility.DespacthConnectionString))
                                {

                                    Database.BeginTransaction();
                                    blnbegin = true;
                                    string CheckInsurane = "";
                                    double TotalAmt = 0;
                                    Database.myreader = Database.GetExecuteReaderCommand("select TotalAmt,CheckInsurance from challan5A where ChallanNo = '"
                                        + textChallanCode.Text + "'");
                                    if (Database.myreader.Read())
                                    {
                                        TotalAmt = Convert.ToDouble(Database.myreader[0].ToString());
                                        CheckInsurane = Database.myreader[1].ToString();
                                    }
                                    Database.myreader.Close();

                                    if (checkInsurance.Checked == false && CheckInsurane == "yes")
                                        UpdateInsurancePolicy(-TotalAmt, true);
                                    else if (checkInsurance.Checked == true && CheckInsurane == "no")
                                        UpdateInsurancePolicy(total, true);
                                    else if (checkInsurance.Checked)
                                        UpdateInsurancePolicy(total - TotalAmt, true);
                                    if (checkInsurance.Checked)
                                    {
                                        CheckInsurane = "yes";
                                    }
                                    else
                                        CheckInsurane = "no";

                                    if (Database.GetExecuteNonQueryCommand("update Challan5A set DescGoods = '" + Arrdesc + "',"
                                                      + " IdenMarks = '" + ArrIden + "',"
                                                      + " Qty = '" + Arrqty + "',"
                                                      + " value = '" + ArrValue + "',"
                                                      + " Tariff ='" + ArrTariff + "',"
                                                      + " SysDate ='" + dateTimePicker1.Value.ToString("yyyy-MM-dd HH:mm") + "',"
                                                      + " NatureProc = '" + textNatureProcessing.Text + "',"
                                                      + " Duration = " + textDuration.Text + ","
                                                      + " AnnexNo = '" + textAnnexNo.Text + "',"
                                                      + " VehicalNo ='" + textVehicalNo.Text + "',"
                                                      + " CompNam = '" + comboBuyerName.Text + "',"
                                                      + " CompCode = '" + CompanyCode + "',"
                                                      + " Unit = '" + ArrUnit + "',"
                                                      + " TransportName = '" + comboTranporterName.Text + "',"
                                                      + " LRNo = '" + textBoxLRNo.Text + "',"
                                                      + " LRDate = '" + dateTimePickerLRNo.Value.ToString(DespatchCommonFunction.ERPDBDateSettings()) + "',"
                                                      + " Excise = " + textExDuty.Text + ","
                                                      + " EduCess = " + textEduCess.Text + ","
                                                      + " HieduCess = " + textSecHiEdu.Text + ","
                                                      + " Vat = " + textVat.Text + ","
                                                      + " AddVat = " + textAddVat.Text + ","
                                                      + " ExciseAmt = " + Excise + ","
                                                      + " EduCessAmt = " + Educess + ","
                                                      + " HieduCessAmt = " + HiEduCess + ","
                                                      + " VatAmt = " + Vat + ","
                                                      + " AddVatAmt = " + AddVat + ","
                                                      + " VatType = '" + comboType.Text + "',"
                                                      + " TotalAmt = " + total + ","
                                                      + " ConsigneeName = '" + comboConsigneeName.Text + "',"
                                                      + " ConsigneeCity = '" + comboConsigneeCity.Text + "',"
                                                      + " Consignee = '" + textConsignee.Text + "',"
                                                      + " Insurance = '" + textInsurance.Text + "', "
                                                      + " checkinsurance = '" + CheckInsurane + "', "
                                                      + " FGCommodity='" + cmbCommodity.Text + "',EWayBillNo='" + TxtEwayNo.Text + "',"
                                                      + " remarks='" + textRemarks.Text.Replace("'", "''") + "' where ChallanNo = '" + textChallanCode.Text + "'"))
                                        MessageBox.Show("Challan Code " + textChallanCode.Text + " is succesfully updated");

                                    Database.GetExecuteNonQueryCommand("delete from ChallanItem where code = '" + textChallanCode.Text + "'");

                                    for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                                    {
                                        //Database.GetExecuteNonQueryCommand("insert into ChallanItem(Code,DescGoods,Identification,Qty,QtyDesc,Unit,Valu,TariffClasss,PendingQty,SubCode) values ('"
                                        //+ textChallanCode.Text + "','" + dataGridView1.Rows[i].Cells[0].Value.ToString() + "','"
                                        //+ dataGridView1.Rows[i].Cells[1].Value.ToString() + "',"
                                        //+ dataGridView1.Rows[i].Cells[2].Value.ToString() + ",'"
                                        //+ dataGridView1.Rows[i].Cells[3].Value.ToString() + "','"
                                        //+ dataGridView1.Rows[i].Cells[4].Value.ToString() + "',"
                                        //+ dataGridView1.Rows[i].Cells[5].Value.ToString() + ",'"
                                        //+ dataGridView1.Rows[i].Cells[6].Value.ToString() + "',"
                                        //+ dataGridView1.Rows[i].Cells[2].Value.ToString() + ","
                                        //+ dataGridView1.Rows[i].Cells["srno"].Value.ToString() + ")");

                                        Database.GetExecuteNonQueryCommand("insert into ChallanItem(Code,DescGoods,Identification,Qty,QtyDesc,Unit,Valu,TariffClasss,PendingQty,SubCode,CGSTrate,CGSTAmt,SGSTrate,SGSTAmt,IGSTrate,IGSTAmt,ItemCode,UnitRate) values ('"
                                        + textChallanCode.Text + "','"
                                        + dataGridView1.Rows[i].Cells[Column1.Index].Value.ToString() + "','"
                                        + dataGridView1.Rows[i].Cells[Column2.Index].Value.ToString() + "',"
                                        + dataGridView1.Rows[i].Cells[Column3.Index].Value.ToString() + ",'"
                                        + dataGridView1.Rows[i].Cells[Column4.Index].Value.ToString() + "','"
                                        + dataGridView1.Rows[i].Cells[Column5.Index].Value.ToString() + "',"
                                        + dataGridView1.Rows[i].Cells[Column6.Index].Value.ToString() + ",'"
                                        + dataGridView1.Rows[i].Cells[Column7.Index].Value.ToString() + "',"
                                        + dataGridView1.Rows[i].Cells[Column3.Index].Value.ToString() + ","
                                        + dataGridView1.Rows[i].Cells[Srno.Index].Value.ToString() + ","
                                        + dataGridView1.Rows[i].Cells[colCGSTPer.Index].Value.ToString() + ","
                                        + dataGridView1.Rows[i].Cells[colCGSTAmt.Index].Value.ToString() + ","
                                        + dataGridView1.Rows[i].Cells[colSGSTPer.Index].Value.ToString() + ","
                                        + dataGridView1.Rows[i].Cells[colSGSTAmt.Index].Value.ToString() + ","
                                        + dataGridView1.Rows[i].Cells[ColIGSTPer.Index].Value.ToString() + ","
                                        + dataGridView1.Rows[i].Cells[ColIGSTAmt.Index].Value.ToString() + ",'"
                                        + Convert.ToString(dataGridView1.Rows[i].Cells[ColItemCode.Index].Value) + "',"
                                        + Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[DUnitPrice.Index].Value.ToString()) + ")");

                                    }
                                    Database.CommitTransaction();
                                    blnbegin = false;
                                    Database.Closeconnection();

                                    #region PackingList
                                    if (DespatchCommonFunction.AllowMappingPackinglist(comboCompanyName.Text) > 0)
                                    {
                                        if (Database.OpenConnection(Utility.DespacthConnectionString))
                                        {
                                            // INVOICE-RULE: plus old packing list back, then minus the list now on the challan.
                                            InvoiceRule_AdjustDespatchGodown(comboCompanyName.Text, desPackinglistBeforeUpdate, false);
                                            Database.GetExecuteNonQueryCommand("DELETE FROM MappPackingList WHERE CompanyName='" + comboCompanyName.Text + "' AND InvNo='" +
                                                textChallanCode.Text + "' AND YR='" + textSuffix.Text + "'");

                                            Database.GetExecuteNonQueryCommand("INSERT INTO MappPackingList (CompanyName,InvNo,Yr,PackingListNo) VALUES('" +
                                            comboCompanyName.Text + "','" + textChallanCode.Text + "','" + textSuffix.Text + "','" + desPackinglist + "')");
                                            InvoiceRule_AdjustDespatchGodown(comboCompanyName.Text, desPackinglist, true);
                                        }
                                    }
                                    #endregion
                                    Utility.UserInformation("Update", "Job Work Challan 5A : " + textChallanCode.Text.ToString() + " is Updated successfully");
                                    dataGridView1.Rows.Clear();
                                    textAnnexNo.Text = "";
                                    textVehicalNo.Text = "";
                                    textDuration.Text = "";
                                    textNatureProcessing.Text = "";
                                    textChallanCode.Text = "";
                                    textRemarks.Text = "";
                                    IsUpdate = false;
                                    btnOK.Enabled = true;
                                    btnUpdate.Enabled = false;
                                    fillData();
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if (blnbegin)
                            Database.RollBackTransaction();
                        ErrorMessageBox.Show(ex);
                    }
                }
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            frmPrintChallan5A frm = new frmPrintChallan5A();
            frm.ShowDialog();
        }


        private void comboBuyerName_SelectedIndexChanged(object sender, EventArgs e)
        {
            textConsignee.Text = "";
            comboState.Text = "";
            if (Database.OpenConnection(Utility.DespacthConnectionString))
            {

                Database.myreader = Database.GetExecuteReaderCommand("select consignee,terms,Destination,typeofInvoice,Companycode,state from companymaster where "
                              + " companyname = '" + comboBuyerName.Text + "'");
                while (Database.myreader.Read())
                {
                    textConsignee.Text = Database.myreader[0].ToString();
                    CompanyCode = Database.myreader[4].ToString();
                    comboState.Text = Convert.ToString(Database.myreader["state"]);
                }
                Database.myreader.Close();

                comboConsigneeName.Items.Clear();
                comboConsigneeName.Text = "";

                string strsql = "";
                strsql = "select Distinct(Consigneename) as Consigneename  from consignee where  companyCode = '" + CompanyCode + "'";
                DespatchCommonFunction.Loadcombo(comboConsigneeName, strsql, "Consigneename", "Consigneename", Utility.DespacthConnectionString);
                Database.Closeconnection();

                if (comboConsigneeName.Items.Count > 0)
                    comboConsigneeName.SelectedIndex = 0;
            }
        }

        private void comboCompanyName_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (Database.OpenConnection(Utility.DespacthConnectionString))
            {
                string strsql = "Select companyname from companymaster where (invoicecategory = 'Domestic'  or invoicecategory = '*')  " +
                    " and isnull(IsFreeze,'no')='no' and (('" + dateTimePicker1.Value.ToString("yyyy-MM-dd") +
                    "' between fromdate and todate) or (fromdate <= '" + dateTimePicker1.Value.ToString("yyyy-MM-dd") +
                    "' and todate is null ))  and CompanyCode not in " +
                    " ( select LCompanyCode from MaterialProcessing.dbo.LedgerMaster where (Todate is not null or IsAccClose='yes' ) and  companyname='" + comboCompanyName.Text +
                    "'  and isnull(LCompanyCode,'')<>'') order by companyname";
                DespatchCommonFunction.LoadCustomerUnlockChallan(comboBuyerName, comboCompanyName.Text, dateTimePicker1.Value);


                Database.myreader = Database.GetExecuteReaderCommand("Select IndentCode from factoryinfo where Name = '"
                     + comboCompanyName.Text + "'");
                if (Database.myreader.Read())
                    IndentCode = Database.myreader[0].ToString();
                Database.myreader.Close();

                //string str = "select CommodityName from commodity where companyname = '" + comboCompanyName.Text + "'  order by CommodityName";


                string str = "Select CommodityName from Commodity where companyname = '" + comboCompanyName.Text
                        + "' and (('" + dateTimePicker1.Value.ToString("yyyy-MM-dd")
                        + "' between nFromDate and nToDate) or (nFromDate <= '"
                        + dateTimePicker1.Value.ToString("yyyy-MM-dd") + "' and nToDate is null )) "
                        + " order by CommodityName";

                DespatchCommonFunction.Loadcombo(cmbCommodity, str, "CommodityName", "CommodityName", Utility.DespacthConnectionString);


                Database.Closeconnection();
            }
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            //frmPrintChallan5A frm = new frmPrintChallan5A();
            //frm.ShowDialog();
            TabClass.OpenForm(new frmPrintChallan5A());

        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            frmPrintJobChallan5A frm = new frmPrintJobChallan5A();
            frm.ShowDialog();

        }

        private void textDuration_KeyPress(object sender, KeyPressEventArgs e)
        {
            Utility.AllowDecimalOnly(e);
        }

        private void textQty_KeyPress(object sender, KeyPressEventArgs e)
        {
            Utility.AllowFloatOnly(e);
        }

        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            //frmPrintJobChallan5A frm = new frmPrintJobChallan5A();
            //frm.Show();
            TabClass.OpenForm(new frmPrintJobChallan5A());
        }
        private void comboType_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboType.SelectedIndex == 0) //VAT
            {
                labelAddVat.Visible = true;
                textAddVat.Visible = true;
                labelVATCST.Text = " VAT %";
                textVat.Text = "4";
                textAddVat.Text = "1";
            }
            else if (comboType.SelectedIndex == 1) //CST
            {
                labelAddVat.Visible = false;
                textAddVat.Visible = false;
                labelVATCST.Text = " CST %";
                textVat.Text = "2";
                textAddVat.Text = "0";
            }
        }

        private double Calculation()
        {
            total = 0;
            TotalCount = 0;
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


                for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                {
                    if (dataGridView1.Rows[i].Cells[5].FormattedValue.ToString().Trim() != "")
                    {
                        total += Convert.ToDouble(dataGridView1.Rows[i].Cells[5].Value);
                        TotalCount += 1;
                        dataGridView1.Rows[i].Cells["Srno"].Value = i + 1;

                    }
                }

                //total = Math.Round(total, 0);

                ///ADD ERPAmoutnDigit
                ///Change by nikhil
                total = Math.Round(total, FrmMain.ERPAmoutnDigit);
                Excise = (total * Convert.ToDouble(textExDuty.Text)) / 100;
                Excise = Math.Round(Excise, 0);

                Educess = ((Convert.ToDouble(textEduCess.Text) * Excise) / 100);
                HiEduCess = ((Convert.ToDouble(textSecHiEdu.Text) * Excise) / 100);
                Educess = Math.Round(Educess, 0);
                HiEduCess = Math.Round(HiEduCess, 0);

                Vat = ((total + Excise + Educess + HiEduCess) * Convert.ToDouble(textVat.Text)) / 100;
                AddVat = ((total + Excise + Educess + HiEduCess) * Convert.ToDouble(textAddVat.Text)) / 100;

                Vat = Math.Round(Vat, 0);
                AddVat = Math.Round(AddVat, 0);

                total = total + Educess + HiEduCess + Vat + AddVat + Excise;


                //total = Math.Round(total, 0);
                ///ADD ERPAmoutnDigit
                ///Change by nikhil

                total = Math.Round(total, FrmMain.ERPAmoutnDigit);
            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
            }
            return total;
        }


        private double CalculationGST()
        {
            total = 0;
            TotalCount = 0;
            double dblCGSTamt = 0;
            double dblSGSTAmt = 0;
            double dblIGSTAmt = 0;

            try
            {

                textExDuty.Text = "0";
                textEduCess.Text = "0";
                textSecHiEdu.Text = "0";
                textVat.Text = "0";
                textAddVat.Text = "0";



                for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                {

                    dataGridView1.Rows[i].Cells[Column6.Index].Value =
                     Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[Column3.Index].Value) *
                     Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[DUnitPrice.Index].Value);

                    total += Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[Column6.Index].Value);
                    //total = Math.Round(total, 0, MidpointRounding.AwayFromZero);

                    ///ADD ERPAmoutnDigit
                    ///Change by nikhil
                    total = Math.Round(total, FrmMain.ERPAmoutnDigit, MidpointRounding.AwayFromZero);
                    TotalCount += 1;
                    dataGridView1.Rows[i].Cells["Srno"].Value = i + 1;
                    //colCGSTrate colCGSTamt  colSGSTRate   ColSGSTAmt
                    //double dbltotal = Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[Column6.Index].Value);
                    //double dblCGSTrate = Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[colCGSTPer.Index].Value);

                    //dataGridView1.Rows[i].Cells[colCGSTAmt.Index].Value = Math.Round((dbltotal) * dblCGSTrate / 100, 2);
                    //dblCGSTamt += Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[colCGSTAmt.Index].Value);


                    //double dblSGSTRate = Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[colSGSTPer.Index].Value);
                    //dataGridView1.Rows[i].Cells[colSGSTAmt.Index].Value =
                    //    Math.Round((dbltotal) * dblSGSTRate / 100, 2);

                    //dblSGSTAmt += Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[colSGSTAmt.Index].Value);

                    //double dblIGSTRate = Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[ColIGSTPer.Index].Value);
                    //dataGridView1.Rows[i].Cells[ColIGSTAmt.Index].Value =
                    //    Math.Round((dbltotal) * dblIGSTRate / 100, 2);

                    //dblIGSTAmt += Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[ColIGSTAmt.Index].Value);

                    ///ADD ERPAmoutnDigit
                    ///Change by nikhil

                    double dbltotal = Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[Column6.Index].Value);
                    double dblCGSTrate = Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[colCGSTPer.Index].Value);

                    dataGridView1.Rows[i].Cells[colCGSTAmt.Index].Value = Math.Round((dbltotal) * dblCGSTrate / 100, FrmMain.ERPAmoutnDigit);
                    dblCGSTamt += Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[colCGSTAmt.Index].Value);


                    double dblSGSTRate = Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[colSGSTPer.Index].Value);
                    dataGridView1.Rows[i].Cells[colSGSTAmt.Index].Value =
                        Math.Round((dbltotal) * dblSGSTRate / 100, FrmMain.ERPAmoutnDigit);

                    dblSGSTAmt += Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[colSGSTAmt.Index].Value);

                    double dblIGSTRate = Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[ColIGSTPer.Index].Value);
                    dataGridView1.Rows[i].Cells[ColIGSTAmt.Index].Value =
                        Math.Round((dbltotal) * dblIGSTRate / 100, FrmMain.ERPAmoutnDigit);

                    dblIGSTAmt += Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[ColIGSTAmt.Index].Value);




                }

                //total = Math.Round(total, 0);
                //total = Math.Round(total + dblSGSTAmt + dblCGSTamt + dblIGSTAmt, 0);
                //total = Math.Round(total, 0);

                ///ADD ERPAmoutnDigit
                ///Change by nikhil

                total = Math.Round(total, FrmMain.ERPAmoutnDigit);
                total = Math.Round(total + dblSGSTAmt + dblCGSTamt + dblIGSTAmt, FrmMain.ERPAmoutnDigit);
                total = Math.Round(total, FrmMain.ERPAmoutnDigit);

            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
            }
            return total;
        }

        private void btnCalc_Click(object sender, EventArgs e)
        {
            if (dateTimePicker1.Value > Convert.ToDateTime("2017-06-30 00:00:00"))
            {
                CalculationGST();
            }
            else
            {
                Calculation();
            }
        }
        private void comboConsigneeName_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (Database.OpenConnection(Utility.DespacthConnectionString))
                {
                    comboConsigneeCity.Items.Clear();
                    comboConsigneeCity.Text = "";
                    string strsql = "";
                    strsql = "select City from Consignee where consigneename = '" + comboConsigneeName.Text + "' and companycode ='" + CompanyCode + "'";
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
                    Database.myreader = Database.GetExecuteReaderCommand("select ConsigneeAdd from Consignee where consigneename = '"
                       + comboConsigneeName.Text + "' and companycode ='" + CompanyCode + "' and city = '" + comboConsigneeCity.Text + "' order by ConsigneeCode desc");
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

        private void dataGridView1_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            try
            {
                if (dataGridView1.CurrentCell.ColumnIndex == Column1.Index) // For Commodity ComboBox
                {
                    comboBox = e.Control as ComboBox;
                    comboBox.SelectedIndexChanged += new EventHandler(comboBox_SelectedIndexChanged);
                }
                if (this.dataGridView1.CurrentCellAddress.X == ColItemCode.Index)
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
                ex.Message.ToString();
            }
        }
        void comboBox_SelectedIndexChanged(object sender, EventArgs e)
        {

            try
            {
                if (dataGridView1.CurrentCell.ColumnIndex == Column1.Index) // For Commodity ComboBox
                {
                    ComboBox cb = ((ComboBox)sender);
                    Database.OpenConnection(Utility.DespacthConnectionString);
                    Database.myreader = Database.GetExecuteReaderCommand("select THeading from commodity where CommodityName ='"
                              + cb.Text + "' and companyname = '" + comboCompanyName.Text + "'");
                    while (Database.myreader.Read())
                    {
                        string Theading = "";
                        Theading = Convert.ToString(Database.myreader[0]);
                        if (Theading.Length > 0 || Theading.Length > 3)
                        {
                            if (dateTimePicker1.Value.Date >= Convert.ToDateTime("2021-04-01"))
                                dataGridView1.Rows[dataGridView1.CurrentRow.Index].Cells[Column7.Index].Value = Theading;
                            else
                                dataGridView1.Rows[dataGridView1.CurrentRow.Index].Cells[Column7.Index].Value = Theading.Substring(0, 4);
                        }
                        else
                            dataGridView1.Rows[dataGridView1.CurrentRow.Index].Cells[Column7.Index].Value = Theading;
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

        private void dataGridView1_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            #region
            try
            {

                //if (!IsUpdate)
                {
                    if (dataGridView1.IsCurrentCellDirty)
                    {

                        dataGridView1.CommitEdit(DataGridViewDataErrorContexts.Commit);
                        if (dataGridView1.CurrentRow.Cells.IndexOf(dataGridView1.CurrentCell) == Column1.Index)
                        {
                            if (Database.OpenConnection(Utility.DespacthConnectionString))
                            {
                                string Commodity = Convert.ToString(dataGridView1.Rows[dataGridView1.CurrentRow.Index].Cells[Column1.Index].Value);
                                string THeading = "";
                                Database.OpenConnection(Utility.DespacthConnectionString);
                                Database.myreader = Database.GetExecuteReaderCommand("Select top 1 THeading,NCGSTRate,NSGSTRate,NIGSTRate from CommodityHSNGST where companyname = '"
                                    + comboCompanyName.Text + "' and CommodityName='" + Commodity + "' and (('" + dateTimePicker1.Value.ToString("yyyy-MM-dd")
                                    + "' between nFromDate and nToDate) or (nFromDate <= '" + dateTimePicker1.Value.ToString("yyyy-MM-dd") + "' and nToDate is null )) ");
                                if (Database.myreader.Read())
                                {
                                    THeading = Database.myreader[0].ToString();
                                    if (THeading.Length > 0 || THeading.Length > 3)
                                        if (dateTimePicker1.Value.Date >= Convert.ToDateTime("2021-04-01"))
                                            dataGridView1.Rows[dataGridView1.CurrentRow.Index].Cells[Column7.Index].Value = THeading;
                                        else
                                            dataGridView1.Rows[dataGridView1.CurrentRow.Index].Cells[Column7.Index].Value = THeading.Substring(0, 4);
                                    else
                                        dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[Column7.Index].Value = "";

                                    if (comboState.Text.ToUpper() == "Gujarat".ToUpper())
                                    {
                                        dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[colCGSTPer.Index].Value = Database.myreader["NCGSTRate"].ToString();
                                        dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[colSGSTPer.Index].Value = Database.myreader["NSGSTRate"].ToString();
                                        dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[ColIGSTPer.Index].Value = 0;
                                    }
                                    else
                                    {
                                        dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[colCGSTPer.Index].Value = 0;
                                        dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[colSGSTPer.Index].Value = 0;
                                        dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[ColIGSTPer.Index].Value = Database.myreader["NIGSTRate"].ToString();
                                    }
                                }
                                Database.myreader.Close();
                                if (THeading.Length > 0)
                                {
                                    //Commodify_ExciseRate
                                    Database.myreader = Database.GetExecuteReaderCommand("Select top 1 ExicseDuty from Commodify_ExciseRate where THeading='" + THeading
                                        + "' and convert(char,cast('" + dateTimePicker1.Value.ToString("yyyy-MM-dd") + " 00:00:00' as date),102) >= convert(char,Fromdate,102)");

                                    if (Database.myreader.Read())
                                    {
                                        textExDuty.Text = Database.myreader[0].ToString();
                                        Database.myreader.Close();
                                    }
                                }

                                Database.Closeconnection();
                            }

                        }
                        else if (dataGridView1.CurrentRow.Cells.IndexOf(dataGridView1.CurrentCell) == colMainGrp.Index) //Added Main Group from Item Master
                        {
                            string deptt = Convert.ToString(dataGridView1.Rows[dataGridView1.CurrentRow.Index].Cells[colMainGrp.Index].Value);
                            DataTable dt = new DataTable();
                            dt = dtIMaster.Copy();
                            DataGridViewComboBoxCell Col11 = (DataGridViewComboBoxCell)dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[ColItemCode.Index];
                            dt.DefaultView.RowFilter = "Deptt='" + deptt + "'";
                            if (dt.DefaultView.ToTable(true, "ItemCode").Rows.Count > 0)
                            {
                                Col11.DataSource = dt.DefaultView.ToTable();
                                Col11.ValueMember = dt.Columns[0].ColumnName.ToString();
                                Col11.DisplayMember = dt.Columns[1].ColumnName.ToString();
                            }
                        }
                        else if (dataGridView1.CurrentRow.Cells.IndexOf(dataGridView1.CurrentCell) == ColItemCode.Index)
                        {
                            string itemcode = Convert.ToString(dataGridView1.Rows[dataGridView1.CurrentRow.Index].Cells[ColItemCode.Index].Value);
                            string strsql = "SELECT  distinct Split.a.value('.', 'VARCHAR(200)') AS Commodity from " +
                                " (select CAST ('<M>' + REPLACE(REPLACE(i.Commodity,'&','&#38;'), ',', '</M><M>') + '</M>' AS XML) AS Commodity from MaterialProcessing..Item i " +
                                " where i.itemcode='" + itemcode + "' and i.CompanyName='" + comboCompanyName.Text + "' and isnull(i.Commodity,'')<>'') AS A " +
                                " CROSS APPLY Commodity.nodes ('/M') AS Split(a) order by 1 ";

                            DataTable dt = Database.GetDataTable(strsql);
                            DataGridViewComboBoxCell colComm = (DataGridViewComboBoxCell)dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[Column1.Index];
                            colComm.DataSource = dt;
                            colComm.ValueMember = dt.Columns[0].ColumnName.ToString();
                            colComm.DisplayMember = dt.Columns[0].ColumnName.ToString();
                        }
                    }
                }


            }
            catch (Exception ex)
            {
                ex.ToString();
            }
            #endregion
        }

        private void textChallanCode_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            try
            {
                if (textChallanCode.Text.Length == 0)
                    return;

                string BuyerName = "";
                string ConsigneeName = "";
                string ConsigneeCity = "";
                string Consignee = "";
                string companyName = "";
                bool IsCheckIsurance = false;
                if (Database.OpenConnection(Utility.DespacthConnectionString))
                {
                    fillData(true);
                    Database.myreader = Database.GetExecuteReaderCommand("Select * from Challan5A with (NOLOCK) where ChallanNo ='"
                                                     + textChallanCode.Text + "'");
                    if (Database.myreader.Read())
                    {
                        IsUpdate = true;
                        dateTimePicker1.Value = Convert.ToDateTime(Database.myreader[5].ToString());
                        textNatureProcessing.Text = Database.myreader[6].ToString();
                        textDuration.Text = Database.myreader[7].ToString();
                        textAnnexNo.Text = Database.myreader[8].ToString();
                        textVehicalNo.Text = Database.myreader[9].ToString();
                        BuyerName = Database.myreader[10].ToString();
                        companyName = Database.myreader[15].ToString();
                        textSuffix.Text = Database.myreader[16].ToString();
                        comboTranporterName.Text = Database.myreader[19].ToString();
                        textBoxLRNo.Text = Database.myreader[20].ToString();
                        dateTimePickerLRNo.Value = Convert.ToDateTime(Database.myreader[21].ToString());
                        textExDuty.Text = Database.myreader[22].ToString();
                        textEduCess.Text = Database.myreader[23].ToString();
                        textSecHiEdu.Text = Database.myreader[24].ToString();
                        textVat.Text = Database.myreader[25].ToString();
                        textAddVat.Text = Database.myreader[26].ToString();
                        comboType.Text = Database.myreader[32].ToString();
                        TxtEwayNo.Text = Convert.ToString(Database.myreader["EWayBillNo"]);
                        TxtEwayNo.Enabled = true;
                        textRemarks.Text = Convert.ToString(Database.myreader["Remarks"]);
                        if (TxtEwayNo.Text.Length > 5)
                        {
                            TxtEwayNo.Enabled = false;
                        }
                        ConsigneeName = Database.myreader[34].ToString();
                        ConsigneeCity = Database.myreader[35].ToString();
                        Consignee = Database.myreader[36].ToString();

                        cmbCommodity.Text = Convert.ToString(Database.myreader["FGCommodity"]);
                        if (Database.myreader[40].ToString() == "yes")
                            IsCheckIsurance = true;
                        btnOK.Enabled = false;
                        btnUpdate.Enabled = true;
                    }
                    Database.myreader.Close();

                    comboCompanyName.Text = companyName;
                    if (!comboBuyerName.Items.Contains(BuyerName))
                        comboBuyerName.Items.Add(BuyerName);
                    comboBuyerName.Text = BuyerName;
                    comboConsigneeName.Text = ConsigneeName;
                    comboConsigneeCity.Text = ConsigneeCity;
                    textConsignee.Text = Consignee;
                    if (IsCheckIsurance)
                        checkInsurance.Checked = true;
                    else
                        checkInsurance.Checked = false;
                    dataGridView1.Rows.Clear();

                    int i = 0;
                    Database.OpenConnection(Utility.DespacthConnectionString);


                    object obj = Database.GetExecuteScalarCommand_return("Select count(*) from ChallanItem with (NOLOCK) where Code ='"
                                                     + textChallanCode.Text + "'");
                    int count = Convert.ToInt32(obj);
                    if (count > 0)
                        dataGridView1.Rows.Add(count);

                    Database.myreader = Database.GetExecuteReaderCommand("Select Code, DescGoods, Identification, Qty, QtyDesc, Unit, Valu, TariffClassS, SubCode,CGSTrate,CGSTAmt,SGSTrate,SGSTAmt,IGSTrate,IGSTAmt,isnull(ItemCode,'') as ItemCode ,isnull(UnitRate,0) as UnitRate "
                                + " from ChallanItem  with (NOLOCK) where Code ='" + textChallanCode.Text + "'");

                    while (Database.myreader.Read())
                    {
                        IsUpdate = true;

                        dataGridView1.Rows[i].Cells[Column1.Index].Value = Convert.ToString(Database.myreader["DescGoods"]);
                        dataGridView1.Rows[i].Cells[Column2.Index].Value = Convert.ToString(Database.myreader["Identification"]);
                        dataGridView1.Rows[i].Cells[Column3.Index].Value = Convert.ToString(Database.myreader["Qty"]);
                        dataGridView1.Rows[i].Cells[Column4.Index].Value = Convert.ToString(Database.myreader["QtyDesc"]);
                        dataGridView1.Rows[i].Cells[Column5.Index].Value = Convert.ToString(Database.myreader["Unit"]);
                        dataGridView1.Rows[i].Cells[Column6.Index].Value = Convert.ToString(Database.myreader["Valu"]);
                        dataGridView1.Rows[i].Cells[Column7.Index].Value = Convert.ToString(Database.myreader["TariffClassS"]);
                        dataGridView1.Rows[i].Cells[Srno.Index].Value = Convert.ToString(Database.myreader["SubCode"]);
                        dataGridView1.Rows[i].Cells[colCGSTPer.Index].Value = Convert.ToString(Database.myreader["CGSTrate"]);
                        dataGridView1.Rows[i].Cells[colCGSTAmt.Index].Value = Convert.ToString(Database.myreader["CGSTAmt"]);
                        dataGridView1.Rows[i].Cells[colSGSTPer.Index].Value = Convert.ToString(Database.myreader["SGSTrate"]);
                        dataGridView1.Rows[i].Cells[colSGSTAmt.Index].Value = Convert.ToString(Database.myreader["SGSTAmt"]);
                        dataGridView1.Rows[i].Cells[ColIGSTPer.Index].Value = Convert.ToString(Database.myreader["IGSTrate"]);
                        dataGridView1.Rows[i].Cells[ColIGSTAmt.Index].Value = Convert.ToString(Database.myreader["IGSTAmt"]);
                        dataGridView1.Rows[i].Cells[colMainGrp.Index].Value = DespatchCommonFunction.GetDeptt(Database.myreader["ItemCode"].ToString());
                        dataGridView1.Rows[i].Cells[ColItemCode.Index].Value = Convert.ToString(Database.myreader["ItemCode"]);
                        if (Utility.SafeConvertToDouble(Database.myreader["UnitRate"].ToString()) == 0)
                        {
                            if (Utility.SafeConvertToDouble(Database.myreader["Qty"].ToString()) != 0 &&
                                Utility.SafeConvertToDouble(Database.myreader["Valu"].ToString()) != 0)
                            {

                                dataGridView1.Rows[i].Cells[DUnitPrice.Index].Value = Math.Round(
                                Utility.SafeConvertToDouble(Database.myreader["Valu"].ToString()) / Utility.SafeConvertToDouble(Database.myreader["Qty"].ToString()), 4);

                            }
                        }
                        else
                        {
                            dataGridView1.Rows[i].Cells[DUnitPrice.Index].Value = Utility.SafeConvertToDouble(Database.myreader["UnitRate"].ToString());
                        }
                        btnOK.Enabled = false;
                        btnUpdate.Enabled = true;
                        i++;
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

        private void tsbtnAttachment_Click(object sender, EventArgs e)
        {
            if (this.textChallanCode.Text.Length > 0)
            {
                Tools.DMSManager.OpenReference("DS_JW", string.Format("{0}/{1}/DS_JW/{2}",
                    AccountCommonFunction.GetCompanyCode(this.comboCompanyName.Text),
                    this.textSuffix.Text,
                    this.textChallanCode.Text
                    ));
            }
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            try
            {
                if (textChallanCode.Text.Length == 0)
                {
                    MessageBox.Show("Please Enter Job work number first");
                    return;
                }
                if (TxtEwayNo.Text.Length < 10)
                {
                    MessageBox.Show("Please Enter Proper E-Way No :");
                    return;
                }
                if (TxtEwayNo.Text.ToUpper().Replace(" ", "") == textChallanCode.Text.ToUpper().Replace(" ", ""))
                {
                    MessageBox.Show("e-way no shound not same with Jobwork no");
                    return;
                }
                if (TxtEwayNo.Text.ToUpper().Replace(" ", "") == "")
                {
                    MessageBox.Show("e-way no shound not be Blank");
                    return;
                }

                if (Database.OpenConnection(Utility.DespacthConnectionString))
                {
                    string sql = "update Challan5A set EWayBillNo='" + TxtEwayNo.Text + "' where ChallanNo = '" + textChallanCode.Text + "'";
                    Database.GetExecuteNonQueryCommand(sql);
                }
                MessageBox.Show("e-way Bill No Updated.");

            }
            catch (Exception EX)
            {
                MessageBox.Show("Problem with e-Way Bill No :" + EX.Message);
            }
        }

        private void dateTimePicker1_ValueChanged(object sender, EventArgs e)
        {

            try
            {
                DespatchCommonFunction.LoadCustomerUnlockChallan(comboBuyerName, comboCompanyName.Text, dateTimePicker1.Value);
                string gstin = "";
                textRemarks.Text = "";
                using (System.Data.SqlClient.SqlConnection con = new System.Data.SqlClient.SqlConnection(Utility.MaterialConnectionString))
                {
                    con.Open();
                    System.Data.SqlClient.SqlCommand command = new System.Data.SqlClient.SqlCommand("select NewGSTNo FROM LEDGERMASTER WHERE LEDGERNAME='"
                        + comboBuyerName.Text + "' AND COMPANYNAME='" + comboCompanyName.Text + "'", con);

                    System.Data.SqlClient.SqlDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        gstin = reader[0].ToString();
                    }
                    reader.Close();
                }
                using (System.Data.SqlClient.SqlConnection con = new System.Data.SqlClient.SqlConnection(Utility.DespacthConnectionString))
                {

                    con.Open();
                    System.Data.SqlClient.SqlCommand command = new System.Data.SqlClient.SqlCommand("select * from JobWorkRemarks where CompanyName='" + comboCompanyName.Text + "' and " +
                    "GSTIN ='" + gstin + "' and (('" + dateTimePicker1.Value.ToString("yyyy-MM-dd") + "' BETWEEN Fromdate  and Todate ) OR (" +
                    " FROMDATE <='" + dateTimePicker1.Value.ToString("yyyy-MM-dd") + "' AND TODATE IS NULL))", con);

                    System.Data.SqlClient.SqlDataReader reader = command.ExecuteReader();

                    while (reader.Read())
                    {
                        textRemarks.Text = reader["JobWorkRemarks"].ToString();
                    }
                    reader.Close();
                }

            }
            catch (Exception ex)
            {

            }

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


        private void comboBuyerName_Leave(object sender, EventArgs e)
        {
            try
            {
                string gstin = "";
                textRemarks.Text = "";
                if (Database.OpenConnection(Utility.MaterialConnectionString))
                {
                    gstin = Convert.ToString(Database.GetExecuteNonQueryCommand_retrun("select NewGSTNo FROM LEDGERMASTER WHERE LEDGERNAME='" + comboBuyerName.Text
                        + "' AND COMPANYNAME='" + comboCompanyName.Text + "'"));
                }
                using (System.Data.SqlClient.SqlConnection con = new System.Data.SqlClient.SqlConnection(Utility.DespacthConnectionString))
                {

                    con.Open();
                    System.Data.SqlClient.SqlCommand command = new System.Data.SqlClient.SqlCommand("select * from JobWorkRemarks where CompanyName='" + comboCompanyName.Text + "' and " +
                    "GSTIN ='" + gstin + "' and (('" + dateTimePicker1.Value.ToString("yyyy-MM-dd") + "' BETWEEN Fromdate  and Todate ) OR (" +
                    " FROMDATE <='" + dateTimePicker1.Value.ToString("yyyy-MM-dd") + "' AND TODATE IS NULL))", con);

                    System.Data.SqlClient.SqlDataReader reader = command.ExecuteReader();

                    while (reader.Read())
                    {
                        textRemarks.Text = reader["JobWorkRemarks"].ToString();
                    }
                    reader.Close();
                }

            }
            catch (Exception ex)
            {

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