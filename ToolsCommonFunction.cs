using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ERP;
using System.Data;


namespace Tools
{
    /// <summary>
    /// Commont Functions for all modules
    /// </summary>
    public class ToolsCommonFunction
    {
        public static string DMSConnection = Utility.MaterialConnectionString;
        #region Safe Convert Function
        /// <summary>
        /// Safe convert an object to DateTime
        /// </summary>
        /// <param name="obj">Object Value</param>
        /// <returns>DateTime</returns>
        public static DateTime SafeConvertToDate(object obj)
        {
            if (obj is DateTime)
                return (DateTime)obj;

            if (obj is String)
            {
                DateTime d;
                DateTime.TryParse((String)obj, out d);
                return d;
            }

            return Utility.nullDate;
        }

        /// <summary>
        /// Safe convert an object to Integer value
        /// </summary>
        /// <param name="oValue">Object Value</param>
        /// <returns>Integer value</returns>
        public static int SafeConvertToInt(object oValue)
        {
            int iValue = 0;
            if (oValue is double) iValue = Convert.ToInt32(oValue);
            else if (oValue is int) iValue = (int)oValue;
            else if (oValue is float) iValue = (int)oValue;
            else if (oValue is decimal) iValue = Convert.ToInt32(oValue);
            else { try { iValue = Convert.ToInt32(oValue); } catch { }; }
            return iValue;
        }

        /// <summary>
        /// Safe convert an object to Double value
        /// </summary>
        /// <param name="oValue">Object Value</param>
        /// <returns>Double Value</returns>
        public static double SafeConvertToDouble(object oValue)
        {
            double iValue = 0;
            if (oValue is DBNull) return 0;
            else if (oValue == null) return 0;
            else if (oValue is double) iValue = (double)oValue;
            else if (oValue is int) iValue = Convert.ToDouble(oValue);
            else if (oValue is float) iValue = Convert.ToDouble(oValue);
            else if (oValue is decimal) iValue = Convert.ToDouble(oValue);
            else { try { iValue = Convert.ToDouble(oValue); } catch { }; }
            return iValue;
        }

        /// <summary>
        /// Safely convert value to bool
        /// <para>Currentyl this convert only (Bool, dbnull(false), string(1=true)</para>
        /// </summary>
        /// <param name="oValue">Object Value</param>
        /// <returns></returns>
        public static bool SafeConvertToBoolean(object oValue)
        {
            if (oValue is bool)
                return (bool)oValue;
            else if (oValue is string)
                return (string)oValue == "1";
            else if (oValue is DBNull)
                return false;

            return false;
        }

        #endregion

        /// <summary>
        /// Save user Log
        /// <para>Connection must be open</para>
        /// </summary>
        /// <param name="Operation">SAVE | UPDATE | DELETE</param>
        /// <param name="Remarks">Message</param>
        public static void SaveUserLog(string Operation, string Remarks)
        {
            Database.GetExecuteNonQueryCommand(string.Format(
                @"INSERT INTO loginentry..UserTask VALUES('{0}',GETDATE(),'{1}','{2}')", FrmMain.UserName, Operation, Remarks));
        }

        /// <summary>
        /// Save Record Log
        /// <para>This is new Table link with entry table which change tracking</para>
        /// </summary>
        /// <param name="RecordLogId">Existing Record Log Id (null for new)</param>
        /// <param name="UserName">UserName</param>
        /// <param name="Flag">
        /// Flag
        /// <para>DR - Draft</para>
        /// <para>DL - Deleted</para>
        /// <para>RG - Regular (Running Entry)</para>
        /// <remarks>Use this parameter using RecordLogFlag - </remarks><code>RecordLogFlag.Regular</code>
        /// </param>
        /// <param name="Remarks">Short Remarks for view few details of entry</param>
        /// <returns>Return RecordLogId</returns>
        public static int SaveRecordLog(int? RecordLogId, string UserName, string Flag, string Remarks)
        {
            RecordLogId = SafeConvertToInt(Database.ExecuteScalar("MaterialProcessing..sp_SaveRecordLog", System.Data.CommandType.StoredProcedure,
                new Database.Parameter("@RecordLogId", RecordLogId),
                new Database.Parameter("@UserName", UserName),
                new Database.Parameter("@Flag", Flag),
                new Database.Parameter("@Remarks", Remarks)
                ));

            return (int)RecordLogId;

            // old code

            //if (RecordLogId == null)
            //{
            //    // New Insert
            //    Database.ExecuteNonQuery("INSERT INTO MaterialProcessing..RecordLog (CreatedBy, ModifiedBy, Flag) VALUES (@CreatedBy, @ModifiedBy, @Flag)",
            //        new Database.Parameter("@CreatedBy", UserName),
            //        new Database.Parameter("@ModifiedBy", UserName),
            //        new Database.Parameter("@Flag", Flag)
            //        );

            //    RecordLogId = Database.GetInsertedId();
            //}
            //else
            //{
            //    // Update
            //    Database.ExecuteNonQuery("UPDATE MaterialProcessing..RecordLog SET ModifiedOn=GETDATE(), ModifiedBy=@ModifiedBy, Flag=@Flag WHERE RecordLogId=@RecordLogId",
            //        new Database.Parameter("@ModifiedBy", UserName),
            //        new Database.Parameter("@Flag", Flag),
            //        new Database.Parameter("@RecordLogId", RecordLogId)
            //        );
            //}

            //// Add Description
            //Database.ExecuteNonQuery("INSERT INTO MaterialProcessing..RecordLogHistory (RecordLogId, HistoryTime, HistoryUserName, HistoryDescription) VALUES (@RecordLogId, GETDATE(), @UserName, @Description)",
            //    new Database.Parameter("@RecordLogId", (int)RecordLogId),
            //    new Database.Parameter("@UserName", UserName),
            //    new Database.Parameter("@Description", Remarks)
            //    );

            //return (int)RecordLogId;
        }

