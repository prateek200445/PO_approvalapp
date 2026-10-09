using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using ERP.Classes;
using System.Data.OleDb;
using System.IO;
using System.Globalization;
using System.Collections;
using System.Reflection;
namespace ERP
{
    /// <summary>
    /// This Function is used to get list of company list for the production module and it will also store in cache.
    /// Indent code is also added in this common function
    /// </summary>
    public class ProductionCommonFunction : Tools.ToolsCommonFunction
    {
        public static DataTable GetCompanyList()
        {
            // Get From cache
            object obj = Cache.GetCache("CompanyList");
            if (obj != null)
                return ((DataTable)obj).Copy();

            // Get From Database
            DataTable dataTable = new DataTable("Company");
            if (Database.OpenConnection(Utility.MaterialConnectionString))
            {
                Database.GetExecuteReaderCommand(string.Format("SELECT SrNo AS CompanyID, Name AS CompanyName,IndentCode FROM FactoryInfo order by OrderPosition"));
                dataTable.Load(Database.myreader);
            }

            if (dataTable.Rows.Count != 0) Cache.AddCache(dataTable, -1, "CompanyList");
            return dataTable.Copy();
        }

        public static void InsertStockJournal(double stock_journal_no, DateTime dt, string companyname, string type, string itemname, double qty, double rate, double amount, string unit,
             string yr, double recordlogid, double srno, string itemcode, string rollno, string plantname)
        {

            //            @stock_journal_no int,
            //@sysdate datetime,
            //@companyname varchar(100),
            //@type varchar(100),
            //@itemname varchar(100),
            //@qty float,
            //--@rate float,
            //--@amount float,
            //--@unit varchar(50),
            //@yr varchar(50),
            //@recordlogid int,
            //@srno int,
            //@itemcode varchar(100),
            //--@rollno varchar(100),
            //@plantname varchar(200)

            Database.mycommand = new System.Data.SqlClient.SqlCommand();
            Database.mycommand.Parameters.Clear();
            Database.mycommand.CommandType = CommandType.StoredProcedure;
            Database.mycommand.Connection = Database.myconn;
            Database.mycommand.Transaction = Database.mytransaction;
            Database.mycommand.CommandText = "Insert_StockJournal";
            Database.mycommand.Parameters.AddWithValue("@stock_journal_no", stock_journal_no);
            Database.mycommand.Parameters.AddWithValue("@sysdate", dt.ToString("yyyy-MM-dd"));
            Database.mycommand.Parameters.AddWithValue("@companyname", companyname);
            Database.mycommand.Parameters.AddWithValue("@type", type);
            Database.mycommand.Parameters.AddWithValue("@itemname", itemname);
            Database.mycommand.Parameters.AddWithValue("@qty", qty);
            Database.mycommand.Parameters.AddWithValue("@rate", rate);
            Database.mycommand.Parameters.AddWithValue("@amount", amount);
            Database.mycommand.Parameters.AddWithValue("@unit", unit);
            Database.mycommand.Parameters.AddWithValue("@yr", yr);
            Database.mycommand.Parameters.AddWithValue("@recordlogid", recordlogid);
            Database.mycommand.Parameters.AddWithValue("@srno", srno);
            Database.mycommand.Parameters.AddWithValue("@itemcode", itemcode);
            Database.mycommand.Parameters.AddWithValue("@rollno", rollno);
            Database.mycommand.Parameters.AddWithValue("@plantname", plantname);
            Database.mycommand.ExecuteNonQuery();

        }


        public static void WareHouseStoreinwards(string itemcode, string itemname, double qty, DateTime sysdate, string CompanyName, string fromwarehouse,
            string towarehouse)
        {
            try
            {
                itemcode = itemcode.Replace("'", "");
                string s = " dbo.GET_PROD_RATE_EBIDTA('" + CompanyName + "','" + sysdate.ToString("yyyy-MM-dd") + "','" + itemcode + "','" + fromwarehouse + "')";
                Database.GetExecuteNonQueryCommand("insert into warehousestoreinwards values('" + itemcode + "','" + itemname
                     + "'," + qty + "," + s + ",'" + sysdate.ToString("yyyy-MM-dd") + "','" + CompanyName + "','" + fromwarehouse + "','" + towarehouse + "')");
            }
            catch (Exception ex)
            {
                ex.Message.ToString();
            }
        }

        public static void WareHouseStoreoutwards(string itemcode, string itemname, double qty, DateTime sysdate, string CompanyName, string fromwarehouse,
            string towarehouse)
        {
            try
            {
                itemcode = itemcode.Replace("'", "");
                string s = " dbo.GET_PROD_RATE_EBIDTA('" + CompanyName + "','" + sysdate.ToString("yyyy-MM-dd") + "','" + itemcode + "','" + fromwarehouse + "')";
                Database.GetExecuteNonQueryCommand("insert into warehousestoreoutwards values('" + itemcode + "','" + itemname
                     + "'," + qty + "," + s + ",'" + sysdate.ToString("yyyy-MM-dd") + "','" + CompanyName + "','" + fromwarehouse + "','" + towarehouse + "')");
            }
            catch (Exception ex)
            {
                ex.Message.ToString();
            }
        }

        public static string GetItemName(string ItemCode)
        {
            string itemname = "";
            Database.myreader = Database.GetExecuteReaderCommand("select itemname from itemcodes where itemcode = '" + ItemCode + "'");
            if (Database.myreader.Read())
                itemname = Database.myreader[0].ToString();
            Database.myreader.Close();
            return itemname;
        }

        public static string GetItemCode(string ItemName)
        {
            string itemcode = "";
            Database.myreader = Database.GetExecuteReaderCommand("select itemcode from itemcodes where itemname = '" + ItemName + "'");
            if (Database.myreader.Read())
                itemcode = Database.myreader[0].ToString();
            Database.myreader.Close();
            return itemcode;
        }

        ///<summaary>
        ///Get User wise list of WareHouse
        /// </summaary>
        /// <returns>Datatable
        /// </returns>
        public static DataTable GetWareHouse_userWise()
        {
            DataTable dataTable = new DataTable("WarehouseUserWise");
            if (Database.OpenConnection(Utility.MaterialConnectionString))
            {
                Database.GetExecuteReaderCommand(string.Format("Select warehousename,srno from warehousemaster  " +
                        " where companyname='" + frmDefaultVale.CompanyName + "' and (entryuser1 = '" + FrmMain.UserName + "' or entryuser2 = '"
                        + FrmMain.UserName + "' or entryuser3 = '" + FrmMain.UserName + "' or entryuser4 = '" + FrmMain.UserName + "' or entryuser5 = '"
                        + FrmMain.UserName + "' or approvalauth1 = '" + FrmMain.UserName + "' or approvalauth2 = '" + FrmMain.UserName + "' or approvalauth3 = '"
                        + FrmMain.UserName + "' or approvalauth4 = '" + FrmMain.UserName + "' or approvalauth5 = '" + FrmMain.UserName + "' ) order by warehousename"));
                dataTable.Load(Database.myreader);
            }


            //if (dataTable.Rows.Count != 0)  
            return dataTable.Copy();
        }

        ///<summaary>
        ///Get User wise list of WareHouse with parameter
        /// </summaary>
        /// <param name="Warehouse">Pass valuse for like condition.  ex:loom</param>
        /// <returns>Datatable
        /// </returns>
        public static DataTable GetWareHouse_userWise(string Warehouse)
        {
            Warehouse = " And warehousename like '%" + Warehouse + "%'";
            DataTable dataTable = new DataTable("WarehouseUserWise");
            if (Database.OpenConnection(Utility.MaterialConnectionString))
            {
                Database.GetExecuteReaderCommand(string.Format("Select warehousename,srno from warehousemaster  " +
                        " where companyname='" + frmDefaultVale.CompanyName + "' " + Warehouse + " and (entryuser1 = '" + FrmMain.UserName + "' or entryuser2 = '"
                        + FrmMain.UserName + "' or entryuser3 = '" + FrmMain.UserName + "' or entryuser4 = '" + FrmMain.UserName + "' or entryuser5 = '"
                        + FrmMain.UserName + "' or approvalauth1 = '" + FrmMain.UserName + "' or approvalauth2 = '" + FrmMain.UserName + "' or approvalauth3 = '"
                        + FrmMain.UserName + "' or approvalauth4 = '" + FrmMain.UserName + "' or approvalauth5 = '" + FrmMain.UserName + "' ) order by warehousename"));
                dataTable.Load(Database.myreader);
            }

            //if (dataTable.Rows.Count != 0)  
            return dataTable.Copy();
        }

