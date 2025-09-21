import {
  Card,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import type { ReactNode } from "react";

interface DataCardProps {
  title: string;
  children: ReactNode;
}

export function DataCard({ title, children}: DataCardProps) {
  return (
    <Card className="py-4">
      <CardHeader className="px-4">
        <CardDescription>{title}</CardDescription>
        <CardTitle className="text-xl font-semibold tabular-nums text-right">
          {children}
        </CardTitle>
        {/* <CardContent className="flex flex-col">
          
        </CardContent> */}
      </CardHeader>
    </Card>
  );
}
