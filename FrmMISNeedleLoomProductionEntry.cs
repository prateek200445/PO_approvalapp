using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using DataGridViewAutoFilter;
using System.IO;

namespace ERP
{
    public partial class FrmMISNeedleLoomProductionEntry : Form
    {
        public System.Windows.Forms.StatusBarPanel pfrm_sbp1 = null;
        private filter.filterData filterData;
        public bool isdoubleclick = false;
        bool isclear = false;
        int CurrentSelectedRow = 0;
        public double RollIndex = 1000;
        public int transid = 0;
        public bool isgrid = false;
        public string ApproveDate = "";
        // public string productioncode = "";
        DataTable dtInvDtl;
        string ProdCode = "";
        DataTable dtOrderMaster;
        public FrmMISNeedleLoomProductionEntry()
        {
            InitializeComponent();
            Form_Load();
        }

        private void Form_Load()
        {
            try
            {

                dateLoomEntry.Format = DateTimePickerFormat.Custom;
                //dateLoomEntry.CustomFormat = "MM/dd/yyyy";
                dateLoomEntry.CustomFormat = Tools.ToolsCommonFunction.ERPDateSettings();
                dateLoomEntry.MaxDate = DateTime.Now;
                dateLoomEntry.MinDate = ProductionCommonFunction.ERPBackDateSettings();

                dtpSaveData.Format = DateTimePickerFormat.Custom;
                dtpSaveData.CustomFormat = Tools.ToolsCommonFunction.ERPDateSettings();
                dtpSaveData.MaxDate = DateTime.Now;

                filterData = new filter.filterData(dataGridView2);

                comboCompanyName.DataSource = ProductionCommonFunction.GetCompanyList();
                comboCompanyName.DisplayMember = ProductionCommonVariables.FieldName.CompanyName;
                comboCompanyName.ValueMember = ProductionCommonVariables.FieldName.IndentCode;

                comboCompanyName.Text = frmDefaultVale.CompanyName;
                ProdCode = comboCompanyName.SelectedValue.ToString();
                fillData();
                comboShift.SelectedIndex = 0;
                buttonUpdate.Enabled = false;
                buttonDelete.Enabled = false;
                BindGrid();

                DataTable dtType = new DataTable();
                dtType.Columns.Add("OrderType");
                dtType.Rows.Add(new object[] { "PP Webbing & Tie" });

                cmbType.DataSource = dtType;
                cmbType.ValueMember = "OrderType";
                cmbType.DisplayMember = "OrderType";
            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
            }
        }

        private void FrmMISNeedleLoomProductionEntry_Load(object sender, EventArgs e)
        {

        }
        void fillData()
        {

            if (Database.OpenConnection(Utility.MaterialConnectionString))
            {


                dtColor.DataSource = ProductionCommonFunction.GetMisColor();
                dtColor.ValueMember = "color"; //dt1.Columns[0].ColumnName.ToString();
                dtColor.DisplayMember = "color";// dt1.Columns[0].ColumnName.ToString();

                colFGCode.DataSource = Database.GetDataTable("select distinct itemcode,ItemName from item  where MISGRUPITEM = 'PP Webbing' and MISCATEGORY='SF' AND ITEMNAME NOT LIKE '%CUTPC%'  and todate is null and CompanyName='" + comboCompanyName.Text + "'");
                colFGCode.DisplayMember = "ItemName";
                colFGCode.ValueMember = "ItemCode";
                colFGCode.ReadOnly = false;
            }

            //   if (Database.OpenConnection(Utility.DespacthConnectionString))
            {

                //Fill Party combo in grid
                dtOrderMaster = ProductionCommonFunction.GetFIBCPartyName(comboCompanyName.Text);

                DataTable dt2 = dtOrderMaster.Copy();
                DtPartyName.DataSource = dt2.DefaultView.ToTable(true, "PartyName").Copy();
                DtPartyName.ValueMember = "PartyName";
                DtPartyName.DisplayMember = "PartyName";

                DtMarketingOrdNo.DataSource = dt2.DefaultView.ToTable(true, "PONO").Copy();
                DtMarketingOrdNo.ValueMember = "PONO";
                DtMarketingOrdNo.DisplayMember = "PONO";

            }


        }

        //private string GenerateMonthName(int monthNo)
        //{
        //    switch (monthNo)
        //    {
        //        case 1:
        //            return "NA";
        //        case 2:
        //            return "NB";
        //        case 3:
        //            return "NC";
        //        case 4:
        //            return "ND";
        //        case 5:
        //            return "NE";
        //        case 6:
        //            return "NF";
        //        case 7:
        //            return "NG";
        //        case 8:
        //            return "NH";
        //        case 9:
        //            return "NI";
        //        case 10:
        //            return "NJ";
        //        case 11:
        //            return "NK";
        //        case 12:
        //            return "NL";

        //    }
        //    return "NA";
        //}

