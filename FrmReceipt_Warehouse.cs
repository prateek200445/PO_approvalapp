using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Data.SqlClient;
using System.Collections;

namespace ERP
{
    public partial class FrmReceipt_Warehouse : Form
    {
        DataTable dtApproval = new DataTable();
        DataTable dtEmail = new DataTable();
        private filter.filterData filterData;
        public System.Windows.Forms.StatusBarPanel pfrm_sbp1 = null;
        bool isBtnReject = false;
        public FrmReceipt_Warehouse()
        {
            InitializeComponent();
            Form_Load();
        }

        private void Form_Load()
        {

            filterData = new filter.filterData(dataGridView1);
            if (Database.OpenConnection(Utility.MaterialConnectionString))
            {
                ComboCmpny.DataSource = ProductionCommonFunction.GetCompanyList();
                ComboCmpny.DisplayMember = ProductionCommonVariables.FieldName.CompanyName;
                ComboCmpny.ValueMember = ProductionCommonVariables.FieldName.IndentCode;

                ComboCmpny.Text = frmDefaultVale.CompanyName;
                ComboCmpny.Enabled = false;
            }

            if (Database.OpenConnection(Utility.MaterialConnectionString))
            {
                if (FrmMain.IsProductionadmin)
                {
                    combowarehouse.DataSource = ProductionCommonFunction.GetWareHouseList(ComboCmpny.Text);
                    combowarehouse.ValueMember = ProductionCommonVariables.FieldName.SrNo;
                    combowarehouse.DisplayMember = ProductionCommonVariables.FieldName.WareHouse;

                    combo_change_warehouse.DataSource = ProductionCommonFunction.GetWareHouseList(ComboCmpny.Text);
                    combo_change_warehouse.ValueMember = ProductionCommonVariables.FieldName.SrNo;
                    combo_change_warehouse.DisplayMember = ProductionCommonVariables.FieldName.WareHouse;

                }
                else
                {
                    combowarehouse.DataSource = ProductionCommonFunction.GetWareHouse_userWise();
                    combowarehouse.ValueMember = ProductionCommonVariables.FieldName.SrNo;
                    combowarehouse.DisplayMember = ProductionCommonVariables.FieldName.WareHouse;

                    combo_change_warehouse.DataSource = ProductionCommonFunction.GetWareHouse_userWise();
                    combo_change_warehouse.ValueMember = ProductionCommonVariables.FieldName.SrNo;
                    combo_change_warehouse.DisplayMember = ProductionCommonVariables.FieldName.WareHouse;

                }
                if (combowarehouse.Items.Count > 0)
                    combowarehouse.SelectedIndex = 0;
                //     dataGridView1.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(229, 218, 251);

            }


            // This code Add by SunilSingh 24-08-15................
            if (FrmMain.IsAdmin || FrmMain.IsPaymentApprove)
            {
                btn_update_warehouse.Enabled = true;
                button2.Enabled = true;
                button2.Visible = true;
            }
            else
            {
                button2.Enabled = false;
                button2.Visible = false;
            }

        }


