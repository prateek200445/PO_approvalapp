type RowWithCompany = {
  GroupName?: string | null;
  groupName?: string | null;
  CompanyName?: string | null;
  companyName?: string | null;
};

export const ALL_GROUPS = "All";

/** Group company from ERP FactoryInfo; the API falls back to the company name when a company has no group. */
export function rowGroupCompany(row: RowWithCompany): string {
  return String(row.GroupName ?? row.groupName ?? row.CompanyName ?? row.companyName ?? "").trim();
}

export function groupCompanyOptions(rows: RowWithCompany[]): string[] {
  const names = new Set<string>();
  for (const row of rows) {
    const name = rowGroupCompany(row);
    if (name) names.add(name);
  }
  return [ALL_GROUPS, ...Array.from(names).sort((a, b) => a.localeCompare(b))];
}

export function matchesGroupCompany(row: RowWithCompany, group: string): boolean {
  return group === ALL_GROUPS || rowGroupCompany(row) === group;
}

export function groupCompanyLabel(name: string): string {
  return name === ALL_GROUPS ? "All group companies" : name;
}
