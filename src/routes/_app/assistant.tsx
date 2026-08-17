import { AssistantShellSkeleton } from "@/components/chat/AssistantShellSkeleton";
import { ChatAssistant } from "@/components/chat/ChatAssistant";
import { createFileRoute } from "@tanstack/react-router";

export const Route = createFileRoute("/_app/assistant")({
  head: () => ({ meta: [{ title: "Data Assistant — PO Portal" }] }),
  pendingMs: 0,
  pendingMinMs: 0,
  pendingComponent: AssistantShellSkeleton,
  component: AssistantPage,
});

function AssistantPage() {
  return <ChatAssistant />;
}