        void getRollNo()
        {
            string preRollNo = "";
            string NewRollNo = "";
            double RollIndx = 0;
            for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
            {
                if (Convert.ToString(dataGridView1.Rows[i].Cells[1].Value) != "")
                    continue;
                if (i == 0)
                {
                    dataGridView1.Rows[i].Cells[1].Value = ProductionCommonFunction.RollNo(comboCompanyName.Text, "NeedleLoom", ProdCode, Convert.ToDateTime(dateLoomEntry.Value));
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

        private void dataGridView1_CurrentCellChanged(object sender, EventArgs e)
        {
            if (dataGridView1.CurrentCell == null
               || dataGridView1.CurrentCell.RowIndex == -1)
                return;
            try
            {
                //string value = "";
                DataTable dt1 = new DataTable();
                if (!isclear)
                {
                    if (!isdoubleclick)
                    {

                        if (dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[0].FormattedValue != null)
                        {
                            if (dataGridView1.CurrentRow.Index == 0)
                                dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[1].Value = ProductionCommonFunction.RollNo(comboCompanyName.Text, "NeedleLoom", ProdCode, Convert.ToDateTime(dateLoomEntry.Value));
                            else
                            {// added byy Rikin on 21-11-2014 to increment rollno form previous roll no
                                getRollNo();
                            }

                            if (dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Index != 0)
                            {
                                if (Convert.ToString(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[8].Value) == "")
                                {
                                    dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[8].Value = dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex - 1].Cells[8].Value;
                                    dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[9].Value = dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex - 1].Cells[9].Value;
                                }
                            }


                        }
                    }

                    if (dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[3].FormattedValue.ToString().Trim() != "" &&
                        dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[5].FormattedValue.ToString().Trim() == "")
                    {
                        if (Convert.ToString(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[3].Value) == "25")
                            dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[5].Value = "BROWN";
                        else if (Convert.ToString(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[3].Value) == "35")
                            dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[5].Value = "BLUE";
                        else if (Convert.ToString(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[3].Value) == "45")
                            dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[5].Value = "RED";
                        else if (Convert.ToString(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[3].Value) == "55")
                            dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[5].Value = "YELLOW";
                        else if (Convert.ToString(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[3].Value) == "65")
                            dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[5].Value = "ORANGE";
                    }
                    if (Convert.ToString(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[0].FormattedValue).Trim() != "" &&
                        dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[1].FormattedValue.ToString().Trim() != "" &&
                        dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[2].FormattedValue.ToString().Trim() != "" &&
                        dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[3].FormattedValue.ToString().Trim() != "" &&
                        dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[4].FormattedValue.ToString().Trim() != "" &&
                        dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[5].FormattedValue.ToString().Trim() != "")
                    {
                        dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[6].Value = dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[2].Value + "/" +
                            dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[3].Value + " " + dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[4].Value + "/" + dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[5].Value;
                    }
                    if (dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[10].FormattedValue.ToString().Trim() == "")
                        dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[10].Value = ".";

                }
            }

            //}
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
            }
        }

        private void buttonInitialize_Click(object sender, EventArgs e)
        {
            dateLoomEntry.Format = DateTimePickerFormat.Custom;
            dateLoomEntry.CustomFormat = Tools.ToolsCommonFunction.ERPDateSettings();
            dateLoomEntry.MinDate = ProductionCommonFunction.ERPBackDateSettings();
            dateLoomEntry.MaxDate = DateTime.Now;
            dateLoomEntry.Value = DateTime.Now;
            isdoubleclick = false;
            isclear = true;
            BtnGetData.Enabled = true;
            if (dataGridView1.Rows.Count > 0)
                dataGridView1.Rows.Clear();
            isclear = false;
            dataGridView1[0, 0].ReadOnly = false;
            buttonSave.Enabled = true;
            buttonUpdate.Enabled = false;
            buttonDelete.Enabled = false;
            dateLoomEntry.Enabled = true;
        }

