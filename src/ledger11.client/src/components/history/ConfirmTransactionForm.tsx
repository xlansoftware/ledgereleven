import {
    Dialog,
    DialogContent,
    DialogTitle,
    DialogFooter,
    DialogHeader,
  } from "../ui/dialog";
  import { Button } from "../ui/button";
  import { ScrollArea } from "../ui/scroll-area";
import TransactionRow from "./TransactionRow";
import { type Transaction } from "@/lib/types";
  
  interface ConfirmTransactionFormProps {
    transaction: Transaction;
    onConfirm?: () => void;
    onUndo?: () => void;
  }
  
  export default function ConfirmTransactionForm({
    transaction,
    onConfirm,
    onUndo,
  }: ConfirmTransactionFormProps) {

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
  