using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Collections;
using System.Data.SqlClient;


namespace ERP
{
    public partial class frmChallan5A_Inwards : Form
    {
        double total = 0;

        bool IsUpdate = false;
        bool ischeked = false;
        string CompanyCode = "";
        public int TotalCount = 0;
        double Excise = 0;
        double Educess = 0;
        double HiEduCess = 0;
        double AddVat = 0;
        double Vat = 0;
        private string MRNNo = "";
        string IndentCode = "";
        public string JobWRKNo = "";

        public frmChallan5A_Inwards()
        {
            InitializeComponent();
            Form_Load();
        }
        public frmChallan5A_Inwards(string srno)
        {
            InitializeComponent();
            Form_Load();

            textSrNo.Text = srno;
            string ChallanCode = srno;
            if (ChallanCode.Length == 0) return;
            if (ChallanCode.Length != 0) ChangeMode(false);
            else ChangeMode(true);

        }
        public frmChallan5A_Inwards(string Customername, string Challanno, string companyName, string MRNNo, string inwardno)
        {
            try
            {
                InitializeComponent();
                this.MRNNo = MRNNo;
                Form_Load();
                //comboMRNO.Text = MRNNo;
                comboCompanyName.Text = companyName;

                string partyname = "";
                string[] challno = Challanno.Split(',');
                if (Database.OpenConnection(Utility.DespacthConnectionString))
                {
                    //for (int j = 0; j <= challno.Length - 1; j++)
                    //{
                    //    string strpartyname = Database.GetExecuteNonQueryCommand_retrun("select distinct CompNam from Challan5A with (NOLOCK) where challanNo='" +
                    //     challno[j].ToString() + "'").ToString();
                    //    if (!partyname.Contains(strpartyname))
                    //        partyname += strpartyname + ",";
                    //}
                    //if (partyname[partyname.Length - 1].ToString() == ",".ToString())
                    //    partyname = partyname.Remove(partyname.Length - 1).ToString();

                }
                comboBuyerName.Text = Customername;
                checkedListBox1.Items.Clear();
                //for (int i = 0; i <= checkedListBox1.Items.Count - 1; i++)

                for (int j = 0; j <= challno.Length - 1; j++)
                {
                    checkedListBox1.Items.Add(challno[j].ToString());
                    checkedListBox1.SetItemChecked(checkedListBox1.Items.Count - 1, true);
                    checkedListBox1.SetItemCheckState(checkedListBox1.Items.Count - 1, CheckState.Checked);
                }

                dataGridView1.Rows.Clear();

                if (Database.OpenConnection(Utility.DespacthConnectionString))
                {
                    for (int i = 0; i < checkedListBox1.CheckedItems.Count; i++)
                    {
                        ischeked = true;
                        Database.myreader = Database.GetExecuteReaderCommand("SELECT ChallanItem.DescGoods, ChallanItem.Qty, ChallanItem.Unit, Code, SubCode, PendingQty,case  when isnull(FGCommodity,'')='' then  NatureProc else FGCommodity end FGCommodity,ChallanItem.ItemCode,item.ItemName " +
                            " from Challan5A with (NOLOCK)  INNER JOIN  ChallanItem with (NOLOCK) ON ChallanNo=Code INNER JOIN MaterialProcessing.DBO.ITEM  with (NOLOCK) ON ITEM.ItemCode=ChallanItem.ItemCode AND ITEM.CompanyName=Challan5A.CompanyName " +
                            " WHERE Code='" + checkedListBox1.CheckedItems[i] + "'");
                        while (Database.myreader.Read())
                        {
                            int rIndex = dataGridView1.Rows.Count;
                            dataGridView1.Rows.Add();
                            dataGridView1.Rows[rIndex].Cells[ColumnDescOfGoods.Index].Value = Convert.ToString(Database.myreader[0]);
                            dataGridView1.Rows[rIndex].Cells[ColumnOrderQty.Index].Value = Convert.ToString(Database.myreader[1]);
                            dataGridView1.Rows[rIndex].Cells[ColumnPendingQty.Index].Value = Convert.ToString(Database.myreader["PendingQty"]);
                            dataGridView1.Rows[rIndex].Cells[ColumnUnit.Index].Value = Convert.ToString(Database.myreader[2]);
                            dataGridView1.Rows[rIndex].Cells[ColumnChallanSubCode.Index].Value = Convert.ToString(Database.myreader[3]) + "/" + Convert.ToString(Database.myreader["SubCode"]);
                            dataGridView1.Rows[rIndex].Cells[colProcessdesc.Index].Value = Convert.ToString(Database.myreader["FGCommodity"]);
                            dataGridView1.Rows[rIndex].Cells[ColChallnItem.Index].Value = Convert.ToString(Database.myreader["ItemName"]);
                            dataGridView1.Rows[rIndex].Cells[ColChnItemCode.Index].Value = Convert.ToString(Database.myreader["ItemCode"]);
                            dataGridView1.Rows[rIndex].Cells[MRNDataGridViewMultiColumnComboBoxColumn.Index].Value = inwardno;

                        }
                        Database.myreader.Close();
                    }
                    if (inwardno.Length > 0)
                    {
                        using (System.Data.SqlClient.SqlConnection conn = new System.Data.SqlClient.SqlConnection(Utility.MaterialConnectionString))
                        {
                            conn.Open();
                            string strsql = " select itemcode,itemname from StoreInwards  WHERE SrNo ='" + inwardno + "'";
                            System.Data.SqlClient.SqlDataAdapter Adapter = new System.Data.SqlClient.SqlDataAdapter();
                            Adapter.SelectCommand = new System.Data.SqlClient.SqlCommand(strsql, conn);
                            DataTable dt = new DataTable();
                            Adapter.Fill(dt);
                            ColMRNItemName.DataSource = null;
                            ColMRNItemName.DataSource = dt;
                            ColMRNItemName.DisplayMember = "itemName";
                            ColMRNItemName.ValueMember = "Itemcode";
                            conn.Close();
                        }
                    }

                }
                checkedListBox1.Enabled = false;
                comboBuyerName.Enabled = false;
                btnAdd.Visible = true;
            }
            catch (Exception ex)
            {
                ex.Message.ToString();
            }

        }
        private void Form_Load()
        {
            if (Database.OpenConnection(Utility.DespacthConnectionString))
            {
                DespatchCommonFunction.GetCompanyList(comboCompanyName);
                comboCompanyName.Text = frmDefaultVale.CompanyName;
                DespatchCommonFunction.GetYear(comboSuffix, Utility.DespacthConnectionString);
                if (comboSuffix.Items.Count > 0) comboSuffix.SelectedIndex = 0;
            }

            bindComboBuyer();
            bindMRNO(false);
        }

        private void frmChallan5A_Load(object sender, EventArgs e)
        {
            //Form_Load();
        }

        private void bindComboBuyer()
        {
            comboBuyerName.Items.Clear();
            if (Database.OpenConnection(Utility.DespacthConnectionString))
            {
                string strsql = "";
                strsql = "Select DISTINCT(CompNam) as CompNam from Challan5A WITH(NOLOCK) WHERE CompanyName='" + comboCompanyName.Text + "' ORDER BY CompNam";
                DespatchCommonFunction.Loadcombo(comboBuyerName, strsql, "CompNam", "CompNam", Utility.DespacthConnectionString);
                comboBuyerName.SelectedIndex = -1;
            }
        }