        #region Load Combo Function
        /// <summary>
        /// Common function to call for Combobox using Datatable and Lambda Exo. for removing While loop.
        /// <param name="objcom">ComboBox to bind</param>
        /// <param name="strsql">Query string to get data from database</param>
        /// </summary>        
        public static void Loadcombo(System.Windows.Forms.ComboBox objcom, string strsql)
        {
            Loadcombo(objcom, strsql, "", "", "");
        }

        /// <summary>
        /// Common function to call for Combobox using Datatable and Lambda Exo. for removing While loop.
        /// <param name="objcom">ComboBox to bind</param>
        /// <param name="strsql">Query string to get data from database</param>
        /// <param name="DisClumn">DispalyMember Column Name</param>
        /// <param name="ValColumn">ValueMember Column Name</param>
        /// </summary>
        public static void Loadcombo(System.Windows.Forms.ComboBox objcom, string strsql, string DisClumn, string ValColumn)
        {
            Loadcombo(objcom, strsql, DisClumn, ValColumn, "");
        }

        /// <summary>
        /// Common function to call for Combobox using Datatable and Lambda Exo. for removing While loop.
        /// <param name="objcom">ComboBox to bind</param>
        /// <param name="strsql">Query string to get data from database</param>
        /// <param name="ConnectionString">Connection String</param>
        /// </summary>        
        public static void Loadcombo(System.Windows.Forms.ComboBox objcom, string strsql, string ConnectionString)
        {
            Loadcombo(objcom, strsql, "", "", ConnectionString);
        }

        /// <summary>
        /// Common function to call for Combobox using Datatable and Lambda Exo. for removing While loop.
        /// <param name="objcom">ComboBox to bind</param>
        /// <param name="strsql">Query string to get data from database</param>
        /// <param name="DisClumn">DispalyMember Column Name</param>
        /// <param name="ValColumn">ValueMember Column Name</param>
        /// <param name="ConnectionString">Connection String</param>
        /// </summary>        
        public static void Loadcombo(System.Windows.Forms.ComboBox objcom, string strsql, string DisClumn, string ValColumn, string ConnectionString)
        {
            if (ConnectionString.Length != 0)
                Database.OpenConnection(ConnectionString);

            DataTable dt = Database.GetDataTable(strsql);

            if (DisClumn.Length == 0)
                DisClumn = ValColumn = dt.Columns[0].ColumnName;

            if (DisClumn == ValColumn)
            {
                objcom.Items.Clear();

                string[] arrCombo;
                DataRow[] rows = dt.Select();
                arrCombo = Array.ConvertAll(rows, row => row[ValColumn].ToString());
                objcom.Items.AddRange(arrCombo);
                objcom.Text = string.Empty;
            }
            else
            {
                objcom.DataSource = dt;
                objcom.DisplayMember = DisClumn;
                objcom.ValueMember = ValColumn;
            }
        }

        #endregion

        /// <summary>
        /// Copy Combobox items to another compbo
        /// </summary>
        /// <param name="SourceCom">Combo from which copy all items</param>
        /// <param name="DestinationCom">Combo to which copy all items</param>
        public static void CopyCombo(System.Windows.Forms.ComboBox SourceCom, System.Windows.Forms.ComboBox DestinationCom)
        {
            object[] obj = new object[SourceCom.Items.Count];
            SourceCom.Items.CopyTo(obj, 0);
            DestinationCom.Items.AddRange(obj);
        }

