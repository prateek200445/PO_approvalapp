using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using DataGridViewAutoFilter;
using System.Collections;
using System.Data.SqlClient;
using System.Linq;
using System.IO;
namespace ERP
{
    public partial class FrmMISLOOMProductionEntryNew : ERPTabPage
    {
        public System.Windows.Forms.StatusBarPanel pfrm_sbp1 = null;
        public int srno = 0;
        public double RollIndex = 1000;
        public bool isgrid = false;
        public bool isdoubleclick = false;
        bool isclear = false;
        public string productioncode = "";
        string ProdCode = "";

        int _RecordLogId = 0;
        DataTable dtLoomAllocationMaster;
        DataTable dtTemp;
        DataTable dtBobinConsummption;
        DataTable dtBobinStock;

        public FrmMISLOOMProductionEntryNew()
        {
            InitializeComponent();
            Form_Load();
        }

        private void Form_Load()
        {


            #region Common Tools Init
            this.CommonToolEnable = true;
            this.CommonToolVisibleSave = true;
            this.CommonToolVisibleHistory = true;
            this.CommonToolVisibleDelete = true;
            // this.OnSave += Save;
            #endregion

            try
            {
                dateLoomEntry.Format = DateTimePickerFormat.Custom;
                dateLoomEntry.CustomFormat = Tools.ToolsCommonFunction.ERPDateSettings();
                dateLoomEntry.MaxDate = DateTime.Now;
                dateLoomEntry.MinDate = ProductionCommonFunction.ERPBackDateSettings();

                dtpSaveData.Format = DateTimePickerFormat.Custom;
                dtpSaveData.CustomFormat = Tools.ToolsCommonFunction.ERPDateSettings();
                dtpSaveData.MaxDate = DateTime.Now;

                if (Database.OpenConnection(Utility.MaterialConnectionString))
                {
                    comboCompanyName.DataSource = ProductionCommonFunction.GetCompanyList();
                    comboCompanyName.DisplayMember = ProductionCommonVariables.FieldName.CompanyName;
                    comboCompanyName.ValueMember = ProductionCommonVariables.FieldName.IndentCode; //Using Indent Instead of comnyid. To generate Roll No form it
                    comboCompanyName.SelectedIndex = 0;
                    dtTemp = ProductionCommonFunction.GetCompanyList();//this dtTemp is used to get comapnyId to get Allocationmaster by Rikin on 31-01-2015
                }
                comboCompanyName.Text = frmDefaultVale.CompanyName;
                ProdCode = comboCompanyName.SelectedValue.ToString();
                comboShift.SelectedIndex = 0;

                colFGCode.DataSource = Database.GetDataTable("select distinct itemcode,ItemName from item  where GroupName like '%Fabric%' and MISCATEGORY='SF' and (itemname not like '%cutpc%' AND itemname like '%ul%') and todate is null and CompanyName='" + comboCompanyName.Text + "'");
                colFGCode.DisplayMember = "ItemName";
                colFGCode.ValueMember = "ItemCode";


                //To select 100 rows 
                BindGrid("Top 100");

                buttonUpdate.Enabled = false;
                dtTemp.DefaultView.RowFilter = "CompanyName='" + comboCompanyName.Text + "'";

                int ComapnyId = ProductionCommonFunction.SafeConvertToInt(dtTemp.DefaultView.ToTable().Rows[0]["companyID"].ToString());

                dtLoomAllocationMaster = ProductionCommonFunction.GetLoomAllocationMaster(ComapnyId, dateLoomEntry.Value);
                btnprodDate.Visible = (FrmMain.UserName.ToLower() == "erp" ? true : false);                
                MessageBox.Show("Please make sure that you have take receipt of all the bobin which was transfered from bobin godown.");
            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
            }
        }



        private void BtnGetData_Click(object sender, EventArgs e)
        {
            getrollNo();

        }
        private void buttonInitialize_Click(object sender, EventArgs e)
        {
            dateLoomEntry.Format = DateTimePickerFormat.Custom;
            dateLoomEntry.CustomFormat = Tools.ToolsCommonFunction.ERPDateSettings();
            dateLoomEntry.MinDate = ProductionCommonFunction.ERPBackDateSettings();
            dateLoomEntry.MaxDate = DateTime.Now;
            dateLoomEntry.Value = DateTime.Now;

            isclear = true;
            if (dataGridView1.Rows.Count > 0)
                dataGridView1.Rows.Clear();
            isclear = false;
            //buttonDelete.Enabled = false;
            dataGridView1[0, 0].ReadOnly = false;
            buttonSave.Enabled = true;
            buttonUpdate.Enabled = false;
            dateLoomEntry.Enabled = true;
            isdoubleclick = false;
            dataGridView1[0, 0].ReadOnly = false;
            srno = 0;
            BtnGetData.Enabled = true;
            isgrid = false;
            dateLoomEntry.Format = DateTimePickerFormat.Custom;
            dateLoomEntry.CustomFormat = Tools.ToolsCommonFunction.ERPDateSettings();
            dateLoomEntry.MaxDate = DateTime.Now;
            dateLoomEntry.MinDate = ProductionCommonFunction.ERPBackDateSettings();
            // FillMarketingOrderNo();
        }

