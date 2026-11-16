using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Collections;
namespace ERP
{
    public partial class FrmWareHouseTransferNew : Form
    {
        int GroupSrNo = 0;
        public System.Windows.Forms.StatusBarPanel pfrm_sbp1 = null;
        private filter.filterData filterData;
        string isApprovalReq = "false";
        bool isformopen = false;
        string MISItemName = "";
        #region Main Functions
        /// <summary>
        /// This form is used to transfer material form one godwon to other. 
        /// Material which is not in item master that materials will be transfered by this form
        /// </summary>

        public FrmWareHouseTransferNew()
        {
            InitializeComponent();

            Form_Load();

            // dataGridView1.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(229, 218, 251);
        }
        //Overloaded method for Open this from Production Report. by Rikin on 11-Des-2014   
        public FrmWareHouseTransferNew(DateTime Fromdate, DateTime Todate, string FromWarehouse, string MISItemName)
        {

            InitializeComponent();
            this.MISItemName = MISItemName;
            Form_Load();
            if (MISItemName.ToLower().Contains("fabric"))
                cmbStatus.Text = "Loom";

            btnTransfer.Enabled = false;
            dateTimePicker2.Value = Fromdate.Date;
            dateTimePicker1.Value = Todate.Date;
            comboWareHouseFrom.Text = FromWarehouse;
            this.MISItemName = MISItemName;
            bindgrid("");
            this.MISItemName = "";
            btnTransfer.Enabled = false;
            // dataGridView1.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(229, 218, 251);
        }
        //Overloaded method for Open this from Production Report. by Rikin on 11-Des-2014   
        public FrmWareHouseTransferNew(string Qry, string FromWarehouse)
        {
            string _Qry = Qry;
            InitializeComponent();
            Form_Load();
            bindgrid(_Qry);
            comboWareHouseFrom.Text = FromWarehouse;

            if (comboWareHouseFrom.SelectedIndex > -1)
                btnTransfer.Enabled = true;
            else
                btnTransfer.Enabled = false;
            // dataGridView1.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(229, 218, 251);
        }

        private void Form_Load()
        {
            cmbStatus.SelectedIndex = 0;

            dateTimePicker1.CustomFormat = Tools.ToolsCommonFunction.ERPDateSettings();
            dateTimePicker1.Format = DateTimePickerFormat.Custom;
            dateTimePicker1.MaxDate = DateTime.Now;

            dateTimePicker2.Value = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);//.Today.AddDays(-1).ToString();
            //dateTimePicker2.Format = DateTimePickerFormat.Custom;
            //dateTimePicker2.CustomFormat = "MM/dd/yyyy";
            dateTimePicker2.CustomFormat = Tools.ToolsCommonFunction.ERPDateSettings();
            dateTimePicker2.Format = DateTimePickerFormat.Custom;
            dateTimePicker2.MaxDate = DateTime.Now;

            dateTransfer.CustomFormat = Tools.ToolsCommonFunction.ERPDateSettings();
            dateTransfer.Format = DateTimePickerFormat.Custom;
            dateTransfer.MaxDate = DateTime.Now;
            dateTransfer.MinDate = ProductionCommonFunction.ERPBackDateSettings();

            //dateTransfer.Format = DateTimePickerFormat.Custom;
            //dateTransfer.CustomFormat = "MM/dd/yyyy";

            filterData = new filter.filterData(dataGridView1);
            //dateTimePicker1.Value = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

            comboCompanyName.DataSource = ProductionCommonFunction.GetCompanyList();
            comboCompanyName.DisplayMember = ProductionCommonVariables.FieldName.CompanyName;
            comboCompanyName.ValueMember = ProductionCommonVariables.FieldName.CompanyId;


            comboCompanyName.Text = frmDefaultVale.CompanyName;


            if (FrmMain.IsProductionadmin)
            {

                comboWareHouseFrom.DataSource = ProductionCommonFunction.GetWareHouseList(comboCompanyName.Text);
                comboWareHouseFrom.DisplayMember = ProductionCommonVariables.FieldName.WareHouse;
                comboWareHouseFrom.ValueMember = ProductionCommonVariables.FieldName.SrNo;
            }
            else
            {
                comboWareHouseFrom.DataSource = ProductionCommonFunction.GetWareHouse_userWise();
                comboWareHouseFrom.DisplayMember = ProductionCommonVariables.FieldName.WareHouse;
                comboWareHouseFrom.ValueMember = ProductionCommonVariables.FieldName.SrNo;

            }

            comboWareHouseFrom.SelectedIndex = -1;
            comboRollForm.SelectedIndex = 0;
            isformopen = true;
        }


        #endregion

        #region

