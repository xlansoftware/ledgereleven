"use client";

import { useNavigate } from "react-router-dom";
import {
  Plus,
  ClipboardList,
  BarChart2,
  Settings,
  Sparkles,
} from "lucide-react";
import { cn } from "@/lib/utils";

interface MobileNavigationProps {
  tint?: string;
  currentPath: string;
}

export default function MobileNavigation({
  tint,
  currentPath,
}: MobileNavigationProps) {
  const navigate = useNavigate();

  return (
    <div
      className={cn(
        "fixed top-0 left-0 z-10 w-full h-16 bg-background border-t md:hidden"
      )}
      style={
        tint
          ? {
              backgroundColor: `color-mix(in oklab, ${tint} 10%, var(--background))`,
            }
          : {}
      }
    >
      <div className="grid grid-cols-5 h-full">
        <button
          onClick={() => navigate("/")}
          className={`flex flex-col items-center justify-center ${
            currentPath === "/" ? "text-primary" : "text-muted-foreground"
          }`}
        >
          <Plus size={20} />
          <span className="text-xs mt-1">Add</span>
        </button>

        <button
          onClick={() => navigate("/scan")}
          className={`flex flex-col items-center justify-center ${
            currentPath === "/scan" ? "text-primary" : "text-muted-foreground"
          }`}
        >
          <Sparkles size={20} />
          <span className="text-xs mt-1">Scan</span>
        </button>

        <button
          onClick={() => navigate("/history")}
          className={`flex flex-col items-center justify-center ${
            currentPath === "/history"
              ? "text-primary"
              : "text-muted-foreground"
          }`}
        >
          <ClipboardList size={20} />
          <span className="text-xs mt-1">History</span>
        </button>

        <button
          onClick={() => navigate("/insights")}
          className={`flex flex-col items-center justify-center ${
            currentPath === "/insights"
              ? "text-primary"
              : "text-muted-foreground"
          }`}
        >
          <BarChart2 size={20} />
          <span className="text-xs mt-1">Insights</span>
        </button>

        <button
          onClick={() => navigate("/settings")}
          className={`flex flex-col items-center justify-center ${
            currentPath === "/settings"
              ? "text-primary"
              : "text-muted-foreground"
          }`}
        >
          <Settings size={20} />
          <span className="text-xs mt-1">Settings</span>
        </button>
      </div>
    </div>
  );
}
