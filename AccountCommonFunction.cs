using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using ERP.Classes;
using System.IO;
using System.Data.OleDb;
using System.Globalization;

namespace ERP
{
    /// <summary>
    /// Account common function
    /// </summary>
    ///test
    public class AccountCommonFunction : Tools.ToolsCommonFunction
    {
        private static Random _random = new Random();

        /// <summary>
        /// GetGroupVoucherList : to Get voucharType
        /// </summary>
        /// <returns></returns>
        public static DataTable GetGroupVoucherList()
        {
            Database.OpenConnection(Utility.MaterialConnectionString);
            DataTable dt = Database.GetDataTable("select distinct GroupName from VoucherType with(Nolock) order by GroupName");
            return dt;
        }
        public static bool ValidateGSTnumber(string GSTNo)
        {
            try
            {
                //24 AACCP 1688 G 1 Z F
                //12 34567 8901 2 3 4 5
                if (GSTNo == "N.A." || GSTNo == "UN-REGISTER")
                    return true;
                if (
                   CheckIfNumeric(GSTNo.Substring(0, 2)) &&
                   !CheckIfNumeric(GSTNo.Substring(2, 5)) &&
                   CheckIfNumeric(GSTNo.Substring(7, 4)) &&
                   !CheckIfNumeric(GSTNo.Substring(11, 1)) &&
                   CheckIfNumeric(GSTNo.Substring(12, 1)) &&
                   !CheckIfNumeric(GSTNo.Substring(13, 1))
                   )
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                ex.Message.ToString();
                return false;
            }
        }
        private static bool CheckIfNumeric(string input)
        {
            if (Regex.IsMatch(input, @"^\d+$"))
            {
                return true;
            }
            else
            {
                return false;
            }
        }