        void getrollNo()
        {
            string preRollNo = "";// added byy Rikin on 21-11-2014 to increment rollno form previous roll no
            string NewRollNo = "";// added byy Rikin on 21-11-2014 to increment rollno form previous roll no
            double RollIndx = 0;// added byy Rikin on 21-11-2014 to increment rollno form previous roll no
            //  int minus = 1;
            try
            {
                for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                {
                    if (Convert.ToString(dataGridView1.Rows[i].Cells[1].Value) != "")
                        continue;

                    if (i == 0)
                    {
                        dataGridView1.Rows[i].Cells[1].Value = ProductionCommonFunction.RollNo(comboCompanyName.Text, "Loom", ProdCode, dateLoomEntry.Value);
                    }
                    else if (i != 0)
                    {
                        preRollNo = dataGridView1.Rows[i - 1].Cells[1].FormattedValue.ToString();
                        //if (preRollNo == "")
                        //    minus++; continue;

                        RollIndx = Utility.SafeConvertToDouble(preRollNo.Substring(preRollNo.Length - 5));
                        NewRollNo = preRollNo.Substring(0, preRollNo.Length - 5) + "" + string.Format("{0:00000.#}", RollIndx + 1);
                        dataGridView1.Rows[i].Cells[1].Value = NewRollNo;// dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex - 1].Cells[1].Value;
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
            }
        }

        private void ShowHistory()
        {
            if (this._RecordLogId == 0)
                MessageBox.Show("No Record to display history");
            else (new frmHistoryLog(_RecordLogId)).ShowDialog();

        }
        void BobinConsumption()
        {
            try
            {
                dtBobinStock = ProductionCommonFunction.GetBobinForLoom(comboCompanyName.Text, "Loom Godown");

                dtBobinConsummption = dtBobinStock.Clone();
                dtBobinConsummption.Columns.Add("ConsumedQty", Type.GetType("System.Double"));
                dtBobinConsummption.Columns.Add("RowId");
                dtBobinConsummption.Rows.Clear();

                for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                {
                    //Need To remove
                    dtTemp = dtLoomAllocationMaster.Copy();
                    dtTemp.DefaultView.RowFilter = "LoomNo=" + dataGridView1.Rows[i].Cells[0].Value;

                    if (dtTemp.DefaultView.ToTable().Rows.Count == 0)
                        throw new ERPErrorException("Please make entry of Loom Allocation master. ");

                    double WarpDNR = ProductionCommonFunction.SafeConvertToDouble(dtTemp.DefaultView.ToTable().Rows[0]["WarpDNR"].ToString());
                    double WeftpDNR = ProductionCommonFunction.SafeConvertToDouble(dtTemp.DefaultView.ToTable().Rows[0]["WeftDNR"].ToString());
                    double size = ProductionCommonFunction.SafeConvertToDouble(dtTemp.DefaultView.ToTable().Rows[0]["size"].ToString());
                    double wrapMesh = ProductionCommonFunction.SafeConvertToDouble(dtTemp.DefaultView.ToTable().Rows[0]["warpMesh"].ToString());
                    double weftMesh = ProductionCommonFunction.SafeConvertToDouble(dtTemp.DefaultView.ToTable().Rows[0]["WeftMesh"].ToString());
                    double RFWidth = ProductionCommonFunction.SafeConvertToDouble(dtTemp.DefaultView.ToTable().Rows[0]["RFWidth"].ToString());
                    double RFNo = ProductionCommonFunction.SafeConvertToDouble(dtTemp.DefaultView.ToTable().Rows[0]["RFNo"].ToString());
                    double RFMesh = ProductionCommonFunction.SafeConvertToDouble(dtTemp.DefaultView.ToTable().Rows[0]["RFMesh"].ToString());
                    double shrinkage = ProductionCommonFunction.SafeConvertToDouble(dtTemp.DefaultView.ToTable().Rows[0]["shrinkage"].ToString());

                    string warpbobbin = Convert.ToString(dtTemp.DefaultView.ToTable().Rows[0]["WarpBobin"]);
                    string weftbobbin = Convert.ToString(dtTemp.DefaultView.ToTable().Rows[0]["WeftBobin"]);

                    int mulFac = 1;
                    double Mtr = ProductionCommonFunction.SafeConvertToDouble(dataGridView1.Rows[i].Cells[6].Value);
                    double NtWt = ProductionCommonFunction.SafeConvertToDouble(dataGridView1.Rows[i].Cells[10].Value);

                    string Typ = dtTemp.DefaultView.ToTable().Rows[0]["Type"].ToString();
                    string WarpBobin = Convert.ToString(dtTemp.DefaultView.ToTable().Rows[0]["WarpBobin"]);// dtTemp.DefaultView.ToTable().Rows[0]["WarpBobin"].ToString();
                    string WeftBobin = Convert.ToString(dtTemp.DefaultView.ToTable().Rows[0]["WeftBobin"]); //dtTemp.DefaultView.ToTable().Rows[0]["WeftBobin"].ToString();

                    if (dataGridView1.Rows[i].Cells[dtWarpBobbin.Index].Value.ToString().Length == 0)
                    {
                        dataGridView1.Rows[i].Cells[dtWarpBobbin.Index].Value = WarpBobin;
                    }
                    if (dataGridView1.Rows[i].Cells[dtWeftBobbin.Index].Value.ToString().Length == 0)
                    {
                        dataGridView1.Rows[i].Cells[dtWeftBobbin.Index].Value = WeftBobin;
                    }
                    if (Typ.ToUpper() == "TUBE" || Typ.ToUpper() == "GZT")
                        mulFac = 2;


                    if (shrinkage == 0)
                        shrinkage = 1;

                    DataTable dtTemp2 = dtBobinStock.Copy();
                    double WarpCon = Math.Round(((WarpDNR / 9000) * (size / 2.54) * (mulFac * Mtr * wrapMesh * (1 + shrinkage)) / 1000) +
               ((WarpDNR / 9000) * ((RFWidth * RFNo) / (2.54)) * (wrapMesh * Mtr * (1 + shrinkage)) / 1000), 2);

                    //     double WarpCon = Math.Round(((((wrapMesh/ 2.54 * size * mulFac * WarpDNR / 9000 * Mtr / 1000) + 
                    //         (RFWidth*RFNo/2.54*(RFMesh-wrapMesh)*WarpDNR/9000)))*(1+shrinkage)),2);
                    ////         (size / 2.54) * (mulFac * Mtr * wrapMesh * shrinkage) / 1000) +
                    //((WarpDNR / 9000) * ((RFWidth * RFNo)) / (2.54) * (wrapMesh * 1500 * shrinkage) / 1000), 2);

                    //  ((weftMesh/2.54*Size*2/1*weftDNR/9000*Mtr/1000))*srinkae

                    double WeftCon = Math.Round((((weftMesh / 2.54 * size * mulFac / 1 * WeftpDNR / 9000 * Mtr / 1000)) * (1 + shrinkage)), 2);

                    double totCon = WeftCon + WarpCon;
                    double CorrectionFraction = NtWt / totCon;

                    double acutalWarpCon = Math.Round(CorrectionFraction * WarpCon, 2);
                    double acutalWeftCon = Math.Round(CorrectionFraction * WeftCon, 2);

                    dtTemp2.DefaultView.RowFilter = "itemcode='" + WarpBobin + "'";
                    dataGridView1.Rows[i].Cells[10].ToolTipText = "Bobin Consumption Summary" + Environment.NewLine + " " + acutalWarpCon + " :" + dtTemp2.DefaultView.ToTable().Rows[0]["ItemName"].ToString() + "" + Environment.NewLine;
                    dataGridView1.Rows[i].Cells[11].Value = Math.Round(((NtWt / Mtr) * 1000), 2);
                    dataGridView1.Rows[i].Cells[12].Value = Math.Round(((NtWt / Mtr) * 1000) / (size * mulFac / 100), 2);

                    double reqgsm = Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[dtReqGSM.Index].Value);
                    double ActualGSM = Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[12].Value);
                    #region Add condition for ActilGSM and ReqGSM 20.06.2019
                    if ((ActualGSM / reqgsm < 1.05) && (ActualGSM / reqgsm > 0.95))
                    {
                        dataGridView1.Rows[i].DefaultCellStyle.BackColor = Color.GreenYellow;
                        dataGridView1.Rows[i].Cells[colLogic.Index].Value = 0;
                    }
                    else
                    {
                        dataGridView1.Rows[i].DefaultCellStyle.BackColor = Color.OrangeRed;
                        dataGridView1.Rows[i].Cells[colLogic.Index].Value = 1;
                    }
                    #endregion
                    dataGridView1.Rows[i].Cells[DTwarpQty.Index].Value = acutalWarpCon.ToString();
                    dataGridView1.Rows[i].Cells[dtweftQty.Index].Value = acutalWeftCon.ToString();

                    dtBobinConsummption.DefaultView.RowFilter = "RowId=" + i + 1 * 101 + ""; //dataGridView1.CurrentCell.RowIndex
                    if (dtBobinConsummption.DefaultView.ToTable().Rows.Count <= 0)
                    {
                        DataRow dr;
                        dr = dtBobinConsummption.NewRow();
                        dr["Itemcode"] = WarpBobin;
                        dr["Itemname"] = dtTemp2.DefaultView.ToTable().Rows[0]["ItemName"].ToString();
                        dr["StkInHAnd"] = dtTemp2.DefaultView.ToTable().Rows[0]["StkInHAnd"].ToString();
                        dr["ConsumedQty"] = acutalWarpCon;
                        dr["RowId"] = i + 1 * 101;
                        dtBobinConsummption.Rows.Add(dr);
                    }
                    else
                    {
                        MessageBox.Show("itemcode='" + WarpBobin + "'");
                    }

                    dtTemp2.DefaultView.RowFilter = "itemcode='" + WeftBobin + "'";
                    dataGridView1.Rows[i].Cells[10].ToolTipText += acutalWeftCon + " :" + dtTemp2.DefaultView.ToTable().Rows[0]["ItemName"].ToString();
                    dtBobinConsummption.DefaultView.RowFilter = "RowId=" + i + 1 * 102 + "";
                    if (dtBobinConsummption.DefaultView.ToTable().Rows.Count <= 0)
                    {
                        DataRow dr;
                        dr = dtBobinConsummption.NewRow();
                        dr["Itemcode"] = WeftBobin;
                        dr["Itemname"] = dtTemp2.DefaultView.ToTable().Rows[0]["ItemName"].ToString();
                        dr["StkInHAnd"] = dtTemp2.DefaultView.ToTable().Rows[0]["StkInHAnd"].ToString();
                        dr["ConsumedQty"] = acutalWeftCon;
                        dr["RowId"] = i + 1 * 102;
                        dtBobinConsummption.Rows.Add(dr);
                    }
                    else
                    {
                        MessageBox.Show("itemcode='" + WarpBobin + "'");
                    }
                }
                var query = dtBobinConsummption.AsEnumerable()
                    .GroupBy(row => new { Name = row.Field<string>("ItemCode"), Code = row.Field<string>("ItemName"), Stk = row.Field<double>("StkInHAnd") });

                var table2 = dtBobinConsummption.Clone(); // empty table with same schema
                table2.Columns.Remove("RowId");
                foreach (var x in query)
                {
                    string name = x.Key.Name;
                    string code = x.Key.Code;
                    double stkinHand = x.Key.Stk;
                    double count = x.Sum(r => r.Field<double>("ConsumedQty"));
                    table2.Rows.Add(x.Key.Name, x.Key.Code, x.Key.Stk, count);
                }

                new FrmShowGrid(table2, "Bobin Consumption Summary").ShowDialog();


                for (int j = 0; j < table2.Rows.Count; j++)
                {
                    if (ProductionCommonFunction.SafeConvertToDouble(table2.Rows[j]["ConsumedQty"].ToString()) > ProductionCommonFunction.SafeConvertToDouble(table2.Rows[j]["StkInHAnd"].ToString()))
                        throw new ERPErrorException("You can't able save entry. " + Environment.NewLine + " Consumption qty can not be more than stock in hand for:" + Environment.NewLine + table2.Rows[j]["Itemname"].ToString());
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        private void dataGridView1_CurrentCellChanged(object sender, EventArgs e)
        {
            if (dataGridView1.CurrentCell == null
                || dataGridView1.CurrentCell.RowIndex == -1)
                return;
            try
            {
                if (Database.OpenConnection(Utility.MaterialConnectionString))
                {
                    if (!isclear)
                    {
                        if (!isdoubleclick)
                        {
                            if (dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[0].Value != null)
                            {
                                if (dataGridView1.CurrentCell.RowIndex == 0)
                                    dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[1].Value = ProductionCommonFunction.RollNo(comboCompanyName.Text, "Loom", ProdCode, dateLoomEntry.Value);
                                else
                                {// added byy Rikin on 21-11-2014 to increment rollno form previous roll no
                                    getrollNo();
                                }
                            }

                            DateTime dt = dateLoomEntry.Value;
                            string year = Convert.ToString(dt.Year);
                            string yearpart = year.Substring(2, 2);

                            string MergeRoll = yearpart + productioncode;
                            string RollNo = MergeRoll;

                            RollIndex = 0;

                            if (Convert.ToString(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[0].Value).Trim() != ""
                                && dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[2].FormattedValue.ToString().Trim() == ""
                                 && dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[3].FormattedValue.ToString().Trim() == "" &&
                                 dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[4].FormattedValue.ToString().Trim() == "")
                            {

                                dtTemp = dtLoomAllocationMaster.Copy();
                                dtTemp.DefaultView.RowFilter = "LoomNo=" + dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[0].Value;

                                if (dtTemp.DefaultView.ToTable().Rows.Count == 0)
                                    throw new ERPErrorException("Please make entry of Loom Allocation master before saving production entry. ");

                                dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[3].Value = dtTemp.DefaultView.ToTable().Rows[0]["warpMesh"].ToString();
                                dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[4].Value = dtTemp.DefaultView.ToTable().Rows[0]["REQGSM"].ToString();
                                dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[5].Value = dtTemp.DefaultView.ToTable().Rows[0]["Sector"].ToString();
                                dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[2].Value = dtTemp.DefaultView.ToTable().Rows[0]["Material"].ToString() + " " + dtTemp.DefaultView.ToTable().Rows[0]["Size"].ToString() + " " + dtTemp.DefaultView.ToTable().Rows[0]["Type"].ToString().ToUpper() + " " + dtTemp.DefaultView.ToTable().Rows[0]["Color"].ToString()
                                    + (dtTemp.DefaultView.ToTable().Rows[0]["AllocationType"].ToString().Length > 0 ? " " + dtTemp.DefaultView.ToTable().Rows[0]["AllocationType"].ToString() : "");

                                dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells["DtPartyName"].Value = dtTemp.DefaultView.ToTable().Rows[0]["partyname"].ToString();
                                dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells["DtiInvNo"].Value = dtTemp.DefaultView.ToTable().Rows[0]["PONO"].ToString();
                                if (Convert.ToString(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells["dtRemrk"].Value) == "")
                                    dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells["dtRemrk"].Value = dtTemp.DefaultView.ToTable().Rows[0]["remarks"].ToString();

                                dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[dtWarpBobbin.Index].Value = Convert.ToString(dtTemp.DefaultView.ToTable().Rows[0]["WarpBobin"]);
                                dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[dtWeftBobbin.Index].Value = Convert.ToString(dtTemp.DefaultView.ToTable().Rows[0]["WeftBobin"]);

                                dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[colFGCode.Index].Value = Convert.ToString(dtTemp.DefaultView.ToTable().Rows[0]["FGItemCode"]);



                            }
                        }

                        //if (Convert.ToString(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[0].FormattedValue).Trim() != "" &&
                        //    Convert.ToString(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[1].FormattedValue).Trim() != "" &&
                        //    Convert.ToString(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[2].FormattedValue).Trim() != "" &&
                        //    Convert.ToString(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[3].FormattedValue).Trim() != "" &&
                        //    Convert.ToString(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[4].FormattedValue).Trim() != "" &&
                        //    Convert.ToString(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[5].FormattedValue).Trim() != "" &&
                        //    Convert.ToString(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[6].FormattedValue).Trim() != "" &&
                        //    Convert.ToString(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[7].FormattedValue).Trim() != "" &&
                        //    Convert.ToString(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[8].FormattedValue).Trim() != "")
                        //{
                        //    if (Convert.ToDouble(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[6].Value) != 0)
                        //    {
                        //        //if (ProductionCommonFunction.SafeConvertToDouble(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[10].Value) != 0)
                        //    return;

                        //dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[9].Value = ProductionCommonFunction.SafeConvertToDouble(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[7].Value) - ProductionCommonFunction.SafeConvertToDouble(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[8].Value);
                        //Added new Bail Cover wt. Netwt=Grosswt-Tare Wt - CoverWt
                        dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[10].Value = ProductionCommonFunction.SafeConvertToDouble(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[7].Value) - ProductionCommonFunction.SafeConvertToDouble(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[8].Value) - ProductionCommonFunction.SafeConvertToDouble(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[9].Value);
                    }

                }
            }
            //}
            catch (Exception ex)
            {
                //int errorCode = ex.Number;
                //int code = System.Runtime.InteropServices.Marshal.GetExceptionCode();
                ErrorMessageBox.Show(ex);
            }
        }

        //topRows Ex: top 1000 this will selected top 1000 rows from the table by Rikin on 06-Nov-2014
        private void BindGrid(string topRows)
        {
            DateTime currentdatetime = dateLoomEntry.Value;
            DateTime firstdate = new DateTime(currentdatetime.Year, currentdatetime.Month, 1);
            if (Database.OpenConnection_Adapter(Utility.MaterialConnectionString))
            {
                //" + topRows + " 
                Database.myadapter = Database.GetAdapterCommand("select * from MISLOOMProductionEntryNew with(nolock) where  companyname = '"
                    + comboCompanyName.Text + "' and sysdate >= '" + firstdate.ToString("yyyy-MM-dd") + "' and LoomNo != 0 order by transid desc");
                DataSet dataset1 = new DataSet();
                Database.myadapter.Fill(dataset1, "MISLOOMProductionEntryNew");

                BindingSource datasource = new BindingSource(dataset1.Tables[0], null);
                dataGridView2.DataSource = datasource;

                // Add the AutoFilter header cell to each column.
                foreach (DataGridViewColumn col in dataGridView2.Columns)
                {
                    col.HeaderCell = new
                        DataGridViewAutoFilterColumnHeaderCell(col.HeaderCell);
                }
            }

        }
        private bool Vallidation()
        {
            int intApproval = 0;
            if (!Utility.CheckDateFrize_Production(dateLoomEntry.Value.ToString("yyyy-MM-dd"), comboCompanyName.Text))
            {
                MessageBox.Show("Entry on this Date is Freezed. So, You Can't do Entry.");
                return false;
            }
            if (Database.OpenConnection(Utility.MaterialConnectionString))
            {
                double dblCnt = 0;
                string strsql = "";
                #region Add ProfileFlag Table for checking Approval for company if FlagValue = 1 then it will check for Approvals
                intApproval = Convert.ToInt32(Database.GetExecuteNonQueryCommand_retrun("select FlagValue from ProfileFlag where Flagname='IsApproval' and Companyname='" + comboCompanyName.Text + "'"));
                #endregion
                if (intApproval == 1)
                {
                    strsql = string.Format("select count(*)  from misloomproductionentrynew with (nolock) where companyname='{0}' and sysdate<='{1}' and  hodApprove='Pending'    and  loomno != 0  ",
                                          comboCompanyName.Text, dateLoomEntry.Value.AddDays(-1).ToString("yyyy-MM-dd"));
                    dblCnt = Utility.SafeConvertToDouble(Database.GetExecuteNonQueryCommand_retrun(strsql));
                    if (dblCnt > 0)
                    {
                        MessageBox.Show("HOD Approval for Production is Pending.\nPlease ask your HOD to Approve Production entry");
                        return false;
                    }
                    strsql = string.Format("select count(*)  from misloomproductionentrynew with (nolock) where companyname='{0}' and sysdate<='{1}' and  exciseapprove='Pending'    and  loomno != 0  ",
                                           comboCompanyName.Text, dateLoomEntry.Value.AddDays(-1).ToString("yyyy-MM-dd"));
                    dblCnt = Utility.SafeConvertToDouble(Database.GetExecuteNonQueryCommand_retrun(strsql));
                    if (dblCnt > 0)
                    {
                        MessageBox.Show("Excise Approval for Production is Pending.\nPlease ask your Excise Person to Approve Production entry");
                        return false;
                    }
                }
            }
            return true; // by Rikin  on 04-Feb-2015 as No lonnger required
            try
            {
                if (Database.OpenConnection(Utility.MaterialConnectionString))
                {

                    for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                    {

                        #region Check Field blank validation

                        ////// Check Field blank validation
                        Database.OpenConnection(Utility.MaterialConnectionString);
                        for (int j = 2; j < dataGridView1.Columns.Count; j++)
                        {
                            if (dataGridView1.Rows[i].Cells[j].FormattedValue.ToString() == "")
                            {
                                MessageBox.Show("Pls fill data properly in: " + dataGridView1.Columns[j].HeaderText);
                                dataGridView1[j, i].Style.BackColor = Color.Gainsboro;
                                return false;
                            }
                            else
                                dataGridView1[j, i].Style.BackColor = Color.White;
                        }
                        //////////
                        #endregion

                        #region Check Validation for Loom No.
                        /////////// Check Validation for Loom No.

                        Database.myreader = Database.GetExecuteReaderCommand("select loomno from newmisloommaster where loomno = '"
                           + dataGridView1.Rows[i].Cells[0].FormattedValue.ToString() + "' and companyname = '" + comboCompanyName.Text + "'");
                        DataTable dt2 = new DataTable();
                        dt2.Load(Database.myreader);
                        if (dt2.Rows.Count <= 0)
                        {
                            MessageBox.Show("Please check Loom no.Loom no is not available in LOOM Master");
                            return false;
                        }
                        Database.myreader.Close();

                        /////////////
                        #endregion

                        //#region Check validation for marketing order no.
                        ////////// Check validation for marketing order no.

                        //Database.OpenConnection(Utility.DespacthConnectionString);

                        //Database.myreader = Database.GetExecuteReaderCommand("select distinct(ItemNO) from vw_ProductionCompAndMarkInvNo with(nolock) where"
                        //+ " ProductionCompanyName ='" + comboCompanyName.Text + "' and buyername = '"
                        //+ dataGridView1.Rows[i].Cells[13].Value + "' and ItemNO = '" + dataGridView1.Rows[i].Cells[14].FormattedValue.ToString() + "'"
                        //+ " order by ItemNO ");

                        //DataTable dt = new DataTable();
                        //dt.Load(Database.myreader);
                        //if (dt.Rows.Count <= 0)
                        //{
                        //    MessageBox.Show("Please check Marketing Order No: " + Convert.ToString(dataGridView1.Rows[i].Cells[14].FormattedValue) + " for Buyer name " + Convert.ToString(dataGridView1.Rows[i].Cells[13].FormattedValue) + "");
                        //    return false;
                        //}
                        //Database.myreader.Close();
                        ///////////////
                        //#endregion


                    }
                }
                Database.OpenConnection(Utility.MaterialConnectionString);

                if (buttonSave.Enabled == true)
                {
                    #region Check Validation for Consumption
                    /////////Check Validation for Consumption

                    DataTable dtdate = new DataTable();
                    DateTime dt1 = dateLoomEntry.Value;
                    DateTime date = dt1.AddDays(-1);
                    Database.myreader = Database.GetExecuteReaderCommand("select Date from ConsumptionMaster where Date = '" + date.ToString("yyyy-MM-dd") + "' and plantname = 'Loom'"
                        + " and Companyname='" + comboCompanyName.Text + "' ");
                    dtdate.Load(Database.myreader);
                    if (dtdate.Rows.Count <= 0)
                    {
                        MessageBox.Show("You can't do entry, first of all do entry in consumption master for previous date.");
                        Database.myreader.Close();
                        return false;
                    }
                    /////////////
                    #endregion

                    #region Check Validation for All ready done entry in Consumption Master

                    Database.myreader = Database.GetExecuteReaderCommand("select Date from ConsumptionMaster where PlantName='Loom' and Companyname='" + comboCompanyName.Text + "'"
                       + " and Date = '" + dateLoomEntry.Value.ToString("yyyy-MM-dd") + "'");
                    DataTable dtcon = new DataTable();
                    dtcon.Load(Database.myreader);
                    if (dtcon.Rows.Count > 0)
                    {
                        MessageBox.Show("You can't do entry because You have already done entry on this date in Consumption.");
                        Database.myreader.Close();
                        return false;
                    }
                    #endregion

                    #region Check Validation for entry within a  Date
                    ///////////// Check Validation for entry within a  Date
                    Database.myreader = Database.GetExecuteReaderCommand("select sysdate from MISLOOMProductionEntryNew where companyname = '"
                        + comboCompanyName.Text + "'  and sysdate > '" + dateLoomEntry.Value.ToString("yyyy-MM-dd") + "' and LoomNo != 0 ");
                    DataTable dt3 = new DataTable();
                    dt3.Load(Database.myreader);
                    if (dt3.Rows.Count > 0)
                    {
                        MessageBox.Show("You can't do entry on this date.");
                        Database.myreader.Close();
                        return false;
                    }
                    //////////////
                    #endregion
                }
                else
                {
                    #region Check Validation for All ready done entry in Consumption Master
                    Database.myreader = Database.GetExecuteReaderCommand("select * from ConsumptionMaster where PlantName='Loom' and Companyname='" + comboCompanyName.Text + "'"
                       + " and Date = '" + dateLoomEntry.Value.ToString("yyyy-MM-dd") + "'");
                    DataTable dtcon = new DataTable();
                    dtcon.Load(Database.myreader);
                    if (dtcon.Rows.Count > 0)
                    {
                        Database.myreader = Database.GetExecuteReaderCommand("select * from MISLOOMProductionEntryNew where "
                        + " rollno='" + Convert.ToString(dataGridView1.Rows[0].Cells[1].Value) + "' and companyname = '" + comboCompanyName.Text + "'");
                        DataTable dtloom = new DataTable();
                        dtloom.Load(Database.myreader);
                        if (dtloom.Rows.Count > 0)
                        {
                            if (Math.Round(Convert.ToDouble(Utility.ConverttoInt(dataGridView1.Rows[0].Cells[9].Value.ToString())), 2) !=
                                Math.Round(Convert.ToDouble(Utility.ConverttoInt(dtloom.Rows[0]["NetWt"].ToString())), 2))
                            {
                                MessageBox.Show("You can't update Net Weight because You have already done entry in Consumption.");
                                Database.myreader.Close();
                                return false;
                            }
                        }
                        Database.myreader.Close();
                    }
                    #endregion

                    #region Check Validation for Already Approved
                    Database.myreader = Database.GetExecuteReaderCommand("select * from MISloomproductionentrynew where rollno = '" + dataGridView1.Rows[0].Cells[1].FormattedValue.ToString() + "' and companyname = '"
                    + comboCompanyName.Text + "' and exciseapprove = 'Approved'");
                    DataTable dt4 = new DataTable();
                    dt4.Load(Database.myreader);
                    if (dt4.Rows.Count > 0)
                    {
                        MessageBox.Show("Roll is already approved ,You can't update");
                        Database.myreader.Close();
                        return false;
                    }
                    #endregion

                    #region Check Validation For Already Issued
                    Database.myreader = Database.GetExecuteReaderCommand(" select rollno from (select RollNo from MISrollissue with(nolock) where companyname = '" + comboCompanyName.Text + "' "
                                              + "     union all "
                                       + " select URollNo from MISLaminationentry with(nolock) where companyname = '" + comboCompanyName.Text + "' "
                                              + "     union all "
                                         + " select uRollNo from MISreformationentry with(nolock) where companyname = '" + comboCompanyName.Text + "' "
                                              + "     union all "
                                       + " select RollNo from MISrollfordespatch with(nolock) where companyname = '" + comboCompanyName.Text + "' "
                                              + "     ) as temp where rollno = '" + dataGridView1.Rows[0].Cells[1].FormattedValue.ToString() + "'");
                    DataTable dt5 = new DataTable();
                    dt5.Load(Database.myreader);
                    if (dt5.Rows.Count > 0)
                    {
                        MessageBox.Show("You can't update as it's already issue");
                        Database.myreader.Close();
                        return false;
                    }
                    #endregion
                }

                return true;
            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
                return false;
            }
        }
        private void buttonSave_Click(object sender, EventArgs e)
        {
            save(false);
        }

        private void save(bool isupdate)
        {
            bool BLNTRANS = false;

            try
            {
                if (Vallidation())
                {


                    string status = "";
                    DialogResult drs = MessageBox.Show("Do you want to save ?"
                         , "Roll Entry", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                    if (drs.CompareTo(DialogResult.Yes) == 0)
                    {

                        bool isauthority = approvalauth();
                        if (isauthority)
                            status = "Approved";
                        else
                            status = "Pending";

                        bool isentry = false;
                        DateTime dt = dateLoomEntry.Value;
                        string year = Convert.ToString(dt.Year);
                        string yearpart = year.Substring(2, 2);
                        string MergeRoll = yearpart + productioncode;
                        double TotNetWt = 0;
                        double TotalUpNetwt = 0;
                        RollIndex = 0;
                        if (Database.OpenConnection(Utility.MaterialConnectionString))
                        {

                            #region Check For FG Code
                            for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                            {
                                if (Convert.ToString(dataGridView1.Rows[i].Cells[colFGCode.Index].Value).Length == 0)
                                {
                                    MessageBox.Show("Please Select FG Code");
                                    return;
                                }
                                #region Check Item availble in Warehouse or not
                                int Rec = Convert.ToInt32(Database.GetExecuteNonQueryCommand_retrun("Select count(*) from Warehouse where itemcode='" +
                                    dataGridView1.Rows[i].Cells[colFGCode.Index].Value + "' and companyname='" + comboCompanyName.Text + "' and  " +
                                    " warehousename='Loom Godown'"));
                                if (Rec == 0)
                                {
                                    Database.GetExecuteNonQueryCommand("Insert into warehouse (ItemCode,ItemName,GroupName,Deptt,Minlevel,Maxlevel,ReOrder,Unit,StkInHand,TypeVendor,NameVendor,EmailVendor,QtyOrder,MnthlyConsum,Rate,CompanyName,LoginName,Dummy1,OpeningStock,SubGroupName,WareHouseName) " +
                                        "select ItemCode,ItemName,GroupName,Deptt,Minlevel,Maxlevel,ReOrder,Unit,0 StkInHand,TypeVendor,NameVendor,EmailVendor,QtyOrder,MnthlyConsum,Rate,CompanyName,LoginName,Dummy1,OpeningStock,SubGroupName,'Loom Godown' from item where itemcode='" +
                                        dataGridView1.Rows[i].Cells[colFGCode.Index].Value + "' and companyname='" + comboCompanyName.Text + "'");
                                }
                                #endregion
                            }
                            #endregion

                            BobinConsumption();
                            TotNetWt = 0;
                            #region Check with Consumption and production

                            for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                            {
                                TotNetWt += Utility.SafeConvertToDouble(dataGridView1["dtNwt", i].FormattedValue);
                            }
                            double dblConsQty = 0;
                            for (int x = 0; x < dtBobinConsummption.Rows.Count; x++)
                            {
                                dblConsQty += Utility.SafeConvertToDouble(dtBobinConsummption.Rows[x]["ConsumedQty"].ToString());
                            }
                            if (Math.Round(TotNetWt, 3) != Math.Round(dblConsQty, 3))
                            {
                                MessageBox.Show("Consumption is not match with Production");
                                return;
                            }
                            #endregion

                            if (Database.OpenConnection(Utility.MaterialConnectionString))
                            {
                                BLNTRANS = false;
                                Database.BeginTransaction();
                                BLNTRANS = true;
                                string preRollNo = string.Empty;
                                if (_RecordLogId == 0)
                                {
                                    _RecordLogId = ProductionCommonFunction.SaveRecordLog(null, FrmMain.UserName, RecordLogFlag.Regular, string.Format("Loom Production Entry"));
                                    preRollNo = ProductionCommonFunction.RollNo(comboCompanyName.Text, "Loom", ProdCode, dateLoomEntry.Value);
                                }
                                else
                                {
                                    for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                                    {
                                        TotalUpNetwt = Utility.SafeConvertToDouble(Database.GetExecuteNonQueryCommand_retrun("select netwt from MISLOOMProductionEntryNew where Rollno='"
                                            + dataGridView1.Rows[i].Cells[dtRollNo.Index].Value + "'"));
                                        Database.GetExecuteNonQueryCommand("UPDATE WareHouse SET StkInHand = StkInHand-" + TotalUpNetwt + " WHERE CompanyName='" + comboCompanyName.Text
                                            + "' and WareHouseName='Loom Godown' and itemcode='" + dataGridView1.Rows[i].Cells[colFGCode.Index].Value + "'");


                                        Database.GetExecuteNonQueryCommand("Update Warehouse set stkinHand=Round(stkinHand + " + dataGridView1.Rows[i].Cells[DTwarpQty.Index].Value
                                        + ",2) where CompanyName='" + comboCompanyName.Text + "' and WarehouseName='Loom Godown' and ItemCode='"
                                        + dataGridView1.Rows[i].Cells[dtWarpBobbin.Index].Value + "'");


                                        Database.GetExecuteNonQueryCommand("Update Warehouse set stkinHand=Round(stkinHand + " + dataGridView1.Rows[i].Cells[dtweftQty.Index].Value
                                       + ",2) where CompanyName='" + comboCompanyName.Text + "' and WarehouseName='Loom Godown' and ItemCode='"
                                       + dataGridView1.Rows[i].Cells[dtWeftBobbin.Index].Value + "'");

                                    }
                                    //_RecordLogId = ProductionCommonFunction.SaveRecordLog(_RecordLogId, FrmMain.UserName, RecordLogFlag.Regular, string.Format("Loom Production Entry"));                                    
                                    Database.GetExecuteNonQueryCommand("delete from Prod_ConsumptionMaster_Bifercation  where  recordLogId =" + _RecordLogId + "");
                                    Database.GetExecuteNonQueryCommand("delete from MISLOOMProductionEntryNew where  recordLogId =" + _RecordLogId + "");
                                    Database.GetExecuteNonQueryCommand("delete from ConsumptionMaster where  recordLogId =" + _RecordLogId + "");
                                    Database.GetExecuteNonQueryCommand("delete from stockJournalProd where  recordLogId =" + _RecordLogId + " and plantname = 'Loom' and company_name='" + comboCompanyName.Text + "'");// added stock journal                                
                                }
                                TotNetWt = 0;
                                string NewRollNo = "";
                                for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                                {

                                    //if (dataGridView1.Rows[i].Cells[12].FormattedValue.ToString() == "")
                                    //    dataGridView1[12, i].Value = ".";
                                    if (!isupdate)
                                        RollIndex = ProductionCommonFunction.SafeConvertToDouble(preRollNo.Substring(preRollNo.Length - 5));
                                    else
                                    {
                                        preRollNo = dataGridView1.Rows[i].Cells["dtRollNo"].FormattedValue.ToString();
                                        //RollIndex = Utility.SafeConvertToDouble(preRollNo.Split('-')[2]);//Utility.SafeConvertToDouble(dataGridView1["dtRollIndex", i].FormattedValue);
                                        RollIndex = Utility.SafeConvertToDouble(preRollNo.Split('-')[3]);// 16.11.2021 
                                    }
                                    //if (
                                    Database.GetExecuteNonQueryCommand("insert into MISLOOMProductionEntryNew (LoomNo,RollNo,Quality,Mesh,ReqGSM,"
                                    + " Sector,Metre,GrossWt,TareWt,"
                                    + " NetWt,ActualGPM,ActualGSM,Remarks,CompanyName,Sysdate,UserName,Shift ,RollIndex,Partyname, "
                                    + " MarketingOrderno,hodApprove,hodApprovedate,exciseApprove,exciseApprovedate,RecordLogid,CoverWt,WarpQty,WeftQty,WarpBobbin,weftBobbin,FGItemCode) values (" + dataGridView1["DtLoomNo", i].FormattedValue + ",'"
                                    + preRollNo + "','" + dataGridView1["dtQuality", i].FormattedValue + "','"
                                    + dataGridView1["dtMash", i].FormattedValue + "'," + dataGridView1["dtReqGSM", i].FormattedValue + ",'"
                                    + dataGridView1["dtSector", i].FormattedValue + "'," + dataGridView1["dtMtr", i].FormattedValue + ","
                                    + dataGridView1["dtGWT", i].FormattedValue + "," + dataGridView1["dtTwt", i].FormattedValue + ","
                                    + dataGridView1["dtNwt", i].FormattedValue + "," + dataGridView1["dtAGMP", i].FormattedValue + ","
                                    + dataGridView1["dtAGSM", i].FormattedValue + ",'" + dataGridView1["dtRemrk", i].FormattedValue + "','"
                                    + comboCompanyName.Text + "','" + dateLoomEntry.Value.ToString("yyyy-MM-dd") + "','"
                                    + FrmMain.UserName + "','" + comboShift.Text + "'," + RollIndex + ",'" + dataGridView1["DtPartyName", i].FormattedValue + "','"
                                    + dataGridView1[DtiInvNo.Index, i].Value + "','" + status + "', CASE WHEN '" + status + "'='Approved' THEN  CAST(FLOOR(CAST(getdate() AS float)) AS datetime) ELSE Null END,'Pending',Null," + _RecordLogId + ","
                                    + dataGridView1["ColBailCoverWt", i].FormattedValue + ","
                                    + Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[DTwarpQty.Index].Value) + ","
                                    + Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[dtweftQty.Index].Value) + ",'"
                                    + Convert.ToString(dataGridView1.Rows[i].Cells[dtWarpBobbin.Index].Value) + "','"
                                    + Convert.ToString(dataGridView1.Rows[i].Cells[dtWeftBobbin.Index].Value) + "','"
                                    + Convert.ToString(dataGridView1.Rows[i].Cells[colFGCode.Index].Value) + "')");
                                    RollIndex++;

                                    int transid = Convert.ToInt32(Database.GetExecuteNonQueryCommand_retrun("select max(transid) from MISLOOMProductionEntryNew"));
                                    ProductionCommonFunction.InsertStockJournal(transid, dateLoomEntry.Value, comboCompanyName.Text, "Production", Convert.ToString(dataGridView1.Rows[i].Cells[colFGCode.Index].FormattedValue),
                                        Convert.ToDouble(dataGridView1["dtNwt", i].FormattedValue), 0, 0, "KGS", frmDefaultVale.Year, _RecordLogId, transid, Convert.ToString(dataGridView1.Rows[i].Cells[colFGCode.Index].Value), preRollNo, "Loom");

                                    ProductionCommonFunction.InsertStockJournal(transid, dateLoomEntry.Value, comboCompanyName.Text, "Consumption", "Bobbin",
                                        Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[DTwarpQty.Index].Value), 0, 0, "KGS", frmDefaultVale.Year, _RecordLogId, transid, Convert.ToString(dataGridView1.Rows[i].Cells[dtWarpBobbin.Index].Value), "..", "Loom");

                                    ProductionCommonFunction.InsertStockJournal(transid, dateLoomEntry.Value, comboCompanyName.Text, "Consumption", "Bobbin",
                                        Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[dtweftQty.Index].Value), 0, 0, "KGS", frmDefaultVale.Year, _RecordLogId, transid, Convert.ToString(dataGridView1.Rows[i].Cells[dtWeftBobbin.Index].Value), "..", "Loom");


                                    if (!isupdate)
                                    {
                                        NewRollNo = preRollNo.Substring(0, preRollNo.Length - 5) + "" + (string.Format("{0:00000.#}", RollIndex));
                                        preRollNo = NewRollNo;
                                    }

                                    TotNetWt += ProductionCommonFunction.SafeConvertToDouble(dataGridView1["dtNwt", i].FormattedValue);

                                    Database.GetExecuteNonQueryCommand("Update Warehouse set stkinHand=Round(stkinHand + " + ProductionCommonFunction.SafeConvertToDouble(dataGridView1["dtNwt", i].FormattedValue)
                                       + ",2) where CompanyName='" + comboCompanyName.Text + "' and WarehouseName='Loom Godown' and ItemCode='"
                                       + Convert.ToString(dataGridView1.Rows[i].Cells[colFGCode.Index].Value) + "'");

                                    isentry = true;

                                }


                                //Consumption entry by Rikin on 03-Feb-2014
                                double Groupno = (double)Database.ExecuteScalar("select ISNULL(max(fGroupNo) + 1,2000) from Prod_ConsumptionMaster_Bifercation");

                                for (int x = 0; x < dtBobinConsummption.Rows.Count; x++)
                                {
                                    Database.GetExecuteNonQueryCommand("Insert into Prod_ConsumptionMaster_Bifercation (dsysDate, vItemBiferacation, vItemCode, " +
                                        " vItemName, fConsumption, fGroupNo, vPlantName, vCompanyName, vUserName, fRateConsumption, fRateProduction,RecordLogid) " +
                                        " Values ('" + dateLoomEntry.Value.ToString("yyyy-MM-dd") + "','" + dtBobinConsummption.Rows[x]["ItemName"].ToString() + "','" + dtBobinConsummption.Rows[x]["ItemCode"].ToString()
                                        + "','BOBIN' ," + dtBobinConsummption.Rows[x]["ConsumedQty"].ToString() + "," + Groupno + ",'LOOM','" + comboCompanyName.Text + "','" + FrmMain.UserName + "',0,0," + _RecordLogId + " )");


                                    Database.GetExecuteNonQueryCommand("Update Warehouse set stkinHand=Round(stkinHand - " + dtBobinConsummption.Rows[x]["ConsumedQty"].ToString()
                                        + ",2) where CompanyName='" + comboCompanyName.Text + "' and WarehouseName='Loom Godown' and ItemCode='"
                                        + dtBobinConsummption.Rows[x]["ItemCode"].ToString() + "'");

                                    ProductionCommonFunction.WareHouseStoreoutwards(dtBobinConsummption.Rows[x]["ItemCode"].ToString(), dtBobinConsummption.Rows[x]["ItemName"].ToString(),
                                          Convert.ToDouble(dtBobinConsummption.Rows[x]["ConsumedQty"].ToString()), dateLoomEntry.Value, comboCompanyName.Text, "Loom Godown", "");


                                    isentry = true;
                                }
                                Database.GetExecuteNonQueryCommand("Insert into ConsumptionMaster(ItemName,Consumption,GroupNo,Date,PlantName,Companyname,Username,RecordLogid) values " +
                                    " ('Bobin'," + TotNetWt + ",'" + Groupno + "', " + " '" + dateLoomEntry.Value.ToString("yyyy-MM-dd") + "','Loom','" + comboCompanyName.Text + "','" + FrmMain.UserName + "'," + _RecordLogId + ")");


                                if (isentry)
                                {
                                    Database.CommitTransaction();
                                    BLNTRANS = false;
                                    Database.Closeconnection();
                                    Utility.UserInformation("Save ", "Roll Entry " + dateLoomEntry.Value.ToString("yyyy-MM-dd") + " is " + (isupdate ? " Updated " : " Saved ") + "  for SHIFT " + comboShift.Text);
                                    MessageBox.Show("Data Saved Succesfully");
                                    _RecordLogId = 0;

                                    #region Send Email for GSM Miss matching
                                    if (Database.OpenConnection(Utility.MaterialConnectionString))
                                    {
                                        string EmailTo = Convert.ToString(Database.GetExecuteNonQueryCommand_retrun("select EmailTo from WareHouseMaster  where warehousename='Loom Godown' and companyname='" + comboCompanyName.Text + "'"));
                                        string EmailCC = Convert.ToString(Database.GetExecuteNonQueryCommand_retrun("select EmailCC from WareHouseMaster  where warehousename='Loom Godown' and companyname='" + comboCompanyName.Text + "'"));
                                        if (EmailTo.Length > 0 || EmailCC.Length > 0)
                                        {
                                            string Subject = "<html><body>  <b>  LOOM PRODUCTION ENTRY </b></body></html>&nbsp;"
                                                       + " <br><br>"
                                                       + "<html><body> Dear Sir </body></html>&nbsp;"
                                                       + "<br><br><html><body> <b>ROLL WISE HIGH DIFFERENCE BETWEEN ACTUAL AND REQUIRED GSM </b> </body></html>&nbsp;"
                                                       + "<br><HTML><body><color = blue> <b><table width=100% border=0 bgcolor= Beige >";


                                            Subject += "<tr bgcolor=Tan> <td bgcolor=Tan>Loom No</td> ";
                                            Subject += "<td bgcolor=Tan>Roll No</td> ";
                                            Subject += "<td bgcolor=Tan>Quality</td> ";
                                            Subject += "<td bgcolor=Tan>Req GSM</td> ";
                                            Subject += "<td bgcolor=Tan>Sector</td> ";
                                            Subject += "<td bgcolor=Tan>Meter</td> ";
                                            Subject += "<td bgcolor=Tan>Netwt</td> ";
                                            Subject += "<td bgcolor=Tan>Actual GSM</td> ";


                                            Subject += Environment.NewLine;
                                            for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                                            {
                                                if (Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[colLogic.Index].Value) == 1)
                                                {
                                                    Subject += "<tr bgcolor=Red> <td bgcolor=Red>  <font color=white>" + dataGridView1.Rows[i].Cells[DtLoomNo.Index].Value + "</font></td> ";
                                                    Subject += "<td bgcolor=Red><font color=white>" + dataGridView1.Rows[i].Cells[dtRollNo.Index].Value + "</font></td> ";
                                                    Subject += "<td bgcolor=Red><font color=white>" + dataGridView1.Rows[i].Cells[dtQuality.Index].Value + "</font></td> ";
                                                    Subject += "<td bgcolor=Red><font color=white>" + dataGridView1.Rows[i].Cells[dtReqGSM.Index].Value + "</font></td> ";
                                                    Subject += "<td bgcolor=Red><font color=white>" + dataGridView1.Rows[i].Cells[dtSector.Index].Value + "</font></td> ";
                                                    Subject += "<td bgcolor=Red><font color=white>" + dataGridView1.Rows[i].Cells[dtMtr.Index].Value + "</font></td> ";
                                                    Subject += "<td bgcolor=Red><font color=white>" + dataGridView1.Rows[i].Cells[dtNwt.Index].Value + "</font></td> ";
                                                    Subject += "<td bgcolor=Red><font color=white>" + dataGridView1.Rows[i].Cells[dtAGSM.Index].Value + "</font></td></tr> ";
                                                    Subject += Environment.NewLine;
                                                }
                                            }
                                            Subject += "</Table></body></HTML> ";

                                            FrmSendMail.SendAnEmail_HTML_NoMessage(EmailTo, EmailCC, "", "Roll Wise High Difference between Actual & Required GSM :" + dateLoomEntry.Value.ToString("dd-MMM-yyyy")
                                           , Subject, "erp@champalalgroup.com", false, "");
                                        }
                                    }
                                    #endregion
                                    isclear = true;
                                    dataGridView1.Rows.Clear();
                                    isclear = false;
                                    isgrid = false;
                                }
                            }
                        }
                        BindGrid("Top 100 ");
                    }
                }
            }
            catch (Exception ex)
            {
                if (!isupdate)
                    _RecordLogId = 0;
                if (BLNTRANS)
                    Database.RollBackTransaction();
                ErrorMessageBox.Show(ex);

            }
        }
        private void dataGridView2_DoubleClick(object sender, EventArgs e)
        {
            dateLoomEntry.MaxDate = DateTimePicker.MaximumDateTime;
            dateLoomEntry.MinDate = DateTimePicker.MinimumDateTime;

            isdoubleclick = true;
            if (dataGridView1.Rows.Count >= 0)
                dataGridView1.Rows.Clear();
            // buttonDelete.Enabled = true;
            buttonSave.Enabled = false;
            buttonUpdate.Enabled = true;
            dateLoomEntry.Enabled = false;
            BtnGetData.Enabled = false;

            btnprodDate.Visible = (FrmMain.UserName.ToLower() == "erp" ? true : false);
            dateLoomEntry.Enabled = (FrmMain.UserName.ToLower() == "erp" ? true : false);

            _RecordLogId = ProductionCommonFunction.SafeConvertToInt(dataGridView2["RecordLogid", dataGridView2.CurrentRow.Index].Value.ToString());
            //  FillMarketingOrderNo();
            int rowCount = 0;
            for (int i = 0; i < dataGridView2.Rows.Count - 1; i++)
            {
                if (ProductionCommonFunction.SafeConvertToInt(dataGridView2["RecordLogid", i].Value) != _RecordLogId)
                    continue;
                dataGridView1.Rows.Add();
                dataGridView1[DtLoomNo.Index, rowCount].ReadOnly = true;
                dataGridView1[DtLoomNo.Index, rowCount].Value = Convert.ToString(dataGridView2["LoomNo", i].Value);
                dataGridView1[dtRollNo.Index, rowCount].Value = dataGridView2["RollNo", i].Value.ToString();
                dataGridView1[dtQuality.Index, rowCount].Value = dataGridView2["Quality", i].Value.ToString();
                dataGridView1[dtMash.Index, rowCount].Value = dataGridView2["Mesh", i].Value.ToString();
                dataGridView1[dtReqGSM.Index, rowCount].Value = dataGridView2["ReqGSM", i].Value.ToString();
                dataGridView1[dtSector.Index, rowCount].Value = dataGridView2["Sector", i].Value.ToString();
                dataGridView1[dtMtr.Index, rowCount].Value = dataGridView2["Metre", i].Value.ToString();
                dataGridView1[dtGWT.Index, rowCount].Value = dataGridView2["GrossWt", i].Value.ToString();
                dataGridView1[dtTwt.Index, rowCount].Value = dataGridView2["TareWt", i].Value.ToString();
                dataGridView1[ColBailCoverWt.Index, rowCount].Value = dataGridView2["CoverWt", i].Value.ToString();
                dataGridView1[dtNwt.Index, rowCount].Value = dataGridView2["NetWt", i].Value.ToString();
                dataGridView1[dtAGMP.Index, rowCount].Value = dataGridView2["ActualGPM", i].Value.ToString();
                dataGridView1[dtAGSM.Index, rowCount].Value = dataGridView2["ActualGSM", i].Value.ToString();
                dataGridView1[dtRemrk.Index, rowCount].Value = dataGridView2["Remarks", i].Value.ToString();
                dataGridView1[DTwarpQty.Index, rowCount].Value = dataGridView2["WarpQty", i].Value.ToString();
                dataGridView1[dtweftQty.Index, rowCount].Value = dataGridView2["WeftQty", i].Value.ToString();
                dataGridView1[dtWarpBobbin.Index, rowCount].Value = dataGridView2["WarpBobbin", i].Value.ToString();
                dataGridView1[dtWeftBobbin.Index, rowCount].Value = dataGridView2["weftBobbin", i].Value.ToString();

                dataGridView1[colFGCode.Index, rowCount].Value = Convert.ToString(dataGridView2["FGItemCode", i].Value);


                comboShift.Text = dataGridView2["Shift", i].Value.ToString();
                srno = Convert.ToInt32(dataGridView2["transid", i].Value.ToString());
                dataGridView1[DtPartyName.Index, rowCount].Value = dataGridView2["Partyname", i].Value.ToString();
                dataGridView1[DtiInvNo.Index, rowCount].Value = dataGridView2["MarketingOrderno", i].Value.ToString();
                dateLoomEntry.MinDate = (DateTime)dataGridView2["sysdate", i].Value;
                dateLoomEntry.Value = (DateTime)dataGridView2["sysdate", i].Value;
                RollIndex = Convert.ToInt32(dataGridView2["RollIndex", i].Value);



                rowCount++;
            }
        }