        /// <summary>
        /// Set Crysal Report Viewer Report Source
        /// <para>This function close old document and clear cache before open new docuemnt</para>
        /// </summary>
        /// <param name="crv">Crystal Report Viewr</param>
        /// <param name="rc">Report Class Document</param>
        public static void CrystalReportSetSource(CrystalDecisions.Windows.Forms.CrystalReportViewer crv, CrystalDecisions.CrystalReports.Engine.ReportClass rc)
        {
            CrystalReportClose(crv);

            crv.Refresh();
            crv.ReportSource = rc;
        }

        /// <summary>
        /// Close Crystal Report Document
        /// </summary>
        /// <param name="crv">Crystal Report Viewer</param>
        public static void CrystalReportClose(CrystalDecisions.Windows.Forms.CrystalReportViewer crv)
        {
            if (crv.ReportSource != null)
            {
                (crv.ReportSource as CrystalDecisions.CrystalReports.Engine.ReportClass).Close();
            }
            crv.ReportSource = null;
        }

        /// <summary>
        /// Check Text Box has value or not
        /// </summary>
        /// <param name="objTextBox">name of Text box</param>
        /// <param name="strName">Infomatic name of Text box. use to display on exception</param>
        /// <returns>Exception</returns>
        public static bool CheckTextbox(System.Windows.Forms.TextBox objTextBox, string strName)
        {
            if (objTextBox.Text.Length == 0)
            {
                throw new ERP.ERPFieldException(strName);
            }
            else
                return true;
        }

        /// <summary>
        /// Check ComboBox has value or not
        /// </summary>
        /// <param name="objTextBox">name of ComboBox</param>
        /// <param name="strName">Infomatic name of Text box. use to display on exception</param>
        /// <returns>Exception</returns>
        public static bool CheckTextbox(System.Windows.Forms.ComboBox objTextBox, string strName)
        {
            if (objTextBox.Text.Length == 0)
            {
                throw new ERP.ERPFieldException(strName);
            }
            else
                return true;
        }
        /// <summary>
        /// Fist date of the month
        /// </summary>
        /// <param name="dt">datetime picker Control</param>
        public static void SetfirstDateOfMonth(System.Windows.Forms.DateTimePicker dt)
        {
            dt.Value = DateTime.Now.Date.AddDays(-(DateTime.Now.Day) + 1);
        }

        /// <summary>
        /// Get gridview cell values for checked rows 
        /// This fucntion can also used to check any raw of gridview is check or not.
        /// this will return string in comma seperated values 
        /// </summary>
        /// <param name="dg"> Dategrid view on which we want to perform the operation</param>
        /// <param name="ColumnHeaderName">Column header name </param>
        /// <param name="isStr">bool required single quote ' or not</param>
        /// <returns>Comma seperated value </returns>
        public static string GetValueofCellfromSelectedRows_Datagridview(ref System.Windows.Forms.DataGridView dg, string ColumnHeaderName, bool isStr)
        {
            string CellValue = "";
            if (isStr)
            {
                for (int i = 0; i < dg.Rows.Count; i++)
                {
                    if ((bool)dg[0, i].FormattedValue && dg[0, i].Visible == true)
                        CellValue += "','" + dg[ColumnHeaderName, i].FormattedValue.ToString();

                }
                CellValue += "'";
            }
            else
            {
                for (int i = 0; i < dg.Rows.Count; i++)
                {
                    if ((bool)dg[0, i].FormattedValue && dg[0, i].Visible == true)
                        CellValue += "," + dg[ColumnHeaderName, i].FormattedValue.ToString();

                }
            }
            if (CellValue.StartsWith(","))
                CellValue = CellValue.Substring(1, CellValue.Length - 1);
            return CellValue;

        }


