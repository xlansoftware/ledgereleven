import useMultiScreen from "@/hooks/useMultiScreen";
import { ReactElement } from "react";

interface ResponsiveScreensProps {
  screens: ReactElement[];
}

export default function ResponsiveScreens({ screens }: ResponsiveScreensProps) {
  useMultiScreen(screens.length > 1);
  return (
    <div className="pl-4 pr-4 relative h-full flex flex-row items-start gap-4">
      {screens.map((screen, index) => (
        <div
          key={index}
          className="relative h-full flex-1 min-h-0 overflow-y-auto"
        >
          {screen}
        </div>
      ))}
    </div>
  );
}
