import { useNavigate } from "react-router-dom";
import { Tabs, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { useMediaQuery } from "@/hooks/use-media-query";
import MobileNavigation from "@/components/mobile-navigation";
import { useSpaceStore } from "@/lib/store-space";
import { cn } from "@/lib/utils";
import { useEffect } from "react";

interface HeaderProps {
  currentPath: string;
}

export default function Header({ currentPath }: HeaderProps) {
  const navigate = useNavigate();
  const isMobile = useMediaQuery("(max-width: 768px)");

  const { current, loadSpaces } = useSpaceStore();
  const tint = current?.tint;

  useEffect(() => {
    if (!current) {
      loadSpaces();
    }
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const getPathValue = () => {
    if (currentPath === "/") return "/";
    if (currentPath === "/scan") return "/scan";
    if (currentPath === "/history") return "/history";
    if (currentPath === "/insights") return "/insights";
    if (currentPath === "/settings") return "/settings";
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
          <h1 className="text-xl font-semibold">Tiny Ledger</h1>
        </div>

        {!isMobile && (
          <Tabs
            value={getPathValue()}
            className="ml-auto"
            onValueChange={(value) => navigate(value)}
          >
            <TabsList>
              <TabsTrigger value="/">Add</TabsTrigger>
              <TabsTrigger value="/scan">Scan</TabsTrigger>
              <TabsTrigger value="/history">History</TabsTrigger>
              <TabsTrigger value="/insights">Insights</TabsTrigger>
              <TabsTrigger value="/settings">Settings</TabsTrigger>
            </TabsList>
          </Tabs>
        )}

        {isMobile && (
          <MobileNavigation tint={current?.tint} currentPath={currentPath} />
        )}
      </div>
    </header>
  );
}
