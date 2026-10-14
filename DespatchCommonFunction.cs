using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using ERP.Classes;

namespace ERP
{
    public class DespatchCommonFunction : Tools.ToolsCommonFunction
    {
        public static string CompanyGSTNo(String Companyname)
        {
            string newgstno = Convert.ToString(Database.GetExecuteNonQueryCommand_retrun("select isnull(NewGSTNo,'') as NewGSTNo from FactoryInfo where name='"
                        + Companyname + "'"));

            return newgstno;
        }
        public static void GetCompanyList(ComboBox objcom)
        {
            Loadcombo(objcom, "Select name from factoryinfo   order by OrderPosition", "name", "name", Utility.DespacthConnectionString); //where (name in ( select CompanyName from Loginentry..LoginRightAccount L where L.LoginID='" + FrmMain.UserName + "') or name in ( select CompName from  defaultvalue  where LoginName='" + FrmMain.UserName + "'))
            objcom.SelectedIndex = 0;
            objcom.SelectedIndex = -1;
        }
        /// <summary>
        /// Get list of Item Deptt
        /// </summary>
        /// <returns>dataTable</returns>        


        public static void InsertJV(string Invno, double IncentiveAmount, string companyname, string yr, DateTime invdate, int Srno = 0)
        {
            try
            {
                if (Database.OpenConnection(Utility.DespacthConnectionString))
                {

                    int JVNo = Convert.ToInt16(Database.GetExecuteNonQueryCommand_retrun("select JournalNo from Materialprocessing.dbo.JournalItem_temp where billno ='" + Invno
                        + "' and CompanyName = '" + companyname + "' and yr = '" + yr + "'"));
                    Database.BeginTransaction();
                    Database.mycommand = new System.Data.SqlClient.SqlCommand();
                    Database.mycommand.Parameters.Clear();
                    Database.mycommand.CommandType = CommandType.StoredProcedure;
                    Database.mycommand.Connection = Database.myconn;
                    Database.mycommand.Transaction = Database.mytransaction;
                    Database.mycommand.CommandText = "SP_Insert_Update_ACJournal";


                    Database.mycommand.Parameters.AddWithValue("@CompanyName", companyname);
                    Database.mycommand.Parameters.AddWithValue("@year", yr);
                    Database.mycommand.Parameters.AddWithValue("@sysdate", DateTime.Now);
                    Database.mycommand.Parameters.AddWithValue("@IncentiveAmt", IncentiveAmount);

                    Database.mycommand.Parameters.AddWithValue("@username", FrmMain.UserName);
                    Database.mycommand.Parameters.AddWithValue("@InvNo", Invno);
                    Database.mycommand.Parameters.AddWithValue("@InvDate", invdate.ToString("yyyy-MM-dd"));
                    Database.mycommand.Parameters.AddWithValue("@UpdateJVNo", JVNo);

                    if (JVNo == 0)
                        Database.mycommand.Parameters.AddWithValue("@Srno", 0);
                    else
                        Database.mycommand.Parameters.AddWithValue("@Srno", Srno);

                    Database.mycommand.ExecuteNonQuery();

                    Database.CommitTransaction();

                }
            }
            catch (Exception ex)
            {
                Database.RollBackTransaction();
                //   MessageBox.Show(ex.ToString());
            }
        }


        public static void GetDepttName(ComboBox objcom)
        {
            Loadcombo(objcom, "select DepttName from itemdeptt order by depttname", "DepttName", "DepttName", Utility.DespacthConnectionString);
        }

        /// <summary>
        /// Get list of Transport Master
        /// </summary>
        /// <returns>dataTable</returns>

        public static void GetTransportMaster(ComboBox objcom)
        {
            Loadcombo(objcom, "Select compname from TransportMaster order by compname", "compname", "compname", Utility.DespacthConnectionString);
        }
        public static DataTable GetRepresentiveList()
        {
            //// Get From cache
            //object obj = Cache.GetCache(string.Format("RepresentiveList-Despatch"));
            //if (obj != null)
            //    return ((DataTable)obj).Copy();

            // Get From Database
            DataTable dataTable = new DataTable("RepresentiveList");
            if (Database.OpenConnection(Utility.DespacthConnectionString))
            {
                //by Raj 26.06.2020 add Representative table in Select QRY
                //Database.GetExecuteReaderCommand(string.Format("select distinct Reprsentativename from CompanyMaster where ISNULL(Reprsentativename,'')!='' and LEN(Reprsentativename) > 2 order by Reprsentativename "));
                Database.GetExecuteReaderCommand(string.Format("select distinct RepresentativeName AS RepresentiveId, RepresentativeName AS RepresentiveName,RepresentativeType from MaterialProcessing.dbo.RepresentativeMaster with(Nolock) where RepresentativeType='Sales' ORDER BY RepresentativeName"));
                dataTable.Load(Database.myreader);
            }
            //if (dataTable.Rows.Count != 0) Cache.AddCache(dataTable, 0, string.Format("RepresentiveList-Despatch"));
            return dataTable.Copy();
        }

        /// <summary>
        /// GET COUNTRY LIS
        /// </summary>
        /// <param name="objcom"></param>
        public static void GetCountryList(ComboBox objcom)
        {
            Loadcombo(objcom, "select Country from CountryMaster order by Country", "Country", "Country", Utility.DespacthConnectionString);
            objcom.SelectedIndex = 0;
            objcom.SelectedIndex = -1;
        }

        public static void GetHSNCodeList(ComboBox objcom)
        {
            Loadcombo(objcom, "select distinct(THeading) from Commodity order by THeading", "THeading", "THeading", Utility.DespacthConnectionString);
            objcom.SelectedIndex = 0;
            objcom.SelectedIndex = -1;
        }


        /// <summary>
        /// Check Party name
        /// By Rikin on 30--12-2014
        /// </summary>
        /// <param name="buyer">selected party name s</param>
        /// <returns>Bool</returns>
        public static bool CheckBuyerNAme(string buyer)
        {
            if (Database.OpenConnection(Utility.DespacthConnectionString))
            {
                string _Buuyer = "";
                _Buuyer = (string)Database.ExecuteScalar("Select CompanyName from CompanyMaster where CompanyName = '"
                     + buyer + "'");
                if (_Buuyer.Length > 0)
                    return true;
            }
            return false;
        }
        public static void GetPortOfLoading(ComboBox objcom, string ConnectionString)
        {
            Loadcombo(objcom, "select loadPortname from loadingPort", "loadPortname", "loadPortname", ConnectionString);
        }
        public static void GetCurrencyCombo(ComboBox objcom, string ConnectionString)
        {
            Loadcombo(objcom, "select currency from currencytype", "currency", "currency", ConnectionString);
        }
        public static void GetVehicalNo(ComboBox objcom, string ConnectionString)
        {
            Loadcombo(objcom, "Select Distinct(vehicalNo) as vehicalNo from DomesticInvoice WITH (NOLOCK)  order by vehicalNo", "vehicalNo", "vehicalNo", ConnectionString);
        }

