import { useEffect, useMemo, useRef, useState, type ReactNode } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import {
  Briefcase,
  Camera,
  ChevronLeft,
  ChevronRight,
  ExternalLink,
  FileUp,
  Landmark,
  Loader2,
  Paperclip,
  Phone,
  RefreshCw,
  Trash2,
  User,
  UserPlus,
  X,
  type LucideIcon,
} from "lucide-react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { cn } from "@/lib/utils";
import {
  createHrEmployee,
  deleteHrEmployeeDocument,
  getHrEmployeeDocuments,
  getHrEmployeeDocumentTypes,
  getHrEmployeeFormOptions,
  getHrNextEmpCode,
  openHrEmployeeDocument,
  uploadHrEmployeeDocument,
  uploadHrEmployeePhoto,
  type HrNewEmployee,
} from "@/lib/hr-reports-api";

type Section = "personal" | "official" | "communication" | "bank" | "docs";

const SECTIONS: { id: Section; label: string; hint: string; icon: LucideIcon }[] = [
  { id: "personal", label: "Personal Information", hint: "Basic details, family and qualification", icon: User },
  { id: "official", label: "Official Records", hint: "Joining, designation, department and salary rules", icon: Briefcase },
  { id: "communication", label: "Communication Details", hint: "Phone, email and addresses", icon: Phone },
  { id: "bank", label: "Bank / Tax Details", hint: "Bank account, PAN, Aadhaar, PF and ESIC", icon: Landmark },
  { id: "docs", label: "Upload Docs", hint: "Employee photo and ID / certificate documents", icon: Paperclip },
];

const MAX_DOC_BYTES = 5 * 1024 * 1024;
const MAX_RAW_PHOTO_BYTES = 15 * 1024 * 1024;
const PHOTO_MAX_SIDE = 600;

type PendingPhoto = { blob: Blob; name: string; previewUrl: string };
type PendingDoc = { id: string; docType: string; file: File };

async function toErpJpeg(file: File): Promise<Blob> {
  const url = URL.createObjectURL(file);
  try {
    const img = await new Promise<HTMLImageElement>((resolve, reject) => {
      const el = new Image();
      el.onload = () => resolve(el);
      el.onerror = () => reject(new Error("Could not read that image. Use a JPG or PNG photo."));
      el.src = url;
    });
    const scale = Math.min(1, PHOTO_MAX_SIDE / Math.max(img.naturalWidth, img.naturalHeight));
    const canvas = document.createElement("canvas");
    canvas.width = Math.max(1, Math.round(img.naturalWidth * scale));
    canvas.height = Math.max(1, Math.round(img.naturalHeight * scale));
    const ctx = canvas.getContext("2d");
    if (!ctx) throw new Error("Could not process the photo.");
    ctx.fillStyle = "#fff";
    ctx.fillRect(0, 0, canvas.width, canvas.height);
    ctx.drawImage(img, 0, 0, canvas.width, canvas.height);
    return await new Promise<Blob>((resolve, reject) =>
      canvas.toBlob((b) => (b ? resolve(b) : reject(new Error("Could not process the photo."))), "image/jpeg", 0.85),
    );
  } finally {
    URL.revokeObjectURL(url);
  }
}