        public bool Validate()
        {
            try
            {
                if (Database.OpenConnection(Utility.MaterialConnectionString))
                {
                    for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                    {
                        if (dataGridView1.Rows[i].Cells[0].FormattedValue.ToString() == "" || dataGridView1.Rows[i].Cells[1].FormattedValue.ToString() == "" ||
                            dataGridView1.Rows[i].Cells[2].FormattedValue.ToString() == "" || dataGridView1.Rows[i].Cells[3].FormattedValue.ToString() == "" ||
                        dataGridView1.Rows[i].Cells[4].FormattedValue.ToString() == "" || dataGridView1.Rows[i].Cells[5].FormattedValue.ToString() == "" ||
                        dataGridView1.Rows[i].Cells[6].FormattedValue.ToString() == "" || dataGridView1.Rows[i].Cells[7].FormattedValue.ToString() == "" ||
                        dataGridView1.Rows[i].Cells[8].FormattedValue.ToString() == "" || dataGridView1.Rows[i].Cells[9].FormattedValue.ToString() == "" ||
                        dataGridView1.Rows[i].Cells[10].FormattedValue.ToString() == "")
                        {
                            MessageBox.Show("Pls fill data properly one of the field is blank");
                            return false;

                        }
                    }

                    if (buttonSave.Enabled == true)
                    {
                        // This validation for Consumption

                        DateTime dt = dateLoomEntry.Value;
                        DataTable dtdate = new DataTable();
                        DateTime date = dt.AddDays(-1);
                        Database.myreader = Database.GetExecuteReaderCommand("select Date from ConsumptionMaster where Date = '" + date.ToString("yyyy-MM-dd") + "' and plantname = 'Needle Loom' and Companyname='" + comboCompanyName.Text + "'");
                        dtdate.Load(Database.myreader);
                        if (dtdate.Rows.Count <= 0)
                        {
                            MessageBox.Show("You can't do entry, first of all do entry in consumption master for previous date.");
                            Database.myreader.Close();
                            return false;
                        }

                        /////////////////

                        #region Check Validation for Already done entry in Consumption Master

                        //Database.myreader = Database.GetExecuteReaderCommand("select PlantName from ConsumptionMaster where PlantName='Needle Loom' and Companyname='" + comboCompanyName.Text + "'"
                        //+ " and Date = '" + dateLoomEntry.Value.ToString("yyyy-MM-dd") + "'");
                        //DataTable dtcon = new DataTable();
                        //dtcon.Load(Database.myreader);
                        //if (dtcon.Rows.Count > 0)
                        //{
                        //    MessageBox.Show("You can't do entry because You have already done entry on this date in Consumption.");
                        //    Database.myreader.Close();
                        //    return false;
                        //}
                        #endregion

                    }
                    else
                    {

                        Database.myreader = Database.GetExecuteReaderCommand("select PlantName from ConsumptionMaster where PlantName='Needle Loom' and Companyname='"
                            + comboCompanyName.Text + "'  and Date = '" + dateLoomEntry.Value.ToString("yyyy-MM-dd") + "'");
                        DataTable dtcon = new DataTable();
                        dtcon.Load(Database.myreader);
                        if (dtcon.Rows.Count > 0)
                        {
                            Database.myreader = Database.GetExecuteReaderCommand("select netwt from misneedleloomproductionentry where "
                            + " rollno='" + Convert.ToString(dataGridView1.Rows[0].Cells[1].Value) + "'");
                            DataTable dtloom = new DataTable();
                            dtloom.Load(Database.myreader);
                            if (dtloom.Rows.Count > 0)
                            {
                                if (Math.Round(Convert.ToDouble(Utility.ConverttoInt(dataGridView1.Rows[0].Cells["Column10"].Value.ToString())), 2) !=
                                    Math.Round(Convert.ToDouble(dtloom.Rows[0]["NetWt"].ToString()), 2))
                                {
                                    MessageBox.Show("You can't update Net Weight because You have already done entry in Consumption.");
                                    Database.myreader.Close();
                                    return false;
                                }
                            }
                            Database.myreader.Close();
                        }
                        #region Check Validation for Already done entry in Consumption Master

                        Database.myreader = Database.GetExecuteReaderCommand("select PlantName from ConsumptionMaster where PlantName='Needle Loom' and Companyname='"
                            + comboCompanyName.Text + "' and Date = '" + dateLoomEntry.Value.ToString("yyyy-MM-dd") + "'");
                        dtcon = new DataTable();
                        dtcon.Load(Database.myreader);
                        if (dtcon.Rows.Count > 0)
                        {
                            MessageBox.Show("You can't do entry because You have already done entry on this date in Consumption.");
                            Database.myreader.Close();
                            return false;
                        }
                        #endregion
                        Database.myreader = Database.GetExecuteReaderCommand("select rollno from misneedleloomproductionentry where rollno = '" + dataGridView1.Rows[0].Cells[1].FormattedValue.ToString() + "' and companyname = '"
                                             + comboCompanyName.Text + "' and ExiceApprove = 'Approved'");
                        DataTable dt2 = new DataTable();
                        dt2.Load(Database.myreader);
                        if (dt2.Rows.Count > 0)
                        {
                            MessageBox.Show("Sorry! You can't modify this record, its already approved.");
                            Database.myreader.Close();
                            return false;
                        }


                        Database.myreader = Database.GetExecuteReaderCommand(" select rollno from (select RollNo from MISrollissue with(nolock) where companyname = '" + comboCompanyName.Text + "' "
                                            + "     union all "
                                            + " select URollNo from MISLaminationentry with(nolock) where companyname = '" + comboCompanyName.Text + "' "
                                            + "     union all "
                                            + " select uRollNo from MISreformationentry with(nolock) where companyname = '" + comboCompanyName.Text + "' "
                                            + "     union all "
                                            + " select RollNo from MISrollfordespatch with(nolock) where companyname = '" + comboCompanyName.Text + "' "
                                            + "     ) as temp where rollno = '" + dataGridView1.Rows[0].Cells[1].FormattedValue.ToString() + "'");
                        DataTable dt3 = new DataTable();
                        dt3.Load(Database.myreader);
                        if (dt3.Rows.Count > 0)
                        {
                            MessageBox.Show("Sorry! You can't modify this record, its already issued.");
                            Database.myreader.Close();
                            return false;
                        }


                    }


                }

                return true;
            }
            catch (Exception)
            {
                return false;
                throw;
            }
        }


