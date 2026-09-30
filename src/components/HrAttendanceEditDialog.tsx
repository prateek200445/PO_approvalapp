import { useEffect, useState } from "react";
import { Loader2, RotateCcw } from "lucide-react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import {
  HR_ATTENDANCE_EDIT_STATUSES,
  revertHrAttendanceEdit,
  saveHrAttendanceEdit,
  type HrAttendanceDay,
} from "@/lib/hr-reports-api";

const QUICK_REASONS = ["Forgot to punch", "Machine not working", "On official duty", "Client visit", "Company holiday"];

function toHhMm(value?: string | null) {
  return value ? value.slice(0, 5) : "";
}

export function HrAttendanceEditDialog({
  day,
  empCode,
  empName,
  username,
  onClose,
  onChanged,
}: {
  day: HrAttendanceDay | null;
  empCode: string;
  empName?: string | null;
  username: string;
  onClose: () => void;
  onChanged: () => void;
}) {
  const [status, setStatus] = useState("Present");
  const [punchIn, setPunchIn] = useState("");
  const [punchOut, setPunchOut] = useState("");
  const [reason, setReason] = useState("");
  const [busy, setBusy] = useState<"save" | "revert" | null>(null);

  useEffect(() => {
    if (!day) return;
    const current = (HR_ATTENDANCE_EDIT_STATUSES as readonly string[]).includes(day.status) ? day.status : "Present";
    setStatus(current);
    setPunchIn(toHhMm(day.punchIn));
    setPunchOut(toHhMm(day.punchOut));
    setReason(day.isEdited ? (day.editReason ?? "") : "");
  }, [day]);

  if (!day) return null;

  const machineStatus = day.machineStatus || day.status;
  const reasonOk = reason.trim().length >= 3;
  const timesOk = !punchIn || !punchOut || punchOut > punchIn;

  async function onSave() {
    if (!day) return;
    setBusy("save");
    try {
      await saveHrAttendanceEdit(
        { empCode, date: day.date, status, punchIn: punchIn || null, punchOut: punchOut || null, reason: reason.trim() },
        username,
      );
      toast.success(`Attendance for ${day.date} updated to ${status}`);
      onChanged();
      onClose();
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Could not save the change");
    } finally {
      setBusy(null);
    }
  }

  async function onRevert() {
    if (!day) return;
    setBusy("revert");
    try {
      await revertHrAttendanceEdit(empCode, day.date, username);
      toast.success(`${day.date} reverted to machine attendance (${machineStatus})`);
      onChanged();
      onClose();
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Could not revert");
    } finally {
      setBusy(null);
    }
  }

  return (
    <Dialog open onOpenChange={(open) => !open && !busy && onClose()}>
      <DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-md">
        <DialogHeader>
          <DialogTitle>Edit attendance</DialogTitle>
          <DialogDescription>
            {empName ? `${empName} (${empCode})` : empCode} · {day.date} {day.dayName}
          </DialogDescription>
        </DialogHeader>

        <div className="rounded-lg border border-border bg-secondary/40 px-3 py-2 text-xs text-muted-foreground">
          Machine record: <span className="font-medium text-foreground">{machineStatus}</span>
          {" · "}In {toHhMm(day.machinePunchIn ?? day.punchIn) || "—"}
          {" · "}Out {toHhMm(day.machinePunchOut ?? day.punchOut) || "—"}
          {day.isEdited ? (
            <div className="mt-1">
              Currently edited to <span className="font-medium text-foreground">{day.status}</span> by{" "}
              {day.editedBy ?? "HR"}
              {day.editedAt ? ` on ${day.editedAt}` : ""}
            </div>
          ) : null}
        </div>

        <div className="space-y-4">
          <div className="space-y-1.5">
            <Label>Status</Label>
            <div className="flex flex-wrap gap-1.5">
              {HR_ATTENDANCE_EDIT_STATUSES.map((s) => (
                <button
                  key={s}
                  type="button"
                  onClick={() => setStatus(s)}
                  className={
                    s === status
                      ? "rounded-md border border-primary bg-primary px-3 py-1.5 text-sm font-medium text-primary-foreground"
                      : "rounded-md border border-border bg-background px-3 py-1.5 text-sm hover:bg-secondary"
                  }
                >
                  {s}
                </button>
              ))}
            </div>
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-1.5">
              <Label htmlFor="att-edit-in">Punch in (optional)</Label>
              <Input id="att-edit-in" type="time" value={punchIn} onChange={(e) => setPunchIn(e.target.value)} />
            </div>
            <div className="space-y-1.5">
              <Label htmlFor="att-edit-out">Punch out (optional)</Label>
              <Input id="att-edit-out" type="time" value={punchOut} onChange={(e) => setPunchOut(e.target.value)} />
            </div>
          </div>
          {!timesOk ? <p className="-mt-2 text-xs text-destructive">Punch out must be after punch in.</p> : null}

          <div className="space-y-1.5">
            <Label htmlFor="att-edit-reason">Reason (required)</Label>
            <div className="flex flex-wrap gap-1.5">
              {QUICK_REASONS.map((r) => (
                <button
                  key={r}
                  type="button"
                  onClick={() => setReason(r)}
                  className="rounded-full border border-border px-2.5 py-0.5 text-xs text-muted-foreground hover:bg-secondary hover:text-foreground"
                >
                  {r}
                </button>
              ))}
            </div>
            <Textarea
              id="att-edit-reason"
              rows={2}
              maxLength={250}
              value={reason}
              placeholder="Why is this day being corrected?"
              onChange={(e) => setReason(e.target.value)}
            />
          </div>

          <p className="text-xs text-muted-foreground">
            Machine punches are not changed. The correction is applied on top in attendance, salary and Excel, and
            every change is logged.
          </p>
        </div>

        <DialogFooter className="gap-2 sm:justify-between sm:space-x-0">
          {day.isEdited ? (
            <Button type="button" variant="outline" disabled={!!busy} onClick={() => void onRevert()}>
              {busy === "revert" ? <Loader2 className="h-4 w-4 animate-spin" /> : <RotateCcw className="h-4 w-4" />}
              Revert to machine
            </Button>
          ) : (
            <span />
          )}
          <div className="flex flex-col-reverse gap-2 sm:flex-row">
            <Button type="button" variant="outline" disabled={!!busy} onClick={onClose}>
              Cancel
            </Button>
            <Button type="button" disabled={!!busy || !reasonOk || !timesOk} onClick={() => void onSave()}>
              {busy === "save" ? <Loader2 className="h-4 w-4 animate-spin" /> : null}
              Save change
            </Button>
          </div>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
