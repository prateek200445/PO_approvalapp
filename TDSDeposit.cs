using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace ERP.DataFormat
{
    /// <summary>
    /// TDS Deposit Class
    /// Select, Save, Delete operation
    /// </summary>
    class TDSDeposit
    {
        #region Variables
        //private string OldCompanyName = "";

        /// <summary>
        /// Deposit Id Unique
        /// </summary>
        public int TDSDepositId { get; private set; }

        /// <summary>
        /// Company for entry
        /// </summary>
        public int CompanyId = 0;

        /// <summary>
        /// Serial No
        /// Restart on new quarter start
        /// </summary>
        public int SerialNo = 1;

        private DateTime _month = Utility.nullDate;

        /// <summary>
        /// Month of TDS Entry
        /// </summary>
        public DateTime Month { get { return this._month; } set { this._month = new DateTime(value.Year, value.Month, 1); } }

        /// <summary>
        /// Section Code of TDS
        /// 94C, 94I, 94A
        /// </summary>
        public int SectionId = 0;

        public string ReturnType { get; private set; }

        /// <summary>
        /// Total amount of TDS
        /// </summary>
        public double TDSAmount { get { return this.tdsTransaction.Sum(x=>x.TDSAmount); } }

        /// <summary>
        /// Interest on TDS if lat payment
        /// </summary>
        public double InterestAmount = 0;

        /// <summary>
        /// Other amount if required
        /// </summary>
        public double OtherAmount = 0;

        /// <summary>
        /// Total TDS Deposited
        /// </summary>
        public double TotalTaxDeposited { get { return this.TDSAmount + this.InterestAmount; } }

        /// <summary>
        /// BSR Code
        /// </summary>
        public string BSRCode = "";

        /// <summary>
        /// Date on which Tax Deposited
        /// </summary>
        public DateTime DepositedDate = Utility.nullDate;

        /// <summary>
        /// Transfer voucher / challan serial No
        /// </summary>
        public string ChallanNo = "";

        /// <summary>
        /// Whether TDS deposited by book entry?
        /// </summary>
        public string TDSBookEntry = "";

        /// <summary>
        /// 24G receipt no
        /// </summary>
        public string ReceiptNo24G = "";

        /// <summary>
        /// DDO Seq Code
        /// </summary>
        public string DDOSeqCode = "";

        /// <summary>
        /// Minor Head
        /// Default value is 200
        /// </summary>
        public double MinorHead = 200;

        /// <summary>
        /// List of Deductee
        /// </summary>
        public List<TDSTransaction> tdsTransaction = new List<TDSTransaction>();

        #endregion

        /// <summary>
        /// Make Empty Model
        /// </summary>
        public TDSDeposit()
        {
        }

        /// <summary>
        /// Open Model from database
        /// </summary>
        /// <param name="DepositId">Id of Deposit Entry</param>
        public TDSDeposit(int DepositId)
        {
            if (!Database.OpenConnection(Utility.MaterialConnectionString))
                throw new ERPErrorException("Connection Error");

            this.TDSDepositId = DepositId;

            Database.myadapter = Database.GetAdapterCommand(string.Format(@"
SELECT
    tdsd.CompanyId, SerialNo, Month, tdsd.SectionId, tdss.ReturnType, InterestAmount, BSRCode, DepositedDate, ChallanNo,
    TDSBookEntry, ReceiptNo24G, DDOSeqCode, MinorHead
FROM ac_tds_deposit tdsd
INNER JOIN ac_tds_sections tdss on tdsd.SectionId=tdss.SectionId
WHERE TDSDepositId={0}

SELECT
    VoucherType, VoucherNo, VoucherDate, PartyName, PANNo, PaymentAmount, TaxDeducteeDate, PaidDate,
    TDSAmount, SurchargeAmount, ECAmount, SHECAmount, BillAmount, ReasonForNonDeduction, CertificateNo
FROM ac_tds_transaction
WHERE TDSDepositId={0}
ORDER BY VoucherDate
", DepositId));

            DataSet dataset = new DataSet();
            Database.myadapter.Fill(dataset);

            if (dataset.Tables[0].Rows.Count == 0)
                throw new ERPErrorException("Entry not found on database");

            // Get Data from Deposit
            {
                DataRow dr = dataset.Tables[0].Rows[0];

                this.CompanyId = AccountCommonFunction.SafeConvertToInt(dr["CompanyId"]);
                this.SerialNo = AccountCommonFunction.SafeConvertToInt(dr["SerialNo"]);
                this.Month = AccountCommonFunction.SafeConvertToDate(dr["Month"]);
                this.SectionId = AccountCommonFunction.SafeConvertToInt(dr["SectionId"]);
                this.ReturnType = Convert.ToString(dr["ReturnType"]);
                this.InterestAmount = Utility.SafeConvertToDouble(dr["InterestAmount"]);
                this.BSRCode = Convert.ToString(dr["BSRCode"]);
                this.DepositedDate = AccountCommonFunction.SafeConvertToDate(dr["DepositedDate"]);
                this.ChallanNo = Convert.ToString(dr["ChallanNo"]);
                this.TDSBookEntry = Convert.ToString(dr["TDSBookEntry"]);
                this.ReceiptNo24G = Convert.ToString(dr["ReceiptNo24G"]);
                this.DDOSeqCode = Convert.ToString(dr["DDOSeqCode"]);
                this.MinorHead = Utility.SafeConvertToDouble(dr["MinorHead"]);
            }

            // Get Data from transaction
            foreach (DataRow dr in dataset.Tables[1].Rows)
            {
                this.tdsTransaction.Add(new TDSTransaction(
                    Convert.ToString(dr["VoucherType"]),
                    Convert.ToString(dr["VoucherNo"]),
                    AccountCommonFunction.SafeConvertToDate(dr["VoucherDate"]),
                    Convert.ToString(dr["PartyName"]),
                    Convert.ToString(dr["PANNo"]),
                    Utility.SafeConvertToDouble(dr["PaymentAmount"]),
                    AccountCommonFunction.SafeConvertToDate(dr["TaxDeducteeDate"]),
                    AccountCommonFunction.SafeConvertToDate(dr["PaidDate"]),
                    Utility.SafeConvertToDouble(dr["TDSAmount"]),
                    Utility.SafeConvertToDouble(dr["SurchargeAmount"]),
                    Utility.SafeConvertToDouble(dr["ECAmount"]),
                    Utility.SafeConvertToDouble(dr["SHECAmount"]),
                    Utility.SafeConvertToDouble(dr["BillAmount"]),
                    Convert.ToString(dr["ReasonForNonDeduction"]),
                    Convert.ToString(dr["CertificateNo"])
                    ));
            }
        }

        /// <summary>
        /// Save or update entry
        /// </summary>
        /// <returns>True on success</returns>
        public bool Save()
        {
            try
            {
                Database.BeginTransaction();

                bool IsUpdate = false;
                if (this.TDSDepositId != 0)
                {
                    // update

                    IsUpdate = true;

                    Database.GetExecuteNonQueryCommand(string.Format(@"
update ac_tds_deposit set
SerialNo={0}, Month='{1:yyyy-MM-dd}', SectionId='{2}', InterestAmount={3}, OtherAmount={4},
BSRCode='{5}', DepositedDate='{6:yyyy-MM-dd}', ChallanNo='{7}', TDSBookEntry='{8}', ReceiptNo24G='{9}',
DDOSeqCode='{10}', MinorHead={11}, ModifiedOn=GETDATE(), Flag='U' WHERE TDSDepositId={12}",
this.SerialNo, this.Month, this.SectionId, this.InterestAmount, this.OtherAmount,
this.BSRCode, this.DepositedDate,this.ChallanNo,this.TDSBookEntry, this.ReceiptNo24G,
this.DDOSeqCode, this.MinorHead,
this.TDSDepositId));

                    // clear old transaction entry from database
                    Database.GetExecuteNonQueryCommand(string.Format("delete from ac_tds_transaction where TDSDepositId={0}", this.TDSDepositId));
                }
                else
                {
                    // insert
                    Database.GetExecuteNonQueryCommand(string.Format(@"
insert into ac_tds_deposit
(CompanyId, SerialNo, Month, SectionId, InterestAmount, OtherAmount, BSRCode, DepositedDate, ChallanNo, TDSBookEntry, ReceiptNo24G, DDOSeqCode, MinorHead, CreatedOn, ModifiedOn, Flag)
VALUES ('{0}', {1}, '{2:yyyy-MM-dd}', '{3}', {4}, {5}, '{6}', '{7:yyyy-MM-dd}', '{8}', '{9}', '{10}', '{11}', '{12}', GETDATE(), GETDATE(), 'N')
",
this.CompanyId, this.SerialNo, this.Month, this.SectionId, this.InterestAmount, this.OtherAmount,
this.BSRCode, this.DepositedDate, this.ChallanNo, this.TDSBookEntry, this.ReceiptNo24G,
this.DDOSeqCode, this.MinorHead));

                    this.TDSDepositId = AccountCommonFunction.SafeConvertToInt(Database.GetExecuteScalarCommand_return("SELECT @@IDENTITY"));

                    if (this.TDSDepositId == 0)
                        throw new ERPErrorException("Unknow error");
                }

                // insert into transaction
                foreach (TDSTransaction t in this.tdsTransaction)
                {
                    Database.GetExecuteNonQueryCommand(string.Format(@"
INSERT INTO ac_tds_transaction
(TDSDepositId, VoucherType, VoucherNo, VoucherDate, PartyName, PANNo,
PaymentAmount, TaxDeducteeDate, PaidDate, TDSAmount, SurchargeAmount, ECAmount, SHECAmount, ReasonForNonDeduction, CertificateNo, BillAmount)
VALUES ({0}, '{1}', '{2}', '{3:yyyy-MM-dd}', '{4}', '{5}', {6}, '{7:yyyy-MM-dd}', '{8:yyyy-MM-dd}', {9}, {10}, {11}, {12}, '{13}', '{14}', {15})",
this.TDSDepositId, t.VoucherType, t.VoucherNo, t.VoucherDate, t.PartyName, t.PANNo, t.PaymentAmount, t.TaxDeducteeDate, t.PaidDate, t.TDSAmount,
t.SurchargeAmount, t.ECAmount, t.SHECAmount, t.ReasonForNonDeduction, t.CertificateNo, t.BillAmount));
                }

                AccountCommonFunction.SaveUserLog((IsUpdate?"Update":"Save"), string.Format("TDS Deposit Entry{0} Saved", this.TDSDepositId));

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
        /// Delete entry
        /// </summary>
        /// <returns>True on success</returns>
        public bool Delete()
        {
            try
            {
                if (!Database.OpenConnection(Utility.MaterialConnectionString))
                    throw new ERPErrorException("Connection error");

                Database.BeginTransaction();

                Database.GetExecuteNonQueryCommand(string.Format("update ac_tds_deposit set flag='D' where TDSDepositId={0}", this.TDSDepositId));

                AccountCommonFunction.SaveUserLog("Delete", string.Format("TDS Deposit Entry{0} Deleted", this.TDSDepositId));

                Database.CommitTransaction();

                return true;
            }
            catch (Exception ex)
            {
                Database.RollBackTransaction();

                throw ex;
            }
        }

        #region Functions

        #endregion

        #region Classes
        public class TDSTransaction
        {
            #region Variables
            /// <summary>
            /// Voucher Type of tds deductee entry
            /// </summary>
            public string VoucherType;

            /// <summary>
            /// Voucher No
            /// </summary>
            public string VoucherNo;

            /// <summary>
            /// Voucher Date
            /// </summary>
            public DateTime VoucherDate = Utility.nullDate;

            /// <summary>
            /// Ledger Name (Party)
            /// </summary>
            public string PartyName;

            private string _PANNo;
            /// <summary>
            /// PAN No of ledger
            /// </summary>
            public string PANNo {
                get { return this._PANNo; }
                set {
                    if (!AccountCommonFunction.ValidatePANNo(value))
                        throw new ERPErrorException("Invalid PAN No passed");

                    this._PANNo = value;
                }
            }

            /// <summary>
            /// Total Payment Amount to party
            /// </summary>
            public double PaymentAmount;

            /// <summary>
            /// BillAmount
            /// <para>Purchase amount or Sales amount</para>
            /// </summary>
            public double BillAmount;

            /// <summary>
            /// Date of Deductee
            /// </summary>
            public DateTime TaxDeducteeDate = Utility.nullDate;

            /// <summary>
            /// Date when paid
            /// </summary>
            public DateTime PaidDate = Utility.nullDate;

            /// <summary>
            /// TDS Amount
            /// </summary>
            public double TDSAmount;

            /// <summary>
            /// Surchange Amount
            /// </summary>
            public double SurchargeAmount;

            /// <summary>
            /// EC Amount
            /// </summary>
            public double ECAmount;

            /// <summary>
            /// SHEC Amount
            /// </summary>
            public double SHECAmount;

            /// <summary>
            /// Total Amount
            /// </summary>
            public double TotalAmount { get { return this.TDSAmount + this.SurchargeAmount + this.ECAmount + this.SHECAmount; } }

            /// <summary>
            /// Total tax deducted
            /// </summary>
            public double TotalTaxDeducted { get { return this.TDSAmount + this.SurchargeAmount + this.ECAmount + this.SHECAmount; } }

            /// <summary>
            /// Reson for non deduction
            /// </summary>
            public string ReasonForNonDeduction;

            /// <summary>
            /// Certificate No
            /// </summary>
            public string CertificateNo;

            #endregion

            /// <summary>
            /// Model for TDSTransaction
            /// </summary>
            /// <param name="VoucherType"></param>
            /// <param name="VoucherNo"></param>
            /// <param name="VoucherDate"></param>
            /// <param name="LedgerId"></param>
            /// <param name="LedgerName"></param>
            /// <param name="PaymentAmount"></param>
            /// <param name="TaxDeducteeDate"></param>
            /// <param name="PaidDate"></param>
            /// <param name="TDSAmount"></param>
            /// <param name="SurchargeAmount"></param>
            /// <param name="ECAmount"></param>
            /// <param name="SHECAmount"></param>
            /// <param name="ResonForNonDeduction"></param>
            /// <param name="CertificateNo"></param>
            public TDSTransaction(string VoucherType, string VoucherNo, DateTime VoucherDate,
                string PartyName, string PANNo, double PaymentAmount, DateTime TaxDeducteeDate, DateTime PaidDate,
                double TDSAmount, double SurchargeAmount, double ECAmount, double SHECAmount, double BillAmount,
                string ResonForNonDeduction, string CertificateNo)
            {
                this.VoucherType = VoucherType;
                this.VoucherNo = VoucherNo;
                this.VoucherDate = VoucherDate;

                this.PartyName = PartyName;
                this.PANNo = PANNo;
                this.PaymentAmount = PaymentAmount;
                this.TaxDeducteeDate = TaxDeducteeDate;
                this.PaidDate = PaidDate;
                this.BillAmount = BillAmount;
                this.TDSAmount = TDSAmount;
                this.SurchargeAmount = SurchargeAmount;
                this.ECAmount = ECAmount;
                this.SHECAmount = SHECAmount;
                this.ReasonForNonDeduction = ResonForNonDeduction;
                this.CertificateNo = CertificateNo;
            }
        }
        #endregion
    }
}
