import ResponsiveMenu from "@/components/responsive/ResponsiveMenu";
import { Trash2 } from "lucide-react";
import { type ReactNode } from "react";
import { useConfirmDialog } from "@/components/dialog/ConfirmDialogContext";

interface ConfirmMenuItemProps {
  icon?: ReactNode;
  title?: string;
  description?: string;
  onConfirmed: () => void;
  className?: string;
}

export default function ConfirmMenuItem({
  icon = <Trash2 className="h-4 w-4 mr-2" />,
  title = "Delete",
  description = "Are you sure you want to proceed?",
  onConfirmed,
  className = "text-destructive",
}: ConfirmMenuItemProps) {
  const { confirm } = useConfirmDialog();

  const handleClick = (e: React.MouseEvent) => {
    e.stopPropagation(); // Prevent menu from immediately closing
    confirm({
      title,
      description,
      onConfirm: onConfirmed,
    });
  };

  return (
    <ResponsiveMenu.Item onClick={handleClick} className={className}>
      {icon}
      {title}
    </ResponsiveMenu.Item>
  );
}
