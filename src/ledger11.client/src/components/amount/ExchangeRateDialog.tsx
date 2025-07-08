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
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";

interface ExchangeRateDialogProps {
  isOpen: boolean;
  onConfirm: (result: { value: number; exchangeRate: number; currency: string }) => void;
  onCancel: () => void;
  title: string;
  description: string;
  ledgerCurrency?: string;
  value?: number;
  currency?: string;
}

export function ExchangeRateDialog({
  isOpen,
  onConfirm,
  onCancel,
  title,
  description,
  ...props
}: ExchangeRateDialogProps) {
  const [value, setValue] = useState<number>(props.value ?? 42);
  const [exchangeRate, setExchangeRate] = useState<number>(1.0);
  const [result, setResult] = useState<number>(0.0);

  useEffect(() => {
    setValue(props.value ?? 42);
    setExchangeRate(1.0);
  }, [props.value, props.currency]);

  if (!isOpen) return null;

  return (
    <Dialog open={isOpen} onOpenChange={(open) => !open && onCancel()}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{title}</DialogTitle>
          <DialogDescription>Provide exchange rate for the <span className="font-bold">{props.ledgerCurrency}</span> to <span className="font-bold">{props.currency}</span> conversion:</DialogDescription>
        </DialogHeader>

        <div className="grid gap-4 py-4">
          <div>
            <Label htmlFor="value">Value ({props.currency})</Label>
            <Input
              id="value"
              type="number"
              value={value}
              onChange={(e) => {
                setValue(parseFloat(e.target.value) || 0);
                setResult(parseFloat((parseFloat(e.target.value) * exchangeRate).toFixed(2)));
              }}
            />
          </div>

          <div>
            <Label htmlFor="exchange-rate">Exchange Rate</Label>
            <Input
              autoFocus
              id="exchange-rate"
              type="number"
              step="0.0001"
              value={exchangeRate}
              onChange={(e) => {
                setExchangeRate(parseFloat(e.target.value) || 0);
                setResult(parseFloat((value * parseFloat(e.target.value)).toFixed(2)));
              }}
            />
          </div>

          <div>
            <Label htmlFor="result">Result ({props.ledgerCurrency})</Label>
            <Input
              id="result"
              type="number"
              step="0.0001"
              value={result}
              onChange={(e) => {
                setResult(parseFloat(e.target.value) || 0);
                setExchangeRate(parseFloat((parseFloat(e.target.value) / value).toFixed(4)));
              }}
            />
          </div>
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={onCancel}>
            Cancel
          </Button>
          <Button
            onClick={() =>
              onConfirm({
                value,
                exchangeRate,
                currency: props.currency || "USD",
              })
            }
          >
            Confirm
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
