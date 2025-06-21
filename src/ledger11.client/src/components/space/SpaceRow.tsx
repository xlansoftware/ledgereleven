import { Space } from "@/lib/types";
import { useState } from "react";
import { cn, formatCurrency } from "@/lib/utils";
import { Badge } from "../ui/badge";

interface SpaceRowProps {
  className?: string;
  space: Space;
  onClick: (id: string) => void | Promise<void>;
}

export default function SpaceRow({ className, space, onClick }: SpaceRowProps) {
  const [, setLoading] = useState(false);

  const handleClick = async () => {
    try {
      setLoading(true);
      await onClick(space.id!);
    } catch (error) {
      console.error("Select failed", error);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className={cn("flex items-center gap-2 min-w-[200px]  ", className)}>
      <div
        className="w-full hover:cursor-pointer flex flex-col gap-2 p-2 rounded-md hover:bg-muted transition-colors"
        onClick={handleClick}
      >
        <span className="text-base flex items-center gap-2">
          <h1 className="text-xl">{space.name}</h1>
          {space.totalValue && (
            <Badge variant={"outline"}>
              Total: {formatCurrency(space.totalValue)}
            </Badge>
          )}
        </span>
        <div className="pl-4 text-sm text-muted-foreground flex flex-row gap-2">
          {space.countCategories && (
            <span>Categories: {space.countCategories}</span>
          )}
          {space.countTransactions && (
            <span>Transactions: {space.countTransactions}</span>
          )}
        </div>
        <div className="pl-4 text-sm text-muted-foreground flex flex-row gap-2 overflow-hidden">
          Members: {space.members?.length
            ? space.members.join(", ")
            : "No members"}
        </div>
      </div>
    </div>
  );
}
