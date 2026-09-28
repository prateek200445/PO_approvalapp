import { useEffect, useMemo, useState, type ReactNode } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { Loader2, RefreshCw, UserPlus } from "lucide-react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { cn } from "@/lib/utils";
import {
  createHrEmployee,
  getHrEmployeeFormOptions,
  getHrNextEmpCode,
  type HrNewEmployee,
} from "@/lib/hr-reports-api";

type Section = "personal" | "official" | "communication" | "bank";

const SECTIONS: { id: Section; label: string }[] = [
  { id: "personal", label: "Personal Information" },
  { id: "official", label: "Official Records" },
  { id: "communication", label: "Communication Details" },
  { id: "bank", label: "Bank / Tax Details" },
];

const BLOOD_GROUPS = ["A+", "A-", "B+", "B-", "AB+", "AB-", "O+", "O-"];

function todayIso() {
  const d = new Date();
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-${String(d.getDate()).padStart(2, "0")}`;
}

function emptyEmployee(): HrNewEmployee {
  return {
    companyName: "",
    branch: "",
    empCode: "",
    name: "",
    gender: "Male",
    maritalStatus: "Single",
    nationality: "INDIAN",
    handicapped: false,
    isDirector: false,
    dateOfJoining: todayIso(),
    isHoEmp: false,
    isSalaryPerDay: false,
    pfApplicable: true,
    bonusApplicable: false,
    hraApplicable: false,
    attendanceBonus: false,
    overtime: false,
    rotation: false,
    employeeRecognition: "A",
    paymentMode: "Bank",
  };
}

type Props = { username: string };

export function HrAddEmployeeForm({ username }: Props) {
  const queryClient = useQueryClient();
  const [form, setForm] = useState<HrNewEmployee>(emptyEmployee);
  const [section, setSection] = useState<Section>("personal");
  const [saving, setSaving] = useState(false);
  const [codeTouched, setCodeTouched] = useState(false);
  const [sameAddress, setSameAddress] = useState(false);

  const optionsQuery = useQuery({
    queryKey: ["hr-employee-form-options", username],
    queryFn: () => getHrEmployeeFormOptions(username),
    enabled: !!username,
    staleTime: 10 * 60 * 1000,
  });

  const nextCodeQuery = useQuery({
    queryKey: ["hr-next-emp-code", form.branch, username],
    queryFn: () => getHrNextEmpCode(form.branch, username),
    enabled: !!username && !!form.branch,
  });

  const companies = useMemo(() => {
    const set = new Set<string>();
    for (const p of optionsQuery.data?.companyBranches ?? []) set.add(p.companyName);
    return [...set].sort((a, b) => a.localeCompare(b));
  }, [optionsQuery.data]);

  const branches = useMemo(
    () =>
      (optionsQuery.data?.companyBranches ?? [])
        .filter((p) => p.companyName === form.companyName)
        .map((p) => p.branch),
    [optionsQuery.data, form.companyName],
  );

  const isConsultant = form.companyName.trim().toLowerCase() === "consultant";

  useEffect(() => {
    if (form.companyName && branches.length > 0 && !branches.includes(form.branch)) {
      setForm((f) => ({ ...f, branch: branches[0] }));
    }
  }, [form.companyName, form.branch, branches]);

  useEffect(() => {
    const suggested = nextCodeQuery.data?.suggestedEmpCode;
    if (suggested && !codeTouched) setForm((f) => ({ ...f, empCode: suggested }));
  }, [nextCodeQuery.data, codeTouched]);

  useEffect(() => {
    if (sameAddress) setForm((f) => ({ ...f, permanentAddress: f.presentAddress }));
  }, [sameAddress, form.presentAddress]);

  function set<K extends keyof HrNewEmployee>(key: K, value: HrNewEmployee[K]) {
    setForm((f) => ({ ...f, [key]: value }));
  }

  const missing = [
    !form.companyName && "Company Name",
    !form.branch && "Branch",
    !form.empCode.trim() && "Employee Code",
    !form.name.trim() && "Employee Name",
    !form.fatherName?.trim() && "Father's Name",
    !form.dateOfJoining && "Date of Joining",
    !form.gender && "Gender",
  ].filter(Boolean) as string[];

  async function submit() {
    if (missing.length > 0) {
      toast.error(`Required: ${missing.join(", ")}`);
      return;
    }
    setSaving(true);
    try {
      const r = await createHrEmployee(form, username);
      toast.success(r.message);
      await queryClient.invalidateQueries({ queryKey: ["hr-employees"] });
      await queryClient.invalidateQueries({ queryKey: ["hr-next-emp-code"] });
      const keep = { companyName: form.companyName, branch: form.branch };
      setForm({ ...emptyEmployee(), ...keep });
      setCodeTouched(false);
      setSameAddress(false);
      setSection("personal");
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Could not add employee");
    } finally {
      setSaving(false);
    }
  }

  const opts = optionsQuery.data;

  return (
    <section className="space-y-4 rounded-xl border border-border bg-card p-4 shadow-sm">
      <div className="flex flex-wrap items-start justify-between gap-2">
        <div>
          <h2 className="flex items-center gap-2 text-lg font-semibold">
            <UserPlus className="h-5 w-5" /> Employee information — new employee
          </h2>
          <p className="text-xs text-muted-foreground">
            Saves straight into ERP payroll <code className="text-[11px]">empinfo</code> (same as the
            ERP Employee Information form). Fields marked * are required.
          </p>
        </div>
        {optionsQuery.isFetching ? <Loader2 className="h-4 w-4 animate-spin text-muted-foreground" /> : null}
      </div>

      {optionsQuery.isError ? (
        <p className="rounded-lg border border-destructive/40 bg-destructive/10 px-3 py-2 text-sm">
          {optionsQuery.error instanceof Error ? optionsQuery.error.message : "Could not load form lists."}
        </p>
      ) : null}

      <datalist id="hr-designations">{opts?.designations.map((v) => <option key={v} value={v} />)}</datalist>
      <datalist id="hr-departments">{opts?.departments.map((v) => <option key={v} value={v} />)}</datalist>
      <datalist id="hr-subdepartments">{opts?.subDepartments.map((v) => <option key={v} value={v} />)}</datalist>
      <datalist id="hr-workroles">{opts?.workRoles.map((v) => <option key={v} value={v} />)}</datalist>
      <datalist id="hr-categories">{opts?.categories.map((v) => <option key={v} value={v} />)}</datalist>
      <datalist id="hr-jobdesc">{opts?.jobDescriptions.map((v) => <option key={v} value={v} />)}</datalist>
      <datalist id="hr-blood">{BLOOD_GROUPS.map((v) => <option key={v} value={v} />)}</datalist>

      <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
        <Field label="Company Name" required>
          <select
            value={form.companyName}
            onChange={(e) => set("companyName", e.target.value)}
            className={selectClass}
          >
            <option value="">Select company…</option>
            {companies.map((c) => (
              <option key={c} value={c}>
                {c}
              </option>
            ))}
          </select>
        </Field>
        <Field label="Branch" required>
          <select
            value={form.branch}
            onChange={(e) => {
              set("branch", e.target.value);
              setCodeTouched(false);
            }}
            disabled={!form.companyName}
            className={selectClass}
          >
            <option value="">Select branch…</option>
            {branches.map((b) => (
              <option key={b} value={b}>
                {b}
              </option>
            ))}
          </select>
        </Field>
        <Field
          label="Employee Code"
          required
          hint={
            nextCodeQuery.data?.lastEmpCode
              ? `Last: ${nextCodeQuery.data.lastEmpCode}`
              : form.branch && nextCodeQuery.isSuccess && !nextCodeQuery.data?.suggestedEmpCode
                ? "No pattern found — type the code"
                : undefined
          }
        >
          <div className="flex gap-1">
            <Input
              value={form.empCode}
              onChange={(e) => {
                setCodeTouched(true);
                set("empCode", e.target.value.toUpperCase().replace(/\s+/g, ""));
              }}
              placeholder="e.g. KPW18565"
            />
            <Button
              type="button"
              variant="outline"
              size="icon"
              title="Use next code"
              disabled={!form.branch || nextCodeQuery.isFetching}
              onClick={() => {
                setCodeTouched(false);
                void nextCodeQuery.refetch();
              }}
            >
              <RefreshCw className={cn("h-4 w-4", nextCodeQuery.isFetching && "animate-spin")} />
            </Button>
          </div>
          {nextCodeQuery.data?.alternatives.length ? (
            <div className="mt-1 flex flex-wrap items-center gap-1 text-[11px] text-muted-foreground">
              Other series:
              {nextCodeQuery.data.alternatives.map((a) => (
                <button
                  key={a.nextEmpCode}
                  type="button"
                  title={`Last in this series: ${a.lastEmpCode}`}
                  onClick={() => {
                    setCodeTouched(true);
                    set("empCode", a.nextEmpCode);
                  }}
                  className={cn(
                    "rounded border border-border px-1.5 py-0.5 hover:bg-secondary",
                    form.empCode === a.nextEmpCode && "border-primary text-foreground",
                  )}
                >
                  {a.nextEmpCode}
                </button>
              ))}
            </div>
          ) : null}
        </Field>
        <Field label="Employee Name" required>
          <Input value={form.name} onChange={(e) => set("name", e.target.value)} maxLength={50} />
        </Field>
      </div>

      {isConsultant ? (
        <p className="rounded-lg border border-sky-500/40 bg-sky-500/10 px-3 py-2 text-sm">
          Consultant — this employee will get PL only (no casual leave).
        </p>
      ) : null}

      <div className="flex flex-wrap gap-1 border-b border-border">
        {SECTIONS.map((s) => (
          <button
            key={s.id}
            type="button"
            onClick={() => setSection(s.id)}
            className={cn(
              "-mb-px rounded-t-lg border border-transparent px-3 py-1.5 text-sm transition",
              section === s.id
                ? "border-border border-b-card bg-card font-medium text-foreground"
                : "text-muted-foreground hover:text-foreground",
            )}
          >
            {s.label}
          </button>
        ))}
      </div>

      {section === "personal" ? (
        <div className="space-y-4">
          <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
            <Field label="Date of Birth">
              <Input type="date" value={form.dateOfBirth ?? ""} onChange={(e) => set("dateOfBirth", e.target.value)} />
            </Field>
            <Field label="Gender" required>
              <select value={form.gender} onChange={(e) => set("gender", e.target.value)} className={selectClass}>
                <option value="Male">Male</option>
                <option value="Female">Female</option>
              </select>
            </Field>
            <Field label="Marital Status">
              <select
                value={form.maritalStatus}
                onChange={(e) => set("maritalStatus", e.target.value)}
                className={selectClass}
              >
                <option value="Single">Single</option>
                <option value="Married">Married</option>
                <option value="Divorced">Divorced</option>
                <option value="Widowed">Widowed</option>
              </select>
            </Field>
            <Text label="Language" value={form.language} onChange={(v) => set("language", v)} />
            <Text label="Religion" value={form.religion} onChange={(v) => set("religion", v)} />
            <Text label="Caste" value={form.caste} onChange={(v) => set("caste", v)} />
            <Text label="Nationality" value={form.nationality} onChange={(v) => set("nationality", v)} />
            <Text label="Blood Group" value={form.bloodGroup} onChange={(v) => set("bloodGroup", v)} list="hr-blood" />
            <Text label="Passport No" value={form.passportNo} onChange={(v) => set("passportNo", v)} />
            <Field label="Passport Valid From">
              <Input
                type="date"
                value={form.passportValidFrom ?? ""}
                onChange={(e) => set("passportValidFrom", e.target.value)}
              />
            </Field>
            <Field label="Passport Valid To">
              <Input
                type="date"
                value={form.passportValidTo ?? ""}
                onChange={(e) => set("passportValidTo", e.target.value)}
              />
            </Field>
            <Text label="Card Id" value={form.cardId} onChange={(v) => set("cardId", v)} />
            <Text
              label="Job Description"
              value={form.jobDescription}
              onChange={(v) => set("jobDescription", v)}
              list="hr-jobdesc"
              maxLength={150}
            />
            <div className="flex items-end gap-4 pb-1">
              <Check label="Handicapped" checked={!!form.handicapped} onChange={(v) => set("handicapped", v)} />
              <Check label="Is Director" checked={!!form.isDirector} onChange={(v) => set("isDirector", v)} />
            </div>
          </div>

          <Group title="Family">
            <Text label="Father's Name" required value={form.fatherName} onChange={(v) => set("fatherName", v)} />
            <Text label="Mother's Name" value={form.motherName} onChange={(v) => set("motherName", v)} />
            <Text label="Spouse Name" value={form.spouseName} onChange={(v) => set("spouseName", v)} />
            <Text
              label="No. of Children"
              value={form.noOfChildren}
              onChange={(v) => set("noOfChildren", v.replace(/\D/g, ""))}
              inputMode="numeric"
            />
          </Group>

          <Group title="Qualification">
            <Text
              label="Educational Qualification"
              value={form.qualification}
              onChange={(v) => set("qualification", v)}
            />
            <Text label="Specialization" value={form.specialization} onChange={(v) => set("specialization", v)} />
            <Text label="University" value={form.university} onChange={(v) => set("university", v)} />
            <Text
              label="Year of Passing"
              value={form.yearOfPassing}
              onChange={(v) => set("yearOfPassing", v.replace(/\D/g, "").slice(0, 4))}
              inputMode="numeric"
            />
            <Text label="Skills" value={form.skills} onChange={(v) => set("skills", v)} />
            <Text label="Abilities" value={form.abilities} onChange={(v) => set("abilities", v)} />
          </Group>
        </div>
      ) : null}

      {section === "official" ? (
        <div className="space-y-4">
          <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
            <Field label="Date of Joining" required>
              <Input
                type="date"
                value={form.dateOfJoining ?? ""}
                onChange={(e) => set("dateOfJoining", e.target.value)}
              />
            </Field>
            <Field label="Confirmation Date">
              <Input
                type="date"
                value={form.confirmationDate ?? ""}
                onChange={(e) => set("confirmationDate", e.target.value)}
              />
            </Field>
            <Text label="Designation" value={form.designation} onChange={(v) => set("designation", v)} list="hr-designations" />
            <Text label="Department" value={form.department} onChange={(v) => set("department", v)} list="hr-departments" />
            <Text
              label="Sub Department"
              value={form.subDepartment}
              onChange={(v) => set("subDepartment", v)}
              list="hr-subdepartments"
            />
            <Text label="Category" value={form.category} onChange={(v) => set("category", v)} list="hr-categories" maxLength={100} />
            <Text label="Sub Category" value={form.subCategory} onChange={(v) => set("subCategory", v)} maxLength={100} />
            <Text label="Work Role" value={form.workRole} onChange={(v) => set("workRole", v)} list="hr-workroles" />
            <Text label="Work Area" value={form.workArea} onChange={(v) => set("workArea", v)} />
            <Text label="Work Location" value={form.workLocation} onChange={(v) => set("workLocation", v)} />
            <Text label="Experience" value={form.experience} onChange={(v) => set("experience", v)} />
            <Text label="Previous Employer" value={form.previousEmployer} onChange={(v) => set("previousEmployer", v)} />
            <Text
              label="Contractor Name"
              value={form.contractorName}
              onChange={(v) => set("contractorName", v)}
              maxLength={200}
            />
            <Text
              label="Employee Recognition"
              value={form.employeeRecognition}
              onChange={(v) => set("employeeRecognition", v)}
            />
          </div>

          <Group title="Salary & applicability">
            <Text label="CTC" value={form.ctc} onChange={(v) => set("ctc", v.replace(/[^\d.]/g, ""))} inputMode="decimal" />
            <div className="flex items-end pb-1">
              <Check
                label="Salary per day"
                checked={!!form.isSalaryPerDay}
                onChange={(v) => set("isSalaryPerDay", v)}
              />
            </div>
            <Text
              label="Salary per day amount"
              value={form.salaryPerDay}
              onChange={(v) => set("salaryPerDay", v.replace(/[^\d.]/g, ""))}
              inputMode="decimal"
              disabled={!form.isSalaryPerDay}
            />
            <Field label="PF Applicable Date" hint="Defaults to date of joining">
              <Input
                type="date"
                value={form.pfApplicableDate ?? ""}
                onChange={(e) => set("pfApplicableDate", e.target.value)}
                disabled={!form.pfApplicable}
              />
            </Field>
            <div className="col-span-full flex flex-wrap gap-x-6 gap-y-2">
              <Check label="HO employee" checked={!!form.isHoEmp} onChange={(v) => set("isHoEmp", v)} />
              <Check label="PF applicable" checked={!!form.pfApplicable} onChange={(v) => set("pfApplicable", v)} />
              <Check
                label="Bonus applicable"
                checked={!!form.bonusApplicable}
                onChange={(v) => set("bonusApplicable", v)}
              />
              <Check label="HRA applicable" checked={!!form.hraApplicable} onChange={(v) => set("hraApplicable", v)} />
              <Check
                label="Attendance bonus"
                checked={!!form.attendanceBonus}
                onChange={(v) => set("attendanceBonus", v)}
              />
              <Check label="Overtime" checked={!!form.overtime} onChange={(v) => set("overtime", v)} />
              <Check label="Rotation" checked={!!form.rotation} onChange={(v) => set("rotation", v)} />
            </div>
          </Group>
        </div>
      ) : null}

      {section === "communication" ? (
        <div className="grid gap-3 sm:grid-cols-2">
          <Text label="Contact No" value={form.contactNo} onChange={(v) => set("contactNo", v)} inputMode="tel" />
          <Text label="Email" value={form.email} onChange={(v) => set("email", v)} type="email" />
          <Area
            label="Present Address"
            value={form.presentAddress}
            onChange={(v) => set("presentAddress", v)}
          />
          <div className="space-y-1">
            <Area
              label="Permanent Address"
              value={form.permanentAddress}
              onChange={(v) => set("permanentAddress", v)}
              disabled={sameAddress}
            />
            <Check label="Same as present address" checked={sameAddress} onChange={setSameAddress} />
          </div>
          <Text
            label="Emergency Contact No"
            value={form.emergencyContactNo}
            onChange={(v) => set("emergencyContactNo", v)}
            inputMode="tel"
          />
          <Text
            label="Emergency Contact / Address"
            value={form.emergencyContactAddress}
            onChange={(v) => set("emergencyContactAddress", v)}
          />
          <Text label="Guardian" value={form.guardian} onChange={(v) => set("guardian", v)} />
        </div>
      ) : null}

      {section === "bank" ? (
        <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
          <Field label="Payment Mode">
            <select
              value={form.paymentMode}
              onChange={(e) => set("paymentMode", e.target.value)}
              className={selectClass}
            >
              <option value="Bank">Bank</option>
              <option value="Cash">Cash</option>
            </select>
          </Field>
          <Text label="Bank Name" value={form.bankName} onChange={(v) => set("bankName", v)} />
          <Text label="Bank Account No" value={form.bankAccountNo} onChange={(v) => set("bankAccountNo", v)} />
          <Text
            label="IFSC Code"
            value={form.ifscCode}
            onChange={(v) => set("ifscCode", v.toUpperCase())}
          />
          <Text
            label="PAN No"
            value={form.panNo}
            onChange={(v) => set("panNo", v.toUpperCase())}
            maxLength={10}
          />
          <Text
            label="Aadhaar No"
            value={form.aadhaarNo}
            onChange={(v) => set("aadhaarNo", v.replace(/\D/g, ""))}
            maxLength={12}
            inputMode="numeric"
          />
          <Text
            label="UAN No"
            value={form.uanNo}
            onChange={(v) => set("uanNo", v.replace(/\D/g, ""))}
            maxLength={12}
            inputMode="numeric"
          />
          <Text label="PF Account No" value={form.pfAccountNo} onChange={(v) => set("pfAccountNo", v)} />
          <Text label="ESIC No" value={form.esicNo} onChange={(v) => set("esicNo", v)} maxLength={20} />
          <Text label="Nominee" value={form.nominee} onChange={(v) => set("nominee", v)} />
          <Text
            label="Nominee Relationship"
            value={form.nomineeRelationship}
            onChange={(v) => set("nomineeRelationship", v)}
          />
          <Field label="Nominee DOB">
            <Input type="date" value={form.nomineeDob ?? ""} onChange={(e) => set("nomineeDob", e.target.value)} />
          </Field>
        </div>
      ) : null}

      <div className="flex flex-wrap items-center justify-between gap-2 border-t border-border pt-3">
        <p className="text-xs text-muted-foreground">
          {missing.length > 0 ? `Still required: ${missing.join(", ")}.` : "Ready to save."} Photo / document
          upload stays in ERP for now.
        </p>
        <div className="flex gap-2">
          <Button
            type="button"
            variant="outline"
            disabled={saving}
            onClick={() => {
              setForm(emptyEmployee());
              setCodeTouched(false);
              setSameAddress(false);
              setSection("personal");
            }}
          >
            Clear
          </Button>
          <Button type="button" disabled={saving || missing.length > 0} onClick={() => void submit()}>
            {saving ? <Loader2 className="h-4 w-4 animate-spin" /> : <UserPlus className="h-4 w-4" />}
            Save employee
          </Button>
        </div>
      </div>
    </section>
  );
}

const selectClass =
  "mt-1 h-9 w-full rounded-md border border-border bg-background px-3 text-sm disabled:opacity-60";

function Field({
  label,
  required,
  hint,
  children,
}: {
  label: string;
  required?: boolean;
  hint?: string;
  children: ReactNode;
}) {
  return (
    <div className="min-w-0">
      <Label className="text-xs">
        {label}
        {required ? <span className="text-destructive"> *</span> : null}
      </Label>
      <div className="mt-1 [&>select]:mt-0">{children}</div>
      {hint ? <p className="mt-0.5 text-[11px] text-muted-foreground">{hint}</p> : null}
    </div>
  );
}

function Text({
  label,
  value,
  onChange,
  required,
  list,
  maxLength = 50,
  type = "text",
  inputMode,
  disabled,
}: {
  label: string;
  value?: string;
  onChange: (v: string) => void;
  required?: boolean;
  list?: string;
  maxLength?: number;
  type?: string;
  inputMode?: "text" | "numeric" | "decimal" | "tel" | "email";
  disabled?: boolean;
}) {
  return (
    <Field label={label} required={required}>
      <Input
        type={type}
        value={value ?? ""}
        onChange={(e) => onChange(e.target.value)}
        list={list}
        maxLength={maxLength}
        inputMode={inputMode}
        disabled={disabled}
      />
    </Field>
  );
}

function Area({
  label,
  value,
  onChange,
  disabled,
}: {
  label: string;
  value?: string;
  onChange: (v: string) => void;
  disabled?: boolean;
}) {
  return (
    <Field label={label}>
      <textarea
        value={value ?? ""}
        onChange={(e) => onChange(e.target.value)}
        maxLength={300}
        rows={3}
        disabled={disabled}
        className="w-full rounded-md border border-border bg-background px-3 py-2 text-sm disabled:opacity-60"
      />
    </Field>
  );
}

function Check({ label, checked, onChange }: { label: string; checked: boolean; onChange: (v: boolean) => void }) {
  return (
    <label className="flex cursor-pointer items-center gap-2 text-sm">
      <input type="checkbox" checked={checked} onChange={(e) => onChange(e.target.checked)} className="h-4 w-4" />
      {label}
    </label>
  );
}

function Group({ title, children }: { title: string; children: ReactNode }) {
  return (
    <fieldset className="rounded-lg border border-border px-3 pb-3 pt-1">
      <legend className="px-1 text-sm font-medium">{title}</legend>
      <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">{children}</div>
    </fieldset>
  );
}
