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
import { Input } from "@/components/ui/input";
import { RefreshCw } from "lucide-react";
import { toast } from "sonner";
import { fetchWithAuth } from "@/api";
import { useSpaceStore } from "@/lib/store-space";
import { useConfirmDialog } from "../dialog/ConfirmDialogContext";

interface SpaceCurrencyDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  currentSpaceId?: string;
  initialCurrency?: string;
  initialExchangeRate?: number;
}

export default function SpaceCurrencyDialog({
  open,
  onOpenChange,
  currentSpaceId,
  initialCurrency,
  initialExchangeRate,
}: SpaceCurrencyDialogProps) {
  const { loadSpaces } = useSpaceStore();
  const [selectedCurrency, setSelectedCurrency] = useState<string>(
    initialCurrency ?? "USD"
  );
  const [exchangeRate, setExchangeRate] = useState<number>(
    initialExchangeRate ?? 1.0
  );
  const [refreshingRate, setRefreshingRate] = useState(false); // New state for loading
  const { confirm } = useConfirmDialog();

  useEffect(() => {
    if (initialCurrency) {
      setSelectedCurrency(initialCurrency);
    }
    if (initialExchangeRate !== undefined) {
      setExchangeRate(initialExchangeRate);
    }
  }, [initialCurrency, initialExchangeRate]);

  const handleRefreshExchangeRate = async () => {
    if (!selectedCurrency) {
      return;
    }

    setRefreshingRate(true);
    try {
      const params = new URLSearchParams({ fromCurrency: initialCurrency || "USD", toCurrency: selectedCurrency });
      const response = await fetchWithAuth(
        `/api/currency/exchange-rate?${params.toString()}`
      );

      if (!response.ok) {
        const errorText = await response.text();
        console.warn(`Exchange rate fetch failed: ${response.status} - ${errorText}`);
        toast.error("Failed to fetch exchange rate.");
        return;
      }

      const data: { rate: number } = await response.json();
      setExchangeRate(data.rate);
    } catch (error) {
      console.error("Error fetching exchange rate:", error);
    } finally {
      setRefreshingRate(false);
    }
  };

  const handleSaveCurrency = async () => {
    if (!currentSpaceId || !selectedCurrency) {
      toast.error("No current book or currency selected.");
      return;
    }

    confirm({
      title: "Confirm Currency Change",
      description: "Changing the currency will recompute all existing transactions in this book according to the new exchange rate. Are you sure you want to proceed?",
      onConfirm: async () => {
        try {
          const response = await fetchWithAuth(`/api/space/currency`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ 
              currency: selectedCurrency, 
              spaceId: currentSpaceId, 
              exchangeRate: exchangeRate }),
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
      },
    });
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
        <div className="py-4 space-y-4"> {/* Added space-y-4 for vertical spacing */}
          <div>
            <Label htmlFor="currency-input">New Default Currency:</Label>
            <Input
              id="currency-input"
              value={selectedCurrency}
              onChange={(e) => setSelectedCurrency(e.target.value)}
              placeholder="e.g. USD"
              className="w-[180px]"
            />
          </div>
          <div>
            <Label htmlFor="exchange-rate-input">Exchange Rate ({initialCurrency || "USD"} - {selectedCurrency}):</Label>
            <div className="flex items-center gap-2"> {/* New div to hold input and button */}
              <Input
                id="exchange-rate-input"
                type="number"
                value={exchangeRate}
                onChange={(e) => setExchangeRate(parseFloat(e.target.value))}
                placeholder="1.0"
                className="w-[180px]"
                min="0.01"
                step="0.01"
              />
              <Button
                variant="outline"
                size="icon"
                onClick={handleRefreshExchangeRate}
                disabled={refreshingRate || !selectedCurrency}
              >
                {refreshingRate ? (
                  <RefreshCw className="h-4 w-4 animate-spin" />
                ) : (
                  <RefreshCw className="h-4 w-4" />
                )}
              </Button>
            </div>
          </div>
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
