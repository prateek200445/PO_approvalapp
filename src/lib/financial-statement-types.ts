export type TrialBalanceColumnMapping = {
  sheetName: string;
  headerRow: number;
  particulars: string;
  opening: string;
  debit: string;
  credit: string;
  closing: string;
  adjustedClosing?: string | null;
  group?: string | null;
};

export type TrialBalancePreview = {
  fileName: string;
  sheetNames: string[];
  selectedSheet: string;
  headerRow: number;
  headers: string[];
  suggestedMapping: TrialBalanceColumnMapping;
  dataRowCount: number;
  sampleRows: Record<string, string | null>[];
};

export type ScheduleLine = {
  group: string;
  label: string;
  amountLakhs: number;
  rawClosing: number;
  ledgerCount: number;
};

export type ScheduleNote = {
  note: string;
  title: string;
  lines: ScheduleLine[];
  totalLakhs: number;
};

export type ReportLine = {
  label: string;
  note: string;
  lineType: string;
  amountLakhs: number;
  isHeader: boolean;
  isSubtotal: boolean;
};

export type ReportSection = {
  title: string;
  lines: ReportLine[];
  sectionTotalLakhs: number;
};

export type UnmappedLedger = {
  ledger: string;
  closing: number;
  closingLakhs: number;
};

export type FinancialStatementResult = {
  companyKey: string;
  companyName: string;
  periodLabel: string;
  totalLedgers: number;
  mappedLedgers: number;
  unmappedLedgers: number;
  schedules: ScheduleNote[];
  balanceSheet: ReportSection[];
  profitAndLoss: ReportLine[];
  unmapped: UnmappedLedger[];
  balanceSheetTotalLakhs: number;
  totalAssetsLakhs: number;
  totalLiabilitiesAndEquityLakhs: number;
};

export type CompanyMappingSummary = {
  companyKey: string;
  companyName: string;
  mappingCount: number;
  usesDefaultMapping: boolean;
};

export function normalizeFinancialStatementResult(data: any): FinancialStatementResult {
  return {
    companyKey: data.companyKey ?? data.CompanyKey ?? "",
    companyName: data.companyName ?? data.CompanyName ?? "",
    periodLabel: data.periodLabel ?? data.PeriodLabel ?? "",
    totalLedgers: num(data.totalLedgers ?? data.TotalLedgers),
    mappedLedgers: num(data.mappedLedgers ?? data.MappedLedgers),
    unmappedLedgers: num(data.unmappedLedgers ?? data.UnmappedLedgers),
    schedules: (data.schedules ?? data.Schedules ?? []).map(normalizeSchedule),
    balanceSheet: (data.balanceSheet ?? data.BalanceSheet ?? []).map(normalizeSection),
    profitAndLoss: (data.profitAndLoss ?? data.ProfitAndLoss ?? []).map(normalizeReportLine),
    unmapped: (data.unmapped ?? data.Unmapped ?? []).map((u: any) => ({
      ledger: u.ledger ?? u.Ledger ?? "",
      closing: num(u.closing ?? u.Closing),
      closingLakhs: num(u.closingLakhs ?? u.ClosingLakhs),
    })),
    balanceSheetTotalLakhs: num(data.balanceSheetTotalLakhs ?? data.BalanceSheetTotalLakhs),
    totalAssetsLakhs: num(data.totalAssetsLakhs ?? data.TotalAssetsLakhs),
    totalLiabilitiesAndEquityLakhs: num(data.totalLiabilitiesAndEquityLakhs ?? data.TotalLiabilitiesAndEquityLakhs),
  };
}

function normalizeSchedule(s: any): ScheduleNote {
  return {
    note: s.note ?? s.Note ?? "",
    title: s.title ?? s.Title ?? "",
    totalLakhs: num(s.totalLakhs ?? s.TotalLakhs),
    lines: (s.lines ?? s.Lines ?? []).map((l: any) => ({
      group: l.group ?? l.Group ?? "",
      label: l.label ?? l.Label ?? "",
      amountLakhs: num(l.amountLakhs ?? l.AmountLakhs),
      rawClosing: num(l.rawClosing ?? l.RawClosing),
      ledgerCount: num(l.ledgerCount ?? l.LedgerCount),
    })),
  };
}

function normalizeSection(s: any): ReportSection {
  return {
    title: s.title ?? s.Title ?? "",
    sectionTotalLakhs: num(s.sectionTotalLakhs ?? s.SectionTotalLakhs),
    lines: (s.lines ?? s.Lines ?? []).map(normalizeReportLine),
  };
}

function normalizeReportLine(l: any): ReportLine {
  return {
    label: l.label ?? l.Label ?? "",
    note: l.note ?? l.Note ?? "",
    lineType: l.lineType ?? l.LineType ?? "line",
    amountLakhs: num(l.amountLakhs ?? l.AmountLakhs),
    isHeader: !!(l.isHeader ?? l.IsHeader),
    isSubtotal: !!(l.isSubtotal ?? l.IsSubtotal),
  };
}

function num(v: unknown): number {
  const n = Number(v);
  return Number.isFinite(n) ? n : 0;
}

export function formatLakhs(value: number): string {
  return value.toLocaleString("en-IN", {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  });
}

export function slugifyCompanyKey(name: string): string {
  return name
    .trim()
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-+|-+$/g, "") || "default";
}

export function normalizeTbMapping(data: any): TrialBalanceColumnMapping {
  const src = data?.suggestedMapping ?? data?.SuggestedMapping ?? data ?? {};
  return {
    sheetName: src.sheetName ?? src.SheetName ?? "",
    headerRow: num(src.headerRow ?? src.HeaderRow) || 1,
    particulars: src.particulars ?? src.Particulars ?? "Particulars",
    opening: src.opening ?? src.Opening ?? "Opening",
    debit: src.debit ?? src.Debit ?? "Debit",
    credit: src.credit ?? src.Credit ?? "Credit",
    closing: src.closing ?? src.Closing ?? "Closing",
    adjustedClosing: src.adjustedClosing ?? src.AdjustedClosing ?? null,
    group: src.group ?? src.Group ?? "Group",
  };
}

export function normalizeTbPreview(data: any): TrialBalancePreview {
  return {
    fileName: data.fileName ?? data.FileName ?? "",
    sheetNames: data.sheetNames ?? data.SheetNames ?? [],
    selectedSheet: data.selectedSheet ?? data.SelectedSheet ?? "",
    headerRow: num(data.headerRow ?? data.HeaderRow) || 1,
    headers: data.headers ?? data.Headers ?? [],
    suggestedMapping: normalizeTbMapping(data),
    dataRowCount: num(data.dataRowCount ?? data.DataRowCount),
    sampleRows: data.sampleRows ?? data.SampleRows ?? [],
  };
}
