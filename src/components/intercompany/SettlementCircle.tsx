import { useMemo } from "react";
import {
  buildMoneyRotation,
  formatCrore,
  type IntercompanyMatrix,
} from "@/lib/intercompany-api";

function wrapName(name: string): string[] {
  const words = name.replace(/\s+/g, " ").trim().split(" ");
  if (name.length <= 26) return [name];
  const lines: string[] = [""];
  for (const word of words) {
    const next = lines[lines.length - 1] ? `${lines[lines.length - 1]} ${word}` : word;
    if (next.length > 24 && lines[lines.length - 1]) {
      if (lines.length === 2) {
        lines[1] = `${lines[1]}…`.slice(0, 25);
        break;
      }
      lines.push(word);
    } else {
      lines[lines.length - 1] = next;
    }
  }
  return lines.slice(0, 2);
}

function ellipsePoint(cx: number, cy: number, rx: number, ry: number, angle: number) {
  return { x: cx + rx * Math.cos(angle), y: cy + ry * Math.sin(angle) };
}

function ellipseArc(
  cx: number,
  cy: number,
  rx: number,
  ry: number,
  a0: number,
  a1: number,
): string {
  let sweep = a1 - a0;
  if (sweep <= 0) sweep += Math.PI * 2;
  const start = ellipsePoint(cx, cy, rx, ry, a0);
  const end = ellipsePoint(cx, cy, rx, ry, a1);
  const large = sweep > Math.PI ? 1 : 0;
  return `M ${start.x} ${start.y} A ${rx} ${ry} 0 ${large} 1 ${end.x} ${end.y}`;
}

export function SettlementCircle({
  companies,
  matrices,
}: {
  companies: string[];
  matrices: IntercompanyMatrix[];
}) {
  const steps = useMemo(() => buildMoneyRotation(companies, matrices), [companies, matrices]);
  const ordered = steps.map((s) => s.from);
  if (ordered.length < 2) return null;

  const n = ordered.length;
  const first = ordered[0];
  const last = ordered[n - 1];
  const width = 920;
  const height = 620;
  const cx = width / 2;
  const cy = height / 2 + 10;
  const rx = 250;
  const ry = 158;
  const labelR = { rx: 318, ry: 218 };

  return (
    <div className="space-y-2">
      <p className="text-center text-sm text-muted-foreground">
        Live loop with arrows: {first} → … → {last} → {first}.
      </p>

      <div className="overflow-x-auto rounded-2xl border border-slate-200 bg-gradient-to-b from-slate-50 to-slate-100">
        <svg
          viewBox={`0 0 ${width} ${height}`}
          className="mx-auto h-auto w-full min-w-[640px] max-w-[920px]"
          role="img"
          aria-label="Live intercompany transfer circle"
        >
          <defs>
            <filter id="node-shadow" x="-30%" y="-30%" width="160%" height="160%">
              <feDropShadow dx="0" dy="6" stdDeviation="4" floodColor="#0f172a" floodOpacity="0.28" />
            </filter>
            <marker
              id="live-arrow"
              viewBox="0 0 12 12"
              refX="10"
              refY="6"
              markerWidth="9"
              markerHeight="9"
              orient="auto"
            >
              <path d="M 0 1 L 12 6 L 0 11 z" fill="#1e3a8a" />
            </marker>
            <linearGradient id="ring-face" x1="0" y1="0" x2="0" y2="1">
              <stop offset="0%" stopColor="#e2e8f0" />
              <stop offset="100%" stopColor="#94a3b8" />
            </linearGradient>
          </defs>

          <ellipse cx={cx} cy={cy + 18} rx={rx} ry={ry} fill="#cbd5e1" opacity="0.45" />
          <ellipse
            cx={cx}
            cy={cy}
            rx={rx}
            ry={ry}
            fill="url(#ring-face)"
            stroke="#1e3a8a"
            strokeWidth="18"
          />
          <ellipse cx={cx} cy={cy} rx={rx - 22} ry={ry - 16} fill="#f8fafc" />

          <text x={cx} y={cy - 8} textAnchor="middle" fontSize="16" fontWeight="700" fill="#14345a">
            Pay the next
          </text>
          <text x={cx} y={cy + 14} textAnchor="middle" fontSize="12" fill="#475569">
            Arrow = one transfer · last pays first
          </text>

          {steps.map((step, i) => {
            const fromA = -Math.PI / 2 + (i * 2 * Math.PI) / n;
            const toA = -Math.PI / 2 + ((i + 1) * 2 * Math.PI) / n;
            const gap = Math.min(0.2, (2 * Math.PI) / n / 4);
            const a0 = fromA + gap;
            const a1 = toA - gap;
            const d = ellipseArc(cx, cy, rx, ry, a0, a1);
            let midSweep = a1 - a0;
            if (midSweep <= 0) midSweep += Math.PI * 2;
            const midA = a0 + midSweep / 2;
            const label = ellipsePoint(cx, cy, rx + 4, ry + 28, midA);
            const pathId = `live-hop-${i}`;
            return (
              <g key={`${step.from}-${step.to}-${i}`}>
                <path
                  id={pathId}
                  d={d}
                  fill="none"
                  stroke="#1e3a8a"
                  strokeWidth="4"
                  markerEnd="url(#live-arrow)"
                  strokeDasharray="14 10"
                >
                  <animate
                    attributeName="stroke-dashoffset"
                    from="0"
                    to="-96"
                    dur="1.8s"
                    repeatCount="indefinite"
                  />
                </path>
                <circle r="6" fill="#f59e0b" stroke="#fff" strokeWidth="2">
                  <animateMotion dur="3.2s" repeatCount="indefinite" rotate="auto">
                    <mpath href={`#${pathId}`} />
                  </animateMotion>
                </circle>
                <rect
                  x={label.x - 40}
                  y={label.y - 13}
                  width="80"
                  height="24"
                  rx="12"
                  fill="#1e3a8a"
                  stroke="#fff"
                  strokeWidth="2"
                />
                <text
                  x={label.x}
                  y={label.y + 5}
                  textAnchor="middle"
                  fontSize="12"
                  fontWeight="700"
                  fill="#fff"
                >
                  {formatCrore(step.amount)} Cr
                </text>
              </g>
            );
          })}

          {ordered.map((name, i) => {
            const angle = -Math.PI / 2 + (i * 2 * Math.PI) / n;
            const node = ellipsePoint(cx, cy, rx, ry, angle);
            const tag = ellipsePoint(cx, cy, labelR.rx, labelR.ry, angle);
            const lines = wrapName(name);
            const anchor =
              Math.cos(angle) > 0.4 ? "start" : Math.cos(angle) < -0.4 ? "end" : "middle";
            return (
              <g key={name} filter="url(#node-shadow)">
                <circle cx={node.x} cy={node.y} r="26" fill="#14345a" stroke="#fff" strokeWidth="4" />
                <text
                  x={node.x}
                  y={node.y + 5}
                  textAnchor="middle"
                  fontSize="13"
                  fontWeight="700"
                  fill="#fff"
                >
                  {i + 1}
                </text>
                {lines.map((line, li) => (
                  <text
                    key={`${name}-${li}`}
                    x={tag.x}
                    y={tag.y + (li - (lines.length - 1) / 2) * 16}
                    textAnchor={anchor}
                    fontSize="13"
                    fontWeight="700"
                    fill="#0f172a"
                  >
                    {line}
                  </text>
                ))}
              </g>
            );
          })}
        </svg>
      </div>

      <p className="text-center text-xs text-muted-foreground">
        Full paid to / got from / received from lines are in the detailed report below.
      </p>
    </div>
  );
}