        private void bindMRNO(bool isSaveMode)
        {

            using (System.Data.SqlClient.SqlConnection conn = new System.Data.SqlClient.SqlConnection(Utility.MaterialConnectionString))
            {
                conn.Open();

                if (comboSuffix.Text.Length == 0) return;
                if (comboBuyerName.Text.Length == 0) return;
                string strsql = "";
                if (isSaveMode)
                {
                    strsql = "SELECT MRNo   ,SrNo, ChallanNo,AcceptedQty Qty FROM MaterialProcessing..Vw_StoreInwards WITH(NOLOCK) WHERE SRNO IN (" +
                            " select distinct SrNo from StoreInwardsPayment WHERE isnull(Cancel ,'')='' and CompanyName='" + comboCompanyName.Text + "' AND Yr='" + comboSuffix.Text +
                            "' AND (PartyName='" + comboBuyerName.Text + "' or VendorGST='" + lblgst.Text + "') AND MRNo like 'JBIN-SE%'  " +
                            (this.MRNNo.Length > 0 ? " AND MRNo='" + this.MRNNo + "'" : "") + " ) and isnull(Cancel ,'')='' order by SrNo";
                }
                else
                {
                    //strsql = "SELECT MRNo,SrNo, ChallanNo,AcceptedQty Qty FROM MaterialProcessing..Vw_StoreInwards WITH(NOLOCK) WHERE SRNO IN (" +
                    //        " select distinct SrNo from StoreInwardsPayment WHERE CompanyName='" + comboCompanyName.Text + "' AND Yr='" + comboSuffix.Text +
                    //        "' AND (PartyName='" + comboBuyerName.Text + "' or VendorGST='" + lblgst.Text + "') AND MRNo like 'JBIN-SE%'" +
                    //        " EXCEPT select distinct inwardno from Challan5AInward where isnull(inwardno,'')<>'' and CompanyName='" + comboCompanyName.Text +
                    //        "' AND Yr='" + comboSuffix.Text + "') order by SrNo";

                    strsql = "select MRNo   ,SrNo, ChallanNo,round(sum(AcceptedQty)-isnull(ChaIwdQ,0),3) as Pending from Vw_StoreInwards " +
                            " left join ( " +
                            " select distinct inwardno,isnull(sum(AcceptedQty),0) ChaIwdQ from Challan5AInward " +
                            " where isnull(inwardno,'')<>'' and CompanyName='" + comboCompanyName.Text + "' AND Yr='" + comboSuffix.Text + "'   group by inwardno" +
                            " ) CI on CI.InwardNo=SrNo" +
                            " WHERE CompanyName='" + comboCompanyName.Text + "' " +
                            " AND Yr='" + comboSuffix.Text + "' AND (PartyName='" + comboBuyerName.Text + "' or VendorGST='" + lblgst.Text + "') AND MRNo like 'JBIN-SE%' and isnull(Cancel ,'')=''  " +
                            (this.MRNNo.Length > 0 ? " AND MRNo='" + this.MRNNo + "'" : "") +
                            " group by SrNo,MRNo, ChallanNo,ChaIwdQ " +
                            " having round(sum(AcceptedQty)-isnull(ChaIwdQ ,0),3) >0 order by SrNo";

                }

                System.Data.SqlClient.SqlDataAdapter Adapter = new System.Data.SqlClient.SqlDataAdapter();
                Adapter.SelectCommand = new System.Data.SqlClient.SqlCommand(strsql, conn);
                DataTable dt = new DataTable();
                Adapter.Fill(dt);
                if (dt.Rows.Count == 0)
                {
                    if (isSaveMode)
                    {
                        strsql = "select MRNo  , SrNo,ChallanNo,ItemName ,AcceptedQty Qty from Vw_StoreInwards where isnull(Cancel ,'')='' and srno in (select  SrNo   " +
                        " from StoreInwardsPayment where   isnull(Cancel ,'')='' and companyname='" + comboCompanyName.Text + "' and yr='" + comboSuffix.Text + "'" +
                        " and  category='job in' and SubCategory='Self' " + (this.MRNNo.Length > 0 ? " AND MRNo='" + this.MRNNo + "'" : "") +
                        " ) order by  srno  ";

                        Adapter.SelectCommand = new System.Data.SqlClient.SqlCommand(strsql, conn);
                        dt = new DataTable();
                        Adapter.Fill(dt);

                    }
                    else
                    {
                        strsql = "select MRNo  , SrNo,ChallanNo,ItemName ,AcceptedQty Qty from Vw_StoreInwards where isnull(Cancel ,'')='' and srno in (select  SrNo   " +
                        " from StoreInwardsPayment where isnull(Cancel ,'')='' and companyname='" + comboCompanyName.Text + "' and yr='" + comboSuffix.Text + "'" +
                        " and  category='job in' and SubCategory='Self' " +
                        " EXCEPT select distinct inwardno from Challan5AInward where isnull(inwardno,'')<>'' and CompanyName='" + comboCompanyName.Text + "'" +
                        " AND Yr='" + comboSuffix.Text + "' " + (this.MRNNo.Length > 0 ? " AND MRNo='" + this.MRNNo + "'" : "") + " ) order by SrNo ";

                        Adapter.SelectCommand = new System.Data.SqlClient.SqlCommand(strsql, conn);
                        dt = new DataTable();
                        Adapter.Fill(dt);
                    }
                }
                MRNDataGridViewMultiColumnComboBoxColumn.DataSource = null;
                MRNDataGridViewMultiColumnComboBoxColumn.DataSource = dt;
                MRNDataGridViewMultiColumnComboBoxColumn.DisplayMember = "MrNo";
                MRNDataGridViewMultiColumnComboBoxColumn.ValueMember = "srno";

                Database.Closeconnection();
            }

            if (comboSuffix.Text.Length == 0 || comboCompanyName.Text.Length == 0 || comboBuyerName.Text.Length == 0) return;
            if (isSaveMode)
            {
                using (System.Data.SqlClient.SqlConnection conn = new System.Data.SqlClient.SqlConnection(Utility.MaterialConnectionString))
                {
                    conn.Open();
                    System.Data.SqlClient.SqlDataAdapter Adapter = new System.Data.SqlClient.SqlDataAdapter();
                    Adapter.SelectCommand = new System.Data.SqlClient.SqlCommand("Select distinct Itemcode,ItemName from Vw_StoreInwards where companyname='" + comboCompanyName.Text + "' and  MRNo like '%JBIN-SE%'", conn);
                    DataTable dt1 = new DataTable();
                    Adapter.Fill(dt1);
                    ColMRNItemName.DataSource = null;
                    ColMRNItemName.DataSource = dt1;
                    ColMRNItemName.DisplayMember = "ItemName";
                    ColMRNItemName.ValueMember = "ItemCode";
                }
            }


            //comboMRNO.Items.Clear();
            //if (Database.OpenConnection(Utility.DespacthConnectionString))
            //{
            //    string strsql = "";
            //    strsql = "SELECT MRNo FROM MaterialProcessing..Vw_StoreInwards WITH(NOLOCK) WHERE " +
            //            " CompanyName='" + comboCompanyName.Text + "' AND Yr='" + comboSuffix.Text +
            //            "' AND (PartyName='" + comboBuyerName.Text + "' or VendorGST='" + lblgst.Text + "') AND MRNo like 'JBIN-SE%'  order by SrNo";
            //    DespatchCommonFunction.Loadcombo(comboMRNO, strsql, "MRNo", "MRNo", Utility.DespacthConnectionString);
            //    comboMRNO.SelectedIndex = -1;
            //    if (comboMRNO.Items.Count > 0)
            //        comboMRNO.SelectedIndex = 0;
            //}
        }

