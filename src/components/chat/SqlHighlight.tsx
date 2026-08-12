import { useMemo } from "react";
import { cn } from "@/lib/utils";

const KEYWORDS = new Set(
  [
    "SELECT",
    "FROM",
    "WHERE",
    "AND",
    "OR",
    "NOT",
    "IN",
    "AS",
    "ON",
    "JOIN",
    "INNER",
    "LEFT",
    "RIGHT",
    "OUTER",
    "CROSS",
    "FULL",
    "GROUP",
    "BY",
    "ORDER",
    "HAVING",
    "TOP",
    "DISTINCT",
    "COUNT",
    "SUM",
    "AVG",
    "MIN",
    "MAX",
    "CASE",
    "WHEN",
    "THEN",
    "ELSE",
    "END",
    "IS",
    "NULL",
    "LIKE",
    "BETWEEN",
    "EXISTS",
    "UNION",
    "ALL",
    "INSERT",
    "INTO",
    "VALUES",
    "UPDATE",
    "SET",
    "DELETE",
    "WITH",
    "OVER",
    "PARTITION",
    "ASC",
    "DESC",
    "OFFSET",
    "FETCH",
    "NEXT",
    "ROWS",
    "ONLY",
    "CAST",
    "CONVERT",
    "ISNULL",
    "COALESCE",
    "DECLARE",
    "TABLE",
    "VIEW",
    "INTO",
  ].map((k) => k.toUpperCase()),
);

type TokenKind = "keyword" | "string" | "number" | "comment" | "punct" | "ident" | "space";

interface Token {
  kind: TokenKind;
  text: string;
}

function tokenizeSql(sql: string): Token[] {
  const tokens: Token[] = [];
  let i = 0;

  while (i < sql.length) {
    const ch = sql[i];

    // Whitespace (preserve newlines)
    if (/\s/.test(ch)) {
      let j = i + 1;
      while (j < sql.length && /\s/.test(sql[j])) j++;
      tokens.push({ kind: "space", text: sql.slice(i, j) });
      i = j;
      continue;
    }

    // Line comment
    if (ch === "-" && sql[i + 1] === "-") {
      let j = i + 2;
      while (j < sql.length && sql[j] !== "\n") j++;
      tokens.push({ kind: "comment", text: sql.slice(i, j) });
      i = j;
      continue;
    }

    // Block comment
    if (ch === "/" && sql[i + 1] === "*") {
      let j = i + 2;
      while (j < sql.length - 1 && !(sql[j] === "*" && sql[j + 1] === "/")) j++;
      j = Math.min(j + 2, sql.length);
      tokens.push({ kind: "comment", text: sql.slice(i, j) });
      i = j;
      continue;
    }

    // String literal
    if (ch === "'" || ch === '"') {
      const quote = ch;
      let j = i + 1;
      while (j < sql.length) {
        if (sql[j] === quote) {
          if (sql[j + 1] === quote) {
            j += 2;
            continue;
          }
          j++;
          break;
        }
        j++;
      }
      tokens.push({ kind: "string", text: sql.slice(i, j) });
      i = j;
      continue;
    }

    // Bracketed identifier [Col Name]
    if (ch === "[") {
      let j = i + 1;
      while (j < sql.length && sql[j] !== "]") j++;
      if (j < sql.length) j++;
      tokens.push({ kind: "ident", text: sql.slice(i, j) });
      i = j;
      continue;
    }

    // Number
    if (/\d/.test(ch)) {
      let j = i + 1;
      while (j < sql.length && /[\d.]/.test(sql[j])) j++;
      tokens.push({ kind: "number", text: sql.slice(i, j) });
      i = j;
      continue;
    }

    // Identifier / keyword
    if (/[A-Za-z_@#]/.test(ch)) {
      let j = i + 1;
      while (j < sql.length && /[A-Za-z0-9_@#$]/.test(sql[j])) j++;
      const text = sql.slice(i, j);
      tokens.push({
        kind: KEYWORDS.has(text.toUpperCase()) ? "keyword" : "ident",
        text,
      });
      i = j;
      continue;
    }

    // Punctuation
    tokens.push({ kind: "punct", text: ch });
    i++;
  }

  return tokens;
}

const KIND_CLASS: Record<TokenKind, string> = {
  keyword: "text-sky-400 font-semibold",
  string: "text-emerald-400",
  number: "text-amber-300",
  comment: "text-slate-500 italic",
  punct: "text-slate-400",
  ident: "text-slate-200",
  space: "",
};

interface SqlHighlightProps {
  sql: string;
  className?: string;
}

export function SqlHighlight({ sql, className }: SqlHighlightProps) {
  const tokens = useMemo(() => tokenizeSql(sql || ""), [sql]);

  if (!sql) {
    return (
      <pre className={cn("font-mono text-[10px] text-slate-400", className)}>—</pre>
    );
  }

  return (
    <pre
      className={cn(
        "max-h-56 overflow-auto whitespace-pre-wrap break-words rounded-xl border border-border/50 bg-[#0f172a] p-3 font-mono text-[10px] leading-relaxed dark:bg-black/50",
        className,
      )}
    >
      {tokens.map((t, idx) =>
        t.kind === "space" ? (
          <span key={idx}>{t.text}</span>
        ) : (
          <span key={idx} className={KIND_CLASS[t.kind]}>
            {t.text}
          </span>
        ),
      )}
    </pre>
  );
}
