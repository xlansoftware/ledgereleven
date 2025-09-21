import { useNavigate } from "react-router-dom";
import { Tabs, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { useMediaQuery } from "@/hooks/use-media-query";
import MobileNavigation from "@/components/mobile-navigation";
import { useSpaceStore } from "@/lib/store-space";
import { cn } from "@/lib/utils";
import { getTintSetting } from "@/lib/spaceSettings";

interface HeaderProps {
  currentPath: string;
}

const allPages = {
  // "/scan": true, -- temporary disabled
  "/history": true,
  "/insights": true,
  "/settings": true,
  "/analysis": true,
}

const validPage = new Set(Object.keys(allPages));

export default function Header({ currentPath }: HeaderProps) {
  const navigate = useNavigate();
  const isMobile = useMediaQuery("(max-width: 768px)");

  const { current } = useSpaceStore();
  const tint = getTintSetting(current);

  const getPathValue = () => {
    if (validPage.has(currentPath)) return currentPath;
    return "/";
  };

  return (
    <header
      className={cn(
        "sticky top-0 z-10 w-full border-b bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/60"
      )}
      style={
        tint
          ? {
              backgroundColor: `color-mix(in oklab, ${tint} 10%, var(--background))`,
            }
          : {}
      }
    >
      <div className="container flex h-14 items-center">
        <div className="mr-4 flex">
          <h1 className="text-xl font-semibold">Ledger Eleven</h1>
        </div>

        {!isMobile && (
          <Tabs
            value={getPathValue()}
            className="ml-auto"
            onValueChange={(value) => navigate(value)}
          >
            <TabsList>
              <TabsTrigger aria-label="Add Screen" value="/">Add</TabsTrigger>
              {/* <TabsTrigger aria-label="Scan Screen" value="/scan">Scan</TabsTrigger> */}
              <TabsTrigger aria-label="History Screen" value="/history">History</TabsTrigger>
              <TabsTrigger aria-label="Insights Screen" value="/insights">Insights</TabsTrigger>
              <TabsTrigger aria-label="Analysis Screen" value="/analysis">Analysis</TabsTrigger>
              <TabsTrigger aria-label="Settings Screen" value="/settings">Settings</TabsTrigger>
            </TabsList>
          </Tabs>
        )}

        {isMobile && (
          <MobileNavigation tint={tint} currentPath={currentPath} />
        )}
      </div>
    </header>
  );
}
