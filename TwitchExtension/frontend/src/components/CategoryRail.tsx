import { CommandIcon } from "./CommandIcon";
import bltLogo from "../assets/blt-logo-v1.png";

interface Props { categories: string[]; selected: string; onSelect(category: string): void; identityName: string; linked: boolean }

export function CategoryRail({ categories, selected, onSelect, identityName, linked }: Props) {
  return <nav className="category-rail" aria-label="Action categories">
    <div className="brand-mark" aria-hidden="true"><img src={bltLogo} alt="" /></div>
    <div className="category-links">
      {categories.map(category => {
        return <button key={category} className={selected === category ? "category active" : "category"} onClick={() => onSelect(category)} aria-current={selected === category ? "page" : undefined}>
          <CommandIcon category={category} /><span>{category}</span>
        </button>;
      })}
    </div>
    <div className="viewer-identity">
      <div className="avatar" aria-hidden="true">{identityName.slice(0, 1).toUpperCase()}</div>
      <strong>{identityName}</strong>
      <span className={linked ? "identity-linked" : "identity-unlinked"}>{linked ? "Identity shared" : "Identity required"}</span>
    </div>
  </nav>;
}
