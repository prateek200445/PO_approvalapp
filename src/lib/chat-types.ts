export interface ChatTableUsed {
  objectName: string;
  domain: string;
  score: number;
}

export interface ChatApiResponse {
  answer: string;
  sql: string;
  tablesUsed: ChatTableUsed[];
  rows: Record<string, unknown>[];
  rowCount: number;
  /** Full matching count when the API ran a companion COUNT. */
  totalCount?: number | null;
  /** True when chat rows are a capped sample. */
  truncated?: boolean;
  warning?: string | null;
}

export type ChatMessageRole = "user" | "assistant" | "error";

export interface ChatMessage {
  id: string;
  role: ChatMessageRole;
  content: string;
  timestamp: number;
  response?: ChatApiResponse;
  pending?: boolean;
}

export const SUGGESTED_PROMPTS = [
  {
    category: "Approvals",
    prompts: [
      "How many purchase orders are pending approval?",
      "Show recent pending bill payments",
    ],
  },
  {
    category: "Stock",
    prompts: [
      "Stock in hand for item at Oswal Extrusion Limited",
      "Items below reorder level at Oswal Extrusion Limited",
    ],
  },
  {
    category: "Ledgers",
    prompts: [
      "How many ledgers does Oswal Extrusion Limited have?",
      "List ledger groups for Oswal Extrusion Limited",
    ],
  },
  {
    category: "Production",
    prompts: [
      "Recent loom rolls at Oswal Extrusion Limited",
      "FIBC bag production for Oswal Extrusion Limited",
    ],
  },
  {
    category: "MRN & Vendors",
    prompts: [
      "Recent material receipts for Oswal Extrusion Limited",
      "Vendor GST and bank details for Chemline",
    ],
  },
] as const;