        ///<summaary>
        ///Get User wise list of WareHouse with parameter
        /// </summaary>
        /// <param name="Warehouse">Pass valuse for like condition.  ex:loom</param>
        /// <returns>Datatable
        /// </returns>
        public static DataTable GetInternalVendor()
        {
            DataTable dataTable = new DataTable("InternalVendor");
            if (Database.OpenConnection(Utility.MaterialConnectionString))
            {
                Database.GetExecuteReaderCommand("select Distinct Firmname from vw_InternalVendor order by Firmname");
                dataTable.Load(Database.myreader);
            }

            //if (dataTable.Rows.Count != 0)  
            return dataTable.Copy();
        }
        public static DataTable GetDespatchCompanyList()
        {
            DataTable dataTable = new DataTable("InternalVendor");
            if (Database.OpenConnection(Utility.DespacthConnectionString))
            {
                string strsql = "SELECT DISTINCT firmname FROM (select distinct firmname from MaterialProcessing.dbo.InternalVendor union all select distinct companyname as firmname from vw_CompanyMaster where IsFreeze='no' ) A order by 1 asc";
                Database.GetExecuteReaderCommand(strsql);
                dataTable.Load(Database.myreader);
            }

            //if (dataTable.Rows.Count != 0)  
            return dataTable.Copy();
        }
        /// <summary>
        /// Get List of All Warehouse
        /// </summary>
        /// <returns>DataTable</returns>
        public static DataTable GetWareHouseList_Company(string CompanyName)
        {
            // Get From cache
            object obj = Cache.GetCache("WarehouseList");
            if (obj != null)
                return ((DataTable)obj).Copy();

            // Get From Database
            DataTable dataTable = new DataTable("Warehouse");
            if (Database.OpenConnection(Utility.MaterialConnectionString))
            {
                Database.GetExecuteReaderCommand(string.Format("SELECT warehousename,srno FROM warehousemaster  WHERE companyname='"
                    + CompanyName + "'  ORDER BY warehousename"));
                dataTable.Load(Database.myreader);
            }

            if (dataTable.Rows.Count != 0) Cache.AddCache(dataTable, -1, "WarehouseList");
            return dataTable.Copy();
        }

        /// <summary>
        /// Get List of Warehouse with parameter
        /// <param name="Warehouse">Pass valuse for like condition.  ex:loom </param>
        /// </summary>
        /// <returns>DataTable</returns>
        public static DataTable GetWareHouseList(string CompanyName)
        {
            //Warehouse = " And warehousename like '%" + Warehouse + "%'";
            // Get From Database
            DataTable dataTable = new DataTable("Warehouse");
            if (Database.OpenConnection(Utility.MaterialConnectionString))
            {
                Database.GetExecuteReaderCommand(string.Format("SELECT warehousename,srno,ApprovalReq FROM warehousemaster  WHERE companyname='"
                    + CompanyName + "' ORDER BY warehousename"));
                dataTable.Load(Database.myreader);
            }

            return dataTable.Copy();
        }

        /// <summary>
        /// Get List of Plant
        /// <param name="Warehouse">Pass valuse for company Name</param>
        /// </summary>
        /// <returns>DataTable</returns>
        public static DataTable GetPlantList(string CompanyName)
        {
            // Get From Database
            DataTable dataTable = new DataTable("PlantName");
            if (Database.OpenConnection(Utility.MaterialConnectionString))
            {
                Database.GetExecuteReaderCommand(string.Format("select srno,PlantName  from plantmaster where companyname='" + CompanyName + "'  order by PlantName"));
                dataTable.Load(Database.myreader);
            }

            return dataTable.Copy();
        }

        /// <summary>
        /// Get List of Plant
        /// <param name="Warehouse">Pass valuse for company Name and user Name</param>
        /// </summary>
        /// <returns>DataTable</returns>
        public static DataTable GetPlantList_userWise(string CompanyName, string userName)
        {

            // Get From Database
            DataTable dataTable = new DataTable("PlantName");
            if (Database.OpenConnection(Utility.MaterialConnectionString))
            {
                Database.GetExecuteReaderCommand(string.Format("select srno,PlantName  from plantmaster where companyname='" + CompanyName + "'   and (entryuser1 = '" + userName + "' or entryuser2 = '"
                        + userName + "' or entryuser3 = '" + userName + "' or entryuser4 = '" + userName + "' or entryuser5 = '"
                        + userName + "' or approvalauth1 = '" + userName + "' or approvalauth2 = '" + userName + "' or approvalauth3 = '"
                        + userName + "' or approvalauth4 = '" + userName + "' or approvalauth5 = '" + userName + "' ) order by plantname "));
                dataTable.Load(Database.myreader);
            }

            return dataTable.Copy();
        }


        /// <summary>
        /// Get List of To Plant Name
        /// <param name="Warehouse">Pass valuse for company Name, Form Plant Name and user Name</param>
        /// </summary>
        /// <returns>DataTable</returns>
        public static DataTable GetToPlantList_userWise(string CompanyName, string fromPlantName, string userName)
        {

            // Get From Database
            DataTable dataTable = new DataTable("PlantName");
            if (Database.OpenConnection(Utility.MaterialConnectionString))
            {
                Database.myreader = Database.GetExecuteReaderCommand("select  towarehouse,fromwarehouse from plantmaster where companyname = '"
                + CompanyName + "' and plantname = '"
                + fromPlantName + "' and (entryuser1 = '" + userName + "' or entryuser2 = '"
                            + userName + "' or entryuser3 = '" + userName + "' or entryuser4 = '" + userName + "' or entryuser5 = '"
                            + userName + "' or approvalauth1 = '" + userName + "' or approvalauth2 = '" + userName + "' or approvalauth3 = '"
                            + userName + "' or approvalauth4 = '" + userName + "' or approvalauth5 = '" + userName + "' ) group by  towarehouse,fromwarehouse  order by towarehouse");

                dataTable.Load(Database.myreader);

            }
            return dataTable.Copy();
        }
        /// <summary>
        /// Get List of Party name from Small bag party order master
        /// <param name="Warehouse">Pass valuse for company Name</param>
        /// </summary>
        /// <returns>DataTable</returns>
        public static DataTable GetPartyName_SamllBag(string CompanyName)
        {

            // Get From Database
            DataTable dataTable = new DataTable("PlantName");
            if (Database.OpenConnection(Utility.MaterialConnectionString))
            {
                Database.GetExecuteReaderCommand("select PartyName from SmallBagPartyOrderMaster where CompanyName='" + CompanyName + "' group by PartyName order by  PartyName");
                dataTable.Load(Database.myreader);
            }

            return dataTable.Copy();
        }

        /// <summary>
        /// Get List of Party name from choosen orderType
        /// <param name="Warehouse">Pass valuse for company Name</param>
        /// </summary>
        /// <returns>DataTable</returns>
        [Obsolete("Now Orders are directly linked with marketing use GetFIBCPartyName method ")]
        public static DataTable GetAllPartyName(string OrderType, string CompanyName)
        {

            // Get From Database
            DataTable dataTable = new DataTable("PartyName");
            if (Database.OpenConnection(Utility.MaterialConnectionString))
            {

                //where OrderType='" + OrderType + "'  Removed By Rikin 
                Database.GetExecuteReaderCommand("select * from ( select PartyName,'Small Bag' as OrderType   from SmallBagPartyOrderMaster where isfreeze != 'yes' and Companyname = '" + CompanyName + "'" +
                     " Union All select PartyName,'FIBC' as OrderType from FIBCPartyOrderMaster where isfreeze != 'yes' and Companyname = '" + CompanyName + "' ) temp  group by PartyName, " +
                     " OrderType order by PartyName ");
                dataTable.Load(Database.myreader);
            }

            return dataTable.Copy();
        }

