import { Skeleton } from "@/components/ui/skeleton";

/** Full-viewport placeholder shown while the assistant route chunk loads. */
export function AssistantShellSkeleton() {
  return (
    <div className="flex h-full min-h-0 flex-col overflow-hidden bg-background animate-in fade-in duration-150">
      <div className="chat-nav-header flex shrink-0 items-center gap-3 border-b border-primary/15 px-4 py-2.5">
        <Skeleton className="h-9 w-9 rounded-xl" />
        <Skeleton className="h-9 w-9 rounded-xl" />
        <Skeleton className="h-9 w-9 rounded-xl" />
        <Skeleton className="h-5 w-28 rounded-md" />
        <div className="ml-auto flex gap-2">
          <Skeleton className="h-9 w-20 rounded-xl" />
          <Skeleton className="h-9 w-9 rounded-xl" />
        </div>
      </div>

      <div className="flex min-h-0 flex-1">
        <aside className="chat-nav-panel hidden w-64 shrink-0 border-r-2 border-primary/20 p-4 lg:block xl:w-72">
          <Skeleton className="mb-4 h-9 w-full rounded-lg" />
          <div className="space-y-2">
            {Array.from({ length: 5 }).map((_, i) => (
              <Skeleton key={i} className="h-8 w-full rounded-md" />
            ))}
          </div>
        </aside>

        <div className="flex min-w-0 flex-1 flex-col px-4 py-8 md:px-6">
          <Skeleton className="mb-6 h-12 w-12 rounded-2xl" />
          <Skeleton className="mb-2 h-8 w-48 rounded-md" />
          <Skeleton className="mb-8 h-5 w-64 rounded-md" />
          <div className="grid gap-2 sm:grid-cols-2">
            {Array.from({ length: 4 }).map((_, i) => (
              <Skeleton key={i} className="h-16 rounded-xl" />
            ))}
          </div>
        </div>
      </div>
    </div>
  );
}
