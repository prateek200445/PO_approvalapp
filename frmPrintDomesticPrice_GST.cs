using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.Shared;
using System.Collections;
using CrystalDecisions.Windows.Forms;
using CrystalDecisions.Shared.Interop;
using crDespatch;
using crvr;
using System.Net.Mail;
using System.Net;

namespace ERP
{
    public partial class frmPrintDomesticPrice_GST : Form
    {
        public frmPrintDomesticPrice_GST()
        {
            InitializeComponent();
            Form_Load();
        }

        private void Form_Load()
        {

            if (Database.OpenConnection(Utility.DespacthConnectionString))
            {
                DespatchCommonFunction.GetCompanyList(comboCompanyName);
                DespatchCommonFunction.GetYear(comboyr, Utility.DespacthConnectionString);
                Database.Closeconnection();
            }
            comboyr.Text = frmDefaultVale.Year;
            comboCompanyName.Text = frmDefaultVale.CompanyName;
        }

        DataSet dataset1 = new DataSet();
        private void button1_Click(object sender, EventArgs e)
        {

            try
            {
                Cursor = Cursors.WaitCursor;

                if (Database.OpenConnection(Utility.DespacthConnectionString))
                {
                    #region OEL2
                    if (comboCompanyName.Text == "Oswal Extrusion Limited (Unit-II)")
                    {
                        bool IsSuppMentary = false;
                        Database.myreader = Database.GetExecuteReaderCommand("select InvoiceNumber from DomesticInvoiceOEL2 where InvoiceNumber = "
                                  + comboInvoice.Text + " and sufix = '" + comboyr.Text + "' and companyname = '"
                                  + comboCompanyName.Text + "'");
                        if (Database.myreader.Read())
                            IsSuppMentary = true;
                        Database.myreader.Close();


                        if (IsSuppMentary)
                        {
                            dataset1.Clear();
                            Database.myadapter = Database.GetAdapterCommand("select * from Vw_DomesticInvoiceOEL2 where InvNo = "
                                      + comboInvoice.Text + " and suffix = '" + comboyr.Text + "' and companyname = '"
                                      + comboCompanyName.Text + "' order by commodity desc ,groupcommodity");
                            // DataSet dataset1 = new DataSet();
                            Database.myadapter.Fill(dataset1, "Vw_DomesticInvoiceOEL2");
                            DateTime BillDate = Convert.ToDateTime(dataset1.Tables[0].Rows[0]["Invdate"]);
                            if (BillDate >= Convert.ToDateTime("2017-07-01"))
                            {
                                CRDomesticPriceOEL2_GST cr = new CRDomesticPriceOEL2_GST();
                                foreach (IConnectionInfo info in cr.DataSourceConnections)
                                    info.SetConnection(Utility.ServerIP, "Despatch", Utility.UserID, Utility.Password);
                                foreach (ReportDocument sub in cr.Subreports)
                                {
                                    foreach (IConnectionInfo info in sub.DataSourceConnections)
                                    {
                                        info.SetConnection(Utility.ServerIP, "Despatch", Utility.UserID, Utility.Password);
                                    }
                                }
                                cr.SetDataSource(dataset1.Tables[0]);
                                Tools.ToolsCommonFunction.CrystalReportSetSource(crystalReportViewer1, cr);
                            }
                            else
                            {
                                CRDomesticPriceOEL2 cr = new CRDomesticPriceOEL2();
                                foreach (IConnectionInfo info in cr.DataSourceConnections)
                                    info.SetConnection(Utility.ServerIP, "Despatch", Utility.UserID, Utility.Password);
                                foreach (ReportDocument sub in cr.Subreports)
                                {
                                    foreach (IConnectionInfo info in sub.DataSourceConnections)
                                    {
                                        info.SetConnection(Utility.ServerIP, "Despatch", Utility.UserID, Utility.Password);
                                    }
                                }
                                cr.SetDataSource(dataset1.Tables[0]);
                                Tools.ToolsCommonFunction.CrystalReportSetSource(crystalReportViewer1, cr);
                            }

                        }
                        else
                        {
                            dataset1.Clear();
                            Database.myadapter = Database.GetAdapterCommand("select * from Vw_DomesticInvoiceOEL2 where InvNo = "
                                      + comboInvoice.Text + " and suffix = '" + comboyr.Text + "' and companyname = '"
                                      + comboCompanyName.Text + "' order by commodity desc ,groupcommodity");
                            //             DataSet dataset1 = new DataSet();
                            Database.myadapter.Fill(dataset1, "Vw_DomesticInvoiceOEL2");
                            DateTime BillDate = Convert.ToDateTime(dataset1.Tables[0].Rows[0]["Invdate"]);
                            if (BillDate >= Convert.ToDateTime("2017-07-01"))
                            {
                                CRDomesticPriceOEL2_GST cr = new CRDomesticPriceOEL2_GST();
                                foreach (IConnectionInfo info in cr.DataSourceConnections)
                                    info.SetConnection(Utility.ServerIP, "Despatch", Utility.UserID, Utility.Password);
                                foreach (ReportDocument sub in cr.Subreports)
                                {
                                    foreach (IConnectionInfo info in sub.DataSourceConnections)
                                    {
                                        info.SetConnection(Utility.ServerIP, "Despatch", Utility.UserID, Utility.Password);
                                    }
                                }
                                cr.SetDataSource(dataset1.Tables[0]);
                                Tools.ToolsCommonFunction.CrystalReportSetSource(crystalReportViewer1, cr);

                            }
                            else
                            {
                                CRDomesticPriceOEL2 cr = new CRDomesticPriceOEL2();
                                foreach (IConnectionInfo info in cr.DataSourceConnections)
                                    info.SetConnection(Utility.ServerIP, "Despatch", Utility.UserID, Utility.Password);
                                foreach (ReportDocument sub in cr.Subreports)
                                {
                                    foreach (IConnectionInfo info in sub.DataSourceConnections)
                                    {
                                        info.SetConnection(Utility.ServerIP, "Despatch", Utility.UserID, Utility.Password);
                                    }
                                }
                                cr.SetDataSource(dataset1.Tables[0]);
                                Tools.ToolsCommonFunction.CrystalReportSetSource(crystalReportViewer1, cr);
                            }
                        }
                    }
                    #endregion
                    else
                    {

                        string MerchantCondition = " and MerchantExport ='";
                        if (checkMerchantExport.Checked)
                            MerchantCondition += "yes" + "'";
                        else
                            MerchantCondition = " and isnull(MerchantExport,'no') = 'no'";


                        bool IsSuppMentary = false;
                        Database.myreader = Database.GetExecuteReaderCommand("select InvNo from Domesticinvoice where InvNo = "
                                  + comboInvoice.Text + " and suffix = '" + comboyr.Text + "' and companyname = '"
                                  + comboCompanyName.Text + "' and supplementaryInvoice = 'Supplementary Invoice' " + MerchantCondition);
                        if (Database.myreader.Read())
                            IsSuppMentary = true;
                        Database.myreader.Close();

                        if (IsSuppMentary)
                        {
                            dataset1.Clear();
                            Database.myadapter = Database.GetAdapterCommand("select * from Vw_DomesticInvoice where InvNo = "
                                      + comboInvoice.Text + " and suffix = '" + comboyr.Text + "' and companyname = '"
                                      + comboCompanyName.Text + "' " +  MerchantCondition +"  order by commodity desc ,groupcommodity");
                            //   DataSet dataset1 = new DataSet();
                            Database.myadapter.Fill(dataset1, "Vw_DomesticInvoice");
                            DateTime BillDate = Convert.ToDateTime(dataset1.Tables[0].Rows[0]["Invdate"]);
                            if (BillDate >= Convert.ToDateTime("2017-07-01"))
                            {
                                CRSuppDomesticPrice_GST cr = new CRSuppDomesticPrice_GST();
                                foreach (IConnectionInfo info in cr.DataSourceConnections)
                                    info.SetConnection(Utility.ServerIP, "Despatch", Utility.UserID, Utility.Password);
                                foreach (ReportDocument sub in cr.Subreports)
                                {
                                    foreach (IConnectionInfo info in sub.DataSourceConnections)
                                    {
                                        info.SetConnection(Utility.ServerIP, "Despatch", Utility.UserID, Utility.Password);
                                    }
                                }
                                cr.SetDataSource(dataset1.Tables[0]);
                                Tools.ToolsCommonFunction.CrystalReportSetSource(crystalReportViewer1, cr);
                            }
                            else
                            {
                                dataset1.Clear();
                                Database.myadapter = Database.GetAdapterCommand("select * from Vw_DomesticInvoice where InvNo = "
                                          + comboInvoice.Text + " and suffix = '" + comboyr.Text + "' and companyname = '"
                                          + comboCompanyName.Text + "' " + MerchantCondition + "  order by commodity desc ,groupcommodity");
                                dataset1 = new DataSet();
                                Database.myadapter.Fill(dataset1, "Vw_DomesticInvoice");
                                CRSuppDomesticPrice cr = new CRSuppDomesticPrice();
                                foreach (IConnectionInfo info in cr.DataSourceConnections)
                                    info.SetConnection(Utility.ServerIP, "Despatch", Utility.UserID, Utility.Password);
                                foreach (ReportDocument sub in cr.Subreports)
                                {
                                    foreach (IConnectionInfo info in sub.DataSourceConnections)
                                    {
                                        info.SetConnection(Utility.ServerIP, "Despatch", Utility.UserID, Utility.Password);
                                    }
                                }
                                cr.SetDataSource(dataset1.Tables[0]);
                                Tools.ToolsCommonFunction.CrystalReportSetSource(crystalReportViewer1, cr);
                            }
                        }
                        else
                        {
                            dataset1.Clear();
                            Database.myadapter = Database.GetAdapterCommand("select * from Vw_DomesticInvoice where InvNo = "
                                      + comboInvoice.Text + " and suffix = '" + comboyr.Text + "' and companyname = '"
                                      + comboCompanyName.Text + "' " + MerchantCondition + " order by commodity desc ,groupcommodity");
                            //DataSet dataset1 = new DataSet();
                            Database.myadapter.Fill(dataset1, "Vw_DomesticInvoice");
                            DateTime BillDate = Convert.ToDateTime(dataset1.Tables[0].Rows[0]["Invdate"]);
                            if (BillDate >= Convert.ToDateTime("2017-07-01"))
                            {
                                CRDomesticPrice_GST cr = new CRDomesticPrice_GST();
                                foreach (IConnectionInfo info in cr.DataSourceConnections)
                                    info.SetConnection(Utility.ServerIP, "Despatch", Utility.UserID, Utility.Password);
                                foreach (ReportDocument sub in cr.Subreports)
                                {
                                    foreach (IConnectionInfo info in sub.DataSourceConnections)
                                    {
                                        info.SetConnection(Utility.ServerIP, "Despatch", Utility.UserID, Utility.Password);
                                    }
                                }
                                cr.SetDataSource(dataset1.Tables[0]);
                                Tools.ToolsCommonFunction.CrystalReportSetSource(crystalReportViewer1, cr);
                            }
                            else
                            {
                                CRDomesticPrice cr = new CRDomesticPrice();
                                foreach (IConnectionInfo info in cr.DataSourceConnections)
                                    info.SetConnection(Utility.ServerIP, "Despatch", Utility.UserID, Utility.Password);
                                foreach (ReportDocument sub in cr.Subreports)
                                {
                                    foreach (IConnectionInfo info in sub.DataSourceConnections)
                                    {
                                        info.SetConnection(Utility.ServerIP, "Despatch", Utility.UserID, Utility.Password);
                                    }
                                }
                                cr.SetDataSource(dataset1.Tables[0]);
                                Tools.ToolsCommonFunction.CrystalReportSetSource(crystalReportViewer1, cr);


                            }
                        }
                    }
                }

            }

            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
            }
            finally
            {
                Cursor = Cursors.Arrow;
            }
        }