        private void dateLoomEntry_ValueChanged(object sender, EventArgs e)
        {
            BindGrid("Top 100 ");
        }

        private bool approvalauth()
        {
            Database.OpenConnection(Utility.MaterialConnectionString);
            bool isauthority = false;
            Database.myreader = Database.GetExecuteReaderCommand("select approvalauth2 from warehousemaster where companyname='"
                       + comboCompanyName.Text + "' and warehousename='Loom Godown' and (approvalauth1 = '"
                       + FrmMain.UserName + "'or approvalauth2 = '"
                       + FrmMain.UserName + "' or approvalauth3 = '"
                       + FrmMain.UserName + "' or approvalauth4 = '" + FrmMain.UserName + "' or  approvalauth5 = '" + FrmMain.UserName + "')");
            if (Database.myreader.Read())
            {
                isauthority = true;
            }
            Database.myreader.Close();

            return isauthority;
        }
        private void buttonUpdate_Click(object sender, EventArgs e)
        {

            save(true);
            //below code is commented by Rikin on 04-Feb-2015
            //try
            //{
            //    if (Vallidation())
            //    {
            //        if (Database.OpenConnection(Utility.MaterialConnectionString))
            //        {
            //            bool isauthority = approvalauth();
            //            string status = "";
            //            if (isauthority)
            //                status = "Approved";
            //            else
            //                status = "Pending";

            //            //double size = 0;
            //            //string type = "";
            //            //string value = "";

            //            //if (Convert.ToDouble(dataGridView1.Rows[0].Cells[6].Value) != 0)
            //            //{
            //            //    value = Convert.ToString(dataGridView1.Rows[0].Cells[2].Value);
            //            //    string[] substring = value.Split(' ');
            //            //    size = Convert.ToDouble(substring[1]);
            //            //    type = substring[2];
            //            //    if (size > 0)
            //            //    {
            //            //        dataGridView1.Rows[0].Cells[9].Value = Convert.ToDouble(dataGridView1.Rows[0].Cells[7].Value) - Convert.ToDouble(dataGridView1.Rows[0].Cells[8].Value);
            //            //        dataGridView1.Rows[0].Cells[10].Value = (Convert.ToDouble(dataGridView1.Rows[0].Cells[9].Value) / Convert.ToDouble(dataGridView1.Rows[0].Cells[6].Value)) * 1000;
            //            //        if (type.ToString().Trim() == "FLAT")
            //            //            dataGridView1.Rows[0].Cells[11].Value = (Convert.ToDouble(dataGridView1.Rows[0].Cells[10].Value) / size) * 100;
            //            //        else if (type.ToString().Trim() == "FLAT-GZT")

            //            //            dataGridView1.Rows[0].Cells[11].Value = (Convert.ToDouble(dataGridView1.Rows[0].Cells[10].Value) / size) * 100;
            //            //        else
            //            //            dataGridView1.Rows[0].Cells[11].Value = (Convert.ToDouble(dataGridView1.Rows[0].Cells[10].Value) / (size * 2)) * 100;
            //            //        dataGridView1.Rows[0].Cells[11].Value = Math.Round(Convert.ToDouble(dataGridView1.Rows[0].Cells[11].Value), 2);
            //            //        dataGridView1.Rows[0].Cells[10].Value = Math.Round(Convert.ToDouble(dataGridView1.Rows[0].Cells[10].Value), 2);
            //            //        if (dataGridView1.Rows[0].Cells[12].Value.ToString().Trim() == "")
            //            //        {
            //            //            dataGridView1.Rows[0].Cells[12].Value = ".";
            //            //        }
            //            //    }
            //            //}

            //            Database.OpenConnection(Utility.MaterialConnectionString);
            //            Database.BeginTransaction();
            //            BobinConsumption();

            //            int RecordLogID = ProductionCommonFunction.SaveRecordLog(_RecordLogId, FrmMain.UserName, RecordLogFlag.Regular, string.Format("Loom Production is updated : {0} is allocated for {1})", dataGridView1[DtLoomNo.Index, 0].Value, comboCompanyName.Text));
            //            Database.GetExecuteNonQueryCommand("delete from Prod_ConsumptionMaster_Bifercation where RecordLogid=" + _RecordLogId + "");
            //            if (Database.GetExecuteNonQueryCommand("update MISLOOMProductionEntryNew set Quality = '"
            //                + dataGridView1[2, 0].Value + "',Mesh = '"
            //                + dataGridView1[3, 0].Value + "',ReqGSM = "
            //                + dataGridView1[4, 0].Value + ", Sector = '"
            //                + dataGridView1[5, 0].Value + "', Metre = "
            //                + dataGridView1[6, 0].Value + ",GrossWt = "
            //                + dataGridView1[7, 0].Value + ",TareWt = "
            //                + dataGridView1[8, 0].Value + ", NetWt = "
            //                + dataGridView1[9, 0].Value + ",ActualGPM = "
            //                + dataGridView1[10, 0].Value + ",ActualGSM = "
            //                + dataGridView1[11, 0].Value + ",Remarks = '"
            //                + dataGridView1[12, 0].Value + "',PartyName = '"
            //                + dataGridView1[13, 0].Value + "',MarketingOrderno='" + dataGridView1[14, 0].Value + "',hodapprove = '" + status + "'  where transid = " + srno + ""))
            //            {
            //                if (status == "Approved")
            //                    Database.GetExecuteNonQueryCommand("update MISLOOMProductionEntryNew set hodapprovedate = CAST(FLOOR(CAST(getdate() AS float)) AS datetime)" +
            //                        " where rollno = '" + dataGridView1[1, 0].Value + "' and companyname = '"
            //                    + comboCompanyName.Text + "'");
            //                MessageBox.Show("Data Updated Successfully");
            //            }

            //            Database.CommitTransaction();

            //            isclear = true;
            //            dataGridView1.Rows.Clear();
            //            isclear = false;
            //            Database.Closeconnection();
            //            Utility.UserInformation("Update ", "Loom Entry " + srno + "is Update for rOLLnO " + dataGridView1[1, 0].Value);
            //            BindGrid(" top 100 ");
            //        }
            //    }
            //}
            //catch (Exception ex)
            //{
            //    Database.RollBackTransaction();
            //    ErrorMessageBox.Show(ex);
            //}
        }

