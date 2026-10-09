using System;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Collections;
using System.Data;
using System.Net.Mail;
using System.Net;
using System.Text;
//using System.DirectoryServices;
//using System.DirectoryServices.ActiveDirectory;
using System.Data.SqlClient;
using System.ComponentModel;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
namespace ERP
{
    /// <summary>
    /// Common Connection related Database
    /// </summary>
    public class Utility
    {
        //// depreceated
        //public static string[] ServerIPList = new string[] {            
        //    "192.168.0.88,3445",
        //    "180.211.107.118,3445",
        //    "180.211.107.114,3445"
        //};

        // Server IP List with User Name and Password
        // [IP, UserName, Password]
        // no password
        //new string[] {"192.168.0.88,3445", "sa", ""},
        //new string[] {"180.211.107.118,3445", "sa", ""},            
        //new string[] {"180.211.107.114,3445", "sa", ""},
        private static string[][] ServerIPUPList = new string[][] {           
            // with password
            //new string[] {"192.168.0.88,3445", "sa", dudv.Properties.Settings.Default.Password1.ToString()},
            //new string[] {"180.211.107.118,3445", "sa", (dudv.Properties.Settings.Default.Password1.ToString())},
            //new string[] {"124.123.122.132", "sa", (dudv.Properties.Settings.Default.Password1.ToString())}

            
          
            new string[] {"103.240.33.122,3445", "sa", dudv.Properties.Settings.Default.Password1.ToString()},
          
            new string[] {"180.211.107.118,3445", "sa", dudv.Properties.Settings.Default.Password1.ToString()},
            new string[] {"180.211.107.118,3445", "sa", (dudv.Properties.Settings.Default.Password1.ToString())},
            new string[] {"124.123.122.132", "sa", (dudv.Properties.Settings.Default.Password1.ToString())},
            new string[] {"180.211.107.118,3445", "sa", dudv.Properties.Settings.Default.Password1.ToString()},
             
            //  new string[] {"192.168.0.251,2882", "sa", dudv.Properties.Settings.Default.Password1.ToString()},
            //new string[] {"180.211.107.116,2882", "sa", (dudv.Properties.Settings.Default.Password1.ToString())}
            
        };

        public static string ServerIP = "103.240.33.122,3445";//"127.0.0.1"
        public static string ServerPort = "3445";//"1433";
        public static string UserID = "sa";
        //public static string Password = "Pil#Osl#Ahd@123$%^&*()iop";//Pil#Ahd@123$%^
        public static string Password = "Pil#Osl#Ahd@123$%^&*()iop";

        //public static string ServerIP = "192.168.0.251,2882";//"127.0.0.1"
        //public static string ServerPort = "2882";//"1433";
        //public static string UserID = "sa";
        //public static string Password = "Mani$&22";//Pil#Ahd@123$%^
        private const string _DECSecurityKey = "ErpTeamPilOsl#15#11#*()";

        /// <summary>
        /// Default Server Time Out value
        /// </summary>
        public static string ServerTimeOut = "0";

        public static int RollIndex = 1000;
        public static bool isgrid = false;
        static int RollIndexNo = 0;

        /// <summary>
        /// Null Date Value
        /// </summary>
        public static DateTime nullDate = new DateTime(1900, 01, 01);

        /// <summary>
        /// Connection String for Database Material Processing
        /// </summary>
        public static string MaterialConnectionString = "Data Source =" + ServerIP + ";Initial Catalog=materialprocessing;User Id=" + UserID + ";Password=" + Password + ";Connection Timeout = " + ServerTimeOut; // "server=manish;database=loginentry;User Id=sa;Password =;";

        /// <summary>
        /// Connection String for Database Despatch
        /// </summary>
        public static string DespacthConnectionString = "Data Source =" + ServerIP + ";Initial Catalog=Despatch;User Id=" + UserID + ";Password=" + Password + ";Connection Timeout = " + ServerTimeOut; // "server=manish;database=loginentry;User Id=sa;Password =;";//public static string DespacthConnectionString = "Data Source = 180.211.107.114,3445;Initial Catalog=Despatch;User Id=sa;Password=;Connection Timeout = " + ServerTimeOut; // "server=manish;database=loginentry;User Id=sa;Password =;";


        public static string ProductionConnectionString = "Data Source =" + ServerIP + ";Initial Catalog=Production;User Id=" + UserID + ";Password=" + Password + ";Connection Timeout = " + ServerTimeOut; // "server=manish;database=loginentry;User Id=sa;Password =;";//public static string DespacthConnectionString = "Data Source = 180.211.107.114,3445;Initial Catalog=Despatch;User Id=sa;Password=;Connection Timeout = " + ServerTimeOut; // "server=manish;database=loginentry;User Id=sa;Password =;";


        /// <summary>
        /// Mail Server Name
        /// </summary>
        public static string MailServerName = "mail.champalalgroup.com"; //173.192.77.216

        private static BackgroundWorker bg;

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        [DllImport("user32.dll")]
        static extern bool IsWindowVisible(IntPtr hWnd);
        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        [DllImport("user32.dll")]
        static extern IntPtr GetDlgItem(IntPtr hWnd, int DlgItem);

        static int SW_HIDE = 0;
        static int SW_SHOW = 5;
        public static IntPtr TaskBarWnd;

        public static void HideWIndow()
        {
            TaskBarWnd = FindWindow("Shell_TrayWnd", null);
            if (IsWindowVisible(TaskBarWnd))
            {
                ShowWindow(FindWindow("Shell_TrayWnd", null), SW_HIDE);
                ShowWindow(GetDlgItem(FindWindow("Shell_TrayWnd", null), 0x130), SW_HIDE);
            }
        }

        public static void showWindow()
        {
            ShowWindow(TaskBarWnd, SW_SHOW);
            ShowWindow(GetDlgItem(FindWindow("Shell_TrayWnd", null), 0x130), SW_SHOW);
        }

