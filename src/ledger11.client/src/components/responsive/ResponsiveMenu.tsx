import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "../ui/dropdown-menu";
import {
  Drawer,
  DrawerContent,
  DrawerTrigger,
} from "../ui/drawer";
import { Button } from "../ui/button";
import { createContext, useContext, useState } from "react";
import useIsMobile from "@/hooks/useIsMobile";
import { DialogTitle } from "@radix-ui/react-dialog";

const ResponsiveMenuContext = createContext<{
  isOpen: boolean;
  setIsOpen: (v: boolean) => void;
  isMobile: boolean;
} | null>(null);

const useResponsiveMenuContext = () => {
  const ctx = useContext(ResponsiveMenuContext);
  if (!ctx) throw new Error("Must be used within <ResponsiveMenu>");
  return ctx;
};

function ResponsiveMenu({ children }: { children: React.ReactNode }) {
  const [isOpen, setIsOpen] = useState(false);
  const isMobile = useIsMobile();

  return (
    <ResponsiveMenuContext.Provider value={{ isOpen, setIsOpen, isMobile }}>
      {isMobile ? (
        <Drawer open={isOpen} onOpenChange={setIsOpen}>
          {children}
        </Drawer>
      ) : (
        <DropdownMenu open={isOpen} onOpenChange={setIsOpen}>
          {children}
        </DropdownMenu>
      )}
    </ResponsiveMenuContext.Provider>
  );
}

ResponsiveMenu.Title = function Title({ children }: { children: React.ReactNode }) {
  const { isMobile } = useResponsiveMenuContext();
  return isMobile ? (
    <DialogTitle>{children}</DialogTitle>
  ) : (
    <div className="font-semibold">{children}</div>
  );
}

ResponsiveMenu.Trigger = function Trigger({ children }: { children: React.ReactNode }) {
  const { isMobile } = useResponsiveMenuContext();
  return isMobile ? (
    <DrawerTrigger asChild>{children}</DrawerTrigger>
  ) : (
    <DropdownMenuTrigger asChild>{children}</DropdownMenuTrigger>
  );
};

ResponsiveMenu.Content = function Content({ children }: { children: React.ReactNode }) {
  const { isMobile } = useResponsiveMenuContext();
  return isMobile ? (
    <DrawerContent>
      <div className="p-4 space-y-2">{children}</div>
    </DrawerContent>
  ) : (
    <DropdownMenuContent align="end">{children}</DropdownMenuContent>
  );
};

ResponsiveMenu.Item = function Item({
  children,
  onClick,
  className = "",
}: {
  children: React.ReactNode;
  onClick?: (e: React.MouseEvent) => void;
  className?: string;
}) {
  const { isMobile, setIsOpen } = useResponsiveMenuContext();
  const handleClick = (e: React.MouseEvent) => {
    onClick?.(e);
    setIsOpen(false);
  };

  return isMobile ? (
    <Button
      variant="ghost"
      className={`w-full justify-start ${className}`}
      onClick={handleClick}
    >
      {children}
    </Button>
  ) : (
    <DropdownMenuItem onClick={handleClick} className={className}>
      {children}
    </DropdownMenuItem>
  );
};

export default ResponsiveMenu;
