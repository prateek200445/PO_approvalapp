using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ERP.DataFormat
{
    /// <summary>
    /// Journal Voucher Data Format
    /// </summary>
    public class Journal
    {
        #region Variables
        private int _JournalNo;
        private string OldCompanyName;
        private string OldYear;
        private DateTime _Date;
        private string _year;


     
        public int JournalNo { get { return this._JournalNo; } set { if (this._JournalNo != 0) throw new Exception("Journal No cannot change"); this._JournalNo = value; } }

        public DateTime Date
        {
            get { return this._Date; }
            set { this._Date = value; this._year = Utility.YRfromDate(value); }
        }
        public int companyId;
       
        public string CompanyName;
        public string Narration;
        public string username;
        public string JournalType;
        public bool IsTDS;
        public string MRNNo;
        public string InwardNo;
        public string DrPartyName { get; private set; }
        public string CrPartyName { get; private set; }
        public int DrPartyID { get; private set; }
        public int CrPartyID { get; private set; }
        public string Year { get { return this._year; } set { ;} }
        public DateTime BillDate = Utility.nullDate;
        public string Terms;
        public double TDSAccessableAmount;
        public List<LedgerEntry> LedgerEntries = new List<LedgerEntry>();
        private List<int> InsertedTransId = new List<int>();
        private int _recordLogId;
        public int RecordLogId { get { return this._recordLogId; } private set { this._recordLogId = value; } }

        #endregion

        /// <summary>
        /// Blank Journal Data Format
        /// </summary>
        public Journal()
        {
        }

        /// <summary>
        /// Get Journal Voucher
        /// </summary>
        /// <param name="JournalNo">Journal Number</param>
        /// <param name="CompanyName">Company Name</param>
        /// <param name="Year">Finance Year</param>
        public Journal(int JournalNo, string CompanyName, string Year, bool isMEIS = false)
        {
            if (!Database.OpenConnection(Utility.MaterialConnectionString))
                throw new ERPErrorException("Connection error");

            // Get Data from Journal Table

            if (isMEIS)
                Database.GetExecuteReaderCommand(string.Format("SELECT sysdate, Type, PartyName, Debit, Credit, Narration AS Remark, UserName, narrationFinal AS Narration, JournalType, IsTDS, MRNo, InwardNo, transid, DrPartyName, CrPartyName, yr, Currency, ExchangeRate, BillDate, PaymentTerms AS Terms, TDSAccessableAmount, RecordLogId,SubLedgerName,companyId FROM Journal_temp with(Nolock) WHERE JournalNo='{0}' AND CompanyName='{1}' AND yr='{2}'", JournalNo, CompanyName, Year));
            else
                Database.GetExecuteReaderCommand(string.Format("SELECT sysdate, Type, PartyName, Debit, Credit, Narration AS Remark, UserName, narrationFinal AS Narration, JournalType, IsTDS, MRNo, InwardNo, transid, DrPartyName, CrPartyName, yr, Currency, ExchangeRate, BillDate, PaymentTerms AS Terms, TDSAccessableAmount, RecordLogId,SubLedgerName,companyId FROM Journal  with(Nolock) WHERE JournalNo='{0}' AND CompanyName='{1}' AND yr='{2}'", JournalNo, CompanyName, Year));

            // Entry not found
            if (!Database.myreader.HasRows)
                throw new ERPErrorException("Journal Entry not found in database");

            this._JournalNo = JournalNo;
            this.CompanyName = this.OldCompanyName = CompanyName;
            this.Year = this.OldYear = Year;

            while (Database.myreader.Read())
            {
                this.companyId = Convert.ToInt32(Database.myreader["companyId"]);
                this.Date = (DateTime)Database.myreader["sysdate"];
                this.Narration = Convert.ToString(Database.myreader["Narration"]);
                this.username = Convert.ToString(Database.myreader["UserName"]);
                this.JournalType = Convert.ToString(Database.myreader["JournalType"]);
                this.IsTDS = Convert.ToString(Database.myreader["IsTDS"]).ToLower() == "yes";
                this.MRNNo = Convert.ToString(Database.myreader["MRNo"]);
                this.InwardNo = Convert.ToString(Database.myreader["InwardNo"]);
                this.DrPartyName = Convert.ToString(Database.myreader["DrPartyName"]);
                this.CrPartyName = Convert.ToString(Database.myreader["CrPartyName"]);
                this.BillDate = (DateTime)Database.myreader["BillDate"];
                this.Terms = Convert.ToString(Database.myreader["Terms"]);
                this._year = Convert.ToString(Database.myreader["yr"]);
                this.Year = Convert.ToString(Database.myreader["yr"]);
                this.TDSAccessableAmount = Utility.SafeConvertToDouble(Database.myreader["TDSAccessableAmount"]);
                this.RecordLogId = AccountCommonFunction.SafeConvertToInt(Database.myreader["RecordLogId"]);

                this.LedgerEntries.Add(
                    new LedgerEntry(
                        Convert.ToString(Database.myreader["Type"]),
                        Convert.ToString(Database.myreader["PartyName"]),
                        Utility.SafeConvertToDouble(Database.myreader["Debit"]),
                        Utility.SafeConvertToDouble(Database.myreader["Credit"]),
                        Convert.ToString(Database.myreader["Currency"]),
                        Utility.SafeConvertToDouble(Database.myreader["ExchangeRate"]),
                        Convert.ToString(Database.myreader["Remark"]),
                        (int)Utility.SafeConvertToDouble(Database.myreader["transid"]),
                        Convert.ToString(Database.myreader["SubLedgerName"])
                        )
                    );
            }
            Database.myreader.Close();

            if (isMEIS)
            {
                // Get Data from JournalItem Table
                Database.GetExecuteReaderCommand(string.Format("SELECT SelectionType, BillNo, BillDate, PaidAmount, BillAmount, journaltransid FROM JournalItem_temp  with(Nolock) WHERE JournalNo='{0}' AND CompanyName='{1}' AND yr='{2}'", JournalNo, CompanyName, Year));
                while (Database.myreader.Read())
                {
                    // Get Ledger Item for Bill Allocation
                    LedgerEntry l = this.LedgerEntries.Find(x => x.TransId == (int)Utility.SafeConvertToDouble(Database.myreader["journaltransid"]));

                    // Skip if Ledger Entry not found
                    if (l == null) continue;

                    // Add BillWise Datails
                    l.billWiseDetails.Add(new BillWiseDetail(
                        Convert.ToString(Database.myreader["SelectionType"]),
                        Convert.ToString(Database.myreader["BillNo"]),
                        (DateTime)Database.myreader["BillDate"],
                        Utility.SafeConvertToDouble(Database.myreader["PaidAmount"]),
                        Utility.SafeConvertToDouble(Database.myreader["BillAmount"])
                        ));
                }
                Database.myreader.Close();
            }
            else
            {
                // Get Data from JournalItem Table
                Database.GetExecuteReaderCommand(string.Format("SELECT SelectionType, BillNo, BillDate, PaidAmount, BillAmount, journaltransid FROM JournalItem  with(Nolock) WHERE JournalNo='{0}' AND CompanyName='{1}' AND yr='{2}'", JournalNo, CompanyName, Year));
                while (Database.myreader.Read())
                {
                    // Get Ledger Item for Bill Allocation
                    LedgerEntry l = this.LedgerEntries.Find(x => x.TransId == (int)Utility.SafeConvertToDouble(Database.myreader["journaltransid"]));

                    // Skip if Ledger Entry not found
                    if (l == null) continue;

                    // Add BillWise Datails
                    l.billWiseDetails.Add(new BillWiseDetail(
                        Convert.ToString(Database.myreader["SelectionType"]),
                        Convert.ToString(Database.myreader["BillNo"]),
                        (DateTime)Database.myreader["BillDate"],
                        Utility.SafeConvertToDouble(Database.myreader["PaidAmount"]),
                        Utility.SafeConvertToDouble(Database.myreader["BillAmount"])
                        ));

                    
                }
                Database.myreader.Close();
            }
            // Get Data from JournalInventory
            Database.GetExecuteReaderCommand(string.Format("SELECT GroupName, Qty, Rate, Amount, journaltransid,Unit,Itemcode FROM JournalInventory  with(Nolock) WHERE JournalNo='{0}' AND CompanyName='{1}' AND yr='{2}'", JournalNo, CompanyName, Year));
            while (Database.myreader.Read())
            {
                // Get Ledger Item for Inventory Allocation
                LedgerEntry l = this.LedgerEntries.Find(x => x.TransId == (int)Utility.SafeConvertToDouble(Database.myreader["journaltransid"]));

                // Skip if Ledger Entry not found
                if (l == null) continue;

                // Add Inventory Datails
                l.inventoryDetails.Add(new InventoryDetail(
                    Convert.ToString(Database.myreader["GroupName"]),
                    Utility.SafeConvertToDouble(Database.myreader["Qty"]),
                    Utility.SafeConvertToDouble(Database.myreader["Rate"]),
                    Utility.SafeConvertToDouble(Database.myreader["Amount"]),
                    Convert.ToString(Database.myreader["unit"]),
                    Convert.ToString(Database.myreader["ItemCode"])
                    ));
            }
            Database.myreader.Close();

            // Get Data from Cost Centre
            Database.GetExecuteReaderCommand(string.Format("SELECT cce.CostCenterId, cce.Amount, cce.srno FROM CostCenterEntries cce  with(Nolock) INNER JOIN Journal j  with(Nolock) on j.transid=cce.Srno WHERE cce.VoucherType='Journal' AND j.JournalNo='{0}' AND j.CompanyName='{1}' AND j.yr='{2}'", JournalNo, CompanyName, Year));
            while (Database.myreader.Read())
            {
                // Get Ledger Item for Cost Allocation
                LedgerEntry l = this.LedgerEntries.Find(x => x.TransId == AccountCommonFunction.SafeConvertToInt(Database.myreader["srno"]));

                // Skip if Ledger Entry not found
                if (l == null) continue;

                // Add Cost Datails
                l.CostCenterDetails.Add(new CostCenterDetail(
                    AccountCommonFunction.SafeConvertToInt(Database.myreader["CostCenterId"]),
                    Utility.SafeConvertToDouble(Database.myreader["Amount"]) * (l.Type.ToLower() == "dr" ? 1 : -1) // DR CR
                    ));
            }
            Database.myreader.Close();
        }

        private double TotalDebit()
        {
            return this.LedgerEntries.Sum(x => x.Debit);
        }

        private double TotalCredit()
        {
            return this.LedgerEntries.Sum(x => x.Credit);
        }

        /// <summary>
        /// Save Journal Voucher
        /// </summary>
        /// <returns>True on success</returns>
        public bool Save()
        {
            if (!Database.OpenConnection(Utility.MaterialConnectionString))
                throw new ERPErrorException("Connection Error");

            if (Math.Round(this.TotalDebit(), 2, MidpointRounding.AwayFromZero) != Math.Round(this.TotalCredit(), 2, MidpointRounding.AwayFromZero))
                throw new ERPErrorException("Total Credit amount must be match with Total Debit");

            //this._JournalNo = this.GetNewJournalNo();
            Database.BeginTransaction();
            try
            {
                this._InsertJournalEntry();

                string transids = string.Join(",", this.InsertedTransId.Select(x => x.ToString()).ToArray());

                this._JournalNo = this.GetNewJournalNo();

                this.RecordLogId = AccountCommonFunction.SaveRecordLog(null, FrmMain.UserName, RecordLogFlag.Regular, string.Format("Create: Journal Entry {0} Saved in {1} for {2}", this._JournalNo, this.CompanyName, this.Year));

                Database.GetExecuteNonQueryCommand(string.Format("update Journal set JournalNo={0}, RecordLogId={2} where transid in ({1})", this._JournalNo, transids, this.RecordLogId));
                Database.GetExecuteNonQueryCommand(string.Format("update JournalItem set JournalNo={0} where journaltransid in ({1})", this._JournalNo, transids));
                Database.GetExecuteNonQueryCommand(string.Format("update JournalInventory set JournalNo={0} where journaltransid in ({1})", this._JournalNo, transids));

                Database.ExecuteNonQuery("sp_ac_UpdateVoucherParty", System.Data.CommandType.StoredProcedure,
                    new Database.Parameter("@datefrom", this.Date),
                    new Database.Parameter("@dateto", this.Date),
                    new Database.Parameter("@companyname", this.CompanyName),
                    new Database.Parameter("@VoucherTypeGroup", "journal"),
                    new Database.Parameter("@VoucherNo", this.JournalNo)
                    );

                // Save Log
                AccountCommonFunction.SaveUserLog("Save", string.Format("Journal Entry {0} is Saved for {1} in yr {2}", this._JournalNo, this.CompanyName, this.Year));

                Database.CommitTransaction();
            }
            catch (Exception ex)
            {
                Database.RollBackTransaction();
                throw ex;
            }

            return true;
        }

        /// <summary>
        /// Update Journal Voucher
        /// </summary>
        /// <returns>True on success</returns>
        public bool Update()
        {
            if (this.OldCompanyName != this.CompanyName)
                throw new ERPErrorException("CompanyName will not be change");

            if (!Database.OpenConnection(Utility.MaterialConnectionString))
                throw new ERPErrorException("Connection Error");

            if (this.Year != this.OldYear)
                throw new ERPErrorException("Date must in same financial year");

            if (Math.Round(this.TotalDebit(), 2, MidpointRounding.AwayFromZero) != Math.Round(this.TotalCredit(), 2, MidpointRounding.AwayFromZero))
                throw new ERPErrorException("Total Credit amount must be match with Total Debit");

            Database.BeginTransaction();

            try
            {
                this._Delete(false);

                if (this.Year != this.OldYear)
                    this._JournalNo = this.GetNewJournalNo();

                this._InsertJournalEntry();

                if (this.RecordLogId == 0)
                {
                    this.RecordLogId = AccountCommonFunction.SaveRecordLog(null, FrmMain.UserName, RecordLogFlag.Regular, string.Format("RecordLogId Add: Journal {0} in {1} for {2}", this.JournalNo, this.CompanyName, this.Year));
                    Database.GetExecuteNonQueryCommand(string.Format("update Journal set RecordLogId={0} where JournalNo={1} and yr='{2}' and CompanyName='{3}'", this.RecordLogId, this.JournalNo, this.Year, this.CompanyName));
                }

                AccountCommonFunction.SaveRecordLog(this.RecordLogId, FrmMain.UserName, RecordLogFlag.Regular, "Modifiy:");

                Database.ExecuteNonQuery("sp_ac_UpdateVoucherParty", System.Data.CommandType.StoredProcedure,
                    new Database.Parameter("@datefrom", this.Date),
                    new Database.Parameter("@dateto", this.Date),
                    new Database.Parameter("@companyname", this.CompanyName),
                    new Database.Parameter("@VoucherTypeGroup", "journal"),
                    new Database.Parameter("@VoucherNo", this.JournalNo)
                    );

                AccountCommonFunction.SaveUserLog("Update", string.Format("Journal Entry No {0} is Updated for {1} in yr {2}", this.JournalNo, this.CompanyName, this.Year));

                Database.CommitTransaction();
            }
            catch (Exception ex)
            {
                Database.RollBackTransaction();
                throw ex;
            }

            return true;
        }

        /// <summary>
        /// Delete Journal Voucher
        /// </summary>
        /// <returns>True on success</returns>
        public bool Delete()
        {
            if (!Database.OpenConnection(Utility.MaterialConnectionString))
                throw new ERPErrorException("Connection Error");

            Database.BeginTransaction();

            try
            {
                if (this.RecordLogId == 0)
                {
                    this.RecordLogId = AccountCommonFunction.SaveRecordLog(null, FrmMain.UserName, RecordLogFlag.Regular, string.Format("RecordLogId Add: Journal {0} in {1} for {2}", this.JournalNo, this.CompanyName, this.Year));
                    Database.GetExecuteNonQueryCommand(string.Format("update Journal set RecordLogId={0} where JournalNo={1} and yr='{2}' and CompanyName='{3}'", this.RecordLogId, this.JournalNo, this.Year, this.CompanyName));
                }

                this._Delete(true);

                AccountCommonFunction.SaveRecordLog(this.RecordLogId, FrmMain.UserName, RecordLogFlag.Deleted, "Deleted");

                AccountCommonFunction.SaveUserLog("Delete", string.Format("Journal Entry No {0} is Deleted for {1} in yr = {2}", this.JournalNo, this.CompanyName, this.Year));

                Database.CommitTransaction();
            }
            catch (Exception ex)
            {
                Database.RollBackTransaction();
                throw ex;
            }

            return true;
        }

        public void Clone()
        {
            this.CompanyName = "";
            this.Narration = "";
            this.username = "";
            this.JournalType = "";
            this.IsTDS = false;
            this.MRNNo = "";
            this.InwardNo = "";
            this.DrPartyName = "";
            this.CrPartyName = "";
            this.Year = "";
            this.BillDate = Utility.nullDate;
            this.Terms = "";
            this.LedgerEntries.Clear();
        }

        #region Private Function
        private int GetNewJournalNo()
        {
            return (int)Utility.SafeConvertToDouble(Database.GetExecuteScalarCommand_return(string.Format("SELECT ISNULL(MAX(JournalNo),0)+1 FROM Journal WHERE CompanyName='{0}' AND yr='{1}'", this.CompanyName, this.Year)));
        }

        private bool _InsertJournalEntry()
        {
            if (this.CompanyName.Length == 0 || this.Year.Length == 0)
                throw new ERPErrorException("Few required field are missing");

            this.SetDrCrPartyName();

            this.InsertedTransId.Clear();

            foreach (Journal.LedgerEntry ledgerEntry in this.LedgerEntries)
            {
                string query_insert_journal = "INSERT INTO Journal (JournalNo, sysdate, CompanyName, Type, PartyName, Debit, Credit, Narration, UserName, narrationFinal, JournalType, IsTDS, MRNo, InwardNo, DrPartyName, CrPartyName, yr, Currency, ExchangeRate, billDate, paymentTerms, TDSAccessableAmount, RecordLogId,SubLedgerName,companyId,PartyId,DrPartyID,CrPartyID) VALUES " +
                        string.Format("({0}, '{1:yyyy-MM-dd}', '{2}', '{3}', '{4}', {5}, {6}, '{7}', '{8}', '{9}', '{10}', '{11}', '{12}', '{13}', '{14}', '{15}', '{16}', '{17}', {18}, '{19:yyyy-MM-dd}', '{20}', '{21}', {22}, '{23}',{24},{25},{26},{27})",
                        this.JournalNo, this.Date, this.CompanyName,
                        ledgerEntry.Type, ledgerEntry.LedgerName, ledgerEntry.Debit, ledgerEntry.Credit, ledgerEntry.Remark,
                        this.username, this.Narration,
                        this.JournalType, this.IsTDS ? "yes" : "no", this.MRNNo, this.InwardNo,
                        this.DrPartyName, this.CrPartyName, this.Year,
                        ledgerEntry.Currency, ledgerEntry.ExcRate, this.BillDate, this.Terms, this.TDSAccessableAmount, this.RecordLogId,
                        ledgerEntry.SubLedgerName, this.companyId, ledgerEntry.LedgerID, this.DrPartyID, this.CrPartyID
                        );

                // insert into journal table
                Database.GetExecuteNonQueryCommand(query_insert_journal);

                // Get Transid of last journal row inserted
                int transid = (int)Utility.SafeConvertToDouble(Database.GetExecuteScalarCommand_return("SELECT @@IDENTITY"));

                InsertedTransId.Add(transid);

                foreach (Journal.BillWiseDetail billWiseDetail in ledgerEntry.billWiseDetails)
                {
                    // Make New Refrence entry if not exists
                    AccountCommonFunction.CreateNewBillRef(this.CompanyName, ledgerEntry.LedgerName, billWiseDetail.SelectionType, billWiseDetail.BillNo, billWiseDetail.BillDate, this.Terms, billWiseDetail.PaidAmount, ledgerEntry.Currency, this.companyId, ledgerEntry.LedgerID);

                    string query_insert_journal_item = "INSERT INTO JournalItem (JournalNo, sysdate, CompanyName, Type, PartyName, Debit, Credit, SelectionType, BillNo, BillDate, PaidAmount, BillAmount, UserName, yr, journaltransid,companyid,partyid) VALUES " +
                        string.Format("({0}, '{1:yyyy-MM-dd}', '{2}', '{3}', '{4}', {5}, {6}, '{7}', '{8}', '{9:yyyy-MM-dd}', {10}, {11}, '{12}', '{13}', {14},{15},{16})",
                        this.JournalNo, this.Date, this.CompanyName,
                        ledgerEntry.Type, ledgerEntry.LedgerName, ledgerEntry.Debit, ledgerEntry.Credit, billWiseDetail.SelectionType,
                        billWiseDetail.BillNo, billWiseDetail.BillDate, billWiseDetail.PaidAmount, billWiseDetail.BillAmount, this.username, this.Year,
                        transid, this.companyId, ledgerEntry.LedgerID // relation transid
                        );

                    // insert into journal item table with transid relation
                    
                    
                    Database.GetExecuteNonQueryCommand(query_insert_journal_item);
                }


                foreach (Journal.InventoryDetail inventoryDetail in ledgerEntry.inventoryDetails)
                {
                    string query_insert_journal_inventory = "INSERT INTO JournalInventory (JournalNo, sysdate, CompanyName, Type, PartyName, GroupName, Qty, Rate, Amount, yr, journaltransid,companyid,PartyID) VALUES " +
                            string.Format("({0}, '{1:yyyy-MM-dd}', '{2}', '{3}', '{4}', '{5}', {6}, {7}, {8}, '{9}', {10}, {11},{12})",
                            this.JournalNo, this.Date, this.CompanyName,
                            ledgerEntry.Type, ledgerEntry.LedgerName,
                            inventoryDetail.GroupName, inventoryDetail.Quantity, inventoryDetail.Rate, inventoryDetail.Amount,
                            this.Year,
                            transid, this.companyId, ledgerEntry.LedgerID // relation transid
                            );

                    // insert into journal inventory table
                    Database.GetExecuteNonQueryCommand(query_insert_journal_inventory);
                }
                foreach (Journal.CostCenterDetail costCenter in ledgerEntry.CostCenterDetails)
                {
                    string query_insert_journal_costcenter = "INSERT INTO CostCenterEntries (Srno,CompanyName,yr,LedgerName,Amount,VoucherType,CostCenterId,companyId,LedgerID) VALUES "
                    + string.Format("({0}, '{1}', '{2}', '{3}', '{4}', '{5}', '{6}',{7},{8})",
                            transid, this.CompanyName, this.Year, ledgerEntry.LedgerName,
                            costCenter.Amount * (ledgerEntry.Type.ToLower() == "dr" ? 1 : -1),
                            "Journal", costCenter.CostCenterId, this.companyId, ledgerEntry.LedgerID
                            );
                    //insert into CostCenterentries table
                    Database.GetExecuteNonQueryCommand(query_insert_journal_costcenter);
                }
            }

            return true;
        }

        private bool _Delete(bool MakeBackup = false)
        {
            // make a backup
            if (MakeBackup)
            {
                Database.GetExecuteNonQueryCommand(string.Format("INSERT INTO journal_delete SELECT * FROM Journal WHERE journalno={0} AND companyname='{1}' and yr='{2}'", this.JournalNo, this.OldCompanyName, this.OldYear));
                Database.GetExecuteNonQueryCommand(string.Format("INSERT INTO Journalitem_delete SELECT * FROM Journalitem WHERE journalno={0} AND companyname='{1}' and yr='{2}'", this.JournalNo, this.OldCompanyName, this.OldYear));
            }

            // delete

            // delete from cost center entries
            Database.GetExecuteNonQueryCommand(string.Format("DELETE cce FROM CostCenterEntries cce INNER JOIN Journal j on cce.srno=j.transid WHERE cce.VoucherType='Journal' AND j.journalno={0} AND j.companyname='{1}' AND j.yr='{2}'", this.JournalNo, this.OldCompanyName, this.OldYear));

            // delete from journal tables
            Database.GetExecuteNonQueryCommand(string.Format("DELETE FROM Journal WHERE journalno={0} AND companyname='{1}' AND yr='{2}'", this.JournalNo, this.OldCompanyName, this.OldYear));
            Database.GetExecuteNonQueryCommand(string.Format("DELETE FROM Journalitem WHERE journalno={0} AND companyname='{1}' AND yr='{2}'", this.JournalNo, this.OldCompanyName, this.OldYear));
            Database.GetExecuteNonQueryCommand(string.Format("DELETE FROM JournalInventory WHERE journalno={0} AND companyname='{1}' AND yr='{2}'", this.JournalNo, this.OldCompanyName, this.OldYear));

            return true;
        }

        private void SetDrCrPartyName()
        {
            this.DrPartyName = this.LedgerEntries.Find(x => x.Debit == this.LedgerEntries.Max(xx => xx.Debit)).LedgerName;
            this.CrPartyName = this.LedgerEntries.Find(x => x.Credit == this.LedgerEntries.Max(xx => xx.Credit)).LedgerName;

            this.DrPartyID = this.LedgerEntries.Find(x => x.Debit == this.LedgerEntries.Max(xx => xx.Debit)).LedgerID;
            this.CrPartyID = this.LedgerEntries.Find(x => x.Credit == this.LedgerEntries.Max(xx => xx.Credit)).LedgerID;

           

        }

        #endregion

        /// <summary>
        /// Ledger Entry Data Format
        /// </summary>
        public class LedgerEntry
        {
            public string Type;
            public string LedgerName;
            public int LedgerID;
            public double Debit = 0;
            public double Credit = 0;
            public string Currency = "Rs.";
            public double ExcRate = 1;
            public string Remark = "";
            public int TransId = -1;
            public string SubLedgerName = "";
            public List<BillWiseDetail> billWiseDetails = new List<BillWiseDetail>();
            public List<InventoryDetail> inventoryDetails = new List<InventoryDetail>();
            public List<CostCenterDetail> CostCenterDetails = new List<CostCenterDetail>();

            public LedgerEntry(string type, string ledgerName, double debit, double credit, string currency, double excRate, string remark, int transId, string subledgerName = "", int LedgerId = 0)
            {
                this.Type = type;
                this.LedgerName = ledgerName;
                this.LedgerID = LedgerId;
                this.Debit = debit;
                this.Credit = credit;
                this.Currency = currency;
                this.ExcRate = excRate;
                this.Remark = remark;
                this.TransId = transId;
                this.SubLedgerName = subledgerName;
            }
        }

        /// <summary>
        /// BillWiseDetail Data Format
        /// </summary>
        public class BillWiseDetail
        {
            public string SelectionType;
            public string BillNo;
            public DateTime BillDate = Utility.nullDate;
            public double PaidAmount;
            public double BillAmount;

            public BillWiseDetail(string selectionType, string billNo, DateTime billDate, double paidAmount, double billAmount)
            {
                this.SelectionType = selectionType;
                this.BillNo = billNo;
                this.BillDate = billDate;
                this.BillAmount = billAmount;
                this.PaidAmount = paidAmount;
            }
        }

        /// <summary>
        /// InventoryDetail Data Format
        /// </summary>
        public class InventoryDetail
        {
            public string GroupName;
            public double Quantity;
            public double Rate;
            public double Amount;
            public string unit;
            public string itemcode;
            public InventoryDetail(string GroupName, double Quantity, double Rate, double Amount, string Unit, string Itemcode)
            {
                this.GroupName = GroupName;
                this.Quantity = Quantity;
                this.Rate = Rate;
                this.Amount = Amount;
                this.unit = Unit;
                this.itemcode = Itemcode;
            }
        }
        public class CostCenterDetail
        {
            public int CostCenterId;
            public double Amount;

            public CostCenterDetail(int CostCenterId, double Amount)
            {
                this.CostCenterId = CostCenterId;
                this.Amount = Amount;
            }
        }
    }
}
