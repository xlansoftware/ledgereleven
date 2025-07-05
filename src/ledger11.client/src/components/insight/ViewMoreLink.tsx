import React from "react";
import { Button } from "@/components/ui/button";

interface ViewMoreLinkProps {
  onClick: () => void;
  children: React.ReactNode;
}

const ViewMoreLink: React.FC<ViewMoreLinkProps> = ({ onClick, children }) => {
  return (
    <Button
      onClick={(e) => {
        e.preventDefault();
        onClick();
      }}
      variant="outline"
    >
      {children}
    </Button>
  );
};

export default ViewMoreLink;