function formatBytes(n: number) {
  if (n < 1024) return `${n} B`;
  if (n < 1024 * 1024) return `${Math.round(n / 1024)} KB`;
  return `${(n / (1024 * 1024)).toFixed(1)} MB`;
}

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
  const sectionIndex = SECTIONS.findIndex((s) => s.id === section);
  const sectionNavRef = useRef<HTMLElement>(null);

  function goToSection(id: Section) {
    setSection(id);
    sectionNavRef.current?.scrollIntoView({ behavior: "smooth", block: "nearest" });
  }
  const [saving, setSaving] = useState(false);
  const [codeTouched, setCodeTouched] = useState(false);
  const [sameAddress, setSameAddress] = useState(false);
  const [photo, setPhoto] = useState<PendingPhoto | null>(null);
  const [docs, setDocs] = useState<PendingDoc[]>([]);
  const [savedEmpCode, setSavedEmpCode] = useState<string | null>(null);
  const [retryEmpCode, setRetryEmpCode] = useState<string | null>(null);

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

  const attachmentCount = (photo ? 1 : 0) + docs.length;

  async function uploadAttachments(empCode: string): Promise<string[]> {
    const failures: string[] = [];
    if (photo) {
      try {
        await uploadHrEmployeePhoto(empCode, photo.blob, photo.name, username);
        URL.revokeObjectURL(photo.previewUrl);
        setPhoto(null);
      } catch (err) {
        failures.push(`Photo: ${err instanceof Error ? err.message : "upload failed"}`);
      }
    }
    const remaining: PendingDoc[] = [];
    for (const doc of docs) {
      try {
        await uploadHrEmployeeDocument(empCode, doc.docType, doc.file, username);
      } catch (err) {
        remaining.push(doc);
        failures.push(`${doc.file.name}: ${err instanceof Error ? err.message : "upload failed"}`);
      }
    }
    setDocs(remaining);
    await queryClient.invalidateQueries({ queryKey: ["hr-employee-docs", empCode] });
    return failures;
  }

  function reportUploads(empCode: string, failures: string[]) {
    if (failures.length === 0) {
      setRetryEmpCode(null);
      return;
    }
    setRetryEmpCode(empCode);
    setSection("docs");
    toast.error(`${failures.length} file(s) did not upload for ${empCode}. ${failures[0]}`);
  }

  async function submit() {
    if (missing.length > 0) {
      toast.error(`Required: ${missing.join(", ")}`);
      return;
    }
    setSaving(true);
    try {
      const r = await createHrEmployee(form, username);
      toast.success(r.message);
      setSavedEmpCode(r.empCode);
      const failures = attachmentCount > 0 ? await uploadAttachments(r.empCode) : [];
      if (attachmentCount > 0 && failures.length === 0) toast.success(`Documents uploaded for ${r.empCode}.`);
      await queryClient.invalidateQueries({ queryKey: ["hr-employees"] });
      await queryClient.invalidateQueries({ queryKey: ["hr-next-emp-code"] });
      const keep = { companyName: form.companyName, branch: form.branch };
      setForm({ ...emptyEmployee(), ...keep });
      setCodeTouched(false);
      setSameAddress(false);
      setSection("personal");
      reportUploads(r.empCode, failures);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Could not add employee");
    } finally {
      setSaving(false);
    }
  }

  async function retryUploads() {
    if (!retryEmpCode) return;
    setSaving(true);
    try {
      const failures = await uploadAttachments(retryEmpCode);
      if (failures.length === 0) toast.success(`Documents uploaded for ${retryEmpCode}.`);
      reportUploads(retryEmpCode, failures);
    } finally {
      setSaving(false);
    }
  }

  function clearAttachments() {
    if (photo) URL.revokeObjectURL(photo.previewUrl);
    setPhoto(null);
    setDocs([]);
    setRetryEmpCode(null);
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

      <div className="space-y-4">
      <div>
        <nav
          ref={sectionNavRef}
          aria-label="Employee form sections"
          className="flex gap-1 overflow-x-auto border-b border-border md:flex-wrap md:overflow-visible"
        >
          {SECTIONS.map((s) => {
            const Icon = s.icon;
            const active = section === s.id;
            return (
              <button
                key={s.id}
                type="button"
                onClick={(e) => {
                  setSection(s.id);
                  e.currentTarget.scrollIntoView({ block: "nearest", inline: "nearest" });
                }}
                aria-current={active ? "step" : undefined}
                className={cn(
                  "-mb-px flex shrink-0 items-center gap-2 whitespace-nowrap border-b-2 px-3 py-2.5 text-sm transition",
                  active
                    ? "border-primary font-medium text-primary"
                    : "border-transparent text-muted-foreground hover:border-border hover:text-foreground",
                )}
              >
                <Icon className="h-4 w-4 shrink-0" />
                {s.label}
                {s.id === "docs" && attachmentCount > 0 ? (
                  <span className="rounded-full bg-primary px-1.5 text-[10px] font-semibold text-primary-foreground">
                    {attachmentCount}
                  </span>
                ) : null}
              </button>
            );
          })}
        </nav>
        <p className="mt-2 text-xs text-muted-foreground">
          Step {sectionIndex + 1} of {SECTIONS.length} — {SECTIONS[sectionIndex].hint}
        </p>
      </div>

      {section === "personal" ? (
        <div className="space-y-4">
          <div className="grid gap-x-4 gap-y-3 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
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
            <div className="flex h-9 items-center gap-5 self-end">
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
          <div className="grid gap-x-4 gap-y-3 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
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
            <div className="flex h-9 items-center self-end">
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
        <div className="grid gap-x-4 gap-y-3 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
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

      {section === "docs" ? (
        <DocsSection
          username={username}
          photo={photo}
          setPhoto={setPhoto}
          docs={docs}
          setDocs={setDocs}
          savedEmpCode={savedEmpCode}
          retryEmpCode={retryEmpCode}
          retrying={saving}
          onRetry={() => void retryUploads()}
        />
      ) : null}

      <div className="flex items-center justify-between gap-2">
        <Button
          type="button"
          variant="ghost"
          size="sm"
          disabled={sectionIndex === 0}
          onClick={() => goToSection(SECTIONS[sectionIndex - 1].id)}
        >
          <ChevronLeft className="h-4 w-4" /> Back
        </Button>
        {sectionIndex < SECTIONS.length - 1 ? (
          <Button type="button" variant="outline" size="sm" onClick={() => goToSection(SECTIONS[sectionIndex + 1].id)}>
            Next: {SECTIONS[sectionIndex + 1].label} <ChevronRight className="h-4 w-4" />
          </Button>
        ) : null}
      </div>
      </div>

      <div className="flex flex-wrap items-center justify-between gap-2 border-t border-border pt-3">
        <p className="text-xs text-muted-foreground">
          {missing.length > 0 ? `Still required: ${missing.join(", ")}.` : "Ready to save."}
          {attachmentCount > 0 ? ` ${attachmentCount} file(s) will upload after save.` : ""}
        </p>
        <div className="grid w-full grid-cols-2 gap-2 sm:flex sm:w-auto">
          <Button
            type="button"
            variant="outline"
            disabled={saving}
            onClick={() => {
              setForm(emptyEmployee());
              setCodeTouched(false);
              setSameAddress(false);
              setSection("personal");
              clearAttachments();
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

const FALLBACK_DOC_TYPES = [
  "Aadhaar Card",
  "PAN Card",
  "Bank Passbook / Cheque",
  "Educational Certificate",
  "Experience / Relieving Letter",
  "Resume",
  "Offer / Appointment Letter",
  "Address Proof",
  "Other",
];

function DocsSection({
  username,
  photo,
  setPhoto,
  docs,
  setDocs,
  savedEmpCode,
  retryEmpCode,
  retrying,
  onRetry,
}: {
  username: string;
  photo: PendingPhoto | null;
  setPhoto: (p: PendingPhoto | null) => void;
  docs: PendingDoc[];
  setDocs: (d: PendingDoc[]) => void;
  savedEmpCode: string | null;
  retryEmpCode: string | null;
  retrying: boolean;
  onRetry: () => void;
}) {
  const queryClient = useQueryClient();
  const photoInput = useRef<HTMLInputElement>(null);
  const docInput = useRef<HTMLInputElement>(null);
  const [docType, setDocType] = useState("Aadhaar Card");
  const [processingPhoto, setProcessingPhoto] = useState(false);

  const typesQuery = useQuery({
    queryKey: ["hr-employee-doc-types"],
    queryFn: getHrEmployeeDocumentTypes,
    staleTime: Infinity,
  });
  const docTypes = typesQuery.data?.length ? typesQuery.data : FALLBACK_DOC_TYPES;

  const savedDocsQuery = useQuery({
    queryKey: ["hr-employee-docs", savedEmpCode],
    queryFn: () => getHrEmployeeDocuments(savedEmpCode!, username),
    enabled: !!savedEmpCode && !!username,
  });

  async function pickPhoto(file: File | undefined) {
    if (!file) return;
    if (!file.type.startsWith("image/")) {
      toast.error("Photo must be an image (JPG or PNG).");
      return;
    }
    if (file.size > MAX_RAW_PHOTO_BYTES) {
      toast.error("Photo is too large (max 15 MB before resizing).");
      return;
    }
    setProcessingPhoto(true);
    try {
      const blob = await toErpJpeg(file);
      if (photo) URL.revokeObjectURL(photo.previewUrl);
      const base = file.name.replace(/\.[^.]+$/, "") || "photo";
      setPhoto({ blob, name: `${base}.jpg`, previewUrl: URL.createObjectURL(blob) });
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Could not process the photo.");
    } finally {
      setProcessingPhoto(false);
    }
  }

  function pickDocs(files: FileList | null) {
    if (!files?.length) return;
    const added: PendingDoc[] = [];
    for (const file of Array.from(files)) {
      const okType = /\.(pdf|jpe?g|png)$/i.test(file.name) || /^(application\/pdf|image\/(jpeg|png))$/.test(file.type);
      if (!okType) {
        toast.error(`${file.name}: only PDF, JPG or PNG files are allowed.`);
        continue;
      }
      if (file.size > MAX_DOC_BYTES) {
        toast.error(`${file.name}: file is larger than 5 MB.`);
        continue;
      }
      added.push({ id: `${Date.now()}-${Math.random().toString(36).slice(2)}`, docType, file });
    }
    if (added.length) setDocs([...docs, ...added]);
  }

  async function removeSaved(docId: number) {
    if (!savedEmpCode) return;
    try {
      await deleteHrEmployeeDocument(savedEmpCode, docId, username);
      await queryClient.invalidateQueries({ queryKey: ["hr-employee-docs", savedEmpCode] });
      toast.success("Document removed.");
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Could not remove document");
    }
  }

  async function viewSaved(docId: number) {
    if (!savedEmpCode) return;
    try {
      await openHrEmployeeDocument(savedEmpCode, docId, username);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Could not open document");
    }
  }

  return (
    <div className="space-y-4">
      {retryEmpCode ? (
        <div className="flex flex-wrap items-center justify-between gap-2 rounded-lg border border-amber-500/40 bg-amber-500/10 px-3 py-2 text-sm">
          <span>
            Employee <b>{retryEmpCode}</b> was saved, but the files below did not upload.
          </span>
          <Button type="button" size="sm" onClick={onRetry} disabled={retrying}>
            {retrying ? <Loader2 className="h-4 w-4 animate-spin" /> : <RefreshCw className="h-4 w-4" />}
            Retry upload
          </Button>
        </div>
      ) : null}

      <div className="grid gap-4 md:grid-cols-[220px_1fr]">
        <fieldset className="rounded-lg border border-border bg-muted/20 p-3">
          <legend className="sr-only">Employee photo</legend>
          <h4 className="mb-3 text-xs font-semibold uppercase tracking-wider text-muted-foreground">Employee photo</h4>
          <div className="flex flex-col items-center gap-2">
            <div className="flex h-44 w-36 items-center justify-center overflow-hidden rounded-md border border-dashed border-border bg-muted/40">
              {photo ? (
                <img src={photo.previewUrl} alt="Employee" className="h-full w-full object-cover" />
              ) : processingPhoto ? (
                <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" />
              ) : (
                <Camera className="h-8 w-8 text-muted-foreground" />
              )}
            </div>
            <input
              ref={photoInput}
              type="file"
              accept="image/jpeg,image/png"
              className="hidden"
              onChange={(e) => {
                void pickPhoto(e.target.files?.[0]);
                e.target.value = "";
              }}
            />
            <div className="flex w-full gap-2">
              <Button
                type="button"
                variant="outline"
                size="sm"
                className="flex-1"
                disabled={processingPhoto}
                onClick={() => photoInput.current?.click()}
              >
                <Camera className="h-4 w-4" /> {photo ? "Change" : "Choose photo"}
              </Button>
              {photo ? (
                <Button
                  type="button"
                  variant="outline"
                  size="icon"
                  className="h-8 w-8"
                  title="Remove photo"
                  onClick={() => {
                    URL.revokeObjectURL(photo.previewUrl);
                    setPhoto(null);
                  }}
                >
                  <X className="h-4 w-4" />
                </Button>
              ) : null}
            </div>
            <p className="text-center text-[11px] text-muted-foreground">
              Saved to ERP (shows on the ERP employee form). Resized to JPG automatically.
            </p>
          </div>
        </fieldset>

        <fieldset className="min-w-0 rounded-lg border border-border bg-muted/20 p-3">
          <legend className="sr-only">Documents</legend>
          <h4 className="mb-3 text-xs font-semibold uppercase tracking-wider text-muted-foreground">Documents</h4>
          <div className="flex flex-col gap-2 sm:flex-row sm:items-end [&>div:first-child]:sm:flex-1">
            <Field label="Document type">
              <select value={docType} onChange={(e) => setDocType(e.target.value)} className={selectClass}>
                {docTypes.map((t) => (
                  <option key={t} value={t}>
                    {t}
                  </option>
                ))}
              </select>
            </Field>
            <input
              ref={docInput}
              type="file"
              multiple
              accept="application/pdf,image/jpeg,image/png,.pdf,.jpg,.jpeg,.png"
              className="hidden"
              onChange={(e) => {
                pickDocs(e.target.files);
                e.target.value = "";
              }}
            />
            <Button type="button" variant="outline" onClick={() => docInput.current?.click()}>
              <FileUp className="h-4 w-4" /> Add file
            </Button>
          </div>
          <p className="mt-1 text-[11px] text-muted-foreground">PDF, JPG or PNG · up to 5 MB each.</p>

          {docs.length > 0 ? (
            <ul className="mt-3 divide-y divide-border rounded-md border border-border">
              {docs.map((d) => (
                <li key={d.id} className="flex items-center gap-2 px-3 py-2 text-sm">
                  <div className="min-w-0 flex-1">
                    <div className="truncate font-medium">{d.file.name}</div>
                    <div className="text-xs text-muted-foreground">
                      {d.docType} · {formatBytes(d.file.size)}
                    </div>
                  </div>
                  <select
                    value={d.docType}
                    onChange={(e) =>
                      setDocs(docs.map((x) => (x.id === d.id ? { ...x, docType: e.target.value } : x)))
                    }
                    className="hidden h-8 rounded-md border border-border bg-background px-2 text-xs sm:block"
                  >
                    {docTypes.map((t) => (
                      <option key={t} value={t}>
                        {t}
                      </option>
                    ))}
                  </select>
                  <Button
                    type="button"
                    variant="ghost"
                    size="icon"
                    className="h-8 w-8 shrink-0"
                    title="Remove"
                    onClick={() => setDocs(docs.filter((x) => x.id !== d.id))}
                  >
                    <X className="h-4 w-4" />
                  </Button>
                </li>
              ))}
            </ul>
          ) : (
            <p className="mt-3 rounded-md border border-dashed border-border px-3 py-4 text-center text-sm text-muted-foreground">
              No documents added yet.
            </p>
          )}
        </fieldset>
      </div>

      {savedEmpCode ? (
        <fieldset className="rounded-lg border border-border bg-muted/20 p-3">
          <legend className="sr-only">Uploaded for {savedEmpCode}</legend>
          <h4 className="mb-2 text-xs font-semibold uppercase tracking-wider text-muted-foreground">
            Uploaded for {savedEmpCode}
          </h4>
          {savedDocsQuery.isLoading ? (
            <Loader2 className="h-4 w-4 animate-spin text-muted-foreground" />
          ) : savedDocsQuery.data?.length ? (
            <ul className="divide-y divide-border">
              {savedDocsQuery.data.map((d) => (
                <li key={d.docId} className="flex items-center gap-2 py-2 text-sm">
                  <div className="min-w-0 flex-1">
                    <div className="truncate font-medium">{d.fileName}</div>
                    <div className="text-xs text-muted-foreground">
                      {d.docType} · {formatBytes(d.fileSize)}
                    </div>
                  </div>
                  <Button
                    type="button"
                    variant="ghost"
                    size="icon"
                    className="h-8 w-8"
                    title="Open"
                    onClick={() => void viewSaved(d.docId)}
                  >
                    <ExternalLink className="h-4 w-4" />
                  </Button>
                  <Button
                    type="button"
                    variant="ghost"
                    size="icon"
                    className="h-8 w-8 text-destructive"
                    title="Remove"
                    onClick={() => void removeSaved(d.docId)}
                  >
                    <Trash2 className="h-4 w-4" />
                  </Button>
                </li>
              ))}
            </ul>
          ) : (
            <p className="text-sm text-muted-foreground">No documents on file.</p>
          )}
        </fieldset>
      ) : null}
    </div>
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
    <section className="space-y-3 border-t border-border pt-4">
      <h4 className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">{title}</h4>
      <div className="grid gap-x-4 gap-y-3 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">{children}</div>
    </section>
  );
}
