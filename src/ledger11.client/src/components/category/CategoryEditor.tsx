import { useEffect, useState } from "react";
import { type Category } from "@/lib/types";
import { useBookStore } from "@/lib/store-book";
import { Input } from "../ui/input";
import { ResponsiveIconPicker } from "../responsive/ResponsiveIconPicker";
import { ResponsiveColorPicker } from "../responsive/ResponsiveColorPicker";

interface Props {
  category: Category;
}

export default function CategoryEditor({ category }: Props) {
  const { updateCategory } = useBookStore();

  const [local, setLocal] = useState<Category>(category);
  const [timeoutId, setTimeoutId] = useState<NodeJS.Timeout | null>(null);

  const handleChange = (field: keyof Category, value: unknown) => {
    setLocal((prev) => ({ ...prev, [field]: value }));
  };

  useEffect(() => {
    if (timeoutId) clearTimeout(timeoutId);

    const id = setTimeout(() => {
      if (JSON.stringify(local) !== JSON.stringify(category)) {
        updateCategory(local.id, {
          name: local.name,
          color: local.color,
          icon: local.icon,
        });
      }
    }, 500);

    setTimeoutId(id);

    return () => clearTimeout(id);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [local]);

  return (
    <div className="flex gap-2 items-center w-full">
      <Input
        className="flex-grow"
        value={local.name}
        onChange={(e) => handleChange("name", e.target.value)}
      />
      <ResponsiveColorPicker
        value={local.color || "#000000"}
        onChange={(newColor) => handleChange("color", newColor)}
        title="Choose a color"
      />
      <ResponsiveIconPicker
        value={local.icon || ""}
        onChange={(val) => handleChange("icon", val)}
        title="Pick an icon"
      />
    </div>
  );
}
