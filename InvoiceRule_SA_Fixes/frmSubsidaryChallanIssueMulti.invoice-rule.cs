// =============================================================================
// INVOICE-RULE COPY — do not compile next to original.
// Replace frmSubsidaryChallanIssueMulti.cs after review. Search tag: INVOICE-RULE
// Subsidiary multi issue: minus Despatch when MappPackingList (InvNo = Sub-...) is written.
//
// LINE MAP (this copy):
//   318-319 Save minus Despatch after Sub- map
//   357 / 403 Update remember old packing list
//   595-603 Update plus old, rewrite map, minus new
//   1245 helper InvoiceRule_AdjustDespatchGodown
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
    public partial class frmSubsidaryChallanIssueMulti : Form
    {
        ComboBox comboBox;
        DataTable dtIMaster;
        bool IsUpdate = false;
        bool isclear = false;
        bool update = false;
        bool delete = false;
        bool flagupdate = false;

        public frmSubsidaryChallanIssueMulti()
        {
            InitializeComponent();
            Form_Load();
        }

        private void Form_Load()
        {
            try
            {
                btnUpdate.Enabled = false;

                //dateMainChallanDate.Format = DateTimePickerFormat.Custom;
                //dateMainChallanDate.CustomFormat = "MM/dd/yyyy HH:mm:ss";

                datePlacedate.Format = DateTimePickerFormat.Custom;
                datePlacedate.CustomFormat = DespatchCommonFunction.ERPDateSettings();


                dateTimeIssue.Format = DateTimePickerFormat.Custom;
                dateTimeIssue.CustomFormat = DespatchCommonFunction.ERPDateSettings();

                dateTimeLR.Format = DateTimePickerFormat.Custom;
                dateTimeLR.CustomFormat = DespatchCommonFunction.ERPDateSettings();

                if (Database.OpenConnection(Utility.DespacthConnectionString))
                {
                    DespatchCommonFunction.GetCompanyList(comboProcessorName);
                    DespatchCommonFunction.GetTransportMaster(comboNameofTransporter);
                    //string strsql = ("select Companyname from companymaster order by companyname");
                    //string strsql = "Select companyname from CompanyMaster where  " +
                    //      "   isnull(IsFreeze,'no')='no' and (('" + dateTimeIssue.Value.ToString("yyyy-MM-dd") +
                    //      "' between fromdate and todate) or (fromdate <= '" + dateTimeIssue.Value.ToString("yyyy-MM-dd") +
                    //      "' and todate is null ))  order by CompanyName";

                    //DespatchCommonFunction.Loadcombo(comboSubsidiaryBuyer, strsql, "Companyname", "Companyname", Utility.DespacthConnectionString);

                    DataTable dt = new DataTable();
                    Database.OpenConnection(Utility.DespacthConnectionString);
                    Database.myreader = Database.GetExecuteReaderCommand("select  mainchallanno from subsidiarychallanItemMulti with(nolock) where ProcessorName='" + frmDefaultVale.CompanyName + "'");
                    dt.Load(Database.myreader);
                    DtMainChallanNo.DataSource = dt;
                    DtMainChallanNo.ValueMember = "mainchallanno";
                    DtMainChallanNo.DisplayMember = "mainchallanno";

                    Database.Closeconnection();
                }
                textYear.Text = frmDefaultVale.Year;
                comboProcessorName.Text = frmDefaultVale.CompanyName;
                if (comboProcessorName.Text == "Plastene India Limited (Unit -II)")
                {
                    textPlace.Text = "RAJPUR";
                }
                //comboSubsidiaryyear.Text = frmDefaultVale.Year;
                fillData();
            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
            }
        }

        private void frmSubsidaryChallanIssue_Load(object sender, EventArgs e)
        {
        }

        private void comboProcessorName_SelectedIndexChanged(object sender, EventArgs e)
        {

            comboSubsidiaryBuyer.Items.Clear();
            comboSubsidiaryBuyer.Text = "";
            if (Database.OpenConnection(Utility.DespacthConnectionString))
            {
                Database.myreader = Database.GetExecuteReaderCommand("select Distinct(SubsidiaryName) from subsidiarychallanMulti WITH (NOLOCK)  where processorname = '"
                     + comboProcessorName.Text + "' order by SubsidiaryName");
                while (Database.myreader.Read())
                    comboSubsidiaryBuyer.Items.Add(Database.myreader[0].ToString());
                Database.myreader.Close();
                Database.Closeconnection();
            }
            if (comboSubsidiaryBuyer.Items.Count > 0)
                comboSubsidiaryBuyer.SelectedIndex = 0;
        }
        private void ClearAll()
        {
            try
            {
                isclear = true;
                dataGridView1.Rows.Clear();
                textTotalAmount.Text = "";
                textVatAmount.Text = "";
                textVehicleNo.Text = "";
                update = false;
                textLrNo.Text = "";
                textQtyDesc.Text = "";
                textQtyRemark.Text = "";
                textVATPer.Text = "";
                textNatureofProcessing.Text = "";
                comboNameofTransporter.Text = "";
                textPlace.Text = "";
                textDriverName.Text = "";
                textDriverAddress.Text = "";
                textDestination.Text = "";
                textDriverLicNo.Text = "";
                textLicenceIssuing.Text = "";
                comboNature.Text = "";
                comboPlaceOrigin.Text = "";
                btnUpdate.Enabled = false;
                btnSave.Enabled = true;
                if (Database.OpenConnection(Utility.DespacthConnectionString))
                {
                    string strsql = "Select companyname from CompanyMaster where (invoicecategory = 'Domestic'  or invoicecategory = '*') " +
                          " and isnull(IsFreeze,'no')='no' and (('" + dateTimeIssue.Value.ToString("yyyy-MM-dd") +
                           "' between fromdate and todate) or (fromdate <= '" +
                           dateTimeIssue.Value.ToString("yyyy-MM-dd") + "' and todate is null ))  order by CompanyName";
                    DespatchCommonFunction.Loadcombo(comboSubsidiaryBuyer, strsql, "CompanyName", "CompanyName", Utility.DespacthConnectionString);
                }
                isclear = false;
            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            try
            {
                if (validate())
                {

                    bool isvalid = false;
                    bool isMultilayer = false;

                    if (Database.OpenConnection(Utility.DespacthConnectionString))
                    {
                        for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                        {

                            Database.myreader = Database.GetExecuteReaderCommand("Select subsidiarybuyer from subsidiarychallanitemMulti WITH (NOLOCK) " +
                                " where mainchallanno = '" + dataGridView1.Rows[i].Cells[DtMainChallanNo.Index].Value.ToString() + "' and Processorname = '"
                                + comboProcessorName.Text + "' and subsidiarybuyer = '" + comboSubsidiaryBuyer.Text
                                + "' and year = '" + dataGridView1.Rows[i].Cells[DYear.Index].Value.ToString() + "' ");
                            if (!Database.myreader.Read())
                                isvalid = true;
                            Database.myreader.Close();

                            //if (dataGridView1.Rows[i].Cells[DtCommodity.Index].Value.ToString().ToLower().Contains("Multilayer".ToLower()) ||
                            //    dataGridView1.Rows[i].Cells[DtCommodity.Index].Value.ToString().ToLower().Contains("liner".ToLower()) ||
                            //    dataGridView1.Rows[i].Cells[DtCommodity.Index].Value.ToString().ToLower().Contains("film".ToLower()) ||
                            //    dataGridView1.Rows[i].Cells[DtCommodity.Index].Value.ToString().ToLower().Contains("ld/lldpe bags".ToLower()) ||
                            //    dataGridView1.Rows[i].Cells[DtCommodity.Index].Value.ToString().ToLower().Contains("ld/lldpe film".ToLower()))
                            //    isMultilayer = true;

                        }

                    }

                    //if (textQtyDesc.Text.ToString().ToLower().Contains("Multilayer".ToLower()) ||
                    //            textQtyDesc.Text.ToString().ToLower().Contains("liner".ToLower()) ||
                    //            textQtyDesc.Text.ToString().ToLower().Contains("film".ToLower()) ||
                    //            textQtyDesc.Text.ToString().ToLower().Contains("ld/lldpe bags".ToLower()) ||
                    //            textQtyDesc.Text.ToString().ToLower().Contains("ld/lldpe film".ToLower()))
                    //    isMultilayer = true;


                    if (isvalid)
                        MessageBox.Show("Main Challan No is not matched with commodity Name");
                    else
                    {

                        bool issave = false;
                        DialogResult drs = MessageBox.Show("Do you want to generate Subsidary Challan  ", "Subsidary Challan"
                                          , MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                        if (drs.CompareTo(DialogResult.Yes) == 0)
                        {
                            if (Database.OpenConnection(Utility.DespacthConnectionString))
                            {
                                #region PackingList
                                string despackinglist = "";
                                if (isMultilayer == false)
                                {
                                    if (DespatchCommonFunction.AllowMappingPackinglist(comboProcessorName.Text) > 0)
                                    {

                                        if (!DespatchCommonFunction.GetPackingListInvoiceNo2(comboProcessorName.Text, comboSubsidiaryBuyer.Text, Utility.SafeConvertToDouble(txttdq.Text), dataGridView1, colItemCode.Name, Column2.Name, ref despackinglist))
                                        {
                                            return;
                                        }
                                    }
                                }
                                #endregion

                                Database.BeginTransaction();
                                int srno = 0;

                                Database.myreader = Database.GetExecuteReaderCommand("SELECT isnull(SRNO,0) FROM (select srno,processorname,yr from SubsidiaryChallan"
                                + " WHERE processorname='" + comboProcessorName.Text + "'AND  yr = '" + textYear.Text + "'  UNION ALL "
                                + " select srno,processorname,yEAr from subsidiaryChallanissueMulti  WHERE processorname='" + comboProcessorName.Text
                                + "' AND  yEAr = '" + textYear.Text + "')AS TEMP  ORDER BY SRNO DESC ");
                                if (Database.myreader.Read())
                                    srno = Convert.ToInt32(Database.myreader[0].ToString());
                                Database.myreader.Close();
                                srno = srno + 1;

                                if (Database.GetExecuteNonQueryCommand("insert into subsidiaryChallanissueMulti values ('"
                                    + comboProcessorName.Text.ToUpper() + "','"
                                    + comboSubsidiaryBuyer.Text.ToUpper() + "','"
                                    + dateTimeIssue.Value.ToString(DespatchCommonFunction.ERPDBDateSettings()) + "','"
                                    + textLrNo.Text + "','"
                                    + dateTimeLR.Value.ToString(DespatchCommonFunction.ERPDBDateSettings()) + "','"
                                    + textQtyDesc.Text + "','" + textQtyRemark.Text + "','" + comboType.Text + "',"
                                    + Utility.ConverttoInt(textVATPer.Text) + "," + Utility.ConverttoInt(textVatAmount.Text) + ","
                                    + Utility.ConverttoInt(textTotalAmount.Text) + ",'"
                                    + textNatureofProcessing.Text.ToUpper() + "','" +
                                    textVehicleNo.Text.ToUpper() + "','"
                                    + comboNameofTransporter.Text.ToUpper() + "','"
                                    + textPlace.Text + "','"
                                    + datePlacedate.Value.ToString(DespatchCommonFunction.ERPDBDateSettings()) + "','" +
                                    textDriverName.Text + "','"
                                    + textDriverAddress.Text + "','"
                                    + textDestination.Text + "','"
                                    + textDriverLicNo.Text + "','" +
                                    textLicenceIssuing.Text + "','"
                                    + comboNature.Text + "','"
                                    + comboPlaceOrigin.Text + "','" + textYear.Text + "'," + srno + ",'','','') "))
                                    issave = true;

                                for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                                {
                                    string st = "";

                                    if (dataGridView1.Rows[i].Cells[Column8.Index].FormattedValue.ToString() != "")
                                        dataGridView1.Rows[i].Cells[Column8.Index].Value = ".";
                                    Database.myreader = Database.GetExecuteReaderCommand("select balancedqty from subsidiarychallanitemMulti where  mainchallanno = '"
                                        + dataGridView1.Rows[i].Cells[DtMainChallanNo.Index].Value.ToString() + "' and Processorname = '"
                                         + comboProcessorName.Text + "' and subsidiarybuyer = '" + comboSubsidiaryBuyer.Text
                                         + "' and year = '" + dataGridView1.Rows[i].Cells[DYear.Index].Value.ToString() + "' and commodityname = '"
                                         + dataGridView1.Rows[i].Cells[DtCommodity.Index].Value.ToString() + "'");
                                    if (Database.myreader.Read())
                                    {
                                        st = Convert.ToString(Convert.ToDouble(Database.myreader[0].ToString())
                                     - Convert.ToDouble(dataGridView1.Rows[i].Cells[Column2.Index].Value));

                                    }
                                    Database.myreader.Close();


                                    Database.GetExecuteNonQueryCommand("insert into subsidiarychallanissueitemMulti values('"
                                     + textYear.Text + "'," + srno + ",'" + comboProcessorName.Text
                                     + "','" + comboSubsidiaryBuyer.Text + "','"
                                     + dataGridView1.Rows[i].Cells[DtMainChallanNo.Index].Value.ToString().Trim() + "','"
                                     + Convert.ToDateTime(dataGridView1.Rows[i].Cells[Column7.Index].Value).ToString("MM/dd/yyyy") + "','"
                                     + dataGridView1.Rows[i].Cells[DtCommodity.Index].Value.ToString() + "','"
                                     + dataGridView1.Rows[i].Cells[Column9.Index].Value.ToString() + "','"
                                     + Convert.ToString(dataGridView1.Rows[i].Cells[Column8.Index].Value) + "','"
                                     + dataGridView1.Rows[i].Cells[Column2.Index].Value.ToString() + "','"
                                     + dataGridView1.Rows[i].Cells[DtUnit.Index].Value.ToString() + "','"
                                      + st + "','" + dataGridView1.Rows[i].Cells[DYear.Index].Value.ToString() + "','"
                                      + Convert.ToString(dataGridView1.Rows[i].Cells[colItemCode.Index].Value) + "')");


                                    Database.GetExecuteNonQueryCommand("update subsidiarychallanitemMulti set balancedqty = balancedqty - "
                                         + dataGridView1.Rows[i].Cells[Column2.Index].Value.ToString()
                                                + " where mainchallanno = '" + dataGridView1.Rows[i].Cells[DtMainChallanNo.Index].Value.ToString() + "' and Processorname = '"
                                         + comboProcessorName.Text + "' and subsidiarybuyer = '" + comboSubsidiaryBuyer.Text
                                         + "' and year = '" + dataGridView1.Rows[i].Cells[DYear.Index].Value.ToString() + "' and commodityname = '"
                                         + dataGridView1.Rows[i].Cells[DtCommodity.Index].Value.ToString() + "'");
                                }

                                for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                                {
                                    dataGridView1.Rows[i].Cells[Column5.Index].Value = Convert.ToString(Convert.ToDouble(dataGridView1.Rows[i].Cells[Column5.Index].Value)
                                       - Convert.ToDouble(dataGridView1.Rows[i].Cells[Column2.Index].Value));
                                }


                                if (issave)
                                    MessageBox.Show("Subsidary Order No " + srno.ToString() + " is generated succesfully");
                                Database.CommitTransaction();

                                #region PackingList
                                if (DespatchCommonFunction.AllowMappingPackinglist(comboProcessorName.Text) > 0)
                                {
                                    if (Database.OpenConnection(Utility.DespacthConnectionString))
                                    {
                                        Database.GetExecuteNonQueryCommand("INSERT INTO MappPackingList (CompanyName,InvNo,Yr,PackingListNo) VALUES('" +
                                        comboProcessorName.Text + "','Sub-" + srno + "','" + textYear.Text + "','" + despackinglist + "')");
                                        // INVOICE-RULE: packing list mapped on subsidiary issue = take kg off Despatch.
                                        InvoiceRule_AdjustDespatchGodown(comboProcessorName.Text, despackinglist, true);
                                    }
                                }
                                #endregion

                                Database.Closeconnection();
                                ClearAll();
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

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            //frmPrintSubsidaryChallanMulti frm = new frmPrintSubsidaryChallanMulti();
            //frm.Show();
            TabClass.OpenForm(new frmPrintSubsidaryChallanMulti());
        }




        private void btnUpdate_Click(object sender, EventArgs e)
        {
            bool blntran = false;
            try
            {
                string desPackinglist = "";
                string desPackinglistBeforeUpdate = ""; // INVOICE-RULE
                update = true;
                bool isMultilayer = false;
                if (validate())
                {
                    bool isvalid = false;
                    if (Database.OpenConnection(Utility.DespacthConnectionString))
                    {
                        for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                        {

                            Database.myreader = Database.GetExecuteReaderCommand("Select * from subsidiarychallanitemMulti WITH (NOLOCK) " +
                                " where mainchallanno = '" + dataGridView1.Rows[i].Cells[DtMainChallanNo.Index].Value.ToString() + "' and Processorname = '"
                                + comboProcessorName.Text + "' and subsidiarybuyer = '" + comboSubsidiaryBuyer.Text
                                + "' and year = '" + dataGridView1.Rows[i].Cells[DYear.Index].Value.ToString() + "' and commodityname = '"
                                + dataGridView1.Rows[i].Cells[DtCommodity.Index].Value.ToString() + "'");
                            if (!Database.myreader.Read())
                                isvalid = true;
                            Database.myreader.Close();
                            //if (dataGridView1.Rows[i].Cells[DtCommodity.Index].Value.ToString().ToLower().Contains("Multilayer".ToLower()) ||
                            //   dataGridView1.Rows[i].Cells[DtCommodity.Index].Value.ToString().ToLower().Contains("liner".ToLower()) ||
                            //   dataGridView1.Rows[i].Cells[DtCommodity.Index].Value.ToString().ToLower().Contains("film".ToLower()) ||
                            //   dataGridView1.Rows[i].Cells[DtCommodity.Index].Value.ToString().ToLower().Contains("ld/lldpe bags".ToLower()) ||
                            //   dataGridView1.Rows[i].Cells[DtCommodity.Index].Value.ToString().ToLower().Contains("ld/lldpe film".ToLower()))
                            //    isMultilayer = true;
                        }

                    }
                    //if (textQtyDesc.Text.ToString().ToLower().Contains("Multilayer".ToLower()) ||
                    //           textQtyDesc.Text.ToString().ToLower().Contains("liner".ToLower()) ||
                    //           textQtyDesc.Text.ToString().ToLower().Contains("film".ToLower()) ||
                    //           textQtyDesc.Text.ToString().ToLower().Contains("ld/lldpe bags".ToLower()) ||
                    //           textQtyDesc.Text.ToString().ToLower().Contains("ld/lldpe film".ToLower()))
                    //    isMultilayer = true;

                    #region PackingList

                    if (isMultilayer == false)
                    {
                        if (DespatchCommonFunction.AllowMappingPackinglist(comboProcessorName.Text) > 0)
                        {
                            //check with Packing List
                            double TotalNetwt = 0;
                            TotalNetwt = Utility.SafeConvertToDouble(txttdq.Text);
                            desPackinglist = Convert.ToString(Database.GetExecuteNonQueryCommand_retrun("SELECT DISTINCT TOP 1 dbo.DistinctList (MappPackingList.PackingListNo ,',') PackingListNo FROM MappPackingList WHERE companyname='"
                                + comboProcessorName.Text + "' AND InvNo='Sub-" + textSrNo.Text + "' AND Yr='" + textYear.Text + "'"));
                            desPackinglistBeforeUpdate = desPackinglist; // INVOICE-RULE

                            if (desPackinglist == "")
                            {
                                if (!DespatchCommonFunction.GetPackingListInvoiceNo2(comboProcessorName.Text, comboSubsidiaryBuyer.Text, TotalNetwt, dataGridView1, colItemCode.Name, Column2.Name, ref desPackinglist))
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
                                        string strsql = "select sum(isnull(Netwt,0)) Netwt from vw_PackingListAll where companyname='" + comboProcessorName.Text
                                            + "' and  yr='" + textYear.Text + "' and "
                                            + " (PACKINGLISTNO ='" + split[i] + "' or invno='" + split[i] + "') ";
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
                    if (isvalid)
                        MessageBox.Show("Main Challan No is not matched with commodity Name");
                    else
                    {

                        if (textNatureofProcessing.Text == "")
                            MessageBox.Show("Please enter value in Nature of Processing");
                        else
                        {
                            bool isupdate = false;
                            DialogResult drs = MessageBox.Show("Do you want to update Subsidary Challan  "
                                                                , "update Challan"
                                              , MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                            if (drs.CompareTo(DialogResult.Yes) == 0)
                            {
                                if (Database.OpenConnection(Utility.DespacthConnectionString))
                                {
                                    blntran = false;
                                    Database.BeginTransaction();
                                    blntran = true;
                                    //Database.GetExecuteNonQueryCommand("update subsidiaryChallanIssueItemmulti set BalancedQty = BalancedQty + OrderQty   where srno = " +
                                    //  textSrNo.Text + " and processorname = '" + comboProcessorName.Text + "' and year = '"
                                    //   + textYear.Text + "' and subsidiarybuyer = '" + comboSubsidiaryBuyer.Text + "'");


                                    System.Collections.ArrayList ArrMainChallanNo = new System.Collections.ArrayList();
                                    System.Collections.ArrayList ArrOrderQty = new System.Collections.ArrayList();
                                    System.Collections.ArrayList ArrCommodity = new System.Collections.ArrayList();
                                    System.Collections.ArrayList ArrYear = new System.Collections.ArrayList();

                                    Database.myreader = Database.GetExecuteReaderCommand("select mainchallanno,OrderQty,commodityname,subsidiaryyear  from subsidiaryChallanIssueItemmulti where srno = " +
                                           textSrNo.Text + " and processorname = '" + comboProcessorName.Text + "' and year = '"
                                           + textYear.Text + "' and subsidiarybuyer = '" + comboSubsidiaryBuyer.Text + "'");
                                    while (Database.myreader.Read())
                                    {
                                        ArrMainChallanNo.Add(Database.myreader[0].ToString());
                                        ArrOrderQty.Add(Database.myreader[1].ToString());
                                        ArrCommodity.Add(Database.myreader[2].ToString());
                                        ArrYear.Add(Database.myreader[3].ToString());
                                    }
                                    Database.myreader.Close();

                                    for (int j = 0; j < ArrMainChallanNo.Count; j++)
                                    {
                                        Database.GetExecuteNonQueryCommand("update subsidiarychallanitemMulti set balancedqty = balancedqty + "
                                             + ArrOrderQty[j].ToString() +
                                             " where mainchallanno = '" + ArrMainChallanNo[j].ToString() + "' and Processorname = '"
                                             + comboProcessorName.Text + "' and subsidiarybuyer = '" + comboSubsidiaryBuyer.Text
                                             + "' and year = '" + ArrYear[j].ToString() + "' and commodityname = '"
                                             + ArrCommodity[j].ToString() + "'");
                                    }
                                    Database.GetExecuteNonQueryCommand("delete from  subsidiaryChallanissueMulti where srno = " +
                                      textSrNo.Text + " and processorname = '" + comboProcessorName.Text + "' and year = '"
                                       + textYear.Text + "' and subsidiarybuyer = '" + comboSubsidiaryBuyer.Text + "'");
                                    Database.GetExecuteNonQueryCommand("delete from  subsidiarychallanissueitemMulti where srno = " +
                                      textSrNo.Text + " and processorname = '" + comboProcessorName.Text + "' and year = '"
                                       + textYear.Text + "' and subsidiarybuyer = '" + comboSubsidiaryBuyer.Text + "'");


                                    Database.GetExecuteNonQueryCommand("insert into subsidiaryChallanissueMulti values ('"
                                   + comboProcessorName.Text.ToUpper() + "','"
                                   + comboSubsidiaryBuyer.Text.ToUpper() + "','"
                                   + dateTimeIssue.Value.ToString(DespatchCommonFunction.ERPDBDateSettings()) + "','"
                                   + textLrNo.Text + "','"
                                   + dateTimeLR.Value.ToString(DespatchCommonFunction.ERPDBDateSettings()) + "','"
                                   + textQtyDesc.Text + "','" + textQtyRemark.Text + "','" + comboType.Text + "',"
                                   + Utility.ConverttoInt(textVATPer.Text) + "," + Utility.ConverttoInt(textVatAmount.Text) + ","
                                   + Utility.ConverttoInt(textTotalAmount.Text) + ",'"
                                   + textNatureofProcessing.Text.ToUpper() + "','" + textVehicleNo.Text.ToUpper() + "','"
                                   + comboNameofTransporter.Text.ToUpper() + "','"
                                   + textPlace.Text + "','"
                                   + datePlacedate.Value.ToString(DespatchCommonFunction.ERPDBDateSettings()) + "','"
                                   + textDriverName.Text + "','"
                                   + textDriverAddress.Text + "','"
                                   + textDestination.Text + "','"
                                   + textDriverLicNo.Text + "','" + textLicenceIssuing.Text + "','"
                                   + comboNature.Text + "','"
                                   + comboPlaceOrigin.Text + "','" + textYear.Text + "'," + textSrNo.Text + ",'','','') ");



                                    for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                                    {
                                        if (dataGridView1.Rows[i].Cells[Column8.Index].FormattedValue.ToString() != "")
                                            dataGridView1.Rows[i].Cells[Column8.Index].Value = ".";
                                        string st = "";
                                        Database.GetExecuteNonQueryCommand("update subsidiarychallanitemMulti set balancedqty = balancedqty - "
                                            + dataGridView1.Rows[i].Cells[Column2.Index].Value.ToString()
                                                   + " where mainchallanno = '" + dataGridView1.Rows[i].Cells[DtMainChallanNo.Index].Value.ToString().Trim() + "' and Processorname = '"
                                            + comboProcessorName.Text + "' and subsidiarybuyer = '" + comboSubsidiaryBuyer.Text
                                            + "' and year = '" + dataGridView1.Rows[i].Cells[DYear.Index].Value.ToString() + "' and commodityname = '"
                                            + dataGridView1.Rows[i].Cells[DtCommodity.Index].Value.ToString() + "'");

                                        Database.myreader = Database.GetExecuteReaderCommand("Select balancedqty from subsidiarychallanitemMulti " +
                                            " where mainchallanno = '" + dataGridView1.Rows[i].Cells[DtMainChallanNo.Index].Value.ToString() + "' and Processorname = '"
                                            + comboProcessorName.Text + "' and subsidiarybuyer = '" + comboSubsidiaryBuyer.Text
                                            + "' and year = '" + dataGridView1.Rows[i].Cells[DYear.Index].Value.ToString() + "' and commodityname = '"
                                            + dataGridView1.Rows[i].Cells[DtCommodity.Index].Value.ToString() + "'");
                                        if (Database.myreader.Read())
                                            st = Database.myreader[0].ToString();
                                        Database.myreader.Close();


                                        Database.GetExecuteNonQueryCommand("insert into subsidiarychallanissueitemMulti values('"
                                         + textYear.Text + "'," + textSrNo.Text + ",'" + comboProcessorName.Text
                                         + "','" + comboSubsidiaryBuyer.Text + "','"
                                         + dataGridView1.Rows[i].Cells[DtMainChallanNo.Index].Value.ToString() + "','"
                                         + Convert.ToDateTime(dataGridView1.Rows[i].Cells[Column7.Index].Value).ToString("MM/dd/yyyy") + "','"
                                         + dataGridView1.Rows[i].Cells[DtCommodity.Index].Value.ToString() + "','"
                                         + dataGridView1.Rows[i].Cells[Column9.Index].Value.ToString() + "','"
                                         + Convert.ToString(dataGridView1.Rows[i].Cells[Column8.Index].Value) + "','"
                                         + dataGridView1.Rows[i].Cells[Column2.Index].Value.ToString() + "','"
                                         + dataGridView1.Rows[i].Cells[DtUnit.Index].Value.ToString() + "','"
                                         + st + "','" + dataGridView1.Rows[i].Cells[DYear.Index].Value.ToString() + "','"
                                         + Convert.ToString(dataGridView1.Rows[i].Cells[colItemCode.Index].Value) + "')");

                                    }

                                    for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                                    {
                                        dataGridView1.Rows[i].Cells[Column5.Index].Value = Convert.ToString(Convert.ToDouble(dataGridView1.Rows[i].Cells[Column5.Index].Value)
                                           - Convert.ToDouble(dataGridView1.Rows[i].Cells[Column2.Index].Value));
                                    }

                                    isupdate = true;
                                    Database.mycmdBuilder = Database.GetCmdBuilderCommand(Database.myadapter);
                                    Database.CommitTransaction();
                                    blntran = false;
                                    Database.Closeconnection();

                                    
                                    if (isupdate == true)
                                        MessageBox.Show("Subsidary Order No " + textSrNo.Text + " is updated succesfully");

                                    #region PackingList
                                    if (DespatchCommonFunction.AllowMappingPackinglist(comboProcessorName.Text) > 0)
                                    {
                                        if (Database.OpenConnection(Utility.DespacthConnectionString))
                                        {
                                            // INVOICE-RULE: plus old packing list back, then minus the list now on this Sub- document.
                                            InvoiceRule_AdjustDespatchGodown(comboProcessorName.Text, desPackinglistBeforeUpdate, false);
                                            Database.GetExecuteNonQueryCommand("DELETE FROM MappPackingList WHERE CompanyName='" + comboProcessorName.Text + "' AND InvNo='Sub-" +
                                                textSrNo.Text + "' AND YR='" + textYear.Text + "'");

                                            Database.GetExecuteNonQueryCommand("INSERT INTO MappPackingList (CompanyName,InvNo,Yr,PackingListNo) VALUES('" +
                                            comboProcessorName.Text + "','Sub-" + textSrNo.Text + "','" + textYear.Text + "','" + desPackinglist + "')");
                                            InvoiceRule_AdjustDespatchGodown(comboProcessorName.Text, desPackinglist, true);
                                        }
                                    }
                                    #endregion

                                    ClearAll();
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (blntran)
                    Database.RollBackTransaction();
                ErrorMessageBox.Show(ex);
            }
        }

        private void textSrNo_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            try
            {
                if (Database.OpenConnection(Utility.DespacthConnectionString))
                {
                    string subsidiarybuyer = "";
                    string subsidiaryyear = "";
                    if (textSrNo.Text.Length == 0)
                        return;

                    double number1 = 0;
                    bool canConvert = double.TryParse(textSrNo.Text, out number1);
                    if (canConvert == false)
                    {
                        MessageBox.Show("Enter Proper Sr No", "ERP", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                        return;

                    }

                    Database.myreader = Database.GetExecuteReaderCommand("select * from subsidiaryChallanIssuemulti WITH (NOLOCK)  where srno = " +
                        textSrNo.Text + " and processorname = '" + comboProcessorName.Text + "' and year = '"
                        + textYear.Text + "'");
                    while (Database.myreader.Read())
                    {

                        subsidiarybuyer = Database.myreader[1].ToString();
                        dateTimeIssue.Value = Convert.ToDateTime(Database.myreader[2].ToString());
                        textLrNo.Text = Database.myreader[3].ToString();
                        dateTimeLR.Value = Convert.ToDateTime(Database.myreader[4].ToString());
                        textQtyDesc.Text = Database.myreader[5].ToString();
                        textQtyRemark.Text = Database.myreader[6].ToString();
                        comboType.Text = Database.myreader[7].ToString();
                        textVATPer.Text = Database.myreader[8].ToString();
                        textVatAmount.Text = Database.myreader[9].ToString();
                        textTotalAmount.Text = Database.myreader[10].ToString();
                        textNatureofProcessing.Text = Database.myreader[11].ToString();
                        textVehicleNo.Text = Database.myreader[12].ToString();
                        comboNameofTransporter.Text = Database.myreader[13].ToString();
                        textPlace.Text = Database.myreader[14].ToString();
                        datePlacedate.Value = Convert.ToDateTime(Database.myreader[15].ToString());
                        textDriverName.Text = Database.myreader[16].ToString();
                        textDriverAddress.Text = Database.myreader[17].ToString();
                        textDestination.Text = Database.myreader[18].ToString();
                        textDriverLicNo.Text = Database.myreader[19].ToString();
                        textLicenceIssuing.Text = Database.myreader[20].ToString();
                        comboNature.Text = Database.myreader[21].ToString();
                        comboPlaceOrigin.Text = Database.myreader[22].ToString();
                        subsidiaryyear = Database.myreader[26].ToString();
                        btnUpdate.Enabled = true;
                        btnSave.Enabled = false;
                        textSrNo.Enabled = false;
                    }
                    Database.myreader.Close();
                    if (!comboSubsidiaryBuyer.Items.Contains(subsidiarybuyer))
                        comboSubsidiaryBuyer.Items.Add(subsidiarybuyer);
                    comboSubsidiaryBuyer.Text = subsidiarybuyer;
                    //comboSubsidiaryyear.Text = subsidiaryyear;
                    dataGridView1.Rows.Clear();
                    Database.OpenConnection(Utility.DespacthConnectionString);
                    object j = Database.GetExecuteScalarCommand_return("select count(*) from subsidiaryChallanIssueItemmulti WITH (NOLOCK)  where srno = " +
                        textSrNo.Text + " and processorname = '" + comboProcessorName.Text + "' and year = '"
                        + textYear.Text + "' and subsidiarybuyer = '" + comboSubsidiaryBuyer.Text + "'");
                    int TotalCount = Convert.ToInt32(j);
                    flagupdate = true;
                    if (TotalCount > 0)
                    {
                        IsUpdate = true;
                        dataGridView1.Rows.Add(TotalCount);
                        int i = 0;

                        Database.myreader = Database.GetExecuteReaderCommand("select * from subsidiaryChallanIssueItemmulti WITH (NOLOCK)  where srno = " +
                        textSrNo.Text + " and processorname = '" + comboProcessorName.Text + "' and year = '"
                        + textYear.Text + "' and subsidiarybuyer = '" + comboSubsidiaryBuyer.Text + "'");
                        while (Database.myreader.Read())
                        {
                            dataGridView1.Rows[i].Cells[DYear.Index].Value = Database.myreader[12].ToString();
                            dataGridView1.Rows[i].Cells[DtMainChallanNo.Index].Value = Database.myreader[4].ToString();
                            dataGridView1.Rows[i].Cells[Column7.Index].Value = Database.myreader[5].ToString();
                            dataGridView1.Rows[i].Cells[DtCommodity.Index].Value = Database.myreader[6].ToString();
                            dataGridView1.Rows[i].Cells[colMainGrp.Index].Value = DespatchCommonFunction.GetDeptt(Database.myreader["ItemCode"].ToString());
                            dataGridView1.Rows[i].Cells[colItemCode.Index].Value = Convert.ToString(Database.myreader["ItemCode"]);
                            dataGridView1.Rows[i].Cells[Column9.Index].Value = Database.myreader[7].ToString();
                            dataGridView1.Rows[i].Cells[Column8.Index].Value = Database.myreader[8].ToString();
                            dataGridView1.Rows[i].Cells[Column2.Index].Value = Database.myreader[9].ToString();
                            dataGridView1.Rows[i].Cells[DtUnit.Index].Value = Database.myreader[10].ToString();
                            dataGridView1.Rows[i].Cells[Column5.Index].Value = Convert.ToDouble(Utility.ConverttoInt(Database.myreader[11].ToString())) + Convert.ToDouble(Utility.ConverttoInt(Database.myreader[9].ToString()));
                            i++;
                        }
                        Database.myreader.Close();
                        IsUpdate = false;

                    }
                    Calculation();

                    Database.Closeconnection();

                }
            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
            }
        }

        void fillData()
        {

            if (Database.OpenConnection(Utility.DespacthConnectionString))
            {
                //Database.myadapter = Database.GetAdapterCommand("Select distinct(year) from subsidiarychallanitemmulti WITH (NOLOCK)  ");
                //Change By Ishan on date 18/04/2014-- query change for make year in desc order
                Database.myadapter = Database.GetAdapterCommand("Select distinct(year) from subsidiarychallanitemmulti WITH (NOLOCK) order by Year desc");
                DataTable dt = new DataTable();
                Database.myadapter.Fill(dt);
                DYear.DataSource = dt;
                DYear.ValueMember = dt.Columns[0].ColumnName.ToString();
                DYear.DisplayMember = dt.Columns[0].ColumnName.ToString();
            }

            //if (Database.OpenConnection(Utility.MaterialConnectionString))
            {

                Database.myadapter = Database.GetAdapterCommand("Select UnitName from MaterialProcessing.dbo.Unit order by UnitName");
                DataTable dt = new DataTable();
                Database.myadapter.Fill(dt);

                DtUnit.DataSource = dt;
                DtUnit.ValueMember = dt.Columns[0].ColumnName.ToString();
                DtUnit.DisplayMember = dt.Columns[0].ColumnName.ToString();
                #region ITem Master Added 22.06.2020
                dtIMaster = new DataTable();
                dtIMaster = DespatchCommonFunction.GetItemMaster(dateTimeIssue.Value.ToString("yyyy-MM-dd"), comboProcessorName.Text);
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

        private void btnInitliaze_Click(object sender, EventArgs e)
        {
            textSrNo.Enabled = true;
            btnSave.Enabled = true;
            btnUpdate.Enabled = false;
            ClearAll();
            textSrNo.Text = "";
        }

        private void dataGridView1_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            try
            {
                if (!isclear)
                {
                    if (IsUpdate == false)
                    {
                        if (dataGridView1.CurrentCell.ColumnIndex == DtMainChallanNo.Index) // For Commodity ComboBox
                        {
                            comboBox = e.Control as ComboBox;
                            comboBox.SelectedIndexChanged += new EventHandler(comboBox_SelectedIndexChanged);
                        }
                        if (dataGridView1.CurrentCell.ColumnIndex == DYear.Index) // For Year ComboBox
                        {
                            //ComboBox comboBox = e.Control as ComboBox;
                            //comboBox.SelectedIndexChanged += new EventHandler(comboBox1_SelectedIndexChanged);
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
            }
            catch (Exception ex)
            { }
        }

        void comboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!isclear)
            {

                try
                {

                    if (dataGridView1.CurrentCell.ColumnIndex == DtMainChallanNo.Index) // For Commodity ComboBox
                    {
                        ComboBox cb = ((ComboBox)sender);
                        if (Database.OpenConnection(Utility.DespacthConnectionString))
                        {
                            Database.myreader = Database.GetExecuteReaderCommand("select * from subsidiarychallanItemMulti WITH (NOLOCK)  where ProcessorName ='"
                                      + comboProcessorName.Text + "' and subsidiaryBuyer = '" + comboSubsidiaryBuyer.Text
                                      + "' and MainChallanNo = '"
                                      + cb.Text + "' and year = '"
                                      + dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[DYear.Index].Value + "'");
                            if (Database.myreader.Read())
                            {
                                dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[Column7.Index].Value = Database.myreader[4].ToString();
                                dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[DtCommodity.Index].Value = Database.myreader[5].ToString();
                                dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[Column9.Index].Value = Database.myreader[6].ToString();
                                dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[DtUnit.Index].Value = Database.myreader[8].ToString();
                                dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[Column5.Index].Value = Math.Round(Convert.ToDouble(Database.myreader[9]), FrmMain.ERPQtyDigit);
                                //dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[colItemCode.Index].Value = Convert.ToString(Database.myreader["ItemCode"]);
                            }
                            Database.myreader.Close();
                            Database.myreader = Database.GetExecuteReaderCommand("select subsidiaryyear from vw_subsidiaryChallanItem where ProcessorName ='"
                                      + comboProcessorName.Text + "' and subsidiaryBuyer = '" + comboSubsidiaryBuyer.Text
                                      + "' and MainChallanNo = '" + cb.Text + "' and year = '"
                                      + dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[DYear.Index].Value + "' ");
                            if (Database.myreader.Read())
                                textNatureofProcessing.Text = Database.myreader[0].ToString();
                            Database.myreader.Close();
                            Database.Closeconnection();
                        }

                    }
                    //else
                    //{
                    //    dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[Column7.Index].Value = string.Empty;
                    //    dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[DtCommodity.Index].Value = string.Empty;
                    //    dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[Column9.Index].Value = string.Empty;
                    //    dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[DtUnit.Index].Value = string.Empty;
                    //    dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[Column5.Index].Value = string.Empty;
                    //}
                }
                catch (Exception ex)
                {
                    ex.ToString();
                }
            }
        }

        public bool validate()
        {
            try
            {
                #region 19-08-2020 lock as per accounting voucher type
                if (!Utility.CheckAccountVoucherDateFreeze(dateTimeIssue.Value, comboProcessorName.Text, "sales"))
                {
                    MessageBox.Show(AccountCommonVariable.ErrMsg.DATE_FREEZE + "\nPlease check Invoice Date");
                    return false;
                }
                #endregion

                
                for (int i = 0; i < dataGridView1.Rows.Count; i++)
                {

                   
                   

                    if (Convert.ToDouble(dataGridView1.Rows[i].Cells[Column2.Index].Value) < 0)
                    {
                        MessageBox.Show("Dispatch quantity must be positive.");
                        return false;
                    }
                    if (Convert.ToDouble(dataGridView1.Rows[i].Cells[Column2.Index].Value) > Math.Round(Convert.ToDouble(dataGridView1.Rows[i].Cells[Column5.Index].Value), 2))
                    {
                        MessageBox.Show("Dispatch quantity must be less than balance quantity.");
                        return false;
                    }
                    #region check for Itemcode
                    //string mainchallano = Convert.ToString(dataGridView1.Rows[i].Cells[DtMainChallanNo.Index].Value);
                    //DateTime MainChalDt = Convert.ToDateTime(dataGridView1.Rows[i].Cells[Column7.Index].Value);
                    //string Itemcode = Convert.ToString(dataGridView1.Rows[i].Cells[colItemCode.Index].Value);
                    //if (Database.OpenConnection(Utility.DespacthConnectionString))
                    //{
                    //    DataTable dt = Database.GetDataTable("select * from subsidiarychallanitemMulti WITH (NOLOCK)  where ProcessorName ='"
                    //                      + comboProcessorName.Text + "' and subsidiaryBuyer = '" + comboSubsidiaryBuyer.Text
                    //                      + "' and MainChallanNo = '" + mainchallano + "' and year = '" + dataGridView1.Rows[i].Cells[DYear.Index].Value + "' and " +
                    //                      " MainChallanDate='" + MainChalDt.ToString("yyyy-MM-dd") + "' and ItemCode='" + Itemcode + "'");
                    //    if (dt.Rows.Count == 0)
                    //    {
                    //        MessageBox.Show("Please Check itemCode for Selected Main Challan No");
                    //        return false;
                    //    }
                    //}
                    #endregion
                }

                for (int j = 0; j < dataGridView1.Rows.Count; j++)
                {
                    double count = Convert.ToDouble(Utility.ConverttoInt(dataGridView1.Rows[j].Cells[Column2.Index].FormattedValue.ToString()));

                    for (int k = 0; k < j; k++)
                    {
                        if (dataGridView1.Rows[k].Cells[DYear.Index].FormattedValue.ToString().Trim() == dataGridView1.Rows[j].Cells[DYear.Index].FormattedValue.ToString().Trim() &&
                            dataGridView1.Rows[k].Cells[DtMainChallanNo.Index].FormattedValue.ToString().Trim() == dataGridView1.Rows[j].Cells[DtMainChallanNo.Index].FormattedValue.ToString().Trim())
                        {
                            if (update == false)
                                count += Convert.ToDouble(dataGridView1.Rows[k].Cells[Column2.Index].FormattedValue);
                        }
                    }
                    if (count > Convert.ToDouble(Utility.ConverttoInt(dataGridView1.Rows[j].Cells[Column5.Index].FormattedValue.ToString())))
                    {
                        MessageBox.Show("Dispatch quantity not more than balance quantity of Chalan no. =" + dataGridView1.Rows[j].Cells[DtMainChallanNo.Index].FormattedValue
                            + " for year ='" + dataGridView1.Rows[j].Cells[DYear.Index].FormattedValue + "'");
                        return false;
                    }
                }
                if (textNatureofProcessing.Text == "")
                {
                    MessageBox.Show("Please enter value in Nature of Processing.");
                    return false;
                }
                if (btnUpdate.Enabled)
                {
                    if (Database.OpenConnection(Utility.DespacthConnectionString))
                    {
                        for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                        {

                            Database.myreader = Database.GetExecuteReaderCommand("Select * from jobworkInvItem WITH (NOLOCK) " +
                                " where mainchallanno = '" + dataGridView1.Rows[i].Cells[DtMainChallanNo.Index].Value.ToString() + "' and Companyname = '"
                                + comboProcessorName.Text + "' and YR = '" + dataGridView1.Rows[i].Cells[DYear.Index].Value.ToString() + "' and challanno = '"
                                + textSrNo.Text + "' and mainchallandate='" + Convert.ToDateTime(dataGridView1.Rows[i].Cells[Column7.Index].Value).ToString("yyyy-MM-dd")
                                + "'");
                            if (Database.myreader.Read())
                            {
                                MessageBox.Show("Job Invoice is already generated.\nYou can't Update this challan no");
                                return false;
                            }
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

        private void dataGridView1_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (!isclear)
            {
                if (dataGridView1.IsCurrentCellDirty)
                {
                    dataGridView1.CommitEdit(DataGridViewDataErrorContexts.Commit);
                    if (dataGridView1.CurrentRow.Cells.IndexOf(dataGridView1.CurrentCell) == DYear.Index)
                    {
                        DataTable dt = new DataTable();
                        try
                        {
                            DataGridViewComboBoxCell cell = (DataGridViewComboBoxCell)dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[DYear.Index];
                            DataGridViewComboBoxCell cell2 = (DataGridViewComboBoxCell)dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[DtMainChallanNo.Index];
                            if (flagupdate == true)
                            {

                                Database.OpenConnection(Utility.DespacthConnectionString);
                                Database.myreader = Database.GetExecuteReaderCommand("select  mainchallanno from subsidiarychallanItemMulti where ProcessorName ='"
                                           + comboProcessorName.Text + "' and subsidiaryBuyer = '" + comboSubsidiaryBuyer.Text
                                           + "'  and year = '" + cell.FormattedValue + "'");

                                dt.Load(Database.myreader);
                            }
                            else
                            {
                                Database.OpenConnection(Utility.DespacthConnectionString);
                                Database.myreader = Database.GetExecuteReaderCommand("select  mainchallanno from subsidiarychallanItemMulti where ProcessorName ='"
                                           + comboProcessorName.Text + "' and subsidiaryBuyer = '" + comboSubsidiaryBuyer.Text
                                           + "'  and year = '" + cell.FormattedValue + "' and  BalancedQty>0 order by mainchallanno desc");

                                dt.Load(Database.myreader);
                            }


                            Database.myreader.Close();

                            if (dt.Rows.Count > 0 && cell2.Value == null)
                            {
                                cell2.DataSource = dt;
                                cell2.ValueMember = dt.Columns[0].ColumnName.ToString();
                                cell2.DisplayMember = dt.Columns[0].ColumnName.ToString();
                            }
                            else
                            {
                                cell2.DataSource = dt;
                                cell2.ValueMember = dt.Columns[0].ColumnName.ToString();
                                cell2.DisplayMember = dt.Columns[0].ColumnName.ToString();
                            }
                            if (dataGridView1.CurrentCell.ColumnIndex > Column8.Index)
                            {
                                if (dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[Column8.Index].Value.ToString() == "" || dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[Column8.Index].Value == null)
                                    dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[Column8.Index].Value = ".";
                            }
                        }
                        catch (Exception ex)
                        {
                            ex.ToString();
                        }

                    }
                    if (dataGridView1.CurrentRow.Cells.IndexOf(dataGridView1.CurrentCell) == colMainGrp.Index)
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
                }

            }
        }

        private void comboSubsidiaryBuyer_SelectedIndexChanged(object sender, EventArgs e)
        {
            dataGridView1.Rows.Clear();
        }

        private double Calculation()
        {
            double total = 0;

            for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
            {
                total += Math.Round(Convert.ToDouble(dataGridView1.Rows[i].Cells[Column2.Index].Value), FrmMain.ERPQtyDigit);
                //total = Math.Round(total, 0, MidpointRounding.AwayFromZero);
            }

            txttdq.Text = total.ToString();
            return total;
        }


        private void btnCalculation_Click(object sender, EventArgs e)
        {
            Calculation();
        }
        //Delete functionality started  added by Ramiz Malek on 09-03-2016
        private void btndelete_Click(object sender, EventArgs e)
        {
            try
            {
                delete = true;
                if (validate())
                {
                    bool isvalid = false;
                    if (Database.OpenConnection(Utility.DespacthConnectionString))
                    {
                        for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                        {

                            Database.myreader = Database.GetExecuteReaderCommand("Select * from subsidiarychallanitemMulti WITH (NOLOCK) " +
                                " where mainchallanno = '" + dataGridView1.Rows[i].Cells[DtMainChallanNo.Index].Value.ToString() + "' and Processorname = '"
                                + comboProcessorName.Text + "' and subsidiarybuyer = '" + comboSubsidiaryBuyer.Text
                                + "' and year = '" + dataGridView1.Rows[i].Cells[DYear.Index].Value.ToString() + "' and commodityname = '"
                                + dataGridView1.Rows[i].Cells[DtCommodity.Index].Value.ToString() + "'");
                            if (!Database.myreader.Read())
                                isvalid = true;
                            Database.myreader.Close();

                            Database.myreader = Database.GetExecuteReaderCommand("Select * from jobworkInvItem WITH (NOLOCK) " +
                                " where mainchallanno = '" + dataGridView1.Rows[i].Cells[DtMainChallanNo.Index].Value.ToString() + "' and Companyname = '"
                                + comboProcessorName.Text + "' and YR = '" + dataGridView1.Rows[i].Cells[DYear.Index].Value.ToString() + "' and challanno = '"
                                + textSrNo.Text + "' and mainchallandate='" + Convert.ToDateTime(dataGridView1.Rows[i].Cells[Column7.Index].Value).ToString("yyyy-MM-dd") + "'");
                            if (Database.myreader.Read())
                            {
                                MessageBox.Show("Job Invoice is already generated.\nYou can't delete this challan no");
                                return;
                            }
                        }

                    }

                    if (isvalid)
                        MessageBox.Show("Main Challan No is not matched with commodity Name");
                    else
                    {

                        if (textNatureofProcessing.Text == "")
                            MessageBox.Show("Please enter value in Nature of Processing");
                        else
                        {
                            bool isupdate = false;
                            DialogResult drs = MessageBox.Show("Do you want to delete Subsidary Challan  "
                                                                , "delete Challan"
                                              , MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                            if (drs.CompareTo(DialogResult.Yes) == 0)
                            {
                                if (Database.OpenConnection(Utility.DespacthConnectionString))
                                {
                                    Database.BeginTransaction();


                                    #region Subsidary order tables : subsidiarychallanMulti &  subsidiarychallanitemMulti

                                    #endregion

                                    //System.Collections.ArrayList ArrMainChallanNo = new System.Collections.ArrayList();
                                    //System.Collections.ArrayList ArrOrderQty = new System.Collections.ArrayList();
                                    //System.Collections.ArrayList ArrCommodity = new System.Collections.ArrayList();
                                    //System.Collections.ArrayList ArrYear = new System.Collections.ArrayList();
                                    DataTable dt = Database.GetDataTable("select *  from subsidiaryChallanIssueItemmulti where srno = " +
                                           textSrNo.Text + " and processorname = '" + comboProcessorName.Text + "' and year = '"
                                           + textYear.Text + "' and subsidiarybuyer = '" + comboSubsidiaryBuyer.Text + "'");
                                    for (int i = 0; i <= dt.Rows.Count - 1; i++)
                                    {
                                        string strsql = "update subsidiarychallanitemMulti set BalancedQty = BalancedQty + " + dt.Rows[i]["OrderQty"].ToString()
                                              + " where MainChallanNo = '"
                                              + dt.Rows[i]["MainChallanNo"].ToString() + "' and processorname = '" + dt.Rows[i]["ProcessorName"].ToString()
                                              + "' and year = '" + dt.Rows[i]["Year"].ToString() + "' and subsidiarybuyer = '"
                                              + dt.Rows[i]["SubsidiaryBuyer"].ToString() + "' and MainChallanDate='"
                                              + Convert.ToDateTime(dt.Rows[i]["MainChallanDate"]).ToString("yyyy-MM-dd")
                                              + "' and CommodityName='" + dt.Rows[i]["CommodityName"].ToString() + "'";
                                        Database.GetExecuteNonQueryCommand(strsql);
                                    }
                                    //while (Database.myreader.Read())
                                    //{
                                    //    Database.GetExecuteNonQueryCommand("update subsidiarychallanitemMulti set BalancedQty = BalancedQty + OrderQty   where MainChallanNo = '"
                                    //    + Database.myreader["MainChallanNo"].ToString() + "' and processorname = '" + Database.myreader["ProcessorName"].ToString()
                                    //    + "' and year = '" + Database.myreader["Year"].ToString() + "' and subsidiarybuyer = '"
                                    //    + Database.myreader["MainChallanNo"].ToString() + "'");
                                    //    //ArrMainChallanNo.Add();
                                    //    //ArrOrderQty.Add(Database.myreader[1].ToString());
                                    //    //ArrCommodity.Add(Database.myreader[2].ToString());
                                    //    //ArrYear.Add(Database.myreader[3].ToString());
                                    //}
                                    //Database.myreader.Close();


                                    Database.GetExecuteNonQueryCommand("delete from  subsidiarychallanissueitemMulti where srno = " +
                                      textSrNo.Text + " and processorname = '" + comboProcessorName.Text + "' and year = '"
                                       + textYear.Text + "' and subsidiarybuyer = '" + comboSubsidiaryBuyer.Text + "'");

                                    Database.GetExecuteNonQueryCommand("delete from  subsidiaryChallanissueMulti where srno = " +
                                      textSrNo.Text + " and processorname = '" + comboProcessorName.Text + "' and year = '"
                                       + textYear.Text + "' and subsidiarybuyer = '" + comboSubsidiaryBuyer.Text + "'");


                                    Database.mycmdBuilder = Database.GetCmdBuilderCommand(Database.myadapter);
                                    // int x = Database.myadapter.Update(dataset1.Tables[0]);
                                    if (isupdate == false)
                                        MessageBox.Show("Subsidary Order No " + textSrNo.Text + " is deleted succesfully");

                                    Database.CommitTransaction();
                                    Database.Closeconnection();
                                    ClearAll();
                                }
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

            //Delete functionality ended 
        }

        private void tsbtnAttachment_Click(object sender, EventArgs e)
        {
            if (this.textSrNo.Text.Length > 0)
            {
                Tools.DMSManager.OpenReference("DS_SC", string.Format("{0}/{1}/DS_SC/{2}",
                    AccountCommonFunction.GetCompanyCode(this.comboProcessorName.Text),
                    this.textYear.Text,
                    this.textSrNo.Text
                    ));
            }
        }

        private void dateTimeIssue_ValueChanged(object sender, EventArgs e)
        {
            //try
            //{
            //    if (Database.OpenConnection(Utility.DespacthConnectionString))
            //    {
            //        string strsql = "Select companyname from CompanyMaster where (invoicecategory = 'Domestic'  or invoicecategory = '*') " +
            //              " and isnull(IsFreeze,'no')='no' and (('" + dateTimeIssue.Value.ToString("yyyy-MM-dd") +
            //               "' between fromdate and todate) or (fromdate <= '" +
            //               dateTimeIssue.Value.ToString("yyyy-MM-dd") + "' and todate is null ))  order by CompanyName";
            //        DespatchCommonFunction.Loadcombo(comboSubsidiaryBuyer, strsql, "CompanyName", "CompanyName", Utility.DespacthConnectionString);
            //    }
            //}
            //catch (Exception ex)
            //{
            //    ex.Message.ToString();
            //}
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

