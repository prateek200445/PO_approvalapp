import { useState, type FormEvent, type ReactNode } from "react";
import { CheckCircle2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Textarea } from "@/components/ui/textarea";
import { createExhibitionLead, PRODUCT_INQUIRIES, type ExhibitionFormType } from "@/lib/exhibition-leads-api";

function Field({
  label,
  required,
  children,
}: {
  label: string;
  required?: boolean;
  children: ReactNode;
}) {
  return (
    <div className="space-y-1.5">
      <Label className="text-base">
        {label}
        {required ? <span className="text-destructive"> *</span> : null}
      </Label>
      {children}
    </div>
  );
}

const fieldClass = "h-12 text-base";

export function PublicExhibitionForm({
  title,
  formType,
}: {
  title: string;
  formType: ExhibitionFormType;
}) {
  const [companyName, setCompanyName] = useState("");
  const [personName, setPersonName] = useState("");
  const [contactNumber, setContactNumber] = useState("");
  const [email, setEmail] = useState("");
  const [address, setAddress] = useState("");
  const [postalCode, setPostalCode] = useState("");
  const [productInquiry, setProductInquiry] = useState("");
  const [quantity, setQuantity] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState("");
  const [done, setDone] = useState(false);

  function reset() {
    setCompanyName("");
    setPersonName("");
    setContactNumber("");
    setEmail("");
    setAddress("");
    setPostalCode("");
    setProductInquiry("");
    setQuantity("");
    setError("");
    setDone(false);
  }

  async function submit(event: FormEvent) {
    event.preventDefault();
    setError("");
    const name = personName.trim();
    const phone = contactNumber.trim();
    if (!name || !phone) {
      setError("Name and contact number are required.");
      return;
    }
    if (formType === "ProductInquiry") {
      if (!productInquiry) {
        setError("Select a product.");
        return;
      }
      const qty = Number(quantity);
      if (!quantity.trim() || Number.isNaN(qty) || qty < 0) {
        setError("Enter a quantity.");
        return;
      }
    }

    setSubmitting(true);
    try {
      await createExhibitionLead(
        formType === "DealerDistributor"
          ? {
              formType,
              companyName: companyName.trim() || null,
              personName: name,
              contactNumber: phone,
              email: email.trim() || null,
              address: address.trim() || null,
              postalCode: postalCode.trim() || null,
            }
          : {
              formType,
              personName: name,
              contactNumber: phone,
              email: email.trim() || null,
              productInquiry,
              quantity: Number(quantity),
            },
      );
      setDone(true);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Could not submit the form.");
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div className="min-h-screen bg-background px-4 py-6 pb-[max(1.5rem,env(safe-area-inset-bottom))]">
      <div className="mx-auto w-full max-w-md">
        <div className="mb-6 flex items-center gap-3">
          <img src="/hcp_logo.jpeg" alt="HCP" className="h-11 w-11 rounded-lg ring-1 ring-black/5" />
          <div>
            <div className="text-sm font-semibold">HCP Plastene Bulkpack Ltd.</div>
            <div className="text-xs text-muted-foreground">Exhibition form</div>
          </div>
        </div>

        <h1 className="text-2xl font-semibold tracking-tight">{title}</h1>

        {done ? (
          <div className="mt-8 rounded-xl border bg-card p-6 text-center">
            <CheckCircle2 className="mx-auto h-10 w-10 text-primary" />
            <p className="mt-3 text-lg font-medium">Thank you</p>
            <p className="mt-1 text-sm text-muted-foreground">Your details have been submitted.</p>
            <Button type="button" className="mt-6 h-12 w-full text-base" onClick={reset}>
              Submit another
            </Button>
          </div>
        ) : (
          <form className="mt-6 space-y-4" onSubmit={submit}>
            {formType === "DealerDistributor" ? (
              <Field label="Company Name">
                <Input className={fieldClass} value={companyName} onChange={(e) => setCompanyName(e.target.value)} autoComplete="organization" />
              </Field>
            ) : null}

            <Field label={formType === "DealerDistributor" ? "Person Name" : "Name"} required>
              <Input className={fieldClass} value={personName} onChange={(e) => setPersonName(e.target.value)} autoComplete="name" required />
            </Field>

            <Field label="Contact Number" required>
              <Input className={fieldClass} type="tel" inputMode="tel" value={contactNumber} onChange={(e) => setContactNumber(e.target.value)} autoComplete="tel" required />
            </Field>

            <Field label="Email">
              <Input className={fieldClass} type="email" inputMode="email" value={email} onChange={(e) => setEmail(e.target.value)} autoComplete="email" />
            </Field>

            {formType === "DealerDistributor" ? (
              <>
                <Field label="Address">
                  <Textarea className="min-h-24 text-base" value={address} onChange={(e) => setAddress(e.target.value)} autoComplete="street-address" />
                </Field>
                <Field label="Postal Code">
                  <Input className={fieldClass} inputMode="numeric" value={postalCode} onChange={(e) => setPostalCode(e.target.value)} autoComplete="postal-code" />
                </Field>
              </>
            ) : (
              <>
                <Field label="Product Inquiry" required>
                  <Select value={productInquiry || undefined} onValueChange={setProductInquiry}>
                    <SelectTrigger className="h-12 w-full bg-background px-3 text-base">
                      <SelectValue placeholder="Select a product" />
                    </SelectTrigger>
                    <SelectContent className="w-[var(--radix-select-trigger-width)]">
                      {PRODUCT_INQUIRIES.map((product) => (
                        <SelectItem key={product} value={product} className="py-2.5 text-base">
                          {product}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </Field>
                <Field label="Quantity" required>
                  <Input className={fieldClass} type="number" inputMode="decimal" min={0} step="any" value={quantity} onChange={(e) => setQuantity(e.target.value)} required />
                </Field>
              </>
            )}

            {error ? <p className="text-sm text-destructive">{error}</p> : null}

            <Button type="submit" className="h-12 w-full text-base" disabled={submitting}>
              {submitting ? "Submitting…" : "Submit"}
            </Button>
          </form>
        )}
      </div>
    </div>
  );
}
