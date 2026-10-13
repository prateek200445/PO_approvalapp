using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using DataGridViewAutoFilter;
using System.Collections;
namespace ERP
{
    public partial class FrmMISLaminationEntry_New : Form
    {
        bool isMeterGreater = true;
        bool isdoubleclick = false;
        bool isclear = false;
        public int srno = 0;
        private filter.filterData filterData;

        DataTable DtRollstock = new DataTable();
        //DataTable dtOrderMaster;
        public FrmMISLaminationEntry_New()
        {
            InitializeComponent();
            Form_Load();
        }

        private void Form_Load()
        {
            try
            {
                filterData = new filter.filterData(dataGridView2);
                dateLoomEntry.Format = DateTimePickerFormat.Custom;
                //dateLoomEntry.CustomFormat = "MM/dd/yyyy";
                dateLoomEntry.CustomFormat = Tools.ToolsCommonFunction.ERPDateSettings();
                dateLoomEntry.MaxDate = DateTime.Now;
                dateLoomEntry.MinDate = ProductionCommonFunction.ERPBackDateSettings();

                if (Database.OpenConnection(Utility.MaterialConnectionString))
                {
                    comboCompanyName.DataSource = ProductionCommonFunction.GetCompanyList();
                    comboCompanyName.DisplayMember = ProductionCommonVariables.FieldName.CompanyName;
                    comboCompanyName.ValueMember = ProductionCommonVariables.FieldName.CompanyId;
                }
                comboCompanyName.Text = frmDefaultVale.CompanyName;
                comboShift.SelectedIndex = 0;
                buttonUpdate.Enabled = false;
                comboBox1.SelectedIndex = 0;
                filterData = new filter.filterData(dataGridView1);

                FGULCode.DataSource = Database.GetDataTable("select distinct itemcode,ItemName from item  where GroupName like '%Fabric%' and MISCATEGORY='SF' and (itemname not like '%cutpc%' AND ITEMNAME LIKE '%UL%' ) and todate is null and CompanyName='" + comboCompanyName.Text + "'");
                FGULCode.DisplayMember = "ItemName";
                FGULCode.ValueMember = "ItemCode";
                FGULCode.ReadOnly = true;


                FGLAMCode.DataSource = Database.GetDataTable("select distinct itemcode,ItemName from item  where GroupName like '%Fabric%' and MISCATEGORY='SF' and (itemname not like '%cutpc%' AND ITEMNAME LIKE '%LAM%'  ) and todate is null and CompanyName='" + comboCompanyName.Text + "'");
                FGLAMCode.DisplayMember = "ItemName";
                FGLAMCode.ValueMember = "ItemCode";
                FGLAMCode.ReadOnly = true;

                //BindGrid();

                //BindGrid_data();
                foreach (DataGridViewColumn col1 in dataGridView1.Columns)
                {
                    col1.SortMode = DataGridViewColumnSortMode.NotSortable;
                }

                //DataTable dt2 = ProductionCommonFunction.GetFIBCPartyName(ComboCmpny.Text);
                //dtOrderMaster = dt2.Copy();
                //colPartyName.DataSource = dtOrderMaster.DefaultView.ToTable(true, "PartyName");
                //colPartyName.ValueMember = "PartyName";
                //colPartyName.DisplayMember = "PartyName";
                //colPONO.DataSource = dtOrderMaster.DefaultView.ToTable(true, "PONO");
                //colPONO.ValueMember = "PONO";
                //colPONO.DisplayMember = "PONO";

                //DtmarketingOrd.DataSource = dtOrderMaster.DefaultView.ToTable(true, "MarketingOrdNo");
                //DtmarketingOrd.ValueMember = "MarketingOrdNo";
                //DtmarketingOrd.DisplayMember = "MarketingOrdNo";

            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
            }

        }
        private void BindGrid()
        {
            if (Database.OpenConnection_Adapter(Utility.MaterialConnectionString))
            {
                DateTime currentdatetime = dateLoomEntry.Value;
                DateTime firstdate = new DateTime(currentdatetime.Year, currentdatetime.Month, 1);
                Database.myadapter = Database.GetAdapterCommand("select  URollNo,UMetre,UGrossWt,UTareWt,UNetwt,RollNo,size,GSM,color,Quality,Metre,Grosswt,tarewt,NetWt, " +
                    " ActualGPM,ActualGSM,Remarks,Sector,transid,shift,category,sysdate,vInternalRoll,FGITEMCODE,FGLAMCODE from MISLaminationEntry with (nolock) where companyname = '"
                    + comboCompanyName.Text + "'  and sysdate >= '" + firstdate.ToString("yyyy-MM-dd") + "'  AND RIGHT(RollNo,1)!='D' order by transid desc");
                DataSet dataset1 = new DataSet();
                Database.myadapter.Fill(dataset1, "MISLaminationEntry");

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
        private void FrmMISLaminationEntry_Load(object sender, EventArgs e)
        {
        }

        private void dataGridView1_CurrentCellChanged(object sender, EventArgs e)
        {

            if (dataGridView1.CurrentCell == null) return;
            else if (dataGridView1.CurrentCell.ColumnIndex <= 15)
            {
                if (dataGridView1.CurrentCell.ColumnIndex != 8)
                    return;
            }

            try
            {
                double size = 0;
                string value = "";
                string type = "";
                string material = "";
                string colour = "";
                if (Database.OpenConnection(Utility.MaterialConnectionString))
                {

                    if (dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells["dtLamSize"].FormattedValue.ToString().Trim() != "")
                    {
                        string oldQuality = dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells["dtLamQuality"].FormattedValue.ToString().Trim();

                        string[] devide = oldQuality.Split(' ');

                        devide[1] = devide[1].Replace(devide[1], dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells["dtLamSize"].FormattedValue.ToString().Trim());

                        string newQuality = string.Join(" ", devide);
                        if (devide.Length != 4 && devide.Length < 4)
                            newQuality = newQuality + ' ' + dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells["dtColor"].FormattedValue.ToString().Trim();

                        dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells["dtLamQuality"].Value = newQuality;

                    }

                    if (dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells["dtLamTareWt"].FormattedValue.ToString().Trim() != ""
                    && dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells["dtRollNo"].FormattedValue.ToString().Trim() != ""
                     && dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells["dtMetre"].FormattedValue.ToString().Trim() != "" &&
                     dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells["dtGrossWt"].FormattedValue.ToString().Trim() != "" &&
                     dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells["dtTareWt"].FormattedValue.ToString().Trim() != "" &&
                     dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells["dtLamRollNo"].FormattedValue.ToString().Trim() != "" &&
                     dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells["dtLamSize"].FormattedValue.ToString().Trim() != "" &&
                     dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells["dtLamGsm"].FormattedValue.ToString().Trim() != "" &&
                     dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells["dtColor"].FormattedValue.ToString().Trim() != "" &&
                     dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells["dtLamQuality"].FormattedValue.ToString().Trim() != "" &&
                     dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells["dtLamMetre"].FormattedValue.ToString().Trim() != "" &&
                     dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells["dtLamGrossWt"].FormattedValue.ToString().Trim() != "")
                    {

                        string[] substring = dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells["dtLamQuality"].FormattedValue.ToString().Split(' ');

                        type = substring[2];
                        material = substring[0];
                        colour = substring[3];
                        size = Convert.ToDouble(substring[1]);

                        if (size > 0)
                        {
                            dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells["dtLamNetWt"].Value = Convert.ToDouble(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells["dtLamGrossWt"].Value) -
                                Convert.ToDouble(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells["dtLamTarewt"].Value);

                            // code written by manish on 5th Aug 2022
                            if (dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells["dtLamNetWt"].Value.ToString() == "0")
                            {
                                dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells["dtActualGPM"].Value = "0";
                                dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells["dtActualGsm"].Value = "0";

                            }
                            else
                            {
                                dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells["dtActualGPM"].Value = (Convert.ToDouble(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells["dtLamNetWt"].Value) / Convert.ToDouble(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells["dtLamMetre"].Value)) * 1000;
                                if (type.ToString().Trim().ToUpper() == "FLAT")
                                    dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells["dtActualGsm"].Value = (Convert.ToDouble(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells["dtActualGPM"].Value) / size) * 100;
                                else if (type.ToString().Trim().ToUpper() == "FLATGST")
                                    dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells["dtActualGsm"].Value = (Convert.ToDouble(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells["dtActualGPM"].Value) / size) * 100;
                                else
                                    dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells["dtActualGsm"].Value = (Convert.ToDouble(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells["dtActualGPM"].Value) / (size * 2)) * 100;
                                dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells["dtActualGsm"].Value = Math.Round(Convert.ToDouble(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells["dtActualGsm"].Value), 2);
                                dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells["dtActualGPM"].Value = Math.Round(Convert.ToDouble(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells["dtActualGPM"].Value), 2);
                          
                            }
                            
                             if (dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells["dtRemarks"].FormattedValue.ToString().Trim() == "")
                                dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells["dtRemarks"].Value = ".";
                        }


                        Getrollno();
                    }
                }

            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
            }
        }
        private string GenerateMonthName(int monthNo)
        {
            switch (monthNo)
            {
                case 1:
                    return "01";
                case 2:
                    return "02";
                case 3:
                    return "03";
                case 4:
                    return "04";
                case 5:
                    return "05";
                case 6:
                    return "06";
                case 7:
                    return "07";
                case 8:
                    return "08";
                case 9:
                    return "09";
                case 10:
                    return "10";
                case 11:
                    return "11";
                case 12:
                    return "12";

            }
            return "01";
        }
        private void BtnGetData_Click(object sender, EventArgs e)
        {
            Getrollno();
        }
        void Getrollno()
        {
            try
            {
                int k = 1;

                for (int j = 1; j < dataGridView1.Rows.Count - 1; j++)
                {
                    if (dataGridView1.Rows[j].Cells["dtRollNo"].FormattedValue.ToString().ToUpper() == Convert.ToString(dataGridView1.Rows[j - 1].Cells["dtRollNo"].Value).ToUpper())
                    {
                        string[] Lam = dataGridView1.Rows[j - 1].Cells["dtRollNo"].FormattedValue.ToString().Split('-');

                        string rollNo = Lam[Lam.GetUpperBound(0)].Substring(0, 5);

                        string finalRollNo = dataGridView1.Rows[j - 1].Cells["dtRollNo"].FormattedValue.ToString().Replace(rollNo, rollNo + "-" + k + "L");// Lam[0] + "-" + Lam[1] + "-" + rollNo + "-" + k + "L";
                        dataGridView1.Rows[j].Cells["dtLamRollNo"].Value = finalRollNo;
                        k++;
                    }
                    else
                        k = 1;
                }
                //isMeterGreater = true;
                //if (Database.OpenConnection(Utility.MaterialConnectionString))
                {
                    //    double SumMeter = 0;
                    //    for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                    //    {
                    //        if ((bool)dataGridView1["dgChkBox", i].Value == true)
                    //        {
                    //            if (dataGridView1["dtRollNo", i].FormattedValue.ToString().Trim() != "")
                    //            {
                    //                string rollno = "";
                    //                rollno = dataGridView1["dtRollNo", i].FormattedValue.ToString().Trim();
                    //                int count = 0;
                    //                SumMeter = 0;


                    //                Database.myreader = Database.GetExecuteReaderCommand("select isnull(count(Rollno),0) from MisLaminationentry where Rollno = '"
                    //                + rollno + "' and companyname = '" + comboCompanyName.Text + "'");
                    //                if (Database.myreader.Read())
                    //                    count = Convert.ToInt32(Database.myreader[0].ToString());
                    //                Database.myreader.Close();

                    //                for (int j = 0; j < i; j++)
                    //                {
                    //                    if (dataGridView1["dtRollNo", i].FormattedValue.ToString().Trim() == dataGridView1["dtRollNo", j].FormattedValue.ToString().Trim())
                    //                    {
                    //                        count = count + 1;
                    //                        if (dataGridView1["dtLamMetre", i].FormattedValue.ToString().Trim() != "")
                    //                            SumMeter += Convert.ToDouble(dataGridView1["dtLamMetre", i].FormattedValue.ToString().Trim());
                    //                    }
                    //                }

                    //                if (count == 0)
                    //                {
                    //                    dataGridView1["dtLamRollNo", i].Value = dataGridView1["dtRollNo", i].FormattedValue.ToString().Trim() + "L";


                    //                }
                    //                else
                    //                {
                    //                    count = count + 1;
                    //                    dataGridView1["dtLamRollNo", i].Value = dataGridView1["dtRollNo", i].FormattedValue.ToString().Trim() + "L" + (count).ToString();
                    //                    if (dataGridView1["dtMetre", i].FormattedValue.ToString() != "")
                    //                    {
                    //                        if (SumMeter > Convert.ToDouble(dataGridView1["dtMetre", i].FormattedValue.ToString()))
                    //                        {
                    //                            isMeterGreater = false;
                    //                            break;
                    //                        }
                    //                    }
                    //                }
                    //            }
                    //        }
                    //    }
                    //}

                }
            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
            }

        }
        //void getdata()
        //{
        //    try
        //    {


        //        DateTime dt = dateLoomEntry.Value;
        //        string year = Convert.ToString(dt.Year);
        //        string yearpart = year.Substring(2, 2);
        //        string month = GenerateMonthName(dt.Month);
        //        string MergeRoll = yearpart + month + "LM";
        //        int Rollindex = 0;
        //        Database.myreader = Database.GetExecuteReaderCommand("select Rollindex from MisLaminationentry where Rollno like '"
        //               + MergeRoll + "%' and companyname = '" + comboCompanyName.Text + "' order by rollindex desc");
        //        if (Database.myreader.Read())
        //        {
        //            Rollindex = Convert.ToInt32(Database.myreader[0].ToString());
        //        }
        //        Database.myreader.Close();
        //        for (int i = 0; i < dataGridView1.Rows.Count; i++)
        //        {
        //            if (dataGridView1[0, i].FormattedValue.ToString().Trim() != "")
        //            {
        //                MergeRoll = yearpart + month + "LM";
        //                Rollindex = Rollindex + 1;
        //                MergeRoll = MergeRoll + Rollindex.ToString();
        //                dataGridView1[5, i].Value = MergeRoll;
        //            }
        //        }


        //    }
        //    catch (Exception ex)
        //    {
        //        ErrorMessageBox.Show(ex);
        //    }

        //}

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

            buttonSave.Enabled = true;
            buttonUpdate.Enabled = false;
            dateLoomEntry.Enabled = true;
            dataGridView1[0, 0].ReadOnly = false;
            BtnGetData.Enabled = true;
            isdoubleclick = false;

        }

        private void BindGrid_data()
        {
            try
            {
                if (dateLoomEntry.Text.Length <= 0)
                {
                    return;
                }
                dataGridView1.Rows.Clear();

                if (Database.OpenConnection(Utility.MaterialConnectionString))
                {

                    DataTable dataset1 = new DataTable();
                    DataTable dtGridView = new DataTable();
                    Database.myreader = Database.GetExecuteReaderCommand(" exec sp_prod_LaminationRollList '" + comboCompanyName.Text + "','" + dateLoomEntry.Value.ToString("yyyy-MM-dd") + "'");

                    dtGridView.Load(Database.myreader);

                    dataset1 = dtGridView.Copy();

                    DtRollstock.Clear();

                    DtRollstock = dtGridView.Copy();

                    if (comboBox1.Text != "*")
                        dataset1.DefaultView.RowFilter = "Category = '" + comboBox1.Text + "'";

                    DataTable dt = dataset1.DefaultView.ToTable();
                    string type = "";
                    string material = "";
                    string colour = "";
                    string size = "";
                    // BindingSource datasource = new BindingSource(dataset1.Tables[0], null);
                    for (int i = 0; i <= dt.Rows.Count - 1; i++)
                    {

                        string[] substring = dataset1.Rows[i]["Quality"].ToString().Split(new char[] { ' ' }, 4);

                        if (substring.Length > 2)
                        {
                            type = substring[2];
                            size = substring[1];
                        }
                        if (substring.Length > 0)
                            material = substring[0];
                        if (substring.Length > 3)
                            colour = substring[3];


                        dataGridView1.Rows.Add(false, dt.Rows[i]["rollNo"].ToString(), dt.Rows[i]["Metre"].ToString()
                                                , dt.Rows[i]["GrossWt"].ToString(), dt.Rows[i]["TareWt"].ToString()
                                                , dt.Rows[i]["NetWt"].ToString(), dt.Rows[i]["rollNo"].ToString() + "L"
                                                , size, dt.Rows[i]["ReqGSM"].ToString(), colour, dt.Rows[i]["Quality"].ToString(),
                                                dt.Rows[i]["partyRollNo"].ToString(), dt.Rows[i]["Category"].ToString(),
                                                "", "", "", "", "", "", "", "", "", Convert.ToString(dt.Rows[i]["Partyname"]),
                                                Convert.ToString(dt.Rows[i]["MarketingOrderno"]),
                                                Convert.ToString(dt.Rows[i]["FGItemCode"]),
                                                Convert.ToString(dt.Rows[i]["FGLAMCode"]),
                                                dt.Rows[i]["NetWt"].ToString()
                                                );




                    }



                }
            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
            }
        }

        private bool Rollvalidation()
        {
            try
            {
                double ULNetwt = 0;
                double LNetwt = 0;
                ArrayList ArrRollNo = new ArrayList();

                for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                {
                    if ((bool)dataGridView1[0, i].Value == true)
                    {
                        for (int j = 0; j < DtRollstock.Rows.Count; j++)
                        {

                            bool isavail = false;

                            if (dataGridView1.Rows[i].Cells["dtRollNo"].FormattedValue.ToString().ToUpper() == DtRollstock.Rows[j]["RollNo"].ToString().ToUpper())
                            {
                                for (int k = 0; k < ArrRollNo.Count; k++)
                                {
                                    if (DtRollstock.Rows[j]["RollNo"].ToString().ToUpper() == ArrRollNo[k].ToString())
                                        isavail = true;
                                }
                                if (isavail == false)
                                    ULNetwt += Convert.ToDouble(DtRollstock.Rows[j]["Netwt"].ToString());
                                LNetwt += Convert.ToDouble(dataGridView1.Rows[i].Cells["dtNetWt"].FormattedValue.ToString());
                                ArrRollNo.Add(dataGridView1.Rows[i].Cells["dtRollNo"].FormattedValue.ToString().ToUpper());

                            }

                        }
                    }
                }
                if (Math.Round(ULNetwt, 2) == Math.Round(LNetwt, 2))
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                 MessageBox.Show(ex.ToString());
                 return false;
               
               
            }
        }

        public bool Validations()
        {
            try
            {
                #region Check for Split Rolls Netwt and Gross wt - 11.Jan.2021
                for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                {
                    string urollno = Convert.ToString(dataGridView1.Rows[i].Cells["dtRollNo"].Value);
                    double unetwt = Convert.ToDouble(dataGridView1.Rows[i].Cells["dtNetWt"].Value);
                    for (int j = dataGridView1.Rows.Count - 1; j-- > i; )
                    {
                        string urollno1 = Convert.ToString(dataGridView1.Rows[j].Cells["dtRollNo"].Value);
                        double unetwt1 = Convert.ToDouble(dataGridView1.Rows[j].Cells["dtNetWt"].Value);
                        double orgnetwt = Convert.ToDouble(dataGridView1.Rows[j].Cells[ColOrgNetwt.Index].Value);
                        if (urollno == urollno1 && j != i)
                        {
                            if (unetwt == unetwt1)
                            {
                                MessageBox.Show("Net Wt should not be same for U/L Roll No : " + urollno1);
                                dataGridView1.Rows[i].Selected = true;
                                dataGridView1.Rows[j].Selected = true;
                                return false;
                            }
                            // comment by manish on 8th sep 20222 due to merger of roll
                            //if ((unetwt + unetwt1) > orgnetwt)
                            //{
                            //    MessageBox.Show("Original Netwt of U/L Roll should not be Grater than Split Rolls : " + urollno1);
                            //    dataGridView1.Rows[i].Selected = true;
                            //    dataGridView1.Rows[j].Selected = true;
                            //    return false;
                            //}
                            // end comment
                        }
                    }
                }
                #endregion

                #region Check for netwt of Selected Laminated Roll and UL Roll. Lam Roll's Netwt must be grater than ULRoll - 11.Jan.2021
                for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                {
                    if ((bool)dataGridView1[0, i].Value == true)
                    {
                        string urollno = Convert.ToString(dataGridView1.Rows[i].Cells["dtRollNo"].Value);
                        double unetwt = Convert.ToDouble(dataGridView1.Rows[i].Cells["dtNetWt"].Value);
                        double lnetwt = Convert.ToDouble(dataGridView1.Rows[i].Cells["dtLamNetWt"].Value);
                        if (unetwt > lnetwt)
                        {
                            MessageBox.Show("Laminated Net Wt should not be Less than for U/L Roll No's Netwt : " + urollno);
                            dataGridView1.Rows[i].Selected = true;
                            return false;
                        }

                    }
                }
                #endregion

                #region Check Validation for All ready done entry in tapePlantConsumption Master 18-11-2021
                if (Database.OpenConnection(Utility.MaterialConnectionString))
                {
                    Database.myreader = Database.GetExecuteReaderCommand("select PlantName from tapePlantConsumption where PlantName='Lamination' and"
                    + " Companyname='" + comboCompanyName.Text + "' and SYSDate = '" + dateLoomEntry.Value.AddDays(-1).ToString("yyyy-MM-dd") + "'");
                    DataTable dt2 = new DataTable();
                    dt2.Load(Database.myreader);
                    if (dt2.Rows.Count == 0)
                    {
                        MessageBox.Show("You can't do entry because You have not done Consumption entry for :" + dateLoomEntry.Value.AddDays(-1).ToString("yyyy-MM-dd"));
                        Database.myreader.Close();
                        return false;
                    }
                    Database.myreader = Database.GetExecuteReaderCommand("select PlantName from tapePlantConsumption where PlantName='Lamination' and"
                     + " Companyname='" + comboCompanyName.Text + "' and SYSDate = '" + dateLoomEntry.Value.ToString("yyyy-MM-dd") + "'");
                    dt2 = new DataTable();
                    dt2.Load(Database.myreader);
                    if (dt2.Rows.Count > 0)
                    {
                        DialogResult drs = MessageBox.Show("You can't do entry because You have already done consumption entry on this date.\nDo you want to save following information", "Save Information",
                             MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                        if (drs.CompareTo(DialogResult.Yes) == 0)
                        {
                            Database.myreader.Close();
                        }
                        else
                        {
                            MessageBox.Show("You can't do entry because You have already done consumption entry on this date.");
                            Database.myreader.Close();
                            return false;
                        }
                    }
                }
                #endregion

                if (buttonSave.Enabled == true)
                {

                    Database.myreader = Database.GetExecuteReaderCommand("select PlantName from tapePlantConsumption where PlantName='Lamination' and"
                    + " Companyname='" + comboCompanyName.Text + "' and SYSDate = '" + dateLoomEntry.Value.ToString("yyyy-MM-dd") + "'");
                    DataTable dt2 = new DataTable();
                    dt2.Load(Database.myreader);
                    if (dt2.Rows.Count > 0)
                    {
                        DialogResult drs = MessageBox.Show("You can't do entry because You have already done consumption entry on this date.\nDo you want to save following information", "Save Information",
                             MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                        if (drs.CompareTo(DialogResult.Yes) == 0)
                        {
                            Database.myreader.Close();
                        }
                        else
                        {
                            //MessageBox.Show("You can't do entry because You have allready done consumption entry on this date.");
                            Database.myreader.Close();
                            return false;
                        }
                    }

                    for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                    {
                        if ((bool)dataGridView1[0, i].Value == true)
                        {
                            if (Convert.ToString(dataGridView1["dtLamNetWt", i].FormattedValue) == "" ||
                                Convert.ToString(dataGridView1["dtActualGPM", i].FormattedValue) == "" ||
                                Convert.ToString(dataGridView1["dtActualGsm", i].FormattedValue) == "" ||
                                Convert.ToString(dataGridView1["dtLamRollNo", i].FormattedValue) == "")
                            {
                                MessageBox.Show("Pls fill data properly one of the field is blank");
                                return false;
                            }



                            Database.myreader = Database.GetExecuteReaderCommand("select Rollno from Vw_RollNo WHERE ROLLNO = '"
                            + dataGridView1["dtRollNo", i].Value + "' and companyname = '"
                            + comboCompanyName.Text + "'");
                            DataTable dt = new DataTable();
                            dt.Load(Database.myreader);
                            if (dt.Rows.Count <= 0)
                            {
                                MessageBox.Show("Roll No :" + dataGridView1["dtRollNo", i].FormattedValue + " is not available.");
                                return false;
                            }
                            Database.myreader.Close();

                            if (Convert.ToString(dataGridView1[FGULCode.Index, i].Value).Length == 0 ||
                                Convert.ToString(dataGridView1[FGLAMCode.Index, i].Value).Length == 0)
                            {
                                MessageBox.Show("Pls fill Item Code for Roll No :" + dataGridView1["dtRollNo", i].FormattedValue);
                                return false;
                            }
                        }
                    }
                }
                else
                {
                    Database.OpenConnection(Utility.MaterialConnectionString);
                    #region Check Validation For Already Issued
                    Database.myreader = Database.GetExecuteReaderCommand("select vItemCode from [Prod_RMD_InOut] where vItemCode = '" + dataGridView1.Rows[0].Cells["dtLamRollNo"].FormattedValue.ToString() + "' and vCompanyname='" + comboCompanyName.Text + "' and vType='Loom'");
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
            catch (Exception)
            {
                throw;
            }

        }
        private void buttonSave_Click(object sender, EventArgs e)
        {
            try
            {

                bool isentry = false;
                if (!Rollvalidation()) // added by manish on 30th Aug 2022
                {
                    MessageBox.Show("UL Fabric Netwt is not matached with Actual Unlaminted Roll netwt");
                    return;
                }
              
                Getrollno();
                if (Validations())
                {
                    // if (isMeterGreater)
                    //{
                    if (Database.OpenConnection(Utility.MaterialConnectionString))
                    {
                        DateTime dt = dateLoomEntry.Value;
                        string year = Convert.ToString(dt.Year);
                        string yearpart = year.Substring(2, 2);
                        string month = GenerateMonthName(dt.Month);
                        string MergeRoll = yearpart + month + "LM";
                        int Rollindex = 0;
                        string strLamRolls = "";
                        bool isauthority = false;
                        string status = "";
                        string Dte = null;
                        Database.myreader = Database.GetExecuteReaderCommand("select approvalauth2 from warehousemaster where companyname='"
                            + comboCompanyName.Text + "' and warehousename='Lamination Godown' and (approvalauth1 = '"
                            + FrmMain.UserName + "'or approvalauth2 = '"
                            + FrmMain.UserName + "' or approvalauth3 = '"
                            + FrmMain.UserName + "' or approvalauth4 = '" + FrmMain.UserName + "' or  approvalauth5 = '" + FrmMain.UserName + "'  or  entryuser1 = '" + FrmMain.UserName + "')");
                        if (Database.myreader.Read())
                        {
                            isauthority = true;
                        }
                        Database.myreader.Close();

                        if (isauthority)
                        {
                            status = "Approved";
                            Dte = "CAST(FLOOR(CAST(getdate() AS float)) AS datetime)";
                        }
                        else
                        {
                            status = "Pending";
                            Dte = "Null";
                        }

                        #region Check For FG Code
                        for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                        {
                            if (Convert.ToString(dataGridView1.Rows[i].Cells["FGULCode"].Value).Length == 0)
                            {
                                MessageBox.Show("Please Select UL FG Code");
                                return;
                            }

                            if (Convert.ToString(dataGridView1.Rows[i].Cells["FGLAMCode"].Value).Length == 0)
                            {
                                MessageBox.Show("Please Select FG Code");
                                return;
                            }

                            #region Check Item availble in Warehouse or not
                            int Rec = Convert.ToInt32(Database.GetExecuteNonQueryCommand_retrun("Select count(*) from Warehouse where itemcode='" +
                                dataGridView1.Rows[i].Cells["FGULCode"].Value + "' and companyname='" + comboCompanyName.Text + "' and  " +
                                " warehousename='Lamination Godown'"));
                            if (Rec == 0)
                            {
                                Database.GetExecuteNonQueryCommand("Insert into warehouse (ItemCode,ItemName,GroupName,Deptt,Minlevel,Maxlevel,ReOrder,Unit,StkInHand,TypeVendor,NameVendor,EmailVendor,QtyOrder,MnthlyConsum,Rate,CompanyName,LoginName,Dummy1,OpeningStock,SubGroupName,WareHouseName) " +
                                    "select ItemCode,ItemName,GroupName,Deptt,Minlevel,Maxlevel,ReOrder,Unit,0 StkInHand,TypeVendor,NameVendor,EmailVendor,QtyOrder,MnthlyConsum,Rate,CompanyName,LoginName,Dummy1,OpeningStock,SubGroupName,'Lamination Godown' from item where itemcode='" +
                                    dataGridView1.Rows[i].Cells["FGULCode"].Value + "' and companyname='" + comboCompanyName.Text + "'");
                            }
                            Rec = Convert.ToInt32(Database.GetExecuteNonQueryCommand_retrun("Select count(*) from Warehouse where itemcode='" +
                                dataGridView1.Rows[i].Cells["FGLAMCode"].Value + "' and companyname='" + comboCompanyName.Text + "' and  " +
                                " warehousename='Lamination Godown'"));
                            if (Rec == 0)
                            {
                                Database.GetExecuteNonQueryCommand("Insert into warehouse (ItemCode,ItemName,GroupName,Deptt,Minlevel,Maxlevel,ReOrder,Unit,StkInHand,TypeVendor,NameVendor,EmailVendor,QtyOrder,MnthlyConsum,Rate,CompanyName,LoginName,Dummy1,OpeningStock,SubGroupName,WareHouseName) " +
                                    "select ItemCode,ItemName,GroupName,Deptt,Minlevel,Maxlevel,ReOrder,Unit,0 StkInHand,TypeVendor,NameVendor,EmailVendor,QtyOrder,MnthlyConsum,Rate,CompanyName,LoginName,Dummy1,OpeningStock,SubGroupName,'Lamination Godown' from item where itemcode='" +
                                    dataGridView1.Rows[i].Cells["FGLAMCode"].Value + "' and companyname='" + comboCompanyName.Text + "'");
                            }

                            #endregion
                        }
                        #endregion

                        Database.myreader = Database.GetExecuteReaderCommand("select Rollindex from MisLaminationentry where Rollno like '"
                               + MergeRoll + "%' and companyname = '" + comboCompanyName.Text + "' order by rollindex desc");
                        if (Database.myreader.Read())
                        {
                            Rollindex = Convert.ToInt32(Database.myreader[0].ToString());
                        }
                        Database.myreader.Close();
                        Database.BeginTransaction();
                        for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                        {
                            if ((bool)dataGridView1[0, i].Value == true)
                            {
                                Rollindex = Rollindex + 1;
                                if (dataGridView1["dtLamRollNo", i].FormattedValue.ToString() != "")
                                {
                                   


                                    if (Database.GetExecuteNonQueryCommand("insert into MISLaminationEntry (URollNo, UMetre, UGrossWt, UTareWt, UNetWt, RollNo, " +
                                        " Metre, GrossWt, TareWT, NetWt, ActualGPM, ActualGSM, Remarks, Companyname, Sysdate, shift, Username, Size, GSM, " +
                                        " Quality, color, Category, Sector, RollIndex, HodApprove, HodApprovedate, ExciseApprove, ExciseApprovedate,vInternalRoll,FGItemCode,FGLAMCode,IsCons) Values('"
                                        + dataGridView1["dtRollNo", i].FormattedValue + "',"
                                        + dataGridView1["dtMetre", i].FormattedValue + ","
                                        + dataGridView1["dtGrossWt", i].FormattedValue + ","
                                        + dataGridView1["dtTareWt", i].FormattedValue + ","
                                        + dataGridView1["dtNetWt", i].FormattedValue + ",'"
                                        + dataGridView1["dtLamRollNo", i].FormattedValue + "',"
                                        + dataGridView1["dtLamMetre", i].FormattedValue + ","
                                        + dataGridView1["dtLamGrossWt", i].FormattedValue + ","
                                        + dataGridView1["dtLamTareWt", i].FormattedValue + ","
                                        + (Convert.ToDouble(Convert.ToDouble(dataGridView1["dtLamGrossWt", i].Value) - Convert.ToDouble(dataGridView1["dtLamTareWt", i].Value))) + ","
                                        + dataGridView1["dtActualGpm", i].FormattedValue + ","
                                        + dataGridView1["dtActualGsm", i].FormattedValue + ",'"
                                        + dataGridView1["dtRemarks", i].FormattedValue + "','"
                                        + comboCompanyName.Text + "','"
                                        + dateLoomEntry.Value.ToString("yyyy-MM-dd") + "','"
                                        + comboShift.Text + "','"
                                        + FrmMain.UserName + "',"
                                        + dataGridView1["dtLamSize", i].FormattedValue + ",'"
                                        + dataGridView1["dtLamGsm", i].FormattedValue + "','"
                                        + dataGridView1["dtLamQuality", i].FormattedValue + "','"
                                        + dataGridView1["dtColor", i].FormattedValue + "','"
                                        + dataGridView1["dtCategory", i].FormattedValue + "','"
                                        + dataGridView1["dtSector", i].FormattedValue + "',"
                                        + Rollindex + ",'" + status + "'," + Dte + ",'Pending',Null,'"
                                        + dataGridView1["dtIntRollNo", i].FormattedValue + "','"
                                        + dataGridView1["FGULCode", i].Value + "','"
                                        + dataGridView1["FGLAMCode", i].Value + "',1)"))
                                    {
                                        isentry = true;
                                        strLamRolls += dataGridView1["dtLamRollNo", i].FormattedValue.ToString() + ",";
                                        int transid = Convert.ToInt32(Database.GetExecuteNonQueryCommand_retrun("select max(transid) from MISLaminationEntry"));
                                        ProductionCommonFunction.InsertStockJournal(transid, dateLoomEntry.Value, comboCompanyName.Text, "Consumption", Convert.ToString(dataGridView1[FGULCode.Index, i].FormattedValue),
                                            Convert.ToDouble(dataGridView1["dtNetWt", i].FormattedValue), 0, 0, "KGS", frmDefaultVale.Year, transid,
                                            transid, Convert.ToString(dataGridView1[FGULCode.Index, i].Value), dataGridView1["dtRollNo", i].FormattedValue.ToString(), "Lamination");



                                        ProductionCommonFunction.InsertStockJournal(transid, dateLoomEntry.Value, comboCompanyName.Text, "Production", Convert.ToString(dataGridView1[FGLAMCode.Index, i].FormattedValue),
                                            Convert.ToDouble(Convert.ToDouble(dataGridView1["dtLamGrossWt", i].Value) - Convert.ToDouble(dataGridView1["dtLamTareWt", i].Value)), 0,
                                            0, "KGS", frmDefaultVale.Year, transid, transid, Convert.ToString(dataGridView1[FGLAMCode.Index, i].Value), dataGridView1["dtLamRollNo", i].FormattedValue.ToString(), "Lamination");


                                        Database.GetExecuteNonQueryCommand("Update Warehouse set stkinHand=case when Round(stkinHand - "
                                                + ProductionCommonFunction.SafeConvertToDouble(dataGridView1["dtNetWt", i].FormattedValue) + ",2) <0 then 0 else Round(stkinHand - "
                                                + ProductionCommonFunction.SafeConvertToDouble(dataGridView1["dtNetWt", i].FormattedValue)
                                                + ",2) end where CompanyName='" + comboCompanyName.Text + "' and WarehouseName='Lamination Godown' and ItemCode='"
                                                + Convert.ToString(dataGridView1.Rows[i].Cells["FGULCode"].Value) + "'");

                                        Database.GetExecuteNonQueryCommand("Update Warehouse set stkinHand=Round(stkinHand + "
                                                + ProductionCommonFunction.SafeConvertToDouble(dataGridView1["dtLamNetWt", i].FormattedValue)
                                                + ",2)  where CompanyName='" + comboCompanyName.Text + "' and WarehouseName='Lamination Godown' and ItemCode='"
                                                + Convert.ToString(dataGridView1.Rows[i].Cells["FGLAMCode"].Value) + "'");


                                    }
                                }
                                //else
                                //{
                                //    MessageBox.Show("Please enter Roll No value.");
                                //}
                            }
                        }
                        if (isentry)
                        {
                            #region Check wheather Entry done by User or not. as we needs to approve then entries
                        ProductionEntry:
                            frmProductionTapePlantConsumption frm = new frmProductionTapePlantConsumption("Lamination", dateLoomEntry.Value.Date);
                            frm.WindowState = FormWindowState.Maximized;
                            frm.ShowDialog();
                            Database.OpenConnection(Utility.MaterialConnectionString);


                            string strql = "   SELECT round(isnull(sum(netwt)-sum(unetwt2),0),2) FROM (select ROW_NUMBER () over(partition by Urollno order by urollno) as Sr,case when ROW_NUMBER () over(partition by Urollno order by urollno) =1   then TOTALULNETWT else 0 end as UNetWt2,* from (select SUM(UNetWt) OVER(PARTITION BY Urollno ORDER BY Urollno) TOTALULNETWT,* "
                               + " from MISlaminationentry with (nolock) where companyname='" + comboCompanyName.Text
                               + "' and sysdate='" + dateLoomEntry.Value.ToString("yyyy-MM-dd") + "' and " +
                               " Shift='" + comboShift.Text + "' and NetWt>0 and Isnull(IsCons,0)=1 ) as  S ) as S";

                            double dblCnt = Utility.SafeConvertToDouble(Database.GetExecuteNonQueryCommand_retrun(strql));

                            //Database.myreader = Database.GetExecuteReaderCommand("select PlantName from tapePlantConsumption where PlantName='Lamination' and"
                            // + " Companyname='" + comboCompanyName.Text + "' and SYSDate = '" + dateLoomEntry.Value.ToString("yyyy-MM-dd") + "'");
                            //DataTable dt2 = new DataTable();
                            //dt2.Load(Database.myreader);
                            //if (dt2.Rows.Count == 0)
                            //{
                            //    DialogResult drs = MessageBox.Show("Consumption Entry for Lamination is not match with Production entry?\nDo You want to change Production Entry"
                            //        , "Roll Entry", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                            //    if (drs.CompareTo(DialogResult.Yes) == 0)
                            //    {
                            //        Database.RollBackTransaction();
                            //        Database.myreader.Close();
                            //        return;
                            //    }
                            //    Database.myreader.Close();
                            //    goto ProductionEntry;
                            //}
                            if (Math.Round(frm.dbltranNetwt, FrmMain.ERPQtyDigit) != Math.Round(dblCnt, FrmMain.ERPQtyDigit))
                            {
                                DialogResult drs = MessageBox.Show("Consumption Entry for Lamination is not match with Production entry?\nDo You want to change Production Entry"
                                   , "Roll Entry", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                                if (drs.CompareTo(DialogResult.Yes) == 0)
                                {
                                    Database.RollBackTransaction();
                                    Database.myreader.Close();
                                    return;
                                }
                                Database.myreader.Close();
                                
                            }
                            if (strLamRolls.Substring(0, strLamRolls.Length - 1).Length > 0)
                            {
                                strLamRolls = strLamRolls.Substring(0, strLamRolls.Length - 1);
                                Database.GetExecuteReaderCommand("UPDATE MISlaminationentry SET IsCons=0 WHERE CompanyName='" + comboCompanyName.Text
                                    + "' and sysdate='" + dateLoomEntry.Value.ToString("yyyy-MM-dd") + "' and IsCons=1 and RollNo in ('"
                                    + strLamRolls.Replace(",", "','") + "')");
                            }

                            #endregion
                            Database.CommitTransaction();
                            Database.Closeconnection();
                            Utility.UserInformation("Save ", "Lamination Entry " + dateLoomEntry.Value.ToString("yyyy-MM-dd") + " is Saved for Shift " + comboShift.Text + " for Company " + comboCompanyName.Text + ".");
                            MessageBox.Show("Data Added Successfully");
                            BindGrid_data();
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

        private void dataGridView2_DoubleClick(object sender, EventArgs e)
        {
            dateLoomEntry.MaxDate = DateTimePicker.MaximumDateTime;
            dateLoomEntry.MinDate = DateTimePicker.MinimumDateTime;

            isclear = true;
            dataGridView1.Rows.Clear();
            isclear = false;
            buttonSave.Enabled = false;
            buttonUpdate.Enabled = true;
            dateLoomEntry.Enabled = false;
            BtnGetData.Enabled = false;
            isdoubleclick = true;
            try
            {
                dataGridView1[0, 0].Value = false;
                dataGridView1[1, 0].Value = dataGridView2["URollNo", dataGridView2.CurrentRow.Index].Value.ToString();
                dataGridView1[2, 0].Value = dataGridView2["UMetre", dataGridView2.CurrentRow.Index].Value.ToString();
                dataGridView1[3, 0].Value = dataGridView2["UGrossWt", dataGridView2.CurrentRow.Index].Value.ToString();
                dataGridView1[4, 0].Value = dataGridView2["UTareWt", dataGridView2.CurrentRow.Index].Value.ToString();
                dataGridView1[5, 0].Value = dataGridView2["UNetWt", dataGridView2.CurrentRow.Index].Value.ToString();
                dataGridView1[6, 0].Value = dataGridView2["RollNo", dataGridView2.CurrentRow.Index].Value.ToString();
                dataGridView1[7, 0].Value = dataGridView2["Size", dataGridView2.CurrentRow.Index].Value.ToString();
                dataGridView1[8, 0].Value = dataGridView2["GSM", dataGridView2.CurrentRow.Index].Value.ToString();
                dataGridView1[9, 0].Value = dataGridView2["color", dataGridView2.CurrentRow.Index].Value.ToString();
                dataGridView1[10, 0].Value = dataGridView2["Quality", dataGridView2.CurrentRow.Index].Value.ToString();
                dataGridView1[11, 0].Value = "";
                dataGridView1[12, 0].Value = dataGridView2["Category", dataGridView2.CurrentRow.Index].Value.ToString();
                dataGridView1[13, 0].Value = dataGridView2["Metre", dataGridView2.CurrentRow.Index].Value.ToString();
                dataGridView1[14, 0].Value = dataGridView2["GrossWt", dataGridView2.CurrentRow.Index].Value.ToString();
                dataGridView1[15, 0].Value = dataGridView2["TareWT", dataGridView2.CurrentRow.Index].Value.ToString();
                dataGridView1[16, 0].Value = dataGridView2["NetWt", dataGridView2.CurrentRow.Index].Value.ToString();
                dataGridView1[17, 0].Value = dataGridView2["ActualGPM", dataGridView2.CurrentRow.Index].Value.ToString();
                dataGridView1[18, 0].Value = dataGridView2["ActualGSM", dataGridView2.CurrentRow.Index].Value.ToString();
                dataGridView1[19, 0].Value = dataGridView2["Remarks", dataGridView2.CurrentRow.Index].Value.ToString();
                dataGridView1[20, 0].Value = dataGridView2["Sector", dataGridView2.CurrentRow.Index].Value.ToString();
                dataGridView1[FGULCode.Index, 0].Value = dataGridView2["FGItemCode", dataGridView2.CurrentRow.Index].Value.ToString();
                dataGridView1[FGLAMCode.Index, 0].Value = dataGridView2["FGLAMCODE", dataGridView2.CurrentRow.Index].Value.ToString();


                //dataGridView1[19, 0].Value = dataGridView2[18, dataGridView2.CurrentRow.Index].Value.ToString();
                comboShift.Text = dataGridView2["shift", dataGridView2.CurrentRow.Index].Value.ToString();
                srno = Convert.ToInt32(dataGridView2["transid", dataGridView2.CurrentRow.Index].Value.ToString());
                //dataGridView1[17, 0].Value = dataGridView2[23, dataGridView2.CurrentRow.Index].Value.ToString();
                dateLoomEntry.Value = (DateTime)dataGridView2["Sysdate", dataGridView2.CurrentRow.Index].Value;

                comboBox1.Text = dataGridView2["Category", dataGridView2.CurrentRow.Index].Value.ToString();
                dataGridView1["dtIntRollNo", 0].Value = Convert.ToString(dataGridView2["vInternalRoll", dataGridView2.CurrentRow.Index].FormattedValue);

            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
            }
        }

        private void dateLoomEntry_ValueChanged(object sender, EventArgs e)
        {
            if (!isdoubleclick)
            {
                isdoubleclick = true;
                dataGridView1.Rows.Clear();
                isdoubleclick = false;

                BindGrid();
                BindGrid_data();
            }
        }

        private void comboShift_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!isdoubleclick)
            {
                BindGrid();
            }
        }

        private void buttonUpdate_Click(object sender, EventArgs e)
        {
            try
            {
                string value = "";

                if (Validations())
                {
                    if (Database.OpenConnection(Utility.MaterialConnectionString))
                    {

                        Database.myreader = Database.GetExecuteReaderCommand("select Quality from VW_rOLLnO where companyname = '"
                                   + comboCompanyName.Text + "' and rollno = '"
                                   + dataGridView1["dtRollNo", 0].Value.ToString() + "'");
                        if (Database.myreader.Read())
                        {
                            value = Convert.ToString(Database.myreader[0].ToString());
                        }
                        Database.myreader.Close();


                        string[] substring = value.Split(' ');

                        string type = substring[2];
                        string material = substring[0];
                        double size = Convert.ToDouble(dataGridView1["dtLamSize", 0].Value);
                        dataGridView1["dtLamQuality", 0].Value = material + " " +
                                dataGridView1["dtLamSize", 0].Value.ToString() + " " + type + " " +
                                dataGridView1["dtColor", 0].Value.ToString();
                        if (size > 0)
                        {
                            dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells["dtLamNetWt"].Value = Convert.ToDouble(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells["dtLamGrossWt"].Value) -
                                Convert.ToDouble(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells["dtLamTarewt"].Value);
                            dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells["dtActualGPM"].Value = (Convert.ToDouble(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells["dtLamNetWt"].Value) / Convert.ToDouble(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells["dtLamMetre"].Value)) * 1000;
                            if (type.ToString().Trim() == "FLAT")
                                dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells["dtActualGsm"].Value = (Convert.ToDouble(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells["dtActualGPM"].Value) / size) * 100;
                            else if (type.ToString().Trim() == "FLATGST")

                                dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells["dtActualGsm"].Value = (Convert.ToDouble(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells["dtActualGPM"].Value) / size) * 100;
                            else
                                dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells["dtActualGsm"].Value = (Convert.ToDouble(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells["dtActualGPM"].Value) / (size * 2)) * 100;
                            dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells["dtActualGsm"].Value = Math.Round(Convert.ToDouble(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells["dtActualGsm"].Value), 2);
                            dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells["dtActualGPM"].Value = Math.Round(Convert.ToDouble(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells["dtActualGPM"].Value), 2);
                            if (dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells["dtRemarks"].FormattedValue.ToString().Trim() == "")
                                dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells["dtRemarks"].Value = ".";
                        }
                        Database.BeginTransaction();

                        #region check with records's Originla data saved
                        double oldULNetwt = Utility.SafeConvertToDouble(Database.GetExecuteNonQueryCommand_retrun("select Unetwt from MISLaminationEntry where transid=" + srno));
                        double oldLAMNetwt = Utility.SafeConvertToDouble(Database.GetExecuteNonQueryCommand_retrun("select NetWt from MISLaminationEntry where transid=" + srno));
                        #endregion

                        Database.GetExecuteNonQueryCommand("Update Warehouse set stkinHand=Round(stkinHand + " + oldULNetwt
                                   + ",2) where CompanyName='" + comboCompanyName.Text + "' and WarehouseName='Lamination Godown' and ItemCode='"
                                   + Convert.ToString(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[FGULCode.Index].Value) + "'");

                        Database.GetExecuteNonQueryCommand("Update Warehouse set stkinHand=Round(stkinHand - "
                                + oldLAMNetwt + ",2) where CompanyName='" + comboCompanyName.Text + "' and WarehouseName='Lamination Godown' and ItemCode='"
                                + Convert.ToString(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[FGLAMCode.Index].Value) + "'");

                        if (Database.GetExecuteNonQueryCommand("update MISLAminationEntry set " +
                            "  Umetre = " + dataGridView1["dtMetre", 0].Value + "" +
                            " ,Ugrosswt = " + dataGridView1["dtGrossWt", 0].Value + "" +
                            " ,UTareWt = " + dataGridView1["dtTareWt", 0].Value + "" + //changes on dated 18th June 2018 mention as Meter
                            " ,UNetWt = " + dataGridView1["dtNetWt", 0].Value + "" +
                            " ,Size = " + dataGridView1["dtLamSize", 0].Value + "" +
                            " ,GSM = '" + dataGridView1["dtLamGsm", 0].Value + "'" +
                            " ,Quality = '" + dataGridView1["dtLamQuality", 0].Value + "'" +
                            " ,NetWt = " + Convert.ToDouble(Convert.ToDouble(dataGridView1["dtLamGrossWt", 0].Value) - Convert.ToDouble(dataGridView1["dtLamTareWt", 0].Value)) + "" +
                            " ,ActualGPM = " + dataGridView1["dtActualGPM", 0].Value + "" +
                            " ,ActualGSM = " + dataGridView1["dtActualGSM", 0].Value + "" +
                            " ,Remarks = '" + dataGridView1["dtRemarks", 0].Value + "'" +
                            " ,Metre = " + dataGridView1["dtLamMetre", 0].Value + "" +
                            " ,GrossWt = " + dataGridView1["dtLamGrossWt", 0].Value + "" +
                            " ,TareWt = " + dataGridView1["dtLamTareWt", 0].Value + "" +
                            " ,color = '" + dataGridView1["dtColor", 0].Value + "'" +
                            " ,Sector = '" + dataGridView1["dtSector", 0].Value + "'" +
                            " ,shift = '" + comboShift.Text + "'" +
                            " ,Sysdate = '" + dateLoomEntry.Value.ToString("yyyy-MM-dd") + "'" +
                            " ,vInternalRoll = '" + dataGridView1["dtIntRollNo", 0].Value + "'" +
                            " where transid = " + srno + ""))
                        {

                            Database.GetExecuteNonQueryCommand("delete from stockJournalProd where recordlogid = " + srno + " and plantname = 'Lamination' and company_name='" + comboCompanyName.Text + "'");

                            ProductionCommonFunction.InsertStockJournal(srno, dateLoomEntry.Value, comboCompanyName.Text, "Consumption", Convert.ToString(dataGridView1[FGULCode.Index, 0].FormattedValue),
                                       Convert.ToDouble(dataGridView1["dtNetWt", 0].FormattedValue), 0, 0, "KGS", frmDefaultVale.Year, srno,
                                       srno, Convert.ToString(dataGridView1[FGULCode.Index, 0].Value), dataGridView1["dtRollNo", 0].FormattedValue.ToString(), "Lamination");

                            ProductionCommonFunction.InsertStockJournal(srno, dateLoomEntry.Value, comboCompanyName.Text, "Production", Convert.ToString(dataGridView1[FGLAMCode.Index, 0].FormattedValue),
                                Convert.ToDouble(Convert.ToDouble(dataGridView1["dtLamGrossWt", 0].Value) - Convert.ToDouble(dataGridView1["dtLamTareWt", 0].Value)), 0,
                                0, "KGS", frmDefaultVale.Year, srno, srno, Convert.ToString(dataGridView1[FGLAMCode.Index, 0].Value), dataGridView1["dtLamRollNo", 0].FormattedValue.ToString(), "Lamination");

                            Database.GetExecuteNonQueryCommand("Update Warehouse set stkinHand=Round(stkinHand - "
                                    + ProductionCommonFunction.SafeConvertToDouble(dataGridView1["dtNetWt", dataGridView1.CurrentCell.RowIndex].FormattedValue)
                                    + ",2) where CompanyName='" + comboCompanyName.Text + "' and WarehouseName='Lamination Godown' and ItemCode='"
                                    + Convert.ToString(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[FGULCode.Index].Value) + "'");

                            Database.GetExecuteNonQueryCommand("Update Warehouse set stkinHand=Round(stkinHand + "
                                    + ProductionCommonFunction.SafeConvertToDouble(dataGridView1["dtLamNetWt", dataGridView1.CurrentCell.RowIndex].FormattedValue)
                                    + ",2) where CompanyName='" + comboCompanyName.Text + "' and WarehouseName='Lamination Godown' and ItemCode='"
                                    + Convert.ToString(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[FGLAMCode.Index].Value) + "'");

                            Database.CommitTransaction();
                            Database.Closeconnection();
                            MessageBox.Show("Data Updated Successfully");
                            //dataGridView1.Rows.Clear();

                        }
                        Utility.UserInformation("Update ", "Lamination Entry " + srno + "is Update for RollNo " + dataGridView1[5, 0].Value);
                    }
                    isclear = true;
                    dataGridView1.Rows.Clear();
                    isclear = false;
                    BindGrid();
                    isdoubleclick = false;
                }

            }
            catch (Exception ex)
            {
                Database.RollBackTransaction();
                ErrorMessageBox.Show(ex);
            }
        }



        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

            BindGrid_data();
            isclear = true;
            isclear = false;
        }

        private void dataGridView1_UserDeletingRow(object sender, DataGridViewRowCancelEventArgs e)
        {
            isclear = true;
        }

        private void dataGridView1_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            if (e.Control is TextBox)
            {
                TextBox tb = e.Control as TextBox;
                tb.KeyPress += new KeyPressEventHandler(tb_KeyPress);
            }
        }
        void tb_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (this.dataGridView1.CurrentCell.ColumnIndex == 11 || this.dataGridView1.CurrentCell.ColumnIndex == 12)
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

        private void cbSelect_CheckedChanged(object sender, EventArgs e)
        {
            if (cbSelect.Checked)
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

        private void btnAdd_Click(object sender, EventArgs e)
        {
            try
            {


                bool isFirst = true;
                for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                {
                    if ((bool)dataGridView1[0, i].Value == true)
                    {
                        if (MessageBox.Show("Do you want to Split Selected Roll : '" + dataGridView1.Rows[i].Cells[1].Value.ToString() + "' ?"
                        , "Lamination Entry", MessageBoxButtons.YesNo, MessageBoxIcon.Information) != System.Windows.Forms.DialogResult.Yes)
                            return;

                        DataGridViewRow row1 = (DataGridViewRow)dataGridView1.Rows[dataGridView1.CurrentRow.Index];

                        string RollNo = row1.Cells[1].Value.ToString();
                        string Metre = row1.Cells[2].Value.ToString();
                        string GrossWt = row1.Cells[3].Value.ToString();
                        string TareWt = row1.Cells[4].Value.ToString();
                        string NetWt = row1.Cells[5].Value.ToString();
                        string LamRollNo = row1.Cells[6].Value.ToString();
                        string LamSize = row1.Cells[7].Value.ToString();
                        string LamGSM = row1.Cells[8].Value.ToString();
                        string Color = row1.Cells[9].Value.ToString();
                        string LamQuality = row1.Cells[10].Value.ToString();
                        string OutSideRollNo = row1.Cells[11].Value.ToString();
                        string Category = row1.Cells[12].Value.ToString();
                        string FGItemCode = row1.Cells[FGULCode.Index].Value.ToString();
                        string FGLamCode = row1.Cells[FGLAMCode.Index].Value.ToString();
                        string partyname = row1.Cells[colPartyName.Index].Value.ToString();
                        string Orderno = row1.Cells[colPONO.Index].Value.ToString();

                        dataGridView1[0, i].Value = false;

                        if (isFirst)
                        {
                            dataGridView1.Rows.Insert(dataGridView1.CurrentRow.Index + 1, false, RollNo, Metre, GrossWt, TareWt, NetWt, LamRollNo, LamSize, LamGSM, Color, LamQuality, OutSideRollNo, Category);
                            dataGridView1.Rows[dataGridView1.CurrentRow.Index + 1].Cells[FGULCode.Index].Value = FGItemCode;
                            dataGridView1.Rows[dataGridView1.CurrentRow.Index + 1].Cells[FGLAMCode.Index].Value = FGLamCode;

                            dataGridView1.Rows[dataGridView1.CurrentRow.Index + 1].Cells[colPartyName.Index].Value = partyname;
                            dataGridView1.Rows[dataGridView1.CurrentRow.Index + 1].Cells[colPONO.Index].Value = Orderno;


                            dataGridView1.Rows[dataGridView1.CurrentRow.Index + 1].Cells[ColOrgNetwt.Index].Value = NetWt;

                        }
                        Getrollno();

                        isFirst = false;
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
            }




        }



        private void selectCheckItemToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (dataGridView1.CurrentCell == null
                          || dataGridView1.CurrentCell.RowIndex == -1)
                return;

            ProductionCommonFunction.CheckSelectedRows_Datagridview(ref dataGridView1, dataGridView1.CurrentCell.ColumnIndex);

        }

        private void dataGridView1_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (Database.OpenConnection_Adapter(Utility.MaterialConnectionString))
            {

                Database.myadapter = Database.GetAdapterCommand("select   URollNo,UMetre,UGrossWt,UTareWt,UNetwt,RollNo,size,GSM,color,Quality,Metre,Grosswt,tarewt,NetWt, " +
                    " ActualGPM,ActualGSM,Remarks,Sector,transid,shift,category,sysdate,vInternalRoll,FGITEMCODE,FGLAMCODE from MISLaminationEntry with (nolock) where companyname = '"
                    + comboCompanyName.Text + "'  and sysdate = '" + dateLoaddata.Value.ToString("yyyy-MM-dd") + "' AND RIGHT(RollNo,1)!='D' order by transid desc");
                DataSet dataset1 = new DataSet();
                Database.myadapter.Fill(dataset1, "MISLaminationEntry");

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


    }
}











