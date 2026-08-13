import type { ChatApiResponse } from "@/lib/chat-types";

/** Built-in mock scenarios for frontend UI testing (no API / LLM key). */
export interface MockScenario {
  id: string;
  label: string;
  /** Shown as a one-click example in mock mode. */
  examplePrompt: string;
  response: ChatApiResponse;
}

const MOCK_SQL_PREFIX = "-- mock:";

export const MOCK_SCENARIOS: MockScenario[] = [
  {
    id: "reorder-list",
    label: "List · below reorder (governed)",
    examplePrompt: "Items below reorder level at Oswal Extrusion Limited",
    response: {
      answer:
        "There are 108 item–godown combinations at Oswal Extrusion Limited where stock in hand is below the reorder level.\n\nThe most critical shortages are in Liner Bag and Bobbin godowns. Consider raising indents for the top items shown below.",
      sql: `${MOCK_SQL_PREFIX}reorder-list
SELECT TOP 50 ItemCode, ItemName, WareHouseName, StkInHand, ReOrder
FROM WareHouse
WHERE CompanyName = 'Oswal Extrusion Limited'
  AND StkInHand < ReOrder AND ReOrder > 0
ORDER BY ReOrder - StkInHand DESC`,
      tablesUsed: [
        { objectName: "WareHouse", domain: "WarehouseStock", score: 0.94 },
        { objectName: "ItemInfo", domain: "WarehouseStock", score: 0.61 },
      ],
      rows: [
        {
          ItemCode: "RM00142",
          ItemName: "PP Granules Natural",
          WareHouseName: "Bobbin Godown",
          StkInHand: 85,
          ReOrder: 500,
        },
        {
          ItemCode: "WIP00013",
          ItemName: "Liner Fabric Roll",
          WareHouseName: "Liner Bag Godown",
          StkInHand: 120,
          ReOrder: 450,
        },
        {
          ItemCode: "PKG00891",
          ItemName: "FIBC Liner 1.4 mil",
          WareHouseName: "FIBC Bag Godown",
          StkInHand: 240,
          ReOrder: 600,
        },
        {
          ItemCode: "FIG00001",
          ItemName: "FIBC Bag Type A",
          WareHouseName: "FG Godown",
          StkInHand: 18,
          ReOrder: 50,
        },
        {
          ItemCode: "YRN00204",
          ItemName: "PP Yarn 1100 DTEX",
          WareHouseName: "Yarn Godown",
          StkInHand: 920,
          ReOrder: 2000,
        },
        {
          ItemCode: "TAP00331",
          ItemName: "PP Tape Grey",
          WareHouseName: "Tape Plant Godown",
          StkInHand: 310,
          ReOrder: 800,
        },
        {
          ItemCode: "CHM00017",
          ItemName: "Anti-UV Masterbatch",
          WareHouseName: "Chemical Godown",
          StkInHand: 12,
          ReOrder: 40,
        },
        {
          ItemCode: "SP00109",
          ItemName: "Sewing Thread HDPE",
          WareHouseName: "Small Bag Godown",
          StkInHand: 55,
          ReOrder: 200,
        },
      ],
      rowCount: 8,
      totalCount: 108,
      truncated: true,
      warning:
        "Governed below-reorder query on WareHouse (StkInHand < ReOrder AND ReOrder > 0).",
    },
  },
  {
    id: "export-ranking",
    label: "List · top export customers",
    examplePrompt: "Top 5 export customers FY 2024-25 Plastene India group excluding inter company",
    response: {
      answer:
        "For financial year 2024-25, the top export customers for the Plastene India group (excluding inter-company buyers) are listed below.\n\nRafia Industries leads by bill amount, followed by Fibco and Comercial Pra Cafe.",
      sql: `${MOCK_SQL_PREFIX}export-ranking
SELECT TOP 5 BuyerName, SUM(BillAMount) AS TotalExport
FROM vw_Salesvoucher
WHERE InvType LIKE '%Export%'
  AND InvDate >= '2024-04-01' AND InvDate < '2025-04-01'
GROUP BY BuyerName
ORDER BY TotalExport DESC`,
      tablesUsed: [
        { objectName: "vw_Salesvoucher", domain: "Sales", score: 0.92 },
        { objectName: "InternalVendor", domain: "Sales", score: 0.78 },
        { objectName: "FactoryInfo", domain: "Sales", score: 0.71 },
      ],
      rows: [
        { BuyerName: "Rafia Industries Pvt Ltd", TotalExport: 84500000 },
        { BuyerName: "Fibco Sacks International", TotalExport: 62100000 },
        { BuyerName: "Comercial Pra Cafe Ltda", TotalExport: 48900000 },
        { BuyerName: "Juta Bags GmbH", TotalExport: 35600000 },
        { BuyerName: "Technopac Solutions BV", TotalExport: 29100000 },
      ],
      rowCount: 5,
      truncated: false,
      warning:
        "Governed top export customers; inter-company excluded via InternalVendor + FactoryInfo.",
    },
  },
  {
    id: "payment-summary",
    label: "Summary · approved payment total",
    examplePrompt: "Total approved bill payments in FY 2024-25 for Oswal Extrusion",
    response: {
      answer:
        "The total value of approved bill payments for Oswal Extrusion Limited in financial year 2024-25 is shown below.\n\nThis includes payments approved through both standard and HOD approval paths.",
      sql: `${MOCK_SQL_PREFIX}payment-summary
SELECT SUM(PaymentAmount) AS TotalApproved
FROM BillPaymentEntry
WHERE CompanyName = 'Oswal Extrusion Limited'
  AND status = 'Approved'
  AND PaymentDate >= '2024-04-01' AND PaymentDate < '2025-04-01'`,
      tablesUsed: [
        { objectName: "BillPaymentEntry", domain: "Payment", score: 0.89 },
        { objectName: "ApprovePayment", domain: "Payment", score: 0.72 },
      ],
      rows: [{ TotalApproved: 124567890.45 }],
      rowCount: 1,
      truncated: false,
      warning: "Governed approved payment total (HOD + entry-only approved UNION).",
    },
  },
  {
    id: "pending-po-table",
    label: "Table · pending PO details",
    examplePrompt: "Pending purchase orders for approval at Oswal Extrusion Limited",
    response: {
      answer:
        "These purchase orders are currently waiting for approval at Oswal Extrusion Limited.\n\nReview firm name, amount, and request date before approving from the PO module.",
      sql: `${MOCK_SQL_PREFIX}pending-po-table
SELECT TOP 50 PoNo, FirmName, ItemCode, ItemName, Qty, Rate, Amount, PODate, LoginName, status
FROM Vw_PurchaseOrder p
WHERE EXISTS (SELECT 1 FROM ApprovePO a WHERE a.PoNo = p.PoNo AND a.status = 'Pending')
  AND p.CompanyName = 'Oswal Extrusion Limited'`,
      tablesUsed: [
        { objectName: "Vw_PurchaseOrder", domain: "PO", score: 0.91 },
        { objectName: "ApprovePO", domain: "PO", score: 0.88 },
        { objectName: "ApprovePOHOD", domain: "PO", score: 0.65 },
      ],
      rows: [
        {
          PoNo: "OEL/PO/25-26/1842",
          FirmName: "Bright Rubber Udyog",
          ItemCode: "RM00218",
          ItemName: "Rubber Compound NR",
          Qty: 1200,
          Rate: 185.5,
          Amount: 222600,
          PODate: "2025-08-10",
          LoginName: "Rajesh.Kumar",
          status: "Pending",
        },
        {
          PoNo: "OEL/PO/25-26/1839",
          FirmName: "Chemline Polymers",
          ItemCode: "CHM00017",
          ItemName: "Anti-UV Masterbatch",
          Qty: 500,
          Rate: 420,
          Amount: 210000,
          PODate: "2025-08-09",
          LoginName: "Priya.S",
          status: "Pending",
        },
        {
          PoNo: "OEL/PO/25-26/1831",
          FirmName: "Global Packaging LLP",
          ItemCode: "PKG00402",
          ItemName: "PP Liner Roll 14 GSM",
          Qty: 8000,
          Rate: 62.75,
          Amount: 502000,
          PODate: "2025-08-08",
          LoginName: "Amit.Verma",
          status: "Pending",
        },
        {
          PoNo: "OEL/PO/25-26/1827",
          FirmName: "Steel Wire Corp",
          ItemCode: "RM00089",
          ItemName: "GI Wire 12 SWG",
          Qty: 2500,
          Rate: 78,
          Amount: 195000,
          PODate: "2025-08-07",
          LoginName: "Rajesh.Kumar",
          status: "Pending",
        },
        {
          PoNo: "OEL/PO/25-26/1820",
          FirmName: "Technopac Solutions",
          ItemCode: "SP00044",
          ItemName: "FIBC Belt 2 Ton",
          Qty: 400,
          Rate: 310,
          Amount: 124000,
          PODate: "2025-08-05",
          LoginName: "Suresh.P",
          status: "Pending",
        },
      ],
      rowCount: 5,
      totalCount: 23,
      truncated: true,
      warning: "Governed pending PO queue (ApprovePO ∪ ApprovePOHOD).",
    },
  },
  {
    id: "empty-result",
    label: "Empty · no matching records",
    examplePrompt: "Rejected purchase orders last month for XYZ Unknown Company",
    response: {
      answer:
        "No rejected purchase orders were found for the company and period you specified.\n\nThis may mean there were no rejections, or the company name did not match any records in the system.",
      sql: `${MOCK_SQL_PREFIX}empty-result
SELECT TOP 50 PoNo, FirmName, PODate, status
FROM Vw_PurchaseOrder
WHERE CompanyName LIKE '%XYZ Unknown%'
  AND status = 'Rejected'
  AND PODate >= DATEADD(month, -1, GETDATE())`,
      tablesUsed: [{ objectName: "Vw_PurchaseOrder", domain: "PO", score: 0.85 }],
      rows: [],
      rowCount: 0,
      totalCount: 0,
      truncated: false,
      warning: null,
    },
  },
  {
    id: "vendor-profile",
    label: "Summary · vendor key-value",
    examplePrompt: "Show vendor details and bank account for Bright Rubber",
    response: {
      answer:
        "Below are the master details and bank information for Bright Rubber Udyog as recorded in the vendor master.",
      sql: `${MOCK_SQL_PREFIX}vendor-profile
SELECT FirmName, VendorCode, NewGSTNo, PANNo, Email, BankAccountNo, BankName, IFSCCode, PaymentTerms
FROM vw_VendorListwithBankdtls
WHERE FirmName LIKE '%Bright Rubber%'`,
      tablesUsed: [
        { objectName: "Vendor", domain: "Vendor", score: 0.9 },
        { objectName: "vw_VendorListwithBankdtls", domain: "Vendor", score: 0.87 },
      ],
      rows: [
        {
          FirmName: "Bright Rubber Udyog",
          VendorCode: "Ven00171",
          NewGSTNo: "27AACFB1249A1Z5",
          PANNo: "AACFB1249A",
          Email: "accounts@brightrubber.com",
          BankAccountNo: "50100234567890",
          BankName: "HDFC Bank",
          IFSCCode: "HDFC0001234",
          PaymentTerms: "30 Days",
        },
      ],
      rowCount: 1,
      truncated: false,
      warning: "Governed vendor profile (Vendor / vw_VendorListwithBankdtls).",
    },
  },
];

