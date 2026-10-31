using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ERP.DataFormat
{
    class Ledger
    {
        #region Variables

        private string _OldLedgerName = "";
        private string _OldCompanyName = "";
        public string OldLedgerName { get { return this._OldLedgerName; } private set { this._OldLedgerName = value; } }
        public string OldCompanyName { get { return this._OldCompanyName; } private set { this._OldCompanyName = value; } }
        private int srno = -1;
        private bool LedgerExists = false;
        //public bool IsAmount = false;
        public bool IsMaintainBillWise = false;
        public string CompanyName = "";
        public int companyid = 0;
        public string LedgerName = "";
        public string GroupName = "";
        public double OpeningBalance = 0;
        public double PendingBalance = 0;
        public int LedgerId { get { return this.srno; } }
        public string Category = "";
        public bool IsTDS = false;
        public string NaturePayment = "";
        public string LedgerTitle = "";
        public string HouseNo = "";
        public string LandMark1 = "";
        public string LandMark2 = "";
        public string City = "";
        public string State = "";
        public string Country = "";
        public string Address = "";
        public string PinCode = "";
        public bool IsMRN = false;
        public bool IsInvoice = false;
        public string TaxType = "";
        public string BRSCode = "";
        public string TDSDeductee = "";
        public string Type = "";
        public double PercentageOfCalculation;
        public string MethodOfCalculation = "";
        public string VatClass = "";
        public bool IsAccessbleValue;
        public string ExciseNo = "";
        public string PANNo = "";
        public string ECCNo = "";
        public string GSTNo = "";
        public string VATNo = "";
        public string IECCode = "";
        public string CSTNo = "";
        public string AliasCompanyName = "";
        public string TypeOfInvoice = "";
        public bool IsVATInclude = false;
        public string BankAC = "";
        public string MICRCode = "";
        public string AccountType = "";
        public string IEFSCCode = "";
        public string BranchCode = "";
        public string BranchName = "";
        public string PaymentTerms = "";
        public string ContactNo = "";
        public bool IsBillWise = false;
        public string ServiceTaxNo = "";
        public string BlockName = "";
        public bool IsAdditionDep = false;
        public string SectionCode = "";
        public string GroupSchedule6 = "";
        public bool IsInventory = false;
        public bool IsCostCenter = false;
        public string RepresentiveName = "";
        public string MainCategory = "";
        public string NewGST = "";
        public bool IsInterCompany = false;
        public int RecordLogId;
        public bool IsAccClose = false;
        public bool IsCashFlowAllow = false;
        public string vendorcode = "";
        public string customercode = "";
        public string Fromdate = "";
        public string ToDate = "";
        public string MSMENumber = "";
        public string TDSRate = string.Empty;
        public string BankName = "";
        public string IBANCode = "";
        public string swiftcode = "";
        public string emailid = "";
        public string website = "";
        public string G2 = "";
        public string G3 = "";

        public string ContactName = "";
        public string TelNo2 = "";
        public string FaxNo = "";
        public string EMAILCC = "";
        public bool IsGovnComp = false; //Added new field 24.09.2020
        public bool IsTCSApp = false;//Added new field 2021.07.02
        #endregion

        public Ledger()
        {
        }

        public Ledger(int LedgerId)
        {
            if (!Database.OpenConnection(Utility.MaterialConnectionString))
                throw new ERPErrorException("Error in opening connection");

            Database.GetExecuteReaderCommand(string.Format("SELECT TOP 1 * FROM LedgerMaster WHERE srno='{0}'", LedgerId));

            if (!Database.myreader.HasRows)
                throw new ERPErrorException("Ledger not found in database");

            if (Database.myreader.Read())
            {
                this.LedgerExists = true;

                this.LedgerName = Convert.ToString(Database.myreader["LedgerName"]);
                this.OldLedgerName = Convert.ToString(Database.myreader["LedgerName"]);
                this.CompanyName = Convert.ToString(Database.myreader["CompanyName"]);
                this.OldCompanyName = Convert.ToString(Database.myreader["CompanyName"]);
                this.companyid = Convert.ToInt32(Database.myreader["companyId"]);
                this.GroupName = Convert.ToString(Database.myreader["Under"]);
                this.OpeningBalance = Utility.SafeConvertToDouble(Database.myreader["Openingbalance"]);
                this.PendingBalance = Utility.SafeConvertToDouble(Database.myreader["PendingBalance"]);
                this.srno = (int)Utility.SafeConvertToDouble(Database.myreader["srno"]);
                this.Category = Convert.ToString(Database.myreader["Category"]);
                this.IsTDS = Convert.ToString(Database.myreader["IsTDS"]).ToLower() == "yes";
                this.NaturePayment = Convert.ToString(Database.myreader["IsNaturePayment"]);
                this.LedgerTitle = Convert.ToString(Database.myreader["Title"]);
                this.HouseNo = Convert.ToString(Database.myreader["Houseno"]);
                this.LandMark1 = Convert.ToString(Database.myreader["LandMark1"]);
                this.LandMark2 = Convert.ToString(Database.myreader["LandMark2"]);
                this.City = Convert.ToString(Database.myreader["City"]);
                this.State = Convert.ToString(Database.myreader["State"]);
                this.Country = Convert.ToString(Database.myreader["Country"]);
                this.Address = Convert.ToString(Database.myreader["Address"]);
                this.PinCode = Convert.ToString(Database.myreader["PINCode"]);
                this.IsMRN = Convert.ToString(Database.myreader["IsMRN"]).ToLower() == "yes";
                this.IsInvoice = Convert.ToString(Database.myreader["ISInvoice"]).ToLower() == "yes";
                this.TaxType = Convert.ToString(Database.myreader["TaxType"]);
                this.BRSCode = Convert.ToString(Database.myreader["BSRCode"]);
                this.TDSDeductee = Convert.ToString(Database.myreader["TDSDeductee"]);
                this.Type = Convert.ToString(Database.myreader["Type"]);
                this.PercentageOfCalculation = Utility.SafeConvertToDouble(Database.myreader["PercentageofCalculation"]);
                this.MethodOfCalculation = Convert.ToString(Database.myreader["MethodofCalculation"]);
                this.VatClass = Convert.ToString(Database.myreader["vatclass"]);
                this.IsAccessbleValue = Convert.ToString(Database.myreader["AccessbleValue"]).ToLower() == "yes";
                this.ExciseNo = Convert.ToString(Database.myreader["ExciseNo"]);
                this.PANNo = Convert.ToString(Database.myreader["PANNo"]);
                this.ECCNo = Convert.ToString(Database.myreader["ECCNo"]);
                this.GSTNo = Convert.ToString(Database.myreader["GSTNo"]);
                this.VATNo = Convert.ToString(Database.myreader["TINNo"]);
                this.IECCode = Convert.ToString(Database.myreader["IECCode"]);
                this.CSTNo = Convert.ToString(Database.myreader["CSTNo"]);
                this.AliasCompanyName = Convert.ToString(Database.myreader["AliasCompanyName"]);
                this.TypeOfInvoice = Convert.ToString(Database.myreader["TypeOfInvoice"]);
                this.IsVATInclude = Convert.ToString(Database.myreader["VATIncluded"]).ToLower() == "yes";
                this.BankAC = Convert.ToString(Database.myreader["BankAccNo"]);
                this.MICRCode = Convert.ToString(Database.myreader["MICRCode"]);
                this.AccountType = Convert.ToString(Database.myreader["Acctype"]);
                this.IEFSCCode = Convert.ToString(Database.myreader["IFSCCode"]);
                this.BranchCode = Convert.ToString(Database.myreader["BranchCode"]);
                this.BranchName = Convert.ToString(Database.myreader["BranchName"]);
                this.PaymentTerms = Convert.ToString(Database.myreader["PaymentTerms"]);
                this.ContactNo = Convert.ToString(Database.myreader["ContactNo"]);
                this.IsBillWise = Convert.ToString(Database.myreader["IsBillWise"]).ToLower() == "yes";
                this.IsInterCompany = Convert.ToString(Database.myreader["IsInterCompany"]).ToLower() == "yes";
                this.ServiceTaxNo = Convert.ToString(Database.myreader["servicetax"]);
                this.BlockName = Convert.ToString(Database.myreader["BlockName"]);
                this.IsAdditionDep = Convert.ToString(Database.myreader["isAddDepreciation"]).ToLower() == "yes";
                this.SectionCode = Convert.ToString(Database.myreader["SectionCode"]);
                this.GroupSchedule6 = Convert.ToString(Database.myreader["underschedule6"]);
                this.RepresentiveName = Convert.ToString(Database.myreader["RepresentiveName"]);
                this.RecordLogId = AccountCommonFunction.SafeConvertToInt(Database.myreader["RecordLogId"]);
                this.MainCategory = Convert.ToString(Database.myreader["MainCategory"]);
                this.NewGST = Convert.ToString(Database.myreader["NewGSTNo"]);
                this.MSMENumber = Convert.ToString(Database.myreader["MSMENumber"]);
                this.vendorcode = Convert.ToString(Database.myreader["LVendorCode"]);
                this.customercode = Convert.ToString(Database.myreader["LCompanyCode"]);
                this.swiftcode = Convert.ToString(Database.myreader["swiftcode"]);
                this.BankName = Convert.ToString(Database.myreader["BankName"]);
                this.IBANCode = Convert.ToString(Database.myreader["IBANCode"]);
                this.emailid = Convert.ToString(Database.myreader["email"]);
                this.website = Convert.ToString(Database.myreader["website"]);
                #region New fields from vendor 19.06.2020
                this.ContactName = Convert.ToString(Database.myreader["ContactName"]);
                this.TelNo2 = Convert.ToString(Database.myreader["TelNo2"]);
                this.FaxNo = Convert.ToString(Database.myreader["FaxNo"]);
                this.EMAILCC = Convert.ToString(Database.myreader["EMAILCC"]);
                #endregion
                #region New fields for TCS Govenment Company 24.09.2020
                this.IsGovnComp = Convert.ToInt32(Database.myreader["isGovComp"]) == 1;
                #endregion
                if (Convert.ToString(Database.myreader["IsAccClose"].ToString().Trim().ToLower()) == "yes")
                {
                    this.IsAccClose = true;
                }

                if (Convert.ToString(Database.myreader["IsCashFlowAllow"].ToString().Trim().ToLower()) == "yes")
                {
                    this.IsCashFlowAllow = true;
                }
                if (Convert.ToString(Database.myreader["IsInventory"].ToString().Trim().ToLower()) == "yes")
                {
                    this.IsInventory = true;
                }
                if (Database.myreader["Fromdate"].ToString().Length > 0)
                    this.Fromdate = Convert.ToDateTime(Database.myreader["Fromdate"]).ToString("yyyy-MM-dd");
                else
                    this.Fromdate = "";

                if (Database.myreader["ToDate"].ToString().Length > 0)
                    this.ToDate = Convert.ToDateTime(Database.myreader["ToDate"]).ToString("yyyy-MM-dd");
                else
                    this.ToDate = "";
                this.TDSRate = Convert.ToString(Database.myreader["TDSRate"]);
                if (Convert.ToInt32(Database.myreader["IsTCSApp"]) == 1) //2021.07.02
                {
                    this.IsTCSApp = true;
                }
            }
            Database.myreader.Close();

            Database.myreader = Database.GetExecuteReaderCommand("select ExpenseHead from commoncashvoucherexpensegrouphead where ExpenseGroupHead ='" + this.GroupName + "'");
            if (Database.myreader.Read())
                this.G3 = Database.myreader[0].ToString();
            Database.myreader.Close();

            Database.myreader = Database.GetExecuteReaderCommand("select ExpenseHead from commoncashvoucherexpensegrouphead where ExpenseGroupHead ='" + this.G3 + "'");
            if (Database.myreader.Read())
                this.G2 = Database.myreader[0].ToString();
            Database.myreader.Close();
        }

        public Ledger(string LedgerName, string CompanyName)
        {
            if (!Database.OpenConnection(Utility.MaterialConnectionString))
                throw new ERPErrorException("Error in opening connection");

            Database.GetExecuteReaderCommand(string.Format("SELECT TOP 1 * FROM LedgerMaster WHERE CompanyName='{0}' AND LedgerName='{1}'", CompanyName, LedgerName));

            if (!Database.myreader.HasRows)
                throw new ERPErrorException("Ledger not found in database");

            if (Database.myreader.Read())
            {
                this.LedgerExists = true;

                this.LedgerName = Convert.ToString(Database.myreader["LedgerName"]);
                this.OldLedgerName = Convert.ToString(Database.myreader["LedgerName"]);
                this.CompanyName = Convert.ToString(Database.myreader["CompanyName"]);
                this.OldCompanyName = Convert.ToString(Database.myreader["CompanyName"]);
                this.companyid = Convert.ToInt32(Database.myreader["companyid"]);
                this.GroupName = Convert.ToString(Database.myreader["Under"]);
                this.OpeningBalance = Utility.SafeConvertToDouble(Database.myreader["Openingbalance"]);
                this.PendingBalance = Utility.SafeConvertToDouble(Database.myreader["PendingBalance"]);
                this.srno = (int)Utility.SafeConvertToDouble(Database.myreader["srno"]);
                this.Category = Convert.ToString(Database.myreader["Category"]);
                this.IsTDS = Convert.ToString(Database.myreader["IsTDS"]).ToLower() == "yes";
                this.NaturePayment = Convert.ToString(Database.myreader["IsNaturePayment"]);
                this.LedgerTitle = Convert.ToString(Database.myreader["Title"]);
                this.HouseNo = Convert.ToString(Database.myreader["Houseno"]);
                this.LandMark1 = Convert.ToString(Database.myreader["LandMark1"]);
                this.LandMark2 = Convert.ToString(Database.myreader["LandMark2"]);
                this.City = Convert.ToString(Database.myreader["City"]);
                this.State = Convert.ToString(Database.myreader["State"]);
                this.Country = Convert.ToString(Database.myreader["Country"]);
                this.Address = Convert.ToString(Database.myreader["Address"]);
                this.PinCode = Convert.ToString(Database.myreader["PINCode"]);
                this.IsMRN = Convert.ToString(Database.myreader["IsMRN"]).ToLower() == "yes";
                this.IsInvoice = Convert.ToString(Database.myreader["ISInvoice"]).ToLower() == "yes";
                this.TaxType = Convert.ToString(Database.myreader["TaxType"]);
                this.BRSCode = Convert.ToString(Database.myreader["BSRCode"]);
                this.TDSDeductee = Convert.ToString(Database.myreader["TDSDeductee"]);
                this.Type = Convert.ToString(Database.myreader["Type"]);
                this.PercentageOfCalculation = Utility.SafeConvertToDouble(Database.myreader["PercentageofCalculation"]);
                this.MethodOfCalculation = Convert.ToString(Database.myreader["MethodofCalculation"]);
                this.VatClass = Convert.ToString(Database.myreader["vatclass"]);
                this.IsAccessbleValue = Convert.ToString(Database.myreader["AccessbleValue"]).ToLower() == "yes";
                this.ExciseNo = Convert.ToString(Database.myreader["ExciseNo"]);
                this.PANNo = Convert.ToString(Database.myreader["PANNo"]);
                this.ECCNo = Convert.ToString(Database.myreader["ECCNo"]);
                this.GSTNo = Convert.ToString(Database.myreader["GSTNo"]);
                this.VATNo = Convert.ToString(Database.myreader["TINNo"]);
                this.IECCode = Convert.ToString(Database.myreader["IECCode"]);
                this.CSTNo = Convert.ToString(Database.myreader["CSTNo"]);
                this.AliasCompanyName = Convert.ToString(Database.myreader["AliasCompanyName"]);
                this.TypeOfInvoice = Convert.ToString(Database.myreader["TypeOfInvoice"]);
                this.IsVATInclude = Convert.ToString(Database.myreader["VATIncluded"]).ToLower() == "yes";
                this.BankAC = Convert.ToString(Database.myreader["BankAccNo"]);
                this.MICRCode = Convert.ToString(Database.myreader["MICRCode"]);
                this.AccountType = Convert.ToString(Database.myreader["Acctype"]);
                this.IEFSCCode = Convert.ToString(Database.myreader["IFSCCode"]);
                this.BranchCode = Convert.ToString(Database.myreader["BranchCode"]);
                this.BranchName = Convert.ToString(Database.myreader["BranchName"]);
                this.PaymentTerms = Convert.ToString(Database.myreader["PaymentTerms"]);
                this.ContactNo = Convert.ToString(Database.myreader["ContactNo"]);
                this.IsBillWise = Convert.ToString(Database.myreader["IsBillWise"]).ToLower() == "yes";
                this.IsInterCompany = Convert.ToString(Database.myreader["IsInterCompany"]).ToLower() == "yes";
                this.ServiceTaxNo = Convert.ToString(Database.myreader["servicetax"]);
                this.BlockName = Convert.ToString(Database.myreader["BlockName"]);
                this.IsAdditionDep = Convert.ToString(Database.myreader["isAddDepreciation"]).ToLower() == "yes";
                this.SectionCode = Convert.ToString(Database.myreader["SectionCode"]);
                this.GroupSchedule6 = Convert.ToString(Database.myreader["underschedule6"]);
                this.RepresentiveName = Convert.ToString(Database.myreader["RepresentiveName"]);
                this.MainCategory = Convert.ToString(Database.myreader["MainCategory"]);
                this.NewGST = Convert.ToString(Database.myreader["NewGSTNo"]);
                this.MSMENumber = Convert.ToString(Database.myreader["MSMENumber"]);
                this.IsCashFlowAllow = Convert.ToString(Database.myreader["IsCashFlowAllow"]).ToLower() == "yes";
                this.IsAccClose = Convert.ToString(Database.myreader["IsAccClose"]).ToLower() == "yes";
                this.RecordLogId = AccountCommonFunction.SafeConvertToInt(Database.myreader["RecordLogId"]);
                this.Fromdate = Convert.ToDateTime(Database.myreader["Fromdate"]).ToString("yyyy-MM-dd");
                if (Database.myreader["ToDate"].ToString().Length > 0)
                    this.ToDate = Convert.ToDateTime(Database.myreader["ToDate"]).ToString("yyyy-MM-dd");
                else
                    this.ToDate = "";
                this.TDSRate = Convert.ToString(Database.myreader["TDSRate"]);
                this.swiftcode = Convert.ToString(Database.myreader["swiftcode"]);
                this.BankName = Convert.ToString(Database.myreader["BankName"]);
                this.IBANCode = Convert.ToString(Database.myreader["IBANCode"]);
                this.emailid = Convert.ToString(Database.myreader["email"]);
                this.website = Convert.ToString(Database.myreader["website"]);
                #region New fields from vendor 19.06.2020
                this.ContactName = Convert.ToString(Database.myreader["ContactName"]);
                this.TelNo2 = Convert.ToString(Database.myreader["TelNo2"]);
                this.FaxNo = Convert.ToString(Database.myreader["FaxNo"]);
                this.EMAILCC = Convert.ToString(Database.myreader["EMAILCC"]);
                #endregion
                #region New fields for TCS Govenment Company 24.09.2020
                this.IsGovnComp = Convert.ToInt32(Database.myreader["isGovComp"]) == 1;
                #endregion
                this.IsTCSApp = Convert.ToInt32(Database.myreader["IsTCSApp"]) == 1;//2021.07.02                
            }
            Database.myreader.Close();
        }

        private bool ValidateData()
        {
            if (this.LedgerName.Length == 0)
                throw new ERPErrorException("Name of ledger must be non empty");

            if (this.CompanyName.Length == 0)
                throw new ERPErrorException("Name of company must be non empty");



            return true;
        }

        private string BoolToString(bool b)
        {
            return b ? "yes" : "no";
        }

        private int GenerateSrNo()
        {
            if (!Database.OpenConnection(Utility.MaterialConnectionString))
                throw new Exception("Error on opening connection");

            return (int)Database.GetExecuteScalarCommand_return("SELECT MAX(srno)+1 FROM LedgerMaster");
        }

        public bool IsExists()
        {
            return this.LedgerExists;
        }

        public void MakeAddressString()
        {
            this.Address = this.HouseNo + (this.LandMark1.Length > 0 ? "," + this.LandMark1 : "")
                + (this.LandMark2.Length > 0 ? Environment.NewLine + "," + this.LandMark2 : "")
                + (this.City.Length > 0 ? Environment.NewLine + "," + this.City : "")
                + (this.State.Length > 0 ? "," + this.State : "")
                + (this.Country.Length > 0 ? "," + this.Country : "");
        }

        /// <summary>
        /// Save Ledger
        /// </summary>
        /// <returns>True on success</returns>
        public bool Save()
        {
            //// Already in database, Need to update
            //if (this.OldLedgerName.Length != 0)
            //{
            //    Update();
            //    return true;
            //}

            if (this.OldCompanyName.Length != 0 || this.OldLedgerName.Length != 0)
                throw new ERPErrorException("Ledger already in database");

            if (!ValidateData())
                return false;

            Database.OpenConnection(Utility.MaterialConnectionString);

            if (0 != AccountCommonFunction.SafeConvertToInt(Database.GetExecuteScalarCommand_return(string.Format("SELECT COUNT(*) FROM LedgerMaster WHERE CompanyName='{0}' AND LedgerName='{1}'", this.CompanyName, this.LedgerName))))
            {
                throw new ERPErrorException("This ledgername already in selected company. You can't create same ledger in same company");
            }

            MakeAddressString();

            //this.srno = GenerateSrNo();

            try
            {
                Database.BeginTransaction();

                string HistoryRemarks = string.Format("Ledger Created (Ledger:{0}, Company:{1})", this.LedgerName, this.CompanyName);

                // Get New RecordLogId
                this.RecordLogId = AccountCommonFunction.SaveRecordLog(null, FrmMain.UserName, RecordLogFlag.Regular, HistoryRemarks);

                string query = string.Format("INSERT INTO LedgerMaster (CompanyName,LedgerName,Under,Openingbalance,PendingBalance,Category,IsTDS,IsNaturePayment,Title,"  // 0-8
                + "Houseno,Landmark1,Landmark2,City,State,Country,Address,PINCode,ISMRN,ISInvoice,TaxType,BSRCode,TDSDeductee,Type,PercentageofCalculation,MethodofCalculation,vatclass,"   // 9-25
                + "AccessbleValue,ExciseNo,PANNo,ECCNo,GSTNo,TINNo,IECCode,CSTNo,AliasCompanyName,TypeOfInvoice,VATIncluded,BankAccNo,MICRCode,Acctype,IFSCCode,BranchCode,"    // 26-41
                + "BranchName,PaymentTerms,ContactNo,IsBillWise,servicetax,BlockName,isAddDepreciation,SectionCode,underschedule6, IsInventory, IsCostCenter, RepresentiveName, RecordLogId,MainCategory,NewGSTNo,IsInterCompany,IsAccClose,IsCashFlowAllow,Fromdate,companyId,MSMENumber,TDSRate,LCompanyCode,LVendorCode,BankName,IBANCode,swiftcode,email,website,ContactName,TelNo2,FaxNo,EMAILCC,IsTCSApp) "    // 42-54
                + "VALUES ("
                + "'{0}', '{1}', '{2}', '{3}', '{4}', '{5}', '{6}', '{7}', '{8}'"
                + ", '{9}', '{10}', '{11}', '{12}', '{13}', '{14}', '{15}', '{16}', '{17}', '{18}', '{19}', '{20}', '{21}', '{22}', '{23}', '{24}', '{25}'"
                + ", '{26}', '{27}', '{28}', '{29}', '{30}', '{31}', '{32}', '{33}', '{34}', '{35}', '{36}', '{37}', '{38}', '{39}', '{40}', '{41}'"
                + ", '{42}', '{43}', '{44}', '{45}', '{46}', '{47}', '{48}', '{49}', '{50}', '{51}', '{52}', '{53}', {54},'{55}','{56}','{57}','{58}','{59}',cast(getdate() as date),{60},'{61}',{62},'{63}','{64}','{65}','{66}','{67}','{68}','{69}','{71}','{72}','{73}','{74}',{75}"
                + ")",
                this.CompanyName, this.LedgerName, this.GroupName, this.OpeningBalance, this.PendingBalance, this.Category, BoolToString(this.IsTDS), this.NaturePayment, this.LedgerTitle,
                this.HouseNo, this.LandMark1, this.LandMark2, this.City, this.State, this.Country, this.Address, this.PinCode, BoolToString(this.IsMRN), BoolToString(this.IsInvoice), this.TaxType, this.BRSCode, this.TDSDeductee, this.Type, this.PercentageOfCalculation, this.MethodOfCalculation, this.VatClass,
                BoolToString(this.IsAccessbleValue), this.ExciseNo, this.PANNo, this.ECCNo, this.GSTNo, this.VATNo, this.IECCode, this.CSTNo, this.AliasCompanyName, this.TypeOfInvoice, BoolToString(this.IsVATInclude), this.BankAC, this.MICRCode, this.AccountType, this.IEFSCCode, this.BranchCode,
                this.BranchName, this.PaymentTerms, this.ContactNo, BoolToString(this.IsBillWise), this.ServiceTaxNo, this.BlockName, BoolToString(this.IsAdditionDep), this.SectionCode, this.GroupSchedule6, BoolToString(this.IsInventory), BoolToString(this.IsCostCenter), this.RepresentiveName, this.RecordLogId, this.MainCategory,
                this.NewGST, BoolToString(this.IsInterCompany), BoolToString(this.IsAccClose), BoolToString(this.IsCashFlowAllow), this.companyid, this.MSMENumber, Utility.SafeConvertToDouble(this.TDSRate), this.vendorcode, this.customercode,
                this.BankName, this.IBANCode, this.swiftcode, this.emailid, this.website, this.Fromdate, this.ContactName, this.TelNo2, this.FaxNo, this.EMAILCC, (this.IsTCSApp == true ? 1 : 0)
                );

                Database.GetExecuteNonQueryCommand(query);
                int transid = (int)Utility.SafeConvertToDouble(Database.GetExecuteScalarCommand_return("SELECT @@IDENTITY"));
                Database.CommitTransaction();
                this.srno = transid;


            }
            catch (Exception ex)
            {
                Database.RollBackTransaction();
                throw ex;
            }

            return true;
        }

        #region Approval set for Ledger Master
        public bool AllocateLedgerMasterApproval(int LedgerId, string companyname, string LedgerName, string Under, string NewGSTIN)
        {
            bool blnFnd = false;
            try
            {
                string approvalname = "";
                using (System.Data.SqlClient.SqlConnection conn = new System.Data.SqlClient.SqlConnection(Utility.MaterialConnectionString))
                {
                    conn.Open();

                    string strsql = "select * from LedgerMasterAllocation where companyname='" + companyname + "'";
                    System.Data.SqlClient.SqlCommand command = new System.Data.SqlClient.SqlCommand(strsql, conn);
                    System.Data.SqlClient.SqlDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        approvalname = Convert.ToString(reader[0]);
                        string sql = "Insert into LedgerMasterEntryApproval(LedgerId,CompanyName,LedgerName,Under,NewGSTIN,ApprovalName,UserName,Status) values(" + LedgerId + ",'" +
                            companyname + "','" + LedgerName + "','" + Under + "','" + NewGSTIN + "','" + approvalname + "','" + FrmMain.UserName + "','Pending')";
                        using (System.Data.SqlClient.SqlConnection conn1 = new System.Data.SqlClient.SqlConnection(Utility.MaterialConnectionString))
                        {
                            System.Data.SqlClient.SqlCommand cmd = new System.Data.SqlClient.SqlCommand(strsql, conn1);
                            cmd.CommandType = System.Data.CommandType.Text;
                            cmd.CommandText = sql;
                            cmd.Connection = conn1;
                            conn1.Open();
                            cmd.ExecuteNonQuery();
                            conn1.Close();

                            string EmailTo = "";
                            Database.myreader = Database.GetExecuteReaderCommand(" select email from LOGINENTRY..loginrights where name ='"
                            + approvalname + "' or  fullname='" + approvalname + "'");
                            if (Database.myreader.Read())
                            {
                                EmailTo = Database.myreader[0].ToString();
                            }
                            Database.myreader.Close();

                            string bodty = "<html><body align=left  >Ledger Master Creation Approval Sent to :" + approvalname + "</br>Company Name=" + companyname + "</br>";
                            bodty += "Ledger Name :" + LedgerName + " </br></body></html>";


                            if (FrmMain.email.Length > 0)
                                FrmSendMail.SendAnEmail_HTML_NoMessage(FrmMain.email, EmailTo, "", "Ledger Master Creation Approval Sent to :"
                                    + approvalname + " For Company Name :" + companyname + " and Ledger Name=" + LedgerName
                                    , bodty,
                                    "erp@champalalgroup.com", false, "");
                            else
                                FrmSendMail.SendAnEmail_HTML_NoMessage(EmailTo, "", "", "Ledger Master Creation Approval Sent to :"
                                    + approvalname + " For Company Name :" + companyname + " and Ledger Name=" + LedgerName
                                    , bodty,
                                    "erp@champalalgroup.com", false, "");
                            blnFnd = true;

                        }
                    }
                    reader.Close();
                }
                return blnFnd;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        #endregion
        /// <summary>
        /// Update Ledger
        /// </summary>
        /// <returns>True on success</returns>
        public bool Update(string UpdateRemarks = "")
        {
            //// Entry Not available, so need to save
            //if (this.OldLedgerName.Length == 0 && this.OldCompanyName.Length == 0)
            //{
            //    Save();
            //    return;
            //}

            if (this.OldCompanyName.Length == 0 || this.OldLedgerName.Length == 0)
                throw new ERPErrorException("Ledger not in database");

            if (this.OldCompanyName != this.CompanyName)
                throw new ERPErrorException("Company name can't change");

            if (!ValidateData())
                return false;

            if (!Database.OpenConnection(Utility.MaterialConnectionString))
                throw new ERPErrorException("Connection error");

            if (this.OldLedgerName.ToUpper() != this.LedgerName.ToUpper())
            {
                if (0 != AccountCommonFunction.SafeConvertToInt(Database.GetExecuteScalarCommand_return(string.Format("SELECT COUNT(*) FROM LedgerMaster WHERE CompanyName='{0}' AND LedgerName='{1}'", this.CompanyName, this.LedgerName))))
                {
                    throw new ERPErrorException("Modified ledgername already in selected company. You can't change ledgername which was already in selected company");
                }
            }

            MakeAddressString();

            string query = string.Format("UPDATE LedgerMaster SET "
                + "CompanyName='{0}',LedgerName='{1}',Under='{2}',Openingbalance='{3}',PendingBalance='{4}',Category='{5}',IsTDS='{6}',IsNaturePayment='{7}',Title='{8}',"  // 0-8
                + "Houseno='{9}',Landmark1='{10}',Landmark2='{11}',City='{12}',State='{13}',Country='{14}',Address='{15}',PINCode='{16}',ISMRN='{17}',ISInvoice='{18}',"   // 9-18
                + "TaxType='{19}',BSRCode='{20}',TDSDeductee='{21}',Type='{22}',PercentageofCalculation='{23}',MethodofCalculation='{24}',vatclass='{25}',"   // 19-25
                + "AccessbleValue='{26}',ExciseNo='{27}',PANNo='{28}',ECCNo='{29}',GSTNo='{30}',TINNo='{31}',IECCode='{32}',CSTNo='{33}',AliasCompanyName='{34}',TypeOfInvoice='{35}',VATIncluded='{36}',BankAccNo='{37}',MICRCode='{38}',Acctype='{39}',IFSCCode='{40}',BranchCode='{41}',"    // 26-41
                + "BranchName='{42}',PaymentTerms='{43}',ContactNo='{44}',IsBillWise='{45}',servicetax='{46}',BlockName='{47}',isAddDepreciation='{48}',SectionCode='{49}',underschedule6='{50}', IsInventory='{51}', IsCostCenter='{52}', RepresentiveName='{53}',MainCategory='{56}',NewGSTNo='{57}',"
                + "IsInterCompany='{58}' ,IsAccClose='{59}',IsCashFlowAllow='{60}',MSMENumber='{61}',TDSRate={62},BankName='{63}',IBANCode='{64}',swiftcode='{65}',email='{66}',website='{67}',Fromdate='{68}',ContactName='{69}',TelNo2='{70}',FaxNo='{71}',EMAILCC='{72}' ,IsTCSApp={73}"    // 42-53
                + " WHERE CompanyName='{54}' AND LedgerName='{55}'",
                this.CompanyName, this.LedgerName, this.GroupName, this.OpeningBalance, this.PendingBalance, this.Category, BoolToString(this.IsTDS), this.NaturePayment, this.LedgerTitle,
                this.HouseNo, this.LandMark1, this.LandMark2, this.City, this.State, this.Country, this.Address, this.PinCode, BoolToString(this.IsMRN), BoolToString(this.IsInvoice),
                this.TaxType, this.BRSCode, this.TDSDeductee, this.Type, this.PercentageOfCalculation, this.MethodOfCalculation, this.VatClass,
                BoolToString(this.IsAccessbleValue), this.ExciseNo, this.PANNo, this.ECCNo, this.GSTNo, this.VATNo, this.IECCode, this.CSTNo, this.AliasCompanyName, this.TypeOfInvoice, BoolToString(this.IsVATInclude), this.BankAC, this.MICRCode, this.AccountType, this.IEFSCCode, this.BranchCode,
                this.BranchName, this.PaymentTerms, this.ContactNo, BoolToString(this.IsBillWise), this.ServiceTaxNo, this.BlockName, BoolToString(this.IsAdditionDep), this.SectionCode, this.GroupSchedule6, BoolToString(this.IsInventory), BoolToString(this.IsCostCenter), this.RepresentiveName,
                this.OldCompanyName, this.OldLedgerName, this.MainCategory, this.NewGST, BoolToString(this.IsInterCompany), BoolToString(this.IsAccClose), BoolToString(this.IsCashFlowAllow), this.MSMENumber, Utility.SafeConvertToDouble(this.TDSRate), this.BankName, this.IBANCode, this.swiftcode,
                this.emailid, this.website, this.Fromdate, this.ContactName, this.TelNo2, this.FaxNo, this.EMAILCC, (this.IsTCSApp == true ? 1 : 0)
                );

            string query_update_transaction = string.Format(@"
                            UPDATE {3}..AccountBills SET LedgerName='{0}' WHERE LedgerName='{1}' '
                            UPDATE {3}..Journal SET PartyName='{0}' WHERE PartyName='{1}' 
                            UPDATE {3}..Journal SET CrPartyName='{0}' WHERE CrPartyName='{1}' 
                            UPDATE {3}..Journal SET DrPartyName='{0}' WHERE DrPartyName='{1}' 
                            UPDATE {3}..JournalItem SET PartyName='{0}' WHERE PartyName='{1}' 
                            UPDATE {3}..JournalInventory SET PartyName='{0}' WHERE PartyName='{1}' 
                            UPDATE {3}..Payment SET LedgerFrom='{0}' WHERE LedgerFrom='{1}' 
                            UPDATE {3}..Payment SET LedgerTo='{0}' WHERE LedgerTo='{1}' 
                            UPDATE {3}..PaymentAgstRef set PartyName='{0}' WHERE PartyName='{1}' 
                            UPDATE {3}..PaymentReceipt SET LedgerFrom='{0}' WHERE LedgerFrom='{1}' 
                            UPDATE {3}..PaymentReceipt SET LedgerTo='{0}' WHERE LedgerTo='{1}' 
                            UPDATE {3}..PaymentReceiptAgstRef SET PartyName='{0}' WHERE PartyName='{1}' 
                            UPDATE {3}..PurchaseVoucher SET SupplierName='{0}' WHERE SupplierName='{1}' 
                            UPDATE {3}..PurchaseVoucher SET PurchaseLedger='{0}' WHERE PurchaseLedger='{1}' 
                            UPDATE {3}..PurchaseVoucherREF SET vendorname='{0}' WHERE vendorname='{1}' 
                            UPDATE {3}..PurchaseVoucherTax SET TaxledgerName='{0}' WHERE TaxledgerName='{1}' 
                            UPDATE {3}..PurchaseVoucherTax SET PartyName='{0}' WHERE PartyName='{1}' 
                            UPDATE {3}..SalesVoucher SET BuyerName='{0}' WHERE BuyerName='{1}' 
                            UPDATE {3}..SalesVoucher SET salesLedger='{0}' WHERE salesLedger='{1}' 
                            UPDATE {3}..SalesVoucherRef SET partyname='{0}' WHERE partyname='{1}' 
                            UPDATE {3}..SalesVoucherTax SET TaxledgerName='{0}' WHERE TaxledgerName='{1}' 
                            UPDATE {3}..SalesVoucherTax SET PartyName='{0}' WHERE PartyName='{1}' 
                            UPDATE {3}..contra SET LedgerFrom='{0}' WHERE LedgerFrom='{1}' 
                            UPDATE {3}..contra SET LedgerTo='{0}' WHERE LedgerTo='{1}' 
                            UPDATE {3}..DebitNote SET PartyName='{0}' WHERE PartyName='{1}' 
                            UPDATE {3}..DebitNote SET PurchaseLedger='{0}' WHERE PurchaseLedger='{1}' 
                            UPDATE {3}..CreditNote SET PartyName='{0}' WHERE PartyName='{1}' 
                            UPDATE {3}..CreditNote SET SalesLedger='{0}' WHERE SalesLedger='{1}' 
                            UPDATE {3}..BankRecoentry SET LedgerFrom='{0}' WHERE LedgerFrom='{1}' 
                            UPDATE {3}..BankRecoentry SET Ledgerto='{0}' WHERE LedgerTo='{1}' 
                            UPDATE {3}..Categorylink SET Vatledger='{0}' WHERE Vatledger='{1}'
                            update {3}..categorylink set addVatledger = '{0}' where addVatledger = '{1}'
                            update {3}..categorylink set actualpurchaseledger = '{0}' where actualpurchaseledger = '{1}'
                            update {3}..ExciseLinkAccount set taxledgername = '{0}' where taxledgername = '{1}' 
                            update {3}..LedgerOpeningBalance set ledgername = '{0}' where ledgername = '{1}' 
                            update {4}..companymaster set accountcompanyname = '{0}' where accountcompanyname = '{1}'
                            update {3}..DepreciationOpening set ledgername = '{0}' where ledgername = '{1}' 
                            update {3}..DepreciationOpeningIT set ledgername = '{0}' where ledgername = '{1}' 
                            update {3}..LedgerVatClass set ledgername = '{0}' where ledgername = '{1}'
                            update {3}..Billpayble set ledgername = '{0}' where ledgername = '{1}' 
                            update {3}..billreceivable set ledgername = '{0}' where ledgername = '{1}' 
                            update {3}..item set ledger = '{0}' where ledger = '{1}' 
                            update {3}..CostCenterEntries set LedgerName='{0}' WHERE LedgerName='{1}' 
                            update p set PartyLedgerName='{0}' from {3}..ac_voucher_party p inner join {3}..FactoryInfo fi on p.CompanyId=fi.srno where PartyLedgerName='{1}' 
                            update p set SecondLedgerName='{0}' from {3}..ac_voucher_party p inner join {3}..FactoryInfo fi on p.CompanyId=fi.srno where SecondLedgerName='{1}' 
                            ", this.LedgerName, this.OldLedgerName, this.OldCompanyName, "MaterialProcessing", "Despatch");

            string HistoryRemarks = "Modify : ";
            if (this.OldLedgerName != this.LedgerName)
                HistoryRemarks += string.Format("Ledger Name Change to {0}", this.LedgerName);
            if (UpdateRemarks.Length > 0)
                HistoryRemarks += "\n" + UpdateRemarks;
            try
            {
                Database.BeginTransaction();

                Database.GetExecuteNonQueryCommand(query);
                if (this.IsAccClose && this.ToDate.Length == 0)
                {
                    Database.GetExecuteNonQueryCommand("Update LedgerMaster set ToDate=cast(getdate() as date) where Srno=" + this.LedgerId);
                }
                if (!this.IsAccClose)
                {
                    Database.GetExecuteNonQueryCommand("Update LedgerMaster set ToDate=NULL where Srno=" + this.LedgerId);
                }
                // update ledgername on all transaction when ledgername change
                if (this.OldLedgerName != this.LedgerName)
                {
                    Database.GetExecuteNonQueryCommand(query_update_transaction);
                    #region Change Vendor Name
                    Database.ExecuteNonQuery(@"
                                    update finalquotation set firmname=@VendorName where firmname=@OldVendorName
                                    update VendorRate set firmname=@VendorName where firmname=@OldVendorName
                                    update storeinwardspayment set PartyName=@VendorName where PartyName=@OldVendorName
                                    update jobworkorderitem set firmname=@VendorName where firmname=@OldVendorName
                                    UPDATE BillPaymentEntry SET VendorName=@VendorName where VendorName=@OldVendorName
                                    UPDATE BillPaymentHODApproval SET PartyName=@VendorName where PartyName=@OldVendorName
                                    ",
                                    new Database.Parameter("@VendorName", this.LedgerName),
                                    new Database.Parameter("@OldVendorName", this.OldLedgerName)
                                    );
                    #endregion
                    #region Customer Name Change
                    Database.GetExecuteNonQueryCommand("update materialprocessing..exportbilladvance  set customername ='" + this.LedgerName + "' where customername ='" + this.OldLedgerName + "'");
                    Database.GetExecuteNonQueryCommand("update  materialprocessing..financebillexport  set buyer='" + this.LedgerName + "' where buyer='" + this.OldLedgerName + "'");
                    Database.GetExecuteNonQueryCommand("update CompanyMaster set companyname ='" + this.LedgerName + "' where companyname  ='" + this.OldLedgerName + "'");
                    Database.GetExecuteNonQueryCommand("update Domesticinvoice set Buyername='" + this.LedgerName + "' where Buyername='" + this.OldLedgerName + "'");
                    Database.GetExecuteNonQueryCommand("update Exportinvoice set Buyername='" + this.LedgerName + "' where Buyername='" + this.OldLedgerName + "'");
                    Database.GetExecuteNonQueryCommand("update commercialinvoice set Buyername='" + this.LedgerName + "' where Buyername='" + this.OldLedgerName + "'");
                    Database.GetExecuteNonQueryCommand("update packinglist set Buyers='" + this.LedgerName + "' where Buyers='" + this.OldLedgerName + "'");
                    Database.GetExecuteNonQueryCommand("update MarketingInvoice set Buyername='" + this.LedgerName + "' where Buyername='" + this.OldLedgerName + "'");
                    Database.GetExecuteNonQueryCommand("update Multilayerworkorder set billingname ='" + this.LedgerName + "' where billingname ='" + this.OldLedgerName + "'");
                    Database.GetExecuteNonQueryCommand("update BOPPWorkOrder set billingname ='" + this.LedgerName + "' where billingname ='" + this.OldLedgerName + "'");
                    Database.GetExecuteNonQueryCommand("update performainvoice set Buyername='" + this.LedgerName + "' where Buyername='" + this.OldLedgerName + "'");
                    Database.GetExecuteNonQueryCommand("update commercialperformainvoice set Buyername='" + this.LedgerName + "' where Buyername='" + this.OldLedgerName + "'");
                    Database.GetExecuteNonQueryCommand("update consolidatedbuyer set Buyername='" + this.LedgerName + "' where Buyername='" + this.OldLedgerName + "'");
                    Database.GetExecuteNonQueryCommand("update containertracking set customername ='" + this.LedgerName + "' where customername ='" + this.OldLedgerName + "'");
                    Database.GetExecuteNonQueryCommand("update InsidecompanyMaster set companyname ='" + this.LedgerName + "' where companyname ='" + this.OldLedgerName + "'");
                    Database.GetExecuteNonQueryCommand("update JobWorkInvoice  set Buyername ='" + this.LedgerName + "' where Buyername ='" + this.OldLedgerName + "'");
                    Database.GetExecuteNonQueryCommand("update challan5A set compnam  ='" + this.LedgerName + "' where compnam  ='" + this.OldLedgerName + "'");
                    Database.GetExecuteNonQueryCommand("update suborder set Subsidaryname  ='" + this.LedgerName + "' where Subsidaryname  ='" + this.OldLedgerName + "'");
                    Database.GetExecuteNonQueryCommand("update subsidiarychallan set Subsidiarybuyer  ='" + this.LedgerName + "' where  Subsidiarybuyer  ='" + this.OldLedgerName + "'");
                    Database.GetExecuteNonQueryCommand("update subsidiarychallanissuemulti set Subsidiarybuyer ='" + this.LedgerName + "' where Subsidiarybuyer  ='" + this.OldLedgerName + "'");
                    Database.GetExecuteNonQueryCommand("update subsidiarychallanmulti set Subsidiaryname ='" + this.LedgerName + "' where  Subsidiaryname ='" + this.OldLedgerName + "'");
                    #endregion
                }

                if (this.RecordLogId == 0)
                {
                    this.RecordLogId = AccountCommonFunction.SaveRecordLog(null, FrmMain.UserName, RecordLogFlag.Regular, "Record Id Add");
                    Database.ExecuteNonQuery("UPDATE LedgerMaster SET RecordLogId=@RecordLogId WHERE srno=@srno",
                        new Database.Parameter("@RecordLogId", this.RecordLogId),
                        new Database.Parameter("@srno", this.LedgerId)
                        );
                }


                AccountCommonFunction.SaveRecordLog(this.RecordLogId, FrmMain.UserName, RecordLogFlag.Regular, HistoryRemarks);

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
        /// Delete ledger
        /// </summary>
        /// <returns>True on success</returns>
        public bool Delete()
        {
            // Entry Not available, Throw Exception
            if (this.OldLedgerName.Length == 0 && this.OldCompanyName.Length == 0)
            {
                throw new ERPErrorException("Nothing to delete");
            }

            if (!Database.OpenConnection(Utility.MaterialConnectionString))
                throw new ERPErrorException("Connection error");

            // Dont delete if ledger have transaction
            if (0 != AccountCommonFunction.SafeConvertToInt(Database.GetExecuteScalarCommand_return(string.Format("SELECT COUNT(*) FROM vw_LedgerSummary WHERE CompanyName='{0}' AND LedgerName='{1}' and VoucherType!='z'", this.OldCompanyName, this.LedgerName))))
                throw new ERPErrorException("This ledger have transaction. You can't deleted this ledger");

            try
            {
                Database.BeginTransaction();

                Database.GetExecuteNonQueryCommand("insert into LedgerMaster_deleted(CompanyName,LedgerName,Under,Openingbalance,PendingBalance,srno,Category,IsTDS,IsNaturePayment,Title,Houseno,Landmark1,Landmark2,City,State,Country,Address,PINCode,ISMRN,ISInvoice,TaxType,BSRCode,TDSDeductee,Type,PercentageofCalculation,MethodofCalculation,vatclass,AccessbleValue,ExciseNo,PANNo,ECCNo,GSTNo,TINNo,IECCode,CSTNo,AliasCompanyName,TypeOfInvoice,VATIncluded,BankAccNo,MICRCode,Acctype,IFSCCode,BranchCode,BranchName,PaymentTerms,ContactNo,IsBillWise,servicetax,BlockName,isAddDepreciation,SectionCode,underschedule6,IsInventory,IsCostCenter,RepresentiveName,RecordLogId,LVendorCode,LCompanyCode,MainCategory,NewGSTNo,IsInterCompany,IsAccClose,IsCashFlowAllow,UserName,IsPNLAc,tdsrate,MSMENumber,companyId, BankName,IBANCode,swiftcode) " +
                            " select CompanyName,LedgerName,Under,Openingbalance,PendingBalance,srno,Category,IsTDS,IsNaturePayment,Title,Houseno,Landmark1,Landmark2,City,State,Country,Address,PINCode,ISMRN,ISInvoice,TaxType,BSRCode,TDSDeductee,Type,PercentageofCalculation,MethodofCalculation,vatclass,AccessbleValue,ExciseNo,PANNo,ECCNo,GSTNo,TINNo,IECCode,CSTNo,AliasCompanyName,TypeOfInvoice,VATIncluded,BankAccNo,MICRCode,Acctype,IFSCCode,BranchCode,BranchName,PaymentTerms,ContactNo,IsBillWise,servicetax,BlockName,isAddDepreciation,SectionCode,underschedule6,IsInventory,IsCostCenter,RepresentiveName,RecordLogId,LVendorCode,LCompanyCode,MainCategory,NewGSTNo,IsInterCompany,IsAccClose,IsCashFlowAllow,'" + FrmMain.UserName + "',isnull(IsPNLAc,0),tdsrate,MSMENumber,companyId,BankName,IBANCode,swiftcode from LedgerMaster where LedgerName ='" +
                            this.OldLedgerName + "'  and CompanyName='" + this.OldCompanyName + "' ");

                Database.GetExecuteNonQueryCommand(string.Format("DELETE FROM LedgerMaster WHERE CompanyName='{0}' AND LedgerName='{1}'", this.OldCompanyName, this.OldLedgerName));

                if (this.RecordLogId == 0)
                {
                    this.RecordLogId = AccountCommonFunction.SaveRecordLog(null, FrmMain.UserName, RecordLogFlag.Regular, "Record Id Add");
                    Database.ExecuteNonQuery("UPDATE LedgerMaster SET RecordLogId=@RecordLogId WHERE srno=@srno",
                        new Database.Parameter("@RecordLogId", this.RecordLogId),
                        new Database.Parameter("@srno", this.LedgerId)
                        );
                }

                AccountCommonFunction.SaveRecordLog(this.RecordLogId, FrmMain.UserName, RecordLogFlag.Deleted, string.Format("Ledger Deleted (Ledger:{0}, Company:{1}", this.OldLedgerName, this.OldCompanyName));

                Database.CommitTransaction();
            }
            catch (Exception ex)
            {
                Database.RollBackTransaction();
                throw ex;
            }

            return true;
        }

        public static int GetRecordLogId(int LedgerId)
        {
            int RecordLogId = 0;

            object objRecordLogId = Database.ExecuteScalar("SELECT RecordLogId FROM MaterialProcessing..LedgerMaster WHERE srno=@LedgerId",
                new Database.Parameter("@LedgerId", LedgerId)
                );

            // scalar return null when no record found
            if (objRecordLogId == null)
                throw new ERPErrorException(AccountCommonVariable.ErrMsg.ENTRY_NOT_FOUND);

            if (objRecordLogId == DBNull.Value)
            {
                // Record Log Id is currently blank
                // So we first fill that
                RecordLogId = AccountCommonFunction.SaveRecordLog(null, FrmMain.UserName, RecordLogFlag.Regular, "Record Id Add");

                Database.ExecuteNonQuery("UPDATE MaterialProcessing..LedgerMaster SET RecordLogId=@RecordLogId WHERE srno=@srno",
                    new Database.Parameter("@RecordLogId", RecordLogId),
                    new Database.Parameter("@srno", LedgerId)
                    );
            }
            else
            {
                // Record Log Id exist
                // Return it
                RecordLogId = (int)objRecordLogId;
            }

            return RecordLogId;
        }

        /// <summary>
        /// Update Ledger Name
        /// </summary>
        /// <param name="CompanyName">Company Name</param>
        /// <param name="LedgerName">Ledger Name</param>
        /// <param name="NewLedgerName">New Ledger Name</param>
        public static void UpdateLedgerName(string CompanyName, string LedgerName, string NewLedgerName, int NewLedgerId)
        {
            #region Affected tables
            /** 
             * Affected tables
             * 
             * Tables
             * =====================================================
             * 1. Journal
             * -----------------------------------------------------
             * Journal (PartyName, CrPartyName, DrPartyName)
             * JournalItem (PartyName)
             * JournalInventory (PartyName)
             * 
             * -----------------------------------------------------
             * 2. Payment
             * -----------------------------------------------------
             * Payment (LedgerFrom, LedgerTo)
             * PaymentAgstRef (PartyName)
             * 
             * -----------------------------------------------------
             * 3. Receipt
             * -----------------------------------------------------
             * PaymentReceipt (LedgerFrom, LedgerTo)
             * PaymentReceiptAgstRef (PartyName)
             * 
             * -----------------------------------------------------
             * 4. Purchase
             * -----------------------------------------------------
             * PurchaseVoucher (SupplierName, PurchaseLedger)
             * PurchaseVoucherRef (vendorname)
             * PurchaseVoucherTax (TaxLedgerName, PartyName)
             * 
             * -----------------------------------------------------
             * 5. Sales
             * -----------------------------------------------------
             * Sales Voucher (BuyerName, SalesLedger)
             * SalesVoucherRef (partyname)
             * SalesVoucherTax (TaxLedgerName, PartyName)
             * 
             * -----------------------------------------------------
             * 6. Contra (LedgerFrom, LedgerTo)
             * 7. DebitNote (PartyName, PurchaseLedger)
             * 8. CreditNote (PartyName, SalesLedger)
             * 9. BankRecoentry (LedgerFrom, LedgerTo)
             * 
             * 10. Categorylink (VatLedger, addVatLedger, actualpurchaseledger)
             * 11. ExciseLinkAccount (taxledgername)
             * 12. LedgerOpeningBalance (ledgername)
             * 13. companymaster (accountcompanyname)
             * 14. DepreciationOpening (ledgername)
             * 15. LedgerVatClass (ledgername)
             * 16. BillPayble (ledgername)
             * 17. BillReceivable (ledgername)
             * 18. item (ledger)
             * 19. CostCenterEntries (LedgerName)
             * 
             * 20. AccountBills (LedgerName)
             * 21. VoucherParty
             * 
             */
            #endregion


            string query_update_transaction = string.Format(@"
update b set b.LedgerName='{0}',LedgerId={4}
from {3}..AccountBills b
left join (
	select * from {3}..AccountBills
	where CompanyName='{2}'
	and LedgerName='{0}'
) as t2 on b.BillNo=t2.BillNo and b.BillDate=t2.BillDate
where t2.BillId is null
and b.CompanyName='{2}'
and b.LedgerName='{1}'

UPDATE {3}..Journal SET PartyName='{0}' ,PartyID={4} WHERE PartyName='{1}' AND CompanyName='{2}'
UPDATE {3}..Journal SET CrPartyName='{0}',CrPartyID={4} WHERE CrPartyName='{1}' AND CompanyName='{2}'
UPDATE {3}..Journal SET DrPartyName='{0}',DrPartyID={4} WHERE DrPartyName='{1}' AND CompanyName='{2}'
UPDATE {3}..JournalItem SET PartyName='{0}',PartyID={4} WHERE PartyName='{1}' AND CompanyName='{2}'
UPDATE {3}..JournalInventory SET PartyName='{0}',PartyID={4} WHERE PartyName='{1}' AND CompanyName='{2}'
UPDATE {3}..Payment SET LedgerFrom='{0}',LedgerFromID={4} WHERE LedgerFrom='{1}' AND CompanyName='{2}'
UPDATE {3}..Payment SET LedgerTo='{0}',LedgerToID={4} WHERE LedgerTo='{1}' AND CompanyName='{2}'
UPDATE {3}..PaymentAgstRef set PartyName='{0}',PartyID={4} WHERE PartyName='{1}' AND CompanyName='{2}'
UPDATE {3}..PaymentReceipt SET LedgerFrom='{0}',LedgerFromID={4} WHERE LedgerFrom='{1}' AND CompanyName='{2}'
UPDATE {3}..PaymentReceipt SET LedgerTo='{0}',LedgerToID={4} WHERE LedgerTo='{1}' AND CompanyName='{2}'
UPDATE {3}..PaymentReceiptAgstRef SET PartyName='{0}',PartyID={4} WHERE PartyName='{1}' AND CompanyName='{2}'
UPDATE {3}..PurchaseVoucher SET SupplierName='{0}',SupplierID={4} WHERE SupplierName='{1}' AND CompanyName='{2}'
UPDATE {3}..PurchaseVoucher SET PurchaseLedger='{0}',PurchaseLedgerID={4} WHERE PurchaseLedger='{1}' AND CompanyName='{2}'
UPDATE {3}..PurchaseVoucherREF SET vendorname='{0}',VendorID={4} WHERE vendorname='{1}' AND CompanyName='{2}'
UPDATE {3}..PurchaseVoucherTax SET TaxledgerName='{0}',TaxledgerId={4} WHERE TaxledgerName='{1}' AND CompanyName='{2}'
UPDATE {3}..PurchaseVoucherTax SET PartyName='{0}',PartyId={4} WHERE PartyName='{1}' AND CompanyName='{2}'
UPDATE {3}..SalesVoucher SET BuyerName='{0}',BuyerID={4} WHERE BuyerName='{1}' AND CompanyName='{2}'
UPDATE {3}..SalesVoucher SET salesLedger='{0}',salesLedgerID={4} WHERE salesLedger='{1}' AND CompanyName='{2}'
UPDATE {3}..SalesVoucherRef SET partyname='{0}',PartyID={4} WHERE partyname='{1}' AND CompanyName='{2}'
UPDATE {3}..SalesVoucherTax SET TaxledgerName='{0}',TaxledgerID={4} WHERE TaxledgerName='{1}' AND CompanyName='{2}'
UPDATE {3}..SalesVoucherTax SET PartyName='{0}',PartyID={4} WHERE PartyName='{1}' AND CompanyName='{2}'
UPDATE {3}..contra SET LedgerFrom='{0}',LedgerFromID={4} WHERE LedgerFrom='{1}' AND CompanyName='{2}'
UPDATE {3}..contra SET LedgerTo='{0}',LedgerToID={4} WHERE LedgerTo='{1}' AND CompanyName='{2}'
UPDATE {3}..DebitNote SET PartyName='{0}',PartyID={4} WHERE PartyName='{1}' AND CompanyName='{2}'
UPDATE {3}..DebitNote SET PurchaseLedger='{0}',PurchaseLedgerID={4} WHERE PurchaseLedger='{1}' AND CompanyName='{2}'
UPDATE {3}..CreditNote SET PartyName='{0}',PartyID={4} WHERE PartyName='{1}' AND CompanyName='{2}'
UPDATE {3}..CreditNote SET SalesLedger='{0}',SalesLedgerID={4} WHERE SalesLedger='{1}' AND CompanyName='{2}'
UPDATE {3}..BankRecoentry SET LedgerFrom='{0}' WHERE LedgerFrom='{1}' AND CompanyName='{2}'
UPDATE {3}..BankRecoentry SET Ledgerto='{0}' WHERE LedgerTo='{1}' AND CompanyName='{2}'
UPDATE {3}..Categorylink SET Vatledger='{0}' WHERE Vatledger='{1}'
update {3}..categorylink set addVatledger = '{0}' where addVatledger = '{1}'
update {3}..categorylink set actualpurchaseledger = '{0}' where actualpurchaseledger = '{1}'
update {3}..ExciseLinkAccount set taxledgername = '{0}' where taxledgername = '{1}' and CompanyName='{2}'
update {3}..LedgerOpeningBalance set ledgername = '{0}' where ledgername = '{1}' and companyname = '{2}'
update {5}..companymaster set accountcompanyname = '{0}' where accountcompanyname = '{1}'
update {3}..DepreciationOpening set ledgername = '{0}' where ledgername = '{1}' and companyname = '{2}'
update {3}..DepreciationOpeningIT set ledgername = '{0}' where ledgername = '{1}' and companyname = '{2}'
update {3}..LedgerVatClass set ledgername = '{0}' where ledgername = '{1}'
update {3}..Billpayble set ledgername = '{0}',LEDGERID={4} where ledgername = '{1}' and companyname = '{2}'
update {3}..billreceivable set ledgername = '{0}',LEDGERID={4} where ledgername = '{1}' and companyname = '{2}'
update {3}..item set ledger = '{0}' where ledger = '{1}' and companyname = '{2}'
update {3}..CostCenterEntries set LedgerName='{0}',LedgerID={4} WHERE LedgerName='{1}' and CompanyName='{2}'
update p set PartyLedgerName='{0}' from {3}..ac_voucher_party p inner join {3}..FactoryInfo fi on p.CompanyId=fi.srno where PartyLedgerName='{1}' and fi.Name='{2}'
update p set SecondLedgerName='{0}' from {3}..ac_voucher_party p inner join {3}..FactoryInfo fi on p.CompanyId=fi.srno where SecondLedgerName='{1}' and fi.Name='{2}'
                            ", NewLedgerName, LedgerName, CompanyName, "MaterialProcessing", NewLedgerId, "Despatch");

            Database.GetExecuteNonQueryCommand(query_update_transaction);
        }
    }

}
