namespace POApprovalAPI.Models;

public class DomesticInvoiceLookups
{
    public List<string> Companies { get; set; } = [];
    public List<string> Years { get; set; } = [];
    public List<string> Departments { get; set; } = [];
    public List<string> Transporters { get; set; } = [];
    public List<string> Currencies { get; set; } = [];
    public List<string> States { get; set; } = [];
    public List<string> PaymentTerms { get; set; } = [];
    public List<string> Buyers { get; set; } = [];
    public List<string> Vehicles { get; set; } = [];
    public List<string> Destinations { get; set; } = [];
    public List<string> AccountHeads { get; set; } = [];
    public List<string> Units { get; set; } = [];
    public List<string> MainGroups { get; set; } = [];
    public List<DomesticItemGroup> Groups { get; set; } = [];
    public List<DomesticItemSubGroup> SubGroups { get; set; } = [];
    public List<DomesticCommodity> Commodities { get; set; } = [];
    public List<DomesticItemOption> Items { get; set; } = [];
    public List<string> UtBonds { get; set; } = [];
    public List<string> Vendors { get; set; } = [];
    public string FactoryState { get; set; } = "";
    public string CompanyGstNo { get; set; } = "";
    public string DispatchCode { get; set; } = "";
    public bool MapPackingList { get; set; }
    public decimal ExciseDuty { get; set; }
    public decimal EduCess { get; set; }
    public decimal SheCess { get; set; }
}

public class DomesticItemGroup
{
    public string Deptt { get; set; } = "";
    public string GroupName { get; set; } = "";
}

public class DomesticItemSubGroup
{
    public string Deptt { get; set; } = "";
    public string GroupName { get; set; } = "";
    public string SubGroupName { get; set; } = "";
}

public class DomesticCommodity
{
    public string Name { get; set; } = "";
    public string Deptt { get; set; } = "";
    public string TariffHeading { get; set; } = "";
}

public class DomesticItemOption
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Deptt { get; set; } = "";
    public string GroupName { get; set; } = "";
}

public class DomesticConsigneeOption
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string City { get; set; } = "";
    public string Address { get; set; } = "";
}

public class UserCompanyDto
{
    public string CompanyName { get; set; } = "";
    public string Message { get; set; } = "";
}

public class BuyerTaxDto
{
    public string VatNo { get; set; } = "";
    public string GstNo { get; set; } = "";
    public bool IsAfTin { get; set; }
    public string NewGstNo { get; set; } = "";
}

public class DomesticTaxRate
{
    public string TariffHeading { get; set; } = "";
    public decimal Cgst { get; set; }
    public decimal Sgst { get; set; }
    public decimal Igst { get; set; }
}

public class DomesticInvoiceLineDto
{
    public string NoOfPkg { get; set; } = "";
    public string MainGroup { get; set; } = "";
    public string GroupName { get; set; } = "";
    public string SubGroup { get; set; } = "";
    public string ItemCode { get; set; } = "";
    public string Commodity { get; set; } = "";
    public string ItemDesc { get; set; } = "";
    public string Deptt { get; set; } = "";
    public string Uom { get; set; } = "";
    public string Hsn { get; set; } = "";
    public decimal Qty { get; set; }
    public decimal Rate { get; set; }
    public decimal Amount { get; set; }
    public decimal CgstRate { get; set; }
    public decimal CgstAmt { get; set; }
    public decimal SgstRate { get; set; }
    public decimal SgstAmt { get; set; }
    public decimal IgstRate { get; set; }
    public decimal IgstAmt { get; set; }
    public decimal NetWt { get; set; }
    public decimal GrossWt { get; set; }
    public decimal NonDisplayQty { get; set; }
    public string NonDisplayUnit { get; set; } = "KGS";
    public string QtyRemarks { get; set; } = "";
    public string CommodityRemarks { get; set; } = "";
}

