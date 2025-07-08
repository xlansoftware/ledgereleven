import { Transaction, TransactionDetails } from "@/lib/types";
import { Card, CardContent } from "../ui/card";
import { useState } from "react";
import { count, formatCurrency, formatDate, invertColor } from "@/lib/utils";
import TransactionRowMenu from "./TransactionRowMenu";
import DetailRowMenu from "./DetailRowMenu";
import { useCategoryStore } from "@/lib/store-category";
import { Badge } from "../ui/badge";
import { getIcon } from "@/lib/getIcon";

interface TransactionRowProps {
  transaction: Transaction;
  expanded?: boolean;
}

function convertValue(
  value: number,
  exchangeRate?: number,
  _currency?: string
) {
  const convertedValue = value * (exchangeRate ?? 1.0);
  return formatCurrency(convertedValue, 2);
}

function transactionValue(transaction: Transaction) {
  return convertValue(
    transaction.value || 0,
    transaction.exchangeRate,
    transaction.currency
  );
}

function detailValue(detail: TransactionDetails, transaction: Transaction) {
  if (!detail.value) return "N/A";
  const totalValue = detail.value * (detail.quantity || 1);
  return convertValue(
    totalValue || 0,
    transaction.exchangeRate,
    transaction.currency
  );
}

function explainValue(value: number, exchangeRate?: number, currency?: string) {
  if (!currency) return null;
  if (!exchangeRate) return null;
  return `${formatCurrency(value, 2)} x ${exchangeRate ?? 1.0} ${currency}`;
}

function explainTransactionValue(transaction: Transaction) {
  return explainValue(
    transaction.value || 0,
    transaction.exchangeRate,
    transaction.currency
  );
}

// function explainDetailValue(
//   detail: TransactionDetails,
//   transaction: Transaction
// ) {
//   if (!detail.value) return "N/A";
//   const totalValue = detail.value * (detail.quantity || 1);
//   return explainValue(
//     totalValue || 0,
//     transaction.exchangeRate,
//     transaction.currency
//   );
// }

export default function TransactionRow({
  transaction,
  expanded,
}: TransactionRowProps) {
  const { categoryById } = useCategoryStore();
  const [expandedDetails, setExpandedDetails] = useState(expanded || false);
  const hasDetails =
    transaction.transactionDetails && transaction.transactionDetails.length > 0;
  const transactionCategory = categoryById(transaction.categoryId);
  const category =
    transactionCategory?.name ||
    (hasDetails &&
      transaction.transactionDetails
        ?.reduce((acc, detail) => {
          const detailCategory = categoryById(detail.categoryId);
          if (detailCategory && !acc.includes(detailCategory.name)) {
            acc.push(detailCategory.name);
          }
          return acc;
        }, [] as string[])
        .join(", "));
  const countItems = hasDetails
    ? `(${count(transaction.transactionDetails?.length, "item", "items")})`
    : "";
  const title = transaction.notes || category || "No category";
  const Icon = getIcon(transactionCategory?.icon);
  return (
    <Card className="overflow-hidden border-t border-b border-l-0 border-r-0 rounded-none">
      <CardContent className="p-0">
        <div className="flex flex-row">
          <div
            className="p-2 bg-muted flex items-center"
            style={
              transactionCategory?.color
                ? {
                    backgroundColor: transactionCategory.color,
                    color: invertColor(transactionCategory.color),
                  }
                : {}
            }
          >
            {transactionCategory?.icon && (
              <div className="flex-shrink-0">
                <Icon />
              </div>
            )}
          </div>
          <div className="pl-2 pt-1 pb-1 flex-grow flex items-start justify-between overflow-hidden">
            <div
              className="flex items-start gap-2 cursor-pointer truncate"
              onClick={() => hasDetails && setExpandedDetails(!expandedDetails)}
            >
              <div className="flex flex-row gap-2 items-center">
                <div>
                  <div className="flex items-center gap-2">
                    <div className="font-medium">{title}</div>
                    <Badge variant="secondary">{countItems}</Badge>
                  </div>
                  <div className="flex flex-wrap gap-x-2 text-sm text-muted-foreground overflow-hidden">
                    <div className="truncate overflow-hidden text-ellipsis whitespace-nowrap">
                      {transaction.user}
                    </div>
                    <div className="truncate overflow-hidden text-ellipsis whitespace-nowrap">
                      {formatDate(transaction.date!)}
                    </div>
                  </div>
                  <div className="text-sm text-muted-foreground">
                    {category}
                  </div>
                </div>
              </div>
            </div>

            <div className="flex flex-col flex-shrink-0 items-end">
              <div className="flex items-center gap-2 flex-shrink-0">
                <div className="text-right">
                  <div className="font-bold">
                    {transactionValue(transaction)}
                  </div>
                </div>

                <TransactionRowMenu transaction={transaction} />
              </div>
              <div className="pr-2 text-sm text-muted-foreground">
                {explainTransactionValue(transaction)}
              </div>
            </div>
          </div>
        </div>

        {hasDetails && expandedDetails && (
          <div className="border-t bg-muted/40">
            {transaction.transactionDetails!.map((detail) => (
              <div
                key={detail.id}
                className="pt-2 pb-2 pl-12 border-b last:border-b-0"
              >
                <div className="flex items-start justify-between gap-2">
                  <div className="flex-grow">
                    <div className="font-medium">
                      {detail.description || "Unnamed Item"}
                    </div>
                    <div className="text-sm text-muted-foreground">
                      {categoryById(detail.categoryId)?.name || "No category"}
                    </div>
                  </div>

                  <div className="text-right">
                    <div className="font-bold">
                      {detailValue(detail, transaction)}
                    </div>

                    {detail.value && detail.quantity && (
                      <div className="text-xs text-muted-foreground">
                        {formatCurrency(detail.value)} × {detail.quantity}
                      </div>
                    )}
                  </div>
                  <DetailRowMenu transaction={transaction} detail={detail} />
                </div>
              </div>
            ))}
          </div>
        )}
      </CardContent>
    </Card>
  );
}