        public static void GetYear(ComboBox objcom, string ConnectionString)
        {
            Loadcombo(objcom, "select yr from materialprocessing..yearmaster order by srno desc", "Yr", "Yr", ConnectionString);
        }
        public static void GetPaymentTerms(ComboBox objcom, string ConnectionString)
        {
            Loadcombo(objcom, "select distinct paymenttermname from MaterialProcessing..PaymentTermsMaster where Type='Sales' and isdelflag=0 order by paymenttermname", "paymenttermname", "paymenttermname", ConnectionString);

        }
        public static void GetBuyerName(ComboBox objcom, string ConnectionString, DateTime fromdate)
        {
            string strsql = "Select companyname from companymaster where   " +
            " isnull(IsFreeze,'no')='no' and (('" + fromdate.ToString("yyyy-MM-dd") + "' between fromdate and todate) or (fromdate <= '" + fromdate.ToString("yyyy-MM-dd") +
            "' and  todate is null ))  and CompanyCode not in " +
            " ( select LCompanyCode from MaterialProcessing.dbo.LedgerMaster where (Todate is not null or IsAccClose='yes' ) and  companyname='"
            + frmDefaultVale.CompanyName + "'  and isnull(LCompanyCode,'')<>'') order by companyname";

            Loadcombo(objcom, strsql, "CompanyName", "CompanyName", ConnectionString);
        }
        public static void GetDestination(ComboBox objcom, string ConnectionString)
        {
            Loadcombo(objcom, "select Country from countrymaster", "Country", "Country", ConnectionString);
        }
        public static int CustomerLedgerName(string customerName, string CompanyName)
        {
            int Return = 0;

            if (customerName.Length == 0)
                return 0;
            if (CompanyName.Length == 0)
                CompanyName = frmDefaultVale.CompanyName;

            if (Database.OpenConnection(Utility.MaterialConnectionString))
            {
                string customercode = "";
                string gstin = "";
                string strsql = "select CompanyCode,NewGSTNo from Despatch.dbo.CompanyMaster where CompanyName='" + customerName + "'";
                Database.myreader = Database.GetExecuteReaderCommand(strsql);
                if (Database.myreader.Read())
                {
                    customercode = Convert.ToString(Database.myreader[0]);
                    gstin = Convert.ToString(Database.myreader["NewGSTNo"]);
                }
                Database.myreader.Close();

                strsql = " select count(*) cnt from LedgerMaster where isnull(LCompanyCode,'') = '" + customercode
                    + "' and companyname='" + CompanyName + "' and NewGSTNo='" + gstin + "'";
                Database.myreader = Database.GetExecuteReaderCommand(strsql);
                if (Database.myreader.Read())
                    Return = Convert.ToInt16(Database.myreader[0]);

                Database.myreader.Close();
            }
            if (Return == 0)
            {
                using (frmLedgerNameSearch f = new frmLedgerNameSearch(CompanyName, customerName, true))
                {
                    string strsql = "";

                    if (f.ShowDialog() == System.Windows.Forms.DialogResult.None)
                    {

                    }

                    string customercode = "";
                    string gst = "";
                    strsql = "select CompanyCode,NewGSTNo from Despatch.dbo.CompanyMaster where CompanyName='" + customerName + "'";
                    Database.myreader = Database.GetExecuteReaderCommand(strsql);
                    if (Database.myreader.Read())
                    {
                        customercode = Convert.ToString(Database.myreader[0]);
                        gst = Convert.ToString(Database.myreader["NewGSTNo"]);
                    }
                    Database.myreader.Close();

                    strsql = " select count(*) cnt from LedgerMaster where isnull(LCompanyCode,'') = '" + customercode
                        + "' and companyname='" + CompanyName + "' and NewGSTNo='" + gst + "'";
                    Database.myreader = Database.GetExecuteReaderCommand(strsql);
                    if (Database.myreader.Read())
                        Return = Convert.ToInt16(Database.myreader[0]);
                    Database.myreader.Close();
                }
            }

            return Return;
        }

        public static string GenerateInvno(string companyname, string yr, ref  int InvoiceSeries)
        {
            //int srno = 0;
            string Invno = "";
            //string temp = "";
            if (Database.OpenConnection(Utility.DespacthConnectionString))
            {
                //dispatchcode 15/09/2020 change for export series as per GST record
                Database.myreader = Database.GetExecuteReaderCommand("Select ExportCode from factoryinfo where name = '"
                  + companyname + "'");
                if (Database.myreader.Read())
                    Invno = Database.myreader[0].ToString();
                Database.myreader.Close();

                //Database.myreader = Database.GetExecuteReaderCommand("Select  ISNULL(MAX(invno),0) from packinglist where companyname = '"
                //  + companyname + "' and yr = '" + yr + "' and isthirdparty='no'  ");//order by invno desc

                string strsql = "select max(isnull(invno,0)) InvNo from ( Select   max(cast(invno as int)) invno from packinglist where Invoice_no like '%E/%' and companyname = '" + companyname + "' and yr = '" + yr + "'   and isthirdparty='no' "; //
                strsql += " union all Select  max(cast(DespatchNo as int)) from ExportInvoice where InvNo like '%E/%' and companyname = '" + companyname + "' and  ARESuffix    = '" + yr + "' ";
                // added by manish 27th Jan 2025
                strsql += "union all Select  max(cast(InvNo as int)) from JobWorkInvoice where job_invno like '%E/%' and companyname = '" + companyname + "' and  Suffix    = '" + yr + "'";
                // end added for jobworkinvoice
                strsql += " ) A";
                Database.myreader = Database.GetExecuteReaderCommand(strsql);
                if (Database.myreader.Read())
                    InvoiceSeries = Convert.ToInt32(Database.myreader[0].ToString());
                Database.myreader.Close();
                InvoiceSeries = InvoiceSeries + 1;
                if (yr == "17&18")
                    Invno = Invno + "/E/" + InvoiceSeries.ToString();
                else
                    Invno = Invno + "/E/" + InvoiceSeries.ToString() + "/" + yr;
                Database.Closeconnection();
            }
            return Invno;
        }


