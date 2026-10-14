using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ERP
{
    public partial class FrmPackingListSelection : Form
    {
        public string packinglist = "";
        DataGridView dg;
        string ColItemCode;
        string Colnetwt;

        public FrmPackingListSelection(string CompanyName, string PartyName, double InvoiceNetWt, DataGridView dg, string ColItemCode, string Colnetwt)
        {
            InitializeComponent();
            txtCompanyName.Text = CompanyName;
            TxtPartyName.Text = PartyName;
            txtProduction.Text = InvoiceNetWt.ToString();
            packinglist = "";
            this.dg = dg;
            this.ColItemCode = ColItemCode;
            this.Colnetwt = Colnetwt;
            Form_Load();

        }
        public FrmPackingListSelection(string CompanyName, string PartyName, double InvoiceNetWt)
        {
            InitializeComponent();
            txtCompanyName.Text = CompanyName;
            TxtPartyName.Text = PartyName;
            txtProduction.Text = InvoiceNetWt.ToString();
            packinglist = "";
            Form_Load();

        }
        private void Form_Load()
        {
            fillData();
        }
        private void fillData()
        {
            try
            {
                if (Database.OpenConnection(Utility.MaterialConnectionString))
                {
                    //and PartyName='" + TxtPartyName.Text + "' 
                    string strsql = "select * from vw_PackingListAll_Pending where companyname='" + txtCompanyName.Text + "'" +
                        "  order by despatchdate,packinglistno ,invno";
                    //and yr='" + frmDefaultVale.Year + "'
                    Database.GetExecuteReaderCommand(strsql);
                    DataTable dt = new DataTable();
                    dt.Load(Database.myreader);
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        dataGridView1.Rows.Add(false,
                                       dt.Rows[i]["PACKINGLISTNO"],
                                       dt.Rows[i]["invno"],
                                       dt.Rows[i]["PartyName"],
                                       Convert.ToDateTime(dt.Rows[i]["despatchdate"]).ToString("dd-MM-yyyy"),
                                       dt.Rows[i]["Netwt"]
                                       );
                    }
                }
            }
            catch (Exception ex)
            {
                ex.Message.ToString();
            }
        }
        private void Ok_Click(object sender, EventArgs e)
        {
            try
            {
                bool ischek = false;
                double TotalNetwt = 0;
                string list = "";
                for (int i = 0; i < dataGridView1.Rows.Count; i++)
                {
                    if ((bool)dataGridView1.Rows[i].Cells[0].Value == true)
                    {
                        ischek = true;
                        TotalNetwt += Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[ColNetwt.Index].Value);
                        if (Convert.ToString(dataGridView1.Rows[i].Cells[colPackingListNo.Index].Value) == "")
                            list += dataGridView1.Rows[i].Cells[colInvNo.Index].Value + ",";
                        else
                            list += dataGridView1.Rows[i].Cells[colPackingListNo.Index].Value + ",";
                    }
                }
                txtBiferQty.Text = TotalNetwt.ToString();
                this.packinglist = list;
                if (ischek == false)
                {
                    MessageBox.Show("Please Check any one Checkbox to Proceed.");
                    return;
                }
                if (Utility.SafeConvertToDouble(txtBiferQty.Text) != Utility.SafeConvertToDouble(txtProduction.Text))
                {
                    MessageBox.Show("Packing List Qty and Invoice Qty Not Match");
                    return;
                }

                #region check for Itemcode in Datagridview
                string pcklstno = list;
                if (pcklstno.Length > 0)
                    pcklstno = pcklstno.Substring(0, pcklstno.Length - 1);

                Database.OpenConnection(Utility.MaterialConnectionString);

                DataTable FinalTable = new DataTable();
                DataColumn dtColumn;
                dtColumn = new DataColumn();
                dtColumn.DataType = typeof(string);
                dtColumn.ColumnName = "ItemCode";
                dtColumn.Caption = "ItemCode";
                dtColumn.ReadOnly = false;
                FinalTable.Columns.Add(dtColumn);
                dtColumn = new DataColumn();
                dtColumn.DataType = typeof(double);
                dtColumn.ColumnName = "Netwt";
                dtColumn.Caption = "Netwt";
                dtColumn.ReadOnly = false;
                FinalTable.Columns.Add(dtColumn);

                for (int i = 0; i < this.dg.Rows.Count - 1; i++)
                {
                    DataRow myDataRow;
                    myDataRow = FinalTable.NewRow();
                    myDataRow["ItemCode"] = Convert.ToString(dg.Rows[i].Cells[this.ColItemCode].Value);
                    myDataRow["Netwt"] = Utility.SafeConvertToDouble(dg.Rows[i].Cells[this.Colnetwt].Value);
                    FinalTable.Rows.Add(myDataRow);
                }
                var query = from r in FinalTable.AsEnumerable()
                            group r by r.Field<string>("ItemCode") into g
                            select new
                            {
                                ItemCode = g.Key,
                                SumNetwt = g.Sum(r => r.Field<double>("Netwt")),
                                OrderCount = g.Count()
                            };

                FinalTable = new DataTable();
                dtColumn = new DataColumn();
                dtColumn.DataType = typeof(string);
                dtColumn.ColumnName = "ItemCode";
                dtColumn.Caption = "ItemCode";
                dtColumn.ReadOnly = false;
                FinalTable.Columns.Add(dtColumn);
                dtColumn = new DataColumn();
                dtColumn.DataType = typeof(double);
                dtColumn.ColumnName = "Netwt";
                dtColumn.Caption = "Netwt";
                dtColumn.ReadOnly = false;
                FinalTable.Columns.Add(dtColumn);

                foreach (var item in query)
                {
                    DataRow myDataRow;
                    myDataRow = FinalTable.NewRow();
                    myDataRow["ItemCode"] = Convert.ToString(item.ItemCode);
                    myDataRow["Netwt"] = Utility.SafeConvertToDouble(item.SumNetwt);
                    FinalTable.Rows.Add(myDataRow);
                }

                DataTable dt = Database.GetDataTable("Select Itemcode,sum(netwt) as Netwt from [vw_PackingListAll_Pending_dtl] where PACKINGLISTNO in ('" + pcklstno.Replace(",", "','") + "') group by Itemcode");
                int cnt = dt.Rows.Count;
                int gridrows = FinalTable.Rows.Count;
                if (cnt != gridrows)
                {
                    this.packinglist = "";
                    MessageBox.Show("Packing List Item is not match ");
                }

                for (int i = 0; i < FinalTable.Rows.Count; i++)
                {
                    string griditemcode = Convert.ToString(FinalTable.Rows[i][0]);
                    double netwt = Math.Round(Utility.SafeConvertToDouble(FinalTable.Rows[i][1]), FrmMain.ERPQtyDigit);
                    dt.DefaultView.RowFilter = "Itemcode='" + griditemcode + "'";
                    DataTable dv = dt.DefaultView.ToTable();
                    if (dv.Rows.Count == 0)
                    {
                        this.packinglist = "";
                        MessageBox.Show("Packing List Item is not match ");
                        break;
                    }
                    else
                    {
                        if (netwt != Math.Round(Utility.SafeConvertToDouble(dv.Rows[0][1].ToString()), FrmMain.ERPQtyDigit))
                        {
                            this.packinglist = "";
                            MessageBox.Show("Packing List Item is not match ");
                            break;
                        }
                    }

                }
                #endregion
                this.DialogResult = System.Windows.Forms.DialogResult.OK;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Problem with Selection of Packing List :" + ex.Message);
            }
        }

        private void dataGridView1_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            try
            {
                if (dataGridView1.CurrentRow.Cells.IndexOf(dataGridView1.CurrentCell) == dtCheck.Index)
                {
                    double TotalNetwt = 0;
                    for (int i = 0; i < dataGridView1.Rows.Count; i++)
                    {
                        if ((bool)dataGridView1.Rows[i].Cells[0].Value == true)
                        {
                            TotalNetwt += Utility.SafeConvertToDouble(dataGridView1.Rows[i].Cells[ColNetwt.Index].Value);
                        }
                    }
                    txtBiferQty.Text = TotalNetwt.ToString();
                }
            }
            catch (Exception ex)
            {
                ex.Message.ToString();
            }
        }
    }
}
