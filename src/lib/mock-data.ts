export type POStatus = "Pending" | "Approved" | "Rejected";
export type ApprovalRole = "Purchase Head" | "HOD" | "Finance Manager" | "Director";

export interface ApprovalStep {
  role: ApprovalRole;
  user: string;
  status: POStatus;
  date?: string;
  remarks?: string;
}

export interface PurchaseOrder {
  poNumber: string;
  vendorName: string;
  poDate: string;
  amount: number;
  department: string;
  requestedBy: string;
  status: POStatus;
  description: string;
  items: { name: string; qty: number; rate: number }[];
  workflow: ApprovalStep[];
}

export interface HistoryEntry {
  id: string;
  poNumber: string;
  user: string;
  role: ApprovalRole;
  action: "Approved" | "Rejected" | "Submitted";
  status: POStatus;
  date: string;
  time: string;
  remarks: string;
}

const vendors = [
  "Tata Steel Ltd",
  "Bharat Forge",
  "L&T Heavy Engineering",
  "Siemens India",
  "ABB Industries",
  "Bosch Manufacturing",
  "Kirloskar Brothers",
  "Crompton Greaves",
  "Havells Industrial",
  "Schneider Electric",
  "Voltas Engineering",
  "Mahindra Logistics",
  "Godrej Industries",
  "Asian Paints Industrial",
  "Wipro Infrastructure",
];

const departments = ["Production", "Maintenance", "Quality", "R&D", "Plant Operations"];
const requesters = ["R. Sharma", "A. Verma", "S. Iyer", "K. Patel", "M. Gupta", "N. Singh"];

function makeWorkflow(status: POStatus, idx: number): ApprovalStep[] {
  const base: ApprovalStep[] = [
    { role: "Purchase Head", user: "V. Reddy", status: "Pending" },
    { role: "HOD", user: "P. Mehta", status: "Pending" },
    { role: "Finance Manager", user: "S. Kulkarni", status: "Pending" },
    { role: "Director", user: "A. Khanna", status: "Pending" },
  ];
  if (status === "Approved") {
    return base.map((s, i) => ({
      ...s,
      status: "Approved",
      date: `2025-05-${String(10 + i).padStart(2, "0")}`,
      remarks: "Approved as per budget.",
    }));
  }
  if (status === "Rejected") {
    const rejectAt = idx % 4;
    return base.map((s, i) => {
      if (i < rejectAt) return { ...s, status: "Approved", date: `2025-05-${String(10 + i).padStart(2, "0")}`, remarks: "OK" };
      if (i === rejectAt) return { ...s, status: "Rejected", date: `2025-05-${String(10 + i).padStart(2, "0")}`, remarks: "Quotation exceeds approved budget." };
      return s;
    });
  }
  // Pending — partial progress
  const progress = idx % 3;
  return base.map((s, i) => {
    if (i < progress) return { ...s, status: "Approved", date: `2025-05-${String(10 + i).padStart(2, "0")}`, remarks: "Verified" };
    return s;
  });
}

function rand(seed: number, max: number) {
  return Math.floor(((seed * 9301 + 49297) % 233280) / 233280 * max);
}

export const mockPOs: PurchaseOrder[] = Array.from({ length: 18 }, (_, i) => {
  const idx = i + 1;
  const statusCycle: POStatus[] = ["Pending", "Pending", "Pending", "Pending", "Pending", "Pending", "Pending", "Approved", "Approved", "Approved", "Approved", "Rejected", "Rejected", "Pending", "Approved", "Pending", "Rejected", "Pending"];
  const status = statusCycle[i];
  const amount = 50000 + rand(idx, 4500000);
  const day = String(((idx * 3) % 27) + 1).padStart(2, "0");
  const month = String(((idx % 6) + 1)).padStart(2, "0");
  return {
    poNumber: `PO/2025/${String(1000 + idx).padStart(4, "0")}`,
    vendorName: vendors[i % vendors.length],
    poDate: `2025-${month}-${day}`,
    amount,
    department: departments[i % departments.length],
    requestedBy: requesters[i % requesters.length],
    status,
    description: "Procurement of industrial equipment & raw materials for plant operations.",
    items: [
      { name: "Industrial Bearings 6205-ZZ", qty: 50 + rand(idx, 100), rate: 450 },
      { name: "MS Steel Plate 10mm", qty: 20 + rand(idx + 1, 40), rate: 8500 },
      { name: "Hydraulic Hose Assembly", qty: 10 + rand(idx + 2, 20), rate: 2200 },
      { name: "Electrical Contactor 32A", qty: 5 + rand(idx + 3, 15), rate: 3400 },
    ],
    workflow: makeWorkflow(status, idx),
  };
});

export const mockHistory: HistoryEntry[] = [
  { id: "h1", poNumber: "PO/2025/1008", user: "A. Khanna", role: "Director", action: "Approved", status: "Approved", date: "2025-05-28", time: "11:42", remarks: "Cleared for procurement." },
  { id: "h2", poNumber: "PO/2025/1012", user: "S. Kulkarni", role: "Finance Manager", action: "Rejected", status: "Rejected", date: "2025-05-27", time: "16:08", remarks: "Budget overrun — revise quotation." },
  { id: "h3", poNumber: "PO/2025/1009", user: "P. Mehta", role: "HOD", action: "Approved", status: "Approved", date: "2025-05-27", time: "10:15", remarks: "Critical spares — approved." },
  { id: "h4", poNumber: "PO/2025/1010", user: "V. Reddy", role: "Purchase Head", action: "Approved", status: "Approved", date: "2025-05-26", time: "14:30", remarks: "Verified rates." },
  { id: "h5", poNumber: "PO/2025/1013", user: "A. Khanna", role: "Director", action: "Rejected", status: "Rejected", date: "2025-05-25", time: "09:20", remarks: "Vendor not on approved list." },
  { id: "h6", poNumber: "PO/2025/1011", user: "P. Mehta", role: "HOD", action: "Approved", status: "Approved", date: "2025-05-24", time: "17:55", remarks: "OK." },
  { id: "h7", poNumber: "PO/2025/1015", user: "V. Reddy", role: "Purchase Head", action: "Approved", status: "Approved", date: "2025-05-23", time: "12:00", remarks: "Standard procurement." },
];

export function summary() {
  return {
    pending: mockPOs.filter((p) => p.status === "Pending").length,
    approved: mockPOs.filter((p) => p.status === "Approved").length,
    rejected: mockPOs.filter((p) => p.status === "Rejected").length,
    total: mockPOs.length,
  };
}

export function formatINR(n: number) {
  return new Intl.NumberFormat("en-IN", { style: "currency", currency: "INR", maximumFractionDigits: 0 }).format(n);
}