        private void buttonSave_Click(object sender, EventArgs e)
        {
            try
            {
                if (Validate())
                {
                    double TotalNetwt = 0;
                    bool isentry = false;
                    RollIndex = 0;
                    bool isauthority = false;
                    string status = "";
                    DialogResult drs = MessageBox.Show("Do you want to save ?"
                         , "Roll Entry", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                    if (drs.CompareTo(DialogResult.Yes) == 0)
                    {
                        if (Database.OpenConnection(Utility.MaterialConnectionString))
                        {
                            Database.myreader = Database.GetExecuteReaderCommand("select companyname from warehousemaster where companyname='"
                                                       + comboCompanyName.Text + "' and warehousename='Needle Loom Godown' and (approvalauth1 = '"
                                                       + FrmMain.UserName + "'or approvalauth2 = '"
                                                       + FrmMain.UserName + "' or approvalauth3 = '"
                                                       + FrmMain.UserName + "' or approvalauth4 = '" + FrmMain.UserName + "' or  approvalauth5 = '" + FrmMain.UserName + "')");
                            if (Database.myreader.Read())
                            {
                                isauthority = true;
                            }
                            Database.myreader.Close();


                            if (isauthority)
                            {
                                status = "Approved";
                                ApproveDate = DateTime.Now.ToShortDateString();
                            }
                            else
                            {
                                status = "Pending";
                                ApproveDate = "NULL";
                            }
                        }

                        DateTime dt = dateLoomEntry.Value;
                        string year = Convert.ToString(dt.Year);
                        string yearpart = year.Substring(2, 2);

                        //string MonthName = GenerateMonthName(dt.Month);
                        string MergeRoll = yearpart + ProdCode;
                        string RollNo = MergeRoll + "0";

                        //getrollno();

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
                                    " warehousename='Roll Material Godown'"));
                                if (Rec == 0)
                                {
                                    Database.GetExecuteNonQueryCommand("Insert into warehouse (ItemCode,ItemName,GroupName,Deptt,Minlevel,Maxlevel,ReOrder,Unit,StkInHand,TypeVendor,NameVendor,EmailVendor,QtyOrder,MnthlyConsum,Rate,CompanyName,LoginName,Dummy1,OpeningStock,SubGroupName,WareHouseName) " +
                                        "select ItemCode,ItemName,GroupName,Deptt,Minlevel,Maxlevel,ReOrder,Unit,0 StkInHand,TypeVendor,NameVendor,EmailVendor,QtyOrder,MnthlyConsum,Rate,CompanyName,LoginName,Dummy1,OpeningStock,SubGroupName,'Roll Material Godown' from item where itemcode='" +
                                        dataGridView1.Rows[i].Cells[colFGCode.Index].Value + "' and companyname='" + comboCompanyName.Text + "'");
                                }
                                #endregion
                            }
                            #endregion

                            string preRollNo = ProductionCommonFunction.RollNo(comboCompanyName.Text, "NeedleLoom", ProdCode, Convert.ToDateTime(dateLoomEntry.Value));
                            string NewRollNo = "";
                            Database.BeginTransaction();

                            for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                            {
                                if (dataGridView1.Rows[i].Cells[7].FormattedValue.ToString() == "")
                                    dataGridView1[7, i].Value = ".";

                                //Database.myreader = Database.GetExecuteReaderCommand("select top 1 RollIndex + 1 from misneedleloomproductionentry where CompanyName='" + comboCompanyName.Text + "' order by TRANSID desc");
                                //if (Database.myreader.Read())
                                //    RollIndex = Convert.ToInt32(Database.myreader[0].ToString());
                                //Database.myreader.Close();
                                RollIndex = Utility.SafeConvertToDouble(preRollNo.Substring(preRollNo.Length - 5));

                                //if (
                                string strsql = "insert into misneedleloomproductionentry(RollNo, Size, ReqGPM, Color, Marking, Quality, NetWt, Remarks, " +
                                    " CompanyName, Sysdate, Shift, RollIndex, LoomNo, PartyName, MarketingOrdNo, HODApprove, HODApproveDate, ExiceApprove, ExiceApproveDate, RollType,UserName,FGITEMCODE,IsCons) " +
                                    " values('" + preRollNo + "',"
                                    + Utility.ConverttoInt(Convert.ToString(dataGridView1["Column1", i].FormattedValue)) + ",'" + dataGridView1["Column5", i].FormattedValue + "','"
                                    + dataGridView1["dtColor", i].FormattedValue + "','" + dataGridView1["dtmarking", i].FormattedValue + "','"
                                    + dataGridView1["Column3", i].FormattedValue + "','" + Utility.ConverttoInt(Convert.ToString(dataGridView1["Column10", i].FormattedValue)) + "','"
                                    + dataGridView1["Column13", i].FormattedValue + "','"
                                    + comboCompanyName.Text + "','"
                                    + dateLoomEntry.Value.ToString("yyyy-MM-dd") + "','" + comboShift.Text + "'," + RollIndex + "," + dataGridView1["DtLoomNo", i].FormattedValue + ",'"
                                    + dataGridView1["DtPartyName", i].FormattedValue + "','" + dataGridView1["DtMarketingOrdNo", i].FormattedValue + "','" + status + "'," + ApproveDate
                                    + ",'Pending',NULL,'" + cmbType.Text + "','" + FrmMain.UserName + "','" + Convert.ToString(dataGridView1[colFGCode.Index, i].Value) + "',1)";

                                Database.GetExecuteNonQueryCommand(strsql);


                                int transid = Convert.ToInt32(Database.GetExecuteNonQueryCommand_retrun("select max(transid) from misneedleloomproductionentry"));
                                ProductionCommonFunction.InsertStockJournal(transid, dateLoomEntry.Value, comboCompanyName.Text, "Production", Convert.ToString(dataGridView1[colFGCode.Index, i].FormattedValue),
                                    Convert.ToDouble(dataGridView1["Column10", i].FormattedValue), 0, 0, "KGS", frmDefaultVale.Year, transid, transid, Convert.ToString(dataGridView1[colFGCode.Index, i].Value), preRollNo, "NLoom");


                                Database.GetExecuteNonQueryCommand("Update Warehouse set stkinHand=Round(stkinHand + "
                                                + ProductionCommonFunction.SafeConvertToDouble(dataGridView1["Column10", i].FormattedValue)
                                                + ",2) where CompanyName='" + comboCompanyName.Text + "' and WarehouseName='Roll Material Godown' and ItemCode='"
                                                + Convert.ToString(dataGridView1.Rows[i].Cells[colFGCode.Index].Value) + "'");

                                RollIndex++;
                                TotalNetwt += ProductionCommonFunction.SafeConvertToDouble(dataGridView1["Column10", i].FormattedValue);
                                NewRollNo = preRollNo.Substring(0, preRollNo.Length - 5) + "" + (string.Format("{0:00000.#}", RollIndex));
                                preRollNo = NewRollNo;
                                isentry = true;

                            }
                            #region Open Consumption Entry form for Entry
                        consumption:
                            FrmConsumptionMaster frm = new FrmConsumptionMaster(dateLoomEntry.Value, true, "NeedleLoom");
                            frm.ShowDialog();

                            //Check for Entry done or not
                            //double TotalCons = Utility.SafeConvertToDouble(Database.GetExecuteNonQueryCommand_retrun("select isnull(sum(Netwt),0) Netwt from( select case when  ItemName='Wastage' then -1 * isnull( (Consumption),0) else   isnull( (Consumption),0)  end as Netwt " +
                            //    " from ConsumptionMaster where Companyname='" + comboCompanyName.Text + "' and date='" + dateLoomEntry.Value.ToString("yyyy-MM-dd") +
                            //    "' and PlantName='Needle Loom'  ) A "));

                            double TotalCons = frm.dblTranTotalProd;

                            if (Math.Round(TotalCons, FrmMain.ERPQtyDigit) != Math.Round(TotalNetwt, FrmMain.ERPQtyDigit))
                            {
                                drs = MessageBox.Show("Consumption Entry for Needle Loom is not match with Production entry?\nDo You want to change Production Entry"
                                    , "Roll Entry", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                                if (drs.CompareTo(DialogResult.Yes) == 0)
                                {
                                    Database.RollBackTransaction();
                                    return;
                                }
                                else
                                {
                                    goto consumption;
                                }
                            }
                            else
                            {
                                Database.GetExecuteReaderCommand("UPDATE misneedleloomproductionentry SET IsCons=0 WHERE CompanyName='" + comboCompanyName.Text
                                + "' and sysdate='" + dateLoomEntry.Value.ToString("yyyy-MM-dd") + "' and IsCons=1");
                            }

                            #endregion
                            Database.CommitTransaction();
                            Database.Closeconnection();

                            if (isentry)
                            {

                                Utility.UserInformation("Save ", "ROLL Entry " + dateLoomEntry.Value.ToString("yyyy-MM-dd") + "is Saved for SHIFT " + comboShift.Text);
                                Utility.UserInformation("Save", "ConsumptionMaster Entry Done Successfully");
                                MessageBox.Show("Data Saved Succesfully");
                                isclear = true;
                                dataGridView1.Rows.Clear();
                                isclear = false;
                            }
                            BindGrid();
                            frm.Close();
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
        private void BindGrid()
        {
            if (Database.OpenConnection_Adapter(Utility.MaterialConnectionString))
            {
                Database.myadapter = Database.GetAdapterCommand("select top 1000  * from misneedleloomproductionentry where  companyname = '"
                    + comboCompanyName.Text + "' order by transid desc");
                DataSet dataset1 = new DataSet();
                Database.myadapter.Fill(dataset1, "misneedleloomproductionentry");

                BindingSource datasource = new BindingSource(dataset1.Tables[0], null);
                dataGridView2.DataSource = datasource;



            }

        }

        private void dataGridView2_DoubleClick(object sender, EventArgs e)
        {
            try
            {

                dateLoomEntry.MaxDate = DateTimePicker.MaximumDateTime;
                dateLoomEntry.MinDate = DateTimePicker.MinimumDateTime;

                buttonSave.Enabled = false;
                buttonUpdate.Enabled = true;
                buttonDelete.Enabled = true;
                dateLoomEntry.Enabled = false;
                BtnGetData.Enabled = false;
                isdoubleclick = true;
                if (dataGridView1.Rows.Count >= 2)
                {
                    dataGridView1.Rows.Clear();
                }
                dataGridView1[0, 0].ReadOnly = true;
                dataGridView1[0, 0].Value = dataGridView2[13, dataGridView2.CurrentRow.Index].Value.ToString();
                dataGridView1[1, 0].Value = dataGridView2[0, dataGridView2.CurrentRow.Index].Value.ToString();
                dataGridView1[2, 0].Value = dataGridView2[1, dataGridView2.CurrentRow.Index].Value.ToString();
                dataGridView1[3, 0].Value = dataGridView2[2, dataGridView2.CurrentRow.Index].Value.ToString();
                dataGridView1[4, 0].Value = dataGridView2[3, dataGridView2.CurrentRow.Index].Value.ToString();
                dataGridView1[5, 0].Value = dataGridView2[4, dataGridView2.CurrentRow.Index].Value.ToString();
                dataGridView1[6, 0].Value = dataGridView2[5, dataGridView2.CurrentRow.Index].Value.ToString();
                dataGridView1[7, 0].Value = dataGridView2[6, dataGridView2.CurrentRow.Index].Value.ToString();
                dataGridView1[8, 0].Value = dataGridView2[14, dataGridView2.CurrentRow.Index].Value.ToString();
                dataGridView1[9, 0].Value = dataGridView2[15, dataGridView2.CurrentRow.Index].Value.ToString();
                dataGridView1[10, 0].Value = dataGridView2[7, dataGridView2.CurrentRow.Index].Value.ToString();

                dataGridView1[colFGCode.Index, 0].Value = Convert.ToString(dataGridView2["FGITEMCODE", dataGridView2.CurrentRow.Index].Value);

                comboShift.Text = dataGridView2[10, dataGridView2.CurrentRow.Index].Value.ToString();
                transid = Convert.ToInt32(Utility.ConverttoInt(dataGridView2[12, dataGridView2.CurrentRow.Index].Value.ToString()));

                dateLoomEntry.Value = (DateTime)dataGridView2[9, dataGridView2.CurrentRow.Index].Value;
            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
            }
        }

        private void buttonUpdate_Click(object sender, EventArgs e)
        {
            try
            {
                if (Validate())
                {

                    bool isauthority = false;
                    string status = "";
                    if (Database.OpenConnection(Utility.MaterialConnectionString))
                    {
                        Database.myreader = Database.GetExecuteReaderCommand("select companyname  from warehousemaster where companyname='"
                                                        + comboCompanyName.Text + "' and warehousename='Needle Loom Godown' and (approvalauth1 = '"
                                                        + FrmMain.UserName + "'or approvalauth2 = '"
                                                        + FrmMain.UserName + "' or approvalauth3 = '"
                                                        + FrmMain.UserName + "' or approvalauth4 = '" + FrmMain.UserName + "' or  approvalauth5 = '" + FrmMain.UserName + "')");
                        if (Database.myreader.Read())
                        {
                            isauthority = true;
                        }
                        Database.myreader.Close();


                        if (isauthority)
                        {
                            status = "Approved";
                            ApproveDate = Convert.ToDateTime(DateTime.Now).ToString("MM-dd-yyyy");
                        }
                        else
                        {
                            status = "Pending";
                            ApproveDate = "";
                        }
                    }
                    double size = 0;
                    string type = "";
                    string value = "";

                    if (Database.OpenConnection(Utility.MaterialConnectionString))
                    {



                        if (Convert.ToString(dataGridView1.Rows[0].Cells[colFGCode.Index].Value).Length == 0)
                        {
                            MessageBox.Show("Please Select FG Code");
                            return;
                        }
                        #region Check Item availble in Warehouse or not
                        int Rec = Convert.ToInt32(Database.GetExecuteNonQueryCommand_retrun("Select count(*) from Warehouse where itemcode='" +
                            dataGridView1.Rows[0].Cells[colFGCode.Index].Value + "' and companyname='" + comboCompanyName.Text + "' and  " +
                            " warehousename='Roll Material Godown'"));
                        if (Rec == 0)
                        {
                            Database.GetExecuteNonQueryCommand("Insert into warehouse (ItemCode,ItemName,GroupName,Deptt,Minlevel,Maxlevel,ReOrder,Unit,StkInHand,TypeVendor,NameVendor,EmailVendor,QtyOrder,MnthlyConsum,Rate,CompanyName,LoginName,Dummy1,OpeningStock,SubGroupName,WareHouseName) " +
                                "select ItemCode,ItemName,GroupName,Deptt,Minlevel,Maxlevel,ReOrder,Unit,0 StkInHand,TypeVendor,NameVendor,EmailVendor,QtyOrder,MnthlyConsum,Rate,CompanyName,LoginName,Dummy1,OpeningStock,SubGroupName,'Roll Material Godown' from item where itemcode='" +
                                dataGridView1.Rows[0].Cells[colFGCode.Index].Value + "' and companyname='" + comboCompanyName.Text + "'");
                        }
                        #endregion

                        Database.BeginTransaction();



                        double TotalUpNetwt = Utility.SafeConvertToDouble(Database.GetExecuteNonQueryCommand_retrun("select netwt from misneedleloomproductionentry where TRANSID="
                            + transid));
                        Database.GetExecuteNonQueryCommand("UPDATE WareHouse SET StkInHand = StkInHand-" + TotalUpNetwt + " WHERE CompanyName='" + comboCompanyName.Text
                            + "' and WareHouseName='Roll Material Godown' and itemcode='" + dataGridView1.Rows[0].Cells[colFGCode.Index].Value + "'");

                        Database.ExecuteNonQuery("delete from stockJournalprod where srno = " + transid + " and company_name='" + comboCompanyName.Text + "'");

                        bool blnsave = Database.GetExecuteNonQueryCommand("update misneedleloomproductionentry set " +
                            " LoomNo = " + dataGridView1.Rows[0].Cells["DtLoomNo"].FormattedValue + "," +
                            " size = " + dataGridView1.Rows[0].Cells["Column1"].FormattedValue + ", " +
                            " ReQGPM = '" + dataGridView1.Rows[0].Cells["Column5"].FormattedValue + "', " +
                            " Color = '" + dataGridView1.Rows[0].Cells["dtColor"].FormattedValue + "', " +
                            " Marking = '" + dataGridView1.Rows[0].Cells["dtmarking"].FormattedValue + "', " +
                            " Quality = '" + dataGridView1.Rows[0].Cells["Column3"].FormattedValue + "', " +
                            " NetWt = " + dataGridView1.Rows[0].Cells["Column10"].FormattedValue + ", " +
                            " Remarks = '" + dataGridView1.Rows[0].Cells["Column13"].FormattedValue + "', " +
                            " shift = '" + comboShift.Text + "', " +
                            " PartyName = '" + dataGridView1.Rows[0].Cells["DtPartyName"].FormattedValue + "', " +
                            " MarketingOrdNo = '" + dataGridView1.Rows[0].Cells["DtMarketingOrdNo"].FormattedValue + "', " +
                            " HODApprove = '" + status + "', " +
                            " HODApproveDate = '" + ApproveDate + "', " +
                            " RollType='" + cmbType.Text + "' " +
                            " WHERE TRANSID = " + transid + "");

                        if (blnsave)
                        {
                            Database.GetExecuteNonQueryCommand("Update Warehouse set stkinHand=Round(stkinHand + "
                                                    + ProductionCommonFunction.SafeConvertToDouble(dataGridView1["Column10", 0].FormattedValue)
                                                    + ",2) where CompanyName='" + comboCompanyName.Text + "' and WarehouseName='Roll Material Godown' and ItemCode='"
                                                    + Convert.ToString(dataGridView1.Rows[0].Cells[colFGCode.Index].Value) + "'");
                            ProductionCommonFunction.InsertStockJournal(transid, dateLoomEntry.Value, comboCompanyName.Text, "Production", Convert.ToString(dataGridView1[colFGCode.Index, 0].FormattedValue),
                                    Convert.ToDouble(dataGridView1["Column10", 0].FormattedValue), 0, 0, "KGS", frmDefaultVale.Year, transid, transid, Convert.ToString(dataGridView1[colFGCode.Index, 0].Value),
                                    Convert.ToString(dataGridView1.Rows[0].Cells["Column2"].Value), "NLoom");

                        }
                        Database.CommitTransaction();
                        Database.Closeconnection();
                        Utility.UserInformation("Update ", "Roll Entry " + transid + "is Update for rOLLnO " + dataGridView1[1, 0].Value);
                        MessageBox.Show("Data Updated Successfully");
                    }

                    BindGrid();
                    isclear = true;
                    dataGridView1.Rows.Clear();
                    isclear = false;
                }
            }
            catch (Exception ex)
            {
                Database.RollBackTransaction();
                ErrorMessageBox.Show(ex);
            }
        }

        private void dataGridView1_UserDeletingRow(object sender, DataGridViewRowCancelEventArgs e)
        {
            isclear = true;
        }

        private void dataGridView1_UserDeletedRow(object sender, DataGridViewRowEventArgs e)
        {
            isclear = false;
        }

        private void dataGridView1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyData == Keys.F6)
            {
                if (this.dataGridView1.CurrentCell.RowIndex - 1 > -1)
                {
                    int rowIndex = this.dataGridView1.CurrentCell.RowIndex;
                    for (int i = 1; i <= 7; i++)
                        this.dataGridView1[i, rowIndex].Value = this.dataGridView1[i, rowIndex - 1].Value;

                }
            }

            if (e.KeyData == Keys.F8)
            {
                if (this.dataGridView1.CurrentCell.RowIndex - 1 > -1)
                {
                    CurrentSelectedRow = this.dataGridView1.CurrentCell.RowIndex;
                }
            }


            if (e.KeyData == Keys.F9)
            {
                if (this.dataGridView1.CurrentCell.RowIndex - 1 > -1)
                {
                    int rowIndex = this.dataGridView1.CurrentCell.RowIndex;
                    for (int i = 1; i <= 7; i++)
                        this.dataGridView1[i, rowIndex].Value = this.dataGridView1[i, CurrentSelectedRow].Value;



                }
            }
        }

        private void dataGridView1_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            try
            {
                if (!isdoubleclick)
                {
                    if (dataGridView1.CurrentRow.Cells.IndexOf(dataGridView1.CurrentCell) == 8)
                    {
                        DataGridViewComboBoxCell Col1 = (DataGridViewComboBoxCell)dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[8];
                        DataGridViewComboBoxCell Col2 = (DataGridViewComboBoxCell)dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[9];

                        if (Database.OpenConnection(Utility.DespacthConnectionString))
                        {
                            DataTable dt = new DataTable();

                            dt = dtOrderMaster.Copy();
                            dt.DefaultView.RowFilter = "PartyName='" + Col1.FormattedValue + "'";

                            Col2.DataSource = dt.DefaultView.ToTable(true, "PONO");
                            Col2.ValueMember = "PONO";
                            Col2.DisplayMember = "PONO";

                        }
                    }
                }
            }
            catch (Exception)
            {

                throw;
            }
        }



