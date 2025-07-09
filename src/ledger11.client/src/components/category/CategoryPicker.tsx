import { Category } from "@/lib/types";
import { Button } from "@/components/ui/button";
import { getIcon } from "@/lib/getIcon";
import { cn, invertColor } from "@/lib/utils";

type CategoryPickerProps = {
  categories: Category[];
  selectedId?: number;
  onSelect: (category: Category) => void;
};

export const CategoryPicker: React.FC<CategoryPickerProps> = ({
  categories,
  selectedId,
  onSelect,
}) => {
  return (
    <div className="grid grid-cols-2 md:grid-cols-3 gap-3">
      {categories
        .sort((a, b) => (a.displsyOrder ?? 0) - (b.displsyOrder ?? 0))
        .map((category) => {
          const Icon = getIcon(category.icon);
          const isSelected = selectedId === category.id;
          const color = category.color ?? "#6b7280"; // default: gray-500

          return (
            <Button
              aria-label={`Category ${category.name}`}
              key={category.id}
              variant={isSelected ? "default" : "outline"}
              onClick={() => onSelect(category)}
              className="flex items-stretch p-0 overflow-hidden"
            >
              <div
                className={cn("min-w-4 flex-shrink-0 flex items-center justify-center p-2", isSelected && "border-l border-t border-b border-primary rounded-l-md")}
                style={{ 
                  backgroundColor: color,
                  color: invertColor(color),
                 }}
              >
                <Icon className="flex-shrink-0" />
              </div>
              <div className="flex items-center gap-2 px-0 py-2 text-left w-full overflow-hidden">
                <span className="truncate whitespace-nowrap overflow-hidden text-ellipsis block">
                  {category.name}
                </span>
              </div>
            </Button>
          );
        })}
    </div>
  );
};