const MOCK_BY_ID = new Map(MOCK_SCENARIOS.map((s) => [s.id, s]));

/** Keywords → scenario id (first match wins). */
const MOCK_TRIGGERS: { pattern: RegExp; id: string }[] = [
  { pattern: /\b(mock\s*empty|empty|no\s+results?|zero\s+rows?)\b/i, id: "empty-result" },
  { pattern: /\b(mock\s*summary|approved\s+payment|payment\s+total|total\s+approved)\b/i, id: "payment-summary" },
  { pattern: /\b(mock\s*table|pending\s+po|pending\s+purchase)\b/i, id: "pending-po-table" },
  { pattern: /\b(mock\s*export|top\s+\d*\s*export|export\s+customer)/i, id: "export-ranking" },
  { pattern: /\b(mock\s*vendor|vendor\s+details?|bank\s+account)\b/i, id: "vendor-profile" },
  { pattern: /\b(mock\s*list|reorder|below\s+reorder|stock)\b/i, id: "reorder-list" },
  { pattern: /^mock\b/i, id: "reorder-list" },
];

export function getMockExamplePrompts(): string[] {
  return MOCK_SCENARIOS.map((s) => s.examplePrompt);
}

export function parseMockIdFromSql(sql: string): string | null {
  const m = sql.match(/^-- mock:([a-z0-9-]+)/i);
  return m?.[1] ?? null;
}

