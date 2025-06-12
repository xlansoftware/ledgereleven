"use client";

import React from "react";
import { BarChart, Bar, XAxis, CartesianGrid } from "recharts";
import { formatCurrency } from "@/lib/utils";
import { Category } from "@/lib/types";
import { getIcon } from "@/lib/getIcon";
import { ChartTooltip, ChartTooltipContent } from "../ui/chart";

export interface IncomeComponentProps {
  data: Record<string, number>;
  title: string;
  categories: Record<string, Category>;
}

export const IncomeComponent: React.FC<IncomeComponentProps> = ({
  data,
  categories,
}) => {
  const chartData = Object.entries(data).map(([name, value]) => ({
    name,
    value,
    color: categories[name]?.color || "#8884d8",
    icon: getIcon(categories[name]?.icon),
  }));

  const totalValue = chartData.reduce((sum, entry) => sum + entry.value, 0);

  return (
    <div>
      {chartData.length === 0 ? (
        <p className="text-muted-foreground text-center">&nbsp;</p>
      ) : (
        <div className="relative h-full w-20 flex flex-col items-center">
          <h2 className="text-xl font-semibold mb-4">
            {formatCurrency(totalValue)}
          </h2>
          <BarChart
            accessibilityLayer
            data={[{...data,
                total: totalValue}
            ]}
            // margin={{ top: 10, right: 30, left: 0, bottom: 30 }}
          >
            <CartesianGrid vertical={false} />
            <XAxis
              dataKey="total"
              tickLine={false}
              tickMargin={10}
              axisLine={false}
              tickFormatter={(value) => value.slice(0, 3)}
            />
            <ChartTooltip content={<ChartTooltipContent hideLabel />} />
            {chartData.map((entry) => (
              <Bar
                dataKey={entry.name}
                stackId="a"
                fill={entry.color}
                radius={[4, 4, 0, 0]}
              />
            ))}
          </BarChart>
        </div>
      )}
    </div>
  );
};
