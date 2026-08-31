import heroIcon from "../assets/command-icons/hero.png";
import battleIcon from "../assets/command-icons/battle.png";
import kingdomIcon from "../assets/command-icons/kingdom.png";
import equipmentIcon from "../assets/command-icons/equipment.png";
import progressionIcon from "../assets/command-icons/progression.png";
import tournamentIcon from "../assets/command-icons/tournament.png";
import communityIcon from "../assets/command-icons/community.png";
import generalIcon from "../assets/command-icons/general.png";

const icons: Record<string, string> = {
  Hero: heroIcon, Battle: battleIcon, Kingdom: kingdomIcon, Equipment: equipmentIcon,
  Progression: progressionIcon, Tournament: tournamentIcon, Community: communityIcon, General: generalIcon,
};

export function CommandIcon({ category, className = "" }: { category: string; className?: string }) {
  return <img className={`command-icon ${className}`} src={icons[category] ?? generalIcon} alt="" aria-hidden="true" />;
}
