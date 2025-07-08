import { useState, useEffect, useRef } from "react";
import {
  Drawer,
  DrawerContent,
  DrawerHeader,
  DrawerTitle,
  DrawerFooter,
} from "@/components/ui/drawer";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";

interface ExchangeRateDialogProps {
  isOpen: boolean;
  onConfirm: (result: {
    value: number;
    exchangeRate: number;
    currency: string;
  }) => void;
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
  ...props
}: ExchangeRateDialogProps) {
  const [value, setValue] = useState<number>(props.value ?? 42);
  const [exchangeRate, setExchangeRate] = useState<number>(1.0);
  const [result, setResult] = useState<number>(0.0);
  const exchangeRateRef = useRef<HTMLInputElement | null>(null);

  useEffect(() => {
    setValue(props.value ?? 42);
    setExchangeRate(1.0);
  }, [props.value, props.currency]);

  // useEffect(() => {
  //   if (isOpen && exchangeRateRef.current) {
  //     exchangeRateRef.current.select();
  //   }
  // }, [isOpen]);

  if (!isOpen) return null;

  return (
    <Drawer direction="top"  open={isOpen} onOpenChange={(open) => !open && onCancel()}>
      <DrawerContent>
        <DrawerHeader>
          <DrawerTitle>Exchange Rate for {props.ledgerCurrency || "USD"} to {props.currency}</DrawerTitle>
        </DrawerHeader>

        <div className="grid gap-4 px-4 pb-4">
          <div>
            <Label htmlFor="value">Value ({props.currency})</Label>
            <Input
              id="value"
              type="number"
              className="text-xl"
              value={value}
              onChange={(e) => {
                const val = parseFloat(e.target.value) || 0;
                setValue(val);
                setResult(parseFloat((val * exchangeRate).toFixed(2)));
              }}
            />
          </div>

          <div>
            <Label htmlFor="exchange-rate">Exchange Rate</Label>
            <Input
              autoFocus
              ref={exchangeRateRef}
              id="exchange-rate"
              type="number"
              step="0.0001"
              className="text-xl"
              value={exchangeRate || ""}
              // onFocus={(e) => e.target.select()}
              onChange={(e) => {
                const rate = parseFloat(e.target.value) || 0;
                setExchangeRate(rate);
                setResult(parseFloat((value * rate).toFixed(2)));
              }}
            />
          </div>

          <div>
            <Label htmlFor="result">Result ({props.ledgerCurrency})</Label>
            <Input
              id="result"
              type="number"
              step="0.0001"
              className="text-xl"
              value={result}
              onChange={(e) => {
                const res = parseFloat(e.target.value) || 0;
                setResult(res);
                setExchangeRate(parseFloat((res / value).toFixed(4)));
              }}
            />
          </div>
        </div>

        <DrawerFooter className="px-4">
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
        </DrawerFooter>
      </DrawerContent>
    </Drawer>
  );
}