        void bindgrid(string Qry)
        {
            string query = "";

            if (Database.OpenConnection(Utility.MaterialConnectionString))
            {
                //DateTime dtfromdate;
                //DateTime dtTOdate;
                //if (dateTimePicker2.Text == string.Empty)
                //{
                //    dtfromdate = DateTime.Now;
                //}
                //else
                //{
                //    dtfromdate = Convert.ToDateTime(dateTimePicker2.Value);
                //}

                //if (dateTimePicker1.Text == string.Empty)
                //{
                //    dtTOdate = DateTime.Now;
                //}
                //else
                //{
                //    dtTOdate = Convert.ToDateTime(dateTimePicker1.Value);
                //}
                dataGridView1.DataSource = null;
                dataGridView1.Enabled = true;
                btnTransfer.Enabled = true;
                DataTable dt = new DataTable();
                if (Qry.Length > 0)
                {
                    query = Qry;
                    Database.myadapter = Database.GetAdapterCommand(query);
                    Database.myadapter.Fill(dt);
                    btnTransfer.Enabled = false;
                }
                else
                    if (comboWareHouseFrom.Text == "Cutting Godown")
                    {
                        if (cmbStatus.Text == "Loom")
                        {
                            query = string.Format(@"select RollNO,PartyRollNo as Marking ,LamRollNo ,Quality, ReqGSm,Type as RollType, '' as Color, '' as Metre, Netwt as GrossWt,0 as TareWT,NetWt,
		 Sysdate, sysdate as TransferDate,'FICB' Sector, ' ' Mesh,ReqGSm as  ActualGPM,ReqGSm as  ActualGSM,Size,status,Category ,'' Remarks,
		 Partyname,MarketingOrderno,'No' SelectRoll ,'' ERemarks,fgitemcode  from vw_Prod_CuttingRoll_rmd where status='{0}' and CompanyName='{1}' and Warehouse='{2}' and (Sysdate between '{3}' and '{4}')  and   RollNo not in (select  vItemCode from prod_rmd_inout where   vFromGodown='Cutting Godown' and vToGodown='Stock Adjustment Entry' and Vtype='{0}' group by vItemCode)",
       cmbStatus.Text, comboCompanyName.Text, comboWareHouseFrom.Text, dateTimePicker2.Value.ToString("yyyy-MM-dd"), dateTimePicker1.Value.ToString("yyyy-MM-dd"));
                        }
                        else
                        {
                            query = string.Format(@"select RollNO,PartyRollNo as Marking ,LamRollNo ,Quality, ReqGSm,Type as  RollType, '' as Color, '' as Metre, Netwt as GrossWt,0 as TareWT,NetWt,
		 Sysdate, sysdate as TransferDate,'FICB' Sector, ' ' Mesh,ReqGSm as  ActualGPM,ReqGSm as  ActualGSM,Size,status,Category ,'' Remarks,
		 Partyname,MarketingOrderno,'No' SelectRoll ,'' ERemarks,fgitemcode from vw_Prod_CuttingRoll_NeedleLoom_rmd where status='{0}' and CompanyName='{1}' and Warehouse='{2}'   and   RollNo not in (select  vItemCode from prod_rmd_inout where   vFromGodown='Cutting Godown' and vToGodown='Stock Adjustment Entry' and Vtype='{0}'  group by vItemCode)",
                                cmbStatus.Text, comboCompanyName.Text, comboWareHouseFrom.Text);
                        }
                        query += " order by Sysdate desc   ";
                        Database.myadapter = Database.GetAdapterCommand(query);

                        Database.myadapter.Fill(dt);
                        if (chkNM.Checked)
                        {
                            dt.DefaultView.RowFilter = "MarketingOrderno='N.M.'";
                            dt = dt.DefaultView.ToTable().Copy();
                        }
                    }
                    else
                    {
                        if (cmbStatus.Text != "Loom")
                        {
                            Database.myreader = Database.GetExecuteReaderCommand("exec  SP_Prod_BeforeRMD '"
                                + comboCompanyName.Text + "','" + comboWareHouseFrom.Text + "','" + cmbStatus.Text
                                + "','Approved','" + dateTimePicker2.Value + "','" + dateTimePicker1.Value + "'");
                            dt.Load(Database.myreader);

                            //dt.DefaultView.ToTable(true, "");
                            //string[] columns = new string[] { "RollNo", "Marking", "LamRollNo", "Quality" };
                            //query = string.Format(@"select isnull(CuttingRollNo,RollNo )  as RollNo, Marking,LamRollNo,Quality as Quality , ReqGSM, RollType, Color, Metre, GrossWt, TareWT, fPendingQty as NetWt, Sysdate,TransferDate, Sector, Mesh, ActualGPM, ActualGSM, Size, [status], Category, Remarks,PartyName,MarketingOrderno,'No' as SelectRoll from vw_Prod_BeforeRMD where  (companyname= '{0}')  and (Sysdate between '{1}' and '{2}')  and ((case when vToGodown is NULL then Location  else vToGodown  end )='{3}') and (status not in ('Loom'))  and (CuttingRollNo is null  or CuttingRollNo like '%CUT') and (vStatus='Approved') AND round(fPendingQty,2) > 0 "
                            //    , comboCompanyName.Text, dtfromdate.ToString("yyyy/MM/dd"), dtTOdate.ToString("yyyy/MM/dd"), comboWareHouseFrom.Text, cmbStatus.Text);
                        }
                        else
                        {
                            if (this.MISItemName.Length > 0)
                            {

                                query = string.Format(@"select   vToGodown as Location, isnull(CuttingRollNo,RollNo )  as Rollno, Marking,LamRollNo,Quality as Quality , ReqGSM, RollType, Color, Metre, GrossWt, TareWT, fPendingQty as NetWt, Sysdate,TransferDate, Sector, Mesh, ActualGPM, ActualGSM, Size, [status], Category, Remarks,PartyName,MarketingOrderno,'No' SelectRoll,newstandno,newrackno,FGItemCode,FGItemname  from vw_Prod_BeforeRMD where  (companyname= '{0}')  and (Sysdate between '{1}' and '{2}')  and   status='{4}' and ( CuttingRollNo like '%CUT' or CuttingRollNo is null ) and (vStatus='Approved') AND round(fPendingQty,2) > 0"
                                    , comboCompanyName.Text, dateTimePicker2.Value.ToString("yyyy-MM-dd 00:00:00"), dateTimePicker1.Value.ToString("yyyy-MM-dd 23:59:59"), comboWareHouseFrom.Text, cmbStatus.Text);
                                query = "select Location,RollNo,Marking,LamRollNo,FGItemname AS ItemName,Quality,ReqGSM,RollType,Color,Metre,GrossWt,TareWT,NetWt,Sysdate,TransferDate,Sector,Mesh,ActualGPM,ActualGSM,Size,status,Category,Remarks,PartyName,MarketingOrderno,SelectRoll,newstandno as StandNo,newrackno as RackNo ,'' ERemarks,FGItemCode from (" + query + ") as t where replace(RollNo,'CUT','') not in (select  vItemCode from prod_rmd_inout where   vToGodown in ('Despatch Godown','Stock Adjustment Entry','Cutting Godown','FIBC Bag Godown','Small Bag Godown') and  vtype='" + cmbStatus.Text + "'   group by vItemCode)";

                                if (this.MISItemName.Length > 0)
                                {
                                    query = query + " and FGItemCode in ( select distinct itemcode from item where item.CompanyName='" + comboCompanyName.Text +
                                        "' and item.SubGroupName='" + this.MISItemName + "')";
                                }
                            }
                            else
                            {
                                //isnull(CuttingRollNo,RollNo ) as
                                query = string.Format(@"select   isnull(CuttingRollNo,RollNo )  as Rollno, Marking,LamRollNo,Quality as Quality , ReqGSM, RollType, Color, Metre, GrossWt, TareWT, fPendingQty as NetWt, Sysdate,TransferDate, Sector, Mesh, ActualGPM, ActualGSM, Size, [status], Category, Remarks,PartyName,MarketingOrderno,'No' SelectRoll,newstandno,newrackno,FGItemCode,FGItemname  from vw_Prod_BeforeRMD where  (companyname= '{0}')  and (Sysdate between '{1}' and '{2}')  and ((case when vToGodown is NULL then Location  else vToGodown  end )='{3}') and status='{4}' and ( CuttingRollNo like '%CUT' or CuttingRollNo is null ) and (vStatus='Approved') AND round(fPendingQty,2) > 0"
                                    , comboCompanyName.Text, dateTimePicker2.Value.ToString("yyyy-MM-dd 00:00:00"), dateTimePicker1.Value.ToString("yyyy-MM-dd 23:59:59"), comboWareHouseFrom.Text, cmbStatus.Text);
                                query = "select RollNo,Marking,LamRollNo,FGItemname AS ItemName,Quality,ReqGSM,RollType,Color,Metre,GrossWt,TareWT,NetWt,Sysdate,TransferDate,Sector,Mesh,ActualGPM,ActualGSM,Size,status,Category,Remarks,PartyName,MarketingOrderno,SelectRoll,newstandno as StandNo,newrackno as RackNo ,'' ERemarks,FGItemCode from (" + query + ") as t where replace(RollNo,'CUT','') not in (select  vItemCode from prod_rmd_inout where   vToGodown in ('Despatch Godown','Stock Adjustment Entry','Cutting Godown','FIBC Bag Godown','Small Bag Godown') and  vtype='" + cmbStatus.Text + "'   group by vItemCode)";


                            }
                            Database.myadapter = Database.GetAdapterCommand(query);

                            Database.myadapter.Fill(dt);
                        }
                        if (chkNM.Checked)
                        {
                            dt.DefaultView.RowFilter = "MarketingOrderno='N.M.'";
                            dt = dt.DefaultView.ToTable().Copy();
                        }

                    }
                // else

                dataGridView1.DataSource = null;
                dataGridView1.DataSource = dt;
                foreach (DataGridViewColumn col in dataGridView1.Columns)
                {
                    if (col.Name == "Quality")
                    {
                        col.Width = 175;
                    }
                    try
                    {
                        double result;
                        string v = Convert.ToString(dataGridView1[col.Index, 0].Value);
                        if (double.TryParse(v, out result))
                        {
                            dataGridView1.Columns[col.Index].DefaultCellStyle.Alignment = DataGridViewContentAlignment.BottomRight;
                        }
                    }
                    catch
                    {

                    }

                    //if (col.Index == 21 || col.Index == 25 || col.Index == 26)
                    //{
                    //    col.ReadOnly = false;
                    //}
                    //else
                    //{
                    //    col.ReadOnly = true;
                    //}

                    //if (col.Index == 27)
                    //{
                    //    col.ReadOnly = false;
                    //}
                }
            }
            if (comboWareHouseTo.Text == "Despatch Godown")
            {
                ddlPartyName.SelectedIndex = -1;
                ddlPurpose.SelectedIndex = -1;
                ddlType.SelectedIndex = -1;
                txtInvNo.Text = "";
                txtRemarks.Text = "";
            }
        }
        #endregion

        private void comboWareHouseFrom_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                // bindgrid();
                comboWareHouseTo.DataSource = ProductionCommonFunction.GetWareHouse_frmWareHouse(comboWareHouseFrom.Text, comboCompanyName.Text);
                comboWareHouseTo.DisplayMember = ProductionCommonVariables.FieldName.WareHouse;
                comboWareHouseTo.ValueMember = ProductionCommonVariables.FieldName.SrNo;

