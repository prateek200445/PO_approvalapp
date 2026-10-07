using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Globalization;

using System.Data.OleDb;
using System.IO;
using System.Globalization;
using System.Collections;
using System.Reflection;


namespace ERP
{
    public partial class FrmGSTSummary : Form
    {
        AccountVoucherDetails avd;
        AccountVoucherDetails avd1;
        CultureInfo us = new CultureInfo("en-US");
        CultureInfo IN = new CultureInfo("en-IN");
        public FrmGSTSummary()
        {
            InitializeComponent();
            Form_Load();
        }

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
                else if (IsCSV && ext.ToLower() == "xlsx")
                {
                    DataTable dtResult = null;
                    int totalSheet = 0; //No of sheets on excel file  
                    using (OleDbConnection objConn = new OleDbConnection(
                    "Provider=Microsoft.ACE.OLEDB.12.0;Data Source='" + fileName +
                    "';Extended Properties='Excel 12.0 Xml;HDR=YES;IMEX=1;MAXSCANROWS=0'"))
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
                return dataTable;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());

            }
            finally
            {
                //return dataTable;
            }
            return dataTable;
        }
        private void Form_Load()
        {
            new filter.filterData(dgB2B);
            new filter.filterData(dgList);
            comboCompanyName.Items.Clear();
            AccountCommonFunction.BindComboCompanyMulti(comboCompanyName, true);
            comboCompanyName.Text = frmDefaultVale.CompanyName;
            //avd = new AccountVoucherDetails(dgList, 0, null, dateTimePicker1, dateTimePicker2, 3, 4, 5, 1, 2, 6, 7, -1, 2, 3);
            avd = new AccountVoucherDetails(dgList, 0, null, dateTimePicker1, dateTimePicker2, 1, 2, 4, 3, 9, 12, 12, 12, 12, 12);
            avd1 = new AccountVoucherDetails(dglist2, 0, null, dateTimePicker1, dateTimePicker2, 1, 2, 4, 3, 9, 12, 12, 12, 12, 12);

        }
        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                DataSet dt = new DataSet();
                DataSet dt2 = new DataSet();
                using (System.Data.SqlClient.SqlConnection conn = new System.Data.SqlClient.SqlConnection(Utility.MaterialConnectionString))
                {
                    conn.Open();

                    DataTable dttemptable = new DataTable();
                    DataTable dttemptable1 = new DataTable();
                    DataTable dttemptable2 = new DataTable();

                    DataTable dtMatchtable = new DataTable();
                    DataTable dtUnMatchtable = new DataTable();

                    DataTable dtUnMatchtableGovtData = new DataTable();


                    for (int C = 0; C <= comboCompanyName.CheckedItems.Count - 1; C++)
                    {
                        string strsql = "";
                        #region Regular GST Summary
                        strsql = "exec sp_Columner_All '" + comboCompanyName.CheckedItems[C].ToString() + "','" +
                            dateTimePicker1.Value.ToString("yyyy/MM/dd") + "','" + dateTimePicker2.Value.ToString("yyyy/MM/dd") + "'";
                        System.Data.SqlClient.SqlDataAdapter Adapter = new System.Data.SqlClient.SqlDataAdapter();
                        Adapter.SelectCommand = new System.Data.SqlClient.SqlCommand(strsql, conn);
                        Adapter.SelectCommand.CommandTimeout = 0;
                        dt = new DataSet();
                        Adapter.Fill(dt);
                        if (dt.Tables.Count > 0)
                        {
                            if (dttemptable.Rows.Count > 0)
                            {
                                dttemptable.Merge(dt.Tables[0]);
                                dttemptable.AcceptChanges();
                            }
                            else
                            {
                                dttemptable = dt.Tables[0].Copy();
                            }

                            if (dttemptable1.Rows.Count > 0)
                            {
                                dttemptable1.Merge(dt.Tables[1]);
                                dttemptable1.AcceptChanges();
                            }
                            else
                            {
                                dttemptable1 = dt.Tables[1].Copy();
                            }


                            if (dtMatchtable.Rows.Count > 0)
                            {
                                dtMatchtable.Merge(dt.Tables[2]);
                                dtMatchtable.AcceptChanges();
                            }
                            else
                            {
                                dtMatchtable  = dt.Tables[2].Copy();
                            }


                            if (dt.Tables.Count > 3)
                            {
                                if (dtUnMatchtable.Rows.Count > 0)
                                {
                                    dtUnMatchtable.Merge(dt.Tables[3]);
                                    dtUnMatchtable.AcceptChanges();
                                }
                                else
                                {
                                    dtUnMatchtable = dt.Tables[3].Copy();
                                }


                                if (dtUnMatchtableGovtData.Rows.Count > 0)
                                {
                                    dtUnMatchtableGovtData.Merge(dt.Tables[4]);
                                    dtUnMatchtableGovtData.AcceptChanges();
                                }
                                else
                                {
                                    dtUnMatchtableGovtData = dt.Tables[4].Copy();
                                }
                            }
                        }
                        #endregion

                        #region New GST Summary Report
                        strsql = "exec sp_Columner_All_New '" + comboCompanyName.CheckedItems[C].ToString() + "','" +
                            dateTimePicker1.Value.ToString("yyyy/MM/dd") + "','" + dateTimePicker2.Value.ToString("yyyy/MM/dd") + "'";
                        System.Data.SqlClient.SqlDataAdapter Adapter1 = new System.Data.SqlClient.SqlDataAdapter();
                        Adapter1.SelectCommand = new System.Data.SqlClient.SqlCommand(strsql, conn);
                        Adapter1.SelectCommand.CommandTimeout = 0;
                        dt2 = new DataSet();
                        Adapter1.Fill(dt2);
                        if (dt2.Tables.Count > 0)
                        {
                            if (dttemptable2.Rows.Count > 0)
                            {
                                dttemptable2.Merge(dt2.Tables[0]);
                                dttemptable2.AcceptChanges();
                            }
                            else
                            {
                                dttemptable2 = dt2.Tables[0].Copy();
                            }
                        }
                        #endregion

                    }


                    //dgMatch.Columns.Clear();
                    //dgMatch.DataSource = null;
                    //dgMatch.DataSource = dt.Tables[2];


                    //dgUnMatch.Columns.Clear();
                    //dgUnMatch.DataSource = null;
                    //dgUnMatch.DataSource = dt.Tables[3];

                    dt = new DataSet();
                    dt.Tables.Clear();
                    dt.Tables.Add(dttemptable);
                    dt.Tables.Add(dttemptable1);
                    dt.Tables.Add(dtMatchtable);
                    dt.Tables.Add(dtUnMatchtable);
                    dt.Tables.Add(dtUnMatchtableGovtData);


                    dt2 = new DataSet();
                    dt2.Tables.Add(dttemptable2);
                    //DataSet dtTempSet = Database.GetDataSet(strsql);

                    //dgB2B.Rows.Clear();
                    dgB2B.Columns.Clear();
                    dgB2B.DataSource = null;
                    dgB2B.DataSource = dt.Tables[1];

                    dgList.Columns.Clear();
                    dgList.DataSource = null;
                    dgList.DataSource = dt.Tables[0];

                    dglist2.Columns.Clear();
                    dglist2.DataSource = null;
                    dglist2.DataSource = dttemptable2;

                    dgMatch.Columns.Clear();
                    dgMatch.DataSource = null;
                    dgMatch.DataSource = dt.Tables[2];


                    dgUnMatch.Columns.Clear();
                    dgUnMatch.DataSource = null;
                    dgUnMatch.DataSource = dt.Tables[3];

                    dgUnMatchGovtData.Columns.Clear();
                    dgUnMatchGovtData.DataSource = null;
                    dgUnMatchGovtData.DataSource = dt.Tables[4];
                   
                    foreach (DataGridViewColumn col in this.dgB2B.Columns)
                    {
                        try
                        {
                            Convert.ToDouble(dgB2B[col.Index, 0].Value);
                            if (dgB2B[col.Index, 0].Value != null)
                            {
                                dgB2B.Columns[col.Index].DefaultCellStyle.Alignment = DataGridViewContentAlignment.BottomRight;
                                dgB2B.Columns[col.Index].DefaultCellStyle.FormatProvider = CultureInfo.GetCultureInfo("en-IN", IN.Name);
                                dgB2B.Columns[col.Index].DefaultCellStyle.Format = "N2";
                            }
                        }
                        catch (Exception ex)
                        {
                            ex.ToString();
                        }
                    }

                    foreach (DataGridViewColumn col in this.dgList.Columns)
                    {
                        try
                        {
                            Convert.ToDouble(dgList[col.Index, 0].Value);
                            if (dgList[col.Index, 0].Value != null)
                            {
                                dgList.Columns[col.Index].DefaultCellStyle.Alignment = DataGridViewContentAlignment.BottomRight;
                                dgList.Columns[col.Index].DefaultCellStyle.FormatProvider = CultureInfo.GetCultureInfo("en-IN", IN.Name);
                                dgList.Columns[col.Index].DefaultCellStyle.Format = "N2";
                            }
                        }
                        catch (Exception ex)
                        {
                            ex.ToString();
                        }
                    }

                    foreach (DataGridViewColumn col in this.dglist2.Columns)
                    {
                        try
                        {
                            Convert.ToDouble(dglist2[col.Index, 0].Value);
                            if (dglist2[col.Index, 0].Value != null)
                            {
                                dglist2.Columns[col.Index].DefaultCellStyle.Alignment = DataGridViewContentAlignment.BottomRight;
                                dglist2.Columns[col.Index].DefaultCellStyle.FormatProvider = CultureInfo.GetCultureInfo("en-IN", IN.Name);
                                dglist2.Columns[col.Index].DefaultCellStyle.Format = "N2";
                            }
                        }
                        catch (Exception ex)
                        {
                            ex.ToString();
                        }
                    }
                    //DataGridViewRow dr = dgB2B.Rows[dgB2B.Rows.Count - 1];
                    //for (int i = 2; i < dgB2B.ColumnCount; i++)
                    //{
                    //    //dr.DefaultCellStyle.BackColor = Color.RoyalBlue;
                    //    //dr.DefaultCellStyle.ForeColor = Color.White;
                    //    DataGridViewCellStyle cellstle = dr.Cells[i].Style;
                    //    cellstle.Alignment = DataGridViewContentAlignment.MiddleRight;
                    //    dgB2B.Columns[i].DefaultCellStyle = FormatStyle.AccountCurrencyFormat();
                    //}

                    //DataGridViewRow dr1 = dgList.Rows[dgList.Rows.Count - 1];
                    //for (int i = 15; i < dgList.ColumnCount; i++)
                    //{
                    //    //dr.DefaultCellStyle.BackColor = Color.RoyalBlue;
                    //    //dr.DefaultCellStyle.ForeColor = Color.White;
                    //    DataGridViewCellStyle cellstle = dr1.Cells[i].Style;
                    //    cellstle.Alignment = DataGridViewContentAlignment.MiddleRight;
                    //    dgList.Columns[i].DefaultCellStyle = FormatStyle.AccountCurrencyFormat();
                    //}

                }
            }
            catch (Exception EX)
            {
                MessageBox.Show(EX.Message);
            }
        }

        private void dgList_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void dgList_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                //OpenEntryForm(dgv.CurrentCell.RowIndex);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                OpenFileDialog fDialog = new OpenFileDialog();

                fDialog.Title = "Open CSV File";
                fDialog.Filter = "Excel File|*.csv";  //"Excel File|*.xls;*.xlsx;*.csv";//

                if (fDialog.ShowDialog() == DialogResult.OK)
                {
                    //  if(dataGridView1.Rows.Count > 0)
                    //  dataGridView1.Rows.Clear();

                    String path = fDialog.FileName;
                    // string connString = "";
                    string strFileType = Path.GetExtension(fDialog.FileName.ToString()).ToLower();


                    string CompanyGSTNo = "";

                    if (Database.OpenConnection(Utility.MaterialConnectionString))
                    {
                        Database.myreader = Database.GetExecuteReaderCommand("select newgstno from factoryinfo where name = '"+ comboCompanyName.Text+"'");
                        if(Database.myreader.Read())
                            CompanyGSTNo = Database.myreader[0].ToString();
                        Database.myreader.Close();
                    }
                    DataTable dt = new DataTable();

                    if (strFileType.Trim() == ".csv" || strFileType.Trim() == ".xlsx") //|| strFileType.Trim() == ".xlsx"
                    {
                        dt = GetCsvFileToDatatable(path, true);
                    }
                    dgB2B.DataSource = dt;

                    if (dt.Rows.Count > 0)
                    {
                        for (int i = 0; i < dt.Rows.Count; i++)
                        {
                            if (Database.OpenConnection(Utility.MaterialConnectionString))
                            {
                                bool Isavail = false;
                                Database.myreader = Database.GetExecuteReaderCommand("Select InvNo from GSTImportData where Invno='" +
                                       dt.Rows[i][1].ToString() + "' and InvDate = '" + dt.Rows[i][2].ToString() + "'");
                                if (Database.myreader.Read())
                                    Isavail = true;
                                Database.myreader.Close();
                                if (Isavail == false)
                                {
                                    DateTime dt1 = new DateTime();

                                    if (dt.Rows[i][1].ToString() != "")
                                    {

                                        if(dt.Rows[i][2].ToString() != "")
                                           dt1 = Convert.ToDateTime(dt.Rows[i][2].ToString());



                                        string st = "insert into GSTImportData values('" + CompanyGSTNo + "','" +
                                              dt.Rows[i][0].ToString() + "','" +
                                               dt.Rows[i][1].ToString() + "','"
                                             + dt1.ToString("yyyy-MM-dd") + "','"
                                              + dt.Rows[i][3].ToString() + "','"
                                              + dt.Rows[i][4].ToString() + "'," + dt.Rows[i][5].ToString() + ","
                                              + dt.Rows[i][6].ToString() + "," + dt.Rows[i][7].ToString() 
                                               + ")";
                                        Database.GetExecuteNonQueryCommand(st);
                                    }

                                }
                            }
                        }
                        MessageBox.Show("Saved Succesfully");
                        BindGrid();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void BindGrid()
        {
            if (Database.OpenConnection_Adapter(Utility.MaterialConnectionString))
            {
                Database.myadapter = Database.GetAdapterCommand("select * from GSTImportData");
                DataSet dataset1 = new DataSet();
                Database.myadapter.Fill(dataset1);
                dgB2B.DataSource = dataset1.Tables[0];
            }

           
        }

        private void label7_Click(object sender, EventArgs e)
        {
            DataTable dt = new DataTable();


            // add column from dg
            //dt.Columns.Add("GSTINNo");
            dt.Columns.Add("SupplierGSTNo");
            dt.Columns.Add("InvNo");
            dt.Columns.Add("InvDate");
            dt.Columns.Add("TwoA");
            dt.Columns.Add("TwoB");
            dt.Columns.Add("Basic");
            dt.Columns.Add("Igst");
            dt.Columns.Add("Gst");
            
            Export.ToFile(dt);
        }
    }
}
