import { useState } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { Download, FileText, Loader2, Send } from "lucide-react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { useAuth } from "@/lib/auth-context";
import { isDailyReportDigestAdmin } from "@/lib/feature-flags";
import {
  downloadDailyReportDigestPdf,
  formatReportDateTime,
  getDailyReportDigestStatus,
  sendDailyReportDigest,
} from "@/lib/daily-report-api";

function todayIso() {
  const d = new Date();
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-${String(d.getDate()).padStart(2, "0")}`;
}

function to12h(hhmm: string) {
  const [h, m] = hhmm.split(":").map(Number);
  if (!Number.isFinite(h) || !Number.isFinite(m)) return hhmm;
  return `${h % 12 || 12}:${String(m).padStart(2, "0")} ${h < 12 ? "AM" : "PM"}`;
}

export function DailyReportDigestCard() {
  const { user } = useAuth();
  const username = user?.username ?? "";
  const canSend = isDailyReportDigestAdmin(username);
  const queryClient = useQueryClient();
  const [date, setDate] = useState(todayIso);
  const [busy, setBusy] = useState<"download" | "send" | null>(null);

  const statusQuery = useQuery({
    queryKey: ["daily-report-digest-status", date],
    queryFn: () => getDailyReportDigestStatus(date),
    enabled: /^\d{4}-\d{2}-\d{2}$/.test(date),
    staleTime: 60_000,
  });
  const status = statusQuery.data;
  const sendTime = status?.sendTime ? to12h(status.sendTime) : "7:00 PM";

  async function onDownload() {
    setBusy("download");
    try {
      await downloadDailyReportDigestPdf(date);
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Could not download the PDF");
    } finally {
      setBusy(null);
    }
  }

  async function onSend() {
    setBusy("send");
    try {
      const res = await sendDailyReportDigest(date, username);
      toast.success(`PDF with ${res.reportCount ?? 0} report(s) sent on WhatsApp to ${res.sentTo ?? "recipients"}`);
      void queryClient.invalidateQueries({ queryKey: ["daily-report-digest-status", date] });
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "WhatsApp send failed");
    } finally {
      setBusy(null);
    }
  }

  return (
    <section className="card-3d rounded-2xl p-3 sm:p-4" aria-label="Daily reports PDF">
      <div className="flex flex-wrap items-end justify-between gap-3">
        <div className="min-w-0">
          <h2 className="flex items-center gap-2 text-base font-semibold">
            <FileText className="h-4 w-4 text-primary" /> Day's reports as one PDF
          </h2>
          <p className="mt-0.5 text-xs text-muted-foreground">
            All reports submitted on the day till {sendTime}. Sent automatically on WhatsApp at {sendTime}
            {status?.recipients?.length ? ` to ${status.recipients.join(", ")}` : ""}.
          </p>
        </div>
        <div className="flex flex-wrap items-end gap-2">
          <div className="space-y-1">
            <Label htmlFor="digest-date" className="text-xs">
              Date
            </Label>
            <Input
              id="digest-date"
              type="date"
              value={date}
              max={todayIso()}
              onChange={(e) => setDate(e.target.value)}
              className="h-10 w-40"
            />
          </div>
          <Button type="button" variant="outline" className="h-10" disabled={!!busy} onClick={() => void onDownload()}>
            {busy === "download" ? <Loader2 className="h-4 w-4 animate-spin" /> : <Download className="h-4 w-4" />}
            Download PDF
          </Button>
          {canSend ? (
            <Button type="button" className="h-10" disabled={!!busy} onClick={() => void onSend()}>
              {busy === "send" ? <Loader2 className="h-4 w-4 animate-spin" /> : <Send className="h-4 w-4" />}
              Send on WhatsApp now
            </Button>
          ) : null}
        </div>
      </div>
      {status?.sentAt ? (
        <p className="mt-2 text-xs text-emerald-700 dark:text-emerald-300">
          Sent {formatReportDateTime(status.sentAt)} to {status.sentTo}
          {status.reportCount != null ? ` · ${status.reportCount} report(s)` : ""}
          {status.triggeredBy ? ` · by ${status.triggeredBy}` : ""}
        </p>
      ) : status?.lastError ? (
        <p className="mt-2 text-xs text-destructive">Last send failed: {status.lastError}</p>
      ) : null}
    </section>
  );
}