        private void dataGridView1_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            e.ThrowException = false;
        }

        private void dataGridView1_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            if (e.Control is TextBox)
            {
                TextBox tb = e.Control as TextBox;
                tb.KeyPress += new KeyPressEventHandler(tb_KeyPress);
            }
            if (dataGridView1.CurrentCell.ColumnIndex == DtPartyName.Index)
            {
                ComboBox combo = e.Control as ComboBox;
                new ComboFilter(combo, true);

                if (combo == null)
                    return;

                combo.DropDownStyle = ComboBoxStyle.DropDown;
            }
            else if (dataGridView1.CurrentCell.ColumnIndex == DtMarketingOrdNo.Index)
            {
                ComboBox combo = e.Control as ComboBox;
                new ComboFilter(combo, true);

                if (combo == null)
                    return;

                combo.DropDownStyle = ComboBoxStyle.DropDown;
            }
        }
        void tb_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (this.dataGridView1.CurrentCell.ColumnIndex == 7)
            {
                if (!(char.IsDigit(e.KeyChar)))
                {
                    if (e.KeyChar != '\b' && e.KeyChar != '.') //allow the backspace key
                    {
                        e.Handled = true;
                    }
                }
            }
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

        private void BtnGetData_Click(object sender, EventArgs e)
        {
            getRollNo();
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
                                 dt.Rows[i]["Size(cm)"],
                                 dt.Rows[i]["ReqGPM"],
                                 dt.Rows[i]["Colour"],
                                 dt.Rows[i]["Marking"],
                                 "" + dt.Rows[i]["Size(cm)"] + "/" + dt.Rows[i]["ReqGPM"] + " " + dt.Rows[i]["Colour"] + "/" + dt.Rows[i]["Marking"] + "",
                                 dt.Rows[i]["NetWt"],
                                 dt.Rows[i]["PartyName"],
                                 dt.Rows[i]["MarketingInvNo"],
                                 dt.Rows[i]["Remarks"], fgcode
                                );
                        }
                    }
                }
            }
        }

        private void label7_Click(object sender, EventArgs e)
        {
            DataTable dt = new DataTable();

            foreach (DataGridViewColumn c in dataGridView1.Columns)
            {
                if (c.HeaderText.ToLower() != "actualgpm" && c.HeaderText.ToLower() != "actualgsm" && c.HeaderText.ToLower() != "roll no")
                    dt.Columns.Add(c.HeaderText);
            }

            Export.ToFile(dt);
        }

        private void buttonDelete_Click(object sender, EventArgs e)
        {
            //27.04.2022 change for Delete functionality
            bool blntran = false;
            try
            {
                if (Database.OpenConnection(Utility.MaterialConnectionString))
                {
                    if (Validate())
                    {
                        Database.BeginTransaction();
                        blntran = true;
                        string rollno = Convert.ToString(dataGridView1.Rows[0].Cells[Column2.Index].Value);
                        double TotalUpNetwt = Utility.SafeConvertToDouble(Database.GetExecuteNonQueryCommand_retrun("select netwt from misneedleloomproductionentry where TRANSID="
                         + transid + " and Rollno='" + rollno + "'"));

                        Database.GetExecuteNonQueryCommand("delete from  Prod_RMD_InOut where vItemCode='" + rollno + "'");

                        Database.GetExecuteNonQueryCommand("UPDATE WareHouse SET StkInHand = StkInHand-" + TotalUpNetwt + " WHERE CompanyName='" + comboCompanyName.Text
                            + "' and WareHouseName='Roll Material Godown' and itemcode='" + dataGridView1.Rows[0].Cells[colFGCode.Index].Value + "'");

                        ProductionCommonFunction.InsertStockJournal(transid, dateLoomEntry.Value, comboCompanyName.Text, "Production",
                            Convert.ToString(dataGridView1[colFGCode.Index, 0].FormattedValue),
                                    -1 * Convert.ToDouble(dataGridView1["Column10", 0].FormattedValue), 0, 0, "Kgs", frmDefaultVale.Year,
                                    transid, transid, Convert.ToString(dataGridView1[colFGCode.Index, 0].Value), rollno, "NLoom");

                        //Database.ExecuteNonQuery("delete from stockJournalprod where srno = " + transid + " and company_name='" + comboCompanyName.Text
                        //+ "' and Rollno='" + rollno + "' and type='Production'");

                        Database.GetExecuteNonQueryCommand_retrun("insert into ConsumptionMaster (ItemName,Consumption,GroupNo,Date,PlantName,Companyname,Username,RateConsumption,RateProduction,RecordLogid,TapeWastage,CrimptYarnWastage,PPYarnWastage,YarnFibcWastage,LabelFibcWastage,WebbingTieFibcWastage,LinerFibcWastage,LineProcessFibcWastage,OtherFibcWastage,fiforate,liforate,latestrate,wghtrate,hodapprove,hodapproveDate,exciseapprove,exciseapproveDate,RG1,RG23)  Select top 1 ItemName,-1 * " + TotalUpNetwt + " as Consumption,GroupNo,Date,PlantName,Companyname,Username,RateConsumption,RateProduction,RecordLogid,TapeWastage,CrimptYarnWastage,PPYarnWastage,YarnFibcWastage,LabelFibcWastage,WebbingTieFibcWastage,LinerFibcWastage,LineProcessFibcWastage,OtherFibcWastage,fiforate,liforate,latestrate,wghtrate,hodapprove,hodapproveDate,exciseapprove,exciseapproveDate,RG1,RG23 from ConsumptionMaster where CompanyName='" + comboCompanyName.Text + "'  and date='" + dateLoomEntry.Value.ToString("yyyy-MM-dd") + "' and PlantName='Needle Loom' and ItemName='BOBIN'");
                        Database.GetExecuteNonQueryCommand_retrun("insert into Prod_ConsumptionMaster_Bifercation (dsysDate,vItemBiferacation,vItemCode,vItemName,fConsumption,fGroupNo,vPlantName,vCompanyName,vUserName,fRateConsumption,fRateProduction,RecordLogid,RG1,RG23) select top 1 dsysDate,vItemBiferacation,vItemCode,vItemName,-1 * " + TotalUpNetwt + " as fConsumption,fGroupNo,vPlantName,vCompanyName,vUserName,fRateConsumption,fRateProduction,RecordLogid,RG1,RG23 from Prod_ConsumptionMaster_Bifercation where vCompanyName='" + comboCompanyName.Text + "' and vPlantName='Needle Loom' and dsysDate='" + dateLoomEntry.Value.ToString("yyyy-MM-dd") + "' and vItemName='BOBIN'");

                        if (Database.GetExecuteNonQueryCommand("delete from misneedleloomproductionentry where transid = " + transid + " and rollno='" + rollno + "'"))
                            MessageBox.Show("Data deleted Suceesfully");

                        Database.CommitTransaction();
                        blntran = false;
                        Utility.UserInformation("Delete ", "Roll Entry " + transid + " is Delete for RollNo : " + dataGridView1[1, 0].Value);
                    }
                    BindGrid();
                    isclear = true;
                    dataGridView1.Rows.Clear();
                    isclear = false;
                }
                else
                {
                    MessageBox.Show("Server connection problem.\nPlease Click on Tools-->Refresh-->Refresh All"); //You can't Delete because it's already issue
                }

            }
            catch (Exception ex)
            {
                if (blntran)
                    Database.RollBackTransaction();
                ErrorMessageBox.Show(ex);
            }
        }

        private void btnok_Click(object sender, EventArgs e)
        {
            try
            {
                if (Database.OpenConnection_Adapter(Utility.MaterialConnectionString))
                {
                    Database.myadapter = Database.GetAdapterCommand("select * from misneedleloomproductionentry where  companyname = '"
                        + comboCompanyName.Text + "' and sysdate>='" + dtpSaveData.Value.ToString("yyyy-MM-dd") + "' order by transid desc");
                    DataSet dataset1 = new DataSet();
                    Database.myadapter.Fill(dataset1, "misneedleloomproductionentry");

                    BindingSource datasource = new BindingSource(dataset1.Tables[0], null);
                    dataGridView2.DataSource = datasource;

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Problem with Load Data : " + ex.Message);
            }
        }

    }
}









