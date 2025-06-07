import {
    Dialog,
    DialogContent,
    DialogTitle,
    DialogFooter,
    DialogHeader,
  } from "../ui/dialog";
  import { Button } from "../ui/button";
  import { ScrollArea } from "../ui/scroll-area";
import { useTransactionStore } from "@/lib/store-transaction";
import TransactionRow from "./TransactionRow";
  
  interface ConfirmTransactionFormProps {
    id: number;
    onConfirm?: () => void;
    onUndo?: () => void;
  }
  
  export default function ConfirmTransactionForm({
    id,
    onConfirm,
    onUndo,
  }: ConfirmTransactionFormProps) {
    const { transactions } = useTransactionStore();
    const transaction = transactions.find((t) => t.id === id)!;

    return (
      <Dialog open={true}>
        <DialogContent className="flex flex-col h-[90vh]">
          <DialogHeader>
            <DialogTitle>Confirm Transaction</DialogTitle>
          </DialogHeader>
          <div className="flex-1 overflow-hidden">
            <ScrollArea className="h-full w-full">
              <TransactionRow transaction={transaction} expanded={true} />
            </ScrollArea>
          </div>
          <DialogFooter>
            <Button variant={"secondary"} onClick={onUndo}>Undo</Button>
            <Button onClick={onConfirm}>Confirm</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    );
  }
  