        private void buttonDelete_Click(object sender, EventArgs e)
        {
            try
            {
                //if (Database.OpenConnection(Utility.MaterialConnectionString))
                //{
                //    if (Database.GetExecuteNonQueryCommand("delete from MISLOOMProductionEntryNew where transid = "
                //        + srno + ""))
                //        MessageBox.Show("Data Deleted Successfully");
                //    dataGridView1.Rows.Clear();
                //    Database.Closeconnection();
                //    Utility.UserInformation("Delete ", "Loom Entry " + srno + "is Deleted for rOLLnO " + dataGridView1[1,0].Value);
                //    BindGrid();
                //    isclear = true;
                //    dataGridView1.Rows.Clear();
                //    isclear = false;
                //}

            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
            }
        }

        private void dataGridView1_UserDeletedRow(object sender, DataGridViewRowEventArgs e)
        {
            isclear = false;
        }

        private void dataGridView1_UserDeletingRow(object sender, DataGridViewRowCancelEventArgs e)
        {
            isclear = true;
        }

        //private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        //{
        //    try
        //    {
        //        double sum = 0;
        //        pfrm_sbp1.Text = "";

        //        foreach (DataGridViewRow row in this.dataGridView1.Rows)
        //        {
        //            foreach (DataGridViewColumn col in this.dataGridView1.Columns)
        //            {
        //                if (dataGridView1[col.Index, row.Index].Selected)
        //                    try
        //                    {
        //                        sum += Convert.ToDouble(dataGridView1[col.Index, row.Index].Value);
        //                    }
        //                    catch (Exception ex)
        //                    {
        //                        ex.ToString();
        //                    }
        //            }
        //        }
        //        pfrm_sbp1.Text = " Sum : " + sum.ToString();

