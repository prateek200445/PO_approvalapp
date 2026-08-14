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

/** Governed / eval-backed prompts only — keep in sync with POApprovalAPI/Chatbot/eval_*.ps1 */
export const SUGGESTED_PROMPTS = [
  {
    category: "Approvals",
    prompts: [
      "How many purchase orders are pending approval?",
      "List pending bill payments with party name and amount",
    ],
  },
  {
    category: "Stock",
    prompts: [
      "What is stock in hand for item WIP00013 at Oswal Extrusion Limited?",
      "List items below reorder level at Oswal Extrusion Limited",
    ],
  },
  {
    category: "Movement",
    prompts: [
      "For Oswal Extrusion Limited show items with outward qty today",
      "Inward and outward qty for item WIP00013 at Oswal Extrusion Limited",
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
    category: "Production & Sales",
    prompts: [
      "Recent loom rolls produced at Oswal Extrusion Limited",
      "Sales by product group for Oswal Extrusion Limited FY 2025-26",
    ],
  },
] as const;