export function getMockChatResponse(message: string): ChatApiResponse {
  const trimmed = message.trim();

  // Explicit: "mock:export-ranking" or "mock export-ranking"
  const explicit = trimmed.match(/^mock[:\s]+([a-z0-9-]+)/i);
  if (explicit) {
    const scenario = MOCK_BY_ID.get(explicit[1].toLowerCase());
    if (scenario) return cloneResponse(scenario.response, scenario.examplePrompt);
  }

  for (const { pattern, id } of MOCK_TRIGGERS) {
    if (pattern.test(trimmed)) {
      const scenario = MOCK_BY_ID.get(id);
      if (scenario) return cloneResponse(scenario.response, trimmed);
    }
  }

  // Cycle by message hash for unknown prompts so arbitrary typing still returns variety
  const idx =
    Math.abs(hashString(trimmed.toLowerCase())) % MOCK_SCENARIOS.length;
  const fallback = MOCK_SCENARIOS[idx];
  return cloneResponse(fallback.response, trimmed);
}

export function getMockExportRows(sql: string): Record<string, unknown>[] {
  const id = parseMockIdFromSql(sql);
  if (id) {
    const scenario = MOCK_BY_ID.get(id);
    if (scenario) return scenario.response.rows;
  }
  return [];
}

function cloneResponse(response: ChatApiResponse, _message: string): ChatApiResponse {
  return {
    ...response,
    rows: response.rows.map((r) => ({ ...r })),
    tablesUsed: response.tablesUsed.map((t) => ({ ...t })),
  };
}

function hashString(s: string): number {
  let h = 0;
  for (let i = 0; i < s.length; i++) h = (h * 31 + s.charCodeAt(i)) | 0;
  return h;
}

export function mockChatDelayMs(): number {
  return 650 + Math.floor(Math.random() * 350);
}
