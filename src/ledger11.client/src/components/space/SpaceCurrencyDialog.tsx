import { useState, useEffect } from "react";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { toast } from "sonner";
import { fetchWithAuth } from "@/api";
import { useSpaceStore } from "@/lib/store-space"; // Needed for loadSpaces

interface SpaceCurrencyDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  currentSpaceId?: string;
  initialCurrency?: string;
}

export default function SpaceCurrencyDialog({
  open,
  onOpenChange,
  currentSpaceId,
  initialCurrency,
}: SpaceCurrencyDialogProps) {
  const { loadSpaces } = useSpaceStore();
  const [selectedCurrency, setSelectedCurrency] = useState<string>(
    initialCurrency ?? "USD"
  );

  useEffect(() => {
    if (initialCurrency) {
      setSelectedCurrency(initialCurrency);
    }
  }, [initialCurrency]);

  const handleSaveCurrency = async () => {
    if (!currentSpaceId || !selectedCurrency) {
      toast.error("No current book or currency selected.");
      return;
    }

    try {
      const response = await fetchWithAuth(`/api/space/currency`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ currency: selectedCurrency, spaceId: currentSpaceId }),
      });

      if (!response.ok) {
        const errorText = await response.text();
        throw new Error(`Failed to update currency: ${errorText}`);
      }

      await loadSpaces(true); // Reload spaces to update the current space's settings
      toast.success(`Currency updated to ${selectedCurrency}`);
      onOpenChange(false); // Close dialog
    } catch (error: unknown) {
      console.error("Error updating currency:", error);
      const message = error && (error as Error).message
      toast.error(`${message}` || "Failed to update currency.");
    }
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Change Currency</DialogTitle>
          <DialogDescription>
            Select the new currency for your current book.
          </DialogDescription>
        </DialogHeader>
        <div className="py-4">
          <Label htmlFor="currency-select">Select Currency:</Label>
          <Select
            value={selectedCurrency}
            onValueChange={setSelectedCurrency}
          >
            <SelectTrigger className="w-[180px]">
              <SelectValue placeholder="Select a currency" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="USD">USD - United States Dollar</SelectItem>
              <SelectItem value="EUR">EUR - Euro</SelectItem>
              <SelectItem value="GBP">GBP - British Pound</SelectItem>
              <SelectItem value="JPY">JPY - Japanese Yen</SelectItem>
              <SelectItem value="CAD">CAD - Canadian Dollar</SelectItem>
              <SelectItem value="AUD">AUD - Australian Dollar</SelectItem>
            </SelectContent>
          </Select>
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>
            Cancel
          </Button>
          <Button onClick={handleSaveCurrency}>Save Changes</Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