        private void frmPrintDomesticPrice_Load(object sender, EventArgs e)
        {
        }

        private void btnPrint_Click(object sender, EventArgs e)
        {
            if (!FrmMain.IsCreateDomesticInvoice)
                MessageBox.Show("You have no right to Print Invoice");
            else
            {
                try
                {
                    {

                        string MerchantCondition = " and MerchantExport ='";
                        if (checkMerchantExport.Checked)
                            MerchantCondition += "yes" + "'";
                        else
                            MerchantCondition = " and isnull(MerchantExport,'no') = 'no'";


                        //  dataset1.Clear();

                        ArrayList Arr = new ArrayList();
                        Arr.Add("ORIGINAL FOR RECIPIENT");
                        Arr.Add("DUPLICATE FOR TRANSPORTER");
                        Arr.Add("TRIPLICATE FOR SUPPLIER");
                        Arr.Add("EXTRA COPY");

                        if (Database.OpenConnection(Utility.DespacthConnectionString))
                        {

                            bool IsAvail = false;
                            Database.myreader = Database.GetExecuteReaderCommand("select InvNo from printdomesticinvoice where InvNo = " + comboInvoice.Text
                             + " and yr  = '" + comboyr.Text + "' and companyname ='"
                             + comboCompanyName.Text + "'");
                            if (Database.myreader.Read())
                                IsAvail = true;
                            Database.myreader.Close();

                            bool IsSuppMentary = false;
                            Database.myreader = Database.GetExecuteReaderCommand("select InvNo from Domesticinvoice where InvNo = "
                                      + comboInvoice.Text + " and suffix = '" + comboyr.Text + "' and companyname = '"
                                      + comboCompanyName.Text + "' and supplementaryInvoice = 'Supplementary Invoice'");
                            if (Database.myreader.Read())
                                IsSuppMentary = true;
                            Database.myreader.Close();

                            if (IsAvail)
                            {
                                DialogResult dr = MessageBox.Show("ORIGINAL COPY OF INVOICE HAS ALREADY BEEN PRINTED "
                                                   + Environment.NewLine + "DO YOU WANT TO TAKE EXTRA COPY", "Print Invoice",
                                                   MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                                if (dr.CompareTo(DialogResult.Yes) == 0)
                                {
                                    Database.GetExecuteNonQueryCommand("update DomesticInvoice set Header = 'EXTRA COPY' WHERE invno = " + comboInvoice.Text
                                          + " and suffix  = '" + comboyr.Text + "' and companyname = '"
                                          + comboCompanyName.Text + "'");
                                    if (comboCompanyName.Text == "Oswal Extrusion Limited (Unit-II)")
                                    {
                                        Database.myadapter = Database.GetAdapterCommand("select * from Vw_DomesticInvoiceOEL2 where  InvNo = "
                                            + comboInvoice.Text + " and suffix = '" + comboyr.Text + "' and  companyname = '"
                                                             + comboCompanyName.Text + "'  order by commodity desc ,groupcommodity");
                                        DataSet dataset1 = new DataSet();
                                        Database.myadapter.Fill(dataset1, "Vw_DomesticInvoiceOEL2");
                                        DateTime BillDate = Convert.ToDateTime(dataset1.Tables[0].Rows[0]["Invdate"]);
                                        if (BillDate >= Convert.ToDateTime("2017-07-01"))
                                        {
                                            if (BillDate >= Convert.ToDateTime("2018-02-01"))
                                            {
                                                if (Convert.ToString(dataset1.Tables[0].Rows[0]["Commodity"]).Contains("YARN") ||
                                                    Convert.ToString(dataset1.Tables[0].Rows[0]["ItemDesc"]).Contains("YARN")
                                                    || Convert.ToString(dataset1.Tables[0].Rows[0]["state"]).ToUpper() != "Gujarat".ToUpper())
                                                {
                                                    string strgstInv = Convert.ToString(dataset1.Tables[0].Rows[0]["GSTInvNo"]);
                                                    if (!DespatchCommonFunction.IsEwayGenerated(comboCompanyName.Text, comboyr.Text, strgstInv))
                                                    {
                                                        MessageBox.Show("E-Way Bill is not generated");
                                                        return;
                                                    }
                                                }
                                            }

                                            CRDomesticPriceOEL2_GST cr = new CRDomesticPriceOEL2_GST();
                                            cr.SetDataSource(dataset1.Tables[0]);
                                            Tools.ToolsCommonFunction.CrystalReportSetSource(crystalReportViewer1, cr);
                                            int pagecnt = cr.FormatEngine.GetLastPageNumber(new ReportPageRequestContext());
                                            cr.PrintToPrinter(1, false, 1, pagecnt);
                                        }
                                        else
                                        {
                                            CRDomesticPriceOEL2 cr = new CRDomesticPriceOEL2();
                                            cr.SetDataSource(dataset1.Tables[0]);
                                            Tools.ToolsCommonFunction.CrystalReportSetSource(crystalReportViewer1, cr);
                                            int pagecnt = cr.FormatEngine.GetLastPageNumber(new ReportPageRequestContext());
                                            cr.PrintToPrinter(1, false, 1, pagecnt);
                                        }
                                    }
                                    else
                                    {

                                        if (IsSuppMentary)
                                        {
                                            Database.myadapter = Database.GetAdapterCommand("select * from Vw_DomesticInvoice where InvNo = "
                                                      + comboInvoice.Text + " and suffix = '" + comboyr.Text + "' and companyname = '"
                                                      + comboCompanyName.Text + "'  order by commodity desc ,groupcommodity");
                                            DataSet dataset1 = new DataSet();
                                            Database.myadapter.Fill(dataset1, "Vw_DomesticInvoice");
                                            DateTime BillDate = Convert.ToDateTime(dataset1.Tables[0].Rows[0]["Invdate"]);
                                            if (BillDate >= Convert.ToDateTime("2017-07-01"))
                                            {
                                                if (BillDate >= Convert.ToDateTime("2018-02-01"))
                                                {
                                                    if (Convert.ToString(dataset1.Tables[0].Rows[0]["Commodity"]).Contains("YARN") ||
                                                    Convert.ToString(dataset1.Tables[0].Rows[0]["ItemDesc"]).Contains("YARN")
                                                    || Convert.ToString(dataset1.Tables[0].Rows[0]["state"]).ToUpper() != "Gujarat".ToUpper())
                                                    {
                                                        string strgstInv = Convert.ToString(dataset1.Tables[0].Rows[0]["GSTInvNo"]);
                                                        if (!DespatchCommonFunction.IsEwayGenerated(comboCompanyName.Text, comboyr.Text, strgstInv))
                                                        {
                                                            MessageBox.Show("E-Way Bill is not generated");
                                                            return;
                                                        }
                                                    }
                                                }
                                                CRDomesticPriceOEL2_GST cr = new CRDomesticPriceOEL2_GST();
                                                cr.SetDataSource(dataset1.Tables[0]);
                                                Tools.ToolsCommonFunction.CrystalReportSetSource(crystalReportViewer1, cr);
                                                int pagecnt = cr.FormatEngine.GetLastPageNumber(new ReportPageRequestContext());
                                                cr.PrintToPrinter(1, false, 1, pagecnt);
                                                MessageBox.Show("Print Succesfully");
                                            }
                                            else
                                            {
                                                CRDomesticPriceOEL2 cr = new CRDomesticPriceOEL2();
                                                cr.SetDataSource(dataset1.Tables[0]);
                                                Tools.ToolsCommonFunction.CrystalReportSetSource(crystalReportViewer1, cr);
                                                int pagecnt = cr.FormatEngine.GetLastPageNumber(new ReportPageRequestContext());
                                                cr.PrintToPrinter(1, false, 1, pagecnt);
                                            }

                                        }
                                        else
                                        {
                                            Database.myadapter = Database.GetAdapterCommand("select * from Vw_DomesticInvoice where InvNo = "
                                              + comboInvoice.Text + " and suffix = '" + comboyr.Text
                                              + "' and companyname = '" + comboCompanyName.Text
                                              + "'" + MerchantCondition + " order by groupcommodity");
                                            DataSet dataset1 = new DataSet();
                                            Database.myadapter.Fill(dataset1, "Vw_DomesticInvoice");
                                            DateTime BillDate = Convert.ToDateTime(dataset1.Tables[0].Rows[0]["Invdate"]);
                                            if (BillDate >= Convert.ToDateTime("2017-07-01"))
                                            {
                                                if (BillDate >= Convert.ToDateTime("2018-02-01"))
                                                {
                                                    if (Convert.ToString(dataset1.Tables[0].Rows[0]["Commodity"]).Contains("YARN") ||
                                                    Convert.ToString(dataset1.Tables[0].Rows[0]["ItemDesc"]).Contains("YARN")
                                                    || Convert.ToString(dataset1.Tables[0].Rows[0]["state"]).ToUpper() != "Gujarat".ToUpper())
                                                    {
                                                        string strgstInv = Convert.ToString(dataset1.Tables[0].Rows[0]["GSTInvNo"]);
                                                        if (!DespatchCommonFunction.IsEwayGenerated(comboCompanyName.Text, comboyr.Text, strgstInv))
                                                        {
                                                            MessageBox.Show("E-Way Bill is not generated");
                                                            return;
                                                        }
                                                    }
                                                }
                                                CRDomesticPrice_GST cr = new CRDomesticPrice_GST();
                                                cr.SetDataSource(dataset1.Tables[0]);
                                                Tools.ToolsCommonFunction.CrystalReportSetSource(crystalReportViewer1, cr);
                                                int pagecnt = cr.FormatEngine.GetLastPageNumber(new ReportPageRequestContext());
                                                cr.PrintToPrinter(1, false, 1, pagecnt);
                                            }
                                            else
                                            {
                                                CRDomesticPrice cr = new CRDomesticPrice();
                                                cr.SetDataSource(dataset1.Tables[0]);
                                                Tools.ToolsCommonFunction.CrystalReportSetSource(crystalReportViewer1, cr);
                                                int pagecnt = cr.FormatEngine.GetLastPageNumber(new ReportPageRequestContext());
                                                cr.PrintToPrinter(1, false, 1, pagecnt);
                                            }
                                            MessageBox.Show("Print Succesfully");
                                        }
                                    }
                                }
                            }
                            else
                            {
                                DialogResult dr = MessageBox.Show("Do you want to take print of following Domsetic Invoice" + comboInvoice.Text, "Print Invoice",
                                       MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                                if (dr.CompareTo(DialogResult.Yes) == 0)
                                {

                                    //Database.GetExecuteNonQueryCommand("update DomesticInvoice set RemDateTime = '" + System.DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss")
                                    //         + "' WHERE invno = " + comboInvoice.Text
                                    //         + " and suffix  = '" + textSuffix.Text
                                    //         + "' and companyname = '" + comboCompanyName.Text
                                    //         + "'");
                                    for (int i = 1; i <= 4; i++)
                                    {
                                        Database.GetExecuteNonQueryCommand("update DomesticInvoice set Header = '" + Arr[i - 1].ToString()
                                                 + "' WHERE invno = " + comboInvoice.Text
                                                 + " and suffix  = '" + comboyr.Text
                                                 + "' and companyname = '" + comboCompanyName.Text
                                                 + "'");
                                        if (comboCompanyName.Text == "Oswal Extrusion Limited (Unit-II)")
                                        {
                                            Database.myadapter = Database.GetAdapterCommand("select * from Vw_DomesticInvoiceOEL2 where  InvNo = "
                                                + comboInvoice.Text + " and suffix = '" + comboyr.Text + "' and  companyname = '"
                                                                 + comboCompanyName.Text + "'  order by commodity desc ,groupcommodity");
                                            DataSet dataset1 = new DataSet();
                                            Database.myadapter.Fill(dataset1, "Vw_DomesticInvoiceOEL2");
                                            DateTime BillDate = Convert.ToDateTime(dataset1.Tables[0].Rows[0]["Invdate"]);
                                            if (BillDate >= Convert.ToDateTime("2017-07-01"))
                                            {
                                                if (BillDate >= Convert.ToDateTime("2018-02-01"))
                                                {
                                                    if (Convert.ToString(dataset1.Tables[0].Rows[0]["Commodity"]).Contains("YARN") ||
                                                    Convert.ToString(dataset1.Tables[0].Rows[0]["ItemDesc"]).Contains("YARN")
                                                    || Convert.ToString(dataset1.Tables[0].Rows[0]["state"]).ToUpper() != "Gujarat".ToUpper())
                                                    {
                                                        string strgstInv = Convert.ToString(dataset1.Tables[0].Rows[0]["GSTInvNo"]);
                                                        if (!DespatchCommonFunction.IsEwayGenerated(comboCompanyName.Text, comboyr.Text, strgstInv))
                                                        {
                                                            MessageBox.Show("E-Way Bill is not generated");
                                                            return;
                                                        }
                                                    }
                                                }
                                                CRDomesticPriceOEL2_GST cr = new CRDomesticPriceOEL2_GST();
                                                cr.SetDataSource(dataset1.Tables[0]);
                                                Tools.ToolsCommonFunction.CrystalReportSetSource(crystalReportViewer1, cr);
                                                int pagecnt = cr.FormatEngine.GetLastPageNumber(new ReportPageRequestContext());
                                                cr.PrintToPrinter(1, false, 1, pagecnt);
                                            }
                                            else
                                            {
                                                CRDomesticPriceOEL2 cr = new CRDomesticPriceOEL2();
                                                cr.SetDataSource(dataset1.Tables[0]);
                                                Tools.ToolsCommonFunction.CrystalReportSetSource(crystalReportViewer1, cr);
                                                int pagecnt = cr.FormatEngine.GetLastPageNumber(new ReportPageRequestContext());
                                                cr.PrintToPrinter(1, false, 1, pagecnt);
                                            }
                                        }
                                        else
                                        {
                                            if (IsSuppMentary)
                                            {
                                                Database.myadapter = Database.GetAdapterCommand("select * from Vw_DomesticInvoice where InvNo = "
                                                          + comboInvoice.Text + " and suffix = '" + comboyr.Text + "' and companyname = '"
                                                          + comboCompanyName.Text + "'  order by commodity desc ,groupcommodity");
                                                DataSet dataset1 = new DataSet();
                                                Database.myadapter.Fill(dataset1, "Vw_DomesticInvoice");
                                                DateTime BillDate = Convert.ToDateTime(dataset1.Tables[0].Rows[0]["Invdate"]);
                                                if (BillDate >= Convert.ToDateTime("2017-07-01"))
                                                {
                                                    if (BillDate >= Convert.ToDateTime("2018-02-01"))
                                                    {
                                                        if (Convert.ToString(dataset1.Tables[0].Rows[0]["Commodity"]).Contains("YARN") ||
                                                    Convert.ToString(dataset1.Tables[0].Rows[0]["ItemDesc"]).Contains("YARN")
                                                    || Convert.ToString(dataset1.Tables[0].Rows[0]["state"]).ToUpper() != "Gujarat".ToUpper())
                                                        {
                                                            string strgstInv = Convert.ToString(dataset1.Tables[0].Rows[0]["GSTInvNo"]);
                                                            if (!DespatchCommonFunction.IsEwayGenerated(comboCompanyName.Text, comboyr.Text, strgstInv))
                                                            {
                                                                MessageBox.Show("E-Way Bill is not generated");
                                                                return;
                                                            }
                                                        }
                                                    }
                                                    CRSuppDomesticPrice_GST cr = new CRSuppDomesticPrice_GST();
                                                    cr.SetDataSource(dataset1.Tables[0]);
                                                    Tools.ToolsCommonFunction.CrystalReportSetSource(crystalReportViewer1, cr);
                                                    int pagecnt = cr.FormatEngine.GetLastPageNumber(new ReportPageRequestContext());
                                                    cr.PrintToPrinter(1, false, 1, pagecnt);
                                                }
                                                else
                                                {
                                                    CRSuppDomesticPrice cr = new CRSuppDomesticPrice();
                                                    cr.SetDataSource(dataset1.Tables[0]);
                                                    Tools.ToolsCommonFunction.CrystalReportSetSource(crystalReportViewer1, cr);
                                                    int pagecnt = cr.FormatEngine.GetLastPageNumber(new ReportPageRequestContext());
                                                    cr.PrintToPrinter(1, false, 1, pagecnt);
                                                }

                                            }
                                            else
                                            {

                                           
                                                Database.myadapter = Database.GetAdapterCommand("select * from Vw_DomesticInvoice where InvNo = "
                                                  + comboInvoice.Text + " and suffix = '" + comboyr.Text
                                                  + "' and companyname = '" + comboCompanyName.Text
                                                  + "' " +MerchantCondition +  " order by groupcommodity");
                                                DataSet dataset1 = new DataSet();
                                                Database.myadapter.Fill(dataset1, "Vw_DomesticInvoice");
                                                DateTime BillDate = Convert.ToDateTime(dataset1.Tables[0].Rows[0]["Invdate"]);
                                                if (BillDate >= Convert.ToDateTime("2017-07-01"))
                                                {
                                                    if (BillDate >= Convert.ToDateTime("2018-02-01"))
                                                    {
                                                        if (Convert.ToString(dataset1.Tables[0].Rows[0]["Commodity"]).Contains("YARN") ||
                                                    Convert.ToString(dataset1.Tables[0].Rows[0]["ItemDesc"]).Contains("YARN")
                                                    || Convert.ToString(dataset1.Tables[0].Rows[0]["state"]).ToUpper() != "Gujarat".ToUpper())
                                                        {
                                                            string strgstInv = Convert.ToString(dataset1.Tables[0].Rows[0]["GSTInvNo"]);
                                                            if (!DespatchCommonFunction.IsEwayGenerated(comboCompanyName.Text, comboyr.Text, strgstInv))
                                                            {
                                                                MessageBox.Show("E-Way Bill is not generated");
                                                                return;
                                                            }
                                                        }
                                                    }
                                                    CRDomesticPrice_GST cr = new CRDomesticPrice_GST();
                                                    cr.SetDataSource(dataset1.Tables[0]);
                                                    Tools.ToolsCommonFunction.CrystalReportSetSource(crystalReportViewer1, cr);
                                                    int pagecnt = cr.FormatEngine.GetLastPageNumber(new ReportPageRequestContext());
                                                    cr.PrintToPrinter(1, false, 1, pagecnt);
                                                }
                                                else
                                                {
                                                    CRDomesticPrice cr = new CRDomesticPrice();
                                                    cr.SetDataSource(dataset1.Tables[0]);
                                                    Tools.ToolsCommonFunction.CrystalReportSetSource(crystalReportViewer1, cr);
                                                    int pagecnt = cr.FormatEngine.GetLastPageNumber(new ReportPageRequestContext());
                                                    cr.PrintToPrinter(1, false, 1, pagecnt);
                                                }
                                            }
                                        }
                                    }

                                    Database.GetExecuteNonQueryCommand("update DomesticInvoice set Header = 'EXTRA COPY' WHERE invno = " + comboInvoice.Text
                                         + " and suffix  = '" + comboyr.Text + "' and companyname = '"
                                         + comboCompanyName.Text + "'");

                                    if (Database.GetExecuteNonQueryCommand("insert into printdomesticinvoice values("
                                        + comboInvoice.Text + ",'" + comboyr.Text
                                        + "','" + Convert.ToString(DateTime.Now.ToString("MM/dd/yyyy"))
                                        + "','" + comboCompanyName.Text + "','"
                                        + FrmMain.UserName
                                        + "')"))
                                        MessageBox.Show("Print Successfully");

                                    string BuyerName = "";
                                    Database.myreader = Database.GetExecuteReaderCommand("select buyerName from Domesticinvoice where InvNo = "
                                            + comboInvoice.Text + " and suffix = '" + comboyr.Text + "' and companyname = '"
                                            + comboCompanyName.Text + "'");
                                    if (Database.myreader.Read())
                                        BuyerName = Database.myreader[0].ToString();
                                    Database.myreader.Close();

                                    string SendMail = "";
                                    bool desptcmail = false;
                                   
                                    string EmailTo = "";
                                    string EmailCc = "";
                                    string EmailBCc = "";
                                    Database.myreader = Database.GetExecuteReaderCommand("select sendMail,EmailTo,EmailCc,EmailBCc from companymaster where companyname = '"
                                         + BuyerName + "'");
                                    if (Database.myreader.Read())
                                    {
                                        SendMail = Database.myreader[0].ToString();
                                        EmailTo = Database.myreader[1].ToString();
                                        EmailCc = Database.myreader[2].ToString();
                                        EmailBCc = Database.myreader[3].ToString();
                                    }
                                    Database.myreader.Close();


                                    if (SendMail == "yes")
                                    {
                                        string Subject = "<html><body> M/s &nbsp; <b>" + BuyerName + "</b></body></html>&nbsp;"
                                                          + "<html><body> This is to inform you that material has been dispatched to you. </body></html>&nbsp;"
                                                          + "<br><br><html><body> <b>The details are as given below of Invoice No: " + comboInvoice.Text + " </b> </body></html>&nbsp;"
                                              + "<HTML><body><color = blue> <b><table width=800 border=2 bgcolor= #E3E4FA ><tr> <td> Commodity </td> <td> Description of Goods  </td> <td> Quantity </td> <td> Rate/Unit </td>  <td>  Assessable Value </td> <td> Total Amt </b></td></tr> "
                                             + Environment.NewLine;
                                        Database.myreader = Database.GetExecuteReaderCommand("select Commodity,ItemDesc,Qty,Unit,Rate,Amount,TotalAmt from Vw_domesticinvoice where InvNo = "
                                            + comboInvoice.Text + " and yr = '" + comboyr.Text + "' and companyname = '"
                                            + comboCompanyName.Text + "' ");
                                        while (Database.myreader.Read())
                                        {
                                            Subject += "<tr> <td>" + Database.myreader[0].ToString() + "</td>";
                                            Subject += "<td>" + Database.myreader[1].ToString() + "</td>";
                                            Subject += "<td>" + Database.myreader[2].ToString() + "</td>";
                                            Subject += "<td>" + Database.myreader[4].ToString() + " --  " + Database.myreader[3].ToString() + "</td>";
                                            Subject += "<td>" + Database.myreader[5].ToString() + "</td>";
                                            Subject += "<td>" + Database.myreader[6].ToString() + "</td> </tr>";

                                        }
                                        Database.myreader.Close();
                                        Subject += "</Table></body></HTML> <br> <html><body> "
                                                         + "<b>This is system generated mail,Please do not revert back on this mail</b></body></html>";


                                        Database.myadapter = Database.GetAdapterCommand("select * from Vw_DomesticInvoice where InvNo = "
                                              + comboInvoice.Text + " and suffix = '" + comboyr.Text
                                              + "' and companyname = '" + comboCompanyName.Text
                                              + "'");
                                        DataSet dataset1 = new DataSet();
                                        Database.myadapter.Fill(dataset1, "Vw_DomesticInvoice");
                                        //CRDestpatchMail cr = new CRDestpatchMail();
                                        CRDomesticPrice_GST cr = new CRDomesticPrice_GST();
                                        cr.SetDataSource(dataset1.Tables[0]);



                                        // Pass in the message information to a new MailMessage
                                        System.Net.Mail.MailMessage msg = new System.Net.Mail.MailMessage("info@champalalgroup.com", EmailTo, "Dispatch Details from " + comboCompanyName.Text, Subject);

                                        msg.Attachments.Add(new Attachment(cr.ExportToStream(ExportFormatType.PortableDocFormat), "Invoice.pdf"));
                                        msg.IsBodyHtml = true;
                                        string[] stringarr = EmailCc.Split(';');
                                        for (int i = 0; i < stringarr.Length; i++)
                                        {
                                            if (EmailCc != null)
                                            {
                                                if (EmailCc.Trim().Length > 0)
                                                {
                                                    MailAddress CCAddress = new MailAddress(stringarr[i]);
                                                    msg.CC.Add(CCAddress);
                                                }
                                            }

                                        }
                                        stringarr = EmailBCc.Split(';');
                                        for (int i = 0; i < stringarr.Length; i++)
                                        {
                                            if (EmailBCc != null)
                                            {
                                                if (EmailBCc.Trim().Length > 0)
                                                {
                                                    MailAddress BCCAddress = new MailAddress(stringarr[i]);
                                                    msg.Bcc.Add(stringarr[i]);
                                                }
                                            }
                                        }




                                        // Create an SmtpClient to send the e-mail
                                        //74.53.39.50
                                        SmtpClient mailClient = new SmtpClient(Utility.MailServerName); // = local machine IP Address

                                        mailClient.UseDefaultCredentials = true;
                                        mailClient.Port = 587;

                                        //mailClient.Credentials = new NetworkCredential("manish@champalalgroup.com", "abcd1234");
                                        mailClient.Credentials = new System.Net.NetworkCredential("manish@champalalgroup.com", "Mani$&22");

                                        mailClient.Send(msg);
                                        MessageBox.Show("mail send succesfully");

                                        msg.Dispose();



                                    }
                                    else
                                    {
                                        if (desptcmail)
                                        {

                                            //       string Subject = "<html><body> M/s &nbsp; <b>" + BuyerName + "</b></body></html>&nbsp;"
                                            //                         + "<html><body> This is to inform you that material has been dispatched to you. </body></html>&nbsp;"
                                            //                         + "<br><br><html><body> <b>The details are as given below of Invoice No: " + comboInvoice.Text + " </b> </body></html>&nbsp;"
                                            //+ "<HTML><body><color = blue> <b><table width=800 border=2 bgcolor= #E3E4FA ><tr> <td> Commodity </td> <td> Description of Goods  </td> <td> Quantity </td> <td> Rate/Unit </td>  <td>  Assessable Value </td> <td> Total Amt </b></td></tr> "
                                            //+ Environment.NewLine;
                                            //       Database.myreader = Database.GetExecuteReaderCommand("select Commodity,ItemDesc,Qty,Unit,Rate,Amount,TotalAmt from Vw_domesticinvoice where InvNo = "
                                            //           + comboInvoice.Text + " and yr = '" + comboyr.Text + "' and companyname = '"
                                            //           + comboCompanyName.Text + "' ");
                                            //       while (Database.myreader.Read())
                                            //       {
                                            //           Subject += "<tr> <td>" + Database.myreader[0].ToString() + "</td>";
                                            //           Subject += "<td>" + Database.myreader[1].ToString() + "</td>";
                                            //           Subject += "<td>" + Database.myreader[2].ToString() + "</td>";
                                            //           Subject += "<td>" + Database.myreader[4].ToString() + " / " + Database.myreader[3].ToString() + "</td>";
                                            //           Subject += "<td>" + Database.myreader[5].ToString() + "</td>";
                                            //           Subject += "<td>" + Database.myreader[6].ToString() + "</td> </tr>";

                                            //       }
                                            //       Database.myreader.Close();
                                            //       Subject += "</Table></body></HTML> <br> <html><body> "
                                            //                        + "<b>This is system generated mail,Please do not revert back on this mail</b></body></html>";





                                            Database.myadapter = Database.GetAdapterCommand("select * from Vw_DomesticInvoice where InvNo = "
                                                  + comboInvoice.Text + " and suffix = '" + comboyr.Text
                                                  + "' and companyname = '" + comboCompanyName.Text
                                                  + "'");

                                            DataSet dataset1 = new DataSet();
                                            Database.myadapter.Fill(dataset1, "Vw_DomesticInvoice");
                                            CRDestpatchMail cr = new CRDestpatchMail();
                                            cr.SetDataSource(dataset1.Tables[0]);


                                            //    //EmailToDesptch = "rikin@champalalgroup.com";
                                            //    //EmailCcDesptch = "abhishek@champalalgroup.com";

                                            //    FrmSendMail.SendAnEmail_HTML(EmailToDesptch, EmailCcDesptch, "", "Dispatch Details from " + comboCompanyName.Text
                                            //, Subject, "info@champalalgroup.com", false, "");


                                        }

                                    }
                                }
                            }
                            Database.Closeconnection();
                            SENDMAIL();
                        }
                    }
                }
                catch (Exception ex)
                {
                    ErrorMessageBox.Show(ex);
                }
            }

        }

        private void comboCompanyName_SelectedIndexChanged(object sender, EventArgs e)
        {
            comboInvoice.Items.Clear();
            comboInvoice.Text = "";
            if (Database.OpenConnection(Utility.DespacthConnectionString))
            {
                string strsql = "select InvNo from DomesticInvoice where suffix = '"
                    + comboyr.Text + "' and companyname = '" + comboCompanyName.Text + "' and SupplementaryInvoice is null order by InvNo desc";
                DespatchCommonFunction.Loadcombo(comboInvoice, strsql, "InvNo", "InvNo", Utility.DespacthConnectionString);
                Database.Closeconnection();
            }
            if (comboInvoice.Items.Count > 0)
                comboInvoice.SelectedIndex = 0;
        }


        private void button2_Click_1(object sender, EventArgs e)
        {
            try
            {
                if (Database.OpenConnection(Utility.DespacthConnectionString))
                {


                    string BuyerName = "";
                    Database.myreader = Database.GetExecuteReaderCommand("select buyerName from Domesticinvoice where InvNo = "
                            + comboInvoice.Text + " and suffix = '" + comboyr.Text + "' and companyname = '"
                            + comboCompanyName.Text + "'");
                    if (Database.myreader.Read())
                        BuyerName = Database.myreader[0].ToString();
                    Database.myreader.Close();



                    string Subject = "<html><body> M/s &nbsp; <b>" + BuyerName + "</b></body></html>&nbsp;"
                                     + "<html><body> This is to inform you that material has been dispatched to you. </body></html>&nbsp;"
                                     + "<br><br><html><body> <b>The details are as given below of Invoice No: " + comboInvoice.Text + " </b> </body></html>&nbsp;"
                                     + "<HTML><body><color = blue> <b><table width=500 border=2 bgcolor= #E3E4FA ><tr> <td> Commodity </td> <td> Description of Goods  </td> <td> Quantity </td> <td> Rate/Unit </td>  <td>  Assessable Value </b></td></tr> "
                                     + Environment.NewLine;
                    Database.myreader = Database.GetExecuteReaderCommand("select Commodity,ItemDesc,Qty,Unit,Rate,Amount from domesticinvitem where InvNo = "
                        + comboInvoice.Text + " and yr = '" + comboyr.Text + "' and companyname = '"
                        + comboCompanyName.Text + "'");
                    while (Database.myreader.Read())
                    {
                        Subject += "<tr> <td>" + Database.myreader[0].ToString() + "</td>";
                        Subject += "<td>" + Database.myreader[1].ToString() + "</td>";
                        Subject += "<td>" + Database.myreader[2].ToString() + "</td>";
                        Subject += "<td>" + Database.myreader[4].ToString() + " / " + Database.myreader[3].ToString() + "</td>";
                        Subject += "<td>" + Database.myreader[5].ToString() + "</td> </tr>";

                    }
                    Database.myreader.Close();
                    Subject += "</Table></body></HTML> <br> <html><body> "
                                     + "<b>This is system generated mail,Please do no revert back on this mail</b></body></html>";

                    Database.myadapter = Database.GetAdapterCommand("select * from Vw_DomesticInvoice where InvNo = "
                          + comboInvoice.Text + " and suffix = '" + comboyr.Text
                          + "' and companyname = '" + comboCompanyName.Text
                          + "'");
                    DataSet dataset1 = new DataSet();
                    Database.myadapter.Fill(dataset1, "Vw_DomesticInvoice");

                    FrmSendMail.SendAnEmail_HTML("manish@champalalgroup.com", "", "", "Desptach Details from " + comboCompanyName.Text
                        , Subject, "info@champalalgroup.com", false, "");
                }


                Database.Closeconnection();

            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
            }

        }

        private void button2_Click_2(object sender, EventArgs e)
        {
            try
            {
                dataset1.Clear();
                SaveFileDialog saveFileDialog1 = new SaveFileDialog();
                saveFileDialog1.InitialDirectory = Convert.ToString(Environment.SpecialFolder.MyDocuments);
                saveFileDialog1.Filter = "PDF File |*.pdf";
                saveFileDialog1.FilterIndex = 1;
                if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    if (comboCompanyName.Text == "Oswal Extrusion Limited (Unit-II)")
                    {
                        Database.myadapter.Fill(dataset1, "Vw_DomesticInvoiceOEL2");
                        DateTime BillDate = Convert.ToDateTime(dataset1.Tables[0].Rows[0]["InvDate"]);
                        if (BillDate >= Convert.ToDateTime("2017-07-01"))
                        {
                            CRDomesticPriceOEL2_GST cr = new CRDomesticPriceOEL2_GST();
                            cr.SetDataSource(dataset1.Tables[0]);
                            //  Tools.ToolsCommonFunction.CrystalReportSetSource(crystalReportViewer1, cr);

                            string filename = saveFileDialog1.FileName.ToString();

                            // Utility.ExcelExport(dataset1.Tables[0], file, "", true);
                            DiskFileDestinationOptions diskOpts = ExportOptions.CreateDiskFileDestinationOptions();
                            // set export format
                            ExportOptions exportOpts = new ExportOptions();
                            exportOpts.ExportFormatType = ExportFormatType.PortableDocFormat; //PortableDocFormat;
                            exportOpts.ExportDestinationType = ExportDestinationType.DiskFile;
                            //set the disk file options
                            diskOpts.DiskFileName = filename;
                            exportOpts.ExportDestinationOptions = diskOpts;
                            cr.Export(exportOpts);
                        }
                        else
                        {
                            CRDomesticPriceOEL2 cr = new CRDomesticPriceOEL2();
                            cr.SetDataSource(dataset1.Tables[0]);
                            //  Tools.ToolsCommonFunction.CrystalReportSetSource(crystalReportViewer1, cr);

                            string filename = saveFileDialog1.FileName.ToString();

                            // Utility.ExcelExport(dataset1.Tables[0], file, "", true);
                            DiskFileDestinationOptions diskOpts = ExportOptions.CreateDiskFileDestinationOptions();
                            // set export format
                            ExportOptions exportOpts = new ExportOptions();
                            exportOpts.ExportFormatType = ExportFormatType.PortableDocFormat; //PortableDocFormat;
                            exportOpts.ExportDestinationType = ExportDestinationType.DiskFile;
                            //set the disk file options
                            diskOpts.DiskFileName = filename;
                            exportOpts.ExportDestinationOptions = diskOpts;
                            cr.Export(exportOpts);
                        }
                    }
                    else
                    {
                        Database.myadapter.Fill(dataset1, "Vw_DomesticInvoice");
                        DateTime BillDate = Convert.ToDateTime(dataset1.Tables[0].Rows[0]["InvDate"]);
                        if (BillDate >= Convert.ToDateTime("2017-07-01"))
                        {
                            CRDomesticPrice_GST cr = new CRDomesticPrice_GST();
                            cr.SetDataSource(dataset1.Tables[0]);
                            string filename = saveFileDialog1.FileName.ToString();

                            // Utility.ExcelExport(dataset1.Tables[0], file, "", true);
                            DiskFileDestinationOptions diskOpts = ExportOptions.CreateDiskFileDestinationOptions();
                            // set export format
                            ExportOptions exportOpts = new ExportOptions();
                            exportOpts.ExportFormatType = ExportFormatType.PortableDocFormat; //PortableDocFormat;
                            exportOpts.ExportDestinationType = ExportDestinationType.DiskFile;
                            //set the disk file options
                            diskOpts.DiskFileName = filename;
                            exportOpts.ExportDestinationOptions = diskOpts;
                            cr.Export(exportOpts);
                        }
                        else
                        {
                            CRDomesticPrice cr = new CRDomesticPrice();
                            cr.SetDataSource(dataset1.Tables[0]);
                            string filename = saveFileDialog1.FileName.ToString();

                            // Utility.ExcelExport(dataset1.Tables[0], file, "", true);
                            DiskFileDestinationOptions diskOpts = ExportOptions.CreateDiskFileDestinationOptions();
                            // set export format
                            ExportOptions exportOpts = new ExportOptions();
                            exportOpts.ExportFormatType = ExportFormatType.PortableDocFormat; //PortableDocFormat;
                            exportOpts.ExportDestinationType = ExportDestinationType.DiskFile;
                            //set the disk file options
                            diskOpts.DiskFileName = filename;
                            exportOpts.ExportDestinationOptions = diskOpts;
                            cr.Export(exportOpts);
                        }

                    }

                }
            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
            }
        }

        private void comboyr_SelectedIndexChanged(object sender, EventArgs e)
        {
            comboInvoice.Items.Clear();
            comboInvoice.Text = "";
            if (Database.OpenConnection(Utility.DespacthConnectionString))
            {
                string strsql = "select InvNo from DomesticInvoice where suffix = '" + comboyr.Text + "' and companyname = '" + comboCompanyName.Text + "' and SupplementaryInvoice is null order by InvNo desc";
                DespatchCommonFunction.Loadcombo(comboInvoice, strsql, "InvNo", "InvNo", Utility.DespacthConnectionString);
                Database.Closeconnection();
            }
            if (comboInvoice.Items.Count > 0)
                comboInvoice.SelectedIndex = 0;
        }

        private void button3_Click_1(object sender, EventArgs e)
        {
            // dataset1.Tables[0].Dispose();
            dataset1.Clear();
            Database.myadapter = Database.GetAdapterCommand("select * from Vw_DomesticInvoice where InvNo = "
                                                   + comboInvoice.Text + " and suffix = '" + comboyr.Text
                                                   + "' and companyname = '" + comboCompanyName.Text
                                                   + "'");

            Database.myadapter.Fill(dataset1, "Vw_DomesticInvoice"); //Vw_ARE
            if (dataset1.Tables[0].Rows[0]["PLA"].ToString() == "yes")
            {
                CRARE1_UTBOND_NoImport cr = new CRARE1_UTBOND_NoImport();
                cr.SetDataSource(dataset1.Tables[0]);
                Tools.ToolsCommonFunction.CrystalReportSetSource(crystalReportViewer1, cr);
            }
            else
                MessageBox.Show("This invoice is not a Third Party Invoice.");
        }
        private void SENDMAIL()
        {
            try
            {
                if (comboCompanyName.Text == "Plastene India Limited (Unit-IV)")
                {
                    if (Database.OpenConnection(Utility.DespacthConnectionString))
                    {


                        string BuyerName = "";
                        Database.myreader = Database.GetExecuteReaderCommand("select buyerName from Domesticinvoice where InvNo = "
                                + comboInvoice.Text + " and suffix = '" + comboyr.Text + "' and companyname = '"
                                + comboCompanyName.Text + "'");
                        if (Database.myreader.Read())
                            BuyerName = Database.myreader[0].ToString();
                        Database.myreader.Close();



                        string Subject = "<html><body> M/s &nbsp; <b>" + BuyerName + "</b></body></html>&nbsp;"
                                         + "<html><body> This is to inform you that material has been dispatched to you. </body></html>&nbsp;"
                                         + "<br><br><html><body> <b>The details are as given below of Invoice No: " + comboInvoice.Text + " </b> </body></html>&nbsp;"
                                         + "<HTML><body><color = blue> <b><table width=500 border=2 bgcolor= #E3E4FA ><tr> <td> Commodity </td> <td> Description of Goods  </td> <td> Quantity </td> <td> Rate/Unit </td>  <td>  Assessable Value </b></td></tr> "
                                         + Environment.NewLine;
                        Database.myreader = Database.GetExecuteReaderCommand("select Commodity,ItemDesc,Qty,Unit,Rate,Amount from domesticinvitem where InvNo = "
                            + comboInvoice.Text + " and yr = '" + comboyr.Text + "' and companyname = '"
                            + comboCompanyName.Text + "'");
                        while (Database.myreader.Read())
                        {
                            Subject += "<tr> <td>" + Database.myreader[0].ToString() + "</td>";
                            Subject += "<td>" + Database.myreader[1].ToString() + "</td>";
                            Subject += "<td>" + Database.myreader[2].ToString() + "</td>";
                            Subject += "<td>" + Database.myreader[4].ToString() + " / " + Database.myreader[3].ToString() + "</td>";
                            Subject += "<td>" + Database.myreader[5].ToString() + "</td> </tr>";

                        }
                        Database.myreader.Close();
                        Subject += "</Table></body></HTML> <br> <html><body> "
                                         + "<b>This is system generated mail,Please do no revert back on this mail</b></body></html>";

                        Database.myadapter = Database.GetAdapterCommand("select * from Vw_DomesticInvoice where InvNo = "
                              + comboInvoice.Text + " and suffix = '" + comboyr.Text
                              + "' and companyname = '" + comboCompanyName.Text
                              + "'");
                        DataSet dataset1 = new DataSet();
                        Database.myadapter.Fill(dataset1, "Vw_DomesticInvoice");

                        FrmSendMail.SendAnEmail_HTML("prakash@champalalgroup.com", "erp@champalalgroup.com", "", "Desptach Details from " + comboCompanyName.Text
                            , Subject, "info@champalalgroup.com", false, "");
                    }


                    Database.Closeconnection();

                }
            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {

            try
            {
                if (Database.OpenConnection(Utility.DespacthConnectionString))
                {
                    string BuyerName = "";
                    Database.myreader = Database.GetExecuteReaderCommand("select buyerName from Domesticinvoice where InvNo = "
                            + comboInvoice.Text + " and suffix = '" + comboyr.Text + "' and companyname = '"
                            + comboCompanyName.Text + "'");
                    if (Database.myreader.Read())
                        BuyerName = Database.myreader[0].ToString();
                    Database.myreader.Close();

                    string SendMail = "";
                    bool desptcmail = false;
                    string EmailToDesptch = "";
                    string EmailCcDesptch = "";

                    string EmailTo = "";
                    string EmailCc = "";
                    string EmailBCc = "";
                    Database.myreader = Database.GetExecuteReaderCommand("select sendMail,EmailTo,EmailCc,EmailBCc from companymaster where companyname = '"
                         + BuyerName + "'");
                    if (Database.myreader.Read())
                    {
                        SendMail = Database.myreader[0].ToString();
                        EmailTo = Database.myreader[1].ToString();
                        EmailCc = Database.myreader[2].ToString();
                        EmailBCc = Database.myreader[3].ToString();
                    }
                    Database.myreader.Close();


                    Database.myreader = Database.GetExecuteReaderCommand("select Emailto,emailCC from factoryinfo where isemail = 'yes' and name = '"
                        + comboCompanyName.Text + "' ");
                    if (Database.myreader.Read())
                    {
                        EmailToDesptch = Database.myreader[0].ToString();
                        EmailCcDesptch = Database.myreader[1].ToString();
                        desptcmail = true;

                    }
                    Database.myreader.Close();



                    string Subject = "<html><body> M/s &nbsp; <b>" + BuyerName + "</b></body></html>&nbsp;"
                                      + "<html><body> This is to inform you that material has been dispatched to you. </body></html>&nbsp;"
                                      + "<br><br><html><body> <b>The details are as given below of Invoice No: " + comboInvoice.Text + " </b> </body></html>&nbsp;"
                          + "<HTML><body><color = blue> <b><table width=800 border=2 bgcolor= #E3E4FA ><tr> <td> Commodity </td> <td> Description of Goods  </td> <td> Quantity </td> <td> Rate/Unit </td>  <td>  Assessable Value </td> <td> Total Amt </b></td></tr> "
                         + Environment.NewLine;
                    Database.myreader = Database.GetExecuteReaderCommand("select Commodity,ItemDesc,Qty,Unit,Rate,Amount,TotalAmt from Vw_domesticinvoice where InvNo = "
                        + comboInvoice.Text + " and yr = '" + comboyr.Text + "' and companyname = '"
                        + comboCompanyName.Text + "' ");
                    while (Database.myreader.Read())
                    {
                        Subject += "<tr> <td>" + Database.myreader[0].ToString() + "</td>";
                        Subject += "<td>" + Database.myreader[1].ToString() + "</td>";
                        Subject += "<td>" + Database.myreader[2].ToString() + "</td>";
                        Subject += "<td>" + Database.myreader[4].ToString() + " /  " + Database.myreader[3].ToString() + "</td>";
                        Subject += "<td>" + Database.myreader[5].ToString() + "</td>";
                        Subject += "<td>" + Database.myreader[6].ToString() + "</td> </tr>";

                    }
                    Database.myreader.Close();
                    Subject += "</Table></body></HTML> <br> <html><body> "
                                     + "<b>This is system generated mail,Please do not revert back on this mail</b></body></html>";





                    Database.myadapter = Database.GetAdapterCommand("select * from Vw_DomesticInvoice where InvNo = "
                          + comboInvoice.Text + " and suffix = '" + comboyr.Text
                          + "' and companyname = '" + comboCompanyName.Text
                          + "'");
                    DataSet dataset1 = new DataSet();
                    Database.myadapter.Fill(dataset1, "Vw_DomesticInvoice");
                    //CRDestpatchMail cr = new CRDestpatchMail();
                    CRDomesticPrice_GST cr = new CRDomesticPrice_GST();
                    cr.SetDataSource(dataset1.Tables[0]);

                    System.Net.Mail.MailMessage msg = new System.Net.Mail.MailMessage("info@champalalgroup.com", "manish@champalalgroup.com");

                    msg.Attachments.Add(new Attachment(cr.ExportToStream(ExportFormatType.PortableDocFormat), "Invoice.pdf"));
                    msg.IsBodyHtml = true;
                    msg.Body = Subject;
                    msg.Subject =  "Dispatch Details from " + comboCompanyName.Text;
                 
  
                    string[] stringarr = EmailCc.Split(';');
                    for (int i = 0; i < stringarr.Length; i++)
                    {
                        if (EmailCc != null)
                        {
                            if (EmailCc.Trim().Length > 0)
                            {
                                MailAddress CCAddress = new MailAddress(stringarr[i]);
                                msg.CC.Add(CCAddress);
                            }
                        }

                    }
                    stringarr = EmailBCc.Split(';');
                    for (int i = 0; i < stringarr.Length; i++)
                    {
                        if (EmailBCc != null)
                        {
                            if (EmailBCc.Trim().Length > 0)
                            {
                                MailAddress BCCAddress = new MailAddress(stringarr[i]);
                                msg.Bcc.Add(stringarr[i]);
                            }
                        }
                    }




                    // Create an SmtpClient to send the e-mail
                    //74.53.39.50
                    SmtpClient mailClient = new SmtpClient(Utility.MailServerName); // = local machine IP Address

                    mailClient.UseDefaultCredentials = true;
                    mailClient.Port = 587;

                    //mailClient.Credentials = new NetworkCredential("manish@champalalgroup.com", "abcd1234");
                    mailClient.Credentials = new System.Net.NetworkCredential("manish@champalalgroup.com", "Mani$&22");
                 //   mailClient.EnableSsl = true;  
                    mailClient.Send(msg);
                    MessageBox.Show("mail send succesfully");

                    msg.Dispose();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void checkMerchantExport_CheckedChanged(object sender, EventArgs e)
        {
            comboInvoice.Items.Clear();
            comboInvoice.Text = "";

            string MerchantCondition = " and MerchantExport ='";
            if (checkMerchantExport.Checked)
                MerchantCondition += "yes" + "'";
            else
                MerchantCondition = " and isnull(MerchantExport,'no') = 'no'";

            if (Database.OpenConnection(Utility.DespacthConnectionString))
            {
                string strsql = "select InvNo from DomesticInvoice where suffix = '"
                    + comboyr.Text + "' and companyname = '" + comboCompanyName.Text + "' and SupplementaryInvoice is null " + MerchantCondition + "  order by InvNo desc";
                DespatchCommonFunction.Loadcombo(comboInvoice, strsql, "InvNo", "InvNo", Utility.DespacthConnectionString);
                Database.Closeconnection();
            }
            if (comboInvoice.Items.Count > 0)
                comboInvoice.SelectedIndex = 0;
        }
    }
}