        private void button2_Click(object sender, EventArgs e)
        {

            dataGridView1.Rows.Clear();
            checkedListBox1.Items.Clear();
            while (checkedListBox1.CheckedItems.Count > 0)
            {
                checkedListBox1.SetItemChecked(checkedListBox1.CheckedIndices[0], false);
            }
            IsUpdate = false;
            btnOK.Enabled = true;
            btnUpdate.Enabled = false;
            textSrNo.Text = "";
            btnAdd.Visible = false;
            ChangeMode(true);
        }

        public void IncreaseValueByone(ref string value, int Startindex)
        {
            string frontstring = value.Substring(0, Startindex);
            string numericstring = value.Substring(Startindex, 3);
            string format = "";
            int i = Convert.ToInt32(numericstring);
            i = i + 1;
            if (i < 10)
                format = "00";
            else if (i < 100)
                format = "0";
            value = frontstring + format + i.ToString();
        }


        private string GenerateChallanCode()
        {
            try
            {
                string PrefixCode = IndentCode + "/" + comboSuffix.Text + "/JOBWORK/";
                int IncreaseValue = 0;
                ArrayList arrpur = new ArrayList();
                int MaxValue = 0;
                if (Database.OpenConnection(Utility.DespacthConnectionString))
                {

                    string strsql = "";
                    strsql = "select isnull(max(cast(replace(challanno ,'" + PrefixCode + "','') as int)),0)   from Challan5a WITH(NOLOCK) where challanno like '" + PrefixCode + "%' ";
                    Database.myreader = Database.GetExecuteReaderCommand(strsql);
                    if (Database.myreader.Read())
                        MaxValue = Convert.ToInt16(Database.myreader[0].ToString());
                    Database.myreader.Close();

                    //Database.myreader = Database.GetExecuteReaderCommand("select challanno from Challan5a WITH(NOLOCK) where challanno like'"
                    //         + PrefixCode + "%'");
                    //while (Database.myreader.Read())
                    //    arrpur.Add(Database.myreader[0].ToString());
                    //Database.myreader.Close();

                    //ArrayList arr = new ArrayList();

                    //for (int i = 0; i < arrpur.Count; i++)
                    //    arr.Add(arrpur[i].ToString().Substring(arrpur[i].ToString().LastIndexOf('/') + 1));

                    //for (int i = 0; i < arr.Count; i++)
                    //{
                    //    if (MaxValue < Convert.ToInt32(arr[i]))
                    //        MaxValue = Convert.ToInt32(arr[i]);
                    //}
                }
                if (MaxValue == 0)
                    IncreaseValue = 1;
                else
                    IncreaseValue = MaxValue + 1;
                PrefixCode = PrefixCode + IncreaseValue.ToString();

                return PrefixCode;
            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
                return "";
            }
        }

