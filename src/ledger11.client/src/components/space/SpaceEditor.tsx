"use client";

import { useState } from "react";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import { PencilIcon, CheckIcon, XIcon } from "lucide-react";
import type { Space } from "@/lib/types";
import { cn } from "@/lib/utils";

interface SpaceEditorProps {
  className?: string;
  space: Space;
  onRename: (id: string, name: string) => void | Promise<void>;
  onClick: (id: string) => void | Promise<void>;
}

export default function SpaceEditor({
  className,
  space,
  onRename,
  onClick,
}: SpaceEditorProps) {
  const [editing, setEditing] = useState(false);
  const [name, setName] = useState(space.name);
  const [loading, setLoading] = useState(false);

  const handleClick = async () => {
    try {
      setLoading(true);
      await onClick(space.id!);
      setEditing(false);
    } catch (error) {
      console.error("Select failed", error);
    } finally {
      setLoading(false);
    }
  };

  const handleSave = async () => {
    if (name?.trim() === "" || name === space.name) {
      setEditing(false);
      return;
    }

    try {
      setLoading(true);
      await onRename(space.id!, name!.trim());
      setEditing(false);
    } catch (error) {
      console.error("Rename failed", error);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className={cn("flex items-center gap-2 min-w-[200px]", className)}>
      {editing ? (
        <>
          <Input
            autoFocus
            value={name}
            onChange={(e) => setName(e.target.value)}
            disabled={loading}
          />
          <Button
            size="icon"
            variant="ghost"
            onClick={handleSave}
            disabled={loading}
          >
            <CheckIcon className="w-4 h-4" />
          </Button>
          <Button
            size="icon"
            variant="ghost"
            onClick={() => {
              setEditing(false);
              setName(space.name);
            }}
            disabled={loading}
          >
            <XIcon className="w-4 h-4" />
          </Button>
        </>
      ) : (
        <>
          <span className="text-base" onClick={handleClick}>
            {space.name}
          </span>
          <Button size="icon" variant="ghost" onClick={() => setEditing(true)}>
            <PencilIcon className="w-4 h-4" />
          </Button>
        </>
      )}
    </div>
  );
}
