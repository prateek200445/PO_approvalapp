using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using DataGridViewAutoFilter;

namespace ERP
{
    public partial class FrmSmallBagBailForDespatch_New : Form
    {
        bool isdoubleclick = false;
        public int transid = 0;
        public System.Windows.Forms.StatusBarPanel pfrm_sbp1 = null;

        bool isDelete = false;
        DataSet dataset1Roll = new DataSet();
        DataView Ro;
        public FrmSmallBagBailForDespatch_New()
        {
            InitializeComponent();
            Form_Load();
        }

        private void Form_Load()
        {
            try
            {
                dateLoomEntry.Format = DateTimePickerFormat.Custom;
                dateLoomEntry.CustomFormat = "MM/dd/yyyy";
                //if (Database.OpenConnection(Utility.MaterialConnectionString))
                //{
                //    Database.myreader = Database.GetExecuteReaderCommand("select name from factoryinfo order by name");
                //while (Database.myreader.Read())
                //{
                //    comboCompanyName.Items.Add(Database.myreader[0].ToString());
                //}
                //Database.myreader.Close();

                comboCompanyName.DataSource = ProductionCommonFunction.GetCompanyList();
                comboCompanyName.DisplayMember = ProductionCommonVariables.FieldName.CompanyName;
                //comboCompanyName.ValueMember = ProductionCommonVariables.FieldName.CompanyId;
                comboCompanyName.Text = frmDefaultVale.CompanyName;



                //Database.myreader = Database.GetExecuteReaderCommand("select distinct(PartyName) from SmallBagPartyOrderMaster where CompanyName='" + comboCompanyName.Text + "' ");
                //while (Database.myreader.Read())
                //{
                //    comboPartyName.Items.Add(Database.myreader[0].ToString());
                //}
                //Database.Closeconnection();
                //  }

                //DataTable dtOrderType = new DataTable();
                //dtOrderType.Columns.Add("OrderType");
                //dtOrderType.Rows.Add(new object[] { "Small Bag" });
                //dtOrderType.Rows.Add(new object[] { "FIBC" });

                //cbOrderType.DataSource = dtOrderType;
                //cbOrderType.ValueMember = "OrderType";
                //cbOrderType.DisplayMember = "OrderType";
                cbOrderType.Items.Add("FIBC");

                cbOrderType.Items.Add("Small Bag");

                //comboPartyName.DataSource = ProductionCommonFunction.GetPartyName_SamllBag(comboCompanyName.Text);
                //comboPartyName.DisplayMember = ProductionCommonVariables.FieldName.PartyName;
                //comboPartyName.ValueMember = ProductionCommonVariables.FieldName.PartyName;

                if (cbOrderType.Items.Count > 0)
                    cbOrderType.SelectedIndex = 0;

                buttonDelete.Enabled = false;
                // BindGrid_data();
                // BindGrid();

            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
            }
        }

