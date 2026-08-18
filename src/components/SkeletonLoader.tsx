import React from "react";

export function SkeletonCard() {
  return (
    <div className="rounded-xl border border-border bg-card p-5 shadow-sm animate-pulse">
      <div className="mb-4 h-4 w-1/3 bg-muted rounded"></div>
      <div className="mb-4 h-6 w-2/3 bg-muted rounded"></div>
      <div className="h-4 w-1/2 bg-muted rounded mb-3"></div>
      <div className="grid grid-cols-2 gap-x-4 gap-y-3 border-t border-border pt-4 md:grid-cols-3">
        {[...Array(6)].map((_, i) => (
          <div key={i}>
            <div className="h-3 w-20 bg-muted rounded mb-2"></div>
            <div className="h-4 w-24 bg-muted rounded"></div>
          </div>
        ))}
      </div>
    </div>
  );
}

export function SkeletonSection({ title }: { title: string }) {
  return (
    <section className="rounded-xl border border-border bg-card animate-pulse">
      <header className="border-b border-border px-5 py-3">
        <h2 className="text-sm font-semibold h-4 w-32 bg-muted rounded"></h2>
      </header>
      <div className="p-5 space-y-4">
        <div className="h-4 w-full bg-muted rounded"></div>
        <div className="h-4 w-5/6 bg-muted rounded"></div>
        <div className="h-4 w-4/6 bg-muted rounded"></div>
      </div>
    </section>
  );
}

export function SkeletonTable() {
  return (
    <div className="rounded-lg border border-border overflow-hidden animate-pulse">
      <table className="w-full">
        <thead className="bg-secondary/50">
          <tr>
            {[...Array(4)].map((_, i) => (
              <th key={i} className="px-3 py-3">
                <div className="h-3 w-12 bg-muted rounded"></div>
              </th>
            ))}
          </tr>
        </thead>
        <tbody className="divide-y divide-border">
          {[...Array(5)].map((_, rowIdx) => (
            <tr key={rowIdx}>
              {[...Array(4)].map((_, colIdx) => (
                <td key={colIdx} className="px-3 py-3">
                  <div className="h-4 w-20 bg-muted rounded"></div>
                </td>
              ))}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

export function SkeletonStats() {
  return (
    <>
      {[...Array(4)].map((_, i) => (
        <div key={i} className="card-3d rounded-2xl p-4 animate-pulse">
          <div className="mb-3 h-9 w-9 bg-muted rounded-lg"></div>
          <div className="h-7 w-16 bg-muted rounded mb-2"></div>
          <div className="h-3 w-12 bg-muted rounded"></div>
        </div>
      ))}
    </>
  );
}

export function SkeletonPendingList() {
  return (
    <>
      {[...Array(5)].map((_, i) => (
        <div key={i} className="flex items-center justify-between gap-3 py-3 animate-pulse">
          <div className="min-w-0 flex-1">
            <div className="h-4 w-24 bg-muted rounded mb-2"></div>
            <div className="h-3 w-32 bg-muted rounded"></div>
          </div>
          <div className="text-right">
            <div className="h-4 w-20 bg-muted rounded mb-2"></div>
            <div className="h-5 w-16 bg-muted rounded"></div>
          </div>
        </div>
      ))}
    </>
  );
}

export function SkeletonWorkflow() {
  return (
    <div className="space-y-3">
      {[...Array(3)].map((_, i) => (
        <div key={i} className="rounded-lg border border-border p-3 animate-pulse">
          <div className="h-4 w-28 bg-muted rounded mb-2"></div>
          <div className="h-6 w-16 bg-muted rounded mb-2"></div>
          <div className="h-3 w-32 bg-muted rounded"></div>
        </div>
      ))}
    </div>
  );
}