        //    }
        //    catch (Exception ex)
        //    {
        //       //Nothing
        //    }
        //}


        private void dataGridView1_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            e.ThrowException = false;
        }

        //modified by Mahendra on 15.06.2015
        private void btn_calculate_Click(object sender, EventArgs e)
        {
            try
            {
                BobinConsumption();
            }
            catch (Exception ex)
            { ErrorMessageBox.Show(ex); }
        }

        //added by Mahendra on 15.06.2015
        private void btn_refresh_Click(object sender, EventArgs e)
        {
            int ComapnyId = ProductionCommonFunction.SafeConvertToInt(dtTemp.DefaultView.ToTable().Rows[0]["companyID"].ToString());
            dtLoomAllocationMaster = ProductionCommonFunction.GetLoomAllocationMaster(ComapnyId, dateLoomEntry.Value);
        }

        private void btnok_Click(object sender, EventArgs e)
        {
            try
            {

                if (Database.OpenConnection_Adapter(Utility.MaterialConnectionString))
                {
                    Database.myadapter = Database.GetAdapterCommand("select   * from MISLOOMProductionEntryNew with(nolock) where  companyname = '"
                        + comboCompanyName.Text + "' and sysdate >= '" + dtpSaveData.Value.ToString("yyyy-MM-dd") + "' and LoomNo != 0 order by transid desc");
                    DataSet dataset1 = new DataSet();
                    Database.myadapter.Fill(dataset1, "MISLOOMProductionEntryNew");

                    BindingSource datasource = new BindingSource(dataset1.Tables[0], null);
                    dataGridView2.DataSource = datasource;

                    // Add the AutoFilter header cell to each column.
                    foreach (DataGridViewColumn col in dataGridView2.Columns)
                    {
                        col.HeaderCell = new
                            DataGridViewAutoFilterColumnHeaderCell(col.HeaderCell);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Problem with loading Data :" + ex.Message);
            }
        }

        private void label7_Click(object sender, EventArgs e)
        {
            DataTable dt = new DataTable();

            foreach (DataGridViewColumn c in dataGridView1.Columns)
            {
                if (c.HeaderText.ToLower() != "roll no")
                    dt.Columns.Add(c.HeaderText);
                else if (c.HeaderText.ToLower() == "Marketing")
                    dt.Columns.Add("MarketingInvNo");
            }

            Export.ToFile(dt);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog fDialog = new OpenFileDialog();

            fDialog.Title = "Open Excel File";
            fDialog.Filter = "Excel File|*.xls;*.xlsx;*.csv";

            if (fDialog.ShowDialog() == DialogResult.OK)
            {
                dataGridView1.Rows.Clear();

                String path = fDialog.FileName;
                string connString = "";
                string strFileType = Path.GetExtension(fDialog.FileName.ToString()).ToLower();

                DataTable dt = new DataTable();

                if (strFileType.Trim() == ".csv")
                {
                    dt = ProductionCommonFunction.GetCsvFileToDatatable(path, true);
                }

                if (dt.Rows.Count > 0)
                {
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        if (Database.OpenConnection(Utility.MaterialConnectionString))
                        {
                            string fgcode = Convert.ToString(Database.GetExecuteNonQueryCommand_retrun("Select Itemcode from item where itemname='" +
                                   dt.Rows[i]["FG Code"] + "' and companyname='" + comboCompanyName.Text + "'"));

                            dataGridView1.Rows.Add
                                (
                                 dt.Rows[i]["Loom No"],
                                 "",
                                 dt.Rows[i]["Quality"],
                                 dt.Rows[i]["Mesh"],
                                 dt.Rows[i]["Req GSM"],
                                 dt.Rows[i]["Sector"],
                                 dt.Rows[i]["Metre"],
                                 dt.Rows[i]["Gross Wt"],
                                 dt.Rows[i]["Tare Wt"],
                                 dt.Rows[i]["Cover Wt"],
                                 dt.Rows[i]["Net Wt"],
                                 dt.Rows[i]["Actual GPM"],
                                 dt.Rows[i]["Actual GSM"],
                                 dt.Rows[i]["Remarks"],
                                 dt.Rows[i]["Party Name"],
                                 dt.Rows[i]["MarketingInvNo"],
                                 dt.Rows[i]["Warp Qty"],
                                 dt.Rows[i]["Weft Qty"],
                                 dt.Rows[i]["Warp Bobbin"],
                                 dt.Rows[i]["Weft Bobbin"], fgcode
                                );
                        }
                    }
                }
            }
        }

        private void PasteClipboard()
        {
            try
            {
                string s = Clipboard.GetText();
                string[] lines = s.Replace("\n", "").Split('\r');
                string[] fields;
                int iFail = 0, iRow = dataGridView1.CurrentCell.RowIndex;
                int iCol = dataGridView1.CurrentCell.ColumnIndex;
                DataGridViewCell oCell;
                int row = 0;
                int column = 0;
                foreach (string l in lines)
                {
                    dataGridView1.Rows.Add();
                    fields = l.Split('\t');
                    foreach (string f in fields)
                    {

                        dataGridView1[column++, row].Value = f;
                    }

                    row++;
                    column = 0;
                }
                return;

                //foreach (string line in lines)
                //{
                //    if (iRow < dataGridView1.RowCount && line.Length > 0)
                //    {
                //        string[] sCells = line.Split('\t');
                //        for (int i = 0; i < sCells.GetLength(0); ++i)
                //        {
                //            if (iCol + i < this.dataGridView1.ColumnCount)
                //            {
                //                oCell = dataGridView1[iCol + i, iRow];
                //                if (!oCell.ReadOnly)
                //                {
                //                    if (oCell.Value.ToString() != sCells[i])
                //                    {
                //                        oCell.Value = Convert.ChangeType(sCells[i],
                //                                              oCell.ValueType);
                //                        oCell.Style.BackColor = Color.Tomato;
                //                    }
                //                    else
                //                        iFail++;
                //                    //only traps a fail if the data has changed 
                //                    //and you are pasting into a read only cell
                //                }
                //            }
                //            else
                //            { break; }
                //        }
                //        dataGridView1.Rows.Add();
                //        iRow++;
                //    }
                //    else
                //    { break; }
                //    //if (iFail > 0)
                //    //    MessageBox.Show(string.Format("{0} updates failed due" +
                //    //                    " to read only column setting", iFail));
                //}
            }
            catch (FormatException)
            {
                MessageBox.Show("The data you pasted is in the wrong format for the cell");
                return;
            }
        }

        private void dataGridView1_KeyUp(object sender, KeyEventArgs e)
        {
            if ((e.Shift && e.KeyCode == Keys.Insert) || (e.Control && e.KeyCode == Keys.V))
            {
                PasteClipboard();
            }
        }

        private void btnprodDate_Click(object sender, EventArgs e)
        {
            try
            {

            }
            catch (Exception ex)
            {
                MessageBox.Show("Problem with Change Production date :" + ex.Message);
            }
        }
    }
}