public class DomesticInvoiceDto
{
    public int InvNo { get; set; }
    public string InvType { get; set; } = "";
    public string GstInvNo { get; set; } = "";
    public string CompanyName { get; set; } = "";
    public string Suffix { get; set; } = "";
    public string TypeOfInvoice { get; set; } = "";
    public string BuyerName { get; set; } = "";
    public string InvDate { get; set; } = "";
    public string DelChallanNo { get; set; } = "";
    public string DelChallanDate { get; set; } = "";
    public string LrNo { get; set; } = "";
    public string LrDate { get; set; } = "";
    public string DespatchNo { get; set; } = "";
    public string DespatchDate { get; set; } = "";
    public string TransportName { get; set; } = "";
    public string VehicleNo { get; set; } = "";
    public string VehicleDate { get; set; } = "";
    public string Destination { get; set; } = "";
    public string TermsOfDelivery { get; set; } = "FOR";
    public string State { get; set; } = "";
    public string MobileNo { get; set; } = "";
    public string InvoiceDateTime { get; set; } = "";
    public string RemovalDateTime { get; set; } = "";
    public string BuyerOrderNo { get; set; } = "";
    public string BuyerOrderDate { get; set; } = "";
    public string TermsOfPayment { get; set; } = "";
    public string Department { get; set; } = "";
    public string AccountHead { get; set; } = "";
    public string TariffHeading { get; set; } = "";
    public bool Insurance { get; set; }
    public string InsurancePolicyNo { get; set; } = "";
    public bool PurchaseReturn { get; set; }
    public string AdvanceLicNo { get; set; } = "";
    public string UtBondNo { get; set; } = "";
    public string Are1No { get; set; } = "";
    public bool DirectExport { get; set; }
    public decimal ExDuty { get; set; }
    public decimal EduCess { get; set; }
    public decimal SheCess { get; set; }
    public string TaxType { get; set; } = "VAT";
    public decimal Vat { get; set; }
    public decimal AddVat { get; set; }
    public string ExciseApplicable { get; set; } = "NIL";
    public string VatApplicable { get; set; } = "NIL";
    public string ExciseNilRemarks { get; set; } = "";
    public string VatNilRemarks { get; set; } = "";
    public decimal Freight { get; set; }
    public decimal TcsPercent { get; set; }
    public bool GstAfterFreight { get; set; }
    public string Remarks { get; set; } = "";
    public string FreightRemarks { get; set; } = "";
    public string Rg23 { get; set; } = "";
    public bool IgstNilNotification { get; set; }
    public string Currency { get; set; } = "";
    public string CurrencyValue { get; set; } = "";
    public bool AvailBenefit { get; set; }
    public string UtBondDetails { get; set; } = "";
    public string ConsigneeName { get; set; } = "";
    public string ConsigneeCity { get; set; } = "";
    public string ConsigneeAddress { get; set; } = "";
    public string ConsigneeCode { get; set; } = "";
    public string Notifier { get; set; } = "";
    public bool DispatchFrom { get; set; }
    public string DispatchFromName { get; set; } = "";
    public string DispatchFromAddress { get; set; } = "";
    public string SpDutyRemarks1 { get; set; } = "";
    public decimal SpDutyAmt1 { get; set; }
    public string SpDutyRemarks2 { get; set; } = "";
    public decimal SpDutyAmt2 { get; set; }
    public string SpDutyRemarks3 { get; set; } = "";
    public decimal SpDutyAmt3 { get; set; }
    public bool GstOnSpDuty1 { get; set; }
    public bool GstOnSpDuty2 { get; set; }
    public bool GstOnSpDuty3 { get; set; }
    public bool MerchantExport { get; set; }
    public string Notification { get; set; } = "";
    public string AdvanceLicense { get; set; } = "";
    public decimal OelBasic { get; set; } = 5;
    public decimal OelCvd { get; set; }
    public decimal OelCess1 { get; set; } = 10;
    public decimal OelShe1 { get; set; }
    public decimal OelCess2 { get; set; }
    public decimal OelShe2 { get; set; }
    public decimal OelCess3 { get; set; }
    public decimal OelShe3 { get; set; }
    public decimal OelAddDuty { get; set; }
    public string EwayNo { get; set; } = "";
    public string PackingListNo { get; set; } = "";
    public decimal GrandTotal { get; set; }
    public bool Printed { get; set; }
    public bool Marketing { get; set; }
    public string Header { get; set; } = "";
    public List<DomesticInvoiceLineDto> Lines { get; set; } = [];
}

public class DomesticInvoiceSaveResult
{
    public int InvNo { get; set; }
    public string InvType { get; set; } = "";
    public string GstInvNo { get; set; } = "";
    public string Message { get; set; } = "";
}

public class DomesticPackingListRow
{
    public string PackingListNo { get; set; } = "";
    public string InvNo { get; set; } = "";
    public string PartyName { get; set; } = "";
    public string DespatchDate { get; set; } = "";
    public decimal NetWt { get; set; }
}

public class DomesticPackingCheck
{
    public string CompanyName { get; set; } = "";
    public decimal InvoiceNetWt { get; set; }
    public List<string> PackingListNos { get; set; } = [];
    public List<DomesticInvoiceLineDto> Lines { get; set; } = [];
}

public class DomesticPrintDocument
{
    public string CopyTitle { get; set; } = "";
    public List<string> Copies { get; set; } = [];
    public bool AlreadyPrinted { get; set; }
    public bool EwayRequired { get; set; }
    public bool EwayGenerated { get; set; }
    public string ReportName { get; set; } = "";
    public DomesticInvoiceDto Invoice { get; set; } = new();
    public List<Dictionary<string, string?>> Rows { get; set; } = [];
}