        public static void InitConnectionString()
        {
            try
            {
                List<SqlConnection> _conns = new List<SqlConnection>();
                List<System.Threading.Thread> _threads = new List<System.Threading.Thread>();

                string WhiteIPServer = "";
                string WhiteIPUserName = "";
                string WhiteIPPassword = "";

                foreach (string[] ipc in Utility.ServerIPUPList)
                {
                    //ip = [0]
                    //username = [1]
                    //password = [2]
                    string pwd = ipc[2].ToString();
                    //string sConnString = "Data Source =" + ipc[0] + ";Initial Catalog=materialprocessing;User Id=" + ipc[1] + ";Password=" + DecryptCipherTextToPlainText(pwd) + ";Connection Timeout = " + ServerTimeOut;

                    string sConnString = "Data Source =" + ipc[0] + ";Initial Catalog=materialprocessing;User Id=" + ipc[1] + ";Password= Pil#Osl#Ahd@123$%^&*()iop;Connection Timeout = " + ServerTimeOut;
                    
                    SqlConnection c = new SqlConnection(sConnString);

                    System.Threading.Thread t = new System.Threading.Thread(delegate()
                    {
                        try
                        {
                            c.Open();
                        }
                        catch { }
                    });

                    _threads.Add(t);
                    _conns.Add(c);

                    t.Start();
                }

                while (true)
                {
                    bool isRunning = false;
                    for (int i = 0; i < _threads.Count; i++)
                    {
                        System.Threading.Thread t = _threads[i];
                        SqlConnection c = _conns[i];

                        if (t.ThreadState != System.Threading.ThreadState.Running)
                        {
                            if (c.State == ConnectionState.Open)
                            {
                                //WhiteIPServer = ServerIPList[i];
                                WhiteIPServer = ServerIPUPList[i][0];
                                WhiteIPUserName = ServerIPUPList[i][1];
                                WhiteIPPassword = ServerIPUPList[i][2];
                                break;
                            }
                        }
                        else
                        {
                            isRunning = true;
                        }
                    }

                    if (!isRunning)
                        break;
                }

                if (WhiteIPServer.Length > 0)
                {
                    Utility.ServerIP = WhiteIPServer;
                    Utility.UserID = WhiteIPUserName;
                    Utility.Password = DecryptCipherTextToPlainText(WhiteIPPassword);
                    //Utility.MaterialConnectionString = "Data Source =" + WhiteIPServer + ";Initial Catalog=materialprocessing;User Id=" + WhiteIPUserName + ";Password=" + DecryptCipherTextToPlainText(WhiteIPPassword) + ";Connection Timeout = " + ServerTimeOut;
                    //Utility.DespacthConnectionString = "Data Source =" + WhiteIPServer + ";Initial Catalog=Despatch;User Id=" + WhiteIPUserName + ";Password=" + DecryptCipherTextToPlainText(WhiteIPPassword) + ";Connection Timeout = " + ServerTimeOut;
                    //Utility.ProductionConnectionString = "Data Source =" + WhiteIPServer + ";Initial Catalog=Production;User Id=" + WhiteIPUserName + ";Password=" + DecryptCipherTextToPlainText(WhiteIPPassword) + ";Connection Timeout = " + ServerTimeOut;
                    //UtilityPayroll.ConnectionString = "Data Source = " + WhiteIPServer + ";Initial Catalog=loginentry;User Id=" + WhiteIPUserName + ";Password=" + DecryptCipherTextToPlainText(WhiteIPPassword) + ";Connection Timeout = ";

                    //Utility.MaterialConnectionString = "Data Source =" + WhiteIPServer + ";Initial Catalog=materialprocessing;User Id=" + WhiteIPUserName + ";Password=Pil#Osl#Ahd@123$%^&*()iop;Connection Timeout = " + ServerTimeOut;
                    //Utility.DespacthConnectionString = "Data Source =" + WhiteIPServer + ";Initial Catalog=Despatch;User Id=" + WhiteIPUserName + ";Password=Pil#Osl#Ahd@123$%^&*()iop;Connection Timeout = " + ServerTimeOut;
                    //Utility.ProductionConnectionString = "Data Source =" + WhiteIPServer + ";Initial Catalog=Production;User Id=" + WhiteIPUserName + ";Password=Pil#Osl#Ahd@123$%^&*()iop;Connection Timeout = " + ServerTimeOut;
                    //UtilityPayroll.ConnectionString = "Data Source = " + WhiteIPServer + ";Initial Catalog=loginentry;User Id=" + WhiteIPUserName + ";Password=Pil#Osl#Ahd@123$%^&*()iop;Connection Timeout = ";


                    //Utility.MaterialConnectionString = @"data source=192.168.0.147,2882;initial catalog=MaterialProcessing;user id=sa;password=PlastOswal#@123$%^&*()iop";
                    //UtilityPayroll.ConnectionString = @"data source=192.168.0.147,2882;initial catalog=loginentry;user id=sa;password=PlastOswal#@123$%^&*()iop";
                    //Utility.DespacthConnectionString = @"data Source=192.168.0.147,2882;Initial Catalog=Despatch;User ID=sa;Password=PlastOswal#@123$%^&*()iop";
                    //Utility.ProductionConnectionString = @"data Source=192.168.0.147,2882;Initial Catalog=Production;User ID=sa;Password=PlastOswal#@123$%^&*()iop";

                    Utility.MaterialConnectionString = @"data source=103.240.33.122,3445;initial catalog=MaterialProcessing;user id=sa;password=Pil#Osl#Ahd@123$%^&*()iop";
                    UtilityPayroll.ConnectionString = @"data source=103.240.33.122,3445;initial catalog=loginentry;user id=sa;password=Pil#Osl#Ahd@123$%^&*()iop";
                    Utility.DespacthConnectionString = @"data Source=103.240.33.122,3445;Initial Catalog=Despatch;User ID=sa;Password=Pil#Osl#Ahd@123$%^&*()iop";
                    Utility.ProductionConnectionString = @"data Source=103.240.33.122,3445;Initial Catalog=Production;User ID=sa;Password=Pil#Osl#Ahd@123$%^&*()iop";

                    //Utility.MaterialConnectionString = @"data source=103.240.33.122,3445;initial catalog=MaterialProcessing;user id=sa;password=Pil#Osl#Ahd@123$%^&*()iop";
                    //UtilityPayroll.ConnectionString = @"data source=103.240.33.122,3445;initial catalog=loginentry;user id=sa;password=Pil#Osl#Ahd@123$%^&*()iop";
                    //Utility.DespacthConnectionString = @"data Source=103.240.33.122,3445;Initial Catalog=Despatch;User ID=sa;Password=Pil#Osl#Ahd@123$%^&*()iop";
                    //Utility.ProductionConnectionString = @"data Source=103.240.33.122,3445;Initial Catalog=Production;User ID=sa;Password=Pil#Osl#Ahd@123$%^&*()iop";
               
                }

                for (int i = 0; i < _threads.Count; i++)
                {
                    System.Threading.Thread t = _threads[i];
                    SqlConnection c = _conns[i];

                    c.Close();
                    c.Dispose();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        /// <summary>
        /// This method is used to convert the plain text to Encrypted/Un-Readable Text format.
        /// </summary>
        /// <param name="PlainText">Plain Text to Encrypt before transferring over the network.</param>
        /// <returns>Cipher Text</returns>
        //private static string EncryptPlainTextToCipherText(string PlainText)
        //{
        //    //Getting the bytes of Input String.
        //    byte[] toEncryptedArray = UTF8Encoding.UTF8.GetBytes(PlainText);

        //    MD5CryptoServiceProvider objMD5CryptoService = new MD5CryptoServiceProvider();

        //    //Gettting the bytes from the Security Key and Passing it to compute the Corresponding Hash Value.
        //    byte[] securityKeyArray = objMD5CryptoService.ComputeHash(UTF8Encoding.UTF8.GetBytes(_DECSecurityKey));

        //    //De-allocatinng the memory after doing the Job.
        //    objMD5CryptoService.Clear();

        //    var objTripleDESCryptoService = new TripleDESCryptoServiceProvider();

        //    //Assigning the Security key to the TripleDES Service Provider.
        //    objTripleDESCryptoService.Key = securityKeyArray;

        //    //Mode of the Crypto service is Electronic Code Book.
        //    objTripleDESCryptoService.Mode = CipherMode.ECB;

        //    //Padding Mode is PKCS7 if there is any extra byte is added.
        //    objTripleDESCryptoService.Padding = PaddingMode.PKCS7;

        //    var objCrytpoTransform = objTripleDESCryptoService.CreateEncryptor();

        //    //Transform the bytes array to resultArray
        //    byte[] resultArray = objCrytpoTransform.TransformFinalBlock(toEncryptedArray, 0, toEncryptedArray.Length);

        //    //Releasing the Memory Occupied by TripleDES Service Provider for Encryption.
        //    objTripleDESCryptoService.Clear();

        //    //Convert and return the encrypted data/byte into string format.
        //    return Convert.ToBase64String(resultArray, 0, resultArray.Length);
        //}


        /// <summary>
        /// This method is used to convert the Cipher/Encypted text to Plain Text.
        /// </summary>
        /// <param name="CipherText">Encrypted Text</param>
        /// <returns>Plain/Decrypted Text</returns>
        private static string DecryptCipherTextToPlainText(string CipherText)
        {
            byte[] toEncryptArray = Convert.FromBase64String(CipherText);

            MD5CryptoServiceProvider objMD5CryptoService = new MD5CryptoServiceProvider();

            //Gettting the bytes from the Security Key and Passing it to compute the Corresponding Hash Value.
            byte[] securityKeyArray = objMD5CryptoService.ComputeHash(UTF8Encoding.UTF8.GetBytes(_DECSecurityKey));

            //De-allocatinng the memory after doing the Job.
            objMD5CryptoService.Clear();

            var objTripleDESCryptoService = new TripleDESCryptoServiceProvider();

            //Assigning the Security key to the TripleDES Service Provider.
            objTripleDESCryptoService.Key = securityKeyArray;

            //Mode of the Crypto service is Electronic Code Book.
            objTripleDESCryptoService.Mode = CipherMode.ECB;

            //Padding Mode is PKCS7 if there is any extra byte is added.
            objTripleDESCryptoService.Padding = PaddingMode.PKCS7;

            var objCrytpoTransform = objTripleDESCryptoService.CreateDecryptor();

            //Transform the bytes array to resultArray
            byte[] resultArray = objCrytpoTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);

            //Releasing the Memory Occupied by TripleDES Service Provider for Decryption.          
            objTripleDESCryptoService.Clear();

            //Convert and return the decrypted data/byte into string format.
            return UTF8Encoding.UTF8.GetString(resultArray);
        }
        // Not required
        public static void IncreaseValueByone(ref string value)
        {
            string frontstring = value.Substring(0, 3);
            string numericstring = value.Substring(3, 5);
            string format = "";
            int i = Convert.ToInt32(numericstring);
            i = i + 1;
            if (i < 10)
                format = "0000";
            else if (i < 100)
                format = "000";
            else if (i < 1000)
                format = "00";
            else if (i < 10000)
                format = "0";

            value = frontstring + format + i.ToString();
        }

        // Not required
        public static void IncreaseValueByone(ref string value, int substringindex)
        {
            string frontstring = value.Substring(0, substringindex);
            string numericstring = value.Substring(substringindex, 5);
            string format = "";
            int i = Convert.ToInt32(numericstring);
            i = i + 1;
            if (i < 10)
                format = "0000";
            else if (i < 100)
                format = "000";
            else if (i < 1000)
                format = "00";
            else if (i < 10000)
                format = "0";

            value = frontstring + format + i.ToString();
        }

        // Not rquired
        public static void IncreaseValueByone_tradinginvoice(ref string value, int substringindex)
        {
            string frontstring = value.Substring(0, substringindex);
            string numericstring = value.Substring(substringindex, 3);
            string format = "";
            int i = Convert.ToInt32(numericstring);
            i = i + 1;
            if (i < 10)
                format = "00";
            else if (i < 100)
                format = "0";


            value = frontstring + format + i.ToString();
        }


        public static int GetTotalMonth(DateTime dt1, DateTime dt2)
        {
            int tMonth = 0;
            int tYear = 0;

            if ((dt2 - dt2).Ticks > 0)
            {
                DateTime dtTemp = dt1;
                dt1 = dt2;
                dt2 = dtTemp;
            }

            tYear = dt2.Year - dt1.Year;
            tMonth = dt2.Month - dt1.Month;
            if (dt2.Day < dt1.Day) tMonth--;
            if (tMonth < 0)
            {
                tMonth += 12;
                tYear--;
            }
            tMonth = tMonth + tYear * 12;

            return tMonth;
        }

        /// Replace by GetTotalMonth
        //public static int CalculateTotalMonths(DateTime d1, DateTime d2)
        //{
        //    int totalMonths = 0;
        //    if (d1.Year != d2.Year)
        //    {
        //        totalMonths = d2.Month - d1.Month + (12 * (d2.Year - d1.Year));
        //    }
        //    else
        //    {
        //        d1 = new DateTime(d1.Year, d1.Month, d1.Day, d1.Hour, d1.Minute, d1.Second);
        //        d2 = new DateTime(d2.Year, d2.Month, d2.Day, d2.Hour, d2.Minute, d2.Second);
        //        d1 = d1.AddMonths(1);
        //        TimeSpan span = d2 - d1;
        //        while (span.TotalDays >= 0)
        //        {
        //            totalMonths++;
        //            d1 = d1.AddMonths(1);
        //            span = d2 - d1;
        //        }
        //    }
        //    return (totalMonths);
        //}

        // Not required // for checking Try float.TryParse

        /// Not required // Float.TryParse
        //public static bool CheckValueOfFloatingNumber(string st)
        //{
        //    try
        //    {
        //        double t = Convert.ToDouble(st);
        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        ex.ToString();
        //        return false;
        //    }
        //}

        /// Not required // To get Days in year use DateTime.IsLeapYear(year)?366:365
        //public static int GetDaysInYear(int intYear, System.DayOfWeek dayOfWeek)
        //{
        //    int[] intMonthDays = { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };
        //    if (System.DateTime.IsLeapYear(intYear))
        //    {
        //        intMonthDays[1] = 29;
        //    }
        //    int intCounter = 1;
        //    int intDays = 0;
        //    int j = 0;
        //    while (j < intMonthDays.GetLength(0))
        //    {
        //        while (intCounter < System.Convert.ToInt32(intMonthDays[j] + 1))
        //        {
        //            if (System.Convert.ToDateTime(intYear + "-" + j + 1 + "-" + intCounter).DayOfWeek == dayOfWeek)
        //            {
        //                intDays += 1;
        //            }
        //            intCounter += 1;
        //        }
        //        intCounter = 1;
        //        j += 1;
        //    }
        //    return intDays;
        //}

        /// Not in use
        //public static System.Drawing.Color GetBackColor(string loginname)
        //{
        //    int colorname = 0;
        //    System.Drawing.Color cr = System.Drawing.Color.LightBlue;
        //    if (Database.OpenConnection(Utility.MaterialConnectionString))
        //    {
        //        Database.myreader = Database.GetExecuteReaderCommand("select colorname from backcolor where loginname = '" + loginname + "'");
        //        while (Database.myreader.Read())
        //            colorname = Convert.ToInt32(Database.myreader[0].ToString());
        //        Database.myreader.Close();
        //        Database.Closeconnection();
        //    }
        //    if (colorname != 0)
        //        cr = System.Drawing.Color.FromArgb(colorname);
        //    return cr;
        //}

        /// Not required
        //public static DateTime ConvertStrintToDate(string dt)
        //{
        //    string st = dt;
        //    string[] st1 = st.Split('/');

        //    DateTime dt1 = new DateTime(Convert.ToInt32(st1[2]),
        //                                      Convert.ToInt32(st1[0]),
        //                                      Convert.ToInt32(st1[1]));

        //    return dt1;
        //}

        /// Not required
        //public static DateTime ConvertStrintToDateTime(string dt)
        //{
        //    string st = dt;
        //    string[] st1;
        //    if (st.Contains("/"))
        //        st1 = st.Split('/');
        //    else
        //        st1 = st.Split('-');
        //    int year = Convert.ToInt32(st1[2].Substring(0, 4));

        //    string[] st2 = st.Split(':');
        //    int Hour = Convert.ToInt32(st2[0].Substring(10, 3));
        //    int Minute = Convert.ToInt32(st2[1]);
        //    int Second = Convert.ToInt32(st2[2]);

        //    DateTime dt1 = new DateTime(year, Convert.ToInt32(st1[0]), Convert.ToInt32(st1[1]), Hour, Minute, Second);
        //    return dt1;
        //}

        public static void UserInformation(string Operation, string UserName, string Remarks)
        {
            if (Database.OpenConnection(UtilityPayroll.ConnectionString))
            {
                Database.GetExecuteNonQueryCommand("INSERT INTO UserTask VALUES('" + UserName + "',GETDATE() " +
                                   " ,'" + Operation
                                       + "','" + Remarks + "')");
                Database.Closeconnection();
            }
        }

        public static void UserInformation(string Operation, string Remarks)
        {
            UserInformation(Operation, FrmMain.UserName, Remarks);
        }

        /// Not Required use DateTime.DaysInMonth(year, month) for get Days in month
        //public static int GetDaysInMonth(int intYear, int intMonth, System.DayOfWeek dayOfWeek)
        //{
        //    int[] intMonthDays = { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };
        //    if (System.DateTime.IsLeapYear(intYear))
        //    {
        //        intMonthDays[1] = 29;
        //    }
        //    int intCounter = 1;
        //    int intDays = 0;
        //    while (intCounter < System.Convert.ToInt32(intMonthDays[intMonth - 1] + 1))
        //    {
        //        if (System.Convert.ToDateTime(intYear + "-" + intMonth + "-" + intCounter).DayOfWeek == dayOfWeek)
        //        {
        //            intDays += 1;
        //        }
        //        intCounter += 1;
        //    }
        //    intCounter = 1;
        //    return intDays;
        //}

        public static string NumericToWord(long inNumeric)
        {
            bool isNegative = false;
            string inWord = "";
            string[] Word3 = new string[] { "Hundred", "Thousand", "Lac", "Crore", "Arab" };

            if (inNumeric == 0) return "Zero Only";
            if (inNumeric < 0)
            {
                isNegative = true;
                inNumeric = (-inNumeric);
            }

            const int Hundred = 100;
            const int Thousand = 1000;
            const int Lac = 1000 * 100;
            const int Carore = 1000 * 100 * 100;
            const int Arub = 1000 * 100 * 100 * 100;

            const long OutOfRange = 100000000000;
            if (inNumeric > OutOfRange) return "Out of range";

            if (inNumeric >= Arub)
            {
                //if (inWord.Length > 0) inWord += " ";
                inWord += NumericToWord2((int)(inNumeric / Arub)) + " Arab";
                inNumeric %= Arub;
            }
            if (inNumeric >= Carore)
            {
                if (inWord.Length > 0) inWord += " ";
                inWord += NumericToWord2((int)(inNumeric / Carore)) + " Crore";
                inNumeric %= Carore;
            }
            if (inNumeric >= Lac)
            {
                if (inWord.Length > 0) inWord += " ";
                inWord += NumericToWord2((int)(inNumeric / Lac)) + " Lac";
                inNumeric %= Lac;
            }
            if (inNumeric >= Thousand)
            {
                if (inWord.Length > 0) inWord += " ";
                inWord += NumericToWord2((int)(inNumeric / Thousand)) + " Thousand";
                inNumeric %= Thousand;
            }
            if (inNumeric >= Hundred)
            {
                if (inWord.Length > 0) inWord += " ";
                inWord += NumericToWord2((int)(inNumeric / Hundred)) + " Hundred";
                inNumeric %= Hundred;
            }
            if (inNumeric > 0)
            {
                if (inWord.Length > 0) inWord += " ";
                inWord += NumericToWord2((int)(inNumeric));
                //inNumeric %= 1;   // Not Required in long value
            }

            if (isNegative) inWord = "Negative " + inWord;
            inWord += " Only";
            return inWord;
        }
        /// Private use only ( for Numeric To Word)
        private static string NumericToWord2(int inNumeric)
        {
            string inWord = "";
            string[] Word1 = new string[] { "Zero", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten", "Eleven", "Twelve", "Thirteen", "Forteen", "Fifteen", "Sixteen", "Seventeen", "Eighteen", "Nineteen" };
            string[] Word2 = new string[] { "Tweenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety" };
            if (inNumeric == 0) return "Zero";
            if (inNumeric > 0 && inNumeric < 100)
            {
                if (inNumeric < 20) return Word1[inNumeric];
                inWord += Word2[(int)(inNumeric / 10) - 2];
                if (inNumeric % 10 != 0) inWord += " " + Word1[(int)(inNumeric % 10)];
            }
            return inWord;
        }

        /// Replaced by NumericToWord(long inNumeric);
        //public static string IntegerToWords(long inputNum)
        //{
        //    int dig1, dig2, dig3, level = 0, lasttwo, threeDigits;

        //    string retval = "";
        //    string x = "";
        //    string[] ones ={
        //    "Zero",
        //    "One",
        //    "Two",
        //    "Three",
        //    "Four",
        //    "Five",
        //    "Six",
        //    "Seven",
        //    "Eight",
        //    "Nine",
        //    "Ten",
        //    "Eleven",
        //    "Twelve",
        //    "Thirteen",
        //    "Fourteen",
        //    "Fifteen",
        //    "Sixteen",
        //    "Seventeen",
        //    "Eighteen",
        //    "Nineteen"
        //  };
        //    string[] tens ={
        //        "Zero",
        //        "Ten",
        //        "Twenty",
        //        "Thirty",
        //        "Forty",
        //        "Fifty",
        //        "Sixty",
        //        "Seventy",
        //        "Eighty",
        //        "Ninety"
        //    };
        //    string[] thou ={
        //            "",
        //            "Thousand",
        //            "Lac",
        //            "Crore",
        //            "Arub",
        //          };


        //    bool isNegative = false;
        //    if (inputNum < 0)
        //    {
        //        isNegative = true;
        //        inputNum *= -1;
        //    }

        //    if (inputNum == 0)
        //        return ("zero");

        //    string s = inputNum.ToString();

        //    while (s.Length > 0)
        //    {
        //        // Get the three rightmost characters
        //        if (level == 0)
        //            x = (s.Length < 3) ? s : s.Substring(s.Length - 3, 3);
        //        else
        //            x = (s.Length < 2) ? s : s.Substring(s.Length - 2, 2);

        //        // Separate the three digits
        //        threeDigits = int.Parse(x);
        //        lasttwo = threeDigits % 100;
        //        dig1 = threeDigits / 100;
        //        dig2 = lasttwo / 10;
        //        dig3 = (threeDigits % 10);

        //        // append a "thousand" where appropriate
        //        if (level > 0 && dig1 + dig2 + dig3 > 0)
        //        {
        //            retval = thou[level] + " " + retval;
        //            retval = retval.Trim();
        //        }

        //        // check that the last two digits is not a zero
        //        if (lasttwo > 0)
        //        {
        //            if (lasttwo < 20) // if less than 20, use "ones" only
        //                retval = ones[lasttwo] + " " + retval;
        //            else if (dig3 != 0) // otherwise, use both "tens" and "ones" array
        //                retval = tens[dig2] + " " + ones[dig3] + " " + retval;
        //            else
        //                retval = tens[dig2] + " " + retval;

        //        }

        //        // if a hundreds part is there, translate it
        //        if (dig1 > 0 && level < 2)
        //            retval = ones[dig1] + " Hundred " + retval;

        //        if (level == 0)
        //            s = (s.Length - 3) > 0 ? s.Substring(0, s.Length - 3) : "";
        //        else
        //            s = (s.Length - 2) > 0 ? s.Substring(0, s.Length - 2) : "";
        //        level++;
        //    }

        //    while (retval.IndexOf("  ") > 0)
        //        retval = retval.Replace("  ", " ");

        //    retval = retval.Trim();

        //    if (isNegative)
        //        retval = "negative " + retval;
        //    retval = retval + " Only";
        //    return (retval);
        //}


        /// Not required use DateTime.DaysInMonth(year, month) for get Days in month
        //public static ArrayList GetDate_DayInMonth(int intYear, int intMonth, System.DayOfWeek dayOfWeek)
        //{
        //    int[] intMonthDays = { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };
        //    if (System.DateTime.IsLeapYear(intYear))
        //        intMonthDays[1] = 29;
        //    int intCounter = 1;
        //    //int intDays = 0;
        //    ArrayList ArrList = new ArrayList();
        //    while (intCounter < System.Convert.ToInt32(intMonthDays[intMonth - 1] + 1))
        //    {
        //        if (System.Convert.ToDateTime(intYear + "-" + intMonth + "-" + intCounter).DayOfWeek == dayOfWeek)
        //        {
        //            ArrList.Add(System.Convert.ToDateTime(intYear + "-" + intMonth + "-" + intCounter));

        //        }
        //        intCounter += 1;
        //    }
        //    return ArrList;
        //}

        /// New Function when using ExportToExcel... Must check is Office Installed first
        public static bool isOfficeInstalled()
        {
            return Export.isOfficeInstalled();
        }

        /// Repaired
        public static void ExportToExcel(DataTable table, string Filename, bool is2003)
        {
            Export.ToExcel(table, Filename, is2003);
        }

        public static void ExportToExcel(DataTable table, string Filename)
        {
            Export.ToExcel(table, Filename, true);
        }

        public static void ExportToExcel(DataSet dataSet, string Filename)
        {
            Export.ToExcel(dataSet.Tables[0], Filename, true);
        }

        public static void ExportToExcel(DataGridView dgv, string Filename, bool is2003)
        {
            Export.ToExcel(dgv, Filename, is2003);
        }

        public static void ExportToText(DataTable dataTable, string FileName)
        {
            Export.ToText(dataTable, FileName);
        }
        public static void ExportToText(DataGridView dgv, string FileName)
        {
            Export.ToText(dgv, FileName);
        }

        /// Get Date from DD-MM-YYYY Format
        public static DateTime getDateFromDDMMYY(string dString)
        {
            int[] d = new int[3] { 0, 0, 0 };
            int pos = 0;
            for (int i = 0; i < dString.Length; i++)
            {
                if (dString[i] == '/' || dString[i] == '-')
                {
                    pos++;
                    continue;
                }
                else
                {
                    if (dString[i] >= '0' && dString[i] <= '9')
                    {
                        d[pos] = d[pos] * 10 + (Convert.ToInt16(dString[i]) - '0');
                    }
                    else throw new Exception("Invalid Date Format");
                }
            }
            if (d[2] < 100)
            {
                if (d[2] > 75) d[3] += 1900;
                else d[2] += 2000;
            }
            return new DateTime(d[2], d[1], d[0]);
        }

        /// Not required
        //public static string ConvertDateFromDDMMYY_To_MMDDYYYY(string dt)
        //{
        //    string st = dt;
        //    string[] st1 = st.Split('/');

        //    string Date = "";

        //    DateTime dt1 = new DateTime(Convert.ToInt32(st1[2]), Convert.ToInt32(st1[1]), Convert.ToInt32(st1[0]));
        //    if (dt1.Month < 10)
        //        Date = "0" + dt1.Month + "/";
        //    else
        //        Date = dt1.Month + "/";

        //    if (dt1.Day < 10)
        //        Date += "0" + dt1.Day + "/";
        //    else
        //        Date += dt1.Day + "/";

        //    Date += dt1.Year;
        //    return Date;
        //}

        /// Not required
        //public static string ConvertDateFromMMDDYYYY_To_DDMMYY(string dt)
        //{
        //    string st = dt;
        //    string[] st1 = st.Split('/');

        //    string Date = "";
        //    string year = "";
        //    string yearpart = "";
        //    DateTime dt1 = new DateTime(Convert.ToInt32(st1[2]), Convert.ToInt32(st1[0]), Convert.ToInt32(st1[1]));


        //    if (dt1.Day < 10)
        //        Date = "0" + dt1.Day + "-";
        //    else
        //        Date = dt1.Day + "-";
        //    if (dt1.Month < 10)
        //        Date += "0" + dt1.Month + "-";
        //    else
        //        Date += dt1.Month + "-";
        //    year = Convert.ToString(dt1.Year);
        //    yearpart = year.Substring(2, 2);
        //    Date += yearpart;
        //    return Date;
        //}

        /// Not required
        //public static string ConvertDateFromDDMMYY_To_MMDDYYYY_WithoutZero(string dt)
        //{
        //    string st = dt;
        //    string[] st1 = st.Split('/');

        //    string Date = "";

        //    DateTime dt1 = new DateTime(Convert.ToInt32(st1[2]), Convert.ToInt32(st1[1]), Convert.ToInt32(st1[0]));
        //    Date = dt1.Month + "/";
        //    Date += dt1.Day + "/";
        //    Date += dt1.Year;
        //    return Date;
        //}

        /// Not required
        //public static string ConvertDateFromMMDDYY_To_DDMMYYYY(string dt)
        //{
        //    string st = dt;
        //    string[] st1 = st.Split('/');

        //    string Date = "";

        //    DateTime dt1 = new DateTime(Convert.ToInt32(st1[2]), Convert.ToInt32(st1[1]), Convert.ToInt32(st1[0]));
        //    if (dt1.Month < 10)
        //        Date = "0" + dt1.Month + "/";
        //    else
        //        Date = dt1.Month + "/";

        //    if (dt1.Day < 10)
        //        Date += "0" + dt1.Day + "/";
        //    else
        //        Date += dt1.Day + "/";

        //    Date += dt1.Year;
        //    return Date;
        //}

        /// Not required
        //public static string ConvertDateFromDDMMYYHHMMSS_To_MMDDYYYYHHMMSS(string dt)
        //{
        //    string st = dt;

        //    string[] st1 = st.Split('/');
        //    string[] st3 = st1[2].Split(' ');
        //    string[] st2 = st.Split(' ');

        //    string Date = "";

        //    DateTime dt1 = new DateTime(Convert.ToInt32(st3[0]), Convert.ToInt32(st1[1]), Convert.ToInt32(st1[0]));
        //    if (dt1.Month < 10)
        //        Date = "0" + dt1.Month + "/";
        //    else
        //        Date = dt1.Month + "/";

        //    if (dt1.Day < 10)
        //        Date += "0" + dt1.Day + "/";
        //    else
        //        Date += dt1.Day + "/";

        //    Date += dt1.Year;
        //    Date += " " + st2[1];
        //    return Date;
        //}

        /// Not required
        //public static DateTime ConvertDatesFromDDMMYY_To_MMDDYYYY(string dt)
        //{
        //    string st = dt;
        //    string[] st1 = st.Split('/');
        //    DateTime dt1 = new DateTime(Convert.ToInt32(st1[2]), Convert.ToInt32(st1[1]), Convert.ToInt32(st1[0]));
        //    return dt1;
        //}

        /// Not required
        //public static string ConvertDateFromDDMMYY_To_DDMMYYYY(string dt)
        //{
        //    string st = dt;
        //    string[] st1 = st.Split('/');

        //    DateTime dt1 = new DateTime(Convert.ToInt32(st1[2]), Convert.ToInt32(st1[1]), Convert.ToInt32(st1[0]));
        //    return dt1.ToString();
        //}

        /// Replace by GetServerDateTime
        //public static string GetServerDate()
        //{
        //    string Date = "";
        //    Database.OpenConnection(Utility.MaterialConnectionString);
        //    Database.myreader = Database.GetExecuteReaderCommand("SELECT CONVERT(VARCHAR(10), GETDATE(), 103) AS [DD/MM/YYYY]");
        //    if (Database.myreader.Read())
        //        Date = Database.myreader[0].ToString();
        //    Database.myreader.Close();
        //    Database.Closeconnection();
        //    return Date;
        //}

        /// What use of this
        //public static PhysicalAddress GetMACAddress()
        //{
        //    foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
        //    {
        //        // Only consider Ethernet network interfaces
        //        if (nic.NetworkInterfaceType == NetworkInterfaceType.Ethernet &&
        //            nic.OperationalStatus == OperationalStatus.Up)
        //        {
        //            return nic.GetPhysicalAddress();
        //        }
        //    }
        //    return null;
        //}

        /// What use of this
        //public static string GetIPAddress()
        //{
        //    string HostName = Dns.GetHostName();
        //    IPAddress[] Add = Dns.GetHostAddresses(HostName);
        //    return Add[0].ToString();
        //}


        public static string YRfromDate(DateTime dt)
        {

            string yr = "";
            int y = dt.Year % 2000;
            if (dt >= Convert.ToDateTime("2017-07-01 00:00:00") && dt <= Convert.ToDateTime("2018-03-31 23:59:59"))
            {
                if (dt.Month >= 7 && dt.Year == 2017)
                {
                    yr = y.ToString("00") + "&" + (y + 1).ToString("00");
                }
                else if (dt.Month <= 3 && dt.Year == 2018)
                {
                    yr = (y - 1).ToString("00") + "&" + (y).ToString("00");
                }
            }
            else
            {
                if (dt.Month >= 4)
                {
                    yr = y.ToString("00") + "-" + (y + 1).ToString("00");
                }
                else
                {
                    yr = (y - 1).ToString("00") + "-" + (y).ToString("00");
                }
            }
            return yr;
        }

        /// Get Serve DateTime
        public static DateTime GetServerDateTime()
        {
            DateTime dt;
            using (SqlConnection conn = new SqlConnection("Data Source = " + ServerIP + ";User Id=" + UserID + ";Password=" + Password + ""))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand("SELECT GETDATE()", conn))
                {
                    dt = Convert.ToDateTime(cmd.ExecuteScalar());
                    MessageBox.Show(dt.ToString());
                }
                conn.Close();
            }
            return dt;
        }

        /// Replace by GetServerDateTime
        //public static DateTime Currentdate()
        //{
        //    DateTime dt = new DateTime();
        //    Database.OpenConnection(Utility.MaterialConnectionString);
        //    Database.myreader = Database.GetExecuteReaderCommand("select  CAST(FLOOR(CAST(getdate() AS float)) AS datetime)");
        //    if (Database.myreader.Read())
        //        dt = Convert.ToDateTime(Database.myreader[0].ToString());
        //    Database.myreader.Close();
        //    Database.Closeconnection();
        //    return dt;
        //}

        public static bool CheckAccountDateFreeze(DateTime date, string CompanyName)
        {
            DateTime StatMonthDate = new DateTime(date.Year, date.Month, 01);
            DateTime LastMonthDate = new DateTime(date.Year, date.Month, System.DateTime.DaysInMonth(date.Year, date.Month));

            Database.OpenConnection(Utility.MaterialConnectionString);
            //object oFreezeId = Database.GetExecuteScalarCommand_return(string.Format("SELECT TOP 1 transid FROM AccountEntryFreeze WHERE companyname='{0}' AND sysdate >= '{1}' ORDER BY sysdate DESC", CompanyName, date.ToString("yyyy-MM-dd")));
            object oFreezeId = Database.GetExecuteScalarCommand_return(string.Format("SELECT TOP 1 transid FROM AccountEntryFreeze WHERE companyname='{0}' AND " +
                " sysdate between  '" + StatMonthDate.ToString("yyyy-MM-dd") + "' and '" + LastMonthDate.ToString("yyyy-MM-dd") + "' ORDER BY sysdate DESC", CompanyName));
            if (oFreezeId == null)
                return true;
            else
            {
                if (null != Database.GetExecuteScalarCommand_return(string.Format("SELECT username FROM AccountEntryUsers WHERE companyname='{0}' AND username='{1}' AND id='{2}'", CompanyName, FrmMain.UserName, oFreezeId)))
                    return true;
                else
                    return false;
            }
        }

        public static bool CheckAccountVoucherDateFreeze(DateTime date, string CompanyName, string groupname)
        {
            DateTime StatMonthDate = new DateTime(date.Year, date.Month, 01);            
            int Dayinmonth = System.DateTime.DaysInMonth(date.Year, date.Month);
            DateTime LastMonthDate = new DateTime(date.Year, date.Month, Dayinmonth); //add condition as per month wise we closed period for entry level

            Database.OpenConnection(Utility.MaterialConnectionString);
            object oFreezeId = Database.GetExecuteScalarCommand_return(string.Format("SELECT * FROM VoucherTypePrdlock WHERE Companyname= '{0}' AND " +
                 " LockDate between '" + StatMonthDate.ToString("yyyy-MM-dd") + "' and '" + LastMonthDate.ToString("yyyy-MM-dd") + "' and  groupname='{1}' ", CompanyName, groupname));
            if (oFreezeId == null) //Allow to do entry for that date
                return true;
            else
                return false;

        }

        //public static bool CheckAccountDateFreeze(string dt)
        //{
        //    try
        //    {

        //        bool isFreeze = false;
        //        int id = 0;
        //        bool isUser = false;
        //        Database.OpenConnection(Utility.MaterialConnectionString);
        //        Database.myreader = Database.GetExecuteReaderCommand("select sysdate,transid from AccountEntryFreeze  where companyname='"
        //            + frmDefaultVale.CompanyName + "' and  sysdate >= '" + dt + "' order by  sysdate desc ");
        //        if (Database.myreader.Read())
        //        {
        //            isFreeze = true;
        //            id = Convert.ToInt32(Database.myreader[1].ToString());
        //        }

        //        Database.myreader.Close();
        //        if (isFreeze)
        //        {
        //            Database.myreader = Database.GetExecuteReaderCommand("select username from AccountEntryUsers  where companyname='"
        //                + frmDefaultVale.CompanyName + "' and username='" + FrmMain.UserName + "' ");
        //            if (Database.myreader.Read())
        //                isUser = true;
        //            Database.myreader.Close();
        //            if (isUser)
        //                return true;
        //            else
        //            {
        //                MessageBox.Show("This date is freezed. you cant change data.");
        //                return false;
        //            }
        //        }
        //        else
        //            return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        ex.ToString();
        //        return false;
        //    }
        //}

        /// Convert String to Double Safly
        public static double SafeConvertToDouble(string str)
        {
            double iValue = 0;
            try { iValue = Convert.ToDouble(str); }
            catch { }
            return iValue;
        }

        public static double SafeConvertToDouble(object oValue)
        {
            return SafeConvertToDouble(oValue, false);
        }

        public static double SafeConvertToDouble(object oValue, bool ExcludeString)
        {
            double iValue = 0;
            if (oValue is double) iValue = (double)oValue;
            else if (oValue is int) iValue = (double)((int)oValue);
            else if (oValue is float) iValue = (double)((float)oValue);
            else if (oValue is decimal) iValue = Convert.ToDouble((decimal)oValue);
            else if (oValue is DBNull) iValue = 0;
            else if (oValue == null) iValue = 0;
            else if (!ExcludeString) { try { iValue = Convert.ToDouble(oValue); } catch { }; }
            return iValue;
        }

        /// Not Required this function
        //public static string ConverttoInt(string ss)
        //{
        //    try
        //    {
        //        if (ss == "")
        //        {
        //            return "0";
        //        }
        //        else
        //        {
        //            return ss;
        //        }
        //    }
        //    catch (Exception)
        //    {
        //        return null;
        //        throw;
        //    }
        //}

        public static string getrollno(string productioncode, string date, string coname, DataGridView dataGridView1, string tablename)
        {
            try
            {
                DateTime dt = Convert.ToDateTime(date);
                string year = Convert.ToString(dt.Year);
                string yearpart = year.Substring(2, 2);

                //string MonthName = GenerateMonthName(dt.Month);
                //string MergeRoll = yearpart + MonthName;
                string MergeRoll = yearpart + productioncode;
                string RollNo = MergeRoll;
                RollIndex = 0;

                if (Database.OpenConnection(Utility.MaterialConnectionString))
                {


                    Database.myreader = Database.GetExecuteReaderCommand("select rollindex from " + tablename + " with(nolock) where rollno like '"
                               + MergeRoll + "%' and companyname = '" + coname + "' order by rollIndex desc");
                    if (Database.myreader.Read())
                    {
                        RollIndex = Convert.ToInt32(Database.myreader[0].ToString());
                        isgrid = true;

                    }
                    Database.myreader.Close();
                    for (int i = 0; i < dataGridView1.Rows.Count; i++)
                    {
                        if (Convert.ToString(dataGridView1.Rows[i].Cells[0].Value) != "")
                        {
                            string mergerollnew = "";
                            string loomno = "";
                            //loomno = dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[0].Value.ToString();
                            if (Convert.ToInt32(Convert.ToString(dataGridView1.Rows[i].Cells[0].Value)) < 10)
                                loomno = "00" + Convert.ToInt32(Convert.ToString(dataGridView1.Rows[i].Cells[0].Value));
                            else if (Convert.ToInt32(Convert.ToString(dataGridView1.Rows[i].Cells[0].Value)) < 100)
                                loomno = "0" + Convert.ToInt32(Convert.ToString(dataGridView1.Rows[i].Cells[0].Value));
                            else
                                loomno = Convert.ToString(dataGridView1.Rows[i].Cells[0].Value);

                            mergerollnew = MergeRoll + loomno;


                            RollIndex = RollIndex + 1;
                            if (RollIndex < 10)
                                RollNo = mergerollnew + "0000" + RollIndex.ToString();
                            else if (RollIndex < 100)
                                RollNo = mergerollnew + "000" + RollIndex.ToString();
                            else if (RollIndex < 1000)
                                RollNo = mergerollnew + "00" + RollIndex.ToString();
                            else if (RollIndex < 10000)
                                RollNo = mergerollnew + "0" + RollIndex.ToString();
                            else
                                RollNo = mergerollnew + RollIndex.ToString();

                            dataGridView1[1, i].Value = RollNo;


                        }
                    }
                    // Database.Closeconnection();

                }
                return null;
            }
            catch (Exception)
            {
                // ErrorMessageBox.Show(ex);
                return null;
            }

        }


        public static string GetRollNoForOutSideRollEntry(string productioncode, string Prefixcode, string date, string coname, DataGridView dataGridView1, string tablename)
        {
            try
            {
                DateTime dt = Convert.ToDateTime(date);
                string year = Convert.ToString(dt.Year);
                string yearpart = year.Substring(2, 2);

                //string MonthName = GenerateMonthName(dt.Month);
                //string MergeRoll = yearpart + MonthName;
                string MergeRoll = yearpart + productioncode;
                string RollNo = MergeRoll;
                RollIndex = 0;
                if (Database.OpenConnection(Utility.MaterialConnectionString))
                {
                    RollIndexNo = 0;

                    Database.myreader = Database.GetExecuteReaderCommand("select isnull(rollindex,0)'rollindex' from " + tablename + " where rollno like '"
                               + MergeRoll + "%' and companyname = '" + coname + "' order by transid desc");
                    if (Database.myreader.Read())
                    {
                        RollIndex = Convert.ToInt32(Database.myreader[0].ToString());

                        isgrid = true;
                    }

                    RollIndexNo = RollIndex;
                    Database.myreader.Close();
                    for (int i = 0; i < dataGridView1.Rows.Count; i++)
                    {
                        if (Convert.ToString(dataGridView1.Rows[i].Cells[0].Value).Trim() != "")
                        {
                            string mergerollnew = "";

                            /// Never used this variable
                            ///string loomno = "";

                            //loomno = dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[0].Value.ToString();
                            //if (Convert.ToInt32(dataGridView1.Rows[i].Cells[0].FormattedValue.ToString()) < 10)
                            //    loomno = "00" + Convert.ToInt32(dataGridView1.Rows[i].Cells[0].FormattedValue.ToString()).ToString();
                            //else if (Convert.ToInt32(dataGridView1.Rows[i].Cells[0].FormattedValue.ToString()) < 100)
                            //    loomno = "0" + Convert.ToInt32(dataGridView1.Rows[i].Cells[0].FormattedValue.ToString()).ToString();
                            //else
                            //    loomno = Convert.ToInt32(dataGridView1.Rows[i].Cells[0].FormattedValue.ToString()).ToString();
                            //loomno = "0";
                            mergerollnew = MergeRoll;


                            RollIndex = RollIndex + 1;
                            if (RollIndex < 10)
                                RollNo = mergerollnew + Prefixcode + "0000" + RollIndex.ToString();
                            else if (RollIndex < 100)
                                RollNo = mergerollnew + Prefixcode + "000" + RollIndex.ToString();
                            else if (RollIndex < 1000)
                                RollNo = mergerollnew + Prefixcode + "00" + RollIndex.ToString();
                            else if (RollIndex < 10000)
                                RollNo = mergerollnew + Prefixcode + "0" + RollIndex.ToString();
                            else
                                RollNo = mergerollnew + Prefixcode + RollIndex.ToString();
                            if (tablename.ToString().Trim() == "MISOutsideRollEntry")
                                dataGridView1[2, i].Value = RollNo;
                            else
                                dataGridView1[1, i].Value = RollNo;
                        }
                    }
                    // Database.Closeconnection();

                }
                return null;
            }
            catch //(Exception ex)
            {
                return null;
                //ErrorMessageBox.Show(ex);
            }

        }

        public static void SumAndCount(DataGridView dgv)
        {
            SumAndCount(dgv, ((StatusBar)dgv.FindForm().Controls["statusBar1"]).Panels["statusBarPanel2"]);
        }

        public static void SumAndCount(DataGridView dgv, StatusBarPanel sBar)
        {
            if (bg != null)
            {
                if (bg.IsBusy)
                    bg.CancelAsync();

                bg.Dispose();

                bg = null;
            }

            bg = new BackgroundWorker();
            bg.WorkerSupportsCancellation = true;
            bg.DoWork += (s, e) =>
            {
                double cSum = 0;
                int cCount = dgv.SelectedCells.Count;
                int ActualCount = 0;

                //List<int> BlackColumns = new List<int>();

                //if (cCount < 2000)
                {
                    try
                    {
                        for (int i = 0; i < cCount && i < dgv.SelectedCells.Count; i++)
                        {
                            if (!dgv.SelectedCells[i].Visible)
                            {
                                continue;
                            }

                            ActualCount++;

                            //if (BlackColumns.Exists(x => x == dgv.SelectedCells[i].ColumnIndex))
                            //    continue;

                            object cValue = dgv.SelectedCells[i].Value;
                            if (cValue == null) continue;
                            if (cValue is DateTime) continue;
                            else if (cValue is double) cSum += (double)cValue;
                            else if (cValue is int) cSum += (int)cValue;
                            else if (cValue is float) cSum += (float)cValue;
                            else if (cValue is decimal) cSum += Convert.ToDouble((decimal)cValue);
                            else
                            {
                                try
                                {
                                    cSum += Convert.ToDouble(cValue);
                                }
                                catch
                                {
                                    //BlackColumns.Add(dgv.SelectedCells[i].ColumnIndex);
                                };
                            }

                            if (i % 1000 == 0)
                            {
                                int progress = (i * 100) / cCount;
                                if (sBar.Parent.InvokeRequired)
                                    sBar.Parent.Invoke(new MethodInvoker(delegate() { sBar.Text = "Count : " + cCount + "     Sum : Calculating...(" + progress + "%)"; }));
                                else
                                    sBar.Text = "Count : " + cCount + "     Sum : Calculating...(" + progress + "%)";
                            }
                        }
                    }
                    catch { }
                }

                if (sBar.Parent.InvokeRequired)
                    sBar.Parent.Invoke(new MethodInvoker(delegate() { sBar.Text = "Count : " + ActualCount.ToString() + "     Sum : " + cSum.ToString("#,##0.00", System.Globalization.CultureInfo.CreateSpecificCulture("gu-IN")); }));
                else
                    sBar.Text = "Count : " + ActualCount.ToString() + "     Sum : " + cSum.ToString("#,##0.00", System.Globalization.CultureInfo.CreateSpecificCulture("gu-IN"));
            };

            bg.RunWorkerAsync();
        }

        ////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////
        public static void AllowFloatOnly(KeyPressEventArgs e) { Validation.AllowFloatOnly(e); }
        public static void AllowDecimalOnly(KeyPressEventArgs e) { Validation.AllowDecimalOnly(e); }

        ////////////////////////////////////////////////////////////////////
        // Delete in future
        ////////////////////////////////////////////////////////////////////
        public static string ConvertDateFromDDMMYY_To_MMDDYYYY(string dt)
        {
            string st = dt;
            string[] st1 = st.Split('/');

            string Date = "";

            DateTime dt1 = new DateTime(Convert.ToInt32(st1[2]), Convert.ToInt32(st1[1]), Convert.ToInt32(st1[0]));
            if (dt1.Month < 10)
                Date = "0" + dt1.Month + "/";
            else
                Date = dt1.Month + "/";

            if (dt1.Day < 10)
                Date += "0" + dt1.Day + "/";
            else
                Date += dt1.Day + "/";

            Date += dt1.Year;
            return Date;
        }

        public static string IntegerToWords(long inputNum)
        {
            int dig1, dig2, dig3, level = 0, lasttwo, threeDigits;

            string retval = "";
            string x = "";
            string[] ones ={
            "Zero",
            "One",
            "Two",
            "Three",
            "Four",
            "Five",
            "Six",
            "Seven",
            "Eight",
            "Nine",
            "Ten",
            "Eleven",
            "Twelve",
            "Thirteen",
            "Fourteen",
            "Fifteen",
            "Sixteen",
            "Seventeen",
            "Eighteen",
            "Nineteen"
          };
            string[] tens ={
                "Zero",
                "Ten",
                "Twenty",
                "Thirty",
                "Forty",
                "Fifty",
                "Sixty",
                "Seventy",
                "Eighty",
                "Ninety"
            };
            string[] thou ={
                    "",
                    "Thousand",
                    "Lac",
                    "Crore",
                    "Arub",
                  };


            bool isNegative = false;
            if (inputNum < 0)
            {
                isNegative = true;
                inputNum *= -1;
            }

            if (inputNum == 0)
                return ("zero");

            string s = inputNum.ToString();

            while (s.Length > 0)
            {
                // Get the three rightmost characters
                if (level == 0)
                    x = (s.Length < 3) ? s : s.Substring(s.Length - 3, 3);
                else
                    x = (s.Length < 2) ? s : s.Substring(s.Length - 2, 2);

                // Separate the three digits
                threeDigits = int.Parse(x);
                lasttwo = threeDigits % 100;
                dig1 = threeDigits / 100;
                dig2 = lasttwo / 10;
                dig3 = (threeDigits % 10);

                // append a "thousand" where appropriate
                if (level > 0 && dig1 + dig2 + dig3 > 0)
                {
                    retval = thou[level] + " " + retval;
                    retval = retval.Trim();
                }

                // check that the last two digits is not a zero
                if (lasttwo > 0)
                {
                    if (lasttwo < 20) // if less than 20, use "ones" only
                        retval = ones[lasttwo] + " " + retval;
                    else if (dig3 != 0) // otherwise, use both "tens" and "ones" array
                        retval = tens[dig2] + " " + ones[dig3] + " " + retval;
                    else
                        retval = tens[dig2] + " " + retval;

                }

                // if a hundreds part is there, translate it
                if (dig1 > 0 && level < 2)
                    retval = ones[dig1] + " Hundred " + retval;

                if (level == 0)
                    s = (s.Length - 3) > 0 ? s.Substring(0, s.Length - 3) : "";
                else
                    s = (s.Length - 2) > 0 ? s.Substring(0, s.Length - 2) : "";
                level++;
            }

            while (retval.IndexOf("  ") > 0)
                retval = retval.Replace("  ", " ");

            retval = retval.Trim();

            if (isNegative)
                retval = "negative " + retval;
            retval = retval + " Only";
            return (retval);
        }

        public static DateTime ConvertDatesFromDDMMYY_To_MMDDYYYY(string dt)
        {
            string st = dt.Replace("-", "/");
            string[] st1 = st.Split('/');
            DateTime dt1 = new DateTime(Convert.ToInt32(st1[2]), Convert.ToInt32(st1[1]), Convert.ToInt32(st1[0]));
            return dt1;
        }

        public static bool CheckValueOfFloatingNumber(string st)
        {
            try
            {
                double t = Convert.ToDouble(st);
                return true;
            }
            catch (Exception ex)
            {
                ex.ToString();
                return false;
            }
        }

        public static DateTime Currentdate()
        {
            DateTime dt = new DateTime();
            Database.OpenConnection(Utility.MaterialConnectionString);
            Database.myreader = Database.GetExecuteReaderCommand("select  CAST(FLOOR(CAST(getdate() AS float)) AS datetime)");
            if (Database.myreader.Read())
                dt = Convert.ToDateTime(Database.myreader[0].ToString());
            Database.myreader.Close();
            Database.Closeconnection();
            return dt;
        }

        public static void ExcelExport(System.Data.DataTable tblExportData, string TempFilePath, string userInfo, bool headerStatus)
        {

            try
            {

                Microsoft.Office.Interop.Excel.Application excelApp = new Microsoft.Office.Interop.Excel.Application();

                //string ExcelPath = Server.MapPath("~/Excel Template/Brand Creation.xls");

                excelApp.Workbooks.Open(TempFilePath, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing);



                //Writing Header in Excel

                if (headerStatus == true)
                {

                    for (int i = 0; i < tblExportData.Columns.Count; i++)
                    {

                        excelApp.Cells[7, i + 1] = tblExportData.Columns[i].ColumnName;

                    }

                }

                //Writing Data in Excel

                for (int i = 1; i <= tblExportData.Rows.Count; i++)
                {

                    for (int j = 0; j < tblExportData.Columns.Count; j++)
                    {

                        string colName = tblExportData.Columns[j].ColumnName;

                        excelApp.Cells[i + 7, j + 1] = tblExportData.Rows[i - 1][colName];

                    }

                }

                //Create a worksheet object

                Microsoft.Office.Interop.Excel.Sheets sheets = excelApp.Worksheets;

                //from the collection of worksheet select one worksheet 

                Microsoft.Office.Interop.Excel.Worksheet mySheet = (Microsoft.Office.Interop.Excel.Worksheet)sheets.get_Item(1);

                //select the cell in the worksheet

                Microsoft.Office.Interop.Excel.Range myCell1 = (Microsoft.Office.Interop.Excel.Range)mySheet.get_Range("A6", "A6");

                myCell1.Value2 = userInfo;

                myCell1.Font.Color = System.Drawing.Color.Black.ToArgb();


                excelApp.Visible = true;


            }

            catch (Exception ex)
            {
                ex.ToString();
            }

        }

        public static DateTime ConvertStrintToDate(string dt)
        {
            string st = dt;
            string[] st1 = st.Split('/');

            DateTime dt1 = new DateTime(Convert.ToInt32(st1[2]),
                                              Convert.ToInt32(st1[0]),
                                              Convert.ToInt32(st1[1]));

            return dt1;
        }

        public static string ConvertDateFromDDMMYY_To_DDMMYYYY(string dt)
        {
            string st = dt;
            string[] st1 = st.Split('/');

            DateTime dt1 = new DateTime(Convert.ToInt32(st1[2]), Convert.ToInt32(st1[1]), Convert.ToInt32(st1[0]));
            return dt1.ToString();
        }

        public static int CalculateTotalMonths(DateTime d1, DateTime d2)
        {
            int totalMonths = 0;
            if (d1.Year != d2.Year)
            {
                totalMonths = d2.Month - d1.Month + (12 * (d2.Year - d1.Year));
            }
            else
            {
                d1 = new DateTime(d1.Year, d1.Month, d1.Day, d1.Hour, d1.Minute, d1.Second);
                d2 = new DateTime(d2.Year, d2.Month, d2.Day, d2.Hour, d2.Minute, d2.Second);
                d1 = d1.AddMonths(1);
                TimeSpan span = d2 - d1;
                while (span.TotalDays >= 0)
                {
                    totalMonths++;
                    d1 = d1.AddMonths(1);
                    span = d2 - d1;
                }
            }
            return (totalMonths);
        }

        public static void AllowFloat_PlusMinus(KeyPressEventArgs e)
        {
            // this is only allow numeric,backspace and decimal point
            if ((!(Char.IsDigit(e.KeyChar))) && (!(e.KeyChar == '\b')) && (!(e.KeyChar == '.')) && (!(e.KeyChar == '+')) && (!(e.KeyChar == '-')))
                e.Handled = true;
        }

        public static bool SendAnEmail(string MsgTo, string CC, string BCC, string MsgSubject, string MsgBody)
        {

            try
            {
                string myHostName = Dns.GetHostName().ToString();
                string ipAddress = Dns.Resolve(myHostName).AddressList[0].ToString();

                string MsgFrom = myHostName + "@champalalgroup.com";
                MsgFrom = "manish@champalalgroup.com";

                // Pass in the message information to a new MailMessage
                System.Net.Mail.MailMessage msg = new System.Net.Mail.MailMessage(MsgFrom, MsgTo, MsgSubject, MsgBody);

                string[] stringarr = CC.Split(';');
                for (int i = 0; i < stringarr.Length; i++)
                {
                    if (CC != null)
                    {
                        if (CC.Trim().Length > 0)
                        {
                            MailAddress CCAddress = new MailAddress(stringarr[0].Trim());
                            msg.CC.Add(CCAddress);
                        }
                    }
                }

                stringarr = BCC.Split(';');
                for (int i = 0; i < stringarr.Length; i++)
                {
                    if (BCC != null)
                    {
                        if (BCC.Trim().Length > 0)
                        {
                            MailAddress BCCAddress = new MailAddress(BCC.Trim());
                            msg.Bcc.Add(BCCAddress);
                        }
                    }
                }


                //Attachment attachment = new Attachment("c:\\PrintPage.jpg");
                // msg.Attachments.Add(attachment);
                // Create an SmtpClient to send the e-mail
                SmtpClient mailClient = new SmtpClient("173.192.77.216", 25); //mail.champalalgroup.com = local machine IP Address
                //mailClient.UseDefaultCredentials = true;

                mailClient.Credentials = new System.Net.NetworkCredential("manish@champalalgroup.com", "ab12cd34");

                mailClient.Send(msg);
                msg.Dispose();
                return true;
            }
            catch (FormatException ex)
            {
                MessageBox.Show(ex.Message + " :Format Exception");
                return false;
            }
            catch (SmtpException ex)
            {
                MessageBox.Show(ex.Message + " :SMTP Exception");
                return false;
            }


        }

        public static bool SendAnEmail(string MsgTo, string CC, string BCC, string MsgSubject, string MsgBody, string MsgFrom)
        {

            try
            {


                // Pass in the message information to a new MailMessage
                System.Net.Mail.MailMessage msg = new System.Net.Mail.MailMessage(MsgFrom, MsgTo, MsgSubject, MsgBody);

                string[] stringarr = CC.Split(';');
                for (int i = 0; i < stringarr.Length; i++)
                {
                    if (CC != null)
                    {
                        if (CC.Trim().Length > 0)
                        {
                            MailAddress CCAddress = new MailAddress(stringarr[i].Trim());
                            msg.CC.Add(CCAddress);
                        }
                    }
                }

                stringarr = BCC.Split(';');
                for (int i = 0; i < stringarr.Length; i++)
                {
                    if (BCC != null)
                    {
                        if (BCC.Trim().Length > 0)
                        {
                            MailAddress BCCAddress = new MailAddress(stringarr[i].Trim());
                            msg.Bcc.Add(BCCAddress);
                        }
                    }
                }


                //Attachment attachment = new Attachment("c:\\PrintPage.jpg");
                // msg.Attachments.Add(attachment);
                // Create an SmtpClient to send the e-mail
                SmtpClient mailClient = new SmtpClient("173.192.77.216", 25); //mail.champalalgroup.com = local machine IP Address
                //mailClient.UseDefaultCredentials = true;

                mailClient.Credentials = new System.Net.NetworkCredential("manish@champalalgroup.com", "Mani$&22");

                mailClient.Send(msg);
                msg.Dispose();
                return true;
            }
            catch (FormatException ex)
            {
                MessageBox.Show(ex.Message + " :Format Exception");
                return false;
            }
            catch (SmtpException ex)
            {
                MessageBox.Show(ex.Message + " :SMTP Exception");
                return false;
            }


        }

        public static string ConvertDateFromMMDDYY_To_DDMMYYYY(string dt)
        {
            string st = dt;
            string[] st1 = st.Split('/');

            string Date = "";

            DateTime dt1 = new DateTime(Convert.ToInt32(st1[2]), Convert.ToInt32(st1[1]), Convert.ToInt32(st1[0]));
            if (dt1.Month < 10)
                Date = "0" + dt1.Month + "/";
            else
                Date = dt1.Month + "/";

            if (dt1.Day < 10)
                Date += "0" + dt1.Day + "/";
            else
                Date += dt1.Day + "/";

            Date += dt1.Year;
            return Date;
        }

        public static string ConverttoInt(string ss)
        {
            try
            {
                ss = ss.Trim();
                if (ss == "" || ss == ".")
                {
                    return "0";
                }
                else
                {
                    return ss;
                }
            }
            catch (Exception)
            {
                return null;
                throw;
            }
        }

        public static string ConvertDateFromMMDDYYYY_To_DDMMYY(string dt)
        {
            string st = dt;
            string[] st1 = st.Split('/');

            string Date = "";
            string year = "";
            string yearpart = "";
            DateTime dt1 = new DateTime(Convert.ToInt32(st1[2]), Convert.ToInt32(st1[0]), Convert.ToInt32(st1[1]));


            if (dt1.Day < 10)
                Date = "0" + dt1.Day + "-";
            else
                Date = dt1.Day + "-";
            if (dt1.Month < 10)
                Date += "0" + dt1.Month + "-";
            else
                Date += dt1.Month + "-";
            year = Convert.ToString(dt1.Year);
            yearpart = year.Substring(2, 2);
            Date += yearpart;
            return Date;
        }
        public static bool CheckDateFrize(string date, string companyname)
        {
            try
            {
                Database.OpenConnection(Utility.MaterialConnectionString);
                Database.myreader = Database.GetExecuteReaderCommand("select companyname from MaterialProcessing..freezeMRN with(nolock) where  '" + date + "' <="
                    + " (select MAX(sysdate)'Date' from freezeMRN with(nolock) where companyname='" + companyname + "') and "
                    + " CompanyName= '" + companyname + "' ");
                DataTable dt = new DataTable();
                dt.Load(Database.myreader);
                Database.Closeconnection();
                if (dt.Rows.Count > 0)
                    return false;
                else
                    return true;


            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool CheckDateFrize_Production(string date, string companyname)
        {
            try
            {
                Database.OpenConnection(Utility.MaterialConnectionString);
                Database.myreader = Database.GetExecuteReaderCommand("select companyname from MaterialProcessing..freezeProdEntry with(nolock) where  '" + date + "' <="
                    + " (select DATEADD(dd,-2,MAX(sysdate))'Date' from freezeProdEntry with(nolock) where companyname='" + companyname + "') and "
                    + " CompanyName= '" + companyname + "' ");
                DataTable dt = new DataTable();
                dt.Load(Database.myreader);
                Database.Closeconnection();
                if (dt.Rows.Count > 0)
                    return false;
                else
                    return true;


            }
            catch (Exception)
            {
                return false;
            }

        }
        public static DateTime ConvertStrintToDateTime(string dt)
        {
            string st = dt;
            string[] st1;
            if (st.Contains("/"))
                st1 = st.Split('/');
            else
                st1 = st.Split('-');
            int year = Convert.ToInt32(st1[2].Substring(0, 4));

            string[] st2 = st.Split(':');
            int Hour = Convert.ToInt32(st2[0].Substring(10, 3));
            int Minute = Convert.ToInt32(st2[1]);
            int Second = Convert.ToInt32(st2[2]);

            DateTime dt1 = new DateTime(year, Convert.ToInt32(st1[0]), Convert.ToInt32(st1[1]), Hour, Minute, Second);
            return dt1;
        }
        public static bool ChckDate(string dtFrom, string dtTo)
        {
            if (ConvertDatesFromDDMMYY_To_MMDDYYYY(dtFrom.Replace("-", "/")) > ConvertDatesFromDDMMYY_To_MMDDYYYY(dtTo.Replace("-", "/")))
            {
                MessageBox.Show("FROM DATE can not be greater than TO DATE");
                return false;
            }
            else
                return true;
        }

        /////////////////////////////////////////////////////////////
        public static string CheckStoreInwardNo(string FormName, string inwardno, string condition)
        {

            try
            {

                DataTable dt = new DataTable();

                Database.OpenConnection(Utility.MaterialConnectionString);

                if (FormName != "PO Without Indent")
                {



                    Database.myreader = Database.GetExecuteReaderCommand("select Vw_StoreInwards.*,finalquotation.Rate 'PoRate' from Vw_StoreInwards inner join "

                    + " finalquotation on finalquotation.transid = Vw_StoreInwards.purchasetransid where SrNo = '" + inwardno + "'  and BillofLadingNumber = '' and PONo != 'No PO'" + condition);

                    dt.Load(Database.myreader);

                    if (dt.Rows.Count > 0)
                    {

                        MessageBox.Show("Please check Inward no in Multi PO w/o Indent.");

                        Database.myreader.Close();

                        return null;

                    }

                }

                if (FormName != "PO With Indent")
                {

                    Database.myreader = Database.GetExecuteReaderCommand("select Vw_StoreInwards.*,finalquotation.rate 'PoRate' from Vw_StoreInwards inner join "

                    + " finalquotation on finalquotation.subcode = Vw_StoreInwards.IndentSubCode and finalquotation.Purchasecode = Vw_StoreInwards.pono where"

                    + " SrNo = '" + inwardno + "' and BillofLadingNumber = '' and PONo != 'No PO' " + condition + "  order by CAST(RIGHT(IndentSubCode,2) as integer)");

                    dt.Load(Database.myreader);

                    if (dt.Rows.Count > 0)
                    {

                        MessageBox.Show("Please check Inward no in Multi PO.");

                        Database.myreader.Close();

                        return null;

                    }

                }

                if (FormName != "PO Without Indent Import")
                {

                    Database.myreader = Database.GetExecuteReaderCommand("select * from Vw_StoreInwards where SrNo = '"

                                  + inwardno + "' and BillofLadingNumber != '' and PONo != 'No PO'" + condition);

                    dt.Load(Database.myreader);

                    if (dt.Rows.Count > 0)
                    {

                        MessageBox.Show("Please check Inward no in Purchase Invoice.");

                        Database.myreader.Close();

                        return null;

                    }

                }

                if (FormName != "Indent")
                {

                    Database.myreader = Database.GetExecuteReaderCommand("select * from Vw_StoreInwards where SrNo = '"

                                 + inwardno + "' and BillofLadingNumber = '' and PONo != 'No PO' "

                                 + condition + "  order by CAST(RIGHT(IndentSubCode,2) as integer)");

                    dt.Load(Database.myreader);

                    if (dt.Rows.Count > 0)
                    {

                        MessageBox.Show("Please check Inward no in Indent.");

                        Database.myreader.Close();

                        return null;

                    }

                }

                if (FormName != "No PO No Indent")
                {

                    Database.myreader = Database.GetExecuteReaderCommand("select * from Vw_StoreInwards where SrNo = '"

                                  + inwardno + "' and PONo = 'No PO' " + condition + "  order by siindex ");

                    dt.Load(Database.myreader);

                    if (dt.Rows.Count > 0)
                    {

                        MessageBox.Show("Please check Inward no in Without PO/Indent.");

                        Database.myreader.Close();

                        return null;

                    }

                }

                dt.Dispose();

                return null;

            }

            catch (Exception ex)
            {

                ErrorMessageBox.Show(ex);

                return null;

            }
        }

        public static DateTime CastDate(object oDate)
        {
            if (oDate is DBNull) return nullDate;
            return (DateTime)oDate;
        }

        public static void setDateTimePickerValue(DateTimePicker dtp, object oDate)
        {
            if (oDate is DBNull)
            {
                dtp.Value = nullDate;
                dtp.Checked = false;
            }
            else
            {
                dtp.Value = (DateTime)oDate;
                dtp.Checked = true;
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
                    string strsql = "Select * from yearMaster where getdate() and fromdate and todate";
                    System.Data.SqlClient.SqlCommand command = new System.Data.SqlClient.SqlCommand(strsql, conn);
                    System.Data.SqlClient.SqlDataReader reader = command.ExecuteReader();
                    if (reader.Read())
                    {
                        fromdate = Convert.ToDateTime(reader["Fromdate"]);
                        todate = Convert.ToDateTime(reader["Todate"]);
                    }
                    reader.Close();
                    conn.Close();
                    if (fromdate >= dt && todate <= dt)
                        bln = true;
                }
                return bln;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }

    public class UtilityPayroll
    {
        //,3444,

        public static string Password = "";
        public static string ConnectionString = "Data Source = " + Utility.ServerIP + ";Initial Catalog=loginentry;User Id=" + Utility.UserID + ";Password=" + Password + ";Connection Timeout = "; // "server=manish;database=loginentry;User Id=sa;Password =;";
       // public static string ConnectionString = "Data Source=192.168.0.251,2882;Initial Catalog=loginentry;User ID=sa;Password=Mani$&22"; //added by manish for dummy erp 28th feb 2023
        public static string OldPayrollConnectionString = "Data Source=103.240.33.122,3445;Initial Catalog=loginentry;User ID=sa;Password=Pil#Osl#Ahd@123$%^&*()iop";
       // public static string OldPayrollConnectionString = "Data Source=103.240.33.122,3445;Initial Catalog=loginentry;User ID=sa;Password=Pil#Osl#Ahd@123$%^&*()iop";
       
        public static string GetEmpName(string Empcode)
        {
            string EmpName = "";
            SqlConnection conn = new SqlConnection(UtilityPayroll.ConnectionString);
            conn.Open();
            SqlCommand mycommand = new SqlCommand("select Name from EmpInfo where Empcode='" + Empcode + "'", conn);
            EmpName = Convert.ToString(mycommand.ExecuteScalar());
            conn.Close();
            return EmpName;
        }

        public static void ArrayListOfEmpCode(ref ArrayList Arrlist)
        {
            SqlConnection conn = new SqlConnection(UtilityPayroll.ConnectionString);
            conn.Open();
            SqlCommand mycommand = new SqlCommand("select EmpCode from EmpInfo order by empcode", conn);
            SqlDataReader myreader = mycommand.ExecuteReader();
            while (myreader.Read())
                Arrlist.Add(myreader["Empcode"].ToString());
            myreader.Close();
            conn.Close();
        }
        public static string GetEmpCode(string EmpName)
        {
            string Empcode = "";
            SqlConnection conn = new SqlConnection(UtilityPayroll.ConnectionString);
            conn.Open();
            SqlCommand mycommand = new SqlCommand("select empcode from EmpInfo where name='" + EmpName + "'", conn);
            Empcode = Convert.ToString(mycommand.ExecuteScalar()); //.ExecuteReader();
            conn.Close();
            return Empcode;
        }

    }
    public class UtilityMarketing
    {
        public static void GetCompanyName(System.Windows.Forms.ComboBox comboComp)
        {
            try
            {
                if (Database.OpenConnection(Utility.DespacthConnectionString))
                {
                    SqlDataReader myreader = Database.GetExecuteReaderCommand("select distinct(companyname) from companymaster order by companyname");
                    while (myreader.Read())
                        comboComp.Items.Add(myreader["CompanyName"].ToString());
                    myreader.Close();
                    if (comboComp.Items.Count > 0)
                        comboComp.SelectedIndex = 0;
                    Database.Closeconnection();
                }
            }
            catch (Exception ex)
            {
                ex.ToString();
            }
        }
    }
}