        /// <summary>0
        /// Get List of FIBC Party name Order  
        /// /// <param name="Warehouse">Pass valuse for company Name</param>
        /// </summary>
        /// <returns>DataTable</returns>
        public static DataTable GetFIBCPartyName(string CompanyName)
        {
            string strsql = "";
            object obj = Cache.GetCache("MarketingOrderDtl");
            if (obj != null)
                return ((DataTable)obj).Copy();
            DataTable dataTable = new DataTable();
            strsql = string.Format(@"  
                    select   ItemNo as MarketingOrdNo,BuyerOrderNo as PONO,Qty as Quantity,ItemDesc AS BagSize,'' AS WtperPcs  
                    ,0 as BagWt ,RTRIM(BuyerName)  as PartyName,0 AS TotalWt,'' AS IsPrinting,'' AS IsLiner,'' AS OrderType,ORDERDATE,0 AS transid 
                    from dESPATCH..vw_ProductionCompAndMarkInvNo with(nolock) where ItemNO is not null  "); //and ( CompanyName='" + CompanyName + "' or  ProductionCompanyName='" + CompanyName + "') 
            strsql += " and ( CompanyName='" + CompanyName + "' or  ProductionCompanyName='" + CompanyName + "') "; //Change as per instruction from PPC to show all orders which are in export company and Production Company.
            
            //Changes by Rishabh Jain on 16-09-2025 for showing Self Production in HPBL4 for Roll from FIBC WIP entry
            //if (CompanyName == "Plastene India Limited (Unit -II)" || CompanyName == "Plastene India Limited " || CompanyName == "Oswal Extrusion Limited")
            //{
            //    strsql += "UNION ALL select   'Self' as MarketingOrdNo,'Self'as PONO,1 as Quantity,'' AS BagSize,'' AS WtperPcs " +
            //              " ,0 as BagWt ,'RP Production'  as PartyName,0 AS TotalWt,'' AS IsPrinting,'' AS IsLiner,'' AS OrderType,Null,0 AS transid ";
            //}
            //if (CompanyName == "Plastene India Limited ")
            //{
            //    strsql += "UNION ALL select   ' Self' as MarketingOrdNo,' Self'as PONO,1 as Quantity,'' AS BagSize,'' AS WtperPcs " +
            //            " ,0 as BagWt ,' Self Production'  as PartyName,0 AS TotalWt,'' AS IsPrinting,'' AS IsLiner,'' AS OrderType,Null,0 AS transid ";
            //}

            if (CompanyName == "Plastene India Limited (Unit -II)" || CompanyName == "Plastene India Limited " || CompanyName == "Oswal Extrusion Limited" || CompanyName == "HCP Plastene Bulkpack Ltd (Unit - IV)")
            {
                strsql += "UNION ALL select   'Self' as MarketingOrdNo,'Self'as PONO,1 as Quantity,'' AS BagSize,'' AS WtperPcs " +
                          " ,0 as BagWt ,'RP Production'  as PartyName,0 AS TotalWt,'' AS IsPrinting,'' AS IsLiner,'' AS OrderType,Null,0 AS transid ";
            }
            if (CompanyName == "Plastene India Limited " || CompanyName == "HCP Plastene Bulkpack Ltd (Unit - IV)")
            {
                strsql += "UNION ALL select   ' Self' as MarketingOrdNo,' Self'as PONO,1 as Quantity,'' AS BagSize,'' AS WtperPcs " +
                        " ,0 as BagWt ,' Self Production'  as PartyName,0 AS TotalWt,'' AS IsPrinting,'' AS IsLiner,'' AS OrderType,Null,0 AS transid ";
            }
            //Change End

            if (CompanyName == "SRCC Polyfab Pvt Ltd")
                strsql += " and ( CompanyName='" + CompanyName + "' or  ProductionCompanyName='" + CompanyName + "')";

            if (CompanyName == "OLIVA GARDEN S.A")
                strsql += " and companyname like 'oliva%' ";

            strsql += " order by RTRIM(BuyerName) asc,ItemNo asc";
            //            if (CompanyName == "Plastene India Limited (Unit -II)")
            //            {
            //                strsql = string.Format(@"  
            //                    select   'Self' as MarketingOrdNo,'Self'as PONO,1 as Quantity,'' AS BagSize,'' AS WtperPcs
            //                     ,0 as BagWt ,'RP Production'  as PartyName,0 AS TotalWt,'' AS IsPrinting,'' AS IsLiner,'' AS OrderType,Null,0 AS transid 
            //                    UNION
            //                    select   ItemNo as MarketingOrdNo,BuyerOrderNo as PONO,Qty as Quantity,ItemDesc AS BagSize,'' AS WtperPcs  
            //                    ,0 as BagWt ,BuyerName as PartyName,0 AS TotalWt,'' AS IsPrinting,'' AS IsLiner,'' AS OrderType,ORDERDATE,0 AS transid 
            //                    from dESPATCH..vw_ProductionCompAndMarkInvNo with(nolock) where ItemNO is not null  and ( CompanyName='Plastene India Limited (Unit -II)' or  ProductionCompanyName='Plastene India Limited (Unit -II)')   order by PartyName asc ");
            //            }
            //            else if (CompanyName == "Oswal Extrusion Limited")
            //            {
            //                strsql = string.Format(@"  
            //                    select   ItemNo as MarketingOrdNo,BuyerOrderNo as PONO,Qty as Quantity,ItemDesc AS BagSize,'' AS WtperPcs  
            //                    ,0 as BagWt ,BuyerName as PartyName,0 AS TotalWt,'' AS IsPrinting,'' AS IsLiner,'' AS OrderType,ORDERDATE,0 AS transid 
            //                    from dESPATCH..vw_ProductionCompAndMarkInvNo with(nolock) where ItemNO is not null order by PartyName asc ");
            //            }
            //            else
            //            {
            //                strsql = string.Format(@"  
            //                    select   ItemNo as MarketingOrdNo,BuyerOrderNo as PONO,Qty as Quantity,ItemDesc AS BagSize,'' AS WtperPcs  
            //                    ,0 as BagWt ,BuyerName as PartyName,0 AS TotalWt,'' AS IsPrinting,'' AS IsLiner,'' AS OrderType,ORDERDATE,0 AS transid 
            //                    from dESPATCH..vw_ProductionCompAndMarkInvNo with(nolock) where ItemNO is not null order by PartyName asc ");

            //            }

            // Get From Database             
            if (Database.OpenConnection(Utility.MaterialConnectionString))
                dataTable = Database.GetDataTable(strsql);
            //    return Database.GetDataTable(string.Format(@"select PartyName ,MarketingOrdNo,PONO, Quantity,TypeofBag,BagSize,WtperPcs,TotalWt, IsPrinting ,IsLiner, OrderType, Sysdate ,transid from FIBCPartyOrderMaster  where companyName ='{0}' and isfreeze='No' ", CompanyName));

            if (dataTable.Rows.Count != 0) Cache.AddCache(dataTable, -1, "MarketingOrderDtl");
            return dataTable.Copy();
        }

        public static DataTable GetSmallBag(string CompanyName)
        {
            string strsql = "";
            object obj = Cache.GetCache("SmallbagOrderDtl");
            if (obj != null)
                return ((DataTable)obj).Copy();
            DataTable dataTable = new DataTable();
            strsql = string.Format(@"  
                    select SmallBagPartyOrderMaster.*,v.BuyerOrderNo as PONO from SmallBagPartyOrderMaster with(nolock) inner join Despatch.dbo.Vw_MarketingInvoice v on v.ItemNO=MarketingOrdNo and v.ProductionCompanyName=SmallBagPartyOrderMaster.Companyname where isfreeze='no' and SmallBagPartyOrderMaster.Companyname='" + CompanyName + "'"); //and ( CompanyName='" + CompanyName + "' or  ProductionCompanyName='" + CompanyName + "') 

            if (Database.OpenConnection(Utility.MaterialConnectionString))
                dataTable = Database.GetDataTable(strsql);
            //    return Database.GetDataTable(string.Format(@"select PartyName ,MarketingOrdNo,PONO, Quantity,TypeofBag,BagSize,WtperPcs,TotalWt, IsPrinting ,IsLiner, OrderType, Sysdate ,transid from FIBCPartyOrderMaster  where companyName ='{0}' and isfreeze='No' ", CompanyName));

            if (dataTable.Rows.Count != 0) Cache.AddCache(dataTable, -1, "SmallbagOrderDtl");
            return dataTable.Copy();
        }

        /// <summary>
        /// Get List of PO No from PRod_Cutting_Inout
        /// </summary>
        /// <returns>DataTable</returns>
        public static DataTable GetPONOFrmCutingInOut()
        {

            // Get From Database
            DataTable dataTable = new DataTable("PONO");
            if (Database.OpenConnection(Utility.MaterialConnectionString))
            {
                Database.GetExecuteReaderCommand("select distinct(PONO)  from Prod_Cutting_InOut where PONO <> ''");
                dataTable.Load(Database.myreader);
            }

            return dataTable.Copy();
        }

        /// <summary>
        /// Get List of Marketing Order No from PRod_Cutting_Inout
        /// </summary>
        /// <returns>DataTable</returns>
        public static DataTable GetMarktngOrdNoFrmCutingInOut()
        {

            // Get From Database
            DataTable dataTable = new DataTable("MarketingOrdNo");
            if (Database.OpenConnection(Utility.MaterialConnectionString))
            {
                Database.GetExecuteReaderCommand("select distinct(MarketingOrdNo)  from Prod_Cutting_InOut where MarketingOrdNo <> ''");
                dataTable.Load(Database.myreader);
            }

            return dataTable.Copy();
        }
        ///<summaary>
        ///Get from Godown wise list of Destination WareHouse
        /// </summaary>
        /// <returns>Datatable
        /// </returns>
        public static DataTable GetWareHouse_frmWareHouse(string FrmWareHouse, string companyName)
        {
            DataTable dt = new DataTable();
            string where = "";
            string mainWhere = "";
            if (FrmWareHouse == "System.Data.DataRowView") return dt;

            if (Database.OpenConnection(Utility.MaterialConnectionString))
            {
                Database.GetExecuteReaderCommand(string.Format("select FrmGodownSrNo,ToGodownSrNo,IncludeGodownSrNo,ExcludeGodownSrNo from prod_warehouseDestination  " +
                           " where WareHouse='" + FrmWareHouse + "' and vCompanyName='" + companyName + "'"));
                dt.Load(Database.myreader);
            }

            if (dt.Rows.Count > 0)
            {
                if (dt.Rows[0]["FrmGodownSrNo"].ToString() != string.Empty && dt.Rows[0]["ToGodownSrNo"].ToString() != string.Empty)
                {
                    if (where != "")
                    {
                        where += " AND ";
                    }
                    where += " Srno between " + dt.Rows[0]["FrmGodownSrNo"].ToString() + " and " + dt.Rows[0]["ToGodownSrNo"].ToString() + " ";
                }

                if (dt.Rows[0]["ExcludeGodownSrNo"].ToString() != string.Empty)
                {
                    if (where != "")
                    {
                        where += " And ";
                    }
                    where += " srno not in (" + dt.Rows[0]["ExcludeGodownSrNo"].ToString() + ")  ";
                }
                if (dt.Rows[0]["IncludeGodownSrNo"].ToString() != string.Empty)
                {
                    string strslist = dt.Rows[0]["IncludeGodownSrNo"].ToString();
                    string storeid = "";
                    Boolean ispurId = false;
                    if (Database.OpenConnection(Utility.MaterialConnectionString))
                    {
                        string stsql = "select count(*) from Loginentry.dbo.LoginRights with(Nolock) where   (right(Password,1)='p' or right(Password,1)='s') and name='" + FrmMain.UserName + "'";
                        int intrec = Convert.ToInt32(Database.GetExecuteNonQueryCommand_retrun(stsql));
                        if (intrec > 0)
                            ispurId = true;
                        if (ispurId)
                        {
                            Database.myreader = Database.GetExecuteReaderCommand("select distinct top 1 SrNo from MaterialProcessing.dbo.prod_warehouseDestination  where    WareHouse like '%Store%Department%' and vCompanyName='" + companyName + "'");
                            if (Database.myreader.Read())
                            {
                                storeid = Convert.ToString(Database.myreader[0]);
                                if (storeid.Length > 0)
                                    strslist += "," + storeid;
                            }
                        }
                    }
                    if (where != "")
                    {
                        where += " Or ";
                    }
                    where += " srno in (" + strslist + ")  "; // dt.Rows[0]["IncludeGodownSrNo"].ToString()
                }

                if (where != "")
                {
                    mainWhere = " where " + where;
                }
            }

            DataTable dataTable = new DataTable("WarehouseUserWise");
            if (Database.OpenConnection(Utility.MaterialConnectionString))
            {
                Database.GetExecuteReaderCommand(string.Format("select distinct WareHouse as warehousename,SrNo from prod_warehouseDestination "
                    + (mainWhere.Length == 0 ? " WHERE " : mainWhere + " AND ") + " vCompanyName='" + companyName + "' order by WareHouse "));
                dataTable.Load(Database.myreader);
            }

            //if (dataTable.Rows.Count != 0)  
            return dataTable.Copy();
        }

        ///<summaary>
        ///Get Hardcoded Godown from table prod_warehouseDestination
        /// </summaary>
        /// <returns>Datatable
        /// </returns>
        public static DataTable GetWareHouse_HardCoded(string hardCodedGodown)
        {
            DataTable dataTable = new DataTable("Warehouse");
            if (Database.OpenConnection(Utility.MaterialConnectionString))
            {
                Database.GetExecuteReaderCommand(string.Format("select distinct WareHouse as warehousename,SrNo from prod_warehouseDestination where SrNo in (" + hardCodedGodown + ") order by WareHouse "));
                dataTable.Load(Database.myreader);
            }

            //if (dataTable.Rows.Count != 0)  
            return dataTable.Copy();
        }

        ///<summaary>
        ///Get New Roll No as per our discussion
        /// </summaary>
        /// <returns>
        /// </returns>
        public static string RollNo(string companyName, string forWhat, string CompCode, DateTime sysDate, bool blnSmallBag = false, string Prefix = "", string mktorderno = "", bool IsInward = false)
        {

            string RollNo = "";
            // string CompCode = "";
            string monthCode = "";
            int rollIndex = 0;
            string rollSuffix = "";
            if (forWhat.ToString().ToUpper() != "BAILING")
            {
                string monthstring = "ABCDEFGHIJKL";

                monthCode = monthstring.Substring(sysDate.Month - 1, 1);

                //switch (sysDate.Month)
                //{
                //    case 1:
                //        monthCode = "A";
                //        break;
                //    case 2:
                //        monthCode = "B";
                //        break;
                //    case 3:
                //        monthCode = "C";
                //        break;
                //    case 4:
                //        monthCode = "D";
                //        break;
                //    case 5:
                //        monthCode = "E";
                //        break;
                //    case 6:
                //        monthCode = "F";
                //        break;
                //    case 7:
                //        monthCode = "G";
                //        break;
                //    case 8:
                //        monthCode = "H";
                //        break;
                //    case 9:
                //        monthCode = "I";
                //        break;
                //    case 10:
                //        monthCode = "J";
                //        break;
                //    case 11:
                //        monthCode = "K";
                //        break;
                //    case 12:
                //        monthCode = "L";
                //        break;
                //    default:
                //        monthCode = "Mon";
                //        break;
                //}

                if (forWhat.ToUpper().Contains("OUT"))
                {
                    if (forWhat == "OutLoom")
                        RollNo = CompCode + "-L-O" + sysDate.Year.ToString().Substring(2) + monthCode;
                    else if (forWhat == "OutNeedleLoom")
                        RollNo = CompCode + "-NL-O" + sysDate.Year.ToString().Substring(2) + monthCode;
                }
                else
                {
                    if (forWhat == "Loom")
                        RollNo = CompCode + "-L-I" + sysDate.Year.ToString().Substring(2) + monthCode;
                    else if (forWhat == "NeedleLoom")
                        RollNo = CompCode + "-NL-I" + sysDate.Year.ToString().Substring(2) + monthCode;
                }

                string DateFilter = " and Sysdate between '" + sysDate.Year.ToString() + "-01-01' and '" + sysDate.Year.ToString() + "-12-31'";

                if (Database.OpenConnection(Utility.MaterialConnectionString))
                {
                    if (forWhat == "Loom")
                    {
                        Database.myreader = Database.GetExecuteReaderCommand("select max(isnull(RollIndex,0)) + 1  from misloomproductionentrynew where CompanyName='" + companyName + "'" + DateFilter + "  ");//order by RollIndex desc top 1  isnull(RollIndex ,0) + 1 order by RollIndex desc
                    }
                    else if (forWhat == "NeedleLoom")
                    {
                        Database.myreader = Database.GetExecuteReaderCommand("select max(isnull(RollIndex,0)) + 1  from misneedleloomproductionentry where CompanyName='" + companyName + "'" + DateFilter + " "); //top 1 isnull(RollIndex ,0) + 1 order by TRANSID desc
                    }
                    else if (forWhat == "OutLoom")// This is also used for liner roll as well
                    {
                        Database.myreader = Database.GetExecuteReaderCommand(" select max(isnull(RollIndex,0)) + 1   as 'rollindex' from MISOutsideRollEntry where  companyname = '" + companyName + "'" + DateFilter + " ");//order by transid desc  
                    }
                    else if (forWhat == "OutNeedleLoom")// This is also used for liner roll as well
                    {
                        Database.myreader = Database.GetExecuteReaderCommand(" select max(isnull(RollIndex,0)) + 1   as 'rollindex' from MISNeedleOutsideRollEntry where  companyname = '" + companyName + "'" + DateFilter + "  "); //top 1 isnull(RollIndex ,0) + 1  order by transid desc 
                    }
                }

                if (Database.myreader.Read())
                    if (Database.myreader[0].ToString().Length == 0)
                        rollIndex = 1;
                    else
                        rollIndex = Convert.ToInt32(Database.myreader[0].ToString());
                else
                    rollIndex = 1;

                rollSuffix = string.Format("{0:00000.#}", rollIndex);
                RollNo = RollNo + "-" + rollSuffix;
            }
            else
            {
                //string[] split = CompCode.Split('+');

                if (Database.OpenConnection(Utility.MaterialConnectionString))
                {
                    if (forWhat.ToString().ToUpper() == "BAILING")
                    {
                        if (blnSmallBag)
                        {
                            if (IsInward)
                            {
                                Database.myreader = Database.GetExecuteReaderCommand("select top 1 isnull(IndexNo,0) + 1 as 'IndexNo' from SmallBagBaillingEntry with(nolock) where BuyerOrdNo = '" + Convert.ToString(CompCode) + "' and companyname = '" + companyName + "' "
                                    + (mktorderno.Length > 0 ? " and MarketingOrdNo='" + mktorderno + "'" : "") + "  order by ProductionSmallBagEntryID desc");
                            }
                            else
                            {
                                Database.myreader = Database.GetExecuteReaderCommand("select top 1 isnull(IndexNo,0) + 1 as 'IndexNo' from SmallBagBaillingEntry with(nolock) where BuyerOrdNo = '" + Convert.ToString(CompCode) + "' and companyname = '" + companyName + "' and MarketingOrdNo='" + mktorderno + "'  order by ProductionSmallBagEntryID desc");
                            }
                        }
                        else
                        {
                            if (IsInward)
                            {
                                //Change By Rishabh Jain on 10/09/2025 for proper bailno against buyer ordno
                                //Database.myreader = Database.GetExecuteReaderCommand("select top 1 isnull(IndexNo,0) + 1 as 'IndexNo' from FIBCBailingEntry with(nolock) where BuyerOrdNo = '" + Convert.ToString(CompCode) + "' and companyname = '" + companyName + "'" +
                                //    (mktorderno.Length > 0 ? "  and MarketingOrdNo='" + mktorderno + "'" : "") + " order by IndexNo desc");
                                Database.myreader = Database.GetExecuteReaderCommand("select top 1 isnull(IndexNo,0) + 1 as 'IndexNo' from FIBCBailingEntry with(nolock) where BuyerOrdNo = '" + Convert.ToString(CompCode) + "' and companyname = '" + companyName + "'" + " order by IndexNo desc");
                                //Change End
                            }
                            else
                            {
                                //Change By Rishabh Jain on 10/09/2025 for proper bailno against buyer ordno
                                //Database.myreader = Database.GetExecuteReaderCommand("select top 1 isnull(IndexNo,0) + 1 as 'IndexNo' from FIBCBailingEntry with(nolock) where BuyerOrdNo = '" + Convert.ToString(CompCode) + "' and companyname = '" + companyName + "' and MarketingOrdNo='" + mktorderno + "' order by IndexNo desc");
                                Database.myreader = Database.GetExecuteReaderCommand("select top 1 isnull(IndexNo,0) + 1 as 'IndexNo' from FIBCBailingEntry with(nolock) where BuyerOrdNo = '" + Convert.ToString(CompCode) + "' and companyname = '" + companyName + "' order by IndexNo desc");
                                //Change End
                            }
                        }

                    }
                }

                if (Database.myreader.Read())
                    rollIndex = Convert.ToInt32(Database.myreader[0].ToString());
                else
                    rollIndex = 1;

                rollSuffix = string.Format("{0:00}", rollIndex);

                RollNo = Convert.ToString(Prefix) + "-BAIL-" + rollSuffix;

            }
            return RollNo;
        }

        ///<summaary>
        ///Get MISColor Master
        /// </summaary>
        /// <returns>Datatable
        /// </returns>
        public static DataTable GetMisColor()
        {
            if (Database.OpenConnection(Utility.MaterialConnectionString))
                //return Database.GetDataTable("select color  from MISLoomColorMaster  group by color order by Color");
                return Database.GetDataTable("select distinct color from( select distinct LTRIM(RTRIM(color)) AS color  from MISLoomColorMaster   union all select distinct LTRIM(RTRIM(Colour)) from MISOutsideRollEntry  where Sysdate>='2021-04-01' )A where isnull(a.Color,'')<>'' and ISNUMERIC(Color)=0 and len(a.color)>1 order by 1");
            return null;
        }

        ///<summaary>
        ///Get sector Master
        /// </summaary>
        /// <returns>Datatable
        /// </returns>
        public static DataTable GetSector()
        {
            if (Database.OpenConnection(Utility.MaterialConnectionString))
                return Database.GetDataTable("select sectorname FROM sectormaster order by sectorname");
            return null;
        }

        ///<summaary>
        ///CSV Convert to DataTable
        /// </summaary>
        /// <returns>Datatable
        /// </returns>
        public static DataTable GetCsvFileToDatatable(string path, bool IsFirstRowHeader, bool IsCSV = true, string ext = "csv")
        {

            string header = "No";
            string sql = string.Empty;
            DataTable dataTable = null;
            string pathOnly = string.Empty;
            string fileName = string.Empty;
            try
            {
                pathOnly = Path.GetDirectoryName(path);
                fileName = Path.GetFileName(path);
                sql = @"SELECT * FROM [" + fileName + "]";
                if (IsFirstRowHeader)
                {
                    header = "Yes";
                }
                if (IsCSV && ext.ToLower() == "csv")
                {
                    using (OleDbConnection connection = new OleDbConnection(@"Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" +
                        pathOnly +
                    ";Extended Properties=\"Text;HDR=" + header + "\""))
                    {
                        using (OleDbCommand command = new OleDbCommand(sql, connection))
                        {
                            using (OleDbDataAdapter adapter = new OleDbDataAdapter(command))
                            {
                                dataTable = new DataTable();
                                dataTable.Locale = CultureInfo.CurrentCulture;
                                adapter.Fill(dataTable);
                            }
                        }
                    }
                }
                else if (!IsCSV && ext.ToLower() == "xlsx")
                {
                    DataTable dtResult = null;
                    int totalSheet = 0; //No of sheets on excel file  
                    using (OleDbConnection objConn = new OleDbConnection(
                    "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + path +
                    ";Extended Properties='Excel 12.0;HDR=YES;IMEX=1;';"))
                    {
                        objConn.Open();
                        OleDbCommand cmd = new OleDbCommand();
                        OleDbDataAdapter oleda = new OleDbDataAdapter();
                        DataSet ds = new DataSet();
                        DataTable dt = objConn.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null);
                        string sheetName = string.Empty;
                        if (dt != null)
                        {
                            var tempDataTable = (from dataRow in dt.AsEnumerable()
                                                 where !dataRow["TABLE_NAME"].ToString().Contains("FilterDatabase")
                                                 select dataRow).CopyToDataTable();
                            dt = tempDataTable;
                            totalSheet = dt.Rows.Count;
                            sheetName = dt.Rows[0]["TABLE_NAME"].ToString();
                        }
                        cmd.Connection = objConn;
                        cmd.CommandType = CommandType.Text;
                        cmd.CommandText = "SELECT * FROM [" + sheetName + "]";
                        oleda = new OleDbDataAdapter(cmd);
                        oleda.Fill(ds, "excelData");
                        dtResult = ds.Tables["excelData"];
                        objConn.Close();
                        return dtResult; //Returning Dattable  
                    }
                }
                else
                {
                    using (OleDbConnection connection = new OleDbConnection(@"Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + path +
                    ";Extended Properties='Excel 8.0;HDR=Yes;IMEX=1';"))
                    {
                        OleDbCommand cmdExcel = new OleDbCommand();
                        cmdExcel.Connection = connection;
                        connection.Open();
                        DataTable dtExcelSchema;
                        dtExcelSchema = connection.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null);
                        connection.Close();
                        connection.Open();
                        OleDbDataAdapter da = new OleDbDataAdapter();
                        DataTable ds = new DataTable();
                        string SheetName = dtExcelSchema.Rows[0]["TABLE_NAME"].ToString();
                        cmdExcel.CommandText = "SELECT * From [" + SheetName + "]";
                        da.SelectCommand = cmdExcel;
                        da.Fill(ds);
                        connection.Close();
                        dataTable = new DataTable();
                        dataTable = ds;
                    }
                }
                return dataTable;
            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
            }
            finally
            {
                //return dataTable;
            }
            return dataTable;
        }
        // by Rikin 01-jul-2015 
        // Load a CSV file into an array of rows and columns.
        // Assume there may be blank lines but every line has
        // the same number of fields.
        public static string[,] LoadCsv(string filename)
        {
            // Get the file's text.
            string whole_file = System.IO.File.ReadAllText(filename);

            // Split into lines.
            whole_file = whole_file.Replace('\n', '\r');
            string[] lines = whole_file.Split(new char[] { '\r' },
                StringSplitOptions.RemoveEmptyEntries);

            // See how many rows and columns there are.
            int num_rows = lines.Length;
            int num_cols = lines[0].Split(',').Length;

            // Allocate the data array.
            string[,] values = new string[num_rows, num_cols];

            // Load the array.
            for (int r = 0; r < num_rows; r++)
            {
                string[] line_r = lines[r].Split(',');
                for (int c = 0; c < num_cols; c++)
                {
                    values[r, c] = line_r[c];
                }
            }

            // Return the values.
            return values;
        }
        ///<summaary>
        ///Excel Convert to DataTable
        /// </summaary>
        /// <returns>Datatable
        /// </returns>
        public DataTable ExcelFileToDatatable(string path, bool IsFirstRowHeader)
        {
            DataTable dt = new DataTable();
            return dt;
            //if (strFileType.Trim().ToLower() == ".xlsx" || strFileType.Trim().ToLower() == ".xls")
            //{
            //    connString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + path + ";Extended Properties='Excel 12.0 xml;HDR=YES;'";

            //    string query = "";

            //    OleDbConnection conn = new OleDbConnection(connString);

            //    if (conn.State == ConnectionState.Closed)
            //        conn.Open();

            //    DataTable Sheets = conn.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null);

            //    foreach (DataRow dr in Sheets.Rows)
            //    {
            //        string sht = dr[2].ToString().Replace("'", "");
            //        query = "select * from [" + sht + "]";
            //        break;
            //    }

            //    OleDbCommand cmd = new OleDbCommand(query, conn);
            //    dt = new DataTable();

            //    OleDbDataAdapter da = new OleDbDataAdapter(cmd);


            //    da.Fill(dt);

            //}
        }
        ///<summaary>
        ///Warehouse Approval Required
        ///When user transfer any material at receiving end approval is required or not
        /// </summaary>
        /// <returns>int 0 or1
        /// </returns>
        public static int IsWareHouseApprovalRequired(string CompanyName, string WareHouseTo)
        {

            int a = (int)Database.ExecuteScalar(@"select cast(isnull(ApprovalReq,0) as int) from warehousemaster where companyname='" + CompanyName + "' and warehousename='" + WareHouseTo + "'");
            return a;
        }
        ///<summaary>
        ///Make Query for Update New Party against Old Party
        /// </summaary>
        /// <returns>String
        /// </returns>
        public static string ScriptUpdatePartyName(string RollNo, string CompanyName, string RollType, string Category, string NewPartyName, string NewPONo)
        {
            string script = "";
            if (Database.OpenConnection(Utility.MaterialConnectionString))
            {
                if (RollType.ToUpper() == "LOOM" && Category.ToUpper() == "IN")
                {
                    script = "Update misloomProductionentrynew set Partyname='" + NewPartyName + "',MarketingOrderno='" + NewPONo + "' where RollNo='" + RollNo + "' and CompanyName='" + CompanyName + "'";
                }
                else if (RollType.ToUpper() == "LOOM" && Category.ToUpper() == "OUT")
                {
                    script = "Update MISOutsideRollEntry set PartyName='" + NewPartyName + "',MarketingInvNo='" + NewPONo + "' where RollNo='" + RollNo + "' and CompanyName='" + CompanyName + "'";
                }
                else if (RollType.ToUpper() == "NEEDLELOOM" && Category.ToUpper() == "IN")
                {
                    script = "Update misneedleloomproductionentry set PartyName = '" + NewPartyName + "', MarketingOrdNo='" + NewPONo + "' where RollNo='" + RollNo + "' and CompanyName='" + CompanyName + "'";
                }
                else if (RollType.ToUpper() == "NEEDLELOOM" && Category.ToUpper() == "OUT")
                {
                    script = "Update MISNeedleOutsideRollEntry set Party_Name = '" + NewPartyName + "', MarkatingInvNo='" + NewPONo + "' where RollNo='" + RollNo + "' and Companyname='" + CompanyName + "'";
                }
            }
            return script;
        }
        /// <summary>
        /// Get List of Active Loom Allocation Master  
        /// /// <param name="companyName">Pass valuse for company Name</param>
        /// </summary>
        /// <returns>DataTable</returns>
        public static DataTable GetLoomAllocationMaster(int Companyid, DateTime dt)
        {
            // Get From Database             
            if (Database.OpenConnection(Utility.MaterialConnectionString))

                return Database.GetDataTable(string.Format(@"select  LA.CompanyId, LoomNo, Material, Type, Size, WarpDNR, WeftDNR, WarpMesh, WeftMesh, RFWidth, RFNo, LF.RFMesh,ReqGSM, Color, Sector, PartyName, PONO, AllocationDate, ToDate,  
Shrinkage, WarpBobin, WeftBobin,LA.Remarks,isnull(LA.AllocationType ,'') as AllocationType,LA.FGItemCode,LA.CondWarp,LA.CondWeft,LA.MonoYarn,LA.CondDNR,LA.Vent  from Prod_LoomAlocationMaster LA inner join  prod_LoomformulaMaster LF on LA.formulaID=lf.formulaID    
where (('{0}' between allocationdate and  todate) or ('{0}' >=allocationdate and todate is null))and LA.companyId={1} and LA.isActive='Yes' and LF.isActive='Yes'", dt.ToString("yyyy-MM-dd"), Companyid));

            //                return Database.GetDataTable(string.Format(@"select   CompanyId, LoomNo, Material, Type, Size, WarpDNR, WeftDNR, WarpMesh, WeftMesh, RFWidth, RFNo, ReqGSM, Color, Sector, PartyName, PONO, AllocationDate, ToDate,  Shrinkage, WarpBobin, WeftBobin
            //                        from    loomAlocationMAster where getdate() between allocationdate and isnull(todate,getdate()) and companyId={0}", Companyid));

            return null;
        }
        /// <summary>
        /// Get List of Bobin at Loom Gogown with till date stock  
        /// /// <param name="Warehouse">Pass company Namea and Godown name</param>
        /// </summary>
        /// <returns>DataTable</returns>
        public static DataTable GetBobinForLoom(string CompanyName, string GodownName)
        {
            // Get From Database             
            if (Database.OpenConnection(Utility.MaterialConnectionString))
                return Database.GetDataTable(string.Format(@"select itemCode,ItemName,Round(StkInHAnd ,2) StkInHAnd  from warehouse with(nolock) where StkInHand >0 and companyName='{0}' and warehouseName='{1}' order by ItemName", CompanyName, GodownName));

            return null;
        }

        /// <summary>
        /// Get List of Mono Yarn at Loom Gogown with till date stock  
        /// /// <param name="Warehouse">Pass company Namea and Godown name</param>
        /// </summary>
        /// <returns>DataTable</returns>
        public static DataTable GetMonoYarnForLoom(string CompanyName, string GodownName)
        {
            // Get From Database             
            if (Database.OpenConnection(Utility.MaterialConnectionString))
                return Database.GetDataTable(string.Format(@"select itemCode,ItemName,Round(StkInHAnd ,2) StkInHAnd  from warehouse with(nolock) where StkInHand >0 and companyName='{0}' and warehouseName='{1}' and Itemname like '%Monofilament%yarn%' order by ItemName", CompanyName, GodownName));

            return null;
        }
        /// <summary>
        /// Get List of Conductive Yarn at Loom Gogown with till date stock  
        /// /// <param name="Warehouse">Pass company Namea and Godown name</param>
        /// </summary>
        /// <returns>DataTable</returns>
        public static DataTable GetConductiveForLoom(string CompanyName, string GodownName)
        {
            // Get From Database             
            if (Database.OpenConnection(Utility.MaterialConnectionString))
                return Database.GetDataTable(string.Format(@"select itemCode,ItemName,Round(StkInHAnd ,2) StkInHAnd  from warehouse with(nolock) where StkInHand >0 and companyName='{0}' and warehouseName='{1}' and Itemname like '%Conductive%Tape%' order by ItemName", CompanyName, GodownName));

            return null;
        }
        /// <summary>
        /// Get List of Bobin at Loom Gogown with till date stock  
        /// /// <param name="Warehouse">Pass company Namea and Godown name</param>
        /// </summary>
        /// <returns>DataTable</returns>
        public static bool CheckDateFreeze(string CompanyName, DateTime date)
        {
            // Get From Database             
            if (Database.OpenConnection(Utility.MaterialConnectionString))
                return (bool)Database.ExecuteScalar(string.Format(@"select case when sysdate is null then 0 else 1 end as Sysdate from freezeProdEntry where CompanyName='{0}' and sysdate>='{1}'", CompanyName, date));
            //return (bool)Database.ExecuteScalar(string.Format(@"select case when sysdate is null then 0 else 1 end as Sysdate from freezeMRN where CompanyName='{0}' and sysdate>='{1}'", CompanyName, date));

            return false;
        }

        public static bool updateTableData(string tableName, string colvalue, string whereConValue)
        {
            string dynamicQuery = "update " + tableName + " set vToGodown = " + "'" + colvalue + "'" + " where iSrNo IN(" + whereConValue + ")";

            if (Database.GetExecuteNonQueryCommand(dynamicQuery))
                return true;
            else
                return false;
        }

        #region FIFO
        public static double GettingRate(string Itemcode, double dblQty)
        {


            ArrayList ArrIssuedQty = new ArrayList();
            ArrayList ArrDate = new ArrayList();
            ArrayList ArrRate = new ArrayList();
            ArrayList ArrSrNo = new ArrayList();

            int t = 0;
            double IssuedLessQty = 0;
            double AvgRate = 0;
            double Leftqty = 0;

            ArrIssuedQty.Clear();
            ArrDate.Clear();
            ArrRate.Clear();
            ArrSrNo.Clear();

            Database.myreader = Database.GetExecuteReaderCommand("select AcceptedQty,sysDate,case when amount > 0 then Rate - discamt else Rate - discamt end  from vw_storeinwards where Itemcode = '"
                           + Itemcode + "'  and companyname = '"
                            + frmDefaultVale.CompanyName + "' and cancel = '' order by sysdate ");


            while (Database.myreader.Read())
            {
                ArrIssuedQty.Add(Database.myreader[0].ToString());
                ArrDate.Add(Database.myreader[1].ToString());
                ArrRate.Add(Database.myreader[2].ToString());
            }
            Database.myreader.Close();

            double OutwardsQty = 0;


            Database.myreader = Database.GetExecuteReaderCommand("select stkinhand from item where Itemcode = '"
                        + Itemcode + "'and companyname = '"
                         + frmDefaultVale.CompanyName + "'");

            if (Database.myreader.Read())
            {
                if (Database.myreader[0].ToString() != "")
                    OutwardsQty = Convert.ToDouble(Database.myreader[0].ToString());



            }
            Database.myreader.Close();

            double ReqdQty = Convert.ToDouble(dblQty);
            double PendingQty = OutwardsQty;
            int count = 0;
            //if (ArrIssuedQty.Count <= 1)
            //    count = ArrIssuedQty.Count;
            //else
            count = ArrIssuedQty.Count - 1;

            if (count == -1)
            {
                //MessageBox.Show("Issued Qty not available");
                return 0;
            }


            for (int i = ArrIssuedQty.Count - 1; i >= 0 && PendingQty > 0; i--)
            {
                Leftqty = PendingQty;
                PendingQty = PendingQty - Convert.ToDouble(ArrIssuedQty[i].ToString());
                if (count != 1)
                    count = count - 1;
                if (PendingQty <= 0)
                {
                    t = i;
                    IssuedLessQty = Convert.ToDouble(ArrIssuedQty[count].ToString()) + PendingQty;
                    break;
                }
            }

            PendingQty = ReqdQty + Leftqty;
            double totalqty = 0;
            double totalAmount = 0;
            for (int i = t; i < ArrIssuedQty.Count && PendingQty > 0; i++)
            {
                PendingQty = PendingQty - Convert.ToDouble(ArrIssuedQty[i].ToString());
                count = count + 1;

                if (i == t)
                {
                    totalqty += PendingQty;
                    totalAmount += (PendingQty) * Convert.ToDouble(ArrRate[i].ToString());
                    AvgRate = totalAmount / totalqty;
                }
                else
                {
                    if (PendingQty < 0)
                    {
                        totalqty += Convert.ToDouble(ArrIssuedQty[i].ToString()) + PendingQty;
                        totalAmount += ((Convert.ToDouble(ArrIssuedQty[i].ToString()) + PendingQty) * Convert.ToDouble(ArrRate[i].ToString()));
                        AvgRate = totalAmount / totalqty;
                    }
                    else
                    {
                        totalqty += Convert.ToDouble(ArrIssuedQty[i].ToString());
                        totalAmount += ((Convert.ToDouble(ArrIssuedQty[i].ToString())) * Convert.ToDouble(ArrRate[i].ToString()));
                        AvgRate = totalAmount / totalqty;
                    }
                }
                if (PendingQty <= 0)
                {
                    AvgRate = totalAmount / totalqty;
                    break;
                }
            }

            AvgRate = Math.Round(AvgRate, 3);

            return AvgRate;


        }
        #endregion

        public static DateTime ERPBackDateSettings()
        {
            object d = Database.ExecuteScalar("select back_days from Loginentry.dbo.erp_setting");
            int days = (int)d;
            DateTime dt = new DateTime();
            dt = DateTime.Now.AddDays(days);
            dt.ToString("dd-MM-yyyy");
            return dt;
        }
        public static string GetMktInvoice(string PONO)
        {
            // Get From Database             
            string MktNo = "";
            DataTable dt = Database.GetDataTable(string.Format(@"select top 1 MarketingInvNo from MarketingInvoice where BuyerOrderNo='{0}'", PONO));
            if (dt.Rows.Count > 0)
                MktNo = dt.Rows[0][0].ToString();

            return MktNo;
        }

        public static bool InsertItemInWareHouse(string itemcode, string warehouse, string companyname)
        {
            try
            {

                string InsertString = "";
                string strsql = "Select count(*) from warehouse where companyname='" + companyname + "' and itemcode='" + itemcode
                   + "' and WareHouseName='" + warehouse + "'";

                object introw = (object)Database.GetExecuteNonQueryCommand_retrun(strsql);
                if (Convert.ToInt16(introw) == 0)
                {
                    DataTable itemDt = Database.GetDataTable("select * from item where ItemCode = '" + itemcode + "' and CompanyName = '"
                                + companyname + "'");
                    string DtlSubGroupName = "";
                    itemDt.Rows[0][4] = "0";
                    itemDt.Rows[0][5] = "0";
                    itemDt.Rows[0][6] = "0";
                    itemDt.Rows[0][8] = "0";
                    itemDt.Rows[0][12] = "0";
                    itemDt.Rows[0][13] = "0";
                    itemDt.Rows[0][14] = "0";
                    itemDt.Rows[0][18] = "0";
                    DtlSubGroupName = Convert.ToString(itemDt.Rows[0]["DtlSubGroupName"]);
                    for (int j = 0; j <= 19; j++)
                    {
                        InsertString += "'" + itemDt.Rows[0][j].ToString() + "',";
                    }

                    if (InsertString != "")
                        InsertString = InsertString.Substring(0, InsertString.Length - 1);
                    if (Database.GetExecuteNonQueryCommand("insert into warehouse values(" + InsertString + ",'" + warehouse + "','" + DtlSubGroupName + "')"))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }

                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }

        }

        public static string GeneratePackingListInvno(string companyname)
        {
            //int srno = 0;
            int InvoiceSeries = 0;
            string Invno = "";
            string StartDate = "";
            string EndDate = "";
            //string temp = "";
            using (System.Data.SqlClient.SqlConnection conn = new System.Data.SqlClient.SqlConnection(Utility.MaterialConnectionString))
            {

                conn.Open();
                System.Data.SqlClient.SqlCommand command = new System.Data.SqlClient.SqlCommand("Select * from yearMaster where yr = '" + frmDefaultVale.Year + "'", conn);
                System.Data.SqlClient.SqlDataReader reader = command.ExecuteReader();

                if (reader.Read())
                {
                    StartDate = Convert.ToDateTime(reader["fromdate"].ToString()).ToString("yyyy-MM-dd");
                    EndDate = Convert.ToDateTime(reader["todate"].ToString()).ToString("yyyy-MM-dd");
                }
                reader.Close();
                command.Dispose();

                System.Data.SqlClient.SqlCommand command1 = new System.Data.SqlClient.SqlCommand("Select dispatchcode from Despatch.dbo.factoryinfo where name = '" + companyname + "'", conn);
                System.Data.SqlClient.SqlDataReader reader1 = command1.ExecuteReader();
                if (reader1.Read())
                    Invno = reader1[0].ToString();
                reader1.Close();
                command1.Dispose();

                if (companyname == "HCP Plastene Bulkpack Ltd")
                    Invno = "GPL";
                if (companyname == "HCP Plastene Bulkpack Ltd (Unit - II)")
                    Invno = "GPL2";

                string strsql = "select max(isnull(Invno,0)) as Invno from( " +
                    " select CAST(max(isnull(cast(replace(PACKINGLISTNO,'" + Invno + "/PKT/'  ,'') as int),0)) AS INT) as Invno from MISRollforDespatch where Companyname ='" + companyname + "'     union all " +
                    " select CAST(max(isnull(cast(replace(PACKINGLISTNO,'" + Invno + "/PKT/'  ,'') as int),0)) AS INT) as Invno from Tapepackingentry where Companyname ='" + companyname + "'  union all " +
                    " select CAST(max(isnull(cast(replace(PACKINGLISTNO,'" + Invno + "/PKT/'  ,'') as int),0)) AS INT) as Invno from FIBCDespatch where Companyname ='" + companyname + "'   union all " +
                    " select CAST(max(isnull(cast(replace(PACKINGLISTNO,'" + Invno + "/PKT/' ,'') as int),0)) AS INT) as Invno from SmallBagBailForDespatch where Companyname ='" + companyname + "'  ) as f";
                System.Data.SqlClient.SqlCommand command2 = new System.Data.SqlClient.SqlCommand(strsql, conn);
                System.Data.SqlClient.SqlDataReader reader2 = command2.ExecuteReader();
                //Database.myreader = Database.GetExecuteReaderCommand(strsql);
                if (reader2.Read())
                    InvoiceSeries = Convert.ToInt32(reader2[0]);
                reader2.Close();
                command2.Dispose();
                InvoiceSeries = InvoiceSeries + 1;
                Invno = Invno + "/PKT/" + InvoiceSeries.ToString();
                conn.Close();
            }
            return Invno;
        }

        public static bool ApprovalForBoth(string companyname, string warehousename, string username)
        {
            try
            {
                int intrec = 0;
                if (Database.OpenConnection(Utility.MaterialConnectionString))
                {
                    string strsql = "select count(*) from warehousemaster where companyname = '{0}' and warehousename='{2}'  and (Approvalauth1 ='{1}' or Approvalauth2 ='{1}' or Approvalauth3 ='{1}' or Approvalauth4 ='{1}' or Approvalauth5 ='{1}' )";
                    strsql = string.Format(strsql, companyname, username, warehousename);
                    DataTable dt = Database.GetDataTable(strsql);
                    if (dt.Rows.Count > 0)
                        intrec = Convert.ToInt32(dt.Rows[0][0]);
                }
                return (intrec == 1 ? true : false);
            }
            catch
            {
                return false;
            }

        }
        public static void LoadInternalVendorUnlockDT(System.Windows.Forms.ComboBox CB, string companyname, DateTime date)
        {
            try
            {
                using (System.Data.SqlClient.SqlConnection conn = new System.Data.SqlClient.SqlConnection(Utility.MaterialConnectionString))
                {
                    string strsql = "select ledgername as VendorName,srno VendorCode from ledgermaster with(NoLock) where   ( Category='Customer-Supplier' OR  Category='Service Provider' OR Category='Supplier'    )  and companyname = '" +
                    companyname + "' and Isnull(IsAccClose,'no')='no'  and (('" + date.ToString("yyyy-MM-dd") + "' between fromdate and todate) or (fromdate <= '"
                    + date.ToString("yyyy-MM-dd") + "' and todate is null )) "
                    + " and NewGSTNo in ( Select NewGSTNo from factoryinfo) order by ledgername";

                    System.Data.SqlClient.SqlCommand command = new System.Data.SqlClient.SqlCommand(strsql, conn);
                    conn.Open();
                    System.Data.SqlClient.SqlDataReader reader = command.ExecuteReader();
                    DataTable DT = new DataTable();
                    DT.Load(reader);
                    CB.DataSource = DT;
                    CB.DisplayMember = mmCommonVariable.FieldName.VendorName;
                    CB.ValueMember = mmCommonVariable.FieldName.VendorId;
                    //while (reader.Read())
                    //{
                    //    CB.Items.Add(Convert.ToString(reader["firmname"]));
                    //}
                    reader.Close();
                }
            }
            catch (Exception ex)
            {
                ex.Message.ToString();
            }
        }

        public static DataTable ToDataTable<T>(List<T> items)
        {
            DataTable dataTable = new DataTable(typeof(T).Name);
            //Get all the properties
            PropertyInfo[] Props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (PropertyInfo prop in Props)
            {
                //Setting column names as Property names
                dataTable.Columns.Add(prop.Name);
            }
            foreach (T item in items)
            {
                var values = new object[Props.Length];
                for (int i = 0; i < Props.Length; i++)
                {
                    //inserting property values to datatable rows
                    values[i] = Props[i].GetValue(item, null);
                }
                dataTable.Rows.Add(values);
            }
            //put a breakpoint here and check datatable
            return dataTable;
        }
    }

}