        private void BindGrid()
        {
            try
            {

                if (Database.OpenConnection_Adapter(Utility.MaterialConnectionString))
                {
                    string commandstring = "";
                    if (combowarehouse.Text == "Wastage Godown")
                    {
                        //status='PlantConsumption' case when tapeplantconsumption.companyname ='Plastene India Limited ' then Convert(Decimal(12,2),isnull(sum(wastage),0))  else Convert(Decimal(12,2),isnull(sum(wastage),0))+Convert(Decimal(12,2),isnull(sum( fTrimWastage ),0)) end ,case when tapeplantconsumption.companyname ='Plastene India Limited ' then Convert(Decimal(12,2),isnull(sum(wastage),0))  else isnull(sum(wastage),0)+Convert(Decimal(12,2),isnull(sum( fTrimWastage ),0)) end 
                        commandstring = "  union " +
                        " select i.itemcode,i.itemname,tapeplantconsumption.Plantname,'' as GroupName,CAST(FLOOR(CAST(sysdate AS float)) AS datetime),isnull( sum(ISNULL(wastage,0)+case when tapeplantconsumption.Plantname='Lamination' then ISNULL(fTrimWastage,0) else 0 end+ISNULL(fSweepingWastage,0)+case when tapeplantconsumption.Plantname='Tape Plant' then ISNULL(fTapeLumpsWastage,0)  else 0 end+ISNULL(fTapeOthersWastage,0) + ISNULL(fFabricTrim,0) + ISNULL(fFabricwaste,0) +ISNULL(fNewLumpsWs ,0)),0),isnull( sum(ISNULL(wastage,0)+case when tapeplantconsumption.Plantname='Lamination' then ISNULL(fTrimWastage,0) else 0 end+ISNULL(fSweepingWastage,0)+case when tapeplantconsumption.Plantname='Tape Plant' then ISNULL(fTapeLumpsWastage,0)  else 0 end+ISNULL(fTapeOthersWastage,0) + ISNULL(fFabricTrim,0) + ISNULL(fFabricwaste,0) +ISNULL(fNewLumpsWs ,0)),0) as 'Accepted Qty',  0  as 'Rejected Qty',0,GroupSrno,tapeplantconsumption.Plantname , '' as 'Return',tapeplantconsumption.fiforate,tapeplantconsumption.liforate,tapeplantconsumption.latestrate,tapeplantconsumption.wghtrate from " +
                        " tapeplantconsumption with (nolock) inner join plantmaster on " +
                        " plantmaster.plantname =tapeplantconsumption.plantname and plantmaster.companyname =tapeplantconsumption.companyname " +
                        " inner join itemcodes i with (nolock) on  i.itemcode = plantmaster.wastageitemcode " +
                        " where tapeplantconsumption.companyname = '"
                        + ComboCmpny.Text + "' and groupsrno not in (select transid from warehouseinwards with (nolock) where companyname = '"
                        + ComboCmpny.Text + "'      ) and   tapeplantconsumption.sysDate >= '01-Jan-2015' and tapeplantconsumption.PlantName not in ( 'cutting') " +
                        " group by tapeplantconsumption.companyname,i.itemcode,i.itemname,tapeplantconsumption.Plantname,GroupSrno,CAST(FLOOR(CAST(sysdate AS float)) AS datetime),tapeplantconsumption.fiforate,tapeplantconsumption.liforate,tapeplantconsumption.latestrate,tapeplantconsumption.wghtrate  having isnull( sum(ISNULL(wastage,0)+ISNULL(fTrimWastage,0)+ISNULL(fSweepingWastage,0)+ISNULL(fTapeLumpsWastage,0)+ISNULL(fTapeOthersWastage,0) + ISNULL(fFabricTrim,0) + ISNULL(fFabricwaste,0) +ISNULL(fNewLumpsWs ,0)),0)>0";
                        // code comment by manish 2nd August 2022
                        //"  union " +
                        //" select i.itemcode,i.itemname,ConsumptionMaster.Plantname,'' as GroupName,CAST(FLOOR(CAST(date AS float)) AS datetime),Convert(Decimal(12,2),isnull(sum(consumption),0)),isnull(sum(consumption),0) as 'Accepted Qty',  0  as 'Rejected Qty',0,GroupNo,'Consumption' , '' as 'Return',ConsumptionMaster.fiforate,ConsumptionMaster.liforate,ConsumptionMaster.latestrate,ConsumptionMaster.wghtrate from " +
                        //" ConsumptionMaster with (nolock) inner join plantmaster on " +
                        //" plantmaster.plantname =ConsumptionMaster.plantname and plantmaster.companyname =ConsumptionMaster.companyname " +
                        //" inner join itemcodes i with (nolock) on  i.itemcode = plantmaster.wastageitemcode " +
                        //" where ConsumptionMaster.companyname = '"
                        //+ ComboCmpny.Text + "' and groupno not in (select transid from warehouseinwards with (nolock) where companyname = '"
                        //+ ComboCmpny.Text + "' and consumption>0 and status='Consumption') and ConsumptionMaster.itemname='Wastage' " +
                        //" group by i.itemcode,i.itemname,ConsumptionMaster.Plantname,Groupno,CAST(FLOOR(CAST(date AS float)) AS datetime) ,ConsumptionMaster.fiforate,ConsumptionMaster.liforate,ConsumptionMaster.latestrate,ConsumptionMaster.wghtrate having isnull(sum(consumption),0)>0 ";//+
                       // code comment by manish 29th July 2022
                        //"union " +
                        //"select i.itemcode,i.itemname,'Liner Bag Godown',i.GroupName as GroupName,CAST(FLOOR(CAST(dSysDate AS float)) AS datetime),Convert(Decimal(12,2),isnull(sum(fWastage),0)),isnull(sum(fWastage),0) as 'Accepted Qty',  0  as 'Rejected Qty',0,iGroupNo,'Liner Bag Godown' , '' as 'Return',0 fiforate,0 liforate,0 latestrate,0  wghtrate " +
                        //"from  Prod_StichPrintEntry ConsumptionMaster with (nolock) inner join plantmaster on plantmaster.plantname ='Liner Roll Plant' inner join itemcodes i with (nolock) on i.itemcode = plantmaster.wastageitemcode and   vCompanyName=plantmaster.companyname  " +
                        //" where vCompanyName = '" + ComboCmpny.Text + "' and iGroupNo not in (select transid from warehouseinwards with (nolock) where companyname = '" + ComboCmpny.Text + "' and fWastage>0 and status='Liner Bag Godown') " +
                        //"and dSysDate>='2020-04-01' group by i.itemcode,i.itemname,i.GroupName,dSysDate,iGroupNo having sum(fWastage)>0 ";
                          // end code comment by manish 29th July 2022
                    }
                    commandstring += "Order by sysdate";

                    string strsqlOEL3 = "select TapepackingEntry.itemcode,TapepackingEntry.itemname,'' Plantname,item.GroupName,TapepackingEntry.Sysdate, TapepackingEntry.Weight Qty,TapepackingEntry.Weight as 'Accepted Qty',  0  as 'Rejected Qty',0,TapepackingEntry.GroupID,'Despatch from ' + TapepackingEntry.Companyname , '' as 'Return' ,0 fiforate,0  liforate, 0 latestrate,0  wghtrate  " +
                           " from TapepackingEntry inner Join Item on item.ItemCode=TapepackingEntry.ItemCode and TapepackingEntry.CompanyName=item.CompanyName " +
                           "where PartyName='" + frmDefaultVale.CompanyName + "' and   TapepackingEntry.Weight>0 AND TapepackingEntry.GroupID NOT IN ( SELECT InwardNo FROM warehouseinwards)";
                    Database.myadapter = Database.GetAdapterCommand("select so.Itemcode,so.Itemname,so.Deptt,isnull(w.GroupName, SO.subDept ) AS GroupName,sysdate'Date',(case when isnull(so.QtyInKgs,0)>0 then isnull(so.QtyInKgs,0) else so.Qty end) as Qty,(case when isnull(so.QtyInKgs,0)>0 then isnull(so.QtyInKgs,0) else so.Qty end) as 'Accepted Qty',convert(decimal,0) as 'Rejected Qty',SO.Rate,so.transid,'StoreOutward' As status,isnull(So.MRNSsrno ,'') as 'Return',so.fiforate,so.liforate,so.latestrate,so.wghtrate " +
                     " from storeoutwards so with (nolock) left join warehouseinwards WI on wi.transid=so.transid and wi.ToWareHouse=so.warehouse and wi.CompanyName=so.CompName and wi.status='StoreOutward' " +
                     " left join warehouse w on w.companyName=so.CompName and w.ItemCode=so.Itemcode and w.WareHouseName=so.WareHouse and so.Itemcode=wi.Itemcode and wi.CompanyName=so.CompName " +
                     "   where warehouse ='" + combowarehouse.Text
                     + "' and so.Qty>0 and (compname='" + ComboCmpny.Text + "'  or  '" + combowarehouse.Text + "' ='OEL3 Godown' or '" + combowarehouse.Text + "' ='OEL3 sub store') and WI.transid is null  " +
                     " Union all " +
                     " select so.Itemcode,so.Itemname,so.FromWareHouse,isnull(w.GroupName ,'')  AS GroupName,so.sysdate'Date', (case when isnull(WI.QtyInKgs,0)>0 then isnull(wi.QtyInKgs,0) else so.Qty end) as  Qty,(case when isnull(WI.QtyInKgs,0)>0 then isnull(wi.QtyInKgs,0) else so.Qty end) as 'Accepted Qty',convert(decimal,0) as 'Rejected Qty',0,so.transid,so.ToWareHouse, '' as 'Return',so.fiforate,so.liforate,so.latestrate,so.wghtrate " +
                     " from warehousetowarehouse so with (nolock) left join warehouseinwards WI on wi.transid=so.transid and wi.ToWareHouse=so.ToWareHouse and wi.CompanyName=so.CompanyName and wi.status<>'StoreOutward' " +
                     " left join warehouse w on w.companyName=so.companyName and w.ItemCode=so.Itemcode and w.WareHouseName=so.ToWarehouse  where so.companyname='" + ComboCmpny.Text
                     + "' and so.Qty>0 and so.Towarehouse='" + combowarehouse.Text + "' and InternalReq ='" + comboInternalReq.Text + "' and Wi.transid  is null and so.istransfer is null  union all" +
                     " select   vItemCode,   m.itemname  AS ITEMNAME, 'Cut Pices' as GroupName," +
                     " 'RawMaterial',dSysdate,fTransferNetWt as Qty,fTransferNetWt as AcceptedQty,0 as RejectedQty,0 as Rate, iSrNo,vFromGodown, '' as 'Return',Prod_Cutting_InOut.fiforate,Prod_Cutting_InOut.liforate,Prod_Cutting_InOut.latestrate,Prod_Cutting_InOut.wghtrate from Prod_Cutting_InOut left join item m on m.ItemCode =Prod_Cutting_InOut.vItemCode and m.CompanyName=Prod_Cutting_InOut.vCompanyName left join warehouseinwards WI on wi.transid=Prod_Cutting_InOut.iSrNo and wi.ToWareHouse=Prod_Cutting_InOut.vToGodown  where  vStatus='Pending' and vCompanyName='"
                     + ComboCmpny.Text + "' and vtOGodown='" + combowarehouse.Text + "'  and Wi.transid  is null " +
                     (combowarehouse.Text == "OEL3 Godown" ? " union all " + strsqlOEL3 : "") + " " + commandstring + "");

                    DataSet dataset1 = new DataSet();
                    Database.myadapter.Fill(dataset1);
                    BindingSource datasource = new BindingSource(dataset1.Tables[0], null);
                    dataGridView1.DataSource = datasource;

                    for (int i = 1; i < dataGridView1.Columns.Count; i++)
                    {
                        if (dataGridView1.Columns[i].HeaderText == "Accepted Qty")
                            dataGridView1.Columns[i].ReadOnly = false;
                        else
                            dataGridView1.Columns[i].ReadOnly = true;


                    }
                    //Only productoin admin can see the rates
                    //so.fiforate,so.liforate,so.latestrate,so.wghtrate
                    dataGridView1.Columns["fiforate"].Visible = FrmMain.IsProductionadmin;
                    dataGridView1.Columns["liforate"].Visible = FrmMain.IsProductionadmin;
                    dataGridView1.Columns["latestrate"].Visible = FrmMain.IsProductionadmin;
                    dataGridView1.Columns["wghtrate"].Visible = FrmMain.IsProductionadmin;



                    //Below code is for Check box is selected when laod
                    //for (int i = 1; i < dataGridView1.Rows.Count; i++)
                    //{

                    //        dataGridView1[0,i].Value= true;
                    //}
                    if (dataset1.Tables[0].Rows.Count > 0)
                        Tools.ToolsCommonFunction.setRightAignmentTodategridview(dataGridView1);
                }
            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (combowarehouse.Text.Length > 0)
                BindGrid();
            else
                MessageBox.Show("Please select proper warehouse name.");
        }

        public bool validations()
        {
            if (combowarehouse.Text == "Store Department")
                return true;
            try
            {
                if (Database.OpenConnection(Utility.MaterialConnectionString))
                {
                    ArrayList itemcode = new ArrayList();
                    string itemcodeWIP = "";
                    string itemNameWip = "";
                    if (combowarehouse.Text == "FIBC Bag Godown")
                    {
                        itemcodeWIP = (string)Database.GetExecuteNonQueryCommand_retrun("select top 1 ItemCode from item where todate is null and CompanyName='" + ComboCmpny.Text + "' and itemname='FIBC WIP' order by fromdate desc ");
                        if (itemcodeWIP.Length == 0)
                        {
                            MessageBox.Show("No Item Code is created for FIBC WIP.\nPlease ask Team to create item code for FIBC WIP");
                            return false;
                        }
                    }
                    if (combowarehouse.Text == "Small Bag Godown")
                    {
                        itemcodeWIP = (string)Database.GetExecuteNonQueryCommand_retrun("select top 1 ItemCode from item where  todate is null and CompanyName='" + ComboCmpny.Text + "' and itemname='Small Bags WIP' order by fromdate desc ");
                        if (itemcodeWIP.Length == 0)
                        {
                            MessageBox.Show("No Item Code is created for Small Bag WIP.\nPlease ask Team to create item code for Small Bag WIP");
                            return false;
                        }
                    }

                    for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                    {
                        if (dataGridView1.Rows[i].Cells[0].FormattedValue.ToString() == "True" && dataGridView1.Rows[i].Cells[2].FormattedValue.ToString() != "")
                        {
                            Database.myreader = Database.GetExecuteReaderCommand("select itemcode from warehouse where itemcode = '" + dataGridView1.Rows[i].Cells[1].FormattedValue.ToString() + "' and companyname = '"
                            + ComboCmpny.Text + "' and warehousename = '" + combowarehouse.Text + "'");
                            DataTable dt1 = new DataTable();
                            dt1.Load(Database.myreader);
                            if (dt1.Rows.Count <= 0)
                            {
                                if (!itemcode.Contains(dataGridView1.Rows[i].Cells[1].FormattedValue.ToString()))
                                    itemcode.Add(dataGridView1.Rows[i].Cells[1].FormattedValue.ToString());
                            }
                            Database.myreader.Close();
                        }

                    }
                    if (itemcode.Count > 0)
                    {
                        if (combowarehouse.Text == "FIBC Bag Godown" || combowarehouse.Text == "Small Bag Godown")
                        {
                            itemcode.Add(itemcodeWIP);
                        }

                        for (int k = 0; k < itemcode.Count; k++)
                        {
                            DialogResult drs = MessageBox.Show("Please transfer following itemcode in warehouse,You can't accept item, itemcode are:- " + itemcode[k] + ",Do you want to transfer?"
                             , "Roll Entry", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                            if (drs.CompareTo(DialogResult.Yes) == 0)
                            {
                                string DtlSubGroupName = "";
                                Database.myreader = Database.GetExecuteReaderCommand("select ItemCode,ItemName,GroupName,Deptt,Minlevel,Maxlevel,ReOrder,Unit,StkInHand,TypeVendor,NameVendor,"
                                + " EmailVendor,QtyOrder,MnthlyConsum,Rate,CompanyName,LoginName,Dummy1,OpeningStock,SubGroupName,DtlSubGroupName from item where"
                                + " ItemCode='" + itemcode[k] + "' and CompanyName='" + ComboCmpny.Text + "'");
                                DataTable dt = new DataTable();
                                dt.Load(Database.myreader);
                                if (dt.Rows.Count > 0)
                                {
                                    string InsertString = "";
                                    for (int j = 0; j <= 19; j++)
                                    {
                                        if (j == 4)
                                            InsertString += "'" + Utility.ConverttoInt(Convert.ToString(dt.Rows[0][j])) + "',";
                                        else if (j == 5)
                                            InsertString += "'" + Utility.ConverttoInt(Convert.ToString(dt.Rows[0][j])) + "',";
                                        else if (j == 6)
                                            InsertString += "'" + Utility.ConverttoInt(Convert.ToString(dt.Rows[0][j])) + "',";
                                        else if (j == 8)
                                            InsertString += "'" + Utility.ConverttoInt(Convert.ToString(dt.Rows[0][j])) + "',";
                                        else if (j == 12)
                                            InsertString += "'" + Utility.ConverttoInt(Convert.ToString(dt.Rows[0][j])) + "',";
                                        else if (j == 13)
                                            InsertString += "'" + Utility.ConverttoInt(Convert.ToString(dt.Rows[0][j])) + "',";
                                        else if (j == 14)
                                            InsertString += "'" + Utility.ConverttoInt(Convert.ToString(dt.Rows[0][j])) + "',";
                                        else if (j == 18)
                                            InsertString += "'" + Utility.ConverttoInt(Convert.ToString(dt.Rows[0][j])) + "',";
                                        else
                                            InsertString += "'" + Convert.ToString(dt.Rows[0][j]) + "',";
                                    }
                                    DtlSubGroupName = Convert.ToString(dt.Rows[0]["DtlSubGroupName"]);
                                    Database.GetExecuteNonQueryCommand("insert into warehouse values(" + InsertString + "'" + combowarehouse.Text + "','" + DtlSubGroupName + "')");
                                    Database.GetExecuteNonQueryCommand("update  warehouse set stkinhand = 0 where itemcode = '" + Convert.ToString(itemcode[k]) + "' and warehousename = '"
                                    + combowarehouse.Text + "' and companyname = '" + ComboCmpny.Text + "'");

                                    //string itemname = ProductionCommonFunction.GetItemName(Convert.ToString(itemcode[k]));
                                    //ProductionCommonFunction.WareHouseStoreinwards(Convert.ToString(itemcode[k]), itemname,
                                    //          0, DateTime.Now, ComboCmpny.Text, combowarehouse.Text, "");

                                }

                            }
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

        void save()
        {
            try
            {
                if (validations())
                {
                    string status;
                    bool isentry = false;
                    if (isBtnReject)
                    {
                        if (MessageBox.Show("Do you want to Reject all the quantity for selected material' ?"
                         , "Receipt Entry", MessageBoxButtons.YesNo, MessageBoxIcon.Information) != System.Windows.Forms.DialogResult.Yes)
                            return;
                        else
                            status = "Rejected";
                    }
                    else
                    {
                        if (MessageBox.Show("Do you want to Accept the quantity for selected material' ?"
                        , "Receipt Entry", MessageBoxButtons.YesNo, MessageBoxIcon.Information) != System.Windows.Forms.DialogResult.Yes)
                            return;
                        else
                            status = "Approved";
                    }
                    if (Database.OpenConnection(Utility.MaterialConnectionString))
                    {
                        try
                        {
                            string itemcodewastg = "";
                            string itemWasteName = "";

                            if (combowarehouse.Text == "Wastage Godown")
                            {

                                itemcodewastg = (string)Database.GetExecuteNonQueryCommand_retrun("SELECT wastageItemCode FROM PlantMaster WHERE companyname='" + ComboCmpny.Text + "' and fromwarehouse='" + combowarehouse.Text + "'");
                                if ((object)itemcodewastg == null)
                                    itemcodewastg = (string)Database.GetExecuteNonQueryCommand_retrun("select top 1 ItemCode from item where MISCATEGORY='SF' and MISGRUPITEM in ('Fabric waste','TAPE WASTE','Wastage/Scrap') and todate is null and CompanyName='" + ComboCmpny.Text + "' order by fromdate desc ");
                                itemcodewastg = Convert.ToString(itemcodewastg);
                                itemWasteName = (string)Database.GetExecuteNonQueryCommand_retrun("select top 1 itemname from item where MISCATEGORY='SF' and MISGRUPITEM in ('Fabric waste','TAPE WASTE','Wastage/Scrap') and todate is null and CompanyName='"
                                    + ComboCmpny.Text + "' and itemcode='" + itemcodewastg + "' order by fromdate desc ");
                            }

                            if (combowarehouse.Text == "FIBC Bag Godown")
                            {
                                itemcodewastg = (string)Database.GetExecuteNonQueryCommand_retrun("select top 1 ItemCode from item where todate is null and CompanyName='" + ComboCmpny.Text + "' and itemname='FIBC WIP' order by fromdate desc ");
                                itemWasteName = "FIBC WIP";

                            }
                            if (combowarehouse.Text == "Small Bag Godown")
                            {
                                itemcodewastg = (string)Database.GetExecuteNonQueryCommand_retrun("select top 1 ItemCode from item where  todate is null and CompanyName='" + ComboCmpny.Text + "' and itemname='Small Bags WIP' order by fromdate desc ");
                                itemWasteName = "Small Bags WIP";
                            }
                            Database.BeginTransaction();
                            //To generate MRN automatically for TFO inwards 
                            int StoreInwardsIndex = 0;
                            string Prefix = "";
                            string value = ComboCmpny.SelectedValue.ToString();

                            if (combowarehouse.Text == "Store Department")
                            {
                                #region Stop Inserting StoreinwaredPayment Entry only StkInHand will be updated when transfer 04.01.2022
                                //Prefix = (string)Database.ExecuteScalar("select isnull(max(srno),'') as srno from StoreInwardspayment where companyname = '"
                                //                + ComboCmpny.Text + "' and yr = '" + frmDefaultVale.Year + "' ");
                                //StoreInwardsIndex = (int)Database.ExecuteScalar("select Max(isnull(SIIndex,0)) from StoreInwards ");
                                //if (Prefix != "")
                                //{
                                //    Prefix = Prefix.Substring(Prefix.LastIndexOf("/") + 1);
                                //    Utility.IncreaseValueByone(ref Prefix);
                                //    Prefix = value + "/" + frmDefaultVale.Year + "/" + Prefix;
                                //}
                                //else
                                //    Prefix = value + "/" + frmDefaultVale.Year + "/" + "SII00001";

                                //StoreInwardsIndex = StoreInwardsIndex + 1;

                                //Database.GetExecuteNonQueryCommand(" insert into  storeinwardspayment (SrNo,PONo,PartyName,Deptt,sysdate,MRNo,CompanyName,LoginName,Yr, SupplierName,ReportType, "
                                //    + "InwardVatType,CombinePONo, Category ,SubCategory ,Categoryseries)   values "
                                //    + " ('" + Prefix + "','No PO','Production Entry','Production',dateadd(d,-5,getdate()),'No MRN','" + ComboCmpny.Text + "','" + FrmMain.UserName + "','" + frmDefaultVale.Year + "','Production Entry','GENERAL',"
                                //    + " 'VAT','No PO,','GENERAL' ,'CONSUMABLES','GNCN')");
                                #endregion
                            }
                            bool isReturn = false;

                            for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                            {
                                if (dataGridView1.Rows[i].Cells[0].FormattedValue.ToString() == "True" && dataGridView1.Rows[i].Cells[2].FormattedValue.ToString() != "")
                                {
                                    //double amount = 0;
                                    double rate = 0;
                                    double qty = 0;
                                    double rejQty = 0;

                                    if (!isBtnReject)
                                    {
                                        rate = Convert.ToDouble(dataGridView1["Rate", i].Value.ToString());
                                        qty = Convert.ToDouble(dataGridView1["Accepted Qty", i].Value.ToString());
                                        rejQty = Convert.ToDouble(dataGridView1["Rejected Qty", i].Value.ToString());
                                    }
                                    else
                                    {

                                        rate = Convert.ToDouble(dataGridView1["Rate", i].Value.ToString());
                                        qty = 0;
                                        rejQty = Convert.ToDouble(dataGridView1["Qty", i].Value.ToString());
                                    }
                                    string strSalesReturn = dataGridView1.Rows[i].Cells["Return"].FormattedValue.ToString();

                                    if (combowarehouse.Text == "FIBC Bag Godown" && strSalesReturn.Length > 0)
                                    {
                                        isReturn = true;
                                        continue;
                                    }
                                    #region Disucssion with team now onwards any entry received in WASTAGE DOWN WILL BE ADDED IN WASTAGE
                                    string itemcode = dataGridView1["ItemCode", i].Value.ToString();
                                    string itemName = dataGridView1["ItemName", i].Value.ToString();
                                    string remrks = "";
                                    if (combowarehouse.Text == "Wastage Godown")
                                    {
                                        itemName = itemWasteName;
                                        itemcode = itemcodewastg;
                                        remrks = "Item Code=" + dataGridView1["ItemCode", i].Value.ToString() + " Merge with " + itemcode;
                                    }
                                    #endregion
                                    #region Disucssion with team now onwards any entry received in FIBC WIP and SMALL BAG WIP
                                    string deptt = AccountCommonFunction.GetDeptt(itemcode); //04-Oct-2021
                                    if (combowarehouse.Text == "FIBC Bag Godown" || combowarehouse.Text == "Small Bag Godown")
                                    {
                                        if (deptt == "SF")
                                        {
                                            itemName = itemWasteName;
                                            itemcode = itemcodewastg;
                                            remrks = "Item Code=" + dataGridView1["ItemCode", i].Value.ToString() + " Merge with " + itemcode;
                                        }
                                    }
                                    #endregion
                                    //Database.GetExecuteNonQueryCommand("Insert into warehouseinwards values " +
                                    //      " ('" + ComboCmpny.Text + "','" + dataGridView1["Deptt", i].FormattedValue.ToString() + "','" + combowarehouse.Text + "','" + dataGridView1["ItemCode", i].FormattedValue.ToString() + "', " +
                                    //      "  '" + dataGridView1["ItemName", i].FormattedValue.ToString() + "','" + qty + "','" + ((DateTime)dataGridView1["Date", i].FormattedValue).ToString("dd-MMM-yyyy") + "', " +
                                    //      "  '" + FrmMain.UserName + "','','" +  dataGridView1["transid", i].FormattedValue.ToString()  + "','','" + frmDefaultVale.Year + "', " +
                                    //      "  '" + rate + "','" + (qty * rate) + "','','" + dataGridView1["Status", i].FormattedValue.ToString() + "','" + rejQty + "')");

                                    string intOELGrpid = "";

                                    if (combowarehouse.Text == "OEL3 Godown")
                                        intOELGrpid = Convert.ToString(dataGridView1["transid", i].FormattedValue);


                                    object intRec = (object)Database.GetExecuteNonQueryCommand_retrun("Insert into warehouseinwards (CompanyName,Deptt,ToWareHouse,Itemcode,ItemName,Qty,InwardDate,Username,Remarks,transid,MrnNo,yr,Rate,Amount,InwardNo,status,RejectedQty,bisApprove,vApproveBy,dApproveDate,fiforate,liforate,latestrate,wghtrate,internalreqno) values " +
                                      " ('" + ComboCmpny.Text + "','" + dataGridView1["Deptt", i].Value.ToString() + "','" + combowarehouse.Text + "','" + itemcode + "', " +
                                      "  '" + itemName + "','" + qty + "','" + ((DateTime)dataGridView1["Date", i].Value).ToString("dd-MMM-yyyy") + "', " +
                                      "  '" + FrmMain.UserName + "','" + remrks + "','" + Convert.ToInt32(dataGridView1["transid", i].Value.ToString()) + "','','" + frmDefaultVale.Year + "', " +
                                      "  '" + rate + "','" + (qty * rate) + "','" + intOELGrpid + "','" + dataGridView1["Status", i].Value.ToString() + "','" + rejQty + "','true','" + FrmMain.UserName + "',getdate()"
                                      + "," + Utility.SafeConvertToDouble(dataGridView1["Fiforate", i].Value.ToString()) + "," + Utility.SafeConvertToDouble(dataGridView1["liforate", i].Value.ToString())
                                      + "," + Utility.SafeConvertToDouble(dataGridView1["latestrate", i].Value.ToString()) + "," + Utility.SafeConvertToDouble(dataGridView1["wghtrate", i].Value.ToString()) + ",'" + comboInternalReq.Text + "'); Select @@IDENTITY");

                                    Database.GetExecuteNonQueryCommand("update warehousetowarehouse set istransfer = 'yes' where itemcode = '" + dataGridView1["ItemCode", i].Value.ToString() + "' and companyname = '"
                                       + ComboCmpny.Text + "' and ToWareHouse = '" + combowarehouse.Text + "' and transid=" + (intRec));

                                    if (combowarehouse.Text == "Wastage Godown")
                                    {
                                        Database.GetExecuteNonQueryCommand("update warehouse set stkinhand = stkinhand + " + qty
                                            + " where itemcode = '" + itemcode + "' and companyname = '"
                                            + ComboCmpny.Text + "' and warehousename = '" + combowarehouse.Text + "'");

                                        ProductionCommonFunction.WareHouseStoreinwards(itemcode, itemWasteName,
                                                  qty, ((DateTime)dataGridView1["Date", i].Value), ComboCmpny.Text, combowarehouse.Text, "");

                                    }
                                    else
                                    {
                                        if (combowarehouse.Text == "FIBC Bag Godown" || combowarehouse.Text == "Small Bag Godown")
                                        {
                                            Database.GetExecuteNonQueryCommand("update warehouse set stkinhand = stkinhand + " + qty
                                               + " where itemcode = '" + itemcode + "' and companyname = '"
                                               + ComboCmpny.Text + "' and warehousename = '" + combowarehouse.Text + "'");

                                            ProductionCommonFunction.WareHouseStoreinwards(itemcode, itemName,
                                                      qty, ((DateTime)dataGridView1["Date", i].Value), ComboCmpny.Text, combowarehouse.Text, "");

                                        }
                                        else
                                        {
                                            if (combowarehouse.Text == "Store Department")
                                            {
                                                #region Stop Inserting StoreinwaredPayment Entry only StkInHand will be updated when transfer 04.01.2022
                                                //Database.GetExecuteNonQueryCommand(" insert into storeinwards (SrNo,PONo,SysDate,ItemName,ItemCode,ChallanQty,RecdQty,Unit,SIIndex,IndentSubCode,LoginName ,IssuedQty,AcceptedQty,companyname ) values ( "
                                                //                            + " '" + Prefix + "','No PO',dateadd(d,-5,getdate()),'" + dataGridView1["ItemName", i].FormattedValue.ToString()
                                                //                            + "','" + dataGridView1["ItemCode", i].FormattedValue.ToString() + "'," + qty + "," + qty + ",'KGS'," + StoreInwardsIndex
                                                //                               + ",'" + value + "','" + FrmMain.UserName + "',0," + qty + ",'" + ComboCmpny.Text + "' )");
                                                #endregion
                                                Database.ExecuteNonQuery("update item set StkInHand = isnull(stkinhand,0)+" + Math.Round(Convert.ToDouble(qty), 2) + ""
                                                 + " where CompanyName='" + ComboCmpny.Text + "' and itemcode= '" + dataGridView1["ItemCode", i].FormattedValue.ToString() + "' ");
                                            }
                                            else
                                            {
                                                Database.GetExecuteNonQueryCommand("update warehouse set stkinhand = stkinhand + " + qty
                                                    + " where itemcode = '" + dataGridView1["ItemCode", i].Value.ToString() + "' and companyname = '"
                                                    + ComboCmpny.Text + "' and warehousename = '" + combowarehouse.Text + "'");
                                               
                                            }
                                            string itemname = ProductionCommonFunction.GetItemName(dataGridView1["ItemCode", i].Value.ToString());
                                            ProductionCommonFunction.WareHouseStoreinwards(dataGridView1["ItemCode", i].Value.ToString(), itemname,
                                                      qty, ((DateTime)dataGridView1["Date", i].Value), ComboCmpny.Text, combowarehouse.Text, "");
                                        }


                                    }




                                    if ((dataGridView1["Deptt", i].Value.ToString().ToUpper() == "Cut Pices".ToString().ToUpper()) && (isBtnReject == false) && (combowarehouse.Text == "Cutting Godown"))
                                    {
                                        string strsql = "";
                                        strsql = "Insert into CuttingEntry (BuyerName, PONO, MarketingOrdNo, RollNo, Size, ReqGSM, Color, Netwt, OrderQty, Cutsize, RollSize, CutPcs, BalCutPcs, WastageInKG, Remarks, sysdate, companyname, warehouse, year, Shift, FinCutPcs, FinCutSize, FinWastage, FinNetwt, CutRollNo, RollType, UseFor) select  BuyerName, PONO, MarketingOrdNo, RollNo, Size, ReqGSM, Color, Netwt, OrderQty, Cutsize, RollSize, CutPcs, BalCutPcs, WastageInKG, Remarks, '" + DateTime.Now.ToString("yyyy-MM-dd") + "', companyname, '" + combowarehouse.Text + "', year, Shift, FinCutPcs, FinCutSize, FinWastage, FinNetwt, CutRollNo, RollType, UseFor from CuttingEntry where transid in ( SELECT Ctransid FROM Prod_Cutting_InOut WHERE ISRNO=" + Convert.ToInt32(dataGridView1["transid", i].Value.ToString()) + ")";
                                        Database.ExecuteNonQuery(strsql);

                                        Database.GetExecuteNonQueryCommand("update warehouse set stkinhand = stkinhand - " + qty
                                        + " where itemcode = '" + dataGridView1["ItemCode", i].Value.ToString() + "' and companyname = '"
                                        + ComboCmpny.Text + "' and warehousename = '" + dataGridView1["Status", i].Value.ToString().ToUpper() + "'");

                                        string itemname = ProductionCommonFunction.GetItemName(dataGridView1["ItemCode", i].Value.ToString());
                                        ProductionCommonFunction.WareHouseStoreoutwards(dataGridView1["ItemCode", i].Value.ToString(), itemname,
                                                  qty, ((DateTime)dataGridView1["Date", i].Value), ComboCmpny.Text, dataGridView1["Status", i].Value.ToString().ToUpper(), "");


                                    }

                                    if (dataGridView1["Status", i].Value.ToString().ToUpper() == "Cutting Godown" || dataGridView1["Status", i].Value.ToString().ToUpper() == "Small Bag Godown")
                                    {
                                        Database.ExecuteNonQuery("UPDATE Prod_Cutting_InOut SET vStatus='" + status + "' where vCompanyName='" + ComboCmpny.Text + "' and isrno= " + Convert.ToInt32(dataGridView1["transid", i].Value.ToString()) + " ");
                                    }
                                    
                                    isentry = true;
                                }
                            }
                            if (isentry)
                            {
                                Database.CommitTransaction();
                                Utility.UserInformation("Save ", "Reciept is Done on " + DateTime.Today + " and Receipt By : " + FrmMain.UserName + "");

                                if (isReturn)
                                    saveReturnBail();

                                MessageBox.Show("Selected Items Accepted SucessFully");
                            }
                            else
                            {

                                if (isReturn)
                                {
                                    saveReturnBail();
                                    MessageBox.Show("Selected Items Accepted SucessFully");
                                }
                            }
                        }
                        catch (Exception)
                        {
                            Database.RollBackTransaction();
                        }
                        if (combowarehouse.Text == "Wastage Godown")
                            Check();

                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
            }
            finally
            {
                BindGrid();
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            isBtnReject = false;
            save();
        }

        private void Check()
        {
            try
            {

                double actualQty;
                double AcceptedQty;
                double CheckQty;
                double RejectedQty;
                double percentage;
                dtApproval.Rows.Clear();
                dtApproval.Columns.Clear();
                dtApproval.Columns.Add("transId", typeof(int));
                dtApproval.Columns.Add("Date", typeof(string));
                dtApproval.Columns.Add("ItemName", typeof(string));
                dtApproval.Columns.Add("Department", typeof(string));
                dtApproval.Columns.Add("Qty", typeof(string));
                dtApproval.Columns.Add("Accepted Qty", typeof(string));
                dtApproval.Columns.Add("Diff. Qty", typeof(string));
                dtApproval.Columns.Add("%", typeof(string));
                dtApproval.Columns.Add("ItemCode", typeof(string));


                for (int i = 0; i < dataGridView1.Rows.Count; i++)
                {
                    if (dataGridView1.Rows[i].Cells["Column1"].FormattedValue.ToString() == "True")
                    {
                        actualQty = Convert.ToDouble(dataGridView1.Rows[i].Cells["Qty"].FormattedValue);
                        AcceptedQty = Convert.ToDouble(dataGridView1.Rows[i].Cells["Accepted Qty"].FormattedValue);
                        RejectedQty = Convert.ToDouble(dataGridView1.Rows[i].Cells["Rejected Qty"].FormattedValue);

                        CheckQty = actualQty / 10;

                        percentage = Math.Round((RejectedQty / actualQty) * 100);

                        // if (CheckQty <= Math.Abs(RejectedQty))
                        dtApproval.Rows.Add(dataGridView1.Rows[i].Cells["transid"].FormattedValue,
                            dataGridView1.Rows[i].Cells["Date"].FormattedValue,
                            dataGridView1.Rows[i].Cells["ItemName"].FormattedValue,
                            dataGridView1.Rows[i].Cells["Deptt"].FormattedValue,
                            dataGridView1.Rows[i].Cells["Qty"].FormattedValue,
                            dataGridView1.Rows[i].Cells["Accepted Qty"].FormattedValue,
                            dataGridView1.Rows[i].Cells["Rejected Qty"].FormattedValue,
                            percentage,
                            dataGridView1.Rows[i].Cells["ItemCode"].FormattedValue);

                    }
                }

                if (dtApproval.Rows.Count != 0)
                {
                    string msgBody = "";

                    msgBody = "<style>td{padding: 2px 10px;} *{FONT-FAMILY: 'Segoe UI', Tahoma} BODY{FONT-SIZE: small}TABLE{FONT-SIZE: small}"
                        + "A {FONT-SIZE: small}TABLE {    WIDTH: 100%; BORDER-COLLAPSE: collapse}A {    TEXT-DECORATION: none; COLOR: black} "
                        + "A:hover {    TEXT-DECORATION: underline; COLOR: blue}BODY {    MARGIN: 0px auto}HTML {    BACKGROUND-COLOR: #edf0f5} "
                        + ".section {    MARGIN: 10px; BACKGROUND-COLOR: white; border-radius: 5px}.header {font-weight:bold;CURSOR: pointer; COLOR: white; PADDING-BOTTOM: 5px; PADDING-TOP: 5px; PADDING-LEFT: 5px; MARGIN: 0px 0px 10px; PADDING-RIGHT: 5px; BACKGROUND-COLOR: #4187f6; border-top-left-radius: 5px; border-top-right-radius: 5px "
                        + "}.body_section {    PADDING-BOTTOM: 10px; PADDING-TOP: 10px; PADDING-LEFT: 10px; PADDING-RIGHT: 10px}</STYLE> ";


                    msgBody += "<div class=\"section\"><div class=\"body_section\">";
                    msgBody += "<p>Respected Sir,</p><p>Please check Below details.</p>";
                    msgBody += "</div></div>";


                    msgBody += "<div class=\"section\"><div class=\"header\">Wastage Summary</div><div class=\"body_section\">";

                    msgBody += " <table border='1' style='border-collapse:collapse'>";

                    msgBody += "<tr>";

                    msgBody += "<th>" + dtApproval.Columns["Date"].ColumnName + "</th>";
                    msgBody += "<th>" + dtApproval.Columns["ItemName"].ColumnName + "</th>";
                    msgBody += "<th>" + dtApproval.Columns["Department"].ColumnName + "</th>";
                    msgBody += "<th>" + dtApproval.Columns["Qty"].ColumnName + "</th>";
                    msgBody += "<th>" + dtApproval.Columns["Accepted Qty"].ColumnName + "</th>";
                    msgBody += "<th>" + dtApproval.Columns["Diff. Qty"].ColumnName + "</th>";
                    msgBody += "<th>" + dtApproval.Columns["%"].ColumnName + "</th>";
                    msgBody += "</tr>";

                    for (int i = 0; i < dtApproval.Rows.Count; i++)
                    {
                        msgBody += "<tr>";

                        msgBody += "<td align='right'>" + Convert.ToString(dtApproval.Rows[i]["Date"]) + "</td>";
                        msgBody += "<td align='center'>" + Convert.ToString(dtApproval.Rows[i]["ItemName"]) + "</td>";
                        msgBody += "<td align='center'>" + Convert.ToString(dtApproval.Rows[i]["Department"]) + "</td>";
                        msgBody += "<td align='right'>" + Convert.ToString(dtApproval.Rows[i]["Qty"]) + "</td>";
                        msgBody += "<td align='right'>" + Convert.ToString(dtApproval.Rows[i]["Accepted Qty"]) + "</td>";
                        msgBody += "<td align='right'>" + Convert.ToString(dtApproval.Rows[i]["Diff. Qty"]) + "</td>";
                        msgBody += "<td align='right'>" + Convert.ToString(dtApproval.Rows[i]["%"]) + "</td>";

                        msgBody += "</tr>";
                    }

                    msgBody += "</table>";

                    msgBody += "</div></div>";

                    if (Database.OpenConnection(Utility.MaterialConnectionString))
                    {
                        dtEmail.Rows.Clear();
                        Database.myreader = Database.GetExecuteReaderCommand("select * from AM_ReportWise_MailList where vReportName='WastageInformation' and vCompanyName='" + ComboCmpny.Text + "'");
                        dtEmail.Load(Database.myreader);
                    }
                    dtApproval.Dispose();
                    if (FrmMain.email == "")
                        FrmMain.email = "automail@chapalalgroup.com";
                    if (dtEmail.Rows.Count > 0)
                        Email.SendMail("" + dtEmail.Rows[0][4].ToString() + "", "" + dtEmail.Rows[0][5].ToString() + "", "" + dtEmail.Rows[0][6].ToString() + "", "Wastage Receipt Report for " + ComboCmpny.Text, msgBody, "" + FrmMain.email + "", false, "", false, true);
                    //               Email.SendMail("rikin@champalalgroup.com", "commercialoel@champalalgroup.com", "raj@champalalgroup.com", "Wastage Receipt Report for " + ComboCmpny.Text, msgBody, "rikin@champalalgroup.com", false, "", false, true);
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void Update()
        {
            Database.BeginTransaction();
            for (int i = 0; i < dtApproval.Rows.Count; i++)
            {
                Database.ExecuteNonQuery("UPDATE warehouseinwards set bisApprove=null,vApproveBy=null,dApproveDate=null where transid='"
                    + Convert.ToString(dtApproval.Rows[i]["transId"]) + "' and ToWareHouse='Wastage Godown'");
                Database.ExecuteNonQuery("UPDATE Warehouse set stkinhand = stkinhand - " + Convert.ToString(dtApproval.Rows[i]["Accepted Qty"]) + " where itemName='"
                    + Convert.ToString(dtApproval.Rows[i]["ItemName"]) + "' and companyname = '"
                    + ComboCmpny.Text + "' and warehousename = '" + combowarehouse.Text + "'");

                string itemcode = ProductionCommonFunction.GetItemCode(Convert.ToString(dtApproval.Rows[i]["ItemName"]));
                ProductionCommonFunction.WareHouseStoreoutwards(itemcode, Convert.ToString(dtApproval.Rows[i]["ItemName"]),
                          Convert.ToDouble(Convert.ToString(dtApproval.Rows[i]["Accepted Qty"])), DateTime.Now, ComboCmpny.Text, combowarehouse.Text, "");

            }
            Database.CommitTransaction();
        }

        private void combowarehouse_SelectedIndexChanged(object sender, EventArgs e)
        {
            comboInternalReq.Items.Clear();
            comboInternalReq.Text = "";

            Database.OpenConnection(Utility.MaterialConnectionString);
            Database.myreader = Database.GetExecuteReaderCommand("select distinct isnull(InternalReq,'') as InternalReq from warehousetowarehouse w inner join WareHouseInwards i on w.CompanyName=i.CompanyName and w.InternalReq =i.InternalReqno where w.companyname = '"
                + ComboCmpny.Text + "'  and w.towarehouse = '" + combowarehouse.Text + "' and istransfer is null group by InternalReq ,w.Qty having SUM(i.qty)-w.Qty!=0 ");
            while (Database.myreader.Read())
                comboInternalReq.Items.Add(Database.myreader[0].ToString());
            Database.myreader.Close();
            Database.Closeconnection();
        }

        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            try
            {
                double sum = 0;
                int count = 0;
                pfrm_sbp1.Text = "";

                foreach (DataGridViewRow row in this.dataGridView1.Rows)
                {
                    foreach (DataGridViewColumn col in this.dataGridView1.Columns)
                    {
                        if (dataGridView1[col.Index, row.Index].Selected)
                        {
                            try
                            {
                                sum += Convert.ToDouble(dataGridView1[col.Index, row.Index].Value);

                            }
                            catch (Exception ex)
                            {
                                ex.ToString();
                            }
                            try
                            {

                                count++;
                            }
                            catch (Exception ex)
                            {
                                ex.ToString();
                            }
                        }
                    }
                }
                pfrm_sbp1.Text = " Count : " + count.ToString() + "     Sum : " + sum.ToString();

            }
            catch (Exception ex)
            {
                ex.ToString();
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            isBtnReject = true;
            save();
            isBtnReject = false;

        }

        //private void dataGridView1_CurrentCellChanged(object sender, EventArgs e)
        //{
        //    if (dataGridView1.CurrentCell == null) return;
        //    int columnIndex = dataGridView1.CurrentCell.ColumnIndex;
        //    int rowIndex = dataGridView1.CurrentCell.RowIndex;

        //    if (columnIndex == 6 || columnIndex == 7)
        //    {
        //        double Qty = Utility.SafeConvertToDouble(dataGridView1.Rows[rowIndex].Cells["Qty"].FormattedValue);

        //        double Acceptance = Utility.SafeConvertToDouble(dataGridView1.Rows[rowIndex].Cells["Accepted Qty"].FormattedValue);

        //        if (Qty >= Acceptance)
        //        {
        //            double rej = Qty - Acceptance;

        //            this.dataGridView1.Rows[rowIndex].Cells["Rejected Qty"].Value = ((decimal)rej).ToString("0.##");
        //        }
        //        else
        //        {
        //            double Qty1 = Utility.SafeConvertToDouble(dataGridView1.Rows[rowIndex].Cells["Qty"].FormattedValue);

        //            this.dataGridView1.Rows[rowIndex].Cells["Accepted Qty"].Value = Qty1.ToString();

        //            MessageBox.Show("Accepted Qty is not Greater than Qty.");
        //        }
        //    }


        //}

        //private void dataGridView1_KeyUp(object sender, KeyEventArgs e)
        //{
        //    if (dataGridView1.CurrentCell == null) return;
        //    int columnIndex = dataGridView1.CurrentCell.ColumnIndex;
        //    int rowIndex = dataGridView1.CurrentCell.RowIndex;

        //    if (columnIndex == 6 || columnIndex == 7)
        //    {
        //        double Qty = Utility.SafeConvertToDouble(dataGridView1.Rows[rowIndex].Cells["Qty"].Value.ToString());

        //        double Acceptance = Utility.SafeConvertToDouble(dataGridView1.Rows[rowIndex].Cells["Accepted Qty"].Value.ToString());

        //        if (Qty >= Acceptance)
        //        {
        //            double rej = Qty - Acceptance;

        //            this.dataGridView1.Rows[rowIndex].Cells["Rejected Qty"].Value = ((decimal)rej).ToString("0.##");
        //        }
        //        else
        //        {
        //            double Qty1 = Utility.SafeConvertToDouble(dataGridView1.Rows[rowIndex].Cells["Qty"].Value.ToString());

        //            this.dataGridView1.Rows[rowIndex].Cells["Accepted Qty"].Value = Qty1.ToString();

        //            MessageBox.Show("Accepted Qty is not Greater than Qty.");
        //        }
        //    }
        //}

        private void dataGridView1_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (dataGridView1.CurrentCell == null) return;
            int columnIndex = dataGridView1.CurrentCell.ColumnIndex;
            int rowIndex = dataGridView1.CurrentCell.RowIndex;

            if (columnIndex == 6 || columnIndex == 7)
            {
                double Qty = Utility.SafeConvertToDouble(dataGridView1.Rows[rowIndex].Cells["Qty"].Value.ToString());

                double Acceptance = Utility.SafeConvertToDouble(dataGridView1.Rows[rowIndex].Cells["Accepted Qty"].Value.ToString());

                //if (Qty >= Acceptance)
                //{
                double rej = Qty - Acceptance;

                this.dataGridView1.Rows[rowIndex].Cells["Rejected Qty"].Value = ((decimal)rej).ToString("0.##");
                //}
                //else
                //{
                //    double Qty1 = Utility.SafeConvertToDouble(dataGridView1.Rows[rowIndex].Cells["Qty"].Value.ToString());

                //    this.dataGridView1.Rows[rowIndex].Cells["Accepted Qty"].Value = Qty1.ToString();

                //    MessageBox.Show("Accepted Qty is not Greater than Qty.");
                //}
            }
        }

        private void chkSelect_CheckedChanged(object sender, EventArgs e)
        {
            if (chkSelect.Checked)
            {
                for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                {
                    dataGridView1.Rows[i].Cells[0].Value = "True";
                }
            }
            else
            {
                for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                {
                    dataGridView1.Rows[i].Cells[0].Value = "false";
                }
            }
        }


        private void selectCheckItemToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            if (dataGridView1.CurrentCell == null
                         || dataGridView1.CurrentCell.RowIndex == -1)
                return;
            ProductionCommonFunction.CheckSelectedRows_Datagridview(ref dataGridView1, dataGridView1.CurrentCell.ColumnIndex);
        }

        private void btn_update_warehouse_Click(object sender, EventArgs e)
        {

            string TransId = string.Empty;
            try
            {
                string status = Tools.ToolsCommonFunction.GetValueofCellfromSelectedRows_Datagridview(ref dataGridView1, "status", true);
                if (status.ToUpper().Contains("STOREOUTWARD") || status.ToUpper().Contains("CUTTING GODOWN"))
                {
                    throw new ERPErrorException("Matrial can to be change From 'Storeoutward' and 'Cutting Godown'.");
                }

                TransId = Tools.ToolsCommonFunction.GetValueofCellfromSelectedRows_Datagridview(ref dataGridView1, "TransId", false);

                if (TransId.Length == 0)
                {
                    throw new ERP.ERPErrorException("Please select some roll to transfer");
                }

                if (combowarehouse.Text.Equals(combo_change_warehouse.Text))
                {
                    throw new ERP.ERPErrorException("Change warehouse name should be different from To warehouse");
                }
                else
                {
                    DialogResult result = MessageBox.Show("Are you sure want to transfer this Item?", "Confirmation",
                         MessageBoxButtons.YesNo);

                    if (result.ToString() == "Yes")
                    {
                        Database.ExecuteNonQuery("update warehousetowarehouse set ToWareHouse='" + combo_change_warehouse.Text + "' where transid in(" + TransId + ")");
                        BindGrid();
                        MessageBox.Show("Warehouse updated sccessfully");

                    }
                }
            }
            catch (Exception exObj)
            { ERP.ErrorMessageBox.Show(exObj); }

        }

        private void saveReturnBail()
        {
            if (combowarehouse.Text == "FIBC Bag Godown")
            {
                for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                {

                    if (dataGridView1.Rows[i].Cells[0].FormattedValue.ToString() == "True" && dataGridView1.Rows[i].Cells[2].FormattedValue.ToString() != "")
                    {
                        double rate = 0;
                        double qty = 0;
                        double rejQty = 0;

                        if (!isBtnReject)
                        {
                            rate = Convert.ToDouble(dataGridView1["Rate", i].Value.ToString());
                            qty = Convert.ToDouble(dataGridView1["Accepted Qty", i].Value.ToString());
                            rejQty = Convert.ToDouble(dataGridView1["Rejected Qty", i].Value.ToString());
                        }
                        else
                        {
                            rate = Convert.ToDouble(dataGridView1["Rate", i].Value.ToString());
                            qty = 0;
                            rejQty = Convert.ToDouble(dataGridView1["Qty", i].Value.ToString());
                        }

                        string strSalesReturn = dataGridView1.Rows[i].Cells["Return"].FormattedValue.ToString();
                        qty = Convert.ToDouble(dataGridView1["Accepted Qty", i].Value.ToString());
                        using (FrmFIBCBailingEntryReceipt f = new FrmFIBCBailingEntryReceipt(true))
                        {

                            f.SalesReturnNo = strSalesReturn;
                            f.dblSalesReturQty = qty;
                            f.GetData();
                            if (f.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                            {

                            }
                            //Check qty with Bail entry
                            double BailQty = 0;
                            string strsql = "Select isnull(sum( BailWt),0) from FIBCBailingEntry where SalesReturnNo='" + strSalesReturn + "' and companyname='" + ComboCmpny.Text + "'";
                            Database.myreader = Database.GetExecuteReaderCommand(strsql);
                            DataTable dtcon = new DataTable();
                            dtcon.Load(Database.myreader);
                            if (dtcon.Rows.Count > 0)
                            {
                                BailQty = Convert.ToDouble(dtcon.Rows[0][0]);
                            }

                            if (qty != BailQty)
                            {
                                MessageBox.Show("return Qty is not match with Accepted Qty in FIBC Bail Entry");
                                return;
                            }

                            Database.GetExecuteNonQueryCommand("Insert into warehouseinwards (CompanyName,Deptt,ToWareHouse,Itemcode,ItemName,Qty,InwardDate,Username,Remarks,transid,MrnNo,yr,Rate,Amount,InwardNo,status,RejectedQty,bisApprove,vApproveBy,dApproveDate) values " +
                                  " ('" + ComboCmpny.Text + "','" + dataGridView1["Deptt", i].Value.ToString() + "','" + combowarehouse.Text + "','" + dataGridView1["ItemCode", i].Value.ToString() + "', " +
                                  "  '" + dataGridView1["ItemName", i].Value.ToString() + "','" + qty + "','" + ((DateTime)dataGridView1["Date", i].Value).ToString("dd-MMM-yyyy") + "', " +
                                  "  '" + FrmMain.UserName + "','','" + Convert.ToInt32(dataGridView1["transid", i].Value.ToString()) + "','','" + frmDefaultVale.Year + "', " +
                                  "  '" + rate + "','" + (qty * rate) + "','','" + dataGridView1["Status", i].Value.ToString() + "','"
                                  + rejQty + "','true','" + FrmMain.UserName + "',getdate())");

                            Database.GetExecuteNonQueryCommand("update warehouse set stkinhand = stkinhand + " + qty
                                + " where itemcode = '" + dataGridView1["ItemCode", i].Value.ToString() + "' and companyname = '"
                                + ComboCmpny.Text + "' and warehousename = '" + combowarehouse.Text + "'");


                            string itemname = ProductionCommonFunction.GetItemName(dataGridView1["ItemCode", i].Value.ToString());
                            ProductionCommonFunction.WareHouseStoreinwards(dataGridView1["ItemCode", i].Value.ToString(), itemname,
                                      qty, ((DateTime)dataGridView1["Date", i].Value), ComboCmpny.Text, combowarehouse.Text, "");

                        }
                    }
                }



            }
        }
    }
}
