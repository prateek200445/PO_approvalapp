import { ChatAssistant } from "@/components/chat/ChatAssistant";
import { createFileRoute } from "@tanstack/react-router";

export const Route = createFileRoute("/_app/assistant")({
  head: () => ({ meta: [{ title: "Data Assistant — PO Portal" }] }),
  component: AssistantPage,
});

function AssistantPage() {
  return <ChatAssistant />;
}
