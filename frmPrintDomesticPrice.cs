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


namespace ERP
{
    public partial class frmPrintDomesticPrice : Form
    {
        public frmPrintDomesticPrice()
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
                                      + comboCompanyName.Text + "'  order by commodity desc ,groupcommodity");
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
                                      + comboCompanyName.Text + "'  order by commodity desc ,groupcommodity");
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
                    else
                    {
                        bool IsSuppMentary = false;
                        Database.myreader = Database.GetExecuteReaderCommand("select InvNo from Domesticinvoice where InvNo = "
                                  + comboInvoice.Text + " and suffix = '" + comboyr.Text + "' and companyname = '"
                                  + comboCompanyName.Text + "' and supplementaryInvoice = 'Supplementary Invoice'");
                        if (Database.myreader.Read())
                            IsSuppMentary = true;
                        Database.myreader.Close();

                        if (IsSuppMentary)
                        {
                            dataset1.Clear();
                            Database.myadapter = Database.GetAdapterCommand("select * from Vw_DomesticInvoice where InvNo = "
                                      + comboInvoice.Text + " and suffix = '" + comboyr.Text + "' and companyname = '"
                                      + comboCompanyName.Text + "'  order by commodity desc ,groupcommodity");
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
                                      + comboCompanyName.Text + "'  order by commodity desc ,groupcommodity");
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
                        //  dataset1.Clear();

                        ArrayList Arr = new ArrayList();
                        Arr.Add("ORIGINAL FOR BUYER");
                        Arr.Add("DUPLICATE FOR TRANSPORTER");
                        Arr.Add("TRIPLICATE FOR ASSESSES");
                        Arr.Add("QUADRUPLICATE COPY");
                        Arr.Add("QUINTUPLICATE COPY");
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
                                            MessageBox.Show("Print Succesfully");

                                        }
                                        else
                                        {
                                            Database.myadapter = Database.GetAdapterCommand("select * from Vw_DomesticInvoice where InvNo = "
                                              + comboInvoice.Text + " and suffix = '" + comboyr.Text
                                              + "' and companyname = '" + comboCompanyName.Text
                                              + "' order by groupcommodity");
                                            DataSet dataset1 = new DataSet();
                                            Database.myadapter.Fill(dataset1, "Vw_DomesticInvoice");
                                            DateTime BillDate = Convert.ToDateTime(dataset1.Tables[0].Rows[0]["Invdate"]);
                                            if (BillDate >= Convert.ToDateTime("2017-07-01"))
                                            {
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
                                    for (int i = 1; i <= 5; i++)
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
                                                  + "' order by groupcommodity");
                                                DataSet dataset1 = new DataSet();
                                                Database.myadapter.Fill(dataset1, "Vw_DomesticInvoice");
                                                DateTime BillDate = Convert.ToDateTime(dataset1.Tables[0].Rows[0]["Invdate"]);
                                                if (BillDate >= Convert.ToDateTime("2017-07-01"))
                                                {
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
                                    //string EmailToDesptch = "";
                                    //string EmailCcDesptch = "";

                                    //string EmailTo = "";
                                    //string EmailCc = "";
                                    //string EmailBCc = "";
                                    Database.myreader = Database.GetExecuteReaderCommand("select sendMail,EmailTo,EmailCc,EmailBCc from companymaster where companyname = '"
                                         + BuyerName + "'");
                                    if (Database.myreader.Read())
                                    {
                                        SendMail = Database.myreader[0].ToString();
                                        //EmailTo = Database.myreader[1].ToString();
                                        //EmailCc = Database.myreader[2].ToString();
                                        //EmailBCc = Database.myreader[3].ToString();
                                    }
                                    Database.myreader.Close();


                                    Database.myreader = Database.GetExecuteReaderCommand("select Emailto,emailCC from factoryinfo where isemail = 'yes' and name = '"
                                        + comboCompanyName.Text + "' ");
                                    if (Database.myreader.Read())
                                    {
                                        //EmailToDesptch = Database.myreader[0].ToString();
                                        //EmailCcDesptch = Database.myreader[1].ToString();
                                        desptcmail = true;

                                    }
                                    Database.myreader.Close();

                                    if (SendMail == "yes")
                                    {

                                        //       //string Subject = "M/s" + Environment.NewLine + BuyerName + Environment.NewLine
                                        //       //                 + "This is to inform you that material has been dispatched to you. " + Environment.NewLine
                                        //       //                 + " The details are as given below: " + Environment.NewLine
                                        //       //                 + " Description of Goods              Quantity         Rate/Unit          Assessable Value "
                                        //       //                 + Environment.NewLine;
                                        //       //Database.myreader = Database.GetExecuteReaderCommand("select ItemDesc,Qty,Unit,Rate,Amount from domesticinvitem where InvNo = "
                                        //       //    + comboInvoice.Text + " and yr = '" + textSuffix.Text + "' and companyname = '"
                                        //       //    + comboCompanyName.Text + "'");
                                        //       //while (Database.myreader.Read())
                                        //       //{
                                        //       //    Subject += Database.myreader[0].ToString() + ",  ";
                                        //       //    Subject += Database.myreader[1].ToString() + ",  ";
                                        //       //    Subject += Database.myreader[3].ToString() + " / " + Database.myreader[2].ToString() + " ,   ";
                                        //       //    Subject += Database.myreader[4].ToString() + ",  ";
                                        //       //}
                                        //       //Database.myreader.Close();
                                        //       //Subject += Environment.NewLine + Environment.NewLine + Environment.NewLine + Environment.NewLine
                                        //       //                 + " This is system generated mail,Please do no revert back on this mail";


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

                                        //if (desptcmail)
                                        //{
                                        //    if (EmailTo == "")
                                        //    {
                                        //        EmailTo = EmailToDesptch;
                                        //        if (EmailCc == "")
                                        //            EmailCc += EmailCcDesptch;
                                        //        else
                                        //            EmailCc += ";" + EmailCcDesptch;

                                        //        //EmailTo = "rikin@champalalgroup.com";
                                        //        //EmailCc = "abhishek@champalalgroup.com";

                                        //        FrmSendMail.SendAnEmail_HTML(EmailTo, EmailCc, EmailBCc, "Dispatch Details from " + comboCompanyName.Text
                                        //    , Subject, "info@champalalgroup.com", false, "");


                                        //    }
                                        //    else
                                        //    {
                                        //        if (EmailCc == "")
                                        //            EmailCc += EmailCcDesptch + ";" + EmailToDesptch;
                                        //        else
                                        //            EmailCc += ";" + EmailCcDesptch + ";" + EmailToDesptch;
                                        //        //EmailTo = "rikin@champalalgroup.com";
                                        //        //EmailCc = "abhishek@champalalgroup.com";
                                        //        FrmSendMail.SendAnEmail_HTML(EmailTo, EmailCc, EmailBCc, "Dispatch Details from " + comboCompanyName.Text
                                        //    , Subject, "info@champalalgroup.com", false, "");

                                        //    }
                                        //}
                                        //else
                                        //{
                                        //    //EmailTo = "rikin@champalalgroup.com";
                                        //    //EmailCc = "abhishek@champalalgroup.com";
                                        //    FrmSendMail.SendAnEmail_HTML(EmailTo, EmailCc, EmailBCc, "Dispatch Details from " + comboCompanyName.Text
                                        //        , Subject, "info@champalalgroup.com", false, "");
                                        //}
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

            cmbinv2.Items.Clear();
            cmbinv2.Text = "";

            if (Database.OpenConnection(Utility.DespacthConnectionString))
            {
                string strsql = "select InvNo from DomesticInvoice where suffix = '"
                    + comboyr.Text + "' and companyname = '" + comboCompanyName.Text + "' and SupplementaryInvoice is null order by InvNo desc";
                DespatchCommonFunction.Loadcombo(comboInvoice, strsql, "InvNo", "InvNo", Utility.DespacthConnectionString);
                DespatchCommonFunction.Loadcombo(cmbinv2, strsql, "InvNo", "InvNo", Utility.DespacthConnectionString);
                Database.Closeconnection();
            }
            if (comboInvoice.Items.Count > 0)
                comboInvoice.SelectedIndex = 0;
            if (cmbinv2.Items.Count > 0)
                cmbinv2.SelectedIndex = 0;
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

                        DateTime BillDate = Convert.ToDateTime(dataset1.Tables[0].Rows[0]["Invdate"]);
                        if (BillDate >= Convert.ToDateTime("2017-07-01"))
                        {
                            CRDomesticPriceOEL2_GST cr = new CRDomesticPriceOEL2_GST();
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
                            CRDomesticPriceOEL2 cr = new CRDomesticPriceOEL2();
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
                    else
                    {
                        Database.myadapter.Fill(dataset1, "Vw_DomesticInvoice");
                        DateTime BillDate = Convert.ToDateTime(dataset1.Tables[0].Rows[0]["Invdate"]);
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

            cmbinv2.Items.Clear();
            cmbinv2.Text = "";

            if (Database.OpenConnection(Utility.DespacthConnectionString))
            {
                string strsql = "select InvNo from DomesticInvoice where suffix = '" + comboyr.Text + "' and companyname = '" + comboCompanyName.Text + "' and SupplementaryInvoice is null order by InvNo desc";
                DespatchCommonFunction.Loadcombo(comboInvoice, strsql, "InvNo", "InvNo", Utility.DespacthConnectionString);
                DespatchCommonFunction.Loadcombo(cmbinv2, strsql, "InvNo", "InvNo", Utility.DespacthConnectionString);
                Database.Closeconnection();
            }
            if (comboInvoice.Items.Count > 0)
                comboInvoice.SelectedIndex = 0;
            if (cmbinv2.Items.Count > 0)
                cmbinv2.SelectedIndex = 0;
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

        private void button4_Click(object sender, EventArgs e)
        {
            try
            {
                int nextinv = Convert.ToInt32(comboInvoice.Text);

                for (int i = nextinv; i <= Convert.ToInt32(cmbinv2.Text); i++)
                {
                    Database.myadapter = Database.GetAdapterCommand("select * from Vw_DomesticInvoice where InvNo = "
                                                  + comboInvoice.Text + " and suffix = '" + comboyr.Text
                                                  + "' and companyname = '" + comboCompanyName.Text
                                                  + "' order by groupcommodity");
                    DataSet dataset1 = new DataSet();
                    Database.myadapter.Fill(dataset1, "Vw_DomesticInvoice");
                    DateTime BillDate = Convert.ToDateTime(dataset1.Tables[0].Rows[0]["Invdate"]);
                    if (BillDate >= Convert.ToDateTime("2017-07-01"))
                    {
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

                    nextinv++;
                    comboInvoice.Text = nextinv.ToString();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }


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
    }
}