                button4.Enabled = Convert.ToString(comboWareHouseFrom.Text).ToUpper() == "Roll Material Godown".ToString().ToUpper();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }

        }
        //Below code is moved into ProductionCommonVariables class
        //static DataTable CreateTable(string param)
        //{
        //    DataTable dt = new DataTable();
        //    switch (param)
        //    {
        //        //case "RMD":

        //        //    dt.Columns.Add("vcompanyname", typeof(string));
        //        //    dt.Columns.Add("iqty", typeof(float));
        //        //    dt.Columns.Add("vfromgodown", typeof(string));
        //        //    dt.Columns.Add("vtogodown", typeof(string));
        //        //    dt.Columns.Add("vitemcode", typeof(string));

        //        //    dt.Columns.Add("vitemname", typeof(string));
        //        //    dt.Columns.Add("dsysdate", typeof(DateTime));
        //        //    dt.Columns.Add("iactualgsm", typeof(string));
        //        //    dt.Columns.Add("imeter", typeof(float));

        //        //    dt.Columns.Add("vusername", typeof(string));
        //        //    dt.Columns.Add("vType", typeof(string));
        //        //    dt.Columns.Add("vCategory", typeof(string));

        //        //    dt.Columns.Add("vRollType", typeof(string));
        //        //    dt.Columns.Add("vStatus", typeof(string));
        //        //    dt.Columns.Add("fPendingQty", typeof(float));

        //        //    return dt;
        //        //case "DESP":
        //        //    dt.Columns.Add("RollNO", typeof(string));
        //        //    dt.Columns.Add("Quality", typeof(string));
        //        //    dt.Columns.Add("Mesh", typeof(string));
        //        //    dt.Columns.Add("reqGsm", typeof(string));
        //        //    dt.Columns.Add("Sector", typeof(string));
        //        //    dt.Columns.Add("Metre", typeof(float));
        //        //    dt.Columns.Add("GrossWT", typeof(string));
        //        //    dt.Columns.Add("TareWt", typeof(float));
        //        //    dt.Columns.Add("NetWt", typeof(float));
        //        //    dt.Columns.Add("ActualGPM", typeof(float));
        //        //    dt.Columns.Add("ActualGSM", typeof(float));
        //        //    dt.Columns.Add("Remarks", typeof(string));
        //        //    dt.Columns.Add("Companyname ", typeof(string));
        //        //    dt.Columns.Add("sysdate", typeof(DateTime));
        //        //    dt.Columns.Add("Partyname", typeof(string));
        //        //    dt.Columns.Add("Invno", typeof(string));
        //        //    dt.Columns.Add("Purpose", typeof(string));

        //        //    dt.Columns.Add("UserName", typeof(string));
        //        //    dt.Columns.Add("type", typeof(string));
        //        //    dt.Columns.Add("Category ", typeof(string));
        //        //    dt.Columns.Add("MainRemarks", typeof(string));
        //        //    dt.Columns.Add("vPartyRollNo ", typeof(string));

        //        //    dt.Columns.Add("RollParyName", typeof(string));
        //        //    dt.Columns.Add("PONO", typeof(string));

        //        //    return dt;
        //        //case "LOOM":

        //        //    dt.Columns.Add("RollNO", typeof(string));
        //        //    dt.Columns.Add("Quality", typeof(string));
        //        //    dt.Columns.Add("Mesh", typeof(string));
        //        //    dt.Columns.Add("reqGsm", typeof(string));
        //        //    dt.Columns.Add("Sector", typeof(string));
        //        //    dt.Columns.Add("Metre", typeof(float));
        //        //    dt.Columns.Add("GrossWT", typeof(string));
        //        //    dt.Columns.Add("TareWt", typeof(float));
        //        //    dt.Columns.Add("NetWt", typeof(float));
        //        //    dt.Columns.Add("ActualGPM", typeof(float));
        //        //    dt.Columns.Add("ActualGSM", typeof(float));
        //        //    dt.Columns.Add("Remarks", typeof(string));
        //        //    dt.Columns.Add("Companyname ", typeof(string));
        //        //    dt.Columns.Add("sysdate", typeof(DateTime));

        //        //    dt.Columns.Add("Deptt", typeof(string));
        //        //    dt.Columns.Add("UserName", typeof(string));
        //        //    dt.Columns.Add("Type", typeof(string));

        //        //    dt.Columns.Add("Category", typeof(string));
        //        //    dt.Columns.Add("isVisibleCut", typeof(string));
        //        //    return dt;

        //        case "NLOOM":
        //            dt.Columns.Add("RollNo", typeof(string));
        //            dt.Columns.Add("Size", typeof(string));
        //            dt.Columns.Add("reqGsm", typeof(string));
        //            dt.Columns.Add("Color", typeof(string));
        //            dt.Columns.Add("Marking", typeof(string));
        //            dt.Columns.Add("Quality", typeof(string));
        //            dt.Columns.Add("NetWt", typeof(float));
        //            dt.Columns.Add("Remarks", typeof(string));
        //            dt.Columns.Add("Companyname ", typeof(string));
        //            dt.Columns.Add("sysdate", typeof(DateTime));
        //            dt.Columns.Add("Deptt", typeof(string));
        //            dt.Columns.Add("isVisibleCut", typeof(string));
        //            return dt;
        //        default:
        //            break;

        //    }
        //    return dt;
        //}
        private void btnTransfer_Click(object sender, EventArgs e)
        {
            try
            {
                if (!Validations())
                    return;

                bool isentry = false;
                bool isalready = false;
                string PackingListNo = "";
                string status = "Pending";
                string qry = "";
                if (comboWareHouseTo.Text == "" || comboWareHouseFrom.Text == "")
                    throw new ERPErrorException("Please select warehouse name");

                if (MessageBox.Show("Do you want to Transfer Roll from '" + comboWareHouseFrom.Text + "' to '" + comboWareHouseTo.Text + "' ?"
                 , "Roll Entry", MessageBoxButtons.YesNo, MessageBoxIcon.Information) != System.Windows.Forms.DialogResult.Yes)
                    return;

                Database.OpenConnection(Utility.MaterialConnectionString);

                try
                {
                    //qry = string.Format(@"select ApprovalReq from warehousemaster where companyname='" + comboCompanyName.Text + "' and warehousename='" + comboWareHouseTo.Text + "'");
                    //Database.myadapter = Database.GetAdapterCommand(qry);
                    int ApprovalReq = ProductionCommonFunction.IsWareHouseApprovalRequired(comboCompanyName.Text, comboWareHouseTo.Text);
                    if (ApprovalReq == 0)
                    {
                        status = "Approved";
                    }
                    if (comboWareHouseTo.Text == "Despatch Godown")
                    {
                        PackingListNo = ProductionCommonFunction.GeneratePackingListInvno(comboCompanyName.Text);
                    }
                    DataTable myRMD_inout = ProductionCommonVariables.FieldName.Create_RMDTable();  //CreateTable("RMD");
                    DataTable myDespatch = ProductionCommonVariables.FieldName.Create_DespatchTable();
                    DataTable myLOOMIssue = ProductionCommonVariables.FieldName.Create_LoomIssueTable();
                    DataTable myNeedeLOOMIssue = ProductionCommonVariables.FieldName.Create_NLoomIssueTable();

                    string tempRollNo = ""; // this string is used for further use to select or check rollno                                        
                    for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                    {
                        if (dataGridView1.Rows[i].Cells[0].FormattedValue.ToString() == "True")
                        {
                            tempRollNo += "," + dataGridView1["RollNo", i].Value;
                            myRMD_inout.Rows.Add(comboCompanyName.Text, dataGridView1["netwt", i].Value, comboWareHouseFrom.Text, comboWareHouseTo.Text,
                                  dataGridView1["RollNo", i].Value, dataGridView1["Quality", i].Value, dateTransfer.Value,
                                  Utility.SafeConvertToDouble(dataGridView1["ReqGSM", i].Value), (dataGridView1["Metre", i].Value == "") ? 0 : dataGridView1["Metre", i].Value, FrmMain.UserName,
                                  dataGridView1["status", i].Value, dataGridView1["Category", i].Value,
                                  dataGridView1["RollType", i].Value, status, dataGridView1["netwt", i].Value,
                                  dataGridView1["fgitemcode", i].Value);

                            if (comboWareHouseTo.Text == "Despatch Godown")
                            {
                                myDespatch.Rows.Add(dataGridView1["RollNo", i].FormattedValue, dataGridView1["Quality", i].FormattedValue,
                                    dataGridView1["Mesh", i].FormattedValue, dataGridView1["ReqGSM", i].FormattedValue,
                                    dataGridView1["Sector", i].FormattedValue, Utility.SafeConvertToDouble(dataGridView1["Metre", i].FormattedValue),
                                    dataGridView1["GrossWt", i].FormattedValue, dataGridView1["TareWT", i].FormattedValue,
                                    dataGridView1["NetWt", i].FormattedValue, dataGridView1["ActualGPM", i].FormattedValue,
                                    dataGridView1["ActualGSM", i].FormattedValue, dataGridView1["Remarks", i].FormattedValue,
                                    comboCompanyName.Text, dateTransfer.Value, ddlPartyName.Text, txtInvNo.Text, ddlPurpose.Text, FrmMain.UserName,
                                    ddlType.Text, dataGridView1["Category", i].FormattedValue, txtRemarks.Text, dataGridView1["Marking", i].FormattedValue,
                                    dataGridView1["PartyName", i].FormattedValue, dataGridView1["MarketingOrderno", i].FormattedValue, PackingListNo,
                                    dataGridView1["ERemarks", i].FormattedValue, Convert.ToString(dataGridView1["fgitemcode", i].Value));
                                //Added A Roll Party Name and PONO on 06-Feb-2015 by Rikin
                            }

                            if (comboWareHouseTo.Text == "Cutting Godown")
                            {
                                Database.myreader = Database.GetExecuteReaderCommand("select rollno from misrOLLissuE where rollno = '"
                           + dataGridView1["RollNo", i].Value + "' and CompanyName='" + comboCompanyName.Text + "'" +
                                " Union  select rollno from MISNEEDLEROllIssue where rollno = '" + dataGridView1["RollNo", i].Value + "' and CompanyName='" + comboCompanyName.Text + "'");
                                if (Database.myreader.Read())
                                    isalready = true;
                                Database.myreader.Close();

                                if (!isalready)
                                {
                                    if (dataGridView1["status", i].Value.ToString() == "Loom")
                                    {
                                        myLOOMIssue.Rows.Add(dataGridView1["RollNo", i].Value, dataGridView1["Quality", i].Value,
                                            dataGridView1["Mesh", i].Value, Utility.SafeConvertToDouble(dataGridView1["ReqGSM", i].Value),
                                            dataGridView1["Sector", i].Value, dataGridView1["Metre", i].Value,
                                            dataGridView1["GrossWt", i].Value, dataGridView1["TareWT", i].Value,
                                            dataGridView1["NetWt", i].Value, dataGridView1["ActualGPM", i].Value,
                                            dataGridView1["ActualGSM", i].Value, dataGridView1["Remarks", i].Value, comboCompanyName.Text,
                                       dateTransfer.Value, comboWareHouseTo.Text, FrmMain.UserName,
                                       dataGridView1["RollType", i].Value, dataGridView1["Category", i].Value, "Yes");

                                    }
                                    else if (dataGridView1["status", i].Value.ToString() == "NeedleLoom")
                                    {
                                        myNeedeLOOMIssue.Rows.Add(dataGridView1["RollNo", i].Value, dataGridView1["Size", i].Value,
                                           dataGridView1["ActualGPM", i].Value, dataGridView1["Color", i].Value, dataGridView1["Marking", i].Value,
                                           dataGridView1["Quality", i].Value, dataGridView1["NetWt", i].Value, dataGridView1["Remarks", i].Value,
                                           comboCompanyName.Text, dateTransfer.Value, comboWareHouseTo.Text, "Yes");

                                    }
                                }
                            }
                        }
                    }
                    tempRollNo += ",";
                    try
                    {
                        if (Database.OpenConnection(Utility.MaterialConnectionString))
                        {
                            Database.BeginTransaction();
                            if (comboWareHouseTo.Text == "Despatch Godown")
                            {
                                PackingListNo = ProductionCommonFunction.GeneratePackingListInvno(comboCompanyName.Text); //by Raj 08.12.2021 due to generated same Number if not in begin tran re-chek for PackingListno
                            }
                            Database.mycommand.Parameters.Clear();
                            Database.mycommand.CommandText = "SP_Prod_RMD_transaction";
                            Database.mycommand.CommandType = CommandType.StoredProcedure;
                            Database.mycommand.Transaction = Database.mytransaction;
                            Database.mycommand.Connection = Database.myconn;

                            //Pass table Valued parameter to Store Procedure
                            Database.myparameter = Database.mycommand.Parameters.AddWithValue("@TempRMDInOut", myRMD_inout);
                            Database.myparameter = Database.mycommand.Parameters.AddWithValue("@TempMISRollforDespatch", myDespatch);
                            Database.myparameter = Database.mycommand.Parameters.AddWithValue("@TempMISRollIssue", myLOOMIssue);
                            Database.myparameter = Database.mycommand.Parameters.AddWithValue("@TempMISRollNloom", myNeedeLOOMIssue);
                            Database.myparameter.SqlDbType = SqlDbType.Structured;
                            Database.mycommand.ExecuteNonQuery();

                            if (comboWareHouseFrom.Text.ToLower() == "cutting godown")
                            {
                                if (cmbStatus.Text.ToLower() == "loom")
                                {
                                    if (Database.GetExecuteNonQueryCommand("Update misrOLLissuE set isVisibleCut='No' where  CHARINDEX(','+rollno +',','," + tempRollNo + ",',0)>0 and CompanyName='" + comboCompanyName.Text + "'"))
                                    {
                                        isentry = true;
                                    }
                                }
                                else if (cmbStatus.Text.ToLower() == "needleloom")
                                {
                                    if (Database.GetExecuteNonQueryCommand("Update MISNEEDLEROllIssue set isVisibleCut='No' where CHARINDEX(','+rollno +',','," + tempRollNo + ",',0)>0  and CompanyName='" + comboCompanyName.Text + "'"))
                                    {
                                        isentry = true;
                                    }
                                }

                            }
                            else if (comboWareHouseTo.Text.ToLower() == "cutting godown")
                            {
                                if (cmbStatus.Text.ToLower() == "loom")
                                {
                                    Database.GetExecuteNonQueryCommand("Update misrOLLissuE set isVisibleCut='Yes' where CHARINDEX(','+rollno +',','," + tempRollNo + ",',0)>0  and CompanyName='" + comboCompanyName.Text + "'");
                                }
                                else if (cmbStatus.Text.ToLower() == "needleloom")
                                {
                                    Database.GetExecuteNonQueryCommand("Update MISNEEDLEROllIssue set isVisibleCut='Yes' where CHARINDEX(','+rollno +',','," + tempRollNo + ",',0)>0  and CompanyName='" + comboCompanyName.Text + "'");
                                }
                            }



                            if (Database.OpenConnection(Utility.MaterialConnectionString))
                            {
                                if (comboWareHouseTo.Text == "Despatch Godown")
                                {
                                    for (int i = 0; i <= myDespatch.Rows.Count - 1; i++)
                                    {
                                        string ITEMCODE = Convert.ToString(myDespatch.Rows[i]["FGITEMCODE"]);
                                        string ITEMNAME = Convert.ToString(Database.GetExecuteNonQueryCommand_retrun("select distinct top 1  itemname from itemcodes with(nolock) where itemcode='" + ITEMCODE + "'"));
                                        string rollno = Convert.ToString(myDespatch.Rows[i]["RollNO"]);
                                        double Netwt = Utility.SafeConvertToDouble(myDespatch.Rows[i]["NetWt"]);
                                        ProductionCommonFunction.InsertStockJournal(0, dateTimePicker1.Value, comboCompanyName.Text, "Consumption", ITEMNAME, Netwt, 0, 0, "KGS", frmDefaultVale.Year, 0, 0, ITEMCODE, rollno, "Despatch");
                                        Database.GetExecuteNonQueryCommand("Update Warehouse set stkinHand=case when Round(stkinHand - " + Netwt + ",2)<0 then 0 else Round(stkinHand - " + Netwt + ",2) end   where CompanyName='" + comboCompanyName.Text + "' and WarehouseName='" + comboWareHouseFrom.Text + "' and ItemCode='" + Convert.ToString(dataGridView1["fgitemcode", i].Value) + "'");

                                    }
                                }
                                else
                                {
                                    string ItemWIPCode = "";
                                    string ItemWIPName = "";
                                    string groupName = "";
                                    #region new WIP Codes
                                    if (comboWareHouseTo.Text == "FIBC Bag Godown" || comboWareHouseTo.Text == "Small Bag Godown")
                                    {
                                        if (Database.OpenConnection(Utility.MaterialConnectionString))
                                        {
                                            if (comboWareHouseTo.Text == "FIBC Bag Godown" && status == "Approved")
                                            {
                                                ItemWIPName = "FIBC WIP";
                                                Database.myreader = Database.GetExecuteReaderCommand("select top 1 isnull(ItemCode,'') as itemcode,GroupName from item with(nolock) where todate is null and CompanyName='" + comboCompanyName.Text + "' and itemname='FIBC WIP' order by fromdate desc ");
                                                if (Database.myreader.Read())
                                                {
                                                    ItemWIPCode = Database.myreader[0].ToString();
                                                    groupName = Database.myreader[1].ToString();
                                                }
                                                Database.myreader.Close();
                                            }
                                            if (comboWareHouseTo.Text == "Small Bag Godown" && status == "Approved")
                                            {
                                                ItemWIPName = "Small Bags WIP";
                                                Database.myreader = Database.GetExecuteReaderCommand("select top 1 ItemCode from item with(nolock) where  todate is null and CompanyName='" + comboCompanyName.Text + "' and itemname='Small Bags WIP' order by fromdate desc ");
                                                if (Database.myreader.Read())
                                                {
                                                    ItemWIPCode = Database.myreader[0].ToString();
                                                    groupName = Database.myreader[1].ToString();
                                                }
                                                Database.myreader.Close();
                                            }
                                        }
                                    }
                                    #endregion
                                    #region Change for Run faster - remove all loop and only selected rows will be check
                                    string commaSeparatedString = String.Join(",", myRMD_inout.AsEnumerable().Select(x => x.Field<string>("vItemcode").ToString()).ToArray());
                                    double TotNetWt = Utility.SafeConvertToDouble(myRMD_inout.Compute("Sum(iqty)", ""));

                                    if (status == "Approved")
                                    {
                                        if (comboWareHouseTo.Text == "FIBC Bag Godown" || comboWareHouseTo.Text == "Small Bag Godown")
                                        {
                                            Database.GetExecuteNonQueryCommand("Update prod_Rmd_inout set vStatus='Approved',fPendingQty=0 where vToGodown='" + comboWareHouseTo.Text
                                                        + "' and vFromGodown='" + comboWareHouseFrom.Text + "' and vCompanyName='" + comboCompanyName.Text
                                                        + "' and vitemcode in ('" + commaSeparatedString.Replace(",", "','") + "')"
                                                        );
                                            if (!Database.GetExecuteNonQueryCommand("update warehouse set StkInHand = isnull(stkinhand,0)+" + Math.Round(TotNetWt, 2) + ""
                                                + " where CompanyName='" + comboCompanyName.Text + "' and WareHouseName= '" + comboWareHouseTo.Text + "' and ItemCode = '"
                                                + ItemWIPCode + "'")
                                                )
                                            {
                                                Database.GetExecuteNonQueryCommand("insert into warehouse  (ItemCode,ItemName,GroupName,Deptt,Unit,StkInHand ,CompanyName,LoginName,WareHouseName) values('"
                                                    + ItemWIPCode + "','" + ItemWIPName + "','" + groupName + "','RawMaterial','kgs'," + Math.Round(TotNetWt, 2)
                                                    + ",'" + comboCompanyName.Text + "','" + FrmMain.UserName + "','" + comboWareHouseTo.Text + "')");
                                            }
                                        }
                                    }
                                    DataTable dtFinal = Database.GetDataTable("Select FGITEMCODE,sum(iqty) Qty from Prod_RMD_InOut INTOT with(nolock) " +
                                        " Inner Join (select Max(isrno) MaxIsrno from Prod_RMD_InOut with(nolock) where vType='" + cmbStatus.Text + "' group by  vItemCode,vType,vCompanyName,vCategory ) MaxInTo on MaxInTo.MaxIsrno=INTOT.iSrNo " +
                                        " WHERE vType='" + cmbStatus.Text + "'  and VITEMCODE IN ('" +
                                    commaSeparatedString.Replace(",", "','") + "') and vFromGodown='" + comboWareHouseFrom.Text + "' AND CAST(dModifyDate AS DATE)='" +
                                    DateTime.Now.Date.ToString("yyyy-MM-dd") + "' group by FGITEMCODE");

                                    TotNetWt = 0;
                                    for (int i = 0; i <= dtFinal.Rows.Count - 1; i++)
                                    {

                                        string FGItemcode = Convert.ToString(dtFinal.Rows[i]["FGITEMCODE"]);
                                        TotNetWt = Utility.SafeConvertToDouble(dtFinal.Rows[i]["qty"]);

                                        int Rec = Convert.ToInt32(Database.GetExecuteNonQueryCommand_retrun("Select count(*) from Warehouse where itemcode='" +
                                            FGItemcode + "' and companyname='" + comboCompanyName.Text + "' and  " + " warehousename='" + comboWareHouseFrom.Text + "'"));
                                        if (Rec == 0)
                                        {
                                            Database.GetExecuteNonQueryCommand("Insert into warehouse (ItemCode,ItemName,GroupName,Deptt,Minlevel,Maxlevel,ReOrder,Unit,StkInHand,TypeVendor,NameVendor,EmailVendor,QtyOrder,MnthlyConsum,Rate,CompanyName,LoginName,Dummy1,OpeningStock,SubGroupName,WareHouseName) " +
                                                "select ItemCode,ItemName,GroupName,Deptt,Minlevel,Maxlevel,ReOrder,Unit,0 StkInHand,TypeVendor,NameVendor,EmailVendor,QtyOrder,MnthlyConsum,Rate,CompanyName,LoginName,Dummy1,OpeningStock,SubGroupName,'" + comboWareHouseFrom.Text + "' from item where itemcode='" +
                                                FGItemcode + "' and companyname='" + comboCompanyName.Text + "'");
                                        }
                                        Database.GetExecuteNonQueryCommand("Update Warehouse set stkinHand=case when Round(stkinHand - " + TotNetWt
                                            + ",2)<0 then 0 else Round(stkinHand - " + TotNetWt + ",2) end where CompanyName='"
                                            + comboCompanyName.Text + "' and WarehouseName='" + comboWareHouseFrom.Text + "' and ItemCode='"
                                            + FGItemcode + "'");

                                        if (status == "Approved")
                                        {
                                            if (comboWareHouseTo.Text == "FIBC Bag Godown" || comboWareHouseTo.Text == "Small Bag Godown")
                                            { }
                                            else
                                            {
                                                Database.GetExecuteNonQueryCommand("Update Warehouse set stkinHand=Round(stkinHand + " + TotNetWt + ",2) where CompanyName='"
                                                + comboCompanyName.Text + "' and WarehouseName='" + comboWareHouseTo.Text + "' and ItemCode='" + FGItemcode + "'");
                                            }
                                        }

                                    }
                                    #endregion
                                    #region Comment due to less effective
                                    //for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                                    //{
                                    //    if (dataGridView1.Rows[i].Cells[0].FormattedValue.ToString() == "True")
                                    //    {

                                    //        string ITEMNAME = Convert.ToString(Database.GetExecuteNonQueryCommand_retrun("select distinct top 1  itemname from itemcodes where itemcode='" +
                                    //            Convert.ToString(dataGridView1["fgitemcode", i].Value) + "'"));

                                    //        double TotNetWt = ProductionCommonFunction.SafeConvertToDouble(dataGridView1["NetWt", i].FormattedValue);

                                    //        Database.GetExecuteNonQueryCommand("Update Warehouse set stkinHand=case when Round(stkinHand - " + TotNetWt + ",2)<0 then 0 else Round(stkinHand - " + TotNetWt + ",2) end where CompanyName='"
                                    //            + comboCompanyName.Text + "' and WarehouseName='" + comboWareHouseFrom.Text + "' and ItemCode='"
                                    //           + Convert.ToString(dataGridView1["fgitemcode", i].Value) + "'");

                                    //        if (status == "Approved")
                                    //        {


                                    //            if (comboWareHouseTo.Text == "FIBC Bag Godown" || comboWareHouseTo.Text == "Small Bag Godown")
                                    //            {
                                    //                Database.GetExecuteNonQueryCommand("Update prod_Rmd_inout set vStatus='Approved',fPendingQty=0 where vToGodown='" + comboWareHouseTo.Text
                                    //                    + "' and vFromGodown='" + comboWareHouseFrom.Text + "' and vCompanyName='" + comboCompanyName.Text
                                    //                    + "' and vitemcode='" + dataGridView1["RollNo", i].FormattedValue + "'"
                                    //                    );

                                    //                if (!Database.GetExecuteNonQueryCommand("update warehouse set StkInHand = isnull(stkinhand,0)+" + Math.Round(TotNetWt, 2) + ""
                                    //                    + " where CompanyName='" + comboCompanyName.Text + "' and WareHouseName= '" + comboWareHouseTo.Text + "' and ItemCode = '" + ItemWIPCode + "'"))
                                    //                    Database.GetExecuteNonQueryCommand("insert into warehouse  (ItemCode,ItemName,GroupName,Deptt,Unit,StkInHand ,CompanyName,LoginName,WareHouseName) values('"
                                    //                        + ItemWIPCode + "','" + ItemWIPName + "','" + groupName + "','RawMaterial','KG'," + Math.Round(TotNetWt, 2)
                                    //                        + ",'" + comboCompanyName.Text + "','" + FrmMain.UserName + "','" + comboWareHouseTo.Text + "')");

                                    //            }
                                    //            else
                                    //            {
                                    //                Database.GetExecuteNonQueryCommand("Update Warehouse set stkinHand=Round(stkinHand + " + TotNetWt + ",2) where CompanyName='"
                                    //                + comboCompanyName.Text + "' and WarehouseName='" + comboWareHouseTo.Text + "' and ItemCode='"
                                    //               + Convert.ToString(dataGridView1["fgitemcode", i].Value) + "'");
                                    //            }
                                    //        }
                                    //    }
                                    //}
                                    #endregion
                                }
                            }
                            Database.CommitTransaction();

                            #region Remove Rolls from datagridview
                            for (int i = 0; i <= myRMD_inout.Rows.Count - 1; i++)
                            {
                                foreach (DataGridViewRow item in this.dataGridView1.Rows)
                                {
                                    if (item.Cells["RollNo"].Value == Convert.ToString(myRMD_inout.Rows[i]["vitemcode"]))
                                    {
                                        dataGridView1.Rows.RemoveAt(item.Index);
                                        break;
                                    }
                                }
                            }

                            dataGridView1.Refresh();
                            #endregion
                        }
                    }
                    catch (Exception ex)
                    {

                        Database.RollBackTransaction();
                        throw ex;
                    }
                    Database.Closeconnection();
                    Utility.UserInformation("Save ", "ROLL Entry " + dateTransfer.Value.ToString("yyyy-MM-dd") + "is Saved for Deptt " + comboWareHouseTo.Text);
                    MessageBox.Show("Data Saved Succesfully");


                }
                catch (Exception ex)
                {
                    throw ex;
                }
                //bindgrid("");
            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
            }



        }

        public bool Validations()
        {
            try
            {
                if (!Utility.CheckDateFrize_Production(dateTransfer.Value.ToString("yyyy-MM-dd"), comboCompanyName.Text))
                {
                    MessageBox.Show("Entry on this Date is Freezed. So, You Can't do Entry.");
                    return false;
                }
                if (comboWareHouseTo.Text == "Despatch Godown")
                {
                    if (ddlPartyName.SelectedIndex == -1)
                    {
                        MessageBox.Show("Please Select Party Name.");
                        return false;
                    }
                    if (ddlPartyName.Text.Length == 0 || ddlPartyName.Text == "")
                    {
                        MessageBox.Show("Please Select Party Name.");
                        return false;
                    }
                    if (ddlPurpose.SelectedIndex == -1)
                    {
                        MessageBox.Show("Please Select Purpose Name.");
                        return false;
                    }
                    if (ddlType.SelectedIndex == -1)
                    {
                        MessageBox.Show("Please Select Type.");
                        return false;
                    }
                    if (txtInvNo.Text.Trim().Length == 0)
                    {
                        MessageBox.Show("Please Write Invoice Number.");
                        return false;
                    }
                }

                bool isViewRequired = true;

                if (MessageBox.Show("Do you want to view selected Rolls to Transfer from '" + comboWareHouseFrom.Text + "' to '" + comboWareHouseTo.Text + "' ?"
                 , "Roll View", MessageBoxButtons.YesNo, MessageBoxIcon.Information) != System.Windows.Forms.DialogResult.Yes)
                { isViewRequired = false; }

                DataTable dtRollTranfer = new DataTable();
                DataRow dr;
                bool ischek = false;
                double totalMtr = 0;
                double totalkgs = 0;
                int rollsCount = 0;
                if (isViewRequired)
                {
                    dtRollTranfer.Columns.Add("RollNo");
                    dtRollTranfer.Columns.Add("Marking");
                    dtRollTranfer.Columns.Add("ItemName");
                    dtRollTranfer.Columns.Add("Quality");
                    dtRollTranfer.Columns.Add("ReqGSM");
                    dtRollTranfer.Columns.Add("RollType");
                    dtRollTranfer.Columns.Add("Metre");
                    dtRollTranfer.Columns.Add("NetWt");
                }
                for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                {
                    if (Convert.ToString(dataGridView1.Rows[i].Cells[0].FormattedValue).Trim() == "True" && dataGridView1.Rows[i].Cells[0].Visible == true)
                    {
                        ischek = true;
                        if (isViewRequired)
                        {
                            dr = dtRollTranfer.NewRow();

                            dr["RollNo"] = dataGridView1.Rows[i].Cells["RollNo"].FormattedValue.ToString();
                            dr["Marking"] = dataGridView1.Rows[i].Cells["Marking"].FormattedValue.ToString();
                            dr["Quality"] = dataGridView1.Rows[i].Cells["Quality"].FormattedValue.ToString();
                            dr["ItemName"] = dataGridView1.Rows[i].Cells["ItemName"].FormattedValue.ToString();
                            dr["ReqGSM"] = dataGridView1.Rows[i].Cells["ReqGSM"].FormattedValue.ToString();
                            dr["RollType"] = dataGridView1.Rows[i].Cells["RollType"].FormattedValue.ToString();
                            dr["Metre"] = dataGridView1.Rows[i].Cells["Metre"].FormattedValue.ToString();
                            dr["NetWt"] = dataGridView1.Rows[i].Cells["NetWt"].FormattedValue.ToString();
                            dtRollTranfer.Rows.Add(dr);
                            rollsCount++;
                            totalMtr += Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells["Metre"].FormattedValue.ToString());
                            totalkgs += Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells["NetWt"].FormattedValue.ToString());
                        }
                        //dtRollTranfer.Rows.Add(dataGridView1.Rows[i].Cells["RollNo"].FormattedValue.ToString());
                        //dtRollTranfer.Rows.Add(dataGridView1.Rows[i].Cells["Marking"].FormattedValue.ToString());
                        //dtRollTranfer.Rows.Add(dataGridView1.Rows[i].Cells["Quality"].FormattedValue.ToString());
                        //dtRollTranfer.Rows.Add(dataGridView1.Rows[i].Cells["ReqGSM"].FormattedValue.ToString());
                        //dtRollTranfer.Rows.Add(dataGridView1.Rows[i].Cells["RollType"].FormattedValue.ToString());
                        //dtRollTranfer.Rows.Add(dataGridView1.Rows[i].Cells["Metre"].FormattedValue.ToString());
                        //dtRollTranfer.Rows.Add(dataGridView1.Rows[i].Cells["NetWt"].FormattedValue.ToString());
                    }
                }
                if (ischek == false)
                {
                    MessageBox.Show("Please Check any one Checkbox to Proceed.");
                    return false;
                }
                if (isViewRequired && dtRollTranfer.Rows.Count > 0)
                {
                    dr = dtRollTranfer.NewRow();
                    dr["RollNo"] = "Total";

                    dr["RollType"] = "Total Rolls=" + rollsCount;
                    dr["Metre"] = totalMtr;
                    dr["NetWt"] = totalkgs;

                    dtRollTranfer.Rows.Add(dr);
                    new FrmShowGrid(dtRollTranfer).ShowDialog();
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }



        private void button1_Click(object sender, EventArgs e)
        {
            bindgrid("");
            for (int i = 0; i < dataGridView1.Columns.Count - 1; i++)
            {
                if (i != 0)
                {
                   // dataGridView1.Columns[i].ReadOnly = false;
                    if ((cmbStatus.Text == "Loom") && (dataGridView1.Columns[i].Name == "Remarks" ||
                        dataGridView1.Columns[i].Name == "StandNo" ||
                        dataGridView1.Columns[i].Name == "RackNo" ||
                        dataGridView1.Columns[i].Name == "ERemarks"))
                    {
                        dataGridView1.Columns[i].ReadOnly = false;
                    }
                    else if ((cmbStatus.Text == "NeedleLoom") && (dataGridView1.Columns[i].Name == "Remarks" ||
                        dataGridView1.Columns[i].Name == "StandNo" ||
                        dataGridView1.Columns[i].Name == "RackNo"))
                    {
                        dataGridView1.Columns[i].ReadOnly = false;
                    }
                    
                    else
                        dataGridView1.Columns[i].ReadOnly = true;
                }
            }
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                DataGridViewCheckBoxCell chk = (DataGridViewCheckBoxCell)row.Cells[0];
                chk.Value = false;

            }

            //dataGridView1.Columns["StandNo"].ReadOnly = false;
        }

        private void comboWareHouseTo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboWareHouseTo.Text == "Despatch Godown")
            {
                ddlPartyName.Visible = true;
                ddlPurpose.Visible = true;
                ddlType.Visible = true;
                txtRemarks.Visible = true;
                txtInvNo.Visible = true;
                label7.Visible = true;
                label8.Visible = true;
                label9.Visible = true;
                label10.Visible = true;
                label11.Visible = true;


                DataTable dtInternal = ProductionCommonFunction.GetDespatchCompanyList();
                dtInternal.Rows.Add("Export Godown");
                ddlPartyName.DataSource = dtInternal;
                ddlPartyName.ValueMember = "firmname";
                ddlPartyName.DisplayMember = "firmname";
            }
            else
            {
                ddlPartyName.Visible = false;
                ddlPurpose.Visible = false;
                ddlType.Visible = false;
                txtRemarks.Visible = false;
                txtInvNo.Visible = false;
                label7.Visible = false;
                label8.Visible = false;
                label9.Visible = false;
                label10.Visible = false;
                label11.Visible = false;
            }
        }

        private void lblInfo_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Enter your rollno/party roll number and click on search button to serach Roll's loacation.");
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (textRollNo.Text == "")
            {
                MessageBox.Show("Please enter proper roll number to search.");
                return;
            }

            //string qry = "select TransferDate,ProductionDate,RollNo,PartyRollNo,Quality,category,status,vFromGodown as FromGodown ,vToGodown as ToGodown  from Vw_prod_LoomTransaferDatewise where Companyname='" + comboCompanyName.Text + "' and category='" + comboRollForm.Text + "' and status='" + cmbStatus.Text
            //    + "' and (RollNo like '%" + textRollNo.Text + "%' or PartyRollNo like '%" + textRollNo.Text + "%') order by TransferDate desc,isrno desc,ProductionDate,RollNo,PartyRollNo";
            if (Database.OpenConnection(Utility.MaterialConnectionString))
            {
                string qry = "select TransferDate,ProductionDate,RollNo,PartyRollNo,Quality,REQGSM,category,status,vFromGodown as FromGodown ,vToGodown as ToGodown,Quality,netwt,fPendingQty as PendingQty,LamRollNo  from Vw_prod_LoomTransaferDatewise where Companyname='" + comboCompanyName.Text + "' and category='" + comboRollForm.Text + "' and status='" + cmbStatus.Text
                    + "' and (RollNo like '%" + textRollNo.Text + "%' or PartyRollNo like '%" + textRollNo.Text + "%' or [LamRollNo] like '%" + textRollNo.Text + "%') order by TransferDate desc,isrno desc,ProductionDate,RollNo,PartyRollNo";

                new FrmShowGrid(qry, "Transfer History").ShowDialog();
            }
        }

        private void cmbStatus_SelectedIndexChanged(object sender, EventArgs e)
        {
            ///bindgrid("");
            if (cmbStatus.Text == "NeedleLoom")
            {
                ddlType.Text = "Webbing";
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            try
            {
                DataTable dt = new DataTable();

                dt.Columns.Add("RollNo", typeof(string));
                dt.Columns.Add("Quality", typeof(string));
                dt.Columns.Add("RollType", typeof(string));
                dt.Columns.Add("Category", typeof(string));
                dt.Columns.Add("OldPartyName", typeof(string));
                dt.Columns.Add("OldPOOrdNo", typeof(string));
                for (int i = 0; i < dataGridView1.Rows.Count; i++)
                {
                    if (dataGridView1.Rows[i].Cells[0].FormattedValue.ToString() == "True")
                    {
                        dt.Rows.Add(dataGridView1.Rows[i].Cells["RollNo"].FormattedValue,
                            dataGridView1.Rows[i].Cells["Quality"].FormattedValue,
                            dataGridView1.Rows[i].Cells["status"].FormattedValue,
                            dataGridView1.Rows[i].Cells["Category"].FormattedValue,
                            dataGridView1.Rows[i].Cells["PartyName"].FormattedValue,
                            dataGridView1.Rows[i].Cells["MarketingOrderno"].FormattedValue
                            );
                    }
                }

                if (dt.Rows.Count > 0)
                {
                    using (SwapPartyNameEntry f = new SwapPartyNameEntry(comboCompanyName.Text, dt, cmbStatus.Text, comboWareHouseFrom.Text))
                    {
                        if (f.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                        {
                            bindgrid("");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
            }
        }

        //private void dataGridView1_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        //{
        //    try
        //    {
        //        int rowIndex = dataGridView1.CurrentRow.Index;
        //        string str = dataGridView1.Rows[rowIndex].Cells[0].FormattedValue.ToString();
        //        if ((bool)dataGridView1.Rows[rowIndex].Cells[0].FormattedValue == true)
        //            dataGridView1.Rows[rowIndex].Cells["SelectRoll"].Value = "Yes";
        //        if ((bool)dataGridView1.Rows[rowIndex].Cells[0].FormattedValue == false)
        //            dataGridView1.Rows[rowIndex].Cells["SelectRoll"].Value = "No";
        //    }
        //    catch (Exception ex)
        //    {
        //        ErrorMessageBox.Show(ex);
        //    }
        //}



        private void editRollToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (dataGridView1.CurrentCell == null
                    || dataGridView1.CurrentCell.RowIndex == -1)
                    return;

                string strRollno = "";

                for (int i = 0; i < dataGridView1.Rows.Count; i++)
                {
                    if (dataGridView1.Rows[i].Cells[0].FormattedValue.ToString() == "True")
                        strRollno += "," + dataGridView1["Rollno", i].FormattedValue.ToString();
                }
                if (strRollno.Length == 0)
                    strRollno = "," + dataGridView1["Rollno", dataGridView1.CurrentCell.RowIndex].FormattedValue.ToString() + ",";
                else
                    strRollno += ",";
                ///FrmLinerRollEntry frm = new FrmLinerRollEntry(strRollno);

                //frm.Show();
                if (cmbStatus.Text.ToLower() == "loom" && comboWareHouseFrom.Text.ToUpper() == "ROLL MATERIAL GODOWN")
                {
                    if (dataGridView1[4, dataGridView1.CurrentCell.RowIndex].FormattedValue.ToString().Substring(0, 2).ToString() == "LL")
                        TabClass.OpenForm(new FrmLinerRollEntry(strRollno));
                    else
                        throw new ERPErrorException("This functionality is working for LLDPE ROLLS");

                }
                else
                    throw new ERPErrorException("This functionality is working for LLDPE ROLLS at ROLL MATERIAL GODOWN");



                //       MessageBox.Show(strRollno);
                //if (cmbStatus.Text.ToLower() == "loom")
                //{
                //    Database.GetExecuteNonQueryCommand("Update misrOLLissuE set isVisibleCut='Yes' where CHARINDEX(','+rollno +',','," + tempRollNo + ",',0)>0  and CompanyName='" + comboCompanyName.Text + "'");

                //}
                //else if (cmbStatus.Text.ToLower() == "needleloom")
                //{
                //    Database.GetExecuteNonQueryCommand("Update MISNEEDLEROllIssue set isVisibleCut='Yes' where CHARINDEX(','+rollno +',','," + tempRollNo + ",',0)>0  and CompanyName='" + comboCompanyName.Text + "'");

                //}
            }
            catch (Exception ex)
            {
                ERP.ErrorMessageBox.Show(ex);
            }
        }

        private void selectCheckRollsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (dataGridView1.CurrentCell == null
               || dataGridView1.CurrentCell.RowIndex == -1)
                return;
            ProductionCommonFunction.CheckSelectedRows_Datagridview(ref dataGridView1, dataGridView1.CurrentCell.ColumnIndex);

            //for (int i = 0; i < dataGridView1.Rows.Count; i++)
            //{
            //    if (dataGridView1["column1", i].Selected)
            //        dataGridView1["column1", i].Value = true;
            //    else
            //        dataGridView1["column1", i].Value = false;
            //}


        }

        private void TxtMultSel_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.KeyCode == Keys.Enter)
                {
                    string str1 = TxtMultSel.Text.Replace(System.Environment.NewLine, "");
                    str1 = str1.Substring(str1.Length - 1);
                    if (str1 != ",")
                    {
                        TxtMultSel.Text = TxtMultSel.Text + ",";
                    }
                    TxtMultSel.Text = TxtMultSel.Text.Replace(System.Environment.NewLine, "");
                    string[] strrolls = TxtMultSel.Text.Split(',');



                    for (int i = 0; i < strrolls.Length; i++)
                    {
                        string searchValue = strrolls.GetValue(i).ToString();
                        if (searchValue.Length > 0)
                        {
                            foreach (DataGridViewRow row in dataGridView1.Rows)
                            {
                                string tempRoll = Convert.ToString(row.Cells["RollNo"].Value);
                                if (tempRoll.Length > 0)
                                {
                                    if (row.Cells["RollNo"].Value.ToString().Equals(searchValue))
                                    {
                                        row.Selected = true;
                                        dataGridView1.Rows[row.Index].Cells[0].Value = "True";
                                        break;
                                    }
                                }
                                tempRoll = Convert.ToString(row.Cells["Marking"].Value);
                                if (tempRoll.Length > 0)
                                {
                                    if (row.Cells["Marking"].Value.ToString().Equals(searchValue))
                                    {
                                        row.Selected = true;
                                        dataGridView1.Rows[row.Index].Cells[0].Value = "True";
                                        break;
                                    }
                                }
                                tempRoll = Convert.ToString(row.Cells["LamRollNo"].Value);
                                if (tempRoll.Length > 0)
                                {
                                    if (row.Cells["LamRollNo"].Value.ToString().Equals(searchValue))
                                    {
                                        row.Selected = true;
                                        dataGridView1.Rows[row.Index].Cells[0].Value = "True";
                                        break;
                                    }
                                }
                                tempRoll = Convert.ToString(row.Cells["Remarks"].Value);
                                if (tempRoll.Length > 0)
                                {
                                    if (row.Cells["Remarks"].Value.ToString().Equals(searchValue))
                                    {
                                        row.Selected = true;
                                        dataGridView1.Rows[row.Index].Cells[0].Value = "True";
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show("Problem with selection :" + ex.Message);
            }
        }

        private void dataGridView1_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (dataGridView1.Rows.Count - 1 <= 0)
                    return;

                if (dataGridView1.CurrentCell.ColumnIndex == null)
                    return;

                if (isformopen)
                {
                    if (dataGridView1.Columns[dataGridView1.CurrentCell.ColumnIndex].Name.ToLower() == "StandNo".ToLower())
                    {

                        Database.OpenConnection(Utility.MaterialConnectionString);

                        int srno = 0;
                        srno = Convert.ToInt32(Database.GetExecuteNonQueryCommand_retrun("select max(isrno) from Prod_RMD_InOut where vitemcode = '"
                            + dataGridView1["RollNo", dataGridView1.CurrentRow.Index].Value.ToString() + "'"));

                        Database.GetExecuteNonQueryCommand("update Prod_RMD_InOut set newstandno = '" +
                            dataGridView1["StandNo", dataGridView1.CurrentRow.Index].Value.ToString()
                            + "' where vitemcode = '"
                            + dataGridView1["RollNo", dataGridView1.CurrentRow.Index].Value.ToString() + "' and isrno = " + srno);

                        Database.Closeconnection();

                    }

                    if (dataGridView1.Columns[dataGridView1.CurrentCell.ColumnIndex].Name.ToLower() == "RackNo".ToLower())
                    {

                        Database.OpenConnection(Utility.MaterialConnectionString);

                        int srno = 0;
                        srno = Convert.ToInt32(Database.GetExecuteNonQueryCommand_retrun("select max(isrno) from Prod_RMD_InOut where vitemcode = '"
                            + dataGridView1["RollNo", dataGridView1.CurrentRow.Index].Value.ToString() + "'"));

                        Database.GetExecuteNonQueryCommand("update Prod_RMD_InOut set newrackno = '" +
                            dataGridView1["RackNo", dataGridView1.CurrentRow.Index].Value.ToString()
                            + "' where vitemcode = '"
                            + dataGridView1["RollNo", dataGridView1.CurrentRow.Index].Value.ToString() + "' and isrno = " + srno);

                        Database.Closeconnection();

                    }
                    if (dataGridView1.Columns[dataGridView1.CurrentCell.ColumnIndex].Name.ToLower() == "Remarks".ToLower())
                    {
                        if (cmbStatus.Text == "Loom")
                        {
                            Database.OpenConnection(Utility.MaterialConnectionString);

                            string rollno = dataGridView1["RollNo", dataGridView1.CurrentRow.Index].Value.ToString();
                            string remarks = dataGridView1["Remarks", dataGridView1.CurrentRow.Index].Value.ToString();

                            if (rollno.Substring(rollno.Length - 1) == "L")
                            {
                                Database.GetExecuteNonQueryCommand_retrun("update MISLaminationEntry set Remarks='" + remarks + "' where Rollno='" + rollno + "' and companyname='" + comboCompanyName.Text + "'");
                            }
                            else
                            {
                                Database.GetExecuteNonQueryCommand_retrun("update MISLOOMProductionEntryNew set Remarks='" + remarks + "' where Rollno='" + rollno + "' and companyname='" + comboCompanyName.Text + "'");
                                Database.GetExecuteNonQueryCommand("update MISOutsideRollEntry set Remarks='" + remarks + "' where Rollno='" + rollno + "'  and companyname='" + comboCompanyName.Text + "'");
                            }

                            Database.Closeconnection();

                        }

                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            bool blnStart = false;
            try
            {
                if (MessageBox.Show("Do you want to Set Roll To Non-Moving Rolls at " + comboWareHouseFrom.Text + "?"
                                , "Roll Entry", MessageBoxButtons.YesNo, MessageBoxIcon.Information) != System.Windows.Forms.DialogResult.Yes)
                    return;

                if (Database.OpenConnection(Utility.MaterialConnectionString))
                {


                    for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                    {
                        if (dataGridView1.Rows[i].Cells[0].FormattedValue.ToString() == "True")
                        {
                            Database.BeginTransaction();
                            blnStart = true;
                            Database.GetExecuteNonQueryCommand_retrun("UPDATE PROD_RMD_INOUT set isnm='Yes' where Vcompanyname='" +
                                comboCompanyName.Text + "' and vitemcode='" + dataGridView1.Rows[i].Cells["RollNo"].FormattedValue.ToString() +
                                "' and VToGodown='" + comboWareHouseFrom.Text + "' and Vtype='" + cmbStatus.Text + "' and " +
                                " isrno in (select Max(isrno) Msrno from Prod_RMD_InOut with(nolock) where vType='" + cmbStatus.Text + "' and Vcompanyname='" +
                                comboCompanyName.Text + "' and vitemcode='" + dataGridView1.Rows[i].Cells["RollNo"].FormattedValue.ToString() + "'  and VToGodown='" + comboWareHouseFrom.Text + "'  group by  vCompanyName  ,vItemCode ,vType)");

                            Database.GetExecuteNonQueryCommand_retrun("UPDATE RollReceipt set isnm='Yes' where companyname='" +
                                comboCompanyName.Text + "' and RollNo='" + dataGridView1.Rows[i].Cells["RollNo"].FormattedValue.ToString() +
                                "' and Warehouse='" + comboWareHouseFrom.Text + "' and Status='" + cmbStatus.Text + "'");

                            Database.GetExecuteNonQueryCommand_retrun("Insert into RollHistory(CompanyName,RollNo,Type,PartyName,BuyerOrderNo,FromGodown) values('" +
                                comboCompanyName.Text + "','" + dataGridView1.Rows[i].Cells["RollNo"].FormattedValue.ToString() + "','" + cmbStatus.Text + "','" +
                                dataGridView1.Rows[i].Cells["PartyName"].FormattedValue.ToString() + "','" +
                                dataGridView1.Rows[i].Cells["MarketingOrderno"].FormattedValue.ToString() + "','" +
                                comboWareHouseFrom.Text + "')");
                            Database.CommitTransaction();
                            blnStart = false;
                        }
                    }



                }
            }
            catch (Exception ex)
            {
                //if (blnStart)
                //    Database.RollBackTransaction();
                MessageBox.Show("Problem in Set Roll To Non-Moving : " + ex.Message);
            }
        }

        private void dataGridView1_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            try
            {
                if (Convert.ToString(dataGridView1.Rows[e.RowIndex].Cells["MarketingOrderno"].Value) == "N.M.")
                {
                    foreach (DataGridViewCell item in dataGridView1.Rows[e.RowIndex].Cells)
                    {
                        item.Style.BackColor = Color.Orange;
                    }
                }
            }
            catch (Exception ex)
            {
                ex.ToString();
            }
        }



    }
}
