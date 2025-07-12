"use client";

import { useEffect, useState } from "react";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import { toast } from "sonner";
import { MergeIcon, MoreHorizontalIcon, PencilIcon, UserPlus2Icon } from "lucide-react";
import { useSpaceStore } from "@/lib/store-space";
import ShareSpaceDialog from "@/components/space/ShareSpaceDialog";
import { cn } from "@/lib/utils";
import ResponsiveMenu from "../responsive/ResponsiveMenu";
import ConfirmMenuItem from "../history/ConfirmMenuItem";
import { Space } from "@/lib/types";
import SpaceEditForm from "../space/SpaceEditForm";
import SpaceRow from "../space/SpaceRow";
import { fetchWithAuth } from "@/api";
import { useBookStore } from "@/lib/store-book";

export default function SpacesScreen() {
  const {
    current,
    spaces,
    addSpace,
    removeSpace,
    loadSpaces,
    updateSpace,
    setCurrentSpace,
  } = useSpaceStore();

  const { openBook } = useBookStore();

  const [newSpaceName, setNewSpaceName] = useState("");
  const [sharingSpaceId, setSharingSpaceId] = useState<string | null>(null);
  const [editSpace, setEditSpace] = useState<Space | null>(null);
  const [mergeSpace, setMergeSpace] = useState<Space | null>(null);

  useEffect(() => {
    loadSpaces(true);
  }, [loadSpaces]);

  const handleAdd = async () => {
    if (!newSpaceName.trim()) return;

    try {
      await addSpace({ name: newSpaceName });
      await loadSpaces(true); // Reload spaces to get the new one with details
      toast.success("Space created");
      setNewSpaceName("");
    } catch (err) {
      toast.error("Failed to create space");
      console.error(err);
    }
  };

  const confirmRemove = async (spaceToDelete: string) => {
    if (!spaceToDelete) return;
    try {
      await removeSpace(spaceToDelete);
      toast.success("Space removed");
    } catch (err) {
      toast.error("Failed to remove space");
      console.error(err);
    }
  };

  const handleUpdate = async (space: Space) => {
    try {
      await updateSpace(space.id!, space);
      toast.success("Space updated");
    } catch (err) {
      toast.error("Failed to update space");
      console.error(err);
    }
  };

  const handheleShare = async (spaceId: string, email: string) => {
    console.log("Sharing space", spaceId, "with", email);
    if (!spaceId || !email) {
      return;
    }

    await fetchWithAuth('/api/space/share', {
      method: 'POST',
      body: JSON.stringify({ spaceId, email }),
      headers: {
        'Content-Type': 'application/json',
      },
    });

    toast.success("Done!");

    await loadSpaces(true); // Reload spaces to reflect changes

    // Reset sharing state
    setSharingSpaceId(null);
  };

  return (
    <div className="p-4 space-y-4">
      <div className="flex items-center gap-2">
        <Input
          placeholder="New book name"
          value={newSpaceName}
          onChange={(e) => setNewSpaceName(e.target.value)}
        />
        <Button onClick={handleAdd}>Create</Button>
      </div>

      <div className="space-y-4">
        {spaces.map((space) => (
          <div
            key={space.id}
            data-testid={`Space: ${space.name}`}
            className={cn(
              "flex items-center gap-2",
              current?.id === space.id && "bg-muted border border-border"
            )}
          >
            <SpaceRow
              space={space}
              onClick={async (spaceId) => {
                await setCurrentSpace(spaceId);
                await openBook(spaceId);
              }}
              className="w-full"
            />

            <ResponsiveMenu>
              <ResponsiveMenu.Trigger>
                <Button variant="ghost" size="icon">
                  <MoreHorizontalIcon className="w-4 h-4" />
                  <span className="sr-only">Actions</span>
                </Button>
              </ResponsiveMenu.Trigger>
              <ResponsiveMenu.Content>
                <ResponsiveMenu.Title>Actions</ResponsiveMenu.Title>
                <ResponsiveMenu.Item
                  onClick={() => {
                    // allow the menu item to close...
                    requestAnimationFrame(() => {
                      setEditSpace(space);
                    });
                  }}
                >
                  <PencilIcon className="w-4 h-4 mr-2" />
                  Edit
                </ResponsiveMenu.Item>

                <ResponsiveMenu.Item
                  onClick={() => {
                    // allow the menu item to close...
                    requestAnimationFrame(() => {
                      setSharingSpaceId(space.id!);
                    });
                  }}
                >
                  <UserPlus2Icon className="w-4 h-4 mr-2" />
                  Share
                </ResponsiveMenu.Item>
                <ResponsiveMenu.Item
                  onClick={() => {
                    // allow the menu item to close...
                    requestAnimationFrame(() => {
                      // open the merge dialog
                      setMergeSpace(space);
                    });
                  }}
                >
                  <MergeIcon className="w-4 h-4 mr-2" />
                  Close and Merge
                </ResponsiveMenu.Item>
                <ConfirmMenuItem
                  title="Remove"
                  description="This will remove the space and all data inside it."
                  onConfirmed={() => confirmRemove(space.id!)}
                />
              </ResponsiveMenu.Content>
            </ResponsiveMenu>
          </div>
        ))}
      </div>

      <ShareSpaceDialog
        spaceId={sharingSpaceId || ""}
        onShare={handheleShare}
        onCancel={() => {
          setSharingSpaceId(null);
        }}
        key={sharingSpaceId || "none"}
      />

      {editSpace && (
        <SpaceEditForm
          space={editSpace}
          onClose={async (updatedSpace) => {
            if (updatedSpace) {
              try {
                await handleUpdate(updatedSpace);
              } finally {
                setEditSpace(null);
              }
            }

            if (!updatedSpace) {
              requestAnimationFrame(() => {
                setEditSpace(null);
              });
            }
          }}
        />
      )}

      {mergeSpace && (
        <>{/* open the merge form here */}</>        
      )}
    </div>
  );
}