        public static string GeneratePackingListInvno(string companyname, string yr, ref  int InvoiceSeries)
        {
            //int srno = 0;
            string Invno = "";
            //string temp = "";
            if (Database.OpenConnection(Utility.DespacthConnectionString))
            {
                //dispatchcode 15/09/2020 change for export series as per GST record
                Database.myreader = Database.GetExecuteReaderCommand("Select ExportCode from factoryinfo where name = '"
                  + companyname + "'");
                if (Database.myreader.Read())
                    Invno = Database.myreader[0].ToString();
                Database.myreader.Close();

                //Database.myreader = Database.GetExecuteReaderCommand("Select  ISNULL(MAX(invno),0) from packinglist where companyname = '"
                //  + companyname + "' and yr = '" + yr + "' and isthirdparty='no'  ");//order by invno desc

                string strsql = "select max(isnull(invno,0)) InvNo from ( Select   max(cast(invno as int)) invno from packinglist where Invoice_no like '%E%' and companyname = '" + companyname + "' and yr = '" + yr + "'   and isthirdparty='no' "; //
                // strsql += " union all Select  max(cast(DespatchNo as int)) from ExportInvoice where InvNo like '%E%' and companyname = '" + companyname + "' and  ARESuffix    = '" + yr + "' ";
                strsql += " ) A";
                Database.myreader = Database.GetExecuteReaderCommand(strsql);
                if (Database.myreader.Read())
                    InvoiceSeries = Convert.ToInt32(Database.myreader[0].ToString());
                Database.myreader.Close();
                InvoiceSeries = InvoiceSeries + 1;
                if (yr == "17&18")
                    Invno = Invno + "/E/" + InvoiceSeries.ToString();
                else
                    Invno = Invno + "/E/" + InvoiceSeries.ToString() + "/" + yr;
                Database.Closeconnection();
            }
            return Invno;
        }
        
        public static string GenerateDomesticInvno(string companyname, string yr, ref  int InvoiceSeries, bool IsPurReturn = false)
        {
            string Invno = "";
            if (Database.OpenConnection(Utility.DespacthConnectionString))
            {

                Database.myreader = Database.GetExecuteReaderCommand("Select dispatchcode from factoryinfo where name = '"
                  + companyname + "'");
                if (Database.myreader.Read())
                    Invno = Database.myreader[0].ToString();
                Database.myreader.Close();

                if (IsPurReturn)
                {
                    string strsql = "select max(isnull(invno,0)) InvNo from ( Select   max(cast(invno as int)) invno from PurReturnInvoice where companyname = '" + companyname + "' and Suffix = '" + yr + "'   ";
                    strsql += " ) A";
                    Database.myreader = Database.GetExecuteReaderCommand(strsql);
                    if (Database.myreader.Read())
                        InvoiceSeries = Convert.ToInt32(Database.myreader[0].ToString());
                    Database.myreader.Close();
                    InvoiceSeries = InvoiceSeries + 1;
                    Invno = Invno + "/PR/" + InvoiceSeries.ToString();
                }
                else
                {
                    string strsql = "select max(isnull(invno,0)) InvNo from ( Select   max(cast(invno as int)) invno from DomesticInvoice where companyname = '" + companyname + "' and Suffix = '" + yr + "'   ";
                    strsql += " union all Select  max(cast(invno as int)) from JobWorkInvoice where companyname = '" + companyname + "' and  Suffix    = '" + yr + "' ";
                    strsql += " ) A";
                    Database.myreader = Database.GetExecuteReaderCommand(strsql);
                    if (Database.myreader.Read())
                        InvoiceSeries = Convert.ToInt32(Database.myreader[0].ToString());
                    Database.myreader.Close();
                    InvoiceSeries = InvoiceSeries + 1;
                    Invno = Invno + "/D/" + InvoiceSeries.ToString();
                }
                Database.Closeconnection();
            }
            return Invno;
        }


        public static string GenerateMechantExportInvno(string companyname, string yr, ref  int InvoiceSeries)
        {
            string Invno = "";
            if (Database.OpenConnection(Utility.DespacthConnectionString))
            {

                Database.myreader = Database.GetExecuteReaderCommand("Select dispatchcode from factoryinfo where name = '"
                  + companyname + "'");
                if (Database.myreader.Read())
                    Invno = Database.myreader[0].ToString();
                Database.myreader.Close();


                string strsql = " Select   isnull(max(cast(invno as int)),0) invno from DomesticInvoice where companyname = '" + companyname + "' and Suffix = '" + yr + "'  and MerchantExport ='yes' ";
                Database.myreader = Database.GetExecuteReaderCommand(strsql);
                if (Database.myreader.Read())
                    InvoiceSeries = Convert.ToInt32(Database.myreader[0].ToString());
                Database.myreader.Close();
                InvoiceSeries = InvoiceSeries + 1;
                Invno = Invno + "/ME/" + InvoiceSeries.ToString();

                Database.Closeconnection();
            }
            return Invno;
        }

