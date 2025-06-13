"use client";

import React from "react";
import { BarChart, Bar, XAxis, CartesianGrid } from "recharts";
// import { formatCurrency } from "@/lib/utils";
import { Category } from "@/lib/types";
import { getIcon } from "@/lib/getIcon";
import { ChartContainer, ChartTooltip, ChartTooltipContent } from "../ui/chart";

export interface IncomeComponentProps {
  data: Record<string, number>;
  title: string;
  categories: Record<string, Category>;
}

export const IncomeComponent: React.FC<IncomeComponentProps> = ({
  data,
  categories,
}) => {
  // const chartData = Object.entries(data).map(([name, value]) => ({
  //   name,
  //   value,
  //   color: categories[name]?.color || "#8884d8",
  //   icon: getIcon(categories[name]?.icon),
  // }));

  // const totalValue = Object.entries(data).reduce((sum, [, value]) => sum + value, 0);
  // debugger;
  const chartData = [
    { month: "Income", desktop: 186, mobile: 80 },
  ];

  const chartConfig2 = Object.entries(data).reduce((acc, [name]) => {
    return {
      ...acc,
      [name]: {
        label: name,
        color: categories[name]?.color || "#8884d8",
        icon: getIcon(categories[name]?.icon),
      }
    }
  }, {});

  const data2 = {
    ...data,
    total: "Income"
  };

  // const chartConfig = {
  //   desktop: {
  //     label: "Desktop",
  //     color: "var(--chart-1)",
  //   },
  //   mobile: {
  //     label: "Mobile",
  //     color: "var(--chart-2)",
  //   },
  // };

  return (
    <div>
      {chartData.length === 0 ? (
        <p className="text-muted-foreground text-center">&nbsp;</p>
      ) : (
        <div className="relative h-64 w-20 flex flex-col items-center">
          <ChartContainer config={chartConfig2} className="min-h-64 w-full">
            <BarChart accessibilityLayer data={[data2]}>
              <CartesianGrid vertical={false} />
              <XAxis
                dataKey="total"
                tickLine={false}
                tickMargin={10}
                axisLine={false}
                // tickFormatter={(value) => value.slice(0, 3)}
              />
              <ChartTooltip content={<ChartTooltipContent hideLabel />} />
              {Object.entries(data).map(([name], index) => <Bar
                dataKey={name}
                stackId="a"
                fill={categories[name]?.color || "#8884d8"}
                radius={index === 0 ? [0, 0, 4, 4] : [4, 4, 0, 0]}
              />)}
              {/* <Bar
                dataKey="desktop"
                stackId="a"
                fill="var(--color-desktop)"
                radius={[0, 0, 4, 4]}
              />
              <Bar
                dataKey="mobile"
                stackId="a"
                fill="var(--color-mobile)"
                radius={[4, 4, 0, 0]}
              /> */}
            </BarChart>
          </ChartContainer>
        </div>
      )}
    </div>
  );
};
