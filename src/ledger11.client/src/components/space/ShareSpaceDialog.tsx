"use client";

import { useState } from "react";
import {
  Dialog,
  DialogTrigger,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import { toast } from "sonner";

interface ShareSpaceDialogProps {
  spaceId: string;
  trigger?: React.ReactNode;
  onShare: (spaceId: string, email: string) => Promise<void> | void;
  onCancel: () => void;
}

export default function ShareSpaceDialog({
  spaceId,
  trigger,
  onShare,
  onCancel,
}: ShareSpaceDialogProps) {
  const [email, setEmail] = useState("");
  const [open, setOpen] = useState(!!spaceId);
  const [loading, setLoading] = useState(false);

  const handleShare = async () => {
    if (!email.trim()) return;

    setLoading(true);
    try {
      await onShare(spaceId, email.trim());
      toast.success(`Space shared with ${email}`);
      setEmail("");
      setOpen(false);
    } catch (err) {
      console.error(err);
      toast.error("Failed to share space");
    } finally {
      setLoading(false);
    }
  };

  const handleOpendChange = (open: boolean) => {
    setOpen(open);
    if (!open) {
      setEmail("");
      onCancel();
    }
  };

  return (
    <Dialog open={open} onOpenChange={handleOpendChange}>
      {trigger && <DialogTrigger asChild>{trigger}</DialogTrigger>}
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Share Book</DialogTitle>
          <DialogDescription>
            Enter the email address of the user you want to share this space
            with.
          </DialogDescription>
        </DialogHeader>
        <Input
          type="email"
          placeholder="user@example.com"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          disabled={loading}
        />
        <DialogFooter>
          <Button onClick={handleShare} disabled={loading || !email.trim()}>
            Share
          </Button>
          <Button onClick={() => handleOpendChange(false)} variant="secondary">
            Cancel
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
