import type { CSSProperties } from "react";
import iconSheet from "../assets/blt-command-icons-v1.png";

const positions: Record<string, string> = {
  Hero: "0%",
  Battle: "14.2857%",
  Kingdom: "28.5714%",
  Equipment: "42.8571%",
  Progression: "57.1429%",
  Tournament: "71.4286%",
  Community: "85.7143%",
  General: "100%",
};

export function CommandIcon({ category, className = "" }: { category: string; className?: string }) {
  return <span
    className={`command-icon ${className}`}
    role="img"
    aria-label={`${category} action`}
    style={{ "--icon-sheet": `url(${iconSheet})`, "--icon-position": positions[category] ?? positions.General } as CSSProperties}
  />;
}
