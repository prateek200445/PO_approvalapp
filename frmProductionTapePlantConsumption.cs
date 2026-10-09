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
    public partial class frmProductionTapePlantConsumption : Form
    {
        bool IsTranStarted = false;
        public double dbltranNetwt = 0;
        //Table Name: TapePlantConsumption
        //fSweepingWastage float Checked
        string itemcode = "";
        public bool isclear = false;
        public System.Windows.Forms.StatusBarPanel pfrm_sbp1 = null;
        //int TapePlantSrNo = 0;
        int GroupSrNo = 0;
        DataTable dtOrderMaster;
        bool IsReturn = false;
        public frmProductionTapePlantConsumption()
        {
            try
            {
                InitializeComponent();
                Form_Load();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }
        public frmProductionTapePlantConsumption(string plantname, DateTime sysdate)
        {
            try
            {
                IsTranStarted = true;
                InitializeComponent();
                Form_Load();
                dateSysdate.Value = sysdate.Date;
                dateSysdate.Enabled = false;
                comboPlant.Text = plantname;

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }
        private void Form_Load()
        {
            try
            {
                lvTextFile.Visible = false;
                //dateSysdate.CustomFormat = "MM/dd/yyyy";
                dateSysdate.CustomFormat = Tools.ToolsCommonFunction.ERPDateSettings();
                dateSysdate.Format = DateTimePickerFormat.Custom;
                dateSysdate.MaxDate = DateTime.Now;
                dateSysdate.MinDate = ProductionCommonFunction.ERPBackDateSettings();

                //dateTimePicker2.CustomFormat = "MM/dd/yyyy";
                dateTimePicker1.Format = DateTimePickerFormat.Custom;
                dateTimePicker1.CustomFormat = Tools.ToolsCommonFunction.ERPDateSettings();
                dateTimePicker1.MaxDate = DateTime.Now;

                dateTimePicker2.Format = DateTimePickerFormat.Custom;
                dateTimePicker2.CustomFormat = Tools.ToolsCommonFunction.ERPDateSettings();
                dateTimePicker2.MaxDate = DateTime.Now;

                dateTimeIn.CustomFormat = "HH:mm:ss";
                dateTimeIn.Format = DateTimePickerFormat.Custom;

                dateTimeOut.CustomFormat = "HH:mm:ss";
                dateTimeOut.Format = DateTimePickerFormat.Custom;

                btnUpdate.Enabled = false;

                if (Database.OpenConnection(Utility.MaterialConnectionString))
                {

                    comboCompanyName.DataSource = ProductionCommonFunction.GetCompanyList();
                    comboCompanyName.ValueMember = ProductionCommonVariables.FieldName.CompanyName;
                    comboCompanyName.DisplayMember = ProductionCommonVariables.FieldName.CompanyName;
                    comboCompanyName.Text = frmDefaultVale.CompanyName;
                    comboCompanyName.Enabled = false;

                    if (FrmMain.IsProductionadmin)
                    {
                        comboPlant.DataSource = ProductionCommonFunction.GetPlantList(comboCompanyName.Text);
                        comboPlant.DisplayMember = ProductionCommonVariables.FieldName.PlantName;
                        comboPlant.ValueMember = ProductionCommonVariables.FieldName.PlantName;

                    }
                    else
                    {
                        comboPlant.DataSource = ProductionCommonFunction.GetPlantList_userWise(comboCompanyName.Text, FrmMain.UserName);
                        comboPlant.DisplayMember = ProductionCommonVariables.FieldName.PlantName;
                        comboPlant.ValueMember = ProductionCommonVariables.FieldName.PlantName;

                    }
                    if (comboPlant.Items.Count > 0)
                        comboPlant.SelectedIndex = -1;

                    dtOrderMaster = ProductionCommonFunction.GetFIBCPartyName(comboCompanyName.Text);
                    comboBuyerName.DataSource = dtOrderMaster.DefaultView.ToTable(true, "PartyName").Copy();
                    comboBuyerName.DisplayMember = "PartyName";
                    comboBuyerName.ValueMember = "PartyName";

                    if (comboBuyerName.Items.Count > 0)
                        comboBuyerName.SelectedIndex = -1;

                    Database.myreader = Database.GetExecuteReaderCommand("select SectorName from sectormaster  order by sectorname");
                    while (Database.myreader.Read())
                        comboSector.Items.Add(Database.myreader[0].ToString());
                    Database.myreader.Close();
                    if (comboWareHosueName.Items.Count > 0)
                        comboWareHosueName.SelectedIndex = 0;
                    if (!this.IsTranStarted) //23.12.2021
                        Database.Closeconnection();
                }
                BindGrid();
                comboShift.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
            // comboBuyerOrderNO.Enabled = false;
        }

        private void BindGrid()
        {
            try
            {
                DateTime currentdatetime = dateTimePicker1.Value;
                DateTime firstdate = new DateTime(currentdatetime.Year, currentdatetime.Month, 1);
                if (Database.OpenConnection_Adapter(Utility.MaterialConnectionString))
                {
                    Database.myadapter = Database.GetAdapterCommand("select * from TapePlantConsumption with (nolock)  where companyname = '"
                    + comboCompanyName.Text + "' and plantname = '" + comboPlant.Text + "' and CAST(FLOOR(CAST(sysdate AS float)) AS datetime)  between   '" + currentdatetime.AddDays(-30).ToString("yyyy-MM-dd") + "'  and  '" + currentdatetime.ToString("yyyy-MM-dd") + "'  order by GroupSRno desc");
                    DataSet dataset1 = new DataSet();
                    Database.myadapter.Fill(dataset1);
                    dataGrid1.DataSource = dataset1.Tables[0];
                }
            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
            }
        }

        void fillData()
        {

            if (Database.OpenConnection(Utility.MaterialConnectionString))
            {
                Database.myadapter = Database.GetAdapterCommand("select distinct subgroupname,ItemName from warehouse with (nolock)  where Deptt in ('RM','SF','FG') and companyname = '"
                    + comboCompanyName.Text + "' and warehousename = '" + comboWareHosueName.Text + "' and itemcode in ( select itemcode from item where companyname='" + comboCompanyName.Text + "' and "
                    + "(('" + dateTimePicker1.Value.ToString("yyyy-MM-dd") + "' BETWEEN FROMDATE AND TODATE) OR (FROMDATE <='"
                    + dateTimePicker1.Value.ToString("yyyy-MM-dd") + "' AND TODATE IS NULL)))  order by subgroupname");
                DataTable dt = new DataTable();
                Database.myadapter.Fill(dt);
                DataTable dtTemp = new DataTable();
                dtTemp = dt.Copy();

                DtQuality.DataSource = dtTemp.DefaultView.ToTable(true, "subgroupname").Copy();
                DtQuality.ValueMember = "subgroupname";
                DtQuality.DisplayMember = "subgroupname";

                dtTemp.Dispose();
                dtTemp = dt.Copy();

                DtGrade.DataSource = dtTemp.DefaultView.ToTable(true, "ItemName").Copy(); ;
                DtGrade.ValueMember = "ItemName";
                DtGrade.DisplayMember = "ItemName";
                dtTemp.Dispose();

                //changed by Rikin on 23-Sep-2014 
                //}

                //if (Database.OpenConnection(Utility.MaterialConnectionString))
                //{
                //Database.myadapter = Database.GetAdapterCommand("select distinct(ItemName) from warehouse with (nolock) where  companyname = '"
                //+ frmDefaultVale.CompanyName + "' and warehousename = '" + comboWareHosueName.Text + "' order by itemname");
                //DataTable dt1 = new DataTable();
                //Database.myadapter.Fill(dt1);

                //DtGrade.DataSource = dt1;
                //DtGrade.ValueMember = dt1.Columns[0].ColumnName.ToString();
                //DtGrade.DisplayMember = dt1.Columns[0].ColumnName.ToString();
            }

            //Database.myadapter = Database.GetAdapterCommand("select ItemName from itemcodes where Deptt = 'Domestic Grade' order by itemname");
            //dt = new DataTable();
            //Database.myadapter.Fill(dt);
            //DtGrade.DataSource = dt;
            //DtGrade.ValueMember = dt.Columns[0].ColumnName.ToString();
            //DtGrade.DisplayMember = dt.Columns[0].ColumnName.ToString();


        }

        //private bool CheckPlantName(string PlantName)
        //{

        //    bool IsAvail = false;
        //    if (Database.OpenConnection(Utility.MaterialConnectionString))
        //    {
        //        Database.myreader = Database.GetExecuteReaderCommand("select Plantname from PlantMaster where Plantname = '"
        //         + PlantName + "' and companyname = '" + comboCompanyName.Text + "'");
        //        if (Database.myreader.Read())
        //            IsAvail = true;
        //        Database.myreader.Close();
        //        Database.Closeconnection();
        //    }
        //    return IsAvail;


        //}


        //private bool CheckPlantSubName(string PlantSubName)
        //{
        //    bool IsAvail = false;
        //    if (Database.OpenConnection(Utility.MaterialConnectionString))
        //    {
        //        Database.myreader = Database.GetExecuteReaderCommand("select PlantSubname from PlantSubMaster where PlantSubname = '"
        //         + PlantSubName + "' and companyname = '" + comboCompanyName.Text + "'");
        //        if (Database.myreader.Read())
        //            IsAvail = true;
        //        Database.myreader.Close();
        //        Database.Closeconnection();
        //    }
        //    return IsAvail;
        //}


        //private bool CheckPlantItemName(string PlantItemName)
        //{
        //    bool IsAvail = false;
        //    if (Database.OpenConnection(Utility.MaterialConnectionString))
        //    {

        //        Database.myreader = Database.GetExecuteReaderCommand("select itemname from warehouse where warehousename  = '"
        //         + comboWareHouseName_To.Text + "' and companyname = '" + comboCompanyName.Text + "' and itemname = '"
        //         + comboItemName.Text + "' union select plantitemname from plantitemmaster where companyname = '" + comboCompanyName.Text + "' and plantname = '"
        //        + comboPlant.Text + "'");
        //        if (Database.myreader.Read())
        //            IsAvail = true;
        //        Database.myreader.Close();
        //        Database.Closeconnection();
        //    }
        //    return IsAvail;
        //}

        //private bool CheckSectorName(string SectorName)
        //{
        //    bool IsAvail = false;
        //    if (Database.OpenConnection(Utility.MaterialConnectionString))
        //    {
        //        Database.myreader = Database.GetExecuteReaderCommand("select sectorname from sectormaster where sectorname = '"
        //         + SectorName + "'");
        //        if (Database.myreader.Read())
        //            IsAvail = true;
        //        Database.myreader.Close();
        //        Database.Closeconnection();
        //    }
        //    return IsAvail;
        //}
        public bool Validations()
        {
            try
            {
                //added by raj 22-05-2020 for re-check net production before validation 
                if (textNet.Text.Length == 0)
                {
                    MessageBox.Show("Please check Net Production..");
                    return false;
                }
                //end by raj
                int intApproval = 0;
                if (!this.IsTranStarted) //23.12.2021
                {
                    if (!Utility.CheckDateFrize_Production(dateSysdate.Value.ToString("yyyy-MM-dd"), comboCompanyName.Text))
                    {
                        MessageBox.Show("Entry on this Date is Freezed. So, You Can't do Entry.");
                        return false;
                    }
                }
                if (ComboProduType.Text == "")
                {
                    MessageBox.Show("Please enter Production type Value.");
                    return false;
                }
                if (comboBuyerName.SelectedIndex == -1)
                {
                    MessageBox.Show("Choose Party Name.");
                    return false;
                }
                if (comboBuyerOrderNO.SelectedIndex == -1)
                {
                    MessageBox.Show("Choose Buyer Order Number.");
                    return false;
                }
                if (comboMarketingInvNo.Text.Length == 0 || comboMarketingInvNo.SelectedIndex == -1)
                {
                    MessageBox.Show("Select Marketing Invoice No");
                    return false;
                }
                #region Check with Plant Item Name and grade should not be same
                for (int i = 0; i <= dataGridView1.Rows.Count - 1; i++)
                {
                    string grade = Convert.ToString(dataGridView1.Rows[i].Cells[DtGrade.Index].Value);
                    if (comboItemName.Text.ToLower() == grade.ToLower())
                    {
                        MessageBox.Show("Product Name and Grade should not be same");
                        return false;
                    }
                }
                #endregion
                if (Database.OpenConnection(Utility.MaterialConnectionString))
                {
                    #region Add ProfileFlag Table for checking Approval for company if FlagValue = 1 then it will check for Approvals
                    intApproval = Convert.ToInt32(Database.GetExecuteNonQueryCommand_retrun("select FlagValue from ProfileFlag where Flagname='IsApproval' and Companyname='" + comboCompanyName.Text + "'"));
                    #endregion
                    string strql = "";
                    double dblCnt = 0;
                    dblCnt = 0;
                    strql = "select count(*) from warehouse with (nolock) where itemname = '" + comboItemName.Text +
                        "' and companyname='" + comboCompanyName.Text + "' and WareHouseName='" + comboWareHouseName_To.Text + "'";
                    dblCnt = Utility.SafeConvertToDouble(Database.GetExecuteNonQueryCommand_retrun(strql));
                    if (dblCnt == 0)
                    {
                        return false;
                    }
                    if (intApproval == 1)
                    {
                        strql = string.Format("select count(*) from TapePlantConsumption where hodApprove='Pending' and PlantName='{0}' and sysdate='{1}' and companyname='{2}'",
                                        comboPlant.Text, dateSysdate.Value.AddDays(-1).ToString("yyyy-MM-dd"), comboCompanyName.Text);
                        dblCnt = Utility.SafeConvertToDouble(Database.GetExecuteNonQueryCommand_retrun(strql));
                        if (dblCnt > 0)
                        {
                            MessageBox.Show("Plant HOD Approval is pending.\nPlease Ask Plant HOD to Approve last date production & Consumption");
                            return false;
                        }

                        strql = string.Format("select count(*) from TapePlantConsumption where exciseapprove='Pending' and PlantName='{0}' and sysdate='{1}' and companyname='{2}'",
                                     comboPlant.Text, dateSysdate.Value.AddDays(-1).ToString("yyyy-MM-dd"), comboCompanyName.Text);
                        dblCnt = Utility.SafeConvertToDouble(Database.GetExecuteNonQueryCommand_retrun(strql));
                        if (dblCnt > 0)
                        {
                            MessageBox.Show("Excise Approval is pending.\nPlease Ask excise person to Approve last date production & Consumption");
                            return false;
                        }
                    }
                }
                if (Database.OpenConnection(Utility.MaterialConnectionString))
                {
                    string strql = "";
                    if (comboPlant.Text == "Lamination")
                    {
                        //added by raj 22-05-2020 for re-check net production before validation 
                        textNet.Text = Convert.ToString(Utility.SafeConvertToDouble(textProduction.Text) - Utility.SafeConvertToDouble(textWastage.Text) -
                        Utility.SafeConvertToDouble(txtTrimWastage.Text) - Utility.SafeConvertToDouble(txt_sweeping_wastage.Text) - Utility.SafeConvertToDouble(TxtFabricTrm.Text) -
                        Utility.SafeConvertToDouble(TxtFabricwaste.Text) - Utility.SafeConvertToDouble(TxtLumpsWs.Text));

                        //end by raj 22-05-2020 
                        //and  right(RollNo,1)!='D' 29.08.2020 allow 1 time Lam Roll weight for Double Lamination
                        //strql = "   select round(isnull(sum(netwt)-sum(unetwt2),0),2) from (select ROW_NUMBER () over(partition by Urollno order by urollno) as Sr,  case when ROW_NUMBER () over(partition by Urollno order by urollno) =1   then UNetWt else 0 end as UNetWt2, * "
                        //        + " from MISlaminationentry with (nolock) where companyname='" + comboCompanyName.Text
                        //        + "' and sysdate='" + dateSysdate.Value.ToString("yyyy-MM-dd") + "' and hodApprove='Approved' and " +
                        //        " Shift='" + comboShift.Text + "' and NetWt>0 ) as  S";

                        #region 2021.01.11  Changes in Lamincation entry, not allow same split rows to be same UL Netwt. as per entry needs to be change in consumption entry.
                        //strql = "   SELECT round(isnull(sum(netwt)-sum(unetwt2),0),2) FROM (select ROW_NUMBER () over(partition by Urollno order by urollno) as Sr,case when ROW_NUMBER () over(partition by Urollno order by urollno) =1   then TOTALULNETWT else 0 end as UNetWt2,* from (select SUM(UNetWt) OVER(PARTITION BY Urollno ORDER BY Urollno) TOTALULNETWT,* "
                        //        + " from MISlaminationentry with (nolock) where companyname='" + comboCompanyName.Text
                        //        + "' and sysdate='" + dateSysdate.Value.ToString("yyyy-MM-dd") + "' and hodApprove='Approved' and " +
                        //        " Shift='" + comboShift.Text + "' and NetWt>0 ) as  S ) as S";
                        ////Chage by raj on 13.12.2021 Change related to Only Entries done in Double Lamination for rolls then we will only consider Total Unetwt 
                        //strql = "   SELECT round(isnull(sum(netwt)-sum(unetwt2),0),2) FROM (select ROW_NUMBER () over(partition by Urollno order by urollno) as Sr, "
                        //    + " case when ROW_NUMBER () over(partition by Urollno order by urollno) =1   then TOTALULNETWT else 0 end as UNetWt2,* "
                        //    + " from (select case when FGItemCode is null then UNetWt else SUM(UNetWt) OVER(PARTITION BY Urollno ORDER BY Urollno) end TOTALULNETWT,* "
                        //    + " from MISlaminationentry with (nolock) where companyname='" + comboCompanyName.Text
                        //    + "' and sysdate='" + dateSysdate.Value.ToString("yyyy-MM-dd") + "' and hodApprove='Approved' and "
                        //    + " Shift='" + comboShift.Text + "' and NetWt>0 ) as  S ) as S";
                        //23.12.2021 Show Consumption form from Production Entry.
                        strql = "   SELECT round(isnull(sum(netwt)-sum(unetwt2),0),2) FROM (select ROW_NUMBER () over(partition by Urollno order by urollno) as Sr,case when ROW_NUMBER () over(partition by Urollno order by urollno) =1   then TOTALULNETWT else 0 end as UNetWt2,* from (select SUM(UNetWt) OVER(PARTITION BY Urollno ORDER BY Urollno) TOTALULNETWT,* "
                                + " from MISlaminationentry with (nolock) where companyname='" + comboCompanyName.Text
                                + "' and sysdate='" + dateSysdate.Value.ToString("yyyy-MM-dd") + "' and " +
                                " Shift='" + comboShift.Text + "' and NetWt>0 and Isnull(IsCons,0)=1 ) as  S ) as S";
                        //End BY Raj  23.12.2021 Show Consumption form from Production Entry.
                        double dblCnt = Utility.SafeConvertToDouble(Database.GetExecuteNonQueryCommand_retrun(strql));
                        if (Math.Round(Utility.SafeConvertToDouble(textNet.Text), FrmMain.ERPQtyDigit) != Math.Round(dblCnt, FrmMain.ERPQtyDigit))
                        {
                            MessageBox.Show("Total Consumption Entry for Lamination is not match with Lamination Production Entry");
                            return false;
                        }
                        #endregion
                    }
                    if (comboPlant.Text == "Liner Roll Plant")
                    {
                        strql = "   SELECT isnull(sum(netwt),0) as Netwt  "
                              + " from MISOutsideRollEntry with (nolock) where companyname='" + comboCompanyName.Text
                              + "' and sysdate='" + dateSysdate.Value.ToString("yyyy-MM-dd") + "' and   " +
                              " storeinwardno='Production' ";
                        double dblCnt = Utility.SafeConvertToDouble(Database.GetExecuteNonQueryCommand_retrun(strql));

                        //Added if any new production after consumption for same day then we will allow todo entry for consumotion of diffe.10.03.2022
                        strql = string.Format("select isnull(sum(Qty) - (sum(wastage)+ sum(fTrimWastage)+ sum(fSweepingWastage) 		+sum(fTapeOthersWastage) +sum(fFabricTrim)+sum(fFabricwaste)+sum(fNewLumpsWs)),0) from TapePlantConsumption where  PlantName='{0}' " +
                                     " and sysdate='{1}' and companyname='{2}'",
                                     comboPlant.Text, dateSysdate.Value.ToString("yyyy-MM-dd"), comboCompanyName.Text);

                        double TotalConsumtion = Utility.SafeConvertToDouble(Database.GetExecuteNonQueryCommand_retrun(strql));
                        if (Math.Round(Utility.SafeConvertToDouble(textNet.Text), FrmMain.ERPQtyDigit) != Math.Round(dblCnt - TotalConsumtion, FrmMain.ERPQtyDigit))
                        {
                            MessageBox.Show("Total Consumption Entry for Liner Roll Plant is not match with Production Entry");
                            return false;
                        }
                    }
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void frmProductionTapePlantConsumption_Load(object sender, EventArgs e)
        {
            if (!IsTranStarted)
                Form_Load();
        }
        private void btnSave_Click(object sender, EventArgs e)
        {
            try
            {
                if (Validations())
                {
                    if (textWastage.Text == "")
                        textWastage.Text = "0";
                    if (txt_sweeping_wastage.Text == "")
                        txt_sweeping_wastage.Text = "0";

                    //if (!comboPlant.Items.Contains(comboPlant.Text))
                    //    MessageBox.Show("Invalid Plant Name");
                    //else if (!comboPlantSubName.Items.Contains(comboPlantSubName.Text))
                    //    MessageBox.Show("Invalid Plant Sub  Name");
                    //else 
                    if (!comboItemName.Items.Contains(comboItemName.Text))
                        MessageBox.Show("Invalid Item Name");
                    else if (Convert.ToDouble(textWastage.Text) < 0)
                        MessageBox.Show("Wastage can't be negative, please update");
                    else if (!comboSector.Items.Contains(comboSector.Text))
                        MessageBox.Show("Invalid Sector Name");
                    else
                    {
                        bool isgrade = false;
                        DateTime dt = dateSysdate.Value;
                        DialogResult drs = MessageBox.Show("Do you want to save following information", "Save Information",
                             MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                        if (drs.CompareTo(DialogResult.Yes) == 0)
                        {
                            for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                            {
                                if (Database.OpenConnection(Utility.MaterialConnectionString))
                                {
                                    Database.myreader = Database.GetExecuteReaderCommand("select itemname from warehouse with (nolock) where  subgroupname = '" + dataGridView1[0, i].Value + "' and itemname = '"
                                         + dataGridView1[1, i].Value + "' and companyname = '" + comboCompanyName.Text + "' and warehousename = '" + comboWareHosueName.Text + "' ");
                                    if (!Database.myreader.Read())
                                    {
                                        isgrade = true;
                                    }
                                    Database.myreader.Close();
                                }
                            }
                            if (!isgrade)
                            {

                                double GroupSrNo = 0;
                                GroupSrNo = Utility.SafeConvertToDouble(Database.ExecuteScalar("select max(isnull(Groupsrno,0))+1 from TapePlantConsumption "));
                                bool isauthority = false;
                                string status = "";
                                Database.myreader = Database.GetExecuteReaderCommand("select plantname from plantmaster where companyname='"
                                    + comboCompanyName.Text + "' and plantname='" + comboPlant.Text + "' and (approvalauth1 = '"
                                    + FrmMain.UserName + "'or approvalauth2 = '"
                                    + FrmMain.UserName + "' or approvalauth3 = '"
                                    + FrmMain.UserName + "' or approvalauth4 = '" + FrmMain.UserName + "' or  approvalauth5 = '" + FrmMain.UserName + "')");
                                if (Database.myreader.Read())
                                    status = "Approved";
                                else
                                    status = "Pending";
                                Database.myreader.Close();
                                double stkinhand = 0;
                                double sum = 0;
                                double checksum = 0;
                                bool isallow = true;

                                //Ankit 12/08/2014
                                double wastage = 0;
                                double lumpsWastage = 0;
                                if (!this.IsTranStarted) //23.12.2021
                                    Database.BeginTransaction();
                                GroupSrNo = Utility.SafeConvertToDouble(Database.ExecuteScalar("select max(isnull(Groupsrno,0))+1 from TapePlantConsumption "));
                                for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                                {
                                    stkinhand = 0;
                                    Database.myreader = Database.GetExecuteReaderCommand("select round(stkinhand,2) from warehouse where warehousename = '"
                                     + comboWareHosueName.Text + "' and subgroupname = '" + dataGridView1[0, i].Value + "' and itemname = '"
                                     + dataGridView1[1, i].Value + "' and companyname = '" + comboCompanyName.Text + "' ");
                                    if (Database.myreader.Read())
                                    {
                                        stkinhand = Utility.SafeConvertToDouble(Database.myreader[0].ToString());
                                    }
                                    Database.myreader.Close();
                                    stkinhand = stkinhand - (Utility.SafeConvertToDouble(dataGridView1[2, i].FormattedValue));
                                    if (stkinhand < 0)
                                        isallow = false;

                                    checksum = checksum + Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[2].FormattedValue.ToString());
                                }
                                double totalwastage = 0;
                                if (isallow)
                                {
                                    if (checksum - Convert.ToDouble(textWastage.Text) >= 0)
                                    {
                                        for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                                        {
                                            if (dataGridView1.Rows[i].Cells[0].FormattedValue.ToString() != ""
                                                && dataGridView1.Rows[i].Cells[1].FormattedValue.ToString() != "")
                                            {
                                                stkinhand = 0;

                                                Database.myreader = Database.GetExecuteReaderCommand("select itemcode from warehouse with (nolock) where itemname = '"
                                                 + dataGridView1[1, i].Value + "'");
                                                if (Database.myreader.Read())
                                                {
                                                    itemcode = Database.myreader[0].ToString();
                                                }
                                                Database.myreader.Close();
                                                {

                                                    stkinhand = Convert.ToDouble(dataGridView1[2, i].Value);

                                                    ProductionCommonFunction.WareHouseStoreoutwards(itemcode, dataGridView1[1, i].Value.ToString(), stkinhand, dt, comboCompanyName.Text
                                                         , comboWareHosueName.Text, comboWareHouseName_To.Text);

                                                    Database.GetExecuteNonQueryCommand("update warehouse set stkinhand =  stkinhand -  " + stkinhand + "  where warehousename = '"
                                                 + comboWareHosueName.Text + "' and subgroupname = '" + dataGridView1[0, i].Value + "' and itemname = '"
                                                 + dataGridView1[1, i].Value + "' and companyname = '" + comboCompanyName.Text + "'");

                                                    string Approved = "Pending";
                                                    string dateOfApproval = "Null";

                                                    object ApprovalAuth = Database.GetExecuteScalarCommand_return("Select Approvalauth1 from WareHouseMaster where companyname='" + comboCompanyName.Text + "' and warehousename='" + comboWareHosueName.Text + "'");

                                                    string Approve = ApprovalAuth.ToString();

                                                    if (Approve == "")
                                                    {
                                                        Approved = "Approved";
                                                        dateOfApproval = "CAST(FLOOR(CAST(getdate() AS float)) AS datetime)";
                                                    }

                                                    //Rate base on FIFO
                                                    //double dblqty = Convert.ToDouble(dataGridView1.Rows[i].Cells[2].Value.ToString());
                                                    //double dblRate = ProductionCommonFunction.GettingRate(itemcode, dblqty);
                                                    //double ProdAmount = dblRate * dblqty;
                                                    ////Add Wastage cost per 1 kg = 15 rs.
                                                    //double dblwastagerate = 15;

                                                    double dblqty = Convert.ToDouble(dataGridView1.Rows[i].Cells[2].Value.ToString());
                                                    double dblRate = 0;// ProductionCommonFunction.GettingRate(itemcode, dblqty);
                                                    double ProdAmount = dblRate * dblqty;
                                                    //Add Wastage cost per 1 kg = 15 rs.
                                                    double dblwastagerate = 15;

                                                    dataGridView1.Rows[i].Cells[4].Value = dblRate;
                                                    dataGridView1.Rows[i].Cells[5].Value = ProdAmount;
                                                    //double dblWastageProd = dblwastagerate * Convert.ToDouble((textWastage.Text + txtTrimWastage.Text + txt_sweeping_wastage.Text));

                                                    if (Database.GetExecuteNonQueryCommand("insert into TapePlantConsumption (sysDate,PlantName,PlantSubName,PlantItemName,Quality,Grade, " +
                                                    " Shift,Sector,Qty,Wastage,GroupSrNo,InTime,OutTime,companyname,buyername,Marketinginvno,buyerdate,hodapprove,hodapproveDate,exciseapprove," +
                                                    " exciseapproveDate,itemcode,ProductionType,fTrimWastage,fSweepingWastage,fTapeLumpsWastage,fTapeOthersWastage,RateConsumption,RateWastage,RateProduction,fFabricTrim,fFabricwaste,fNewLumpsWs) values(' "
                                                    + dt.ToString("yyyy-MM-dd") + "', '" + comboPlant.Text + "', '" + comboPlantSubName.Text + "','"
                                                    + comboItemName.Text + "','"
                                                    + dataGridView1.Rows[i].Cells[0].Value.ToString() + "','"
                                                    + dataGridView1.Rows[i].Cells[1].Value.ToString() + "','"
                                                    + comboShift.Text + "','" + comboSector.Text + "',"
                                                    + Convert.ToDouble(dataGridView1.Rows[i].Cells[2].Value.ToString()) + ","
                                                    + textWastage.Text + ","
                                                    + GroupSrNo + ",'"
                                                    + dateTimeIn.Text + "','"
                                                    + dateTimeOut.Text + "','"
                                                    + comboCompanyName.Text + "','"
                                                    + comboBuyerName.Text + "','"
                                                    + comboMarketingInvNo.Text + "','"
                                                    + dateTimePicker2.Value.ToString("yyyy-MM-dd") + "','" + status + "',Null,'" + Approved + "',"
                                                    + dateOfApproval + ",'" + itemcode + "','"
                                                    + ComboProduType.Text + "',"
                                                    + (comboPlant.Text.ToLower() == "lamination" ? txtTrimWastage.Text : "0") + "," + txt_sweeping_wastage.Text + ","
                                                    + (comboPlant.Text.ToLower() == "tape plant" ? txtTrimWastage.Text : "0") + "," + txt_sweeping_wastage.Text + "," + dblRate + "," + dblwastagerate + "," + ProdAmount + ","
                                                    + Utility.SafeConvertToDouble(TxtFabricTrm.Text) + "," + Utility.SafeConvertToDouble(TxtFabricwaste.Text) + ","
                                                    + Utility.SafeConvertToDouble(TxtLumpsWs.Text) + ")"))
                                                        sum = sum + Convert.ToDouble(dataGridView1.Rows[i].Cells[2].Value.ToString());


                                                    if (i == 0)
                                                    {
                                                        wastage = Utility.SafeConvertToDouble(textWastage.Text);
                                                        lumpsWastage = Utility.SafeConvertToDouble(txtTrimWastage.Text);

                                                        totalwastage = Utility.SafeConvertToDouble(textWastage.Text) + Utility.SafeConvertToDouble(txtTrimWastage.Text)
                                                             + Utility.SafeConvertToDouble(txt_sweeping_wastage.Text) + Utility.SafeConvertToDouble(TxtFabricTrm.Text)
                                                             + Utility.SafeConvertToDouble(TxtFabricwaste.Text) + Utility.SafeConvertToDouble(TxtLumpsWs.Text);
                                                    }

                                                    textWastage.Text = "0";
                                                    txtTrimWastage.Text = "0";
                                                    txt_sweeping_wastage.Text = "0";
                                                    TxtFabricTrm.Text = "0";
                                                    TxtFabricwaste.Text = "0";
                                                    TxtLumpsWs.Text = "0";
                                                }
                                            }
                                        }
                                    }
                                    else
                                        MessageBox.Show("Net Production can't be negative");


                                }
                                else
                                {
                                    MessageBox.Show("One of Grade stock in warehouse is less than issuing stock so entry not saved");
                                    return;
                                }
                                if (sum > 0)
                                {
                                    // if (status == "Approved")
                                    //Commented By Rikin on 8-8-2014 as this if managed in above codition
                                    //Database.GetExecuteNonQueryCommand("update TapePlantConsumption set hodapprovedate = CAST(FLOOR(CAST(getdate() AS float)) AS datetime)" +
                                    //    " where groupsrno = " + GroupSrNo + " and companyname = '"
                                    //+ comboCompanyName.Text + "'");
                                    double ProRate = 0;
                                    for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                                    {
                                        ProRate += Convert.ToDouble(dataGridView1.Rows[i].Cells[5].Value);
                                    }
                                    double prodWastageRate = 0;
                                    prodWastageRate = Utility.SafeConvertToDouble(textWastage.Text) * 15;
                                    ProRate = ProRate - prodWastageRate;

                                    textProduction.Text = Convert.ToString(sum);
                                    //22-07-2020 now all wastage will be deduct for Net Production
                                    //if (lblTrimWastage.Text == "Lumps Wastage :")
                                    //    textNet.Text = Convert.ToString(Utility.SafeConvertToDouble(textProduction.Text) - wastage - lumpsWastage);
                                    //else
                                    //    textNet.Text = Convert.ToString(Utility.SafeConvertToDouble(textProduction.Text) - wastage);

                                    textNet.Text = Convert.ToString(Convert.ToDouble(textProduction.Text) - totalwastage);

                                    string warehousemaster = comboWareHouseName_To.Text;

                                    if (warehousemaster != "")
                                    {
                                        double warestkinhand = Convert.ToDouble(textNet.Text);

                                        string itmCode = "";

                                        Database.myreader = Database.GetExecuteReaderCommand("select itemcode from warehouse with (nolock) where itemname = '" + comboItemName.Text + "'");
                                        if (Database.myreader.Read())
                                        {
                                            itmCode = Database.myreader[0].ToString();
                                        }
                                        Database.myreader.Close();

                                        Database.GetExecuteNonQueryCommand("Insert into Prod_Transaction(CompanyName,Qty,FromGodown,ToGodown,ItemName,Sysdate,ItemCode,ProdAmount,tid) values('" + comboCompanyName.Text + "'," + warestkinhand + ",'" + comboPlant.Text +
                                            "','" + comboWareHouseName_To.Text + "','" + comboItemName.Text + "','" + dt.ToString("yyyy-MM-dd") + "','" + itmCode + "'," + ProRate + "," + Convert.ToInt64(GroupSrNo) + " )");

                                        // to get stock of raw material
                                        ProductionCommonFunction.WareHouseStoreinwards(itmCode, comboItemName.Text, warestkinhand, dt, comboCompanyName.Text
                                                        , comboWareHosueName.Text, comboWareHouseName_To.Text);


                                        if (comboPlant.Text == "Liner Roll Plant" || comboPlant.Text == "Lamination")
                                        {
                                            //24.05.2021 not allow to update in stkinhand for Lamination as we already get data from MIS Lamination Entry.
                                            //08.02.2022 add condition for Liner Roll Plant , already we added stock in RMD Godown for Rolls so no needs to add in LinerRollGdn
                                        }
                                        else
                                            Database.GetExecuteNonQueryCommand("update warehouse set stkinhand = stkinhand + " + warestkinhand + " where warehousename = '" + warehousemaster + "' and companyname = '"
                                            + comboCompanyName.Text + "' and itemname = '" + comboItemName.Text + "'");
                                    }

                                }


                                {
                                    // // to insert values in stock Journal table
                                    int srno = GetNewSrNo();
                                    int RecordLogId = AccountCommonFunction.SaveRecordLog(null, FrmMain.UserName, RecordLogFlag.Regular, string.Format("Create: Stock Journal {0} Saved in {1} for {2}", "", this.CompanyName, frmDefaultVale.Year));

                                    for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                                    {
                                        if (Database.OpenConnection(Utility.MaterialConnectionString))
                                        {
                                            if (i == 0)
                                            {
                                                string itemcode1 = "";
                                                Database.myreader = Database.GetExecuteReaderCommand("select itemcode from warehouse with (nolock) where itemname = '" + comboItemName.Text + "'");
                                                if (Database.myreader.Read())
                                                    itemcode1 = Database.myreader[0].ToString();
                                                Database.myreader.Close();

                                                if (comboPlant.Text != "Lamination") //25.07.2020 not allow Stock Productoin entry for Lamination as we already get data from MIS Lamination Entry.
                                                    ProductionCommonFunction.InsertStockJournal(srno, dt, comboCompanyName.Text, (ComboProduType.Text == "Outside Job" ? "Outside Production" : "Production"), comboItemName.Text,
                                                        Convert.ToDouble(textNet.Text), 0, 0, "KGS", frmDefaultVale.Year, RecordLogId, Convert.ToInt32(GroupSrNo), itemcode1, "..", comboWareHouseName_To.Text);


                                                if (comboPlant.Text != "Lamination")
                                                {
                                                    ProductionCommonFunction.InsertStockJournal(srno, dt, comboCompanyName.Text, (ComboProduType.Text == "Outside Job" ? "Outside Production" : "Production"), "Tape Wastage",
                                                       totalwastage, 0, 0, "KGS", frmDefaultVale.Year, RecordLogId, Convert.ToInt32(GroupSrNo), "WIP00031", "..", comboWareHouseName_To.Text); //change RAW01144 to WIP00031
                                                }
                                                else
                                                {
                                                    ProductionCommonFunction.InsertStockJournal(srno, dt, comboCompanyName.Text, (ComboProduType.Text == "Outside Job" ? "Outside Production" : "Production"), "Fabric Wastage",
                                                       totalwastage, 0, 0, "KGS", frmDefaultVale.Year, RecordLogId, Convert.ToInt32(GroupSrNo), "WIP00031", "..", comboWareHouseName_To.Text);// Change RAW00832 too WIP00031
                                                }
                                                //RAW00832	Fabric Wastage ,RAW01144	Tape Wastage
                                            }

                                            string itemcode2 = "";
                                            Database.myreader = Database.GetExecuteReaderCommand("select itemcode from warehouse with (nolock) where itemname = '"
                                                    + dataGridView1[1, i].Value + "'");
                                            if (Database.myreader.Read())
                                                itemcode2 = Database.myreader[0].ToString();
                                            Database.myreader.Close();

                                            ProductionCommonFunction.InsertStockJournal(srno, dt, comboCompanyName.Text, (ComboProduType.Text == "Outside Job" ? "Outside consumption" : "consumption"), dataGridView1.Rows[i].Cells[1].Value.ToString(),
                                                      Convert.ToDouble(dataGridView1.Rows[i].Cells[2].Value.ToString()), 0, 0, "KGS", frmDefaultVale.Year, RecordLogId, Convert.ToInt32(GroupSrNo), itemcode2, "..", comboWareHouseName_To.Text);


                                        }
                                    }
                                }
                                ///////////////////////////////////

                                if (!this.IsTranStarted)
                                {

                                    Database.CommitTransaction();
                                    Database.Closeconnection();
                                    Utility.UserInformation("Save", "Tape Plant Consumption No " + GroupSrNo.ToString() + " is saved");
                                    if (sum > 0)
                                    {
                                        MessageBox.Show("Plant Consumption is successfully added");
                                    }
                                    BindGrid();
                                    ClearAll(false);
                                }
                                else
                                {
                                    dbltranNetwt = Utility.SafeConvertToDouble(textNet.Text);
                                    this.Hide();
                                }


                            }
                            else
                                MessageBox.Show("Please select proper group and item");
                        }

                    }
                }
                else
                {
                    MessageBox.Show("Please select proper group and item");
                }
            }

            catch (Exception ex)
            {
                Database.RollBackTransaction();
                ErrorMessageBox.Show(ex);
            }
        }

        private int GetNewSrNo()
        {
            if (!Database.OpenConnection(Utility.MaterialConnectionString))
                throw new Exception("Connection error");

            return (int)Utility.SafeConvertToDouble(Database.GetExecuteScalarCommand_return("SELECT ISNULL(MAX(srno),0)+1 FROM stockJournalProd")); //CHANGE stockJournal TO stockJournalProd
        }

        //private void btnSave1_Click(object sender, EventArgs e)
        //{
        //    try
        //    {
        //        if (Validations())
        //        {
        //            if (textWastage.Text == "")
        //                textWastage.Text = "0";
        //            if (txt_sweeping_wastage.Text == "")
        //                txt_sweeping_wastage.Text = "0";

        //            //if (!comboPlant.Items.Contains(comboPlant.Text))
        //            //    MessageBox.Show("Invalid Plant Name");
        //            //else if (!comboPlantSubName.Items.Contains(comboPlantSubName.Text))
        //            //    MessageBox.Show("Invalid Plant Sub  Name");
        //            //else 
        //            if (!comboItemName.Items.Contains(comboItemName.Text))
        //                MessageBox.Show("Invalid Item Name");
        //            else if (Convert.ToDouble(textWastage.Text) < 0)
        //                MessageBox.Show("Wastage can't be negative, please update");
        //            else if (!comboSector.Items.Contains(comboSector.Text))
        //                MessageBox.Show("Invalid Sector Name");
        //            else
        //            {
        //                bool isgrade = false;
        //                DateTime dt = dateSysdate.Value;
        //                DialogResult drs = MessageBox.Show("Do you want to save following information", "Save Information",
        //                     MessageBoxButtons.YesNo, MessageBoxIcon.Information);
        //                if (drs.CompareTo(DialogResult.Yes) == 0)
        //                {
        //                    for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
        //                    {
        //                        if (Database.OpenConnection(Utility.MaterialConnectionString))
        //                        {
        //                            Database.myreader = Database.GetExecuteReaderCommand("select itemname from warehouse with (nolock) where  groupname = '" + dataGridView1[0, i].Value + "' and itemname = '"
        //                                 + dataGridView1[1, i].Value + "' and companyname = '" + comboCompanyName.Text + "' and warehousename = '" + comboWareHosueName.Text + "' ");
        //                            if (!Database.myreader.Read())
        //                            {
        //                                isgrade = true;
        //                            }
        //                            Database.myreader.Close();
        //                        }
        //                    }
        //                    if (!isgrade)
        //                    {

        //                        double GroupSrNo = 0;
        //                        GroupSrNo = Utility.SafeConvertToDouble(Database.ExecuteScalar("select max(isnull(Groupsrno,0))+1 from TapePlantConsumption "));
        //                        bool isauthority = false;
        //                        string status = "";
        //                        Database.myreader = Database.GetExecuteReaderCommand("select plantname from plantmaster where companyname='"
        //                            + comboCompanyName.Text + "' and plantname='" + comboPlant.Text + "' and (approvalauth1 = '"
        //                            + FrmMain.UserName + "'or approvalauth2 = '"
        //                            + FrmMain.UserName + "' or approvalauth3 = '"
        //                            + FrmMain.UserName + "' or approvalauth4 = '" + FrmMain.UserName + "' or  approvalauth5 = '" + FrmMain.UserName + "')");
        //                        if (Database.myreader.Read())
        //                            status = "Approved";
        //                        else
        //                            status = "Pending";
        //                        Database.myreader.Close();
        //                        double stkinhand = 0;
        //                        double sum = 0;
        //                        double checksum = 0;
        //                        bool isallow = true;

        //                        //Ankit 12/08/2014
        //                        double wastage = 0;
        //                        double lumpsWastage = 0;
        //                        Database.BeginTransaction();
        //                        for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
        //                        {
        //                            stkinhand = 0;
        //                            Database.myreader = Database.GetExecuteReaderCommand("select round(stkinhand,2) from warehouse where warehousename = '"
        //                             + comboWareHosueName.Text + "' and groupname = '" + dataGridView1[0, i].Value + "' and itemname = '"
        //                             + dataGridView1[1, i].Value + "' and companyname = '" + comboCompanyName.Text + "' ");
        //                            if (Database.myreader.Read())
        //                            {
        //                                stkinhand = Utility.SafeConvertToDouble(Database.myreader[0].ToString());
        //                            }
        //                            Database.myreader.Close();
        //                            stkinhand = stkinhand - (Utility.SafeConvertToDouble(dataGridView1[2, i].FormattedValue));
        //                            if (stkinhand < 0)
        //                                isallow = false;

        //                            checksum = checksum + Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[2].FormattedValue.ToString());

        //                        }
        //                        if (isallow)
        //                        {
        //                            if (checksum - Convert.ToDouble(textWastage.Text) >= 0)
        //                            {
        //                                for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
        //                                {
        //                                    if (dataGridView1.Rows[i].Cells[0].FormattedValue.ToString() != ""
        //                                        && dataGridView1.Rows[i].Cells[1].FormattedValue.ToString() != "")
        //                                    {
        //                                        stkinhand = 0;

        //                                        Database.myreader = Database.GetExecuteReaderCommand("select itemcode from warehouse with (nolock) where itemname = '"
        //                                         + dataGridView1[1, i].Value + "'");
        //                                        if (Database.myreader.Read())
        //                                        {
        //                                            itemcode = Database.myreader[0].ToString();
        //                                        }
        //                                        Database.myreader.Close();
        //                                        {

        //                                            stkinhand = Convert.ToDouble(dataGridView1[2, i].Value);

        //                                            Database.GetExecuteNonQueryCommand("update warehouse set stkinhand =  stkinhand -  " + stkinhand + "  where warehousename = '"
        //                                         + comboWareHosueName.Text + "' and groupname = '" + dataGridView1[0, i].Value + "' and itemname = '"
        //                                         + dataGridView1[1, i].Value + "' and companyname = '" + comboCompanyName.Text + "'");

        //                                            string Approved = "Pending";
        //                                            string dateOfApproval = "Null";

        //                                            object ApprovalAuth = Database.GetExecuteScalarCommand_return("Select Approvalauth1 from WareHouseMaster where companyname='" + comboCompanyName.Text + "' and warehousename='" + comboWareHosueName.Text + "'");

        //                                            string Approve = ApprovalAuth.ToString();

        //                                            if (Approve == "")
        //                                            {
        //                                                Approved = "Approved";
        //                                                dateOfApproval = "CAST(FLOOR(CAST(getdate() AS float)) AS datetime)";
        //                                            }

        //                                            //Rate base on FIFO
        //                                            //double dblqty = Convert.ToDouble(dataGridView1.Rows[i].Cells[2].Value.ToString());
        //                                            //double dblRate = ProductionCommonFunction.GettingRate(itemcode, dblqty);
        //                                            //double ProdAmount = dblRate * dblqty;
        //                                            ////Add Wastage cost per 1 kg = 15 rs.
        //                                            //double dblwastagerate = 15;

        //                                            double dblqty = Convert.ToDouble(dataGridView1.Rows[i].Cells[2].Value.ToString());
        //                                            double dblRate = 0;// ProductionCommonFunction.GettingRate(itemcode, dblqty);
        //                                            double ProdAmount = dblRate * dblqty;
        //                                            //Add Wastage cost per 1 kg = 15 rs.
        //                                            double dblwastagerate = 15;

        //                                            dataGridView1.Rows[i].Cells[4].Value = dblRate;
        //                                            dataGridView1.Rows[i].Cells[5].Value = ProdAmount;
        //                                            //double dblWastageProd = dblwastagerate * Convert.ToDouble((textWastage.Text + txtTrimWastage.Text + txt_sweeping_wastage.Text));

        //                                            if (Database.GetExecuteNonQueryCommand("insert into TapePlantConsumption (sysDate,PlantName,PlantSubName,PlantItemName,Quality,Grade, " +
        //                                            " Shift,Sector,Qty,Wastage,GroupSrNo,InTime,OutTime,companyname,buyername,Marketinginvno,buyerdate,hodapprove,hodapproveDate,exciseapprove," +
        //                                            " exciseapproveDate,itemcode,ProductionType,fTrimWastage,fSweepingWastage,fTapeLumpsWastage,fTapeOthersWastage,RateConsumption,RateWastage,RateProduction) values(' "
        //                                            + dt.ToString("yyyy-MM-dd") + "', '" + comboPlant.Text + "', '" + comboPlantSubName.Text + "','"
        //                                            + comboItemName.Text + "','"
        //                                            + dataGridView1.Rows[i].Cells[0].Value.ToString() + "','"
        //                                            + dataGridView1.Rows[i].Cells[1].Value.ToString() + "','"
        //                                            + comboShift.Text + "','" + comboSector.Text + "',"
        //                                            + Convert.ToDouble(dataGridView1.Rows[i].Cells[2].Value.ToString()) + ","
        //                                            + textWastage.Text + ","
        //                                            + GroupSrNo + ",'"
        //                                            + dateTimeIn.Text + "','"
        //                                            + dateTimeOut.Text + "','"
        //                                            + comboCompanyName.Text + "','"
        //                                            + comboBuyerName.Text + "','"
        //                                            + comboMarketingInvNo.Text + "','"
        //                                            + dateTimePicker2.Value.ToString("yyyy-MM-dd") + "','" + status + "',Null,'" + Approved + "'," + dateOfApproval + ",'" +
        //                                            itemcode + "','" + ComboProduType.Text + "'," + txtTrimWastage.Text + "," + txt_sweeping_wastage.Text + "," +
        //                                            txtTrimWastage.Text + "," + txt_sweeping_wastage.Text + "," + dblRate + "," + dblwastagerate + "," + ProdAmount + ")"))
        //                                                sum = sum + Convert.ToDouble(dataGridView1.Rows[i].Cells[2].Value.ToString());

        //                                            if (i == 0)
        //                                            {
        //                                                wastage = Utility.SafeConvertToDouble(textWastage.Text);
        //                                                lumpsWastage = Utility.SafeConvertToDouble(txtTrimWastage.Text);
        //                                            }

        //                                            textWastage.Text = "0";
        //                                            txtTrimWastage.Text = "0";
        //                                            txt_sweeping_wastage.Text = "0";

        //                                        }
        //                                    }
        //                                }
        //                            }
        //                            else
        //                                MessageBox.Show("Net Production can't be negative");


        //                        }
        //                        else
        //                        {
        //                            MessageBox.Show("One of Grade stock in warehouse is less than issuing stock so entry not saved");
        //                            return;
        //                        }
        //                        if (sum > 0)
        //                        {
        //                            // if (status == "Approved")
        //                            //Commented By Rikin on 8-8-2014 as this if managed in above codition
        //                            //Database.GetExecuteNonQueryCommand("update TapePlantConsumption set hodapprovedate = CAST(FLOOR(CAST(getdate() AS float)) AS datetime)" +
        //                            //    " where groupsrno = " + GroupSrNo + " and companyname = '"
        //                            //+ comboCompanyName.Text + "'");
        //                            double ProRate = 0;
        //                            for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
        //                            {
        //                                ProRate += Convert.ToDouble(dataGridView1.Rows[i].Cells[5].Value);
        //                            }
        //                            double prodWastageRate = 0;
        //                            prodWastageRate = Utility.SafeConvertToDouble(textWastage.Text) * 15;
        //                            ProRate = ProRate - prodWastageRate;

        //                            textProduction.Text = Convert.ToString(sum);

        //                            if (lblTrimWastage.Text == "Lumps Wastage :")
        //                                textNet.Text = Convert.ToString(Utility.SafeConvertToDouble(textProduction.Text) - wastage - lumpsWastage);
        //                            else
        //                                textNet.Text = Convert.ToString(Utility.SafeConvertToDouble(textProduction.Text) - wastage);

        //                            textNet.Text = Convert.ToString(Convert.ToDouble(textProduction.Text) - wastage);

        //                            string warehousemaster = comboWareHouseName_To.Text;

        //                            if (warehousemaster != "")
        //                            {
        //                                double warestkinhand = Convert.ToDouble(textNet.Text);

        //                                string itmCode = "";

        //                                Database.myreader = Database.GetExecuteReaderCommand("select itemcode from warehouse with (nolock) where itemname = '" + comboItemName.Text + "'");
        //                                if (Database.myreader.Read())
        //                                {
        //                                    itmCode = Database.myreader[0].ToString();
        //                                }
        //                                Database.myreader.Close();

        //                                Database.GetExecuteNonQueryCommand("Insert into Prod_Transaction(CompanyName,Qty,FromGodown,ToGodown,ItemName,Sysdate,ItemCode,ProdAmount,tid) values('" + comboCompanyName.Text + "'," + warestkinhand + ",'" + comboPlant.Text +
        //                                    "','" + comboWareHouseName_To.Text + "','" + comboItemName.Text + "','" + dt.ToString("yyyy-MM-dd") + "','" + itmCode + "'," + ProRate + "," + Convert.ToInt64(GroupSrNo) + " )");

        //                                // to get stock of raw material
        //                                ProductionCommonFunction.WareHouseStoreinwards(itmCode, comboItemName.Text, warestkinhand, dt, comboCompanyName.Text
        //                                                , comboWareHosueName.Text, comboWareHouseName_To.Text);

        //                                Database.GetExecuteNonQueryCommand("update warehouse set stkinhand = stkinhand + " + warestkinhand + " where warehousename = '" + warehousemaster + "' and companyname = '"
        //                                + comboCompanyName.Text + "' and itemname = '" + comboItemName.Text + "'");
        //                            }

        //                        }

        //                        Database.CommitTransaction();
        //                        Database.Closeconnection();
        //                        Utility.UserInformation("Save", "Tape Plant Consumption No " + GroupSrNo.ToString() + " is saved");

        //                        if (sum > 0)
        //                        {
        //                            MessageBox.Show("Plant Consumption is successfully added");

        //                        }

        //                        BindGrid();
        //                        ClearAll(false);
        //                    }
        //                    else
        //                        MessageBox.Show("Please select proper group and item");
        //                }

        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Database.RollBackTransaction();
        //        ErrorMessageBox.Show(ex);
        //    }
        //}

        private void dataGrid1_DoubleClick(object sender, EventArgs e)
        {
            try
            {
                btnSave.Enabled = false;
                btnUpdate.Enabled = true;
                btnDelete.Enabled = true;
                comboPlant.Enabled = false;
                comboWareHosueName.Enabled = false;
                comboPlantSubName.Enabled = false;
                comboWareHouseName_To.Enabled = false;
                dateSysdate.Enabled = false;

                dateSysdate.MaxDate = DateTimePicker.MaximumDateTime;
                dateSysdate.MinDate = DateTimePicker.MinimumDateTime;

                isclear = true;
                dateSysdate.Value = (DateTime)dataGrid1[dataGrid1.CurrentRowIndex, 0];
                comboPlant.Text = dataGrid1[dataGrid1.CurrentRowIndex, 1].ToString();
                comboPlantSubName.Text = dataGrid1[dataGrid1.CurrentRowIndex, 2].ToString();
                comboItemName.Text = dataGrid1[dataGrid1.CurrentRowIndex, 3].ToString();
                comboShift.Text = dataGrid1[dataGrid1.CurrentRowIndex, 6].ToString();
                comboSector.Text = dataGrid1[dataGrid1.CurrentRowIndex, 5].ToString();

                dataGridView1.Rows.Clear();

                GroupSrNo = Convert.ToInt32(dataGrid1[dataGrid1.CurrentRowIndex, 10].ToString());

                dateTimeOut.Text = dataGrid1[dataGrid1.CurrentRowIndex, 11].ToString();
                dateTimeOut.Text = dataGrid1[dataGrid1.CurrentRowIndex, 12].ToString();
                comboCompanyName.Text = dataGrid1[dataGrid1.CurrentRowIndex, 13].ToString();
                comboBuyerName.Text = dataGrid1[dataGrid1.CurrentRowIndex, 14].ToString();

                comboMarketingInvNo.Text = dataGrid1[dataGrid1.CurrentRowIndex, 15].ToString();
                dateTimePicker2.Text = dataGrid1[dataGrid1.CurrentRowIndex, 16].ToString();
                comboSector.Text = dataGrid1[dataGrid1.CurrentRowIndex, 7].ToString();
                ComboProduType.Text = dataGrid1[dataGrid1.CurrentRowIndex, 23].ToString();

                if (Database.OpenConnection(Utility.MaterialConnectionString))
                {
                    Database.myreader = Database.GetExecuteReaderCommand("select distinct(ItemNO) from Despatch..vw_ProductionCompAndMarkInvNo with(nolock) where"
                            + " ProductionCompanyName ='" + comboCompanyName.Text + "' and buyername = '" + comboBuyerName.Text + "'  order by ItemNO   ");

                    DataTable dt = new DataTable();
                    dt.Load(Database.myreader);

                    comboMarketingInvNo.DataSource = dt;
                    comboMarketingInvNo.DisplayMember = "ItemNO";

                    Database.myreader.Close();


                }
                Database.OpenConnection(Utility.MaterialConnectionString);
                object j = Database.GetExecuteScalarCommand_return("select count(*) from tapeplantconsumption where groupsrno = "
                     + GroupSrNo + " and PlantName='" + comboPlant.Text + "' and companyname='" + comboCompanyName.Text + "'");
                int TotalCount = Convert.ToInt32(j);


                if (TotalCount > 0)
                {
                    dataGridView1.Rows.Add(TotalCount);
                    int i = 0;
                    //DataGridViewComboBoxColumn Col2 = (DataGridViewComboBoxColumn)dataGridView1.Columns[1];
                    //if (Col2.Items.Count != 0) Col2.Items.Clear();
                    //if (Database.OpenConnection(Utility.MaterialConnectionString))
                    //{
                    //    for ( i = 0; i < dataGridView1.Rows.Count - 1; i++)
                    //    {
                    //        Database.myreader = Database.GetExecuteReaderCommand("select distinct(ItemName) from warehouse where warehousename = '" + comboWareHosueName.Text + "' "
                    //            + " and groupname = '" + dataGridView1.Rows[i].Cells[0].Value + "' and companyname = '" + comboCompanyName.Text + "' order by itemname");
                    //        while (Database.myreader.Read())
                    //            Col2.Items.Add(Database.myreader[0].ToString());
                    //        Database.myreader.Close();
                    //    }

                    //    //for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                    //    //{
                    //    //    Col2.Items.Add(dataGridView1.Rows[i].Cells[1].Value.ToString());
                    //    //}
                    //}
                    textWastage.Text = "";
                    txtTrimWastage.Text = "";
                    Database.myreader = Database.GetExecuteReaderCommand("select Quality,Grade,qty,transid,itemcode, wastage,fTrimWastage,fSweepingWastage,isnull(fFabricTrim,0) as fFabricTrim,isnull(fFabricwaste,0) as fFabricwaste,isnull(fNewLumpsWs,0) as fNewLumpsWs from tapeplantconsumption where groupsrno = "
                     + GroupSrNo + " and PlantName='" + comboPlant.Text + "' and companyname='" + comboCompanyName.Text + "'");
                    while (Database.myreader.Read())
                    {
                        dataGridView1.Rows[i].Cells[0].Value = Database.myreader["Quality"].ToString();
                        dataGridView1.Rows[i].Cells[1].Value = Database.myreader["Grade"].ToString();
                        dataGridView1.Rows[i].Cells[2].Value = Database.myreader["qty"].ToString();
                        dataGridView1.Rows[i].Cells[6].Value = Database.myreader["transid"].ToString();
                        dataGridView1.Rows[i].Cells[7].Value = Database.myreader["itemcode"].ToString();
                        if (Utility.SafeConvertToDouble(Database.myreader["wastage"].ToString()) > 0)
                            textWastage.Text = Database.myreader["wastage"].ToString();
                        if (Utility.SafeConvertToDouble(Database.myreader["fTrimWastage"].ToString()) > 0)
                            txtTrimWastage.Text = Database.myreader["fTrimWastage"].ToString();
                        if (Utility.SafeConvertToDouble(Database.myreader["fSweepingWastage"].ToString()) > 0)
                            txt_sweeping_wastage.Text = Database.myreader["fSweepingWastage"].ToString();
                        if (Utility.SafeConvertToDouble(Database.myreader["fFabricTrim"].ToString()) > 0)
                            TxtFabricTrm.Text = Database.myreader["fFabricTrim"].ToString();
                        if (Utility.SafeConvertToDouble(Database.myreader["fFabricwaste"].ToString()) > 0)
                            TxtFabricwaste.Text = Database.myreader["fFabricwaste"].ToString();
                        if (Utility.SafeConvertToDouble(Database.myreader["fNewLumpsWs"].ToString()) > 0)
                            TxtLumpsWs.Text = Database.myreader["fNewLumpsWs"].ToString();

                        //dataGridView1.Rows[i].Cells[4].Value = Database.myreader[3].ToString();
                        i++;
                    }
                    Database.myreader.Close();
                    dataGridView1.Rows[0].Cells[3].Selected = true;
                }
                if (!this.IsTranStarted) //23.12.2021
                    Database.Closeconnection();
                isclear = false;

                //dataGridView1.ReadOnly = true;
                textProduction.ReadOnly = true;
                textWastage.ReadOnly = false;
                txtTrimWastage.ReadOnly = false;
                txt_sweeping_wastage.ReadOnly = false;
                textNet.ReadOnly = true;
            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
            }
        }

        private void ClearAll(bool Clear)
        {
            try
            {
                if (Clear == false)
                {
                    for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                    {
                        dataGridView1.Rows[i].Cells[2].Value = "";
                        dataGridView1.Rows[i].Cells[3].Value = 0;
                    }

                }
                else
                {
                    dataGridView1.Rows.Clear();
                    comboPlant.SelectedIndex = 0;
                    comboBuyerOrderNO.SelectedIndex = -1;
                    comboMarketingInvNo.SelectedIndex = -1;
                    #region 02.Sep.2020 Allow clear all fields when Click on Initialize button
                    comboPlant.SelectedIndex = -1;
                    comboWareHosueName.SelectedIndex = -1;
                    comboWareHouseName_To.SelectedIndex = -1;
                    comboWareHosueName.Text = "";
                    comboWareHouseName_To.Text = "";
                    comboWareHosueName.SelectedIndex = -1;
                    comboWareHouseName_To.SelectedIndex = -1;
                    #endregion
                }

                textProduction.Text = "";
                textNet.Text = "";
                textWastage.Text = "0";
                txtTrimWastage.Text = "0";
                txt_sweeping_wastage.Text = "0";
                TXTROLLPROD.Text = "";
                TxtFabricTrm.Text = "0";
                TxtFabricwaste.Text = "0";
                TxtLumpsWs.Text = "0";

                textWastage.Text = "0";
                txtTrimWastage.Text = "0";
                txt_sweeping_wastage.Text = "0";
                TxtFabricTrm.Text = "0";
                TxtFabricwaste.Text = "0";
                TxtLumpsWs.Text = "0";
                textNet.Text = "0";
                textProduction.Text = "0";

                textWastage.ReadOnly = false;
                txtTrimWastage.ReadOnly = false;
                txt_sweeping_wastage.ReadOnly = false;

                comboSector.SelectedIndex = 0;
                comboShift.SelectedIndex = 0;

                btnUpdate.Enabled = false;
                btnSave.Enabled = true;
                btnDelete.Enabled = false;

                comboPlant.Enabled = true;
                comboWareHosueName.Enabled = true;
                comboPlantSubName.Enabled = true;
                comboWareHouseName_To.Enabled = true;
                dateSysdate.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            IsTranStarted = false;
            dateSysdate.CustomFormat = Tools.ToolsCommonFunction.ERPDateSettings();
            dateSysdate.Format = DateTimePickerFormat.Custom;
            dateSysdate.Value = Utility.ConvertStrintToDate(DateTime.Now.ToShortDateString());
            dateSysdate.MaxDate = DateTime.Now;
            dateSysdate.MinDate = ProductionCommonFunction.ERPBackDateSettings();

            isclear = true;
            ClearAll(true);
            //textNet.Text = "0";
            //textProduction.Text = "0";
            //textWastage.Text = "0";
            isclear = false;
        }
        private void btnUpdate_Click(object sender, EventArgs e)
        {
            DataTable dataDt = new DataTable();
            DataTable tempDt = new DataTable();
            DataTable oldProdDt = new DataTable();
            DataTable newProdDt = new DataTable();
            tempDt.Columns.Add("Itemcode");
            tempDt.Columns.Add("OldQty");
            tempDt.Columns.Add("NewQty");
            tempDt.Columns.Add("transid");
            decimal OldQty = 0;
            decimal OldWastageQty = 0;
            decimal NewQty = 0;
            decimal diffQty = 0;
            if (!Validations())
            {
                return;
            }

            try
            {
                Database.OpenConnection(Utility.MaterialConnectionString);
                Database.BeginTransaction();
                if (FrmMain.IsAdmin == false && FrmMain.IsProductionadmin == false)
                {
                    if ((Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd")) - Convert.ToDateTime(dateSysdate.Value.ToString("yyyy-MM-dd"))).TotalDays > 2)
                    {
                        //MessageBox.Show("Please check date");
                        //return;
                    }
                }
                dataDt = Database.GetDataTable("select *,isnull(Wastage,0)+isnull(fTrimWastage,0)+isnull(fSweepingWastage,0)+isnull(fTapeLumpsWastage,0)+isnull(fTapeOthersWastage,0)+isnull(fFabricTrim,0)+isnull(fFabricwaste,0)+isnull(fNewLumpsWs,0) as Totalwasage from TapePlantConsumption where GroupSrNo = " + GroupSrNo + " and plantname='" + comboPlant.Text + "'");
                // for old product name
                oldProdDt = Database.GetDataTable("select ItemCode,StkInHand,ItemName from WareHouse where ItemName = '" + dataDt.Rows[0]["PlantItemName"].ToString() + "' and WareHouseName = '" + comboWareHouseName_To.Text + "' and CompanyName = '" + comboCompanyName.Text + "'");
                // for new product name
                newProdDt = Database.GetDataTable("select ItemCode,StkInHand,ItemName from WareHouse where ItemName = '" + comboItemName.Text + "' and WareHouseName = '" + comboWareHouseName_To.Text + "' and CompanyName = '" + comboCompanyName.Text + "'");
                // to get old qty sum
                object QtySum = dataDt.Compute("Sum(Qty)", "");
                OldWastageQty = Convert.ToDecimal(dataDt.Compute("Sum(Totalwasage)", ""));
                OldQty = Convert.ToDecimal(QtySum);

                // to get new qty sum
                for (int i = 0; i < dataGridView1.Rows.Count; i++)
                {
                    NewQty += Convert.ToDecimal(dataGridView1.Rows[i].Cells["DtQty"].Value);
                }

                //DataTable dataGridTable = (DataTable)dataGridView1.DataSource;
                //object QtySum1 = dataGridTable.Compute("Sum(Qty)", "");
                //NewQty = Convert.ToDecimal(QtySum1);                          
                DateTime dt = dateSysdate.Value;

                DataRow[] foundProductName = dataDt.Select("PlantItemName = '" + comboItemName.Text + "'");
                if (foundProductName.Length == 0)
                {
                    if (MessageBox.Show("Are you sure want to change prpduct name from '" + dataDt.Rows[0]["PlantItemName"].ToString() + "' to '" + comboItemName.Text + "' ?"
                  , "Plant Consumption", MessageBoxButtons.YesNo, MessageBoxIcon.Information) != System.Windows.Forms.DialogResult.Yes)

                        return;

                    // to update stock of new product name
                    Database.ExecuteNonQuery("update warehouse set StkInHand = StkInHand + " + Convert.ToDouble(textNet.Text) + " where itemcode = '" + newProdDt.Rows[0]["ItemCode"].ToString() + "' and WareHouseName = '" + comboWareHouseName_To.Text + "' and CompanyName = '" + comboCompanyName.Text + "' ");
                    ProductionCommonFunction.WareHouseStoreinwards(newProdDt.Rows[0]["ItemCode"].ToString(), comboItemName.Text, Convert.ToDouble(textNet.Text), dt, comboCompanyName.Text
                                                       , comboWareHosueName.Text, comboWareHouseName_To.Text);

                    // to update stock of old product name
                    object StkInHand = Database.GetExecuteScalarCommand_return("select StkInHand from warehouse where itemcode = '" + oldProdDt.Rows[0]["ItemCode"].ToString() + "' and WareHouseName = '" + comboWareHouseName_To.Text + "' and CompanyName = '" + comboCompanyName.Text + "'");
                    if ((Convert.ToDecimal(StkInHand) - (OldQty - OldWastageQty)) >= 0)
                    {
                        Database.ExecuteNonQuery("update warehouse set StkInHand = StkInHand - " + (OldQty - OldWastageQty) + " where itemcode = '" + oldProdDt.Rows[0]["ItemCode"].ToString() + "' and WareHouseName = '" + comboWareHouseName_To.Text + "' and CompanyName = '" + comboCompanyName.Text + "'");
                        ProductionCommonFunction.WareHouseStoreoutwards(oldProdDt.Rows[0]["ItemCode"].ToString(), comboItemName.Text, Convert.ToDouble((OldQty - OldWastageQty)), dt, comboCompanyName.Text
                                                , comboWareHosueName.Text, comboWareHouseName_To.Text);
                    }
                    else
                    {
                        Database.RollBackTransaction();
                        MessageBox.Show("There is no enough stock of item '" + oldProdDt.Rows[0]["ItemCode"].ToString() + "' in '" + comboWareHouseName_To.Text + "' warehouse");
                        return;
                    }
                    Database.ExecuteNonQuery("update TapePlantConsumption set buyername = '" + comboBuyerName.Text + "',Shift = '" + comboShift.Text + "', InTime = '" + dateTimeIn.Text + "', OutTime = '" + dateTimeOut.Text + "', ProductionType = '" + ComboProduType.Text + "', Sector = '" + comboSector.Text + "', PlantItemName = '" + comboItemName.Text
                        + "' where GroupSrNo = " + GroupSrNo + " and CompanyName = '" + comboCompanyName.Text + "' and PlantName='" + comboPlant.Text + "'");

                    Database.ExecuteNonQuery("UPDATE stockJournalProd SET ITEM_NAME='" + comboItemName.Text + "',itemcode='" + newProdDt.Rows[0]["ItemCode"].ToString()
                        + "' where srno=" + GroupSrNo + " and  Company_Name = '" + comboCompanyName.Text + "' and   itemcode = '" + oldProdDt.Rows[0]["ItemCode"].ToString() + "'");
                }
                else
                {
                    Database.ExecuteNonQuery("update TapePlantConsumption set buyername = '" + comboBuyerName.Text + "', Shift = '" + comboShift.Text + "', InTime = '" + dateTimeIn.Text + "', OutTime = '" + dateTimeOut.Text + "', ProductionType = '" + ComboProduType.Text + "', Sector = '" + comboSector.Text + "' where GroupSrNo = " + GroupSrNo + " and CompanyName = '" + comboCompanyName.Text + "'");
                    //to update stock of finish product
                    if (oldProdDt.Rows.Count > 0)
                    {
                        Database.ExecuteNonQuery("update warehouse set StkInHand = StkInHand + " + NewQty + " where itemcode = '" + oldProdDt.Rows[0]["ItemCode"].ToString() + "' and WareHouseName = '" + comboWareHouseName_To.Text + "' and CompanyName = '" + comboCompanyName.Text + "'");
                        ProductionCommonFunction.WareHouseStoreinwards(oldProdDt.Rows[0]["ItemCode"].ToString(), comboItemName.Text, Convert.ToDouble(NewQty), dt, comboCompanyName.Text
                                                       , comboWareHosueName.Text, comboWareHouseName_To.Text);

                        object StkInHand = Database.GetExecuteScalarCommand_return("select StkInHand from warehouse where itemcode = '" + oldProdDt.Rows[0]["ItemCode"].ToString() + "' and WareHouseName = '" + comboWareHouseName_To.Text + "' and CompanyName = '" + comboCompanyName.Text + "'");
                        if ((Convert.ToDecimal(StkInHand) - OldQty) >= 0)
                        {
                            Database.ExecuteNonQuery("update warehouse set StkInHand = StkInHand - " + OldQty + " where itemcode = '" + oldProdDt.Rows[0]["ItemCode"].ToString() + "' and WareHouseName = '" + comboWareHouseName_To.Text + "' and CompanyName = '" + comboCompanyName.Text + "'");
                            ProductionCommonFunction.WareHouseStoreoutwards(oldProdDt.Rows[0]["ItemCode"].ToString(), comboItemName.Text, Convert.ToDouble(OldQty), dt, comboCompanyName.Text
                                                    , comboWareHosueName.Text, comboWareHouseName_To.Text);
                        }
                        else
                        {
                            Database.RollBackTransaction();
                            MessageBox.Show("There is no enough stock of item '" + oldProdDt.Rows[0]["ItemCode"].ToString() + "' in '" + comboWareHouseName_To.Text + "' warehouse");
                            return;
                        }
                    }
                }
                for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                {
                    DataRow[] foundRows = dataDt.Select("Quality = '" + dataGridView1.Rows[i].Cells[0].Value.ToString() + "' and Grade = '" + dataGridView1.Rows[i].Cells[1].Value.ToString() + "' and Qty = '" + dataGridView1.Rows[i].Cells[2].Value.ToString() + "' and transid = '" + dataGridView1.Rows[i].Cells[6].Value.ToString() + "'");
                    if (foundRows.Length == 0)
                    {
                        DataRow[] foundOldQty = dataDt.Select("transid = '" + dataGridView1.Rows[i].Cells[6].Value + "'");
                        DataRow dr = tempDt.NewRow();
                        dr[0] = dataGridView1.Rows[i].Cells[7].Value.ToString();
                        dr[1] = foundOldQty[0].ItemArray[8].ToString();
                        dr[2] = dataGridView1.Rows[i].Cells[2].Value.ToString();
                        dr[3] = dataGridView1.Rows[i].Cells[6].Value.ToString();
                        tempDt.Rows.InsertAt(dr, i);
                    }
                    else
                    {
                        DataRow dr = tempDt.NewRow();
                        dr[0] = dataGridView1.Rows[i].Cells[7].Value.ToString();
                        dr[1] = dataGridView1.Rows[i].Cells[2].Value.ToString();
                        dr[2] = dataGridView1.Rows[i].Cells[2].Value.ToString();
                        dr[3] = dataGridView1.Rows[i].Cells[6].Value.ToString();
                        tempDt.Rows.InsertAt(dr, i);
                    }
                }
                int srno = GetNewSrNo();
                int RecordLogId = AccountCommonFunction.SaveRecordLog(null, FrmMain.UserName, RecordLogFlag.Regular, string.Format("Create: Stock Journal {0} Saved in {1} for {2}", "", this.CompanyName, frmDefaultVale.Year));

                for (int i = 0; i < tempDt.Rows.Count; i++)
                {
                    if (tempDt.Rows[i]["OldQty"].ToString() != tempDt.Rows[i]["NewQty"].ToString())
                    {
                        if (Convert.ToDecimal(tempDt.Rows[i]["OldQty"].ToString()) > Convert.ToDecimal(tempDt.Rows[i]["NewQty"].ToString()))
                        {
                            diffQty = Convert.ToDecimal(tempDt.Rows[i]["OldQty"].ToString()) - Convert.ToDecimal(tempDt.Rows[i]["NewQty"].ToString());
                            Database.ExecuteNonQuery("update warehouse set StkInHand = StkInHand + " + diffQty + " where itemcode = '" + tempDt.Rows[i]["Itemcode"] + "' and warehousename = '" + comboWareHosueName.Text + "' and CompanyName = '" + comboCompanyName.Text + "' ");

                            ProductionCommonFunction.WareHouseStoreinwards(tempDt.Rows[i]["Itemcode"].ToString(), comboItemName.Text, Convert.ToDouble(diffQty), dt, comboCompanyName.Text
                                                           , comboWareHosueName.Text, comboWareHouseName_To.Text);

                            Database.ExecuteNonQuery("update TapePlantConsumption set Qty = " + Convert.ToDecimal(tempDt.Rows[i]["NewQty"].ToString()) + " where transid= " + tempDt.Rows[i]["transid"].ToString() + " and CompanyName = '" + comboCompanyName.Text + "' ");

                            ProductionCommonFunction.InsertStockJournal(srno, dt, comboCompanyName.Text,
                                (ComboProduType.Text == "Outside Job" ? "Outside consumption" : "consumption"), dataGridView1.Rows[i].Cells[1].Value.ToString(),
                                -1 * Convert.ToDouble(diffQty), 0, 0, "KGS", frmDefaultVale.Year, RecordLogId, Convert.ToInt32(GroupSrNo), tempDt.Rows[i]["Itemcode"].ToString(), "..", comboWareHouseName_To.Text);

                        }
                        else
                        {
                            diffQty = Convert.ToDecimal(tempDt.Rows[i]["NewQty"].ToString()) - Convert.ToDecimal(tempDt.Rows[i]["OldQty"].ToString());

                            object StkInHand = Database.GetExecuteScalarCommand_return("select StkInHand from warehouse where itemcode = '" + tempDt.Rows[i]["Itemcode"] + "' and warehousename = '" + comboWareHosueName.Text + "' and CompanyName = '" + comboCompanyName.Text + "'");
                            if ((Convert.ToDecimal(StkInHand) - diffQty) >= 0)
                            {
                                Database.ExecuteNonQuery("update warehouse set StkInHand = StkInHand - " + diffQty + " where itemcode = '" + tempDt.Rows[i]["Itemcode"] + "' and warehousename = '" + comboWareHosueName.Text + "' and CompanyName = '" + comboCompanyName.Text + "'");
                                ProductionCommonFunction.WareHouseStoreoutwards(tempDt.Rows[i]["Itemcode"].ToString(), comboItemName.Text, Convert.ToDouble(diffQty), dt, comboCompanyName.Text
                                                           , comboWareHosueName.Text, comboWareHouseName_To.Text);

                                ProductionCommonFunction.InsertStockJournal(srno, dt, comboCompanyName.Text,
                                (ComboProduType.Text == "Outside Job" ? "Outside consumption" : "consumption"), dataGridView1.Rows[i].Cells[1].Value.ToString(),
                                Convert.ToDouble(diffQty), 0, 0, "KGS", frmDefaultVale.Year, RecordLogId, Convert.ToInt32(GroupSrNo), tempDt.Rows[i]["Itemcode"].ToString(), "..", comboWareHouseName_To.Text);
                            }
                            else
                            {
                                Database.RollBackTransaction();
                                MessageBox.Show("There is no enough stock of item '" + tempDt.Rows[i]["Itemcode"] + "' in '" + comboWareHosueName.Text + "' warehouse");
                                return;
                            }
                            Database.ExecuteNonQuery("update TapePlantConsumption set Qty = " + Convert.ToDecimal(tempDt.Rows[i]["NewQty"].ToString()) + " where transid= " + tempDt.Rows[i]["transid"].ToString() + " and CompanyName = '" + comboCompanyName.Text + "' ");
                        }
                    }
                }

                // for wastage column update
                string updateString = string.Empty;
                object transid = Database.GetExecuteScalarCommand_return("select min(transid) from TapePlantConsumption where GroupSrNo = " + GroupSrNo + " and PlantName='" + comboPlant.Text + "'");

                updateString += "update TapePlantConsumption set Wastage = '" + textWastage.Text + "'";
                updateString += ",fFabricTrim=" + Utility.SafeConvertToDouble(TxtFabricTrm.Text) + ",fFabricwaste="
                    + Utility.SafeConvertToDouble(TxtFabricwaste.Text) + ",fNewLumpsWs=" + Utility.SafeConvertToDouble(TxtLumpsWs.Text);

                if (txt_sweeping_wastage.Text.Length > 0)
                    updateString += ", fSweepingWastage = '" + txt_sweeping_wastage.Text + "', fTapeOthersWastage= '" + txt_sweeping_wastage.Text + "'";
                if (txtTrimWastage.Text.Length > 0)
                    updateString += ", fTrimWastage= '" + (comboPlant.Text.ToLower() == "lamination" ? txtTrimWastage.Text : "0") + "', " +
                        " fTapeLumpsWastage='" + (comboPlant.Text.ToLower() == "tape plant" ? txtTrimWastage.Text : "0") + "'";

                updateString += " where transid = '" + transid.ToString() + "' and CompanyName = '" + comboCompanyName.Text + "'";
                Database.ExecuteNonQuery(updateString);

                //Differnce in Wastage 22.07.2020

                string itemcodewastg = (string)Database.GetExecuteNonQueryCommand_retrun("SELECT wastageItemCode FROM PlantMaster WHERE companyname='" + comboCompanyName.Text + "' and fromwarehouse='" + comboWareHouseName_To.Text + "'");
                if ((object)itemcodewastg == null)
                    itemcodewastg = (string)Database.GetExecuteNonQueryCommand_retrun("select top 1 ItemCode from item where MISCATEGORY='SF' and MISGRUPITEM in ('Fabric waste','TAPE WASTE','Wastage/Scrap') and todate is null and CompanyName='" + comboCompanyName.Text + "' order by fromdate desc ");
                itemcodewastg = Convert.ToString(itemcodewastg);
                string itemWasteName = (string)Database.GetExecuteNonQueryCommand_retrun("select top 1 itemname from item where MISCATEGORY='SF' and MISGRUPITEM in ('Fabric waste','TAPE WASTE','Wastage/Scrap') and todate is null and CompanyName='"
                    + comboCompanyName.Text + "' and itemcode='" + itemcodewastg + "' order by fromdate desc ");


                double totalwastage = Utility.SafeConvertToDouble(textWastage.Text) + Utility.SafeConvertToDouble(txtTrimWastage.Text)
                                                           + Utility.SafeConvertToDouble(txt_sweeping_wastage.Text) + Utility.SafeConvertToDouble(TxtFabricTrm.Text)
                                                           + Utility.SafeConvertToDouble(TxtFabricwaste.Text) + Utility.SafeConvertToDouble(TxtLumpsWs.Text);
                double difftotalwastage = 0;
                double oldtotalwastage = 0;
                if (dataDt.Rows.Count > 0)
                {
                    oldtotalwastage = Utility.SafeConvertToDouble(dataDt.Rows[0]["wastage"]);
                    oldtotalwastage += Utility.SafeConvertToDouble(dataDt.Rows[0]["fTrimWastage"]);
                    oldtotalwastage += Utility.SafeConvertToDouble(dataDt.Rows[0]["fSweepingWastage"]);
                    oldtotalwastage += Utility.SafeConvertToDouble(dataDt.Rows[0]["fFabricTrim"]);
                    oldtotalwastage += Utility.SafeConvertToDouble(dataDt.Rows[0]["fFabricwaste"]);
                    oldtotalwastage += Utility.SafeConvertToDouble(dataDt.Rows[0]["fNewLumpsWs"]);
                }
                difftotalwastage = totalwastage - oldtotalwastage; //23.11.2021 change for wastage
                if (difftotalwastage != 0)
                    ProductionCommonFunction.InsertStockJournal(srno, dt, comboCompanyName.Text, (ComboProduType.Text == "Outside Job" ? "Outside Production" : "Production"), itemWasteName,
                        Convert.ToDouble(difftotalwastage), 0, 0, "KGS", frmDefaultVale.Year, RecordLogId, Convert.ToInt32(GroupSrNo), itemcodewastg, "..", comboWareHouseName_To.Text);
                //Differnce in Wastage 22.07.2020
                // for Prod_Transaction table update

                if (textNet.Text.Length == 0)
                {
                    Database.RollBackTransaction();
                    MessageBox.Show("Net Production should not be blank..!!!");
                    return;
                }
                else
                {
                    double diffNetPro = Convert.ToDouble(NewQty) - (Convert.ToDouble(OldQty) - oldtotalwastage) - totalwastage;
                    string itemcode1 = newProdDt.Rows[0]["Itemcode"].ToString();
                    Database.ExecuteNonQuery("update Prod_Transaction set Qty = " + Convert.ToDecimal(textNet.Text)
                        + " where tid= " + GroupSrNo + " and CompanyName = '" + comboCompanyName.Text + "' and itemcode='" + itemcode1 + "'");

                    if (comboPlant.Text != "Lamination") //25.07.2020 not allow Stock Productoin entry for Lamination as we already get data from MIS Lamination Entry.
                        ProductionCommonFunction.InsertStockJournal(srno, dt, comboCompanyName.Text, (ComboProduType.Text == "Outside Job" ? "Outside Production" : "Production"), comboItemName.Text,
                        Convert.ToDouble(diffNetPro), 0, 0, "KGS", frmDefaultVale.Year, RecordLogId, Convert.ToInt32(GroupSrNo), itemcode1, "..", comboWareHouseName_To.Text);

                }

                Database.CommitTransaction();
                BindGrid();
                dataGridView1.Rows.Clear();
                Utility.UserInformation("Update", "Tape Plant Consumption No " + GroupSrNo.ToString() + " is Updated");

                MessageBox.Show("Details updated Sccessfully..!!");

            }
            catch (Exception exObj)
            {
                Database.RollBackTransaction();
                ErrorMessageBox.Show(exObj);
            }
        }
        //private void btnUpdate1_Click(object sender, EventArgs e)
        //{
        //    DataTable dataDt = new DataTable();
        //    DataTable tempDt = new DataTable();
        //    DataTable oldProdDt = new DataTable();
        //    DataTable newProdDt = new DataTable();
        //    tempDt.Columns.Add("Itemcode");
        //    tempDt.Columns.Add("OldQty");
        //    tempDt.Columns.Add("NewQty");
        //    tempDt.Columns.Add("transid");
        //    decimal OldQty = 0;
        //    decimal NewQty = 0;
        //    decimal diffQty = 0;

        //    try
        //    {
        //        Database.BeginTransaction();
        //        if (FrmMain.IsAdmin == false && FrmMain.IsProductionadmin == false)
        //        {
        //            if ((Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd")) - Convert.ToDateTime(dateSysdate.Value.ToString("yyyy-MM-dd"))).TotalDays > 2)
        //            {
        //                //MessageBox.Show("Please check date");
        //                //return;
        //            }
        //        }
        //        dataDt = Database.GetDataTable("select * from TapePlantConsumption where GroupSrNo = " + GroupSrNo + "");


        //        // for old product name
        //        oldProdDt = Database.GetDataTable("select ItemCode,StkInHand from WareHouse where ItemName = '" + dataDt.Rows[0]["PlantItemName"].ToString() + "' and WareHouseName = '" + comboWareHouseName_To.Text + "' and CompanyName = '" + comboCompanyName.Text + "'");

        //        // for new product name
        //        newProdDt = Database.GetDataTable("select ItemCode,StkInHand from WareHouse where ItemName = '" + comboItemName.Text + "' and WareHouseName = '" + comboWareHouseName_To.Text + "' and CompanyName = '" + comboCompanyName.Text + "'");

        //        // to get old qty sum
        //        object QtySum = dataDt.Compute("Sum(Qty)", "");
        //        OldQty = Convert.ToDecimal(QtySum);

        //        // to get new qty sum
        //        for (int i = 0; i < dataGridView1.Rows.Count; i++)
        //        {
        //            NewQty += Convert.ToDecimal(dataGridView1.Rows[i].Cells["DtQty"].Value);
        //        }

        //        //DataTable dataGridTable = (DataTable)dataGridView1.DataSource;
        //        //object QtySum1 = dataGridTable.Compute("Sum(Qty)", "");
        //        //NewQty = Convert.ToDecimal(QtySum1);                          

        //        DataRow[] foundProductName = dataDt.Select("PlantItemName = '" + comboItemName.Text + "'");
        //        if (foundProductName.Length == 0)
        //        {
        //            if (MessageBox.Show("Are you sure want to change prpduct name from '" + dataDt.Rows[0]["PlantItemName"].ToString() + "' to '" + comboItemName.Text + "' ?"
        //          , "CutPiece Entry", MessageBoxButtons.YesNo, MessageBoxIcon.Information) != System.Windows.Forms.DialogResult.Yes)

        //                return;

        //            // to update stock of new product name
        //            Database.ExecuteNonQuery("update warehouse set StkInHand = StkInHand + " + NewQty + " where itemcode = '" + newProdDt.Rows[0]["ItemCode"].ToString() + "' and WareHouseName = '" + comboWareHouseName_To.Text + "' and CompanyName = '" + comboCompanyName.Text + "' ");
        //            ProductionCommonFunction.WareHouseStoreinwards(newProdDt.Rows[0]["ItemCode"].ToString(), comboItemName.Text, Convert.ToDouble(NewQty), dt, comboCompanyName.Text
        //                                               , comboWareHosueName.Text, comboWareHouseName_To.Text);
        //            // to update stock of old product name

        //            object StkInHand = Database.GetExecuteScalarCommand_return("select StkInHand from warehouse where itemcode = '" + oldProdDt.Rows[0]["ItemCode"].ToString() + "' and WareHouseName = '" + comboWareHouseName_To.Text + "' and CompanyName = '" + comboCompanyName.Text + "'");
        //            if ((Convert.ToDecimal(StkInHand) - OldQty) >= 0)
        //            {
        //                Database.ExecuteNonQuery("update warehouse set StkInHand = StkInHand - " + OldQty + " where itemcode = '" + oldProdDt.Rows[0]["ItemCode"].ToString() + "' and WareHouseName = '" + comboWareHouseName_To.Text + "' and CompanyName = '" + comboCompanyName.Text + "'");
        //                ProductionCommonFunction.WareHouseStoreoutwards(oldProdDt.Rows[0]["ItemCode"].ToString(), comboItemName.Text, Convert.ToDouble(OldQty), dt, comboCompanyName.Text
        //                                                                       , comboWareHosueName.Text, comboWareHouseName_To.Text);
        //            }
        //            else
        //            {
        //                Database.RollBackTransaction();
        //                MessageBox.Show("There is no enough stock of item '" + oldProdDt.Rows[0]["ItemCode"].ToString() + "' in '" + comboWareHouseName_To.Text + "' warehouse");
        //                return;
        //            }

        //            Database.ExecuteNonQuery("update TapePlantConsumption set buyername = '" + comboBuyerName.Text + "',Shift = '" + comboShift.Text + "', InTime = '" + dateTimeIn.Text + "', OutTime = '" + dateTimeOut.Text + "', ProductionType = '" + ComboProduType.Text + "', Sector = '" + comboSector.Text + "', PlantItemName = '" + comboItemName.Text + "' where GroupSrNo = " + GroupSrNo + " and CompanyName = '" + comboCompanyName.Text + "'");
        //        }
        //        else
        //        {
        //            Database.ExecuteNonQuery("update TapePlantConsumption set buyername = '" + comboBuyerName.Text + "', Shift = '" + comboShift.Text + "', InTime = '" + dateTimeIn.Text + "', OutTime = '" + dateTimeOut.Text + "', ProductionType = '" + ComboProduType.Text + "', Sector = '" + comboSector.Text + "' where GroupSrNo = " + GroupSrNo + " and CompanyName = '" + comboCompanyName.Text + "'");
        //            //to update stock of finish product
        //            if (oldProdDt.Rows.Count > 0)
        //            {
        //                Database.ExecuteNonQuery("update warehouse set StkInHand = StkInHand + " + NewQty + " where itemcode = '" + oldProdDt.Rows[0]["ItemCode"].ToString() + "' and WareHouseName = '" + comboWareHouseName_To.Text + "' and CompanyName = '" + comboCompanyName.Text + "'");
        //                ProductionCommonFunction.WareHouseStoreinwards(oldProdDt.Rows[0]["ItemCode"].ToString(), comboItemName.Text, Convert.ToDouble(NewQty), dt, comboCompanyName.Text
        //                                              , comboWareHosueName.Text, comboWareHouseName_To.Text);
        //                object StkInHand = Database.GetExecuteScalarCommand_return("select StkInHand from warehouse where itemcode = '" + oldProdDt.Rows[0]["ItemCode"].ToString() + "' and WareHouseName = '" + comboWareHouseName_To.Text + "' and CompanyName = '" + comboCompanyName.Text + "'");
        //                if ((Convert.ToDecimal(StkInHand) - OldQty) >= 0)
        //                {
        //                    Database.ExecuteNonQuery("update warehouse set StkInHand = StkInHand - " + OldQty + " where itemcode = '" + oldProdDt.Rows[0]["ItemCode"].ToString() + "' and WareHouseName = '" + comboWareHouseName_To.Text + "' and CompanyName = '" + comboCompanyName.Text + "'");
        //                    ProductionCommonFunction.WareHouseStoreoutwards(oldProdDt.Rows[0]["ItemCode"].ToString(), comboItemName.Text, Convert.ToDouble(OldQty), dt, comboCompanyName.Text
        //                                                   , comboWareHosueName.Text, comboWareHouseName_To.Text);
        //                }
        //                else
        //                {
        //                    Database.RollBackTransaction();
        //                    MessageBox.Show("There is no enough stock of item '" + oldProdDt.Rows[0]["ItemCode"].ToString() + "' in '" + comboWareHouseName_To.Text + "' warehouse");
        //                    return;
        //                }
        //            }
        //        }
        //        for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
        //        {
        //            DataRow[] foundRows = dataDt.Select("Quality = '" + dataGridView1.Rows[i].Cells[0].Value.ToString() + "' and Grade = '" + dataGridView1.Rows[i].Cells[1].Value.ToString() + "' and Qty = '" + dataGridView1.Rows[i].Cells[2].Value.ToString() + "' and transid = '" + dataGridView1.Rows[i].Cells[6].Value.ToString() + "'");
        //            if (foundRows.Length == 0)
        //            {
        //                DataRow[] foundOldQty = dataDt.Select("transid = '" + dataGridView1.Rows[i].Cells[6].Value + "'");
        //                DataRow dr = tempDt.NewRow();
        //                dr[0] = dataGridView1.Rows[i].Cells[7].Value.ToString();
        //                dr[1] = foundOldQty[0].ItemArray[8].ToString();
        //                dr[2] = dataGridView1.Rows[i].Cells[2].Value.ToString();
        //                dr[3] = dataGridView1.Rows[i].Cells[6].Value.ToString();
        //                tempDt.Rows.InsertAt(dr, i);
        //            }
        //            else
        //            {
        //                DataRow dr = tempDt.NewRow();
        //                dr[0] = dataGridView1.Rows[i].Cells[7].Value.ToString();
        //                dr[1] = dataGridView1.Rows[i].Cells[2].Value.ToString();
        //                dr[2] = dataGridView1.Rows[i].Cells[2].Value.ToString();
        //                dr[3] = dataGridView1.Rows[i].Cells[6].Value.ToString();
        //                tempDt.Rows.InsertAt(dr, i);
        //            }
        //        }

        //        for (int i = 0; i < tempDt.Rows.Count; i++)
        //        {
        //            if (tempDt.Rows[i]["OldQty"].ToString() != tempDt.Rows[i]["NewQty"].ToString())
        //            {
        //                if (Convert.ToDecimal(tempDt.Rows[i]["OldQty"].ToString()) > Convert.ToDecimal(tempDt.Rows[i]["NewQty"].ToString()))
        //                {
        //                    diffQty = Convert.ToDecimal(tempDt.Rows[i]["OldQty"].ToString()) - Convert.ToDecimal(tempDt.Rows[i]["NewQty"].ToString());
        //                    Database.ExecuteNonQuery("update warehouse set StkInHand = StkInHand + " + diffQty + " where itemcode = '" + tempDt.Rows[i]["Itemcode"] + "' and warehousename = '" + comboWareHosueName.Text + "' and CompanyName = '" + comboCompanyName.Text + "' ");
        //                    ProductionCommonFunction.WareHouseStoreinwards(tempDt.Rows[i]["Itemcode"].ToString(), comboItemName.Text, Convert.ToDouble(diffQty), dt, comboCompanyName.Text
        //                                                   , comboWareHosueName.Text, comboWareHouseName_To.Text);
        //                    Database.ExecuteNonQuery("update TapePlantConsumption set Qty = " + Convert.ToDecimal(tempDt.Rows[i]["NewQty"].ToString()) + " where transid= " + tempDt.Rows[i]["transid"].ToString() + " and CompanyName = '" + comboCompanyName.Text + "' ");
        //                }
        //                else
        //                {
        //                    diffQty = Convert.ToDecimal(tempDt.Rows[i]["NewQty"].ToString()) - Convert.ToDecimal(tempDt.Rows[i]["OldQty"].ToString());

        //                    object StkInHand = Database.GetExecuteScalarCommand_return("select StkInHand from warehouse where itemcode = '" + tempDt.Rows[i]["Itemcode"] + "' and warehousename = '" + comboWareHosueName.Text + "' and CompanyName = '" + comboCompanyName.Text + "'");
        //                    if ((Convert.ToDecimal(StkInHand) - diffQty) >= 0)
        //                    {
        //                        Database.ExecuteNonQuery("update warehouse set StkInHand = StkInHand - " + diffQty + " where itemcode = '" + tempDt.Rows[i]["Itemcode"] + "' and warehousename = '" + comboWareHosueName.Text + "' and CompanyName = '" + comboCompanyName.Text + "'");
        //                        ProductionCommonFunction.WareHouseStoreoutwards(tempDt.Rows[i]["Itemcode"].ToString(), comboItemName.Text, Convert.ToDouble(diffQty), dt, comboCompanyName.Text
        //                                                   , comboWareHosueName.Text, comboWareHouseName_To.Text);
        //                    }
        //                    else
        //                    {
        //                        Database.RollBackTransaction();
        //                        MessageBox.Show("There is no enough stock of item '" + tempDt.Rows[i]["Itemcode"] + "' in '" + comboWareHosueName.Text + "' warehouse");
        //                        return;
        //                    }

        //                    Database.ExecuteNonQuery("update TapePlantConsumption set Qty = " + Convert.ToDecimal(tempDt.Rows[i]["NewQty"].ToString()) + " where transid= " + tempDt.Rows[i]["transid"].ToString() + " and CompanyName = '" + comboCompanyName.Text + "' ");
        //                }
        //            }
        //        }

        //        // for wastage column update
        //        string updateString = string.Empty;
        //        object transid = Database.GetExecuteScalarCommand_return("select min(transid) from TapePlantConsumption where GroupSrNo = " + GroupSrNo + "");

        //        updateString += "update TapePlantConsumption set Wastage = '" + textWastage.Text + "'";
        //        if (txt_sweeping_wastage.Text.Length > 0)
        //            updateString += ", fSweepingWastage = '" + txt_sweeping_wastage.Text + "', fTapeOthersWastage= '" + txt_sweeping_wastage.Text + "'";
        //        if (txtTrimWastage.Text.Length > 0)
        //            updateString += ", fTrimWastage= '" + txtTrimWastage.Text + "',fTapeLumpsWastage='" + txtTrimWastage.Text + "'";
        //        updateString += " where transid = '" + transid.ToString() + "' and CompanyName = '" + comboCompanyName.Text + "'";
        //        Database.ExecuteNonQuery(updateString);

        //        // for Prod_Transaction table update

        //        if (textNet.Text.Length == 0)
        //        {
        //            Database.RollBackTransaction();
        //            MessageBox.Show("Net Production should not be blank..!!!");
        //            return;
        //        }
        //        else
        //        {
        //            Database.ExecuteNonQuery("update Prod_Transaction set Qty = " + Convert.ToDecimal(textNet.Text) + " where tid= " + GroupSrNo + " and CompanyName = '" + comboCompanyName.Text + "'");
        //        }

        //        Database.CommitTransaction();
        //        BindGrid();
        //        dataGridView1.Rows.Clear();
        //        MessageBox.Show("Details updated Sccessfully..!!");

        //    }
        //    catch (Exception exObj)
        //    {
        //        Database.RollBackTransaction();
        //        ErrorMessageBox.Show(exObj);
        //    }
        //}

        private void textQty_KeyPress(object sender, KeyPressEventArgs e)
        {
            Utility.AllowFloatOnly(e);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            frmProductionPlantMaster frm = new frmProductionPlantMaster();
            frm.Show();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            frmProductionPlantItemMaster frm = new frmProductionPlantItemMaster();
            frm.Show();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            try
            {
                BindGrid();
                DataGridViewComboBoxColumn Col2 = (DataGridViewComboBoxColumn)dataGridView1.Columns[1];
                if (Col2.Items.Count != 0) Col2.Items.Clear();
                if (Database.OpenConnection(Utility.MaterialConnectionString))
                {
                    Database.myreader = Database.GetExecuteReaderCommand("select distinct(ItemName) from warehouse with (nolock) where warehousename = '" + comboWareHosueName.Text + "' "
                        + " and subgroupname = '" + dataGridView1.Rows[dataGridView1.CurrentRow.Index].Cells[0].Value + "' and companyname = '" + frmDefaultVale.CompanyName + "' order by itemname");
                    while (Database.myreader.Read())
                        Col2.Items.Add(Database.myreader[0].ToString());
                    Database.myreader.Close();
                    Database.Closeconnection();

                    for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                    {
                        Col2.Items.Add(dataGridView1.Rows[i].Cells[1].Value.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
            }
        }

        private void dataGridView1_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            double TotalBags = 0;
            try
            {
                {
                    if (comboPlant.Text == "Tape Plant")
                    {
                        //Find Total Bags :
                        for (int i = 0; i <= dataGridView1.Rows.Count - 1; i++)
                        {
                            if (Utility.SafeConvertToDouble(Convert.ToString(dataGridView1.Rows[i].Cells[colBag.Index].Value)) > 0)
                            {
                                if (dataGridView1.Rows[i].Cells[DtQuality.Index].Value.ToString() == "PP" ||
                                    dataGridView1.Rows[i].Cells[DtQuality.Index].Value.ToString() == "PP Granuals" ||
                                    dataGridView1.Rows[i].Cells[DtQuality.Index].Value.ToString() == "HD" ||
                                    dataGridView1.Rows[i].Cells[DtQuality.Index].Value.ToString() == "HDPE Granuals"
                                    )
                                {
                                    TotalBags = Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[colBag.Index].Value.ToString()) * 25;
                                    dataGridView1.Rows[i].Cells[DtQty.Index].Value = TotalBags;
                                }
                            }
                        }
                        if (TotalBags > 0)
                        {
                            for (int i = 0; i <= dataGridView1.Rows.Count - 1; i++)
                            {
                                if (Utility.SafeConvertToDouble(Convert.ToString(dataGridView1.Rows[i].Cells[colBag.Index].Value)) == 0)
                                {
                                    if (dataGridView1.Rows[i].Cells[DtQuality.Index].Value.ToString() == "PP" ||
                                        dataGridView1.Rows[i].Cells[DtQuality.Index].Value.ToString() == "PP Granuals" ||
                                        dataGridView1.Rows[i].Cells[DtQuality.Index].Value.ToString() == "HD" ||
                                        dataGridView1.Rows[i].Cells[DtQuality.Index].Value.ToString() == "HDPE Granuals"
                                        )
                                    {

                                    }
                                    else
                                    {
                                        dataGridView1.Rows[i].Cells[DtQty.Index].Value = Math.Round(Utility.SafeConvertToDouble(TotalBags * Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[colPer.Index].Value) / 100), 0);
                                    }
                                }
                            }
                            if (dataGridView1.CurrentCell.ColumnIndex == colBag.Index)
                            {
                                if (dataGridView1.Rows[dataGridView1.CurrentRow.Index].Cells[DtQuality.Index].Value.ToString() == "PP" ||
                                        dataGridView1.Rows[dataGridView1.CurrentRow.Index].Cells[DtQuality.Index].Value.ToString() == "PP Granuals" ||
                                        dataGridView1.Rows[dataGridView1.CurrentRow.Index].Cells[DtQuality.Index].Value.ToString() == "HD" ||
                                        dataGridView1.Rows[dataGridView1.CurrentRow.Index].Cells[DtQuality.Index].Value.ToString() == "HDPE Granuals")
                                {
                                    TotalBags = Utility.SafeConvertToDouble(dataGridView1.Rows[dataGridView1.CurrentRow.Index].Cells[colBag.Index].Value.ToString()) * 25;
                                    dataGridView1.Rows[dataGridView1.CurrentRow.Index].Cells[DtQty.Index].Value = TotalBags;
                                }
                                else
                                {
                                    dataGridView1.Rows[dataGridView1.CurrentRow.Index].Cells[colBag.Index].Value = "";
                                }
                            }
                            if (dataGridView1.CurrentCell.ColumnIndex == colPer.Index)
                            {
                                if (dataGridView1.Rows[dataGridView1.CurrentRow.Index].Cells[DtQuality.Index].Value.ToString() == "PP" ||
                                        dataGridView1.Rows[dataGridView1.CurrentRow.Index].Cells[DtQuality.Index].Value.ToString() == "PP Granuals" ||
                                        dataGridView1.Rows[dataGridView1.CurrentRow.Index].Cells[DtQuality.Index].Value.ToString() == "HD" ||
                                        dataGridView1.Rows[dataGridView1.CurrentRow.Index].Cells[DtQuality.Index].Value.ToString() == "HDPE Granuals")
                                {
                                    dataGridView1.Rows[dataGridView1.CurrentRow.Index].Cells[colPer.Index].Value = "";
                                }
                                else
                                {
                                    dataGridView1.Rows[dataGridView1.CurrentRow.Index].Cells[DtQty.Index].Value = Math.Round(Utility.SafeConvertToDouble(TotalBags * Utility.SafeConvertToDouble(dataGridView1.Rows[dataGridView1.CurrentRow.Index].Cells[colPer.Index].Value) / 100), 0);
                                }
                            }
                        }
                    }
                    if (dataGridView1.CurrentRow.Cells.IndexOf(dataGridView1.CurrentCell) == 0)
                    {
                        DataGridViewComboBoxCell Col2 = (DataGridViewComboBoxCell)dataGridView1.Rows[dataGridView1.CurrentRow.Index].Cells[1];

                        if (Database.OpenConnection(Utility.MaterialConnectionString))
                        {
                            DataTable dt1 = new DataTable();
                            Database.myreader = Database.GetExecuteReaderCommand("select  distinct ItemName  from warehouse with (nolock) where  subgroupname = '" + dataGridView1.Rows[dataGridView1.CurrentRow.Index].Cells[0].Value + "' and companyname = '"
                                + comboCompanyName.Text + "' and warehousename = '" + comboWareHosueName.Text + "' and itemcode in ( select itemcode from item where companyname='" + comboCompanyName.Text + "' and "
                                + "(('" + dateTimePicker1.Value.ToString("yyyy-MM-dd") + "' BETWEEN FROMDATE AND TODATE) OR (FROMDATE <='"
                                + dateTimePicker1.Value.ToString("yyyy-MM-dd") + "' AND TODATE IS NULL)))  and  round(StkInHand,2)>0   order by itemname");

                            dt1.Load(Database.myreader);

                            Col2.DataSource = dt1;
                            Col2.ValueMember = dt1.Columns[0].ColumnName.ToString();
                            Col2.DisplayMember = dt1.Columns[1].ColumnName.ToString();
                            if (!this.IsTranStarted) //23.12.2021
                                Database.Closeconnection();

                        }
                    }

                }
            }
            catch (Exception ex)
            {
                ex.ToString();
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            frmProductionSectorMaster frm = new frmProductionSectorMaster();
            frm.Show();
        }

        private void comboPlant_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (comboPlant.Text == "System.Data.DataRowView") return;

                comboWareHosueName.Text = "";
                comboWareHouseName_To.Text = "";
                comboWareHosueName.SelectedIndex = -1;
                comboWareHouseName_To.SelectedIndex = -1;

                comboPlantSubName.Items.Clear();
                comboPlantSubName.Text = "";
                //comboWareHouseName_To.Items.Clear();
                //comboWareHouseName_To.Text = "";
                comboItemName.Items.Clear();
                comboItemName.Text = "";
                //comboWareHosueName.Items.Clear();
                //comboWareHosueName.Text = "";
                isclear = true;
                dataGridView1.Rows.Clear();
                isclear = false;
                dataGridView1.Columns[colBag.Index].Visible = false;
                dataGridView1.Columns[colPer.Index].Visible = false;

                dataGridView1.Columns[colPer.Index].Width = 40;
                dataGridView1.Columns[colBag.Index].Width = 40;

                dataGridView1.Columns[DtQuality.Index].Width = 200;
                dataGridView1.Columns[DtGrade.Index].Width = 220;

                if (comboPlant.Text.ToUpper() == "LAMINATION")
                {
                    txtTrimWastage.Visible = true;
                    lblTrimWastage.Visible = true;
                    txt_sweeping_wastage.Visible = true;
                    lbl_Sweeping_wastage.Visible = true;

                    label23.Visible = true;
                    label21.Visible = true;
                    label22.Visible = true;

                    TxtFabricTrm.Visible = true;
                    TxtFabricwaste.Visible = true;
                    TxtLumpsWs.Visible = true;
                    //and  right(RollNo,1)!='D' 29.08.2020 allow 1 time Lam Roll weight for Double Lamination
                    //string strql = "Select round(isnull(sum(netwt)-sum(unetwt2),0),2) from (select ROW_NUMBER () over(partition by Urollno order by urollno) as Sr,  case when ROW_NUMBER () over(partition by Urollno order by urollno) =1  then UNetWt else 0 end as UNetWt2, * "
                    //                + " from MISlaminationentry with (nolock) where companyname='" + comboCompanyName.Text
                    //                + "' and sysdate='" + dateSysdate.Value.ToString("yyyy-MM-dd") + "' and hodApprove='Approved' and " +
                    //                " Shift='" + comboShift.Text + "' and NetWt>0 ) as  S";

                    #region 2021.01.11  Changes in Lamincation entry, not allow same split rows to be same UL Netwt. as per entry needs to be change in consumption entry.
                    //string strql = "   SELECT round(isnull(sum(netwt)-sum(unetwt2),0),2) FROM (select ROW_NUMBER () over(partition by Urollno order by urollno) as Sr,case when ROW_NUMBER () over(partition by Urollno order by urollno) =1   then TOTALULNETWT else 0 end as UNetWt2,* from (select SUM(UNetWt) OVER(PARTITION BY Urollno ORDER BY Urollno) TOTALULNETWT,* "
                    //        + " from MISlaminationentry with (nolock) where companyname='" + comboCompanyName.Text
                    //        + "' and sysdate='" + dateSysdate.Value.ToString("yyyy-MM-dd") + "' and hodApprove='Approved' and " +
                    //        " Shift='" + comboShift.Text + "' and NetWt>0 ) as  S ) as S";
                    //Change related to Only Entries done in Double Lamination for rolls then we will only consider Total Unetwt 13.Dec.2021
                    //string strql = "   SELECT round(isnull(sum(netwt)-sum(unetwt2),0),2) FROM (select ROW_NUMBER () over(partition by Urollno order by urollno) as Sr, "
                    //        + " case when ROW_NUMBER () over(partition by Urollno order by urollno) =1   then TOTALULNETWT else 0 end as UNetWt2,* "
                    //        + " from (select case when FGItemCode is null then UNetWt else SUM(UNetWt) OVER(PARTITION BY Urollno ORDER BY Urollno) end TOTALULNETWT,* "
                    //        + " from MISlaminationentry with (nolock) where companyname='" + comboCompanyName.Text
                    //        + "' and sysdate='" + dateSysdate.Value.ToString("yyyy-MM-dd") + "' and hodApprove='Approved' and "
                    //        + " Shift='" + comboShift.Text + "' and NetWt>0 ) as  S ) as S";
                    //End by Raj

                    //By Raj 23.12.2021 Remove Hod Approval at time when prod/Consumption entry is started 
                    string strql = "   SELECT round(isnull(sum(netwt)-sum(unetwt2),0),2) FROM (select ROW_NUMBER () over(partition by Urollno order by urollno) as Sr,case when ROW_NUMBER () over(partition by Urollno order by urollno) =1   then TOTALULNETWT else 0 end as UNetWt2,* from (select SUM(UNetWt) OVER(PARTITION BY Urollno ORDER BY Urollno) TOTALULNETWT,* "
                            + " from MISlaminationentry with (nolock) where companyname='" + comboCompanyName.Text
                            + "' and sysdate='" + dateSysdate.Value.ToString("yyyy-MM-dd") + "' and   " +
                            " Shift='" + comboShift.Text + "' and NetWt>0 and Isnull(IsCons,0)=1 ) as  S ) as S";
                    //End  by Raj
                    double dblCnt = Utility.SafeConvertToDouble(Database.GetExecuteNonQueryCommand_retrun(strql));
                    TXTROLLPROD.Text = dblCnt.ToString();
                    #endregion
                }
                else if (comboPlant.Text.ToUpper() == "Tape Plant".ToUpper())
                {
                    dataGridView1.Columns[DtQuality.Index].Width = 160;
                    dataGridView1.Columns[DtGrade.Index].Width = 200;

                    dataGridView1.Columns[colBag.Index].Visible = true;
                    dataGridView1.Columns[colPer.Index].Visible = true;
                }
                else if (comboPlant.Text.ToUpper() == "Liner Roll Plant".ToUpper())
                {

                    string strql = "   SELECT isnull(sum(netwt),0) as Netwt  "
                           + " from MISOutsideRollEntry with (nolock) where companyname='" + comboCompanyName.Text
                           + "' and sysdate='" + dateSysdate.Value.ToString("yyyy-MM-dd") + "' and   " +
                           " storeinwardno='Production' ";

                    double dblCnt = Utility.SafeConvertToDouble(Database.GetExecuteNonQueryCommand_retrun(strql));
                    TXTROLLPROD.Text = dblCnt.ToString();


                    txtTrimWastage.Visible = false;
                    lblTrimWastage.Visible = false;
                    txt_sweeping_wastage.Visible = false;
                    lbl_Sweeping_wastage.Visible = false;

                    label23.Visible = false;
                    label21.Visible = false;
                    label22.Visible = false;

                    TxtFabricTrm.Visible = false;
                    TxtFabricwaste.Visible = false;
                    TxtLumpsWs.Visible = false;

                }
                else
                {
                    txtTrimWastage.Visible = false;
                    lblTrimWastage.Visible = false;
                    txt_sweeping_wastage.Visible = false;
                    lbl_Sweeping_wastage.Visible = false;

                    label23.Visible = false;
                    label21.Visible = false;
                    label22.Visible = false;

                    TxtFabricTrm.Visible = false;
                    TxtFabricwaste.Visible = false;
                    TxtLumpsWs.Visible = false;

                }

                //(TrimWastage + Sweeping Wastage textbox & label are used for Lumps Wastage & Others Wastage)
                if (comboPlant.Text.ToUpper() == "TAPE PLANT")
                {
                    lblTrimWastage.Text = "Lumps Wastage :";
                    lbl_Sweeping_wastage.Text = "Others Wastage :";
                    txtTrimWastage.Visible = true;
                    lblTrimWastage.Visible = true;
                    txt_sweeping_wastage.Visible = true;
                    lbl_Sweeping_wastage.Visible = true;
                }
                else
                {
                    lblTrimWastage.Text = "Triming Wastage :";
                    lbl_Sweeping_wastage.Text = "Sweeping Wastage :";
                    if (comboPlant.Text.ToUpper() != "LAMINATION")
                    {
                        txtTrimWastage.Visible = false;
                        lblTrimWastage.Visible = false;
                        txt_sweeping_wastage.Visible = false;
                        lbl_Sweeping_wastage.Visible = false;
                    }
                }

                if (Database.OpenConnection(Utility.MaterialConnectionString))
                {
                    Database.myreader = Database.GetExecuteReaderCommand("select plantsubname from plantsubmaster where plantname = '"
                     + comboPlant.Text + "' and companyname = '" + comboCompanyName.Text + "' ");
                    while (Database.myreader.Read())
                        comboPlantSubName.Items.Add(Database.myreader[0].ToString());
                    Database.myreader.Close();
                    if (FrmMain.IsProductionadmin)
                    {
                        DataTable dt1 = new DataTable();

                        Database.myreader = Database.GetExecuteReaderCommand("Select distinct(towarehouse) as towarehouse from plantmaster  " +
                        " where companyname='" + comboCompanyName.Text + "' and plantname = '" + comboPlant.Text + "'  order by towarehouse");
                        dt1.Load(Database.myreader);
                        comboWareHouseName_To.DataSource = dt1;
                        comboWareHouseName_To                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                               .DisplayMember = "towarehouse";
                        comboWareHouseName_To.ValueMember = "towarehouse";

                        //while (Database.myreader.Read())
                        //{
                        //    comboWareHouseName_To.Items.Add(Database.myreader[0].ToString());
                        //}
                        Database.myreader.Close();

                        DataTable dt2 = new DataTable();

                        Database.myreader = Database.GetExecuteReaderCommand("Select distinct(fromwarehouse) as fromwarehouse from plantmaster " +
                        " where companyname='" + comboCompanyName.Text + "' and plantname = '" + comboPlant.Text + "'  order by fromwarehouse");

                        dt2.Load(Database.myreader);
                        comboWareHosueName.DataSource = dt2;
                        comboWareHosueName.DisplayMember = "fromwarehouse";
                        comboWareHosueName.ValueMember = "fromwarehouse";

                        //while (Database.myreader.Read())
                        //{
                        //    comboWareHosueName.Items.Add(Database.myreader[0].ToString());
                        //}
                        Database.myreader.Close();
                    }
                    else
                    {
                        comboWareHouseName_To.DataSource = ProductionCommonFunction.GetToPlantList_userWise(comboCompanyName.Text, comboPlant.Text, FrmMain.UserName);
                        comboWareHouseName_To.DisplayMember = ProductionCommonVariables.FieldName.towarehouse;
                        comboWareHouseName_To.ValueMember = ProductionCommonVariables.FieldName.towarehouse;

                        comboWareHosueName.DataSource = ProductionCommonFunction.GetToPlantList_userWise(comboCompanyName.Text, comboPlant.Text, FrmMain.UserName);
                        comboWareHosueName.DisplayMember = ProductionCommonVariables.FieldName.fromwarehouse;
                        comboWareHosueName.ValueMember = ProductionCommonVariables.FieldName.fromwarehouse;
                    }

                    if (comboWareHouseName_To.Items.Count > 0)
                        comboWareHouseName_To.SelectedIndex = 0;
                    if (comboWareHosueName.Items.Count > 0)
                        comboWareHosueName.SelectedIndex = 0;

                    if (!this.IsTranStarted)
                        Database.Closeconnection();
                }
                if (comboPlantSubName.Items.Count > 0)
                    comboPlantSubName.SelectedIndex = 0;
                BindGrid();
            }
            catch (Exception ex)
            {

            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            frmProductionSubDeptt frm = new frmProductionSubDeptt();
            frm.Show();
        }

        private void dataGridView1_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {

        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            try
            {
                if (!FrmMain.IsProductionadmin)
                {
                    MessageBox.Show("You Don't have rights to delete this record.");
                    return;
                }
                DataTable dataDt = new DataTable();
                if (Database.OpenConnection(Utility.MaterialConnectionString))
                {
                    Database.BeginTransaction();

                    decimal QSum = 0;

                    dataDt = Database.GetDataTable("select * from TapePlantConsumption where GroupSrNo = '" + GroupSrNo + "'");
                    for (int i = 0; i < dataGridView1.Rows.Count; i++)
                    {
                        QSum += Convert.ToDecimal(dataGridView1.Rows[i].Cells["DtQty"].Value);
                    }

                    Database.myreader = Database.GetExecuteReaderCommand("select StkInHand,ItemCode from warehouse where ItemName = '" + comboItemName.Text + "' and WareHouseName = '" + comboWareHouseName_To.Text + "' and CompanyName = '" + comboCompanyName.Text + "'");
                    Database.myreader.Read();
                    DateTime dt = dateSysdate.Value;
                    if (Database.myreader.HasRows)
                    {
                        if (QSum > Convert.ToDecimal(Database.myreader[0].ToString()))
                        {
                            MessageBox.Show("There is no enough stock of item '" + Database.myreader[1].ToString() + "' in '" + comboWareHouseName_To.Text + "' warehouse");
                            return;
                        }
                        else
                        {
                            string itemcode = Convert.ToString(Database.myreader[1]);
                            ProductionCommonFunction.WareHouseStoreoutwards(itemcode, comboItemName.Text, Convert.ToDouble(QSum)
                                , dt, comboCompanyName.Text, comboWareHosueName.Text, comboWareHouseName_To.Text);
                            Database.ExecuteNonQuery("update warehouse set StkInHand = StkInHand - " + QSum + " where itemcode = '"
                            + itemcode
                                + "' and WareHouseName = '" + comboWareHouseName_To.Text + "' and CompanyName = '" + comboCompanyName.Text + "' ");
                        }
                    }
                    for (int i = 0; i < dataDt.Rows.Count; i++)
                    {
                        string itemname = ProductionCommonFunction.GetItemName(dataDt.Rows[i]["itemcode"].ToString());
                        ProductionCommonFunction.WareHouseStoreinwards(dataDt.Rows[i]["itemcode"].ToString(),
                        itemname, Convert.ToDouble(dataDt.Rows[i]["Qty"].ToString()), dt, comboCompanyName.Text, comboWareHosueName.Text, comboWareHouseName_To.Text);

                        Database.ExecuteNonQuery("update warehouse set StkInHand = StkInHand + " + Convert.ToDecimal(dataDt.Rows[i]["Qty"].ToString())
                            + " where warehousename = '" + comboWareHosueName.Text + "' and itemcode = '" + dataDt.Rows[i]["itemcode"] + "' and companyname = '" + comboCompanyName.Text + "'");
                    }

                    Database.ExecuteNonQuery("delete from TapePlantConsumption where GroupSrNo = '" + GroupSrNo + "' and PlantName='" + comboPlant.Text + "'");
                    Database.ExecuteNonQuery("delete from Prod_Transaction where tid = '" + GroupSrNo + "'");
                    Database.ExecuteNonQuery("delete from stockJournalprod where srno = " + GroupSrNo + " and company_name='" + comboCompanyName.Text + "'"); // delete stock jv on dated 18th June 2018

                    Database.CommitTransaction();
                }
                BindGrid();
                dataGridView1.Rows.Clear();

                MessageBox.Show("Consumption entry deleted sucessfully..!!!");

            }
            catch (Exception exObj)
            {
                Database.RollBackTransaction();
                ErrorMessageBox.Show(exObj);
            }
        }

        private void comboItemName_SelectedIndexChanged(object sender, EventArgs e)
        {
            //using (System.Data.SqlClient.SqlConnection conn = new System.Data.SqlClient.SqlConnection(Utility.MaterialConnectionString))
            //{
            //    //  lvTextFile.Enabled = false;
            //    string strsql = "select distinct Quality,Grade,transid from TapePlantConsumption where PlantName='" + comboPlant.Text + "' and companyname='"
            //        + comboCompanyName.Text + "' and PlantItemName='" + comboItemName.Text + "' and GroupSrNo in (select max(GroupSrNo) from TapePlantConsumption where  "
            //        + " PlantName='" + comboPlant.Text + "' and companyname='" + comboCompanyName.Text + "' and PlantItemName='" + comboItemName.Text + "') order by transid asc";
            //    System.Data.SqlClient.SqlCommand command = new System.Data.SqlClient.SqlCommand(strsql, conn);
            //    conn.Open();

            //    System.Data.SqlClient.SqlDataReader reader = command.ExecuteReader();

            //    dataGridView1.Rows.Clear();
            //    while (reader.Read())
            //    {
            //        dataGridView1.Rows.Add(reader[0].ToString(), reader[1].ToString());
            //    }
            //    reader.Close();
            //    lvTextFile.Visible = false;
            //    //Database.Closeconnection();
            //    //  fillData();            
            //}

        }

        private void comboSector_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void comboWareHosueName_SelectedIndexChanged(object sender, EventArgs e)
        {
            fillData();
        }

        private void dataGridView1_CurrentCellChanged(object sender, EventArgs e)
        {
            //try
            //{
            if (dataGridView1.CurrentCell == null) return;

            if (!isclear)
            {

                if (dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[1].FormattedValue.ToString().Trim() != "")
                {
                    double Production = 0;
                    if (Database.OpenConnection(Utility.MaterialConnectionString))
                    {
                        Database.myreader = Database.GetExecuteReaderCommand("select round(stkinhand,2) from warehouse where Itemname = '"
                        + dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[1].FormattedValue.ToString() + "' and warehousename = '"
                        + comboWareHosueName.Text + "' and CompanyName='" + comboCompanyName.Text + "' and itemcode in ( select itemcode from item where companyname='" + comboCompanyName.Text + "' and "
                    + "(('" + dateTimePicker1.Value.ToString("yyyy-MM-dd") + "' BETWEEN FROMDATE AND TODATE) OR (FROMDATE <='"
                    + dateTimePicker1.Value.ToString("yyyy-MM-dd") + "' AND TODATE IS NULL))) ");
                        if (Database.myreader.Read())
                        {
                            dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[3].Value = Convert.ToString(Math.Round(Convert.ToDouble(Database.myreader[0].ToString()), 2));
                        }
                        Database.myreader.Close();
                    }
                    //if (dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[DtQty.Index].FormattedValue.ToString().Trim() != "")
                    {
                        textProduction.Text = "0";

                        for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                        {
                            //if (dataGridView1.Rows[i].Cells[DtQty.Index] != null)
                            Production += Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[DtQty.Index].FormattedValue.ToString());
                        }
                    }

                    textProduction.Text = Convert.ToString(Production);

                    //if (lblTrimWastage.Text == "Lumps Wastage :")
                    //{
                    //    textNet.Text = Convert.ToString(Utility.SafeConvertToDouble(textProduction.Text) - Utility.SafeConvertToDouble(textWastage.Text) - Utility.SafeConvertToDouble(txtTrimWastage.Text));
                    //}
                    //if (textWastage.Text != "")
                    //    textNet.Text = Convert.ToString(Convert.ToDouble(textProduction.Text) - Convert.ToDouble(textWastage.Text));

                    textNet.Text = Convert.ToString(Utility.SafeConvertToDouble(textProduction.Text) - Utility.SafeConvertToDouble(textWastage.Text) -
                        Utility.SafeConvertToDouble(txtTrimWastage.Text) - Utility.SafeConvertToDouble(txt_sweeping_wastage.Text) - Utility.SafeConvertToDouble(TxtFabricTrm.Text) -
                        Utility.SafeConvertToDouble(TxtFabricwaste.Text) - Utility.SafeConvertToDouble(TxtLumpsWs.Text));

                }
            }
            //}
            //catch (Exception ex)
            //{
            //    ErrorMessageBox.Show(ex);
            //}
        }

        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            try
            {
                double sum = 0;
                pfrm_sbp1.Text = "";

                foreach (DataGridViewRow row in this.dataGridView1.Rows)
                {
                    foreach (DataGridViewColumn col in this.dataGridView1.Columns)
                    {
                        if (dataGridView1[col.Index, row.Index].Selected)
                            try
                            {
                                sum += Convert.ToDouble(dataGridView1[col.Index, row.Index].Value);
                            }
                            catch (Exception ex)
                            {
                                ex.ToString();
                            }
                    }
                }
                pfrm_sbp1.Text = " Sum : " + sum.ToString();

            }
            catch (Exception ex)
            {
                ex.ToString();
            }
        }
        private void TextItemName_KeyUp(object sender, EventArgs e)
        {

        }

        private void comboItemName_KeyUp(object sender, KeyEventArgs e)
        {
            try
            {
                lvTextFile.Clear();
                if (comboItemName.Text.Length >= 3)
                {
                    if (Database.OpenConnection_Adapter(Utility.MaterialConnectionString))
                    {
                        Database.myadapter = Database.GetAdapterCommand("select plantitemname from Plantitemmaster where plantitemname like '%"
                             + comboItemName.Text + "%' and companyname = '" + frmDefaultVale.CompanyName + "' and plantname = '"
                        + comboPlant.Text + "' order by plantitemname");
                        DataSet dataset1 = new DataSet();
                        Database.myadapter.Fill(dataset1, "itemcodes");
                        SetupListView(dataset1);

                        for (int i = 0; i < lvTextFile.Columns.Count; i++)
                            lvTextFile.Columns[i].Width = 250;

                        string errorInfo = String.Empty;
                        //show data in list view:
                        if (!ShowDataInListView(dataset1, out errorInfo))
                        {
                            MessageBox.Show("Failed to display text file (listview) :\n" + errorInfo,
                                "Load Text File");
                            return;
                        }
                    }
                    lvTextFile.Visible = true;
                }
            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
            }

        }
        private bool SetupListView(DataSet dataToShow)
        {
            try
            {
                lvTextFile.Columns.Clear();
                lvTextFile.View = View.Details;
                lvTextFile.FullRowSelect = true;
                lvTextFile.GridLines = true;
                lvTextFile.MultiSelect = false;

                foreach (DataColumn textColumn in dataToShow.Tables[0].Columns)
                {
                    ColumnHeader header = new ColumnHeader();
                    header.Text = textColumn.Caption;
                    lvTextFile.Columns.Add(header);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
        private bool ShowDataInListView(DataSet dataToShow, out string errorInfo)
        {
            errorInfo = String.Empty;
            try
            {
                lvTextFile.Items.Clear();
                foreach (DataRow textRow in dataToShow.Tables[0].Rows)
                {
                    ListViewItem li = new ListViewItem(textRow[0].ToString());
                    for (int i = 1; i < dataToShow.Tables[0].Columns.Count; i++)
                    {
                        li.SubItems.Add(textRow[i].ToString());
                    }
                    lvTextFile.Items.Add(li);
                }

                return true;
            }
            catch (Exception ex_show_data)
            {
                errorInfo = ex_show_data.Message;
                return false;
            }
        }

        private void lvTextFile_SelectedIndexChanged(object sender, EventArgs e)
        {
            comboItemName.Text = lvTextFile.FocusedItem.SubItems[0].Text;
        }

        private void comboItemName_Leave(object sender, EventArgs e)
        {
            lvTextFile.Visible = false;
        }
        private void Calculate()
        {
            try
            {
                //added by raj 22-05-2020 for re-check net production before validation 
                textNet.Text = Convert.ToString(Utility.SafeConvertToDouble(textProduction.Text) - Utility.SafeConvertToDouble(textWastage.Text) -
                Utility.SafeConvertToDouble(txtTrimWastage.Text) - Utility.SafeConvertToDouble(txt_sweeping_wastage.Text) - Utility.SafeConvertToDouble(TxtFabricTrm.Text) -
                Utility.SafeConvertToDouble(TxtFabricwaste.Text) - Utility.SafeConvertToDouble(TxtLumpsWs.Text));
                //end by raj 22-05-2020
            }
            catch (Exception ex)
            {
                MessageBox.Show("Calculate :" + ex.Message);
            }
        }
        private void textWastage_KeyUp(object sender, KeyEventArgs e)
        {
            try
            {
                //double Wastage = Utility.SafeConvertToDouble(textWastage.Text);
                //double trimWastage = Utility.SafeConvertToDouble(txtTrimWastage.Text);
                //double Production = Utility.SafeConvertToDouble(textProduction.Text);
                //if (lblTrimWastage.Text == "Lumps Wastage :")
                //{
                //    textNet.Text = Convert.ToString(Production - Wastage - trimWastage);
                //}
                //else
                //{
                //    textNet.Text = Convert.ToString(Production - Wastage);
                //}
                Calculate();

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }

        private void button7_Click(object sender, EventArgs e)
        {

        }

        private void comboWareHouseName_To_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (Database.OpenConnection(Utility.MaterialConnectionString))
            {
                if (comboWareHouseName_To.Text == "Bobbin Godown")
                {
                    Database.myreader = Database.GetExecuteReaderCommand("select distinct(itemname) from warehouse with (nolock) where companyname = '"
                    + comboCompanyName.Text + "' and warehousename = '"
                    + comboWareHouseName_To.Text + "' and  itemcode in ( select itemcode from item where MISGRUPITEM='PP/HD TAPE' AND companyname='" + comboCompanyName.Text + "' and "
                    + "(('" + dateTimePicker1.Value.ToString("yyyy-MM-dd") + "' BETWEEN FROMDATE AND TODATE) OR (FROMDATE <='"
                    + dateTimePicker1.Value.ToString("yyyy-MM-dd") + "' AND TODATE IS NULL))) ");
                }
                else if (comboWareHouseName_To.Text == "Raw Material Godown")
                {
                    Database.myreader = Database.GetExecuteReaderCommand("select distinct(itemname) from warehouse with (nolock) where companyname = '"
                    + comboCompanyName.Text + "' and warehousename = '"
                    + comboWareHouseName_To.Text + "' and itemcode in ( select itemcode from item where companyname='" + comboCompanyName.Text + "' and "
                    + "(('" + dateTimePicker1.Value.ToString("yyyy-MM-dd") + "' BETWEEN FROMDATE AND TODATE) OR (FROMDATE <='"
                    + dateTimePicker1.Value.ToString("yyyy-MM-dd") + "' AND TODATE IS NULL))) ");
                }
                else
                {
                    Database.myreader = Database.GetExecuteReaderCommand("select distinct(itemname) from warehouse with (nolock) where companyname = '"
                    + comboCompanyName.Text + "' and warehousename = '"
                    + comboWareHouseName_To.Text + "' and itemcode in ( select itemcode from item where companyname='" + comboCompanyName.Text + "' and "
                    + "(('" + dateTimePicker1.Value.ToString("yyyy-MM-dd") + "' BETWEEN FROMDATE AND TODATE) OR (FROMDATE <='"
                    + dateTimePicker1.Value.ToString("yyyy-MM-dd") + "' AND TODATE IS NULL))) AND ITEMNAME IN ("
                    + " select distinct(plantitemname) from plantitemmaster with (nolock) where companyname = '"
                      + comboCompanyName.Text + "' and plantname = '" + comboPlant.Text + "' and plantitemname in ( select distinct ItemName from item where companyname='" + comboCompanyName.Text + "' and "
                    + "(('" + dateTimePicker1.Value.ToString("yyyy-MM-dd") + "' BETWEEN FROMDATE AND TODATE) OR (FROMDATE <='"
                    + dateTimePicker1.Value.ToString("yyyy-MM-dd") + "' AND TODATE IS NULL)))  ) "
                    );

                    //Database.myreader = Database.GetExecuteReaderCommand("select distinct(plantitemname) from plantitemmaster with (nolock) where companyname = '"
                    //  + comboCompanyName.Text + "' and plantname = '" + comboPlant.Text + "' and plantitemname in ( select distinct ItemName from item where companyname='" + comboCompanyName.Text + "' and "
                    //+ "(('" + dateTimePicker1.Value.ToString("yyyy-MM-dd") + "' BETWEEN FROMDATE AND TODATE) OR (FROMDATE <='"
                    //+ dateTimePicker1.Value.ToString("yyyy-MM-dd") + "' AND TODATE IS NULL)))  order by plantitemname");

                }
                comboItemName.Items.Clear();
                while (Database.myreader.Read())
                {
                    comboItemName.Items.Add(Database.myreader[0].ToString());
                }
                Database.myreader.Close();

                if (comboItemName.Items.Count > 0)
                    comboItemName.SelectedIndex = 0;

            }
        }

        private void comboPlantSubName_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void comboBuyerName_SelectedIndexChanged(object sender, EventArgs e)
        {


            //
            DataTable dtMarketingDtl = dtOrderMaster.Copy();
            dtMarketingDtl.DefaultView.RowFilter = "PartyName='" + comboBuyerName.Text + "'";

            if (dtMarketingDtl.DefaultView.ToTable().Rows.Count <= 0) return;
            comboBuyerOrderNO.DataSource = dtMarketingDtl.DefaultView.ToTable(true, "PONO").Copy();
            comboBuyerOrderNO.DisplayMember = "PONO";
            comboBuyerOrderNO.ValueMember = "PONO";

            comboBuyerOrderNO.SelectedIndex = 0;
            //comboMarketingInvNo.DataSource = dtMarketingDtl.DefaultView.ToTable(true, "MarketingOrdNo").Copy();
            //comboMarketingInvNo.DisplayMember = "MarketingOrdNo";
            //comboMarketingInvNo.ValueMember = "MarketingOrdNo";

            // comboMarketingInvNo.SelectedIndex = 0;
            //comboMarketingInvNo.Items.Clear();
            //comboBuyerOrderNO.Items.Clear();
            //comboBuyerOrderNO.Text = "";
            //comboMarketingInvNo.Text = "";
            //if (Database.OpenConnection(Utility.MaterialConnectionString))
            //{
            //    Database.myreader = Database.GetExecuteReaderCommand("select distinct(ItemNO) from Despatch..vw_ProductionCompAndMarkInvNo with(nolock) where"
            //                + " ProductionCompanyName ='" + comboCompanyName.Text + "' and buyername = '" + comboBuyerName.Text + "'  order by ItemNO   ");
            //    while (Database.myreader.Read())
            //        //comboBuyerOrderNO.Items.Add(Database.myreader[0].ToString());
            //        comboMarketingInvNo.Items.Add(Database.myreader[0].ToString());
            //    Database.myreader.Close();

            //    Database.myreader = Database.GetExecuteReaderCommand("select distinct(buyerorderno) from Despatch..vw_ProductionCompAndMarkInvNo with (nolock)"
            //    + " where ProductionCompanyName = '" + comboCompanyName.Text + "' and buyername='" + comboBuyerName.Text + "' order by buyerorderno");
            //    // Database.myreader = Database.GetExecuteReaderCommand("select distinct(buyerorderno) from Despatch..MarketingInvoice where " +
            //    //     " companyname='" + comboCompanyName.Text + "' and buyername='" + comboBuyerName.Text + "'  and marketingINVNo not in  " +
            //    //"  (Select orderno from Despatch..freezemarketingorder where companyname = '" + comboCompanyName.Text + "') order by buyerorderno");
            //    while (Database.myreader.Read())

            //        comboBuyerOrderNO.Items.Add(Database.myreader[0].ToString());

            //    Database.myreader.Close();


            //}
        }

        private void comboBuyerOrderNO_SelectedIndexChanged(object sender, EventArgs e)
        {
            DataTable dtMarketingOrderNo = dtOrderMaster.Copy();
            dtMarketingOrderNo.DefaultView.RowFilter = "PartyName='" + comboBuyerName.Text + "' and  PONO='" + comboBuyerOrderNO.Text + "'";
            if (dtMarketingOrderNo.DefaultView.ToTable().Rows.Count <= 0) return;
            comboMarketingInvNo.DataSource = dtMarketingOrderNo.DefaultView.ToTable(true, "MarketingOrdNo").Copy();
            comboMarketingInvNo.DisplayMember = "MarketingOrdNo";
            comboMarketingInvNo.ValueMember = "MarketingOrdNo";
            comboMarketingInvNo.SelectedIndex = 0;
            //      comboBuyerOrderNO.SelectedIndex = comboBuyerOrderNO.SelectedIndex;

        }

        private void comboMarketingInvNo_SelectedIndexChanged(object sender, EventArgs e)
        {

            // comboBuyerOrderNO.SelectedIndex = comboBuyerOrderNO.SelectedIndex;

            //DataTable dtMarketingOrderNo = dtOrderMaster.Copy();
            //dtMarketingOrderNo.DefaultView.RowFilter = "PartyName='" + comboBuyerName.Text + "' and  MarketingOrdNo='" + comboMarketingInvNo.Text + "'";
            //if (dtMarketingOrderNo.DefaultView.ToTable().Rows.Count <= 0) return;
            //comboBuyerOrderNO.DataSource = dtMarketingOrderNo.DefaultView.ToTable(true, "PONO").Copy();
            //comboBuyerOrderNO.DisplayMember = "PONO";
            //comboBuyerOrderNO.ValueMember = "PONO";
            //comboBuyerOrderNO.SelectedIndex = 0;


            //comboBuyerOrderNO.Items.Clear();
            //comboBuyerOrderNO.Text = "";
            //if (Database.OpenConnection(Utility.MaterialConnectionString))
            //{
            //    Database.myreader = Database.GetExecuteReaderCommand("select distinct(buyerorderno) from Despatch..vw_ProductionCompAndMarkInvNo with(nolock) where"
            //       + " ProductionCompanyName ='" + comboCompanyName.Text + "' and buyername='" + comboBuyerName.Text + "' order by buyerorderno   ");
            //    //Database.myreader = Database.GetExecuteReaderCommand("select buyerorderno from Despatch..MarketingInvoice where " +
            //    //    " companyname='" + comboCompanyName.Text + "' and buyername='" + comboBuyerName.Text + "' and marketinginvno='" + comboMarketingInvNo.Text + "' order by buyerorderno");
            //    if (Database.myreader.Read())

            //        comboBuyerOrderNO.Text = Database.myreader[0].ToString();

            //    Database.myreader.Close();

            //    Database.myreader = Database.GetExecuteReaderCommand("select buyerdate from Despatch..vw_ProductionCompAndMarkInvNo with(nolock) where"
            //      + " ProductionCompanyName ='" + comboCompanyName.Text + "' and buyername='" + comboBuyerName.Text + "' and ItemNO='" + comboMarketingInvNo.Text + "' order by buyerdate");
            //    //Database.myreader = Database.GetExecuteReaderCommand("select buyerdate from Despatch..MarketingInvoice where " +
            //    //            " companyname='" + comboCompanyName.Text + "' and buyername='" + comboBuyerName.Text + "' and marketinginvno='" + comboMarketingInvNo.Text + "' order by MarketingInvNo");
            //    if (Database.myreader.Read())
            //        dateTimePicker2.Text = Database.myreader[0].ToString();
            //    Database.myreader.Close();
            //    //Utility.ConvertDatesFromDDMMYY_To_MMDDYYYY(dateTimePicker2.Text);
            //    dateTimePicker2.Enabled = false;



            //}
        }

        //private void comboBuyerOrderNO_SelectedValueChanged(object sender, EventArgs e)
        //{

        //    if (Database.OpenConnection(Utility.MaterialConnectionString))
        //    {
        //        Database.myreader = Database.GetExecuteReaderCommand("select distinct(ItemNO) from vw_ProductionCompAndMarkInvNo with(nolock) where"
        //                    + " ProductionCompanyName ='" + comboCompanyName.Text + "' and buyername = '" + comboBuyerName.Text + "' and buyerorderno='" + comboBuyerOrderNO.Text + "' order by ItemNO   ");

        //        //Database.myreader = Database.GetExecuteReaderCommand("select MarketingInvNo from Despatch..MarketingInvoice where " +
        //        //            " companyname='" + comboCompanyName.Text + "' and buyername='" + comboBuyerName.Text + "'and buyerorderno='" + comboBuyerOrderNO.Text + "' order by MarketingInvNo");
        //        if (Database.myreader.Read())
        //            comboMarketingInvNo.Text = Database.myreader[0].ToString();
        //        Database.myreader.Close();


        //        Database.myreader = Database.GetExecuteReaderCommand("select buyerdate from Despatch..vw_ProductionCompAndMarkInvNo with(nolock) where"
        //                   + " ProductionCompanyName ='" + comboCompanyName.Text + "' and buyername = '" + comboBuyerName.Text + "' and buyerorderno='" + comboBuyerOrderNO.Text + "' order by ItemNO   ");


        //        //Database.myreader = Database.GetExecuteReaderCommand("select buyerdate from Despatch..MarketingInvoice where " +
        //        //            " companyname='" + comboCompanyName.Text + "' and buyername='" + comboBuyerName.Text + "'and buyerorderno='" + comboBuyerOrderNO.Text + "' order by MarketingInvNo");
        //        if (Database.myreader.Read())
        //            dateTimePicker2.Text = Database.myreader[0].ToString();
        //        Database.myreader.Close();
        //        //Utility.ConvertDatesFromDDMMYY_To_MMDDYYYY(dateTimePicker2.Text);
        //        dateTimePicker2.Enabled = false;


        //    }
        //}

        private void dateTimePicker1_ValueChanged(object sender, EventArgs e)
        {
            BindGrid();
        }

        private void dataGridView1_UserDeletedRow(object sender, DataGridViewRowEventArgs e)
        {
            isclear = false;
        }

        private void dataGridView1_UserDeletingRow(object sender, DataGridViewRowCancelEventArgs e)
        {
            isclear = true;
        }

        private void btnok_Click(object sender, EventArgs e)
        {
            BindGrid();
        }
        /// <summary>
        /// This event will change Rate from item Master base  FIFo method where no rate has been given
        /// Start checkk from begining then it will change Item rate where it's not been set. it will not change consumption entry only rate will be apply
        /// and from that Changes in Prodction Qty like Tape To Bobbin then Bobbin Prod cost will be change.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnRate_Click(object sender, EventArgs e)
        {
            try
            {
                //Get Plant wise Production rate
                int FGProdRate = 0;

                Database.myreader = Database.GetExecuteReaderCommand("Select isnull(FGProdRate,0) as FGProdRate  from PlantMaster where companyname='" + frmDefaultVale.CompanyName + "' and PlantName='" + comboPlant.Text + "'");
                if (Database.myreader.Read())
                    FGProdRate = Convert.ToInt16(Database.myreader[0].ToString());

                string strsql = "Select transid,itemcode,isnull(qty,0) as qty,isnull(RateConsumption,0) as RateConsumption from TapePlantConsumption where companyname='" + frmDefaultVale.CompanyName
                        + "' and PlantName='" + comboPlant.Text + "'  and sysDate >'2015-10-01' and isnull(RateConsumption,0)=0 order by sysDate desc";
                DataTable dt = new DataTable();
                dt = Database.GetDataTable(strsql);
                Database.BeginTransaction();
                IsReturn = true;
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    //Rate base on FIFO
                    int tranid = Convert.ToInt32(dt.Rows[i]["transid"].ToString());
                    string itemcode = dt.Rows[i]["itemcode"].ToString();
                    double dblqty = Convert.ToDouble(dt.Rows[i]["qty"].ToString());
                    double dblRate = Convert.ToDouble(dt.Rows[i]["RateConsumption"].ToString());
                    if (dblRate > 0)
                        continue;
                    dblRate = ProductionCommonFunction.GettingRate(itemcode, dblqty);
                    if (dblRate.ToString().Length <= 0)
                        continue;
                    dblRate += FGProdRate;
                    double ProdAmount = dblRate * dblqty;
                    //Add Wastage cost per 1 kg = 15 rs.
                    double dblwastagerate = 15;
                    strsql = "";
                    strsql = "update TapePlantConsumption set RateConsumption=" + dblRate +
                            ",RateWastage=" + dblwastagerate + ",RateProduction=" + ProdAmount + " where transid=" + tranid;

                    Database.GetExecuteNonQueryCommand(strsql);

                    //Utility.UserInformation("Update", "Plant Rate " + tranid.ToString() + " is Updated");

                }
                Database.CommitTransaction();
                IsReturn = false;
                MessageBox.Show("Rate Calculation updated");
                BindGrid();
                ClearAll(false);
            }
            catch (Exception ex)
            {
                if (IsReturn)
                    Database.RollBackTransaction();

                MessageBox.Show("Problem with rate Calculation : " + ex.Message);
            }
        }

        private void txtTrimWastage_KeyUp(object sender, KeyEventArgs e)
        {
            try
            {
                //if (lblTrimWastage.Text == "Lumps Wastage :")
                //{
                //    double TrimWastage = Utility.SafeConvertToDouble(txtTrimWastage.Text);
                //    double Wastage = Utility.SafeConvertToDouble(textWastage.Text);
                //    double Production = Utility.SafeConvertToDouble(textProduction.Text);
                //    textNet.Text = Convert.ToString(Production - Wastage - TrimWastage);
                //}
                Calculate();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }

        private void comboShift_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (comboPlant.Text.ToUpper() == "LAMINATION")
                {
                    //string strql = "   select round(isnull(sum(netwt)-sum(unetwt2),0),2) from (select ROW_NUMBER () over(partition by Urollno order by urollno) as Sr,  case when ROW_NUMBER () over(partition by Urollno order by urollno) =1 then UNetWt else 0 end as UNetWt2, * "
                    //                + " from MISlaminationentry with (nolock) where companyname='" + comboCompanyName.Text
                    //                + "' and sysdate='" + dateSysdate.Value.ToString("yyyy-MM-dd") + "' and hodApprove='Approved' and " +
                    //                " Shift='" + comboShift.Text + "' and NetWt>0 ) as  S";

                    //string strql = "   SELECT round(isnull(sum(netwt)-sum(unetwt2),0),2) FROM (select ROW_NUMBER () over(partition by Urollno order by urollno) as Sr,case when ROW_NUMBER () over(partition by Urollno order by urollno) =1   then TOTALULNETWT else 0 end as UNetWt2,* from (select SUM(UNetWt) OVER(PARTITION BY Urollno ORDER BY Urollno) TOTALULNETWT,* "
                    //        + " from MISlaminationentry with (nolock) where companyname='" + comboCompanyName.Text
                    //        + "' and sysdate='" + dateSysdate.Value.ToString("yyyy-MM-dd") + "' and hodApprove='Approved' and " +
                    //        " Shift='" + comboShift.Text + "' and NetWt>0 ) as  S ) as S";

                    //string strql = "   select round(isnull(sum(netwt)-sum(unetwt2),0),2) from (select ROW_NUMBER () over(partition by Urollno order by urollno) as Sr,  case when ROW_NUMBER () over(partition by Urollno order by urollno) =1 and  right(RollNo,1)!='D' then UNetWt else 0 end as UNetWt2, * "
                    //                + " from MISlaminationentry with (nolock) where companyname='" + comboCompanyName.Text
                    //                + "' and sysdate='" + dateSysdate.Value.ToString("yyyy-MM-dd") + "' and hodApprove='Approved' and " +
                    //                " Shift='" + comboShift.Text + "' and NetWt>0 ) as  S";
                    //23.12.2021 Show Consumption form from Production Entry.
                    string strql = "   SELECT round(isnull(sum(netwt)-sum(unetwt2),0),2) FROM (select ROW_NUMBER () over(partition by Urollno order by urollno) as Sr,case when ROW_NUMBER () over(partition by Urollno order by urollno) =1   then TOTALULNETWT else 0 end as UNetWt2,* from (select SUM(UNetWt) OVER(PARTITION BY Urollno ORDER BY Urollno) TOTALULNETWT,* "
                            + " from MISlaminationentry with (nolock) where companyname='" + comboCompanyName.Text
                            + "' and sysdate='" + dateSysdate.Value.ToString("yyyy-MM-dd") + "' and "
                            + " Shift='" + comboShift.Text + "' and NetWt>0 and Isnull(IsCons,0)=1 ) as  S ) as S";
                    //End By Raj 23.12.2021 Show Consumption form from Production Entry.
                    double dblCnt = Utility.SafeConvertToDouble(Database.GetExecuteNonQueryCommand_retrun(strql));
                    TXTROLLPROD.Text = dblCnt.ToString();
                }
            }
            catch (Exception ex)
            {

            }
        }

        private void txt_sweeping_wastage_KeyUp(object sender, KeyEventArgs e)
        {
            Calculate();
        }

        private void TxtFabricTrm_KeyUp(object sender, KeyEventArgs e)
        {
            Calculate();
        }

        private void TxtFabricwaste_KeyUp(object sender, KeyEventArgs e)
        {
            Calculate();
        }

        private void TxtLumpsWs_KeyUp(object sender, KeyEventArgs e)
        {
            Calculate();
        }

        // Rikin on 27-
        //void BobinConsumption()
        //{
        //    try
        //    {
        //        dtBobinStock = ProductionCommonFunction.GetBobinForLoom(comboCompanyName.Text, "Loom Godown");

        //        dtBobinConsummption = dtBobinStock.Clone();
        //        dtBobinConsummption.Columns.Add("ConsumedQty", Type.GetType("System.Double"));
        //        dtBobinConsummption.Columns.Add("RowId");
        //        dtBobinConsummption.Rows.Clear();

        //        for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
        //        {
        //            //Need To remove
        //            dtTemp = dtLoomAllocationMaster.Copy();
        //            dtTemp.DefaultView.RowFilter = "LoomNo=" + dataGridView1.Rows[i].Cells[0].Value;

        //            if (dtTemp.DefaultView.ToTable().Rows.Count == 0)
        //                throw new ERPErrorException("Please make entry of Loom Allocation master. ");

        //            double WarpDNR = ProductionCommonFunction.SafeConvertToDouble(dtTemp.DefaultView.ToTable().Rows[0]["WarpDNR"].ToString());
        //            double WeftpDNR = ProductionCommonFunction.SafeConvertToDouble(dtTemp.DefaultView.ToTable().Rows[0]["WeftDNR"].ToString());
        //            double size = ProductionCommonFunction.SafeConvertToDouble(dtTemp.DefaultView.ToTable().Rows[0]["size"].ToString());
        //            double wrapMesh = ProductionCommonFunction.SafeConvertToDouble(dtTemp.DefaultView.ToTable().Rows[0]["warpMesh"].ToString());
        //            double weftMesh = ProductionCommonFunction.SafeConvertToDouble(dtTemp.DefaultView.ToTable().Rows[0]["warpMesh"].ToString());
        //            double RFWidth = ProductionCommonFunction.SafeConvertToDouble(dtTemp.DefaultView.ToTable().Rows[0]["RFWidth"].ToString());
        //            double RFNo = ProductionCommonFunction.SafeConvertToDouble(dtTemp.DefaultView.ToTable().Rows[0]["RFNo"].ToString());
        //            double shrinkage = ProductionCommonFunction.SafeConvertToDouble(dtTemp.DefaultView.ToTable().Rows[0]["shrinkage"].ToString());

        //            int mulFac = 1;
        //            double Mtr = ProductionCommonFunction.SafeConvertToDouble(dataGridView1.Rows[i].Cells[6].Value);
        //            double NtWt = ProductionCommonFunction.SafeConvertToDouble(dataGridView1.Rows[i].Cells[9].Value);

        //            string Typ = dtTemp.DefaultView.ToTable().Rows[0]["Type"].ToString();
        //            string WarpBobin = dtTemp.DefaultView.ToTable().Rows[0]["WarpBobin"].ToString();
        //            string WeftBobin = dtTemp.DefaultView.ToTable().Rows[0]["WeftBobin"].ToString();
        //            if (Typ.ToUpper() == "TUBE")
        //                mulFac = 2;


        //            if (shrinkage == 0)
        //                shrinkage = 1;

        //            DataTable dtTemp2 = dtBobinStock.Copy();
        //            double WarpCon = Math.Round(((WarpDNR / 9000) * (size / 2.54) * (mulFac * Mtr * wrapMesh * shrinkage) / 1000) +
        //        ((WarpDNR / 9000) * ((RFWidth * RFNo)) / (2.54) * (wrapMesh * 1500 * shrinkage) / 1000), 2);

        //            //  ((weftMesh/2.54*Size*2/1*weftDNR/9000*Mtr/1000))*srinkae

        //            double WeftCon = Math.Round(((weftMesh / 2.54 * size * 2 / 1 * WeftpDNR / 9000 * Mtr / 1000)) * shrinkage, 2);

        //            double totCon = WeftCon + WarpCon;
        //            double CorrectionFraction = NtWt / totCon;

        //            double acutalWarpCon = Math.Round(CorrectionFraction * WarpCon, 2);
        //            double acutalWeftCon = Math.Round(CorrectionFraction * WeftCon, 2);

        //            dtTemp2.DefaultView.RowFilter = "itemcode='" + WarpBobin + "'";
        //            dataGridView1.Rows[i].Cells[9].ToolTipText = "Bobin Consumption Summary" + Environment.NewLine + " " + acutalWarpCon + " :" + dtTemp2.DefaultView.ToTable().Rows[0]["ItemName"].ToString() + "" + Environment.NewLine;
        //            dataGridView1.Rows[i].Cells[10].Value = Math.Round(((NtWt / Mtr) * 1000), 2);
        //            dataGridView1.Rows[i].Cells[11].Value = Math.Round(((NtWt / Mtr) * 1000) / (size * mulFac / 100), 2);
        //            dtBobinConsummption.DefaultView.RowFilter = "RowId=" + dataGridView1.CurrentCell.RowIndex + 1 * 101 + "";
        //            if (dtBobinConsummption.DefaultView.ToTable().Rows.Count <= 0)
        //            {
        //                DataRow dr;
        //                dr = dtBobinConsummption.NewRow();
        //                dr["Itemcode"] = WarpBobin;
        //                dr["Itemname"] = dtTemp2.DefaultView.ToTable().Rows[0]["ItemName"].ToString();
        //                dr["StkInHAnd"] = dtTemp2.DefaultView.ToTable().Rows[0]["StkInHAnd"].ToString();
        //                dr["ConsumedQty"] = acutalWarpCon;
        //                dr["RowId"] = i + 1 * 101;
        //                dtBobinConsummption.Rows.Add(dr);
        //            }

        //            dtTemp2.DefaultView.RowFilter = "itemcode='" + WeftBobin + "'";
        //            dataGridView1.Rows[i].Cells[9].ToolTipText += acutalWeftCon + " :" + dtTemp2.DefaultView.ToTable().Rows[0]["ItemName"].ToString();
        //            dtBobinConsummption.DefaultView.RowFilter = "RowId=" + i + 1 * 102 + "";
        //            if (dtBobinConsummption.DefaultView.ToTable().Rows.Count <= 0)
        //            {
        //                DataRow dr;
        //                dr = dtBobinConsummption.NewRow();
        //                dr["Itemcode"] = WeftBobin;
        //                dr["Itemname"] = dtTemp2.DefaultView.ToTable().Rows[0]["ItemName"].ToString();
        //                dr["StkInHAnd"] = dtTemp2.DefaultView.ToTable().Rows[0]["StkInHAnd"].ToString();
        //                dr["ConsumedQty"] = acutalWeftCon;
        //                dr["RowId"] = i + 1 * 102;
        //                dtBobinConsummption.Rows.Add(dr);
        //            }
        //        }

        //        var query = dtBobinConsummption.AsEnumerable()
        //            .GroupBy(row => new { Name = row.Field<string>("ItemCode"), Code = row.Field<string>("ItemName"), Stk = row.Field<double>("StkInHAnd") });

        //        var table2 = dtBobinConsummption.Clone(); // empty table with same schema
        //        table2.Columns.Remove("RowId");
        //        foreach (var x in query)
        //        {
        //            string name = x.Key.Name;
        //            string code = x.Key.Code;
        //            double stkinHand = x.Key.Stk;
        //            double count = x.Sum(r => r.Field<double>("ConsumedQty"));
        //            table2.Rows.Add(x.Key.Name, x.Key.Code, x.Key.Stk, count);
        //        }

        //        new FrmShowGrid(table2, "Bobin Consumption Summary").ShowDialog();


        //        for (int j = 0; j < table2.Rows.Count; j++)
        //        {
        //            if (ProductionCommonFunction.SafeConvertToDouble(table2.Rows[j]["ConsumedQty"].ToString()) > ProductionCommonFunction.SafeConvertToDouble(table2.Rows[j]["StkInHAnd"].ToString()))
        //                throw new ERPErrorException("You can't able save entry. " + Environment.NewLine + " Consumption qty can not be more than stock in hand for:" + Environment.NewLine + table2.Rows[j]["Itemname"].ToString());
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }
        //}


    }
}