        /// <summary>
        /// Check  or return true for selected row's first cell.
        /// to select check box of selected cell
        /// </summary>
        /// <param name="dg">Datagridview</param>
        ///<param name="columnIndex">Index of selected Column</param>
        public static void CheckSelectedRows_Datagridview(ref System.Windows.Forms.DataGridView dg, int columnIndex)
        {

            for (int i = 0; i < dg.Rows.Count; i++)
            {
                if (dg[columnIndex, i].Selected && dg[columnIndex, i].Visible == true)
                {
                    if (!(bool)dg[0, i].FormattedValue)
                        dg[0, i].Value = true;
                    else
                        dg[0, i].Value = false;
                }
            }
        }
        /// <summary>
        /// Right Aignment Todate gridview
        /// </summary>
        /// <param name="dg">Gridview</param>
        public static void setRightAignmentTodategridview(System.Windows.Forms.DataGridView dg)
        {
            if (dg.Rows.Count - 2 < 1) return;

            for (int i = 0; i < dg.Columns.Count; i++)
            {
                //if (dataGridView1.Columns[i].ValueType.Name.ToString().Contains("Decimal Double"))
                if (dg.Columns[i].ValueType == typeof(double) || dg.Columns[i].ValueType == typeof(float) || dg.Columns[i].ValueType == typeof(decimal))
                    dg.Columns[i].DefaultCellStyle = FormatStyle.NumberFormat(2, 3);



                if (Convert.ToString(dg[i, dg.Rows.Count - 2].FormattedValue).Equals("Total"))
                    dg.Rows[dg.Rows.Count - 2].DefaultCellStyle.BackColor = System.Drawing.Color.Green;
            }
        }
        ///<summary>
        ///ERP Date format Settings get from Table
        ///</summary>
        ///dateformat  value 
        public static string ERPDateSettings()
        {
            string strsql = "Select value from Loginentry..erp_setting where name='dateformat'";
            string returnvalue = "MM-dd-yyyy";
            DataTable dt = Database.GetDataTable(strsql);
            if (dt.Rows.Count > 0)
            {
                returnvalue = dt.Rows[0][0].ToString();
            }
            return returnvalue;
        }
        public static string ERPDBDateSettings()
        {
            string strsql = "Select dbDateFormat from Loginentry..erp_setting where name='dateformat'";
            string returnvalue = "yyyy-MM-dd";
            DataTable dt = Database.GetDataTable(strsql);
            if (dt.Rows.Count > 0)
            {
                returnvalue = dt.Rows[0][0].ToString();
            }
            return returnvalue;
        }
        public static DateTime StartYear(string year, string column)
        {
            string strsql = "select " + column + " from MaterialProcessing..yearmaster where yr='" + year + "' order by srno desc";
            DataTable dt = Database.GetDataTable(strsql);
            DateTime retDate = Convert.ToDateTime(null);

            if (dt.Rows.Count > 0)
            {
                retDate = Convert.ToDateTime(dt.Rows[0][0].ToString());
            }
            return retDate;
        }
        ///<summary>
        ///ERP Date format Settings get from Table
        ///</summary>
        ///dateformat  value 
        public static string ERPBackDateSettings()
        {
            object d = Database.ExecuteScalar("select back_days from erp_setting");
            int days = (int)d;
            DateTime dt = new DateTime();
            dt = DateTime.Now.AddDays(days);
            return dt.ToString("dd-MM-yyyy");
        }

        public static void QtyRateAmount(ref int qty, ref int rate, ref int amount)
        {
            string strsql = "Select name,value from Loginentry..erp_setting where name in('Qty','Rate','Amount') order by id";
            DataTable dt = Database.GetDataTable(strsql);
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                if (dt.Rows[0][0] == "QTY")
                    qty = Convert.ToInt32(dt.Rows[i][1]);
                if (dt.Rows[0][0] == "RATE")
                    rate = Convert.ToInt32(dt.Rows[i][1]);
                if (dt.Rows[0][0] == "AMOUNT")
                    amount = Convert.ToInt32(dt.Rows[i][1]);
            }
            //FrmMain.ERPQtyDigit = qty;
            //FrmMain.ERPRateDigit = rate;
            //FrmMain.ERPAmoutnDigit = amount;


        }




        public static bool ValidateGSTnumber(string GSTNo)
        {
            try
            {
                //24 AACCP 1688 G 1 Z F
                //12 34567 8901 2 3 4 5
                bool blnReturn = false;
                if (Database.OpenConnection(Utility.MaterialConnectionString))
                {
                    if (GSTNo == "N.A." || GSTNo == "UN-REGISTER")
                        blnReturn = true;

                    DataTable dt = new DataTable();
                    dt = Database.GetDataTable("exec Sp_GSTIN '" + GSTNo + "'");
                    if (dt.Rows.Count > 0)
                    {
                        blnReturn = true;
                    }
                }
                return blnReturn;

                //if (
                //    CheckIfNumeric(GSTNo.Substring(0, 2)) &&
                //    !CheckIfNumeric(GSTNo.Substring(2, 5)) &&
                //    CheckIfNumeric(GSTNo.Substring(7, 4)) &&
                //    !CheckIfNumeric(GSTNo.Substring(11, 1)) &&
                //    CheckIfNumeric(GSTNo.Substring(12, 1)) &&
                //    !CheckIfNumeric(GSTNo.Substring(13, 1))
                //    )

            }
            catch (Exception ex)
            {
                ex.Message.ToString();
                return false;
            }

        }
        private static bool CheckIfNumeric(string input)
        {
            if (System.Text.RegularExpressions.Regex.IsMatch(input, @"^\d+$"))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        

    
    }
}