        private void FrmMISRollforDespatchExcelFormat_Load(object sender, EventArgs e)
        {
        }
        # region   Function
        private void BindGrid_data()
        {
            try
            {

                if (Database.OpenConnection(Utility.MaterialConnectionString))
                {
                    Database.myreader = Database.GetExecuteReaderCommand("select rollno,PCS,Weight,PartyName,MarketingOrdNo,v.BuyerOrderNo,SysDate from Prod_SmallBagBaillingEntry"
                        + " sb left join Despatch..vw_ProductionCompAndMarkInvNo v on v.itemno = sb.MarketingOrdNo and v.buyername = sb.PartyName and"
                        + " v.ProductionCompanyname = sb.CompanyName where rollno not in (select BailNo from Prod_SmallBagBailForDespatch where CompanyName='"
                        + comboCompanyName.Text + "' and"
                    + " PartyName='" + comboPartyName.Text + "' and Deppt='" + cbOrderType.Text + "') and sb.CompanyName='" + comboCompanyName.Text
                    + "' and PartyName='" + comboPartyName.Text + "' and sb.OrderType = '" + cbOrderType.Text + "'");
                    DataTable dt = new DataTable();
                    dt.Load(Database.myreader);
                    BindingSource datasource = new BindingSource(dt, null);
                    dataGridView1.DataSource = datasource;
                    dataset1Roll.Tables.Add(dt);


                    Ro = new DataView(dt);
                    comboRollNo.DataSource = Ro;
                    comboRollNo.DisplayMember = "RollNo";
                    // Add the AutoFilter header cell to each column.
                    foreach (DataGridViewColumn col in dataGridView1.Columns)
                    {
                        col.HeaderCell = new
                            DataGridViewAutoFilterColumnHeaderCell(col.HeaderCell);
                    }
                    dataGridView1.Columns[0].ReadOnly = true;
                    dataGridView1.Columns[0].Width = 100;
                    dataGridView1.Columns[1].Width = 50;
                    dataGridView1.Columns[2].Width = 150;
                    dataGridView1.Columns[3].Width = 75;
                    dataGridView1.Columns[4].Width = 75;
                    dataGridView1.Columns[5].Width = 75;
                    dataGridView1.Columns[6].Width = 75;

                }
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
                Database.myadapter = Database.GetAdapterCommand("select * from Prod_SmallBagBailForDespatch where CompanyName = '"
                    + comboCompanyName.Text + "' and SysDate >= '" + firstdate.ToString("yyyy-MM-dd") + "' and Deppt='Small Bag'   order by TransId desc");
                DataSet dataset1 = new DataSet();
                Database.myadapter.Fill(dataset1, "SmallBagBailForDespatch");

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
        public bool Validations()
        {
            try
            {
                if (textInvno.Text == "")
                {
                    MessageBox.Show("Please enter Inv No..");
                    return false;
                }

                Database.OpenConnection(Utility.MaterialConnectionString);
                for (int i = 0; i < dataGridView3.Rows.Count - 1; i++)
                {
                    if (Convert.ToString(dataGridView3[0, i].FormattedValue) == "")
                    {
                        MessageBox.Show("Please enter roll no.");
                        return false;
                    }
                    if (Convert.ToString(dataGridView3[1, i].FormattedValue) == "")
                    {
                        MessageBox.Show("Please enter PCS value.");
                        return false;
                    }
                    if (Convert.ToString(dataGridView3[2, i].FormattedValue) == "")
                    {
                        MessageBox.Show("Please enter Weight value.");
                        return false;
                    }
                    if (Convert.ToString(dataGridView3[3, i].FormattedValue) == "")
                    {
                        MessageBox.Show("Please enter Party name.");
                        return false;
                    }
                    if (Convert.ToString(dataGridView3[4, i].FormattedValue) == "")
                    {
                        MessageBox.Show("Please enter Marketing ord No.");
                        return false;
                    }

                    Database.myreader = Database.GetExecuteReaderCommand("select BailNo from Prod_SmallBagBailForDespatch where BailNo = '"
                            + dataGridView3[0, i].FormattedValue + "' and Deppt='" + cbOrderType.Text + "' and CompanyName='" + comboCompanyName.Text + "'");
                    DataTable dt = new DataTable();
                    dt.Load(Database.myreader);
                    if (dt.Rows.Count > 0)
                    {
                        MessageBox.Show("You all ready done entry for this Roll No : " + dataGridView3[0, i].FormattedValue + "");
                        return false;
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
                return false;
            }
        }
        public void BindDrop()
        {
            DataTable dt = new DataTable();
            dt = dataset1Roll.Tables[0];
            Ro = new DataView(dt);
            comboRollNo.DataSource = Ro;
            comboRollNo.DisplayMember = "RollNo";
        }

        public void FilterComboBox()
        {
            try
            {
                if (!isDelete)
                {
                    string SearchFor = comboRollNo.Text;
                    Ro.RowFilter = string.Concat("rollno LIKE '%", SearchFor, "%'");
                    comboRollNo.DataSource = Ro;
                    comboRollNo.DisplayMember = "RollNo";

                }
                else
                {

                    DataTable dt = new DataTable();
                    dt = dataset1Roll.Tables[0];
                    Ro = new DataView(dt);
                    comboRollNo.DataSource = Ro;
                    comboRollNo.DisplayMember = "RollNo";

                }
                comboRollNo.Height = 30;

                isDelete = false;
            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
            }
        }
        #endregion
        # region   Button Event
        private void buttonInitialize_Click(object sender, EventArgs e)
        {
            try
            {
                isdoubleclick = true;
                dataGridView3[0, 0].ReadOnly = false;
                if (dataGridView3.Rows.Count > 0)
                    dataGridView3.Rows.Clear();

                buttonDelete.Enabled = false;
                buttonSave.Enabled = true;

                dateLoomEntry.Enabled = true;
                isdoubleclick = false;
            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
            }
        }
        private void buttonSave_Click(object sender, EventArgs e)
        {
            try
            {
                if (Validations())
                {
                    bool isentry = false;
                    if (Database.OpenConnection(Utility.MaterialConnectionString))
                    {
                        Database.BeginTransaction();
                        for (int i = 0; i < dataGridView3.Rows.Count - 1; i++)
                        {
                            if (Database.GetExecuteNonQueryCommand("insert into Prod_SmallBagBailForDespatch values('" + dataGridView3[0, i].Value + "','"
                                      + comboCompanyName.Text + "','" + dataGridView3[3, i].Value + "','"
                                      + dataGridView3[4, i].Value + "','"
                                      + Utility.ConverttoInt(dataGridView3[1, i].Value.ToString()) + "',"
                                      + Utility.ConverttoInt(dataGridView3[2, i].Value.ToString()) + ",'"
                                      + dateLoomEntry.Value.ToString("yyyy-MM-dd") + "','"
                                      + dataGridView3[6, i].Value + "','" + textInvno.Text + "','" + cbOrderType.Text + "',0.0,'" + TextRemarks.Text + "','Despatch Godown')"))
                            {
                                isentry = true;
                            }
                        }
                    }
                    if (isentry)
                    {
                        Database.CommitTransaction();
                        Database.Closeconnection();
                        Utility.UserInformation("Save ", "Bail Entry " + dateLoomEntry.Value.ToString("yyyy-MM-dd") + "is Saved for Partyname " + comboPartyName.Text);
                        MessageBox.Show("Data Saved Succesfully");
                        dataGridView3.Rows.Clear();
                        textInvno.Text = "";
                        BindGrid_data();
                        BindGrid();
                    }
                    BindGrid();
                }

            }
            catch (Exception ex)
            {
                Database.RollBackTransaction();
                ErrorMessageBox.Show(ex);
            }
        }

        private void buttonDelete_Click(object sender, EventArgs e)
        {
            try
            {
                bool isrollno = false;
                if (Database.OpenConnection(Utility.MaterialConnectionString))
                {
                    Database.myreader = Database.GetExecuteReaderCommand("select rollno  from RollReceipt where companyname='" + comboCompanyName.Text + "' and rollno='"
                              + dataGridView3[0, 0].FormattedValue.ToString() + "' and warehouse = 'Small Bag Godown' ");
                    if (Database.myreader.Read())
                        isrollno = true;
                    Database.myreader.Close();

                    if (isrollno)
                        MessageBox.Show("This Bail is already receipt you can't delete it");

                    else
                    {
                        if (Database.GetExecuteNonQueryCommand("delete from SmallBagBailForDespatch where Deppt='Small Bag' and transid = " + transid + ""))
                            MessageBox.Show("Data Deleted Succesfully");
                        Database.Closeconnection();
                        isdoubleclick = true;
                        dataGridView3.Rows.Clear();
                        isdoubleclick = false;
                        Utility.UserInformation("Delete ", "Bail Entry " + transid + "is Delete for Bail No " + dataGridView1[1, 0].Value);
                    }
                }
                BindGrid();

            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
            }
        }
        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                for (int j = 0; j < dataGridView1.Rows.Count; j++)
                {
                    if ((Convert.ToString(dataGridView1.Rows[j].Cells[0].Value)) == (Convert.ToString(comboRollNo.Text)))
                    {
                        dataGridView1.Rows[j].Selected = true;
                        dataGridView1.FirstDisplayedScrollingRowIndex = j;
                    }
                    else
                    {
                        dataGridView1.Rows[j].Selected = false;
                    }
                }

                BindDrop();
                return;

            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
            }
        }
        #endregion
        #region Gridevent
        private void dataGridView2_DoubleClick(object sender, EventArgs e)
        {
            isdoubleclick = true;
            if (dataGridView3.Rows.Count >= 2)
                dataGridView3.Rows.Clear();
            buttonDelete.Enabled = true;
            buttonSave.Enabled = false;
            dateLoomEntry.Enabled = false;

            try
            {
                dataGridView3[0, 0].ReadOnly = true;
                dataGridView3[0, 0].Value = dataGridView2[1, dataGridView2.CurrentRow.Index].Value.ToString();
                dataGridView3[1, 0].Value = dataGridView2[5, dataGridView2.CurrentRow.Index].Value.ToString();
                dataGridView3[2, 0].Value = dataGridView2[6, dataGridView2.CurrentRow.Index].Value.ToString();
                dataGridView3[3, 0].Value = dataGridView2[3, dataGridView2.CurrentRow.Index].Value.ToString();
                dataGridView3[4, 0].Value = dataGridView2[4, dataGridView2.CurrentRow.Index].Value.ToString();
                dataGridView3[5, 0].Value = dataGridView2[7, dataGridView2.CurrentRow.Index].Value.ToString();
                dataGridView3[6, 0].Value = dataGridView2[8, dataGridView2.CurrentRow.Index].Value.ToString();
                transid = Convert.ToInt32(dataGridView2[0, dataGridView2.CurrentRow.Index].Value.ToString());

                comboPartyName.Text = dataGridView2[3, dataGridView2.CurrentRow.Index].Value.ToString();
                textInvno.Text = dataGridView2[9, dataGridView2.CurrentRow.Index].Value.ToString();
                dateLoomEntry.Value = (DateTime)dataGridView2[7, dataGridView2.CurrentRow.Index].Value;
                TextRemarks.Text = dataGridView2[12, dataGridView2.CurrentRow.Index].Value.ToString();
            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
            }
        }
        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                bool yes = false;
                for (int i = 0; i < dataGridView3.Rows.Count; i++)
                {
                    if ((Convert.ToString(dataGridView3.Rows[i].Cells[0].Value)) != (Convert.ToString(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[0].Value)))
                    {
                        yes = false;
                    }
                    else
                    {
                        yes = true;
                        return;
                    }
                }
                if (yes == false)
                {
                    //DataGridViewRow r2 = (DataGridViewRow)dataGridView1.Rows[0].Clone();
                    isdoubleclick = true;
                    //r2.Cells[0].Value = dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[0].Value;
                    //r2.Cells[1].Value = dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[1].Value;
                    //r2.Cells[2].Value = dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[2].Value;
                    //r2.Cells[3].Value = dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[3].Value;
                    //r2.Cells[4].Value = dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[4].Value;
                    //r2.Cells[5].Value = dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[5].Value;
                    //r2.Cells[6].Value = (dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[6].Value);
                    //dataGridView3.Rows.Add(r2);
                    //dataGridView3.Refresh();

                    dataGridView3.Rows.Add(
                        dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[0].Value,
                        dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[1].Value,
                        dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[2].Value,
                        dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[3].Value,
                        dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[4].Value,
                        dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[6].Value,
                        ""
                        );
                    isdoubleclick = false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
            }
        }
        private void dataGridView3_SelectionChanged(object sender, EventArgs e)
        {
            try
            {
                double sum = 0;
                pfrm_sbp1.Text = "";

                foreach (DataGridViewRow row in this.dataGridView3.Rows)
                {
                    foreach (DataGridViewColumn col in this.dataGridView3.Columns)
                    {
                        if (dataGridView3[col.Index, row.Index].Selected)
                            try
                            {
                                sum += Convert.ToDouble(dataGridView3[col.Index, row.Index].Value);
                            }
                            catch (Exception ex)
                            {
                                ex.ToString();
                            }
                    }
                }
                pfrm_sbp1.Text = " Sum : " + sum.ToString();

            }
            catch (Exception ex)
            {
                ex.ToString();
            }
        }
        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            try
            {
                double sum = 0;
                pfrm_sbp1.Text = "";

                foreach (DataGridViewRow row in this.dataGridView1.Rows)
                {
                    foreach (DataGridViewColumn col in this.dataGridView1.Columns)
                    {
                        if (dataGridView1[col.Index, row.Index].Selected)
                            try
                            {
                                sum += Convert.ToDouble(dataGridView1[col.Index, row.Index].Value);
                            }
                            catch (Exception ex)
                            {
                                ex.ToString();
                            }
                    }
                }
                pfrm_sbp1.Text = " Sum : " + sum.ToString();

            }
            catch (Exception ex)
            {
                ex.ToString();
            }
        }