        /// <summary>
        /// GetInterCompanyLedgerList : Get the list of all InterCompany with 2 company 
        /// </summary>
        /// <param name="CompanyName1"></param>
        /// <param name="CompanyName2"></param>
        /// <returns></returns>
        public static DataTable GetInterCompanyLedgerList(string CompanyName1, string CompanyName2)
        {
            Database.OpenConnection(Utility.MaterialConnectionString);
            DataTable dt = Database.GetDataTable(@"
select l.srno AS LedgerId, l.LedgerName
from ac_interCompanyLedger icl with(Nolock)
inner join LedgerMaster l with(Nolock) on icl.LedgerId=l.srno
inner join FactoryInfo fi with(Nolock) on fi.SrNo=icl.InterCompanyID
where l.CompanyName=@CompanyName1 and fi.Name=@CompanyName2
",
        new Database.Parameter("@CompanyName1", CompanyName1),
        new Database.Parameter("@CompanyName2", CompanyName2)
        );
            return dt;
        }
        /// <summary>
        /// GetInterCompanyLedgerList : Get the list of all InterCompany 
        /// </summary>
        /// <param name="CompanyName1"></param>
        /// <returns></returns>
        public static DataTable GetInterCompanyLedgerList(string CompanyName1, int LedgerSrno = 0)
        {
            Database.OpenConnection(Utility.MaterialConnectionString);

            DataTable dt = Database.GetDataTable(@"
select l.CompanyName, l.LedgerName, fi.Name AS InterCompanyName, icl.TransID AS transId
from ac_interCompanyLedger icl with(Nolock)
inner join LedgerMaster l with(Nolock) on icl.LedgerId=l.srno
inner join FactoryInfo fi with(Nolock) on fi.SrNo=icl.InterCompanyID
where l.CompanyName=@CompanyName " + (LedgerSrno == 0 ? "" : " and l.srno=" + LedgerSrno), new Database.Parameter("@CompanyName", CompanyName1));
            return dt;

            //' Need to check with SQL
        }

        /// <summary>
        /// GetInterCompanyLedgerList : Get the list of all InterCompany 
        /// </summary>
        /// <param name="CompanyName1"></param>
        /// <returns></returns>
        public static DataTable GetInterCompanyLedgerList(string CompanyName1, string ledgerName = null, bool ChkLedger = false)
        {
            Database.OpenConnection(Utility.MaterialConnectionString);
            DataTable dt = Database.GetDataTable(@"
select l.CompanyName, l.LedgerName, fi.Name AS InterCompanyName, icl.TransID AS transId
from ac_interCompanyLedger icl with(Nolock)
inner join LedgerMaster l with(Nolock) on icl.LedgerId=l.srno
inner join FactoryInfo fi with(Nolock) on fi.SrNo=icl.InterCompanyID
where l.CompanyName=@CompanyName and l.LedgerName=@LedgerName", new Database.Parameter("@CompanyName", CompanyName1), new Database.Parameter("@LedgerName", ledgerName));
            return dt;

            //' Need to check with SQL
        }

        #region Validation
        public static bool isLedgerInAccount(string LedgerName, string CompanyName)
        {
            if (Database.OpenConnection(Utility.MaterialConnectionString))
            {
                bool isLedger = (0 != (int)Database.GetExecuteScalarCommand_return(string.Format("SELECT COUNT(*) FROM LedgerMaster with(Nolock) WHERE LedgerName='{0}' AND CompanyName='{1}'", LedgerName, CompanyName)));
                Database.Closeconnection();
                return isLedger;
            }
            return false;
        }

        public static bool isBillwise(string LedgerName, string CompanyName)
        {
            if (Database.OpenConnection(Utility.MaterialConnectionString))
            {
                return (0 != (int)Database.GetExecuteNonQueryCommand_retrun(string.Format("SELECT COUNT(*) FROM LedgerMaster with(Nolock) WHERE LedgerName='{0}' AND CompanyName='{1}' AND isBillwise='yes'", LedgerName, CompanyName)));
            }
            return false;
        }

        public static bool isCostCenter(string LedgerName, string CompanyName)
        {
            if (Database.OpenConnection(Utility.MaterialConnectionString))
            {
                return (0 != (int)Database.GetExecuteNonQueryCommand_retrun(string.Format("SELECT COUNT(*) FROM LedgerMaster with(Nolock) WHERE LedgerName='{0}' AND CompanyName='{1}' and IsCostCenter='yes'", LedgerName, CompanyName)));
            }
            return false;
        }

        public static bool isInventory(string LedgerName, string CompanyName)
        {
            if (Database.OpenConnection(Utility.MaterialConnectionString))
            {
                return (0 != (int)Database.GetExecuteNonQueryCommand_retrun(string.Format("SELECT COUNT(*) FROM LedgerMaster with(Nolock) WHERE LedgerName='{0}' AND CompanyName='{1}' AND IsInventory='yes'", LedgerName, CompanyName)));
            }
            return false;
        }

        public static void AcceptOnlyNumeric(System.Windows.Forms.Control control)
        {
            if (control is TextBox)
            {
                (control as TextBox).TextChanged += (s, e) =>
                {
                    TextBox tb = s as TextBox;
                    Regex r = new Regex(@"[^0-9\.\-]", RegexOptions.IgnoreCase);
                    if (r.IsMatch(tb.Text))
                    {
                        tb.Text = r.Replace(tb.Text, "");
                    }
                };
            }
        }

        /// <summary>
        /// Validation for accept only numeric value in grid
        /// </summary>
        /// <param name="dgv">Which DataGridView need to validation</param>
        /// <param name="AllowBlank">Allow Blank Value</param>
        /// <param name="ColumnIndex">Columns ids</param>
        public static void AcceptOnlyNumericGrid(DataGridView dgv, bool AllowBlank, params int[] ColumnIndex)
        {
            List<int> ColumnIndexs = new List<int>();
            ColumnIndexs.AddRange(ColumnIndex);

            dgv.CellValidating += (s, e) =>
            {
                DataGridView dgvv = s as DataGridView;
                if (e.ColumnIndex == -1 || e.RowIndex == -1)
                    return;

                if (!ColumnIndexs.Exists(x => x == e.ColumnIndex))
                    return;

                if (dgvv.AllowUserToAddRows && e.RowIndex == dgvv.Rows.Count - 1)
                    return;

                if (e.FormattedValue.ToString().Trim().Length == 0 && AllowBlank)
                    return;

                double a = 0;

                if (!double.TryParse(e.FormattedValue.ToString(), out a))
                {
                    MessageBox.Show("Invalid value", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    e.Cancel = true;
                }
            };
        }

        /// <summary>
        /// Validation for accept only numeric value in grid
        /// </summary>
        /// <param name="dgv">Which DataGridView need to validation</param>
        /// <param name="ColumnIndex">Columns ids</param>
        public static void AcceptOnlyNumericGrid(DataGridView dgv, params int[] ColumnIndex)
        {
            AcceptOnlyNumericGrid(dgv, false, ColumnIndex);
        }

        /// <summary>
        /// Validate PAN No is valid
        /// </summary>
        /// <param name="PANNo">PANNo</param>
        /// <returns>True on correct</returns>
        public static bool ValidatePANNo(string PANNo)
        {
            Regex r = new Regex("[a-zA-Z]{5}[0-9]{4}[a-zA-Z]");
            return r.IsMatch(PANNo);
        }

        #endregion

        #region GetData

        /// <summary>
        /// Get Item List
        /// </summary>
        /// <param name="CompanyName">Company Name</param>
        /// <returns></returns>
        public static DataTable GetItemGroupList(string CompanyName)
        {
            // Get From cache
            object obj = Cache.GetCache(string.Format("acItemGroup-{0}", CompanyName));
            if (obj != null)
                return ((DataTable)obj).Copy();

            DataTable dataTable = new DataTable("acItemGroup");
            if (Database.OpenConnection(Utility.MaterialConnectionString))
            {
                Database.GetExecuteReaderCommand(string.Format("SELECT distinct subgroupname AS GroupName, subgroupname AS GroupId FROM SubGroupPrimaryItemMaster with(Nolock) WHERE CompanyName='{0}' and isnull(IsClosed,0)=0 ORDER BY subgroupname", CompanyName));
                //Database.GetExecuteReaderCommand("SELECT distinct GroupName AS GroupName, GroupName AS GroupId FROM ItemGroup with(Nolock)  ORDER BY GroupName");
                dataTable.Load(Database.myreader);
            }
            if (dataTable.Rows.Count != 0) Cache.AddCache(dataTable, 10, string.Format("acItemGroup-{0}", CompanyName));
            return dataTable.Copy();
        }

        /// <summary>
        /// Get Primary Item Group List
        /// </summary>
        /// <param name="CompanyName">Company Name</param>
        /// <returns></returns>
        public static DataTable GetItemPrimaryGroupList(string CompanyName)
        {
            // Get From cache
            object obj = Cache.GetCache(string.Format("acItemPrimaryGroup-{0}", CompanyName));
            if (obj != null)
                return ((DataTable)obj).Copy();

            DataTable dataTable = new DataTable("acItemPrimaryGroup");
            if (Database.OpenConnection(Utility.MaterialConnectionString))
            {
                Database.GetExecuteReaderCommand(string.Format("SELECT DISTINCT groupname AS GroupId, groupname AS GroupName FROM subgroupprimaryitemmaster with(Nolock) WHERE CompanyName='{0}' and isnull(IsClosed,0)=0  ORDER BY groupname", CompanyName));
                //Database.GetExecuteReaderCommand("SELECT DISTINCT DepttName AS GroupId, DepttName AS GroupName FROM ItemDeptt with(Nolock)   ORDER BY groupname");
                dataTable.Load(Database.myreader);
            }

            if (dataTable.Rows.Count != 0) Cache.AddCache(dataTable, 10, string.Format("acItemPrimaryGroup-{0}", CompanyName));

            return dataTable.Copy();
        }

        /// <summary>
        /// Get List of Companies
        /// </summary>
        /// <returns>DataTable</returns>
        public static DataTable GetCompanyList()
        {
            return GetCompanyList(false);
        }

        /// <summary>
        /// Get List of Companies
        /// </summary>
        /// <param name="IncludeBlank">Include * as blank for use all</param>
        /// <returns>DataTable</returns>
        public static DataTable GetCompanyList(bool IncludeBlank)
        {
            // Get From cache
            object obj = Cache.GetCache("CompanyList" + (IncludeBlank ? "1" : "0"));
            if (obj != null)
                return ((DataTable)obj).Copy();

            // Get From Database
            string stsql = "";
            Boolean ispurId = false;
            if (Database.OpenConnection(Utility.MaterialConnectionString))
            {
                stsql = "select count(*) from Loginentry.dbo.LoginRights with(Nolock) where   right(Password,1)='p' and name='" + FrmMain.UserName + "' and EmpCode in ( select empcode from Loginentry.dbo.empinfo where isnull(IsHOEmp,0)=1)";
                int intrec = Convert.ToInt32(Database.GetExecuteNonQueryCommand_retrun(stsql));
                if (intrec > 0)
                    ispurId = true;

            }

            if (FrmMain.UserName.ToLower() == "exauditor".ToLower())
            {
                stsql = "SELECT SrNo AS CompanyID, Name AS CompanyName FROM FactoryInfo with(Nolock) inner join [Loginentry].[dbo].[LoginRightAccount] L with(Nolock) on L.CompanyName=FactoryInfo.name  where L.LoginID='" + FrmMain.UserName + "' order by OrderPosition asc";
            }
            else
            {
                if (FrmMain.IsAdmin || FrmMain.isaccounthod || ispurId)
                    stsql = "SELECT SrNo AS CompanyID, Name AS CompanyName FROM FactoryInfo with(Nolock) order by OrderPosition asc";
                else
                    stsql = "SELECT SrNo AS CompanyID, Name AS CompanyName FROM FactoryInfo with(Nolock) where (name in ( select CompanyName from Loginentry..LoginRightAccount L with(Nolock) where L.LoginID='" + FrmMain.UserName + "') OR NAME IN ( select CompName from Despatch..DefaultValue with(Nolock) WHERE LOGINNAME='" + FrmMain.UserName + "')) order by OrderPosition asc";
            }
            DataTable dataTable = new DataTable("Company");
            if (Database.OpenConnection(Utility.MaterialConnectionString))
            {
                Database.GetExecuteReaderCommand(string.Format(stsql));
                dataTable.Load(Database.myreader);
            }

            if (IncludeBlank)
            {
                DataRow dr = dataTable.NewRow();
                dr["CompanyId"] = 0;
                dr["CompanyName"] = "* (All)";
                dataTable.Rows.InsertAt(dr, 0);
            }

            if (dataTable.Rows.Count - (IncludeBlank ? 1 : 0) != 0) Cache.AddCache(dataTable, -1, "CompanyList" + (IncludeBlank ? "1" : "0"));
            return dataTable.Copy();
        }

        /// <summary>
        /// Get Company Code
        /// </summary>
        /// <param name="CompanyName">Company Name</param>
        /// <returns></returns>
        public static string GetCompanyCode(string CompanyName)
        {
            DataTable dt;
            // Get From cache
            object obj = Cache.GetCache("CompanyCodeList");

            if (obj != null)
            {
                dt = (DataTable)obj;
            }
            else
            {
                // Get From Database
                Database.OpenConnection(Utility.MaterialConnectionString);
                dt = Database.GetDataTable(string.Format("SELECT IndentCode AS CompanyCode, Name AS CompanyName FROM FactoryInfo with(Nolock) order by OrderPosition asc"));
                if (dt.Rows.Count > 0) Cache.AddCache(dt, -1, "CompanyCodeList");
            }

            using (DataView dv = new DataView(dt))
            {
                dv.RowFilter = "CompanyName='" + CompanyName + "'";
                using (DataTable dt2 = dv.ToTable())
                {
                    if (dt2.Rows.Count == 0)
                        return "";

                    return Convert.ToString(dt2.Rows[0]["CompanyCode"]);
                }
            }
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
            string stsql = "SELECT DISTINCT GroupName AS CompanyID, GroupName AS CompanyName FROM FactoryInfo with(Nolock)";
            if (FrmMain.UserName.ToLower() == "exauditor".ToLower())
            {
                stsql = "SELECT DISTINCT GroupName AS CompanyID, GroupName AS CompanyName FROM FactoryInfo with(Nolock) inner join [Loginentry].[dbo].[LoginRightAccount] L with(Nolock) on L.CompanyName=FactoryInfo.name  where L.LoginID='" + FrmMain.UserName + "' order by OrderPosition asc";
            }
            else
            {
                stsql = "SELECT DISTINCT GroupName AS CompanyID, GroupName AS CompanyName FROM FactoryInfo with(Nolock) WHERE  (name in ( select CompanyName from Loginentry..LoginRightAccount L with(Nolock) where L.LoginID='" + FrmMain.UserName + "') OR NAME IN ( select CompName from Despatch..DefaultValue with(Nolock) WHERE LOGINNAME='" + FrmMain.UserName + "'))";
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

        /// <summary>
        /// Get List of Ledgers
        /// </summary>
        /// <param name="CompanyName">Company Name of Ledgers</param>
        /// <returns>DataTable</returns>
        public static DataTable GetLedgerList(string CompanyName)
        {
            // Get From cache
            object obj = Cache.GetCache("Ledgers-" + CompanyName);
            if (obj != null)
                return ((DataTable)obj).Copy();

            // Get From Database
            DataTable dataTable = new DataTable("Ledgers");
            if (Database.OpenConnection(Utility.MaterialConnectionString))
            {
                Database.GetExecuteReaderCommand(string.Format("SELECT LedgerName AS LedgerName, srno AS LedgerId FROM LedgerMaster with(Nolock) WHERE CompanyName='{0}'  order by Todate ,LedgerName", CompanyName));
                dataTable.Load(Database.myreader);
            }

            if (dataTable.Rows.Count != 0) Cache.AddCache(dataTable, 10, "Ledgers-" + CompanyName);
            return dataTable.Copy();
        }

        /// <summary>
        /// Get List of Ledgers with FromDate
        /// </summary>
        /// <param name="CompanyName">Company Name of Ledgers</param>
        /// <returns>DataTable</returns>
        public static DataTable GetLedgerListwithCondition(string CompanyName)
        {
            // Get From Database
            Tools.Cache.ClearAllCache();
            DataTable dataTable = new DataTable("Ledgers");
            if (Database.OpenConnection(Utility.MaterialConnectionString))
            {
                Database.GetExecuteReaderCommand(string.Format("SELECT case when IsAccClose='yes' then LedgerName + ' - [Closed]' when IsAccClose='no' and Todate is not null then LedgerName + ' - [Closed up to-' + convert(varchar(10),Todate,103) + ']' else LedgerName end AS LedgerName, Srno AS LedgerId FROM LedgerMaster with(Nolock) WHERE CompanyName='{0}' order by Todate ,LedgerName", CompanyName));
                dataTable.Load(Database.myreader);
            }

            return dataTable.Copy();
        }

        /// <summary>
        /// Get List of Ledgers Categorywise
        /// </summary>
        /// <param name="SelectedCompany">Company Name (GC) of Ledgers</param>
        /// <para>A- All</para>
        /// <para>G- Group Company</para>
        /// <para>C- Company</para>
        /// <returns></returns>
        public static DataTable GetLedgerListGC(string SelectedCompany, string category)
        {
            // Get From cache

            // Get From Database
            DataTable dataTable = new DataTable("LedgersCategory");

            //if (SelectedCompany.Length < 2)
            //    return dataTable;

            if (Database.OpenConnection(Utility.MaterialConnectionString))
            {
                string v = SelectedCompany;

                string Query = string.Format("SELECT LedgerName AS LedgerName, l.srno AS LedgerId FROM LedgerMaster l with(Nolock) INNER JOIN FactoryInfo fi with(Nolock) ON l.CompanyName=fi.Name WHERE fi.SrNo={0} and l.category='{1}'", 0,
                    category);

                if (v.Length > 0)
                {
                    switch (v[0])
                    {
                        case 'A':
                            Query = "SELECT DISTINCT LedgerName AS LedgerName, 0 AS LedgerId FROM LedgerMaster l with(Nolock) INNER JOIN FactoryInfo fi with(Nolock) ON l.CompanyName=fi.Name where l.category='" + category + "'";
                            break;
                        case 'G':
                            Query = string.Format("SELECT DISTINCT LedgerName AS LedgerName, 0 AS LedgerId FROM LedgerMaster l with(Nolock) INNER JOIN FactoryInfo fi  with(Nolock) ON l.CompanyName=fi.Name WHERE fi.GroupName='{0}' and l.category='" + category + "'", v.Substring(2));
                            break;
                        case 'C':
                            Query = string.Format("SELECT LedgerName AS LedgerName, l.srno AS LedgerId FROM LedgerMaster l with(Nolock) INNER JOIN FactoryInfo fi with(Nolock) ON l.CompanyName=fi.Name WHERE fi.SrNo={0} and l.category='" + category + "'", AccountCommonFunction.SafeConvertToInt(v.Substring(2)));
                            break;
                        default:
                            Query = string.Format("SELECT LedgerName AS LedgerName, l.srno AS LedgerId FROM LedgerMaster l with(Nolock) INNER JOIN FactoryInfo fi with(Nolock) ON l.CompanyName=fi.Name WHERE fi.SrNo={0} and l.category='" + category + "'", AccountCommonFunction.SafeConvertToInt(v.Substring(2)));
                            break;
                    }
                }

                Query += " ORDER BY LedgerName";

                Database.GetExecuteReaderCommand(Query);
                dataTable.Load(Database.myreader);
            }

            if (dataTable.Rows.Count != 0) Cache.AddCache(dataTable, 10, "LedgersCategory-" + SelectedCompany);
            return dataTable.Copy();
        }

        /// <summary>
        /// Get List of Ledgers
        /// </summary>
        /// <param name="SelectedCompany">Company Name (GC) of Ledgers</param>
        /// <para>A- All</para>
        /// <para>G- Group Company</para>
        /// <para>C- Company</para>
        /// <returns></returns>
        public static DataTable GetLedgerListGC(string SelectedCompany, bool blnMSME = false)
        {
            // Get From cache
            object obj = Cache.GetCache("Ledgers-" + SelectedCompany);
            if (obj != null)
                return ((DataTable)obj).Copy();

            // Get From Database
            DataTable dataTable = new DataTable("Ledgers");

            //if (SelectedCompany.Length < 2)
            //    return dataTable;

            if (Database.OpenConnection(Utility.MaterialConnectionString))
            {
                string v = SelectedCompany;

                string Query = string.Format("SELECT LedgerName AS LedgerName, l.srno AS LedgerId FROM LedgerMaster l with(Nolock) INNER JOIN FactoryInfo fi with(Nolock) ON l.CompanyName=fi.Name  WHERE fi.SrNo={0}", 0);

                if (v.Length > 0)
                {
                    switch (v[0])
                    {
                        case 'A':
                            Query = "SELECT DISTINCT LedgerName AS LedgerName, 0 AS LedgerId FROM LedgerMaster l with(Nolock) INNER JOIN FactoryInfo fi with(Nolock) ON l.CompanyName=fi.Name  ";
                            break;
                        case 'G':
                            Query = string.Format("SELECT DISTINCT LedgerName AS LedgerName, 0 AS LedgerId FROM LedgerMaster l with(Nolock) INNER JOIN FactoryInfo fi  with(Nolock) ON l.CompanyName=fi.Name   WHERE fi.GroupName='{0}'", v.Substring(2));
                            break;
                        case 'C':
                            Query = string.Format("SELECT LedgerName AS LedgerName, l.srno AS LedgerId FROM LedgerMaster l with(Nolock) INNER JOIN FactoryInfo fi with(Nolock) ON l.CompanyName=fi.Name   WHERE fi.SrNo={0}", AccountCommonFunction.SafeConvertToInt(v.Substring(2)));
                            break;
                        default:
                            Query = string.Format("SELECT LedgerName AS LedgerName, l.srno AS LedgerId FROM LedgerMaster l with(Nolock) INNER JOIN FactoryInfo fi with(Nolock) ON l.CompanyName=fi.Name  WHERE fi.SrNo={0}", AccountCommonFunction.SafeConvertToInt(v.Substring(2)));
                            break;
                    }
                }
                if (blnMSME)
                    Query += " and isnull(MSMENumber,'')<>'' and len(MSMENumber)>10 ";
               // Query += " ORDER BY LedgerName";
               // added by manish on 20th March 2023 for DERP
                Query += " and ledgername not in(select ledgername from AuditLedgers) ORDER BY LedgerName";
               //  end added

                Database.GetExecuteReaderCommand(Query);
                dataTable.Load(Database.myreader);
            }

            if (dataTable.Rows.Count != 0) Cache.AddCache(dataTable, 10, "Ledgers-" + SelectedCompany);
            return dataTable.Copy();
        }

        /// <summary>
        /// Get list of Groups for ledgers in Accounts
        /// </summary>
        /// <param name="CompanyName">Company Name</param>
        /// <returns>DataTable</returns>
        public static DataTable GetLedgerGroupList(string CompanyName)
        {
            DataTable dataTable = new DataTable("LedgerGroup");
            if (Database.OpenConnection(Utility.MaterialConnectionString))
            {
                Database.GetExecuteReaderCommand(string.Format(@"SELECT GroupName, GroupId FROM (
SELECT ExpenseGroupHead AS GroupName, ExpenseGroupHead AS GroupId, Companyname FROM CashVoucherExpenseGroupHead with(Nolock)
UNION ALL SELECT ExpenseHead AS GroupName, ExpenseHead AS GroupId, CompanyName FROM CashVoucherExpenseHead with(Nolock)
) AS g WHERE g.GroupName!='' AND g.Companyname='{0}' order by Groupname", CompanyName));
                dataTable.Load(Database.myreader);
            }
            return dataTable;
        }

        public static DataTable GetLedgerGroupList(string CompanyName, string groupname)
        {
            DataTable dataTable = new DataTable("LedgerGroup");
            if (Database.OpenConnection(Utility.MaterialConnectionString))
            {
                Database.GetExecuteReaderCommand(string.Format(@"SELECT GroupName, GroupId FROM (
SELECT ExpenseGroupHead AS GroupName, ExpenseGroupHead AS GroupId, Companyname FROM CashVoucherExpenseGroupHead with(Nolock)
UNION ALL SELECT ExpenseHead AS GroupName, ExpenseHead AS GroupId, CompanyName FROM CashVoucherExpenseHead with(Nolock)
) AS g WHERE g.GroupName!='' AND g.Companyname='{0}'and g.GroupName='{1}'", CompanyName, groupname));
                dataTable.Load(Database.myreader);
            }
            return dataTable;
        }

        /// <summary>
        /// Get Group List of Scchedule 6 for ledgers in Account
        /// </summary>
        /// <param name="CompanyName"></param>
        /// <returns></returns>
        public static DataTable GetLedgerGroupS6List(string CompanyName)
        {
            DataTable dataTable = new DataTable("LedgerGroupS6");
            if (Database.OpenConnection(Utility.MaterialConnectionString))
            {
                Database.GetExecuteReaderCommand(string.Format(@"SELECT GroupName, GroupId FROM (
SELECT ExpenseGroupHead AS GroupName, ExpenseGroupHead AS GroupId, Companyname FROM CashVoucherExpenseGroupHeadSchedule6 with(Nolock)
UNION ALL SELECT ExpenseHead AS GroupName, ExpenseHead AS GroupId, CompanyName FROM CashVoucherExpenseHeadSchedule6 with(Nolock)
) AS g WHERE g.GroupName!='' AND g.Companyname='{0}'", CompanyName));
                dataTable.Load(Database.myreader);
            }
            return dataTable;
        }

        public static DataTable GetLedgerGroupS6List(string CompanyName, string groupname)
        {
            DataTable dataTable = new DataTable("LedgerGroupS6");
            if (Database.OpenConnection(Utility.MaterialConnectionString))
            {
                Database.GetExecuteReaderCommand(string.Format(@"SELECT GroupName, GroupId FROM (
SELECT ExpenseGroupHead AS GroupName, ExpenseGroupHead AS GroupId, Companyname FROM CashVoucherExpenseGroupHeadSchedule6 with(Nolock)
UNION ALL SELECT ExpenseHead AS GroupName, ExpenseHead AS GroupId, CompanyName FROM CashVoucherExpenseHeadSchedule6 with(Nolock)
) AS g WHERE g.GroupName!='' AND g.Companyname='{0}' AND g.GroupName='{1}'", CompanyName, groupname));
                dataTable.Load(Database.myreader);
            }
            return dataTable;
        }

        /// <summary>
        /// Get Vat Class List
        /// </summary>
        /// <returns></returns>
        public static DataTable GetVatClassList()
        {
            DataTable dataTable = new DataTable("VatClass");
            if (Database.OpenConnection(Utility.MaterialConnectionString))
            {
                Database.GetExecuteReaderCommand(string.Format("SELECT Name AS VatClassID, Name AS VatClassName FROM vatclass with(Nolock) ORDER BY name"));
                dataTable.Load(Database.myreader);
            }
            DataRow dr = dataTable.NewRow();
            dr["VatClassId"] = "";
            dr["VatClassName"] = "";
            dataTable.Rows.InsertAt(dr, 0);
            return dataTable;
        }

        public static DataTable GetTDSNatureofPaymentList()
        {
            DataTable dataTable = new DataTable("TDSNatureofPayment");
            if (Database.OpenConnection(Utility.MaterialConnectionString))
            {
                Database.GetExecuteReaderCommand(string.Format("select NatureofPayment from TDSNatureofPayment with(Nolock) order by NatureofPayment"));
                dataTable.Load(Database.myreader);
            }
            DataRow dr = dataTable.NewRow();
            dr["NatureofPayment"] = "";
            dataTable.Rows.InsertAt(dr, 0);
            return dataTable;
        }

        public static DataTable GetPaymentTermsMasterList()
        {
            DataTable dataTable = new DataTable("PaymentTermsMaster");
            if (Database.OpenConnection(Utility.MaterialConnectionString))
            {
                Database.GetExecuteReaderCommand(string.Format("select distinct paymenttermname from paymenttermsmaster with(Nolock) where isdelflag=0 order by Paymenttermname"));
                dataTable.Load(Database.myreader);
            }
            DataRow dr = dataTable.NewRow();
            dr["paymenttermname"] = "";
            dataTable.Rows.InsertAt(dr, 0);
            return dataTable;
        }

        public static DataTable GetTDSDeducteeTypeList()
        {
            DataTable dataTable = new DataTable("TDSDeducteeType");
            if (Database.OpenConnection(Utility.MaterialConnectionString))
            {
                Database.GetExecuteReaderCommand(string.Format("select Distinct(Name) AS Name from TDSDeducteetype with(Nolock) order by Name"));
                dataTable.Load(Database.myreader);
            }
            DataRow dr = dataTable.NewRow();
            dr["Name"] = "";
            dataTable.Rows.InsertAt(dr, 0);
            return dataTable;
        }

        public static DataTable GetTDSSectionCodeList()
        {
            DataTable dataTable = new DataTable("TDSPerrcentageMaster");
            if (Database.OpenConnection(Utility.MaterialConnectionString))
            {
                Database.GetExecuteReaderCommand(string.Format("select distinct sectioncode  from TDSpercentageMaster with(Nolock)  order by sectioncode"));
                dataTable.Load(Database.myreader);
            }
            DataRow dr = dataTable.NewRow();
            dr["sectioncode"] = "";
            dataTable.Rows.InsertAt(dr, 0);
            return dataTable;
        }

        public static DataTable GetDepriciationBlockList()
        {
            DataTable dataTable = new DataTable("DepriciationITMaster");
            if (Database.OpenConnection(Utility.MaterialConnectionString))
            {
                Database.GetExecuteReaderCommand(string.Format("select distinct Block AS BlockName from DepriciationITMaster with(Nolock) order by Block"));
                dataTable.Load(Database.myreader);
            }
            DataRow dr = dataTable.NewRow();
            dr["BlockName"] = "";
            dataTable.Rows.InsertAt(dr, 0);
            return dataTable;
        }

        public static DataTable GetCostCenterList()
        {
            DataTable dataTable = new DataTable("CostCenter");
            if (Database.OpenConnection(Utility.MaterialConnectionString))
            {
                Database.GetExecuteReaderCommand(string.Format("select cost_center_id AS CostCenterId, cost_center_name AS CostCenterName from CostCenterMaster with(Nolock)"));
                dataTable.Load(Database.myreader);
            }
            //DataRow dr = dataTable.NewRow();
            //dr.ItemArray[0] = "";
            //dataTable.Rows.InsertAt(dr, 0);
            return dataTable;
        }

        /// <summary>
        /// Year list from YearMaster
        /// </summary>
        /// <returns>DataTable</returns>
        public static DataTable GetYearList()
        {
            DataTable dataTable = new DataTable("YearMaster");
            if (Database.OpenConnection(Utility.MaterialConnectionString))
            {
                Database.GetExecuteReaderCommand(string.Format("SELECT Yr AS Year FROM yearMaster with(Nolock) ORDER BY srno DESC"));
                dataTable.Load(Database.myreader);
            }
            return dataTable;
        }

        /// <summary>
        /// Get Tax Ledger List
        /// </summary>
        /// <param name="CompanyName">Which company we want list of tax ledger</param>
        /// <returns></returns>
        public static DataTable GetTaxLedgers(string CompanyName)
        {
            // Get From cache
            object obj = Cache.GetCache("TaxLedgers-" + CompanyName);
            if (obj != null)
                return ((DataTable)obj).Copy();

            // Get From Database
            DataTable dataTable = new DataTable("TaxLedgers");
            if (Database.OpenConnection(Utility.MaterialConnectionString))
            {
                Database.GetExecuteReaderCommand(string.Format("select LedgerName AS TaxLedgerName, srno AS TaxLedgerId from LedgerMaster with(Nolock) WHERE ( category in ('Expense','Income', 'TAX')  OR (ISMRN='yes' or ISInvoice='yes')) and companyname = '{0}' ORDER BY TaxLedgerName", CompanyName));
                dataTable.Load(Database.myreader);
            }

            DataRow dr = dataTable.NewRow();
            dr[0] = "";
            dr[1] = 0;
            dataTable.Rows.InsertAt(dr, 0);

            if (dataTable.Rows.Count != 0) Cache.AddCache(dataTable, 10, "TaxLedgers-" + CompanyName);
            return dataTable.Copy();
        }

        public static DataTable GetBankList(int CompanyId)
        {
            // Get From cache
            object obj = Cache.GetCache("LedgerBanks-" + CompanyId);
            if (obj != null)
                return ((DataTable)obj).Copy();

            // Get From Database
            DataTable dataTable = new DataTable("LedgerBanks");
            if (Database.OpenConnection(Utility.MaterialConnectionString))
            {
                Database.GetExecuteReaderCommand(string.Format(@"
select l.LedgerName AS LedgerName, l.srno AS LedgerId
from ledgermaster l with(Nolock)
inner join FactoryInfo f with(Nolock) on l.CompanyName=f.Name
where Category = 'Bank Accounts'  and f.srno = '{0}'
order by LedgerName
", CompanyId));
                dataTable.Load(Database.myreader);
            }

            if (dataTable.Rows.Count != 0) Cache.AddCache(dataTable, 10, "LedgerBanks-" + CompanyId);
            return dataTable.Copy();
        }

        /// <summary>
        /// Get TDS Sections
        /// </summary>
        /// <param name="CompanyId">Company of TDS Sections</param>
        /// <param name="ShowFullSectionCode">Show Full Section code like 194C or show in short 94C</param>
        /// <param name="ReturnType">Filter Section by Return Type [26Q, 24Q, 27EQ, 27Q]</param>
        /// <returns>DataTable</returns>
        public static DataTable GetTDSSections(int CompanyId, string ReturnType, bool ShowFullSectionCode = false, string condition = "")
        {
            // Get From cache
            object obj = Cache.GetCache(string.Format("TDSSections({0})-{1}-{2}", ShowFullSectionCode ? 1 : 0, CompanyId, ReturnType));
            if (obj != null)
                return ((DataTable)obj).Copy();

            // Get From Database
            DataTable dataTable = new DataTable("TDSSections");
            if (Database.OpenConnection(Utility.MaterialConnectionString))
            {
                string SelectColumnName = ShowFullSectionCode ? "SectionCodeFull" : "SectionCode";
                Database.GetExecuteReaderCommand(string.Format("SELECT SectionId, {0} AS SectionCode FROM ac_tds_sections  with(Nolock) WHERE CompanyId='{1}' AND ReturnType='{2}'" +
                     condition, SelectColumnName, CompanyId, ReturnType));
                dataTable.Load(Database.myreader);
            }

            if (dataTable.Rows.Count != 0) Cache.AddCache(dataTable, -1, string.Format("TDSSections({0})-{1}-{2}", ShowFullSectionCode ? 1 : 0, CompanyId, ReturnType));
            return dataTable.Copy();
        }

        /// <summary>
        /// Get TDS Return Type [26Q, 24Q, 27EQ, 27Q]
        /// </summary>
        /// <param name="CompanyId">Which company to get list</param>
        /// <returns></returns>
        public static DataTable GetTDSReturnType(int CompanyId)
        {
            // Get From cache
            object obj = Cache.GetCache(string.Format("TDSReturnType-{0}", CompanyId));
            if (obj != null)
                return ((DataTable)obj).Copy();

            // Get From Database
            DataTable dataTable = new DataTable("TDSReturnType");
            if (Database.OpenConnection(Utility.MaterialConnectionString))
            {
                Database.GetExecuteReaderCommand(string.Format("SELECT DISTINCT ReturnType AS ReturnTypeId, ReturnType AS ReturnTypeName FROM ac_tds_sections with(Nolock) WHERE CompanyId='{0}'", CompanyId));
                dataTable.Load(Database.myreader);
            }

            if (dataTable.Rows.Count != 0) Cache.AddCache(dataTable, -1, string.Format("TDSReturnType-{0}", CompanyId));
            return dataTable.Copy();
        }

        /// <summary>
        /// Get TDS Return Type [26Q, 24Q, 27EQ, 27Q]
        /// </summary>
        /// <param name="GroupCompanyName">Which company to get list</param>
        /// <returns></returns>
        public static DataTable GetTDSReturnTypeByGroupCompany(string GroupCompanyName)
        {
            // Get From cache
            object obj = Cache.GetCache(string.Format("TDSReturnTypeGroup-{0}", GroupCompanyName));
            if (obj != null)
                return ((DataTable)obj).Copy();

            // Get From Database
            DataTable dataTable = new DataTable("TDSReturnTypeGroup");
            if (Database.OpenConnection(Utility.MaterialConnectionString))
            {
                Database.GetExecuteReaderCommand(string.Format(@"
SELECT DISTINCT ReturnType AS ReturnTypeId, ReturnType AS ReturnTypeName
FROM ac_tds_sections tdss with(Nolock)
INNER JOIN FactoryInfo f with(Nolock) on f.SrNo=tdss.CompanyId
WHERE f.Name='{0}'", GroupCompanyName));
                dataTable.Load(Database.myreader);
            }

            if (dataTable.Rows.Count != 0) Cache.AddCache(dataTable, -1, string.Format("TDSReturnTypeGroup-{0}", GroupCompanyName));
            return dataTable.Copy();
        }

        /// <summary>
        /// Get list of employee
        /// <para>This list is not releated with account. It was related with payroll.</para>
        /// <para>But wee need this list in saving data of TDS</para>
        /// </summary>
        /// <param name="CompanyName">Which company we want list of employee</param>
        /// <returns>Employee list</returns>
        public static DataTable GetEmployeeList(string CompanyName)
        {
            // Get From cache
            object obj = Cache.GetCache(string.Format("Employee-{0}", CompanyName));
            if (obj != null)
                return ((DataTable)obj).Copy();

            // Get From Database
            DataTable dataTable = new DataTable("Employee");
            if (Database.OpenConnection(Utility.MaterialConnectionString))
            {
                Database.GetExecuteReaderCommand(string.Format(@"select EmpCode As EmpId, EmpCode + ' - ' + Name AS EmpName from loginentry..empinfo with(Nolock) where CompanyName='{0}'", CompanyName));
                dataTable.Load(Database.myreader);
            }

            if (dataTable.Rows.Count != 0) Cache.AddCache(dataTable, -1, string.Format("Employee-{0}", CompanyName));
            return dataTable.Copy();
        }

        /// <summary>
        /// Get list of employee which have TDS deduction
        /// </summary>
        /// <param name="CompanyName">Which company we want list of employee</param>
        /// <returns>Employee list</returns>
        public static DataTable GetEmployeeListTDS(string CompanyName, string Year)
        {
            // Get From cache
            object obj = Cache.GetCache(string.Format("EmployeeTDS-{0}-{1}", CompanyName, Year));
            if (obj != null)
                return ((DataTable)obj).Copy();

            // Get From Database
            DataTable dataTable = new DataTable("EmployeeTDS");
            if (Database.OpenConnection(Utility.MaterialConnectionString))
            {
                Database.GetExecuteReaderCommand(string.Format(@"
select e.EmpCode As EmpId, e.EmpCode + ' - ' + e.Name AS EmpName
from loginentry..empinfo e with(Nolock)
inner join loginentry..FreezeSalary s with(Nolock) on e.empcode=s.empcode
inner join MaterialProcessing..yearMaster y with(Nolock) on convert(date, ltrim(str(s.yr))+'-'+ltrim(str(s.mnth))+'-01') between y.fromDate and y.ToDate
where e.CompanyName='{0}' and y.yr='{1}'
group by e.EmpCode, e.Name
having SUM(s.TDS)>0
", CompanyName, Year));
                dataTable.Load(Database.myreader);
            }

            if (dataTable.Rows.Count != 0) Cache.AddCache(dataTable, -1, string.Format("EmployeeTDS-{0}-{1}", CompanyName, Year));
            return dataTable.Copy();
        }

        /// <summary>
        /// Get Sales Ledger List
        /// </summary>
        /// <param name="CompanyName"></param>
        /// <returns></returns>
        public static DataTable GetSalesLedgers(string CompanyName)
        {
            // Get From cache
            object obj = Cache.GetCache("SalesLedgers-" + CompanyName);
            if (obj != null)
                return ((DataTable)obj).Copy();

            // Get From Database
            DataTable dataTable = new DataTable("SalesLedgers");
            if (Database.OpenConnection(Utility.MaterialConnectionString))
            {
                Database.GetExecuteReaderCommand(string.Format("select LedgerName AS LedgerName, srno AS LedgerId from LedgerMaster with(Nolock) WHERE isinvoice = 'yes' and companyname = '{0}' ORDER BY LedgerName", CompanyName));
                dataTable.Load(Database.myreader);
            }

            if (dataTable.Rows.Count != 0) Cache.AddCache(dataTable, 10, "SalesLedgers-" + CompanyName);
            return dataTable.Copy();
        }

        /// <summary>
        /// Get Item List from database
        /// </summary>
        /// <param name="CompanyName">Company Name</param>
        /// <param name="IncludeBlank">Include Blank</param>
        /// <returns>DataTable</returns>
        public static DataTable GetItemList(string CompanyName, bool IncludeBlank)
        {
            // Get From cache
            object obj = Cache.GetCache(string.Format("ItemList({0})-", IncludeBlank ? 1 : 0) + CompanyName);
            if (obj != null)
                return ((DataTable)obj).Copy();

            // Get From Database
            DataTable dataTable = new DataTable("ItemList");
            if (Database.OpenConnection(Utility.MaterialConnectionString))
            {
                Database.GetExecuteReaderCommand(string.Format("select ItemCode AS ItemCode, ItemName AS ItemName from Item with(Nolock) WHERE companyname = '{0}'   ORDER BY ItemName", CompanyName));
                dataTable.Load(Database.myreader);
            }

            if (IncludeBlank)
            {
                DataRow dr = dataTable.NewRow();
                dr[0] = "";
                dr[1] = "";

                dataTable.Rows.InsertAt(dr, 0);
            }

            if (dataTable.Rows.Count != 0) Cache.AddCache(dataTable, 60, string.Format("ItemList({0})-", IncludeBlank ? 1 : 0) + CompanyName);
            return dataTable.Copy();
        }

        public static DataTable GetRepresentiveList()
        {
            // Get From cache
            object obj = Cache.GetCache(string.Format("RepresentiveList"));
            if (obj != null)
                return ((DataTable)obj).Copy();

            // Get From Database
            DataTable dataTable = new DataTable("RepresentiveList");
            if (Database.OpenConnection(Utility.MaterialConnectionString))
            {
                //Database.GetExecuteReaderCommand(string.Format("select distinct RepresentiveName AS RepresentiveId, RepresentiveName AS RepresentiveName from LedgerMaster with(Nolock) ORDER BY RepresentiveName"));
                Database.GetExecuteReaderCommand(string.Format("select distinct RepresentativeName AS RepresentiveId, RepresentativeName AS RepresentiveName,RepresentativeType from RepresentativeMaster with(Nolock) ORDER BY RepresentativeName"));
                dataTable.Load(Database.myreader);
            }
            if (dataTable.Rows.Count != 0) Cache.AddCache(dataTable, -1, string.Format("RepresentiveList"));
            return dataTable.Copy();
        }

        public static string GetItemCodeFromItemName(string CompanyName, string ItemName)
        {
            DataView dv = new DataView(GetItemList(CompanyName, true));

            dv.RowFilter = string.Format("ItemName='{0}'", ItemName);

            DataTable dataTable = dv.ToTable();

            if (dataTable.Rows.Count == 0)
                return "";

            return Convert.ToString(dataTable.Rows[0]["ItemCode"]);
        }

        public static string GetItemNameFromItemCode(string CompanyName, string ItemCode)
        {
            DataView dv = new DataView(GetItemList(CompanyName, true));

            dv.RowFilter = string.Format("ItemCode='{0}'", ItemCode);

            DataTable dataTable = dv.ToTable();

            if (dataTable.Rows.Count == 0)
                return "";

            return Convert.ToString(dataTable.Rows[0]["ItemName"]);
        }

        public static DataTable GetVoucherTypeList(string GroupName = "")
        {
            // Get From cache
            object obj = Cache.GetCache(string.Format("acVoucherType-{0}", GroupName));
            if (obj != null)
                return ((DataTable)obj).Copy();

            // Get From Database
            DataTable dataTable = new DataTable("VoucherType");
            if (Database.OpenConnection(Utility.MaterialConnectionString))
            {
                string Query = "select srno AS VoucherTypeId, VoucherTypeName AS VoucherTypeName from VoucherType with(Nolock)";
                if (GroupName.Length > 0)
                    Query += string.Format(" GroupName='{0}'", GroupName);
                Query += " ORDER BY VoucherTypeName";
                Database.GetExecuteReaderCommand(Query);
                dataTable.Load(Database.myreader);
            }
            if (dataTable.Rows.Count != 0) Cache.AddCache(dataTable, -1, string.Format("acVoucherType-{0}", GroupName));
            return dataTable.Copy();
        }

        public static DataTable GetCurrencyList(bool IncludeAll = false)
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("CurrencyName", typeof(string));
            dt.Columns.Add("CurrencyName2", typeof(string));

            dt.Rows.Add("Rs.", "INR");
            dt.Rows.Add("$", "USD");
            dt.Rows.Add("GBP", "GBP");
            dt.Rows.Add("€", "EURO");
            dt.Rows.Add("CHF", "CHF");

            return dt;
        }

        #endregion

        /// <summary>
        /// Get Unique Number
        /// </summary>
        /// <param name="Positive">Get Only Positive Unique Number (Defult True)</param>
        /// <returns>Unique Number in INT</returns>
        public static int GetUniqNumber(bool Positive = true)
        {
            System.Threading.Thread.Sleep(1);
            DateTime d = DateTime.Now;
            int n = 0;
            n = d.Millisecond + d.Second * 1000 + d.Minute * 1000 * 60;
            n += _random.Next(0, 100) + n * 100;
            if (!Positive) n = -n;
            return n;
        }

        /// <summary>
        /// Convert Double Value to Account DR|CR Format
        /// </summary>
        /// <param name="obj">Amount</param>
        /// <returns></returns>
        public static string ConvertToAccountCurrencyFormat(object obj)
        {
            double d = SafeConvertToDouble(obj);

            System.Globalization.NumberFormatInfo CustomNumberFormat = (System.Globalization.NumberFormatInfo)System.Globalization.NumberFormatInfo.CurrentInfo.Clone();
            CustomNumberFormat.NumberGroupSizes = new int[] { 3, 2 };

            return d.ToString("#,##0.00 Dr;#,##0.00 Cr", CustomNumberFormat);
        }

        /// <summary>
        /// Convert Double Value to Amount Format
        /// </summary>
        /// <param name="obj">Amount</param>
        /// <returns></returns>
        public static string ConvertToAmountFormat(object obj)
        {
            double d = SafeConvertToDouble(obj);

            System.Globalization.NumberFormatInfo CustomNumberFormat = (System.Globalization.NumberFormatInfo)System.Globalization.NumberFormatInfo.CurrentInfo.Clone();
            CustomNumberFormat.NumberGroupSizes = new int[] { 3, 2 };

            return d.ToString("#,##0.00", CustomNumberFormat);
        }

        /// <summary>
        /// Convert Double to Quantity Format
        /// </summary>
        /// <param name="obj">Quantity</param>
        /// <returns></returns>
        public static string ConvertToQtyFormat(object obj)
        {
            double d = SafeConvertToDouble(obj);

            System.Globalization.NumberFormatInfo CustomNumberFormat = (System.Globalization.NumberFormatInfo)System.Globalization.NumberFormatInfo.CurrentInfo.Clone();
            CustomNumberFormat.NumberGroupSizes = new int[] { 3, 2 };

            return d.ToString("#,##0.000", CustomNumberFormat);
        }

        /// <summary>
        /// Get Branch Name by an Account Number
        /// </summary>
        /// <param name="BankName">Bank Name</param>
        /// <param name="AccountNo">Account</param>
        /// <returns>Branch Name</returns>
        public static string GetBankBranchName(string BankName, string AccountNo)
        {
            return Convert.ToString(Database.GetExecuteScalarCommand_return(string.Format("SELECT TOP 1 BankBranch FROM BankMaster with(Nolock) WHERE BankName='{0}' AND AccountNo='{1}'", BankName, AccountNo)));
        }

        /// <summary>
        /// Get Bank Reco Date
        /// </summary>
        /// <param name="VoucherNo">Voucher No</param>
        /// <param name="CompanyName">Company Name</param>
        /// <param name="Year">Financial Year</param>
        /// <param name="VoucherType">Voucher Type</param>
        /// <returns>Bank Reco Date</returns>
        public static DateTime GetBankRecoDate(int VoucherNo, string CompanyName, string Year, string VoucherType)
        {
            if (!Database.OpenConnection(Utility.MaterialConnectionString))
                throw new Exception("Connection error");

            object oBankRecoDate = Database.GetExecuteScalarCommand_return(string.Format("select BankRecoDate from BankRecoEntry with(Nolock) where paymentno = {0} and companyname='{1}' and yr = '{2}' and vouchertype='{3}'", VoucherNo, CompanyName, Year, VoucherType));

            // value type must be datetime
            if (oBankRecoDate is DateTime)
                return (DateTime)oBankRecoDate;

            // return null date if value  is null or dbnull
            return Utility.nullDate;
        }

        /// <summary>
        /// Bind a company combobox
        /// </summary>
        /// <param name="combo">ComboBox</param>
        public static void BindComboCompany(ComboBox combo)
        {
            BindComboCompany(combo, false);
        }

        /// <summary>
        /// Bind a company comobox
        /// </summary>
        /// <param name="combo">ComboBox</param>
        /// <param name="IncludeBlank">Include Blank</param>
        public static void BindComboCompany(ComboBox combo, bool IncludeBlank)
        {
            combo.DataSource = AccountCommonFunction.GetCompanyList(IncludeBlank);
            combo.DisplayMember = AccountCommonVariable.FieldName.CompanyName;
            combo.ValueMember = AccountCommonVariable.FieldName.CompanyId;

            combo.SelectedIndex = -1;
        }


        /// <summary>
        /// Bind a company comobox BindComboCompanyMulti
        /// </summary>
        /// <param name="combo">CheckedComboBox</param>
        /// <param name="IncludeBlank">Include Blank</param>
        public static void BindComboCompanyMulti(CheckedComboBox combo, bool IncludeBlank)
        {
            DataTable dt = AccountCommonFunction.GetCompanyList(IncludeBlank);
            for (int i = 0; i <= dt.Rows.Count - 1; i++)
            {
                string companyname = dt.Rows[i][1].ToString().Trim();
                combo.Items.Add(companyname);
                if (companyname == frmDefaultVale.CompanyName)
                    combo.SetItemChecked(i, true);
            }
        }
        /// <summary>
        /// Bind ComboBox including all, Group companies and companies
        /// </summary>
        /// <param name="combo">ComboBox</param>
        public static void BindComboCompanyWithGC(ComboBox combo)
        {
            object obj = Cache.GetCache("CompanyWithGC");
            DataTable dt = null;

            if (obj != null)
                dt = ((DataTable)obj).Copy();
            else
            {
                Database.OpenConnection(UtilityPayroll.ConnectionString);
                int IsHOEmp = Convert.ToInt32(Database.GetExecuteNonQueryCommand_retrun("select isnull(IsHOEmp,0) from empinfo with(Nolock) where Empcode='" + FrmMain.Empcode + "'"));

                Database.OpenConnection(Utility.MaterialConnectionString);
                string strsql = "";

                if (FrmMain.UserName.ToLower() == "exauditor".ToLower())
                {
                    strsql = "select * from (select 'A-0' AS Value, 'All(*)' AS Name, 0 AS OridinalPosition union all select GroupNameValue, GroupName, "
                               + " ROW_NUMBER() OVER(ORDER BY GroupName asc) from ( "
                                + "  select distinct 'G-'+GroupName AS GroupNameValue, 'G-' + GroupName AS GroupName  "
                                + "  from FactoryInfo with(Nolock) inner join [Loginentry].[dbo].[LoginRightAccount] L with(Nolock)  on L.CompanyName=FactoryInfo.name "
                                + "  where GroupName!='' and len(GroupName)>5 and L.LoginID='" + FrmMain.UserName + "' "
                            + " ) as t union all select 'C-' + convert(varchar, SrNo), 'C-' + Name, OrderPosition+1000 from factoryInfo with(Nolock) "
                    + " inner join [Loginentry].[dbo].[LoginRightAccount] L with(Nolock) on L.CompanyName=FactoryInfo.name where L.LoginID='" + FrmMain.UserName + "' ) a  order by OridinalPosition";


                }
                else if (FrmMain.UserName.ToLower() == "auditorkpw".ToLower() || FrmMain.UserName.ToLower() == "kpwoutm".ToLower()
                    || FrmMain.UserName.ToLower() == "kpwoutm2".ToLower() || FrmMain.UserName.ToLower() == "kpwoutm1".ToLower()
                    || FrmMain.UserName.ToLower() == "kpwoutm3".ToLower() || FrmMain.UserName.ToLower() == "kpwoutm4".ToLower()
                    || FrmMain.UserName.ToLower() == "kpwoutc".ToLower() || FrmMain.UserName.ToLower() == "kpwoutm5".ToLower())
                {
                    strsql = " select * from (select GroupNameValue as Value, GroupName as Name, 1034 OridinalPosition from ( " +
                                  "  select distinct 'G-' + GroupName AS GroupNameValue, 'G-'+GroupName AS GroupName    from FactoryInfo with(Nolock) inner join [Loginentry].[dbo].[LoginRightAccount] L with(Nolock)  on L.CompanyName=FactoryInfo.name " +
                                  "   where GroupName!='' and len(GroupName)>5 and L.LoginID='auditorkpw' " +
                               ") as t union all select 'C-' + convert(varchar, SrNo), 'C-' + Name, OrderPosition+1000 from factoryInfo with(Nolock) " +
                       "inner join [Loginentry].[dbo].[LoginRightAccount] L with(Nolock) on L.CompanyName=FactoryInfo.name where L.LoginID='auditorkpw' ) a  order by OridinalPosition";
                }
                else if (FrmMain.isaccounthod || FrmMain.IsAdmin || IsHOEmp == 1)
                {
                    strsql = @"
                                select * from (select 'A-0' AS Value, 'All(*)' AS Name, 0 AS OridinalPosition union all select GroupNameValue, GroupName, 
                                ROW_NUMBER() OVER(ORDER BY GroupName asc) from (
                                select distinct 'G-'+GroupName AS GroupNameValue, 'G-' + GroupName AS GroupName from FactoryInfo with(Nolock) where GroupName!='' and len(GroupName)>5
                            ) as t union all select 'C-' + convert(varchar, SrNo), 'C-' + Name, OrderPosition+1000 from factoryInfo with(Nolock) ) A order by OridinalPosition";
                }
                else
                {
                    strsql = @" select * from (select 'C-' + convert(varchar, SrNo) AS Value, 'C-' + Name as   Name, OrderPosition+1000 as OridinalPosition 
                            from factoryInfo  with(Nolock)
                            where (name in ( select CompanyName from Loginentry..LoginRightAccount L with(Nolock) where L.LoginID='" + FrmMain.UserName
                            + "') OR NAME IN ( select CompName from Despatch..DefaultValue with(Nolock) WHERE LOGINNAME='" + FrmMain.UserName + "')) ) a order by OridinalPosition";

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
        /// FinancialYearStart : To get Current Financial year Date
        /// Created By : Raj Kansara On 05-11-2014
        /// </summary>
        /// <returns>DateTime</returns>
        public static DateTime FinancialYearStart()
        {

            DateTime dtStartYear = System.DateTime.Now;
            int cYear = DateTime.Now.Year;
            if (dtStartYear.Month < 4) cYear--;

            dtStartYear = new System.DateTime(cYear, 4, 1);
            return (DateTime)dtStartYear;

        }
        /// <summary>
        /// EndYearDate : To get Current Financial year End Date
        /// Created By : Raj Kansara On 09-04-2022
        /// </summary>
        /// <returns>DateTime</returns>
        public static DateTime EndYearStart()
        {
            DateTime dtEndYear = System.DateTime.Now;
            using (System.Data.SqlClient.SqlConnection conn = new System.Data.SqlClient.SqlConnection(Utility.MaterialConnectionString))
            {
                conn.Open();
                string strsql = @"SELECT top 1 todate FROM yearMaster with(Nolock) WHERE yr='" + frmDefaultVale.Year + "'";
                System.Data.SqlClient.SqlCommand command = new System.Data.SqlClient.SqlCommand(strsql, conn);
                System.Data.SqlClient.SqlDataReader reader = command.ExecuteReader();
                if (reader.Read())
                {
                    dtEndYear = Convert.ToDateTime(reader[0]);
                }
                reader.Close();
                conn.Close();
            }
            return dtEndYear;

        }
        /// <summary>
        /// Get Exchange Rate of currency
        /// </summary>
        /// <param name="currency">Currency</param>
        /// <param name="date">Date when we want rate</param>
        /// <returns>Currency Exchange Rate</returns>
        public static double GetExchangeRate(string currency, DateTime date)
        {
            if (currency == "INR" || currency.ToLower().StartsWith("rs"))
                return 1;

            if (Database.OpenConnection(Utility.MaterialConnectionString))
            {
                string columnName = "Dollar";
                switch (currency)
                {
                    case "USD":
                        columnName = "Dollar";
                        break;
                    case "GBP":
                        columnName = "Pound";
                        break;
                    case "EURO":
                        columnName = "Euro";
                        break;
                    case "CHF":
                        columnName = "CHF";
                        break;
                    case "ZAR":
                        columnName = "ZAR";
                        break;
                }
                double ExcRate = 0;
                using (System.Data.SqlClient.SqlConnection conn = new System.Data.SqlClient.SqlConnection(Utility.DespacthConnectionString))
                {


                    conn.Open();
                    string strsql = string.Format(
                    @"SELECT TOP 1 {1} FROM Despatch..Currency with(Nolock) WHERE '{0:yyyy-MM-dd}' between sysdate and ISNULL(todate, dateadd(YEAR, 5, getdate()))",
                    date, columnName);
                    System.Data.SqlClient.SqlCommand command = new System.Data.SqlClient.SqlCommand(strsql, conn);
                    System.Data.SqlClient.SqlDataReader reader = command.ExecuteReader();
                    if (reader.Read())
                    {
                        ExcRate = Convert.ToDouble(reader[0]);
                    }
                    reader.Close();
                    conn.Close();
                }


                return Math.Round(ExcRate, 4, MidpointRounding.AwayFromZero);
            }

            return 1;
        }

        /// <summary>
        /// Get ExchangeRate for Forex Gain loss in ERD
        /// </summary>
        /// <param name="currency">Currency</param>
        /// <param name="date">Date when we want rate</param>
        /// <returns>Currency Exchange Rate</returns>
        public static double GetERDExchangeRate(string currency, DateTime date)
        {
            if (currency == "INR" || currency == "Rs.")
                return 1;

            if (Database.OpenConnection(Utility.MaterialConnectionString))
            {
                string columnName = "Dollar";
                switch (currency)
                {
                    case "USD":
                        columnName = "Dollar";
                        break;
                    case "GBP":
                        columnName = "Pound";
                        break;
                    case "EURO":
                        columnName = "Euro";
                        break;
                    case "$":
                        columnName = "Dollar";
                        break;
                    case "€":
                        columnName = "Euro";
                        break;
                }
                return Utility.SafeConvertToDouble(Database.GetExecuteScalarCommand_return(string.Format(
                    @"SELECT TOP 1 {1} FROM Currency_RBI with(Nolock) WHERE '{0:yyyy-MM-dd}' between sysdate and ISNULL(todate, dateadd(YEAR, 5, getdate()))",
                    date, columnName)));
            }

            return 1;
        }

        /// <summary>
        /// Get Ledger Balance
        /// </summary>
        /// <param name="CompanyName">Company Name</param>
        /// <param name="LedgerName">Ledger Name</param>
        /// <returns>Ledger Balance</returns>
        public static double GetLedgerBalance(string CompanyName, string LedgerName)
        {
            double Balance = 0;

            object objBalance = Classes.Cache.GetCache(string.Format("LedgerBalance:{0}-{1}", CompanyName, LedgerName));
            if (objBalance != null)
                return (double)objBalance;

            Database.OpenConnection(Utility.MaterialConnectionString);
            Balance = AccountCommonFunction.SafeConvertToDouble(Database.ExecuteScalar("select sum(amount) from vw_LedgerSummary where CompanyName=@CompanyName and LedgerName=@LedgerName",
                new Database.Parameter("@CompanyName", CompanyName),
                new Database.Parameter("@LedgerName", LedgerName)
                ));

            Classes.Cache.AddCache(Balance, 10, string.Format("LedgerBalance:{0}-{1}", CompanyName, LedgerName));

            return Balance;
        }


        /// <summary>
        /// Get Ledger Balance Groupwise
        /// </summary>
        /// <param name="CompanyName">Company Name</param>
        /// <param name="LedgerName">Ledger Name</param>
        /// <returns>Ledger Balance</returns>
        public static double GetLedgerBalanceGroup(string CompanyName, string LedgerName)
        {
            double Balance = 0;

            //object objBalance = Classes.Cache.GetCache(string.Format("LedgerGRPBalance:{0}-{1}", CompanyName, LedgerName));
            //if (objBalance != null)
            //    return (double)objBalance;

            Database.OpenConnection(Utility.MaterialConnectionString);
            string Groupname = Convert.ToString(Database.GetExecuteNonQueryCommand_retrun("Select isnull(GroupName,'') as GroupName from FactoryInfo with(Nolock) where name='" + CompanyName + "'"));
            Balance = AccountCommonFunction.SafeConvertToDouble(Database.ExecuteScalar("select sum(amount) from vw_LedgerSummary V with(Nolock) inner join FactoryInfo F with(Nolock) on F.Name=v.CompanyName  where F.GroupName=@GroupName and LedgerName=@LedgerName",
                new Database.Parameter("@GroupName", Groupname),
                new Database.Parameter("@LedgerName", LedgerName)
                ));

            //Classes.Cache.AddCache(Balance, 10, string.Format("LedgerGRPBalance:{0}-{1}", CompanyName, LedgerName));

            return Balance;
        }

        /// <summary>
        /// Make New Ref when create new invoice
        /// </summary>
        /// <param name="CompanyName"></param>
        /// <param name="LedgerName"></param>
        /// <param name="SelectionType"></param>
        /// <param name="BillNo"></param>
        /// <param name="BillDate"></param>
        /// <param name="PaymentTerms"></param>
        /// <param name="Amount"></param>
        /// <param name="Currency"></param>
        /// <returns></returns>
        public static bool CreateNewBillRef(string CompanyName, string LedgerName, string SelectionType, string BillNo, DateTime BillDate, string PaymentTerms, double Amount, string Currency, int companyId, int LedgerId)
        {
            // we save only if selection type is newRef or advance
            if (SelectionType != "New Ref" && SelectionType != "Advance")
                return true;

            if (0 == (int)Database.GetExecuteScalarCommand_return(string.Format("SELECT COUNT(*) FROM AccountBills with(nolock) WHERE CompanyName='{0}' AND LedgerName='{1}' AND BillNo='{2}'", CompanyName, LedgerName, BillNo)))
            {
                int days = PaymentTerms.Length == 0 ? 0 : (int)Utility.SafeConvertToDouble(Database.GetExecuteScalarCommand_return(string.Format("SELECT distinct DAYS FROM PaymentTermsMaster  with(nolock) WHERE PaymentTermName='{0}'", PaymentTerms)));
                DateTime DueDate = BillDate.AddDays(days);

                if (String.IsNullOrEmpty(Currency))
                    Currency = "Rs.";

                Database.GetExecuteNonQueryCommand(string.Format(@"INSERT INTO AccountBills (CompanyName, LedgerName, BillNo, BillDate, DueDate, BillAmount, BillCurrency, IsClosed,companyId,LedgerId) VALUES (
                    '{0}', '{1}', '{2}', '{3:yyyy-MM-dd}', '{4:yyyy-MM-dd}', {5}, '{6}', 0,{7},{8})",
                    CompanyName, LedgerName, BillNo, BillDate, DueDate, Amount, Currency, companyId, LedgerId));
            }

            return true;
        }

        /// <summary>
        /// Get Record Log History
        /// </summary>
        /// <param name="RecordLogId">Record Log Id</param>
        /// <returns>DataTable</returns>
        public static DataTable GetRecordLogHistory(int RecordLogId)
        {
            return frmHistoryLog.GetRecordLogHistory(RecordLogId);
        }

        /// <summary>
        /// Get Record Log History data in ListViewItems
        /// </summary>
        /// <param name="RecordLogId">Record Log Id</param>
        /// <returns>List of ListViewItem</returns>
        public static List<ListViewItem> GetRecordLogHistoryListViewItems(int RecordLogId)
        {
            return frmHistoryLog.GetRecordLogHistoryListViewItems(RecordLogId);
        }

        /// <summary>
        /// Get Entry Access
        /// </summary>
        /// <param name="CompanyName">Company Name</param>
        /// <param name="VoucherDate">Voucher Date</param>
        /// <param name="VoucherType">Voucher Type</param>
        /// <param name="VoucherNo">Voucher No</param>
        /// <returns>true on success</returns>
        public static bool GetEntryAccess(string CompanyName, DateTime VoucherDate, string VoucherType, string VoucherNo)
        {
            return GetEntryAccess(CompanyName, VoucherDate, VoucherType, VoucherNo, false);
        }

        /// <summary>
        /// Get Entry Access
        /// </summary>
        /// <param name="CompanyName">Company Name</param>
        /// <param name="VoucherDate">Voucher Date</param>
        /// <param name="VoucherType">Voucher Type</param>
        /// <param name="VoucherNo">Voucher No</param>
        /// <param name="DontThrow">Dont throw exception</param>
        /// <returns>true on success</returns>
        public static bool GetEntryAccess(string CompanyName, DateTime VoucherDate, string VoucherType, string VoucherNo, bool DontThrow)
        {
            Database.OpenConnection(Utility.MaterialConnectionString);

            int Count = AccountCommonFunction.SafeConvertToInt(Database.ExecuteScalar(
@"
select COUNT(*)
from ac_voucher_lock vl with(Nolock)
inner join FactoryInfo fi with(Nolock) on vl.CompanyId=fi.SrNo
inner join yearMaster y with(Nolock) on vl.FinancialYear=y.Yr
inner join VoucherType vt with(Nolock) on vl.VoucherTypeGroup=vt.GroupName
where vl.VoucherNo=@VoucherNo
	and @Date between y.fromdate and y.todate
	and fi.Name=@CompanyName
	and vt.VoucherTypeName=@VoucherType
	and vl.IsLocked=1
",
    new Database.Parameter("@VoucherNo", VoucherNo),
    new Database.Parameter("@Date", VoucherDate),
    new Database.Parameter("@CompanyName", CompanyName),
    new Database.Parameter("@VoucherType", VoucherType)
 ));

            if (Count == 0)
                return true;

            if (DontThrow)
                return false;

            throw new ERPErrorException("This entry was locked. You can't modify or delete this entry\n\nContact account admin to get access to modify or delete this entry");
        }
        #region Context Menu Class for Hide/UnHide Columns
        public class ColumnsHide
        {
            public static void HileColumn(DataGridView dv, string ColumnName)
            {
                try
                {
                    dv.Columns[ColumnName].Visible = !dv.Columns[ColumnName].Visible;
                }
                catch (Exception ex)
                {
                    ex.Message.ToString();
                }
            }
            public static ContextMenuStrip GetColumns(ContextMenuStrip cm, DataGridView dt)
            {
                ToolStripMenuItem mi = new ToolStripMenuItem();
                mi.Name = "Columns";
                mi.Text = "Columns";
                for (int i = 0; i < dt.Columns.Count - 1; i++)
                {
                    ToolStripMenuItem mi2 = new ToolStripMenuItem();
                    mi2.Text = dt.Columns[i].HeaderText;
                    mi2.Name = dt.Columns[i].Name;
                    mi2.Checked = dt.Columns[i].Visible;
                    mi2.Click += (s, e) => { HileColumn(dt, dt.Columns[i].Name); };
                    mi.DropDownItems.Add(mi2);
                }
                cm.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { mi });
                return cm;
            }


        }
        #endregion
        #region Context Menu Class
        /// <summary>
        /// Account Context Menu for DataGridView
        /// </summary>
        public class AccountContextMenu
        {
            /// <summary>
            /// Voucher Context Menu
            /// </summary>
            /// <param name="avd">AccountVoucherDetail Object</param>
            /// <returns></returns>
            public static ContextMenuStrip GetCommonVoucherContext(AccountVoucherDetails avd)
            {
                ContextMenuStrip cm = new System.Windows.Forms.ContextMenuStrip();
                cm.Name = "contextMenuStrip";

                AddCommonVoucherOption(cm, avd);

                return cm;
            }

            /// <summary>
            /// Add Option for voucher
            /// </summary>
            /// <param name="cm">Context Menu</param>
            /// <param name="avd">AccountVoucherDetail Object</param>
            public static void AddCommonVoucherOption(ContextMenuStrip cm, AccountVoucherDetails avd)
            {
                ToolStripMenuItem openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
                ToolStripMenuItem printToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
                ToolStripMenuItem expandDetailsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
                ToolStripMenuItem ledgerDetailToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
                ToolStripMenuItem viewLedgerSummaryToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
                ToolStripMenuItem voucherReferenceTransactionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();

                ToolStripMenuItem ExcelExportToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();


                // open
                openToolStripMenuItem.Name = "openToolStripMenuItem";
                openToolStripMenuItem.Text = "&Open";
                openToolStripMenuItem.Font = new System.Drawing.Font(openToolStripMenuItem.Font, System.Drawing.FontStyle.Bold);
                openToolStripMenuItem.Click += (s, e) => { avd.Open(); };

                // print
                printToolStripMenuItem.Name = "printToolStripMenuItem";
                printToolStripMenuItem.Text = "&Print";
                printToolStripMenuItem.Click += (s, e) => { avd.Print(); };

                // expand
                expandDetailsToolStripMenuItem.Name = "expandDetailsToolStripMenuItem";
                //expandDetailsToolStripMenuItem.Size = new System.Drawing.Size(232, 22);
                expandDetailsToolStripMenuItem.Text = "Expand Details";
                expandDetailsToolStripMenuItem.Click += (s, e) => { avd.Expand(); };
                expandDetailsToolStripMenuItem.Enabled = avd.AllowExpand;

                // ledger detail
                ledgerDetailToolStripMenuItem.Name = "ledgerDetailToolStripMenuItem";
                ledgerDetailToolStripMenuItem.Text = "Ledger Detail";
                ledgerDetailToolStripMenuItem.Click += (s, e) => { avd.ShowLedgerDetail(); };

                // view ledger summary
                viewLedgerSummaryToolStripMenuItem.Name = "viewLedgerSummaryToolStripMenuItem";
                viewLedgerSummaryToolStripMenuItem.Text = "View Ledger Summary";
                viewLedgerSummaryToolStripMenuItem.Click += (s, e) => { avd.ShowLedgerSummary(); };

                // voucher reference transaction
                voucherReferenceTransactionToolStripMenuItem.Name = "voucherRefrenceTransactionToolStripMenuItem";
                voucherReferenceTransactionToolStripMenuItem.Text = "Voucher Reference Transaction";
                voucherReferenceTransactionToolStripMenuItem.Click += (s, e) => { avd.ShowRefTransaction(); };


                //Excel Export

                ExcelExportToolStripMenuItem.Name = "ExcelExportToolStripMenuItem";
                ExcelExportToolStripMenuItem.Text = "Excel Export";
                ExcelExportToolStripMenuItem.Click += (s, e) => { avd.ExcelExport(); };



                cm.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
                    openToolStripMenuItem,
                    printToolStripMenuItem,
                    expandDetailsToolStripMenuItem,
                    ledgerDetailToolStripMenuItem,
                    viewLedgerSummaryToolStripMenuItem,
                    voucherReferenceTransactionToolStripMenuItem,
                    ExcelExportToolStripMenuItem});
            }
        }
        #endregion
        /// <summary>
        ///  StereoNarration
        /// Press Ctrl + L and it will open list of all entered Remarks List and then chose appro. One
        /// </summary>
        public static DataTable StereoNarration(string tablename, string FieldName, string condition, string orderbyfld)
        {

            string strql = "select distinct isnull(" + FieldName + ",'') ," + orderbyfld + " from " + tablename + " with(nolock) where isnull(" + FieldName + ",'')<>'' ";
            strql = strql + " and CompanyName='" + frmDefaultVale.CompanyName + "'";
            if (condition.Length > 0)
                //strql = strql + " and " + tablename + " like '" + condition + "%'";
                strql = strql + " Order by " + (orderbyfld.Length > 0 ? orderbyfld : FieldName) + " desc";

            DataTable dataTable = new DataTable("StereoNarration");
            if (Database.OpenConnection(Utility.MaterialConnectionString))
            {
                Database.GetExecuteReaderCommand(strql);
                dataTable.Load(Database.myreader);
            }
            return dataTable.Copy();
        }
        /// <summary>
        /// GetexchangeRateDiff : SQL function name dbo.GetForex
        /// </summary>
        /// <parameter>
        /// @CompanyName : Pass company name
        /// @DateTo : Upto Date
        /// @LedgerName : Ledger Name
        /// </parameter>
        /// <returns></returns>
        public static double GetExchangeRateDiff(string companyname, string Ledgername, DateTime DateTo)
        {
            string strsql = "select [dbo].[GetForex]('" + companyname + "','" + Ledgername + "','" + Convert.ToString(DateTo) + "')";
            double Excvalue = Utility.SafeConvertToDouble(Database.GetExecuteScalarCommand_return(strsql));

            return Math.Round(Excvalue);
        }
        public static List<Currency> GetCurrencyRate(List<Currency> currency, DateTime DateTo)
        {
            currency.Clear();
            currency.Add(new Currency("Rs.", 1));

            if (Database.OpenConnection(Utility.MaterialConnectionString))
            {
                Database.myreader = Database.GetExecuteReaderCommand(string.Format("SELECT ISNULL(dollar,0) dollar,ISNULL(pound,0) pound,ISNULL(euro,0) euro, ISNULL(CHF,0) AS CHF ,ISNULL(ZAR,0) AS ZAR  ,ISNULL(AED,0) AS AED ,ISNULL(AUD,0) AS AUD,ISNULL(JPY,0) AS JPY FROM Currency_RBI with(Nolock) WHERE ('{0}' BETWEEN sysdate AND todate OR('{0}' >= sysdate AND todate IS NULL)) ORDER BY sysdate DESC", DateTo.ToString("yyyy-MM-dd")));
                if (Database.myreader.Read())
                {
                    currency.Add(new Currency("$", (double)Database.myreader[0]));
                    currency.Add(new Currency("GBP", (double)Database.myreader[1]));
                    currency.Add(new Currency("€", (double)Database.myreader[2]));
                    currency.Add(new Currency("CHF", (double)Database.myreader[3]));
                    currency.Add(new Currency("ZAR", (double)Database.myreader[4]));
                    currency.Add(new Currency("AED", (double)Database.myreader[5]));
                    currency.Add(new Currency("A$", (double)Database.myreader[6]));
                    currency.Add(new Currency("JP¥", (double)Database.myreader[7]));
                }
                else
                {
                    currency.Add(new Currency("$", 0));
                    currency.Add(new Currency("GBP", 0));
                    currency.Add(new Currency("€", 0));
                    currency.Add(new Currency("CHF", 0));
                    currency.Add(new Currency("ZAR", 0));
                    currency.Add(new Currency("AED", 0));
                    currency.Add(new Currency("A$", 0));
                    currency.Add(new Currency("JP¥", 0));
                }
                Database.myreader.Close();
            }
            return currency;

        }
        public static bool FoundLedgerInMaster(string ledgerName, string companyname)
        {
            bool blnNotfnd = true;
            int intcnt;
            string strsql = "Select isnull(count(*),0) from LedgerMaster with(Nolock) where Ledgername='" + ledgerName + "' and companyname='" + companyname + "'";
            intcnt = Convert.ToInt16(Database.GetExecuteNonQueryCommand_retrun(strsql));
            if (intcnt == 0)
            {
                blnNotfnd = false;
            }
            return blnNotfnd;

        }
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
            int num_cols = 3;// lines[0].Split(',').Length;

            // Allocate the data array.
            string[,] values = new string[num_rows, 3];

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
        public static bool IsEBIDTALedger(string companyname, string groupName)
        {
            bool blnNotfnd = true;
            int intcnt = 0;

            string strsql = @"
;WITH GROUP_CTE(GroupName, IsPNL)
AS
(
	SELECT ExpenseHead, CONVERT(BIT, CASE WHEN m.CommonExpId IS NOT NULL THEN 0 WHEN IsPnL='yes' THEN 1 ELSE 0 END) AS IsPNL
	FROM CashVoucherExpenseHead c with(Nolock)
	left join ac_mis_common_exp_master m with(Nolock) on c.ExpenseHead=m.GroupName
	where CompanyName='{0}'
		--and m.CommonExpId is null
	UNION ALL
	SELECT ExpenseGroupHead, CONVERT(BIT, CASE WHEN ExpenseGroupHead in (select GroupName from ac_mis_common_exp_master with(Nolock)) THEN 0 ELSE g.IsPNL END)
	FROM CashVoucherExpenseGroupHead c with(Nolock)
	INNER JOIN GROUP_CTE g ON c.ExpenseHead=g.GroupName
	--left join ac_mis_common_exp_master m on c.ExpenseGroupHead=m.GroupName
	WHERE CompanyName='{0}'
		--and c.ExpenseGroupHead not in (select GroupName from ac_mis_common_exp_master with(Nolock))
)
select count(l.Under)
from LedgerMaster l with(Nolock)
inner join GROUP_CTE g on l.Under=g.GroupName
inner join FactoryInfo fi with(Nolock) on fi.Name=l.CompanyName
left join ac_mis_tb_head tb with(Nolock) on l.srno=tb.LedgerId and tb.IsEditable=1 and tb.LedgerId!=0
left join ac_mis_main_report_head m with(Nolock) on tb.MainHeadId=m.Id
where l.CompanyName='{0}' and l.Under ='{1}'
	and ( IsPNL=1)
--(IsPNL=0 and tb.Id is not null) or
";
            strsql = string.Format(strsql, companyname, groupName);
            if (Database.OpenConnection(Utility.MaterialConnectionString))
                intcnt = Convert.ToInt16(Database.GetExecuteNonQueryCommand_retrun(strsql));
            if (intcnt == 0)
            {
                blnNotfnd = false;
            }
            return blnNotfnd;

        }

        public static AutoCompleteStringCollection GetCommodityList()
        {
            AutoCompleteStringCollection returnColl = new AutoCompleteStringCollection();
            try
            {
                string strsql = "select distinct CommodityName from Commodity with(Nolock) where CompanyName='" + frmDefaultVale.CompanyName + "'";

                using (System.Data.SqlClient.SqlConnection conn = new System.Data.SqlClient.SqlConnection(Utility.DespacthConnectionString))
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

        /// <summary>
        /// Get List of Ledgers After 2019-04-01 From To Logic with is Account Closed
        /// </summary>
        /// <param name="CompanyName">Company Name of Ledgers</param>
        /// <returns>DataTable</returns>
        public static DataTable GetLedgerName(string CompanyName, DateTime Date)
        {
            // Get From cache
            object obj = Cache.GetCache("Ledgers-" + CompanyName);
            if (obj != null)
                return ((DataTable)obj).Copy();

            // Get From Database
            DataTable dataTable = new DataTable("Ledgers");
            if (Database.OpenConnection(Utility.MaterialConnectionString))
            {
                Database.GetExecuteReaderCommand("select Ledgername from ledgermaster  with(Nolock) Where  companyname = '"
                        + CompanyName + "' and isnull(IsAccClose,'no')='no' and (('" + Date.ToString("yyyy-MM-dd")
                        + "' between fromdate and todate) or (fromdate <= '"
                        + Date.ToString("yyyy-MM-dd") + "' and todate is null )) "
                        + " order by LedgerName");
                dataTable.Load(Database.myreader);
            }

            if (dataTable.Rows.Count != 0) Cache.AddCache(dataTable, 10, "Ledgers-" + CompanyName);
            return dataTable.Copy();
        }
        /// <summary>
        /// Get List of Ledgers After 2019-04-01 From To Logic with is Account Closed
        /// </summary>
        /// <param name="CompanyName">Company Name of Ledgers</param>
        /// <returns>DataTable</returns>
        public static DataTable GetLedgerNameofBank(string CompanyName, DateTime Date)
        {
            // Get From cache
            object obj = Cache.GetCache("LedgersBank-" + CompanyName);
            if (obj != null)
                return ((DataTable)obj).Copy();

            // Get From Database
            DataTable dataTable = new DataTable("LedgersBank");
            if (Database.OpenConnection(Utility.MaterialConnectionString))
            {
                Database.GetExecuteReaderCommand("select Ledgername,Srno from ledgermaster with(Nolock) Where (category = 'Bank Accounts' or category = 'Cash-in-hand') and companyname = '"
                        + CompanyName + "' and isnull(IsAccClose,'no')='no' and (('" + Date.ToString("yyyy-MM-dd")
                        + "' between fromdate and todate) or (fromdate <= '"
                        + Date.ToString("yyyy-MM-dd") + "' and todate is null )) "
                        + " order by LedgerName");
                dataTable.Load(Database.myreader);
            }

            if (dataTable.Rows.Count != 0) Cache.AddCache(dataTable, 10, "LedgersBank-" + CompanyName);
            return dataTable.Copy();
        }

        /// <summary>
        /// Get List of Ledgers After 2019-04-01 From To Logic with is Account Closed for Tax ledgers
        /// </summary>
        /// <param name="CompanyName">Company Name of Ledgers</param>
        /// <returns>DataTable</returns>
        public static DataTable GetLedgerNameofTax(string CompanyName, DateTime Date)
        {
            // Get From cache
            object obj = Cache.GetCache("LedgersTax-" + CompanyName);
            if (obj != null)
                return ((DataTable)obj).Copy();

            // Get From Database
            DataTable dataTable = new DataTable("LedgersTax");
            if (Database.OpenConnection(Utility.MaterialConnectionString))
            {
                Database.GetExecuteReaderCommand("select Ledgername  as TaxLedgerName ,Srno as TaxLedgerId from ledgermaster with(Nolock) Where (category in ('Expense','Income', 'TAX','TDS') ) and companyname = '"
                        + CompanyName + "' and isnull(IsAccClose,'no')='no' and (('" + Date.ToString("yyyy-MM-dd")
                        + "' between fromdate and todate) or (fromdate <= '"
                        + Date.ToString("yyyy-MM-dd") + "' and todate is null )) "
                        + " order by LedgerName");
                dataTable.Load(Database.myreader);
            }

            if (dataTable.Rows.Count != 0) Cache.AddCache(dataTable, 10, "LedgersTax-" + CompanyName);
            return dataTable.Copy();
        }
        public static DataTable GetLedgerNameofTDS(string CompanyName, DateTime Date)
        {
            // Get From cache
            object obj = Cache.GetCache("LedgersTDS-" + CompanyName);
            if (obj != null)
                return ((DataTable)obj).Copy();

            // Get From Database
            DataTable dataTable = new DataTable("LedgersTDS");
            if (Database.OpenConnection(Utility.MaterialConnectionString))
            {
                Database.GetExecuteReaderCommand("select Ledgername  as TaxLedgerName ,Srno as TaxLedgerId from ledgermaster with(Nolock) Where (category in ( 'TDS') ) and companyname = '"
                        + CompanyName + "' and isnull(IsAccClose,'no')='no' and (('" + Date.ToString("yyyy-MM-dd")
                        + "' between fromdate and todate) or (fromdate <= '"
                        + Date.ToString("yyyy-MM-dd") + "' and todate is null )) "
                        + " order by LedgerName");
                dataTable.Load(Database.myreader);
            }

            if (dataTable.Rows.Count != 0) Cache.AddCache(dataTable, 10, "LedgersTDS-" + CompanyName);
            return dataTable.Copy();
        }
        public static bool CheckBillPaymentReq(string companyname, string vendorName, string BillNo, string MRNNo, string yr)
        {
            try
            {
                return false;

                if (Database.OpenConnection(Utility.MaterialConnectionString))
                {

                    string GSTNo = "";
                    string VendorCode = "";
                    string PanNo = "";
                    Database.myreader = Database.GetExecuteReaderCommand("Select NewGSTNO,LVendorCode,PANNo from ledgermaster with(Nolock) where ledgername = '"
                            + vendorName + "' and companyname = '" + companyname + "'");
                    if (Database.myreader.Read())
                    {
                        GSTNo = Convert.ToString(Database.myreader["NewGSTNO"]);
                        VendorCode = Convert.ToString(Database.myreader["LVendorCode"]);
                        PanNo = Convert.ToString(Database.myreader["PANNo"]);
                    }

                    string LvendorName = Convert.ToString(Database.GetExecuteNonQueryCommand_retrun("Select FirmName from Vendor with(Nolock) where " +
                        "NewGSTNo='" + GSTNo + "' and Vendorcode='" + VendorCode + "'"));


                    object obj = Database.GetExecuteNonQueryCommand_retrun("select count(*) from BillPaymentEntry  with(Nolock) where BillNo = '"
                                                  + BillNo + "' and MRNno='" + MRNNo + "' and VendorName='" + LvendorName + "' and yr='" + yr + "' AND PaymentTerms NOT LIKE '%Advance%'");
                    int TotalCount = Convert.ToInt32(obj);
                    if (TotalCount > 0)
                    {
                        return true;
                    }

                    //Check For PANCode
                }
                return false;

            }
            catch (Exception ex)
            {
                return false;
                ex.Message.ToString();
            }
        }
        public static void SetAccountEntryApproval(string companyname, string LoginUser, string VoucherType, string VoucherNo, DateTime VoucherDate, string yr)
        {
            try
            {
                if (Database.OpenConnection(Utility.MaterialConnectionString))
                {
                    string strsql = "select * from AccountEntryAllocation with(Nolock) where companyname='" + companyname + "'";
                    DataTable dt = Database.GetDataTable(strsql);
                    for (int i = 0; i <= dt.Rows.Count - 1; i++)
                    {
                        string ApprovalName = Convert.ToString(dt.Rows[i]["ApprovalName"].ToString());
                        string insert = "insert into AccountEntryApproval(LoginUser,CompanyName,VoucherType,VoucherNo,VoucherDate,yr,Status,ApprovalName,ApprovalDate) " +
                            " values ('" + LoginUser + "','" + companyname + "','" + VoucherType + "','" + VoucherNo + "','" + VoucherDate.ToString("yyyy-MM-dd HH:mm:ss") +
                             "','" + yr + "','Pending','" + ApprovalName + "',NULL)";
                        Database.GetExecuteNonQueryCommand_retrun(insert);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Problem with SetAccountEntryApproval : " + ex.Message);
            }
        }
        public static int AccoutnEntryLevel(string companyname, string LoginUser)
        {
            int ret = 0;

            try
            {
                string strsql = "select * from AccountEntryAllocation with(Nolock) where companyname='" + companyname + "'";
                using (System.Data.SqlClient.SqlConnection conn = new System.Data.SqlClient.SqlConnection(Utility.DespacthConnectionString))
                {
                    conn.Open();
                    System.Data.SqlClient.SqlCommand command = new System.Data.SqlClient.SqlCommand(strsql, conn);
                    System.Data.SqlClient.SqlDataReader reader = command.ExecuteReader();

                    if (reader.Read())
                    {
                        ret = Convert.ToInt32(reader[0]);
                    }
                }
                return ret;

            }
            catch (Exception ex)
            {
                return ret;
            }
        }
        /// <summary>
        /// GetClosedLedger with FromDate
        /// </summary>
        /// <param name="CompanyName">Company Name of Ledgers</param>
        /// <returns>DataTable</returns>
        public static DataTable GetClosedLedger(string CompanyName)
        {
            // Get From Database
            Tools.Cache.ClearAllCache();
            DataTable dataTable = new DataTable("Ledgers");
            if (Database.OpenConnection(Utility.MaterialConnectionString))
            {
                Database.GetExecuteReaderCommand(string.Format("SELECT case when IsAccClose='yes' then LedgerName + ' - [Closed]' when IsAccClose='no' and Todate is not null then LedgerName + ' - [Closed up to-' + convert(varchar(10),Todate,103) + ']' else LedgerName end AS LedgerName, Srno AS LedgerId FROM LedgerMaster with(Nolock) WHERE CompanyName='{0}' and (IsAccClose='yes' or todate is not null)  order by Todate ,LedgerName", CompanyName));
                dataTable.Load(Database.myreader);
            }

            return dataTable.Copy();
        }

        /// <summary>
        /// Get List of Ledgers
        /// </summary>
        /// <param name="SelectedCompany">Company Name (GC) of Ledgers</param>
        /// <para>A- All</para>
        /// <para>G- Group Company</para>
        /// <para>C- Company</para>
        /// <returns></returns>
        public static DataTable GetClosedLedgerListGC(string SelectedCompany)
        {

            // Get From Database
            DataTable dataTable = new DataTable("Ledgers");

            //if (SelectedCompany.Length < 2)
            //    return dataTable;

            if (Database.OpenConnection(Utility.MaterialConnectionString))
            {
                string v = SelectedCompany;

                string Query = string.Format("SELECT LedgerName AS LedgerName, l.srno AS LedgerId FROM LedgerMaster l with(Nolock) INNER JOIN FactoryInfo fi with(Nolock) ON l.CompanyName=fi.Name WHERE fi.SrNo={0} and (l.IsAccClose='yes' or l.todate is not null)", 0);

                if (v.Length > 0)
                {
                    switch (v[0])
                    {
                        case 'A':
                            Query = "SELECT DISTINCT LedgerName AS LedgerName, 0 AS LedgerId FROM LedgerMaster l with(Nolock) INNER JOIN FactoryInfo fi with(Nolock) ON l.CompanyName=fi.Name where   (l.IsAccClose='yes' or l.todate is not null)";
                            break;
                        case 'G':
                            Query = string.Format("SELECT DISTINCT LedgerName AS LedgerName, 0 AS LedgerId FROM LedgerMaster l with(Nolock) INNER JOIN FactoryInfo fi  with(Nolock) ON l.CompanyName=fi.Name WHERE fi.GroupName='{0}' and (l.IsAccClose='yes' or l.todate is not null)", v.Substring(2));
                            break;
                        case 'C':
                            Query = string.Format("SELECT LedgerName AS LedgerName, l.srno AS LedgerId FROM LedgerMaster l with(Nolock) INNER JOIN FactoryInfo fi with(Nolock) ON l.CompanyName=fi.Name WHERE fi.SrNo={0} and (l.IsAccClose='yes' or l.todate is not null)", AccountCommonFunction.SafeConvertToInt(v.Substring(2)));
                            break;
                        default:
                            Query = string.Format("SELECT LedgerName AS LedgerName, l.srno AS LedgerId FROM LedgerMaster l with(Nolock) INNER JOIN FactoryInfo fi with(Nolock) ON l.CompanyName=fi.Name WHERE fi.SrNo={0} and (l.IsAccClose='yes' or l.todate is not null)", AccountCommonFunction.SafeConvertToInt(v.Substring(2)));
                            break;
                    }
                }
                Query += " ORDER BY LedgerName";

                Database.GetExecuteReaderCommand(Query);
                dataTable.Load(Database.myreader);
            }
            return dataTable.Copy();
        }
        /// <summary>
        /// Get List of Ledgers
        /// </summary>
        /// <param name="CompanyName">Company Name of Ledgers</param>
        /// <returns>DataTable</returns>
        public static DataTable GetOpenLedgerList(string CompanyName)
        {
            // Get From Database
            Tools.Cache.ClearAllCache();
            DataTable dataTable = new DataTable("Ledgers");
            if (Database.OpenConnection(Utility.MaterialConnectionString))
            {
                Database.GetExecuteReaderCommand(string.Format("SELECT LedgerName AS LedgerName, srno AS LedgerId FROM LedgerMaster with(Nolock) WHERE CompanyName='{0}' and (todate is null and fromdate is not null ) order by Todate ,LedgerName", CompanyName));
                dataTable.Load(Database.myreader);
            }
            return dataTable.Copy();
        }
        public static void AccountEntryApproval(string companyname, string vouchertype, string voucherno, string yr, DateTime Date)
        {
            #region Send for Account Entry Approve

            if (Database.OpenConnection(Utility.MaterialConnectionString))
            {

                Database.ExecuteNonQuery("sp_AccountEntryApproval", System.Data.CommandType.StoredProcedure,
                  new Database.Parameter("@datefrom", Date.ToString("yyyy-MM-dd")),
                  new Database.Parameter("@Date", Date.ToString("yyyy-MM-dd")),
                  new Database.Parameter("@companyname", companyname),
                  new Database.Parameter("@vouchertype", vouchertype),
                  new Database.Parameter("@VoucherNo", voucherno),
                  new Database.Parameter("@YR", yr),
                  new Database.Parameter("@UserName", FrmMain.UserName)
                  );

            }
            #endregion
        }
        public static void LoadTDSLedger(DataGridView dataGridView1, int companyid, string ledgername, double Amount, DateTime fromdate, int TransId,
            int rowIndex, string remarks, bool Isremarks = false, bool IsJV = false, string companyname = "", bool blntcsapp = false,
            bool IsPur = true, double exrate = 1, bool isSales = false, bool IsJobWork = false)
        {
            try
            {
                if (Amount == 0) return;
                if (Database.OpenConnection(Utility.MaterialConnectionString))
                {
                    double TotalPurBillAmt = 0;
                    string IsNaturePayment = "";
                    double dblTDSRate = 0;
                    int IsvalidPan = 0;
                    bool Iscalculate = false;
                    string TDSApp = "";
                    string grp = "";
                    int IntUnitAc = AccountCommonFunction.GetInterUnit(companyname, ledgername, fromdate);
                    if (IntUnitAc == 1)
                    {
                        return;
                    }

                    if (IsPur)
                    {
                        for (int j = 0; j <= dataGridView1.Rows.Count - 1; j++)
                        {
                            string TaxLedger = Convert.ToString(dataGridView1.Rows[j].Cells[0].FormattedValue);
                            if (TaxLedger.ToLower().Contains("tcs"))
                            {
                                blntcsapp = true;
                                break;
                            }
                        }
                    }
                    if (IsPur)
                        TotalPurBillAmt = Math.Abs(Utility.SafeConvertToDouble(Database.GetExecuteNonQueryCommand_retrun("Select dbo.[fn_Get_Ledger_Purchase]('"
                            + fromdate.ToString("yyyy-MM-dd") + "','" + companyname + "','" + ledgername + "')")));

                    string strsql = "Select lower(isnull(IsTDS,'no')) as IsTDS,IsNaturePayment,tdsRate,dbo.udf_CheckPAN(PANNo) as IsvalidPan,PANNo,under " +
                        " from ledgerMaster where Ledgername='" + ledgername + "' and " + " CompanyID=" + companyid;
                    DataTable tdsApp = Database.GetDataTable(strsql);
                    if (tdsApp.Rows.Count > 0)
                    {
                        IsNaturePayment = Convert.ToString(tdsApp.Rows[0]["IsNaturePayment"]);
                        dblTDSRate = Utility.SafeConvertToDouble(tdsApp.Rows[0]["tdsRate"]);
                        IsvalidPan = Convert.ToInt32(tdsApp.Rows[0]["IsvalidPan"]);
                        TDSApp = Convert.ToString(tdsApp.Rows[0]["IsTDS"]);
                        grp = Convert.ToString(tdsApp.Rows[0]["under"]);
                    }
                    if (IsPur)
                    {
                        if (TDSApp.ToLower() == "yes")
                        {
                            if (TotalPurBillAmt + Amount > 5000000)
                                Iscalculate = true;
                        }
                    }
                    else
                    {
                        Iscalculate = !blntcsapp;
                        if (grp.ToLower() == "Debtors-Overseas".ToLower())
                            Iscalculate = false;
                    }
                    if (Iscalculate)
                    {

                        if (dblTDSRate == 0)
                        {
                            dblTDSRate = Utility.SafeConvertToDouble(Database.GetExecuteNonQueryCommand_retrun("select isnull(TDSRate,0) from TDSNatureofPayment where Natureofpayment='" + IsNaturePayment + "'"));
                        }
                        if (IsvalidPan == 1)
                        {
                            if (dblTDSRate == 0)
                                dblTDSRate = 0.1;
                        }
                        else if (IsvalidPan == 0)
                        {
                            if (dblTDSRate == 0)
                                dblTDSRate = 5;
                        }
                        if (IsJobWork) //as per telephonic discussion in Jobwork Invoice 2% TDS will be apply
                            dblTDSRate = 2;

                        string SectionFullname = Convert.ToString(Database.GetExecuteNonQueryCommand_retrun("select isnull(TDSRateSection,0) from TDSNatureofPayment where Natureofpayment='" + IsNaturePayment + "'"));
                        if (dblTDSRate != 0)
                        {
                            double Amt = Amount / exrate;

                            // comment by 6th May 2024
                            //double dblTDSAmt = Math.Round(Amt * dblTDSRate / 100, 2);
                            // end comment
                            double dblTDSAmt = Math.Round(Amt * dblTDSRate / 100, 6);
                        
                            DataTable dt = new DataTable();
                            string LedgerName = "";

                            strsql = "select Ledgername from ledgermaster with(Nolock) Where  companyId = '"
                                + companyid + "' and isnull(IsAccClose,'no')='no' and (('" + fromdate.ToString("yyyy-MM-dd")
                                + "' between fromdate and todate) or (fromdate <= '"
                                + fromdate.ToString("yyyy-MM-dd") + "' and todate is null )) "
                                + " and srno in ( select ledgerid from   ac_tds_sections a where CompanyId=" + companyid + " and a.SectionCodeFull='"
                                + SectionFullname + "') order by LedgerName";
                            dt = Database.GetDataTable(strsql);
                            if (dt.Rows.Count > 0)
                            {
                                LedgerName = Convert.ToString(dt.Rows[0][0]);
                            }
                            else
                            {
                                LedgerName = "TDS";
                                Database.OpenConnection(Utility.MaterialConnectionString);

                                if (companyname.Length == 0)
                                    companyname = Convert.ToString(Database.GetExecuteNonQueryCommand_retrun("Select Name from factoryinfo where srno=" + companyid));

                                if (isSales)
                                {
                                    Database.myreader = Database.GetExecuteReaderCommand("select taxledgername from exciselinkAccount with(NoLock) where type = 'Else' and parameter = 'TDS'" +
                                        " and   companyname = '" + companyname + "' and (('" + fromdate.ToString("yyyy-MM-dd") + "' between cast(datefrom  as date) and cast(dateto as date)) or ('" + fromdate + "'>=cast(datefrom  as date) and dateto is null) )");
                                    if (Database.myreader.Read())
                                    {
                                        LedgerName = Convert.ToString(Database.myreader[0]);
                                    }
                                }
                                else
                                {
                                    Database.myreader = Database.GetExecuteReaderCommand("select taxledgername from exciselinkAccount with(NoLock) where type = 'As Such' and parameter = 'TDS'" +
                                        " and   companyname = '" + companyname + "' and (('" + fromdate.ToString("yyyy-MM-dd") + "' between cast(datefrom  as date) and cast(dateto as date)) or ('" + fromdate + "'>=cast(datefrom  as date) and dateto is null) )");
                                    if (Database.myreader.Read())
                                    {
                                        LedgerName = Convert.ToString(Database.myreader[0]);
                                    }
                                }
                            }
                            //(string)Database.GetExecuteNonQueryCommand_retrun(strsql);
                            if (LedgerName.Length > 0)
                            {
                                bool blnfnd = false;
                                string PartyName = ledgername;
                                string rownu = Convert.ToString(TransId);
                                for (int i = rowIndex; i < dataGridView1.Rows.Count - 1; i++)
                                {

                                    string remaks = "";// Convert.ToString(remarks);
                                    if (!Isremarks)
                                    {
                                        if (!IsJV)
                                        {
                                            remaks = Convert.ToString(LedgerName);
                                            if (remaks.Contains(Convert.ToString(dataGridView1.Rows[i].Cells[0].Value)))
                                            {
                                                dataGridView1.Rows.RemoveAt(i);
                                                blnfnd = false;
                                                break;
                                            }
                                        }
                                        else
                                        {
                                            remaks = Convert.ToString(LedgerName);
                                            if (remaks.Contains(Convert.ToString(dataGridView1.Rows[i].Cells[1].Value)))
                                            {

                                                dataGridView1.Rows.RemoveAt(i);
                                                blnfnd = false;
                                                break;
                                            }
                                        }
                                    }
                                    else
                                    {

                                        if (!IsJV)
                                            remaks = Convert.ToString(dataGridView1.Rows[i].Cells[5].Value);
                                        else
                                            remaks = Convert.ToString(dataGridView1.Rows[i].Cells[7].Value);
                                        //remaks = Convert.ToString(remarks);
                                        if (remaks.Contains("TDS Applicable Amt : " + Amt + " for PartyName :" + PartyName + " and Row : " + rownu))
                                        {
                                            dataGridView1.Rows.RemoveAt(i);
                                            blnfnd = false;
                                            break;
                                        }
                                    }
                                }
                                int TCSApp = AccountCommonFunction.GetTCSApplicable(companyname, PartyName, fromdate);
                                if (TCSApp == 1)
                                {
                                    return;
                                }
                                int IntComp = AccountCommonFunction.GetIntercompany(companyname, ledgername, fromdate);
                                if (IntComp == 1 && blntcsapp)
                                {
                                    return;
                                }
                                if (IntComp == 0 && blntcsapp)
                                {
                                    return;
                                }
                                int IntUnit = AccountCommonFunction.GetInterUnit(companyname, ledgername, fromdate);
                                if (IntUnit == 1)
                                {
                                    return;
                                }

                                if (!blnfnd && Isremarks)
                                {
                                    if (!IsJV)
                                    {
                                        dataGridView1.Rows.Add(LedgerName, "Rs.", -1 * dblTDSAmt, 1, -1 * dblTDSAmt, "TDS Applicable Amt : " + Amt + " for PartyName :" + PartyName + " and Row : " + rownu, "");
                                    }
                                }
                                if (!blnfnd && !Isremarks)
                                {
                                    if (!IsJV)
                                    {
                                        dataGridView1.Rows.Add(LedgerName, dblTDSRate, "%", -1 * dblTDSAmt);
                                    }
                                }
                                if (!blnfnd && Isremarks)
                                {
                                    if (IsJV)
                                    {
                                        dataGridView1.Rows.Add("CR", LedgerName, "Rs.", dblTDSAmt, 1, 0, dblTDSAmt, "TDS Applicable Amt : " + Amt + " for PartyName :" + PartyName + " and Row : " + rownu + " TDS Rate:" + dblTDSRate);//
                                    }
                                }
                                if (!blnfnd && !Isremarks)
                                {
                                    if (IsJV)
                                    {
                                        dataGridView1.Rows.Add("CR", LedgerName, "Rs.", dblTDSAmt, 1, 0, dblTDSAmt, "TDS Applicable Amt : " + Amt + " for PartyName :" + PartyName + " and Row : " + rownu + " TDS Rate:" + dblTDSRate);//
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("LoadTDSLedger :" + ex.Message);
            }
        }


        /// <summary>
        /// Get list of Groups for ledgers in Accounts
        /// </summary>
        /// <param name="CompanyName">Company Name</param>
        /// <returns>DataTable</returns>
        public static DataTable GeCommontLedgerGroupList(string CompanyName)
        {
            DataTable dataTable = new DataTable("CommonLedgerGroup");
            if (Database.OpenConnection(Utility.MaterialConnectionString))
            {
                Database.GetExecuteReaderCommand(string.Format(@"SELECT GroupName, GroupId FROM (
SELECT ExpenseGroupHead AS GroupName, ExpenseGroupHead AS GroupId, Companyname FROM commoncashvoucherexpensegrouphead with(Nolock)
UNION ALL SELECT ExpenseHead AS GroupName, ExpenseHead AS GroupId, CompanyName FROM commonCashVoucherExpenseHead with(Nolock)
) AS g WHERE g.GroupName!='' AND g.Companyname='{0}' order by GroupName asc", CompanyName));
                dataTable.Load(Database.myreader);
            }
            return dataTable;
        }

        public static DataTable GeCommontGroupList(string CompanyName, string G3)
        {
            DataTable dataTable = new DataTable("CommonLedgerGroup");
            if (Database.OpenConnection(Utility.MaterialConnectionString))
            {
                Database.GetExecuteReaderCommand(string.Format(@"select ExpenseGroupHead AS GroupName, ExpenseGroupHead AS GroupId from commoncashvoucherexpensegrouphead where ExpenseHead ='{1}' and Companyname='{0}' order by GroupName asc", CompanyName, G3));
                dataTable.Load(Database.myreader);
                //if (dataTable.Rows.Count == 0)  //If no G4 then consider G3 as G4 2021.02.09
                dataTable.Rows.Add(G3, G3);
            }
            return dataTable;
        }
        public static DataTable GetCompanyGroupList(string CompanyName, string G3)
        {
            DataTable dataTable = new DataTable("LedgerGroup");
            if (Database.OpenConnection(Utility.MaterialConnectionString))
            {
                Database.GetExecuteReaderCommand(string.Format(@"select ExpenseGroupHead AS GroupName, ExpenseGroupHead AS GroupId from cashvoucherexpensegrouphead where ExpenseHead ='{1}' and Companyname='{0}' order by GroupName asc", CompanyName, G3));
                dataTable.Load(Database.myreader);
                //if (dataTable.Rows.Count == 0)  //If no G4 then consider G3 as G4 2021.02.09
                dataTable.Rows.Add(G3, G3);
            }
            return dataTable;
        }
        public static DataTable GetCommonLedgerGroupList(string CompanyName, string groupname)
        {
            DataTable dataTable = new DataTable("CommonLedgerGroup");
            if (Database.OpenConnection(Utility.MaterialConnectionString))
            {
                Database.GetExecuteReaderCommand(string.Format(@"SELECT GroupName, GroupId FROM (
SELECT ExpenseGroupHead AS GroupName, ExpenseGroupHead AS GroupId, Companyname FROM commoncashvoucherexpensegrouphead with(Nolock)
UNION ALL SELECT ExpenseHead AS GroupName, ExpenseHead AS GroupId, CompanyName FROM commonCashVoucherExpenseHead with(Nolock)
) AS g WHERE g.GroupName!='' AND g.Companyname='{0}'and g.GroupName='{1}'  order by GroupName asc", CompanyName, groupname));
                dataTable.Load(Database.myreader);
            }
            return dataTable;
        }
        /// <summary>
        /// Get List of Item Master 22.06.2020
        /// </summary>
        /// <returns>DataTable</returns>
        public static DataTable GetItemMaster(string dateTimePicker1, string companyname)
        {

            string strsql = "select distinct itemcode,Itemname,deptt,GroupName from MaterialProcessing.dbo.Item where companyname='" + companyname + "'"
                    + " and (('" + dateTimePicker1 + "' BETWEEN FROMDATE AND TODATE) OR (FROMDATE <='"
                    + dateTimePicker1 + "' AND TODATE IS NULL))  order by deptt,ItemName ";

            DataTable dataTable = new DataTable("GetItemMaster");
            if (Database.OpenConnection(Utility.MaterialConnectionString))
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
        public static DataTable GetIMainGrp()
        {
            string strsql = "select distinct DepttName from MaterialProcessing.dbo.ItemDeptt ";
            DataTable dataTable = new DataTable("GetItemMainGroup");
            if (Database.OpenConnection(Utility.MaterialConnectionString))
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

        public static void GetadvaceLic(System.Windows.Forms.ComboBox objcom, string companyname, string commodity)
        {
            try
            {
                objcom.Items.Clear();
                objcom.Text = "";
                if (Database.OpenConnection(Utility.DespacthConnectionString))
                {
                    string strsql = "";
                    strsql = "select advlicenseno from advlicense where companyname = '" + companyname + "' and isnull(isclose,0)=0 and " +
                        " commodity='" + commodity + "'"; // and setdefault = 'yes'  removed by manish on 8th Dec 2022

                    AccountCommonFunction.Loadcombo(objcom, strsql, "advlicenseno", "advlicenseno", Utility.DespacthConnectionString);
                    objcom.Items.Add("N.A.");
                    //objcom.Text = "N.A.";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        public static bool CheckDateBetweenYr(DateTime dt)
        {
            try
            {
                bool bln = false;
                using (System.Data.SqlClient.SqlConnection conn = new System.Data.SqlClient.SqlConnection(Utility.MaterialConnectionString))
                {
                    DateTime fromdate = Utility.nullDate;
                    DateTime todate = Utility.nullDate;
                    conn.Open();
                    string strsql = "Select * from yearMaster where cast('" + dt.ToString("yyyy-MM-dd") + "' as date) between fromdate and todate";
                    System.Data.SqlClient.SqlCommand command = new System.Data.SqlClient.SqlCommand(strsql, conn);
                    System.Data.SqlClient.SqlDataReader reader = command.ExecuteReader();
                    if (reader.Read())
                    {
                        fromdate = Convert.ToDateTime(reader["Fromdate"]);
                        todate = Convert.ToDateTime(reader["Todate"]);
                    }
                    reader.Close();
                    conn.Close();
                    if (dt.Date >= fromdate && dt.Date <= todate)
                        bln = true;
                }
                return bln;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public static int GetIntercompany(string companyname, string ledgername, DateTime dt)
        {
            int IntComp = 0;

            using (System.Data.SqlClient.SqlConnection conn = new System.Data.SqlClient.SqlConnection(Utility.MaterialConnectionString))
            {
                string s = string.Format(
                    "Select dbo.[fn_Get_Ledger_InterCompany]('{0}','{1}','{2}')", companyname, ledgername, dt.ToString("yyyy-MM-dd"));

                conn.Open();
                System.Data.SqlClient.SqlCommand command = new System.Data.SqlClient.SqlCommand(s, conn);
                System.Data.SqlClient.SqlDataReader reader = command.ExecuteReader();
                if (reader.Read())
                {
                    IntComp = Convert.ToInt32(reader[0]);
                }
            }
            return IntComp;
        }
        public static int GetTCSApplicable(string companyname, string ledgername, DateTime dt)
        {
            int TCSApp = 1;

            using (System.Data.SqlClient.SqlConnection conn = new System.Data.SqlClient.SqlConnection(Utility.MaterialConnectionString))
            {
                string s = string.Format(
                    "Select dbo.[fn_Get_IsTCSApp]('{0}','{1}','{2}')", companyname, ledgername, dt.ToString("yyyy-MM-dd"));

                conn.Open();
                System.Data.SqlClient.SqlCommand command = new System.Data.SqlClient.SqlCommand(s, conn);
                System.Data.SqlClient.SqlDataReader reader = command.ExecuteReader();
                if (reader.Read())
                {
                    TCSApp = Convert.ToInt32(reader[0]);
                }
            }
            return TCSApp;
        }
        public static int GetInterUnit(string companyname, string ledgername, DateTime dt)
        {
            int IntUnit = 0;

            using (System.Data.SqlClient.SqlConnection conn = new System.Data.SqlClient.SqlConnection(Utility.MaterialConnectionString))
            {
                string s = string.Format(
                    "Select dbo.[fn_Get_Ledger_InterUnit]('{0}','{1}','{2}')", companyname, ledgername, dt.ToString("yyyy-MM-dd"));

                conn.Open();
                System.Data.SqlClient.SqlCommand command = new System.Data.SqlClient.SqlCommand(s, conn);
                System.Data.SqlClient.SqlDataReader reader = command.ExecuteReader();
                if (reader.Read())
                {
                    IntUnit = Convert.ToInt32(reader[0]);
                }
            }
            return IntUnit;
        }
        ///<summaary>
        ///CSV Convert to DataTable
        /// </summaary>
        /// <returns>Datatable
        /// </returns>
        public static DataTable GetCsvFileToDatatable(string path, bool IsFirstRowHeader)
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
                using (OleDbConnection connection = new OleDbConnection(@"Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + pathOnly +
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
    }
}