        private string GetCompanyCode()
        {
            try
            {
                string CompanyCode = "";
                if (Database.OpenConnection(Utility.DespacthConnectionString))
                {
                    Database.myreader = Database.GetExecuteReaderCommand("select CompanyCode from CompanyMaster WITH(NOLOCK) where CompanyName = '"
                                     + comboBuyerName.Text + "'");
                    while (Database.myreader.Read())
                        CompanyCode = Database.myreader[0].ToString();
                    Database.myreader.Close();
                    //  Database.Closeconnection();
                }
                return CompanyCode;
            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
                return "";
            }
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            if (comboBuyerName.Text.Length == 0)
            {
                MessageBox.Show("Please enter any value in Processer Name");
                return;
            }

            string srNo = "";
            if (Database.OpenConnection(Utility.MaterialConnectionString))
            {
                #region 19-08-2020 lock as per accounting voucher type
                if (!Utility.CheckAccountVoucherDateFreeze(dateTimePicker1.Value, comboCompanyName.Text, "sales"))
                {
                    MessageBox.Show(AccountCommonVariable.ErrMsg.DATE_FREEZE + "\nPlease check Date");
                    return;
                }
                #endregion

                for (int i = 0; i < dataGridView1.Rows.Count; i++)
                {
                    if (!Convert.ToBoolean(dataGridView1.Rows[i].Cells["ColumnCheck"].Value)) continue;
                    if (Convert.ToString(dataGridView1.Rows[i].Cells[MRNDataGridViewMultiColumnComboBoxColumn.Index].Value) == "")
                    {
                        MessageBox.Show("Please Select MRN No for Seleted Commodity");
                        return;
                    }
                }

                #region validation for acceptedqty cannot be greater than orderqty code made by manish on 12th April 2024

                //for (int i = 0; i < dataGridView1.Rows.Count; i++)
                //{
                //    if (Convert.ToBoolean(dataGridView1.Rows[i].Cells["ColumnCheck"].Value))
                //    {
                //        string Challansubcode = dataGridView1.Rows[i].Cells[11].Value.ToString();
                //        double Acceptedqty = Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[7].Value.ToString());
                //        double OrderQty = Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[4].Value.ToString());
                //        double AlreadyInwardedQty = 0;

                //        if (Database.OpenConnection(Utility.MaterialConnectionString))
                //        {
                //            Database.myreader = Database.GetExecuteReaderCommand(" select sum(AcceptedQty) from Challan5AInward where ChallanSubCode = '"
                //                + Challansubcode + "'");
                //            if (Database.myreader.Read())
                //                AlreadyInwardedQty = Utility.SafeConvertToDouble(Database.myreader[0].ToString());
                //            Database.myreader.Close();

                //            if (OrderQty > AlreadyInwardedQty + Acceptedqty)
                //            {
                //                MessageBox.Show("Accepted Qty is more than Order Qty");
                //                return;
                //            }
                //        }
                //    }
                //}


                #endregion

                #region check for logic if added new line by user 20.12.2021

                DataTable dtMrn = new DataTable();
                dtMrn.Columns.Add("MRNNo");
                dtMrn.Columns.Add("MRNItemcode");
                dtMrn.Columns.Add("MRNQty");
                dtMrn.Columns.Add("AccQty");


                for (int i = 0; i < dataGridView1.Rows.Count; i++)
                {
                    if (!Convert.ToBoolean(dataGridView1.Rows[i].Cells["ColumnCheck"].Value)) continue;
                    DataRow row = dtMrn.NewRow();
                    row["MRNNo"] = Convert.ToString(dataGridView1.Rows[i].Cells[MRNDataGridViewMultiColumnComboBoxColumn.Index].Value);
                    row["MRNItemcode"] = Convert.ToString(dataGridView1.Rows[i].Cells[ColMRNItemName.Index].Value);
                    row["MRNQty"] = Convert.ToString(dataGridView1.Rows[i].Cells[ColMRNQty.Index].Value);
                    row["AccQty"] = Convert.ToString(dataGridView1.Rows[i].Cells[ColumnAcceptedQty.Index].Value);
                    dtMrn.Rows.Add(row);
                }

                DataView view = dtMrn.DefaultView;
                DataTable distinctValues = view.ToTable(true, "MRNNo", "MRNItemcode", "MRNQty");

                double dblChk = 0;

                for (int i = 0; i < distinctValues.Rows.Count; i++)
                {
                    double MRNQty = Utility.SafeConvertToDouble(distinctValues.Rows[i]["MRNQty"]);
                    dtMrn.DefaultView.RowFilter = "MRNNo='" + distinctValues.Rows[i]["MRNNO"] + "' " +
                        " and MRNItemcode='" + distinctValues.Rows[i]["MRNItemcode"] + "'" +
                        " and MRNQty='" + distinctValues.Rows[i]["MRNQty"] + "'";

                    DataTable final = dtMrn.DefaultView.ToTable();
                    dblChk = 0;
                    for (int j = 0; j < final.Rows.Count; j++)
                    {
                        dblChk += Utility.SafeConvertToDouble(final.Rows[j]["AccQty"]);
                    }
                    if (Math.Round(MRNQty - dblChk, FrmMain.ERPQtyDigit) != 0)
                    {
                        MessageBox.Show("Please check with MRN and Accepted Qty.\nAccepted Qty is more than MRN Qty :" +
                            Math.Round(MRNQty - dblChk, FrmMain.ERPQtyDigit).ToString());
                        return;
                    }
                }
                #endregion
                int sNo = 1;
                object oSrNo = Database.GetExecuteScalarCommand_return("select top 1 srNo from Challan5AInward WITH(NOLOCK) where companyname='" + comboCompanyName.Text + "' and yr='" + comboSuffix.Text + "' order by transId desc");
                if (oSrNo != null)
                {
                    string sSrNo = Convert.ToString(oSrNo);
                    sNo = Convert.ToInt32(sSrNo.Substring(sSrNo.Length - 5, 5));
                    sNo++;
                }

                object oIndentCode = Database.GetExecuteScalarCommand_return("select indentcode from FactoryInfo WITH(NOLOCK) where Name = '" + comboCompanyName.Text + "'");
                if (oIndentCode == null) return;
                srNo = oIndentCode.ToString() + "/" + comboSuffix.Text + "/JOB" + sNo.ToString("00000");


                string ChallanNo = "";
                for (int i = 0; i < checkedListBox1.CheckedItems.Count; i++)
                {
                    if (i != 0) ChallanNo += ",";
                    ChallanNo += checkedListBox1.CheckedItems[i].ToString();
                }

                try
                {
                    Database.BeginTransaction();
                    for (int i = 0; i < dataGridView1.Rows.Count; i++)
                    {
                        if (!Convert.ToBoolean(dataGridView1.Rows[i].Cells["ColumnCheck"].Value)) continue;
                        SqlParameter[] p = new SqlParameter[]{
                            new SqlParameter("@Serial",'0'),
                            new SqlParameter("@companyname", comboCompanyName.Text),
                            new SqlParameter("@sysdate", dateTimePicker1.Value.ToString("yyyy-MM-dd")),
                            new SqlParameter("@SrNO", srNo),
                            new SqlParameter("@yr", comboSuffix.Text),
                            new SqlParameter("@BuyerName", comboBuyerName.Text),
                            new SqlParameter("@CombineChallanNo", ChallanNo),
                            new SqlParameter("@totalQty", '0'),
                            new SqlParameter("@transportername", ""),
                            new SqlParameter("@vehicalno", ""),
                            new SqlParameter("@Remarks", Convert.ToString(dataGridView1.Rows[i].Cells["ColumnRemarks"].FormattedValue)),
                            new SqlParameter("@mrno", Convert.ToString(dataGridView1.Rows[i].Cells[MRNDataGridViewMultiColumnComboBoxColumn.Index].FormattedValue)),
                            new SqlParameter("@DescOfGoods", Convert.ToString(dataGridView1.Rows[i].Cells["ColumnDescOfGoods"].FormattedValue)),
                            new SqlParameter("@Unit", Convert.ToString(dataGridView1.Rows[i].Cells["ColumnUnit"].FormattedValue)),
                            new SqlParameter("@OrderQty", Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells["ColumnOrderQty"].FormattedValue.ToString())),
                            new SqlParameter("@AcceptedQty", Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells["ColumnAcceptedQty"].FormattedValue.ToString())),
                            new SqlParameter("@ChallanSubCode", Convert.ToString(dataGridView1.Rows[i].Cells["ColumnChallanSubCode"].FormattedValue)),
                            new SqlParameter("@InwardNo", Convert.ToString(dataGridView1.Rows[i].Cells[MRNDataGridViewMultiColumnComboBoxColumn.Index].Value)),
                            new SqlParameter("@Itemcode", Convert.ToString(dataGridView1.Rows[i].Cells[ColMRNItemName.Index].Value)),
                            //Change By Rishabh on 07 July 2026 for ITC 5A (GST) report requirement
                            new SqlParameter("@subsidiary_challan_no", Convert.ToString(dataGridView1.Rows[i].Cells["subsidiary_challan_no"].FormattedValue)),
                            new SqlParameter("@subsidiary_challan_dt", Convert.ToDateTime(dataGridView1.Rows[i].Cells["subsidiary_challan_dt"].Value)),
                            //Change End
                        };

                        using (SqlCommand cmd = new SqlCommand("SP_InsertUpdate_Challan5AInward", Database.myconn, Database.mytransaction))
                        {
                            cmd.Parameters.AddRange(p);
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.ExecuteNonQuery();
                        }
                        double pQty = Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells["ColumnPendingQty"].FormattedValue.ToString()) - Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells["ColumnAcceptedQty"].FormattedValue.ToString());

                        DateTime dtinwarddate = new DateTime();

                        Database.GetExecuteNonQueryCommand("UPDATE Despatch..ChallanItem SET PendingQty=" + pQty + " WHERE (Code+'/'+LTRIM(STR(SubCode))='" + Convert.ToString(dataGridView1.Rows[i].Cells["ColumnChallanSubCode"].FormattedValue) + "')");
                       