        #endregion
        private void comboPartyName_SelectedIndexChanged(object sender, EventArgs e)
        {
            BindGrid_data();
        }
        private void dateLoomEntry_ValueChanged(object sender, EventArgs e)
        {
            BindGrid_data();
            BindGrid();
            isdoubleclick = true;
            dataGridView3.Rows.Clear();
            isdoubleclick = false;
        }
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            BindGrid_data();
            isdoubleclick = true;
            dataGridView3.Rows.Clear();
            isdoubleclick = false;
        }
        private void comboRollNo_KeyUp(object sender, KeyEventArgs e)
        {
            if (comboRollNo.Text.Length < 3) return;

            if ((e.KeyCode >= Keys.A && e.KeyCode <= Keys.Z) || (e.KeyCode >= Keys.D0 && e.KeyCode <= Keys.D9) || (e.KeyCode >= Keys.NumPad0 && e.KeyCode <= Keys.NumPad9) || e.KeyCode == Keys.Back || e.KeyCode == Keys.Delete)
            {
                if (e.KeyCode == Keys.Back || e.KeyCode == Keys.Delete)
                    isDelete = true;

                FilterComboBox();
            }
        }
      
       
        private void cbOrderType_SelectedIndexChanged(object sender, EventArgs e)
        {
            Database.myadapter = Database.GetAdapterCommand("select distinct(PartyName) from Prod_SmallBagBaillingEntry where"
                          + " CompanyName='" + comboCompanyName.Text + "' and Sysdate <='" + dateLoomEntry.Value.ToString("yyyy-MM-dd") + "' and OrderType = '" + cbOrderType.Text + "' and PoNo is not null ");
            DataTable dt2 = new DataTable();
            Database.myadapter.Fill(dt2);
            comboPartyName.DataSource = dt2;
            comboPartyName.ValueMember = dt2.Columns["PartyName"].ColumnName.ToString();
            comboPartyName.DisplayMember = dt2.Columns["PartyName"].ColumnName.ToString();
        }
    }
}