        public static Boolean IsEwayGenerated(string companyname, string yr, string invno)
        {
            int Rec = 0;
            if (Database.OpenConnection(Utility.DespacthConnectionString))
            {

                string strsql = "Select count(*) from EWayBillMaster where companyname='" + companyname + "' and yr='" + yr + "' and InvNo='" + invno + "'";
                Database.myreader = Database.GetExecuteReaderCommand(strsql);
                if (Database.myreader.Read())
                {
                    Rec = Convert.ToInt32(Database.myreader[0].ToString());
                }
                Database.myreader.Close();
            }
            if (Rec > 0)
                return true;
            else
                return false;
        }
        public static int AllowMappingPackinglist(string companyname)
        {
            int packing = 0;
            if (Database.OpenConnection(Utility.DespacthConnectionString))
            {
                Database.myreader = Database.GetExecuteReaderCommand("Select isnull(IsMappPackingList,0) from FactoryInfo where NAME = '"
                     + companyname + "'");
                if (Database.myreader.Read())
                    packing = Convert.ToInt32(Database.myreader[0]);
                Database.myreader.Close();
            }
            return packing;
        }
        public static Boolean GetPackingListInvoiceNo(string companyname, string PartyName, double InvoiceNetWt, ref string Packinglist)
        {
            //DataTable dt = new DataTable();
            try
            {
                int pACKINGaLLO = AllowMappingPackinglist(companyname);

                if (pACKINGaLLO > 0)
                {
                    using (FrmPackingListSelection f = new FrmPackingListSelection(companyname, PartyName, InvoiceNetWt))
                    {
                        if (f.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                        {
                            Packinglist = f.packinglist;
                            Database.OpenConnection(Utility.DespacthConnectionString);
                            return true;
                        }
                        else
                        {
                            Packinglist = "";
                            MessageBox.Show("Packing List Is not selected");
                            Database.OpenConnection(Utility.DespacthConnectionString);
                            return false;
                        }
                    }
                }
                else
                {
                    Database.OpenConnection(Utility.DespacthConnectionString);
                    return true;
                }

                //using (System.Data.SqlClient.SqlConnection conn = new System.Data.SqlClient.SqlConnection(Utility.MaterialConnectionString))
                //{
                //    conn.Open();



                //string strsql = "select * from (select distinct PACKINGLISTNO,Invno ,PartyName,cast(Sysdate as date) despatchdate,sum(MISRollforDespatch.NetWt) NetWt from MISRollforDespatch  where Companyname='" + companyname + "' and"
                //    + "  sysdate >= (select fromdate from yearMaster where Yr='" + frmDefaultVale.Year
                //    + "') and  Sysdate <= (select todate from yearMaster where Yr='" + frmDefaultVale.Year + "') group by PACKINGLISTNO,Invno ,PartyName , cast(sysdate as date)"
                //    + " union all"
                //    + " select distinct PACKINGLISTNO,Invno ,PartyName,cast(Sysdate as date) despatchdate,sum(Weight) Netwt from vw_SmallBagPackingList  where Companyname='" + companyname + "' and"
                //    + "  sysdate >= (select fromdate from yearMaster where Yr='" + frmDefaultVale.Year + "') and  "
                //    + " Sysdate <= (select todate from yearMaster where Yr='" + frmDefaultVale.Year + "') and Deppt= 'Small Bag' group by PACKINGLISTNO,Invno ,PartyName , cast(sysdate as date)"
                //    + " union all"
                //    + " select distinct PACKINGLISTNO,Invno ,PartyName,cast(Sysdate as date) despatchdate,sum(Vw_PackingListReport.NetWt) from Vw_PackingListReport  where Companyname='" + companyname + "' and"
                //    + "  sysdate >= (select fromdate from yearMaster where Yr='" + frmDefaultVale.Year + "') and  "
                //    + " Sysdate <= (select todate from yearMaster where Yr='" + frmDefaultVale.Year + "')  group by PACKINGLISTNO,Invno ,PartyName , cast(sysdate as date) "
                //    + " union all"
                //    + " select distinct PACKINGLISTNO,InvoiceNo invno ,PartyName,cast(Sysdate as date) despatchdate ,sum(Weight) Netwt  from vw_TapePackingList  where Companyname='" + companyname + "' and"
                //    + "  sysdate >= (select fromdate from yearMaster where Yr='" + frmDefaultVale.Year + "') and  "
                //    + " Sysdate <= (select todate from yearMaster where Yr='" + frmDefaultVale.Year + "') group by PACKINGLISTNO,InvoiceNo ,PartyName , cast(sysdate as date)) as temp where temp.PartyName ='"
                //    + PartyName.Replace("'", "''") + "'";
                //System.Data.SqlClient.SqlCommand command = new System.Data.SqlClient.SqlCommand(strsql, conn);
                //System.Data.SqlClient.SqlDataAdapter adapter = new System.Data.SqlClient.SqlDataAdapter();
                //adapter.SelectCommand = command;
                //adapter.Fill(dt);
                //}

                //return dt;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public static Boolean IsAccountingDone(string companyname, string InvNo, string Yr)
        {
            bool blnAccDone = false;
            try
            {
                string CommandString = "select * from vw_SalesRegister where companyname = '"
                 + companyname + "' and  vw_SalesRegister.Invno in(select Invno from MaterialProcessing..salesvoucher where vw_SalesRegister.aresuffix = InvYear " +
                 @" and vw_SalesRegister.companyname = MaterialProcessing..salesvoucher.companyname and InvNo is not null
                union select InwardNo
                from Journal
                where vw_SalesRegister.aresuffix=Journal.yr and vw_SalesRegister.companyname=Journal.CompanyName and InwardNo is not null
                ) AND vw_SalesRegister.Invno='" + InvNo + "' AND vw_SalesRegister.aresuffix='" + Yr + "'";
                if (Database.OpenConnection(Utility.MaterialConnectionString))
                {
                    Database.myadapter = Database.GetAdapterCommand(CommandString);
                    DataSet dataset1 = new DataSet();
                    Database.myadapter.Fill(dataset1);
                    if (dataset1.Tables[0].Rows.Count > 0)
                        blnAccDone = true;
                }
                return blnAccDone;
            }
            catch (Exception ex)
            {
                return blnAccDone;
            }
        }

        public static AutoCompleteStringCollection GetItemList(string companyname, string commodity)
        {
            AutoCompleteStringCollection returnColl = new AutoCompleteStringCollection();
            try
            {
                string strsql = "select distinct Itemname from ITEM where CompanyName='" + companyname + "' and Commodity='" + commodity + "'";

                using (System.Data.SqlClient.SqlConnection conn = new System.Data.SqlClient.SqlConnection(Utility.MaterialConnectionString))
                {
                    conn.Open();
                    System.Data.SqlClient.SqlCommand command = new System.Data.SqlClient.SqlCommand(strsql, conn);

                    System.Data.SqlClient.SqlDataReader reader = command.ExecuteReader();

                    while (reader.Read())
                    {
                        returnColl.Add(Convert.ToString(reader[0]));
                    }
                }

            }
            catch (Exception ex)
            {
                ex.Message.ToString();
            }
            return returnColl;

        }

        public static void UpdateAdvLic_PendingLimit(string companyname, string LicenseNo, double Limit)
        {
            using (System.Data.SqlClient.SqlConnection conn = new System.Data.SqlClient.SqlConnection(Utility.DespacthConnectionString))
            {
                conn.Open();
                double PendingLimit = 0;
                double TotalLimit = 0;
                string strsql = "select * from advLicense with(Nolock) where AdvLicenseNo = '"
                        + LicenseNo + "' and companyname = '" + companyname + "'";
                System.Data.SqlClient.SqlCommand command = new System.Data.SqlClient.SqlCommand(strsql, conn);
                System.Data.SqlClient.SqlDataReader reader = command.ExecuteReader();
                if (reader.Read())
                {
                    PendingLimit = Convert.ToDouble(reader["PendingLimit"].ToString());
                    TotalLimit = Convert.ToDouble(reader["Limit"].ToString());
                }
                reader.Close();
                Limit = Limit / 1000;
                if ((PendingLimit - Limit) >= 0)
                {
                    strsql = "update advLicense set PendingLimit = " + (PendingLimit - Limit)
                    + " where AdvLicenseNo = '" + LicenseNo + "' and companyname = '" + companyname + "'";
                    if (Database.OpenConnection(Utility.DespacthConnectionString))
                    {
                        Database.GetExecuteNonQueryCommand(strsql);
                    }
                }
            }
        }

        public static double PendingAdvLic(string companyname, string LicenseNo, double ExpNetWt)
        {
            try
            {
                double dblPending = 0;
                if (LicenseNo.Length == 0) return 0;
                using (System.Data.SqlClient.SqlConnection conn = new System.Data.SqlClient.SqlConnection(Utility.DespacthConnectionString))
                {
                    conn.Open();
                    ExpNetWt = ExpNetWt / 1000; //Convert into MT. for checking
                    double AdvLimit = 0;
                    double Netwt = 0;
                    string strsql = "Select distinct isnull(A.Limit,0) Limit from AdvLicense A  " +
                        " where   A.AdvLicenseNo='" + LicenseNo + "' ";
                    System.Data.SqlClient.SqlCommand command = new System.Data.SqlClient.SqlCommand(strsql, conn);
                    System.Data.SqlClient.SqlDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        AdvLimit += Convert.ToDouble(reader["Limit"].ToString());
                    }
                    reader.Close();

                    strsql = "Select round(isnull(sum(V.netwt),0)/1000,0) as netwt from   [vw_ExportAdvanceLicensesummary] V  " +
                        " where   v.advno='" + LicenseNo + "'  ";
                    System.Data.SqlClient.SqlCommand command1 = new System.Data.SqlClient.SqlCommand(strsql, conn);
                    System.Data.SqlClient.SqlDataReader reader1 = command1.ExecuteReader();
                    if (reader1.Read())
                    {
                        Netwt = Convert.ToDouble(reader1["netwt"].ToString());
                    }
                    reader.Close();
                    dblPending = Math.Round(AdvLimit - Netwt - ExpNetWt, FrmMain.ERPQtyDigit);
                }
                return dblPending;

            }
            catch
            {
                return 0;
            }
        }
        public static AutoCompleteStringCollection BondAutoComplete(string company)
        {
            AutoCompleteStringCollection returnColl = new AutoCompleteStringCollection();
            try
            {
                if (Database.OpenConnection(Utility.DespacthConnectionString))
                {
                    DataTable dt = new DataTable();
                    dt = Database.GetDataTable("Select adcode from ADCODEMASTER where companyname='" + company + "' and IsClose=1");
                    for (int i = 0; i <= dt.Rows.Count - 1; i++)
                    {
                        returnColl.Add(Convert.ToString(dt.Rows[i][0]));
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("BondAutoComplete :" + ex.Message.ToString());
            }
            return returnColl;
        }

        /// <summary>
        /// Get List of Group Companies
        /// </summary>
        /// <returns>DataTable</returns>
        public static DataTable GetGroupCompanyList()
        {
            // Get From cache
            object obj = Cache.GetCache("GroupCompanyList");
            if (obj != null)
                return ((DataTable)obj).Copy();

            // Get From Database
            string stsql = "SELECT DISTINCT GroupName AS CompanyID, GroupName AS CompanyName FROM FactoryInfo";
            if (FrmMain.UserName.ToLower() == "exauditor".ToLower() || FrmMain.UserName.ToLower() == "erp")
            {
                stsql = "SELECT DISTINCT GroupName AS CompanyID, GroupName AS CompanyName FROM FactoryInfo inner join [Loginentry].[dbo].[LoginRightAccount] L on L.CompanyName=FactoryInfo.name  where L.LoginID='" + FrmMain.UserName + "' order by OrderPosition asc";
            }
            else
            {
                stsql = "SELECT DISTINCT GroupName AS CompanyID, GroupName AS CompanyName FROM FactoryInfo WHERE  (name in ( select CompanyName from Loginentry..LoginRightAccount L where L.LoginID='" + FrmMain.UserName + "') OR NAME IN ( select CompName from Despatch..DefaultValue WHERE LOGINNAME='" + FrmMain.UserName + "'))";
            }
            //stsql = "SELECT DISTINCT GroupName AS CompanyID, GroupName AS CompanyName FROM FactoryInfo inner join [Loginentry].[dbo].[LoginRightAccount] L on L.CompanyName=FactoryInfo.name  where L.LoginID='" + FrmMain.UserName + "' order by OrderPosition asc";

            DataTable dataTable = new DataTable("GroupCompany");
            if (Database.OpenConnection(Utility.MaterialConnectionString))
            {
                Database.GetExecuteReaderCommand(string.Format(stsql)); //"SELECT DISTINCT GroupName AS CompanyID, GroupName AS CompanyName FROM FactoryInfo"
                dataTable.Load(Database.myreader);
            }

            if (dataTable.Rows.Count != 0) Cache.AddCache(dataTable, -1, "GroupCompanyList");
            return dataTable.Copy();
        }

        public static void BindComboCompanyWithGC(ComboBox combo)
        {
            object obj = Cache.GetCache("CompanyWithGC");
            DataTable dt = null;

            if (obj != null)
                dt = ((DataTable)obj).Copy();
            else
            {
                Database.OpenConnection(UtilityPayroll.ConnectionString);
                int IsHOEmp = Convert.ToInt32(Database.GetExecuteNonQueryCommand_retrun("select isnull(IsHOEmp,0) from empinfo where Empcode='" + FrmMain.Empcode + "'"));

                Database.OpenConnection(Utility.DespacthConnectionString);
                string strsql = "";

                if (FrmMain.UserName.ToLower() == "exauditor".ToLower())
                {
                    strsql = "select * from (select 'A-0' AS Value, 'All(*)' AS Name, 0 AS OridinalPosition union all select GroupNameValue, GroupName, "
                               + " ROW_NUMBER() OVER(ORDER BY GroupName asc) from ( "
                                + "  select distinct 'G-'+GroupCompany AS GroupNameValue, 'G-' + GroupCompany AS GroupName  "
                                + "  from FactoryInfo inner join [Loginentry].[dbo].[LoginRightAccount] L on L.CompanyName=FactoryInfo.name "
                                + "  where GroupCompany!='' and len(GroupCompany)>5 and L.LoginID='" + FrmMain.UserName + "' "
                            + " ) as t union all select 'C-' + convert(varchar, SrNo), 'C-' + Name, OrderPosition+1000 from factoryInfo "
                    + " inner join [Loginentry].[dbo].[LoginRightAccount] L on L.CompanyName=FactoryInfo.name where L.LoginID='" + FrmMain.UserName + "' ) a  order by OridinalPosition";


                }
                else if (FrmMain.isaccounthod || FrmMain.IsAdmin || IsHOEmp == 1)
                {
                    strsql = @"
                                select * from (select 'A-0' AS Value, 'All(*)' AS Name, 0 AS OridinalPosition union all select GroupNameValue, GroupName, 
                                ROW_NUMBER() OVER(ORDER BY GroupName asc) from (
                                select distinct 'G-'+GroupCompany AS GroupNameValue, 'G-' + GroupCompany AS GroupName from FactoryInfo where GroupCompany!='' and len(GroupCompany)>5
                            ) as t union all select 'C-' + convert(varchar, SrNo), 'C-' + Name, OrderPosition+1000 from factoryInfo ) A order by OridinalPosition";
                }
                else
                {
                    strsql = @" select * from (select 'C-' + convert(varchar, SrNo) AS Value, 'C-' + Name as   Name, OrderPosition+1000 as OridinalPosition 
                            from factoryInfo 
                            where (name in ( select CompanyName from Loginentry..LoginRightAccount L where L.LoginID='" + FrmMain.UserName
                            + "') OR NAME IN ( select CompName from Despatch..DefaultValue WHERE LOGINNAME='" + FrmMain.UserName + "')) ) a order by OridinalPosition";

                }

                DataTable dataTable = Database.GetDataTable(strsql);
                if (dataTable.Rows.Count != 0) Cache.AddCache(dataTable, -1, "CompanyWithGC");

                dt = dataTable.Copy();
            }

            combo.DataSource = dt;
            combo.DisplayMember = "Name";
            combo.ValueMember = "Value";

            combo.SelectedIndex = -1;
            if (dt.Rows.Count != 0)
                combo.SelectedIndex = 0;
        }
        /// <summary>
        /// Bind a company comobox BindComboCompanyMulti
        /// </summary>
        /// <param name="combo">CheckedComboBox</param>
        /// <param name="IncludeBlank">Include Blank</param>
        public static void BindComboCompanyMulti(CheckedComboBox combo, bool IncludeBlank)
        {
            if (Database.OpenConnection(Utility.DespacthConnectionString))
            {
                DataTable dt = Database.GetDataTable("Select name from factoryinfo   order by OrderPosition");
                for (int i = 0; i <= dt.Rows.Count - 1; i++)
                {
                    string companyname = dt.Rows[i][0].ToString().Trim();
                    combo.Items.Add(companyname);
                    if (companyname == frmDefaultVale.CompanyName)
                        combo.SetItemChecked(i, true);
                }
            }
        }
        public static void LoadCustomerUnlock(System.Windows.Forms.ComboBox CB, string companyname, DateTime date)
        {
            try
            {
                if (companyname.Length == 0) return;
                if (frmDefaultVale.CompanyName.Length > 0)
                    if (companyname != frmDefaultVale.CompanyName) return;
                using (System.Data.SqlClient.SqlConnection conn = new System.Data.SqlClient.SqlConnection(Utility.MaterialConnectionString))
                {
                    CB.Items.Clear();
                    //if (strsql.Length == 0)
                    string strsql = "select distinct ledgername as companyname from ledgermaster with(NoLock) where (Category ='Customer'  OR Category='Customer-Supplier'  or Category ='Service Provider'  ) and companyname = '" +
                    companyname + "' and Isnull(IsAccClose,'no')='no'  and (('" + date.ToString("yyyy-MM-dd") + "' between fromdate and todate) or (fromdate <= '"
                    + date.ToString("yyyy-MM-dd") + "' and todate is null )) "
                    + "  order by ledgername";
                    System.Data.SqlClient.SqlCommand command = new System.Data.SqlClient.SqlCommand(strsql, conn);
                    conn.Open();
                    System.Data.SqlClient.SqlDataReader reader = command.ExecuteReader();

                    while (reader.Read())
                    {
                        CB.Items.Add(Convert.ToString(reader["companyname"]));
                    }
                    reader.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("LoadCustomerUnlock : " + ex.Message);
            }
        }

        public static void LoadCustomerUnlockChallan(System.Windows.Forms.ComboBox CB, string companyname, DateTime date)
        {
            try
            {
                using (System.Data.SqlClient.SqlConnection conn = new System.Data.SqlClient.SqlConnection(Utility.MaterialConnectionString))
                {
                    CB.Items.Clear();
                    //if (strsql.Length == 0)
                    string strsql = "select distinct ledgername as companyname from ledgermaster with(NoLock) where (Category ='Customer'  OR Category='Customer-Supplier'  or Category ='Service Provider' or Category ='Supplier'  ) and companyname = '" +
                    companyname + "' and Isnull(IsAccClose,'no')='no'  and (('" + date.ToString("yyyy-MM-dd") + "' between fromdate and todate) or (fromdate <= '"
                    + date.ToString("yyyy-MM-dd") + "' and todate is null )) "
                    + "  order by ledgername";
                    System.Data.SqlClient.SqlCommand command = new System.Data.SqlClient.SqlCommand(strsql, conn);
                    conn.Open();
                    System.Data.SqlClient.SqlDataReader reader = command.ExecuteReader();

                    while (reader.Read())
                    {
                        CB.Items.Add(Convert.ToString(reader["companyname"]));
                    }
                    reader.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("LoadCustomerUnlock : " + ex.Message);
            }
        }
        /// <summary>
        /// Get List of Item Master 22.06.2020
        /// </summary>
        /// <returns>DataTable</returns>
        public static DataTable GetItemMaster(string dateTimePicker1 = "", string companyname = "")
        {

            //string strsql = "select distinct itemcode,Itemname,Deptt from MaterialProcessing.dbo.Item where companyname='" + companyname + "'"
            //        + " and (('" + dateTimePicker1 + "' BETWEEN FROMDATE AND TODATE) OR (FROMDATE <='"
            //        + dateTimePicker1 + "' AND TODATE IS NULL))   order by ItemName ";

            string strsql = "select distinct itemcode,Itemname,Deptt,GroupName from MaterialProcessing.dbo.ItemCodes where todate is null order by ItemName ";

            DataTable dataTable = new DataTable("GetItemMaster");
            if (Database.OpenConnection(Utility.DespacthConnectionString))
            {
                Database.GetExecuteReaderCommand(string.Format(strsql));
                dataTable.Load(Database.myreader);
            }
            return dataTable.Copy();
        }

        /// <summary>
        /// Get List of Main group from ItemDeptt 13.07.2020
        /// </summary>
        /// <returns>DataTable</returns>
        public static DataTable GetItemMainGrp()
        {
            string strsql = "select distinct DepttName from MaterialProcessing.dbo.ItemDeptt ";
            DataTable dataTable = new DataTable("GetItemMainGroup");
            if (Database.OpenConnection(Utility.DespacthConnectionString))
            {
                Database.GetExecuteReaderCommand(string.Format(strsql));
                dataTable.Load(Database.myreader);
            }
            return dataTable.Copy();
        }

        /// <summary>
        /// Get List of GetItemGroupName from Item 15.09.2020
        /// </summary>
        /// <returns>DataTable</returns>
        public static DataTable GetItemGroupName()
        {
            string strsql = "select distinct Depttname,GroupName from MaterialProcessing.dbo.ItemGroup ";
            DataTable dataTable = new DataTable("GetItemGroupName");
            if (Database.OpenConnection(Utility.DespacthConnectionString))
            {
                Database.GetExecuteReaderCommand(string.Format(strsql));
                dataTable.Load(Database.myreader);
            }
            return dataTable.Copy();
        }

        /// <summary>
        /// Get List of GetItemSubGroupName from Item 15.09.2020
        /// </summary>
        /// <returns>DataTable</returns>
        public static DataTable GetItemSubGroupName()
        {
            string strsql = "select distinct DepartmentName,GroupName,SubGroupName  from MaterialProcessing.dbo.SubGroupMaster ";
            DataTable dataTable = new DataTable("GetItemSubGroupName");
            if (Database.OpenConnection(Utility.DespacthConnectionString))
            {
                Database.GetExecuteReaderCommand(string.Format(strsql));
                dataTable.Load(Database.myreader);
            }
            return dataTable.Copy();
        }
        public static string GetDeptt(string itemcode)
        {
            string Deptt = "";
            try
            {
                string strsql = "select distinct isnull(deptt,'') from Itemcodes where Itemcode='" + itemcode + "'";
                using (System.Data.SqlClient.SqlConnection conn = new System.Data.SqlClient.SqlConnection(Utility.MaterialConnectionString))
                {
                    conn.Open();
                    System.Data.SqlClient.SqlCommand command = new System.Data.SqlClient.SqlCommand(strsql, conn);
                    System.Data.SqlClient.SqlDataReader reader = command.ExecuteReader();
                    if (reader.Read())
                    {
                        Deptt = reader[0].ToString();
                    }
                }
                return Deptt;
            }
            catch (Exception ex)
            {
                return "";
            }
        }


        public static string GetItemGropuName(string itemcode, string companyname, ref string groupname, ref string subgroup)
        {
            string Deptt = "";
            try
            {
                string strsql = "select distinct isnull(deptt,'') deptt,isnull(GroupName,'') as GroupName,isnull(SubGroupName,'') as SubGroupName from Item where Itemcode='" + itemcode + "' and companyname='" + companyname + "'";
                using (System.Data.SqlClient.SqlConnection conn = new System.Data.SqlClient.SqlConnection(Utility.MaterialConnectionString))
                {
                    conn.Open();
                    System.Data.SqlClient.SqlCommand command = new System.Data.SqlClient.SqlCommand(strsql, conn);
                    System.Data.SqlClient.SqlDataReader reader = command.ExecuteReader();
                    if (reader.Read())
                    {
                        Deptt = reader[0].ToString();
                        groupname = reader[1].ToString();
                        subgroup = reader[2].ToString();
                    }
                }
                return Deptt;
            }
            catch (Exception ex)
            {
                return "";
            }
        }

        /// <summary>
        /// Get List of MIs item Name from ItemDeptt 07.08.2020
        /// </summary>
        /// <returns>DataTable</returns>
        public static DataTable GetItemMISItem()
        {
            string strsql = "select distinct ItemName,category from MaterialProcessing.dbo.ac_mis_stock_item_master_temp ";
            DataTable dataTable = new DataTable("GetItemMISItem");
            if (Database.OpenConnection(Utility.DespacthConnectionString))
            {
                Database.GetExecuteReaderCommand(string.Format(strsql));
                dataTable.Load(Database.myreader);
            }
            return dataTable.Copy();
        }

        /// <summary>
        /// Get List of MIs item Name from ItemDeptt 07.08.2020
        /// </summary>
        /// <returns>DataTable</returns>
        public static DataTable GetItemMISCategory()
        {
            string strsql = "select distinct Category from MaterialProcessing.dbo.ac_mis_stock_item_master_temp ";
            DataTable dataTable = new DataTable("GetItemMISCategory");
            if (Database.OpenConnection(Utility.DespacthConnectionString))
            {
                Database.GetExecuteReaderCommand(string.Format(strsql));
                dataTable.Load(Database.myreader);
            }
            return dataTable.Copy();
        }

        public static string GetMISCategory(string itemcode)
        {
            string Deptt = "";
            try
            {
                string strsql = "select distinct category from ac_mis_stock_item_master_temp where ItemName='" + itemcode + "'";
                using (System.Data.SqlClient.SqlConnection conn = new System.Data.SqlClient.SqlConnection(Utility.MaterialConnectionString))
                {
                    conn.Open();
                    System.Data.SqlClient.SqlCommand command = new System.Data.SqlClient.SqlCommand(strsql, conn);
                    System.Data.SqlClient.SqlDataReader reader = command.ExecuteReader();
                    if (reader.Read())
                    {
                        Deptt = reader[0].ToString();
                    }
                }
                return Deptt;
            }
            catch (Exception ex)
            {
                return "";
            }
        }


        public static double LoadTCSLedger(string companyname, string ledgername, string commodity, DateTime dt)
        {

            double TCSRATE = 0;
            int intercompany = 0;
            int intunit = 0;
            int IsTCSApp = 0;
            try
            {
                #region check for InterCompany & TCS Applicable or not
                if (Database.OpenConnection(Utility.MaterialConnectionString))
                {
                    Database.myreader = Database.GetExecuteReaderCommand(string.Format(
                        "Select dbo.[fn_Get_Ledger_InterCompany]('{0}','{1}','{2}')", companyname, ledgername, dt.ToString("yyyy-MM-dd")));
                    if (Database.myreader.Read())
                    {
                        intercompany = Convert.ToInt32(Database.myreader[0]);
                    }
                    Database.myreader.Close();

                    Database.myreader = Database.GetExecuteReaderCommand(string.Format(
                        "Select dbo.[fn_Get_Ledger_InterUnit]('{0}','{1}','{2}')", companyname, ledgername, dt.ToString("yyyy-MM-dd")));
                    if (Database.myreader.Read())
                    {
                        intunit = Convert.ToInt32(Database.myreader[0]);
                    }
                    Database.myreader.Close();

                    Database.myreader = Database.GetExecuteReaderCommand(string.Format(
                        "Select dbo.[fn_Get_IsTCSApp]('{0}','{1}','{2}')", companyname, ledgername, dt.ToString("yyyy-MM-dd")));
                    if (Database.myreader.Read())
                    {
                        IsTCSApp = Convert.ToInt32(Database.myreader[0]);
                    }
                    Database.myreader.Close();
                }
                #endregion
                if (Database.OpenConnection(Utility.MaterialConnectionString))
                {
                    string strsql = "select TotalTCS  from Despatch.dbo.tcsfill where Commodity ='"
                                  + commodity + "' and companyname = '" + companyname + "' and (('"
                                  + dt.ToString("yyyy-MM-dd") + "' between fromdate and todate) or ('"
                                  + dt.ToString("yyyy-MM-dd") + "' >= fromdate and todate is null))";
                    Database.myreader = Database.GetExecuteReaderCommand(strsql);
                    if (Database.myreader.Read())
                    {
                        TCSRATE = Utility.SafeConvertToDouble(Database.myreader[0]);
                        Database.myreader.Close();
                    }
                    else
                    {
                        if (Convert.ToDateTime(dt.ToString("yyyy-MM-dd")) < Convert.ToDateTime("2020-10-01"))
                            return TCSRATE;

                        //string groupname = Convert.ToString(Database.GetExecuteScalarCommand_return("select GroupName from FactoryInfo where Name='" + companyname + "'"));
                        //double SalesAmt = 0;
                        string PANNo = "";
                        Database.myreader = Database.GetExecuteReaderCommand("SELECT  isnull(PANNo,'') as PANNo FROM [MaterialProcessing].[dbo].[vw_TCS_Sales_Appliacble] " +
                            " where companyname='" + companyname + "' and LedgerName='" + ledgername + "'");
                        if (Database.myreader.Read())
                        {
                            //SalesAmt = Utility.SafeConvertToDouble(Database.myreader["BillAmtInLakh"]);
                            PANNo = Convert.ToString(Database.myreader["PANNo"]);
                        }
                        Database.myreader.Close();
                        if (PANNo.Length < 10)
                        {
                            if (IsTCSApp == 0)
                                TCSRATE = 0;
                            else
                                TCSRATE = 1;
                        }
                        else
                        {
                            Database.myreader = Database.GetExecuteReaderCommand("select TOP 1 TotalTCS  from Despatch.dbo.tcsfill where companyname = '"
                                          + companyname + "' and isnull(Commodity,'')='' and (('"
                                          + dt.ToString("yyyy-MM-dd") + "' between fromdate and todate) or ('"
                                          + dt.ToString("yyyy-MM-dd") + "' >= fromdate and todate is null)) order by FromDate desc");
                            if (Database.myreader.Read())
                                TCSRATE = Utility.SafeConvertToDouble(Database.myreader[0]);
                            Database.myreader.Close();
                            if (intercompany == 1)
                                TCSRATE = 0;
                            if (intunit == 1)
                                TCSRATE = 0;
                            if (IsTCSApp == 0)
                                TCSRATE = 0;
                        }
                    }
                }
                return TCSRATE;
            }
            catch (Exception ex)
            {
                return TCSRATE;
            }
        }

        public static Boolean GetPackingListInvoiceNo2(string companyname, string PartyName, double InvoiceNetWt, DataGridView dg, string ColItemCode, string Colnetwt, ref string Packinglist)
        {
            //DataTable dt = new DataTable();
            try
            {
                int pACKINGaLLO = AllowMappingPackinglist(companyname);

                if (pACKINGaLLO > 0)
                {
                    using (FrmPackingListSelection f = new FrmPackingListSelection(companyname, PartyName, InvoiceNetWt, dg, ColItemCode, Colnetwt))
                    {
                        if (f.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                        {
                            Packinglist = f.packinglist;
                            if (Packinglist.Length == 0)
                            {
                                Database.OpenConnection(Utility.DespacthConnectionString);
                                return false;
                            }
                            Database.OpenConnection(Utility.DespacthConnectionString);
                            return true;
                        }
                        else
                        {
                            Packinglist = "";
                            MessageBox.Show("Packing List Is not selected");
                            Database.OpenConnection(Utility.DespacthConnectionString);
                            return false;
                        }
                    }
                }
                else
                {
                    Database.OpenConnection(Utility.DespacthConnectionString);
                    return true;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }


        //Change by Rishabh on 03-11-2025 for new way of getting ADcodes in dropdown from the master copied from Anil sir's master excel.

        public static void GetADCODE(ComboBox objcom)
        {
            Loadcombo(objcom, "select ADCode from ExportADCodesmaster", "ADCode", "ADCode", Utility.DespacthConnectionString);
        }
        //Change End

        //Change by Rishabh on 03-11-2025 for new way of getting Bank details in dropdown from the master copied from Anil sir's master excel.

        public static void GetExportBank(ComboBox objcom)
        {
            Loadcombo(objcom, "select BankDetails from ExportBankMaster", "BankDetails", "BankDetails", Utility.DespacthConnectionString);
        }
        //Change End

    }
}