                        // change date in challan 5a inward  added by manish on 7th Sep 2022
                        Database.myreader = Database.GetExecuteReaderCommand("select sysdate from storeinwardspayment where srno = '" + Convert.ToString(dataGridView1.Rows[i].Cells[MRNDataGridViewMultiColumnComboBoxColumn.Index].Value) + "'");
                        if(Database.myreader.Read())
                              dtinwarddate = Convert.ToDateTime(Database.myreader[0].ToString());
                        Database.myreader.Close();
                        Database.GetExecuteNonQueryCommand("UPDATE challan5ainward set sysdate = '" + dtinwarddate + "' where inwardno = '" + Convert.ToString(dataGridView1.Rows[i].Cells[MRNDataGridViewMultiColumnComboBoxColumn.Index].Value) + "'");
                        /// end
                    }
                    JobWRKNo = srNo;
                    Database.CommitTransaction();
                    Utility.UserInformation("Save", "Job Work Inward Challan No : " + srNo.ToString() + " is saved successfully"); //07.01.2022
                    MessageBox.Show("Data Saved with Job Work Inward Challan No :" + srNo);
                    if (this.MRNNo.Length > 0)
                        this.Close();
                }
                catch (Exception ex)
                {
                    Database.RollBackTransaction();
                    ErrorMessageBox.Show(ex);
                }
                finally { }

                dataGridView1.Rows.Clear();
                while (checkedListBox1.CheckedItems.Count > 0) checkedListBox1.SetItemChecked(checkedListBox1.CheckedIndices[0], false);
            }
        }



        private void btnUpdate_Click(object sender, EventArgs e)
        {
            if (!FrmMain.IsUpdateJobWork)
                MessageBox.Show("you have no right to update Job work");
            else
            {
                #region 19-08-2020 lock as per accounting voucher type
                if (!Utility.CheckAccountVoucherDateFreeze(dateTimePicker1.Value, comboCompanyName.Text, "sales"))
                {
                    MessageBox.Show(AccountCommonVariable.ErrMsg.DATE_FREEZE + "\nPlease check Date");
                    return;
                }
                #endregion

                for (int i = 0; i < dataGridView1.Rows.Count; i++)
                {
                    if (!Convert.ToBoolean(dataGridView1.Rows[i].Cells["ColumnCheck"].Value)) continue;
                    if (Convert.ToString(dataGridView1.Rows[i].Cells[MRNDataGridViewMultiColumnComboBoxColumn.Index].Value) == "")
                    {
                        MessageBox.Show("Please Select MRN No for Seleted Commodity");
                        return;
                    }
                }
                string srNo = textSrNo.Text;
                if (Database.OpenConnection(Utility.MaterialConnectionString))
                {
                    try
                    {
                        Database.BeginTransaction();
                        for (int i = 0; i < dataGridView1.Rows.Count; i++)
                        {
                            string query = "UPDATE Challan5AInward SET " +
                                "Mrno='" + Convert.ToString(dataGridView1.Rows[i].Cells[MRNDataGridViewMultiColumnComboBoxColumn.Index].FormattedValue) + "', " +
                                "InwardNo ='" + Convert.ToString(dataGridView1.Rows[i].Cells[MRNDataGridViewMultiColumnComboBoxColumn.Index].Value) + "'," +
                                "AcceptedQty=" + Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells["ColumnAcceptedQty"].FormattedValue.ToString()) + ", " +
                                "Remarks='" + Convert.ToString(dataGridView1.Rows[i].Cells["ColumnRemarks"].FormattedValue) +
                                "',Itemcode='" + Convert.ToString(dataGridView1.Rows[i].Cells[ColMRNItemName.Index].Value) +
                                "',sysdate='" + dateTimePicker1.Value.ToString("yyyy-MM-dd") 
                                + "' WHERE SrNo='" + srNo + "' and challansubcode = '" 
                                +  Convert.ToString(dataGridView1.Rows[i].Cells["ColumnChallanSubCode"].FormattedValue) + "'";

                            Database.GetExecuteNonQueryCommand(query);

                            double pQty = Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells["ColumnPendingQty"].FormattedValue.ToString()) - Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells["ColumnAcceptedQty"].FormattedValue.ToString());

                            Database.GetExecuteNonQueryCommand("UPDATE Despatch..ChallanItem SET PendingQty=" + pQty + " WHERE (Code+'/'+LTRIM(STR(SubCode))='" + Convert.ToString(dataGridView1.Rows[i].Cells["ColumnChallanSubCode"].FormattedValue) + "')");
                        }

                        Database.CommitTransaction();
                        Utility.UserInformation("Update", "Job Work Inward Challan No : " + srNo.ToString() + " is Updated successfully");
                        MessageBox.Show("Data Updated");
                        JobWRKNo = srNo;
                    }
                    catch (Exception ex)
                    {
                        Database.RollBackTransaction();
                        ErrorMessageBox.Show(ex);
                    }
                    finally { }
                }

                ChangeMode(true);
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }


        private void button1_Click(object sender, EventArgs e)
        {
            frmPrintChallan5A frm = new frmPrintChallan5A();
            frm.ShowDialog();
        }

        private void comboBuyerName_SelectedIndexChanged(object sender, EventArgs e)
        {
            checkedListBox1.Items.Clear();
            if (Database.OpenConnection(Utility.DespacthConnectionString))
            {

                Database.myreader = Database.GetExecuteReaderCommand("select consignee,terms,Destination,typeofInvoice,Companycode,NEWGSTNO from companymaster WITH(NOLOCK) where "
                              + " companyname = '" + comboBuyerName.Text + "'");
                while (Database.myreader.Read())
                {
                    CompanyCode = Database.myreader[4].ToString();
                    lblgst.Text = Convert.ToString(Database.myreader["NEWGSTNO"]);
                }
                Database.myreader.Close();

                Database.myreader = Database.GetExecuteReaderCommand("SELECT ChallanNo FROM Challan5A WITH (NOLOCK) WHERE CompNam='"
                    + comboBuyerName.Text + "'  and  iscancel != 'yes' and CompanyName = '" + comboCompanyName.Text + "' and challanno in (select code from challanitem WITH (NOLOCK) where pendingqty > 0 ) order by Sysdate");
                while (Database.myreader.Read())
                    checkedListBox1.Items.Add(Database.myreader[0].ToString());
                Database.myreader.Close();
                if (checkedListBox1.Items.Count > 0)
                    bindMRNO(btnUpdate.Enabled);
            }
        }

        private void comboCompanyName_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (Database.OpenConnection(Utility.DespacthConnectionString))
            {
                Database.myreader = Database.GetExecuteReaderCommand("Select IndentCode from factoryinfo WITH(NOLOCK) where Name = '"
                     + comboCompanyName.Text + "'");
                if (Database.myreader.Read())
                    IndentCode = Database.myreader[0].ToString();
                Database.myreader.Close();
            }
            bindComboBuyer();
            bindMRNO(btnUpdate.Enabled);
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            //FrmChallan5AInwardPrintFormate frm = new FrmChallan5AInwardPrintFormate();
            //frm.ShowDialog();
            TabClass.OpenForm(new FrmChallan5AInwardPrintFormate());
        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            frmPrintJobChallan5A frm = new frmPrintJobChallan5A();
            frm.ShowDialog();

        }

        private void toolStripButton3_Click(object sender, EventArgs e)
        {

        }

        private void dataGridView1_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            if (dataGridView1.CurrentCell.ColumnIndex == 0) // For Commodity ComboBox
            {
                ComboBox comboBox = e.Control as ComboBox;
                comboBox.SelectedIndexChanged += new EventHandler(comboBox_SelectedIndexChanged);
            }

        }

        void comboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            ComboBox cb = ((ComboBox)sender);
            try
            {
                if (dataGridView1.CurrentCell.ColumnIndex == 0) // For Commodity ComboBox
                {
                    //Database.OpenConnection(Utility.DespacthConnectionString);
                    //Database.myreader = Database.GetExecuteReaderCommand("select THeading from commodity WITH(NOLOCK) where CommodityName ='"
                    //          + cb.Text + "' and companyname = '" + comboCompanyName.Text + "'");
                    //while (Database.myreader.Read())
                    //    dataGridView1.Rows[dataGridView1.CurrentRow.Index].Cells[6].Value = Database.myreader[0].ToString();
                    //Database.myreader.Close();
                    //Database.Closeconnection();
                }
            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
            }

        }

        private void UpdateInsurancePolicy(double TotalAmt, bool CheckInsurence)
        {

            if (CheckInsurence)
            {
                TotalAmt = (TotalAmt * 110) / 100;
                TotalAmt = Math.Round(TotalAmt, 2);
            }

        }

        bool isSystemUpdate = false;
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (isSystemUpdate) return;
            //bindComboBuyer();
            bindMRNO(btnUpdate.Enabled);
        }

        private void checkedListBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            dataGridView1.Rows.Clear();

            if (Database.OpenConnection(Utility.DespacthConnectionString))
            {
                for (int i = 0; i < checkedListBox1.CheckedItems.Count; i++)
                {
                    ischeked = true;
                    Database.myreader = Database.GetExecuteReaderCommand("SELECT ChallanItem.DescGoods, ChallanItem.Qty, ChallanItem.Unit, Code, SubCode, PendingQty,case  when isnull(FGCommodity,'')='' then  NatureProc else FGCommodity end FGCommodity,ChallanItem.ItemCode,item.ItemName from Challan5A with (NOLOCK)  INNER JOIN  ChallanItem with (NOLOCK) ON ChallanNo=Code " +
                        " inner join MaterialProcessing.dbo.item with(nolock) on Item.CompanyName=Challan5A.CompanyName and ChallanItem.ItemCode = item.ItemCode WHERE Code='" + checkedListBox1.CheckedItems[i] + "'");
                    while (Database.myreader.Read())
                    {
                        int rIndex = dataGridView1.Rows.Count;
                        dataGridView1.Rows.Add();
                        dataGridView1.Rows[rIndex].Cells[ColumnDescOfGoods.Index].Value = Convert.ToString(Database.myreader[0]);
                        dataGridView1.Rows[rIndex].Cells[ColumnOrderQty.Index].Value = Convert.ToString(Database.myreader[1]);
                        dataGridView1.Rows[rIndex].Cells[ColumnPendingQty.Index].Value = Math.Round(Utility.SafeConvertToDouble(Database.myreader["PendingQty"]), FrmMain.ERPQtyDigit);
                        dataGridView1.Rows[rIndex].Cells[ColumnUnit.Index].Value = Convert.ToString(Database.myreader[2]);
                        dataGridView1.Rows[rIndex].Cells[ColumnChallanSubCode.Index].Value = Convert.ToString(Database.myreader[3]) + "/" + Convert.ToString(Database.myreader["SubCode"]);
                        dataGridView1.Rows[rIndex].Cells[colProcessdesc.Index].Value = Convert.ToString(Database.myreader["FGCommodity"]);
                        dataGridView1.Rows[rIndex].Cells[ColChallnItem.Index].Value = Convert.ToString(Database.myreader["ItemName"]);
                        dataGridView1.Rows[rIndex].Cells[ColChnItemCode.Index].Value = Convert.ToString(Database.myreader["ItemCode"]);

                    }
                    Database.myreader.Close();
                }
            }
        }

        private void dataGridView1_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            if (e.ColumnIndex == dataGridView1.Columns["ColumnAcceptedQty"].Index)
            {
                if (e.FormattedValue.ToString().Length == 0) return;

                if (!Convert.ToBoolean(dataGridView1.Rows[e.RowIndex].Cells["ColumnCheck"].Value))
                {
                    e.Cancel = true;
                    ischeked = true;
                    MessageBox.Show("Check this row first", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                double MRNPendingQty = Utility.SafeConvertToDouble(dataGridView1.Rows[e.RowIndex].Cells["ColMRNQty"].Value);
                double PendingQty = Convert.ToDouble(dataGridView1.Rows[e.RowIndex].Cells["ColumnPendingQty"].FormattedValue);


                double Qty = 0;
                if (!double.TryParse(e.FormattedValue.ToString(), out Qty))
                {
                    e.Cancel = true;
                    MessageBox.Show("Invalid Qty", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else if (Qty > MRNPendingQty)
                {
                    e.Cancel = true;
                    dataGridView1.Rows[e.RowIndex].Cells["ColumnAcceptedQty"].Value = "";
                    MessageBox.Show("Accepted Qty cannot be greater then Pending MRN Qty", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                    return;
                }
                else if (Qty > PendingQty)
                {
                    e.Cancel = true;
                    MessageBox.Show("Accepted Qty cannot be greater then Pending Qty", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else if (Qty < 0)
                {
                    e.Cancel = true;
                    MessageBox.Show("Negitive Value not allowed!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

        }

        private void textChallanCode_Leave(object sender, EventArgs e)
        {
            string ChallanCode = ((TextBox)sender).Text;
            if (ChallanCode.Length == 0) return;
            if (ChallanCode.Length != 0) ChangeMode(false);
            else ChangeMode(true);
        }

        private void ChangeMode(bool isSaveMode)
        {
            if (!isSaveMode)
            {
                #region Update Mode

                btnOK.Enabled = false;
                btnUpdate.Enabled = true;
                bindMRNO(btnUpdate.Enabled);
                checkedListBox1.Items.Clear();
                checkedListBox1.Enabled = false;
                comboBuyerName.Items.Clear();
                comboBuyerName.Enabled = false;
                comboSuffix.Items.Clear();
                comboSuffix.Enabled = false;
                dateTimePicker1.Enabled = false;
                dataGridView1.Rows.Clear();
                dataGridView1.Columns[0].ReadOnly = true;
                dateTimePicker1.MinDate = Convert.ToDateTime("2017-07-01 00:00:00");
                dateTimePicker1.Enabled = true;
                //string MrNo = "";
                //if (comboMRNO.Items.Count > 0) MrNo = comboMRNO.Items[0].ToString();

                //comboMRNO.Text = MrNo;
                if (Database.OpenConnection(Utility.MaterialConnectionString))
                {
                    //bool isFirst = false;
                    Database.myreader = Database.GetExecuteReaderCommand("SELECT sysdate, companyname, sysdate ,yr ,BuyerName ,CombineChallanNo ,totalQty , Mrno ,DescOfGoods ,Unit ,OrderQty ,AcceptedQty ,Remarks ,ChallanSubCode,inwardno FROM Challan5AInward WITH(NOLOCK) WHERE SrNo='" + textSrNo.Text + "'");
                    if (Database.myreader.Read())
                    {
                        //comboBuyerName.Items.Add(Convert.ToString(Database.myreader["BuyerName"]));
                        comboBuyerName.Text = Convert.ToString(Database.myreader["BuyerName"]);
                        //comboBuyerName.SelectedIndex = 0;
                        isSystemUpdate = true;
                        comboSuffix.Items.Add(Convert.ToString(Database.myreader["yr"]));
                        comboSuffix.SelectedIndex = 0;
                        isSystemUpdate = false;
                        comboMRNO.Items.Add(Convert.ToString(Database.myreader["Mrno"]));
                        dateTimePicker1.Value = Convert.ToDateTime(Database.myreader["sysdate"]);
                        string[] ChallanNo = Convert.ToString(Database.myreader["CombineChallanNo"]).Split(',');
                        for (int i = 0; i < ChallanNo.Length; i++)
                        {
                            checkedListBox1.Items.Add(ChallanNo[i], true);
                        }
                        bindMRNO(btnUpdate.Enabled);
                    }
                    Database.myreader.Close();

                    Database.myreader = Database.GetExecuteReaderCommand("SELECT C.sysdate, C.companyname, C.sysdate ,C.yr ,BuyerName ,CombineChallanNo ,totalQty , C.Mrno ,DescOfGoods ," +
                        " C.Unit ,C.OrderQty ,C.AcceptedQty ,C.Remarks ,ChallanSubCode,inwardno,C.itemcode  ,(select sum(v.AcceptedQty) from MaterialProcessing.dbo.StoreInwards v where v.SrNo=C.InwardNo and v.ItemCode=c.Itemcode ) as mrnqty ,D.ItemCode,D.ItemName,D.FGCommodity " +
                        " FROM MaterialProcessing.dbo.Challan5AInward C WITH(NOLOCK)  " +
                        " inner join Despatch.dbo.Vw_Challan5A d on d.ChallanNo + '/' + cast(d.SubCode as char) =C.ChallanSubCode  WHERE c.SrNo='" + textSrNo.Text + "'");
                    while (Database.myreader.Read())
                    {

                        dataGridView1.Rows.Add(//DescOfGoods ,Unit ,OrderQty ,AcceptedQty ,Remarks ,ChallanSubCode
                            true,
                            Convert.ToString(Database.myreader["inwardno"]),
                            Convert.ToString(Database.myreader["itemcode"]),
                            Convert.ToString(Database.myreader["DescOfGoods"]),
                            Convert.ToString(Database.myreader["ItemName"]),
                            Convert.ToString(Database.myreader["OrderQty"]),
                            Convert.ToString(Database.myreader["OrderQty"]),
                            Convert.ToString(Database.myreader["Unit"]),
                            Convert.ToString(Database.myreader["AcceptedQty"]), "",
                            Convert.ToString(Database.myreader["Remarks"]),
                            Convert.ToString(Database.myreader["mrnqty"]),
                            Convert.ToString(Database.myreader["ChallanSubCode"]),
                            Convert.ToString(Database.myreader["FGCommodity"]),
                            Convert.ToString(Database.myreader["ItemName"])
                            );


                    }
                    Database.myreader.Close();

                    for (int i = 0; i < dataGridView1.Rows.Count; i++)
                    {
                        object opQty = Database.GetExecuteScalarCommand_return("SELECT PendingQty FROM Despatch..ChallanItem WITH(NOLOCK) where (Code+'/'+ltrim(str(SubCode))='" + dataGridView1.Rows[i].Cells["ColumnChallanSubCode"].FormattedValue.ToString() + "')");
                        float pQty = 0;
                        if (opQty != null)
                        {
                            float.TryParse(opQty.ToString(), out pQty);
                        }
                        float aQty = 0;
                        float.TryParse(dataGridView1.Rows[i].Cells["ColumnAcceptedQty"].FormattedValue.ToString(), out aQty);
                        dataGridView1.Rows[i].Cells["ColumnPendingQty"].Value = pQty + aQty;
                    }

                }


                #endregion
            }
            else
            {
                #region Save Mode

                textSrNo.Text = "";
                btnOK.Enabled = true;
                btnUpdate.Enabled = false;
                dataGridView1.Rows.Clear();
                dataGridView1.Columns[0].ReadOnly = false;
                checkedListBox1.Enabled = true;
                comboBuyerName.Items.Clear();
                comboBuyerName.Enabled = true;
                comboSuffix.Items.Clear();
                comboSuffix.Enabled = true;
                dateTimePicker1.Enabled = true;
                dateTimePicker1.Value = DateTime.Now;
                dateTimePicker1.MinDate = Convert.ToDateTime("2017-07-01 00:00:00");
                if (Database.OpenConnection(Utility.DespacthConnectionString))
                {
                    Database.myreader = Database.GetExecuteReaderCommand("Select name,IndentCode from factoryinfo WITH(NOLOCK) order by Name");
                    while (Database.myreader.Read())
                    {
                        comboCompanyName.Items.Add(Database.myreader[0].ToString());
                        IndentCode = Database.myreader[1].ToString();
                    }
                    Database.myreader.Close();
                    comboCompanyName.Text = frmDefaultVale.CompanyName;

                    Database.myreader = Database.GetExecuteReaderCommand("SELECT Yr FROM materialprocessing..yearmaster WITH(NOLOCK) ORDER BY srno DESC");
                    while (Database.myreader.Read())
                    {
                        comboSuffix.Items.Add(Database.myreader[0].ToString());
                    }
                    Database.myreader.Close();
                    if (comboSuffix.Items.Count > 0) comboSuffix.SelectedIndex = 0;
                }

                bindComboBuyer();
                bindMRNO(isSaveMode);
                #endregion
            }
        }

        private void dataGridView1_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            try
            {
                if (this.dataGridView1.CurrentCell.ColumnIndex == ColumnAcceptedQty.Index)
                {
                    DataGridViewTextBoxCell Col1 = (DataGridViewTextBoxCell)dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[ColumnPendingQty.Index];
                    DataGridViewTextBoxCell Col2 = (DataGridViewTextBoxCell)dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[ColumnAcceptedQty.Index];
                    DataGridViewTextBoxCell Col3 = (DataGridViewTextBoxCell)dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[Column1.Index];
                    Col3.Value = Math.Round(Convert.ToDouble(Utility.ConverttoInt(Col1.FormattedValue.ToString())) -
                                 Convert.ToDouble(Utility.ConverttoInt(Col2.FormattedValue.ToString())), 3);
                }
                else if (dataGridView1.CurrentCell.ColumnIndex == dataGridView1.Columns["MRNDataGridViewMultiColumnComboBoxColumn"].Index)
                {
                    //if (textSrNo.Text.Length == 0)
                    {
                        string strno = Convert.ToString(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells["MRNDataGridViewMultiColumnComboBoxColumn"].Value);
                        if (strno.Length > 0)
                        {
                            using (System.Data.SqlClient.SqlConnection conn = new System.Data.SqlClient.SqlConnection(Utility.MaterialConnectionString))
                            {
                                conn.Open();
                                string strsql = " select  distinct itemcode,itemname from StoreInwards  WHERE SrNo ='" + strno + "'";
                                System.Data.SqlClient.SqlDataAdapter Adapter = new System.Data.SqlClient.SqlDataAdapter();
                                Adapter.SelectCommand = new System.Data.SqlClient.SqlCommand(strsql, conn);
                                DataTable dt = new DataTable();
                                Adapter.Fill(dt);
                                DataGridViewComboBoxCell Col11 = (DataGridViewComboBoxCell)dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[ColMRNItemName.Index];

                                Col11.DataSource = null;
                                Col11.DataSource = dt;
                                Col11.DisplayMember = "itemName";
                                Col11.ValueMember = "Itemcode";
                                conn.Close();
                            }
                        }


                    }
                }
                else if (dataGridView1.CurrentCell.ColumnIndex == dataGridView1.Columns[ColMRNItemName.Index].Index)
                {
                    string strno = Convert.ToString(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells["MRNDataGridViewMultiColumnComboBoxColumn"].Value);
                    string strsql = "select sum(AcceptedQty)-isnull(ChaIwdQ,0) as Pending  from Vw_StoreInwards  left join (  " +
                            " select distinct inwardno,Itemcode,isnull(sum(AcceptedQty),0) ChaIwdQ from Challan5AInward   where isnull(inwardno,'')<>'' " +
                            " and CompanyName='" + comboCompanyName.Text + "' AND Yr='" + comboSuffix.Text + "'   group by inwardno,Itemcode ) CI on " +
                            " CI.InwardNo=SrNo and CI.Itemcode =Vw_StoreInwards.Itemcode WHERE SrNo ='" + strno + "' and Vw_StoreInwards.itemcode='" + Convert.ToString(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells["ColMRNItemName"].Value) + "'" +
                            " group by MRNo,SrNo, ChallanNo,ChaIwdQ  having sum(AcceptedQty)-isnull(ChaIwdQ ,0) >0";
                    if (Database.OpenConnection(Utility.MaterialConnectionString))
                    {
                        double Inwaredqt = Utility.SafeConvertToDouble(Database.GetExecuteNonQueryCommand_retrun(strsql));
                        dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells["ColMRNQty"].Value = Math.Round(Inwaredqt, 3);
                    }
                }

            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
            }
        }

        private void dataGridView1_Leave(object sender, EventArgs e)
        {
            try
            {


            }
            catch (Exception ex)
            {
                ex.Message.ToString();
            }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            bool blntan = false;

            if (!FrmMain.IsUpdateJobWork)
                MessageBox.Show("you have no right to delete Job work");
            else
            {
                string srNo = textSrNo.Text;
                if (Database.OpenConnection(Utility.MaterialConnectionString))
                {
                    try
                    {
                        Database.BeginTransaction();
                        blntan = true;
                        for (int i = 0; i < dataGridView1.Rows.Count; i++)
                        {
                            double pQty = Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells["ColumnAcceptedQty"].FormattedValue.ToString());
                            Database.GetExecuteNonQueryCommand("UPDATE Despatch..ChallanItem SET PendingQty=PendingQty + " + pQty
                                + " WHERE (Code+'/'+LTRIM(STR(SubCode))='" + Convert.ToString(dataGridView1.Rows[i].Cells["ColumnChallanSubCode"].FormattedValue) + "')");
                        }
                        Database.GetExecuteNonQueryCommand("insert into Challan5AInwarddelete(companyname,sysdate,SrNO,yr,BuyerName,BuyerAddress,CombineChallanNo,totalQty,transportername,vehicalno,natureOfProcess,transId,Mrno,DescOfGoods,Unit,OrderQty,AcceptedQty,Remarks,ChallanSubCode,InwardNo) select companyname,sysdate,SrNO,yr,BuyerName,BuyerAddress,CombineChallanNo,totalQty,transportername,vehicalno,natureOfProcess,transId,Mrno,DescOfGoods,Unit,OrderQty,AcceptedQty,Remarks,ChallanSubCode,InwardNo from Challan5AInward WHERE SrNo='" + srNo + "'");

                        string query = "Delete from  Challan5AInward WHERE SrNo='" + srNo + "'";
                        Database.GetExecuteNonQueryCommand(query);

                        Database.CommitTransaction();
                        Utility.UserInformation("Deleted", "Job Work Inward Challan No : " + srNo.ToString() + " is Deleted successfully");
                        blntan = false;

                        MessageBox.Show("Data Deleted");
                        dataGridView1.Rows.Clear();
                        while (checkedListBox1.CheckedItems.Count > 0)
                        {
                            checkedListBox1.SetItemChecked(checkedListBox1.CheckedIndices[0], false);
                        }
                        IsUpdate = false;
                        btnOK.Enabled = true;
                        btnUpdate.Enabled = false;
                        textSrNo.Text = "";
                        ChangeMode(true);

                    }
                    catch (Exception ex)
                    {
                        if (blntan)
                            Database.RollBackTransaction();
                        ErrorMessageBox.Show(ex);
                    }
                    finally { }
                }
            }


        }

        private void dataGridView1_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            e.Cancel = true;
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            try
            {
                if (dataGridView1.SelectedRows.Count > 1)
                {
                    MessageBox.Show("Please Select any one rows");
                    return;
                }
                foreach (DataGridViewRow row in dataGridView1.SelectedRows)
                {
                    if (!Convert.ToBoolean(row.Cells["ColumnCheck"].Value)) continue;
                    if (Utility.SafeConvertToDouble(row.Cells["ColumnAcceptedQty"].Value) == 0)
                    {
                        MessageBox.Show("Please enter Accepted Qty");
                        break;
                    }
                    if (Utility.SafeConvertToDouble(row.Cells["ColumnPendingQty"].Value) == 0)
                    {
                        MessageBox.Show("Pending Qty is Zero can't use this challan");
                        break;
                    }
                    //Check for Seleted ChallanNo
                    string Challano = Convert.ToString(row.Cells["ColumnChallanSubCode"].Value);
                    double TotalPendingqty = Utility.SafeConvertToDouble(Database.GetExecuteNonQueryCommand_retrun("select PendingQty from Despatch.dbo.ChallanItem  " +
                        " where ChallanItem.Code + '/' + cast(ChallanItem.SubCode as varchar)='" + Challano + "'"));
                    double TotalAccqty = 0;
                    for (int i = 0; i <= dataGridView1.Rows.Count - 1; i++)
                    {
                        string chno = Convert.ToString(dataGridView1.Rows[i].Cells["ColumnChallanSubCode"].Value);
                        if (chno == Challano)
                            TotalAccqty += Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells["ColumnAcceptedQty"].Value);
                    }
                    if (Utility.SafeConvertToDouble(TotalPendingqty - TotalAccqty) <= 0)
                    {
                        MessageBox.Show("Please Select other Challan as it's already seleted");
                        break;
                    }
                    object[] rowData = new object[row.Cells.Count];
                    for (int i = 0; i <= rowData.Length - 1; i++)
                    {
                        if (i == ColumnAcceptedQty.Index)
                            rowData[i] = "";
                        else if (i == ColumnPendingQty.Index)
                            rowData[i] = Math.Round(Utility.SafeConvertToDouble(TotalPendingqty - TotalAccqty), FrmMain.ERPQtyDigit);
                        else if (i == ColMRNQty.Index)
                            rowData[i] = Math.Round(Utility.SafeConvertToDouble(TotalPendingqty - TotalAccqty), FrmMain.ERPQtyDigit);
                        else
                            rowData[i] = row.Cells[i].Value;
                    }
                    this.dataGridView1.Rows.Add(rowData);
                }
            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
            }
        }
    }
}