using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace ERP
{
    public partial class frmSubsidaryChallanIssue : Form
    {
        public frmSubsidaryChallanIssue()
        {
            InitializeComponent();
            Form_Load();
        }

        private void Form_Load()
        {
            btnUpdate.Enabled = false;

            dateMainChallanDate.Format = DateTimePickerFormat.Custom;
            dateMainChallanDate.CustomFormat = DespatchCommonFunction.ERPDateSettings();

            datePlacedate.Format = DateTimePickerFormat.Custom;
            datePlacedate.CustomFormat = DespatchCommonFunction.ERPDateSettings();


            dateTimeIssue.Format = DateTimePickerFormat.Custom;
            dateTimeIssue.CustomFormat = DespatchCommonFunction.ERPDateSettings();

            dateTimeLR.Format = DateTimePickerFormat.Custom;
            dateTimeLR.CustomFormat = DespatchCommonFunction.ERPDateSettings();

            if (Database.OpenConnection(Utility.DespacthConnectionString))
            {
                DespatchCommonFunction.GetCompanyList(comboProcessorName);
                DespatchCommonFunction.GetBuyerName(comboSubsidiaryBuyer, Utility.DespacthConnectionString, dateMainChallanDate.Value);
                DespatchCommonFunction.GetTransportMaster(comboNameofTransporter);
                Database.Closeconnection();

            }
            textYear.Text = frmDefaultVale.Year;
            comboProcessorName.Text = frmDefaultVale.CompanyName;

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
                string strsql = ("select Distinct(commodityname) as commodityname from commodity with (NoLock) where companyname = '"
                  + comboProcessorName.Text + "' and nToDate is null order by commodityname");
                DespatchCommonFunction.Loadcombo(comboCommodity, strsql, "commodityname", "commodityname", Utility.DespacthConnectionString);

                strsql = ("select Distinct(SubsidaryName) as SubsidaryName from suborder with (NoLock) where processorname = '"
                     + comboProcessorName.Text + "' order by SubsidaryName");
                DespatchCommonFunction.Loadcombo(comboSubsidiaryBuyer, strsql, "SubsidaryName", "SubsidaryName", Utility.DespacthConnectionString);
                Database.Closeconnection();
            }
            if (comboSubsidiaryBuyer.Items.Count > 0)
                comboSubsidiaryBuyer.SelectedIndex = 0;
        }

        private void comboSubsidiaryBuyer_SelectedIndexChanged(object sender, EventArgs e)
        {
            comboChallanNo.Items.Clear();
            comboChallanNo.Text = "";
            if (Database.OpenConnection(Utility.DespacthConnectionString))
            {
                string strsql = ("select MainChallan from suborder with (NoLock) where processorname = '"
                     + comboProcessorName.Text + "' and SubsidaryName = '" + comboSubsidiaryBuyer.Text + "'");
                DespatchCommonFunction.Loadcombo(comboChallanNo, strsql, "MainChallan", "MainChallan", Utility.DespacthConnectionString);
                Database.Closeconnection();
            }

            if (comboChallanNo.Items.Count > 0)
                comboChallanNo.SelectedIndex = 0;

        }

        private void comboChallanNo_SelectedIndexChanged(object sender, EventArgs e)
        {
            textNatureofProcessing.Text = "";
            textQtyUnit.Text = "";
            textBalanceQty.Text = "";

            if (Database.OpenConnection(Utility.DespacthConnectionString))
            {
                Database.myreader = Database.GetExecuteReaderCommand("select Item,Unit,Balanceqty,MainChallanDate from suborder with (NoLock)  where processorname = '"
                     + comboProcessorName.Text + "' and SubsidaryName = '" + comboSubsidiaryBuyer.Text + "' and MainChallan = '"
                      + comboChallanNo.Text + "'");
                while (Database.myreader.Read())
                {
                    textNatureofProcessing.Text = Database.myreader[0].ToString();
                    textQtyUnit.Text = Database.myreader[1].ToString();
                    textBalanceQty.Text = Database.myreader[2].ToString();
                    dateMainChallanDate.Value = Convert.ToDateTime(Database.myreader[3].ToString());
                    //textBalanceQty.Text = Convert.ToString(Convert.ToDouble(Utility.ConverttoInt(Database.myreader[2].ToString())) + Convert.ToDouble(Utility.ConverttoInt(Database.myreader[4].ToString())));
                }
                Database.myreader.Close();
                Database.Closeconnection();
            }

        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            try
            {
                if (validate())
                {
                    if (textNatureofProcessing.Text == "")
                        MessageBox.Show("Please enter value in Nature of Processing");
                    else
                    {
                        int VenCnt = DespatchCommonFunction.CustomerLedgerName(comboSubsidiaryBuyer.Text, comboProcessorName.Text);
                        if (VenCnt == 0)
                        {
                            MessageBox.Show("No Account Ledger Found for this customer.\nPlease Ask Account Dept to create Ledger for this customer:- [" + comboSubsidiaryBuyer.Text + "]");
                            return;
                        }
                        DialogResult drs = MessageBox.Show("Do you want to generate Subsidary Challan  "
                                                            , "Subsidary Challan"
                                          , MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                        if (drs.CompareTo(DialogResult.Yes) == 0)
                        {
                            if (Database.OpenConnection(Utility.DespacthConnectionString))
                            {
                                Database.BeginTransaction();
                                int srno = 0;
                                Database.myreader = Database.GetExecuteReaderCommand("SELECT isnull(SRNO,0)  FROM (select srno,processorname,yr from SubsidiaryChallan"
                                + " WHERE processorname='" + comboProcessorName.Text + "'AND  yr = '" + textYear.Text + "'  UNION ALL "
                                + " select srno,processorname,yEAr from subsidiaryChallanissueMulti  WHERE processorname='" + comboProcessorName.Text
                                + "' AND  yEAr = '" + textYear.Text + "')AS TEMP  ORDER BY SRNO DESC ");
                                if (Database.myreader.Read())
                                    srno = Convert.ToInt32(Database.myreader[0].ToString());
                                Database.myreader.Close();
                                srno = srno + 1;
                                string InsuranceApp = "";
                                string Insurance = "";
                                if (checkInsurance.Checked)
                                {
                                    InsuranceApp = "yes";
                                    Insurance = textInsurance.Text;
                                }
                                else
                                    InsuranceApp = "no";

                                Database.myadapter = Database.GetAdapterCommand("select * from SubsidiaryChallan");
                                DataSet dataset1 = new DataSet();
                                Database.myadapter.Fill(dataset1, "SubsidiaryChallan");

                                //insert data in database 
                                DataRow dr;
                                dr = dataset1.Tables[0].NewRow();
                                dr[0] = comboProcessorName.Text.ToUpper();
                                dr[1] = comboSubsidiaryBuyer.Text.ToUpper();
                                dr[2] = comboChallanNo.Text;
                                dr[3] = dateMainChallanDate.Value.ToString(DespatchCommonFunction.ERPDBDateSettings());
                                dr[4] = textLrNo.Text;
                                dr[5] = dateTimeLR.Value.ToString(DespatchCommonFunction.ERPDBDateSettings());
                                dr[6] = dateTimeIssue.Value.ToString(DespatchCommonFunction.ERPDBDateSettings());
                                dr[7] = textQtyDispatched.Text;
                                dr[8] = (Convert.ToDouble(textBalanceQty.Text)
                                                  - Convert.ToDouble(textQtyDispatched.Text));
                                dr[9] = textNatureofProcessing.Text.ToUpper();
                                dr[10] = comboNameofTransporter.Text.ToUpper();
                                dr[11] = textVehicleNo.Text.ToUpper();
                                dr[12] = textPlace.Text;
                                dr[13] = datePlacedate.Value.ToString(DespatchCommonFunction.ERPDBDateSettings());
                                dr[14] = textYear.Text;
                                dr[15] = srno;
                                dr[17] = textQtyDesc.Text;
                                dr[18] = textDriverName.Text;
                                dr[19] = textDriverAddress.Text;
                                dr[20] = textDriverLicNo.Text;
                                dr[21] = textLicenceIssuing.Text;
                                dr[22] = comboNature.Text;
                                dr[23] = comboPlaceOrigin.Text;
                                dr[24] = comboCommodity.Text;
                                dr[25] = textTHeading.Text;
                                dr[26] = comboType.Text;
                                dr[27] = textVATPer.Text;
                                dr[28] = textVatAmount.Text;
                                dr[29] = textTotalAmount.Text;
                                dr[30] = textNoofPkg.Text;
                                dr[31] = textQtyRemark.Text;
                                dr[32] = textDestination.Text;
                                dr[34] = Insurance;
                                dr[35] = InsuranceApp;
                                dataset1.Tables[0].Rows.Add(dr);

                                UpdateInsurancePolicy(Convert.ToDouble(Utility.ConverttoInt(textTotalAmount.Text)), checkInsurance.Checked);
                                Database.mycmdBuilder = Database.GetCmdBuilderCommand(Database.myadapter);
                                int x = Database.myadapter.Update(dataset1.Tables[0]);
                                if (x > 0)
                                {

                                    Database.GetExecuteNonQueryCommand("update suborder set balanceqty = Balanceqty - " + textQtyDispatched.Text
                                                + " where mainchallan = '" + comboChallanNo.Text + "' and Processorname = '"
                                                + comboProcessorName.Text + "' and subsidaryname = '" + comboSubsidiaryBuyer.Text
                                                + "' and yr = '" + textYear.Text + "'");
                                    textBalanceQty.Text = Convert.ToString(Convert.ToDouble(textBalanceQty.Text)
                                                   - Convert.ToDouble(textQtyDispatched.Text));
                                    MessageBox.Show("Subsidary Order No " + srno.ToString() + " is generated succesfully");
                                    Database.CommitTransaction();
                                    Clear();
                                }
                                Database.Closeconnection();
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
            //frmPrintSubsidaryChallan frm = new frmPrintSubsidaryChallan();
            //frm.Show();
            TabClass.OpenForm(new frmPrintSubsidaryChallan());
        }
        private void btnUpdate_Click(object sender, EventArgs e)
        {
            try
            {
                int VenCnt = DespatchCommonFunction.CustomerLedgerName(comboSubsidiaryBuyer.Text, comboProcessorName.Text);
                if (VenCnt == 0)
                {
                    MessageBox.Show("No Account Ledger Found for this customer.\nPlease Ask Account Dept to create Ledger for this customer:- [" + comboSubsidiaryBuyer.Text + "]");
                    return;
                }
                if (Database.OpenConnection(Utility.DespacthConnectionString))
                {
                    Database.BeginTransaction();

                    string CheckInsurane = "";
                    double TotalAmt = 0;
                    Database.myreader = Database.GetExecuteReaderCommand("select TotalAmount,CheckInsurance from SubsidiaryChallan where srno = " + textSrNo.Text
                    + " and processorname = '" + comboProcessorName.Text + "' and yr = '"
                    + textYear.Text + "'");
                    if (Database.myreader.Read())
                    {
                        TotalAmt = Convert.ToDouble(Utility.ConverttoInt(Database.myreader[0].ToString()));
                        CheckInsurane = Database.myreader[1].ToString();
                    }
                    Database.myreader.Close();

                    if (checkInsurance.Checked == false && CheckInsurane == "yes")
                        UpdateInsurancePolicy(-TotalAmt, true);
                    else if (checkInsurance.Checked == true && CheckInsurane == "no")
                        UpdateInsurancePolicy(Convert.ToDouble(textTotalAmount.Text), true);
                    else if (checkInsurance.Checked)
                        UpdateInsurancePolicy(Convert.ToDouble(textTotalAmount.Text) - TotalAmt, true);
                    if (checkInsurance.Checked)
                    {
                        CheckInsurane = "yes";
                    }
                    else
                        CheckInsurane = "no";
                    double QtyDispatched = 0;
                    Database.myreader = Database.GetExecuteReaderCommand("select QuantiyDispatched from SubsidiaryChallan where srno = "
                         + textSrNo.Text + " and processorname = '" + comboProcessorName.Text + "' and yr = '"
                         + textYear.Text + "'");
                    while (Database.myreader.Read())
                        QtyDispatched = Convert.ToDouble(Database.myreader[0].ToString());
                    Database.myreader.Close();

                    Database.GetExecuteNonQueryCommand("update suborder set balanceqty = Balanceqty + " + QtyDispatched
                                + " where mainchallan = '" + comboChallanNo.Text + "' and Processorname = '"
                                + comboProcessorName.Text + "' and subsidaryname = '" + comboSubsidiaryBuyer.Text
                                + "' and yr = '" + textYear.Text + "'");
                    Database.GetExecuteNonQueryCommand("delete from SubsidiaryChallan where srno = " + textSrNo.Text
                    + " and processorname = '" + comboProcessorName.Text + "' and yr = '"
                    + textYear.Text + "'");

                    Database.myadapter = Database.GetAdapterCommand("select * from SubsidiaryChallan");
                    DataSet dataset1 = new DataSet();
                    Database.myadapter.Fill(dataset1, "SubsidiaryChallan");

                    //insert data in database 
                    DataRow dr;
                    dr = dataset1.Tables[0].NewRow();
                    dr[0] = comboProcessorName.Text.ToUpper();
                    dr[1] = comboSubsidiaryBuyer.Text.ToUpper();
                    dr[2] = comboChallanNo.Text;
                    dr[3] = dateMainChallanDate.Value.ToString(DespatchCommonFunction.ERPDBDateSettings());
                    dr[4] = textLrNo.Text;
                    dr[5] = dateTimeLR.Value.ToString(DespatchCommonFunction.ERPDBDateSettings());
                    dr[6] = dateTimeIssue.Value.ToString(DespatchCommonFunction.ERPDBDateSettings());
                    dr[7] = textQtyDispatched.Text;
                    dr[8] = QtyDispatched + (Convert.ToDouble(textBalanceQty.Text)
                                      - Convert.ToDouble(textQtyDispatched.Text));
                    dr[9] = textNatureofProcessing.Text.ToUpper();
                    dr[10] = comboNameofTransporter.Text.ToUpper();
                    dr[11] = textVehicleNo.Text.ToUpper();
                    dr[12] = textPlace.Text;
                    dr[13] = datePlacedate.Value.ToString(DespatchCommonFunction.ERPDBDateSettings());
                    dr[14] = textYear.Text;
                    dr[15] = textSrNo.Text;
                    dr[17] = textQtyDesc.Text;
                    dr[18] = textDriverName.Text;
                    dr[19] = textDriverAddress.Text;
                    dr[20] = textDriverLicNo.Text;
                    dr[21] = textLicenceIssuing.Text;
                    dr[22] = comboNature.Text;
                    dr[23] = comboPlaceOrigin.Text;
                    dr[24] = comboCommodity.Text;
                    dr[25] = textTHeading.Text;
                    dr[26] = comboType.Text;
                    dr[27] = textVATPer.Text;
                    dr[28] = textVatAmount.Text;
                    dr[29] = textTotalAmount.Text;
                    dr[30] = textNoofPkg.Text;
                    dr[31] = textQtyRemark.Text;
                    dr[32] = textDestination.Text;
                    dr[34] = textInsurance.Text;
                    dr[35] = CheckInsurane;
                    dataset1.Tables[0].Rows.Add(dr);
                    Database.mycmdBuilder = Database.GetCmdBuilderCommand(Database.myadapter);
                    int x = Database.myadapter.Update(dataset1.Tables[0]);
                    if (x > 0)
                    {
                        MessageBox.Show("Subsidary Order No " + textSrNo.Text + " is updated succesfully");

                        Database.GetExecuteNonQueryCommand("update suborder set balanceqty = Balanceqty - " + textQtyDispatched.Text
                                + " where mainchallan = '" + comboChallanNo.Text + "' and Processorname = '"
                                + comboProcessorName.Text + "' and subsidaryname = '" + comboSubsidiaryBuyer.Text
                                + "' and yr = '" + textYear.Text + "'");
                        textBalanceQty.Text = Convert.ToString(Convert.ToDouble(textBalanceQty.Text)
                                       - Convert.ToDouble(textQtyDispatched.Text) + QtyDispatched);

                        Database.CommitTransaction();
                        Clear();
                    }
                    Database.Closeconnection();
                }

            }
            catch (Exception ex)
            {
                Database.RollBackTransaction();
                ErrorMessageBox.Show(ex);
            }
        }

        private void textSrNo_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (Database.OpenConnection(Utility.DespacthConnectionString))
            {
                string processorname = "";
                string subsidiarybuyer = "";
                string challano = "";
                string Commodity = "";
                bool IsCheckIsurance = false;
                int number1 = 0;
                bool canConvert = int.TryParse(textSrNo.Text, out number1);

                if (canConvert == false)
                {
                    MessageBox.Show("Enter Proper Sr No", "ERP");
                    return;
                }
                Database.myreader = Database.GetExecuteReaderCommand("select * from SubsidiaryChallan with (NoLock) where srno = " +
                    textSrNo.Text + " and processorname = '" + comboProcessorName.Text + "' and yr = '"
                    + textYear.Text + "'");
                while (Database.myreader.Read())
                {

                    processorname = Database.myreader[0].ToString();
                    subsidiarybuyer = Database.myreader[1].ToString();
                    challano = Database.myreader[2].ToString();
                    dateMainChallanDate.Value = Convert.ToDateTime(Database.myreader[3].ToString());
                    textLrNo.Text = Database.myreader[4].ToString();
                    dateTimeLR.Value = Convert.ToDateTime(Database.myreader[5].ToString());
                    dateTimeIssue.Value = Convert.ToDateTime(Database.myreader[6].ToString());
                    textQtyDispatched.Text = Database.myreader[7].ToString();
                    textNatureofProcessing.Text = Database.myreader[9].ToString();
                    comboNameofTransporter.Text = Database.myreader[10].ToString();
                    textVehicleNo.Text = Database.myreader[11].ToString();
                    textPlace.Text = Database.myreader[12].ToString();
                    datePlacedate.Value = Convert.ToDateTime(Database.myreader[13].ToString());
                    textYear.Text = Database.myreader[14].ToString();
                    textQtyDesc.Text = Database.myreader[17].ToString();
                    textDriverName.Text = Database.myreader[18].ToString();
                    textDriverAddress.Text = Database.myreader[19].ToString();
                    textDriverLicNo.Text = Database.myreader[20].ToString();
                    textLicenceIssuing.Text = Database.myreader[21].ToString();
                    comboNature.Text = Database.myreader[22].ToString();
                    comboPlaceOrigin.Text = Database.myreader[23].ToString();
                    Commodity = Database.myreader[24].ToString();
                    textTHeading.Text = Database.myreader[25].ToString();
                    comboType.Text = Database.myreader[26].ToString();
                    textVATPer.Text = Database.myreader[27].ToString();
                    textVatAmount.Text = Database.myreader[28].ToString();
                    textTotalAmount.Text = Database.myreader[29].ToString();
                    textNoofPkg.Text = Database.myreader[30].ToString();
                    textQtyRemark.Text = Database.myreader[31].ToString();
                    textDestination.Text = Database.myreader[32].ToString();
                    if (Database.myreader[35].ToString() == "yes")
                        IsCheckIsurance = true;
                    btnUpdate.Enabled = true;
                    btnSave.Enabled = false;
                    textSrNo.Enabled = false;
                }
                Database.myreader.Close();
                Database.Closeconnection();
                if (IsCheckIsurance)
                    checkInsurance.Checked = true;
                else
                    checkInsurance.Checked = false;
                comboProcessorName.Text = processorname;
                comboSubsidiaryBuyer.Text = subsidiarybuyer;
                comboChallanNo.Text = challano;
                comboCommodity.Text = Commodity;
            }
        }

        private void btnInitliaze_Click(object sender, EventArgs e)
        {
            Clear();
        }

        private void comboCommodity_SelectedIndexChanged(object sender, EventArgs e)
        {
            textTHeading.Text = "";
            Database.OpenConnection(Utility.DespacthConnectionString);
            Database.myreader = Database.GetExecuteReaderCommand("select THeading from commodity where CommodityName ='"
                              + comboCommodity.Text + "'");
            while (Database.myreader.Read())
                textTHeading.Text = Database.myreader[0].ToString();
            Database.myreader.Close();
            Database.Closeconnection();
        }

        private void comboCommodity_KeyUp(object sender, KeyEventArgs e)
        {
            textTHeading.Text = "";
            Database.OpenConnection(Utility.DespacthConnectionString);
            Database.myreader = Database.GetExecuteReaderCommand("select THeading from commodity where CommodityName ='"
                              + comboCommodity.Text + "'");
            while (Database.myreader.Read())
                textTHeading.Text = Database.myreader[0].ToString();
            Database.myreader.Close();
            Database.Closeconnection();
        }

        private void checkInsurance_CheckedChanged(object sender, EventArgs e)
        {
            if (checkInsurance.Checked == true)
            {
                groupInsurance.Visible = true;
                if (Database.OpenConnection(Utility.DespacthConnectionString))
                {
                    Database.myreader = Database.GetExecuteReaderCommand("Select top 1 policyno from InsurancePolicy where companyname = '"
                        + comboProcessorName.Text + "' order by srno desc");
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
                  + " where policyno ='" + textInsurance.Text + "' and companyname = '" + comboProcessorName.Text + "'");
            }
        }

        public bool validate()
        {
            try
            {
                #region 19-08-2020 lock as per accounting voucher type                
                if (!Utility.CheckAccountVoucherDateFreeze(dateTimeIssue.Value, comboProcessorName.Text, "sales"))
                {
                    MessageBox.Show(AccountCommonVariable.ErrMsg.DATE_FREEZE + "\nPlease check Issue Date");
                    return false;
                }
                #endregion

                if (Convert.ToDouble(Utility.ConverttoInt(textQtyDispatched.Text)) < 0)
                {
                    MessageBox.Show("Dispatch quantity must be positive.");
                    return false;
                }
                if (Convert.ToDouble(Utility.ConverttoInt(textQtyDispatched.Text)) > Convert.ToDouble(Utility.ConverttoInt(textBalanceQty.Text)))
                {
                    MessageBox.Show("Dispatch quantity not more than balance quantity.");
                    return false;
                }

                return true;
            }
            catch (Exception)
            {
                return false;
                throw;
            }
        }

        public void Clear()
        {
            try
            {
                textSrNo.Enabled = true;
                comboSubsidiaryBuyer.Text = "";
                comboChallanNo.Text = "";
                textQtyDispatched.Text = "";
                textQtyUnit.Text = "";
                textBalanceQty.Text = "";
                textNoofPkg.Text = "";
                textQtyRemark.Text = "";
                textQtyDesc.Text = "";
                comboType.Text = "";
                textVATPer.Text = "";
                textVatAmount.Text = "";
                textTotalAmount.Text = "";
                textNatureofProcessing.Text = "";
                textVehicleNo.Text = "";
                comboNameofTransporter.Text = "";
                textDriverName.Text = "";
                textDriverAddress.Text = "";
                textDriverLicNo.Text = "";
                textDestination.Text = "";
                comboNature.Text = "";
                comboPlaceOrigin.Text = "";
                textDriverName.Text = "";
                btnSave.Enabled = true;
                btnUpdate.Enabled = false;
            }
            catch (Exception)
            {

                throw;
            }
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

    }
}